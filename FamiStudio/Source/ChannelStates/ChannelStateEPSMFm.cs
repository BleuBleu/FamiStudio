
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
        protected byte epsmInstrument = 0;
        protected byte prevPeriodHi;
        int lastNoteOn = 0;

        public ChannelStateEPSMFm(IPlayerInterface player, int apuIdx, int channelType, bool pal) : base(player, apuIdx, channelType, pal)
        {
            channelIdx = channelType - ChannelType.EPSMFm1;
            customRelease = true;
        }

        private void WriteEPSMRegister(int reg, int data, int a1)
        {
            string hex;
            if (a1 == 0)
            {
                WriteRegister(NesApu.EPSM_ADDR0, reg);
                WriteRegister(NesApu.EPSM_DATA0, data);
                //Console.WriteLine($"writereg {NesApu.EPSM_ADDR0:X4} reg {reg:X2} {NesApu.EPSM_DATA0:X4} data {data:X2} ");
            }
            else
            {
                WriteRegister(NesApu.EPSM_ADDR1, reg);
                WriteRegister(NesApu.EPSM_DATA1, data);

               // Console.WriteLine($"writereg {NesApu.EPSM_ADDR1:X4} reg {reg:X2} {NesApu.EPSM_DATA1:X4} data {data:X2} ");
            }
        }

        protected override void LoadInstrument(Instrument instrument)
        {
           // if (instrument != null)
           // {
                Debug.Assert(instrument.ExpansionType == ExpansionType.EPSM);
            int[] opRegisters = { 0xb0, 0xb4, 0x30, 0x40, 0x50, 0x60, 0x70, 0x80, 0x90 };
            //Console.WriteLine("load instrument" + channelIdx);
            int a1 = 0;
            if (channelIdx >= 3)
            {
                channelIdxHigh = channelIdx - 3;
                a1 = 1;
            }
            else
            {
                channelIdxHigh = channelIdx;
            }

            if (instrument.ExpansionType == ExpansionType.EPSM)
            {
                //Console.WriteLine("load instrument type" + channelIdx);
                //if (instrument.EpsmPatch == 0)
                //{
                  //  Console.WriteLine("load instrument patch" + channelIdx);
                // Tell other channels using custom patches that they will need 
                // to reload their instruments.
                /*player.NotifyInstrumentLoaded(
                    instrument,
                    (1 << ChannelType.EPSMFm1) |
                    (1 << ChannelType.EPSMFm2) |
                    (1 << ChannelType.EPSMFm3) |
                    (1 << ChannelType.EPSMFm4) |
                    (1 << ChannelType.EPSMFm5) |
                    (1 << ChannelType.EPSMFm6));*/

                //for (byte a1 = 0; a1 < 2; a1++) {
                    //for (byte i = 0; i < 4; i++) {
                            WriteEPSMRegister(opRegisters[0] + channelIdxHigh, instrument.EpsmPatchRegs[0], a1);
                            WriteEPSMRegister(opRegisters[1] + channelIdxHigh, instrument.EpsmPatchRegs[1], a1);
                int[] opOrder = { 0,2,1,3 };
                for (byte y = 0; y < 4; y++)
                {
                    WriteEPSMRegister(opRegisters[2] + channelIdxHigh + opOrder[y] * 4, instrument.EpsmPatchRegs[2 + (y * 7)], a1);
                    WriteEPSMRegister(opRegisters[3] + channelIdxHigh + opOrder[y] * 4, instrument.EpsmPatchRegs[3 + (y * 7)], a1);
                    WriteEPSMRegister(opRegisters[4] + channelIdxHigh + opOrder[y] * 4, instrument.EpsmPatchRegs[4 + (y * 7)], a1);
                    WriteEPSMRegister(opRegisters[5] + channelIdxHigh + opOrder[y] * 4, instrument.EpsmPatchRegs[5 + (y * 7)], a1);
                    WriteEPSMRegister(opRegisters[6] + channelIdxHigh + opOrder[y] * 4, instrument.EpsmPatchRegs[6 + (y * 7)], a1);
                    WriteEPSMRegister(opRegisters[7] + channelIdxHigh + opOrder[y] * 4, instrument.EpsmPatchRegs[7 + (y * 7)], a1);
                    WriteEPSMRegister(opRegisters[8] + channelIdxHigh + opOrder[y] * 4, instrument.EpsmPatchRegs[8 + (y * 7)], a1);
                }
                //}
                //}
               


                //}

                epsmInstrument = (byte)(instrument.EpsmPatch << 4);
                }
           // }
        }

        public override void IntrumentLoadedNotify(Instrument instrument)
        {
            Debug.Assert(instrument.IsExpansionInstrument && instrument.EpsmPatch == 0);

            // This will be called when another channel loads a custom patch.
            if (note.Instrument != null &&
                note.Instrument != instrument &&
                note.Instrument.EpsmPatch == 0)
            {
                forceInstrumentReload = true;
            }
        }

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


                    //WriteRegister(ChannelAddr, 0xB4 + channelIdxHigh);
                    //WriteRegister(ChannelData, 0xC0);
                    WriteRegister(ChannelAddr, 0xA4 + channelIdxHigh);
                    WriteRegister(ChannelData, periodHi);

                    WriteRegister(ChannelAddr, 0xA0 + channelIdxHigh);
                    WriteRegister(ChannelData, periodLo);

                    /*WriteRegister(ChannelAddr, 0x40 + channelIdxHigh);
                    WriteRegister(ChannelData, 0x7f);
                    WriteRegister(ChannelAddr, 0x44 + channelIdxHigh);
                    WriteRegister(ChannelData, 0x7f);
                    WriteRegister(ChannelAddr, 0x48 + channelIdxHigh);
                    WriteRegister(ChannelData, 0x7f);*/
                    //WriteRegister(ChannelAddr, 0x4c + channelIdxHigh);
                    //WriteRegister(ChannelData, volume);

                    //WriteRegister(ChannelAddr, 0xb0 + channelIdxHigh);
                    //WriteRegister(ChannelData, 0x4);
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
