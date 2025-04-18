
#define FAMISTUDIO_CFG_SFX_SUPPORT 1

#include "../famistudio_cc65.h"


/* Masks for joy_read */
#define JOY_UP_MASK     0x10
#define JOY_DOWN_MASK   0x20
#define JOY_LEFT_MASK   0x40
#define JOY_RIGHT_MASK  0x80
#define JOY_BTN_1_MASK  0x01
#define JOY_BTN_2_MASK  0x02
#define JOY_BTN_3_MASK  0x04
#define JOY_BTN_4_MASK  0x08

#define JOY_BTN_A_MASK  JOY_BTN_1_MASK
#define JOY_BTN_B_MASK  JOY_BTN_2_MASK
#define JOY_SELECT_MASK JOY_BTN_3_MASK
#define JOY_START_MASK  JOY_BTN_4_MASK

/**
 * All drawing and NMI code is handled in the ASM source
 * 
 * Below are functions that will be called from the demo_ca65.s application.
 */

#define SILVER_SURFER    0
#define JOURNEY_TO_SILAS 1
#define SHATTERHAND      2
#define NUM_SONGS        2

// data array for each of the exported songs
extern unsigned char music_data_journey_to_silius[];
extern unsigned char music_data_silver_surfer_c_stephen_ruddy[];
extern unsigned char music_data_shatterhand[];

// data array for the exported sound effects
extern unsigned char sounds[];

// title information thats stored in the demo_ca65.s file
extern unsigned char song_title_silver_surfer[];
extern unsigned char song_title_jts[];
extern unsigned char song_title_shatterhand[];

extern volatile unsigned char gamepad_pressed;
extern volatile void* p0;
#pragma zpsym("gamepad_pressed");
#pragma zpsym("p0");

/**
 * ASM helper that draws the text in p0 to the screen
 */
extern void __fastcall__ update_title();

/**
 * Loads the song and starts audio playback.
 * 
 * This will be called on startup to play the first song.
 */
void __fastcall__ play_song(unsigned char song_index) {
    static unsigned char* song_address;

    switch (song_index) {
    case SILVER_SURFER:
        song_address = music_data_silver_surfer_c_stephen_ruddy;
        p0 = &song_title_silver_surfer;
        break;
    case JOURNEY_TO_SILAS:
        song_address = music_data_journey_to_silius;
        p0 = &song_title_jts;
        break;
    case SHATTERHAND:
        song_address = music_data_shatterhand;
        p0 = &song_title_shatterhand;
        break;
    }

    // Here since both of our songs came from different FamiStudio projects, 
    // they are actually 3 different song data, with a single song in each.
    // For a real game, if would be preferable to export all songs together
    // so that instruments shared across multiple songs are only exported once.
    famistudio_init(FAMISTUDIO_PLATFORM_NTSC, song_address);
    famistudio_music_play(0);
    update_title();
}

/**
 * Called once during the first load.
 * 
 * Normally one would do famistudio_init here as well, but currently each of the
 * songs are in different exported files, so they need to be initialized when
 * switching tracks.
 */
void __fastcall__ init() {
    famistudio_sfx_init(sounds);
}

/**
 * Called once a frame by the main loop in demo_ca65.s
 * 
 * Ideally this would be in the NMI handler
 */
void __fastcall__ update() {
    static unsigned char pause_flag = 0;
    static unsigned char song_index = 0;

    if (gamepad_pressed & JOY_RIGHT_MASK && song_index < NUM_SONGS) {
        play_song(++song_index);

    } else if (gamepad_pressed & JOY_LEFT_MASK && song_index > 0) {
        play_song(--song_index);

    } else if (gamepad_pressed & JOY_SELECT_MASK && song_index == JOURNEY_TO_SILAS) {
        // Undocumented: selects plays a SFX sample when journey to silius is loaded.
        famistudio_sfx_sample_play(21);

    } else if (gamepad_pressed & JOY_START_MASK) {
        pause_flag ^= 1;
        famistudio_music_pause(pause_flag);

    } else if (gamepad_pressed & JOY_BTN_A_MASK) {
        famistudio_sfx_play(0, FAMISTUDIO_SFX_CH0);

    } else if (gamepad_pressed & JOY_BTN_B_MASK) {
        famistudio_sfx_play(1, FAMISTUDIO_SFX_CH1);

    }

    famistudio_update();
}

