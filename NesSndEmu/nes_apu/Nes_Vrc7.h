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
	void treble_eq(blip_eq_t const&);
	void output(Blip_Buffer*);
	void end_frame(cpu_time_t);
	void write_register(cpu_time_t time, cpu_addr_t addr, int data);

	// TODO: Cache wave table. Also, how to cache mod table?
	enum { shadow_regs_count = 11 };
	static int addr_to_shadow_reg(int addr);
	static int shadow_reg_to_addr(int idx);

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
	int16_t sample_buffer[1000]; // This is very conservative, normally would be ceil(sample_rate / frame_rate).
	struct __OPLL* opll;
	Blip_Buffer* output_buffer;
};

inline int Nes_Vrc7::addr_to_shadow_reg(int addr)
{
	return -1; // addr >= regs_addr && addr < regs_addr + shadow_regs_count ? addr - regs_addr : -1;
}

inline int Nes_Vrc7::shadow_reg_to_addr(int idx)
{
	return 0; // regs_addr + idx;
}

#endif

