
// Namco 106 sound chip emulator

// Nes_Snd_Emu 0.1.7. Copyright (C) 2003-2005 Shay Green. GNU LGPL license.

#ifndef NES_NAMCO_H
#define NES_NAMCO_H

#include "Nes_Apu.h"

struct namco_snapshot_t;

class Nes_Namco {
public:
	Nes_Namco();
	~Nes_Namco();
	
	// See Nes_Apu.h for reference.
	void volume( double );
	void treble_eq( const blip_eq_t& );
	void output( Blip_Buffer* );
	enum { osc_count = 8 };
	void osc_output( int index, Blip_Buffer* );
	void reset();
	void end_frame( cpu_time_t );
	
	// Read/write data register is at 0x4800 to 0x4fff.
	enum { data_reg_addr = 0x4800 };
	enum { reg_range = 0x800 };
	void write_data( cpu_time_t, int );
	int read_data();
	
	// Write-only address register is at 0xF800 to 0xffff.
	enum { addr_reg_addr = 0xF800 };
	void write_addr( int );

	void write_register( cpu_time_t, int addr, int data );
	
	// to do: implement save/restore
	void save_snapshot( namco_snapshot_t* out );
	void load_snapshot( namco_snapshot_t const& );
	
	enum { shadow_internal_regs_count = 128 };
	void start_seeking();
	void stop_seeking(blip_time_t& clock);
	void write_shadow_register(int addr, int data);

private:
	// noncopyable
	Nes_Namco( const Nes_Namco& );
	Nes_Namco& operator = ( const Nes_Namco& );
	
	enum { osc_update_time = 15 };

	struct Namco_Osc {
		long delay;
		short sample;
		Blip_Buffer* output;
	};
	
	Namco_Osc oscs [osc_count];
	
	cpu_time_t last_time;
	int addr_reg;
	int last_amp;
	int active_osc;
	long delay;
	
	enum { reg_count = 0x80 };
	BOOST::uint8_t reg [reg_count];
	Blip_Synth<blip_good_quality,225> synth;
	Blip_Buffer* buffer;

	short shadow_internal_regs[shadow_internal_regs_count];

	BOOST::uint8_t& access();
	void run_until( cpu_time_t );
};

inline void Nes_Namco::volume( double v ) { synth.volume( 0.66 * v ); }

inline void Nes_Namco::treble_eq( const blip_eq_t& eq ) { synth.treble_eq( eq ); }

inline void Nes_Namco::write_addr( int v ) { addr_reg = v; }

inline int Nes_Namco::read_data() { return access(); }

inline void Nes_Namco::osc_output( int i, Blip_Buffer* buf )
{
	assert( (unsigned) i < osc_count );
	oscs [osc_count - i - 1].output = buf;
}

inline void Nes_Namco::write_data( cpu_time_t time, int data )
{
	if (time > last_time)
		run_until( time );
	access() = data;
}

inline void Nes_Namco::write_register(cpu_time_t time, int addr, int data)
{
	if (addr >= addr_reg_addr && addr < (addr_reg_addr + reg_range))
		write_addr(data);
	else if (addr >= data_reg_addr && addr < (data_reg_addr + reg_range))
		write_data(time, data);
}

#endif

