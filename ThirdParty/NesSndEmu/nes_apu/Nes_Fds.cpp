// Famicom Disk System audio chip emulator for FamiStudio.
// Added to Nes_Snd_Emu by @NesBleuBleu, mostly adapted from Disch / NotSoFatso

#include "Nes_Fds.h"
#include <string.h>

#include BLARGG_SOURCE_BEGIN

Nes_Fds::Nes_Fds() : vol(1.0f)
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
	memset(&osc.ages, 0, sizeof(osc.ages));
	osc.mod_pos = 0;
	osc.mod_phase = 0;
	osc.mod_pos = 0;
	osc.delay = 0;
	osc.last_amp = 0;
	osc.phase = 0;
	osc.volume_env = 0x20;
	osc.regs[10] = 0xff;
	osc.trigger = trigger_hold;
}

void Nes_Fds::volume(double v)
{
	vol = v;
	synth.volume(vol * 0.195f);
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
	if (addr >= wave_addr && addr < (regs_addr + regs_count))
	{
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
				break;
			case 3:
				// Reset phase when turning off wave. See comment below.
				if (((data & 0x80) != 0))
				{
					osc.phase = 0;
				}
				break;
			case 4:
				// TODO: Sweep envelope support.
				require(data & 0x80);
				break;
			case 5:
				osc.mod_pos = data & 0x7f;
				break;
			case 7:
				if (data & 0x80) 
					osc.mod_phase = osc.mod_phase & 0x3F0000; 
				break;
			case 8:
				// TODO: Ring buffer? I cant imagine that's of the hardware does it.
				memmove(&osc.modt[0], &osc.modt[2], modt_count - 2); 
				osc.modt[modt_count - 2] = data;
				osc.modt[modt_count - 1] = data;
				break;
			}

			osc.regs[reg] = data;
			osc.ages[reg] = 0;
		}
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
	{
		osc.trigger = trigger_none;
		return;
	}

	// Code here is kind of a mix of Disch/NotSoFatso + NSFPlay.
	bool mod_on = osc.mod_period() && !(osc.regs[7] & 0x80);
	bool wav_on = osc.wav_period() && !(osc.regs[3] & 0x80) && !(osc.regs[9] & 0x80);

	cpu_time_t time = last_time;

	time += osc.delay;
	osc.delay = 0;
	int last_amp = osc.last_amp;

	while (time < end_time)
	{
		int sub_step = min(16, end_time - time);

		// Modulation
		if (mod_on)
		{
			const int modulation_table[8] = { 0,1,2,4,0,-4,-2,-1 };

			int start_pos = osc.mod_phase >> 16;
			osc.mod_phase += (sub_step * osc.mod_period());
			int end_pos = osc.mod_phase >> 16;

			osc.mod_phase = osc.mod_phase & 0x3fffff;

			for (int p = start_pos; p < end_pos; ++p)
			{
				int wv = osc.modt[p & 0x3f];
				osc.mod_pos = wv == 4 ? 0 : osc.mod_pos + modulation_table[wv];
				osc.mod_pos &= 0x7f;
			}
		}

		// Wave generation
		if (wav_on)
		{
			int mod = 0;
			int sweep_gain = osc.regs[4] & 0x3f; // TODO: Sweep envelopes.

			if (sweep_gain && mod_on)
			{
				int pos = (osc.mod_pos < 64) ? osc.mod_pos : (osc.mod_pos - 128);

				while (pos >= 64) pos -= 128;
				while (pos < -64) pos += 128;

				int temp = pos * sweep_gain;
				int rem = temp & 0xf;
				temp >>= 4;
				if ((rem > 0) && ((temp & 0x80) == 0))
					temp += pos < 0 ? -1 : 2;

				while (temp >= 192) temp -= 256;
				while (temp <  -64) temp += 256;

				temp *= osc.wav_period();
				rem = temp & 0x3f;
				temp >>= 6;
				if (rem >= 32)
					temp++;

				mod = temp;
			}

			int prev_phase = osc.phase;
			int f = osc.wav_period() + mod;
			osc.phase = osc.phase + (sub_step * f);
			osc.phase = osc.phase & 0x3fffff;

			// Wrapping around the wave is our trigger.
			if (osc.phase < prev_phase)
				update_trigger(osc.output, time, osc.trigger);
		}
		else
		{
			osc.trigger = trigger_none;
		
			// Need to test on my FDS when im back home:
			//	- Mesen keeps the phase at zero while the wave is disabled. 
			//  - NSFPlay resets the phase only when disabling it through 4083
			//  - NotSoFatso never resets the phase.
			// From the wiki : 
			//  "For the wave output, disabling the unit also resets the wave
			//   position to 0 (i.e. the $4040 value)."
			
			//osc.phase = 0;
		}
		
		int volume = min(osc.volume_env, 0x20);
		int amp = osc.wave[(osc.phase >> 16) & 0x3f] * volume;

		// Master volume.
		amp = amp * 2 / ((osc.regs[9] & 0x03) + 2);

		int delta = amp - last_amp;
		if (delta)
		{
			synth.offset(time, delta, osc.output);
			last_amp = amp;
		}

		time += sub_step;
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
		shadow_wave[addr - wave_addr] = (data & 0x3f) - 0x20;
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

void Nes_Fds::get_register_values(struct fds_register_values* regs)
{
	for (int i = 0; i < regs_count; i++)
	{
		regs->regs[i] = osc.regs[i];
		regs->ages[i] = osc.ages[i];

		osc.ages[i] = increment_saturate(osc.ages[i]);
	}

	memcpy(&regs->wave[0], &osc.wave[0], wave_count);
	memcpy(&regs->modt[0], &osc.modt[0], modt_count);
}

void Nes_Fds::reset_triggers()
{
	osc.trigger = trigger_hold;
}

int Nes_Fds::get_channel_trigger(int idx) const
{
	return osc.trigger;
}

