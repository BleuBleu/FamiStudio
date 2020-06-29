# Concepts

A FamiStudio project contains:

* A list of Songs
* A list of Instruments
* A list of DPCM samples

Songs are made of Patterns, which are on one of the five Channels supported by the NES. Patterns contain Notes which are played by an Instrument (DPCM samples do not require an instrument). Instruments may have some of their attributes (pitch, volume, arpeggio) modulated by Envelopes.

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

The sequencer and piano roll will display the information for the currently selected song. When pressing the keys on the piano roll, it will play notes for the currently selected instrument, and output it on the currently selected channel. The same applies if you have a MIDI keyboard connected.

# Toolbar

The main toolbar contains your usual stuff: file operation, undo/redo, timecode and play control.

![](images/Toolbar.png#center)

## Tooltip

Keyboard shortcuts or special actions are always displayed as tooltips in the upper-right corner of the toolbar. Please keep an eye on it to learn new functionalities.

![](images/Tooltip.png#center)

## Playing/pausing the song

Besides the toolbar, space bar is used to play/pause the song. Ctrl-space plays in pattern loop mode, Shift-space plays in song loop mode.

## Changing the looping mode

There are 3 looping modes:

* **Song**: loops the entire song
* **Pattern**: loops at the end of the current pattern
* **None**: stops at the end of the song

## Saving the project

Clicking on the icon save the project, right-clicking is a "save as..." and will prompt you for a new filename.

## Exporting to various formats

Please see the [Import/Export](importexport.md) section for details on how to export to various formats.

## Cleanup dialog

Please see the [Cleanup](cleanup.md) section for details on how to cleanup your project.

## Configuration dialog

Please see the [Configuration](config.md) section for details on how to configure FamiStudio.

# Project Explorer

The project explorer displays the list of songs and instruments in the current project.

![](images/ProjectExplorer.png#center)

For more details on what you can do with the Project Explorer, please check out these sections:
* [Editing Songs & Project](song.md)
* [Editing Instruments](instruments.md)

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
* **Ctrl+Space**: Plays from beginning of current pattern.
* **Shift+Space**: Plays from beginning of the song.
* **Home**: Seeks back to beginning of the song.
* **Ctrl+Home**: Seeks to beginning of the current pattern.
* **Esc**: Stops any stop, clears the selection.
* **Ctrl+Z**: Undo
* **Ctrl+Y**: Redo
* **Ctrl+N**: New project
* **Ctrl+S**: Save
* **Ctrl+E**: Export
* **Ctrl+O**: Open
* **Delete**: Delete selected patterns or notes.
* **Escape**: Deselects patterns or notes, stops any sound that is stuck playing.
* **1...5**: Changes the active channel (more than 5 if you have expansion audio enabled).
* **Ctrl+1...5**: Force display a channel. (more than 5 if you have expansion audio enabled).

Some keyboard shortcuts specific to the sequencer:

* **L+Click**: Sets the loop point at the clicked pattern.

Some keyboard shortcuts specific to the piano roll:

* **Ctrl+Click**: Adds a stop note.
* **Shift+Click**: Adds a release note.
* **S+Click** (and drag): Creates or edit a slide note.
* **A+Click**: Toggles the attack of a note.
* **~ (Tilde)**: Expand/collapse the effect panel.
* **Shift+S**: Toggles snapping.
