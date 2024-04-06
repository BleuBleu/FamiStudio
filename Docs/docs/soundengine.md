# FamiStudio NES/Famicom Sound Engine

This section will cover the NES sound engine that comes with FamiStudio that you can use to play music/sound effects made with FamiStudio in your homebrew NES games.

## Overview

The FamiStudio sound engine is used by the NSF and ROM exporter of FamiStudio and can be used to make homebrew NES/Famicom games. It supports every feature from FamiStudio, including audio expansions, and will continue to do so in the future. Some of these features are toggeable to save CPU/memory.

The engine is essentially a heavily modified version of [FamiTone2 by Shiru](https://shiru.untergrund.net/code.shtml). A lot of his code and comments are still present, so massive thanks to him!! I am not trying to steal his work or anything, i renamed a lot of functions and variables because at some point it was becoming a mess of coding standards and getting hard to maintain.

The engine, as well as a demo project, is available for all 3 major assemblers:

* CA65 (and CC65)
* NESASM
* ASM6

## Features

The engine has a basic set of features, as well as a few extra toggeable features that you may wish to disable to same CPU/RAM. If you disable a feature, you need to make sure that you definitely are not using it in any of your songs. Using a feature in a song without enabling it in the song engine may lead to crashes (BRK) or undefined behaviors.

The basic feature set that is always available in engine is:

* Support for the first four 2A03 channels (2 squares, triangle and noise channel).
* Support for the full 96 notes (C0 to B7).
* Instruments with duty cycle, volume, pitch and arpeggio envelopes.
	* Absolute and relative pitch envelopes.
	* Looping sections in envelopes.
	* Release points for volume envelopes.
* Ability to change the speed (FamiTracker tempo mode only).
* Ability to loop over a portion of the song.
* Up to 64 instruments per export, an export can consist of as little as 1 song, or as many as 17.

Features that can be toggled on/off depending on the needs of your projects:

* Audio expansions chips (at most one can be enabled) : VRC6, VRC7, FDS, S5B and N163.
* PAL/NTSC playback support.
* DPCM sample support
* Sound effect support (with configurable number of streams)
* Blaarg Smooth Vibrato technique to eliminate "pops" on square channels
* FamiTracker/FamiStudio tempo mode.
* Release notes support
* Volume track support.
* Volume slides
* Fine pitch track support.
* Slide notes.
* Slide notes (noise channel).
* Vibrato effect.
* Arpeggios (not to be confused with arpeggio instrument envelopes which are always enabled).
* Duty cycle effect track
* Delayed notes/cuts (only when FamiTracker tempo is enabled)
* Delta counter effect track

## RAM/CODE usage

Enabling more features will make the sound engine code larger and use more RAM. The zeropage usage is 7 bytes and can be easily aliased with some of your ZP variables as they are only used as temporary variables inside the famistudio_xxx subroutines.

Here is a table to give a rough idea of the best/worst case of RAM/CODE usage. Note that each column includes all features of the columns on the left. So the rightmost column has every feature enabled. In reality, you can toggle features individually. These tables where generated with only NTSC support, DPCM support enabled and no SFX streams.

**Code size**

![](images/SoundEngineCodeTable.png#center)

**RAM usage**

![](images/SoundEngineRamTable.png#center)

## Demo

A small demo is included with the engine sound code. The demo is available for all 3 major assemblers and they will all generate binary identical ROMs.

![](images/SoundEngineDemo.png#center)

The source code for the demo is located in the \DemoSource subfolder.

* CA65: `DemoSource\demo_ca65.s`
* CC65: `DemoSource\demo_cc65.c`
* NESASM: `DemoSource\demo_nesasm.asm`
* ASM6: `DemoSource\demo_asm6.asm`

The songs used in the demo are available in the demo songs that are included with FamiStudio:

* `Silver Surfer.fms`
* `Journey To Silius.fms`
* `Shatterhand.fms`

The sound effects used in the demo are available in SFX.fms, which is in the \DemoAssets folder that comes with the sound engine.

## Integrating in your game

The sound engine is contained in a single file which can be simply included in one of your assembly file, like it is done in the demo.

* CA65: `famistudio_ca65.s`
* NESASM: `famistudio_nesasm.asm`
* ASM6: `famistudio_asm6.asm`

Another approach would be to compile the engine as a separate obj file and link it. This might require you to import the famistudio_xxx calls in other parts of your project.

All the instructions to use it in your project are included as comments at the top of these files.

For using the C bindings with CC65, you will need to include the `famistudio_cc65.h` header and also include the `famistudio_ca65.s` into your assembly startup routine. Make sure that `FAMISTUDIO_CFG_C_BINDINGS = 1` is set either as part of your external config or in the file or else the linker will be unable link object files.

## Interface

The interface is pretty much the same as FamiTone2, with a slightly different naming convention. The subroutines you can call from your game are:

* **famistudio_init**            : Initialize the engine with some music data.
* **famistudio_music_play**      : Start music playback with a specific song.
* **famistudio_music_pause**     : Pause/unpause music playback.
* **famistudio_music_stop**      : Stops music playback.
* **famistudio_sfx_init**        : Initialize SFX engine with SFX data.
* **famistudio_sfx_play**        : Play a SFX.
* **famistudio_sfx_sample_play** : Play a DPCM SFX.
* **famistudio_update**          : Updates the music/SFX engine, call once per frame, ideally from NMI.

To play a song, you will need to first call **famistudio_init** with you song data, then **famistudio_play** with your song number, then call **famistudio_update** once per frame.

## Configuration

There are 2 main ways of configuring the sound engine:

* **Internally**: which means modifying the famistudio_xxx assembly file directly. This is the simplest way for small projects.
* **Externally**: which means that all the configuration is done through defines provided from outside of the famistudio_xxx assembly file, without modifying it. This is the recommended way of using the engine when the code file is shared across multiple projects. This is how the NSF and ROM exported, as well as the demo project uses it, as they all points to the same engine file, but use different settings. To enable this mode, simply define FAMISTUDIO_CFG_EXTERNAL=1 and you will be in charge of providing all the configuration.

There are four main things to configure in the engine:

1. Segments (ZP/RAM/PRG)
2. Audio expansion
3. Global engine parameters
4. Supported features

Note that unless specified, the engine uses `if` and not `ifdef` for all boolean values so you need to define these to non-zero values. Undefined values will be assumed to be zero.

### 1. Segments Configuration

You need to tell where you want to allocate the zeropage, RAM and code. This section will be slightly different for each assembler.

#### CA65 (and CC65)

For CA65, you need to specify the name of your ZEROPAGE, RAM/BSS and CODE/PRG segments as c-style macros (`.define`) like the example below.

    .define FAMISTUDIO_CA65_ZP_SEGMENT   ZP
    .define FAMISTUDIO_CA65_RAM_SEGMENT  RAM
    .define FAMISTUDIO_CA65_CODE_SEGMENT PRG

#### NESASM

For NESASM, you may specify the .rsset location for ZP/BSS and the `.bank`/`.org` location for the code. Optionally, the `.zp`/`.bss`/`.code` directives may be emitted. Please refer to the demo or engine assembly file for an example of how to use this.

    ; Define this to emit the ".zp" directive before the zeropage variables.
    FAMISTUDIO_NESASM_ZP_SECTION   = 1

    ; Address where to allocate the zeropage variables that the engine use. 
    FAMISTUDIO_NESASM_ZP_RSSET     = $00a0

    ; Define this to emit the ".bss" directive before the RAM/BSS variables.
    FAMISTUDIO_NESASM_BSS_SECTION  = 1

    ; Address where to allocate the RAN/BSS variables that the engine use. 
    FAMISTUDIO_NESASM_BSS_RSSET    = $0400

    ; Define this to emit the ".code" directive before the code section.
    FAMISTUDIO_NESASM_CODE_SECTION = 1

    ; Define this to emit the ".bank" directive before the code section.
    FAMISTUDIO_NESASM_CODE_BANK    = 0

    ; Address where to place the engine code.
    FAMISTUDIO_NESASM_CODE_ORG     = $8000

#### ASM6

For ASM6, you simply need to specify the location at which to allocate the `ZP`/`BSS` and `CODE`.

    FAMISTUDIO_ASM6_ZP_ENUM   = $0000
    FAMISTUDIO_ASM6_BSS_ENUM  = $0200
    FAMISTUDIO_ASM6_CODE_BASE = $8000

### 2. Audio Expansions Configuration

You can only enable one audio expansion (`FAMISTUDIO_EXP_XXX`). Enabling more than one expansion will lead to undefined behavior. Memory usage goes up as more complex expansions are used. The audio expansion you choose **MUST MATCH** with the data you will load in the engine. Loading a FDS song while enabling VRC6 will lead to undefined behavior.

    ; Konami VRC6 (2 extra square + saw)
    FAMISTUDIO_EXP_VRC6          = 1 

    ; Rainbow-Net mapper (homebrew clone of VRC6)
    FAMISTUDIO_EXP_RAINBOW       = 1

    ; Konami VRC7 (6 FM channels)
    FAMISTUDIO_EXP_VRC7          = 1 

    ; Nintendo MMC5 (2 extra squares, extra DPCM not supported)
    FAMISTUDIO_EXP_MMC5          = 1 

    ; Sunsoft S5B (2 extra squares, advanced features not supported.)
    FAMISTUDIO_EXP_S5B           = 1 

    ; Famicom Disk System (extra wavetable channel)
    FAMISTUDIO_EXP_FDS           = 1 

    ; Namco 163 (between 1 and 8 extra wavetable channels) + number of channels.
    FAMISTUDIO_EXP_N163          = 1 
    FAMISTUDIO_EXP_N163_CHN_CNT  = 4

For more information on the Rainbow Mapper, [check the documentation here](https://github.com/BrokeStudio/rainbow-net)

### 3. Global Engine Configuration

These are parameters that configures the engine, but are independent of the data you will be importing, such as which platform (`PAL`/`NTSC`) you want to support playback for, whether SFX are enabled or not, etc. They all have the form `FAMISTUDIO_CFG_XXX`.

    ; One of these MUST be defined (PAL or NTSC playback). 
    ; Note that only NTSC support is supported when using any of the audio expansions.
    FAMISTUDIO_CFG_PAL_SUPPORT   = 1
    FAMISTUDIO_CFG_NTSC_SUPPORT  = 1

    ; Support for sound effects playback + number of SFX that can play at once.
    FAMISTUDIO_CFG_SFX_SUPPORT   = 1 
    FAMISTUDIO_CFG_SFX_STREAMS   = 2

    ; Blaarg's smooth vibrato technique. Eliminates phase resets ("pops") on
    ; square channels. 
    FAMISTUDIO_CFG_SMOOTH_VIBRATO = 1 

    ; Enables DPCM playback support.
    FAMISTUDIO_CFG_DPCM_SUPPORT   = 1

    ; Must be enabled if you are calling sound effects from a different 
    ; thread than the sound engine update.
    FAMISTUDIO_CFG_THREAD         = 1   

### 4. Supported Features Configuration

Every feature supported in FamiStudio is supported by this sound engine. If you know for sure that you are not using specific features in your music, you can disable them to save memory/processing time. Using a feature in your song and failing to enable it will likely lead to crashes (`BRK`), or undefined behavior. They all have the form `FAMISTUDIO_USE_XXX`.

    ; Must be enabled if the songs you will be importing have been created using FamiTracker tempo mode. 
    ; If you are using FamiStudio tempo mode, this must be undefined. You cannot mix and match tempo modes, 
    ; the engine can only run in one mode or the other. 
    ; More information at: https://famistudio.org/doc/song/#tempo-modes
    FAMISTUDIO_USE_FAMITRACKER_TEMPO = 1
    
    ; Must be enabled if the songs uses delayed notes or delayed cuts. This is obviously only available when using
    ; FamiTracker tempo mode as FamiStudio tempo mode does not need this.
    FAMISTUDIO_USE_FAMITRACKER_DELAYED_NOTES_OR_CUTS = 1

    ; Must be enabled if the songs uses release notes. 
    ; More information at: https://famistudio.org/doc/pianoroll/#release-point
    FAMISTUDIO_USE_RELEASE_NOTES = 1
    
    ; Must be enabled if any song uses the volume track. The volume track allows manipulating the volume at the track
    ; level independently from instruments.
    ; More information at: https://famistudio.org/doc/pianoroll/#editing-volume-tracks-effects
    FAMISTUDIO_USE_VOLUME_TRACK      = 1
    
    ; Must be enabled if any song uses slides on the volume track. Volume track must be enabled too.
    ; More information at: https://famistudio.org/doc/pianoroll/#editing-volume-tracks-effects
    FAMISTUDIO_USE_VOLUME_SLIDES     = 1
    
    ; Must be enabled if any song uses the pitch track. The pitch track allows manipulating the pitch at the track
    ; level independently from instruments.
    ; More information at: https://famistudio.org/doc/pianoroll/#pitch
    FAMISTUDIO_USE_PITCH_TRACK       = 1
    
    ; Must be enabled if any song uses slide notes. Slide notes allows portamento and slide effects.
    ; More information at: https://famistudio.org/doc/pianoroll/#slide-notes
    FAMISTUDIO_USE_SLIDE_NOTES       = 1

    ; Must be enabled if any song uses slide notes on the noise channel too. 
    ; More information at: https://famistudio.org/doc/pianoroll/#slide-notes
    FAMISTUDIO_USE_NOISE_SLIDE_NOTES = 1
    
    ; Must be enabled if any song uses the vibrato speed/depth effect track. 
    ; More information at: https://famistudio.org/doc/pianoroll/#vibrato-depth-speed
    FAMISTUDIO_USE_VIBRATO           = 1
    
    ; Must be enabled if any song uses arpeggios (not to be confused with instrument arpeggio envelopes, those 
    ; are always supported).
    ; More information at: (TODO)
    FAMISTUDIO_USE_ARPEGGIO          = 1
    
    ; Must be enabled if any song uses the "Duty Cycle" effect (equivalent of FamiTracker Vxx, also called "Timbre").  
    FAMISTUDIO_USE_DUTYCYCLE_EFFECT  = 1

    ; Must be enabled if any song uses the DPCM delta counter. Only makes sense if DPCM samples
    ; are enabled (FAMISTUDIO_CFG_DPCM_SUPPORT).
    ; More information at: (TODO)
    ; FAMISTUDIO_USE_DELTA_COUNTER     = 1

## Exporting Music/SFX to the engine

You can export music or sound effect data to the engine by using either the [Export Dialog](export.md) or from the [command line](cmdline.md).

## Issues with ASM6

There is a bug in ASM6 that makes it use the wrong macro values in some rare situations. The FamiStudio sound engine is sometimes affected by this. 

The bug is in the way ASM6 expands macros. You can fix it yourself in the original ASM6 code, or download the fixed executable from the [FamiStudio Github](https://github.com/BleuBleu/FamiStudio/blob/master/Tools/asm6_fixed.exe).

Code fix if you want to compile it yourself:

    void equ(label *id, char **next) {
        char str[LINEMAX];
        char *s=*next;
        if(!labelhere)
            errmsg=NeedName;//EQU without a name
        else {
            if((*labelhere).type==LABEL) {//new EQU.. good
                reverse(str,s+strspn(s,whitesp));       //eat whitesp off both ends
                reverse(s,str+strspn(str,whitesp));
                if(*s) {
                    (*labelhere).line=my_strdup(s);
                    (*labelhere).type=EQUATE;
                } else {
                    errmsg=IncompleteExp;
                }
            } else if((*labelhere).type!=EQUATE) {
                errmsg=LabelDefined;
            } else {
                (*labelhere).line = my_strdup(s); // ***** MISSING ASSIGNMENT HERE *****
            }
            *s=0;//end line
        }
    }