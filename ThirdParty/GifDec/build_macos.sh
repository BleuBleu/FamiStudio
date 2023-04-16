gcc -dynamiclib -I. -O2 -target x86_64-apple-macos11 -Wno-unused-value -Wno-deprecated -Wno-ignored-attributes -Wno-constant-conversion DllWrapper.c gifdec.c -o GifDec_x86_64.dylib
gcc -dynamiclib -I. -O2 -target arm64-apple-macos11 -Wno-unused-value -Wno-deprecated -Wno-ignored-attributes -Wno-constant-conversion DllWrapper.c gifdec.c -o GifDec_arm64.dylib
lipo -create -output GifDec.dylib GifDec_x86_64.dylib GifDec_arm64.dylib
cp GifDec.dylib ../../FamiStudio/
cp GifDec.dylib ../../Setup/FamiStudio.app/Contents/MacOS/
