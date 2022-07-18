
using System;
using System.Diagnostics;

namespace FamiStudio
{
    public class ChannelStateEPSMFm : ChannelState
    {
        protected int channelIdx = 0;
        protected int channelIdxHigh = 0;
        protected int channelAddr = NesApu.EPSM_ADDR0;
        protected int channelData = NesApu.EPSM_DATA0;
        protected int channelKey = 0;
        protected byte epsmInstrument = 0;
        int[] opOrder = { 0, 2, 1, 3 };
        int[] opRegisters = { 0xb0, 0xb4, 0x30, 0x40, 0x50, 0x60, 0x70, 0x80, 0x90, 0x22 };
        int[] opStereo = { 0,0,0,0,0,0};
        //chip write limiting
        int[] opRelease = { 0, 0, 0, 0, 0, 0 };
        int[] opStop = { 0, 0, 0, 0, 0, 0 };
        int[] lastPeriod = { 0, 0, 0, 0, 0, 0 };
        int[] lastVolume = { 0, 0, 0, 0, 0, 0 };
        int[] newInstrument = { 0, 0, 0, 0, 0, 0 };
        //-------------------
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
                WriteRegister(NesApu.EPSM_ADDR0, reg, 16);
                WriteRegister(NesApu.EPSM_DATA0, data, 16);
            }
            else
            {
                WriteRegister(NesApu.EPSM_ADDR1, reg, 16);
                WriteRegister(NesApu.EPSM_DATA1, data, 16);
            }
        }

        protected override void LoadInstrument(Instrument instrument)
        {
            Debug.Assert(instrument.IsEpsm);
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
            if (instrument.IsEpsm)
            {
                newInstrument[channelIdx] = 1;
                channelAlgorithm[channelIdx] = instrument.EpsmPatchRegs[0];
                WriteEPSMRegister(opRegisters[0] + channelIdxHigh, instrument.EpsmPatchRegs[0], a1);
                WriteEPSMRegister(opRegisters[1] + channelIdxHigh, instrument.EpsmPatchRegs[1], a1);
                WriteEPSMRegister(opRegisters[9], instrument.EpsmPatchRegs[30], 0); //LFO - This accounts for all channels
                for (byte y = 0; y < 4; y++)
                {
                    opVolume[opOrder[y] + 4 * channelIdx] = instrument.EpsmPatchRegs[3 + (y * 7)];
                    WriteEPSMRegister(opRegisters[2] + channelIdxHigh + opOrder[y] * 4, instrument.EpsmPatchRegs[2 + (y * 7)], a1);
                    WriteEPSMRegister(opRegisters[4] + channelIdxHigh + opOrder[y] * 4, instrument.EpsmPatchRegs[4 + (y * 7)], a1);
                    WriteEPSMRegister(opRegisters[5] + channelIdxHigh + opOrder[y] * 4, instrument.EpsmPatchRegs[5 + (y * 7)], a1);
                    WriteEPSMRegister(opRegisters[6] + channelIdxHigh + opOrder[y] * 4, instrument.EpsmPatchRegs[6 + (y * 7)], a1);
                    WriteEPSMRegister(opRegisters[7] + channelIdxHigh + opOrder[y] * 4, instrument.EpsmPatchRegs[7 + (y * 7)], a1);
                    WriteEPSMRegister(opRegisters[8] + channelIdxHigh + opOrder[y] * 4, instrument.EpsmPatchRegs[8 + (y * 7)], a1);
                }

            }
        }
        public static int Clamp(int value, int min, int max)
        {
            return (value < min) ? min : (value > max) ? max : value;
        }
        private int GetOctave(ref int period)
        {
            var octave = 0;
            while (period >= 0x200)
            {
                period >>= 1;
                octave++;
            }
            return octave;
        }

        public override void UpdateAPU()
        {

            int a1 = 0;
            if (channelIdx >= 3)
            {
                channelIdxHigh = channelIdx - 3;
                channelAddr = NesApu.EPSM_ADDR1;
                channelData = NesApu.EPSM_DATA1;
                channelKey = 0x4 | (channelIdx - 3);
                a1 = 1;
            }
            else { 
                channelIdxHigh = channelIdx;
                channelKey = channelIdx;
            }


            if (note.IsStop && opStop[channelIdx] == 0)
            {
                WriteRegister(NesApu.EPSM_ADDR0, 0x28);
                WriteRegister(NesApu.EPSM_DATA0, 0x00 + channelKey);
                WriteRegister(channelAddr, 0xb4 + channelIdxHigh);
                WriteRegister(channelData, 0x00); //volume 0
                opStop[channelIdx] = 1;
                opRelease[channelIdx] = 0;
            }
            else if (note.IsRelease && opRelease[channelIdx] == 0)
            {
                opRelease[channelIdx] = 1;
                opStop[channelIdx] = 0;
                WriteRegister(NesApu.EPSM_ADDR0, 0x28);
                WriteRegister(NesApu.EPSM_DATA0, 0x00 + channelKey);
            }
            else if (note.IsMusical)
            {
                opRelease[channelIdx] = 0;
                opStop[channelIdx] = 0;
                var period = GetPeriod();
                var octave = GetOctave(ref period);
                //var periodHi = (byte)(period >> 8);
                //var periodLo = (byte)(period & 0xff);

                var periodLo = (byte)(period << 2) & 0xff;
                var periodHi = (byte)(((octave & 0x7) << 3) | ((period >> 6) & 7));
                var volume = GetVolume();

                int steps = 16;
                int adjustment = 5;
                //Logarithmic volume adjustment
                var step = (Math.Log(127+adjustment) - Math.Log(adjustment)) / (steps - 1);


                volume = (int)((Math.Exp(Math.Log(adjustment) + (15 - volume) * step)) - adjustment);
                int[] channelAlgorithmMask = { 0x8, 0x8, 0x8, 0x8, 0xC, 0xE, 0xE, 0xF };
                if (newInstrument[channelIdx] == 1 || lastVolume[channelIdx] != volume)
                {
                    lastVolume[channelIdx] = volume;
                    newInstrument[channelIdx] = 0;
                    switch (channelAlgorithmMask[channelAlgorithm[channelIdx] & 0x7])
                    {
                        case 0xF:
                            WriteEPSMRegister(0x40 + channelIdxHigh, Clamp(opVolume[0 + 4 * channelIdx] + volume,0,127), a1);
                            WriteEPSMRegister(0x44 + channelIdxHigh, Clamp(opVolume[1 + 4 * channelIdx] + volume, 0, 127), a1);
                            WriteEPSMRegister(0x48 + channelIdxHigh, Clamp(opVolume[2 + 4 * channelIdx] + volume, 0, 127), a1);
                            WriteEPSMRegister(0x4c + channelIdxHigh, Clamp(opVolume[3 + 4 * channelIdx] + volume, 0, 127), a1);
                            break;
                        case 0xE:
                            WriteEPSMRegister(0x40 + channelIdxHigh, opVolume[0 + 4 * channelIdx], a1);
                            WriteEPSMRegister(0x44 + channelIdxHigh, Clamp(opVolume[1 + 4 * channelIdx] + volume, 0, 127), a1);
                            WriteEPSMRegister(0x48 + channelIdxHigh, Clamp(opVolume[2 + 4 * channelIdx] + volume, 0, 127), a1);
                            WriteEPSMRegister(0x4c + channelIdxHigh, Clamp(opVolume[3 + 4 * channelIdx] + volume, 0, 127), a1);
                            break;
                        case 0xC:
                            WriteEPSMRegister(0x40 + channelIdxHigh, opVolume[0 + 4 * channelIdx], a1);
                            WriteEPSMRegister(0x44 + channelIdxHigh, opVolume[1 + 4 * channelIdx], a1);
                            WriteEPSMRegister(0x48 + channelIdxHigh, Clamp(opVolume[2 + 4 * channelIdx] + volume, 0, 127), a1);
                            WriteEPSMRegister(0x4c + channelIdxHigh, Clamp(opVolume[3 + 4 * channelIdx] + volume, 0, 127), a1);
                            break;
                        case 0x8:
                            WriteEPSMRegister(0x40 + channelIdxHigh, opVolume[0 + 4 * channelIdx], a1);
                            WriteEPSMRegister(0x44 + channelIdxHigh, opVolume[1 + 4 * channelIdx], a1);
                            WriteEPSMRegister(0x48 + channelIdxHigh, opVolume[2 + 4 * channelIdx], a1);
                            WriteEPSMRegister(0x4c + channelIdxHigh, Clamp(opVolume[3 + 4 * channelIdx] + volume, 0, 127), a1);
                            break;

                    }
                }

                if (noteTriggered)
                {
                    WriteRegister(NesApu.EPSM_ADDR0, 0x28);
                    WriteRegister(NesApu.EPSM_DATA0, 0x00 + channelKey);
                    WriteRegister(NesApu.EPSM_ADDR0, 0x28);
                    WriteRegister(NesApu.EPSM_DATA0, 0xF0 + channelKey);
                    WriteEPSMRegister(opRegisters[1] + channelIdxHigh, opStereo[channelIdx], a1);
                }
                    lastPeriod[channelIdx] = (periodLo + periodHi);
                    WriteRegister(channelAddr, 0xA4 + channelIdxHigh);
                    WriteRegister(channelData, periodHi);
                    WriteRegister(channelAddr, 0xA0 + channelIdxHigh);
                    WriteRegister(channelData, periodLo);
            }

            base.UpdateAPU();
        }
    };
}
