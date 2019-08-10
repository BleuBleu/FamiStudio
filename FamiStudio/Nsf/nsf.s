.include "famitone2.s"

.global nsf_init
.global nsf_play

NSF_INIT_ADDR   = $8500
NSF_PLAY_ADDR   = $8600

SONG_TABLE_ADDR = $8700
DPCM_ADDR       = $c000

.align $100
.assert * = NSF_INIT_ADDR, error, "NSF init addr is not what was expected, has the sound engine got bigger?"

; [in] a = song index.
.proc nsf_init

	; Each table entry is 4-bytes:
	;   - start page (1-byte)
	;   - start addr in page starting at $9000 (2-byte)
	;   - flags (1 = uses DPCM)
	
	asl
	asl
	tax
	
	ldy SONG_TABLE_ADDR+0, x

	sty $5FF9
	iny
	sty $5FFa
	iny
	sty $5FFb
	iny

	lda SONG_TABLE_ADDR+3, x
	beq no_samples
	
	has_samples:
		ldy #1
	
	no_samples:
		sty $5FFc
		iny
		sty $5FFd
		iny
		sty $5FFe
		iny
		sty $5FFf
	
	init:

	ldy SONG_TABLE_ADDR+2, x ; hi-byte
	lda SONG_TABLE_ADDR+1, x ; lo-byte
	tax
	lda #1 ; NTSC
	jsr FamiToneInit
	
	lda #0
	jsr FamiToneMusicPlay

	rts

.endproc

.align $100
.assert * = NSF_PLAY_ADDR, error, "NSF play addr is not what was expected, has the sound engine got bigger?"

.proc nsf_play
	jsr FamiToneUpdate
	rts
.endproc

