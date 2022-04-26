// EPSM 5B audio chip emulator for FamiStudio.
// Added to Nes_Snd_Emu by @NesBleuBleu.

#include "Nes_EPSM.h"
#include "emu2149.h"
#include "ym3438.h"
#include BLARGG_SOURCE_BEGIN

Nes_EPSM::Nes_EPSM() : psg(NULL), output_buffer(NULL), output_buffer_right(NULL)
{
	output(NULL,NULL);
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
	synth_right.volume(v);
}



void Nes_EPSM::reset_psg()
{
	if (psg)
		PSG_delete(psg);

	psg = PSG_new(psg_clock, (uint32_t)(psg_clock / 16.0 / (4000000.0/1789773.0)));
	PSG_reset(psg);
}

void Nes_EPSM::reset_opn2()
{
	OPN2_Reset(&opn2);
	OPN2_SetChipType(0);
}

void Nes_EPSM::output(Blip_Buffer* buf, Blip_Buffer* buf_right)
{
	output_buffer = buf;
	output_buffer_right = buf_right;

	if (output_buffer && (!psg || output_buffer->sample_rate() != psg->rate))
		reset_psg();
}

void Nes_EPSM::treble_eq(blip_eq_t const& eq)
{
	synth.treble_eq(eq);
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
			if((addr == 0x401d) && (reg < 0x10)){
			}
	}
	int mask = 0;
	switch(addr) {
		case 0x401c:
		case 0x401e:
			current_register = data;
			break;
		case 0x401d:
		case 0x401f:
			if (current_register == 0x10) {
				data = data & maskRythm;
			}
				//currentRegister = data;
			else if (current_register == 0x28) {
				if(!(mask_fm & 0x1) && ((data & 0x7)) == 0){ mask = 1; }
				else if (!(mask_fm & 0x2) && ((data & 0x7)) == 1) { mask = 1; }
				else if (!(mask_fm & 0x4) && ((data & 0x7)) == 2) { mask = 1; }
				else if (!(mask_fm & 0x8) && ((data & 0x7)) == 4) { mask = 1; }
				else if (!(mask_fm & 0x10) && ((data & 0x7)) == 5) { mask = 1; }
				else if (!(mask_fm & 0x20) && ((data & 0x7)) == 6) { mask = 1; }
				//std::cout << "fm" << std::endl;
			}
			break;
	}


	a0 = (addr & 0x000D) == 0x000D; //const uint8_t a0 = (addr & 0xF000) == 0xE000;
	a1 = !!(addr & 0x2); //const uint8_t a1 = !!(addr & 0xF);
	if (a1 == 0x0) { PSG_writeReg(psg, reg, data); }
	if (!mask) 
	{	
		queue.push(a0 | (a1 << 1), data);
	
		switch (addr) {
		case 0x401d:
			regs_a0[current_register] = data;
			ages_a0[current_register] = 0;
			break;
		case 0x401f:
			regs_a1[current_register] = data;
			ages_a1[current_register] = 0;
			break;
		}
	}
	//if (!mask) OPN2_Write(&opn2, (a0 | (a1 << 1)), data);
	//run_until(time);
}



long Nes_EPSM::run_until(cpu_time_t time)
{
	if (!output_buffer)
		return 0;

	cpu_time_t t = last_time;

	while (t < time)
	{
		if (!queue.empty() && !(t % 1))
		{
			epsm_write write = queue.pop();
			OPN2_Write(&opn2, write.addr, write.data);
		}
		int sample = (int)(PSG_calc(psg)/1.8);
		int sample_right;
		sample = clamp(sample, -7710, 7710);
		sample_right = clamp(sample, -7710, 7710);
		int t2 = 0;
		int16_t samples[4];
		while (t2 < 12)
		{
			OPN2_Clock(&opn2, samples);
			sample += (int)(samples[0] * 12);
			sample += (int)(samples[2]*1.1);
			sample_right += (int)(samples[1]) * 12;
			sample_right += (int)(samples[3] * 1.1);
			t2++;
		}
		int delta = sample - last_amp;
		int delta_right = sample_right - last_amp_right;
		if (delta)
		{
			synth.offset(t, delta, output_buffer);
			last_amp = sample;
		}
		if (delta_right)
		{
			synth_right.offset(t, delta_right, output_buffer_right);
			last_amp_right = sample_right;
		}
		t += 16;
	}

	last_time = time;
	return t;
}

void Nes_EPSM::end_frame(cpu_time_t time)
{
	if (time > last_time)
	last_time = run_until(time) - time;
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
			if(i >= 0xC0){
				write_register(clock += 4, reg_select, 0x28);
				write_register(clock += 4, reg_write, shadow_internal_regs[i]);
            }
			else{
				write_register(clock += 4, reg_select, i);
				write_register(clock += 4, reg_write, shadow_internal_regs[i]);
			}
		}
	}
	for (int i = 0; i < array_count(shadow_internal_regs2); i++)
	{
		if (shadow_internal_regs2[i] >= 0)
		{
			write_register(clock += 4, reg_select2, i);
			write_register(clock += 4, reg_write2, shadow_internal_regs2[i]);
		}
	}
}

void Nes_EPSM::write_shadow_register(int addr, int data)
{
	if (addr >= reg_select && addr < (reg_select + reg_range)) {
		reg = data;
	}
	else if (addr >= reg_write && addr < (reg_write + reg_range)) {
		
		if(reg == 0x28){
			shadow_internal_regs[0xC0+(0xF & data)] = data;
        }
		else{
			shadow_internal_regs[reg] = data;
        }
	}
	if (addr >= reg_select2 && addr < (reg_select2 + reg_range)) {

		reg = data;
	}
	else if (addr >= reg_write2 && addr < (reg_write2 + reg_range)) {

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

