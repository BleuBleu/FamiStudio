
// Nes_Snd_Emu 0.1.7. http://www.slack.net/~ant/libs/

#include "Nonlinear_Buffer.h"

#include "Nes_Apu.h"

/* Library Copyright (C) 2003-2005 Shay Green. This library is free software;
you can redistribute it and/or modify it under the terms of the GNU Lesser
General Public License as published by the Free Software Foundation; either
version 2.1 of the License, or (at your option) any later version. This
module is distributed in the hope that it will be useful, but WITHOUT ANY
WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR
A PARTICULAR PURPOSE.  See the GNU Lesser General Public License for more
details. You should have received a copy of the GNU Lesser General Public
License along with this library; if not, write to the Free Software
Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA */

#include BLARGG_SOURCE_BEGIN
#include BLARGG_ENABLE_OPTIMIZER

// Nes_Nonlinearizer

Nes_Nonlinearizer::Nes_Nonlinearizer()
{
	nonlinear = false;
	
	double gain = 0x7fff * 1.3;
	// don't use entire range, so any overflow will stay within table
	int const range = (int)(half * 0.75); // to do: must match that in Nes_Apu.cpp
	for ( int i = 0; i < half * 2; i++ )
	{
		int out = i << shift;
		if ( i > half )
		{
			int j = i - half;
			if ( j >= range )
				j = range - 1;
			double n = 202.0 / (range - 1) * j;
			double d = 163.67 / (24329.0 / n + 100);
			out = int (d * gain) + 0x8000;
			assert( out < 0x10000 );
		}
		table [i] = out;
	}
	clear();
}
	
void Nes_Nonlinearizer::enable( Nes_Apu& apu, bool b )
{
	nonlinear = b;
	if ( b )
		apu.enable_nonlinear( 1.0 );
	else
		apu.volume( 1.0 );
}

long Nes_Nonlinearizer::make_nonlinear( Blip_Buffer& buf, long count )
{
	long avail = buf.samples_avail();
	if ( count > avail )
		count = avail;
	
	if ( count && nonlinear )
	{
		int const sample_shift = blip_sample_bits - 16;
		int const sample_frac_mask = ((1 << sample_shift) - 1);

		Blip_Buffer::buf_t_* p = buf.buffer_;

		long accum = this->accum;
		//long prev = lookup2(accum >> sample_shift, accum & sample_frac_mask);
		//long prev = accum >> sample_shift;
		long prev = lookup(accum >> sample_shift);

		for (unsigned n = count; n--; )
		{
			accum += (long)*p;
			//long entry = lookup2(accum >> sample_shift, accum & sample_frac_mask);
			//long entry = accum >> sample_shift;
			long entry = lookup(accum >> sample_shift);
			*p++ = (entry - prev) << sample_shift;
			prev = entry;
		}
				
		this->accum = accum;
	}
	
	return count;
}

