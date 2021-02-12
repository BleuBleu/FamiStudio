
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
	active_osc = osc_count - 1;
	delay = 0;
	last_amp = 0;
	last_time = 0;
	
	int i;
	for ( i = 0; i < reg_count; i++ )
		reg [i] = 0;
	
	for ( i = 0; i < osc_count; i++ )
	{
		Namco_Osc& osc = oscs [i];
		osc.delay = 0;
		osc.sample = 0;
	}
}

void Nes_Namco::output( Blip_Buffer* buf )
{
	buffer = buf;
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

void Nes_Namco::end_frame( cpu_time_t time )
{
	if ( time > last_time )
		run_until( time );
	
	last_time -= time;
	assert( last_time >= 0 );
}

#include BLARGG_ENABLE_OPTIMIZER

void Nes_Namco::run_until(cpu_time_t end_time)
{
	require(end_time >= last_time);

	int active_oscs = ((reg[0x7f] >> 4) & 7) + 1;

	cpu_time_t time = last_time + delay;

	while (time < end_time)
	{
		Namco_Osc& osc = oscs[active_osc];

		BOOST::uint8_t* osc_reg = &reg[active_osc * 8 + 0x40];

		long freq = ((osc_reg[4] & 3) << 16) | (osc_reg[2] << 8) | osc_reg[0];
		int volume = osc_reg[7] & 15;
		int wave_size = 256 - osc_reg[4];

		// This is not very accurate. We always do the entire 15-cycle channel update.
		// We should only update until end_time. This will fail to emulate mid-update
		// register changes, but in practice should be OK.
		if (osc.output && volume && freq && wave_size)
		{
			// read wave sample
			long phase = (osc_reg[5] << 16) | (osc_reg[3] << 8) | osc_reg[1];
			phase = (phase + freq) % (wave_size << 16);

			int addr = ((phase >> 16) + osc_reg[6]) & 0xff;
			int sample = reg[addr >> 1];

			if (addr & 1)
				sample >>= 4;

			osc.sample = (sample & 15) * volume;

			osc_reg[5] = (phase >> 16) & 0xff;
			osc_reg[3] = (phase >>  8) & 0xff;
			osc_reg[1] = (phase >>  0) & 0xff;
		}
		else
		{
			osc.sample = 0;
		}

		float sum = 0.0f;
		for (int i = osc_count - active_oscs; i < osc_count; i++)
			sum += oscs[i].sample;
		int sample = (int)(sum / min(6, max(1, active_oscs)) + 0.5f); 

		// output impulse if amplitude changed
		int delta = sample - last_amp;
		if (delta)
		{
			last_amp = sample;
			synth.offset(time, delta, buffer);
		}

		time += osc_update_time;

		if (--active_osc < osc_count - active_oscs)
			active_osc = osc_count - 1;
	}

	delay = time - end_time;
	last_time = time;
}

void Nes_Namco::start_seeking()
{
	memset(shadow_internal_regs, -1, sizeof(shadow_internal_regs));
}

void Nes_Namco::stop_seeking(blip_time_t& clock)
{
	for (int i = 0; i < array_count(shadow_internal_regs); i++)
	{
		if (shadow_internal_regs[i] >= 0)
		{
			write_register(clock += 4, addr_reg_addr, i);
			write_register(clock += 4, data_reg_addr, shadow_internal_regs[i]);
		}
	}
}

void Nes_Namco::write_shadow_register(int addr, int data)
{
	if (addr >= addr_reg_addr && addr < (addr_reg_addr + reg_range))
		addr_reg = data;
	else if (addr >= data_reg_addr && addr < (data_reg_addr + reg_range))
		shadow_internal_regs[addr_reg] = data;
}


