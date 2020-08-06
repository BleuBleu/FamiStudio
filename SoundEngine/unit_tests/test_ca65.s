.segment "HEADER"
INES_MAPPER = 0 ; 0 = NROM
INES_MIRROR = 1 ; 0 = horizontal mirroring, 1 = vertical mirroring
INES_SRAM   = 0 ; 1 = battery backed SRAM at $6000-7FFF

.byte 'N', 'E', 'S', $1A ; ID 
.byte $02 ; 16k PRG bank count
.byte $01 ; 8k CHR bank count
.byte INES_MIRROR | (INES_SRAM << 1) | ((INES_MAPPER & $f) << 4)
.byte (INES_MAPPER & %11110000)
.byte $0, $0, $0, $0, $0, $0, $0, $0 ; padding

.segment "ZEROPAGE"
dummy_zp: .res 18 ; MATTT

.segment "RAM"
dummy_ram: .res 160 ; MATTT

.segment "CODE"

FAMISTUDIO_CFG_EXTERNAL = 1
.include "test_defs.inc"
.include "..\famistudio_ca65.s"

.segment "CHARS"
.res 8192


