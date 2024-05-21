using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PatchBin
{
    class Program
    {
        private static int FindString(byte[] fdsData, string str)
        {
            var filenameAscii = Encoding.ASCII.GetBytes(str);

            for (int i = 0; i < fdsData.Length; i++)
            {
                bool match = true;
                for (int j = 0; j < filenameAscii.Length; j++)
                {
                    if (filenameAscii[j] != fdsData[i + j])
                    {
                        match = false;
                        break;
                    }
                }
                if (match)
                    return i;
            }
            
            return -1;
        }
    
        private static void TruncateToLastFile(ref byte[] fds)
        {
            //var i = FindString(fds, "BYPASS..");
            //Array.Resize(ref fds, i + 15);
            var i = FindString(fds, "KYODAKU-");
            Array.Resize(ref fds, i + 238);
        }
    
        private static void PatchFile(byte[] fds, string filename, byte[] newFileData)
        {
            var i = FindString(fds, filename);

            for (int j = 0; j < newFileData.Length; j++)
                fds[i + j + 14] = newFileData[j];
        }

        static void Main(string[] args)
        {
            var fds = File.ReadAllBytes(args[0]);
            var toc = File.ReadAllBytes(args[1]);
            var dat = File.ReadAllBytes(args[2]);

            TruncateToLastFile(ref fds);
            PatchFile(fds, "TOC.....", toc);

            var packedFds = new List<byte>();
            packedFds.AddRange(fds);
            packedFds.AddRange(dat);
            packedFds.AddRange(new byte[65516 - packedFds.Count]);
            
            File.WriteAllBytes(args[3], packedFds.ToArray());
        }
    }
}
