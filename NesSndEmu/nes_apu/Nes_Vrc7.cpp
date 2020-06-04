// VRC7 audio chip emulator for FamiStudio.
// Added to Nes_Snd_Emu by @NesBleuBleu, using the YM2413 emulator by Mitsutaka Okazaki.

#include "Nes_Vrc7.h"
#include "emu2413.h"

#include BLARGG_SOURCE_BEGIN

Nes_Vrc7::Nes_Vrc7() : opll(NULL), output_buffer(NULL)
{
	output(NULL);
	volume(1.0);
	reset();
}

Nes_Vrc7::~Nes_Vrc7()
{
	if (opll) 
		OPLL_delete(opll);
}

void Nes_Vrc7::reset()
{
	reg = 0;
	silence = false;
	reset_opll();
}

void Nes_Vrc7::volume(double v)
{
	vol = v * 8.76; 
}

void Nes_Vrc7::reset_opll()
{
	if (opll)
		OPLL_delete(opll);

	opll = OPLL_new(vrc7_clock, output_buffer ? output_buffer->sample_rate() : 44100);
	OPLL_reset(opll);
	OPLL_setChipMode(opll, 1); // VRC7 mode.
	OPLL_resetPatch(opll, OPLL_VRC7_TONE); // Use VRC7 default instruments.
	OPLL_setMask(opll, ~0x3f); // Only 6 channels.
}

void Nes_Vrc7::output(Blip_Buffer* buf)
{
	output_buffer = buf;

	if (output_buffer && (!opll || output_buffer->sample_rate() != opll->rate))
		reset_opll();
}

void Nes_Vrc7::enable_channel(int idx, bool enabled)
{
	if (opll)
	{
		if (enabled)
			OPLL_setMask(opll, opll->mask & ~(1 << idx));
		else
			OPLL_setMask(opll, opll->mask |  (1 << idx));
	}
}

void Nes_Vrc7::write_register(cpu_time_t time, cpu_addr_t addr, int data)
{
	switch (addr)
	{
	case reg_silence:
		silence = (data & 0x40) != 0;
		break;
	case reg_select:
		reg = data;
		break;
	case reg_write:
		OPLL_writeReg(opll, reg, data);
		break;
	}
}

void Nes_Vrc7::end_frame(cpu_time_t time)
{
}

void Nes_Vrc7::mix_samples(blip_sample_t* sample_buffer, long sample_cnt)
{
	if (!output_buffer || silence)
		return;

	for (int i = 0; i < sample_cnt; i++)
	{
		int sample = OPLL_calc(opll);
		sample = clamp(sample, -3200, 3600);
		sample_buffer[i] = (int16_t)clamp(sample_buffer[i] + (int)(sample * vol), -32768, 32767);
	}
}

void Nes_Vrc7::start_seeking()
{
	memset(shadow_regs, -1, sizeof(shadow_regs));
	memset(shadow_internal_regs, -1, sizeof(shadow_internal_regs));
}

void Nes_Vrc7::stop_seeking(blip_time_t& clock)
{
	if (shadow_regs[0] >= 0)
		write_register(clock += 4, reg_silence, shadow_regs[0]);

	for (int i = 0; i < array_count(shadow_internal_regs); i++)
	{
		if (shadow_internal_regs[i] >= 0)
		{
			write_register(clock += 4, reg_select, i);
			write_register(clock += 4, reg_write,  shadow_internal_regs[i]);
		}
	}
}

void Nes_Vrc7::write_shadow_register(int addr, int data)
{
	switch (addr)
	{
		case reg_silence: shadow_regs[0] = data; break;
		case reg_select:  reg = data; break;
		case reg_write:   shadow_internal_regs[reg] = data; break;
	}
}
