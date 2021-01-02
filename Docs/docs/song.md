# Editing projects

The project explorer displays the name of the project, the list of songs and instruments in the current project. This section is going to focus on the very top part, which is the project and the songs.

![](images/ProjectExplorer.png#center)

## Editing project properties

Double-clicking on project name (first button in the project explorer) will allow you to change its name, author and copyright information. This information is used when exporting to NSF, for example.

![](images/EditProject.png#center)

## Expansion audio

The project properties is also where you select your [Expansion Audio](expansion.md). Expansion will add extra channels on top of the default 5 that the NES supported. Note that changing the expansion audio in a project will delete all data (patterns, notes, instrument) related to the previous expansion.

Please visit the [Expansion Audio](expansion.md) section for more detail about each expansion.

## Tempo modes

FamiStudio supports two tempo modes : **FamiStudio** and **FamiTracker**. 

The tempo mode will affect how the tempo of you songs is calculated, how much control you have over the notes of your song and how your song plays on PAL systems. Note that changing the tempo mode when you have songs created is possible, but not recommended, the conversion is quite crude at the moment.

### FamiStudio Tempo Mode

FamiStudio tempo modes gives full control over every frame (1/60th of a second on NTSC, 1/50th on PAL). It is the default mode. In this mode you will see the individual frames in the piano roll and will have more precise control on where each note starts/end. On the other hand, it makes suddent tempo changes harder to manage, and it is also harder to achieve "non-integral" tempos. It can faithfully recreate the vast majority of the NES music library very easily. 

For example, a C-D-E scale where each note is stopped for 1 frame between each note will look like this using FamiStudio tempo. The dashed lines seperate individual frames, so you can place a stop note (triangle) 1 frame before the new note starts. Very intuituve and visual.

![](images/TempoExampleFamiStudio.png#center) 

### FamiTracker Tempo Mode

FamiTracker tempo has a limited visual granularity and relies on effects (delayed notes/cuts) to get frame-level precision. It uses the speed/tempo paradigm. Please check out the [FamiTracker documentation](http://famitracker.com/wiki/index.php?title=Fxx) for a detailed explanation on how the playback speed is affected. If you import a FamiTracker Text or FTM file, the project will be in this tempo mode. 

Same example, but using FamiTracker tempo. Here we dont have the individual frames so we need to use a "delayed cut" effect to achieve the same result. The delayed cut tells the sound engine to insert a stop note after 9 frames have elapsed, achieving the exact same result. That being said, one might argue that it is not very visual and feels like using a Tracker. Moreover, this would not always work correctly on PAL.

![](images/TempoExampleFamiTracker.png#center) 

### Which one to use?

You should use FamiStudio tempo mode if:

* You want to be able to visually control the position of every note at a frame-level precision.
* You are not too strict about the BPM you want to achieve.
* You are not planning to do sophisticated tempo changes during the song.

You should use FamiTracker tempo mode if:

* You are OK with using effects tracks (delayed notes, cuts) to finely tune the start/end of each notes.
* You want to achieve a very specific BPM, even if this mean some notes will be uneven.
* You want to have complex tempo changes during the song.
* You need compatibility with FamiTracker.

# Editing songs

Right below the project name are the songs.

## Adding/Deleting songs

To add new songs, simply click on the little "+" icon on the song header.

## Importing/Merging songs

To merge a song coming from another project, click on the little folder icon on the song header. 

Songs from another project must use the same audio expansion and tempo mode. Also, please note that instruments, samples and arpeggios, used by the other song will be matched by their name. In other words, instruments with the same names are assumed to be the same. If you project already contains an instrument called "Piano" and you try to import a song using another one called "Piano", the existing one will be used. You are responsible to uniquely name your instruments if they are truly different.

## Editing song properties

Double-clicking on the a song will allow you to change its name, color and other attributes. Names must be unique.

This dialog will look different depending on the **Tempo Mode** of the current project.

FamiStudio Tempo | FamiTracker tempo
--- | ---
![](images/EditSong.png#center) | ![](images/EditSongFamiTracker.png#center) 

Common properties:

* **Song length**: The number of patterns in the song
* **Notes Per Pattern**: Default number of notes in a pattern. Pattern length can be customized on a per-pattern basis in the Sequencer.
* **Notes Per Beat**: Number of notes in a beat. The piano roll will draw a darker line at every beat. Simply a visual aid, does not affect the audio in any way, although it does affect the displayed BPM calculation. It is recommended to keep that at 4.

Properties unique FamiTracker tempo mode:

* **Speed**: How much the timer is increment each frame, values other than 150 might create uneven notes
* **Tempo**: How many frames to wait before advancing to the next note (at least when using the integral tempo or 150)

Properties unique FamiStudio tempo mode:

* **Frames Per Notes**: How many frames (1/60th) in a typical notes. Values between 5 and 16 are recommended as they create the least error on PAL system.

## Tempo

Configuring tempo in FamiStudio is definately less intuitive than in a regular DAW, so please bear with me. This is both for technical reasons (the fact that the NES runs at 50/60 FPS) and historical reasons (influence from FamiTracker).

The key thing to understand is that the piano roll simply gives you a series of, what I am going to loosely call, **notes** (**rows** if you come from FamiTracker). It is up to the composer to configure these notes and achieve the desired tempo and time signature.

For example, let's look at the simple melody of the children song "A, B, C, ...". On the right side are the song settings that were used to achieve this. In this example, the project was set to use the FamiStudio tempo mode, but the same logic applies to the FamiTracker tempo mode. 

[![](images/TempoABC.png#center)](images/TempoABC.png#center)
*Click on the image to zoom in.*

Here is a zoomed-in version of the first beat.
![](images/TempoABCZoom.png#center)
Observations:

* Frames are seperated by dashes grey line (they will disapear if you zoom out enough).
* Notes are seperated by thin grey lines.
* Beats are seperated by thin black lines.
* Patterns are seperated by thick black lines.

Here we chosen to have 1 bar = 1 pattern, we could have chosen to fit the whole song in a single pattern, this is totally arbitrary. You should try to use a pattern size is a good tradeoff between reusability (being able to copy patterns and re-use them) and size (having many small patterns is annoying).

The **Frames per Note** setting is the main driver of tempo and determines the base length our "notes". The more frames (1/60th of a second) we wait, the slower the song will play. So 8 frames per note give us a BPM of 112.5. When using FamiTracker tempo, this is the equalivalent of the **Speed** parameter.

Also we have chosen to assemble 4 notes into a beat, and 4 beats in a pattern (16 notes), which is how we get something that looks like a 4/4 time signature. This also means our smallest granularity for our melody is 1/16th of a note. That being said, you can move notes at the frame-level, so you actually have a lot more control than this.

### Composite notes

Note that you arent not limited to the BPM values suggested by the different note lengths. You can create in-between ones by putting multiples notes into a larger one. For example, Gimmick! uses a 11 frames per note, but puts 2 notes inside it. One of 5 frames and one of 6 frames. This creates an approximate tempo of 163.6 BPM and the different note lengths is not audible.

![](images/GimmickNote.png#center)

The FamiTracker documentation has a [handy chart](http://famitracker.com/wiki/index.php?title=Common_tempo_values) for these. Note that you are limited to 18 frames per note a the moment.

## FamiStudio Tempo & PAL conversion

This is a more technical discussion of how FamiStudio tempo handles NTSC -> PAL and PAL -> NTSC conversion.

### NTSC to PAL

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

### PAL to NTSC

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
