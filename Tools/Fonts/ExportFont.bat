@echo off
copy /y QuickSand.bmfc tmp.bmfc
fart tmp.bmfc {FONTSIZE} %1
if "%2" == "Bold" (fart tmp.bmfc {BOLD} 1) else (fart tmp.bmfc {BOLD} 0)
fart tmp.bmfc {WIDTH} %3
fart tmp.bmfc {HEIGHT} %4
if "%2" == "Bold" (..\bmfont64.exe -c tmp.bmfc -o ..\..\FamiStudio\Resources\Fonts\QuickSand%1Bold.fnt) else (..\bmfont64.exe -c tmp.bmfc -o ..\..\FamiStudio\Resources\Fonts\QuickSand%1.fnt)
del tmp.bmfc

