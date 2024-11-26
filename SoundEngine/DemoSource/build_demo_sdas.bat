..\..\Tools\sdas6500 -pogn -I"." -I".." -y -s -l demo_sdas.o demo_sdas.s
..\..\Tools\sdld6808 -n -i -j -y -w -u -w -b _ZP=0x0000 -b _OAM=0x0200 -b _CODE=0x8000 demo_sdas.ihx demo_sdas.o
..\..\Tools\ihxcheck demo_sdas.ihx
..\..\Tools\makebin -s 73728 -o 32752 demo_sdas.ihx demo_sdas.nes
copy demo_sdas.nes ..\

