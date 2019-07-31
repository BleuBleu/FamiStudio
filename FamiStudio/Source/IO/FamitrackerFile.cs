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

            var envelopes = new Dictionary<int, Envelope>[Envelope.Max] { new Dictionary<int, Envelope>(), new Dictionary<int, Envelope>(), new Dictionary<int, Envelope>() };
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

            DPCMSample currentDpcm = null;
            int dpcmWriteIdx = 0;
            Song song = null;
            string patternName = "";
            string projectName;

            var lines = File.ReadAllLines(filename);

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();

                if (line.StartsWith("TITLE"))
                {
                    projectName = line.Substring(5).Trim(' ', '"');
                }
                else if (line.StartsWith("MACRO"))
                {
                    var halves = line.Substring(5).Split(':');
                    var param = halves[0].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    var curve = halves[1].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    var type = int.Parse(param[0]);
                    var idx  = int.Parse(param[1]);
                    var loop = int.Parse(param[2]);

                    if (type < 3)
                    {
                        var env = new Envelope();
                        env.Length = curve.Length;
                        env.Loop = loop;
                        for (int j = 0; j < curve.Length; j++)
                            env.Values[j] = sbyte.Parse(curve[j]);
                        if (type == 2)
                        {
                            env.ConvertToAbsolute();
                        }
                        envelopes[type][idx] = env;
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
                        int note = octave * 12 + semitone - 11;

                        if (note >= Note.NoteMin && note <= Note.NoteMax)
                        {
                            int dpcm  = int.Parse(param[3]);
                            int pitch = int.Parse(param[4]);
                            int loop  = int.Parse(param[5]);

                            project.MapDPCMSample(note, dpcms[dpcm], pitch, loop != 0);
                        }
                    }
                }
                else if (line.StartsWith("INST2A03"))
                {
                    var param = SplitStringKeepQuotes(line.Substring(8));

                    int idx = int.Parse(param[0]);
                    int vol = int.Parse(param[1]);
                    int arp = int.Parse(param[2]);
                    int pit = int.Parse(param[3]);
                    int dut = int.Parse(param[5]);
                    var name = param[6];
                    var j = 2;

                    if (!project.IsInstrumentNameUnique(name))
                        name = param[6] + "-" + j++;

                    var instrument = project.CreateInstrument(name);

                    if (vol >= 0) instrument.Envelopes[0] = envelopes[0][vol].Clone();
                    if (arp >= 0) instrument.Envelopes[1] = envelopes[1][arp].Clone();
                    if (pit >= 0) instrument.Envelopes[2] = envelopes[2][pit].Clone();
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
                }
                else if (line.StartsWith("COLUMNS"))
                {
                    var param = line.Substring(7).Split(new[] { ' ', ':' }, StringSplitOptions.RemoveEmptyEntries);
                    for (int j = 0; j < 5; j++)
                        columns[j] = int.Parse(param[j]);
                }
                else if (line.StartsWith("ORDER"))
                {
                    var orderIdx = Convert.ToInt32(line.Substring(6, 2), 16);
                    var values = line.Substring(5).Split(':')[1].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    var order = new int[5];
                    for (int j = 0; j < 5; j++)
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

                    for (int j = 1; j <= 5; j++)
                    {
                        var noteData = channels[j].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        var pattern = song.Channels[j - 1].GetPattern(patternName);

                        if (pattern == null)
                            continue;

                        if (noteData[0] == "---")
                        {
                            pattern.Notes[rowIdx].Value = 0;
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
                                famitoneNote = octave * 12 + semitone - 11;
                            }

                            if (famitoneNote > 0 && famitoneNote < 64)
                            {
                                pattern.Notes[rowIdx].Value = (byte)famitoneNote;
                                pattern.Notes[rowIdx].Instrument = j == 5 ? null : instruments[Convert.ToInt32(noteData[1], 16)];
                            }
                            else
                            {
                                // Note outside of range.
                            }
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

                foreach (var c in s.Channels)
                {
                    c.ColorizePatterns();
                }
            }

            return project;
        }
    }
}
