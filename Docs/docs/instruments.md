# Editing Instruments

The project explorer displays the list of songs and instruments in the current project. This section is only going to cover editing of instruments. Please refer to the [Editing Songs & Project](song.md) section to learn how to edit project and song properties.

![](images/ProjectExplorer.png#center)

Most instrument (except DPCM samples) has 4 buttons :

* The duty cycle envelope
* The volume envelope
* The pitch envelope
* The arpeggio envelope

If an instrument currently has no envelope for a particular type, it will appear dimmed.

## Editing instrument properties

Double-clicking on an instrument will show its properties.

* **Relative pitch** : By default, FamiStudio's pitch envelope are absolute. Meaning that the envelope values are the pitch you are going to hear, this is especially useful for vibrato where you can draw a simple sine wave. It is sometimes useful to have relative pitch envelope to create pitches that rapidly ascend or decend (useful for bassdrum sounds). This is how FamiTracker handles pitch envelopes.

![](images/EditInstrument.png#center)

## Adding/removing songs and instruments

You can add a song or instrument by pressing the "+" sign, and you can delete a song or instrument by right-clicking on it. Deleting an instrument will delete all notes used by that instrument. Note that there always needs to be at least one song in a project.

## Replacing an instrument by another

Clicking on an instrument name and dragging it over another instrument will allow you to replace all notes of the first instrument by the second. This is useful prior to deleting an instrument.

## Editing envelopes

Clicking on an envelope button will start editing it in the piano roll. The duty cycle button will cycle between the 4 possible settings: 12.5%, 25%, 50% and inverted 25% since FamiTone2 does not support duty cycle envelopes. For more info on how to edit or delete envelopes, please refer to the piano roll section.

## Copying envelopes

Clicking on an envelope button and dragging it on another instrument will copy that envelope from the first to the second. Note that unlike FamiTracker, envelopes are not explicitly shared between instruments. Identical envelopes will be combined when exporting to FamiTone2, but it is your responsibility to optimize the content and ensure that you limit the number of unique envelopes.

## Deleting envelopes

Right-clicking on the icon of an envelope deletes it.

## Editing instrument envelopes

Clicking on an envelope icon in the project explorer will open the envelope of that instrument in the piano roll. The length of the envelope can be changed by left-clicking (and potentially dragging) in the timeline of the piano roll. Setting the length of an envelope to zero will disable it.

![](images/EditEnvelope.png#center)

The loop point of an envelope can be set by right-clicking in the timeline. Volume tracks are also allowed to have release envelopes. Release envelopes are played when a release note is encountered and terminates the loop by jumping to the release point. This is useful for fading out notes smoothly. The release point is set by right-dragging from the rightmost side of the envelope.

![](images/EditEnvelopeRelease.png#center)

## Editing DPCM samples

Clicking on the little icon next to the DPCM samples in the project explorer will open the piano roll in DPCM edition mode.

![](images/EditDPCM.png#center)

Clicking anywhere on a note that does not have a DPCM sample associated will prompt you to open a .DMC file. No DMC edition tool is provided, you can use FamiTracker, RJDMC or any other tool. DPCM samples are assumed to have unique names and 2 samples with the same name will be assume to be the same. Double-clicking on an existing sample edits its pitch and toggle loop. Note that only notes between C1 and D6 are allowed to have DPCM samples.
