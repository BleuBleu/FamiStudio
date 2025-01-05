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
        private Dictionary<Song, byte[]> songEffectColumnCount = new Dictionary<Song, byte[]>();
        private Instrument[] instruments = new Instrument[MaxInstruments];
        private DPCMSample[] samples = new DPCMSample[MaxSamples];

        private bool ReadParams(int idx)
        {
            Debug.Assert(blockVersion >= 3);

            var expansion = ConvertExpansionAudio(bytes[idx++]);
            if (expansion < 0)
                return false;

            var numChannels = BitConverter.ToInt32(bytes, idx); idx += sizeof(int);
            var machine     = BitConverter.ToInt32(bytes, idx); idx += sizeof(int); 
            idx += sizeof(int); // m_iEngineSpeed;
            idx += sizeof(int); // m_iVibratoStyle;
            if (blockVersion >= 4)
            {
                barLength = BitConverter.ToInt32(bytes, idx); idx += sizeof(int); 
                idx += sizeof(int); // m_iSecondHighlight
            }

            var numN163Channels = 0;
            if (blockVersion >= 5)
            {
                if (expansion == ExpansionType.N163)
                    numN163Channels = BitConverter.ToInt32(bytes, idx); idx += sizeof(int);
            }

            project.SetExpansionAudioMask(ExpansionType.GetMaskFromValue(expansion), numN163Channels);
            project.PalMode = machine == 1;

            if (numChannels != project.GetActiveChannelCount())
            {
                Log.LogMessage(LogSeverity.Error, "Unexpected audio channel count.");
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
                    Log.LogMessage(LogSeverity.Warning, $"Channel type mismatch, expected {ChannelType.InternalNames[channelList[i]]}, got {ChannelType.InternalNames[ChanIdLookup[chanType]]}. Import may fail.");
                }

                for (int j = 0; j < numSongs; j++)
                    songEffectColumnCount[project.Songs[j]][i] = bytes[idx++];
            }

            return true;
        }

        private void ReadCommonEnvelopes(Instrument instrument, int instIdx, ref int idx, Envelope[,] envelopesArray)
        {
            idx += sizeof(int); // SEQ_COUNT

            var usedEnvelopes = new bool[EnvelopeType.Count];

            for (int i = 0; i < SequenceCount; ++i)
            {
                var enabled = bytes[idx++];
                var index = bytes[idx++];

                if (enabled != 0)
                {
                    var envType = FamiTrackerToFamiStudioEnvelopeLookup[i];

                    if (envType != EnvelopeType.Count)
                    {
                        if (instrument.IsN163 && i == 4 /* SEQ_DUTYCYCLE */)
                        {
                            // N163 wave index envelopes are special since we need to 
                            // convert them to our internal representation (repeat-based).
                            n163WaveEnvs[instrument] = index;
                        }
                        else if (instrument.Envelopes[envType] != null && envelopesArray[index, i] != null)
                        {
                            instrument.Envelopes[envType] = envelopesArray[index, i];
                            usedEnvelopes[envType] = true;
                        } 
                    }
                    else
                    {
                        Log.LogMessage(LogSeverity.Warning, $"Hi-pitch envelopes are unsupported (instrument {instIdx:X2}), ignoring.");
                    }
                }
            }

            // When people use a single looping zero arpeggio, they are likely trying to fake an "absolute" pitch
            // envelope, so let's give them that.
            if (usedEnvelopes[EnvelopeType.Arpeggio] && usedEnvelopes[EnvelopeType.Pitch])
            {
                var arp = instrument.Envelopes[EnvelopeType.Arpeggio];
                if (arp.IsEmpty(EnvelopeType.Arpeggio) && arp.Length > 0 && arp.Loop >= 0)
                {
                    Log.LogMessage(LogSeverity.Warning, $"Instrument {instIdx:X2} uses a looping null arpeggio envelope and a pitch envelope. Assuming envelope should be 'Absolute'.");
                    instrument.Envelopes[EnvelopeType.Pitch].Relative = false;
                }
                else
                {
                    Log.LogMessage(LogSeverity.Warning, $"Instrument {instIdx:X2} uses both an arpeggio envelope and a pitch envelope. This instrument will likely require manual corrections due to the vastly different handling of those between FamiTracker and FamiStudio.");
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
            env.Relative = type == EnvelopeType.Pitch;

            idx += sizeof(int); // Skip settings.

            for (int i = 0; i < env.Length; i++)
                env.Values[i] = (sbyte)bytes[idx++];
        }

        private void ReadInstrument2A03(Instrument instrument, int instIdx, ref int idx)
        {
            ReadCommonEnvelopes(instrument, instIdx, ref idx, envelopes);

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
                        var note = i * 12 + j + 1;
                        if (sample != null && sample.ProcessedData != null)
                        {
                            if (instrument.IsRegular)
                            {
                                instrument.MapDPCMSample(note, sample, pitch & 0x0f, (pitch & 0x80) != 0);
                            }
                            else
                            {
                                Log.LogMessage(LogSeverity.Warning, $"Instrument {instrument.Name} has DPCM samples but is an expansion instrument. Ignoring.");
                            }
                        }
                    }
                }
            }
        }

        private void ReadInstrumentVRC6(Instrument instrument, int instIdx, ref int idx)
        {
            ReadCommonEnvelopes(instrument, instIdx, ref idx, envelopesExp);
        }

        private void ReadInstrumentVRC7(Instrument instrument, int instIdx, ref int idx)
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

        private void ReadInstrumentFds(Instrument instrument, int instIdx, ref int idx)
        {
            var wavEnv = instrument.Envelopes[EnvelopeType.FdsWaveform];
            var modEnv = instrument.Envelopes[EnvelopeType.FdsModulation];

            for (int i = 0; i < 0x40; i++)
                wavEnv.Values[i] = (sbyte)bytes[idx++];

            for (int i = 0; i < 0x20; i++)
                modEnv.Values[i] = (sbyte)bytes[idx++];

            instrument.FdsWavePreset = WavePresetType.Custom;
            instrument.FdsModPreset  = WavePresetType.Custom;

            modEnv.ConvertFdsModulationToAbsolute();

            instrument.FdsModSpeed = (ushort)BitConverter.ToInt32(bytes, idx); idx += sizeof(int);
            instrument.FdsModDepth =   (byte)BitConverter.ToInt32(bytes, idx); idx += sizeof(int);
            instrument.FdsModDelay =   (byte)BitConverter.ToInt32(bytes, idx); idx += sizeof(int);

            ReadSingleEnvelope(EnvelopeType.Volume,   instrument.Envelopes[EnvelopeType.Volume],   ref idx);
            ReadSingleEnvelope(EnvelopeType.Arpeggio, instrument.Envelopes[EnvelopeType.Arpeggio], ref idx);
            ReadSingleEnvelope(EnvelopeType.Pitch,    instrument.Envelopes[EnvelopeType.Pitch],    ref idx);
        }

        private void ReadInstrumentN163(Instrument instrument, int instIdx, ref int idx)
        {
            ReadCommonEnvelopes(instrument, instIdx, ref idx, envelopesExp);

            var fileWaveSize = BitConverter.ToInt32(bytes, idx); idx += sizeof(int);

            instrument.N163WavePreset = WavePresetType.Custom;
            instrument.N163WaveSize   = (byte)fileWaveSize;
            instrument.N163WavePos    = (byte)BitConverter.ToInt32(bytes, idx); idx += sizeof(int);

            var wavCount = (byte)BitConverter.ToInt32(bytes, idx); idx += sizeof(int);

            instrument.N163WaveCount = wavCount;

            for (int i = 0; i < wavCount; i++)
            {
                if (i < instrument.N163WaveCount)
                {
                    for (int j = 0; j < fileWaveSize; j++)
                        instrument.Envelopes[EnvelopeType.N163Waveform].Values[i * instrument.N163WaveSize + j] = (sbyte)bytes[idx++];
                }
                else
                {
                    // TODO : Give warning here.
                    idx += fileWaveSize;
                }
            }
        }

        private void ReadInstrumentS5B(Instrument instrument, int instIdx, ref int idx)
        {
            ReadCommonEnvelopes(instrument, instIdx, ref idx, envelopesExp);
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
                    Log.LogMessage(LogSeverity.Error, $"Failed to create instrument {index}.");
                    return false;
                }

                switch (type)
                {
                    case ExpansionType.None: ReadInstrument2A03(instrument, index, ref idx); break;
                    case ExpansionType.Vrc6: ReadInstrumentVRC6(instrument, index, ref idx); break;
                    case ExpansionType.Vrc7: ReadInstrumentVRC7(instrument, index, ref idx); break;
                    case ExpansionType.Fds:  ReadInstrumentFds(instrument,  index, ref idx); break;
                    case ExpansionType.N163: ReadInstrumentN163(instrument, index, ref idx); break;
                    case ExpansionType.S5B:  ReadInstrumentS5B(instrument,  index, ref idx); break;
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
            // Version 5 had a serialization error, im not handling this.
            if (blockVersion == 5)
            {
                Log.LogMessage(LogSeverity.Error, $"Unsupported block version, please open the FTM file with FamiTracker 0.4.6 and re-save it to fix the issue.");
                return false;
            }

            var count = BitConverter.ToInt32(bytes, idx); idx += sizeof(int);
            var indices = new int[count];
            var types = new int[count];
            var loops = new int[count];
            var releases = new int[count];
            var settings = new int[count];

            for (int i = 0; i < count; ++i)
            {
                releases[i] = -1;
            }
            
            for (int i = 0; i < count; ++i)
            {
                var index = BitConverter.ToInt32(bytes, idx); idx += sizeof(int);
                var type = BitConverter.ToInt32(bytes, idx); idx += sizeof(int);
                var seqCount = bytes[idx++];
                var loopPoint = BitConverter.ToInt32(bytes, idx); idx += sizeof(int);

                indices[i] = index;
                types[i] = type;
                loops[i] = loopPoint;

                // Version 4 had the release/settings here.
                if (blockVersion == 4)
                {
                    releases[i] = BitConverter.ToInt32(bytes, idx); idx += sizeof(int);
                    settings[i] = BitConverter.ToInt32(bytes, idx); idx += sizeof(int);
                }

                var env = new Envelope(FamiTrackerToFamiStudioEnvelopeLookup[type]);

                if (env.CanResize)
                    env.Length = seqCount;

                envelopeArray[index, type] = env;

                for (int j = 0; j < seqCount; ++j)
                    env.Values[j] = (sbyte)bytes[idx++];
            }

            if (blockVersion == 6)
            {
                for (int i = 0; i < count; ++i)
                {
                    releases[i] = BitConverter.ToInt32(bytes, idx); idx += sizeof(int);
                    settings[i] = BitConverter.ToInt32(bytes, idx); idx += sizeof(int);
                }
            }

            for (int i = 0; i < count; ++i)
            {
                var loopPoint = loops[i];
                var releasePoint = releases[i];
                var setting = settings[i];
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
                env.Relative = type == 2 /* SEQ_PITCH */;

                if (type == 1 /*SEQ_ARPEGGIO*/ && setting != 0)
                    Log.LogMessage(LogSeverity.Warning, $"The arpeggio envelope {indices[i]} uses 'Fixed' or 'Relative' mode. FamiStudio only supports the default 'Absolute' mode.");
            }

            return true;
        }

        private bool ReadSequences(int idx)
        {
            return ReadSequences2A03Vrc6(idx, envelopes);
        }

        private bool ReadSequencesVrc6(int idx)
        {
            if (!project.UsesVrc6Expansion)
                return true;

            return ReadSequences2A03Vrc6(idx, envelopesExp);
        }

        private bool ReadSequencesN163(int idx)
        {
            if (!project.UsesN163Expansion)
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
                var releasePoint = BitConverter.ToInt32(bytes, idx); idx += sizeof(int);
                var setting      = BitConverter.ToInt32(bytes, idx); idx += sizeof(int);

                indices[i] = index;
                types[i] = type;
                loops[i] = loopPoint;

                var envType = type == 4 /* SEQ_DUTYCYCLE */ ? EnvelopeType.WaveformRepeat : FamiTrackerToFamiStudioEnvelopeLookup[type];
                var env = new Envelope(envType);

                if (env.CanResize)
                { 
                    env.Length = seqCount;

                    if (env.Length < seqCount)
                        Log.LogMessage(LogSeverity.Warning, $"{EnvelopeType.LocalizedNames[envType]} envelope {indices[i]} is longer ({seqCount}) than what FamiStudio supports ({env.Length}). Truncating.");
                }
               
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
                { 
                    var val = (sbyte)bytes[idx++];
                    if (j < env.Length)
                        env.Values[j] = val;
                }

                if (type == 1 /*SEQ_ARPEGGIO*/ && setting != 0)
                    Log.LogMessage(LogSeverity.Warning, $"The arpeggio envelope {indices[i]} uses 'Fixed' or 'Relative' mode. FamiStudio only supports the default 'Absolute' mode.");
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
                var dummy   = false;

                // Famitracker can have patterns that arent actually used in the song. 
                // Skip with a dummy pattern.
                if (pattern == null)
                {
                    pattern = new Pattern(project.GenerateUniqueId(), song, channel.Type, $"{patIdx:X2}");
                    dummy = true;
                }

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
                        if (volume != 16 && channel.SupportsEffect(Note.EffectVolume))
                            pattern.GetOrCreateNoteAt(n).Volume = (byte)(volume & 0x0f);

                        if (blockVersion < 5 && project.UsesFdsExpansion && channel.Type == ChannelType.FdsWave && octave < 6 && octave != 0)
                            octave += 2;

                        if (note == 13)
                        {
                            pattern.GetOrCreateNoteAt(n).Value = Note.NoteRelease;
                        }
                        else if (note == 14)
                        {
                            pattern.GetOrCreateNoteAt(n).Value = Note.NoteStop;
                        }
                        else if (note != 0)
                        {
                            if (instrument < MaxInstruments && channel.SupportsInstrument(instruments[instrument]))
                                pattern.GetOrCreateNoteAt(n).Instrument = instruments[instrument];
                            if (channel.Type == ChannelType.Noise)
                                pattern.GetOrCreateNoteAt(n).Value = (byte)(((octave * 12 + note + 15) & 0x0f) + 32);
                            else
                                pattern.GetOrCreateNoteAt(n).Value = (byte)(octave * 12 + note);
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

                        ApplySimpleEffects(fx, pattern, n, !dummy);
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

                samples[index] = CreateUniquelyNamedSampleFromDmcData(name, data);
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
                Log.LogMessage(LogSeverity.Error, "Invalid FTM file ID.");
                return null;
            }

            var version = BitConverter.ToUInt32(bytes, idx); idx += sizeof(uint);
            if (version < MinVersion || version > MaxVersion)
            {
                Log.LogMessage(LogSeverity.Error, "Unsupported file version. Only FTM version 0.4.4 to 0.4.6 are supported.");
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
            project.TempoMode = TempoType.FamiTracker;

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
