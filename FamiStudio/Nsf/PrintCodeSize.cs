using System;
using System.IO;

namespace PrintCodeSize
{
    class Program
    {
        static void Main(string[] args)
        {
            var bytes = File.ReadAllBytes(args[0]);
            var bankCount = int.Parse(args[1].Substring(12)); // {CODEBANKS}=X

            var driverSize = bytes.Length;

            for (; driverSize >= 1; driverSize--)
            {
                if (bytes[driverSize - 1] != 0)
                    break;
            }

            driverSize -= 128; // NSF header.
            driverSize = ((driverSize + 4095) / 4096) * 4096;

            var driverBankCount = driverSize / 4096;

            if (driverBankCount != bankCount)
            {
                Console.WriteLine($"*** BANK COUNT MISMATCH! Banks needed = {driverBankCount}, Banks specified = {bankCount}");            
            }
        }
    }
}

