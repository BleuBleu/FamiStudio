# Welcome to the FamiStudio GitHub page
This is the GitHub page of FamiStudio, which is intended for people wanting to view/download the source code or report bug fixes.

**If you are simply interested in installing FamiStudio, please visit the brand new [famistudio.org](https://famistudio.org/) or [itch.io](https://bleubleu.itch.io/famistudio).**

For any questions, you can reach me at [famistudio@outlook.com](mailto:famistudio@outlook.com).

## Compiled versions
All releases are available in the [Releases](https://github.com/BleuBleu/FamiStudio/releases) section. If you are on Windows, simply download and run the .MSI installer and a shortcut to FamiStudio will be placed in your Start menu.

## Compiling
FamiStudio is composed of a few projects:
- The main FamiStudio application is written in C#. 
- NesSndEmu, NotSoFatso, ShineMp3 & Vorbis are C++ DLLs and are provided as binaries since they rarely change. 
- The Setup project (Windows only)

### Windows
On Windows, Visual Studio 2019 is used for development. The project contains everything, including both DLLs and the Setup project. In you plan to recompile the C++ DLLs, you will need to install C++ support in Visual Studio. The Setup project is built using the "Microsoft Visual Studio Installer Projects" extension which can be installed from Visual Studio in the "Extensions and Updates" menu.

### MacOS
On MacOS, Visual Studio 2019 for Mac is used to compile the main application. The C++ DLLs can be compiled using a little shell script "build_macos.sh" that is located in the each of the folders. No fancy makefile here. [PortAudio](http://www.portaudio.com/) and [RtMidi](https://www.music.mcgill.ca/~gary/rtmidi/), which are used for audio output and MIDI input respectively have been pre-compiled using Homebrew and are likely not going to change often. The Application bundle is updated manually at each release.

### Linux.
On Linux, MonoDevelop 7.8.4 (build 2) is used to compile the main application. Much like MacOS, a little shell script, "build_linux.sh" is provided to compile each of the C++ DLLs. No makefile is provided at the moment. [OpenAL Soft](https://openal-soft.org/) is provided as a precompiled AMD64 binary with ALSA support, if you recompile your own you will need to replace libopenal32.so or use a [dllmap](https://www.mono-project.com/docs/advanced/pinvoke/dllmap/) to point FamiStudio to the correct library. Same for [RtMidi](https://www.music.mcgill.ca/~gary/rtmidi/) which is provided as librtmidi.so.

### Android
On Android, Visual Studio 2019 is used, along with the Xamarin Android assemblies. The C++ DLLs needs to be compiled manually for all 4 architectures (x86, x64, ARM, ARM64) whenever there is a change. We target a minimum version of Android 8.0 (Oreo), so make sure to test features on all versions in between. 

## Contributing
I'm hesitant to take unsolicited pull requests. If you want to contribute a feature, please get in touch with me first so we can come up with a plan. This will avoid wasting both your time and mine.

## Platforms, Feature Parity & Testing
Assuming we agree on a feature to be developed, I expect:
- All features needs to be implemented and tested on all 4 platforms (Windows, MacOS, Linux and Android). 
- Testing must include Hi-DPI scaling on Windows (150% and 200%), Retina display on MacOS, 100%/200% on Linux as these have been know to break often.
- Any feature that impacts the music needs to be integrated to all import/export format (FamiTracker, NSF, FTI, etc.)
- Any new feature needs to be added to the NSF driver (including its multi-expansion chip variant) and needs to be toggeable if it has a cost (RAM or CPU cycles).
- New features need to be integrated with any of the 3 unit tests that applies (Sound engine code similarity, NSF import/export and sound emulation tests).
- Documentation needs to be updated as well.

## Contact
Please open issues contact me if you find bugs or have feature suggestion ideas. 
You can find me:
- On the [NESDEV Forums](https://forums.nesdev.com/) as BleuBleu 
- On Twitter [@NesBleuBleu](http://www.twitter.com/nesbleubleu)
- On [YouTube](https://www.youtube.com/channel/UC-dGLo2XZqXNA_aOYjaucgA?view_as=subscriber)
- On [Itch.io](https://bleubleu.itch.io/famistudio)
- On the [FamiStudio Discord](https://discord.gg/88UPmxh)

## Acknowledgments
- [Shiru](https://shiru.untergrund.net/code.shtml) for the FamiTone2 library.
- [Blargg](http://www.slack.net/~ant/) for Nes_Snd_Emu and the underlying Blip_Buffer. Also for it's Smooth Vibrato tech.
- [RainWarrior](http://rainwarrior.ca) for NSFImport and other tools.
- [Mitsutaka Okazaki](https://github.com/okaxaki) For emu2413 and emu2149 which are used for VRC7 and Sunsoft 5B emulation.

