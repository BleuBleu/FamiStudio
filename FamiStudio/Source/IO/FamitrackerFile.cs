using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FamiStudio
{
    public class FamitrackerFile
    {
        private static readonly int[] VibratoSpeedImportLookup = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 10, 11, 11, 11, 12 };
        private static readonly int[] VibratoSpeedExportLookup = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 12, 15 };

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
            var duties = new Dictionary<int, int>[Project.ExpansionCount];
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

            for (int i = 0; i < duties.Length; i++)
                duties[i] = new Dictionary<int, int>();

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
                        project.SetExpansionAudio(Project.ExpansionVrc6);
                }
                else if (line.StartsWith("MACRO"))
                {
                    var expansion = line.Substring(5, 4) == "VRC6" ? Project.ExpansionVrc6 : Project.ExpansionNone;
                    var halves = line.Substring(line.IndexOf(' ')).Split(':');
                    var param = halves[0].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    var curve = halves[1].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    var type = int.Parse(param[0]);
                    var idx  = int.Parse(param[1]);
                    var loop = int.Parse(param[2]);
                    var rel  = int.Parse(param[3]);

                    if (type < 3)
                    {
                        var env = new Envelope(type);
                        env.Length = curve.Length;

                        // FamiTracker allows envelope with release with no loop. We dont allow that.
                        if (type == Envelope.Volume && loop == -1 && rel != -1)
                        {
                            loop = rel;
                        }

                        env.Loop = loop;
                        env.Release = type == Envelope.Volume && rel != -1 ? rel + 1 : -1;

                        for (int j = 0; j < curve.Length; j++)
                            env.Values[j] = sbyte.Parse(curve[j]);
                        if (type == 2)
                            env.Relative = true;
                        envelopes[expansion, type][idx] = env;
                    }
                    else if (type == 4)
                    {
                        duties[expansion][idx] = int.Parse(curve[0]);
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
                    var expansion = line.Substring(4, 4) == "VRC6" ? Project.ExpansionVrc6 : Project.ExpansionNone;
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

                    var expansionType = line.StartsWith("INSTVRC6") ? Project.ExpansionVrc6 : Project.ExpansionNone;
                    var instrument = project.CreateInstrument(expansionType, name);

                    if (vol >= 0) instrument.Envelopes[0] = envelopes[expansion, 0][vol].ShallowClone();
                    if (arp >= 0) instrument.Envelopes[1] = envelopes[expansion, 1][arp].ShallowClone();
                    if (pit >= 0) instrument.Envelopes[2] = envelopes[expansion, 2][pit].ShallowClone();
                    if (dut >= 0) instrument.DutyCycle = duties[expansionType][dut];

                    instruments[idx] = instrument;
                }
                else if (line.StartsWith("TRACK"))
                {
                    var param = SplitStringKeepQuotes(line.Substring(5));

                    song = project.CreateSong(param[3]);
                    song.SetLength(0);
                    song.SetPatternLength(int.Parse(param[0]));
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

                        song.Channels[j].PatternInstances[orderIdx].Pattern = pattern;
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
                    var rowIdx = Convert.ToInt32(channels[0].Substring(4, 2), 16);

                    for (int j = 1; j <= song.Channels.Length; j++)
                    {
                        var noteData = channels[j].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        var pattern = song.Channels[j - 1].GetPattern(patternName);

                        if (pattern == null)
                            continue;

                        //var fxData = new RowFxData[song.PatternLength];
                        if (!patternFxData.ContainsKey(pattern))
                            patternFxData[pattern] = new RowFxData[song.DefaultPatternLength, 4];

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
                                    pattern.Notes[rowIdx].Jump = param;
                                    break;
                                case 'D': // Skip
                                    pattern.Notes[rowIdx].Skip = param;
                                    break;
                                case 'F': // Tempo
                                    if (param <= 0x1f) // We only support speed change for now.
                                        pattern.Notes[rowIdx].Speed = param;
                                    break;
                                case '4': // Vibrato
                                    pattern.Notes[rowIdx].VibratoDepth = (byte)(param & 0x0f);
                                    pattern.Notes[rowIdx].VibratoSpeed = (byte)VibratoSpeedImportLookup[param >> 4];

                                    if (pattern.Notes[rowIdx].VibratoDepth == 0 ||
                                        pattern.Notes[rowIdx].VibratoSpeed == 0)
                                    {
                                        pattern.Notes[rowIdx].Vibrato = 0;
                                    }
                                    break;
                            }
                        }
                    }
                }
            }

            foreach (var s in project.Songs)
            {
                CreateSlideNotes(s, patternFxData);

                s.RemoveEmptyPatterns();
                s.SetSensibleBarLength();

                foreach (var c in s.Channels)
                    c.ColorizePatterns();
            }

            project.UpdateAllLastValidNotesAndVolume();

            return project;
        }

        private static int FindPrevNoteForPortamento(Channel channel, int patternIdx, int noteIdx, Dictionary<Pattern, RowFxData[,]> patternFxData)
        {
            for (int n = noteIdx - 1; n >= 0; n--)
            {
                var tmpNote = channel.PatternInstances[patternIdx].Pattern.Notes[n];
                if (tmpNote.IsMusical || tmpNote.IsStop)
                    return tmpNote.Value;
            }

            for (var p = patternIdx - 1; p >= 0; p--)
            {
                var pattern = channel.PatternInstances[p].Pattern;
                if (pattern != null)
                {
                    for (int n = channel.Song.GetPatternInstanceLength(p) - 1; n >= 0; n--)
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

            var pattern = channel.PatternInstances[patternIdx].Pattern;
            var patInstLen = channel.Song.GetPatternInstanceLength(patternIdx);
            var fxData = patternFxData[pattern];

            for (int n = noteIdx + 1; n < patInstLen; n++)
            {
                var fxChanged = false;
                for (int i = 0; i < fxData.GetLength(1); i++)
                {
                    var fx = fxData[n, i];
                    if (fx.fx == '1' || fx.fx == '2' || fx.fx == '3' || fx.fx == 'Q' || fx.fx == 'R')
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
                pattern = channel.PatternInstances[p].Pattern;
                patInstLen = channel.Song.GetPatternInstanceLength(p);
                fxData = patternFxData[pattern];

                for (int n = 0; n < patInstLen; n++)
                {
                    var fxChanged = false;
                    for (int i = 0; i < fxData.GetLength(1); i++)
                    {
                        var fx = fxData[n, i];
                        if (fx.fx == '1' || fx.fx == '2' || fx.fx == '3' || fx.fx == 'Q' || fx.fx == 'R')
                        {
                            fxChanged = true;
                            break;
                        }
                    }

                    var tmpNote = channel.PatternInstances[p].Pattern.Notes[n];
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

        private static int FindBestMatchingNote(ushort[] noteTable, int pitch, int sign)
        {
            var bestIdx  = -1;
            var bestDiff = 99999;

            for (int i = 1; i < noteTable.Length; i++)
            {
                var diff = (pitch - noteTable[i]) * sign;
                if (diff >= 0 && diff < bestDiff)
                {
                    bestIdx = i;
                    bestDiff = diff;
                }
            }

            return bestIdx;
        }

        private static void CreateSlideNotes(Song s, Dictionary<Pattern, RowFxData[,]> patternFxData)
        {
            // Convert slide notes + portamento to our format.
            foreach (var c in s.Channels)
            {
                if (!c.SupportsSlideNotes)
                    continue;

                var lastNoteInstrument = (Instrument)null;
                var lastNoteValue = (byte)Note.NoteInvalid;
                var portamentoSpeed = 0;

                for (int p = 0; p < s.Length; p++)
                {
                    var pattern = c.PatternInstances[p].Pattern;
                    var fxData = patternFxData[pattern];

                    for (int n = 0; n < s.GetPatternInstanceLength(p); n++)
                    {
                        var note = pattern.Notes[n];
                        var slideSpeed = 0;
                        var slideTarget = 0;

                        for (int i = 0; i < fxData.GetLength(1); i++)
                        {
                            var fx = fxData[n, i];

                            if (fx.param != 0)
                            {
                                // When the effect it turned on, we need to add a note.
                                if ((fx.fx == '1' || fx.fx == '2' || fx.fx == 'Q' || fx.fx == 'R') && lastNoteValue >= Note.MusicalNoteMin && lastNoteValue <= Note.MusicalNoteMax && !note.IsValid)
                                {
                                    pattern.Notes[n].Value = lastNoteValue;
                                    pattern.Notes[n].Instrument = lastNoteInstrument;
                                    pattern.Notes[n].HasAttack = false;
                                    note = pattern.Notes[n];
                                }

                                if (fx.fx == '1') slideSpeed = -fx.param;
                                if (fx.fx == '2') slideSpeed =  fx.param;
                                if (fx.fx == '3')
                                {
                                    portamentoSpeed = fx.param;
                                }
                                if (fx.fx == 'Q')
                                {
                                    slideTarget = note.Value + (fx.param & 0xf);
                                    slideSpeed = -((fx.param >> 4) * 2 + 1);
                                }
                                if (fx.fx == 'R')
                                {
                                    slideTarget = note.Value - (fx.param & 0xf);
                                    slideSpeed = ((fx.param >> 4) * 2 + 1);
                                }
                            }
                            else if (fx.fx == '3')
                            {
                                portamentoSpeed = 0;
                            }
                        }

                        // Create a slide note.
                        if (!note.IsSlideNote)
                        {
                            if (note.IsMusical)
                            {
                                var noteTable = NesApu.GetNoteTableForChannelType(c.Type, false);
                                var pitchLimit = NesApu.GetPitchLimitForChannelType(c.Type);

                                // If we have a new note with auto-portamento enabled, we need to
                                // swap the notes since our slide notes work backward compared to 
                                // FamiTracker.
                                if (portamentoSpeed != 0)
                                {
                                    if (lastNoteValue >= Note.MusicalNoteMin && lastNoteValue <= Note.MusicalNoteMax)
                                    {
                                        pattern.Notes[n].SlideNoteTarget = pattern.Notes[n].Value;
                                        pattern.Notes[n].Value = lastNoteValue;
                                    }
                                }
                                else if (slideTarget != 0)
                                {
                                    var numFrames = Math.Abs((noteTable[note.Value] - noteTable[slideTarget]) / (slideSpeed * s.Speed));
                                    pattern.Notes[n].SlideNoteTarget = (byte)slideTarget;

                                    var nn = n + numFrames;
                                    var np = p;
                                    while (nn >= s.GetPatternInstanceLength(np))
                                    {
                                        nn -= s.GetPatternInstanceLength(np);
                                        np++;
                                    }
                                    if (np >= s.Length)
                                    {
                                        np = s.Length;
                                        nn = 0;
                                    }

                                    // Still to see if there is a note between the current one and the 
                                    // next note, this could append if you add a note before the slide 
                                    // is supposed to finish.
                                    if (FindNextNoteForSlide(c, p, n, out var np2, out var nn2, patternFxData))
                                    {
                                        if (np2 < np)
                                        {
                                            np = np2;
                                            nn = nn2;
                                        }
                                        else if (np2 == np)
                                        {
                                            nn = Math.Min(nn, nn2);
                                        }
                                    }

                                    // Add an extra note with no attack to stop the slide.
                                    var nextPattern = c.PatternInstances[np].Pattern;
                                    if (!nextPattern.Notes[nn].IsValid)
                                    {
                                        nextPattern.Notes[nn].Instrument = note.Instrument;
                                        nextPattern.Notes[nn].Value = (byte)slideTarget;
                                        nextPattern.Notes[nn].HasAttack = false;
                                    }
                                }
                                // Find the next note that would stop the slide or change the FX settings.
                                else if (slideSpeed != 0 && FindNextNoteForSlide(c, p, n, out var np, out var nn, patternFxData))
                                {
                                    // Compute the pitch delta and find the closest target note.
                                    var numFrames = (s.GetPatternInstanceStartNote(np, nn) - s.GetPatternInstanceStartNote(p, n)) * s.Speed;

                                    // TODO: PAL.
                                    var newNotePitch = Utils.Clamp(noteTable[note.Value] + numFrames * slideSpeed, 0, pitchLimit);
                                    var newNote = FindBestMatchingNote(noteTable, newNotePitch, Math.Sign(slideSpeed));

                                    pattern.Notes[n].SlideNoteTarget = (byte)newNote;

                                    // If the FX was turned off, we need to add an extra note.
                                    var nextPattern = c.PatternInstances[np].Pattern;
                                    if (!nextPattern.Notes[nn].IsMusical &&
                                        !nextPattern.Notes[nn].IsStop)
                                    {
                                        nextPattern.Notes[nn].Instrument = note.Instrument;
                                        nextPattern.Notes[nn].Value = (byte)newNote;
                                        nextPattern.Notes[nn].HasAttack = false;
                                    }
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

                    if (env == null)
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
                    if (channel.PatternInstances[i].Pattern == null)
                    {
                        if (emptyPatternIdx == -1)
                        {
                            emptyPatternIdx = channel.Patterns.IndexOf(channel.CreatePattern());
                        }

                        channel.PatternInstances[i].Pattern = channel.Patterns[emptyPatternIdx];
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
            var project = originalProject.DeepClone();
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
                    lines.Add($"MACRO{i,8} {j,4} {env.Loop,4} {(env.Release >= 0 ? env.Release - 1 : -1),4}   0 : {string.Join(" ", env.Values.Take(env.Length))}");
                }
            }
            lines.Add($"MACRO{4,8} {0,4} {-1}   -1    0 : 0");
            lines.Add($"MACRO{4,8} {1,4} {-1}   -1    0 : 1");
            lines.Add($"MACRO{4,8} {2,4} {-1}   -1    0 : 2");
            lines.Add($"MACRO{4,8} {3,4} {-1}   -1    0 : 3");

            if (project.ExpansionAudio == Project.ExpansionVrc6)
            {
                for (int i = 0; i < Envelope.Max; i++)
                {
                    var envArray = envelopes[Project.ExpansionVrc6, i];
                    for (int j = 0; j < envArray.Length; j++)
                    {
                        var env = envArray[j];
                        lines.Add($"MACROVRC6{i,8} {j,4} {env.Loop,4} {(env.Release >= 0 ? env.Release - 1 : -1),4}   0 : {string.Join(" ", env.Values.Take(env.Length))}");
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

                var expIdx = instrument.IsExpansionInstrument ? 1 : 0;
                int volEnvIdx = instrument.Envelopes[Envelope.Volume].Length   > 0 ? Array.IndexOf(envelopes[expIdx, Envelope.Volume],   instrument.Envelopes[Envelope.Volume])   : -1;
                int arpEnvIdx = instrument.Envelopes[Envelope.Arpeggio].Length > 0 ? Array.IndexOf(envelopes[expIdx, Envelope.Arpeggio], instrument.Envelopes[Envelope.Arpeggio]) : -1;
                int pitEnvIdx = instrument.Envelopes[Envelope.Pitch].Length    > 0 ? Array.IndexOf(envelopes[expIdx, Envelope.Pitch],    instrument.Envelopes[Envelope.Pitch])    : -1;

                if (instrument.ExpansionType == Project.ExpansionNone)
                {
                    lines.Add($"INST2A03{i,4}{volEnvIdx,6}{arpEnvIdx,4}{pitEnvIdx,4}{-1,4}{instrument.DutyCycle,4} \"{instrument.Name}\"");
                }
                else if (instrument.ExpansionType == Project.ExpansionVrc6)
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

                CreateMissingPatterns(song);
                song.CleanupUnusedPatterns();
                song.DuplicateInstancesWithDifferentLengths();

                lines.Add($"TRACK{song.DefaultPatternLength,4}{song.Speed,4}{song.Tempo,4} \"{song.Name}\"");
                lines.Add($"COLUMNS : {string.Join(" ", Enumerable.Repeat(3, song.Channels.Length))}");
                lines.Add("");

                for (int j = 0; j < song.Length; j++)
                {
                    var line = $"ORDER {j:X2} :";

                    for (int k = 0; k < song.Channels.Length; k++)
                        line += $" {song.Channels[k].Patterns.IndexOf(song.Channels[k].PatternInstances[j].Pattern):X2}";

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
                    var prevSlideEffect = '\0';
                    
                    for (int p = 0; p < song.Length; p++)
                    {
                        var instance = channel.PatternInstances[p];
                        var pattern  = instance.Pattern;

                        if (patternRows.ContainsKey(pattern))
                            continue;

                        var patternLines = new List<string>();

                        for (int n = 0; n < song.DefaultPatternLength; n++)
                        {
                            var line = " : ... .. . ... ... ...";

                            if (n < instance.Length)
                            {
                                var note = pattern.Notes[n];
                                var noteString = GetFamiTrackerNoteName(c, note);
                                var volumeString = note.HasVolume ? note.Volume.ToString("X") : ".";
                                var instrumentString = note.IsValid && !note.IsStop ? (note.Instrument == null ? project.Instruments.Count : project.Instruments.IndexOf(note.Instrument)).ToString("X2") : "..";
                                var effectString = "";
                                var noAttack = !note.HasAttack && prevNoteValue == note.Value && (prevSlideEffect == '\0' || prevSlideEffect == 'Q' || prevSlideEffect == '3');

                                if (note.IsSlideNote && note.IsMusical)
                                {
                                    // TODO: PAL.
                                    var noteTable = NesApu.GetNoteTableForChannelType(channel.Type, false);
                                    channel.ComputeSlideNoteParams(p, n, noteTable, out _, out int stepSize, out _);

                                    var absNoteDelta = Math.Abs(note.Value - note.SlideNoteTarget);

                                    // See if we can use Qxy/Rxy (slide up/down y semitones, at speed x), this is preferable.
                                    if (absNoteDelta < 16)
                                    {
                                        if (prevSlideEffect == '1' || prevSlideEffect == '2' || prevSlideEffect == '3')
                                            effectString += $" {prevSlideEffect}00";

                                        // FamiTracker use 2x + 1, find the number that is just above our speed.
                                        var speed = 0;
                                        for (int x = 14; x >= 0; x--)
                                        {
                                            if ((2 * x + 1) < Math.Abs(stepSize / 2.0f))
                                            {
                                                speed = x + 1;
                                                break;
                                            }
                                        }

                                        if (note.SlideNoteTarget > note.Value)
                                            effectString += $" Q{speed:X1}{absNoteDelta:X1}";
                                        else
                                            effectString += $" R{speed:X1}{absNoteDelta:X1}";

                                        prevSlideEffect = 'Q';
                                    }
                                    else
                                    {
                                        // We have one bit of fraction. FramiTracker does not.
                                        var ceilStepSize = Utils.SignedCeil(stepSize / 2.0f);

                                        // If the previous note matched too, we can use 3xx (auto-portamento).
                                        if (prevNoteValue == note.Value)
                                        {
                                            if (prevSlideEffect == '1' || prevSlideEffect == '2')
                                                effectString += $" 100";

                                            noteString = GetFamiTrackerNoteName(c, new Note(note.SlideNoteTarget));
                                            effectString += $" 3{Math.Abs(ceilStepSize):X2}";
                                            prevSlideEffect = '3';
                                            noAttack = false; // Need to force attack when starting auto-portamento unfortunately.
                                        }
                                        else
                                        {
                                            // We have one bit of fraction. FramiTracker does not.
                                            var floorStepSize = Utils.SignedFloor(stepSize / 2.0f);

                                            if (prevSlideEffect == '3')
                                                effectString += $" 300";

                                            if (stepSize > 0)
                                            {
                                                effectString += $" 2{ floorStepSize:X2}";
                                                prevSlideEffect = '2';
                                            }
                                            else if (stepSize < 0)
                                            {
                                                effectString += $" 1{-floorStepSize:X2}";
                                                prevSlideEffect = '1';
                                            }
                                        }
                                    }
                                }
                                else if ((note.IsMusical || note.IsStop) && prevSlideEffect != '\0')
                                {
                                    if (prevSlideEffect == '1' || prevSlideEffect == '2' || prevSlideEffect == '3')
                                        effectString += $" {prevSlideEffect}00";

                                    prevSlideEffect = '\0';
                                }

                                if (n == instance.Length - 1)
                                {
                                    if (p == song.Length - 1)
                                        effectString += $" B{song.LoopPoint:X2}";
                                    else if (instance.Length != song.DefaultPatternLength)
                                        effectString += $" D00";
                                }

                                if (note.HasSpeed)
                                    effectString += $" F{note.Speed:X2}";
                                if (note.HasVibrato)
                                    effectString += $" 4{VibratoSpeedExportLookup[note.VibratoSpeed]:X1}{note.VibratoDepth:X1}";

                                while (effectString.Length < 12)
                                    effectString += " ...";

                                if (noAttack)
                                {
                                    noteString = "...";
                                    instrumentString = "..";
                                }

                                line = $" : {noteString} {instrumentString} {volumeString}{effectString}";

                                if (note.IsMusical || note.IsStop)
                                    prevNoteValue = note.IsSlideNote ? note.SlideNoteTarget : note.Value;
                            }

                            patternLines.Add(line);
                        }

                        patternRows[pattern] = patternLines;
                    }
                }

                for (int j = 0; j < maxPatternCount; j++)
                {
                    lines.Add($"PATTERN {j:X2}");

                    for (int p = 0; p < song.DefaultPatternLength; p++)
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

            File.WriteAllLines(filename, lines);

            return true;
        }
    }
}
