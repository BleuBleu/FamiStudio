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
//  Wave_FDS.h

#include <stdio.h>

static const int ModulationTable[8] = {0,1,2,4,0,-4,-2,-1};

class CFDSWave
{
public:
	/*	Envelope Unit	*/
	BYTE		bEnvelopeEnable;
	BYTE		nEnvelopeSpeed;

	/*	Volume Envelope	*/
	BYTE		nVolEnv_Mode;
	BYTE		nVolEnv_Decay;
	BYTE		nVolEnv_Gain;
	int			nVolEnv_Timer;
	int			nVolEnv_Count;
	BYTE		nVolume;
	BYTE		bVolEnv_On;

	/*	Sweep Envenlope	*/
	BYTE		nSweep_Mode;
	BYTE		nSweep_Decay;
	int			nSweep_Timer;
	int			nSweep_Count;
	BYTE		nSweep_Gain;
	BYTE		bSweepEnv_On;

	/*	Effector / LFO / Modulation	Unit	*/
	int			nSweepBias;
	BYTE		bLFO_Enabled;
	TWIN		nLFO_Freq;
	float		fLFO_Timer;
	float		fLFO_Count;
	BYTE		nLFO_Addr;
	BYTE		nLFO_Table[0x40];
	BYTE		bLFO_On;

	/*	Main Output		*/
	BYTE		nMainVolume;
	BYTE		bEnabled;
	TWIN		nFreq;
	float		fFreqCount;
	BYTE		nMainAddr;
	BYTE		nWaveTable[0x40];
	BYTE		bWaveWrite;
	BYTE		bMain_On;

	/*	Output and Downsampling	*/
	short		nOutputTable_L[4][0x21][0x40];
	short		nOutputTable_R[4][0x21][0x40];
	int			nMixL;
	int			nMixR;

	/*	Inversion				*/
	BYTE		bInvert;

	/*	Pop Reducer				*/
	BYTE		bPopReducer;
	BYTE		nPopOutput;
	int			nPopCount;
	
	///////////////////////////////////////////////////////////////////
	///////////////////////////////////////////////////////////////////
	//  Functions
	///////////////////////////////////////////////////////////////////
	///////////////////////////////////////////////////////////////////

	FORCEINLINE void DoTicks(int ticks,BYTE mix)
	{
		int mn;
		float freq;

		while(ticks)
		{
			mn = ticks;
			if(bVolEnv_On)						mn = min(mn,nVolEnv_Count);
			if(bSweepEnv_On)					mn = min(mn,nSweep_Count);
			if(bLFO_On)							mn = (int)min(mn,(fLFO_Count + 1));
			if(bMain_On)						mn = (int)min(mn,(fFreqCount + 1));
			else if(bPopReducer && nPopOutput)	mn = min(mn,nPopCount);
			ticks -= mn;

			/*	Volume Envelope Unit	*/
			if(bVolEnv_On)
			{
				nVolEnv_Count -= mn;
				if(nVolEnv_Count <= 0)
				{
					nVolEnv_Count += nVolEnv_Timer;
					if(nVolEnv_Mode){	if(nVolEnv_Gain < 0x20)	nVolEnv_Gain++; }
					else			{	if(nVolEnv_Gain)		nVolEnv_Gain--; }
				}
			}
			
			/*	Sweep Envelope Unit	*/
			if(bSweepEnv_On)
			{
				nSweep_Count -= mn;
				if(nSweep_Count <= 0)
				{
					nSweep_Count += nSweep_Timer;
					if(nSweep_Mode)	{	if(nSweep_Gain < 0x20)	nSweep_Gain++; }
					else			{	if(nSweep_Gain)			nSweep_Gain--; }
				}
			}

			/*	Effector / LFO		*/
			int		subfreq = 0;
			if(bLFO_On)
			{
				fLFO_Count -= mn;
				if(fLFO_Count <= 0)
				{
					fLFO_Count += fLFO_Timer;
					if(nLFO_Table[nLFO_Addr] == 4)	nSweepBias = 0;
					else							nSweepBias += ModulationTable[ nLFO_Table[nLFO_Addr] ];
					nLFO_Addr = (nLFO_Addr + 1) & 0x3F;
				}

				while(nSweepBias >  63)	nSweepBias -= 128;
				while(nSweepBias < -64)	nSweepBias += 128;

				register int temp = nSweepBias * nSweep_Gain;
				if(temp & 0x0F)
				{
					temp /= 16;
					if(nSweepBias < 0)	temp--;
					else				temp += 2;
				}
				else
					temp /= 16;

				if(temp > 193)	temp -= 258;
				if(temp < -64)	temp += 256;

				subfreq = nFreq.W * temp / 64;
			}

			/*	Main Unit		*/
			if(bMain_On)
			{
				if(mix)
				{
					nMixL += nOutputTable_L[nMainVolume][nVolume][ nWaveTable[nMainAddr] ] * mn;
					nMixR += nOutputTable_R[nMainVolume][nVolume][ nWaveTable[nMainAddr] ] * (bInvert ? -mn : mn);
				}

				if((subfreq + nFreq.W) > 0)
				{
					freq = 65536.0f / (subfreq + nFreq.W);

					fFreqCount -= mn;
					if(fFreqCount <= 0)
					{
						fFreqCount += freq;

						nMainAddr = (nMainAddr + 1) & 0x3F;
						nPopOutput = nWaveTable[nMainAddr];
						if(!nMainAddr)
						{
							if(nVolEnv_Gain < 0x20)		nVolume = nVolEnv_Gain;
							else						nVolume = 0x20;
						}
					}
				}
				else
					fFreqCount = fLFO_Count;
			}
			else if(bPopReducer && nPopOutput)
			{
				if(mix)
				{
					nMixL += nOutputTable_L[nMainVolume][nVolume][ nPopOutput ] * mn;
					nMixR += nOutputTable_R[nMainVolume][nVolume][ nPopOutput ] * (bInvert ? -mn : mn);
				}

				nPopCount -= mn;
				if(nPopCount <= 0)
				{
					nPopCount += 500;
					nPopOutput--;
					if(!nPopOutput)
						nMainAddr = 0;
				}
			}
		}
	}
	
	//////////////////////////////////////////////////////////////////////////
	//  Mixing

	FORCEINLINE void Mix_Mono(int& mix,int downsample)
	{
		mix += (nMixL / downsample);
		nMixL = 0;
	}
	
	FORCEINLINE void Mix_Stereo(int& mixL,int& mixR,int downsample)
	{
		mixL += (nMixL / downsample);
		mixR += (nMixR / downsample);

		nMixL = nMixR = 0;
	}
};