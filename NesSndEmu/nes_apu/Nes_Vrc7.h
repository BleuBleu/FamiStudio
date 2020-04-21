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
	void enable_channel(int idx, bool enabled);
	void end_frame(cpu_time_t);
	void mix_samples(blip_sample_t* p, long s);
	void write_register(cpu_time_t time, cpu_addr_t addr, int data);

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
	int reg;
	double vol;
	struct __OPLL* opll;
	Blip_Buffer* output_buffer;

	short shadow_regs[shadow_regs_count];
	short shadow_internal_regs[shadow_internal_regs_count];

};

#endif

