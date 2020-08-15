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

        private bool samplesLoaded;
        private int blockVersion;
        private int blockSize;
        private byte[] bytes;
        private Envelope[,] envelopes    = new Envelope[MaxSequences, SequenceCount];
        private Envelope[,] envelopesExp = new Envelope[MaxSequences, SequenceCount];
        private Dictionary<Song, byte[]> songEffectColumnCount = new Dictionary<Song, byte[]>();
        private Instrument[] instruments = new Instrument[MaxInstruments];
        private DPCMSample[] samples = new DPCMSample[MaxSamples];

        public FamitrackerBinaryFile(ILogInterface log)
        {
            this.log = log;
        }

        private bool ReadParams(int idx)
        {
            Debug.Assert(blockVersion >= 4);

            var expansion = ConvertExpansionAudio(bytes[idx++]);
            if (expansion < 0)
                return false;

            var numChannels = BitConverter.ToInt32(bytes, idx); idx += sizeof(int);
            var machine     = BitConverter.ToInt32(bytes, idx); idx += sizeof(int); 
            idx += sizeof(int); // m_iEngineSpeed;
            idx += sizeof(int); // m_iVibratoStyle;
            if (blockVersion >= 4)
            {
                idx += sizeof(int); // m_iFirstHighlight;
                barLength = BitConverter.ToInt32(bytes, idx); idx += sizeof(int);
            }

            var numN163Channels = 0;
            if (blockVersion >= 5)
            {
                if (expansion == Project.ExpansionN163)
                    numN163Channels = BitConverter.ToInt32(bytes, idx); idx += sizeof(int);
            }

            project.PalMode = machine == 1;

            if (numChannels != project.GetActiveChannelCount())
            {
                log?.Log("Error, unexpected audio channel count.");
                return false;
            }

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
                var song = CreateUniquelyNamedSong(songName);
                songEffectColumnCount[song] = new byte[chanCount];
            }

            var channelList = project.GetActiveChannelList();

            for (int i = 0; i < chanCount; i++)
            {
                var chanType = bytes[idx++];

                if (ChanIdLookup[chanType] != channelList[i])
                {
                    log?.Log("Error, channel type mismatch.");
                    return false;
                }

                for (int j = 0; j < numSongs; j++)
                    songEffectColumnCount[project.Songs[j]][i] = bytes[idx++];
            }

            return true;
        }

        private void ReadCommonEnvelopes(Instrument instrument, ref int idx, Envelope[,] envelopesArray)
        {
            idx += sizeof(int); // SEQ_COUNT

            for (int i = 0; i < SequenceCount; ++i)
            {
                var enabled = bytes[idx++];
                var index = bytes[idx++];

                if (enabled != 0)
                {
                    var envType = EnvelopeTypeLookup[i];

                    if (envType != Envelope.Count)
                    {
                        if (instrument.Envelopes[envType] != null)
                            instrument.Envelopes[envType] = envelopesArray[index, i];
                    }
                }
            }
        }

        private void ReadSingleEnvelope(int type, Envelope env, ref int idx)
        {
            env.Length = bytes[idx++];

            int loopPoint    = BitConverter.ToInt32(bytes, idx); idx += sizeof(int);
            int releasePoint = BitConverter.ToInt32(bytes, idx); idx += sizeof(int);

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

            env.Loop     = loopPoint;
            env.Release  = releasePoint;
            env.Relative = type == Envelope.Pitch;

            idx += sizeof(int); // Skip settings.

            for (int i = 0; i < env.Length; i++)
                env.Values[i] = (sbyte)bytes[idx++];
        }

        private void ReadInstrument2A03(Instrument instrument, ref int idx)
        {
            ReadCommonEnvelopes(instrument, ref idx, envelopes);

            // We only consider the first 2A03 instrument with samples. Rest is ignored.
            if (!samplesLoaded)
            {
                for (int i = 0; i < OctaveRange; ++i)
                {
                    for (int j = 0; j < 12; ++j)
                    {
                        var index = bytes[idx++];
                        var pitch = bytes[idx++];

                        if (blockVersion > 5)
                            idx++; // sample delta

                        if (index > 0 && pitch != 0)
                        {
                            var sample = samples[index - 1];
                            if (sample != null && sample.Data != null)
                            {
                                samplesLoaded = true;
                                project.MapDPCMSample(i * 12 + j + 1, sample, pitch & 0x0f, (pitch & 0x80) != 0);
                            }
                        }

                    }
                }
            }
            else
            {
                idx += OctaveRange * 12 * (blockVersion > 5 ? 3 : 2);
            }
        }

        private void ReadInstrumentVRC6(Instrument instrument, ref int idx)
        {
            ReadCommonEnvelopes(instrument, ref idx, envelopesExp);
        }

        private void ReadInstrumentVRC7(Instrument instrument, ref int idx)
        {
            instrument.Vrc7Patch = (byte)BitConverter.ToInt32(bytes, idx); idx += sizeof(int);

            if (instrument.Vrc7Patch == 0)
            {
                for (int i = 0; i < 8; ++i)
                    instrument.Vrc7PatchRegs[i] = bytes[idx++];
            }
            else
            {
                idx += 8;
            }
        }

        private void ReadInstrumentFds(Instrument instrument, ref int idx)
        {
            var wavEnv = instrument.Envelopes[Envelope.FdsWaveform];
            var modEnv = instrument.Envelopes[Envelope.FdsModulation];

            for (int i = 0; i < 0x40; i++)
                wavEnv.Values[i] = (sbyte)bytes[idx++];

            for (int i = 0; i < 0x20; i++)
                modEnv.Values[i] = (sbyte)bytes[idx++];

            instrument.FdsWavePreset = Envelope.WavePresetCustom;
            instrument.FdsModPreset  = Envelope.WavePresetCustom;

            modEnv.ConvertFdsModulationToAbsolute();

            instrument.FdsModSpeed = (ushort)BitConverter.ToInt32(bytes, idx); idx += sizeof(int);
            instrument.FdsModDepth =   (byte)BitConverter.ToInt32(bytes, idx); idx += sizeof(int);
            instrument.FdsModDelay =   (byte)BitConverter.ToInt32(bytes, idx); idx += sizeof(int);

            ReadSingleEnvelope(Envelope.Volume,   instrument.Envelopes[Envelope.Volume],   ref idx);
            ReadSingleEnvelope(Envelope.Arpeggio, instrument.Envelopes[Envelope.Arpeggio], ref idx);
            ReadSingleEnvelope(Envelope.Pitch,    instrument.Envelopes[Envelope.Pitch],    ref idx);
        }

        private void ReadInstrumentN163(Instrument instrument, ref int idx)
        {
            ReadCommonEnvelopes(instrument, ref idx, envelopesExp);

            var fileWaveSize = BitConverter.ToInt32(bytes, idx); idx += sizeof(int);

            instrument.N163WavePreset = Envelope.WavePresetCustom;
            instrument.N163WaveSize   = (byte)fileWaveSize;
            instrument.N163WavePos    = (byte)BitConverter.ToInt32(bytes, idx); idx += sizeof(int);

            var waveCount = BitConverter.ToInt32(bytes, idx); idx += sizeof(int); 

            for (int j = 0; j < fileWaveSize; j++)
                instrument.Envelopes[Envelope.N163Waveform].Values[j] = (sbyte)bytes[idx++];

            // Skip any extra waves.
            idx += (waveCount - 1) * fileWaveSize;
        }

        private void ReadInstrumentS5B(Instrument instrument, ref int idx)
        {
            ReadCommonEnvelopes(instrument, ref idx, envelopesExp);
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
                {
                    log?.Log($"Error, failed to create instrument {index}.");
                    return false;
                }

                switch (type)
                {
                    case Project.ExpansionNone: ReadInstrument2A03(instrument,  ref idx); break;
                    case Project.ExpansionVrc6: ReadInstrumentVRC6(instrument,  ref idx); break;
                    case Project.ExpansionVrc7: ReadInstrumentVRC7(instrument,  ref idx); break;
                    case Project.ExpansionFds:  ReadInstrumentFds(instrument,   ref idx); break;
                    case Project.ExpansionN163: ReadInstrumentN163(instrument,  ref idx); break;
                    case Project.ExpansionS5B:  ReadInstrumentS5B(instrument,   ref idx); break;
                    default:
                        return false;
                }

                var len  = BitConverter.ToInt32(bytes, idx); idx += sizeof(int);
                var name = Encoding.ASCII.GetString(bytes, idx, len); idx += len;

                RenameInstrumentEnsureUnique(instrument, name);

                instruments[index] = instrument;
            }

            return true;
        }

        private bool ReadSequences2A03Vrc6(int idx, Envelope[,] envelopeArray)
        {
            var count = BitConverter.ToInt32(bytes, idx); idx += sizeof(int);
            var indices = new int[count];
            var types = new int[count];
            var loops = new int[count];

            for (int i = 0; i < count; ++i)
            {
                var index = BitConverter.ToInt32(bytes, idx); idx += sizeof(int);
                var type = BitConverter.ToInt32(bytes, idx); idx += sizeof(int);
                var seqCount = bytes[idx++];
                var loopPoint = BitConverter.ToInt32(bytes, idx); idx += sizeof(int);

                indices[i] = index;
                types[i] = type;
                loops[i] = loopPoint;

                var env = new Envelope(EnvelopeTypeLookup[type]);

                if (env.CanResize)
                    env.Length = seqCount;

                envelopeArray[index, type] = env;

                for (int j = 0; j < seqCount; ++j)
                    env.Values[j] = (sbyte)bytes[idx++];
            }

            for (int i = 0; i < count; ++i)
            {
                var loopPoint = loops[i];
                var releasePoint = BitConverter.ToInt32(bytes, idx); idx += sizeof(int) * 2; // Skip settings
                var type = types[i];

                var env = envelopeArray[indices[i], types[i]];
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
                env.Relative = type == 2;
            }

            return true;
        }

        private bool ReadSequences(int idx)
        {
            return ReadSequences2A03Vrc6(idx, envelopes);
        }

        private bool ReadSequencesVrc6(int idx)
        {
            if (project.ExpansionAudio != Project.ExpansionVrc6)
                return true;

            return ReadSequences2A03Vrc6(idx, envelopesExp);
        }

        private bool ReadSequencesN163(int idx)
        {
            if (project.ExpansionAudio != Project.ExpansionN163)
                return true;

            var count = BitConverter.ToInt32(bytes, idx); idx += sizeof(int);
            var indices = new int[count];
            var types = new int[count];
            var loops = new int[count];

            for (int i = 0; i < count; ++i)
            {
                var index        = BitConverter.ToInt32(bytes, idx); idx += sizeof(int);
                var type         = BitConverter.ToInt32(bytes, idx); idx += sizeof(int);
                var seqCount     = bytes[idx++];
                var loopPoint    = BitConverter.ToInt32(bytes, idx); idx += sizeof(int);
                var releasePoint = BitConverter.ToInt32(bytes, idx); idx += sizeof(int) * 2; // Skip settings

                indices[i] = index;
                types[i] = type;
                loops[i] = loopPoint;

                var env = new Envelope(EnvelopeTypeLookup[type]);

                if (env.CanResize)
                    env.Length = seqCount;
               
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
                env.Relative = type == 2;

                envelopesExp[index, type] = env;

                for (int j = 0; j < seqCount; ++j)
                    env.Values[j] = (sbyte)bytes[idx++];
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
                song.FamitrackerTempo = tempo;
                song.FamitrackerSpeed = speed;

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
                var fxData  = new RowFxData[song.PatternLength, fxCount + 1];
                patternFxData[pattern] = fxData;

                for (int i = 0; i < items; i++)
                {
                    var n = BitConverter.ToInt32(bytes, idx); idx += sizeof(int);

                    var note       = bytes[idx++];
                    var octave     = bytes[idx++];
                    var instrument = bytes[idx++];
                    var volume     = bytes[idx++];

                    // This happens when some patterns are longer than the song pattern length.
                    // The TNMT song from FamiTracker has this.
                    if (n < song.PatternLength)
                    {
                        if (volume != 16)
                            pattern.GetOrCreateNoteAt(n).Volume = (byte)(volume & 0x0f);

                        if (blockVersion < 5 && project.ExpansionAudio == Project.ExpansionFds && channel.Type == Channel.FdsWave && octave < 6 && octave != 0)
                            octave += 2;

                        if (note != 0 && octave != 0)
                        {
                            switch (note)
                            {
                                case 13: pattern.GetOrCreateNoteAt(n).Value = Note.NoteRelease; break;
                                case 14: pattern.GetOrCreateNoteAt(n).Value = Note.NoteStop; break;
                                default:
                                    if (instrument < MaxInstruments && channel.Type != Channel.Dpcm)
                                        pattern.GetOrCreateNoteAt(n).Instrument = instruments[instrument];
                                    if (channel.Type == Channel.Noise)
                                        pattern.GetOrCreateNoteAt(n).Value = (byte)(octave * 12 + note + 15);
                                    else
                                        pattern.GetOrCreateNoteAt(n).Value = (byte)(octave * 12 + note);
                                    break;
                            }
                        }
                    }

                    for (int j = 0; j < fxCount + 1; ++j)
                    {
                        RowFxData fx;
                        fx.fx    = bytes[idx++];
                        fx.param = bytes[idx++];

                        // See comment above.
                        if (n < song.PatternLength)
                            fxData[n, j] = fx;

                        ApplySimpleEffects(fx, pattern, n, patternLengths);
                    }
                }
            }

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
            {
                log?.Log("Error, invalid FTM file ID.");
                return null;
            }

            var version = BitConverter.ToUInt32(bytes, idx); idx += sizeof(uint);
            if (version < MinVersion || version > MaxVersion)
            {
                log?.Log("Error, unsupported file version. Only FTM version 0.4.4 to 0.4.6 are supported.");
                return null;
            }

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
                { "PARAMS",         ReadParams        },
                { "INFO",           ReadInfo          },
                { "HEADER",         ReadHeader        },
                { "DPCM SAMPLES",   ReadDpcmSamples   },
                { "SEQUENCES",      ReadSequences     },
                { "SEQUENCES_VRC6", ReadSequencesVrc6 },
                { "SEQUENCES_N163", ReadSequencesN163 },
                { "INSTRUMENTS",    ReadInstruments   },
                { "FRAMES",         ReadFrames        },
                { "PATTERNS",       ReadPatterns      },
            };

            project = new Project();
            project.TempoMode = Project.TempoFamiTracker;

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
