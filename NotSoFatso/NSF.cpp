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
//  NSF.cpp
//
//

#include "NSF.h"


//////////////////////////////////////////////////////////////////////////
//   Thread Entry
DWORD CALLBACK LaunchPlayThread(void* arg)
{
	((CNSF*)arg)->PlayThread();
	return 0;
}

//////////////////////////////////////////////////////////////////////////
//   WINAMP ROUTINES
//

void CNSF::Config(HWND hwndParent)
{
	if(!InitGUI())	return;

	mControlDlg.OpenAsConfig();
}

void CNSF::About(HWND hwndParent)
{
	if(!InitGUI())	return;

	mControlDlg.OpenAsAbout();
}

int CNSF::InitGUI()
{
	if(bGUIInited)		return 1;

	WNDCLASSEX wc;
	ZeroMemory(&wc,sizeof(WNDCLASSEX));
	wc.cbSize = sizeof(WNDCLASSEX);
	wc.hInstance = inMod->hDllInstance;
	wc.lpfnWndProc = (WNDPROC)DefWindowProc;
	wc.lpszClassName = "NotSo Fatso";

	if(!RegisterClassEx(&wc))
		return 0;

	hDummyWindow = CreateWindowEx(0,"NotSo Fatso","",WS_POPUP,0,0,0,0,NULL,NULL,inMod->hDllInstance,NULL);
	if(!hDummyWindow)
		return 0;

	
	mControlDlg.Build(hDummyWindow);
	mMiniPlayDlg.DoModeless(inMod->hDllInstance,hDummyWindow,IDD_MINIPLAY);
	bGUIInited = 1;

	LoadConfiguration();

	return 1;
}


void CNSF::Init()
{
#ifdef _DEBUG
	MessageBox(NULL,"NotSo Fatso in Debug mode!!","NotSo Fatso",MB_OK);
//#else
//	MessageBox(NULL,"NotSo Fatso WIP Build.  Distributed for a handful on IRC only.\nDo not distribute plzkthx.","NotSo Fatso",MB_OK);
#endif

	inMod = winampGetInModule2();

	//init gui
	mControlDlg.m_config.pNSF = this;
	mControlDlg.m_config2.pNSF = this;
	mControlDlg.m_config3.pNSF = this;
	mControlDlg.m_channels.pNSF = this;
	mControlDlg.m_vrc6.pNSF = this;
	mControlDlg.m_mmc5.pNSF = this;
	mControlDlg.m_n106.pNSF = this;
	mControlDlg.m_vrc7.pNSF = this;
	mControlDlg.m_fme07.pNSF = this;
	mControlDlg.m_playcontrol_slide.pNSF = this;
	mControlDlg.pNSF = this;
	mControlDlg.hInst = inMod->hDllInstance;
	mMiniPlayDlg.pNSF = this;
	bGUIInited = 0;

	bIsPaused = 0;
	bSignalThreadClose = 0;
	bIsThreadOpen = 0;
	nSeekTime = 0xFFFFFFFF;

	wndWinampPL = NULL;
	wndWinampEQ = NULL;
	pTrueWinampWndProc = NULL;
	pTrueWinampPLWndProc = NULL;
	pTrueWinampEQWndProc = NULL;
}

void CNSF::Quit()
{
	SaveConfiguration();
	mControlDlg.UnBuild();
	mMiniPlayDlg.Destroy();
	nsfFile.Destroy();
	nsfCore.Destroy();

	if(bGUIInited)
		DestroyWindow(hDummyWindow);

	bGUIInited = 0;
}

void CNSF::GetFileInfo(char *file, char *title, int *length_in_ms)
{
	if(!InitGUI())	return;

	CNSFFile* fl;
	CNSFFile  _fl;
	BYTE shadow;
	BYTE track;

	CDString temp;
	CDString str;
	int i, j;

	mControlDlg.m_config2.GetFormatStrings(szFormatString,szSingleFormatString,szShadowFormatString);
	mControlDlg.m_config.GetDefaultTimes(nDefaultSongLength,nDefaultFadeLength);

	if( file && *file )
	{
		if(!GetNSFType(&file,&shadow,&track))		return;
		if(_fl.LoadFile(file,0,bIgnoreNSFVersion))	return;
		fl = &_fl;
		if(!shadow)
			track = FindFirstTrack(fl);
	}
	else
	{
		fl = &nsfFile;
		shadow = bIsShadow;
		track = nCurrentTrack;
	}

	if(shadow)	str = szShadowFormatString;
	else
	{
		if(bUsePlaylists && fl->pPlaylist)
		{
			if(fl->nPlaylistSize > 1)	str = szFormatString;
			else						str = szSingleFormatString;
		}
		else
		{
			if(fl->nTrackCount > 1)		str = szFormatString;
			else						str = szSingleFormatString;
		}
	}

	/*
	 *	Fill in the file's display name
	 */
	// %g -> Game Title
	str.Replace("%g",GetGameTitle(fl,0));

	// %a -> Artist
	str.Replace("%a",GetArtist(fl,0));

	// %c -> Copyright
	str.Replace("%c",GetCopyright(fl,0));

	// %r -> Ripper
	str.Replace("%r",GetRipper(fl,0));

	// %l -> Track Label
	str.Replace("%l",GetTrackLabel(fl,track,0));

	// %f -> File name
	if(fl == &nsfFile)	temp = szLoadedPath;
	else				temp = file;
	i = temp.ReverseFind('\\');
	if(i > -1)	temp = temp.Mid(i + 1);
	i = temp.ReverseFind('.');
	if(i > -1)	temp = temp.Left(i);

	str.Replace("%f",temp);

	// %t -> Current Track
	if(shadow)
		temp.Format("%d",track + 1);
	else
	{
		if(fl == &nsfFile)
		{
			if(bUsePlaylists && fl->pPlaylist)
				temp.Format("%d",nPlaylistIndex + 1);
			else
				temp.Format("%d",nCurrentTrack + 1);
		}
		else
		{
			if(bUsePlaylists && fl->pPlaylist)
				temp = "1";
			else
				temp.Format("%d",track + 1);
		}
	}
	str.Replace("%t",temp);

	// %o -> Track total
	if(shadow)
		i = fl->nTrackCount;
	else
	{
		if(bUsePlaylists && fl->pPlaylist)	i = fl->nPlaylistSize;
		else								i = fl->nTrackCount;
	}
	temp.Format("%d",i);
	str.Replace("%o",temp);

	lstrcpy(title,str);

	/*
	 *	Fill in the total time
	 */

	//song lengths
	if(shadow)
	{
		GetTrackTimes(fl,track,(UINT*)&i,(UINT*)&j,NULL);
		*length_in_ms = i + j;
	}
	else
	{
		j = 0;
		if(bUsePlaylists && fl->pPlaylist && (fl->nPlaylistSize > 0))
		{
			if(fl->pTrackTime)
			{
				for(i = 0; i < fl->nPlaylistSize; i++)
				{
					if(i >= fl->nTrackCount)	j += nDefaultSongLength;
					else
						j += ((fl->pTrackTime[fl->pPlaylist[i]] < 0) ? nDefaultSongLength : fl->pTrackTime[fl->pPlaylist[i]]);
				}
			}
			else
				j = nDefaultSongLength * fl->nPlaylistSize;

			if(fl->pTrackFade)
			{
				for(i = 0; i < fl->nPlaylistSize; i++)
				{
					if(i >= fl->nTrackCount)	j += nDefaultFadeLength;
					else
						j += ((fl->pTrackFade[fl->pPlaylist[i]] < 0) ? nDefaultFadeLength : fl->pTrackFade[fl->pPlaylist[i]]);
				}
			}
			else
				j += nDefaultFadeLength * fl->nPlaylistSize;
		}
		else
		{
			if(fl->pTrackTime)
			{
				for(i = 0; i < fl->nTrackCount; i++)
					j += ((fl->pTrackTime[i] < 0) ? nDefaultSongLength : fl->pTrackTime[i]);
			}
			else
				j = nDefaultSongLength * fl->nTrackCount;

			if(fl->pTrackFade)
			{
				for(i = 0; i < fl->nTrackCount; i++)
					j += ((fl->pTrackFade[i] < 0) ? nDefaultFadeLength : fl->pTrackFade[i]);
			}
			else
				j += nDefaultFadeLength * fl->nTrackCount;
		}
		*length_in_ms = j;
	}
}

int CNSF::InfoBox(char *file, HWND hwndParent)
{
	CNSFFile		fl;
	BYTE shadow = 0;
	BYTE track = 0;

	GetNSFType(&file,&shadow,&track);
	if(fl.LoadFile(file,1,bIgnoreNSFVersion))		return 1;

	CFileInfoDlg	dlg;
	dlg.Run(this,&fl,file,shadow,track,hwndParent);
	return 0;
}

int CNSF::IsOurFile(char *fn)
{
	BYTE shadow = 0;
	BYTE dummy;
/*
	if(!fn) return 0;
	
	int len = strlen(fn) - 1;

	while(len >= 0)
	{
		if(fn[len] == '.')
			break;
		len--;
	}

	if(len >= 0)
	{
		if(!strcmpi(&fn[len + 1],"nsf"))	return 1;
		if(!strcmpi(&fn[len + 1],"nsfe"))	return 1;
	}*/

	if(!GetNSFType(&fn,&shadow,&dummy))	return 0;

	return shadow;
}

/*
 *	This section is for hooking procedures... taken from NezPlug.
 */

typedef struct
{
	DWORD dwThreadId;
	LPCSTR lpszClassName;
	HWND hwndRet;
} FINDTHREADWINDOW_WORK;

static BOOL CALLBACK FindThreadWindowsProc(HWND hwnd, LPARAM lParam)
{
	FINDTHREADWINDOW_WORK *pWork = (FINDTHREADWINDOW_WORK *)lParam;
	if (GetWindowThreadProcessId(hwnd, NULL) == pWork->dwThreadId)
	{
#define MAX_CLASS_NAME MAX_PATH
		CHAR szClassName[MAX_CLASS_NAME];
		if (GetClassName(hwnd, szClassName, MAX_CLASS_NAME))
		{
			if (lstrcmp(szClassName, pWork->lpszClassName) == 0)
			{
				pWork->hwndRet = hwnd;
				return FALSE;
			}
		}
	}
	return TRUE;
}

static HWND FindThreadWindow(LPCSTR lpszClassName, DWORD dwThreadId)
{
	FINDTHREADWINDOW_WORK fwww;
	fwww.dwThreadId = dwThreadId;
	fwww.lpszClassName = lpszClassName;
	fwww.hwndRet = NULL;
	EnumWindows(FindThreadWindowsProc, (LONG)&fwww);
	return fwww.hwndRet;
}

/*
 *	Back to normal routines...
 */

int CNSF::Play(char *fn)
{
	if(!InitGUI())
		return 1;

	if(!(wndWinampPL && wndWinampEQ))
	{
		DWORD dwThreadId = GetWindowThreadProcessId(inMod->hMainWindow,NULL);
		wndWinampEQ = FindThreadWindow("Winamp EQ",dwThreadId);
		wndWinampPL = FindThreadWindow("Winamp PE",dwThreadId);
	}

	if(!pTrueWinampWndProc)			pTrueWinampWndProc   = (void*)GetWindowLong(inMod->hMainWindow,GWL_WNDPROC);
	if(!pTrueWinampPLWndProc)		pTrueWinampPLWndProc = (void*)GetWindowLong(wndWinampPL,GWL_WNDPROC);
	if(!pTrueWinampEQWndProc)		pTrueWinampEQWndProc = (void*)GetWindowLong(wndWinampEQ,GWL_WNDPROC);

	SetWindowLong(inMod->hMainWindow,GWL_WNDPROC,(LONG)Winamp_WndProc);
	SetWindowLong(wndWinampPL,GWL_WNDPROC,(LONG)Winamp_WndProc);
	SetWindowLong(wndWinampEQ,GWL_WNDPROC,(LONG)Winamp_WndProc);


	mControlDlg.m_config.GetDefaultTimes(nDefaultSongLength,nDefaultFadeLength);

	outMod = inMod->outMod;
	if(!outMod)	return 1;	//gotta have an output plugin
	
	if(!GetNSFType(&fn,&bIsShadow,&nShadowTrack))	return -1;
	
	if(nsfFile.LoadFile(fn,1,bIgnoreNSFVersion))	return -1;
	
	if(!nsfCore.Initialize())						return -1;

	mControlDlg.m_config.GetPlayMode(&nSampleRate,&nChannels);
	nBlockShift = nChannels;

	if(!nsfCore.SetPlaybackOptions(nSampleRate,nChannels))								return -1;
	if(!nsfCore.LoadNSF(&nsfFile))														return -1;

	if((nsfFile.nIsPal & 0x03) == 0x01)		fBasedPlaysPerSec = PAL_NMIRATE;
	else									fBasedPlaysPerSec = NTSC_NMIRATE;

	if(bUseDefaultSpeed)
		nsfCore.SetPlaybackSpeed(0);
	else
		nsfCore.SetPlaybackSpeed(fSpeedThrottle);

	if(bIsShadow)
		nCurrentTrack = nShadowTrack;
	else
		nCurrentTrack = FindFirstTrack(&nsfFile);
	nPlaylistIndex = 0;

	mControlDlg.m_playcontrol_slide.pFile = &nsfFile;
	mControlDlg.m_playcontrol_slide.bShadow = bIsShadow;
	mControlDlg.m_playcontrol_slide.LoadValues();

	mMiniPlayDlg.pFile = &nsfFile;
	mMiniPlayDlg.bShadow = bIsShadow;
	mMiniPlayDlg.LoadValues();

	DWORD tid;
	tid = 0;

	if(bIsShadow)
	{
		tid = bOpenPlay_Shadow;
	}
	else
	{
		if(bUsePlaylists && nsfFile.pPlaylist)
		{
			if(nsfFile.nPlaylistSize > 1)
				tid = bOpenPlay_Normal;
			else
				tid = bOpenPlay_Single;
		}
		else
		{
			if(nsfFile.nTrackCount > 1)
				tid = bOpenPlay_Normal;
			else
				tid = bOpenPlay_Single;
		}
	}

	if(tid)
	{
		if(bUseMiniPlayControl)	mMiniPlayDlg.Open();
		else					mControlDlg.OpenAsPlay();
	}

	inMod->SetInfo(nSampleRate * 16 * nChannels / 1000,nSampleRate / 1000,nChannels,1);
	
	bIsThreadOpen = 0;
	bSignalThreadClose = 0;
	SetTrack(nCurrentTrack);

	
	nMaxLatency = outMod->Open(nSampleRate,nChannels,16,0,0);
	if(nMaxLatency < 0)
		return 1;
	inMod->SAVSAInit(nMaxLatency,nSampleRate);
	
	bSignalThreadClose = 0;
	HANDLE hand = CreateThread(0,0,LaunchPlayThread,this,0,&tid);
	if(!hand)
		return 1;

	szLoadedPath = fn;

	return 0;
}

void CNSF::Stop()
{
	if(pTrueWinampWndProc)			SetWindowLong(inMod->hMainWindow,GWL_WNDPROC,(LONG)pTrueWinampWndProc);
	if(pTrueWinampPLWndProc)		SetWindowLong(wndWinampPL,GWL_WNDPROC,(LONG)pTrueWinampPLWndProc);
	if(pTrueWinampEQWndProc)		SetWindowLong(wndWinampEQ,GWL_WNDPROC,(LONG)pTrueWinampEQWndProc);

	pTrueWinampWndProc = NULL;
	pTrueWinampPLWndProc = NULL;
	pTrueWinampEQWndProc = NULL;

	mControlDlg.m_playcontrol_slide.pFile = NULL;
	mControlDlg.m_playcontrol_slide.LoadValues();
	mControlDlg.ClosePlay();
	mMiniPlayDlg.pFile = NULL;
	mMiniPlayDlg.LoadValues();
	mMiniPlayDlg.Close();
	bSignalThreadClose = 1;
	szLoadedPath = "";
	while(bIsThreadOpen)
		Sleep(50);
}

void CNSF::Pause(BYTE pause)
{
	bIsPaused = pause;
	inMod->outMod->Pause(pause);
}

int CNSF::IsPaused()
{
	return bIsPaused;
}

int CNSF::GetLength()
{
	register UINT song;
	register UINT fade;

	GetTrackTimes(&nsfFile,nCurrentTrack,&song,&fade,NULL);
	return song + fade;
}

int CNSF::GetOutputTime()
{
	register int dif = inMod->outMod->GetWrittenTime() - inMod->outMod->GetOutputTime();

	if(!fBasedPlaysPerSec)
		return nsfCore.GetWrittenTime(0) - dif;
	else
		return nsfCore.GetWrittenTime(fBasedPlaysPerSec) - (UINT)(dif * nsfCore.GetPlaybackSpeed() / fBasedPlaysPerSec);
}

void CNSF::SetOutputTime(int time_in_ms)
{
	nSeekTime = (unsigned)time_in_ms;
}

void CNSF::SetVolume(int volume)
{ inMod->outMod->SetVolume(volume); }

void CNSF::SetPan(int pan)
{ inMod->outMod->SetPan(pan); }

//////////////////////////////////////////////////////////////////////////
//  The Play Thread
//

void CNSF::PlayThread()
{
	bIsThreadOpen = 1;
	int buffersize, halfbuffersize;
	halfbuffersize = 576 << nBlockShift;
	buffersize = halfbuffersize << 1;

	BYTE* buffer = new BYTE[buffersize + 200];	//200 bytes padding, just in case of a little overflow
	if(!buffer)
		bSignalThreadClose = 1;

	int write, stamp;

	while(!bSignalThreadClose)
	{
		if(nSeekTime != 0xFFFFFFFF)
		{
			stamp = outMod->Pause(1);
			outMod->Flush(0);
			nsfCore.SetWrittenTime(nSeekTime,fBasedPlaysPerSec);
			outMod->Pause(stamp);
			nSeekTime = 0xFFFFFFFF;
		}

		if(nsfCore.SongCompleted())
		{
			if(outMod->IsPlaying())
				Sleep(50);
			else if(!NextTrack())
			{
				PostMessage(inMod->hMainWindow,WM_USER+2,0,0);	//song over
				bSignalThreadClose = 1;
			}
		}
		else
		{
			if(outMod->CanWrite() >= buffersize)
			{
				stamp = nsfCore.GetWrittenTime(fBasedPlaysPerSec);

				if(inMod->dsp_isactive())
				{
					write = nsfCore.GetSamples(buffer,halfbuffersize) >> nBlockShift;
					write = inMod->dsp_dosamples((short*)buffer,write,16,nChannels,nSampleRate) << nBlockShift;
				}
				else
					write = nsfCore.GetSamples(buffer,halfbuffersize);

				if(write >= halfbuffersize)
				{
					inMod->SAAddPCMData(buffer,nChannels,16,stamp);
					inMod->VSAAddPCMData(buffer,nChannels,16,stamp);
				}

				outMod->Write((char*)buffer,write);
			}
			else
				Sleep(50);
		}
	}

	inMod->SAVSADeInit();
	outMod->Close();
	if(buffer)
		delete[] buffer;
	bIsThreadOpen = 0;
}

//////////////////////////////////////////////////////////////////////////
//  Other shiznit

void CNSF::ResetFade(BYTE loop)
{
	bLoopForever = loop;
	if(loop)
		nsfCore.StopFade();
	else
		nsfCore.SetFadeTime(nCurrentTrackLength,nCurrentTrackFade,fBasedPlaysPerSec,!bDefaultTime);
}

void CNSF::SetTrack(BYTE track)
{	
	GetTrackTimes(&nsfFile,track,&nCurrentTrackLength,&nCurrentTrackFade,&bDefaultTime);
	nCurrentTrackFade += nCurrentTrackLength;

	inMod->SAVSADeInit();
	nsfCore.SetTrack(track);
	ResetFade(bLoopForever);
	nCurrentTrack = track;
	outMod->Flush(0);
	inMod->SAVSAInit(nMaxLatency,nSampleRate);

	if(inMod->hMainWindow)
		SendMessage(inMod->hMainWindow,WM_USER,0,243);	//refresh title

	mControlDlg.m_playcontrol_slide.UpdateTrack(1);
	mMiniPlayDlg.UpdateTrack(1);
}

BYTE CNSF::NextTrack()
{
	if(bIsShadow)	return 0;

	if(bUsePlaylists && nsfFile.pPlaylist)
	{
		nPlaylistIndex++;
		if(nPlaylistIndex >= nsfFile.nPlaylistSize)
		{
			nPlaylistIndex--;
			return 0;
		}
		nCurrentTrack = nsfFile.pPlaylist[nPlaylistIndex];
	}
	else
	{
		if((nCurrentTrack + 1) >= nsfFile.nTrackCount)
			return 0;
		nCurrentTrack++;
	}
	SetTrack(nCurrentTrack);
	return 1;
}

BYTE CNSF::PrevTrack()
{
	if(bIsShadow)	return 0;

	if(bUsePlaylists && nsfFile.pPlaylist)
	{
		if(nPlaylistIndex <= 0)	return 0;
		nPlaylistIndex--;
		nCurrentTrack = nsfFile.pPlaylist[nPlaylistIndex];
	}
	else
	{
		if(nCurrentTrack == 0)	return 0;
		nCurrentTrack--;
	}
	SetTrack(nCurrentTrack);
	return 1;
}

BYTE CNSF::GetNSFType(char** fn,BYTE* shadow,BYTE* track)
{
	*shadow = 0;
	*track = 0;

	char* str = *fn;
	if(lstrlen(str) >= 6)
	{
		int temp = str[6];
		str[6] = 0;
		if(!lstrcmp(str,"nsf://"))
		{
			str[6] = (char)temp;
			str += 6;

			int i = 0;
			while(str[i] && (str[i] != ':')) i++;
			if(!str[i])	return 0;

			str[i] = 0;
			sscanf(str,"%d",&temp);
			*track = (BYTE)temp;
			str[i] = ':';

			(*fn) = str + i + 1;

			*shadow = 1;
		}
		else
			str[6] = (char)temp;
	}
	return 1;
}

BYTE CNSF::FindFirstTrack(CNSFFile* file)
{
	if(bUsePlaylists && file->pPlaylist)
		return file->pPlaylist[0];
	else
		return file->nInitialTrack;
}

void CNSF::GetTrackTimes(CNSFFile* file,BYTE track,UINT* song,UINT* fade,BYTE* isdefault)
{
	*song = nDefaultSongLength;
	*fade = nDefaultFadeLength;
	if(isdefault)
		*isdefault = 1;
	if(track >= file->nTrackCount)
		return;

	if(file->pTrackTime)
	{
		if(file->pTrackTime[track] >= 0)
		{
			*song = file->pTrackTime[track];
			if(isdefault)
				*isdefault = 0;
		}
	}
	if(file->pTrackFade)
	{
		if(file->pTrackFade[track] >= 0)
			*fade = file->pTrackFade[track];
	}
}

CDString CNSF::GetGameTitle(CNSFFile* file,BYTE replaceamp)
{
	if(!file->szGameTitle)		return "<?>";
	if(!(*(file->szGameTitle)))	return "<?>";

	CDString temp = file->szGameTitle;
	if(replaceamp)	temp.Replace("&","&&");

	return temp;
}

CDString CNSF::GetArtist(CNSFFile* file,BYTE replaceamp)
{
	if(!file->szArtist)			return "<?>";
	if(!(*(file->szArtist)))	return "<?>";
	
	CDString temp = file->szArtist;
	if(replaceamp)	temp.Replace("&","&&");

	return temp;
}

CDString CNSF::GetCopyright(CNSFFile* file,BYTE replaceamp)
{
	if(!file->szCopyright)		return "<?>";
	if(!(*(file->szCopyright)))	return "<?>";
	
	CDString temp = file->szCopyright;
	if(replaceamp)	temp.Replace("&","&&");

	return temp;
}

CDString CNSF::GetRipper(CNSFFile* file,BYTE replaceamp)
{
	if(!file->szRipper)			return "<?>";
	if(!(*(file->szRipper)))	return "<?>";
	
	CDString temp = file->szRipper;
	if(replaceamp)	temp.Replace("&","&&");

	return temp;
}

CDString CNSF::GetExpansionString(CNSFFile* file)
{
	CDString ret;
	if(!file->nChipExtensions)
		return "None";

	if(file->nChipExtensions & 0x01)
		ret += "[VRC6] ";
	if(file->nChipExtensions & 0x02)
		ret += "[VRC7] ";
	if(file->nChipExtensions & 0x04)
		ret += "[FDS] ";
	if(file->nChipExtensions & 0x08)
		ret += "[MMC5] ";
	if(file->nChipExtensions & 0x10)
		ret += "[N106] ";
	if(file->nChipExtensions & 0x20)
		ret += "[FME-07] ";
	if(file->nChipExtensions & 0x40)
		ret += "[???] ";
	if(file->nChipExtensions & 0x80)
		ret += "[???] ";

	return ret;
}

CDString CNSF::GetNTSCPALString(CNSFFile* file)
{
	if(file->nIsPal & 2)
		return "NTSC / PAL";

	if(file->nIsPal & 1)
		return "PAL";

	return "NTSC";
}

CDString CNSF::GetTrackLabel(CNSFFile* file,BYTE track,BYTE replaceamp)
{
	CDString str;
	if(track >= file->nTrackCount)		goto exit;
	if(!file->szTrackLabels)			goto exit;
	if(!file->szTrackLabels[track])		goto exit;
	if(!file->szTrackLabels[track][0])	goto exit;

	str = file->szTrackLabels[track];
	if(replaceamp)		str.Replace("&","&&");
	return str;
exit:
	str.Format("Track %d",track + 1);
	return str;
}

void CNSF::SetAlwaysOnTop(BYTE alwaysontop)
{
	bAlwaysOnTop = alwaysontop;
	SetWindowPos(mControlDlg.GetHandle(),(bAlwaysOnTop ? HWND_TOPMOST : HWND_NOTOPMOST),0,0,0,0,
		SWP_NOSIZE | SWP_NOACTIVATE | SWP_NOMOVE);
}

void CNSF::SetMiniAlwaysOnTop(BYTE alwaysontop)
{
	bMiniAlwaysOnTop = alwaysontop;
	SetWindowPos(mMiniPlayDlg.GetHandle(),(bMiniAlwaysOnTop ? HWND_TOPMOST : HWND_NOTOPMOST),0,0,0,0,
		SWP_NOSIZE | SWP_NOACTIVATE | SWP_NOMOVE);
}


//////////////////////////////////////////////////////////////////////////
//  Configuration

void CNSF::LoadConfiguration()
{
	int i, vol, pan, mix, inv;
	char buffer[101];
	char secondbuffer[20];
	char filename[MAX_PATH + 1];
	NSF_ADVANCEDOPTIONS opt;

	buffer[100] = 0;

	GetModuleFileName(inMod->hDllInstance,filename,MAX_PATH);

	for(i = lstrlen(filename) - 1; filename[i] != '\\'; i--);
	filename[i + 1] = 0;
	lstrcat(filename,"plugin.ini");


	/*	Sample Rate	*/
	GetPrivateProfileString("NotSo Fatso","Sample Rate       ","44100",buffer,100,filename);
	sscanf(buffer,"%d",&nSampleRate);
	if(nSampleRate < 8000)	nSampleRate = 8000;
	if(nSampleRate > 96000)	nSampleRate = 96000;

	/*	Channels		*/
	GetPrivateProfileString("NotSo Fatso","Mono/Stereo       ","2",buffer,100,filename);
	sscanf(buffer,"%d",&nChannels);
	if(nChannels != 1)		nChannels = 2;

	/*	DMC pop reducer	*/
	GetPrivateProfileString("NotSo Fatso","DMC Pop Reducer","1",buffer,100,filename);
	sscanf(buffer,"%d",&i);
	opt.bDMCPopReducer = (BYTE)(i != 0);
	
	/*	FDS pop reducer	*/
	GetPrivateProfileString("NotSo Fatso","FDS Pop Reducer","1",buffer,100,filename);
	sscanf(buffer,"%d",&i);
	opt.bFDSPopReducer = (BYTE)(i != 0);
	
	/*	Force 4017 Write	*/
	GetPrivateProfileString("NotSo Fatso","Force 4017","0",buffer,100,filename);
	sscanf(buffer,"%d",&i);
	if((i < 0) || (i > 2)) i = 0;
	opt.nForce4017Write = (BYTE)i;
	
	/*	N106 pop reducer	*/
	GetPrivateProfileString("NotSo Fatso","N106 Pop Reducer","1",buffer,100,filename);
	sscanf(buffer,"%d",&i);
	opt.bN106PopReducer = (BYTE)(i != 0);

	/*	Ignore $4011 Writes	*/
	GetPrivateProfileString("NotSo Fatso","Ignore 4011","0",buffer,100,filename);
	sscanf(buffer,"%d",&i);
	opt.bIgnore4011Writes = (BYTE)(i != 0);
	
	/*	Ignore BRK	*/
	GetPrivateProfileString("NotSo Fatso","Ignore BRK","0",buffer,100,filename);
	sscanf(buffer,"%d",&i);
	opt.bIgnoreBRK = (BYTE)(i != 0);
	
	/*	Ignore Illegal	*/
	GetPrivateProfileString("NotSo Fatso","Ignore Illegal","0",buffer,100,filename);
	sscanf(buffer,"%d",&i);
	opt.bIgnoreIllegalOps = (BYTE)(i != 0);
	
	/*	No Wait for Return	*/
	GetPrivateProfileString("NotSo Fatso","No Wait","0",buffer,100,filename);
	sscanf(buffer,"%d",&i);
	opt.bNoWaitForReturn = (BYTE)(i != 0);
	
	/*	Clean AXY regs	*/
	GetPrivateProfileString("NotSo Fatso","Clean AXY","0",buffer,100,filename);
	sscanf(buffer,"%d",&i);
	opt.bCleanAXY = (BYTE)(i != 0);
	
	/*	Ignore NSF Version	*/
	GetPrivateProfileString("NotSo Fatso","Ignore NSF Version","1",buffer,100,filename);
	sscanf(buffer,"%d",&i);
	bIgnoreNSFVersion = (BYTE)(i != 0);
	
	/*	Reset Duty		*/
	GetPrivateProfileString("NotSo Fatso","Reset Duty","0",buffer,100,filename);
	sscanf(buffer,"%d",&i);
	opt.bResetDuty = (BYTE)(i != 0);
	
	/*	Prefer PAL	*/
	GetPrivateProfileString("NotSo Fatso","Prefer PAL","0",buffer,100,filename);
	sscanf(buffer,"%d",&i);
	opt.bPALPreference = (BYTE)(i != 0);

	/*	Filter		*/
	GetPrivateProfileString("NotSo Fatso","LowPass","40000",buffer,100,filename);
	sscanf(buffer,"%d",&opt.nLowPassBase);
	opt.bLowPassEnabled = (opt.nLowPassBase >= 0);
	if(opt.nLowPassBase < 0)		opt.nLowPassBase = -opt.nLowPassBase;
	if(opt.nLowPassBase < 8000)		opt.nLowPassBase = 8000;
	if(opt.nLowPassBase > 60000)	opt.nLowPassBase = 60000;
	
	GetPrivateProfileString("NotSo Fatso","HighPass","150",buffer,100,filename);
	sscanf(buffer,"%d",&opt.nHighPassBase);
	opt.bHighPassEnabled = (opt.nHighPassBase >= 0);
	if(opt.nHighPassBase < 0)		opt.nHighPassBase = -opt.nHighPassBase;
	if(opt.nHighPassBase < 50)		opt.nHighPassBase = 50;
	if(opt.nHighPassBase > 5000)	opt.nHighPassBase = 5000;

	GetPrivateProfileString("NotSo Fatso","PrePass","40",buffer,100,filename);
	sscanf(buffer,"%d",&opt.nPrePassBase);
	opt.bPrePassEnabled = (opt.nPrePassBase >= 0);
	if(opt.nPrePassBase < 0)		opt.nPrePassBase = -opt.nPrePassBase;
	if(opt.nPrePassBase < 0)		opt.nPrePassBase = 0;
	if(opt.nPrePassBase > 100)		opt.nPrePassBase = 100;
	
	/*	Silence Tracking	*/
	GetPrivateProfileString("NotSo Fatso","Silence Tracking","1200",buffer,100,filename);
	sscanf(buffer,"%d",&opt.nSilenceTrackMS);

	/*  No Silence If Length	*/
	GetPrivateProfileString("NotSo Fatso","No Silence If Len","1",buffer,100,filename);
	i = 1;
	sscanf(buffer,"%d",&i);
	opt.bNoSilenceIfTime = (i != 0);
	
	/*	Inversion Cutoff	*/
	GetPrivateProfileString("NotSo Fatso","Inversion Cutoff","210",buffer,100,filename);
	sscanf(buffer,"%d",&opt.nInvertCutoffHz);

	/*	Master Volume	*/
	GetPrivateProfileString("NotSo Fatso","Master Volume     ","1.000",buffer,100,filename);
	i = vol = 0;
	sscanf(buffer,"%d.%d",&i,&vol);
	fMasterVolume = (float)i + (vol / 1000.0f);
	if(fMasterVolume < 0)	fMasterVolume = 0;
	if(fMasterVolume > 2.0f)fMasterVolume = 2.0f;
	nsfCore.SetMasterVolume(fMasterVolume);

	/*	Default Speed	*/
	GetPrivateProfileString("NotSo Fatso","Default Speed     ","1",buffer,100,filename);
	sscanf(buffer,"%d",&i);
	bUseDefaultSpeed = (i ? 1 : 0);

	/*	Speed Throttle	*/
	GetPrivateProfileString("NotSo Fatso","Speed Throttle    ","60.1",buffer,100,filename);
	vol = pan = 0;
	sscanf(buffer,"%d.%d",&vol,&pan);
	while(pan >= 10)	pan /= 10;
	fSpeedThrottle = (float)(vol) + (pan / 10.0f);
	if(fSpeedThrottle < 10)		fSpeedThrottle = 10;
	if(fSpeedThrottle > 120)	fSpeedThrottle = 120;

	/*	Default Song Length	*/
	GetPrivateProfileString("NotSo Fatso","Default Length    ","120000",buffer,100,filename);
	sscanf(buffer,"%u",&nDefaultSongLength);

	/*	Default Fade Length	*/
	GetPrivateProfileString("NotSo Fatso","Default Fade      ","2000",buffer,100,filename);
	sscanf(buffer,"%u",&nDefaultFadeLength);

	/*	Auto Open Play Control (Multi-track)	*/
	GetPrivateProfileString("NotSo Fatso","Auto-Open Multi   ","1",buffer,100,filename);
	sscanf(buffer,"%d",&i);
	bOpenPlay_Normal = (i ? 1 : 0);

	/*	Auto Open Play Control (Single-track)	*/
	GetPrivateProfileString("NotSo Fatso","Auto-Open Single  ","1",buffer,100,filename);
	sscanf(buffer,"%d",&i);
	bOpenPlay_Single = (i ? 1 : 0);

	/*	Auto Open Play Control (Shadow)		*/
	GetPrivateProfileString("NotSo Fatso","Auto-Open Shadow  ","1",buffer,100,filename);
	sscanf(buffer,"%d",&i);
	bOpenPlay_Shadow = (i ? 1 : 0);

	/*	Format String (Multi)	*/
	GetPrivateProfileString("NotSo Fatso","Format Multi      ","%g",buffer,100,filename);
	szFormatString = buffer;

	/*	Format String (Single)	*/
	GetPrivateProfileString("NotSo Fatso","Format Single     ","%g",buffer,100,filename);
	szSingleFormatString = buffer;

	/*	Format String (Shadow)	*/
	GetPrivateProfileString("NotSo Fatso","Format Shadow     ","%g - %l",buffer,100,filename);
	szShadowFormatString = buffer;

	/*	Disable Playlists		*/
	GetPrivateProfileString("NotSo Fatso","Disable Playlists ","0",buffer,100,filename);
	sscanf(buffer,"%d",&i);
	bUsePlaylists = !i;

	/*	Loop Forever     		*/
	GetPrivateProfileString("NotSo Fatso","Loop Forever      ","0",buffer,100,filename);
	sscanf(buffer,"%d",&i);

	bLoopForever = ((i == 1) ? 1 : 0);

	/*	Channel Options			*/
	LPCSTR defaults[29] = {
		"1:255:-45:1","1:255:45:1","1:255:0:0","1:255:0:0","1:255:0:0",	/* native	*/
		"1:255:-50:1","1:255:50:0","1:255:0:0",							/* VRC6		*/
		"1:255:-50:1","1:255:50:1","1:255:0:0",							/* MMC5		*/
		"1:255:-32:1","1:255:32:1","1:255:-32:0","1:255:32:0","1:255:-32:1","1:255:32:1","1:255:-32:0","1:255:32:0", /* N106 */
		"1:255:-35:1","1:255:35:1","1:255:-30:0","1:255:30:0","1:255:-20:1","1:255:20:1",	/* VRC7	*/
		"1:255:-40:0","1:255:0:1","1:255:40:0",							/* FME-07	*/
		"1:255:0:0"														/* FDS		*/
	};
	const BYTE defaultinv[29] = {1,1,0,0,0,   1,0,0,   1,1,0,   1,1,0,0,1,1,0,0,   1,1,0,0,1,1,  0,1,0, 0 };
	for(i = 0; i < 29; i++)
	{
		sprintf(secondbuffer,"Channel %02d        ",i);
		GetPrivateProfileString("NotSo Fatso",secondbuffer,defaults[i],buffer,100,filename);
		pan = 0;
		mix = 1;
		vol = 255;
		inv = defaultinv[i];
		sscanf(buffer,"%d:%d:%d:%d",&mix,&vol,&pan,&inv);
		if(vol < 0)		vol = 0;
		if(vol > 255)	vol = 255;
		if(pan < -127)	pan = -127;
		if(pan > 127)	pan = 127;
		if(mix != 0)	mix = 1;
		if(inv != 1)	inv = 0;

		nsfCore.SetChannelOptions(i,mix,vol,pan,inv);

		     if(i == 28)mControlDlg.m_channels.SetOptions(i,mix,vol,pan,inv);
		else if(i < 5)	mControlDlg.m_channels.SetOptions(i,mix,vol,pan,inv);
		else if(i < 8)	mControlDlg.m_vrc6.SetOptions(i,mix,vol,pan,inv);
		else if(i < 11)	mControlDlg.m_mmc5.SetOptions(i,mix,vol,pan,inv);
		else if(i < 19)	mControlDlg.m_n106.SetOptions(i,mix,vol,pan,inv);
		else if(i < 25)	mControlDlg.m_vrc7.SetOptions(i,mix,vol,pan,inv);
		else			mControlDlg.m_fme07.SetOptions(i,mix,vol,pan,inv);
	}
	
	/*	Window position			*/
	GetPrivateProfileString("NotSo Fatso","Window Position","0,0",buffer,100,filename);
	vol = pan = 0;
	sscanf(buffer,"%d,%d",&vol,&pan);
	mControlDlg.PutWindow(vol,pan);

	/*	Cling to Winamp			*/
	GetPrivateProfileString("NotSo Fatso","Subclass Winamp","0",buffer,100,filename);
	vol = 0;
	sscanf(buffer,"%d",&vol);
	bClingToWinamp = vol ? 1 : 0;

	/*	Always on Top			*/
	GetPrivateProfileString("NotSo Fatso","Always on Top","0",buffer,100,filename);
	vol = 0;
	sscanf(buffer,"%d",&vol);
	bAlwaysOnTop = vol ? 1 : 0;
	SetAlwaysOnTop(bAlwaysOnTop);
	
	/*	Mini-Window position		*/
	GetPrivateProfileString("NotSo Fatso","Mini Window Position","0,0",buffer,100,filename);
	vol = pan = 0;
	sscanf(buffer,"%d,%d",&vol,&pan);
	mMiniPlayDlg.PutWindow(vol,pan);
	
	/*	Mini-Always on Top			*/
	GetPrivateProfileString("NotSo Fatso","Mini Always on Top","0",buffer,100,filename);
	vol = 0;
	sscanf(buffer,"%d",&vol);
	bMiniAlwaysOnTop = vol ? 1 : 0;
	SetMiniAlwaysOnTop(bMiniAlwaysOnTop);

	/*	Use Mini			*/
	GetPrivateProfileString("NotSo Fatso","Use Mini","0",buffer,100,filename);
	vol = 0;
	sscanf(buffer,"%d",&vol);
	bUseMiniPlayControl = vol ? 1 : 0;

	/*
	 *	Send it on over to the core
	 */
	mControlDlg.m_config.SetOptions(nSampleRate,nChannels,fMasterVolume,
		bUseDefaultSpeed,fSpeedThrottle,nDefaultSongLength,nDefaultFadeLength,&opt);
	mControlDlg.m_config2.SetOptions(bOpenPlay_Normal,bOpenPlay_Single,bOpenPlay_Shadow,szFormatString,szSingleFormatString,
		szShadowFormatString,bUsePlaylists,bLoopForever,bClingToWinamp,bAlwaysOnTop,bUseMiniPlayControl,bMiniAlwaysOnTop);
	mControlDlg.m_config3.SetOptions(&opt,bIgnoreNSFVersion);
	nsfCore.SetAdvancedOptions(&opt);
}

void CNSF::SaveConfiguration()
{
	if(!bGUIInited)
		return;

	int i, vol, pan, mix, inv;
	char buffer[101];
	char secondbuffer[20];
	char filename[MAX_PATH + 1];
	NSF_ADVANCEDOPTIONS opt;

	buffer[100] = 0;

	GetModuleFileName(inMod->hDllInstance,filename,MAX_PATH);

	for(i = lstrlen(filename) - 1; filename[i] != '\\'; i--);
	filename[i + 1] = 0;
	lstrcat(filename,"plugin.ini");

	/*
	 *	Get values from the GUI	
	 */
	mControlDlg.m_config.GetPlayMode(&nSampleRate,&nChannels);
	mControlDlg.m_config.GetDefaultTimes(nDefaultSongLength,nDefaultFadeLength);
	mControlDlg.m_config2.GetFormatStrings(szFormatString,szSingleFormatString,szShadowFormatString);
	nsfCore.GetAdvancedOptions(&opt);


	/*	Sample Rate	*/
	sprintf(buffer,"%d",nSampleRate);
	WritePrivateProfileString("NotSo Fatso","Sample Rate       ",buffer,filename);

	/*	Channels		*/
	sprintf(buffer,"%d",nChannels);
	WritePrivateProfileString("NotSo Fatso","Mono/Stereo       ",buffer,filename);

	/*	Master Volume	*/
	sprintf(buffer,"%d.%03d",(int)(fMasterVolume * 1000) / 1000,(int)(fMasterVolume * 1000) % 1000);
	WritePrivateProfileString("NotSo Fatso","Master Volume     ",buffer,filename);
	
	/*	DMC pop reducer	*/
	sprintf(buffer,"%d",opt.bDMCPopReducer);
	WritePrivateProfileString("NotSo Fatso","DMC Pop Reducer",buffer,filename);
	
	/*	FDS pop reducer	*/
	sprintf(buffer,"%d",opt.bFDSPopReducer);
	WritePrivateProfileString("NotSo Fatso","FDS Pop Reducer",buffer,filename);
		
	/*	Force 4017 Write	*/
	sprintf(buffer,"%d",opt.nForce4017Write);
	WritePrivateProfileString("NotSo Fatso","Force 4017",buffer,filename);

	/*	N106 pop reducer	*/
	sprintf(buffer,"%d",opt.bN106PopReducer);
	WritePrivateProfileString("NotSo Fatso","N106 Pop Reducer",buffer,filename);
	
	/*	Ignore $4011 Writes	*/
	sprintf(buffer,"%d",opt.bIgnore4011Writes);
	WritePrivateProfileString("NotSo Fatso","Ignore 4011",buffer,filename);
	
	/*	Ignore BRK	*/
	sprintf(buffer,"%d",opt.bIgnoreBRK);
	WritePrivateProfileString("NotSo Fatso","Ignore BRK",buffer,filename);
	
	/*	Ignore Illegal	*/
	sprintf(buffer,"%d",opt.bIgnoreIllegalOps);
	WritePrivateProfileString("NotSo Fatso","Ignore Illegal",buffer,filename);
	
	/*	No Wait for Return	*/
	sprintf(buffer,"%d",opt.bNoWaitForReturn);
	WritePrivateProfileString("NotSo Fatso","No Wait",buffer,filename);
	
	/*	Clean AXY Regs	*/
	sprintf(buffer,"%d",opt.bPALPreference);
	WritePrivateProfileString("NotSo Fatso","Prefer PAL",buffer,filename);
	
	/*	Ignore NSF Version	*/
	sprintf(buffer,"%d",bIgnoreNSFVersion);
	WritePrivateProfileString("NotSo Fatso","Ignore NSF Version",buffer,filename);

	/*	Reset Duty		*/
	sprintf(buffer,"%d",opt.bResetDuty);
	WritePrivateProfileString("NotSo Fatso","Reset Duty",buffer,filename);
	
	/*	Prefer PAL	*/
	sprintf(buffer,"%d",opt.bCleanAXY);
	WritePrivateProfileString("NotSo Fatso","Clean AXY",buffer,filename);
	
	/*	Filter		*/
	sprintf(buffer,"%d",(opt.bLowPassEnabled ? opt.nLowPassBase : -opt.nLowPassBase));
	WritePrivateProfileString("NotSo Fatso","LowPass",buffer,filename);
	sprintf(buffer,"%d",(opt.bHighPassEnabled ? opt.nHighPassBase : -opt.nHighPassBase));
	WritePrivateProfileString("NotSo Fatso","HighPass",buffer,filename);
	sprintf(buffer,"%d",(opt.bPrePassEnabled ? opt.nPrePassBase : -opt.nPrePassBase));
	WritePrivateProfileString("NotSo Fatso","PrePass",buffer,filename);

	/*	Silence Tracking	*/
	sprintf(buffer,"%d",opt.nSilenceTrackMS);
	WritePrivateProfileString("NotSo Fatso","Silence Tracking",buffer,filename);

	/*  No Silence If Length	*/
	sprintf(buffer,"%d",opt.bNoSilenceIfTime);
	WritePrivateProfileString("NotSo Fatso","No Silence If Len",buffer,filename);

	/*	Inversion Cutoff	*/
	sprintf(buffer,"%d",opt.nInvertCutoffHz);
	WritePrivateProfileString("NotSo Fatso","Inversion Cutoff",buffer,filename);

	/*	Default Speed	*/
	sprintf(buffer,"%d",bUseDefaultSpeed);
	WritePrivateProfileString("NotSo Fatso","Default Speed     ",buffer,filename);

	/*	Speed Throttle	*/
	vol = (int)(fSpeedThrottle * 10);
	while(((float)vol / 10.0f) > fSpeedThrottle)
		vol--;
	pan = vol % 10;
	vol /= 10;
	sprintf(buffer,"%d.%d",vol,pan);
	WritePrivateProfileString("NotSo Fatso","Speed Throttle    ",buffer,filename);

	/*	Default Song Length	*/
	sprintf(buffer,"%u",nDefaultSongLength);
	WritePrivateProfileString("NotSo Fatso","Default Length    ",buffer,filename);

	/*	Default Fade Length	*/
	sprintf(buffer,"%u",nDefaultFadeLength);
	WritePrivateProfileString("NotSo Fatso","Default Fade      ",buffer,filename);

	/*	Auto Open Play Control (Multi-track)	*/
	sprintf(buffer,"%d",bOpenPlay_Normal);
	WritePrivateProfileString("NotSo Fatso","Auto-Open Multi   ",buffer,filename);

	/*	Auto Open Play Control (Single-track)	*/
	sprintf(buffer,"%d",bOpenPlay_Single);
	WritePrivateProfileString("NotSo Fatso","Auto-Open Single  ",buffer,filename);

	/*	Auto Open Play Control (Shadow)		*/
	sprintf(buffer,"%d",bOpenPlay_Shadow);
	WritePrivateProfileString("NotSo Fatso","Auto-Open Shadow  ",buffer,filename);

	/*	Format String (Multi)	*/
	WritePrivateProfileString("NotSo Fatso","Format Multi      ",szFormatString,filename);

	/*	Format String (Single)	*/
	WritePrivateProfileString("NotSo Fatso","Format Single     ",szSingleFormatString,filename);

	/*	Format String (Shadow)	*/
	WritePrivateProfileString("NotSo Fatso","Format Shadow     ",szShadowFormatString,filename);

	/*	Disable Playlists		*/
	sprintf(buffer,"%d",!bUsePlaylists);
	WritePrivateProfileString("NotSo Fatso","Disable Playlists ",buffer,filename);

	/*	Loop Forever     		*/
	sprintf(buffer,"%d",bLoopForever);
	WritePrivateProfileString("NotSo Fatso","Loop Forever      ",buffer,filename);
	
	/*	Channel Options			*/
	for(i = 0; i < 29; i++)
	{
		     if(i == 28)mControlDlg.m_channels.GetOptions(i,mix,vol,pan,inv);
		else if(i < 5)	mControlDlg.m_channels.GetOptions(i,mix,vol,pan,inv);
		else if(i < 8)	mControlDlg.m_vrc6.GetOptions(i,mix,vol,pan,inv);
		else if(i < 11)	mControlDlg.m_mmc5.GetOptions(i,mix,vol,pan,inv);
		else if(i < 19)	mControlDlg.m_n106.GetOptions(i,mix,vol,pan,inv);
		else if(i < 25)	mControlDlg.m_vrc7.GetOptions(i,mix,vol,pan,inv);
		else			mControlDlg.m_fme07.GetOptions(i,mix,vol,pan,inv);

		sprintf(secondbuffer,"Channel %02d        ",i);
		sprintf(buffer,"%d:%d:%d:%d",mix,vol,pan,inv);
		WritePrivateProfileString("NotSo Fatso",secondbuffer,buffer,filename);
	}
	
	/*	Window position			*/
	sprintf(buffer,"%d,%d",mControlDlg.x_pos,mControlDlg.y_pos);
	WritePrivateProfileString("NotSo Fatso","Window Position",buffer,filename);
	
	/*	Cling to Winamp			*/
	sprintf(buffer,"%d",bClingToWinamp);
	WritePrivateProfileString("NotSo Fatso","Subclass Winamp",buffer,filename);
	
	/*	Always on Top		*/
	sprintf(buffer,"%d",bAlwaysOnTop);
	WritePrivateProfileString("NotSo Fatso","Always on Top",buffer,filename);

	
	/*	Mini-Window position		*/
	sprintf(buffer,"%d,%d",mMiniPlayDlg.x_pos,mMiniPlayDlg.y_pos);
	WritePrivateProfileString("NotSo Fatso","Mini Window Position",buffer,filename);
	
	/*	Mini-Always on Top			*/
	sprintf(buffer,"%d",bMiniAlwaysOnTop);
	WritePrivateProfileString("NotSo Fatso","Mini Always on Top",buffer,filename);

	/*	Use Mini			*/
	sprintf(buffer,"%d",bUseMiniPlayControl);
	WritePrivateProfileString("NotSo Fatso","Use Mini",buffer,filename);
}