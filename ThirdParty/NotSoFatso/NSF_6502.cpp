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

#include "NSF_Core.h"


//////////////////////////////////////////////////////////////////////////
//  Memory reading/writing and other defines

#define		Zp(a)			pRAM[a]											//reads zero page memory
#define		ZpWord(a)		(Zp(a) | (Zp((BYTE)(a + 1)) << 8))				//reads zero page memory in word form

#define		Rd(a)			((this->*ReadMemory[((WORD)(a)) >> 12])(a))		//reads memory
#define		RdWord(a)		(Rd(a) | (Rd(a + 1) << 8))						//reads memory in word form

#define		Wr(a,v)			((this->*WriteMemory[((WORD)(a)) >> 12])(a,v))	//writes memory
#define		WrZ(a,v)		pRAM[a] = v										//writes zero paged memory

#define		PUSH(v)			pStack[SP--] = v								//pushes a value onto the stack
#define		PULL(v)			v = pStack[++SP]								//pulls a value from the stack

//////////////////////////////////////////////////////////////////////////
//  Addressing Modes

// first set - gets the value that's being addressed
#define	Ad_VlIm()	val = Rd(PC.W); PC.W++									//Immediate
#define Ad_VlZp()	final.W = Rd(PC.W); val = Zp(final.W); PC.W++			//Zero Page
#define Ad_VlZx()	front.W = final.W = Rd(PC.W); final.B.l += X;			\
					val = Zp(final.B.l); PC.W++								//Zero Page, X
#define Ad_VlZy()	front.W = final.W = Rd(PC.W); final.B.l += Y;			\
					val = Zp(final.B.l); PC.W++								//Zero Page, Y
#define Ad_VlAb()	final.W = RdWord(PC.W); val = Rd(final.W); PC.W += 2	//Absolute
#define Ad_VlAx()	front.W = final.W = RdWord(PC.W); final.W += X; PC.W += 2;	\
					if(front.B.h != final.B.h) nCPUCycle++; val = Rd(final.W)	//Absolute, X [uses extra cycle if crossed page]
#define Ad_VlAy()	front.W = final.W = RdWord(PC.W); final.W += Y; PC.W += 2;	\
					if(front.B.h != final.B.h) nCPUCycle++; val = Rd(final.W)	//Absolute, X [uses extra cycle if crossed page]
#define Ad_VlIx()	front.W = final.W = Rd(PC.W); final.B.l += X; PC.W++;	\
					final.W = ZpWord(final.B.l); val = Rd(final.W)			//(Indirect, X)
#define Ad_VlIy()	val = Rd(PC.W); front.W = final.W = ZpWord(val); PC.W++;\
					final.W += Y; if(final.B.h != front.B.h) nCPUCycle++;		\
					front.W = val; val = Rd(final.W)						//(Indirect), Y [uses extra cycle if crossed page]

// second set - gets the ADDRESS that the mode is referring to (for operators that write to memory)
//              note that AbsoluteX, AbsoluteY, and IndirectY modes do NOT check for page boundary crossing here
//				since that extra cycle isn't added for operators that write to memory (it only applies to ones that
//				only read from memory.. in which case the 1st set should be used)
#define Ad_AdZp()	final.W = Rd(PC.W); PC.W++								//Zero Page
#define Ad_AdZx()	final.W = front.W = Rd(PC.W); final.B.l += X; PC.W++	//Zero Page, X
#define Ad_AdZy()	final.W = front.W = Rd(PC.W); final.B.l += Y; PC.W++	//Zero Page, Y
#define Ad_AdAb()	final.W = RdWord(PC.W); PC.W += 2						//Absolute
#define Ad_AdAx()	front.W = final.W = RdWord(PC.W); PC.W += 2;			\
					final.W += X											//Absolute, X
#define Ad_AdAy()	front.W = final.W = RdWord(PC.W); PC.W += 2;			\
					final.W += Y											//Absolute, Y
#define Ad_AdIx()	front.W = final.W = Rd(PC.W); PC.W++; final.B.l += X;	\
					final.W = ZpWord(final.B.l)								//(Indirect, X)
#define Ad_AdIy()	front.W = Rd(PC.W); final.W = ZpWord(front.W) + Y;		\
					PC.W++													//(Indirect), Y

// third set - reads memory, performs the desired operation on the value, then writes back to memory
//				used for operators that directly change memory (ASL, INC, DEC, etc)
#define MRW_Zp(cmd)	Ad_AdZp(); val = Zp(final.W); cmd(val); WrZ(final.W,val)	//Zero Page
#define MRW_Zx(cmd) Ad_AdZx(); val = Zp(final.W); cmd(val); WrZ(final.W,val)	//Zero Page, X
#define MRW_Zy(cmd) Ad_AdZy(); val = Zp(final.W); cmd(val); WrZ(final.W,val)	//Zero Page, Y
#define MRW_Ab(cmd)	Ad_AdAb(); val = Rd(final.W); cmd(val); Wr(final.W,val)		//Absolute
#define MRW_Ax(cmd)	Ad_AdAx(); val = Rd(final.W); cmd(val); Wr(final.W,val)		//Absolute, X
#define MRW_Ay(cmd)	Ad_AdAy(); val = Rd(final.W); cmd(val); Wr(final.W,val)		//Absolute, Y
#define MRW_Ix(cmd)	Ad_AdIx(); val = Rd(final.W); cmd(val); Wr(final.W,val)		//(Indirect, X)
#define MRW_Iy(cmd)	Ad_AdIy(); val = Rd(final.W); cmd(val); Wr(final.W,val)		//(Indirect), Y

// Relative modes are special in that they're only used by branch commands
//  this macro handles the jump, and should only be called if the branch condition was true
//  if the branch condition was false, the PC must be incrimented

#define RelJmp(cond)	val = Rd(PC.W); PC.W++; final.W = (val & 0x80 ? PC.W - (255 - val + 1) : PC.W + val);						\
						if(cond) { nCPUCycle += ((final.B.h != PC.B.h) ? 2 : 1); PC.W = final.W; }



//////////////////////////////////////////////////////////////////////////
//  Status Flags

#define		C_FLAG		0x01			//carry flag
#define		Z_FLAG		0x02			//zero flag
#define		I_FLAG		0x04			//mask interrupt flag
#define		D_FLAG		0x08			//decimal flag (decimal mode is unsupported on NES)
#define		B_FLAG		0x10			//break flag (not really in the status register!  It's value in ST is never used.  When ST is put in memory (by an interrupt or PHP), this flag is set only if BRK was called) ** also when PHP is called due to a bug
#define		R_FLAG		0x20			//reserved flag (not really in the register.  It's value is never used.  Whenever ST is put in memory, this flag is always set)
#define		V_FLAG		0x40			//overflow flag
#define		N_FLAG		0x80			//sign flag


//////////////////////////////////////////////////////////////////////////
//  Lookup Tables

static const BYTE CPU_Cycles[0x100] = {		//the number of CPU cycles used for each instruction
7,6,0,8,3,3,5,5,3,2,2,2,4,4,6,6,
2,5,0,8,4,4,6,6,2,4,2,7,4,4,7,7,
6,6,0,8,3,3,5,5,4,2,2,2,4,4,6,6,
2,5,0,8,4,4,6,6,2,4,2,7,4,4,7,7,
6,6,0,8,3,3,5,5,3,2,2,2,3,4,6,6,
2,5,0,8,4,4,6,6,2,4,2,7,4,4,7,7,
6,6,0,8,3,3,5,5,4,2,2,2,5,4,6,6,
2,5,0,8,4,4,6,6,2,4,2,7,4,4,7,7,
2,6,2,6,3,3,3,3,2,2,2,2,4,4,4,4,
2,6,0,6,4,4,4,4,2,5,2,5,5,5,5,5,
2,6,2,6,3,3,3,3,2,2,2,2,4,4,4,4,
2,5,0,5,4,4,4,4,2,4,2,4,4,4,4,4,
2,6,2,8,3,3,5,5,2,2,2,2,4,4,6,6,
2,5,0,8,4,4,6,6,2,4,2,7,4,4,7,7,
2,6,2,8,3,3,5,5,2,2,2,2,4,4,6,6,
2,5,0,8,4,4,6,6,2,4,2,7,4,4,7,7		};


static const BYTE NZTable[0x100] = {		//the status of the NZ flags for the given value
Z_FLAG,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,
N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,
N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,
N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,
N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,
N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,
N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,
N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG,N_FLAG	};

//  A quick macro for working with the above table
#define UpdateNZ(v)	ST = (ST & ~(N_FLAG|Z_FLAG)) | NZTable[v]


//////////////////////////////////////////////////////////////////////////
//  Opcodes
//
//		These opcodes perform the action with the given value (changing that value
//	if necessary).  Registers and flags associated with the operation are
//	changed accordingly.  There are a few exceptions which will be noted when they arise


/*  ADC
		Adds the value to the accumulator with carry
		Changes:  A, NVZC
		- Decimal mode not supported on the NES
		- Due to a bug, NVZ flags are not altered if the Decimal flag is on	--(taken out)-- */
#define ADC()															\
	tw.W = A + val + (ST & C_FLAG);										\
	ST = (ST & (I_FLAG|D_FLAG)) | tw.B.h | NZTable[tw.B.l] |			\
		( (0x80 & ~(A ^ val) & (A ^ tw.B.l)) ? V_FLAG : 0 );			\
	A = tw.B.l

/*	AND
		Combines the value with the accumulator using a bitwise AND operation
		Changes:  A, NZ		*/
#define AND()															\
	A &= val;															\
	UpdateNZ(A)

/*	ASL
		Left shifts the value 1 bit.  The bit that gets shifted out goes to
		the carry flag.
		Changes:  value, NZC		*/
#define ASL(value)														\
	tw.W = value << 1;													\
	ST = (ST & ~(N_FLAG|Z_FLAG|C_FLAG)) | tw.B.h | NZTable[tw.B.l];		\
	value = tw.B.l

/*	BIT
		Compares memory with the accumulator with an AND operation, but changes neither.
		The two high bits of memory get transferred to the status reg
		Z is set if the AND operation yielded zero, otherwise it's cleared
		Changes:  NVZ				*/
#define BIT()															\
	ST = (ST & ~(N_FLAG|V_FLAG|Z_FLAG)) | (val & (N_FLAG|V_FLAG)) |		\
			((A & val) ? 0 : Z_FLAG)

/*	CMP, CPX, CPY
		Compares memory with the given register with a subtraction operation.
		Flags are set accordingly depending on the result:
		Reg < Memory:  Z=0, C=0
		Reg = Memory:  Z=1, C=1
		Reg > Memory:  Z=0, C=1
		N is set according to the result of the subtraction operation
		Changes:  NZC

		NOTE -- CMP, CPX, CPY all share this same routine, so the desired register
				(A, X, or Y respectively) must be given when calling this macro... as well
				as the memory to compare it with.		*/
#define CMP(reg)														\
	tw.W = reg - val;													\
	ST = (ST & ~(N_FLAG|Z_FLAG|C_FLAG)) | (tw.B.h ? 0 : C_FLAG) |		\
			NZTable[tw.B.l]

/*	DEC, DEX, DEY
		Decriments a value by one.
		Changes:  value, NZ				*/
#define DEC(value)														\
	value--;															\
	UpdateNZ(value)

/*	EOR
		Combines a value with the accumulator using a bitwise exclusive-OR operation
		Changes:  A, NZ					*/
#define EOR()															\
	A ^= val;															\
	UpdateNZ(A)

/*	INC, INX, INY
		Incriments a value by one.
		Changes:  value, NZ				*/
#define INC(value)														\
	value++;															\
	UpdateNZ(value)

/*	LSR
		Shifts value one bit to the right.  Bit that gets shifted out goes to the
		Carry flag.
		Changes:  value, NZC			*/
#define LSR(value)														\
	tw.W = value >> 1;													\
	ST = (ST & ~(N_FLAG|Z_FLAG|C_FLAG)) | NZTable[tw.B.l] |				\
		(value & 0x01);													\
	value = tw.B.l

/*	ORA
		Combines a value with the accumulator using a bitwise inclusive-OR operation
		Changes:  A, NZ					*/
#define ORA()															\
	A |= val;															\
	UpdateNZ(A)

/*	ROL
		Rotates a value one bit to the left:
		C <-   7<-6<-5<-4<-3<-2<-1<-0    <- C
		Changes:  value, NZC			*/
#define ROL(value)														\
	tw.W = (value << 1) | (ST & 0x01);									\
	ST = (ST & ~(N_FLAG|Z_FLAG|C_FLAG)) | NZTable[tw.B.l] | tw.B.h;		\
	value = tw.B.l

/*	ROR
		Rotates a value one bit to the right:
		C ->   7->6->5->4->3->2->1->0   -> C
		Changes:  value, NZC			*/
#define ROR(value)														\
	tw.W = (value >> 1) | (ST << 7);									\
	ST = (ST & ~(N_FLAG|Z_FLAG|C_FLAG)) | NZTable[tw.B.l] |				\
		(value & 0x01);													\
	value = tw.B.l

/*	SBC
		Subtracts a value from the accumulator with borrow (inverted carry)
		Changes:  A, NVZC
		- Decimal mode not supported on the NES
		- Due to a bug, NVZ flags are not altered if the Decimal flag is on	--(taken out)-- */
#define SBC()																\
	tw.W = A - val - ((ST & C_FLAG) ? 0 : 1);								\
	ST = (ST & (I_FLAG|D_FLAG)) | (tw.B.h ? 0 : C_FLAG) | NZTable[tw.B.l] |	\
					(((A ^ val) & (A ^ tw.B.l) & 0x80) ? V_FLAG : 0);		\
	A = tw.B.l

//////////////////////////////////////////////////////////////////////////
//  Undocumented Opcodes
//
//		These opcodes are not included in the official specifications.  However,
//	some of the unused opcode values perform operations which have since been
//	documented.


/*	ASO
		Left shifts a value, then ORs the result with the accumulator
		Changes:  value, A, NZC											*/
#define ASO(value)														\
	tw.W = value << 1;													\
	A |= tw.B.l;														\
	ST = (ST & ~(N_FLAG|Z_FLAG|C_FLAG)) | NZTable[A] | tw.B.h;			\
	value = tw.B.l

/*	RLA
		Roll memory left 1 bit, then AND the result with the accumulator
		Changes:  value, A, NZC											*/
#define RLA(value)														\
	tw.W = (value << 1) | (ST & 0x01);									\
	A &= tw.B.l;														\
	ST = (ST & ~(N_FLAG|Z_FLAG|C_FLAG)) | NZTable[A] | tw.B.h;			\
	value = tw.B.l

/*	LSE
		Right shifts a value one bit, then EORs the result with the accumulator
		Changes:  value, A, NZC											*/
#define LSE(value)														\
	tw.W = value >> 1;													\
	A ^= tw.B.l;														\
	ST = (ST & ~(N_FLAG|Z_FLAG|C_FLAG)) | NZTable[A] | (value & 0x01);	\
	value = tw.B.l

/*	RRA
		Roll memory right one bit, then ADC the result
		Changes:  value, A, NVZC										*/
#define RRA(value)														\
	tw.W = (value >> 1) | (ST << 7);									\
	ST = (ST & ~C_FLAG) | (value & 0x01);								\
	value = tw.B.l;														\
	ADC()

/*	AXS
		ANDs the contents of the X and A registers and stores the result
		int memory.
		Changes:  value  [DOES NOT CHANGE X, A, or any flags]			*/
#define AXS(value)														\
	value = A & X

/*	DCM
		Decriments a value and compares it with the A register.
		Changes:  value, NZC											*/
#define DCM(value)															\
	value--;																\
	CMP(A)

/*	INS
		Incriments a value then SBCs it
		Changes:  value, A, NVZC										*/
#define INS(value)														\
	value++;															\
	SBC()

/*	AXA		*/
#define AXA(value)														\
	value = A & X & (Rd(PC.W - 1) + 1)


//////////////////////////////////////////////////////////////////////////
//
//		The 6502 emulation function!
//
//

TWIN front;
TWIN final;
BYTE val;
BYTE op;

UINT CNSFCore::Emulate6502(UINT runto)
{
	/////////////////////////////////////////
	//  If the CPU is jammed... don't bother
	if(bCPUJammed == 1)
		return 0;

	register TWIN	tw;		//used in calculations
	register BYTE	ST = regP;
	register TWIN	PC;
	BYTE			SP = regSP;
	register BYTE	A = regA;
	register BYTE	X = regX;
	register BYTE	Y = regY;
	TWIN			front;
	TWIN			final;
	PC.W = regPC;

	UINT ret = nCPUCycle;

	////////////////////
	//  Start the loop

	while(nCPUCycle < runto)
	{
		op = Rd(PC.W);
		PC.W++;

		nCPUCycle += CPU_Cycles[op];
		switch(op)
		{
			//////////////////////////////////////////////////////////////////////////
			//  Documented Opcodes first
			
			//////////////////////////////////////////////////////////////////////////
			//  Flag setting/clearing
		case 0x18:	ST &= ~C_FLAG;	break;		/* CLC	*/
		case 0x38:	ST |=  C_FLAG;	break;		/* SEC	*/
		case 0x58:	ST &= ~I_FLAG;	break;		/* CLI	*/
		case 0x78:	ST |=  I_FLAG;	break;		/* SEI	*/
		case 0xB8:	ST &= ~V_FLAG;	break;		/* CLV	*/
		case 0xD8:	ST &= ~D_FLAG;	break;		/* CLD	*/
		case 0xF8:	ST |=  D_FLAG;	break;		/* SED	*/

			//////////////////////////////////////////////////////////////////////////
			//  Branch commands
		case 0x10:	RelJmp(!(ST & N_FLAG)); break;							/* BPL	*/
		case 0x30:	RelJmp( (ST & N_FLAG)); break;							/* BMI	*/
		case 0x50:	RelJmp(!(ST & V_FLAG)); break;							/* BVC	*/
		case 0x70:	RelJmp( (ST & V_FLAG)); break;							/* BVS	*/
		case 0x90:	RelJmp(!(ST & C_FLAG)); break;							/* BCC	*/
		case 0xB0:	RelJmp( (ST & C_FLAG)); break;							/* BCS	*/
		case 0xD0:	RelJmp(!(ST & Z_FLAG)); break;							/* BNE	*/
		case 0xF0:	RelJmp( (ST & Z_FLAG)); break;							/* BEQ	*/

			//////////////////////////////////////////////////////////////////////////
			//  Direct stack alteration commands (push/pull commands)

		case 0x08:	PUSH(ST | R_FLAG | B_FLAG);						break;	/* PHP	*/
		case 0x28:	PULL(ST);										break;	/* PLP	*/
		case 0x48:	PUSH(A);										break;	/* PHA	*/
		case 0x68:	PULL(A); UpdateNZ(A);							break;	/* PLA	*/

			//////////////////////////////////////////////////////////////////////////
			//  Register Transfers

		case 0x8A:	A = X;	UpdateNZ(A);							break;	/* TXA	*/
		case 0x98:	A = Y;	UpdateNZ(A);							break;	/* TYA	*/
		case 0x9A:	SP = X;											break;	/* TXS	*/
		case 0xA8:	Y = A;	UpdateNZ(A);							break;	/* TAY	*/
		case 0xAA:	X = A;	UpdateNZ(A);							break;	/* TAX	*/
		case 0xBA:	X = SP;	UpdateNZ(X);							break;	/* TSX	*/


			//////////////////////////////////////////////////////////////////////////
			//  Other commands

			/* ADC	*/
		case 0x61:	Ad_VlIx();	ADC();	break;
		case 0x65:	Ad_VlZp();	ADC();	break;
		case 0x69:	Ad_VlIm();	ADC();	break;
		case 0x6D:	Ad_VlAb();	ADC();	break;
		case 0x71:	Ad_VlIy();	ADC();	break;
		case 0x75:	Ad_VlZx();	ADC();	break;
		case 0x79:	Ad_VlAy();	ADC();	break;
		case 0x7D:	Ad_VlAx();	ADC();	break;

			/* AND	*/
		case 0x21:	Ad_VlIx();	AND();	break;
		case 0x25:	Ad_VlZp();	AND();	break;
		case 0x29:	Ad_VlIm();	AND();	break;
		case 0x2D:	Ad_VlAb();	AND();	break;
		case 0x31:	Ad_VlIy();	AND();	break;
		case 0x35:	Ad_VlZx();	AND();	break;
		case 0x39:	Ad_VlAy();	AND();	break;
		case 0x3D:	Ad_VlAx();	AND();	break;

			/* ASL	*/
		case 0x0A:	ASL(A);						break;
		case 0x06:	MRW_Zp(ASL);				break;
		case 0x0E:	MRW_Ab(ASL);				break;
		case 0x16:	MRW_Zx(ASL);				break;
		case 0x1E:	MRW_Ax(ASL);				break;

			/* BIT	*/
		case 0x24:	Ad_VlZp();	BIT();	break;
		case 0x2C:	Ad_VlAb();	BIT();	break;

			/* BRK	*/
		case 0x00:
			if(bIgnoreBRK)
				break;
			PC.W++;							//BRK has a padding byte
			PUSH(PC.B.h);					//push high byte of the return address
			PUSH(PC.B.l);					//push low byte of return address
			PUSH(ST | R_FLAG | B_FLAG);		//push processor status with R|B flags
			ST |= I_FLAG;					//mask interrupts
			PC.W = RdWord(0xFFFE);			//read the IRQ vector and jump to it

			//extra check to make sure we didn't hit an infinite BRK loop
			if(!Rd(PC.W))					//next command will be BRK
			{
				bCPUJammed = 1;				//the CPU will endlessly loop... just just jam it to ease processing power
				goto jammed;
			}
			break;

			/* CMP	*/
		case 0xC1:	Ad_VlIx();	CMP(A);	break;
		case 0xC5:	Ad_VlZp();	CMP(A);	break;
		case 0xC9:	Ad_VlIm();	CMP(A); break;
		case 0xCD:	Ad_VlAb();	CMP(A);	break;
		case 0xD1:	Ad_VlIy();	CMP(A);	break;
		case 0xD5:	Ad_VlZx();	CMP(A);	break;
		case 0xD9:	Ad_VlAy();	CMP(A);	break;
		case 0xDD:	Ad_VlAx();	CMP(A);	break;

			/* CPX	*/
		case 0xE0:	Ad_VlIm();	CMP(X);	break;
		case 0xE4:	Ad_VlZp();	CMP(X);	break;
		case 0xEC:	Ad_VlAb();	CMP(X);	break;

			/* CPY	*/
		case 0xC0:	Ad_VlIm();	CMP(Y);	break;
		case 0xC4:	Ad_VlZp();	CMP(Y);	break;
		case 0xCC:	Ad_VlAb();	CMP(Y);	break;

			/* DEC	*/
		case 0xCA:	DEC(X);						break;		/* DEX	*/
		case 0x88:	DEC(Y);						break;		/* DEY	*/
		case 0xC6:	MRW_Zp(DEC);				break;
		case 0xCE:	MRW_Ab(DEC);				break;
		case 0xD6:	MRW_Zx(DEC);				break;
		case 0xDE:	MRW_Ax(DEC);				break;

			/* EOR	*/
		case 0x41:	Ad_VlIx();	EOR();	break;
		case 0x45:	Ad_VlZp();	EOR();	break;
		case 0x49:	Ad_VlIm();	EOR();	break;
		case 0x4D:	Ad_VlAb();	EOR();	break;
		case 0x51:	Ad_VlIy();	EOR();	break;
		case 0x55:	Ad_VlZx();	EOR();	break;
		case 0x59:	Ad_VlAy();	EOR();	break;
		case 0x5D:	Ad_VlAx();	EOR();	break;

			/* INC	*/
		case 0xE8:	INC(X);						break;		/* INX	*/
		case 0xC8:	INC(Y);						break;		/* INY	*/
		case 0xE6:	MRW_Zp(INC);				break;
		case 0xEE:	MRW_Ab(INC);				break;
		case 0xF6:	MRW_Zx(INC);				break;
		case 0xFE:	MRW_Ax(INC);				break;

			/* JMP	*/
		case 0x4C:	final.W = RdWord(PC.W);  PC.W = final.W; val = 0;	break;		/* Absolute JMP	*/
		case 0x6C:	front.W = final.W = RdWord(PC.W);
					PC.B.l = Rd(final.W); final.B.l++;
					PC.B.h = Rd(final.W); final.W = PC.W;
					break;		/* Indirect JMP -- must take caution:
										Indirection at 01FF will read from 01FF and 0100 (not 0200) */
			/* JSR	*/
		case 0x20:
			val = 0;
			final.W = RdWord(PC.W);
			PC.W++;				//JSR only incriments the return address by one.  It's incrimented again upon RTS
			PUSH(PC.B.h);		//push high byte of return address
			PUSH(PC.B.l);		//push low byte of return address
			PC.W = final.W;
			break;

			/* LDA	*/
		case 0xA1:	Ad_VlIx(); A = val; UpdateNZ(A);	break;
		case 0xA5:	Ad_VlZp(); A = val; UpdateNZ(A);	break;
		case 0xA9:	Ad_VlIm(); A = val; UpdateNZ(A);	break;
		case 0xAD:	Ad_VlAb(); A = val; UpdateNZ(A);	break;
		case 0xB1:	Ad_VlIy(); A = val; UpdateNZ(A);	break;
		case 0xB5:	Ad_VlZx(); A = val; UpdateNZ(A);	break;
		case 0xB9:	Ad_VlAy(); A = val; UpdateNZ(A);	break;
		case 0xBD:	Ad_VlAx(); A = val; UpdateNZ(A);	break;

			/* LDX	*/
		case 0xA2:	Ad_VlIm(); X = val; UpdateNZ(X);	break;
		case 0xA6:	Ad_VlZp(); X = val; UpdateNZ(X);	break;
		case 0xAE:	Ad_VlAb(); X = val; UpdateNZ(X);	break;
		case 0xB6:	Ad_VlZy(); X = val; UpdateNZ(X);	break;
		case 0xBE:	Ad_VlAy(); X = val; UpdateNZ(X);	break;

			/* LDY	*/
		case 0xA0:	Ad_VlIm(); Y = val; UpdateNZ(Y);	break;
		case 0xA4:	Ad_VlZp(); Y = val; UpdateNZ(Y);	break;
		case 0xAC:	Ad_VlAb(); Y = val; UpdateNZ(Y);	break;
		case 0xB4:	Ad_VlZx(); Y = val; UpdateNZ(Y);	break;
		case 0xBC:	Ad_VlAx(); Y = val; UpdateNZ(Y);	break;

			/* LSR	*/
		case 0x4A:	LSR(A);						break;
		case 0x46:	MRW_Zp(LSR);				break;
		case 0x4E:	MRW_Ab(LSR);				break;
		case 0x56:	MRW_Zx(LSR);				break;
		case 0x5E:	MRW_Ax(LSR);				break;

			/* NOP	*/
		case 0xEA:

			/* --- Undocumented ---
				These opcodes perform the same action as NOP	*/
		case 0x1A:	case 0x3A:	case 0x5A:
		case 0x7A:	case 0xDA:	case 0xFA:		break;

			/* ORA	*/
		case 0x01:	Ad_VlIx();	ORA();	break;
		case 0x05:	Ad_VlZp();	ORA();	break;
		case 0x09:	Ad_VlIm();	ORA();	break;
		case 0x0D:	Ad_VlAb();	ORA();	break;
		case 0x11:	Ad_VlIy();	ORA();	break;
		case 0x15:	Ad_VlZx();	ORA();	break;
		case 0x19:	Ad_VlAy();	ORA();	break;
		case 0x1D:	Ad_VlAx();	ORA();	break;

			/* ROL	*/
		case 0x2A:	ROL(A);						break;
		case 0x26:	MRW_Zp(ROL);				break;
		case 0x2E:	MRW_Ab(ROL);				break;
		case 0x36:	MRW_Zx(ROL);				break;
		case 0x3E:	MRW_Ax(ROL);				break;

			/* ROR	*/
		case 0x6A:	ROR(A);						break;
		case 0x66:	MRW_Zp(ROR);				break;
		case 0x6E:	MRW_Ab(ROR);				break;
		case 0x76:	MRW_Zx(ROR);				break;
		case 0x7E:	MRW_Ax(ROR);				break;

			/* RTI	*/
		case 0x40:
			PULL(ST);						//pull processor status
			PULL(PC.B.l);					//pull low byte of return address
			PULL(PC.B.h);					//pull high byte of return address
			break;

			/* RTS	*/
		case 0x60:
			PULL(PC.B.l);
			PULL(PC.B.h);
			PC.W++;				//the return address is one less of what it needs
			break;

			/* SBC	*/
		case 0xE1:	Ad_VlIx();	SBC();	break;
		case 0xE5:	Ad_VlZp();	SBC();	break;
		case 0xEB:										/* -- Undocumented --  EB performs the same operation as SBC immediate */
		case 0xE9:	Ad_VlIm();	SBC();	break;
		case 0xED:	Ad_VlAb();	SBC();	break;
		case 0xF1:	Ad_VlIy();	SBC();	break;
		case 0xF5:	Ad_VlZx();	SBC();	break;
		case 0xF9:	Ad_VlAy();	SBC();	break;
		case 0xFD:	Ad_VlAx();	SBC();	break;

			/* STA	*/
		case 0x81:	Ad_AdIx(); val = A; Wr(final.W,A);	break;
		case 0x85:	Ad_AdZp(); val = A; WrZ(final.W,A);	break;
		case 0x8D:	Ad_AdAb(); val = A; Wr(final.W,A);	break;
		case 0x91:	Ad_AdIy(); val = A; Wr(final.W,A);	break;
		case 0x95:	Ad_AdZx(); val = A; WrZ(final.W,A);	break;
		case 0x99:	Ad_AdAy(); val = A; Wr(final.W,A);	break;
		case 0x9D:	Ad_AdAx(); val = A; Wr(final.W,A);	break;

			/* STX	*/
		case 0x86:	Ad_AdZp(); val = X; WrZ(final.W,X);	break;
		case 0x8E:	Ad_AdAb(); val = X; Wr(final.W,X);	break;
		case 0x96:	Ad_AdZy(); val = X; WrZ(final.W,X);	break;

			/* STY	*/
		case 0x84:	Ad_AdZp(); val = Y; WrZ(final.W,Y);	break;
		case 0x8C:	Ad_AdAb(); val = Y; Wr(final.W,Y);	break;
		case 0x94:	Ad_AdZx(); val = Y; WrZ(final.W,Y);	break;


			//////////////////////////////////////////////////////////////////////////
			//  Undocumented Opcodes
			/* ASO	*/
		case 0x03:	if(bIgnoreIllegalOps) break;	MRW_Ix(ASO);				break;
		case 0x07:	if(bIgnoreIllegalOps) break;	MRW_Zp(ASO);				break;
		case 0x0F:	if(bIgnoreIllegalOps) break;	MRW_Ab(ASO);				break;
		case 0x13:	if(bIgnoreIllegalOps) break;	MRW_Iy(ASO);				break;
		case 0x17:	if(bIgnoreIllegalOps) break;	MRW_Zx(ASO);				break;
		case 0x1B:	if(bIgnoreIllegalOps) break;	MRW_Ay(ASO);				break;
		case 0x1F:	if(bIgnoreIllegalOps) break;	MRW_Ax(ASO);				break;

			/* RLA	*/
		case 0x23:	if(bIgnoreIllegalOps) break;	MRW_Ix(RLA);				break;
		case 0x27:	if(bIgnoreIllegalOps) break;	MRW_Zp(RLA);				break;
		case 0x2F:	if(bIgnoreIllegalOps) break;	MRW_Ab(RLA);				break;
		case 0x33:	if(bIgnoreIllegalOps) break;	MRW_Iy(RLA);				break;
		case 0x37:	if(bIgnoreIllegalOps) break;	MRW_Zx(RLA);				break;
		case 0x3B:	if(bIgnoreIllegalOps) break;	MRW_Ay(RLA);				break;
		case 0x3F:	if(bIgnoreIllegalOps) break;	MRW_Ax(RLA);				break;

			/* LSE	*/
		case 0x43:	if(bIgnoreIllegalOps) break;	MRW_Ix(LSE);				break;
		case 0x47:	if(bIgnoreIllegalOps) break;	MRW_Zp(LSE);				break;
		case 0x4F:	if(bIgnoreIllegalOps) break;	MRW_Ab(LSE);				break;
		case 0x53:	if(bIgnoreIllegalOps) break;	MRW_Iy(LSE);				break;
		case 0x57:	if(bIgnoreIllegalOps) break;	MRW_Zx(LSE);				break;
		case 0x5B:	if(bIgnoreIllegalOps) break;	MRW_Ay(LSE);				break;
		case 0x5F:	if(bIgnoreIllegalOps) break;	MRW_Ax(LSE);				break;

			/* RRA	*/
		case 0x63:	if(bIgnoreIllegalOps) break;	MRW_Ix(RRA);				break;
		case 0x67:	if(bIgnoreIllegalOps) break;	MRW_Zp(RRA);				break;
		case 0x6F:	if(bIgnoreIllegalOps) break;	MRW_Ab(RRA);				break;
		case 0x73:	if(bIgnoreIllegalOps) break;	MRW_Iy(RRA);				break;
		case 0x77:	if(bIgnoreIllegalOps) break;	MRW_Zx(RRA);				break;
		case 0x7B:	if(bIgnoreIllegalOps) break;	MRW_Ay(RRA);				break;
		case 0x7F:	if(bIgnoreIllegalOps) break;	MRW_Ax(RRA);				break;

			/* AXS	*/
		case 0x83:	if(bIgnoreIllegalOps) break;	MRW_Ix(AXS);				break;
		case 0x87:	if(bIgnoreIllegalOps) break;	MRW_Zp(AXS);				break;
		case 0x8F:	if(bIgnoreIllegalOps) break;	MRW_Ab(AXS);				break;
		case 0x97:	if(bIgnoreIllegalOps) break;	MRW_Zy(AXS);				break;

			/* LAX	*/
		case 0xA3:	if(bIgnoreIllegalOps) break;	Ad_VlIx();	X = A = val; UpdateNZ(A);	break;
		case 0xA7:	if(bIgnoreIllegalOps) break;	Ad_VlZp();	X = A = val; UpdateNZ(A);	break;
		case 0xAF:	if(bIgnoreIllegalOps) break;	Ad_VlAb();	X = A = val; UpdateNZ(A);	break;
		case 0xB3:	if(bIgnoreIllegalOps) break;	Ad_VlIy();	X = A = val; UpdateNZ(A);	break;
		case 0xB7:	if(bIgnoreIllegalOps) break;	Ad_VlZy();	X = A = val; UpdateNZ(A);	break;
		case 0xBF:	if(bIgnoreIllegalOps) break;	Ad_VlAy();	X = A = val; UpdateNZ(A);	break;

			/* DCM	*/
		case 0xC3:	if(bIgnoreIllegalOps) break;	MRW_Ix(DCM);				break;
		case 0xC7:	if(bIgnoreIllegalOps) break;	MRW_Zp(DCM);				break;
		case 0xCF:	if(bIgnoreIllegalOps) break;	MRW_Ab(DCM);				break;
		case 0xD3:	if(bIgnoreIllegalOps) break;	MRW_Iy(DCM);				break;
		case 0xD7:	if(bIgnoreIllegalOps) break;	MRW_Zx(DCM);				break;
		case 0xDB:	if(bIgnoreIllegalOps) break;	MRW_Ay(DCM);				break;
		case 0xDF:	if(bIgnoreIllegalOps) break;	MRW_Ax(DCM);				break;

			/* INS	*/
		case 0xE3:	if(bIgnoreIllegalOps) break;	MRW_Ix(INS);				break;
		case 0xE7:	if(bIgnoreIllegalOps) break;	MRW_Zp(INS);				break;
		case 0xEF:	if(bIgnoreIllegalOps) break;	MRW_Ab(INS);				break;
		case 0xF3:	if(bIgnoreIllegalOps) break;	MRW_Iy(INS);				break;
		case 0xF7:	if(bIgnoreIllegalOps) break;	MRW_Zx(INS);				break;
		case 0xFB:	if(bIgnoreIllegalOps) break;	MRW_Ay(INS);				break;
		case 0xFF:	if(bIgnoreIllegalOps) break;	MRW_Ax(INS);				break;

			/* ALR
					AND Accumulator with memory and LSR the result	*/
		case 0x4B:	if(bIgnoreIllegalOps) break;	Ad_VlIm();	A &= val;	LSR(A);	break;

			/* ARR
					ANDs memory with the Accumulator and RORs the result	*/
		case 0x6B:	if(bIgnoreIllegalOps) break;	Ad_VlIm();	A &= val;	ROR(A);	break;

			/* XAA
					Transfers X -> A, then ANDs A with memory				*/
		case 0x8B:	if(bIgnoreIllegalOps) break;	Ad_VlIm();	A = X & val; UpdateNZ(A);	break;

			/* OAL
					OR the Accumulator with #EE, AND Accumulator with Memory, Transfer A -> X	*/
		case 0xAB:	if(bIgnoreIllegalOps) break;	Ad_VlIm();	X = (A &= (val | 0xEE));
													UpdateNZ(A);	break;

			/* SAX
					ANDs A and X registers (does not change A), subtracts memory from result (CMP style, not SBC style)
					result is stored in X								*/
		case 0xCB:	if(bIgnoreIllegalOps) break;
				Ad_VlIm();	tw.W = (X & A) - val; X = tw.B.l;
					ST = (ST & ~(N_FLAG|Z_FLAG|C_FLAG)) | NZTable[X] | (tw.B.h ? C_FLAG : 0);	break;

			/* SKB
					Skip Byte... or DOP - Double No-Op
					These bytes do nothing, but take a parameter (which can be ignored)	*/
		case 0x04:	case 0x14:	case 0x34:	case 0x44:	case 0x54:	case 0x64:
		case 0x80:	case 0x82:	case 0x89:	case 0xC2:	case 0xD4:	case 0xE2:	case 0xF4:
			if(bIgnoreIllegalOps) break;
			PC.W++;		//skip unused byte
			break;

			/* SKW
					Swip Word... or TOP - Tripple No-Op
					These bytes are the same as SKB, only they take a 2 byte parameter.
					This can be ignored in some cases, but the read needs to be performed in a some cases
					because an extra clock cycle may be used in the process		*/
		case 0x0C:		//Absolute address... no need for operator
			if(bIgnoreIllegalOps) break;
			PC.W += 2;	break;
		case 0x1C:	case 0x3C:	case 0x5C:	case 0x7C:	case 0xDC:	case 0xFC:	//Absolute X address... may cross page, have to perform the read
			if(bIgnoreIllegalOps) break;
			Ad_VlAx(); break;

			/* HLT / JAM
					Jams up CPU operation			*/
		case 0x02:	case 0x12:	case 0x22:	case 0x32:	case 0x42:	case 0x52:
		case 0x62:	case 0x72:	case 0x92:	case 0xB2:	case 0xD2:	case 0xF2:
			if(PC.W == 0x5004)	bCPUJammed = 2;		//it's not -really- jammed... only the NSF code has ended
			else
			{
				if(bIgnoreIllegalOps) break;
				bCPUJammed = 1;
			}
			goto jammed;

			/* TAS	*/
		case 0x9B:
			if(bIgnoreIllegalOps) break;
			Ad_AdAy();
			SP = A & X & (Rd(PC.W - 1) + 1);
			Wr(final.W,SP);
			break;

			/* SAY	*/
		case 0x9C:
			if(bIgnoreIllegalOps) break;
			Ad_AdAx();
			Y &= (Rd(PC.W - 1) + 1);
			Wr(final.W,Y);
			break;

			/* XAS	*/
		case 0x9E:
			if(bIgnoreIllegalOps) break;
			Ad_AdAy();
			X &= (Rd(PC.W - 1) + 1);
			Wr(final.W,X);
			break;

			/* AXA	*/
		case 0x93:	if(bIgnoreIllegalOps) break;	MRW_Iy(AXA);					break;
		case 0x9F:	if(bIgnoreIllegalOps) break;	MRW_Ay(AXA);					break;

			/* ANC	*/
		case 0x0B:	case 0x2B:
			if(bIgnoreIllegalOps) break;
			Ad_VlIm();
			A &= val;
			ST = (ST & ~(N_FLAG|Z_FLAG|C_FLAG)) | NZTable[A] | ((A & 0x80) ? C_FLAG : 0);
			break;

			/* LAS	*/
		case 0xBB:
			if(bIgnoreIllegalOps) break;
			Ad_VlAy();
			X = A = (SP &= val);
			UpdateNZ(A);
			break;
		}
	}

jammed:
	regPC = PC.W;
	regA = A;
	regX = X;
	regY = Y;
	regSP = SP;
	regP = ST;

	return (nCPUCycle - ret);
}