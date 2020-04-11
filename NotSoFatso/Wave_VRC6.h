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
//  Wave_VRC6.h
//

class CVRC6PulseWave
{
public:

	///////////////////////////////////
	// Frequency Control
	TWIN		nFreqTimer;
	int			nFreqCount;
	int			nFreqInvHalt;

	///////////////////////////////////
	// Flags
	BYTE		bChannelEnabled;
	BYTE		bDigitized;

	///////////////////////////////////
	// Volume
	BYTE		nVolume;

	///////////////////////////////////
	// Duty Cycle
	BYTE		nDutyCycle;
	BYTE		nDutyCount;

	///////////////////////////////////
	// Output and Downsampling
	short		nOutputTable_L[0x10];
	short		nOutputTable_R[0x10];
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

	FORCEINLINE void DoTicks(int ticks,BYTE mix)
	{
		register int mn;

		if(!bChannelEnabled)
			return;

		while(ticks)
		{
			mn = min(nFreqCount,ticks);
			ticks -= mn;

			nFreqCount -= mn;

			if(mix && (nDutyCount <= nDutyCycle))
			{
				nMixL += nOutputTable_L[nVolume] * mn;
				nMixR += nOutputTable_R[nVolume] * (bDoInvert ? -mn : mn);
			}

			if(nFreqCount > 0) continue;
			nFreqCount = nFreqTimer.W + 1;

			if(!bDigitized)
			{
				nDutyCount = (nDutyCount + 1) & 0x0F;
				if(nDutyCount == 10)
				{
					bDoInvert = bInvert;
					if(nInvertFreqCutoff < nFreqTimer.W)
						bDoInvert = 0;
				}
			}
			else
			{
				bDoInvert = bInvert;
				if(nInvertFreqCutoff < nFreqTimer.W)
					bDoInvert = 0;
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

class CVRC6SawWave
{
public:

	///////////////////////////////////
	// Frequency Control
	TWIN		nFreqTimer;
	int			nFreqCount;

	///////////////////////////////////
	// Flags
	BYTE		bChannelEnabled;

	///////////////////////////////////
	// Phase Accumulator
	BYTE		nAccumRate;
	BYTE		nAccum;
	BYTE		nAccumStep;

	///////////////////////////////////
	// Output and Downsampling
	short		nOutputTable_L[0x20];
	short		nOutputTable_R[0x20];
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

	FORCEINLINE void DoTicks(int ticks,BYTE mix)
	{
		register int mn;

		if(!bChannelEnabled)
			return;

		while(ticks)
		{
			mn = min(nFreqCount,ticks);
			ticks -= mn;

			nFreqCount -= mn;

			if(mix)
			{
				nMixL += nOutputTable_L[nAccum >> 3] * mn;
				nMixR += nOutputTable_R[nAccum >> 3] * (bDoInvert ? -mn : mn);
			}

			if(nFreqCount > 0) continue;
			nFreqCount = nFreqTimer.W + 1;

			nAccumStep++;
			if(nAccumStep == 14)
			{
				nAccumStep = 0;
				nAccum = 0;
				bDoInvert = bInvert;
				if(nInvertFreqCutoff < nFreqTimer.W)
					bDoInvert = 0;
			}
			else if(!(nAccumStep & 1))
				nAccum += nAccumRate;
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