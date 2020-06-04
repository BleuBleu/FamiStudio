using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FamiStudio
{
    class CommandLineInterface
    {
        private string[] args;
        private Project project;

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

        private int ParseOption(string name, int defaultValue)
        {
            foreach (var a in args)
            {
                if (a.Length >= 2 && a[0] == '-' && a.Substring(1).ToLower().StartsWith(name))
                {
                    var colonIdx = a.LastIndexOf(':');
                    if (colonIdx >= 0)
                        return Convert.ToInt32(a.Substring(colonIdx + 1));
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

#if FAMISTUDIO_WINDOWS
        [DllImport("kernel32.dll")]
        static extern bool AttachConsole(int dwProcessId);
        private const int ATTACH_PARENT_PROCESS = -1;
#endif

        private void InitializeConsole()
        {
#if FAMISTUDIO_WINDOWS
            AttachConsole(ATTACH_PARENT_PROCESS);
#endif
        }

        private void DisplayHelp()
        {
            var version = Application.ProductVersion.Substring(0, Application.ProductVersion.LastIndexOf('.'));

            InitializeConsole();
            Console.WriteLine($"FamiStudio {version} Command-Line Usage");
            Console.WriteLine($"");
            Console.WriteLine($"Usage:");
            Console.WriteLine($"  FamiStudio <input> <command> <output> [-options]");
            Console.WriteLine($"");
            Console.WriteLine($"Examples:");
            Console.WriteLine($"  FamiStudio music.fms wav-export music.wave -export-songs:2 -wav-export-rate:48000");
            Console.WriteLine($"  FamiStudio music.fms famitracker-txt-export music.txt -export-songs:0,1,2");
            Console.WriteLine($"  FamiStudio music.fms famitone2-export music.s -famitone2-format:ca65");
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
            Console.WriteLine($"  nsf-export : Export to a NSF file (*.nsf).");
            Console.WriteLine($"  rom-export : Export to a NES ROM file (.nes).");
            Console.WriteLine($"  famitracker-txt-export : Export to a FamiTracker text file (*.txt).");
            Console.WriteLine($"  famistudio-txt-export : Export to a FamiStudio text file (*.txt).");
            Console.WriteLine($"  famitone2-export : Export to FamiTone2 assembly file(s) (*.s, *.asm).");
            Console.WriteLine($"");
            Console.WriteLine($"General options:");
            Console.WriteLine($"  -export-songs:<songs> : Comma-seperated zero-based indices of the songs to export (default:all).");
            Console.WriteLine($"");
            Console.WriteLine($"NSF import specific options");
            Console.WriteLine($"  -nsf-import-song:<song> : Zero-based index of the song to import (default:0).");
            Console.WriteLine($"  -nsf-import-duration:<duration> : Duration, in sec, to record from the NSF (default:120).");
            Console.WriteLine($"  -nsf-import-pattern-length:<length> : Pattern length to split the NSF into (default:256).");
            Console.WriteLine($"  -nsf-import-start-frame:<frame> : Frame to skips before starting the NSF capture (default:0).");
            Console.WriteLine($"");
            Console.WriteLine($"WAV export specific options");
            Console.WriteLine($"  -wav-export-rate:<rate> : Sample rate of the exported wave : 11025, 22050, 44100 or 48000 (default:44100).");
            Console.WriteLine($"");
            Console.WriteLine($"NSF export specific options");
            Console.WriteLine($"  -nsf-export-mode:<mode> : Target machine: ntsc, pal or dual (default:ntsc).");
            Console.WriteLine($"");
            Console.WriteLine($"FamiTone2 export specific options");
            Console.WriteLine($"  -famitone2-format:<format> : Assembly format to export to : nesasm, ca65 or asm6 (default:nesasm).");
            Console.WriteLine($"  -famitone2-variant:<variant> : The variant of FamiTone2 to use : famitone2, famitone2fs or famistudio (default:famitone2).");
            Console.WriteLine($"  -famitone2-seperate-files : Export songs to individual files, output filename is the output path (default:disabled).");
            Console.WriteLine($"  -famitone2-seperate-song-pattern:<pattern> : Name pattern to use when exporting songs to seperate files (default:{{project}}_{{song}}).");
            Console.WriteLine($"  -famitone2-seperate-dmc-pattern:<pattern> : DMC filename pattern to use when exporting songs to seperate files (default:{{project}}).");
            Console.WriteLine($"");
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
                project = new FamistudioTextFile().Load(filename);

                if (project == null)
                    project = new FamitrackerTextFile().Load(filename);
            }
            else if (filename.ToLower().EndsWith("nsf") || filename.ToLower().EndsWith("nsfe"))
            {
                var songIndex  = ParseOption("nsf-import-song", 0);
                var duration   = ParseOption("nsf-import-duration", 120);
                var patternLen = ParseOption("nsf-import-pattern-length", 256);
                var startFrame = ParseOption("nsf-import-start-frame", 0);

                project = new NsfFile().Load(filename, songIndex, duration, patternLen, startFrame, true);
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

        private void WavExport(string filename)
        {
            if (!ValidateExtension(filename, ".wav"))
                return;

            var songIndex  = ParseOption("export-song", 0);
            var sampleRate = ParseOption("wav-export-rate", 44100);
            var song       = GetProjectSong(songIndex);

            if (song != null)
                WaveFile.Save(song, filename, sampleRate);
        }

        private void NsfExport(string filename)
        {
            if (!ValidateExtension(filename, ".nsf"))
                return;

            var mode = ParseOption("nsf-export-mode", "ntsc");
            var machine = MachineType.NTSC;

            switch (mode.ToLower())
            {
                case "pal"  : machine = MachineType.PAL;  break;
                case "dual" : machine = MachineType.Dual; break;
            }

            var exportSongIds = GetExportSongIds();
            if (exportSongIds != null)
            {
                new NsfFile().Save(
                    project,
                    FamitoneMusicFile.FamiToneKernel.FamiStudio,
                    filename,
                    exportSongIds,
                    project.Name,
                    project.Author,
                    project.Copyright,
                    machine);
            }
        }

        private void RomExport(string filename)
        {
            if (!ValidateExtension(filename, ".nes"))
                return;

            var exportSongIds = GetExportSongIds();
            if (exportSongIds != null)
            {
                if (exportSongIds.Length > RomFile.MaxSongs)
                {
                    Console.WriteLine("There is currently a hard limit of 8 songs for NES ROM export.");
                    return;
                }

                RomFile.Save(
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
                new FamistudioTextFile().Save(project, filename, exportSongIds);
            }
        }

        private void FamiTone2Export(string filename)
        {
            var formatString = ParseOption("famitone2-format", "nesasm");

            var format = AssemblyFormat.NESASM;
            switch (formatString)
            {
                case "ca65": format = AssemblyFormat.CA65; break;
                case "asm6": format = AssemblyFormat.ASM6; break;
            }

            var extension = format == AssemblyFormat.CA65 ? ".s" : ".asm";
            var seperate = HasOption("famitone2-seperate-files");

            if (!seperate && !ValidateExtension(filename, extension))
                return;

            var kernelString = ParseOption("famitone2-variant", "famitone2");
            var kernel = FamitoneMusicFile.FamiToneKernel.FamiTone2;
            switch (formatString)
            {
                case "famitone2fs": kernel = FamitoneMusicFile.FamiToneKernel.FamiTone2FS; break;
                case "famistudio":  kernel = FamitoneMusicFile.FamiToneKernel.FamiStudio;  break;
            }

            var exportSongIds = GetExportSongIds();
            if (exportSongIds != null)
            {
                if (seperate)
                {
                    var songNamePattern = ParseOption("famitone2-seperate-song-pattern", "{project}_{song}");
                    var dpcmNamePattern = ParseOption("famitone2-seperate-dmc-pattern", "{project}");

                    foreach (var songId in exportSongIds)
                    {
                        var song = project.GetSong(songId);
                        var formattedSongName = songNamePattern.Replace("{project}", project.Name).Replace("{song}", song.Name);
                        var formattedDpcmName = dpcmNamePattern.Replace("{project}", project.Name).Replace("{song}", song.Name);
                        var songFilename = Path.Combine(filename, Utils.MakeNiceAsmName(formattedSongName) + extension);
                        var dpcmFilename = Path.Combine(filename, Utils.MakeNiceAsmName(formattedDpcmName) + ".dmc");

                        FamitoneMusicFile f = new FamitoneMusicFile(kernel);
                        f.Save(project, new int[] { songId }, format, true, songFilename, dpcmFilename, MachineType.Dual);
                    }
                }
                else
                {
                    FamitoneMusicFile f = new FamitoneMusicFile(kernel);
                    f.Save(project, exportSongIds, format, false, filename, Path.ChangeExtension(filename, ".dmc"), MachineType.Dual);
                }
            }
        }

        public bool Run()
        {
            if (HasOption("?") || HasOption("help"))
            {
                DisplayHelp();
                return true;
            }

            if (args.Length >= 3)
            {
                InitializeConsole();

                if (OpenProject())
                {
                    var outputFilename = args[2];

                    switch (args[1].ToLower().Trim())
                    {
                        case "wav-export": WavExport(outputFilename); break;
                        case "nsf-export": NsfExport(outputFilename); break;
                        case "rom-export": RomExport(outputFilename); break;
                        case "famitracker-txt-export": FamiTrackerTextExport(outputFilename); break;
                        case "famistudio-txt-export":  FamiStudioTextExport(outputFilename);  break;
                        case "famitone2-export": FamiTone2Export(outputFilename); break;
                        default:
                            Console.WriteLine($"Unknown command {args[1]}. Use -help or -? for help.");
                            break;
                    }
                }

                return true;
            }

            return false;
        }
    }
}
