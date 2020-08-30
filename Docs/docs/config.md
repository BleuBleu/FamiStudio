# Configuring FamiStudio

The configuration dialog is accessed from the main toolbar.

## Configuration Dialog

The configuration dialog dialog is accessed from the toolbar.

## User Interface Configuration

![](images/ConfigUI.png#center)

* **Scaling**: By default, FamiStudio will use the scaling of your primary monitor on Windows (100%, 150% and 200% are support) and on macOS it will choose between 100% or 200% depending on if you have a retina display or not. This behavior can be overriden by a scaling of your choosing. This requires restarting the app:

* **Time Format**: Allow changing the format of the timer in the main toolbar of the application. 

* **Follow Mode**: Select which type of follow mode to use:

    * **Jump**: Once the play position reaches the right edge of the screen, advances by a full screen.
    * **Continuous**: Once the play position is at least 75% of the way to the right, starts scrolling smoothly.<br><br>

* **Follow Views**: Select which type of follow mode to use:

    * **Sequencer**: Only enables follow mode in the sequencer.
    * **Piano Roll**: Only enables follow mode in the piano roll.
    * **Both**: Enables follow mode on both controls.<br><br>

* **Check for updates**: At startup FamiStudio checks for new version online. This can be disabled.

* **Show Piano Roll View Range**: Displays a yellow rectangle in the sequencer representing the view range of the piano roll.

* **Trackpad controls**: Enabling trackpad controls will switch to a control scheme that is better suited for trackpad users:

    * Vertical mouse wheel up/down will scroll up/down instead of zooming in/out. This is the equivalent of swiping your fingers up/down on the trackpad.
    * Horizontal mouse wheel will scroll left/right. This is equivalent of swiping your fingers left/right on the trackpad.
    * Pinch to zoom (or alternatively CTRL + MouseWheel) will zoom in/out. If pinch-to-zoom does not work on Windows, please check your trackpad drivers, this is not an issue with FamiStudio.

    Note that these are poorly supported on Linux due to the fact that the app uses GTK 2 and gestures are generally poorly supported in Linux.

## Sound Configuration

![](images/ConfigSound.png#center)

* **Stop instrument after**: When instruments have release notes, there is no way for FamiStudio to know when to stop the notes. This allows stopping any sound after a specified number of seconds. This only applies to MIDI or when previewing instruments on the piano roll and has no impact on the actual song.

* **Prevent popping on square channels**: The NES had a bug where the phase of square channels will reset around some notes (A-3, A-2, D-2, A-1, F-1, D-1, and B-0 on NTSC, or A#3, A#2, D#2, A#1, F#1, D#1, and C-0 on PAL), resulting in audible clicks or pops. This option will work around that bug using the Smooth Vibrato technique by Blargg, resulting in smooth pitch changes. Note that this option will not carry over to FamiTracker if you export.

## MIDI Configuration

![](images/ConfigMIDI.png#center)

* **Device**: Allows choosing the MIDI device to use for previewing instruments.
