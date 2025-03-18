using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace FamiStudio
{
    public class FamitrackerTextFile : FamitrackerFileBase
    {
        private string[] SplitStringKeepQuotes(string str)
        {
            return str.Split('"').Select((element, index) => index % 2 == 0
                                                        ? element.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                                        : new string[] { element })
                                  .SelectMany(element => element).ToArray();
        }

        static readonly Dictionary<string, int> TextToNoteLookup = new Dictionary<string, int>
        {
            { "A-",   9 },
            { "A#",  10 },
            { "B-",  11 },
            { "C-",   0 },
            { "C#",   1 },
            { "D-",   2 },
            { "D#",   3 },
            { "E-",   4 },
            { "F-",   5 },
            { "F#",   6 },
            { "G-",   7 },
            { "G#",   8 }
        };

        static readonly string[] NoteToTextLookup =
        {
            "C-",
            "C#",
            "D-",
            "D#",
            "E-",
            "F-",
            "F#",
            "G-",
            "G#",
            "A-",
            "A#",
            "B-"
        };

        public Project Load(string filename)
        {
            var instruments  = new Dictionary<int, Instrument>();
            var dpcms        = new Dictionary<int, DPCMSample>();
            var columns      = new int[5] { 1, 1, 1, 1, 1 };

            project = new Project();
            project.TempoMode = TempoType.FamiTracker;

            DPCMSample currentDpcm = null;
            int dpcmWriteIdx = 0;
            Song song = null;
            string patternName = "";

            var lines = File.ReadAllLines(filename);

            var headerLine = lines[0].Trim();

            if (headerLine.StartsWith("# FamiTracker text export"))
            {
                var version = headerLine.Substring(26); 
                if (version != "0.4.2")
                {
                    Log.LogMessage(LogSeverity.Warning, $"Invalid FamiTracker text version. Only version 0.4.2 is supported.");
                    return null;
                }
            }
            else
            {
                Log.LogMessage(LogSeverity.Warning, $"Missing header, the file is likely not a FamiTracker text export.");
                return null;
            }

            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i].Trim();

#if !DEBUG
                try
#endif
                { 
                    if (line.StartsWith("TITLE"))
                    {
                        project.Name = line.Substring(5).Trim(' ', '"');
                    }
                    else if (line.StartsWith("AUTHOR"))
                    {
                        project.Author = line.Substring(6).Trim(' ', '"');
                    }
                    else if (line.StartsWith("COPYRIGHT"))
                    {
                        project.Copyright = line.Substring(9).Trim(' ', '"');
                    }
                    else if (line.StartsWith("EXPANSION"))
                    {
                        var exp = int.Parse(line.Substring(9));
                        var convertedExp = ConvertExpansionAudio(exp);

                        if (convertedExp < 0)
                            return null;

                        project.SetExpansionAudioMask(ExpansionType.GetMaskFromValue(convertedExp));
                    }
                    else if (line.StartsWith("MACHINE"))
                    {
                        var machine = int.Parse(line.Substring(8));
                        project.PalMode = machine == 1;
                    }
                    else if (line.StartsWith("N163CHANNELS"))
                    {
                        var numExpChannels = int.Parse(line.Substring(12).Trim(' ', '"'));
                        project.SetExpansionAudioMask(ExpansionType.N163Mask, numExpChannels); 
                    }
                    else if (line.StartsWith("MACRO"))
                    {
                        var expansion = line.StartsWith("MACROVRC6") ? ExpansionType.Vrc6 :
                                        line.StartsWith("MACRON163") ? ExpansionType.N163 : ExpansionType.None;

                        var halves = line.Substring(line.IndexOf(' ')).Split(':');
                        var param = halves[0].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        var curve = halves[1].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                        var type = int.Parse(param[0]);
                        var idx  = int.Parse(param[1]);
                        var loop = int.Parse(param[2]);
                        var rel  = int.Parse(param[3]);
                        var arp  = int.Parse(param[4]);

                        var famistudioType = expansion == ExpansionType.N163 && type == 4 /* SEQ_DUTYCYCLE */ ? EnvelopeType.WaveformRepeat : FamiTrackerToFamiStudioEnvelopeLookup[type];

                        if (famistudioType < EnvelopeType.Count)
                        {
                            var env = new Envelope(famistudioType);
                            env.Length = curve.Length;

                            // FamiTracker allows envelope with release with no loop. We dont allow that.
                            if (env.CanRelease && loop == -1 && rel != -1)
                                loop = rel;

                            env.Loop = loop;
                            env.Release = env.CanRelease && rel != -1 ? rel + 1 : -1;
                            env.Relative = famistudioType == EnvelopeType.Pitch;

                            for (int j = 0; j < curve.Length; j++)
                                env.Values[j] = sbyte.Parse(curve[j]);

                            SetFamiTrackerEnvelope(expansion, type, idx, env);

                            if (famistudioType == EnvelopeType.Arpeggio && arp != 0)
                                Log.LogMessage(LogSeverity.Warning, $"The arpeggio envelope {idx} uses 'Fixed' or 'Relative' mode. FamiStudio only supports the default 'Absolute' mode.");
                        }
                        else
                        {
                            Log.LogMessage(LogSeverity.Warning, $"Hi-pitch envelopes are unsupported, ignoring.");
                        }
                    }
                    else if (line.StartsWith("DPCMDEF"))
                    {
                        var param = SplitStringKeepQuotes(line.Substring(7));
                        currentDpcm = CreateUniquelyNamedSampleFromDmcData(param[2], new byte[int.Parse(param[1])]);
                        dpcms[int.Parse(param[0])] = currentDpcm;
                        dpcmWriteIdx = 0;
                    }
                    else if (line.StartsWith("DPCM"))
                    {
                        if (currentDpcm != null)
                        {
                            var param = line.Substring(6).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            foreach (var s in param)
                            {
                                currentDpcm.SourceDmcData.Data[dpcmWriteIdx++] = Convert.ToByte(s, 16);
                            }
                        }
                    }
                    else if (line.StartsWith("KEYDPCM"))
                    {
                        var param    = line.Substring(7).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        var inst     = instruments[int.Parse(param[0])];
                        int octave   = int.Parse(param[1]);
                        int semitone = int.Parse(param[2]);
                        int note     = octave * 12 + semitone + 1;
                        int dpcm     = int.Parse(param[3]);
                        int pitch    = int.Parse(param[4]);
                        int loop     = int.Parse(param[5]);

                        if (dpcms.TryGetValue(dpcm, out var foundSample))
                        {
                            inst.MapDPCMSample(note, foundSample, pitch, loop != 0);
                        }
                    }
                    else if (line.StartsWith("INST2A03") || line.StartsWith("INSTVRC6") || line.StartsWith("INSTN163"))
                    {
                        var expansion = line.StartsWith("INSTVRC6") ? ExpansionType.Vrc6 : 
                                        line.StartsWith("INSTN163") ? ExpansionType.N163 : ExpansionType.None;

                        var param = SplitStringKeepQuotes(line.Substring(line.IndexOf(' ')));

                        int idx = int.Parse(param[0]);
                        int[] commonEnvelopes = new int[]
                        {
                            int.Parse(param[1]), // Envelope.Volume
                            int.Parse(param[2]), // Envelope.Arpeggio
                            int.Parse(param[3]), // Envelope.Pitch
                            int.Parse(param[5])  // Envelope.DutyCycle
                        };

                        var instrument = CreateUniquelyNamedInstrument(expansion, param[param.Length - 1]);

                        if (expansion == ExpansionType.N163)
                        {
                            instrument.N163WavePreset = WavePresetType.Custom;
                            instrument.N163WaveSize   = byte.Parse(param[6]);
                            instrument.N163WavePos    = byte.Parse(param[7]);
                            instrument.N163WaveCount  = byte.Parse(param[8]);

                            // Store for later, we use a different representation and will need to convert.
                            n163WaveEnvs[instrument] = int.Parse(param[5]); 
                        }

                        var usedEnvelopes = new bool[commonEnvelopes.Length];

                        for (int envTypeIdx = 0; envTypeIdx <= EnvelopeType.DutyCycle; envTypeIdx++)
                        {
                            var envIdx = commonEnvelopes[envTypeIdx];
                            if (envIdx >= 0 && instrument.IsEnvelopeActive(envTypeIdx))
                            {
                                var env = GetFamiTrackerEnvelope(expansion, FamiStudioToFamiTrackerEnvelopeLookup[envTypeIdx], envIdx);
                                if (env != null)
                                {
                                    instrument.Envelopes[envTypeIdx] = env.ShallowClone();
                                    usedEnvelopes[envTypeIdx] = true;
                                }
                            }
                        }

                        if (usedEnvelopes[EnvelopeType.Arpeggio] && usedEnvelopes[EnvelopeType.Pitch])
                        {
                            var arp = instrument.Envelopes[EnvelopeType.Arpeggio];
                            if (arp.IsEmpty(EnvelopeType.Arpeggio) && arp.Length > 0 && arp.Loop >= 0)
                            {
                                Log.LogMessage(LogSeverity.Warning, $"Instrument '{instrument.Name}' uses a looping null arpeggio envelope and a pitch envelope. Assuming envelope should be 'Absolute'.");
                                instrument.Envelopes[EnvelopeType.Pitch].Relative = false;
                            }
                            else
                            {
                                Log.LogMessage(LogSeverity.Warning, $"Instrument '{instrument.Name}' uses both an arpeggio envelope and a pitch envelope. This instrument will likely require manual corrections due to the vastly different handling of those between FamiTracker and FamiStudio.");
                            }
                        }

                        instruments[idx] = instrument;
                    }
                    else if (line.StartsWith("INSTVRC7"))
                    {
                        var param = SplitStringKeepQuotes(line.Substring(line.IndexOf(' ')));

                        int idx = int.Parse(param[0]);
                        var instrument = CreateUniquelyNamedInstrument(ExpansionType.Vrc7, param[param.Length - 1]);

                        instrument.Vrc7Patch = byte.Parse(param[1]);
                        if (instrument.Vrc7Patch == Vrc7InstrumentPatch.Custom)
                        {
                            for (int j = 0; j < 8; j++)
                                instrument.Vrc7PatchRegs[j] = Convert.ToByte(param[2 + j], 16);
                        }

                        instruments[idx] = instrument;
                    }
                    else if (line.StartsWith("INSTFDS"))
                    {
                        var param = SplitStringKeepQuotes(line.Substring(line.IndexOf(' ')));

                        int idx       = int.Parse(param[0]);
                        int modEnable = int.Parse(param[1]);
                        int modSpeed  = int.Parse(param[2]);
                        int modDepth  = int.Parse(param[3]);
                        int modDelay  = int.Parse(param[4]);

                        if (modEnable == 0)
                        {
                            modSpeed = 0;
                            modDepth = 0;
                            modDelay = 0;
                        }

                        instruments[idx] = CreateUniquelyNamedInstrument(ExpansionType.Fds, param[5]);
                        instruments[idx].FdsModSpeed   = (ushort)modSpeed;
                        instruments[idx].FdsModDepth   = (byte)modDepth;
                        instruments[idx].FdsModDelay   = (byte)modDelay;
                        instruments[idx].FdsWavePreset = WavePresetType.Custom;
                        instruments[idx].FdsModPreset  = WavePresetType.Custom;
                    }
                    else if (line.StartsWith("FDSMACRO"))
                    {
                        var halves = line.Substring(line.IndexOf(' ')).Split(':');
                        var param = halves[0].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        var curve = halves[1].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                        var inst = int.Parse(param[0]);
                        var type = int.Parse(param[1]);
                        var loop = int.Parse(param[2]);
                        var rel  = int.Parse(param[3]);

                        var famistudioType = FamiTrackerToFamiStudioEnvelopeLookup[type];

                        var env = instruments[inst].Envelopes[famistudioType];

                        env.Length = curve.Length;

                        // FamiTracker allows envelope with release with no loop. We dont allow that.
                        if (env.CanRelease && loop == -1 && rel != -1)
                            loop = rel;

                        env.Loop = loop;
                        env.Release = env.CanRelease && rel != -1 ? rel + 1 : -1;
                        env.Relative = famistudioType == EnvelopeType.Pitch;

                        for (int j = 0; j < curve.Length; j++)
                            env.Values[j] = sbyte.Parse(curve[j]);
                    }
                    else if (line.StartsWith("FDSMOD") || line.StartsWith("FDSWAVE"))
                    {
                        var mod = line.StartsWith("FDSMOD");
                        var halves = line.Substring(line.IndexOf(' ')).Split(':');
                        var param = halves[0].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        var curve = halves[1].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                        var inst = int.Parse(param[0]);
                        var env = instruments[inst].Envelopes[mod ? EnvelopeType.FdsModulation : EnvelopeType.FdsWaveform];
                        for (int j = 0; j < curve.Length; j++)
                            env.Values[j] = sbyte.Parse(curve[j]);
                        if (mod)
                            env.ConvertFdsModulationToAbsolute();
                    }
                    else if (line.StartsWith("N163WAVE"))
                    {
                        var param = line.Substring(8).Split(new[] { ' ', ':' }, StringSplitOptions.RemoveEmptyEntries);

                        var instIdx = int.Parse(param[0]);
                        var waveIdx = int.Parse(param[1]);

                        // Here we will load all the waveforms, but will later convert to our
                        // representation. FamiTracker use a "wave index" envelope, we simply
                        // store a repeat count for each wave. 
                        var inst = instruments[instIdx];
                        var env  = inst.Envelopes[EnvelopeType.N163Waveform];

                        Debug.Assert(param.Length == inst.N163WaveSize + 2);

                        for (int j = 0; j < inst.N163WaveSize; j++)
                            env.Values[waveIdx * inst.N163WaveSize + j] = sbyte.Parse(param[j + 2]);
                    }
                    else if (line.StartsWith("TRACK"))
                    {
                        var param = SplitStringKeepQuotes(line.Substring(5));

                        song = CreateUniquelyNamedSong(param[3]);
                        song.SetLength(0);
                        song.SetDefaultPatternLength(int.Parse(param[0]));
                        song.FamitrackerSpeed = int.Parse(param[1]);
                        song.FamitrackerTempo = int.Parse(param[2]);
                        columns = new int[song.Channels.Length];
                    }
                    else if (line.StartsWith("COLUMNS"))
                    {
                        var param = line.Substring(7).Split(new[] { ' ', ':' }, StringSplitOptions.RemoveEmptyEntries);
                        for (int j = 0; j < song.Channels.Length; j++)
                            columns[j] = int.Parse(param[j]);
                    }
                    else if (line.StartsWith("ORDER"))
                    {
                        var orderIdx = Convert.ToInt32(line.Substring(6, 2), 16);
                        var values = line.Substring(5).Split(':')[1].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        var order = new int[song.Channels.Length];
                        for (int j = 0; j < song.Channels.Length; j++)
                        {
                            int patternIdx = Convert.ToInt32(values[j], 16);
                            var name = values[j];
                            var pattern = song.Channels[j].GetPattern(name);

                            if (pattern == null)
                                pattern = song.Channels[j].CreatePattern(name);

                            song.Channels[j].PatternInstances[orderIdx] = pattern;
                        }

                        song.SetLength(song.Length + 1);
                    }
                    else if (line.StartsWith("PATTERN"))
                    {
                        patternName = line.Substring(8);
                    }
                    else if (line.StartsWith("ROW"))
                    {
                        var channels = line.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                        var n = Convert.ToInt32(channels[0].Substring(4, 2), 16);

                        for (int j = 1; j <= song.Channels.Length; j++)
                        {
                            var noteData = channels[j].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            var channel = song.Channels[j - 1];
                            var pattern = channel.GetPattern(patternName);

                            if (pattern == null)
                                continue;

                            //var fxData = new RowFxData[song.PatternLength];
                            if (!patternFxData.ContainsKey(pattern))
                                patternFxData[pattern] = new RowFxData[song.PatternLength, 4];

                            // Note
                            if (noteData[0] == "---")
                            {
                                pattern.GetOrCreateNoteAt(n).Value = Note.NoteStop;
                            }
                            else if (noteData[0] == "===")
                            {
                                pattern.GetOrCreateNoteAt(n).Value = Note.NoteRelease;
                            }
                            else if (noteData[0] != "...")
                            {
                                int famitoneNote;

                                if (j == 4)
                                { 
                                    famitoneNote = (Convert.ToInt32(noteData[0].Substring(0, 1), 16) + 31) + 1;
                                }
                                else
                                {
                                    int semitone = TextToNoteLookup[noteData[0].Substring(0, 2)];
                                    int octave = noteData[0][2] - '0';
                                    famitoneNote = octave * 12 + semitone + 1;
                                }

                                if (famitoneNote >= Note.MusicalNoteMin && famitoneNote <= Note.MusicalNoteMax)
                                {
                                    var note = pattern.GetOrCreateNoteAt(n);
                                    note.Value = (byte)famitoneNote;

                                    if (noteData[1] != "..")
                                    {
                                        instruments.TryGetValue(Convert.ToInt32(noteData[1], 16), out var foundInstrument);
                                        if (channel.SupportsInstrument(foundInstrument))
                                            note.Instrument = foundInstrument;
                                    }
                                }
                                else
                                {
                                    // Note outside of range.
                                }
                            }

                            // Volume
                            if (noteData[2] != "." && channel.SupportsEffect(Note.EffectVolume))
                            {
                                pattern.GetOrCreateNoteAt(n).Volume = Convert.ToByte(noteData[2], 16);
                            }

                            // Read FX.
                            for (int k = 0; k < columns[j - 1]; k++)
                            {
                                var fxStr = noteData[3 + k];

                                if (fxStr == "...")
                                    continue;

                                var fx = new RowFxData();

                                if (project.UsesFdsExpansion && FdsTextToEffectLookup.TryGetValue(fxStr[0], out var fdsFx))
                                    fx.fx = (byte)fdsFx;
                                else {
                                    if (!TextToEffectLookup.TryGetValue(fxStr[0], out fx.fx))
                                        Log.LogMessage(LogSeverity.Warning, $"The effect code ({fxStr[0]}) is unknown and will be ignored. {GetPatternString(pattern, n)}");
                                }

                                fx.param = Convert.ToByte(fxStr.Substring(1), 16);
                                patternFxData[pattern][n, k] = fx;

                                ApplySimpleEffects(fx, pattern, n, true);
                            }
                        }
                    }
                }
#if !DEBUG
                catch (Exception e)
                {
                    Log.LogMessage(LogSeverity.Error, $"Line {i} could not be parsed correctly.");
                    Log.LogMessage(LogSeverity.Error, e.Message);
                    Log.LogMessage(LogSeverity.Error, e.StackTrace);

                    return null;
                }
#endif
            }

            FinishImport();

            return project;
        }

        private void ConvertPitchEnvelopes(Project project)
        {
            foreach (var instrument in project.Instruments)
            {
                var env = instrument.Envelopes[EnvelopeType.Pitch];
                if (env != null && !env.IsEmpty(EnvelopeType.Pitch) && !env.Relative)
                {
                    // Make relative.
                    for (int i = env.Length - 1; i > 0; i--)
                    {
                        env.Values[i] -= env.Values[i - 1];
                    }

                    if (env.Loop >= 0)
                    {
                        // Make the looping par sum to zero.
                        int delta = 0;
                        for (int i = env.Loop; i < env.Length; i++)
                        {
                            delta += env.Values[i];
                        }

                        // Force loop part to sum to zero.
                        if (delta != 0)
                        {
                            env.Values[env.Length - 1] = (sbyte)(env.Values[env.Length - 1] - delta);
                        }
                    }
                }
            }
        }

        private Envelope[,][] MergeIdenticalEnvelopes(Project project)
        {
            var uniqueEnvelopes = new Dictionary<uint, Envelope>[2, EnvelopeType.Count];

            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < EnvelopeType.Count; j++)
                {
                    uniqueEnvelopes[i, j] = new Dictionary<uint, Envelope>();
                }
            }

            foreach (var instrument in project.Instruments)
            {
                for (int i = 0; i < EnvelopeType.Count; i++)
                {
                    var env = instrument.Envelopes[i];

                    // Our FDS/N163 use a repeat count and needs to be converted to a 
                    // "wave index" envelope before exporting.
                    if (instrument.IsN163 && i == EnvelopeType.WaveformRepeat)
                        instrument.BuildWaveformsAndWaveIndexEnvelope(out _, out env, false);

                    if (env == null || env.IsEmpty(i))
                        continue;

                    uint crc = env.CRC;

                    if (uniqueEnvelopes[instrument.IsExpansionInstrument ? 1 : 0, i].TryGetValue(crc, out var existingEnv))
                    {
                        instrument.Envelopes[i] = existingEnv;
                    }
                    else
                    {
                        uniqueEnvelopes[instrument.IsExpansionInstrument ? 1 : 0, i][crc] = env;
                    }
                }
            }

            var envelopeArray = new Envelope[2, EnvelopeType.Count][];
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < EnvelopeType.Count; j++)
                {
                    envelopeArray[i, j] = uniqueEnvelopes[i, j].Values.ToArray();
                }
            }

            return envelopeArray;
        }

        private void TruncateLongPatterns(Song song)
        {
            if (song.PatternLength > 256)
            {
                Log.LogMessage(LogSeverity.Warning, $"Number of rows per pattern ({song.PatternLength}) in song properties is more than 256. FamiTracker does not support this and will be truncated to 256.");
                song.SetDefaultPatternLength(256);
            }

            // FamiTracker can only shorten patterns using skips.
            // We allow patterns to be longer than the default, so we will truncate those.
            for (int i = 0; i < song.Length; i++)
            {
                var patternLen = song.GetPatternLength(i);
                if (patternLen > song.PatternLength)
                {
                    Log.LogMessage(LogSeverity.Warning, $"Custom number of rows per pattern ({patternLen}) is longer than the song default ({song.PatternLength}). FamiTracker does not support this and will be truncated.");
                    song.ClearPatternCustomSettings(i);
                }
            }

            song.DeleteNotesPastMaxInstanceLength();
        }

        private void CreateMissingPatterns(Song song)
        {
            foreach (var channel in song.Channels)
            {
                int emptyPatternIdx = -1;
                
                for (int i = 0; i < channel.Patterns.Count; i++)
                {
                    if (!channel.Patterns[i].HasAnyNotes)
                    {
                        emptyPatternIdx = i;
                        break;
                    }
                }

                for (int i = 0; i < song.Length; i++)
                {
                    if (channel.PatternInstances[i] == null)
                    {
                        if (emptyPatternIdx == -1)
                        {
                            emptyPatternIdx = channel.Patterns.IndexOf(channel.CreatePattern());
                        }

                        channel.PatternInstances[i] = channel.Patterns[emptyPatternIdx];
                    }
                }
            }
        }

        private string GetFamiTrackerNoteName(int channel, Note note)
        {
            if (note.IsStop)
            {
                return "---";
            }
            else if (note.IsRelease)
            {
                return "===";
            }
            else if (!note.IsMusical)
            {
                return "...";
            }
            else
            {
                if (channel == ChannelType.Noise)
                {
                    return (note.Value & 0xf).ToString("X") + "-#";
                }
                else
                {
                    int octave = (note.Value - 1) / 12;
                    int semitone = (note.Value - 1) % 12;

                    return NoteToTextLookup[semitone] + octave.ToString();
                }
            }
        }

        private int FindEnvelopeIndex(Envelope[,][] envelopes, Instrument inst, Envelope env, int envType)
        {
            var envList = envelopes[inst.IsExpansionInstrument ? 1 : 0, envType];

            // HACK : Our N163 envelope gets transformed, so we cant compare by reference
            // so we compare by value.
            if (envType == EnvelopeType.WaveformRepeat)
            {
                var crc = env.CRC;
                for (int i = 0; i < envList.Length; i++)
                {
                    if (crc == envList[i].CRC)
                        return i;
                }

                return -1;
            }
            else
            {
                return env != null && env.Length > 0 ? Array.IndexOf(envList, env) : -1;
            }
        }

        private bool ComparePatternLines(List<string> p0, List<string> p1)
        {
            if (p0.Count != p1.Count)
            {
                return false;
            }

            for (int i = 0; i < p0.Count; i++)
            {
                if (p0[i] != p1[i])
                {
                    Debug.WriteLine($"**** {p0[i]} != {p1[i]}");
                    return false;
                }
            }

            return true;
        }

        public bool Save(Project originalProject, string filename, int[] songIds)
        {
            if (originalProject.UsesMultipleExpansionAudios)
            {
                Log.LogMessage(LogSeverity.Error, $"Project uses multiple audio expansions. The original FamiTracker did not support this.");
                return false;
            }

            var project = originalProject.DeepClone();
            project.DeleteAllSongsBut(songIds);

            if (project.UsesFamiStudioTempo)
            {
                Log.LogMessage(LogSeverity.Warning, $"Song uses FamiStudio tempo. Tempo will be converted with speed of 1 and tempo 150. If you need FamiTracker compatibility, it is recommended that you use FamiTracker tempo in the future.");
                project.ConvertToFamiTrackerTempo(false);
            }

            project.ConvertToSimpleNotes();

            ConvertPitchEnvelopes(project);
            var envelopes = MergeIdenticalEnvelopes(project);

            var uniqueWarnings = new HashSet<string>();
            var lines = new List<string>();

            lines.Add("# FamiTracker text export 0.4.2");
            lines.Add("");

            lines.Add("# Song information");
            lines.Add("TITLE           \"" + project.Name      + "\"");
            lines.Add("AUTHOR          \"" + project.Author    + "\"");
            lines.Add("COPYRIGHT       \"" + project.Copyright + "\"");
            lines.Add("");

            lines.Add("# Song comment");
            lines.Add("COMMENT         \"\"");

            lines.Add("# Global settings");
            lines.Add("MACHINE         " + (project.PalMode ? "1" : "0"));
            lines.Add("FRAMERATE       0");
            lines.Add("EXPANSION       " + project.ExpansionAudioMask);
            lines.Add("VIBRATO         1");
            lines.Add("SPLIT           32");
            lines.Add("");

            var realNumExpansionChannels = project.ExpansionNumN163Channels;

            if (project.UsesN163Expansion)
            {
                lines.Add("# Namco 163 global settings");
                lines.Add($"N163CHANNELS    {project.ExpansionNumN163Channels}");
                lines.Add("");

                // The text format always export all 8 channels, even if there are less.
                project.SetExpansionAudioMask(project.ExpansionAudioMask, 8);
            }

            lines.Add("# Macros");
            for (int i = 0; i < EnvelopeType.RegularCount; i++)
            {
                var envArray = envelopes[ExpansionType.None, i];
                for (int j = 0; j < envArray.Length; j++)
                {
                    var env = envArray[j];
                    lines.Add($"MACRO{FamiStudioToFamiTrackerEnvelopeLookup[i],8} {j,4} {env.Loop,4} {(env.Release >= 0 ? env.Release - 1 : -1),4}   0 : {string.Join(" ", env.Values.Take(env.Length))}");
                }
            }

            if (project.UsesVrc6Expansion || project.UsesN163Expansion)
            {
                var suffix = project.UsesVrc6Expansion ? "VRC6" : "N163";

                for (int i = 0; i < EnvelopeType.RegularCount; i++)
                {
                    var envArray = envelopes[1, i];
                    for (int j = 0; j < envArray.Length; j++)
                    {
                        var env = envArray[j];
                        lines.Add($"MACRO{suffix}{FamiStudioToFamiTrackerEnvelopeLookup[i],8} {j,4} {env.Loop,4} {(env.Release >= 0 ? env.Release - 1 : -1),4}   0 : {string.Join(" ", env.Values.Take(env.Length))}");
                    }
                }

                // Special case for N163 wave index envelopes.
                if (project.UsesN163Expansion)
                {
                    var envArray = envelopes[1, EnvelopeType.WaveformRepeat];
                    for (int j = 0; j < envArray.Length; j++)
                    {
                        var env = envArray[j];
                        lines.Add($"MACRO{suffix}{4 /* SEQ_DUTYCYCLE */,8} {j,4} {env.Loop,4} {(env.Release >= 0 ? env.Release - 1 : -1),4}   0 : {string.Join(" ", env.Values.Take(env.Length))}");
                    }
                }
            }

            lines.Add("");

            if (project.UsesSamples)
            {
                lines.Add("# DPCM samples");
                for (int i = 0; i < project.Samples.Count; i++)
                {
                    var sample = project.Samples[i];
                    lines.Add($"DPCMDEF{i,4}{sample.ProcessedData.Length,6} \"{sample.Name}\"");
                    lines.Add($"DPCM : {String.Join(" ", sample.ProcessedData.Select(x => $"{x:X2}"))}");
                }
                lines.Add("");
            }

            lines.Add("# Instruments");
            for (int i = 0; i < project.Instruments.Count; i++)
            {
                var instrument = project.Instruments[i];

                var volEnv = instrument.Envelopes[EnvelopeType.Volume];
                var arpEnv = instrument.Envelopes[EnvelopeType.Arpeggio];
                var pitEnv = instrument.Envelopes[EnvelopeType.Pitch];
                var dutEnv = instrument.Envelopes[EnvelopeType.DutyCycle];

                var expIdx    = instrument.IsExpansionInstrument ? 1 : 0;
                int volEnvIdx = FindEnvelopeIndex(envelopes, instrument, volEnv, EnvelopeType.Volume);
                int arpEnvIdx = FindEnvelopeIndex(envelopes, instrument, arpEnv, EnvelopeType.Arpeggio);
                int pitEnvIdx = FindEnvelopeIndex(envelopes, instrument, pitEnv, EnvelopeType.Pitch);
                int dutEnvIdx = FindEnvelopeIndex(envelopes, instrument, dutEnv, EnvelopeType.DutyCycle);

                if (instrument.IsRegular)
                {
                    lines.Add($"INST2A03{i,4}{volEnvIdx,6}{arpEnvIdx,4}{pitEnvIdx,4}{-1,4}{dutEnvIdx,4} \"{instrument.Name}\"");

                    if (instrument.HasAnyMappedSamples)
                    {
                        foreach (var kv in instrument.SamplesMapping)
                        {
                            var note = kv.Key;
                            var mapping = kv.Value;

                            if (mapping != null)
                            {
                                var idx = project.Samples.IndexOf(mapping.Sample);
                                if (idx >= 0)
                                {
                                    var octave   = (note - 1) / 12;
                                    var semitone = (note - 1) % 12;
                                    var loop     = mapping.Loop ? 1 : 0;

                                    lines.Add($"KEYDPCM{i,4}{octave,4}{semitone,4}{idx,6}{mapping.Pitch,4}{loop,4}{0,6}{-1,4}");
                                }
                            }
                        }
                    }
                }
                else if (instrument.IsVrc6)
                {
                    lines.Add($"INSTVRC6{i,4}{volEnvIdx,6}{arpEnvIdx,4}{pitEnvIdx,4}{-1,4}{dutEnvIdx,4} \"{instrument.Name}\"");
                }
                else if (instrument.IsVrc7)
                {
                    lines.Add($"INSTVRC7{i,4}{instrument.Vrc7Patch,4} {String.Join(" ", instrument.Vrc7PatchRegs.Select(x => $"{x:X2}"))} \"{instrument.Name}\"");

                    if (!instrument.IsEnvelopeEmpty(EnvelopeType.Volume) ||
                        !instrument.IsEnvelopeEmpty(EnvelopeType.Pitch)  ||
                        !instrument.IsEnvelopeEmpty(EnvelopeType.Arpeggio))
                    {
                        Log.LogMessage(LogSeverity.Warning, $"The VRC7 Instrument '{instrument.Name}' uses a volume, pitch or arpeggio envelope. FamiTracker does not support this. Ignoring.");
                    }
                }
                else if (instrument.IsN163)
                {
                    instrument.BuildWaveformsAndWaveIndexEnvelope(out var waves, out var wavIndexEnv, false);

                    var wavIndexEnvIdx = FindEnvelopeIndex(envelopes, instrument, wavIndexEnv, EnvelopeType.WaveformRepeat);
                    lines.Add($"INSTN163{i,4}{volEnvIdx,6}{arpEnvIdx,4}{pitEnvIdx,4}{-1,4}{wavIndexEnvIdx,4}{instrument.N163WaveSize,4}{instrument.N163WavePos,4}{waves.Length,4} \"{instrument.Name}\"");

                    for (int j = 0; j < waves.Length; j++)
                        lines.Add($"N163WAVE{i,4}{j,6} : {string.Join(" ", waves[j])}");
                }
                else if (instrument.IsFds)
                {
                    lines.Add($"INSTFDS{i,5}{1,6}{instrument.FdsModSpeed,4}{instrument.FdsModDepth,4}{instrument.FdsModDelay,4} \"{instrument.Name}\"");
                    
                    if (instrument.FdsWaveCount > 1)
                    {
                        uniqueWarnings.Add($"FDS Instrument '{instrument.Name}' uses multiple waveforms, the original FamiTracker only supported one, truncating.");
                        instrument.FdsWaveCount = 1;
                    }
                    
                    var wavEnv = instrument.Envelopes[EnvelopeType.FdsWaveform];
                    lines.Add($"FDSWAVE{i,5} : {string.Join(" ", wavEnv.Values.Take(wavEnv.Length))}");
                    var modEnv = instrument.Envelopes[EnvelopeType.FdsModulation].BuildFdsModulationTable();
                    lines.Add($"FDSMOD{i,6} : {string.Join(" ", modEnv.Take(modEnv.Length))}");

                    for (int j = 0; j <= EnvelopeType.Pitch; j++)
                    {
                        var env = instrument.Envelopes[j];
                        if (!env.IsEmpty(j))
                            lines.Add($"FDSMACRO{i,4} {j,5} {env.Loop,4} {(env.Release >= 0 ? env.Release - 1 : -1),4}   0 : {string.Join(" ", env.Values.Take(env.Length))}");
                    }
                }
            }

            lines.Add("");

            lines.Add("# Tracks");
            for (int i = 0; i < project.Songs.Count; i++)
            {
                var song = project.Songs[i];

                TruncateLongPatterns(song);
                CreateMissingPatterns(song);
                song.CleanupUnusedPatterns();
                song.MakePatternsWithDifferentLengthsUnique();

                // Multi-pass export to detect when the same pattern generates different output.
                while (true)
                {
                    var success = true;
                    var maxPatternCount = -1;
                    foreach (var channel in song.Channels)
                        maxPatternCount = Math.Max(maxPatternCount, channel.Patterns.Count);

                    var patternRows = new Dictionary<Pattern, List<string>>();

                    for (int c = 0; c < song.Channels.Length && success; c++)
                    {
                        var channel = song.Channels[c];
                        var prevNoteValue = Note.NoteInvalid;
                        var prevInstrument = (Instrument)null;
                        var prevSlideEffect = Effect_None;
                        var activeVolumeSlide = false;
                        var prevArpeggio = (Arpeggio)null;
                        var famitrackerSpeed = song.FamitrackerSpeed;
                    
                        for (int p = 0; p < song.Length; p++)
                        {
                            var pattern = channel.PatternInstances[p];
                            var patternLen = song.GetPatternLength(p);

                            var patternLines = new List<string>();

                            for (var it = pattern.GetDenseNoteIterator(0, song.PatternLength); !it.Done; it.Next())
                            {
                                var location = new NoteLocation(p, it.CurrentTime);
                                var note = it.CurrentNote;

                                song.ApplySpeedEffectAt(location, ref famitrackerSpeed);

                                // Keeps the code a lot simpler.
                                if (note == null)
                                    note = Note.EmptyNote;

                                var line = " : ... .. . ... ... ...";

                                var noteString = GetFamiTrackerNoteName(c, note);
                                var volumeString = note.HasVolume ? note.Volume.ToString("X") : ".";
                                var instrumentString = note.IsValid && !note.IsStop ? (note.Instrument == null ? project.Instruments.Count : project.Instruments.IndexOf(note.Instrument)).ToString("X2") : "..";
                                var effectString = "";
                                var noAttack = !note.HasAttack && prevNoteValue == note.Value && (prevSlideEffect == Effect_None || prevSlideEffect == Effect_SlideUp || prevSlideEffect == Effect_Portamento);

                                if (note.IsSlideNote && note.IsMusical)
                                {
                                    var noteTable = NesApu.GetNoteTableForChannelType(channel.Type, false, realNumExpansionChannels);

                                    var noteValue   = note.Value;
                                    var slideTarget = note.SlideNoteTarget;

                                    // FamiTracker only has 12-pitches and doesnt change the octave when doing 
                                    // slides. This helps make the slides more compatible, but its not great.
                                    if (channel.IsVrc7Channel)
                                    {
                                        while (noteValue >= 12 && slideTarget >= 12)
                                        {
                                            noteValue   -= 12;
                                            slideTarget -= 12;
                                        }
                                    }

                                    var tempNote = note.Clone();
                                    tempNote.Value = note.Value;
                                    tempNote.SlideNoteTarget = slideTarget;
                                    channel.ComputeSlideNoteParams(tempNote, location, famitrackerSpeed, noteTable, false, false, out _, out _, out var stepSizeFloat);

                                    if (channel.IsN163Channel)
                                    {
                                        stepSizeFloat /= 4.0f;
                                    }
                                    else if (channel.IsNoiseChannel)
                                    {
                                        uniqueWarnings.Add($"Slide notes on the noise channel will almost certainly require manual correct. FamiTracker's support for them is very limited.");
                                    }

                                    // Undo any kind of shifting we had done. This will kill the 1-bit of fraction we have on most channel.
                                    var absNoteDelta  = Math.Abs(note.Value - note.SlideNoteTarget);
                                    var force1xx2xx = channel.IsNoiseChannel;

                                    // See if we can use Qxy/Rxy (slide up/down y semitones, at speed x), this is preferable.
                                    if (absNoteDelta < 16 && !force1xx2xx)
                                    {
                                        if (prevSlideEffect == Effect_PortaUp   ||
                                            prevSlideEffect == Effect_PortaDown ||
                                            prevSlideEffect == Effect_Portamento)
                                        {
                                            effectString += $" {EffectToTextLookup[prevSlideEffect]}00";
                                        }

                                        // FamiTracker use 2x + 1, find the number that is just above our speed.
                                        var speed = 0;
                                        for (int x = 14; x >= 0; x--)
                                        {
                                            if ((2 * x + 1) < Math.Abs(stepSizeFloat))
                                            {
                                                speed = x + 1;
                                                break;
                                            }
                                        }

                                        if (note.SlideNoteTarget > note.Value)
                                            effectString += $" Q{speed:X1}{absNoteDelta:X1}";
                                        else
                                            effectString += $" R{speed:X1}{absNoteDelta:X1}";

                                        prevSlideEffect = Effect_SlideUp;
                                    }
                                    else
                                    {
                                        // We have one bit of fraction. FramiTracker does not.
                                        var ceilStepSize = Utils.SignedCeil(stepSizeFloat);

                                        // If the previous note matched too, we can use 3xx (auto-portamento).
                                        // Avoid using portamento on instrument with relative pitch envelopes, their previous pitch isnt reliable.
                                        if (!force1xx2xx && prevNoteValue == note.Value && (prevInstrument == null || prevInstrument.Envelopes[EnvelopeType.Pitch].IsEmpty(EnvelopeType.Pitch) || !prevInstrument.Envelopes[EnvelopeType.Pitch].Relative))
                                        {
                                            if (prevSlideEffect == Effect_PortaUp ||
                                                prevSlideEffect == Effect_PortaDown)
                                            {
                                                effectString += $" 100";
                                            }

                                            noteString = GetFamiTrackerNoteName(c, new Note(note.SlideNoteTarget));
                                            effectString += $" 3{Math.Min(0xff, Math.Abs(ceilStepSize)):X2}";
                                            prevSlideEffect = Effect_Portamento;
                                            noAttack = false; // Need to force attack when starting auto-portamento unfortunately.
                                        }
                                        else
                                        {
                                            // Inverted channels.
                                            if (channel.IsFdsChannel || channel.IsN163Channel)
                                                stepSizeFloat = -stepSizeFloat;

                                            var absFloorStepSize = Math.Abs(Utils.SignedCeil(stepSizeFloat));

                                            if (prevSlideEffect == Effect_Portamento)
                                                effectString += $" 300";

                                            var fx = channel.IsNoiseChannel ? 
                                                (note.SlideNoteTarget > note.Value ? "2" : "1") : 
                                                (note.SlideNoteTarget > note.Value ? "1" : "2");

                                            effectString += $" {fx}{Math.Min(0xff, absFloorStepSize):X2}";

                                            // Doesnt matter if we set up/down.
                                            prevSlideEffect = Effect_PortaUp;
                                        }
                                    }
                                }
                                else if ((note.IsMusical || note.IsStop) && prevSlideEffect != Effect_None)
                                {
                                    if (prevSlideEffect == Effect_PortaUp   ||
                                        prevSlideEffect == Effect_PortaDown ||
                                        prevSlideEffect == Effect_Portamento)
                                    {
                                        effectString += $" {EffectToTextLookup[prevSlideEffect]}00";
                                    }

                                    prevSlideEffect = Effect_None;
                                }

                                if (note.HasVolumeSlide)
                                {
                                    if (channel.ComputeVolumeSlideNoteParams(note, location, famitrackerSpeed, false, out _, out var stepSizeFloat))
                                    {
                                        if (stepSizeFloat < 0)
                                        {
                                            var clampedSlope = Utils.Clamp((int)Math.Round(-stepSizeFloat * 8.0f), 0, 15);
                                            effectString += $" A{clampedSlope << 4:X2}";
                                        }
                                        else
                                        {
                                            var clampedSlope = Utils.Clamp((int)Math.Round(stepSizeFloat * 8.0f), 0, 15);
                                            effectString += $" A{clampedSlope:X2}";
                                        }

                                        activeVolumeSlide = true;
                                    }
                                }
                                else if (note.HasVolume && activeVolumeSlide)
                                {
                                    effectString += $" A00";
                                    activeVolumeSlide = false;
                                }

                                if (location.NoteIndex == patternLen - 1)
                                {
                                    if (p == song.Length - 1 && song.LoopPoint >= 0)
                                        effectString += $" B{song.LoopPoint:X2}";
                                    else if (patternLen != song.PatternLength)
                                        effectString += $" D00";
                                }

                                if (note.HasSpeed)
                                    effectString += $" F{note.Speed:X2}";
                                if (note.HasVibrato)
                                    effectString += $" 4{VibratoSpeedExportLookup[note.VibratoSpeed]:X1}{note.VibratoDepth:X1}";
                                if (note.HasFinePitch)
                                    effectString += $" P{(byte)(-note.FinePitch + 0x80):X2}";
                                if (note.HasFdsModDepth)
                                    effectString += $" H{note.FdsModDepth:X2}";
                                if (note.HasDutyCycle)
                                    effectString += $" V{note.DutyCycle:X2}";
                                if (note.HasNoteDelay)
                                    effectString += $" G{note.NoteDelay:X2}";
                                if (note.HasCutDelay)
                                    effectString += $" S{note.CutDelay:X2}";
                                if (note.HasDeltaCounter)
                                    effectString += $" Z{note.DeltaCounter:X2}";

                                if (note.IsMusical && note.Arpeggio != prevArpeggio)
                                {
                                    var arpeggioString = " 0";

                                    if (note.Arpeggio != null)
                                    {
                                        arpeggioString += note.Arpeggio.Envelope.Length >= 2 ? $"{Utils.Clamp(note.Arpeggio.Envelope.Values[1], 0, 15):X1}" : "0";
                                        arpeggioString += note.Arpeggio.Envelope.Length >= 3 ? $"{Utils.Clamp(note.Arpeggio.Envelope.Values[2], 0, 15):X1}" : "0";

                                        if (note.Arpeggio.Envelope.Length    != 3 ||
                                            note.Arpeggio.Envelope.Values[0] != 0)
                                        {
                                            uniqueWarnings.Add($"FamiTracker only supports arpeggios of exactly 3 notes and the first value needs to be zero. Arpeggio {note.Arpeggio.Name} will not sound correct.");
                                        }
                                    }
                                    else
                                    {
                                        arpeggioString += "00";
                                    }

                                    effectString += arpeggioString;
                                    prevArpeggio  = note.Arpeggio;
                                }

                                if (note.HasFdsModSpeed)
                                {
                                    effectString += $" I{(note.FdsModSpeed >> 8) & 0xff:X2}";
                                    effectString += $" J{(note.FdsModSpeed >> 0) & 0xff:X2}";
                                }

                                while (effectString.Length < 12)
                                    effectString += " ...";

                                if (noAttack)
                                {
                                    noteString = "...";
                                    instrumentString = "..";
                                }

                                line = $" : {noteString} {instrumentString} {volumeString}{effectString}";

                                if (note.IsMusical || note.IsStop)
                                {
                                    prevNoteValue = note.IsSlideNote ? note.SlideNoteTarget : note.Value;
                                    if (note.IsMusical)
                                        prevInstrument = note.Instrument;
                                }

                                patternLines.Add(line);
                            }


                            if (patternRows.TryGetValue(pattern, out var existingPatternLines))
                            {
                                if (!ComparePatternLines(existingPatternLines, patternLines))
                                {
                                    uniqueWarnings.Add($"Pattern '{pattern.Name}' generated different outputs. Making unique.");
                                    channel.PatternInstances[p] = pattern.ShallowClone();
                                    success = false;
                                    break;
                                }

                                continue;
                            }

                            patternRows[pattern] = patternLines;
                        }
                    }

                    if (success)
                    {
                        lines.Add($"TRACK{song.PatternLength,4}{song.FamitrackerSpeed,4}{song.FamitrackerTempo,4} \"{song.Name}\"");
                        lines.Add($"COLUMNS : {string.Join(" ", Enumerable.Repeat(3, song.Channels.Length))}");
                        lines.Add("");

                        for (int j = 0; j < song.Length; j++)
                        {
                            var line = $"ORDER {j:X2} :";

                            for (int k = 0; k < song.Channels.Length; k++)
                                line += $" {song.Channels[k].Patterns.IndexOf(song.Channels[k].PatternInstances[j]):X2}";

                            lines.Add(line);
                        }
                        lines.Add("");

                        for (int j = 0; j < maxPatternCount; j++)
                        {
                            lines.Add($"PATTERN {j:X2}");

                            for (int p = 0; p < song.PatternLength; p++)
                            {
                                var line = $"ROW {p:X2}";
                                for (int c = 0; c < song.Channels.Length; c++)
                                {
                                    var channel = song.Channels[c];

                                    if (j >= channel.Patterns.Count)
                                        line += " : ... .. . ... ... ...";
                                    else
                                        line += patternRows[channel.Patterns[j]][p];
                                }

                                lines.Add(line);
                            }

                            lines.Add("");
                        }

                        break;
                    }
                }
            }

            lines.Add("# End of export");

            foreach (var warning in uniqueWarnings)
                Log.LogMessage(LogSeverity.Warning, warning);

            File.WriteAllLines(filename, lines);

            return true;
        }
    }
}
