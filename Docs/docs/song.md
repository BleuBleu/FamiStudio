# Editing project properties

The project explorer displays the name of the project, the list of songs and instruments in the current project. 

![](images/ProjectExplorer.png#center)

Double-clicking on project name (first button in the project explorer) will allow you to change its name, author and copyright information. This information are used when exporting to NSF, for example.

![](images/EditProject.png#center)

The project properties is also where you select your [Expansion Audio](expansion.md). Expansion will add extra channels on top of the default 5 that the NES supported. Note that changing the expansion audio in a project will delete all data (patterns, notes, instrument) related to the previous expansion.

Please visit the [Expansion Audio](expansion.md) section for more detail about each expansion.

## Tempo Modes

The tempo mode will affect how the tempo of you songs is calculated, how much control you have over the notes of your song and how your song plays on PAL systems. Note that changing the tempo mode when you have songs created is possible, but not recommended, the conversion is quite crude at the moment.

### FamiStudio tempo mode (default) 

In this mode, you always have control over every single frame (1/60th of a second of the song) and you always edit in NTSC. You will be able to choose a **Note Length** which has a fixed number a frame (1/60s). More frames means a slower tempo. 

The biggest different with FamiTracker tempo is in the way it handles PAL conversion. 

For example, in the image below, we have a 7 NTSC frames (1/60th of a sec) per note. On PAL system, it takes only 6 frames (1/50th of a sec) to cover approximately the same amount of time. 

![](images/NtscPalFrames.png#center)

Since our song is edited and stored as NTSC and we want to faitfully play it back on PAL systems, FamiStudio will sometimes run 2 NTSC frames in a single PAL frame so it can keep up with NTSC. 

![](images/PalSkipFrames.png#center)

To make the PAL playback speed even more similar, the frame skips are ditributed over multiple notes, leading to a PAL playback speed error that is always less that 0.15%. 

This table sumarizes the number of frames that will be skipped on PAL for different note length, as well as their exact positions inside the notes.

Number of NTSC frames (1/60 sec) | Number of frames skipped by PAL | Position of skipped frames | BPM
--- | --- | --- | ---
1 | 1 frame every 6 notes | 0 | 900.0 
2 | 1 frame every 3 notes | 1 | 450.0 
3 | 1 frame every 2 notes | 1 | 300.0 
4 | 2 frames every 3 notes | 2 | 225.0 
5 | 5 frames every 6 note | 2 | 180.0 
6 | 1 frames every 1 note | 2 | 150.0 
7 | 7 frames every 6 note | 2, 4 | 128.6 
8 | 4 frames every 3 note | 2, 5 | 112.5 
9 | 3 frames every note | 2, 6 | 100.0 
10 | 5 frames every 3 note | 2, 7 | 90.0 
11 | 11 frames every 6 note | 2, 8 | 81.8 
12 | 2 frames every 1 note | 3, 8 | 75.0 
13 | 11 frames every 5 note | 2, 6, 10 | 69.2 
14 | 7 frames every 3 note | 2, 6, 11 | 64.3 
15 | 5 frames every 2 note  | 2, 7, 12 | 60.0 
16 | 8 frames every 3 note | 3, 8, 12 | 56.3 
17 | 20 frames every 7 note | 3, 8, 13 | 52.9
18 | 3 frames every 1 note | 3, 8, 14 | 50.0

Note that you arent not limited to the BPM values suggested by the different note lengths. You can create in-between ones by putting multiples notes into a larger one. For example, Gimmick! uses a 11 frames per note, but puts 2 notes inside it. One of 5 frames and one of 6 frames. This creates an approximate tempo of 163.6 BPM and the different note lengths is not audible.

![](images/GimmickNote.png#center)

The FamiTracker documentation has a [handy chart](http://famitracker.com/wiki/index.php?title=Common_tempo_values) for these. Note that you are limited to 18 frames per note a the moment.

If you are using the FamiStudio tempo mode and wish to have correct PAL playback in you homebrew game, please check out this custom version of FamiTone2 (named FamiTone2FS) that supports it:

[https://github.com/BleuBleu/FamiTone2FS](https://github.com/BleuBleu/FamiTone2FS)

### FamiTracker tempo mode

This mode is mostly for compatibility with FamiTracker. It uses the speed/tempo paradigm. Please check out the [FamiTracker documentation](http://famitracker.com/wiki/index.php?title=Fxx) for a detailed explanation on how the playback speed is affected. If you import a FamiTracker Text or FTM file, it will be in this tempo mode. 

Only use this mode if you have specific compatibility needs with FamiTracker (such as exporting to an audio engine that only supports this tempo mode), as it will not receive any future improvement. When using this tempo mode, the *Speed* effect will be available in the effect panel.

Delayed notes and delayed cuts are not supported and will likely never be as they are inherentely a tracker-centric feature.

# Editing song properties

Double-clicking on the a song will allow you to change its name, color and other attributes. Names must be unique.

This dialog will look very different depending on the **Tempo Mode** of the current project.

FamiStudio Tempo | FamiTracker tempo
--- | ---
![](images/EditSong.png#center) | ![](images/EditSongFamiTracker.png#center) 

Common properties:

* **Song length**: The number of patterns in the song
* **Notes Per Pattern**: Default number of notes in a pattern. Pattern length can be customized on a per-pattern basis in the Sequencer.
* **Notes Per Bar**: Number of notes in a bar. The piano roll will draw a darker line at every bar. Simply a visual aid, does not affect the audio in any way

Properties unique FamiTracker tempo mode:

* **Speed**: How much the timer is increment each frame, values other than 150 might create uneven notes
* **Tempo**: How many frames to wait before advancing to the next note (at least when using the integral tempo or 150)

Properties unique FamiStudio tempo mode:

* **Frames Per Notes**: How many frames (1/60th) in a typical notes. Values between 5 and 16 are recommended as they create the least error on PAL system.

These parameters will affect the look of the piano roll. 

![](images/PianoRollFrames.png#center)
