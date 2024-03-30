// EPSM 5B audio chip emulator for FamiStudio.
// Added to Nes_Snd_Emu by @NesBleuBleu.

#ifndef NES_EPSM_H
#define NES_EPSM_H

#include "Nes_Apu.h"
#include "ym3438.h"

class Nes_EPSM {
public:
	Nes_EPSM();
	~Nes_EPSM();

	// See Nes_Apu.h for reference
	void reset(bool pal);
	void volume(double);
	void output(Blip_Buffer*, Blip_Buffer*);
	void treble_eq(blip_eq_t const& eq);
	void enable_channel(int idx, bool enabled);
	long run_until(cpu_time_t);
	void end_frame(cpu_time_t);
	void mix_samples(blip_sample_t* sample_buffer, long sample_cnt);
	void write_register(cpu_time_t time, cpu_addr_t addr, int data);
	void get_register_values(struct epsm_register_values* regs);
	void WriteToChip(uint8_t a, uint8_t d, cpu_time_t time);


	unsigned char regs_a0[184];
	unsigned char ages_a0[184];
	unsigned char regs_a1[184];
	unsigned char ages_a1[184];

	enum { psg_clock = 2000000 };	// EPSM uses a de facto prescaler of 4 for its SSG
	enum { epsm_clock = 8000000 };
	enum { ntsc_clock = 1789773 };
	enum { pal_clock = 1662607 };
	enum { epsm_internal_multiplier = 6 }; //no clue why it behaves like this
	enum { reg_select = 0x401c };
	enum { reg_write = 0x401d };
	enum { reg_select2 = 0x401e };
	enum { reg_write2 = 0x401f };
	enum { reg_range = 0x1 };
	enum { reg_addr_cycle_skip = 4 };
	enum { reg_data_cycle_skip = 20 };

	enum { shadow_internal_regs_count = 220 };
	void start_seeking();
	void stop_seeking(blip_time_t& clock);
	void write_shadow_register(int addr, int data);

	void reset_triggers(bool force_none = false);
	int  get_channel_trigger(int idx) const;

private:

	// noncopyable
	Nes_EPSM(const Nes_EPSM&);
	Nes_EPSM& operator = (const Nes_EPSM&);
	
	void reset_psg();
	void reset_opn2();

	int reg;
	BOOST::uint8_t current_register;
	BOOST::uint8_t mask_fm;
	BOOST::uint8_t mask_rhythm;
	double vol;
	struct __PSG* psg;
	ym3438_t opn2;

	Blip_Buffer* output_buffer_left;
	Blip_Buffer* output_buffer_right;
	cpu_time_t last_time;
	bool pal_mode;
	int psg_delay;
	int opn2_delay;
	int last_psg_amp;
	int sample_left;
	int sample_right;
	int last_opn2_amp_left;
	int last_opn2_amp_right;
	uint16_t opn2_mask;
	Blip_Synth<blip_med_quality, 163430> synth_left;
	Blip_Synth<blip_med_quality, 163430> synth_right;
	int triggers[15];

	const int epsm_time_precision = 14;

	short shadow_internal_regs[shadow_internal_regs_count];
	short shadow_internal_regs2[shadow_internal_regs_count];
};


// Must match the definition in NesApu.cs.
struct epsm_register_values
{
	// $9030 (Internal registers $10 to $35)
	unsigned char regs_a0[184];
	unsigned char ages_a0[184];
	unsigned char regs_a1[184];
	unsigned char ages_a1[184];
};
#endif
