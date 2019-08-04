# FamiStudio NES Music Editor
FamiStudio is a very simple NES Music editor. It is intended to be more user-friendly than FamiTracker. 

Its feature set is designed to match [FamiTone2](https://shiru.untergrund.net/code.shtml "FamiTone2"), a popular audio library among NES homebrew developers.

While I intend to keep this reference version simple and on-par with FamiTone2, it could easily be extended to support modified/unofficial FamiTone versions (3 and 4, by nesdoug for example), add support for more effects, or audio expansion chips. 

## Tutorial

[![Tutorial](https://github.com/BleuBleu/FamiStudio/blob/master/Wiki/Video.png)](https://www.youtube.com/watch?v=_unlyRlsbcM)

## Features
- Modern DAW-style UI with sequencer and piano roll, no hexadecimal anywhere
- Full Undo/Redo support
- Native export to FamiTone2 music format
- Famitracker text import
- Basic MIDI input support 
- Low CPU usage (Direct2D for graphics, XAudio2 for audio, fairly well threaded)
- Runs on top of Blargg's [Nes_Snd_Emu](http://www.slack.net/~ant/libs/audio.html#Nes_Snd_Emu "Nes_Snd_Emu").

## Limitations / Known Issues 
- No High-DPI support, any scaling larger than 100% will make the application look blurry
- No PAL support (*)
- No copy-paste support (other than pattern instancing) (*)
- No FamiTone2 SFX format support (could be added, but for short SFX, Famitracker is probably a better tool)
- Lots of missing keyboard shortcuts (also no piano keyboard input) (*)
- MIDI support is barebone: it only uses device #0 and notes are never stopped (*)
- The UI code is terrible

_(*): plan to improve in the coming weeks/months_

## Releases
Version 1.0 is available for download in the Releases section.

## Demo Songs
A few demo songs are included in the installation folder (typically _C:\Program Files (x86)\FamiStudio_):
- After The Rain (by Shiru, included with FamiTone2)
- Danger Streets (by Shiru, included with FamiTone2)
- Mega Man 2 - Stage Select (my approximate recreation)
- Journey To Silius - Intro (my approximate recreation)

## Getting Started
Please check out the [Wiki](https://github.com/BleuBleu/FamiStudio/wiki) for instructions on how to use FamiStudio.

## Compiling
The solution/projects are in VS2017:
- The main application is written in C#.
- You will need C++ support installed if you plan to edit the DLL wrapper around Nes_Snd_Emu.
- The Setup project is built using the "Microsoft Visual Studio Installer Projects" extension which can be installed from Visual Studio in the "Extensions and Updates" menu.

The C# application is built on top of SharpDX 4.2.0. It should install the required packages automatically, but in case it does not, simply open the Package Manager console and type in the following:
```
Install-Package SharpDX -Version 4.2.0
Install-Package SharpDX.Direct3D11 -Version 4.2.0
Install-Package SharpDX.Direct2D1 -Version 4.2.0
Install-Package SharpDX.DXGI -Version 4.2.0
Install-Package SharpDX.XAudio2 -Version 4.2.0
```
If that fails, or if you have general issues with packages, running this in the Package Manager console often fixes issue:
```
Update-Package -reinstall
```
## Issues and Contributing
Please open issues contact me if you find bugs or have feature suggestion ideas. 
You can find me:
- On the [NESDEV Forums](https://forums.nesdev.com/) as BleuBleu 
- On twitter [@NesBleuBleu](http://www.twitter.com/nesbleubleu)

## Acknowledgments
- [Shiru](https://shiru.untergrund.net/code.shtml) for the FamiTone2 library and the demo songs that are included (_After the Rain_ and _Danger Streets_)
- [Blargg](http://www.slack.net/~ant/) for Nes_Snd_Emu and the underlying Blip_Buffer

