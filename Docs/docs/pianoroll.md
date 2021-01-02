# Editing Notes 

The Piano Roll is where you editing the actual notes of the song, the instrument envelopes, as well as some special effects.

![](images/PianoRoll.png#center)

You can also use it to preview instrument by clicking on the keyboard. The currently selected instrument (in the project explorer) will play on the currently selected channel (in the sequencer).

## Subdivision

Horizontal lines in the piano rolls are align with the notes of the piano. The vertical lines represents multiple levels of subdivisions:

* Thin dashes gray lines seperate individual NTSC frames, 1/60th of a sec. (FamiStudio tempo mode only)
* Thin gray lines seperate notes
* Thin black lines seperate beats
* Thick black lines seperate patterns

In FamiTracker tempo mode, you do not have access to the individual frames, so the dashes lines will not be visible.

![](images/PianoRollFrames.png#center)

## Seeking

Clicking in the timeline (header) of the piano roll will move the play position.

## Adding and deleting notes

Clicking a pattern in the sequencer will scroll the piano roll to its location. Left-clicking in the piano roll will add a note of the currently selected instrument. Holding the mouse when clicking will allow you to drag the note and will give audio feedback. You cannot add a note where the is not a pattern created.

Right-clicking deletes a note.

## Recoding mode

Recording mode is another way to input notes and is enabled by pressing the record button on the toolbar. Recording mode allows inputting notes using a MIDI controller or using a piano-type layout on the QWERTY keyboard. This layout is very similar to what FamiTracker uses. Note that this is not a "real-time" recording mode, simply a note-by-note input mode.

![](images/QWERTY.png#center)

The QWERTY keyboard input will give you a range of approximately 2.5 octaves. By default, this will go from C3 to C5 but it can be moved up/down by pressing **Page Up** and **Page Down**. Which keyboard key maps to which piano key will be displayed in the piano roll. 

![](images/RecordingPiano.png#center)

When recording mode is active, the seek bar will turn red. Each note you input will be snapped to the current snap precision (see next section for info about snapping) and the seek bar will advance to the next snapping position. 

There are a few other special keys that are enabled when recording mode is activated:

* **Backspace**: moves back by 1 note (or snapping interval), erasing any note inside the interval. 
* **Tab**: Advances by 1 note (or snapping interval), not recording anything.
* **1**: Inserts a stop note and advances.
* **Page Up/Down**: Moves the octave range that can be inputted using the QWERTY keyboard up/down.

## Snapping

Snapping can be toggle by clicking the little magnet in the top-left corner of the piano roll. **SHIFT+S** can also be used to quickly toggle snapping on/off. The precision of the snapping can be changed by left/right clicking the number of using the mousewheel over the number or magnet icon. 

![](images/Snap.png#center)

Using FamiStudio tempo mode will allow you to use both fractional snapping (1/4, 1/3, 1/2) and integer snapping (1, 2, 3, 4). FamiTracker tempo mode is limited to integer snapping since it does not give you full control over the individual frames. Fractional snapping will perform rounding when the number of frames in a note isnt exactly divisible by the precision.

Adding, selecting and dragging of notes are currently the only actions affected by snapping. More might be added in future based on user feedback.

## Stop & Release notes

Using Ctrl+click will add a stop note. Stops notes are displayed as little triangles. Although they are displayed next to the note preceding them, they actually have no pitch or instrument, they simply stop the sound. Stop notes are important because on the NES, a note will play indefinitely unless you tell it to stop.

![](images/StopNote.png#center)

Using Shift+click will add a release notes. Release notes are shown as making the note thinner and triggers the envelope to jump to the release point. Release envelopes are useful to nicely fade out a note when its release, while preserving other effects like vibrato. There is no point to adding a release note to an instrument that does not have a release envelope.

![](images/ReleaseNote.png#center)

Hovering the mouse in the piano roll will display the location and note in the toolbar. Hovering over a note will display which instrument it uses.

## Note attack

By default, notes will have an "attack" which mean they will restart their envelopes (volume, pitch, etc.) from the beginning. This is represented by the little dark rectangle on the left of each note. This can be toggled for a particular note by hold the A key and clicking on a note. Note that if a note does not use the same instrument as the previous one, the attack will still play, even if disabled. Also please note that this will generally not carry over to FamiTracker, besides specific use cases around slide notes.

In this example, the first note will have an attack, while the second one will not.

![](images/NoAttack.png#center)

Since envelopes are not resetted, this means that if a note was released, it will remain released if the subsequent notes have no attack.

![](images/NoAttackReleased.png#center)

## Slide notes

Slide notes are notes that start at a given pitch (the pitch of the note) and slowly change to hit a target pitch which is represented by where the triangle ends. In this example, the attack of the second note has been disabled.

![](images/SlideNote.png#center)

Slide notes garantees that the target pitch will be reached by the end of the note (end of the triangle) but this might happen a bit earlier than the visual repesentation suggests. Especially in the higher pitches. This is due to the fact that the pitch calculations are all integer-based (with 1-bit of fraction) and it is often impossible to get the exact required slope to reach the pitch at the exact time. 

## Arpeggios

Arpeggios (not to be confused with arpeggio instrument envelopes) are typically used to simulate chords by playing changing notes very rapidly. A note can optionally have an arpeggio associated to it, just like it has an instrument associated to it. When an arpeggio is used, the notes used by this arpeggio will be displayed in a semi-transparent way and the extra notes will take the color of the associated arpeggio. Note that in order to keep things simple, the individual sequence of notes inside the arpeggio will not be displayed.

![](images/Arpeggio.png#center)

You can change which arpeggio is associated with a note the same way to can replace an instrument. You can select a new arpeggio and click on an existing note. Or you can select a few notes and drag and drop an arpeggio from the Project Explorer on to the selection.

If an instrument uses an arpeggio envelope and also uses an arpeggio chord, the chord will take over and completely override the arpeggio envelope of the instrument. 

## Selecting and editing notes

You can select notes by right-clicking and dragging in the header of the piano roll. Selected notes will appear with a thick silver border. Once notes are selected, then can be moved using the arrows keys (up, down, left and right). Holding CTRL while doing so will make the notes move by larger increments.

![](images/SelectNotes.png#center)

## Cutting, copying and pasting notes

Much like the sequencer, selcted notes can be copy (or cut) by pressing CTRL+C (or CTRL+X). You can then move the selection somewhere else and paste the notes with CTRL+V.

## Special paste

"Special paste" is a more advanced form of pasting. It is used to to do things like pasting notes without their associated effets or volume track (or vice-versa), or to just paste specific effects. You can use a "special paste" by pressing CTRL+SHIFT+V. This will open a popup dialog.

![](images/PasteSpecial.png#center)

* **Mix With Existing Notes** : The default behavior of pasting is to completely replace everything by the content of the clipboard. Using the "Mix with existing notes" option will preserve any existing data (notes, volumes, effects) and only insert new data if there is nothing there already.

* **Paste Notes** : Will paste the actual notes (including slide notes and arpeggio information).

* **Paste Effects** : You can choose the list of effect that you with to paste. Unchecked effect will not be pasted.

* **Repeat** : You can repeat the same paste operation multiple times to quickly repeat a sequence of note/effect values mutiple times. The start of the next paste will be where the last one ended.

## Special delete

Much like Special paste, "Special delete" is a more advanced form of deletion. You can bring up the special delete dialong by selecting a rango of notes and pressing CTRL+SHIFT+DELETE. 

![](images/DeleteSpecial.png#center)

* **Delete Notes** : If checked, will delete all the selected notes.

* **Delete Effects** : You can choose here the list of effects to delete. All unchecked effects will be preserved.

## Copy & pasting notes between projects 

It is possible to copy notes from one project to another. When doing so, FamiStudio will first look to make sure the instruments used by these notes exist in the other project. It will look for instruments having _the same name_. If some instruments are not found in the second project, it will offer you to create them for you, thus bringing the instrument from one project to another.

For example, if you copy a note from project 1 using an instrument called "Piano", FamiStudio will look for an instrument named "Piano" in the second. If it finds one, it will assume it is the same (even though it might not be). If no "Piano" instrument is found, it will offer you to create it for you.

This can be used as a way of transfering instruments from one project to another. Place a few notes using all the instruments you can to transfer. Copy them and paste them in the second project.

## Editing volume tracks & effects

The effect panel can be opened by clicking the little triangle at the top-left of the piano roll. 

Here is the list of effects currently supported, note that not every effect is available on every channel:

* **Volume**: The overall volume of the channel.
* **Vib Speed**: Vibrato speed, used in conjuction with vibrato depth to create a vibrato effect.
* **Vib Depth**: Vibrato depth, used in conjuction with vibrato speed to create a vibrato effect.
* **Pitch**: Allow tweaking the fine-pitch of a channel.
* **Duty Cycle** : Allow changing the duty cycle of an instrument without using a duty cycle envelope (only instruments with no duty cycle envelopes will use the specifed value).
* **FDS Speed**: Famicom Disk System audio modulation speed (FDS audio expansion only).
* **FDS Depth**: Famicom Disk System audio modulation depth (FDS audio expansion only).
* **Speed**: Changes the speed of the song (FamiTracker tempo mode only)
* **Note Delay**: Number of frames to delay the current note (FamiTracker tempo mode only)
* **Cut Delay**: Stop the note after the specified number of frames (FamiTracker tempo mode only)

Effects are edited by selecting and effect and dragging up or down to change the value. Right-clicking on an effect value deletes it. 

For effects that have huge values (such as FDS Depth), you can hold Shift to fine tune the exact value (a movement of 1 pixel will change the value by 1).

![](images/VolumeTrack.png#center)

## Volume track

The volume tracks dictates how loud the current channel should play. This volume is combined with volume envelope by multiplication (50% volume track x 50% envelope volume = 25% total volume). It is much more efficient to use volume envelopes wherever possible and only use volume tracks to control the global volume of the song.

## Vibrato depth & speed.

Vibrato depth and speed are used to add vibrato to a portion of the song without having to bother creating a new instrument. Please note that vibrato will temporarely override any pitch envelope on the current instrument. When vibrato is disabled (by setting depth or speed, or both to zero), the instrument will essentially have no pitch envelope until a new note is played.

The depth values for the vibrato are indentical to FamiTracker but the speeds are slightly different. The way FamiTracker implements vibrato, while clever, is flawed as it undersamples the vibrato curve at high speed, leading to aliasing which ends up with a low-frequency tone that has a "ringing" sound to it. Please see the [Import/Export](importexport.md) for a table that maps between FamiStudio and FamiTracker.

## Pitch 

Controls the global pitch of the track. Can be used to make an entire channel slightly out of tune.

## Duty Cycle

Allow changing the duty cycle of an instrument without using a duty cycle envelope. This can only affect instruments with no duty cycle envelopes. This effect is mostly just there for compatibility with FamiTracker. Unless you have special cases where you often need to change the duty cycle, you should always favor creating different instruments instead of using this effect. 

## Speed

This changes the speed parameter of the FamiTracker tempo settings and is only available in FamiTracker tempo mode. Larger values will make the song scroll slower. Please refer to the [Editing Songs & Project](song.md) section for more information about tempo management.

## Note and Cut delay

Note delays allows delaying the moment a note is played by a few frames while cut delay will stop a note after a few frames. This also is only available in FamiTracker tempo mode. Please refer to the [Editing Songs & Project](song.md) section for more information about tempo management.