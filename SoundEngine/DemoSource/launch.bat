

@findstr /V "zeropage.*type=equ @LOCAL-MACRO_SYMBOL" demo_ca65.dbg > demo_ca65.dbg.new
@del demo_ca65.dbg
@ren demo_ca65.dbg.new demo_ca65.dbg

@del %USERPROFILE%\Documents\Mesen\Debugger\demo_ca65.cdl
@del %USERPROFILE%\Documents\Mesen\RecentGames\demo_ca65.*

::..\..\NES\tools\bin\Mesen.exe demo_ca65.nes
::..\..\..\NES\tools\bin\Mesen.exe demo_nesasm.nes
..\..\..\NES\tools\bin\Mesen.exe demo_asm6.nes

