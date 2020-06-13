using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace FamiStudio
{
    static class FamiStudioTempoUtils
    {
        private const float FrameTimeMsPAL  = 1000.0f / NesApu.FpsPAL;
        private const float FrameTimeMsNTSC = 1000.0f / NesApu.FpsNTSC;

        // NTSC Note Length  1 Error = 0.15 ms Must run  1 double frames over 6 notes  0/0/0/0/0/1
        // NTSC Note Length  2 Error = 0.15 ms Must run  1 double frames over 3 notes  0/0/1
        // NTSC Note Length  3 Error = 0.15 ms Must run  1 double frames over 2 notes  0/1
        // NTSC Note Length  4 Error = 0.30 ms Must run  2 double frames over 3 notes  0/1/1
        // NTSC Note Length  5 Error = 0.75 ms Must run  5 double frames over 6 notes  0/1/1/1/1/1
        // NTSC Note Length  6 Error = 0.15 ms Must run  1 double frames over 1 notes  1
        // NTSC Note Length  7 Error = 1.05 ms Must run  7 double frames over 6 notes  1/1/1/1/1/2
        // NTSC Note Length  8 Error = 0.60 ms Must run  4 double frames over 3 notes  1/1/2
        // NTSC Note Length  9 Error = 0.45 ms Must run  3 double frames over 2 notes  1/2
        // NTSC Note Length 10 Error = 0.75 ms Must run  5 double frames over 3 notes  1/2/2
        // NTSC Note Length 11 Error = 1.65 ms Must run 11 double frames over 6 notes  1/2/2/2/2/2
        // NTSC Note Length 12 Error = 0.30 ms Must run  2 double frames over 1 notes  2
        // NTSC Note Length 13 Error = 1.70 ms Must run 11 double frames over 5 notes  2/2/2/2/3
        // NTSC Note Length 14 Error = 1.05 ms Must run  7 double frames over 3 notes  2/2/3
        // NTSC Note Length 15 Error = 0.75 ms Must run  5 double frames over 2 notes  2/3
        // NTSC Note Length 16 Error = 1.20 ms Must run  8 double frames over 3 notes  2/3/3
        // NTSC Note Length 17 Error = 0.35 ms Must run 20 double frames over 7 notes  2/3/3/3/3/3/3
        // NTSC Note Length 18 Error = 0.45 ms Must run  3 double frames over 1 notes  3

        // This table gives how a series of notes should perform PAL frame skips
        // in order to maintain pace with NTSC.
        //
        // For example, for a note that last 10 NTSC frame, the minimum PAL error
        // is when skipping 5 frames over 3 notes (30 NTSC frames):
        //
        //   30 * 16.64 ~ 25 * 20.0 (0.8ms error over 30 frames)
        //
        // To distribute these 5 frames over 3 notes:
        //   - First note should skip 1 frame
        //   - Second and & Thirds note should skip 2 frames.
        // 
        // Thus the entry in this table will be { 1, 2 }.

        private static readonly int[,] PalNotesShortLongSkips =
        {
            { 5, 1 }, // Note length 1
            { 2, 1 }, // Note length 2
            { 1, 1 }, // Note length 3
            { 1, 2 }, // Note length 4
            { 1, 5 }, // Note length 5
            { 0, 1 }, // Note length 6

            { 5, 1 }, // Note length 7
            { 2, 1 }, // Note length 8
            { 1, 1 }, // Note length 9
            { 1, 2 }, // Note length 10
            { 1, 5 }, // Note length 11
            { 0, 1 }, // Note length 12

            { 4, 1 }, // Note length 13
            { 2, 1 }, // Note length 14
            { 1, 1 }, // Note length 15
            { 1, 2 }, // Note length 16
            { 1, 6 }, // Note length 17
            { 0, 1 }  // Note length 18
        };

        // PAL Note Length  1 Error = 0.15 ms (0.15 %) Must skip  1 frames over 5 notes 0/0/0/0/1
        // PAL Note Length  2 Error = 0.30 ms (0.15 %) Must skip  2 frames over 5 notes 0/0/0/1/1
        // PAL Note Length  3 Error = 0.45 ms (0.15 %) Must skip  3 frames over 5 notes 0/0/1/1/1
        // PAL Note Length  4 Error = 0.60 ms (0.15 %) Must skip  4 frames over 5 notes 0/1/1/1/1
        // PAL Note Length  5 Error = 0.15 ms (0.15 %) Must skip  1 frames over 1 notes 1
        // PAL Note Length  6 Error = 0.90 ms (0.15 %) Must skip  6 frames over 5 notes 1/1/1/1/2
        // PAL Note Length  7 Error = 1.05 ms (0.15 %) Must skip  7 frames over 5 notes 1/1/1/2/2
        // PAL Note Length  8 Error = 1.20 ms (0.15 %) Must skip  8 frames over 5 notes 1/1/2/2/2
        // PAL Note Length  9 Error = 1.35 ms (0.15 %) Must skip  9 frames over 5 notes 1/2/2/2/2
        // PAL Note Length 10 Error = 0.30 ms (0.15 %) Must skip  2 frames over 1 notes 2
        // PAL Note Length 11 Error = 1.65 ms (0.15 %) Must skip 11 frames over 5 notes 2/2/2/2/3
        // PAL Note Length 12 Error = 1.80 ms (0.15 %) Must skip 12 frames over 5 notes 2/2/2/3/3
        // PAL Note Length 13 Error = 1.96 ms (0.15 %) Must skip 13 frames over 5 notes 2/2/3/3/3
        // PAL Note Length 14 Error = 2.11 ms (0.15 %) Must skip 14 frames over 5 notes 2/3/3/3/3
        // PAL Note Length 15 Error = 0.45 ms (0.15 %) Must skip  3 frames over 1 notes 3
        // PAL Note Length 16 Error = 1.40 ms (0.11 %) Must skip 13 frames over 4 notes 3/3/3/4
        // PAL Note Length 17 Error = 2.31 ms (0.34 %) Must skip  7 frames over 2 notes 3/4
        // PAL Note Length 18 Error = 1.70 ms (0.16 %) Must skip 11 frames over 3 notes 3/4/4

        private static byte[][] PalSkipEnvelopes = new byte[Song.MaxNoteLength][];

        private static int[] GetDefaultPalSkipFrames(int noteLength)
        {
            int numSkipFrames = (int)Math.Ceiling(noteLength / 6.0f);

            var frames = new int[numSkipFrames];

            if (noteLength == 2)
            {
                frames[0] = 1;
            }
            else
            {
                // By default, put the skip frames in the middle of the notes, this is
                // where its least likely to have anything interesting (attacks tend
                // to be at the beginning, stop notes at the end).
                for (int i = 0; i < numSkipFrames; i++)
                {
                    float ratio = (i + 0.5f) / (numSkipFrames);
                    frames[i] = (int)Math.Round(ratio * (noteLength - 1));
                }
            }

            return frames;
        }

        private static void BuildPalSkipEnvelopes()
        {
            for (var n = 1; n <= Song.MaxNoteLength; n++)
            {
                var numShortSkips = PalNotesShortLongSkips[n - 1, 0];
                var numLongSkips  = PalNotesShortLongSkips[n - 1, 1];
                var numNotes      = numShortSkips + numLongSkips;

                var noteSkipsLong  = GetDefaultPalSkipFrames(n);
                var noteSkipsShort = new int[noteSkipsLong.Length - 1];
                Array.Copy(noteSkipsLong, noteSkipsShort, noteSkipsShort.Length);

                var envelope = new List<byte>();

                var lastFrameIndex = 0;
                for (int i = 0; i < numNotes; i++)
                {
                    var noteSkips = i >= numShortSkips ? noteSkipsLong : noteSkipsShort;

                    for (int j = 0; j < noteSkips.Length; j++)
                    {
                        var frameIndex = i* n + noteSkips[j];
                        envelope.Add((byte)(frameIndex - lastFrameIndex));
                        lastFrameIndex = frameIndex;
                    }
                }

                int sum = 0;
                for (int i = 0; i < envelope.Count; i++)
                    sum += envelope[i];

                Debug.Assert(sum + n - noteSkipsLong[noteSkipsLong.Length - 1] == n * numNotes);

                envelope.Add((byte)(n - noteSkipsLong[noteSkipsLong.Length - 1] + envelope[0]));
                envelope.Add(0x80);

                Debug.Assert(envelope[0] >= 2);

                PalSkipEnvelopes[n - 1] = envelope.ToArray();
            }
        }

        public static byte[] GetPalSkipEnvelope(int noteLength)
        {
            return PalSkipEnvelopes[noteLength - 1];
        }

        public static void Initialize()
        {
#if DEBUG
            DumpNtscInfo();
            DumpPalInfo();
#endif
            BuildPalSkipEnvelopes();
        }

#if DEBUG
        public static void DumpNtscInfo()
        {
            var numNotes    = new int[Song.MaxNoteLength];
            var frameCounts = new int[Song.MaxNoteLength];

            for (var n = 1; n <= Song.MaxNoteLength; n++)
            {
                var bestNumNotes = 0;
                var bestFrameCount = 0;
                var minOverallError = 999.9f;

                for (int i = 1; i < 10; i++)
                {
                    var durationNTSC = n * i * FrameTimeMsNTSC;

                    var minError = 9999.0f;
                    var bestLength = 0;

                    for (int j = n * i; j >= 1; j--)
                    {
                        var durationPAL = j * FrameTimeMsPAL;

                        var error = Math.Abs(durationNTSC - durationPAL);
                        if (error < minError)
                        {
                            minError = error;
                            bestLength = j;
                        }
                    }

                    if (minError < minOverallError)
                    {
                        minOverallError = minError;
                        bestNumNotes = i;
                        bestFrameCount = bestLength;
                    }
                }

                numNotes[n - 1] = bestNumNotes;
                frameCounts[n - 1] = bestFrameCount;

                Debug.WriteLine($"NTSC Note Length {n} Error = {minOverallError:0.00} ms ({minOverallError * 100.0f / (float)(n * bestNumNotes * FrameTimeMsNTSC):0.00} %) Must run {n * bestNumNotes - bestFrameCount} double frames over {bestNumNotes} notes in PAL mode.");
            }
        }

        public static void DumpPalInfo()
        {
            var numNotes = new int[Song.MaxNoteLength];
            var frameCounts = new int[Song.MaxNoteLength];

            for (var n = 1; n <= Song.MaxNoteLength; n++)
            {
                var bestNumNotes = 0;
                var bestFrameCount = 0;
                var minOverallError = 999.9f;

                for (int i = 1; i < 6; i++)
                {
                    var durationPAL = n * i * FrameTimeMsPAL;

                    var minError = 9999.0f;
                    var bestLength = 0;

                    for (int j = n * i * 2; j >= 1; j--)
                    {
                        var durationNTSC = j * FrameTimeMsNTSC;

                        var error = Math.Abs(durationNTSC - durationPAL);
                        if (error < minError)
                        {
                            minError = error;
                            bestLength = j;
                        }
                    }

                    if (minError < minOverallError)
                    {
                        minOverallError = minError;
                        bestNumNotes = i;
                        bestFrameCount = bestLength;
                    }
                }

                numNotes[n - 1] = bestNumNotes;
                frameCounts[n - 1] = bestFrameCount;

                Debug.WriteLine($"PAL Note Length {n} Error = {minOverallError:0.00} ms ({minOverallError * 100.0f / (float)(n * bestNumNotes * FrameTimeMsPAL):0.00} %) Must skip {bestFrameCount - n * bestNumNotes} frames over {bestNumNotes} notes in NTSC mode.");
            }
        }
#endif
    }
}

