
// NES 2A03 APU sound chip emulator with simpler interface

// Nes_Snd_Emu 0.1.7. Copyright (C) 2003-2005 Shay Green. GNU LGPL license.

#ifndef SIMPLE_APU_H
#define SIMPLE_APU_H

#include "nes_apu/Nes_Apu.h"
#include "nes_apu/Nes_Vrc6.h"
#include "nes_apu/Nes_Vrc7.h"
#include "nes_apu/Nes_Fds.h"
#include "nes_apu/Nes_Mmc5.h"
#include "nes_apu/Nes_Namco.h"
#include "nes_apu/Nes_Sunsoft.h"
#include "nes_apu/Blip_Buffer.h"

class Simple_Apu {
public:

	enum { expansion_none    = 0 };
	enum { expansion_vrc6    = 1 };
	enum { expansion_vrc7    = 2 };
	enum { expansion_fds     = 3 };
	enum { expansion_mmc5    = 4 };
	enum { expansion_namco   = 5 };
	enum { expansion_sunsoft = 6 };

	Simple_Apu();
	~Simple_Apu();
	
	// This simpler interface works well for most games. Some benefit from
	// the higher precision of the full Nes_Apu interface, which provides
	// clock-cycle accurate register read/write and IRQ timing functions.
	
	// Set function for APU to call when it needs to read memory (DMC samples)
	void dmc_reader( int (*callback)( void* user_data, cpu_addr_t ), void* user_data = NULL );
	
	// Set output sample rate
	blargg_err_t sample_rate( long rate, bool pal );
	
	// Write to register (0x4000-0x4017, except 0x4014 and 0x4016)
	void write_register( cpu_addr_t, int data );
	
	// Read from status register at 0x4015
	int read_status();
	
	// End a 1/60 sound frame
	void end_frame();
	
	// Resets
	void reset();

	void set_audio_expansion(long exp);
	int get_audio_expansion() const { return expansion; }

	// Number of samples in buffer
	long samples_avail() const;

	void enable_channel(int, bool);
	
	void treble_eq(int exp, double treble, int cutoff, int sample_rate);

	// Read at most 'count' samples and return number of samples actually read
	typedef blip_sample_t sample_t;
	long read_samples( sample_t* buf, long buf_size );

	// Discard 'count' samples.
	void remove_samples(long buf_size);
	
	// Save/load snapshot of emulation state
	void save_snapshot( apu_snapshot_t* out ) const;
	void load_snapshot( apu_snapshot_t const& );

	void start_seeking();
	void stop_seeking();
	bool is_seeking() const { return seeking; }

private:
	bool seeking;
	int  expansion;
	Nes_Apu apu;
	Nes_Vrc6 vrc6;
	Nes_Vrc7 vrc7;
	Nes_Fds fds;
	Nes_Mmc5 mmc5;
	Nes_Namco namco;
	Nes_Sunsoft sunsoft;
	Blip_Buffer buf;
	blip_time_t time;
	blip_time_t frame_length;
	blip_time_t clock() { return time += 4; }
};

#endif

