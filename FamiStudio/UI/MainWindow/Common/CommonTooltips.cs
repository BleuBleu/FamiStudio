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
        public readonly static string Tempo = "Rate at which the internal tempo counter in incremented. Values other than 150 may yield uneven notes. Please see FamiTracker documentation.";
        public readonly static string Speed = "If tempo is 150, number of NTSC frames (1/60th of a second) between each notes. Larger values lead to slower tempo. Please see FamiTracker documentation.";
        public readonly static string BPM = "Beats per minute.";
        public readonly static string FramesPerNote = "Number of NTSC frames (1/60th of a second) in a notes. Longer notes lead to slower tempo.";
        public readonly static string NotesPerPattern = "Number of notes in a pattern. A pattern is the smallest unit of your song that you may want to repeat multiple times.";
        public readonly static string NotesPerBar = "Number of notes in a bar. This is purely cosmetic. A darker line will be drawn between bars in the piano roll.";
    }
}
