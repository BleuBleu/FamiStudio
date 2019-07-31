
// Nes_Snd_Emu 0.1.7. http://www.slack.net/~ant/libs/

#include "Nes_Namco.h"

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

Nes_Namco::Nes_Namco()
{
	output( NULL );
	volume( 1.0 );
	reset();
}

Nes_Namco::~Nes_Namco()
{
}

void Nes_Namco::reset()
{
	addr_reg = 0;
	
	int i;
	for ( i = 0; i < reg_count; i++ )
		reg [i] = 0;
	
	for ( i = 0; i < osc_count; i++ )
	{
		Namco_Osc& osc = oscs [i];
		osc.delay = 0;
		osc.last_amp = 0;
		osc.wave_pos = 0;
	}
}

void Nes_Namco::output( Blip_Buffer* buf )
{
	for ( int i = 0; i < osc_count; i++ )
		osc_output( i, buf );
}

BOOST::uint8_t& Nes_Namco::access()
{
	int addr = addr_reg & 0x7f;
	if ( addr_reg & 0x80 )
		addr_reg = (addr + 1) | 0x80;
	return reg [addr];
}

/*
void Nes_Namco::reflect_state( Tagged_Data& data )
{
	reflect_int16( data, 'ADDR', &addr_reg );
	
	static const char hex [17] = "0123456789ABCDEF";
	int i;
	for ( i = 0; i < reg_count; i++ )
		reflect_int16( data, 'RG\0\0' + hex [i >> 4] * 0x100 + hex [i & 15], &reg [i] );
	
	for ( i = 0; i < osc_count; i++ )
	{
		reflect_int32( data, 'DLY0' + i, &oscs [i].delay );
		reflect_int16( data, 'POS0' + i, &oscs [i].wave_pos );
	}
}
*/

void Nes_Namco::end_frame( cpu_time_t time )
{
	if ( time > last_time )
		run_until( time );
	
	last_time -= time;
	assert( last_time >= 0 );
}

#include BLARGG_ENABLE_OPTIMIZER

void Nes_Namco::run_until( cpu_time_t nes_end_time )
{
	int active_oscs = ((reg [0x7f] >> 4) & 7) + 1;
	for ( int i = osc_count - active_oscs; i < osc_count; i++ )
	{
		Namco_Osc& osc = oscs [i];
		Blip_Buffer* output = osc.output;
		if ( !output )
			continue;
		
		Blip_Buffer::resampled_time_t time =
				output->resampled_time( last_time ) + osc.delay;
		Blip_Buffer::resampled_time_t end_time = output->resampled_time( nes_end_time );
		osc.delay = 0;
		if ( time < end_time )
		{
			const BOOST::uint8_t* osc_reg = &reg [i * 8 + 0x40];
			if ( !(osc_reg [4] & 0xe0) )
				continue;
			
			int volume = osc_reg [7] & 15;
			if ( !volume )
				continue;
			
			long freq = (osc_reg [4] & 3) * 0x10000 + osc_reg [2] * 0x100L + osc_reg [0];
			if ( !freq )
				continue;
			Blip_Buffer::resampled_time_t period =
					output->resampled_duration( 983040 ) / freq * active_oscs;
			
			int wave_size = (8 - ((osc_reg [4] >> 2) & 7)) * 4;
			if ( !wave_size )
				continue;
			
			int last_amp = osc.last_amp;
			int wave_pos = osc.wave_pos;
			
			do
			{
				// read wave sample
				int addr = wave_pos + osc_reg [6];
				int sample = reg [addr >> 1];
				wave_pos++;
				if ( addr & 1 )
					sample >>= 4;
				sample = (sample & 15) * volume;
				
				// output impulse if amplitude changed
				int delta = sample - last_amp;
				if ( delta )
				{
					last_amp = sample;
					synth.offset_resampled( time, delta, output );
				}
				
				// next sample
				time += period;
				if ( wave_pos >= wave_size )
					wave_pos = 0;
			}
			while ( time < end_time );
			
			osc.wave_pos = wave_pos;
			osc.last_amp = last_amp;
		}
		osc.delay = time - end_time;
	}
	
	last_time = nes_end_time;
}

