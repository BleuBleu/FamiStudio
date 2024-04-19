# Change Log

Version history / release notes for each release. 

To download older versions or view the release dates, please visit the [Github Releases](https://github.com/BleuBleu/FamiStudio/releases) page.

## Version 4.2.0

[![](releases/420/Thumbnail420.png#center)](releases/420.md)

Changes/Fixes:

* S5B envelope support (S5B & EPSM)
* FDS auto-modulation support
* FDS emulation improvements : Proper filtering, DAC emulation & fixes.
* Phase reset support
* Accurate seek support (fully emulate entire song from start, useful for phase resets)
* Allow disabling attack on notes if all instrument envelopes matches (useful for FM channels)
* Folders support in Project Explorer
* More filtering options for audio expansions & ability to store settings in project
* Support for up to 256 instruments (for both regular and expansion) in sound engine
* German translation (thanks Arda & VRC6Lover123!)
* Audio backend improvements:
	* Reduced audio latency on all platform 
	* Switch to WASAPI on Windows (non-exclusive mode)
	* Audio device change detection & improved BT headphones support on MacOS
* Video export improvements: 
	* Unified piano roll mode
	* Piano roll 3D effect
	* Option to overlay registers
	* Preview mode
* Small quality of life improvements:
	* Eraser mode
	* Ability to copy samples between instruments
	* Ability to type effect values and project explorer parameters
	* Function to replace specific instruments
	* Logarithmic sliders for effects and parameters with huge values

Breaking/Behavior changes:

* FDS mod speed/depth effects will be resetted to the instrument values on notes with an attack.
* FDS emulation now matches the hardware much more closely. Some instruments may now sound very different.
* S5B/EPSM noise no longer has a "NOP" frequency, noise frequency will be set if it is enabled by the mixer envelope. 

System Requirement Changes:

* Upgraded to .NET 7.0
* Minimum OpenGL requirement for Desktop lowered to OpenGL 3.0 (was 3.3)

## Version 4.1.3 (Hotfix)

Changes/Fixes:

* Fixed "pop" when opening N163 projects or playing N163 songs
* Fixed colors becoming progressively washed out when performing specific actions on Android
* Fixed overwrite confirmation dialog not appearing if you type the exact same name of an existing project on Android
* Fixed issue where envelope editor would let you set releases on FDS waveform  
* Fixed "Unassign unused DPCM instrument keys" cleanup option not working if another DPCM instrument was using the same piano key
* Fixed bug introduced in 4.1.2 where notes were replaying their attack if a gap was left between the notes
* Fixed issue where note attacks were re-triggers unconditionally when crossing the loop point when exporting to NSF/ROM/SoundEngine
* Fixed loophole where you could disable note attacks on the DPCM channel by duplicating patterns
* Fixed DPCM samples not importing correctly from FamiTracker TXT files
* Fixed issue where arpeggios imported from FamiTracker files would not play sometimes
* Fixed issue when exporting N163 songs to NSF/ROM/FDS/SoundEngine with instrument names only differentiated by special characters
* Fixed QWERTY piano input getting stuck if pressing another key while holding a key
* Names that are too long to fit in the project explorer are now truncated with ellipsis (...) on Desktop (was already the case on Mobile)
* Upgraded to Android API level 33 (Android 13.0)

## Version 4.1.2 (Hotfix)

Changes/Fixes:

* Fixed instrument context menu disappearing when using a left-click
* Fixed "Follow Mode" jittering when using small % values
* Fixed out of range volumes when importing .fti containing FDS instruments
* Fixed effect icons sometimes visible past the end of a pattern
* Fixed issue where instruments fail to load properly when a stop note is used before any other notes
* Fixed crash when playing a note from all channels with all expansions enabled
* Fixed a couple of minor issues with demo songs
* Fixed N163 emulation issue where waves where not properly biased by -8
* OPNI instrument import support for EPSM instruments (Perkka contribution)
* Minor localization fixes
* An APK of the Android version is now available for download, but no support will be provided if it does not work on your device

Breaking/Behavior changes:

* Songs using 7 or 8 N163 channels may sound slight quieter now and volume might react a bit differently as well

## Version 4.1.1 (Hotfix)

Changes/Fixes:

* Fixed DPCM samples not playing when exporting to ROM with VRC6
* Fixed transposition of DPCM samples incorrectly affecting all instruments
* Fixed "Clear effect values" not always working
* Fixed startup crashes with rtmidi on Linux and updating the version we provide
* Fixed export of DPCM samples with FamiStudio Text format
* Fixed file association on Windows when using the portable EXE version
* Fixed file association on MacOS not always working
* Fixed first mouse click outside a context menu being ignored (will behave like 4.0.x)
* Fixed occasional crash on MacOS when using Cmd+Q
* Fixed issues with MIDI device on MacOS
* Fixed trackpad controls on MacOS 
* Fixed keyboard shortcut confusion with select/force display channels
* Fixed minor localization issues.
* Changed the ffmpeg path option to allow text input, and allow using ffmpeg if its on the PATH
* Changed the default keyboard shortcut to force display channel to SHIFT + F-keys on MacOS to avoid issues

## Version 4.1.0

Changes/Fixes:

* DPCM improvements:
	* No more "DPCM instrument", each instrument can have DPCM samples assigned (a-la FamiTracker)
	* Up to 256KB of samples using bank switching.
* Configurable keyboard shortcuts
* Text rendering changes, small text may look slightly blurrier. 
* More UI scaling % options
* More context menus options throughout the app
* Option to mix N163 or not
* Project explorer sorting improvements
* NSF import support on Android
* Stereo and delay export support on Android.
* Sunsoft 5B noise support (Perkka contribution)
* VGM & WAV code cleanup (alexmush contribution)
* VGM import support (Perkka & alexmush contribution)
* Translations:
	* Spanish (NicolAR, zukinnyk and LagMager contribution)
	* Portuguese (BeepsNBoops contribution)
	* Simplified chinese (xwjcool123 and FRC contribution)

System Requirement Changes:

* Windows version is now 64-bit and targets .NET 5.0.
* Linux/MacOS versions now uses .NET 6.0. Mono is not longer used (See [installation](install.md) page for .NET download links)
* MacOS now requires at least Catalina and will run natively on ARM if you install the ARM version of .NET
* All desktop version now requires OpenGL 3.3.
* Android version now requires OpenGL ES 2.0.
* Windows 7 and 32-bit systems are longer supported.

<div style="position:relative;margin-left: auto;margin-right: auto;width:80%;height:0;padding-bottom:45%;">
	<iframe style="position:absolute;top:0;left:0;width:100%;height:100%" src="https://www.youtube.com/embed/1xQbFUGz0Co" frameborder="0" allow="accelerometer; autoplay; encrypted-media; gyroscope; picture-in-picture" allowfullscreen></iframe>
</div>
<br/>

## Version 4.0.6 (Hotfix)

Changes/Fixes:

* Fixed issue where modifying the N163 repeat envelope would sometimes reset the loop point.

## Version 4.0.5 (Hotfix)

Changes/Fixes:

* Added option for default pattern name
* Added option to rewind to the previous position when stopping playback
* Added warning when user is editing an arpeggio different than the currently selected one
* Added a proper warning when songs are too big to be exported to NSF, ROM or sound engine
* Allowed longer pattern on NSF import
* Fixed crash when using pattern length of 2048 frames
* Fixed opening of NSF when FamiStudio is set to the default app
* Fixed glitch that could cause visual overlap of notes in piano roll
* Fixed notes ending prematurely near the end of songs
* Fixed crash when exporting to NSF, ROM or sound engine with certain vibrato settings
* Fixed multiple issues when importing instruments from other projects
* Fixed issue causing exported NSFe to start at song #2.
* Fixed crash when MIDI notes are out of range.
* Fixed rasterization issue in the pattern thumbnails
* Fixed multiple FamiTracker (FTM/TXT) import/export issues
* Fixed minor annoyances in the piano roll on mobile
* Fixed app crashing when using more than 2GB of RAM on Windows
* Fixed loophole where the FDS envelopes could be cleared/resized
* Fixed various issues with dropdown list scrollbars 
* Fixed crash when exporting video with lots of channels
* Fixed releases for VRC7 and EPSM 
* Fixed multiple inconsistencies and improved the register viewer (Thanks alexmush!)

Sound Engine changes/fixes:

* Fixed inconsistencies with vibrato with the main app
* Fixed issues with relative pitch envelopes
* Fixed notes always reapply instruments on EPSM FM Channel 4,5,6 in NSF driver (Thanks Perkka!)
* Fixed EPSM rhythm occasionally missing beats (Thanks Perkka!)
* Fixed volume being off by one for EPSM (Thanks Perkka!)

Breaking/Behavior changes:

* The register viewer will now show the real note pitches and wont necessarily match the notes on the piano roll
* Vibrato depths of more than 13 cant unfortunately be supported in the sound engine at the moment. A warning will be given instead of producing the wrong result.
* Releases for VRC7/EPSM instrument will now be handled correctly. That was always the intention, but a bug was preventing them from working

## Version 4.0.4 (Hotfix)

Fixes:

* Fixed video export when there are more than 32 channels in a project.

## Version 4.0.3 (Hotfix)

Fixes:

* Fixed drop-down lists requiring double-clicks.

## Version 4.0.2 (Hotfix)

Fixes:

* Added option for trigger algorithm in oscilloscope export (desktop only)
* Fixed files without extensions not displaying correctly in the file dialog on Linux
* Fixed drag & drop of files containing unicode characters
* Fixed crash when saving with CTRL+S while editing something
* Fixed crash when exporting NSF/ROM with instrument containing dashes in their names
* Fixed crash when attempting to export a FDS project as a NES ROM
* Fixed crash when importing NSF containing non-ASCII characters on Linux/MacOS
* Fixed crash when importing some Famitracker modules/instruments with N163 instruments
* Added safeguards for import of Famitracker N163 instruments that were generated in forks
* Added option for Alt+Right-click zoom gesture (Input section of the settings), off by default
* Fixed typos

## Version 4.0.1 (Linux & Android Hotfix)

Fixes:

* Fixed file dialog on Linux.
* Fixed some piano touches being ignored on Android.

## Version 4.0.0

Changes:

* Revamped desktop version : 
	* Redesigned controls, more similar to mobile version : Context menus, Gizmos, etc.
    * Based on GLFW using custom drawn widgets, consistent across Win/MacOS/Linux
    * Getting rid of all dependencies to OpenTK, WinForms, GTK# and System.Drawing in preparation to eventual migration to modern .NET.
	* Animated GIFs for intro tutorial
* More control over sequencer height and ability to hide unused channels on desktop
* Support to multiple audio expansions with EPSM
* Support for multiple waveforms for N163
* Support for single audio expansions for ROM export 
* Wave resampling for N163/FDS waveforms, import WAV files and adjust period & offset.
* Basic NSFe export support, only track names and durations for now.
* Improved oscilloscope stability:
	* Use of emulation-generated triggers when rendering oscilloscope in exported video or in toolbar when previewing instruments.
	* Improved trigger detection for toolbar oscilloscope when playing a song (using "peak speed trigger" algorithm).
* Audio delay effect when exporting video or audio.
* Snapping improvements, most notably:
	* **Alt+1**, **Alt+2**, **Alt+3** and **Alt+4** quickly changes between common snapping values
	* Hold **Alt** anytime when resizing or moving notes to temporarily disable snapping
	* Context menu to set snapping to a specific note duration
	* Option to snap effect values

Breaking/Behavior changes:

* The sound engine now has a dedicated define for release note. If you use release notes you now must set `FAMISTUDIO_USE_RELEASE_NOTES`. 

<div style="position:relative;margin-left: auto;margin-right: auto;width:80%;height:0;padding-bottom:45%;">
	<iframe style="position:absolute;top:0;left:0;width:100%;height:100%" src="https://www.youtube.com/embed/pgJYHGu8yio" frameborder="0" allow="accelerometer; autoplay; encrypted-media; gyroscope; picture-in-picture" allowfullscreen></iframe>
</div>
<br/>

## Version 3.3.1 (Hotfix)

Fixes/Changes:

* Fixed crash when exporting (or cancelling export) to audio or video
* Fixed crash when undo-ing certain manipulations in the Sequencer
* Allowing register viewer on Mobile (off by default)

## Version 3.3.0

Changes:

* EPSM support (Thanks Perkka!)
* Register viewer (desktop version only)
* Delta Counter (Zxx) effect support
* Ability to override initial DMC value for each sample assignment
* VGM export (Thanks Perkka!)
* Bamboo Tracker instruments import (desktop version only) (Thanks Perkka!)
* Minor cosmetic changes (ADSR diagrams, tabs in instrument editor, etc.)
* CTRL+A selects all patterns/notes in sequencer/piano roll on desktop
* Additional selection options on mobile (select note/pattern/all)
* Option not to clamp periods/notes to make the app behave more like the NSF driver
* Experimental file association on MacOS (Thanks beetrootpaul!)
* Tons of small bugfixes (N163 tuning, crashes, etc.)

<div style="position:relative;margin-left: auto;margin-right: auto;width:80%;height:0;padding-bottom:45%;">
	<iframe style="position:absolute;top:0;left:0;width:100%;height:100%" src="https://www.youtube.com/embed/n1sOtT-s65A" frameborder="0" allow="accelerometer; autoplay; encrypted-media; gyroscope; picture-in-picture" allowfullscreen></iframe>
</div>
<br/>

## Version 3.2.3 (Hotfix)

Fixes/Changes:

* Fixed OpenGL crash at startup when SDL2.dll is found
* Fixed export to sound engine when using an audio expansion
* Fixed release of wrong instrument being played when editing envelopes
* Fixed FTM import of some noise channel notes
* Clamping pitch values to the range supported by FamiStudio (-64...63) when importing Famitracker files 
* Added a new tutorial about snapping
* Saving snap settings to INI file

## Version 3.2.2 (Hotfix)

Fixes/Changes:

* Fixed issue where some effect values could go beyond their intended range, and lead to crashes desktop.
* Fixed FDS modulation on Mobile.
* Fixed crash in MIDI import dialog when lots of channels are present (desktop only)
* Potential CPU/GPU usage reduction on some computers (desktop only)

## Version 3.2.1 (Hotfix)

Fixes/Changes:

* Added option to disable vibration (new "Mobile" section of Settings)
* Added support for importing 8-bit and 24-bit WAV files for DPCM samples.
* Added support for importing NSF2, if they do not use any of the advanced features.
* Fixed FamiTracker text export not asking for a filename.
* Fixed DPCM samples not playing if placing the seek bar exactly on their attack.
* Fixed NSF export tempo issue when using grooves in specific situations.
* Creating new effect values will respect snapping on mobile.
* Drawing note attacks for force display channel to help readability.
* Fixed an issue with release note that are beyond the visual duration of the note.
* Correct emulation of Tri-Noise-DMC volumes when using stereo export (+hidden INI option to also do it for separate channels)
* Experimental "double-tap to delete patterns/note" option (off by default)
* Minor UI tweaks (hit boxes of buttons, zoom levels, scroll bar issues, etc.).

## Version 3.2.0

Changes:

* Android app (free on Play Store)
* Multi-expansion support
* Proper emulation of triangle/noise/DPCM volume interactions
* Significant graphic optimizations, especially on lower-end machines or large projects.
* Minor quality of life features
* Minor cosmetic UI changes (icons, envelope editor, coloring piano with DPCM colors, etc.)

Behavior changes:

* Snapping precision is now expressed in beats. So with the default settings, 1 means a quarter note.
* Channel volumes have been slightly adjusted to match the NES hardware more closely.

<div style="position:relative;margin-left: auto;margin-right: auto;width:80%;height:0;padding-bottom:45%;">
	<iframe style="position:absolute;top:0;left:0;width:100%;height:100%" src="https://www.youtube.com/embed/vXfvDSRZYco" frameborder="0" allow="accelerometer; autoplay; encrypted-media; gyroscope; picture-in-picture" allowfullscreen></iframe>
</div>
<br/>

## Version 3.1.1 (Hotfix)

Fixes:

* Fixed piano roll piano keys not highlighting when exporting video
* Fixed application stalling when closing on MacOS
* Fixed "advanced properties" buttons growing when using >100% scaling on Windows
* Adding failsafe when time signature cannot be computed when exporting MIDI
* Associating FMS files with FamiStudio on startup in portable mode
* Experimental support for OpenH264 in video export

## Version 3.1.0

Changes:

* OGG export
* Oscilloscope video export
* Stereo WAV/MP3/OGG/Video export
* Metronome
* Noise slides
* Volume slides
* Noise emulation improvements
* Portable mode (save settings in root folder if portable.txt is detected)
* Import samples from other projects
* Loop in selection mode
* ROM export now uses MMC3 mapper
* Fine tuning + DMC initial value parameters on DPCM samples
* Option to auto-save copy every 2 minutes
* Option for thicker scroll bars
* Minor sequencer selection improvements
* MacOS Monterey Beta support, Beta 3 or newer needed (Thanks OpenTK team!)
* Sound Engine CC65 bindings (Contribution from jroweboy!)

Breaking/behavior changes:

* Pattern numbers in the sequencer and piano roll now start at 1 instead of 0. This is a purely esthetic change and does not impact anything.

<div style="position:relative;margin-left: auto;margin-right: auto;width:80%;height:0;padding-bottom:45%;">
	<iframe style="position:absolute;top:0;left:0;width:100%;height:100%" src="https://www.youtube.com/embed/GSYfj4MFGGE" frameborder="0" allow="accelerometer; autoplay; encrypted-media; gyroscope; picture-in-picture" allowfullscreen></iframe>
</div>
<br/>

## Version 3.0.2 (Hotfix)

Fixes:

* Fixed crash when pasting past the end of an envelope
* Fixed FamiTracker (FTM/TXT) import when a jump or skip effect is exactly at row 255
* Fixed FamiStudio corrupting the keyboard state on some European keyboards
* Fixed oscilloscope vertical orientation in toolbar + video export
* Fixed crash when using 150% scaling on Linux
* Fixed crash when parsing FamiStudio text file using VRC6 expansion.
* Fixed fine effect adjustment (shift + drag) again
* Stopping all audio when undo/redo happens to change the selected song
* Showing all 8 channels when importing MIDI using N163 expansion
* Added error message when trying to import a corrupted WAV file
* Made auto-scrolling when selecting more gradual in both sequencer and piano roll.

## Version 3.0.1 (Hotfix)

Fixes:

* Fixed crash on startup on Linux when RtMidi fails to initialize
* Fixed fine effect adjustment (SHIFT + drag) when multiple values are selected
* Fixed race condition when stopping song playback that could cause a crash
* Fixed small OpenGL leaks
* Fixed pattern duplication when CTRL+SHIFT were already pressed when starting the drag
* Fixed import of MIDI files containing polyphonic key pressure events
* Fixed duplicated labels when exporting projects containing multiple songs to Famitone2 or FamiStudio sound engine
* Fixed crash when double clicking or left clicking in some cells of grids
* Fixed export of VRC6 duty cycles to FamiTracker
* Properly shutting down MIDI input on Windows
* Adding error message when trying to import NSF using multiple expansion chips

## Version 3.0.0

Changes :

* Redesigned FamiStudio tempo
* Redesigned note editing
* MIDI import/export support
* Oscilloscope in toolbar
* Optional scrollbars
* Revamped Mac version
* 1/2 and 1/4 playback speed
* Configurable QWERTY keyboard keys
* Improved sound engine music data compression (15-30% smaller)
* Audio expansion volume/low-pass filter configuration
* S5B and VRC7 and now properly low-pass filtered
* Ability to re-order songs
* Hi-DPI support on Linux
* Quick DPCM source data reload
* Improved video rendering speed

Breaking/behavior changes:

* Setting a zero volume on the volume track of the triangle channel will now stop the sound. This was always the intention (this is what SoundEngine/NSF and FamiTracker does) but it was not correctly implemented inside FamiStudio.

<div style="position:relative;margin-left: auto;margin-right: auto;width:80%;height:0;padding-bottom:45%;">
	<iframe style="position:absolute;top:0;left:0;width:100%;height:100%" src="https://www.youtube.com/embed/7j9mhY9XNsc" frameborder="0" allow="accelerometer; autoplay; encrypted-media; gyroscope; picture-in-picture" allowfullscreen></iframe>
</div>
<br/>

## Version 2.4.2 (Hotfix)

Fixes:

* Fixed crash on startup.
* Fixed crash when exporting SFX to sound engine from command-line.

## Version 2.4.1 (Hotfix)

Fixes:

* Fixed corruption when importing looping samples from .FTI files.
* Fixed minor graphical issue in piano roll.
* Fixed issues on Windows 7 installations that are missing some D3D11 components such as KB2670838.
* Removed 320 kbps MP3 export option since it creates choppy audio.

## Version 2.4.0

Changes :

* Basic DPCM sample editor
* Darker theme and more color choices
* Ability to use QWERTY keyboard input without using recording mode
* Slightly reduced audio latency and configurable number of buffered frames
* Right-clicking anywhere in the background also starts a selection
* Seek bar can be dragged and respects snapping precision
* Pattern duplication preserves names when possible
* Instrument picker tool (hold "I" + click on a note)
* Paste special in Sequencer
* Pattern are created automatically when adding notes.
* Quickly repeat last export by right clicking button or CTRL+SHIFT+E
* Edit multiple custom patterns at once
* Video export resolution/framerate options (Contributed by Thomas McGrew)
* Multiple VRC7 fixes
* Multiple FamiTracker fixes
* Multiple sound engine improvements & fixes 
	* Blaarg smooth vibrato support for SFX
	* Unlimited SFX size (Contributed by Brad Smith)
	* Linker support in CA65 (Based on idea by Brad Smith)

Breaking/behavior changes:

* Songs will always be sorted alphabetically. This include songs loaded from any file format. This was always the intention but was poorly enforced in previous versions.
* There was a bug in 2.3.x where instrument with volume envelope consisting exclusively of zeroes would play at full volume. This is no longer the case and the instruments will be silent.

<div style="position:relative;margin-left: auto;margin-right: auto;width:80%;height:0;padding-bottom:45%;">
	<iframe style="position:absolute;top:0;left:0;width:100%;height:100%" src="https://www.youtube.com/embed/zwkIV4VwlLw" frameborder="0" allow="accelerometer; autoplay; encrypted-media; gyroscope; picture-in-picture" allowfullscreen></iframe>
</div>
<br/>

## Version 2.3.2 (Hotfix)

Changes:

* Added loop count to video export.

Fixes: 

* Fixed scaling issues on Retina display.
* Fixed lingering noise when dragging/adding notes in VRC7.
* Fixed multiple FamiTracker import (Text + Binary) issues/crashes.
* Fixed potential de-sync in NSF/sound engine when using delayed notes.
* Fixed various issues with expansion instruments UI (sliders/checkboxes for VRC7/FDS/N163).
* Fixed import of older FamiStudio text files (pre 2.3.0).
* Fixed NSF/sound engine crash when exporting empty arpeggios.
* Fixed issue with arpeggios sometimes persisting when a song loops. 

## Version 2.3.1 (Linux Hotfix)

Fixes:

* Fix startup crash on Linux.

## Version 2.3.0

Changes:

* MP3 export
* Video export
* Song merge functionality
* Duty cycle effect track support (equivalent of Vxx in FamiTracker) 
* Special paste improvements (repeat, effects, etc.)
* Special delete
* Copy patterns to different channels in sequencer
* Option to display note labels in piano roll
* FamiTracker tempo improvements (delayed notes, cuts, fixes).
* Added support for Cxx (Halt) FamiTracker effect.
* Option to export each channel to a separate WAV/MP3 file. 
* Small DPCM improvements (Drag & drop, bit reverse option)
* Small tempo improvements
* Sound engine code size reduction

<div style="position:relative;margin-left: auto;margin-right: auto;width:80%;height:0;padding-bottom:45%;">
	<iframe style="position:absolute;top:0;left:0;width:100%;height:100%" src="https://www.youtube.com/embed/wD5eZTc4H5o" frameborder="0" allow="accelerometer; autoplay; encrypted-media; gyroscope; picture-in-picture" allowfullscreen></iframe>
</div>
<br/>

## Version 2.2.1 (Hotfix)

Changes:

* Fixed import of slide notes from FamiStudio Text format.
* Added option to not trim unused data when exporting to FamiStudio Text format.
* Fixed crash when exporting to some formats on MacOS
* Fixed crash when exporting FamiStudio/FamiTone2 SFX longer than 256 bytes.
* Fixed glibc dependency issue on Linux that go introduced in 2.2.0.

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

<div style="position:relative;margin-left: auto;margin-right: auto;width:80%;height:0;padding-bottom:45%;">
	<iframe style="position:absolute;top:0;left:0;width:100%;height:100%" src="https://www.youtube.com/embed/X1zPjnM1wb0" frameborder="0" allow="accelerometer; autoplay; encrypted-media; gyroscope; picture-in-picture" allowfullscreen></iframe>
</div>
<br/>

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
* Workaround "pops" on square channels (Blaarg's smooth vibrato technique)
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
