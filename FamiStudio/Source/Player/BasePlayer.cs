using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace FamiStudio
{
    public enum LoopMode
    {
        LoopPoint,
        Song,
        Pattern,
        None,
        Max
    };

    public interface IPlayerInterface
    {
        void NotifyInstrumentLoaded(Instrument instrument, int channelTypeMask);
        void NotifyRegisterWrite(int apuIndex, int reg, int data);
    }

    public class BasePlayer : IPlayerInterface
    {
        protected int apuIndex;
        protected NesApu.DmcReadDelegate dmcCallback;
        protected int famitrackerTempoCounter = 0;
        protected int playPattern = 0;
        protected int playNote = 0;
        protected int famitrackerSpeed = 6;
        protected byte[] tempoEnvelope;
        protected int tempoEnvelopeIndex;
        protected int tempoEnvelopeCounter;
        protected int sampleRate;
        protected int loopCount = 0;
        protected int maxLoopCount = -1;
        protected int frameNumber = 0;
        protected bool famitrackerTempo = true;
        protected bool palPlayback = false;
        protected Song song;
        protected ChannelState[] channelStates;
        protected LoopMode loopMode = LoopMode.Song;
        protected int channelMask = 0xffff;
        protected int playPosition = 0;

        protected BasePlayer(int apu, int rate = 44100)
        {
            apuIndex = apu;
            sampleRate = rate;
            dmcCallback = new NesApu.DmcReadDelegate(NesApu.DmcReadCallback);
        }

        public virtual void Shutdown()
        {
        }

        public int ChannelMask
        {
            get { return channelMask; }
            set { channelMask = value; }
        }

        public LoopMode Loop
        {
            get { return loopMode; }
            set { loopMode = value; }
        }

        public int CurrentFrame
        {
            get { return Math.Max(0, playPosition); }
            set { playPosition = value; }
        }

        // Returns the number of frames to run (0, 1 or 2)
        public int UpdateFamiStudioTempo()
        {
            if (famitrackerTempo || song.Project.PalMode == palPlayback)
            {
                return 1;
            }
            else
            {
                if (--tempoEnvelopeCounter <= 0)
                {
                    tempoEnvelopeIndex++;

                    if (tempoEnvelope[tempoEnvelopeIndex] == 0x80)
                        tempoEnvelopeIndex = 1;

                    tempoEnvelopeCounter = tempoEnvelope[tempoEnvelopeIndex];

#if FALSE //DEBUG
                    if (song.Project.PalMode)
                        Debug.WriteLine("*** Will do nothing for 1 frame!");
                    else
                        Debug.WriteLine("*** Will run 2 frames!"); 
#endif

                    // A NTSC song playing on PAL will sometimes need to run 2 frames to keep up.
                    // A PAL song playing on NTSC will sometimes need to do nothing for 1 frame to keep up.
                    return palPlayback ? 2 : 0;
                }
                else
                {
                    return 1;
                }
            }
        }

        private void ResetFamiStudioTempo(bool force)
        {
            if (!famitrackerTempo)
            {
                var newNoteLength = song.GetPatternNoteLength(playPattern);
                var newTempoEnvelope = FamiStudioTempoUtils.GetTempoEnvelope(newNoteLength, song.Project.PalMode);

                if (newTempoEnvelope != tempoEnvelope || force)
                {
                    tempoEnvelope = newTempoEnvelope;
                    tempoEnvelopeCounter = tempoEnvelope[0];
                    tempoEnvelopeIndex = 0;
                }
            }
        }

        public bool ShouldFamitrackerTempoAdvance()
        {
            return !famitrackerTempo || famitrackerTempoCounter <= 0;
        }

        public void UpdateFamitrackerTempo(int speed, int tempo)
        {
            if (famitrackerTempo)
            {
                // Tempo/speed logic straight from Famitracker.
                var tempoDecrement = (tempo * 24) / speed;
                var tempoRemainder = (tempo * 24) % speed;

                if (famitrackerTempoCounter <= 0)
                {
                    int ticksPerSec = palPlayback ? 50 : 60;
                    famitrackerTempoCounter += (60 * ticksPerSec) - tempoRemainder;
                }
                famitrackerTempoCounter -= tempoDecrement;
            }
        }

        protected void AdvanceChannels()
        {
            foreach (var channel in channelStates)
            {
                channel.Advance(song, playPattern, playNote, ref famitrackerSpeed);
            }
        }

        protected void UpdateChannels()
        {
            foreach (var channel in channelStates)
            {
                channel.Update();
            }
        }

        protected void UpdateChannelsMuting()
        {
            for (int i = 0; i < channelStates.Length; i++)
            {
                NesApu.EnableChannel(apuIndex, i, (channelMask & (1 << i)));
            }
        }

        public bool BeginPlaySong(Song s, bool pal, int startNote)
        {
            song = s;
            famitrackerTempo = song.UsesFamiTrackerTempo;
            famitrackerSpeed = song.FamitrackerSpeed;
            palPlayback = pal;
            playPosition = startNote;
            playPattern = 0;
            playNote = 0;
            frameNumber = 0;
            famitrackerTempoCounter = 0;
            channelStates = CreateChannelStates(this, song.Project, apuIndex, song.Project.ExpansionNumChannels, palPlayback);

            NesApu.InitAndReset(apuIndex, sampleRate, palPlayback, song.Project.ExpansionAudio, song.Project.ExpansionNumChannels, dmcCallback);

            ResetFamiStudioTempo(true);
            UpdateChannelsMuting();

            //Debug.WriteLine($"START SEEKING!!"); 

            if (startNote != 0)
            {
                NesApu.StartSeeking(apuIndex);

                AdvanceChannels();
                UpdateChannels();
                UpdateFamitrackerTempo(famitrackerSpeed, song.FamitrackerTempo);

                while (song.GetPatternStartNote(playPattern) + playNote < startNote)
                {
                    if (!PlaySongFrameInternal(true))
                        break;
                }

                NesApu.StopSeeking(apuIndex);
            }
            else
            {
                AdvanceChannels();
                UpdateChannels();
                UpdateFamitrackerTempo(famitrackerSpeed, song.FamitrackerTempo);
            }

            EndFrame();

            playPosition = song.GetPatternStartNote(playPattern) + playNote;

            return true;
        }

        protected bool PlaySongFrameInternal(bool seeking)
        {
            //Debug.WriteLine($"PlaySongFrameInternal {playPosition}!");
            //Debug.WriteLine($"PlaySongFrameInternal {song.GetPatternStartNote(playPattern) + playNote}!");

            // Increment before for register listeners to get correct frame number.
            frameNumber++;

            int numFramesToRun = UpdateFamiStudioTempo();

            for (int i = 0; i < numFramesToRun; i++)
            {
                if (ShouldFamitrackerTempoAdvance())
                {
                    //Debug.WriteLine($"  Seeking Frame {song.GetPatternStartNote(playPattern) + playNote}!");

                    if (!AdvanceSong(song.Length, seeking ? LoopMode.None : loopMode))
                        return false;

                    AdvanceChannels();
                }

                UpdateChannels();
                UpdateFamitrackerTempo(famitrackerSpeed, song.FamitrackerTempo);

#if DEBUG
                if (i > 0)
                {
                    var noteLength = song.GetPatternNoteLength(playPattern);
                    if ((playNote % noteLength) == 0 && noteLength != 1)
                        Debug.WriteLine("*********** INVALID SKIPPED NOTE!");
                }
#endif
            }

            if (!seeking)
                playPosition = song.GetPatternStartNote(playPattern) + playNote;

            return true;
        }

        public bool PlaySongFrame()
        {
            if (!PlaySongFrameInternal(false))
                return false;

            UpdateChannelsMuting();
            EndFrame();

            return true;
        }

        public bool AdvanceSong(int songLength, LoopMode loopMode)
        {
            bool advancedPattern = false;
            bool forceResetTempo = false;

            if (++playNote >= song.GetPatternLength(playPattern))
            {
                playNote = 0;
                if (loopMode != LoopMode.Pattern)
                {
                    playPattern++;
                    advancedPattern = true;
                    forceResetTempo = playPattern == song.LoopPoint;
                }
            }

            if (playPattern >= songLength)
            {
                loopCount++;

                if (maxLoopCount > 0 && loopCount >= maxLoopCount)
                {
                    return false;
                }

                if (loopMode == LoopMode.LoopPoint) // This loop mode is actually unused.
                {
                    if (song.LoopPoint >= 0)
                    {
                        playPattern = song.LoopPoint;
                        playNote = 0;
                        advancedPattern = true;
                        forceResetTempo = true;
                    }
                    else 
                    {
                        return false;
                    }
                }
                else if (loopMode == LoopMode.Song)
                {
                    playPattern = Math.Max(0, song.LoopPoint);
                    playNote = 0;
                    advancedPattern = true;
                    forceResetTempo = true;
                }
                else if (loopMode == LoopMode.None)
                {
                    return false;
                }
            }

            if (advancedPattern)
                ResetFamiStudioTempo(forceResetTempo);

            return true;
        }

        private ChannelState CreateChannelState(int apuIdx, int channelType, int expNumChannels, bool pal)
        {
            switch (channelType)
            {
                case ChannelType.Square1:
                case ChannelType.Square2:
                    return new ChannelStateSquare(this, apuIdx, channelType, pal);
                case ChannelType.Triangle:
                    return new ChannelStateTriangle(this, apuIdx, channelType, pal);
                case ChannelType.Noise:
                    return new ChannelStateNoise(this, apuIdx, channelType, pal);
                case ChannelType.Dpcm:
                    return new ChannelStateDpcm(this, apuIdx, channelType, pal);
                case ChannelType.Vrc6Square1:
                case ChannelType.Vrc6Square2:
                    return new ChannelStateVrc6Square(this, apuIdx, channelType);
                case ChannelType.Vrc6Saw:
                    return new ChannelStateVrc6Saw(this, apuIdx, channelType);
                case ChannelType.Vrc7Fm1:
                case ChannelType.Vrc7Fm2:
                case ChannelType.Vrc7Fm3:
                case ChannelType.Vrc7Fm4:
                case ChannelType.Vrc7Fm5:
                case ChannelType.Vrc7Fm6:
                    return new ChannelStateVrc7(this, apuIdx, channelType);
                case ChannelType.FdsWave:
                    return new ChannelStateFds(this, apuIdx, channelType);
                case ChannelType.Mmc5Square1:
                case ChannelType.Mmc5Square2:
                    return new ChannelStateMmc5Square(this, apuIdx, channelType);
                case ChannelType.N163Wave1:
                case ChannelType.N163Wave2:
                case ChannelType.N163Wave3:
                case ChannelType.N163Wave4:
                case ChannelType.N163Wave5:
                case ChannelType.N163Wave6:
                case ChannelType.N163Wave7:
                case ChannelType.N163Wave8:
                    return new ChannelStateN163(this, apuIdx, channelType, expNumChannels, pal);
                case ChannelType.S5BSquare1:
                case ChannelType.S5BSquare2:
                case ChannelType.S5BSquare3:
                    return new ChannelStateS5B(this, apuIdx, channelType, pal);
            }

            Debug.Assert(false);
            return null;
        }

        public ChannelState[] CreateChannelStates(IPlayerInterface player, Project project, int apuIdx, int expNumChannels, bool pal)
        {
            var channelCount = project.GetActiveChannelCount();
            var states = new ChannelState[channelCount];

            int idx = 0;
            for (int i = 0; i < ChannelType.Count; i++)
            {
                if (project.IsChannelActive(i))
                {
                    var state = CreateChannelState(apuIdx, i, expNumChannels, pal);
                    states[idx++] = state;
                }
            }

            return states;
        }
        
        protected virtual unsafe short[] EndFrame()
        {
            NesApu.EndFrame(apuIndex);

            int numTotalSamples = NesApu.SamplesAvailable(apuIndex);
            short[] samples = new short[numTotalSamples];

            fixed (short* ptr = &samples[0])
            {
                NesApu.ReadSamples(apuIndex, new IntPtr(ptr), numTotalSamples);
            }

            return samples;
        }

        public void NotifyInstrumentLoaded(Instrument instrument, int channelTypeMask)
        {
            foreach (var channelState in channelStates)
            {
                if (((1 << channelState.GetChannelType()) & channelTypeMask) != 0)
                {
                    channelState.IntrumentLoadedNotify(instrument);
                }
            }
        }

        public virtual void NotifyRegisterWrite(int apuIndex, int reg, int data)
        {
        }
    };
}
