using System;
using System.Collections.Generic;
using System.Linq;
namespace FamiStudio
{
    class RegisterWriteOptimizer
    {
        private int[] APUStatus = Enumerable.Repeat(0xFF00, 15).ToArray();
        //0 - 7: $4000 + (index<<1)
        //8: $400B
        //9: $400F
        //10-13: $4010-$4013
        //14: $4015
        bool SampleStartedSince4011 = true; //To force writes
        private int S5BAddr = 0xFF00;
        private int[] S5BStatus = Enumerable.Repeat(0xFF00, 13).ToArray();     //  S5B data (everything except for the reg 0xD, which phase resets the envelope when written to)

        private int EPSMA0Addr = 0xFF00;
        private int EPSMA1Addr = 0xFF00;
        private int[] EPSMSSGStatus = Enumerable.Repeat(0xFF00, 13).ToArray();  // Same as S5B
        private int[] EPSMRhythmStatus = Enumerable.Repeat(0xFF00, 7).ToArray();
        //0-5: $18-$1D
        //6: $11

        private bool useAPU;
        private bool useVRC6;
        private bool useVRC7;
        private bool useFDS;
        private bool useMMC5;
        private bool useN163;
        private bool useS5B;
        private bool useEPSM;
        public RegisterWriteOptimizer(Project project, ushort chipMask = 0xFFFF)
        {
            useAPU = (chipMask & 0x8000) != 0 ? true : false;
            useVRC6 = (chipMask & 0x0001) != 0 ? true : false;
            useVRC7 = (chipMask & 0x0002) != 0 ? true : false;
            useFDS = (chipMask & 0x0004) != 0 ? true : false;
            useMMC5 = (chipMask & 0x0008) != 0 ? true : false;
            useN163 = (chipMask & 0x0010) != 0 ? true : false;
            useS5B = (chipMask & 0x0020) != 0 ? true : false;
            useEPSM = (chipMask & 0x0040) != 0 ? true : false;
        }

        public RegisterWrite[] OptimizeRegisterWrites (RegisterWrite[] regWrites){
            var result = new List<RegisterWrite>();
            foreach (var regWrite in regWrites){
                switch (regWrite.Register){
                    case 0x4009: case 0x400D: //Non-existent registers:
                        break;
                    case >= NesApu.APU_PL1_VOL and < NesApu.APU_TRI_HI:     //  $4000 - $400A
                    case >= NesApu.APU_NOISE_VOL and < NesApu.APU_NOISE_HI: //  $400C, $400E
                        if (!useAPU) break;
                        if ((regWrite.Register & 1) != 0)    //If the register is $4001/03/05/07, which reset stuff on write
                            result.Add(regWrite);
                        else if (regWrite.Value != APUStatus[(regWrite.Register&0xF)>>1]){  //If the register does not reset stuff
                            result.Add(regWrite);
                            APUStatus[(regWrite.Register&0xF)>>1] = regWrite.Value;
                        }
                        break;
                    case NesApu.APU_TRI_HI:
                        if (!useAPU) break;
                        if ((APUStatus[4] & 0x80) == 0)  //If the length & linear counters are active
                            result.Add(regWrite);
                        else if (regWrite.Value != APUStatus[8]){ //If the counters are not active, but the period is different
                            APUStatus[8] = regWrite.Value;
                            result.Add(regWrite);
                        }
                        break;
                    case NesApu.APU_NOISE_HI:
                        if (!useAPU) break;
                        if ((APUStatus[6] & 0x30) == 0x30 && regWrite.Value != APUStatus[9]){    //If envelope and length counters are disabled, and the value is different (makes sense in hypothetical situation "store now, activate later")
                            result.Add(regWrite);
                            APUStatus[9] = regWrite.Value;
                        } else if ((APUStatus[6] & 0x30) != 0x30 )  //If either is enabled (phase resets envelope and/or length counter)
                            result.Add(regWrite);
                        break;
                    case NesApu.APU_DMC_FREQ:
                    case NesApu.APU_DMC_START:
                    case NesApu.APU_DMC_LEN:
                        if (!useAPU) break;
                        if ((APUStatus[regWrite.Register-0x4006]) != regWrite.Value){
                            result.Add(regWrite);
                            APUStatus[regWrite.Register-0x4006] = regWrite.Value;
                        }
                        break;
                    case NesApu.APU_DMC_RAW:
                        if (!useAPU) break;
                        if (regWrite.Value != APUStatus[11] || SampleStartedSince4011){
                            result.Add(regWrite);
                            APUStatus[11] = regWrite.Value;
                        }
                        break;
                    case NesApu.APU_SND_CHN:
                        if (!useAPU) break;
                        if ((regWrite.Value & 0x10) == 0x10 || APUStatus[14] != regWrite.Value){
                            result.Add(regWrite);
                            APUStatus[14] = regWrite.Value;
                        }
                        SampleStartedSince4011 = (regWrite.Value & 0x10) != 0 ? true : false;
                        break;
                    case NesApu.S5B_ADDR:
                        if (!useS5B) break;
                        if (regWrite.Value != S5BAddr){
                            result.Add(regWrite);
                            S5BAddr = regWrite.Value;
                        }
                        break;
                    case NesApu.S5B_DATA:
                        if (!useS5B) break;
                        if (S5BAddr < NesApu.S5B_REG_SHAPE && regWrite.Value != S5BStatus[S5BAddr]){   //Aka S5B regs that are saved
                            result.Add(regWrite);
                            S5BStatus[S5BAddr] = regWrite.Value;
                        } else if (S5BAddr >= NesApu.S5B_REG_SHAPE)  //If envelope shape register (which is not saved), or non-S5B register 
                            result.Add(regWrite);
                        break;
                    case NesApu.EPSM_ADDR0:
                        if (!useEPSM) break;
                        if (regWrite.Value != EPSMA0Addr){
                            result.Add(regWrite);
                            EPSMA0Addr = regWrite.Value;
                        }
                        break;
                    case NesApu.EPSM_ADDR1:
                        if (!useEPSM) break;
                        if (regWrite.Value != EPSMA1Addr){
                            result.Add(regWrite);
                            EPSMA1Addr = regWrite.Value;
                        }
                        break;
                    case NesApu.EPSM_DATA0:
                        if (!useEPSM) break;
                        if (EPSMA0Addr < NesApu.EPSM_REG_SHAPE && regWrite.Value != EPSMSSGStatus[EPSMA0Addr]){ //Aka EPSM SSG regs that are saved
                            result.Add(regWrite);
                            EPSMSSGStatus[EPSMA0Addr] = regWrite.Value;
                        } else if (EPSMA0Addr == 0x11 && regWrite.Value != EPSMRhythmStatus[6]){
                            result.Add(regWrite);
                            EPSMRhythmStatus[6] = regWrite.Value;
                        } else if (EPSMA0Addr >= NesApu.EPSM_REG_RYTHM_LEVEL && EPSMA0Addr < 0x1E && regWrite.Value != EPSMRhythmStatus[EPSMA0Addr&0x07]){
                            result.Add(regWrite);
                            EPSMRhythmStatus[EPSMA0Addr&0x07] = regWrite.Value;
                        } else if (EPSMA0Addr == NesApu.EPSM_REG_RYTHM)
                            result.Add(regWrite);
                        else if (EPSMA0Addr >= 0x20) {
                            result.Add(regWrite);
                        }
                        break;
                    default:
                        result.Add(regWrite);
                        break;
                }
            }
            return result.ToArray();
        }
    }
};