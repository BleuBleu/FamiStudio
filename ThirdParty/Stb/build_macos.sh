gcc -dynamiclib -I. -O2 -target x86_64-apple-macos11 -Wno-unused-value -Wno-deprecated -Wno-ignored-attributes -Wno-constant-conversion DllWrapper.c -o Stb_x86_64.dylib
gcc -dynamiclib -I. -O2 -target arm64-apple-macos11 -Wno-unused-value -Wno-deprecated -Wno-ignored-attributes -Wno-constant-conversion DllWrapper.c -o Stb_arm64.dylib
lipo -create -output Stb.dylib Stb_x86_64.dylib Stb_arm64.dylib
cp Stb.dylib ../../FamiStudio/
cp Stb.dylib ../../Setup/FamiStudio.app/Contents/MacOS/
