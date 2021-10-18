
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
	pal_mode = false;
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

blargg_err_t Simple_Apu::sample_rate( long rate, bool pal)
{
	pal_mode = pal;
	frame_length = pal ? 33247 : 29780;
	apu.output(&buf);
	vrc6.output(&buf);
	vrc7.output(&buf);
	fds.output(&buf);
	mmc5.output(&buf);
	namco.output(&buf);
	sunsoft.output(&buf);
	epsm.output(&bufLeft, &bufRight);
	buf.clock_rate( pal ? 1662607 : 1789773);
	bufLeft.clock_rate(pal ? 1662607 : 1789773);
	bufLeft.sample_rate(rate);
	bufRight.clock_rate(pal ? 1662607 : 1789773);
	bufRight.sample_rate(rate);
	return buf.sample_rate( rate);
}

void Simple_Apu::enable_channel(int idx, bool enable)
{
	if (idx < 5)
	{
		apu.osc_output(idx, enable ? &buf : NULL);
	}
	else
	{
		idx -= 5;

		switch (expansion)
		{
			case expansion_vrc6: vrc6.osc_output(idx, enable ? &buf : NULL); break;
			case expansion_vrc7: vrc7.enable_channel(idx, enable); break;
			case expansion_fds: fds.output(enable ? &buf : NULL); break;
			case expansion_mmc5: mmc5.osc_output(idx, enable ? &buf : NULL); break;
			case expansion_namco: namco.osc_output(idx, enable ? &buf : NULL); break;
			//case expansion_sunsoft: sunsoft.osc_output(idx, enable ? &buf : NULL); break;
			case expansion_sunsoft: sunsoft.enable_channel(idx, enable ? &buf : NULL); break;
			case expansion_epsm: epsm.enable_channel(idx, enable ? &buf : NULL); break;
		}
	}
}

void Simple_Apu::treble_eq(int exp, double treble, int sample_rate)
{
	blip_eq_t eq(treble, 0, sample_rate);

	switch (exp)
	{
		case expansion_none: apu.treble_eq(eq); break;
		case expansion_vrc6: vrc6.treble_eq(eq); break;
		case expansion_vrc7: vrc7.treble_eq(eq); break;
		case expansion_fds: fds.treble_eq(eq); break;
		case expansion_mmc5: mmc5.treble_eq(eq); break;
		case expansion_namco: namco.treble_eq(eq); break;
		case expansion_sunsoft: sunsoft.treble_eq(eq); break;
		case expansion_epsm: epsm.treble_eq(eq); break;
	}
}

void Simple_Apu::set_expansion_volume(int exp, double volume)
{
	switch (exp)
	{
		case expansion_none: apu.volume(volume); break;
		case expansion_vrc6: vrc6.volume(volume); break;
		case expansion_vrc7: vrc7.volume(volume); break;
		case expansion_fds: fds.volume(volume); break;
		case expansion_mmc5: mmc5.volume(volume); break;
		case expansion_namco: namco.volume(volume); break;
		case expansion_sunsoft: sunsoft.volume(volume); break;
		case expansion_epsm: epsm.volume(volume); break;
	}
}

void Simple_Apu::write_register(cpu_addr_t addr, int data)
{
	if (seeking)
	{
		if (addr >= Nes_Apu::start_addr && addr <= Nes_Apu::end_addr)
		{
			apu.write_shadow_register(addr, data);
		}
		else
		{
			switch (expansion)
			{
				case expansion_vrc6: vrc6.write_shadow_register(addr, data); break;
				case expansion_vrc7: vrc7.write_shadow_register(addr, data); break;
				case expansion_fds: fds.write_shadow_register(addr, data); break;
				case expansion_mmc5: mmc5.write_shadow_register(addr, data); break;
				case expansion_namco: namco.write_shadow_register(addr, data); break;
				case expansion_sunsoft: sunsoft.write_shadow_register(addr, data); break;
				case expansion_epsm: epsm.write_shadow_register(addr, data); break;

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
				case expansion_vrc6: vrc6.write_register(clock(), addr, data); break;
				case expansion_vrc7: vrc7.write_register(clock(), addr, data); break;
				case expansion_fds: fds.write_register(clock(), addr, data); break;
				case expansion_mmc5: mmc5.write_register(clock(), addr, data); break;
				case expansion_namco: namco.write_register(clock(), addr, data); break;
				case expansion_sunsoft: sunsoft.write_register(clock(), addr, data); break;
				case expansion_epsm: epsm.write_register(clock(), addr, data); break;
			}
		}
	}
}

void Simple_Apu::start_seeking()
{
	seeking = true;
	apu.start_seeking();

	switch (expansion)
	{
		case expansion_vrc6: vrc6.start_seeking(); break;
		case expansion_vrc7: vrc7.start_seeking(); break;
		case expansion_fds: fds.start_seeking(); break;
		case expansion_mmc5: mmc5.start_seeking(); break;
		case expansion_namco: namco.start_seeking(); break;
		case expansion_sunsoft: sunsoft.start_seeking(); break;
		case expansion_epsm: epsm.start_seeking(); break;
	}
}

void Simple_Apu::stop_seeking()
{
	apu.stop_seeking(time);

	switch (expansion)
	{
		case expansion_vrc6: vrc6.stop_seeking(time); break;
		case expansion_vrc7: vrc7.stop_seeking(time); break;
		case expansion_fds: fds.stop_seeking(time); break;
		case expansion_mmc5: mmc5.stop_seeking(time); break;
		case expansion_namco: namco.stop_seeking(time); break;
		case expansion_sunsoft: sunsoft.stop_seeking(time); break;
		case expansion_epsm: epsm.stop_seeking(time); break;
	}

	seeking = false;
}

int Simple_Apu::read_status()
{
	return apu.read_status( clock() );
}

void Simple_Apu::skip_cycles(long cycles)
{
	if (!seeking)
	{
		time += cycles;
		apu.run_until(time);
	}
}

void Simple_Apu::end_frame()
{
	time = 0;
	frame_length ^= 1;

	apu.end_frame( frame_length );

	switch (expansion)
	{
		case expansion_vrc6: vrc6.end_frame(frame_length); break;
		case expansion_vrc7: vrc7.end_frame(frame_length); break;
		case expansion_fds: fds.end_frame(frame_length); break;
		case expansion_mmc5: mmc5.end_frame(frame_length); break;
		case expansion_namco: namco.end_frame(frame_length); break;
		case expansion_sunsoft: sunsoft.end_frame(frame_length); break;
		case expansion_epsm: epsm.end_frame(frame_length); break;
	}

	buf.end_frame( frame_length );
	bufLeft.end_frame(frame_length);
	bufRight.end_frame(frame_length);
}

void Simple_Apu::reset()
{
	seeking = false;
	apu.reset(pal_mode);
	vrc6.reset();
	vrc7.reset();
	fds.reset();
	mmc5.reset();
	namco.reset();
	sunsoft.reset();
	epsm.reset();
}

void Simple_Apu::set_audio_expansion(long exp)
{
	expansion = exp;
}

long Simple_Apu::samples_avail() const
{
	//return buf.samples_avail();
	return (buf.samples_avail()*2);
}

long Simple_Apu::read_samples( sample_t* p, long s )
{
	sample_t outMono [4096];
	sample_t outLeft[4096];
	sample_t outRight[4096];
	long samples =  buf.read_samples(outMono, s, false);

	//bufLeft.mix_samples(outMono, samples);
	//bufRight.mix_samples(outMono, samples);
	bufLeft.read_samples(outLeft, s, false);
	bufRight.read_samples(outRight, s, false);
	for (long i = 0; i < samples; ++i)
	{
		*p++ = (blip_sample_t)clamp((int)(outMono[i]+ outLeft[i]),-32767, 32767);
		*p++ = (blip_sample_t)clamp((int)(outMono[i]+ outRight[i]), -32767, 32767);
	}
	return samples*2;
}

void Simple_Apu::remove_samples(long s)
{
	buf.remove_samples(s);
	bufLeft.remove_samples(s);
	bufRight.remove_samples(s);
}

void Simple_Apu::save_snapshot( apu_snapshot_t* out ) const
{
	apu.save_snapshot( out );
}

void Simple_Apu::load_snapshot( apu_snapshot_t const& in )
{
	apu.load_snapshot( in );
}
