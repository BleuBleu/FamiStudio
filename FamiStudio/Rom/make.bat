@echo [Compiling]
@echo off

..\..\..\NES\tools\bin\ca65 rom.s -g -o rom.o -D FT_EQUALIZER
@del rom.nes
@echo [Linking]
..\..\..\NES\tools\bin\ld65 -C rom.cfg -o rom.nes rom.o --mapfile rom.map --dbgfile rom.dbg
