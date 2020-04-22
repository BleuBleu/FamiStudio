# Configuring FamiStudio

The configuration dialog is accessed from the main toolbar.

## Configuration Dialog

The configuration dialog dialog is accessed from the toolbar.

## User Interface Configuration

![](images/ConfigUI.png#center)

* **Scaling**: By default, FamiStudio will use the scaling of your primary monitor on Windows (100%, 150% and 200% are support) and on macOS it will choose between 100% or 200% depending on if you have a retina display or not. This behavior can be overriden by a scaling of your choosing. This requires restarting the app.

* **Check for updates**: At startup FamiStudio checks for new version online. This can be disabled.

## Sound Configuration

![](images/ConfigSound.png#center)

* **Stop instrument after**: When instruments have release notes, there is no way for FamiStudio to know when to stop the notes. This allows stopping any sound after a specified number of seconds. This only applies to MIDI or when previewing instruments on the piano roll and has no impact on the actual song.

* **Prevent popping on square channels**: The NES had a bug where the phase of square channels will reset around some notes (A-3, A-2, D-2, A-1, F-1, D-1, and B-0 on NTSC, or A#3, A#2, D#2, A#1, F#1, D#1, and C-0 on PAL), resulting in audible clicks or pops. This option will work around that bug using the Smooth Vibrato technique by Blargg, resulting in smooth pitch changes. Note that this option will not carry over to FamiTracker if you export.

## MIDI Configuration

![](images/ConfigMIDI.png#center)

* **Device**: Allows choosing the MIDI device to use for previewing instruments.
