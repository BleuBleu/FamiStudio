using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FamiStudio
{
    class UnitTestPlayer : BasePlayer, IRegisterListener
    {
        StreamWriter file;

        public UnitTestPlayer() : base(NesApu.APU_WAV_EXPORT)
        {
            loopMode = LoopMode.None;
        }

        public void GenerateUnitTestOutput(Song song, string filename, bool pal = false)
        {
            file = new StreamWriter(filename);

            if (BeginPlaySong(song, pal, 0, this))
                while (PlaySongFrame()) ;

            file.Close();
        }

        public void WriteRegister(int apuIndex, int reg, int data)
        {
            if (apuIndex == NesApu.APU_WAV_EXPORT)
                file.WriteLine($"Frame {frameNumber} Register {reg:X4} {data:X2}");
        }
    }
}
