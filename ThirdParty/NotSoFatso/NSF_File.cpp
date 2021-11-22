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
//  NSF_File.cpp
//
//


#include <stdio.h>
#include "NSF_Core.h"
#include "NSF_File.h"

#define SAFE_DELETE(p) { if(p){ delete[] p; p = NULL; } }
#define SAFE_NEW(p,t,s,r) p = new t[s]; if(!p) return r; ZeroMemory(p,sizeof(t) * s)


int		CNSFFile::LoadFile(LPCSTR path,BYTE needdata,BYTE ignoreversion)
{
	Destroy();

	FILE* file = fopen(path,"rb");
	if(!file) return -1;

	UINT type = 0;
	fread(&type,4,1,file);
	int ret = -1;

	if(type == HEADERTYPE_NESM)		ret = LoadFile_NESM(file,needdata,ignoreversion);
	if(type == HEADERTYPE_NSFE)		ret = LoadFile_NSFE(file,needdata);

	fclose(file);

	// Snake's revenge puts '00' for the initial track, which (after subtracting 1) makes it 256 or -1 (bad!)
	// This prevents that crap
	if(nInitialTrack >= nTrackCount)
		nInitialTrack = 0;
	if(nInitialTrack < 0)
		nInitialTrack = 0;

	// if there's no tracks... this is a crap NSF
	if(nTrackCount < 1)
	{
		Destroy();
		return -1;
	}

	return ret;
}

void	CNSFFile::Destroy()
{
	SAFE_DELETE(pDataBuffer);
	SAFE_DELETE(pPlaylist);
	SAFE_DELETE(pTrackTime);
	SAFE_DELETE(pTrackFade);
	if(szTrackLabels)
	{
		for(int i = 0; i < nTrackCount; i++)
			SAFE_DELETE(szTrackLabels[i]);
		SAFE_DELETE(szTrackLabels);
	}
	SAFE_DELETE(szGameTitle);
	SAFE_DELETE(szArtist);
	SAFE_DELETE(szCopyright);
	SAFE_DELETE(szRipper);

	ZeroMemory(this,sizeof(CNSFFile));
}

int CNSFFile::LoadFile_NESM(FILE* file,BYTE needdata,BYTE ignoreversion)
{
	int len;

	fseek(file,0,SEEK_END);
	len = ftell(file) - 0x80;
	fseek(file,0,SEEK_SET);

	if(len < 1) return -1;

	//read the info
	NESM_HEADER					hdr;
	fread(&hdr,0x80,1,file);

	//confirm the header
	if(hdr.nHeader != HEADERTYPE_NESM)			return -1;
	if(hdr.nHeaderExtra != 0x1A)				return -1;
	if((!ignoreversion) && (hdr.nVersion > 2))	return -1; //stupid NSFs claim to be above version 1  >_>

	// We support the NSF2 format, but none of its advanced features yet.
	if (hdr.nVersion == 2 && hdr.nNSF2Features != 0) return -1;

	//NESM is generally easier to work with (but limited!)
	//  just move the data over from NESM_HEADER over to our member data

	bIsExtended =				0;
	nIsPal =					hdr.nNTSC_PAL & 0x03;
	nPAL_PlaySpeed =			hdr.nSpeedPAL;			//blarg
	nNTSC_PlaySpeed =			hdr.nSpeedNTSC;			//blarg
	nLoadAddress =				hdr.nLoadAddress;
	nInitAddress =				hdr.nInitAddress;
	nPlayAddress =				hdr.nPlayAddress;
	nChipExtensions =			hdr.nExtraChip;


	nTrackCount =				hdr.nTrackCount;
	nInitialTrack =				hdr.nInitialTrack - 1;	//stupid 1-based number =P

	memcpy(nBankswitch,hdr.nBankSwitch,8);

	SAFE_NEW(szGameTitle,char,33,1);
	SAFE_NEW(szArtist   ,char,33,1);
	SAFE_NEW(szCopyright,char,33,1);

	memcpy(szGameTitle,hdr.szGameTitle,32);
	memcpy(szArtist   ,hdr.szArtist   ,32);
	memcpy(szCopyright,hdr.szCopyright,32);

	//read the NSF data
	if(needdata)
	{
		SAFE_NEW(pDataBuffer,BYTE,len,1);
		fread(pDataBuffer,len,1,file);
		nDataBufferSize = len;
	}

	//if we got this far... it was a successful read
	return 0;
}

int CNSFFile::LoadFile_NSFE(FILE* file,BYTE needdata)
{
	//restart the file
	fseek(file,0,SEEK_SET);

	//the vars we'll be using
	UINT nChunkType;
	int  nChunkSize;
	int  nChunkUsed;
	int  nDataPos = 0;
	BYTE	bInfoFound = 0;
	BYTE	bEndFound = 0;
	BYTE	bBankFound = 0;

	NSFE_INFOCHUNK	info;
	ZeroMemory(&info,sizeof(NSFE_INFOCHUNK));
	info.nTrackCount = 1;		//default values

	//confirm the header!
	fread(&nChunkType,4,1,file);
	if(nChunkType != HEADERTYPE_NSFE)			return -1;

	//begin reading chunks
	while(!bEndFound)
	{
		if(feof(file))							return -1;
		fread(&nChunkSize,4,1,file);
		fread(&nChunkType,4,1,file);

		switch(nChunkType)
		{
		case CHUNKTYPE_INFO:
			if(bInfoFound)						return -1;	//only one info chunk permitted
			if(nChunkSize < 8)					return -1;	//minimum size

			bInfoFound = 1;
			nChunkUsed = min((int)sizeof(NSFE_INFOCHUNK),nChunkSize);

			fread(&info,nChunkUsed,1,file);
			fseek(file,nChunkSize - nChunkUsed,SEEK_CUR);

			bIsExtended =			1;
			nIsPal =				info.nIsPal & 3;
			nLoadAddress =			info.nLoadAddress;
			nInitAddress =			info.nInitAddress;
			nPlayAddress =			info.nPlayAddress;
			nChipExtensions =		info.nExt;
			nTrackCount =			info.nTrackCount;
			nInitialTrack =			info.nStartingTrack;

			nPAL_PlaySpeed =		(WORD)(1000000 / PAL_NMIRATE);		//blarg
			nNTSC_PlaySpeed =		(WORD)(1000000 / NTSC_NMIRATE);		//blarg
			break;

		case CHUNKTYPE_DATA:
			if(!bInfoFound)						return -1;
			if(nDataPos)						return -1;
			if(nChunkSize < 1)					return -1;

			nDataBufferSize = nChunkSize;
			nDataPos = ftell(file);

			fseek(file,nChunkSize,SEEK_CUR);
			break;

		case CHUNKTYPE_NEND:
			bEndFound = 1;
			break;

		case CHUNKTYPE_TIME:
			if(!bInfoFound)						return -1;
			if(pTrackTime)						return -1;

			SAFE_NEW(pTrackTime,int,nTrackCount,1);
			nChunkUsed = min(nChunkSize / 4,nTrackCount);

			fread(pTrackTime,nChunkUsed,4,file);
			fseek(file,nChunkSize - (nChunkUsed * 4),SEEK_CUR);

			for(; nChunkUsed < nTrackCount; nChunkUsed++)
				pTrackTime[nChunkUsed] = -1;	//negative signals to use default time

			break;

		case CHUNKTYPE_FADE:
			if(!bInfoFound)						return -1;
			if(pTrackFade)						return -1;

			SAFE_NEW(pTrackFade,int,nTrackCount,1);
			nChunkUsed = min(nChunkSize / 4,nTrackCount);

			fread(pTrackFade,nChunkUsed,4,file);
			fseek(file,nChunkSize - (nChunkUsed * 4),SEEK_CUR);

			for(; nChunkUsed < nTrackCount; nChunkUsed++)
				pTrackFade[nChunkUsed] = -1;	//negative signals to use default time

			break;

		case CHUNKTYPE_BANK:
			if(bBankFound)						return -1;

			bBankFound = 1;
			nChunkUsed = min(8,nChunkSize);

			fread(nBankswitch,nChunkUsed,1,file);
			fseek(file,nChunkSize - nChunkUsed,SEEK_CUR);
			break;

		case CHUNKTYPE_PLST:
			if(pPlaylist)						return -1;

			nPlaylistSize = nChunkSize;
			if(nPlaylistSize < 1)				break;  //no playlist?

			SAFE_NEW(pPlaylist,BYTE,nPlaylistSize,1);
			fread(pPlaylist,nChunkSize,1,file);
			break;

		case CHUNKTYPE_AUTH:		{
			if(szGameTitle)						return -1;

			char*		buffer;
			char*		ptr;
			SAFE_NEW(buffer,char,nChunkSize + 4,1);

			fread(buffer,nChunkSize,1,file);
			ptr = buffer;

			char**		ar[4] = {&szGameTitle,&szArtist,&szCopyright,&szRipper};
			int			i;
			for(i = 0; i < 4; i++)
			{
				nChunkUsed = strlen(ptr) + 1;
				*ar[i] = new char[nChunkUsed];
				if(!*ar[i]) { SAFE_DELETE(buffer); return 0; }
				memcpy(*ar[i],ptr,nChunkUsed);
				ptr += nChunkUsed;
			}
			SAFE_DELETE(buffer);
									}break;

		case CHUNKTYPE_TLBL:		{
			if(!bInfoFound)						return -1;
			if(szTrackLabels)					return -1;

			SAFE_NEW(szTrackLabels,char*,nTrackCount,1);

			char*		buffer;
			char*		ptr;
			SAFE_NEW(buffer,char,nChunkSize + nTrackCount,1);

			fread(buffer,nChunkSize,1,file);
			ptr = buffer;

			int			i;
			for(i = 0; i < nTrackCount; i++)
			{
				nChunkUsed = strlen(ptr) + 1;
				szTrackLabels[i] = new char[nChunkUsed];
				if(!szTrackLabels[i]) { SAFE_DELETE(buffer); return 0; }
				memcpy(szTrackLabels[i],ptr,nChunkUsed);
				ptr += nChunkUsed;
			}
			SAFE_DELETE(buffer);
									}break;

		default:		//unknown chunk
			nChunkType &= 0x000000FF;  //check the first byte
			if((nChunkType >= 'A') && (nChunkType <= 'Z'))	//chunk is vital... don't continue
				return -1;
			//otherwise, just skip it
			fseek(file,nChunkSize,SEEK_CUR);

			break;
		}		//end switch
	}			//end while

	//if we exited the while loop without a 'return', we must have hit an NEND chunk
	//  if this is the case, the file was layed out as it was expected.
	//  now.. make sure we found both an info chunk, AND a data chunk... since these are
	//  minimum requirements for a valid NSFE file

	if(!bInfoFound)			return -1;
	if(!nDataPos)			return -1;

	//if both those chunks existed, this file is valid.  Load the data if it's needed

	if(needdata)
	{
		fseek(file,nDataPos,SEEK_SET);
		SAFE_NEW(pDataBuffer,BYTE,nDataBufferSize,1);
		fread(pDataBuffer,nDataBufferSize,1,file);
	}
	else
		nDataBufferSize = 0;

	//return success!
	return 0;
}


//////////////////////////////////////////////////////////////////////////
//  File saving

int CNSFFile::SaveFile(LPCSTR path)
{
	if(!pDataBuffer)		//if we didn't grab the data, we can't save it
		return 1;

	FILE* file = fopen(path,"wb");
	if(file == NULL) return 1;

	int ret;
	if(bIsExtended)		ret = SaveFile_NSFE(file);
	else				ret = SaveFile_NESM(file);

	fclose(file);
	return ret;
}

int CNSFFile::SaveFile_NESM(FILE* file)
{
	NESM_HEADER			hdr;
	ZeroMemory(&hdr,0x80);

	hdr.nHeader =				HEADERTYPE_NESM;
	hdr.nHeaderExtra =			0x1A;
	hdr.nVersion =				1;
	hdr.nTrackCount =			nTrackCount;
	hdr.nInitialTrack =			nInitialTrack + 1;
	hdr.nLoadAddress =			nLoadAddress;
	hdr.nInitAddress =			nInitAddress;
	hdr.nPlayAddress =			nPlayAddress;

	if(szGameTitle)				memcpy(hdr.szGameTitle,szGameTitle,min(strlen(szGameTitle),31));
	if(szArtist)				memcpy(hdr.szArtist   ,szArtist   ,min(strlen(szArtist)   ,31));
	if(szCopyright)				memcpy(hdr.szCopyright,szCopyright,min(strlen(szCopyright),31));

	hdr.nSpeedNTSC =			nNTSC_PlaySpeed;
	memcpy(hdr.nBankSwitch,nBankswitch,8);
	hdr.nSpeedPAL =				nPAL_PlaySpeed;
	hdr.nNTSC_PAL =				nIsPal;
	hdr.nExtraChip =			nChipExtensions;

	//the header is all set... slap it in
	fwrite(&hdr,0x80,1,file);

	//slap in the NSF info
	fwrite(pDataBuffer,nDataBufferSize,1,file);

	//we're done.. all the other info that isn't recorded is dropped for regular NSFs
	return 0;
}

int CNSFFile::SaveFile_NSFE(FILE* file)
{
	//////////////////////////////////////////////////////////////////////////
	// I must admit... NESM files are a bit easier to work with than NSFEs =P

	UINT			nChunkType;
	int				nChunkSize;
	NSFE_INFOCHUNK	info;

	//write the header
	nChunkType = HEADERTYPE_NSFE;
	fwrite(&nChunkType,4,1,file);


	//write the info chunk
	nChunkType =			CHUNKTYPE_INFO;
	nChunkSize =			sizeof(NSFE_INFOCHUNK);
	info.nExt =				nChipExtensions;
	info.nInitAddress =		nInitAddress;
	info.nIsPal =			nIsPal;
	info.nLoadAddress =		nLoadAddress;
	info.nPlayAddress =		nPlayAddress;
	info.nStartingTrack =	nInitialTrack;
	info.nTrackCount =		nTrackCount;

	fwrite(&nChunkSize,4,1,file);
	fwrite(&nChunkType,4,1,file);
	fwrite(&info,nChunkSize,1,file);

	//if we need bankswitching... throw it in
	for(nChunkSize = 0; nChunkSize < 8; nChunkSize++)
	{
		if(nBankswitch[nChunkSize])
		{
			nChunkType = CHUNKTYPE_BANK;
			nChunkSize = 8;
			fwrite(&nChunkSize,4,1,file);
			fwrite(&nChunkType,4,1,file);
			fwrite(nBankswitch,nChunkSize,1,file);
			break;
		}
	}

	//if there's a time chunk, slap it in
	if(pTrackTime)
	{
		nChunkType =		CHUNKTYPE_TIME;
		nChunkSize =		4 * nTrackCount;
		fwrite(&nChunkSize,4,1,file);
		fwrite(&nChunkType,4,1,file);
		fwrite(pTrackTime,nChunkSize,1,file);
	}

	//slap in a fade chunk if needed
	if(pTrackFade)
	{
		nChunkType =		CHUNKTYPE_FADE;
		nChunkSize =		4 * nTrackCount;
		fwrite(&nChunkSize,4,1,file);
		fwrite(&nChunkType,4,1,file);
		fwrite(pTrackFade,nChunkSize,1,file);
	}

	//auth!
	if(szGameTitle || szCopyright || szArtist || szRipper)
	{
		nChunkType =		CHUNKTYPE_AUTH;
		nChunkSize =		4;
		if(szGameTitle)		nChunkSize += strlen(szGameTitle);
		if(szArtist)		nChunkSize += strlen(szArtist);
		if(szCopyright)		nChunkSize += strlen(szCopyright);
		if(szRipper)		nChunkSize += strlen(szRipper);
		fwrite(&nChunkSize,4,1,file);
		fwrite(&nChunkType,4,1,file);

		if(szGameTitle)		fwrite(szGameTitle,strlen(szGameTitle) + 1,1,file);
		else				fwrite("",1,1,file);
		if(szArtist)		fwrite(szArtist,strlen(szArtist) + 1,1,file);
		else				fwrite("",1,1,file);
		if(szCopyright)		fwrite(szCopyright,strlen(szCopyright) + 1,1,file);
		else				fwrite("",1,1,file);
		if(szRipper)		fwrite(szRipper,strlen(szRipper) + 1,1,file);
		else				fwrite("",1,1,file);
	}

	//plst
	if(pPlaylist)
	{
		nChunkType =		CHUNKTYPE_PLST;
		nChunkSize =		nPlaylistSize;
		fwrite(&nChunkSize,4,1,file);
		fwrite(&nChunkType,4,1,file);
		fwrite(pPlaylist,nChunkSize,1,file);
	}

	//tlbl
	if(szTrackLabels)
	{
		nChunkType =		CHUNKTYPE_TLBL;
		nChunkSize =		nTrackCount;

		for(int i = 0; i < nTrackCount; i++)
			nChunkSize += strlen(szTrackLabels[i]);

		fwrite(&nChunkSize,4,1,file);
		fwrite(&nChunkType,4,1,file);

		for(int i = 0; i < nTrackCount; i++)
		{
			if(szTrackLabels[i])
				fwrite(szTrackLabels[i],strlen(szTrackLabels[i]) + 1,1,file);
			else
				fwrite("",1,1,file);
		}
	}

	//data
	nChunkType =			CHUNKTYPE_DATA;
	nChunkSize =			nDataBufferSize;
	fwrite(&nChunkSize,4,1,file);
	fwrite(&nChunkType,4,1,file);
	fwrite(pDataBuffer,nChunkSize,1,file);

	//END
	nChunkType =			CHUNKTYPE_NEND;
	nChunkSize =			0;
	fwrite(&nChunkSize,4,1,file);
	fwrite(&nChunkType,4,1,file);

	//w00t
	return 0;
}