g++ -fPIC -O2 -shared -I. -DLINUX -static-libgcc -static-libstdc++ DllWrapper.c -o libStb.so
cp libStb.so ../../FamiStudio/
