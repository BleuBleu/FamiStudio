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

            Array.Copy(code, 128, file, 128, code.Length - 128 - 256); // 256 = leave song table untouched.

            File.WriteAllBytes(args[2], file);
        }
    }
}
