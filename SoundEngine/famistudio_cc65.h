

#ifndef FAMISTUDIO_H
#define FAMISTUDIO_H

// Workaround for code analyzers that don't know about fastcall
#ifndef __fastcall__
#define __fastcall__ 
#endif

/**
 * =====================================================================================================================
 * famistudio_init (public)
 *
 * Reset APU, initialize the sound engine with some music data.
 *
 * [in] platform : Playback platform, zero for PAL, non-zero for NTSC.
 * [in] music_data : Pointer to music data 
 * ======================================================================================================================
 */
#define FAMISTUDIO_PLATFORM_PAL 0
#define FAMISTUDIO_PLATFORM_NTSC 1

void __fastcall__ famistudio_init(unsigned char platform, void* music_data);

/**
 * ======================================================================================================================
 * famistudio_music_play (public)
 * 
 * Plays a song from the loaded music data (from a previous call to famistudio_init).
 * 
 * [in] song_index : Song index.
 * ======================================================================================================================
 */

void __fastcall__ famistudio_music_play(unsigned char song_index);

/**
 * ======================================================================================================================
 * famistudio_music_pause (public)
 * 
 * Pause/unpause the currently playing song. Note that this will not update the APU, so sound might linger. Calling
 * famistudio_update after this will update the APU.
 * 
 * [in] mode : zero to play, non-zero to pause.
 * ======================================================================================================================
 */
#define FAMISTUDIO_PLAY 0
#define FAMISTUDIO_PAUSE 1

void __fastcall__ famistudio_music_pause(unsigned char mode);

/**
 * ======================================================================================================================
 * famistudio_music_stop (public)
 * 
 * Stops any music currently playing, if any. Note that this will not update the APU, so sound might linger. Calling
 * famistudio_update after this will update the APU.
 * 
 * [in] no input params.
 * ======================================================================================================================
 */

void __fastcall__ famistudio_music_stop(void);

/**
 * ======================================================================================================================
 * famistudio_update (public)
 * 
 * Main update function, should be called once per frame, ideally at the end of NMI. Will update the tempo, advance
 * the song if needed, update instrument and apply any change to the APU registers.
 * 
 * [in] no input params.
 * ======================================================================================================================
 */

void __fastcall__ famistudio_update(void);

#ifdef FAMISTUDIO_CFG_SFX_SUPPORT

/**
 * ======================================================================================================================
 * famistudio_sfx_init(public)
 * 
 * Initialize the sound effect player.
 * 
 * [in] sfx_data: Sound effect data pointer
 * ======================================================================================================================
 */

void __fastcall__ famistudio_sfx_init(void* sfx_data);


/**
 * ======================================================================================================================
 * famistudio_sfx_play (public)
 * 
 * Plays a sound effect.
 * 
 * [in] sfx_index: Sound effect index (0...127)
 * [in] channel: Offset of sound effect channel, should be FAMISTUDIO_SFX_CH0..FAMISTUDIO_SFX_CH3
 * ======================================================================================================================
 */

// #define FAMISTUDIO_SFX_STRUCT_SIZE = 15
#define FAMISTUDIO_SFX_CH0 0
#define FAMISTUDIO_SFX_CH1 15 // 1 * FAMISTUDIO_SFX_STRUCT_SIZE
#define FAMISTUDIO_SFX_CH2 30 // 2 * FAMISTUDIO_SFX_STRUCT_SIZE
#define FAMISTUDIO_SFX_CH3 45 // 3 * FAMISTUDIO_SFX_STRUCT_SIZE

void __fastcall__ famistudio_sfx_play(unsigned char sfx_index, unsigned char channel);

/**
 * ======================================================================================================================
 * famistudio_sfx_sample_play (public)
 * 
 * Play DPCM sample with higher priority, for sound effects
 * 
 * [in] index: Sample index, 1...63.
 * ======================================================================================================================
 */

void __fastcall__ famistudio_sfx_sample_play(unsigned char sample_index);

/**
 * ======================================================================================================================
 * famistudio_sfx_stop_all (public)
 *
 * Stop all currently playing sound effects.
 *
 * [in] no input params.
 * ======================================================================================================================
 */

void __fastcall__ famistudio_sfx_stop_all(void);

#endif
#endif
