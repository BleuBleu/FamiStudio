using System;
using System.Diagnostics;

namespace FamiStudio
{
    public class ChannelStateEPSMBase : ChannelState
    {
        public ChannelStateEPSMBase(IPlayerInterface player, int apuIdx, int channelType, bool pal) : base(player, apuIdx, channelType, pal)
        {
        }

        protected void WriteEPSMRegister(int reg, int data, bool a1 = false, int extraCycles = 0)
        {
            // Registers starting in 0x30, 0x40, 0x50, 0x60, 0x70, 0x80, 0x90 seem to 
            // require a bit more cycles looking at the OPN2 code.
            var dataSkipCycles = (reg & 0xf0) < 0x30 || (reg & 0xf0) > 0x90 ? NesApu.EpsmCycleDataSkipShort : NesApu.EpsmCycleDataSkip;

            WriteRegister(a1 ? NesApu.EPSM_ADDR1 : NesApu.EPSM_ADDR0, reg,  NesApu.EpsmCycleAddrSkip);
            WriteRegister(a1 ? NesApu.EPSM_DATA1 : NesApu.EPSM_DATA0, data, dataSkipCycles + extraCycles);
        }
    }

    public class ChannelStateEPSMFm : ChannelStateEPSMBase
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

        private int    stereoFlags = 0;
        private bool   stop = false;
        private int    lastVolume = 0;
        private byte[] patchRegs = null; // Only set when a new instrument is loaded.
        private int    algorithm = 0;
        private int[]  opVolume = { 0, 0, 0, 0 };

        public ChannelStateEPSMFm(IPlayerInterface player, int apuIdx, int channelType, bool pal) : base(player, apuIdx, channelType, pal)
        {
            channelIdx = channelType - ChannelType.EPSMFm1;
            channelIdxHigh = channelIdx >= 3 ? channelIdx - 3 : channelIdx;
        }

        protected override void LoadInstrument(Instrument instrument)
        {
            if (instrument != null)
            { 
                Debug.Assert(instrument.IsEpsm);
                
                stereoFlags = instrument.EpsmPatchRegs[1];
                algorithm   = instrument.EpsmPatchRegs[0];
                patchRegs   = instrument.EpsmPatchRegs;
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
            bool a1 = false;

            if (channelIdx >= 3)
            {
                channelIdxHigh = channelIdx - 3;
                channelAddr = NesApu.EPSM_ADDR1;
                channelData = NesApu.EPSM_DATA1;
                channelKey = 0x4 | (channelIdx - 3);
                a1 = true;
            }
            else
            { 
                channelIdxHigh = channelIdx;
                channelKey = channelIdx;
            }

            if (note.IsStop && !stop)
            {
                WriteEPSMRegister(0x28, 0x00 + channelKey);
                WriteEPSMRegister(0xb4 + channelIdxHigh, 0x00, a1); //volume 0
                stop = true;
            }
            else if (note.IsMusical)
            {
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

                if (patchRegs != null || lastVolume != volume)
                {
                    // We delay loading of instrument here instead of in LoadInstrument().
                    // The number of writes we need to do and the number of cycles between each
                    // means we end up with "chirps" when many channels change instruments at 
                    // the same time, since all channels first change their instrument THEN
                    // the all update the volume/note. This is probably something we need to 
                    // revisit here, and in the sound engine itself.
                    if (patchRegs != null)
                    {
                        WriteEPSMRegister(Registers[0] + channelIdxHigh, patchRegs[0], a1);
                        WriteEPSMRegister(Registers[1] + channelIdxHigh, patchRegs[1], a1);
                        WriteEPSMRegister(Registers[9], patchRegs[30]); //LFO - This accounts for all channels

                        for (byte y = 0; y < 4; y++)
                        {
                            opVolume[Order[y]] = patchRegs[3 + (y * 7)];
                            WriteEPSMRegister(Registers[2] + channelIdxHigh + Order[y] * 4, patchRegs[2 + (y * 7)], a1);
                            WriteEPSMRegister(Registers[4] + channelIdxHigh + Order[y] * 4, patchRegs[4 + (y * 7)], a1);
                            WriteEPSMRegister(Registers[5] + channelIdxHigh + Order[y] * 4, patchRegs[5 + (y * 7)], a1);
                            WriteEPSMRegister(Registers[6] + channelIdxHigh + Order[y] * 4, patchRegs[6 + (y * 7)], a1);
                            WriteEPSMRegister(Registers[7] + channelIdxHigh + Order[y] * 4, patchRegs[7 + (y * 7)], a1);
                            WriteEPSMRegister(Registers[8] + channelIdxHigh + Order[y] * 4, patchRegs[8 + (y * 7)], a1);
                        }

                        patchRegs = null;
                    }

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

                    lastVolume = volume;
                }

                WriteEPSMRegister(0xA4 + channelIdxHigh, periodHi, a1);
                WriteEPSMRegister(0xA0 + channelIdxHigh, periodLo, a1);

                if (noteTriggered)
                {
                    WriteEPSMRegister(0x28, 0x00 + channelKey, false, NesApu.EpsmCycleKeyOnSkip);
                    WriteEPSMRegister(0x28, 0xF0 + channelKey);
                    WriteEPSMRegister(Registers[1] + channelIdxHigh, stereoFlags, a1);
                }
                else if (noteReleased)
                {
                    WriteEPSMRegister(0x28, 0x00 + channelKey);
                }
            }

            base.UpdateAPU();
        }
    };
}

