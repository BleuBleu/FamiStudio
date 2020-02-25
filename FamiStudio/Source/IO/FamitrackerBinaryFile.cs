using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FamiStudio
{ 
    public class FamitrackerBinaryFile : FamitrackerFileBase
    {
        const uint MinVersion = 0x0420;
        const uint MaxVersion = 0x0440;

        const string FileHeaderId = "FamiTracker Module";

        const int BlockNameLength = 16;
        const int MaxInstruments = 64;
        const int MaxSamples     = 64;
        const int MaxSequences   = 128;
        const int SequenceCount  = 5;
        const int OctaveRange    = 8;

        struct BlockInfo
        {
            public int offset;
            public int size;
            public int version;
        };

        private delegate bool ReadBlockDelegate(int idx);

        private int blockVersion;
        private int blockSize;
        private byte[] bytes;
        private Envelope[,] envelopes = new Envelope[MaxSequences, SequenceCount];
        private Dictionary<Song, byte[]> songEffectColumnCount = new Dictionary<Song, byte[]>();
        private Instrument[] instruments = new Instrument[MaxInstruments];
        private DPCMSample[] samples = new DPCMSample[MaxSamples];

        private bool ReadParams(int idx)
        {
            var expansion = ConvertExpansionAudio(bytes[idx++]);
            if (expansion < 0)
                return false;

            project.SetExpansionAudio(expansion);

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

        private bool ReadInstrument2A03(Instrument instrument, ref int idx, bool readSamples)
        {
            idx += sizeof(int); // SEQ_COUNT

            for (int i = 0; i < SequenceCount; ++i)
            {
                var enabled = bytes[idx++];
                var index   = bytes[idx++];

                if (enabled != 0)
                {
                    var envType = EnvelopeTypeLookup[i];

                    if (envType != Envelope.Max)
                    {
                        Debug.Assert(instrument.Envelopes[envType] != null);
                        instrument.Envelopes[envType] = envelopes[index, i];
                    }
                }
            }

            // MATTT: Read from the first instrument that has sample. Instrument 00 might not even be 2A03.
            if (readSamples)
            {
                for (int i = 0; i < OctaveRange; ++i)
                {
                    for (int j = 0; j < 12; ++j)
                    {
                        var index = bytes[idx++];
                        var pitch = bytes[idx++];

                        if (blockVersion > 5)
                            idx++; // sample delta

                        if (index != 0 || pitch != 0)
                        {
                            var sample = samples[index - 1];
                            if (sample != null && sample.Data != null)
                                project.MapDPCMSample(i * 12 + j + 1, sample, pitch & 0x0f, (pitch & 0x80) != 0);
                        }

                    }
                }
            }
            else
            {
                idx += OctaveRange * 12 * (blockVersion > 5 ? 3 : 2);
            }

            return true;
        }

        private bool ReadInstrumentVRC6(Instrument instrument, ref int idx)
        {
            idx += sizeof(int); // SEQ_COUNT

            for (int i = 0; i < SequenceCount; ++i)
            {
                var enabled = bytes[idx++];
                var index   = bytes[idx++];

                if (enabled != 0)
                {
                    var envType = EnvelopeTypeLookup[i];

                    if (envType != Envelope.Max)
                    {
                        Debug.Assert(instrument.Envelopes[envType] != null);
                        instrument.Envelopes[envType] = envelopes[index, i];
                    }
                }
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
                var instrument = project.CreateInstrument(type, $"___TEMP_INSTRUMENT_NAME___{index}");

                if (instrument == null)
                    return false;

                switch (type)
                {
                    case Project.ExpansionNone: ReadInstrument2A03(instrument, ref idx, index == 0); break;
                    case Project.ExpansionVrc6: ReadInstrumentVRC6(instrument, ref idx); break;
                    default:
                        return false;
                }

                var len  = BitConverter.ToInt32(bytes, idx); idx += sizeof(int);
                var name = Encoding.ASCII.GetString(bytes, idx, len); idx += len;

                RenameInstrumentEnsureUnique(instrument, name);

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

                env.Loop    = loopPoint;
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

                for (int i = 0; i < frameCount; ++i)
                {
                    for (int j = 0; j < song.Channels.Length; ++j)
                    {
                        var frameIdx = bytes[idx++];
                        var name = $"{frameIdx:X2}";
                        var pattern = song.Channels[j].GetPattern(name);

                        if (pattern == null)
                            pattern = song.Channels[j].CreatePattern(name);

                        song.Channels[j].PatternInstances[i] = pattern;
                    }
                }
            }

            return true;
        }

        private bool ReadPatterns(int idx)
        {
            var maxIdx = idx + blockSize;

            while (idx < maxIdx)
            { 
                var songIdx = BitConverter.ToInt32(bytes, idx); idx += sizeof(int);
                var chanIdx = BitConverter.ToInt32(bytes, idx); idx += sizeof(int);
                var patIdx  = BitConverter.ToInt32(bytes, idx); idx += sizeof(int);
                var items   = BitConverter.ToInt32(bytes, idx); idx += sizeof(int);

                var song    = project.Songs[songIdx];
                var channel = song.Channels[chanIdx];
                var pattern = channel.GetPattern($"{patIdx:X2}");

                // Famitracker can have patterns that arent actually used in the song. 
                // Skip with a dummy pattern.
                if (pattern == null)
                    pattern = new Pattern();

                var fxCount = songEffectColumnCount[song][chanIdx];
                var fxData  = new RowFxData[song.DefaultPatternLength, fxCount + 1];
                patternFxData[pattern] = fxData;

                for (int i = 0; i < items; i++)
                {
                    var n = BitConverter.ToInt32(bytes, idx); idx += sizeof(int);

                    var note       = bytes[idx++];
                    var octave     = bytes[idx++];
                    var instrument = bytes[idx++];
                    var volume     = bytes[idx++];

                    if (volume != 16)
                        pattern.Notes[n].Volume = (byte)(volume & 0x0f);

                    if (note != 0 || octave != 0)
                    {
                        switch (note)
                        {
                            case 13: pattern.Notes[n].Value = Note.NoteRelease; break;
                            case 14: pattern.Notes[n].Value = Note.NoteStop; break;
                            default:
                                if (instrument < MaxInstruments && channel.Type != Channel.Dpcm)
                                    pattern.Notes[n].Instrument = instruments[instrument];
                                if (channel.Type == Channel.Noise)
                                    pattern.Notes[n].Value = (byte)(octave * 12 + note + 15);
                                else
                                    pattern.Notes[n].Value = (byte)(octave * 12 + note);
                                break;
                        }
                    }

                    for (int j = 0; j < fxCount + 1; ++j)
                    {
                        RowFxData fx;
                        fx.fx    = bytes[idx++];
                        fx.param = bytes[idx++];
                        fxData[n, j] = fx;

                        ApplySimpleEffects(fx, pattern, n, patternLengths);
                    }

                    // FDS octave
                    if (blockVersion < 5 && project.ExpansionAudio == Project.ExpansionFds && channel.Type == Channel.FdsWave && octave < 6)
                        octave += 2;
                }
            }

            // TODO: Apply pattern lengths.

            return true;
        }

        private bool ReadDpcmSamples(int idx)
        {
            var count = bytes[idx++];

            for (int i = 0; i < count; ++i)
            {
                var index   = bytes[idx++];
                var nameLen = BitConverter.ToInt32(bytes, idx); idx += sizeof(int);
                var name    = Encoding.ASCII.GetString(bytes, idx, nameLen); idx += nameLen;
                var size    = BitConverter.ToInt32(bytes, idx); idx += sizeof(int);
                var data    = new byte[size];

                Array.Copy(bytes, idx, data, 0, size); idx += size;

                samples[i] = CreateUniquelyNamedSample(name, data);
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

            var blockToc = new Dictionary<string, BlockInfo>();

            while (bytes[idx + 0] != 'E' ||
                   bytes[idx + 1] != 'N' || 
                   bytes[idx + 2] != 'D')
            {
                var blockId     = Encoding.ASCII.GetString(bytes, idx, BlockNameLength).TrimEnd('\0'); idx += BlockNameLength;
                var blockVer    = BitConverter.ToInt32(bytes, idx); idx += sizeof(uint);
                var blockSize   = BitConverter.ToInt32(bytes, idx); idx += sizeof(uint);

                blockToc[blockId] = new BlockInfo() { offset = idx, version = blockVer, size = blockSize };

                idx += blockSize;
            }

            // We read block in a specific order to minimize the amound of bookeeping we need to do.
            var blocksToRead = new Dictionary<string, ReadBlockDelegate>
            {
                { "PARAMS",       ReadParams      },
                { "INFO",         ReadInfo        },
                { "HEADER",       ReadHeader      },
                { "DPCM SAMPLES", ReadDpcmSamples },
                { "SEQUENCES",    ReadSequences   },
                { "INSTRUMENTS",  ReadInstruments },
                { "FRAMES",       ReadFrames      },
                { "PATTERNS",     ReadPatterns    },
            };

            project = new Project();

            foreach (var kv in blocksToRead)
            {
                var blockName = kv.Key;
                var blockFunc = kv.Value;

                if (blockToc.TryGetValue(blockName, out var info))
                {
                    blockSize    = info.size;
                    blockVersion = info.version;

                    if (!blockFunc(info.offset))
                        return null;
                }

            }

            FinishImport();

            return project;
        }
    }
}
