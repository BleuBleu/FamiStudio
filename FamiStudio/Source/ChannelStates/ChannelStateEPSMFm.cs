
using System;
using System.Diagnostics;

namespace FamiStudio
{
    public class ChannelStateEPSMFm : ChannelState
    {
        protected int channelIdx = 0;
        protected int channelIdxHigh = 0;
        protected int ChannelAddr = NesApu.EPSM_ADDR0;
        protected int ChannelData = NesApu.EPSM_DATA0;
        protected int ChannelKey = 0;
        protected byte vrc7Instrument = 0;
        protected byte prevPeriodHi;
        int lastNoteOn = 0;

        public ChannelStateEPSMFm(IPlayerInterface player, int apuIdx, int channelType, bool pal) : base(player, apuIdx, channelType, pal)
        {
            channelIdx = channelType - ChannelType.EPSMFm1;
            customRelease = true;
        }

        private void WriteVrc7Register(int reg, int data)
        {
         /*   WriteRegister(NesApu.VRC7_REG_SEL, reg);
            WriteRegister(NesApu.VRC7_REG_WRITE, data);*/
        }

        protected override void LoadInstrument(Instrument instrument)
        {
           // if (instrument != null)
           // {
                Debug.Assert(instrument.ExpansionType == ExpansionType.EPSM);

                if (instrument.ExpansionType == ExpansionType.EPSM)
                {
                    if (instrument.Vrc7Patch == 0)
                    {
                        // Tell other channels using custom patches that they will need 
                        // to reload their instruments.
                        player.NotifyInstrumentLoaded(
                            instrument,
                            (1 << ChannelType.EPSMFm1) |
                            (1 << ChannelType.EPSMFm2) |
                            (1 << ChannelType.EPSMFm3) |
                            (1 << ChannelType.EPSMFm4) |
                            (1 << ChannelType.EPSMFm5) |
                            (1 << ChannelType.EPSMFm6));

                        //for (byte i = 0; i < 8; i++)
                        //    WriteVrc7Register(i, instrument.Vrc7PatchRegs[i]);
                    }

                    //vrc7Instrument = (byte)(instrument.Vrc7Patch << 4);
                }
           // }
        }

       /* public override void IntrumentLoadedNotify(Instrument instrument)
        {
            Debug.Assert(instrument.IsExpansionInstrument && instrument.Vrc7Patch == 0);

            // This will be called when another channel loads a custom patch.
            if (note.Instrument != null &&
                note.Instrument != instrument &&
                note.Instrument.Vrc7Patch == 0)
            {
                forceInstrumentReload = true;
            }
        }*/

        private int GetOctave(ref int period)
        {
            var octave = 0;
            while (period > 0x100)
            {
                period >>= 1;
                octave++;
            }
            return octave;
        }

        public override void UpdateAPU()
        {

            if (channelIdx >= 3)
            {
                channelIdxHigh = channelIdx - 3;
                ChannelAddr = NesApu.EPSM_ADDR1;
                ChannelData = NesApu.EPSM_DATA1;
                ChannelKey = 0x4 | (channelIdx - 3);
            }
            else { 
                channelIdxHigh = channelIdx;
                ChannelKey = channelIdx;
            }


            if (note.IsStop)
            {
                lastNoteOn = 0;
                prevPeriodHi = (byte)(prevPeriodHi & ~(0x30));
                WriteRegister(NesApu.EPSM_ADDR0, 0x28);
                WriteRegister(NesApu.EPSM_DATA0, 0x00 + ChannelKey);
            }
            else if (note.IsRelease)
            {
                lastNoteOn = 0;
                prevPeriodHi = (byte)(prevPeriodHi & ~(0x10));
                WriteRegister(NesApu.EPSM_ADDR0, 0x28);
                WriteRegister(NesApu.EPSM_DATA0, 0x00 + ChannelKey);
            }
            else if (note.IsMusical)
            {
                    //Console.WriteLine("note musical" + channelIdx);
                    var period = GetPeriod();
                    var periodHi = (byte)(period >> 8);
                    var periodLo = (byte)(period & 0xff);
                    var octave = GetOctave(ref period);
                    var volume = (15 - GetVolume()) * 8;
                    if (volume == 120)
                        volume = 127;

                    //Console.WriteLine("note volume" + volume + " getvolume " + GetVolume());

                    //var periodHi = (byte)(0x30 | ((octave & 0x7) << 1) | ((period >> 8) & 1));

                    if (noteTriggered && (prevPeriodHi & 0x10) != 0)
                    {
                        //WriteRegister(NesApu.EPSM_ADDR0, 0x28);
                        //WriteRegister(NesApu.EPSM_DATA0, 0xF6);
                        //WriteVrc7Register(NesApu.VRC7_REG_HI_1 + channelIdx, prevPeriodHi & ~(0x10));
                    }


                    WriteRegister(ChannelAddr, 0xB4 + channelIdxHigh);
                    WriteRegister(ChannelData, 0xC0);
                    WriteRegister(ChannelAddr, 0xA4 + channelIdxHigh);
                    WriteRegister(ChannelData, periodHi);

                    WriteRegister(ChannelAddr, 0xA0 + channelIdxHigh);
                    WriteRegister(ChannelData, periodLo);

                    WriteRegister(ChannelAddr, 0x40 + channelIdxHigh);
                    WriteRegister(ChannelData, 0x7f);
                    WriteRegister(ChannelAddr, 0x44 + channelIdxHigh);
                    WriteRegister(ChannelData, 0x7f);
                    WriteRegister(ChannelAddr, 0x48 + channelIdxHigh);
                    WriteRegister(ChannelData, 0x7f);
                    WriteRegister(ChannelAddr, 0x4c + channelIdxHigh);
                    WriteRegister(ChannelData, volume);

                    WriteRegister(ChannelAddr, 0xb0 + channelIdxHigh);
                    WriteRegister(ChannelData, 0x4);
                if (lastNoteOn == 0)
                {
                    lastNoteOn = 1;
                    WriteRegister(NesApu.EPSM_ADDR0, 0x28);
                    WriteRegister(NesApu.EPSM_DATA0, 0xF0 + ChannelKey);

                    prevPeriodHi = periodHi;
                }
            }

            base.UpdateAPU();
        }
    };
}
