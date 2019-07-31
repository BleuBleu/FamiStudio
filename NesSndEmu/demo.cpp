
// Use Simple_Apu to play random tones. Write output to sound file "out.wav".

#include "Simple_Apu.h"

#include <stdlib.h>

// Uncomment to use SDL sound
//#include "SDL.h"

const long sample_rate = 44100;
static Simple_Apu apu;

// "emulate" 1/60 second of sound
static void emulate_frame()
{
	// Decay current tone
	static int volume;
	apu.write_register( 0x4000, 0xb0 | volume );
	volume--;
	if ( volume < 0 )
	{
		volume = 15;
		
		// Start a new random tone
		apu.write_register( 0x4015, 0x01 );
		apu.write_register( 0x4002, rand() & 0xff );
		apu.write_register( 0x4003, (rand() & 3) | 0x11 );
	}
	
	// Generate 1/60 second of sound into APU's sample buffer
	apu.end_frame();
}

static int read_dmc( void*, cpu_addr_t addr )
{
	// call your memory read function here
	//return read_memory( addr );
	return 0;
}

static void init_sound();
static void play_samples( const blip_sample_t*, long count );
static void cleanup_sound();

int main( int argc, char** argv )
{
	init_sound();
	
	// Set sample rate and check for out of memory error
	if ( apu.sample_rate( sample_rate ) )
		return EXIT_FAILURE;
	
	// Set function for APU to read memory with (required for DMC samples to play properly)
	apu.dmc_reader( read_dmc, NULL );
	
	// Generate a few seconds of sound
	for ( int n = 60 * 4; n--; )
	{
		// Simulate emulation of 1/60 second frame
		emulate_frame();
		
		// Samples from the frame can now be read out of the apu, or
		// allowed to accumulate and read out later. Use samples_avail()
		// to find out how many samples are currently in the buffer.
		
		int const buf_size = 2048;
		static blip_sample_t buf [buf_size];
		
		// Play whatever samples are available
		long count = apu.read_samples( buf, buf_size );
		play_samples( buf, count );
	}
	
	cleanup_sound();
	
	return 0;
}


// Sound output handling (either to SDL or wave file)

#ifdef SDL_INIT_AUDIO

	#include "Sound_Queue.h"

	static Sound_Queue* sound_queue;
	
	static void init_sound()
	{
		if ( SDL_Init( SDL_INIT_AUDIO ) < 0 )
			exit( EXIT_FAILURE );
		
		atexit( SDL_Quit );
		
		sound_queue = new Sound_Queue;
		if ( !sound_queue )
			exit( EXIT_FAILURE );
		
		if ( sound_queue->init( sample_rate ) )
			exit( EXIT_FAILURE );
	}

	static void cleanup_sound()
	{
		delete sound_queue;
	}
	
	static void play_samples( const blip_sample_t* samples, long count )
	{
		sound_queue->write( samples, count );
	}
	
#else

	#include "Wave_Writer.hpp"
	
	static void init_sound()    { }
	static void cleanup_sound() { }

	static void play_samples( const blip_sample_t* samples, long count )
	{
		// write samples to sound file
		static Wave_Writer wave( sample_rate );
		wave.write( samples, count );
	}
	
#endif

