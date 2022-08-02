
using System;
using System.Diagnostics;

namespace FamiStudio
{
    public class ChannelStateEPSMFm : ChannelState
    {
        protected int  channelIdx = 0;
        protected int  channelIdxHigh = 0;
        protected int  channelAddr = NesApu.EPSM_ADDR0;
        protected int  channelData = NesApu.EPSM_DATA0;
        protected int  channelKey = 0;
        protected byte epsmInstrument = 0;
        
        private readonly int[] Order     = { 0, 2, 1, 3 };
        private readonly int[] Registers = { 0xb0, 0xb4, 0x30, 0x40, 0x50, 0x60, 0x70, 0x80, 0x90, 0x22 };
        private readonly int[] ChannelAlgorithmMask = { 0x8, 0x8, 0x8, 0x8, 0xC, 0xE, 0xE, 0xF };

        private int   stereoFlags = 0;
        private bool  release = false;
        private bool  stop = false;
        private int   lastVolume = 0;
        private bool  newInstrument = false;
        private int   algorithm = 0;
        private int[] opVolume = { 0, 0, 0, 0 };

        public ChannelStateEPSMFm(IPlayerInterface player, int apuIdx, int channelType, bool pal) : base(player, apuIdx, channelType, pal)
        {
            channelIdx = channelType - ChannelType.EPSMFm1;
            customRelease = true;
        }

        private void WriteEPSMRegister(int reg, int data, int a1)
        {
            if (a1 == 0)
            {
                WriteRegister(NesApu.EPSM_ADDR0, reg,  34);
                WriteRegister(NesApu.EPSM_DATA0, data, 34);
            }
            else
            {
                WriteRegister(NesApu.EPSM_ADDR1, reg,  34);
                WriteRegister(NesApu.EPSM_DATA1, data, 34);
            }
        }

        protected override void LoadInstrument(Instrument instrument)
        {
            if (instrument != null)
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

                stereoFlags = instrument.EpsmPatchRegs[1];

                if (instrument.IsEpsm)
                {
                    newInstrument = true;
                    algorithm = instrument.EpsmPatchRegs[0];

                    WriteEPSMRegister(Registers[0] + channelIdxHigh, instrument.EpsmPatchRegs[0], a1);
                    WriteEPSMRegister(Registers[1] + channelIdxHigh, instrument.EpsmPatchRegs[1], a1);
                    WriteEPSMRegister(Registers[9], instrument.EpsmPatchRegs[30], 0); //LFO - This accounts for all channels
                    
                    for (byte y = 0; y < 4; y++)
                    {
                        opVolume[Order[y]] = instrument.EpsmPatchRegs[3 + (y * 7)];
                        WriteEPSMRegister(Registers[2] + channelIdxHigh + Order[y] * 4, instrument.EpsmPatchRegs[2 + (y * 7)], a1);
                        WriteEPSMRegister(Registers[4] + channelIdxHigh + Order[y] * 4, instrument.EpsmPatchRegs[4 + (y * 7)], a1);
                        WriteEPSMRegister(Registers[5] + channelIdxHigh + Order[y] * 4, instrument.EpsmPatchRegs[5 + (y * 7)], a1);
                        WriteEPSMRegister(Registers[6] + channelIdxHigh + Order[y] * 4, instrument.EpsmPatchRegs[6 + (y * 7)], a1);
                        WriteEPSMRegister(Registers[7] + channelIdxHigh + Order[y] * 4, instrument.EpsmPatchRegs[7 + (y * 7)], a1);
                        WriteEPSMRegister(Registers[8] + channelIdxHigh + Order[y] * 4, instrument.EpsmPatchRegs[8 + (y * 7)], a1);
                    }
                }
            }
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
            else
            { 
                channelIdxHigh = channelIdx;
                channelKey = channelIdx;
            }

            if (note.IsStop && !stop)
            {
                WriteEPSMRegister(0x28, 0x00 + channelKey, 0);
                WriteEPSMRegister(0xb4 + channelIdxHigh, 0x00, a1); //volume 0
                stop = true;
                release = false;
            }
            else if (note.IsRelease && !release)
            {
                release = true;
                stop = false;
                WriteEPSMRegister(0x28, 0x00 + channelKey, 0);
            }
            else if (note.IsMusical)
            {
                release = false;
                stop = false;

                var period = GetPeriod();
                var octave = GetOctave(ref period);
                var periodLo = (byte)(period << 2) & 0xff;
                var periodHi = (byte)(((octave & 0x7) << 3) | ((period >> 6) & 7));
                var volume = GetVolume();

                int steps = 16;
                int adjustment = 5;

                //Logarithmic volume adjustment
                var step = (Math.Log(127+adjustment) - Math.Log(adjustment)) / (steps - 1);

                volume = (int)((Math.Exp(Math.Log(adjustment) + (15 - volume) * step)) - adjustment);

                if (newInstrument || lastVolume != volume)
                {
                    lastVolume = volume;
                    newInstrument = false;

                    switch (ChannelAlgorithmMask[algorithm & 0x7])
                    {
                        case 0xF:
                            WriteEPSMRegister(0x40 + channelIdxHigh, Utils.Clamp(opVolume[0] + volume, 0, 127), a1);
                            WriteEPSMRegister(0x44 + channelIdxHigh, Utils.Clamp(opVolume[1] + volume, 0, 127), a1);
                            WriteEPSMRegister(0x48 + channelIdxHigh, Utils.Clamp(opVolume[2] + volume, 0, 127), a1);
                            WriteEPSMRegister(0x4c + channelIdxHigh, Utils.Clamp(opVolume[3] + volume, 0, 127), a1);
                            break;
                        case 0xE:
                            WriteEPSMRegister(0x40 + channelIdxHigh, opVolume[0], a1);
                            WriteEPSMRegister(0x44 + channelIdxHigh, Utils.Clamp(opVolume[1] + volume, 0, 127), a1);
                            WriteEPSMRegister(0x48 + channelIdxHigh, Utils.Clamp(opVolume[2] + volume, 0, 127), a1);
                            WriteEPSMRegister(0x4c + channelIdxHigh, Utils.Clamp(opVolume[3] + volume, 0, 127), a1);
                            break;
                        case 0xC:
                            WriteEPSMRegister(0x40 + channelIdxHigh, opVolume[0], a1);
                            WriteEPSMRegister(0x44 + channelIdxHigh, opVolume[1], a1);
                            WriteEPSMRegister(0x48 + channelIdxHigh, Utils.Clamp(opVolume[2] + volume, 0, 127), a1);
                            WriteEPSMRegister(0x4c + channelIdxHigh, Utils.Clamp(opVolume[3] + volume, 0, 127), a1);
                            break;
                        case 0x8:
                            WriteEPSMRegister(0x40 + channelIdxHigh, opVolume[0], a1);
                            WriteEPSMRegister(0x44 + channelIdxHigh, opVolume[1], a1);
                            WriteEPSMRegister(0x48 + channelIdxHigh, opVolume[2], a1);
                            WriteEPSMRegister(0x4c + channelIdxHigh, Utils.Clamp(opVolume[3] + volume, 0, 127), a1);
                            break;

                    }
                }

                if (noteTriggered)
                {
                    WriteEPSMRegister(0x28, 0x00 + channelKey, 0);
                    WriteEPSMRegister(0x28, 0xF0 + channelKey, 0);
                    WriteEPSMRegister(Registers[1] + channelIdxHigh, stereoFlags, a1);
                }

                WriteEPSMRegister(0xA4 + channelIdxHigh, periodHi, a1);
                WriteEPSMRegister(0xA0 + channelIdxHigh, periodLo, a1);
            }

            base.UpdateAPU();
        }
    };
}
