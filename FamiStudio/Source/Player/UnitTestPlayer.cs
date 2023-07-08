using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FamiStudio
{
    class UnitTestPlayer : BasePlayer
    {
        StreamWriter file;

        public UnitTestPlayer(bool stereo) : base(NesApu.APU_WAV_EXPORT, stereo)
        {
            loopMode = LoopMode.None;
        }

        public void GenerateUnitTestOutput(Song song, string filename, bool pal = false)
        {
            file = new StreamWriter(filename);

            if (BeginPlaySong(song, pal, 0))
                while (PlaySongFrame()) ;

            file.Close();
        }

        public override void NotifyRegisterWrite(int apuIndex, int reg, int data, List<int> metadata = null)
        {
            if (apuIndex == NesApu.APU_WAV_EXPORT)
                file.WriteLine($"Frame {frameNumber} Register {reg:X4} {data:X2}");
        }
    }
}
