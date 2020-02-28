// Sunsoft 5B audio chip emulator for FamiStudio.
// Added to Nes_Snd_Emu by @NesBleuBleu.

#include "Nes_Sunsoft.h"
#include "emu2149.h"

#include BLARGG_SOURCE_BEGIN

Nes_Sunsoft::Nes_Sunsoft() : psg(NULL), output_buffer(NULL)
{
	output( NULL );
	volume( 1.0 );
	reset();
}

Nes_Sunsoft::~Nes_Sunsoft()
{
	if (psg)
		PSG_delete(psg);
}

void Nes_Sunsoft::reset()
{
	reset_psg();
}

void Nes_Sunsoft::volume( double v )
{
	vol = v; // MATTT
}

void Nes_Sunsoft::reset_psg()
{
	if (psg)
		PSG_delete(psg);

	psg = PSG_new(psg_clock, output_buffer ? output_buffer->sample_rate() : 44100);
	PSG_reset(psg);
	PSG_setVolumeMode(psg, 1);
}

void Nes_Sunsoft::output( Blip_Buffer* buf )
{
	output_buffer = buf;

	if (output_buffer && (!psg || output_buffer->sample_rate() != psg->rate))
		reset_psg();
}

void Nes_Sunsoft::enable_channel(int idx, bool enabled)
{
	if (psg)
	{
		if (enabled)
			PSG_setMask(psg, psg->mask & ~(1 << idx));
		else
			PSG_setMask(psg, psg->mask |  (1 << idx));
	}
}

void Nes_Sunsoft::write_register(cpu_time_t time, cpu_addr_t addr, int data)
{
	if (addr >= reg_select && addr < (reg_select + reg_range))
		reg = data;
	else if (addr >= reg_write && addr < (reg_write + reg_range))
		PSG_writeReg(psg, reg, data);
}

void Nes_Sunsoft::end_frame(cpu_time_t time)
{
	if (!output_buffer)
		return;

	int sample_cnt = output_buffer->count_samples(time);
	require(sample_cnt < array_count(sample_buffer));

	for (int i = 0; i < sample_cnt; i++)
	{
		int sample = PSG_calc(psg);
		//sample = clamp(sample, -3200, 3600); // MATTT
		sample = clamp((int)(sample * vol), -32768, 32767);
		sample_buffer[i] = (int16_t)sample;
	}

	output_buffer->mix_samples(sample_buffer, sample_cnt);
}

void Nes_Sunsoft::start_seeking()
{
	memset(shadow_internal_regs, -1, sizeof(shadow_internal_regs));
}

void Nes_Sunsoft::stop_seeking(blip_time_t& clock)
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

void Nes_Sunsoft::write_shadow_register(int addr, int data)
{
	if (addr >= reg_select && addr < (reg_select + reg_range))
		reg = data;
	else if (addr >= reg_write && addr < (reg_write + reg_range))
		shadow_internal_regs[reg] = data;
}
