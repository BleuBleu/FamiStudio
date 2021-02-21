g++ -fPIC -O2 -shared -I. -DLINUX -static-libgcc -static-libstdc++ fmopl.c -Wno-narrowing NSF_Core.cpp NSF_File.cpp NSF_6502.cpp Wave_VRC7.cpp DllWrapper.cpp -o libNotSoFatso.so
cp libNotSoFatso.so ../../FamiStudio/
