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
//  Wave_Square.h
//

class CSquareWaves
{
public:

	///////////////////////////////////
	// Programmable Timer
	TWIN		nFreqTimer[2];
	int			nFreqCount[2];

	///////////////////////////////////
	// Length Counter
	BYTE		nLengthCount[2];
	BYTE		bLengthEnabled[2];
	BYTE		bChannelEnabled[2];

	///////////////////////////////////
	// Volume / Decay
	BYTE		nVolume[2];
	BYTE		nDecayVolume[2];
	BYTE		bDecayEnable[2];
	BYTE		bDecayLoop[2];
	BYTE		nDecayTimer[2];
	BYTE		nDecayCount[2];

	///////////////////////////////////
	// Sweep Unit
	BYTE		bSweepEnable[2];
	BYTE		bSweepMode[2];
	BYTE		bSweepForceSilence[2];
	BYTE		nSweepTimer[2];
	BYTE		nSweepCount[2];
	BYTE		nSweepShift[2];

	///////////////////////////////////
	// Duty Cycle
	BYTE		nDutyCount[2];
	BYTE		nDutyCycle[2];

	///////////////////////////////////
	// Output and Downsampling
	BYTE		bChannelMix[2];
	short		nOutputTable_L[0x100];
	short		nOutputTable_R[3][0x100];
	int			nMixL;
	int			nMixR;

	///////////////////////////////////
	// Inverting
	BYTE		bInvert;
	BYTE		bDoInvert;
	WORD		nInvertFreqCutoff;

	///////////////////////////////////////////////////////////////////
	///////////////////////////////////////////////////////////////////
	//  Functions
	///////////////////////////////////////////////////////////////////
	///////////////////////////////////////////////////////////////////

	FORCEINLINE void ClockMajor()		//decay
	{
		static int i = 0;
		for(i = 0; i < 2; i++)
		{
			if(nDecayCount[i])
				nDecayCount[i]--;
			else
			{
				nDecayCount[i] = nDecayTimer[i];
				if(nDecayVolume[i])
					nDecayVolume[i]--;
				else
				{
					if(bDecayLoop[i])
						nDecayVolume[i] = 0x0F;
				}

				if(bDecayEnable[i])
					nVolume[i] = nDecayVolume[i];
			}
		}
	}

	FORCEINLINE void CheckSweepForcedSilence(register int i)
	{
		if(nFreqTimer[i].W < 8){	bSweepForceSilence[i] = 1; return; }
		if(!bSweepMode[i] && (( nFreqTimer[i].W + (nFreqTimer[i].W >> nSweepShift[i])) >= 0x0800))
			{ bSweepForceSilence[i] = 1; return; }

		bSweepForceSilence[i] = 0;
	}

	FORCEINLINE void ClockMinor()		//sweep / length
	{
		static int i = 0;
		for(i = 0; i < 2; i++)
		{
			if(bLengthEnabled[i] && nLengthCount[i])
				nLengthCount[i]--;

			if(!bSweepEnable[i] || !nLengthCount[i] || bSweepForceSilence[i] || !nSweepShift[i])
				continue;

			if(nSweepCount[i])
				nSweepCount[i]--;
			else
			{
				nSweepCount[i] = nSweepTimer[i];
				if(bSweepMode[i])	nFreqTimer[i].W -= (nFreqTimer[i].W >> nSweepShift[i]) + !i;
				else				nFreqTimer[i].W += (nFreqTimer[i].W >> nSweepShift[i]);

				CheckSweepForcedSilence(i);
			}
		}
	}

	FORCEINLINE void DoTicks(int ticks)
	{
		register int mn;
		register BYTE out;

		while(ticks)
		{
			mn = min(nFreqCount[0],nFreqCount[1]);
			mn = min(mn,ticks);
			ticks -= mn;

			nFreqCount[0] -= mn;
			nFreqCount[1] -= mn;

			if((nDutyCount[0] < nDutyCycle[0]) && nLengthCount[0] && !bSweepForceSilence[0] && bChannelMix[0])
				out = (nVolume[0] << 4);
			else
				out = 0;

			if((nDutyCount[1] < nDutyCycle[1]) && nLengthCount[1] && !bSweepForceSilence[1] && bChannelMix[1])
				out |= nVolume[1];

			nMixL += nOutputTable_L[out] * mn;


			if(bDoInvert == 3)	nMixR -= nOutputTable_R[0][out] * mn;
			else				nMixR += nOutputTable_R[bDoInvert][out] * mn;

			if(!nFreqCount[0])
			{
				nFreqCount[0] = nFreqTimer[0].W + 1;
				if(++nDutyCount[0] >= 0x10)
				{
					nDutyCount[0] = 0;
					if(bInvert & 1)
					{
						if(nFreqTimer[0].W <= nInvertFreqCutoff)
							bDoInvert |= 1;
						else
							bDoInvert &= 2;
					}
					else
						bDoInvert &= 2;
				}
			}
			if(!nFreqCount[1])
			{
				nFreqCount[1] = nFreqTimer[1].W + 1;
				if(++nDutyCount[1] >= 0x10)
				{
					nDutyCount[1] = 0;
					if(bInvert & 2)
					{
						if(nFreqTimer[1].W <= nInvertFreqCutoff)
							bDoInvert |= 2;
						else
							bDoInvert &= 1;
					}
					else
						bDoInvert &= 1;
				}
			}
		}
	}

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