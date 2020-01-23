// MMC5 audio chip emulator for FamiStudio.
// Added to Nes_Snd_Emu by @NesBleuBleu

#include "Nes_Mmc5.h"
#include <string.h>

#include BLARGG_SOURCE_BEGIN

Nes_Mmc5::Nes_Mmc5()
{
	square1.synth = &square_synth;
	square2.synth = &square_synth;

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
	write_register(0, 0x5015, 0x00);
	osc_enables = 0;

	for (cpu_addr_t addr = start_addr; addr <= 0x4013; addr++)
		write_register(0, addr, (addr & 3) ? 0x00 : 0x10);
}

void Nes_Mmc5::volume(double v)
{
	square_synth.volume(0.1128 * v);
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

void Nes_Mmc5::write_register(cpu_time_t time, cpu_addr_t addr, int data)
{
	require(addr > 0x20); // addr must be actual address (i.e. 0x40xx)
	require((unsigned)data <= 0xff);

	// Ignore addresses outside range
	if (addr < start_addr || end_addr < addr)
		return;

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
		osc->regs[reg] = data;
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

		// MATTT: Remove period < 8 check in run(), from the wiki:
		//        "Frequency values less than 8 do not silence the MMC5 pulse channels; 
		//         they can output ultrasonic frequencies."

		// run oscs to present
		square1.run(last_time, time);
		square2.run(last_time, time);
		last_time = time;

		if (time == end_time)
			break; // no more frames to run

		// MATTT: Review this, from the wiki 
		//		  "MMC5 does not have an equivalent frame sequencer (APU $4017); envelope and 
		//         length counter are fixed to a 240hz update rate."
		
		// take frame-specific actions
		frame_delay = frame_period;
		switch (frame++)
		{
		case 0:
		case 2:
			// clock length and sweep on frames 0 and 2
			square1.clock_length(0x20);
			square2.clock_length(0x20);
			break;

		case 1:
			// frame 1 is slightly shorter
			frame_delay -= 2;
			break;

		case 3:
			frame = 0;
			break;
		}
	}
}

