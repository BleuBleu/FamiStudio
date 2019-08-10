@echo [Compiling]
@echo off

..\..\..\NES\tools\bin\ca65 nsf.s -g -o nsf.o

@del nsf.bin
@del nsf.dbg

@echo [Linking]
..\..\..\NES\tools\bin\ld65 -o nsf.bin -C nsf.cfg nsf.o --dbgfile nsf.dbg --mapfile nsf.map --force-import nsf_init --force-import nsf_play

