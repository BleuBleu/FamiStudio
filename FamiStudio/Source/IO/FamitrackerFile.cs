using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FamiStudio
{
    public class FamitrackerFile
    {
        private static string[] SplitStringKeepQuotes(string str)
        {
            return str.Split('"').Select((element, index) => index % 2 == 0
                                                        ? element.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                                        : new string[] { element })
                                  .SelectMany(element => element).ToArray();
        }

        public static Project Load(string filename)
        {
            var project = new Project();

            var envelopes = new Dictionary<int, Envelope>[Project.ExpansionCount, Envelope.Max];
            var duties = new Dictionary<int, int>();
            var instruments = new Dictionary<int, Instrument>();
            var dpcms = new Dictionary<int, DPCMSample>();
            var columns = new int[5] { 1, 1, 1, 1, 1 };
            var noteLookup = new Dictionary<string, int>
            {
                ["A-"] = 9,
                ["A#"] = 10,
                ["B-"] = 11,
                ["C-"] = 0,
                ["C#"] = 1,
                ["D-"] = 2,
                ["D#"] = 3,
                ["E-"] = 4,
                ["F-"] = 5,
                ["F#"] = 6,
                ["G-"] = 7,
                ["G#"] = 8
            };

            for (int i = 0; i < envelopes.GetLength(0); i++)
                for (int j = 0; j < envelopes.GetLength(1); j++)
                    envelopes[i, j] = new Dictionary<int, Envelope>();

            DPCMSample currentDpcm = null;
            int dpcmWriteIdx = 0;
            Song song = null;
            string patternName = "";

            var lines = File.ReadAllLines(filename);

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();

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
                    if (exp == 1)
                        project.SetExpansionAudio(Project.ExpansionVRC6);
                }
                else if (line.StartsWith("MACRO"))
                {
                    var expansion = line.Substring(5, 4) == "VRC6" ? Project.ExpansionVRC6 : Project.ExpansionNone;
                    var halves = line.Substring(line.IndexOf(' ')).Split(':');
                    var param = halves[0].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    var curve = halves[1].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    var type = int.Parse(param[0]);
                    var idx  = int.Parse(param[1]);
                    var loop = int.Parse(param[2]);
                    var rel  = int.Parse(param[3]);

                    if (type < 3)
                    {
                        var env = new Envelope();
                        env.Length = curve.Length;

                        // FamiTracker allows envelope with release with no loop. We dont allow that.
                        if (type == Envelope.Volume && loop == -1 && rel != -1)
                        {
                            loop = rel;
                            rel++;
                        }

                        env.Loop = loop;
                        env.Release = type == Envelope.Volume ? rel : -1;

                        for (int j = 0; j < curve.Length; j++)
                            env.Values[j] = sbyte.Parse(curve[j]);
                        if (type == 2)
                        {
                            env.Relative = true;
                            for (int j = 0; j < env.Length; j++)
                                env.Values[j] *= -1;
                        }
                        envelopes[expansion, type][idx] = env;
                    }
                    else if (type == 4)
                    {
                        duties[idx] = int.Parse(curve[0]);
                    }
                }
                else if (line.StartsWith("DPCMDEF"))
                {
                    var param = SplitStringKeepQuotes(line.Substring(7));
                    var name = param[2];
                    var j = 2;

                    while (!project.IsDPCMSampleNameUnique(name))
                        name = param[2] + "-" + j++;
                    currentDpcm = project.CreateDPCMSample(name, new byte[int.Parse(param[1])]);
                    dpcms[int.Parse(param[0])] = currentDpcm;
                    dpcmWriteIdx = 0;
                }
                else if (line.StartsWith("DPCM"))
                {
                    var param = line.Substring(6).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var s in param)
                    {
                        currentDpcm.Data[dpcmWriteIdx++] = Convert.ToByte(s, 16);
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

                            project.MapDPCMSample(note, dpcms[dpcm], pitch, loop != 0);
                        }
                    }
                }
                else if (line.StartsWith("INST2A03") || line.StartsWith("INSTVRC6"))
                {
                    var expansion = line.Substring(4, 4) == "VRC6" ? Project.ExpansionVRC6 : Project.ExpansionNone;
                    var param = SplitStringKeepQuotes(line.Substring(line.IndexOf(' ')));

                    int idx = int.Parse(param[0]);
                    int vol = int.Parse(param[1]);
                    int arp = int.Parse(param[2]);
                    int pit = int.Parse(param[3]);
                    int dut = int.Parse(param[5]);
                    var name = param[6];
                    var j = 2;

                    if (!project.IsInstrumentNameUnique(name))
                        name = param[6] + "-" + j++;

                    var expansionType = line.StartsWith("INSTVRC6") ? Project.ExpansionVRC6 : Project.ExpansionNone;
                    var instrument = project.CreateInstrument(expansionType, name);

                    if (vol >= 0) instrument.Envelopes[0] = envelopes[expansion, 0][vol].Clone();
                    if (arp >= 0) instrument.Envelopes[1] = envelopes[expansion, 1][arp].Clone();
                    if (pit >= 0) instrument.Envelopes[2] = envelopes[expansion, 2][pit].Clone();
                    if (dut >= 0) instrument.DutyCycle = duties[dut];

                    instruments[idx] = instrument;
                }
                else if (line.StartsWith("TRACK"))
                {
                    var param = SplitStringKeepQuotes(line.Substring(5));

                    song = project.CreateSong(param[3]);
                    song.Length = 0;
                    song.PatternLength = int.Parse(param[0]);
                    song.Speed = int.Parse(param[1]);
                    song.Tempo = int.Parse(param[2]);
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

                    song.Length++;
                }
                else if (line.StartsWith("PATTERN"))
                {
                    patternName = line.Substring(8);
                }
                else if (line.StartsWith("ROW"))
                {
                    var channels = line.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                    var rowIdx = Convert.ToInt32(channels[0].Substring(4, 2), 16);

                    for (int j = 1; j <= song.Channels.Length; j++)
                    {
                        var noteData = channels[j].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        var pattern = song.Channels[j - 1].GetPattern(patternName);

                        if (pattern == null)
                            continue;

                        // Note
                        if (noteData[0] == "---")
                        {
                            pattern.Notes[rowIdx].Value = Note.NoteStop;
                        }
                        else if (noteData[0] == "===")
                        {
                            pattern.Notes[rowIdx].Value = Note.NoteRelease;
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
                                int semitone = noteLookup[noteData[0].Substring(0, 2)];
                                int octave = noteData[0][2] - '0';
                                famitoneNote = octave * 12 + semitone + 1;
                            }

                            if (famitoneNote >= Note.NoteMin && famitoneNote <= Note.NoteMax)
                            {
                                pattern.Notes[rowIdx].Value = (byte)famitoneNote;
                                pattern.Notes[rowIdx].Instrument = j == 5 ? null : instruments[Convert.ToInt32(noteData[1], 16)];
                            }
                            else
                            {
                                // Note outside of range.
                            }
                        }

                        // Volume
                        if (noteData[2] != ".")
                        {
                            pattern.Notes[rowIdx].Volume = Convert.ToByte(noteData[2], 16);
                        }

                        // Read FX.
                        for (int k = 0; k < columns[j - 1]; k++)
                        {
                            string fx = noteData[3 + k];

                            switch (fx[0])
                            {
                                case 'B': // Jump
                                    pattern.Notes[rowIdx].Effect = Note.EffectJump;
                                    break;
                                case 'D': // Skip
                                    pattern.Notes[rowIdx].Effect = Note.EffectSkip;
                                    break;
                                case 'F': // Tempo
                                    pattern.Notes[rowIdx].Effect = Note.EffectSpeed;
                                    break;
                                default:
                                    continue;
                            }

                            pattern.Notes[rowIdx].EffectParam = Convert.ToByte(fx.Substring(1), 16);
                        }
                    }
                }
            }

            foreach (var s in project.Songs)
            {
                s.RemoveEmptyPatterns();
                s.SetSensibleBarLength();

                foreach (var c in s.Channels)
                {
                    c.ColorizePatterns();
                }
            }

            return project;
        }

        private static string[] FamiTrackerNoteNames =
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

        private static void ConvertPitchEnvelopes(Project project)
        {
            foreach (var instrument in project.Instruments)
            {
                var env = instrument.Envelopes[Envelope.Pitch];
                if (!env.IsEmpty)
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

        private static Envelope[][] MergeIdenticalEnvelopes(Project project)
        {
            var uniqueEnvelopes = new Dictionary<uint, Envelope>[Envelope.Max];

            for (int i = 0; i < Envelope.Max; i++)
            {
                uniqueEnvelopes[i] = new Dictionary<uint, Envelope>();
            }

            foreach (var instrument in project.Instruments)
            {
                for (int i = 0; i < Envelope.Max; i++)
                {
                    var env = instrument.Envelopes[i];
                    uint crc = env.CRC;

                    if (uniqueEnvelopes[i].TryGetValue(crc, out var existingEnv))
                    {
                        instrument.Envelopes[i] = existingEnv;
                    }
                    else
                    {
                        uniqueEnvelopes[i][crc] = env;
                    }
                }
            }

            var envelopeArray = new Envelope[Envelope.Max][];
            for (int i = 0; i < Envelope.Max; i++)
            {
                envelopeArray[i] = uniqueEnvelopes[i].Values.ToArray();
            }

            return envelopeArray;
        }

        private static void CreateMissingPatterns(Song song)
        {
            foreach (var channel in song.Channels)
            {
                int emptyPatternIdx = -1;
                
                for (int i = 0; i < channel.Patterns.Count; i++)
                {
                    if (!channel.Patterns[i].HasAnyNotes && !channel.Patterns[i].HasAnyEffect)
                    {
                        emptyPatternIdx = i;
                        break;
                    }
                }

                for (int i = 0; i < channel.PatternInstances.Length; i++)
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

        private static string GetFamiTrackerNoteName(int channel, Note note)
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
                if (channel == Channel.Noise)
                {
                    return (note.Value & 0xf).ToString("X") + "-#";
                }
                else
                {
                    int octave = (note.Value - 1) / 12;
                    int semitone = (note.Value - 1) % 12;

                    return FamiTrackerNoteNames[semitone] + octave.ToString();
                }
            }
        }

        public static bool Save(Project originalProject, string filename, int[] songIds)
        {
            var project = originalProject.Clone();
            project.RemoveAllSongsBut(songIds);

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

            lines.Add("# Global settings");
            lines.Add("MACHINE         0");
            lines.Add("FRAMERATE       0");
            lines.Add("EXPANSION       0");
            lines.Add("VIBRATO         1");
            lines.Add("SPLIT           21");
            lines.Add("");

            lines.Add("# Macros");
            for (int i = 0; i < Envelope.Max; i++)
            {
                var envArray = envelopes[i];
                for (int j = 0; j < envArray.Length; j++)
                {
                    var env = envArray[j];
                    lines.Add($"MACRO{i,8} {j,4} {env.Loop,4} {env.Release,4}   0 : {string.Join(" ", env.Values.Take(env.Length))}");
                }
            }
            lines.Add($"MACRO{4,8} {0,4} {-1}   -1    0 : 0");
            lines.Add($"MACRO{4,8} {1,4} {-1}   -1    0 : 1");
            lines.Add($"MACRO{4,8} {2,4} {-1}   -1    0 : 2");
            lines.Add($"MACRO{4,8} {3,4} {-1}   -1    0 : 3");
            lines.Add("");

            if (project.UsesSamples)
            {
                lines.Add("# DPCM samples");
                for (int i = 0; i < project.Samples.Count; i++)
                {
                    var sample = project.Samples[i];
                    lines.Add($"DPCMDEF{i,4}{sample.Data.Length,6} \"{sample.Name}\"");
                    lines.Add($"DPCM : {String.Join(" ", sample.Data.Select(x => $"{x:X2}"))}");
                }
                lines.Add("");
            }

            lines.Add("# Instruments");
            for (int i = 0; i < project.Instruments.Count; i++)
            {
                var instrument = project.Instruments[i];

                int volEnvIdx = instrument.Envelopes[Envelope.Volume].Length   > 0 ? Array.IndexOf(envelopes[Envelope.Volume],   instrument.Envelopes[Envelope.Volume])   : -1;
                int arpEnvIdx = instrument.Envelopes[Envelope.Arpeggio].Length > 0 ? Array.IndexOf(envelopes[Envelope.Arpeggio], instrument.Envelopes[Envelope.Arpeggio]) : -1;
                int pitEnvIdx = instrument.Envelopes[Envelope.Pitch].Length    > 0 ? Array.IndexOf(envelopes[Envelope.Pitch],    instrument.Envelopes[Envelope.Pitch])    : -1;

                lines.Add($"INST2A03{i,4}{volEnvIdx,6}{arpEnvIdx,4}{pitEnvIdx,4}{-1,4}{instrument.DutyCycle,4} \"{instrument.Name}\"");
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

                CreateMissingPatterns(song);

                lines.Add($"TRACK{song.PatternLength,4}{song.Speed,4}{song.Tempo,4} \"{song.Name}\"");
                lines.Add($"COLUMNS : 1 1 1 1 1");
                lines.Add("");

                for (int j = 0; j < song.Length; j++)
                {
                    var line = $"ORDER {j:X2} :";

                    for (int k = 0; k < song.Channels.Length; k++)
                    {
                        line += $" {song.Channels[k].Patterns.IndexOf(song.Channels[k].PatternInstances[j]):X2}";
                    }
                    lines.Add(line);
                }
                lines.Add("");

                int maxPatternCount = -1;
                foreach (var channel in song.Channels)
                {
                    maxPatternCount = Math.Max(maxPatternCount, channel.Patterns.Count);
                }

                for (int j = 0; j < maxPatternCount; j++)
                {
                    lines.Add($"PATTERN {j:X2}");

                    for (int k = 0; k < song.PatternLength; k++)
                    {
                        var line = $"ROW {k:X2}";
                        for (int l = 0; l < song.Channels.Length; l++)
                        {
                            var channel = song.Channels[l];

                            if (j >= channel.Patterns.Count)
                            {
                                line += " : ... .. . ...";
                            }
                            else
                            {
                                var pattern = channel.Patterns[j];
                                var note = pattern.Notes[k];
                                var noteString = GetFamiTrackerNoteName(l, note);
                                var volumeString = note.HasVolume ? note.Volume.ToString("X") : ".";
                                var instrumentString = note.IsValid && !note.IsStop ? (note.Instrument == null ? project.Instruments.Count : project.Instruments.IndexOf(note.Instrument)).ToString("X2") : "..";
                                var effectString = "...";

                                switch (note.Effect)
                                {
                                    case Note.EffectJump  : effectString = $"B{note.EffectParam:X2}"; break;
                                    case Note.EffectSkip  : effectString = $"D{note.EffectParam:X2}"; break;
                                    case Note.EffectSpeed : effectString = $"F{note.EffectParam:X2}"; break;
                                }

                                line += $" : {noteString} {instrumentString} {volumeString} {effectString}";
                            }
                        }
                        lines.Add(line);
                    }

                    lines.Add("");
                }
            }

            File.WriteAllLines(filename, lines);

            return true;
        }
    }
}
