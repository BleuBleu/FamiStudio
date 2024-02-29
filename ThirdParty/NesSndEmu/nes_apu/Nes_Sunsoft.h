// Sunsoft 5B audio chip emulator for FamiStudio.
// Added to Nes_Snd_Emu by @NesBleuBleu.

#ifndef NES_SUNSOFT_H
#define NES_SUNSOFT_H

#include "Nes_Apu.h"

class Nes_Sunsoft {
public:
	Nes_Sunsoft();
	~Nes_Sunsoft();
	
	// See Nes_Apu.h for reference
	void reset();
	void volume( double );
	void output( Blip_Buffer* );
	void treble_eq(blip_eq_t const& eq);
	void enable_channel(int idx, bool enabled);
	long run_until(cpu_time_t);
	void end_frame( cpu_time_t );
	void mix_samples(blip_sample_t* sample_buffer, long sample_cnt);
	void write_register(cpu_time_t time, cpu_addr_t addr, int data);
	void get_register_values(struct sunsoft5b_register_values* regs);

	enum { psg_clock  = 1789773 };
	enum { reg_select = 0xc000  };
	enum { reg_write  = 0xe000 };
	enum { reg_range  = 0x2000 };

	enum { shadow_internal_regs_count = 16 };
	void start_seeking();
	void stop_seeking(blip_time_t& clock);
	void write_shadow_register(int addr, int data);

	void reset_triggers();
	int  get_channel_trigger(int idx) const;

private:
	// noncopyable
	Nes_Sunsoft( const Nes_Sunsoft& );
	Nes_Sunsoft& operator = ( const Nes_Sunsoft& );
	
	void reset_psg();

	int reg;
	BOOST::uint8_t ages[16];
	double vol;
	struct __PSG* psg;
	Blip_Buffer* output_buffer;
	cpu_time_t last_time;
	int delay;
	int last_amp;
	Blip_Synth<blip_med_quality, 6120> synth;
	int triggers[3];

	short shadow_internal_regs[shadow_internal_regs_count];
};

//// Must match the definition in NesApu.cs.
struct sunsoft5b_register_values
{
	// e000 (Internal registers 0 to f).
	unsigned char regs[16];
	unsigned char ages[16];
};

#endif

