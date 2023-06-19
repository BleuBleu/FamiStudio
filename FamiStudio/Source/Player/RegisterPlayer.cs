using System;
using System.Collections.Generic;

namespace FamiStudio
{
    struct RegisterWrite
    {
        public int FrameNumber;
        public int Register;
        public int Value;
        public List<int> Metadata;  //depends on the register
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
                seeking = true;
                NesApu.StartSeeking(apuIndex);
                while (PlaySongFrameInternal(true));
                NesApu.StopSeeking(apuIndex);
                seeking = false;
            }

            return registerWrites.ToArray();
        }

        public RegisterWrite[] GetRegisterValues(Song song, bool pal, out int length)
        {
            length = 0;
            registerWrites = new List<RegisterWrite>();

            if (BeginPlaySong(song, pal, 0))
            {
                seeking = true;
                NesApu.StartSeeking(apuIndex);
                while (PlaySongFrameInternal(true)){length++;};
                NesApu.StopSeeking(apuIndex);
                seeking = false;
            }
            length++;

            return registerWrites.ToArray();
        }

        public override void NotifyRegisterWrite(int apuIndex, int reg, int data, List<int> metadata = null)
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
