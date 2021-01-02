using System;
using System.Collections.Generic;

namespace FamiStudio
{
    struct RegisterWrite
    {
        public int FrameNumber;
        public int Register;
        public int Value;
    };

    class RegisterPlayer : BasePlayer, IRegisterListener
    {
        List<RegisterWrite> registerWrites;

        public RegisterPlayer() : base(NesApu.APU_WAV_EXPORT)
        {
            loopMode = LoopMode.None;
        }

        public RegisterWrite[] GetRegisterValues(Song song, bool pal)
        {
            registerWrites = new List<RegisterWrite>();

            if (BeginPlaySong(song, pal, 0, this))
            {
                while (PlaySongFrame());
            }

            return registerWrites.ToArray();
        }

        public void WriteRegister(int apuIndex, int reg, int data)
        {
            if (apuIndex == NesApu.APU_WAV_EXPORT)
            {
                var write = new RegisterWrite();

                write.FrameNumber = frameNumber;
                write.Register    = reg;
                write.Value       = data;

                registerWrites.Add(write);
            }
        }
    }
}
