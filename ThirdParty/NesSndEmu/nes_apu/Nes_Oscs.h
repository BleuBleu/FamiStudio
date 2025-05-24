
// Private oscillators used by Nes_Apu

// Nes_Snd_Emu 0.1.7. Copyright (C) 2003-2005 Shay Green. GNU LGPL license.

#ifndef NES_OSCS_H
#define NES_OSCS_H

#include "Blip_Buffer.h"
#include "blargg_common.h"

class Nes_Apu;

struct Nes_Osc
{
	unsigned char regs [4];
	unsigned char ages [4];
	bool reg_written [4];
	Blip_Buffer* output;
	int length_counter;// length counter (0 if unused by oscillator)
	int delay;      // delay until next (potential) transition
	int last_amp;   // last amplitude oscillator was outputting
	int trigger;

	void clock_length( int halt_mask );
	int period() const {
		return (regs [3] & 7) * 0x100 + (regs [2] & 0xff);
	}
	void reset() {
		delay = 0;
		last_amp = 0;
		memset(ages, 0, sizeof(ages));
	}
	int update_amp( int amp ) {
		int delta = amp - last_amp;
		last_amp = amp;
		return delta;
	}
	virtual void set_output(Blip_Buffer* o)
	{
		output = o;
	}
};

struct Nes_Envelope : Nes_Osc
{
	int envelope;
	int env_delay;
	
	void clock_envelope();
	int volume() const;
	void reset() {
		envelope = 0;
		env_delay = 0;
		Nes_Osc::reset();
	}
};

// Nes_Square
struct Nes_Square : Nes_Envelope
{
	enum { negate_flag = 0x08 };
	enum { shift_mask = 0x07 };
	enum { phase_range = 8 };
	int phase;
	int sweep_delay;
	int min_period;
	
	typedef Blip_Synth<blip_good_quality,30> Synth; // Should in theory be 30 since it's shared
	const Synth* synth; // shared between squares
	
	Nes_Square() : min_period(8) {}
	void clock_sweep( int adjust );
	void run( cpu_time_t, cpu_time_t );
	void reset() {
		sweep_delay = 0;
		Nes_Envelope::reset();
	}
	cpu_time_t maintain_phase( cpu_time_t time, cpu_time_t end_time,
			cpu_time_t timer_period );
};

// Nes_Triangle
struct Nes_Triangle : Nes_Osc
{
	enum { phase_range = 16 };
	int phase;
	int linear_counter;
	Blip_Synth<blip_good_quality,15> synth;
	
	int calc_amp() const;
	void run( cpu_time_t, cpu_time_t );
	void clock_linear_counter();
	void reset() {
		linear_counter = 0;
		// According to NESDev, the triangle actually initialises on 
		// level 15 after a reset, which is phase 1 here. Songs with no triangle 
		// would be affected if we start on 0 (louder DPCM and Noise than hardware).
		phase = 1;
		Nes_Osc::reset();
	}
	cpu_time_t maintain_phase( cpu_time_t time, cpu_time_t end_time,
			cpu_time_t timer_period );
};

// Nes_Noise
struct Nes_Noise : Nes_Envelope
{
	int noise;
	bool pal_mode;
	Blip_Synth<blip_good_quality,15> synth;
	
	void run( cpu_time_t, cpu_time_t );
	void reset() {
		// Although the specs says it is initialized at 1, the reality 
		// is that by the time any music starts playing, it will be 
		// in a random state. This will avoid having to skip cycles
		// at the beginning of the song.
		noise = 4141;
		Nes_Envelope::reset();
	}
};

// Nes_Dmc
struct Nes_Dmc : Nes_Osc
{
	int address;    // address of next byte to read
	int period;
	//int length_counter; // bytes remaining to play (already defined in Nes_Osc)
	int buf;
	int bits_remain;
	int bits;
	bool buf_full;
	bool silence;
	
	enum { loop_flag = 0x40 };
	
	int dac;
	int paused_dac;
	
	cpu_time_t next_irq;
	bool irq_enabled;
	bool irq_flag;
	bool pal_mode;
	bool nonlinear;
	
	int (*rom_reader)( void*, cpu_addr_t ); // needs to be initialized to rom read function
	void* rom_reader_data;
	
	Nes_Apu* apu;
	
	Blip_Synth<blip_med_quality,127> synth;
	
	void start();
	void write_register( int, int );
	void run( cpu_time_t, cpu_time_t );
	void recalc_irq();
	void fill_buffer();
	void reload_sample();
	void reset();
	virtual void set_output(Blip_Buffer* output) override;
	int count_reads( cpu_time_t, cpu_time_t* ) const;
	cpu_time_t next_read_time() const;
};

// Must match the definition in NesApu.cs.
struct apu_register_values
{
	// $4000 to $4013
	unsigned char regs[20];
	unsigned char ages[20];

	// Extra internal states.
	unsigned short dpcm_bytes_left;
	unsigned char  dpcm_dac;
};

#endif

