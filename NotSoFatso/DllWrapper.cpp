// MATTT: Remove this!
#define min(a, b) ((a) < (b) ? (a) : (b))
#define max(a, b) ((a) > (b) ? (a) : (b))

#include "NSF_File.h"
#include "NSF_Core.h"

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

extern "C" void __stdcall NsfClose(void* nsfPtr)
{
	delete (NsfCoreFile*)nsfPtr;
}

extern "C" void __stdcall NsfSetTrack(void* nsfPtr, int track)
{
	((NsfCoreFile*)nsfPtr)->core.SetTrack(track);
}

extern "C" void __stdcall NsfRunFrame(void* nsfPtr)
{
	((NsfCoreFile*)nsfPtr)->core.RunOneFrame();
}

extern "C" int __stdcall NsfGetState(void* nsfPtr, int channel, int state, int sub)
{
	return ((NsfCoreFile*)nsfPtr)->core.GetState(channel, state, sub);
}
