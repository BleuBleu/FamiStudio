@echo off
@del /q *.o *.bin *.dbg *.map

SETLOCAL

CALL :CompileNsfPermutation nsf_template.cfg, "{CODESIZE}=$480 {SONGDATASTART}=$8500", "", nsf_ft2
CALL :CompileNsfPermutation nsf_template.cfg, "{CODESIZE}=$980 {SONGDATASTART}=$8a00", "-D FS", nsf_ft2_fs
CALL :CompileNsfPermutation nsf_template.cfg, "{CODESIZE}=$c80 {SONGDATASTART}=$8d00", "-D FS -D FT_VRC6", nsf_ft2_fs_vrc6
CALL :CompileNsfPermutation nsf_template.cfg, "{CODESIZE}=$b80 {SONGDATASTART}=$8c00", "-D FS -D FT_MMC5", nsf_ft2_fs_mmc5
CALL :CompileNsfPermutation nsf_template.cfg, "{CODESIZE}=$c80 {SONGDATASTART}=$8d00", "-D FS -D FT_S5B", nsf_ft2_fs_s5b

EXIT /B %ERRORLEVEL%

:CompileNsfPermutation
@echo %~4
PatchText %~1 tmp.cfg %~2
..\..\..\NES\tools\bin\ca65 nsf.s -g -o %~4.o %~3 
..\..\..\NES\tools\bin\ld65 -C tmp.cfg -o %~4.bin %~4.o --mapfile %~4.map --dbgfile %~4.dbg
del tmp.cfg
EXIT /B 0
