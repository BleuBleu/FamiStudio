# Exporting Songs

The export dialog is access through the main toolbar or with CTRL+E on the keyboard. To quickly repeat a previous export (same format and output file), you can right click on the export icon in the toolbar or press CTRL+SHIFT+E.

## Wave / MP3 / OGG Vorbis File

Only a single song can be exported at a time. You can choose the sample rate (and bitrate in case of MP3/OGG). It is recommended to stick to 44.1KHz if you want the sound to be exactly as you hear it in FamiStudio. Lower sample rate might lack high frequencies.

The song can be exported in one of two modes:

* **Play N Times**: Will play the song a specified number of times.
* **Duration** : Will loop though the song for the specified number of seconds.

Other options:

* **Delay (ms)** : Optional audio delay effect that will include an echo of the audio at the specified delay. Highly recommanded to use with Stereo and heavy L/R panning (ex: set channels entirely on one side) as the echo effect will be on the opposite side. 
* **Separate channel files** : Will output each channel in a separate audio file.
* **Separate intro file** : Will output the part before the loop point (intro) in a separate file. 
* **Stereo** : Will output a stereo file and will allow setting the panning % in the channel list.

Note that the quality of the MP3 encoding may not going to be as good as full fledged MP3 encoder such as LAME, but should be good enough for sending quick previews to people.

![](images/ExportWav.png#center)

Channels can optionally be muted. This can be used, for example, to create stereo mix using and external application.

OGG Vorbis is currently only available on the desktop version of FamiStudio.

## Video

Video export is a great way to add a visual element to your songs when sharing them on YouTube/social media. For this to work, you will need to [manually download FFmpeg](ffmpeg.md) and extract it somewhere on your computer. 

![](images/ExportVideo.png#center)

There are 3 main video modes:

* **Oscilloscope** : A grid of oscilloscope, one per channel.
* **Piano Roll (Separate Channels)** : A grid of piano rolls, one per channel.
* **Piano Roll (Unified)** : A single piano roll containing all channels.

Besides the audio/video quality settings, there are a few options to control the look and feel of the video:

* **Audio Delay (ms)** : Optional audio delay effect that will include an echo of the audio at the specified delay. Highly recommanded to use with Stereo and heavy L/R panning (ex: set channels entirely on one side) as the echo effect will be on the opposite side. 
* **Loop count** : Number of times to repeat the looping part of the song.
* **Stereo** : Same as WAV/MP3/OGG.
* **Channels** : It is recommended to not export channels that do not have any notes, this will leave more space to the other channels.
* **Overlay Register Values** : Will draw the register viewer in the top-right corner of the video.

Options specific to piano roll video:

* **Piano Roll Note Width** : Width of the piano roll keys. 
* **Piano Roll Zoom** : The higher the zoom, the faster notes will scroll by. 
* **Piano Roll Rows** : The number of rows to have in separate channel mode.
* **Piano Roll 3D Perspective** : If > 0, will add a 3D perspective effect with a slight depth-of-field effect.

Options specific to oscilloscope video:

* **Oscilloscope Columns** : Number of columns to split the channels into.
* **Oscilloscope Thickness** : Thickness of the oscilloscope line, in pixels.
* **Oscilloscope Window** : The number of frames the oscilloscope should contain.
* **Oscilloscope Color** : Can color the oscilloscope using the colors of the instruments or samples. Otherwise will use a neutral light grey. 

Example of the piano roll format with 3D effect and overlaid register values.
![](images/VideoScreenshot.jpg#center)

Example of the oscilloscope format:
![](images/VideoOscScreenshot.jpg#center)

### Video Preview

On desktop, you can can get a rough preview of the video by pressing the "Preview" button which is located at the bottom of all the video settings.

On mobile, the "Preview" button is accessed by pressing the "..." button 

![](images/MobileVideoPreview.gif#center)

## Nintendo Sound Format (NSF)

Every feature supported by FamiStudio can be used in an NSF. 

![](images/ExportNsf.png#center)

Options:

* **Format** : You can choose between NSF/NSFe. NSF is more widely support, NSFe adds support for per-track names and durations.
* **Mode** : Song that do not use any audio expansion can export as PAL and Dual mode, where as only NTSC is available for expansion audio.

The maximum song size is approximately 28KB minus the size of the DPCM samples used. Note that this size are not printed anywhere and are not related to the size of the \*.fms file. Best to simply try and see if it works.

## ROM / FDS Disk

Song(s) can be exported to a NES ROM file or a Famicom Disk System disk to play if back on an emulator or on actual hardware. 

![](images/ExportRom.png#center)

The exported ROMs can run on actual hardware using devices such as the [Everdrive N8](https://krikzz.com/store/home/31-everdrive-n8-nes.html).

Here are the mappers used for each audio expansions:

* **None** : MMC3 - iNES Mapper 004 
* **VRC6** : VRC6a - iNES Mapper 024
* **VRC7** : VRC7 - iNES Mapper 085 
* **MMC5** : MMC5 - iNES Mapper 005
* **N163** : N163 - iNES Mapper 019
* **S5B** : FME-7 - iNES Mapper 069
* **EPSM** : MMC3 - iNES Mapper 004 

Note that projects using multiple audio expansions cannot be exported to ROM/FDS.

![](images/Rom.png#center)

The user interface is very simple, pressing left/right on the D-PAD will change the current track. Track/song names will be truncated to 28 characters. For something a lot more customizable, check out [EZNSF](https://github.com/bbbradsmith/eznsf) by Brad Smith.

For FDS export, the process is very similar and will output a FDS disk image instead of a ROM. It has been tested on actual hardware using the [FDSStick](https://3dscapture.com/fdsstick/). Songs are loaded on demand from the disk, so switching tracks will be a bit slower on FDS than on a ROM. 

![](images/ExportFds.png#center)

## MIDI

Songs created using the FamiStudio tempo mode can be exported to MIDI. This is only available in the desktop version.

![](images/ExportMIDI.png#center)

There are 2 ways in which FamiStudio can assign General-MIDI instruments:

* **Instrument** : Each FamiStudio instrument will be assigned to a GM instrument. Program changes will be inserted in the song to change to these instruments.
* **Channel** : A single instrument per-channel will be used.

At the bottom of the dialog is the instrument table. This is where you can assign instrument to channels or FamiStudio instrument (depending on the Instrument Mode). Double-clicking on a row will allow selecting the instrument.

## VGM

Songs can be exported to VGM files for supported sound expansions, VGM files are basically a log of the writes being sent to the various sound chips.

![](images/ExportVgm.png#center)

The expansions that's supported by the VGM file format is:

* **NES APU**
* **FDS**
* **VRC7**
* **S5B**
* **EPSM**

## FamiTracker Text

You can export songs to FamiTracker using their Text Export format. This is only available in the desktop version.

![](images/ExportFamiTracker.png#center)

There are some limitations:

* Pitch envelopes with looping sections will be modified on export so that the looping part sums to zero. This is done to prevent pitch from drifting up/down every time the envelope loops. The reason for this is that FamiTracker's pitch envelopes are relative while FamiStudio's are absolute.

* Instruments using both pitch and arpeggio envelopes at the same time will not sound correct in FamiTracker. This is due to the vastly different way both applications handles these. FamiTracker re-triggers the pitch envelope at each arpeggio notes (probably the more sensible way), while FamiStudio simply runs both at the same time.

* For slide notes, FamiStudio will do its very best to choose which FamiTracker effect to use. Note that importing/exporting slide notes with FamiTracker should be considered a **lossy** process. Here are the general rules:
	* If the slide note and its target are within 16 semitones, Qxx/Rxx (note slide up/down) will be favored as it is the most similar effect to what we are doing.
	* Otherwise, if the previous note has the same pitch as the slide note, 3xx (auto-portamento) will be used.
	* Finally, if none of these conditions are satisfied, 1xx/2xx (slide up/down) will be used. This is not ideal since the pitch might not exactly match the target note.<br><br>

* Slide notes of VRC7 will definitely not export perfectly due to the different way in which both applications handle VRC7 pitches.

* VRC6 saw channel is not influenced by duty cycle in FamiStudio. FamiStudio always allow the full volume range for the saw. Import/export process does not try account for this. This might lead to volume inconsistencies between FamiTracker and FamiStudio where the volume needs to be doubled or halved to sound correct.

* Vibrato effect might sound slightly different once exported to FamiTracker. The speed values in FamiStudio are slightly different than FamiTracker. Here is a table relating the speeds in FamiStudio and FamiTracker (this is applied automatically when importing/exporting):

FamiTracker speed | FamiTracker period | FamiStudio speed | FamiStudio period
--- | --- | --- | ---
1 | 64 | 1 | 64
2 | 32 | 2 | 32
3 | 21.3 | 3 | 21
4 | 16 | 4 | 16
5 | 12.8 | 5 | 13
6 | 10.7 | 6 | 11
7 | 9.1 | 7 | 9
8 | 8 | 8 | 8
9 | 7.1 | 9 | 7
10 | 6.4 | 10 | 6
11 | 5.8 | 10 | 6
12 | 5.3 | 11 | 5
13 | 4.9 | 11 | 5
14 | 4.6 | 11 | 5
15 | 4.3 | 12 | 4

## FamiStudio Text

FamiStudio text format is a textual representation of the of the binary FamiStudio format. It is designed to favor interoperability with other apps, tools and sound engines. Unlike .FMS files, this format is not forward or backward compatible. FamiStudio can only read FamiStudio text files generated with the same major and minor version (ex: 3.0.x).

![](images/ExportFamiStudioText.png#center)

Exporting or importing using this format mostly lossless. These are the features that will not be preserved:

* The custom colors assigned to songs, instruments, patterns, arpeggios and DPCM samples will not be preserved, they will be randomized every time you re-import the file.

* Only the final processed data of DPCM samples will exported. The source data as well as any processing parameter will be lost. 

Only the desktop version of FamiStudio supports exporting to this format.

### Format Specification

The format describe one object per line followed by some attributes and values. Attribute are always between double-quotes. All indentation is purely cosmetic. 

	Object Attribute1="Value1" Attribute2="Value2"

Double quotes in values can be escaped by doubling them, similar to some CSV dialects:

	Object Attribute1="Example ""Quoted"" Value1" Attribute2="Value2"

The general structure of a file has these objects nested like this:

* **Project** : The root of the file.
	* **DPCMSample** : A DPCM sample
	* **DPCMMapping** : Maps a piano key to a DPCMSample.
	* **Instrument** : An instrument
		* **Envelope** : One envelope of an instrument
	* **Song** : A song
		* **PatternCustomSettings** : Custom length or tempo settings of a pattern column.
		* **Channel** : A channel of the NES (or audio expansion)
			* **Pattern** : A short part of the song that can be repeated.
				* **Note** : A note inside a pattern.
		* **PatternInstance** : An instance (copy) of a pattern at a specific time.
		
Here is an example of a very short file:

	Project Version="2.0.0" TempoMode="FamiStudio" Name="FamiStudio Tutorial" Author="NesBleuBleu"
		DPCMSample Name="BassDrum" Data="aaaaaaaaaaaff09e9ff006ae7c1b98f0000300"
		DPCMMapping Note="C3" Sample="BassDrum" Pitch="15" Loop="False"
		Instrument Name="Lead"
			Envelope Type="Volume" Length="1" Values="12"
			Envelope Type="DutyCycle" Length="1" Values="2"
		Instrument Name="Lead2"
			Envelope Type="Volume" Length="12" Values="15,12,11,9,7,6,5,4,3,2,1,1"
		Song Name="Tutorial Song" Length="5" LoopPoint="1" PatternLength="16" BeatLength="4" NoteLength="7"
			PatternCustomSettings Time="0" Length="10" NoteLength="7" BeatLength="4"
			Channel Type="Square1"
				Pattern Name="Intro1"
					Note Time="0" Value="G3" Instrument="Lead"
					Note Time="14" Value="A#3" Instrument="Lead"
					Note Time="28" Value="G3" Instrument="Lead"
					Note Time="42" Value="A#3" Instrument="Lead"
					Note Time="63" Value="A#3" Instrument="Lead" SlideTarget="C4"
				Pattern Name="Melody1"
					Note Time="0" Value="C4" Instrument="Lead2" Volume="15"
					Note Time="70" Value="D#4" Instrument="Lead2"
					Note Time="98" Value="D4" Instrument="Lead2"
				PatternInstance Time="0" Pattern="Intro1"
				PatternInstance Time="1" Pattern="Melody1"


The possible types of objects and their attributes:

Object | Attributes | Mandatory | Description
--- | --- | --- | ---
Project | Version | Yes | The FamiStudio version that exported the file.
| Expansion | | Expansion audio used by the project: VRC6, VRC7, FDS, MMC5, N163 or S5B
| TempoMode | | FamiTracker or FamiStudio
| Name | | Name of the project
| Author | | Author of the project
| Copyright | | Copyright of the project
DPCMSample | Name | Yes | The name of the sample.
| Data | Yes | The processed data as a series of hexadecimal nibbles.
DPCMMapping | Note | Yes | Piano note to map the sample to (Between C1 and D6).
| Sample | Yes | Name of the DPCMSample to map.
| Pitch | | Pitch of the sample.
| Loop | | True or False.
Arpeggio | Name | Yes | Name of the arpeggio.
| Length | Yes | Length of the arpeggio note sequence, 0 to 255.
| Loop | Yes | The loop point of the envelope.
| Values | Yes | Coma separated values of the arpeggio -64 to 63 range.
Instrument | Name | Yes | Name of the instrument.
| Expansion | | Expansion audio of the instrument: VRC6, VRC7, FDS, MMC5, N163, S5B
| FdsWavePreset | | (FDS instrument only) Sine, Triangle, Sawtooth, Square 50%, Square 25%, Flat or Custom.
| FdsModPreset | |  (FDS instrument only) Sine, Triangle, Sawtooth, Square 50%, Square 25%, Flat or Custom.
| FdsMasterVolume | | (FDS instrument only) 0 to 3.
| FdsModSpeed | | (FDS instrument only) 0 to 4095.
| FdsModDepth | | (FDS instrument only) 0 to 63.
| FdsModDelay | | (FDS instrument only) 0 to 255.
| N163WavePreset | | (N163 instrument only) Sine, Triangle, Sawtooth, Square 50%, Square 25%, Flat or Custom.
| N163WaveSize | | (N163 instrument only) 4 to 248, multiple of 4 (4, 8, 12, etc.).
| N163WavePos | | (N163 instrument only) 0 to 244, multiple of 4 (4, 8, 12, etc.).
| Vrc6SawMasterVolume | | (VRC6 instrument only) Full, Half or Quarter.
| Vrc7Patch | | (VRC7 instrument only) 0 to 15.
| Vrc7Reg{1 to 8} | | (VRC7 instrument only, if Vrc7Patch is 0) Values of the 8 custom patch registers.
Envelope | Type | Yes | Volume, Arpeggio, Pitch, DutyCycle, FDSWave, FDSMod or N163Wave.
| Length | Yes | Length of the envelope, 0 to 255.
| Loop | | The loop point of the envelope.
| Release | | (Volume envelope only) The release point of the envelope.
| Relative | | (Pitch envelope only) True or False.
| Values | Yes | Coma separated values of the envelopes -64 to 63 range.
Song | Name | Yes | The name of the song.
| Length | Yes | The length of the song in number of patterns.
| LoopPoint | Yes | The loop point of the song, -1 to disable.
| PatternLength | Yes | The number of notes in a pattern.
| BeatLength | Yes | The number of notes in a beat.
| NoteLength | Yes | (FamiStudio tempo only) The number of frames in a note. 
| Groove | Yes | (FamiStudio tempo only) | The groove (sequence of frames) to use.
| GroovePaddingMode | Yes | (FamiStudio tempo only) | Where to add idle frames in the groove : Beginning, Middle or End. 
| FamiTrackerTempo | Yes | (FamiTracker tempo only) The FamiTracker tempo.
| FamiTrackerSpeed | Yes | (FamiTracker tempo only) The FamiTracker speed.
PatternCustomSettings | Time | Yes | Index of the column of pattern that uses these custom settings.
| Length | Yes | The custom length (in notes or frames) of the pattern.
| NoteLength | Yes | (FamiStudio tempo only) The number of frames in a note. 
| BeatLength | Yes | (FamiStudio tempo only) The number of notes in a beat.
| Groove | Yes | (FamiStudio tempo only) | The groove (sequence of frames) to use.
| GroovePaddingMode | Yes | (FamiStudio tempo only) | Where to add idle frames in the groove : Beginning, Middle or End. 
Channel | Type | Yes | Square1, Square2, Triangle, Noise, DPCM, VRC6Square1, VRC6Square2, VRC6Saw, VRC7FM1, VRC7FM2, VRC7FM3, VRC7FM4, VRC7FM5, VRC7FM6, FDS, MMC5Square1, MMC5Square2, MMC5DPCM, N163Wave1, N163Wave2, N163Wave3, N163Wave4, N163Wave5, N163Wave6, N163Wave7, N163Wave8, S5BSquare1, S5BSquare2 or S5BSquare3 
Pattern | Name | Yes | Name of the pattern.
Note | Time | Yes | The frame (or note) number inside the pattern where this note is.
| Value | | Note from C0 to B7, unspecified for non-musical notes. "Stop" for orphan stop notes.
| Duration | | Note duration, only for musical notes.
| Instrument | | (Only if note has a musical value, from C0 to B7) Name of the instrument to use.
| Arpeggio | | Optional, name of the arpeggio to use.
| Attack | | If the note has an attack, True of False.
| Volume | | The volume of the note, 0 to 15.
| VolumeSlide | | If specified, the note has a volume slide and slides to the specified value. Must also have a Volume.
| VibratoSpeed | | The Vibrato speed, 0 to 12.
| VibratoDepth | | The Vibrato depth, 0 to 15.
| Speed | | (FamiTracker tempo only) Updates the FamiTracker speed to a new value.
| FinePitch | | The fine pitch, -128 to 127.
| SlideTarget | | The slide note target, from C0 to B7.
| FdsModSpeed | | FDS modulation speed, 0 to 4095.
| FdsModDepth | | FDS modulation depth, 0 to 63.
| DutyCycle | | Duty cycle, 0 to 3 on most channels, 0 to 7 on VRC6 squares.
| NoteDelay | | Number of frames to delay the note, 0 to 31.
| CutDelay | | Stops the note after a number of frames, 0 to 31.
PatternInstance | Time | Yes | Index of the column of patterns where to place this instance.
| Pattern | Yes | Name of the pattern to instantiate.

## FamiStudio / FamiTone2 Music

Exporting music to the [FamiStudio Sound Engine](soundengine.md) or [FamiTone2](https://shiru.untergrund.net/code.shtml) work very similarly. It will generate assembly code that can be included in your homebrew project and played using the FamiTone2 sound engine. 

![](images/ExportFamiTone2.png#center)

When exporting file in separate files, you can specific a name format template for each song. The {project} and {song} macros are available.

When exporting as a single file (non-separate), you will be prompt to name the output assembly file. If any of the exported songs uses DPCM samples, a .dmc file of the same name will also be outputted.

Only the desktop version of FamiStudio supports exporting to this format.

## FamiStudio / FamiTone2 SFX

The same goes for sound effect export. In this mode, one song is one sound effect, so songs should be very short. Unlike the FamiStudio/FamiTone2 music format which only support a subset of all the FamiTracker/FamiStudio features, here any feature (except DPCM samples) can be used and the sound effect should export correctly.

![](images/ExportFamiTone2SFX.png#center)

Only the desktop version of FamiStudio supports exporting to this format.

## Share (Mobile Only)

FamiStudio stores your projects in its own internal folder, inaccessible from the rest of your phone. To copy a `.fms` file onto your device storage, or to send it to someone through another app on your phone (Email, Discord or various messaging or social media apps, etc.), you can use the **Share** export feature. 
