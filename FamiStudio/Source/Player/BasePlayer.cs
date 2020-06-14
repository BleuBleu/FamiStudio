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

    public class BasePlayer
    {
        protected const int SampleRate = 44100;

        // NSTC: 734 = ceil(SampleRate / FrameRate) = ceil(44100 / 60.0988).
        // PAL:  882 = ceil(SampleRate / FrameRate) = ceil(44100 / 50.0070).
        protected const int BufferSize = 882 * sizeof(short);
#if FAMISTUDIO_LINUX
        protected const int NumAudioBuffers = 4; // ALSA seems to like to have one extra buffer.
#else
        protected const int NumAudioBuffers = 3;
#endif

        protected int apuIndex;
        protected NesApu.DmcReadDelegate dmcCallback;
        protected int tempoCounter = 0;
        protected int playPattern = 0;
        protected int playNote = 0;
        protected int famitrackerSpeed = 6;
        protected int famitrackerNativeTempo = Song.NativeTempoNTSC;
        protected byte[] tempoEnvelope;
        protected int tempoEnvelopeIndex;
        protected int tempoEnvelopeCounter;
        protected bool famitrackerTempo = true;
        protected bool palMode = false;
        protected bool firstFrame = false;
        protected Song song;
        protected ChannelState[] channelStates;
        protected LoopMode loopMode = LoopMode.Song;
        protected int channelMask = 0xffff;
        protected int playPosition = 0;

        protected BasePlayer(int apu)
        {
            apuIndex = apu;
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

        public bool UpdateFrameSkip()
        {
            if (famitrackerTempo)
            {
                return false;
            }
            else
            {
                if (--tempoEnvelopeCounter <= 0)
                {
                    tempoEnvelopeIndex++;

                    if (tempoEnvelope[tempoEnvelopeIndex] == 0x80)
                        tempoEnvelopeIndex = 1;

                    tempoEnvelopeCounter = tempoEnvelope[tempoEnvelopeIndex];

#if DEBUG
                    var noteLength = song.GetPatternNoteLength(playPattern);
                    Debug.Assert((playNote % noteLength) != 0 || noteLength == 1);
                    //Debug.WriteLine("*** SKIP!"); 
#endif

                    return palMode;
                }
                else
                {
                    return false;
                }
            }
        }

        public bool UpdateTempo(int speed, int tempo)
        {
            if (famitrackerTempo)
            {
                // Tempo/speed logic straight from Famitracker.
                var tempoDecrement = (tempo * 24) / speed;
                var tempoRemainder = (tempo * 24) % speed;

                if (tempoCounter <= 0)
                {
                    int ticksPerSec = palMode ? 50 : 60;
                    tempoCounter += (60 * ticksPerSec) - tempoRemainder;
                }
                tempoCounter -= tempoDecrement;

                return tempoCounter <= 0;
            }
            else
            {
                return true;
            }
        }

        private void ResetFamiStudioTempo(bool force)
        {
            if (!famitrackerTempo)
            {
                var newNoteLength = song.GetPatternNoteLength(playPattern);
                var newPalSkipEnvelope = FamiStudioTempoUtils.GetPalSkipEnvelope(newNoteLength);

                if (newPalSkipEnvelope != tempoEnvelope || force)
                {
                    tempoEnvelope = newPalSkipEnvelope;
                    tempoEnvelopeCounter = tempoEnvelope[0];
                    tempoEnvelopeIndex = 0;
                }
            }
        }

        public bool BeginPlaySong(Song s, bool pal, int startNote, IRegisterListener listener = null)
        {
            song = s;
            famitrackerTempo = song.UsesFamiTrackerTempo;
            famitrackerSpeed = song.FamitrackerSpeed;
            famitrackerNativeTempo = pal ? Song.NativeTempoPAL : Song.NativeTempoNTSC;
            palMode = pal;
            playPosition = startNote;
            playPattern = 0;
            playNote = 0;
            tempoCounter = 0;
            firstFrame = true;
            ResetFamiStudioTempo(true);
            channelStates = CreateChannelStates(song.Project, apuIndex, song.Project.ExpansionNumChannels, palMode, listener);

            NesApu.InitAndReset(apuIndex, SampleRate, palMode, GetNesApuExpansionAudio(song.Project), song.Project.ExpansionNumChannels, dmcCallback);

            if (startNote != 0)
            {
                NesApu.StartSeeking(apuIndex);
#if DEBUG
                NesApu.seeking = true;
#endif

                while (song.GetPatternStartNote(playPattern) + playNote < startNote)
                {
                    foreach (var channel in channelStates)
                    {
                        channel.Advance(song, playPattern, playNote, famitrackerSpeed, famitrackerNativeTempo);
                        channel.ProcessEffects(song, playPattern, playNote, ref famitrackerSpeed);
                        channel.UpdateEnvelopes();
                        channel.UpdateAPU();
                    }

                    if (!AdvanceSong(song.Length, LoopMode.None))
                        return false;

                    //Debug.WriteLine($"Seeking Frame {song.GetPatternStartNote(playPattern) + playNote}!"); 

                    UpdateFrameSkip();
                }

                NesApu.StopSeeking(apuIndex);
#if DEBUG
                NesApu.seeking = false;
#endif
            }

            return true;
        }

        public bool PlaySongFrame()
        {
            do
            {
                if (firstFrame || UpdateTempo(famitrackerSpeed, song.FamitrackerTempo))
                {
                    // Advance to next note.
                    if (!firstFrame && !AdvanceSong(song.Length, loopMode))
                        return false;

                    foreach (var channel in channelStates)
                    {
                        channel.Advance(song, playPattern, playNote, famitrackerSpeed, famitrackerNativeTempo);
                        channel.ProcessEffects(song, playPattern, playNote, ref famitrackerSpeed);
                    }

                    playPosition = song.GetPatternStartNote(playPattern) + playNote;
                }

                // Debug.WriteLine($"Running Frame {playPosition}!"); 

                // Update envelopes + APU registers.
                foreach (var channel in channelStates)
                {
                    channel.UpdateEnvelopes();
                    channel.UpdateAPU();
                }
            }
            while (!firstFrame && UpdateFrameSkip());

            firstFrame = false;

            // Mute.
            for (int i = 0; i < channelStates.Length; i++)
            {
                NesApu.EnableChannel(apuIndex, i, (channelMask & (1 << i)));
            }

            EndFrame();

            return true;
        }

        public bool AdvanceSong(int songLength, LoopMode loopMode)
        {
            bool resetTempo = false;

            if (++playNote >= song.GetPatternLength(playPattern))
            {
                playNote = 0;
                if (loopMode != LoopMode.Pattern)
                {
                    playPattern++;
                    resetTempo = playPattern == song.LoopPoint;
                }
            }

            if (playPattern >= songLength)
            {
                if (loopMode == LoopMode.LoopPoint)
                {
                    if (song.LoopPoint >= 0)
                    {
                        playPattern = song.LoopPoint;
                        playNote = 0;
                        resetTempo = true;
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
                    resetTempo = true;
                }
                else if (loopMode == LoopMode.None)
                {
                    return false;
                }
            }

            if (resetTempo)
                ResetFamiStudioTempo(resetTempo);

            return true;
        }

        private ChannelState CreateChannelState(int apuIdx, int channelType, int expNumChannels, bool pal)
        {
            switch (channelType)
            {
                case Channel.Square1:
                case Channel.Square2:
                    return new ChannelStateSquare(apuIdx, channelType, pal);
                case Channel.Triangle:
                    return new ChannelStateTriangle(apuIdx, channelType, pal);
                case Channel.Noise:
                    return new ChannelStateNoise(apuIdx, channelType, pal);
                case Channel.Dpcm:
                    return new ChannelStateDpcm(apuIdx, channelType, pal);
                case Channel.Vrc6Square1:
                case Channel.Vrc6Square2:
                    return new ChannelStateVrc6Square(apuIdx, channelType);
                case Channel.Vrc6Saw:
                    return new ChannelStateVrc6Saw(apuIdx, channelType);
                case Channel.Vrc7Fm1:
                case Channel.Vrc7Fm2:
                case Channel.Vrc7Fm3:
                case Channel.Vrc7Fm4:
                case Channel.Vrc7Fm5:
                case Channel.Vrc7Fm6:
                    return new ChannelStateVrc7(apuIdx, channelType);
                case Channel.FdsWave:
                    return new ChannelStateFds(apuIdx, channelType);
                case Channel.Mmc5Square1:
                case Channel.Mmc5Square2:
                    return new ChannelStateMmc5Square(apuIdx, channelType);
                case Channel.N163Wave1:
                case Channel.N163Wave2:
                case Channel.N163Wave3:
                case Channel.N163Wave4:
                case Channel.N163Wave5:
                case Channel.N163Wave6:
                case Channel.N163Wave7:
                case Channel.N163Wave8:
                    return new ChannelStateN163(apuIdx, channelType, expNumChannels, pal);
                case Channel.S5BSquare1:
                case Channel.S5BSquare2:
                case Channel.S5BSquare3:
                    return new ChannelStateS5B(apuIdx, channelType, pal);
            }

            Debug.Assert(false);
            return null;
        }

        public ChannelState[] CreateChannelStates(Project project, int apuIdx, int expNumChannels, bool pal, IRegisterListener listener)
        {
            var channelCount = project.GetActiveChannelCount();
            var states = new ChannelState[channelCount];

            int idx = 0;
            for (int i = 0; i < Channel.Count; i++)
            {
                if (project.IsChannelActive(i))
                {
                    var state = CreateChannelState(apuIdx, i, expNumChannels, pal);

                    if (listener != null)
                        state.SetRegisterListener(listener);

                    states[idx++] = state;
                }
            }

            return states;
        }
        
        public int GetNesApuExpansionAudio(Project project)
        {
            switch (project.ExpansionAudio)
            {
                case Project.ExpansionNone:
                    return NesApu.APU_EXPANSION_NONE;
                case Project.ExpansionVrc6:
                    return NesApu.APU_EXPANSION_VRC6;
                case Project.ExpansionVrc7:
                    return NesApu.APU_EXPANSION_VRC7;
                case Project.ExpansionFds:
                    return NesApu.APU_EXPANSION_FDS;
                case Project.ExpansionMmc5:
                    return NesApu.APU_EXPANSION_MMC5;
                case Project.ExpansionN163:
                    return NesApu.APU_EXPANSION_NAMCO;
                case Project.ExpansionS5B:
                    return NesApu.APU_EXPANSION_SUNSOFT;
            }

            Debug.Assert(false);
            return 0;
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
    };
}
