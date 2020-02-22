using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FamiStudio
{ 
    public class FamiTrackerFile
    {
        const uint MinVersion = 0x0420;
        const uint MaxVersion = 0x0440;

        const int BlockNameLength = 16;

        const string FileHeaderId = "FamiTracker Module";
        const string FileEndId    = "END";

        const byte SndChip_NONE = 0;
        const byte SndChip_VRC6 = 1;  // Konami VRCVI
        const byte SndChip_VRC7 = 2;  // Konami VRCVII
        const byte SndChip_FDS  = 4;  // Famicom Disk Sound
        const byte SndChip_MMC5 = 8;  // Nintendo MMC5
        const byte SndChip_N163 = 16; // Namco N-106
        const byte SndChip_S5B  = 32; // Sunsoft 5B

        static readonly int[] ChanIdLookup = new[]
        {
            Channel.Square1,        // CHANID_SQUARE1
            Channel.Square2,        // CHANID_SQUARE2
            Channel.Triangle,       // CHANID_TRIANGLE
            Channel.Noise,          // CHANID_NOISE
            Channel.Dpcm,           // CHANID_DPCM
            Channel.Vrc6Square1,    // CHANID_VRC6_PULSE1
            Channel.Vrc6Square2,    // CHANID_VRC6_PULSE2
            Channel.Vrc6Saw,        // CHANID_VRC6_SAWTOOTH
            Channel.Mmc5Square1,    // CHANID_MMC5_SQUARE1
            Channel.Mmc5Square2,    // CHANID_MMC5_SQUARE2
            Channel.Mmc5Dpcm,       // CHANID_MMC5_VOICE
            Channel.NamcoWave1,     // CHANID_N163_CHAN1
            Channel.NamcoWave2,     // CHANID_N163_CHAN2
            Channel.NamcoWave3,     // CHANID_N163_CHAN3
            Channel.NamcoWave4,     // CHANID_N163_CHAN4
            Channel.NamcoWave5,     // CHANID_N163_CHAN5
            Channel.NamcoWave6,     // CHANID_N163_CHAN6
            Channel.NamcoWave7,     // CHANID_N163_CHAN7
            Channel.NamcoWave8,     // CHANID_N163_CHAN8
            Channel.FdsWave,        // CHANID_FDS
            Channel.Vrc7Fm1,        // CHANID_VRC7_CH1
            Channel.Vrc7Fm2,        // CHANID_VRC7_CH2
            Channel.Vrc7Fm3,        // CHANID_VRC7_CH3
            Channel.Vrc7Fm4,        // CHANID_VRC7_CH4
            Channel.Vrc7Fm5,        // CHANID_VRC7_CH5
            Channel.Vrc7Fm6,        // CHANID_VRC7_CH6
            Channel.SunsoftSquare1, // CHANID_S5B_CH1
            Channel.SunsoftSquare2, // CHANID_S5B_CH2
            Channel.SunsoftSquare3  // CHANID_S5B_CH3
        };

        static int[] InstrumentTypeLookup =
{
            Project.ExpansionCount,  // INST_NONE: Should never happen.
            Project.ExpansionNone,   // INST_2A03
            Project.ExpansionVrc6,   // INST_VRC6
            Project.ExpansionVrc7,   // INST_VRC7
            Project.ExpansionFds,    // INST_FDS
            Project.ExpansionNamco,  // INST_N163
            Project.ExpansionSunsoft // INST_S5B
        };

        static int[] EnvelopeTypeLookup =
        {
            Envelope.Volume,   // SEQ_VOLUME
            Envelope.Arpeggio, // SEQ_ARPEGGIO
            Envelope.Pitch,    // SEQ_PITCH
            Envelope.Max,      // SEQ_HIPITCH
            Envelope.DutyCycle // SEQ_DUTYCYCLE
        };

        const int MaxInstruments = 64;
        const int MaxSequences   = 128;
        const int SequenceCount  = 5;
        const int OctaveRange    = 8;

        private Project project;
        private int blockVersion;
        private byte[] bytes;
        private Envelope[,] envelopes = new Envelope[MaxSequences, SequenceCount];
        private Dictionary<Song, byte[,]> songFrameIndices = new Dictionary<Song, byte[,]>();
        private Dictionary<Song, byte[]> songEffectColumnCount = new Dictionary<Song, byte[]>();
        private Instrument[] instruments = new Instrument[MaxInstruments];
        private bool ReadParams(int idx)
        {
            var expansionChip = bytes[idx++];

            switch (expansionChip)
            {
                case SndChip_NONE : break;
                case SndChip_VRC6 : project.SetExpansionAudio(Project.ExpansionVrc6);    break;
                case SndChip_VRC7 : project.SetExpansionAudio(Project.ExpansionVrc7);    break;
                case SndChip_FDS  : project.SetExpansionAudio(Project.ExpansionFds);     break;
                case SndChip_MMC5 : project.SetExpansionAudio(Project.ExpansionMmc5);    break;
                case SndChip_N163 : project.SetExpansionAudio(Project.ExpansionNamco);   break;
                case SndChip_S5B  : project.SetExpansionAudio(Project.ExpansionSunsoft); break;
                default: return false;
            }

            var numChannels = BitConverter.ToInt32(bytes, idx); idx += sizeof(int);
            if (numChannels != project.GetActiveChannelCount())
                return false;

            //idx += sizeof(int); // m_iMachine;
            //idx += sizeof(int); // m_iEngineSpeed;
            //idx += sizeof(int); // m_iVibratoStyle;
            //idx += sizeof(int); // m_iFirstHighlight;
            //idx += sizeof(int); // m_iSecondHighlight;

            //if (blockVersion >= 5 && expansionChip == SndChip_N163)
            //    idx += sizeof(int); // m_iNamcoChannels;

            //if (blockVersion >= 6)
            //    idx += sizeof(int); // m_iSpeedSplitPoint

            return true;
        }

        private bool ReadInfo(int idx)
        {
            project.Name      = Encoding.ASCII.GetString(bytes, idx, FileHeaderId.Length).TrimEnd('\0'); idx += 32;
            project.Author    = Encoding.ASCII.GetString(bytes, idx, FileHeaderId.Length).TrimEnd('\0'); idx += 32;
            project.Copyright = Encoding.ASCII.GetString(bytes, idx, FileHeaderId.Length).TrimEnd('\0'); idx += 32;

            return true;
        }
        
        private bool ReadHeader(int idx)
        {
            var numSongs = bytes[idx++] + 1;
            var chanCount = project.GetActiveChannelCount();

            for (int i = 0; i < numSongs; i++)
            {
                int len = Array.IndexOf<byte>(bytes, 0, idx) - idx + 1;
                var songName = Encoding.ASCII.GetString(bytes, idx, len).TrimEnd('\0'); idx += len;
                var song = project.CreateSong(songName);
                songEffectColumnCount[song] = new byte[chanCount];
            }

            var channelList = project.GetActiveChannelList();

            for (int i = 0; i < chanCount; i++)
            {
                var chanType = bytes[idx++];

                if (ChanIdLookup[chanType] != channelList[i])
                    return false;

                for (int j = 0; j < numSongs; j++)
                    songEffectColumnCount[project.Songs[j]][i] = bytes[idx++];
            }

            return true;
        }

        private bool ReadInstrument2A03(ref int idx)
        {
            idx += sizeof(int); // SEQ_COUNT

            for (int i = 0; i < SequenceCount; ++i)
            {
                idx++; // GetSeqEnable(i);
                idx++; // GetSeqIndex(i);
            }

            for (int i = 0; i < OctaveRange; ++i)
            {
                for (int j = 0; j < 12; ++j)
                {
                    idx++; // GetSample(i, j);
                    idx++; // GetSamplePitch(i, j);
                    if (blockVersion > 5)
                        idx++; // GetSampleDeltaValue(i, j);
                }
            }

            return true;
        }

        private bool ReadInstrumentVRC6(ref int idx)
        {
            idx += sizeof(int); // SEQ_COUNT

            for (int i = 0; i < SequenceCount; ++i)
            {
                idx++; // GetSeqEnable(i);
                idx++; // GetSeqIndex(i);
            }

            return true;
        }

        private bool ReadInstruments(int idx)
        {
            var instrumentCount = BitConverter.ToInt32(bytes, idx); idx += sizeof(int);

            for (int i = 0; i < instrumentCount; i++)
            {
                var index = BitConverter.ToInt32(bytes, idx); idx += sizeof(int);
                var type  = InstrumentTypeLookup[bytes[idx++]];
                var instrument = project.CreateInstrument(type);

                if (instrument == null)
                    return false;

                switch (type)
                {
                    case Project.ExpansionNone: ReadInstrument2A03(ref idx); break;
                    case Project.ExpansionVrc6: ReadInstrumentVRC6(ref idx); break;
                    default:
                        return false;
                }

                var len = BitConverter.ToInt32(bytes, idx); idx += sizeof(int);
                var name = Encoding.ASCII.GetString(bytes, idx, len); idx += len;

                // MATTT: Ensure unique.
                if (!project.RenameInstrument(instrument, name))
                    return false;

                instruments[i] = instrument;
            }

            return true;
        }

        private bool ReadSequences(int idx)
        {
            var count = BitConverter.ToInt32(bytes, idx); idx += sizeof(int);
            var indices = new int[count];
            var types   = new int[count];
            var loops   = new int[count];

            for (int i = 0; i < count; ++i)
            {
                var index     = BitConverter.ToInt32(bytes, idx); idx += sizeof(int);
                var type      = BitConverter.ToInt32(bytes, idx); idx += sizeof(int);
                var seqCount  = bytes[idx++];
                var loopPoint = BitConverter.ToInt32(bytes, idx); idx += sizeof(int);

                indices[i] = index;
                types[i]   = type;
                loops[i]   = loopPoint;

                var env = new Envelope(EnvelopeTypeLookup[type]);

                if (env.CanResize)
                   env.Length = seqCount;

                envelopes[index, type] = env;

                for (int j = 0; j < seqCount; ++j)
                    env.Values[j] = (sbyte)bytes[idx++];
            }

            for (int i = 0; i < count; ++i)
            {
                var loopPoint      = loops[i];
                var releasePoint   = BitConverter.ToInt32(bytes, idx); idx += sizeof(int) * 2; // Skip settings
                var type           = types[i];
                var famistudioType = EnvelopeTypeLookup[type];

                var env = envelopes[indices[i], types[i]];
                Debug.Assert(env != null);

                if (releasePoint >= 0 && !env.CanRelease)
                    releasePoint = -1;

                // FamiTracker allows envelope with release with no loop. We dont allow that.
                if (env.CanRelease && releasePoint != -1)
                {
                    if (loopPoint == -1)
                        loopPoint = releasePoint;
                    if (releasePoint != -1)
                        releasePoint++;
                }

                env.Loop = loopPoint;
                env.Release = releasePoint;
            }

            return true;
        }

        private bool ReadFrames(int idx)
        {
            foreach (var song in project.Songs)
            {
                var frameCount = BitConverter.ToInt32(bytes, idx); idx += sizeof(int);
                var speed      = BitConverter.ToInt32(bytes, idx); idx += sizeof(int);
                var tempo      = BitConverter.ToInt32(bytes, idx); idx += sizeof(int);
                var patLength  = BitConverter.ToInt32(bytes, idx); idx += sizeof(int);

                song.SetLength(frameCount);
                song.SetDefaultPatternLength(patLength);
                song.Tempo = tempo;
                song.Speed = speed;

                var frameIndices = new byte[song.Channels.Length, frameCount];

                for (int i = 0; i < frameCount; ++i)
                {
				    for (int j = 0; j < song.Channels.Length; ++j)
                    {
                        frameIndices[j, i] = bytes[idx++];
				    }
			    }

                songFrameIndices[song] = frameIndices;
            }

            return true;
        }

        private bool ReadPatterns(int idx, int maxIdx)
        {
            while (idx < maxIdx)
            { 
                var songIdx = BitConverter.ToInt32(bytes, idx); idx += sizeof(int);
                var chanIdx = BitConverter.ToInt32(bytes, idx); idx += sizeof(int);
                var patIdx  = BitConverter.ToInt32(bytes, idx); idx += sizeof(int);
                var items   = BitConverter.ToInt32(bytes, idx); idx += sizeof(int);

                var song    = project.Songs[songIdx];
                var channel = song.Channels[chanIdx];
                var pattern = channel.CreatePattern($"{patIdx:X2}");

                for (int i = 0; i < items; i++)
                {
                    var n = BitConverter.ToInt32(bytes, idx); idx += sizeof(int);

                    var note       = bytes[idx++];
                    var octave     = bytes[idx++];
                    var instrument = bytes[idx++];
                    var volume     = bytes[idx++];

                    if (channel.Type == Channel.Dpcm) instrument = MaxInstruments;
                    if (note != 0 || octave != 0)     pattern.Notes[n].Value  = (byte)(octave * 12 + note);
                    if (volume != 16)                 pattern.Notes[n].Volume = (byte)(volume & 0x0f);
                    if (instrument < MaxInstruments)  pattern.Notes[n].Instrument = instruments[instrument];

                    var effectColumnCount = songEffectColumnCount[song][chanIdx];

                    for (int j = 0; j < effectColumnCount + 1; ++j)
                    {
                        var fx    = bytes[idx++];
                        var param = bytes[idx++];

                        //Note->EffNumber[n] = EffectNumber;
                        //Note->EffParam[n]  = EffectParam;
                    }

                    // FDS octave
                    if (blockVersion < 5 && project.ExpansionAudio == Project.ExpansionFds && channel.Type == Channel.FdsWave && octave < 6)
                        octave += 2;
                }
            }

            // Create pattern instances.
            foreach (var kv in songFrameIndices)
            {
                var song = kv.Key;
                var frameIndices = kv.Value;

                for (int c = 0; c < frameIndices.GetLength(0); c++)
                {
                    var channel = song.Channels[c];

                    for (int p = 0; p < frameIndices.GetLength(1); p++)
                    {
                        var patIdx = frameIndices[c, p];
                        channel.PatternInstances[p] = channel.Patterns[patIdx];
                    }
                }
            }

            return true;
        }

        public Project Load(string filename)
        {
            var idx = 0;
            bytes = File.ReadAllBytes(filename);

            var id = Encoding.ASCII.GetString(bytes, idx, FileHeaderId.Length); idx += FileHeaderId.Length;
            if (id != FileHeaderId)
                return null;

            var version = BitConverter.ToUInt32(bytes, idx); idx += sizeof(uint);
            if (version < MinVersion || version > MaxVersion)
                return null;

            project = new Project();

            while (bytes[idx + 0] != 'E' ||
                   bytes[idx + 1] != 'N' || 
                   bytes[idx + 2] != 'D')
            {
                var blockId   = Encoding.ASCII.GetString(bytes, idx, BlockNameLength).TrimEnd('\0'); idx += BlockNameLength;
                var blockVer  = BitConverter.ToInt32(bytes, idx); idx += sizeof(uint);
                var blockSize = BitConverter.ToInt32(bytes, idx); idx += sizeof(uint);
                var success   = true;

                blockVersion = blockVer;

                switch (blockId)
                {
                    case "PARAMS"       : success = ReadParams(idx); break;
                    case "INFO"         : success = ReadInfo(idx); break;
                    case "HEADER"       : success = ReadHeader(idx); break;
                    case "INSTRUMENTS"  : success = ReadInstruments(idx); break;
                    case "SEQUENCES"    : success = ReadSequences(idx); break;
                    case "FRAMES"       : success = ReadFrames(idx); break;
                    case "PATTERNS"     : success = ReadPatterns(idx, idx + blockSize); break;
                    //case "DPCM SAMPLES" : break;
                }

                if (!success)
                    return null;

                idx += blockSize;
            }

            return project;
        }
    }
}
