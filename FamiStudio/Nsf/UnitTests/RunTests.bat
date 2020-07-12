..\..\bin\Release\FamiStudio.exe TestBase.fms nsf-export TestBase.nsf
..\..\bin\Release\FamiStudio.exe TestFDS.fms nsf-export TestFDS.nsf
..\..\bin\Release\FamiStudio.exe TestMMC5.fms nsf-export TestMMC5.nsf
..\..\bin\Release\FamiStudio.exe TestN163.fms nsf-export TestN163.nsf
..\..\bin\Release\FamiStudio.exe TestS5B.fms nsf-export TestS5B.nsf
..\..\bin\Release\FamiStudio.exe TestVRC6.fms nsf-export TestVRC6.nsf
..\..\bin\Release\FamiStudio.exe TestVRC7.fms nsf-export TestVRC7.nsf

..\..\bin\Release\FamiStudio.exe TestBase.nsf famistudio-txt-export TestBase_Test.txt
..\..\bin\Release\FamiStudio.exe TestFDS.nsf famistudio-txt-export TestFDS_Test.txt
..\..\bin\Release\FamiStudio.exe TestMMC5.nsf famistudio-txt-export TestMMC5_Test.txt
..\..\bin\Release\FamiStudio.exe TestN163.nsf famistudio-txt-export TestN163_Test.txt
..\..\bin\Release\FamiStudio.exe TestS5B.nsf famistudio-txt-export TestS5B_Test.txt
..\..\bin\Release\FamiStudio.exe TestVRC6.nsf famistudio-txt-export TestVRC6_Test.txt
..\..\bin\Release\FamiStudio.exe TestVRC7.nsf famistudio-txt-export TestVRC7_Test.txt

del /q *.nsf

fc TestBase_Test.txt TestBase_Ref.txt > nul
@if errorlevel 1 goto error
fc TestFDS_Test.txt TestFDS_Ref.txt > nul
@if errorlevel 1 goto error
fc TestMMC5_Test.txt TestMMC5_Ref.txt > nul
@if errorlevel 1 goto error
fc TestN163_Test.txt TestN163_Ref.txt > nul
@if errorlevel 1 goto error
fc TestS5B_Test.txt TestS5B_Ref.txt > nul
@if errorlevel 1 goto error
fc TestVRC6_Test.txt TestVRC6_Ref.txt > nul
@if errorlevel 1 goto error
fc TestVRC7_Test.txt TestVRC7_Ref.txt > nul
@if errorlevel 1 goto error

del /q *_Test.txt

echo NSF unit tests passed!
goto done

:error
echo NSF unit tests failed!

:done
