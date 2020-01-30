/* taken from FCE Ultra - NES/Famicom Emulator
 *
 * Copyright notice for this file:
 *  Copyright (C) 1999,2000 Tatsuyuki Satoh
 *  Copyright (C) 2001,2002 Ben Parnell
 *  Copyright (C) 2004      Disch
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
/* This file has been heavily modified from the original(mostly unused
   code was removed).  If you want to use it for anything other than
   VRC7 sound emulation, you should get the original from the AdPlug
   source distribution or the MAME(version 0.37b16) source distribution
   (but be careful about the different licenses).
        - Xodnizel
*/

/* I added a few modifications to YM3812UpdateOne and the FM_OPL struct
   to get channel volume/pan control.  But other than that, things are
   pretty much the way Xodnizel left them.

   All my changes are commented, so search the file for "Disch" to see alterations.
		- Disch
*/


/*	Section added to get this linkable from a C++ environment  -Disch		*/
#ifdef __cplusplus
extern "C" {
#endif

#ifndef __FMOPL_H_
#define __FMOPL_H_

/* --- system optimize --- */
/* select bit size of output : 8 or 16 */
#define OPL_OUTPUT_BIT 16

/* compiler dependence */
#ifndef OSD_CPU_H
#define OSD_CPU_H

typedef unsigned char	UINT8;   /* unsigned  8bit */
typedef unsigned short	UINT16;  /* unsigned 16bit */
typedef unsigned long	UINT32;  /* unsigned 32bit */
typedef signed char		INT8;    /* signed  8bit   */
typedef signed short	INT16;   /* signed 16bit   */
typedef signed long		INT32;   /* signed 32bit   */
#endif

#if (OPL_OUTPUT_BIT==16)
typedef INT16 OPLSAMPLE;
#endif
#if (OPL_OUTPUT_BIT==8)
typedef unsigned char  OPLSAMPLE;
#endif


/* !!!!! here is private section , do not access there member direct !!!!! */

#define OPL_TYPE_WAVESEL   0x01  /* waveform select    */

/* Saving is necessary for member of the 'R' mark for suspend/resume */
/* ---------- OPL one of slot  ---------- */
typedef struct fm_opl_slot {
	INT32 TL;		/* total level     :TL << 8            */
	INT32 TLL;		/* adjusted now TL                     */
	UINT8  KSR;		/* key scale rate  :(shift down bit)   */
	INT32 *AR;		/* attack rate     :&AR_TABLE[AR<<2]   */
	INT32 *DR;		/* decay rate      :&DR_TALBE[DR<<2]   */
	INT32 SL;		/* sustin level    :SL_TALBE[SL]       */
	INT32 *RR;		/* release rate    :&DR_TABLE[RR<<2]   */
	UINT8 ksl;		/* keyscale level  :(shift down bits)  */
	UINT8 ksr;		/* key scale rate  :kcode>>KSR         */
	UINT32 mul;		/* multiple        :ML_TABLE[ML]       */
	UINT32 Cnt;		/* frequency count :                   */
	UINT32 Incr;	/* frequency step  :                   */
	/* envelope generator state */
	UINT8 eg_typ;	/* envelope type flag                  */
	UINT8 evm;		/* envelope phase                      */
	INT32 evc;		/* envelope counter                    */
	INT32 eve;		/* envelope counter end point          */
	INT32 evs;		/* envelope counter step               */
	INT32 evsa;	/* envelope step for AR :AR[ksr]           */
	INT32 evsd;	/* envelope step for DR :DR[ksr]           */
	INT32 evsr;	/* envelope step for RR :RR[ksr]           */
	/* LFO */
	UINT8 ams;		/* ams flag                            */
	UINT8 vib;		/* vibrate flag                        */
	/* wave selector */
	INT32 **wavetable;
}OPL_SLOT;

/* ---------- OPL one of channel  ---------- */
typedef struct fm_opl_channel {
	OPL_SLOT SLOT[2];
	UINT8 CON;			/* connection type                     */
	UINT8 FB;			/* feed back       :(shift down bit)   */
	INT32 *connect1;	/* slot1 output pointer                */
	INT32 *connect2;	/* slot2 output pointer                */
	INT32 op1_out[2];	/* slot1 output for selfeedback        */
	/* phase generator state */
	UINT32  block_fnum;	/* block+fnum      :                   */
	UINT8 kcode;		/* key code        : KeyScaleCode      */
	UINT32  fc;			/* Freq. Increment base                */
	UINT32  ksl_base;	/* KeyScaleLevel Base step             */
	UINT8 keyon;		/* key on/off flag                     */
} OPL_CH;

/* OPL state */
typedef struct fm_opl_f {
	UINT8 type;			/* chip type                         */
	int clock;			/* master clock  (Hz)                */
	int rate;			/* sampling rate (Hz)                */
	double freqbase;	/* frequency base                    */
	double TimerBase;	/* Timer base time (==sampling time) */
	UINT8 address;		/* address register                  */
	UINT32 mode;		/* Reg.08 : CSM , notesel,etc.       */

	/* FM channel slots */
	OPL_CH *P_CH;		/* pointer of CH                     */
	int	max_ch;			/* maximum channel                   */

	/* time tables */
	INT32 AR_TABLE[75];	/* atttack rate tables */
	INT32 DR_TABLE[75];	/* decay rate tables   */
	UINT32 FN_TABLE[1024];  /* fnumber -> increment counter */
	/* LFO */
	INT32 *ams_table;
	INT32 *vib_table;
	INT32 amsCnt;
	INT32 amsIncr;
	INT32 vibCnt;
	INT32 vibIncr;
	/* wave selector enable flag */
	UINT8 wavesel;


	/*
	 *	Values added to track left/right channel amplitude control (for volume/pan control)
	 *		-Disch
	 */
	/*	Channel Volume/Panning control	*/
	float	fLeftMultiplier[6];
	float	fRightMultiplier[6];

	/*
	 *	More stuf added by me to get inversion and frequency cutoff
	 *		-Disch
	 */
	UINT16	nFreqReg[6];
	UINT8	bInvert[6];
	UINT8	bDoInvert[6];
	UINT16	nInvertFreqCutoff;

} FM_OPL;

/* ---------- Generic interface section ---------- */
#define OPL_TYPE_YM3812 (OPL_TYPE_WAVESEL)

FM_OPL *OPLCreate(int type, int clock, int rate);
void OPLDestroy(FM_OPL *OPL);

void OPLResetChip(FM_OPL *OPL);
void OPLWrite(FM_OPL *OPL,UINT8 a,UINT8 v);

/* YM3626/YM3812 local section */	// 'mix' values (for channel disabling) and stereo boolean value added -Disch
void YM3812UpdateOne(FM_OPL *OPL, UINT8 *buffer, int size, UINT8* mix,UINT8 stereo);

#endif


/*	Section added to get this linkable from a C++ environment  -Disch		*/
#ifdef __cplusplus
}
#endif