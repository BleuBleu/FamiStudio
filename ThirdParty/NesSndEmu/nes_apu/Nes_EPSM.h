// EPSM 5B audio chip emulator for FamiStudio.
// Added to Nes_Snd_Emu by @NesBleuBleu.

#ifndef NES_EPSM_H
#define NES_EPSM_H

#include "Nes_Apu.h"
#include "ym3438.h"

struct epsm_write
{
	int            addr;
	BOOST::uint8_t data;
};

class epsm_write_queue
{
private:
	enum { queue_size = 1024 };
	int queue_tail;
	int queue_head;
	epsm_write queue[queue_size];

public:
	epsm_write_queue() : queue_tail(0), queue_head(0)
	{
	}

	inline bool empty()
	{
		return queue_tail == queue_head;
	}

	inline void push(int addr, BOOST::uint8_t data)
	{
		queue[queue_head].addr = addr;
		queue[queue_head].data = data;
		queue_head = (queue_head + 1) % queue_size;
		assert(queue_head != queue_tail);
	}

	inline epsm_write pop()
	{
		assert(queue_head != queue_tail);
		int last_tail = queue_tail;
		queue_tail = (queue_tail + 1) % queue_size;
		return queue[last_tail];
	}
};

class Nes_EPSM {
public:
	Nes_EPSM();
	~Nes_EPSM();

	// See Nes_Apu.h for reference
	void reset();
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

	enum { psg_clock = 4000000 };
	enum { reg_select = 0x401c };
	enum { reg_write = 0x401d };
	enum { reg_select2 = 0x401e };
	enum { reg_write2 = 0x401f };
	enum { reg_range = 0x1 };

	enum { shadow_internal_regs_count = 220 };
	void start_seeking();
	void stop_seeking(blip_time_t& clock);
	void write_shadow_register(int addr, int data);

private:

	// noncopyable
	Nes_EPSM(const Nes_EPSM&);
	Nes_EPSM& operator = (const Nes_EPSM&);
	
	void reset_psg();
	void reset_opn2();

	int reg;
	BOOST::uint8_t a0;
	BOOST::uint8_t a1;
	BOOST::uint8_t current_register;
	BOOST::uint8_t mask_fm;
	BOOST::uint8_t maskRythm;
	double vol;
	struct __PSG* psg;
	ym3438_t opn2;

	Blip_Buffer* output_buffer;
	Blip_Buffer* output_buffer_right;
	cpu_time_t last_time;
	int delay;
	int last_amp;
	int last_amp_right;
	uint16_t opn2_mask;
	Blip_Synth<blip_med_quality, 15420> synth;
	Blip_Synth<blip_med_quality, 15420> synth_right;

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
