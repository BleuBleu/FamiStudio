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
        static void Main(string[] args)
        {
            var file = File.ReadAllBytes(args[0]);
            var code = File.ReadAllBytes(args[1]);

            Array.Copy(code, 0, file, int.Parse(args[2]), code.Length);

            File.WriteAllBytes(args[3], file);
        }
    }
}
