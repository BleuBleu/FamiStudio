﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FamiStudio
{
    public class FamistudioTextFile
    {
        public bool Save(Project originalProject, string filename, int[] songIds, bool deleteUnusedData)
        {
            var project = originalProject.DeepClone();
            project.RemoveAllSongsBut(songIds, deleteUnusedData);

            var lines = new List<string>();

            var versionString = Application.ProductVersion.Substring(0, Application.ProductVersion.LastIndexOf('.'));
            var projectLine = $"Project Version=\"{versionString}\" TempoMode=\"{TempoType.Names[project.TempoMode]}\"";

            if (project.Name      != "")     projectLine += $" Name=\"{project.Name}\"";
            if (project.Author    != "")     projectLine += $" Author=\"{project.Author}\"";
            if (project.Copyright != "")     projectLine += $" Copyright=\"{project.Copyright}\"";
            if (project.UsesExpansionAudio)  projectLine += $" Expansion=\"{ExpansionType.ShortNames[project.ExpansionAudio]}\"";
            if (project.PalMode)             projectLine += $" PAL=\"{true}\"";

            lines.Add(projectLine);

            // DPCM samples
            foreach (var sample in project.Samples)
            {
                lines.Add($"\tDPCMSample Name=\"{sample.Name}\" ReverseBits=\"{sample.ReverseBits.ToString()}\" Data=\"{String.Join("", sample.ProcessedData.Select(x => $"{x:x2}"))}\"");
            }

            // DPCM mappings
            for (int i = 0; i < project.SamplesMapping.Length; i++)
            {
                var mapping = project.SamplesMapping[i];

                if (mapping != null && mapping.Sample != null)
                    lines.Add($"\tDPCMMapping Note=\"{Note.GetFriendlyName(i + Note.DPCMNoteMin)}\" Sample=\"{mapping.Sample.Name}\" Pitch=\"{mapping.Pitch}\" Loop=\"{mapping.Loop}\"");
            }

            // Instruments
            foreach (var instrument in project.Instruments)
            {
                var instrumentLine = $"\tInstrument Name=\"{instrument.Name}\"";
                if (instrument.IsExpansionInstrument)
                {
                    instrumentLine += $" Expansion=\"{ExpansionType.ShortNames[project.ExpansionAudio]}\"";

                    if (instrument.ExpansionType == ExpansionType.Fds)
                    {
                        instrumentLine += $" FdsWavePreset=\"{WavePresetType.Names[instrument.FdsWavePreset]}\"";
                        instrumentLine += $" FdsModPreset=\"{WavePresetType.Names[instrument.FdsModPreset]}\"";
                        if (instrument.FdsMasterVolume != 0) instrumentLine += $" FdsMasterVolume=\"{instrument.FdsMasterVolume}\"";
                        if (instrument.FdsModSpeed     != 0) instrumentLine += $" FdsModSpeed=\"{instrument.FdsModSpeed}\"";
                        if (instrument.FdsModDepth     != 0) instrumentLine += $" FdsModDepth=\"{instrument.FdsModDepth}\"";
                        if (instrument.FdsModDelay     != 0) instrumentLine += $" FdsModDelay=\"{instrument.FdsModDelay}\"";
                    }
                    else if (instrument.ExpansionType == ExpansionType.N163)
                    {
                        instrumentLine += $" N163WavePreset=\"{WavePresetType.Names[instrument.N163WavePreset]}\"";
                        instrumentLine += $" N163WaveSize=\"{instrument.N163WaveSize}\"";
                        instrumentLine += $" N163WavePos=\"{instrument.N163WavePos}\"";
                    }
                    else if (instrument.ExpansionType == ExpansionType.Vrc7)
                    {
                        instrumentLine += $" Vrc7Patch=\"{instrument.Vrc7Patch}\"";

                        if (instrument.Vrc7Patch == Vrc7InstrumentPatch.Custom)
                        {
                            for (int i = 0; i < 8; i++)
                                instrumentLine += $" Vrc7Reg{i}=\"{instrument.Vrc7PatchRegs[i]}\"";
                        }
                    }
                }
                lines.Add(instrumentLine);

                for (int i = 0; i < EnvelopeType.Count; i++)
                {
                    var env = instrument.Envelopes[i];
                    if (env != null && !env.IsEmpty)
                    {
                        var envelopeLine = $"\t\tEnvelope Type=\"{EnvelopeType.ShortNames[i]}\" Length=\"{env.Length}\"";

                        if (env.Loop     >= 0) envelopeLine += $" Loop=\"{env.Loop}\"";
                        if (env.Release  >= 0) envelopeLine += $" Release=\"{env.Release}\"";
                        if (env.Relative)      envelopeLine += $" Relative=\"{env.Relative}\"";

                        envelopeLine += $" Values=\"{String.Join(",", env.Values.Take(env.Length))}\"";

                        lines.Add(envelopeLine);
                    }
                }
            }

            // Arpeggios
            foreach (var arpeggio in project.Arpeggios)
            {
                var env = arpeggio.Envelope;
                var arpeggioLine = $"\tArpeggio Name=\"{arpeggio.Name}\" Length=\"{env.Length}\"";
                if (env.Loop >= 0) arpeggioLine += $" Loop=\"{env.Loop}\"";
                arpeggioLine += $" Values=\"{String.Join(",", env.Values.Take(env.Length))}\"";
                lines.Add(arpeggioLine);
            }

            // Songs
            foreach (var song in project.Songs)
            {
                var songStr = $"\tSong Name=\"{song.Name}\" Length=\"{song.Length}\" LoopPoint=\"{song.LoopPoint}\"";

                if (song.UsesFamiTrackerTempo)
                {
                    songStr += $" PatternLength=\"{song.PatternLength}\" BeatLength=\"{song.BeatLength}\" FamiTrackerTempo=\"{song.FamitrackerTempo}\" FamiTrackerSpeed=\"{song.FamitrackerSpeed}\"";
                }
                else
                {
                    songStr += $" PatternLength=\"{song.PatternLength / song.NoteLength}\" BeatLength=\"{song.BeatLength / song.NoteLength}\" NoteLength=\"{song.NoteLength}\"";
                }

                lines.Add(songStr);

                for (int i = 0; i < song.Length; i++)
                {
                    if (song.PatternHasCustomSettings(i))
                    {
                        var patternLength = song.GetPatternLength(i);

                        if (song.UsesFamiTrackerTempo)
                        {
                            lines.Add($"\t\tPatternCustomSettings Time=\"{i}\" Length=\"{patternLength}\"");
                        }
                        else
                        {
                            var noteLength = song.GetPatternNoteLength(i);
                            var beatLength = song.GetPatternBeatLength(i);

                            lines.Add($"\t\tPatternCustomSettings Time=\"{i}\" Length=\"{patternLength / noteLength}\" NoteLength=\"{noteLength}\" BeatLength=\"{beatLength / noteLength}\"");
                        }
                    }
                }

                foreach (var channel in song.Channels)
                {
                    lines.Add($"\t\tChannel Type=\"{ChannelType.ShortNames[channel.Type]}\"");

                    foreach (var pattern in channel.Patterns)
                    {
                        lines.Add($"\t\t\tPattern Name=\"{pattern.Name}\"");

                        foreach (var kv in pattern.Notes)
                        {
                            var note = kv.Value;

                            if (!note.IsEmpty)
                            {
                                var noteLine = $"\t\t\t\tNote Time=\"{kv.Key}\"";

                                if (note.IsValid)
                                {
                                    noteLine += $" Value=\"{note.FriendlyName}\"";
                                    if (note.Instrument != null)
                                        noteLine += $" Instrument=\"{note.Instrument.Name}\"";
                                    if (note.IsArpeggio)
                                        noteLine += $" Arpeggio=\"{note.Arpeggio.Name}\"";
                                }

                                if (!note.HasAttack)     noteLine += $" Attack=\"{false.ToString()}\"";
                                if (note.HasVolume)      noteLine += $" Volume=\"{note.Volume}\"";
                                if (note.HasVibrato)     noteLine += $" VibratoSpeed=\"{note.VibratoSpeed}\" VibratoDepth=\"{note.VibratoDepth}\"";
                                if (note.HasSpeed)       noteLine += $" Speed=\"{note.Speed}\"";
                                if (note.HasFinePitch)   noteLine += $" FinePitch=\"{note.FinePitch}\"";
                                if (note.HasFdsModSpeed) noteLine += $" FdsModSpeed=\"{note.FdsModSpeed}\"";
                                if (note.HasFdsModDepth) noteLine += $" FdsModDepth=\"{note.FdsModDepth}\"";
                                if (note.HasDutyCycle)   noteLine += $" DutyCycle=\"{note.DutyCycle}\"";
                                if (note.HasNoteDelay)   noteLine += $" NoteDelay=\"{note.NoteDelay}\"";
                                if (note.HasCutDelay)    noteLine += $" CutDelay=\"{note.CutDelay}\"";
                                if (note.IsMusical && note.IsSlideNote) noteLine += $" SlideTarget=\"{Note.GetFriendlyName(note.SlideNoteTarget)}\""; 

                                lines.Add(noteLine);
                            }
                        }
                    }

                    for (int p = 0; p < song.Length; p++)
                    {
                        var pattern = channel.PatternInstances[p];

                        if (pattern != null)
                            lines.Add($"\t\t\tPatternInstance Time=\"{p}\" Pattern=\"{pattern.Name}\"");
                    }
                }
            }

            File.WriteAllLines(filename, lines);

            return true;
        }

        private static string[] SplitStringKeepQuotes(string str)
        {
            return str.Split('"').Select((element, index) => index % 2 == 0
                                                        ? element.Split(new[] { ' ', '=' }, StringSplitOptions.RemoveEmptyEntries)
                                                        : new string[] { element })
                                  .SelectMany(element => element).ToArray();
        }

        private static string SplitLine(string line, ref Dictionary<string, string> parameters)
        {
            var splits = SplitStringKeepQuotes(line);

            if (splits != null && splits.Length > 0)
            {
                parameters.Clear();
                for (int i = 1; i < splits.Length; i += 2)
                    parameters[splits[i]] = splits[i + 1];
                return splits[0];
            }

            return null;
        }

        public static bool LooksLikeFamiStudioText(string filename)
        {
            try
            {
                var lines = File.ReadAllLines(filename);
                var parameters = new Dictionary<string, string>();

                // TODO: Ignore empty lines. Whitespace shouldnt matter.
                return SplitLine(lines[0].Trim(), ref parameters) == "Project";
            }
            catch
            {
                return false;
            }
        }

        public Project Load(string filename)
        {
            try
            {
                var lines = File.ReadAllLines(filename);
                var parameters = new Dictionary<string, string>();
                var project = (Project)null;
                var instrument = (Instrument)null;
                var arpeggio = (Arpeggio)null;
                var song = (Song)null;
                var channel = (Channel)null;
                var pattern = (Pattern)null;
                var version = "9.9.9";

                foreach (var line in lines)
                {
                    var cmd = SplitLine(line.Trim(), ref parameters);

                    switch (cmd)
                    {
                        case "Project":
                        {
                            project = new Project();
                            parameters.TryGetValue("Version", out version);
                            if (parameters.TryGetValue("Name", out var name)) project.Name = name;
                            if (parameters.TryGetValue("Author", out var author)) project.Author = author;
                            if (parameters.TryGetValue("Copyright", out var copyright)) project.Copyright = copyright;
                            if (parameters.TryGetValue("Expansion", out var expansion)) project.SetExpansionAudio(ExpansionType.GetValueForShortName(expansion));
                            if (parameters.TryGetValue("TempoMode", out var tempoMode)) project.TempoMode = TempoType.GetValueForName(tempoMode);
                            if (parameters.TryGetValue("PAL", out var pal)) project.PalMode = bool.Parse(pal);
                            break;
                        }
                        case "DPCMSample":
                        {
                            var str = parameters["Data"];
                            var data = new byte[str.Length / 2];
                            for (int i = 0; i < data.Length; i++)
                                data[i] = Convert.ToByte(str.Substring(i * 2, 2), 16);
                            var sample = project.CreateDPCMSampleFromDmcData(parameters["Name"], data);
                            if (parameters.TryGetValue("ReverseBits", out var reverseStr)) sample.ReverseBits = bool.Parse(reverseStr);
                            break;
                        }
                        case "DPCMMapping":
                        {
                            var pitch = 15;
                            var loop = false;
                            if (parameters.TryGetValue("Pitch", out var pitchStr)) pitch = int.Parse(pitchStr);
                            if (parameters.TryGetValue("Loop", out var loopStr)) loop = bool.Parse(loopStr);
                            project.MapDPCMSample(Note.FromFriendlyName(parameters["Note"]), project.GetSample(parameters["Sample"]), pitch, loop);
                            break;
                        }
                        case "Instrument":
                        {
                            instrument = project.CreateInstrument(parameters.TryGetValue("Expansion", out _) ? project.ExpansionAudio : ExpansionType.None, parameters["Name"]);

                            if (instrument.ExpansionType == ExpansionType.Fds)
                            {
                                if (parameters.TryGetValue("FdsWavePreset",   out var wavPresetStr))    instrument.FdsWavePreset   = (byte)WavePresetType.GetValueForName(wavPresetStr);
                                if (parameters.TryGetValue("FdsModPreset",    out var modPresetStr))    instrument.FdsWavePreset   = (byte)WavePresetType.GetValueForName(modPresetStr);
                                if (parameters.TryGetValue("FdsMasterVolume", out var masterVolumeStr)) instrument.FdsMasterVolume = byte.Parse(masterVolumeStr);
                                if (parameters.TryGetValue("FdsModSpeed",     out var fdsModSpeedStr))  instrument.FdsModSpeed     = ushort.Parse(fdsModSpeedStr);
                                if (parameters.TryGetValue("FdsModDepth",     out var fdsModDepthStr))  instrument.FdsModDepth     = byte.Parse(fdsModDepthStr);
                                if (parameters.TryGetValue("FdsModDelay",     out var fdsModDelayStr))  instrument.FdsModDelay     = byte.Parse(fdsModDelayStr);
                            }
                            else if (instrument.ExpansionType == ExpansionType.N163)
                            {
                                 if (parameters.TryGetValue("N163WavePreset", out var wavPresetStr))    instrument.N163WavePreset = (byte)WavePresetType.GetValueForName(wavPresetStr);
                                 if (parameters.TryGetValue("N163WaveSize",   out var n163WavSizeStr))  instrument.N163WaveSize   = byte.Parse(n163WavSizeStr);
                                 if (parameters.TryGetValue("N163WavePos",    out var n163WavPosStr))   instrument.N163WavePos    = byte.Parse(n163WavPosStr);
                            }
                            else if (instrument.ExpansionType == ExpansionType.Vrc7)
                            {
                                if (parameters.TryGetValue("Vrc7Patch", out var vrc7PatchStr)) instrument.Vrc7Patch = byte.Parse(vrc7PatchStr);

                                if (instrument.Vrc7Patch == Vrc7InstrumentPatch.Custom)
                                {
                                    for (int i = 0; i < 8; i++)
                                    {
                                        if (parameters.TryGetValue($"Vrc7Reg{i}", out var regStr))
                                           instrument.Vrc7PatchRegs[i] = byte.Parse(regStr);
                                    }
                                }
                            }

                            break;
                        }
                        case "Arpeggio":
                        {
                            arpeggio = project.CreateArpeggio(parameters["Name"]);
                            arpeggio.Envelope.Length = int.Parse(parameters["Length"]);
                            
                            if (parameters.TryGetValue("Loop", out var loopStr))
                                arpeggio.Envelope.Loop = int.Parse(loopStr);

                            var values = parameters["Values"].Split(',');
                            for (int j = 0; j < values.Length; j++)
                                arpeggio.Envelope.Values[j] = sbyte.Parse(values[j]);

                            break;
                        }
                        case "Envelope":
                        {
                            var env = instrument.Envelopes[EnvelopeType.GetValueForShortName(parameters["Type"])];
                            if (env != null)
                            {
                                if (env.CanResize)
                                    env.Length = int.Parse(parameters["Length"]);

                                if (parameters.TryGetValue("Loop",     out var loopStr))      env.Loop     = int.Parse(loopStr);
                                if (parameters.TryGetValue("Release",  out var releaseStr))   env.Release  = int.Parse(releaseStr);
                                if (parameters.TryGetValue("Relative", out var relativeStr))  env.Relative = bool.Parse(relativeStr);

                                var values = parameters["Values"].Split(',');
                                for (int j = 0; j < values.Length; j++)
                                    env.Values[j] = sbyte.Parse(values[j]);
                            }
                            break;
                        }
                        case "Song":
                        {
                            song = project.CreateSong(parameters["Name"]);
                            song.SetLength(int.Parse(parameters["Length"]));
                            song.SetBeatLength(int.Parse(parameters[string.CompareOrdinal(version, "2.3.0") >= 0 ? "BeatLength" : "BarLength"]));
                            song.SetLoopPoint(int.Parse(parameters["LoopPoint"]));

                            if (song.UsesFamiTrackerTempo)
                            {
                                song.SetDefaultPatternLength(int.Parse(parameters["PatternLength"]));
                                song.FamitrackerTempo = int.Parse(parameters["FamiTrackerTempo"]);
                                song.FamitrackerSpeed = int.Parse(parameters["FamiTrackerSpeed"]);
                            }
                            else
                            {
                                var noteLength = int.Parse(parameters["NoteLength"]);
                                song.ResizeNotes(noteLength, false);
                                song.SetBeatLength(song.BeatLength * noteLength);
                                song.SetDefaultPatternLength(int.Parse(parameters["PatternLength"]) * noteLength);
                            }
                            break;
                        }
                        case "PatternCustomSettings":
                        {
                            if (project.UsesFamiTrackerTempo)
                            {
                                var beatLength = song.BeatLength;
                                if (parameters.TryGetValue(string.CompareOrdinal(version, "2.3.0") >= 0 ? "BeatLength" : "BarLength", out var beatLengthStr))
                                    beatLength = int.Parse(beatLengthStr);

                                song.SetPatternCustomSettings(int.Parse(parameters["Time"]), int.Parse(parameters["Length"]), beatLength);
                            }
                            else
                            {
                                var patternLength = int.Parse(parameters["Length"]);
                                var noteLength = int.Parse(parameters["NoteLength"]);
                                var beatLength = int.Parse(parameters[string.CompareOrdinal(version, "2.3.0") >= 0 ? "BeatLength" : "BarLength"]);

                                song.SetPatternCustomSettings(int.Parse(parameters["Time"]), patternLength * noteLength, beatLength * noteLength, noteLength);
                            }
                            break;
                        }
                        case "Channel":
                        {
                            var channelType = ChannelType.GetValueForShortName(parameters["Type"]);
                            channel = song.GetChannelByType(channelType);
                            break;
                        }
                        case "Pattern":
                        {
                            pattern = channel.CreatePattern(parameters["Name"]);
                            break;
                        }
                        case "Note":
                        {
                            var time = int.Parse(parameters["Time"]);
                            var note = pattern.GetOrCreateNoteAt(time);

                            if (parameters.TryGetValue("Value", out var valueStr))
                                note.Value = (byte)Note.FromFriendlyName(valueStr);
                            if (parameters.TryGetValue("Instrument", out var instStr) && channel.SupportsInstrument(project.GetInstrument(instStr)))
                                note.Instrument = project.GetInstrument(instStr);
                            if (parameters.TryGetValue("Arpeggio", out var arpStr) && channel.SupportsArpeggios)
                                note.Arpeggio = project.GetArpeggio(arpStr);
                            if (parameters.TryGetValue("SlideTarget", out var slideStr) && channel.SupportsSlideNotes)
                                note.SlideNoteTarget = (byte)Note.FromFriendlyName(slideStr);
                            if (parameters.TryGetValue("Attack", out var attackStr))
                                note.HasAttack = bool.Parse(attackStr);
                            if (parameters.TryGetValue("Volume", out var volumeStr) && channel.SupportsEffect(Note.EffectVolume))
                                note.Volume = byte.Parse(volumeStr);
                            if (parameters.TryGetValue("VibratoSpeed", out var vibSpeedStr) && channel.SupportsEffect(Note.EffectVibratoSpeed))
                                note.VibratoSpeed = byte.Parse(vibSpeedStr);
                            if (parameters.TryGetValue("VibratoDepth", out var vibDepthStr) && channel.SupportsEffect(Note.EffectVibratoDepth))
                                note.VibratoDepth = byte.Parse(vibDepthStr);
                            if (parameters.TryGetValue("Speed", out var speedStr) && channel.SupportsEffect(Note.EffectSpeed))
                                note.Speed = byte.Parse(speedStr);
                            if (parameters.TryGetValue("FinePitch", out var finePitchStr) && channel.SupportsEffect(Note.EffectFinePitch))
                                note.FinePitch = sbyte.Parse(finePitchStr);
                            if (parameters.TryGetValue("FdsModSpeed", out var modSpeedStr) && channel.SupportsEffect(Note.EffectFdsModSpeed))
                                note.FdsModSpeed = ushort.Parse(modSpeedStr);
                            if (parameters.TryGetValue("FdsModDepth", out var modDepthStr) && channel.SupportsEffect(Note.EffectFdsModDepth))
                                note.FdsModDepth = byte.Parse(modDepthStr);
                            if (parameters.TryGetValue("DutyCycle", out var dutyCycleStr) && channel.SupportsEffect(Note.EffectDutyCycle))
                                note.DutyCycle = byte.Parse(dutyCycleStr);
                            if (parameters.TryGetValue("NoteDelay", out var noteDelayStr) && channel.SupportsEffect(Note.EffectNoteDelay))
                                note.NoteDelay = byte.Parse(noteDelayStr);
                            if (parameters.TryGetValue("CutDelay", out var cutDelayStr) && channel.SupportsEffect(Note.EffectCutDelay))
                                note.CutDelay = byte.Parse(cutDelayStr);

                            break;
                        }
                        case "PatternInstance":
                        {
                            var time = int.Parse(parameters["Time"]);
                            channel.PatternInstances[time] = channel.GetPattern(parameters["Pattern"]);
                            break;
                        }
                    }
                }

                return project;
            }
            catch (Exception e)
            {
                Log.LogMessage(LogSeverity.Error, "Please contact the developer on GitHub!");
                Log.LogMessage(LogSeverity.Error, e.Message);
                Log.LogMessage(LogSeverity.Error, e.StackTrace);
                return null;
            }
        }
    }
}
