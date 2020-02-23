using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FamiStudio
{
    public class FamitrackerFileBase
    {
        protected static readonly int[] VibratoSpeedImportLookup = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 10, 11, 11, 11, 12 };
        protected static readonly int[] VibratoSpeedExportLookup = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 12, 15 };

        protected const byte SndChip_NONE = 0;
        protected const byte SndChip_VRC6 = 1;  // Konami VRCVI
        protected const byte SndChip_VRC7 = 2;  // Konami VRCVII
        protected const byte SndChip_FDS = 4;  // Famicom Disk Sound
        protected const byte SndChip_MMC5 = 8;  // Nintendo MMC5
        protected const byte SndChip_N163 = 16; // Namco N-106
        protected const byte SndChip_S5B = 32; // Sunsoft 5B

        protected const byte Effect_None = 0;
        protected const byte Effect_Speed = 1;
        protected const byte Effect_Jump = 2;
        protected const byte Effect_Skip = 3;
        protected const byte Effect_Halt = 4;
        protected const byte Effect_Volume = 5;
        protected const byte Effect_Portamento = 6;
        protected const byte Effect_Sweepup = 8;
        protected const byte Effect_Sweepdown = 9;
        protected const byte Effect_Arpeggio = 10;
        protected const byte Effect_Vibrato = 11;
        protected const byte Effect_Tremolo = 12;
        protected const byte Effect_Pitch = 13;
        protected const byte Effect_Delay = 14;
        protected const byte Effect_Dac = 15;
        protected const byte Effect_PortaUp = 16;
        protected const byte Effect_PortaDown = 17;
        protected const byte Effect_DutyCycle = 18;
        protected const byte Effect_SampleOffset = 19;
        protected const byte Effect_SlideUp = 20;
        protected const byte Effect_SlideDown = 21;
        protected const byte Effect_VolumeSlide = 22;
        protected const byte Effect_NoteCut = 23;
        protected const byte Effect_Retrigger = 24;
        protected const byte Effect_FdsModDepth = 26;
        protected const byte Effect_FdsModSpeedHi = 27;
        protected const byte Effect_FdsModSpeedLo = 28;
        protected const byte Effect_DpcmPitch = 29;
        protected const byte Effect_SunsoftEnvLo = 30;
        protected const byte Effect_SunsoftEnvHi = 31;
        protected const byte Effect_SunsoftEnvType = 32;
        protected const byte Effect_Count = 33;

        protected static readonly int[] ChanIdLookup = new[]
        {
            Channel.Square1,        // CHANID_SQUARE1
            Channel.Square2,        // CHANID_SQUARE2
            Channel.Triangle,       // CHANID_TRIANGLE
            Channel.Noise,          // CHANID_NOISE
            Channel.Dpcm,           // CHANID_DPCM
            Channel.Vrc6Square1,    // CHANID_VRC6_PULSE1
            Channel.Vrc6Square2,    // CHANID_VRC6_PULSE2
            Channel.Vrc6Saw,        // CHANID_VRC6_SAWTOOTH
            Channel.Mmc5Square1,    // CHANID_MMC5_SQUARE1
            Channel.Mmc5Square2,    // CHANID_MMC5_SQUARE2
            Channel.Mmc5Dpcm,       // CHANID_MMC5_VOICE
            Channel.NamcoWave1,     // CHANID_N163_CHAN1
            Channel.NamcoWave2,     // CHANID_N163_CHAN2
            Channel.NamcoWave3,     // CHANID_N163_CHAN3
            Channel.NamcoWave4,     // CHANID_N163_CHAN4
            Channel.NamcoWave5,     // CHANID_N163_CHAN5
            Channel.NamcoWave6,     // CHANID_N163_CHAN6
            Channel.NamcoWave7,     // CHANID_N163_CHAN7
            Channel.NamcoWave8,     // CHANID_N163_CHAN8
            Channel.FdsWave,        // CHANID_FDS
            Channel.Vrc7Fm1,        // CHANID_VRC7_CH1
            Channel.Vrc7Fm2,        // CHANID_VRC7_CH2
            Channel.Vrc7Fm3,        // CHANID_VRC7_CH3
            Channel.Vrc7Fm4,        // CHANID_VRC7_CH4
            Channel.Vrc7Fm5,        // CHANID_VRC7_CH5
            Channel.Vrc7Fm6,        // CHANID_VRC7_CH6
            Channel.SunsoftSquare1, // CHANID_S5B_CH1
            Channel.SunsoftSquare2, // CHANID_S5B_CH2
            Channel.SunsoftSquare3  // CHANID_S5B_CH3
        };

        protected static int[] InstrumentTypeLookup =
        {
            Project.ExpansionCount,  // INST_NONE: Should never happen.
            Project.ExpansionNone,   // INST_2A03
            Project.ExpansionVrc6,   // INST_VRC6
            Project.ExpansionVrc7,   // INST_VRC7
            Project.ExpansionFds,    // INST_FDS
            Project.ExpansionNamco,  // INST_N163
            Project.ExpansionSunsoft // INST_S5B
        };

        protected static int[] EnvelopeTypeLookup =
        {
            Envelope.Volume,   // SEQ_VOLUME
            Envelope.Arpeggio, // SEQ_ARPEGGIO
            Envelope.Pitch,    // SEQ_PITCH
            Envelope.Max,      // SEQ_HIPITCH
            Envelope.DutyCycle // SEQ_DUTYCYCLE
        };

        protected struct RowFxData
        {
            public byte fx;
            public byte param;
        }

        protected int ConvertExpansionAudio(int exp)
        {
            switch (exp)
            {
                case SndChip_VRC6 : return Project.ExpansionVrc6;
                case SndChip_VRC7 : return Project.ExpansionVrc7;
                case SndChip_FDS  : return Project.ExpansionFds;
                case SndChip_MMC5 : return Project.ExpansionMmc5;
                case SndChip_N163 : return Project.ExpansionNamco;
                case SndChip_S5B  : return Project.ExpansionSunsoft;
            }

            return -1; // We dont support exotic combinations.
        }

        protected void ApplySimpleEffects(RowFxData fx, Pattern pattern, int n, Dictionary<Pattern, byte> patternLengths)
        {
            switch (fx.fx)
            {
                case Effect_Jump:
                    pattern.Song.SetLoopPoint(fx.param);
                    break;
                case Effect_Skip:
                    patternLengths[pattern] = (byte)(n + 1);
                    break;
                case Effect_Speed:
                    if (fx.param <= 0x1f) // We only support speed change for now.
                        pattern.Notes[n].Speed = fx.param;
                    break;
                case Effect_Pitch:
                    pattern.Notes[n].FinePitch = (sbyte)(0x80 - fx.param);
                    break;
                case Effect_Vibrato:
                    pattern.Notes[n].VibratoDepth = (byte)(fx.param & 0x0f);
                    pattern.Notes[n].VibratoSpeed = (byte)VibratoSpeedImportLookup[fx.param >> 4];

                    if (pattern.Notes[n].VibratoDepth == 0 ||
                        pattern.Notes[n].VibratoSpeed == 0)
                    {
                        pattern.Notes[n].Vibrato = 0;
                    }
                    break;
            }
        }
    };
}
