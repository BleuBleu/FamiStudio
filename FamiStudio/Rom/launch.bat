@taskkill /im mesen.exe

@..\..\..\NES\tools\bin\msxsl.exe %USERPROFILE%\Documents\Mesen\Debugger\rom.Workspace.xml ..\..\..\NES\tools\bin\cleandebug.xml -o %USERPROFILE%\Documents\Mesen\Debugger\rom.Workspace.xml

@findstr /V "zeropage.*type=equ @LOCAL-MACRO_SYMBOL" rom.dbg > rom.dbg.new
@del rom.dbg
@ren rom.dbg.new rom.dbg

@del %USERPROFILE%\Documents\Mesen\Debugger\rom.cdl
@del %USERPROFILE%\Documents\Mesen\RecentGames\rom.*

..\..\..\NES\tools\bin\Mesen.exe rom.nes
