# Editing project properties

The project explorer displays the name of the project, the list of songs and instruments in the current project. 

![](images/ProjectExplorer.png#center)

Double-clicking on project name (first button in the project explorer) will allow you to change its name, author and copyright information. This information are used when exporting to NSF, for example.

![](images/EditProject.png#center)

The project properties is also where you select your [Expansion Audio](expansion.md). Expansion will add extra channels on top of the default 5 that the NES supported. Note that changing the expansion audio in a project will delete all data (patterns, notes, instrument) related to the previous expansion.

Please visit the [Expansion Audio](expansion.md) section for more detail about each expansion.

The tempo mode will affect how the tempo of you songs is calculated, how much control you have over the notes of your song and how your song plays on PAL systems. Note that changing the tempo mode when you have songs created is possible, but not recommended, the conversion is quite crude at the moment.

## Tempo Modes

### FamiStudio tempo mode (default) 

In this mode, you always have control over every single frame (1/60th of a second of the song) and you always edit in NTSC. You will be able to choose a **Note Length** which has a fixed number a frame (1/60s). More frames means a slower tempo. 

For example, in the image below, we have a 7 NTSC frames (1/60th of a sec) per note. On PAL system, it takes only 6 frames (1/50th of a sec) to cover approximately the same amount of time. 

![](images/NtscPalFrames.png#center)

Since our song is edited and stored as NTSC and we want to faitfully play it back on PAL systems, the solution is to run 2 NTSC frames, once every notes (twice if notes are >= 10 frames long). You can choose which of the frames will be skipped in the tempo settings. Note that the frame is not really skipped but merely run extremely fast. In the exemple below, on PAL systems, frame #2 is will almost impossible to hear since its sound will immediately be replaced by frame #3.

![](images/PalSkipFrames.png#center)

This will lead to a small error in playback speed on PAL system, but a very predictable behavior. 

This table shows the relation between the number of NTSC frames, number of PAL frames and the tempo error generated.

Number of NTSC frames (1/60 sec) | Number of PAL frames (1/50 sec) | NTSC time (ms) | PAL time (ms) | Diff (ms) | Error (%) | BPM
--- | --- | --- | --- | --- | --- | ---
1 | 1 | 16.64 | 20.00 | 3.357933 | -20.18% | 900.0 
2 | 2 | 33.28 | 39.99 | 6.715866 | -20.18% | 450.0 
3 | 3 | 49.92 | 59.99 | 10.0738 | -20.18% | 300.0 
4 | 3 | 66.56 | 59.99 | -6.56547 | 10.94% | 225.0 
5 | 4 | 83.20 | 79.99 | -3.20754 | 4.01% | 180.0 
6 | 5 | 99.84 | 99.99 | 0.150398 | -0.15% | 150.0 
7 | 6 | 116.47 | 119.98 | 3.508331 | -3.01% | 128.6 
8 | 7 | 133.11 | 139.98 | 6.866264 | -5.16% | 112.5 
9 | 8 | 149.75 | 159.98 | 10.2242 | -6.83% | 100.0 
10 | 8 | 166.39 | 159.98 | -6.41507 | 4.01% | 90.0 
11 | 9 | 183.03 | 179.97 | -3.05714 | 1.70% | 81.8 
12 | 10 | 199.67 | 199.97 | 0.300796 | -0.15% | 75.0 
13 | 11 | 216.31 | 219.97 | 3.658729 | -1.69% | 69.2 
14 | 12 | 232.95 | 239.97 | 7.016662 | -3.01% | 64.3 
15 | 13 | 249.59 | 259.96 | 10.37459 | -4.16% | 60.0 
16 | 14 | 266.23 | 279.96 | 13.73253 | -5.16% | 56.3 

Note that BPM values outside of the ones in this table can be created by putting multiples notes into a larger one. 

For example, Gimmick! uses a 11 frames per note, but puts 2 notes inside it. One of 5 frames and one of 6 frames. This creates an approximate tempo of 163.6 BPM and the different note lengths is not audible.

![](images/GimmickNote.png#center)

The FamiTracker documentation has a [handy chart](http://famitracker.com/wiki/index.php?title=Common_tempo_values) for these. Note that you are limited to 16 frames per note a the moment.

### FamiTracker tempo mode

This mode is mostly for compatibility with FamiTracker. It uses the speed/tempo paradigm. Please check out the [FamiTracker documentation](http://famitracker.com/wiki/index.php?title=Fxx) for a detailed explanation on how the playback speed is affected. If you import a FamiTracker Text or FTM file, it will be in this tempo mode. 

Only use this mode if you have specific compatibility needs with FamiTracker (such as exporting to an audio engine that only supports this tempo mode), as it will not receive any future improvement. When using this tempo mode, the *Speed* effect will be available in the effect panel.

Delayed notes and delayed cuts are not supported and will likely never be as they are inherentely a tracker-centric feature.

# Editing song properties

Double-clicking on the a song will allow you to change its name, color and other attributes. Names must be unique.

This dialog will look very different depending on:

* The **Tempo Mode** of the current project.
* If the project uses expansion audio or not. Expansion audio cannot run on PAL, so all PAL-related information will be hidden.

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
* **PAL Skip Frame 1**: Which frame will be skipped on PAL system. To keep up with NTSC, PAL will advance twice in one frame, once per note (twice in Frames Per Notes >= 10).
* **PAL Skip Frame 2**: Second frame to skip on PAL system. Only applies when Frames Per Notes >= 10.

