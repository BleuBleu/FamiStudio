using System;
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
        public bool Save(Project originalProject, string filename, int[] songIds)
        {
            var project = originalProject.DeepClone();
            project.RemoveAllSongsBut(songIds);

            var lines = new List<string>();

            var versionString = Application.ProductVersion.Substring(0, Application.ProductVersion.LastIndexOf('.'));
            var projectLine = $"Project Version=\"{versionString}\" TempoMode=\"{Project.TempoModeNames[project.TempoMode]}\"";

            if (project.Name      != "")    projectLine += $" Name=\"{project.Name}\"";
            if (project.Author    != "")    projectLine += $" Author=\"{project.Author}\"";
            if (project.Copyright != "")    projectLine += $" Copyright=\"{project.Copyright}\"";
            if (project.UsesExpansionAudio) projectLine += $" Expansion=\"{Project.ExpansionShortNames[project.ExpansionAudio]}\"";

            lines.Add(projectLine);

            // DPCM samples
            foreach (var sample in project.Samples)
            {
                lines.Add($"\tDPCMSample Name=\"{sample.Name}\" Data=\"{String.Join("", sample.Data.Select(x => $"{x:x2}"))}\"");
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
                    instrumentLine += $" Expansion=\"{Project.ExpansionShortNames[project.ExpansionAudio]}\"";

                    if (instrument.ExpansionType == Project.ExpansionFds)
                    {
                        instrumentLine += $" FdsWavePreset=\"{Envelope.PresetNames[instrument.FdsWavePreset]}\"";
                        instrumentLine += $" FdsModPreset=\"{Envelope.PresetNames[instrument.FdsModPreset]}\"";
                        if (instrument.FdsMasterVolume != 0) instrumentLine += $" FdsMasterVolume=\"{instrument.FdsMasterVolume}\"";
                        if (instrument.FdsModSpeed     != 0) instrumentLine += $" FdsModSpeed=\"{instrument.FdsModSpeed}\"";
                        if (instrument.FdsModDepth     != 0) instrumentLine += $" FdsModDepth=\"{instrument.FdsModDepth}\"";
                        if (instrument.FdsModDelay     != 0) instrumentLine += $" FdsModDelay=\"{instrument.FdsModDelay}\"";
                    }
                    else if (instrument.ExpansionType == Project.ExpansionN163)
                    {
                        instrumentLine += $" N163WavePreset=\"{Envelope.PresetNames[instrument.N163WavePreset]}\"";
                        instrumentLine += $" N163WaveSize=\"{instrument.N163WaveSize}\"";
                        instrumentLine += $" N163WavePos=\"{instrument.N163WavePos}\"";
                    }
                    else if (instrument.ExpansionType == Project.ExpansionVrc7)
                    {
                        instrumentLine += $" Vrc7Patch=\"{instrument.Vrc7Patch}\"";

                        if (instrument.Vrc7Patch == 0)
                        {
                            for (int i = 0; i < 8; i++)
                                instrumentLine += $" Vrc7Reg{i}=\"{instrument.Vrc7PatchRegs[i]}\"";
                        }
                    }
                }
                lines.Add(instrumentLine);

                for (int i = 0; i < Envelope.Count; i++)
                {
                    var env = instrument.Envelopes[i];
                    if (env != null && !env.IsEmpty)
                    {
                        var envelopeLine = $"\t\tEnvelope Type=\"{Envelope.EnvelopeShortNames[i]}\" Length=\"{env.Length}\"";

                        if (env.Loop     >= 0) envelopeLine += $" Loop=\"{env.Loop}\"";
                        if (env.Release  >= 0) envelopeLine += $" Release=\"{env.Release}\"";
                        if (env.Relative)      envelopeLine += $" Relative=\"{env.Relative}\"";

                        envelopeLine += $" Values=\"{String.Join(",", env.Values.Take(env.Length))}\"";

                        lines.Add(envelopeLine);
                    }
                }
            }

            // Songs
            foreach (var song in project.Songs)
            {
                var songStr = $"\tSong Name=\"{song.Name}\" Length=\"{song.Length}\" LoopPoint=\"{song.LoopPoint}\"";

                if (song.UsesFamiTrackerTempo)
                {
                    songStr += $" PatternLength=\"{song.PatternLength}\" BarLength=\"{song.BarLength}\" FamiTrackerTempo=\"{song.FamitrackerTempo}\" FamiTrackerSpeed=\"{song.FamitrackerSpeed}\"";
                }
                else
                {
                    songStr += $" PatternLength=\"{song.PatternLength / song.NoteLength}\" BarLength=\"{song.BarLength / song.NoteLength}\" NoteLength=\"{song.NoteLength}\" PalSkipFrames=\"{String.Join(",", song.PalSkipFrames)}\"";
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
                            var noteLength    = song.GetPatternNoteLength(i);
                            var barLength     = song.GetPatternBarLength(i);
                            var palSkipFrames = song.GetPatternPalSkipFrames(i);

                            lines.Add($"\t\tPatternCustomSettings Time=\"{i}\" Length=\"{patternLength / noteLength}\" NoteLength=\"{noteLength}\" BarLength=\"{barLength / noteLength}\" PalSkipFrames=\"{String.Join(",", palSkipFrames)}\"");
                        }
                    }
                }

                foreach (var channel in song.Channels)
                {
                    lines.Add($"\t\tChannel Type=\"{Channel.ChannelExportNames[channel.Type]}\"");

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
                                }
                                if (!note.HasAttack)     noteLine += $" Attack=\"{false.ToString()}\"";
                                if (note.HasVolume)      noteLine += $" Volume=\"{note.Volume}\"";
                                if (note.HasVibrato)     noteLine += $" VibratoSpeed=\"{note.VibratoSpeed}\" VibratoDepth=\"{note.VibratoDepth}\"";
                                if (note.HasSpeed)       noteLine += $" Speed=\"{note.Speed}\"";
                                if (note.HasFinePitch)   noteLine += $" FinePitch=\"{note.FinePitch}\"";
                                if (note.HasFdsModSpeed) noteLine += $" FdsModSpeed=\"{note.FdsModSpeed}\"";
                                if (note.HasFdsModDepth) noteLine += $" FdsModDepth=\"{note.FdsModDepth}\"";
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

        private string[] SplitStringKeepQuotes(string str)
        {
            return str.Split('"').Select((element, index) => index % 2 == 0
                                                        ? element.Split(new[] { ' ', '=' }, StringSplitOptions.RemoveEmptyEntries)
                                                        : new string[] { element })
                                  .SelectMany(element => element).ToArray();
        }

        private string SplitLine(string line, ref Dictionary<string, string> parameters)
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

        public Project Load(string filename)
        {
            try
            {
                var lines = File.ReadAllLines(filename);
                var parameters = new Dictionary<string, string>();
                var project = (Project)null;
                var instrument = (Instrument)null;
                var song = (Song)null;
                var channel = (Channel)null;
                var pattern = (Pattern)null;

                foreach (var line in lines)
                {
                    var cmd = SplitLine(line.Trim(), ref parameters);

                    switch (cmd)
                    {
                        case "Project":
                        {
                            project = new Project();
                            if (parameters.TryGetValue("Name", out var name)) project.Name = name;
                            if (parameters.TryGetValue("Author", out var author)) project.Author = author;
                            if (parameters.TryGetValue("Copyright", out var copyright)) project.Copyright = copyright;
                            if (parameters.TryGetValue("Expansion", out var expansion)) project.SetExpansionAudio(Array.IndexOf(Project.ExpansionShortNames, expansion));
                            if (parameters.TryGetValue("TempoMode", out var tempoMode)) project.TempoMode = Array.IndexOf(Project.TempoModeNames, tempoMode);
                            break;
                        }
                        case "DPCMSample":
                        {
                            var str = parameters["Data"];
                            var data = new byte[str.Length / 2];
                            for (int i = 0; i < data.Length; i++)
                                data[i] = Convert.ToByte(str.Substring(i * 2, 2), 16);
                            project.CreateDPCMSample(parameters["Name"], data);
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
                            instrument = project.CreateInstrument(parameters.TryGetValue("Expansion", out _) ? project.ExpansionAudio : Project.ExpansionNone, parameters["Name"]);

                            if (instrument.ExpansionType == Project.ExpansionFds)
                            {
                                if (parameters.TryGetValue("FdsWavePreset",   out var wavPresetStr))    instrument.FdsWavePreset   = (byte)Array.IndexOf(Envelope.PresetNames, wavPresetStr);
                                if (parameters.TryGetValue("FdsModPreset",    out var modPresetStr))    instrument.FdsWavePreset   = (byte)Array.IndexOf(Envelope.PresetNames, modPresetStr);
                                if (parameters.TryGetValue("FdsMasterVolume", out var masterVolumeStr)) instrument.FdsMasterVolume = byte.Parse(masterVolumeStr);
                                if (parameters.TryGetValue("FdsModSpeed",     out var fdsModSpeedStr))  instrument.FdsModSpeed     = ushort.Parse(fdsModSpeedStr);
                                if (parameters.TryGetValue("FdsModDepth",     out var fdsModDepthStr))  instrument.FdsModDepth     = byte.Parse(fdsModDepthStr);
                                if (parameters.TryGetValue("FdsModDelay",     out var fdsModDelayStr))  instrument.FdsModDelay     = byte.Parse(fdsModDelayStr);
                            }
                            else if (instrument.ExpansionType == Project.ExpansionN163)
                            {
                                 if (parameters.TryGetValue("N163WavePreset", out var wavPresetStr))    instrument.N163WavePreset = (byte)Array.IndexOf(Envelope.PresetNames, wavPresetStr);
                                 if (parameters.TryGetValue("N163WaveSize",   out var n163WavSizeStr))  instrument.N163WaveSize   = byte.Parse(n163WavSizeStr);
                                 if (parameters.TryGetValue("N163WavePos",    out var n163WavPosStr))   instrument.N163WavePos    = byte.Parse(n163WavPosStr);
                            }
                            else if (instrument.ExpansionType == Project.ExpansionVrc7)
                            {
                                if (parameters.TryGetValue("Vrc7Patch", out var vrc7PatchStr)) instrument.Vrc7Patch = byte.Parse(vrc7PatchStr);

                                if (instrument.Vrc7Patch == 0)
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
                        case "Envelope":
                        {
                            var env = instrument.Envelopes[Array.IndexOf(Envelope.EnvelopeShortNames, parameters["Type"])];
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
                            song.SetBarLength(int.Parse(parameters["BarLength"]));
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
                                song.SetBarLength(int.Parse(parameters["BarLength"]) * noteLength);
                                song.SetDefaultPatternLength(int.Parse(parameters["PatternLength"]) * noteLength);

                                var skipFrames = parameters["PalSkipFrames"].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                                song.PalSkipFrames[0] = int.Parse(skipFrames[0]);
                                song.PalSkipFrames[1] = int.Parse(skipFrames[1]);
                            }
                            break;
                        }
                        case "PatternCustomSettings":
                        {
                            if (project.UsesFamiTrackerTempo)
                            {
                                song.SetPatternCustomSettings(int.Parse(parameters["Time"]), int.Parse(parameters["Length"]));
                            }
                            else
                            {
                                var patternLength = int.Parse(parameters["Length"]);
                                var noteLength = int.Parse(parameters["NoteLength"]);
                                var barLength = int.Parse(parameters["BarLength"]);

                                var skipFramesStr = parameters["PalSkipFrames"].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                                var skipFrames = new int[2];
                                skipFrames[0] = int.Parse(skipFramesStr[0]);
                                skipFrames[1] = int.Parse(skipFramesStr[1]);
                                song.SetPatternCustomSettings(int.Parse(parameters["Time"]), patternLength * noteLength, noteLength, barLength * noteLength, skipFrames);
                            }
                            break;
                        }
                        case "Channel":
                        {
                            var channelType = Array.IndexOf(Channel.ChannelExportNames, parameters["Type"]);
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

                            if (parameters.TryGetValue("Value",        out var valueStr))     note.Value           = (byte)Note.FromFriendlyName(valueStr);
                            if (parameters.TryGetValue("Instrument",   out var instStr))      note.Instrument      = project.GetInstrument(instStr);
                            if (parameters.TryGetValue("Attack",       out var attackStr))    note.HasAttack       = bool.Parse(attackStr);
                            if (parameters.TryGetValue("Volume",       out var volumeStr))    note.Volume          = byte.Parse(volumeStr);
                            if (parameters.TryGetValue("VibratoSpeed", out var vibSpeedStr))  note.VibratoSpeed    = byte.Parse(vibSpeedStr);
                            if (parameters.TryGetValue("VibratoDepth", out var vibDepthStr))  note.VibratoDepth    = byte.Parse(vibDepthStr);
                            if (parameters.TryGetValue("FinePitch",    out var finePitchStr)) note.FinePitch       = sbyte.Parse(finePitchStr);
                            if (parameters.TryGetValue("SlideTarget",  out var slideStr))     note.SlideNoteTarget = (byte)Note.FromFriendlyName(valueStr);
                            if (parameters.TryGetValue("FdsModSpeed",  out var modSpeedStr))  note.FdsModSpeed     = ushort.Parse(modSpeedStr);
                            if (parameters.TryGetValue("FdsModDepth",  out var modDepthStr))  note.FdsModDepth     = byte.Parse(modDepthStr);

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
            catch
            {
                return null;
            }
        }
    }
}
