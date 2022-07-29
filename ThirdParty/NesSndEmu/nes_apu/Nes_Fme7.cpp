// Game_Music_Emu 0.5.2. http://www.slack.net/~ant/

#include "Nes_Fme7.h"

#include <string.h>

/* Copyright (C) 2003-2006 Shay Green. This module is free software; you
can redistribute it and/or modify it under the terms of the GNU Lesser
General Public License as published by the Free Software Foundation; either
version 2.1 of the License, or (at your option) any later version. This
module is distributed in the hope that it will be useful, but WITHOUT ANY
WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public License for more
details. You should have received a copy of the GNU Lesser General Public
License along with this module; if not, write to the Free Software Foundation,
Inc., 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA */

#include BLARGG_SOURCE_BEGIN

void Nes_Fme7::reset()
{
	last_time = 0;
	
	for ( int i = 0; i < osc_count; i++ )
		oscs [i].last_amp = 0;
	
	fme7_apu_state_t* state = this;
	memset( state, 0, sizeof *state );

	reset_triggers();
}

unsigned char const Nes_Fme7::amp_table [16] =
{
	#define ENTRY( n ) (unsigned char) (n * amp_range + 0.5)
	ENTRY(0.0000), ENTRY(0.0078), ENTRY(0.0110), ENTRY(0.0156),
	ENTRY(0.0221), ENTRY(0.0312), ENTRY(0.0441), ENTRY(0.0624),
	ENTRY(0.0883), ENTRY(0.1249), ENTRY(0.1766), ENTRY(0.2498),
	ENTRY(0.3534), ENTRY(0.4998), ENTRY(0.7070), ENTRY(1.0000)
	#undef ENTRY
};

void Nes_Fme7::run_until( blip_time_t end_time )
{
	require( end_time >= last_time );
	
	for ( int index = 0; index < osc_count; index++ )
	{
		int mode = regs [7] >> index;
		int vol_mode = regs [010 + index];
		int volume = amp_table [vol_mode & 0x0F];
		
		Blip_Buffer* const osc_output = oscs [index].output;
		if ( !osc_output )
		{
			oscs[index].trigger = trigger_none;
			continue;
		}

		// check for unsupported mode
		#ifndef NDEBUG
			if ( (mode & 011) <= 001 && vol_mode & 0x1F )
				dprintf( "FME7 used unimplemented sound mode: %02X, vol_mode: %02X\n",
						mode, vol_mode & 0x1F );
		#endif
		
		if ( (mode & 001) | (vol_mode & 0x10) )
			volume = 0; // noise and envelope aren't supported
		
		// period
		int const period_factor = 16;
		unsigned period = (regs [index * 2 + 1] & 0x0F) * 0x100 * period_factor +
				regs [index * 2] * period_factor;
		if ( period < 50 ) // around 22 kHz
		{
			volume = 0;
			if ( !period ) // on my AY-3-8910A, period doesn't have extra one added
				period = period_factor;
		}
		
		// current amplitude
		int amp = volume;
		if ( !phases [index] )
			amp = 0;
		{
			int delta = amp - oscs [index].last_amp;
			if ( delta )
			{
				oscs [index].last_amp = amp;
				synth.offset( last_time, delta, osc_output );
			}
		}
		
		blip_time_t time = last_time + delays [index];
		if ( time < end_time )
		{
			int delta = amp * 2 - volume;
			if ( volume )
			{
				do
				{
					delta = -delta;
					if (delta > 0)
						update_trigger(osc_output, time, oscs[index].trigger);
					synth.offset_inline( time, delta, osc_output );
					time += period;
				}
				while ( time < end_time );
				
				oscs [index].last_amp = (delta + volume) >> 1;
				phases [index] = (delta > 0);
			}
			else
			{
				// maintain phase when silent
				int count = (end_time - time + period - 1) / period;
				phases [index] ^= count & 1;
				time += (blip_time_t) count * period;
				oscs[index].trigger = trigger_none;
			}
		}
		
		delays [index] = (uint16_t)(time - end_time);
	}
	
	last_time = end_time;
}

void Nes_Fme7::write_register(cpu_time_t time, cpu_addr_t addr, int data)
{
	if (addr >= latch_addr && addr < data_addr)
		write_latch(data);
	else if (addr >= data_addr)
		write_data(time, data);
}

void Nes_Fme7::start_seeking()
{
	memset(shadow_internal_regs, -1, sizeof(shadow_internal_regs));
}

void Nes_Fme7::stop_seeking(blip_time_t& clock)
{
	for (int i = 0; i < array_count(shadow_internal_regs); i++)
	{
		if (shadow_internal_regs[i] >= 0)
		{
			write_latch(i);
			write_data(clock += 4, shadow_internal_regs[i]);
		}
	}
}

void Nes_Fme7::write_shadow_register(int addr, int data)
{
	if (addr >= latch_addr && addr < data_addr)
		latch = data;
	else if (addr >= data_addr)
		shadow_internal_regs[latch] = data;
}

void Nes_Fme7::get_register_values(struct s5b_register_values* r)
{
	for (int i = 0; i < reg_count; i++)
	{
		r->regs[i] = regs[i];
		r->ages[i] = ages[i];

		ages[i] = increment_saturate(ages[i]);
	}
}


void Nes_Fme7::reset_triggers()
{
	oscs[0].trigger = trigger_hold;
	oscs[1].trigger = trigger_hold;
	oscs[2].trigger = trigger_hold;
}

int Nes_Fme7::get_channel_trigger(int idx) const
{
	return oscs[idx].trigger;
}
