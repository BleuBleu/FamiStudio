..\bin\Release\net7.0\FamiStudio.exe TestBase.fms nsf-export TestBase.nsf
..\bin\Release\net7.0\FamiStudio.exe TestFDS.fms nsf-export TestFDS.nsf
..\bin\Release\net7.0\FamiStudio.exe TestMMC5.fms nsf-export TestMMC5.nsf
..\bin\Release\net7.0\FamiStudio.exe TestN163.fms nsf-export TestN163.nsf
..\bin\Release\net7.0\FamiStudio.exe TestS5B.fms nsf-export TestS5B.nsf
..\bin\Release\net7.0\FamiStudio.exe TestVRC6.fms nsf-export TestVRC6.nsf
..\bin\Release\net7.0\FamiStudio.exe TestVRC7.fms nsf-export TestVRC7.nsf
..\bin\Release\net7.0\FamiStudio.exe TestFamiTrackerTempo.fms nsf-export TestFamiTrackerTempo.nsf
..\bin\Release\net7.0\FamiStudio.exe TestEPSM.fms nsf-export TestEPSM.nsf
..\bin\Release\net7.0\FamiStudio.exe TestMulti.fms nsf-export TestMulti.nsf
..\bin\Release\net7.0\FamiStudio.exe TestMultiEPSM.fms nsf-export TestMultiEPSM.nsf

..\bin\Release\net7.0\FamiStudio.exe TestBase.nsf famistudio-txt-export TestBase_NsfTest.txt -nsf-import-pattern-length:160 -famistudio-txt-bare
..\bin\Release\net7.0\FamiStudio.exe TestFDS.nsf famistudio-txt-export TestFDS_NsfTest.txt -nsf-import-pattern-length:160 -famistudio-txt-bare
..\bin\Release\net7.0\FamiStudio.exe TestMMC5.nsf famistudio-txt-export TestMMC5_NsfTest.txt -nsf-import-pattern-length:160 -famistudio-txt-bare
..\bin\Release\net7.0\FamiStudio.exe TestN163.nsf famistudio-txt-export TestN163_NsfTest.txt -nsf-import-pattern-length:160 -famistudio-txt-bare
..\bin\Release\net7.0\FamiStudio.exe TestS5B.nsf famistudio-txt-export TestS5B_NsfTest.txt -nsf-import-pattern-length:160 -famistudio-txt-bare
..\bin\Release\net7.0\FamiStudio.exe TestVRC6.nsf famistudio-txt-export TestVRC6_NsfTest.txt -nsf-import-pattern-length:160 -famistudio-txt-bare
..\bin\Release\net7.0\FamiStudio.exe TestVRC7.nsf famistudio-txt-export TestVRC7_NsfTest.txt -nsf-import-pattern-length:160 -famistudio-txt-bare
..\bin\Release\net7.0\FamiStudio.exe TestFamiTrackerTempo.nsf famistudio-txt-export TestFamiTrackerTempo_NsfTest.txt -nsf-import-pattern-length:160 -famistudio-txt-bare
..\bin\Release\net7.0\FamiStudio.exe TestEPSM.nsf famistudio-txt-export TestEPSM_NsfTest.txt -nsf-import-pattern-length:160 -nsf-import-duration:170 -famistudio-txt-bare
..\bin\Release\net7.0\FamiStudio.exe TestMulti.nsf famistudio-txt-export TestMulti_NsfTest.txt -nsf-import-pattern-length:160 -nsf-import-duration:260 -famistudio-txt-bare
..\bin\Release\net7.0\FamiStudio.exe TestMultiEPSM.nsf famistudio-txt-export TestMultiEPSM_NsfTest.txt -nsf-import-pattern-length:160 -nsf-import-duration:380 -famistudio-txt-bare

fc TestBase_NsfTest.txt TestBase_NsfRef.txt > nul
@if errorlevel 1 goto error
fc TestFDS_NsfTest.txt TestFDS_NsfRef.txt > nul
@if errorlevel 1 goto error
fc TestMMC5_NsfTest.txt TestMMC5_NsfRef.txt > nul
@if errorlevel 1 goto error
fc TestN163_NsfTest.txt TestN163_NsfRef.txt > nul
@if errorlevel 1 goto error
fc TestS5B_NsfTest.txt TestS5B_NsfRef.txt > nul
@if errorlevel 1 goto error
fc TestVRC6_NsfTest.txt TestVRC6_NsfRef.txt > nul
@if errorlevel 1 goto error
fc TestVRC7_NsfTest.txt TestVRC7_NsfRef.txt > nul
@if errorlevel 1 goto error
fc TestFamiTrackerTempo_NsfTest.txt TestFamiTrackerTempo_NsfRef.txt > nul
@if errorlevel 1 goto error
fc TestEPSM_NsfTest.txt TestEPSM_NsfRef.txt > nul
@if errorlevel 1 goto error
fc TestMulti_NsfTest.txt TestMulti_NsfRef.txt > nul
@if errorlevel 1 goto error
fc TestMultiEPSM_NsfTest.txt TestMultiEPSM_NsfRef.txt > nul
@if errorlevel 1 goto error

del /q *_NsfTest.txt
del /q *.nsf

echo NSF unit tests passed!
goto done

:error
echo NSF unit tests failed!

:done
