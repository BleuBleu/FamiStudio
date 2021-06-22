gcc -fPIC -O2 -shared -I. -DLINUX -static-libgcc -static-libstdc++ DllWrapper.c -Lvorbis/Linux -lvorbis -Logg/Linux -logg -o libVorbis.so
cp libVorbis.so ../../FamiStudio/

