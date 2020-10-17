using System;
using System.Diagnostics;

namespace FamiStudio
{
    public class Note
    {
        public static readonly string[] NoteNames =
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

        public const int VolumeMax        = 0x0f;
        public const int VibratoSpeedMax  = 0x0c;
        public const int VibratoDepthMax  = 0x0f;
        public const int FinePitchMin     = -128;
        public const int FinePitchMax     =  127;

        public const int FlagsNone       = 0x00;
        public const int FlagsNoAttack   = 0x01;

        public const int NoteInvalid     = 0xff;
        public const int NoteStop        = 0x00;
        public const int MusicalNoteMin  = 0x01;
        public const int MusicalNoteMax  = 0x60;
        public const int NoteRelease     = 0x80;
        public const int DPCMNoteMin     = 0x0c;
        public const int DPCMNoteMax     = 0x4b;

        public readonly static Note EmptyNote = new Note();

        // TODO: Create properties. This used to be a struct, its a proper class now...
        public byte       Value = NoteInvalid; // (0 = stop, 1 = C0 ... 96 = B7).
        public byte       Flags;
        public byte       Slide;
        public ushort     EffectMask;
        public Instrument Instrument;
        public Arpeggio   Arpeggio;

        // Effects.
        private byte   FxVolume;
        private byte   FxVibrato;
        private byte   FxSpeed;
        private sbyte  FxFinePitch;
        private byte   FxFdsModDepth;
        private ushort FxFdsModSpeed;
        private byte   FxDutyCycle;
        private byte   FxNoteDelay;
        private byte   FxCutDelay;

        // As of version 5 (FamiStudio 2.0.0), these are deprecated and are only kepth around
        // for migration.
        public byte FxJump = 0xff;
        public byte FxSkip = 0xff;

        public Note()
        {
        }

        public Note(int value)
        {
            Value = (byte)value;
        }

        public void Clear(bool preserveFx = true)
        {
            Value = NoteInvalid;
            Instrument = null;
            Arpeggio = null;
            Slide = 0;
            Flags = 0;

            if (!preserveFx)
            {
                EffectMask = 0;
                FxVolume = 0;
                FxVibrato = 0;
                FxSpeed = 0;
                FxFinePitch = 0;
                FxFdsModDepth = 0;
                FxFdsModSpeed = 0;
                FxDutyCycle = 0;
                FxNoteDelay = 0;
                FxCutDelay = 0;
            }
        }

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

        public bool IsSlideNote
        {
            get { return Slide != 0; }
            set { if (!value) Slide = 0; }
        }

        public byte SlideNoteTarget
        {
            get { return Slide; }
            set { Slide = value; }
        }

        public bool IsArpeggio
        {
            get { return Arpeggio != null; }
            set { if (!value) Arpeggio = null; }
        }

        public byte Volume
        {
            get { Debug.Assert(HasVolume); return FxVolume; }
            set { Debug.Assert(value >= 0 && value <= VolumeMax); FxVolume = value; HasVolume = true; }
        }

        public byte RawVibrato
        {
            get { return FxVibrato; }
            set
            {
                FxVibrato  = value;
                HasVibrato = true;
            }
        }

        public byte VibratoSpeed
        {
            get { Debug.Assert(HasVibrato); return (byte)(FxVibrato >> 4); }
            set
            {
                Debug.Assert(value >= 0 && value <= VibratoSpeedMax);

                FxVibrato &= 0x0f;
                FxVibrato |= (byte)(value << 4);
                HasVibrato = true;
            }
        }

        public byte VibratoDepth
        {
            get { Debug.Assert(HasVibrato); return (byte)(FxVibrato & 0x0f); }
            set
            {
                Debug.Assert(value >= 0 && value <= VibratoDepthMax);

                FxVibrato &= 0xf0;
                FxVibrato |= value;

                if (HasVibrato)
                    VibratoSpeed = (byte)Utils.Clamp(VibratoSpeed, 0, VibratoSpeedMax);

                HasVibrato = true;
            }
        }

        public sbyte FinePitch
        {
            get { Debug.Assert(HasFinePitch); return FxFinePitch; }
            set { FxFinePitch = value; HasFinePitch = true; }
        }

        public byte Speed
        {
            get { Debug.Assert(HasSpeed); return FxSpeed; }
            set { Debug.Assert(value > 0 && value <= 31); FxSpeed = value; HasSpeed = true; }
        }

        public byte FdsModDepth
        {
            get { Debug.Assert(HasFdsModDepth); return FxFdsModDepth; }
            set { Debug.Assert(value >= 0 && value <= 63); FxFdsModDepth = value; HasFdsModDepth = true; }
        }

        public ushort FdsModSpeed
        {
            get { Debug.Assert(HasFdsModSpeed); return FxFdsModSpeed; }
            set { Debug.Assert(value >= 0 && value <= 4096); FxFdsModSpeed = value; HasFdsModSpeed = true; }
        }

        public byte DutyCycle
        {
            get { Debug.Assert(HasDutyCycle); return FxDutyCycle; }
            set { Debug.Assert(value >= 0 && value <= 7); FxDutyCycle = value; HasDutyCycle = true; }
        }

        public byte NoteDelay
        {
            get { Debug.Assert(HasNoteDelay); return FxNoteDelay; }
            set { Debug.Assert(value >= 0 && value <= 31); FxNoteDelay = value; HasNoteDelay = true; }
        }

        public byte CutDelay
        {
            get { Debug.Assert(HasCutDelay); return FxCutDelay; }
            set { Debug.Assert(value >= 0 && value <= 31); FxCutDelay = value; HasCutDelay = true; }
        }

        public bool HasVolume
        {
            get { return (EffectMask & EffectVolumeMask) != 0; }
            set { if (value) EffectMask |= EffectVolumeMask; else EffectMask = (ushort)(EffectMask & ~EffectVolumeMask); }
        }

        public bool HasVibrato
        {
            get { return (EffectMask & EffectVibratoMask) != 0; }
            set { if (value) EffectMask |= EffectVibratoMask; else EffectMask = (ushort)(EffectMask & ~EffectVibratoMask); }
        }

        public bool HasFinePitch
        {
            get { return (EffectMask & EffectFinePitchMask) != 0; }
            set { if (value) EffectMask |= EffectFinePitchMask; else EffectMask = (ushort)(EffectMask & ~EffectFinePitchMask); }
        }

        public bool HasSpeed
        {
            get { return (EffectMask & EffectSpeedMask) != 0; }
            set { if (value) EffectMask |= EffectSpeedMask; else EffectMask = (ushort)(EffectMask & ~EffectSpeedMask); }
        }

        public bool HasFdsModDepth
        {
            get { return (EffectMask & EffectFdsModDepthMask) != 0; }
            set { if (value) EffectMask |= EffectFdsModDepthMask; else EffectMask = (ushort)(EffectMask & ~EffectFdsModDepthMask); }
        }

        public bool HasFdsModSpeed
        {
            get { return (EffectMask & EffectFdsModSpeedMask) != 0; }
            set { if (value) EffectMask |= EffectFdsModSpeedMask; else EffectMask = (ushort)(EffectMask & ~EffectFdsModSpeedMask); }
        }

        public bool HasDutyCycle
        {
            get { return (EffectMask & EffectDutyCycleMask) != 0; }
            set { if (value) EffectMask |= EffectDutyCycleMask; else EffectMask = (ushort)(EffectMask & ~EffectDutyCycleMask); }
        }

        public bool HasNoteDelay
        {
            get { return (EffectMask & EffectNoteDelayMask) != 0; }
            set { if (value) EffectMask |= EffectNoteDelayMask; else EffectMask = (ushort)(EffectMask & ~EffectNoteDelayMask); }
        }

        public bool HasCutDelay
        {
            get { return (EffectMask & EffectCutDelayMask) != 0; }
            set { if (value) EffectMask |= EffectCutDelayMask; else EffectMask = (ushort)(EffectMask & ~EffectCutDelayMask); }
        }

        public bool HasAttack
        {
            get { return (Flags & FlagsNoAttack) == 0; }
            set
            {
                Flags = (byte)(Flags & ~FlagsNoAttack);
                if (!value) Flags = (byte)(Flags | FlagsNoAttack);
            }
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
                return "Stop";
            if (value == NoteRelease)
                return "Release";
            if (value == NoteInvalid)
                return "";

            int octave = (value - 1) / 12;
            int note   = (value - 1) % 12;

            return NoteNames[note] + octave.ToString();
        }

        public static int FromFriendlyName(string name)
        {
            if (name == "")
                return Note.NoteInvalid;
            if (name == "Stop")
                return Note.NoteStop;
            if (name == "Release")
                return Note.NoteRelease;

            var note   = Array.IndexOf(NoteNames, name.Substring(0, name.Length - 1));
            var octave = int.Parse(name[name.Length - 1].ToString());

            return octave * 12 + note + 1;
        }
        
        public static int Clamp(int note)
        {
            Debug.Assert(note != NoteInvalid);
            if (note < MusicalNoteMin) return MusicalNoteMin;
            if (note > MusicalNoteMax) return MusicalNoteMax;
            return note;
        }

        public Note Clone()
        {
            return (Note)MemberwiseClone();
        }

        public bool IsEmpty => Value == Note.NoteInvalid && Flags == 0 && Slide == 0 && EffectMask == 0;
        public bool HasJumpOrSkip => FxJump != 0xff || FxSkip != 0xff;

        // Serialization for notes before version 5 (before FamiStudio 2.0.0)
        public void SerializeStatePreVer5(ProjectBuffer buffer)
        {
            buffer.Serialize(ref Value);
            
            // At version 5 (FamiStudio 2.0.0) we refactored the note effects.
            const int SpeedInvalid   = 0xff;
            const int VolumeInvalid  = 0xff;
            const int VibratoInvalid = 0xf0;

            // At version 5 (FamiStudio 2.0.0), we changed the numerical value of the release note.
            if (Value == 0xf7)
                Value = Note.NoteRelease;

            // At version 4 (FamiStudio 1.4.0), we refactored the notes, added slide notes, vibrato and no-attack notes (flags).
            if (buffer.Version >= 4)
            {
                buffer.Serialize(ref FxJump);
                buffer.Serialize(ref FxSkip);
                buffer.Serialize(ref FxSpeed);
                buffer.Serialize(ref FxVibrato);
                buffer.Serialize(ref Flags);
                buffer.Serialize(ref Slide);
            }
            else
            {
                byte effect = 0;
                byte effectParam = 255;
                buffer.Serialize(ref effect);
                buffer.Serialize(ref effectParam);

                FxVolume  = VolumeInvalid;
                FxVibrato = VibratoInvalid;
                FxSpeed   = SpeedInvalid;

                switch (effect)
                {
                    case 1: FxJump  = effectParam; break;
                    case 2: FxSkip  = effectParam; break;
                    case 3: FxSpeed = effectParam; break;
                }
            }

            // At version 3 (FamiStudio 1.2.0), we added a volume track.
            if (buffer.Version >= 3)
                buffer.Serialize(ref FxVolume);

            buffer.Serialize(ref Instrument);

            if (FxVolume  != VolumeInvalid)  EffectMask |= EffectVolumeMask;
            if (FxSpeed   != SpeedInvalid)   EffectMask |= EffectSpeedMask;
            if (FxVibrato != VibratoInvalid) EffectMask |= EffectVibratoMask;
        }

        public void SerializeState(ProjectBuffer buffer)
        {
            buffer.Serialize(ref Value);
            buffer.Serialize(ref Flags);
            buffer.Serialize(ref Slide);
            buffer.Serialize(ref Instrument);
            buffer.Serialize(ref EffectMask);

            if ((EffectMask & EffectVolumeMask)      != 0) buffer.Serialize(ref FxVolume);
            if ((EffectMask & EffectVibratoMask)     != 0) buffer.Serialize(ref FxVibrato);
            if ((EffectMask & EffectSpeedMask)       != 0) buffer.Serialize(ref FxSpeed);
            if ((EffectMask & EffectFinePitchMask)   != 0) buffer.Serialize(ref FxFinePitch);
            if ((EffectMask & EffectFdsModSpeedMask) != 0) buffer.Serialize(ref FxFdsModSpeed);
            if ((EffectMask & EffectFdsModDepthMask) != 0) buffer.Serialize(ref FxFdsModDepth);
            if (buffer.Version >= 8 && (EffectMask & EffectDutyCycleMask) != 0) buffer.Serialize(ref FxDutyCycle);
            if (buffer.Version >= 8 && (EffectMask & EffectNoteDelayMask) != 0) buffer.Serialize(ref FxNoteDelay);
            if (buffer.Version >= 8 && (EffectMask & EffectCutDelayMask)  != 0) buffer.Serialize(ref FxCutDelay);

            // At version 7 (FamiStudio 2.2.0) we added support for arpeggios.
            if (buffer.Version >= 7)
                buffer.Serialize(ref Arpeggio);
        }

        //
        // TODO: Move this to a seperate class, just a way to expose param and render the effect panel.
        //

        public static readonly string[] EffectNames =
        {
            "Volume",
            "Vib Speed",
            "Vib Depth",
            "Pitch",
            "Speed",
            "FDS Depth",
            "FDS Speed",
            "Duty Cycle",
            "Note Delay",
            "Cut Delay"
        };

        public const int EffectVolume       =  0;
        public const int EffectVibratoSpeed =  1; // 4Xy
        public const int EffectVibratoDepth =  2; // 4xY
        public const int EffectFinePitch    =  3; // Pxx
        public const int EffectSpeed        =  4; // Fxx
        public const int EffectFdsModDepth  =  5; // Gxx
        public const int EffectFdsModSpeed  =  6; // Ixx/Jxx
        public const int EffectDutyCycle    =  7; // Vxx
        public const int EffectNoteDelay    =  8; // Gxx
        public const int EffectCutDelay     =  9; // Sxx
        public const int EffectCount        = 10;

        public const int EffectVolumeMask       = (1 << EffectVolume);
        public const int EffectVibratoMask      = (1 << EffectVibratoSpeed) | (1 << EffectVibratoDepth);
        public const int EffectFinePitchMask    = (1 << EffectFinePitch);
        public const int EffectSpeedMask        = (1 << EffectSpeed);
        public const int EffectFdsModDepthMask  = (1 << EffectFdsModDepth);
        public const int EffectFdsModSpeedMask  = (1 << EffectFdsModSpeed);
        public const int EffectDutyCycleMask    = (1 << EffectDutyCycle);
        public const int EffectNoteDelayMask    = (1 << EffectNoteDelay);
        public const int EffectCutDelayMask     = (1 << EffectCutDelay);

        public bool HasValidEffectValue(int fx)
        {
            switch (fx)
            {
                case EffectVolume       : return HasVolume;
                case EffectVibratoDepth : return HasVibrato;
                case EffectVibratoSpeed : return HasVibrato;
                case EffectFinePitch    : return HasFinePitch;
                case EffectSpeed        : return HasSpeed;
                case EffectFdsModDepth  : return HasFdsModDepth;
                case EffectFdsModSpeed  : return HasFdsModSpeed;
                case EffectDutyCycle    : return HasDutyCycle;
                case EffectNoteDelay    : return HasNoteDelay;
                case EffectCutDelay     : return HasCutDelay;
            }

            return false;
        }
        
        public int GetEffectValue(int fx)
        {
            switch (fx)
            {
                case EffectVolume       : return Volume;
                case EffectVibratoDepth : return VibratoDepth;
                case EffectVibratoSpeed : return VibratoSpeed;
                case EffectFinePitch    : return FinePitch;
                case EffectSpeed        : return Speed;
                case EffectFdsModDepth  : return FdsModDepth;
                case EffectFdsModSpeed  : return FdsModSpeed;
                case EffectDutyCycle    : return DutyCycle;
                case EffectNoteDelay    : return NoteDelay;
                case EffectCutDelay     : return CutDelay;
            }

            return 0;
        }

        public void SetEffectValue(int fx, int val)
        {
            switch (fx)
            {
                case EffectVolume       : Volume       =   (byte)val; break;
                case EffectVibratoDepth : VibratoDepth =   (byte)val; break;
                case EffectVibratoSpeed : VibratoSpeed =   (byte)val; break;
                case EffectFinePitch    : FinePitch    =  (sbyte)val; break;
                case EffectSpeed        : Speed        =   (byte)val; break;
                case EffectFdsModDepth  : FdsModDepth  =   (byte)val; break;
                case EffectFdsModSpeed  : FdsModSpeed  = (ushort)val; break;
                case EffectDutyCycle    : DutyCycle    =   (byte)val; break;
                case EffectNoteDelay    : NoteDelay    =   (byte)val; break;
                case EffectCutDelay     : CutDelay     =   (byte)val; break;
            }
        }
        
        public void ClearEffectValue(int fx)
        {
            switch (fx)
            {
                case EffectVolume       : HasVolume      = false; break;
                case EffectVibratoDepth : HasVibrato     = false; break;
                case EffectVibratoSpeed : HasVibrato     = false; break;
                case EffectFinePitch    : HasFinePitch   = false; break;
                case EffectSpeed        : HasSpeed       = false; break;
                case EffectFdsModDepth  : HasFdsModDepth = false; break;
                case EffectFdsModSpeed  : HasFdsModSpeed = false; break;
                case EffectDutyCycle    : HasDutyCycle   = false; break;
                case EffectNoteDelay    : HasNoteDelay   = false; break;
                case EffectCutDelay     : HasCutDelay    = false; break;
            }
        }

        public static bool EffectWantsPreviousValue(int fx)
        {
            return true;
        }

        public static int GetEffectMinValue(Song song, int fx)
        {
            switch (fx)
            {
                case EffectFinePitch:
                    return FinePitchMin;
            }
            return 0;
        }

        public static int GetEffectMaxValue(Song song, int fx)
        {
            switch (fx)
            {
                case EffectVolume       : return VolumeMax;
                case EffectVibratoDepth : return VibratoDepthMax;
                case EffectVibratoSpeed : return VibratoSpeedMax;
                case EffectFinePitch    : return FinePitchMax;
                case EffectSpeed        : return 31;
                case EffectFdsModDepth  : return 63;
                case EffectFdsModSpeed  : return 4095;
                case EffectDutyCycle    : return song.Project.ExpansionAudio == Project.ExpansionVrc6 ? 7 : 3;
                case EffectNoteDelay    : return 31;
                case EffectCutDelay     : return 31;
            }

            return 0;
        }

        public static int GetEffectDefaultValue(Song song, int fx)
        {
            switch (fx)
            {
                case EffectVolume : return VolumeMax;
                case EffectSpeed  : return song.FamitrackerSpeed;
            }

            return 0;
        }
    }
}
