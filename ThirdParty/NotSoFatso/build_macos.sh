g++ -dynamiclib -I. -O2 -target x86_64-apple-macos11 -Wno-deprecated -Wno-ignored-attributes -Wno-comment fmopl.c NSF_Core.cpp NSF_File.cpp NSF_6502.cpp Wave_VRC7.cpp DllWrapper.cpp -o NotSoFatso_x86_64.dylib
g++ -dynamiclib -I. -O2 -target arm64-apple-macos11 -Wno-deprecated -Wno-ignored-attributes -Wno-comment fmopl.c NSF_Core.cpp NSF_File.cpp NSF_6502.cpp Wave_VRC7.cpp DllWrapper.cpp -o NotSoFatso_arm64.dylib
lipo -create -output NotSoFatso.dylib NotSoFatso_x86_64.dylib NotSoFatso_arm64.dylib
cp NotSoFatso.dylib ../../FamiStudio/
cp NotSoFatso.dylib ../../Setup/FamiStudio.app/Contents/MacOS/
