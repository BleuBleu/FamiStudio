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

Clicking in the timeline (header) of the piano roll will move the play position. You can also drag the seek back to more it more accurately.

## Adding & deleting notes

Clicking a pattern in the sequencer will scroll the piano roll to its location. 

Left-clicking in the piano roll will add a note of the currently selected instrument and dragging while still holding the left button will allow you to set the duration. A pattern may be automatically created if you click on an area where there was no pattern. 

![](images/CreateNote.gif#center)

Right-clicking deletes a note.

On mobile, notes are created by a quick tap.

![](images/MobileCreateNote.gif#center)

They are deleted by long pressing and selecting "Delete Note", or by making a selection and deleting it.

## Moving & resizing notes

When moving the mouse around the piano roll, the note under the mouse cursor will be highlighted and the mouse cursor will change to reflect the type of action you can do :

![](images/EditNote.gif#center)

Possible actions are:

* Move one or multiple note(s)
* Resize one or multiple note(s)
* Move the release point of a note

On mobile, notes must be first given a **white highlight** before being edited. Only they have that, you can move them or resize them. 

![](images/MobileEditNote.gif#center)

## Note duration & priority

FamiStudio is a monophonic app, in other words, only one note can play at a time on a given channel. 

The general rule is that the latest/newest note (the one that is rightmost on the timeline) always has priority, this mean that it can interrupt a previous note, making it sorted than it would normally be. 

As you can see, in the following example, even through i am trying to resize the left note, I cannot go beyond the right note. If I move the right now back, it is still interrupting the left note. Only when I move the right now far enough to the right we can see the real duration of the left note.

![](images/NotePriority.gif#center)

## Selecting notes

There are three main wars of selecting notes, selected notes will appear with a thick silver border. 

* You can select notes by right-clicking and dragging in the header of the piano roll or anywhere in the background of the piano roll (where there is no note). 
* You can select an entire pattern by double right clicking in the header of a pattern.
* You can select the duration of a note by double right clicking in the above or below a now.

The following images shows all 3 techniques.

![](images/SelectNotes.gif#center)

Once notes are selected, then can be moved or resized all at once. They can also be moved or transposed using the keyboard array keys (up, down, left and right). Holding CTRL while doing so will make the notes move by larger increments.

On mobile, there is just only way to select notes and it is done by swiping in the header of the piano roll. A long press in the background will allow you to clear the selection.

![](images/MobileSelectNotes.gif#center)

## Release point

Using Shift+click on an existing will add a release point on a note. Release points are shown as making the note thinner and triggers the envelope to jump to the release point. Release envelopes are useful to nicely fade out a note when its release, while preserving other effects like vibrato. There is no point to adding a release point to an instrument that does not have a release envelope.

Once a note has a release point, it can be moved by dragging. To remove a release point, you can right-click on it, or shift-click again.

![](images/ReleaseNote.gif#center)

On mobile, the release point is toggled by long pressing on a note and selecting "Toggle Release". The default position of a release will be in the middle of the note but you can move it afterwards.

![](images/MobileReleaseNote.gif#center)

## Stop notes

Using T+click will add a stop note. A stop note simple stops the sound and are displayed as little triangles. Although they are displayed next to the note preceding them, they actually have no pitch or instrument, they simply stop the sound. 

*As of FamiStudio 3.0.0 stop notes are no longer needed since they have been replaced by note durations. There are cases where stop notes will still remain when notes are detected to have inconsistent durations, but those should be relatively rare.*

![](images/StopNote.png#center)

## Instrument Picker

Holding **I** and clicking on a note will make the instrument of that note the current instrument. This is often known as the eye dropper or pipette tool in drawing software.

![](images/InstrumentPicker.png#center)

On mobile, this is achieved by long pressing on a note and selecting "Make Instrument Current".

## Recording mode

Recording mode is another way to input notes and is enabled by pressing the record button on the toolbar. Recording mode allows inputting notes using a MIDI controller or using a piano-type layout on the QWERTY keyboard. This layout is very similar to what FamiTracker uses. Note that this is not a "real-time" recording mode, simply a note-by-note input mode.

This is default layout for an en-US keyboard. The [Configuration Dialog](config.md) allows re-mapping the keys for other types of keyboards.

![](images/QWERTY.png#center)

The QWERTY keyboard input will give you a range of approximately 2.5 octaves. By default, this will go from C3 to C5 but it can be moved up/down by pressing **Page Up** and **Page Down**. Which keyboard key maps to which piano key will be displayed in the piano roll. 

![](images/RecordingPiano.png#center)

When recording mode is active, the seek bar will turn red. Each note you input will be snapped to the current snap precision (see next section for info about snapping) and the seek bar will advance to the next snapping position. 

There are a few other special keys that are enabled when recording mode is activated:

* **Backspace**: moves back by 1 note (or snapping interval), erasing any note inside the interval. 
* **Tab**: Advances by 1 note (or snapping interval), not recording anything.
* **Page Up/Down**: Moves the octave range that can be inputted using the QWERTY keyboard up/down.

On mobile, recording mode will automatically pop-up the piano and each note played will be recorded. 

![](images/MobileRecordingMode.png#center)

## Snapping

Snapping can be toggle by clicking the little magnet in the top-left corner of the piano roll. **SHIFT+S** can also be used to quickly toggle snapping on/off. The precision of the snapping can be changed by left/right clicking the number of using the mousewheel over the number or magnet icon. 

![](images/Snap.png#center)

The snapping precision is expressed in *Beats* (which are numbered as x.1, x.2, x.3, etc. in the header). So with the default settings, a snapping precision of 1 will mean a quarter note.

Adding, selecting and dragging of notes are currently the only actions affected by snapping. More might be added in future based on user feedback.

On mobile, the snapping precision is set from the Quick Access Bar, on the right size. 

![](images/MobileSnap.png#center)

## Note attack

By default, notes will have an "attack" which mean they will restart their envelopes (volume, pitch, etc.) from the beginning. This is represented by the little dark rectangle on the left of each note. This can be toggled for a particular note by holding the A key and clicking on a note. Note that if a note does not use the same instrument as the previous one, the attack will still play, even if disabled. Also please note that this will generally not carry over to FamiTracker, besides specific use cases around slide notes.

In this example, the first note will have an attack, while the second one will not.

![](images/NoAttack.png#center)

Since envelopes are not resetted, this means that if a note was released, it will remain released if the subsequent notes have no attack.

![](images/NoAttackReleased.png#center)

On mobile, toggling the note attack is done by long pressing on a note and selecting "Toggle Attack".

## Slide notes

Slide notes are notes that start at a given pitch (the pitch of the note) and slowly change to hit a target pitch which is represented by where the triangle ends. In this example, the attack of the second note has been disabled.

![](images/SlideNote.png#center)

Slide notes garantees that the target pitch will be reached by the end of the note (end of the triangle) but this might happen a bit earlier than the visual repesentation suggests. Especially in the higher pitches. This is due to the fact that the pitch calculations are all integer-based (with 1-bit of fraction) and it is often impossible to get the exact required slope to reach the pitch at the exact time. 

On mobile, you can toggle the slide  by long pressing on a note and selecting "Toggle Slide Note".

## Arpeggios

Arpeggios (not to be confused with arpeggio instrument envelopes) are typically used to simulate chords by playing changing notes very rapidly. A note can optionally have an arpeggio associated to it, just like it has an instrument associated to it. When an arpeggio is used, the notes used by this arpeggio will be displayed in a semi-transparent way and the extra notes will take the color of the associated arpeggio. Note that in order to keep things simple, the individual sequence of notes inside the arpeggio will not be displayed.

![](images/Arpeggio.png#center)

You can change which arpeggio is associated with a note the same way to can replace an instrument. You can select a new arpeggio and click on an existing note. Or you can select a few notes and drag and drop an arpeggio from the Project Explorer on to the selection.

If an instrument uses an arpeggio envelope and also uses an arpeggio chord, the chord will take over and completely override the arpeggio envelope of the instrument. 

## Replacing Instruments

To replace the instrument used by a note, you can simply drag the instrument on the note. To replace multiple notes at a time, simply create a selection and drag the instrument on the selection.

![](images/ReplaceInstrument.gif#center)

On mobile, you can replace an instrument or arpeggio by first making a selection, then long pressing on an instrument from the Quick Access Bar, then selecting "Replace Selection Instrument".

![](images/MobileReplaceInstrument.gif#center)

## Copying & pasting notes

Much like the sequencer, selcted notes can be copy (or cut) by pressing CTRL+C (or CTRL+X). You can then move the selection somewhere else and paste the notes with CTRL+V.

### Copying & pasting notes between projects 

It is possible to copy notes from one project to another. When doing so, FamiStudio will first look to make sure the instruments used by these notes exist in the other project. It will look for instruments having _the same name_. If some instruments are not found in the second project, it will offer you to create them for you, thus bringing the instrument from one project to another.

For example, if you copy a note from project 1 using an instrument called "Piano", FamiStudio will look for an instrument named "Piano" in the second. If it finds one, it will assume it is the same (even though it might not be). If no "Piano" instrument is found, it will offer you to create it for you.

This can be used as a way of transfering instruments from one project to another. Place a few notes using all the instruments you can to transfer. Copy them and paste them in the second project.

## Special paste

"Special paste" is a more advanced form of pasting. It is used to to do things like pasting notes without their associated effets or volume track (or vice-versa), or to just paste specific effects. You can use a "special paste" by pressing CTRL+SHIFT+V. This will open a popup dialog.

![](images/PasteSpecial.png#center)

* **Mix With Existing Notes** : The default behavior of pasting is to completely replace everything by the content of the clipboard. Using the "Mix with existing notes" option will preserve any existing data (notes, volumes, effects) and only insert new data if there is nothing there already.

* **Paste Notes** : Will paste the actual notes (including slide notes and arpeggio information).

* **Paste Effects** : You can choose the list of effect that you with to paste. Unchecked effect will not be pasted.

* **Repeat** : You can repeat the same paste operation multiple times to quickly repeat a sequence of note/effect values mutiple times. The start of the next paste will be where the last one ended.

On mobile, the same functionality is accessible by long pressing on the "Paste" icon of the toolbar. The dialog will look different by contains the same functionality.

![](images/MobilePasteSpecial.png#center)

## Special delete

Much like Special paste, "Special delete" is a more advanced form of deletion. You can bring up the special delete dialong by selecting a rango of notes and pressing CTRL+SHIFT+DELETE. 

![](images/DeleteSpecial.png#center)

* **Delete Notes** : If checked, will delete all the selected notes.

* **Delete Effects** : You can choose here the list of effects to delete. All unchecked effects will be preserved.
mes. The start of the next paste will be where the last one ended.

On mobile, the same functionality is accessible by long pressing on the "Delete" icon of the toolbar. The dialog will look different by contains the same functionality.

![](images/MobileDeleteSpecial.png#center)

## Editing volume & effects

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

### Volume track

The volume tracks dictates how loud the current channel should play. This volume is combined with volume envelope by multiplication (50% volume track x 50% envelope volume = 25% total volume). It is much more efficient to use volume envelopes wherever possible and only use volume tracks to control the global volume of the song.

The volume track is allowed to have slides to smoothly raise or lower the volume. They are created exactly like regular slide notes, by holding "S", clicking and dragging up or down. These slides use fixed point arithmetic and have limited precision. They will go up/down by 1 volume unit every 16 frames at most. Very slow/long slides may end earlier than their visual representation.

![](images/VolumeSlide.gif#center)

On mobile, a volume slide is created by long pressing on a volume value and selecting "Toggle Volume Slide". 

![](images/MobileVolumeSlide.gif#center)

### Vibrato depth & speed

Vibrato depth and speed are used to add vibrato to a portion of the song without having to bother creating a new instrument. Please note that vibrato will temporarely override any pitch envelope on the current instrument. When vibrato is disabled (by setting depth or speed, or both to zero), the instrument will essentially have no pitch envelope until a new note is played.

The depth values for the vibrato are indentical to FamiTracker but the speeds are slightly different. The way FamiTracker implements vibrato, while clever, is flawed as it undersamples the vibrato curve at high speed, leading to aliasing which ends up with a low-frequency tone that has a "ringing" sound to it. Please see the [Export](export.md) page for a table that maps between FamiStudio and FamiTracker.

### Pitch 

Controls the global pitch of the track. Can be used to make an entire channel slightly out of tune.

### Duty Cycle

Allow changing the duty cycle of an instrument without using a duty cycle envelope. This can only affect instruments with no duty cycle envelopes. This effect is mostly just there for compatibility with FamiTracker. Unless you have special cases where you often need to change the duty cycle, you should always favor creating different instruments instead of using this effect. 

### Speed

This changes the speed parameter of the FamiTracker tempo settings and is only available in FamiTracker tempo mode. Larger values will make the song scroll slower. Please refer to the [Editing Songs & Project](song.md) section for more information about tempo management.

### Note and Cut delay

Note delays allows delaying the moment a note is played by a few frames while cut delay will stop a note after a few frames. This also is only available in FamiTracker tempo mode. Please refer to the [Editing Songs & Project](song.md) section for more information about tempo management.
