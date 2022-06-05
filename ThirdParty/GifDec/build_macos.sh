gcc -dynamiclib -I. -O2 -Wno-unused-value -Wno-deprecated -Wno-ignored-attributes -Wno-constant-conversion DllWrapper.c gifdec.c -o GifDec.dylib
cp GifDec.dylib ../../FamiStudio/
cp GifDec.dylib ../../Setup/FamiStudio.app/Contents/MacOS/
