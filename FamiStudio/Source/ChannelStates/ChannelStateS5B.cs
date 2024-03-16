using System;
using System.Diagnostics;

namespace FamiStudio
{
    public class ChannelStateS5B : ChannelState
    {
        private int  channelIdx = 0;
        private int  invToneMask;

        // Last channel will upload these.       
        private int  toneReg = 0x38;
        private int  envPeriod;
        private int  envShape;
        private bool envPeriodEffect;
        private bool envAutoPitch;
        private int  envAutoOctave;
        private bool envReset;
        private int  noiseFreq;

        // From instrument.
        private bool instAutoPitch;
        private int  instAutoOctave;
        private int  instEnvShape;
        private int  instEnvPeriod;

        public ChannelStateS5B(IPlayerInterface player, int apuIdx, int channelType, bool pal) : base(player, apuIdx, channelType, pal)
        {
            channelIdx = channelType - ChannelType.S5BSquare1;
            invToneMask = 0xff - (9 << channelIdx);
        }

        protected override void LoadInstrument(Instrument instrument)
        {
            if (instrument != null)
            {
                Debug.Assert(instrument.IsS5B);

                if (instrument.IsS5B)
                {
                    if (instrument.S5BEnvelopeShape > 0)
                    {
                        instEnvShape   = (byte)(instrument.S5BEnvelopeShape + 7); // 1...8 maps to 0x8...0xf
                        instAutoPitch  = instrument.S5BEnvAutoPitch;
                        instAutoOctave = instrument.S5BEnvAutoPitchOctave;
                        instEnvPeriod  = instrument.S5BEnvelopePitch;
                    }
                    else
                    {
                        instEnvShape = 0;
                    }
                }
            }
        }

        public override void UpdateAPU()
        {
            var lastChannel = player.GetChannelByType(instrumentPlayer ? InnerChannelType : ChannelType.S5BSquare3) as ChannelStateS5B;
            var firstChannelIndex = instrumentPlayer ? channelIdx : 0;
            var lastChannelIndex  = instrumentPlayer ? channelIdx : 2;

            // All channels will update the channel 3 variables. This is pretty ugly
            // but mimics what the assemble code does pretty closely.
            if (channelIdx == firstChannelIndex)
            {
                lastChannel.envAutoPitch = false;
                lastChannel.envReset = false;
                lastChannel.noiseFreq = 0;
                lastChannel.envPeriodEffect = false;
            }

            lastChannel.envReset |= (noteTriggered && instEnvShape > 0);

            if (note.HasEnvelopePeriod)
            {
                lastChannel.envPeriod = note.EnvelopePeriod;
                lastChannel.envPeriodEffect = true;
            }

            if (note.IsStop)
            {
                WriteRegister(NesApu.S5B_ADDR, NesApu.S5B_REG_VOL_A + channelIdx);
                WriteRegister(NesApu.S5B_DATA, 0);
            }
            else if (note.IsMusical)
            {
                var period = GetPeriod() + 1; // Unlike the 2A03 and VRC6 pulse channels' frequency formulas, the formula for 5B does not add 1 to the period.
                var volume = GetVolume();

                var periodHi = (period >> 8) & 0x0f;
                var periodLo = (period >> 0) & 0xff;

                WriteRegister(NesApu.S5B_ADDR, NesApu.S5B_REG_LO_A + channelIdx * 2);
                WriteRegister(NesApu.S5B_DATA, periodLo);
                WriteRegister(NesApu.S5B_ADDR, NesApu.S5B_REG_HI_A + channelIdx * 2);
                WriteRegister(NesApu.S5B_DATA, periodHi);
                WriteRegister(NesApu.S5B_ADDR, NesApu.S5B_REG_VOL_A + channelIdx);
                WriteRegister(NesApu.S5B_DATA, volume | (instEnvShape != 0 ? 0x10 : 0x00));

                var mixerEnv  = envelopeValues[EnvelopeType.S5BMixer];
                var noiseFreq = envelopeValues[EnvelopeType.S5BNoiseFreq];
                var noiseEnabled = (mixerEnv & 0x2) == 0;
                lastChannel.toneReg = (lastChannel.toneReg & invToneMask) | (((mixerEnv & 1) | ((mixerEnv & 0x2) << 2)) << channelIdx);
                lastChannel.noiseFreq = noiseEnabled ? noiseFreq : lastChannel.noiseFreq;

                if (instEnvShape != 0)
                {
                    lastChannel.envPeriod = instAutoPitch ? period : (lastChannel.envPeriodEffect || !noteTriggered ? lastChannel.envPeriod : instEnvPeriod);
                    lastChannel.envShape = instEnvShape;
                    lastChannel.envAutoOctave = instAutoOctave;
                    lastChannel.envAutoPitch = instAutoPitch;
                }
            }

            // Last channel will be in charge of writing to the shared registers.
            if (channelIdx == lastChannelIndex)
            {
                if (envAutoPitch)
                {
                    if (envAutoOctave > 0)
                    {
                        envPeriod >>= Math.Abs(envAutoOctave) - 1;
                        if ((envPeriod & 1) != 0) envPeriod++;
                        envPeriod >>= 1;
                    }
                    else
                    {
                        envPeriod <<= Math.Abs(envAutoOctave);
                    }
                }

                var envPeriodLo = (envPeriod >> 0) & 0xff;
                var envPeriodHi = (envPeriod >> 8) & 0xff;

                WriteRegister(NesApu.S5B_ADDR, NesApu.S5B_REG_ENV_LO);
                WriteRegister(NesApu.S5B_DATA, envPeriodLo);
                WriteRegister(NesApu.S5B_ADDR, NesApu.S5B_REG_ENV_HI);
                WriteRegister(NesApu.S5B_DATA, envPeriodHi);
                WriteRegister(NesApu.S5B_ADDR, NesApu.S5B_REG_NOISE_FREQ);
                WriteRegister(NesApu.S5B_DATA, noiseFreq);
                WriteRegister(NesApu.S5B_ADDR, NesApu.S5B_REG_MIXER_SETTING);
                WriteRegister(NesApu.S5B_DATA, toneReg);

                if (envReset)
                {
                    WriteRegister(NesApu.S5B_ADDR, NesApu.S5B_REG_SHAPE);
                    WriteRegister(NesApu.S5B_DATA, envShape);
                }
            }

            // HACK : There are conflicts between N163 registers and S5B register, a N163 addr write
            // can be interpreted as a S5B data write. To prevent this, we select a dummy register 
            // for S5B so that these writes will be discarded.
            //
            // N163: 
            //   f800-ffff (addr)
            //   4800-4fff (data)
            // S5B:
            //   c000-e000 (addr)
            //   f000-ffff (data)

            if ((NesApu.GetAudioExpansions(apuIdx) & NesApu.APU_EXPANSION_MASK_NAMCO) != 0)
                WriteRegister(NesApu.S5B_ADDR, NesApu.S5B_REG_IO_A);

            base.UpdateAPU();
        }
    };
}
