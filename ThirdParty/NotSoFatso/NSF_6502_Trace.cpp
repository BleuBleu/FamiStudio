/*
 *
 *	Copyright (C) 2003  Disch

 *	This library is free software; you can redistribute it and/or
 *	modify it under the terms of the GNU Lesser General Public
 *	License as published by the Free Software Foundation; either
 *	version 2.1 of the License, or (at your option) any later version.

 *	This library is distributed in the hope that it will be useful,
 *	but WITHOUT ANY WARRANTY; without even the implied warranty of
 *	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 *	Lesser General Public License for more details.

 *	You should have received a copy of the GNU Lesser General Public
 *	License along with this library; if not, write to the Free Software
 *	Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA

 */

//////////////////////////////////////////////////////////////////////////
//
//  NSF_6502_Trace.cpp
//


#define WIN32_LEAN_AND_MEAN
#include <Windows.h>
#include <stdio.h>
#include "NSF_Core.h"

#ifdef TRACE_6502

FILE* pLogFile = NULL;


const LPCSTR OpCodeNames[0x100] = {
" BRK "," ORA ","(HLT)","(ASO)","(SKB)"," ORA "," ASL ","(ASO)"," PHP "," ORA "," ASL ","(ANC)","(SKW)"," ORA "," ASL ","(ASO)",
" BPL "," ORA ","(HLT)","(ASO)","(SKB)"," ORA "," ASL ","(ASO)"," CLC "," ORA ","(NOP)","(ASO)","(SKW)"," ORA "," ASL ","(ASO)",
" JSR "," AND ","(HLT)","(RLA)"," BIT "," AND "," ROL ","(RLA)"," PLP "," AND "," ROL ","(ANC)"," BIT "," AND "," ROL ","(RLA)",
" BMI "," AND ","(HLT)","(RLA)","(SKB)"," AND "," ROL ","(RLA)"," SEC "," AND ","(NOP)","(RLA)","(SKW)"," AND "," ROL ","(RLA)",
" RTI "," EOR ","(HLT)","(LSE)","(SKB)"," EOR "," LSR ","(LSE)"," PHA "," EOR "," LSR ","(ALR)"," JMP "," EOR "," LSR ","(LSE)",
" BVC "," EOR ","(HLT)","(LSE)","(SKB)"," EOR "," LSR ","(LSE)"," CLI "," EOR ","(NOP)","(LSE)","(SKW)"," EOR "," LSR ","(LSE)",
" RTS "," ADC ","(HLT)","(RRA)","(SKB)"," ADC "," ROR ","(RRA)"," PLA "," ADC "," ROR ","(ARR)"," JMP "," ADC "," ROR ","(RRA)",
" BVS "," ADC ","(HLT)","(RRA)","(SKB)"," ADC "," ROR ","(RRA)"," SEI "," ADC ","(NOP)","(RRA)","(SKW)"," ADC "," ROR ","(RRA)",
"(SKB)"," STA ","(SKB)","(AXS)"," STY "," STA "," STX ","(AXS)"," DEY ","(SKB)"," TXA ","(XAA)"," STY "," STA "," STX ","(AXS)",
" BCC "," STA ","(HLT)","(AXA)"," STY "," STA "," STX ","(AXS)"," TYA "," STA "," TXS ","(TAS)","(SAY)"," STA ","(XAS)","(AXA)",
" LDY "," LDA "," LDX ","(LAX)"," LDY "," LDA "," LDX ","(LAX)"," TAY "," LDA "," TAX ","(OAL)"," LDY "," LDA "," LDX ","(LAX)",
" BCS "," LDA ","(HLT)","(LAX)"," LDY "," LDA "," LDX ","(LAX)"," CLV "," LDA "," TSX ","(LAS)"," LDY "," LDA "," LDX ","(LAX)",
" CPY "," CMP ","(SKB)","(DCM)"," CPY "," CMP "," DEC ","(DCM)"," INY "," CMP "," DEX ","(SAX)"," CPY "," CMP "," DEC ","(DCM)",
" BNE "," CMP ","(HLT)","(DCM)","(SKB)"," CMP "," DEC ","(DCM)"," CLD "," CMP ","(NOP)","(DCM)","(SKW)"," CMP "," DEC ","(DCM)",
" CPX "," SBC ","(SKB)","(INS)"," CPX "," SBC "," INC ","(INS)"," INX "," SBC "," NOP ","(SBC)"," CPX "," SBC "," INC ","(INS)",
" BEQ "," SBC ","(HLT)","(INS)","(SKB)"," SBC "," INC ","(INS)"," SED "," SBC ","(NOP)","(INS)","(SKW)"," SBC "," INC ","(INS)"		};

#define amXx		0			//Implied (no operand needed)
#define amIx		1			//Indirect X (useless!)
#define amIy		2			//Indirect Y (useful!)
#define amIn		3			//Indirect (rarly used!)
#define amZp		4			//Zero Page
#define amZx		5			//Zero Page X
#define amZy		7			//Zero Page Y
#define amAb		8			//Absolute
#define amAx		9			//Absolute X
#define amAy		10			//Absolute Y
#define amAc		11			//Accumulator
#define amIm		12			//Immediate
#define amRl		13			//Relative

const BYTE AddressingModes[0x100] = {
amXx,amIx,amXx,amIx,amZp,amZp,amZp,amZp,amXx,amIm,amAc,amIm,amAb,amAb,amAb,amAb,
amRl,amIy,amXx,amIy,amZx,amZx,amZx,amZx,amXx,amAy,amXx,amAy,amAx,amAx,amAx,amAx,
amAb,amIx,amXx,amIx,amZp,amZp,amZp,amZp,amXx,amIm,amAc,amIm,amAb,amAb,amAb,amAb,
amRl,amIy,amXx,amIy,amZx,amZx,amZx,amZx,amXx,amAy,amXx,amAy,amAx,amAx,amAx,amAx,
amXx,amIx,amXx,amIx,amZp,amZp,amZp,amZp,amXx,amIm,amAc,amIm,amAb,amAb,amAb,amAb,
amRl,amIy,amXx,amIy,amZx,amZx,amZx,amZx,amXx,amAy,amXx,amAy,amAx,amAx,amAx,amAx,
amXx,amIx,amXx,amIx,amZp,amZp,amZp,amZp,amXx,amIm,amAc,amIm,amIn,amAb,amAb,amAb,
amRl,amIy,amXx,amIy,amZx,amZx,amZx,amZx,amXx,amAy,amXx,amAy,amAx,amAx,amAx,amAx,
amIm,amIx,amIm,amIx,amZp,amZp,amZp,amZp,amXx,amIm,amXx,amIm,amAb,amAb,amAb,amAb,
amRl,amIy,amXx,amIy,amZx,amZx,amZy,amZy,amXx,amAy,amXx,amAy,amAx,amAx,amAy,amAy,
amIm,amIx,amIm,amIx,amZp,amZp,amZp,amZp,amXx,amIm,amXx,amIm,amAb,amAb,amAb,amAb,
amRl,amIy,amXx,amIy,amZx,amZx,amZy,amZy,amXx,amAy,amXx,amAy,amAx,amAx,amAy,amAy,
amIm,amIx,amIm,amIx,amZp,amZp,amZp,amZp,amXx,amIm,amXx,amIm,amAb,amAb,amAb,amAb,
amRl,amIy,amXx,amIy,amZx,amZx,amZx,amZx,amXx,amAy,amXx,amAy,amAx,amAx,amAx,amAx,
amIm,amIx,amIm,amIx,amZp,amZp,amZp,amZp,amXx,amIm,amXx,amIm,amAb,amAb,amAb,amAb,
amRl,amIy,amXx,amIy,amZx,amZx,amZx,amZx,amXx,amAy,amXx,amAy,amAx,amAx,amAx,amAx
};

#define Read(a) ((this->*ReadMemory[(a) >> 12])(a))	

void CNSFCore::Trace6502(WORD pc,BYTE a, BYTE x, BYTE y, BYTE sp, BYTE st)
{
	if(bHasBeenOpened)
	{
		pLogFile = fopen("C:\\NotSo Log.txt","r+b");
		if(!pLogFile)
			return;

		fseek(pLogFile,0,SEEK_END);
	}
	else
	{
		pLogFile = fopen("C:\\NotSo Log.txt","wb");
		if(!pLogFile)
			return;

		bHasBeenOpened = 1;

		fprintf(pLogFile,"PC     Instr.      Context            A  X  Y  Status    SP\r\n===========================================================\r\n\r\n");
	}

	BYTE inst = Read(pc);

	WORD frontaddress;
	WORD finaladdress;
	BYTE val;
	
	fprintf(pLogFile,"%04X   %02X   %s  ",pc,inst,OpCodeNames[inst]);

	switch(AddressingModes[inst])
	{
	case amXx:		//implied
		fprintf(pLogFile,"                ");
		break;

	case amIx:		//indirect X
		frontaddress = Read(pc + 1);
		finaladdress = BYTE(frontaddress + x);
		finaladdress = Read(finaladdress) | (Read(finaladdress + 1) << 8);
		val = Read(finaladdress);
		fprintf(pLogFile,"(%02X,X) [%04X=%02X]",frontaddress,finaladdress,val);
		break;

	case amIy:		//indirect Y
		frontaddress = Read(pc + 1);
		finaladdress = (Read(frontaddress) | (Read(frontaddress + 1) << 8)) + y;
		val = Read(finaladdress);
		fprintf(pLogFile,"(%02X),Y [%04X=%02X]",frontaddress,finaladdress,val);
		break;

	case amIn:		//indirect
		frontaddress = Read(pc + 1) | (Read(pc + 2) << 8);
		finaladdress = (Read(frontaddress) | (Read(frontaddress + 1) << 8));
		fprintf(pLogFile,"(%04X)    [%04X]",frontaddress,finaladdress);
		break;

	case amZp:		//zero page
		frontaddress = Read(pc + 1);
		val = Read(frontaddress);
		fprintf(pLogFile,"%02X          [%02X]",frontaddress,val);
		break;

	case amZx:		//zero page X
		frontaddress = Read(pc + 1);
		finaladdress = frontaddress + x;
		val = Read(finaladdress);
		fprintf(pLogFile,"%02X,X   [%04X=%02X]",frontaddress,finaladdress,val);
		break;

	case amZy:		//zero page Y
		frontaddress = Read(pc + 1);
		finaladdress = frontaddress + y;
		val = Read(finaladdress);
		fprintf(pLogFile,"%02X,Y   [%04X=%02X]",frontaddress,finaladdress,val);
		break;

	case amAb:		//absolute
		frontaddress = Read(pc + 1) | (Read(pc + 2) << 8);
		val = Read(frontaddress);
		fprintf(pLogFile,"%04X        [%02X]",frontaddress,val);
		break;

	case amAx:		//absolute X
		frontaddress = Read(pc + 1) | (Read(pc + 2) << 8);
		finaladdress = frontaddress + x;
		val = Read(finaladdress);
		fprintf(pLogFile,"%04X,X [%04X=%02X]",frontaddress,finaladdress,val);
		break;

	case amAy:		//absolute Y
		frontaddress = Read(pc + 1) | (Read(pc + 2) << 8);
		finaladdress = frontaddress + y;
		val = Read(finaladdress);
		fprintf(pLogFile,"%04X,Y [%04X=%02X]",frontaddress,finaladdress,val);
		break;

	case amAc:		//Accumulator
		fprintf(pLogFile,"A               ");
		break;

	case amIm:		//Immediate
		val = Read(pc + 1);
		fprintf(pLogFile,"#%02X             ",val);
		break;

	case amRl:		//relative
		val = Read(pc + 1);
		frontaddress = pc + 2 + (char)val;
		fprintf(pLogFile,"%02X        [%04X]",val,frontaddress);
		break;
	}

	fprintf(pLogFile,"  %02X %02X %02X  [%c%c%c%c%c]   %02X\r\n",
		a,x,y,
		(st & 0x80) ? 'N' : '.',
		(st & 0x40) ? 'V' : '.',
		(st & 0x04) ? 'I' : '.',
		(st & 0x02) ? 'Z' : '.',
		(st & 0x01) ? 'C' : '.',
		sp);

	fclose(pLogFile);
}





#endif