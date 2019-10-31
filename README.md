# Welcome to the FamiStudio GitHub page
This is the GitHub page of FamiStudio, which is intended for people wanting to view/download the source code or report bug fixes.

**If you are simply interested in installing FamiStudio, please visit the brand new [famistudio.org](https://famistudio.org/) or [itch.io](https://bleubleu.itch.io/famistudio).**

## Compiled versions
All releases are available in the [Releases](https://github.com/BleuBleu/FamiStudio/releases) section. If you are on Windows, simply download and run the .MSI installer and a shortcut to FamiStudio will be placed in your Start menu.

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

