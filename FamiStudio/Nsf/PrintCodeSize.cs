using System;
using System.IO;

namespace PrintCodeSize
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var lines = File.ReadAllLines(args[0]);
                var codeSize = 0;
                var songDataStart = 0;

                foreach (var line in lines)
                {
                    if (line.StartsWith("CODE "))
                    {
                        var splits = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        codeSize = Convert.ToInt32(splits[3], 16);
                    }
                    else if (line.StartsWith("SONG_DATA "))
                    {
                        var splits = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        songDataStart = Convert.ToInt32(splits[1], 16);
                    }
                }

                var roundedCodeSize = ((codeSize + 127) & ~127);
                var allocatedCodeSize = (songDataStart - 0x8080);
                if ((roundedCodeSize % 256) == 0)
                roundedCodeSize += 128;
                Console.WriteLine($"    Code size is {codeSize.ToString("X")} (Padded size = {roundedCodeSize.ToString("X")}, Allocated size = {allocatedCodeSize.ToString("X")} {(roundedCodeSize != allocatedCodeSize ? "*** RESIZE NEEDED ***" : "")})");
            }
            catch {}
        }
    }
}
