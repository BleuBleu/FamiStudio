# Editing Patterns

The Sequencer is where you decide which pattern plays when. A pattern is a small portion of a song (usually of fixed length) that is likely to be repeated. 

Patterns are laid out on one of the five channels that NES support (more if you have expansion audio enabled).

![](images/Sequencer.png#center)

## Seeking

Clicking in the timeline (header) of the sequencer will move the play position.

## Editing patterns

Clicking a pattern selects it and opens the piano roll for the current channel at the location of the pattern. Double-clicking a pattern allows renaming and changing its color (pattern names need to be unique per channel).

![](images/EditPattern.png#center)

You can select multiple patterns by right-cliking and dragging in the header bar of the Sequencer. To un-select everything, simply press Esc. When multiple patterns are selected, only the color can be edited.

![](images/PatternSelection2.png#center)

You can select multiple patterns in a rectangular grid, first select a pattern and shift-clicking to a second pattern.

![](images/SquareSelection.png#center)

## Adding/removing patterns

You can add a new pattern by left-clicking on an empty space. Right-clicking deletes.

## Moving/copying patterns

When one or multiple patterns are selected, dragging them will move them in the timeline. While dragging, holding Ctrl will copy a of the pattern(s). Note that when copying a pattern, it creates an instance of the same pattern, so modifying one instance will modify all of them.

## Cut/copy/pasting patterns

When one or multiple patterns are selected, press CTRL+C (or CTRL+X for cut). Move the selection somewhere else and paste with CTRL+V.

## Muting/soloing channels

Left-clicking on the icon of a channel (Square, triangle, noise, DPCM) will toggle mute. Right-clicking will toggle solo.

## Force display channel

Clicking the tiny square icon next to the channel name will force display it in the piano roll.

![](images/ForceDisplayButton.png#center)

Channels that are force displayed and are not the current channel will appear dimmed in the piano roll. This is useful when harmonizing between multiple channels, or editing drum patterns.

![](images/ForceDisplayPianoRoll.png#center)

This can also achieved with the keyboard by pressing Ctrl + 1...5 (the number of the channel).
