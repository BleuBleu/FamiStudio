// MMC5 audio chip emulator for FamiStudio.
// Added to Nes_Snd_Emu by @NesBleuBleu

#ifndef NES_MMC5_H
#define NES_MMC5_H

#include "Nes_Apu.h"

class Nes_Mmc5 {
public:
	Nes_Mmc5();
	~Nes_Mmc5();

	// See Nes_Apu.h for reference
	void reset();
	void volume(double);
	void treble_eq(blip_eq_t const&);
	void output(Blip_Buffer*);
	void end_frame(cpu_time_t);
	void write_register(cpu_time_t time, cpu_addr_t addr, int data);

	enum { start_addr = 0x5000 };
	enum { end_addr   = 0x5015 };

	enum { shadow_regs_count = 8 };
	void start_seeking();
	void stop_seeking(blip_time_t& clock);
	void write_shadow_register(int addr, int data);

private:
	// noncopyable
	Nes_Mmc5(const Nes_Mmc5&);
	Nes_Mmc5& operator = (const Nes_Mmc5&);

	typedef Nes_Osc    Mmc5_Osc;
	typedef Nes_Square Mmc5_Square;

	// TODO: MMC5 sample channel.
	enum { osc_count = 2 };

	Mmc5_Osc*   oscs[osc_count];
	Mmc5_Square square1;
	Mmc5_Square square2;
	Mmc5_Square::Synth square_synth; // shared by squares

	cpu_time_t last_time;
	int frame_period;
	int frame_delay; // cycles until frame counter runs next
	int frame; // current frame (0-3)
	int osc_enables;

	short shadow_regs[shadow_regs_count];

	void run_until(cpu_time_t);
};

#endif

