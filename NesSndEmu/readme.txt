Nes_Snd_Emu 0.1.7: NES Sound Emulator
-------------------------------------
This is a portable Nintendo Entertainment System (NES) 2A03 APU sound chip
emulator library for use in a NES emulator. Its main features are high
accuracy, sound quality, and efficiency. Also included are Namco 106 and Konami
VRC6 expansion sound chip emulators.

Licensed under the GNU Lesser General Public License (LGPL); see LGPL.TXT.
Copyright (C) 2003-2005 Shay Green.

Website: http://www.slack.net/~ant/libs/
Forum  : http://groups-beta.google.com/group/blargg-sound-libs
Contact: hotpop.com@blargg (swap to e-mail)


Getting Started
---------------
Build a program consisting of demo.cpp, Simple_Apu.cpp, and all source files in
the nes_apu/ directory. Running the program should generate a WAVE sound file
"out.wav" of random tones.

See notes.txt for more information, and respective header (.h) files for
reference. Visit the discussion forum to get assistance.


Files
-----
notes.txt                  General notes about the library
changes.txt                Changes since previous releases
LGPL.TXT                   GNU Lesser General Public License

usage.txt                  Two ways of using Nes_Apu in a NES emulator
demo.cpp                   Shows how to use Simple_Apu in an emulator
Simple_Apu.h               Simpler interface for APU for getting started
Simple_Apu.cpp
Wave_Writer.hpp            WAVE sound file writer used for demo output
Wave_Writer.cpp
Sound_Queue.h              Synchronous sound queue for use with SDL sound
Sound_Queue.cpp

nes_apu/                   Core library modules
  Nes_Apu.h                NES APU emulator
  Nes_Apu.cpp
  Nes_Oscs.h
  Nes_Oscs.cpp
  blargg_common.h          Common services
  blargg_source.h
  Blip_Synth.h             Sound synthesis buffer
  Blip_Buffer.h
  Blip_Buffer.cpp
                           Optional modules
  apu_snapshot.cpp         Snapshot support
  apu_snapshot.h
  Nes_Vrc6.h               Konami VRC6 sound chip emulator
  Nes_Vrc6.cpp
  Nes_Namco.h              Namco 106 sound chip emulator
  Nes_Namco.cpp
  Nonlinear_Buffer.h       Sample buffer that emulates APU's non-linearity
  Nonlinear_Buffer.cpp
  Multi_Buffer.h
  Multi_Buffer.cpp

boost/                     Substitute for boost library if it's unavailable

-- 
Shay Green <hotpop.com@blargg> (swap to e-mail)
