
echo FAMISTUDIO_DEMO_USE_C = 1 > demo_ca65.inc
..\..\..\NES\tools\bin\cc65 -t nes demo_cc65.c -Oisr --add-source
..\..\..\NES\tools\bin\ca65 demo_cc65.s -g -o demo_cc65.o
..\..\..\NES\tools\bin\ca65 demo_ca65.s -g -o demo_ca65.o
..\..\..\NES\tools\bin\ld65 -C demo_ca65.cfg -o demo_cc65.nes demo_ca65.o demo_cc65.o nes.lib --mapfile demo_cc65.map --dbgfile demo_cc65.dbg
copy demo_cc65.nes ..\
del demo_cc65.nes *.o

echo ; This file intentionally empty. When building the C demo, this sets FAMISTUDIO_DEMO_USE_C = 1 > demo_ca65.inc
