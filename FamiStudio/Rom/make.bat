@echo [Compiling]
@echo off
@del *.o
..\..\..\NES\tools\bin\ca65 rom.s -g -o rom_famitracker_ntsc.o -D FAMISTUDIO_CFG_EQUALIZER -D FAMISTUDIO_CFG_NTSC_SUPPORT -D FAMISTUDIO_USE_FAMITRACKER_TEMPO
..\..\..\NES\tools\bin\ca65 rom.s -g -o rom_famitracker_pal.o -D FAMISTUDIO_CFG_EQUALIZER -D FAMISTUDIO_CFG_PAL_SUPPORT -D FAMISTUDIO_USE_FAMITRACKER_TEMPO
..\..\..\NES\tools\bin\ca65 rom.s -g -o rom_ntsc.o -D FAMISTUDIO_CFG_EQUALIZER -D FAMISTUDIO_CFG_NTSC_SUPPORT
..\..\..\NES\tools\bin\ca65 rom.s -g -o rom_pal.o -D FAMISTUDIO_CFG_EQUALIZER -D FAMISTUDIO_CFG_PAL_SUPPORT
..\..\..\NES\tools\bin\ca65 rom.s -g -o fds_famitracker.o -D FAMISTUDIO_CFG_EQUALIZER -D FAMISTUDIO_CFG_NTSC_SUPPORT -D FAMISTUDIO_USE_FAMITRACKER_TEMPO -D FAMISTUDIO_EXP_FDS
..\..\..\NES\tools\bin\ca65 rom.s -g -o fds.o -D FAMISTUDIO_CFG_EQUALIZER -D FAMISTUDIO_CFG_NTSC_SUPPORT -D FAMISTUDIO_EXP_FDS
@del *.nes
@del *.fds
@echo [Linking]
..\..\..\NES\tools\bin\ld65 -C rom.cfg -o rom_famitracker_ntsc.nes rom_famitracker_ntsc.o --mapfile rom_famitracker_ntsc.map --dbgfile rom_famitracker_ntsc.dbg
..\..\..\NES\tools\bin\ld65 -C rom.cfg -o rom_famitracker_pal.nes rom_famitracker_pal.o --mapfile rom_famitracker_pal.map --dbgfile rom_famitracker_pal.dbg
..\..\..\NES\tools\bin\ld65 -C rom.cfg -o rom_ntsc.nes rom_ntsc.o --mapfile rom_ntsc.map --dbgfile rom_ntsc.dbg
..\..\..\NES\tools\bin\ld65 -C rom.cfg -o rom_pal.nes rom_pal.o --mapfile rom_pal.map --dbgfile rom_pal.dbg
..\..\..\NES\tools\bin\ld65 -C fds.cfg -o fds_famitracker.fds fds_famitracker.o --mapfile fds_famitracker.map --dbgfile fds_famitracker.dbg
..\..\..\NES\tools\bin\ld65 -C fds.cfg -o fds.fds fds.o --mapfile fds.map --dbgfile fds.dbg

