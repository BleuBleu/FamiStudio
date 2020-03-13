using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FamiStudio
{
    public static class FamistudioTextFile
    {
        public static bool Save(Project originalProject, string filename, int[] songIds)
        {
            var project = originalProject.DeepClone();
            project.RemoveAllSongsBut(songIds);

            var lines = new List<string>();

            string projectLine = "Project Version=\"1.5.0\"";

            if (project.Name      != "") projectLine += $" Name=\"{project.Name}\"";
            if (project.Author    != "") projectLine += $" Author=\"{project.Author}\"";
            if (project.Copyright != "") projectLine += $" Copyright=\"{project.Copyright}\"";
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
                    instrumentLine += $" Expansion=\"{Project.ExpansionShortNames[project.ExpansionAudio]}\"";
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
                lines.Add($"\tSong Name=\"{song.Name}\" Length=\"{song.Length}\" PatternLength=\"{song.DefaultPatternLength}\" BarLength=\"{song.BarLength}\" Tempo=\"{song.Tempo}\" Speed=\"{song.Speed}\" LoopPoint=\"{song.LoopPoint}\"");

                for (int i = 0; i < song.Length; i++)
                {
                    if (song.PatternHasCustomLength(i))
                        lines.Add($"\t\tPatternInstanceParams Time=\"{i}\" Length=\"{song.GetPatternLength(i)}\"");
                }

                foreach (var channel in song.Channels)
                {
                    lines.Add($"\t\tChannel Type=\"{Channel.ChannelExportNames[channel.Type]}\"");

                    foreach (var pattern in channel.Patterns)
                    {
                        lines.Add($"\t\t\tPattern Name=\"{pattern.Name}\"");

                        for (int n = 0; n < Pattern.MaxLength; n++)
                        {
                            var note = pattern.Notes[n];

                            if (!note.IsEmpty)
                            {
                                var noteLine = $"\t\t\t\tNote Time=\"{n}\"";

                                if (note.IsValid)
                                {
                                    noteLine += $" Value=\"{note.FriendlyName}\"";
                                    if (note.Instrument != null)
                                        noteLine += $" Instrument=\"{note.Instrument.Name}\"";
                                }
                                if (!note.HasAttack)   noteLine += $" Attack=\"{false.ToString()}\"";
                                if (note.HasVolume)    noteLine += $" Volume=\"{note.Volume}\"";
                                if (note.HasVibrato)   noteLine += $" VibratoSpeed=\"{note.VibratoSpeed}\" VibratoDepth=\"{note.VibratoDepth}\"";
                                if (note.HasSpeed)     noteLine += $" Speed=\"{note.Speed}\"";
                                if (note.HasFinePitch) noteLine += $" FinePitch=\"{note.FinePitch}\"";
                                if (note.IsMusical && note.IsSlideNote)
                                {
                                    // Add duration for convenience.
                                    var p = Array.IndexOf(channel.PatternInstances, pattern);
                                    var noteTable = NesApu.GetNoteTableForChannelType(channel.Type, false, project.ExpansionNumChannels);
                                    channel.ComputeSlideNoteParams(p, n, noteTable, out _, out _, out var duration, out _);

                                    // MATTT: PAL here (check of channeltype).
                                    noteLine += $" SlideTarget=\"{Note.GetFriendlyName(note.SlideNoteTarget)}\" FrameCountNTSC=\"{duration}\""; 
                                }

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

        public static Project Load(string filename)
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
                            song.SetDefaultPatternLength(int.Parse(parameters["PatternLength"]));
                            song.SetBarLength(int.Parse(parameters["BarLength"]));
                            song.SetLoopPoint(int.Parse(parameters["LoopPoint"]));
                            song.Tempo = int.Parse(parameters["Tempo"]);
                            song.Speed = int.Parse(parameters["Speed"]);
                            break;
                        }
                        case "PatternInstanceParams":
                        {
                            song.SetPatternLength(int.Parse(parameters["Time"]), int.Parse(parameters["Length"]));
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

                            if (parameters.TryGetValue("Value",        out var valueStr))     pattern.Notes[time].Value           = (byte)Note.FromFriendlyName(valueStr);
                            if (parameters.TryGetValue("Instrument",   out var instStr))      pattern.Notes[time].Instrument      = project.GetInstrument(instStr);
                            if (parameters.TryGetValue("Attack",       out var attackStr))    pattern.Notes[time].HasAttack       = bool.Parse(attackStr);
                            if (parameters.TryGetValue("Volume",       out var volumeStr))    pattern.Notes[time].Volume          = byte.Parse(volumeStr);
                            if (parameters.TryGetValue("VibratoSpeed", out var vibSpeedStr))  pattern.Notes[time].VibratoSpeed    = byte.Parse(vibSpeedStr);
                            if (parameters.TryGetValue("VibratoDepth", out var vibDepthStr))  pattern.Notes[time].VibratoDepth    = byte.Parse(vibDepthStr);
                            if (parameters.TryGetValue("FinePitch",    out var finePitchStr)) pattern.Notes[time].FinePitch       = sbyte.Parse(finePitchStr);
                            if (parameters.TryGetValue("SlideTarget",  out var slideStr))     pattern.Notes[time].SlideNoteTarget = (byte)Note.FromFriendlyName(valueStr);

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
