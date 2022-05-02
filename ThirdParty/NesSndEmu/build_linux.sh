g++ -fPIC -O2 -shared -I. -DLINUX -static-libgcc -static-libstdc++ DllWrapper.cpp Simple_Apu.cpp nes_apu/apu_snapshot.cpp nes_apu/Blip_Buffer.cpp nes_apu/Nes_Apu.cpp nes_apu/Nes_Namco.cpp nes_apu/Nes_Oscs.cpp nes_apu/Nes_Vrc6.cpp nes_apu/Nes_Vrc7.cpp nes_apu/Nes_Fds.cpp nes_apu/Nes_Mmc5.cpp nes_apu/Nes_Sunsoft.cpp nes_apu/Nes_Fme7.cpp nes_apu/emu2413.c nes_apu/emu2149.c nes_apu/Nes_EPSM.cpp nes_apu/ym3438.cpp -o libNesSndEmu.so
cp libNesSndEmu.so ../../FamiStudio/

