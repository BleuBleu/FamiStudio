.ifdef FAMISTUDIO
.include "famistudio.s"
.else
.include "famitone2.s"
.endif

.segment "HEADER"

; NSF header Placeholder so that the debug info addresses matches.
HEADER : .res 128

.segment "CODE_INIT"

; [in] a = song index.
.proc nsf_init

	; Each table entry is 4-bytes:
	;   - start page (1-byte)
	;   - start addr in page starting at $9000 (2-byte)
	;   - flags (3 low bits = num dpcm pages)
	
.if ::FT_PAL_SUPPORT && ::FT_NTSC_SUPPORT
	mode = FT_TEMP_VAR1
	stx FT_TEMP_VAR1
.endif	

	asl
	asl
	tax
	
	ldy SONG_TABLE+0, x

	; First map the full 0x9000 - 0xf000 to song data.
	sty $5FF9
	iny
	sty $5FFa
	iny
	sty $5FFb
	iny
	sty $5FFc
	iny
	sty $5FFd
	iny
	sty $5FFe
	iny
	sty $5FFf
	
	; Then map the samples at the very end (if 1 page => start at 0xf000, if 2 pages => start at 0xe000, etc.)
	ldy DPCM_PAGE_START
	lda DPCM_PAGE_CNT
	beq samples_none
	
	cmp #1
	beq samples_1_pages
	cmp #2
	beq samples_2_pages
	cmp #3
	beq samples_3_pages

	samples_4_pages:
		sty $5FFc
		iny
	samples_3_pages:
		sty $5FFd
		iny
	samples_2_pages:
		sty $5FFe
		iny
	samples_1_pages:
		sty $5FFf
	samples_none:

	; Load song data and play
	ldy SONG_TABLE+2, x ; hi-byte
	lda SONG_TABLE+1, x ; lo-byte
	tax

.if ::FT_PAL_SUPPORT && ::FT_NTSC_SUPPORT
	lda mode
	eor #1
.elseif ::FT_PAL_SUPPORT
	lda #0 ; PAL
.else
	lda #1 ; NTSC
.endif
	jsr FamiToneInit
	
	lda #0
	jsr FamiToneMusicPlay

	rts

.endproc

.segment "CODE_PLAY"

.proc nsf_play
	jsr FamiToneUpdate
	rts
.endproc

.segment "SONG_DATA"

DPCM_PAGE_START: .res 1
DPCM_PAGE_CNT:   .res 1

; each entry in the song table is 4 bytes
;  - first page of the song (1 byte)
;  - address of the start of the song in page starting at 0x9000 (2 byte)
;  - unused (1-byte)

SONG_TABLE:      .res 4
