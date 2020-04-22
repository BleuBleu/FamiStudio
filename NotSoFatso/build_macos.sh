g++ -dynamiclib -I. -O2 -Wno-deprecated -Wno-ignored-attributes -Wno-comment fmopl.c NSF_Core.cpp NSF_File.cpp NSF_6502.cpp Wave_VRC7.cpp DllWrapper.cpp -o NotSoFatso.dylib
		