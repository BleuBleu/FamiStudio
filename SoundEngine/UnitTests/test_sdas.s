.area _HEADER (ABS)
.org 0x7FF0
INES_MAPPER = 0 ; 0 = NROM
INES_MIRROR = 1 ; 0 = horizontal mirroring, 1 = vertical mirroring
INES_SRAM   = 0 ; 1 = battery backed SRAM at 0x6000-7FFF

.db 'N', 'E', 'S', 0x1A ; ID 
.db 0x02 ; 16k PRG bank count
.db 0x01 ; 8k CHR bank count
.db INES_MIRROR | (INES_SRAM << 1) | ((INES_MAPPER & 0xf) << 4)
.db (INES_MAPPER & 0b11110000)
.db 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0 ; padding

.area _CODE
famistudio_dpcm_bank_callback:
    rts

.ds 255

FAMISTUDIO_CFG_EXTERNAL = 1
.define FAMISTUDIO_SDAS_ZP_SEGMENT   "_ZP"
.define FAMISTUDIO_SDAS_RAM_SEGMENT  "_BSS"
.define FAMISTUDIO_SDAS_CODE_SEGMENT "_CODE"
.include "test_defs.inc"
.include "..\famistudio_sdas.s"

.area _CHARS (ABS)
.org 0x10000
.ds 8192


