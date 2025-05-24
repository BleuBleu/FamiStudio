# Editing projects

The project explorer displays the name of the project, the list of songs and instruments in the current project. This section is going to focus on the very top part, which is the project and the songs.

![](images/ProjectExplorer.png#center)

## Editing project properties

Clicking on the little gear icon or selecting the "Project Properties..." option from the context menu of the project (first button in the project explorer) will allow you to change its name, author and copyright information. This information is used when exporting to NSF, for example.

![](images/EditProject.png#center)

It is also possible to set the tuning to something other than A = 440Hz.

## Tempo modes

FamiStudio supports two tempo modes : **FamiStudio** and **FamiTracker**. 

The tempo mode will affect how the tempo of you songs is calculated, how much control you have over the notes of your song and how your song plays on PAL systems. Note that changing the tempo mode when you have songs created is possible, but not recommended, the conversion is still quite crude at the moment.

### FamiStudio Tempo Mode

FamiStudio tempo modes gives full control over every frame (1/60th of a second on NTSC, 1/50th on PAL). It is the default mode. In this mode you will see the individual frames in the piano roll and will have more precise control on where each note starts/end. 
For example, a C-D-E-F scale where each note is stopped for 1 frame between each note will look like this using FamiStudio tempo. The dashed lines separate individual frames, so you can leave and empty frame 1 frame before the new note starts. Very intuitive and visual.

![](images/TempoExampleFamiStudio.png#center) 

FamiStudio tempo mode let's you simply choose a BPM value for the song (or an individual pattern) and will automatically choose the appropriate number of frames to make each notes. Some BPMs will require the use of a *groove* which is an uneven sequence of frames. 

For example, at 142 BPM (in NTSC), FamiStudio will know to use a 7-6-6 groove, which mean that the first note will be 7 frames long, then followed by two notes of 6 frames, and the whole thing will repeat until there is a tempo change in the song. But in order to keep the piano roll nice and even, FamiStudio will only only display the minimum values of the groove, 6 in our example. This mean that out of 19 frames in the groove, you only have control over 18. In other words, every 3rd note, FamiStudio will inject an empty frame for which you do not have any control. Effects, instrument envelopes & arpeggios will still advance on these empty frames, but otherwise no new note will be processed. You can tell FamiStudio *where* to inject this empty frame, by changing the **Groove Padding Mode** (Beginning, Middle or End).

On of the limitation of FamiStudio tempo mode is that it will limit your ability to suddenly changes tempo in the middle of a pattern. When using FamiStudio tempo, you can only change the BPM at the start of a new pattern.

### FamiTracker Tempo Mode

FamiTracker tempo has a limited visual granularity and relies on effects (delayed notes/cuts) to get frame-level precision. It uses the speed/tempo paradigm. Please check out the [FamiTracker documentation](http://famitracker.com/wiki/index.php?title=Fxx) for a detailed explanation on how the playback speed is affected. If you import a FamiTracker Text or FTM file, the project will be in this tempo mode. 

Same example, but using FamiTracker tempo. Here we do not have the individual frames so we need to use a "delayed cut" effect to achieve the same result. The delayed cut tells the sound engine to insert a stop note after 9 frames have elapsed, achieving the exact same result. That being said, one might argue that it is not very visual and feels like using a Tracker. Moreover, this would not always work correctly on PAL.

![](images/TempoExampleFamiTracker.png#center) 

### Which one to use?

You should use FamiStudio tempo mode if:

* You want to be able to visually control the position of every note at a frame-level precision.
* You are not planning to do smooth tempo changes during the song and you are OK with changing the tempo only at the start of a new pattern.

You should use FamiTracker tempo mode if:

* You are OK with using effects tracks (delayed notes, cuts) to finely tune the start/end of each notes.
* You need compatibility with FamiTracker.
* You want to have smooth tempo changes during the song, especially in the middle of a pattern.

## Expansion audio

The project properties is also where you select your [Expansion Audio](expansion.md). Expansion will add extra channels on top of the default 5 that the NES supported. Note that removing an audio expansion in a project will delete all data (patterns, notes, instrument) related to it.

![](images/EditProjectExp.png#center)

Please visit the [Expansion Audio](expansion.md) section for more detail about each expansion.

## Mixer Settings

Projects can also override some audio settings such as expansion volumes and bass/treble filtering. Settings overriden by the project will take precedence over the global settings (the ones in the [settings dialog](config.md#mixer)). Storing mixer settings in the project guaratees that a project you send to someone will sound exactly as you intended, regardless of their global settings. Note that you can only override settings for the audio expansions that you are using in your project.

![](images/EditProjectMixer.png#center)

Setings overriden by the project can be quickly disabled by clicking the little mixer icon next to the project name. When this icon is dimmed, it means that you do no wish to use the settings override by the project in which case the app will use your own global settings. When the icon is lit, you will be using the project's setting, if any.

![](images/MixerSettingsOverride.png#center)

# Editing songs

THe songs are right below the project name

## Adding/Deleting songs

To add new songs, simply click on the little "+" icon on the song header.

## Reordering songs, instruments, DPCM Samples or Arpeggios

Each section of the project explorer has a little "sort" button (A-Z) next to it. If highlighted, it means items will be kept in alphabetical order. Disabling this will allow you to re-order things to your liking. Re-ordering an item will disable auto-sorting.

## Importing/Merging songs

To merge a song coming from another project, click on the little folder icon on the song header. 

Songs from another project must use the same audio expansion and tempo mode. Also, please note that instruments, samples and arpeggios, used by the other song will be matched by their name. In other words, instruments with the same names are assumed to be the same. If you project already contains an instrument called "Piano" and you try to import a song using another one called "Piano", the existing one will be used. You are responsible to uniquely name your instruments if they are truly different.

## Editing song properties

Clicking the gear icon next to a song or using the "Song/Tempo Properties..." will allow you to change its name, color and other attributes. Names must be unique.

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

* **Groove**: For some BPM values, you will have multiple choices of "grooves". A groove is a sequence of number of frames that will be applied to get close to the desired BPM. This is again a consequence of the fact that the NES runs at 60 FPS (50 FPS on PAL), limiting our ability to achieve exact BPMs.

* **Groove Padding**: FamiStudio will sometimes inject frames where the song does not advance to maintain the correct BPM. You can set a preference as to the location of those "do nothing" frames. Putting them in the middle of notes is preferable since it is less likely to lead to audible unevenness in the tempo.

## Tempo

Configuring tempo in FamiStudio is definitely less intuitive than in a regular DAW, so please bear with me. This mainly for technical reasons (the fact that the NES runs at 50/60 FPS).

The key thing to understand is that the piano roll simply gives you a series of, what I am going to loosely call, **notes** (**rows** if you come from FamiTracker). It is up to the composer to configure these notes and achieve the desired time signature.

For example, let's look at the simple melody of the children song "A, B, C, ...". On the right side are the song settings that were used to achieve this. In this example, the project was set to use the FamiStudio tempo mode, but the same logic applies to the FamiTracker tempo mode. 

[![](images/TempoABC.png#center)](images/TempoABC.png#center)
*Click on the image to zoom in.*

Here is a zoomed-in version of the first beat.
![](images/TempoABCZoom.png#center)
Observations:

* Frames are separated by dashes grey line (they will disappear if you zoom out enough).
* Notes are separated by thin grey lines.
* Beats are separated by thin black lines.
* Patterns are separated by thick black lines.

Here we chosen to have 1 bar = 1 pattern, we could have chosen to fit the whole song in a single pattern, this is totally arbitrary. You should try to use a pattern size is a good trade off between re-usability (being able to copy patterns and re-use them) and size (having many small patterns is annoying).

Also we have chosen to assemble 4 notes into a beat, and 4 beats in a pattern (16 notes), which is how we get something that looks like a 4/4 time signature. This also means our smallest granularity for our melody is 1/16th of a note. That being said, you can move notes at the frame-level, so you actually have a lot more control than this.

When using FamiStudio tempo mode, as you change the BPM, the number of frames (1/60th of a second) in a note may change. At a BPM of 112.5, FamiStudio calculates that we need 8 frames per note. When using FamiTracker tempo, this is the equivalent of the **Speed** parameter.

## Example of time signatures

There is no real concept of time signature in FamiStudio. But given the subdivisions discussed in the previous section, we can group notes in a way that feels like a time signature. 

Here are a few examples of settings to achieve various time signatures, assuming you want 1 bar per pattern. Also, this table assumes that you left "Notes per Beat" to 4, which is the default.

Time Signature | Notes per Pattern | Additional Setting
--- | --- | ---
2/4 | 8 | 
3/4 | 12 | 
4/4 | 16 | 
5/4 | 20 | 
6/8 | 24 | Then double the BPM
2/2 | 8 | Then halve the BPM

You can obviously double or triple the "Notes per Pattern" to have 2 or 3 bars in a pattern, for example. You are free to decide what a pattern represents, it does not have to be 1 bar.

## FamiStudio Tempo & PAL conversion

This is a more technical discussion of how FamiStudio tempo handles NTSC -> PAL and PAL -> NTSC conversion.

### Example of NTSC to PAL conversion

For example, in the image below, we have a NTSC at 150 BPM, so we have frames (1/60th of a sec) per note. On PAL system (50 FPS), if were were to play back this song, it would play 20% slower.

![](images/NtscPalFrames6.png#center)

To faithfully play back our NTSC song on PAL systems, FamiStudio will sometimes run 2 NTSC frames in a single PAL frame so it can keep up with NTSC. 

![](images/PalSkipFrames6.png#center)

This makes the playback speed almost the same, but unfortunately, this is not always this simple. Let's look at an example with a BPM of 112.5, which is 8 frames per note. Again, if we were to try to naively play this back on PAL, it would play back 20% slower than NTSC.

![](images/NtscPalFrames8.png#center)

If we try to apply the same strategy of running 2 NTSC frame once per note, we would still end up playing 5% slower.

![](images/PalSkipFrames8.png#center)

The solution here is to skip 4 NTSC frames, over 3 notes to distribute the error. This ends up reduce the playback speed error well below 1%.

![](images/PalSkipFrames8-3Notes.png#center)

### Tempo Envelopes

So in general, FamiStudio will automatically compute what is called a *tempo envelope* that needs to be applied when playing on the non-native platform (e.g. playing a NTSC song on PAL, and vice versa). This envelope will determine where double-frames must be ran (playing NTSC on PAL), or where idle frames must be inserted (playing PAL on NSTC). 

This tempo envelope will be optimized and will try to place the double/idle frames following these rules:

* Distribute the double/idle frames as evenly as possible while maintaining deterministic positions inside the notes.
* Avoid placing a double/idle frame on the first frame of the note, where the attack usually is.
* Avoid placing a double/idle frame on the last frame of the note, where a silent note could be.
