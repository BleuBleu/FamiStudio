; Simple FamiStudio ROM to play music on actual hardware.
; Based off Brad's (rainwarrior.ca) CA65 template, both the regular and FDS version.

FAMISTUDIO_VERSION_MAJOR  = 4
FAMISTUDIO_VERSION_MINOR  = 1
FAMISTUDIO_VERSION_HOTFIX = 0

; Enable all features.
FAMISTUDIO_CFG_EXTERNAL                  = 1
FAMISTUDIO_CFG_SMOOTH_VIBRATO            = 1
FAMISTUDIO_CFG_DPCM_SUPPORT              = 1
FAMISTUDIO_CFG_EQUALIZER                 = 1

; FIXME : EPSM is now too big. Disabling a bunch of features until Perkka can reduce size
; or create a custom config for it.
.ifndef FAMISTUDIO_EXP_EPSM
    FAMISTUDIO_USE_VOLUME_TRACK              = 1
    FAMISTUDIO_USE_VOLUME_SLIDES             = 1
    FAMISTUDIO_USE_PITCH_TRACK               = 1
    FAMISTUDIO_USE_SLIDE_NOTES               = 1
    FAMISTUDIO_USE_NOISE_SLIDE_NOTES         = 1
    FAMISTUDIO_USE_VIBRATO                   = 1
    FAMISTUDIO_USE_ARPEGGIO                  = 1
    FAMISTUDIO_USE_DUTYCYCLE_EFFECT          = 1
    FAMISTUDIO_USE_DELTA_COUNTER             = 1
    FAMISTUDIO_USE_RELEASE_NOTES             = 1
    FAMISTUDIO_USE_PHASE_RESET               = 1
    FAMISTUDIO_USE_INSTRUMENT_EXTENDED_RANGE = 1
.endif

.ifdef FAMISTUDIO_EXP_FDS
    FAMISTUDIO_USE_FDS_AUTOMOD           = 1
.else
    FAMISTUDIO_USE_DPCM_EXTENDED_RANGE   = 1
    FAMISTUDIO_USE_DPCM_BANKSWITCHING    = 1
.endif

.ifdef FAMISTUDIO_USE_FAMITRACKER_TEMPO
    FAMISTUDIO_USE_FAMITRACKER_DELAYED_NOTES_OR_CUTS = 1
.endif

.define FAMISTUDIO_CA65_ZP_SEGMENT   ZEROPAGE
.define FAMISTUDIO_CA65_RAM_SEGMENT  RAM
.define FAMISTUDIO_CA65_CODE_SEGMENT CODE

.include "../../SoundEngine/famistudio_ca65.s"

.segment "ZEROPAGE"
nmi_lock:           .res 1 ; prevents NMI re-entry
nmi_count:          .res 1 ; is incremented every NMI
nmi_ready:          .res 1 ; set to 1 to push a PPU frame update, 2 to turn rendering off next NMI
scroll_x:           .res 1 ; x scroll position
scroll_y:           .res 1 ; y scroll position
scroll_nmt:         .res 1 ; nametable select (0-3 = $2000,$2400,$2800,$2C00)
gamepad:            .res 1
gamepad_previous:   .res 1
gamepad_pressed:    .res 1
song_index:         .res 1
nmt_update_mode:    .res 1   ; update "mode", 0 = nothing to do, 1 = column mode, 2 = row mode + palettes
nmt_update_data:    .res 128 ; nametable update entry buffer for PPU update
nmt_update_len:     .res 1 ; number of bytes in nmt_update_data buffer
palette:            .res 32  ; palette buffer for PPU update

; General purpose temporary vars.
r0: .res 1
r1: .res 1
r2: .res 1
r3: .res 1
r4: .res 1
r5: .res 1

; General purpose pointers.
p0: .res 2
p1: .res 2

.segment "RAM"

.segment "OAM"
oam: .res 256        ; sprite OAM data to be uploaded by DMA

.segment "HEADER"

.if FAMISTUDIO_EXP_VRC6
INES_MAPPER = 24 ; VRC6 mapper.
.elseif FAMISTUDIO_EXP_VRC7
INES_MAPPER = 85 ; VRC7 mapper.
.elseif FAMISTUDIO_EXP_S5B
INES_MAPPER = 69 ; FME7 mapper.
.elseif FAMISTUDIO_EXP_N163
INES_MAPPER = 19 ; N163 mapper.
.else
INES_MAPPER = 5  ; MMC5 mapper.
.endif

INES_MIRROR = 1 ; 0 = horizontal mirroring, 1 = vertical mirroring

.if FAMISTUDIO_EXP_FDS
    .byte 'F','D','S',$1a
    .byte 1 ; side count
.else
    .byte 'N', 'E', 'S', $1A ; ID
    .byte $02 ; 16k PRG bank count
    .byte $01 ; 8k CHR bank count
    .byte INES_MIRROR | ((INES_MAPPER & $f) << 4)
.if FAMISTUDIO_EXP_EPSM    
    .byte (INES_MAPPER & %11110000) | (%00001011) ; ines v2 + extended console type.
    .byte $0, $0, $0, $0, $0, $4, $0, $0 ; padding
.else
    .byte (INES_MAPPER & %11110000)
    .byte $0, $0, $0, $0, $0, $0, $0, $0 ; padding
.endif    
.endif

.if FAMISTUDIO_EXP_FDS

; FDS File headers
FILE_COUNT = 6

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
.if FAMISTUDIO_EXP_VRC6
.incbin "song_vrc6.bin" ; VRC6 debug song, Another Winter
.elseif FAMISTUDIO_EXP_VRC7
.incbin "song_vrc7.bin" ; VRC7 debug song, Lagrange Point
.elseif FAMISTUDIO_EXP_MMC5
.incbin "song_mmc5.bin" ; MMC5 debug song, Temple Raiders
.elseif FAMISTUDIO_EXP_N163
.incbin "song_n163_4ch.bin" ; N163 debug song, Megami Tensei II (4 channels)
.elseif FAMISTUDIO_EXP_S5B
.incbin "song_s5b.bin" ; S5B debug song, Disco Decent
.elseif FAMISTUDIO_EXP_EPSM
.incbin "song_epsm.bin" ; EPSM debug song, Sonic 1 title theme
.elseif !FAMISTUDIO_EXP_FDS
.incbin "song.bin" ; Debug song, Bloody Tears.
.endif

.segment "DPCM"

.segment "TOC"

; Will be overwritten by FamiStudio.
; General info about the project (author, etc.), 64-bytes.
max_song:        .byte $00
first_dpcm_bank: .byte $00 ; Index of the first DPCM bank. 
fds_file_count:  .byte $06 ; Number of actual file on the disk.
padding:         .res 5    ; reserved
project_name:    .res 28   ; Project name
project_author:  .res 28   ; Project author

.if FAMISTUDIO_EXP_FDS
MAX_SONGS = 12 ; 12 * 32 bytes song header + 64 bytes header = 448 bytes.
.else
MAX_SONGS = 48 ; 48 * 32 bytes song header + 64 bytes header = 1600 bytes.
.endif

; Will be overwritten by FamiStudio.
; Each entry in the song table is 32-bytes:
;  - 28 bytes: song name.
;  - 1 byte: start page of the song
;  - 2 byte: start address of the song.
;  - 1 byte: song flags (uses DPCM or not)
song_table:
song_name:       .res  28
.if FAMISTUDIO_EXP_FDS
song_fds_file:   .byte $00
.else
song_page_start: .byte $00
.endif
song_flags:      .byte $00
song_addr_start: .word $8000

; the remaining songs.
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
.elseif FAMISTUDIO_EXP_MMC5
.incbin "rom_mmc5.rle"
.elseif FAMISTUDIO_EXP_S5B
.incbin "rom_s5b.rle"
.elseif FAMISTUDIO_EXP_N163
.incbin "rom_n163.rle"
.elseif FAMISTUDIO_EXP_VRC6
.incbin "rom_vrc6.rle"
.elseif FAMISTUDIO_EXP_VRC7
.incbin "rom_vrc7.rle"
.elseif FAMISTUDIO_EXP_EPSM
.incbin "rom_epsm.rle"
.else
.incbin "rom.rle"
.endif

default_palette:
.incbin "rom.pal"

.if FAMISTUDIO_EXP_VRC6

    VRC6_PRG_SELECT_8000 = $8000
    VRC6_PRG_SELECT_C000 = $C000

    VRC6_CHR_SELECT_0000 = $D000
    VRC6_CHR_SELECT_0400 = $D001
    VRC6_CHR_SELECT_0800 = $D002
    VRC6_CHR_SELECT_0C00 = $D003
    VRC6_CHR_SELECT_1000 = $E000
    VRC6_CHR_SELECT_1400 = $E001
    VRC6_CHR_SELECT_1800 = $E002
    VRC6_CHR_SELECT_1C00 = $E003

    VRC6_BANK_MODE       = $B003

.elseif FAMISTUDIO_EXP_VRC7
        
    VRC7_PRG_SELECT_8000 = $8000
    VRC7_PRG_SELECT_A000 = $8008
    VRC7_PRG_SELECT_C000 = $9000

    VRC7_CHR_SELECT_0000 = $A000
    VRC7_CHR_SELECT_0400 = $A008
    VRC7_CHR_SELECT_0800 = $B000
    VRC7_CHR_SELECT_0C00 = $B008
    VRC7_CHR_SELECT_1000 = $C000
    VRC7_CHR_SELECT_1400 = $C008
    VRC7_CHR_SELECT_1800 = $D000
    VRC7_CHR_SELECT_1C00 = $D008

.elseif FAMISTUDIO_EXP_N163

    N163_CHR_SELECT_0000 = $8000
    N163_CHR_SELECT_0400 = $8800
    N163_CHR_SELECT_0800 = $9000
    N163_CHR_SELECT_0C00 = $9800
    N163_CHR_SELECT_1000 = $A000
    N163_CHR_SELECT_1400 = $A800
    N163_CHR_SELECT_1800 = $B000
    N163_CHR_SELECT_1C00 = $B800
    
    N163_PRG_SELECT_8000 = $E000
    N163_PRG_SELECT_A000 = $E800
    N163_PRG_SELECT_C000 = $F000

.elseif FAMISTUDIO_EXP_S5B

    FME7_COMMAND = $8000
    FME7_PARAM   = $a000

.elseif !FAMISTUDIO_EXP_FDS

    MMC5_PRG_MODE = $5100
    MMC5_CHR_MODE = $5101
    MMC5_NMT_MAP  = $5105

    MMC5_PRG_SELECT_8000 = $5114
    MMC5_PRG_SELECT_A000 = $5115
    MMC5_PRG_SELECT_C000 = $5116
    MMC5_PRG_SELECT_E000 = $5117

    MMC5_CHR_SELECT_0000 = $5120
    MMC5_CHR_SELECT_0400 = $5121
    MMC5_CHR_SELECT_0800 = $5122
    MMC5_CHR_SELECT_0C00 = $5123
    MMC5_CHR_SELECT_1000 = $5124
    MMC5_CHR_SELECT_1400 = $5125
    MMC5_CHR_SELECT_1800 = $5126
    MMC5_CHR_SELECT_1C00 = $5127

    MMC5_ROM_FLAGS = $80

.endif

.proc reset

    .if ::FAMISTUDIO_EXP_FDS

        ; set FDS to use vertical mirroring
        lda $fa
        and #%11110111
        sta $4025

    .elseif ::FAMISTUDIO_EXP_VRC6

        lda #0
        sta VRC6_BANK_MODE
        lda #0
        sta VRC6_CHR_SELECT_0000
        lda #1
        sta VRC6_CHR_SELECT_0400
        lda #2
        sta VRC6_CHR_SELECT_0800
        lda #3
        sta VRC6_CHR_SELECT_0C00
        lda #4
        sta VRC6_CHR_SELECT_1000
        lda #5
        sta VRC6_CHR_SELECT_1400
        lda #6
        sta VRC6_CHR_SELECT_1800
        lda #7
        sta VRC6_CHR_SELECT_1C00

    .elseif ::FAMISTUDIO_EXP_VRC7

        lda #0
        sta VRC7_CHR_SELECT_0000
        lda #1
        sta VRC7_CHR_SELECT_0400
        lda #2
        sta VRC7_CHR_SELECT_0800
        lda #3
        sta VRC7_CHR_SELECT_0C00
        lda #4
        sta VRC7_CHR_SELECT_1000
        lda #5
        sta VRC7_CHR_SELECT_1400
        lda #6
        sta VRC7_CHR_SELECT_1800
        lda #7
        sta VRC7_CHR_SELECT_1C00

    .elseif ::FAMISTUDIO_EXP_S5B

        ; Setup FME7 CHR banks.
        ldx #0
        bank_loop:  
            stx FME7_COMMAND
            stx FME7_PARAM
            inx
            cpx #8
            bne bank_loop

    .elseif ::FAMISTUDIO_EXP_N163

        lda #0
        sta N163_CHR_SELECT_0000
        lda #1
        sta N163_CHR_SELECT_0400
        lda #2
        sta N163_CHR_SELECT_0800
        lda #3
        sta N163_CHR_SELECT_0C00
        lda #4
        sta N163_CHR_SELECT_1000
        lda #5
        sta N163_CHR_SELECT_1400
        lda #6
        sta N163_CHR_SELECT_1800
        lda #7
        sta N163_CHR_SELECT_1C00

    .else

        lda #3
        sta MMC5_PRG_MODE
        lda #3
        sta MMC5_CHR_MODE
        lda #$44 ; Vertical mirrorring.
        sta MMC5_NMT_MAP

        ; TODO : This could be a loop.
        lda #0
        sta MMC5_CHR_SELECT_0000
        lda #1
        sta MMC5_CHR_SELECT_0400
        lda #2
        sta MMC5_CHR_SELECT_0800
        lda #3
        sta MMC5_CHR_SELECT_0C00
        lda #4
        sta MMC5_CHR_SELECT_1000
        lda #5
        sta MMC5_CHR_SELECT_1400
        lda #6
        sta MMC5_CHR_SELECT_1800
        lda #7
        sta MMC5_CHR_SELECT_1C00

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

; nametable update
nmt_update:
    lda nmt_update_mode 
    bne do_update
    jmp update_done
    do_update:
        ldx #0
        cpx nmt_update_len
        beq palettes
        asl
        asl
        ora #%10000000
        sta $2000
        ldx #0
        nmt_update_loop:
            lda nmt_update_data, x
            inx
            sta $2006
            lda nmt_update_data, x
            inx
            sta $2006
            ldy nmt_update_data, x
            inx
            col_loop:
                lda nmt_update_data, x
                inx
                sta $2007
                dey
                bne col_loop
            cpx nmt_update_len
            bcc nmt_update_loop

; palettes
palettes:
    lda nmt_update_mode
    cmp #2
    beq palettes_need_update
    jmp update_done
    palettes_need_update:
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

update_done:
    ; Clear update mode.
    lda #0 
    sta nmt_update_mode
    sta nmt_update_len

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

version_text: ; 
    .byte $34 + FAMISTUDIO_VERSION_MAJOR, $3e, $34 + FAMISTUDIO_VERSION_MINOR, $3e, $34 + FAMISTUDIO_VERSION_HOTFIX

.if FAMISTUDIO_EXP_VRC6 || FAMISTUDIO_EXP_VRC7 || FAMISTUDIO_EXP_S5B || FAMISTUDIO_EXP_N163 || FAMISTUDIO_EXP_EPSM
    TEXT_OFFSET_Y = 0
.else
    TEXT_OFFSET_Y = 1
.endif

.proc play_song

    song_ptr = p1
    text_ptr = p0

    ; each song table entry is 32-bytes.
    ldy #0
    sty song_ptr+1
    asl
    asl ; Less than 64 max song, first 2 shift cant overflow
    asl
    rol song_ptr+1
    asl
    rol song_ptr+1
    asl
    rol song_ptr+1
    sta song_ptr+0

    lda #<song_table
    adc song_ptr+0
    sta song_ptr+0
    sta text_ptr+0
    lda #>song_table
    adc song_ptr+1
    sta song_ptr+1
    sta text_ptr+1

.if ::FAMISTUDIO_EXP_FDS
    
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
    ldy #(10+TEXT_OFFSET_Y)
    jsr draw_text
    jsr ppu_update

    ; Load song + DPCM if used.
    ldy #28
    lda (song_ptr), y
    sta load_list+0

    ; We use the "song_flags" field to store the DPCM file index (-1 = no DPCM)
    iny
    lda (song_ptr), y
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

    lda song_ptr+0
    sta text_ptr+0
    lda song_ptr+1
    sta text_ptr+1    

.else

    ; Map 2 consecutive 8KB pages from the song start page.
    .if ::FAMISTUDIO_EXP_VRC7

        ldy #28
        lda (song_ptr), y
        tax
        stx VRC7_PRG_SELECT_8000
        inx
        stx VRC7_PRG_SELECT_A000
        ldx first_dpcm_bank
        stx VRC7_PRG_SELECT_C000

    .elseif ::FAMISTUDIO_EXP_S5B

        ; Commands $9, $A, $B control PRG bank at $8000, $a000 and $c000 respectively.
        ldx #9
        stx FME7_COMMAND
        ldy #28
        lda (song_ptr), y
        sta FME7_PARAM
        inx
        clc
        adc #1
        stx FME7_COMMAND
        sta FME7_PARAM
        inx        
        stx FME7_COMMAND
        lda first_dpcm_bank
        sta FME7_PARAM     

    .elseif ::FAMISTUDIO_EXP_N163

        ldy #28
        lda (song_ptr), y
        sta N163_PRG_SELECT_8000
        clc
        adc #1
        sta N163_PRG_SELECT_A000
        lda first_dpcm_bank
        sta N163_PRG_SELECT_C000

    .elseif ::FAMISTUDIO_EXP_VRC6

        ; VRC6 uses 16KB pages, so just one page to map.
        ldy #28
        lda (song_ptr), y
        sta VRC6_PRG_SELECT_8000
        lda first_dpcm_bank
        sta VRC6_PRG_SELECT_C000

    .else

        ldy #28
        lda (song_ptr), y
        ora #MMC5_ROM_FLAGS
        sta MMC5_PRG_SELECT_8000
        clc
        adc #1
        sta MMC5_PRG_SELECT_A000
        lda first_dpcm_bank
        ora #MMC5_ROM_FLAGS
        sta MMC5_PRG_SELECT_C000    

    .endif

    ; Load song pointer
    ldy #30
    lda (song_ptr), y    
    tax
    iny
    lda (song_ptr), y    
    tay

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
    ldx #2
    ldy #(10+TEXT_OFFSET_Y)
    jsr draw_text
    jsr ppu_update

done:
    rts

.endproc 

.if FAMISTUDIO_EXP_VRC6
    NUM_EQUALIZERS = 8
.elseif FAMISTUDIO_EXP_VRC7
    NUM_EQUALIZERS = 11
.elseif FAMISTUDIO_EXP_MMC5
    NUM_EQUALIZERS = 7
.elseif FAMISTUDIO_EXP_S5B
    NUM_EQUALIZERS = 8
.elseif FAMISTUDIO_EXP_EPSM
    NUM_EQUALIZERS = 14 ; We dont display the rhythm channels
.elseif FAMISTUDIO_EXP_N163
    NUM_EQUALIZERS = 5 + FAMISTUDIO_EXP_N163_CHN_CNT
.elseif FAMISTUDIO_EXP_FDS
    NUM_EQUALIZERS = 6
.else
    NUM_EQUALIZERS = 5
.endif

; Position and number of equalizers (well VU meter i guess) for each expansion
equalizer_ppu_addr_lo_lookup:
.if FAMISTUDIO_EXP_VRC6 || FAMISTUDIO_EXP_VRC7 || FAMISTUDIO_EXP_N163 || FAMISTUDIO_EXP_S5B || FAMISTUDIO_EXP_EPSM 
    .byte $87 ; Square 1
    .byte $8b ; Square 2
    .byte $8f ; Triangle
    .byte $93 ; Noise
    .byte $97 ; DPCM
.endif
.if FAMISTUDIO_EXP_VRC6
    .byte $8b ; VRC6 Square 1
    .byte $8f ; VRC6 Square 2
    .byte $93 ; VRC6 Saw
.elseif FAMISTUDIO_EXP_VRC7
    .byte $85 ; FM1
    .byte $89 ; FM2
    .byte $8d ; FM3
    .byte $91 ; FM4
    .byte $95 ; FM5
    .byte $99 ; FM6
.elseif FAMISTUDIO_EXP_N163
    .byte $85 ; Wavetable 1
    .byte $88 ; Wavetable 2
    .byte $8b ; Wavetable 3
    .byte $8e ; Wavetable 4
    .byte $91 ; Wavetable 5
    .byte $94 ; Wavetable 6
    .byte $97 ; Wavetable 7
    .byte $9a ; Wavetable 8
.elseif FAMISTUDIO_EXP_S5B    
    .byte $8b ; S5B Square 1
    .byte $8f ; S5B Square 2
    .byte $93 ; S5B Square 3
.elseif FAMISTUDIO_EXP_EPSM
    .byte $83 ; EPSM Square 1
    .byte $86 ; EPSM Square 2
    .byte $89 ; EPSM Square 3
    .byte $8c ; EPSM FM1
    .byte $8f ; EPSM FM2
    .byte $92 ; EPSM FM3
    .byte $95 ; EPSM FM4
    .byte $98 ; EPSM FM5
    .byte $9b ; EPSM FM6
.elseif FAMISTUDIO_EXP_FDS
    .byte $45 ; Square 1
    .byte $49 ; Square 2
    .byte $4d ; Triangle
    .byte $51 ; Noise
    .byte $55 ; DPCM
    .byte $59 ; FDS
.elseif FAMISTUDIO_EXP_MMC5
    .byte $43 ; Square 1
    .byte $47 ; Square 2
    .byte $4b ; Triangle
    .byte $4f ; Noise
    .byte $53 ; DPCM
    .byte $57 ; MMC5 Square 1
    .byte $5b ; MMC5 Square 2
.else
    .byte $47 ; Square 1
    .byte $4b ; Square 2
    .byte $4f ; Triangle
    .byte $53 ; Noise
    .byte $57 ; DPCM
.endif    

equalizer_ppu_addr_hi_lookup:
.if FAMISTUDIO_EXP_VRC6 || FAMISTUDIO_EXP_VRC7 || FAMISTUDIO_EXP_S5B || FAMISTUDIO_EXP_N163 || FAMISTUDIO_EXP_EPSM
    .byte $21 ; Square 1
    .byte $21 ; Square 2
    .byte $21 ; Triangle
    .byte $21 ; Noise
    .byte $21 ; DPCM
.else
    .byte $22 ; Square 1
    .byte $22 ; Square 2
    .byte $22 ; Triangle
    .byte $22 ; Noise
    .byte $22 ; DPCM
.endif
    .byte $22
    .byte $22
    .byte $22
    .byte $22
    .byte $22
    .byte $22
    .byte $22
    .byte $22
    .byte $22

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
    .byte $00, $01, $01
    .byte $02, $00, $01
    .byte $02, $00, $01
    .byte $01, $02, $00

; x = channel to update
.proc update_equalizer
    
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

.endproc

.proc update_all_equalizers

    lda nmt_update_mode
    bne equalizers_done ; Dont allow update if we already have an update pending.

    ldx #0
    update_equalizer_loop:
        jsr update_equalizer
        inx
        cpx #NUM_EQUALIZERS
        bne update_equalizer_loop

    lda #1
    sta nmt_update_mode

    equalizers_done:
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

    ; Force palette update.
    lda #2
    sta nmt_update_mode

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
    sta temp
    lda temp_x
    ora temp
    sta nmt_update_data,x
    inx
    lda #28 ; all our strings have 28 characters.
    sta nmt_update_data,x
    inx

    ldy #0
    text_loop:
        lda (p0),y
        sta nmt_update_data,x
        inx
        iny
        cpy #28
        bne text_loop


    stx nmt_update_len
    lda #2
    sta nmt_update_mode
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

    ; Draw title
    ldx #2
    ldy #(6+TEXT_OFFSET_Y)
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
    ldy #(8+TEXT_OFFSET_Y)
    jsr ppu_address_tile

    ldy #0
    author_loop:
        lda project_author,y
        sta $2007
        iny
        cpy #28
        bne author_loop

    ; Draw version
    ldx #25
    ldy #27
    jsr ppu_address_tile

    ldy #0
    version_loop:
        lda version_text,y
        sta $2007
        iny
        cpy #5
        bne version_loop

    rts

.endproc

.if FAMISTUDIO_USE_DPCM_BANKSWITCHING
.proc famistudio_dpcm_bank_callback
    clc
    .if ::FAMISTUDIO_EXP_VRC7
        adc first_dpcm_bank
        sta VRC7_PRG_SELECT_C000
    .elseif ::FAMISTUDIO_EXP_S5B
        ldx #$B ; $B control PRG bank $c000
        stx FME7_COMMAND
        adc first_dpcm_bank
        sta FME7_PARAM     
    .elseif ::FAMISTUDIO_EXP_N163
        adc first_dpcm_bank
        sta N163_PRG_SELECT_C000
    .elseif ::FAMISTUDIO_EXP_VRC6
        adc first_dpcm_bank
        sta VRC6_PRG_SELECT_C000
    .else
        adc first_dpcm_bank
        ora #MMC5_ROM_FLAGS
        sta MMC5_PRG_SELECT_C000    
    .endif
    rts
.endproc
.endif

