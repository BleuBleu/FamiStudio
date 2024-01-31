using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace FamiStudio
{
    static class RegisterWriteOptimizer
    {
        public static RegisterWrite[] OptimizeRegisterWrites(RegisterWrite[] regWrites)
        {
            int[] APUStatus = Enumerable.Repeat(0xFF00, 15).ToArray();
            /* 0-7: $4000 + (index<<1)
             * 8: $400B
             * 9: $400F
             * 10-13: $4010-$4013
             * 14: $4015
             */
            bool SampleStartedSince4011 = true; //To force writes

            int VRC7Addr = 0xFF00;
            int[] VRC7Status = Enumerable.Repeat(0xFF00, 26).ToArray();
            /* 0-7: $00-$07
             * 8-13: $10-$15
             * 14-19: $20-$25
             * 20-25: $30-$35
             */
            int[] VRC7StatusLookupTable = new int[] { 0, 8, 18, 28 };
            int S5BAddr = 0xFF00;
            int[] S5BStatus = Enumerable.Repeat(0xFF00, 13).ToArray();     //  S5B data (everything except for the reg 0xD, which phase resets the envelope when written to)

            int EPSMA0Addr = 0xFF00;
            int EPSMA1Addr = 0xFF00;
            int[] EPSMSSGStatus = Enumerable.Repeat(0xFF00, 13).ToArray();  // Same as S5B
            int[] EPSMRhythmStatus = Enumerable.Repeat(0xFF00, 7).ToArray();    //0-5: $18-$1D, $11
            int[] EPSMFMA0Status = Enumerable.Repeat(0xFF00, 135).ToArray();
            int[] EPSMFMA1Status = Enumerable.Repeat(0xFF00, 135).ToArray();
            RegisterWrite EPSMFMHiPitchBuffer = new RegisterWrite { Register = 0xFF00, Value = 0xFF00 };
            bool EPSMFMHiPitchBuffered = false;



            var firstPass = new List<RegisterWrite>();
            foreach (var regWrite in regWrites)
            {
                switch (regWrite.Register)
                {
                    case 0x4009:
                    case 0x400D: //Non-existent registers:
                        break;
                    /* Less bulky versions for when we stop using Mono on Android:
                    case >= NesApu.APU_PL1_VOL and < NesApu.APU_TRI_HI:     //  $4000 - $400A
                    case >= NesApu.APU_NOISE_VOL and < NesApu.APU_NOISE_HI: //  $400C, $400E
                    */
                    case int bulky1 when regWrite.Register >= NesApu.APU_PL1_VOL && regWrite.Register < NesApu.APU_TRI_HI:     //  $4000 - $400A
                    case int bulky2 when regWrite.Register >= NesApu.APU_NOISE_VOL && regWrite.Register < NesApu.APU_NOISE_HI: //  $400C, $400E
                        if ((regWrite.Register & 1) != 0)    //If the register is $4001/03/05/07, which reset stuff on write
                            firstPass.Add(regWrite);
                        else if (regWrite.Value != APUStatus[(regWrite.Register & 0xF) >> 1])
                        {  //If the register does not reset stuff
                            firstPass.Add(regWrite);
                            APUStatus[(regWrite.Register & 0xF) >> 1] = regWrite.Value;
                        }
                        break;
                    case NesApu.APU_TRI_HI:
                        if ((APUStatus[4] & 0x80) == 0)  //If the length & linear counters are active
                            firstPass.Add(regWrite);
                        else if (regWrite.Value != APUStatus[8])
                        { //If the counters are not active, but the period is different
                            APUStatus[8] = regWrite.Value;
                            firstPass.Add(regWrite);
                        }
                        break;
                    case NesApu.APU_NOISE_HI:
                        if ((APUStatus[6] & 0x30) == 0x30 && regWrite.Value != APUStatus[9])
                        {    //If envelope and length counters are disabled, and the value is different (makes sense in hypothetical situation "store now, activate later")
                            firstPass.Add(regWrite);
                            APUStatus[9] = regWrite.Value;
                        }
                        else if ((APUStatus[6] & 0x30) != 0x30)  //If either is enabled (phase resets envelope and/or length counter)
                            firstPass.Add(regWrite);
                        break;
                    case NesApu.APU_DMC_FREQ:
                    case NesApu.APU_DMC_LEN:
                        if ((APUStatus[regWrite.Register - 0x4006]) != regWrite.Value)
                        {
                            firstPass.Add(regWrite);
                            APUStatus[regWrite.Register - 0x4006] = regWrite.Value;
                        }
                        break;
                    case NesApu.APU_DMC_START:
                        if ((APUStatus[12]) != regWrite.Metadata)
                        {   //Compare the sample IDs
                            firstPass.Add(regWrite);
                            APUStatus[12] = regWrite.Metadata;
                        }
                        break;
                    case NesApu.APU_DMC_RAW:
                        if (regWrite.Value != APUStatus[11] || SampleStartedSince4011)
                        {
                            firstPass.Add(regWrite);
                            APUStatus[11] = regWrite.Value;
                        }
                        break;
                    case NesApu.APU_SND_CHN:
                        if ((regWrite.Value & 0x10) == 0x10 || APUStatus[14] != regWrite.Value)
                        {
                            firstPass.Add(regWrite);
                            APUStatus[14] = regWrite.Value;
                        }
                        SampleStartedSince4011 = (regWrite.Value & 0x10) != 0 ? true : false;
                        break;

                    case NesApu.VRC7_REG_SEL:
                        if (regWrite.Value != VRC7Addr)
                        {
                            firstPass.Add(regWrite);
                            VRC7Addr = regWrite.Value;
                        }
                        break;
                    case NesApu.VRC7_REG_WRITE:
                        int index = VRC7Addr - (VRC7StatusLookupTable[VRC7Addr >> 4]);
                        if (regWrite.Value != VRC7Status[index])
                        {
                            firstPass.Add(regWrite);
                            VRC7Status[index] = regWrite.Value;
                        }
                        break;
                    case NesApu.S5B_ADDR:
                        if (regWrite.Value != S5BAddr)
                        {
                            firstPass.Add(regWrite);
                            S5BAddr = regWrite.Value;
                        }
                        break;
                    case NesApu.S5B_DATA:
                        if (S5BAddr < NesApu.S5B_REG_SHAPE && regWrite.Value != S5BStatus[S5BAddr])
                        {   //Aka S5B regs that are saved
                            firstPass.Add(regWrite);
                            S5BStatus[S5BAddr] = regWrite.Value;
                        }
                        else if (S5BAddr >= NesApu.S5B_REG_SHAPE)  //If envelope shape register (which is not saved), or non-S5B register 
                            firstPass.Add(regWrite);
                        break;
                    case NesApu.EPSM_ADDR0:
                        if (regWrite.Value >= 0x2D && regWrite.Value <= 0x2F)
                        {  //Prescaler setting
                            firstPass.Add(regWrite);
                            EPSMA0Addr = regWrite.Value;
                        }
                        else if (regWrite.Value != EPSMA0Addr)
                        {
                            EPSMA0Addr = regWrite.Value;
                            if ((EPSMA0Addr & 0xF4) != 0xA0)
                            {
                                firstPass.Add(regWrite);
                            }
                        }
                        break;
                    case NesApu.EPSM_ADDR1:
                        if (regWrite.Value != EPSMA1Addr)
                        {
                            EPSMA1Addr = regWrite.Value;
                            if ((EPSMA1Addr & 0xF4) != 0xA0)
                            {
                                firstPass.Add(regWrite);
                            }
                        }
                        break;
                    case NesApu.EPSM_DATA0:
                        if (EPSMA0Addr < NesApu.EPSM_REG_SHAPE && regWrite.Value != EPSMSSGStatus[EPSMA0Addr])
                        { //Aka EPSM SSG regs that are saved
                            firstPass.Add(regWrite);
                            EPSMSSGStatus[EPSMA0Addr] = regWrite.Value;
                        }
                        else if (EPSMA0Addr == 0x11 && regWrite.Value != EPSMRhythmStatus[6])
                        {
                            firstPass.Add(regWrite);
                            EPSMRhythmStatus[6] = regWrite.Value;
                        }
                        else if (EPSMA0Addr >= NesApu.EPSM_REG_RYTHM_LEVEL && EPSMA0Addr < 0x1E && regWrite.Value != EPSMRhythmStatus[EPSMA0Addr & 0x07])
                        {
                            firstPass.Add(regWrite);
                            EPSMRhythmStatus[EPSMA0Addr & 0x07] = regWrite.Value;
                        }
                        else if (EPSMA0Addr == NesApu.EPSM_REG_RYTHM)
                        {
                            firstPass.Add(regWrite);
                        }
                        else if ((EPSMA0Addr & 0xF4) == 0xA4)
                        {    //hi pitch/block / CH3 hi pitch/block
                            EPSMFMHiPitchBuffer = regWrite;
                            EPSMFMHiPitchBuffered = true;
                        }
                        else if ((EPSMA0Addr & 0xF4) == 0xA0 && EPSMFMHiPitchBuffered)
                        { //lo pitch / CH3 lo pitch
                            EPSMFMHiPitchBuffered = false;
                            if (!(EPSMFMHiPitchBuffer.Value == EPSMFMA0Status[EPSMA0Addr - 0x2C] && regWrite.Value == EPSMFMA0Status[EPSMA0Addr - 0x30]))
                            {
                                EPSMFMA0Status[EPSMA0Addr - 0x2C] = EPSMFMHiPitchBuffer.Value;
                                EPSMFMA0Status[EPSMA0Addr - 0x30] = regWrite.Value;
                                firstPass.Add(EPSMFMHiPitchBuffer);
                                firstPass.Add(new RegisterWrite { Register = NesApu.EPSM_ADDR0, Value = EPSMA0Addr, FrameNumber = regWrite.FrameNumber });
                                firstPass.Add(regWrite);
                            }
                        }
                        else if ((EPSMA0Addr >= 0x30 && EPSMA0Addr < 0xA0 || EPSMA0Addr >= 0xB0 && EPSMA0Addr < 0xB7) && regWrite.Value != EPSMFMA0Status[EPSMA0Addr - 0x30])
                        {
                            firstPass.Add(regWrite);
                            EPSMFMA0Status[EPSMA0Addr - 0x30] = regWrite.Value;
                        }
                        else if (EPSMA0Addr >= 0x20 && EPSMA0Addr < 0x2D)
                        {
                            firstPass.Add(regWrite);
                        }
                        break;
                    case NesApu.EPSM_DATA1:
                        if ((EPSMA1Addr & 0xF4) == 0xA4)
                        {    //hi pitch/block / CH3 hi pitch/block
                            EPSMFMHiPitchBuffer = regWrite;
                            EPSMFMHiPitchBuffered = true;
                        }
                        else if ((EPSMA1Addr & 0xF4) == 0xA0 && EPSMFMHiPitchBuffered)
                        { //lo pitch / CH3 lo pitch
                            EPSMFMHiPitchBuffered = false;
                            if (!(EPSMFMHiPitchBuffer.Value == EPSMFMA1Status[EPSMA1Addr - 0x2C] && regWrite.Value == EPSMFMA1Status[EPSMA1Addr - 0x30]))
                            {
                                EPSMFMA1Status[EPSMA1Addr - 0x2C] = EPSMFMHiPitchBuffer.Value;
                                EPSMFMA1Status[EPSMA1Addr - 0x30] = regWrite.Value;
                                firstPass.Add(EPSMFMHiPitchBuffer);
                                firstPass.Add(new RegisterWrite { Register = NesApu.EPSM_ADDR1, Value = EPSMA1Addr, FrameNumber = regWrite.FrameNumber });
                                firstPass.Add(regWrite);
                            }
                        }
                        else if ((EPSMA1Addr >= 0x30 && EPSMA1Addr < 0xA0 || EPSMA1Addr >= 0xB0 && EPSMA1Addr < 0xB7) && regWrite.Value != EPSMFMA1Status[EPSMA1Addr - 0x30])
                        {
                            firstPass.Add(regWrite);
                            EPSMFMA1Status[EPSMA1Addr - 0x30] = regWrite.Value;
                        }
                        else if (EPSMA1Addr >= 0x20 && EPSMA1Addr < 0x30)
                        {
                            firstPass.Add(regWrite);
                        }
                        break;
                    default:
                        firstPass.Add(regWrite);
                        break;
                }
            }
            var result = new List<RegisterWrite>();
            // Second pass (optimizing wait commands)
            EPSMA0Addr = 0xFF00;
            EPSMA1Addr = 0xFF00;
            S5BAddr = 0xFF00;
            VRC7Addr = 0xFF00;
            for (int i = 0; i < firstPass.Count; i++)
            {
                var regWrite = firstPass[i];
                var nextWrite = firstPass[Utils.Clamp(i + 1, 0, firstPass.Count - 1)];
                switch (regWrite.Register)
                {
                    case NesApu.EPSM_ADDR0:
                        if ((nextWrite.Register == NesApu.EPSM_DATA0
                        && regWrite.Value != EPSMA0Addr) || // If it actually wrote to EPSM
                        (regWrite.Value >= 0x2D && regWrite.Value <= 0x2F))
                        { // Or selected a prescaler    
                            result.Add(regWrite);
                            EPSMA0Addr = regWrite.Value;
                        }
                        break;
                    case NesApu.EPSM_ADDR1:
                        if (nextWrite.Register == NesApu.EPSM_DATA1
                        && regWrite.Value != EPSMA1Addr)
                        {  // If it actually wrote to EPSM 
                            result.Add(regWrite);
                            EPSMA1Addr = regWrite.Value;
                        }
                        break;
                    case NesApu.VRC7_REG_SEL:
                        if (nextWrite.Register == NesApu.VRC7_REG_WRITE &&
                        regWrite.Value != VRC7Addr)  // If it actually wrote to VRC7 
                            result.Add(regWrite);
                        VRC7Addr = regWrite.Value;
                        break;
                    case NesApu.S5B_ADDR:
                        if (nextWrite.Register == NesApu.S5B_DATA &&
                        regWrite.Value != S5BAddr)  // If it actually wrote to S5B 
                            result.Add(regWrite);
                        S5BAddr = regWrite.Value;
                        break;
                    default:
                        result.Add(regWrite);
                        break;
                }
            }

            return result.ToArray();
        }
        public static RegisterWrite[] RemoveExpansionWritesBut(RegisterWrite[] writes, ushort chipMask)
        {
            bool useAPU = (chipMask & 0x8000) != 0 ? true : false;
            bool useVRC6 = (chipMask & 0x0001) != 0 ? true : false;
            bool useVRC7 = (chipMask & 0x0002) != 0 ? true : false;
            bool useFDS = (chipMask & 0x0004) != 0 ? true : false;
            bool useMMC5 = (chipMask & 0x0008) != 0 ? true : false;
            bool useN163 = (chipMask & 0x0010) != 0 ? true : false;
            bool useS5B = (chipMask & 0x0020) != 0 ? true : false;
            bool useEPSM = (chipMask & 0x0040) != 0 ? true : false;
            Debug.WriteLine($"============ REMOVE EXPANSION WRITES BUT =============\nuseAPU = {useAPU};\nuseVRC6 = {useVRC6};\nuseVRC7 = {useVRC7};\nuseFDS = {useFDS};\nuseMMC5 = {useMMC5};\nuseN163 = {useN163};\nuseS5B = {useS5B}\nuseEPSM = {useEPSM};");
            List<RegisterWrite> output = new List<RegisterWrite>();
            foreach (var w in writes)
            {
                if (
                    (w.Register >= NesApu.APU_PL1_VOL && w.Register <= NesApu.APU_FRAME_CNT && useAPU) ||
                    (((w.Register >= NesApu.VRC6_PL1_VOL && w.Register <= NesApu.VRC6_CTRL) ||
                    (w.Register >= NesApu.VRC6_PL2_VOL && w.Register <= NesApu.VRC6_PL2_HI) ||
                    (w.Register >= NesApu.VRC6_SAW_VOL && w.Register <= NesApu.VRC6_SAW_HI)) && useVRC6) ||
                    ((w.Register == NesApu.VRC7_REG_SEL || w.Register == NesApu.VRC7_REG_WRITE) && useVRC7) ||
                    (w.Register >= NesApu.FDS_WAV_START && w.Register <= NesApu.FDS_ENV_SPEED && useFDS) ||
                    (((w.Register >= NesApu.MMC5_PL1_VOL && w.Register <= NesApu.MMC5_PL2_HI) || w.Register == NesApu.MMC5_SND_CHN) && useMMC5) ||
                    ((w.Register == NesApu.N163_DATA || w.Register == NesApu.N163_ADDR) && useN163) ||
                    (w.Register == NesApu.S5B_ADDR && useS5B) ||
                    (w.Register >= NesApu.EPSM_ADDR0 && w.Register <= NesApu.EPSM_DATA1 && useEPSM) ||
                    (w.Register == NesApu.N163_SILENCE && (useN163 || useS5B || useVRC7))   //Bus conflicts
                ) output.Add(w);
            }
            return output.ToArray();
        }
    }
};