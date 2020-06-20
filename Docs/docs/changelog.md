# Change Log

Version history / release notes for each release.

## Version 2.0.3

New features:

* Fix crash when clicking on a note when a incompatible instrument is selected.
* Fix WAV file export when using a sample rate different than 44100Hz.

## Version 2.0.2

New features:

* Fixing seek bar position in sequencer on Hi-DPI (Retina on MacOS or scaling > 100% on Windows).
* Fixing crash when importing FTM containing multiple songs with the same name.
* Fixing missing or out-of-order songs when exporting to FamiTone2.
* Interpreting zero-velocity MIDI note on events as note off as per specification.
* Highlighting notes being dragged on the piano roll.
* Detection of audio device changes on Windows 7.
* Handling of D3D device lost to improve stability on Windows 7.

## Version 2.0.1

New features:

* Hotfix for 150% scaling crash.

## Version 2.0.0

New features:

* More audio expansions: FDS, MMC5, VRC7, Namco 163 & Sunsoft S5B.
* Real-time expansion instrument parameters edition
* NSF import
* Note drag & drop with audio preview
* FamiTracker FTM import
* ROM export
* Fine pitch effect track
* New tempo mode
* PAL support
* FamiStudio text import/export
* Custom pattern settings and loop point

Breaking/behavior changes:

* VRC6 saw is no longer affected by duty. The 0 to 15 volume range will map to the entire 0 to 31 possible range. Songs using VRC6 created prior to 2.0.0 will have to cut the volume in half to sound the same (either using volume track or instrument envelope).
* Notes without attack will remain without attack until a new note with attack is reached (even after a stop note).

<div style="position:relative;margin-left: auto;margin-right: auto;width:80%;height:0;padding-bottom:45%;">
	<iframe style="position:absolute;top:0;left:0;width:100%;height:100%" src="https://www.youtube.com/embed/QRn_ymIdUp8" frameborder="0" allow="accelerometer; autoplay; encrypted-media; gyroscope; picture-in-picture" allowfullscreen></iframe>
</div>

## Version 1.4.0

New features:

* VRC6 audio expansion support
* Slide notes
* Vibrato effect
* Workaround "pops" on square channels (Blargg's smooth vibrato technique)
* NSF improvements
* Ability to remove attack on notes
* Relative pitch envelopes

Breaking/behavior changes:

* Jumps and skips will now be applied at the END of the frame, as opposed to the beginning. This is how FamiTracker and the rest of the world does it.

<div style="position:relative;margin-left: auto;margin-right: auto;width:80%;height:0;padding-bottom:45%;">
	<iframe style="position:absolute;top:0;left:0;width:100%;height:100%" src="https://www.youtube.com/embed/Ox_D0Z_H2NY" frameborder="0" allow="accelerometer; autoplay; encrypted-media; gyroscope; picture-in-picture" allowfullscreen></iframe>
</div>

## Version 1.3.0

New features:

* Selection, cut, copy & paste support for piano roll and sequencer
* FamiTracker instrument file import (thanks @Tgamemaker!)
* More helpful tooltips
* Bug fixes and improvements

<div style="position:relative;margin-left: auto;margin-right: auto;width:80%;height:0;padding-bottom:45%;">
	<iframe style="position:absolute;top:0;left:0;width:100%;height:100%" src="https://www.youtube.com/embed/7rYloart1wI" frameborder="0" allow="accelerometer; autoplay; encrypted-media; gyroscope; picture-in-picture" allowfullscreen></iframe>
</div>

## Version 1.2.1

New features:

* Windows 7 hotfix

## Version 1.2.0

New features:

* macOS support
* Extended note range to 8 octaves
* Volume tracks support
* Release envelopes & release notes
* Hi-DPI support (100%, 150% and 200% scaling on Windows, Retina on macOS)
* Config dialog
* MIDI improvements (device selection and note release)
* Improved NSF memory usage
* Misc bug fixes

<div style="position:relative;margin-left: auto;margin-right: auto;width:80%;height:0;padding-bottom:45%;">
	<iframe style="position:absolute;top:0;left:0;width:100%;height:100%" src="https://www.youtube.com/embed/o8VI4vKZtXY" frameborder="0" allow="accelerometer; autoplay; encrypted-media; gyroscope; picture-in-picture" allowfullscreen></iframe>
</div>

## Version 1.1.0

New features:

* New export dialog
* Added WAV export
* Added FamiTracker Text export (see the wiki for limitations)
* Added NSF export (see the wiki for limitations)
* Bug fixes and code refactoring.

<div style="position:relative;margin-left: auto;margin-right: auto;width:80%;height:0;padding-bottom:45%;">
	<iframe style="position:absolute;top:0;left:0;width:100%;height:100%" src="https://www.youtube.com/embed/kpQmQ-PRlaY" frameborder="0" allow="accelerometer; autoplay; encrypted-media; gyroscope; picture-in-picture" allowfullscreen></iframe>
</div>

## Version 1.0.0

Initial release.
