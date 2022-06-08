gcc -fPIC -O2 -shared -I. -DLINUX -static-libgcc -static-libstdc++ DllWrapper.c gifdec.c -o libGifDec.so
cp libGifDec.so ../../FamiStudio/

