; This file is for the FamiStudio Sound Engine and was generated by FamiStudio
; Required flags for Journey to Silius:
; FAMISTUDIO_USE_RELEASE_NOTES = 1
; FAMISTUDIO_USE_SLIDE_NOTES = 1
; FAMISTUDIO_USE_ARPEGGIO = 1
; FAMISTUDIO_USE_DELTA_COUNTER = 1

music_data_journey_to_silius:
	db 1
	dw @instruments
	dw @samples-4
; 00 : Title Screen
	dw @song0ch0
	dw @song0ch1
	dw @song0ch2
	dw @song0ch3
	dw @song0ch4
	db <(@tempo_env_1_mid), >(@tempo_env_1_mid), 0, 0

@instruments:
	dw @env26,@env12,@env13,@env0 ; 00 : Noise: Hat & Tom
	dw @env2,@env12,@env13,@env0 ; 01 : Noise: Snare
	dw @env9,@env3,@env13,@env0 ; 02 : Triangle: Tom
	dw @env9,@env8,@env13,@env0 ; 03 : Triangle: Tom (Fill)
	dw @env4,@env12,@env17,@env0 ; 04 : Duty 1: Reverb
	dw @env24,@env12,@env13,@env7 ; 05 : Duty 0: Main (Release 1)
	dw @env10,@env12,@env13,@env7 ; 06 : Duty 0: Main (Release 2)
	dw @env9,@env12,@env13,@env0 ; 07 : Triangle: Tom (Plain)
	dw @env24,@env12,@env17,@env7 ; 08 : Duty 1: Main (Release 1)
	dw @env5,@env12,@env14,@env0 ; 09 : Duty 2: Reverb
	dw @env10,@env12,@env17,@env7 ; 0a : Duty 1: Main (Release 2)
	dw @env1,@env12,@env14,@env7 ; 0b : Duty 2: Main
	dw @env20,@env12,@env13,@env7 ; 0c : Duty 0: Sustain (Vol 1)
	dw @env23,@env12,@env13,@env7 ; 0d : Duty 0: Harsh
	dw @env16,@env12,@env13,@env25 ; 0e : Duty 0: Outro
	dw @env22,@env12,@env13,@env0 ; 0f : Duty 0: Outro Slide
	dw @env5,@env12,@env13,@env0 ; 10 : Duty 0: Reverb
	dw @env23,@env12,@env17,@env7 ; 11 : Duty 1: Harsh
	dw @env15,@env12,@env13,@env7 ; 12 : Duty 0: Sustain (Vol 2)
	dw @env11,@env6,@env13,@env0 ; 13 : Noise: Break Snare

@env0:
	db $00,$c0,$7f,$00,$02
@env1:
	db $0e,$c5,$c6,$c6,$ca,$cb,$cc,$cb,$ca,$c9,$c8,$c7,$00,$0b,$c1,$c5,$c4,$c3,$c2,$00,$12
@env2:
	db $00,$ce,$cd,$cc,$ca,$c9,$c7,$c5,$c4,$c2,$c1,$00,$0a
@env3:
	db $c0,$bf,$be,$bd,$bc,$bb,$ba,$b9,$b8,$00,$08
@env4:
	db $00,$c3,$7f,$00,$02
@env5:
	db $04,$c4,$00,$01,$c2,$09,$c1,$00,$06
@env6:
	db $c0,$ba,$c0,$00,$02
@env7:
	db $00,$c0,$07,$c1,$c3,$c6,$c3,$c1,$bf,$bd,$ba,$bd,$bf,$00,$03
@env8:
	db $c0,$bf,$c0,$c1,$00,$03
@env9:
	db $00,$cf,$7f,$00,$02
@env10:
	db $11,$c4,$c6,$c9,$c8,$0e,$c7,$0e,$c6,$0e,$c5,$0e,$c4,$0e,$c3,$00,$0e,$c1,$c5,$c4,$c3,$c2,$00,$15
@env11:
	db $00,$cd,$ce,$cd,$cb,$ca,$c8,$c7,$c5,$c3,$c2,$c1,$00,$0b
@env12:
	db $c0,$7f,$00,$01
@env13:
	db $7f,$00,$00
@env14:
	db $c2,$7f,$00,$00
@env15:
	db $00,$c2,$7f,$00,$02
@env16:
	db $00,$c5,$c9,$c9,$c8,$00,$04
@env17:
	db $c1,$7f,$00,$00
@env18:
	db $c0,$c3,$00,$01
@env19:
	db $c0,$c6,$00,$01
@env20:
	db $00,$c1,$7f,$00,$02
@env21:
	db $c0,$c9,$00,$01
@env22:
	db $00,$c8,$7f,$00,$02
@env23:
	db $07,$ce,$cb,$ca,$c9,$00,$04,$c1,$c5,$c4,$c3,$c2,$c1,$00,$0c
@env24:
	db $11,$c4,$c6,$c9,$c8,$0e,$c7,$0e,$c6,$0e,$c5,$0e,$c4,$0e,$c3,$00,$0e,$c1,$c5,$c4,$c3,$c2,$c1,$00,$16
@env25:
	db $00,$c0,$27,$c1,$c3,$c6,$c3,$c1,$bf,$bd,$ba,$bd,$bf,$00,$03
@env26:
	db $00,$cd,$cc,$c9,$c6,$c3,$00,$05

@samples:
	db $00+<(FAMISTUDIO_DPCM_PTR),$3e,$0c,$40 ; 00 Sunsoft Bass: A# (Pitch:12)
	db $00+<(FAMISTUDIO_DPCM_PTR),$3e,$0f,$40 ; 01 Sunsoft Bass: A# (Pitch:15)
	db $10+<(FAMISTUDIO_DPCM_PTR),$3f,$0d,$40 ; 02 Sunsoft Bass: B (Pitch:13)
	db $10+<(FAMISTUDIO_DPCM_PTR),$3f,$0e,$40 ; 03 Sunsoft Bass: B (Pitch:14)
	db $20+<(FAMISTUDIO_DPCM_PTR),$3f,$09,$40 ; 04 Sunsoft Bass: C (Pitch:9)
	db $20+<(FAMISTUDIO_DPCM_PTR),$3f,$0a,$40 ; 05 Sunsoft Bass: C (Pitch:10)
	db $20+<(FAMISTUDIO_DPCM_PTR),$3f,$0c,$40 ; 06 Sunsoft Bass: C (Pitch:12)
	db $20+<(FAMISTUDIO_DPCM_PTR),$3f,$0e,$40 ; 07 Sunsoft Bass: C (Pitch:14)
	db $20+<(FAMISTUDIO_DPCM_PTR),$3f,$0f,$40 ; 08 Sunsoft Bass: C (Pitch:15)
	db $30+<(FAMISTUDIO_DPCM_PTR),$3e,$08,$40 ; 09 Sunsoft Bass: C# (Pitch:8)
	db $30+<(FAMISTUDIO_DPCM_PTR),$3e,$0a,$40 ; 0a Sunsoft Bass: C# (Pitch:10)
	db $30+<(FAMISTUDIO_DPCM_PTR),$3e,$0d,$40 ; 0b Sunsoft Bass: C# (Pitch:13)
	db $30+<(FAMISTUDIO_DPCM_PTR),$3e,$0e,$40 ; 0c Sunsoft Bass: C# (Pitch:14)
	db $40+<(FAMISTUDIO_DPCM_PTR),$3f,$0a,$40 ; 0d Sunsoft Bass: D (Pitch:10)
	db $40+<(FAMISTUDIO_DPCM_PTR),$3f,$0e,$40 ; 0e Sunsoft Bass: D (Pitch:14)

@tempo_env_1_mid:
	db $03,$05,$80

@song0ch0:
	db $cf, $48, $00, $a5, $88, $19, $91, $1c, $91, $19, $91, $1e, $43, $1f, $81, $43, $20, $9f, $1e, $91
@song0ch0loop:
	db $47, <(@tempo_env_1_mid), >(@tempo_env_1_mid)
@song0ref25:
	db $8a, $14, $af, $45, $87, $16
@song0ref31:
	db $d7, $45, $87, $48, $00, $a5, $88, $19, $91, $1c, $91, $19, $91, $1e, $43, $1f, $81, $43, $20, $9f, $1e, $91, $48, $8a
	db $20, $af, $45, $87, $1e
	db $41, $12
	dw @song0ref31
	db $48
	db $41, $1c
	dw @song0ref25
	db $d7, $45, $87, $48, $a7, $8c
@song0ref73:
	db $17, $91, $45, $91, $16, $87, $45, $87, $17, $91, $45, $91, $17, $91, $48, $93, $45, $91, $16, $91, $45, $91, $16, $87
	db $45, $87, $17, $87, $45, $af, $48, $a7
	db $41, $1e
	dw @song0ref73
	db $41, $1e
	dw @song0ref73
	db $17, $91, $45, $91, $16, $87, $45, $87, $17, $91, $45, $91, $14, $91, $48, $ff, $81, $45, $87, $96
@song0ref131:
	db $19, $91, $48, $89, $45, $87, $19, $87, $45, $87, $1c, $87, $45, $87, $19, $87, $45, $87, $1e, $87, $45, $87, $19, $91
	db $45, $91, $20, $91, $48, $89, $45, $87
@song0ref163:
	db $19, $87, $45, $87, $1e, $87, $45, $87, $19, $87, $45, $87, $1c, $87, $45, $87, $19, $91, $45, $91, $14, $91, $48, $89
	db $45, $87, $14, $87, $45, $87, $17, $87, $45, $87, $14, $87, $45, $87, $17, $87, $45, $87, $1a, $43, $1b, $81, $43, $1c
	db $8b, $1b, $87, $45, $87, $19, $91, $48, $ff, $81, $45, $87
	db $41, $19
	dw @song0ref131
	db $1e, $43, $1f, $81, $43, $20, $8b, $48, $93
	db $41, $21
	dw @song0ref163
	db $19, $87, $45, $87, $1a, $43, $1b, $81, $43, $1c, $8b, $1b, $87, $45, $87, $17, $87, $45, $87, $19, $91, $48, $ff, $95
	db $45, $87, $48
@song0ref265:
	db $1e, $81, $43, $1f, $81, $43, $20, $d9, $1e, $87, $45, $87, $1c, $87, $45, $87, $1b, $91, $48, $c5, $45, $87, $1c, $91
	db $45, $87, $1e, $91, $45, $87, $1e, $43, $1f, $81, $43, $20, $8b, $48, $ff, $8b, $94, $1e, $43, $1f, $81, $43, $20, $8b
	db $48, $93, $1e, $43, $1f, $81, $43, $20, $8b, $22, $91, $45, $91, $90, $23, $9b, $45, $af, $48, $96
	db $41, $1b
	dw @song0ref265
	db $19, $91, $48, $ff, $9f, $48, $93, $a2, $19, $87, $45, $87, $19, $87, $45, $87, $19, $87, $45, $87, $19, $87, $45, $c3
	db $48, $8c
@song0ref362:
	db $19, $c3, $45, $87, $19, $c3, $45, $87, $48, $19, $af, $45, $87, $19, $cd, $45, $91, $48
	db $41, $10
	dw @song0ref362
	db $48
	db $41, $10
	dw @song0ref362
	db $48, $1b, $c3, $45, $87, $1b, $c3, $45, $87, $48, $1b, $af, $45, $87, $1b, $cd, $45, $91, $48, $19, $9b
@song0ref408:
	db $45, $87, $19, $9b, $45, $87, $19, $91, $45, $87, $19, $91, $45, $87, $19, $91, $48, $9d
	db $41, $11
	dw @song0ref408
	db $41, $10
	dw @song0ref408
	db $48, $b1, $45, $87, $17, $c3, $45, $87, $19, $91, $48, $9d
	db $41, $11
	dw @song0ref408
	db $41, $11
	dw @song0ref408
	db $45, $87, $19, $9b, $45, $87, $1b, $91, $45, $87, $20, $91, $45, $87, $20, $91, $48, $ff, $95, $45, $87, $48, $00, $a5
	db $a0
@song0ref475:
	db $19, $91, $1b, $91, $1c, $91, $1e, $91, $19, $91, $1b, $91, $48, $1c, $91, $1e, $91
	db $41, $10
	dw @song0ref475
	db $41, $10
	dw @song0ref475
	db $41, $10
	dw @song0ref475
	db $41, $10
	dw @song0ref475
	db $1b, $91, $1c, $91, $1e, $91, $20, $b9, $48, $23, $91, $25, $a5, $27, $91, $28, $91, $2a, $91, $25, $91, $27, $91, $48
	db $28, $91, $2a, $a5, $2c, $91, $2d, $91, $2f, $91, $2a, $91, $2c, $91, $48, $2d, $91, $2f, $91, $31, $f5, $48, $f7, $43
	db $50, $06, $3d, $30, $a5, $48, $45, $a5, $88, $19, $91, $1c, $91, $19, $91, $1e, $43, $1f, $81, $43, $20, $9f, $1e, $91
	db $42
	dw @song0ch0loop
@song0ch1:
	db $cf
@song0ref580:
	db $90
@song0ref581:
	db $19, $87, $45, $87, $1c, $87, $45, $87, $19, $87, $45, $87, $1e, $43, $1f, $81, $43, $20, $9f, $1e, $87, $45, $87, $1c
	db $9b, $45, $87
@song0ch1loop:
	db $17, $af, $45, $87, $19, $d7, $45, $87
	db $41, $19
	dw @song0ref581
	db $23, $af, $45, $87, $22, $d7, $45, $87
	db $41, $19
	dw @song0ref581
	db $17, $af, $45, $87, $19, $d7, $45, $87
	db $41, $19
	dw @song0ref581
	db $23, $af, $45, $87, $22, $d7, $45, $87, $a7, $8c
@song0ref652:
	db $1c, $91, $45, $91, $1b, $87, $45, $87, $1c, $91, $45, $91, $1c, $91, $93, $45, $91, $1b, $91, $45, $91, $1b, $87, $45
	db $87, $1c, $87, $45, $af, $a7
	db $41, $1e
	dw @song0ref652
	db $41, $1e
	dw @song0ref652
	db $1c, $91, $45, $91, $1b, $87, $45, $87, $1c, $91, $45, $91, $1b, $91, $ff, $95, $45, $87, $a4, $1b, $91, $92, $19, $b9
	db $1c, $91, $19, $91, $1e, $91, $19, $91, $93, $20, $a5, $19, $91, $1e, $91, $19, $91, $1c, $91, $19, $91, $93, $14, $b9
	db $17, $91, $14, $91, $17, $91, $1a, $43, $1b, $81, $43, $1c, $8b, $1b, $91, $19, $ff, $89, $cf, $1c, $91, $19, $91, $1e
	db $91, $19, $91, $93, $1e, $43, $1f, $81, $43, $20, $9f, $19, $91, $1e, $91, $19, $91, $1c, $91, $19, $91, $93, $14, $b9
	db $17, $91, $19, $91, $1a, $43, $1b, $81, $43, $1c, $8b, $1b, $91, $17, $91, $19, $ff, $89, $a7
@song0ref803:
	db $1e, $81, $43, $1f, $81, $43, $20, $d9, $1e, $91, $1c, $91, $1b, $e1, $1c, $9b, $1e, $87, $93, $1e, $43, $1f, $81, $43
	db $20, $ef, $8c, $1b, $91, $89, $45, $87, $1b, $87, $45, $87, $1e, $91, $45, $91, $20, $9b, $45, $87, $98, $20, $a5, $a7
	db $92
	db $41, $11
	dw @song0ref803
	db $19, $ff, $89, $93, $9a, $14, $87, $45, $87, $14, $87, $45, $87, $14, $87, $45, $87, $14, $87, $45, $c3, $94
@song0ref877:
	db $20, $c3, $45, $87, $1e, $c3, $45, $87, $1c, $af, $45, $87, $1e, $cd, $45, $91
	db $41, $10
	dw @song0ref877
	db $41, $10
	dw @song0ref877
	db $21, $c3, $45, $87, $20, $c3, $45, $87, $1e, $af, $45, $87, $20, $cd, $45, $91, $20, $9b
@song0ref917:
	db $45, $87, $1e, $9b, $45, $87, $1c, $91, $45, $87, $1e, $91, $45, $87, $20, $91, $9d
	db $41, $11
	dw @song0ref917
	db $45, $87, $1e, $9b, $45, $87, $1c, $91, $45, $87, $1e, $91, $45, $87, $1c, $91, $b1, $45, $87, $1b, $c3, $45, $87, $20
	db $91, $9d
	db $41, $11
	dw @song0ref917
	db $41, $11
	dw @song0ref917
	db $45, $87, $1e, $9b, $45, $87, $20, $91, $45, $87, $23, $91, $45, $87, $25, $91, $ff, $95, $45, $87, $9c
@song0ref990:
	db $19, $91, $1b, $91, $1c, $91, $1e, $91, $19, $91, $1b, $91, $1c, $91, $1e, $91
	db $41, $10
	dw @song0ref990
	db $41, $10
	dw @song0ref990
	db $41, $10
	dw @song0ref990
	db $41, $10
	dw @song0ref990
	db $1b, $91, $1c, $91, $1e, $91, $20, $b9, $23, $91, $25, $91, $93, $27, $91, $28, $91, $2a, $91, $25, $91, $27, $91, $28
	db $91, $2a, $91, $93, $2c, $91, $2d, $91, $2f, $91, $2a, $91, $2c, $91, $2d, $91, $2f, $91, $31, $ff, $9d, $cf, $9e, $50
	db $06, $3d, $30, $cd
	db $41, $19
	dw @song0ref580
	db $42
	dw @song0ch1loop
@song0ch2:
@song0ref1076:
	db $86, $26, $83, $26, $85, $26, $83, $84, $26, $85, $00
@song0ref1087:
	db $26, $85, $00, $1e, $85, $00, $1e, $85, $00, $1a
@song0ref1097:
	db $8f
@song0ref1098:
	db $00
@song0ref1099:
	db $1c, $83, $00, $8b, $1c, $83, $00, $8b, $22, $8f, $00, $1c, $83, $00, $8b, $1c, $83, $00, $8b, $1c, $83, $00, $8b, $22
	db $8f, $00, $1c, $83, $00, $8b
@song0ch2loop:
	db $41, $1e
	dw @song0ref1099
	db $41, $1e
	dw @song0ref1099
@song0ref1136:
	db $26, $8f, $00, $26, $8f, $00, $1e, $8f, $00, $1a, $8f, $00, $26, $85, $00, $26, $85, $00, $89
	db $41, $2a
	dw @song0ref1087
	db $41, $1e
	dw @song0ref1099
	db $41, $1e
	dw @song0ref1099
@song0ref1164:
	db $26, $85, $00, $26, $85, $00, $1e, $85, $00, $1a, $85, $00, $26, $85, $00, $1e, $85, $00, $1e, $85, $00, $1a, $85, $00
	db $41, $13
	dw @song0ref1076
	db $85, $00, $1a, $85
	db $41, $1f
	dw @song0ref1098
	db $41, $1e
	dw @song0ref1099
	db $41, $1e
	dw @song0ref1099
	db $41, $13
	dw @song0ref1136
	db $41, $2a
	dw @song0ref1087
	db $41, $1e
	dw @song0ref1099
	db $41, $1e
	dw @song0ref1099
	db $41, $18
	dw @song0ref1164
	db $41, $13
	dw @song0ref1076
	db $85, $00, $1a, $85
	db $41, $1f
	dw @song0ref1098
	db $41, $1e
	dw @song0ref1099
	db $41, $1e
	dw @song0ref1099
	db $41, $13
	dw @song0ref1136
	db $41, $2a
	dw @song0ref1087
	db $41, $1e
	dw @song0ref1099
	db $41, $1e
	dw @song0ref1099
	db $41, $18
	dw @song0ref1164
	db $41, $13
	dw @song0ref1076
	db $85, $00, $1a, $85
	db $41, $1f
	dw @song0ref1098
	db $41, $1e
	dw @song0ref1099
	db $41, $1e
	dw @song0ref1099
	db $41, $13
	dw @song0ref1136
	db $41, $2a
	dw @song0ref1087
	db $41, $1e
	dw @song0ref1099
	db $41, $1e
	dw @song0ref1099
	db $1c, $83, $00, $8b, $22, $8f, $00, $22, $8f, $00, $22, $8f, $00, $22, $8f, $00, $95, $8e, $22, $83, $00, $9d, $84
@song0ref1301:
	db $1c, $83, $00, $c7, $1c, $83, $00, $c7, $1c, $83, $00, $b3, $1c, $83, $00, $9f, $8e, $22, $83, $00, $8b, $84, $1c, $83
	db $00, $9f
	db $41, $18
	dw @song0ref1301
	db $41, $18
	dw @song0ref1301
	db $41, $10
	dw @song0ref1301
	db $22, $8f, $00, $22, $8f, $00, $22
	db $41, $20
	dw @song0ref1097
	db $41, $1e
	dw @song0ref1099
	db $41, $1e
	dw @song0ref1099
	db $41, $13
	dw @song0ref1136
	db $41, $2a
	dw @song0ref1087
	db $41, $1e
	dw @song0ref1099
	db $41, $1e
	dw @song0ref1099
	db $41, $18
	dw @song0ref1164
	db $41, $13
	dw @song0ref1076
	db $85, $00, $1a, $85
	db $41, $1f
	dw @song0ref1098
	db $41, $1e
	dw @song0ref1099
	db $41, $1e
	dw @song0ref1099
	db $41, $13
	dw @song0ref1136
	db $41, $2a
	dw @song0ref1087
	db $41, $1e
	dw @song0ref1099
	db $41, $1e
	dw @song0ref1099
	db $41, $18
	dw @song0ref1164
	db $41, $13
	dw @song0ref1076
	db $85, $00, $1a, $85
	db $41, $1f
	dw @song0ref1098
	db $41, $18
	dw @song0ref1164
	db $41, $13
	dw @song0ref1076
	db $85, $00, $1a, $85
	db $41, $1f
	dw @song0ref1098
	db $42
	dw @song0ch2loop
@song0ch3:
	db $4b, <(@env21), >(@env21), $80
@song0ref1428:
	db $1a, $83, $4d, $1a, $85, $4d, $1a, $83, $4d, $1a
@song0ref1438:
	db $87, $4d, $1a, $87, $4d, $1a, $87, $4d, $1a, $87, $4d, $1a, $87, $00
@song0ref1452:
	db $87
@song0ref1453:
	db $4b, <(@env18), >(@env18)
@song0ref1456:
	db $20, $87, $00, $87, $4d, $20, $87, $00, $87, $4b, <(@env19), >(@env19), $82
@song0ref1469:
	db $1a, $91, $4b, <(@env18), >(@env18), $80, $20, $87, $00, $87, $4d, $20, $87, $00, $87, $4d, $20, $87, $00, $87, $4b, <(@env19)
	db >(@env19), $82, $1a, $91, $4b, <(@env18), >(@env18), $80, $20, $87, $00, $87
@song0ch3loop:
	db $4c
	db $41, $1c
	dw @song0ref1453
	db $4d
	db $41, $1c
	dw @song0ref1456
@song0ref1512:
	db $4b, <(@env21), >(@env21), $1a, $87, $00, $87, $4d, $1a, $87, $00, $87, $4d, $1a, $87, $00, $87, $4d, $1a, $87, $00, $87
	db $4d, $1a, $87, $4d, $1a, $87, $00
	db $41, $27
	dw @song0ref1438
	db $4d
	db $41, $1c
	dw @song0ref1456
	db $4d
	db $41, $1c
	dw @song0ref1456
@song0ref1552:
	db $4b, <(@env21), >(@env21), $1a, $87, $4d, $1a, $87, $4d, $1a, $87, $4d, $1a, $87, $4d, $1a, $87, $4d, $1a, $87, $4d, $1a
	db $87, $4d, $1a, $87, $4d
	db $41, $10
	dw @song0ref1428
	db $4d, $1a
	db $41, $1d
	dw @song0ref1452
	db $4d
	db $41, $1c
	dw @song0ref1456
	db $4d
	db $41, $1c
	dw @song0ref1456
	db $41, $15
	dw @song0ref1512
	db $41, $27
	dw @song0ref1438
	db $4d
	db $41, $1c
	dw @song0ref1456
	db $4d
	db $41, $1c
	dw @song0ref1456
	db $41, $10
	dw @song0ref1552
	db $4d
	db $41, $10
	dw @song0ref1428
	db $4d, $1a
	db $41, $1d
	dw @song0ref1452
	db $4d
	db $41, $1c
	dw @song0ref1456
	db $4d
	db $41, $1c
	dw @song0ref1456
	db $41, $15
	dw @song0ref1512
	db $41, $27
	dw @song0ref1438
	db $4d
	db $41, $1c
	dw @song0ref1456
	db $4d
	db $41, $1c
	dw @song0ref1456
	db $41, $10
	dw @song0ref1552
	db $4d
	db $41, $10
	dw @song0ref1428
	db $4d, $1a
	db $41, $1d
	dw @song0ref1452
	db $4d
	db $41, $1c
	dw @song0ref1456
	db $4d
	db $41, $1c
	dw @song0ref1456
	db $41, $15
	dw @song0ref1512
	db $41, $27
	dw @song0ref1438
	db $4d
	db $41, $1c
	dw @song0ref1456
	db $4d
	db $41, $1c
	dw @song0ref1456
	db $4d, $20, $87, $00, $87, $4b, <(@env19), >(@env19), $82, $1a, $91, $4d, $1a, $91, $4d, $1a, $91, $4d, $1a, $91, $00, $91
	db $4b, <(@env12), >(@env12), $4c, $a6, $20, $91, $00, $91, $4b, <(@env18), >(@env18), $80
@song0ref1712:
	db $20, $87, $00, $c3, $4d, $20, $87, $00, $c3, $4d, $20, $87, $00, $af, $4d, $20, $87, $00, $9b, $4b, <(@env19), >(@env19)
	db $82, $1a, $91, $4b, <(@env18), >(@env18), $80, $20, $87, $00, $9b, $4d
	db $41, $16
	dw @song0ref1712
	db $4d
	db $41, $16
	dw @song0ref1712
	db $4d
	db $41, $12
	dw @song0ref1712
	db $4d, $1a, $91, $4d, $1a, $91, $4b, <(@env18), >(@env18), $20, $91, $4d, $20, $91, $4b, <(@env19), >(@env19)
	db $41, $14
	dw @song0ref1469
	db $4d
	db $41, $1c
	dw @song0ref1456
	db $4d
	db $41, $1c
	dw @song0ref1456
	db $41, $15
	dw @song0ref1512
	db $41, $27
	dw @song0ref1438
	db $4d
	db $41, $1c
	dw @song0ref1456
	db $4d
	db $41, $1c
	dw @song0ref1456
	db $41, $10
	dw @song0ref1552
	db $4d
	db $41, $10
	dw @song0ref1428
	db $4d, $1a
	db $41, $1d
	dw @song0ref1452
	db $4d
	db $41, $1c
	dw @song0ref1456
	db $4d
	db $41, $1c
	dw @song0ref1456
	db $41, $15
	dw @song0ref1512
	db $41, $27
	dw @song0ref1438
	db $4d
	db $41, $1c
	dw @song0ref1456
	db $4d
	db $41, $1c
	dw @song0ref1456
	db $41, $10
	dw @song0ref1552
	db $4d
	db $41, $10
	dw @song0ref1428
	db $4d, $1a
	db $41, $1d
	dw @song0ref1452
	db $41, $10
	dw @song0ref1552
	db $4d
	db $41, $10
	dw @song0ref1428
	db $4d, $1a
	db $41, $1d
	dw @song0ref1452
	db $42
	dw @song0ch3loop
@song0ch4:
	db $52, $9e, $cf, $52, $34
@song0ref1865:
	db $07, $91, $07, $91, $07, $91, $07, $91, $07, $91, $07, $91, $07, $91, $07, $91
@song0ch4loop:
@song0ref1882:
	db $06, $91, $06, $91, $08, $91, $05, $91, $05, $91, $0c, $91, $05, $91, $0c, $91
	db $41, $10
	dw @song0ref1865
@song0ref1901:
	db $0a, $91, $0a, $91, $03, $91, $05, $91, $05, $91, $0c, $91, $05, $91, $0c, $91
	db $41, $10
	dw @song0ref1865
	db $41, $10
	dw @song0ref1882
	db $41, $10
	dw @song0ref1865
	db $41, $10
	dw @song0ref1901
	db $41, $10
	dw @song0ref1865
	db $41, $10
	dw @song0ref1865
	db $41, $10
	dw @song0ref1865
	db $41, $10
	dw @song0ref1865
	db $41, $10
	dw @song0ref1865
	db $41, $10
	dw @song0ref1865
	db $07, $91, $07, $91, $07, $91, $07, $91, $07, $91, $07, $91, $07, $91, $06, $91, $06, $91, $06, $91, $06, $91, $06, $91
	db $06, $91, $06, $91, $06, $91, $06
@song0ref1978:
	db $91, $07, $91, $07, $91, $07, $91, $09, $91, $07, $91, $09, $91, $07, $91, $07, $91, $01, $91, $02, $91, $01, $91, $01
	db $91, $01, $91, $02, $91, $01, $91, $01, $91, $06, $91, $06, $91, $06, $91, $08, $91, $01, $91, $02, $91, $01, $91, $01
	db $91, $07, $91, $09, $91, $07, $91, $07, $91, $07, $91, $09, $91, $07, $91, $07
	db $41, $40
	dw @song0ref1978
@song0ref2045:
	db $91, $05, $91, $05, $91, $0c, $91, $05, $91, $05, $91, $05, $91, $0c, $91, $05, $91, $06, $91, $06, $91, $08, $91, $06
	db $91, $06, $91, $06, $91, $08, $91, $06, $91, $07, $91, $07, $91, $09, $91, $07, $91, $09, $91, $07, $91, $07, $91, $09
@song0ref2093:
	db $91, $07, $91, $07, $91, $09, $91, $07, $91, $07, $91, $09, $91, $07, $91, $09
	db $41, $35
	dw @song0ref2045
	db $07, $91, $07, $91, $07, $cd, $07, $cd, $07, $cd, $07, $b9, $07, $a5, $07, $91, $09, $91, $07, $91, $01, $cd, $01, $cd
	db $01, $b9, $01, $a5, $01, $91, $02, $91, $01, $91, $0b, $cd, $0b, $cd, $0b, $b9, $0b, $a5, $0b, $91, $02, $91, $0b, $91
	db $01, $cd, $01, $cd, $01, $b9, $01, $a5, $02, $91, $01, $91, $02
	db $41, $10
	dw @song0ref2093
@song0ref2176:
	db $91, $01, $91, $01, $91, $02, $91, $01, $91, $01, $91, $02, $91, $01, $91, $02, $91, $0b, $91, $0b, $91, $0d, $91, $0b
	db $91, $0b, $91, $0d, $91, $0b, $91, $0d
	db $41, $11
	dw @song0ref2176
	db $07, $91, $07, $91, $09, $91, $07, $91, $07, $91, $09, $91, $07, $91, $09
	db $41, $20
	dw @song0ref2176
	db $41, $10
	dw @song0ref2093
@song0ref2232:
	db $91, $07, $91, $07, $91, $07, $91, $09, $91, $07, $91, $07, $91, $02, $91, $09, $91, $01, $91, $01, $91, $01, $91, $02
	db $91, $01, $91, $01, $91, $0d, $91, $02, $91, $0e, $91, $0e, $91, $0e, $91, $0f, $91, $0e, $91, $0e, $91, $06, $91, $0f
	db $91, $0b, $91, $0b, $91, $0b, $91, $0d, $91, $0b, $91, $0b, $91, $04, $91, $0d
	db $41, $40
	dw @song0ref2232
	db $41, $11
	dw @song0ref2232
	db $07, $91, $07, $91, $09, $91, $07, $91, $07, $91, $09, $91, $07, $91, $09, $91
	db $41, $10
	dw @song0ref1865
	db $42
	dw @song0ch4loop
