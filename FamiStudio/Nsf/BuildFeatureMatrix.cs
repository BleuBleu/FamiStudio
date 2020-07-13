using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;

namespace PrintCodeSize
{
    class Program
    {
        static void GetStats(string mapFile, out int codeSize, out int ramSize, out int zpSize)
        {
            codeSize = 0;
            ramSize = 0;
            zpSize = 0;

            var lines = File.ReadAllLines(mapFile);

            foreach (var line in lines)
            {
                if (line.StartsWith("CODE "))
                {
                    var splits = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    codeSize = Convert.ToInt32(splits[3], 16);
                }
                else if (line.StartsWith("ZEROPAGE "))
                {
                    var splits = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    zpSize = Convert.ToInt32(splits[3], 16);
                }
                else if (line.StartsWith("RAM "))
                {
                    var splits = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    ramSize = Convert.ToInt32(splits[3], 16);
                }
            }
        }

        static void RunProcess(string exe, string args)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = exe,
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            // Console.WriteLine($"Running {exe} {args}");

            process.Start();
            process.WaitForExit();
        }

        static void Main(string[] args)
        {
            var expansionDefines = new string[14][]
            {
                new string [] { },
                new [] { "FAMISTUDIO_EXP_VRC6" },
                new [] { "FAMISTUDIO_EXP_VRC7" },
                new [] { "FAMISTUDIO_EXP_MMC5" },
                new [] { "FAMISTUDIO_EXP_S5B"  },
                new [] { "FAMISTUDIO_EXP_FDS"  },
                new [] { "FAMISTUDIO_EXP_N163", "FAMISTUDIO_EXP_N163_CHN_CNT=1" },
                new [] { "FAMISTUDIO_EXP_N163", "FAMISTUDIO_EXP_N163_CHN_CNT=2" },
                new [] { "FAMISTUDIO_EXP_N163", "FAMISTUDIO_EXP_N163_CHN_CNT=3" },
                new [] { "FAMISTUDIO_EXP_N163", "FAMISTUDIO_EXP_N163_CHN_CNT=4" },
                new [] { "FAMISTUDIO_EXP_N163", "FAMISTUDIO_EXP_N163_CHN_CNT=5" },
                new [] { "FAMISTUDIO_EXP_N163", "FAMISTUDIO_EXP_N163_CHN_CNT=6" },
                new [] { "FAMISTUDIO_EXP_N163", "FAMISTUDIO_EXP_N163_CHN_CNT=7" },
                new [] { "FAMISTUDIO_EXP_N163", "FAMISTUDIO_EXP_N163_CHN_CNT=8" }
            };

            var expansionDesc = new[]
            {
                "2A03",
                "VRC6",
                "VRC7",
                "MMC5",
                "S5B",
                "FDS",
                "N163 (1 channels)",
                "N163 (2 channels)",
                "N163 (3 channels)",
                "N163 (4 channels)",
                "N163 (5 channels)",
                "N163 (6 channels)",
                "N163 (7 channels)",
                "N163 (8 channels)"
            };

            var featureDefines = new[]
            {
                "",
                //"FAMISTUDIO_USE_FAMITRACKER_TEMPO",
                "FAMISTUDIO_USE_SLIDE_NOTES",
                "FAMISTUDIO_USE_VOLUME_TRACK",
                "FAMISTUDIO_USE_PITCH_TRACK",
                "FAMISTUDIO_USE_VIBRATO",
                "FAMISTUDIO_USE_ARPEGGIO"
            };

            var featureDesc = new[]
            {
                "Basic",
                "Slide Notes",
                "Volume Track",
                "Fine Pitch Track",
                "Vibrato Effect",
                "Arpeggio Chords"
            };

            var codeMatrix = new int[expansionDefines.GetLength(0), featureDefines.Length];
            var ramMatrix = new int[expansionDefines.GetLength(0), featureDefines.Length];
            var zpMatrix = new int[expansionDefines.GetLength(0), featureDefines.Length];

            const string ca65Exe = @"..\..\..\NES\tools\bin\ca65";
            const string ld65Exe = @"..\..\..\NES\tools\bin\ld65";

            for (int i = 0; i < expansionDefines.GetLength(0); i++)
            {
                var ca65Args = @"famistudio.s -g -o tmp.o -D FT_NTSC_SUPPORT=1 ";
                var ld65Args = @"-C FeatureMatrix.cfg -o tmp.bin tmp.o --mapfile tmp.map";

                foreach (var def in expansionDefines[i])
                    ca65Args += $" -D {def}";

                for (int j = 0; j < featureDefines.Length; j++)
                {
                    if (featureDefines[j].Length > 0)
                        ca65Args += $" -D {featureDefines[j]}";

                    RunProcess(ca65Exe, ca65Args);
                    RunProcess(ld65Exe, ld65Args);

                    GetStats("tmp.map", out var codeSize, out var ramSize, out var zpSize);

                    codeMatrix[i, j] = codeSize;
                    ramMatrix[i, j] = ramSize;
                    zpMatrix[i, j] = zpSize;

                    Console.WriteLine($"CODE = {codeSize}, RAM = {ramSize}, ZP = {zpSize}");
                }
            }

            var lines = new List<string>();
            var header = "<html><body><table><th>";

            for (int j = 0; j < featureDesc.Length; j++)
            {
                header += $"<td>{featureDesc[j]}</td>";
            }

            header += "</th>";
            lines.Add(header);

            for (int i = 0; i < codeMatrix.GetLength(0); i++)
            {
                var row = $"<tr><td>{expansionDesc[i]}</td>";

                for (int j = 0; j < codeMatrix.GetLength(1); j++)
                    row += $"<td>CODE: {codeMatrix[i, j]}<br>RAM: {ramMatrix[i, j]}<br>ZEROPAGE: {zpMatrix[i, j]}</td>";

                row += "</tr>";
                lines.Add(row);
            }

            lines.Add("</body></html>");
            File.WriteAllLines("FeatureMatrix.html", lines);
        }
    }
}
