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
                        Console.WriteLine($"    Code size is {splits[3]}");
                    }
                }
            }
            catch {}
        }
    }
}
