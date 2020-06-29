using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace FamiStudio
{
    static class FamiStudioTempoUtils
    {
        //NTSC Note Length  1 Error = 0.15 ms (0.15 %). Must run  1 double frames over 6 notes in PAL mode.
        //NTSC Note Length  2 Error = 0.15 ms (0.15 %). Must run  1 double frames over 3 notes in PAL mode.
        //NTSC Note Length  3 Error = 0.15 ms (0.15 %). Must run  1 double frames over 2 notes in PAL mode.
        //NTSC Note Length  4 Error = 0.30 ms (0.15 %). Must run  2 double frames over 3 notes in PAL mode.
        //NTSC Note Length  5 Error = 0.75 ms (0.15 %). Must run  5 double frames over 6 notes in PAL mode.
        //NTSC Note Length  6 Error = 0.15 ms (0.15 %). Must run  1 double frames over 1 notes in PAL mode.
        //NTSC Note Length  7 Error = 1.05 ms (0.15 %). Must run  7 double frames over 6 notes in PAL mode.
        //NTSC Note Length  8 Error = 0.60 ms (0.15 %). Must run  4 double frames over 3 notes in PAL mode.
        //NTSC Note Length  9 Error = 0.45 ms (0.15 %). Must run  3 double frames over 2 notes in PAL mode.
        //NTSC Note Length 10 Error = 0.75 ms (0.15 %). Must run  5 double frames over 3 notes in PAL mode.
        //NTSC Note Length 11 Error = 1.65 ms (0.15 %). Must run 11 double frames over 6 notes in PAL mode.
        //NTSC Note Length 12 Error = 0.30 ms (0.15 %). Must run  2 double frames over 1 notes in PAL mode.
        //NTSC Note Length 13 Error = 1.96 ms (0.15 %). Must run 13 double frames over 6 notes in PAL mode.
        //NTSC Note Length 14 Error = 1.05 ms (0.15 %). Must run  7 double frames over 3 notes in PAL mode.
        //NTSC Note Length 15 Error = 0.75 ms (0.15 %). Must run  5 double frames over 2 notes in PAL mode.
        //NTSC Note Length 16 Error = 1.20 ms (0.15 %). Must run  8 double frames over 3 notes in PAL mode.
        //NTSC Note Length 17 Error = 2.56 ms (0.15 %). Must run 17 double frames over 6 notes in PAL mode.
        //NTSC Note Length 18 Error = 0.45 ms (0.15 %). Must run  3 double frames over 1 notes in PAL mode.

        // The note where the bit is set will be inaudible.
        // Frames were hand-placed to avoid the first/last note (attack/stop) and the 2 middle spots (for half notes)
        private static readonly int[][] NtscSourcePalTargetLookup = 
        {
            /*  1 ( 1 over 6) */ new [] { 0b0,0b0,0b0,0b0,0b0,0b1 },
            /*  2 ( 1 over 3) */ new [] { 0b00,0b00,0b01 },
            /*  3 ( 1 over 2) */ new [] { 0b000,0b010 },
            /*  4 ( 2 over 3) */ new [] { 0b0000,0b0100,0b0100 },
            /*  5 ( 5 over 6) */ new [] { 0b00000,0b01000,0b01000,0b01000,0b01000,0b01000 },
            /*  6 ( 1 over 1) */ new [] { 0b010000 },
            /*  7 ( 7 over 6) */ new [] { 0b0100000,0b0100000,0b0100000,0b0100000,0b0100000,0b0100010 },
            /*  8 ( 4 over 3) */ new [] { 0b01000000,0b01000000,0b01000100 },
            /*  9 ( 3 over 2) */ new [] { 0b010000000,0b010001000 },
            /* 10 ( 5 over 3) */ new [] { 0b0100000000,0b0100001000,0b0100001000 },
            /* 11 (11 over 6) */ new [] { 0b01000000000,0b01000001000,0b01000001000,0b01000001000,0b01000001000,0b01000001000 },
            /* 12 ( 2 over 1) */ new [] { 0b010000010000 },
            /* 13 (13 over 6) */ new [] { 0b0100000001000,0b0100000001000,0b0100000001000,0b0100000001000,0b0100000001000,0b0100010001000 },
            /* 14 ( 7 over 3) */ new [] { 0b01000000001000,0b01000000001000,0b01000100001000 },
            /* 15 ( 5 over 2) */ new [] { 0b010000000001000,0b010000100001000 },
            /* 16 ( 8 over 3) */ new [] { 0b0100000000010000,0b0100001000010000,0b0100001000010000 },
            /* 17 (17 over 6) */ new [] { 0b01000000000001000,0b01000001000001000,0b01000001000001000,0b01000001000001000,0b01000001000001000,0b01000001000001000 },
            /* 18 ( 3 over 1) */ new [] { 0b010000010000010000 },
        };

        // PAL Note Length  1 Error = 0.15 ms (0.15 %). Must skip  1 frames over 5 notes in NTSC mode.
        // PAL Note Length  2 Error = 0.30 ms (0.15 %). Must skip  2 frames over 5 notes in NTSC mode.
        // PAL Note Length  3 Error = 0.45 ms (0.15 %). Must skip  3 frames over 5 notes in NTSC mode.
        // PAL Note Length  4 Error = 0.60 ms (0.15 %). Must skip  4 frames over 5 notes in NTSC mode.
        // PAL Note Length  5 Error = 0.15 ms (0.15 %). Must skip  1 frames over 1 notes in NTSC mode.
        // PAL Note Length  6 Error = 0.90 ms (0.15 %). Must skip  6 frames over 5 notes in NTSC mode.
        // PAL Note Length  7 Error = 1.05 ms (0.15 %). Must skip  7 frames over 5 notes in NTSC mode.
        // PAL Note Length  8 Error = 1.20 ms (0.15 %). Must skip  8 frames over 5 notes in NTSC mode.
        // PAL Note Length  9 Error = 1.35 ms (0.15 %). Must skip  9 frames over 5 notes in NTSC mode.
        // PAL Note Length 10 Error = 0.30 ms (0.15 %). Must skip  2 frames over 1 notes in NTSC mode.
        // PAL Note Length 11 Error = 1.65 ms (0.15 %). Must skip 11 frames over 5 notes in NTSC mode.
        // PAL Note Length 12 Error = 1.80 ms (0.15 %). Must skip 12 frames over 5 notes in NTSC mode.
        // PAL Note Length 13 Error = 1.96 ms (0.15 %). Must skip 13 frames over 5 notes in NTSC mode.
        // PAL Note Length 14 Error = 2.11 ms (0.15 %). Must skip 14 frames over 5 notes in NTSC mode.
        // PAL Note Length 15 Error = 0.45 ms (0.15 %). Must skip  3 frames over 1 notes in NTSC mode.
        // PAL Note Length 16 Error = 2.41 ms (0.15 %). Must skip 16 frames over 5 notes in NTSC mode.
        // PAL Note Length 17 Error = 2.56 ms (0.15 %). Must skip 17 frames over 5 notes in NTSC mode.
        // PAL Note Length 18 Error = 2.71 ms (0.15 %). Must skip 18 frames over 5 notes in NTSC mode.

        // The note where the bit is set, the engine will take a 1 frame pause.
        // Frames were hand-placed to avoid the first/last note (attack/stop) and the 2 middle spots (for half notes)
        private static readonly int[][] PalSourceNtscTargetLookup =
        {
            /*  1 ( 1 over 5) */ new [] { 0b0,0b0,0b0,0b0,0b1 },
            /*  2 ( 2 over 5) */ new [] { 0b00,0b01,0b00,0b01,0b00 },
            /*  3 ( 3 over 5) */ new [] { 0b010,0b000,0b010,0b000,0b010 },
            /*  4 ( 4 over 5) */ new [] { 0b0000,0b0100,0b0100,0b0100,0b0100 },
            /*  5 ( 1 over 1) */ new [] { 0b01000 },
            /*  6 ( 6 over 5) */ new [] { 0b010000,0b010000,0b010000,0b010000,0b010010 },
            /*  7 ( 7 over 5) */ new [] { 0b0100000,0b0100010,0b0100000,0b0100010,0b0100000 },
            /*  8 ( 8 over 5) */ new [] { 0b01000100,0b01000000,0b01000100,0b01000000,0b01000100 },
            /*  9 ( 9 over 5) */ new [] { 0b010000000,0b010000100,0b010000100,0b010000100,0b010000100 },
            /* 10 ( 2 over 1) */ new [] { 0b0100001000 },
            /* 11 (11 over 5) */ new [] { 0b01000001000,0b01000001000,0b01000001000,0b01000001000,0b01001001000 },
            /* 12 (12 over 5) */ new [] { 0b010000000100,0b010010000100,0b010000000100,0b010010000100,0b010000000100 },
            /* 13 (13 over 5) */ new [] { 0b0100010001000,0b0100000001000,0b0100010001000,0b0100000001000,0b0100010001000 },
            /* 14 (14 over 5) */ new [] { 0b01000000001000,0b01000100001000,0b01000100001000,0b01000100001000,0b01000100001000 },
            /* 15 ( 3 over 1) */ new [] { 0b010000100001000 },
            /* 16 (16 over 5) */ new [] { 0b0010000000100010,0b0010000000100010,0b0010000000100010,0b0010000000100010,0b0010001000100010 },
            /* 17 (17 over 5) */ new [] { 0b00100000001000100,0b00100010001000100,0b00100000001000100,0b00100010001000100,0b00100000001000100},
            /* 18 (18 over 5) */ new [] { 0b001000100010000100,0b001000000010000100,0b001000100010000100,0b001000000010000100,0b001000100010000100 }
        };

        private static byte[][] NtscToPalTempoEnvelopes = new byte[Song.MaxNoteLength][];
        private static byte[][] PalToNtscTempoEnvelopes = new byte[Song.MaxNoteLength][];

        private static void BuildTempoEnvelopes(bool pal)
        {
            var lookup = pal ? PalSourceNtscTargetLookup : NtscSourcePalTargetLookup;

            for (int i = 0; i < lookup.Length; i++)
            {
                var noteLength = i + 1;
                var frames = lookup[i];
                var totalFrames = noteLength * frames.Length;
                var numBitSets = 0;
                var lastFrameIndex = -1;
                var firstFrameIndex = -1;
                var envelope = new List<byte>();
                var sum = 0;

                for (int n = 0; n < frames.Length; n++)
                {
                    var bitPattern = frames[n];

                    for (int f = 0, b = 1 << i; f < noteLength; f++, b >>= 1)
                    {
                        if ((bitPattern & b) != 0)
                        {
                            var frameIndex = n * noteLength + f;
                            var frameDelta = frameIndex - lastFrameIndex;
                            envelope.Add((byte)(frameDelta + (pal ? 1 : -1)));
                            sum += frameDelta;
                            lastFrameIndex = frameIndex;
                            if (firstFrameIndex < 0) firstFrameIndex = frameIndex;
                        }
                    }

                    numBitSets += Utils.NumberOfSetBits(bitPattern);
                }

                if (pal)
                    envelope[0]--;

                var remainingFrames = totalFrames - sum;

                if (remainingFrames != 0)
                    envelope.Add((byte)(remainingFrames + firstFrameIndex + 1 + (pal ? 1 : -1)));
                envelope.Add(0x80);

                if (pal)
                    PalToNtscTempoEnvelopes[i] = envelope.ToArray();
                else
                    NtscToPalTempoEnvelopes[i] = envelope.ToArray();

                Debug.WriteLine($"{(pal ? "PAL" : "NTSC")} note length {noteLength} has {numBitSets} bit sets over {frames.Length} notes.");
            }
        }

        public static byte[] GetTempoEnvelope(int noteLength, bool palSource)
        {
            return palSource ? 
                PalToNtscTempoEnvelopes[noteLength - 1] :
                NtscToPalTempoEnvelopes[noteLength - 1];
        }

        public static void Initialize()
        {
#if DEBUG
            DumpFrameSkipInfo(false);
            DumpFrameSkipInfo(true);
#endif
            BuildTempoEnvelopes(false);
            BuildTempoEnvelopes(true);
        }

#if DEBUG
        public static void DumpFrameSkipInfo(bool pal)
        {
            const float frameTimeMsPAL  = 1000.0f / NesApu.FpsPAL;
            const float frameTimeMsNTSC = 1000.0f / NesApu.FpsNTSC;


            // 5/6th is pretty much the perfect ratio between PAL/NTSC. 
            // Look for how many notes of one machine you need to run on the other
            // until you find a number of frame that is divisible by 5 (pal) or 6 (ntsc).

            var divider = pal ? 5 : 6;
            var frameTimeSource = pal ? frameTimeMsPAL  : frameTimeMsNTSC;
            var frameTimeTarget = pal ? frameTimeMsNTSC : frameTimeMsPAL;

            for (var n = 1; n <= Song.MaxNoteLength; n++)
            {
                for (int i = 1; i < 10; i++)
                {
                    int numFrames = n * i;

                    if ((numFrames % divider) == 0)
                    {
                        var numFrameSkipped = numFrames / divider;
                        var durationSource  = numFrames * frameTimeSource;
                        var durationTarget  = (numFrames + (pal ? numFrameSkipped : -numFrameSkipped)) * frameTimeTarget;
                        var error = Math.Abs(durationTarget - durationSource);

                        if (pal)
                            Debug.WriteLine($"PAL Note Length  {n} Error = {error:0.00} ms ({error * 100 / durationSource:0.00} %). Must skip {numFrameSkipped} frames over {i} notes in NTSC mode.");
                        else
                            Debug.WriteLine($"NTSC Note Length {n} Error = {error:0.00} ms ({error * 100 / durationSource:0.00} %). Must run {numFrameSkipped} double frames over {i} notes in PAL mode.");

                        break;
                    }
                }

            }
        }
#endif
    }
}

