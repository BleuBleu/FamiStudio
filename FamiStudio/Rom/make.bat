@echo [Compiling]
@echo off

..\..\..\NES\tools\bin\ca65 rom.s -g -o rom.o -D FT_EQUALIZER -D FT_NTSC_SUPPORT=1
..\..\..\NES\tools\bin\ca65 rom.s -g -o rom_tempo.o -D FT_EQUALIZER -D FT_NTSC_SUPPORT=1 -D FT_FAMISTUDIO_TEMPO=1
@del rom.nes
@del rom_tempo.nes
@echo [Linking]
..\..\..\NES\tools\bin\ld65 -C rom.cfg -o rom.nes rom.o --mapfile rom.map --dbgfile rom.dbg
..\..\..\NES\tools\bin\ld65 -C rom.cfg -o rom_tempo.nes rom_tempo.o --mapfile rom_tempo.map --dbgfile rom_tempo.dbg
