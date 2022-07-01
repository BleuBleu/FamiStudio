; Simple FamiStudio ROM to play music on actual hardware.
; Based off Brad's (rainwarrior.ca) CA65 template, both the regular and FDS version.

; Enable all features.
FAMISTUDIO_CFG_EXTERNAL          = 1
FAMISTUDIO_CFG_SMOOTH_VIBRATO    = 1
FAMISTUDIO_CFG_DPCM_SUPPORT      = 1
FAMISTUDIO_CFG_EQUALIZER         = 1
FAMISTUDIO_USE_VOLUME_TRACK      = 1
FAMISTUDIO_USE_VOLUME_SLIDES     = 1
FAMISTUDIO_USE_PITCH_TRACK       = 1
FAMISTUDIO_USE_SLIDE_NOTES       = 1
FAMISTUDIO_USE_NOISE_SLIDE_NOTES = 1
FAMISTUDIO_USE_VIBRATO           = 1
FAMISTUDIO_USE_ARPEGGIO          = 1
FAMISTUDIO_USE_DUTYCYCLE_EFFECT  = 1
FAMISTUDIO_USE_DELTA_COUNTER     = 1
FAMISTUDIO_USE_RELEASE_NOTES     = 1

.ifdef FAMISTUDIO_USE_FAMITRACKER_TEMPO
    FAMISTUDIO_USE_FAMITRACKER_DELAYED_NOTES_OR_CUTS=1
.endif

.define FAMISTUDIO_CA65_ZP_SEGMENT   ZEROPAGE
.define FAMISTUDIO_CA65_RAM_SEGMENT  RAM
.define FAMISTUDIO_CA65_CODE_SEGMENT CODE

.include "../../SoundEngine/famistudio_ca65.s"

.segment "ZEROPAGE"
nmi_lock:           .res 1 ; prevents NMI re-entry
nmi_count:          .res 1 ; is incremented every NMI
nmi_ready:          .res 1 ; set to 1 to push a PPU frame update, 2 to turn rendering off next NMI
nmt_row_update_len: .res 1 ; number of bytes in nmt_row_update buffer
nmt_col_update_len: .res 1 ; number of bytes in nmt_row_update buffer
scroll_x:           .res 1 ; x scroll position
scroll_y:           .res 1 ; y scroll position
scroll_nmt:         .res 1 ; nametable select (0-3 = $2000,$2400,$2800,$2C00)
gamepad:            .res 1
gamepad_previous:   .res 1
gamepad_pressed:    .res 1
song_index:         .res 1

; General purpose temporary vars.
r0: .res 1
r1: .res 1
r2: .res 1
r3: .res 1
r4: .res 1
r5: .res 1

; General purpose pointers.
p0: .res 2

.segment "RAM"
; TODO: These 2 arent actually used at the same time... unify.
nmt_col_update: .res 128 ; nametable update entry buffer for PPU update (column mode)
nmt_row_update: .res 128 ; nametable update entry buffer for PPU update (row mode)
palette:        .res 32  ; palette buffer for PPU update

.segment "OAM"
oam: .res 256        ; sprite OAM data to be uploaded by DMA

.segment "HEADER"

INES_MAPPER = 4 ; 4 = MMC3 mapper.
INES_MIRROR = 1 ; 0 = horizontal mirroring, 1 = vertical mirroring
INES_SRAM   = 0 ; 1 = battery backed SRAM at $6000-7FFF

.if FAMISTUDIO_EXP_FDS
    .byte 'F','D','S',$1a
    .byte 1 ; side count
.elseif FAMISTUDIO_EXP_EPSM
    .byte 'N', 'E', 'S', $1A ; ID
    .byte $02 ; 16k PRG bank count
    .byte $01 ; 8k CHR bank count
    .byte INES_MIRROR | (INES_SRAM << 1) | ((INES_MAPPER & $f) << 4)
    .byte (INES_MAPPER & %11110000) | 11
    .byte $0, $0, $0, $0, $0, $4, $0, $0 ; padding
.else
    .byte 'N', 'E', 'S', $1A ; ID
    .byte $02 ; 16k PRG bank count
    .byte $01 ; 8k CHR bank count
    .byte INES_MIRROR | (INES_SRAM << 1) | ((INES_MAPPER & $f) << 4)
    .byte (INES_MAPPER & %11110000)
    .byte $0, $0, $0, $0, $0, $0, $0, $0 ; padding
.endif

.if FAMISTUDIO_EXP_FDS

; FDS File headers
FILE_COUNT = 6 + 1

.segment "SIDE1A"
; block 1
.byte $01 
.byte "*NINTENDO-HVC*"
.byte $00 ; manufacturer
.byte "FMS"
.byte $20 ; normal disk
.byte $00 ; game version
.byte $00 ; side
.byte $00 ; disk
.byte $00 ; disk type
.byte $00 ; unknown
.byte FILE_COUNT ; boot file count
.byte $FF,$FF,$FF,$FF,$FF
.byte $92 ; 2017
.byte $04 ; april
.byte $17 ; 17
.byte $49 ; country
.byte $61, $00, $00, $02, $00, $00, $00, $00, $00 ; unknown
.byte $92 ; 2017
.byte $04 ; april
.byte $17 ; 17
.byte $00, $80 ; unknown
.byte $00, $00 ; disk writer serial number
.byte $07 ; unknown
.byte $00 ; disk write count
.byte $00 ; actual disk side
.byte $00 ; unknown
.byte $00 ; price
; block 2
.byte $02
.byte FILE_COUNT

.segment "CODE_HDR"
; block 3
.import __CODE_RUN__
.import __CODE_SIZE__
.byte $03
.byte 0,0
.byte "CODE...."
.word __CODE_RUN__
.word __CODE_SIZE__
.byte 0 ; PRG
; block 4
.byte $04

.segment "TOC_HDR"
; block 3
.import __TOC_RUN__
.import __TOC_SIZE__
.byte $03
.byte 1,1
.byte "TOC....."
.word __TOC_RUN__
.word __TOC_SIZE__
.byte 0 ; PRG
; block 4
.byte $04

.segment "VECTORS_HDR"
; block 3
.import __VECTORS_RUN__
.import __VECTORS_SIZE__
.byte $03
.byte 2,2
.byte "VECTORS."
.word __VECTORS_RUN__
.word __VECTORS_SIZE__
.byte 0 ; PRG
; block 4
.byte $04

.segment "CHR0_HDR"
; block 3
.import __CHR0_SIZE__
.import __CHR0_RUN__
.byte $03
.byte 3,3
.byte "CHR0...."
.word __CHR0_RUN__
.word __CHR0_SIZE__
.byte 1 ; CHR
; block 4
.byte $04

.segment "CHR1_HDR"
; block 3
.import __CHR1_SIZE__
.import __CHR1_RUN__
.byte $03
.byte 4,4
.byte "CHR1...."
.word __CHR1_RUN__
.word __CHR1_SIZE__
.byte 1 ; CHR
; block 4
.byte $04

;; This block is the last to load, and enables NMI by "loading" the NMI enable value
;; directly into the PPU control register at $2000.
;; While the disk loader continues searching for one more boot file,
;; eventually an NMI fires, allowing us to take control of the CPU before the
;; license screen is displayed.
;.segment "BYPASS_HDR"
;; block 3
;.import __BYPASS_SIZE__
;.import __BYPASS_RUN__
;.byte $03
;.byte 5,5
;.byte "BYPASS.."
;.word $2000
;.word __BYPASS_SIZE__
;.byte 0 ; PRG (CPU:$2000)
;; block 4
;.byte $04

;.segment "BYPASS"
;.byte $90 ; enable NMI byte sent to $2000

; Alternative to the license screen bypass, just put the required copyright message at PPU:$2800.
.segment "BYPASS_HDR"
; block 3
.import __BYPASS_SIZE__
.import __BYPASS_RUN__
.byte $03
.byte 5,5
.byte "KYODAKU-"
.word $2800
.word __BYPASS_SIZE__
.byte 2 ; nametable (PPU:$2800)
; block 4
.byte $04
.segment "BYPASS"
.incbin "check.bin"

.endif

.segment "SONG"
.if !FAMISTUDIO_EXP_FDS
.incbin "song.bin" ; Test song, Bloody Tears.
.endif

.segment "DPCM"

.segment "TOC"

; Will be overwritten by FamiStudio.
; General info about the project (author, etc.), 64-bytes.
max_song:        .byte $00
.if FAMISTUDIO_EXP_FDS
fds_file_count:  .byte $06 ; Number of actual file on the disk.
.else
mmc_unused:      .byte $00
.endif
padding:         .res 6    ; reserved
project_name:    .res 28   ; Project name
project_author:  .res 28   ; Project author

; Up to 12 songs for now, 384 bytes + 64 bytes header = 448 bytes.
MAX_SONGS = 12

; Will be overwritten by FamiStudio.
; Each entry in the song table is 32-bytes:
;  - 1 byte: start page of the song
;  - 2 byte: start address of the song.
;  - 1 byte: song flags (uses DPCM or not)
;  - 28 bytes: song name.
song_table:
.if FAMISTUDIO_EXP_FDS
song_fds_file:   .byte $00
.else
song_page_start: .byte $00
.endif
song_addr_start: .word $8222 ; Test song has $222 bytes of DPCM, song is right after.
song_flags:      .byte $01   ; Test song uses DPCM
song_name:       .res  28

; the remaining 7 songs.
.res 32 * (MAX_SONGS - 1)

.segment "CHR0"
.incbin "rom.chr"

.segment "CHR1"
.incbin "rom.chr"

.segment "VECTORS"
vectors:
.word nmi
.if FAMISTUDIO_EXP_FDS
    .word nmi
    .word nmi ;bypass
.endif
.word reset
.word irq

.segment "CODE"

.if FAMISTUDIO_EXP_FDS
; FDS BIOS functions
fds_bios_load_files     = $e1f8
fds_bios_set_file_count = $e305
.endif

; Our single screen.
screen_data_rle:

.if FAMISTUDIO_EXP_FDS
.incbin "fds.rle"
.else
.incbin "rom.rle"
.endif

default_palette:
.incbin "rom.pal"

;.if FAMISTUDIO_EXP_FDS

;; this routine is entered by interrupting the last boot file load
;; by forcing an NMI not expected by the BIOS, allowing the license
;; screen to be skipped entirely.
;;
;; The last file writes $90 to $2000, enabling NMI during the file load.
;; The "extra" file in the FILE_COUNT causes the disk to keep seeking
;; past the last file, giving enough delay for an NMI to fire and interrupt
;; the process.
;bypass:
;   ; disable NMI
;   lda #0
;   sta $2000
;   ; replace NMI 3 "bypass" vector at $DFFA
;   lda #<nmi
;   sta $dffa
;   lda #>nmi
;   sta $dffb
;   ; tell the FDS reset routine that the BIOS initialized correctly
;   lda #$35
;   sta $0102
;   lda #$ac
;   sta $0103
;   ; reset the FDS to begin our program properly
;   jmp ($fffc)

;.endif

.if !FAMISTUDIO_EXP_FDS
; inital values for the mmc3_banks to load at startup.
init_mmc3_banks:
    .byte 0 ; CHR at 0 (2KB)
    .byte 2 ; CHR at 800 (2KB)
    .byte 4 ; CHR at 1000 (1KB)
    .byte 5 ; CHR at 1400 (1KB)
    .byte 6 ; CHR at 1800 (1KB)
    .byte 7 ; CHR at 1C00 (1KB)
    .byte 0 ; SONG data at 8000
    .byte 1 ; SONG data at A000
.endif

.proc reset

    .if ::FAMISTUDIO_EXP_FDS
        ; set FDS to use vertical mirroring
        lda $fa
        and #%11110111
        sta $4025
    .else
        ; MMC3 setup.
        lda #00 ; vertical mirroring
        sta $a000
        lda #$00 ; disable wram, not needed for such a simple demo.
        sta $a001
    .endif

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
    wait_vblank_loop:
        bit $2002
        bpl wait_vblank_loop
    ; clear all RAM to 0
    lda #0
    ldx #0
    clear_ram_loop:
        ; FDS uses some of that memory, will be cleared in the loop below.
        .if !::FAMISTUDIO_EXP_FDS
            sta $0000, x
            sta $0100, x
        .endif
        sta $0200, x
        sta $0300, x
        sta $0400, x
        sta $0500, x
        sta $0600, x
        sta $0700, x
        inx
        bne clear_ram_loop
    .if ::FAMISTUDIO_EXP_FDS
        ldx #0
        clear_zp_loop:
            sta $00, x
            inx
            cpx #$f9 ; $f9-ff used by FDS BIOS
            bcc clear_zp_loop
        ldx #$04 ; $0100-$0103 used by FDS BIOS
        clear_stack_loop:
            sta $100, x
            inx
            bne clear_stack_loop
    .endif
    ; place all sprites offscreen at Y=255
    lda #255
    ldx #0
    clear_oam_loop:
        sta oam, x
        inx
        bne clear_oam_loop

    ; wipe unused portion of FDS RAM (between SONG and VECTORS)
    .if ::FAMISTUDIO_EXP_FDS
        .import __SONG_RUN__
        WIPE_ADDR = __SONG_RUN__
        WIPE_SIZE = __VECTORS_RUN__ - __SONG_RUN__
        lda #<WIPE_ADDR
        sta p0+0
        lda #>WIPE_ADDR
        sta p0+1
        lda #0
        tay
        ldx #>WIPE_SIZE
        beq lefover
        block_clear_loop: ; 256 byte blocks
            sta (p0), y
            iny
            bne block_clear_loop
            inc p0+1
            dex
            bne block_clear_loop
        lefover:
        ldy #<WIPE_SIZE
        beq wait_vblank_loop2
        leftover_loop:
            dey
            sta (p0), y
            bne leftover_loop

        lda fds_file_count
        jsr fds_bios_set_file_count
        .word disk_id
    .else
        ; Setup initial MMC3 banks.
        ldx #0
        bank_loop:  
            lda init_mmc3_banks, x
            stx $8000
            sta $8001       
            inx
            cpx #8
            bne bank_loop
    .endif

    ; wait for second vblank
    wait_vblank_loop2:
        bit $2002
        bpl wait_vblank_loop2
    ; NES is initialized, ready to begin!
    ; enable the NMI for graphical updates, and jump to our main program
    lda #%10001000
    sta $2000
    jmp main

.endproc

.proc nmi
    ; save registers
    pha
    txa
    pha
    tya
    pha
    ; prevent NMI re-entry
    lda nmi_lock
    beq lock_nmi
        jmp nmi_end
lock_nmi:
    lda #1
    sta nmi_lock
    ; increment frame counter
    inc nmi_count
    ;
    lda nmi_ready
    bne check_rendering_off ; nmi_ready == 0 not ready to update PPU
        jmp ppu_update_end
check_rendering_off:
    cmp #2 ; nmi_ready == 2 turns rendering off
    bne oam_dma
        lda #%00000000
        sta $2001
        ldx #0
        stx nmi_ready
        jmp ppu_update_end
oam_dma:
    ; sprite OAM DMA
    ldx #0
    stx $2003
    lda #>oam
    sta $4014

; nametable update (column)
col_update:
    ldx #0
    cpx nmt_col_update_len
    beq row_update
    lda #%10001100
    sta $2000 ; set vertical nametable increment
    ldx #0
    cpx nmt_col_update_len
    bcs palettes
    nmt_col_update_loop:
        lda nmt_col_update, x
        inx
        sta $2006
        lda nmt_col_update, x
        inx
        sta $2006
        ldy nmt_col_update, x
        inx
        col_loop:
            lda nmt_col_update, x
            inx
            sta $2007
            dey
            bne col_loop
        cpx nmt_col_update_len
        bcc nmt_col_update_loop
    lda #0
    sta nmt_col_update_len

; nametable update (row)
row_update:
    lda #%10001000
    sta $2000 ; set horizontal nametable increment
    ldx #0
    cpx nmt_row_update_len
    bcs palettes
    nmt_row_update_loop:
        lda nmt_row_update, x
        inx
        sta $2006
        lda nmt_row_update, x
        inx
        sta $2006
        ldy nmt_row_update, x
        inx
        row_loop:
            lda nmt_row_update, x
            inx
            sta $2007
            dey
            bne row_loop
        cpx nmt_row_update_len
        bcc nmt_row_update_loop
    lda #0
    sta nmt_row_update_len

; palettes
palettes:
    lda $2002
    lda #$3F
    sta $2006
    ldx #0
    stx $2006 ; set PPU address to $3F00

.if ::FAMISTUDIO_EXP_FDS        

    ; Need to squeeze a few more cycles of the NMI when in FDS mode.
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
.else
    pal_loop:
        lda palette, x
        sta $2007
        inx
        cpx #32
        bne pal_loop
.endif

scroll:
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
ppu_update_end:
    ; if this engine had music/sound, this would be a good place to play it
    ; unlock re-entry flag
    lda #0
    sta nmi_lock
nmi_end:
    ; restore registers and return
    pla
    tay
    pla
    tax
    pla
    rti

.endproc 

.proc irq
    rti
.endproc 

; ppu_update: waits until next NMI, turns rendering on (if not already), uploads OAM, palette, and nametable update to PPU
.proc ppu_update
    lda #1
    sta nmi_ready
    wait:
        lda nmi_ready
        bne wait
    rts
.endproc

; ppu_skip: waits until next NMI, does not update PPU
.proc ppu_skip
    lda nmi_count
    wait:
        cmp nmi_count
        beq wait
    rts
.endproc 

; ppu_off: waits until next NMI, turns rendering off (now safe to write PPU directly via $2007)
.proc ppu_off
    lda #2
    sta nmi_ready
    wait:
        lda nmi_ready
        bne wait
    rts
.endproc 

; ppu_address_tile: use with rendering off, sets memory address to tile at x/Y, ready for a $2007 write
;   Y =  0- 31 nametable $2000
;   Y = 32- 63 nametable $2400
;   Y = 64- 95 nametable $2800
;   Y = 96-127 nametable $2C00
.proc ppu_address_tile
    lda $2002 ; reset latch
    tya
    lsr
    lsr
    lsr
    ora #$20 ; high bits of Y + $20
    sta $2006
    tya
    asl
    asl
    asl
    asl
    asl
    sta r0
    txa
    ora r0
    sta $2006 ; low bits of Y + x
    rts
.endproc 

;
; gamepad
;

PAD_A      = $01
PAD_B      = $02
PAD_SELECT = $04
PAD_START  = $08
PAD_U      = $10
PAD_D      = $20
PAD_L      = $40
PAD_R      = $80

.proc gamepad_poll
    ; strobe the gamepad to latch current button state
    lda #1
    sta $4016
    lda #0
    sta $4016
    ; read 8 bytes from the interface at $4016
    ldx #8
    gamepad_loop:
        pha
        lda $4016
        ; combine low two bits and store in carry bit
        and #%00000011
        cmp #%00000001
        pla
        ; rotate carry into gamepad variable
        ror
        dex
        bne gamepad_loop
    sta gamepad
    rts
.endproc 

.proc gamepad_poll_dpcm_safe
    
    lda gamepad
    sta gamepad_previous
    jsr gamepad_poll
    reread:
        lda gamepad
        pha
        jsr gamepad_poll
        pla
        cmp gamepad
        bne reread

    toggle:
    eor gamepad_previous
    and gamepad
    sta gamepad_pressed

    rts

.endproc

.if ::FAMISTUDIO_EXP_FDS

disk_id:
    .byte $ff, $ff, $ff, $ff, $ff, $ff, $00, $00, $00, $00
load_list:
    .byte $01, $ff, $ff
loading_text: ; Loading....
    .byte $ff, $ff, $ff, $ff, $ff, $ff, $ff, $ff, $ff, $0b, $28, $1a, $1d, $22, $27, $20, $3e, $3e, $3e, $ff, $ff, $ff, $ff, $ff, $ff, $ff, $ff, $ff

.endif


.proc play_song

    text_ptr = p0
    song_idx_mul_32 = r4
    temp_x = r5

    ; each song table entry is 32-bytes.
    asl
    asl
    asl
    asl
    asl
    sta song_idx_mul_32
    tax

.if ::FAMISTUDIO_EXP_FDS
    
    stx temp_x
    jsr famistudio_music_stop
    jsr famistudio_update
    jsr update_all_equalizers
    jsr ppu_update

    ; Write "Loading..." text.
    lda #<loading_text
    sta text_ptr+0
    lda #>loading_text
    sta text_ptr+1

    ldx #2
    ldy #11
    jsr draw_text
    jsr ppu_update

    ; Load song + DPCM if used.
    ldx temp_x
    lda song_fds_file, x
    sta load_list+0

    ; We use the "song_flags" field to store the DPCM file index (0 = no DPCM)
    lda song_flags, x
    beq no_dpcm
    sta load_list+1
    jmp load_file

no_dpcm:
    lda #$ff
    sta load_list+1

load_file:
    ; Kick off file loading.
    jsr fds_bios_load_files
    .word disk_id
    .word load_list
    bne done

load_success:
    .import __SONG_RUN__
    ldy #>__SONG_RUN__
    ldx #<__SONG_RUN__

.else

    ; Map 2 consecutive 8KB pages from the song start page.
    lda #6
    ldy song_page_start, x
    sta $8000
    sty $8001       
    lda #7
    iny
    sta $8000
    sty $8001       

    ; Load song pointer
    ldy song_addr_start+1, x ; hi-byte
    lda song_addr_start+0, x ; lo-byte
    tax

.endif

.if ::FAMISTUDIO_CFG_PAL_SUPPORT
    lda #0
.else
    lda #1 ; NTSC
.endif  
    jsr famistudio_init
    
    lda #0
    jsr famistudio_music_play

    ;update title.
    lda #<song_name
    clc
    adc song_idx_mul_32
    sta text_ptr+0
    lda #>song_name
    adc #0
    sta text_ptr+1

    ldx #2
    ldy #11
    jsr draw_text
    jsr ppu_update

done:
    rts

.endproc 

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
    .byte $01, $02, $00, $02, $02, $02

; a = channel to update
.proc update_equalizer
    
    pos_x = r0
    color_offset = r1

    tay
    lda equalizer_color_lookup, y
    sta color_offset
    tya

    ; compute x position.
    asl
    asl
    sta pos_x

    ; compute lookup index.
    lda famistudio_chn_note_counter, y
    asl
    asl
    tay

    ; compute 2 addresses
    ldx nmt_col_update_len
    lda #$22
    sta nmt_col_update,x
    sta nmt_col_update+7,x
.if ::FAMISTUDIO_EXP_FDS    
    lda #$45
.else
    lda #$47
.endif
    clc
    adc pos_x
    sta nmt_col_update+1,x
    adc #1
    sta nmt_col_update+8,x
    lda #4
    sta nmt_col_update+2,x
    sta nmt_col_update+9,x

    lda equalizer_lookup, y
    adc color_offset
    sta nmt_col_update+3,x
    sta nmt_col_update+10,x
    lda equalizer_lookup+1, y
    adc color_offset
    sta nmt_col_update+4,x
    sta nmt_col_update+11,x
    lda equalizer_lookup+2, y
    adc color_offset
    sta nmt_col_update+5,x
    sta nmt_col_update+12,x
    lda equalizer_lookup+3, y
    adc color_offset
    sta nmt_col_update+6,x
    sta nmt_col_update+13,x
    
    lda #14
    adc nmt_col_update_len
    sta nmt_col_update_len 

    rts

.endproc

.proc update_all_equalizers

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
.if ::FAMISTUDIO_EXP_FDS
    lda #5
    jsr update_equalizer
.endif

    rts

.endproc

.proc main

    jsr ppu_off

    ldx #0
    palette_loop:
        lda default_palette, x
        sta palette, x
        sta palette+16, x
        inx
        cpx #16
        bcc palette_loop
    
    jsr setup_background
    jsr ppu_update

    lda #0 ; song zero.
    sta song_index
    jsr play_song

loop:

    jsr gamepad_poll_dpcm_safe
    
    check_right:
        lda gamepad_pressed
        and #PAD_R
        beq check_left

        ; dont go beyond last song.
        lda song_index
        cmp max_song
        beq draw

        ; next song.
        clc
        adc #1
        sta song_index
        jsr play_song
        jmp draw_done ; Intentionally skipping equalizer update to keep NMI update small.

    check_left:
        lda gamepad_pressed
        and #PAD_L
        beq draw

        ; dont go below zero
        lda song_index
        beq draw

        sec
        sbc #1
        sta song_index
        jsr play_song
        jmp draw_done ; Intentionally skipping equalizer update to keep NMI update small.

draw:

    jsr famistudio_update
    jsr update_all_equalizers

draw_done:

    jsr ppu_update
    jmp loop

.endproc 

; Shiru's code.
; x = lo byte of RLE data addr
; y = hi byte of RLE data addr
.proc rle_decompress

    rle_lo   = r0
    rle_high = r1
    rle_tag  = r2
    rle_byte = r3

    stx rle_lo
    sty rle_high
    ldy #0
    jsr rle_read_byte
    sta rle_tag
loop:
    jsr rle_read_byte
    cmp rle_tag
    beq is_rle
    sta $2007
    sta rle_byte
    bne loop
is_rle:
    jsr rle_read_byte
    cmp #0
    beq done
    tax
    lda rle_byte
rle_loop:
    sta $2007
    dex
    bne rle_loop
    beq loop
done: ;.4
    rts

.endproc

.proc rle_read_byte

    rle_lo   = r0
    rle_high = r1

    lda (rle_lo),y
    inc rle_lo
    bne done
    inc rle_high
done:
    rts

.endproc

; Draws text with rendering on.
; x/y = tile position
; p0  = pointer to text data.
.proc draw_text

    temp_x = r2
    temp   = r3
    
    stx temp_x
    ldx nmt_row_update_len
    tya
    lsr
    lsr
    lsr
    ora #$20 ; high bits of Y + $20
    sta nmt_row_update,x
    inx
    tya
    asl
    asl
    asl
    asl
    asl
    sta temp
    lda temp_x
    ora temp
    sta nmt_row_update,x
    inx
    lda #28 ; all our strings have 28 characters.
    sta nmt_row_update,x
    inx

    ldy #0
    text_loop:
        lda (p0),y
        sta nmt_row_update,x
        inx
        iny
        cpy #28
        bne text_loop


    stx nmt_row_update_len
    rts

.endproc

.proc setup_background

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

    ; Draw title
    ldx #2
    ldy #7
    jsr ppu_address_tile

    ldy #0
    name_loop:
        lda project_name,y
        sta $2007
        iny
        cpy #28
        bne name_loop

    ; Draw author
    ldx #2
    ldy #9
    jsr ppu_address_tile

    ldy #0
    author_loop:
        lda project_author,y
        sta $2007
        iny
        cpy #28
        bne author_loop

    rts

.endproc
