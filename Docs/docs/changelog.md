# Change Log

Version history / release notes for each release.

## Version 2.2.0

Changes:

* Recording mode to record note-by-note using MIDI controller or QWERTY keyboard
* Arpeggio support
* Official FamiStudio Sound Engine release
* Follow mode
* Displaying the piano roll view range in the sequencer
* FDS disk export
* Basic tutorials for first time users
* Error logging when importing/exporting files
* Wav export loop count
* Option to disable dragging sounds when a song is playing

## Version 2.1.1 (Hotfix)

Changes:

* Fixed first frame not being audible when pressing play
* Fixed multiple crashes & issues when exporting to NSF
* Fixed missing notes on noise channel when importing NSFs
* Fixed import of FamiTracker FTM files that uses expansion audio
* Fixed incorrect DPCM samples alignment when exporting NSF, ROM & FamiTone2
* Renaming .so files on Linux to support older version of Mono
* Setting correct file extension when saving on Linux
* FamiTone2 sound effect export fixes

## Version 2.1.0

New features:

* First Linux release
* Trackpad controls
* Note snapping in the piano roll
* New time format (MM:SS:mmm)
* Command-line interface
* Import of instruments from any supported format
* Duplication of patterns in the sequencer
* Mix paste 
* Improved WAV export
* FamiTone2 sound effect export support
* Support for pasting text values in envelopes (comma, semicolon, space, tab or newline seperated)
* PAL native authoring for FamiStudio tempo
* MacOS+Linux MIDI keyboard support
* Shift+Space plays from the beginning of the song
* Alt+Right click up/down as an alternate zooming method

Breaking/behavior changes:

* There was a bug in 2.0.x where release notes would abrutly interrupt slide notes (the NSF was doing it correctly). This is no longer the case.

<div style="position:relative;margin-left: auto;margin-right: auto;width:80%;height:0;padding-bottom:45%;">
	<iframe style="position:absolute;top:0;left:0;width:100%;height:100%" src="https://www.youtube.com/embed/r3oHXZ3MhyA" frameborder="0" allow="accelerometer; autoplay; encrypted-media; gyroscope; picture-in-picture" allowfullscreen></iframe>
</div>
<br/>

## Version 2.0.3 (Hotfix)

New features:

* Fix crash when clicking on a note when a incompatible instrument is selected.
* Fix WAV file export when using a sample rate different than 44100Hz.

## Version 2.0.2 (Hotfix)

New features:

* Fixing seek bar position in sequencer on Hi-DPI (Retina on MacOS or scaling > 100% on Windows).
* Fixing crash when importing FTM containing multiple songs with the same name.
* Fixing missing or out-of-order songs when exporting to FamiTone2.
* Interpreting zero-velocity MIDI note on events as note off as per specification.
* Highlighting notes being dragged on the piano roll.
* Detection of audio device changes on Windows 7.
* Handling of D3D device lost to improve stability on Windows 7.

## Version 2.0.1 (Hotfix)

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
<br/>

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
<br/>

## Version 1.3.0

New features:

* Selection, cut, copy & paste support for piano roll and sequencer
* FamiTracker instrument file import (thanks @Tgamemaker!)
* More helpful tooltips
* Bug fixes and improvements

<div style="position:relative;margin-left: auto;margin-right: auto;width:80%;height:0;padding-bottom:45%;">
	<iframe style="position:absolute;top:0;left:0;width:100%;height:100%" src="https://www.youtube.com/embed/7rYloart1wI" frameborder="0" allow="accelerometer; autoplay; encrypted-media; gyroscope; picture-in-picture" allowfullscreen></iframe>
</div>
<br/>

## Version 1.2.1 (Hotfix)

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
<br/>

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
<br/>

## Version 1.0.0

Initial release.
