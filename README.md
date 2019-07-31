# FamiStudio NES Music Editor
FamiStudio is a very simple NES Music editor. It is designed to match the feature set of [FamiTone2](https://shiru.untergrund.net/code.shtml "FamiTone2"), a popular audio library among NES homebrew developers.

This could serve as a basis to extend to add support for modified/unofficial FamiTone versions (3 and 4) or to add support for more effects, or audio expansion chips. I have no intention of doing such a thing but you are welcome to. I want to keep this reference version simple and on-par with FamiTone2.

Features:
- Modern DAW-style UI with sequencer and piano roll
- Full Undo/Redo support
- Native export to FamiTone2 music format
- Famitracker text import
- Basic MIDI input support
- Low CPU usage (Direct2D for graphics, XAudio2 for audio, fairly well threaded)
- Runs on top of Blargg's [Nes_Snd_Emu](http://www.slack.net/~ant/libs/audio.html#Nes_Snd_Emu "Nes_Snd_Emu").

Limitations/Known Issues:
- No High-DPI support, any scaling larger than 100% will make the application look blurry
- MIDI support is barebone, notes are never stopped for example
- FamiTone2 export has not been extensively tested
- The UI code is terrible

## Releases
Version 1.0 is available for download in the Releases section.
## Getting Started
The solution/projects are in VS2017:
- The main application is written in C#.
- You will need C++ support installed if you plan to edit the DLL wrapper around Nes_Snd_Emu.
- The Setup project is built using the "Microsoft Visual Studio Installer Projects" extension which can be installed from Visual Studio in the "Extensions and Updates" menu.

The C# application is built on top of SharpDX 4.2.0. It should install the required packages automatically, but in case it does not, simply open the Package Manager and type in the following:
```
Install-Package SharpDX -Version 4.2.0
Install-Package SharpDX.Direct3D11 -Version 4.2.0
Install-Package SharpDX.Direct2D1 -Version 4.2.0
Install-Package SharpDX.DXGI -Version 4.2.0
Install-Package SharpDX.XAudio2 -Version 4.2.0
```
## Issues and Contributing
Please open issues if you find bugs or have feature suggestion ideas. 
You can contact me:
- On the [NESDEV Forums](https://forums.nesdev.com/) as BleuBleu 
- On twitter [@NesBleuBleu](http://www.twitter.com/nesbleubleu)

## Acknowledgments
- [Shiru](https://shiru.untergrund.net/code.shtml) for the FamiTone2 library and the demo songs that are included (_After the Rain_ and _Danger Streets_)
- [Blargg](http://www.slack.net/~ant/) for Nes_Snd_Emu and the underlying Blip_Buffer

