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
        int frameIndex = 0;
        List<RegisterWrite> registerWrites;

        public RegisterPlayer() : base(NesApu.APU_WAV_EXPORT)
        {
            loopMode = LoopMode.None;
        }

        public RegisterWrite[] GetRegisterValues(Song song)
        {
            registerWrites = new List<RegisterWrite>();

            if (BeginPlaySong(song, false, 0, this))
            {
                while (PlaySongFrame())
                    frameIndex++;
            }

            return registerWrites.ToArray();
        }

        public void WriteRegister(int apuIndex, int reg, int data)
        {
            if (apuIndex == NesApu.APU_WAV_EXPORT)
            {
                var write = new RegisterWrite();

                write.FrameNumber = frameIndex;
                write.Register    = reg;
                write.Value       = data;

                registerWrites.Add(write);
            }
        }
    }
}
