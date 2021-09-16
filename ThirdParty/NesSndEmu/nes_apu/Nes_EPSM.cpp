// EPSM 5B audio chip emulator for FamiStudio.
// Added to Nes_Snd_Emu by @NesBleuBleu.

#include "Nes_EPSM.h"
#include "emu2149.h"
#include "ym3438.h"
#include <iostream>
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
	//if (opn2)
		// destruct or handle opn2 somehow
}

void Nes_EPSM::reset()
{
	reset_psg();
	reset_opn2();
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

void Nes_EPSM::reset_opn2()
{
	//if (opn2)
		// destruct or handle opn2 somehow
	//opn2 = new ym3438_t();
	//opn2 = OPN_New();
	// construct opn2 somehow
	OPN2_Reset(&opn2);
	OPN2_SetChipType(0);
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
	if (idx < 3)
	{
		//Console.WriteLine("enable idx " + idx);
	//idx = idx - 3;
		if (psg)
		{
			if (enabled)
				PSG_setMask(psg, psg->mask & ~(1 << idx));
			else
				PSG_setMask(psg, psg->mask | (1 << idx));
		}
	}
}

void Nes_EPSM::write_register(cpu_time_t time, cpu_addr_t addr, int data)
{
	if (addr >= reg_select && addr < (reg_select + reg_range)) {
		//a0 = 0;
		//a1 = 0;
		reg = data;
		//if (addr == 0xC002) { a1 = 1; }
		//OPN2_Write(opn2, a0 | (a1 << 1), data);
	}
	else if (addr >= reg_write && addr < (reg_write + reg_range)) {
		//std::cout << "addr" << addr << std::endl;
			if((addr == 0xE000) && (reg < 0x10)){
			//	std::cout << "addr ok write still" << addr << "a1" << a1 <<  std::endl;
				//PSG_writeReg(psg, reg, data);
			//	a0 = 1;
			}
			//if ((addr == 0xE002) && (reg < 0x10)) {
			//	a0 = 1;
			//}
			//std::cout << "addr again " << addr << "a1" << a1 << std::endl;
		//OPN2_Write(opn2, a0 | (a1 << 1), data);
	}

	int t = 0;
	int sample = 0;
	while (t < 10)
	{
		int16_t samples[4];
		OPN2_Clock(&opn2, samples);
		sample += (samples[0] + samples[1]) / 2;
		sample += (samples[2] + samples[3]) / 2;
		t++;
	}
	sample = clamp(sample, -7710, 7710);
	int delta = sample - last_amp;
	if (delta)
	{
		synth.offset(t, delta, output_buffer);
		last_amp = sample;
	}

		a0 = (addr & 0xF000) == 0xE000; //const uint8_t a0 = (addr & 0xF000) == 0xE000;
		a1 = !!(addr & 0xF); //const uint8_t a1 = !!(addr & 0xF);
		//if (a0 == 0x0) { addr = 0xC000; }
		//if (a0 == 0x1) { addr = 0xE000; }
		if (a1 == 0x0) { PSG_writeReg(psg, reg, data); }
		OPN2_Write(&opn2,a0 | (a1 << 1), data);
		std::cout << "time " << time << std::endl;

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
		int sampleopn2 = 0;
		int t2 = 0;
		int16_t samples[4];
		while (t2 < 12)
		{
			OPN2_Clock(&opn2, samples);
			sampleopn2 += (samples[0] + samples[1]) / 2;
			sampleopn2 += (samples[2] + samples[3]) / 4;
			t2++;
		}

		sampleopn2 = clamp(sampleopn2, -7710, 7710);
		sample = sample + sampleopn2;
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