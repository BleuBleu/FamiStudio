# Editing project properties

The project explorer displays the name of the project, the list of songs and instruments in the current project. 

![](images/ProjectExplorer.png#center)

Double-clicking on project name (first button in the project explorer) will allow you to change its name, author and copyright information. This information are used when exporting to NSF, for example.

![](images/EditProject.png#center)

The project properties is also where you select your [Expansion Audio](expansion.md). Expansion will add extra channels on top of the default 5 that the NES supported. Note that changing the expansion audio in a project will delete all data (patterns, notes, instrument) related to the previous expansion.

Please visit the [Expansion Audio](expansion.md) section for more detail about each expansion.

## Tempo Modes

The tempo mode will affect how the tempo of you songs is calculated, how much control you have over the notes of your song and how your song plays on PAL systems. Note that changing the tempo mode when you have songs created is possible, but not recommended, the conversion is quite crude at the moment.

FamiStudio supports two tempo modes : **FamiTracker** and **FamiStudio**, this handy table will summarize the differences between the 2 tempo modes.

&nbsp; | FamiTracker Tempo | FamiStudio Tempo
--- | --- | ---
Granularity | Maximum precision is dictated by the speed parameter. Anything finer has to use effects which operate at a frame-level. Very few FamiTracker effects are supported. | Gives control over every frame (1/60th or 1/50th or a second).
Paradigm | Makes a lot of sense in a tracker where information density is important. | Makes a lot of sense in a DAW where simply zooming in/out can reveal more detail.
Editing | Mostly machine-agnostic when it comes to editing. Your are not explicitely making the song in PAL or NTSC, except for some effects like delayed notes/cuts, slides, etc. which makes assumption on the duration of a frame. | You are explicitely editing in NTSC or PAL space.
Playback | Playback to NTSC or PAL is done by changing the speed at which the internal counter is incremented. This breaks down at low speed values. | Playback to NTSC or PAL is done by either running the sound engine twice (when playing NTSC song on PAL) or idling (when running a PAL song on NTSC) at strategically chosen frames. The frames where where skipping/idling can happen are deterministic.
Support | Will no longuer receive any improvement in FamiStudio | Will keep improving in the future.
Compatiblity | Better suited if you need to export to FamiTracker | Will export to FamiTracker, but at a speed of 1, tempo 150.

### FamiStudio tempo mode (default) 

In this mode, you always have control over every single frame (1/60th of a second in NTSC, 1/50th in PAL). You will be able to choose a **Note Length** which has a fixed number a frames. More frames means a slower tempo. Unlike FamiTracker tempo which is (mostly) machine-agnostic when it comes to editing, in FamiStudio tempo, your project is authored for a specific machine (NTSC or PAL) and a conversion is applied at playback if necessary (PAL playing on NTSC or vice-versa).

One of the big different with FamiTracker tempo is in the way it handles PAL to NTSC or NTSC to PAL conversion. 

#### NTSC to PAL

For example, in the image below, we have a NTSC song with 6 frames (1/60th of a sec) per note. On PAL system (50 FPS), if were were to play back this song, it would play 20% slower.

![](images/NtscPalFrames6.png#center)

To faitfully play back our NTSC song on PAL systems, FamiStudio will sometimes run 2 NTSC frames in a single PAL frame so it can keep up with NTSC. 

![](images/PalSkipFrames6.png#center)

This makes the playback speed almost the same, but unfortunately, this is not always this simple. Let's take a note length of 8 frames for example. Again, if we were to try to naively play this back on PAL, it would play back 20% slower than NTSC.

![](images/NtscPalFrames8.png#center)

If we try to apply the same strategy of running 2 NTSC frame once per note, we would still end up playing 5% slower.

![](images/PalSkipFrames8.png#center)

The solution here is to skip 4 NTSC frames, over 3 notes to distribute the error. This ends up reduce the playback speed error well below 1%.

![](images/PalSkipFrames8-3Notes.png#center)

This table sumarizes the number of frames that will be skipped on PAL for different note length.

Number of NTSC frames (1/60 sec) | Number of frames skipped by PAL | Positions of potential double-frames | BPM
--- | --- | --- | ---
 1 |  1 double frames over 6 notes | 1 | 900.0 
 2 |  1 double frames over 3 notes | 01 |450.0 
 3 |  1 double frames over 2 notes | 010 |300.0 
 4 |  2 double frames over 3 notes | 0100 |225.0 
 5 |  5 double frames over 6 notes | 01000 |180.0 
 6 |  1 double frames over 1 notes | 010000 |150.0 
 7 |  7 double frames over 6 notes | 0100010 |128.6 
 8 |  4 double frames over 3 notes | 01000100 |112.5 
 9 |  3 double frames over 2 notes | 010001000 |100.0 
10 |  5 double frames over 3 notes | 0100001000 |90.0 
11 | 11 double frames over 6 notes | 01000001000 |81.8 
12 |  2 double frames over 1 notes | 010000010000 |75.0 
13 | 13 double frames over 6 notes | 0100010001000 |69.2 
14 |  7 double frames over 3 notes | 01000100001000 |64.3 
15 |  5 double frames over 2 notes | 010000100001000 |60.0 
16 |  8 double frames over 3 notes | 0100001000010000 |56.3 
17 | 17 double frames over 6 notes | 01000001000001000 |52.9
18 |  3 double frames over 1 notes | 010000010000010000 |50.0

#### PAL to NTSC

The sample principle apply when playing PAL songs on NTSC, but instead of running 2 frames rapidly, the sound engine will simply "do nothing" (idle) every once in a while to avoid going too fast. 

Number of PAL frames(1/50 sec) | Number of idle frames in NTSC | Positions of potential idle-frames | BPM
--- | --- | --- | ---
 1 | idle  1 frames over 5 notes | 1 | 750.0
 2 | idle  2 frames over 5 notes | 01 | 375.0
 3 | idle  3 frames over 5 notes | 010 | 250.0
 4 | idle  4 frames over 5 notes | 0100 | 187.5
 5 | idle  1 frames over 1 notes | 01000 | 150.0
 6 | idle  6 frames over 5 notes | 010010 | 125.0
 7 | idle  7 frames over 5 notes | 0100010 | 107.1
 8 | idle  8 frames over 5 notes | 01000100 | 93.8
 9 | idle  9 frames over 5 notes | 010000100 | 83.3
10 | idle  2 frames over 1 notes | 0100001000 | 75.0
11 | idle 11 frames over 5 notes | 01001001000 | 68.2
12 | idle 12 frames over 5 notes | 010010000100 | 62.5
13 | idle 13 frames over 5 notes | 0100010001000 | 57.7
14 | idle 14 frames over 5 notes | 01000100001000 | 53.6
15 | idle  3 frames over 1 notes | 010000100001000 | 50.0
16 | idle 16 frames over 5 notes | 0010001000100010 | 46.9
17 | idle 17 frames over 5 notes | 00100010001000100 | 44.1
18 | idle 18 frames over 5 notes | 001000100010000100 | 41.7

### Position of double or idle frames

The position of potential double or idle frames where chosen to try to:

* Distribute the double/idle frames as evenly as possible while maintaining deterministic positions inside the notes
* Avoid placing a double/idle frame on the first frame of the note, where the attack usually is
* Avoid placing a double/idle frame on the last frame of the note, where a stop note could be
* Avoid placing a double/idle frames on the 2 middle notes, to allow 1/2 notes to be used occasionally and improve support for composite notes.

### Composite notes

Note that you arent not limited to the BPM values suggested by the different note lengths. You can create in-between ones by putting multiples notes into a larger one. For example, Gimmick! uses a 11 frames per note, but puts 2 notes inside it. One of 5 frames and one of 6 frames. This creates an approximate tempo of 163.6 BPM and the different note lengths is not audible.

![](images/GimmickNote.png#center)

The FamiTracker documentation has a [handy chart](http://famitracker.com/wiki/index.php?title=Common_tempo_values) for these. Note that you are limited to 18 frames per note a the moment.

### FamiTone2 support of FamiStudio tempo mode

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
