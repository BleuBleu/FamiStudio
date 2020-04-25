# Editing Patterns

The Sequencer is where you decide which pattern plays when. A pattern is a small portion of a song (usually of fixed length) that is likely to be repeated. 

Patterns are laid out on one of the five channels that NES support (more if you have expansion audio enabled).

![](images/Sequencer.png#center)

## Seeking

Clicking in the timeline (header) of the sequencer will move the play position.

## Changing the active channel

Clicking on a channel will make it the active one. Alternatively, you can press the keyboard key **1...5** (more if using expansion) to quickly change the active channel.

## Muting/soloing channels

Left-clicking on the icon of a channel (Square, triangle, noise, DPCM) will toggle mute. Right-clicking will toggle solo.

## Force display channel

Clicking the tiny square icon next to the channel name will force display it in the piano roll.  Alternatively, you can press the keyboard key **Ctrl + 1...5** (more if using expansion).

## Adding/removing patterns

You can add a new pattern by left-clicking on an empty space. Right-clicking deletes.

## Editing patterns

Clicking a pattern selects it and opens the piano roll for the current channel at the location of the pattern. Double-clicking a pattern allows renaming and changing its color (pattern names need to be unique per channel).

![](images/EditPattern.png#center)

You can select multiple patterns by right-cliking and dragging in the header bar of the Sequencer. To un-select everything, simply press Esc. When multiple patterns are selected, only the color can be edited.

![](images/PatternSelection2.png#center)

You can select multiple patterns in a rectangular grid, first select a pattern and shift-clicking to a second pattern.

![](images/SquareSelection.png#center)

## Moving/copying patterns

When one or multiple patterns are selected, dragging them will move them in the timeline. While dragging, holding Ctrl will copy a of the pattern(s). Note that when copying a pattern, it creates an instance of the same pattern, so modifying one instance will modify all of them.

## Cut/copy/pasting patterns

When one or multiple patterns are selected, press CTRL+C (or CTRL+X for cut). Move the selection somewhere else and paste with CTRL+V.

![](images/ForceDisplayButton.png#center)

Channels that are force displayed and are not the current channel will appear dimmed in the piano roll. This is useful when harmonizing between multiple channels, or editing drum patterns.

![](images/ForceDisplayPianoRoll.png#center)

This can also achieved with the keyboard by pressing Ctrl + 1...5 (the number of the channel).

## Setting the loop point

The **Loop point** is where the song will repeat once it reaches the end and is represented by a little arrow. 

![](images/LoopPoint.png#center)

You can move the loop point around by holding the **L** key and clicking at a location in the Sequencer. The loop point is optional and can be toggle by re-setting it to the current location.

## Custom pattern settings

Double clicking on the header of sequencer will allow you to set some customs settings for a column of patterns. This will allow you to change the number of notes and tempo parameters.

![](images/CustomPatternSettings.png#center)

This dialog will look quite different depending on the tempo mode you are using. To learn more about tempo modes, please check out the [Project properties section](song.md). 

FamiStudio Tempo | FamiTracker tempo
---  | ---
![](images/CustomPatternSettingsNoPAL.png#center) | ![](images/CustomPatternSettingsFamiTracker.png#center) 

Enabling **Custom Pattern** will allow you to change the values. Once a pattern has any kind of custom setting, it will no longer take its values from the Song's properties and will be display with a **asterisk (*)** next to its index in the Sequencer.

The parameters here are the same as when editing the [Song properties](song.md), but are localized to a column of patterns.