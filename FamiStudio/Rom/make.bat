@echo [Compiling]
@echo off
@del *.o
..\..\..\NES\tools\bin\ca65 rom.s -g -o rom_ntsc.o -D FAMISTUDIO_CFG_EQUALIZER -D FT_NTSC_SUPPORT=1
..\..\..\NES\tools\bin\ca65 rom.s -g -o rom_pal.o -D FAMISTUDIO_CFG_EQUALIZER -D FT_PAL_SUPPORT=1
..\..\..\NES\tools\bin\ca65 rom.s -g -o rom_ntsc_tempo.o -D FAMISTUDIO_CFG_EQUALIZER -D FT_NTSC_SUPPORT=1 -D FT_FAMISTUDIO_TEMPO=1
..\..\..\NES\tools\bin\ca65 rom.s -g -o rom_pal_tempo.o -D FAMISTUDIO_CFG_EQUALIZER -D FT_PAL_SUPPORT=1 -D FT_FAMISTUDIO_TEMPO=1
@del *.nes
@echo [Linking]
..\..\..\NES\tools\bin\ld65 -C rom.cfg -o rom_ntsc.nes rom_ntsc.o --mapfile rom_ntsc.map --dbgfile rom_ntsc.dbg
..\..\..\NES\tools\bin\ld65 -C rom.cfg -o rom_pal.nes rom_pal.o --mapfile rom_pal.map --dbgfile rom_pal.dbg
..\..\..\NES\tools\bin\ld65 -C rom.cfg -o rom_ntsc_tempo.nes rom_ntsc_tempo.o --mapfile rom_ntsc_tempo.map --dbgfile rom_ntsc_tempo.dbg
..\..\..\NES\tools\bin\ld65 -C rom.cfg -o rom_pal_tempo.nes rom_pal_tempo.o --mapfile rom_pal_tempo.map --dbgfile rom_pal_tempo.dbg
