using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;

namespace FamiStudio
{
    static class EpsmUnitTest
    {
        private static int addr0 = 0;
        private static int addr1 = 0;
        private static byte[] regs0 = new byte[184];
        private static byte[] regs1 = new byte[184];
        private static NotSoFatso.WriteRegisterDelegate regCallback;

        private static void ApuRegisterWriteDeledate(int addr, int data)
        {
            switch (addr)
            {
                case NesApu.EPSM_ADDR0: addr0 = data; break;
                case NesApu.EPSM_ADDR1: addr1 = data; break;
                case NesApu.EPSM_DATA0: regs0[addr0] = (byte)data; break;
                case NesApu.EPSM_DATA1: regs1[addr1] = (byte)data; break;
            }
        }

        private static void DumpRegs(List<string> lines, string prefix, byte[] regs)
        {
            var tmp = new byte[8];

            for (int i = 0; i < regs.Length; i += 8)
            {
                for (int j = 0; j < 8; j++)
                    tmp[j] = regs[i + j];

                lines.Add($"{prefix} {i:X2}: " + String.Join(",", tmp.Select(x => $"{x:X2}")));
            }
        }

        public static void DumpEpsmRegs(string nsfFilename, string outputFilename, int numFrames)
        {
            var nsf = NotSoFatso.NsfOpen(nsfFilename);

            if (nsf == IntPtr.Zero)
            {
                Log.LogMessage(LogSeverity.Error, "Error opening NSF file.");
                return;
            }

            var exp = NotSoFatso.NsfGetExpansion(nsf);

            if ((exp & 0x80) == 0)
            {
                Log.LogMessage(LogSeverity.Error, "NSF does not use EPSM.");
                return;
            }
            
            regCallback = new NotSoFatso.WriteRegisterDelegate(ApuRegisterWriteDeledate);
            NotSoFatso.NsfSetApuWriteCallback(nsf, regCallback);
            NotSoFatso.NsfSetTrack(nsf, 0);

            var lines = new List<string>();

            for (int i = 0; i < numFrames; i++)
            {
                NotSoFatso.NsfRunFrame(nsf);

                lines.Add($"[Frame {i}]");
                DumpRegs(lines, "A0", regs0);
                DumpRegs(lines, "A1", regs1);
            }

            NotSoFatso.NsfSetApuWriteCallback(nsf, null);

            File.WriteAllLines(outputFilename, lines);
        }
    }
}
