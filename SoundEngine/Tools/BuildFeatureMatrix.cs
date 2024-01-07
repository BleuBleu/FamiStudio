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
                    //RedirectStandardOutput = true,
                    //CreateNoWindow = true
                }
            };

            // Console.WriteLine($"Running {exe} {args}");

            process.Start();
            process.WaitForExit();
        }

        static void Main(string[] args)
        {
            var expansionDefines = new string[19][]
            {
                new string [] { },
                new [] { "FAMISTUDIO_EXP_MMC5=1" },
                new [] { "FAMISTUDIO_EXP_S5B=1"  },
                new [] { "FAMISTUDIO_EXP_VRC6=1" },
                new [] { "FAMISTUDIO_EXP_VRC7=1" },
                new [] { "FAMISTUDIO_EXP_EPSM=1" },
                new [] { "FAMISTUDIO_EXP_EPSM=1" , "FAMISTUDIO_EXP_EPSM_SSG_CHN_CNT=3" , "FAMISTUDIO_EXP_EPSM_FM_CHN_CNT=0", "FAMISTUDIO_EXP_EPSM_RHYTHM_CHN1_ENABLE=0", "FAMISTUDIO_EXP_EPSM_RHYTHM_CHN2_ENABLE=0", "FAMISTUDIO_EXP_EPSM_RHYTHM_CHN3_ENABLE=0", "FAMISTUDIO_EXP_EPSM_RHYTHM_CHN4_ENABLE=0", "FAMISTUDIO_EXP_EPSM_RHYTHM_CHN5_ENABLE=0", "FAMISTUDIO_EXP_EPSM_RHYTHM_CHN6_ENABLE=0" },
                new [] { "FAMISTUDIO_EXP_EPSM=1" , "FAMISTUDIO_EXP_EPSM_SSG_CHN_CNT=0" , "FAMISTUDIO_EXP_EPSM_FM_CHN_CNT=3", "FAMISTUDIO_EXP_EPSM_RHYTHM_CHN1_ENABLE=0", "FAMISTUDIO_EXP_EPSM_RHYTHM_CHN2_ENABLE=0", "FAMISTUDIO_EXP_EPSM_RHYTHM_CHN3_ENABLE=0", "FAMISTUDIO_EXP_EPSM_RHYTHM_CHN4_ENABLE=0", "FAMISTUDIO_EXP_EPSM_RHYTHM_CHN5_ENABLE=0", "FAMISTUDIO_EXP_EPSM_RHYTHM_CHN6_ENABLE=0" },
                new [] { "FAMISTUDIO_EXP_EPSM=1" , "FAMISTUDIO_EXP_EPSM_SSG_CHN_CNT=0" , "FAMISTUDIO_EXP_EPSM_FM_CHN_CNT=6", "FAMISTUDIO_EXP_EPSM_RHYTHM_CHN1_ENABLE=0", "FAMISTUDIO_EXP_EPSM_RHYTHM_CHN2_ENABLE=0", "FAMISTUDIO_EXP_EPSM_RHYTHM_CHN3_ENABLE=0", "FAMISTUDIO_EXP_EPSM_RHYTHM_CHN4_ENABLE=0", "FAMISTUDIO_EXP_EPSM_RHYTHM_CHN5_ENABLE=0", "FAMISTUDIO_EXP_EPSM_RHYTHM_CHN6_ENABLE=0" },
                new [] { "FAMISTUDIO_EXP_EPSM=1" , "FAMISTUDIO_EXP_EPSM_SSG_CHN_CNT=0" , "FAMISTUDIO_EXP_EPSM_FM_CHN_CNT=0"},
                new [] { "FAMISTUDIO_EXP_FDS=1"  },
                new [] { "FAMISTUDIO_EXP_N163=1", "FAMISTUDIO_EXP_N163_CHN_CNT=1" },
                new [] { "FAMISTUDIO_EXP_N163=1", "FAMISTUDIO_EXP_N163_CHN_CNT=2" },
                new [] { "FAMISTUDIO_EXP_N163=1", "FAMISTUDIO_EXP_N163_CHN_CNT=3" },
                new [] { "FAMISTUDIO_EXP_N163=1", "FAMISTUDIO_EXP_N163_CHN_CNT=4" },
                new [] { "FAMISTUDIO_EXP_N163=1", "FAMISTUDIO_EXP_N163_CHN_CNT=5" },
                new [] { "FAMISTUDIO_EXP_N163=1", "FAMISTUDIO_EXP_N163_CHN_CNT=6" },
                new [] { "FAMISTUDIO_EXP_N163=1", "FAMISTUDIO_EXP_N163_CHN_CNT=7" },
                new [] { "FAMISTUDIO_EXP_N163=1", "FAMISTUDIO_EXP_N163_CHN_CNT=8" }
            };

            var expansionDesc = new[]
            {
                "2A03",
                "MMC5",
                "S5B",
                "VRC6",
                "VRC7",
                "EPSM",
                "EPSM SSG Only",
                "EPSM FM 1-3 Only",
                "EPSM FM 1-6 Only",
                "EPSM Rythm Only",
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
                //"FAMISTUDIO_USE_FAMITRACKER_TEMPO=1",
                "FAMISTUDIO_USE_RELEASE_NOTES=1",
                "FAMISTUDIO_USE_SLIDE_NOTES=1",
                "FAMISTUDIO_USE_NOISE_SLIDE_NOTES=1",
                "FAMISTUDIO_USE_VOLUME_TRACK=1",
                "FAMISTUDIO_USE_VOLUME_SLIDES=1",
                "FAMISTUDIO_USE_PITCH_TRACK=1",
                "FAMISTUDIO_USE_VIBRATO=1",
                "FAMISTUDIO_USE_ARPEGGIO=1",
                "FAMISTUDIO_USE_DUTYCYCLE_EFFECT=1",
                "FAMISTUDIO_USE_DELTA_COUNTER=1"
            };

            var featureDesc = new[]
            {
                "Basic",
                "Release Notes",
                "Slide Notes",
                "Slide Notes (Noise)",
                "Volume Track",
                "Volume Slides",
                "Fine Pitch Track",
                "Vibrato Effect",
                "Arpeggio Chords",
                "Duty Cycle Track",
                "Delta Counter"
            };

            var codeMatrix = new int[expansionDefines.GetLength(0), featureDefines.Length];
            var ramMatrix = new int[expansionDefines.GetLength(0), featureDefines.Length];
            var zpMatrix = new int[expansionDefines.GetLength(0), featureDefines.Length];

            const string ca65Exe = @"..\..\..\NES\tools\bin\ca65";
            const string ld65Exe = @"..\..\..\NES\tools\bin\ld65";

            for (int i = 0; i < expansionDefines.GetLength(0); i++)
            {
                var ca65Args = @"feature_matrix.s -g -o tmp.o -D FAMISTUDIO_CFG_EXTERNAL=1 -D FT_NTSC_SUPPORT=1 -D FAMISTUDIO_CFG_DPCM_SUPPORT=1 ";
                var ld65Args = @"-C feature_matrix.cfg -o tmp.bin tmp.o --mapfile tmp.map";

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

                    Console.WriteLine($"CODE = {codeSize}, RAM = {ramSize}, ZEROPAGE = {zpSize}");
                }
            }

            var lines = new List<string>();
            lines.Add("<html><body>");
            lines.Add("<h1>CODE</h1>");
            var header = "<table><th>";
            for (int j = 0; j < featureDesc.Length; j++)
                header += $"<td>{featureDesc[j]}</td>";
            header += "</th>";
            lines.Add(header);

            for (int i = 0; i < codeMatrix.GetLength(0); i++)
            {
                var row = $"<tr><td>{expansionDesc[i]}</td>";

                for (int j = 0; j < codeMatrix.GetLength(1); j++)
                    row += $"<td>{codeMatrix[i, j]}</td>";

                row += "</tr>";
                lines.Add(row);
            }
            
            lines.Add("</table><h1>RAM</h1><table>");
            header = "<table><th>";
            for (int j = 0; j < featureDesc.Length; j++)
                header += $"<td>{featureDesc[j]}</td>";
            header += "</th>";
            lines.Add(header);

            for (int i = 0; i < codeMatrix.GetLength(0); i++)
            {
                var row = $"<tr><td>{expansionDesc[i]}</td>";

                for (int j = 0; j < codeMatrix.GetLength(1); j++)
                    row += $"<td>{ramMatrix[i, j]}</td>";

                row += "</tr>";
                lines.Add(row);
            }

            lines.Add("</body></html>");
            File.WriteAllLines("FeatureMatrix.html", lines);
        }
    }
}
