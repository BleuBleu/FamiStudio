# Configuring FamiStudio

The configuration dialog is accessed from the main toolbar.

## Configuration Dialog

The configuration dialog dialog is accessed from the toolbar.

## General Configuration

![](images/ConfigGeneral.png#center)

* **Check for updates**: At startup FamiStudio checks for new version online. This can be disabled.

* **Trackpad controls**: Enabling trackpad controls will switch to a control scheme that is better suited for trackpad users:

    * Vertical mouse wheel up/down will scroll up/down instead of zooming in/out. This is the equivalent of swiping your fingers up/down on the trackpad.
    * Horizontal mouse wheel will scroll left/right. This is equivalent of swiping your fingers left/right on the trackpad.
    * Pinch to zoom (or alternatively CTRL + MouseWheel) will zoom in/out. If pinch-to-zoom does not work on Windows, please check your trackpad drivers, this is not an issue with FamiStudio.

    Note that these are poorly supported on Linux due to the fact that the app uses GTK 2 and gestures are generally poorly supported in Linux.

* **Clear Undo/Redo on save**: Wipes the undo/redo stack every time the project is saved. This help keep the memory usage lower, but limits your ability to undo indefinately.

* **Open last project on start**: Remember which project you last open and re-opens it next time you launch FamiStudio.

* **Auto-save a copy every 2 minutes**: Save a backup copy of the current project every 2 minutes. This may prevent loosing data when the application crashes.

## User Interface Configuration

![](images/ConfigUI.png#center)

* **Scaling**: By default, FamiStudio will use the scaling of your primary monitor on Windows (100%, 150% and 200% are support) and on macOS it will choose between 100% or 200% depending on if you have a retina display or not. This behavior can be overriden by a scaling of your choosing. This requires restarting the app:

* **Time Format**: Allow changing the format of the timer in the main toolbar of the application. 

* **Follow Mode**: Select which type of follow mode to use:

    * **Jump**: Once the play position reaches the right edge of the screen, advances by a full screen.
    * **Continuous**: Once the play position is at least 75% of the way to the right, starts scrolling smoothly.<br><br>

* **Follow Views**: Select which views to scroll:

    * **Sequencer**: Only enables follow mode in the sequencer.
    * **Piano Roll**: Only enables follow mode in the piano roll.
    * **Both**: Enables follow mode on both controls.<br><br>

* **Scroll Bars**: Display scrolls bars in the Sequencer and the Piano Roll.

    * **None**: No scroll bares.
    * **Thin**: Small scroll bars.
    * **Thick**: Large scroll bars.<br><br>

* **Show Piano Roll View Range**: Displays a yellow rectangle in the sequencer representing the view range of the piano roll.

* **Show Note Labels**: Displays note labels (e.q. "C#3") in the piano roll.

* **Show FamiTracker Stop Notes**: When using FamiTracker tempo mode, display implicity stop notes (whenever a note ends without being interrupted by another note) as little triangles. This makes it easier to set note delays for those.

* **Show Oscilloscope**: Display an oscilloscope in the toolbar.

* **Force Compact Sequencer**: Always keep the size of the channels in the sequencer to their minimum height. This is useful on monitor with very low resolution. 

## Sound Configuration

![](images/ConfigSound.png#center)

* **Number of buffered frames** : Number of NES/Famicom frames to buffer ahead of time. Higher values help prevent audio starvation, but also raise latency. The minimum is 2 and requires a relatively fast CPU to run without audio starvation.

* **Stop instrument after**: When instruments have release notes, there is no way for FamiStudio to know when to stop the notes. This allows stopping any sound after a specified number of seconds. This only applies to MIDI or when previewing instruments on the piano roll and has no impact on the actual song.

* **Prevent popping on square channels**: The NES/Famicom had a bug where the phase of square channels will reset around some notes (A-3, A-2, D-2, A-1, F-1, D-1, and B-0 on NTSC, or A#3, A#2, D#2, A#1, F#1, D#1, and C-0 on PAL), resulting in audible clicks or pops. This option will work around that bug using the Smooth Vibrato technique by Blargg, resulting in smooth pitch changes. Note that this option will not carry over to FamiTracker if you export.

* **Mute piano roll interactions during playback** : When enabled, dragging/adding notes in the piano roll will not preview the notes when the song is playing. Some users find this distracting.

## Mixer

![](images/ConfigMixer.png#center)

This section allows adjusting the global volume of FamiStudio and the volume/treble of each audio expansion in the FamiStudio NES sound emulation. The global audio volume may need to be lowered to avoid clipping when using a massive amount of audio expansions at the same time.

Audio expansion volume is a tricky subject since even different revisions of the Famicom had different resistor values which dramatically affected the volume of expansion audio. Here you can set any value you want.

The treble (low-pass filter) has a logarithmic rolloff to treble dB at half sampling rate. Negative values reduce treble, small positive values (0 to 5.0) increase treble. FamiStudio emulates audio at 44.1KHz.

Note that these settings have no effect outside of FamiStudio.

## MIDI Configuration

![](images/ConfigMIDI.png#center)

* **Device**: Allows choosing the MIDI device to use for previewing instruments.

## FFMpeg Configuration

![](images/ConfigFFMpeg.png#center)

FFMpeg is a video encoder required to enable video export. You can download it from [here](ffmpeg.md). Once downloaded and installed somewhere on your computer, you will need to tell FamiStudio where it is.

## QWERTY Configuration

![](images/ConfigQWERTY.png#center)

This section allows remapping the keyboard keys for keyboard-piano input. This allows FamiStudio to support AZERTY, QWERTZ or any other international keyboard. Up to 2 keys can be assigned to a note.

## Mobile Configuration

![](images/ConfigMobile.png#center)

This section is only available on the Mobile version of FamiStudio.

* **Allow vibration**: Toggle for all vibration effect in the app.
* **Double-tap to delete**: Experimental option to delete pattern/notes with a double-tap.

# Configuration File

All FamiStudio settings are saved in "FamiStudio.ini". The location of this file will vary depending on the operating system. 

* On Windows it will be in `AppData\Local\FamiStudio`, unless you are using the portable version, in which case it will be in the root folder.
* On Linux it will be in `~/.config/FamiStudio`
* On MacOS it will be in `~/Library/Application Support/FamiStudio`
* On Android it is not user accessible.

## Undocumented Settings

Almost all settings are configurable through the user interface. This section will contain some settings that can only be changed directly in the INI file.

Value | Description
--- | ---
SeparateChannelsExportTndMode | This define if the triangle-noise-DPCM volume interactions should be emulated when exporting to a WAV file using separate channels (1 WAV file per channel). Possible values are 0, 1 and 2. <br/>- 0 (default) will not emulate those at all<br/>- 1 will emulated them fully, which will give the correct triangle channel volume but will also make the three channels bleed into each other slightly.<br/>- 2 will allow the triangle/noise to be affected by the DPCM, but not the other way around. This will give the correct triangle volume, but will prevent bleeding into the DPCM channel.

