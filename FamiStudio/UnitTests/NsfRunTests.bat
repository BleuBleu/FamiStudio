..\bin\Release\FamiStudio.exe TestBase.fms nsf-export TestBase.nsf
..\bin\Release\FamiStudio.exe TestFDS.fms nsf-export TestFDS.nsf
..\bin\Release\FamiStudio.exe TestMMC5.fms nsf-export TestMMC5.nsf
..\bin\Release\FamiStudio.exe TestN163.fms nsf-export TestN163.nsf
..\bin\Release\FamiStudio.exe TestS5B.fms nsf-export TestS5B.nsf
..\bin\Release\FamiStudio.exe TestVRC6.fms nsf-export TestVRC6.nsf
..\bin\Release\FamiStudio.exe TestVRC7.fms nsf-export TestVRC7.nsf
..\bin\Release\FamiStudio.exe TestMulti.fms nsf-export TestMulti.nsf
..\bin\Release\FamiStudio.exe TestFamiTrackerTempo.fms nsf-export TestFamiTrackerTempo.nsf
..\bin\Release\FamiStudio.exe TestEPSM.fms nsf-export TestEPSM.nsf

..\bin\Release\FamiStudio.exe TestBase.nsf famistudio-txt-export TestBase_NsfTest.txt -nsf-import-pattern-length:160 -famistudio-txt-noversion
..\bin\Release\FamiStudio.exe TestFDS.nsf famistudio-txt-export TestFDS_NsfTest.txt -nsf-import-pattern-length:160 -famistudio-txt-noversion
..\bin\Release\FamiStudio.exe TestMMC5.nsf famistudio-txt-export TestMMC5_NsfTest.txt -nsf-import-pattern-length:160 -famistudio-txt-noversion
..\bin\Release\FamiStudio.exe TestN163.nsf famistudio-txt-export TestN163_NsfTest.txt -nsf-import-pattern-length:160 -famistudio-txt-noversion
..\bin\Release\FamiStudio.exe TestS5B.nsf famistudio-txt-export TestS5B_NsfTest.txt -nsf-import-pattern-length:160 -famistudio-txt-noversion
..\bin\Release\FamiStudio.exe TestVRC6.nsf famistudio-txt-export TestVRC6_NsfTest.txt -nsf-import-pattern-length:160 -famistudio-txt-noversion
..\bin\Release\FamiStudio.exe TestVRC7.nsf famistudio-txt-export TestVRC7_NsfTest.txt -nsf-import-pattern-length:160 -famistudio-txt-noversion
..\bin\Release\FamiStudio.exe TestMulti.nsf famistudio-txt-export TestMulti_NsfTest.txt -nsf-import-pattern-length:160 -nsf-import-duration:250 -famistudio-txt-noversion
..\bin\Release\FamiStudio.exe TestFamiTrackerTempo.nsf famistudio-txt-export TestFamiTrackerTempo_NsfTest.txt -nsf-import-pattern-length:160 -famistudio-txt-noversion
..\bin\Release\FamiStudio.exe TestEPSM.nsf unit-test-epsm TestEPSM_NsfTest.txt

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
fc TestMulti_NsfTest.txt TestMulti_NsfRef.txt > nul
@if errorlevel 1 goto error
fc TestFamiTrackerTempo_NsfTest.txt TestFamiTrackerTempo_NsfRef.txt > nul
@if errorlevel 1 goto error
fc TestEPSM_NsfTest.txt TestEPSM_NsfRef.txt > nul
@if errorlevel 1 goto error

del /q *_NsfTest.txt
del /q *.nsf

echo NSF unit tests passed!
goto done

:error
echo NSF unit tests failed!

:done
