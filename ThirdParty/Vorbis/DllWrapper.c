#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <time.h>
#include <math.h>
#include <vorbis/vorbisenc.h>

#define READ 1024
signed char readbuffer[READ * 4 + 44]; /* out of the data segment, not the stack */

#ifdef LINUX
#define __stdcall
#endif

int __stdcall VorbisOggEncode(int wav_rate, int wav_channels, int wav_num_samples, short* wav_data, int ogg_bitrate, int ogg_data_size, unsigned char* ogg_data)
{
	ogg_stream_state os;
	ogg_page         og;
	ogg_packet       op;

	vorbis_info      vi;
	vorbis_comment   vc;
	vorbis_dsp_state vd;
	vorbis_block     vb;

	int eos = 0, ret;
	int read_idx = 0;
	int write_idx = 0;

	vorbis_info_init(&vi);

	//ret = vorbis_encode_init_vbr(&vi, 2, 44100, .4);
	ret = vorbis_encode_init(&vi, wav_channels, wav_rate, -1, ogg_bitrate * 1000, -1);

	if (ret) 
		return -1;

	vorbis_comment_init(&vc);
	vorbis_comment_add_tag(&vc, "ENCODER", "FamiStudio");
	vorbis_analysis_init(&vd, &vi);
	vorbis_block_init(&vd, &vb);

	ogg_stream_init(&os, 123);

	{
		ogg_packet header;
		ogg_packet header_comm;
		ogg_packet header_code;

		vorbis_analysis_headerout(&vd, &vc, &header, &header_comm, &header_code);
		ogg_stream_packetin(&os, &header);
		ogg_stream_packetin(&os, &header_comm);
		ogg_stream_packetin(&os, &header_code);

		while (!eos) 
		{
			int result = ogg_stream_flush(&os, &og);
			
			if (result == 0)
				break;

			memcpy(&ogg_data[write_idx], og.header, og.header_len);
			write_idx += og.header_len;
			memcpy(&ogg_data[write_idx], og.body, og.body_len);
			write_idx += og.body_len;
		}
	}

	while (!eos) 
	{
		long i = 0;
		float **buffer = vorbis_analysis_buffer(&vd, READ);

		if (wav_channels == 2)
		{
			for (i = 0; i < READ && read_idx < wav_num_samples; i++, read_idx += 2)
			{
				buffer[0][i] = wav_data[read_idx + 0] / 32768.f;
				buffer[1][i] = wav_data[read_idx + 1] / 32768.f;
			}
		}
		else
		{
			for (i = 0; i < READ && read_idx < wav_num_samples; i++, read_idx++)
			{
				buffer[0][i] = wav_data[read_idx] / 32768.f;
			}
		}

		vorbis_analysis_wrote(&vd, i);

		while (vorbis_analysis_blockout(&vd, &vb) == 1)
		{
			vorbis_analysis(&vb, NULL);
			vorbis_bitrate_addblock(&vb);

			while (vorbis_bitrate_flushpacket(&vd, &op)) 
			{
				ogg_stream_packetin(&os, &op);

				while (!eos) 
				{
					int result = ogg_stream_pageout(&os, &og);
					
					if (result == 0)
						break;

					memcpy(&ogg_data[write_idx], og.header, og.header_len);
					write_idx += og.header_len;
					memcpy(&ogg_data[write_idx], og.body, og.body_len);
					write_idx += og.body_len;

					if (ogg_page_eos(&og))
						eos = 1;
				}
			}
		}
	}

	ogg_stream_clear(&os);
	vorbis_block_clear(&vb);
	vorbis_dsp_clear(&vd);
	vorbis_comment_clear(&vc);
	vorbis_info_clear(&vi);

	return write_idx;
}

