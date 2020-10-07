gcc -fPIC -O2 -shared -I. -DLINUX -static-libgcc -static-libstdc++ DllWrapper.c bitstream.c huffman.c l3bitstream.c l3loop.c l3mdct.c l3subband.c layer3.c reservoir.c tables.c -o libShineMp3.so

