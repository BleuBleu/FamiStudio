
// NES 2A03 APU sound chip emulator

// Nes_Snd_Emu 0.1.7. Copyright (C) 2003-2005 Shay Green. GNU LGPL license.

#ifndef NES_APU_H
#define NES_APU_H

typedef long     cpu_time_t; // CPU clock cycle count
typedef unsigned cpu_addr_t; // 16-bit memory address

#include "Nes_Oscs.h"

struct apu_snapshot_t;
class Nonlinear_Buffer;

extern const unsigned char length_table[0x20];

class Nes_Apu {
public:
	Nes_Apu();
	~Nes_Apu();
	
	// Set buffer to generate all sound into, or disable sound if NULL
	void output( Blip_Buffer* );
	
	// Set memory reader callback used by DMC oscillator to fetch samples.
	// When callback is invoked, 'user_data' is passed unchanged as the
	// first parameter.
	void dmc_reader( int (*callback)( void* user_data, cpu_addr_t ), void* user_data = NULL );
	
	// All time values are the number of CPU clock cycles relative to the
	// beginning of the current time frame. Before resetting the CPU clock
	// count, call end_frame( last_cpu_time ).
	
	// Write to register (0x4000-0x4017, except 0x4014 and 0x4016)
	enum { start_addr = 0x4000 };
	enum { end_addr   = 0x4017 };
	void write_register( cpu_time_t, cpu_addr_t, int data );
	
	// Read from status register at 0x4015
	enum { status_addr = 0x4015 };
	int read_status( cpu_time_t );
	
	// Run all oscillators up to specified time, end current time frame, then
	// start a new time frame at time 0. Time frames have no effect on emulation
	// and each can be whatever length is convenient.
	void end_frame( cpu_time_t );
	
// Additional optional features (can be ignored without any problem)

	// Reset internal frame counter, registers, and all oscillators.
	// Use PAL timing if pal_timing is true, otherwise use NTSC timing.
	// Set the DMC oscillator's initial DAC value to initial_dmc_dac without
	// any audible click.
	void reset( bool pal_timing = false, int initial_dmc_dac = 0 );
	
	// Save/load snapshot of exact emulation state
	void save_snapshot( apu_snapshot_t* out ) const;
	void load_snapshot( apu_snapshot_t const& );
	
	// Set overall volume (default is 1.0)
	void volume( double );
	
	// Reset oscillator amplitudes. Must be called when clearing buffer while
	// using non-linear sound.
	void buffer_cleared();
	
	// Set treble equalization (see notes.txt).
	void treble_eq( const blip_eq_t& );
	
	// Set sound output of specific oscillator to buffer. If buffer is NULL,
	// the specified oscillator is muted and emulation accuracy is reduced.
	// The oscillators are indexed as follows: 0) Square 1, 1) Square 2,
	// 2) Triangle, 3) Noise, 4) DMC.
	enum { osc_count = 5 };
	void osc_output( int index, Blip_Buffer* buffer );
	
	// Set IRQ time callback that is invoked when the time of earliest IRQ
	// may have changed, or NULL to disable. When callback is invoked,
	// 'user_data' is passed unchanged as the first parameter.
	void irq_notifier( void (*callback)( void* user_data ), void* user_data = NULL );
	
	// Get time that APU-generated IRQ will occur if no further register reads
	// or writes occur. If IRQ is already pending, returns irq_waiting. If no
	// IRQ will occur, returns no_irq.
	enum { no_irq = INT_MAX / 2 + 1 };
	enum { irq_waiting = 0 };
	cpu_time_t earliest_irq() const;
	
	// Count number of DMC reads that would occur if 'run_until( t )' were executed.
	// If last_read is not NULL, set *last_read to the earliest time that
	// 'count_dmc_reads( time )' would result in the same result.
	int count_dmc_reads( cpu_time_t t, cpu_time_t* last_read = NULL ) const;
	
	// Run APU until specified time, so that any DMC memory reads can be
	// accounted for (i.e. inserting CPU wait states).
	void run_until( cpu_time_t );
	
	// Not caching DPCM regs.
	enum { shadow_regs_count = 21 }; 
	void start_seeking();
	void stop_seeking(blip_time_t& clock);
	void write_shadow_register(int addr, int data);

// End of public interface.
private:
	friend class Nes_Nonlinearizer;
	void enable_nonlinear( double volume );
private:
	// noncopyable
	Nes_Apu( const Nes_Apu& );
	Nes_Apu& operator = ( const Nes_Apu& );
	
	Nes_Osc*            oscs [osc_count];
	Nes_Square          square1;
	Nes_Square          square2;
	Nes_Noise           noise;
	Nes_Triangle        triangle;
	Nes_Dmc             dmc;
	
	cpu_time_t last_time; // has been run until this time in current frame
	cpu_time_t earliest_irq_;
	cpu_time_t next_irq;
	int frame_period;
	int frame_delay; // cycles until frame counter runs next
	int frame; // current frame (0-3)
	int osc_enables;
	int frame_mode;
	bool irq_flag;
	void (*irq_notifier_)( void* user_data );
	void* irq_data;
	Nes_Square::Synth square_synth; // shared by squares
	
	short shadow_regs[shadow_regs_count];

	void irq_changed();
	void state_restored();
	
	friend struct Nes_Dmc;
};

inline void Nes_Apu::osc_output( int osc, Blip_Buffer* buf )
{
	assert(( "Nes_Apu::osc_output(): Index out of range", 0 <= osc && osc < osc_count ));
	oscs [osc]->output = buf;
}

inline cpu_time_t Nes_Apu::earliest_irq() const
{
	return earliest_irq_;
}

inline void Nes_Apu::dmc_reader( int (*func)( void*, cpu_addr_t ), void* user_data )
{
	dmc.rom_reader_data = user_data;
	dmc.rom_reader = func;
}

inline void Nes_Apu::irq_notifier( void (*func)( void* user_data ), void* user_data )
{
	irq_notifier_ = func;
	irq_data = user_data;
}

inline int Nes_Apu::count_dmc_reads( cpu_time_t time, cpu_time_t* last_read ) const
{
	return dmc.count_reads( time, last_read );
}

#endif

