using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace FamiStudio
{
    class CommandLineInterface : ILogOutput
    {
        private string[] args;
        private Project project;

        public bool HasAnythingToDo => HasOption("?") || HasOption("help") || args.Length >= 3;

        public CommandLineInterface(string[] args)
        {
            this.args = args;
        }

        private bool HasOption(string name)
        {
            foreach (var a in args)
            {
                if (a.Length >= 2 && a[0] == '-' && a.Substring(1).ToLower() == name)
                    return true;
            }

            return false;
        }

        private string ParseOption(string name, string defaultValue)
        {
            foreach (var a in args)
            {
                if (a.Length >= 2 && a[0] == '-' && a.Substring(1).ToLower().StartsWith(name))
                {
                    var colonIdx = a.LastIndexOf(':');
                    if (colonIdx >= 0)
                        return a.Substring(colonIdx + 1);
                    break;
                }
            }

            return defaultValue;
        }

        private int ParseOption(string name, int defaultValue, bool hex = false)
        {
            foreach (var a in args)
            {
                if (a.Length >= 2 && a[0] == '-' && a.Substring(1).ToLower().StartsWith(name))
                {
                    var colonIdx = a.LastIndexOf(':');
                    if (colonIdx >= 0)
                    {
                        if (hex)
                            return Convert.ToInt32(a.Substring(colonIdx + 1), 16);
                        else
                            return Convert.ToInt32(a.Substring(colonIdx + 1));
                    }
                    break;
                }
            }

            return defaultValue;
        }

        private int[] ParseOption(string name, int[] defaultValue)
        {
            foreach (var a in args)
            {
                if (a.Length >= 2 && a[0] == '-' && a.Substring(1).ToLower().StartsWith(name))
                {
                    var colonIdx = a.LastIndexOf(':');
                    if (colonIdx >= 0)
                    {
                        var splits = a.Substring(colonIdx + 1).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        var vals = new int[splits.Length];
                        for (int i = 0; i < splits.Length; i++)
                            vals[i] = Convert.ToInt32(splits[i]);
                        return vals;
                    }
                    break;
                }
            }

            return defaultValue;
        }

        private void InitializeConsole()
        {
            Platform.InitializeConsole();
            Console.WriteLine($"");
        }

        private void ShutdownConsole()
        {
            Platform.ShutdownConsole();
        }

        private void DisplayHelp()
        {
            var version = Utils.SplitVersionNumber(Platform.ApplicationVersion, out _);

            InitializeConsole();
            Console.WriteLine($"FamiStudio {version} Command-Line Usage");
            Console.WriteLine($"");
            Console.WriteLine($"Usage:");
            Console.WriteLine($"  FamiStudio <input> <command> <output> [-options]");
            Console.WriteLine($"");
            Console.WriteLine($"Examples:");
            Console.WriteLine($"  FamiStudio music.fms wav-export music.wav -export-songs:2 -wav-export-rate:48000");
            Console.WriteLine($"  FamiStudio music.fms famitracker-txt-export music.txt -export-songs:0,1,2");
            Console.WriteLine($"  FamiStudio music.fms famitone2-asm-export music.s -famitone2-format:ca65");
            Console.WriteLine($"");
            Console.WriteLine($"Supported input formats:");
            Console.WriteLine($"  FamiStudio project (*.fms)");
            Console.WriteLine($"  FamiStudio text file (*.txt)");
            Console.WriteLine($"  FamiTracker 0.4.6 file (*.ftm)");
            Console.WriteLine($"  FamiTracker 0.4.6 text file (*.txt)");
            Console.WriteLine($"  Nintendo Sound Format (*.nsf, *.nsfe)");
            Console.WriteLine($"");
            Console.WriteLine($"Supported commands and corresponding output format(s):");
            Console.WriteLine($"  wav-export : Export to a WAV file (*.wav).");
            Console.WriteLine($"  mp3-export : Export to a WAV file (*.mp3).");
            Console.WriteLine($"  ogg-export : Export to a OGG file (*.ogg).");
            Console.WriteLine($"  nsf-export : Export to a NSF file (*.nsf).");
            Console.WriteLine($"  rom-export : Export to a NES ROM file (.nes).");
            Console.WriteLine($"  fds-export : Export to a FDS disk file (.fds).");
            Console.WriteLine($"  famitracker-txt-export : Export to a FamiTracker text file (*.txt).");
            Console.WriteLine($"  famistudio-txt-export : Export to a FamiStudio text file (*.txt).");
            Console.WriteLine($"  famistudio-asm-export : Export to FamiStudio sound engine music assembly file(s) (*.s, *.asm).");
            Console.WriteLine($"  famistudio-asm-sfx-export : Export to FamiStudio sound engine sound effect assembly file(s) (*.s, *.asm).");
            Console.WriteLine($"  famitone2-asm-export : Export to FamiTone2 music assembly file(s) (*.s, *.asm).");
            Console.WriteLine($"  famitone2-asm-sfx-export : Export to FamiTone2 sound effect assembly file(s) (*.s, *.asm).");
            Console.WriteLine($"");
            Console.WriteLine($"General options:");
            Console.WriteLine($"  -export-songs:<songs> : Comma-seperated zero-based indices of the songs to export (default:all).");
            Console.WriteLine($"");
            Console.WriteLine($"NSF import specific options");
            Console.WriteLine($"  -nsf-import-song:<song> : Zero-based index of the song to import (default:0).");
            Console.WriteLine($"  -nsf-import-duration:<duration> : Duration, in sec, to record from the NSF (default:120).");
            Console.WriteLine($"  -nsf-import-pattern-length:<length> : Pattern length to split the NSF into (default:256).");
            Console.WriteLine($"  -nsf-import-start-frame:<frame> : Frame to skips before starting the NSF capture (default:0).");
            Console.WriteLine($"  -nsf-import-reverse-dpcm : Reverse bits of DPCM samples (default:disabled).");
            Console.WriteLine($"  -nsf-import-preserve-padding : Preserve 1-byte of padding after DPCM samples (default:disabled).");
            Console.WriteLine($"");
            Console.WriteLine($"WAV export specific options");
            Console.WriteLine($"  -wav-export-rate:<rate> : Sample rate of the exported wave : 11025, 22050, 44100 or 48000 (default:44100).");
            Console.WriteLine($"  -wav-export-duration:<duration> : Duration in second, 0 plays song once and stop (default:0).");
            Console.WriteLine($"  -wav-export-loop:<count> : Number of times to play the song (default:1).");
            Console.WriteLine($"  -wav-export-channels:<mask> : Channel mask in hexadecimal, bit zero in channel 0 and so on (default:ff).");
            Console.WriteLine($"  -wav-export-separate-channels : Export each channels to separate file (default:off).");
            Console.WriteLine($"  -wav-export-separate-intro : Export the intro of the song seperately from the looping section (default:off).");
            Console.WriteLine($"");
            Console.WriteLine($"MP3 export specific options");
            Console.WriteLine($"  -mp3-export-rate:<rate> : Sample rate of the exported mp3 : 44100 or 48000 (default:44100).");
            Console.WriteLine($"  -mp3-export-bitrate:<rate> : Bitrate of the exported mp3 : 96, 112, 128, 160, 192, 224 or 256 (default:192).");
            Console.WriteLine($"  -mp3-export-duration:<duration> : Duration in second, 0 plays song once and stop (default:0).");
            Console.WriteLine($"  -mp3-export-loop:<count> : Number of times to play the song (default:1).");
            Console.WriteLine($"  -mp3-export-channels:<mask> : Channel mask in hexadecimal, bit zero in channel 0 and so on (default:ff).");
            Console.WriteLine($"  -mp3-export-separate-channels : Export each channels to separate file (default:off).");
            Console.WriteLine($"  -mp3-export-separate-intro : Export the intro of the song seperately from the looping section (default:off).");
            Console.WriteLine($"");
            Console.WriteLine($"OGG export specific options");
            Console.WriteLine($"  -ogg-export-rate:<rate> : Sample rate of the exported mp3 : 44100 or 48000 (default:44100).");
            Console.WriteLine($"  -ogg-export-bitrate:<rate> : Bitrate of the exported mp3 : 96, 112, 128, 160, 192, 224 or 256 (default:192).");
            Console.WriteLine($"  -ogg-export-duration:<duration> : Duration in second, 0 plays song once and stop (default:0).");
            Console.WriteLine($"  -ogg-export-loop:<count> : Number of times to play the song (default:1).");
            Console.WriteLine($"  -ogg-export-channels:<mask> : Channel mask in hexadecimal, bit zero in channel 0 and so on (default:ff).");
            Console.WriteLine($"  -ogg-export-separate-channels : Export each channels to separate file (default:off).");
            Console.WriteLine($"  -ogg-export-separate-intro : Export the intro of the song seperately from the looping section (default:off).");
            Console.WriteLine($"");
            Console.WriteLine($"NSF export specific options");
            Console.WriteLine($"  -nsf-export-mode:<mode> : Target machine: ntsc or pal (default:project mode).");
            Console.WriteLine($"  -nsf-nsfe : Use NSFe format (default:off).");
            Console.WriteLine($"");
            Console.WriteLine($"ROM export specific options");
            Console.WriteLine($"  -rom-export-mode:<mode> : Target machine: ntsc, pal or dual (default:project mode).");
            Console.WriteLine($"");
            Console.WriteLine($"FamiStudio text export specific options");
            Console.WriteLine($"  -famistudio-txt-cleanup : Cleanup unused data on export (default:disabled).");
            Console.WriteLine($"");
            Console.WriteLine($"FamiStudio sound engine export specific options");
            Console.WriteLine($"  -famistudio-asm-format:<format> : Assembly format to export to : nesasm, ca65 or asm6 (default:nesasm).");
            Console.WriteLine($"  -famistudio-asm-seperate-files : Export songs to individual files, output filename is the output path (default:disabled).");
            Console.WriteLine($"  -famistudio-asm-seperate-song-pattern:<pattern> : Name pattern to use when exporting songs to seperate files (default:{{project}}_{{song}}).");
            Console.WriteLine($"  -famistudio-asm-seperate-dmc-pattern:<pattern> : DMC filename pattern to use when exporting songs to seperate files (default:{{project}}).");
            Console.WriteLine($"  -famistudio-asm-generate-list : Generate song list include file along with music data (default:disabled).");
            Console.WriteLine($"  -famistudio-asm-force-dpcm-bankswitch : Forces exporter to assume DPCM bankswitching is used. (default:disabled).");
            Console.WriteLine($"  -famistudio-asm-sfx-mode:<mode> : Target machine for SFX : ntsc, pal or dual (default:project mode).");
            Console.WriteLine($"  -famistudio-asm-sfx-generate-list : Generate sfx list include file along with SFX data (default:disabled).");
            Console.WriteLine($"");
            Console.WriteLine($"FamiTone2 export specific options");
            Console.WriteLine($"  -famitone2-asm-format:<format> : Assembly format to export to : nesasm, ca65 or asm6 (default:nesasm).");
            Console.WriteLine($"  -famitone2-asm-seperate-files : Export songs to individual files, output filename is the output path (default:disabled).");
            Console.WriteLine($"  -famitone2-asm-seperate-song-pattern:<pattern> : Name pattern to use when exporting songs to seperate files (default:{{project}}_{{song}}).");
            Console.WriteLine($"  -famitone2-asm-seperate-dmc-pattern:<pattern> : DMC filename pattern to use when exporting songs to seperate files (default:{{project}}).");
            Console.WriteLine($"  -famitone2-asm-generate-list : Generate song list include file along with music data (default:disabled).");
            Console.WriteLine($"  -famitone2-asm-sfx-mode:<mode> : Target machine for SFX : ntsc, pal or dual (default:project mode).");
            Console.WriteLine($"  -famitone2-asm-sfx-generate-list : Generate sfx list include file along with SFX data (default:disabled).");
            Console.WriteLine($"");
            ShutdownConsole();
        }

        private bool OpenProject()
        {
            var filename = args[0];

            if (filename.ToLower().EndsWith("fms"))
            {
                project = new ProjectFile().Load(filename);
            }
            else if (filename.ToLower().EndsWith("ftm"))
            {
                project = new FamitrackerBinaryFile().Load(filename);
            }
            else if (filename.ToLower().EndsWith("txt"))
            {
                if (FamistudioTextFile.LooksLikeFamiStudioText(filename))
                    project = new FamistudioTextFile().Load(filename);
                else
                    project = new FamitrackerTextFile().Load(filename);
            }
            else if (filename.ToLower().EndsWith("nsf") || filename.ToLower().EndsWith("nsfe"))
            {
                var songIndex   = ParseOption("nsf-import-song", 0);
                var duration    = ParseOption("nsf-import-duration", 120);
                var patternLen  = ParseOption("nsf-import-pattern-length", 256);
                var startFrame  = ParseOption("nsf-import-start-frame", 0);
                var reverseDpcm = HasOption("nsf-import-reverse-dpcm");
                var preservePad = HasOption("nsf-import-preserve-padding");
                
                project = new NsfFile().Load(filename, songIndex, duration, patternLen, startFrame, true, reverseDpcm, preservePad);
            }

            if (project == null)
            {
                Console.WriteLine($"Error opening input file {filename}.");
                return false;
            }

            FamiStudio.StaticProject = project;

            return true;
        }

        private bool ValidateExtension(string filename, string expectedExtension)
        {
            var extension = Path.GetExtension(filename.ToLower().Trim());

            if (extension != expectedExtension)
            {
                Console.WriteLine($"Invalid extension for output file, expected {expectedExtension}, got {extension}. Use -help or -? for help.");
                return false;
            }

            return true;
        }

        private Song GetProjectSong(int index)
        {
            if (index >= 0 && index < project.Songs.Count)
            {
                return project.Songs[index];
            }
            else
            {
                Console.WriteLine($"Invalid song index {index}, project has only {project.Songs.Count} songs.");
                return null;
            }
        }

        private int[] GetExportSongIds()
        {
            var songIndices = ParseOption("export-song", (int[])null);
            var songIds = (int[])null;

            if (songIndices == null)
            {
                songIds = new int[project.Songs.Count];
                for (int i = 0; i < project.Songs.Count; i++)
                    songIds[i] = project.Songs[i].Id;
            }
            else
            {
                songIds = new int[songIndices.Length];
                for (int i = 0; i < songIndices.Length; i++)
                {
                    var song = GetProjectSong(songIndices[i]);
                    if (song == null)
                        return null;
                    songIds[i] = song.Id;
                }
            }

            return songIds;
        }

        private void AudioExport(string filename, int format)
        {
            var extension = format == AudioFormatType.Mp3 ? "mp3" : (format == AudioFormatType.Vorbis ? "ogg" : "wav");

            if (!ValidateExtension(filename, "." + extension))
                return;

            var songIndex  = ParseOption("export-song", 0);
            var sampleRate = ParseOption($"{extension}-export-rate", 44100);
            var loopCount  = ParseOption($"{extension}-export-loop", 1);
            var duration   = ParseOption($"{extension}-export-duration", 0);
            var mask       = ParseOption($"{extension}-export-channels", 0xffffff, true);
            var separate   = HasOption($"{extension}-export-separate-channels");
            var intro      = HasOption($"{extension}-export-separate-intro");
            var bitrate    = ParseOption($"{extension}-export-bitrate", 192);
            var song       = GetProjectSong(songIndex);

            if (duration > 0)
                loopCount = -1;
            else
                loopCount = Math.Max(1, loopCount);

            // TODO : Add a stereo flag on the command line.
            var stereo = project.OutputsStereoAudio;
            var pan = (float[])null;

            if (stereo)
            {
                var channelCount = project.GetActiveChannelCount(); 
                pan = new float[channelCount];
                for (int i = 0; i < channelCount; i++)
                    pan[i] = 0.5f;
            }

            if (song != null)
            {
                AudioExportUtils.Save(song, filename, sampleRate, loopCount, duration, mask, separate, intro, stereo, pan, 0, true,
                     (samples, samplesChannels, fn) =>
                     {
                         switch (format)
                         {
                             case AudioFormatType.Mp3:
                                 Mp3File.Save(samples, fn, sampleRate, bitrate, samplesChannels);
                                 break;
                             case AudioFormatType.Wav:
                                 WaveFile.Save(samples, fn, sampleRate, samplesChannels);
                                 break;
                             case AudioFormatType.Vorbis:
                                 VorbisFile.Save(samples, fn, sampleRate, bitrate, samplesChannels);
                                 break;
                         }
                     });
            }
        }

        private void NsfExport(string filename)
        {
            if (!ValidateExtension(filename, ".nsf"))
                return;

            var machineString = ParseOption("nsf-export-mode", project.PalMode ? "pal" : "ntsc");
            var nsfe = HasOption($"nsf-nsfe");
            var machine = project.PalMode ? MachineType.PAL : MachineType.NTSC;

            switch (machineString.ToLower())
            {
                case "pal"  : machine = MachineType.PAL;  break;
                case "dual" : machine = MachineType.Dual; break;
                case "ntsc" : machine = MachineType.NTSC; break;
            }

            if (project.UsesAnyExpansionAudio)
                machine = MachineType.NTSC;

            var exportSongIds = GetExportSongIds();
            if (exportSongIds != null)
            {
                new NsfFile().Save(
                    project,
                    FamiToneKernel.FamiStudio,
                    filename,
                    exportSongIds,
                    project.Name,
                    project.Author,
                    project.Copyright,
                    machine,
                    nsfe);
            }
        }

        private void RomExport(string filename)
        {
            if (!ValidateExtension(filename, ".nes"))
                return;

            var exportSongIds = GetExportSongIds();
            if (exportSongIds != null)
            {
                if (exportSongIds.Length > RomFile.RomMaxSongs)
                {
                    Console.WriteLine($"There is currently a hard limit of {RomFile.RomMaxSongs} songs for NES ROM export.");
                    return;
                }

                var machineString = ParseOption("nsf-export-mode", project.PalMode ? "pal" : "ntsc");
                var pal = project.PalMode;

                switch (machineString.ToLower())
                {
                    case "pal"  : pal = true;  break;
                    case "ntsc" : pal = false; break;
                }

                if (project.UsesAnyExpansionAudio)
                    pal = false;

                var rom = new RomFile();
                rom.Save(
                    project, 
                    filename,
                    exportSongIds,
                    project.Name,
                    project.Author,
                    pal);
            }
        }

        private void FdsExport(string filename)
        {
            if (!ValidateExtension(filename, ".fds"))
                return;

            var exportSongIds = GetExportSongIds();
            if (exportSongIds != null)
            {
                if (exportSongIds.Length > FdsFile.FdsMaxSongs)
                {
                    Console.WriteLine($"There is currently a hard limit of {FdsFile.FdsMaxSongs} songs for FDS disk export.");
                    return;
                }

                var fds = new FdsFile();
                fds.Save(
                    project,
                    filename,
                    exportSongIds,
                    project.Name,
                    project.Author);
            }
        }

        private void FamiTrackerTextExport(string filename)
        {
            if (!ValidateExtension(filename, ".txt"))
                return;

            var exportSongIds = GetExportSongIds();
            if (exportSongIds != null)
            {
                new FamitrackerTextFile().Save(project, filename, exportSongIds);
            }
        }

        private void FamiStudioTextExport(string filename)
        {
            if (!ValidateExtension(filename, ".txt"))
                return;

            var exportSongIds = GetExportSongIds();
            if (exportSongIds != null)
            {
                var cleanup = HasOption("famistudio-txt-cleanup");
                var flags   = HasOption("famistudio-txt-bare") ? 
                        FamiStudioTextFlags.NoVersion | FamiStudioTextFlags.NoColors : 
                        FamiStudioTextFlags.None;

                new FamistudioTextFile().Save(project, filename, exportSongIds, cleanup, flags);
            }
        }

        private void FamiTone2MusicExport(string filename, bool famistudio)
        {
            var kernel = famistudio ? FamiToneKernel.FamiStudio : FamiToneKernel.FamiTone2;
            var engineName = famistudio ? "famistudio" : "famitone2";
            var formatString = ParseOption($"{engineName}-asm-format", "nesasm");

            var format = AssemblyFormat.NESASM;
            switch (formatString)
            {
                case "ca65": format = AssemblyFormat.CA65; break;
                case "asm6": format = AssemblyFormat.ASM6; break;
            }

            var extension = format == AssemblyFormat.CA65 ? ".s" : ".asm";
            var seperate = HasOption($"{engineName}-asm-seperate-files");
            var generateInclude = HasOption($"{engineName}-asm-generate-list");
            var forceDpcmBankswitch = famistudio && HasOption($"{engineName}-asm-force-dpcm-bankswitch");

            if (!seperate && !ValidateExtension(filename, extension))
                return;

            var exportSongIds = GetExportSongIds();
            if (exportSongIds != null)
            {
                if (seperate)
                {
                    if (!project.EnsureSongAssemblyNamesAreUnique())
                    {
                        return;
                    }

                    var songNamePattern = ParseOption($"{engineName}-asm-seperate-song-pattern", "{project}_{song}");
                    var dpcmNamePattern = ParseOption($"{engineName}-asm-seperate-dmc-pattern", "{project}");

                    foreach (var songId in exportSongIds)
                    {
                        var song = project.GetSong(songId);
                        var formattedSongName = songNamePattern.Replace("{project}", project.Name).Replace("{song}", song.Name);
                        var formattedDpcmName = dpcmNamePattern.Replace("{project}", project.Name).Replace("{song}", song.Name);
                        var songFilename = Path.Combine(filename, Utils.MakeNiceAsmName(formattedSongName) + extension);
                        var dpcmFilename = Path.Combine(filename, Utils.MakeNiceAsmName(formattedDpcmName) + ".dmc");
                        var includeFilename = generateInclude ? Path.ChangeExtension(songFilename, null) + "_songlist.inc" : null;

                        Log.LogMessage(LogSeverity.Info, $"Exporting song '{song.Name}' as separate assembly files.");

                        FamitoneMusicFile f = new FamitoneMusicFile(kernel, true);
                        f.Save(project, new int[] { songId }, format, -1, true, songFilename, dpcmFilename, includeFilename, MachineType.Dual); 
                    }
                }
                else
                {
                    var includeFilename = generateInclude ? Path.ChangeExtension(filename, null) + "_songlist.inc" : null;

                    Log.LogMessage(LogSeverity.Info, $"Exporting all songs to a single assembly file.");

                    FamitoneMusicFile f = new FamitoneMusicFile(kernel, true);
                    f.Save(project, exportSongIds, format, -1, false, filename, Path.ChangeExtension(filename, ".dmc"), includeFilename, MachineType.Dual);
                }
            }
        }

        private void FamiTone2SfxExport(string filename, bool famiStudio)
        {
            var engineName = famiStudio ? "famistudio" : "famitone2";
            var formatString = ParseOption($"{engineName}-asm-format", "nesasm");
            var format = AssemblyFormat.NESASM;
            switch (formatString)
            {
                case "ca65": format = AssemblyFormat.CA65; break;
                case "asm6": format = AssemblyFormat.ASM6; break;
            }

            var machineString = ParseOption($"{engineName}-asm-sfx-mode", project.PalMode ? "pal" : "ntsc");
            var machine = project.PalMode ? MachineType.PAL : MachineType.NTSC;
            switch (machineString.ToLower())
            {
                case "pal"  : machine = MachineType.PAL;  break;
                case "dual" : machine = MachineType.Dual; break;
                case "ntsc" : machine = MachineType.NTSC; break;
            }

            var extension = format == AssemblyFormat.CA65 ? ".s" : ".asm";

            if (!ValidateExtension(filename, extension))
                return;

            var generateInclude = HasOption($"{engineName}-sfx-generate-list");

            var exportSongIds = GetExportSongIds();
            if (exportSongIds != null)
            {
                var includeFilename = generateInclude ? Path.ChangeExtension(filename, null) + "_sfxlist.inc" : null;

                FamitoneSoundEffectFile f = new FamitoneSoundEffectFile();
                f.Save(project, exportSongIds, format, machine, famiStudio ? FamiToneKernel.FamiStudio : FamiToneKernel.FamiTone2, filename, includeFilename);
            }
        }

        private void RunUnitTest(string filename)
        {
            if (!ValidateExtension(filename, ".txt"))
                return;

            // HACK: For consistency.
            Settings.ClampPeriods = true;
            Settings.SquareSmoothVibrato = true;

            new UnitTestPlayer(HasOption("pal"), project.OutputsStereoAudio).GenerateUnitTestOutput(project.Songs[0], filename);
        }

        private void RunEpsmUnitTest(string nsf, string filename)
        {
            if (!ValidateExtension(filename, ".txt"))
                return;

            var numFrames = ParseOption("epsm-num-frames", 7200);

            InitializeConsole();
            Log.SetLogOutput(this);
            EpsmUnitTest.DumpEpsmRegs(nsf, filename, numFrames);
            ShutdownConsole();
        }

        public bool Run()
        {
            if (HasOption("?") || HasOption("help"))
            {
                DisplayHelp();
                return true;
            }

            // Super hacky EPSM unit tests.
            if (args.Length >= 3 && args[1].ToLower().Trim() == "unit-test-epsm")
            {
                RunEpsmUnitTest(args[0], args[2]);
                return true;
            }

#if DEBUG
            // Font dictionary
            if (args.Contains("generate-font-dictionary"))
            {
                FontCollection.DumpGlyphDictionary(Fonts.FontListRegular);
                return true;
            }
#endif

            if (args.Length >= 3)
            {
                InitializeConsole();
                Log.SetLogOutput(this);
                
                if (OpenProject())
                {
                    var outputFilename = args[2];

                    switch (args[1].ToLower().Trim())
                    {
                        case "wav-export": AudioExport(outputFilename, AudioFormatType.Wav); break;
                        case "mp3-export": AudioExport(outputFilename, AudioFormatType.Mp3); break;
                        case "ogg-export": AudioExport(outputFilename, AudioFormatType.Vorbis); break;
                        case "nsf-export": NsfExport(outputFilename); break;
                        case "rom-export": RomExport(outputFilename); break;
                        case "fds-export": FdsExport(outputFilename); break;
                        case "famitracker-txt-export": FamiTrackerTextExport(outputFilename); break;
                        case "famistudio-txt-export": FamiStudioTextExport(outputFilename); break;
                        case "famitone2-asm-export": FamiTone2MusicExport(outputFilename, false); break;
                        case "famitone2-asm-sfx-export": FamiTone2SfxExport(outputFilename, false); break;
                        case "famistudio-asm-export": FamiTone2MusicExport(outputFilename, true); break;
                        case "famistudio-asm-sfx-export": FamiTone2SfxExport(outputFilename, true); break;
                        case "unit-test": RunUnitTest(outputFilename); break;
                        default:
                            Console.WriteLine($"Unknown command {args[1]}. Use -help or -? for help.");
                            break;
                    }
                }

                ShutdownConsole();
                return true;
            }

            return false;
        }

        public void LogMessage(string msg)
        {
            Console.WriteLine("    " + msg);
        }

        public void ReportProgress(float progress)
        {
        }

        public bool AbortOperation => false;
    }
}
