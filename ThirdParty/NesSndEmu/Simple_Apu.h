
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
#include "nes_apu/Nes_EPSM.h"
#include "nes_apu/Nes_Fme7.h"
#include "nes_apu/Blip_Buffer.h"

class Simple_Apu {
public:

	enum { expansion_none       = 0 };
	enum { expansion_vrc6       = 1 };
	enum { expansion_vrc7       = 2 };
	enum { expansion_fds        = 3 };
	enum { expansion_mmc5       = 4 };
	enum { expansion_namco      = 5 };
	enum { expansion_sunsoft    = 6 };
	enum { expansion_epsm		= 7 };

	enum { channel_square1      = 0 };
	enum { channel_square2      = 1 };
	enum { channel_triangle     = 2 };
	enum { channel_noise        = 3 };
	enum { channel_dpcm         = 4 };
	enum { channel_vrc6_square1 = 5 };
	enum { channel_vrc6_square2 = 6 };
	enum { channel_vrc6_saw     = 7 };
	enum { channel_vrc7_fm1     = 8 };
	enum { channel_vrc7_fm2     = 9 };
	enum { channel_vrc7_fm3     = 10 };
	enum { channel_vrc7_fm4     = 11 };
	enum { channel_vrc7_fm5     = 12 };
	enum { channel_vrc7_fm6     = 13 };
	enum { channel_fds          = 14 };
	enum { channel_mmc5_square1 = 15 };
	enum { channel_mmc5_square2 = 16 };
	enum { channel_mmc5_dpcm    = 17 };
	enum { channel_n163_wave1   = 18 };
	enum { channel_n163_wave2   = 19 };
	enum { channel_n163_wave3   = 20 };
	enum { channel_n163_wave4   = 21 };
	enum { channel_n163_wave5   = 22 };
	enum { channel_n163_wave6   = 23 };
	enum { channel_n163_wave7   = 24 };
	enum { channel_n163_wave8   = 25 };
	enum { channel_s5b_square1  = 26 };
	enum { channel_s5b_square2  = 27 };
	enum { channel_s5b_square3  = 28 };
	enum { channel_epsm_square1 = 29 };
	enum { channel_epsm_square2 = 30 };
	enum { channel_epsm_square3 = 31 };
	enum { channel_epsm_fm1		= 32 };
	enum { channel_epsm_fm2		= 33 };
	enum { channel_epsm_fm3		= 34 };
	enum { channel_epsm_fm4		= 35 };
	enum { channel_epsm_fm5		= 36 };
	enum { channel_epsm_fm6		= 37 };
	enum { channel_epsm_rythm1	= 38 };
	enum { channel_epsm_rythm2	= 39 };
	enum { channel_epsm_rythm3	= 40 };
	enum { channel_epsm_rythm4	= 41 };
	enum { channel_epsm_rythm5	= 42 };
	enum { channel_epsm_rythm6	= 43 };


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
	void skip_cycles(long cycles);
	
	// Resets
	void reset();

	void set_audio_expansion(long exp);
	int get_audio_expansion() const { return expansion; }

	// Number of samples in buffer
	long samples_avail() const;

	void enable_channel(int, bool);
	
	void treble_eq(int exp, double treble, int sample_rate);
	void set_expansion_volume(int expansion, double evolume);

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
	bool pal_mode;
	bool seeking;
	int  expansion;
	Nes_Apu apu;
	Nes_Vrc6 vrc6;
	Nes_Vrc7 vrc7;
	Nes_Fds fds;
	Nes_Mmc5 mmc5;
	Nes_Namco namco;
	Nes_Sunsoft sunsoft; // My version, based on emu2149
	//Nes_Fme7 sunsoft; // Blaarg's version from Game_Music_Emu.
	Nes_EPSM epsm;
	Blip_Buffer buf;
	Blip_Buffer bufLeft;
	Blip_Buffer bufRight;
	blip_time_t time;
	blip_time_t frame_length;
	blip_time_t clock() { return time += 4; }
};

#endif

