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

    class RegisterPlayer : BasePlayer
    {
        List<RegisterWrite> registerWrites;

        public RegisterPlayer(bool stereo) : base(NesApu.APU_WAV_EXPORT, stereo)
        {
            loopMode = LoopMode.None;
        }

        public RegisterWrite[] GetRegisterValues(Song song, bool pal)
        {
            registerWrites = new List<RegisterWrite>();

            if (BeginPlaySong(song, pal, 0))
            {
                while (PlaySongFrame());
            }

            return registerWrites.ToArray();
        }

        public override void NotifyRegisterWrite(int apuIndex, int reg, int data)
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
