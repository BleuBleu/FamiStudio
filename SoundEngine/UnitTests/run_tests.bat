@echo off

:: Compile a bunch of permutations and make sure the NES roms are binary identical across all 3 assemblers.
set NES_INCLUDE=..\
set count=0
:Loop
	set /a count=%count%+1
	call :CompileRomPermutation || goto Error
	if %count% neq 1000 goto Loop

echo All ROMs are identical!
goto Done

:CompileRomPermutation
echo ===========================
echo Comparing with definitions:
echo ===========================

:: Generate random definition include file.
type NUL > test_defs.inc

@setlocal enabledelayedexpansion

set /a rnd=%random% %%3
if "%rnd%"=="0" (
	echo FAMISTUDIO_CFG_NTSC_SUPPORT=1 >> test_defs.inc
) else (
	if "%rnd%"=="1" (
		echo FAMISTUDIO_CFG_PAL_SUPPORT=1 >> test_defs.inc
	) else (
		echo FAMISTUDIO_CFG_NTSC_SUPPORT=1 >> test_defs.inc
		echo FAMISTUDIO_CFG_PAL_SUPPORT=1 >> test_defs.inc
	)
)

set /a rnd=%random% %%2
if "%rnd%"=="1" (
	echo FAMISTUDIO_CFG_SFX_SUPPORT=1 >> test_defs.inc
	set /a rnd=%random% %%4+1
	echo FAMISTUDIO_CFG_SFX_STREAMS=!rnd! >> test_defs.inc
)

set /a rnd=%random% %%2
if "%rnd%"=="1" (
	echo FAMISTUDIO_CFG_SMOOTH_VIBRATO=1 >> test_defs.inc
)

set /a rnd=%random% %%3
if "%rnd%"=="1" (
	echo FAMISTUDIO_CFG_DPCM_SUPPORT=1 >> test_defs.inc
	set /a rnd=%random% %%2
	if "!rnd!"=="1" (
		echo FAMISTUDIO_USE_DELTA_COUNTER=1 >> test_defs.inc
	)
	set /a rnd=%random% %%2
	if "!rnd!"=="1" (
		echo FAMISTUDIO_USE_DPCM_BANKSWITCHING=1 >> test_defs.inc
	)
	set /a rnd=%random% %%2
	if "!rnd!"=="1" (
		echo FAMISTUDIO_USE_DPCM_EXTENDED_RANGE=1 >> test_defs.inc
	)
)

set /a rnd=%random% %%8
if "%rnd%"=="0" (
	echo FAMISTUDIO_EXP_VRC6=1 >> test_defs.inc
) else (
	if "%rnd%"=="1" (
		echo FAMISTUDIO_EXP_VRC7=1 >> test_defs.inc
	) else (
		if "%rnd%"=="2" (
			echo FAMISTUDIO_EXP_MMC5=1 >> test_defs.inc
		) else (
			if "%rnd%"=="3" (
				echo FAMISTUDIO_EXP_S5B=1 >> test_defs.inc
			) else (
				if "%rnd%"=="4" (
					echo FAMISTUDIO_EXP_FDS=1 >> test_defs.inc
					set /a rnd=%random% %%2
					if "!rnd!"=="1" (
						echo FAMISTUDIO_USE_FDS_AUTOMOD=1 >> test_defs.inc
					)
				) else (
					if "%rnd%"=="5" (
						echo FAMISTUDIO_EXP_N163=1 >> test_defs.inc
						set /a rnd=%random% %%8+1
						echo FAMISTUDIO_EXP_N163_CHN_CNT=!rnd! >> test_defs.inc
						) else (
						if "%rnd%"=="6" (
							echo FAMISTUDIO_EXP_EPSM=1 >> test_defs.inc
							set /a rnd=%random% %%4
							echo FAMISTUDIO_EXP_EPSM_SSG_CHN_CNT=!rnd! >> test_defs.inc
							set /a rnd=%random% %%7
							echo FAMISTUDIO_EXP_EPSM_FM_CHN_CNT=!rnd! >> test_defs.inc
							set /a rnd=%random% %%2
							echo FAMISTUDIO_EXP_EPSM_RHYTHM_CHN1_ENABLE=!rnd! >> test_defs.inc
							set /a rnd=%random% %%2
							echo FAMISTUDIO_EXP_EPSM_RHYTHM_CHN2_ENABLE=!rnd! >> test_defs.inc
							set /a rnd=%random% %%2
							echo FAMISTUDIO_EXP_EPSM_RHYTHM_CHN3_ENABLE=!rnd! >> test_defs.inc
							set /a rnd=%random% %%2
							echo FAMISTUDIO_EXP_EPSM_RHYTHM_CHN4_ENABLE=!rnd! >> test_defs.inc
							set /a rnd=%random% %%2
							echo FAMISTUDIO_EXP_EPSM_RHYTHM_CHN5_ENABLE=!rnd! >> test_defs.inc
							set /a rnd=%random% %%2
							echo FAMISTUDIO_EXP_EPSM_RHYTHM_CHN6_ENABLE=!rnd! >> test_defs.inc
						) else (
							if "%rnd%"=="7" (
								echo FAMISTUDIO_EXP_RAINBOW=1 >> test_defs.inc
							)
						)
					)
				)
			)
		)
	)
)

set /a rnd=%random% %%2
if "%rnd%"=="1" (
	echo FAMISTUDIO_USE_FAMITRACKER_TEMPO=1 >> test_defs.inc
	set /a rnd=%random% %%2
	if "!rnd!"=="1" (
		echo FAMISTUDIO_USE_FAMITRACKER_DELAYED_NOTES_OR_CUTS=1 >> test_defs.inc
	)
)

set /a rnd=%random% %%2
if "%rnd%"=="1" (
	echo FAMISTUDIO_USE_VOLUME_TRACK=1 >> test_defs.inc
	set /a rnd=%random% %%2
	if "!rnd!"=="1" (
		echo FAMISTUDIO_USE_VOLUME_SLIDES=1 >> test_defs.inc
	)
)

set /a rnd=%random% %%2
if "%rnd%"=="1" (
	echo FAMISTUDIO_USE_PITCH_TRACK=1 >> test_defs.inc
)

set /a rnd=%random% %%2
if "%rnd%"=="1" (
	echo FAMISTUDIO_USE_RELEASE_NOTES=1 >> test_defs.inc
)

set /a rnd=%random% %%2
if "%rnd%"=="1" (
	echo FAMISTUDIO_USE_SLIDE_NOTES=1 >> test_defs.inc
	set /a rnd=%random% %%2
	if "!rnd!"=="1" (
		echo FAMISTUDIO_USE_NOISE_SLIDE_NOTES=1 >> test_defs.inc
	)
)

set /a rnd=%random% %%2
if "%rnd%"=="1" (
	echo FAMISTUDIO_USE_VIBRATO=1 >> test_defs.inc
)

set /a rnd=%random% %%2
if "%rnd%"=="1" (
	echo FAMISTUDIO_USE_ARPEGGIO=1 >> test_defs.inc
)

set /a rnd=%random% %%2
if "%rnd%"=="1" (
	echo FAMISTUDIO_USE_DUTYCYCLE_EFFECT=1 >> test_defs.inc
)

set /a rnd=%random% %%2
if "%rnd%"=="1" (
	echo FAMISTUDIO_USE_PHASE_RESET=1 >> test_defs.inc
)

set /a rnd=%random% %%2
if "%rnd%"=="1" (
	echo FAMISTUDIO_USE_INSTRUMENT_EXTENDED_RANGE=1 >> test_defs.inc
)

type test_defs.inc

:: Compile everything.
..\..\Tools\ca65 test_ca65.s -g -o test_ca65.o
..\..\Tools\ld65 -C test_ca65.cfg -o test_ca65.nes test_ca65.o --mapfile test_ca65.map --dbgfile test_ca65.dbg
pushd ..
REM ..\Tools\asm6 UnitTests\test_asm6.asm UnitTests\test_asm6.nes
..\Tools\asm6_fixed UnitTests\test_asm6.asm UnitTests\test_asm6.nes
popd
..\..\Tools\NESASM3 test_nesasm.asm

..\..\Tools\sdas6500 -pogn -I"." -I".." -y -s -l test_sdas.o test_sdas.s
..\..\Tools\sdld6808 -n -i -j -y -w -u -w -b _ZP=0x0000 -b _BSS=0x300 -b _CODE=0x8000 test_sdas.ihx test_sdas.o
..\..\Tools\makebin -s 73728 -o 32752 test_sdas.ihx test_sdas.nes

:: Binary comparison of all 3 ROMs.
fc /b test_ca65.nes test_asm6.nes > nul
@if errorlevel 1 exit /b 1
fc /b test_ca65.nes test_nesasm.nes > nul
@if errorlevel 1 exit /b 1
fc /b test_ca65.nes test_sdas.nes > nul
@if errorlevel 1 exit /b 1

:: Cleanup.
del /q *.o
del /q *.fns
rem del /q *.nes
del /q *.map
del /q *.dbg
del /q *.lst
del /q *.rst
del /q *.ihx

exit /b 0

:Error
echo Error! ROMs are NOT identical with these definitions!
type test_defs.inc

:Done
