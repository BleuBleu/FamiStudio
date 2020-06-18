@taskkill /im mesen.exe

@..\..\..\NES\tools\bin\msxsl.exe %USERPROFILE%\Documents\Mesen\Debugger\nsf.Workspace.xml ..\..\..\NES\tools\bin\cleandebug.xml -o %USERPROFILE%\Documents\Mesen\Debugger\nsf.Workspace.xml

PatchNsf dual.nsf nsf_famistudio_dual_tempo.bin nsf.nsf
copy /y nsf_famistudio_dual_tempo.dbg nsf.dbg

@findstr /V "zeropage.*type=equ @LOCAL-MACRO_SYMBOL" nsf.dbg > nsf.dbg.new
@del nsf.dbg
@ren nsf.dbg.new nsf.dbg

@del %USERPROFILE%\Documents\Mesen\Debugger\nsf.cdl
@del %USERPROFILE%\Documents\Mesen\RecentGames\nsf.*

..\..\..\NES\tools\bin\Mesen.exe nsf.nsf
