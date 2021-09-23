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
		maskFm = maskFm | (1 << (idx-3));
		else
		maskFm = maskFm & ~(1 << (idx-3));
	}
	if (idx > 8)
	{
		//std::cout << "enabled: " << enabled << " index" << (idx - 9) << std::endl;
		if (enabled)
			maskRythm = maskRythm | (1 << (idx-9));
		else
			maskRythm = maskRythm & ~(1 << (idx-9));
	}
}

void Nes_EPSM::write_register(cpu_time_t time, cpu_addr_t addr, int data)
{	if (addr >= reg_select && addr < (reg_select + reg_range)) {
		reg = data;
	}
	else if (addr >= reg_write && addr < (reg_write + reg_range)) {
			if((addr == 0xE000) && (reg < 0x10)){
			}
	}
	int mask = 0;
	switch(addr) {
		case 0xC000:
		case 0xC002:
			currentRegister = data;
			break;
		case 0xE000:
		case 0xE002:
			if (currentRegister == 0x10) {
				data = data & maskRythm;
			}
				//currentRegister = data;
			else if (currentRegister == 0x28) {
				if(!(maskFm & 0x1) && ((data & 0x7)) == 0){ mask = 1; }
				else if (!(maskFm & 0x2) && ((data & 0x7)) == 1) { mask = 1; }
				else if (!(maskFm & 0x4) && ((data & 0x7)) == 2) { mask = 1; }
				else if (!(maskFm & 0x8) && ((data & 0x7)) == 4) { mask = 1; }
				else if (!(maskFm & 0x10) && ((data & 0x7)) == 5) { mask = 1; }
				else if (!(maskFm & 0x20) && ((data & 0x7)) == 6) { mask = 1; }
				//std::cout << "fm" << std::endl;
			}
			break;
	}


	a0 = (addr & 0xF000) == 0xE000; //const uint8_t a0 = (addr & 0xF000) == 0xE000;
	a1 = !!(addr & 0xF); //const uint8_t a1 = !!(addr & 0xF);
	if (a1 == 0x0) { PSG_writeReg(psg, reg, data); }
	if (!mask) dataWrite.push(data);
	if (!mask) aWrite.push((a0 | (a1 << 1)));

}



void Nes_EPSM::end_frame(cpu_time_t time)
{
	if (!output_buffer)
		return;

	cpu_time_t t = last_time;

	while (t < time)
	{
		if (!dataWrite.empty() && !aWrite.empty() && !(t % 1))
		{
			OPN2_Write(&opn2, aWrite.front(), dataWrite.front());
			dataWrite.pop();
			aWrite.pop();
		}
		int sample = PSG_calc(psg)/2.3;
		sample = clamp(sample, -7710, 7710);
		int t2 = 0;
		int16_t samples[4];
		while (t2 < 12)
		{
			OPN2_Clock(&opn2, samples);
			sample += (samples[0] + samples[1]) * 8;
			sample += (samples[2] + samples[3]) / 2;
			t2++;
		}
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
	/*if (addr >= reg_select && addr < (reg_select + reg_range)) {

		reg = data;
	}
	else if (addr >= reg_write && addr < (reg_write + reg_range)) {

		shadow_internal_regs[reg] = data;
	}*/
}