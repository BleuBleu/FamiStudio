; This file is for the FamiStudio Sound Engine and was generated by FamiStudio

.export _sounds=sounds

sounds:
	.word @ntsc
	.word @ntsc
@ntsc:
	.word @sfx_ntsc_megamanhit
	.word @sfx_ntsc_mushroom

@sfx_ntsc_megamanhit:
	.byte $84,$b3,$85,$04,$83,$ff,$8a,$0a,$89,$3f,$01,$84,$f3,$8a,$0b,$01
	.byte $84,$32,$85,$05,$89,$f0,$01,$83,$f0,$01,$84,$07,$85,$01,$83,$f8
	.byte $8a,$09,$89,$37,$01,$84,$2d,$8a,$08,$89,$3f,$01,$84,$53,$8a,$07
	.byte $01,$84,$79,$8a,$06,$01,$84,$9f,$8a,$05,$01,$84,$c5,$8a,$04,$01
	.byte $84,$eb,$8a,$03,$01,$84,$11,$85,$02,$8a,$02,$01,$84,$37,$8a,$01
	.byte $01,$84,$5c,$8a,$00,$01,$8a,$0f,$01,$83,$f0,$00
@sfx_ntsc_mushroom:
	.byte $84,$d5,$85,$00,$83,$7d,$89,$f0,$02,$84,$1c,$85,$01,$02,$84,$d5
	.byte $85,$00,$02,$84,$a9,$02,$84,$8e,$02,$84,$6a,$02,$84,$8e,$02,$84
	.byte $0c,$85,$01,$02,$84,$d5,$85,$00,$02,$84,$b3,$02,$84,$86,$02,$84
	.byte $b3,$02,$84,$86,$02,$84,$6a,$02,$84,$59,$02,$84,$42,$02,$84,$59
	.byte $02,$84,$ef,$02,$84,$bd,$02,$84,$9f,$02,$84,$77,$02,$84,$9f,$02
	.byte $84,$77,$02,$84,$5e,$02,$84,$4f,$02,$84,$3b,$02,$84,$4f,$01,$00

.export sounds
