..\bin\Release\net7.0\FamiStudio.exe TestBase.fms unit-test TestBase_FamiStudioTest.txt
..\bin\Release\net7.0\FamiStudio.exe TestFDS.fms unit-test TestFDS_FamiStudioTest.txt
..\bin\Release\net7.0\FamiStudio.exe TestMMC5.fms unit-test TestMMC5_FamiStudioTest.txt
..\bin\Release\net7.0\FamiStudio.exe TestN163.fms unit-test TestN163_FamiStudioTest.txt
..\bin\Release\net7.0\FamiStudio.exe TestS5B.fms unit-test TestS5B_FamiStudioTest.txt
..\bin\Release\net7.0\FamiStudio.exe TestVRC6.fms unit-test TestVRC6_FamiStudioTest.txt
..\bin\Release\net7.0\FamiStudio.exe TestVRC7.fms unit-test TestVRC7_FamiStudioTest.txt
..\bin\Release\net7.0\FamiStudio.exe TestEPSM.fms unit-test TestEPSM_FamiStudioTest.txt
..\bin\Release\net7.0\FamiStudio.exe TestMulti.fms unit-test TestMulti_FamiStudioTest.txt
..\bin\Release\net7.0\FamiStudio.exe TestFamiTrackerTempo.fms unit-test TestFamiTrackerTempo_FamiStudioTest.txt

fc TestBase_FamiStudioTest.txt TestBase_FamiStudioRef.txt > nul
@if errorlevel 1 goto error
fc TestFDS_FamiStudioTest.txt TestFDS_FamiStudioRef.txt > nul
@if errorlevel 1 goto error
fc TestMMC5_FamiStudioTest.txt TestMMC5_FamiStudioRef.txt > nul
@if errorlevel 1 goto error
fc TestN163_FamiStudioTest.txt TestN163_FamiStudioRef.txt > nul
@if errorlevel 1 goto error
fc TestS5B_FamiStudioTest.txt TestS5B_FamiStudioRef.txt > nul
@if errorlevel 1 goto error
fc TestVRC6_FamiStudioTest.txt TestVRC6_FamiStudioRef.txt > nul
@if errorlevel 1 goto error
fc TestVRC7_FamiStudioTest.txt TestVRC7_FamiStudioRef.txt > nul
@if errorlevel 1 goto error
fc TestEPSM_FamiStudioTest.txt TestEPSM_FamiStudioRef.txt > nul
@if errorlevel 1 goto error
fc TestMulti_FamiStudioTest.txt TestMulti_FamiStudioRef.txt > nul
@if errorlevel 1 goto error
fc TestFamiTrackerTempo_FamiStudioTest.txt TestFamiTrackerTempo_FamiStudioRef.txt > nul
@if errorlevel 1 goto error

del /q *_FamiStudioTest.txt

echo FamiStudio unit tests passed!
goto done

:error
echo FamiStudio unit tests failed!

:done
