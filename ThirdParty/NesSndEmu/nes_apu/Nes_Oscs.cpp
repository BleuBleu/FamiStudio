
// Nes_Snd_Emu 0.1.7. http://www.slack.net/~ant/libs/

#include "Nes_Apu.h"

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

// Nes_Osc

void Nes_Osc::clock_length( int halt_mask )
{
	if ( length_counter && !(regs [0] & halt_mask) )
		length_counter--;
}

void Nes_Envelope::clock_envelope()
{
	int period = regs [0] & 15;
	if ( reg_written [3] ) {
		reg_written [3] = false;
		env_delay = period;
		envelope = 15;
	}
	else if ( --env_delay < 0 ) {
		env_delay = period;
		if ( envelope | (regs [0] & 0x20) )
			envelope = (envelope - 1) & 15;
	}
}

int Nes_Envelope::volume() const
{
	return length_counter == 0 ? 0 : (regs [0] & 0x10) ? (regs [0] & 15) : envelope;
}

// Nes_Square

void Nes_Square::clock_sweep( int negative_adjust )
{
	int sweep = regs [1];
	
	if ( --sweep_delay < 0 )
	{
		reg_written [1] = true;
		
		int period = this->period();
		int shift = sweep & shift_mask;
		if ( shift && (sweep & 0x80) && period >= 8 )
		{
			int offset = period >> shift;
			
			if ( sweep & negate_flag )
				offset = negative_adjust - offset;
			
			if ( period + offset < 0x800 )
			{
				period += offset;
				// rewrite period
				regs [2] = period & 0xff;
				regs [3] = (regs [3] & ~7) | ((period >> 8) & 7);
			}
		}
	}
	
	if ( reg_written [1] ) {
		reg_written [1] = false;
		sweep_delay = (sweep >> 4) & 7;
	}
}

void Nes_Square::run( cpu_time_t time, cpu_time_t end_time )
{
	if ( !output )
		return;
	
	const int volume = this->volume();
	const int period = this->period();
	int offset = period >> (regs [1] & shift_mask);
	if ( regs [1] & negate_flag )
		offset = 0;
	
	const int timer_period = (period + 1) * 2;
	if ( volume == 0 || period < min_period || (period + offset) >= 0x800 )
	{
		if ( last_amp ) {
			synth->offset( time, -last_amp, output );
			last_amp = 0;
		}
		
		time += delay;
		if ( time < end_time )
		{
			// maintain proper phase
			int count = (end_time - time + timer_period - 1) / timer_period;
			phase = (phase + count) & (phase_range - 1);
			time += (long) count * timer_period;
		}
	}
	else
	{
		// handle duty select
		int duty_select = (regs [0] >> 6) & 3;
		int duty = 1 << duty_select; // 1, 2, 4, 2
		int amp = 0;
		if ( duty_select == 3 ) {
			duty = 2; // negated 25%
			amp = volume;
		}
		if ( phase < duty )
			amp ^= volume;
		
		int delta = update_amp( amp );
		if ( delta )
			synth->offset( time, delta, output );
		
		time += delay;
		if ( time < end_time )
		{
			Blip_Buffer* const output = this->output;
			const Synth* synth = this->synth;
			int delta = amp * 2 - volume;
			int phase = this->phase;
			
			do {
				phase = (phase + 1) & (phase_range - 1);
				if ( phase == 0 || phase == duty ) {
					delta = -delta;
					synth->offset_inline( time, delta, output );
				}
				time += timer_period;
			}
			while ( time < end_time );
			
			last_amp = (delta + volume) >> 1;
			this->phase = phase;
		}
	}
	
	delay = time - end_time;
}

// Nes_Triangle

void Nes_Triangle::clock_linear_counter()
{
	if ( reg_written [3] )
		linear_counter = regs [0] & 0x7f;
	else if ( linear_counter )
		linear_counter--;
	
	if ( !(regs [0] & 0x80) )
		reg_written [3] = false;
}

inline int Nes_Triangle::calc_amp() const
{
	int amp = phase_range - phase;
	if ( amp < 0 )
		amp = phase - (phase_range + 1);
	return amp;
}

void Nes_Triangle::run( cpu_time_t time, cpu_time_t end_time )
{
	if ( !output )
		return;
	
	// to do: track phase when period < 3
	// to do: Output 7.5 on dac when period < 2? More accurate, but results in more clicks.
	
	int delta = update_amp( calc_amp() );
	if ( delta )
		synth.offset( time, delta, output );
	
	time += delay;
	const int timer_period = period() + 1;
	if ( length_counter == 0 || linear_counter == 0 || timer_period < 3 )
	{
		time = end_time;
	}
	else if ( time < end_time )
	{
		Blip_Buffer* const output = this->output;
		
		int phase = this->phase;
		int volume = 1;
		if ( phase > phase_range ) {
			phase -= phase_range;
			volume = -volume;
		}
		
		do {
			if ( --phase == 0 ) {
				phase = phase_range;
				volume = -volume;
			}
			else {
				synth.offset_inline( time, volume, output );
			}
			
			time += timer_period;
		}
		while ( time < end_time );
		
		if ( volume < 0 )
			phase += phase_range;
		this->phase = phase;
		last_amp = calc_amp();
 	}
	delay = time - end_time;
}

// Nes_Dmc

void Nes_Dmc::reset()
{
	address = 0;
	dac = 0;
	buf = 0;
	bits_remain = 1;
	bits = 0;
	buf_empty = true;
	silence = true;
	next_irq = Nes_Apu::no_irq;
	irq_flag = false;
	irq_enabled = false;
	
	Nes_Osc::reset();
	period = 0x036;
}

void Nes_Dmc::recalc_irq()
{
	cpu_time_t irq = Nes_Apu::no_irq;
	if ( irq_enabled && length_counter )
		irq = apu->last_time + delay +
				((length_counter - 1) * 8 + bits_remain - 1) * cpu_time_t (period) + 1;
	if ( irq != next_irq ) {
		next_irq = irq;
		apu->irq_changed();
	}
}

int Nes_Dmc::count_reads( cpu_time_t time, cpu_time_t* last_read ) const
{
	if ( last_read )
		*last_read = time;
	
	if ( length_counter == 0 )
		return 0; // not reading
	
	long first_read = apu->last_time + delay + long (bits_remain - 1) * period;
	long avail = time - first_read;
	if ( avail <= 0 )
		return 0;
	
	int count = (avail - 1) / (period * 8) + 1;
	if ( !(regs [0] & loop_flag) && count > length_counter )
		count = length_counter;
	
	if ( last_read ) {
		*last_read = first_read + (count - 1) * (period * 8) + 1;
		assert( *last_read <= time );
		assert( count == count_reads( *last_read, NULL ) );
		assert( count - 1 == count_reads( *last_read - 1, NULL ) );
	}
	
	return count;
}

static const short dmc_period_table [2] [16] = {
	0x1ac, 0x17c, 0x154, 0x140, 0x11e, 0x0fe, 0x0e2, 0x0d6, // NTSC
	0x0be, 0x0a0, 0x08e, 0x080, 0x06a, 0x054, 0x048, 0x036,
	
	0x18e, 0x161, 0x13c, 0x129, 0x10a, 0x0ec, 0x0d2, 0x0c7, // PAL (totally untested)
	0x0b1, 0x095, 0x084, 0x077, 0x062, 0x04e, 0x043, 0x032  // to do: verify PAL periods
};

inline void Nes_Dmc::reload_sample()
{
	address = 0x4000 + regs [2] * 0x40;
	length_counter = regs [3] * 0x10 + 1;
}

static const unsigned char dac_table [128] = {
	 0,  0,  1,  2,  2,  3,  3,  4,  5,  5,  6,  7,  7,  8,  8,  9,
	10, 10, 11, 11, 12, 13, 13, 14, 14, 15, 15, 16, 17, 17, 18, 18,
	19, 19, 20, 20, 21, 21, 22, 22, 23, 23, 24, 24, 25, 25, 26, 26,
	27, 27, 28, 28, 29, 29, 30, 30, 31, 31, 32, 32, 32, 33, 33, 34,
	34, 35, 35, 35, 36, 36, 37, 37, 38, 38, 38, 39, 39, 40, 40, 40,
	41, 41, 42, 42, 42, 43, 43, 44, 44, 44, 45, 45, 45, 46, 46, 47,
	47, 47, 48, 48, 48, 49, 49, 49, 50, 50, 50, 51, 51, 51, 52, 52,
	52, 53, 53, 53, 54, 54, 54, 55, 55, 55, 56, 56, 56, 57, 57, 57
};

void Nes_Dmc::write_register( int addr, int data )
{
	if ( addr == 0 ) {
		period = dmc_period_table [pal_mode] [data & 15];
		irq_enabled = (data & 0xc0) == 0x80; // enabled only if loop disabled
		irq_flag &= irq_enabled;
		recalc_irq();
	}
	else if ( addr == 1 )
	{
		if ( !nonlinear )
		{
			// adjust last_amp so that "pop" amplitude will be properly non-linear
			// with respect to change in dac
			int old_amp = dac_table [dac];
			dac = data & 0x7F;
			int diff = dac_table [dac] - old_amp;
			last_amp = dac - diff;
		}
		
		dac = data & 0x7F;
	}
}

void Nes_Dmc::start()
{
	reload_sample();
	fill_buffer();
	recalc_irq();
}

void Nes_Dmc::fill_buffer()
{
	if ( buf_empty && length_counter )
	{
		require( rom_reader ); // rom_reader must be set
		buf = rom_reader( rom_reader_data, 0x8000u + address );
		address = (address + 1) & 0x7FFF;
		buf_empty = false;
		if ( --length_counter == 0 )
		{
			if ( regs [0] & loop_flag ) {
				reload_sample();
			}
			else {
				apu->osc_enables &= ~0x10;
				irq_flag = irq_enabled;
				next_irq = Nes_Apu::no_irq;
				apu->irq_changed();
			}
		}
	}
}

void Nes_Dmc::run( cpu_time_t time, cpu_time_t end_time )
{
	if ( !output )
		return;
	
	int delta = update_amp( dac );
	if ( delta )
		synth.offset( time, delta, output );
	
	time += delay;
	if ( time < end_time )
	{
		int bits_remain = this->bits_remain;
		if ( silence && buf_empty )
		{
			int count = (end_time - time + period - 1) / period;
			bits_remain = (bits_remain - 1 + 8 - (count % 8)) % 8 + 1;
			time += count * period;
		}
		else
		{
			Blip_Buffer* const output = this->output;
			const int period = this->period;
			int bits = this->bits;
			int dac = this->dac;
			
			do
			{
				if ( !silence )
				{
					const int step = (bits & 1) * 4 - 2;
					bits >>= 1;
					if ( unsigned (dac + step) <= 0x7F ) {
						dac += step;
						synth.offset_inline( time, step, output );
					}
				}
				
				time += period;
				
				if ( --bits_remain == 0 )
				{
					bits_remain = 8;
					if ( buf_empty ) {
						silence = true;
					}
					else {
						silence = false;
						bits = buf;
						buf_empty = true;
						fill_buffer();
					}
				}
			}
			while ( time < end_time );
			
			this->dac = dac;
			this->last_amp = dac;
			this->bits = bits;
		}
		this->bits_remain = bits_remain;
	}
	delay = time - end_time;
}

// Nes_Noise

#include BLARGG_ENABLE_OPTIMIZER

static const short noise_period_table [16] = {
	0x004, 0x008, 0x010, 0x020, 0x040, 0x060, 0x080, 0x0A0,
	0x0CA, 0x0FE, 0x17C, 0x1FC, 0x2FA, 0x3F8, 0x7F2, 0xFE4
};

void Nes_Noise::run( cpu_time_t time, cpu_time_t end_time )
{
	if ( !output )
		return;
	
	const int volume = this->volume();
	int amp = (noise & 1) ? volume : 0;
	int delta = update_amp( amp );
	if ( delta )
		synth.offset( time, delta, output );
	
	time += delay;
	if ( time < end_time )
	{
		const int mode_flag = 0x80;
		
		int period = noise_period_table [regs [2] & 15];
		if ( !volume )
		{
			// round to next multiple of period
			time += (end_time - time + period - 1) / period * period;
			
			// approximate noise cycling while muted, by shuffling up noise register
			// to do: precise muted noise cycling?
			if ( !(regs [2] & mode_flag) ) {
				int feedback = (noise << 13) ^ (noise << 14);
				noise = (feedback & 0x4000) | (noise >> 1);
			}
		}
		else
		{
			Blip_Buffer* const output = this->output;
			
			// using resampled time avoids conversion in synth.offset()
			Blip_Buffer::resampled_time_t rperiod = output->resampled_duration( period );
			Blip_Buffer::resampled_time_t rtime = output->resampled_time( time );
			
			int noise = this->noise;
			int delta = amp * 2 - volume;
			const int tap = (regs [2] & mode_flag ? 8 : 13);
			
			do {
				int feedback = (noise << tap) ^ (noise << 14);
				time += period;
				
				if ( (noise + 1) & 2 ) {
					// bits 0 and 1 of noise differ
					delta = -delta;
					synth.offset_resampled( rtime, delta, output );
				}
				
				rtime += rperiod;
				noise = (feedback & 0x4000) | (noise >> 1);
			}
			while ( time < end_time );
			
			last_amp = (delta + volume) >> 1;
			this->noise = noise;
		}
	}
	
	delay = time - end_time;
}

