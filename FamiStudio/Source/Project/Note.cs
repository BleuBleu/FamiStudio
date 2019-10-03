using System.Diagnostics;

namespace FamiStudio
{
    public struct Note
    {
        public static string[] NoteNames = 
        {
            "C",
            "C#",
            "D",
            "D#",
            "E",
            "F",
            "F#",
            "G",
            "G#",
            "A",
            "A#",
            "B"
        };

        public const int EffectNone  = 0;
        public const int EffectJump  = 1; // Bxx
        public const int EffectSkip  = 2; // Dxx
        public const int EffectSpeed = 3; // Fxx

        public const int VolumeInvalid = 0xff;
        public const int VolumeMax     = 0x0f;

        public const int NoteInvalid = 0xff;
        public const int NoteStop    = 0x00;
        public const int NoteMin     = 0x01;
        public const int NoteMax     = 0x60;
        public const int NoteRelease = 0xf7;

        public byte Value; // (0 = stop, 1 = C0 ... 96 = B7).
        public byte Effect; // Tempo/Jump/Skip
        public byte EffectParam; // Value for fx.
        public byte Volume; // 0-15. 0xff = no volume change.
        public Instrument Instrument;

        public bool IsValid
        {
            get { return Value != NoteInvalid; }
            set { if (!value) Value = NoteInvalid; }
        }

        public bool IsStop
        {
            get { return Value == NoteStop; }
            set { if (value) Value = NoteStop; }
        }

        public bool IsRelease
        {
            get { return Value == NoteRelease; }
        }

        public bool IsMusical
        {
            get { return IsValid && !IsStop && !IsRelease; }
        }

        public bool HasEffect
        {
            get { return Effect != EffectNone; }
            set { if (!value) Effect = EffectNone; }
        }

        public bool HasVolume
        {
            get { return Volume != VolumeInvalid; }
            set { if (!value) Volume = VolumeInvalid; }
        }

        public string FriendlyName
        {
            get
            {
                return GetFriendlyName(Value);
            }
        }

        public static string GetFriendlyName(int value)
        {
            if (value == NoteStop)
                return "Stop Note";
            if (value == NoteRelease)
                return "Release Note";
            if (value == NoteInvalid)
                return "";

            int octave = (value - 1) / 12 + 1;
            int note   = (value - 1) % 12;

            return NoteNames[note] + octave.ToString();
        }

        public static int GetEffectMaxValue(Song song, int fx)
        {
            switch (fx)
            {
                case EffectJump  : return song.Length;
                case EffectSkip  : return song.PatternLength;
                case EffectSpeed : return 31;
            }

            return 0;
        }

        public static int Clamp(int note)
        {
            Debug.Assert(note != NoteInvalid);
            if (note < NoteMin) return NoteMin;
            if (note > NoteMax) return NoteMax;
            return note;
        }
    }
}
