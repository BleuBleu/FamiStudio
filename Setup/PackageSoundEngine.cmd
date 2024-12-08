set /p Version=<Version.txt
if not exist ..\FamiStudio%Version%-SoundEngine mkdir ..\FamiStudio%Version%-SoundEngine
robocopy ..\SoundEngine\ ..\FamiStudio%Version%-SoundEngine *.h *.asm *.nes *.s *.txt
robocopy ..\SoundEngine\DemoSource\ ..\FamiStudio%Version%-SoundEngine\DemoSource *.c *.asm *.s *.cfg *.chr *.dmc *.pal *.rle build*.bat demo_ca65.inc export_from_famistudio.bat
..\Tools\fart --remove ..\FamiStudio%Version%-SoundEngine\DemoSource\*.bat ..\..\Tools\
..\Tools\fart --remove ..\FamiStudio%Version%-SoundEngine\DemoSource\build_demo_asm6.bat ..\Tools\
del ..\FamiStudio%Version%-SoundEngine\DemoSource\*_cc65.s
del ..\FamiStudio%Version%-SoundEngine\*multi*.s
tar -a -c -f FamiStudio%Version%-SoundEngine.zip -C ..\FamiStudio%Version%-SoundEngine\ *
rmdir /S /Q ..\FamiStudio%Version%-SoundEngine
