; HEADER
INES_MAPPER = 0 ; 0 = NROM
INES_MIRROR = 1 ; 0 = horizontal mirroring, 1 = vertical mirroring
INES_SRAM   = 0 ; 1 = battery backed SRAM at $6000-7FFF

.db 'N', 'E', 'S', $1A ; ID 
.db $02 ; 16k PRG bank count
.db $01 ; 8k CHR bank count
.db INES_MIRROR | (INES_SRAM << 1) | ((INES_MAPPER & $f) << 4)
.db (INES_MAPPER & %11110000)
.db $0, $0, $0, $0, $0, $0, $0, $0 ; padding
.fillvalue $ff

; CODE
.org $8000
famistudio_dpcm_bank_callback:
    rts

; CODE
.org $8100
FAMISTUDIO_CFG_EXTERNAL   = 1
FAMISTUDIO_ASSEMBLER_ASM6F = 1
FAMISTUDIO_ASM6_ZP_ENUM   = $0000
FAMISTUDIO_ASM6_BSS_ENUM  = $0300
FAMISTUDIO_ASM6_CODE_BASE = $8100
.include "test_defs.inc"
.include "../famistudio.s"

; VECTORS
.org $fffA
.db $ff, $ff, $ff, $ff, $ff, $ff

; CHARS
.dsb 8192, $ff
