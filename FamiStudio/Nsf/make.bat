@echo off
@del /q *.o *.bin *.dbg *.map

SETLOCAL

CALL :CompileNsfPermutation nsf_template.cfg, "{CODESIZE}=$480 {SONGDATASTART}=$8500", "", nsf_ft2
CALL :CompileNsfPermutation nsf_template.cfg, "{CODESIZE}=$980 {SONGDATASTART}=$8a00", "-D FS", nsf_ft2_fs
CALL :CompileNsfPermutation nsf_template.cfg, "{CODESIZE}=$c80 {SONGDATASTART}=$8d00", "-D FS -D FT_VRC6", nsf_ft2_fs_vrc6
CALL :CompileNsfPermutation nsf_template.cfg, "{CODESIZE}=$d80 {SONGDATASTART}=$8e00", "-D FS -D FT_VRC7", nsf_ft2_fs_vrc7
CALL :CompileNsfPermutation nsf_template.cfg, "{CODESIZE}=$a80 {SONGDATASTART}=$8b00", "-D FS -D FT_MMC5", nsf_ft2_fs_mmc5
CALL :CompileNsfPermutation nsf_template.cfg, "{CODESIZE}=$a80 {SONGDATASTART}=$8b00", "-D FS -D FT_S5B", nsf_ft2_fs_s5b
CALL :CompileNsfPermutation nsf_template.cfg, "{CODESIZE}=$c80 {SONGDATASTART}=$8d00", "-D FS -D FT_FDS", nsf_ft2_fs_fds
CALL :CompileNsfPermutation nsf_template.cfg, "{CODESIZE}=$c80 {SONGDATASTART}=$8d00", "-D FS -D FT_N163 -D FT_N163_CHN_CNT=1", nsf_ft2_fs_n163_1ch
CALL :CompileNsfPermutation nsf_template.cfg, "{CODESIZE}=$c80 {SONGDATASTART}=$8d00", "-D FS -D FT_N163 -D FT_N163_CHN_CNT=2", nsf_ft2_fs_n163_2ch
CALL :CompileNsfPermutation nsf_template.cfg, "{CODESIZE}=$c80 {SONGDATASTART}=$8d00", "-D FS -D FT_N163 -D FT_N163_CHN_CNT=3", nsf_ft2_fs_n163_3ch
CALL :CompileNsfPermutation nsf_template.cfg, "{CODESIZE}=$c80 {SONGDATASTART}=$8d00", "-D FS -D FT_N163 -D FT_N163_CHN_CNT=4", nsf_ft2_fs_n163_4ch
CALL :CompileNsfPermutation nsf_template.cfg, "{CODESIZE}=$d80 {SONGDATASTART}=$8e00", "-D FS -D FT_N163 -D FT_N163_CHN_CNT=5", nsf_ft2_fs_n163_5ch
CALL :CompileNsfPermutation nsf_template.cfg, "{CODESIZE}=$d80 {SONGDATASTART}=$8e00", "-D FS -D FT_N163 -D FT_N163_CHN_CNT=6", nsf_ft2_fs_n163_6ch
CALL :CompileNsfPermutation nsf_template.cfg, "{CODESIZE}=$d80 {SONGDATASTART}=$8e00", "-D FS -D FT_N163 -D FT_N163_CHN_CNT=7", nsf_ft2_fs_n163_7ch
CALL :CompileNsfPermutation nsf_template.cfg, "{CODESIZE}=$d80 {SONGDATASTART}=$8e00", "-D FS -D FT_N163 -D FT_N163_CHN_CNT=8", nsf_ft2_fs_n163_8ch

EXIT /B %ERRORLEVEL%

:CompileNsfPermutation
@echo %~4
PatchText %~1 tmp.cfg %~2
..\..\..\NES\tools\bin\ca65 nsf.s -g -o %~4.o %~3 
..\..\..\NES\tools\bin\ld65 -C tmp.cfg -o %~4.bin %~4.o --mapfile %~4.map --dbgfile %~4.dbg
del tmp.cfg
EXIT /B 0
