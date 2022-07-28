// Sunsoft FME-7 sound emulator

// Game_Music_Emu 0.5.2
#ifndef NES_FME7_APU_H
#define NES_FME7_APU_H

#include "blargg_common.h"
#include "Nes_Apu.h"

struct fme7_apu_state_t
{
	enum { reg_count = 14 };
	BOOST::uint8_t regs [reg_count];
	BOOST::uint8_t ages [reg_count];
	BOOST::uint8_t phases [3]; // 0 or 1
	BOOST::uint8_t latch;
	BOOST::uint16_t delays [3]; // a, b, c
};

class Nes_Fme7 : private fme7_apu_state_t {
public:
	// See Nes_Apu.h for reference
	void reset();
	void volume( double );
	void treble_eq( blip_eq_t const& );
	void output( Blip_Buffer* );
	enum { osc_count = 3 };
	void osc_output( int index, Blip_Buffer* );
	void run_until(blip_time_t);
	void end_frame( blip_time_t );
	void save_state( fme7_apu_state_t* ) const;
	void load_state( fme7_apu_state_t const& );
	
	// Mask and addresses of registers
	enum { addr_mask = 0xE000 };
	enum { data_addr = 0xE000 };
	enum { latch_addr = 0xC000 };
	
	void write_register(cpu_time_t time, cpu_addr_t addr, int data);
	void get_register_values(struct s5b_register_values* regs);

	void reset_triggers();
	int  get_channel_trigger(int idx) const;

	// (addr & addr_mask) == latch_addr
	void write_latch( int );
	
	// (addr & addr_mask) == data_addr
	void write_data( blip_time_t, int data );

	enum { shadow_internal_regs_count = 16 };
	void start_seeking();
	void stop_seeking(blip_time_t& clock);
	void write_shadow_register(int addr, int data);

public:
	Nes_Fme7();
	//BLARGG_DISABLE_NOTHROW
private:
	// noncopyable
	Nes_Fme7( const Nes_Fme7& );
	Nes_Fme7& operator = ( const Nes_Fme7& );
	
	static unsigned char const amp_table [16];
	
	struct {
		Blip_Buffer* output;
		int last_amp;
		int trigger;
	} oscs [osc_count];
	blip_time_t last_time;
	
	enum { amp_range = 192 }; // can be any value; this gives best error/quality tradeoff
	Blip_Synth<blip_good_quality,1> synth;

	short shadow_internal_regs[shadow_internal_regs_count];
};

inline void Nes_Fme7::volume( double v )
{
	synth.volume( 0.39 / amp_range * v ); // to do: fine-tune
}

inline void Nes_Fme7::treble_eq( blip_eq_t const& eq )
{
	synth.treble_eq( eq );
}

inline void Nes_Fme7::osc_output( int i, Blip_Buffer* buf )
{
	assert( (unsigned) i < osc_count );
	oscs [i].output = buf;
}

inline void Nes_Fme7::output( Blip_Buffer* buf )
{
	for ( int i = 0; i < osc_count; i++ )
		osc_output( i, buf );
}

inline Nes_Fme7::Nes_Fme7()
{
	output( NULL );
	volume( 1.0 );
	reset();
}

inline void Nes_Fme7::write_latch( int data ) { latch = data; }

inline void Nes_Fme7::write_data( blip_time_t time, int data )
{
	if ( (unsigned) latch >= reg_count )
	{
		#ifdef dprintf
			dprintf( "FME7 write to %02X (past end of sound registers)\n", (int) latch );
		#endif
		return;
	}
	
	run_until( time );
	regs [latch] = data;
	ages [latch] = 0;
}

inline void Nes_Fme7::end_frame( blip_time_t time )
{
	if ( time > last_time )
		run_until( time );
	
	assert( last_time >= time );
	last_time -= time;
}

inline void Nes_Fme7::save_state( fme7_apu_state_t* out ) const
{
	*out = *this;
}

inline void Nes_Fme7::load_state( fme7_apu_state_t const& in )
{
	reset();
	fme7_apu_state_t* state = this;
	*state = in;
}

// Must match the definition in NesApu.cs.
struct s5b_register_values
{
	// e000 (Internal registers 0 to f).
	unsigned char regs[16];
	unsigned char ages[16];
};

#endif
