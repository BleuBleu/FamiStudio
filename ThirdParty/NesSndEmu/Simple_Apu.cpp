
// Nes_Snd_Emu 0.1.7. http://www.slack.net/~ant/libs/

#include "Simple_Apu.h"

#include <malloc.h>

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

#define NONLINEAR_TND 1

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
	tnd_volume = 1.0;
	expansions = expansion_mask_none;
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
#if NONLINEAR_TND
	apu.output(&buf, &tnd);
#else
	apu.output(&buf, &buf);
#endif
	vrc6.output(&buf);
	vrc7.output(&buf);
	fds.output(&buf);
	mmc5.output(&buf);
	namco.output(&buf);
	sunsoft.output(&buf);
	
#if NONLINEAR_TND
	tnd.clock_rate(pal ? 1662607 : 1789773);
	buf.clock_rate(pal ? 1662607 : 1789773);

	tnd.sample_rate(rate);
	return buf.sample_rate( rate );
#else
	buf.clock_rate(pal ? 1662607 : 1789773);
	return buf.sample_rate(rate);
#endif
}

void Simple_Apu::enable_channel(int expansion, int idx, bool enable)
{
	if (expansion == 0)
	{
		#if NONLINEAR_TND
			if (idx < 2)
				apu.osc_output(idx, enable ? &buf : NULL);
			else
				apu.osc_output(idx, enable ? &tnd : NULL);
		#else
			apu.osc_output(idx, enable ? &buf : NULL);
		#endif
	}
	else
	{
		switch (expansion)
		{
		case expansion_vrc6: vrc6.osc_output(idx, enable ? &buf : NULL); break;
		case expansion_vrc7: vrc7.enable_channel(idx, enable); break;
		case expansion_fds: fds.output(enable ? &buf : NULL); break;
		case expansion_mmc5: mmc5.osc_output(idx, enable ? &buf : NULL); break;
		case expansion_namco: namco.osc_output(idx, enable ? &buf : NULL); break;
		case expansion_sunsoft: sunsoft.osc_output(idx, enable ? &buf : NULL); break;
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
	}
}

void Simple_Apu::set_expansion_volume(int exp, double volume)
{
	switch (exp)
	{
	#if NONLINEAR_TND
		case expansion_none: apu.enable_nonlinear(volume); tnd_volume = volume; break;
	#else
		case expansion_none: apu.volume(volume); break;
	#endif
		case expansion_vrc6: vrc6.volume(volume); break;
		case expansion_vrc7: vrc7.volume(volume); break;
		case expansion_fds: fds.volume(volume); break;
		case expansion_mmc5: mmc5.volume(volume); break;
		case expansion_namco: namco.volume(volume); break;
		case expansion_sunsoft: sunsoft.volume(volume); break;
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
			if (expansions & expansion_mask_vrc6) vrc6.write_shadow_register(addr, data);
			if (expansions & expansion_mask_vrc7) vrc7.write_shadow_register(addr, data);
			if (expansions & expansion_mask_fds) fds.write_shadow_register(addr, data);
			if (expansions & expansion_mask_mmc5) mmc5.write_shadow_register(addr, data);
			if (expansions & expansion_mask_namco) namco.write_shadow_register(addr, data);
			if (expansions & expansion_mask_sunsoft) sunsoft.write_shadow_register(addr, data); 
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
			if (expansions & expansion_mask_vrc6) vrc6.write_register(clock(), addr, data);
			if (expansions & expansion_mask_vrc7) vrc7.write_register(clock(), addr, data);
			if (expansions & expansion_mask_fds) fds.write_register(clock(), addr, data);
			if (expansions & expansion_mask_mmc5) mmc5.write_register(clock(), addr, data);
			if (expansions & expansion_mask_namco) namco.write_register(clock(), addr, data);
			if (expansions & expansion_mask_sunsoft) sunsoft.write_register(clock(), addr, data);
		}
	}
}

void Simple_Apu::start_seeking()
{
	seeking = true;
	apu.start_seeking();

	if (expansions & expansion_mask_vrc6) vrc6.start_seeking();
	if (expansions & expansion_mask_vrc7) vrc7.start_seeking();
	if (expansions & expansion_mask_fds) fds.start_seeking();
	if (expansions & expansion_mask_mmc5) mmc5.start_seeking();
	if (expansions & expansion_mask_namco) namco.start_seeking();
	if (expansions & expansion_mask_sunsoft) sunsoft.start_seeking();
}

void Simple_Apu::stop_seeking()
{
	apu.stop_seeking(time);

	if (expansions & expansion_mask_vrc6) vrc6.stop_seeking(time);
	if (expansions & expansion_mask_vrc7) vrc7.stop_seeking(time);
	if (expansions & expansion_mask_fds) fds.stop_seeking(time);
	if (expansions & expansion_mask_mmc5) mmc5.stop_seeking(time);
	if (expansions & expansion_mask_namco) namco.stop_seeking(time);
	if (expansions & expansion_mask_sunsoft) sunsoft.stop_seeking(time);

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

	if (expansions & expansion_mask_vrc6) vrc6.end_frame(frame_length);
	if (expansions & expansion_mask_vrc7) vrc7.end_frame(frame_length);
	if (expansions & expansion_mask_fds) fds.end_frame(frame_length); 
	if (expansions & expansion_mask_mmc5) mmc5.end_frame(frame_length); 
	if (expansions & expansion_mask_namco) namco.end_frame(frame_length); 
	if (expansions & expansion_mask_sunsoft) sunsoft.end_frame(frame_length); 

	buf.end_frame( frame_length );
#if NONLINEAR_TND
	tnd.end_frame( frame_length );
#endif
}

void Simple_Apu::reset()
{
#if NONLINEAR_TND 
	apu.enable_nonlinear(1.0);
#endif
	seeking = false;
	nonlinear_accum = 0;
	apu.reset(pal_mode);
	vrc6.reset();
	vrc7.reset();
	fds.reset();
	mmc5.reset();
	namco.reset();
	sunsoft.reset();
}

void Simple_Apu::set_audio_expansions(long exp)
{
	expansions = exp;
}

long Simple_Apu::samples_avail() const
{
#if NONLINEAR_TND
	assert(buf.samples_avail() == tnd.samples_avail());
#endif
	return buf.samples_avail();
}

inline long nonlinearize(long raw_sample, double volume)
{
	const int    sample_shift     = blip_sample_bits - 16;
	const double sample_scale_inv = (1 << sample_shift) * 65535.0;
	const double sample_scale     = 1.0 / sample_scale_inv;

	// Using the 3 * tri (15) + 2 * noise (15) + dmc (127) approximation = maximum value is 202.
	const double tnd_scale = 202.0;

	// Convert the raw fixed point sample to floating point + apply nonlinear approximation.
	double sample_float = max(0.00001, raw_sample * sample_scale);
	double sample_nonlinear = 163.67 / (24329.0 / (sample_float * tnd_scale) + 100.0);

	// Rescale to the fixed point, blip buffer format.
	return (long)(sample_nonlinear * volume * sample_scale_inv);
}

long Simple_Apu::read_samples( sample_t* out, long count )
{
#if NONLINEAR_TND
	assert(buf.samples_avail() == tnd.samples_avail());

	if (count)
	{
		// Apply non-linear mixing to the TND buffer.
		Blip_Buffer::buf_t_* p = tnd.buffer_;

		long prev = nonlinearize(nonlinear_accum, tnd_volume);

		for (unsigned n = count; n--; )
		{
			nonlinear_accum += (long)*p;
			long entry = nonlinearize(nonlinear_accum, tnd_volume);
			*p++ = (entry - prev);
			prev = entry;
		}

		// Then mix both blip buffers.
		Blip_Reader lin;
		Blip_Reader nonlin;

		int lin_bass = lin.begin(buf);
		int nonlin_bass = nonlin.begin(tnd);

		for (int n = count; n--; )
		{
			int s = lin.read() + nonlin.read();
			lin.next(lin_bass);
			nonlin.next(nonlin_bass);
			*out++ = s;

			if ((BOOST::int16_t) s != s)
				out[-1] = 0x7FFF - (s >> 24);
		}

		lin.end(buf);
		nonlin.end(tnd);

		buf.remove_samples(count);
		tnd.remove_samples(count);
	}

#else
	buf.read_samples(out, count);
#endif

	return count;
}

void Simple_Apu::remove_samples(long s)
{
	buf.remove_samples(s);
#if NONLINEAR_TND
	tnd.remove_samples(s);
#endif
}

void Simple_Apu::save_snapshot( apu_snapshot_t* out ) const
{
	apu.save_snapshot( out );
}

void Simple_Apu::load_snapshot( apu_snapshot_t const& in )
{
	apu.load_snapshot( in );
}

