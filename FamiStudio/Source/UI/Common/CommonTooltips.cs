using System;

namespace FamiStudio
{
    public static class CommonTooltips
    {
        // Custom pattern.
#if !FAMISTUDIO_WINDOWS
        public readonly static string CustomPattern = null;
#else
        public readonly static string CustomPattern = "Enable to use different length or tempo parameter for this pattern.";
#endif

        // Project properties.
        public readonly static string ExpansionAudio = "Expansion audio chip to use. This will add extra audio channels and disable any PAL support.";
        public readonly static string ExpansionNumChannels = "Namco 163 audio supports between 1 and 8 channels. As you add more channels the audio quality will deteriorate.";
        public readonly static string TempoMode = "FamiStudio tempo gives you precise control to every frame, has good PAL/NTSC conversion support and is the recommended way to use FamiStudio.\nFamiTracker tempo behaves like FamiTracker with speed/tempo settings. Use only if you have very specific compatibility needs as support is limited and it will not yield the best FamiStudio experience.";
        public readonly static string AuthoringMachine = "For use with FamiStudio tempo. Defines the machine on which the music is edited. Playback to the other space will be approximate, but still good.";

        // Song properties.
        public readonly static string SongLength = "Number of patterns in the song.";
        public readonly static string Tempo = "This is not the BPM! It is the rate at which the internal tempo counter in incremented.\nValues other than 150 may yield uneven notes. Please see FamiTracker documentation.";
        public readonly static string Speed = "If tempo is 150, number of NTSC frames (1/60th of a second) between each notes.\nLarger values lead to slower tempo. Please see FamiTracker documentation.";
        public readonly static string BPM = "Beats per minute.";
        public readonly static string FramesPerNote = "Number of NTSC frames (1/60th of a second) in a notes.\nLonger notes lead to slower tempo.";
        public readonly static string NotesPerPattern = "Number of notes in a pattern. A pattern is the smallest unit of your song that you may want to repeat multiple times.";
        public readonly static string NotesPerBeat = "Number of notes in a beat. A darker line will be drawn between beats in the piano roll. Affects BPM calculation.";
    }

    public static class TutorialMessages
    {
        public static readonly string[] Messages = new[]
        {
            @"Welcome to FamiStudio! Let's take a few seconds to review some of the basic controls to make sure you use the app to its fullest.",
            @"To PAN around the piano roll or the sequencer, simply PRESS and HOLD the MIDDLE MOUSE BUTTON and DRAG around to smoothly move the viewport. Yes, that wheel on your mouse is also a button!",
            @"To ZOOM in and out in the piano roll or the sequencer, simply rotate the mouse wheel.",
            @"If you are on a TRACKPAD or a LAPTOP, simply enable TRACKPAD CONTROLS in the settings.",
            @"To ADD things like patterns and notes, simply CLICK with the LEFT MOUSE BUTTON.",
            @"To DELETE things like patterns, notes, instruments and songs, simply CLICK on them with the RIGHT MOUSE BUTTON.",
            @"Always keep and eye on the TOOLTIPS! They change constantly as you move the mouse and they will teach you how to use the app! For the complete DOCUMENTATION and over 1 hour of VIDEO TUTORIAL, please click on the big QUESTION MARK!"
        };

        public static readonly string[] Images = new[]
        {
            "Tutorial0.jpg",
            "Tutorial1.jpg",
            "Tutorial2.jpg",
            "Tutorial3.jpg",
            "Tutorial4.jpg",
            "Tutorial5.jpg",
            "Tutorial6.jpg"
        };
    }
}
