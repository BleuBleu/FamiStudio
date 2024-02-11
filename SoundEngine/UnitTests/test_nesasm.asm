
    .list

    ; HEADER
    .inesprg 2 ; 1x 16KB PRG code
    .ineschr 1 ; 1x  8KB CHR data
    .inesmap 0 ; mapper 0 = NROM, no bank swapping
    .inesmir 1 ; background mirroring


    ; CODE
    .bank 0
    .org $8000 
famistudio_dpcm_bank_callback:
    rts

    ; CODE2
    .bank 0
    .org $8100 

FAMISTUDIO_CFG_EXTERNAL      = 1
FAMISTUDIO_ASSEMBLER_NESASM  = 1
FAMISTUDIO_NESASM_ZP_RSSET   = $0000
FAMISTUDIO_NESASM_BSS_RSSET  = $0300
FAMISTUDIO_NESASM_CODE_BANK  = 0
FAMISTUDIO_NESASM_CODE_ORG   = $8100

    .include "test_defs.inc"
    .include "../famistudio.s"
    .bank 4

    .org $0000
    .rs 8192
