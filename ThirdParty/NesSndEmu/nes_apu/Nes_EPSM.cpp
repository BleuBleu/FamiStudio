// EPSM 5B audio chip emulator for FamiStudio.
// Added to Nes_Snd_Emu by @NesBleuBleu.

#include "Nes_EPSM.h"
#include "emu2149.h"
#include "ym3438.h"
#include BLARGG_SOURCE_BEGIN

Nes_EPSM::Nes_EPSM() : psg(NULL), output_buffer_left(NULL), output_buffer_right(NULL)
{
	output(NULL,NULL);
	volume(1.0);
	reset(false);
}

Nes_EPSM::~Nes_EPSM()
{
	if (psg)
		PSG_delete(psg);
	//if (opn2)
		// destruct or handle opn2 somehow
}

void Nes_EPSM::reset(bool pal = false)
{
	pal_mode = pal;
	reset_psg();
	reset_opn2();
	last_time = 0;
	last_psg_amp = 0;
	sample_left = 0;
	sample_right = 0;
	last_opn2_amp_left = 0;
	last_opn2_amp_right = 0;
	psg_delay = 0;
	opn2_delay = 0;
}

void Nes_EPSM::volume(double v)
{
	synth_left.volume(v * 10.59857f);
	synth_right.volume(v * 10.59857f);
}

void Nes_EPSM::reset_psg()
{
	if (psg)
		PSG_delete(psg);

	psg = PSG_new(psg_clock, (uint32_t)((pal_mode ? pal_clock : ntsc_clock) / 16));
	PSG_reset(psg);
}

void Nes_EPSM::reset_opn2()
{
	OPN2_Reset(&opn2);
	OPN2_SetChipType(0);
}

void Nes_EPSM::output(Blip_Buffer* buf, Blip_Buffer* buf_right)
{
	output_buffer_left = buf;
	output_buffer_right = buf_right;

	if (output_buffer_left && (!psg || output_buffer_left->sample_rate() != psg->rate))
		reset_psg();
}

void Nes_EPSM::treble_eq(blip_eq_t const& eq)
{
	synth_left.treble_eq(eq);
	synth_right.treble_eq(eq);
}

void Nes_EPSM::enable_channel(int idx, bool enabled)
{
	if (idx < 3)
	{
		if (psg)
		{
			if (enabled)
				PSG_setMask(psg, psg->mask & ~(1 << idx));
			else
				PSG_setMask(psg, psg->mask | (1 << idx));
		}
	}

	if (idx < 9 && idx > 2)
	{
		if (enabled)
			mask_fm = mask_fm | (1 << (idx-3));
		else
			mask_fm = mask_fm & ~(1 << (idx-3));
	}

	if (idx > 8)
	{
		if (enabled)
			mask_rhythm = mask_rhythm | (1 << (idx-9));
		else
			mask_rhythm = mask_rhythm & ~(1 << (idx-9));
	}

	if (idx > 2)
	{
		if (enabled)
		{
			opn2_mask = opn2_mask & ~(1 << (idx-3));
			OPN2_MuteChannel(&opn2, opn2_mask);
		}
		else
		{
			opn2_mask = opn2_mask | (1 << (idx-3));
			OPN2_MuteChannel(&opn2, opn2_mask);
		}
	}
}

void Nes_EPSM::write_register(cpu_time_t time, cpu_addr_t addr, int data)
{
	switch (addr)
	{
	case reg_select:
	case reg_write:
	case reg_select2:
	case reg_write2:
		bool psg_reg = false;
		switch (addr)
		{
		case reg_select:
			reg = data;
		case reg_select2:
			current_register = data;
			break;
		case reg_write:
			if (reg < 0x10)
			{
				PSG_writeReg(psg, reg, data);
				psg_reg = true;
			}
			regs_a0[current_register] = data;
			ages_a0[current_register] = 0;
			break;
		case reg_write2:
			regs_a1[current_register] = data;
			ages_a1[current_register] = 0;
			break;
		}

		int a0 = (addr & 0x000D) == 0x000D; //const uint8_t a0 = (addr & 0xF000) == 0xE000;
		int a1 = !!(addr & 0x2); //const uint8_t a1 = !!(addr & 0xF);

		if (!psg_reg)
			OPN2_Write(&opn2, (a0 | (a1 << 1)), data);

		run_until(time);
		break;
	}
}

long Nes_EPSM::run_until(cpu_time_t end_time)
{
	if (!output_buffer_left)
		return 0;

	end_time <<= epsm_time_precision;

	require(end_time >= last_time);

	cpu_time_t psg_increment = 16 << epsm_time_precision;
	cpu_time_t psg_time = last_time + psg_delay;

	while (psg_time < end_time)
	{
		int sample = (int)PSG_calc(psg) * 10 / 18;
		int delta = sample - last_psg_amp;

		if (delta)
		{
			synth_left.offset(psg_time >> epsm_time_precision, delta, output_buffer_left);
			synth_right.offset(psg_time >> epsm_time_precision, delta, output_buffer_right);
			
			last_psg_amp = sample;
		}

		for (int i = 0; i < 3; i++)
		{
			if (psg->trigger_mask & (1 << i))
				update_trigger(output_buffer_left, psg_time >> epsm_time_precision, triggers[i]);
			else if (psg->freq[i] <= 1 || !psg->tmask[i])
				triggers[i] = trigger_none;
		}

		psg_time += psg_increment;
	}

	cpu_time_t opn2_increment = ((int64_t)(output_buffer_left->clock_rate() * 6) << epsm_time_precision) / epsm_clock;
	cpu_time_t opn2_time = last_time + opn2_delay;

	while (opn2_time < end_time)
	{
		int16_t samples[4];
		OPN2_Clock(&opn2, samples, mask_fm, mask_rhythm, false);

		sample_left  += (int)(samples[0] * 6);
		sample_left  += (int)(samples[2] * 11 / 20);
		sample_right += (int)(samples[1] * 6);
		sample_right += (int)(samples[3] * 11 / 20);

		// The chip does a full update in 24-steps. It outputs the value of 
		// certain channels at each of those 24 steps. So for maximum audio 
		// quality, we wait until the chip has done a full update (which takes 
		// ~32.2159 NES cycles in NTSC) so we get even output from all the channels.
		if (opn2.cycles == 0)
		{
			int delta_left  = sample_left  - last_opn2_amp_left;
			int delta_right = sample_right - last_opn2_amp_right;

			if (delta_left)
			{
				synth_left.offset(opn2_time >> epsm_time_precision, delta_left, output_buffer_left);
				last_opn2_amp_left = sample_left;
			}

			if (delta_right)
			{
				synth_right.offset(opn2_time >> epsm_time_precision, delta_right, output_buffer_right);
				last_opn2_amp_right = sample_right;
			}

			for (int i = 0; i < 6; i++)
			{
				if (opn2.triggers[i] == 1)
					update_trigger(output_buffer_left, opn2_time >> epsm_time_precision, triggers[i + 3]);
				else if (opn2.triggers[i] == 2)
					triggers[i + 3] = trigger_none;
			}

			sample_left  = 0;
			sample_right = 0;
		}

		opn2_time += opn2_increment;
	}

	opn2_delay = opn2_time - end_time;
	psg_delay  = psg_time  - end_time;

	last_time = end_time;

	return max(opn2_time, psg_time);
}

void Nes_EPSM::end_frame(cpu_time_t time)
{
	if ((time << epsm_time_precision) > last_time)
		run_until(time);

	last_time -= (time << epsm_time_precision);
	assert(last_time >= 0);
}

void Nes_EPSM::start_seeking()
{
	memset(shadow_internal_regs, -1, sizeof(shadow_internal_regs));
	memset(shadow_internal_regs2, -1, sizeof(shadow_internal_regs2));
}

void Nes_EPSM::stop_seeking(blip_time_t& clock)
{
	for (int i = 0; i < array_count(shadow_internal_regs); i++)
	{
		if (shadow_internal_regs[i] >= 0)
		{
			if (i >= 0xC0)
			{
				write_register(clock += reg_addr_cycle_skip, reg_select, 0x28);
				write_register(clock += reg_data_cycle_skip, reg_write, shadow_internal_regs[i]);
            }
			else
			{
				write_register(clock += reg_addr_cycle_skip, reg_select, i);
				write_register(clock += reg_data_cycle_skip, reg_write, shadow_internal_regs[i]);
			}
		}
	}

	for (int i = 0; i < array_count(shadow_internal_regs2); i++)
	{
		if (shadow_internal_regs2[i] >= 0)
		{
			write_register(clock += reg_addr_cycle_skip, reg_select2, i);
			write_register(clock += reg_data_cycle_skip, reg_write2, shadow_internal_regs2[i]);
		}
	}
}

void Nes_EPSM::write_shadow_register(int addr, int data)
{
	if (addr >= reg_select && addr < (reg_select + reg_range))
	{
		reg = data;
	}
	else if (addr >= reg_write && addr < (reg_write + reg_range))
	{
		if(reg == 0x28)
			shadow_internal_regs[0xC0+(0xF & data)] = data;
		else if(reg != 0x10)
			shadow_internal_regs[reg] = data;
	}
	if (addr >= reg_select2 && addr < (reg_select2 + reg_range)) 
	{
		reg = data;
	}
	else if (addr >= reg_write2 && addr < (reg_write2 + reg_range)) 
	{
		shadow_internal_regs2[reg] = data;
	}
}

void Nes_EPSM::get_register_values(struct epsm_register_values* r)
{
	for (int i = 0; i < 184; i++)
	{
		r->regs_a0[i] = regs_a0[i];
		r->ages_a0[i] = ages_a0[i];
		ages_a0[i] = increment_saturate(ages_a0[i]);

		r->regs_a1[i] = regs_a1[i];
		r->ages_a1[i] = ages_a1[i];
		ages_a1[i] = increment_saturate(ages_a1[i]);
	}
}

void Nes_EPSM::reset_triggers(bool force_none)
{
	for (int i = 0; i < array_count(triggers); i++)
		triggers[i] = force_none ? trigger_none : (i >= 9 ? trigger_none : trigger_hold);
}

int Nes_EPSM::get_channel_trigger(int idx) const
{
	return triggers[idx];
}
