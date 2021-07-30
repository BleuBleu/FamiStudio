# Concepts

A FamiStudio project contains:

* A list of Songs
* A list of Instruments
* A list of DPCM samples
* A list of Arpeggios

Songs are made of Patterns, which are on one of the five Channels supported by the NES. Patterns contain Notes which are played by an Instrument (DPCM samples do not require an instrument) and may refer to an arpeggio. Instruments may have some of their attributes (pitch, volume, arpeggio) modulated by Envelopes.

Most of the operations are performed with the mouse. In general:

* The **left mouse button** adds stuff, double-clicking something edits properties (songs, patterns, instruments, DPCM)
* The **right mouse button** removes stuff. Right clicking in the header of the Sequencer or Piano Roll selects.
* The **middle mouse button** pans when you press it and zooms and you use the mouse wheel.

Alternatively, if your mouse does not have a middle mouse button or mouse wheel:

* All actions requiring pressing the middle mouse buttons can be done with Alt+Left click.
* All actions requiring the mouse wheel can be performed with Alt+Right click, then dragging up/down.

If you are working on a trackpad, please check out how to enable [Trackpad controls](config.md#user-interface-configuration) in the configuration dialog.

# Main Window

The UI was designed to be a simple as possible, there are almost no context menus.

The main window has 4 main components:

* The Toolbar (on top)
* The Project Explorer (on the right) is where you add/remove/edit songs and instruments.
* The Sequencer (below the toolbar) is where you schedule your patterns on one of the 5 channels. It gives a high-level view of the song.
* The Piano Roll (below the sequencer) is where you edit your patterns.

![](images/MainWindow.png#center)

At any given moment there is always:

* A selected channel, in bold in the sequencer
* A selected song, in bold in the project explorer song list
* A selected instrument, in bold in the project explorer instrument list.
* A selected arpeggio, in bold in the project explorer arpeggio list.

The sequencer and piano roll will display the information for the currently selected song. When pressing the keys on the piano roll, it will play notes for the currently selected instrument, and output it on the currently selected channel. The same applies if you have a MIDI keyboard connected.

# Toolbar

The main toolbar contains your usual stuff: file operation, undo/redo, timecode, oscilloscope and play controls.

![](images/Toolbar.png#center)

## Tooltip

Keyboard shortcuts or special actions are always displayed as tooltips in the upper-right corner of the toolbar. Please keep an eye on it to learn new functionalities.

![](images/Tooltip.png#center)

## Playing/pausing the song

Besides the toolbar, space bar is used to play/pause the song. Ctrl-space plays in pattern loop mode, Shift-space plays in song loop mode.

## Changing the playback speed

Rotating the mouse-wheel over the play button will allow you to change the playback speed to 1/2 or 1/4 speed. This is useful to hear very small/short notes.

![](images/PlaybackSpeed.gif#center)

## Changing the looping mode

There are 2 looping modes:

* **Song / None**: Loops the entire song if a loop point is step, stops at the end if there are no loop point.
* **Pattern** or **Selection**: Loops over the current pattern if there is no selection in the sequencer. If there is a selection, will loop over that selection. The icon in the toolbar will change to reflection this.

![](images/LoopingMode.png#center)

## Toggling the metronome

The metronome can be enabled by clicking the icon on the toolbar. The metronome will tick at every beat and will only work when the song is playing.

## Saving the project

Clicking on the icon save the project, right-clicking is a "save as..." and will prompt you for a new filename.

## Exporting to various formats

Please see the [Export](export.md) section for details on how to export to various formats.

## Cleanup dialog

Please see the [Cleanup](cleanup.md) section for details on how to cleanup your project.

## Configuration dialog

Please see the [Configuration](config.md) section for details on how to configure FamiStudio.

# Project Explorer

The project explorer displays the list of songs and instruments in the current project.

![](images/ProjectExplorer.png#center)

For more details on what you can do with the Project Explorer, please check out these sections:

* [Editing Songs & Project](song.md)
* [Editing Instruments & Arpeggios](instruments.md)

# Sequencer

The sequencer is where you organize the high-level structure of the song: which patterns play and when they play. The thumbnails of the patterns in the sequencer are by no mean accurate. Please visit the [Editing Patterns](sequencer.md) section for more details.

![](images/Sequencer.png#center)

# Piano Roll

The piano roll is where you editing the actual notes of the song, the instrument envelopes, as well as some special effects.

![](images/PianoRoll.png#center)

Please check out the [Editing Notes](pianoroll.md) section for more details.

# Keyboard shortcuts

Here is a list of useful keyboard shortcuts:

* **Space**: Play/stop the stop
* **Enter**: Toggles recording mode.
* **Ctrl+Space**: Plays from beginning of current pattern.
* **Shift+Space**: Plays from beginning of the song.
* **Ctrl+Shift+Space**: Plays from loop point of the song (if any).
* **Home**: Seeks back to beginning of the song.
* **Ctrl+Home**: Seeks to beginning of the current pattern.
* **Esc**: Stops any lindering sound, stops recording mode, clears the selection.
* **Ctrl+Z**: Undo
* **Ctrl+Y**: Redo
* **Ctrl+N**: New project
* **Ctrl+S**: Save
* **Ctrl+E**: Export
* **Ctrl+Shift+E**: Repeat last export
* **Ctrl+O**: Open
* **Shift+Q**: Toggle QWERTY keyboard input.
* **Delete**: Delete selected patterns or notes.
* **Escape**: Deselects patterns or notes, stops any sound that is stuck playing.
* **F1...F5**: Changes the active channel (more than 5 if you have expansion audio enabled).
* **Ctrl+F1...F5**: Force display a channel. (more than 5 if you have expansion audio enabled).

Some keyboard shortcuts specific to the sequencer:

* **L+Click**: Sets the loop point at the clicked pattern.

Some keyboard shortcuts specific to the piano roll:

* **Shift+Click**: Adds a release note.
* **S+Click** (and drag): Creates or edit a slide note.
* **A+Click**: Toggles the attack of a note.
* **I+Click**: Instrument picker
* **T+Click**: Adds an orphan stop note.
* **~ (Tilde)**: Expand/collapse the effect panel.
* **Shift+S**: Toggles snapping.

When QWERTY keyboard input is active, some of the keys will be overriden to support piano input. This is default layout for an en-US keyboard. The [Configuration Dialog](config.md) allows re-mapping the keys for other types of keyboards.

![](images/QWERTY.png#center)

