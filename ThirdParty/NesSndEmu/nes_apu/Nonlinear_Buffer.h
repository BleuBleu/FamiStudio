
// NES non-linear audio output handling.

// Nes_Snd_Emu 0.1.7. Copyright (C) 2003-2005 Shay Green. GNU LGPL license.

#ifndef NONLINEAR_BUFFER_H
#define NONLINEAR_BUFFER_H

#include "Blip_Buffer.h"
#include "blargg_common.h"

class Nes_Apu;

// Use to make samples non-linear in Blip_Buffer used for triangle, noise, and DMC only
class Nes_Nonlinearizer {
public:
	Nes_Nonlinearizer();
	
	// Must be called when buffer is cleared
	void clear() { accum = 0; }
	
	// Enable/disable non-linear output
	void enable( Nes_Apu&, bool = true );
	
	// Make at most 'count' samples in buffer non-linear and return number
	// of samples modified. This many samples must then be read out of the buffer.
	long make_nonlinear( Blip_Buffer&, long count );

	inline long lookup(long s)
	{
		// Here we have no low-pass filter, so the (shifted) samples are in the 0-65535 range.
		if (s < 0) s = 0;
		else if (s > 0xffff) s = 0xffff;

		assert((s >> shift) < half * 2); // MATTT Disable!

		float tnd = (s / 65535.0f * 202.0f);
		return (long)((163.67f / (24329.0f / tnd + 100.0f)) * 65535.0f);

		//return table[((s) >> shift) & entry_mask];
	}

	inline long lookup2(long s, long f)
	{
		// Here we have no low-pass filter, so the (shifted) samples are in the 0-65535 range.
		if (s < 0) s = 0;
		else if (s > 0xffff) s = 0xffff;

		// MATTT Disable!
		assert((s >> shift) < half * 2);
		assert(f >= 0 && f < 0x4000); // 14-bit of fraction.

		// Interpolate the 2 table entries.
		long v0 = table[(((s) >> shift) + 0) & entry_mask];
		long v1 = table[(((s) >> shift) + 1) & entry_mask];

		return (v0 * (0x3fff - f) + v1 * f) >> 14;
	}

private:
	enum { shift = 5 };
	enum { half = 0x8000 >> shift };
	enum { entry_mask = half * 2 - 1 };
	BOOST::uint16_t table [half * 2];
	long accum;
	bool nonlinear;
};

#endif

