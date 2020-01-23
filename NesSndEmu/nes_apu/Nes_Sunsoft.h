// Sunsoft 5B audio chip emulator for FamiStudio.
// Added to Nes_Snd_Emu by @NesBleuBleu.

#ifndef NES_SUNSOFT_H
#define NES_SUNSOFT_H

#include "Nes_Apu.h"

class Nes_Sunsoft {
public:
	Nes_Sunsoft();
	~Nes_Sunsoft();
	
	// See Nes_Apu.h for reference
	void reset();
	void volume( double );
	void treble_eq( blip_eq_t const& );
	void output( Blip_Buffer* );
	enum { osc_count = 3 };
	void osc_output( int index, Blip_Buffer* );
	void end_frame( cpu_time_t );
	
	// Oscillator 0 write-only registers are at $9000-$9002
	// Oscillator 1 write-only registers are at $A000-$A002
	// Oscillator 2 write-only registers are at $B000-$B002
	enum { reg_count = 3 };
	enum { base_addr = 0x9000 };
	enum { addr_step = 0x1000 };
	void write_osc( cpu_time_t, int osc, int reg, int data );
	void write_register(cpu_time_t time, cpu_addr_t addr, int data);

	enum { shadow_regs_count = 9 };
	static int addr_to_shadow_reg(int addr);
	static int shadow_reg_to_addr(int idx);

private:
	// noncopyable
	Nes_Sunsoft( const Nes_Sunsoft& );
	Nes_Sunsoft& operator = ( const Nes_Sunsoft& );
	
	struct Sunsoft_Osc
	{
		BOOST::uint8_t regs [3];
		Blip_Buffer* output;
		int delay;
		int last_amp;
		int phase;
		int amp; // only used by saw
		
		int period() const
		{
			return (regs [2] & 0x0f) * 0x100L + regs [1] + 1;
		}
	};
	
	Sunsoft_Osc oscs [osc_count];
	cpu_time_t last_time;

	Blip_Synth<blip_med_quality,31> synth;
	
	void run_until( cpu_time_t );
	void run_osc( Sunsoft_Osc& osc, cpu_time_t );
};

inline void Nes_Sunsoft::osc_output( int i, Blip_Buffer* buf )
{
	assert( (unsigned) i < osc_count );
	oscs [i].output = buf;
}

inline int Nes_Sunsoft::addr_to_shadow_reg(int addr)
{
	//for (int i = 0; i < osc_count; i++)
	//{
	//	int osc_base_addr = base_addr + addr_step * i;
	//	if (addr >= osc_base_addr && addr <= osc_base_addr + reg_count)
	//		return i * reg_count + (addr - osc_base_addr);
	//}
	return -1;
}

inline int Nes_Sunsoft::shadow_reg_to_addr(int idx)
{
	//int osc_idx = idx / osc_count;
	//int reg_idx = idx % osc_count;

	//return base_addr + addr_step * osc_idx + reg_idx;
}

#endif

