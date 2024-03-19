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
* Fxx (Speed) : Only speed is supported. Speed changed are not allowed to be delayed with Gxx. 
* Gxx (Note delay) : Fully supported.
* Pxx (Fine pitch) : Fully supported.
* Hxx (FDS Modulation depth) : Fully supported.
* Ixx/Jxx (FDS Modulation speed) : Fully supported, but combined in one 16-bit value.
* Sxx (Delayed cut) : Fully supported. 
* Vxx (Timbre) : Supported, but only affects the duty cycle of 2A03 and VRC6 square channels. Does not affect Saw or anything else.
* Zxx (Delta counter) : Fully supported.

Besides effects, there are also other limitations:

* VRC7 1xx/2xx/3xx/Qxx/Rxx effects will likely not sound like FamiTracker and will need manual corrections.
* Instruments using both pitch and arpeggio envelopes at the same time will likely not sound the same as in FamiTracker. This is due to the vastly different way both applications handles these. FamiTracker resets the pitch at each arpeggio notes, while FamiStudio does not. 
* VRC6 saw channel is not influenced by duty cycle in FamiStudio. FamiStudio has a "saw master volume" on VRC6 instruments. Import/export process does not try account for this. Manual corrections may be needed.
* Vibrato effect might sound a bit different, please see table above for exact mappings.

When importing from FamiTracker, all possible slide effects (1xx, 2xx, 3xx, Qxx and Rxx) will be converted to slide notes. Sometimes attack will be disabled as well to mimic the same behavior. This in an inherently imperfect process since they approaches are so different. For this reason, importing/exporting slide notes with FamiTracker should be considered a **lossy** process.

## MIDI

Standard MIDI files can be imported with the desktop version of FamiStudio. 

Notes, time signature and tempo changes will be imported. Only blank instruments, named after their GM instrument, will be created. Users should not expect the imported song to sound anything like the original. 

Moreover, since the NES can only play one note at a time for a given channel, any kind of polyphony is not supported. Users are responsible for properly processing their MIDI files before importing them. That being said, this can be a handy feature to bring notes from an old project in FamiStudio.

![](images/ImportMIDI.png#center)

Available options:

* **Expansion**: Allows selecting the expansion audio to set for the project. This will add extra sound channels.
* **Polyphony behavior**: Although polyphony is not supported, if it were to happen, there are 2 ways FamiStudio can resolve it:
	* **Favor most recent note**: If a new note is triggered while one was still playing, the old note will be stopped and the new note will start.
	* **Favor currently playing note**: If a new note is triggered while one was still playing, the old note will keep playing and the new note will be ignored.
* **Measures per pattern**: Having a single measure per pattern tends to create too many patterns. FamiStudio can try to pack multiple measures in a single pattern. Note that tempo/time signature changes will always cause a new pattern to start.
* **Import velocity as volume**: Will convert velocity values to volume effects. 
* **Create PAL project**: Will create a PAL project and do all BPMs calculations using PAL speeds.

At the bottom of the dialog is the channel mapping table. For each of the NES channels, you can specific which MIDI data should be put in that channel. Double-clicking on a NES channel will bring up more options. 

![](images/ImportMIDIChannel.png#center)

There are 3 MIDI sources from which MIDI data can be read from:

* **Channel**: Data from the specified MIDI channel will be assigned to the NES channel. This is basically a 1:1 mapping between NES an channel and a MIDI channel.
* **Track**: If the MIDI file has proper tracks defined, data from the that track can be assigned to the NES channel.
* **None**: Will not read any data in this channel. The NES channel will be left blank.

The MIDI channel 10 is special an specific keys can be selected to filter specific drum sounds.

## FamiStudio Text 

The FamiStudio text format is supported, please see the documentation above for the format specification.

## Nintendo Sound Format

NSF (and NSFE) files can be imported in the desktop version of FamiStudio. This includes NSF with expansion audio. When opening and NSF file, you will be presented a dialog box.

![](images/ImportNsf.png#center)

Most NSF files will not contain the names of songs, so they will usually have placeholder names. Besides the song to import, are the most important settings :

* **Duration** : Time (in sec) to extract from the NSF. 
* **Pattern Length** : Number of frames in a pattern.
* **Start frame** : Used to offset the entire song by a number of frames. This is useful when a song has an intro that is not the same length as the other patterns.
* **Remove intro silence** : Some songs start with a bit of silence, this will wait until any sound is produced to start recording.
* **Reverse DPCM bits** : This will set the "Reverse Bits" flag on all the imported samples. This come from a recent discovery that quite a few games had packed their bits in the wrong order, leading to samples sounding worse than they should.
* **Preserve DPCM padding bytes** : Force FamiStudio to keep the last byte of every sample, this will make all samples 16 bytes larger simply to keep an extra byte. This could be useful to keep looping samples intact. Should remain off most of the time since most games seem to ignore this byte.

NSF file are essentially programs designed to run on actual hardware. They will contain no instrument, no envelopes, no repeating pattern, etc. All the volumes and pitches will be set through the effect panel. They are going to appear extremely messy and unoptimized. 

That being said, NSF can still be extremely useful to reverse engineer how songs were made. In fact, this is how most of the Demo Songs that come with FamiStudio were made. 

In the example below, we can imagine that the notes on the left were all using the same instrument with a decreasing volume envelope. The note of the right clearly was using a vibrato effect or a pitch envelope. 

![](images/NsfMess.png#center)

Only features that are supported by FamiStudio will be imported. Things like hardware sweeps & advanced DPCM manipulations will all be ignored.

## VGM Files

It's possible to import VGM/VGZ files containing supported sound chips.

Import options:

* **Pattern Length** : Number of frames in a pattern.
* **Skip frames at start** : Used to remove frames in the start of the track, can be useful if the song dont start right away.
* **Reverse DPCM bits** : This will set the "Reverse Bits" flag on all the imported samples. This come from a recent discovery that quite a few games had packed their bits in the wrong order, leading to samples sounding worse than they should.
* **Preserve DPCM padding bytes** : Force FamiStudio to keep the last byte of every sample, this will make all samples 16 bytes larger simply to keep an extra byte. This could be useful to keep looping samples intact. Should remain off most of the time 
* **Adjust notes to match chip clock in VGM** : Used to convert notes where the sound chip clock being imported does not match the frequency used on the chip on the NES/Expansion.
* **Import YM2149 as EPSM** : Imports YM2149/AY-3-8910 As EPSM Squares instead of importing them as S5b that is the default behaviour.

Supported Chips:

* **NES APU** : Imported as 2A03/2A07
* **FDS** : Imported as FDS
* **YM2149/AY-3-8910** : Can be imported as S5b or EPSM
* **YM2203 (OPN)**
  **YM2608 (OPNA)**
  **YM2612 (OPN2)**
  **YM2610 (OPNB)**
  **YMF288 (OPN3)** : Can be imported as EPSM
* **VRC7**
  **YM2413 (OPLL)** : Can be imported as VRC7

# Error Log

When importing/exporting from/to specific format, an error log may appear providing a list of warnings/errors that occurred during the process. Please pay attention to these as they may explain why your song sound differently than intended.

![](images/ErrorLog.png#center)

The mobile version does not display these types of errors. So if you encounter a weird behavior, opening the file with the desktop version may help diagnose the issue.
