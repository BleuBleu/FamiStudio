
// Nes_Snd_Emu 0.1.7. http://www.slack.net/~ant/libs/

#include "Nes_Vrc6.h"

/* Copyright (C) 2003-2005 Shay Green. This module is free software; you
can redistribute it and/or modify it under the terms of the GNU Lesser
General Public License as published by the Free Software Foundation; either
version 2.1 of the License, or (at your option) any later version. This
module is distributed in the hope that it will be useful, but WITHOUT ANY
WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public License for
more details. You should have received a copy of the GNU Lesser General
Public License along with this module; if not, write to the Free Software
Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA */

#include BLARGG_SOURCE_BEGIN

Nes_Vrc6::Nes_Vrc6()
{
	output( NULL );
	volume( 1.0 );
	reset();
}

Nes_Vrc6::~Nes_Vrc6()
{
}

void Nes_Vrc6::reset()
{
	last_time = 0;
	for ( int i = 0; i < osc_count; i++ )
	{
		Vrc6_Osc& osc = oscs [i];
		for ( int j = 0; j < reg_count; j++ )
		{
			osc.regs [j] = 0;
			osc.ages [j] = 0;
		}
		osc.delay = 0;
		osc.last_amp = 0;
		osc.phase = i == 2 ? 1 : 0;
		osc.amp = 0;
		osc.trigger = trigger_hold;
	}
}

void Nes_Vrc6::volume( double v )
{
	saw_synth.volume( v * 0.333 );
	square_synth.volume( v * 0.169 );
}

void Nes_Vrc6::treble_eq( blip_eq_t const& eq )
{
	saw_synth.treble_eq( eq );
	square_synth.treble_eq( eq );
}

void Nes_Vrc6::output( Blip_Buffer* buf )
{
	for ( int i = 0; i < osc_count; i++ )
		osc_output( i, buf );
}

void Nes_Vrc6::run_until( cpu_time_t time )
{
	require( time >= last_time );
	run_square( oscs [0], time );
	run_square( oscs [1], time );
	run_saw( time );
	last_time = time;
}

void Nes_Vrc6::write_osc( cpu_time_t time, int osc_index, int reg, int data )
{
	require( (unsigned) osc_index < osc_count );
	require( (unsigned) reg < reg_count );
	
	run_until( time );
	oscs [osc_index].regs [reg] = data;
	oscs [osc_index].ages [reg] = 0;

	if (reg == 2 && (data & 0x80) == 0)
	{
		// MATTT : This is wrong. VRC6 squares don't do a full cycles the first time 
		// they run. This is a bug and needs to be fixed. Need to review saw as well,
		// i doubt it works correctly. Phase is initialized at 1 in constructor + reset,
		// review that too.
		oscs[osc_index].phase = osc_index == 2 ? 1 : 0;
		oscs[osc_index].amp = 0;
	}
}

void Nes_Vrc6::write_register(cpu_time_t time, cpu_addr_t addr, int data)
{
	for (int i = 0; i < 3; i++)
	{
		cpu_addr_t base = base_addr + addr_step * i;
		if (addr >= base && addr < base + reg_count)
		{
			write_osc(time, i, addr - base, data);
			break;
		}
	}
}

void Nes_Vrc6::end_frame( cpu_time_t time )
{
	if ( time > last_time )
		run_until( time );
	last_time -= time;
	assert( last_time >= 0 );
}

void Nes_Vrc6::save_snapshot( vrc6_snapshot_t* out ) const
{
	out->saw_amp = oscs [2].amp;
	for ( int i = 0; i < osc_count; i++ )
	{
		Vrc6_Osc const& osc = oscs [i];
		for ( int r = 0; r < reg_count; r++ )
			out->regs [i] [r] = osc.regs [r];
		
		out->delays [i] = osc.delay;
		out->phases [i] = osc.phase;
	}
}

void Nes_Vrc6::load_snapshot( vrc6_snapshot_t const& in )
{
	reset();
	oscs [2].amp = in.saw_amp;
	for ( int i = 0; i < osc_count; i++ )
	{
		Vrc6_Osc& osc = oscs [i];
		for ( int r = 0; r < reg_count; r++ )
			osc.regs [r] = in.regs [i] [r];
		
		osc.delay = in.delays [i];
		osc.phase = in.phases [i];
	}
	if ( !oscs [2].phase )
		oscs [2].phase = 1;
}

void Nes_Vrc6::reset_triggers()
{
	oscs[0].trigger = trigger_hold;
	oscs[1].trigger = trigger_hold;
	oscs[2].trigger = trigger_hold;
}

int Nes_Vrc6::get_channel_trigger(int idx) const
{
	return oscs[idx].trigger;
}

#include BLARGG_ENABLE_OPTIMIZER

void Nes_Vrc6::run_square( Vrc6_Osc& osc, cpu_time_t end_time )
{
	Blip_Buffer* output = osc.output;
	if ( !output )
	{
		osc.trigger = trigger_none;
		return;
	}

	int volume = osc.regs [0] & 15;
	if ( !(osc.regs [2] & 0x80) )
		volume = 0;
	
	int gate = osc.regs [0] & 0x80;
	int duty = ((osc.regs [0] >> 4) & 7) + 1;
	int delta = ((gate || osc.phase < duty) ? volume : 0) - osc.last_amp;
	cpu_time_t time = last_time;
	if ( delta )
	{
		osc.last_amp += delta;
		square_synth.offset( time, delta, output );
	}
	
	time += osc.delay;
	osc.delay = 0;
	int period = osc.period();
	if ( volume && !gate && period > 4 )
	{
		if ( time < end_time )
		{
			int phase = osc.phase;
			
			do
			{
				phase++;
				if ( phase == 16 )
				{
					phase = 0;
					osc.last_amp = volume;
					update_trigger(output, time, osc.trigger);
					square_synth.offset( time, volume, output );
				}
				if ( phase == duty )
				{
					osc.last_amp = 0;
					square_synth.offset( time, -volume, output );
				}
				time += period;
			}
			while ( time < end_time );
			
			osc.phase = phase;
		}
		osc.delay = time - end_time;
	}
	else
	{
		osc.trigger = trigger_none;
	}
}

void Nes_Vrc6::run_saw( cpu_time_t end_time )
{
	Vrc6_Osc& osc = oscs [2];
	Blip_Buffer* output = osc.output;
	if ( !output )
	{
		osc.trigger = trigger_none;
		return;
	}

	int amp = osc.amp;
	int amp_step = osc.regs [0] & 0x3F;
	cpu_time_t time = last_time;
	int last_amp = osc.last_amp;
	if ( !(osc.regs [2] & 0x80) || !(amp_step | amp) )
	{
		osc.delay = 0;
		int delta = (amp >> 3) - last_amp;
		last_amp = amp >> 3;
		saw_synth.offset( time, delta, output );
		osc.trigger = trigger_none;
	}
	else
	{
		time += osc.delay;
		if ( time < end_time )
		{
			int period = osc.period() * 2;
			int phase = osc.phase;
			
			do
			{
				if ( --phase == 0 )
				{
					phase = 7;
					amp = 0;
					update_trigger(output, time, osc.trigger);
				}
				
				int delta = (amp >> 3) - last_amp;
				if ( delta )
				{
					last_amp = amp >> 3;
					saw_synth.offset( time, delta, output );
				}
				
				time += period;
				amp = (amp + amp_step) & 0xFF;
			}
			while ( time < end_time );
			
			osc.phase = phase;
			osc.amp = amp;
		}
		
		osc.delay = time - end_time;
	}
	
	osc.last_amp = last_amp;
}

void Nes_Vrc6::start_seeking()
{
	memset(shadow_regs, -1, sizeof(shadow_regs));
}

void Nes_Vrc6::stop_seeking(blip_time_t& clock)
{
	for (int i = 0; i < array_count(shadow_regs); i++)
	{
		if (shadow_regs[i] >= 0)
		{
			int osc_idx = i / osc_count;
			int reg_idx = i % osc_count;

			write_register(clock += 4, base_addr + addr_step * osc_idx + reg_idx, shadow_regs[i]);
		}
	}
}

void Nes_Vrc6::write_shadow_register(int addr, int data)
{
	for (int i = 0; i < osc_count; i++)
	{
		int osc_base_addr = base_addr + addr_step * i;
		if (addr >= osc_base_addr && addr <= osc_base_addr + reg_count)
		{
			shadow_regs[i * reg_count + (addr - osc_base_addr)] = data;
			return;
		}
	}
}

void Nes_Vrc6::get_register_values(struct vrc6_register_values* regs)
{
	for (int i = 0; i < osc_count; i++)
	{
		Vrc6_Osc* osc = &oscs[i];

		for (int j = 0; j < 3; j++)
		{
			regs->regs[i * 3 + j] = osc->regs[j];
			regs->ages[i * 3 + j] = osc->ages[j];

			osc->ages[j] = increment_saturate(osc->ages[j]);
		}
	}
}
