
FAMISTUDIO_VERSION_MAJOR  = 4
FAMISTUDIO_VERSION_MINOR  = 2
FAMISTUDIO_VERSION_HOTFIX = 0

    ; HEADER
    INES_MAPPER = 0 ; 0 = NROM
    INES_MIRROR = 1 ; 0 = horizontal mirroring, 1 = vertical mirroring
    INES_SRAM   = 0 ; 1 = battery backed SRAM at $6000-7FFF

    .db 'N', 'E', 'S', $1A ; ID 
    .db $02 ; 16k PRG bank count
    .db $01 ; 8k CHR bank count
    .db INES_MIRROR | (INES_SRAM << 1) | ((INES_MAPPER & $f) << 4)
    .db (INES_MAPPER & %11110000)
    .db $0, $0, $0, $0, $0, $0, $0, $0 ; padding
    .fillvalue $ff

    ; ZEROPAGE
    .enum $0000
    nmi_lock:           .dsb 1 ; prevents NMI re-entry
    nmi_count:          .dsb 1 ; is incremented every NMI
    nmi_ready:          .dsb 1 ; set to 1 to push a PPU frame update, 2 to turn rendering off next NMI
    scroll_x:           .dsb 1 ; x scroll position
    scroll_y:           .dsb 1 ; y scroll position
    scroll_nmt:         .dsb 1 ; nametable select (0-3 = $2000,$2400,$2800,$2C00)
    gamepad:            .dsb 1
    gamepad_previous:   .dsb 1
    gamepad_pressed:    .dsb 1
    song_index:         .dsb 1
    pause_flag:         .dsb 1
    nmt_update_mode:    .dsb 1   ; update "mode", 0 = nothing to do, 1 = column mode, 2 = row mode + palettes
    nmt_update_data:    .dsb 128 ; nametable update entry buffer for PPU update
    nmt_update_len:     .dsb 1 ; number of bytes in nmt_update_data buffer
    palette:            .dsb 32  ; palette buffer for PPU update

    ; General purpose temporary vars.
    r0: .dsb 1
    r1: .dsb 1
    r2: .dsb 1
    r3: .dsb 1
    r4: .dsb 1

    ; General purpose pointers.
    p0: .dsb 2
    .ende

    ; RAM
    .enum $0300
    .ende

    ; OAM
    .enum $0200
    oam: .dsb 256        ; sprite OAM data to be uploaded by DMA
    .ende

    ; CODE
    .base $8000

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
FAMISTUDIO_DPCM_OFF           = $e000

; ASM6-specific config.
FAMISTUDIO_ASM6_ZP_ENUM   = $00b4
FAMISTUDIO_ASM6_BSS_ENUM  = $0300
FAMISTUDIO_ASM6_CODE_BASE = $8000

    .include "..\famistudio_asm6.asm"

; Our single screen.
screen_data_rle:
    .incbin "demo.rle"

default_palette:
    .incbin "demo.pal"
    .incbin "demo.pal"

; Silver Surfer - BGM 2
song_title_silver_surfer:
    .db $ff, $ff, $ff, $12, $22, $25, $2f, $1e, $2b, $ff, $12, $2e, $2b, $1f, $1e, $2b, $ff, $4f, $ff, $01, $06, $0c, $ff, $36, $ff, $ff, $ff, $ff

; Journey To Silius - Menu
song_title_jts:
    .db $ff, $ff, $09, $28, $2e, $2b, $27, $1e, $32, $ff, $13, $28, $ff, $12, $22, $25, $22, $2e, $2c, $ff, $4f, $ff, $0c, $1e, $27, $2e, $ff, $ff

; Shatterhand - Final Area
song_title_shatterhand:
    .db $ff, $ff, $12, $21, $1a, $2d, $2d, $1e, $2b, $21, $1a, $27, $1d, $ff, $4f, $ff, $05, $22, $27, $1a, $25, $ff, $00, $2b, $1e, $1a, $ff, $ff

NUM_SONGS = 3

reset:

    sei       ; mask interrupts
    lda #0
    sta $2000 ; disable NMI
    sta $2001 ; disable rendering
    sta $4015 ; disable APU sound
    sta $4010 ; disable DMC IRQ
    lda #$40
    sta $4017 ; disable APU IRQ
    cld       ; disable decimal mode
    ldx #$FF
    txs       ; initialize stack
    ; wait for first vblank
    bit $2002
    @wait_vblank_loop:
        bit $2002
        bpl @wait_vblank_loop
    ; clear all RAM to 0
    lda #0
    ldx #0
    @clear_ram_loop:
        sta $0000, X
        sta $0100, X
        sta $0200, X
        sta $0300, X
        sta $0400, X
        sta $0500, X
        sta $0600, X
        sta $0700, X
        inx
        bne @clear_ram_loop
    ; place all sprites offscreen at Y=255
    lda #255
    ldx #0
    @clear_oam_loop:
        sta oam, X
        inx
        inx
        inx
        inx
        bne @clear_oam_loop
    ; wait for second vblank
    @wait_vblank_loop2:
        bit $2002
        bpl @wait_vblank_loop2
    ; NES is initialized, ready to begin!
    ; enable the NMI for graphical updates, and jump to our main program
    lda #%10001000
    sta $2000
    jmp main

nmi:
    ; save registers
    pha
    txa
    pha
    tya
    pha
    ; prevent NMI re-entry
    lda nmi_lock
    beq @lock_nmi
    jmp @nmi_end
@lock_nmi:
    lda #1
    sta nmi_lock
    inc nmi_count
    lda nmi_ready
    bne @check_rendering_off ; nmi_ready == 0 not ready to update PPU
    jmp @ppu_update_end
@check_rendering_off:
    cmp #2 ; nmi_ready == 2 turns rendering off
    bne @oam_dma
        lda #%00000000
        sta $2001
        ldx #0
        stx nmi_ready
        jmp @ppu_update_end
@oam_dma:
    ; sprite OAM DMA
    ldx #0
    stx $2003
    lda #>oam
    sta $4014

; nametable update
@nmt_update:
    lda nmt_update_mode 
    bne @do_update
    jmp @update_done
    @do_update:
        ldx #0
        cpx nmt_update_len
        beq @palettes
        asl
        asl
        ora #%10000000
        sta $2000
        ldx #0
        @nmt_update_loop:
            lda nmt_update_data, x
            inx
            sta $2006
            lda nmt_update_data, x
            inx
            sta $2006
            ldy nmt_update_data, x
            inx
            @col_loop:
                lda nmt_update_data, x
                inx
                sta $2007
                dey
                bne @col_loop
            cpx nmt_update_len
            bcc @nmt_update_loop

; palettes
@palettes:
    lda nmt_update_mode
    cmp #2
    beq @palettes_need_update
    jmp @update_done
    @palettes_need_update:
        lda $2002
        lda #$3F
        sta $2006
        ldx #0
        stx $2006 ; set PPU address to $3F00
        lda palette+0
        sta $2007
        lda palette+1
        sta $2007
        lda palette+2
        sta $2007
        lda palette+3
        sta $2007
        lda palette+4
        sta $2007
        lda palette+5
        sta $2007
        lda palette+6
        sta $2007
        lda palette+7
        sta $2007
        lda palette+8
        sta $2007
        lda palette+9
        sta $2007
        lda palette+10
        sta $2007
        lda palette+11
        sta $2007
        lda palette+12
        sta $2007
        lda palette+13
        sta $2007
        lda palette+14
        sta $2007
        lda palette+15
        sta $2007
        lda palette+16
        sta $2007
        lda palette+17
        sta $2007
        lda palette+18
        sta $2007
        lda palette+19
        sta $2007
        lda palette+20
        sta $2007
        lda palette+21
        sta $2007
        lda palette+22
        sta $2007
        lda palette+23
        sta $2007
        lda palette+24
        sta $2007
        lda palette+25
        sta $2007
        lda palette+26
        sta $2007
        lda palette+27
        sta $2007
        lda palette+28
        sta $2007
        lda palette+29
        sta $2007
        lda palette+30
        sta $2007
        lda palette+31
        sta $2007

@update_done:
    ; Clear update mode.
    lda #0 
    sta nmt_update_mode
    sta nmt_update_len

@scroll:
    lda scroll_nmt
    and #%00000011 ; keep only lowest 2 bits to prevent error
    ora #%10001000
    sta $2000
    lda scroll_x
    sta $2005
    lda scroll_y
    sta $2005
    ; enable rendering
    lda #%00011110
    sta $2001
    ; flag PPU update complete
    ldx #0
    stx nmi_ready
@ppu_update_end:
    ; if this engine had music/sound, this would be a good place to play it
    ; unlock re-entry flag
    lda #0
    sta nmi_lock
@nmi_end:
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
    sta nmi_ready
    @wait:
        lda nmi_ready
        bne @wait
    rts

; ppu_skip: waits until next NMI, does not update PPU
ppu_skip:
    lda nmi_count
    @wait:
        cmp nmi_count
        beq @wait
    rts

; ppu_off: waits until next NMI, turns rendering off (now safe to write PPU directly via $2007)
ppu_off:
    lda #2
    sta nmi_ready
    @wait:
        lda nmi_ready
        bne @wait
    rts

PAD_A      = $01
PAD_B      = $02
PAD_SELECT = $04
PAD_START  = $08
PAD_U      = $10
PAD_D      = $20
PAD_L      = $40
PAD_R      = $80

gamepad_poll:
    ; strobe the gamepad to latch current button state
    lda #1
    sta $4016
    lda #0
    sta $4016
    ; read 8 bytes from the interface at $4016
    ldx #8
    @gamepad_loop:
        pha
        lda $4016
        ; combine low two bits and store in carry bit
        and #%00000011
        cmp #%00000001
        pla
        ; rotate carry into gamepad variable
        ror a
        dex
        bne @gamepad_loop
    sta gamepad
    rts

gamepad_poll_dpcm_safe:
    
    lda gamepad
    sta gamepad_previous
    jsr gamepad_poll
    @reread:
        lda gamepad
        pha
        jsr gamepad_poll
        pla
        cmp gamepad
        bne @reread

    @toggle:
    eor gamepad_previous
    and gamepad
    sta gamepad_pressed

    rts

version_text: ; 
    .byte $34 + FAMISTUDIO_VERSION_MAJOR, $3e, $34 + FAMISTUDIO_VERSION_MINOR, $3e, $34 + FAMISTUDIO_VERSION_HOTFIX

play_song:

    @text_ptr = p0

    lda song_index
    cmp #1
    beq @journey_to_silius
    cmp #2
    beq @shatterhand

    ; Here since both of our songs came from different FamiStudio projects, 
    ; they are actually 3 different song data, with a single song in each.
    ; For a real game, if would be preferable to export all songs together
    ; so that instruments shared across multiple songs are only exported once.
    @silver_surfer:
        lda #<song_title_silver_surfer
        sta @text_ptr+0
        lda #>song_title_silver_surfer
        sta @text_ptr+1
        ldx #<music_data_silver_surfer_c_stephen_ruddy
        ldy #>music_data_silver_surfer_c_stephen_ruddy
        jmp @play_song

    @journey_to_silius:
        lda #<song_title_jts
        sta @text_ptr+0
        lda #>song_title_jts
        sta @text_ptr+1
        ldx #<music_data_journey_to_silius
        ldy #>music_data_journey_to_silius
        jmp @play_song

    @shatterhand:
        lda #<song_title_shatterhand
        sta @text_ptr+0
        lda #>song_title_shatterhand
        sta @text_ptr+1
        ldx #<music_data_shatterhand
        ldy #>music_data_shatterhand
        jmp @play_song
    
    @play_song:
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
    .byte $47 ; Square 1
    .byte $4b ; Square 2
    .byte $4f ; Triangle
    .byte $53 ; Noise
    .byte $57 ; DPCM

equalizer_ppu_addr_hi_lookup:
    .byte $22 ; Square 1
    .byte $22 ; Square 2
    .byte $22 ; Triangle
    .byte $22 ; Noise
    .byte $22 ; DPCM

; Which 4 BG tiles to draw for each "volume" [0-7]
equalizer_volume_lookup:
    .byte $e3, $e3, $e3, $e3 ; 0
    .byte $e3, $e3, $e3, $e0 ; 1
    .byte $e3, $e3, $e3, $f0 ; 2
    .byte $e3, $e3, $e0, $f0 ; 3
    .byte $e3, $e3, $f0, $f0 ; 4
    .byte $e3, $e0, $f0, $f0 ; 5
    .byte $e3, $f0, $f0, $f0 ; 6
    .byte $e0, $f0, $f0, $f0 ; 7
    .byte $f0, $f0, $f0, $f0 ; 8

; To add some color variety.
equalizer_color_lookup:
    .byte $00, $01, $02
    .byte $00, $01

; x = channel to update
update_equalizer:
    
    channel_idx = r0
    color_offset = r1

    stx channel_idx
    lda equalizer_color_lookup, x
    sta color_offset

    ; Write 2 addresses
    ldy nmt_update_len
    lda equalizer_ppu_addr_hi_lookup, x
    sta nmt_update_data,y
    lda equalizer_ppu_addr_lo_lookup, x
    sta nmt_update_data+1,y

    ; Always update 4 tiles
    lda #4
    sta nmt_update_data+2,y

    ; Compute lookup index based on "volume".
    lda famistudio_chn_note_counter, x
    asl
    asl
    tax

    clc
    lda equalizer_volume_lookup, x
    adc color_offset
    sta nmt_update_data+3,y
    lda equalizer_volume_lookup+1, x
    adc color_offset
    sta nmt_update_data+4,y
    lda equalizer_volume_lookup+2, x
    adc color_offset
    sta nmt_update_data+5,y
    lda equalizer_volume_lookup+3, x
    adc color_offset
    sta nmt_update_data+6,y
    
    lda #7
    adc nmt_update_len
    sta nmt_update_len 
    ldx channel_idx

    rts

main:

    ldx #0
    @palette_loop:
        lda default_palette, x
        sta palette, x
        sta palette+16, x
        inx
        cpx #16
        bcc @palette_loop

    ; Force palette update.
    lda #2
    sta nmt_update_mode

    jsr setup_background
    jsr ppu_update

    lda #0 ; song zero.
    sta song_index
    jsr play_song

    ; Load SFX
    ldx #<sounds
    ldy #>sounds
    jsr famistudio_sfx_init

@loop:

    jsr gamepad_poll_dpcm_safe
    
    @check_right:
        lda gamepad_pressed
        and #PAD_R
        beq @check_left

        ; dont go beyond last song.
        lda song_index
        cmp #(NUM_SONGS - 1)
        beq @draw

        ; next song.
        clc
        adc #1
        sta song_index
        jsr play_song
        jmp @draw_done 

    @check_left:
        lda gamepad_pressed
        and #PAD_L
        beq @check_select

        ; dont go below zero
        lda song_index
        beq @draw

        sec
        sbc #1
        sta song_index
        jsr play_song
        jmp @draw_done 

    @check_select:
        lda gamepad_pressed
        and #PAD_SELECT
        beq @check_start

        ; Undocumented: selects plays a SFX sample when journey to silius is loaded.
        lda song_index
        cmp #1
        bne @draw

        lda #21
        jsr famistudio_sfx_sample_play
        jmp @draw

    @check_start:
        lda gamepad_pressed
        and #PAD_START
        beq @check_a

        lda #1
        eor pause_flag
        sta pause_flag

        jsr famistudio_music_pause
        jmp @draw

    @check_a:
        lda gamepad_pressed
        and #PAD_A
        beq @check_b

        lda #0
        ldx #FAMISTUDIO_SFX_CH0
        jsr famistudio_sfx_play
        beq @draw

    @check_b:
        lda gamepad_pressed
        and #PAD_B
        beq @draw

        lda #1
        ldx #FAMISTUDIO_SFX_CH1
        jsr famistudio_sfx_play
        beq @draw

@draw:

    jsr famistudio_update ; TODO: Call in NMI.
    
    lda nmt_update_mode
    bne @draw_done ; Dont allow update if we already have an update pending.

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
    sta nmt_update_mode

@draw_done:

    jsr ppu_update
    jmp @loop

; Shiru's code.
; x = lo byte of RLE data addr
; y = hi byte of RLE data addr
rle_decompress:

@rle_lo   = r0
@rle_high = r1
@rle_tag  = r2
@rle_byte = r3

    stx <@rle_lo
    sty <@rle_high
    ldy #0
    jsr rle_read_byte
    sta <@rle_tag
@loop:
    jsr rle_read_byte
    cmp @rle_tag
    beq @is_rle
    sta $2007
    sta <@rle_byte
    bne @loop
@is_rle:
    jsr rle_read_byte
    cmp #0
    beq @done
    tax
    lda <@rle_byte
@rle_loop:
    sta $2007
    dex
    bne @rle_loop
    beq @loop
@done: ;.4
    rts

rle_read_byte:

@rle_lo   = r0
@rle_high = r1

    lda (@rle_lo),y
    inc @rle_lo
    bne @done
    inc @rle_high
@done:
    rts

; Draws text with rendering on.
; x/y = tile position
; p0  = pointer to text data.
draw_text:

    @temp_x = r2
    @temp   = r3
    @text_ptr = p0
    
    stx @temp_x
    ldx nmt_update_len
    tya
    lsr
    lsr
    lsr
    ora #$20 ; high bits of Y + $20
    sta nmt_update_data,x
    inx
    tya
    asl
    asl
    asl
    asl
    asl
    sta @temp
    lda @temp_x
    ora @temp
    sta nmt_update_data,x
    inx
    lda #28 ; all our strings have 28 characters.
    sta nmt_update_data,x
    inx

    ldy #0
    @text_loop:
        lda (@text_ptr),y
        sta nmt_update_data,x
        inx
        iny
        cpy #28
        bne @text_loop

    stx nmt_update_len
    lda #2
    sta nmt_update_mode
    rts

setup_background:

    ; first nametable, start by clearing to empty
    lda $2002 ; reset latch
    lda #$20
    sta $2006
    lda #$00
    sta $2006

    ; BG image.
    ldx #<screen_data_rle
    ldy #>screen_data_rle
    jsr rle_decompress

    ; Add a few sprites to the FamiStudio logo.
    lda #80
    sta oam+3
    lda #15
    sta oam+0
    lda #$51
    sta oam+1
    lda #1
    sta oam+2

    lda #72
    sta oam+7
    lda #23
    sta oam+4
    lda #$60
    sta oam+5
    lda #1
    sta oam+6

    lda #88
    sta oam+11
    lda #23
    sta oam+8
    lda #$62
    sta oam+9
    lda #1
    sta oam+10

    ; Draw version
    lda $2002
    lda #$23
    sta $2006
    lda #$79
    sta $2006

    ldy #0
    @version_loop:
        lda version_text,y
        sta $2007
        iny
        cpy #5
        bne @version_loop

    rts

    ; SONG
    .org $a000
song_silver_surfer:
    .include "song_silver_surfer_asm6.asm"
sfx_data:
    .include "sfx_asm6.asm"
    .org $c000
song_journey_to_silius:
    .include "song_journey_to_silius_asm6.asm"
    .org $d000
song_shatterhand:
    .include "song_shatterhand_asm6.asm"

    ; DPCM
    .org $e000
    .incbin "song_journey_to_silius_asm6.dmc"

    ; VECTORS
    .org $fffa
    .dw nmi
    .dw reset
    .dw irq

    ; CHARS
    .incbin "demo.chr"
    .incbin "demo.chr"
