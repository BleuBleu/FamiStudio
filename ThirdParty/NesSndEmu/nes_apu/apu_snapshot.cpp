
// Nes_Snd_Emu 0.1.7. http://www.slack.net/~ant/libs/

#include "apu_snapshot.h"
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

template<int mode>
struct apu_reflection
{
	#define REFLECT( apu, state ) (mode ? void (apu = state) : void (state = apu))

	static void reflect_env( apu_snapshot_t::env_t& state, Nes_Envelope& osc )
	{
		REFLECT( state [0],             osc.env_delay );
		REFLECT( state [1],             osc.envelope );
		REFLECT( state [2],             osc.reg_written [3] );
	}
	
	static void reflect_square( apu_snapshot_t::square_t& state, Nes_Square& osc )
	{
		reflect_env( state.env, osc );
		REFLECT( state.delay,           osc.delay );
		REFLECT( state.length,          osc.length_counter );
		REFLECT( state.phase,           osc.phase );
		REFLECT( state.swp_delay,       osc.sweep_delay );
		REFLECT( state.swp_reset,       osc.reg_written [1] );
	}
	
	static void reflect_triangle( apu_snapshot_t::triangle_t& state, Nes_Triangle& osc )
	{
		REFLECT( state.delay,           osc.delay );
		REFLECT( state.length,          osc.length_counter );
		REFLECT( state.linear_counter,  osc.linear_counter );
		REFLECT( state.linear_mode,     osc.reg_written [3] );
	}
	
	static void reflect_noise( apu_snapshot_t::noise_t& state, Nes_Noise& osc )
	{
		reflect_env( state.env, osc );
		REFLECT( state.delay,           osc.delay );
		REFLECT( state.length,          osc.length_counter );
		REFLECT( state.shift_reg,       osc.noise );
	}
	
	static void reflect_dmc( apu_snapshot_t::dmc_t& state, Nes_Dmc& osc )
	{
		REFLECT( state.delay,           osc.delay );
		REFLECT( state.remain,          osc.length_counter );
		REFLECT( state.buf,             osc.buf );
		REFLECT( state.bits_remain,     osc.bits_remain );
		REFLECT( state.bits,            osc.bits );
		REFLECT( state.buf_empty,       osc.buf_empty );
		REFLECT( state.silence,         osc.silence );
		REFLECT( state.irq_flag,        osc.irq_flag );
		if ( mode )
			state.addr = osc.address | 0x8000;
		else
			osc.address = state.addr & 0x7fff;
	}
};

void Nes_Apu::save_snapshot( apu_snapshot_t* state ) const
{
	for ( int i = 0; i < osc_count * 4; i++ )
		state->w40xx [i] = oscs [i >> 2]->regs [i & 3];
	state->w40xx [0x11] = dmc.dac;
	
	state->w4015    = osc_enables;
	state->w4017    = frame_mode;
	state->delay    = frame_delay;
	state->step     = frame;
	state->irq_flag = irq_flag;
	
	typedef apu_reflection<1> refl;
	Nes_Apu& apu = *(Nes_Apu*) this; // const_cast
	refl::reflect_square  ( state->square1,     apu.square1 );
	refl::reflect_square  ( state->square2,     apu.square2 );
	refl::reflect_triangle( state->triangle,    apu.triangle );
	refl::reflect_noise   ( state->noise,       apu.noise );
	refl::reflect_dmc     ( state->dmc,         apu.dmc );
}

void Nes_Apu::load_snapshot( apu_snapshot_t const& state )
{
	reset();
	
	write_register( 0, 0x4017, state.w4017 );
	write_register( 0, 0x4015, state.w4015 );
	
	for ( int i = 0; i < osc_count * 4; i++ )
	{
		int n = state.w40xx [i];
		oscs [i >> 2]->regs [i & 3] = n;
		write_register( 0, 0x4000 + i, n );
	}
	
	frame_delay = state.delay;
	frame       = state.step;
	irq_flag    = state.irq_flag;
	
	typedef apu_reflection<0> refl;
	apu_snapshot_t& st = (apu_snapshot_t&) state; // const_cast
	refl::reflect_square  ( st.square1,     square1 );
	refl::reflect_square  ( st.square2,     square2 );
	refl::reflect_triangle( st.triangle,    triangle );
	refl::reflect_noise   ( st.noise,       noise );
	refl::reflect_dmc     ( st.dmc,         dmc );
	dmc.recalc_irq();
	dmc.last_amp = dmc.dac;
}

