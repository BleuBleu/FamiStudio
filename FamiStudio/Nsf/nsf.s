.ifdef FS
.include "famitone2fs.s"
.else
.include "famitone2.s"
.endif

.global nsf_init
.global nsf_play

SONG_TABLE_ADDR = $8c00
DPCM_ADDR       = $c000

.segment "CODE_INIT"

; [in] a = song index.
.proc nsf_init

	; Each table entry is 4-bytes:
	;   - start page (1-byte)
	;   - start addr in page starting at $9000 (2-byte)
	;   - flags (3 low bits = num dpcm pages)
	
	asl
	asl
	tax
	
	ldy SONG_TABLE_ADDR+0, x

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
	ldy #1
	lda SONG_TABLE_ADDR+3, x
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
	ldy SONG_TABLE_ADDR+2, x ; hi-byte
	lda SONG_TABLE_ADDR+1, x ; lo-byte
	tax
	lda #1 ; NTSC
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

