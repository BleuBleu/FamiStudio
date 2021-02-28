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
//  Wave_MMC5.h
//
//    These are similar to the native NES square waves, only they lack a sweep unit and aren't
//  (as far as I know), interdependant on each other's output.
//
//    The voice is similar to the $4011 register, but without the DMC's sample playback capabilities.
//  so it's rather useless.  I haven't been able to find any game to test it with (since I'm not aware of
//  any who use it... nor do I see how it could be used in an NSF because of lack of IRQ support).  But it's
//  included anyway.  Theoretically it should work... but like I said, can't test it.

class CMMC5SquareWave
{
public:

	///////////////////////////////////
	// Programmable Timer
	TWIN		nFreqTimer;
	int			nFreqCount;

	///////////////////////////////////
	// Length Counter
	BYTE		nLengthCount;
	BYTE		bLengthEnabled;
	BYTE		bChannelEnabled;

	///////////////////////////////////
	// Volume / Decay
	BYTE		nVolume;
	BYTE		nDecayVolume;
	BYTE		bDecayEnable;
	BYTE		bDecayLoop;
	BYTE		nDecayTimer;
	BYTE		nDecayCount;

	///////////////////////////////////
	// Duty Cycle
	BYTE		nDutyCount;
	BYTE		nDutyCycle;

	///////////////////////////////////
	// Output and Downsampling
	BYTE		bChannelMix;
	short		nOutputTable_L[0x10];
	short		nOutputTable_R[0x10];
	int			nMixL;
	int			nMixR;

	///////////////////////////////////
	// Inverting
	BYTE		bDoInvert;
	BYTE		bInvert;
	WORD		nInvertFreqCutoff;

	///////////////////////////////////////////////////////////////////
	///////////////////////////////////////////////////////////////////
	//  Functions
	///////////////////////////////////////////////////////////////////
	///////////////////////////////////////////////////////////////////

	FORCEINLINE void ClockMajor()		//decay
	{
		if(nDecayCount)
			nDecayCount--;
		else
		{
			nDecayCount = nDecayTimer;
			if(nDecayVolume)
				nDecayVolume--;
			else
			{
				if(bDecayLoop)
					nDecayVolume = 0x0F;
			}

			if(bDecayEnable)
				nVolume = nDecayVolume;
		}
	}

	FORCEINLINE void ClockMinor()		//length
	{
		if(bLengthEnabled && nLengthCount)
			nLengthCount--;
	}

	FORCEINLINE void DoTicks(int ticks,BYTE mix)
	{
		register int mn;

		if(nFreqTimer.W < 8) return;

		while(ticks)
		{
			mn = min(nFreqCount,ticks);
			ticks -= mn;

			nFreqCount -= mn;

			if(mix && (nDutyCount < nDutyCycle) && nLengthCount)
			{
				nMixL += nOutputTable_L[nVolume] * mn;
				nMixR += nOutputTable_R[nVolume] * (bDoInvert ? -mn : mn);
			}

			if(!nFreqCount)
			{
				nFreqCount = nFreqTimer.W + 1;
				nDutyCount = (nDutyCount + 1) & 0x0F;
				if(!nDutyCount)
				{
					bDoInvert = bInvert;
					if(nInvertFreqCutoff < nFreqTimer.W)
						bDoInvert = 0;
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


class CMMC5VoiceWave
{
public:
	///////////////////////////////////
	// Everything
	BYTE		nOutput;
	short		nOutputTable_L[0x80];
	short		nOutputTable_R[0x80];
	int			nMixL;
	int			nMixR;
	BYTE		bInvert;

	///////////////////////////////////////////////////////////////////
	///////////////////////////////////////////////////////////////////
	//  Functions
	///////////////////////////////////////////////////////////////////
	///////////////////////////////////////////////////////////////////


	FORCEINLINE void DoTicks(int ticks)
	{
		nMixL += nOutputTable_L[nOutput] * ticks;
		nMixR += nOutputTable_R[nOutput] * (bInvert ? -ticks : ticks);
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