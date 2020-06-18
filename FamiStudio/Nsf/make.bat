@echo off
@del /q *.o *.bin *.dbg *.map

SETLOCAL

:: Stock FamiTone2 (FamiTracker tempo)
CALL :CompileNsfPermutation nsf_template.cfg, "{CODESIZE}=$480 {SONGDATASTART}=$8500", "-D FT_NTSC_SUPPORT=1", nsf_famitone2_ntsc
CALL :CompileNsfPermutation nsf_template.cfg, "{CODESIZE}=$480 {SONGDATASTART}=$8500", "-D FT_PAL_SUPPORT=1", nsf_famitone2_pal
CALL :CompileNsfPermutation nsf_template.cfg, "{CODESIZE}=$580 {SONGDATASTART}=$8600", "-D FT_NTSC_SUPPORT=1 -D FT_PAL_SUPPORT=1", nsf_famitone2_dual

:: Stock FamiTone2 (FamiStudio tempo mode)
CALL :CompileNsfPermutation nsf_template.cfg, "{CODESIZE}=$480 {SONGDATASTART}=$8500", "-D FT_NTSC_SUPPORT=1 -D FT_FAMISTUDIO_TEMPO=1", nsf_famitone2_ntsc_tempo
CALL :CompileNsfPermutation nsf_template.cfg, "{CODESIZE}=$580 {SONGDATASTART}=$8600", "-D FT_PAL_SUPPORT=1 -D FT_FAMISTUDIO_TEMPO=1", nsf_famitone2_pal_tempo
CALL :CompileNsfPermutation nsf_template.cfg, "{CODESIZE}=$580 {SONGDATASTART}=$8600", "-D FT_NTSC_SUPPORT=1 -D FT_PAL_SUPPORT=1 -D FT_FAMISTUDIO_TEMPO=1", nsf_famitone2_dual_tempo

:: FamiStudio engine (FamiTracker tempo)
CALL :CompileNsfPermutation nsf_template.cfg, "{CODESIZE}=$980 {SONGDATASTART}=$8a00", "-D FAMISTUDIO -D FT_NTSC_SUPPORT=1", nsf_famistudio_ntsc
CALL :CompileNsfPermutation nsf_template.cfg, "{CODESIZE}=$980 {SONGDATASTART}=$8a00", "-D FAMISTUDIO -D FT_PAL_SUPPORT=1", nsf_famistudio_pal
CALL :CompileNsfPermutation nsf_template.cfg, "{CODESIZE}=$a80 {SONGDATASTART}=$8b00", "-D FAMISTUDIO -D FT_NTSC_SUPPORT=1 -D FT_PAL_SUPPORT=1", nsf_famistudio_dual
CALL :CompileNsfPermutation nsf_template.cfg, "{CODESIZE}=$c80 {SONGDATASTART}=$8d00", "-D FAMISTUDIO -D FT_NTSC_SUPPORT=1 -D FT_VRC6", nsf_famistudio_vrc6_ntsc
CALL :CompileNsfPermutation nsf_template.cfg, "{CODESIZE}=$d80 {SONGDATASTART}=$8e00", "-D FAMISTUDIO -D FT_NTSC_SUPPORT=1 -D FT_VRC7", nsf_famistudio_vrc7_ntsc
CALL :CompileNsfPermutation nsf_template.cfg, "{CODESIZE}=$a80 {SONGDATASTART}=$8b00", "-D FAMISTUDIO -D FT_NTSC_SUPPORT=1 -D FT_MMC5", nsf_famistudio_mmc5_ntsc
CALL :CompileNsfPermutation nsf_template.cfg, "{CODESIZE}=$a80 {SONGDATASTART}=$8b00", "-D FAMISTUDIO -D FT_NTSC_SUPPORT=1 -D FT_S5B", nsf_famistudio_s5b_ntsc
CALL :CompileNsfPermutation nsf_template.cfg, "{CODESIZE}=$c80 {SONGDATASTART}=$8d00", "-D FAMISTUDIO -D FT_NTSC_SUPPORT=1 -D FT_FDS", nsf_famistudio_fds_ntsc
CALL :CompileNsfPermutation nsf_template.cfg, "{CODESIZE}=$c80 {SONGDATASTART}=$8d00", "-D FAMISTUDIO -D FT_NTSC_SUPPORT=1 -D FT_N163 -D FT_N163_CHN_CNT=1", nsf_famistudio_n163_1ch_ntsc
CALL :CompileNsfPermutation nsf_template.cfg, "{CODESIZE}=$c80 {SONGDATASTART}=$8d00", "-D FAMISTUDIO -D FT_NTSC_SUPPORT=1 -D FT_N163 -D FT_N163_CHN_CNT=2", nsf_famistudio_n163_2ch_ntsc
CALL :CompileNsfPermutation nsf_template.cfg, "{CODESIZE}=$c80 {SONGDATASTART}=$8d00", "-D FAMISTUDIO -D FT_NTSC_SUPPORT=1 -D FT_N163 -D FT_N163_CHN_CNT=3", nsf_famistudio_n163_3ch_ntsc
CALL :CompileNsfPermutation nsf_template.cfg, "{CODESIZE}=$d80 {SONGDATASTART}=$8e00", "-D FAMISTUDIO -D FT_NTSC_SUPPORT=1 -D FT_N163 -D FT_N163_CHN_CNT=4", nsf_famistudio_n163_4ch_ntsc
CALL :CompileNsfPermutation nsf_template.cfg, "{CODESIZE}=$d80 {SONGDATASTART}=$8e00", "-D FAMISTUDIO -D FT_NTSC_SUPPORT=1 -D FT_N163 -D FT_N163_CHN_CNT=5", nsf_famistudio_n163_5ch_ntsc
CALL :CompileNsfPermutation nsf_template.cfg, "{CODESIZE}=$d80 {SONGDATASTART}=$8e00", "-D FAMISTUDIO -D FT_NTSC_SUPPORT=1 -D FT_N163 -D FT_N163_CHN_CNT=6", nsf_famistudio_n163_6ch_ntsc
CALL :CompileNsfPermutation nsf_template.cfg, "{CODESIZE}=$d80 {SONGDATASTART}=$8e00", "-D FAMISTUDIO -D FT_NTSC_SUPPORT=1 -D FT_N163 -D FT_N163_CHN_CNT=7", nsf_famistudio_n163_7ch_ntsc
CALL :CompileNsfPermutation nsf_template.cfg, "{CODESIZE}=$d80 {SONGDATASTART}=$8e00", "-D FAMISTUDIO -D FT_NTSC_SUPPORT=1 -D FT_N163 -D FT_N163_CHN_CNT=8", nsf_famistudio_n163_8ch_ntsc

:: FamiStudio engine (FamiStudio tempo)
CALL :CompileNsfPermutation nsf_template.cfg, "{CODESIZE}=$a80 {SONGDATASTART}=$8a00", "-D FAMISTUDIO -D FT_NTSC_SUPPORT=1 -D FT_FAMISTUDIO_TEMPO=1", nsf_famistudio_ntsc_tempo
CALL :CompileNsfPermutation nsf_template.cfg, "{CODESIZE}=$a80 {SONGDATASTART}=$8b00", "-D FAMISTUDIO -D FT_PAL_SUPPORT=1 -D FT_FAMISTUDIO_TEMPO=1", nsf_famistudio_pal_tempo
CALL :CompileNsfPermutation nsf_template.cfg, "{CODESIZE}=$a80 {SONGDATASTART}=$8b00", "-D FAMISTUDIO -D FT_NTSC_SUPPORT=1 -D FT_PAL_SUPPORT=1 -D FT_FAMISTUDIO_TEMPO=1", nsf_famistudio_dual_tempo
CALL :CompileNsfPermutation nsf_template.cfg, "{CODESIZE}=$c80 {SONGDATASTART}=$8d00", "-D FAMISTUDIO -D FT_NTSC_SUPPORT=1 -D FT_VRC6 -D FT_FAMISTUDIO_TEMPO=1", nsf_famistudio_vrc6_ntsc_tempo
CALL :CompileNsfPermutation nsf_template.cfg, "{CODESIZE}=$d80 {SONGDATASTART}=$8e00", "-D FAMISTUDIO -D FT_NTSC_SUPPORT=1 -D FT_VRC7 -D FT_FAMISTUDIO_TEMPO=1", nsf_famistudio_vrc7_ntsc_tempo
CALL :CompileNsfPermutation nsf_template.cfg, "{CODESIZE}=$b80 {SONGDATASTART}=$8b00", "-D FAMISTUDIO -D FT_NTSC_SUPPORT=1 -D FT_MMC5 -D FT_FAMISTUDIO_TEMPO=1", nsf_famistudio_mmc5_ntsc_tempo
CALL :CompileNsfPermutation nsf_template.cfg, "{CODESIZE}=$a80 {SONGDATASTART}=$8b00", "-D FAMISTUDIO -D FT_NTSC_SUPPORT=1 -D FT_S5B -D FT_FAMISTUDIO_TEMPO=1", nsf_famistudio_s5b_ntsc_tempo
CALL :CompileNsfPermutation nsf_template.cfg, "{CODESIZE}=$c80 {SONGDATASTART}=$8d00", "-D FAMISTUDIO -D FT_NTSC_SUPPORT=1 -D FT_FDS -D FT_FAMISTUDIO_TEMPO=1", nsf_famistudio_fds_ntsc_tempo
CALL :CompileNsfPermutation nsf_template.cfg, "{CODESIZE}=$c80 {SONGDATASTART}=$8d00", "-D FAMISTUDIO -D FT_NTSC_SUPPORT=1 -D FT_N163 -D FT_N163_CHN_CNT=1 -D FT_FAMISTUDIO_TEMPO=1", nsf_famistudio_n163_1ch_ntsc_tempo
CALL :CompileNsfPermutation nsf_template.cfg, "{CODESIZE}=$d80 {SONGDATASTART}=$8d00", "-D FAMISTUDIO -D FT_NTSC_SUPPORT=1 -D FT_N163 -D FT_N163_CHN_CNT=2 -D FT_FAMISTUDIO_TEMPO=1", nsf_famistudio_n163_2ch_ntsc_tempo
CALL :CompileNsfPermutation nsf_template.cfg, "{CODESIZE}=$d80 {SONGDATASTART}=$8d00", "-D FAMISTUDIO -D FT_NTSC_SUPPORT=1 -D FT_N163 -D FT_N163_CHN_CNT=3 -D FT_FAMISTUDIO_TEMPO=1", nsf_famistudio_n163_3ch_ntsc_tempo
CALL :CompileNsfPermutation nsf_template.cfg, "{CODESIZE}=$d80 {SONGDATASTART}=$8e00", "-D FAMISTUDIO -D FT_NTSC_SUPPORT=1 -D FT_N163 -D FT_N163_CHN_CNT=4 -D FT_FAMISTUDIO_TEMPO=1", nsf_famistudio_n163_4ch_ntsc_tempo
CALL :CompileNsfPermutation nsf_template.cfg, "{CODESIZE}=$d80 {SONGDATASTART}=$8e00", "-D FAMISTUDIO -D FT_NTSC_SUPPORT=1 -D FT_N163 -D FT_N163_CHN_CNT=5 -D FT_FAMISTUDIO_TEMPO=1", nsf_famistudio_n163_5ch_ntsc_tempo
CALL :CompileNsfPermutation nsf_template.cfg, "{CODESIZE}=$d80 {SONGDATASTART}=$8e00", "-D FAMISTUDIO -D FT_NTSC_SUPPORT=1 -D FT_N163 -D FT_N163_CHN_CNT=6 -D FT_FAMISTUDIO_TEMPO=1", nsf_famistudio_n163_6ch_ntsc_tempo
CALL :CompileNsfPermutation nsf_template.cfg, "{CODESIZE}=$d80 {SONGDATASTART}=$8e00", "-D FAMISTUDIO -D FT_NTSC_SUPPORT=1 -D FT_N163 -D FT_N163_CHN_CNT=7 -D FT_FAMISTUDIO_TEMPO=1", nsf_famistudio_n163_7ch_ntsc_tempo
CALL :CompileNsfPermutation nsf_template.cfg, "{CODESIZE}=$d80 {SONGDATASTART}=$8e00", "-D FAMISTUDIO -D FT_NTSC_SUPPORT=1 -D FT_N163 -D FT_N163_CHN_CNT=8 -D FT_FAMISTUDIO_TEMPO=1", nsf_famistudio_n163_8ch_ntsc_tempo

EXIT /B %ERRORLEVEL%

:CompileNsfPermutation
@echo %~4
PatchText %~1 tmp.cfg %~2
..\..\..\NES\tools\bin\ca65 nsf.s -g -o %~4.o %~3 
..\..\..\NES\tools\bin\ld65 -C tmp.cfg -o %~4.bin %~4.o --mapfile %~4.map --dbgfile %~4.dbg
PrintCodeSize %~4.map
del tmp.cfg
EXIT /B 0
