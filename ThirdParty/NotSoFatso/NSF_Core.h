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
//  NSF_Core.h
//

typedef unsigned char  BYTE;
typedef unsigned short WORD;
typedef unsigned int   UINT;
typedef const char*    LPCSTR;
typedef long long      INT64;

#ifdef _MSC_VER
#define FORCEINLINE __forceinline
#define FASTCALL __fastcall
#define CDECL __cdecl
#else
#define FORCEINLINE inline
#define FASTCALL
#ifdef LINUX
	#define CDECL
#else
	#define CDECL __cdecl
#endif
#endif

union TWIN
{
	WORD						W;
	struct{ BYTE l; BYTE h; }	B;
};

union QUAD
{
	UINT								D;
	struct{ BYTE l; BYTE h; WORD w; }	B;
};

struct NSF_ADVANCEDOPTIONS
{
	int			nSilenceTrackMS;
	int			nInvertCutoffHz;
	BYTE		bDMCPopReducer;
	BYTE		nForce4017Write;
	BYTE		bN106PopReducer;
	BYTE		bFDSPopReducer;

	BYTE		bIgnore4011Writes;
	BYTE		bIgnoreBRK;
	BYTE		bIgnoreIllegalOps;
	BYTE		bNoWaitForReturn;

	BYTE		bPALPreference;
	BYTE		bCleanAXY;
	BYTE		bResetDuty;
	BYTE		bNoSilenceIfTime;

	BYTE		bHighPassEnabled;
	BYTE		bLowPassEnabled;
	BYTE		bPrePassEnabled;
	int			nHighPassBase;
	int			nLowPassBase;
	int			nPrePassBase;
};

#define NTSC_FREQUENCY			 1789772.727273f
#define PAL_FREQUENCY			 1652097.692308f
#define NTSC_NMIRATE			      60.098814f
#define PAL_NMIRATE				      50.006982f

#define NES_FREQUENCY				21477270
#define NTSC_FRAME_COUNTER_FREQ		(NTSC_FREQUENCY / (NES_FREQUENCY / 89490.0f))
#define PAL_FRAME_COUNTER_FREQ		(PAL_FREQUENCY / (NES_FREQUENCY / 89490.0f))


#define EXTSOUND_VRC6			0x01
#define EXTSOUND_VRC7			0x02
#define EXTSOUND_FDS			0x04
#define EXTSOUND_MMC5			0x08
#define EXTSOUND_N106			0x10
#define EXTSOUND_FME07			0x20
#define EXTSOUND_EPSM			0x80

#define CHANNEL_SQUARE1          0
#define CHANNEL_SQUARE2          1
#define CHANNEL_TRIANGLE         2
#define CHANNEL_NOISE            3
#define CHANNEL_DPCM             4
#define CHANNEL_VRC6SQUARE1      5
#define CHANNEL_VRC6SQUARE2      6
#define CHANNEL_VRC6SAW          7
#define CHANNEL_VRC7FM1          8
#define CHANNEL_VRC7FM2          9
#define CHANNEL_VRC7FM3          10
#define CHANNEL_VRC7FM4          11
#define CHANNEL_VRC7FM5          12
#define CHANNEL_VRC7FM6          13
#define CHANNEL_FDS              14
#define MMC5_SQUARE1             15
#define MMC5_SQUARE2             16
#define MMC5_DPCM                17
#define N163_WAVE1               18
#define N163_WAVE2               19
#define N163_WAVE3               20
#define N163_WAVE4               21
#define N163_WAVE5               22
#define N163_WAVE6               23
#define N163_WAVE7               24
#define N163_WAVE8               25
#define S5B_SQUARE1              26
#define S5B_SQUARE2              27
#define S5B_SQUARE3              28
#define EPSM_SQUARE1             29
#define EPSM_SQUARE2             30
#define EPSM_SQUARE3             31
#define EPSM_FM1                 32
#define EPSM_FM2                 33
#define EPSM_FM3                 34
#define EPSM_FM4                 35
#define EPSM_FM5                 36
#define EPSM_FM6                 37
#define EPSM_RYTHM1              38
#define EPSM_RYTHM2              39
#define EPSM_RYTHM3              40
#define EPSM_RYTHM4              41
#define EPSM_RYTHM5              42
#define EPSM_RYTHM6              43
							     
#define STATE_VOLUME             0
#define STATE_PERIOD             1
#define STATE_DUTYCYCLE          2
#define STATE_DPCMSAMPLELENGTH   3
#define STATE_DPCMSAMPLEADDR     4
#define STATE_DPCMSAMPLEDATA     5
#define STATE_DPCMLOOP           6
#define STATE_DPCMPITCH          7
#define STATE_DPCMCOUNTER        8
#define STATE_DPCMACTIVE         9
#define STATE_FDSWAVETABLE       10
#define STATE_FDSMODULATIONTABLE 11
#define STATE_FDSMODULATIONDEPTH 12
#define STATE_FDSMODULATIONSPEED 13
#define STATE_FDSMASTERVOLUME    14
#define STATE_VRC7PATCH          15
#define STATE_FMPATCHREG         16
#define STATE_FMOCTAVE           17
#define STATE_FMTRIGGER          18
#define STATE_FMTRIGGERCHANGE    19
#define STATE_FMSUSTAIN          20
#define STATE_N163WAVEPOS        21
#define STATE_N163WAVESIZE       22
#define STATE_N163WAVE           23
#define STATE_N163NUMCHANNELS    24
#define STATE_S5BMIXER           25
#define STATE_S5BNOISEFREQUENCY  26
#define STATE_S5BENVFREQUENCY    27
#define STATE_S5BENVSHAPE        28
#define STATE_S5BENVTRIGGER      29
#define STATE_S5BENVENABLED      30
#define STATE_STEREO             31

#include <math.h>

#define min(a, b) ((a) < (b) ? (a) : (b))
#define max(a, b) ((a) > (b) ? (a) : (b))
#define ZeroMemory(p, s) memset(p, 0, s)

#include "Wave_Square.h"
#include "Wave_TND.h"

#include "Wave_VRC6.h"
#include "Wave_MMC5.h"
#include "Wave_N106.h"
#include "Wave_FME07.h"
#include "Wave_EPSM.h"
#include "Wave_FDS.h"

class CNSFCore;
class CNSFFile;

typedef BYTE (FASTCALL CNSFCore::*ReadProc)(WORD);
typedef void (FASTCALL CNSFCore::*WriteProc)(WORD,BYTE);
typedef void (CDECL* ApuRegWriteCallback)(int addr, int data);

class CNSFCore
{
public:
	//
	//	Construction / Destruction
	//
			CNSFCore();
			~CNSFCore() { Destroy(); }
	int		Initialize();				//1 = initialized ok, 0 = couldn't initialize (memory allocation error)
	void	Destroy();					//cleans up memory

	//
	//	Song Loading
	//
	int		LoadNSF(const CNSFFile* file);	//grab data from an existing file  1 = loaded ok, 0 = error loading

	//
	//	Track Control
	//
	void	SetTrack(BYTE track);			//Change tracks

	//
	//	Getting Samples
	//
	int		GetSamples(BYTE* buffer, int buffersize);	//fill a buffer with samples
	int		RunOneFrame();

	//
	//  FamiStudio Stuff
	//
	int		GetState(int channel, int state, int sub);
	void	ResetFrameState();
	void	SetApuWriteCallback(ApuRegWriteCallback callback);

	//
	//	Playback options
	//
	int		SetPlaybackOptions(int samplerate,int channels);				//Set desired playback options (0 = bad options couldn't be set)
	void	SetPlaybackSpeed(float playspersec);							//Speed throttling (0 = uses NSF specified speed)
	void	SetMasterVolume(float vol);										//1 = full volume, 2 = double volume, .5 = half volume, etc
	void	SetChannelOptions(UINT chan,int mix,int vol,int pan,int inv);	//chan is the zero based index of the channel to change.
	void	SetAdvancedOptions(const NSF_ADVANCEDOPTIONS* opt);				//misc options

	float	GetPlaybackSpeed();
	float	GetMasterVolume();
	void	GetAdvancedOptions(NSF_ADVANCEDOPTIONS* opt);

	//
	//	Seeking
	//
	float	GetPlayCalls();								//gets the number of 'play' routine calls executed
	UINT	GetWrittenTime(float basedplayspersec = 0);	//gets the output time (based on the given play rate, if basedplayspersec is zero, current playback speed is used
	void	SetPlayCalls(float plays);					//sets the number os 'plays' routines executed (for precise seeking)
	void	SetWrittenTime(UINT ms,float basedplays = 0);	//sets the written time (approx. seeking)

	//
	//	Fading
	//

	void	StopFade();									//stops all fading (plays indefinitely)
	BYTE	SongCompleted();							//song has faded out (samples have stopped being generated)
	void	SetFade(int fadestart,int fadestop,BYTE bNotDefault);						//parameters are play calls
	void	SetFadeTime(UINT fadestart,UINT fadestop,float basedplays,BYTE bNotDefault);//parameters are in milliseconds

protected:
	//
	//	Internal Functions
	//
	void	RebuildOutputTables(UINT chans);			//Rebuilds the output tables (when output options are changed.. like volume, pan, etc)
	void	CalculateChannelVolume(int maxvol,int& left, int& right, BYTE vol, char pan);	//used in above function
	void	RecalculateFade();							//called when fade status is changed.
	void	RecalcFilter();
	void	RecalcSilenceTracker();
	void	RecalculateInvertFreqs(int cutoff);

	void FORCEINLINE WaitForSamples() { /*while(bIsGeneratingSamples) sleep(5);*/ }

protected:
	/*
	 *	Memory Read/Write routines
	 */
	BYTE FASTCALL		ReadMemory_RAM(WORD a)				{ return pRAM[a & 0x07FF]; }
	BYTE FASTCALL		ReadMemory_ExRAM(WORD a)			{ nPlayCalled |= (a == 0x5007); return pExRAM[a & 0x0FFF]; }
	BYTE FASTCALL		ReadMemory_SRAM(WORD a)				{ return pSRAM[a & 0x1FFF]; }
	BYTE FASTCALL		ReadMemory_pAPU(WORD a);
	BYTE FASTCALL		ReadMemory_ROM(WORD a)				{ return pROM[(a >> 12) - 6][a & 0x0FFF]; }
	BYTE FASTCALL		ReadMemory_Default(WORD a)			{ return (a >> 8); }

	BYTE FASTCALL		ReadMemory_N106(WORD a);


	void FASTCALL		WriteMemory_RAM(WORD a,BYTE v)		{ pRAM[a & 0x07FF] = v; }
	void FASTCALL		WriteMemory_ExRAM(WORD a,BYTE v);
	void FASTCALL		WriteMemory_SRAM(WORD a,BYTE v)		{ pSRAM[a & 0x1FFF] = v; }
	void FASTCALL		WriteMemory_pAPU(WORD a,BYTE v);
	void FASTCALL		WriteMemory_FDSRAM(WORD a,BYTE v)	{ if (a >= 0x6000 && (a < 0x8000 || nExternalSound == EXTSOUND_FDS)) pROM[(a >> 12) - 6][a & 0x0FFF] = v; } // Dont allow changing the ROM if multiple expansions, i dont care.
	void FASTCALL		WriteMemory_Default(WORD a,BYTE v)	{ }

	void FASTCALL		WriteMemory_VRC6(WORD a,BYTE v);
	void FASTCALL		WriteMemory_MMC5(WORD a,BYTE v);
	void FASTCALL		WriteMemory_N106(WORD a,BYTE v);
	void FASTCALL		WriteMemory_VRC7(WORD a,BYTE v);
	void FASTCALL		WriteMemory_FME07(WORD a,BYTE v);
	void FASTCALL		WriteMemory_EPSM(WORD a, BYTE v);

	/*
	 *	Emulation
	 */
	void				EmulateAPU(BYTE bBurnCPUCycles);
	UINT				Emulate6502(UINT runto);


protected:
	
	//
	//	Initialization flags
	//
	BYTE			bMemoryOK;					//did memory get allocated ok?
	BYTE			bFileLoaded;				//is a file loaded?
	BYTE			bTrackSelected;				//did they select a track?
	volatile BYTE	bIsGeneratingSamples;		//currently generating samples... if set it prevents vital stats from changing (thread safe)

	/*
	 *	Memory
	 */
	BYTE*		pRAM;			//RAM:		0x0000 - 0x07FF
	BYTE*		pSRAM;			//SRAM:		0x6000 - 0x7FFF (non-FDS only)
	BYTE*		pExRAM;			//ExRAM:	0x5C00 - 0x5FF5 (MMC5 only)
								// Also holds NSF player info (at 0x5000 - 0x500F)
	BYTE*		pROM_Full;		//Full ROM buffer

	BYTE*		pROM[10];		//ROM banks (point to areas in pROM_Full)
								//0x8000 - 0xFFFF
								//also includes 0x6000 - 0x7FFF (FDS only)
	BYTE*		pStack;			//the stack (points to areas in pRAM)
								//0x0100 - 0x01FF

	int			nROMSize;		//size of this ROM file in bytes
	int			nROMBankCount;	//max number of 4k banks
	int			nROMMaxSize;	//size of allocated pROM_Full buffer

	/*
	 *	Memory Proc Pointers
	 */

	ReadProc	ReadMemory[0x10];
	WriteProc	WriteMemory[0x10];
	
	/*
	 *	6502 Registers / Mode
	 */

	BYTE		regA;			// Accumulator
	BYTE		regX;			// X-Index
	BYTE		regY;			// Y-Index
	BYTE		regP;			// Processor Status
	BYTE		regSP;			// Stack Pointer
	WORD		regPC;			// Program Counter

	BYTE		bPALMode;		// 1 if in PAL emulation mode, 0 if in NTSC
	BYTE		bCPUJammed;		// 0 = not jammed.  1 = really jammed.  2 = 'fake' jammed
								//  fake jam caused by the NSF code to signal the end of the play/init routine


	BYTE		nMultIn_Low;	//Multiplication Register, for MMC5 chip only (5205+5206)
	BYTE		nMultIn_High;

	BYTE        nPlayCalled;

	/*
	 *	NSF Preparation Information
	 */

	BYTE		nBankswitchInitValues[10];	//banks to swap to on tune init
	WORD		nPlayAddress;				// Play routine address
	WORD		nInitAddress;				// Init routine address

	BYTE		nExternalSound;				// external sound chips
	BYTE		nCurTrack;

	float		fNSFPlaybackSpeed;

	/*
	 *	pAPU
	 */

	BYTE		nFrameCounter;		//Frame Sequence Counter
	BYTE		nFrameCounterMax;	//Frame Sequence Counter Size (3 or 4 depending on $4017.7)
	BYTE		bFrameIRQEnabled;	//TRUE if frame IRQs are enabled
	BYTE		bFrameIRQPending;	//TRUE if the frame sequencer is holding down an IRQ

	BYTE		bChannelMix[24];	//Mixing flags for each channel (except the main 5!)
	BYTE		nChannelVol[29];	//Volume settings for each channel
	char		nChannelPan[29];	//Panning for each channel

	CSquareWaves	mWave_Squares;			//Square channels 1 and 2
	CTNDWaves		mWave_TND;				//Triangle/Noise/DMC channels
	CVRC6PulseWave	mWave_VRC6Pulse[2];
	CVRC6SawWave	mWave_VRC6Saw;
	CMMC5SquareWave	mWave_MMC5Square[2];
	CMMC5VoiceWave	mWave_MMC5Voice;
	CN106Wave		mWave_N106;
	CFDSWave		mWave_FDS;
	
	BYTE			nFME07_Address;
	CFME07Wave		mWave_FME07[3];			//FME-07's 3 pulse channels

	BYTE			nEPSM_Address;
	EPSMWave		mWave_EPSM[15];			//FME-07's 3 pulse channels

	/*
	 *	VRC7 stuffs
	 */

	BYTE		nVRC7Address;			//address written to the VRC7 port
	BYTE*		pVRC7Buffer;			//pointer to the position to write VRC7 samples
	void*		pFMOPL;
	BYTE		VRC7Chan[3][6];
	char		VRC7Triggered[6];       // 0 = nothing, 1 = triggered, -1 = released.
	BYTE		bVRC7_FadeChanged;
	BYTE		bVRC7Inv[6];

	void		VRC7_Init();
	void		VRC7_Reset();
	void		VRC7_Mix();
	void		VRC7_Write(BYTE val);
	void		VRC7_Destroy();
	void		VRC7_LoadInstrument(BYTE Chan);
	void		VRC7_RecalcMultiplier(BYTE chan);
	void		VRC7_ChangeInversion(BYTE chan,BYTE inv);
	void		VRC7_ChangeInversionFreq();

	/*
	 *	Timing and Counters
	 */
	float		fTicksUntilNextFrame;	//clock cycles until the next frame

	float		fTicksPerPlay;			//clock cycles between play calls
	float		fTicksUntilNextPlay;	//clock cycles until the next play call

	float		fTicksPerSample;		//clock cycles between generated samples
	float		fTicksUntilNextSample;	//clocks until the next sample

	UINT		nCPUCycle;
	UINT		nAPUCycle;
	UINT		nTotalPlays;			//number of times the play subroutine has been called (for tracking output time)


	/*
	 *	Silence Tracker
	 */
	int			nSilentSamples;
	int			nSilentSampleMax;
	int			nSilenceTrackMS;
	BYTE		bNoSilenceIfTime;
	BYTE		bTimeNotDefault;

	//
	//	Sound output options
	//
	int				nSampleRate;
	int				nMonoStereo;

	//
	//	Volume/fading/filter tracking
	//

	float			fMasterVolume;

	UINT			nStartFade;					//play call to start fading out
	UINT			nEndFade;					//play call to stop fading out (song is over)
	BYTE			bFade;						//are we fading?
	float			fFadeVolume;
	float			fFadeChange;

	/*
	 *	Designated Output Buffer
	 */
	BYTE*		pOutput;
	int			nDownsample;

	/*
	 *	Misc Options
	 */
	BYTE		bDMCPopReducer;					//1 = enabled, 0 = disabled
	BYTE		nDMCPop_Prev;
	BYTE		bDMCPop_Skip;
	BYTE		bDMCPop_SamePlay;
	BYTE		bDMCTriggered;

	BYTE		nForce4017Write;
	BYTE		bN106PopReducer;
	int			nInvertCutoffHz;
	BYTE		bIgnore4011Writes;
	
	BYTE		bIgnoreBRK;
	BYTE		bIgnoreIllegalOps;
	BYTE		bNoWaitForReturn;
	BYTE		bPALPreference;
	BYTE		bCleanAXY;
	BYTE		bResetDuty;

	/*
	 *	Sound Filter
	 */

	INT64		nFilterAccL;
	INT64		nFilterAccR;
	INT64		nFilterAcc2L;
	INT64		nFilterAcc2R;
	INT64		nHighPass;
	INT64		nLowPass;

	int			nHighPassBase;
	int			nLowPassBase;

	BYTE		bHighPassEnabled;
	BYTE		bLowPassEnabled;
	BYTE		bPrePassEnabled;

	int			nSmPrevL;
	int			nSmAccL;
	int			nSmPrevR;
	int			nSmAccR;
	int			nPrePassBase;
	float		fSmDiv;

	// 
	// FamiStudio stuff
	// 
	ApuRegWriteCallback apuRegWriteCallback;
};
