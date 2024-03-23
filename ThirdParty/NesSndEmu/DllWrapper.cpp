#include "Simple_Apu.h"

#if defined(LINUX) || defined(__clang__)
#define __stdcall
#define __cdecl
#endif

// Must match NesApu.cs.
#define NUM_WAV_EXPORT_APU 8 

// 0  = Song player
// 1  = Instrument player
// 2+ = WAV/Video export, one for each potential thread.
static Simple_Apu apu[2 + NUM_WAV_EXPORT_APU];

extern "C" int __stdcall NesApuInit(int apuIdx, int sampleRate, int bass_freq, int pal, int seperate_tnd, int expansions, int (__cdecl *dmcReadFunc)(void* user_data, cpu_addr_t))
{
	if (apu[apuIdx].sample_rate(sampleRate, pal, seperate_tnd))
		return -1;

	apu[apuIdx].set_audio_expansions(expansions);
	apu[apuIdx].dmc_reader(dmcReadFunc, (void*)apuIdx);
	apu[apuIdx].bass_freq(bass_freq);

	return 0;
}

extern "C" void __stdcall NesApuWriteRegister(int apuIdx, unsigned int addr, int data)
{
	apu[apuIdx].write_register(addr, data);
}

extern "C" int __stdcall NesApuSamplesAvailable(int apuIdx)
{
	return apu[apuIdx].samples_avail();
}

extern "C" int __stdcall NesApuReadSamples(int apuIdx, blip_sample_t* buffer, int bufferSize)
{
	return apu[apuIdx].read_samples(buffer, bufferSize);
}

extern "C" void __stdcall NesApuRemoveSamples(int apuIdx, int count)
{
	return apu[apuIdx].remove_samples(count);
}

extern "C" int __stdcall NesApuReadStatus(int apuIdx)
{
	return apu[apuIdx].read_status();
}

extern "C" void __stdcall NesApuEndFrame(int apuIdx)
{
	apu[apuIdx].end_frame();
}

extern "C" void __stdcall NesApuReset(int apuIdx)
{
	apu[apuIdx].reset();
}

extern "C" void __stdcall NesApuEnableChannel(int apuIdx, int exp, int idx, int enable)
{
	apu[apuIdx].enable_channel(exp, idx, enable != 0);
}

extern "C" void __stdcall NesApuStartSeeking(int apuIdx)
{
	apu[apuIdx].start_seeking();
}

extern "C" void __stdcall NesApuStopSeeking(int apuIdx)
{
	apu[apuIdx].stop_seeking();
}

extern "C" int __stdcall NesApuIsSeeking(int apuIdx)
{
	return apu[apuIdx].is_seeking();
}

extern "C" void __stdcall NesApuTrebleEq(int apuIdx, int expansion, double treble_amount, int treble_freq, int sample_rate)
{
	apu[apuIdx].treble_eq(expansion, treble_amount, treble_freq, sample_rate);
}

extern "C" int __stdcall NesApuGetAudioExpansions(int apuIdx)
{
	return apu[apuIdx].get_audio_expansions();
}

extern "C" void __stdcall NesApuSetExpansionVolume(int apuIdx, int expansion, double volume)
{
	apu[apuIdx].set_expansion_volume(expansion, volume);
}

extern "C" int __stdcall NesApuSkipCycles(int apuIdx, int cycles)
{
	return apu[apuIdx].skip_cycles(cycles);
}

extern "C" void __stdcall NesApuGetRegisterValues(int apuIdx, int exp, void* regs)
{
	apu[apuIdx].get_register_values(exp, regs);
}

extern "C" int __stdcall NesApuGetN163WavePos(int apuIdx, int n163ChanIndex)
{
	return apu[apuIdx].get_namco_wave_pos(n163ChanIndex);
}

extern "C" int __stdcall NesApuGetFdsWavePos(int apuIdx)
{
	return apu[apuIdx].get_fds_wave_pos();
}

extern "C" void __stdcall NesApuResetTriggers(int apuIdx)
{
	return apu[apuIdx].reset_triggers();
}

extern "C" int __stdcall NesApuGetChannelTrigger(int apuIdx, int exp, int idx)
{
	return apu[apuIdx].get_channel_trigger(exp, idx);
}

extern "C" void __stdcall NesApuSetN163Mix(int apuIdx, int mix)
{
	return apu[apuIdx].set_namco_mix(mix);
}
