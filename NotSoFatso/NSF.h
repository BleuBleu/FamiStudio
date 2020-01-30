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
//  NSF.h
//
//

// I made pretty much every file include this header... kinda sloppy.
//  feel free to change it if you don't like the compile time.

#define WIN32_LEAN_AND_MEAN
#include <Windows.h>
#include <stdio.h>
#include "resource.h"

#include "DFC.h"

#include "NSF_File.h"
#include "NSF_Core.h"

class CNSF;

#include "AboutDlg.h"
#include "ConfigDlg.h"
#include "ChannelsDlg.h"
#include "VRC6Dlg.h"
#include "MMC5Dlg.h"
#include "N106Dlg.h"
#include "VRC7Dlg.h"
#include "FME07Dlg.h"
#include "PlayControlDlg.h"
#include "MainControlDlg.h"
#include "MiniPlayControlDlg.h"

#include "PlaylistDlg.h"
#include "TrackInfoDlg.h"
#include "GeneralFileInfoDlg.h"
#include "TagInfoDlg.h"
#include "FileInfoDlg.h"

#include "Winamp.h"

/*
 *	I tried my best to comment everything so that it's understandable.  Be sure
 *		to check out the readmes for more information
 *
 *	-Disch-
 */

class CNSF
{
public:
	CNSF() { }
	~CNSF() { }

	// Winamp functions
	void	Config(HWND hwndParent);
	void	About(HWND hwndParent);
	void	Init();
	void	Quit();
	void	GetFileInfo(char *file, char *title, int *length_in_ms);
	int		InfoBox(char *file, HWND hwndParent);
	int		IsOurFile(char *fn);
	int		Play(char *fn);
	void	Pause(BYTE pause);
	int		IsPaused();
	void	Stop();
	int		GetLength();
	int		GetOutputTime();
	void	SetOutputTime(int time_in_ms);
	void	SetVolume(int volume);
	void	SetPan(int pan);

	void	PlayThread();

	// Other functions
	int		InitGUI();
	void	ResetFade(BYTE loop);			//called when the loop forever option is toggled.. this resets the core's fading
	void	SetTrack(BYTE track);			//handles all the preparation for switching tracks.  Called WHENEVER the NSF track is set/changed
	BYTE	NextTrack();					//returns 0 if there's no more tracks, 1 otherwise
	BYTE	PrevTrack();
	BYTE	GetNSFType(char** fn,BYTE* shadow,BYTE* track);	//picks out the filename from NSF Shadow strings
	BYTE	FindFirstTrack(CNSFFile* file);					//Gets the first track from a file
	void	GetTrackTimes(CNSFFile* file,BYTE track,UINT* song,UINT* fade,BYTE* isdefault);	//pulls the length/fade times for the desired track

	void	LoadConfiguration();			//called once at bootup
	void	SaveConfiguration();			//called once at shutdown

	CDString	GetGameTitle(CNSFFile* file,BYTE replaceamp);		//quick functions for grabbing the NSF strings
	CDString	GetArtist(CNSFFile* file,BYTE replaceamp);
	CDString	GetCopyright(CNSFFile* file,BYTE replaceamp);
	CDString	GetRipper(CNSFFile* file,BYTE replaceamp);
	CDString	GetTrackLabel(CNSFFile* file,BYTE track,BYTE replaceamp);
	CDString	GetExpansionString(CNSFFile* file);			//"none" if no expansion, otherwise "[VRC6]", "[MMC5]", etc
	CDString	GetNTSCPALString(CNSFFile* file);


	void	SetAlwaysOnTop(BYTE alwaysontop);
	void	SetMiniAlwaysOnTop(BYTE alwaysontop);

	// Winamp Module
	In_Module*		inMod;					//interfacing with Winamp
	Out_Module*		outMod;

	// NSF stuff
	CNSFCore		nsfCore;				//The Core!  Does ALL the sound emulation... see it for further info
	CNSFFile		nsfFile;				//Currently loaded file... contains track lengths and other crap

	// communication stuff
	BYTE			bIsPaused;				//just to make Winamp happy (used in the IsPaused func).  Otherwise not used.

	// thread stuff (inter-thread communication)
	volatile BYTE	bIsThreadOpen;			//1 if the thread is open (sound playing), 0 otherwise
	volatile BYTE	bSignalThreadClose;		//we want the thread closed!
	volatile UINT	nSeekTime;				//-1 (0xFFFFFFFF) if we don't need to seek, otherwise the desired time in MS to seek to (the seeking is done IN the play thread)

	// sound output stuff
	int				nBlockShift;			//0 = 1 byte/sample, 1 = 2 bytes/sample, 2 = 4 bytes/sample.  Used for some stupid math Winamp makes you do
	int				nMaxLatency;			//Max latency of the output plugin.  Recorded so I can re-init the vis at will.

	int				nSampleRate;			//Samples per sec (96000 is the only way to live)
	int				nChannels;				//1 or 2

	// tracking stuff
	BYTE			bIsShadow;				//are we playing an NSF shadow?
	BYTE			nShadowTrack;			//if so... what track is it playing?
	BYTE			nCurrentTrack;			//what track are we playing now?
	int				nPlaylistIndex;			//where are we in the NSFE playlist?

	UINT			nCurrentTrackLength;	//how long is this track (in ms)?
	UINT			nCurrentTrackFade;		//how long is the fade time? (in ms)?
	BYTE			bDefaultTime;			//was this track using the default Time?

	CDString		szLoadedPath;			//the pathname of the currently loaded file

	// options
	//  -NOTE NOTE-
	//  A lot of this shit ISN'T used.  I originally planned to have CNSF take care of everything... but it turned
	//  out that I had the GUI dialogs feed the info STRAIGHT to the nsf core.  I should probably clean all this up
	//  to get rid of the unused stuff

	BYTE			bUsePlaylists;		//This IS used... a LOT... keep it here.  This is basically the status of the "Disable NSFE Playlists" checkbox
	UINT			nDefaultFadeLength;
	UINT			nDefaultSongLength;
	float			fBasedPlaysPerSec;
	float			fMasterVolume;
	BYTE			bUseDefaultSpeed;
	float			fSpeedThrottle;
	BYTE			bLoopForever;
	CDString		szFormatString;
	CDString		szSingleFormatString;
	CDString		szShadowFormatString;
	BYTE			bOpenPlay_Normal;
	BYTE			bOpenPlay_Single;
	BYTE			bOpenPlay_Shadow;
	BYTE			bClingToWinamp;
	BYTE			bAlwaysOnTop;
	BYTE			bUseMiniPlayControl;
	BYTE			bMiniAlwaysOnTop;
	BYTE			bIgnoreNSFVersion;

	// Hooking Winamp
	HWND			wndWinampPL;
	HWND			wndWinampEQ;
	void*			pTrueWinampWndProc;
	void*			pTrueWinampPLWndProc;
	void*			pTrueWinampEQWndProc;

	// GUI
	HWND			hDummyWindow;
	BYTE			bGUIInited;			//has the GUI been inited?
	CMainControlDlg	mControlDlg;		//The GUI parent dialog
	CMiniPlayControlDlg mMiniPlayDlg;	//The mini play control Dlg
};