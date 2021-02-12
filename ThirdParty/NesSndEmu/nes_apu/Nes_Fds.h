// Famicom Disk System audio chip emulator for FamiStudio.
// Added to Nes_Snd_Emu by @NesBleuBleu

#ifndef NES_FDS_H
#define NES_FDS_H

#include "Nes_Apu.h"

class Nes_Fds {
public:
	Nes_Fds();
	~Nes_Fds();

	// See Nes_Apu.h for reference
	void reset();
	void volume(double);
	void treble_eq(blip_eq_t const&);
	void output(Blip_Buffer*);
	void end_frame(cpu_time_t);
	void write_register(cpu_time_t time, cpu_addr_t addr, int data);

	enum { shadow_regs_count = 11 };
	void start_seeking();
	void stop_seeking(blip_time_t& clock);
	void write_shadow_register(int addr, int data);

private:
	// noncopyable
	Nes_Fds(const Nes_Fds&);
	Nes_Fds& operator = (const Nes_Fds&);

	enum { wave_addr  = 0x4040 };
	enum { regs_addr  = 0x4080 };
	enum { wave_count = 64 };
	enum { modt_count = 64 };
	enum { regs_count = 11 };

	struct Fds_Osc
	{
		BOOST::int8_t  wave[wave_count];
		BOOST::uint8_t modt[modt_count];
		BOOST::uint8_t regs[regs_count];
		Blip_Buffer* output;
		unsigned int mod_pos;
		int mod_phase;
		int delay;
		int last_amp;
		int phase;
		int volume_env;

		int wav_period() const 
		{
			return (regs[3] & 0x0f) << 8 | regs[2];
		}
		int mod_period() const
		{
			return (regs[7] & 0x0f) << 8 | regs[6];
		}
	};

	double vol;
	Fds_Osc osc;
	cpu_time_t last_time;
	Blip_Synth<blip_good_quality,2016> synth;

	short shadow_regs[shadow_regs_count];
	BOOST::uint8_t shadow_wave[modt_count];
	BOOST::uint8_t shadow_modt[modt_count];
	BOOST::uint8_t shadow_modt_idx;

	void run_until(cpu_time_t);
	void run_fds(cpu_time_t end_time);
	void update_volume();
};

#endif

