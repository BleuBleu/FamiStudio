using System;
using System.IO;
using System.Collections.Generic;

namespace ExtractNoteTable
{
    class Program
    {
        static void Main(string[] args)
        {
            var debugLines = File.ReadAllLines(args[0]);
            var bankCount = int.Parse(args[1].Substring(12)); // {CODEBANKS}=X
            var driverBase = 0x10000 - (bankCount * 4096);
            var noteTableFile = new List<string>();

            foreach (var line in debugLines)
            {
                if ((line.Contains("_note_table_") || line.Contains("_FT2NoteTable")) && !line.Contains("_slide_") && !line.Contains("_exp_") && line.Contains("addrsize=absolute") && line.Contains("type=lab"))
                {
                    var splits = line.Split(new [] { ',', '=', '"' }, StringSplitOptions.RemoveEmptyEntries);                        
                    var name = "";
                    var addr = -1;

                    for (int i = 0; i < splits.Length; i++)
                    {
                        if (splits[i] == "name")
                        {
                            i++;
                            name = splits[i];
                        }
                        else if (splits[i] == "val")
                        {
                            i++;
                            addr = Convert.ToInt32(splits[i], 16) - driverBase;
                        }
                    }

                    if (name.Length > 0 && addr >= 0)
                    {
                        noteTableFile.Add($"{name}={addr}");
                    }
                }
            }

            File.WriteAllLines(Path.ChangeExtension(args[0], ".tbl"), noteTableFile);
        }
    }
}

