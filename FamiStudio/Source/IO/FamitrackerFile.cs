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

        struct RowFxData
        {
            public char fx;
            public byte param;
        }

        public static Project Load(string filename)
        {
            var project = new Project();

            var envelopes = new Dictionary<int, Envelope>[Project.ExpansionCount, Envelope.Max];
            var duties = new Dictionary<int, int>();
            var instruments = new Dictionary<int, Instrument>();
            var dpcms = new Dictionary<int, DPCMSample>();
            var columns = new int[5] { 1, 1, 1, 1, 1 };
            var patternFxData = new Dictionary<Pattern, RowFxData[,]>();
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

                        //var fxData = new RowFxData[song.PatternLength];
                        if (!patternFxData.ContainsKey(pattern))
                            patternFxData[pattern] = new RowFxData[song.PatternLength, 4];

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

                            if (famitoneNote >= Note.MusicalNoteMin && famitoneNote <= Note.MusicalNoteMax)
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
                            var fx = noteData[3 + k];

                            if (fx == "...")
                                continue;

                            var param = Convert.ToByte(fx.Substring(1), 16);

                            var fxData = patternFxData[pattern];
                            fxData[rowIdx, k].fx = fx[0];
                            fxData[rowIdx, k].param = param;

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

                            pattern.Notes[rowIdx].EffectParam = param;
                        }
                    }
                }
            }

            foreach (var s in project.Songs)
            {
                CreateSlidesAndPortamento(s, patternFxData);

                s.RemoveEmptyPatterns();
                s.SetSensibleBarLength();

                foreach (var c in s.Channels)
                    c.ColorizePatterns();
            }

            return project;
        }

        private static int FindPrevNoteForPortamento(Channel channel, int patternIdx, int noteIdx, Dictionary<Pattern, RowFxData[,]> patternFxData)
        {
            for (int n = noteIdx - 1; n >= 0; n--)
            {
                var tmpNote = channel.PatternInstances[patternIdx].Notes[n];
                if (tmpNote.IsMusical || tmpNote.IsStop)
                    return tmpNote.Value;
            }

            for (var p = patternIdx - 1; p >= 0; p--)
            {
                var pattern = channel.PatternInstances[p];
                if (pattern != null)
                {
                    for (int n = channel.Song.PatternLength - 1; n >= 0; n--)
                    {
                        var tmpNote = pattern.Notes[n];
                        if (tmpNote.IsMusical || tmpNote.IsStop)
                            return tmpNote.Value;
                    }
                }
            }

            return Note.NoteInvalid;
        }

        private static bool FindNextNoteForSlide(Channel channel, int patternIdx, int noteIdx, out int nextPatternIdx, out int nextNoteIdx, Dictionary<Pattern, RowFxData[,]> patternFxData)
        {
            nextPatternIdx = -1;
            nextNoteIdx = -1;

            var pattern = channel.PatternInstances[patternIdx];
            var fxData = patternFxData[pattern];

            for (int n = noteIdx + 1; n < channel.Song.PatternLength; n++)
            {
                var fxChanged = false;
                for (int i = 0; i < fxData.GetLength(1); i++)
                {
                    var fx = fxData[n, i];
                    if (fx.fx == '1' || fx.fx == '2' || fx.fx == '3')
                    {
                        fxChanged = true;
                        break;
                    }
                }

                var tmpNote = pattern.Notes[n];
                if (tmpNote.IsMusical || tmpNote.IsStop || fxChanged)
                {
                    nextPatternIdx = patternIdx;
                    nextNoteIdx = n;
                    return true;
                }
            }

            for (int p = patternIdx + 1; p < channel.Song.Length; p++)
            {
                pattern = channel.PatternInstances[p];
                fxData = patternFxData[pattern];

                for (int n = 0; n < channel.Song.PatternLength; n++)
                {
                    var fxChanged = false;
                    for (int i = 0; i < fxData.GetLength(1); i++)
                    {
                        var fx = fxData[n, i];
                        if (fx.fx == '1' || fx.fx == '2' || fx.fx == '3')
                        {
                            fxChanged = true;
                            break;
                        }
                    }

                    var tmpNote = channel.PatternInstances[p].Notes[n];
                    if (tmpNote.IsMusical || tmpNote.IsStop || fxChanged)
                    {
                        nextPatternIdx = p;
                        nextNoteIdx = n;
                        return true;
                    }
                }
            }

            return false;
        }

        private static int FindBestMatchingNote(ushort[] noteTable, int pitch)
        {
            var bestIdx  = -1;
            var bestDiff = 99999;

            for (int i = 1; i < noteTable.Length; i++)
            {
                var diff = Math.Abs(pitch - noteTable[i]);
                if (diff < bestDiff)
                {
                    bestIdx = i;
                    bestDiff = diff;
                }
            }

            return bestIdx;
        }

        private static void CreateSlidesAndPortamento(Song s, Dictionary<Pattern, RowFxData[,]> patternFxData)
        {
            // Convert slide notes + portamento to our format.
            foreach (var c in s.Channels)
            {
                if (!c.SupportsSlideNotes)
                    continue;

                var portamentoSpeed = 0;
                var slideSpeed = 0;
                var lastNoteInstrument = (Instrument)null;
                var lastNoteValue = (byte)Note.NoteInvalid;

                for (int p = 0; p < s.Length; p++)
                {
                    var pattern = c.PatternInstances[p];
                    var fxData = patternFxData[pattern];

                    for (int n = 0; n < s.PatternLength; n++)
                    {
                        var note = pattern.Notes[n];

                        for (int i = 0; i < fxData.GetLength(1); i++)
                        {
                            var fx = fxData[n, i];

                            // When the effect it turned on, we need to add a note.
                            if ((fx.fx == '1' || fx.fx == '2' || fx.fx == '3') && fx.param != 0 && lastNoteValue >= Note.MusicalNoteMin && lastNoteValue <= Note.MusicalNoteMax && !note.IsValid)
                            {
                                pattern.Notes[n].Value = lastNoteValue;
                                pattern.Notes[n].Instrument = lastNoteInstrument;
                                note = pattern.Notes[n];
                            }

                            if (fx.fx == '1') slideSpeed = -fx.param;
                            if (fx.fx == '2') slideSpeed =  fx.param;
                            if (fx.fx == '3') portamentoSpeed = fx.param;
                        }

                        // Create a slide or portamento note.
                        if (!note.IsSlideOrPortamento)
                        {
                            if (note.IsMusical && slideSpeed != 0)
                            {
                                // Find the next note that would stop the slide or change the FX settings.
                                if (FindNextNoteForSlide(c, p, n, out var np, out var nn, patternFxData))
                                {
                                    // Compute the pitch delta and find the closest target note.
                                    var numFrames = ((np * s.PatternLength + nn) - (p * s.PatternLength + n)) * s.Speed;

                                    // TODO: PAL.
                                    var noteTable = NesApu.GetNoteTableForChannelType(c.Type, false);
                                    var pitchLimit = NesApu.GetPitchLimitForChannelType(c.Type);
                                    var newNotePitch = Utils.Clamp(noteTable[note.Value] + numFrames * slideSpeed, 0, pitchLimit);
                                    var newNote = FindBestMatchingNote(noteTable, newNotePitch);

                                    pattern.Notes[n].SlideNoteTarget = (byte)newNote;

                                    // If the FX was turned off, we need to add an extra note.
                                    if (!c.PatternInstances[np].Notes[nn].IsMusical &&
                                        !c.PatternInstances[np].Notes[nn].IsStop)
                                    {
                                        c.PatternInstances[np].Notes[nn].Instrument = note.Instrument;
                                        c.PatternInstances[np].Notes[nn].Value = (byte)newNote;
                                    }
                                }
                            }
                            else if (note.IsMusical && portamentoSpeed != 0)
                            {
                                // Find the previous note that we are sliding from.
                                var prevNote = FindPrevNoteForPortamento(c, p, n, patternFxData);

                                if (prevNote >= Note.MusicalNoteMin &&
                                    prevNote <= Note.MusicalNoteMax &&
                                    prevNote != note.Value)
                                {
                                    // TODO: PAL.
                                    var noteTable = NesApu.GetNoteTableForChannelType(c.Type, false);

                                    // Compute the pitch difference and set the custom portamento length.
                                    var pitchDelta = Math.Abs(noteTable[prevNote] - noteTable[note.Value]);
                                    var numFrames = pitchDelta / portamentoSpeed;
                                    var numNotes = Utils.Clamp(numFrames / c.Song.Speed, 0, 127);

                                    pattern.Notes[n].IsPortamento = true;
                                    pattern.Notes[n].PortamentoLength = (byte)numNotes;
                                }
                            }
                        }

                        if (note.IsMusical || note.IsStop)
                        {
                            lastNoteValue = note.Value;
                            lastNoteInstrument = note.Instrument;
                        }
                    }
                }
            }
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
                if (!env.IsEmpty && !env.Relative)
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

        private static Envelope[,][] MergeIdenticalEnvelopes(Project project)
        {
            var uniqueEnvelopes = new Dictionary<uint, Envelope>[2, Envelope.Max];

            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < Envelope.Max; j++)
                {
                    uniqueEnvelopes[i, j] = new Dictionary<uint, Envelope>();
                }
            }

            foreach (var instrument in project.Instruments)
            {
                for (int i = 0; i < Envelope.Max; i++)
                {
                    var env = instrument.Envelopes[i];
                    uint crc = env.CRC;

                    if (uniqueEnvelopes[instrument.ExpansionType != Project.ExpansionNone ? 1 : 0, i].TryGetValue(crc, out var existingEnv))
                    {
                        instrument.Envelopes[i] = existingEnv;
                    }
                    else
                    {
                        uniqueEnvelopes[instrument.ExpansionType != Project.ExpansionNone ? 1 : 0, i][crc] = env;
                    }
                }
            }

            var envelopeArray = new Envelope[2, Envelope.Max][];
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < Envelope.Max; j++)
                {
                    envelopeArray[i, j] = uniqueEnvelopes[i, j].Values.ToArray();
                }
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

        private static void FindSlideNoteTransitions(Song song, Dictionary<Pattern, List<int>> portamentoTransitions, Dictionary<Pattern, List<int>> slideTransitions)
        {
            foreach (var channel in song.Channels)
            {
                bool lastNoteWasSlide      = false;
                bool lastNoteWasPortamento = false;

                for (int p = 0; p < song.Length; p++)
                {
                    var pattern = channel.PatternInstances[p];

                    if (pattern == null)
                        continue;

                    if (!slideTransitions.ContainsKey(pattern))
                        slideTransitions[pattern] = new List<int>();
                    if (!portamentoTransitions.ContainsKey(pattern))
                        portamentoTransitions[pattern] = new List<int>();

                    for (int n = 0; n < song.PatternLength; n++)
                    {
                        var note = pattern.Notes[n];

                        if (lastNoteWasSlide && (note.IsMusical || note.IsStop) && !note.IsSlideNote)
                            slideTransitions[pattern].Add(n);
                        if (lastNoteWasPortamento && (note.IsMusical || note.IsStop) && !note.IsPortamento)
                            portamentoTransitions[pattern].Add(n);

                        if (note.IsMusical || note.IsStop)
                        {
                            lastNoteWasSlide      = note.IsSlideNote;
                            lastNoteWasPortamento = note.IsPortamento;
                        }
                    }
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
            lines.Add("EXPANSION       " + project.ExpansionAudio);
            lines.Add("VIBRATO         1");
            lines.Add("SPLIT           21");
            lines.Add("");

            lines.Add("# Macros");
            for (int i = 0; i < Envelope.Max; i++)
            {
                var envArray = envelopes[Project.ExpansionNone, i];
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

            if (project.ExpansionAudio == Project.ExpansionVRC6)
            {
                for (int i = 0; i < Envelope.Max; i++)
                {
                    var envArray = envelopes[Project.ExpansionVRC6, i];
                    for (int j = 0; j < envArray.Length; j++)
                    {
                        var env = envArray[j];
                        lines.Add($"MACROVRC6{i,8} {j,4} {env.Loop,4} {env.Release,4}   0 : {string.Join(" ", env.Values.Take(env.Length))}");
                    }
                }

                lines.Add($"MACROVRC6{4,8} {0,4} {-1}   -1    0 : 0");
                lines.Add($"MACROVRC6{4,8} {1,4} {-1}   -1    0 : 1");
                lines.Add($"MACROVRC6{4,8} {2,4} {-1}   -1    0 : 2");
                lines.Add($"MACROVRC6{4,8} {3,4} {-1}   -1    0 : 3");
                lines.Add($"MACROVRC6{4,8} {4,4} {-1}   -1    0 : 4");
                lines.Add($"MACROVRC6{4,8} {5,4} {-1}   -1    0 : 5");
                lines.Add($"MACROVRC6{4,8} {6,4} {-1}   -1    0 : 6");
                lines.Add($"MACROVRC6{4,8} {7,4} {-1}   -1    0 : 7");
            }

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

                var expIdx = instrument.ExpansionType != Project.ExpansionNone ? 1 : 0;
                int volEnvIdx = instrument.Envelopes[Envelope.Volume].Length   > 0 ? Array.IndexOf(envelopes[expIdx, Envelope.Volume],   instrument.Envelopes[Envelope.Volume])   : -1;
                int arpEnvIdx = instrument.Envelopes[Envelope.Arpeggio].Length > 0 ? Array.IndexOf(envelopes[expIdx, Envelope.Arpeggio], instrument.Envelopes[Envelope.Arpeggio]) : -1;
                int pitEnvIdx = instrument.Envelopes[Envelope.Pitch].Length    > 0 ? Array.IndexOf(envelopes[expIdx, Envelope.Pitch],    instrument.Envelopes[Envelope.Pitch])    : -1;

                if (instrument.ExpansionType == Project.ExpansionNone)
                {
                    lines.Add($"INST2A03{i,4}{volEnvIdx,6}{arpEnvIdx,4}{pitEnvIdx,4}{-1,4}{instrument.DutyCycle,4} \"{instrument.Name}\"");
                }
                else if (instrument.ExpansionType == Project.ExpansionVRC6)
                {
                    lines.Add($"INSTVRC6{i,4}{volEnvIdx,6}{arpEnvIdx,4}{pitEnvIdx,4}{-1,4}{instrument.DutyCycle,4} \"{instrument.Name}\"");
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

                song.CleanupUnusedPatterns();
                CreateMissingPatterns(song);

                // Find all the places where we need to turn of 1xx/2xx/3xx after we are done.
                var portamentoTransitions = new Dictionary<Pattern, List<int>>();
                var slideTransitions      = new Dictionary<Pattern, List<int>>();
                FindSlideNoteTransitions(song, portamentoTransitions, slideTransitions);

                lines.Add($"TRACK{song.PatternLength,4}{song.Speed,4}{song.Tempo,4} \"{song.Name}\"");
                lines.Add($"COLUMNS : {string.Join(" ", Enumerable.Repeat(2, song.Channels.Length))}");
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
                                line += " : ... .. . ... ...";
                            }
                            else
                            {
                                var pattern = channel.Patterns[j];
                                var note = pattern.Notes[k];
                                var noteString = GetFamiTrackerNoteName(l, note);
                                var volumeString = note.HasVolume ? note.Volume.ToString("X") : ".";
                                var instrumentString = note.IsValid && !note.IsStop ? (note.Instrument == null ? project.Instruments.Count : project.Instruments.IndexOf(note.Instrument)).ToString("X2") : "..";
                                var effectString1 = "...";
                                var effectString2 = "...";

                                if (note.IsSlideOrPortamento && note.IsMusical)
                                {
                                    // TODO: PAL.
                                    var noteTable = NesApu.GetNoteTableForChannelType(channel.Type, false);

                                    // HACK: We only consider the first instance of the pattern. Will definately cause
                                    // problems if we have slides between patterns that have different pitches.
                                    var instIdx = Array.IndexOf(channel.PatternInstances, pattern);
                                    channel.ComputeSlideNoteParams(instIdx, k, noteTable, out _, out int stepSize, out _, out _, out _);

                                    // We have one bit of fraction. FramiTracker does not.
                                    if (Math.Abs(stepSize) > 1)
                                        stepSize /= 2;

                                    if (note.IsSlideNote)
                                    {
                                        if (stepSize > 0)
                                            effectString1 = $"2{ stepSize:X2}";
                                        else if (stepSize < 0)
                                            effectString1 = $"1{-stepSize:X2}";
                                    }
                                    else
                                    {
                                        effectString1 = $"3{Math.Abs(stepSize):X2}";
                                    }
                                }

                                // Turn off 1xx/2xx/3xx when not needed.
                                if (portamentoTransitions[pattern].IndexOf(k) >= 0)
                                    effectString1 = "300";
                                if (slideTransitions[pattern].IndexOf(k) >= 0)
                                    effectString1 = "200";

                                switch (note.Effect)
                                {
                                    case Note.EffectJump  : effectString2 = $"B{note.EffectParam:X2}"; break;
                                    case Note.EffectSkip  : effectString2 = $"D{note.EffectParam:X2}"; break;
                                    case Note.EffectSpeed : effectString2 = $"F{note.EffectParam:X2}"; break;
                                }

                                line += $" : {noteString} {instrumentString} {volumeString} {effectString1} {effectString2}";
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
