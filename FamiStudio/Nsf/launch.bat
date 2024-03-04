@taskkill /im mesen.exe

@..\..\Tools\msxsl.exe %USERPROFILE%\Documents\Mesen\Debugger\nsf.Workspace.xml ..\..\..\NES\tools\bin\cleandebug.xml -o %USERPROFILE%\Documents\Mesen\Debugger\nsf.Workspace.xml

PatchNsf epsm.nsf nsf_famistudio_epsm_ntsc.bin nsf.nsf
copy /y nsf_famistudio_epsm_ntsc.dbg nsf.dbg

@findstr /V "zeropage.*type=equ @LOCAL-MACRO_SYMBOL" nsf.dbg > nsf.dbg.new
@del nsf.dbg
@ren nsf.dbg.new nsf.dbg

@del %USERPROFILE%\Documents\Mesen\Debugger\nsf.cdl
@del %USERPROFILE%\Documents\Mesen\RecentGames\nsf.*

::..\..\Tools\Mesen.exe nsf.nsf
..\..\Tools\MesenX.exe nsf.nsf
::..\..\Tools\nsfplay.exe nsf.nsf
