gcc -dynamiclib -I. -O2 -target x86_64-apple-macos11 -Wno-unused-value -Wno-deprecated -Wno-ignored-attributes -Wno-constant-conversion DllWrapper.c bitstream.c huffman.c l3bitstream.c l3loop.c l3mdct.c l3subband.c layer3.c reservoir.c tables.c -o ShineMp3_x86_64.dylib
gcc -dynamiclib -I. -O2 -target arm64-apple-macos11 -Wno-unused-value -Wno-deprecated -Wno-ignored-attributes -Wno-constant-conversion DllWrapper.c bitstream.c huffman.c l3bitstream.c l3loop.c l3mdct.c l3subband.c layer3.c reservoir.c tables.c -o ShineMp3_arm64.dylib
lipo -create -output ShineMp3.dylib ShineMp3_x86_64.dylib ShineMp3_arm64.dylib
cp ShineMp3.dylib ../../FamiStudio/
cp ShineMp3.dylib ../../Setup/FamiStudio.app/Contents/MacOS/
