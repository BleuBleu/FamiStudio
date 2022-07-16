// VRC7 audio chip emulator for FamiStudio.
// Added to Nes_Snd_Emu by @NesBleuBleu, using the YM2413 emulator by Mitsutaka Okazaki.

#ifndef NES_VRC7_H
#define NES_VRC7_H

#include "Nes_Apu.h"

class Nes_Vrc7 {
public:
	Nes_Vrc7();
	~Nes_Vrc7();

	// See Nes_Apu.h for reference
	void reset();
	void volume(double);
	void output(Blip_Buffer*);
	void treble_eq(blip_eq_t const& eq);
	void enable_channel(int idx, bool enabled);
	void run_until(cpu_time_t);
	void end_frame(cpu_time_t);
	void write_register(cpu_time_t time, cpu_addr_t addr, int data);
	void get_register_values(struct vrc7_register_values* regs);

	enum { shadow_regs_count = 1 };
	enum { shadow_internal_regs_count = 54 };
	void start_seeking();
	void stop_seeking(blip_time_t& clock);
	void write_shadow_register(int addr, int data);

	enum { vrc7_clock  = 3579545 };
	enum { reg_silence = 0xe000  };
	enum { reg_select  = 0x9010  };
	enum { reg_write   = 0x9030  };

private:
	// noncopyable
	Nes_Vrc7(const Nes_Vrc7&);
	Nes_Vrc7& operator = (const Nes_Vrc7&);

	void reset_opll();

	bool silence;
	BOOST::uint8_t silence_age;
	BOOST::uint8_t regs_age[54];
	int reg;
	struct __OPLL* opll;
	Blip_Buffer* output_buffer;
	cpu_time_t last_time;
	int delay;
	int last_amp;
	Blip_Synth<blip_med_quality, 7200> synth;

	short shadow_regs[shadow_regs_count];
	short shadow_internal_regs[shadow_internal_regs_count];

};

// Must match the definition in NesApu.cs.
struct vrc7_register_values
{  
	// $e000 
	unsigned char regs[1];
	unsigned char ages[1];

	// $9030 (Internal registers $10 to $35)
	unsigned char internal_regs[54];
	unsigned char internal_ages[54];
};

#endif

