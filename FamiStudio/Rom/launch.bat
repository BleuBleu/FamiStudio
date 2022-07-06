@taskkill /im mesen.exe

@..\..\Tools\msxsl.exe %USERPROFILE%\Documents\Mesen\Debugger\rom_ntsc.Workspace.xml ..\..\..\NES\tools\bin\cleandebug.xml -o %USERPROFILE%\Documents\Mesen\Debugger\rom_ntsc.Workspace.xml

::@findstr /V "zeropage.*type=equ @LOCAL-MACRO_SYMBOL" fds.dbg > fds.dbg.new
::@del fds.dbg
::@ren fds.dbg.new fds.dbg

@del %USERPROFILE%\Documents\Mesen\Debugger\rom_*.cdl
@del %USERPROFILE%\Documents\Mesen\Debugger\fds*.cdl
@del %USERPROFILE%\Documents\Mesen\RecentGames\rom_*.*
@del %USERPROFILE%\Documents\Mesen\RecentGames\fds*.*
@del %USERPROFILE%\Documents\Mesen\Saves\fds*.ips

PackFds fds.fds fdstoc.bin fdsdata.bin fds_patched.fds
copy fds.dbg fds_patched.dbg
::..\..\Tools\Mesen.exe fds.fds
..\..\Tools\Mesen.exe fds_patched.fds

::..\..\Tools\Mesen.exe rom_s5b_ntsc.nes
::..\..\Tools\MesenX.exe rom_epsm_ntsc.nes
