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

..\bin\Release\net7.0\FamiStudio.exe TestBase.nsf famistudio-txt-export TestBase_NsfRef.txt -nsf-import-pattern-length:160 -famistudio-txt-bare
..\bin\Release\net7.0\FamiStudio.exe TestFDS.nsf famistudio-txt-export TestFDS_NsfRef.txt -nsf-import-pattern-length:160 -famistudio-txt-bare
..\bin\Release\net7.0\FamiStudio.exe TestMMC5.nsf famistudio-txt-export TestMMC5_NsfRef.txt -nsf-import-pattern-length:160 -famistudio-txt-bare
..\bin\Release\net7.0\FamiStudio.exe TestN163.nsf famistudio-txt-export TestN163_NsfRef.txt -nsf-import-pattern-length:160 -famistudio-txt-bare
..\bin\Release\net7.0\FamiStudio.exe TestS5B.nsf famistudio-txt-export TestS5B_NsfRef.txt -nsf-import-pattern-length:160 -famistudio-txt-bare
..\bin\Release\net7.0\FamiStudio.exe TestVRC6.nsf famistudio-txt-export TestVRC6_NsfRef.txt -nsf-import-pattern-length:160 -famistudio-txt-bare
..\bin\Release\net7.0\FamiStudio.exe TestVRC7.nsf famistudio-txt-export TestVRC7_NsfRef.txt -nsf-import-pattern-length:160 -famistudio-txt-bare
..\bin\Release\net7.0\FamiStudio.exe TestFamiTrackerTempo.nsf famistudio-txt-export TestFamiTrackerTempo_NsfRef.txt -nsf-import-pattern-length:160 -famistudio-txt-bare
..\bin\Release\net7.0\FamiStudio.exe TestEPSM.nsf famistudio-txt-export TestEPSM_NsfRef.txt -nsf-import-pattern-length:160 -nsf-import-duration:170 -famistudio-txt-bare
..\bin\Release\net7.0\FamiStudio.exe TestMulti.nsf famistudio-txt-export TestMulti_NsfRef.txt -nsf-import-pattern-length:160 -nsf-import-duration:260 -famistudio-txt-bare
..\bin\Release\net7.0\FamiStudio.exe TestMultiEPSM.nsf famistudio-txt-export TestMultiEPSM_NsfRef.txt -nsf-import-pattern-length:160 -nsf-import-duration:380 -famistudio-txt-bare

del /q *_NsfTest.txt
del /q *.nsf
