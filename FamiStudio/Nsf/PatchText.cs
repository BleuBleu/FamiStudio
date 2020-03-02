using System;
using System.IO;

namespace PatchText
{
    class Program
    {
        static void Main(string[] args)
        {
            var text = File.ReadAllText(args[0]);

            for (int i = 2; i < args.Length; i++)
            {
                var splits = args[i].Split('=');
                text = text.Replace(splits[0], splits[1]);
            }

            File.WriteAllText(args[1], text);
        }
    }
}
