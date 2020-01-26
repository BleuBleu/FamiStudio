;FamiTone2 v1.12

;settings, uncomment or put them into your main program; the latter makes possible updates easier

; FT_BASE_ADR        = $0300  ;page in the RAM used for FT2 variables, should be $xx00
; FT_TEMP            = $00    ;3 bytes in zeropage used by the library as a scratchpad
FT_DPCM_OFF        = $c000
FT_SFX_STREAMS    = 0     ;number of sound effects played at once, 1..4

FT_DPCM_ENABLE = 1        ;undefine to exclude all DMC code
FT_SFX_ENABLE = 0         ;undefine to exclude all sound effects code
FT_THREAD    = 1          ;undefine if you are calling sound effects from the same thread as the sound update call

FT_PAL_SUPPORT = 0        ;undefine to exclude PAL support
FT_NTSC_SUPPORT    = 1    ;undefine to exclude NTSC support

;internal defines
FT_PITCH_FIX    = 0 ;(FT_PAL_SUPPORT|FT_NTSC_SUPPORT) ;add PAL/NTSC pitch correction code only when both modes are enabled
FT_SMOOTH_VIBRATO = 1 ; Blaarg's smooth vibrato technique

.segment "RAM"

.ifdef FT_VRC6_ENABLE
FT_NUM_ENVELOPES        = 2+2+2+2+2+2+2+2 ; DPCM envelopes [8-9] are unused. 
FT_NUM_PITCH_ENVELOPES  = 6
.else
FT_NUM_ENVELOPES        = 2+2+2+2
FT_NUM_PITCH_ENVELOPES  = 3
.endif

FT_ENVELOPES:
FT_ENV_VALUE  : .res FT_NUM_ENVELOPES
FT_ENV_REPEAT : .res FT_NUM_ENVELOPES
FT_ENV_ADR_L  : .res FT_NUM_ENVELOPES
FT_ENV_ADR_H  : .res FT_NUM_ENVELOPES
FT_ENV_PTR    : .res FT_NUM_ENVELOPES

FT_PITCH_ENVELOPES:
FT_PITCH_ENV_VALUE_L  : .res FT_NUM_PITCH_ENVELOPES
FT_PITCH_ENV_VALUE_H  : .res FT_NUM_PITCH_ENVELOPES
FT_PITCH_ENV_REPEAT   : .res FT_NUM_PITCH_ENVELOPES
FT_PITCH_ENV_ADR_L    : .res FT_NUM_PITCH_ENVELOPES
FT_PITCH_ENV_ADR_H    : .res FT_NUM_PITCH_ENVELOPES
FT_PITCH_ENV_PTR      : .res FT_NUM_PITCH_ENVELOPES
FT_PITCH_ENV_OVERRIDE : .res FT_NUM_PITCH_ENVELOPES

;slide structure offsets, 3 bytes per slide.

.ifdef FT_VRC6_ENABLE
FT_NUM_SLIDES = 6
.else
FT_NUM_SLIDES = 3 ;square and triangle have slide notes.
.endif

FT_SLIDES:
FT_SLIDE_STEP    : .res FT_NUM_SLIDES
FT_SLIDE_PITCH_L : .res FT_NUM_SLIDES
FT_SLIDE_PITCH_H : .res FT_NUM_SLIDES

;channel structure offsets, 10 bytes per channel

.ifdef FT_VRC6_ENABLE
FT_NUM_CHANNELS = 8
.else
FT_NUM_CHANNELS = 5
.endif

FT_CHANNELS:
FT_CHN_PTR_L        : .res FT_NUM_CHANNELS
FT_CHN_PTR_H        : .res FT_NUM_CHANNELS
FT_CHN_NOTE         : .res FT_NUM_CHANNELS
FT_CHN_INSTRUMENT   : .res FT_NUM_CHANNELS
FT_CHN_REPEAT       : .res FT_NUM_CHANNELS
FT_CHN_RETURN_L     : .res FT_NUM_CHANNELS
FT_CHN_RETURN_H     : .res FT_NUM_CHANNELS
FT_CHN_REF_LEN      : .res FT_NUM_CHANNELS
FT_CHN_DUTY         : .res FT_NUM_CHANNELS
FT_CHN_VOLUME_TRACK : .res FT_NUM_CHANNELS ; DPCM(4) + Triangle(2) are unused.
.ifdef FT_EQUALIZER
FT_CHN_NOTE_COUNTER : .res FT_NUM_CHANNELS
.endif

;variables and aliases

FT_CH1_ENVS = 0
FT_CH2_ENVS = 2
FT_CH3_ENVS = 4
FT_CH4_ENVS = 6
.ifdef FT_VRC6_ENABLE
FT_CH6_ENVS = 10
FT_CH7_ENVS = 12
FT_CH8_ENVS = 14
.endif

FT_ENV_VOLUME_OFF = 0
FT_ENV_NOTE_OFF   = 1

FT_SFX_STRUCT_SIZE    = 15

FT_VARS: .res 13 - FT_SFX_STRUCT_SIZE * FT_SFX_STREAMS

FT_PAL_ADJUST   = FT_VARS+0
FT_SONG_LIST_L  = FT_VARS+1
FT_SONG_LIST_H  = FT_VARS+2
FT_INSTRUMENT_L = FT_VARS+3
FT_INSTRUMENT_H = FT_VARS+4
FT_TEMPO_STEP_L = FT_VARS+5
FT_TEMPO_STEP_H = FT_VARS+6
FT_TEMPO_ACC_L  = FT_VARS+7
FT_TEMPO_ACC_H  = FT_VARS+8
FT_SONG_SPEED   = FT_CHN_INSTRUMENT+4
FT_PULSE1_PREV  = FT_CHN_DUTY+2
FT_PULSE2_PREV  = FT_CHN_DUTY+4
FT_DPCM_LIST_L  = FT_VARS+9
FT_DPCM_LIST_H  = FT_VARS+10
FT_DPCM_EFFECT  = FT_VARS+11
FT_OUT_BUF      = FT_VARS+12    ;11 bytes


;sound effect stream variables, 2 bytes and 15 bytes per stream
;when sound effects are disabled, this memory is not used

FT_SFX_ADR_L    = FT_VARS+23
FT_SFX_ADR_H    = FT_VARS+24
FT_SFX_BASE_ADR = FT_VARS+25

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

.segment "ZEROPAGE"
;zero page variables

FT_TEMP:
FT_TEMP_VAR1 : .res 1
FT_TEMP_VAR2 : .res 1
FT_TEMP_VAR3 : .res 1
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

.ifdef FT_VRC6_ENABLE
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

;------------------------------------------------------------------------------
; reset APU, initialize FamiTone
; in: A   0 for PAL, not 0 for NTSC
;     X,Y pointer to music data
;------------------------------------------------------------------------------

FamiToneInit:

    stx FT_SONG_LIST_L         ;store music data pointer for further use
    sty FT_SONG_LIST_H
    stx <FT_TEMP_PTR_L
    sty <FT_TEMP_PTR_H

    .if(FT_PITCH_FIX)
    tax                        ;set SZ flags for A
    beq @pal
    lda #64
@pal:
    .else
    .if(FT_PAL_SUPPORT)
    lda #0
    .endif
    .if(FT_NTSC_SUPPORT)
    lda #64
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

    ;jmp FamiToneMusicStop


;------------------------------------------------------------------------------
; stop music that is currently playing, if any
; in: none
;------------------------------------------------------------------------------

FamiToneMusicStop:

    lda #0
    sta FT_SONG_SPEED          ;stop music, reset pause flag
    sta FT_DPCM_EFFECT         ;no DPCM effect playing

    ldx #0    ;initialize channel structures

@set_channels:

    lda #0
    sta FT_CHN_REPEAT,x
    sta FT_CHN_INSTRUMENT,x
    sta FT_CHN_NOTE,x
    sta FT_CHN_REF_LEN,x
    sta FT_CHN_VOLUME_TRACK,x
.ifdef FT_VRC6_ENABLE
    cpx #5
    bcc @regular_inst
@vrc6_inst:
    lda #$0
    sta FT_CHN_DUTY,x
    jmp @nextchannel
@regular_inst:
.endif
    lda #$30
    sta FT_CHN_DUTY,x

@nextchannel:
    inx                        ;next channel
    cpx #FT_NUM_CHANNELS
    bne @set_channels

    ldx #0    ;initialize all slides to zero
    lda #0
@set_slides:

    sta FT_SLIDE_STEP, x
    inx                        ;next channel
    cpx #FT_NUM_SLIDES
    bne @set_slides

    ldx #0    ;initialize all envelopes to the dummy envelope

@set_envelopes:

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
    bne @set_envelopes

    ldx #0    ;initialize all envelopes to the dummy envelope

@set_pitch_envelopes:

    lda #.lobyte(_FT2DummyPitchEnvelope)
    sta FT_PITCH_ENV_ADR_L,x
    lda #.hibyte(_FT2DummyPitchEnvelope)
    sta FT_PITCH_ENV_ADR_H,x
    lda #0
    sta FT_PITCH_ENV_REPEAT,x
    sta FT_PITCH_ENV_VALUE_L,x
    sta FT_PITCH_ENV_VALUE_H,x
    sta FT_PITCH_ENV_OVERRIDE,x
    lda #1
    sta FT_PITCH_ENV_PTR,x
    inx
    cpx #FT_NUM_PITCH_ENVELOPES
    bne @set_pitch_envelopes

    jmp FamiToneSampleStop


;------------------------------------------------------------------------------
; play music
; in: A number of subsong
;------------------------------------------------------------------------------

FamiToneMusicPlay:

    ldx FT_SONG_LIST_L
    stx <FT_TEMP_PTR_L
    ldx FT_SONG_LIST_H
    stx <FT_TEMP_PTR_H

    ldy #0
    cmp (FT_TEMP_PTR1),y       ;check if there is such sub song
    bcs @skip

.if FT_NUM_CHANNELS = 5
    asl a                      ;multiply song number by 14
    sta <FT_TEMP_PTR_L         ;use pointer LSB as temp variable
    asl a
    tax
    asl a
    adc <FT_TEMP_PTR_L
    stx <FT_TEMP_PTR_L
    adc <FT_TEMP_PTR_L
.elseif FT_NUM_CHANNELS = 8
    asl                        ;multiply song number by 20
    asl
    sta <FT_TEMP_PTR_L
    asl
    asl
    adc <FT_TEMP_PTR_L
.endif

    adc #5                     ;add offset
    tay

    lda FT_SONG_LIST_L         ;restore pointer LSB
    sta <FT_TEMP_PTR_L

    jsr FamiToneMusicStop      ;stop music, initialize channels and envelopes

    ldx #0    ;initialize channel structures

@set_channels:

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
.ifdef FT_VRC6_ENABLE
    cpx #5
    bcc @regular_inst
@vrc6_inst:
    lda #$0
    sta FT_CHN_DUTY,x
    jmp @nextchannel
@regular_inst:
.endif
    sta FT_CHN_DUTY,x

@nextchannel:
    inx                        ;next channel
    cpx #FT_NUM_CHANNELS
    bne @set_channels


    lda FT_PAL_ADJUST          ;read tempo for PAL or NTSC
    beq @pal
    iny
    iny
@pal:

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

@skip:
    rts


;------------------------------------------------------------------------------
; pause and unpause current music
; in: A 0 or not 0 to play or pause
;------------------------------------------------------------------------------

FamiToneMusicPause:

    tax                        ;set SZ flags for A
    beq @unpause
    
@pause:

    jsr FamiToneSampleStop
    
    lda #0                     ;mute sound
    sta FT_ENV_VALUE+FT_CH1_ENVS+FT_ENV_VOLUME_OFF
    sta FT_ENV_VALUE+FT_CH2_ENVS+FT_ENV_VOLUME_OFF
    sta FT_ENV_VALUE+FT_CH3_ENVS+FT_ENV_VOLUME_OFF
    sta FT_ENV_VALUE+FT_CH4_ENVS+FT_ENV_VOLUME_OFF
.ifdef FT_VRC6_ENABLE
    sta FT_ENV_VALUE+FT_CH6_ENVS+FT_ENV_VOLUME_OFF
    sta FT_ENV_VALUE+FT_CH7_ENVS+FT_ENV_VOLUME_OFF
    sta FT_ENV_VALUE+FT_CH8_ENVS+FT_ENV_VOLUME_OFF
.endif
    lda FT_SONG_SPEED          ;set pause flag
    ora #$80
    bne @done
@unpause:
    lda FT_SONG_SPEED          ;reset pause flag
    and #$7f
@done:
    sta FT_SONG_SPEED

    rts


;------------------------------------------------------------------------------
; update FamiTone state, should be called every NMI
; in: none
;------------------------------------------------------------------------------

.macro update_channel_sound idx, env_offset, slide_offset, pulse_prev, vol_ora, hi_ora, reg_hi, reg_lo, reg_vol, reg_sweep

    .local @slide
    .local @slidesign
    .local @noslide
    .local @sign
    .local @checkprevpulse
    .local @prev
    .local @cut
    .local @zero_vol
    .local noteTableLSB
    .local noteTableMSB

.if .defined(FT_VRC6_ENABLE) && idx = 7
    noteTableLSB = _FT2SawNoteTableLSB
    noteTableMSB = _FT2SawNoteTableMSB
.else
    noteTableLSB = _FT2NoteTableLSB
    noteTableMSB = _FT2NoteTableMSB
.endif

    lda FT_CHN_NOTE+idx
.if !.blank(pulse_prev) && (FT_SMOOTH_VIBRATO)
    bne @nocut
    jmp @cut
@nocut:
.else    
    beq @cut
.endif
    clc
    adc FT_ENV_VALUE+env_offset+FT_ENV_NOTE_OFF

.if idx = 3 ;  noise channel is a bit special    

    and #$0f
    eor #$0f
    sta <FT_TEMP_VAR1
    lda FT_CHN_DUTY+idx
    asl a
    and #$80
    ora <FT_TEMP_VAR1

.else

    .if(FT_PITCH_FIX)
    ora FT_PAL_ADJUST ; TODO: Some expansions wont need this.
    .endif
    tax

.ifnblank slide_offset

    ldy FT_SLIDE_STEP+slide_offset
    beq @noslide
@slide:
    clc
    lda FT_PITCH_ENV_VALUE_L+slide_offset
    adc noteTableLSB,x
    sta FT_TEMP_PTR2_L
    lda FT_PITCH_ENV_VALUE_H+slide_offset
    adc noteTableMSB,x
    sta FT_TEMP_PTR2_H
    lda FT_SLIDE_PITCH_H+slide_offset
    asl ; sign extend upcoming right shift.
    ror ; we have 1 bit of fraction for slides, shift right hi byte.
    ror 
    sta FT_TEMP_VAR1
    lda FT_SLIDE_PITCH_L+slide_offset
    ror ; shift right low byte.
    clc
    adc FT_TEMP_PTR2_L
    sta reg_lo
.if !.blank(pulse_prev) && (FT_SMOOTH_VIBRATO)
    sta FT_TEMP_VAR2 ; need to keep the lo period in case we do the sweep trick.
.endif
    lda FT_TEMP_VAR1
    adc FT_TEMP_PTR2_H 
    jmp @checkprevpulse
@noslide:    

.endif

    lda FT_PITCH_ENV_VALUE_L+slide_offset
    adc noteTableLSB,x
.if !.blank(pulse_prev) && (FT_SMOOTH_VIBRATO)
    sta FT_TEMP_VAR2 ; need to keep the lo period in case we do the sweep trick.
.endif
    sta reg_lo
    lda FT_PITCH_ENV_VALUE_H+slide_offset
    adc noteTableMSB,x

@checkprevpulse:

.ifnblank pulse_prev

    .if(!FT_SFX_ENABLE)
        .if(FT_SMOOTH_VIBRATO)
            ; Blaarg's smooth vibrato technique, only used if high period delta is 1 or -1.
            tax ; X = new hi-period
            sec
            sbc pulse_prev ; A = signed hi-period delta.
            beq @prev
            stx pulse_prev
            tay 
            iny ; we only care about -1 ($ff) and 1. Adding one means we only check of 0 or 2, we already checked for zero (so < 3).
            cpy #$03
            bcs @hi_delta_too_big
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
            lda FT_TEMP_VAR2
            sta reg_lo ; restore lo-period.
            jmp @prev
        @hi_delta_too_big:
            stx reg_hi
        .else
            cmp pulse_prev
            beq @prev
            sta pulse_prev    
        .endif
    .endif
    
.endif

.ifnblank hi_ora
    ora hi_ora
.endif

.endif ; idx = 3

.if .blank(pulse_prev) || (!FT_SMOOTH_VIBRATO)
    sta reg_hi
.endif

@prev:

    lda FT_ENV_VALUE+env_offset+FT_ENV_VOLUME_OFF
    ora FT_CHN_VOLUME_TRACK+idx ; TODO: Triangle channel doesnt really need a volume track. Make it optional.
    tax
    lda _FT2VolumeTable, x 
@cut:
.ifnblank vol_ora
    .if .defined(FT_VRC6_ENABLE) && idx = 7 
        ; saw channel has 6 bit volumes. 
        ; get hi-bit from duty, similar to FamiTracker, but taking volume into account.
        ; FamiTracker looses ability to output low volume when duty is odd.
        beq @zero_vol
        tax
        lda vol_ora ; duty is already bit shifted 4 times at export.
        and #$10
        bne @odd_duty
        txa
        jmp @zero_vol
        @odd_duty:
        txa
        asl
        adc #1
        @zero_vol:
    .else
        ora vol_ora
    .endif
.endif
    sta reg_vol

.endmacro

.macro update_row_standard channel_idx, env_idx, duty

    .local @no_new_note

    ldx #channel_idx
    jsr _FT2ChannelUpdate
    bcc @no_new_note
    ldx #env_idx
    lda FT_CHN_INSTRUMENT+channel_idx
    jsr _FT2SetInstrument
.ifnblank duty
    sta duty
.endif

.ifdef FT_EQUALIZER
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
.endmacro

.macro update_row_dpcm channel_idx
.if(FT_DPCM_ENABLE)
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

.ifdef FT_EQUALIZER
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

FamiToneUpdate:

    .if(FT_THREAD)
    lda FT_TEMP_PTR_L
    pha
    lda FT_TEMP_PTR_H
    pha
    .endif

    lda FT_SONG_SPEED          ;speed 0 means that no music is playing currently
    bmi @pause                 ;bit 7 set is the pause flag
    bne @update
@pause:
    jmp @update_sound

@update:

    clc                        ;update frame counter that considers speed, tempo, and PAL/NTSC
    lda FT_TEMPO_ACC_L
    adc FT_TEMPO_STEP_L
    sta FT_TEMPO_ACC_L
    lda FT_TEMPO_ACC_H
    adc FT_TEMPO_STEP_H
    cmp FT_SONG_SPEED
    bcs @update_row            ;overflow, row update is needed
    sta FT_TEMPO_ACC_H         ;no row update, skip to the envelopes update
    jmp @update_envelopes

;----------------------------------------------------------------------------------------------------------------------
@update_row:

    sec
    sbc FT_SONG_SPEED
    sta FT_TEMPO_ACC_H

    update_row_standard 0, FT_CH1_ENVS, FT_CHN_DUTY+0
    update_row_standard 1, FT_CH2_ENVS, FT_CHN_DUTY+1
    update_row_standard 2, FT_CH3_ENVS, 
    update_row_standard 3, FT_CH4_ENVS, FT_CHN_DUTY+3
    update_row_dpcm 4
.ifdef FT_VRC6_ENABLE
    update_row_standard 5, FT_CH6_ENVS, FT_CHN_DUTY+5
    update_row_standard 6, FT_CH7_ENVS, FT_CHN_DUTY+6
    update_row_standard 7, FT_CH8_ENVS, FT_CHN_DUTY+7
.endif

;----------------------------------------------------------------------------------------------------------------------
@update_envelopes:
    ldx #0    ;process 11 envelopes

@env_process:
    lda FT_ENV_REPEAT,x
    beq @env_read  
    dec FT_ENV_REPEAT,x
    bne @env_next

@env_read:
    lda FT_ENV_ADR_L,x         ;load envelope data address into temp
    sta <FT_TEMP_PTR_L
    lda FT_ENV_ADR_H,x
    sta <FT_TEMP_PTR_H
    ldy FT_ENV_PTR,x           ;load envelope pointer

@env_read_value:
    lda (FT_TEMP_PTR1),y       ;read a byte of the envelope data
    bpl @env_special           ;values below 128 used as a special code, loop or repeat
    clc                        ;values above 128 are output value+192 (output values are signed -63..64)
    adc #256-192
    sta FT_ENV_VALUE,x         ;store the output value
    iny                        ;advance the pointer
    bne @env_next_store_ptr    ;bra

@env_special:
    bne @env_set_repeat        ;zero is the loop point, non-zero values used for the repeat counter
    iny                        ;advance the pointer
    lda (FT_TEMP_PTR1),y       ;read loop position
    tay                        ;use loop position
    jmp @env_read_value        ;read next byte of the envelope

@env_set_repeat:
    iny
    sta FT_ENV_REPEAT,x        ;store the repeat counter value

@env_next_store_ptr:
    tya                        ;store the envelope pointer
    sta FT_ENV_PTR,x

@env_next:
    inx                        ;next envelope

    cpx #FT_NUM_ENVELOPES
    bne @env_process

;----------------------------------------------------------------------------------------------------------------------
@update_pitch_envelopes:
    ldx #0
    jmp @pitch_env_process

@pitch_relate_update_with_last_value:
    lda FT_PITCH_ENV_REPEAT,x
    sec 
    sbc #1
    sta FT_PITCH_ENV_REPEAT,x
    and #$7f 
    beq @pitch_env_read
    lda FT_PITCH_ENV_ADR_L,x 
    sta <FT_TEMP_PTR_L
    lda FT_PITCH_ENV_ADR_H,x
    sta <FT_TEMP_PTR_H
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
     bpl @pitch_relative_last_pos  
    lda #$ff
@pitch_relative_last_pos:
    adc FT_PITCH_ENV_VALUE_H,x
    sta FT_PITCH_ENV_VALUE_H,x
    jmp @pitch_env_next

@pitch_env_process:
    lda FT_PITCH_ENV_REPEAT,x
    cmp #$81
    bcs @pitch_relate_update_with_last_value
    and #$7f
    beq @pitch_env_read
    dec FT_PITCH_ENV_REPEAT,x
    bne @pitch_env_next

@pitch_env_read:
    lda FT_PITCH_ENV_ADR_L,x 
    sta <FT_TEMP_PTR_L
    lda FT_PITCH_ENV_ADR_H,x
    sta <FT_TEMP_PTR_H
    ldy #0
    lda (FT_TEMP_PTR1),y
    sta FT_TEMP_VAR1 ; going to be 0 for absolute envelope, 0x80 for relative.
    ldy FT_PITCH_ENV_PTR,x

@pitch_env_read_value:
    lda (FT_TEMP_PTR1),y
    bpl @pitch_env_special 
    clc  
    adc #256-192
    bit FT_TEMP_VAR1
    bmi @pitch_relative

@pitch_absolute:
    sta FT_PITCH_ENV_VALUE_L,x
    ora #0
    bmi @pitch_absolute_neg  
    lda #0
    jmp @pitch_absolute_set_value_hi
@pitch_absolute_neg:
    lda #$ff
@pitch_absolute_set_value_hi:
    sta FT_PITCH_ENV_VALUE_H,x
    iny 
    jmp @pitch_env_next_store_ptr

@pitch_relative:
    sta FT_TEMP_VAR2
    clc
    adc FT_PITCH_ENV_VALUE_L,x
    sta FT_PITCH_ENV_VALUE_L,x
    lda FT_TEMP_VAR2
    and #$80
    bpl @pitch_relative_pos  
    lda #$ff
@pitch_relative_pos:
    adc FT_PITCH_ENV_VALUE_H,x
    sta FT_PITCH_ENV_VALUE_H,x
    iny 
    jmp @pitch_env_next_store_ptr

@pitch_env_special:
    bne @pitch_env_set_repeat
    iny 
    lda (FT_TEMP_PTR1),y 
    tay
    jmp @pitch_env_read_value 

@pitch_env_set_repeat:
    iny
    ora FT_TEMP_VAR1 ; this is going to set the relative flag in the hi-bit.
    sta FT_PITCH_ENV_REPEAT,x

@pitch_env_next_store_ptr:
    tya 
    sta FT_PITCH_ENV_PTR,x

@pitch_env_next:
    inx 

    cpx #FT_NUM_PITCH_ENVELOPES
    bne @pitch_env_process

;----------------------------------------------------------------------------------------------------------------------
@update_slides:
    ldx #0    ;process 3 slides

@slide_process:
    lda FT_SLIDE_STEP,x        ; zero repeat means no active slide.
    beq @slide_next
    clc                        ; add step to slide pitch (16bit + 8bit signed).
    lda FT_SLIDE_STEP,x
    adc FT_SLIDE_PITCH_L,x
    sta FT_SLIDE_PITCH_L,x
    lda FT_SLIDE_STEP,x
    and #$80
    beq @positive_slide

@negative_slide:
    lda #$ff
    adc FT_SLIDE_PITCH_H,x
    sta FT_SLIDE_PITCH_H,x
    bpl @slide_next
    jmp @clear_slide

@positive_slide:
    adc FT_SLIDE_PITCH_H,x
    sta FT_SLIDE_PITCH_H,x
    bmi @slide_next

@clear_slide:
    lda #0
    sta FT_SLIDE_STEP,x

@slide_next:
    inx                        ;next slide
    cpx #FT_NUM_SLIDES
    bne @slide_process

;----------------------------------------------------------------------------------------------------------------------
@update_sound:

    update_channel_sound 0, FT_CH1_ENVS, 0, FT_PULSE1_PREV, FT_CHN_DUTY+0, , FT_MR_PULSE1_H, FT_MR_PULSE1_L, FT_MR_PULSE1_V, APU_PL1_SWEEP
    update_channel_sound 1, FT_CH2_ENVS, 1, FT_PULSE2_PREV, FT_CHN_DUTY+1, , FT_MR_PULSE2_H, FT_MR_PULSE2_L, FT_MR_PULSE2_V, APU_PL2_SWEEP
    update_channel_sound 2, FT_CH3_ENVS, 2, , #$80, , FT_MR_TRI_H, FT_MR_TRI_L, FT_MR_TRI_V
    update_channel_sound 3, FT_CH4_ENVS,  , , #$f0, , FT_MR_NOISE_F, , FT_MR_NOISE_V
.ifdef FT_VRC6_ENABLE
    update_channel_sound 5, FT_CH6_ENVS, 3, , FT_CHN_DUTY+5, #$80, VRC6_PL1_HI, VRC6_PL1_LO, VRC6_PL1_VOL
    update_channel_sound 6, FT_CH7_ENVS, 4, , FT_CHN_DUTY+6, #$80, VRC6_PL2_HI, VRC6_PL2_LO, VRC6_PL2_VOL
    update_channel_sound 7, FT_CH8_ENVS, 5, , FT_CHN_DUTY+7, #$80, VRC6_SAW_HI, VRC6_SAW_LO, VRC6_SAW_VOL
.endif

;----------------------------------------------------------------------------------------------------------------------
.if(FT_SFX_ENABLE)

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
    beq @no_pulse1_upd
    sta FT_PULSE1_PREV
    sta APU_PL1_HI
@no_pulse1_upd:

    lda FT_OUT_BUF+3    ;pulse 2 volume
    sta APU_PL2_VOL
    lda FT_OUT_BUF+4    ;pulse 2 period LSB
    sta APU_PL2_LO
    lda FT_OUT_BUF+5    ;pulse 2 period MSB, only applied when changed
    cmp FT_PULSE2_PREV
    beq @no_pulse2_upd
    sta FT_PULSE2_PREV
    sta APU_PL2_HI
@no_pulse2_upd:

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

    .if(FT_THREAD)
    pla
    sta FT_TEMP_PTR_H
    pla
    sta FT_TEMP_PTR_L
    .endif

    rts

;internal routine, sets up envelopes of a channel according to current instrument
;in X envelope group offset, A instrument number

.proc _FT2SetInstrument

    asl a                      ;instrument number is pre multiplied by 4
    tay
    lda FT_INSTRUMENT_H
    adc #0                     ;use carry to extend range for 64 instruments
    sta<FT_TEMP_PTR_H
    lda FT_INSTRUMENT_L
    sta FT_TEMP_PTR_L

    lda (FT_TEMP_PTR1),y       ;duty cycle
    sta FT_TEMP_VAR1
    iny

    lda (FT_TEMP_PTR1),y       ;instrument pointer LSB
    sta FT_ENV_ADR_L,x
    iny
    lda (FT_TEMP_PTR1),y       ;instrument pointer MSB
    iny
    sta FT_ENV_ADR_H,x
    inx                        ;next envelope

    lda (FT_TEMP_PTR1),y       ;instrument pointer LSB
    sta FT_ENV_ADR_L,x
    iny
    lda (FT_TEMP_PTR1),y       ;instrument pointer MSB
    sta FT_ENV_ADR_H,x

    lda #1
    sta FT_ENV_PTR-1,x         ;reset env1 pointer (env1 is volume and volume can have releases)
    lda #0
    sta FT_ENV_REPEAT-1,x      ;reset env1 repeat counter
    sta FT_ENV_REPEAT,x        ;reset env2 repeat counter
    sta FT_ENV_PTR,x           ;reset env2 pointer

    txa
    lsr ; the channel number is basically envelope index / 2 now...
    tax
    lda _FT2ChannelToPitch, x
    bmi @no_pitch
    tax
    lda FT_PITCH_ENV_OVERRIDE,x ; instrument pitch is overriden by vibrato, dont touch!
    bne @no_pitch
    iny
    lda #1
    sta FT_PITCH_ENV_PTR,x     ;reset env3 pointer (pitch envelope have relative/absolute flag in the first byte)
    lda #0
    sta FT_PITCH_ENV_REPEAT,x  ;reset env3 repeat counter
    sta FT_PITCH_ENV_VALUE_L,x
    sta FT_PITCH_ENV_VALUE_H,x
    sta FT_PITCH_ENV_OVERRIDE,x
    lda (FT_TEMP_PTR1),y       ;instrument pointer LSB
    sta FT_PITCH_ENV_ADR_L,x
    iny
    lda (FT_TEMP_PTR1),y       ;instrument pointer MSB
    sta FT_PITCH_ENV_ADR_H,x

@no_pitch:
    lda FT_TEMP_VAR1
    rts

.endproc

;internal routine, parses channel note data

.proc _FT2ChannelUpdate

    FT_DISABLE_ATTACK = FT_TEMP_VAR3

    lda FT_CHN_REPEAT,x        ;check repeat counter
    beq @no_repeat
    dec FT_CHN_REPEAT,x        ;decrease repeat counter
    clc                        ;no new note
    rts

@no_repeat:
    lda #0
    sta FT_DISABLE_ATTACK
    lda FT_CHN_PTR_L,x         ;load channel pointer into temp
    sta FT_TEMP_PTR_L
    lda FT_CHN_PTR_H,x
    sta FT_TEMP_PTR_H
    ldy #0

@read_byte:
    lda (FT_TEMP_PTR1),y       ;read byte of the channel
    inc FT_TEMP_PTR_L         ;advance pointer
    bne @check_regular_note
    inc FT_TEMP_PTR_H

@check_regular_note:
    cmp #$61
    bcs @check_special_code    ; $00 to $60 are regular notes, most common case.
    jmp @regular_note

@check_special_code:
    ora #0
    bpl @check_volume
    jmp @special_code           ;bit 7 0=note 1=special code

@check_volume:
    cmp #$70
    bcc @check_slide
    and #$0f
    asl ; a LUT would be nice, but x/y are both in-use here.
    asl
    asl
    asl
    sta FT_CHN_VOLUME_TRACK,x
    jmp @read_byte

@check_slide:
    cmp #$61                  ; slide note (followed by num steps, step size and the target note)
    beq @slide

@check_disable_attack:    
    cmp #$62
    beq @disable_attack

@check_override_pitch_envelope:
    cmp #$63
    beq @override_pitch_envelope

;cmp #$64
;beq @override_pitch_envelope

@clear_pitch_override_flag:
    ldy _FT2ChannelToPitch,x
    lda #0
    sta FT_PITCH_ENV_OVERRIDE,y
    ldy #0
    jmp @read_byte 

@override_pitch_envelope:
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
    sta FT_PITCH_ENV_OVERRIDE,x
    ldx FT_TEMP_VAR1
    clc
    lda #2
    adc FT_TEMP_PTR_L
    sta FT_TEMP_PTR_L
    bcc @read_byte 
    inc FT_TEMP_PTR_H
    jmp @read_byte 

@disable_attack:
    lda #1
    sta FT_DISABLE_ATTACK    
    jmp @read_byte 

@slide:
    stx FT_TEMP_VAR1
    lda _FT2ChannelToSlide,x
    tax
    lda (FT_TEMP_PTR1),y       ; read slide step size
    iny
    sta FT_SLIDE_STEP,x
    lda (FT_TEMP_PTR1),y       ; read slide note from
    sta FT_TEMP_VAR2
    iny
    lda (FT_TEMP_PTR1),y       ; read slide note to
    ldy FT_TEMP_VAR2           ; start note
    stx FT_TEMP_VAR2           ; store slide index.    
    tax
.ifdef FT_VRC6_ENABLE
    lda FT_TEMP_VAR1
    cmp #7
    beq @note_table_saw
@note_table_regular:
.endif
    sec                        ; subtract the pitch of both notes. TODO: PAL.
    lda _FT2NoteTableLSB,y
    sbc _FT2NoteTableLSB,x
    sta FT_TEMP_PTR2_H
    lda _FT2NoteTableMSB,y
    sbc _FT2NoteTableMSB,x
.ifdef FT_VRC6_ENABLE
    jmp @note_table_done
@note_table_saw:
    sec
    lda _FT2SawNoteTableLSB,y
    sbc _FT2SawNoteTableLSB,x
    sta FT_TEMP_PTR2_H
    lda _FT2SawNoteTableMSB,y
    sbc _FT2SawNoteTableMSB,x
@note_table_done:
.endif
    ldx FT_TEMP_VAR2           ; slide index.
    sta FT_SLIDE_PITCH_H,x
    lda FT_TEMP_PTR2_H
    asl                        ; shift-left, we have 1 bit of fractional slide.
    sta FT_SLIDE_PITCH_L,x
    rol FT_SLIDE_PITCH_H,x     ; shift-left, we have 1 bit of fractional slide.
    ldx FT_TEMP_VAR1
    ldy #2
    lda (FT_TEMP_PTR1),y       ; re-read the target note (ugly...)
    sta FT_CHN_NOTE,x          ; store note code
    lda #3
    clc
    adc FT_TEMP_PTR_L
    sta FT_TEMP_PTR_L
    bcc @slide_done_pos
    inc FT_TEMP_PTR_H

@slide_done_pos:
    ldy #0
    jmp @sec_and_done

@regular_note:    
    sta FT_CHN_NOTE,x          ; store note code
    ldy _FT2ChannelToSlide,x   ; clear any previous slide on new node.
    bmi @sec_and_done
    lda #0
    sta FT_SLIDE_STEP,y
@sec_and_done:
    lda FT_DISABLE_ATTACK
    bne @no_attack
    sec                        ;new note flag is set
    jmp @done
@no_attack:
    clc                        ;pretend there is no new note.
    jmp @done

@special_code:
    and #$7f
    lsr a
    bcs @set_empty_rows
    asl a
    asl a
    sta FT_CHN_INSTRUMENT,x    ;store instrument number*4
    jmp @read_byte 

@set_empty_rows:
    cmp #$3b
    beq @release_note
    cmp #$3d
    bcc @set_repeat
    beq @set_speed
    cmp #$3e
    beq @set_loop

@set_reference:
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
    sta <FT_TEMP_VAR1          ;remember in temp
    iny
    lda (FT_TEMP_PTR1),y
    sta FT_TEMP_PTR_H
    lda FT_TEMP_VAR1
    sta FT_TEMP_PTR_L
    ldy #0
    jmp @read_byte

@set_speed:
    lda (FT_TEMP_PTR1),y
    sta FT_SONG_SPEED
    inc FT_TEMP_PTR_L         ;advance pointer after reading the speed value
    bne @jump_back
    inc FT_TEMP_PTR_H
@jump_back:    
    jmp @read_byte 

@set_loop:
    lda (FT_TEMP_PTR1),y
    sta FT_TEMP_VAR1
    iny
    lda (FT_TEMP_PTR1),y
    sta FT_TEMP_PTR_H
    lda FT_TEMP_VAR1
    sta FT_TEMP_PTR_L
    dey
    jmp @read_byte

@release_note:
    stx <FT_TEMP_VAR1
    lda _FT2ChannelToVolumeEnvelope,x ; DPCM(5) will never have releases.
    tax

    lda FT_ENV_ADR_L,x         ;load envelope data address into temp
    sta FT_TEMP_PTR2_L
    lda FT_ENV_ADR_H,x
    sta FT_TEMP_PTR2_H    
    
    ldy #0
    lda (FT_TEMP_PTR2),y       ;read first byte of the envelope data, this contains the release index.
    beq @env_has_no_release

    sta FT_ENV_PTR,x
    lda #0
    sta FT_ENV_REPEAT,x        ;need to reset envelope repeat to force update.
    
@env_has_no_release:
    ldx FT_TEMP_VAR1
    clc
    jmp @done

@set_repeat:
    sta FT_CHN_REPEAT,x        ;set up repeat counter, carry is clear, no new note

@done:
    lda FT_CHN_REF_LEN,x       ;check reference row counter
    beq @no_ref                ;if it is zero, there is no reference
    dec FT_CHN_REF_LEN,x       ;decrease row counter
    bne @no_ref

    lda FT_CHN_RETURN_L,x      ;end of a reference, return to previous pointer
    sta FT_CHN_PTR_L,x
    lda FT_CHN_RETURN_H,x
    sta FT_CHN_PTR_H,x
    rts

@no_ref:
    lda FT_TEMP_PTR_L
    sta FT_CHN_PTR_L,x
    lda FT_TEMP_PTR_H
    sta FT_CHN_PTR_H,x
    rts

.endproc

;------------------------------------------------------------------------------
; stop DPCM sample if it plays
;------------------------------------------------------------------------------

FamiToneSampleStop:

    lda #%00001111
    sta APU_SND_CHN

    rts


    
    .if(FT_DPCM_ENABLE)

;------------------------------------------------------------------------------
; play DPCM sample, used by music player, could be used externally
; in: A is number of a sample, 1..63
;------------------------------------------------------------------------------

FamiToneSamplePlayM:           ;for music (low priority)

    ldx FT_DPCM_EFFECT
    beq _FT2SamplePlay
    tax
    lda APU_SND_CHN
    and #16
    beq @not_busy
    rts

@not_busy:
    sta FT_DPCM_EFFECT
    txa
    jmp _FT2SamplePlay

;------------------------------------------------------------------------------
; play DPCM sample with higher priority, for sound effects
; in: A is number of a sample, 1..63
;------------------------------------------------------------------------------

FamiToneSamplePlay:

    ldx #1
    stx FT_DPCM_EFFECT

_FT2SamplePlay:

    sta <FT_TEMP               ;sample number*3, offset in the sample table
    asl a
    clc
    adc <FT_TEMP
    
    adc FT_DPCM_LIST_L
    sta <FT_TEMP_PTR_L
    lda #0
    adc FT_DPCM_LIST_H
    sta <FT_TEMP_PTR_H

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

    .endif

    .if(FT_SFX_ENABLE)

;------------------------------------------------------------------------------
; init sound effects player, set pointer to data
; in: X,Y is address of sound effects data
;------------------------------------------------------------------------------

FamiToneSfxInit:

    stx <FT_TEMP_PTR_L
    sty <FT_TEMP_PTR_H
    
    ldy #0
    
    .if(FT_PITCH_FIX)

    lda FT_PAL_ADJUST          ;add 2 to the sound list pointer for PAL
    bne @ntsc
    iny
    iny
@ntsc:

    .endif
    
    lda (FT_TEMP_PTR1),y       ;read and store pointer to the effects list
    sta FT_SFX_ADR_L
    iny
    lda (FT_TEMP_PTR1),y
    sta FT_SFX_ADR_H

    ldx #FT_SFX_CH0            ;init all the streams

@set_channels:
    jsr _FT2SfxClearChannel
    txa
    clc
    adc #FT_SFX_STRUCT_SIZE
    tax
    cpx #FT_SFX_STRUCT_SIZE*FT_SFX_STREAMS
    bne @set_channels

    rts


;internal routine, clears output buffer of a sound effect
;in: A is 0
;    X is offset of sound effect stream

_FT2SfxClearChannel:

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


;------------------------------------------------------------------------------
; play sound effect
; in: A is a number of the sound effect 0..127
;     X is offset of sound effect channel, should be FT_SFX_CH0..FT_SFX_CH3
;------------------------------------------------------------------------------

FamiToneSfxPlay:

    asl a                      ;get offset in the effects list
    tay

    jsr _FT2SfxClearChannel    ;stops the effect if it plays

    lda FT_SFX_ADR_L
    sta <FT_TEMP_PTR_L
    lda FT_SFX_ADR_H
    sta <FT_TEMP_PTR_H

    lda (FT_TEMP_PTR1),y       ;read effect pointer from the table
    sta FT_SFX_PTR_L,x         ;store it
    iny
    lda (FT_TEMP_PTR1),y
    sta FT_SFX_PTR_H,x         ;this write enables the effect

    rts


;internal routine, update one sound effect stream
;in: X is offset of sound effect stream

_FT2SfxUpdate:

    lda FT_SFX_REPEAT,x        ;check if repeat counter is not zero
    beq @no_repeat
    dec FT_SFX_REPEAT,x        ;decrement and return
    bne @update_buf            ;just mix with output buffer

@no_repeat:
    lda FT_SFX_PTR_H,x         ;check if MSB of the pointer is not zero
    bne @sfx_active
    rts                        ;return otherwise, no active effect

@sfx_active:
    sta <FT_TEMP_PTR_H         ;load effect pointer into temp
    lda FT_SFX_PTR_L,x
    sta <FT_TEMP_PTR_L
    ldy FT_SFX_OFF,x
    clc

@read_byte:
    lda (FT_TEMP_PTR1),y       ;read byte of effect
    bmi @get_data              ;if bit 7 is set, it is a register write
    beq @eof
    iny
    sta FT_SFX_REPEAT,x        ;if bit 7 is reset, it is number of repeats
    tya
    sta FT_SFX_OFF,x
    jmp @update_buf

@get_data:
    iny
    stx <FT_TEMP_VAR1          ;it is a register write
    adc <FT_TEMP_VAR1          ;get offset in the effect output buffer
    tax
    lda (FT_TEMP_PTR1),y       ;read value
    iny
    sta FT_SFX_BUF-128,x       ;store into output buffer
    ldx <FT_TEMP_VAR1
    jmp @read_byte             ;and read next byte

@eof:
    sta FT_SFX_PTR_H,x         ;mark channel as inactive

@update_buf:

    lda FT_OUT_BUF             ;compare effect output buffer with main output buffer
    and #$0f                   ;if volume of pulse 1 of effect is higher than that of the
    sta <FT_TEMP_VAR1          ;main buffer, overwrite the main buffer value with the new one
    lda FT_SFX_BUF+0,x
    and #$0f
    cmp <FT_TEMP_VAR1
    bcc @no_pulse1
    lda FT_SFX_BUF+0,x
    sta FT_OUT_BUF+0
    lda FT_SFX_BUF+1,x
    sta FT_OUT_BUF+1
    lda FT_SFX_BUF+2,x
    sta FT_OUT_BUF+2
@no_pulse1:

    lda FT_OUT_BUF+3           ;same for pulse 2
    and #$0f
    sta <FT_TEMP_VAR1
    lda FT_SFX_BUF+3,x
    and #$0f
    cmp <FT_TEMP_VAR1
    bcc @no_pulse2
    lda FT_SFX_BUF+3,x
    sta FT_OUT_BUF+3
    lda FT_SFX_BUF+4,x
    sta FT_OUT_BUF+4
    lda FT_SFX_BUF+5,x
    sta FT_OUT_BUF+5
@no_pulse2:

    lda FT_SFX_BUF+6,x           ;overwrite triangle of main output buffer if it is active
    beq @no_triangle
    sta FT_OUT_BUF+6
    lda FT_SFX_BUF+7,x
    sta FT_OUT_BUF+7
    lda FT_SFX_BUF+8,x
    sta FT_OUT_BUF+8
@no_triangle:

    lda FT_OUT_BUF+9           ;same as for pulse 1 and 2, but for noise
    and #$0f
    sta <FT_TEMP_VAR1
    lda FT_SFX_BUF+9,x
    and #$0f
    cmp <FT_TEMP_VAR1
    bcc @no_noise
    lda FT_SFX_BUF+9,x
    sta FT_OUT_BUF+9
    lda FT_SFX_BUF+10,x
    sta FT_OUT_BUF+10
@no_noise:

    rts

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

.ifdef FT_VRC6_ENABLE
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
    .byte $0f, $0e, $0d, $0c, $0c, $0b, $0a, $0a, $09, $09, $08, $08  ; Octave 0
    .byte $07, $07, $06, $06, $06, $05, $05, $05, $04, $04, $04, $04  ; Octave 1
    .byte $03, $03, $03, $03, $03, $02, $02, $02, $02, $02, $02, $02  ; Octave 2
    .byte $01, $01, $01, $01, $01, $01, $01, $01, $01, $01, $01, $01  ; Octave 3
    .byte $00, $00, $00, $00, $00, $00, $00, $00, $00, $00, $00, $00  ; Octave 4
    .byte $00, $00, $00, $00, $00, $00, $00, $00, $00, $00, $00, $00  ; Octave 5
    .byte $00, $00, $00, $00, $00, $00, $00, $00, $00, $00, $00, $00  ; Octave 6
    .byte $00, $00, $00, $00, $00, $00, $00, $00, $00, $00, $00, $00  ; Octave 7
.endif

_FT2ChannelToVolumeEnvelope:
    .byte FT_CH1_ENVS+FT_ENV_VOLUME_OFF
    .byte FT_CH2_ENVS+FT_ENV_VOLUME_OFF
    .byte FT_CH3_ENVS+FT_ENV_VOLUME_OFF
    .byte FT_CH4_ENVS+FT_ENV_VOLUME_OFF
    .byte $ff
.ifdef FT_VRC6_ENABLE
    .byte FT_CH6_ENVS+FT_ENV_VOLUME_OFF
    .byte FT_CH7_ENVS+FT_ENV_VOLUME_OFF
    .byte FT_CH8_ENVS+FT_ENV_VOLUME_OFF
.endif

_FT2ChannelToPitch:
_FT2ChannelToSlide:
    .byte $00
    .byte $01
    .byte $02
    .byte $ff ; no slide for noise
    .byte $ff ; no slide for DPCM
.ifdef FT_VRC6_ENABLE
    .byte $03
    .byte $04
    .byte $05
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
