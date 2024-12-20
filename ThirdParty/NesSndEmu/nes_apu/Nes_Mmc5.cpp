// MMC5 audio chip emulator for FamiStudio.
// Added to Nes_Snd_Emu by @NesBleuBleu

#include "Nes_Mmc5.h"
#include <string.h>

#include BLARGG_SOURCE_BEGIN

Nes_Mmc5::Nes_Mmc5()
{
	square1.synth = &square_synth;
	square2.synth = &square_synth;
	square1.min_period = 0;
	square2.min_period = 0;

	oscs[0] = &square1;
	oscs[1] = &square2;

	output(NULL);
	volume(1.0);
	reset();

}

Nes_Mmc5::~Nes_Mmc5()
{
}

void Nes_Mmc5::reset()
{
	square1.reset();
	square2.reset();

	frame_period = false /*pal_mode*/ ? 8314 : 7458;
	last_time = 0;
	frame_delay = 1;
	osc_enables = 0;

	square1.regs[0] = 0x10;
	square1.regs[1] = 0x08; // MMC5 has no sweep.
	square1.regs[2] = 0x00;
	square1.regs[3] = 0x00;
	square2.regs[0] = 0x10;
	square2.regs[1] = 0x08; // MMC5 has no sweep.
	square2.regs[2] = 0x00;
	square2.regs[3] = 0x00;

	write_register(0, 0x5015, 0x00);
	reset_triggers();
}

void Nes_Mmc5::volume(double v)
{
	square_synth.volume(0.3 * v);
}

void Nes_Mmc5::treble_eq(blip_eq_t const& eq)
{
	square_synth.treble_eq(eq);
}

void Nes_Mmc5::output(Blip_Buffer* buf)
{
	oscs[0]->output = buf;
	oscs[1]->output = buf;
}

void Nes_Mmc5::osc_output(int index, Blip_Buffer* buf)
{
	oscs[index]->output = buf;
}

void Nes_Mmc5::write_register(cpu_time_t time, cpu_addr_t addr, int data)
{
	// Ignore addresses outside range
	if (addr < start_addr || end_addr < addr)
		return;

	require(addr > 0x20); // addr must be actual address (i.e. 0x40xx)
	require((unsigned)data <= 0xff);

	run_until(time);

	if (addr == 0x5015)
	{
		// Channel enables
		for (int i = osc_count; i--; )
			if (!((data >> i) & 1))
				oscs[i]->length_counter = 0;

		osc_enables = data;
	}
	else
	{
		// Write to channel
		int osc_index = (addr - start_addr) >> 2;
		Nes_Osc* osc = oscs[osc_index];

		int reg = addr & 3;

		// No sweep.
		if (reg == 1)
			return;

		osc->regs[reg] = data;
		osc->ages[reg] = 0;
		osc->reg_written[reg] = true;

		if (reg == 3)
		{
			// load length counter
			if ((osc_enables >> osc_index) & 1)
				osc->length_counter = length_table[(data >> 3) & 0x1f];

			// reset square phase
			if (osc_index < 2)
				((Nes_Square*)osc)->phase = Nes_Square::phase_range - 1;
		}
	}
}

void Nes_Mmc5::end_frame(cpu_time_t end_time)
{
	if (end_time > last_time)
		run_until(end_time);

	// make times relative to new frame
	last_time -= end_time;
	require(last_time >= 0);
}

#include BLARGG_ENABLE_OPTIMIZER

void Nes_Mmc5::run_until(cpu_time_t end_time)
{
	require(end_time >= last_time);

	if (end_time == last_time)
		return;

	while (true)
	{
		// earlier of next frame time or end time
		cpu_time_t time = last_time + frame_delay;
		if (time > end_time)
			time = end_time;
		frame_delay -= time - last_time;

		// run oscs to present
		square1.run(last_time, time);
		square2.run(last_time, time);
		last_time = time;

		if (time == end_time)
			break; // no more frames to run

		frame_delay = frame_period;

		square1.clock_length(0x20);
		square2.clock_length(0x20);
	}
}

void Nes_Mmc5::start_seeking()
{
	memset(shadow_regs, -1, sizeof(shadow_regs));
}

void Nes_Mmc5::stop_seeking(blip_time_t& clock)
{
	for (int i = 0; i < array_count(shadow_regs); i++)
	{
		if (shadow_regs[i] >= 0)
			write_register(clock += 4, start_addr + i, shadow_regs[i]);
	}
}

void Nes_Mmc5::write_shadow_register(int addr, int data)
{
	if (addr >= start_addr && addr < start_addr + shadow_regs_count)
		shadow_regs[addr - start_addr] = data;
}

void Nes_Mmc5::get_register_values(struct mmc5_register_values* regs)
{
	for (int i = 0; i < osc_count; i++)
	{
		Nes_Osc* osc = oscs[i];

		for (int j = 0; j < 4; j++)
		{
			regs->regs[i * 4 + j] = osc->regs[j];
			regs->ages[i * 4 + j] = osc->ages[j];

			osc->ages[j] = increment_saturate(osc->ages[j]);
		}
	}

	regs->regs[8] = osc_enables;
	regs->regs[8] = 0xff; // TODO : Keep track of that one too.
}

void Nes_Mmc5::reset_triggers()
{
	square1.trigger = trigger_hold;
	square2.trigger = trigger_hold;
}

int Nes_Mmc5::get_channel_trigger(int idx) const
{
	return oscs[idx]->trigger;
}

