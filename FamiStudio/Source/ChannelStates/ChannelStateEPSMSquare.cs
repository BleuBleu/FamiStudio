using System;
using System.Diagnostics;

namespace FamiStudio
{
    public class ChannelStateEPSMSquare : ChannelState
    {
        private int  channelIdx = 0;
        private int  invToneMask;

        // Last channel will upload these.       
        private int  toneReg = 0x38;
        private int  envPeriod;
        private int  envShape;
        private bool envAutoPitch;
        private int  envAutoOctave;
        private bool envReset;
        private int  noiseFreq;

        // From instrument.
        private bool instAutoPitch;
        private int  instAutoOctave;
        private int  instEnvShape;

        public ChannelStateEPSMSquare(IPlayerInterface player, int apuIdx, int channelType, bool pal) : base(player, apuIdx, channelType, pal)
        {
            channelIdx = channelType - ChannelType.EPSMSquare1;
            invToneMask = 0xff - (9 << channelIdx);
        }

        protected override void LoadInstrument(Instrument instrument)
        {
            if (instrument != null)
            {
                Debug.Assert(instrument.IsEpsm);

                if (instrument.IsEpsm)
                {
                    if (instrument.EPSMSquareEnvelopeShape > 0)
                    {
                        instEnvShape   = (byte)(instrument.EPSMSquareEnvelopeShape + 7); // 1...8 maps to 0x8...0xf
                        instAutoPitch  = instrument.EPSMSquareEnvAutoPitch;
                        instAutoOctave = instrument.EPSMSquareEnvAutoPitchOctave;
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
            var lastChannel = player.GetChannelByType(ChannelType.EPSMSquare3) as ChannelStateEPSMSquare;

            // All channels will update the channel 3 variables. This is pretty ugly
            // but mimics what the assemble code does pretty closely.
            if (channelIdx == 0)
            {
                lastChannel.envAutoPitch = false;
                lastChannel.envReset = false;
                lastChannel.noiseFreq = 0;
            }

            lastChannel.envReset |= (noteTriggered && instEnvShape > 0);

            if (note.HasEnvelopePeriod)
            {
                lastChannel.envPeriod = note.EnvelopePeriod;
            }

            if (note.IsStop)
            {
                WriteRegister(NesApu.EPSM_ADDR0, NesApu.EPSM_REG_VOL_A + channelIdx);
                WriteRegister(NesApu.EPSM_DATA0, 0);
            }
            else if (note.IsMusical)
            {
                var period = GetPeriod() + 1; // Unlike the 2A03 and VRC6 pulse channels' frequency formulas, the formula for 5B does not add 1 to the period.
                var volume = GetVolume();

                var periodHi = (period >> 8) & 0x0f;
                var periodLo = (period >> 0) & 0xff;

                WriteRegister(NesApu.EPSM_ADDR0, NesApu.EPSM_REG_LO_A + channelIdx * 2);
                WriteRegister(NesApu.EPSM_DATA0, periodLo);
                WriteRegister(NesApu.EPSM_ADDR0, NesApu.EPSM_REG_HI_A + channelIdx * 2);
                WriteRegister(NesApu.EPSM_DATA0, periodHi);
                WriteRegister(NesApu.EPSM_ADDR0, NesApu.EPSM_REG_VOL_A + channelIdx);
                WriteRegister(NesApu.EPSM_DATA0, volume | (instEnvShape != 0 ? 0x10 : 0x00));

                var mixerEnv  = envelopeValues[EnvelopeType.S5BMixer];
                var noiseFreq = envelopeValues[EnvelopeType.S5BNoiseFreq];
                lastChannel.toneReg = (lastChannel.toneReg & invToneMask) | (((mixerEnv & 1) | ((mixerEnv & 0x2) << 2)) << channelIdx);
                lastChannel.noiseFreq = noiseFreq > 0 ? noiseFreq : lastChannel.noiseFreq;

                if (instEnvShape != 0)
                {
                    lastChannel.envPeriod = instAutoPitch ? period : lastChannel.envPeriod;
                    lastChannel.envShape = instEnvShape;
                    lastChannel.envAutoOctave = instAutoOctave;
                    lastChannel.envAutoPitch = instAutoPitch;
                }
            }

            // Last channel will be in charge of writing to the shared registers.
            if (channelIdx == 2)
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

                WriteRegister(NesApu.EPSM_ADDR0, NesApu.EPSM_REG_ENV_LO);
                WriteRegister(NesApu.EPSM_DATA0, envPeriodLo);
                WriteRegister(NesApu.EPSM_ADDR0, NesApu.EPSM_REG_ENV_HI);
                WriteRegister(NesApu.EPSM_DATA0, envPeriodHi);
                WriteRegister(NesApu.EPSM_ADDR0, NesApu.EPSM_REG_NOISE_FREQ);
                WriteRegister(NesApu.EPSM_DATA0, noiseFreq);
                WriteRegister(NesApu.EPSM_ADDR0, NesApu.EPSM_REG_MIXER_SETTING);
                WriteRegister(NesApu.EPSM_DATA0, toneReg);

                if (envReset)
                {
                    WriteRegister(NesApu.EPSM_ADDR0, NesApu.EPSM_REG_SHAPE);
                    WriteRegister(NesApu.EPSM_DATA0, envShape);
                }
            }

            base.UpdateAPU();
        }
    };
}
