# Exporting Songs

## Wave File (*.wav)

Only a single song can be exported at a time. You can choose the sample rate, it is recommended to stick to 44.1KHz if you want the soung to be exactly as you hear it in FamiStudio. Lower sample rate might lack high frequencies.

When exporting to WAV, the song will simply play once fully, all jump effects will be ignored.

![](images/ExportWav.png#center)

## Nintendo Sound Format (*.nsf)

Export to NSF is very basic for now.

![](images/ExportNsf.png#center)

Some limitations worth mentioning:

* If the song does not use sample, the maximum song size is between 24KB and 28KB.
* If the song uses samples, the maximum song size is between 8KB and 24KB depending on the size of the samples.

Note that these size are not printed anywhere and are not related to the size of the *.fms file. Best to simply try and see if it works.

## FamiTracker Text (*.txt)

You can export songs to FamiTracker using their Text Export format.

![](images/ExportFamiTracker.png#center)

There are some limitations:

* Pitch envelopes with looping sections will be modified on export so that the looping part sums to zero. This is done to prevent pitch from drifting up/down every time the envelope looks. The reason for this is that FamiTracker's pitch envelopes are relative while FamiStudio's are absolute.
* Instruments using both pitch and arpeggio envlopes at the same time will not sound correct in FamiTracker. This is due to the vastly different way both applications handles these. FamiTracker re-triggers the pitch envelope at each arpeggio notes (probably the more sensible way), while FamiStudio simply runs both at the same time.

... FIX THIS ...

This will be somewhat worse when exporting to FamiTracker since FamiTracker does not have the 1-bit of fraction FamiStudio has.

When importing from FamiTracker, all possible slide effects (1xx, 2xx, 3xx, Qxx and Rxx) will be converted to slide notes. Sometimes attack will be disabled as well to mimic the same behavior. This in an inherently imperfect process since they approaches are so different. For this reason, importing/exporting slide notes with FamiTracker should be considered a lossy process.

When exporting to FamiTracker, FamiStudio will do its very best to choose which FamiTracker effect to use. Here are the general rules:

* If the slide note and its target are within 16 semitones, Qxx/Rxx (note slide up/down) will be favored as it is the most similar effect to what we are doing.
* Otherwise, if the previous note has the same pitch as the slide note, 3xx (auto-portamento) will be used.
* Finally, if none of these conditions are satisfied, 1xx/2xx (slide up/down) will be used. This is not ideal since the pitch might not exactly match the target note.

... AND THIS...

Here is a table relating the speeds in FamiStudio and FamiTracker (this is applied automatically when importing/exporting):

FamiTracker speed | FamiTracker period | FamiStudio speed | FamiStudio period
--- | --- | --- | ---
1 | 64 | 1 | 64
2 | 32 | 2 | 32
3 | 21.3 | 3 | 21
4 | 16 | 4 | 16
5 | 12.8 | 5 | 13
6 | 10.7 | 6 | 11
7 | 9.1 | 7 | 9
8 | 8 | 8 | 8
9 | 7.1 | 9 | 7
10 | 6.4 | 10 | 6
11 | 5.8 | 10 | 6
12 | 5.3 | 11 | 5
13 | 4.9 | 11 | 5
14 | 4.6 | 11 | 5
15 | 4.3 | 12 | 4


## FamiTone2 Assembly Code (*.s, *.asm, *.dmc)

Exporting to FamiTone2 works in the same way as the command line tools provided by Shiru.

![](images/ExportFamiTone2.png#center)

When exporting file in seperate files, you can specific a name format template for each song. The {project} and {song} macros are available.

When exporting as a single file (non-seperate), you will be prompt to name the output assembly file. If any of the exported songs uses DPCM samples, a .dmc file of the same name will also be outputted.

# Importing Songs
