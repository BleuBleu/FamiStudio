// Famicom Disk System audio chip emulator for FamiStudio.
// Added to Nes_Snd_Emu by @NesBleuBleu, mostly adapted from Disch / NotSoFatso

#include "Nes_Fds.h"
#include <string.h>

#include BLARGG_SOURCE_BEGIN

Nes_Fds::Nes_Fds()
{
	output(NULL);
	volume(1.0);
	reset();
}

Nes_Fds::~Nes_Fds()
{
}

void Nes_Fds::reset()
{
	last_time = 0;
	memset(&osc.wave, 0, sizeof(osc.wave));
	memset(&osc.modt, 0, sizeof(osc.modt));
	memset(&osc.regs, 0, sizeof(osc.regs));
	osc.mod_count = 0;
	osc.mod_phase = 0;
	osc.wav_count = 0;
	osc.sweep_bias = 0;
	osc.delay = 0;
	osc.last_amp = 0;
	osc.phase = 0;
	osc.volume_env = 0x20;
	osc.volume = 0x20;
	osc.regs[10] = 0xff;
}

void Nes_Fds::volume(double v)
{
	// TODO: Review this. Seems kind-of right?
	synth.volume(v * 0.1f);
}

void Nes_Fds::treble_eq(blip_eq_t const& eq)
{
	synth.treble_eq(eq);
}

void Nes_Fds::output(Blip_Buffer* buf)
{
	osc.output = buf;
}

void Nes_Fds::write_register(cpu_time_t time, cpu_addr_t addr, int data)
{
	require(addr >= wave_addr && addr < (regs_addr + regs_count));
	require((unsigned)data <= 0xff);

	run_until(time);

	if (addr >= wave_addr && addr < (wave_addr + wave_count))
	{
		if (osc.regs[9] & 0x80)
			osc.wave[addr - wave_addr] = (data & 0x3f) - 0x20;
	}
	else
	{
		cpu_addr_t reg = addr - regs_addr;
		
		switch (reg)
		{
		case 0:
			// TODO: Volume envelope support.
			require(data & 0x80);
			osc.volume_env = data & 0x3f;
			if (osc.phase == 0) 
				osc.volume = min(osc.volume_env, 0x20); 
			break;
		case 4:
			// TODO: Sweep envelope support.
			require(data & 0x80);
			break;
		case 8:
			// TODO: Ring buffer? I cant imagine that's of the hardware does it.
			memcpy(&osc.modt[0], &osc.modt[2], modt_count - 2); 
			osc.modt[modt_count - 2] = data;
			osc.modt[modt_count - 1] = data;
			break;
		case 9:
			// Master volume.
			if ((osc.regs[reg] & 0x03) != (data & 0x03))
			{
				switch (data & 0x03)
				{
					case 0: synth.volume(1.0f / 1.0f); break;
					case 1: synth.volume(2.0f / 3.0f); break;
					case 2: synth.volume(2.0f / 4.0f); break;
					case 3: synth.volume(2.0f / 5.0f); break;
				}
			}
			break;
		}

		osc.regs[reg] = data;
	}
}

void Nes_Fds::end_frame(cpu_time_t time)
{
	if (time > last_time)
		run_until(time);
	last_time -= time;
	assert(last_time >= 0);
}

void Nes_Fds::run_until(cpu_time_t time)
{
	require(time >= last_time);
	run_fds(time);
	last_time = time;
}

#include BLARGG_ENABLE_OPTIMIZER

void Nes_Fds::run_fds(cpu_time_t end_time)
{
	require(end_time >= last_time);

	if (!osc.output)
		return;

	cpu_time_t time = last_time;

	time += osc.delay;
	osc.delay = 0;
	int last_amp = osc.last_amp;

	while (time < end_time)
	{
		int amp = 0;
		int sub_step = end_time - time;

		// Code here is mostly adapted from Disch / NotSoFatso
		bool mod_on = osc.mod_period() && !(osc.regs[7] & 0x80);
		bool wav_on = osc.wav_period() && !(osc.regs[3] & 0x80) && !(osc.regs[9] & 0x80);

		if (mod_on) sub_step = (int)min(sub_step, (osc.mod_count + 1));
		if (wav_on) sub_step = (int)min(sub_step, (osc.wav_count + 1));

		// Modulation
		int sub_freq = 0;
		if (mod_on)
		{
			osc.mod_count -= sub_step;
			if (osc.mod_count <= 0)
			{
				const int modulation_table[8] = { 0,1,2,4,0,-4,-2,-1 };

				osc.mod_count += 65536.0f / osc.mod_period();
				osc.sweep_bias = osc.modt[osc.mod_phase] == 4 ? 0 : osc.sweep_bias + modulation_table[osc.modt[osc.mod_phase]];
				osc.mod_phase  = (osc.mod_phase + 1) & 0x3f;
			}

			while (osc.sweep_bias >  63) osc.sweep_bias -= 128;
			while (osc.sweep_bias < -64) osc.sweep_bias += 128;

			// Mysterious modulation calulation...
			int sweep_gain = osc.regs[4] & 0x3f; // TODO: Sweep envelopes.
			
			int mod = osc.sweep_bias * sweep_gain;
			if (mod & 0x0f) 
				mod = (mod >> 4) + (osc.sweep_bias < 0 ? -1 : 2);
			else
				mod = (mod >> 4);

			if (mod >  193) mod -= 258;
			if (mod <  -64) mod += 256;

			sub_freq = (osc.wav_period() * mod) >> 6;
		}

		// Wave generation
		if (wav_on)
		{
			amp = (osc.wave[osc.phase] * osc.volume); 
			if (sub_freq + osc.wav_period() > 0)
			{
				osc.wav_count -= sub_step;
				if (osc.wav_count <= 0)
				{
					osc.wav_count += 65536.0f / (sub_freq + osc.wav_period());
					osc.phase = (osc.phase + 1) & 0x3f;
					if (osc.phase == 0)
						osc.volume = min(osc.volume_env, 0x20);
				}
			}
			else
			{
				osc.wav_count = osc.mod_count;
			}
		}

		int delta = amp - last_amp;
		if (delta)
			synth.offset(time, delta, osc.output);
		time += sub_step;
		last_amp = amp;
	}

	osc.last_amp = last_amp;
	osc.delay = time - end_time;
}

void Nes_Fds::start_seeking()
{
	shadow_modt_idx = 0;
	memset(shadow_regs, -1, sizeof(shadow_regs));
	memset(shadow_wave,  0, sizeof(shadow_wave));
	memset(shadow_modt,  0, sizeof(shadow_modt));
}

void Nes_Fds::stop_seeking(blip_time_t& clock)
{
	memcpy(osc.modt, shadow_modt, modt_count);
	memcpy(osc.wave, shadow_wave, wave_count);

	for (int i = 0; i < array_count(shadow_regs); i++)
	{
		if (shadow_regs[i] >= 0)
			write_register(clock += 4, regs_addr + i, shadow_regs[i]);
	}
}

void Nes_Fds::write_shadow_register(int addr, int data)
{
	if (addr >= wave_addr && addr < (wave_addr + wave_count))
	{
		// Ignore write enable.
		shadow_wave[addr - wave_addr] = data;
	}
	else if (addr >= regs_addr && addr < (regs_addr + regs_count))
	{
		if (addr == 0x4088)
		{
			// Assume we always write to mod table in batch of 32. This is true for FamiStudio.
			shadow_modt[shadow_modt_idx + 0] = data;
			shadow_modt[shadow_modt_idx + 1] = data;
			shadow_modt_idx = (shadow_modt_idx + 2) % modt_count;
		}
		else
		{
			shadow_regs[addr - regs_addr] = data;
		}
	}
}
