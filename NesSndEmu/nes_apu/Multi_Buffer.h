
// Multi-channel sound buffer interface, and basic mono and stereo buffers

// Blip_Buffer 0.3.3. Copyright (C) 2003-2005 Shay Green. GNU LGPL license.

#ifndef MULTI_BUFFER_H
#define MULTI_BUFFER_H

#include "Blip_Buffer.h"

// Multi_Buffer is an interface to one or more Blip_Buffers mapped to one or
// more channels consisting of left, center, and right buffers.
class Multi_Buffer {
public:
	Multi_Buffer( int samples_per_frame );
	virtual ~Multi_Buffer() { }
	
	// Set the number of channels available
	virtual blargg_err_t set_channel_count( int );
	
	// Get indexed channel, from 0 to channel count - 1
	struct channel_t {
		Blip_Buffer* center;
		Blip_Buffer* left;
		Blip_Buffer* right;
	};
	virtual channel_t channel( int index ) = 0;
	
	// See Blip_Buffer.h
	// to do: rename to set_sample_rate
	virtual blargg_err_t sample_rate( long rate, int msec = blip_default_length ) = 0;
	virtual void clock_rate( long ) = 0;
	virtual void bass_freq( int ) = 0;
	virtual void clear() = 0;
	long sample_rate() const;
	
	// Length of buffer, in milliseconds
	int length() const;
	
	// See Blip_Buffer.h. For optimal operation, pass false for 'added_stereo'
	// if nothing was added to the left and right buffers of any channel for
	// this time frame.
	virtual void end_frame( blip_time_t, bool added_stereo = true ) = 0;
	
	// Number of samples per output frame (1 = mono, 2 = stereo)
	int samples_per_frame() const;
	
	// See Blip_Buffer.h
	virtual long read_samples( blip_sample_t*, long ) = 0;
	virtual long samples_avail() const = 0;
	
private:
	// noncopyable
	Multi_Buffer( const Multi_Buffer& );
	Multi_Buffer& operator = ( const Multi_Buffer& );
	
	long sample_rate_;
	int length_;
	int const samples_per_frame_;
};

// Mono_Buffer uses a single buffer and outputs mono samples.
class Mono_Buffer : public Multi_Buffer {
	Blip_Buffer buf;
public:
	Mono_Buffer();
	~Mono_Buffer();
	
	// Buffer used for all channels
	Blip_Buffer* center();
	
	// See Multi_Buffer
	blargg_err_t sample_rate( long rate, int msec = blip_default_length );
	Multi_Buffer::sample_rate;
	void clock_rate( long );
	void bass_freq( int );
	void clear();
	channel_t channel( int );
	void end_frame( blip_time_t, bool unused = true );
	long samples_avail() const;
	long read_samples( blip_sample_t*, long );
};

// Stereo_Buffer uses three buffers (one for center) and outputs stereo sample pairs.
class Stereo_Buffer : public Multi_Buffer {
public:
	Stereo_Buffer();
	~Stereo_Buffer();
	
	// Buffers used for all channels
	Blip_Buffer* center();
	Blip_Buffer* left();
	Blip_Buffer* right();
	
	// See Multi_Buffer
	blargg_err_t sample_rate( long, int msec = blip_default_length );
	Multi_Buffer::sample_rate;
	void clock_rate( long );
	void bass_freq( int );
	void clear();
	channel_t channel( int index );
	void end_frame( blip_time_t, bool added_stereo = true );
	
	long samples_avail() const;
	long read_samples( blip_sample_t*, long );
	
private:
	enum { buf_count = 3 };
	Blip_Buffer bufs [buf_count];
	channel_t chan;
	bool stereo_added;
	bool was_stereo;
	
	void mix_stereo( blip_sample_t*, long );
	void mix_mono( blip_sample_t*, long );
};


// End of public interface

inline blargg_err_t Multi_Buffer::sample_rate( long rate, int msec )
{
	sample_rate_ = rate;
	length_ = msec;
	return blargg_success;
}

inline int Multi_Buffer::samples_per_frame() const { return samples_per_frame_; }

inline Blip_Buffer* Stereo_Buffer::left() { return &bufs [1]; }

inline Blip_Buffer* Stereo_Buffer::center() { return &bufs [0]; }

inline Blip_Buffer* Stereo_Buffer::right() { return &bufs [2]; }

inline long Stereo_Buffer::samples_avail() const { return bufs [0].samples_avail() * 2; }

inline Stereo_Buffer::channel_t Stereo_Buffer::channel( int index ) { return chan; }

inline long Multi_Buffer::sample_rate() const { return sample_rate_; }

inline int Multi_Buffer::length() const { return length_; }

inline Blip_Buffer* Mono_Buffer::center() { return &buf; }

inline void Mono_Buffer::clock_rate( long rate ) { buf.clock_rate( rate ); }

inline void Mono_Buffer::clear() { buf.clear(); }

inline void Mono_Buffer::bass_freq( int freq ) { buf.bass_freq( freq ); }

inline long Mono_Buffer::read_samples( blip_sample_t* p, long s ) { return buf.read_samples( p, s ); }

inline long Mono_Buffer::samples_avail() const { return buf.samples_avail(); }

#endif

