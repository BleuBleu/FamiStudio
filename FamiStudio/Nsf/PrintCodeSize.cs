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
            
                foreach (var line in lines)
                {
                    if (line.StartsWith("CODE "))
                    {
                        var splits = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        var codeSize = Convert.ToInt32(splits[3], 16);
                        var roundedCodeSize = ((codeSize + 127) & ~127);
                        if ((roundedCodeSize % 256) == 0)
                            roundedCodeSize += 128;
                        Console.WriteLine($"    Code size is {codeSize.ToString("X")} (Rounded = {roundedCodeSize.ToString("X")})");
                    }
                }
            }
            catch {}
        }
    }
}
