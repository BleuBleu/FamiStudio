
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
        int[] opOrder = { 0, 2, 1, 3 };
        int[] opRegisters = { 0xb0, 0xb4, 0x30, 0x40, 0x50, 0x60, 0x70, 0x80, 0x90, 0x22 };
        int[] opStereo = { 0,0,0,0,0,0};
        int[] channelAlgorithm = { 0, 0, 0, 0, 0, 0 };
        int[] opVolume = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

        public ChannelStateEPSMFm(IPlayerInterface player, int apuIdx, int channelType, bool pal) : base(player, apuIdx, channelType, pal)
        {
            channelIdx = channelType - ChannelType.EPSMFm1;
            customRelease = true;
        }

        private void WriteEPSMRegister(int reg, int data, int a1)
        {
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

                //Console.WriteLine($"writereg {NesApu.EPSM_ADDR1:X4} reg {reg:X2} {NesApu.EPSM_DATA1:X4} data {data:X2} ");
            }
        }

        protected override void LoadInstrument(Instrument instrument)
        {
            Debug.Assert(instrument.IsEPSMInstrument);
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
            opStereo[channelIdx] = instrument.EpsmPatchRegs[1];
            //opVolume[0 + 4 * channelIdx] = instrument.EpsmPatchRegs[3];
            //opVolume[1 + 4 * channelIdx] = instrument.EpsmPatchRegs[3 + 7];
            //opVolume[2 + 4 * channelIdx] = instrument.EpsmPatchRegs[3 + 14];
            //opVolume[3 + 4 * channelIdx] = instrument.EpsmPatchRegs[3 + 21];
            if (instrument.IsEPSMInstrument)
            {
                channelAlgorithm[channelIdx] = instrument.EpsmPatchRegs[0];
                WriteEPSMRegister(opRegisters[0] + channelIdxHigh, instrument.EpsmPatchRegs[0], a1);
                WriteEPSMRegister(opRegisters[1] + channelIdxHigh, instrument.EpsmPatchRegs[1], a1);
                WriteEPSMRegister(opRegisters[9], instrument.EpsmPatchRegs[30], 0); //LFO - This accounts for all channels
                for (byte y = 0; y < 4; y++)
                {
                    opVolume[opOrder[y] + 4 * channelIdx] = instrument.EpsmPatchRegs[3 + (y * 7)];
                    WriteEPSMRegister(opRegisters[2] + channelIdxHigh + opOrder[y] * 4, instrument.EpsmPatchRegs[2 + (y * 7)], a1);
                    //WriteEPSMRegister(opRegisters[3] + channelIdxHigh + opOrder[y] * 4, instrument.EpsmPatchRegs[3 + (y * 7)], a1);
                    WriteEPSMRegister(opRegisters[4] + channelIdxHigh + opOrder[y] * 4, instrument.EpsmPatchRegs[4 + (y * 7)], a1);
                    WriteEPSMRegister(opRegisters[5] + channelIdxHigh + opOrder[y] * 4, instrument.EpsmPatchRegs[5 + (y * 7)], a1);
                    WriteEPSMRegister(opRegisters[6] + channelIdxHigh + opOrder[y] * 4, instrument.EpsmPatchRegs[6 + (y * 7)], a1);
                    WriteEPSMRegister(opRegisters[7] + channelIdxHigh + opOrder[y] * 4, instrument.EpsmPatchRegs[7 + (y * 7)], a1);
                    WriteEPSMRegister(opRegisters[8] + channelIdxHigh + opOrder[y] * 4, instrument.EpsmPatchRegs[8 + (y * 7)], a1);
                }

            }
        }



        public override void UpdateAPU()
        {

            int a1 = 0;
            if (channelIdx >= 3)
            {
                channelIdxHigh = channelIdx - 3;
                ChannelAddr = NesApu.EPSM_ADDR1;
                ChannelData = NesApu.EPSM_DATA1;
                ChannelKey = 0x4 | (channelIdx - 3);
                a1 = 1;
            }
            else { 
                channelIdxHigh = channelIdx;
                ChannelKey = channelIdx;
            }


            if (note.IsStop)
            {
                WriteRegister(NesApu.EPSM_ADDR0, 0x28);
                WriteRegister(NesApu.EPSM_DATA0, 0x00 + ChannelKey);
                WriteRegister(ChannelAddr, 0xb4 + channelIdxHigh);
                WriteRegister(ChannelData, 0x00); //volume 0
            }
            else if (note.IsRelease)
            {
                WriteRegister(NesApu.EPSM_ADDR0, 0x28);
                WriteRegister(NesApu.EPSM_DATA0, 0x00 + ChannelKey);
            }
            else if (note.IsMusical)
            {
                var period = GetPeriod();
                var periodHi = (byte)(period >> 8);
                var periodLo = (byte)(period & 0xff);
                var volume = GetVolume();

                int steps = 16;
                int adjustment = 5;
                //Logarithmic volume adjustment
                var step = (Math.Log(127+adjustment) - Math.Log(adjustment)) / (steps - 1);
                volume = 127-(int)((Math.Exp(Math.Log(adjustment) + (15-volume) * step)) - adjustment);
                int[] channelAlgorithmMask = { 0x8, 0x8, 0x8, 0x8, 0xC, 0xE, 0xE, 0xF };
                switch (channelAlgorithmMask[channelAlgorithm[channelIdx] & 0x7])
                {
                    case  0xF:
                        WriteEPSMRegister(0x40 + channelIdxHigh, 127 - (int)((127 - (float)opVolume[0 + 4 * channelIdx]) / 127 * volume), a1);
                        WriteEPSMRegister(0x44 + channelIdxHigh, 127 - (int)((127 - (float)opVolume[1 + 4 * channelIdx]) / 127 * volume), a1);
                        WriteEPSMRegister(0x48 + channelIdxHigh, 127 - (int)((127 - (float)opVolume[2 + 4 * channelIdx]) / 127 * volume), a1);
                        WriteEPSMRegister(0x4c + channelIdxHigh, 127 - (int)((127 - (float)opVolume[3 + 4 * channelIdx]) / 127 * volume), a1);
                        break;
                    case 0xE:
                        WriteEPSMRegister(0x40 + channelIdxHigh, opVolume[0 + 4 * channelIdx], a1);
                        WriteEPSMRegister(0x44 + channelIdxHigh, 127 - (int)((127 - (float)opVolume[1 + 4 * channelIdx]) / 127 * volume), a1);
                        WriteEPSMRegister(0x48 + channelIdxHigh, 127 - (int)((127 - (float)opVolume[2 + 4 * channelIdx]) / 127 * volume), a1);
                        WriteEPSMRegister(0x4c + channelIdxHigh, 127 - (int)((127 - (float)opVolume[3 + 4 * channelIdx]) / 127 * volume), a1);
                        break;
                    case 0xC:
                        WriteEPSMRegister(0x40 + channelIdxHigh, opVolume[0 + 4 * channelIdx], a1);
                        WriteEPSMRegister(0x44 + channelIdxHigh, opVolume[1 + 4 * channelIdx], a1);
                        WriteEPSMRegister(0x48 + channelIdxHigh, 127 - (int)((127 - (float)opVolume[2 + 4 * channelIdx]) / 127 * volume), a1);
                        WriteEPSMRegister(0x4c + channelIdxHigh, 127 - (int)((127 - (float)opVolume[3 + 4 * channelIdx]) / 127 * volume), a1);
                        break;
                    case 0x8:
                        WriteEPSMRegister(0x40 + channelIdxHigh, opVolume[0 + 4 * channelIdx], a1);
                        WriteEPSMRegister(0x44 + channelIdxHigh, opVolume[1 + 4 * channelIdx], a1);
                        WriteEPSMRegister(0x48 + channelIdxHigh, opVolume[2 + 4 * channelIdx], a1);
                        WriteEPSMRegister(0x4c + channelIdxHigh, 127 - (int)((127 - (float)opVolume[3 + 4 * channelIdx]) / 127 * volume), a1);
                        break;

                }

                //WriteEPSMRegister(0x40 + channelIdxHigh, 127 - (int)((127 - (float)opVolume[0 + 4 * channelIdx]) / 127 * volume), a1);
                //WriteEPSMRegister(0x44 + channelIdxHigh, 127 - (int)((127 - (float)opVolume[1 + 4 * channelIdx]) / 127 * volume), a1);
                //WriteEPSMRegister(0x48 + channelIdxHigh, 127 - (int)((127 - (float)opVolume[2 + 4 * channelIdx]) / 127 * volume), a1);
                //WriteEPSMRegister(0x4c + channelIdxHigh, 127 - (int)((127 - (float)opVolume[3 + 4 * channelIdx]) / 127 * volume), a1);
                /*WriteEPSMRegister(0x40 + channelIdxHigh, 127 - (int)((127 - (float)opVolume[0 + 4 * channelIdx]) / 15 * volume), a1);
                WriteEPSMRegister(0x44 + channelIdxHigh, 127 - (int)((127 - (float)opVolume[1 + 4 * channelIdx]) / 15 * volume), a1);
                WriteEPSMRegister(0x48 + channelIdxHigh, 127 - (int)((127 - (float)opVolume[2 + 4 * channelIdx]) / 15 * volume), a1);
                WriteEPSMRegister(0x4c + channelIdxHigh, 127 - (int)((127 - (float)opVolume[3 + 4 * channelIdx]) / 15 * volume), a1);*/
                if (noteTriggered)
                {
                    WriteRegister(NesApu.EPSM_ADDR0, 0x28);
                    WriteRegister(NesApu.EPSM_DATA0, 0x00 + ChannelKey);
                    WriteRegister(NesApu.EPSM_ADDR0, 0x28);
                    WriteRegister(NesApu.EPSM_DATA0, 0xF0 + ChannelKey);
                    WriteEPSMRegister(opRegisters[1] + channelIdxHigh, opStereo[channelIdx], a1);
                }

                WriteRegister(ChannelAddr, 0xA4 + channelIdxHigh);
                WriteRegister(ChannelData, periodHi);

                WriteRegister(ChannelAddr, 0xA0 + channelIdxHigh);
                WriteRegister(ChannelData, periodLo);
            }

            base.UpdateAPU();
        }
    };
}
