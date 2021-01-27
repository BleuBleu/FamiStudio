
// Simple sound queue for synchronous sound handling in SDL

// Copyright (C) 2005 Shay Green. MIT license.

#ifndef SOUND_QUEUE_H
#define SOUND_QUEUE_H

#include "SDL.h"

// Simple SDL sound wrapper that has a synchronous interface
class Sound_Queue {
public:
	Sound_Queue();
	~Sound_Queue();
	
	// Initialize with specified sample rate and channel count.
	// Returns NULL on success, otherwise error string.
	const char* init( long sample_rate, int chan_count = 1 );
	
	// Number of samples in buffer waiting to be played
	int sample_count() const;
	
	// Write samples to buffer and block until enough space is available
	typedef short sample_t;
	void write( const sample_t*, int count );
	
private:
	enum { buf_size = 2048 };
	enum { buf_count = 3 };
	sample_t* volatile bufs;
	SDL_sem* volatile free_sem;
	int volatile read_buf;
	int write_buf;
	int write_pos;
	bool sound_open;
	
	sample_t* buf( int index );
	void fill_buffer( Uint8*, int );
	static void fill_buffer_( void*, Uint8*, int );
};

#endif

