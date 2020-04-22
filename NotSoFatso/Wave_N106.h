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
//  Wave_N106.h
//
//    I love this chip!  Although it's not as easy as the others to emulate... but it sure sounds nice.
//


class CN106Wave
{
public:
	//////////////////////////////
	//  All Channel Stuff
	BYTE		nActiveChannels;
	BYTE		bAutoIncrement;
	BYTE		nCurrentAddress;
	BYTE		nRAM[0x100];				//internal memory for registers/wave data
	float		fFrequencyLookupTable[8];	//lookup table for frequency conversions


	//////////////////////////////
	//  Individual channel stuff
	//////////////////////////////
	//  Wavelength / Frequency
	QUAD		nFreqReg[8];
	float		fFreqTimer[8];
	float		fFreqCount[8];

	//////////////////////////////
	//  Wave data length / remaining
	BYTE		nWaveSize[8];
	BYTE		nWaveRemaining[8];

	//////////////////////////////
	//  Wave data position
	BYTE		nWavePosStart[8];
	BYTE		nWavePos[8];
	BYTE		nOutput[8];

	//////////////////////////////
	//  Volume
	BYTE		nVolume[8];

	//////////////////////////////
	//  Pop Reducer
	BYTE		nPreVolume[8];
	BYTE		nPopCheck[8];

	//////////////////////////////
	// Mixing
	short		nOutputTable_L[8][0x10][0x10];
	short		nOutputTable_R[8][0x10][0x10];
	int			nMixL[8];
	int			nMixR[8];

	//////////////////////////////
	// Inverting
	BYTE		bInvert[8];
	BYTE		bDoInvert[8];
	UINT		nInvertFreqCutoff[8][8];
	BYTE		nWaveSizeWritten[8];
	BYTE		nInvCheck[8];

	
	///////////////////////////////////////////////////////////////////
	///////////////////////////////////////////////////////////////////
	//  Functions
	///////////////////////////////////////////////////////////////////
	///////////////////////////////////////////////////////////////////

	FORCEINLINE void DoTicks(int ticks,BYTE* mix)
	{
		register int mn;
		register int i;
		int usetick;
		BYTE temp;

		for(i = (7 - nActiveChannels); i < 8; i++)
		{
			if(!nFreqReg[i].D)
			{
				// written frequency of zero will cause divide by zero error
				// makes me wonder if the formula was supposed to be Reg+1
				nVolume[i] = nPreVolume[i];
				bDoInvert[i] = bInvert[i];
				continue;
			}

			usetick = ticks;
			while(usetick)
			{
				mn = (int)ceil(fFreqCount[i]);
				mn = min(mn,usetick);

				usetick -= mn;
				if(mix[i])
				{
					nMixL[i] += nOutputTable_L[i][nVolume[i]][nOutput[i]] * mn;
					nMixR[i] += nOutputTable_R[i][nVolume[i]][nOutput[i]] * (bDoInvert[i] ? -mn : mn);
				}

				if(fFreqTimer[i] < 0)
					fFreqTimer[i] = (fFrequencyLookupTable[nActiveChannels] / nFreqReg[i].D);
				if(fFreqCount[i] > fFreqTimer[i])
					fFreqCount[i] = fFreqTimer[i];

				fFreqCount[i] -= mn;
				if(fFreqCount[i] <= 0)
				{
					fFreqCount[i] += fFreqTimer[i];
					if(nWaveRemaining[i])
					{
						nWaveRemaining[i]--;
						nWavePos[i]++;
					}
					if(!nWaveRemaining[i])
					{
						nWaveRemaining[i] = nWaveSize[i];
						nWavePos[i] = nWavePosStart[i];
						if(nVolume[i] != nPreVolume[i])
						{
							if(++nPopCheck[i] >= 2)
							{
								nPopCheck[i] = 0;
								nVolume[i] = nPreVolume[i];
							}
						}

						temp = bInvert[i] && (nInvertFreqCutoff[nActiveChannels][nWaveSizeWritten[i]] < nFreqReg[i].D);
						if(temp != bDoInvert[i])
						{
							if(++nInvCheck[i] >= 2)
							{
								nInvCheck[i] = 0;
								bDoInvert[i] = temp;
							}
						}
					}

					nOutput[i] = nRAM[nWavePos[i]];
					if(!nOutput[i])
					{
						nPopCheck[i] = 0;
						nVolume[i] = nPreVolume[i];
						bDoInvert[i] = bInvert[i] && (nInvertFreqCutoff[nActiveChannels][nWaveSizeWritten[i]] < nFreqReg[i].D);
					}
				}
			}
		}
	}

	FORCEINLINE void Mix_Mono(int& mix,int downsample)
	{
		register int i;
		for(i = 0; i < 8; i++)
		{
			mix += (nMixL[i] / downsample);
			nMixL[i] = 0;
		}
	}
	
	FORCEINLINE void Mix_Stereo(int& mixL,int& mixR,int downsample)
	{
		register int i;
		for(i = 0; i < 8; i++)
		{
			mixL += (nMixL[i] / downsample);
			mixR += (nMixR[i] / downsample);
			nMixL[i] = nMixR[i] = 0;
		}
	}
};