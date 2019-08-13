#include "Simple_Apu.h"

#include <stdlib.h>
#include <memory.h>

static Simple_Apu apu[3];

extern "C" int __stdcall NesApuInit(int apuIdx, long sampleRate, int (__cdecl *dmcReadFunc)(void* user_data, cpu_addr_t))
{
	if (apu[apuIdx].sample_rate(sampleRate))
		return -1;

	apu[apuIdx].dmc_reader(dmcReadFunc, NULL);

	return 0;
}

extern "C" void __stdcall  NesApuWriteRegister(int apuIdx, unsigned int addr, int data)
{
	apu[apuIdx].write_register(addr, data);
}

extern "C" int __stdcall  NesApuSamplesAvailable(int apuIdx)
{
	return apu[apuIdx].samples_avail();
}

extern "C" int __stdcall  NesApuReadSamples(int apuIdx, blip_sample_t* buffer, int bufferSize)
{
	return apu[apuIdx].read_samples(buffer, bufferSize);
}

extern "C" void __stdcall  NesApuRemoveSamples(int apuIdx, int count)
{
	return apu[apuIdx].remove_samples(count);
}

extern "C" int __stdcall  NesApuReadStatus(int apuIdx)
{
	return apu[apuIdx].read_status();
}

extern "C" void __stdcall  NesApuEndFrame(int apuIdx)
{
	apu[apuIdx].end_frame();
}

extern "C" void __stdcall NesApuReset(int apuIdx)
{
	apu[apuIdx].reset();
}

extern "C" void __stdcall  NesApuEnableChannel(int apuIdx, int idx, int enable)
{
	apu[apuIdx].enable_channel(idx, enable != 0);
}
