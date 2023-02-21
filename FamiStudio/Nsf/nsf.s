
.ifdef FAMISTUDIO

    ; Enable all features.
    FAMISTUDIO_CFG_EXTERNAL            = 1
    FAMISTUDIO_CFG_SMOOTH_VIBRATO      = 1
    FAMISTUDIO_CFG_DPCM_SUPPORT        = 1
    FAMISTUDIO_USE_VOLUME_TRACK        = 1
    FAMISTUDIO_USE_VOLUME_SLIDES       = 1
    FAMISTUDIO_USE_PITCH_TRACK         = 1
    FAMISTUDIO_USE_SLIDE_NOTES         = 1
    FAMISTUDIO_USE_NOISE_SLIDE_NOTES   = 1
    FAMISTUDIO_USE_VIBRATO             = 1
    FAMISTUDIO_USE_ARPEGGIO            = 1
    FAMISTUDIO_USE_DUTYCYCLE_EFFECT    = 1
    FAMISTUDIO_USE_DELTA_COUNTER       = 1
    FAMISTUDIO_USE_RELEASE_NOTES       = 1
    FAMISTUDIO_USE_DPCM_EXTENDED_RANGE = 1
    FAMISTUDIO_USE_DPCM_BANKSWITCHING  = 1

    .define FAMISTUDIO_CA65_ZP_SEGMENT   ZEROPAGE
    .define FAMISTUDIO_CA65_RAM_SEGMENT  RAM
    .define FAMISTUDIO_CA65_CODE_SEGMENT CODE

    .ifdef FAMISTUDIO_CFG_NTSC_SUPPORT
        NSF_NTSC_SUPPORT=1
    .endif
    .ifdef FAMISTUDIO_CFG_PAL_SUPPORT
        NSF_PAL_SUPPORT=1
    .endif
    .ifdef FAMISTUDIO_USE_FAMITRACKER_TEMPO
        FAMISTUDIO_USE_FAMITRACKER_DELAYED_NOTES_OR_CUTS=1
    .endif

    .ifdef FAMISTUDIO_EXP_EPSM
        FAMISTUDIO_USE_EPSM=1
    .endif

    .ifdef FAMISTUDIO_MULTI_EXPANSION
        .include "../../SoundEngine/famistudio_multi_ca65.s"
    .else
        .include "../../SoundEngine/famistudio_ca65.s"
    .endif

.else

    .ifdef FT_NTSC_SUPPORT
        NSF_NTSC_SUPPORT=1
    .endif
    .ifdef FT_PAL_SUPPORT
        NSF_PAL_SUPPORT=1
    .endif
    .include "famitone2.s"
    
.endif

.segment "HEADER"

; NSF header Placeholder so that the debug info addresses matches.
header:   .res 128

.segment "RAM"

.if .defined(NSF_NTSC_SUPPORT) && .defined(NSF_PAL_SUPPORT)
nsf_mode: .res 1
.endif

.segment "CODE_INIT"

; [in] a = song index.
.proc nsf_init

    ; Each table entry is 4-bytes:
    ;   - start page (1-byte)
    ;   - start addr in page starting at $9000 (2-byte)
    ;   - flags (3 low bits = num dpcm pages)
    
.if .defined(NSF_NTSC_SUPPORT) && .defined(NSF_PAL_SUPPORT)
    stx nsf_mode
.endif  

    asl
    asl
    tax
    
    ldy nsf_song_table+0, x

    ; First map the full 0x9000 - 0xf000 to song data. 
.if .not(.defined(FAMISTUDIO_MULTI_EXPANSION) || .defined(FAMISTUDIO_USE_EPSM))
    ; The multi-expansion NSF driver code (and EPSM apparently) take 2 pages
    sty $5ff9
    iny
.endif    
    sty $5ffa
    iny
    sty $5ffb
    iny
    sty $5ffc
    iny
    sty $5ffd
    iny
    sty $5ffe
    iny
    sty $5fff ; Will be switch by DPCM (if any).

    ; Load song data and play
    ldy nsf_song_table+2, x ; hi-byte
    lda nsf_song_table+1, x ; lo-byte
    tax

.if .defined(NSF_NTSC_SUPPORT) && .defined(NSF_PAL_SUPPORT)
    lda nsf_mode
    eor #1
.elseif .defined(NSF_PAL_SUPPORT)
    lda #0 ; PAL
.else
    lda #1 ; NTSC
.endif

.ifdef FAMISTUDIO

    .ifdef FAMISTUDIO_MULTI_EXPANSION
        lda nsf_expansion_mask
        jsr famistudio_multi_init
    .endif

    jsr famistudio_init
    lda #0
    jsr famistudio_music_play
.else
    jsr FamiToneInit
    lda #0
    jsr FamiToneMusicPlay
.endif

    rts

.endproc

.segment "CODE_PLAY"

.proc nsf_play
.ifdef FAMISTUDIO
    jsr famistudio_update
.else
    jsr FamiToneUpdate
.endif
    rts
.endproc

.segment "SONG_DATA"

nsf_dpcm_page_start: .res 1
nsf_expansion_mask:  .res 1

; each entry in the song table is 4 bytes
;  - first page of the song (1 byte)
;  - address of the start of the song in page starting at 0x9000 (2 byte)
;  - unused (1-byte)

nsf_song_table:      .res 4

.segment "CODE"

.ifdef FAMISTUDIO
.proc famistudio_dpcm_bank_callback
    clc
    adc nsf_dpcm_page_start
    sta $5fff
    rts
.endproc
.endif
