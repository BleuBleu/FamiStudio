# Editing Instruments

The project explorer displays the list of songs and instruments in the current project. This section is only going to cover editing of instruments. Please refer to the [Editing Songs & Project](song.md) section to learn how to edit project and song properties.

![](images/ProjectExplorer.png#center)

Most instrument (except DPCM samples) has 4 buttons :

* The duty cycle envelope
* The volume envelope
* The pitch envelope
* The arpeggio envelope

An envelope is simply a parameter that can change over the duration of a note as it plays. It can be used to create vibrato, tremolo, change the attack and release of a note, for example. If an instrument currently has no envelope for a particular type, it will appear dimmed.

For details on Expansion audio instruments please visit the [Expansion Audio section](expansion.md).

## Editing instrument properties

Double-clicking on an instrument will show its properties.

* **Relative pitch** : By default, FamiStudio's pitch envelope are absolute. Meaning that the envelope values are the pitch you are going to hear, this is especially useful for vibrato where you can draw a simple sine wave. It is sometimes useful to have relative pitch envelope to create pitches that rapidly ascend or decend (useful for bassdrum sounds). This is how FamiTracker handles pitch envelopes.

![](images/EditInstrument.png#center)

## Adding instruments

You can an instrument by pressing the "+" sign, and you can one by right-clicking on it. Deleting an instrument will delete all notes used by that instrument.

## Replacing an instrument by another

Clicking on an instrument name and dragging it over another instrument will allow you to replace all notes of the first instrument by the second. This is useful prior to deleting an instrument.

## Importing instruments

You can import intruments in your project from any supported input format, as well as FamiTracker instrument files (FTI files, official FamiTracker 0.4.6 only) by clicking the little folder icon. 

When importing instruments from another project or a FamiTracker file, you will be prompted with a list of instruments to import. Simply check the instrument to bring over.

![](images/ImportInstruments.png#center)

Note that instruments that are using incompatible expansion audio will not be able to be imported. Also, instruments with the same names are assumed to be the same. If you project already contains an instrument called "Piano" and you try to import another one called "Piano", nothing will happen. You are responsible to uniquely name your instruments if they are truly different.

## Editing instrument envelopes

Clicking on an envelope icon in the project explorer will open the envelope of that instrument in the piano roll. The length of the envelope can be changed by left-clicking (and potentially dragging) in the timeline of the piano roll. Setting the length of an envelope to zero will disable it.

![](images/EditEnvelope.png#center)

The loop point of an envelope can be set by right-clicking in the timeline. Volume tracks are also allowed to have release envelopes. Release envelopes are played when a release note is encountered and terminates the loop by jumping to the release point. This is useful for fading out notes smoothly. The release point is set by right-dragging from the rightmost side of the envelope.

![](images/EditEnvelopeRelease.png#center)

## Copying envelope values

Right-clicking on the numbers on the header of the envelope editor allows for range selection of envelope values. These can then be copy and pasted elsewhere.

![](images/CopyEnvelopeValues.png#center)

It is also possible to paste envelope values coming for raw text. Any series of number that is space, tab, comma, semicolon or newline separated can be pasted in the envelope editor. 

## Copying envelopes

Clicking on an envelope button and dragging it on another instrument will copy that envelope from the first to the second. Note that unlike FamiTracker, envelopes are not explicitly shared between instruments. Identical envelopes will be combined when exporting to FamiTone2, but it is your responsibility to optimize the content and ensure that you limit the number of unique envelopes.

## Deleting envelopes

Right-clicking on the icon of an envelope deletes it.

## Editing DPCM samples

Clicking on the little icon next to the DPCM samples in the project explorer will open the piano roll in DPCM edition mode.

![](images/EditDPCM.png#center)

Clicking anywhere on a note that does not have a DPCM sample associated will prompt you to open a .DMC file. No DMC edition tool is provided, you can use [FamiTracker](http://famitracker.com/), [RJDMC](http://forums.famitracker.com/viewtopic.php?t=95) or any other tool. DPCM samples are assumed to have unique names and 2 samples with the same name will be assume to be the same. Double-clicking on an existing sample edits its pitch and toggle loop. Note that only notes between C1 and D6 are allowed to have DPCM samples.
