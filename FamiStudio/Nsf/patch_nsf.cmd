PatchBin EnvTest.nsf nsf_ft2_fs_mmc5.bin 128 EnvTest_patched.nsf
copy /y nsf_ft2_fs_mmc5.dbg EnvTest_patched.dbg
..\..\..\NES\tools\bin\Mesen.exe EnvTest_patched.nsf
