@echo [Compiling]
@del *.o
..\..\Tools\ca65 rom.s -g -o rom_ntsc.o -D FAMISTUDIO_CFG_NTSC_SUPPORT=1
..\..\Tools\ca65 rom.s -g -o rom_ntsc_famitracker.o -D FAMISTUDIO_CFG_NTSC_SUPPORT=1 -D FAMISTUDIO_USE_FAMITRACKER_TEMPO=1
..\..\Tools\ca65 rom.s -g -o rom_pal.o -D FAMISTUDIO_CFG_PAL_SUPPORT=1
..\..\Tools\ca65 rom.s -g -o rom_pal_famitracker.o -D FAMISTUDIO_CFG_PAL_SUPPORT=1 -D FAMISTUDIO_USE_FAMITRACKER_TEMPO=1
..\..\Tools\ca65 rom.s -g -o rom_mmc5_ntsc.o -D FAMISTUDIO_CFG_NTSC_SUPPORT=1 -D FAMISTUDIO_EXP_MMC5=1
..\..\Tools\ca65 rom.s -g -o rom_mmc5_ntsc_famitracker.o -D FAMISTUDIO_CFG_NTSC_SUPPORT=1 -D FAMISTUDIO_USE_FAMITRACKER_TEMPO=1 -D FAMISTUDIO_EXP_MMC5=1
..\..\Tools\ca65 rom.s -g -o rom_n163_1ch_ntsc.o -D FAMISTUDIO_CFG_NTSC_SUPPORT=1 -D FAMISTUDIO_EXP_N163=1 -D FAMISTUDIO_EXP_N163_CHN_CNT=1
..\..\Tools\ca65 rom.s -g -o rom_n163_1ch_ntsc_famitracker.o -D FAMISTUDIO_CFG_NTSC_SUPPORT=1 -D FAMISTUDIO_USE_FAMITRACKER_TEMPO=1 -D FAMISTUDIO_EXP_N163=1 -D FAMISTUDIO_EXP_N163_CHN_CNT=1
..\..\Tools\ca65 rom.s -g -o rom_n163_2ch_ntsc.o -D FAMISTUDIO_CFG_NTSC_SUPPORT=1 -D FAMISTUDIO_EXP_N163=1 -D FAMISTUDIO_EXP_N163_CHN_CNT=2
..\..\Tools\ca65 rom.s -g -o rom_n163_2ch_ntsc_famitracker.o -D FAMISTUDIO_CFG_NTSC_SUPPORT=1 -D FAMISTUDIO_USE_FAMITRACKER_TEMPO=1 -D FAMISTUDIO_EXP_N163=1 -D FAMISTUDIO_EXP_N163_CHN_CNT=2
..\..\Tools\ca65 rom.s -g -o rom_n163_3ch_ntsc.o -D FAMISTUDIO_CFG_NTSC_SUPPORT=1 -D FAMISTUDIO_EXP_N163=1 -D FAMISTUDIO_EXP_N163_CHN_CNT=3
..\..\Tools\ca65 rom.s -g -o rom_n163_3ch_ntsc_famitracker.o -D FAMISTUDIO_CFG_NTSC_SUPPORT=1 -D FAMISTUDIO_USE_FAMITRACKER_TEMPO=1 -D FAMISTUDIO_EXP_N163=1 -D FAMISTUDIO_EXP_N163_CHN_CNT=3
..\..\Tools\ca65 rom.s -g -o rom_n163_4ch_ntsc.o -D FAMISTUDIO_CFG_NTSC_SUPPORT=1 -D FAMISTUDIO_EXP_N163=1 -D FAMISTUDIO_EXP_N163_CHN_CNT=4
..\..\Tools\ca65 rom.s -g -o rom_n163_4ch_ntsc_famitracker.o -D FAMISTUDIO_CFG_NTSC_SUPPORT=1 -D FAMISTUDIO_USE_FAMITRACKER_TEMPO=1 -D FAMISTUDIO_EXP_N163=1 -D FAMISTUDIO_EXP_N163_CHN_CNT=4
..\..\Tools\ca65 rom.s -g -o rom_n163_5ch_ntsc.o -D FAMISTUDIO_CFG_NTSC_SUPPORT=1 -D FAMISTUDIO_EXP_N163=1 -D FAMISTUDIO_EXP_N163_CHN_CNT=5
..\..\Tools\ca65 rom.s -g -o rom_n163_5ch_ntsc_famitracker.o -D FAMISTUDIO_CFG_NTSC_SUPPORT=1 -D FAMISTUDIO_USE_FAMITRACKER_TEMPO=1 -D FAMISTUDIO_EXP_N163=1 -D FAMISTUDIO_EXP_N163_CHN_CNT=5
..\..\Tools\ca65 rom.s -g -o rom_n163_6ch_ntsc.o -D FAMISTUDIO_CFG_NTSC_SUPPORT=1 -D FAMISTUDIO_EXP_N163=1 -D FAMISTUDIO_EXP_N163_CHN_CNT=6
..\..\Tools\ca65 rom.s -g -o rom_n163_6ch_ntsc_famitracker.o -D FAMISTUDIO_CFG_NTSC_SUPPORT=1 -D FAMISTUDIO_USE_FAMITRACKER_TEMPO=1 -D FAMISTUDIO_EXP_N163=1 -D FAMISTUDIO_EXP_N163_CHN_CNT=6
..\..\Tools\ca65 rom.s -g -o rom_n163_7ch_ntsc.o -D FAMISTUDIO_CFG_NTSC_SUPPORT=1 -D FAMISTUDIO_EXP_N163=1 -D FAMISTUDIO_EXP_N163_CHN_CNT=7
..\..\Tools\ca65 rom.s -g -o rom_n163_7ch_ntsc_famitracker.o -D FAMISTUDIO_CFG_NTSC_SUPPORT=1 -D FAMISTUDIO_USE_FAMITRACKER_TEMPO=1 -D FAMISTUDIO_EXP_N163=1 -D FAMISTUDIO_EXP_N163_CHN_CNT=7
..\..\Tools\ca65 rom.s -g -o rom_n163_8ch_ntsc.o -D FAMISTUDIO_CFG_NTSC_SUPPORT=1 -D FAMISTUDIO_EXP_N163=1 -D FAMISTUDIO_EXP_N163_CHN_CNT=8
..\..\Tools\ca65 rom.s -g -o rom_n163_8ch_ntsc_famitracker.o -D FAMISTUDIO_CFG_NTSC_SUPPORT=1 -D FAMISTUDIO_USE_FAMITRACKER_TEMPO=1 -D FAMISTUDIO_EXP_N163=1 -D FAMISTUDIO_EXP_N163_CHN_CNT=8
..\..\Tools\ca65 rom.s -g -o rom_s5b_ntsc.o -D FAMISTUDIO_CFG_NTSC_SUPPORT=1 -D FAMISTUDIO_EXP_S5B=1
..\..\Tools\ca65 rom.s -g -o rom_s5b_ntsc_famitracker.o -D FAMISTUDIO_CFG_NTSC_SUPPORT=1 -D FAMISTUDIO_USE_FAMITRACKER_TEMPO=1 -D FAMISTUDIO_EXP_S5B=1
..\..\Tools\ca65 rom.s -g -o rom_vrc6_ntsc.o -D FAMISTUDIO_CFG_NTSC_SUPPORT=1 -D FAMISTUDIO_EXP_VRC6=1
..\..\Tools\ca65 rom.s -g -o rom_vrc6_ntsc_famitracker.o -D FAMISTUDIO_CFG_NTSC_SUPPORT=1 -D FAMISTUDIO_USE_FAMITRACKER_TEMPO=1 -D FAMISTUDIO_EXP_VRC6=1
..\..\Tools\ca65 rom.s -g -o rom_vrc7_ntsc.o -D FAMISTUDIO_CFG_NTSC_SUPPORT=1 -D FAMISTUDIO_EXP_VRC7=1
..\..\Tools\ca65 rom.s -g -o rom_vrc7_ntsc_famitracker.o -D FAMISTUDIO_CFG_NTSC_SUPPORT=1 -D FAMISTUDIO_USE_FAMITRACKER_TEMPO=1 -D FAMISTUDIO_EXP_VRC7=1
..\..\Tools\ca65 rom.s -g -o rom_epsm_ntsc.o -D FAMISTUDIO_CFG_NTSC_SUPPORT=1 -D FAMISTUDIO_EXP_EPSM=1
..\..\Tools\ca65 rom.s -g -o rom_epsm_ntsc_famitracker.o -D FAMISTUDIO_CFG_NTSC_SUPPORT=1 -D FAMISTUDIO_USE_FAMITRACKER_TEMPO=1 -D FAMISTUDIO_EXP_EPSM=1
..\..\Tools\ca65 rom.s -g -o fds.o -D FAMISTUDIO_CFG_NTSC_SUPPORT=1 -D FAMISTUDIO_EXP_FDS=1
..\..\Tools\ca65 rom.s -g -o fds_famitracker.o -D FAMISTUDIO_CFG_NTSC_SUPPORT=1 -D FAMISTUDIO_USE_FAMITRACKER_TEMPO=1 -D FAMISTUDIO_EXP_FDS=1
@del *.nes
@del *.fds
@echo [Linking]
..\..\Tools\ld65 -C rom.cfg -o rom_ntsc.nes rom_ntsc.o --mapfile rom_ntsc.map --dbgfile rom_ntsc.dbg
..\..\Tools\ld65 -C rom.cfg -o rom_ntsc_famitracker.nes rom_ntsc_famitracker.o --mapfile rom_ntsc_famitracker.map --dbgfile rom_ntsc_famitracker.dbg
..\..\Tools\ld65 -C rom.cfg -o rom_pal.nes rom_pal.o --mapfile rom_pal.map --dbgfile rom_pal.dbg
..\..\Tools\ld65 -C rom.cfg -o rom_pal_famitracker.nes rom_pal_famitracker.o --mapfile rom_pal_famitracker.map --dbgfile rom_pal_famitracker.dbg
..\..\Tools\ld65 -C rom.cfg -o rom_mmc5_ntsc.nes rom_mmc5_ntsc.o --mapfile rom_mmc5_ntsc.map --dbgfile rom_mmc5_ntsc.dbg
..\..\Tools\ld65 -C rom.cfg -o rom_mmc5_ntsc_famitracker.nes rom_mmc5_ntsc_famitracker.o --mapfile rom_mmc5_ntsc_famitracker.map --dbgfile rom_mmc5_ntsc_famitracker.dbg
..\..\Tools\ld65 -C rom.cfg -o rom_n163_1ch_ntsc.nes rom_n163_1ch_ntsc.o --mapfile rom_n163_1ch_ntsc.map --dbgfile rom_n163_1ch_ntsc.dbg
..\..\Tools\ld65 -C rom.cfg -o rom_n163_1ch_ntsc_famitracker.nes rom_n163_1ch_ntsc_famitracker.o --mapfile rom_n163_1ch_ntsc_famitracker.map --dbgfile rom_n163_1ch_ntsc_famitracker.dbg
..\..\Tools\ld65 -C rom.cfg -o rom_n163_2ch_ntsc.nes rom_n163_2ch_ntsc.o --mapfile rom_n163_2ch_ntsc.map --dbgfile rom_n163_2ch_ntsc.dbg
..\..\Tools\ld65 -C rom.cfg -o rom_n163_2ch_ntsc_famitracker.nes rom_n163_2ch_ntsc_famitracker.o --mapfile rom_n163_2ch_ntsc_famitracker.map --dbgfile rom_n163_2ch_ntsc_famitracker.dbg
..\..\Tools\ld65 -C rom.cfg -o rom_n163_3ch_ntsc.nes rom_n163_3ch_ntsc.o --mapfile rom_n163_3ch_ntsc.map --dbgfile rom_n163_3ch_ntsc.dbg
..\..\Tools\ld65 -C rom.cfg -o rom_n163_3ch_ntsc_famitracker.nes rom_n163_3ch_ntsc_famitracker.o --mapfile rom_n163_3ch_ntsc_famitracker.map --dbgfile rom_n163_3ch_ntsc_famitracker.dbg
..\..\Tools\ld65 -C rom.cfg -o rom_n163_4ch_ntsc.nes rom_n163_4ch_ntsc.o --mapfile rom_n163_4ch_ntsc.map --dbgfile rom_n163_4ch_ntsc.dbg
..\..\Tools\ld65 -C rom.cfg -o rom_n163_4ch_ntsc_famitracker.nes rom_n163_4ch_ntsc_famitracker.o --mapfile rom_n163_4ch_ntsc_famitracker.map --dbgfile rom_n163_4ch_ntsc_famitracker.dbg
..\..\Tools\ld65 -C rom.cfg -o rom_n163_5ch_ntsc.nes rom_n163_5ch_ntsc.o --mapfile rom_n163_5ch_ntsc.map --dbgfile rom_n163_5ch_ntsc.dbg
..\..\Tools\ld65 -C rom.cfg -o rom_n163_5ch_ntsc_famitracker.nes rom_n163_5ch_ntsc_famitracker.o --mapfile rom_n163_5ch_ntsc_famitracker.map --dbgfile rom_n163_5ch_ntsc_famitracker.dbg
..\..\Tools\ld65 -C rom.cfg -o rom_n163_6ch_ntsc.nes rom_n163_6ch_ntsc.o --mapfile rom_n163_6ch_ntsc.map --dbgfile rom_n163_6ch_ntsc.dbg
..\..\Tools\ld65 -C rom.cfg -o rom_n163_6ch_ntsc_famitracker.nes rom_n163_6ch_ntsc_famitracker.o --mapfile rom_n163_6ch_ntsc_famitracker.map --dbgfile rom_n163_6ch_ntsc_famitracker.dbg
..\..\Tools\ld65 -C rom.cfg -o rom_n163_7ch_ntsc.nes rom_n163_7ch_ntsc.o --mapfile rom_n163_7ch_ntsc.map --dbgfile rom_n163_7ch_ntsc.dbg
..\..\Tools\ld65 -C rom.cfg -o rom_n163_7ch_ntsc_famitracker.nes rom_n163_7ch_ntsc_famitracker.o --mapfile rom_n163_7ch_ntsc_famitracker.map --dbgfile rom_n163_7ch_ntsc_famitracker.dbg
..\..\Tools\ld65 -C rom.cfg -o rom_n163_8ch_ntsc.nes rom_n163_8ch_ntsc.o --mapfile rom_n163_8ch_ntsc.map --dbgfile rom_n163_8ch_ntsc.dbg
..\..\Tools\ld65 -C rom.cfg -o rom_n163_8ch_ntsc_famitracker.nes rom_n163_8ch_ntsc_famitracker.o --mapfile rom_n163_8ch_ntsc_famitracker.map --dbgfile rom_n163_8ch_ntsc_famitracker.dbg
..\..\Tools\ld65 -C rom.cfg -o rom_s5b_ntsc.nes rom_s5b_ntsc.o --mapfile rom_s5b_ntsc.map --dbgfile rom_s5b_ntsc.dbg
..\..\Tools\ld65 -C rom.cfg -o rom_s5b_ntsc_famitracker.nes rom_s5b_ntsc_famitracker.o --mapfile rom_s5b_ntsc_famitracker.map --dbgfile rom_s5b_ntsc_famitracker.dbg
..\..\Tools\ld65 -C rom.cfg -o rom_vrc6_ntsc.nes rom_vrc6_ntsc.o --mapfile rom_vrc6_ntsc.map --dbgfile rom_vrc6_ntsc.dbg
..\..\Tools\ld65 -C rom.cfg -o rom_vrc6_ntsc_famitracker.nes rom_vrc6_ntsc_famitracker.o --mapfile rom_vrc6_ntsc_famitracker.map --dbgfile rom_vrc6_ntsc_famitracker.dbg
..\..\Tools\ld65 -C rom.cfg -o rom_vrc7_ntsc.nes rom_vrc7_ntsc.o --mapfile rom_vrc7_ntsc.map --dbgfile rom_vrc7_ntsc.dbg
..\..\Tools\ld65 -C rom.cfg -o rom_vrc7_ntsc_famitracker.nes rom_vrc7_ntsc_famitracker.o --mapfile rom_vrc7_ntsc_famitracker.map --dbgfile rom_vrc7_ntsc_famitracker.dbg
..\..\Tools\ld65 -C rom_epsm.cfg -o rom_epsm_ntsc.nes rom_epsm_ntsc.o --mapfile rom_epsm_ntsc.map --dbgfile rom_epsm_ntsc.dbg
..\..\Tools\ld65 -C rom_epsm.cfg -o rom_epsm_ntsc_famitracker.nes rom_epsm_ntsc_famitracker.o --mapfile rom_epsm_ntsc_famitracker.map --dbgfile rom_epsm_ntsc_famitracker.dbg
..\..\Tools\ld65 -C fds.cfg -o fds.fds fds.o --mapfile fds.map --dbgfile fds.dbg
..\..\Tools\ld65 -C fds.cfg -o fds_famitracker.fds fds_famitracker.o --mapfile fds_famitracker.map --dbgfile fds_famitracker.dbg

