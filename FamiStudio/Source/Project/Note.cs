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

        public const int VolumeMax       = 0x0f;
        public const int VibratoSpeedMax = 0x0c;
        public const int VibratoDepthMax = 0x0f;
        public const int FinePitchMin    = -128;
        public const int FinePitchMax    =  127;

        public const int FlagsNone       = 0x00;
        public const int FlagsNoAttack   = 0x01;
                                         
        public const int NoteInvalid     = 0xff;
        public const int MusicalNoteMin  = 0x01;
        public const int MusicalNoteMax  = 0x60;
        public const int MusicalNoteC4   = 0x31;
        public const int MusicalNoteC7   = 0x55;

        // TODO : Move to "EffectType".
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
        public const int EffectVolumeSlide  = 10; // Axy
        public const int EffectDeltaCounter = 11; // Zxx
        public const int EffectPhaseReset   = 12; // =xx (is famitracker running out of letters? lol)
        public const int EffectCount        = 13; 

        public const int EffectVolumeMask         = (1 << EffectVolume);
        public const int EffectVibratoMask        = (1 << EffectVibratoSpeed) | (1 << EffectVibratoDepth);
        public const int EffectFinePitchMask      = (1 << EffectFinePitch);
        public const int EffectSpeedMask          = (1 << EffectSpeed);
        public const int EffectFdsModDepthMask    = (1 << EffectFdsModDepth);
        public const int EffectFdsModSpeedMask    = (1 << EffectFdsModSpeed);
        public const int EffectDutyCycleMask      = (1 << EffectDutyCycle);
        public const int EffectNoteDelayMask      = (1 << EffectNoteDelay);
        public const int EffectCutDelayMask       = (1 << EffectCutDelay);
        public const int EffectVolumeSlideMask    = (1 << EffectVolumeSlide);
        public const int EffectVolumeAndSlideMask = (1 << EffectVolume) | (1 << EffectVolumeSlide);
        public const int EffectDeltaCounterMask   = (1 << EffectDeltaCounter);
        public const int EffectPhaseResetMask     = (1 << EffectPhaseReset);

        public const int EffectAllMask = 0x1fff; // Must be updated every time a new effect is added.

        // As of FamiStudio 3.0.0, these are semi-deprecated and mostly used in parts of the
        // code that have not been migrated to compound notes (notes with release and duration) 
        // yet. The piano roll does note handle those AT ALL.
        public const int NoteStop    = 0x00;
        public const int NoteRelease = 0x80;

        public readonly static Note EmptyNote = new Note();

        // General properties.
        private byte       val = NoteInvalid; // (0 = stop, 1 = C0 ... 96 = B7).
        private byte       flags;
        private byte       slide;
        private ushort     effectMask;
        private ushort     duration;
        private ushort     release;
        private Instrument instrument;
        private Arpeggio   arpeggio;

        // Effects.
        private byte   volume;
        private byte   volumeSlide;
        private byte   vibrato;
        private byte   speed;
        private sbyte  finePitch;
        private byte   fdsModDepth;
        private ushort fdsModSpeed;
        private byte   dutyCycle;
        private byte   noteDelay;
        private byte   cutDelay;
        private byte   dmcCounter;
        private byte   phaseReset;

        // As of version 5 (FamiStudio 2.0.0), these are deprecated and are only kept around
        // for migration.
        private byte jump = 0xff;
        private byte skip = 0xff;

        public byte Jump => jump;
        public byte Skip => skip;

        public Note()
        {
        }

        public Note(int value)
        {
            val = (byte)value;
        }

        public void Clear(bool preserveFx = true)
        {
            val = NoteInvalid;
            instrument = null;
            arpeggio = null;
            slide = 0;
            flags = 0;
            duration = 0;
            release = 0;

            if (!preserveFx)
            {
                effectMask = 0;
                volume = 0;
                volumeSlide = 0;
                vibrato = 0;
                speed = 0;
                finePitch = 0;
                fdsModDepth = 0;
                fdsModSpeed = 0;
                dutyCycle = 0;
                noteDelay = 0;
                cutDelay = 0;
                dmcCounter = NesApu.DACDefaultValue;
            }
        }

        public void ClearJumpSkip()
        {
            jump = 0xff;
            skip = 0xff;
        }

        public byte Value
        {
            get { return val; }
            set { val = value; }
        }

        public byte Flags
        {
            get { return flags; }
            set { flags = value; }
        }

        public ushort EffectMask
        {
            get { return effectMask; }
        }

        public int Duration
        {
            get { return duration; }
            set
            {
                if (IsMusical)
                {
                    duration = (ushort)Utils.Clamp(value, 1, (int)ushort.MaxValue);
                    ClearReleaseIfPastDuration();
                }
                else if (IsStop || IsRelease)
                {
                    Debug.Assert(value == 1);
                    duration = 1;
                }
                else
                {
                    Debug.Assert(value == 0);
                    duration = 0;
                }
            }
        }

        public int Release
        {
            get { return release; }
            set
            {
                if (IsMusical)
                {
                    release = (ushort)Utils.Clamp(value, 0, (int)ushort.MaxValue);
                    ClearReleaseIfPastDuration();
                }
            }
        }

        public void SetReleaseNoDurationCheck(int value)
        {
            if (IsMusical)
                release = (ushort)Math.Min(value, (int)ushort.MaxValue);
        }

        public Instrument Instrument
        {
            get { return instrument; }
            set { instrument = value; }
        }

        public Arpeggio Arpeggio
        {
            get => arpeggio;
            set => arpeggio = value;
        }

        public bool IsMusicalOrStop
        {
            get { return IsMusical || IsStop; }
        }

        public bool IsValid
        {
            get { return Value != NoteInvalid; }
            set { if (!value) Value = NoteInvalid; }
        }

        public bool IsStop
        {
            get { return val == NoteStop; }
            set { val = (byte)(value ? NoteStop : NoteInvalid); }
        }

        public bool HasRelease
        {
            get { return release > 0; }
            set { if (!value) release = 0; }
        }

        public bool IsRelease
        {
            get { return val == NoteRelease; }
        }

        public bool IsMusical
        {
            get { return IsValid && !IsStop && !IsRelease; }
        }

        public bool IsSlideNote
        {
            get { return slide != 0; }
            set { if (!value) slide = 0; }
        }

        public byte SlideNoteTarget
        {
            get { return slide; }
            set { slide = value; }
        }

        public bool IsArpeggio
        {
            get { return arpeggio != null; }
            set { if (!value) arpeggio = null; }
        }

        public byte Volume
        {
            get { Debug.Assert(HasVolume); return volume; }
            set { volume = (byte)Utils.Clamp(value, 0, VolumeMax); HasVolume = true; }
        }

        public byte VolumeSlideTarget
        {
            get { Debug.Assert(HasVolume); Debug.Assert(HasVolumeSlide); return volumeSlide; }
            set { volumeSlide = (byte)Utils.Clamp(value, 0, VolumeMax); HasVolumeSlide = true; }
        }

        public byte RawVibrato
        {
            get { return vibrato; }
            set
            {
                vibrato  = value;
                HasVibrato = true;
            }
        }

        public byte VibratoSpeed
        {
            get { Debug.Assert(HasVibrato); return (byte)(vibrato >> 4); }
            set
            {
                vibrato &= 0x0f;
                vibrato |= (byte)((byte)Utils.Clamp(value, 0, VibratoSpeedMax) << 4);
                HasVibrato = true;
            }
        }

        public byte VibratoDepth
        {
            get { Debug.Assert(HasVibrato); return (byte)(vibrato & 0x0f); }
            set
            {
                vibrato &= 0xf0;
                vibrato |= (byte)Utils.Clamp(value, 0, VibratoDepthMax);

                if (HasVibrato)
                    VibratoSpeed = (byte)Utils.Clamp(VibratoSpeed, 0, VibratoSpeedMax);

                HasVibrato = true;
            }
        }

        public sbyte FinePitch
        {
            get { Debug.Assert(HasFinePitch); return finePitch; }
            set { finePitch = value; HasFinePitch = true; }
        }

        public byte Speed
        {
            get { Debug.Assert(HasSpeed); return speed; }
            set { speed = (byte)Utils.Clamp(value, 0, 31); HasSpeed = true; }
        }

        public byte FdsModDepth
        {
            get { Debug.Assert(HasFdsModDepth); return fdsModDepth; }
            set { fdsModDepth = (byte)Utils.Clamp(value, 0, 63); HasFdsModDepth = true; }
        }

        public ushort FdsModSpeed
        {
            get { Debug.Assert(HasFdsModSpeed); return fdsModSpeed; }
            set { fdsModSpeed = (ushort)Utils.Clamp(value, 0, 4095); HasFdsModSpeed = true; }
        }

        public byte DutyCycle
        {
            get { Debug.Assert(HasDutyCycle); return dutyCycle; }
            set { dutyCycle = (byte)Utils.Clamp(value, 0, 7); HasDutyCycle = true; }
        }

        public byte NoteDelay
        {
            get { Debug.Assert(HasNoteDelay); return noteDelay; }
            set { noteDelay = (byte)Utils.Clamp(value, 0, 31); HasNoteDelay = true; }
        }

        public byte CutDelay
        {
            get { Debug.Assert(HasCutDelay); return cutDelay; }
            set { cutDelay = (byte)Utils.Clamp(value, 0, 31); HasCutDelay = true; }
        }

        public byte DeltaCounter
        {
            get { Debug.Assert(HasDeltaCounter); return dmcCounter; }
            set { dmcCounter = (byte)Utils.Clamp(value, 0, 127); HasDeltaCounter = true; }
        }

        public byte PhaseReset
        {
            get { Debug.Assert(HasPhaseReset); return phaseReset; }
            set { phaseReset = (byte)Utils.Clamp(value, 1, 1); HasPhaseReset = true; }
        }

        public bool HasVolume
        {
            get { return (effectMask & EffectVolumeMask) != 0; }
            set { if (value) effectMask |= EffectVolumeMask; else effectMask = (ushort)(effectMask & ~EffectVolumeAndSlideMask); }
        }
        
        public bool HasVolumeSlide
        {
            get { return (effectMask & EffectVolumeAndSlideMask) == EffectVolumeAndSlideMask; }
            set { if (value) effectMask |= EffectVolumeAndSlideMask; else effectMask = (ushort)(effectMask & ~EffectVolumeSlideMask); }
        }

        public bool HasVibrato
        {
            get { return (effectMask & EffectVibratoMask) != 0; }
            set { if (value) effectMask |= EffectVibratoMask; else effectMask = (ushort)(effectMask & ~EffectVibratoMask); }
        }

        public bool HasFinePitch
        {
            get { return (effectMask & EffectFinePitchMask) != 0; }
            set { if (value) effectMask |= EffectFinePitchMask; else effectMask = (ushort)(effectMask & ~EffectFinePitchMask); }
        }

        public bool HasSpeed
        {
            get { return (effectMask & EffectSpeedMask) != 0; }
            set { if (value) effectMask |= EffectSpeedMask; else effectMask = (ushort)(effectMask & ~EffectSpeedMask); }
        }

        public bool HasFdsModDepth
        {
            get { return (effectMask & EffectFdsModDepthMask) != 0; }
            set { if (value) effectMask |= EffectFdsModDepthMask; else effectMask = (ushort)(effectMask & ~EffectFdsModDepthMask); }
        }

        public bool HasFdsModSpeed
        {
            get { return (effectMask & EffectFdsModSpeedMask) != 0; }
            set { if (value) effectMask |= EffectFdsModSpeedMask; else effectMask = (ushort)(effectMask & ~EffectFdsModSpeedMask); }
        }

        public bool HasDutyCycle
        {
            get { return (effectMask & EffectDutyCycleMask) != 0; }
            set { if (value) effectMask |= EffectDutyCycleMask; else effectMask = (ushort)(effectMask & ~EffectDutyCycleMask); }
        }

        public bool HasNoteDelay
        {
            get { return (effectMask & EffectNoteDelayMask) != 0; }
            set { if (value) effectMask |= EffectNoteDelayMask; else effectMask = (ushort)(effectMask & ~EffectNoteDelayMask); }
        }

        public bool HasCutDelay
        {
            get { return (effectMask & EffectCutDelayMask) != 0; }
            set { if (value) effectMask |= EffectCutDelayMask; else effectMask = (ushort)(effectMask & ~EffectCutDelayMask); }
        }

        public bool HasDeltaCounter
        {
            get { return (effectMask & EffectDeltaCounterMask) != 0; }
            set { if (value) effectMask |= EffectDeltaCounterMask; else effectMask = (ushort)(effectMask & ~EffectDeltaCounterMask); }
        }

        public bool HasPhaseReset
        {
            get { return (effectMask & EffectPhaseResetMask) != 0; }
            set { if (value) effectMask |= EffectPhaseResetMask; else effectMask = (ushort)(effectMask & ~EffectPhaseResetMask); }
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
        
        public bool HasAnyEffect
        {
            get { return effectMask != 0; }
        }

        public string FriendlyName
        {
            get
            {
                return GetFriendlyName(Value);
            }
        }

        public void ClearReleaseIfPastDuration()
        {
            if (HasRelease && release >= duration)
                release = 0;
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

        public static bool IsMusicalNote(int note)
        {
            return note >= MusicalNoteMin && note <= MusicalNoteMax;
        }

        public static void GetOctaveAndNote(int note, out int octave, out int octaveNote)
        {
            octave     = (note - 1) / 12;
            octaveNote = (note - 1) - octave * 12;
        }

        public Note Clone()
        {
            return (Note)MemberwiseClone();
        }

        public override string ToString()
        {
            return IsMusical ? FriendlyName : base.ToString();
        }

        public bool IsEmpty => Value == Note.NoteInvalid && Flags == 0 && SlideNoteTarget == 0 && EffectMask == 0;
        public bool HasJumpOrSkip => jump != 0xff || skip != 0xff;

        // To fix some bad data from old versions.
        // NOTE : Keeping notes if they have jumps/skips since they will be cleaned up once the file is completely loaded.
        public bool IsUseless => (IsSlideNote || instrument != null) && !IsValid && Flags == 0 && EffectMask == 0;

        public bool MatchesFilter(NoteFilter filter)
        {
            if (IsMusical && filter.HasFlag(NoteFilter.Musical))
                return true;
            if (IsStop && filter.HasFlag(NoteFilter.Stop))
                return true;
            if (((int)filter & (effectMask << 16)) != 0)
                return true;

            return false;
        }

        // Serialization for notes before version 5 (before FamiStudio 2.0.0)
        public void SerializeStatePreVer5(ProjectBuffer buffer)
        {
            buffer.Serialize(ref val);
            
            // At version 5 (FamiStudio 2.0.0) we refactored the note effects.
            const int SpeedInvalid   = 0xff;
            const int VolumeInvalid  = 0xff;
            const int VibratoInvalid = 0xf0;

            // At version 5 (FamiStudio 2.0.0), we changed the numerical value of the release note.
            if (val == 0xf7)
                val = Note.NoteRelease;

            // At version 4 (FamiStudio 1.4.0), we refactored the notes, added slide notes, vibrato and no-attack notes (flags).
            if (buffer.Version >= 4)
            {
                buffer.Serialize(ref jump);
                buffer.Serialize(ref skip);
                buffer.Serialize(ref speed);
                buffer.Serialize(ref vibrato);
                buffer.Serialize(ref flags);
                buffer.Serialize(ref slide);
            }
            else
            {
                byte effect = 0;
                byte effectParam = 255;
                buffer.Serialize(ref effect);
                buffer.Serialize(ref effectParam);

                volume  = VolumeInvalid;
                vibrato = VibratoInvalid;
                speed   = SpeedInvalid;

                switch (effect)
                {
                    case 1: jump  = effectParam; break;
                    case 2: skip  = effectParam; break;
                    case 3: speed = effectParam; break;
                }
            }

            // At version 3 (FamiStudio 1.2.0), we added a volume track.
            if (buffer.Version >= 3)
                buffer.Serialize(ref volume);

            buffer.Serialize(ref instrument);

            if (volume  != VolumeInvalid)  effectMask |= EffectVolumeMask;
            if (speed   != SpeedInvalid)   effectMask |= EffectSpeedMask;
            if (vibrato != VibratoInvalid) effectMask |= EffectVibratoMask;
        }

        public void Serialize(ProjectBuffer buffer)
        {
            buffer.Serialize(ref val);
            buffer.Serialize(ref flags);

            if (buffer.Version < 10 || IsMusical)
            {
                buffer.Serialize(ref slide);
                buffer.Serialize(ref instrument);
            }

            // At version 10 (FamiStudio 3.0.0), we switched to compound notes.
            if (buffer.Version >= 10)
            {
                if (IsMusical)
                {
                    buffer.Serialize(ref duration);
                    buffer.Serialize(ref release);
                }
                else if (buffer.IsReading && IsStop)
                {
                    duration = 1;
                    instrument = null;
                }
            }

            buffer.Serialize(ref effectMask);

            if ((EffectMask & EffectVolumeMask)      != 0) buffer.Serialize(ref volume);
            if ((EffectMask & EffectVibratoMask)     != 0) buffer.Serialize(ref vibrato);
            if ((EffectMask & EffectSpeedMask)       != 0) buffer.Serialize(ref speed);
            if ((EffectMask & EffectFinePitchMask)   != 0) buffer.Serialize(ref finePitch);
            if ((EffectMask & EffectFdsModSpeedMask) != 0) buffer.Serialize(ref fdsModSpeed);
            if ((EffectMask & EffectFdsModDepthMask) != 0) buffer.Serialize(ref fdsModDepth);
            if (buffer.Version >=  8 && (EffectMask & EffectDutyCycleMask) != 0) buffer.Serialize(ref dutyCycle);
            if (buffer.Version >=  8 && (EffectMask & EffectNoteDelayMask) != 0) buffer.Serialize(ref noteDelay);
            if (buffer.Version >=  8 && (EffectMask & EffectCutDelayMask)  != 0) buffer.Serialize(ref cutDelay);
            if (buffer.Version >= 11 && (EffectMask & EffectVolumeAndSlideMask) == EffectVolumeAndSlideMask) buffer.Serialize(ref volumeSlide);
            if (buffer.Version >= 13 && (EffectMask & EffectDeltaCounterMask)   == EffectDeltaCounterMask)   buffer.Serialize(ref dmcCounter);
            if (buffer.Version >= 15 && (EffectMask & EffectPhaseResetMask)     == EffectPhaseResetMask)     buffer.Serialize(ref phaseReset);

            // At version 7 (FamiStudio 2.2.0) we added support for arpeggios.
            if (buffer.Version >= 7)
                buffer.Serialize(ref arpeggio);
        }

        public bool HasValidEffectValue(int fx)
        {
            switch (fx)
            {
                case EffectVolume       : return HasVolume;
                case EffectVolumeSlide  : return HasVolumeSlide;
                case EffectVibratoDepth : return HasVibrato;
                case EffectVibratoSpeed : return HasVibrato;
                case EffectFinePitch    : return HasFinePitch;
                case EffectSpeed        : return HasSpeed;
                case EffectFdsModDepth  : return HasFdsModDepth;
                case EffectFdsModSpeed  : return HasFdsModSpeed;
                case EffectDutyCycle    : return HasDutyCycle;
                case EffectNoteDelay    : return HasNoteDelay;
                case EffectCutDelay     : return HasCutDelay;
                case EffectDeltaCounter : return HasDeltaCounter;
                case EffectPhaseReset   : return HasPhaseReset;
            }

            return false;
        }
        
        public int GetEffectValue(int fx)
        {
            switch (fx)
            {
                case EffectVolume       : return Volume;
                case EffectVolumeSlide  : return VolumeSlideTarget;
                case EffectVibratoDepth : return VibratoDepth;
                case EffectVibratoSpeed : return VibratoSpeed;
                case EffectFinePitch    : return FinePitch;
                case EffectSpeed        : return Speed;
                case EffectFdsModDepth  : return FdsModDepth;
                case EffectFdsModSpeed  : return FdsModSpeed;
                case EffectDutyCycle    : return DutyCycle;
                case EffectNoteDelay    : return NoteDelay;
                case EffectCutDelay     : return CutDelay;
                case EffectDeltaCounter : return DeltaCounter;
                case EffectPhaseReset   : return PhaseReset;
            }

            return 0;
        }

        public void SetEffectValue(int fx, int val)
        {
            switch (fx)
            {
                case EffectVolume       : Volume            = (byte)Utils.Clamp(val, byte.MinValue, byte.MaxValue); break;
                case EffectVolumeSlide  : VolumeSlideTarget = (byte)Utils.Clamp(val, byte.MinValue, byte.MaxValue); break;
                case EffectVibratoDepth : VibratoDepth      = (byte)Utils.Clamp(val, byte.MinValue, byte.MaxValue); break;
                case EffectVibratoSpeed : VibratoSpeed      = (byte)Utils.Clamp(val, byte.MinValue, byte.MaxValue); break;
                case EffectFinePitch    : FinePitch         = (sbyte)Utils.Clamp(val, sbyte.MinValue, sbyte.MaxValue); break;
                case EffectSpeed        : Speed             = (byte)Utils.Clamp(val, byte.MinValue, byte.MaxValue); break;
                case EffectFdsModDepth  : FdsModDepth       = (byte)Utils.Clamp(val, byte.MinValue, byte.MaxValue); break;
                case EffectFdsModSpeed  : FdsModSpeed       = (ushort)Utils.Clamp(val, ushort.MinValue, ushort.MaxValue); break;
                case EffectDutyCycle    : DutyCycle         = (byte)Utils.Clamp(val, byte.MinValue, byte.MaxValue); break;
                case EffectNoteDelay    : NoteDelay         = (byte)Utils.Clamp(val, byte.MinValue, byte.MaxValue); break;
                case EffectCutDelay     : CutDelay          = (byte)Utils.Clamp(val, byte.MinValue, byte.MaxValue); break;
                case EffectDeltaCounter : DeltaCounter      = (byte)Utils.Clamp(val, byte.MinValue, byte.MaxValue); break;
                case EffectPhaseReset   : PhaseReset        = (byte)Utils.Clamp(val, 1, 1); break;
            }
        }
        
        public void ClearEffectValue(int fx)
        {
            switch (fx)
            {
                case EffectVolume       : HasVolume       = false; break;
                case EffectVolumeSlide  : HasVolumeSlide  = false; break;
                case EffectVibratoDepth : HasVibrato      = false; break;
                case EffectVibratoSpeed : HasVibrato      = false; break;
                case EffectFinePitch    : HasFinePitch    = false; break;
                case EffectSpeed        : HasSpeed        = false; break;
                case EffectFdsModDepth  : HasFdsModDepth  = false; break;
                case EffectFdsModSpeed  : HasFdsModSpeed  = false; break;
                case EffectDutyCycle    : HasDutyCycle    = false; break;
                case EffectNoteDelay    : HasNoteDelay    = false; break;
                case EffectCutDelay     : HasCutDelay     = false; break;
                case EffectDeltaCounter : HasDeltaCounter = false; break;
                case EffectPhaseReset   : HasPhaseReset   = false; break;
            }
        }

        public static bool EffectWantsPreviousValue(int fx)
        {
            switch (fx)
            {
                case EffectNoteDelay:
                case EffectCutDelay:
                    return false;
            }

            return true;
        }

        public static int GetEffectMinValue(Song song, Channel channel, int fx)
        {
            switch (fx)
            {
                case EffectFinePitch  : return FinePitchMin;
                case EffectSpeed      : return 1;
                case EffectPhaseReset : return 1;
            }
            return 0;
        }

        public static int GetEffectMaxValue(Song song, Channel channel, int fx)
        {
            switch (fx)
            {
                case EffectVolume       : return VolumeMax;
                case EffectVolumeSlide  : return VolumeMax;
                case EffectVibratoDepth : return VibratoDepthMax;
                case EffectVibratoSpeed : return VibratoSpeedMax;
                case EffectFinePitch    : return FinePitchMax;
                case EffectSpeed        : return 31;
                case EffectFdsModDepth  : return 63;
                case EffectFdsModSpeed  : return 4095;
                case EffectDutyCycle    : return channel.IsVrc6Channel ? 7 : 3;
                case EffectNoteDelay    : return 31;
                case EffectCutDelay     : return 31;
                case EffectDeltaCounter : return 127;
                case EffectPhaseReset   : return 1;
            }

            return 0;
        }

        public static int ClampEffectValue(Song song, Channel channel, int fx, int value)
        {
            return Utils.Clamp(
                value,
                GetEffectMinValue(song, channel, fx),
                GetEffectMaxValue(song, channel, fx));
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

        public static NoteFilter GetFilterForEffect(int fx)
        {
            return (NoteFilter)(1 << (fx + 16));
        }
    }

    public static class EffectType
    {
        public static LocalizedString[] LocalizedNames = new LocalizedString[Note.EffectCount];

        public static readonly string[] Icons = new string[]
        {
            "EffectVolume",
            "EffectVibrato",
            "EffectVibrato",
            "EffectPitch",
            "EffectSpeed",
            "EffectMod",
            "EffectMod",
            "EffectDutyCycle",
            "EffectNoteDelay",
            "EffectCutDelay",
            "EffectVolume",
            "EffectDAC",
            "EffectPhaseReset",
        };

        static EffectType()
        {
            Localization.LocalizeStatic(typeof(EffectType));
        }
    }

    [Flags]
    public enum NoteFilter
    {
        None    = 0,
        Musical = 1,
        Stop    = 2,

        // All effects are in the upper 16-bit.
        EffectVolume      = Note.EffectVolumeMask      << 16,
        EffectVibrato     = Note.EffectVibratoMask     << 16,
        EffectFinePitch   = Note.EffectFinePitchMask   << 16,
        EffectSpeed       = Note.EffectSpeedMask       << 16,
        EffectFdsModDepth = Note.EffectFdsModDepthMask << 16,
        EffectFdsModSpeed = Note.EffectFdsModSpeedMask << 16,
        EffectDutyCycle   = Note.EffectDutyCycleMask   << 16,
        EffectNoteDelay   = Note.EffectNoteDelayMask   << 16,
        EffectCutDelay    = Note.EffectCutDelayMask    << 16,

        // There are all the filter that can interrupt the duration of a note.
        CutDurationMask = Musical | Stop | EffectCutDelay,

        All = -1,
    }
}
