@echo [Compiling]
@echo off

..\..\..\NES\tools\bin\ca65 nsf.s -g -o nsf_ft2.o
..\..\..\NES\tools\bin\ca65 nsf.s -g -o nsf_ft2_fs.o -D FS
..\..\..\NES\tools\bin\ca65 nsf.s -g -o nsf_ft2_fs_vrc6.o -D FS -D FT_VRC6_ENABLE

@del nsf_ft2.bin
@del nsf_ft2_fs.bin
@del nsf_ft2_fs_vrc6.bin

@echo [Linking]
..\..\..\NES\tools\bin\ld65 -C nsf_ft2.cfg -o nsf_ft2.bin nsf_ft2.o --mapfile nsf_ft2.map
..\..\..\NES\tools\bin\ld65 -C nsf_ft2_fs.cfg -o nsf_ft2_fs.bin nsf_ft2_fs.o --mapfile nsf_ft2_fs.map
..\..\..\NES\tools\bin\ld65 -C nsf_ft2_fs_vrc6.cfg -o nsf_ft2_fs_vrc6.bin nsf_ft2_fs_vrc6.o --mapfile nsf_ft2_fs_vrc6.map

