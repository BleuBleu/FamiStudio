// Sunsoft 5B audio chip emulator for FamiStudio.
// Added to Nes_Snd_Emu by @NesBleuBleu.

#include "Nes_Sunsoft.h"

#include BLARGG_SOURCE_BEGIN

Nes_Sunsoft::Nes_Sunsoft()
{
	output( NULL );
	volume( 1.0 );
	reset();
}

Nes_Sunsoft::~Nes_Sunsoft()
{
}

void Nes_Sunsoft::reset()
{
	last_time = 0;
	for ( int i = 0; i < osc_count; i++ )
	{
		Sunsoft_Osc& osc = oscs [i];
		for ( int j = 0; j < reg_count; j++ )
			osc.regs [j] = 0;
		osc.delay = 0;
		osc.last_amp = 0;
		osc.phase = 1;
		osc.amp = 0;
	}
}

void Nes_Sunsoft::volume( double v )
{
	//v *= 0.0967 * 2;
	//saw_synth.volume( v );
	//square_synth.volume( v * 0.5 );
}

void Nes_Sunsoft::treble_eq( blip_eq_t const& eq )
{
	synth.treble_eq( eq );
}

void Nes_Sunsoft::output( Blip_Buffer* buf )
{
	for ( int i = 0; i < osc_count; i++ )
		osc_output( i, buf );
}

void Nes_Sunsoft::run_until( cpu_time_t time )
{
	require( time >= last_time );
	//run_square( oscs [0], time );
	//run_square( oscs [1], time );
	//run_saw( time );
	last_time = time;
}

void Nes_Sunsoft::write_osc( cpu_time_t time, int osc_index, int reg, int data )
{
	require( (unsigned) osc_index < osc_count );
	require( (unsigned) reg < reg_count );
	
	//run_until( time );
	//oscs [osc_index].regs [reg] = data;
}

void Nes_Sunsoft::write_register(cpu_time_t time, cpu_addr_t addr, int data)
{
	//for (int i = 0; i < 3; i++)
	//{
	//	cpu_addr_t base = base_addr + addr_step * i;
	//	if (addr >= base && addr < base + reg_count)
	//	{
	//		write_osc(time, i, addr - base, data);
	//		break;
	//	}
	//}
}

void Nes_Sunsoft::end_frame( cpu_time_t time )
{
	if ( time > last_time )
		run_until( time );
	last_time -= time;
	assert( last_time >= 0 );
}

#include BLARGG_ENABLE_OPTIMIZER
//
//void Nes_Sunsoft::run_square( Sunsoft_Osc& osc, cpu_time_t end_time )
//{
//	Blip_Buffer* output = osc.output;
//	if ( !output )
//		return;
//	
//	int volume = osc.regs [0] & 15;
//	if ( !(osc.regs [2] & 0x80) )
//		volume = 0;
//	
//	int gate = osc.regs [0] & 0x80;
//	int duty = ((osc.regs [0] >> 4) & 7) + 1;
//	int delta = ((gate || osc.phase < duty) ? volume : 0) - osc.last_amp;
//	cpu_time_t time = last_time;
//	if ( delta )
//	{
//		osc.last_amp += delta;
//		square_synth.offset( time, delta, output );
//	}
//	
//	time += osc.delay;
//	osc.delay = 0;
//	int period = osc.period();
//	if ( volume && !gate && period > 4 )
//	{
//		if ( time < end_time )
//		{
//			int phase = osc.phase;
//			
//			do
//			{
//				phase++;
//				if ( phase == 16 )
//				{
//					phase = 0;
//					osc.last_amp = volume;
//					square_synth.offset( time, volume, output );
//				}
//				if ( phase == duty )
//				{
//					osc.last_amp = 0;
//					square_synth.offset( time, -volume, output );
//				}
//				time += period;
//			}
//			while ( time < end_time );
//			
//			osc.phase = phase;
//		}
//		osc.delay = time - end_time;
//	}
//}
