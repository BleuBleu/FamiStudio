
// Nes_Snd_Emu 0.1.7. http://www.slack.net/~ant/libs/

#include "Simple_Apu.h"

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

static int null_dmc_reader( void*, cpu_addr_t )
{
	return 0x55; // causes dmc sample to be flat
}

Simple_Apu::Simple_Apu()
{
	seeking = false;
	time = 0;
	frame_length = 29780;
	expansion = expansion_none;
	apu.dmc_reader( null_dmc_reader, NULL );
}

Simple_Apu::~Simple_Apu()
{
}

void Simple_Apu::dmc_reader( int (*f)( void* user_data, cpu_addr_t ), void* p )
{
	assert( f );
	apu.dmc_reader( f, p );
}

blargg_err_t Simple_Apu::sample_rate( long rate )
{
	apu.output( &buf );
	vrc6.output(&buf);
	buf.clock_rate( 1789773 );
	return buf.sample_rate( rate );
}

void Simple_Apu::enable_channel(int idx, bool enable)
{
	if (idx < 5)
		apu.osc_output(idx, enable ? &buf : NULL);
	else if (expansion == expansion_vrc6)
		vrc6.osc_output(idx - 5, enable ? &buf : NULL);
}

void Simple_Apu::write_register(cpu_addr_t addr, int data)
{
	if (seeking)
	{
		if (addr >= 0x4000 && addr <= 0x4013)
		{
			shadowRegistersApu[addr - 0x4000] = data;
		}
		else if (expansion == expansion_vrc6)
		{
			if (addr >= 0x9000 && addr <= 0x9002) shadowRegistersVrc6[0 + addr - 0x9000] = data;
			if (addr >= 0xa000 && addr <= 0xa002) shadowRegistersVrc6[3 + addr - 0xa000] = data;
			if (addr >= 0xb000 && addr <= 0xb002) shadowRegistersVrc6[6 + addr - 0xb000] = data;
		}
	}
	else
	{
		if (addr >= Nes_Apu::start_addr && addr <= Nes_Apu::end_addr)
			apu.write_register(clock(), addr, data);
		else if (expansion == expansion_vrc6)
			vrc6.write_register(clock(), addr, data);
	}
}

#define ARRAY_COUNT(x) (sizeof(x) / sizeof(x[0]))

void Simple_Apu::start_seeking()
{
	seeking = true;

	for (int i = 0; i < ARRAY_COUNT(shadowRegistersApu); i++)
		shadowRegistersApu[i] = -1;
	for (int i = 0; i < ARRAY_COUNT(shadowRegistersVrc6); i++)
		shadowRegistersVrc6[i] = -1;
}

void Simple_Apu::stop_seeking()
{
	for (int i = 0; i < ARRAY_COUNT(shadowRegistersApu); i++)
	{
		if (shadowRegistersApu[i] >= 0)
			apu.write_register(clock(), 0x4000 + i, shadowRegistersApu[i]);
	}

	if (expansion == expansion_vrc6)
	{
		for (int i = 0; i < 3; i++)
		{
			if (shadowRegistersVrc6[0 + i]) vrc6.write_register(clock(), 0x9000 + i, shadowRegistersVrc6[0 + i]);
			if (shadowRegistersVrc6[3 + i]) vrc6.write_register(clock(), 0xa000 + i, shadowRegistersVrc6[3 + i]);
			if (shadowRegistersVrc6[6 + i]) vrc6.write_register(clock(), 0xb000 + i, shadowRegistersVrc6[6 + i]);
		}
	}

	seeking = false;
}

int Simple_Apu::read_status()
{
	return apu.read_status( clock() );
}

void Simple_Apu::end_frame()
{
	time = 0;
	frame_length ^= 1;
	apu.end_frame( frame_length );
	if (expansion == expansion_vrc6)
		vrc6.end_frame( frame_length );
	buf.end_frame( frame_length );
}

void Simple_Apu::reset()
{
	seeking = false;
	apu.reset();
	vrc6.reset();
}

void Simple_Apu::set_audio_expansion(long exp)
{
	expansion = exp;
}

long Simple_Apu::samples_avail() const
{
	return buf.samples_avail();
}

long Simple_Apu::read_samples( sample_t* p, long s )
{
	return buf.read_samples( p, s );
}

void Simple_Apu::remove_samples(long s)
{
	buf.remove_samples(s);
}

void Simple_Apu::save_snapshot( apu_snapshot_t* out ) const
{
	apu.save_snapshot( out );
}

void Simple_Apu::load_snapshot( apu_snapshot_t const& in )
{
	apu.load_snapshot( in );
}

