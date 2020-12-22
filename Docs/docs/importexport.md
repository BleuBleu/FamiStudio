# Exporting Songs

The export dialog is access through the main toolbar.

## Wave File

Only a single song can be exported at a time. You can choose the sample rate, it is recommended to stick to 44.1KHz if you want the sound to be exactly as you hear it in FamiStudio. Lower sample rate might lack high frequencies.

The song can be exported in one of two modes:

* Play N Times: Will play the song a specified number of times.
* Duration : Will loop though the song for the specified number of seconds.

![](images/ExportWav.png#center)

Channels can optionally be muted. This can be used, for example, to create stereo mix using and external application.

## Nintendo Sound Format

Every feature supported by FamiStudio can be used in an NSF. 

![](images/ExportNsf.png#center)

Song that do not use any audio expansion can export as PAL and Dual mode, where as only NTSC is available for expansion audio.

The maximum song size is approximately 28KB minus the size of the DPCM samples used. Note that this size are not printed anywhere and are not related to the size of the *.fms file. Best to simply try and see if it works.

## ROM / FDS Disk

Song(s) can be exported to a NES ROM file or a Famicom Disk System disk to play if back on an emulator or on actual hardware. 

For ROM export, the current iteration uses [Mapper 31](https://wiki.nesdev.com/w/index.php/INES_Mapper_031) which is supported by most emulators and the [Everdrive N8](https://krikzz.com/store/home/31-everdrive-n8-nes.html). You can then copy the ROM to an SD card and listen to it on actual hardware. Note that expansion audio is not supported with this mapper.

![](images/Rom.png#center)

The user interface is very simple, pressing left/right on the D-PAD will change the current track. Track/song names will be truncated to 28 characters. For something a lot more customizable, check out [EZNSF](https://github.com/bbbradsmith/eznsf) by Brad Smith.

For FDS export, the process is very similar and will output a FDS disk image instead of a ROM. It has been tested on actual hardware using the [FDSStick](https://3dscapture.com/fdsstick/). Songs are loaded on demand from the disk, so switching tracks will be a bit slower on FDS than on a ROM. 

![](images/FdsExport.png#center)

## FamiTracker Text

You can export songs to FamiTracker using their Text Export format.

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

FamiStudio text format is a textual representation of the of the binary FamiStudio format. It is designed to favor interopability with other softwares, tools and sound engines. Exporting or importing this mostly lossless. The only this not stored in this format are the custom colors assigned to instruments, patterns and songs (they will be randomized every time you re-import the file).

![](images/ExportFamiStudioText.png#center)

### Format Specification

The format describe one object per line followed by some attributes and values. Attribute are always between double-quotes. All indentation is purely cosmetic. 

	Object Attribute1="Value1" Attribute2="Value2"

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
		Song Name="Tutorial Song" Length="5" LoopPoint="1" PatternLength="16" BarLength="4" NoteLength="7"
			PatternCustomSettings Time="0" Length="10" NoteLength="7" BarLength="4"
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
| Data | Yes | The data as a series of hexadecimal nibbles.
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
| N163WaveSize | | (N163 instrument only) 4, 8, 16 or 32.
| N163WavePos | | (N163 instrument only) 0 to 124.
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
| BarLength | Yes | The number of notes in a bar.
| NoteLength | Yes | (FamiStudio tempo only) The number of frames in a note. 
| Tempo | Yes | (FamiTracker tempo only) The FamiTracker tempo.
| Speed | Yes | (FamiTracker tempo only) The FamiTracker speed.
PatternCustomSettings | Time | Yes | Index of the column of pattern that uses these custom settings.
| Length | Yes | The custom length (in notes or frames) of the pattern.
| NoteLength | Yes | (FamiStudio tempo only) The number of frames in a note. 
| BarLength | Yes | (FamiStudio tempo only) The number of notes in a bar.
Channel | Type | Yes | Square1, Square2, Triangle, Noise, DPCM, VRC6Square1, VRC6Square2, VRC6Saw, VRC7FM1, VRC7FM2, VRC7FM3, VRC7FM4, VRC7FM5, VRC7FM6, FDS, MMC5Square1, MMC5Square2, MMC5DPCM, N163Wave1, N163Wave2, N163Wave3, N163Wave4, N163Wave5, N163Wave6, N163Wave7, N163Wave8, S5BSquare1, S5BSquare2 or S5BSquare3 
Pattern | Name | Yes | Name of the pattern.
Note | Time | Yes | The frame (or note) number inside the pattern where this note is.
| Value | | Note from C0 to B7, Stop or Release.
| Instrument | | (Only if note has a musical value, from C0 to B7) Name of the instrument to use.
| Arpeggio | | Optional, name of the arpeggio to use.
| Attack | | If the note has an attack, True of False.
| Volume | | The volume of the note, 0 to 15.
| VibratoSpeed | | The Vibrato speed, 0 to 12.
| VibratoDepth | | The Vibrato depth, 0 to 15.
| FinePitch | | The fine pitch, -128 to 127.
| SlideTarget | | The slide note target, from C0 to B7.
| FdsModSpeed | | FDS modulation speed, 0 to 4095.
| FdsModDepth | | FDS modulation depth, 0 to 63.
PatternInstance | Time | Yes | Index of the column of patterns where to place this instance.
| Pattern | Yes | Name of the pattern to instantiate.

## FamiStudio / FamiTone2 Music Code

Exporting music to the [FamiStudio Sound Engine](soundengine.md) or [FamiTone2](https://shiru.untergrund.net/code.shtml) work very similarely. It will generate assembly code that can be included in your homebrew project and played using the FamiTone2 sound engine. 

![](images/ExportFamiTone2.png#center)

When exporting file in separate files, you can specific a name format template for each song. The {project} and {song} macros are available.

When exporting as a single file (non-separate), you will be prompt to name the output assembly file. If any of the exported songs uses DPCM samples, a .dmc file of the same name will also be outputted.

## FamiStudio / FamiTone2 Sound Effect Code

The same goes for sound effect export. In this mode, one song is one sound effect, so songs should be very short. Unlike the FamiStudio/FamiTone2 music format which only support a subset of all the FamiTracker/FamiStudio features, here any feature (except DPCM samples) can be used and the sound effect should export correctly.

![](images/ExportFamiTone2SFX.png#center)

# Importing Songs

To import songs, simply open a file. On Windows it is also possible drag & drop a file in the application.

## FamiTracker Text or Binary

Import from FamiTracker (official 0.4.6 only) is supported through the text (TXT file) or binary format (FTM file). 

Note that only a small subset of features is supported. Only the following effects are supported. Every other effect will be ignored:

* 0xy (Arpeggio) : Will be converted to arpeggio, name will be based one the xy value.
* 1xx/2xx (Slide up/down) : Will be converted to slide notes
* 3xx (Portamento) : Will be converted to slide notes.
* 4xy (Vibrato) : Vibrator speed will be slightly modified, see table above for mapping.
* Bxx (Jump) : Will be converted to loop point. 
* Cxx (Half) : Will truncate the song at the location of the effect and remove the loop point.
* Dxx (Skip) : Will be converted to a custom pattern length.
* Fxx (Speed) : Only speed is supported.
* Pxx (Fine pitch) : Fully supported.
* Hxx (FDS Modulation depth) : Fully supported.
* Ixx/Jxx (FDS Modulation speed) : Fully supported, but combined in one 16-bit value.

Besides effects, there are also other limitations:

* When the limit of 16KB is reached, all subsequent DPCM samples will stop loading.
* Namco 163 instrument can only have a single waveform. Any other waveform than zero will be ignored.
* VRC7 1xx/2xx/3xx/Qxx/Rxx effects will likely not sound like FamiTracker and will need manual corrections.
* Instruments using both pitch and arpeggio envelopes at the same time will not sound the same as in FamiTracker. This is due to the vastly different way both applications handles these. FamiTracker re-triggers the pitch envelope at each arpeggio notes (probably the more sensible way), while FamiStudio simply runs both at the same time.
* VRC6 saw channel is not influenced by duty cycle in FamiStudio. FamiStudio always allow the full volume range for the saw. Import/export process does not try account for this. This might lead to volume inconsistencies between FamiTracker and FamiStudio where the volume needs to be doubled or halved to sound correct.
* Vibrato effect might sound a bit different, please see table above for exact mappings.

When importing from FamiTracker, all possible slide effects (1xx, 2xx, 3xx, Qxx and Rxx) will be converted to slide notes. Sometimes attack will be disabled as well to mimic the same behavior. This in an inherently imperfect process since they approaches are so different. For this reason, importing/exporting slide notes with FamiTracker should be considered a **lossy** process.

## FamiStudio Text 

The FamiStudio text format is support, please see the documentation above for the format specification.

## Nintendo Sound Format

NSF (and NSFE) files can be imported in FamiStudio. This includes NSF with expansion audio. When opening and NSF file, you will be presented a dialog box.

![](images/ImportNsf.png#center)

You will need to chose the song to import and specify a duration (in sec) to extract from the NSF. Most NSF file will not contain the names of songs so they will usually have placeholder names. 

NSF file are essentially programs designed to run on actual hardware. They will contain no instrument, no envelopes, no repeating pattern, etc. All the volumes and pitches will be set through the effect panel. They are going to appear extremely messy and unoptimized. 

That being said, NSF can still be extremely useful to reverse engineer how songs were made. In fact, this is how most of the Demo Songs that come with FamiStudio were made. 

In the example below, we can imagine that the notes on the right were all using the same instrument with a decreasing volume envelope. The notes of the left were clearly using a vibrato effect or a pitch envelope. 

![](images/NsfMess.png#center)

Only features that are supported by FamiStudio will be imported. Things like hardware sweeps & advanced DPCM manipulations will all be ignored.

# Error Log

When importing/exporting from/to specific format, an error log may appear providing a list of warnings/errors that occured during the process. Please pay attention to these as they may explain why your song sound differently than intended.

![](images/ErrorLog.png#center)
