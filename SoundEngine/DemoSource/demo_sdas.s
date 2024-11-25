
; Assembly version of the demo, matching other assemblers exactly when built.
; The C version of the demo uses GBDK's patched SDCC binaries, and has no graphics/UI.

FAMISTUDIO_VERSION_MAJOR  = 4
FAMISTUDIO_VERSION_MINOR  = 2
FAMISTUDIO_VERSION_HOTFIX = 1

.area _HEADER (ABS)
.org 0x7FF0
INES_MAPPER = 0 ; 0 = NROM
INES_MIRROR = 1 ; 0 = horizontal mirroring, 1 = vertical mirroring
INES_SRAM   = 0 ; 1 = battery backed SRAM at 0x6000-7FFF
.db 'N', 'E', 'S', 0x1A ; ID 
.db 0x02 ; 16k PRG bank count
.db 0x01 ; 8k CHR bank count
.db INES_MIRROR | (INES_SRAM << 1) | ((INES_MAPPER & 0xf) << 4)
.db (INES_MAPPER & 0b11110000)
.db 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0 ; padding

.area _ZP (PAG)
nmi_lock:           .ds 1 ; prevents NMI re-entry
nmi_count:          .ds 1 ; is incremented every NMI
nmi_ready:          .ds 1 ; set to 1 to push a PPU frame update, 2 to turn rendering off next NMI
scroll_x:           .ds 1 ; x scroll position
scroll_y:           .ds 1 ; y scroll position
scroll_nmt:         .ds 1 ; nametable select (0-3 = 0x2000,0x2400,0x2800,0x2C00)
gamepad:            .ds 1
gamepad_previous:   .ds 1
gamepad_pressed:    .ds 1
song_index:         .ds 1
pause_flag:         .ds 1
nmt_update_mode:    .ds 1   ; update "mode", 0 = nothing to do, 1 = column mode, 2 = row mode + palettes
nmt_update_data:    .ds 128 ; nametable update entry buffer for PPU update
nmt_update_len:     .ds 1 ; number of bytes in nmt_update_data buffer
palette:            .ds 32  ; palette buffer for PPU update

; General purpose temporary vars.
r0: .ds 1
r1: .ds 1
r2: .ds 1
r3: .ds 1
r4: .ds 1

; General purpose pointers.
p0: .ds 2

.area _OAM (PAG)
oam: .ds 256        ; sprite OAM data to be uploaded by DMA

; FamiStudio config.
FAMISTUDIO_CFG_EXTERNAL       = 1
FAMISTUDIO_CFG_DPCM_SUPPORT   = 1
FAMISTUDIO_CFG_SFX_SUPPORT    = 1
FAMISTUDIO_CFG_SFX_STREAMS    = 2
FAMISTUDIO_CFG_EQUALIZER      = 1
FAMISTUDIO_USE_VOLUME_TRACK   = 1
FAMISTUDIO_USE_PITCH_TRACK    = 1
FAMISTUDIO_USE_SLIDE_NOTES    = 1
FAMISTUDIO_USE_VIBRATO        = 1
FAMISTUDIO_USE_ARPEGGIO       = 1
FAMISTUDIO_CFG_SMOOTH_VIBRATO = 1
FAMISTUDIO_USE_RELEASE_NOTES  = 1
FAMISTUDIO_USE_DELTA_COUNTER  = 1
FAMISTUDIO_DPCM_OFF           = 0xe000

FAMISTUDIO_CFG_C_BINDINGS = 0

; SDAS-specific config.
.define FAMISTUDIO_SDAS_ZP_SEGMENT   "_ZP"
.define FAMISTUDIO_SDAS_RAM_SEGMENT  "_BSS"
.define FAMISTUDIO_SDAS_CODE_SEGMENT "_CODE"

.include "..\famistudio_sdas.s"

; Our single screen.
screen_data_rle:
.incbin "demo.rle"

default_palette:
.incbin "demo.pal"
.incbin "demo.pal"

; Silver Surfer - BGM 2
song_title_silver_surfer:
    .db 0xff, 0xff, 0xff, 0x12, 0x22, 0x25, 0x2f, 0x1e, 0x2b, 0xff, 0x12, 0x2e, 0x2b, 0x1f, 0x1e, 0x2b, 0xff, 0x4f, 0xff, 0x01, 0x06, 0x0c, 0xff, 0x36, 0xff, 0xff, 0xff, 0xff

; Journey To Silius - Menu
song_title_jts:
    .db 0xff, 0xff, 0x09, 0x28, 0x2e, 0x2b, 0x27, 0x1e, 0x32, 0xff, 0x13, 0x28, 0xff, 0x12, 0x22, 0x25, 0x22, 0x2e, 0x2c, 0xff, 0x4f, 0xff, 0x0c, 0x1e, 0x27, 0x2e, 0xff, 0xff

; Shatterhand - Final Area
song_title_shatterhand:
    .db 0xff, 0xff, 0x12, 0x21, 0x1a, 0x2d, 0x2d, 0x1e, 0x2b, 0x21, 0x1a, 0x27, 0x1d, 0xff, 0x4f, 0xff, 0x05, 0x22, 0x27, 0x1a, 0x25, 0xff, 0x00, 0x2b, 0x1e, 0x1a, 0xff, 0xff

NUM_SONGS = 3

_exit:
reset:

    sei       ; mask interrupts
    lda #0
    sta 0x2000 ; disable NMI
    sta 0x2001 ; disable rendering
    sta 0x4015 ; disable APU sound
    sta 0x4010 ; disable DMC IRQ
    lda #0x40
    sta 0x4017 ; disable APU IRQ
    cld       ; disable decimal mode
    ldx #0xFF
    txs       ; initialize stack
    ; wait for first vblank
    bit 0x2002
    reset_wait_vblank_loop:
        bit 0x2002
        bpl reset_wait_vblank_loop
    ; clear all RAM to 0
    lda #0
    ldx #0
    reset_clear_ram_loop:
        sta 0x0000, X
        sta 0x0100, X
        sta 0x0200, X
        sta 0x0300, X
        sta 0x0400, X
        sta 0x0500, X
        sta 0x0600, X
        sta 0x0700, X
        inx
        bne reset_clear_ram_loop
    ; place all sprites offscreen at Y=255
    lda #255
    ldx #0
    reset_clear_oam_loop:
        sta oam, X
        inx
        inx
        inx
        inx
        bne reset_clear_oam_loop
    ; wait for second vblank
    reset_wait_vblank_loop2:
        bit 0x2002
        bpl reset_wait_vblank_loop2
    ; NES is initialized, ready to begin!
    ; enable the NMI for graphical updates, and jump to our main program
    lda #0b10001000
    sta 0x2000
    jmp main

nmi:
    ; save registers
    pha
    txa
    pha
    tya
    pha
    ; prevent NMI re-entry
    lda *nmi_lock
    beq nmi_lock_nmi
    jmp nmi_end
nmi_lock_nmi:
    lda #1
    sta *nmi_lock
    inc *nmi_count
    lda *nmi_ready
    bne nmi_check_rendering_off ; nmi_ready == 0 not ready to update PPU
    jmp nmi_ppu_update_end
nmi_check_rendering_off:
    cmp #2 ; nmi_ready == 2 turns rendering off
    bne nmi_oam_dma
        lda #0b00000000
        sta 0x2001
        ldx #0
        stx *nmi_ready
        jmp nmi_ppu_update_end
nmi_oam_dma:
    ; sprite OAM DMA
    ldx #0
    stx 0x2003
    lda #>oam
    sta 0x4014

; nametable update
nmi_nmt_update:
    lda *nmt_update_mode 
    bne nmi_do_update
    jmp nmi_update_done
    nmi_do_update:
        ldx #0
        cpx *nmt_update_len
        beq nmi_palettes
        asl
        asl
        ora #0b10000000
        sta 0x2000
        ldx #0
        nmi_nmt_update_loop:
            lda *nmt_update_data, x
            inx
            sta 0x2006
            lda *nmt_update_data, x
            inx
            sta 0x2006
            ldy *nmt_update_data, x
            inx
            nmi_col_loop:
                lda *nmt_update_data, x
                inx
                sta 0x2007
                dey
                bne nmi_col_loop
            cpx *nmt_update_len
            bcc nmi_nmt_update_loop

; palettes
nmi_palettes:
    lda *nmt_update_mode
    cmp #2
    beq nmi_palettes_need_update
    jmp nmi_update_done
    nmi_palettes_need_update:
        lda 0x2002
        lda #0x3F
        sta 0x2006
        ldx #0
        stx 0x2006 ; set PPU address to 0x3F00
        lda *palette+0
        sta 0x2007
        lda *palette+1
        sta 0x2007
        lda *palette+2
        sta 0x2007
        lda *palette+3
        sta 0x2007
        lda *palette+4
        sta 0x2007
        lda *palette+5
        sta 0x2007
        lda *palette+6
        sta 0x2007
        lda *palette+7
        sta 0x2007
        lda *palette+8
        sta 0x2007
        lda *palette+9
        sta 0x2007
        lda *palette+10
        sta 0x2007
        lda *palette+11
        sta 0x2007
        lda *palette+12
        sta 0x2007
        lda *palette+13
        sta 0x2007
        lda *palette+14
        sta 0x2007
        lda *palette+15
        sta 0x2007
        lda *palette+16
        sta 0x2007
        lda *palette+17
        sta 0x2007
        lda *palette+18
        sta 0x2007
        lda *palette+19
        sta 0x2007
        lda *palette+20
        sta 0x2007
        lda *palette+21
        sta 0x2007
        lda *palette+22
        sta 0x2007
        lda *palette+23
        sta 0x2007
        lda *palette+24
        sta 0x2007
        lda *palette+25
        sta 0x2007
        lda *palette+26
        sta 0x2007
        lda *palette+27
        sta 0x2007
        lda *palette+28
        sta 0x2007
        lda *palette+29
        sta 0x2007
        lda *palette+30
        sta 0x2007
        lda *palette+31
        sta 0x2007

nmi_update_done:
    ; Clear update mode.
    lda #0 
    sta *nmt_update_mode
    sta *nmt_update_len

nmi_scroll:
    lda *scroll_nmt
    and #0b00000011 ; keep only lowest 2 bits to prevent error
    ora #0b10001000
    sta 0x2000
    lda *scroll_x
    sta 0x2005
    lda *scroll_y
    sta 0x2005
    ; enable rendering
    lda #0b00011110
    sta 0x2001
    ; flag PPU update complete
    ldx #0
    stx *nmi_ready
nmi_ppu_update_end:
    ; if this engine had music/sound, this would be a good place to play it
    ; unlock re-entry flag
    lda #0
    sta *nmi_lock
nmi_end:
    ; restore registers and return
    pla
    tay
    pla
    tax
    pla
    rti

irq:
    rti

; ppu_update: waits until next NMI, turns rendering on (if not already), uploads OAM, palette, and nametable update to PPU
ppu_update:
    lda #1
    sta *nmi_ready
    ppu_update_wait:
        lda *nmi_ready
        bne ppu_update_wait
    rts

; ppu_skip: waits until next NMI, does not update PPU
ppu_skip:
    lda *nmi_count
    ppu_skip_wait:
        cmp *nmi_count
        beq ppu_skip_wait
    rts

; ppu_off: waits until next NMI, turns rendering off (now safe to write PPU directly via 0x2007)
ppu_off:
    lda #2
    sta *nmi_ready
    ppu_off_wait:
        lda *nmi_ready
        bne ppu_off_wait
    rts

PAD_A      = 0x01
PAD_B      = 0x02
PAD_SELECT = 0x04
PAD_START  = 0x08
PAD_U      = 0x10
PAD_D      = 0x20
PAD_L      = 0x40
PAD_R      = 0x80

gamepad_poll:
    ; strobe the gamepad to latch current button state
    lda #1
    sta 0x4016
    lda #0
    sta 0x4016
    ; read 8 bytes from the interface at 0x4016
    ldx #8
    gamepad_loop:
        pha
        lda 0x4016
        ; combine low two bits and store in carry bit
        and #0b00000011
        cmp #0b00000001
        pla
        ; rotate carry into gamepad variable
        ror a
        dex
        bne gamepad_loop
    sta *gamepad
    rts

gamepad_poll_dpcm_safe:
    
    lda *gamepad
    sta *gamepad_previous
    jsr gamepad_poll
    gamepad_reread:
        lda *gamepad
        pha
        jsr gamepad_poll
        pla
        cmp *gamepad
        bne gamepad_reread

    gamepad_toggle:
    eor *gamepad_previous
    and *gamepad
    sta *gamepad_pressed

    rts

version_text: ; 
    .db 0x34 + FAMISTUDIO_VERSION_MAJOR, 0x3e, 0x34 + FAMISTUDIO_VERSION_MINOR, 0x3e, 0x34 + FAMISTUDIO_VERSION_HOTFIX

play_song:
    .define text_ptr "p0"

    lda *song_index
    cmp #1
    beq journey_to_silius
    cmp #2
    beq shatterhand

    ; Here since both of our songs came from different FamiStudio projects, 
    ; they are actually 3 different song data, with a single song in each.
    ; For a real game, if would be preferable to export all songs together
    ; so that instruments shared across multiple songs are only exported once.
    silver_surfer:
        lda #<song_title_silver_surfer
        sta *text_ptr+0
        lda #>song_title_silver_surfer
        sta *text_ptr+1
        ldx #<music_data_silver_surfer_c_stephen_ruddy
        ldy #>music_data_silver_surfer_c_stephen_ruddy
        jmp play_song_play_song

    journey_to_silius:
        lda #<song_title_jts
        sta *text_ptr+0
        lda #>song_title_jts
        sta *text_ptr+1
        ldx #<music_data_journey_to_silius
        ldy #>music_data_journey_to_silius
        jmp play_song_play_song

    shatterhand:
        lda #<song_title_shatterhand
        sta *text_ptr+0
        lda #>song_title_shatterhand
        sta *text_ptr+1
        ldx #<music_data_shatterhand
        ldy #>music_data_shatterhand
        jmp play_song_play_song
    
    play_song_play_song:
    lda #1 ; NTSC
    jsr famistudio_init
    lda #0
    jsr famistudio_music_play

    ;update title.
    ldx #2
    ldy #15
    jsr draw_text
    jsr ppu_update

    rts

equalizer_ppu_addr_lo_lookup:
    .db 0x47 ; Square 1
    .db 0x4b ; Square 2
    .db 0x4f ; Triangle
    .db 0x53 ; Noise
    .db 0x57 ; DPCM

equalizer_ppu_addr_hi_lookup:
    .db 0x22 ; Square 1
    .db 0x22 ; Square 2
    .db 0x22 ; Triangle
    .db 0x22 ; Noise
    .db 0x22 ; DPCM

; Which 4 BG tiles to draw for each "volume" [0-7]
equalizer_volume_lookup:
    .db 0xe3, 0xe3, 0xe3, 0xe3 ; 0
    .db 0xe3, 0xe3, 0xe3, 0xe0 ; 1
    .db 0xe3, 0xe3, 0xe3, 0xf0 ; 2
    .db 0xe3, 0xe3, 0xe0, 0xf0 ; 3
    .db 0xe3, 0xe3, 0xf0, 0xf0 ; 4
    .db 0xe3, 0xe0, 0xf0, 0xf0 ; 5
    .db 0xe3, 0xf0, 0xf0, 0xf0 ; 6
    .db 0xe0, 0xf0, 0xf0, 0xf0 ; 7
    .db 0xf0, 0xf0, 0xf0, 0xf0 ; 8

; To add some color variety.
equalizer_color_lookup:
    .db 0x00, 0x01, 0x02
    .db 0x00, 0x01

; x = channel to update
update_equalizer:
    
    channel_idx = r0
    color_offset = r1

    stx *channel_idx
    lda equalizer_color_lookup, x
    sta *color_offset

    ; Write 2 addresses
    ldy *nmt_update_len
    lda equalizer_ppu_addr_hi_lookup, x
    sta *nmt_update_data,y
    lda equalizer_ppu_addr_lo_lookup, x
    sta *nmt_update_data+1,y

    ; Always update 4 tiles
    lda #4
    sta *nmt_update_data+2,y

    ; Compute lookup index based on "volume".
    lda famistudio_chn_note_counter, x
    asl
    asl
    tax

    clc
    lda equalizer_volume_lookup, x
    adc *color_offset
    sta *nmt_update_data+3,y
    lda equalizer_volume_lookup+1, x
    adc *color_offset
    sta *nmt_update_data+4,y
    lda equalizer_volume_lookup+2, x
    adc *color_offset
    sta *nmt_update_data+5,y
    lda equalizer_volume_lookup+3, x
    adc *color_offset
    sta *nmt_update_data+6,y
    
    lda #7
    adc *nmt_update_len
    sta *nmt_update_len 
    ldx *channel_idx

    rts

main:

    ldx #0
    palette_loop:
        lda default_palette, x
        sta *palette, x
        sta *palette+16, x
        inx
        cpx #16
        bcc palette_loop

    ; Force palette update.
    lda #2
    sta *nmt_update_mode

    jsr setup_background
    jsr ppu_update

    lda #0 ; song zero.
    sta *song_index
    jsr play_song

    ; Load SFX
    ldx #<sounds
    ldy #>sounds
    jsr famistudio_sfx_init

main_loop:
    jsr gamepad_poll_dpcm_safe

    check_right:
        lda *gamepad_pressed
        and #PAD_R
        beq check_left

        ; dont go beyond last song.
        lda *song_index
        cmp #(NUM_SONGS - 1)
        beq draw

        ; next song.
        clc
        adc #1
        sta *song_index
        jsr play_song
        jmp draw_done 

    check_left:
        lda *gamepad_pressed
        and #PAD_L
        beq check_select

        ; dont go below zero
        lda *song_index
        beq draw

        sec
        sbc #1
        sta *song_index
        jsr play_song
        jmp draw_done 

    check_select:
        lda *gamepad_pressed
        and #PAD_SELECT
        beq check_start

        ; Undocumented: selects plays a SFX sample when journey to silius is loaded.
        lda *song_index
        cmp #1
        bne draw

        lda #21
        jsr famistudio_sfx_sample_play
        jmp draw

    check_start:
        lda *gamepad_pressed
        and #PAD_START
        beq check_a

        lda #1
        eor *pause_flag
        sta *pause_flag

        jsr famistudio_music_pause
        jmp draw

    check_a:
        lda *gamepad_pressed
        and #PAD_A
        beq check_b

        lda #0
        ldx #FAMISTUDIO_SFX_CH0
        jsr famistudio_sfx_play
        beq draw

    check_b:
        lda *gamepad_pressed
        and #PAD_B
        beq draw

        lda #1
        ldx #FAMISTUDIO_SFX_CH1
        jsr famistudio_sfx_play
        beq draw
draw:
    jsr famistudio_update ; TODO: Call in NMI.
 
    lda *nmt_update_mode
    bne draw_done ; Dont allow update if we already have an update pending.

    ldx #0
    jsr update_equalizer
    ldx #1
    jsr update_equalizer
    ldx #2
    jsr update_equalizer
    ldx #3
    jsr update_equalizer
    ldx #4
    jsr update_equalizer

    lda #1
    sta *nmt_update_mode

draw_done:

    jsr ppu_update
    jmp main_loop

; Shiru's code.
; x = lo byte of RLE data addr
; y = hi byte of RLE data addr
rle_decompress:

    rle_lo   = r0
    rle_high = r1
    rle_tag  = r2
    rle_byte = r3

    stx *rle_lo
    sty *rle_high
    ldy #0
    jsr .rle_decompress_read_byte
    sta *rle_tag
.rle_decompress_loop:
    jsr .rle_decompress_read_byte
    cmp *rle_tag
    beq .rle_decompress_is_rle
    sta 0x2007
    sta *rle_byte
    bne .rle_decompress_loop
.rle_decompress_is_rle:
    jsr .rle_decompress_read_byte
    cmp #0
    beq .rle_decompress_done
    tax
    lda *rle_byte
.rle_loop:
    sta 0x2007
    dex
    bne .rle_loop
    beq .rle_decompress_loop
.rle_decompress_done: ;.4
    rts

.rle_decompress_read_byte:

    rle_lo   = r0
    rle_high = r1

    lda [*rle_lo],y
    inc *rle_lo
    bne .rle_decompress_read_byte_done
    inc *rle_high
.rle_decompress_read_byte_done:
    rts

; Draws text with rendering on.
; x/y = tile position
; p0  = pointer to text data.
draw_text:

    temp_x = r2
    temp   = r3
    text_ptr = p0
    
    stx *temp_x
    ldx *nmt_update_len
    tya
    lsr
    lsr
    lsr
    ora #0x20 ; high bits of Y + 0x20
    sta *nmt_update_data,x
    inx
    tya
    asl
    asl
    asl
    asl
    asl
    sta *temp
    lda *temp_x
    ora *temp
    sta *nmt_update_data,x
    inx
    lda #28 ; all our strings have 28 characters.
    sta *nmt_update_data,x
    inx

    ldy #0
    text_loop:
        lda [*text_ptr],y
        sta *nmt_update_data,x
        inx
        iny
        cpy #28
        bne text_loop

    stx *nmt_update_len
    lda #2
    sta *nmt_update_mode
    rts

setup_background:

    ; first nametable, start by clearing to empty
    lda 0x2002 ; reset latch
    lda #0x20
    sta 0x2006
    lda #0x00
    sta 0x2006

    ; BG image.
    ldx #<screen_data_rle
    ldy #>screen_data_rle
    jsr rle_decompress

    ; Add a few sprites to the FamiStudio logo.
    lda #80
    sta oam+3
    lda #15
    sta oam+0
    lda #0x51
    sta oam+1
    lda #1
    sta oam+2

    lda #72
    sta oam+7
    lda #23
    sta oam+4
    lda #0x60
    sta oam+5
    lda #1
    sta oam+6

    lda #88
    sta oam+11
    lda #23
    sta oam+8
    lda #0x62
    sta oam+9
    lda #1
    sta oam+10

    ; Draw version
    lda 0x2002
    lda #0x23
    sta 0x2006
    lda #0x79
    sta 0x2006

    ldy #0
    version_loop:
        lda version_text,y
        sta 0x2007
        iny
        cpy #5
        bne version_loop

    rts

.area _SONG1 (ABS)
.org 0xA000
song_silver_surfer:
.include "song_silver_surfer_sdas.s"

sfx_data:
.include "sfx_sdas.s"

.area _SONG2 (ABS)
.org 0xC000
song_journey_to_silius:
.include "song_journey_to_silius_sdas.s"

.area _SONG3 (ABS)
.org 0xD000
song_shatterhand:
.include "song_shatterhand_sdas.s"

.area _DPCM (ABS)
.org 0xE000
.incbin "song_journey_to_silius_sdas.dmc"

.area VECTORS (ABS)
.org 0xFFFA
.dw nmi
.dw reset
.dw irq

.area _CHARS (ABS)
.org 0x10000
.incbin "demo.chr"
.incbin "demo.chr"
