// EPSM 5B audio chip emulator for FamiStudio.
// Added to Nes_Snd_Emu by @NesBleuBleu.

#include "Nes_EPSM.h"
#include "emu2149.h"

#include BLARGG_SOURCE_BEGIN

Nes_EPSM::Nes_EPSM() : psg(NULL), output_buffer(NULL)
{
	output(NULL);
	volume(1.0);
	reset();
}

Nes_EPSM::~Nes_EPSM()
{
	if (psg)
		PSG_delete(psg);
}

void Nes_EPSM::reset()
{
	reset_psg();
	last_time = 0;
	last_amp = 0;
}

void Nes_EPSM::volume(double v)
{
	synth.volume(v);
}

void Nes_EPSM::reset_psg()
{
	if (psg)
		PSG_delete(psg);

	psg = PSG_new(psg_clock, psg_clock / 16);
	PSG_reset(psg);
}

void Nes_EPSM::output(Blip_Buffer* buf)
{
	output_buffer = buf;

	if (output_buffer && (!psg || output_buffer->sample_rate() != psg->rate))
		reset_psg();
}

void Nes_EPSM::treble_eq(blip_eq_t const& eq)
{
	synth.treble_eq(eq);
}

void Nes_EPSM::enable_channel(int idx, bool enabled)
{
	if (psg)
	{
		if (enabled)
			PSG_setMask(psg, psg->mask & ~(1 << idx));
		else
			PSG_setMask(psg, psg->mask | (1 << idx));
	}
}

void Nes_EPSM::write_register(cpu_time_t time, cpu_addr_t addr, int data)
{
	if (addr >= reg_select && addr < (reg_select + reg_range))
		reg = data;
	else if (addr >= reg_write && addr < (reg_write + reg_range))
		PSG_writeReg(psg, reg, data);
}

void Nes_EPSM::end_frame(cpu_time_t time)
{
	if (!output_buffer)
		return;

	cpu_time_t t = last_time;

	while (t < time)
	{
		int sample = PSG_calc(psg);
		sample = clamp(sample, -7710, 7710);

		int delta = sample - last_amp;
		if (delta)
		{
			synth.offset(t, delta, output_buffer);
			last_amp = sample;
		}

		t += 16;
	}

	last_time = t - time;
}

void Nes_EPSM::start_seeking()
{
	memset(shadow_internal_regs, -1, sizeof(shadow_internal_regs));
}

void Nes_EPSM::stop_seeking(blip_time_t& clock)
{
	for (int i = 0; i < array_count(shadow_internal_regs); i++)
	{
		if (shadow_internal_regs[i] >= 0)
		{
			write_register(clock += 4, reg_select, i);
			write_register(clock += 4, reg_write, shadow_internal_regs[i]);
		}
	}
}

void Nes_EPSM::write_shadow_register(int addr, int data)
{
	if (addr >= reg_select && addr < (reg_select + reg_range))
		reg = data;
	else if (addr >= reg_write && addr < (reg_write + reg_range))
		shadow_internal_regs[reg] = data;
}
