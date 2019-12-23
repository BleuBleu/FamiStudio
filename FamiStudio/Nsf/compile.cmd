@echo [Compiling]
@echo off

..\..\..\NES\tools\bin\ca65 nsf.s -g -o nsf_ft2.o
..\..\..\NES\tools\bin\ca65 nsf.s -g -o nsf_ft2_fs.o -D FS
..\..\..\NES\tools\bin\ca65 nsf.s -g -o nsf_ft2_fs_vrc6.o -D FS -D FT_VRC6_ENABLE

@del nsf_ft2.bin
@del nsf_ft2.dbg
@del nsf_ft2_fs.bin
@del nsf_ft2_fs.dbg
@del nsf_ft2_fs_vrc6.bin
@del nsf_ft2_fs_vrc6.dbg

@echo [Linking]
..\..\..\NES\tools\bin\ld65 -C nsf.cfg -o nsf_ft2.bin         nsf_ft2.o         --dbgfile nsf_ft2.dbg         --mapfile nsf_ft2.map         --force-import nsf_init --force-import nsf_play
..\..\..\NES\tools\bin\ld65 -C nsf.cfg -o nsf_ft2_fs.bin      nsf_ft2_fs.o      --dbgfile nsf_ft2_fs.dbg      --mapfile nsf_ft2_fs.map      --force-import nsf_init --force-import nsf_play
..\..\..\NES\tools\bin\ld65 -C nsf.cfg -o nsf_ft2_fs_vrc6.bin nsf_ft2_fs_vrc6.o --dbgfile nsf_ft2_fs_vrc6.dbg --mapfile nsf_ft2_fs_vrc6.map --force-import nsf_init --force-import nsf_play

