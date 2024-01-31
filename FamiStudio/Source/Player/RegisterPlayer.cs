using System;
using System.Collections.Generic;

namespace FamiStudio
{
    struct RegisterWrite
    {
        public int FrameNumber;
        public int Register;
        public int Value;
        public int Metadata;  //depends on the register
    };

    class RegisterPlayer : BasePlayer
    {
        List<RegisterWrite> registerWrites;

        public RegisterPlayer(bool pal, bool stereo) : base(NesApu.APU_WAV_EXPORT, pal, stereo)
        {
            loopMode = LoopMode.None;
        }

        public RegisterWrite[] GetRegisterValues(Song song)
        {
            registerWrites = new List<RegisterWrite>();

            BeginPlaySong(song);

            StartSeeking();
            while (PlaySongFrameInternal(true));
            StopSeeking();

            return registerWrites.ToArray();
        }

        public RegisterWrite[] GetRegisterValues(Song song, out int length)
        {
            length = 0;
            registerWrites = new List<RegisterWrite>();

            BeginPlaySong(song);

            StartSeeking();
            while (PlaySongFrameInternal(true)){length++;};
            StopSeeking();

            return registerWrites.ToArray();
        }

        public override void NotifyRegisterWrite(int apuIndex, int reg, int data, int metadata = 0)
        {
            if (apuIndex == NesApu.APU_WAV_EXPORT)
            {
                var write = new RegisterWrite();

                write.FrameNumber = frameNumber;
                write.Register    = reg;
                write.Value       = data;
                write.Metadata    = metadata;

                registerWrites.Add(write);
            }
        }
    }
}
