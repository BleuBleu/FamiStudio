..\..\..\NES\tools\bin\ca65 demo_ca65.s -g -o demo_ca65.o
..\..\..\NES\tools\bin\ld65 -C demo_ca65.cfg -o demo_ca65.nes demo_ca65.o --mapfile demo_ca65.map --dbgfile demo_ca65.dbg
copy demo_ca65.nes ..\

