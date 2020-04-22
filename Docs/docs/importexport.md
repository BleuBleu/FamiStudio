# Exporting Songs

The export dialog is access through the main toolbar.

## Wave File

Only a single song can be exported at a time. You can choose the sample rate, it is recommended to stick to 44.1KHz if you want the soung to be exactly as you hear it in FamiStudio. Lower sample rate might lack high frequencies.

When exporting to WAV, the song will simply play once fully, all jump effects will be ignored.

![](images/ExportWav.png#center)

## Nintendo Sound Format

Every feature supported by FamiStudio can be used in an NSF. 

![](images/ExportNsf.png#center)

Song that do not use any audio expansion can export as PAL and Dual mode, where as only NTSC is available for expansion audio.

The maximum song sizet is approximately 28KB minus the size of the DPCM samples used. Note that this size are not printed anywhere and are not related to the size of the *.fms file. Best to simply try and see if it works.

## ROM

Song(s) can be exported to a NES ROM file to play if back on an emulator or on actual hardware. The current iteration uses [Mapper 31](https://wiki.nesdev.com/w/index.php/INES_Mapper_031) which is supported by most emulators and the [Everdrive N8](https://krikzz.com/store/home/31-everdrive-n8-nes.html). You can then copy the ROM to an SD card and listen to it on actual hardware. Note that expansion audio is not supported with this mapper.

![](images/Rom.png#center)

The user interface is very simple, pressing left/right on the D-PAD will change the current track. Track/song names will be truncated to 28 characters.

For something a lot more customizable, check out [EZNSF](https://github.com/bbbradsmith/eznsf) by Brad Smith.

## FamiTracker Text

You can export songs to FamiTracker using their Text Export format.

![](images/ExportFamiTracker.png#center)

There are some limitations:

* Pitch envelopes with looping sections will be modified on export so that the looping part sums to zero. This is done to prevent pitch from drifting up/down every time the envelope loops. The reason for this is that FamiTracker's pitch envelopes are relative while FamiStudio's are absolute.

* Instruments using both pitch and arpeggio envlopes at the same time will not sound correct in FamiTracker. This is due to the vastly different way both applications handles these. FamiTracker re-triggers the pitch envelope at each arpeggio notes (probably the more sensible way), while FamiStudio simply runs both at the same time.

* For slide notes, FamiStudio will do its very best to choose which FamiTracker effect to use. Note that importing/exporting slide notes with FamiTracker should be considered a **lossy** process. Here are the general rules:
	* If the slide note and its target are within 16 semitones, Qxx/Rxx (note slide up/down) will be favored as it is the most similar effect to what we are doing.
	* Otherwise, if the previous note has the same pitch as the slide note, 3xx (auto-portamento) will be used.
	* Finally, if none of these conditions are satisfied, 1xx/2xx (slide up/down) will be used. This is not ideal since the pitch might not exactly match the target note.<br><br>

* Slide notes of VRC7 will definately not export perfectly due to the different way in which both applications handle VRC7 pitches.

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
		* **PatternCustomSettings** : Custom length or tempo settings of a pattern colume.
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
		Song Name="Tutorial Song" Length="5" LoopPoint="1" PatternLength="16" BarLength="4" NoteLength="7" PalSkipFrames="3,-1"
			PatternCustomSettings Time="0" Length="10" NoteLength="7" BarLength="4" PalSkipFrames="3,-1"
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
| Vrc7Reg{1 to 8}) | | (VRC7 instrument only, if Vrc7Patch is 0) Values of the 8 custom patch registers.
Envelope | Type | Yes | Volume, Arpeggio, Pitch, DutyCycle, FDSWave, FDSMod or N163Wave.
| Length | | Length of the envelope, 0 to 255.
| Loop | | The loop point of the envelope.
| Release | | (Volume envelope only) The release point of the envelope.
| Relative | | (Pitch envelope only) True or False.
| Values | Yes | Coma seperated values of the envelopes -64 to 63 range.
Song | Name | Yes | The name of the song.
| Length | Yes | The length of the song in number of patterns.
| LoopPoint | Yes | The loop point of the song, -1 to disable.
| PatternLength | Yes | The number of notes in a pattern.
| BarLength | Yes | The number of notes in a bar.
| NoteLength | Yes | (FamiStudio tempo only) The number of frames in a note. 
| PalSkipFrames | Yes | (FamiStudio tempo only) The pal skip frames. 1 value if NoteLength < 10, 2 otherwise.
| Tempo | Yes | (FamiTracker tempo only) The FamiTracker tempo.
| Speed | Yes | (FamiTracker tempo only) The FamiTracker speed.
PatternCustomSettings | Time | Yes | Index of the column of pattern that uses these custom settings.
| Length | Yes | The custom length (in notes or frames) of the pattern.
| NoteLength | Yes | (FamiStudio tempo only) The number of frames in a note. 
| BarLength | Yes | (FamiStudio tempo only) The number of notes in a bar.
| PalSkipFrames | Yes | (FamiStudio tempo only) The pal skip frames. 1 value if NoteLength < 10, 2 otherwise.
Channel | Type | Yes | Square1, Square2, Triangle, Noise, DPCM, VRC6Square1, VRC6Square2, VRC6Saw, VRC7FM1, VRC7FM2, VRC7FM3, VRC7FM4, VRC7FM5, VRC7FM6, FDS, MMC5Square1, MMC5Square2, MMC5DPCM, N163Wave1, N163Wave2, N163Wave3, N163Wave4, N163Wave5, N163Wave6, N163Wave7, N163Wave8, S5BSquare1, S5BSquare2 or S5BSquare3 
Pattern | Name | Yes | Name of the pattern.
Note | Time | Yes | The frame (or note) number inside the pattern where this note is.
| Value | | Note from C0 to B7, Stop or Release.
| Instrument | | (Only if note has a musical value, from C0 to B7) Name of the instrument to use.
| Attack | | If the note has an attack, True of False.
| Volume | | The volume of the note, 0 to 15.
| VibratoSpeed | | The Vibrato speed, 0 to 12.
| VibratoDepth | | The Vibrato depth, 0 to 15.
| FinePitch | | The fine pitch, -128 to 127.
| SlideTarget | | The slide note target, from C0 to B7.
| FdsModSpeed | | FDS modulation speed, 0 to 4095.
| FdsModDepth | | FDS modulation depth, 0 to 63.
PatternInstance | Time | Yes | Index of the column of patterns where to place this instance.
| Pattern | Yes | Name of the pattern to instanciate.

## FamiTone2 Assembly Code

Exporting to FamiTone2 works in the same way as the command line tools provided by Shiru.

![](images/ExportFamiTone2.png#center)

When exporting file in seperate files, you can specific a name format template for each song. The {project} and {song} macros are available.

When exporting as a single file (non-seperate), you will be prompt to name the output assembly file. If any of the exported songs uses DPCM samples, a .dmc file of the same name will also be outputted.

# Importing Songs

To import songs, simply open a file. On Windows it is also possible drag & drop a file in the application.

## FamiTracker Text or Binary

Import from FamiTracker (official 0.4.6 only) is supported through the text (TXT file) or binary format (FTM file). 

Note that only a small subset of features is supported. Only the following effects are supported. Every other effect will be ignored:

* 1xx/2xx (Slide up/down) : Will be converted to slide notes
* 3xx (Portamento) : Will be converted to slide notes.
* 4xy (Vibrato) : Vibrator speed will be slightly modified, see table above for mapping.
* Bxx (Jump) : Will be converted to loop point. 
* Dxx (Skip) : Will be converted to a custom pattern length.
* Fxx (Speed) : Only speed is supported.
* Pxx (Fine pitch) : Fully supported.
* Hxx (FDS Modulation depth) : Fully supported.
* Ixx/Jxx (FDS Modulation speed) : Fully supported, but combined in one 16-bit value.

Besides effects, there are also other limitations:

* Namco 163 instrument can only have a single waveform. Any other waveform than zero will be ignored.
* Namco 163 instrument only support wave size 4, 8, 16 and 32 and wave positions that are multiples of these sizes. Other values might lead to undefined behaviors.
* VRC7 1xx/2xx/3xx/Qxx/Rxx effects will likely not sound like FamiTracker and will need manual corrections.

When importing from FamiTracker, all possible slide effects (1xx, 2xx, 3xx, Qxx and Rxx) will be converted to slide notes. Sometimes attack will be disabled as well to mimic the same behavior. This in an inherently imperfect process since they approaches are so different. For this reason, importing/exporting slide notes with FamiTracker should be considered a **lossy** process.

## FamiStudio Text 

The FamiStudio text format is support, please see the documentation above for the format specification.

## Nintendo Sound Format

NSF (and NSFE) files can be imported in FamiStudio. This includes NSF with expansion audio. When opening and NSF file, you will be presented a dialog box.

![](images/ImportNsf.png#center)

You will need to chose the song to import and specify a duration (in sec) to extract from the NSF. Most NSF file will not contain the names of songs so they will usually have placeholder names. 

NSF file are essentially programs designed to run on actual hardware. They will contain no instrument, no envelopes, no repeating pattern, etc. All the volumes and pitches will be set through the effect panel. They are going to appear extremely messy and unoptimized. 

That being said, NSF can still be extremely useful to reverse engineer how songs were made. In fact, this is how most of the Demo Songs that come with FamiStudio were made. 

In the example below, we can imagine that the notes on the right were all using the same instrument with decreasing volume. The notes of the left were clearly using a vibrato effect or a pitch envelope. 

![](images/NsfMess.png#center)

Only features that are supported by FamiStudio will be imported. Things like hardware sweeps & advanced DPCM manipulations will all be ignored.


