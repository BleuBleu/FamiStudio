@echo [Compiling]
@echo off

..\..\..\NES\tools\bin\ca65 nsf.s -g -o nsf_ft2.o
..\..\..\NES\tools\bin\ca65 nsf.s -g -o nsf_ft2_fs.o

@del nsf_ft2.bin
@del nsf_ft2.dbg
@del nsf_ft2_fs.bin
@del nsf_ft2_fs.dbg

@echo [Linking]
..\..\..\NES\tools\bin\ld65 -o nsf_ft2.bin    -C nsf.cfg nsf_ft2.o    --dbgfile nsf_ft2.dbg    --mapfile nsf_ft2.map    --force-import nsf_init --force-import nsf_play
..\..\..\NES\tools\bin\ld65 -o nsf_ft2_fs.bin -C nsf.cfg nsf_ft2_fs.o --dbgfile nsf_ft2_fs.dbg --mapfile nsf_ft2_fs.map --force-import nsf_init --force-import nsf_play

