/* 
 * Copyright (C) 2004      Disch
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA 
 */

//////////////////////////////////////////////////////////////////////////
//
//  NSF_Core.cpp
//

#include <stdio.h>
#include "NSF_Core.h"
#include "NSF_File.h"

//////////////////////////////////////////////////////////////////////////
//
//		A few macros
//

#define CLOCK_MAJOR() {	mWave_Squares.ClockMajor(); mWave_TND.ClockMajor();			\
	if(nExternalSound & EXTSOUND_MMC5) { mWave_MMC5Square[0].ClockMajor(); mWave_MMC5Square[1].ClockMajor(); }}
#define CLOCK_MINOR() {	mWave_Squares.ClockMinor(); mWave_TND.ClockMinor();			\
	if(nExternalSound & EXTSOUND_MMC5) { mWave_MMC5Square[0].ClockMinor(); mWave_MMC5Square[1].ClockMinor(); }}
#define SAFE_DELETE(p) { if(p){ delete[] p; p = NULL; }}

//////////////////////////////////////////////////////////////////////////
//
//		Lookup tables
//

const WORD	DMC_FREQ_TABLE[2][0x10] = {
//NTSC
{0x1AC,0x17C,0x154,0x140,0x11E,0x0FE,0x0E2,0x0D6,0x0BE,0x0A0,0x08E,0x080,0x06A,0x054,0x048,0x036},
//PAL
{0x18C,0x160,0x13A,0x128,0x108,0x0EA,0x0D0,0x0C6,0x0B0,0x094,0x082,0x076,0x062,0x04E,0x042,0x032}
};

const BYTE DUTY_CYCLE_TABLE[4] = {2,4,8,12};

const BYTE LENGTH_COUNTER_TABLE[0x20] = {
0x0A,0xFE,0x14,0x02,0x28,0x04,0x50,0x06,0xA0,0x08,0x3C,0x0A,0x0E,0x0C,0x1A,0x0E,
0x0C,0x10,0x18,0x12,0x30,0x14,0x60,0x16,0xC0,0x18,0x48,0x1A,0x10,0x1C,0x20,0x1E	};

const WORD NOISE_FREQ_TABLE[0x10] = {
0x004,0x008,0x010,0x020,0x040,0x060,0x080,0x0A0,0x0CA,0x0FE,0x17C,0x1FC,0x2FA,0x3F8,0x7F2,0xFE4	};


#define SILENCE_THRESHOLD		3


//////////////////////////////////////////////////////////////////////////
//
//		Read Memory Procs
//

BYTE FASTCALL CNSFCore::ReadMemory_pAPU(WORD a)
{
	EmulateAPU(1);

	if(a == 0x4015)
	{
		BYTE ret = 0;
		if(mWave_Squares.nLengthCount[0])		ret |= 0x01;
		if(mWave_Squares.nLengthCount[1])		ret |= 0x02;
		if(mWave_TND.nTriLengthCount)			ret |= 0x04;
		if(mWave_TND.nNoiseLengthCount)			ret |= 0x08;
		if(mWave_TND.nDMCBytesRemaining)		ret |= 0x10;

		if(bFrameIRQPending)					ret |= 0x40;
		if(mWave_TND.bDMCIRQPending)			ret |= 0x80;

		bFrameIRQPending = 0;
		return ret;
	}

	if(!(nExternalSound & EXTSOUND_FDS))		return 0x40;

	if((a >= 0x4040) && (a <= 0x407F))
		return mWave_FDS.nWaveTable[a & 0x3F] | 0x40;
	if(a == 0x4090)
		return (mWave_FDS.nVolEnv_Gain & 0x3F) | 0x40;
	if(a == 0x4092)
		return (mWave_FDS.nSweep_Gain & 0x3F) | 0x40;

	return 0x40;
}

BYTE FASTCALL CNSFCore::ReadMemory_N106(WORD a)
{
	if(a != 0x4800)
		return ReadMemory_pAPU(a);

    BYTE ret = mWave_N106.nRAM[(mWave_N106.nCurrentAddress << 1)] | (mWave_N106.nRAM[(mWave_N106.nCurrentAddress << 1) + 1] << 4);
	if(mWave_N106.bAutoIncrement)
		mWave_N106.nCurrentAddress = (mWave_N106.nCurrentAddress + 1) & 0x7F;

	return ret;
}

//////////////////////////////////////////////////////////////////////////
//
//		Write Memory Procs
//

void FASTCALL CNSFCore::WriteMemory_ExRAM(WORD a,BYTE v)
{
	if(a < 0x5FF6)				//Invalid
		return;

	a -= 0x5FF6;

	// Swap out banks

	EmulateAPU(1);
	if(v >= nROMBankCount)		//stop it from swapping to a bank that doesn't exist
		v = 0;

	pROM[a] = pROM_Full + (v << 12);

	// Update the DMC's DMA pointer, as well
	if(a >= 2)
		mWave_TND.pDMCDMAPtr[a - 2] = pROM[a];
}

void FASTCALL CNSFCore::WriteMemory_pAPU(WORD a,BYTE v)
{
	EmulateAPU(1);

	if (apuRegWriteCallback)
		apuRegWriteCallback(a, v);


	if ((a <= 0x401f) && (a >= 0x401c) && (nExternalSound & EXTSOUND_EPSM))
		WriteMemory_EPSM(a, v);


	switch(a)
	{
		//////////////////////////
		// Square 1
	case 0x4000:
		mWave_Squares.nDutyCycle[0] = DUTY_CYCLE_TABLE[v >> 6];
		mWave_Squares.bLengthEnabled[0] = !(mWave_Squares.bDecayLoop[0] = (v & 0x20));
		mWave_Squares.bDecayEnable[0] = !(v & 0x10);
		mWave_Squares.nDecayTimer[0] = (v & 0x0F);

		if(!mWave_Squares.bDecayEnable[0])
            mWave_Squares.nVolume[0] = mWave_Squares.nDecayTimer[0];
		break;

	case 0x4001:
		mWave_Squares.bSweepEnable[0] = (v & 0x80);
		mWave_Squares.nSweepTimer[0] = (v & 0x70) >> 4;
		mWave_Squares.bSweepMode[0] = v & 0x08;
		mWave_Squares.nSweepShift[0] = v & 0x07;
		mWave_Squares.CheckSweepForcedSilence(0);
		break;
		
	case 0x4002:
		mWave_Squares.nFreqTimer[0].B.l = v;
		mWave_Squares.CheckSweepForcedSilence(0);
		break;
		
	case 0x4003:
		mWave_Squares.nFreqTimer[0].B.h = v & 0x07;
		mWave_Squares.CheckSweepForcedSilence(0);

		mWave_Squares.nDecayVolume[0] = 0x0F;

		if(mWave_Squares.bChannelEnabled[0])
			mWave_Squares.nLengthCount[0] = LENGTH_COUNTER_TABLE[v >> 3];

		if(bResetDuty)
			mWave_Squares.nDutyCount[0] = 0;
		break;
		

		//////////////////////////
		// Square 2
	case 0x4004:
		mWave_Squares.nDutyCycle[1] = DUTY_CYCLE_TABLE[v >> 6];
		mWave_Squares.bLengthEnabled[1] = !(mWave_Squares.bDecayLoop[1] = (v & 0x20));
		mWave_Squares.bDecayEnable[1] = !(v & 0x10);
		mWave_Squares.nDecayTimer[1] = (v & 0x0F);

		if(!mWave_Squares.bDecayEnable[1])
			mWave_Squares.nVolume[1] = mWave_Squares.nDecayTimer[1];
		break;

	case 0x4005:
		mWave_Squares.bSweepEnable[1] = (v & 0x80);
		mWave_Squares.nSweepTimer[1] = (v & 0x70) >> 4;
		mWave_Squares.bSweepMode[1] = v & 0x08;
		mWave_Squares.nSweepShift[1] = v & 0x07;
		mWave_Squares.CheckSweepForcedSilence(1);
		break;
		
	case 0x4006:
		mWave_Squares.nFreqTimer[1].B.l = v;
		mWave_Squares.CheckSweepForcedSilence(1);
		break;
		
	case 0x4007:
		mWave_Squares.nFreqTimer[1].B.h = v & 0x07;
		mWave_Squares.CheckSweepForcedSilence(1);

		mWave_Squares.nDecayVolume[1] = 0x0F;

		if(mWave_Squares.bChannelEnabled[1])
			mWave_Squares.nLengthCount[1] = LENGTH_COUNTER_TABLE[v >> 3];

		if(bResetDuty)
			mWave_Squares.nDutyCount[1] = 0;
		break;

		
		//////////////////////////
		// Triangle
	case 0x4008:
		mWave_TND.nTriLinearLoad = v & 0x7F;
		mWave_TND.bTriLinearControl = v & 0x80;
		mWave_TND.bTriLengthHalt = mWave_TND.bTriLinearControl;
		break;

	case 0x400A:
		mWave_TND.nTriFreqTimer.B.l = v;
		break;

	case 0x400B:
		mWave_TND.nTriFreqTimer.B.h = v & 0x07;
		mWave_TND.bTriLinearReloadFlag = 1;

		if(mWave_TND.bTriChannelEnabled)
			mWave_TND.nTriLengthCount = LENGTH_COUNTER_TABLE[v >> 3];
		break;

		//////////////////////////
		// Noise
	case 0x400C:
		mWave_TND.bNoiseLengthEnabled = !(mWave_TND.bNoiseDecayLoop = (v & 0x20));
		mWave_TND.bNoiseDecayEnable = !(v & 0x10);
		mWave_TND.nNoiseDecayTimer = (v & 0x0F);

		if(mWave_TND.bNoiseDecayEnable)
			mWave_TND.nNoiseVolume = mWave_TND.nNoiseDecayVolume;
		else
			mWave_TND.nNoiseVolume = mWave_TND.nNoiseDecayTimer;
		break;

	case 0x400E:
		mWave_TND.nNoiseFreqTimer = NOISE_FREQ_TABLE[v & 0x0F];
		mWave_TND.bNoiseRandomMode = (v & 0x80) ? 6 : 1;
		break;

	case 0x400F:
		if(mWave_TND.bNoiseChannelEnabled)
			mWave_TND.nNoiseLengthCount = LENGTH_COUNTER_TABLE[v >> 3];

		mWave_TND.nNoiseDecayVolume = 0x0F;
		if(mWave_TND.bNoiseDecayEnable)
			mWave_TND.nNoiseVolume = 0x0F;
		break;

		//////////////////////////
		// DMC
	case 0x4010:
		mWave_TND.bDMCLoop = v & 0x40;
		mWave_TND.bDMCIRQEnabled = v & 0x80;
		if(!mWave_TND.bDMCIRQEnabled)
			mWave_TND.bDMCIRQPending = 0;		//IRQ can't be pending if it's disabled

		mWave_TND.nDMCFreqTimer = DMC_FREQ_TABLE[bPALMode][v & 0x0F];
		break;

	case 0x4011:
		if(bIgnore4011Writes)
			break;
		v &= 0x7F;
		if(bDMCPopReducer)
		{
			if(bDMCPop_SamePlay)
				mWave_TND.nDMCOutput = v;
			else
			{
				if(bDMCPop_Skip)
				{
					bDMCPop_Skip = 0;
					break;
				}
				if(nDMCPop_Prev == v) break;
				if(mWave_TND.nDMCOutput == v) break;
				mWave_TND.nDMCOutput = nDMCPop_Prev;
				nDMCPop_Prev = v;
				bDMCPop_SamePlay = 1;
			}
		}
		else
			mWave_TND.nDMCOutput = v;
		mWave_TND.bDMCLastDeltaWrite = v;
		break;

	case 0x4012:
		mWave_TND.nDMCDMABank_Load = (v >> 6) | 0x04;
		mWave_TND.nDMCDMAAddr_Load = (v << 6) & 0x0FFF;
		break;

	case 0x4013:
		mWave_TND.nDMCLength = (v << 4) + 1;
		break;

		//////////////////////////
		// All / General Purpose
	case 0x4015:
		mWave_TND.bDMCIRQPending = 0;

		if(v & 0x01){	mWave_Squares.bChannelEnabled[0] =									1;	}
		else		{	mWave_Squares.bChannelEnabled[0] = mWave_Squares.nLengthCount[0] =	0;	}
		if(v & 0x02){	mWave_Squares.bChannelEnabled[1] =									1;	}
		else		{	mWave_Squares.bChannelEnabled[1] = mWave_Squares.nLengthCount[1] =	0;	}
		if(v & 0x04){	mWave_TND.bTriChannelEnabled =										1;	}
		else		{	mWave_TND.bTriChannelEnabled = mWave_TND.nTriLengthCount =			0;	}
		if(v & 0x08){	mWave_TND.bNoiseChannelEnabled =									1;	}
		else		{	mWave_TND.bNoiseChannelEnabled = mWave_TND.nNoiseLengthCount =		0;	}

		if(v & 0x10)
		{
			if(!mWave_TND.nDMCBytesRemaining)
			{
				bDMCPop_Skip = 1;
				mWave_TND.bDMCTriggered = 1;
				mWave_TND.nDMCDMAAddr = mWave_TND.nDMCDMAAddr_Load;
				mWave_TND.nDMCDMABank = mWave_TND.nDMCDMABank_Load;
				mWave_TND.nDMCBytesRemaining = mWave_TND.nDMCLength;
				mWave_TND.bDMCActive = 1;
			}
		}
		else
			mWave_TND.nDMCBytesRemaining = 0;
		break;

	case 0x4017:
		bFrameIRQEnabled = !(v & 0x40);
		bFrameIRQPending = 0;
		nFrameCounter = 0;
		nFrameCounterMax = (v & 0x80) ? 4 : 3;
		fTicksUntilNextFrame = (bPALMode ? PAL_FRAME_COUNTER_FREQ : NTSC_FRAME_COUNTER_FREQ);

		CLOCK_MAJOR();
		if(v & 0x80) CLOCK_MINOR();
		break;
	}

	if(!(nExternalSound & EXTSOUND_FDS))		return;

	//////////////////////////////////////////////////////////////////////////
	//   FDS Sound registers

	if(a < 0x4040)		return;

	// wave table
	if(a <= 0x407F)
	{
		if(mWave_FDS.bWaveWrite)
			mWave_FDS.nWaveTable[a - 0x4040] = v;
	}
	else
	{
		switch(a)
		{
		case 0x4080:
			mWave_FDS.nVolEnv_Mode = (v >> 6);
			if(v & 0x80)
			{
				mWave_FDS.nVolEnv_Gain = v & 0x3F;
				if(!mWave_FDS.nMainAddr)
				{
					if(mWave_FDS.nVolEnv_Gain < 0x20)	mWave_FDS.nVolume = mWave_FDS.nVolEnv_Gain;
					else								mWave_FDS.nVolume = 0x20;
				}
			}
			mWave_FDS.nVolEnv_Decay = v & 0x3F;
			mWave_FDS.nVolEnv_Timer = ((mWave_FDS.nVolEnv_Decay + 1) * mWave_FDS.nEnvelopeSpeed * 8);

			mWave_FDS.bVolEnv_On = mWave_FDS.bEnvelopeEnable && mWave_FDS.nEnvelopeSpeed && !(v & 0x80);
			break;

		case 0x4082:
			mWave_FDS.nFreq.B.l = v;
			mWave_FDS.bMain_On = mWave_FDS.nFreq.W && mWave_FDS.bEnabled && !mWave_FDS.bWaveWrite;
			break;

		case 0x4083:
			mWave_FDS.bEnabled =		!(v & 0x80);
			mWave_FDS.bEnvelopeEnable = !(v & 0x40);
			if(v & 0x80)
			{
				if(mWave_FDS.nVolEnv_Gain < 0x20)	mWave_FDS.nVolume = mWave_FDS.nVolEnv_Gain;
				else								mWave_FDS.nVolume = 0x20;
			}
			mWave_FDS.nFreq.B.h = v & 0x0F;
			mWave_FDS.bMain_On = mWave_FDS.nFreq.W && mWave_FDS.bEnabled && !mWave_FDS.bWaveWrite;

			mWave_FDS.bVolEnv_On = mWave_FDS.bEnvelopeEnable && mWave_FDS.nEnvelopeSpeed && !(mWave_FDS.nVolEnv_Mode & 2);
			mWave_FDS.bSweepEnv_On = mWave_FDS.bEnvelopeEnable && mWave_FDS.nEnvelopeSpeed && !(mWave_FDS.nSweep_Mode & 2);
			break;


		case 0x4084:
			mWave_FDS.nSweep_Mode = v >> 6;
			if(v & 0x80)
				mWave_FDS.nSweep_Gain = v & 0x3F;
			mWave_FDS.nSweep_Decay = v & 0x3F;
			mWave_FDS.nSweep_Timer = ((mWave_FDS.nSweep_Decay + 1) * mWave_FDS.nEnvelopeSpeed * 8);
			mWave_FDS.bSweepEnv_On = mWave_FDS.bEnvelopeEnable && mWave_FDS.nEnvelopeSpeed && !(v & 0x80);
			break;


		case 0x4085:
			if(v & 0x40)	mWave_FDS.nSweepBias = (v & 0x3F) - 0x40;
			else			mWave_FDS.nSweepBias = v & 0x3F;
			mWave_FDS.nLFO_Addr = 0;
			break;


		case 0x4086:
			mWave_FDS.nLFO_Freq.B.l = v;
			mWave_FDS.bLFO_On = mWave_FDS.bLFO_Enabled && mWave_FDS.nLFO_Freq.W;
			if(mWave_FDS.nLFO_Freq.W)
				mWave_FDS.fLFO_Timer = 65536.0f / mWave_FDS.nLFO_Freq.W;
			break;

		case 0x4087:
			mWave_FDS.bLFO_Enabled = !(v & 0x80);
			mWave_FDS.nLFO_Freq.B.h = v & 0x0F;
			mWave_FDS.bLFO_On = mWave_FDS.bLFO_Enabled && mWave_FDS.nLFO_Freq.W;
			if(mWave_FDS.nLFO_Freq.W)
				mWave_FDS.fLFO_Timer = 65536.0f / mWave_FDS.nLFO_Freq.W;
			break;

		case 0x4088:
			if(mWave_FDS.bLFO_Enabled)	break;
			register int i;
			for(i = 0; i < 62; i++)
				mWave_FDS.nLFO_Table[i] = mWave_FDS.nLFO_Table[i + 2];
			mWave_FDS.nLFO_Table[62] = mWave_FDS.nLFO_Table[63] = v & 7;
			break;

		case 0x4089:
			mWave_FDS.nMainVolume = v & 3;
			mWave_FDS.bWaveWrite = v & 0x80;
			mWave_FDS.bMain_On = mWave_FDS.nFreq.W && mWave_FDS.bEnabled && !mWave_FDS.bWaveWrite;
			break;

		case 0x408A:
			mWave_FDS.nEnvelopeSpeed = v;
			mWave_FDS.bVolEnv_On = mWave_FDS.bEnvelopeEnable && mWave_FDS.nEnvelopeSpeed && !(mWave_FDS.nVolEnv_Mode & 2);
			mWave_FDS.bSweepEnv_On = mWave_FDS.bEnvelopeEnable && mWave_FDS.nEnvelopeSpeed && !(mWave_FDS.nSweep_Mode & 2);
			break;
		}
	}
}

void FASTCALL CNSFCore::WriteMemory_VRC6(WORD a,BYTE v)
{
	EmulateAPU(1);

	if((a < 0xA000) && (nExternalSound & EXTSOUND_VRC7))
		WriteMemory_VRC7(a,v);
	else if(nExternalSound & EXTSOUND_FDS)
		WriteMemory_FDSRAM(a,v);

	switch(a)
	{
		//////////////////////////
		// Pulse 1
	case 0x9000:
		mWave_VRC6Pulse[0].nVolume = v & 0x0F;
		mWave_VRC6Pulse[0].nDutyCycle = (v >> 4) & 0x07;
		mWave_VRC6Pulse[0].bDigitized = v & 0x80;
		if(mWave_VRC6Pulse[0].bDigitized)
			mWave_VRC6Pulse[0].nDutyCount = 0;
		break;

	case 0x9001:
		mWave_VRC6Pulse[0].nFreqTimer.B.l = v;
		break;

	case 0x9002:
		mWave_VRC6Pulse[0].nFreqTimer.B.h = v & 0x0F;
		mWave_VRC6Pulse[0].bChannelEnabled = v & 0x80;
		break;
		

		//////////////////////////
		// Pulse 2
	case 0xA000:
		mWave_VRC6Pulse[1].nVolume = v & 0x0F;
		mWave_VRC6Pulse[1].nDutyCycle = (v >> 4) & 0x07;
		mWave_VRC6Pulse[1].bDigitized = v & 0x80;
		if(mWave_VRC6Pulse[1].bDigitized)
			mWave_VRC6Pulse[1].nDutyCount = 0;
		break;

	case 0xA001:
		mWave_VRC6Pulse[1].nFreqTimer.B.l = v;
		break;

	case 0xA002:
		mWave_VRC6Pulse[1].nFreqTimer.B.h = v & 0x0F;
		mWave_VRC6Pulse[1].bChannelEnabled = v & 0x80;
		break;
		
		//////////////////////////
		// Sawtooth
	case 0xB000:
		mWave_VRC6Saw.nAccumRate = (v & 0x3F);
		break;

	case 0xB001:
		mWave_VRC6Saw.nFreqTimer.B.l = v;
		break;

	case 0xB002:
		mWave_VRC6Saw.nFreqTimer.B.h = v & 0x0F;
		mWave_VRC6Saw.bChannelEnabled = v & 0x80;
		break;
	}
}

void FASTCALL CNSFCore::WriteMemory_MMC5(WORD a,BYTE v)
{
	if((a <= 0x5015))
	{
		EmulateAPU(1);
		switch(a)
		{
			//////////////////////////
			// Square 1
		case 0x5000:
			mWave_MMC5Square[0].nDutyCycle = DUTY_CYCLE_TABLE[v >> 6];
			mWave_MMC5Square[0].bLengthEnabled = !(mWave_MMC5Square[0].bDecayLoop = (v & 0x20));
			mWave_MMC5Square[0].bDecayEnable = !(v & 0x10);
			mWave_MMC5Square[0].nDecayTimer = (v & 0x0F);

			if(!mWave_MMC5Square[0].bDecayEnable)
				mWave_MMC5Square[0].nVolume = mWave_MMC5Square[0].nDecayTimer;
			break;
		
		case 0x5002:
			mWave_MMC5Square[0].nFreqTimer.B.l = v;
			break;
		
		case 0x5003:
			mWave_MMC5Square[0].nFreqTimer.B.h = v & 0x07;
			mWave_MMC5Square[0].nDecayVolume = 0x0F;

			if(mWave_MMC5Square[0].bChannelEnabled)
				mWave_MMC5Square[0].nLengthCount = LENGTH_COUNTER_TABLE[v >> 3];
			break;
			
			//////////////////////////
			// Square 2
		case 0x5004:
			mWave_MMC5Square[1].nDutyCycle = DUTY_CYCLE_TABLE[v >> 6];
			mWave_MMC5Square[1].bLengthEnabled = !(mWave_MMC5Square[1].bDecayLoop = (v & 0x20));
			mWave_MMC5Square[1].bDecayEnable = !(v & 0x10);
			mWave_MMC5Square[1].nDecayTimer = (v & 0x0F);

			if(!mWave_MMC5Square[1].bDecayEnable)
				mWave_MMC5Square[1].nVolume = mWave_MMC5Square[1].nDecayTimer;
			break;
		
		case 0x5006:
			mWave_MMC5Square[1].nFreqTimer.B.l = v;
			break;
		
		case 0x5007:
			mWave_MMC5Square[1].nFreqTimer.B.h = v & 0x07;
			mWave_MMC5Square[1].nDecayVolume = 0x0F;

			if(mWave_MMC5Square[1].bChannelEnabled)
				mWave_MMC5Square[1].nLengthCount = LENGTH_COUNTER_TABLE[v >> 3];
			break;

		case 0x5011:
			mWave_MMC5Voice.nOutput = v & 0x7F;
			break;
		

		case 0x5015:
			if(v & 0x01){	mWave_MMC5Square[0].bChannelEnabled =										1;	}
			else		{	mWave_MMC5Square[0].bChannelEnabled = mWave_MMC5Square[0].nLengthCount =	0;	}
			if(v & 0x02){	mWave_MMC5Square[1].bChannelEnabled =										1;	}
			else		{	mWave_MMC5Square[1].bChannelEnabled = mWave_MMC5Square[1].nLengthCount =	0;	}
			break;
		}
		return;
	}

	if(a == 0x5205)
	{
		nMultIn_Low = v;
		goto multiply;
	}
	if(a == 0x5206)
	{
		nMultIn_High = v;
multiply:
		a = nMultIn_Low * nMultIn_High;
		pExRAM[0x205] = a & 0xFF;
		pExRAM[0x206] = a >> 8;
		return;
	}

	if(a < 0x5C00) return;

	pExRAM[a & 0x0FFF] = v;
	if(a >= 0x5FF6)
		WriteMemory_ExRAM(a,v);
}

void FASTCALL CNSFCore::WriteMemory_N106(WORD a,BYTE v)
{
	if(a < 0x4800)
	{
		WriteMemory_pAPU(a,v);
		return;
	}

	if(a == 0xF800)
	{
		mWave_N106.nCurrentAddress = v & 0x7F;
		mWave_N106.bAutoIncrement = (v & 0x80);
		return;
	}

	if(a == 0x4800)
	{
		EmulateAPU(1);
		mWave_N106.nRAM[mWave_N106.nCurrentAddress << 1] = v & 0x0F;
		mWave_N106.nRAM[(mWave_N106.nCurrentAddress << 1) + 1] = v >> 4;
		a = mWave_N106.nCurrentAddress;
		if(mWave_N106.bAutoIncrement)
			mWave_N106.nCurrentAddress = (mWave_N106.nCurrentAddress + 1) & 0x7F;

#define N106REGWRITE(ch,r0,r1,r2,r3,r4)							\
	case r0:	if(mWave_N106.nFreqReg[ch].B.l == v) break;		\
				mWave_N106.nFreqReg[ch].B.l = v;				\
				mWave_N106.fFreqTimer[ch] = -1.0f;				\
				break;											\
	case r1:	if(mWave_N106.nFreqReg[ch].B.h == v) break;		\
				mWave_N106.nFreqReg[ch].B.h = v;				\
				mWave_N106.fFreqTimer[ch] = -1.0f;				\
				break;											\
	case r2:	if(mWave_N106.nFreqReg[ch].B.w != (v & 3)){		\
					mWave_N106.nFreqReg[ch].B.w = v & 0x03;		\
					mWave_N106.fFreqTimer[ch] = -1.0f;}			\
				mWave_N106.nWaveSize[ch] = 256 - (v & 0xFC);	\
				mWave_N106.nWaveSizeWritten[ch] = (v >> 2) & 0x3F;	\
				break;											\
	case r3:	mWave_N106.nWavePosStart[ch] = v;				\
				break;											\
	case r4:	mWave_N106.nPreVolume[ch] = v & 0x0F;			\
				if(!bN106PopReducer)							\
					mWave_N106.nVolume[ch] = v & 0x0F

		switch(a)
		{
			N106REGWRITE(0,0x40,0x42,0x44,0x46,0x47); break;
			N106REGWRITE(1,0x48,0x4A,0x4C,0x4E,0x4F); break;
			N106REGWRITE(2,0x50,0x52,0x54,0x56,0x57); break;
			N106REGWRITE(3,0x58,0x5A,0x5C,0x5E,0x5F); break;
			N106REGWRITE(4,0x60,0x62,0x64,0x66,0x67); break;
			N106REGWRITE(5,0x68,0x6A,0x6C,0x6E,0x6F); break;
			N106REGWRITE(6,0x70,0x72,0x74,0x76,0x77); break;
			N106REGWRITE(7,0x78,0x7A,0x7C,0x7E,0x7F);
				v = (v >> 4) & 7;
				if(mWave_N106.nActiveChannels == v) break;
				mWave_N106.nActiveChannels = v;
				mWave_N106.fFreqTimer[0] = -1.0f;
				mWave_N106.fFreqTimer[1] = -1.0f;
				mWave_N106.fFreqTimer[2] = -1.0f;
				mWave_N106.fFreqTimer[3] = -1.0f;
				mWave_N106.fFreqTimer[4] = -1.0f;
				mWave_N106.fFreqTimer[5] = -1.0f;
				mWave_N106.fFreqTimer[6] = -1.0f;
				mWave_N106.fFreqTimer[7] = -1.0f;
				break;
		}
#undef N106REGWRITE
	}

	// Handle conflicts if both N163 and S5B are active.
	if ((nExternalSound & EXTSOUND_FME07) && a >= 0xE000)
		WriteMemory_FME07(a,v);
}

void CNSFCore::WriteMemory_VRC7(WORD a,BYTE v)
{
	if(a == 0x9010)
	{
		nVRC7Address = v;
		return;
	}
	if(a == 0x9030)
	{
		if(pVRC7Buffer)
			VRC7_Mix();
		VRC7_Write(v);
	}
}

void CNSFCore::WriteMemory_FME07(WORD a,BYTE v)
{
	if((a < 0xD000) && (nExternalSound & EXTSOUND_FDS))
		WriteMemory_FDSRAM(a,v);

	if(a == 0xC000)
		nFME07_Address = v;
	if(a == 0xE000 || a == 0xF000) // TODO : It should be anything from 0xE000 to 0xFFFF.
	{
		switch(nFME07_Address)
		{
		case 0x00:	mWave_FME07[0].nFreqTimer.B.l = v;			break;
		case 0x01:	mWave_FME07[0].nFreqTimer.B.h = v & 0x0F;	break;
		case 0x02:	mWave_FME07[1].nFreqTimer.B.l = v;			break;
		case 0x03:	mWave_FME07[1].nFreqTimer.B.h = v & 0x0F;	break;
		case 0x04:	mWave_FME07[2].nFreqTimer.B.l = v;			break;
		case 0x05:	mWave_FME07[2].nFreqTimer.B.h = v & 0x0F;	break;
		case 0x06:	mWave_FME07[0].bNoiseFrequency = v;	        break;
		case 0x07:
			mWave_FME07[0].bChannelMixer   = (v & 0x09);
			mWave_FME07[1].bChannelMixer   = (v & 0x12) >> 1;
			mWave_FME07[2].bChannelMixer   = (v & 0x24) >> 2;
			mWave_FME07[0].bChannelEnabled = (mWave_FME07[0].bChannelMixer != 0x9) || (mWave_FME07[0].bEnvelopeEnabled) ? 1 : 0;
			mWave_FME07[1].bChannelEnabled = (mWave_FME07[1].bChannelMixer != 0x9) || (mWave_FME07[1].bEnvelopeEnabled) ? 1 : 0;
			mWave_FME07[2].bChannelEnabled = (mWave_FME07[2].bChannelMixer != 0x9) || (mWave_FME07[2].bEnvelopeEnabled) ? 1 : 0;
			break;
		case 0x08:	
			mWave_FME07[0].bEnvelopeEnabled = (v & 0x10) != 0;
			mWave_FME07[0].bChannelEnabled = (mWave_FME07[0].bChannelMixer != 0x9) || (mWave_FME07[0].bEnvelopeEnabled) ? 1 : 0;
			mWave_FME07[0].nVolume = v & 0x0F;
			break;
		case 0x09:	
			mWave_FME07[1].bEnvelopeEnabled = (v & 0x10) != 0;
			mWave_FME07[1].bChannelEnabled = (mWave_FME07[1].bChannelMixer != 0x9) || (mWave_FME07[1].bEnvelopeEnabled) ? 1 : 0;
			mWave_FME07[1].nVolume = v & 0x0F;
			break;
		case 0x0A:	
			mWave_FME07[2].bEnvelopeEnabled = (v & 0x10) != 0;
			mWave_FME07[2].bChannelEnabled = (mWave_FME07[2].bChannelMixer != 0x9) || (mWave_FME07[2].bEnvelopeEnabled) ? 1 : 0;
			mWave_FME07[2].nVolume = v & 0x0F;
			break;
		case 0x0B: mWave_FME07[0].nEnvFreq.B.l = v; break;
		case 0x0C: mWave_FME07[0].nEnvFreq.B.h = v; break;
		case 0x0D:
			mWave_FME07[0].nEnvelopeShape = v & 0x0F;
			mWave_FME07[0].bEnvelopeTriggered = 1;
			break;
		}
	}
}

void CNSFCore::WriteMemory_EPSM(WORD a, BYTE v)
{
	/*if ((a < 0xD000) && (nExternalSound & EXTSOUND_FDS))
		WriteMemory_FDSRAM(a, v);*/

	if (a == 0x401c)
		nEPSM_Address = v;
	if (a == 0x401d)
	{
		switch (nEPSM_Address)
		{
		case 0x00:	mWave_EPSM[0].nFreqTimer.B.l = v;			break;
		case 0x01:	mWave_EPSM[0].nFreqTimer.B.h = v & 0x0F;	break;
		case 0x02:	mWave_EPSM[1].nFreqTimer.B.l = v;			break;
		case 0x03:	mWave_EPSM[1].nFreqTimer.B.h = v & 0x0F;	break;
		case 0x04:	mWave_EPSM[2].nFreqTimer.B.l = v;			break;
		case 0x05:	mWave_EPSM[2].nFreqTimer.B.h = v & 0x0F;	break;
		case 0x06:	mWave_EPSM[0].bNoiseFrequency = v;	        break;
		case 0x07:
			mWave_EPSM[0].bChannelMixer = (v & 0x09);
			mWave_EPSM[1].bChannelMixer = (v & 0x12) >> 1;
			mWave_EPSM[2].bChannelMixer = (v & 0x24) >> 2;
			mWave_EPSM[0].bChannelEnabled = (mWave_EPSM[0].bChannelMixer != 0x9) || (mWave_EPSM[0].bEnvelopeEnabled) ? 1 : 0;
			mWave_EPSM[1].bChannelEnabled = (mWave_EPSM[1].bChannelMixer != 0x9) || (mWave_EPSM[1].bEnvelopeEnabled) ? 1 : 0;
			mWave_EPSM[2].bChannelEnabled = (mWave_EPSM[2].bChannelMixer != 0x9) || (mWave_EPSM[2].bEnvelopeEnabled) ? 1 : 0;
			break;
		case 0x08:
			mWave_EPSM[0].bEnvelopeEnabled = (v & 0x10) != 0;
			mWave_EPSM[0].bChannelEnabled = (mWave_FME07[0].bChannelMixer != 0x9) || (mWave_FME07[0].bEnvelopeEnabled) ? 1 : 0;
			mWave_EPSM[0].nVolume = v & 0x0F; break;
		case 0x09:
			mWave_EPSM[1].bEnvelopeEnabled = (v & 0x10) != 0;
			mWave_EPSM[1].bChannelEnabled = (mWave_FME07[1].bChannelMixer != 0x9) || (mWave_FME07[1].bEnvelopeEnabled) ? 1 : 0;
			mWave_EPSM[1].nVolume = v & 0x0F; break;
		case 0x0A:
			mWave_EPSM[2].bEnvelopeEnabled = (v & 0x10) != 0;
			mWave_EPSM[2].bChannelEnabled = (mWave_FME07[2].bChannelMixer != 0x9) || (mWave_FME07[2].bEnvelopeEnabled) ? 1 : 0;
			mWave_EPSM[2].nVolume = v & 0x0F; break;
		case 0x0B:  mWave_EPSM[0].nEnvFreq.B.l = v; break;
		case 0x0C:  mWave_EPSM[0].nEnvFreq.B.h = v; break;
		case 0x0D:
			mWave_EPSM[0].nEnvelopeShape = v & 0x0F;
			mWave_EPSM[0].bEnvelopeTriggered = 1;
			break;
		case 0x10:
			mWave_EPSM[9].bChannelEnabled =  (v & 0x01);
			mWave_EPSM[10].bChannelEnabled = (v & 0x02);
			mWave_EPSM[11].bChannelEnabled = (v & 0x04);
			mWave_EPSM[12].bChannelEnabled = (v & 0x08);
			mWave_EPSM[13].bChannelEnabled = (v & 0x10);
			mWave_EPSM[14].bChannelEnabled = (v & 0x20);
			//std::cout << "value " << std::hex << (int)v << std::endl;
			break;
		case 0x18:	mWave_EPSM[9].nVolume = v; break;
		case 0x19:	mWave_EPSM[10].nVolume = v; break;
		case 0x1A:	mWave_EPSM[11].nVolume = v; break;
		case 0x1B:	mWave_EPSM[12].nVolume = v; break;
		case 0x1C:	mWave_EPSM[13].nVolume = v; break;
		case 0x1D:	mWave_EPSM[14].nVolume = v; break;
		case 0x28:
				if ((v & 0x7) == 0) {
					if ((v & 0xf0) && !mWave_EPSM[3].bChannelEnabled)
							mWave_EPSM[3].nTriggered = 1;
					mWave_EPSM[3].bChannelEnabled = (v & 0xf0) ? 1 : 0;
				}
				if ((v & 0x7) == 1) {
					if ((v & 0xf0) && !mWave_EPSM[4].bChannelEnabled)
						mWave_EPSM[4].nTriggered = 1;
					mWave_EPSM[4].bChannelEnabled = (v & 0xf0) ? 1 : 0;
				}
				if ((v & 0x7) == 2) {
					if ((v & 0xf0) && !mWave_EPSM[5].bChannelEnabled)
						mWave_EPSM[5].nTriggered = 1;
					mWave_EPSM[5].bChannelEnabled = (v & 0xf0) ? 1 : 0;
				}
				if ((v & 0x7) == 4) {
					if ((v & 0xf0) && !mWave_EPSM[6].bChannelEnabled)
						mWave_EPSM[6].nTriggered = 1;
					mWave_EPSM[6].bChannelEnabled = (v & 0xf0) ? 1 : 0;
				}
				if ((v & 0x7) == 5) {
					if ((v & 0xf0) && !mWave_EPSM[7].bChannelEnabled)
						mWave_EPSM[7].nTriggered = 1;
					mWave_EPSM[7].bChannelEnabled = (v & 0xf0) ? 1 : 0;
				}
				if ((v & 0x7) == 6) {
					if ((v & 0xf0) && !mWave_EPSM[8].bChannelEnabled)
						mWave_EPSM[8].nTriggered = 1;
					mWave_EPSM[8].bChannelEnabled = (v & 0xf0) ? 1 : 0;
				}
			//std::cout << "value " << std::hex << (int)v << std::endl;
			break;
		case 0xA0:	mWave_EPSM[3].nFreqTimer.B.l = v;			break;
		case 0xA4:	mWave_EPSM[3].nFreqTimer.B.h = v & 0x3F;	break;
		case 0xA1:	mWave_EPSM[4].nFreqTimer.B.l = v;			break;
		case 0xA5:	mWave_EPSM[4].nFreqTimer.B.h = v & 0x3F;	break;
		case 0xA2:	mWave_EPSM[5].nFreqTimer.B.l = v;			break;
		case 0xA6:	mWave_EPSM[5].nFreqTimer.B.h = v & 0x3F;	break;
		case 0x22:  
			mWave_EPSM[3].nPatchReg[30] = v;
			mWave_EPSM[4].nPatchReg[30] = v;
			mWave_EPSM[5].nPatchReg[30] = v;
			mWave_EPSM[6].nPatchReg[30] = v;
			mWave_EPSM[7].nPatchReg[30] = v;
			mWave_EPSM[8].nPatchReg[30] = v;
			break;
		case 0xB0:  mWave_EPSM[3].nPatchReg[0] = v;break;
		case 0xB1:  mWave_EPSM[4].nPatchReg[0] = v;break;
		case 0xB2:  mWave_EPSM[5].nPatchReg[0] = v;break;
			break;
		case 0xB4:	mWave_EPSM[3].nStereo = v & 0xc0;
			mWave_EPSM[3].nPatchReg[1] = v;
			break;
		case 0xB5:	mWave_EPSM[4].nStereo = v & 0xc0;
			mWave_EPSM[4].nPatchReg[1] = v;
			break;
		case 0xB6:	mWave_EPSM[5].nStereo = v & 0xc0;
			mWave_EPSM[5].nPatchReg[1] = v;
			break;
		case 0x30:  mWave_EPSM[3].nPatchReg[2] = (v & 0x7f);break;
		case 0x31:  mWave_EPSM[4].nPatchReg[2] = (v & 0x7f);break;
		case 0x32:  mWave_EPSM[5].nPatchReg[2] = (v & 0x7f);break;
		case 0x40:  mWave_EPSM[3].nPatchReg[3] = (v & 0x7f);break;
		case 0x41:  mWave_EPSM[4].nPatchReg[3] = (v & 0x7f);break;
		case 0x42:  mWave_EPSM[5].nPatchReg[3] = (v & 0x7f);break;
		case 0x50:  mWave_EPSM[3].nPatchReg[4] = v;break;
		case 0x51:  mWave_EPSM[4].nPatchReg[4] = v;break;
		case 0x52:  mWave_EPSM[5].nPatchReg[4] = v;break;
		case 0x60:  mWave_EPSM[3].nPatchReg[5] = v;break;
		case 0x61:  mWave_EPSM[4].nPatchReg[5] = v;break;
		case 0x62:  mWave_EPSM[5].nPatchReg[5] = v;break;
		case 0x70:  mWave_EPSM[3].nPatchReg[6] = v;break;
		case 0x71:  mWave_EPSM[4].nPatchReg[6] = v;break;
		case 0x72:  mWave_EPSM[5].nPatchReg[6] = v;break;
		case 0x80:  mWave_EPSM[3].nPatchReg[7] = v;break;
		case 0x81:  mWave_EPSM[4].nPatchReg[7] = v;break;
		case 0x82:  mWave_EPSM[5].nPatchReg[7] = v;break;
		case 0x90:  mWave_EPSM[3].nPatchReg[8] = v;break;
		case 0x91:  mWave_EPSM[4].nPatchReg[8] = v;break;
		case 0x92:  mWave_EPSM[5].nPatchReg[8] = v;break;
		case 0x38:  mWave_EPSM[3].nPatchReg[9] = (v & 0x7f);break;
		case 0x39:  mWave_EPSM[4].nPatchReg[9] = (v & 0x7f);break;
		case 0x3a:  mWave_EPSM[5].nPatchReg[9] = (v & 0x7f);break;
		case 0x48:  mWave_EPSM[3].nPatchReg[10] = (v & 0x7f);break;
		case 0x49:  mWave_EPSM[4].nPatchReg[10] = (v & 0x7f);break;
		case 0x4a:  mWave_EPSM[5].nPatchReg[10] = (v & 0x7f);break;
		case 0x58:  mWave_EPSM[3].nPatchReg[11] = v;break;
		case 0x59:  mWave_EPSM[4].nPatchReg[11] = v;break;
		case 0x5a:  mWave_EPSM[5].nPatchReg[11] = v;break;
		case 0x68:  mWave_EPSM[3].nPatchReg[12] = v;break;
		case 0x69:  mWave_EPSM[4].nPatchReg[12] = v;break;
		case 0x6a:  mWave_EPSM[5].nPatchReg[12] = v;break;
		case 0x78:  mWave_EPSM[3].nPatchReg[13] = v;break;
		case 0x79:  mWave_EPSM[4].nPatchReg[13] = v;break;
		case 0x7a:  mWave_EPSM[5].nPatchReg[13] = v;break;
		case 0x88:  mWave_EPSM[3].nPatchReg[14] = v;break;
		case 0x89:  mWave_EPSM[4].nPatchReg[14] = v;break;
		case 0x8a:  mWave_EPSM[5].nPatchReg[14] = v;break;
		case 0x98:  mWave_EPSM[3].nPatchReg[15] = v;break;
		case 0x99:  mWave_EPSM[4].nPatchReg[15] = v;break;
		case 0x9a:  mWave_EPSM[5].nPatchReg[15] = v;break;
		case 0x34:  mWave_EPSM[3].nPatchReg[16] = (v & 0x7f);break;
		case 0x35:  mWave_EPSM[4].nPatchReg[16] = (v & 0x7f);break;
		case 0x36:  mWave_EPSM[5].nPatchReg[16] = (v & 0x7f);break;
		case 0x44:  mWave_EPSM[3].nPatchReg[17] = (v & 0x7f);break;
		case 0x45:  mWave_EPSM[4].nPatchReg[17] = (v & 0x7f);break;
		case 0x46:  mWave_EPSM[5].nPatchReg[17] = (v & 0x7f);break;
		case 0x54:  mWave_EPSM[3].nPatchReg[18] = v;break;
		case 0x55:  mWave_EPSM[4].nPatchReg[18] = v;break;
		case 0x56:  mWave_EPSM[5].nPatchReg[18] = v;break;
		case 0x64:  mWave_EPSM[3].nPatchReg[19] = v;break;
		case 0x65:  mWave_EPSM[4].nPatchReg[19] = v;break;
		case 0x66:  mWave_EPSM[5].nPatchReg[19] = v;break;
		case 0x74:  mWave_EPSM[3].nPatchReg[20] = v;break;
		case 0x75:  mWave_EPSM[4].nPatchReg[20] = v;break;
		case 0x76:  mWave_EPSM[5].nPatchReg[20] = v;break;
		case 0x84:  mWave_EPSM[3].nPatchReg[21] = v;break;
		case 0x85:  mWave_EPSM[4].nPatchReg[21] = v;break;
		case 0x86:  mWave_EPSM[5].nPatchReg[21] = v;break;
		case 0x94:  mWave_EPSM[3].nPatchReg[22] = v;break;
		case 0x95:  mWave_EPSM[4].nPatchReg[22] = v;break;
		case 0x96:  mWave_EPSM[5].nPatchReg[22] = v;break;
		case 0x3c:  mWave_EPSM[3].nPatchReg[23] = (v & 0x7f);break;
		case 0x3d:  mWave_EPSM[4].nPatchReg[23] = (v & 0x7f);break;
		case 0x3e:  mWave_EPSM[5].nPatchReg[23] = (v & 0x7f);break;
		case 0x4c:  mWave_EPSM[3].nPatchReg[24] = (v & 0x7f);break;
		case 0x4d:  mWave_EPSM[4].nPatchReg[24] = (v & 0x7f);break;
		case 0x4e:  mWave_EPSM[5].nPatchReg[24] = (v & 0x7f);break;
		case 0x5c:  mWave_EPSM[3].nPatchReg[25] = v;break;
		case 0x5d:  mWave_EPSM[4].nPatchReg[25] = v;break;
		case 0x5e:  mWave_EPSM[5].nPatchReg[25] = v;break;
		case 0x6c:  mWave_EPSM[3].nPatchReg[26] = v;break;
		case 0x6d:  mWave_EPSM[4].nPatchReg[26] = v;break;
		case 0x6e:  mWave_EPSM[5].nPatchReg[26] = v;break;
		case 0x7c:  mWave_EPSM[3].nPatchReg[27] = v;break;
		case 0x7d:  mWave_EPSM[4].nPatchReg[27] = v;break;
		case 0x7e:  mWave_EPSM[5].nPatchReg[27] = v;break;
		case 0x8c:  mWave_EPSM[3].nPatchReg[28] = v;break;
		case 0x8d:  mWave_EPSM[4].nPatchReg[28] = v;break;
		case 0x8e:  mWave_EPSM[5].nPatchReg[28] = v;break;
		case 0x9c:  mWave_EPSM[3].nPatchReg[29] = v;break;
		case 0x9d:  mWave_EPSM[4].nPatchReg[29] = v;break;
		case 0x9e:  mWave_EPSM[5].nPatchReg[29] = v;break;
		}
	}
	if (a == 0x401e)
		nEPSM_Address = v;
	if (a == 0x401f)
	{
		switch (nEPSM_Address)
		{
		case 0xA0:	mWave_EPSM[6].nFreqTimer.B.l = v;			break; //todo freq calc
		case 0xA4:	mWave_EPSM[6].nFreqTimer.B.h = v & 0x3F;	break;
		case 0xA1:	mWave_EPSM[7].nFreqTimer.B.l = v;			break;
		case 0xA5:	mWave_EPSM[7].nFreqTimer.B.h = v & 0x3F;	break;
		case 0xA2:	mWave_EPSM[8].nFreqTimer.B.l = v;			break;
		case 0xA6:	mWave_EPSM[8].nFreqTimer.B.h = v & 0x3F;	break;
		
		case 0xB0:  mWave_EPSM[6].nPatchReg[0] = v;break;
		case 0xB1:  mWave_EPSM[7].nPatchReg[0] = v;break;
		case 0xB2:  mWave_EPSM[8].nPatchReg[0] = v;break;
			break;
		case 0xB4:	mWave_EPSM[6].nStereo = v & 0xc0;
			mWave_EPSM[6].nPatchReg[1] = v;
			break;
		case 0xB5:	mWave_EPSM[7].nStereo = v & 0xc0;
			mWave_EPSM[7].nPatchReg[1] = v;
			break;
		case 0xB6:	mWave_EPSM[8].nStereo = v & 0xc0;
			mWave_EPSM[8].nPatchReg[1] = v;
			break;
		case 0x30:  mWave_EPSM[6].nPatchReg[2] = (v & 0x7f);break;
		case 0x31:  mWave_EPSM[7].nPatchReg[2] = (v & 0x7f);break;
		case 0x32:  mWave_EPSM[8].nPatchReg[2] = (v & 0x7f);break;
		case 0x40:  mWave_EPSM[6].nPatchReg[3] = (v & 0x7f);break;
		case 0x41:  mWave_EPSM[7].nPatchReg[3] = (v & 0x7f);break;
		case 0x42:  mWave_EPSM[8].nPatchReg[3] = (v & 0x7f);break;
		case 0x50:  mWave_EPSM[6].nPatchReg[4] = v;break;
		case 0x51:  mWave_EPSM[7].nPatchReg[4] = v;break;
		case 0x52:  mWave_EPSM[8].nPatchReg[4] = v;break;
		case 0x60:  mWave_EPSM[6].nPatchReg[5] = v;break;
		case 0x61:  mWave_EPSM[7].nPatchReg[5] = v;break;
		case 0x62:  mWave_EPSM[8].nPatchReg[5] = v;break;
		case 0x70:  mWave_EPSM[6].nPatchReg[6] = v;break;
		case 0x71:  mWave_EPSM[7].nPatchReg[6] = v;break;
		case 0x72:  mWave_EPSM[8].nPatchReg[6] = v;break;
		case 0x80:  mWave_EPSM[6].nPatchReg[7] = v;break;
		case 0x81:  mWave_EPSM[7].nPatchReg[7] = v;break;
		case 0x82:  mWave_EPSM[8].nPatchReg[7] = v;break;
		case 0x90:  mWave_EPSM[6].nPatchReg[8] = v;break;
		case 0x91:  mWave_EPSM[7].nPatchReg[8] = v;break;
		case 0x92:  mWave_EPSM[8].nPatchReg[8] = v;break;
		case 0x38:  mWave_EPSM[6].nPatchReg[9] = (v & 0x7f);break;
		case 0x39:  mWave_EPSM[7].nPatchReg[9] = (v & 0x7f);break;
		case 0x3a:  mWave_EPSM[8].nPatchReg[9] = (v & 0x7f);break;
		case 0x48:  mWave_EPSM[6].nPatchReg[10] = (v & 0x7f);break;
		case 0x49:  mWave_EPSM[7].nPatchReg[10] = (v & 0x7f);break;
		case 0x4a:  mWave_EPSM[8].nPatchReg[10] = (v & 0x7f);break;
		case 0x58:  mWave_EPSM[6].nPatchReg[11] = v;break;
		case 0x59:  mWave_EPSM[7].nPatchReg[11] = v;break;
		case 0x5a:  mWave_EPSM[8].nPatchReg[11] = v;break;
		case 0x68:  mWave_EPSM[6].nPatchReg[12] = v;break;
		case 0x69:  mWave_EPSM[7].nPatchReg[12] = v;break;
		case 0x6a:  mWave_EPSM[8].nPatchReg[12] = v;break;
		case 0x78:  mWave_EPSM[6].nPatchReg[13] = v;break;
		case 0x79:  mWave_EPSM[7].nPatchReg[13] = v;break;
		case 0x7a:  mWave_EPSM[8].nPatchReg[13] = v;break;
		case 0x88:  mWave_EPSM[6].nPatchReg[14] = v;break;
		case 0x89:  mWave_EPSM[7].nPatchReg[14] = v;break;
		case 0x8a:  mWave_EPSM[8].nPatchReg[14] = v;break;
		case 0x98:  mWave_EPSM[6].nPatchReg[15] = v;break;
		case 0x99:  mWave_EPSM[7].nPatchReg[15] = v;break;
		case 0x9a:  mWave_EPSM[8].nPatchReg[15] = v;break;
		case 0x34:  mWave_EPSM[6].nPatchReg[16] = (v & 0x7f);break;
		case 0x35:  mWave_EPSM[7].nPatchReg[16] = (v & 0x7f);break;
		case 0x36:  mWave_EPSM[8].nPatchReg[16] = (v & 0x7f);break;
		case 0x44:  mWave_EPSM[6].nPatchReg[17] = (v & 0x7f);break;
		case 0x45:  mWave_EPSM[7].nPatchReg[17] = (v & 0x7f);break;
		case 0x46:  mWave_EPSM[8].nPatchReg[17] = (v & 0x7f);break;
		case 0x54:  mWave_EPSM[6].nPatchReg[18] = v;break;
		case 0x55:  mWave_EPSM[7].nPatchReg[18] = v;break;
		case 0x56:  mWave_EPSM[8].nPatchReg[18] = v;break;
		case 0x64:  mWave_EPSM[6].nPatchReg[19] = v;break;
		case 0x65:  mWave_EPSM[7].nPatchReg[19] = v;break;
		case 0x66:  mWave_EPSM[8].nPatchReg[19] = v;break;
		case 0x74:  mWave_EPSM[6].nPatchReg[20] = v;break;
		case 0x75:  mWave_EPSM[7].nPatchReg[20] = v;break;
		case 0x76:  mWave_EPSM[8].nPatchReg[20] = v;break;
		case 0x84:  mWave_EPSM[6].nPatchReg[21] = v;break;
		case 0x85:  mWave_EPSM[7].nPatchReg[21] = v;break;
		case 0x86:  mWave_EPSM[8].nPatchReg[21] = v;break;
		case 0x94:  mWave_EPSM[6].nPatchReg[22] = v;break;
		case 0x95:  mWave_EPSM[7].nPatchReg[22] = v;break;
		case 0x96:  mWave_EPSM[8].nPatchReg[22] = v;break;
		case 0x3c:  mWave_EPSM[6].nPatchReg[23] = (v & 0x7f);break;
		case 0x3d:  mWave_EPSM[7].nPatchReg[23] = (v & 0x7f);break;
		case 0x3e:  mWave_EPSM[8].nPatchReg[23] = (v & 0x7f);break;
		case 0x4c:  mWave_EPSM[6].nPatchReg[24] = (v & 0x7f);break;
		case 0x4d:  mWave_EPSM[7].nPatchReg[24] = (v & 0x7f);break;
		case 0x4e:  mWave_EPSM[8].nPatchReg[24] = (v & 0x7f);break;
		case 0x5c:  mWave_EPSM[6].nPatchReg[25] = v;break;
		case 0x5d:  mWave_EPSM[7].nPatchReg[25] = v;break;
		case 0x5e:  mWave_EPSM[8].nPatchReg[25] = v;break;
		case 0x6c:  mWave_EPSM[6].nPatchReg[26] = v;break;
		case 0x6d:  mWave_EPSM[7].nPatchReg[26] = v;break;
		case 0x6e:  mWave_EPSM[8].nPatchReg[26] = v;break;
		case 0x7c:  mWave_EPSM[6].nPatchReg[27] = v;break;
		case 0x7d:  mWave_EPSM[7].nPatchReg[27] = v;break;
		case 0x7e:  mWave_EPSM[8].nPatchReg[27] = v;break;
		case 0x8c:  mWave_EPSM[6].nPatchReg[28] = v;break;
		case 0x8d:  mWave_EPSM[7].nPatchReg[28] = v;break;
		case 0x8e:  mWave_EPSM[8].nPatchReg[28] = v;break;
		case 0x9c:  mWave_EPSM[6].nPatchReg[29] = v;break;
		case 0x9d:  mWave_EPSM[7].nPatchReg[29] = v;break;
		case 0x9e:  mWave_EPSM[8].nPatchReg[29] = v;break;
		}
	}
}


//////////////////////////////////////////////////////////////////////////
//
//		Emulate APU
//

void CNSFCore::EmulateAPU(BYTE bBurnCPUCycles)
{
	int fulltick = (signed)(nCPUCycle - nAPUCycle);
	int tick;

	int mixL, mixR;
	INT64 diff;
	int dif;

	if(bFade && nSilentSampleMax && (nSilentSamples >= nSilentSampleMax))
		fulltick = 0;

	while(fulltick)
	{
		if(pOutput)
		{
			if(fTicksUntilNextFrame < fTicksUntilNextSample)
				tick = (int)ceil(fTicksUntilNextFrame);
			else
				tick = (int)ceil(fTicksUntilNextSample);
		}
		else
			tick = (int)ceil(fTicksUntilNextFrame);

		tick = min(tick,fulltick);

		fulltick -= tick;

		// Sample Generation
		nDownsample += tick;
		mWave_Squares.DoTicks(tick);
		mixL = mWave_TND.DoTicks(tick);

		if(nExternalSound)
		{
			if(nExternalSound & EXTSOUND_VRC6)
			{
				mWave_VRC6Pulse[0].DoTicks(tick,bChannelMix[0]);
				mWave_VRC6Pulse[1].DoTicks(tick,bChannelMix[1]);
				mWave_VRC6Saw.DoTicks(tick,bChannelMix[2]);
			}
			if(nExternalSound & EXTSOUND_MMC5)
			{
				mWave_MMC5Square[0].DoTicks(tick,bChannelMix[3]);
				mWave_MMC5Square[1].DoTicks(tick,bChannelMix[4]);
				if(bChannelMix[5]) mWave_MMC5Voice.DoTicks(tick);
			}
			if(nExternalSound & EXTSOUND_N106)
				mWave_N106.DoTicks(tick,&bChannelMix[6]);
			if(nExternalSound & EXTSOUND_FME07)
			{
				mWave_FME07[0].DoTicks(tick,bChannelMix[20]);
				mWave_FME07[1].DoTicks(tick,bChannelMix[21]);
				mWave_FME07[2].DoTicks(tick,bChannelMix[22]);
			}
			if (nExternalSound & EXTSOUND_EPSM)
			{
				mWave_EPSM[0].DoTicks(tick, bChannelMix[20]);
				mWave_EPSM[1].DoTicks(tick, bChannelMix[21]);
				mWave_EPSM[2].DoTicks(tick, bChannelMix[22]);
			}
			if(nExternalSound & EXTSOUND_FDS)
				mWave_FDS.DoTicks(tick,bChannelMix[23]);
		}


		if(bBurnCPUCycles)
		{
			nCPUCycle += mixL;
			fulltick += mixL;
		}

		
		// Frame Sequencer

		fTicksUntilNextFrame -= tick;
		if(fTicksUntilNextFrame <= 0)
		{
			fTicksUntilNextFrame += (bPALMode ? PAL_FRAME_COUNTER_FREQ : NTSC_FRAME_COUNTER_FREQ);
			nFrameCounter++;
			if(nFrameCounter > nFrameCounterMax)
				nFrameCounter = 0;

			if(nFrameCounterMax == 4)
			{
				if(nFrameCounter < 4)
				{
					CLOCK_MAJOR();
					if(!(nFrameCounter & 1))
						CLOCK_MINOR();
				}
			}
			else
			{
				CLOCK_MAJOR();
				if(nFrameCounter & 1)
					CLOCK_MINOR();

				if((nFrameCounter == 3) && bFrameIRQEnabled)
					bFrameIRQPending = 1;
			}
		}

		if(!pOutput)
			continue;

		fTicksUntilNextSample -= tick;
		if(fTicksUntilNextSample <= 0)
		{
			fTicksUntilNextSample += fTicksPerSample;
			if(!nDownsample) continue;
			if(nMonoStereo == 1)		//mono mixing
			{
				mixL = 0;
				mWave_Squares.Mix_Mono(mixL,nDownsample);
				mWave_TND.Mix_Mono(mixL,nDownsample);
				if(nExternalSound)
				{
					if(nExternalSound & EXTSOUND_VRC6)
					{
						mWave_VRC6Pulse[0].Mix_Mono(mixL,nDownsample);
						mWave_VRC6Pulse[1].Mix_Mono(mixL,nDownsample);
						mWave_VRC6Saw.Mix_Mono(mixL,nDownsample);
					}
					if(nExternalSound & EXTSOUND_MMC5)
					{
						mWave_MMC5Square[0].Mix_Mono(mixL,nDownsample);
						mWave_MMC5Square[1].Mix_Mono(mixL,nDownsample);
						mWave_MMC5Voice.Mix_Mono(mixL,nDownsample);
					}
					if(nExternalSound & EXTSOUND_N106)
						mWave_N106.Mix_Mono(mixL,nDownsample);
					if(nExternalSound & EXTSOUND_FME07)
					{
						mWave_FME07[0].Mix_Mono(mixL,nDownsample);
						mWave_FME07[1].Mix_Mono(mixL,nDownsample);
						mWave_FME07[2].Mix_Mono(mixL,nDownsample);
					}
					if (nExternalSound & EXTSOUND_EPSM)
					{
						mWave_EPSM[0].Mix_Mono(mixL, nDownsample);
						mWave_EPSM[1].Mix_Mono(mixL, nDownsample);
						mWave_EPSM[2].Mix_Mono(mixL, nDownsample);
					}
					if(nExternalSound & EXTSOUND_FDS)
						mWave_FDS.Mix_Mono(mixL,nDownsample);
				}

				/*	Filter	*/

				if(bPrePassEnabled)
				{
					dif = mixL - nSmPrevL;
					nSmAccL -= dif;
					nSmPrevL = mixL;
					nSmAccL -= (int)(nSmAccL * fSmDiv);
					mixL += nSmAccL;
				}

				diff = ((INT64)mixL << 25) - nFilterAccL;
				if(bHighPassEnabled)
					nFilterAccL += (diff * nHighPass) >> 16;
				if(bLowPassEnabled)
				{
					nFilterAcc2L += ((diff - nFilterAcc2L) * nLowPass) >> 16;
					mixL = (int)((nFilterAcc2L) >> (23));
				}
				else
					mixL = (int)(diff >> 23);

				/*	End Filter	*/
				
				if(bFade && (fFadeVolume < 1))
					mixL = (int)(mixL * fFadeVolume);
				
				if(mixL < -32768)	mixL = -32768;
				if(mixL >  32767)	mixL =  32767;

				*((WORD*)pOutput) = (WORD)mixL;
				pOutput += 2;
			}
			else						//stereo mixing
			{
				mixL = mixR = 0;

				mWave_Squares.Mix_Stereo(mixL,mixR,nDownsample);
				mWave_TND.Mix_Stereo(mixL,mixR,nDownsample);
				if(nExternalSound)
				{
					if(nExternalSound & EXTSOUND_VRC6)
					{
						mWave_VRC6Pulse[0].Mix_Stereo(mixL,mixR,nDownsample);
						mWave_VRC6Pulse[1].Mix_Stereo(mixL,mixR,nDownsample);
						mWave_VRC6Saw.Mix_Stereo(mixL,mixR,nDownsample);
					}
					if(nExternalSound & EXTSOUND_MMC5)
					{
						mWave_MMC5Square[0].Mix_Stereo(mixL,mixR,nDownsample);
						mWave_MMC5Square[1].Mix_Stereo(mixL,mixR,nDownsample);
						mWave_MMC5Voice.Mix_Stereo(mixL,mixR,nDownsample);
					}
					if(nExternalSound & EXTSOUND_N106)
						mWave_N106.Mix_Stereo(mixL,mixR,nDownsample);
					if(nExternalSound & EXTSOUND_FME07)
					{
						mWave_FME07[0].Mix_Stereo(mixL,mixR,nDownsample);
						mWave_FME07[1].Mix_Stereo(mixL,mixR,nDownsample);
						mWave_FME07[2].Mix_Stereo(mixL,mixR,nDownsample);
					}
					if (nExternalSound & EXTSOUND_EPSM)
					{
						mWave_EPSM[0].Mix_Stereo(mixL, mixR, nDownsample);
						mWave_EPSM[1].Mix_Stereo(mixL, mixR, nDownsample);
						mWave_EPSM[2].Mix_Stereo(mixL, mixR, nDownsample);
					}
					if(nExternalSound & EXTSOUND_FDS)
						mWave_FDS.Mix_Stereo(mixL,mixR,nDownsample);
				}
				
				/*	Filter	*/

				if(bPrePassEnabled)
				{
					dif = mixL - nSmPrevL;
					nSmAccL -= dif;
					nSmPrevL = mixL;
					nSmAccL -= (int)(nSmAccL * fSmDiv);
					mixL += nSmAccL;
					
					dif = mixR - nSmPrevR;
					nSmAccR -= dif;
					nSmPrevR = mixR;
					nSmAccR -= (int)(nSmAccR * fSmDiv);
					mixR += nSmAccR;
				}

				diff = ((INT64)mixL << 25) - nFilterAccL;
				if(bHighPassEnabled)
					nFilterAccL += (diff * nHighPass) >> 16;
				if(bLowPassEnabled)
				{
					nFilterAcc2L += ((diff - nFilterAcc2L) * nLowPass) >> 16;
					mixL = (int)((nFilterAcc2L) >> (23));
				}
				else
					mixL = (int)(diff >> 23);

				
				diff = ((INT64)mixR << 25) - nFilterAccR;
				if(bHighPassEnabled)
					nFilterAccR += (diff * nHighPass) >> 16;
				if(bLowPassEnabled)
				{
					nFilterAcc2R += ((diff - nFilterAcc2R) * nLowPass) >> 16;
					mixR = (int)((nFilterAcc2R) >> (23));
				}
				else
					mixR = (int)(diff >> 23);

				/*	End Filter	*/
								
				if(bFade && (fFadeVolume < 1))
				{
					mixL = (int)(mixL * fFadeVolume);
					mixR = (int)(mixR * fFadeVolume);
				}
				
				if(mixL < -32768)	mixL = -32768;
				if(mixL >  32767)	mixL =  32767;
				if(mixR < -32768)	mixR = -32768;
				if(mixR >  32767)	mixR =  32767;

				*((UINT*)pOutput) = (UINT)((mixL & 0x0000FFFF) | (mixR << 16));
				pOutput += 4;
			}
			nDownsample = 0;
		}
	}

	nAPUCycle = nCPUCycle;
}


/*
 *	CNSF Contructor
 */

CNSFCore::CNSFCore()
{
	int i;
	ZeroMemory(this,sizeof(CNSFCore));

	// Default filter bases
	nHighPassBase = 150;
	nLowPassBase = 27000;
	fSmDiv = 0.75f;

	bLowPassEnabled = 0;
	bPrePassEnabled = 0;
	bHighPassEnabled = 0;

	mWave_TND.nNoiseRandomShift =	1;
	for(i = 0; i < 8; i++)
		mWave_TND.pDMCDMAPtr[i] = pROM[i + 2];


	nSampleRate = 44100;		//default
	nMonoStereo = 2;			//default;
	fMasterVolume = 1.0f;

	for(i = 0; i < 24; i++)
		bChannelMix[i] = 1;

	for(i = 0; i < 29; i++)
	{
		nChannelVol[i] = 255;
		nChannelPan[i] = 0;
	}
	nChannelPan[0] = -45;
	nChannelPan[1] = 45;
	nChannelPan[5] = -50;
	nChannelPan[6] = 50;
	nChannelPan[8] = -50;
	nChannelPan[9] = 50;

	SetPlaybackOptions(nSampleRate,nMonoStereo);

	bDMCPopReducer = 0;

	for(i = 0; i < 8; i++)
		mWave_N106.fFrequencyLookupTable[i] = (((i + 1) * 45 * 0x40000) / (float)NES_FREQUENCY) * (bPALMode ? (float)PAL_FREQUENCY : (float)NTSC_FREQUENCY);

	
	mWave_Squares.nInvertFreqCutoff = 0xFFFF;
	mWave_TND.nInvertFreqCutoff_Tri = 0xFFFF;
	mWave_TND.nInvertFreqCutoff_Noise = 0xFFFF;
	mWave_VRC6Pulse[0].nInvertFreqCutoff = 0xFFFF;
	mWave_VRC6Pulse[1].nInvertFreqCutoff = 0xFFFF;
	mWave_VRC6Saw.nInvertFreqCutoff = 0xFFFF;
	mWave_MMC5Square[0].nInvertFreqCutoff = 0xFFFF;
	mWave_MMC5Square[1].nInvertFreqCutoff = 0xFFFF;
	ZeroMemory(mWave_N106.nInvertFreqCutoff,4*8*8);
	mWave_FME07[0].nInvertFreqCutoff = 0xFFFF;
	mWave_FME07[1].nInvertFreqCutoff = 0xFFFF;
	mWave_FME07[2].nInvertFreqCutoff = 0xFFFF;
	mWave_EPSM[0].nInvertFreqCutoff = 0xFFFF;
	mWave_EPSM[1].nInvertFreqCutoff = 0xFFFF;
	mWave_EPSM[2].nInvertFreqCutoff = 0xFFFF;
}

/*
 *	Initialize
 *
 *		Initializes Memory
 */

int CNSFCore::Initialize()
{
	if(bMemoryOK)		return 1;

	pRAM = new BYTE[0x800];
	pSRAM = new BYTE[0x2000];
	pExRAM = new BYTE[0x1000];
	mWave_TND.nOutputTable_L = new short[0x8000];
	mWave_TND.nOutputTable_R = new short[4 * 0x8000];

	if(pRAM && pSRAM && pExRAM && mWave_TND.nOutputTable_L && mWave_TND.nOutputTable_R)
	{
		ZeroMemory(pRAM,0x800);
		ZeroMemory(pSRAM,0x2000);
		ZeroMemory(pExRAM,0x1000);
		ZeroMemory(mWave_TND.nOutputTable_L,0x10000);
		ZeroMemory(mWave_TND.nOutputTable_R,0x10000);
		pStack = pRAM + 0x100;
		bMemoryOK = 1;
		return 1;
	}

	bMemoryOK = 0;
	SAFE_DELETE(pRAM);
	SAFE_DELETE(pSRAM);
	SAFE_DELETE(pExRAM);
	SAFE_DELETE(mWave_TND.nOutputTable_L);
	SAFE_DELETE(mWave_TND.nOutputTable_R);
	pStack = NULL;
	return 0;
}

/*
 *	Destroy
 */

void CNSFCore::Destroy()
{
	WaitForSamples();

	SAFE_DELETE(pRAM);
	SAFE_DELETE(pSRAM);
	SAFE_DELETE(pExRAM);
	SAFE_DELETE(pROM_Full);
	SAFE_DELETE(mWave_TND.nOutputTable_L);
	SAFE_DELETE(mWave_TND.nOutputTable_R);

	pStack = NULL;
	ZeroMemory(pROM,sizeof(BYTE*) * 10);
	nROMMaxSize = 0;
	nROMBankCount = 0;
	nROMSize = 0;
	bMemoryOK = 0;
	bFileLoaded = 0;
	bTrackSelected = 0;

	VRC7_Destroy();
}

/*
 *	LoadNSF
 */

int CNSFCore::LoadNSF(const CNSFFile* fl)
{
	WaitForSamples();
	if(!bMemoryOK)	return 0;

	if(!fl)								return 0;
	if(fl->nDataBufferSize < 1)			return 0;
	if(!fl->pDataBuffer)				return 0;

	int i;

	bFileLoaded = 0;
	bTrackSelected = 0;
	nExternalSound = fl->nChipExtensions;
	if(fl->nIsPal & 2)
		bPALMode = bPALPreference;
	else
		bPALMode = fl->nIsPal & 1;

	SetPlaybackOptions(nSampleRate,nMonoStereo);
	
	int neededsize = fl->nDataBufferSize + (fl->nLoadAddress & 0x0FFF);
	if(neededsize & 0x0FFF)		neededsize += 0x1000 - (neededsize & 0x0FFF);
	if(neededsize < 0x1000)		neededsize = 0x1000;

	BYTE specialload = 0;
	
	for(i = 0; (i < 8) && (!fl->nBankswitch[i]); i++);
	if(i < 8)		//uses bankswitching
	{
		memcpy(&nBankswitchInitValues[2],fl->nBankswitch,8);
		nBankswitchInitValues[0] = fl->nBankswitch[6];
		nBankswitchInitValues[1] = fl->nBankswitch[7];
		if(nExternalSound & EXTSOUND_FDS)
		{
			if(!(nBankswitchInitValues[0] || nBankswitchInitValues[1]))
			{
				//FDS sound with '00' specified for both $6000 and $7000 banks.
				// point this to an area of fresh RAM (sort of hackish solution
				// for those FDS tunes that don't quite follow the nsf specs.
				nBankswitchInitValues[0] = (BYTE)(neededsize >> 12);
				nBankswitchInitValues[1] = (BYTE)(neededsize >> 12) + 1;
				neededsize += 0x2000;
			}
		}
	}
	else			//doesn't
	{
		if(nExternalSound & EXTSOUND_FDS)
		{
			// bad load address
			if(fl->nLoadAddress < 0x6000)		return 0;

			if(neededsize < 0xA000)
				neededsize = 0xA000;
			specialload = 1;
			for(i = 0; i < 10; i++)
				nBankswitchInitValues[i] = (BYTE)i;
		}
		else
		{
			// bad load address
			if(fl->nLoadAddress < 0x8000)		return 0;

			int j = (fl->nLoadAddress >> 12) - 6;
			for(i = 0; i < j; i++)
				nBankswitchInitValues[i] = 0;
			for(j = 0; i < 10; i++, j++)
				nBankswitchInitValues[i] = (BYTE)j;
		}
	}

	if(neededsize > nROMMaxSize)
	{
		SAFE_DELETE(pROM_Full);
		pROM_Full = new BYTE[neededsize];
		if(!pROM_Full)
		{
			nROMMaxSize = 0;
			nROMSize = 0;
			return 0;
		}
		nROMMaxSize = neededsize;
	}

	nROMSize = neededsize;
	nROMBankCount = neededsize >> 12;

	ZeroMemory(pROM_Full,nROMMaxSize);
	if(specialload)
		memcpy(pROM_Full + (fl->nLoadAddress - 0x6000),fl->pDataBuffer,fl->nDataBufferSize);
	else
		memcpy(pROM_Full + (fl->nLoadAddress & 0x0FFF),fl->pDataBuffer,fl->nDataBufferSize);

	ZeroMemory(pRAM,0x0800);
	ZeroMemory(pExRAM,0x1000);
	ZeroMemory(pSRAM,0x2000);

	nExternalSound = fl->nChipExtensions;
	fNSFPlaybackSpeed = (bPALMode ? PAL_NMIRATE : NTSC_NMIRATE);
	
	bFileLoaded = 1;

	SetPlaybackSpeed(0);

	nPlayAddress = fl->nPlayAddress;
	nInitAddress = fl->nInitAddress;

	pExRAM[0x00] = 0x20;						//JSR
	memcpy(&pExRAM[0x01],&nInitAddress,2);		//Init Address
	pExRAM[0x03] = 0xF2;						//JAM
	pExRAM[0x04] = 0x20;						//JSR
	memcpy(&pExRAM[0x05],&nPlayAddress,2);		//Play Address
	pExRAM[0x07] = 0x4C;						//JMP
	pExRAM[0x08] = 0x03;						//$5003  (JAM right before the JSR to play address)
	pExRAM[0x09] = 0x50;

	regA = regX = regY = 0;
	regP = 0x04;			//I_FLAG
	regSP = 0xFF;

	nFilterAccL = nFilterAccR = nFilterAcc2L = nFilterAcc2R = 0;


	/*	Reset Read/Write Procs			*/
	
	ReadMemory[0] = ReadMemory[1] = &CNSFCore::ReadMemory_RAM;
	ReadMemory[2] = ReadMemory[3] = &CNSFCore::ReadMemory_Default;
	ReadMemory[4] =					&CNSFCore::ReadMemory_pAPU;
	ReadMemory[5] =					&CNSFCore::ReadMemory_ExRAM;
	ReadMemory[6] = ReadMemory[7] = &CNSFCore::ReadMemory_SRAM;

	WriteMemory[0] = WriteMemory[1] =	&CNSFCore::WriteMemory_RAM;
	WriteMemory[2] = WriteMemory[3] =	&CNSFCore::WriteMemory_Default;
	WriteMemory[4] =					&CNSFCore::WriteMemory_pAPU;
	WriteMemory[5] =					&CNSFCore::WriteMemory_ExRAM;
	WriteMemory[6] = WriteMemory[7] =	&CNSFCore::WriteMemory_SRAM;

	for(i = 8; i < 16; i++)
	{
		ReadMemory[i] = &CNSFCore::ReadMemory_ROM;
		WriteMemory[i] = &CNSFCore::WriteMemory_Default;
	}

	if(nExternalSound & EXTSOUND_FDS)
	{
		WriteMemory[0x06] = &CNSFCore::WriteMemory_FDSRAM;
		WriteMemory[0x07] = &CNSFCore::WriteMemory_FDSRAM;
		WriteMemory[0x08] = &CNSFCore::WriteMemory_FDSRAM;
		WriteMemory[0x09] = &CNSFCore::WriteMemory_FDSRAM;
		WriteMemory[0x0A] = &CNSFCore::WriteMemory_FDSRAM;
		WriteMemory[0x0B] = &CNSFCore::WriteMemory_FDSRAM;
		WriteMemory[0x0C] = &CNSFCore::WriteMemory_FDSRAM;
		WriteMemory[0x0D] = &CNSFCore::WriteMemory_FDSRAM;
		ReadMemory[0x06] = &CNSFCore::ReadMemory_ROM;
		ReadMemory[0x07] = &CNSFCore::ReadMemory_ROM;
	}

	if(nExternalSound & EXTSOUND_VRC7)
	{
		WriteMemory[9] = &CNSFCore::WriteMemory_VRC7;
		VRC7_Init();
	}
	if(nExternalSound & EXTSOUND_VRC6)
	{
		WriteMemory[0x09] = &CNSFCore::WriteMemory_VRC6;	//if both VRC6+VRC7... it MUST go to WriteMemory_VRC6 
		WriteMemory[0x0A] = &CNSFCore::WriteMemory_VRC6;	// or register writes will be lost (WriteMemory_VRC6 calls
		WriteMemory[0x0B] = &CNSFCore::WriteMemory_VRC6;	// WriteMemory_VRC7 if needed)
	}
	if(nExternalSound & EXTSOUND_N106)
	{
		WriteMemory[0x04] = &CNSFCore::WriteMemory_N106;
		ReadMemory[0x04] = &CNSFCore::ReadMemory_N106;
		WriteMemory[0x0F] = &CNSFCore::WriteMemory_N106;
	}
	if(nExternalSound & EXTSOUND_FME07)
	{
		WriteMemory[0x0C] = &CNSFCore::WriteMemory_FME07;
		WriteMemory[0x0E] = &CNSFCore::WriteMemory_FME07;

		// We use F000 in multi-expansion NSF export, if N163 is not active, hook the address, 
		// otherwise will be handled in "WriteMemory_N106"
		if (!(nExternalSound & EXTSOUND_N106))
			WriteMemory[0x0F] = &CNSFCore::WriteMemory_FME07; 
	}
	//if (nExternalSound & EXTSOUND_EPSM)
	//{
	//	WriteMemory[0x07] = &CNSFCore::WriteMemory_EPSM;
	//	WriteMemory[0x08] = &CNSFCore::WriteMemory_EPSM;
	//	WriteMemory[0x09] = &CNSFCore::WriteMemory_EPSM;
	//	WriteMemory[0x0a] = &CNSFCore::WriteMemory_EPSM;
	//	WriteMemory[0x0b] = &CNSFCore::WriteMemory_EPSM;
	//	WriteMemory[0x0c] = &CNSFCore::WriteMemory_EPSM;
	//	WriteMemory[0x0d] = &CNSFCore::WriteMemory_EPSM;
	//	WriteMemory[0x0E] = &CNSFCore::WriteMemory_EPSM;
	//	WriteMemory[0x0f] = &CNSFCore::WriteMemory_EPSM;
	//}
	
	if(nExternalSound & EXTSOUND_MMC5)			//MMC5 still has a multiplication reg that needs to be available on PAL tunes
		WriteMemory[0x05] = &CNSFCore::WriteMemory_MMC5;

	return 1;
}

/*
 *	SetTrack
 */

void CNSFCore::SetTrack(BYTE track)
{
	WaitForSamples();
	if(!bFileLoaded)		return;

	bTrackSelected = 1;
	nCurTrack = track;

	regPC = 0x5000;
	regA = track;
	regX = bPALMode;
	regY = bCleanAXY ? 0 : 0xCD;
	regSP = 0xFF;
	if(bCleanAXY)
		regP = 0x04;
	bCPUJammed = 0;

	nCPUCycle = nAPUCycle = 0;
	nDMCPop_Prev = 0;
	bDMCPop_Skip = 0;

	for(int i = 0x4000; i < 0x400F; i++)
		WriteMemory_pAPU(i,0);
	WriteMemory_pAPU(0x4010,0);
	WriteMemory_pAPU(0x4012,0);
	WriteMemory_pAPU(0x4013,0);
	WriteMemory_pAPU(0x4014,0);
	WriteMemory_pAPU(0x4015,0);
	WriteMemory_pAPU(0x4015,0x0F);
	WriteMemory_pAPU(0x4017,0);

	for(int i = 0; i < 10; i++)
		WriteMemory_ExRAM(0x5FF6 + i,nBankswitchInitValues[i]);

	ZeroMemory(pRAM,0x0800);
	ZeroMemory(pSRAM,0x2000);
	ZeroMemory(&pExRAM[0x10],0x0FF0);
	bFade = 0;


	fTicksUntilNextSample = fTicksPerSample;
	fTicksUntilNextFrame = (bPALMode ? PAL_FRAME_COUNTER_FREQ : NTSC_FRAME_COUNTER_FREQ);
	fTicksUntilNextPlay = fTicksPerPlay;
	nTotalPlays = 0;
	
	/*	Clear mixing vals	*/
	mWave_Squares.nMixL = mWave_Squares.nMixR = 0;
	mWave_TND.nMixL = mWave_TND.nMixR = 0;
	mWave_VRC6Pulse[0].nMixL = mWave_VRC6Pulse[0].nMixR = 0;
	mWave_VRC6Pulse[1].nMixL = mWave_VRC6Pulse[1].nMixR = 0;
	mWave_VRC6Saw.nMixL = mWave_VRC6Saw.nMixR = 0;
	mWave_MMC5Square[0].nMixL = mWave_MMC5Square[0].nMixR = 0;
	mWave_MMC5Square[1].nMixL = mWave_MMC5Square[1].nMixR = 0;
	mWave_MMC5Voice.nMixL = mWave_MMC5Voice.nMixR = 0;


	/*	Reset Tri/Noise/DMC	*/
	mWave_TND.nTriStep = mWave_TND.nTriOutput = 0;
	mWave_TND.nDMCOutput = 0;
	mWave_TND.bNoiseRandomOut = 0;
	mWave_Squares.nDutyCount[0] = mWave_Squares.nDutyCount[1] = 0;
	mWave_TND.bDMCActive = 0;
	mWave_TND.nDMCBytesRemaining = 0;
	mWave_TND.bDMCSampleBufferEmpty = 1;
	mWave_TND.bDMCDeltaSilent = 1;

	/*	Reset VRC6	*/
	mWave_VRC6Pulse[0].nVolume = 0;
	mWave_VRC6Pulse[1].nVolume = 0;
	mWave_VRC6Saw.nAccumRate = 0;

	/*	Reset MMC5	*/
	mWave_MMC5Square[0].nVolume = 0;
	mWave_MMC5Square[1].nVolume = 0;
	mWave_MMC5Voice.nOutput = 0;

	/*	Reset N106	*/
	ZeroMemory(mWave_N106.nRAM,0x100);
	ZeroMemory(mWave_N106.nVolume,8);
	ZeroMemory(mWave_N106.nOutput,8);
	ZeroMemory(mWave_N106.nMixL,32);
	ZeroMemory(mWave_N106.nMixR,32);

	/*	Reset FME-07	*/
	mWave_FME07[0].nVolume = 0;
	mWave_FME07[1].nVolume = 0;
	mWave_FME07[2].nVolume = 0;

	/*	Reset EPSM	*/
	mWave_EPSM[0].nVolume = 0;
	mWave_EPSM[1].nVolume = 0;
	mWave_EPSM[2].nVolume = 0;
	mWave_EPSM[3].nVolume = 15;
	mWave_EPSM[4].nVolume = 15;
	mWave_EPSM[5].nVolume = 15;
	mWave_EPSM[6].nVolume = 15;
	mWave_EPSM[7].nVolume = 15;
	mWave_EPSM[8].nVolume = 15;

	/*	Clear FDS crap		*/
	mWave_FDS.bEnvelopeEnable = 0;
	mWave_FDS.nEnvelopeSpeed = 0xFF;
	mWave_FDS.nVolEnv_Mode = 2;
	mWave_FDS.nVolEnv_Decay = 0;
	mWave_FDS.nVolEnv_Gain = 0;
	mWave_FDS.nVolume = 0;
	mWave_FDS.bVolEnv_On = 0;
	mWave_FDS.nSweep_Mode = 2;
	mWave_FDS.nSweep_Decay = 0;
	mWave_FDS.nSweep_Gain = 0;
	mWave_FDS.bSweepEnv_On = 0;
	mWave_FDS.nSweepBias = 0;
	mWave_FDS.bLFO_Enabled = 0;
	mWave_FDS.nLFO_Freq.W = 0;
	mWave_FDS.fLFO_Timer = 0;
	mWave_FDS.fLFO_Count = 0;
	mWave_FDS.nLFO_Addr = 0;
	mWave_FDS.bLFO_On = 0;
	mWave_FDS.nMainVolume = 0;
	mWave_FDS.bEnabled = 0;
	mWave_FDS.nFreq.W = 0;
	mWave_FDS.fFreqCount = 0;
	mWave_FDS.nMainAddr = 0;
	mWave_FDS.bWaveWrite = 0;
	mWave_FDS.bMain_On = 0;
	mWave_FDS.nMixL = mWave_FDS.nMixR = 0;
	ZeroMemory(mWave_FDS.nWaveTable,0x40);
	ZeroMemory(mWave_FDS.nLFO_Table,0x40);

	mWave_FDS.nSweep_Count = mWave_FDS.nSweep_Timer = ((mWave_FDS.nSweep_Decay + 1) * mWave_FDS.nEnvelopeSpeed * 8);
	mWave_FDS.nVolEnv_Count = mWave_FDS.nVolEnv_Timer = ((mWave_FDS.nVolEnv_Decay + 1) * mWave_FDS.nEnvelopeSpeed * 8);

	nSilentSamples = 0;
	nDownsample = 0;
	if(nExternalSound & EXTSOUND_VRC7)
		VRC7_Reset();

	nFilterAccL = 0;
	nFilterAccR = 0;
	nFilterAcc2L = 0;
	nFilterAcc2R = 0;
	nSmPrevL = 0;
	nSmAccL = 0;
	nSmPrevR = 0;
	nSmAccR = 0;
}

/*
 *	SetPlaybackOptions
 */

int CNSFCore::SetPlaybackOptions(int samplerate,int channels)
{
	WaitForSamples();
	if(samplerate < 2000)					return 0;
	if(samplerate > 96000)					return 0;
	if((channels != 1) && (channels != 2))	return 0;

	nSampleRate = samplerate;
	nMonoStereo = channels;
    
	fTicksPerSample = (bPALMode ? PAL_FREQUENCY : NTSC_FREQUENCY) / samplerate;
	fTicksUntilNextSample = fTicksPerSample;
	RebuildOutputTables((UINT)(-1));

	RecalcFilter();
	RecalcSilenceTracker();

	if(bFileLoaded && (nExternalSound & EXTSOUND_VRC7))
		VRC7_Init();

	return 1;
}

/*
 *	SetPlaybackSpeed
 */

void CNSFCore::SetPlaybackSpeed(float playspersec)
{
	WaitForSamples();
	if(playspersec < 1)
	{
		if(!bFileLoaded)	return;
		playspersec = fNSFPlaybackSpeed;
	}

	fTicksPerPlay = fTicksUntilNextPlay = (bPALMode ? PAL_FREQUENCY : NTSC_FREQUENCY) / playspersec;
}

/*
 *	SetMasterVolume
 */

void CNSFCore::SetMasterVolume(float vol)
{
	WaitForSamples();
	fMasterVolume = vol;
	RebuildOutputTables((UINT)(-1));
}

/*
 *	SetChannelOptions
 */

void CNSFCore::SetChannelOptions(UINT chan,int mix,int vol,int pan,int inv)
{
	WaitForSamples();
	BYTE bRebuild = 0;

	if(chan >= 29)			return;

	if((mix == 0) || (mix == 1))
	{
		switch(chan)
		{
		case 0:	mWave_Squares.bChannelMix[0] = mix; break;
		case 1:	mWave_Squares.bChannelMix[1] = mix; break;
		case 2:	mWave_TND.bTriChannelMix = mix; break;
		case 3:	mWave_TND.bNoiseChannelMix = mix; break;
		case 4:	mWave_TND.bDMCChannelMix = mix; break;
		default: bChannelMix[chan - 5] = mix; break;
		}
	}

	if((vol >= 0) && (vol <= 255)){		nChannelVol[chan] = vol; bRebuild = 1; }
	if((pan >= -127) && (pan <= 127)){	nChannelPan[chan] = pan; if(nMonoStereo == 2) bRebuild = 1; }
	if((inv >= 0) && (inv <= 1)){
		if(chan < 2)
		{
			if(inv)	mWave_Squares.bInvert |=  (1 << chan);
			else	mWave_Squares.bInvert &= ~(1 << chan);
		}
		else if(chan < 5)
		{
			if(inv)	mWave_TND.bInvert |=  (1 << (chan - 2));
			else	mWave_TND.bInvert &= ~(1 << (chan - 2));
		}
		else
		{
			switch(chan)
			{
			case 5:	mWave_VRC6Pulse[0].bInvert = (BYTE)inv; break;
			case 6:	mWave_VRC6Pulse[1].bInvert = (BYTE)inv; break;
			case 7:	mWave_VRC6Saw.bInvert = (BYTE)inv; break;
			case 8: mWave_MMC5Square[0].bInvert = (BYTE)inv; break;
			case 9: mWave_MMC5Square[1].bInvert = (BYTE)inv; break;
			case 10: mWave_MMC5Voice.bInvert = (BYTE)inv; break;
			case 11: case 12: case 13: case 14: case 15: case 16: case 17: case 18:
				mWave_N106.bInvert[chan - 11] = (BYTE)inv; break;
			case 19: case 20: case 21: case 22: case 23: case 24:
				bVRC7Inv[chan - 19] = inv;
				VRC7_ChangeInversion(chan - 19,inv);
				break;
			case 25: mWave_FME07[0].bInvert = (BYTE)inv; break;
			case 26: mWave_FME07[1].bInvert = (BYTE)inv; break;
			case 27: mWave_FME07[2].bInvert = (BYTE)inv; break;
			case 28: mWave_FDS.bInvert = (BYTE)inv; break;
			case 29: mWave_EPSM[0].bInvert = (BYTE)inv; break;
			case 30: mWave_EPSM[1].bInvert = (BYTE)inv; break;
			case 31: mWave_EPSM[2].bInvert = (BYTE)inv; break;
			}
		}
	}

	if(bRebuild)
		RebuildOutputTables(1 << chan);
}

/*
 *	SetAdvancedOptions
 */

void CNSFCore::SetAdvancedOptions(const NSF_ADVANCEDOPTIONS* opt)
{
	WaitForSamples();
	if(!opt)	return;

	mWave_FDS.bPopReducer = opt->bFDSPopReducer;
	bDMCPopReducer = opt->bDMCPopReducer;
	nForce4017Write = opt->nForce4017Write;
	bN106PopReducer = opt->bN106PopReducer;
	nSilenceTrackMS = opt->nSilenceTrackMS;
	bNoSilenceIfTime = opt->bNoSilenceIfTime;
	RecalcSilenceTracker();
	
	bIgnore4011Writes = opt->bIgnore4011Writes;
	bIgnoreBRK = opt->bIgnoreBRK;
	bIgnoreIllegalOps = opt->bIgnoreIllegalOps;
	bNoWaitForReturn = opt->bNoWaitForReturn;
	bPALPreference = opt->bPALPreference;
	bCleanAXY = opt->bCleanAXY;
	bResetDuty = opt->bResetDuty;

	bHighPassEnabled = opt->bHighPassEnabled;
	bLowPassEnabled = opt->bLowPassEnabled;
	bPrePassEnabled = opt->bPrePassEnabled;

	nHighPassBase = opt->nHighPassBase;
	nLowPassBase = opt->nLowPassBase;
	nPrePassBase = opt->nPrePassBase;
	if(nHighPassBase < 50)				nHighPassBase = 50;
	if(nHighPassBase > 5000)			nHighPassBase = 5000;
	if(nLowPassBase < 8000)				nLowPassBase = 8000;
	if(nLowPassBase > 60000)			nLowPassBase = 60000;
	if(nPrePassBase < 0)				nPrePassBase = 0;
	if(nPrePassBase > 1000)				nPrePassBase = 1000;
	RecalcFilter();

	/*
	 *	Frequency Cutoff
	 */

	if(nInvertCutoffHz != opt->nInvertCutoffHz)
		RecalculateInvertFreqs(opt->nInvertCutoffHz);
}

void CNSFCore::RecalculateInvertFreqs(int cutoff)
{
	nInvertCutoffHz = cutoff;
	if(nInvertCutoffHz > 0)
	{
		//all calculations are approximate.. no need for precision here
		float base = (bPALMode ? PAL_FREQUENCY : NTSC_FREQUENCY);

		//square frequency
		//  Hz = Base / ((Freq + 1) * 16)
		//  Freq = (Base / (16 * Hz)) - 1
		mWave_Squares.nInvertFreqCutoff = (WORD)(base / (16 * nInvertCutoffHz)) - 1;

		//tri frequency
		//  Same formula, but 32 steps instead of 16
		mWave_TND.nInvertFreqCutoff_Tri = (WORD)(base / (32 * nInvertCutoffHz)) - 1;

		//noise frequency
		//  this is harder since it's a random wavelength and thus no real frequency
		//  use same freq as Squares
		mWave_TND.nInvertFreqCutoff_Noise = mWave_Squares.nInvertFreqCutoff;
		
		//VRC6 Pulses = same frequency as squares
		mWave_VRC6Pulse[0].nInvertFreqCutoff = mWave_VRC6Pulse[1].nInvertFreqCutoff = 
			mWave_Squares.nInvertFreqCutoff;

		//VRC6 Saw = same formula, but 14 steps instead of 16
		mWave_VRC6Saw.nInvertFreqCutoff = (WORD)(base / (14 * nInvertCutoffHz)) - 1;

		//MMC5 Squares = same as normal squares
		mWave_MMC5Square[0].nInvertFreqCutoff = mWave_MMC5Square[1].nInvertFreqCutoff = 
			mWave_Squares.nInvertFreqCutoff;

		//N106 Frequencies are a nightmare
		int i, j;
		for(i = 0; i < 8; i++)
		{
			for(j = 0; j < 8; j++)
				mWave_N106.nInvertFreqCutoff[i][j] =
					((0x40000 * 4 * 45) / NES_FREQUENCY) * (i + 1) * (8 - j) * nInvertCutoffHz;
		}

		//FME-07 uses the normal formula, but is 32 steps instead of 16 (approx. same as tri)
		mWave_FME07[0].nInvertFreqCutoff = mWave_FME07[1].nInvertFreqCutoff = 
			mWave_FME07[2].nInvertFreqCutoff = mWave_TND.nInvertFreqCutoff_Tri;
		mWave_EPSM[0].nInvertFreqCutoff = mWave_EPSM[1].nInvertFreqCutoff =
			mWave_EPSM[2].nInvertFreqCutoff = mWave_TND.nInvertFreqCutoff_Tri;
		
	}
	else
	{
		mWave_Squares.nInvertFreqCutoff = 0xFFFF;
		mWave_TND.nInvertFreqCutoff_Tri = 0xFFFF;
		mWave_TND.nInvertFreqCutoff_Noise = 0xFFFF;
		mWave_VRC6Pulse[0].nInvertFreqCutoff = 0xFFFF;
		mWave_VRC6Pulse[1].nInvertFreqCutoff = 0xFFFF;
		mWave_VRC6Saw.nInvertFreqCutoff = 0xFFFF;
		mWave_MMC5Square[0].nInvertFreqCutoff = 0xFFFF;
		mWave_MMC5Square[1].nInvertFreqCutoff = 0xFFFF;
		ZeroMemory(mWave_N106.nInvertFreqCutoff,4*8*8);
		mWave_FME07[0].nInvertFreqCutoff = 0xFFFF;
		mWave_FME07[1].nInvertFreqCutoff = 0xFFFF;
		mWave_FME07[2].nInvertFreqCutoff = 0xFFFF;
		mWave_EPSM[0].nInvertFreqCutoff = 0xFFFF;
		mWave_EPSM[1].nInvertFreqCutoff = 0xFFFF;
		mWave_EPSM[2].nInvertFreqCutoff = 0xFFFF;
	}
	VRC7_ChangeInversionFreq();
}

/*
*	GetPlaybackSpeed
*/

float CNSFCore::GetPlaybackSpeed()
{
	if(fTicksPerPlay <= 0)	return 0;
	return ((bPALMode ? PAL_FREQUENCY : NTSC_FREQUENCY) / fTicksPerPlay);
}

/*
*	GetMasterVolume
*/

float CNSFCore::GetMasterVolume()
{
	return fMasterVolume;
}

/*
 *	GetAdvancedOptions
 */

void CNSFCore::GetAdvancedOptions(NSF_ADVANCEDOPTIONS* opt)
{
	if(!opt)	return;
	opt->bDMCPopReducer = bDMCPopReducer;
	opt->nForce4017Write = nForce4017Write;
	opt->bN106PopReducer = bN106PopReducer;
	opt->bFDSPopReducer = mWave_FDS.bPopReducer;
	opt->nSilenceTrackMS = nSilenceTrackMS;
	opt->bNoSilenceIfTime = bNoSilenceIfTime;
	opt->nInvertCutoffHz = nInvertCutoffHz;
	opt->bIgnore4011Writes = bIgnore4011Writes;
	opt->bIgnoreBRK = bIgnoreBRK;
	opt->bIgnoreIllegalOps = bIgnoreIllegalOps;
	opt->bNoWaitForReturn = bNoWaitForReturn;
	opt->bPALPreference = bPALPreference;
	opt->bCleanAXY = bCleanAXY;
	opt->nHighPassBase = nHighPassBase;
	opt->nLowPassBase = nLowPassBase;
	opt->nPrePassBase = nPrePassBase;
	opt->bHighPassEnabled = bHighPassEnabled;
	opt->bLowPassEnabled = bLowPassEnabled;
	opt->bPrePassEnabled = bPrePassEnabled;
	opt->bResetDuty = bResetDuty;
}

/*
 *	RecalcFilter
 */

void CNSFCore::RecalcFilter()
{
	if(!nSampleRate) return;

	nHighPass = ((INT64)nHighPassBase << 16) / nSampleRate;
	nLowPass = ((INT64)nLowPassBase << 16) / nSampleRate;

	if(nHighPass > (1<<16)) nHighPass = 1<<16;
	if(nLowPass > (1<<16)) nLowPass = 1<<16;

	fSmDiv = (100 - nPrePassBase) / 100.0f;
}

/*
 *	RecalcSilenceTracker
 */

void CNSFCore::RecalcSilenceTracker()
{
	if(nSilenceTrackMS <= 0 || !nSampleRate || (bNoSilenceIfTime && bTimeNotDefault))
	{
		nSilentSampleMax = 0;
		return;
	}

	nSilentSampleMax = nSilenceTrackMS * nSampleRate / 500;
	if(nMonoStereo == 1)
		nSilentSampleMax /= 2;
}

/*
 *	CalculateChannelVolume
 */

void CNSFCore::CalculateChannelVolume(int maxvol,int& left, int& right, BYTE vol, char pan)
{
	left = right = (int)(maxvol * vol * fMasterVolume);

	if(nMonoStereo == 2)
	{
		if(pan < 0)		right = (right * (127 + pan)) / 127;
		if(pan > 0)		left =  (left  * (127 - pan)) / 127;
	}

	left /= 255;
	right /= 255;

	if(left > 32767)		left = 32767;
	if(right > 32767)		right = 32767;
	if(left < -32768)		left = -32768;
	if(right < -32768)		right = -32768;
}


/*
 *	RebuildOutputTables
 */

void CNSFCore::RebuildOutputTables(UINT chans)
{
	int i,j;
	float l[3];
	float r[3];
	int v;
	int temp;
	int tl, tr;
	float ftemp;

	if(chans & 0x00000003)		//squares
	{
		for(i = 0; i < 2; i++)
		{
			l[i] = r[i] = nChannelVol[i];
			if(nMonoStereo == 2)
			{
				if(nChannelPan[i] < 0)		r[i] = (r[i] * (127 + nChannelPan[i])) / 127.0f;
				if(nChannelPan[i] > 0)		l[i] = (l[i] * (127 - nChannelPan[i])) / 127.0f;
			}
		}

		v = (int)(1438200 * fMasterVolume);

		for(i = 0; i < 0x100; i++)
		{
			temp = (int)(l[0] * (i >> 4));
			temp += (int)(l[1] * (i & 0x0F));

			if(!temp)
				mWave_Squares.nOutputTable_L[i] = 0;
			else
				mWave_Squares.nOutputTable_L[i] = v / ((2072640 / temp) + 100);
			
			temp = (int)(r[0] * (i >> 4));
			temp += (int)(r[1] * (i & 0x0F));
			if(!temp)			mWave_Squares.nOutputTable_R[0][i] = 0;
			else if(temp < 0)	mWave_Squares.nOutputTable_R[0][i] = v / ((2072640 / temp) - 100);
			else				mWave_Squares.nOutputTable_R[0][i] = v / ((2072640 / temp) + 100);
			
			temp = (int)(r[1] * (i & 0x0F));
			temp -= (int)(r[0] * (i >> 4));
			if(!temp)			mWave_Squares.nOutputTable_R[1][i] = 0;
			else if(temp < 0)	mWave_Squares.nOutputTable_R[1][i] = v / ((2072640 / temp) - 100);
			else				mWave_Squares.nOutputTable_R[1][i] = v / ((2072640 / temp) + 100);
			
			temp = (int)(r[0] * (i >> 4));
			temp -= (int)(r[1] * (i & 0x0F));
			if(!temp)			mWave_Squares.nOutputTable_R[2][i] = 0;
			else if(temp < 0)	mWave_Squares.nOutputTable_R[2][i] = v / ((2072640 / temp) - 100);
			else				mWave_Squares.nOutputTable_R[2][i] = v / ((2072640 / temp) + 100);
		}
	}
	if(chans & 0x0000001C)		//Tri/Noise/DMC
	{
		if(mWave_TND.nOutputTable_L && mWave_TND.nOutputTable_R)
		{
			for(i = 0; i < 3; i++)
			{
				l[i] = r[i] = nChannelVol[i + 2];
				if(nMonoStereo == 2)
				{
					if(nChannelPan[i + 2] < 0)		r[i] = (r[i] * (127 + nChannelPan[i + 2])) / 127.0f;
					if(nChannelPan[i + 2] > 0)		l[i] = (l[i] * (127 - nChannelPan[i + 2])) / 127.0f;
				}
			}

			v = (int)(2396850 * fMasterVolume);
			for(i = 0; i < 0x8000; i++)
			{
				ftemp = (l[0] * (i >> 11)) / 2097885;
				ftemp += (l[1] * ((i >> 7) & 0x0F)) / 3121455;
				ftemp += (l[2] * (i & 0x7F)) / 5772690;

				if(!ftemp)
					mWave_TND.nOutputTable_L[i] = 0;
				else
					mWave_TND.nOutputTable_L[i] = (short)(v / ((1.0f / ftemp) + 100));
				
				ftemp = (r[0] * (i >> 11)) / 2097885;
				ftemp += (r[1] * ((i >> 7) & 0x0F)) / 3121455;
				ftemp += (r[2] * (i & 0x7F)) / 5772690;
				if(!ftemp)			mWave_TND.nOutputTable_R[0x00000 + i] = 0;
				else if(ftemp < 0)	mWave_TND.nOutputTable_R[0x00000 + i] = (short)(v / ((1.0f / ftemp) - 100));
				else				mWave_TND.nOutputTable_R[0x00000 + i] = (short)(v / ((1.0f / ftemp) + 100));
				
				ftemp = (r[0] * (i >> 11)) / -2097885;
				ftemp += (r[1] * ((i >> 7) & 0x0F)) / 3121455;
				ftemp += (r[2] * (i & 0x7F)) / 5772690;
				if(!ftemp)			mWave_TND.nOutputTable_R[0x08000 + i] = 0;
				else if(ftemp < 0)	mWave_TND.nOutputTable_R[0x08000 + i] = (short)(v / ((1.0f / ftemp) - 100));
				else				mWave_TND.nOutputTable_R[0x08000 + i] = (short)(v / ((1.0f / ftemp) + 100));
				
				ftemp = (r[0] * (i >> 11)) / 2097885;
				ftemp -= (r[1] * ((i >> 7) & 0x0F)) / 3121455;
				ftemp += (r[2] * (i & 0x7F)) / 5772690;
				if(!ftemp)			mWave_TND.nOutputTable_R[0x10000 + i] = 0;
				else if(ftemp < 0)	mWave_TND.nOutputTable_R[0x10000 + i] = (short)(v / ((1.0f / ftemp) - 100));
				else				mWave_TND.nOutputTable_R[0x10000 + i] = (short)(v / ((1.0f / ftemp) + 100));
				
				ftemp = (r[0] * (i >> 11)) / -2097885;
				ftemp -= (r[1] * ((i >> 7) & 0x0F)) / 3121455;
				ftemp += (r[2] * (i & 0x7F)) / 5772690;
				if(!ftemp)			mWave_TND.nOutputTable_R[0x18000 + i] = 0;
				else if(ftemp < 0)	mWave_TND.nOutputTable_R[0x18000 + i] = (short)(v / ((1.0f / ftemp) - 100));
				else				mWave_TND.nOutputTable_R[0x18000 + i] = (short)(v / ((1.0f / ftemp) + 100));
			}
		}
	}
	if(chans & 0x00000020)		//VRC6 Pulse 1
	{
		CalculateChannelVolume(1875,tl,tr,nChannelVol[5],nChannelPan[5]);
		for(i = 0; i < 0x10; i++)
		{
			mWave_VRC6Pulse[0].nOutputTable_L[i] = tl * i / 0x0F;
			mWave_VRC6Pulse[0].nOutputTable_R[i] = tr * i / 0x0F;
		}
	}
	if(chans & 0x00000040)		//VRC6 Pulse 2
	{
		CalculateChannelVolume(1875,tl,tr,nChannelVol[6],nChannelPan[6]);
		for(i = 0; i < 0x10; i++)
		{
			mWave_VRC6Pulse[1].nOutputTable_L[i] = tl * i / 0x0F;
			mWave_VRC6Pulse[1].nOutputTable_R[i] = tr * i / 0x0F;
		}
	}
	if(chans & 0x00000080)		//VRC6 Saw
	{
		CalculateChannelVolume(3750,tl,tr,nChannelVol[7],nChannelPan[7]);
		for(i = 0; i < 0x20; i++)
		{
			mWave_VRC6Saw.nOutputTable_L[i] = tl * i / 0x1F;
			mWave_VRC6Saw.nOutputTable_R[i] = tr * i / 0x1F;
		}
	}

	//the 2 MMC5 squares are probably too loud (1875 seems like it -should- be the proper base), but
	// they seemed way to quiet in contrast to the triangle in Just Breed.  Therefore their base vol
	// has been bumped up a bit
	if(chans & 0x00000100)		//MMC5 Square 1
	{
		CalculateChannelVolume(2500,tl,tr,nChannelVol[8],nChannelPan[8]);
		for(i = 0; i < 0x10; i++)
		{
			mWave_MMC5Square[0].nOutputTable_L[i] = tl * i / 0x0F;
			mWave_MMC5Square[0].nOutputTable_R[i] = tr * i / 0x0F;
		}
	}
	if(chans & 0x00000200)		//MMC5 Square 2
	{
		CalculateChannelVolume(2500,tl,tr,nChannelVol[9],nChannelPan[9]);
		for(i = 0; i < 0x10; i++)
		{
			mWave_MMC5Square[1].nOutputTable_L[i] = tl * i / 0x0F;
			mWave_MMC5Square[1].nOutputTable_R[i] = tr * i / 0x0F;
		}
	}
	if(chans & 0x00000400)		//MMC5 Voice
	{
		CalculateChannelVolume(15000,tl,tr,nChannelVol[10],nChannelPan[10]);
		for(i = 0; i < 0x10; i++)
		{
			mWave_MMC5Voice.nOutputTable_L[i] = tl * i / 0x7F;
			mWave_MMC5Voice.nOutputTable_R[i] = tr * i / 0x7F;
		}
	}
	if(chans & 0x0007F800)		//N106 channels
	{
		for(v = 0; v < 8; v++)
		{
			if(!(chans & (0x800 << v))) continue;

			CalculateChannelVolume(3000,tl,tr,nChannelVol[11 + v],nChannelPan[11 + v]);
			//this amplitude is just a guess =\

			for(i = 0; i < 0x10; i++)
			{
				for(j = 0; j < 0x10; j++)
				{
					mWave_N106.nOutputTable_L[v][i][j] = (tl * i * j) / 0xE1;
					mWave_N106.nOutputTable_R[v][i][j] = (tr * i * j) / 0xE1;
				}
			}
		}
	}
	if(chans & 0x01F80000)		//VRC7 channels
	{
		if(pFMOPL)
		{
			for(v = 0; v < 6; v++)
			{
				if(chans & (0x80000 << v))
					VRC7_RecalcMultiplier((BYTE)v);
			}
		}
	}

	
	if(chans & 0x02000000)		//FME-07 Square A
	{
		CalculateChannelVolume(3000,tl,tr,nChannelVol[25],nChannelPan[25]);
		mWave_FME07[0].nOutputTable_L[15] = tl;
		mWave_FME07[0].nOutputTable_R[15] = tr;
		mWave_FME07[0].nOutputTable_L[0] = 0;
		mWave_FME07[0].nOutputTable_R[0] = 0;
		for(i = 14; i > 0; i--)
		{
			mWave_FME07[0].nOutputTable_L[i] = mWave_FME07[0].nOutputTable_L[i + 1] * 80 / 100;
			mWave_FME07[0].nOutputTable_R[i] = mWave_FME07[0].nOutputTable_R[i + 1] * 80 / 100;
		}
	}
	if(chans & 0x04000000)		//FME-07 Square B
	{
		CalculateChannelVolume(3000,tl,tr,nChannelVol[26],nChannelPan[26]);
		mWave_FME07[1].nOutputTable_L[15] = tl;
		mWave_FME07[1].nOutputTable_R[15] = tr;
		mWave_FME07[1].nOutputTable_L[0] = 0;
		mWave_FME07[1].nOutputTable_R[0] = 0;
		for(i = 14; i > 0; i--)
		{
			mWave_FME07[1].nOutputTable_L[i] = mWave_FME07[1].nOutputTable_L[i + 1] * 80 / 100;
			mWave_FME07[1].nOutputTable_R[i] = mWave_FME07[1].nOutputTable_R[i + 1] * 80 / 100;
		}
	}
	if(chans & 0x08000000)		//FME-07 Square C
	{
		CalculateChannelVolume(3000,tl,tr,nChannelVol[27],nChannelPan[27]);
		mWave_FME07[2].nOutputTable_L[15] = tl;
		mWave_FME07[2].nOutputTable_R[15] = tr;
		mWave_FME07[2].nOutputTable_L[0] = 0;
		mWave_FME07[2].nOutputTable_R[0] = 0;
		for(i = 14; i > 0; i--)
		{
			mWave_FME07[2].nOutputTable_L[i] = mWave_FME07[2].nOutputTable_L[i + 1] * 80 / 100;
			mWave_FME07[2].nOutputTable_R[i] = mWave_FME07[2].nOutputTable_R[i + 1] * 80 / 100;
		}
	}

	/*
	 *	FDS
	 */
	if(chans & 0x10000000)
	{
		//  this base volume (4000) is just a guess to what sounds right.  Given the number of steps available in an FDS
		//	wave... it seems like it should be much much more... but then it's TOO loud.
		CalculateChannelVolume(4000,tl,tr,nChannelVol[28],nChannelPan[28]);
		for(i = 0; i < 0x21; i++)
		{
			for(j = 0; j < 0x40; j++)
			{
				int sj = j - 0x20;
				mWave_FDS.nOutputTable_L[0][i][j] = (tl * i * sj * 30) / (0x21 * 0x40 * 30);
				mWave_FDS.nOutputTable_R[0][i][j] = (tr * i * sj * 30) / (0x21 * 0x40 * 30);

				mWave_FDS.nOutputTable_L[1][i][j] = (tl * i * sj * 20) / (0x21 * 0x40 * 30);
				mWave_FDS.nOutputTable_R[1][i][j] = (tr * i * sj * 20) / (0x21 * 0x40 * 30);

				mWave_FDS.nOutputTable_L[2][i][j] = (tl * i * sj * 15) / (0x21 * 0x40 * 30);
				mWave_FDS.nOutputTable_R[2][i][j] = (tr * i * sj * 15) / (0x21 * 0x40 * 30);

				mWave_FDS.nOutputTable_L[3][i][j] = (tl * i * sj * 12) / (0x21 * 0x40 * 30);
				mWave_FDS.nOutputTable_R[3][i][j] = (tr * i * sj * 12) / (0x21 * 0x40 * 30);
			}
		}
	}
}

/*
 *	GetPlayCalls
 */

float CNSFCore::GetPlayCalls()
{
	if(!fTicksPerPlay)	return 0;

	return ((float)nTotalPlays) + (1.0f - (fTicksUntilNextPlay / fTicksPerPlay));
}

/*
 *	GetWrittenTime
 */
UINT CNSFCore::GetWrittenTime(float basedplayspersec /* = 0 */)
{
	if(basedplayspersec <= 0)
		basedplayspersec = GetPlaybackSpeed();

	if(basedplayspersec <= 0)
		return 0;

	return (UINT)((GetPlayCalls() * 1000) / basedplayspersec);
}

/*
 *	SetPlayCalls
 */

void CNSFCore::SetPlayCalls(float plays)
{
	WaitForSamples();
	if(!bTrackSelected)		return;

	float old = GetPlayCalls();
	if(old == plays)		return;		//long shot... but don't seek to the exact same time we're at

	if(old > plays)
	{
		//can't seek backwards, so restart the song and seek forwards
		BYTE temp = bFade;
		SetTrack(nCurTrack);
		bFade = temp;
		old = 0;
	}

	int runto = (int)((plays - old) * fTicksPerPlay);
	int tick;
	pOutput = NULL;
	pVRC7Buffer = NULL;

	while(runto)
	{
		nCPUCycle = nAPUCycle = 0;
		tick = (int)ceil(fTicksUntilNextPlay);
		if(tick > runto)
			tick = runto;

		if(!bCPUJammed)
			tick = Emulate6502(tick);

		runto -= tick;
		fTicksUntilNextPlay -= tick;
		if(fTicksUntilNextPlay <= 0)
		{
			fTicksUntilNextPlay += fTicksPerPlay;
			if(bCPUJammed == 2)
			{
				nTotalPlays++;
				bCPUJammed = 0;
			}
		}
	}

	EmulateAPU(0);
	nCPUCycle = nAPUCycle = 0;


	if(bFade)
		RecalculateFade();
}

/*
 *	SetWrittenTime
 */

void CNSFCore::SetWrittenTime(UINT ms,float basedplays /* = 0 */)
{
	WaitForSamples();
	if(!bTrackSelected)		return;

	if(basedplays <= 0)
		basedplays = GetPlaybackSpeed();
	if(basedplays <= 0)
		return;

	SetPlayCalls(ms * basedplays / 1000);
}

/*
 *	StopFade
 */
void CNSFCore::StopFade()
{
	bFade = 0;
	fFadeVolume = 1;
	bVRC7_FadeChanged = 0;
}

/*
 *	SongCompleted
 */

BYTE CNSFCore::SongCompleted()
{
	if(!bFade)						return 0;
	if(nTotalPlays >= nEndFade)		return 1;
	if(nSilentSampleMax)			return (nSilentSamples >= nSilentSampleMax);

	return 0;
}

/*
 *	SetFade
 */

void CNSFCore::SetFade(int fadestart,int fadestop,BYTE bNotDefault)	//play routine calls
{
	if(fadestart < 0)	fadestart = 0;
	if(fadestop < fadestart) fadestop = fadestart;

	nStartFade = (unsigned)fadestart;
	nEndFade = (unsigned)fadestop;
	bFade = 1;
	bTimeNotDefault = bNotDefault;

	RecalcSilenceTracker();
	RecalculateFade();
}

/*
 *	SetFadeTime
 */

void CNSFCore::SetFadeTime(UINT fadestart,UINT fadestop,float basedplays,BYTE bNotDefault)	//time in MS
{
	if(basedplays <= 0)
		basedplays = GetPlaybackSpeed();
	if(basedplays <= 0)
		return;

	SetFade((int)(fadestart * basedplays / 1000),(int)(fadestop * basedplays / 1000),bNotDefault);
}

/*
 *	RecalculateFade
 */

void CNSFCore::RecalculateFade()
{
	if(!bFade)	return;

	int temp = (int)(GetPlaybackSpeed() / 4);		//make it hit silence a little before the song ends... otherwise we're not really fading OUT, we're just fading umm... quieter =P

	if(nEndFade <= nStartFade)
	{
		nEndFade = nStartFade;
		fFadeChange = 1.0f;
	}
	else if((nEndFade - temp) <= nStartFade)
		fFadeChange = 1.0f;
	else
		fFadeChange = 1.0f / (nEndFade - nStartFade - temp);

	if(nTotalPlays < nStartFade)
		fFadeVolume = 1.0f;
	else if(nTotalPlays >= nEndFade)
		fFadeVolume = 0.0f;
	else
	{
		fFadeVolume = 1.0f - ( (nTotalPlays - nStartFade + 1) * fFadeChange );
		if(fFadeVolume < 0)
			fFadeVolume = 0;
	}

	bVRC7_FadeChanged = 1;
}

int CNSFCore::RunOneFrame()
{
	if (!bTrackSelected)
		return 0;

	ResetFrameState();

	nCPUCycle = nAPUCycle = 0;
	UINT tick;
		
	while (1)
	{
		tick = (UINT)ceil(fTicksUntilNextPlay);

		if (bCPUJammed)
		{
			nCPUCycle += tick;
			EmulateAPU(0);
		}
		else
		{
			tick = Emulate6502(tick + nCPUCycle);
			EmulateAPU(1);
		}

		fTicksUntilNextPlay -= tick;
		if (fTicksUntilNextPlay <= 0)
		{
			fTicksUntilNextPlay += fTicksPerPlay;
			if ((bCPUJammed == 2) || bNoWaitForReturn)
			{
				regX = regY = regA = (bCleanAXY ? 0 : 0xCD);
				regPC = 0x5004;
				nTotalPlays++;
				bDMCPop_SamePlay = 0;
				bCPUJammed = 0;
			}

			break;
		}
	}

	nCPUCycle = nAPUCycle = 0;
	bIsGeneratingSamples = 0;

	return nPlayCalled;
}

template <typename T>
int IndexOf(const T* array, int arraySize, T val)
{
	for (int i = 0; i < arraySize; i++)
	{
		if (array[i] == val)
			return i;
	}

	return -1;
}

extern BYTE VRC7Instrument[16][8];

void CNSFCore::ResetFrameState()
{
	for (int i = 0; i < 6; i++)
		VRC7Triggered[i] = 0;
	mWave_FME07[0].bEnvelopeTriggered = 0;
	mWave_EPSM[0].bEnvelopeTriggered = 0;
}

int CNSFCore::GetState(int channel, int state, int sub)
{
	switch (channel)
	{
		case CHANNEL_SQUARE1:
		case CHANNEL_SQUARE2:
		{
			switch (state)
			{
				case STATE_PERIOD:    return mWave_Squares.nFreqTimer[channel].W;
				case STATE_DUTYCYCLE: return IndexOf(DUTY_CYCLE_TABLE, 4, mWave_Squares.nDutyCycle[channel]);
				case STATE_VOLUME:    return mWave_Squares.nLengthCount[channel] && mWave_Squares.bChannelEnabled[channel] ? mWave_Squares.nVolume[channel] : 0;
			}
			break;
		}
		case CHANNEL_TRIANGLE:
		{
			switch (state)
			{
				case STATE_PERIOD:    return mWave_TND.nTriFreqTimer.W;
				case STATE_VOLUME:    return mWave_TND.nTriLengthCount && mWave_TND.bTriChannelEnabled ? mWave_TND.nTriLinearCount : 0;
			}
			break;
		}
		case CHANNEL_NOISE:
		{
			switch (state)
			{
			    case STATE_VOLUME:    return mWave_TND.nNoiseLengthCount && mWave_TND.bNoiseChannelEnabled ? mWave_TND.nNoiseVolume : 0;
				case STATE_DUTYCYCLE: return mWave_TND.bNoiseRandomMode == 6 ? 1 : 0;
				case STATE_PERIOD:    return IndexOf(NOISE_FREQ_TABLE, 16, mWave_TND.nNoiseFreqTimer);
			}
			break;
		}
		case CHANNEL_DPCM:
		{
			switch (state)
			{
				case STATE_DPCMSAMPLELENGTH: 	
				{
					if (mWave_TND.bDMCTriggered)
					{
						mWave_TND.bDMCTriggered = 0;
						return mWave_TND.nDMCLength;
					}
					else
					{
						return 0;
					}
				}
				case STATE_DPCMSAMPLEADDR:
				{
					return mWave_TND.nDMCDMABank_Load << 16 | mWave_TND.nDMCDMAAddr_Load;
				}
				case STATE_DPCMLOOP:
				{
					return mWave_TND.bDMCLoop;
				}
				case STATE_DPCMPITCH:
				{
					return IndexOf(DMC_FREQ_TABLE[bPALMode], 0x10, mWave_TND.nDMCFreqTimer);
				}
				case STATE_DPCMSAMPLEDATA:   
				{
					int bank = mWave_TND.nDMCDMABank_Load;
					int addr = mWave_TND.nDMCDMAAddr_Load + sub;
					if (addr & 0x1000)
					{
						addr &= 0x0FFF;
						bank = (bank + 1) & 0x07;
					}
					return mWave_TND.pDMCDMAPtr[bank][addr];
				}
				case STATE_DPCMCOUNTER:
				{
					return mWave_TND.bDMCLastDeltaWrite;
				}
				case STATE_DPCMACTIVE:
				{
					return mWave_TND.bDMCActive;
				}
			}
			break;
		}
		case CHANNEL_VRC6SQUARE1:
		case CHANNEL_VRC6SQUARE2:
		{
			int idx = channel - CHANNEL_VRC6SQUARE1;
			switch (state)
			{
				case STATE_PERIOD:    return mWave_VRC6Pulse[idx].nFreqTimer.W;
				case STATE_DUTYCYCLE: return mWave_VRC6Pulse[idx].nDutyCycle;
				case STATE_VOLUME:    return mWave_VRC6Pulse[idx].bChannelEnabled ? mWave_VRC6Pulse[idx].nVolume : 0;
			}
			break;
		}
		case CHANNEL_VRC6SAW:
		{
			switch (state)
			{
				case STATE_PERIOD:    return mWave_VRC6Saw.nFreqTimer.W;
				case STATE_VOLUME:    return mWave_VRC6Saw.bChannelEnabled ? mWave_VRC6Saw.nAccumRate : 0;
			}
			break;
		}
		case CHANNEL_FDS:
		{
			switch (state)
			{
				case STATE_PERIOD:             return mWave_FDS.nFreq.W;
				case STATE_VOLUME:             return mWave_FDS.bEnabled ? mWave_FDS.nVolume : 0;
				case STATE_FDSWAVETABLE:       return mWave_FDS.nWaveTable[sub];
				case STATE_FDSMODULATIONTABLE: return mWave_FDS.nLFO_Table[sub * 2];
				case STATE_FDSMODULATIONDEPTH: return mWave_FDS.bLFO_On && (mWave_FDS.nSweep_Mode & 2) ? mWave_FDS.nSweep_Gain : 0;
				case STATE_FDSMODULATIONSPEED: return mWave_FDS.bLFO_On ? mWave_FDS.nLFO_Freq.W : 0;
				case STATE_FDSMASTERVOLUME:    return mWave_FDS.nMainVolume;
			}
		}
		case CHANNEL_VRC7FM1:
		case CHANNEL_VRC7FM2:
		case CHANNEL_VRC7FM3:
		case CHANNEL_VRC7FM4:
		case CHANNEL_VRC7FM5:
		case CHANNEL_VRC7FM6:
		{
			int idx = channel - CHANNEL_VRC7FM1;
			switch (state)
			{
				case STATE_PERIOD:          return ((VRC7Chan[1][idx] & 1) << 8) | (VRC7Chan[0][idx]);
				case STATE_VOLUME:          return (VRC7Chan[2][idx] >> 0) & 0xF;
				case STATE_VRC7PATCH:       return (VRC7Chan[2][idx] >> 4) & 0xF;
				case STATE_FMPATCHREG:      return (VRC7Instrument[0][sub]);
				case STATE_FMOCTAVE:        return (VRC7Chan[1][idx] >> 1) & 0x07;
				case STATE_FMTRIGGER:       return (VRC7Chan[1][idx] >> 4) & 0x01;
				case STATE_FMTRIGGERCHANGE: return (VRC7Triggered[idx]);
				case STATE_FMSUSTAIN:       return (VRC7Chan[1][idx] >> 5) & 0x01;
			}
		}
		case MMC5_SQUARE1:
		case MMC5_SQUARE2:
		{
			int idx = channel - MMC5_SQUARE1;
			switch (state)
			{
				case STATE_PERIOD:    return mWave_MMC5Square[idx].nFreqTimer.W;
				case STATE_DUTYCYCLE: return IndexOf(DUTY_CYCLE_TABLE, 4, mWave_MMC5Square[idx].nDutyCycle);
				case STATE_VOLUME:    return mWave_MMC5Square[idx].nLengthCount && mWave_MMC5Square[idx].bChannelEnabled ? mWave_MMC5Square[idx].nVolume : 0;
			}
			break;
		}
		case N163_WAVE1:
		case N163_WAVE2:
		case N163_WAVE3:
		case N163_WAVE4:
		case N163_WAVE5:
		case N163_WAVE6:
		case N163_WAVE7:
		case N163_WAVE8:
		{
			int idx = 7 - (channel - N163_WAVE1);
			switch (state)
			{
				case STATE_PERIOD:          return mWave_N106.nFreqReg[idx].D;
				case STATE_VOLUME:          return mWave_N106.nVolume[idx];
				case STATE_N163WAVEPOS:     return mWave_N106.nWavePosStart[idx];
				case STATE_N163WAVESIZE:    return mWave_N106.nWaveSize[idx];
				case STATE_N163WAVE:        return mWave_N106.nRAM[sub];
				case STATE_N163NUMCHANNELS: return mWave_N106.nActiveChannels + 1;
			}
			break;
		}
		case S5B_SQUARE1:
		case S5B_SQUARE2:
		case S5B_SQUARE3:
		{
			int idx = channel - S5B_SQUARE1;
			switch (state)
			{
				case STATE_PERIOD:				return mWave_FME07[idx].nFreqTimer.W;
				case STATE_VOLUME:				return mWave_FME07[idx].bChannelEnabled ? mWave_FME07[idx].nVolume : 0;
				case STATE_S5BMIXER:			return mWave_FME07[idx].bChannelMixer;
				case STATE_S5BNOISEFREQUENCY:	return mWave_FME07[0].bNoiseFrequency;
				case STATE_S5BENVFREQUENCY:		return mWave_FME07[0].nEnvFreq.W;
				case STATE_S5BENVSHAPE:			return mWave_FME07[0].nEnvelopeShape;
				case STATE_S5BENVTRIGGER:		return mWave_FME07[0].bEnvelopeTriggered;
				case STATE_S5BENVENABLED:		return mWave_FME07[idx].bEnvelopeEnabled;
			}
			break;
		}
		
		case EPSM_RYTHM1:
		case EPSM_RYTHM2:
		case EPSM_RYTHM3:
		case EPSM_RYTHM4:
		case EPSM_RYTHM5:
		case EPSM_RYTHM6:
		{
			int idx = channel - EPSM_SQUARE1;
			switch (state)
			{
			case STATE_STEREO: return (mWave_EPSM[idx].nVolume & 0xc0);
			case STATE_PERIOD: return 0xc20;
			case STATE_VOLUME: 
				int returnval = mWave_EPSM[idx].bChannelEnabled ? ((mWave_EPSM[idx].nVolume & 0x1f) >> 1) : 0;
				mWave_EPSM[idx].bChannelEnabled = 0;
				return returnval;
			}
			break;
		}
		case EPSM_SQUARE1:
		case EPSM_SQUARE2:
		case EPSM_SQUARE3:
		{
			int idx = channel - EPSM_SQUARE1;
			switch (state)
			{
			case STATE_PERIOD:				return mWave_EPSM[idx].nFreqTimer.W;
			case STATE_VOLUME:				return mWave_EPSM[idx].bChannelEnabled ? mWave_EPSM[idx].nVolume : 0;
			case STATE_S5BMIXER:			return mWave_EPSM[idx].bChannelMixer;
			case STATE_S5BNOISEFREQUENCY:	return mWave_EPSM[0].bNoiseFrequency;
			case STATE_S5BENVFREQUENCY:		return mWave_EPSM[0].nEnvFreq.W;
			case STATE_S5BENVSHAPE:			return mWave_EPSM[0].nEnvelopeShape;
			case STATE_S5BENVTRIGGER:		return mWave_EPSM[0].bEnvelopeTriggered;
			case STATE_S5BENVENABLED:		return mWave_EPSM[idx].bEnvelopeEnabled;
			}
			break;
		}
		case EPSM_FM1:
		case EPSM_FM2:
		case EPSM_FM3:
		case EPSM_FM4:
		case EPSM_FM5:
		case EPSM_FM6:
		{
			int idx = channel - EPSM_SQUARE1;
			switch (state)
			{
			case STATE_FMTRIGGER: {
				int trigger = mWave_EPSM[idx].nTriggered;
				mWave_EPSM[idx].nTriggered = 0;
				return trigger;}
			case STATE_FMOCTAVE: return (mWave_EPSM[idx].nFreqTimer.B.h >> 3) & 0x07;
			case STATE_PERIOD: return (mWave_EPSM[idx].nFreqTimer.B.l + ((mWave_EPSM[idx].nFreqTimer.B.h & 7) << 8))/4;
			case STATE_VOLUME: return (mWave_EPSM[idx].nStereo) ? 15 : 0;
			case STATE_FMSUSTAIN: return mWave_EPSM[idx].bChannelEnabled ? 1 : 0;
			case STATE_FMPATCHREG: return (mWave_EPSM[idx].nPatchReg[sub]);
			}
			break;
		}
	}

	return 0;
}

void CNSFCore::SetApuWriteCallback(ApuRegWriteCallback callback)
{
	apuRegWriteCallback = callback;
}

int CNSFCore::GetSamples(BYTE* buffer,int buffersize)
{
	if(!buffer)								return 0;
	if(buffersize < 16)						return 0;
	if(!bTrackSelected)						return 0;
	if(bFade && (nTotalPlays >= nEndFade))	return 0;
	if(bIsGeneratingSamples)				return 0;
	
	bIsGeneratingSamples = 1;

	
	pOutput = pVRC7Buffer = buffer;
	UINT runtocycle = (UINT)((buffersize / ((nMonoStereo == 2) ? 4 : 2)) * fTicksPerSample);
	nCPUCycle = nAPUCycle = 0;
	UINT tick;

	while(1)
	{
		tick = (UINT)ceil(fTicksUntilNextPlay);
		if((tick + nCPUCycle) > runtocycle)
			tick = runtocycle - nCPUCycle;

		if(bCPUJammed)
		{
			nCPUCycle += tick;
			EmulateAPU(0);
		}
		else
		{
			tick = Emulate6502(tick + nCPUCycle);
			EmulateAPU(1);
		}

		fTicksUntilNextPlay -= tick;
		if(fTicksUntilNextPlay <= 0)
		{
			fTicksUntilNextPlay += fTicksPerPlay;
			if((bCPUJammed == 2) || bNoWaitForReturn)
			{
				regX = regY = regA = (bCleanAXY ? 0 : 0xCD);
				regPC = 0x5004;
				nTotalPlays++;
				bDMCPop_SamePlay = 0;
				bCPUJammed = 0;
				if(nForce4017Write == 1)	WriteMemory_pAPU(0x4017,0x00);
				if(nForce4017Write == 2)	WriteMemory_pAPU(0x4017,0x80);
			}
			
			if(bFade && (nTotalPlays >= nStartFade))
			{
				bVRC7_FadeChanged = 1;
				fFadeVolume -= fFadeChange;
				if(fFadeVolume < 0)
					fFadeVolume = 0;
				if(nTotalPlays >= nEndFade)
					break;
			}
		}

		if(nCPUCycle >= runtocycle)
			break;
	}

	if((nExternalSound & EXTSOUND_VRC7))
		VRC7_Mix();

	nCPUCycle = nAPUCycle = 0;
	bIsGeneratingSamples = 0;
	pVRC7Buffer = NULL;

	if(nSilentSampleMax && bFade)
	{
		short* tempbuf = (short*)buffer;
		while( ((BYTE*)tempbuf) < pOutput)
		{
			if( (*tempbuf < -SILENCE_THRESHOLD) || (*tempbuf > SILENCE_THRESHOLD) )
				nSilentSamples = 0;
			else
			{
				if(++nSilentSamples >= nSilentSampleMax)
					return (int)( ((BYTE*)tempbuf) - buffer);
			}
			tempbuf++;
		}
	}

	return (int)(pOutput - buffer);
}