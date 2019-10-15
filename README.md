# FamiStudio NES Music Editor
FamiStudio is very simple NES music editor. It is designed to be easier to use than FamiTracker, but its feature set is much more limited.

If you are simply interested in installing FamiStudio, please visit the brand new [www.famistudio.org](http://famistudio.org/index.html).

## Features
- Modern DAW-style UI with sequencer and piano roll, no hexadecimal anywhere
- Full Undo/Redo support
- Export to various formats (WAV, NSF, FamiTone2, FamiTracker)
- Import from FamiTracker TXT
- Basic MIDI input support 
- Low CPU usage (Direct2D for graphics, XAudio2 for audio, fairly well threaded)

## Download
All releases are available in the [Releases](https://github.com/BleuBleu/FamiStudio/releases) section. If you are on Windows, simply download and run the .MSI installer. A shortcut to FamiStudio will be placed in your Start menu.

### [Version 1.2.1](https://github.com/BleuBleu/FamiStudio/releases/tag/1.2.1)
- Windows 7 hotfix

### [Version 1.2.0](https://github.com/BleuBleu/FamiStudio/releases/tag/1.2.0)
- macOS support 
- Extended note range to 8 octaves
- Volume tracks support
- Release envelopes & release notes
- Hi-DPI support (100%, 150% and 200% scaling on Windows, Retina on macOS)
- Config dialog
- MIDI improvements (device selection and note release)
- Improved NSF memory usage
- Misc bug fixes

### [Version 1.1.0](https://github.com/BleuBleu/FamiStudio/releases/tag/1.1.0)
- New export dialog
- Added WAV export
- Added FamiTracker Text export (see the [wiki](https://github.com/BleuBleu/FamiStudio/wiki) for limitations)
- Added NSF export (see the [wiki](https://github.com/BleuBleu/FamiStudio/wiki) for limitations)
- Bug fixes and code refactoring.

### [Version 1.0.0](https://github.com/BleuBleu/FamiStudio/releases/tag/1.0.0)
- Initial release

## Tutorial
There is a 10 minutes tutorial on YouTube:

[![Tutorial](https://github.com/BleuBleu/FamiStudio/blob/master/Wiki/Video.png)](https://www.youtube.com/watch?v=_unlyRlsbcM)

## Limitations / Known Issues 
- No PAL support
- No copy-paste support (other than pattern instancing)
- Very few effects are supported

## Demo Songs
A few demo songs are included in the installation folder (typically _C:\Program Files (x86)\FamiStudio_):
- After The Rain (by Shiru, included with FamiTone2)
- Danger Streets (by Shiru, included with FamiTone2)
- Mega Man 2 - Stage Select (my approximate recreation)
- Journey To Silius - Intro (my approximate recreation)
- DuckTales! - The Moon (my approximate recreation)
- Castlevania 2 - BLoody Tears (my approximate recreation)

## Getting Started
Please check out the [Wiki](https://github.com/BleuBleu/FamiStudio/wiki) for instructions on how to use FamiStudio.

## Compiling
The solution/projects are in VS2017 on Windows and VS2019 on Mac:
- The main application is written in C#.
- You will need C++ support installed if you plan to edit the DLL wrapper around Nes_Snd_Emu.
- On Windows, the Setup project is built using the "Microsoft Visual Studio Installer Projects" extension which can be installed from Visual Studio in the "Extensions and Updates" menu.

The C# application is built on top of SharpDX 4.2.0 on Windows and OpenTK on Mac. Visual Studio will install the required packages automatically when building the project.
To manually fetch the packages, run `msbuild /t:Restore` on the project from the Visual Studio Developer Command Prompt.

## Issues and Contributing
Please open issues contact me if you find bugs or have feature suggestion ideas. 
You can find me:
- On the [NESDEV Forums](https://forums.nesdev.com/) as BleuBleu 
- On twitter [@NesBleuBleu](http://www.twitter.com/nesbleubleu)

## Acknowledgments
- [Shiru](https://shiru.untergrund.net/code.shtml) for the FamiTone2 library and the demo songs that are included (_After the Rain_ and _Danger Streets_)
- [Blargg](http://www.slack.net/~ant/) for Nes_Snd_Emu and the underlying Blip_Buffer

