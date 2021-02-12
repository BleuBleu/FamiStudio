﻿using System;
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
            var envelopes   = new Dictionary<int, Envelope>[ExpansionType.Count, EnvelopeType.Count];
            var instruments = new Dictionary<int, Instrument>();
            var dpcms       = new Dictionary<int, DPCMSample>();
            var columns     = new int[5] { 1, 1, 1, 1, 1 };

            for (int i = 0; i < envelopes.GetLength(0); i++)
                for (int j = 0; j < envelopes.GetLength(1); j++)
                    envelopes[i, j] = new Dictionary<int, Envelope>();

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
                Log.LogMessage(LogSeverity.Warning, $"Missing header, file is likely not a FamiTracker text export.");
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

                        project.SetExpansionAudio(convertedExp);
                    }
                    else if (line.StartsWith("MACHINE"))
                    {
                        var machine = int.Parse(line.Substring(8));
                        project.PalMode = machine == 1;
                    }
                    else if (line.StartsWith("N163CHANNELS"))
                    {
                        var numExpChannels = int.Parse(line.Substring(12).Trim(' ', '"'));
                        project.SetExpansionAudio(ExpansionType.N163, numExpChannels);
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

                        var famistudioType = EnvelopeTypeLookup[type];

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

                            envelopes[expansion, famistudioType][idx] = env;

                            if (famistudioType == EnvelopeType.Arpeggio && arp != 0)
                                Log.LogMessage(LogSeverity.Warning, $"Arpeggio envelope {idx} uses 'Fixed' or 'Relative' mode. FamiStudio only supports the default 'Absolute' mode.");
                        }
                        else
                        {
                            Log.LogMessage(LogSeverity.Warning, $"Hi-pitch envelopes are unsupported, ignoring.");
                        }
                    }
                    else if (line.StartsWith("DPCMDEF"))
                    {
                        var param = SplitStringKeepQuotes(line.Substring(7));
                        currentDpcm = CreateUniquelyNamedSample(param[2], new byte[int.Parse(param[1])]);
                        dpcms[int.Parse(param[0])] = currentDpcm;
                        dpcmWriteIdx = 0;

                        if (currentDpcm == null)
                            Log.LogMessage(LogSeverity.Warning, $"Cannot allocate DPCM sample '{param[2]}'. Maximum total size allowed is 16KB.");
                    }
                    else if (line.StartsWith("DPCM"))
                    {
                        if (currentDpcm != null) // Can happen if more than 16KB of samples
                        {
                            var param = line.Substring(6).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            foreach (var s in param)
                            {
                                currentDpcm.ProcessedData[dpcmWriteIdx++] = Convert.ToByte(s, 16);
                            }
                        }
                    }
                    else if (line.StartsWith("KEYDPCM"))
                    {
                        var param = line.Substring(7).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (param[0] == "0")
                        {
                            int octave   = int.Parse(param[1]);
                            int semitone = int.Parse(param[2]);
                            int note = octave * 12 + semitone + 1;

                            if (project.NoteSupportsDPCM(note))
                            {
                                int dpcm  = int.Parse(param[3]);
                                int pitch = int.Parse(param[4]);
                                int loop  = int.Parse(param[5]);

                                if (dpcms.TryGetValue(dpcm, out var foundSample))
                                    project.MapDPCMSample(note, foundSample, pitch, loop != 0);
                            }
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

                            var wavCount = byte.Parse(param[8]);
                            if (wavCount > 1)
                                Log.LogMessage(LogSeverity.Warning, $"N163 instrument '{instrument.Name}' has more than 1 waveform ({wavCount}). All others will be ignored.");
                        }

                        for (int envTypeIdx = 0; envTypeIdx <= EnvelopeType.DutyCycle; envTypeIdx++)
                        {
                            int envIdx = commonEnvelopes[envTypeIdx];
                            if (envIdx >= 0 && instrument.IsEnvelopeActive(envTypeIdx) && envelopes[expansion, envTypeIdx].TryGetValue(envIdx, out var foundEnv) && foundEnv != null)
                                instrument.Envelopes[envTypeIdx] = envelopes[expansion, envTypeIdx][envIdx].ShallowClone();
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

                        var famistudioType = EnvelopeTypeLookup[type];

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
                        var param = line.Substring(8).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                        var instIdx = int.Parse(param[0]);
                        var waveIdx = int.Parse(param[1]);

                        // TODO: We could create different instruments for each wave.
                        if (waveIdx == 0)
                        {
                            var env = instruments[instIdx].Envelopes[EnvelopeType.N163Waveform];

                            for (int j = 3; j < param.Length; j++)
                                env.Values[j - 3] = sbyte.Parse(param[j]);
                        }
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
                            var pattern = song.Channels[j - 1].GetPattern(patternName);

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
                                    instruments.TryGetValue(Convert.ToInt32(noteData[1], 16), out var foundInstrument);
                                    note.Value = (byte)famitoneNote;
                                    note.Instrument = j == 5 ? null : foundInstrument;
                                }
                                else
                                {
                                    // Note outside of range.
                                }
                            }

                            // Volume
                            if (noteData[2] != ".")
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

                                if (project.ExpansionAudio == ExpansionType.Fds && FdsTextToEffectLookup.TryGetValue(fxStr[0], out var fdsFx))
                                    fx.fx = (byte)fdsFx;
                                else
                                    fx.fx = TextToEffectLookup[fxStr[0]];

                                fx.param = Convert.ToByte(fxStr.Substring(1), 16);
                                patternFxData[pattern][n, k] = fx;

                                ApplySimpleEffects(fx, pattern, n, patternLengths, true);
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
                if (env != null && !env.IsEmpty && !env.Relative)
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

                    if (env == null || env.IsEmpty)
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

        public bool Save(Project originalProject, string filename, int[] songIds)
        {
            var project = originalProject.DeepClone();
            project.RemoveAllSongsBut(songIds);

            if (project.UsesFamiStudioTempo)
            {
                Log.LogMessage(LogSeverity.Warning, $"Song uses FamiStudio tempo. Will be exported with speed of 1, tempo 150.");
                project.ConvertToFamiTrackerTempo(false);
            }

            ConvertPitchEnvelopes(project);
            var envelopes = MergeIdenticalEnvelopes(project);

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
            lines.Add("EXPANSION       " + (project.ExpansionAudio != ExpansionType.None ? (1 << (project.ExpansionAudio - 1)) : 0));
            lines.Add("VIBRATO         1");
            lines.Add("SPLIT           32");
            lines.Add("");

            var realNumExpansionChannels = project.ExpansionNumChannels;

            if (project.ExpansionAudio == ExpansionType.N163)
            {
                lines.Add("# Namco 163 global settings");
                lines.Add($"N163CHANNELS    {project.ExpansionNumChannels}");
                lines.Add("");

                // The text format always export all 8 channels, even if there are less.
                project.SetExpansionAudio(ExpansionType.N163, 8);
            }

            lines.Add("# Macros");
            for (int i = 0; i < EnvelopeType.RegularCount; i++)
            {
                var envArray = envelopes[ExpansionType.None, i];
                for (int j = 0; j < envArray.Length; j++)
                {
                    var env = envArray[j];
                    lines.Add($"MACRO{ReverseEnvelopeTypeLookup[i],8} {j,4} {env.Loop,4} {(env.Release >= 0 ? env.Release - 1 : -1),4}   0 : {string.Join(" ", env.Values.Take(env.Length))}");
                }
            }

            if (project.ExpansionAudio == ExpansionType.Vrc6 ||
                project.ExpansionAudio == ExpansionType.N163)
            {
                var suffix = project.ExpansionAudio == ExpansionType.Vrc6 ? "VRC6" : "N163";

                for (int i = 0; i < EnvelopeType.RegularCount; i++)
                {
                    var envArray = envelopes[1, i];
                    for (int j = 0; j < envArray.Length; j++)
                    {
                        var env = envArray[j];
                        lines.Add($"MACRO{suffix}{i,8} {j,4} {env.Loop,4} {(env.Release >= 0 ? env.Release - 1 : -1),4}   0 : {string.Join(" ", env.Values.Take(env.Length))}");
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
                int volEnvIdx = volEnv != null && volEnv.Length > 0 ? Array.IndexOf(envelopes[expIdx, EnvelopeType.Volume],    instrument.Envelopes[EnvelopeType.Volume])    : -1;
                int arpEnvIdx = arpEnv != null && arpEnv.Length > 0 ? Array.IndexOf(envelopes[expIdx, EnvelopeType.Arpeggio],  instrument.Envelopes[EnvelopeType.Arpeggio])  : -1;
                int pitEnvIdx = pitEnv != null && pitEnv.Length > 0 ? Array.IndexOf(envelopes[expIdx, EnvelopeType.Pitch],     instrument.Envelopes[EnvelopeType.Pitch])     : -1;
                int dutEnvIdx = dutEnv != null && dutEnv.Length > 0 ? Array.IndexOf(envelopes[expIdx, EnvelopeType.DutyCycle], instrument.Envelopes[EnvelopeType.DutyCycle]) : -1;

                if (instrument.ExpansionType == ExpansionType.None)
                {
                    lines.Add($"INST2A03{i,4}{volEnvIdx,6}{arpEnvIdx,4}{pitEnvIdx,4}{-1,4}{dutEnvIdx,4} \"{instrument.Name}\"");
                }
                else if (instrument.ExpansionType == ExpansionType.Vrc6)
                {
                    lines.Add($"INSTVRC6{i,4}{volEnvIdx,6}{arpEnvIdx,4}{pitEnvIdx,4}{-1,4}{dutEnvIdx,4} \"{instrument.Name}\"");
                }
                else if (instrument.ExpansionType == ExpansionType.Vrc7)
                {
                    lines.Add($"INSTVRC7{i,4}{instrument.Vrc7Patch,4} {String.Join(" ", instrument.Vrc7PatchRegs.Select(x => $"{x:X2}"))} \"{instrument.Name}\"");
                }
                else if (instrument.ExpansionType == ExpansionType.N163)
                {
                    lines.Add($"INSTN163{i,4}{volEnvIdx,6}{arpEnvIdx,4}{pitEnvIdx,4}{-1,4}{dutEnvIdx,4}{instrument.N163WaveSize,4}{instrument.N163WavePos,4}{1,4} \"{instrument.Name}\"");

                    var wavEnv = instrument.Envelopes[EnvelopeType.N163Waveform];
                    lines.Add($"N163WAVE{i,4}{0,6} : {string.Join(" ", wavEnv.Values.Take(wavEnv.Length))}");
                }
                else if (instrument.ExpansionType == ExpansionType.Fds)
                {
                    lines.Add($"INSTFDS{i,5}{1,6}{instrument.FdsModSpeed,4}{instrument.FdsModDepth,4}{instrument.FdsModDelay,4} \"{instrument.Name}\"");

                    var wavEnv = instrument.Envelopes[EnvelopeType.FdsWaveform];
                    lines.Add($"FDSWAVE{i,5} : {string.Join(" ", wavEnv.Values.Take(wavEnv.Length))}");
                    var modEnv = instrument.Envelopes[EnvelopeType.FdsModulation].BuildFdsModulationTable();
                    lines.Add($"FDSMOD{i,6} : {string.Join(" ", modEnv.Take(modEnv.Length))}");

                    for (int j = 0; j <= EnvelopeType.Pitch; j++)
                    {
                        var env = instrument.Envelopes[j];
                        if (!env.IsEmpty)
                            lines.Add($"FDSMACRO{i,4} {j,5} {env.Loop,4} {(env.Release >= 0 ? env.Release - 1 : -1),4}   0 : {string.Join(" ", env.Values.Take(env.Length))}");
                    }
                }
            }

            if (project.UsesSamples)
            {
                lines.Add($"INST2A03{project.Instruments.Count,4}{-1,6}{-1,4}{-1,4}{-1,4}{-1,4} \"DPCM\"");

                for (int i = 0; i < project.SamplesMapping.Length; i++)
                {
                    var mapping = project.SamplesMapping[i];

                    if (mapping != null && mapping.Sample != null)
                    {
                        int note     = i + Note.DPCMNoteMin;
                        var octave   = (note - 1) / 12 ;
                        var semitone = (note - 1) % 12;
                        var idx      = project.Samples.IndexOf(mapping.Sample);
                        var loop     = mapping.Loop ? 1 : 0;

                        lines.Add($"KEYDPCM{project.Instruments.Count,4}{octave,4}{semitone,4}{idx,6}{mapping.Pitch,4}{loop,4}{0,6}{-1,4}");
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
                song.DuplicateInstancesWithDifferentLengths();

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

                int maxPatternCount = -1;
                foreach (var channel in song.Channels)
                    maxPatternCount = Math.Max(maxPatternCount, channel.Patterns.Count);

                var patternRows = new Dictionary<Pattern, List<string>>();
                for (int c = 0; c < song.Channels.Length; c++)
                {
                    var channel = song.Channels[c];
                    var prevNoteValue = Note.NoteInvalid;
                    var prevInstrument = (Instrument)null;
                    var prevSlideEffect = Effect_None;
                    var prevArpeggio = (Arpeggio)null;
                    var famitrackerSpeed = song.FamitrackerSpeed;
                    
                    for (int p = 0; p < song.Length; p++)
                    {
                        var pattern = channel.PatternInstances[p];
                        var patternLen = song.GetPatternLength(p);

                        if (patternRows.ContainsKey(pattern))
                            continue;

                        var patternLines = new List<string>();

                        for (var it = pattern.GetNoteIterator(0, song.PatternLength); !it.Done; it.Next())
                        {
                            var time = it.CurrentTime;
                            var note = it.CurrentNote;

                            song.ApplySpeedEffectAt(p, time, ref famitrackerSpeed);

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
                                if (channel.IsVrc7FmChannel)
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
                                channel.ComputeSlideNoteParams(tempNote, p, time, famitrackerSpeed, noteTable, false, false, out _, out _, out var stepSizeFloat); 

                                if (channel.IsN163WaveChannel)
                                {
                                    stepSizeFloat /= 4.0f;
                                }

                                // Undo any kind of shifting we had done. This will kill the 1-bit of fraction we have on most channel.
                                var absNoteDelta  = Math.Abs(note.Value - note.SlideNoteTarget);

                                // See if we can use Qxy/Rxy (slide up/down y semitones, at speed x), this is preferable.
                                if (absNoteDelta < 16)
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
                                    if (prevNoteValue == note.Value && (prevInstrument == null || prevInstrument.Envelopes[EnvelopeType.Pitch].IsEmpty || !prevInstrument.Envelopes[EnvelopeType.Pitch].Relative))
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
                                        if (channel.IsFdsWaveChannel || channel.IsN163WaveChannel)
                                            stepSizeFloat = -stepSizeFloat;

                                        var absFloorStepSize = Math.Abs(Utils.SignedFloor(stepSizeFloat));

                                        if (prevSlideEffect == Effect_Portamento)
                                            effectString += $" 300";

                                        if (note.SlideNoteTarget > note.Value)
                                        {
                                            effectString += $" 1{Math.Min(0xff, absFloorStepSize):X2}";
                                            prevSlideEffect = Effect_PortaUp;
                                        }
                                        else if (note.SlideNoteTarget < note.Value)
                                        {
                                            effectString += $" 2{Math.Min(0xff, absFloorStepSize):X2}";
                                            prevSlideEffect = Effect_PortaDown;
                                        }
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

                            if (time == patternLen - 1)
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

                            if (note.IsMusical && note.Arpeggio != prevArpeggio)
                            {
                                var arpeggioString = " 0";

                                if (note.Arpeggio != null)
                                {
                                    arpeggioString += note.Arpeggio.Envelope.Length >= 2 ? $"{Utils.Clamp(note.Arpeggio.Envelope.Values[1], 0, 15):X1}" : "0";
                                    arpeggioString += note.Arpeggio.Envelope.Length >= 3 ? $"{Utils.Clamp(note.Arpeggio.Envelope.Values[2], 0, 15):X1}" : "0";
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

                        patternRows[pattern] = patternLines;
                    }
                }

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
            }

            lines.Add("# End of export");

            File.WriteAllLines(filename, lines);

            return true;
        }
    }
}
