#include "NSF_Core.h"
#include "NSF_File.h"

#ifdef LINUX
#define __stdcall
#define __cdecl
#endif
 
struct NsfCoreFile
{
	CNSFFile file;
	CNSFCore core;
};

extern "C" void* __stdcall NsfOpen(const char* file)
{
	NsfCoreFile* nsf = new NsfCoreFile();

	if (!nsf->file.LoadFile(file, 1, false) &&
		 nsf->core.Initialize() &&
		 nsf->core.SetPlaybackOptions(44100, 1) &&
		 nsf->core.LoadNSF(&nsf->file))
	{
		float fBasedPlaysPerSec;
		if ((nsf->file.nIsPal & 0x03) == 0x01)
			fBasedPlaysPerSec = PAL_NMIRATE;
		else
			fBasedPlaysPerSec = NTSC_NMIRATE;

		for (int i = 0; i < 29; i++)
			nsf->core.SetChannelOptions(i, 1, 255, 0, 0);

		nsf->core.SetPlaybackSpeed(0);

		return nsf;
	}
	else
	{
		delete nsf;
		return NULL;
	}
}

extern "C" int __stdcall NsfGetTrackCount(void* nsfPtr)
{
	return ((NsfCoreFile*)nsfPtr)->file.nTrackCount;
}

extern "C" int __stdcall NsfIsPal(void* nsfPtr)
{
	return ((NsfCoreFile*)nsfPtr)->file.nIsPal;
}

extern "C" int __stdcall NsfGetClockSpeed(void* nsfPtr)
{
	NsfCoreFile* f = (NsfCoreFile*)nsfPtr;
	return f->file.nIsPal ? f->file.nPAL_PlaySpeed : f->file.nNTSC_PlaySpeed;
}

extern "C" int __stdcall NsfGetExpansion(void* nsfPtr)
{
	return ((NsfCoreFile*)nsfPtr)->file.nChipExtensions;
}

extern "C" const char* __stdcall NsfGetTitle(void* nsfPtr)
{
	return ((NsfCoreFile*)nsfPtr)->file.szGameTitle;
}

extern "C" const char* __stdcall NsfGetArtist(void* nsfPtr)
{
	return ((NsfCoreFile*)nsfPtr)->file.szArtist;
}

extern "C" const char* __stdcall NsfGetCopyright(void* nsfPtr)
{
	return ((NsfCoreFile*)nsfPtr)->file.szCopyright;
}

extern "C" const char* __stdcall NsfGetTrackName(void* nsfPtr, int track)
{
	CNSFFile& file = ((NsfCoreFile*)nsfPtr)->file;
	return file.szTrackLabels == NULL ? "" : file.szTrackLabels[track];
}

extern "C" const int __stdcall NsfGetTrackDuration(void* nsfPtr, int track)
{
	CNSFFile& file = ((NsfCoreFile*)nsfPtr)->file;
	return file.pTrackTime == NULL ? -1 : file.pTrackTime[track];
}

extern "C" void __stdcall NsfClose(void* nsfPtr)
{
	delete (NsfCoreFile*)nsfPtr;
}

extern "C" void __stdcall NsfSetTrack(void* nsfPtr, int track)
{
	((NsfCoreFile*)nsfPtr)->core.SetTrack(track);
}

extern "C" int __stdcall NsfRunFrame(void* nsfPtr)
{
	return ((NsfCoreFile*)nsfPtr)->core.RunOneFrame();
}

extern "C" int __stdcall NsfGetState(void* nsfPtr, int channel, int state, int sub)
{
	return ((NsfCoreFile*)nsfPtr)->core.GetState(channel, state, sub);
}

extern "C" void __stdcall NsfSetApuWriteCallback(void* nsfPtr, ApuRegWriteCallback callback)
{
	return ((NsfCoreFile*)nsfPtr)->core.SetApuWriteCallback(callback);
}
