using System;
using System.Collections.Generic;
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

        static readonly Dictionary<char, byte> TextToEffectLookup = new Dictionary<char, byte>
        {
            { '0', Effect_Arpeggio     },
            { '1', Effect_PortaUp      },
            { '2', Effect_PortaDown    },
            { '3', Effect_Portamento   },
            { '4', Effect_Vibrato      },
            { '7', Effect_Tremolo      },
            { 'A', Effect_VolumeSlide  },
            { 'B', Effect_Jump         },
            { 'C', Effect_Halt         },
            { 'D', Effect_Skip         },
            { 'E', Effect_Volume       },
            { 'F', Effect_Speed        },
            { 'G', Effect_Delay        },
            { 'H', Effect_Sweepup      },
            { 'I', Effect_Sweepdown    },
            { 'P', Effect_Pitch        },
            { 'Q', Effect_SlideUp      },
            { 'R', Effect_SlideDown    },
            { 'S', Effect_NoteCut      },
            { 'V', Effect_DutyCycle    },
            { 'W', Effect_DpcmPitch    },
            { 'X', Effect_Retrigger    },
            { 'Y', Effect_SampleOffset },
            { 'Z', Effect_Dac          },
        };

        static readonly Dictionary<char, int> FdsTextToEffectLookup = new Dictionary<char, int>
        {
            { 'H', Effect_FdsModDepth   },
            { 'I', Effect_FdsModSpeedHi },
            { 'J', Effect_FdsModSpeedLo },
        };

        static readonly Dictionary<byte, char> EffectToTextLookup = new Dictionary<byte, char>
        {
            { Effect_Arpeggio      , '0' },
            { Effect_PortaUp       , '1' },
            { Effect_PortaDown     , '2' },
            { Effect_Portamento    , '3' },
            { Effect_Vibrato       , '4' },
            { Effect_Tremolo       , '7' },
            { Effect_VolumeSlide   , 'A' },
            { Effect_Jump          , 'B' },
            { Effect_Halt          , 'C' },
            { Effect_Skip          , 'D' },
            { Effect_Volume        , 'E' },
            { Effect_Speed         , 'F' },
            { Effect_Delay         , 'G' },
            { Effect_Sweepup       , 'H' },
            { Effect_Sweepdown     , 'I' },
            { Effect_Pitch         , 'P' },
            { Effect_SlideUp       , 'Q' },
            { Effect_SlideDown     , 'R' },
            { Effect_NoteCut       , 'S' },
            { Effect_DutyCycle     , 'V' },
            { Effect_DpcmPitch     , 'W' },
            { Effect_Retrigger     , 'X' },
            { Effect_SampleOffset  , 'Y' },
            { Effect_Dac           , 'Z' },
            { Effect_FdsModDepth   , 'H' },
            { Effect_FdsModSpeedHi , 'I' },
            { Effect_FdsModSpeedLo , 'J' },
        };

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
            var project = new Project();

            var envelopes = new Dictionary<int, Envelope>[Project.ExpansionCount, Envelope.Max];
            var duties = new Dictionary<int, int>[Project.ExpansionCount];
            var instruments = new Dictionary<int, Instrument>();
            var patternLengths = new Dictionary<Pattern, byte>();
            var dpcms = new Dictionary<int, DPCMSample>();
            var columns = new int[5] { 1, 1, 1, 1, 1 };
            var patternFxData = new Dictionary<Pattern, RowFxData[,]>();

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
                    var convertedExp = ConvertExpansionAudio(exp);

                    if (convertedExp < 0 || !Project.IsExpansionAudioAllowed(convertedExp))
                        return null;

                    project.SetExpansionAudio(convertedExp);
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
                        if (env.CanRelease && loop == -1 && rel != -1)
                            loop = rel;

                        env.Loop = loop;
                        env.Release = env.CanRelease && rel != -1 ? rel + 1 : -1;
                        env.Relative = type == 2;

                        for (int j = 0; j < curve.Length; j++)
                            env.Values[j] = sbyte.Parse(curve[j]);

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
                    if (currentDpcm != null) // Can happen if more than 16KB of samples
                    {
                        var param = line.Substring(6).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var s in param)
                        {
                            currentDpcm.Data[dpcmWriteIdx++] = Convert.ToByte(s, 16);
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
                    while (!project.IsInstrumentNameUnique(name))
                        name = param[6] + "-" + j++;

                    var expansionType = line.StartsWith("INSTVRC6") ? Project.ExpansionVrc6 : Project.ExpansionNone;
                    var instrument = project.CreateInstrument(expansionType, name);

                    if (vol >= 0) instrument.Envelopes[Envelope.Volume]    = envelopes[expansion, 0][vol].ShallowClone();
                    if (arp >= 0) instrument.Envelopes[Envelope.Arpeggio]  = envelopes[expansion, 1][arp].ShallowClone();
                    if (pit >= 0) instrument.Envelopes[Envelope.Pitch]     = envelopes[expansion, 2][pit].ShallowClone();
                    if (dut >= 0) instrument.Envelopes[Envelope.DutyCycle] = envelopes[expansion, 3][pit].ShallowClone();

                    instruments[idx] = instrument;
                }
                else if (line.StartsWith("INSTFDS"))
                {
                    var param = SplitStringKeepQuotes(line.Substring(line.IndexOf(' ')));

                    // TODO:
                    //mod_enable: int[0,1] - 0 for modulator disabled, 1 for enabled 
                    //mod_speed: int[0,4905] - modulator speed 
                    //mod_depth: int[0,63] - modulator depth 
                    //mod_delay: int[0,255] - modulator delay 

                    int idx = int.Parse(param[0]);
                    var name = param[5];

                    var j = 2;
                    while (!project.IsInstrumentNameUnique(name))
                        name = param[5] + "-" + j++;

                    instruments[idx] = project.CreateInstrument(Project.ExpansionFds, name);
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

                    var env = instruments[inst].Envelopes[type];

                    env.Length = curve.Length;

                    // FamiTracker allows envelope with release with no loop. We dont allow that.
                    if (env.CanRelease && loop == -1 && rel != -1)
                        loop = rel;

                    env.Loop = loop;
                    env.Release = env.CanRelease && rel != -1 ? rel + 1 : -1;
                    env.Relative = type == 2;

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
                    var env = instruments[inst].Envelopes[mod ? Envelope.FdsModulation : Envelope.FdsWaveform];
                    for (int j = 0; j < curve.Length; j++)
                        env.Values[j] = sbyte.Parse(curve[j]);
                    if (mod)
                        env.ConvertFdsModulationToAbsolute();
                }
                else if (line.StartsWith("TRACK"))
                {
                    var param = SplitStringKeepQuotes(line.Substring(5));

                    song = project.CreateSong(param[3]);
                    song.SetLength(0);
                    song.SetDefaultPatternLength(int.Parse(param[0]));
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
                            patternFxData[pattern] = new RowFxData[song.DefaultPatternLength, 4];

                        // Note
                        if (noteData[0] == "---")
                        {
                            pattern.Notes[n].Value = Note.NoteStop;
                        }
                        else if (noteData[0] == "===")
                        {
                            pattern.Notes[n].Value = Note.NoteRelease;
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
                                pattern.Notes[n].Value = (byte)famitoneNote;
                                pattern.Notes[n].Instrument = j == 5 ? null : instruments[Convert.ToInt32(noteData[1], 16)];
                            }
                            else
                            {
                                // Note outside of range.
                            }
                        }

                        // Volume
                        if (noteData[2] != ".")
                        {
                            pattern.Notes[n].Volume = Convert.ToByte(noteData[2], 16);
                        }

                        // Read FX.
                        for (int k = 0; k < columns[j - 1]; k++)
                        {
                            var fxStr = noteData[3 + k];

                            if (fxStr == "...")
                                continue;

                            var fx = new RowFxData();

                            fx.fx    = TextToEffectLookup[fxStr[0]];
                            fx.param = Convert.ToByte(fxStr.Substring(1), 16);
                            patternFxData[pattern][n, k] = fx;

                            ApplySimpleEffects(fx, pattern, n, patternLengths);
                        }
                    }
                }
            }

            FinishImport(project, patternLengths, patternFxData);

            return project;
        }

        private void ConvertPitchEnvelopes(Project project)
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

        private Envelope[,][] MergeIdenticalEnvelopes(Project project)
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
                if (channel == Channel.Noise)
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
            lines.Add("MACHINE         0");
            lines.Add("FRAMERATE       0");
            lines.Add("EXPANSION       " + (project.ExpansionAudio != Project.ExpansionNone ? (1 << (project.ExpansionAudio - 1)) : 0));
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

                var volEnv = instrument.Envelopes[Envelope.Volume];
                var arpEnv = instrument.Envelopes[Envelope.Arpeggio];
                var pitEnv = instrument.Envelopes[Envelope.Pitch];
                var dutEnv = instrument.Envelopes[Envelope.DutyCycle];

                var expIdx    = instrument.IsExpansionInstrument ? 1 : 0;
                int volEnvIdx = volEnv != null && volEnv.Length > 0 ? Array.IndexOf(envelopes[expIdx, Envelope.Volume],    instrument.Envelopes[Envelope.Volume])    : -1;
                int arpEnvIdx = arpEnv != null && arpEnv.Length > 0 ? Array.IndexOf(envelopes[expIdx, Envelope.Arpeggio],  instrument.Envelopes[Envelope.Arpeggio])  : -1;
                int pitEnvIdx = pitEnv != null && pitEnv.Length > 0 ? Array.IndexOf(envelopes[expIdx, Envelope.Pitch],     instrument.Envelopes[Envelope.Pitch])     : -1;
                int dutEnvIdx = dutEnv != null && dutEnv.Length > 0 ? Array.IndexOf(envelopes[expIdx, Envelope.DutyCycle], instrument.Envelopes[Envelope.DutyCycle]) : -1;

                if (instrument.ExpansionType == Project.ExpansionNone)
                {
                    lines.Add($"INST2A03{i,4}{volEnvIdx,6}{arpEnvIdx,4}{pitEnvIdx,4}{-1,4}{dutEnvIdx,4} \"{instrument.Name}\"");
                }
                else if (instrument.ExpansionType == Project.ExpansionVrc6)
                {
                    lines.Add($"INSTVRC6{i,4}{volEnvIdx,6}{arpEnvIdx,4}{pitEnvIdx,4}{-1,4}{dutEnvIdx,4} \"{instrument.Name}\"");
                }
                else if (instrument.ExpansionType == Project.ExpansionFds)
                {
                    // TODO: Modulation parameters.
                    lines.Add($"INSTFDS{i,5}{0,6}{0,4}{0,4}{0,4} \"{instrument.Name}\"");

                    var wavEnv = instrument.Envelopes[Envelope.FdsWaveform];
                    lines.Add($"FDSWAVE{i,5} : {string.Join(" ", wavEnv.Values.Take(wavEnv.Length))}");
                    var modEnv = instrument.Envelopes[Envelope.FdsModulation].BuildFdsModulationTable();
                    lines.Add($"FDSMOD{i,6} : {string.Join(" ", modEnv.Take(modEnv.Length))}");

                    for (int j = 0; j <= Envelope.Pitch; j++)
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
                    var prevSlideEffect = Effect_None;
                    
                    for (int p = 0; p < song.Length; p++)
                    {
                        var pattern = channel.PatternInstances[p];
                        var patternLen = song.GetPatternLength(p);

                        if (patternRows.ContainsKey(pattern))
                            continue;

                        var patternLines = new List<string>();

                        for (int n = 0; n < song.DefaultPatternLength; n++)
                        {
                            var line = " : ... .. . ... ... ...";

                            if (n < patternLen)
                            {
                                var note = pattern.Notes[n];
                                var noteString = GetFamiTrackerNoteName(c, note);
                                var volumeString = note.HasVolume ? note.Volume.ToString("X") : ".";
                                var instrumentString = note.IsValid && !note.IsStop ? (note.Instrument == null ? project.Instruments.Count : project.Instruments.IndexOf(note.Instrument)).ToString("X2") : "..";
                                var effectString = "";
                                var noAttack = !note.HasAttack && prevNoteValue == note.Value && (prevSlideEffect == Effect_None || prevSlideEffect == Effect_SlideUp || prevSlideEffect == Effect_Portamento);

                                if (note.IsSlideNote && note.IsMusical)
                                {
                                    // TODO: PAL.
                                    var noteTable = NesApu.GetNoteTableForChannelType(channel.Type, false);
                                    channel.ComputeSlideNoteParams(p, n, noteTable, out _, out int stepSize, out _);

                                    var absNoteDelta = Math.Abs(note.Value - note.SlideNoteTarget);

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

                                        prevSlideEffect = Effect_SlideUp;
                                    }
                                    else
                                    {
                                        // We have one bit of fraction. FramiTracker does not.
                                        var ceilStepSize = Utils.SignedCeil(stepSize / 2.0f);

                                        // If the previous note matched too, we can use 3xx (auto-portamento).
                                        if (prevNoteValue == note.Value)
                                        {
                                            if (prevSlideEffect == Effect_PortaUp ||
                                                prevSlideEffect == Effect_PortaDown)
                                            {
                                                effectString += $" 100";
                                            }

                                            noteString = GetFamiTrackerNoteName(c, new Note(note.SlideNoteTarget));
                                            effectString += $" 3{Math.Abs(ceilStepSize):X2}";
                                            prevSlideEffect = Effect_Portamento;
                                            noAttack = false; // Need to force attack when starting auto-portamento unfortunately.
                                        }
                                        else
                                        {
                                            // We have one bit of fraction. FramiTracker does not.
                                            var floorStepSize = Utils.SignedFloor(stepSize / 2.0f);

                                            if (prevSlideEffect == Effect_Portamento)
                                                effectString += $" 300";

                                            if (stepSize > 0)
                                            {
                                                effectString += $" 2{ floorStepSize:X2}";
                                                prevSlideEffect = Effect_PortaDown;
                                            }
                                            else if (stepSize < 0)
                                            {
                                                effectString += $" 1{-floorStepSize:X2}";
                                                prevSlideEffect = Effect_PortaUp;
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

                                if (n == patternLen - 1)
                                {
                                    if (p == song.Length - 1)
                                        effectString += $" B{song.LoopPoint:X2}";
                                    else if (patternLen != song.DefaultPatternLength)
                                        effectString += $" D00";
                                }

                                if (note.HasSpeed)
                                    effectString += $" F{note.Speed:X2}";
                                if (note.HasVibrato)
                                    effectString += $" 4{VibratoSpeedExportLookup[note.VibratoSpeed]:X1}{note.VibratoDepth:X1}";
                                if (note.HasFinePitch)
                                    effectString += $" P{(byte)(-note.FinePitch + 0x80):X2}";

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

            lines.Add("# End of export");

            File.WriteAllLines(filename, lines);

            return true;
        }
    }
}
