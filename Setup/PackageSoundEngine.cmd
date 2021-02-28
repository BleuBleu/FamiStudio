set /p Version=<Version.txt

tar -a -c -f FamiStudio%Version%-SoundEngine.zip -C ..\SoundEngine\ *.asm *.nes *.s *.txt "DemoSource\*.asm" "DemoSource\*.s" "DemoSource\*.cfg" "DemoSource\*.chr" "DemoSource\*.dmc" "DemoSource\*.pal" "DemoSource\*.rle" "DemoSource\build*.bat" "DemoSource\export_from_famistudio.bat"
