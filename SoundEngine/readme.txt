FAMISTUDIO SOUND ENGINE
=======================

This is the FamiStudio sound engine. It is used by the NSF and ROM exporter of FamiStudio and can be used to make 
games. It supports every feature from FamiStudio, some of them are toggeable to save CPU/memory.

This is essentially a heavily modified version of FamiTone2 by Shiru. A lot of his code and comments are still
present here, so massive thanks to him!! I am not trying to steal his work or anything, i renamed a lot of functions
and variables because at some point it was becoming a mess of coding standards and getting hard to maintain.

Moderately advanced users can probably figure out how to use the sound engine simply by reading the comments
that are in the code files.

For more in-depth documentation, please visit:
https://famistudio.org/doc/soundengine/

CODE
====

The sound engine is contained in a single file which can be simply included in one of your assembly file, like it 
is done in the demo. 

* CA65:   famistudio_ca65.s
* NESASM: famistudio_nesasm.asm
* ASM6:   famistudio_asm6.asm

For C coding with CC65, there is a provided header file which can be used along with the normal ca65 include.
See the documentation and demo for more information on how to include it.

DEMO
====

A small demo is included with the engine sound code. The demo is available for all 3 major assemblers and they will 
all generate binary identical ROMs.

The source code for the demo is located in the \DemoSource subfolder.

* CC65:   DemoSource\demo_cc65.c
* CA65:   DemoSource\demo_ca65.s
* NESASM: DemoSource\demo_nesasm.asm
* ASM6:   DemoSource\demo_asm6.asm

To build the demo, run the appropriate batch file build_demo_xxx.bat, but make sure to make it point to the 
executable of the assembler. 

EXPORTING MUSIC/SFX DATA
========================

You can export music or sound effect data to the engine by using either the FamiStudio export dialog
or from the command line.


