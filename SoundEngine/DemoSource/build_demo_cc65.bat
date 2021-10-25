
echo FAMISTUDIO_DEMO_USE_C = 1 > demo_ca65.inc
..\..\Tools\cc65 -t nes demo_cc65.c -Oisr --add-source
..\..\Tools\ca65 demo_cc65.s -g -I ..\..\Tools\asminc -o demo_cc65.o
..\..\Tools\ca65 demo_ca65.s -g -I ..\..\Tools\asminc -o demo_ca65.o
..\..\Tools\ld65 -C demo_ca65.cfg -o demo_cc65.nes demo_ca65.o demo_cc65.o ..\..\Tools\lib\nes.lib --mapfile demo_cc65.map --dbgfile demo_cc65.dbg
copy demo_cc65.nes ..\
del demo_cc65.nes *.o

echo ; This file intentionally empty. When building the C demo, this sets FAMISTUDIO_DEMO_USE_C = 1 > demo_ca65.inc
