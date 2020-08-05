    ; HEADER
    .inesprg 2 ; 1x 16KB PRG code
    .ineschr 1 ; 1x  8KB CHR data
    .inesmap 0 ; mapper 0 = NROM, no bank swapping
    .inesmir 1 ; background mirroring

    ; ZEROPAGE
    .rsset $0000
nmi_lock:           .rs 1 ; prevents NMI re-entry
nmi_count:          .rs 1 ; is incremented every NMI
nmi_ready:          .rs 1 ; set to 1 to push a PPU frame update, 2 to turn rendering off next NMI
nmt_col_update_len: .rs 1 ; number of bytes in nmt_col_update buffer
scroll_x:           .rs 1 ; x scroll position
scroll_y:           .rs 1 ; y scroll position
scroll_nmt:         .rs 1 ; nametable select (0-3 = $2000,$2400,$2800,$2C00)
gamepad:            .rs 1
gamepad_previous:   .rs 1
gamepad_pressed:    .rs 1
song_index:         .rs 1

; General purpose temporary vars.
r0: .rs 1
r1: .rs 1
r2: .rs 1
r3: .rs 1
r4: .rs 1

; General purpose pointers.
p0: .rs 2

    ; RAM
    .rsset $0300
nmt_col_update: .rs 128 ; nametable update entry buffer for PPU update (column mode)
palette:        .rs 32  ; palette buffer for PPU update

    ; OAM
    .rsset $0200
oam: .rs 256        ; sprite OAM data to be uploaded by DMA

    ; CODE
    .bank 0
    .org $8000 

FAMISTUDIO_CFG_EXTERNAL     = 1
FAMISTUDIO_EXP_VRC7         = 1
FAMISTUDIO_CFG_DPCM_SUPPORT = 1
FAMISTUDIO_CFG_SFX_SUPPORT  = 1
FAMISTUDIO_CFG_SFX_STREAMS  = 2
FAMISTUDIO_USE_VOLUME_TRACK = 1
FAMISTUDIO_USE_PITCH_TRACK  = 1
FAMISTUDIO_USE_SLIDE_NOTES  = 1
FAMISTUDIO_USE_VIBRATO      = 1
FAMISTUDIO_USE_ARPEGGIO     = 1

    .include "..\famistudio_nesasm.asm"

; Our single screen.
screen_data_rle:
    .incbin "demo.rle"

default_palette:
    .incbin "demo.pal"
    .incbin "demo.pal"

;test_macro .macro 
;;    .local @ok
;    inc \1+0
;;    bne @ok
;;    inc addr+1
;;@ok:
;    .endm

;proc1:

;.test = r0
;    test_macro .test

;proc2:

;.test = r1
;    test_macro .test

;test_macro .macro 
;;    .local @ok
;    lda \1+0 \2
;    adc \3 \4
;;    bne @ok
;;    inc addr+1
;;@ok:
;    .endm

;proc1:

;    test_macro r0,,y, a, b

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
    .wait_vblank_loop:
        bit $2002
        bpl .wait_vblank_loop
    ; clear all RAM to 0
    lda #0
    ldx #0
    .clear_ram_loop:
        sta <$0000, X
        sta $0100, X
        sta $0200, X
        sta $0300, X
        sta $0400, X
        sta $0500, X
        sta $0600, X
        sta $0700, X
        inx
        bne .clear_ram_loop
    ; place all sprites offscreen at Y=255
    lda #255
    ldx #0
    .clear_oam_loop:
        sta oam, X
        inx
        inx
        inx
        inx
        bne .clear_oam_loop
    ; wait for second vblank
    .wait_vblank_loop2:
        bit $2002
        bpl .wait_vblank_loop2
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
    lda <nmi_lock
    beq .lock_nmi
    jmp .nmi_end
.lock_nmi:
    lda #1
    sta <nmi_lock
    inc <nmi_count
    lda <nmi_ready
    bne .check_rendering_off ; nmi_ready == 0 not ready to update PPU
    jmp .ppu_update_end
.check_rendering_off:
    cmp #2 ; nmi_ready == 2 turns rendering off
    bne .oam_dma
        lda #%00000000
        sta $2001
        ldx #0
        stx <nmi_ready
        jmp .ppu_update_end
.oam_dma:
    ; sprite OAM DMA
    ldx #0
    stx $2003
    lda #HIGH(oam)
    sta $4014

    ; nametable update (column)
    .col_update:
        ldx #0
        cpx <nmt_col_update_len
        beq .palettes
        lda #%10001100
        sta $2000 ; set vertical nametable increment
        ldx #0
        cpx <nmt_col_update_len
        bcs .palettes
        .nmt_col_update_loop:
            lda nmt_col_update, x
            inx
            sta $2006
            lda nmt_col_update, x
            inx
            sta $2006
            ldy nmt_col_update, x
            inx
            .col_loop:
                lda nmt_col_update, x
                inx
                sta $2007
                dey
                bne .col_loop
            cpx <nmt_col_update_len
            bcc .nmt_col_update_loop
        lda #0
        sta <nmt_col_update_len

    ; palettes
    .palettes:
        lda #%10001000
        sta $2000 ; set horizontal nametable increment  
        lda $2002
        lda #$3F
        sta $2006
        ldx #0
        stx $2006 ; set 0PPU address to $3F00
        .pal_loop:
            lda palette, X
            sta $2007
            inx
            cpx #32
            bne .pal_loop

.scroll:
    lda <scroll_nmt
    and #%00000011 ; keep only lowest 2 bits to prevent error
    ora #%10001000
    sta $2000
    lda <scroll_x
    sta $2005
    lda <scroll_y
    sta $2005
    ; enable rendering
    lda #%00011110
    sta $2001
    ; flag PPU update complete
    ldx #0
    stx <nmi_ready
.ppu_update_end:
    ; if this engine had music/sound, this would be a good place to play it
    ; unlock re-entry flag
    lda #0
    sta <nmi_lock
.nmi_end:
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
    sta <nmi_ready
    .wait:
        lda <nmi_ready
        bne .wait
    rts

; ppu_skip: waits until next NMI, does not update PPU
ppu_skip:
    lda <nmi_count
    .wait:
        cmp <nmi_count
        beq .wait
    rts

; ppu_off: waits until next NMI, turns rendering off (now safe to write PPU directly via $2007)
ppu_off:
    lda #2
    sta <nmi_ready
    .wait:
        lda <nmi_ready
        bne .wait
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
    .gamepad_loop:
        pha
        lda $4016
        ; combine low two bits and store in carry bit
        and #%00000011
        cmp #%00000001
        pla
        ; rotate carry into gamepad variable
        ror a
        dex
        bne .gamepad_loop
    sta <gamepad
    rts

gamepad_poll_dpcm_safe:
    
    lda <gamepad
    sta <gamepad_previous
    jsr gamepad_poll
    .reread:
        lda <gamepad
        pha
        jsr gamepad_poll
        pla
        cmp <gamepad
        bne .reread

    .toggle:
    eor <gamepad_previous
    and <gamepad
    sta <gamepad_pressed

    rts

play_song:

;    ldx #.lobyte(castlevania_2_music_data)
;    ldy #.hibyte(castlevania_2_music_data)
    .if FAMISTUDIO_CFG_PAL_SUPPORT
    lda #0
    .else
    lda #1 ; NTSC
    .endif  
;    jsr famistudio_init
    
    lda #0
;    jsr famistudio_music_play

    rts

equalizer_lookup:
    .byte $f0, $f0, $f0, $f0 ; 0
    .byte $f0, $f0, $f0, $b8 ; 1
    .byte $f0, $f0, $f0, $c8 ; 2
    .byte $f0, $f0, $b8, $c8 ; 3
    .byte $f0, $f0, $c8, $c8 ; 4
    .byte $f0, $b8, $c8, $c8 ; 5
    .byte $f0, $c8, $c8, $c8 ; 6
    .byte $b8, $c8, $c8, $c8 ; 7
    .byte $c8, $c8, $c8, $c8 ; 8
equalizer_color_lookup:
    .byte $01, $02, $00, $02, $02

; a = channel to update
update_equalizer:
    
.pos_x = r0
.color_offset = r1

    tay
    lda equalizer_color_lookup, y
    sta <.color_offset
    tya

    ; compute x position.
    asl a
    asl a
    sta <.pos_x

    ; compute lookup index.
    ;lda famistudio_chn_note_counter, y
    asl a
    asl a
    tay

    ; compute 2 addresses
    ldx <nmt_col_update_len
    lda #$22
    sta nmt_col_update,x
    sta nmt_col_update+7,x
    lda #$47
    clc
    adc <.pos_x
    sta nmt_col_update+1,x
    adc #1
    sta nmt_col_update+8,x
    lda #4
    sta nmt_col_update+2,x
    sta nmt_col_update+9,x

    lda equalizer_lookup, y
    adc <.color_offset
    sta nmt_col_update+3,x
    sta nmt_col_update+10,x
    lda equalizer_lookup+1, y
    adc <.color_offset
    sta nmt_col_update+4,x
    sta nmt_col_update+11,x
    lda equalizer_lookup+2, y
    adc <.color_offset
    sta nmt_col_update+5,x
    sta nmt_col_update+12,x
    lda equalizer_lookup+3, y
    adc <.color_offset
    sta nmt_col_update+6,x
    sta nmt_col_update+13,x
    
    lda #14
    adc <nmt_col_update_len
    sta <nmt_col_update_len 

    rts

main:

    ldx #0
    .palette_loop:
        lda default_palette, X
        sta palette, X
        inx
        cpx #32
        bcc .palette_loop
    
    jsr setup_background

    lda #0 ; song zero.
    sta <song_index
    jsr play_song

    jsr ppu_update

.loop:

    jsr gamepad_poll_dpcm_safe
    
    ;check_right:
    ;   lda gamepad_pressed
    ;   and #PAD_R
    ;   beq check_left

    ;   ; dont go beyond last song.
    ;   lda <song_index
    ;   cmp max_song
    ;   beq .draw

    ;   ; next song.
    ;   clc
    ;   adc #1
    ;   sta <song_index
    ;   jsr play_song
    ;   jmp .draw_done ; Intentionally skipping equalizer update to keep NMI update small.

    ;check_left:
    ;   lda gamepad_pressed
    ;   and #PAD_L
    ;   beq draw

    ;   ; dont go below zero
    ;   lda <song_index
    ;   beq .draw

    ;   sec
    ;   sbc #1
    ;   sta <song_index
    ;   jsr play_song
    ;   jmp .draw_done ; Intentionally skipping equalizer update to keep NMI update small.

.draw:

    ;jsr famistudio_update ; MATTT: Call in NMI.
    
    lda #0
    jsr update_equalizer
    lda #1
    jsr update_equalizer
    lda #2
    jsr update_equalizer
    lda #3
    jsr update_equalizer
    lda #4
    jsr update_equalizer

.draw_done:

    jsr ppu_update
    jmp .loop

; Shiru's code.
; x = lo byte of RLE data addr
; y = hi byte of RLE data addr
rle_decompress:

.rle_lo   = r0
.rle_high = r1
.rle_tag  = r2
.rle_byte = r3

    stx <.rle_lo
    sty <.rle_high
    ldy #0
    jsr rle_read_byte
    sta <.rle_tag
.loop:
    jsr rle_read_byte
    cmp <.rle_tag
    beq .is_rle
    sta $2007
    sta <.rle_byte
    bne .loop
.is_rle:
    jsr rle_read_byte
    cmp #0
    beq .done
    tax
    lda <.rle_byte
.rle_loop:
    sta $2007
    dex
    bne .rle_loop
    beq .loop
.done: ;.4
    rts

rle_read_byte:

.rle_lo   = r0
.rle_high = r1

    lda [.rle_lo],y
    inc <.rle_lo
    bne .done
    inc <.rle_high
.done:
    rts

setup_background:

    ; first nametable, start by clearing to empty
    lda $2002 ; reset latch
    lda #$20
    sta $2006
    lda #$00
    sta $2006

    ; BG image.
    ldx #LOW(screen_data_rle)
    ldy #HIGH(screen_data_rle)
    jsr rle_decompress

    ; Add a few sprites to the FamiStudio logo.
    lda #80
    sta oam+3
    lda #15
    sta oam+0
    lda #$81
    sta oam+1
    lda #1
    sta oam+2

    lda #72
    sta oam+7
    lda #23
    sta oam+4
    lda #$90
    sta oam+5
    lda #1
    sta oam+6

    lda #88
    sta oam+11
    lda #23
    sta oam+8
    lda #$92
    sta oam+9
    lda #1
    sta oam+10

    rts

    ; VECTORS
    .bank 3
    .org $fffA
    .dw nmi
    .dw reset
    .dw irq

    ; CHARS
    .bank 4
    .org $0000
    .incbin "demo.chr"
    .incbin "demo.chr"
