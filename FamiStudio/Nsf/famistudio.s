;======================================================================================================================
; FamiStudio NSF driver, heavily customized version of FamiTone2. 
; Terrible code. Not for the faint of hearth.
;======================================================================================================================

FT_DPCM_OFF       = $c000

; Expansions.
;FAMISTUDIO_EXP_VRC6
;FAMISTUDIO_EXP_VRC7
;FAMISTUDIO_EXP_MMC5
;FAMISTUDIO_EXP_S5B
;FAMISTUDIO_EXP_FDS
;FAMISTUDIO_EXP_N163 (+ FAMISTUDIO_EXP_N163_CHN_CNT)

; Global configuration.
;FAMISTUDIO_CFG_PAL_SUPPORT
;FAMISTUDIO_CFG_NTSC_SUPPORT
;FAMISTUDIO_CFG_SFX_SUPPORT
;FAMISTUDIO_CFG_EQUALIZER
FAMISTUDIO_CFG_SFX_STREAMS    = 0     ;number of sound effects played at once, 1..4
FAMISTUDIO_CFG_SMOOTH_VIBRATO = 1    ; Blaarg's smooth vibrato technique
FAMISTUDIO_CFG_DPCM_SUPPORT   = 1
FAMISTUDIO_CFG_THREAD         = 0     ;undefine if you are calling sound effects from the same thread as the sound update call

; Toggeable features.
;FAMISTUDIO_USE_FAMITRACKER_TEMPO = 1
FAMISTUDIO_USE_VOLUME_TRACK   = 1
FAMISTUDIO_USE_PITCH_TRACK    = 1
FAMISTUDIO_USE_SLIDE_NOTES    = 1
FAMISTUDIO_USE_VIBRATO        = 1
FAMISTUDIO_USE_ARPEGGIO       = 1

; Internal defines, do not change.
.if .defined(FAMISTUDIO_CFG_NTSC_SUPPORT) && .defined(FAMISTUDIO_CFG_PAL_SUPPORT)
FAMISTUDIO_DUAL_SUPPORT = 1
.endif

.if .defined(FAMISTUDIO_EXP_VRC7)
    FAMISTUDIO_NUM_ENVELOPES        = 3+3+2+3+2+2+2+2+2+2
    FAMISTUDIO_NUM_PITCH_ENVELOPES  = 9
    FAMISTUDIO_NUM_CHANNELS         = 11
.elseif .defined(FAMISTUDIO_EXP_VRC6)
    FAMISTUDIO_NUM_ENVELOPES        = 3+3+2+3+3+3+3
    FAMISTUDIO_NUM_PITCH_ENVELOPES  = 6
    FAMISTUDIO_NUM_CHANNELS         = 8
.elseif .defined(FAMISTUDIO_EXP_S5B)
    FAMISTUDIO_NUM_ENVELOPES        = 3+3+2+3+2+2+2
    FAMISTUDIO_NUM_PITCH_ENVELOPES  = 6
    FAMISTUDIO_NUM_CHANNELS         = 8    
.elseif .defined(FAMISTUDIO_EXP_N163) 
    FAMISTUDIO_NUM_ENVELOPES        = 3+3+2+3+(FAMISTUDIO_EXP_N163_CHN_CNT*2)
    FAMISTUDIO_NUM_PITCH_ENVELOPES  = 3+FAMISTUDIO_EXP_N163_CHN_CNT
    FAMISTUDIO_NUM_CHANNELS         = 5+FAMISTUDIO_EXP_N163_CHN_CNT
.elseif .defined(FAMISTUDIO_EXP_MMC5)
    FAMISTUDIO_NUM_ENVELOPES        = 3+3+2+3+3+3
    FAMISTUDIO_NUM_PITCH_ENVELOPES  = 5
    FAMISTUDIO_NUM_CHANNELS         = 7
.elseif .defined(FAMISTUDIO_EXP_FDS)
    FAMISTUDIO_NUM_ENVELOPES        = 3+3+2+3+2
    FAMISTUDIO_NUM_PITCH_ENVELOPES  = 4
    FAMISTUDIO_NUM_CHANNELS         = 6
.else
    FAMISTUDIO_NUM_ENVELOPES        = 3+3+2+3
    FAMISTUDIO_NUM_PITCH_ENVELOPES  = 3
    FAMISTUDIO_NUM_CHANNELS         = 5
.endif

FAMISTUDIO_CH0_ENVS = 0
FAMISTUDIO_CH1_ENVS = 3
FAMISTUDIO_CH2_ENVS = 6
FAMISTUDIO_CH3_ENVS = 8

.if .defined(FAMISTUDIO_EXP_VRC6) 
    FAMISTUDIO_CH5_ENVS  = 11
    FAMISTUDIO_CH6_ENVS  = 14
    FAMISTUDIO_CH7_ENVS  = 17
.elseif .defined(FAMISTUDIO_EXP_VRC7) 
    FAMISTUDIO_CH5_ENVS  = 11
    FAMISTUDIO_CH6_ENVS  = 13
    FAMISTUDIO_CH7_ENVS  = 15    
    FAMISTUDIO_CH8_ENVS  = 17
    FAMISTUDIO_CH9_ENVS  = 19
    FAMISTUDIO_CH10_ENVS = 21    
.elseif .defined(FAMISTUDIO_EXP_N163)
    FAMISTUDIO_CH5_ENVS  = 11
    FAMISTUDIO_CH6_ENVS  = 13
    FAMISTUDIO_CH7_ENVS  = 15    
    FAMISTUDIO_CH8_ENVS  = 17
    FAMISTUDIO_CH9_ENVS  = 19
    FAMISTUDIO_CH10_ENVS = 21   
    FAMISTUDIO_CH11_ENVS = 23   
    FAMISTUDIO_CH12_ENVS = 25   
.elseif .defined(FAMISTUDIO_EXP_FDS)
    FAMISTUDIO_CH5_ENVS  = 11
.elseif .defined(FAMISTUDIO_EXP_MMC5)
    FAMISTUDIO_CH5_ENVS  = 11
    FAMISTUDIO_CH6_ENVS  = 14
.elseif .defined(FAMISTUDIO_EXP_S5B)    
    FAMISTUDIO_CH5_ENVS  = 11
    FAMISTUDIO_CH6_ENVS  = 13
    FAMISTUDIO_CH7_ENVS  = 15
.endif

FAMISTUDIO_ENV_VOLUME_OFF = 0
FAMISTUDIO_ENV_NOTE_OFF   = 1
FAMISTUDIO_ENV_DUTY_OFF   = 2

.if .defined(FAMISTUDIO_EXP_VRC7)
    FAMISTUDIO_PITCH_SHIFT = 3
.elseif .defined(FAMISTUDIO_EXP_N163)
    .if FAMISTUDIO_EXP_N163_CHN_CNT > 4
        FAMISTUDIO_PITCH_SHIFT = 5
    .elseif FAMISTUDIO_EXP_N163_CHN_CNT > 2
        FAMISTUDIO_PITCH_SHIFT = 4
    .elseif FAMISTUDIO_EXP_N163_CHN_CNT > 1
        FAMISTUDIO_PITCH_SHIFT = 3
    .else
        FAMISTUDIO_PITCH_SHIFT = 2
    .endif 
.else
    FAMISTUDIO_PITCH_SHIFT = 0
.endif

.if .defined(FAMISTUDIO_EXP_N163)
FAMISTUDIO_N163_CHN_MASK  = (FAMISTUDIO_EXP_N163_CHN_CNT - 1) << 4
.endif

.if .defined(FAMISTUDIO_CFG_SFX_SUPPORT)
FAMISTUDIO_SFX_STRUCT_SIZE = 15

FAMISTUDIO_SFX_CH0 = FAMISTUDIO_SFX_STRUCT_SIZE * 0
FAMISTUDIO_SFX_CH1 = FAMISTUDIO_SFX_STRUCT_SIZE * 1
FAMISTUDIO_SFX_CH2 = FAMISTUDIO_SFX_STRUCT_SIZE * 2
FAMISTUDIO_SFX_CH3 = FAMISTUDIO_SFX_STRUCT_SIZE * 3
.endif

.segment "RAM"

famistudio_env_value:             .res FAMISTUDIO_NUM_ENVELOPES
famistudio_env_repeat:            .res FAMISTUDIO_NUM_ENVELOPES
famistudio_env_addr_lo:           .res FAMISTUDIO_NUM_ENVELOPES
famistudio_env_addr_hi:           .res FAMISTUDIO_NUM_ENVELOPES
famistudio_env_ptr:               .res FAMISTUDIO_NUM_ENVELOPES

famistudio_pitch_env_value_lo:    .res FAMISTUDIO_NUM_PITCH_ENVELOPES
famistudio_pitch_env_value_hi:    .res FAMISTUDIO_NUM_PITCH_ENVELOPES
famistudio_pitch_env_repeat:      .res FAMISTUDIO_NUM_PITCH_ENVELOPES
famistudio_pitch_env_addr_lo:     .res FAMISTUDIO_NUM_PITCH_ENVELOPES
famistudio_pitch_env_addr_hi:     .res FAMISTUDIO_NUM_PITCH_ENVELOPES
famistudio_pitch_env_ptr:         .res FAMISTUDIO_NUM_PITCH_ENVELOPES
.if .defined(FAMISTUDIO_USE_PITCH_TRACK)
famistudio_pitch_env_fine_value:  .res FAMISTUDIO_NUM_PITCH_ENVELOPES
.endif

.if .defined(FAMISTUDIO_USE_SLIDE_NOTES)
famistudio_slide_step:            .res FAMISTUDIO_NUM_PITCH_ENVELOPES
famistudio_slide_pitch_lo:        .res FAMISTUDIO_NUM_PITCH_ENVELOPES
famistudio_slide_pitch_hi:        .res FAMISTUDIO_NUM_PITCH_ENVELOPES
.endif

famistudio_chn_ptr_lo:            .res FAMISTUDIO_NUM_CHANNELS
famistudio_chn_ptr_hi:            .res FAMISTUDIO_NUM_CHANNELS
famistudio_chn_note:              .res FAMISTUDIO_NUM_CHANNELS
famistudio_chn_instrument:        .res FAMISTUDIO_NUM_CHANNELS
famistudio_chn_repeat:            .res FAMISTUDIO_NUM_CHANNELS
famistudio_chn_return_lo:         .res FAMISTUDIO_NUM_CHANNELS
famistudio_chn_return_hi:         .res FAMISTUDIO_NUM_CHANNELS
famistudio_chn_ref_len:           .res FAMISTUDIO_NUM_CHANNELS
.if .defined(FAMISTUDIO_USE_VOLUME_TRACK)
famistudio_chn_volume_track:      .res FAMISTUDIO_NUM_CHANNELS
.endif
.if .defined(FAMISTUDIO_USE_VIBRATO) || .defined(FAMISTUDIO_USE_ARPEGGIO)
famistudio_chn_env_override:      .res FAMISTUDIO_NUM_CHANNELS ; bit 7 = pitch, bit 0 = arpeggio.
.endif
.if .defined(FAMISTUDIO_EXP_N163) || .defined(FAMISTUDIO_EXP_VRC7) || .defined(FAMISTUDIO_EXP_FDS)
famistudio_chn_inst_changed:      .res FAMISTUDIO_NUM_CHANNELS-5
.endif
.if .defined(FAMISTUDIO_CFG_EQUALIZER)
famistudio_chn_note_counter:      .res FAMISTUDIO_NUM_CHANNELS
.endif
.if .defined(FAMISTUDIO_EXP_VRC7)
famistudio_chn_vrc7_prev_hi:      .res 6
famistudio_chn_vrc7_patch:        .res 6
famistudio_chn_vrc7_trigger:      .res 6 ; bit 0 = new note triggered, bit 7 = note released.
.endif
.if .defined(FAMISTUDIO_EXP_N163)
famistudio_chn_n163_wave_len:     .res FAMISTUDIO_EXP_N163_CHN_CNT
.endif

.if .defined(FAMISTUDIO_USE_FAMITRACKER_TEMPO)
famistudio_tempo_step_lo:         .res 1
famistudio_tempo_step_hi:         .res 1
famistudio_tempo_acc_lo:          .res 1
famistudio_tempo_acc_hi:          .res 1
.else
famistudio_tempo_env_ptr_lo:      .res 1
famistudio_tempo_env_ptr_hi:      .res 1
famistudio_tempo_env_counter:     .res 1
famistudio_tempo_env_idx:         .res 1
famistudio_tempo_frame_num:       .res 1
famistudio_tempo_frame_cnt:       .res 1
.endif

famistudio_pal_adjust:            .res 1
famistudio_song_list_lo:          .res 1
famistudio_song_list_hi:          .res 1
famistudio_instrument_lo:         .res 1
famistudio_instrument_hi:         .res 1
famistudio_dpcm_list_lo:          .res 1 ; TODO: Not needed if DPCM support is disabled.
famistudio_dpcm_list_hi:          .res 1 ; TODO: Not needed if DPCM support is disabled.
famistudio_dpcm_effect:           .res 1 ; TODO: Not needed if DPCM support is disabled.
famistudio_pulse1_prev:           .res 1
famistudio_pulse2_prev:           .res 1
famistudio_song_speed             = famistudio_chn_instrument+4

.if .defined(FAMISTUDIO_EXP_MMC5)
famistudio_mmc5_pulse1_prev:      .res 1
famistudio_mmc5_pulse2_prev:      .res 1
.endif

.if .defined(FAMISTUDIO_EXP_FDS)
famistudio_fds_mod_speed:         .res 2
famistudio_fds_mod_depth:         .res 1
famistudio_fds_mod_delay:         .res 1
famistudio_fds_override_flags:    .res 1 ; Bit 7 = mod speed overriden, bit 6 mod depth overriden
.endif

.if .defined(FAMISTUDIO_EXP_VRC7)
famistudio_vrc7_dummy:            .res 1 ; TODO: Find a dummy address i can simply write to without side effects.
.endif

; FDS, N163 and VRC7 have very different instrument layout and are 16-bytes, so we keep them seperate.
.if .defined(FAMISTUDIO_EXP_FDS) || .defined(FAMISTUDIO_EXP_N163) || .defined(FAMISTUDIO_EXP_VRC7) 
famistudio_exp_instrument_lo:     .res 1
famistudio_exp_instrument_hi:     .res 1
.endif

.if .defined(FAMISTUDIO_CFG_SFX_SUPPORT)

famistudio_sfx_addr_lo:    .res 1
famistudio_sfx_addr_hi:    .res 1
famistudio_sfx_base_addr:  .res (FAMISTUDIO_CFG_SFX_STREAMS * FAMISTUDIO_SFX_STRUCT_SIZE)

; TODO: Refactor SFX memory layout. These uses a AoS approach, not fan. 
famistudio_sfx_repeat: famistudio_sfx_base_addr + 0
famistudio_sfx_ptr_lo: famistudio_sfx_base_addr + 1
famistudio_sfx_ptr_hi: famistudio_sfx_base_addr + 2
famistudio_sfx_offset: famistudio_sfx_base_addr + 3
famistudio_sfx_buffer: famistudio_sfx_base_addr + 4

.endif 

.segment "ZEROPAGE"

famistudio_r0:   .res 1
famistudio_r1:   .res 1
famistudio_r2:   .res 1

famistudio_ptr0: .res 2
famistudio_ptr1: .res 2

famistudio_ptr0_lo = famistudio_ptr0+0
famistudio_ptr0_hi = famistudio_ptr0+1
famistudio_ptr1_lo = famistudio_ptr1+0
famistudio_ptr2_hi = famistudio_ptr1+1

.segment "CODE"

FAMISTUDIO_APU_PL1_VOL    = $4000
FAMISTUDIO_APU_PL1_SWEEP  = $4001
FAMISTUDIO_APU_PL1_LO     = $4002
FAMISTUDIO_APU_PL1_HI     = $4003
FAMISTUDIO_APU_PL2_VOL    = $4004
FAMISTUDIO_APU_PL2_SWEEP  = $4005
FAMISTUDIO_APU_PL2_LO     = $4006
FAMISTUDIO_APU_PL2_HI     = $4007
FAMISTUDIO_APU_TRI_LINEAR = $4008
FAMISTUDIO_APU_TRI_LO     = $400a
FAMISTUDIO_APU_TRI_HI     = $400b
FAMISTUDIO_APU_NOISE_VOL  = $400c
FAMISTUDIO_APU_NOISE_LO   = $400e
FAMISTUDIO_APU_NOISE_HI   = $400f
FAMISTUDIO_APU_DMC_FREQ   = $4010
FAMISTUDIO_APU_DMC_RAW    = $4011
FAMISTUDIO_APU_DMC_START  = $4012
FAMISTUDIO_APU_DMC_LEN    = $4013
FAMISTUDIO_APU_SND_CHN    = $4015
FAMISTUDIO_APU_FRAME_CNT  = $4017

.ifdef FAMISTUDIO_EXP_VRC6
FAMISTUDIO_VRC6_PL1_VOL   = $9000
FAMISTUDIO_VRC6_PL1_LO    = $9001
FAMISTUDIO_VRC6_PL1_HI    = $9002
FAMISTUDIO_VRC6_PL2_VOL   = $a000
FAMISTUDIO_VRC6_PL2_LO    = $a001
FAMISTUDIO_VRC6_PL2_HI    = $a002
FAMISTUDIO_VRC6_SAW_VOL   = $b000
FAMISTUDIO_VRC6_SAW_LO    = $b001
FAMISTUDIO_VRC6_SAW_HI    = $b002
.endif

.ifdef FAMISTUDIO_EXP_VRC7
FAMISTUDIO_VRC7_SILENCE   = $e000
FAMISTUDIO_VRC7_REG_SEL   = $9010
FAMISTUDIO_VRC7_REG_WRITE = $9030
FAMISTUDIO_VRC7_REG_LO_1  = $10
FAMISTUDIO_VRC7_REG_LO_2  = $11
FAMISTUDIO_VRC7_REG_LO_3  = $12
FAMISTUDIO_VRC7_REG_LO_4  = $13
FAMISTUDIO_VRC7_REG_LO_5  = $14
FAMISTUDIO_VRC7_REG_LO_6  = $15
FAMISTUDIO_VRC7_REG_HI_1  = $20
FAMISTUDIO_VRC7_REG_HI_2  = $21
FAMISTUDIO_VRC7_REG_HI_3  = $22
FAMISTUDIO_VRC7_REG_HI_4  = $23
FAMISTUDIO_VRC7_REG_HI_5  = $24
FAMISTUDIO_VRC7_REG_HI_6  = $25
FAMISTUDIO_VRC7_REG_VOL_1 = $30
FAMISTUDIO_VRC7_REG_VOL_2 = $31
FAMISTUDIO_VRC7_REG_VOL_3 = $32
FAMISTUDIO_VRC7_REG_VOL_4 = $33
FAMISTUDIO_VRC7_REG_VOL_5 = $34
FAMISTUDIO_VRC7_REG_VOL_6 = $35 
.endif

.ifdef FAMISTUDIO_EXP_MMC5
FAMISTUDIO_MMC5_PL1_VOL   = $5000
FAMISTUDIO_MMC5_PL1_SWEEP = $5001
FAMISTUDIO_MMC5_PL1_LO    = $5002
FAMISTUDIO_MMC5_PL1_HI    = $5003
FAMISTUDIO_MMC5_PL2_VOL   = $5004
FAMISTUDIO_MMC5_PL2_SWEEP = $5005
FAMISTUDIO_MMC5_PL2_LO    = $5006
FAMISTUDIO_MMC5_PL2_HI    = $5007
FAMISTUDIO_MMC5_PCM_MODE  = $5010
FAMISTUDIO_MMC5_SND_CHN   = $5015
.endif

.ifdef FAMISTUDIO_EXP_N163
FAMISTUDIO_N163_SILENCE       = $e000
FAMISTUDIO_N163_ADDR          = $f800
FAMISTUDIO_N163_DATA          = $4800 
FAMISTUDIO_N163_REG_FREQ_LO   = $78
FAMISTUDIO_N163_REG_PHASE_LO  = $79
FAMISTUDIO_N163_REG_FREQ_MID  = $7a
FAMISTUDIO_N163_REG_PHASE_MID = $7b
FAMISTUDIO_N163_REG_FREQ_HI   = $7c
FAMISTUDIO_N163_REG_PHASE_HI  = $7d
FAMISTUDIO_N163_REG_WAVE      = $7e
FAMISTUDIO_N163_REG_VOLUME    = $7f
.endif

.ifdef FAMISTUDIO_EXP_S5B
FAMISTUDIO_S5B_ADDR       = $c000
FAMISTUDIO_S5B_DATA       = $e000
FAMISTUDIO_S5B_REG_LO_A   = $00
FAMISTUDIO_S5B_REG_HI_A   = $01
FAMISTUDIO_S5B_REG_LO_B   = $02
FAMISTUDIO_S5B_REG_HI_B   = $03
FAMISTUDIO_S5B_REG_LO_C   = $04
FAMISTUDIO_S5B_REG_HI_C   = $05
FAMISTUDIO_S5B_REG_NOISE  = $06
FAMISTUDIO_S5B_REG_TONE   = $07
FAMISTUDIO_S5B_REG_VOL_A  = $08
FAMISTUDIO_S5B_REG_VOL_B  = $09
FAMISTUDIO_S5B_REG_VOL_C  = $0a
FAMISTUDIO_S5B_REG_ENV_LO = $0b
FAMISTUDIO_S5B_REG_ENV_HI = $0c
FAMISTUDIO_S5B_REG_SHAPE  = $0d
FAMISTUDIO_S5B_REG_IO_A   = $0e
FAMISTUDIO_S5B_REG_IO_B   = $0f
.endif

.ifdef FAMISTUDIO_EXP_FDS
FAMISTUDIO_FDS_WAV_START  = $4040
FAMISTUDIO_FDS_VOL_ENV    = $4080
FAMISTUDIO_FDS_FREQ_LO    = $4082
FAMISTUDIO_FDS_FREQ_HI    = $4083
FAMISTUDIO_FDS_SWEEP_ENV  = $4084
FAMISTUDIO_FDS_SWEEP_BIAS = $4085
FAMISTUDIO_FDS_MOD_LO     = $4086
FAMISTUDIO_FDS_MOD_HI     = $4087
FAMISTUDIO_FDS_MOD_TABLE  = $4088
FAMISTUDIO_FDS_VOL        = $4089
FAMISTUDIO_FDS_ENV_SPEED  = $408A
.endif

;aliases for the APU registers in the output buffer

.ifndef FAMISTUDIO_CFG_SFX_SUPPORT
FT_MR_PULSE1_V = FAMISTUDIO_APU_PL1_VOL
FT_MR_PULSE1_L = FAMISTUDIO_APU_PL1_LO
FT_MR_PULSE1_H = FAMISTUDIO_APU_PL1_HI
FT_MR_PULSE2_V = FAMISTUDIO_APU_PL2_VOL
FT_MR_PULSE2_L = FAMISTUDIO_APU_PL2_LO
FT_MR_PULSE2_H = FAMISTUDIO_APU_PL2_HI
FT_MR_TRI_V    = FAMISTUDIO_APU_TRI_LINEAR
FT_MR_TRI_L    = FAMISTUDIO_APU_TRI_LO
FT_MR_TRI_H    = FAMISTUDIO_APU_TRI_HI
FT_MR_NOISE_V  = FAMISTUDIO_APU_NOISE_VOL
FT_MR_NOISE_F  = FAMISTUDIO_APU_NOISE_LO
.else ;otherwise write to the output buffer
FT_MR_PULSE1_V = famistudio_output_buf + 0
FT_MR_PULSE1_L = famistudio_output_buf + 1
FT_MR_PULSE1_H = famistudio_output_buf + 2
FT_MR_PULSE2_V = famistudio_output_buf + 3
FT_MR_PULSE2_L = famistudio_output_buf + 4
FT_MR_PULSE2_H = famistudio_output_buf + 5
FT_MR_TRI_V    = famistudio_output_buf + 6
FT_MR_TRI_L    = famistudio_output_buf + 7
FT_MR_TRI_H    = famistudio_output_buf + 8
FT_MR_NOISE_V  = famistudio_output_buf + 9
FT_MR_NOISE_F  = famistudio_output_buf + 10
.endif

;------------------------------------------------------------------------------
; reset APU, initialize FamiTone
; in: A   0 for PAL, not 0 for NTSC
;     X,Y pointer to music data
;------------------------------------------------------------------------------

.proc famistudio_init

    stx famistudio_song_list_lo         ;store music data pointer for further use
    sty famistudio_song_list_hi
    stx famistudio_ptr0_lo
    sty famistudio_ptr0_hi

.ifdef ::FAMISTUDIO_DUAL_SUPPORT
    tax                        ;set SZ flags for A
    beq pal
    lda #97
pal:
.else
    .ifdef ::FAMISTUDIO_CFG_PAL_SUPPORT
        lda #0
    .endif
    .ifdef ::FAMISTUDIO_CFG_NTSC_SUPPORT
        lda #97
    .endif
.endif
    sta famistudio_pal_adjust

    jsr famistudio_music_stop      ;initialize channels and envelopes

    ldy #1
    lda (famistudio_ptr0),y       ;get instrument list address
    sta famistudio_instrument_lo
    iny
    lda (famistudio_ptr0),y
    sta famistudio_instrument_hi
    iny

    .if .defined(::FAMISTUDIO_EXP_FDS) || .defined(::FAMISTUDIO_EXP_N163) || .defined(::FAMISTUDIO_EXP_VRC7) 
        lda (famistudio_ptr0),y       ;get expansion instrument list address
        sta famistudio_exp_instrument_lo
        iny
        lda (famistudio_ptr0),y
        sta famistudio_exp_instrument_hi
        iny
    .endif

    lda (famistudio_ptr0),y       ;get sample list address
    sta famistudio_dpcm_list_lo
    iny
    lda (famistudio_ptr0),y
    sta famistudio_dpcm_list_hi

    lda #$80                   ;previous pulse period MSB, to not write it when not changed
    sta famistudio_pulse1_prev
    sta famistudio_pulse2_prev

    lda #$0f                   ;enable channels, stop DMC
    sta FAMISTUDIO_APU_SND_CHN
    lda #$80                   ;disable triangle length counter
    sta FAMISTUDIO_APU_TRI_LINEAR
    lda #$00                   ;load noise length
    sta FAMISTUDIO_APU_NOISE_HI

    lda #$30                   ;volumes to 0
    sta FAMISTUDIO_APU_PL1_VOL
    sta FAMISTUDIO_APU_PL2_VOL
    sta FAMISTUDIO_APU_NOISE_VOL
    lda #$08                   ;no sweep
    sta FAMISTUDIO_APU_PL1_SWEEP
    sta FAMISTUDIO_APU_PL2_SWEEP

.ifdef ::FAMISTUDIO_EXP_VRC7
    lda #0
    sta FAMISTUDIO_VRC7_SILENCE ; Enable VRC7 audio.
.endif

.ifdef ::FAMISTUDIO_EXP_MMC5
    lda #$00
    sta FAMISTUDIO_MMC5_PCM_MODE
    lda #$03
    sta FAMISTUDIO_MMC5_SND_CHN
.endif

.ifdef ::FAMISTUDIO_EXP_S5B
    lda #FAMISTUDIO_S5B_REG_TONE
    sta FAMISTUDIO_S5B_ADDR
    lda #$38 ; No noise, just 3 tones for now.
    sta FAMISTUDIO_S5B_DATA
.endif

    jmp famistudio_music_stop

.endproc

;------------------------------------------------------------------------------
; stop music that is currently playing, if any
; in: none
;------------------------------------------------------------------------------

.proc famistudio_music_stop

    lda #0
    sta famistudio_song_speed          ;stop music, reset pause flag
    sta famistudio_dpcm_effect         ;no DPCM effect playing

    ldx #0    ;initialize channel structures

set_channels:

    lda #0
    sta famistudio_chn_repeat,x
    sta famistudio_chn_instrument,x
    sta famistudio_chn_note,x
    sta famistudio_chn_ref_len,x
    .ifdef ::FAMISTUDIO_USE_VOLUME_TRACK    
        sta famistudio_chn_volume_track,x
    .endif    
    .if .defined(FAMISTUDIO_USE_VIBRATO) || .defined(FAMISTUDIO_USE_ARPEGGIO)
        sta famistudio_chn_env_override,x
    .endif

nextchannel:
    inx                        ;next channel
    cpx #FAMISTUDIO_NUM_CHANNELS
    bne set_channels

.ifdef ::FAMISTUDIO_USE_SLIDE_NOTES
    ldx #0    ;initialize all slides to zero
    lda #0
set_slides:

    sta famistudio_slide_step, x
    inx                        ;next channel
    cpx #FAMISTUDIO_NUM_PITCH_ENVELOPES
    bne set_slides
.endif

    ldx #0    ;initialize all envelopes to the dummy envelope

set_envelopes:

    lda #.lobyte(famistudio_dummy_envelope)
    sta famistudio_env_addr_lo,x
    lda #.hibyte(famistudio_dummy_envelope)
    sta famistudio_env_addr_hi,x
    lda #0
    sta famistudio_env_repeat,x
    sta famistudio_env_value,x
    sta famistudio_env_ptr,x
    inx
    cpx #FAMISTUDIO_NUM_ENVELOPES
    bne set_envelopes

    ldx #0    ;initialize all envelopes to the dummy envelope

set_pitch_envelopes:

    lda #.lobyte(famistudio_dummy_pitch_envelope)
    sta famistudio_pitch_env_addr_lo,x
    lda #.hibyte(famistudio_dummy_pitch_envelope)
    sta famistudio_pitch_env_addr_hi,x
    lda #0
    sta famistudio_pitch_env_repeat,x
    sta famistudio_pitch_env_value_lo,x
    sta famistudio_pitch_env_value_hi,x
    .ifdef ::FAMISTUDIO_USE_PITCH_TRACK
        sta famistudio_pitch_env_fine_value,x
    .endif
    lda #1
    sta famistudio_pitch_env_ptr,x
    inx
    cpx #FAMISTUDIO_NUM_PITCH_ENVELOPES
    bne set_pitch_envelopes

    jmp famistudio_sample_stop

.endproc

;------------------------------------------------------------------------------
; play music
; in: A number of subsong
;------------------------------------------------------------------------------

.proc famistudio_music_play

    tmp = famistudio_ptr0_lo

    ldx famistudio_song_list_lo
    stx famistudio_ptr0_lo
    ldx famistudio_song_list_hi
    stx famistudio_ptr0_hi

    ldy #0
    cmp (famistudio_ptr0),y       ;check if there is such sub song
    bcc valid_song
    rts

valid_song:
.if ::FAMISTUDIO_NUM_CHANNELS = 5
    asl
    sta tmp
    asl
    tax
    asl
    adc tmp
    stx tmp
    adc tmp
.elseif ::FAMISTUDIO_NUM_CHANNELS = 6
    asl
    asl
    asl
    asl
.elseif ::FAMISTUDIO_NUM_CHANNELS = 7
    asl
    sta tmp
    asl
    asl
    asl
    adc tmp  
.elseif ::FAMISTUDIO_NUM_CHANNELS = 8
    asl
    asl
    sta tmp
    asl
    asl
    adc tmp
.elseif ::FAMISTUDIO_NUM_CHANNELS = 9
    asl
    sta tmp
    asl
    tax
    asl
    asl
    adc tmp
    stx tmp
    adc tmp
.elseif ::FAMISTUDIO_NUM_CHANNELS = 10
    asl
    asl
    asl
    sta tmp
    asl
    adc tmp  
.elseif ::FAMISTUDIO_NUM_CHANNELS = 11
    asl
    sta tmp
    asl
    asl
    tax
    asl
    adc tmp
    stx tmp
    adc tmp
.elseif ::FAMISTUDIO_NUM_CHANNELS = 12
    asl
    asl
    sta tmp
    asl
    tax
    asl
    adc tmp
    stx tmp
    adc tmp
.elseif ::FAMISTUDIO_NUM_CHANNELS = 13
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

.if .defined(::FAMISTUDIO_EXP_FDS) || .defined(::FAMISTUDIO_EXP_VRC7) || .defined(::FAMISTUDIO_EXP_N163)
    adc #7                     ;add offset
.else
    adc #5                     ;add offset
.endif
    tay

    lda famistudio_song_list_lo         ;restore pointer LSB
    sta famistudio_ptr0_lo

    jsr famistudio_music_stop      ;stop music, initialize channels and envelopes

    ldx #0    ;initialize channel structures

set_channels:

    lda (famistudio_ptr0),y       ;read channel pointers
    sta famistudio_chn_ptr_lo,x
    iny
    lda (famistudio_ptr0),y
    sta famistudio_chn_ptr_hi,x
    iny

    lda #0
    sta famistudio_chn_repeat,x
    sta famistudio_chn_instrument,x
    sta famistudio_chn_note,x
    sta famistudio_chn_ref_len,x
    .ifdef ::FAMISTUDIO_USE_VOLUME_TRACK
        lda #$f0
        sta famistudio_chn_volume_track,x
    .endif

nextchannel:
    inx                        ;next channel
    cpx #FAMISTUDIO_NUM_CHANNELS
    bne set_channels

.ifdef ::FAMISTUDIO_USE_FAMITRACKER_TEMPO
    lda famistudio_pal_adjust          ;read tempo for PAL or NTSC
    beq pal
    iny
    iny
pal:

    lda (famistudio_ptr0),y       ;read the tempo step
    sta famistudio_tempo_step_lo
    iny
    lda (famistudio_ptr0),y
    sta famistudio_tempo_step_hi

    lda #0                     ;reset tempo accumulator
    sta famistudio_tempo_acc_lo
    lda #6                     ;default speed
    sta famistudio_tempo_acc_hi
    sta famistudio_song_speed          ;apply default speed, this also enables music
.else
    lda (famistudio_ptr0),y
    sta famistudio_tempo_env_ptr_lo
    sta famistudio_ptr1+0
    iny
    lda (famistudio_ptr0),y
    sta famistudio_tempo_env_ptr_hi
    sta famistudio_ptr1+1
    iny
    lda (famistudio_ptr0),y
.if .defined(::FAMISTUDIO_DUAL_SUPPORT) ; Dual mode
    ldx famistudio_pal_adjust
    bne ntsc_target
    ora #1
    ntsc_target:
.elseif .defined(::FAMISTUDIO_CFG_PAL_SUPPORT) ; PAL only
    ora #1
.endif
    tax
    lda famistudio_tempo_frame_lookup, x ; Lookup contains the number of frames to run (0,1,2) to maintain tempo
    sta famistudio_tempo_frame_num
    ldy #0
    sty famistudio_tempo_env_idx
    lda (famistudio_ptr1),y
    clc 
    adc #1
    sta famistudio_tempo_env_counter
    lda #6
    sta famistudio_song_speed          ; simply so the song isnt considered paused.
.endif

.ifdef ::FAMISTUDIO_EXP_VRC7
    lda #0
    ldx #5
    clear_vrc7_loop:
        sta famistudio_chn_vrc7_prev_hi, x
        sta famistudio_chn_vrc7_patch, x
        sta famistudio_chn_vrc7_trigger,x
        dex
        bpl clear_vrc7_loop 
.endif

.ifdef FAMISTUDIO_EXP_FDS
    lda #0
    sta famistudio_fds_mod_speed+0
    sta famistudio_fds_mod_speed+1
    sta famistudio_fds_mod_depth
    sta famistudio_fds_mod_delay
    sta famistudio_fds_override_flags
.endif

.ifdef famistudio_chn_inst_changed
    lda #0
    ldx #(FAMISTUDIO_NUM_CHANNELS-5)
    clear_inst_changed_loop:
        sta famistudio_chn_inst_changed, x
        dex
        bpl clear_inst_changed_loop 
.endif

.ifdef ::FAMISTUDIO_EXP_N163
    lda #0
    ldx #FAMISTUDIO_EXP_N163_CHN_CNT
    clear_vrc7_loop:
        sta famistudio_chn_n163_wave_len, x
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

.proc famistudio_music_pause

    tax                        ;set SZ flags for A
    beq unpause
    
pause:

    jsr famistudio_sample_stop
    
    lda #0                     ;mute sound
    sta famistudio_env_value+FAMISTUDIO_CH0_ENVS+FAMISTUDIO_ENV_VOLUME_OFF
    sta famistudio_env_value+FAMISTUDIO_CH1_ENVS+FAMISTUDIO_ENV_VOLUME_OFF
    sta famistudio_env_value+FAMISTUDIO_CH2_ENVS+FAMISTUDIO_ENV_VOLUME_OFF
    sta famistudio_env_value+FAMISTUDIO_CH3_ENVS+FAMISTUDIO_ENV_VOLUME_OFF
    lda famistudio_song_speed          ;set pause flag
    ora #$80
    bne done
unpause:
    lda famistudio_song_speed          ;reset pause flag
    and #$7f
done:
    sta famistudio_song_speed

    rts

.endproc

; x = note index
; y = slide/pitch envelope index
.macro famistudio_get_note_pitch_macro pitch_env_offset, pitch_env_indexer, note_table_lsb, note_table_msb

    .local pitch
    .local tmp_ror
    .local pos
    .local no_slide

    pitch   = famistudio_ptr1
    tmp_ror = famistudio_r0
 
.ifdef ::FAMISTUDIO_USE_PITCH_TRACK

    ; Pitch envelope + fine pitch (sign extended)
    clc
    lda famistudio_pitch_env_fine_value+pitch_env_offset pitch_env_indexer
    adc famistudio_pitch_env_value_lo+pitch_env_offset pitch_env_indexer
    sta pitch+0
    lda famistudio_pitch_env_fine_value+pitch_env_offset pitch_env_indexer
    and #$80
    beq pos
    lda #$ff
pos:
    adc famistudio_pitch_env_value_hi+pitch_env_offset pitch_env_indexer
    sta pitch+1

.else

    lda famistudio_pitch_env_value_lo+pitch_env_offset pitch_env_indexer
    sta pitch+0
    lda famistudio_pitch_env_value_hi+pitch_env_offset pitch_env_indexer
    sta pitch+1

.endif

.ifdef ::FAMISTUDIO_USE_SLIDE_NOTES
    ; Check if there is an active slide.
    lda famistudio_slide_step+pitch_env_offset pitch_env_indexer
    beq no_slide

    ; Add slide
.if pitch_env_offset >= 3 && (.defined(::FAMISTUDIO_EXP_VRC7) || .defined(::FAMISTUDIO_EXP_N163))
    ; These channels dont have fractional part for slides and have the same shift for slides + pitch.
    clc
    lda famistudio_slide_pitch_lo+pitch_env_offset pitch_env_indexer
    adc pitch+0
    sta pitch+0
    lda famistudio_slide_pitch_hi+pitch_env_offset pitch_env_indexer
    adc pitch+1
    sta pitch+1     
 .else
    ; Most channels have 1 bit of fraction for slides.
    lda famistudio_slide_pitch_hi+pitch_env_offset pitch_env_indexer
    cmp #$80 ; sign extend upcoming right shift.
    ror ; we have 1 bit of fraction for slides, shift right hi byte.
    sta tmp_ror
    lda famistudio_slide_pitch_lo+pitch_env_offset pitch_env_indexer
    ror ; shift right low byte.
    clc
    adc pitch+0
    sta pitch+0
    lda tmp_ror
    adc pitch+1 
    sta pitch+1 
.endif
.endif

no_slide:    

.if pitch_env_offset >= 3 && (.defined(::FAMISTUDIO_EXP_VRC7) || .defined(::FAMISTUDIO_EXP_N163))
    .if ::FAMISTUDIO_PITCH_SHIFT >= 1
        asl pitch+0
        rol pitch+1
    .if ::FAMISTUDIO_PITCH_SHIFT >= 2
        asl pitch+0
        rol pitch+1
    .if ::FAMISTUDIO_PITCH_SHIFT >= 3
        asl pitch+0
        rol pitch+1
    .if ::FAMISTUDIO_PITCH_SHIFT >= 4
        asl pitch+0
        rol pitch+1
    .if ::FAMISTUDIO_PITCH_SHIFT >= 5
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

.proc famistudio_get_note_pitch
    famistudio_get_note_pitch_macro 0, {,y}, famistudio_note_table_lsb, famistudio_note_table_msb
    rts
.endproc

.ifdef ::FAMISTUDIO_EXP_VRC6
.proc famistudio_get_note_pitch_vrc6_saw
    famistudio_get_note_pitch_macro 0, {,y}, famistudio_saw_note_table_lsb, famistudio_saw_note_table_msb
    rts
.endproc
.endif

.macro famistudio_update_channel_sound idx, env_offset, slide_offset, pulse_prev, vol_ora, hi_ora, reg_hi, reg_lo, reg_vol, reg_sweep

    .local note_table_lsb
    .local note_table_msb
    .local pitch
    .local tmp
    .local nocut
    .local set_volume
    .local compute_volume
    .local hi_delta_too_big

    tmp   = famistudio_r0
    pitch = famistudio_ptr1

    lda famistudio_chn_note+idx
    bne nocut
    jmp set_volume

nocut:
    clc
    adc famistudio_env_value+env_offset+FAMISTUDIO_ENV_NOTE_OFF

.ifblank slide_offset ;  noise channel is a bit special    

    and #$0f
    eor #$0f
    sta tmp
    ldx famistudio_env_value+env_offset+FAMISTUDIO_ENV_DUTY_OFF
    lda famistudio_duty_lookup, x
    asl a
    and #$80
    ora tmp

.else

    .ifdef ::FAMISTUDIO_DUAL_SUPPORT
        clc
        adc famistudio_pal_adjust
    .endif
    tax

    ldy #slide_offset
    .if .defined(::FAMISTUDIO_EXP_VRC6) && idx = 7
        jsr famistudio_get_note_pitch_vrc6_saw
    .else
        jsr famistudio_get_note_pitch
    .endif

    lda pitch+0
    sta reg_lo
    lda pitch+1

    .ifnblank pulse_prev

        .ifndef ::FAMISTUDIO_CFG_SFX_SUPPORT
            .if (!.blank(reg_sweep)) && .defined(::FAMISTUDIO_CFG_SMOOTH_VIBRATO)
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
                stx FAMISTUDIO_APU_FRAME_CNT ; reset frame counter in case it was about to clock
                lda famistudio_smooth_vibrato_period_lo_lookup, y ; be sure low 8 bits of timer period are $ff (for positive delta), or $00 (for negative delta)
                sta reg_lo
                lda famistudio_smooth_vibrato_sweep_lookup, y ; sweep enabled, shift = 7, set negative flag or delta is negative..
                sta reg_sweep
                lda #$c0
                sta FAMISTUDIO_APU_FRAME_CNT ; clock sweep immediately
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

.if .blank(pulse_prev) || .blank(reg_sweep) || (!.defined(::FAMISTUDIO_CFG_SMOOTH_VIBRATO))
    sta reg_hi
.endif

compute_volume:
    lda famistudio_env_value+env_offset+FAMISTUDIO_ENV_VOLUME_OFF

    .ifdef ::FAMISTUDIO_USE_VOLUME_TRACK    
        ora famistudio_chn_volume_track+idx ; TODO: Triangle channel doesnt really need a volume track. Make it optional.
        tax
        lda famistudio_volume_table, x 
    .endif

.if .defined(FAMISTUDIO_EXP_VRC6) && idx = 7 
    ; VRC6 saw has 6-bits
    asl
    asl
.endif

set_volume:

.if idx = 0 || idx = 1 || idx = 3 || (idx >= 5 && .defined(::FAMISTUDIO_EXP_MMC5))
    ldx famistudio_env_value+env_offset+FAMISTUDIO_ENV_DUTY_OFF
    ora famistudio_duty_lookup, x
.elseif (idx = 5 || idx = 6) && .defined(::FAMISTUDIO_EXP_VRC6)
    ldx famistudio_env_value+env_offset+FAMISTUDIO_ENV_DUTY_OFF
    ora famistudio_vrc6_duty_lookup, x
.endif

.ifnblank vol_ora
    ora vol_ora
.endif

    sta reg_vol

.endmacro

.ifdef FAMISTUDIO_EXP_FDS

.proc famistudio_update_fds_channel_sound

    pitch = famistudio_ptr1

    lda famistudio_chn_note+5
    bne nocut
    jmp set_volume

nocut:
    clc
    adc famistudio_env_value+FAMISTUDIO_CH5_ENVS+FAMISTUDIO_ENV_NOTE_OFF
    tax

    famistudio_get_note_pitch_macro 3, , famistudio_fds_note_table_lsb, famistudio_fds_note_table_msb

    lda pitch+0
    sta FAMISTUDIO_FDS_FREQ_LO
    lda pitch+1
    sta FAMISTUDIO_FDS_FREQ_HI

check_mod_delay:
    lda famistudio_fds_mod_delay
    beq zero_delay
    dec famistudio_fds_mod_delay
    lda #$80
    sta FAMISTUDIO_FDS_MOD_HI
    bne compute_volume

zero_delay:
    lda famistudio_fds_mod_speed+1
    sta FAMISTUDIO_FDS_MOD_HI
    lda famistudio_fds_mod_speed+0
    sta FAMISTUDIO_FDS_MOD_LO
    lda famistudio_fds_mod_depth
    ora #$80
    sta FAMISTUDIO_FDS_SWEEP_ENV

compute_volume:
    lda famistudio_env_value+FAMISTUDIO_CH5_ENVS+FAMISTUDIO_ENV_VOLUME_OFF
    .ifdef ::FAMISTUDIO_USE_VOLUME_TRACK
        ora famistudio_chn_volume_track+5 
        tax
        lda famistudio_volume_table, x 
    .endif
    asl ; FDS volume is 6-bits, but clamped to 32. Just double it.

set_volume:
    ora #$80
    sta FAMISTUDIO_FDS_VOL_ENV
    lda #0
    sta famistudio_fds_override_flags

    rts 

.endproc

.endif

.ifdef FAMISTUDIO_EXP_VRC7

famistudio_vrc7_reg_table_lo:
    .byte FAMISTUDIO_VRC7_REG_LO_1, FAMISTUDIO_VRC7_REG_LO_2, FAMISTUDIO_VRC7_REG_LO_3, FAMISTUDIO_VRC7_REG_LO_4, FAMISTUDIO_VRC7_REG_LO_5, FAMISTUDIO_VRC7_REG_LO_6
famistudio_vrc7_reg_table_hi:
    .byte FAMISTUDIO_VRC7_REG_HI_1, FAMISTUDIO_VRC7_REG_HI_2, FAMISTUDIO_VRC7_REG_HI_3, FAMISTUDIO_VRC7_REG_HI_4, FAMISTUDIO_VRC7_REG_HI_5, FAMISTUDIO_VRC7_REG_HI_6
famistudio_vrc7_vol_table:
    .byte FAMISTUDIO_VRC7_REG_VOL_1, FAMISTUDIO_VRC7_REG_VOL_2, FAMISTUDIO_VRC7_REG_VOL_3, FAMISTUDIO_VRC7_REG_VOL_4, FAMISTUDIO_VRC7_REG_VOL_5, FAMISTUDIO_VRC7_REG_VOL_6
famistudio_vrc7_env_table:
    .byte FAMISTUDIO_CH5_ENVS, FAMISTUDIO_CH6_ENVS, FAMISTUDIO_CH7_ENVS, FAMISTUDIO_CH8_ENVS, FAMISTUDIO_CH9_ENVS, FAMISTUDIO_CH10_ENVS 
famistudio_vrc7_invert_vol_table:
    .byte $0f, $0e, $0d, $0c, $0b, $0a, $09, $08, $07, $06, $05, $04, $03, $02, $01, $00

; From nesdev wiki.
.proc famistudio_vrc7_wait_reg_write
    stx famistudio_vrc7_dummy
    ldx #$08
    wait_loop:
        dex
        bne wait_loop
        ldx famistudio_vrc7_dummy
    rts
.endproc

.proc famistudio_vrc7_wait_reg_select
    rts
.endproc

; y = VRC7 channel idx (0,1,2,3,4,5)
.proc famistudio_update_vrc7_channel_sound

    chan_idx = famistudio_r2
    pitch    = famistudio_ptr1

    lda #0
    sta famistudio_chn_inst_changed,y

    lda famistudio_chn_vrc7_trigger,y
    bpl check_cut

release:
   
    ; Untrigger note.  
    lda famistudio_vrc7_reg_table_hi,y
    sta FAMISTUDIO_VRC7_REG_SEL
    jsr famistudio_vrc7_wait_reg_select

    lda famistudio_chn_vrc7_prev_hi, y
    and #$ef ; remove trigger
    sta famistudio_chn_vrc7_prev_hi, y
    sta FAMISTUDIO_VRC7_REG_WRITE
    jsr famistudio_vrc7_wait_reg_write   

    rts

check_cut:

    lda famistudio_chn_note+5,y
    bne nocut

cut:  
    ; Untrigger note.  
    lda famistudio_vrc7_reg_table_hi,y
    sta FAMISTUDIO_VRC7_REG_SEL
    jsr famistudio_vrc7_wait_reg_select

    lda famistudio_chn_vrc7_prev_hi, y
    and #$cf ; remove trigger + sustain
    sta famistudio_chn_vrc7_prev_hi, y
    sta FAMISTUDIO_VRC7_REG_WRITE
    jsr famistudio_vrc7_wait_reg_write

    rts

nocut:

    ; Read note, apply arpeggio 
    clc
    ldx famistudio_vrc7_env_table,y    
    adc famistudio_env_value+FAMISTUDIO_ENV_NOTE_OFF,x
    tax

    ; Apply pitch envelope, fine pitch & slides
    famistudio_get_note_pitch_macro 3, {,y}, famistudio_vrc7_note_table_lsb, famistudio_vrc7_note_table_msb

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
    lda famistudio_vrc7_reg_table_lo,y
    sta FAMISTUDIO_VRC7_REG_SEL
    jsr famistudio_vrc7_wait_reg_select

    lda pitch+0
    sta FAMISTUDIO_VRC7_REG_WRITE
    jsr famistudio_vrc7_wait_reg_write

    ; Un-trigger previous note if needed.
    lda famistudio_chn_vrc7_prev_hi, y
    and #$10 ; set trigger.
    beq write_hi_period
    lda famistudio_chn_vrc7_trigger,y
    beq write_hi_period
    untrigger_prev_note:
        lda famistudio_vrc7_reg_table_hi,y
        sta FAMISTUDIO_VRC7_REG_SEL
        jsr famistudio_vrc7_wait_reg_select

        lda famistudio_chn_vrc7_prev_hi,y
        and #$ef ; remove trigger
        sta FAMISTUDIO_VRC7_REG_WRITE
        jsr famistudio_vrc7_wait_reg_write

    write_hi_period:

    ; Write pitch (hi)
    lda famistudio_vrc7_reg_table_hi,y
    sta FAMISTUDIO_VRC7_REG_SEL
    jsr famistudio_vrc7_wait_reg_select

    txa
    asl
    ora #$30
    ora pitch+1
    sta famistudio_chn_vrc7_prev_hi, y
    sta FAMISTUDIO_VRC7_REG_WRITE
    jsr famistudio_vrc7_wait_reg_write

    ; Read/multiply volume
    ldx famistudio_vrc7_env_table,y
    lda famistudio_env_value+FAMISTUDIO_ENV_VOLUME_OFF,x
    .ifdef ::FAMISTUDIO_USE_VOLUME_TRACK
        ora famistudio_chn_volume_track+5, y
    .endif
    tax

    lda #0
    sta famistudio_chn_vrc7_trigger,y

update_volume:

    ; Write volume
    lda famistudio_vrc7_vol_table,y
    sta FAMISTUDIO_VRC7_REG_SEL
    jsr famistudio_vrc7_wait_reg_select
    .ifdef ::FAMISTUDIO_USE_VOLUME_TRACK
        lda famistudio_volume_table,x
        tax
    .endif
    lda famistudio_vrc7_invert_vol_table,x
    ora famistudio_chn_vrc7_patch,y
    sta FAMISTUDIO_VRC7_REG_WRITE
    jsr famistudio_vrc7_wait_reg_write

    rts

.endproc 

.endif

.ifdef FAMISTUDIO_EXP_N163

_FT2N163RegLoTable:
    .byte FAMISTUDIO_N163_REG_FREQ_LO - $00
    .byte FAMISTUDIO_N163_REG_FREQ_LO - $08
    .byte FAMISTUDIO_N163_REG_FREQ_LO - $10
    .byte FAMISTUDIO_N163_REG_FREQ_LO - $18
    .byte FAMISTUDIO_N163_REG_FREQ_LO - $20
    .byte FAMISTUDIO_N163_REG_FREQ_LO - $28
    .byte FAMISTUDIO_N163_REG_FREQ_LO - $30
    .byte FAMISTUDIO_N163_REG_FREQ_LO - $38
_FT2N163RegMidTable:
    .byte FAMISTUDIO_N163_REG_FREQ_MID - $00
    .byte FAMISTUDIO_N163_REG_FREQ_MID - $08
    .byte FAMISTUDIO_N163_REG_FREQ_MID - $10
    .byte FAMISTUDIO_N163_REG_FREQ_MID - $18
    .byte FAMISTUDIO_N163_REG_FREQ_MID - $20
    .byte FAMISTUDIO_N163_REG_FREQ_MID - $28
    .byte FAMISTUDIO_N163_REG_FREQ_MID - $30
    .byte FAMISTUDIO_N163_REG_FREQ_MID - $38
_FT2N163RegHiTable:
    .byte FAMISTUDIO_N163_REG_FREQ_HI - $00
    .byte FAMISTUDIO_N163_REG_FREQ_HI - $08
    .byte FAMISTUDIO_N163_REG_FREQ_HI - $10
    .byte FAMISTUDIO_N163_REG_FREQ_HI - $18
    .byte FAMISTUDIO_N163_REG_FREQ_HI - $20
    .byte FAMISTUDIO_N163_REG_FREQ_HI - $28
    .byte FAMISTUDIO_N163_REG_FREQ_HI - $30
    .byte FAMISTUDIO_N163_REG_FREQ_HI - $38
_FT2N163VolTable:
    .byte FAMISTUDIO_N163_REG_VOLUME - $00
    .byte FAMISTUDIO_N163_REG_VOLUME - $08
    .byte FAMISTUDIO_N163_REG_VOLUME - $10
    .byte FAMISTUDIO_N163_REG_VOLUME - $18
    .byte FAMISTUDIO_N163_REG_VOLUME - $20
    .byte FAMISTUDIO_N163_REG_VOLUME - $28
    .byte FAMISTUDIO_N163_REG_VOLUME - $30
    .byte FAMISTUDIO_N163_REG_VOLUME - $38    
_FT2N163EnvelopeTable:
    .byte FAMISTUDIO_CH5_ENVS
    .byte FAMISTUDIO_CH6_ENVS
    .byte FAMISTUDIO_CH7_ENVS
    .byte FAMISTUDIO_CH8_ENVS
    .byte FAMISTUDIO_CH9_ENVS
    .byte FAMISTUDIO_CH10_ENVS
    .byte FAMISTUDIO_CH11_ENVS
    .byte FAMISTUDIO_CH12_ENVS

; y = N163 channel idx (0,1,2,3,4,5,6,7)
.proc famistudio_update_n163_channel_sound
    
    pitch    = famistudio_ptr1
    pitch_hi = famistudio_r2

    lda famistudio_chn_note+5,y
    bne nocut
    ldx #0 ; this will fetch volume 0.
    bne nocut
    jmp update_volume

nocut:

    ; Read note, apply arpeggio 
    clc
    ldx _FT2N163EnvelopeTable,y
    adc famistudio_env_value+FAMISTUDIO_ENV_NOTE_OFF,x
    tax

    ; Apply pitch envelope, fine pitch & slides
    famistudio_get_note_pitch_macro 3, {,y}, famistudio_n163_note_table_lsb, famistudio_n163_note_table_msb

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
    sta FAMISTUDIO_N163_ADDR
    lda pitch+0
    sta FAMISTUDIO_N163_DATA
    lda _FT2N163RegMidTable,y
    sta FAMISTUDIO_N163_ADDR
    lda pitch+1
    sta FAMISTUDIO_N163_DATA
    lda _FT2N163RegHiTable,y
    sta FAMISTUDIO_N163_ADDR
    lda famistudio_chn_n163_wave_len,y
    ora pitch_hi
    sta FAMISTUDIO_N163_DATA

    ; Read/multiply volume
    ldx _FT2N163EnvelopeTable,y
    lda famistudio_env_value+FAMISTUDIO_ENV_VOLUME_OFF,x
    .ifdef ::FAMISTUDIO_USE_VOLUME_TRACK
        ora famistudio_chn_volume_track+5, y
    .endif
    tax

update_volume:
    ; Write volume
    lda _FT2N163VolTable,y
    sta FAMISTUDIO_N163_ADDR
    .ifdef ::FAMISTUDIO_USE_VOLUME_TRACK
        lda famistudio_volume_table,x 
    .else
        txa
    .endif
    ora #FAMISTUDIO_N163_CHN_MASK
    sta FAMISTUDIO_N163_DATA
    
    lda #0
    sta famistudio_chn_inst_changed,y

    rts

.endproc
.endif

.ifdef FAMISTUDIO_EXP_S5B

_FT2S5BRegLoTable:
    .byte FAMISTUDIO_S5B_REG_LO_A, FAMISTUDIO_S5B_REG_LO_B, FAMISTUDIO_S5B_REG_LO_C
_FT2S5BRegHiTable:
    .byte FAMISTUDIO_S5B_REG_HI_A, FAMISTUDIO_S5B_REG_HI_B, FAMISTUDIO_S5B_REG_HI_C
_FT2S5BVolTable:
    .byte FAMISTUDIO_S5B_REG_VOL_A, FAMISTUDIO_S5B_REG_VOL_B, FAMISTUDIO_S5B_REG_VOL_C
_FT2S5BEnvelopeTable:
    .byte FAMISTUDIO_CH5_ENVS, FAMISTUDIO_CH6_ENVS, FAMISTUDIO_CH7_ENVS

; y = S5B channel idx (0,1,2)
.proc famistudio_update_s5b_channel_sound
    
    pitch = famistudio_ptr1

    lda famistudio_chn_note+5,y
    bne nocut
    ldx #0 ; this will fetch volume 0.
    beq update_volume

nocut:
    
    ; Read note, apply arpeggio 
    clc
    ldx _FT2S5BEnvelopeTable,y
    adc famistudio_env_value+FAMISTUDIO_ENV_NOTE_OFF,x
    tax

    ; Apply pitch envelope, fine pitch & slides
    famistudio_get_note_pitch_macro 3, {,y}, famistudio_note_table_lsb, famistudio_note_table_msb

    ; Write pitch
    lda _FT2S5BRegLoTable,y
    sta FAMISTUDIO_S5B_ADDR
    lda pitch+0
    sta FAMISTUDIO_S5B_DATA
    lda _FT2S5BRegHiTable,y
    sta FAMISTUDIO_S5B_ADDR
    lda pitch+1
    sta FAMISTUDIO_S5B_DATA

    ; Read/multiply volume
    ldx _FT2S5BEnvelopeTable,y
    lda famistudio_env_value+FAMISTUDIO_ENV_VOLUME_OFF,x
    .ifdef ::FAMISTUDIO_USE_VOLUME_TRACK
        ora famistudio_chn_volume_track+5, y
    .endif
    tax

update_volume:
    ; Write volume
    lda _FT2S5BVolTable,y
    sta FAMISTUDIO_S5B_ADDR
    .ifdef ::FAMISTUDIO_USE_VOLUME_TRACK    
        lda famistudio_volume_table,x 
        sta FAMISTUDIO_S5B_DATA
    .else
        stx FAMISTUDIO_S5B_DATA
    .endif

    rts
.endproc
.endif

.macro famistudio_update_row channel_idx, env_idx

    .local @no_new_note
.ifdef ::FAMISTUDIO_CFG_EQUALIZER
    .local @new_note
    .local @done
.endif

    ldx #channel_idx
    jsr famistudio_channel_update
    bcc @no_new_note
    ldx #env_idx
    ldy #channel_idx
    lda famistudio_chn_instrument+channel_idx

.if .defined(::FAMISTUDIO_EXP_FDS) && channel_idx >= 5
    jsr famistudio_set_fds_instrument
.elseif .defined(::FAMISTUDIO_EXP_VRC7) && channel_idx >= 5
    jsr famistudio_set_vrc7_instrument
.elseif .defined(::FAMISTUDIO_EXP_N163) && channel_idx >= 5
    jsr famistudio_set_n163_instrument
.else
    jsr famistudio_set_instrument
.endif

.ifdef ::FAMISTUDIO_CFG_EQUALIZER
    @new_note:
        ldx #channel_idx
        lda #8
        sta famistudio_chn_note_counter, x
        jmp @done
    @no_new_note:
        ldx #channel_idx
        lda famistudio_chn_note_counter, x
        beq @done
        dec famistudio_chn_note_counter, x
    @done:    
.else
    .local @no_new_note ; why do i need this?
    @no_new_note:
.endif

.endmacro

.macro famistudio_update_row_dpcm channel_idx
.ifdef ::FAMISTUDIO_CFG_DPCM_SUPPORT
    .local @play_sample
    .local @no_new_note
    ldx #channel_idx    ;process channel 5
    jsr famistudio_channel_update
    bcc @no_new_note
    lda famistudio_chn_note+channel_idx
    bne @play_sample
    jsr famistudio_sample_stop
    bne @no_new_note    ;A is non-zero after famistudio_sample_stop
@play_sample:
    jsr famistudio_sample_play_music

.ifdef ::FAMISTUDIO_CFG_EQUALIZER
    .local @done
    .local @new_note
    @new_note:
        ldx #channel_idx
        lda #8
        sta famistudio_chn_note_counter, x
        jmp @done
    @no_new_note:
        ldx #channel_idx
        lda famistudio_chn_note_counter, x
        beq @done
        dec famistudio_chn_note_counter, x
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

.proc famistudio_update

    .if(::FAMISTUDIO_CFG_THREAD)
    lda famistudio_ptr0_lo
    pha
    lda famistudio_ptr0_hi
    pha
    .endif

    lda famistudio_song_speed          ;speed 0 means that no music is playing currently
    bmi pause                 ;bit 7 set is the pause flag
    bne update
pause:
    jmp update_sound

;----------------------------------------------------------------------------------------------------------------------
update:

.ifdef ::FAMISTUDIO_USE_FAMITRACKER_TEMPO
    clc                        ;update frame counter that considers speed, tempo, and PAL/NTSC
    lda famistudio_tempo_acc_lo
    adc famistudio_tempo_step_lo
    sta famistudio_tempo_acc_lo
    lda famistudio_tempo_acc_hi
    adc famistudio_tempo_step_hi
    cmp famistudio_song_speed
    bcs update_row            ;overflow, row update is needed
    sta famistudio_tempo_acc_hi         ;no row update, skip to the envelopes update
    jmp update_envelopes

update_row:
    sec
    sbc famistudio_song_speed
    sta famistudio_tempo_acc_hi

.else ; FamiStudio tempo

    dec famistudio_tempo_env_counter
    beq advance_tempo_envelope
    lda #1
    jmp store_frame_count

advance_tempo_envelope:
    lda famistudio_tempo_env_ptr_lo
    sta famistudio_ptr0+0
    lda famistudio_tempo_env_ptr_hi
    sta famistudio_ptr0+1

    inc famistudio_tempo_env_idx
    ldy famistudio_tempo_env_idx
    lda (famistudio_ptr0),y
    bpl store_counter

tempo_envelope_end:
    ldy #1
    sty famistudio_tempo_env_idx
    lda (famistudio_ptr0),y

store_counter:
    sta famistudio_tempo_env_counter
    lda famistudio_tempo_frame_num
    bne store_frame_count
    jmp skip_frame

store_frame_count:
    sta famistudio_tempo_frame_cnt

update_row:

.endif

    ; TODO: Turn most of these in loops, no reasons to be macros.
    famistudio_update_row 0, FAMISTUDIO_CH0_ENVS
    famistudio_update_row 1, FAMISTUDIO_CH1_ENVS
    famistudio_update_row 2, FAMISTUDIO_CH2_ENVS
    famistudio_update_row 3, FAMISTUDIO_CH3_ENVS
    famistudio_update_row_dpcm 4

.ifdef ::FAMISTUDIO_EXP_VRC6
    famistudio_update_row 5, FAMISTUDIO_CH5_ENVS
    famistudio_update_row 6, FAMISTUDIO_CH6_ENVS
    famistudio_update_row 7, FAMISTUDIO_CH7_ENVS
.endif

.ifdef ::FAMISTUDIO_EXP_VRC7
    famistudio_update_row  5, FAMISTUDIO_CH5_ENVS
    famistudio_update_row  6, FAMISTUDIO_CH6_ENVS
    famistudio_update_row  7, FAMISTUDIO_CH7_ENVS
    famistudio_update_row  8, FAMISTUDIO_CH8_ENVS
    famistudio_update_row  9, FAMISTUDIO_CH9_ENVS
    famistudio_update_row 10, FAMISTUDIO_CH10_ENVS
.endif

.ifdef ::FAMISTUDIO_EXP_FDS
    famistudio_update_row 5, FAMISTUDIO_CH5_ENVS
.endif

.ifdef ::FAMISTUDIO_EXP_MMC5
    famistudio_update_row 5, FAMISTUDIO_CH5_ENVS
    famistudio_update_row 6, FAMISTUDIO_CH6_ENVS
.endif

.ifdef ::FAMISTUDIO_EXP_S5B
    famistudio_update_row 5, FAMISTUDIO_CH5_ENVS
    famistudio_update_row 6, FAMISTUDIO_CH6_ENVS
    famistudio_update_row 7, FAMISTUDIO_CH7_ENVS
.endif

.ifdef ::FAMISTUDIO_EXP_N163
    .if ::FAMISTUDIO_EXP_N163_CHN_CNT >= 1
        famistudio_update_row  5, FAMISTUDIO_CH5_ENVS
    .endif
    .if ::FAMISTUDIO_EXP_N163_CHN_CNT >= 2
        famistudio_update_row  6, FAMISTUDIO_CH6_ENVS
    .endif
    .if ::FAMISTUDIO_EXP_N163_CHN_CNT >= 3
        famistudio_update_row  7, FAMISTUDIO_CH7_ENVS
    .endif
    .if ::FAMISTUDIO_EXP_N163_CHN_CNT >= 4
        famistudio_update_row  8, FAMISTUDIO_CH8_ENVS
    .endif
    .if ::FAMISTUDIO_EXP_N163_CHN_CNT >= 5
        famistudio_update_row  9, FAMISTUDIO_CH9_ENVS
    .endif
    .if ::FAMISTUDIO_EXP_N163_CHN_CNT >= 6
        famistudio_update_row 10, FAMISTUDIO_CH10_ENVS
    .endif
    .if ::FAMISTUDIO_EXP_N163_CHN_CNT >= 7
        famistudio_update_row 11, FAMISTUDIO_CH11_ENVS
    .endif
    .if ::FAMISTUDIO_EXP_N163_CHN_CNT >= 8
        famistudio_update_row 12, FAMISTUDIO_CH12_ENVS
    .endif
.endif

;----------------------------------------------------------------------------------------------------------------------
update_envelopes:
    ldx #0    ;process 11 envelopes

env_process:
    lda famistudio_env_repeat,x
    beq env_read  
    dec famistudio_env_repeat,x
    bne env_next

env_read:
    lda famistudio_env_addr_lo,x         ;load envelope data address into temp
    sta famistudio_ptr0_lo
    lda famistudio_env_addr_hi,x
    sta famistudio_ptr0_hi
    ldy famistudio_env_ptr,x           ;load envelope pointer

env_read_value:
    lda (famistudio_ptr0),y       ;read a byte of the envelope data
    bpl env_special           ;values below 128 used as a special code, loop or repeat
    clc                        ;values above 128 are output value+192 (output values are signed -63..64)
    adc #256-192
    sta famistudio_env_value,x         ;store the output value
    iny                        ;advance the pointer
    bne env_next_store_ptr    ;bra

env_special:
    bne env_set_repeat        ;zero is the loop point, non-zero values used for the repeat counter
    iny                        ;advance the pointer
    lda (famistudio_ptr0),y       ;read loop position
    tay                        ;use loop position
    jmp env_read_value        ;read next byte of the envelope

env_set_repeat:
    iny
    sta famistudio_env_repeat,x        ;store the repeat counter value

env_next_store_ptr:
    tya                        ;store the envelope pointer
    sta famistudio_env_ptr,x

env_next:
    inx                        ;next envelope

    cpx #FAMISTUDIO_NUM_ENVELOPES
    bne env_process

;----------------------------------------------------------------------------------------------------------------------
update_pitch_envelopes:
    ldx #0
    jmp pitch_env_process

pitch_relate_update_with_last_value:
    lda famistudio_pitch_env_repeat,x
    sec 
    sbc #1
    sta famistudio_pitch_env_repeat,x
    and #$7f 
    beq pitch_env_read
    lda famistudio_pitch_env_addr_lo,x 
    sta famistudio_ptr0_lo
    lda famistudio_pitch_env_addr_hi,x
    sta famistudio_ptr0_hi
    ldy famistudio_pitch_env_ptr,x
    dey    
    dey
    lda (famistudio_ptr0),y
    clc  
    adc #256-192
    sta famistudio_r1
    clc
    adc famistudio_pitch_env_value_lo,x
    sta famistudio_pitch_env_value_lo,x
    lda famistudio_r1
     bpl pitch_relative_last_pos  
    lda #$ff
pitch_relative_last_pos:
    adc famistudio_pitch_env_value_hi,x
    sta famistudio_pitch_env_value_hi,x
    jmp pitch_env_next

pitch_env_process:
    lda famistudio_pitch_env_repeat,x
    cmp #$81
    bcs pitch_relate_update_with_last_value
    and #$7f
    beq pitch_env_read
    dec famistudio_pitch_env_repeat,x
    bne pitch_env_next

pitch_env_read:
    lda famistudio_pitch_env_addr_lo,x 
    sta famistudio_ptr0_lo
    lda famistudio_pitch_env_addr_hi,x
    sta famistudio_ptr0_hi
    ldy #0
    lda (famistudio_ptr0),y
    sta famistudio_r0 ; going to be 0 for absolute envelope, 0x80 for relative.
    ldy famistudio_pitch_env_ptr,x

pitch_env_read_value:
    lda (famistudio_ptr0),y
    bpl pitch_env_special 
    clc  
    adc #256-192
    bit famistudio_r0
    bmi pitch_relative

pitch_absolute:
    sta famistudio_pitch_env_value_lo,x
    ora #0
    bmi pitch_absolute_neg  
    lda #0
    jmp pitch_absolute_set_value_hi
pitch_absolute_neg:
    lda #$ff
pitch_absolute_set_value_hi:
    sta famistudio_pitch_env_value_hi,x
    iny 
    jmp pitch_env_next_store_ptr

pitch_relative:
    sta famistudio_r1
    clc
    adc famistudio_pitch_env_value_lo,x
    sta famistudio_pitch_env_value_lo,x
    lda famistudio_r1
    and #$80
    bpl pitch_relative_pos  
    lda #$ff
pitch_relative_pos:
    adc famistudio_pitch_env_value_hi,x
    sta famistudio_pitch_env_value_hi,x
    iny 
    jmp pitch_env_next_store_ptr

pitch_env_special:
    bne pitch_env_set_repeat
    iny 
    lda (famistudio_ptr0),y 
    tay
    jmp pitch_env_read_value 

pitch_env_set_repeat:
    iny
    ora famistudio_r0 ; this is going to set the relative flag in the hi-bit.
    sta famistudio_pitch_env_repeat,x

pitch_env_next_store_ptr:
    tya 
    sta famistudio_pitch_env_ptr,x

pitch_env_next:
    inx 

    cpx #FAMISTUDIO_NUM_PITCH_ENVELOPES
    bne pitch_env_process

.ifdef ::FAMISTUDIO_USE_SLIDE_NOTES
;----------------------------------------------------------------------------------------------------------------------
update_slides:
    ldx #0    ;process 3 slides

slide_process:
    lda famistudio_slide_step,x        ; zero repeat means no active slide.
    beq slide_next
    clc                        ; add step to slide pitch (16bit + 8bit signed).
    lda famistudio_slide_step,x
    adc famistudio_slide_pitch_lo,x
    sta famistudio_slide_pitch_lo,x
    lda famistudio_slide_step,x
    and #$80
    beq positive_slide

negative_slide:
    lda #$ff
    adc famistudio_slide_pitch_hi,x
    sta famistudio_slide_pitch_hi,x
    bpl slide_next
    jmp clear_slide

positive_slide:
    adc famistudio_slide_pitch_hi,x
    sta famistudio_slide_pitch_hi,x
    bmi slide_next

clear_slide:
    lda #0
    sta famistudio_slide_step,x

slide_next:
    inx                        ;next slide
    cpx #FAMISTUDIO_NUM_PITCH_ENVELOPES
    bne slide_process
.endif

;----------------------------------------------------------------------------------------------------------------------
update_sound:

    famistudio_update_channel_sound 0, FAMISTUDIO_CH0_ENVS, 0, famistudio_pulse1_prev, , , FT_MR_PULSE1_H, FT_MR_PULSE1_L, FT_MR_PULSE1_V, FAMISTUDIO_APU_PL1_SWEEP
    famistudio_update_channel_sound 1, FAMISTUDIO_CH1_ENVS, 1, famistudio_pulse2_prev, , , FT_MR_PULSE2_H, FT_MR_PULSE2_L, FT_MR_PULSE2_V, FAMISTUDIO_APU_PL2_SWEEP
    famistudio_update_channel_sound 2, FAMISTUDIO_CH2_ENVS, 2, , #$80, , FT_MR_TRI_H, FT_MR_TRI_L, FT_MR_TRI_V
    famistudio_update_channel_sound 3, FAMISTUDIO_CH3_ENVS,  , , #$f0, , FT_MR_NOISE_F, , FT_MR_NOISE_V

.ifdef ::FAMISTUDIO_EXP_VRC6
    famistudio_update_channel_sound 5, FAMISTUDIO_CH5_ENVS, 3, , , #$80, FAMISTUDIO_VRC6_PL1_HI, FAMISTUDIO_VRC6_PL1_LO, FAMISTUDIO_VRC6_PL1_VOL
    famistudio_update_channel_sound 6, FAMISTUDIO_CH6_ENVS, 4, , , #$80, FAMISTUDIO_VRC6_PL2_HI, FAMISTUDIO_VRC6_PL2_LO, FAMISTUDIO_VRC6_PL2_VOL
    famistudio_update_channel_sound 7, FAMISTUDIO_CH7_ENVS, 5, , , #$80, FAMISTUDIO_VRC6_SAW_HI, FAMISTUDIO_VRC6_SAW_LO, FAMISTUDIO_VRC6_SAW_VOL
.endif

.ifdef ::FAMISTUDIO_EXP_MMC5
    famistudio_update_channel_sound 5, FAMISTUDIO_CH5_ENVS, 3, famistudio_mmc5_pulse1_prev, , , FAMISTUDIO_MMC5_PL1_HI, FAMISTUDIO_MMC5_PL1_LO, FAMISTUDIO_MMC5_PL1_VOL
    famistudio_update_channel_sound 6, FAMISTUDIO_CH6_ENVS, 4, famistudio_mmc5_pulse2_prev, , , FAMISTUDIO_MMC5_PL2_HI, FAMISTUDIO_MMC5_PL2_LO, FAMISTUDIO_MMC5_PL2_VOL
.endif

.ifdef ::FAMISTUDIO_EXP_FDS
    jsr famistudio_update_fds_channel_sound
.endif

.ifdef ::FAMISTUDIO_EXP_VRC7
    ldy #0
    vrc7_channel_loop:
        jsr famistudio_update_vrc7_channel_sound
        iny
        cpy #6
        bne vrc7_channel_loop
.endif

.ifdef ::FAMISTUDIO_EXP_N163
    ldy #0
    n163_channel_loop:
        jsr famistudio_update_n163_channel_sound
        iny
        cpy #FAMISTUDIO_EXP_N163_CHN_CNT
        bne n163_channel_loop
.endif

.ifdef ::FAMISTUDIO_EXP_S5B
    ldy #0
    s5b_channel_loop:
        jsr famistudio_update_s5b_channel_sound
        iny
        cpy #3
        bne s5b_channel_loop
.endif

.ifndef ::FAMISTUDIO_USE_FAMITRACKER_TEMPO
    ; See if we need to run a double frame (playing NTSC song on PAL)
    dec famistudio_tempo_frame_cnt
    beq skip_frame
    jmp update_row
.endif

skip_frame:

;----------------------------------------------------------------------------------------------------------------------
.ifdef ::FAMISTUDIO_CFG_SFX_SUPPORT

    ;process all sound effect streams

    .if FAMISTUDIO_CFG_SFX_STREAMS>0
    ldx #FAMISTUDIO_SFX_CH0
    jsr famistudio_sfx_update
    .endif
    .if FAMISTUDIO_CFG_SFX_STREAMS>1
    ldx #FAMISTUDIO_SFX_CH1
    jsr _FT2SfxUpdat
    .endif
    .if FAMISTUDIO_CFG_SFX_STREAMS>2
    ldx #FAMISTUDIO_SFX_CH2
    jsr famistudio_sfx_update
    .endif
    .if FAMISTUDIO_CFG_SFX_STREAMS>3
    ldx #FAMISTUDIO_SFX_CH3
    jsr famistudio_sfx_update
    .endif

    ;send data from the output buffer to the APU

    lda famistudio_output_buf      ;pulse 1 volume
    sta FAMISTUDIO_APU_PL1_VOL
    lda famistudio_output_buf+1    ;pulse 1 period LSB
    sta FAMISTUDIO_APU_PL1_LO
    lda famistudio_output_buf+2    ;pulse 1 period MSB, only applied when changed
    cmp famistudio_pulse1_prev
    beq no_pulse1_upd
    sta famistudio_pulse1_prev
    sta FAMISTUDIO_APU_PL1_HI
no_pulse1_upd:

    lda famistudio_output_buf+3    ;pulse 2 volume
    sta FAMISTUDIO_APU_PL2_VOL
    lda famistudio_output_buf+4    ;pulse 2 period LSB
    sta FAMISTUDIO_APU_PL2_LO
    lda famistudio_output_buf+5    ;pulse 2 period MSB, only applied when changed
    cmp famistudio_pulse2_prev
    beq no_pulse2_upd
    sta famistudio_pulse2_prev
    sta FAMISTUDIO_APU_PL2_HI
no_pulse2_upd:

    lda famistudio_output_buf+6    ;triangle volume (plays or not)
    sta FAMISTUDIO_APU_TRI_LINEAR
    lda famistudio_output_buf+7    ;triangle period LSB
    sta FAMISTUDIO_APU_TRI_LO
    lda famistudio_output_buf+8    ;triangle period MSB
    sta FAMISTUDIO_APU_TRI_HI

    lda famistudio_output_buf+9    ;noise volume
    sta FAMISTUDIO_APU_NOISE_VOL
    lda famistudio_output_buf+10   ;noise period
    sta FAMISTUDIO_APU_NOISE_LO

.endif

    .if(::FAMISTUDIO_CFG_THREAD)
    pla
    sta famistudio_ptr0_hi
    pla
    sta famistudio_ptr0_lo
    .endif

    rts

.endproc

;internal routine, sets up envelopes of a channel according to current instrument
;in X envelope group offset, y = channel idx, A instrument number

.proc famistudio_set_instrument

    ptr = famistudio_ptr0
    wave_ptr = famistudio_ptr1
    chan_idx = famistudio_r1
    tmp_x = famistudio_r2

    sty chan_idx
    asl                        ;instrument number is pre multiplied by 4
    tay
    lda famistudio_instrument_hi
    adc #0                     ;use carry to extend range for 64 instruments
    sta ptr+1
    lda famistudio_instrument_lo
    sta ptr+0

    ; Volume envelope
    lda (ptr),y
    sta famistudio_env_addr_lo,x
    iny
    lda (famistudio_ptr0),y
    iny
    sta famistudio_env_addr_hi,x
    inx

    ; Arpeggio envelope
.ifdef ::FAMISTUDIO_USE_ARPEGGIO
    stx tmp_x
    ldx chan_idx
    lda famistudio_chn_env_override,x ; Check if its overriden by arpeggio.
    lsr
    ldx tmp_x
    bcc read_arpeggio_ptr 
    iny ; instrument arpeggio is overriden by arpeggio, dont touch!
    jmp init_envelopes
.endif

read_arpeggio_ptr:    
    lda (famistudio_ptr0),y
    sta famistudio_env_addr_lo,x
    iny
    lda (famistudio_ptr0),y
    sta famistudio_env_addr_hi,x

init_envelopes:
    ; Initialize volume + arpeggio envelopes.
    lda #1
    sta famistudio_env_ptr-1,x         ;reset env1 pointer (env1 is volume and volume can have releases)
    lda #0
    sta famistudio_env_repeat-1,x      ;reset env1 repeat counter
    sta famistudio_env_repeat,x        ;reset env2 repeat counter
    sta famistudio_env_ptr,x           ;reset env2 pointer

    ; Duty cycle envelope
    lda chan_idx
    cmp #2                     ;triangle has no duty.
.if !.defined(::FAMISTUDIO_EXP_S5B)
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
        lda (famistudio_ptr0),y
        sta famistudio_env_addr_lo,x
        iny
        lda (famistudio_ptr0),y
        sta famistudio_env_addr_hi,x
        lda #0
        sta famistudio_env_repeat,x        ;reset env3 repeat counter
        sta famistudio_env_ptr,x           ;reset env3 pointer
    pitch_env:
    ; Pitch envelopes.
    ldx chan_idx
.ifdef ::FAMISTUDIO_USE_VIBRATO 
    lda famistudio_chn_env_override,x ; instrument pitch is overriden by vibrato, dont touch!
    bmi no_pitch    
.endif    
    lda famistudio_channel_to_pitch_env, x
    bmi no_pitch
    tax
    lda #1
    sta famistudio_pitch_env_ptr,x     ;reset env3 pointer (pitch envelope have relative/absolute flag in the first byte)
    lda #0
    sta famistudio_pitch_env_repeat,x  ;reset env3 repeat counter
    sta famistudio_pitch_env_value_lo,x
    sta famistudio_pitch_env_value_hi,x
    iny
    lda (famistudio_ptr0),y       ;instrument pointer LSB
    sta famistudio_pitch_env_addr_lo,x
    iny
    lda (famistudio_ptr0),y       ;instrument pointer MSB
    sta famistudio_pitch_env_addr_hi,x
    no_pitch:
    rts

.endproc

.if .defined(FAMISTUDIO_EXP_FDS) || .defined(FAMISTUDIO_EXP_N163) || .defined(FAMISTUDIO_EXP_VRC7) 
.macro famistudio_set_exp_instrument_base

    chan_idx = famistudio_r1
    tmp_x = famistudio_r2

    sty chan_idx
    asl                        ;instrument number is pre multiplied by 4
    asl
    tay
    lda famistudio_exp_instrument_hi
    adc #0                     ;use carry to extend range for 64 instruments
    sta ptr+1
    lda famistudio_exp_instrument_lo
    sta ptr+0

    ; Volume envelope
    lda (ptr),y
    sta famistudio_env_addr_lo,x
    iny
    lda (ptr),y
    iny
    sta famistudio_env_addr_hi,x
    inx

    ; Arpeggio envelope
.ifdef ::FAMISTUDIO_USE_ARPEGGIO
    stx tmp_x
    ldx chan_idx
    lda famistudio_chn_env_override,x ; Check if its overriden by arpeggio.
    lsr
    ldx tmp_x
    bcc read_arpeggio_ptr 
    iny ; instrument arpeggio is overriden by arpeggio, dont touch!
    jmp init_envelopes
.endif

read_arpeggio_ptr:    
    lda (ptr),y
    sta famistudio_env_addr_lo,x
    iny
    lda (ptr),y
    sta famistudio_env_addr_hi,x
    jmp init_envelopes

init_envelopes:
    iny
    ; Initialize volume + arpeggio envelopes.
    lda #1
    sta famistudio_env_ptr-1,x         ;reset env1 pointer (env1 is volume and volume can have releases)
    lda #0
    sta famistudio_env_repeat-1,x      ;reset env1 repeat counter
    sta famistudio_env_repeat,x        ;reset env2 repeat counter
    sta famistudio_env_ptr,x           ;reset env2 pointer

    ; Pitch envelopes.
    ldx chan_idx
.ifdef ::FAMISTUDIO_USE_VIBRATO
    lda famistudio_chn_env_override,x ; instrument pitch is overriden by vibrato, dont touch!
    bpl pitch_env
    iny
    iny
    bne pitch_overriden
.endif

    pitch_env:
    dex
    dex                        ; Noise + DPCM dont have pitch envelopes             
    lda #1
    sta famistudio_pitch_env_ptr,x     ;reset env3 pointer (pitch envelope have relative/absolute flag in the first byte)
    lda #0
    sta famistudio_pitch_env_repeat,x  ;reset env3 repeat counter
    sta famistudio_pitch_env_value_lo,x
    sta famistudio_pitch_env_value_hi,x
    lda (ptr),y       ;instrument pointer LSB
    sta famistudio_pitch_env_addr_lo,x
    iny
    lda (ptr),y       ;instrument pointer MSB
    sta famistudio_pitch_env_addr_hi,x
    iny

    pitch_overriden:

.endmacro
.endif

.ifdef FAMISTUDIO_EXP_VRC7
.proc famistudio_set_vrc7_instrument

    ptr = famistudio_ptr0

    famistudio_set_exp_instrument_base

    lda chan_idx
    sec
    sbc #5
    tax

    lda famistudio_chn_inst_changed,x
    beq done

    lda (ptr),y
    sta famistudio_chn_vrc7_patch, x
    bne done

    read_custom_patch:
    ldx #0
    iny
    read_patch_loop:
        stx FAMISTUDIO_VRC7_REG_SEL
        jsr famistudio_vrc7_wait_reg_select
        lda (ptr),y
        iny
        sta FAMISTUDIO_VRC7_REG_WRITE
        jsr famistudio_vrc7_wait_reg_write
        inx
        cpx #8
        bne read_patch_loop

    done:
    rts

.endproc
.endif

.ifdef FAMISTUDIO_EXP_FDS
.proc famistudio_set_fds_instrument

    ptr        = famistudio_ptr0
    wave_ptr   = famistudio_ptr1
    master_vol = famistudio_r1
    tmp_y      = famistudio_r2

    famistudio_set_exp_instrument_base

    lda #0
    sta FAMISTUDIO_FDS_SWEEP_BIAS

    lda famistudio_chn_inst_changed
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
        sta FAMISTUDIO_FDS_VOL ; Enable wave RAM write

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
            sta FAMISTUDIO_FDS_WAV_START,y
            iny
            cpy #64
            bne wave_loop

        lda #$80
        sta FAMISTUDIO_FDS_MOD_HI ; Need to disable modulation before writing.
        lda master_vol
        sta FAMISTUDIO_FDS_VOL ; Disable RAM write.
        lda #0
        sta FAMISTUDIO_FDS_SWEEP_BIAS

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
            sta FAMISTUDIO_FDS_MOD_TABLE
            iny
            cpy #32
            bne mod_loop

        lda #0
        sta famistudio_chn_inst_changed

        ldy tmp_y

    load_mod_param:

        check_mod_speed:
            bit famistudio_fds_override_flags
            bmi mod_speed_overriden

            load_mod_speed:
                lda (ptr),y
                sta famistudio_fds_mod_speed+0
                iny
                lda (ptr),y
                sta famistudio_fds_mod_speed+1
                jmp check_mod_depth

            mod_speed_overriden:
                iny

        check_mod_depth:
            iny
            bit famistudio_fds_override_flags
            bvs mod_depth_overriden

            load_mod_depth:
                lda (ptr),y
                sta famistudio_fds_mod_depth

            mod_depth_overriden:
                iny
                lda (ptr),y
                sta famistudio_fds_mod_delay

    rts

.endproc
.endif

.ifdef FAMISTUDIO_EXP_N163

_FT2N163WaveTable:
    .byte FAMISTUDIO_N163_REG_WAVE - $00
    .byte FAMISTUDIO_N163_REG_WAVE - $08
    .byte FAMISTUDIO_N163_REG_WAVE - $10
    .byte FAMISTUDIO_N163_REG_WAVE - $18
    .byte FAMISTUDIO_N163_REG_WAVE - $20
    .byte FAMISTUDIO_N163_REG_WAVE - $28
    .byte FAMISTUDIO_N163_REG_WAVE - $30
    .byte FAMISTUDIO_N163_REG_WAVE - $38

.proc famistudio_set_n163_instrument

    ptr      = famistudio_ptr0
    wave_ptr = famistudio_ptr1
    wave_len = famistudio_r0
    wave_pos = famistudio_r1
    tmp_y    = famistudio_r2

    famistudio_set_exp_instrument_base

    ; Wave position
    lda chan_idx
    sec
    sbc #5
    tax

    lda famistudio_chn_inst_changed,x
    beq done

    lda _FT2N163WaveTable, x
    sta FAMISTUDIO_N163_ADDR
    lda (ptr),y
    sta wave_pos
    sta FAMISTUDIO_N163_DATA
    iny

    ; Wave length
    lda (ptr),y
    sta wave_len
    lda #$00 ; 256 - wave length
    sec
    sbc wave_len
    sec
    sbc wave_len
    sta famistudio_chn_n163_wave_len, x
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
    sta FAMISTUDIO_N163_ADDR
    ldy #0
    wave_loop:
        lda (wave_ptr),y
        sta FAMISTUDIO_N163_DATA
        iny
        cpy wave_len
        bne wave_loop

    done:
    rts

.endproc
.endif

; increments 16-bit.
.macro famistudio_inc_16 addr
    .local @ok
    inc addr+0
    bne @ok
    inc addr+1
@ok:
.endmacro

; add 8-bit to a 16-bit (unsigned).
.macro famistudio_add_16_8 addr, val
    .local @ok
    clc
    lda val
    adc addr+0
    sta addr+0
    bcc @ok
    inc addr+1
@ok:
.endmacro

;internal routine, parses channel note data
.proc famistudio_channel_update

    no_attack_flag = famistudio_r2
    slide_delta_lo = famistudio_ptr2_hi

.if .defined(::FAMISTUDIO_EXP_VRC6)
    exp_note_start = 7
    exp_note_table_lsb = famistudio_saw_note_table_lsb
    exp_note_table_msb = famistudio_saw_note_table_msb
.elseif .defined(::FAMISTUDIO_EXP_VRC7)
    exp_note_start = 5
    exp_note_table_lsb = famistudio_vrc7_note_table_lsb
    exp_note_table_msb = famistudio_vrc7_note_table_msb
.elseif .defined(::FAMISTUDIO_EXP_N163)
    exp_note_start = 5
    exp_note_table_lsb = famistudio_n163_note_table_lsb
    exp_note_table_msb = famistudio_n163_note_table_msb
.elseif .defined(::FAMISTUDIO_EXP_FDS)
    exp_note_start = 5
    exp_note_table_lsb = famistudio_fds_note_table_lsb
    exp_note_table_msb = famistudio_fds_note_table_msb
.endif

    lda famistudio_chn_repeat,x        ;check repeat counter
    beq no_repeat
    dec famistudio_chn_repeat,x        ;decrease repeat counter
    clc                        ;no new note
    rts

no_repeat:
    lda #0
    sta no_attack_flag
    lda famistudio_chn_ptr_lo,x         ;load channel pointer into temp
    sta famistudio_ptr0_lo
    lda famistudio_chn_ptr_hi,x
    sta famistudio_ptr0_hi
    ldy #0

read_byte:
    lda (famistudio_ptr0),y       ;read byte of the channel
    famistudio_inc_16 famistudio_ptr0

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

.ifdef ::FAMISTUDIO_USE_VOLUME_TRACK
volume_track:    
    and #$0f
    asl ; a LUT would be nice, but x/y are both in-use here.
    asl
    asl
    asl
    sta famistudio_chn_volume_track,x
    jmp read_byte
.else
    brk ; If you hit this, this mean you use the volume track in your songs, but did not enable the "FAMISTUDIO_USE_VOLUME_TRACK" feature.
.endif

special_code_6x:
    stx famistudio_r0
    and #$0f
    tax
    lda famistudio_special_code_jmp_lo-1,x
    sta famistudio_ptr1+0
    lda famistudio_special_code_jmp_hi-1,x
    sta famistudio_ptr1+1
    ldx famistudio_r0
    jmp (famistudio_ptr1)

.ifdef ::FAMISTUDIO_EXP_FDS

fds_mod_depth:    
    lda (famistudio_ptr0),y
    famistudio_inc_16 famistudio_ptr0
    sta famistudio_fds_mod_depth
    lda #$40
    ora famistudio_fds_override_flags
    sta famistudio_fds_override_flags
    jmp read_byte

fds_mod_speed:
    lda (famistudio_ptr0),y
    sta famistudio_fds_mod_speed+0
    iny
    lda (famistudio_ptr0),y
    sta famistudio_fds_mod_speed+1
    famistudio_add_16_8 famistudio_ptr0, #2
    lda #$80
    ora famistudio_fds_override_flags
    sta famistudio_fds_override_flags
    dey
    jmp read_byte

.endif

.ifdef ::FAMISTUDIO_USE_PITCH_TRACK
fine_pitch:
    stx famistudio_r0
    lda famistudio_channel_to_pitch_env,x
    tax
    lda (famistudio_ptr0),y
    famistudio_inc_16 famistudio_ptr0
    sta famistudio_pitch_env_fine_value,x
    ldx famistudio_r0
    jmp read_byte 
.endif

.ifdef ::FAMISTUDIO_USE_VIBRATO
clear_pitch_override_flag:
    lda #$7f
    and famistudio_chn_env_override,x
    sta famistudio_chn_env_override,x
    jmp read_byte 

override_pitch_envelope:
    lda #$80
    ora famistudio_chn_env_override,x
    sta famistudio_chn_env_override,x
    stx famistudio_r0
    lda famistudio_channel_to_pitch_env,x
    tax
    lda (famistudio_ptr0),y
    sta famistudio_pitch_env_addr_lo,x
    iny
    lda (famistudio_ptr0),y
    sta famistudio_pitch_env_addr_hi,x
    lda #0
    tay
    sta famistudio_pitch_env_repeat,x
    lda #1
    sta famistudio_pitch_env_ptr,x
    ldx famistudio_r0
    famistudio_add_16_8 famistudio_ptr0, #2
    jmp read_byte 
.endif

.ifdef ::FAMISTUDIO_USE_ARPEGGIO
clear_arpeggio_override_flag:
    lda #$fe
    and famistudio_chn_env_override,x
    sta famistudio_chn_env_override,x
    jmp read_byte

override_arpeggio_envelope:
    lda #$01
    ora famistudio_chn_env_override,x
    sta famistudio_chn_env_override,x
    stx famistudio_r0    
    lda famistudio_channel_to_arpeggio_env,x
    tax    
    lda (famistudio_ptr0),y
    sta famistudio_env_addr_lo,x
    iny
    lda (famistudio_ptr0),y
    sta famistudio_env_addr_hi,x
    lda #0
    tay
    sta famistudio_env_repeat,x ; Reset the envelope since this might be a no-attack note.
    sta famistudio_env_value,x
    sta famistudio_env_ptr,x
    ldx famistudio_r0
    famistudio_add_16_8 famistudio_ptr0, #2
    jmp read_byte

reset_arpeggio:
    stx famistudio_r0    
    lda famistudio_channel_to_arpeggio_env,x
    tax
    lda #0
    sta famistudio_env_repeat,x
    sta famistudio_env_value,x
    sta famistudio_env_ptr,x
    ldx famistudio_r0
    jmp read_byte
.endif

disable_attack:
    lda #1
    sta no_attack_flag    
    jmp read_byte 

.ifdef ::FAMISTUDIO_USE_SLIDE_NOTES
slide:
    stx famistudio_r0
    lda famistudio_channel_to_slide,x
    tax
    lda (famistudio_ptr0),y       ; read slide step size
    iny
    sta famistudio_slide_step,x
    lda (famistudio_ptr0),y       ; read slide note from
.ifdef ::FAMISTUDIO_DUAL_SUPPORT
    clc
    adc famistudio_pal_adjust
.endif
    sta famistudio_r1
    iny
    lda (famistudio_ptr0),y       ; read slide note to
    ldy famistudio_r1           ; start note
.ifdef ::FAMISTUDIO_DUAL_SUPPORT
    adc famistudio_pal_adjust
.endif
    stx famistudio_r1           ; store slide index.    
    tax
.ifdef exp_note_start
    lda famistudio_r0
    cmp #exp_note_start
    bcs note_table_expansion
.endif
    sec                        ; subtract the pitch of both notes.
    lda famistudio_note_table_lsb,y
    sbc famistudio_note_table_lsb,x
    sta slide_delta_lo
    lda famistudio_note_table_msb,y
    sbc famistudio_note_table_msb,x
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
    ldx famistudio_r1           ; slide index.
    sta famistudio_slide_pitch_hi,x
    .if .defined(::FAMISTUDIO_EXP_N163) || .defined(::FAMISTUDIO_EXP_VRC7)
        cpx #3 ; slide #3 is the first of expansion slides.
        bcs positive_shift
    .endif
    negative_shift:
        lda slide_delta_lo
        asl                        ; shift-left, we have 1 bit of fractional slide.
        sta famistudio_slide_pitch_lo,x
        rol famistudio_slide_pitch_hi,x     ; shift-left, we have 1 bit of fractional slide.
    .if .defined(::FAMISTUDIO_EXP_N163) || .defined(::FAMISTUDIO_EXP_VRC7)
        jmp shift_done
    positive_shift:
        lda slide_delta_lo
        sta famistudio_slide_pitch_lo,x
        .if ::FAMISTUDIO_PITCH_SHIFT >= 1
            lda famistudio_slide_pitch_hi,x
            cmp #$80
            ror famistudio_slide_pitch_hi,x 
            ror famistudio_slide_pitch_lo,x
        .if ::FAMISTUDIO_PITCH_SHIFT >= 2
            lda famistudio_slide_pitch_hi,x
            cmp #$80
            ror famistudio_slide_pitch_hi,x 
            ror famistudio_slide_pitch_lo,x
        .if ::FAMISTUDIO_PITCH_SHIFT >= 3
            lda famistudio_slide_pitch_hi,x
            cmp #$80
            ror famistudio_slide_pitch_hi,x 
            ror famistudio_slide_pitch_lo,x
        .if ::FAMISTUDIO_PITCH_SHIFT >= 4
            lda famistudio_slide_pitch_hi,x
            cmp #$80
            ror famistudio_slide_pitch_hi,x 
            ror famistudio_slide_pitch_lo,x
        .if ::FAMISTUDIO_PITCH_SHIFT >= 5
            lda famistudio_slide_pitch_hi,x
            cmp #$80
            ror famistudio_slide_pitch_hi,x 
            ror famistudio_slide_pitch_lo,x
        .endif 
        .endif
        .endif
        .endif
        .endif
    shift_done:
    .endif
    ldx famistudio_r0
    ldy #2
    lda (famistudio_ptr0),y       ; re-read the target note (ugly...)
    sta famistudio_chn_note,x          ; store note code
    famistudio_add_16_8 famistudio_ptr0, #3

slide_done_pos:
    ldy #0
    jmp sec_and_done
.endif

regular_note:    
    sta famistudio_chn_note,x          ; store note code
.ifdef ::FAMISTUDIO_USE_SLIDE_NOTES
    ldy famistudio_channel_to_slide,x   ; clear any previous slide on new node.
    bmi sec_and_done
    lda #0
    sta famistudio_slide_step,y
.endif
sec_and_done:
    lda no_attack_flag
    bne no_attack
    lda famistudio_chn_note,x          ; dont trigger attack on stop notes.
    beq no_attack
.if .defined(::FAMISTUDIO_EXP_VRC7)
    cpx #5
    bcs vrc7_channel
    sec                        ;new note flag is set
    jmp done
    vrc7_channel:
        lda #1
        sta famistudio_chn_vrc7_trigger-5,x ; set trigger flag for VRC7
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
    sta famistudio_chn_instrument,x    ;store instrument number*4

.if .defined(::FAMISTUDIO_EXP_N163) || .defined(::FAMISTUDIO_EXP_VRC7) || .defined(::FAMISTUDIO_EXP_FDS)
    cpx #5
    bcc regular_channel
        lda #1
        sta famistudio_chn_inst_changed-5, x
    regular_channel:
.endif
    jmp read_byte 

set_speed:
.ifndef ::FAMISTUDIO_USE_FAMITRACKER_TEMPO
    lda (famistudio_ptr0),y
    sta famistudio_tempo_env_ptr_lo
    sta famistudio_ptr1+0
    iny
    lda (famistudio_ptr0),y
    sta famistudio_tempo_env_ptr_hi
    sta famistudio_ptr1+1
    ldy #0
    sty famistudio_tempo_env_idx
    lda (famistudio_ptr1),y
    sta famistudio_tempo_env_counter
    famistudio_add_16_8 famistudio_ptr0, #2
.else
    lda (famistudio_ptr0),y
    sta famistudio_song_speed
    famistudio_inc_16 famistudio_ptr0
.endif
    jmp read_byte 

set_loop:
    lda (famistudio_ptr0),y
    sta famistudio_r0
    iny
    lda (famistudio_ptr0),y
    sta famistudio_ptr0_hi
    lda famistudio_r0
    sta famistudio_ptr0_lo
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
    lda famistudio_ptr0_lo
    adc #3
    sta famistudio_chn_return_lo,x
    lda famistudio_ptr0_hi
    adc #0
    sta famistudio_chn_return_hi,x
    lda (famistudio_ptr0),y       ;read length of the reference (how many rows)
    sta famistudio_chn_ref_len,x
    iny
    lda (famistudio_ptr0),y       ;read 16-bit absolute address of the reference
    sta famistudio_r0          ;remember in temp
    iny
    lda (famistudio_ptr0),y
    sta famistudio_ptr0_hi
    lda famistudio_r0
    sta famistudio_ptr0_lo
    ldy #0
    jmp read_byte

release_note:

.ifdef ::FAMISTUDIO_EXP_VRC7
    cpx #5
    bcc apu_channel
    lda #$80
    sta famistudio_chn_vrc7_trigger-5,x ; set release flag for VRC7
    apu_channel:
.endif    

    stx famistudio_r0
    lda famistudio_channel_to_volume_env,x ; DPCM(5) will never have releases.
    tax

    lda famistudio_env_addr_lo,x         ;load envelope data address into temp
    sta famistudio_ptr1_lo
    lda famistudio_env_addr_hi,x
    sta famistudio_ptr2_hi    
    
    ldy #0
    lda (famistudio_ptr1),y       ;read first byte of the envelope data, this contains the release index.
    beq env_has_no_release

    sta famistudio_env_ptr,x
    lda #0
    sta famistudio_env_repeat,x        ;need to reset envelope repeat to force update.
    
env_has_no_release:
    ldx famistudio_r0
    clc
    jmp done

set_repeat:
    sta famistudio_chn_repeat,x        ;set up repeat counter, carry is clear, no new note

done:
    lda famistudio_chn_ref_len,x       ;check reference row counter
    beq no_ref                ;if it is zero, there is no reference
    dec famistudio_chn_ref_len,x       ;decrease row counter
    bne no_ref

    lda famistudio_chn_return_lo,x      ;end of a reference, return to previous pointer
    sta famistudio_chn_ptr_lo,x
    lda famistudio_chn_return_hi,x
    sta famistudio_chn_ptr_hi,x
    rts

no_ref:
    lda famistudio_ptr0_lo
    sta famistudio_chn_ptr_lo,x
    lda famistudio_ptr0_hi
    sta famistudio_chn_ptr_hi,x
    rts

.ifndef ::FAMISTUDIO_USE_PITCH_TRACK
fine_pitch:
.endif
.ifndef ::FAMISTUDIO_USE_VIBRATO
override_pitch_envelope:
clear_pitch_override_flag:
.endif
.ifndef ::FAMISTUDIO_USE_ARPEGGIO
override_arpeggio_envelope:
clear_arpeggio_override_flag:
reset_arpeggio:
.endif
.ifndef ::FAMISTUDIO_USE_SLIDE_NOTES
slide:
.endif
    ; If you hit this, this mean you either:
    ; - have fine pitches in your songs, but didnt enable "FAMISTUDIO_USE_PITCH_TRACK"
    ; - have vibrato effect in your songs, but didnt enable "FAMISTUDIO_USE_VIBRATO"
    ; - have arpeggiated chords in your songs, but didnt enable "FAMISTUDIO_USE_ARPEGGIO"
    ; - have slide notes in your songs, but didnt enable "FAMISTUDIO_USE_SLIDE_NOTES"
    brk 

.endproc

famistudio_special_code_jmp_lo:
    .byte <famistudio_channel_update::slide                        ; $61
    .byte <famistudio_channel_update::disable_attack               ; $62
    .byte <famistudio_channel_update::override_pitch_envelope      ; $63
    .byte <famistudio_channel_update::override_arpeggio_envelope   ; $64
    .byte <famistudio_channel_update::clear_pitch_override_flag    ; $65
    .byte <famistudio_channel_update::clear_arpeggio_override_flag ; $66
    .byte <famistudio_channel_update::reset_arpeggio               ; $67
    .byte <famistudio_channel_update::fine_pitch                   ; $68
.ifdef ::FAMISTUDIO_EXP_FDS        
    .byte <famistudio_channel_update::fds_mod_speed                ; $69
    .byte <famistudio_channel_update::fds_mod_depth                ; $6a
.endif        
famistudio_special_code_jmp_hi:
    .byte >famistudio_channel_update::slide                        ; $61
    .byte >famistudio_channel_update::disable_attack               ; $62
    .byte >famistudio_channel_update::override_pitch_envelope      ; $63
    .byte >famistudio_channel_update::override_arpeggio_envelope   ; $64
    .byte >famistudio_channel_update::clear_pitch_override_flag    ; $65
    .byte >famistudio_channel_update::clear_arpeggio_override_flag ; $66
    .byte >famistudio_channel_update::reset_arpeggio               ; $67
    .byte >famistudio_channel_update::fine_pitch                   ; $68
.ifdef ::FAMISTUDIO_EXP_FDS        
    .byte >famistudio_channel_update::fds_mod_speed                ; $69
    .byte >famistudio_channel_update::fds_mod_depth                ; $6a
.endif

;------------------------------------------------------------------------------
; stop DPCM sample if it plays
;------------------------------------------------------------------------------

.proc famistudio_sample_stop

    lda #%00001111
    sta FAMISTUDIO_APU_SND_CHN

    rts

.endproc

    
.ifdef ::FAMISTUDIO_CFG_DPCM_SUPPORT

;------------------------------------------------------------------------------
; play DPCM sample with higher priority, for sound effects
; in: A is number of a sample, 1..63
;------------------------------------------------------------------------------

.proc famistudio_sample_play_sfx

    ldx #1
    stx famistudio_dpcm_effect

sample_play:

    sta famistudio_r0               ;sample number*3, offset in the sample table
    asl a
    clc
    adc famistudio_r0
    
    adc famistudio_dpcm_list_lo
    sta famistudio_ptr0_lo
    lda #0
    adc famistudio_dpcm_list_hi
    sta famistudio_ptr0_hi

    lda #%00001111             ;stop DPCM
    sta FAMISTUDIO_APU_SND_CHN

    ldy #0
    lda (famistudio_ptr0),y       ;sample offset
    sta FAMISTUDIO_APU_DMC_START
    iny
    lda (famistudio_ptr0),y       ;sample length
    sta FAMISTUDIO_APU_DMC_LEN
    iny
    lda (famistudio_ptr0),y       ;pitch and loop
    sta FAMISTUDIO_APU_DMC_FREQ

    lda #32                    ;reset DAC counter
    sta FAMISTUDIO_APU_DMC_RAW
    lda #%00011111             ;start DMC
    sta FAMISTUDIO_APU_SND_CHN

    rts

.endproc 

;------------------------------------------------------------------------------
; play DPCM sample, used by music player, could be used externally
; in: A is number of a sample, 1..63
;------------------------------------------------------------------------------

.proc famistudio_sample_play_music           ;for music (low priority)

    ldx famistudio_dpcm_effect
    beq famistudio_sample_play_sfx::sample_play
    tax
    lda FAMISTUDIO_APU_SND_CHN
    and #16
    beq not_busy
    rts

not_busy:
    sta famistudio_dpcm_effect
    txa
    jmp famistudio_sample_play_sfx::sample_play

.endproc 

.endif

.ifdef ::FAMISTUDIO_CFG_SFX_SUPPORT

;------------------------------------------------------------------------------
; init sound effects player, set pointer to data
; in: X,Y is address of sound effects data
;------------------------------------------------------------------------------

.proc famistudio_sfx_init

    stx famistudio_ptr0_lo
    sty famistudio_ptr0_hi
    
    ldy #0
    
.ifdef ::FAMISTUDIO_DUAL_SUPPORT
    lda famistudio_pal_adjust          ;add 2 to the sound list pointer for PAL
    bne ntsc
    iny
    iny
ntsc:
.endif
    
    lda (famistudio_ptr0),y       ;read and store pointer to the effects list
    sta famistudio_sfx_addr_lo
    iny
    lda (famistudio_ptr0),y
    sta famistudio_sfx_addr_hi

    ldx #FAMISTUDIO_SFX_CH0            ;init all the streams

set_channels:
    jsr famistudio_sfx_clear_channel
    txa
    clc
    adc #FT_SFX_STRUCT_SIZE
    tax
    cpx #FT_SFX_STRUCT_SIZE*FAMISTUDIO_CFG_SFX_STREAMS
    bne set_channels

    rts

.endproc 

;internal routine, clears output buffer of a sound effect
;in: A is 0
;    X is offset of sound effect stream

.proc famistudio_sfx_clear_channel

    lda #0
    sta famistudio_sfx_ptr_hi,x         ;this stops the effect
    sta famistudio_sfx_repeat,x
    sta famistudio_sfx_offset,x
    sta famistudio_sfx_buffer+6,x         ;mute triangle
    lda #$30
    sta famistudio_sfx_buffer+0,x         ;mute pulse1
    sta famistudio_sfx_buffer+3,x         ;mute pulse2
    sta famistudio_sfx_buffer+9,x         ;mute noise

    rts

.endproc 

;------------------------------------------------------------------------------
; play sound effect
; in: A is a number of the sound effect 0..127
;     X is offset of sound effect channel, should be FAMISTUDIO_SFX_CH0..FAMISTUDIO_SFX_CH3
;------------------------------------------------------------------------------

.proc famistudio_sfx_play

    asl a                      ;get offset in the effects list
    tay

    jsr famistudio_sfx_clear_channel    ;stops the effect if it plays

    lda famistudio_sfx_addr_lo
    sta famistudio_ptr0_lo
    lda famistudio_sfx_addr_hi
    sta famistudio_ptr0_hi

    lda (famistudio_ptr0),y       ;read effect pointer from the table
    sta famistudio_sfx_ptr_lo,x         ;store it
    iny
    lda (famistudio_ptr0),y
    sta famistudio_sfx_ptr_hi,x         ;this write enables the effect

    rts

.endproc 

;internal routine, update one sound effect stream
;in: X is offset of sound effect stream

.proc famistudio_sfx_update

    lda famistudio_sfx_repeat,x        ;check if repeat counter is not zero
    beq no_repeat
    dec famistudio_sfx_repeat,x        ;decrement and return
    bne update_buf            ;just mix with output buffer

no_repeat:
    lda famistudio_sfx_ptr_hi,x         ;check if MSB of the pointer is not zero
    bne sfx_active
    rts                        ;return otherwise, no active effect

sfx_active:
    sta famistudio_ptr0_hi         ;load effect pointer into temp
    lda famistudio_sfx_ptr_lo,x
    sta famistudio_ptr0_lo
    ldy famistudio_sfx_offset,x
    clc

read_byte:
    lda (famistudio_ptr0),y       ;read byte of effect
    bmi get_data              ;if bit 7 is set, it is a register write
    beq eof
    iny
    sta famistudio_sfx_repeat,x        ;if bit 7 is reset, it is number of repeats
    tya
    sta famistudio_sfx_offset,x
    jmp update_buf

get_data:
    iny
    stx famistudio_r0          ;it is a register write
    adc famistudio_r0          ;get offset in the effect output buffer
    tax
    lda (famistudio_ptr0),y       ;read value
    iny
    sta famistudio_sfx_buffer-128,x       ;store into output buffer
    ldx famistudio_r0
    jmp read_byte             ;and read next byte

eof:
    sta famistudio_sfx_ptr_hi,x         ;mark channel as inactive

update_buf:

    lda famistudio_output_buf             ;compare effect output buffer with main output buffer
    and #$0f                   ;if volume of pulse 1 of effect is higher than that of the
    sta famistudio_r0          ;main buffer, overwrite the main buffer value with the new one
    lda famistudio_sfx_buffer+0,x
    and #$0f
    cmp famistudio_r0
    bcc no_pulse1
    lda famistudio_sfx_buffer+0,x
    sta famistudio_output_buf+0
    lda famistudio_sfx_buffer+1,x
    sta famistudio_output_buf+1
    lda famistudio_sfx_buffer+2,x
    sta famistudio_output_buf+2
no_pulse1:

    lda famistudio_output_buf+3           ;same for pulse 2
    and #$0f
    sta famistudio_r0
    lda famistudio_sfx_buffer+3,x
    and #$0f
    cmp famistudio_r0
    bcc no_pulse2
    lda famistudio_sfx_buffer+3,x
    sta famistudio_output_buf+3
    lda famistudio_sfx_buffer+4,x
    sta famistudio_output_buf+4
    lda famistudio_sfx_buffer+5,x
    sta famistudio_output_buf+5
no_pulse2:

    lda famistudio_sfx_buffer+6,x           ;overwrite triangle of main output buffer if it is active
    beq no_triangle
    sta famistudio_output_buf+6
    lda famistudio_sfx_buffer+7,x
    sta famistudio_output_buf+7
    lda famistudio_sfx_buffer+8,x
    sta famistudio_output_buf+8
no_triangle:

    lda famistudio_output_buf+9           ;same as for pulse 1 and 2, but for noise
    and #$0f
    sta famistudio_r0
    lda famistudio_sfx_buffer+9,x
    and #$0f
    cmp famistudio_r0
    bcc no_noise
    lda famistudio_sfx_buffer+9,x
    sta famistudio_output_buf+9
    lda famistudio_sfx_buffer+10,x
    sta famistudio_output_buf+10
no_noise:

    rts

.endproc 

.endif

;dummy envelope used to initialize all channels with silence

famistudio_dummy_envelope:
    .byte $c0,$7f,$00,$00

famistudio_dummy_pitch_envelope:
    .byte $00,$c0,$7f,$00,$01

;PAL and NTSC, 11-bit dividers
;rest note, then octaves 1-5, then three zeroes
;first 64 bytes are PAL, next 64 bytes are NTSC

famistudio_note_table_lsb:
    .ifdef FAMISTUDIO_CFG_PAL_SUPPORT
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
    .ifdef FAMISTUDIO_CFG_NTSC_SUPPORT
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

famistudio_note_table_msb:
    .ifdef FAMISTUDIO_CFG_PAL_SUPPORT
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
    .ifdef FAMISTUDIO_CFG_NTSC_SUPPORT
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

.ifdef FAMISTUDIO_EXP_VRC6
famistudio_saw_note_table_lsb:
    .byte $00
    .byte $44, $69, $9a, $d6, $1e, $70, $cb, $30, $9e, $13, $91, $16 ; Octave 0
    .byte $a2, $34, $cc, $6b, $0e, $b7, $65, $18, $ce, $89, $48, $0a ; Octave 1
    .byte $d0, $99, $66, $35, $07, $db, $b2, $8b, $67, $44, $23, $05 ; Octave 2
    .byte $e8, $cc, $b2, $9a, $83, $6d, $59, $45, $33, $22, $11, $02 ; Octave 3
    .byte $f3, $e6, $d9, $cc, $c1, $b6, $ac, $a2, $99, $90, $88, $80 ; Octave 4
    .byte $79, $72, $6c, $66, $60, $5b, $55, $51, $4c, $48, $44, $40 ; Octave 5
    .byte $3c, $39, $35, $32, $2f, $2d, $2a, $28, $25, $23, $21, $1f ; Octave 6
    .byte $1e, $1c, $1a, $19, $17, $16, $15, $13, $12, $11, $10, $0f ; Octave 7
famistudio_saw_note_table_msb:    
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

.ifdef FAMISTUDIO_EXP_VRC7
famistudio_vrc7_note_table_lsb:
    .byte $00
    .byte $ac, $b7, $c2, $cd, $d9, $e6, $f4, $02, $12, $22, $33, $46 ; Octave 0
    .byte $58, $6e, $84, $9a, $b2, $cc, $e8, $04, $24, $44, $66, $8c ; Octave 1
    .byte $b0, $dc, $08, $34, $64, $98, $d0, $08, $48, $88, $cc, $18 ; Octave 2
    .byte $60, $b8, $10, $68, $c8, $30, $a0, $10, $90, $10, $98, $30 ; Octave 3
    .byte $c0, $70, $20, $d0, $90, $60, $40, $20, $20, $20, $30, $60 ; Octave 4
    .byte $80, $e0, $40, $a0, $20, $c0, $80, $40, $40, $40, $60, $c0 ; Octave 5
    .byte $00, $c0, $80, $40, $40, $80, $00, $80, $80, $80, $c0, $80 ; Octave 6
    .byte $00, $80, $00, $80, $80, $00, $00, $00, $00, $00, $80, $00 ; Octave 7
famistudio_vrc7_note_table_msb:
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

.ifdef FAMISTUDIO_EXP_FDS
famistudio_fds_note_table_lsb:
    .byte $00
    .byte $13, $14, $16, $17, $18, $1a, $1b, $1d, $1e, $20, $22, $24 ; Octave 0
    .byte $26, $29, $2b, $2e, $30, $33, $36, $39, $3d, $40, $44, $48 ; Octave 1
    .byte $4d, $51, $56, $5b, $61, $66, $6c, $73, $7a, $81, $89, $91 ; Octave 2
    .byte $99, $a2, $ac, $b6, $c1, $cd, $d9, $e6, $f3, $02, $11, $21 ; Octave 3
    .byte $33, $45, $58, $6d, $82, $99, $b2, $cb, $e7, $04, $22, $43 ; Octave 4
    .byte $65, $8a, $b0, $d9, $04, $32, $63, $97, $cd, $07, $44, $85 ; Octave 5
    .byte $ca, $13, $60, $b2, $09, $65, $c6, $2d, $9b, $0e, $89, $0b ; Octave 6
    .byte $94, $26, $c1, $64, $12, $ca, $8c, $5b, $35, $1d, $12, $16 ; Octave 7
famistudio_fds_note_table_msb:
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

.ifdef FAMISTUDIO_EXP_N163
.if FAMISTUDIO_EXP_N163_CHN_CNT = 1
    famistudio_n163_note_table_lsb:
        .byte $00
        .byte $47,$4c,$50,$55,$5a,$5f,$65,$6b,$72,$78,$80,$87 ; Octave 0
        .byte $8f,$98,$a1,$aa,$b5,$bf,$cb,$d7,$e4,$f1,$00,$0f ; Octave 1
        .byte $1f,$30,$42,$55,$6a,$7f,$96,$ae,$c8,$e3,$00,$1e ; Octave 2
        .byte $3e,$60,$85,$ab,$d4,$ff,$2c,$5d,$90,$c6,$00,$3d ; Octave 3
        .byte $7d,$c1,$0a,$57,$a8,$fe,$59,$ba,$20,$8d,$00,$7a ; Octave 4
        .byte $fb,$83,$14,$ae,$50,$fd,$b3,$74,$41,$1a,$00,$f4 ; Octave 5
        .byte $f6,$07,$29,$5c,$a1,$fa,$67,$e9,$83,$35,$01,$e8 ; Octave 6
        .byte $ec,$0f,$52,$b8,$43,$f4,$ce,$d3,$06,$6a,$02,$d1 ; Octave 7
    famistudio_n163_note_table_msb:
        .byte $00
        .byte $00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00 ; Octave 0
        .byte $00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$01,$01 ; Octave 1
        .byte $01,$01,$01,$01,$01,$01,$01,$01,$01,$01,$02,$02 ; Octave 2
        .byte $02,$02,$02,$02,$02,$02,$03,$03,$03,$03,$04,$04 ; Octave 3
        .byte $04,$04,$05,$05,$05,$05,$06,$06,$07,$07,$08,$08 ; Octave 4
        .byte $08,$09,$0a,$0a,$0b,$0b,$0c,$0d,$0e,$0f,$10,$10 ; Octave 5
        .byte $11,$13,$14,$15,$16,$17,$19,$1a,$1c,$1e,$20,$21 ; Octave 6
        .byte $23,$26,$28,$2a,$2d,$2f,$32,$35,$39,$3c,$40,$43 ; Octave 7
.elseif FAMISTUDIO_EXP_N163_CHN_CNT = 2
    famistudio_n163_note_table_lsb:
        .byte $00
        .byte $8f,$98,$a1,$aa,$b5,$bf,$cb,$d7,$e4,$f1,$00,$0f ; Octave 0
        .byte $1f,$30,$42,$55,$6a,$7f,$96,$ae,$c8,$e3,$00,$1e ; Octave 1
        .byte $3e,$60,$85,$ab,$d4,$ff,$2c,$5d,$90,$c6,$00,$3d ; Octave 2
        .byte $7d,$c1,$0a,$57,$a8,$fe,$59,$ba,$20,$8d,$00,$7a ; Octave 3
        .byte $fb,$83,$14,$ae,$50,$fd,$b3,$74,$41,$1a,$00,$f4 ; Octave 4
        .byte $f6,$07,$29,$5c,$a1,$fa,$67,$e9,$83,$35,$01,$e8 ; Octave 5
        .byte $ec,$0f,$52,$b8,$43,$f4,$ce,$d3,$06,$6a,$02,$d1 ; Octave 6
        .byte $d9,$1f,$a5,$71,$86,$e8,$9c,$a7,$0d,$d5,$05,$a2 ; Octave 7
    famistudio_n163_note_table_msb:
        .byte $00
        .byte $00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$01,$01 ; Octave 0
        .byte $01,$01,$01,$01,$01,$01,$01,$01,$01,$01,$02,$02 ; Octave 1
        .byte $02,$02,$02,$02,$02,$02,$03,$03,$03,$03,$04,$04 ; Octave 2
        .byte $04,$04,$05,$05,$05,$05,$06,$06,$07,$07,$08,$08 ; Octave 3
        .byte $08,$09,$0a,$0a,$0b,$0b,$0c,$0d,$0e,$0f,$10,$10 ; Octave 4
        .byte $11,$13,$14,$15,$16,$17,$19,$1a,$1c,$1e,$20,$21 ; Octave 5
        .byte $23,$26,$28,$2a,$2d,$2f,$32,$35,$39,$3c,$40,$43 ; Octave 6
        .byte $47,$4c,$50,$55,$5a,$5f,$65,$6b,$72,$78,$80,$87 ; Octave 7
.elseif FAMISTUDIO_EXP_N163_CHN_CNT = 3
    famistudio_n163_note_table_lsb:
        .byte $00
        .byte $d7,$e4,$f1,$00,$0f,$1f,$30,$42,$56,$6a,$80,$96 ; Octave 0
        .byte $af,$c8,$e3,$00,$1f,$3f,$61,$85,$ac,$d5,$00,$2d ; Octave 1
        .byte $5e,$91,$c7,$01,$3e,$7e,$c3,$0b,$58,$aa,$00,$5b ; Octave 2
        .byte $bc,$22,$8f,$02,$7c,$fd,$86,$17,$b1,$54,$00,$b7 ; Octave 3
        .byte $78,$45,$1f,$05,$f9,$fb,$0d,$2f,$62,$a8,$01,$6e ; Octave 4
        .byte $f1,$8b,$3e,$0a,$f2,$f7,$1a,$5e,$c5,$50,$02,$dc ; Octave 5
        .byte $e3,$17,$7c,$15,$e4,$ee,$35,$bd,$8a,$a0,$04,$b9 ; Octave 6
        .byte $c6,$2e,$f8,$2a,$c9,$dc,$6a,$7a,$14,$40,$08,$73 ; Octave 7
    famistudio_n163_note_table_msb:
        .byte $00
        .byte $00,$00,$00,$01,$01,$01,$01,$01,$01,$01,$01,$01 ; Octave 0
        .byte $01,$01,$01,$02,$02,$02,$02,$02,$02,$02,$03,$03 ; Octave 1
        .byte $03,$03,$03,$04,$04,$04,$04,$05,$05,$05,$06,$06 ; Octave 2
        .byte $06,$07,$07,$08,$08,$08,$09,$0a,$0a,$0b,$0c,$0c ; Octave 3
        .byte $0d,$0e,$0f,$10,$10,$11,$13,$14,$15,$16,$18,$19 ; Octave 4
        .byte $1a,$1c,$1e,$20,$21,$23,$26,$28,$2a,$2d,$30,$32 ; Octave 5
        .byte $35,$39,$3c,$40,$43,$47,$4c,$50,$55,$5a,$60,$65 ; Octave 6
        .byte $6b,$72,$78,$80,$87,$8f,$98,$a1,$ab,$b5,$c0,$cb ; Octave 7
.elseif FAMISTUDIO_EXP_N163_CHN_CNT = 4
famistudio_n163_note_table_lsb:
        .byte $00
        .byte $1f,$30,$42,$55,$6a,$7f,$96,$ae,$c8,$e3,$00,$1e ; Octave 0
        .byte $3e,$60,$85,$ab,$d4,$ff,$2c,$5d,$90,$c6,$00,$3d ; Octave 1
        .byte $7d,$c1,$0a,$57,$a8,$fe,$59,$ba,$20,$8d,$00,$7a ; Octave 2
        .byte $fb,$83,$14,$ae,$50,$fd,$b3,$74,$41,$1a,$00,$f4 ; Octave 3
        .byte $f6,$07,$29,$5c,$a1,$fa,$67,$e9,$83,$35,$01,$e8 ; Octave 4
        .byte $ec,$0f,$52,$b8,$43,$f4,$ce,$d3,$06,$6a,$02,$d1 ; Octave 5
        .byte $d9,$1f,$a5,$71,$86,$e8,$9c,$a7,$0d,$d5,$05,$a2 ; Octave 6
        .byte $b2,$3e,$4b,$e3,$0c,$d0,$38,$4e,$1b,$ab,$ff,$ff ; Octave 7
    famistudio_n163_note_table_msb:
        .byte $00
        .byte $01,$01,$01,$01,$01,$01,$01,$01,$01,$01,$02,$02 ; Octave 0
        .byte $02,$02,$02,$02,$02,$02,$03,$03,$03,$03,$04,$04 ; Octave 1
        .byte $04,$04,$05,$05,$05,$05,$06,$06,$07,$07,$08,$08 ; Octave 2
        .byte $08,$09,$0a,$0a,$0b,$0b,$0c,$0d,$0e,$0f,$10,$10 ; Octave 3
        .byte $11,$13,$14,$15,$16,$17,$19,$1a,$1c,$1e,$20,$21 ; Octave 4
        .byte $23,$26,$28,$2a,$2d,$2f,$32,$35,$39,$3c,$40,$43 ; Octave 5
        .byte $47,$4c,$50,$55,$5a,$5f,$65,$6b,$72,$78,$80,$87 ; Octave 6
        .byte $8f,$98,$a1,$aa,$b5,$bf,$cb,$d7,$e4,$f1,$ff,$ff ; Octave 7
.elseif FAMISTUDIO_EXP_N163_CHN_CNT = 5
    famistudio_n163_note_table_lsb:
        .byte $00
        .byte $67,$7c,$93,$ab,$c4,$df,$fc,$1a,$3a,$5c,$80,$a6 ; Octave 0
        .byte $ce,$f9,$26,$56,$89,$bf,$f8,$34,$74,$b8,$00,$4c ; Octave 1
        .byte $9c,$f2,$4c,$ac,$12,$7e,$f0,$69,$e9,$70,$00,$98 ; Octave 2
        .byte $39,$e4,$99,$59,$24,$fc,$e0,$d2,$d2,$e1,$00,$31 ; Octave 3
        .byte $73,$c9,$33,$b3,$49,$f8,$c0,$a4,$a4,$c2,$01,$62 ; Octave 4
        .byte $e7,$93,$67,$67,$93,$f1,$81,$48,$48,$85,$03,$c5 ; Octave 5
        .byte $cf,$26,$cf,$ce,$27,$e2,$03,$90,$91,$0b,$06,$8a ; Octave 6
        .byte $9f,$4d,$9e,$9c,$4f,$c4,$06,$ff,$ff,$ff,$ff,$ff ; Octave 7
    famistudio_n163_note_table_msb:
        .byte $00
        .byte $01,$01,$01,$01,$01,$01,$01,$02,$02,$02,$02,$02 ; Octave 0
        .byte $02,$02,$03,$03,$03,$03,$03,$04,$04,$04,$05,$05 ; Octave 1
        .byte $05,$05,$06,$06,$07,$07,$07,$08,$08,$09,$0a,$0a ; Octave 2
        .byte $0b,$0b,$0c,$0d,$0e,$0e,$0f,$10,$11,$12,$14,$15 ; Octave 3
        .byte $16,$17,$19,$1a,$1c,$1d,$1f,$21,$23,$25,$28,$2a ; Octave 4
        .byte $2c,$2f,$32,$35,$38,$3b,$3f,$43,$47,$4b,$50,$54 ; Octave 5
        .byte $59,$5f,$64,$6a,$71,$77,$7f,$86,$8e,$97,$a0,$a9 ; Octave 6
        .byte $b3,$be,$c9,$d5,$e2,$ef,$fe,$ff,$ff,$ff,$ff,$ff ; Octave 7
.elseif FAMISTUDIO_EXP_N163_CHN_CNT = 6
    famistudio_n163_note_table_lsb:
        .byte $00
        .byte $af,$c8,$e3,$00,$1f,$3f,$61,$85,$ac,$d5,$00,$2d ; Octave 0
        .byte $5e,$91,$c7,$01,$3e,$7e,$c3,$0b,$58,$aa,$00,$5b ; Octave 1
        .byte $bc,$22,$8f,$02,$7c,$fd,$86,$17,$b1,$54,$00,$b7 ; Octave 2
        .byte $78,$45,$1f,$05,$f9,$fb,$0d,$2f,$62,$a8,$01,$6e ; Octave 3
        .byte $f1,$8b,$3e,$0a,$f2,$f7,$1a,$5e,$c5,$50,$02,$dc ; Octave 4
        .byte $e3,$17,$7c,$15,$e4,$ee,$35,$bd,$8a,$a0,$04,$b9 ; Octave 5
        .byte $c6,$2e,$f8,$2a,$c9,$dc,$6a,$7a,$14,$40,$08,$73 ; Octave 6
        .byte $8c,$5d,$f1,$ff,$ff,$ff,$ff,$ff,$ff,$ff,$ff,$ff ; Octave 7
    famistudio_n163_note_table_msb:
        .byte $00
        .byte $01,$01,$01,$02,$02,$02,$02,$02,$02,$02,$03,$03 ; Octave 0
        .byte $03,$03,$03,$04,$04,$04,$04,$05,$05,$05,$06,$06 ; Octave 1
        .byte $06,$07,$07,$08,$08,$08,$09,$0a,$0a,$0b,$0c,$0c ; Octave 2
        .byte $0d,$0e,$0f,$10,$10,$11,$13,$14,$15,$16,$18,$19 ; Octave 3
        .byte $1a,$1c,$1e,$20,$21,$23,$26,$28,$2a,$2d,$30,$32 ; Octave 4
        .byte $35,$39,$3c,$40,$43,$47,$4c,$50,$55,$5a,$60,$65 ; Octave 5
        .byte $6b,$72,$78,$80,$87,$8f,$98,$a1,$ab,$b5,$c0,$cb ; Octave 6
        .byte $d7,$e4,$f1,$ff,$ff,$ff,$ff,$ff,$ff,$ff,$ff,$ff ; Octave 7
.elseif FAMISTUDIO_EXP_N163_CHN_CNT = 7
    famistudio_n163_note_table_lsb:
        .byte $00
        .byte $f6,$14,$34,$56,$79,$9f,$c7,$f1,$1e,$4d,$80,$b5 ; Octave 0
        .byte $ed,$29,$69,$ac,$f3,$3e,$8e,$e3,$3c,$9b,$00,$6a ; Octave 1
        .byte $db,$53,$d2,$58,$e6,$7d,$1d,$c6,$79,$37,$00,$d5 ; Octave 2
        .byte $b7,$a6,$a4,$b0,$cd,$fa,$3a,$8c,$f3,$6e,$01,$ab ; Octave 3
        .byte $6f,$4d,$48,$61,$9a,$f5,$74,$19,$e6,$dd,$02,$56 ; Octave 4
        .byte $de,$9b,$91,$c3,$35,$eb,$e8,$32,$cc,$bb,$04,$ad ; Octave 5
        .byte $bc,$36,$22,$86,$6b,$d6,$d1,$64,$98,$76,$09,$5b ; Octave 6
        .byte $79,$ff,$ff,$ff,$ff,$ff,$ff,$ff,$ff,$ff,$ff,$ff ; Octave 7
    famistudio_n163_note_table_msb:
        .byte $00
        .byte $01,$02,$02,$02,$02,$02,$02,$02,$03,$03,$03,$03 ; Octave 0
        .byte $03,$04,$04,$04,$04,$05,$05,$05,$06,$06,$07,$07 ; Octave 1
        .byte $07,$08,$08,$09,$09,$0a,$0b,$0b,$0c,$0d,$0e,$0e ; Octave 2
        .byte $0f,$10,$11,$12,$13,$14,$16,$17,$18,$1a,$1c,$1d ; Octave 3
        .byte $1f,$21,$23,$25,$27,$29,$2c,$2f,$31,$34,$38,$3b ; Octave 4
        .byte $3e,$42,$46,$4a,$4f,$53,$58,$5e,$63,$69,$70,$76 ; Octave 5
        .byte $7d,$85,$8d,$95,$9e,$a7,$b1,$bc,$c7,$d3,$e0,$ed ; Octave 6
        .byte $fb,$ff,$ff,$ff,$ff,$ff,$ff,$ff,$ff,$ff,$ff,$ff ; Octave 7
.elseif FAMISTUDIO_EXP_N163_CHN_CNT = 8
    famistudio_n163_note_table_lsb:
        .byte $00
        .byte $3e,$60,$85,$ab,$d4,$ff,$2c,$5d,$90,$c6,$00,$3d ; Octave 0
        .byte $7d,$c1,$0a,$57,$a8,$fe,$59,$ba,$20,$8d,$00,$7a ; Octave 1
        .byte $fb,$83,$14,$ae,$50,$fd,$b3,$74,$41,$1a,$00,$f4 ; Octave 2
        .byte $f6,$07,$29,$5c,$a1,$fa,$67,$e9,$83,$35,$01,$e8 ; Octave 3
        .byte $ec,$0f,$52,$b8,$43,$f4,$ce,$d3,$06,$6a,$02,$d1 ; Octave 4
        .byte $d9,$1f,$a5,$71,$86,$e8,$9c,$a7,$0d,$d5,$05,$a2 ; Octave 5
        .byte $b2,$3e,$4b,$e3,$0c,$d0,$38,$4e,$1b,$ab,$ff,$ff ; Octave 6
        .byte $ff,$ff,$ff,$ff,$ff,$ff,$ff,$ff,$ff,$ff,$ff,$ff ; Octave 7
    famistudio_n163_note_table_msb:
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

famistudio_channel_to_volume_env:
    .byte FAMISTUDIO_CH0_ENVS+FAMISTUDIO_ENV_VOLUME_OFF
    .byte FAMISTUDIO_CH1_ENVS+FAMISTUDIO_ENV_VOLUME_OFF
    .byte FAMISTUDIO_CH2_ENVS+FAMISTUDIO_ENV_VOLUME_OFF
    .byte FAMISTUDIO_CH3_ENVS+FAMISTUDIO_ENV_VOLUME_OFF
    .byte $ff
.if .defined(FAMISTUDIO_CH5_ENVS)
    .byte FAMISTUDIO_CH5_ENVS+FAMISTUDIO_ENV_VOLUME_OFF
.endif
.if .defined(FAMISTUDIO_CH6_ENVS)
    .byte FAMISTUDIO_CH6_ENVS+FAMISTUDIO_ENV_VOLUME_OFF
.endif
.if .defined(FAMISTUDIO_CH7_ENVS)
    .byte FAMISTUDIO_CH7_ENVS+FAMISTUDIO_ENV_VOLUME_OFF
.endif
.if .defined(FAMISTUDIO_CH8_ENVS)
    .byte FAMISTUDIO_CH8_ENVS+FAMISTUDIO_ENV_VOLUME_OFF
.endif
.if .defined(FAMISTUDIO_CH9_ENVS)
    .byte FAMISTUDIO_CH9_ENVS+FAMISTUDIO_ENV_VOLUME_OFF
.endif
.if .defined(FAMISTUDIO_CH10_ENVS)
    .byte FAMISTUDIO_CH10_ENVS+FAMISTUDIO_ENV_VOLUME_OFF
.endif
.if .defined(FAMISTUDIO_CH11_ENVS)
    .byte FAMISTUDIO_CH11_ENVS+FAMISTUDIO_ENV_VOLUME_OFF
.endif
.if .defined(FAMISTUDIO_CH12_ENVS)
    .byte FAMISTUDIO_CH12_ENVS+FAMISTUDIO_ENV_VOLUME_OFF
.endif

.ifdef FAMISTUDIO_USE_ARPEGGIO
famistudio_channel_to_arpeggio_env:
    .byte FAMISTUDIO_CH0_ENVS+FAMISTUDIO_ENV_NOTE_OFF
    .byte FAMISTUDIO_CH1_ENVS+FAMISTUDIO_ENV_NOTE_OFF
    .byte FAMISTUDIO_CH2_ENVS+FAMISTUDIO_ENV_NOTE_OFF
    .byte FAMISTUDIO_CH3_ENVS+FAMISTUDIO_ENV_NOTE_OFF
    .byte $ff
.if .defined(FAMISTUDIO_CH5_ENVS)
    .byte FAMISTUDIO_CH5_ENVS+FAMISTUDIO_ENV_NOTE_OFF
.endif
.if .defined(FAMISTUDIO_CH6_ENVS)
    .byte FAMISTUDIO_CH6_ENVS+FAMISTUDIO_ENV_NOTE_OFF
.endif
.if .defined(FAMISTUDIO_CH7_ENVS)
    .byte FAMISTUDIO_CH7_ENVS+FAMISTUDIO_ENV_NOTE_OFF
.endif
.if .defined(FAMISTUDIO_CH8_ENVS)
    .byte FAMISTUDIO_CH8_ENVS+FAMISTUDIO_ENV_NOTE_OFF
.endif
.if .defined(FAMISTUDIO_CH9_ENVS)
    .byte FAMISTUDIO_CH9_ENVS+FAMISTUDIO_ENV_NOTE_OFF
.endif
.if .defined(FAMISTUDIO_CH10_ENVS)
    .byte FAMISTUDIO_CH10_ENVS+FAMISTUDIO_ENV_NOTE_OFF
.endif
.if .defined(FAMISTUDIO_CH11_ENVS)
    .byte FAMISTUDIO_CH11_ENVS+FAMISTUDIO_ENV_NOTE_OFF
.endif
.if .defined(FAMISTUDIO_CH12_ENVS)
    .byte FAMISTUDIO_CH12_ENVS+FAMISTUDIO_ENV_NOTE_OFF
.endif
.endif

famistudio_channel_to_pitch_env:
famistudio_channel_to_slide:
    .byte $00
    .byte $01
    .byte $02
    .byte $ff ; no slide for noise
    .byte $ff ; no slide for DPCM
.if FAMISTUDIO_NUM_PITCH_ENVELOPES >= 4
    .byte $03
.endif
.if FAMISTUDIO_NUM_PITCH_ENVELOPES >= 5
    .byte $04
.endif    
.if FAMISTUDIO_NUM_PITCH_ENVELOPES >= 6
    .byte $05
.endif
.if FAMISTUDIO_NUM_PITCH_ENVELOPES >= 7
    .byte $06
.endif
.if FAMISTUDIO_NUM_PITCH_ENVELOPES >= 8
    .byte $07
.endif
.if FAMISTUDIO_NUM_PITCH_ENVELOPES >= 9
    .byte $08
.endif
.if FAMISTUDIO_NUM_PITCH_ENVELOPES >= 10
    .byte $09
.endif
.if FAMISTUDIO_NUM_PITCH_ENVELOPES >= 11
    .byte $0a
.endif

famistudio_duty_lookup:
    .byte $30
    .byte $70
    .byte $b0
    .byte $f0

.ifdef FAMISTUDIO_EXP_VRC6
famistudio_vrc6_duty_lookup:
    .byte $00
    .byte $10
    .byte $20
    .byte $30
    .byte $40
    .byte $50
    .byte $60
    .byte $70
.endif

.ifndef FAMISTUDIO_USE_FAMITRACKER_TEMPO
famistudio_tempo_frame_lookup:
    .byte $01, $02 ; NTSC -> NTSC, NTSC -> PAL
    .byte $00, $01 ; PAL  -> NTSC, PAL  -> PAL
.endif

.ifdef FAMISTUDIO_CFG_SMOOTH_VIBRATO
; lookup table for the 2 registers we need to set for smooth vibrato.
; Index 0 decrement the hi-period, index 2 increments. Index 1 is unused. 
famistudio_smooth_vibrato_period_lo_lookup:
	.byte $00, $00, $ff
famistudio_smooth_vibrato_sweep_lookup:
	.byte $8f, $00, $87
.endif

.ifdef ::FAMISTUDIO_USE_VOLUME_TRACK

; Precomputed volume multiplication table (rounded but never to zero unless one of the value is zero).
; Load the 2 volumes in the lo/hi nibble and fetch.

famistudio_volume_table:
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

.endif
