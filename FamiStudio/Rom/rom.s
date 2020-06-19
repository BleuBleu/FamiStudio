; Simple FamiStudio ROM to play music on actual hardware.
; Based off Brad's (rainwarrior.ca) CA65 template.

.include "../Nsf/famistudio.s"

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

INES_MAPPER = 31 ; 31 = NSF-like mapper.
INES_MIRROR = 1  ; 0 = horizontal mirroring, 1 = vertical mirroring
INES_SRAM   = 0  ; 1 = battery backed SRAM at $6000-7FFF

.byte 'N', 'E', 'S', $1A ; ID
.byte $02 ; 16k PRG bank count
.byte $01 ; 8k CHR bank count
.byte INES_MIRROR | (INES_SRAM << 1) | ((INES_MAPPER & $f) << 4)
.byte (INES_MAPPER & %11110000)
.byte $0, $0, $0, $0, $0, $0, $0, $0 ; padding

.segment "SONG"
.incbin "song.bin" ; Test song, Bloody Tears.

.segment "SONG_TABLE"

; Will be overwritten by FamiStudio.
; General info about the project (author, etc.), 64-bytes.
max_song:        .byte $01
dpcm_page_start: .byte $00
dpcm_page_count: .byte $01 ; Test song has 1 page of DPCM.
padding:         .res 5    ; reserved
project_name:    .res 28   ; Project name
project_author:  .res 28   ; Project author

; Up to 8 songs for now, 256 bytes.
MAX_SONGS = 8

; Will be overwritten by FamiStudio.
; Each entry in the song table is 32-bytes:
;  - 1 byte: start page of the song
;  - 2 byte: start address of the song.
;  - 1 byte: song flags (uses DPCM or not)
;  - 28 bytes: song name.
song_table:
song_page_start: .byte $00
song_addr_start: .word $8222 ; Test song has $222 bytes of DPCM, song is right after.
song_flags:      .byte $01   ; Test song uses DPCM
song_name:       .res  28

; the remaining 7 songs.
.res 32 * (MAX_SONGS - 1)

.segment "TILES"
.incbin "rom.chr"
.incbin "rom.chr"

.segment "VECTORS"
.word nmi
.word reset
.word irq

.segment "CODE"

; Our single screen.
screen_data_rle:
.incbin "rom.rle"

default_palette:
.incbin "rom.pal"
.incbin "rom.pal"

.proc reset

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
		sta $0000, X
		sta $0100, X
		sta $0200, X
		sta $0300, X
		sta $0400, X
		sta $0500, X
		sta $0600, X
		sta $0700, X
		inx
		bne clear_ram_loop
	; place all sprites offscreen at Y=255
	lda #255
	ldx #0
	clear_oam_loop:
		sta oam, X
		inx
		inx
		inx
		inx
		bne clear_oam_loop
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
	beq :+
		jmp nmi_end
	:
	lda #1
	sta nmi_lock
	; increment frame counter
	inc nmi_count
	;
	lda nmi_ready
	bne :+ ; nmi_ready == 0 not ready to update PPU
		jmp ppu_update_end
	:
	cmp #2 ; nmi_ready == 2 turns rendering off
	bne :+
		lda #%00000000
		sta $2001
		ldx #0
		stx nmi_ready
		jmp ppu_update_end
	:
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
		pal_loop:
			lda palette, X
			sta $2007
			inx
			cpx #32
			bne pal_loop

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
	:
		lda nmi_ready
		bne :-
	rts
.endproc

; ppu_skip: waits until next NMI, does not update PPU
.proc ppu_skip
	lda nmi_count
	:
		cmp nmi_count
		beq :-
	rts
.endproc 

; ppu_off: waits until next NMI, turns rendering off (now safe to write PPU directly via $2007)
.proc ppu_off
	lda #2
	sta nmi_ready
	:
		lda nmi_ready
		bne :-
	rts
.endproc 

; ppu_address_tile: use with rendering off, sets memory address to tile at X/Y, ready for a $2007 write
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
	sta $2006 ; low bits of Y + X
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
	:
		pha
		lda $4016
		; combine low two bits and store in carry bit
		and #%00000011
		cmp #%00000001
		pla
		; rotate carry into gamepad variable
		ror
		dex
		bne :-
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

.proc play_song

	text_ptr = p0
	song_idx_mul_32 = r0

	; each song table entry is 32-bytes.
	asl
	asl
	asl
	asl
	asl
	sta song_idx_mul_32
	tax

	; Map the entire range for the song.
	ldy song_page_start, x
	sty $5000
	iny
	sty $5001
	iny
	sty $5002
	iny
	sty $5003
	iny
	sty $5004
	iny
	sty $5005
	iny
	sty $5006

	; If the song uses DPCM, map them as well.	
	lda song_flags, x
	beq samples_none

	ldy dpcm_page_start
	lda dpcm_page_count

	cmp #1
	beq samples_1_pages
	cmp #2
	beq samples_2_pages

samples_3_pages:
	sty $5004
	iny
samples_2_pages:
	sty $5005
	iny
samples_1_pages:
	sty $5006	

samples_none:
	ldy song_addr_start+1, x ; hi-byte
	lda song_addr_start+0, x ; lo-byte
	tax
.if(::FT_PAL_SUPPORT)
	lda #0
.else
	lda #1 ; NTSC
.endif	
	jsr FamiToneInit
	
	lda #0
	jsr FamiToneMusicPlay

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
	.byte $01, $02, $00, $02, $02

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
	lda FT_CHN_NOTE_COUNTER, y
	asl
	asl
	tay

	; compute 2 addresses
	ldx nmt_col_update_len
	lda #$22
	sta nmt_col_update,x
	sta nmt_col_update+7,x
	lda #$47
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

.proc main

	ldx #0
	palette_loop:
		lda default_palette, X
		sta palette, X
		inx
		cpx #32
		bcc palette_loop
	
	jsr setup_background

	lda #0 ; song zero.
	sta song_index
	jsr play_song

	jsr ppu_update

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

	jsr FamiToneUpdate
	
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
