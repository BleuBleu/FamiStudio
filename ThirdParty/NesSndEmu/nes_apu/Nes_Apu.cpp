
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

Nes_Apu::Nes_Apu()
{
	dmc.apu = this;
	dmc.rom_reader = NULL;
	square1.synth = &square_synth;
	square2.synth = &square_synth;
	irq_notifier_ = NULL;
	
	oscs [0] = &square1;
	oscs [1] = &square2;
	oscs [2] = &triangle;
	oscs [3] = &noise;
	oscs [4] = &dmc;
	
	output( NULL, NULL );
	volume( 1.0 );
	reset( false );
}

Nes_Apu::~Nes_Apu()
{
}

void Nes_Apu::treble_eq( const blip_eq_t& eq )
{
	square_synth.treble_eq( eq );
	triangle.synth.treble_eq( eq );
	noise.synth.treble_eq( eq );
	dmc.synth.treble_eq( eq );
}

void Nes_Apu::buffer_cleared()
{
	square1.last_amp = 0;
	square2.last_amp = 0;
	triangle.last_amp = 0;
	noise.last_amp = 0;
	dmc.last_amp = 0;
}

void Nes_Apu::enable_nonlinear( double v )
{
	dmc.nonlinear = true;

	// 0.00752: Blargg's approximation, but i find that it is pretty 
	//          bad compared to my NES. Much too low.
	// 0.00861: Matches the end points of 95.52 / (8128.0 / n + 100) exactly, 
	//          but tends to underestimate the middle range where most music is 
	//          composed.
	// 0.00955: Best linear fit of 95.52 / (8128.0 / n + 100) found in Mathematica. 
	//          Will likely be too loud when  both squares are at full volume, 
	//          but this rarely happens in real songs.

	// Still, I find my NES's squares to be about 10-15% louder than Mesen, NSFPlay
	// and FamiStudio. So I wonder if we put too much trust in the equations on the 
	// wiki. Or if my NES is broken!

	square_synth.volume( 0.5 );
	
	const double tnd = 1.0 / 202;
	triangle.synth.volume( 3 * tnd * 15.0 );
	noise.synth.volume( 2 * tnd * 15.0 );
	dmc.synth.volume( tnd * 127.0 );
	
	buffer_cleared();
}

void Nes_Apu::reset_triggers()
{
	square1.trigger = trigger_hold;
	square2.trigger = trigger_hold;
	triangle.trigger = trigger_hold;
	noise.trigger = trigger_none; // Not implemented yet, would be nice to support mode 1 (93/31 sequence)
	dmc.trigger = trigger_none; // Looping samples would be nice to support.
}

int Nes_Apu::get_channel_trigger(int idx) const
{
	return oscs[idx]->trigger;
}

void Nes_Apu::volume( double v )
{
	dmc.nonlinear = false;

	// Should be 0.00752 * 15, but i find this to be a better approximation.
	square_synth.volume( 0.00861 * 15 * v );
	triangle.synth.volume( 0.12765 * v );
	noise.synth.volume( 0.095 * v );
	dmc.synth.volume( 0.42545 * v );
}

void Nes_Apu::output( Blip_Buffer* buffer, Blip_Buffer* buffer_tnd )
{
	osc_output(0, buffer);
	osc_output(1, buffer);
	osc_output(2, buffer_tnd);
	osc_output(3, buffer_tnd);
	osc_output(4, buffer_tnd);
}

void Nes_Apu::reset( bool pal_mode, int initial_dmc_dac )
{
	// to do: time pal frame periods exactly
	frame_period = pal_mode ? 8314 : 7458;
	dmc.pal_mode = pal_mode;
	noise.pal_mode = pal_mode;
	
	square1.reset();
	square2.reset();
	triangle.reset();
	noise.reset();
	dmc.reset();
	
	last_time = 0;
	osc_enables = 0;
	irq_flag = false;
	earliest_irq_ = no_irq;
	frame_delay = 1;
	write_register( 0, 0x4017, 0x00 );
	write_register( 0, 0x4015, 0x00 );
	
	for ( cpu_addr_t addr = start_addr; addr <= 0x4013; addr++ )
		write_register( 0, addr, (addr & 3) ? 0x00 : 0x10 );
	
	dmc.dac = initial_dmc_dac;
	if ( !dmc.nonlinear )
		dmc.last_amp = initial_dmc_dac; // prevent output transition

	reset_triggers();
}

void Nes_Apu::irq_changed()
{
	cpu_time_t new_irq = dmc.next_irq;
	if ( dmc.irq_flag | irq_flag ) {
		new_irq = 0;
	}
	else if ( new_irq > next_irq ) {
		new_irq = next_irq;
	}
	
	if ( new_irq != earliest_irq_ ) {
		earliest_irq_ = new_irq;
		if ( irq_notifier_ )
			irq_notifier_( irq_data );
	}
}

// frames

void Nes_Apu::run_until( cpu_time_t end_time )
{
	require( end_time >= last_time );
	
	if ( end_time == last_time )
		return;
	
	while ( true )
	{
		// earlier of next frame time or end time
		cpu_time_t time = last_time + frame_delay;
		if ( time > end_time )
			time = end_time;
		frame_delay -= time - last_time;
		
		// run oscs to present
		square1.run( last_time, time );
		square2.run( last_time, time );
		triangle.run( last_time, time );
		noise.run( last_time, time );
		dmc.run( last_time, time );
		last_time = time;
		
		if ( time == end_time )
			break; // no more frames to run
		
		// take frame-specific actions
		frame_delay = frame_period;
		switch ( frame++ )
		{
			case 0:
				if ( !(frame_mode & 0xc0) ) {
		 			next_irq = time + frame_period * 4 + 1;
		 			irq_flag = true;
		 		}
		 		// fall through
		 	case 2:
		 		// clock length and sweep on frames 0 and 2
				square1.clock_length( 0x20 );
				square2.clock_length( 0x20 );
				noise.clock_length( 0x20 );
				triangle.clock_length( 0x80 ); // different bit for halt flag on triangle
				
				square1.clock_sweep( -1 );
				square2.clock_sweep( 0 );
		 		break;
		 	
			case 1:
				// frame 1 is slightly shorter
				frame_delay -= 2;
				break;
			
		 	case 3:
		 		frame = 0;
		 		
		 		// frame 3 is almost twice as long in mode 1
		 		if ( frame_mode & 0x80 )
					frame_delay += frame_period - 6;
				break;
		}
		
		// clock envelopes and linear counter every frame
		triangle.clock_linear_counter();
		square1.clock_envelope();
		square2.clock_envelope();
		noise.clock_envelope();
	}
}

void Nes_Apu::end_frame( cpu_time_t end_time )
{
	if ( end_time > last_time )
		run_until( end_time );
	
	// make times relative to new frame
	last_time -= end_time;
	require( last_time >= 0 );
	
	if ( next_irq != no_irq ) {
		next_irq -= end_time;
		assert( next_irq >= 0 );
	}
	if ( dmc.next_irq != no_irq ) {
		dmc.next_irq -= end_time;
		assert( dmc.next_irq >= 0 );
	}
	if ( earliest_irq_ != no_irq ) {
		earliest_irq_ -= end_time;
		if ( earliest_irq_ < 0 )
			earliest_irq_ = 0;
	}
}

// registers

const unsigned char length_table [0x20] = {
	0x0A, 0xFE, 0x14, 0x02, 0x28, 0x04, 0x50, 0x06,
	0xA0, 0x08, 0x3C, 0x0A, 0x0E, 0x0C, 0x1A, 0x0E, 
	0x0C, 0x10, 0x18, 0x12, 0x30, 0x14, 0x60, 0x16,
	0xC0, 0x18, 0x48, 0x1A, 0x10, 0x1C, 0x20, 0x1E
};

void Nes_Apu::write_register( cpu_time_t time, cpu_addr_t addr, int data )
{
	require( addr > 0x20 ); // addr must be actual address (i.e. 0x40xx)
	require( (unsigned) data <= 0xff );
	
	// Ignore addresses outside range
	if ( addr < start_addr || end_addr < addr )
		return;
	
	run_until( time );
	
	if ( addr < 0x4014 )
	{
		// Write to channel
		int osc_index = (addr - start_addr) >> 2;
		Nes_Osc* osc = oscs [osc_index];
		
		int reg = addr & 3;
		osc->regs [reg] = data;
		osc->reg_written [reg] = true;
		osc->ages [reg] = 0;
		
		if ( osc_index == 4 )
		{
			// handle DMC specially
			dmc.write_register( reg, data );
		}
		else if ( reg == 3 )
		{
			// load length counter
			if ( (osc_enables >> osc_index) & 1 )
				osc->length_counter = length_table [(data >> 3) & 0x1f];
			
			// reset square phase
			if ( osc_index < 2 )
				((Nes_Square*) osc)->phase = Nes_Square::phase_range - 1;
		}
	}
	else if ( addr == 0x4015 )
	{
		// Channel enables
		for ( int i = osc_count; i--; )
			if ( !((data >> i) & 1) )
				oscs [i]->length_counter = 0;
		
		bool recalc_irq = dmc.irq_flag;
		dmc.irq_flag = false;
		
		int old_enables = osc_enables;
		osc_enables = data;
		if ( !(data & 0x10) ) {
			dmc.next_irq = no_irq;
			recalc_irq = true;
		}
		else if ( !(old_enables & 0x10) ) {
			dmc.start(); // dmc just enabled
		}
		
		if ( recalc_irq )
			irq_changed();
	}
	else if ( addr == 0x4017 )
	{
		// Frame mode
		frame_mode = data;
		
		bool irq_enabled = !(data & 0x40);
		irq_flag &= irq_enabled;
		next_irq = no_irq;
		
		// mode 1
		frame_delay = (frame_delay & 1);
		frame = 0;
		
		if ( !(data & 0x80) )
		{
			// mode 0
			frame = 1;
			frame_delay += frame_period;
			if ( irq_enabled )
				next_irq = time + frame_delay + frame_period * 3;
		}
		
		irq_changed();
	}
}

int Nes_Apu::read_status( cpu_time_t time )
{
	run_until( time - 1 );
	
	int result = (dmc.irq_flag << 7) | (irq_flag << 6);
	
	for ( int i = 0; i < osc_count; i++ )
		if ( oscs [i]->length_counter )
			result |= 1 << i;
	
	run_until( time );
	
	if ( irq_flag ) {
		irq_flag = false;
		irq_changed();
	}
	
	return result;
}

void Nes_Apu::start_seeking()
{
	memset(shadow_regs, -1, sizeof(shadow_regs));
}

void Nes_Apu::stop_seeking(blip_time_t& clock)
{
	for (int i = 0; i < array_count(shadow_regs); i++)
	{
		if (shadow_regs[i] >= 0)
			write_register(clock += 4, start_addr + i, shadow_regs[i]);
	}
}

void Nes_Apu::write_shadow_register(int addr, int data)
{
	if (addr >= start_addr && addr < start_addr + shadow_regs_count)
		shadow_regs[addr - start_addr] = data;
}

void Nes_Apu::get_register_values(struct apu_register_values* regs)
{
	for (int i = 0; i < osc_count; i++)
	{
		Nes_Osc* osc = oscs[i];

		for (int j = 0; j < 4; j++)
		{
			regs->regs[i * 4 + j] = osc->regs[j];
			regs->ages[i * 4 + j] = osc->ages[j];

			osc->ages[j] = increment_saturate(osc->ages[j]);
		}

		regs->dpcm_bytes_left = dmc.length_counter;
		regs->dpcm_dac        = dmc.last_amp;
	}
}
