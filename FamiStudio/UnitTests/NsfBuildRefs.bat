..\bin\Release\FamiStudio.exe TestBase.fms nsf-export TestBase.nsf
..\bin\Release\FamiStudio.exe TestFDS.fms nsf-export TestFDS.nsf
..\bin\Release\FamiStudio.exe TestMMC5.fms nsf-export TestMMC5.nsf
..\bin\Release\FamiStudio.exe TestN163.fms nsf-export TestN163.nsf
..\bin\Release\FamiStudio.exe TestS5B.fms nsf-export TestS5B.nsf
..\bin\Release\FamiStudio.exe TestVRC6.fms nsf-export TestVRC6.nsf
..\bin\Release\FamiStudio.exe TestVRC7.fms nsf-export TestVRC7.nsf
..\bin\Release\FamiStudio.exe TestFamiTrackerTempo.fms nsf-export TestFamiTrackerTempo.nsf
..\bin\Release\FamiStudio.exe TestEPSM.fms nsf-export TestEPSM.nsf
..\bin\Release\FamiStudio.exe TestMulti.fms nsf-export TestMulti.nsf
..\bin\Release\FamiStudio.exe TestMultiEPSM.fms nsf-export TestMultiEPSM.nsf

..\bin\Release\FamiStudio.exe TestBase.nsf famistudio-txt-export TestBase_NsfRef.txt -nsf-import-pattern-length:160 -famistudio-txt-noversion
..\bin\Release\FamiStudio.exe TestFDS.nsf famistudio-txt-export TestFDS_NsfRef.txt -nsf-import-pattern-length:160 -famistudio-txt-noversion
..\bin\Release\FamiStudio.exe TestMMC5.nsf famistudio-txt-export TestMMC5_NsfRef.txt -nsf-import-pattern-length:160 -famistudio-txt-noversion
..\bin\Release\FamiStudio.exe TestN163.nsf famistudio-txt-export TestN163_NsfRef.txt -nsf-import-pattern-length:160 -famistudio-txt-noversion
..\bin\Release\FamiStudio.exe TestS5B.nsf famistudio-txt-export TestS5B_NsfRef.txt -nsf-import-pattern-length:160 -famistudio-txt-noversion
..\bin\Release\FamiStudio.exe TestVRC6.nsf famistudio-txt-export TestVRC6_NsfRef.txt -nsf-import-pattern-length:160 -famistudio-txt-noversion
..\bin\Release\FamiStudio.exe TestVRC7.nsf famistudio-txt-export TestVRC7_NsfRef.txt -nsf-import-pattern-length:160 -famistudio-txt-noversion
..\bin\Release\FamiStudio.exe TestFamiTrackerTempo.nsf famistudio-txt-export TestFamiTrackerTempo_NsfRef.txt -nsf-import-pattern-length:160 -famistudio-txt-noversion
..\bin\Release\FamiStudio.exe TestEPSM.nsf unit-test-epsm TestEPSM_NsfRef.txt -epsm-num-frames:14880
..\bin\Release\FamiStudio.exe TestMulti.nsf famistudio-txt-export TestMulti_NsfRef.txt -nsf-import-pattern-length:160 -nsf-import-duration:248 -famistudio-txt-noversion
..\bin\Release\FamiStudio.exe TestMultiEPSM.nsf famistudio-txt-export TestMultiEPSM1_NsfRef.txt -nsf-import-pattern-length:160 -nsf-import-duration:360 -famistudio-txt-noversion
..\bin\Release\FamiStudio.exe TestMultiEPSM.nsf unit-test-epsm TestMultiEPSM2_NsfRef.txt -epsm-num-frames:21600

del /q *_NsfTest.txt
del /q *.nsf
