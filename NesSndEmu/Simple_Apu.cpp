
// Nes_Snd_Emu 0.1.7. http://www.slack.net/~ant/libs/

#include "Simple_Apu.h"

#include <string.h>

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
	vrc7.output(&buf);
	fds.output(&buf);
	mmc5.output(&buf);
	namco.output(&buf);
	sunsoft.output(&buf);
	buf.clock_rate( 1789773 );
	return buf.sample_rate( rate );
}

void Simple_Apu::enable_channel(int idx, bool enable)
{
	if (idx < 5)
	{
		apu.osc_output(idx, enable ? &buf : NULL);
	}
	else
	{
		switch (expansion)
		{
			case expansion_vrc6: 
				vrc6.osc_output(idx - 5, enable ? &buf : NULL);
				break;
			case expansion_vrc7:
				vrc7.output(enable ? &buf : NULL);
				break;
			case expansion_fds:
				fds.output(enable ? &buf : NULL);
				break;
			case expansion_mmc5:
				mmc5.output(enable ? &buf : NULL);
				break;
			case expansion_namco:
				namco.output(enable ? &buf : NULL);
				break;
			case expansion_sunsoft:
				sunsoft.output(enable ? &buf : NULL);
				break;
		}
	}
}

void Simple_Apu::treble_eq(int exp, double treble, int cutoff, int sample_rate)
{
	blip_eq_t eq(blip_eq_t(treble, cutoff, sample_rate));

	switch (exp)
	{
		case expansion_none:
			apu.treble_eq(eq);
			break;
		case expansion_vrc6:
			vrc6.treble_eq(eq);
			break;
		case expansion_fds:
			fds.treble_eq(eq);
			break;
		case expansion_mmc5:
			mmc5.treble_eq(eq);
			break;
		case expansion_namco:
			namco.treble_eq(eq);
			break;
		case expansion_sunsoft:
			sunsoft.treble_eq(eq);
			break;
	}
}

void Simple_Apu::write_register(cpu_addr_t addr, int data)
{
	if (seeking)
	{
		int idx = Nes_Apu::addr_to_shadow_reg(addr);
		if (idx >= 0)
		{
			shadow_regs_apu[idx] = data;
		}
		else
		{
			switch (expansion)
			{
			case expansion_vrc6:
				idx = Nes_Vrc6::addr_to_shadow_reg(addr);
				if (idx >= 0)
					shadow_regs_vrc6[idx] = data;
				break;
			case expansion_fds:
				idx = Nes_Fds::addr_to_shadow_reg(addr);
				if (idx >= 0)
					shadow_regs_fds[idx] = data;
				break;
			case expansion_mmc5:
				idx = Nes_Mmc5::addr_to_shadow_reg(addr);
				if (idx >= 0)
					shadow_regs_mmc5[idx] = data;
				break;
			}
		}
	}
	else
	{
		if (addr >= Nes_Apu::start_addr && addr <= Nes_Apu::end_addr)
		{
			apu.write_register(clock(), addr, data);
		}
		else
		{
			switch (expansion)
			{
			case expansion_vrc6:
				vrc6.write_register(clock(), addr, data);
				break;
			case expansion_vrc7:
				vrc7.write_register(clock(), addr, data);
				break;
			case expansion_fds:
				fds.write_register(clock(), addr, data);
				break;
			case expansion_mmc5:
				mmc5.write_register(clock(), addr, data);
				break;
			case expansion_namco:
				namco.write_register(clock(), addr, data);
				break;
			case expansion_sunsoft:
				sunsoft.write_register(clock(), addr, data);
				break;
			}
		}
	}
}

void Simple_Apu::start_seeking()
{
	seeking = true;
	memset(shadow_regs_apu,  -1, sizeof(shadow_regs_apu));
	memset(shadow_regs_vrc6, -1, sizeof(shadow_regs_vrc6));
	memset(shadow_regs_fds,  -1, sizeof(shadow_regs_fds));
	memset(shadow_regs_mmc5, -1, sizeof(shadow_regs_mmc5));
}

void Simple_Apu::stop_seeking()
{
	for (int i = 0; i < array_count(shadow_regs_apu); i++)
	{
		if (shadow_regs_apu[i] >= 0)
			apu.write_register(clock(), Nes_Apu::shadow_reg_to_addr(i), shadow_regs_apu[i]);
	}

	switch (expansion)
	{
		case expansion_vrc6:
			for (int i = 0; i < array_count(shadow_regs_vrc6); i++)
			{
				if (shadow_regs_vrc6[i] >= 0)
					apu.write_register(clock(), Nes_Vrc6::shadow_reg_to_addr(i), shadow_regs_vrc6[i]);
			}
			break;
		case expansion_fds:
			for (int i = 0; i < array_count(shadow_regs_fds); i++)
			{
				if (shadow_regs_fds[i] >= 0)
					apu.write_register(clock(), Nes_Fds::shadow_reg_to_addr(i), shadow_regs_fds[i]);
			}
			break;
		case expansion_mmc5:
			for (int i = 0; i < array_count(shadow_regs_mmc5); i++)
			{
				if (shadow_regs_mmc5[i] >= 0)
					apu.write_register(clock(), Nes_Mmc5::shadow_reg_to_addr(i), shadow_regs_mmc5[i]);
			}
			break;
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

	switch (expansion)
	{
	case expansion_vrc6:
		vrc6.end_frame(frame_length);
		break;
	case expansion_vrc7:
		vrc7.end_frame(frame_length);
		break;
	case expansion_fds:
		fds.end_frame(frame_length);
		break;
	case expansion_mmc5:
		mmc5.end_frame(frame_length);
		break;
	case expansion_namco:
		namco.end_frame(frame_length);
		break;
	case expansion_sunsoft:
		sunsoft.end_frame(frame_length);
		break;
	}

	buf.end_frame( frame_length );
}

void Simple_Apu::reset()
{
	seeking = false;
	apu.reset();
	vrc6.reset();
	vrc7.reset();
	fds.reset();
	mmc5.reset();
	namco.reset();
	sunsoft.reset();
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

