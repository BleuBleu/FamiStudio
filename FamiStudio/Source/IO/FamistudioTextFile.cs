using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Globalization;

namespace FamiStudio
{
    public class FamistudioTextFile
    {
        CultureInfo oldCulture;

        private void SetInvariantCulture()
        {
            oldCulture = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        }

        private void ResetCulture()
        {
            CultureInfo.CurrentCulture = oldCulture;
        }

        public bool Save(Project originalProject, string filename, int[] songIds, bool deleteUnusedData, FamiStudioTextFlags flags = FamiStudioTextFlags.None)
        {
            var project = originalProject.DeepClone();
            project.DeleteAllSongsBut(songIds, deleteUnusedData);

            SetInvariantCulture();

            var noVersion = flags.HasFlag(FamiStudioTextFlags.NoVersion);
            var noColors = flags.HasFlag(FamiStudioTextFlags.NoColors);

            var lines = new List<string>();

            var versionString = Utils.SplitVersionNumber(Platform.ApplicationVersion, out _);
            var projectLine = $"Project";
            if (!noVersion)
                projectLine += $"{GenerateAttribute("Version", versionString)}";
            projectLine += $"{GenerateAttribute("TempoMode", TempoType.Names[project.TempoMode])}";

            if (project.Name      != "")    projectLine += GenerateAttribute("Name", project.Name);
            if (project.Author    != "")    projectLine += GenerateAttribute("Author", project.Author);
            if (project.Copyright != "")    projectLine += GenerateAttribute("Copyright", project.Copyright);
            if (project.PalMode)            projectLine += GenerateAttribute("PAL", true);

            if (project.UsesAnyExpansionAudio)
            {
                var expansionStrings = new List<string>();

                for (int i = ExpansionType.Start; i <= ExpansionType.End; i++)
                {
                    if (project.UsesExpansionAudio(i))
                        expansionStrings.Add(ExpansionType.InternalNames[i]);
                }

                projectLine += GenerateAttribute("Expansions", string.Join(",", expansionStrings));

                if (project.UsesN163Expansion)
                    projectLine += GenerateAttribute("NumN163Channels", project.ExpansionNumN163Channels);

                for (var i = 0; i < project.ExpansionMixerSettings.Length; i++)
                {
                    var mixer = project.ExpansionMixerSettings[i];
                    if (mixer.Override)
                    {
                        var expName = ExpansionType.InternalNames[i];
                        projectLine += GenerateAttribute(expName + "VolumeDb", mixer.VolumeDb);
                        projectLine += GenerateAttribute(expName + "TrebleDb", mixer.TrebleDb);
                        projectLine += GenerateAttribute(expName + "TrebleRolloffHz", mixer.TrebleRolloffHz);
                    }
                }
            }

            lines.Add(projectLine);

            // DPCM samples
            foreach (var sample in project.Samples)
            {
                // We don't include any DPCM sample source data or processing data. We simply write the final
                // processed data. Including giant WAV files or asking other importers to implement all the 
                // processing options is unrealistic.
                if (sample.HasAnyProcessingOptions)
                {
                    if (sample.SourceDataIsWav)
                        Log.LogMessage(LogSeverity.Warning, $"Sample {sample.Name} has WAV data as source. Only the final processed DMC data will be exported.");
                    else
                        Log.LogMessage(LogSeverity.Warning, $"Sample {sample.Name} has processing option(s) enabled. Only the final processed DMC data will be exported.");
                }

                sample.PermanentlyApplyAllProcessing();

                Debug.Assert(!sample.HasAnyProcessingOptions);

                lines.Add($"\tDPCMSample{GenerateAttribute("Name", sample.Name)}{ConditionalGenerateAttribute("Color", sample.Color.ToHexString(), !noColors)}{GenerateAttributeIfNonEmpty("Folder", sample.FolderName)}{GenerateAttribute("Data", String.Join("", sample.ProcessedData.Select(x => $"{x:x2}")))}");
            }

            // Instruments
            foreach (var instrument in project.Instruments)
            {
                if (instrument.IsN163 && instrument.N163WavePreset == WavePresetType.Resample && instrument.N163ResampleWaveData != null ||
                    instrument.IsFds  && instrument.FdsWavePreset  == WavePresetType.Resample && instrument.FdsResampleWaveData  != null)
                {
                    Log.LogMessage(LogSeverity.Warning, $"Instrument {instrument.Name} is resampled from WAV data. Discarding WAV data.");
                    instrument.DeleteFdsResampleWavData();
                    instrument.DeleteN163ResampleWavData();
                }

                var instrumentLine = $"\tInstrument{GenerateAttribute("Name", instrument.Name)}{ConditionalGenerateAttribute("Color", instrument.Color.ToHexString(), !noColors)}{GenerateAttributeIfNonEmpty("Folder", instrument.FolderName)}";

                if (instrument.IsExpansionInstrument)
                {
                    instrumentLine += GenerateAttribute("Expansion", ExpansionType.InternalNames[instrument.Expansion]);

                    if (instrument.IsFds)
                    {
                        instrumentLine += GenerateAttribute("FdsWavePreset", WavePresetType.InternalNames[instrument.FdsWavePreset]);
                        instrumentLine += GenerateAttribute("FdsModPreset",  WavePresetType.InternalNames[instrument.FdsModPreset]);
                        if (instrument.FdsMasterVolume != 0) instrumentLine += GenerateAttribute("FdsMasterVolume", instrument.FdsMasterVolume);
                        if (instrument.FdsModSpeed     != 0) instrumentLine += GenerateAttribute("FdsModSpeed", instrument.FdsModSpeed);
                        if (instrument.FdsModDepth     != 0) instrumentLine += GenerateAttribute("FdsModDepth", instrument.FdsModDepth);
                        if (instrument.FdsModDelay     != 0) instrumentLine += GenerateAttribute("FdsModDelay", instrument.FdsModDelay);
                        if (instrument.FdsAutoMod)
                        {
                            instrumentLine += GenerateAttribute("FdsAutoMod", instrument.FdsAutoMod);
                            instrumentLine += GenerateAttribute("FdsAutoModDenom", instrument.FdsAutoModDenom);
                            instrumentLine += GenerateAttribute("FdsAutoModNumer", instrument.FdsAutoModNumer);
                        }
                    }
                    else if (instrument.IsN163)
                    {
                        instrumentLine += GenerateAttribute("N163WavePreset", WavePresetType.InternalNames[instrument.N163WavePreset]);
                        instrumentLine += GenerateAttribute("N163WaveSize", instrument.N163WaveSize);
                        instrumentLine += GenerateAttribute("N163WavePos", instrument.N163WavePos);
                        instrumentLine += GenerateAttribute("N163WaveCount", instrument.N163WaveCount);
                    }
                    else if (instrument.IsS5B)
                    {
                        instrumentLine += GenerateAttribute("S5BEnvelopeShape", instrument.S5BEnvelopeShape);
                        instrumentLine += GenerateAttribute("S5BEnvelopeAutoPitch", instrument.S5BEnvAutoPitch);
                        instrumentLine += GenerateAttribute("S5BEnvelopeAutoPitchOctave", instrument.S5BEnvAutoPitchOctave);
                        instrumentLine += GenerateAttribute("S5BEnvelopePitch", instrument.S5BEnvelopePitch);
                    }
                    else if (instrument.IsVrc6)
                    {
                        instrumentLine += GenerateAttribute("Vrc6SawMasterVolume", Vrc6SawMasterVolumeType.Names[instrument.Vrc6SawMasterVolume]);
                    }
                    else if (instrument.IsVrc7)
                    {
                        instrumentLine += GenerateAttribute("Vrc7Patch", instrument.Vrc7Patch);

                        if (instrument.Vrc7Patch == Vrc7InstrumentPatch.Custom)
                        {
                            for (int i = 0; i < 8; i++)
                                instrumentLine += GenerateAttribute($"Vrc7Reg{i}", instrument.Vrc7PatchRegs[i]);
                        }
                    }
                    else if (instrument.IsEpsm)
                    {
                        instrumentLine += GenerateAttribute("EpsmPatch", instrument.EpsmPatch);

                        if (instrument.EpsmPatch == EpsmInstrumentPatch.Custom)
                        {
                            for (int i = 0; i < 31; i++)
                                instrumentLine += GenerateAttribute($"EpsmReg{i}", instrument.EpsmPatchRegs[i]);
                        }

                        instrumentLine += GenerateAttribute("EPSMSquareEnvelopeShape", instrument.EPSMSquareEnvelopeShape);
                        instrumentLine += GenerateAttribute("EPSMSquareEnvelopeAutoPitch", instrument.EPSMSquareEnvAutoPitch);
                        instrumentLine += GenerateAttribute("EPSMSquareEnvelopeAutoPitchOctave", instrument.EPSMSquareEnvAutoPitchOctave);
                    }
                }

                lines.Add(instrumentLine);

                if (instrument.HasAnyMappedSamples)
                {
                    foreach (var kv in instrument.SamplesMapping)
                    {
                        var note = kv.Key;
                        var mapping = kv.Value;

                        if (mapping != null && mapping.Sample != null)
                        {
                            var mappingStr = $"\t\tDPCMMapping{GenerateAttribute("Note", Note.GetFriendlyName(kv.Key))}{GenerateAttribute("Sample", mapping.Sample.Name)}{GenerateAttribute("Pitch", mapping.Pitch)}{GenerateAttribute("Loop", mapping.Loop)}";

                            if (mapping.OverrideDmcInitialValue)
                                mappingStr += $"{GenerateAttribute("DmcInitialValue", mapping.DmcInitialValueDiv2)}";

                            lines.Add(mappingStr);
                        }
                    }
                }

                for (int i = 0; i < EnvelopeType.Count; i++)
                {
                    var env = instrument.Envelopes[i];
                    if (env != null && !env.IsEmpty(i))
                    {
                        var envelopeLine = $"\t\tEnvelope{GenerateAttribute("Type", EnvelopeType.InternalNames[i])}{GenerateAttribute("Length", env.Length)}";

                        if (env.Loop     >= 0) envelopeLine += GenerateAttribute("Loop", env.Loop);
                        if (env.Release  >= 0) envelopeLine += GenerateAttribute("Release", env.Release);
                        if (env.Relative)      envelopeLine += GenerateAttribute("Relative", env.Relative);

                        envelopeLine += GenerateAttribute("Values", String.Join(",", env.Values.Take(env.Length)));

                        lines.Add(envelopeLine);
                    }
                }
            }

            // Arpeggios
            foreach (var arpeggio in project.Arpeggios)
            {
                var env = arpeggio.Envelope;
                var arpeggioLine = $"\tArpeggio{GenerateAttribute("Name", arpeggio.Name)}{ConditionalGenerateAttribute("Color", arpeggio.Color.ToHexString(), !noColors)}{GenerateAttributeIfNonEmpty("Folder", arpeggio.FolderName)}{GenerateAttribute("Length", env.Length)}";
                if (env.Loop >= 0) arpeggioLine += GenerateAttribute("Loop", env.Loop);
                arpeggioLine += GenerateAttribute("Values", String.Join(",", env.Values.Take(env.Length)));
                lines.Add(arpeggioLine);
            }

            // Songs
            foreach (var song in project.Songs)
            {
                var songStr = $"\tSong{GenerateAttribute("Name", song.Name)}{ConditionalGenerateAttribute("Color", song.Color.ToHexString(), !noColors)}{GenerateAttributeIfNonEmpty("Folder", song.FolderName)}{GenerateAttribute("Length", song.Length)}{GenerateAttribute("LoopPoint", song.LoopPoint)}";

                if (song.UsesFamiTrackerTempo)
                {
                    songStr += $"{GenerateAttribute("PatternLength", song.PatternLength)}{GenerateAttribute("BeatLength", song.BeatLength)}{GenerateAttribute("FamiTrackerTempo", song.FamitrackerTempo)}{GenerateAttribute("FamiTrackerSpeed", song.FamitrackerSpeed)}";
                }
                else
                {
                    songStr += $"{GenerateAttribute("PatternLength", song.PatternLength / song.NoteLength)}{GenerateAttribute("BeatLength", song.BeatLength / song.NoteLength)}{GenerateAttribute("NoteLength", song.NoteLength)}{GenerateAttribute("Groove", string.Join("-", song.Groove))}{GenerateAttribute("GroovePaddingMode", GroovePaddingType.Names[song.GroovePaddingMode])}";
                }

                lines.Add(songStr);

                for (int i = 0; i < song.Length; i++)
                {
                    if (song.PatternHasCustomSettings(i))
                    {
                        var patternLength = song.GetPatternLength(i);

                        if (song.UsesFamiTrackerTempo)
                        {
                            lines.Add($"\t\tPatternCustomSettings{GenerateAttribute("Time", i)}{GenerateAttribute("Length", patternLength)}");
                        }
                        else
                        {
                            var noteLength = song.GetPatternNoteLength(i);
                            var beatLength = song.GetPatternBeatLength(i);
                            var groove     = song.GetPatternGroove(i);
                            var groovePaddingMode = song.GetPatternGroovePaddingMode(i);

                            lines.Add($"\t\tPatternCustomSettings{GenerateAttribute("Time", i)}{GenerateAttribute("Length", patternLength / noteLength)}{GenerateAttribute("NoteLength", noteLength)}{GenerateAttribute("Groove", string.Join("-", groove))}{GenerateAttribute("GroovePaddingMode", GroovePaddingType.Names[groovePaddingMode])}{GenerateAttribute("BeatLength", beatLength / noteLength)}");
                        }
                    }
                }

                foreach (var channel in song.Channels)
                {
                    lines.Add($"\t\tChannel{GenerateAttribute("Type", ChannelType.InternalNames[channel.Type])}");

                    foreach (var pattern in channel.Patterns)
                    {
                        lines.Add($"\t\t\tPattern{GenerateAttribute("Name", pattern.Name)}{ConditionalGenerateAttribute("Color", pattern.Color.ToHexString(), !noColors)}");

                        foreach (var kv in pattern.Notes)
                        {
                            var note = kv.Value;

                            if (!note.IsEmpty)
                            {
                                var noteLine = $"\t\t\t\tNote{GenerateAttribute("Time", kv.Key)}";

                                if (note.IsMusicalOrStop)
                                {
                                    noteLine += GenerateAttribute("Value", note.FriendlyName);

                                    if (note.IsMusical)
                                    {
                                        noteLine += GenerateAttribute("Duration", note.Duration);

                                        if (note.HasRelease)
                                            noteLine += GenerateAttribute("Release", note.Release);
                                        if (note.Instrument != null)
                                            noteLine += GenerateAttribute("Instrument", note.Instrument.Name);
                                        if (note.IsArpeggio)
                                            noteLine += GenerateAttribute("Arpeggio", note.Arpeggio.Name);
                                        if (note.IsSlideNote)
                                            noteLine += GenerateAttribute("SlideTarget", Note.GetFriendlyName(note.SlideNoteTarget));
                                    }
                                }

                                if (!note.HasAttack)        noteLine += GenerateAttribute("Attack", false);
                                if (note.HasVolume)         noteLine += GenerateAttribute("Volume", note.Volume);
                                if (note.HasVolumeSlide)    noteLine += GenerateAttribute("VolumeSlideTarget", note.VolumeSlideTarget);
                                if (note.HasVibrato)        noteLine += $"{GenerateAttribute("VibratoSpeed", note.VibratoSpeed)}{GenerateAttribute("VibratoDepth", note.VibratoDepth)}";
                                if (note.HasSpeed)          noteLine += GenerateAttribute("Speed", note.Speed);
                                if (note.HasFinePitch)      noteLine += GenerateAttribute("FinePitch", note.FinePitch);
                                if (note.HasFdsModSpeed)    noteLine += GenerateAttribute("FdsModSpeed", note.FdsModSpeed);
                                if (note.HasFdsModDepth)    noteLine += GenerateAttribute("FdsModDepth", note.FdsModDepth);
                                if (note.HasDutyCycle)      noteLine += GenerateAttribute("DutyCycle", note.DutyCycle);
                                if (note.HasNoteDelay)      noteLine += GenerateAttribute("NoteDelay", note.NoteDelay);
                                if (note.HasCutDelay)       noteLine += GenerateAttribute("CutDelay", note.CutDelay);
                                if (note.HasDeltaCounter)   noteLine += GenerateAttribute("DeltaCounter", note.DeltaCounter);
                                if (note.HasPhaseReset)     noteLine += GenerateAttribute("PhaseReset", note.PhaseReset);
                                if (note.HasEnvelopePeriod) noteLine += GenerateAttribute("EnvelopePeriod", note.EnvelopePeriod);

                                lines.Add(noteLine);
                            }
                        }
                    }

                    for (int p = 0; p < song.Length; p++)
                    {
                        var pattern = channel.PatternInstances[p];

                        if (pattern != null)
                            lines.Add($"\t\t\tPatternInstance{GenerateAttribute("Time", p)}{GenerateAttribute("Pattern", pattern.Name)}");
                    }
                }
            }

            File.WriteAllLines(filename, lines);

            ResetCulture();
            return true;
        }

        private static string GenerateAttribute(string key, object value)
        {
            return $" {key}=\"{value.ToString().Replace("\"", "\"\"")}\"";
        }

        private static string GenerateAttributeIfNonEmpty(string key, object value)
        {
            return !string.IsNullOrEmpty(value.ToString()) ? GenerateAttribute(key, value) : string.Empty;
        }

        private static string ConditionalGenerateAttribute(string key, object value, bool condition)
        {
            return condition ? GenerateAttribute(key, value) : string.Empty;
        }

        private static readonly Regex NameRegex = new Regex("^\\s*([^\"=\\s]+)\\s*(.*)\\s*$", RegexOptions.Compiled);

        private static readonly Regex AttributeRegex = new Regex("([^\"=\\s]+)\\s*=\\s*\"((?:\"\"|[^\"])+)\"", RegexOptions.Compiled);

        private static string SplitLine(string line, ref Dictionary<string, string> parameters)
        {
            parameters.Clear();

            var nameSeparated = NameRegex.Match(line);

            if (nameSeparated.Success)
            {
                var type = nameSeparated.Groups[1].Value;
                var attributes = nameSeparated.Groups[2].Value;

                // Ensure that everything following the type looks like an attribute.
                if (string.IsNullOrWhiteSpace(AttributeRegex.Replace(attributes, "")))
                {
                    foreach (Match match in AttributeRegex.Matches(attributes))
                    {
                        var key = match.Groups[1].Value;
                        var value = match.Groups[2].Value.Replace("\"\"", "\"");

                        parameters[key] = value;
                    }

                    return type;
                }
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
#if !DEBUG
            try
#endif
            {
                var lines = File.ReadAllLines(filename);
                var parameters = new Dictionary<string, string>();
                var project = (Project)null;
                var instrument = (Instrument)null;
                var arpeggio = (Arpeggio)null;
                var song = (Song)null;
                var channel = (Channel)null;
                var pattern = (Pattern)null;

                SetInvariantCulture();

                foreach (var line in lines)
                {
                    var cmd = SplitLine(line.Trim(), ref parameters);

                    switch (cmd)
                    {
                        case "Project":
                        {
                            var currentVersion = Utils.SplitVersionNumber(Platform.ApplicationVersion, out _);

                            project = new Project();
                            if (!parameters.TryGetValue("Version", out var version))
                                version = currentVersion;
                            if (parameters.TryGetValue("Name", out var name)) project.Name = name;
                            if (parameters.TryGetValue("Author", out var author)) project.Author = author;
                            if (parameters.TryGetValue("Copyright", out var copyright)) project.Copyright = copyright;
                            if (parameters.TryGetValue("TempoMode", out var tempoMode)) project.TempoMode = TempoType.GetValueForName(tempoMode);
                            if (parameters.TryGetValue("PAL", out var pal)) project.PalMode = bool.Parse(pal);
                            if (parameters.TryGetValue("Expansions", out var expansions))
                            {
                                var expansionMask = 0;
                                var expansionStrings = expansions.Split(',');

                                foreach (var s in expansionStrings)
                                {
                                    var exp = ExpansionType.GetValueForInternalName(s.Trim());
                                    expansionMask |= ExpansionType.GetMaskFromValue(exp);
                                }

                                var numN163Channels = 1;
                                if ((expansionMask & ExpansionType.N163Mask) != 0 && parameters.TryGetValue("NumN163Channels", out var numN163ChannelsStr))
                                    numN163Channels = int.Parse(numN163ChannelsStr);

                                project.SetExpansionAudioMask(expansionMask, numN163Channels);

                                for (var i = 0; i < project.ExpansionMixerSettings.Length; i++)
                                {
                                    var expName = ExpansionType.InternalNames[i];

                                    if (parameters.TryGetValue(expName + "VolumeDb", out var volumeDbStr) &&
                                        parameters.TryGetValue(expName + "TrebleDb", out var trebleDbStr) &&
                                        parameters.TryGetValue(expName + "TrebleRolloffHz", out var trebleRolloffHzStr))
                                    {
                                        project.ExpansionMixerSettings[i].Override = true;
                                        project.ExpansionMixerSettings[i].VolumeDb = float.Parse(volumeDbStr);
                                        project.ExpansionMixerSettings[i].TrebleDb = float.Parse(trebleDbStr);
                                        project.ExpansionMixerSettings[i].TrebleRolloffHz = int.Parse(trebleRolloffHzStr);
                                    }
                                }
                            }

                            if (!version.StartsWith(currentVersion.Substring(0, 3)))
                            {
                                Log.LogMessage(LogSeverity.Error, "File was created with an incompatible version of FamiStudio. The text format is only compatible with the current version.");
                                return null;
                            }
                            break;
                        }
                        case "DPCMSample":
                        {
                            var str = parameters["Data"];
                            var data = new byte[str.Length / 2];
                            for (int i = 0; i < data.Length; i++)
                                data[i] = Convert.ToByte(str.Substring(i * 2, 2), 16);
                            var sample = project.CreateDPCMSampleFromDmcData(parameters["Name"], data);
                            if (parameters.TryGetValue("Folder", out var folderName) && project.CreateFolder(FolderType.Sample, folderName) != null)
                                sample.FolderName = folderName;
                            if (parameters.TryGetValue("Color", out var hexColor))
                                sample.Color = Theme.EnforceThemeColor(Color.FromHexString(hexColor));
                            break;
                        }
                        case "Instrument":
                        {
                            var instrumentExp = ExpansionType.None;
                            if (parameters.TryGetValue("Expansion", out var instrumentExpStr))
                                instrumentExp = ExpansionType.GetValueForInternalName(instrumentExpStr);
                            instrument = project.CreateInstrument(instrumentExp, parameters["Name"]);
                            if (parameters.TryGetValue("Folder", out var folderName) && project.CreateFolder(FolderType.Instrument, folderName) != null)
                                instrument.FolderName = folderName;
                            if (parameters.TryGetValue("Color", out var hexColor))
                                instrument.Color = Theme.EnforceThemeColor(Color.FromHexString(hexColor));

                            if (instrument.IsFds)
                            {
                                if (parameters.TryGetValue("FdsWavePreset",   out var wavPresetStr))    instrument.FdsWavePreset   = (byte)WavePresetType.GetValueForInternalName(wavPresetStr);
                                if (parameters.TryGetValue("FdsModPreset",    out var modPresetStr))    instrument.FdsModPreset    = (byte)WavePresetType.GetValueForInternalName(modPresetStr);
                                if (parameters.TryGetValue("FdsMasterVolume", out var masterVolumeStr)) instrument.FdsMasterVolume = byte.Parse(masterVolumeStr);
                                if (parameters.TryGetValue("FdsModSpeed",     out var fdsModSpeedStr))  instrument.FdsModSpeed     = ushort.Parse(fdsModSpeedStr);
                                if (parameters.TryGetValue("FdsModDepth",     out var fdsModDepthStr))  instrument.FdsModDepth     = byte.Parse(fdsModDepthStr);
                                if (parameters.TryGetValue("FdsModDelay",     out var fdsModDelayStr))  instrument.FdsModDelay     = byte.Parse(fdsModDelayStr);
                                if (parameters.TryGetValue("FdsAutoMod",      out var fdsAutoModStr))   instrument.FdsAutoMod      = bool.Parse(fdsAutoModStr);
                                if (instrument.FdsAutoMod)
                                {
                                    if (parameters.TryGetValue("FdsAutoModDenom", out var fdsAutoModDenomStr)) instrument.FdsAutoModDenom = byte.Parse(fdsAutoModDenomStr);
                                    if (parameters.TryGetValue("FdsAutoModNumer", out var fdsAutoModNumerStr)) instrument.FdsAutoModNumer = byte.Parse(fdsAutoModNumerStr);
                                }
                            }
                            else if (instrument.IsN163)
                            {
                                 if (parameters.TryGetValue("N163WavePreset", out var wavPresetStr))    instrument.N163WavePreset = (byte)WavePresetType.GetValueForInternalName(wavPresetStr);
                                 if (parameters.TryGetValue("N163WaveSize",   out var n163WavSizeStr))  instrument.N163WaveSize   = byte.Parse(n163WavSizeStr);
                                 if (parameters.TryGetValue("N163WavePos",    out var n163WavPosStr))   instrument.N163WavePos    = byte.Parse(n163WavPosStr);
                                 if (parameters.TryGetValue("N163WaveCount",  out var wavCountStr))     instrument.N163WaveCount  = byte.Parse(wavCountStr);
                            }
                            else if (instrument.IsVrc6)
                            {
                                 if (parameters.TryGetValue("Vrc6SawMasterVolume", out var vrc6SawVolumeStr)) instrument.Vrc6SawMasterVolume = (byte)Vrc6SawMasterVolumeType.GetValueForName(vrc6SawVolumeStr);
                            }
                            else if (instrument.IsS5B)
                            {
                                 if (parameters.TryGetValue("S5BEnvelopeShape",           out var s5bEnvShapeStr))     instrument.S5BEnvelopeShape      = byte.Parse(s5bEnvShapeStr);
                                 if (parameters.TryGetValue("S5BEnvelopeAutoPitch",       out var s5bEnvAutoPitchStr)) instrument.S5BEnvAutoPitch       = bool.Parse(s5bEnvAutoPitchStr);
                                 if (parameters.TryGetValue("S5BEnvelopeAutoPitchOctave", out var s5bEnvAutoOctStr))   instrument.S5BEnvAutoPitchOctave = sbyte.Parse(s5bEnvAutoOctStr);
                                 if (parameters.TryGetValue("S5BEnvelopePitch",           out var s5bEnvPitchStr))     instrument.S5BEnvelopePitch      = ushort.Parse(s5bEnvPitchStr);
                            }
                            else if (instrument.IsVrc7)
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
                            else if (instrument.IsEpsm)
                            {
                                if (parameters.TryGetValue("EpsmPatch", out var epsmPatchStr)) instrument.EpsmPatch = byte.Parse(epsmPatchStr);

                                if (instrument.EpsmPatch == EpsmInstrumentPatch.Custom)
                                {
                                    for (int i = 0; i < 31; i++)
                                    {
                                        if (parameters.TryGetValue($"EpsmReg{i}", out var regStr))
                                           instrument.EpsmPatchRegs[i] = byte.Parse(regStr);
                                    }
                                }

                                if (parameters.TryGetValue("EPSMSquareEnvelopeShape",           out var epsmEnvShapeStr))     instrument.EPSMSquareEnvelopeShape      = byte.Parse(epsmEnvShapeStr);
                                if (parameters.TryGetValue("EPSMSquareEnvelopeAutoPitch",       out var epsmEnvAutoPitchStr)) instrument.EPSMSquareEnvAutoPitch       = bool.Parse(epsmEnvAutoPitchStr);
                                if (parameters.TryGetValue("EPSMSquareEnvelopeAutoPitchOctave", out var epsmEnvAutoOctStr))   instrument.EPSMSquareEnvAutoPitchOctave = sbyte.Parse(epsmEnvAutoOctStr);
                            }
                            else if (instrument.IsRegular)
                            {

                            }

                            break;
                        }
                        case "DPCMMapping":
                        {
                            if (instrument != null && instrument.IsRegular)
                            {
                                var pitch = 15;
                                var loop = false;
                                if (parameters.TryGetValue("Pitch", out var pitchStr)) pitch = int.Parse(pitchStr);
                                if (parameters.TryGetValue("Loop", out var loopStr)) loop = bool.Parse(loopStr);
                                var mapping = instrument.MapDPCMSample(Note.FromFriendlyName(parameters["Note"]), project.GetSample(parameters["Sample"]), pitch, loop);
                                if (parameters.TryGetValue("DmcInitialValue", out var dmcInitialStr))
                                {
                                    mapping.OverrideDmcInitialValue = true;
                                    mapping.DmcInitialValueDiv2 = byte.Parse(dmcInitialStr);
                                }
                            }
                            break;
                        }

                        case "Arpeggio":
                        {
                            arpeggio = project.CreateArpeggio(parameters["Name"]);
                            arpeggio.Envelope.Length = int.Parse(parameters["Length"]);
                            
                            if (parameters.TryGetValue("Folder", out var folderName) && project.CreateFolder(FolderType.Arpeggio, folderName) != null)
                                arpeggio.FolderName = folderName;
                            if (parameters.TryGetValue("Color", out var hexColor))
                                arpeggio.Color = Theme.EnforceThemeColor(Color.FromHexString(hexColor));
                            arpeggio.Envelope.Loop = parameters.TryGetValue("Loop", out var loopStr) ? int.Parse(loopStr) : -1;

                            var values = parameters["Values"].Split(',');
                            for (int j = 0; j < values.Length; j++)
                                arpeggio.Envelope.Values[j] = sbyte.Parse(values[j]);

                            break;
                        }
                        case "Envelope":
                        {
                            var env = instrument.Envelopes[EnvelopeType.GetValueForInternalName(parameters["Type"])];
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
                            song.SetBeatLength(int.Parse(parameters["BeatLength"]));
                            song.SetLoopPoint(int.Parse(parameters["LoopPoint"]));

                            if (parameters.TryGetValue("Folder", out var folderName) && project.CreateFolder(FolderType.Song, folderName) != null)
                                song.FolderName = folderName;
                            if (parameters.TryGetValue("Color", out var hexColor))
                                song.Color = Theme.EnforceThemeColor(Color.FromHexString(hexColor));

                            if (song.UsesFamiTrackerTempo)
                            {
                                song.SetDefaultPatternLength(int.Parse(parameters["PatternLength"]));
                                song.FamitrackerTempo = int.Parse(parameters["FamiTrackerTempo"]);
                                song.FamitrackerSpeed = int.Parse(parameters["FamiTrackerSpeed"]);
                            }
                            else
                            {
                                var noteLength = int.Parse(parameters["NoteLength"]);

                                var groove = parameters["Groove"].Split('-').Select(Int32.Parse).ToArray();
                                var groovePaddingMode = GroovePaddingType.GetValueForName(parameters["GroovePaddingMode"]);

                                if (!FamiStudioTempoUtils.ValidateGroove(groove) || Utils.Min(groove) != noteLength)
                                {
                                    Log.LogMessage(LogSeverity.Error, "Invalid tempo settings.");
                                    return null;
                                }

                                song.ChangeFamiStudioTempoGroove(groove, false);
                                song.SetBeatLength(song.BeatLength * noteLength);
                                song.SetDefaultPatternLength(int.Parse(parameters["PatternLength"]) * noteLength);
                                song.SetGroovePaddingMode(groovePaddingMode);
                            }
                            break;
                        }
                        case "PatternCustomSettings":
                        {
                            if (project.UsesFamiTrackerTempo)
                            {
                                var beatLength = song.BeatLength;
                                if (parameters.TryGetValue("BeatLength", out var beatLengthStr))
                                    beatLength = int.Parse(beatLengthStr);

                                song.SetPatternCustomSettings(int.Parse(parameters["Time"]), int.Parse(parameters["Length"]), beatLength);
                            }
                            else
                            {
                                var patternLength = int.Parse(parameters["Length"]);
                                var noteLength = int.Parse(parameters["NoteLength"]);
                                var beatLength = int.Parse(parameters["BeatLength"]);

                                var groove = parameters["Groove"].Split('-').Select(Int32.Parse).ToArray();
                                var groovePaddingMode = GroovePaddingType.GetValueForName(parameters["GroovePaddingMode"]);

                                if (!FamiStudioTempoUtils.ValidateGroove(groove) || Utils.Min(groove) != noteLength)
                                {
                                    Log.LogMessage(LogSeverity.Error, "Invalid tempo settings.");
                                    return null;
                                }

                                song.SetPatternCustomSettings(int.Parse(parameters["Time"]), patternLength * noteLength, beatLength * noteLength, groove, groovePaddingMode);
                            }
                            break;
                        }
                        case "Channel":
                        {
                            var channelType = ChannelType.GetValueForInternalName(parameters["Type"]);
                            channel = song.GetChannelByType(channelType);
                            break;
                        }
                        case "Pattern":
                        {
                            pattern = channel.CreatePattern(parameters["Name"]);
                            if (parameters.TryGetValue("Color", out var hexColor))
                                pattern.Color = Theme.EnforceThemeColor(Color.FromHexString(hexColor));
                            break;
                        }
                        case "Note":
                        {
                            var time = int.Parse(parameters["Time"]);
                            var note = pattern.GetOrCreateNoteAt(time);

                            if (parameters.TryGetValue("Value", out var valueStr))
                                note.Value = (byte)Note.FromFriendlyName(valueStr);
                            if (note.IsMusical && parameters.TryGetValue("Duration", out var durationStr))
                                note.Duration = int.Parse(durationStr);
                            else if (note.IsStop)
                                note.Duration = 1;
                            if (note.IsMusical && parameters.TryGetValue("Release", out var releaseStr))
                                note.Release = int.Parse(releaseStr);
                            if (note.IsMusical && parameters.TryGetValue("Instrument", out var instStr) && channel.SupportsInstrument(project.GetInstrument(instStr)))
                                note.Instrument = project.GetInstrument(instStr);
                            if (note.IsMusical && parameters.TryGetValue("Arpeggio", out var arpStr) && channel.SupportsArpeggios)
                                note.Arpeggio = project.GetArpeggio(arpStr);
                            if (note.IsMusical && parameters.TryGetValue("SlideTarget", out var slideStr) && channel.SupportsSlideNotes)
                                note.SlideNoteTarget = (byte)Note.FromFriendlyName(slideStr);
                            if (note.IsMusical && parameters.TryGetValue("Attack", out var attackStr))
                                note.HasAttack = bool.Parse(attackStr);

                            if (parameters.TryGetValue("Volume", out var volumeStr) && channel.SupportsEffect(Note.EffectVolume))
                                note.Volume = byte.Parse(volumeStr);
                            if (parameters.TryGetValue("VolumeSlideTarget", out var volumeSlideStr) && channel.SupportsEffect(Note.EffectVolumeSlide))
                                note.VolumeSlideTarget = byte.Parse(volumeSlideStr);
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
                            if (parameters.TryGetValue("DeltaCounter", out var deltaCounterStr) && channel.SupportsEffect(Note.EffectDeltaCounter))
                                note.DeltaCounter = byte.Parse(deltaCounterStr);
                            if (parameters.TryGetValue("PhaseReset", out var phaseResetStr) && channel.SupportsEffect(Note.EffectPhaseReset))
                                note.PhaseReset = byte.Parse(phaseResetStr);
                            if (parameters.TryGetValue("EnvelopePeriod", out var envPeriodStr) && channel.SupportsEffect(Note.EffectEnvelopePeriod))
                                note.EnvelopePeriod = ushort.Parse(envPeriodStr);

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

                // Post-load
                foreach (var inst in project.Instruments)
                {
                    inst.PerformPostLoadActions();
                }

                project.AutoSortSongs = false;
                project.ConditionalSortEverything();
                project.ValidateIntegrity();

                ResetCulture();

                return project;
            }
#if !DEBUG
            catch (Exception e)
            {
                Log.LogMessage(LogSeverity.Error, "Please contact the developer on GitHub!");
                Log.LogMessage(LogSeverity.Error, e.Message);
                Log.LogMessage(LogSeverity.Error, e.StackTrace);
                ResetCulture();
                return null;
            }
#endif
        }
    }

    [Flags]
    public enum FamiStudioTextFlags
    {
        None = 0,
        NoVersion = 1,
        NoColors = 2
    }
}
