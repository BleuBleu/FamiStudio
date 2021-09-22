// EPSM 5B audio chip emulator for FamiStudio.
// Added to Nes_Snd_Emu by @NesBleuBleu.

#ifndef NES_EPSM_H
#define NES_EPSM_H

#include "Nes_Apu.h"
#include "ym3438.h"
#include <queue>
#include <array>

class Nes_EPSM {
public:
	Nes_EPSM();
	~Nes_EPSM();

	// See Nes_Apu.h for reference
	void reset();
	void volume(double);
	void output(Blip_Buffer*);
	void treble_eq(blip_eq_t const& eq);
	void enable_channel(int idx, bool enabled);
	void end_frame(cpu_time_t);
	void mix_samples(blip_sample_t* sample_buffer, long sample_cnt);
	void write_register(cpu_time_t time, cpu_addr_t addr, int data);
	void WriteToChip(uint8_t a, uint8_t d, cpu_time_t time);

	enum { psg_clock = 4000000 };
	enum { reg_select = 0xc000 };
	enum { reg_write = 0xe000 };
	enum { reg_range = 0x2000 };

	enum { shadow_internal_regs_count = 76 };
	void start_seeking();
	void stop_seeking(blip_time_t& clock);
	void write_shadow_register(int addr, int data);

private:
	// noncopyable
	Nes_EPSM(const Nes_EPSM&);
	Nes_EPSM& operator = (const Nes_EPSM&);
	std::queue<int> dataWrite;
	std::queue<int> aWrite;

	void reset_psg();
	void reset_opn2();

	int reg;
	uint8_t a0;
	uint8_t a1;
	double vol;
	struct __PSG* psg;
	ym3438_t opn2;

	Blip_Buffer* output_buffer;
	cpu_time_t last_time;
	int last_amp;
	Blip_Synth<blip_med_quality, 15420> synth;

	short shadow_internal_regs[shadow_internal_regs_count];
};

#endif