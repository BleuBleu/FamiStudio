
// WAVE sound file writer for recording 16-bit output during program development

// Copyright (C) 2003-2004 Shay Green. MIT license.

#ifndef WAVE_WRITER_HPP
#define WAVE_WRITER_HPP

#include <stddef.h>
#include <stdio.h>

class Wave_Writer {
public:
	typedef short sample_t;
	
	// Create sound file with given sample rate (in Hz) and filename.
	// Exit program if there's an error.
	Wave_Writer( long sample_rate, char const* filename = "out.wav" );
	
	// Enable stereo output
	void stereo( int );
	
	// Append 'count' samples to file. Use every 'skip'th source sample; allows
	// one channel of stereo sample pairs to be written by specifying a skip of 2.
	void write( const sample_t*, long count, int skip = 1 );
	
	// Number of samples written so far
	long sample_count() const;
	
	// Write sound file header and close file. If no samples were written,
	// delete file.
	~Wave_Writer();
	
	
// End of public interface
private:
	enum { buf_size = 32768 * 2 };
	unsigned char* buf;
	FILE*   file;
	long    sample_count_;
	long    rate;
	long    buf_pos;
	int     chan_count;
	
	void flush();
};

inline void Wave_Writer::stereo( int s ) {
	chan_count = s ? 2 : 1;
}

inline long Wave_Writer::sample_count() const {
	return sample_count_;
}

#endif

