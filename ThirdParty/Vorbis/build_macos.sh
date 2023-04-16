gcc -dynamiclib -I. -O2 -target x86_64-apple-macos11 -Wno-unused-value -Wno-deprecated -Wno-ignored-attributes -Wno-constant-conversion DllWrapper.c -Lvorbis/MacOS -lvorbis -lvorbisenc -lvorbisfile -Logg/MacOS -logg -o Vorbis_x86_64.dylib
gcc -dynamiclib -I. -O2 -target arm64-apple-macos11 -Wno-unused-value -Wno-deprecated -Wno-ignored-attributes -Wno-constant-conversion DllWrapper.c -Lvorbis/MacOS -lvorbis -lvorbisenc -lvorbisfile -Logg/MacOS -logg -o Vorbis_arm64.dylib
lipo -create -output Vorbis.dylib Vorbis_x86_64.dylib Vorbis_arm64.dylib
cp Vorbis.dylib ../../FamiStudio/
cp Vorbis.dylib ../../Setup/FamiStudio.app/Contents/MacOS/
