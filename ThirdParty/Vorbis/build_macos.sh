gcc -dynamiclib -I. -O2 -Wno-unused-value -Wno-deprecated -Wno-ignored-attributes -Wno-constant-conversion DllWrapper.c -Lvorbis/MacOS -lvorbis -lvorbisenc -lvorbisfile -Logg/MacOS -logg -o Vorbis.dylib
cp Vorbis.dylib ../../FamiStudio/
cp Vorbis.dylib ../../Setup/FamiStudio.app/Contents/MacOS/
