    ; HEADER
    .inesprg 2 ; 1x 16KB PRG code
    .ineschr 1 ; 1x  8KB CHR data
    .inesmap 0 ; mapper 0 = NROM, no bank swapping
    .inesmir 1 ; background mirroring

    ; CODE
    .bank 0
    .org $8000 

FAMISTUDIO_CFG_EXTERNAL = 1
    .include "test_defs.inc"
    .include "..\famistudio_nesasm.asm"
    .bank 4

    .org $0000
    .rs 8192
