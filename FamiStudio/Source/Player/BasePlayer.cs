using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

#if FAMISTUDIO_WINDOWS
using AudioStream = FamiStudio.XAudio2Stream;
#else
using AudioStream = FamiStudio.PortAudioStream;
#endif

namespace FamiStudio
{
    public enum LoopMode
    {
        None,
        Song,
        Pattern,
        Max
    };

    public class BasePlayer
    {
        protected const int SampleRate = 44100;

        // NSTC: 734 = ceil(SampleRate / FrameRate) = ceil(44100 / 60.0988).
        // PAL:  882 = ceil(SampleRate / FrameRate) = ceil(44100 / 50.0070).
        protected const int BufferSize = 882 * sizeof(short);
        protected const int NumAudioBuffers = 3;

        protected int apuIndex;
        protected NesApu.DmcReadDelegate dmcCallback;
        protected int tempoCounter = 0;
        protected int playPattern = 0;
        protected int playNote = 0;
        protected int speed = 6;
        protected bool palMode = false;
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

        public bool UpdateFamitrackerTempo(int speed, int tempo, ref int tempoCounter)
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

        public bool UpdateFamistudioTempo(int speed, ref int tempoCounter, ref int numFrames)
        {
            numFrames = (palMode && (tempoCounter % speed) == (speed - 2)) ? 2 : 1;
            tempoCounter += numFrames;
            return true;
        }

        public bool BeginPlaySong(Song s, bool pal, int startNote)
        {
            song = s;
            speed = song.Speed;
            palMode = pal;
            playPosition = startNote;
            channelStates = CreateChannelStates(song.Project, apuIndex, palMode);

            NesApu.InitAndReset(apuIndex, SampleRate, palMode, GetNesApuExpansionAudio(song.Project), dmcCallback);

            if (startNote != 0)
            {
                NesApu.StartSeeking(apuIndex);
#if DEBUG
                NesApu.seeking = true;
#endif

                while (playPattern * song.PatternLength + playNote < startNote)
                {
                    if (!AdvanceSong(song.Length, song.PatternLength, loopMode))
                        return false;

                    foreach (var channel in channelStates)
                    {
                        channel.Advance(song, playPattern, playNote);
                        channel.ProcessEffects(song, playPattern, playNote,ref speed);
                        channel.UpdateEnvelopes();
                        channel.UpdateAPU();
                    }
                }

                NesApu.StopSeeking(apuIndex);
#if DEBUG
                NesApu.seeking = false;
#endif
            }
            else
            {
                foreach (var channel in channelStates)
                {
                    channel.Advance(song, 0, 0);
                    channel.ProcessEffects(song, 0, 0, ref speed);
                }
            }

            return true;
        }

        public bool PlaySongFrame()
        {
            playPosition = playPattern * song.PatternLength + playNote;

            // Update envelopes + APU registers.
            foreach (var channel in channelStates)
            {
                channel.UpdateEnvelopes();
                channel.UpdateAPU();
            }

            // Mute.
            for (int i = 0; i < channelStates.Length; i++)
            {
                NesApu.EnableChannel(apuIndex, i, (channelMask & (1 << i)));
            }

            EndFrame();

            if (UpdateFamitrackerTempo(speed, song.Tempo, ref tempoCounter))
            //if (UpdateFamistudioTempo(6, ref tempoCounter, ref numFrames))
            {
                // Advance to next note.
                if (!AdvanceSong(song.Length, song.PatternLength, loopMode))
                    return false;

                foreach (var channel in channelStates)
                {
                    channel.Advance(song, playPattern, playNote);
                    channel.ProcessEffects(song, playPattern, playNote, ref speed);
                }
            }

            return true;
        }

        public bool AdvanceSong(int songLength, int patternLength, LoopMode loopMode)
        {
            if (++playNote >= patternLength)
            {
                playNote = 0;
                if (loopMode != LoopMode.Pattern)
                    playPattern++;
            }

            if (playPattern >= songLength)
            {
                if (loopMode == LoopMode.None)
                {
                    return false;
                }
                else if (loopMode == LoopMode.Song)
                {
                    playPattern = song.LoopPoint;
                    playNote = 0;
                }
            }

            return true;
        }

        private ChannelState CreateChannelState(int apuIdx, int channelType, bool pal)
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
                case Channel.NamcoWave1:
                case Channel.NamcoWave2:
                case Channel.NamcoWave3:
                case Channel.NamcoWave4:
                case Channel.NamcoWave5:
                case Channel.NamcoWave6:
                case Channel.NamcoWave7:
                case Channel.NamcoWave8:
                    return new ChannelStateNamco(apuIdx, channelType, pal);
                case Channel.SunsoftSquare1:
                case Channel.SunsoftSquare2:
                case Channel.SunsoftSquare3:
                    return new ChannelStateSunsoftSquare(apuIdx, channelType, pal);
            }

            Debug.Assert(false);
            return null;
        }

        public ChannelState[] CreateChannelStates(Project project, int apuIdx, bool pal)
        {
            var channelCount = project.GetActiveChannelCount();
            var states = new ChannelState[channelCount];

            int idx = 0;
            for (int i = 0; i < Channel.Count; i++)
            {
                if (project.IsChannelActive(i))
                    states[idx++] = CreateChannelState(apuIdx, i, pal);
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
                case Project.ExpansionNamco:
                    return NesApu.APU_EXPANSION_NAMCO;
                case Project.ExpansionSunsoft:
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
