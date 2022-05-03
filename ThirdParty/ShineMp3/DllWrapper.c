#include "layer3.h"
#include <stdlib.h>
#include <string.h>

#ifdef LINUX
#define __stdcall
#endif

#define MIN(a,b) ((a) < (b) ? (a) : (b)) 

int __stdcall ShineMp3Encode(int wav_rate, int wav_channels, int wav_num_samples, short* wavData, int mp3_bitrate, int mp3_data_size, unsigned char* mp3_data)
{
	if (shine_check_config(wav_rate, mp3_bitrate) < 0)
		return -1;

	shine_config_t config;
	config.wave.channels   = wav_channels;
	config.wave.samplerate = wav_rate;
	config.mpeg.emph = NONE;
	config.mpeg.copyright = 0;
	config.mpeg.original  = 0;
	config.mpeg.mode = wav_channels > 1 ? JOINT_STEREO : MONO;
	config.mpeg.bitr = mp3_bitrate;

	shine_t s = shine_initialise(&config);

	int samples_per_pass = shine_samples_per_pass(s);
	int wav_buffer_size = samples_per_pass * sizeof(short) * wav_channels;
	int mp3_buffer_pos = 0;

	short* wav_buffer = (short*)malloc(wav_buffer_size);

	for (int i = 0; i < wav_num_samples; i += samples_per_pass * wav_channels)
	{
		int batch_size = (wav_num_samples - i) * sizeof(short);

		if (batch_size >= wav_buffer_size)
		{
			memcpy(wav_buffer, &wavData[i], wav_buffer_size);
		}
		else
		{
			memcpy(wav_buffer, &wavData[i], batch_size);
			memset((unsigned char*)wav_buffer + batch_size, 0, wav_buffer_size - batch_size);
		}

		int written = 0;
		unsigned char* data = shine_encode_buffer_interleaved(s, wav_buffer, &written);

		if (mp3_buffer_pos + written > mp3_data_size)
		{
			mp3_buffer_pos = -1;
			break;
		}

		memcpy(mp3_data + mp3_buffer_pos, data, written);
		mp3_buffer_pos += written;
	}

	free(wav_buffer);

	if (mp3_buffer_pos >= 0)
	{
		int written = 0;
		unsigned char* data = shine_flush(s, &written);

		if (mp3_buffer_pos + written <= mp3_data_size)
		{
			memcpy(mp3_data + mp3_buffer_pos, data, written);
			mp3_buffer_pos += written;
		}
		else
		{
			mp3_buffer_pos = -1;
		}
	}

	shine_close(s);

	return mp3_buffer_pos;
}
