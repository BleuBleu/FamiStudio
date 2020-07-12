; FamiStudio NSF driver, heavily customized version of FamiTone2. 
; Terrible code. Not for the faint of hearth.

;settings, uncomment or put them into your main program; the latter makes possible updates easier

; FT_BASE_ADR     = $0300  ;page in the RAM used for FT2 variables, should be $xx00
; FT_TEMP         = $00    ;3 bytes in zeropage used by the library as a scratchpad
FT_DPCM_OFF       = $c000
FT_SFX_STREAMS    = 0     ;number of sound effects played at once, 1..4
  
FT_DPCM_ENABLE    = 1     ;undefine to exclude all DMC code
FT_SFX_ENABLE     = 0     ;undefine to exclude all sound effects code
FT_THREAD         = 0     ;undefine if you are calling sound effects from the same thread as the sound update call

.ifndef FT_PAL_SUPPORT
    FT_PAL_SUPPORT = 0
.endif
.ifndef FT_NTSC_SUPPORT
    FT_NTSC_SUPPORT = 0
.endif
.ifndef FT_FAMISTUDIO_TEMPO
    FT_FAMISTUDIO_TEMPO = 0
.endif

;internal defines
FT_PITCH_FIX      = FT_PAL_SUPPORT && FT_NTSC_SUPPORT ;add PAL/NTSC pitch correction code only when both modes are enabled
FT_SMOOTH_VIBRATO = 1    ; Blaarg's smooth vibrato technique

.segment "RAM"

.if .defined(FT_VRC7)
    FT_NUM_ENVELOPES        = 3+3+2+3+2+2+2+2+2+2
    FT_NUM_PITCH_ENVELOPES  = 9
    FT_NUM_CHANNELS         = 11
.elseif .defined(FT_VRC6)
    FT_NUM_ENVELOPES        = 3+3+2+3+3+3+3
    FT_NUM_PITCH_ENVELOPES  = 6
    FT_NUM_CHANNELS         = 8
.elseif .defined(FT_S5B)
    FT_NUM_ENVELOPES        = 3+3+2+3+2+2+2
    FT_NUM_PITCH_ENVELOPES  = 6
    FT_NUM_CHANNELS         = 8    
.elseif .defined(FT_N163) 
    FT_NUM_ENVELOPES        = 3+3+2+3+(FT_N163_CHN_CNT*2)
    FT_NUM_PITCH_ENVELOPES  = 3+FT_N163_CHN_CNT
    FT_NUM_CHANNELS         = 5+FT_N163_CHN_CNT
.elseif .defined(FT_MMC5)
    FT_NUM_ENVELOPES        = 3+3+2+3+3+3
    FT_NUM_PITCH_ENVELOPES  = 5
    FT_NUM_CHANNELS         = 7
.elseif .defined(FT_FDS)
    FT_NUM_ENVELOPES        = 3+3+2+3+2
    FT_NUM_PITCH_ENVELOPES  = 4
    FT_NUM_CHANNELS         = 6
.else
    FT_NUM_ENVELOPES        = 3+3+2+3
    FT_NUM_PITCH_ENVELOPES  = 3
    FT_NUM_CHANNELS         = 5
.endif

FT_ENV_VALUE  : .res FT_NUM_ENVELOPES
FT_ENV_REPEAT : .res FT_NUM_ENVELOPES
FT_ENV_ADR_L  : .res FT_NUM_ENVELOPES
FT_ENV_ADR_H  : .res FT_NUM_ENVELOPES
FT_ENV_PTR    : .res FT_NUM_ENVELOPES

FT_PITCH_ENV_VALUE_L  : .res FT_NUM_PITCH_ENVELOPES
FT_PITCH_ENV_VALUE_H  : .res FT_NUM_PITCH_ENVELOPES
FT_PITCH_ENV_REPEAT   : .res FT_NUM_PITCH_ENVELOPES
FT_PITCH_ENV_ADR_L    : .res FT_NUM_PITCH_ENVELOPES
FT_PITCH_ENV_ADR_H    : .res FT_NUM_PITCH_ENVELOPES
FT_PITCH_ENV_PTR      : .res FT_NUM_PITCH_ENVELOPES
FT_PITCH_FINE_VALUE   : .res FT_NUM_PITCH_ENVELOPES

;slide structure offsets, 3 bytes per slide.
FT_SLIDE_STEP    : .res FT_NUM_PITCH_ENVELOPES
FT_SLIDE_PITCH_L : .res FT_NUM_PITCH_ENVELOPES
FT_SLIDE_PITCH_H : .res FT_NUM_PITCH_ENVELOPES

;channel structure offsets, 10 bytes per channel
FT_CHN_PTR_L        : .res FT_NUM_CHANNELS
FT_CHN_PTR_H        : .res FT_NUM_CHANNELS
FT_CHN_NOTE         : .res FT_NUM_CHANNELS
FT_CHN_INSTRUMENT   : .res FT_NUM_CHANNELS
FT_CHN_REPEAT       : .res FT_NUM_CHANNELS
FT_CHN_RETURN_L     : .res FT_NUM_CHANNELS
FT_CHN_RETURN_H     : .res FT_NUM_CHANNELS
FT_CHN_REF_LEN      : .res FT_NUM_CHANNELS
FT_CHN_VOLUME_TRACK : .res FT_NUM_CHANNELS
FT_CHN_ENV_OVERRIDE : .res FT_NUM_CHANNELS ; bit 7 = pitch, bit 0 = arpeggio.
.if .defined(FT_N163) || .defined(FT_VRC7) || .defined(FT_FDS)
FT_CHN_INST_CHANGED : .res FT_NUM_CHANNELS-5
.endif
.ifdef FT_EQUALIZER
FT_CHN_NOTE_COUNTER : .res FT_NUM_CHANNELS
.endif

.ifdef FT_VRC7
FT_CHN_PREV_HI      : .res 6
FT_CHN_VRC7_PATCH   : .res 6
FT_CHN_VRC7_TRIGGER : .res 6 ; bit 0 = new note triggered, bit 7 = note released.
.endif

.ifdef FT_N163
FT_CHN_N163_WAVE_LEN: .res FT_N163_CHN_CNT
.endif

;variables and aliases
FT_CH0_ENVS = 0
FT_CH1_ENVS = 3
FT_CH2_ENVS = 6
FT_CH3_ENVS = 8

.if .defined(FT_VRC6) 
    FT_CH5_ENVS  = 11
    FT_CH6_ENVS  = 14
    FT_CH7_ENVS  = 17
.elseif .defined(FT_VRC7) 
    FT_CH5_ENVS  = 11
    FT_CH6_ENVS  = 13
    FT_CH7_ENVS  = 15    
    FT_CH8_ENVS  = 17
    FT_CH9_ENVS  = 19
    FT_CH10_ENVS = 21    
.elseif .defined(FT_N163)
    FT_CH5_ENVS  = 11
    FT_CH6_ENVS  = 13
    FT_CH7_ENVS  = 15    
    FT_CH8_ENVS  = 17
    FT_CH9_ENVS  = 19
    FT_CH10_ENVS = 21   
    FT_CH11_ENVS = 23   
    FT_CH12_ENVS = 25   
.elseif .defined(FT_FDS)
    FT_CH5_ENVS  = 11
.elseif .defined(FT_MMC5)
    FT_CH5_ENVS  = 11
    FT_CH6_ENVS  = 14
.elseif .defined(FT_S5B)    
    FT_CH5_ENVS  = 11
    FT_CH6_ENVS  = 13
    FT_CH7_ENVS  = 15
.endif

.if .defined(FT_VRC7)
    FT_PITCH_SHIFT = 3
.elseif .defined(FT_N163)
    .if FT_N163_CHN_CNT > 4
        FT_PITCH_SHIFT = 5
    .elseif FT_N163_CHN_CNT > 2
        FT_PITCH_SHIFT = 4
    .elseif FT_N163_CHN_CNT > 1
        FT_PITCH_SHIFT = 3
    .else
        FT_PITCH_SHIFT = 2
    .endif 
.else
    FT_PITCH_SHIFT = 0
.endif


FT_ENV_VOLUME_OFF = 0
FT_ENV_NOTE_OFF   = 1
FT_ENV_DUTY_OFF   = 2

FT_PAL_ADJUST:    .res 1
FT_SONG_LIST_L:   .res 1
FT_SONG_LIST_H:   .res 1
FT_INSTRUMENT_L:  .res 1
FT_INSTRUMENT_H:  .res 1
.if !FT_FAMISTUDIO_TEMPO
FT_TEMPO_STEP_L:  .res 1
FT_TEMPO_STEP_H:  .res 1
FT_TEMPO_ACC_L:   .res 1
FT_TEMPO_ACC_H:   .res 1
.else
FT_TEMPO_ENV_PTR_L:   .res 1
FT_TEMPO_ENV_PTR_H:   .res 1
FT_TEMPO_ENV_COUNTER: .res 1
FT_TEMPO_ENV_IDX:     .res 1
FT_TEMPO_FRAME_NUM:   .res 1
FT_TEMPO_FRAME_CNT:   .res 1
.endif
FT_DPCM_LIST_L:   .res 1
FT_DPCM_LIST_H:   .res 1
FT_DPCM_EFFECT:   .res 1
FT_OUT_BUF:       .res 1
FT_PULSE1_PREV:   .res 1
FT_PULSE2_PREV:   .res 1
FT_SONG_SPEED     = FT_CHN_INSTRUMENT+4

.ifdef FT_N163
FT_N163_CHN_MASK  = (FT_N163_CHN_CNT - 1) << 4
.endif

.ifdef FT_MMC5
FT_MMC5_PULSE1_PREV: .res 1
FT_MMC5_PULSE2_PREV: .res 1
.endif

.ifdef FT_FDS
FT_FDS_MOD_SPEED: .res 2
FT_FDS_MOD_DEPTH: .res 1
FT_FDS_MOD_DELAY: .res 1
FT_FDS_OVERRIDE_FLAGS: .res 1 ; Bit 7 = mod speed overriden, bit 6 mod depth overriden
.endif

; FDS, N163 and VRC7 have very different instrument layout and are 16-bytes, so we keep them seperate.
.if .defined(FT_FDS) || .defined(FT_N163) || .defined(FT_VRC7) 
FT_EXP_INSTRUMENT_L: .res 1
FT_EXP_INSTRUMENT_H: .res 1
.endif

; SFX Definately dont work right now.
.if(FT_SFX_ENABLE)

FT_SFX_STRUCT_SIZE = 15

;sound effect stream variables, 2 bytes and 15 bytes per stream
;when sound effects are disabled, this memory is not used

FT_SFX_ADR_L    = .res 1
FT_SFX_ADR_H    = .res 1
FT_SFX_BASE_ADR = .res 1

FT_SFX_REPEAT   = FT_SFX_BASE_ADR+0
FT_SFX_PTR_L    = FT_SFX_BASE_ADR+1
FT_SFX_PTR_H    = FT_SFX_BASE_ADR+2
FT_SFX_OFF      = FT_SFX_BASE_ADR+3
FT_SFX_BUF      = FT_SFX_BASE_ADR+4    ;11 bytes

;aliases for sound effect channels to use in user calls

FT_SFX_CH0      = FT_SFX_STRUCT_SIZE*0
FT_SFX_CH1      = FT_SFX_STRUCT_SIZE*1
FT_SFX_CH2      = FT_SFX_STRUCT_SIZE*2
FT_SFX_CH3      = FT_SFX_STRUCT_SIZE*3

.endif 

.segment "ZEROPAGE"
;zero page variables

FT_TEMP:
FT_TEMP_VAR1 : .res 1
FT_TEMP_VAR2 : .res 1
FT_TEMP_VAR3 : .res 1
.ifdef FT_VRC7
FT_TEMP_VAR4 : .res 1
.endif

FT_TEMP_PTR1 : .res 2
FT_TEMP_PTR2 : .res 2

FT_TEMP_PTR_L  = FT_TEMP_PTR1+0
FT_TEMP_PTR_H  = FT_TEMP_PTR1+1
FT_TEMP_PTR2_L = FT_TEMP_PTR2+0
FT_TEMP_PTR2_H = FT_TEMP_PTR2+1

.segment "CODE"

;aliases for the APU registers
APU_PL1_VOL    = $4000
APU_PL1_SWEEP  = $4001
APU_PL1_LO     = $4002
APU_PL1_HI     = $4003
APU_PL2_VOL    = $4004
APU_PL2_SWEEP  = $4005
APU_PL2_LO     = $4006
APU_PL2_HI     = $4007
APU_TRI_LINEAR = $4008
APU_TRI_LO     = $400a
APU_TRI_HI     = $400b
APU_NOISE_VOL  = $400c
APU_NOISE_LO   = $400e
APU_NOISE_HI   = $400f
APU_DMC_FREQ   = $4010
APU_DMC_RAW    = $4011
APU_DMC_START  = $4012
APU_DMC_LEN    = $4013
APU_SND_CHN    = $4015
APU_FRAME_CNT  = $4017

.ifdef FT_VRC6
VRC6_PL1_VOL   = $9000
VRC6_PL1_LO    = $9001
VRC6_PL1_HI    = $9002
VRC6_PL2_VOL   = $a000
VRC6_PL2_LO    = $a001
VRC6_PL2_HI    = $a002
VRC6_SAW_VOL   = $b000
VRC6_SAW_LO    = $b001
VRC6_SAW_HI    = $b002
.endif

.ifdef FT_VRC7
VRC7_SILENCE   = $e000
VRC7_REG_SEL   = $9010
VRC7_REG_WRITE = $9030
VRC7_REG_LO_1  = $10
VRC7_REG_LO_2  = $11
VRC7_REG_LO_3  = $12
VRC7_REG_LO_4  = $13
VRC7_REG_LO_5  = $14
VRC7_REG_LO_6  = $15
VRC7_REG_HI_1  = $20
VRC7_REG_HI_2  = $21
VRC7_REG_HI_3  = $22
VRC7_REG_HI_4  = $23
VRC7_REG_HI_5  = $24
VRC7_REG_HI_6  = $25
VRC7_REG_VOL_1 = $30
VRC7_REG_VOL_2 = $31
VRC7_REG_VOL_3 = $32
VRC7_REG_VOL_4 = $33
VRC7_REG_VOL_5 = $34
VRC7_REG_VOL_6 = $35 
.endif

.ifdef FT_MMC5
MMC5_PL1_VOL   = $5000
MMC5_PL1_SWEEP = $5001
MMC5_PL1_LO    = $5002
MMC5_PL1_HI    = $5003
MMC5_PL2_VOL   = $5004
MMC5_PL2_SWEEP = $5005
MMC5_PL2_LO    = $5006
MMC5_PL2_HI    = $5007
MMC5_PCM_MODE  = $5010
MMC5_SND_CHN   = $5015
.endif

.ifdef FT_N163
N163_SILENCE       = $e000
N163_ADDR          = $f800
N163_DATA          = $4800 
N163_REG_FREQ_LO   = $78
N163_REG_PHASE_LO  = $79
N163_REG_FREQ_MID  = $7a
N163_REG_PHASE_MID = $7b
N163_REG_FREQ_HI   = $7c
N163_REG_PHASE_HI  = $7d
N163_REG_WAVE      = $7e
N163_REG_VOLUME    = $7f
.endif

.ifdef FT_S5B
S5B_ADDR       = $c000
S5B_DATA       = $e000
S5B_REG_LO_A   = $00
S5B_REG_HI_A   = $01
S5B_REG_LO_B   = $02
S5B_REG_HI_B   = $03
S5B_REG_LO_C   = $04
S5B_REG_HI_C   = $05
S5B_REG_NOISE  = $06
S5B_REG_TONE   = $07
S5B_REG_VOL_A  = $08
S5B_REG_VOL_B  = $09
S5B_REG_VOL_C  = $0a
S5B_REG_ENV_LO = $0b
S5B_REG_ENV_HI = $0c
S5B_REG_SHAPE  = $0d
S5B_REG_IO_A   = $0e
S5B_REG_IO_B   = $0f
.endif

.ifdef FT_FDS
FDS_WAV_START  = $4040
FDS_VOL_ENV    = $4080
FDS_FREQ_LO    = $4082
FDS_FREQ_HI    = $4083
FDS_SWEEP_ENV  = $4084
FDS_SWEEP_BIAS = $4085
FDS_MOD_LO     = $4086
FDS_MOD_HI     = $4087
FDS_MOD_TABLE  = $4088
FDS_VOL        = $4089
FDS_ENV_SPEED  = $408A
.endif

;aliases for the APU registers in the output buffer

.if(!FT_SFX_ENABLE) ;if sound effects are disabled, write to the APU directly
    FT_MR_PULSE1_V = APU_PL1_VOL
    FT_MR_PULSE1_L = APU_PL1_LO
    FT_MR_PULSE1_H = APU_PL1_HI
    FT_MR_PULSE2_V = APU_PL2_VOL
    FT_MR_PULSE2_L = APU_PL2_LO
    FT_MR_PULSE2_H = APU_PL2_HI
    FT_MR_TRI_V    = APU_TRI_LINEAR
    FT_MR_TRI_L    = APU_TRI_LO
    FT_MR_TRI_H    = APU_TRI_HI
    FT_MR_NOISE_V  = APU_NOISE_VOL
    FT_MR_NOISE_F  = APU_NOISE_LO
.else               ;otherwise write to the output buffer
    FT_MR_PULSE1_V = FT_OUT_BUF
    FT_MR_PULSE1_L = FT_OUT_BUF+1
    FT_MR_PULSE1_H = FT_OUT_BUF+2
    FT_MR_PULSE2_V = FT_OUT_BUF+3
    FT_MR_PULSE2_L = FT_OUT_BUF+4
    FT_MR_PULSE2_H = FT_OUT_BUF+5
    FT_MR_TRI_V    = FT_OUT_BUF+6
    FT_MR_TRI_L    = FT_OUT_BUF+7
    FT_MR_TRI_H    = FT_OUT_BUF+8
    FT_MR_NOISE_V  = FT_OUT_BUF+9
    FT_MR_NOISE_F  = FT_OUT_BUF+10
.endif

; increments 16-bit.
.macro inc_16 addr
    .local @ok
    inc addr+0
    bne @ok
    inc addr+1
@ok:
.endmacro

; add 8-bit to a 16-bit (unsigned).
.macro add_16_8 addr, val
    .local @ok
    clc
    lda val
    adc addr+0
    sta addr+0
    bcc @ok
    inc addr+1
@ok:
.endmacro

.ifdef FT_VRC7
.proc _FT2Vrc7WaitRegWrite
    stx FT_TEMP_VAR4
    ldx #$08
    wait_loop:
        dex
        bne wait_loop
        ldx FT_TEMP_VAR4
    rts
.endproc

.proc _FT2Vrc7WaitRegSelect
    rts
.endproc
.endif

;------------------------------------------------------------------------------
; reset APU, initialize FamiTone
; in: A   0 for PAL, not 0 for NTSC
;     X,Y pointer to music data
;------------------------------------------------------------------------------

.proc FamiToneInit

    stx FT_SONG_LIST_L         ;store music data pointer for further use
    sty FT_SONG_LIST_H
    stx FT_TEMP_PTR_L
    sty FT_TEMP_PTR_H

    .if(::FT_PITCH_FIX)
    tax                        ;set SZ flags for A
    beq pal
    lda #97
pal:
    .else
    .if(::FT_PAL_SUPPORT)
    lda #0
    .endif
    .if(::FT_NTSC_SUPPORT)
    lda #97
    .endif
    .endif
    sta FT_PAL_ADJUST

    jsr FamiToneMusicStop      ;initialize channels and envelopes

    ldy #1
    lda (FT_TEMP_PTR1),y       ;get instrument list address
    sta FT_INSTRUMENT_L
    iny
    lda (FT_TEMP_PTR1),y
    sta FT_INSTRUMENT_H
    iny

    .if .defined(::FT_FDS) || .defined(::FT_N163) || .defined(::FT_VRC7) 
        lda (FT_TEMP_PTR1),y       ;get expansion instrument list address
        sta FT_EXP_INSTRUMENT_L
        iny
        lda (FT_TEMP_PTR1),y
        sta FT_EXP_INSTRUMENT_H
        iny
    .endif

    lda (FT_TEMP_PTR1),y       ;get sample list address
    sta FT_DPCM_LIST_L
    iny
    lda (FT_TEMP_PTR1),y
    sta FT_DPCM_LIST_H

    lda #$80                   ;previous pulse period MSB, to not write it when not changed
    sta FT_PULSE1_PREV
    sta FT_PULSE2_PREV

    lda #$0f                   ;enable channels, stop DMC
    sta APU_SND_CHN
    lda #$80                   ;disable triangle length counter
    sta APU_TRI_LINEAR
    lda #$00                   ;load noise length
    sta APU_NOISE_HI

    lda #$30                   ;volumes to 0
    sta APU_PL1_VOL
    sta APU_PL2_VOL
    sta APU_NOISE_VOL
    lda #$08                   ;no sweep
    sta APU_PL1_SWEEP
    sta APU_PL2_SWEEP

.ifdef ::FT_VRC7
    lda #0
    sta VRC7_SILENCE ; Enable VRC7 audio.
.endif

.ifdef ::FT_MMC5
    lda #$00
    sta MMC5_PCM_MODE
    lda #$03
    sta MMC5_SND_CHN
.endif

.ifdef ::FT_S5B
    lda #S5B_REG_TONE
    sta S5B_ADDR
    lda #$38 ; No noise, just 3 tones for now.
    sta S5B_DATA
.endif

    jmp FamiToneMusicStop

.endproc

;------------------------------------------------------------------------------
; stop music that is currently playing, if any
; in: none
;------------------------------------------------------------------------------

.proc FamiToneMusicStop

    lda #0
    sta FT_SONG_SPEED          ;stop music, reset pause flag
    sta FT_DPCM_EFFECT         ;no DPCM effect playing

    ldx #0    ;initialize channel structures

set_channels:

    lda #0
    sta FT_CHN_REPEAT,x
    sta FT_CHN_INSTRUMENT,x
    sta FT_CHN_NOTE,x
    sta FT_CHN_REF_LEN,x
    sta FT_CHN_VOLUME_TRACK,x
    sta FT_CHN_ENV_OVERRIDE,x

nextchannel:
    inx                        ;next channel
    cpx #FT_NUM_CHANNELS
    bne set_channels

    ldx #0    ;initialize all slides to zero
    lda #0
set_slides:

    sta FT_SLIDE_STEP, x
    inx                        ;next channel
    cpx #FT_NUM_PITCH_ENVELOPES
    bne set_slides

    ldx #0    ;initialize all envelopes to the dummy envelope

set_envelopes:

    lda #.lobyte(_FT2DummyEnvelope)
    sta FT_ENV_ADR_L,x
    lda #.hibyte(_FT2DummyEnvelope)
    sta FT_ENV_ADR_H,x
    lda #0
    sta FT_ENV_REPEAT,x
    sta FT_ENV_VALUE,x
    sta FT_ENV_PTR,x
    inx
    cpx #FT_NUM_ENVELOPES
    bne set_envelopes

    ldx #0    ;initialize all envelopes to the dummy envelope

set_pitch_envelopes:

    lda #.lobyte(_FT2DummyPitchEnvelope)
    sta FT_PITCH_ENV_ADR_L,x
    lda #.hibyte(_FT2DummyPitchEnvelope)
    sta FT_PITCH_ENV_ADR_H,x
    lda #0
    sta FT_PITCH_ENV_REPEAT,x
    sta FT_PITCH_ENV_VALUE_L,x
    sta FT_PITCH_ENV_VALUE_H,x
    sta FT_PITCH_FINE_VALUE,x
    lda #1
    sta FT_PITCH_ENV_PTR,x
    inx
    cpx #FT_NUM_PITCH_ENVELOPES
    bne set_pitch_envelopes

    jmp FamiToneSampleStop

.endproc

;------------------------------------------------------------------------------
; play music
; in: A number of subsong
;------------------------------------------------------------------------------

.proc FamiToneMusicPlay

    tmp = FT_TEMP_PTR_L

    ldx FT_SONG_LIST_L
    stx FT_TEMP_PTR_L
    ldx FT_SONG_LIST_H
    stx FT_TEMP_PTR_H

    ldy #0
    cmp (FT_TEMP_PTR1),y       ;check if there is such sub song
    bcc valid_song
    rts

valid_song:
.if ::FT_NUM_CHANNELS = 5
    asl
    sta tmp
    asl
    tax
    asl
    adc tmp
    stx tmp
    adc tmp
.elseif ::FT_NUM_CHANNELS = 6
    asl
    asl
    asl
    asl
.elseif ::FT_NUM_CHANNELS = 7
    asl
    sta tmp
    asl
    asl
    asl
    adc tmp  
.elseif ::FT_NUM_CHANNELS = 8
    asl
    asl
    sta tmp
    asl
    asl
    adc tmp
.elseif ::FT_NUM_CHANNELS = 9
    asl
    sta tmp
    asl
    tax
    asl
    asl
    adc tmp
    stx tmp
    adc tmp
.elseif ::FT_NUM_CHANNELS = 10
    asl
    asl
    asl
    sta tmp
    asl
    adc tmp  
.elseif ::FT_NUM_CHANNELS = 11
    asl
    sta tmp
    asl
    asl
    tax
    asl
    adc tmp
    stx tmp
    adc tmp
.elseif ::FT_NUM_CHANNELS = 12
    asl
    asl
    sta tmp
    asl
    tax
    asl
    adc tmp
    stx tmp
    adc tmp
.elseif ::FT_NUM_CHANNELS = 13
    asl
    sta tmp
    asl
    asl
    asl
    asl
    sec
    sbc tmp
.else
    .assert 0, error, "Missing song multiplier."
.endif

.if .defined(::FT_FDS) || .defined(::FT_VRC7) || .defined(::FT_N163)
    adc #7                     ;add offset
.else
    adc #5                     ;add offset
.endif
    tay

    lda FT_SONG_LIST_L         ;restore pointer LSB
    sta FT_TEMP_PTR_L

    jsr FamiToneMusicStop      ;stop music, initialize channels and envelopes

    ldx #0    ;initialize channel structures

set_channels:

    lda (FT_TEMP_PTR1),y       ;read channel pointers
    sta FT_CHN_PTR_L,x
    iny
    lda (FT_TEMP_PTR1),y
    sta FT_CHN_PTR_H,x
    iny

    lda #0
    sta FT_CHN_REPEAT,x
    sta FT_CHN_INSTRUMENT,x
    sta FT_CHN_NOTE,x
    sta FT_CHN_REF_LEN,x
    lda #$f0
    sta FT_CHN_VOLUME_TRACK,x

nextchannel:
    inx                        ;next channel
    cpx #FT_NUM_CHANNELS
    bne set_channels

.if !::FT_FAMISTUDIO_TEMPO
    lda FT_PAL_ADJUST          ;read tempo for PAL or NTSC
    beq pal
    iny
    iny
pal:

    lda (FT_TEMP_PTR1),y       ;read the tempo step
    sta FT_TEMPO_STEP_L
    iny
    lda (FT_TEMP_PTR1),y
    sta FT_TEMPO_STEP_H

    lda #0                     ;reset tempo accumulator
    sta FT_TEMPO_ACC_L
    lda #6                     ;default speed
    sta FT_TEMPO_ACC_H
    sta FT_SONG_SPEED          ;apply default speed, this also enables music
.else
    lda (FT_TEMP_PTR1),y
    sta FT_TEMPO_ENV_PTR_L
    sta FT_TEMP_PTR2+0
    iny
    lda (FT_TEMP_PTR1),y
    sta FT_TEMPO_ENV_PTR_H
    sta FT_TEMP_PTR2+1
    iny
    lda (FT_TEMP_PTR1),y
.if(::FT_PITCH_FIX) ; Dual mode
    ldx FT_PAL_ADJUST
    bne ntsc_target
    ora #1
    ntsc_target:
.elseif(::FT_PAL_SUPPORT) ; PAL only
    ora #1
.endif
    tax
    lda _FT2FamiStudioTempoFrameLookup, x ; Lookup contains the number of frames to run (0,1,2) to maintain tempo
    sta FT_TEMPO_FRAME_NUM
    ldy #0
    sty FT_TEMPO_ENV_IDX
    lda (FT_TEMP_PTR2),y
    clc 
    adc #1
    sta FT_TEMPO_ENV_COUNTER
    lda #6
    sta FT_SONG_SPEED          ; simply so the song isnt considered paused.
.endif

.ifdef ::FT_VRC7
    lda #0
    ldx #5
    clear_vrc7_loop:
        sta FT_CHN_PREV_HI, x
        sta FT_CHN_VRC7_PATCH, x
        sta FT_CHN_VRC7_TRIGGER,x
        dex
        bpl clear_vrc7_loop 
.endif

.ifdef FT_FDS
    lda #0
    sta FT_FDS_MOD_SPEED+0
    sta FT_FDS_MOD_SPEED+1
    sta FT_FDS_MOD_DEPTH
    sta FT_FDS_MOD_DELAY
    sta FT_FDS_OVERRIDE_FLAGS
.endif

.ifdef FT_CHN_INST_CHANGED
    lda #0
    ldx #(FT_NUM_CHANNELS-5)
    clear_inst_changed_loop:
        sta FT_CHN_INST_CHANGED, x
        dex
        bpl clear_inst_changed_loop 
.endif

.ifdef ::FT_N163
    lda #0
    ldx #FT_N163_CHN_CNT
    clear_vrc7_loop:
        sta FT_CHN_N163_WAVE_LEN, x
        dex
        bpl clear_vrc7_loop 
.endif

skip:
    rts

.endproc

;------------------------------------------------------------------------------
; pause and unpause current music
; in: A 0 or not 0 to play or pause
;------------------------------------------------------------------------------

.proc FamiToneMusicPause

    tax                        ;set SZ flags for A
    beq unpause
    
pause:

    jsr FamiToneSampleStop
    
    lda #0                     ;mute sound
    sta FT_ENV_VALUE+FT_CH0_ENVS+FT_ENV_VOLUME_OFF
    sta FT_ENV_VALUE+FT_CH1_ENVS+FT_ENV_VOLUME_OFF
    sta FT_ENV_VALUE+FT_CH2_ENVS+FT_ENV_VOLUME_OFF
    sta FT_ENV_VALUE+FT_CH3_ENVS+FT_ENV_VOLUME_OFF
    lda FT_SONG_SPEED          ;set pause flag
    ora #$80
    bne done
unpause:
    lda FT_SONG_SPEED          ;reset pause flag
    and #$7f
done:
    sta FT_SONG_SPEED

    rts

.endproc

; x = note index
; y = slide/pitch envelope index
.macro FamiToneComputeNoteFinalPitch pitch_env_offset, pitch_env_indexer, note_table_lsb, note_table_msb

    .local pitch
    .local tmp_ror
    .local pos
    .local no_slide

    pitch   = FT_TEMP_PTR2
    tmp_ror = FT_TEMP_VAR1
 
    ; Pitch envelope + fine pitch (sign extended)
    clc
    lda FT_PITCH_FINE_VALUE+pitch_env_offset pitch_env_indexer
    adc FT_PITCH_ENV_VALUE_L+pitch_env_offset pitch_env_indexer
    sta pitch+0
    lda FT_PITCH_FINE_VALUE+pitch_env_offset pitch_env_indexer
    and #$80
    beq pos
    lda #$ff
pos:
    adc FT_PITCH_ENV_VALUE_H+pitch_env_offset pitch_env_indexer
    sta pitch+1

    ; Check if there is an active slide.
    lda FT_SLIDE_STEP+pitch_env_offset pitch_env_indexer
    beq no_slide

    ; Add slide
.if pitch_env_offset >= 3 && (.defined(::FT_VRC7) || .defined(::FT_N163))
    ; These channels dont have fractional part for slides and have the same shift for slides + pitch.
    clc
    lda FT_SLIDE_PITCH_L+pitch_env_offset pitch_env_indexer
    adc pitch+0
    sta pitch+0
    lda FT_SLIDE_PITCH_H+pitch_env_offset pitch_env_indexer
    adc pitch+1
    sta pitch+1     
 .else
    ; Most channels have 1 bit of fraction for slides.
    lda FT_SLIDE_PITCH_H+pitch_env_offset pitch_env_indexer
    cmp #$80 ; sign extend upcoming right shift.
    ror ; we have 1 bit of fraction for slides, shift right hi byte.
    sta tmp_ror
    lda FT_SLIDE_PITCH_L+pitch_env_offset pitch_env_indexer
    ror ; shift right low byte.
    clc
    adc pitch+0
    sta pitch+0
    lda tmp_ror
    adc pitch+1 
    sta pitch+1 
.endif

no_slide:    

.if pitch_env_offset >= 3 && (.defined(::FT_VRC7) || .defined(::FT_N163))
    .if ::FT_PITCH_SHIFT >= 1
        asl pitch+0
        rol pitch+1
    .if ::FT_PITCH_SHIFT >= 2
        asl pitch+0
        rol pitch+1
    .if ::FT_PITCH_SHIFT >= 3
        asl pitch+0
        rol pitch+1
    .if ::FT_PITCH_SHIFT >= 4
        asl pitch+0
        rol pitch+1
    .if ::FT_PITCH_SHIFT >= 5
        asl pitch+0
        rol pitch+1
    .endif 
    .endif
    .endif
    .endif
    .endif
.endif

    ; Finally, add note pitch.
    clc
    lda note_table_lsb,x
    adc pitch+0
    sta pitch+0
    lda note_table_msb,x
    adc pitch+1
    sta pitch+1   

.endmacro

.macro FamiToneUpdateChannelSound idx, env_offset, slide_offset, pulse_prev, vol_ora, hi_ora, reg_hi, reg_lo, reg_vol, reg_sweep

    .local note_table_lsb
    .local note_table_msb
    .local pitch
    .local tmp
    .local nocut
    .local set_volume
    .local compute_volume
    .local hi_delta_too_big

    tmp   = FT_TEMP_VAR1
    pitch = FT_TEMP_PTR2

.if .defined(::FT_VRC6) && idx = 7
    note_table_lsb = _FT2SawNoteTableLSB
    note_table_msb = _FT2SawNoteTableMSB
.else
    note_table_lsb = _FT2NoteTableLSB
    note_table_msb = _FT2NoteTableMSB
.endif

    lda FT_CHN_NOTE+idx
    bne nocut
    jmp set_volume

nocut:
    clc
    adc FT_ENV_VALUE+env_offset+FT_ENV_NOTE_OFF

.ifblank slide_offset ;  noise channel is a bit special    

    and #$0f
    eor #$0f
    sta tmp
    ldx FT_ENV_VALUE+env_offset+FT_ENV_DUTY_OFF
    lda _FT2DutyLookup, x
    asl a
    and #$80
    ora tmp

.else

    .if(::FT_PITCH_FIX)
        clc
        adc FT_PAL_ADJUST
    .endif
    tax

    FamiToneComputeNoteFinalPitch slide_offset, , note_table_lsb, note_table_msb

    lda pitch+0
    sta reg_lo
    lda pitch+1

    .ifnblank pulse_prev

        .if(!::FT_SFX_ENABLE)
            .if (!.blank(reg_sweep)) && (::FT_SMOOTH_VIBRATO)
                ; Blaarg's smooth vibrato technique, only used if high period delta is 1 or -1.
                tax ; X = new hi-period
                sec
                sbc pulse_prev ; A = signed hi-period delta.
                beq compute_volume
                stx pulse_prev
                tay 
                iny ; we only care about -1 ($ff) and 1. Adding one means we only check of 0 or 2, we already checked for zero (so < 3).
                cpy #$03
                bcs hi_delta_too_big
                ldx #$40
                stx APU_FRAME_CNT ; reset frame counter in case it was about to clock
                lda _FT2SmoothVibratoLoPeriodLookup, y ; be sure low 8 bits of timer period are $ff (for positive delta), or $00 (for negative delta)
                sta reg_lo
                lda _FT2SmoothVibratoSweepLookup, y ; sweep enabled, shift = 7, set negative flag or delta is negative..
                sta reg_sweep
                lda #$c0
                sta APU_FRAME_CNT ; clock sweep immediately
                lda #$08
                sta reg_sweep ; disable sweep
                lda pitch+0
                sta reg_lo ; restore lo-period.
                jmp compute_volume
            hi_delta_too_big:
                stx reg_hi
            .else
                cmp pulse_prev
                beq compute_volume
                sta pulse_prev    
            .endif
        .endif
        
    .endif

    .ifnblank hi_ora
        ora hi_ora
    .endif

.endif ; idx = 3

.if .blank(pulse_prev) || .blank(reg_sweep) || (!::FT_SMOOTH_VIBRATO)
    sta reg_hi
.endif

compute_volume:
    lda FT_ENV_VALUE+env_offset+FT_ENV_VOLUME_OFF
    ora FT_CHN_VOLUME_TRACK+idx ; TODO: Triangle channel doesnt really need a volume track. Make it optional.
    tax
    lda _FT2VolumeTable, x 

.if .defined(FT_VRC6) && idx = 7 
    ; VRC6 saw has 6-bits
    asl
    asl
.endif

set_volume:

.if idx = 0 || idx = 1 || idx = 3 || (idx >= 5 && .defined(::FT_MMC5))
    ldx FT_ENV_VALUE+env_offset+FT_ENV_DUTY_OFF
    ora _FT2DutyLookup, x
.elseif (idx = 5 || idx = 6) && .defined(::FT_VRC6)
    ldx FT_ENV_VALUE+env_offset+FT_ENV_DUTY_OFF
    ora _FT2Vrc6DutyLookup, x
.endif

.ifnblank vol_ora
    ora vol_ora
.endif

    sta reg_vol

.endmacro

.ifdef FT_FDS

.proc FamiToneUpdateFdsChannelSound

    pitch = FT_TEMP_PTR2

    lda FT_CHN_NOTE+5
    bne nocut
    jmp set_volume

nocut:
    clc
    adc FT_ENV_VALUE+FT_CH5_ENVS+FT_ENV_NOTE_OFF
    tax

    FamiToneComputeNoteFinalPitch 3, , _FT2FdsNoteTableLSB, _FT2FdsNoteTableMSB

    lda pitch+0
    sta FDS_FREQ_LO
    lda pitch+1
    sta FDS_FREQ_HI

check_mod_delay:
    lda FT_FDS_MOD_DELAY
    beq zero_delay
    dec FT_FDS_MOD_DELAY
    lda #$80
    sta FDS_MOD_HI
    bne compute_volume

zero_delay:
    lda FT_FDS_MOD_SPEED+1
    sta FDS_MOD_HI
    lda FT_FDS_MOD_SPEED+0
    sta FDS_MOD_LO
    lda FT_FDS_MOD_DEPTH
    ora #$80
    sta FDS_SWEEP_ENV

compute_volume:
    lda FT_ENV_VALUE+FT_CH5_ENVS+FT_ENV_VOLUME_OFF
    ora FT_CHN_VOLUME_TRACK+5 
    tax
    lda _FT2VolumeTable, x 
    asl ; FDS volume is 6-bits, but clamped to 32. Just double it.

set_volume:
    ora #$80
    sta FDS_VOL_ENV
    lda #0
    sta FT_FDS_OVERRIDE_FLAGS

    rts 

.endproc

.endif

.ifdef FT_VRC7

_FT2Vrc7RegLoTable:
    .byte VRC7_REG_LO_1, VRC7_REG_LO_2, VRC7_REG_LO_3, VRC7_REG_LO_4, VRC7_REG_LO_5, VRC7_REG_LO_6
_FT2Vrc7RegHiTable:
    .byte VRC7_REG_HI_1, VRC7_REG_HI_2, VRC7_REG_HI_3, VRC7_REG_HI_4, VRC7_REG_HI_5, VRC7_REG_HI_6
_FT2Vrc7VolTable:
    .byte VRC7_REG_VOL_1, VRC7_REG_VOL_2, VRC7_REG_VOL_3, VRC7_REG_VOL_4, VRC7_REG_VOL_5, VRC7_REG_VOL_6
_FT2Vrc7EnvelopeTable:
    .byte FT_CH5_ENVS, FT_CH6_ENVS, FT_CH7_ENVS, FT_CH8_ENVS, FT_CH9_ENVS, FT_CH10_ENVS 
_FT2Vrc7InvertVolumeTable:
    .byte $0f, $0e, $0d, $0c, $0b, $0a, $09, $08, $07, $06, $05, $04, $03, $02, $01, $00

; y = VRC7 channel idx (0,1,2,3,4,5)
.proc _FT2UpdateVrc7ChannelSound

    chan_idx = FT_TEMP_VAR3
    pitch    = FT_TEMP_PTR2

    lda #0
    sta FT_CHN_INST_CHANGED,y

    lda FT_CHN_VRC7_TRIGGER,y
    bpl check_cut

release:
   
    ; Untrigger note.  
    lda _FT2Vrc7RegHiTable,y
    sta VRC7_REG_SEL
    jsr _FT2Vrc7WaitRegSelect

    lda FT_CHN_PREV_HI, y
    and #$ef ; remove trigger
    sta FT_CHN_PREV_HI, y
    sta VRC7_REG_WRITE
    jsr _FT2Vrc7WaitRegWrite   

    rts

check_cut:

    lda FT_CHN_NOTE+5,y
    bne nocut

cut:  
    ; Untrigger note.  
    lda _FT2Vrc7RegHiTable,y
    sta VRC7_REG_SEL
    jsr _FT2Vrc7WaitRegSelect

    lda FT_CHN_PREV_HI, y
    and #$cf ; remove trigger + sustain
    sta FT_CHN_PREV_HI, y
    sta VRC7_REG_WRITE
    jsr _FT2Vrc7WaitRegWrite

    rts

nocut:

    ; Read note, apply arpeggio 
    clc
    ldx _FT2Vrc7EnvelopeTable,y    
    adc FT_ENV_VALUE+FT_ENV_NOTE_OFF,x
    tax

    ; Apply pitch envelope, fine pitch & slides
    FamiToneComputeNoteFinalPitch 3, {,y}, _FT2Vrc7NoteTableLSB, _FT2Vrc7NoteTableMSB

    ; Compute octave by dividing by 2 until we are <= 512 (0x100).
    ldx #0
    compute_octave_loop:
        lda pitch+1
        cmp #2
        bcc octave_done
        lsr
        sta pitch+1
        ror pitch+0
        inx
        jmp compute_octave_loop

    octave_done:

    ; Write pitch (lo)
    lda _FT2Vrc7RegLoTable,y
    sta VRC7_REG_SEL
    jsr _FT2Vrc7WaitRegSelect

    lda pitch+0
    sta VRC7_REG_WRITE
    jsr _FT2Vrc7WaitRegWrite

    ; Un-trigger previous note if needed.
    lda FT_CHN_PREV_HI, y
    and #$10 ; set trigger.
    beq write_hi_period
    lda FT_CHN_VRC7_TRIGGER,y
    beq write_hi_period
    untrigger_prev_note:
        lda _FT2Vrc7RegHiTable,y
        sta VRC7_REG_SEL
        jsr _FT2Vrc7WaitRegSelect

        lda FT_CHN_PREV_HI,y
        and #$ef ; remove trigger
        sta VRC7_REG_WRITE
        jsr _FT2Vrc7WaitRegWrite

    write_hi_period:

    ; Write pitch (hi)
    lda _FT2Vrc7RegHiTable,y
    sta VRC7_REG_SEL
    jsr _FT2Vrc7WaitRegSelect

    txa
    asl
    ora #$30
    ora pitch+1
    sta FT_CHN_PREV_HI, y
    sta VRC7_REG_WRITE
    jsr _FT2Vrc7WaitRegWrite

    ; Read/multiply volume
    ldx _FT2Vrc7EnvelopeTable,y
    lda FT_ENV_VALUE+FT_ENV_VOLUME_OFF,x
    ora FT_CHN_VOLUME_TRACK+5, y
    tax

    lda #0
    sta FT_CHN_VRC7_TRIGGER,y

update_volume:

    ; Write volume
    lda _FT2Vrc7VolTable,y
    sta VRC7_REG_SEL
    jsr _FT2Vrc7WaitRegSelect

    lda _FT2VolumeTable,x
    tax
    lda _FT2Vrc7InvertVolumeTable,x
    ora FT_CHN_VRC7_PATCH,y
    sta VRC7_REG_WRITE
    jsr _FT2Vrc7WaitRegWrite

    rts

.endproc 

.endif

.ifdef FT_N163

_FT2N163RegLoTable:
    .byte N163_REG_FREQ_LO - $00
    .byte N163_REG_FREQ_LO - $08
    .byte N163_REG_FREQ_LO - $10
    .byte N163_REG_FREQ_LO - $18
    .byte N163_REG_FREQ_LO - $20
    .byte N163_REG_FREQ_LO - $28
    .byte N163_REG_FREQ_LO - $30
    .byte N163_REG_FREQ_LO - $38
_FT2N163RegMidTable:
    .byte N163_REG_FREQ_MID - $00
    .byte N163_REG_FREQ_MID - $08
    .byte N163_REG_FREQ_MID - $10
    .byte N163_REG_FREQ_MID - $18
    .byte N163_REG_FREQ_MID - $20
    .byte N163_REG_FREQ_MID - $28
    .byte N163_REG_FREQ_MID - $30
    .byte N163_REG_FREQ_MID - $38
_FT2N163RegHiTable:
    .byte N163_REG_FREQ_HI - $00
    .byte N163_REG_FREQ_HI - $08
    .byte N163_REG_FREQ_HI - $10
    .byte N163_REG_FREQ_HI - $18
    .byte N163_REG_FREQ_HI - $20
    .byte N163_REG_FREQ_HI - $28
    .byte N163_REG_FREQ_HI - $30
    .byte N163_REG_FREQ_HI - $38
_FT2N163VolTable:
    .byte N163_REG_VOLUME - $00
    .byte N163_REG_VOLUME - $08
    .byte N163_REG_VOLUME - $10
    .byte N163_REG_VOLUME - $18
    .byte N163_REG_VOLUME - $20
    .byte N163_REG_VOLUME - $28
    .byte N163_REG_VOLUME - $30
    .byte N163_REG_VOLUME - $38    
_FT2N163EnvelopeTable:
    .byte FT_CH5_ENVS
    .byte FT_CH6_ENVS
    .byte FT_CH7_ENVS
    .byte FT_CH8_ENVS
    .byte FT_CH9_ENVS
    .byte FT_CH10_ENVS
    .byte FT_CH11_ENVS
    .byte FT_CH12_ENVS

; y = N163 channel idx (0,1,2,3,4,5,6,7)
.proc _FT2UpdateN163ChannelSound
    
    pitch    = FT_TEMP_PTR2
    pitch_hi = FT_TEMP_VAR3

    lda FT_CHN_NOTE+5,y
    bne nocut
    ldx #0 ; this will fetch volume 0.
    bne nocut
    jmp update_volume

nocut:

    ; Read note, apply arpeggio 
    clc
    ldx _FT2N163EnvelopeTable,y
    adc FT_ENV_VALUE+FT_ENV_NOTE_OFF,x
    tax

    ; Apply pitch envelope, fine pitch & slides
    FamiToneComputeNoteFinalPitch 3, {,y}, _FT2N163NoteTableLSB, _FT2N163NoteTableMSB

    ; Convert 16-bit -> 18-bit.
    asl pitch+0
    rol pitch+1
    lda #0
    adc #0
    sta pitch_hi
    asl pitch+0
    rol pitch+1
    rol pitch_hi 

    ; Write pitch
    lda _FT2N163RegLoTable,y
    sta N163_ADDR
    lda pitch+0
    sta N163_DATA
    lda _FT2N163RegMidTable,y
    sta N163_ADDR
    lda pitch+1
    sta N163_DATA
    lda _FT2N163RegHiTable,y
    sta N163_ADDR
    lda FT_CHN_N163_WAVE_LEN,y
    ora pitch_hi
    sta N163_DATA

    ; Read/multiply volume
    ldx _FT2N163EnvelopeTable,y
    lda FT_ENV_VALUE+FT_ENV_VOLUME_OFF,x
    ora FT_CHN_VOLUME_TRACK+5, y
    tax

update_volume:
    ; Write volume
    lda _FT2N163VolTable,y
    sta N163_ADDR
    lda _FT2VolumeTable,x 
    ora #FT_N163_CHN_MASK
    sta N163_DATA
    
    lda #0
    sta FT_CHN_INST_CHANGED,y

    rts

.endproc
.endif

.ifdef FT_S5B

_FT2S5BRegLoTable:
    .byte S5B_REG_LO_A, S5B_REG_LO_B, S5B_REG_LO_C
_FT2S5BRegHiTable:
    .byte S5B_REG_HI_A, S5B_REG_HI_B, S5B_REG_HI_C
_FT2S5BVolTable:
    .byte S5B_REG_VOL_A, S5B_REG_VOL_B, S5B_REG_VOL_C
_FT2S5BEnvelopeTable:
    .byte FT_CH5_ENVS, FT_CH6_ENVS, FT_CH7_ENVS

; y = S5B channel idx (0,1,2)
.proc _FT2UpdateS5BChannelSound
    
    pitch = FT_TEMP_PTR2

    lda FT_CHN_NOTE+5,y
    bne nocut
    ldx #0 ; this will fetch volume 0.
    beq update_volume

nocut:
    
    ; Read note, apply arpeggio 
    clc
    ldx _FT2S5BEnvelopeTable,y
    adc FT_ENV_VALUE+FT_ENV_NOTE_OFF,x
    tax

    ; Apply pitch envelope, fine pitch & slides
    FamiToneComputeNoteFinalPitch 3, {,y}, _FT2NoteTableLSB, _FT2NoteTableMSB

    ; Write pitch
    lda _FT2S5BRegLoTable,y
    sta S5B_ADDR
    lda pitch+0
    sta S5B_DATA
    lda _FT2S5BRegHiTable,y
    sta S5B_ADDR
    lda pitch+1
    sta S5B_DATA

    ; Read/multiply volume
    ldx _FT2S5BEnvelopeTable,y
    lda FT_ENV_VALUE+FT_ENV_VOLUME_OFF,x
    ora FT_CHN_VOLUME_TRACK+5, y
    tax

update_volume:
    ; Write volume
    lda _FT2S5BVolTable,y
    sta S5B_ADDR
    lda _FT2VolumeTable,x 
    sta S5B_DATA

    rts
.endproc
.endif

.macro _FT2UpdateRow channel_idx, env_idx

    .local @no_new_note
.ifdef ::FT_EQUALIZER
    .local @new_note
    .local @done
.endif

    ldx #channel_idx
    jsr _FT2ChannelUpdate
    bcc @no_new_note
    ldx #env_idx
    ldy #channel_idx
    lda FT_CHN_INSTRUMENT+channel_idx

.if .defined(::FT_FDS) && channel_idx >= 5
    jsr _FT2SetFdsInstrument
.elseif .defined(::FT_VRC7) && channel_idx >= 5
    jsr _FT2SetVrc7Instrument
.elseif .defined(::FT_N163) && channel_idx >= 5
    jsr _FT2SetN163Instrument
.else
    jsr _FT2SetInstrument
.endif

.ifdef ::FT_EQUALIZER
    @new_note:
        ldx #channel_idx
        lda #8
        sta FT_CHN_NOTE_COUNTER, x
        jmp @done
    @no_new_note:
        ldx #channel_idx
        lda FT_CHN_NOTE_COUNTER, x
        beq @done
        dec FT_CHN_NOTE_COUNTER, x
    @done:    
.else
    .local @no_new_note ; why do i need this?
    @no_new_note:
.endif

.endmacro

.macro _FT2UpdateRowDpcm channel_idx
.if(::FT_DPCM_ENABLE)
    .local @play_sample
    .local @no_new_note
    ldx #channel_idx    ;process channel 5
    jsr _FT2ChannelUpdate
    bcc @no_new_note
    lda FT_CHN_NOTE+channel_idx
    bne @play_sample
    jsr FamiToneSampleStop
    bne @no_new_note    ;A is non-zero after FamiToneSampleStop
@play_sample:
    jsr FamiToneSamplePlayM

.ifdef ::FT_EQUALIZER
    .local @done
    .local @new_note
    @new_note:
        ldx #channel_idx
        lda #8
        sta FT_CHN_NOTE_COUNTER, x
        jmp @done
    @no_new_note:
        ldx #channel_idx
        lda FT_CHN_NOTE_COUNTER, x
        beq @done
        dec FT_CHN_NOTE_COUNTER, x
    @done:    
.else
    @no_new_note:
.endif

.endif
.endmacro

;------------------------------------------------------------------------------
; update FamiTone state, should be called every NMI
; in: none
;------------------------------------------------------------------------------

.proc FamiToneUpdate

    .if(::FT_THREAD)
    lda FT_TEMP_PTR_L
    pha
    lda FT_TEMP_PTR_H
    pha
    .endif

    lda FT_SONG_SPEED          ;speed 0 means that no music is playing currently
    bmi pause                 ;bit 7 set is the pause flag
    bne update
pause:
    jmp update_sound

;----------------------------------------------------------------------------------------------------------------------
update:

.if !::FT_FAMISTUDIO_TEMPO 
    clc                        ;update frame counter that considers speed, tempo, and PAL/NTSC
    lda FT_TEMPO_ACC_L
    adc FT_TEMPO_STEP_L
    sta FT_TEMPO_ACC_L
    lda FT_TEMPO_ACC_H
    adc FT_TEMPO_STEP_H
    cmp FT_SONG_SPEED
    bcs update_row            ;overflow, row update is needed
    sta FT_TEMPO_ACC_H         ;no row update, skip to the envelopes update
    jmp update_envelopes

update_row:
    sec
    sbc FT_SONG_SPEED
    sta FT_TEMPO_ACC_H

.else ; ::FT_FAMISTUDIO_TEMPO here

    dec FT_TEMPO_ENV_COUNTER
    beq advance_tempo_envelope
    lda #1
    jmp store_frame_count

advance_tempo_envelope:
    lda FT_TEMPO_ENV_PTR_L
    sta FT_TEMP_PTR1+0
    lda FT_TEMPO_ENV_PTR_H
    sta FT_TEMP_PTR1+1

    inc FT_TEMPO_ENV_IDX
    ldy FT_TEMPO_ENV_IDX
    lda (FT_TEMP_PTR1),y
    bpl store_counter

tempo_envelope_end:
    ldy #1
    sty FT_TEMPO_ENV_IDX
    lda (FT_TEMP_PTR1),y

store_counter:
    sta FT_TEMPO_ENV_COUNTER
    lda FT_TEMPO_FRAME_NUM
    bne store_frame_count
    jmp skip_frame

store_frame_count:
    sta FT_TEMPO_FRAME_CNT

update_row:

.endif

    ; TODO: Turn most of these in loops, no reasons to be macros.
    _FT2UpdateRow 0, FT_CH0_ENVS
    _FT2UpdateRow 1, FT_CH1_ENVS
    _FT2UpdateRow 2, FT_CH2_ENVS
    _FT2UpdateRow 3, FT_CH3_ENVS
    _FT2UpdateRowDpcm 4

.ifdef ::FT_VRC6
    _FT2UpdateRow 5, FT_CH5_ENVS
    _FT2UpdateRow 6, FT_CH6_ENVS
    _FT2UpdateRow 7, FT_CH7_ENVS
.endif

.ifdef ::FT_VRC7
    _FT2UpdateRow  5, FT_CH5_ENVS
    _FT2UpdateRow  6, FT_CH6_ENVS
    _FT2UpdateRow  7, FT_CH7_ENVS
    _FT2UpdateRow  8, FT_CH8_ENVS
    _FT2UpdateRow  9, FT_CH9_ENVS
    _FT2UpdateRow 10, FT_CH10_ENVS
.endif

.ifdef ::FT_FDS
    _FT2UpdateRow 5, FT_CH5_ENVS
.endif

.ifdef ::FT_MMC5
    _FT2UpdateRow 5, FT_CH5_ENVS
    _FT2UpdateRow 6, FT_CH6_ENVS
.endif

.ifdef ::FT_S5B
    _FT2UpdateRow 5, FT_CH5_ENVS
    _FT2UpdateRow 6, FT_CH6_ENVS
    _FT2UpdateRow 7, FT_CH7_ENVS
.endif

.ifdef ::FT_N163
    .if ::FT_N163_CHN_CNT >= 1
        _FT2UpdateRow  5, FT_CH5_ENVS
    .endif
    .if ::FT_N163_CHN_CNT >= 2
        _FT2UpdateRow  6, FT_CH6_ENVS
    .endif
    .if ::FT_N163_CHN_CNT >= 3
        _FT2UpdateRow  7, FT_CH7_ENVS
    .endif
    .if ::FT_N163_CHN_CNT >= 4
        _FT2UpdateRow  8, FT_CH8_ENVS
    .endif
    .if ::FT_N163_CHN_CNT >= 5
        _FT2UpdateRow  9, FT_CH9_ENVS
    .endif
    .if ::FT_N163_CHN_CNT >= 6
        _FT2UpdateRow 10, FT_CH10_ENVS
    .endif
    .if ::FT_N163_CHN_CNT >= 7
        _FT2UpdateRow 11, FT_CH11_ENVS
    .endif
    .if ::FT_N163_CHN_CNT >= 8
        _FT2UpdateRow 12, FT_CH12_ENVS
    .endif
.endif

;----------------------------------------------------------------------------------------------------------------------
update_envelopes:
    ldx #0    ;process 11 envelopes

env_process:
    lda FT_ENV_REPEAT,x
    beq env_read  
    dec FT_ENV_REPEAT,x
    bne env_next

env_read:
    lda FT_ENV_ADR_L,x         ;load envelope data address into temp
    sta FT_TEMP_PTR_L
    lda FT_ENV_ADR_H,x
    sta FT_TEMP_PTR_H
    ldy FT_ENV_PTR,x           ;load envelope pointer

env_read_value:
    lda (FT_TEMP_PTR1),y       ;read a byte of the envelope data
    bpl env_special           ;values below 128 used as a special code, loop or repeat
    clc                        ;values above 128 are output value+192 (output values are signed -63..64)
    adc #256-192
    sta FT_ENV_VALUE,x         ;store the output value
    iny                        ;advance the pointer
    bne env_next_store_ptr    ;bra

env_special:
    bne env_set_repeat        ;zero is the loop point, non-zero values used for the repeat counter
    iny                        ;advance the pointer
    lda (FT_TEMP_PTR1),y       ;read loop position
    tay                        ;use loop position
    jmp env_read_value        ;read next byte of the envelope

env_set_repeat:
    iny
    sta FT_ENV_REPEAT,x        ;store the repeat counter value

env_next_store_ptr:
    tya                        ;store the envelope pointer
    sta FT_ENV_PTR,x

env_next:
    inx                        ;next envelope

    cpx #FT_NUM_ENVELOPES
    bne env_process

;----------------------------------------------------------------------------------------------------------------------
update_pitch_envelopes:
    ldx #0
    jmp pitch_env_process

pitch_relate_update_with_last_value:
    lda FT_PITCH_ENV_REPEAT,x
    sec 
    sbc #1
    sta FT_PITCH_ENV_REPEAT,x
    and #$7f 
    beq pitch_env_read
    lda FT_PITCH_ENV_ADR_L,x 
    sta FT_TEMP_PTR_L
    lda FT_PITCH_ENV_ADR_H,x
    sta FT_TEMP_PTR_H
    ldy FT_PITCH_ENV_PTR,x
    dey    
    dey
    lda (FT_TEMP_PTR1),y
    clc  
    adc #256-192
    sta FT_TEMP_VAR2
    clc
    adc FT_PITCH_ENV_VALUE_L,x
    sta FT_PITCH_ENV_VALUE_L,x
    lda FT_TEMP_VAR2
     bpl pitch_relative_last_pos  
    lda #$ff
pitch_relative_last_pos:
    adc FT_PITCH_ENV_VALUE_H,x
    sta FT_PITCH_ENV_VALUE_H,x
    jmp pitch_env_next

pitch_env_process:
    lda FT_PITCH_ENV_REPEAT,x
    cmp #$81
    bcs pitch_relate_update_with_last_value
    and #$7f
    beq pitch_env_read
    dec FT_PITCH_ENV_REPEAT,x
    bne pitch_env_next

pitch_env_read:
    lda FT_PITCH_ENV_ADR_L,x 
    sta FT_TEMP_PTR_L
    lda FT_PITCH_ENV_ADR_H,x
    sta FT_TEMP_PTR_H
    ldy #0
    lda (FT_TEMP_PTR1),y
    sta FT_TEMP_VAR1 ; going to be 0 for absolute envelope, 0x80 for relative.
    ldy FT_PITCH_ENV_PTR,x

pitch_env_read_value:
    lda (FT_TEMP_PTR1),y
    bpl pitch_env_special 
    clc  
    adc #256-192
    bit FT_TEMP_VAR1
    bmi pitch_relative

pitch_absolute:
    sta FT_PITCH_ENV_VALUE_L,x
    ora #0
    bmi pitch_absolute_neg  
    lda #0
    jmp pitch_absolute_set_value_hi
pitch_absolute_neg:
    lda #$ff
pitch_absolute_set_value_hi:
    sta FT_PITCH_ENV_VALUE_H,x
    iny 
    jmp pitch_env_next_store_ptr

pitch_relative:
    sta FT_TEMP_VAR2
    clc
    adc FT_PITCH_ENV_VALUE_L,x
    sta FT_PITCH_ENV_VALUE_L,x
    lda FT_TEMP_VAR2
    and #$80
    bpl pitch_relative_pos  
    lda #$ff
pitch_relative_pos:
    adc FT_PITCH_ENV_VALUE_H,x
    sta FT_PITCH_ENV_VALUE_H,x
    iny 
    jmp pitch_env_next_store_ptr

pitch_env_special:
    bne pitch_env_set_repeat
    iny 
    lda (FT_TEMP_PTR1),y 
    tay
    jmp pitch_env_read_value 

pitch_env_set_repeat:
    iny
    ora FT_TEMP_VAR1 ; this is going to set the relative flag in the hi-bit.
    sta FT_PITCH_ENV_REPEAT,x

pitch_env_next_store_ptr:
    tya 
    sta FT_PITCH_ENV_PTR,x

pitch_env_next:
    inx 

    cpx #FT_NUM_PITCH_ENVELOPES
    bne pitch_env_process

;----------------------------------------------------------------------------------------------------------------------
update_slides:
    ldx #0    ;process 3 slides

slide_process:
    lda FT_SLIDE_STEP,x        ; zero repeat means no active slide.
    beq slide_next
    clc                        ; add step to slide pitch (16bit + 8bit signed).
    lda FT_SLIDE_STEP,x
    adc FT_SLIDE_PITCH_L,x
    sta FT_SLIDE_PITCH_L,x
    lda FT_SLIDE_STEP,x
    and #$80
    beq positive_slide

negative_slide:
    lda #$ff
    adc FT_SLIDE_PITCH_H,x
    sta FT_SLIDE_PITCH_H,x
    bpl slide_next
    jmp clear_slide

positive_slide:
    adc FT_SLIDE_PITCH_H,x
    sta FT_SLIDE_PITCH_H,x
    bmi slide_next

clear_slide:
    lda #0
    sta FT_SLIDE_STEP,x

slide_next:
    inx                        ;next slide
    cpx #FT_NUM_PITCH_ENVELOPES
    bne slide_process

;----------------------------------------------------------------------------------------------------------------------
update_sound:

    FamiToneUpdateChannelSound 0, FT_CH0_ENVS, 0, FT_PULSE1_PREV, , , FT_MR_PULSE1_H, FT_MR_PULSE1_L, FT_MR_PULSE1_V, APU_PL1_SWEEP
    FamiToneUpdateChannelSound 1, FT_CH1_ENVS, 1, FT_PULSE2_PREV, , , FT_MR_PULSE2_H, FT_MR_PULSE2_L, FT_MR_PULSE2_V, APU_PL2_SWEEP
    FamiToneUpdateChannelSound 2, FT_CH2_ENVS, 2, , #$80, , FT_MR_TRI_H, FT_MR_TRI_L, FT_MR_TRI_V
    FamiToneUpdateChannelSound 3, FT_CH3_ENVS,  , , #$f0, , FT_MR_NOISE_F, , FT_MR_NOISE_V

.ifdef ::FT_VRC6
    FamiToneUpdateChannelSound 5, FT_CH5_ENVS, 3, , , #$80, VRC6_PL1_HI, VRC6_PL1_LO, VRC6_PL1_VOL
    FamiToneUpdateChannelSound 6, FT_CH6_ENVS, 4, , , #$80, VRC6_PL2_HI, VRC6_PL2_LO, VRC6_PL2_VOL
    FamiToneUpdateChannelSound 7, FT_CH7_ENVS, 5, , , #$80, VRC6_SAW_HI, VRC6_SAW_LO, VRC6_SAW_VOL
.endif

.ifdef ::FT_MMC5
    FamiToneUpdateChannelSound 5, FT_CH5_ENVS, 3, FT_MMC5_PULSE1_PREV, , , MMC5_PL1_HI, MMC5_PL1_LO, MMC5_PL1_VOL
    FamiToneUpdateChannelSound 6, FT_CH6_ENVS, 4, FT_MMC5_PULSE2_PREV, , , MMC5_PL2_HI, MMC5_PL2_LO, MMC5_PL2_VOL
.endif

.ifdef ::FT_FDS
    jsr FamiToneUpdateFdsChannelSound
.endif

.ifdef ::FT_VRC7
    ldy #0
    vrc7_channel_loop:
        jsr _FT2UpdateVrc7ChannelSound
        iny
        cpy #6
        bne vrc7_channel_loop
.endif

.ifdef ::FT_N163
    ldy #0
    n163_channel_loop:
        jsr _FT2UpdateN163ChannelSound
        iny
        cpy #FT_N163_CHN_CNT
        bne n163_channel_loop
.endif

.ifdef ::FT_S5B
    ldy #0
    s5b_channel_loop:
        jsr _FT2UpdateS5BChannelSound
        iny
        cpy #3
        bne s5b_channel_loop
.endif

.if ::FT_FAMISTUDIO_TEMPO 
    ; See if we need to run a double frame (playing NTSC song on PAL)
    dec FT_TEMPO_FRAME_CNT
    beq skip_frame
    jmp update_row
.endif

skip_frame:

;----------------------------------------------------------------------------------------------------------------------
.if(::FT_SFX_ENABLE)

    ;process all sound effect streams

    .if FT_SFX_STREAMS>0
    ldx #FT_SFX_CH0
    jsr _FT2SfxUpdate
    .endif
    .if FT_SFX_STREAMS>1
    ldx #FT_SFX_CH1
    jsr _FT2SfxUpdat
    .endif
    .if FT_SFX_STREAMS>2
    ldx #FT_SFX_CH2
    jsr _FT2SfxUpdate
    .endif
    .if FT_SFX_STREAMS>3
    ldx #FT_SFX_CH3
    jsr _FT2SfxUpdate
    .endif

    ;send data from the output buffer to the APU

    lda FT_OUT_BUF      ;pulse 1 volume
    sta APU_PL1_VOL
    lda FT_OUT_BUF+1    ;pulse 1 period LSB
    sta APU_PL1_LO
    lda FT_OUT_BUF+2    ;pulse 1 period MSB, only applied when changed
    cmp FT_PULSE1_PREV
    beq no_pulse1_upd
    sta FT_PULSE1_PREV
    sta APU_PL1_HI
no_pulse1_upd:

    lda FT_OUT_BUF+3    ;pulse 2 volume
    sta APU_PL2_VOL
    lda FT_OUT_BUF+4    ;pulse 2 period LSB
    sta APU_PL2_LO
    lda FT_OUT_BUF+5    ;pulse 2 period MSB, only applied when changed
    cmp FT_PULSE2_PREV
    beq no_pulse2_upd
    sta FT_PULSE2_PREV
    sta APU_PL2_HI
no_pulse2_upd:

    lda FT_OUT_BUF+6    ;triangle volume (plays or not)
    sta APU_TRI_LINEAR
    lda FT_OUT_BUF+7    ;triangle period LSB
    sta APU_TRI_LO
    lda FT_OUT_BUF+8    ;triangle period MSB
    sta APU_TRI_HI

    lda FT_OUT_BUF+9    ;noise volume
    sta APU_NOISE_VOL
    lda FT_OUT_BUF+10   ;noise period
    sta APU_NOISE_LO

.endif

    .if(::FT_THREAD)
    pla
    sta FT_TEMP_PTR_H
    pla
    sta FT_TEMP_PTR_L
    .endif

    rts

.endproc

;internal routine, sets up envelopes of a channel according to current instrument
;in X envelope group offset, y = channel idx, A instrument number

.proc _FT2SetInstrument

    ptr = FT_TEMP_PTR1
    wave_ptr = FT_TEMP_PTR2
    chan_idx = FT_TEMP_VAR2
    tmp_x = FT_TEMP_VAR3

    sty chan_idx
    asl                        ;instrument number is pre multiplied by 4
    tay
    lda FT_INSTRUMENT_H
    adc #0                     ;use carry to extend range for 64 instruments
    sta ptr+1
    lda FT_INSTRUMENT_L
    sta ptr+0

    ; Volume envelope
    lda (ptr),y
    sta FT_ENV_ADR_L,x
    iny
    lda (FT_TEMP_PTR1),y
    iny
    sta FT_ENV_ADR_H,x
    inx

    ; Arpeggio envelope
    ; TODO: CLeanup this tmp_x mess, so ugly.
    stx tmp_x
    ldx chan_idx
    lda FT_CHN_ENV_OVERRIDE,x ; instrument arpeggio is overriden by arpeggio, dont touch!
    lsr
    ldx tmp_x
    bcs skip_arpeggio_ptr

read_arpeggio_ptr:    
    lda (FT_TEMP_PTR1),y
    sta FT_ENV_ADR_L,x
    iny
    lda (FT_TEMP_PTR1),y
    sta FT_ENV_ADR_H,x
    jmp init_envelopes
skip_arpeggio_ptr:
    iny

init_envelopes:
    ; Initialize volume + arpeggio envelopes.
    lda #1
    sta FT_ENV_PTR-1,x         ;reset env1 pointer (env1 is volume and volume can have releases)
    lda #0
    sta FT_ENV_REPEAT-1,x      ;reset env1 repeat counter
    sta FT_ENV_REPEAT,x        ;reset env2 repeat counter
    sta FT_ENV_PTR,x           ;reset env2 pointer

    ; Duty cycle envelope
    lda chan_idx
    cmp #2                     ;triangle has no duty.
.if !.defined(::FT_S5B)
    bne duty
.else
    beq no_duty
    cmp #5                     ;S5B has no duty.
    bcc duty
.endif
    no_duty:
        iny
        iny
        bne pitch_env
    duty:
        inx
        iny
        lda (FT_TEMP_PTR1),y
        sta FT_ENV_ADR_L,x
        iny
        lda (FT_TEMP_PTR1),y
        sta FT_ENV_ADR_H,x
        lda #0
        sta FT_ENV_REPEAT,x        ;reset env3 repeat counter
        sta FT_ENV_PTR,x           ;reset env3 pointer
    pitch_env:
    ; Pitch envelopes.
    ldx chan_idx
    lda FT_CHN_ENV_OVERRIDE,x ; instrument pitch is overriden by vibrato, dont touch!
    bmi no_pitch    
    lda _FT2ChannelToPitch, x
    bmi no_pitch
    tax
    lda #1
    sta FT_PITCH_ENV_PTR,x     ;reset env3 pointer (pitch envelope have relative/absolute flag in the first byte)
    lda #0
    sta FT_PITCH_ENV_REPEAT,x  ;reset env3 repeat counter
    sta FT_PITCH_ENV_VALUE_L,x
    sta FT_PITCH_ENV_VALUE_H,x
    iny
    lda (FT_TEMP_PTR1),y       ;instrument pointer LSB
    sta FT_PITCH_ENV_ADR_L,x
    iny
    lda (FT_TEMP_PTR1),y       ;instrument pointer MSB
    sta FT_PITCH_ENV_ADR_H,x
    no_pitch:
    rts

.endproc

.if .defined(FT_FDS) || .defined(FT_N163) || .defined(FT_VRC7) 
.macro _FT2SetExpInstrumentBase

    chan_idx = FT_TEMP_VAR2
    tmp_x = FT_TEMP_VAR3

    sty chan_idx
    asl                        ;instrument number is pre multiplied by 4
    asl
    tay
    lda FT_EXP_INSTRUMENT_H
    adc #0                     ;use carry to extend range for 64 instruments
    sta ptr+1
    lda FT_EXP_INSTRUMENT_L
    sta ptr+0

    ; Volume envelope
    lda (ptr),y
    sta FT_ENV_ADR_L,x
    iny
    lda (ptr),y
    iny
    sta FT_ENV_ADR_H,x
    inx

    ; Arpeggio envelope
    ; TODO: CLeanup this tmp_x mess, so ugly.
    stx tmp_x
    ldx chan_idx
    lda FT_CHN_ENV_OVERRIDE,x ; instrument arpeggio is overriden by arpeggio, dont touch!
    lsr
    ldx tmp_x
    bcs skip_arpeggio_ptr

read_arpeggio_ptr:    
    lda (ptr),y
    sta FT_ENV_ADR_L,x
    iny
    lda (ptr),y
    sta FT_ENV_ADR_H,x
    iny
    jmp init_envelopes
skip_arpeggio_ptr:
    iny
    iny

init_envelopes:
    ; Initialize volume + arpeggio envelopes.
    lda #1
    sta FT_ENV_PTR-1,x         ;reset env1 pointer (env1 is volume and volume can have releases)
    lda #0
    sta FT_ENV_REPEAT-1,x      ;reset env1 repeat counter
    sta FT_ENV_REPEAT,x        ;reset env2 repeat counter
    sta FT_ENV_PTR,x           ;reset env2 pointer

    ; Pitch envelopes.
    ldx chan_idx
    lda FT_CHN_ENV_OVERRIDE,x ; instrument pitch is overriden by vibrato, dont touch!
    bpl pitch_env
    iny
    iny
    bne pitch_overriden

    pitch_env:
    dex
    dex                        ; Noise + DPCM dont have pitch envelopes             
    lda #1
    sta FT_PITCH_ENV_PTR,x     ;reset env3 pointer (pitch envelope have relative/absolute flag in the first byte)
    lda #0
    sta FT_PITCH_ENV_REPEAT,x  ;reset env3 repeat counter
    sta FT_PITCH_ENV_VALUE_L,x
    sta FT_PITCH_ENV_VALUE_H,x
    lda (ptr),y       ;instrument pointer LSB
    sta FT_PITCH_ENV_ADR_L,x
    iny
    lda (ptr),y       ;instrument pointer MSB
    sta FT_PITCH_ENV_ADR_H,x
    iny

    pitch_overriden:

.endmacro
.endif

.ifdef FT_VRC7
.proc _FT2SetVrc7Instrument

    ptr = FT_TEMP_PTR1

    _FT2SetExpInstrumentBase

    lda chan_idx
    sec
    sbc #5
    tax

    lda FT_CHN_INST_CHANGED,x
    beq done

    lda (ptr),y
    sta FT_CHN_VRC7_PATCH, x
    bne done

    read_custom_patch:
    ldx #0
    iny
    read_patch_loop:
        stx VRC7_REG_SEL
        jsr _FT2Vrc7WaitRegSelect
        lda (ptr),y
        iny
        sta VRC7_REG_WRITE
        jsr _FT2Vrc7WaitRegWrite
        inx
        cpx #8
        bne read_patch_loop

    done:
    rts

.endproc
.endif

.ifdef FT_FDS
.proc _FT2SetFdsInstrument

    ptr        = FT_TEMP_PTR1
    wave_ptr   = FT_TEMP_PTR2
    master_vol = FT_TEMP_VAR2
    tmp_y      = FT_TEMP_VAR3

    _FT2SetExpInstrumentBase

    lda #0
    sta FDS_SWEEP_BIAS

    lda FT_CHN_INST_CHANGED
    bne write_fds_wave

    iny ; Skip master volume + wave + mod envelope.
    iny
    iny
    iny
    iny

    jmp load_mod_param

    write_fds_wave:

        lda (ptr),y
        sta master_vol
        iny

        ora #$80
        sta FDS_VOL ; Enable wave RAM write

        ; FDS Waveform
        lda (ptr),y
        sta wave_ptr+0
        iny
        lda (ptr),y
        sta wave_ptr+1
        iny
        sty tmp_y

        ldy #0
        wave_loop:
            lda (wave_ptr),y
            sta FDS_WAV_START,y
            iny
            cpy #64
            bne wave_loop

        lda #$80
        sta FDS_MOD_HI ; Need to disable modulation before writing.
        lda master_vol
        sta FDS_VOL ; Disable RAM write.
        lda #0
        sta FDS_SWEEP_BIAS

        ; FDS Modulation
        ldy tmp_y
        lda (ptr),y
        sta wave_ptr+0
        iny
        lda (ptr),y
        sta wave_ptr+1
        iny
        sty tmp_y

        ldy #0
        mod_loop:
            lda (wave_ptr),y
            sta FDS_MOD_TABLE
            iny
            cpy #32
            bne mod_loop

        lda #0
        sta FT_CHN_INST_CHANGED

        ldy tmp_y

    load_mod_param:

        check_mod_speed:
            bit FT_FDS_OVERRIDE_FLAGS
            bmi mod_speed_overriden

            load_mod_speed:
                lda (ptr),y
                sta FT_FDS_MOD_SPEED+0
                iny
                lda (ptr),y
                sta FT_FDS_MOD_SPEED+1
                jmp check_mod_depth

            mod_speed_overriden:
                iny

        check_mod_depth:
            iny
            bit FT_FDS_OVERRIDE_FLAGS
            bvs mod_depth_overriden

            load_mod_depth:
                lda (ptr),y
                sta FT_FDS_MOD_DEPTH

            mod_depth_overriden:
                iny
                lda (ptr),y
                sta FT_FDS_MOD_DELAY

    rts

.endproc
.endif

.ifdef FT_N163

_FT2N163WaveTable:
    .byte N163_REG_WAVE - $00
    .byte N163_REG_WAVE - $08
    .byte N163_REG_WAVE - $10
    .byte N163_REG_WAVE - $18
    .byte N163_REG_WAVE - $20
    .byte N163_REG_WAVE - $28
    .byte N163_REG_WAVE - $30
    .byte N163_REG_WAVE - $38

.proc _FT2SetN163Instrument

    ptr      = FT_TEMP_PTR1
    wave_ptr = FT_TEMP_PTR2
    wave_len = FT_TEMP_VAR1
    wave_pos = FT_TEMP_VAR2
    tmp_y    = FT_TEMP_VAR3

    _FT2SetExpInstrumentBase

    ; Wave position
    lda chan_idx
    sec
    sbc #5
    tax

    lda FT_CHN_INST_CHANGED,x
    beq done

    lda _FT2N163WaveTable, x
    sta N163_ADDR
    lda (ptr),y
    sta wave_pos
    sta N163_DATA
    iny

    ; Wave length
    lda (ptr),y
    sta wave_len
    lda #$00 ; 256 - wave length
    sec
    sbc wave_len
    sec
    sbc wave_len
    sta FT_CHN_N163_WAVE_LEN, x
    iny

    ; N163 wave pointer.
    lda (ptr),y
    sta wave_ptr+0
    iny
    lda (ptr),y
    sta wave_ptr+1

    ; N163 wave
    lda wave_pos
    ora #$80
    sta N163_ADDR
    ldy #0
    wave_loop:
        lda (wave_ptr),y
        sta N163_DATA
        iny
        cpy wave_len
        bne wave_loop

    done:
    rts

.endproc
.endif

;internal routine, parses channel note data

.proc _FT2ChannelUpdate

    no_attack_flag = FT_TEMP_VAR3
    slide_delta_lo = FT_TEMP_PTR2_H

.if .defined(::FT_VRC6)
    exp_note_start = 7
    exp_note_table_lsb = _FT2SawNoteTableLSB
    exp_note_table_msb = _FT2SawNoteTableMSB
.elseif .defined(::FT_VRC7)
    exp_note_start = 5
    exp_note_table_lsb = _FT2Vrc7NoteTableLSB
    exp_note_table_msb = _FT2Vrc7NoteTableMSB
.elseif .defined(::FT_N163)
    exp_note_start = 5
    exp_note_table_lsb = _FT2N163NoteTableLSB
    exp_note_table_msb = _FT2N163NoteTableMSB
.elseif .defined(::FT_FDS)
    exp_note_start = 5
    exp_note_table_lsb = _FT2FdsNoteTableLSB
    exp_note_table_msb = _FT2FdsNoteTableMSB
.endif

    lda FT_CHN_REPEAT,x        ;check repeat counter
    beq no_repeat
    dec FT_CHN_REPEAT,x        ;decrease repeat counter
    clc                        ;no new note
    rts

no_repeat:
    lda #0
    sta no_attack_flag
    lda FT_CHN_PTR_L,x         ;load channel pointer into temp
    sta FT_TEMP_PTR_L
    lda FT_CHN_PTR_H,x
    sta FT_TEMP_PTR_H
    ldy #0

read_byte:
    lda (FT_TEMP_PTR1),y       ;read byte of the channel
    inc_16 FT_TEMP_PTR1

check_regular_note:
    cmp #$61
    bcs check_special_code    ; $00 to $60 are regular notes, most common case.
    jmp regular_note

check_special_code:
    ora #0
    bpl check_volume_track
    jmp special_code           ;bit 7 0=note 1=special code

check_volume_track:
    cmp #$70
    bcc special_code_6x

volume_track:    
    and #$0f
    asl ; a LUT would be nice, but x/y are both in-use here.
    asl
    asl
    asl
    sta FT_CHN_VOLUME_TRACK,x
    jmp read_byte

special_code_6x:
    stx FT_TEMP_VAR1
    and #$0f
    tax
    lda special_code_jmp_lo-1,x
    sta FT_TEMP_PTR2+0
    lda special_code_jmp_hi-1,x
    sta FT_TEMP_PTR2+1
    ldx FT_TEMP_VAR1
    jmp (FT_TEMP_PTR2)

.ifdef ::FT_FDS

fds_mod_depth:    
    lda (FT_TEMP_PTR1),y
    inc_16 FT_TEMP_PTR1
    sta FT_FDS_MOD_DEPTH
    lda #$40
    ora FT_FDS_OVERRIDE_FLAGS
    sta FT_FDS_OVERRIDE_FLAGS
    jmp read_byte

fds_mod_speed:
    lda (FT_TEMP_PTR1),y
    sta FT_FDS_MOD_SPEED+0
    iny
    lda (FT_TEMP_PTR1),y
    sta FT_FDS_MOD_SPEED+1
    add_16_8 FT_TEMP_PTR1, #2
    lda #$80
    ora FT_FDS_OVERRIDE_FLAGS
    sta FT_FDS_OVERRIDE_FLAGS
    dey
    jmp read_byte

.endif

fine_pitch:
    stx FT_TEMP_VAR1
    lda _FT2ChannelToPitch,x
    tax
    lda (FT_TEMP_PTR1),y
    inc_16 FT_TEMP_PTR1
    sta FT_PITCH_FINE_VALUE,x
    ldx FT_TEMP_VAR1
    jmp read_byte 

clear_pitch_override_flag:
    lda #$7f
    and FT_CHN_ENV_OVERRIDE,x
    sta FT_CHN_ENV_OVERRIDE,x
    jmp read_byte 

clear_arpeggio_override_flag:
    lda #$fe
    and FT_CHN_ENV_OVERRIDE,x
    sta FT_CHN_ENV_OVERRIDE,x
    jmp read_byte

override_pitch_envelope:
    lda #$80
    ora FT_CHN_ENV_OVERRIDE,x
    sta FT_CHN_ENV_OVERRIDE,x
    stx FT_TEMP_VAR1
    lda _FT2ChannelToPitch,x
    tax
    lda (FT_TEMP_PTR1),y
    sta FT_PITCH_ENV_ADR_L,x
    iny
    lda (FT_TEMP_PTR1),y
    sta FT_PITCH_ENV_ADR_H,x
    lda #0
    tay
    sta FT_PITCH_ENV_REPEAT,x
    lda #1
    sta FT_PITCH_ENV_PTR,x
    ldx FT_TEMP_VAR1
    add_16_8 FT_TEMP_PTR1, #2
    jmp read_byte 

override_arpeggio_envelope:
    lda #$01
    ora FT_CHN_ENV_OVERRIDE,x
    sta FT_CHN_ENV_OVERRIDE,x
    stx FT_TEMP_VAR1    
    lda _FT2ChannelToArpeggioEnvelope,x
    tax    
    lda (FT_TEMP_PTR1),y
    sta FT_ENV_ADR_L,x
    iny
    lda (FT_TEMP_PTR1),y
    sta FT_ENV_ADR_H,x
    lda #0
    tay
    sta FT_ENV_REPEAT,x ; Reset the envelope since this might be a no-attack note.
    sta FT_ENV_VALUE,x
    sta FT_ENV_PTR,x
    ldx FT_TEMP_VAR1
    add_16_8 FT_TEMP_PTR1, #2
    jmp read_byte

reset_arpeggio:
    stx FT_TEMP_VAR1    
    lda _FT2ChannelToArpeggioEnvelope,x
    tax
    lda #0
    sta FT_ENV_REPEAT,x
    sta FT_ENV_VALUE,x
    sta FT_ENV_PTR,x
    ldx FT_TEMP_VAR1
    jmp read_byte

disable_attack:
    lda #1
    sta no_attack_flag    
    jmp read_byte 

slide:
    stx FT_TEMP_VAR1
    lda _FT2ChannelToSlide,x
    tax
    lda (FT_TEMP_PTR1),y       ; read slide step size
    iny
    sta FT_SLIDE_STEP,x
    lda (FT_TEMP_PTR1),y       ; read slide note from
.if(::FT_PITCH_FIX)
    clc
    adc FT_PAL_ADJUST
.endif
    sta FT_TEMP_VAR2
    iny
    lda (FT_TEMP_PTR1),y       ; read slide note to
    ldy FT_TEMP_VAR2           ; start note
.if(::FT_PITCH_FIX)
    adc FT_PAL_ADJUST
.endif
    stx FT_TEMP_VAR2           ; store slide index.    
    tax
.ifdef exp_note_start
    lda FT_TEMP_VAR1
    cmp #exp_note_start
    bcs note_table_expansion
.endif
    sec                        ; subtract the pitch of both notes.
    lda _FT2NoteTableLSB,y
    sbc _FT2NoteTableLSB,x
    sta slide_delta_lo
    lda _FT2NoteTableMSB,y
    sbc _FT2NoteTableMSB,x
.ifdef exp_note_start
    jmp note_table_done
note_table_expansion:
    sec
    lda exp_note_table_lsb,y
    sbc exp_note_table_lsb,x
    sta slide_delta_lo
    lda exp_note_table_msb,y
    sbc exp_note_table_msb,x
note_table_done:
.endif
    ldx FT_TEMP_VAR2           ; slide index.
    sta FT_SLIDE_PITCH_H,x
    .if .defined(::FT_N163) || .defined(::FT_VRC7)
        cpx #3 ; slide #3 is the first of expansion slides.
        bcs positive_shift
    .endif
    negative_shift:
        lda slide_delta_lo
        asl                        ; shift-left, we have 1 bit of fractional slide.
        sta FT_SLIDE_PITCH_L,x
        rol FT_SLIDE_PITCH_H,x     ; shift-left, we have 1 bit of fractional slide.
    .if .defined(::FT_N163) || .defined(::FT_VRC7)
        jmp shift_done
    positive_shift:
        lda slide_delta_lo
        sta FT_SLIDE_PITCH_L,x
        .if ::FT_PITCH_SHIFT >= 1
            lda FT_SLIDE_PITCH_H,x
            cmp #$80
            ror FT_SLIDE_PITCH_H,x 
            ror FT_SLIDE_PITCH_L,x
        .if ::FT_PITCH_SHIFT >= 2
            lda FT_SLIDE_PITCH_H,x
            cmp #$80
            ror FT_SLIDE_PITCH_H,x 
            ror FT_SLIDE_PITCH_L,x
        .if ::FT_PITCH_SHIFT >= 3
            lda FT_SLIDE_PITCH_H,x
            cmp #$80
            ror FT_SLIDE_PITCH_H,x 
            ror FT_SLIDE_PITCH_L,x
        .if ::FT_PITCH_SHIFT >= 4
            lda FT_SLIDE_PITCH_H,x
            cmp #$80
            ror FT_SLIDE_PITCH_H,x 
            ror FT_SLIDE_PITCH_L,x
        .if ::FT_PITCH_SHIFT >= 5
            lda FT_SLIDE_PITCH_H,x
            cmp #$80
            ror FT_SLIDE_PITCH_H,x 
            ror FT_SLIDE_PITCH_L,x
        .endif 
        .endif
        .endif
        .endif
        .endif
    shift_done:
    .endif
    ldx FT_TEMP_VAR1
    ldy #2
    lda (FT_TEMP_PTR1),y       ; re-read the target note (ugly...)
    sta FT_CHN_NOTE,x          ; store note code
    add_16_8 FT_TEMP_PTR1, #3

slide_done_pos:
    ldy #0
    jmp sec_and_done

regular_note:    
    sta FT_CHN_NOTE,x          ; store note code
    ldy _FT2ChannelToSlide,x   ; clear any previous slide on new node.
    bmi sec_and_done
    lda #0
    sta FT_SLIDE_STEP,y
sec_and_done:
    lda no_attack_flag
    bne no_attack
    lda FT_CHN_NOTE,x          ; dont trigger attack on stop notes.
    beq no_attack
.if .defined(::FT_VRC7)
    cpx #5
    bcs vrc7_channel
    sec                        ;new note flag is set
    jmp done
    vrc7_channel:
        lda #1
        sta FT_CHN_VRC7_TRIGGER-5,x ; set trigger flag for VRC7
.endif    
    sec                        ;new note flag is set
    jmp done
no_attack:
    clc                        ;pretend there is no new note.
    jmp done

special_code:
    and #$7f
    lsr a
    bcs set_empty_rows
    asl a
    asl a
    sta FT_CHN_INSTRUMENT,x    ;store instrument number*4

.if .defined(::FT_N163) || .defined(::FT_VRC7) || .defined(::FT_FDS)
    cpx #5
    bcc regular_channel
        lda #1
        sta FT_CHN_INST_CHANGED-5, x
    regular_channel:
.endif
    jmp read_byte 

set_speed:
.if ::FT_FAMISTUDIO_TEMPO 
    lda (FT_TEMP_PTR1),y
    sta FT_TEMPO_ENV_PTR_L
    sta FT_TEMP_PTR2+0
    iny
    lda (FT_TEMP_PTR1),y
    sta FT_TEMPO_ENV_PTR_H
    sta FT_TEMP_PTR2+1
    ldy #0
    sty FT_TEMPO_ENV_IDX
    lda (FT_TEMP_PTR2),y
    sta FT_TEMPO_ENV_COUNTER
    add_16_8 FT_TEMP_PTR1, #2
.else
    lda (FT_TEMP_PTR1),y
    sta FT_SONG_SPEED
    inc_16 FT_TEMP_PTR1
.endif
    jmp read_byte 

set_loop:
    lda (FT_TEMP_PTR1),y
    sta FT_TEMP_VAR1
    iny
    lda (FT_TEMP_PTR1),y
    sta FT_TEMP_PTR_H
    lda FT_TEMP_VAR1
    sta FT_TEMP_PTR_L
    dey
    jmp read_byte

set_empty_rows:
    cmp #$3d
    beq set_speed
    cmp #$3c
    beq release_note
    bcc set_repeat
    cmp #$3e
    beq set_loop

set_reference:
    clc                        ;remember return address+3
    lda FT_TEMP_PTR_L
    adc #3
    sta FT_CHN_RETURN_L,x
    lda FT_TEMP_PTR_H
    adc #0
    sta FT_CHN_RETURN_H,x
    lda (FT_TEMP_PTR1),y       ;read length of the reference (how many rows)
    sta FT_CHN_REF_LEN,x
    iny
    lda (FT_TEMP_PTR1),y       ;read 16-bit absolute address of the reference
    sta FT_TEMP_VAR1          ;remember in temp
    iny
    lda (FT_TEMP_PTR1),y
    sta FT_TEMP_PTR_H
    lda FT_TEMP_VAR1
    sta FT_TEMP_PTR_L
    ldy #0
    jmp read_byte

release_note:

.ifdef ::FT_VRC7
    cpx #5
    bcc apu_channel
    lda #$80
    sta FT_CHN_VRC7_TRIGGER-5,x ; set release flag for VRC7
    apu_channel:
.endif    

    stx FT_TEMP_VAR1
    lda _FT2ChannelToVolumeEnvelope,x ; DPCM(5) will never have releases.
    tax

    lda FT_ENV_ADR_L,x         ;load envelope data address into temp
    sta FT_TEMP_PTR2_L
    lda FT_ENV_ADR_H,x
    sta FT_TEMP_PTR2_H    
    
    ldy #0
    lda (FT_TEMP_PTR2),y       ;read first byte of the envelope data, this contains the release index.
    beq env_has_no_release

    sta FT_ENV_PTR,x
    lda #0
    sta FT_ENV_REPEAT,x        ;need to reset envelope repeat to force update.
    
env_has_no_release:
    ldx FT_TEMP_VAR1
    clc
    jmp done

set_repeat:
    sta FT_CHN_REPEAT,x        ;set up repeat counter, carry is clear, no new note

done:
    lda FT_CHN_REF_LEN,x       ;check reference row counter
    beq no_ref                ;if it is zero, there is no reference
    dec FT_CHN_REF_LEN,x       ;decrease row counter
    bne no_ref

    lda FT_CHN_RETURN_L,x      ;end of a reference, return to previous pointer
    sta FT_CHN_PTR_L,x
    lda FT_CHN_RETURN_H,x
    sta FT_CHN_PTR_H,x
    rts

no_ref:
    lda FT_TEMP_PTR_L
    sta FT_CHN_PTR_L,x
    lda FT_TEMP_PTR_H
    sta FT_CHN_PTR_H,x
    rts

.endproc

special_code_jmp_lo:
    .byte <_FT2ChannelUpdate::slide                        ; $61
    .byte <_FT2ChannelUpdate::disable_attack               ; $62
    .byte <_FT2ChannelUpdate::override_pitch_envelope      ; $63
    .byte <_FT2ChannelUpdate::override_arpeggio_envelope   ; $64
    .byte <_FT2ChannelUpdate::clear_pitch_override_flag    ; $65
    .byte <_FT2ChannelUpdate::clear_arpeggio_override_flag ; $66
    .byte <_FT2ChannelUpdate::reset_arpeggio               ; $67
    .byte <_FT2ChannelUpdate::fine_pitch                   ; $68
.ifdef ::FT_FDS        
    .byte <_FT2ChannelUpdate::fds_mod_speed                ; $69
    .byte <_FT2ChannelUpdate::fds_mod_depth                ; $6a
.endif        
special_code_jmp_hi:
    .byte >_FT2ChannelUpdate::slide                        ; $61
    .byte >_FT2ChannelUpdate::disable_attack               ; $62
    .byte >_FT2ChannelUpdate::override_pitch_envelope      ; $63
    .byte >_FT2ChannelUpdate::override_arpeggio_envelope   ; $64
    .byte >_FT2ChannelUpdate::clear_pitch_override_flag    ; $65
    .byte >_FT2ChannelUpdate::clear_arpeggio_override_flag ; $66
    .byte >_FT2ChannelUpdate::reset_arpeggio               ; $67
    .byte >_FT2ChannelUpdate::fine_pitch                   ; $68
.ifdef ::FT_FDS        
    .byte >_FT2ChannelUpdate::fds_mod_speed                ; $69
    .byte >_FT2ChannelUpdate::fds_mod_depth                ; $6a
.endif

;------------------------------------------------------------------------------
; stop DPCM sample if it plays
;------------------------------------------------------------------------------

.proc FamiToneSampleStop

    lda #%00001111
    sta APU_SND_CHN

    rts

.endproc

    
.if(FT_DPCM_ENABLE)

;------------------------------------------------------------------------------
; play DPCM sample with higher priority, for sound effects
; in: A is number of a sample, 1..63
;------------------------------------------------------------------------------

.proc FamiToneSamplePlay

    ldx #1
    stx FT_DPCM_EFFECT

_FT2SamplePlay:

    sta FT_TEMP               ;sample number*3, offset in the sample table
    asl a
    clc
    adc FT_TEMP
    
    adc FT_DPCM_LIST_L
    sta FT_TEMP_PTR_L
    lda #0
    adc FT_DPCM_LIST_H
    sta FT_TEMP_PTR_H

    lda #%00001111             ;stop DPCM
    sta APU_SND_CHN

    ldy #0
    lda (FT_TEMP_PTR1),y       ;sample offset
    sta APU_DMC_START
    iny
    lda (FT_TEMP_PTR1),y       ;sample length
    sta APU_DMC_LEN
    iny
    lda (FT_TEMP_PTR1),y       ;pitch and loop
    sta APU_DMC_FREQ

    lda #32                    ;reset DAC counter
    sta APU_DMC_RAW
    lda #%00011111             ;start DMC
    sta APU_SND_CHN

    rts

.endproc 

;------------------------------------------------------------------------------
; play DPCM sample, used by music player, could be used externally
; in: A is number of a sample, 1..63
;------------------------------------------------------------------------------

.proc FamiToneSamplePlayM           ;for music (low priority)

    ldx FT_DPCM_EFFECT
    beq FamiToneSamplePlay::_FT2SamplePlay
    tax
    lda APU_SND_CHN
    and #16
    beq not_busy
    rts

not_busy:
    sta FT_DPCM_EFFECT
    txa
    jmp FamiToneSamplePlay::_FT2SamplePlay

.endproc 

.endif

.if(FT_SFX_ENABLE)

;------------------------------------------------------------------------------
; init sound effects player, set pointer to data
; in: X,Y is address of sound effects data
;------------------------------------------------------------------------------

.proc FamiToneSfxInit

    stx FT_TEMP_PTR_L
    sty FT_TEMP_PTR_H
    
    ldy #0
    
    .if(::FT_PITCH_FIX)

    lda FT_PAL_ADJUST          ;add 2 to the sound list pointer for PAL
    bne ntsc
    iny
    iny
ntsc:

    .endif
    
    lda (FT_TEMP_PTR1),y       ;read and store pointer to the effects list
    sta FT_SFX_ADR_L
    iny
    lda (FT_TEMP_PTR1),y
    sta FT_SFX_ADR_H

    ldx #FT_SFX_CH0            ;init all the streams

set_channels:
    jsr _FT2SfxClearChannel
    txa
    clc
    adc #FT_SFX_STRUCT_SIZE
    tax
    cpx #FT_SFX_STRUCT_SIZE*FT_SFX_STREAMS
    bne set_channels

    rts

.endproc 

;internal routine, clears output buffer of a sound effect
;in: A is 0
;    X is offset of sound effect stream

.proc _FT2SfxClearChannel

    lda #0
    sta FT_SFX_PTR_H,x         ;this stops the effect
    sta FT_SFX_REPEAT,x
    sta FT_SFX_OFF,x
    sta FT_SFX_BUF+6,x         ;mute triangle
    lda #$30
    sta FT_SFX_BUF+0,x         ;mute pulse1
    sta FT_SFX_BUF+3,x         ;mute pulse2
    sta FT_SFX_BUF+9,x         ;mute noise

    rts

.endproc 

;------------------------------------------------------------------------------
; play sound effect
; in: A is a number of the sound effect 0..127
;     X is offset of sound effect channel, should be FT_SFX_CH0..FT_SFX_CH3
;------------------------------------------------------------------------------

.proc FamiToneSfxPlay

    asl a                      ;get offset in the effects list
    tay

    jsr _FT2SfxClearChannel    ;stops the effect if it plays

    lda FT_SFX_ADR_L
    sta FT_TEMP_PTR_L
    lda FT_SFX_ADR_H
    sta FT_TEMP_PTR_H

    lda (FT_TEMP_PTR1),y       ;read effect pointer from the table
    sta FT_SFX_PTR_L,x         ;store it
    iny
    lda (FT_TEMP_PTR1),y
    sta FT_SFX_PTR_H,x         ;this write enables the effect

    rts

.endproc 

;internal routine, update one sound effect stream
;in: X is offset of sound effect stream

.proc _FT2SfxUpdate

    lda FT_SFX_REPEAT,x        ;check if repeat counter is not zero
    beq no_repeat
    dec FT_SFX_REPEAT,x        ;decrement and return
    bne update_buf            ;just mix with output buffer

no_repeat:
    lda FT_SFX_PTR_H,x         ;check if MSB of the pointer is not zero
    bne sfx_active
    rts                        ;return otherwise, no active effect

sfx_active:
    sta FT_TEMP_PTR_H         ;load effect pointer into temp
    lda FT_SFX_PTR_L,x
    sta FT_TEMP_PTR_L
    ldy FT_SFX_OFF,x
    clc

read_byte:
    lda (FT_TEMP_PTR1),y       ;read byte of effect
    bmi get_data              ;if bit 7 is set, it is a register write
    beq eof
    iny
    sta FT_SFX_REPEAT,x        ;if bit 7 is reset, it is number of repeats
    tya
    sta FT_SFX_OFF,x
    jmp update_buf

get_data:
    iny
    stx FT_TEMP_VAR1          ;it is a register write
    adc FT_TEMP_VAR1          ;get offset in the effect output buffer
    tax
    lda (FT_TEMP_PTR1),y       ;read value
    iny
    sta FT_SFX_BUF-128,x       ;store into output buffer
    ldx FT_TEMP_VAR1
    jmp read_byte             ;and read next byte

eof:
    sta FT_SFX_PTR_H,x         ;mark channel as inactive

update_buf:

    lda FT_OUT_BUF             ;compare effect output buffer with main output buffer
    and #$0f                   ;if volume of pulse 1 of effect is higher than that of the
    sta FT_TEMP_VAR1          ;main buffer, overwrite the main buffer value with the new one
    lda FT_SFX_BUF+0,x
    and #$0f
    cmp FT_TEMP_VAR1
    bcc no_pulse1
    lda FT_SFX_BUF+0,x
    sta FT_OUT_BUF+0
    lda FT_SFX_BUF+1,x
    sta FT_OUT_BUF+1
    lda FT_SFX_BUF+2,x
    sta FT_OUT_BUF+2
no_pulse1:

    lda FT_OUT_BUF+3           ;same for pulse 2
    and #$0f
    sta FT_TEMP_VAR1
    lda FT_SFX_BUF+3,x
    and #$0f
    cmp FT_TEMP_VAR1
    bcc no_pulse2
    lda FT_SFX_BUF+3,x
    sta FT_OUT_BUF+3
    lda FT_SFX_BUF+4,x
    sta FT_OUT_BUF+4
    lda FT_SFX_BUF+5,x
    sta FT_OUT_BUF+5
no_pulse2:

    lda FT_SFX_BUF+6,x           ;overwrite triangle of main output buffer if it is active
    beq no_triangle
    sta FT_OUT_BUF+6
    lda FT_SFX_BUF+7,x
    sta FT_OUT_BUF+7
    lda FT_SFX_BUF+8,x
    sta FT_OUT_BUF+8
no_triangle:

    lda FT_OUT_BUF+9           ;same as for pulse 1 and 2, but for noise
    and #$0f
    sta FT_TEMP_VAR1
    lda FT_SFX_BUF+9,x
    and #$0f
    cmp FT_TEMP_VAR1
    bcc no_noise
    lda FT_SFX_BUF+9,x
    sta FT_OUT_BUF+9
    lda FT_SFX_BUF+10,x
    sta FT_OUT_BUF+10
no_noise:

    rts

.endproc 

.endif


;dummy envelope used to initialize all channels with silence

_FT2DummyEnvelope:
    .byte $c0,$7f,$00,$00

_FT2DummyPitchEnvelope:
    .byte $00,$c0,$7f,$00,$01

;PAL and NTSC, 11-bit dividers
;rest note, then octaves 1-5, then three zeroes
;first 64 bytes are PAL, next 64 bytes are NTSC

_FT2NoteTableLSB:
    .if(FT_PAL_SUPPORT)
        .byte $00
        .byte $68, $b6, $0e, $6f, $d9, $4b, $c6, $48, $d1, $60, $f6, $92 ; Octave 0
        .byte $34, $db, $86, $37, $ec, $a5, $62, $23, $e8, $b0, $7b, $49 ; Octave 1
        .byte $19, $ed, $c3, $9b, $75, $52, $31, $11, $f3, $d7, $bd, $a4 ; Octave 2
        .byte $8c, $76, $61, $4d, $3a, $29, $18, $08, $f9, $eb, $de, $d1 ; Octave 3
        .byte $c6, $ba, $b0, $a6, $9d, $94, $8b, $84, $7c, $75, $6e, $68 ; Octave 4
        .byte $62, $5d, $57, $52, $4e, $49, $45, $41, $3e, $3a, $37, $34 ; Octave 5
        .byte $31, $2e, $2b, $29, $26, $24, $22, $20, $1e, $1d, $1b, $19 ; Octave 6
        .byte $18, $16, $15, $14, $13, $12, $11, $10, $0f, $0e, $0d, $0c ; Octave 7
    .endif
    .if(FT_NTSC_SUPPORT)
        .byte $00
        .byte $5b, $9c, $e6, $3b, $9a, $01, $72, $ea, $6a, $f1, $7f, $13 ; Octave 0
        .byte $ad, $4d, $f3, $9d, $4c, $00, $b8, $74, $34, $f8, $bf, $89 ; Octave 1
        .byte $56, $26, $f9, $ce, $a6, $80, $5c, $3a, $1a, $fb, $df, $c4 ; Octave 2
        .byte $ab, $93, $7c, $67, $52, $3f, $2d, $1c, $0c, $fd, $ef, $e1 ; Octave 3
        .byte $d5, $c9, $bd, $b3, $a9, $9f, $96, $8e, $86, $7e, $77, $70 ; Octave 4
        .byte $6a, $64, $5e, $59, $54, $4f, $4b, $46, $42, $3f, $3b, $38 ; Octave 5
        .byte $34, $31, $2f, $2c, $29, $27, $25, $23, $21, $1f, $1d, $1b ; Octave 6
        .byte $1a, $18, $17, $15, $14, $13, $12, $11, $10, $0f, $0e, $0d ; Octave 7
    .endif

_FT2NoteTableMSB:
    .if(FT_PAL_SUPPORT)
        .byte $00
        .byte $0c, $0b, $0b, $0a, $09, $09, $08, $08, $07, $07, $06, $06 ; Octave 0
        .byte $06, $05, $05, $05, $04, $04, $04, $04, $03, $03, $03, $03 ; Octave 1
        .byte $03, $02, $02, $02, $02, $02, $02, $02, $01, $01, $01, $01 ; Octave 2
        .byte $01, $01, $01, $01, $01, $01, $01, $01, $00, $00, $00, $00 ; Octave 3
        .byte $00, $00, $00, $00, $00, $00, $00, $00, $00, $00, $00, $00 ; Octave 4
        .byte $00, $00, $00, $00, $00, $00, $00, $00, $00, $00, $00, $00 ; Octave 5
        .byte $00, $00, $00, $00, $00, $00, $00, $00, $00, $00, $00, $00 ; Octave 6
        .byte $00, $00, $00, $00, $00, $00, $00, $00, $00, $00, $00, $00 ; Octave 7
    .endif
    .if(FT_NTSC_SUPPORT)
        .byte $00
        .byte $0d, $0c, $0b, $0b, $0a, $0a, $09, $08, $08, $07, $07, $07 ; Octave 0
        .byte $06, $06, $05, $05, $05, $05, $04, $04, $04, $03, $03, $03 ; Octave 1
        .byte $03, $03, $02, $02, $02, $02, $02, $02, $02, $01, $01, $01 ; Octave 2
        .byte $01, $01, $01, $01, $01, $01, $01, $01, $01, $00, $00, $00 ; Octave 3
        .byte $00, $00, $00, $00, $00, $00, $00, $00, $00, $00, $00, $00 ; Octave 4
        .byte $00, $00, $00, $00, $00, $00, $00, $00, $00, $00, $00, $00 ; Octave 5
        .byte $00, $00, $00, $00, $00, $00, $00, $00, $00, $00, $00, $00 ; Octave 6
        .byte $00, $00, $00, $00, $00, $00, $00, $00, $00, $00, $00, $00 ; Octave 7
    .endif

.ifdef FT_VRC6
_FT2SawNoteTableLSB:
    .byte $00
    .byte $44, $69, $9a, $d6, $1e, $70, $cb, $30, $9e, $13, $91, $16 ; Octave 0
    .byte $a2, $34, $cc, $6b, $0e, $b7, $65, $18, $ce, $89, $48, $0a ; Octave 1
    .byte $d0, $99, $66, $35, $07, $db, $b2, $8b, $67, $44, $23, $05 ; Octave 2
    .byte $e8, $cc, $b2, $9a, $83, $6d, $59, $45, $33, $22, $11, $02 ; Octave 3
    .byte $f3, $e6, $d9, $cc, $c1, $b6, $ac, $a2, $99, $90, $88, $80 ; Octave 4
    .byte $79, $72, $6c, $66, $60, $5b, $55, $51, $4c, $48, $44, $40 ; Octave 5
    .byte $3c, $39, $35, $32, $2f, $2d, $2a, $28, $25, $23, $21, $1f ; Octave 6
    .byte $1e, $1c, $1a, $19, $17, $16, $15, $13, $12, $11, $10, $0f ; Octave 7
_FT2SawNoteTableMSB:    
    .byte $00
    .byte $0f, $0e, $0d, $0c, $0c, $0b, $0a, $0a, $09, $09, $08, $08 ; Octave 0
    .byte $07, $07, $06, $06, $06, $05, $05, $05, $04, $04, $04, $04 ; Octave 1
    .byte $03, $03, $03, $03, $03, $02, $02, $02, $02, $02, $02, $02 ; Octave 2
    .byte $01, $01, $01, $01, $01, $01, $01, $01, $01, $01, $01, $01 ; Octave 3
    .byte $00, $00, $00, $00, $00, $00, $00, $00, $00, $00, $00, $00 ; Octave 4
    .byte $00, $00, $00, $00, $00, $00, $00, $00, $00, $00, $00, $00 ; Octave 5
    .byte $00, $00, $00, $00, $00, $00, $00, $00, $00, $00, $00, $00 ; Octave 6
    .byte $00, $00, $00, $00, $00, $00, $00, $00, $00, $00, $00, $00 ; Octave 7
.endif

.ifdef FT_VRC7
_FT2Vrc7NoteTableLSB:
    .byte $00
    .byte $ac, $b7, $c2, $cd, $d9, $e6, $f4, $02, $12, $22, $33, $46 ; Octave 0
    .byte $58, $6e, $84, $9a, $b2, $cc, $e8, $04, $24, $44, $66, $8c ; Octave 1
    .byte $b0, $dc, $08, $34, $64, $98, $d0, $08, $48, $88, $cc, $18 ; Octave 2
    .byte $60, $b8, $10, $68, $c8, $30, $a0, $10, $90, $10, $98, $30 ; Octave 3
    .byte $c0, $70, $20, $d0, $90, $60, $40, $20, $20, $20, $30, $60 ; Octave 4
    .byte $80, $e0, $40, $a0, $20, $c0, $80, $40, $40, $40, $60, $c0 ; Octave 5
    .byte $00, $c0, $80, $40, $40, $80, $00, $80, $80, $80, $c0, $80 ; Octave 6
    .byte $00, $80, $00, $80, $80, $00, $00, $00, $00, $00, $80, $00 ; Octave 7
_FT2Vrc7NoteTableMSB:
    .byte $00
    .byte $00, $00, $00, $00, $00, $00, $00, $01, $01, $01, $01, $01 ; Octave 0
    .byte $01, $01, $01, $01, $01, $01, $01, $02, $02, $02, $02, $02 ; Octave 1
    .byte $02, $02, $03, $03, $03, $03, $03, $04, $04, $04, $04, $05 ; Octave 2
    .byte $05, $05, $06, $06, $06, $07, $07, $08, $08, $09, $09, $0a ; Octave 3
    .byte $0a, $0b, $0c, $0c, $0d, $0e, $0f, $10, $11, $12, $13, $14 ; Octave 4
    .byte $15, $16, $18, $19, $1b, $1c, $1e, $20, $22, $24, $26, $28 ; Octave 5
    .byte $2b, $2d, $30, $33, $36, $39, $3d, $40, $44, $48, $4c, $51 ; Octave 6
    .byte $56, $5b, $61, $66, $6c, $73, $7a, $81, $89, $91, $99, $a3 ; Octave 7    
.endif

.ifdef FT_FDS
_FT2FdsNoteTableLSB:
    .byte $00
    .byte $13, $14, $16, $17, $18, $1a, $1b, $1d, $1e, $20, $22, $24 ; Octave 0
    .byte $26, $29, $2b, $2e, $30, $33, $36, $39, $3d, $40, $44, $48 ; Octave 1
    .byte $4d, $51, $56, $5b, $61, $66, $6c, $73, $7a, $81, $89, $91 ; Octave 2
    .byte $99, $a2, $ac, $b6, $c1, $cd, $d9, $e6, $f3, $02, $11, $21 ; Octave 3
    .byte $33, $45, $58, $6d, $82, $99, $b2, $cb, $e7, $04, $22, $43 ; Octave 4
    .byte $65, $8a, $b0, $d9, $04, $32, $63, $97, $cd, $07, $44, $85 ; Octave 5
    .byte $ca, $13, $60, $b2, $09, $65, $c6, $2d, $9b, $0e, $89, $0b ; Octave 6
    .byte $94, $26, $c1, $64, $12, $ca, $8c, $5b, $35, $1d, $12, $16 ; Octave 7
_FT2FdsNoteTableMSB:
    .byte $00
    .byte $00, $00, $00, $00, $00, $00, $00, $00, $00, $00, $00, $00 ; Octave 0
    .byte $00, $00, $00, $00, $00, $00, $00, $00, $00, $00, $00, $00 ; Octave 1
    .byte $00, $00, $00, $00, $00, $00, $00, $00, $00, $00, $00, $00 ; Octave 2
    .byte $00, $00, $00, $00, $00, $00, $00, $00, $00, $01, $01, $01 ; Octave 3
    .byte $01, $01, $01, $01, $01, $01, $01, $01, $01, $02, $02, $02 ; Octave 4
    .byte $02, $02, $02, $02, $03, $03, $03, $03, $03, $04, $04, $04 ; Octave 5
    .byte $04, $05, $05, $05, $06, $06, $06, $07, $07, $08, $08, $09 ; Octave 6
    .byte $09, $0a, $0a, $0b, $0c, $0c, $0d, $0e, $0f, $10, $11, $12 ; Octave 7
.endif

.ifdef FT_N163
.if FT_N163_CHN_CNT = 1
    _FT2N163NoteTableLSB:
        .byte $00
        .byte $47,$4c,$50,$55,$5a,$5f,$65,$6b,$72,$78,$80,$87 ; Octave 0
        .byte $8f,$98,$a1,$aa,$b5,$bf,$cb,$d7,$e4,$f1,$00,$0f ; Octave 1
        .byte $1f,$30,$42,$55,$6a,$7f,$96,$ae,$c8,$e3,$00,$1e ; Octave 2
        .byte $3e,$60,$85,$ab,$d4,$ff,$2c,$5d,$90,$c6,$00,$3d ; Octave 3
        .byte $7d,$c1,$0a,$57,$a8,$fe,$59,$ba,$20,$8d,$00,$7a ; Octave 4
        .byte $fb,$83,$14,$ae,$50,$fd,$b3,$74,$41,$1a,$00,$f4 ; Octave 5
        .byte $f6,$07,$29,$5c,$a1,$fa,$67,$e9,$83,$35,$01,$e8 ; Octave 6
        .byte $ec,$0f,$52,$b8,$43,$f4,$ce,$d3,$06,$6a,$02,$d1 ; Octave 7
    _FT2N163NoteTableMSB:
        .byte $00
        .byte $00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00 ; Octave 0
        .byte $00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$01,$01 ; Octave 1
        .byte $01,$01,$01,$01,$01,$01,$01,$01,$01,$01,$02,$02 ; Octave 2
        .byte $02,$02,$02,$02,$02,$02,$03,$03,$03,$03,$04,$04 ; Octave 3
        .byte $04,$04,$05,$05,$05,$05,$06,$06,$07,$07,$08,$08 ; Octave 4
        .byte $08,$09,$0a,$0a,$0b,$0b,$0c,$0d,$0e,$0f,$10,$10 ; Octave 5
        .byte $11,$13,$14,$15,$16,$17,$19,$1a,$1c,$1e,$20,$21 ; Octave 6
        .byte $23,$26,$28,$2a,$2d,$2f,$32,$35,$39,$3c,$40,$43 ; Octave 7
.elseif FT_N163_CHN_CNT = 2
    _FT2N163NoteTableLSB:
        .byte $00
        .byte $8f,$98,$a1,$aa,$b5,$bf,$cb,$d7,$e4,$f1,$00,$0f ; Octave 0
        .byte $1f,$30,$42,$55,$6a,$7f,$96,$ae,$c8,$e3,$00,$1e ; Octave 1
        .byte $3e,$60,$85,$ab,$d4,$ff,$2c,$5d,$90,$c6,$00,$3d ; Octave 2
        .byte $7d,$c1,$0a,$57,$a8,$fe,$59,$ba,$20,$8d,$00,$7a ; Octave 3
        .byte $fb,$83,$14,$ae,$50,$fd,$b3,$74,$41,$1a,$00,$f4 ; Octave 4
        .byte $f6,$07,$29,$5c,$a1,$fa,$67,$e9,$83,$35,$01,$e8 ; Octave 5
        .byte $ec,$0f,$52,$b8,$43,$f4,$ce,$d3,$06,$6a,$02,$d1 ; Octave 6
        .byte $d9,$1f,$a5,$71,$86,$e8,$9c,$a7,$0d,$d5,$05,$a2 ; Octave 7
    _FT2N163NoteTableMSB:
        .byte $00
        .byte $00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$01,$01 ; Octave 0
        .byte $01,$01,$01,$01,$01,$01,$01,$01,$01,$01,$02,$02 ; Octave 1
        .byte $02,$02,$02,$02,$02,$02,$03,$03,$03,$03,$04,$04 ; Octave 2
        .byte $04,$04,$05,$05,$05,$05,$06,$06,$07,$07,$08,$08 ; Octave 3
        .byte $08,$09,$0a,$0a,$0b,$0b,$0c,$0d,$0e,$0f,$10,$10 ; Octave 4
        .byte $11,$13,$14,$15,$16,$17,$19,$1a,$1c,$1e,$20,$21 ; Octave 5
        .byte $23,$26,$28,$2a,$2d,$2f,$32,$35,$39,$3c,$40,$43 ; Octave 6
        .byte $47,$4c,$50,$55,$5a,$5f,$65,$6b,$72,$78,$80,$87 ; Octave 7
.elseif FT_N163_CHN_CNT = 3
    _FT2N163NoteTableLSB:
        .byte $00
        .byte $d7,$e4,$f1,$00,$0f,$1f,$30,$42,$56,$6a,$80,$96 ; Octave 0
        .byte $af,$c8,$e3,$00,$1f,$3f,$61,$85,$ac,$d5,$00,$2d ; Octave 1
        .byte $5e,$91,$c7,$01,$3e,$7e,$c3,$0b,$58,$aa,$00,$5b ; Octave 2
        .byte $bc,$22,$8f,$02,$7c,$fd,$86,$17,$b1,$54,$00,$b7 ; Octave 3
        .byte $78,$45,$1f,$05,$f9,$fb,$0d,$2f,$62,$a8,$01,$6e ; Octave 4
        .byte $f1,$8b,$3e,$0a,$f2,$f7,$1a,$5e,$c5,$50,$02,$dc ; Octave 5
        .byte $e3,$17,$7c,$15,$e4,$ee,$35,$bd,$8a,$a0,$04,$b9 ; Octave 6
        .byte $c6,$2e,$f8,$2a,$c9,$dc,$6a,$7a,$14,$40,$08,$73 ; Octave 7
    _FT2N163NoteTableMSB:
        .byte $00
        .byte $00,$00,$00,$01,$01,$01,$01,$01,$01,$01,$01,$01 ; Octave 0
        .byte $01,$01,$01,$02,$02,$02,$02,$02,$02,$02,$03,$03 ; Octave 1
        .byte $03,$03,$03,$04,$04,$04,$04,$05,$05,$05,$06,$06 ; Octave 2
        .byte $06,$07,$07,$08,$08,$08,$09,$0a,$0a,$0b,$0c,$0c ; Octave 3
        .byte $0d,$0e,$0f,$10,$10,$11,$13,$14,$15,$16,$18,$19 ; Octave 4
        .byte $1a,$1c,$1e,$20,$21,$23,$26,$28,$2a,$2d,$30,$32 ; Octave 5
        .byte $35,$39,$3c,$40,$43,$47,$4c,$50,$55,$5a,$60,$65 ; Octave 6
        .byte $6b,$72,$78,$80,$87,$8f,$98,$a1,$ab,$b5,$c0,$cb ; Octave 7
.elseif FT_N163_CHN_CNT = 4
_FT2N163NoteTableLSB:
        .byte $00
        .byte $1f,$30,$42,$55,$6a,$7f,$96,$ae,$c8,$e3,$00,$1e ; Octave 0
        .byte $3e,$60,$85,$ab,$d4,$ff,$2c,$5d,$90,$c6,$00,$3d ; Octave 1
        .byte $7d,$c1,$0a,$57,$a8,$fe,$59,$ba,$20,$8d,$00,$7a ; Octave 2
        .byte $fb,$83,$14,$ae,$50,$fd,$b3,$74,$41,$1a,$00,$f4 ; Octave 3
        .byte $f6,$07,$29,$5c,$a1,$fa,$67,$e9,$83,$35,$01,$e8 ; Octave 4
        .byte $ec,$0f,$52,$b8,$43,$f4,$ce,$d3,$06,$6a,$02,$d1 ; Octave 5
        .byte $d9,$1f,$a5,$71,$86,$e8,$9c,$a7,$0d,$d5,$05,$a2 ; Octave 6
        .byte $b2,$3e,$4b,$e3,$0c,$d0,$38,$4e,$1b,$ab,$ff,$ff ; Octave 7
    _FT2N163NoteTableMSB:
        .byte $00
        .byte $01,$01,$01,$01,$01,$01,$01,$01,$01,$01,$02,$02 ; Octave 0
        .byte $02,$02,$02,$02,$02,$02,$03,$03,$03,$03,$04,$04 ; Octave 1
        .byte $04,$04,$05,$05,$05,$05,$06,$06,$07,$07,$08,$08 ; Octave 2
        .byte $08,$09,$0a,$0a,$0b,$0b,$0c,$0d,$0e,$0f,$10,$10 ; Octave 3
        .byte $11,$13,$14,$15,$16,$17,$19,$1a,$1c,$1e,$20,$21 ; Octave 4
        .byte $23,$26,$28,$2a,$2d,$2f,$32,$35,$39,$3c,$40,$43 ; Octave 5
        .byte $47,$4c,$50,$55,$5a,$5f,$65,$6b,$72,$78,$80,$87 ; Octave 6
        .byte $8f,$98,$a1,$aa,$b5,$bf,$cb,$d7,$e4,$f1,$ff,$ff ; Octave 7
.elseif FT_N163_CHN_CNT = 5
    _FT2N163NoteTableLSB:
        .byte $00
        .byte $67,$7c,$93,$ab,$c4,$df,$fc,$1a,$3a,$5c,$80,$a6 ; Octave 0
        .byte $ce,$f9,$26,$56,$89,$bf,$f8,$34,$74,$b8,$00,$4c ; Octave 1
        .byte $9c,$f2,$4c,$ac,$12,$7e,$f0,$69,$e9,$70,$00,$98 ; Octave 2
        .byte $39,$e4,$99,$59,$24,$fc,$e0,$d2,$d2,$e1,$00,$31 ; Octave 3
        .byte $73,$c9,$33,$b3,$49,$f8,$c0,$a4,$a4,$c2,$01,$62 ; Octave 4
        .byte $e7,$93,$67,$67,$93,$f1,$81,$48,$48,$85,$03,$c5 ; Octave 5
        .byte $cf,$26,$cf,$ce,$27,$e2,$03,$90,$91,$0b,$06,$8a ; Octave 6
        .byte $9f,$4d,$9e,$9c,$4f,$c4,$06,$ff,$ff,$ff,$ff,$ff ; Octave 7
    _FT2N163NoteTableMSB:
        .byte $00
        .byte $01,$01,$01,$01,$01,$01,$01,$02,$02,$02,$02,$02 ; Octave 0
        .byte $02,$02,$03,$03,$03,$03,$03,$04,$04,$04,$05,$05 ; Octave 1
        .byte $05,$05,$06,$06,$07,$07,$07,$08,$08,$09,$0a,$0a ; Octave 2
        .byte $0b,$0b,$0c,$0d,$0e,$0e,$0f,$10,$11,$12,$14,$15 ; Octave 3
        .byte $16,$17,$19,$1a,$1c,$1d,$1f,$21,$23,$25,$28,$2a ; Octave 4
        .byte $2c,$2f,$32,$35,$38,$3b,$3f,$43,$47,$4b,$50,$54 ; Octave 5
        .byte $59,$5f,$64,$6a,$71,$77,$7f,$86,$8e,$97,$a0,$a9 ; Octave 6
        .byte $b3,$be,$c9,$d5,$e2,$ef,$fe,$ff,$ff,$ff,$ff,$ff ; Octave 7
.elseif FT_N163_CHN_CNT = 6
    _FT2N163NoteTableLSB:
        .byte $00
        .byte $af,$c8,$e3,$00,$1f,$3f,$61,$85,$ac,$d5,$00,$2d ; Octave 0
        .byte $5e,$91,$c7,$01,$3e,$7e,$c3,$0b,$58,$aa,$00,$5b ; Octave 1
        .byte $bc,$22,$8f,$02,$7c,$fd,$86,$17,$b1,$54,$00,$b7 ; Octave 2
        .byte $78,$45,$1f,$05,$f9,$fb,$0d,$2f,$62,$a8,$01,$6e ; Octave 3
        .byte $f1,$8b,$3e,$0a,$f2,$f7,$1a,$5e,$c5,$50,$02,$dc ; Octave 4
        .byte $e3,$17,$7c,$15,$e4,$ee,$35,$bd,$8a,$a0,$04,$b9 ; Octave 5
        .byte $c6,$2e,$f8,$2a,$c9,$dc,$6a,$7a,$14,$40,$08,$73 ; Octave 6
        .byte $8c,$5d,$f1,$ff,$ff,$ff,$ff,$ff,$ff,$ff,$ff,$ff ; Octave 7
    _FT2N163NoteTableMSB:
        .byte $00
        .byte $01,$01,$01,$02,$02,$02,$02,$02,$02,$02,$03,$03 ; Octave 0
        .byte $03,$03,$03,$04,$04,$04,$04,$05,$05,$05,$06,$06 ; Octave 1
        .byte $06,$07,$07,$08,$08,$08,$09,$0a,$0a,$0b,$0c,$0c ; Octave 2
        .byte $0d,$0e,$0f,$10,$10,$11,$13,$14,$15,$16,$18,$19 ; Octave 3
        .byte $1a,$1c,$1e,$20,$21,$23,$26,$28,$2a,$2d,$30,$32 ; Octave 4
        .byte $35,$39,$3c,$40,$43,$47,$4c,$50,$55,$5a,$60,$65 ; Octave 5
        .byte $6b,$72,$78,$80,$87,$8f,$98,$a1,$ab,$b5,$c0,$cb ; Octave 6
        .byte $d7,$e4,$f1,$ff,$ff,$ff,$ff,$ff,$ff,$ff,$ff,$ff ; Octave 7
.elseif FT_N163_CHN_CNT = 7
    _FT2N163NoteTableLSB:
        .byte $00
        .byte $f6,$14,$34,$56,$79,$9f,$c7,$f1,$1e,$4d,$80,$b5 ; Octave 0
        .byte $ed,$29,$69,$ac,$f3,$3e,$8e,$e3,$3c,$9b,$00,$6a ; Octave 1
        .byte $db,$53,$d2,$58,$e6,$7d,$1d,$c6,$79,$37,$00,$d5 ; Octave 2
        .byte $b7,$a6,$a4,$b0,$cd,$fa,$3a,$8c,$f3,$6e,$01,$ab ; Octave 3
        .byte $6f,$4d,$48,$61,$9a,$f5,$74,$19,$e6,$dd,$02,$56 ; Octave 4
        .byte $de,$9b,$91,$c3,$35,$eb,$e8,$32,$cc,$bb,$04,$ad ; Octave 5
        .byte $bc,$36,$22,$86,$6b,$d6,$d1,$64,$98,$76,$09,$5b ; Octave 6
        .byte $79,$ff,$ff,$ff,$ff,$ff,$ff,$ff,$ff,$ff,$ff,$ff ; Octave 7
    _FT2N163NoteTableMSB:
        .byte $00
        .byte $01,$02,$02,$02,$02,$02,$02,$02,$03,$03,$03,$03 ; Octave 0
        .byte $03,$04,$04,$04,$04,$05,$05,$05,$06,$06,$07,$07 ; Octave 1
        .byte $07,$08,$08,$09,$09,$0a,$0b,$0b,$0c,$0d,$0e,$0e ; Octave 2
        .byte $0f,$10,$11,$12,$13,$14,$16,$17,$18,$1a,$1c,$1d ; Octave 3
        .byte $1f,$21,$23,$25,$27,$29,$2c,$2f,$31,$34,$38,$3b ; Octave 4
        .byte $3e,$42,$46,$4a,$4f,$53,$58,$5e,$63,$69,$70,$76 ; Octave 5
        .byte $7d,$85,$8d,$95,$9e,$a7,$b1,$bc,$c7,$d3,$e0,$ed ; Octave 6
        .byte $fb,$ff,$ff,$ff,$ff,$ff,$ff,$ff,$ff,$ff,$ff,$ff ; Octave 7
.elseif FT_N163_CHN_CNT = 8
    _FT2N163NoteTableLSB:
        .byte $00
        .byte $3e,$60,$85,$ab,$d4,$ff,$2c,$5d,$90,$c6,$00,$3d ; Octave 0
        .byte $7d,$c1,$0a,$57,$a8,$fe,$59,$ba,$20,$8d,$00,$7a ; Octave 1
        .byte $fb,$83,$14,$ae,$50,$fd,$b3,$74,$41,$1a,$00,$f4 ; Octave 2
        .byte $f6,$07,$29,$5c,$a1,$fa,$67,$e9,$83,$35,$01,$e8 ; Octave 3
        .byte $ec,$0f,$52,$b8,$43,$f4,$ce,$d3,$06,$6a,$02,$d1 ; Octave 4
        .byte $d9,$1f,$a5,$71,$86,$e8,$9c,$a7,$0d,$d5,$05,$a2 ; Octave 5
        .byte $b2,$3e,$4b,$e3,$0c,$d0,$38,$4e,$1b,$ab,$ff,$ff ; Octave 6
        .byte $ff,$ff,$ff,$ff,$ff,$ff,$ff,$ff,$ff,$ff,$ff,$ff ; Octave 7
    _FT2N163NoteTableMSB:
        .byte $00
        .byte $02,$02,$02,$02,$02,$02,$03,$03,$03,$03,$04,$04 ; Octave 0
        .byte $04,$04,$05,$05,$05,$05,$06,$06,$07,$07,$08,$08 ; Octave 1
        .byte $08,$09,$0a,$0a,$0b,$0b,$0c,$0d,$0e,$0f,$10,$10 ; Octave 2
        .byte $11,$13,$14,$15,$16,$17,$19,$1a,$1c,$1e,$20,$21 ; Octave 3
        .byte $23,$26,$28,$2a,$2d,$2f,$32,$35,$39,$3c,$40,$43 ; Octave 4
        .byte $47,$4c,$50,$55,$5a,$5f,$65,$6b,$72,$78,$80,$87 ; Octave 5
        .byte $8f,$98,$a1,$aa,$b5,$bf,$cb,$d7,$e4,$f1,$ff,$ff ; Octave 6
        .byte $ff,$ff,$ff,$ff,$ff,$ff,$ff,$ff,$ff,$ff,$ff,$ff ; Octave 7
.endif
.endif

_FT2ChannelToVolumeEnvelope:
    .byte FT_CH0_ENVS+FT_ENV_VOLUME_OFF
    .byte FT_CH1_ENVS+FT_ENV_VOLUME_OFF
    .byte FT_CH2_ENVS+FT_ENV_VOLUME_OFF
    .byte FT_CH3_ENVS+FT_ENV_VOLUME_OFF
    .byte $ff
.if .defined(FT_CH5_ENVS)
    .byte FT_CH5_ENVS+FT_ENV_VOLUME_OFF
.endif
.if .defined(FT_CH6_ENVS)
    .byte FT_CH6_ENVS+FT_ENV_VOLUME_OFF
.endif
.if .defined(FT_CH7_ENVS)
    .byte FT_CH7_ENVS+FT_ENV_VOLUME_OFF
.endif
.if .defined(FT_CH8_ENVS)
    .byte FT_CH8_ENVS+FT_ENV_VOLUME_OFF
.endif
.if .defined(FT_CH9_ENVS)
    .byte FT_CH9_ENVS+FT_ENV_VOLUME_OFF
.endif
.if .defined(FT_CH10_ENVS)
    .byte FT_CH10_ENVS+FT_ENV_VOLUME_OFF
.endif
.if .defined(FT_CH11_ENVS)
    .byte FT_CH11_ENVS+FT_ENV_VOLUME_OFF
.endif
.if .defined(FT_CH12_ENVS)
    .byte FT_CH12_ENVS+FT_ENV_VOLUME_OFF
.endif

_FT2ChannelToArpeggioEnvelope:
    .byte FT_CH0_ENVS+FT_ENV_NOTE_OFF
    .byte FT_CH1_ENVS+FT_ENV_NOTE_OFF
    .byte FT_CH2_ENVS+FT_ENV_NOTE_OFF
    .byte FT_CH3_ENVS+FT_ENV_NOTE_OFF
    .byte $ff
.if .defined(FT_CH5_ENVS)
    .byte FT_CH5_ENVS+FT_ENV_NOTE_OFF
.endif
.if .defined(FT_CH6_ENVS)
    .byte FT_CH6_ENVS+FT_ENV_NOTE_OFF
.endif
.if .defined(FT_CH7_ENVS)
    .byte FT_CH7_ENVS+FT_ENV_NOTE_OFF
.endif
.if .defined(FT_CH8_ENVS)
    .byte FT_CH8_ENVS+FT_ENV_NOTE_OFF
.endif
.if .defined(FT_CH9_ENVS)
    .byte FT_CH9_ENVS+FT_ENV_NOTE_OFF
.endif
.if .defined(FT_CH10_ENVS)
    .byte FT_CH10_ENVS+FT_ENV_NOTE_OFF
.endif
.if .defined(FT_CH11_ENVS)
    .byte FT_CH11_ENVS+FT_ENV_NOTE_OFF
.endif
.if .defined(FT_CH12_ENVS)
    .byte FT_CH12_ENVS+FT_ENV_NOTE_OFF
.endif

_FT2ChannelToPitch:
_FT2ChannelToSlide:
    .byte $00
    .byte $01
    .byte $02
    .byte $ff ; no slide for noise
    .byte $ff ; no slide for DPCM
.if FT_NUM_PITCH_ENVELOPES >= 4
    .byte $03
.endif
.if FT_NUM_PITCH_ENVELOPES >= 5
    .byte $04
.endif    
.if FT_NUM_PITCH_ENVELOPES >= 6
    .byte $05
.endif
.if FT_NUM_PITCH_ENVELOPES >= 7
    .byte $06
.endif
.if FT_NUM_PITCH_ENVELOPES >= 8
    .byte $07
.endif
.if FT_NUM_PITCH_ENVELOPES >= 9
    .byte $08
.endif
.if FT_NUM_PITCH_ENVELOPES >= 10
    .byte $09
.endif
.if FT_NUM_PITCH_ENVELOPES >= 11
    .byte $0a
.endif

_FT2DutyLookup:
    .byte $30
    .byte $70
    .byte $b0
    .byte $f0

.ifdef FT_VRC6
_FT2Vrc6DutyLookup:
    .byte $00
    .byte $10
    .byte $20
    .byte $30
    .byte $40
    .byte $50
    .byte $60
    .byte $70
.endif

.if(FT_FAMISTUDIO_TEMPO)
_FT2FamiStudioTempoFrameLookup:
    .byte $01, $02 ; NTSC -> NTSC, NTSC -> PAL
    .byte $00, $01 ; PAL  -> NTSC, PAL  -> PAL
.endif

.if(FT_SMOOTH_VIBRATO)
; lookup table for the 2 registers we need to set for smooth vibrato.
; Index 0 decrement the hi-period, index 2 increments. Index 1 is unused. 
_FT2SmoothVibratoLoPeriodLookup:
	.byte $00, $00, $ff
_FT2SmoothVibratoSweepLookup:
	.byte $8f, $00, $87
.endif

; Precomputed volume multiplication table (rounded but never to zero unless one of the value is zero).
; Load the 2 volumes in the lo/hi nibble and fetch.

_FT2VolumeTable:
    .byte $00, $00, $00, $00, $00, $00, $00, $00, $00, $00, $00, $00, $00, $00, $00, $00
    .byte $00, $01, $01, $01, $01, $01, $01, $01, $01, $01, $01, $01, $01, $01, $01, $01
    .byte $00, $01, $01, $01, $01, $01, $01, $01, $01, $01, $01, $01, $02, $02, $02, $02
    .byte $00, $01, $01, $01, $01, $01, $01, $01, $02, $02, $02, $02, $02, $03, $03, $03
    .byte $00, $01, $01, $01, $01, $01, $02, $02, $02, $02, $03, $03, $03, $03, $04, $04
    .byte $00, $01, $01, $01, $01, $02, $02, $02, $03, $03, $03, $04, $04, $04, $05, $05
    .byte $00, $01, $01, $01, $02, $02, $02, $03, $03, $04, $04, $04, $05, $05, $06, $06
    .byte $00, $01, $01, $01, $02, $02, $03, $03, $04, $04, $05, $05, $06, $06, $07, $07
    .byte $00, $01, $01, $02, $02, $03, $03, $04, $04, $05, $05, $06, $06, $07, $07, $08
    .byte $00, $01, $01, $02, $02, $03, $04, $04, $05, $05, $06, $07, $07, $08, $08, $09
    .byte $00, $01, $01, $02, $03, $03, $04, $05, $05, $06, $07, $07, $08, $09, $09, $0a
    .byte $00, $01, $01, $02, $03, $04, $04, $05, $06, $07, $07, $08, $09, $0a, $0a, $0b
    .byte $00, $01, $02, $02, $03, $04, $05, $06, $06, $07, $08, $09, $0a, $0a, $0b, $0c
    .byte $00, $01, $02, $03, $03, $04, $05, $06, $07, $08, $09, $0a, $0a, $0b, $0c, $0d
    .byte $00, $01, $02, $03, $04, $05, $06, $07, $07, $08, $09, $0a, $0b, $0c, $0d, $0e
    .byte $00, $01, $02, $03, $04, $05, $06, $07, $08, $09, $0a, $0b, $0c, $0d, $0e, $0f
