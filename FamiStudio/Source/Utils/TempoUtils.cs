using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace FamiStudio
{
    static class FamiStudioTempoUtils
    {
        public const int MinNoteLength = 1;
        public const int MaxNoteLength = 18;

        private const float BpmThreshold = 0.5f;

        // We limit ourselves to grooves composed of just 2 note sizes.
        private static readonly int[][] GroovePatterns = new[]
        {
            new[] { 0 },
            new[] { 1, 0 },
            new[] { 1, 0, 0, 0 },
            new[] { 1, 1, 1, 0 },
            new[] { 1, 0, 0 },
            new[] { 1, 1, 0 },
            new[] { 1, 0, 0, 0, 0, 0, 0, 0 },
            new[] { 1, 1, 1, 1, 1, 1, 1, 0 },
            new[] { 1, 0, 1, 0, 1, 0, 0, 0 }
        };

        // This essentially build a table very similar to this:
        // http://famitracker.com/wiki/index.php?title=Common_tempo_values
        public static TempoInfo[] GetAvailableTempos(bool pal, int notesPerBeat)
        {
            List<TempoInfo>[] tempos = new List<TempoInfo>[2] { new List<TempoInfo>(), new List<TempoInfo>() };

            // We build both NTSC/PAL in parallel since we need to make sure that all grooves
            // present in one is in the other as well, since use may convert their project
            // from/to NTSC/PAL at run-time. A groove missing in one or the other will lead to
            // a crash.
            tempos[0].Add(new TempoInfo(new int[] { MaxNoteLength }, false, notesPerBeat));
            tempos[1].Add(new TempoInfo(new int[] { MaxNoteLength }, true,  notesPerBeat));

            foreach (var pattern in GroovePatterns)
            {
                for (int noteLen = MaxNoteLength - 1; noteLen >= MinNoteLength; noteLen--)
                {
                    var groove = pattern.Clone() as int[];

                    for (int i = 0; i < groove.Length; i++)
                        groove[i] += noteLen;

                    // See if this new groove is different enough from the other we added so far.
                    var foundSimilar = false;

                    // Force include the simplest grooves (length 1 and 2)
                    if (groove.Length > 2)
                    {
                        // Compute BPM with 4 notes per beat so we get the same grooves for all note lengths.
                        var bpms = new float[] 
                        {
                            ComputeBpmForGroove(false, groove, 4),
                            ComputeBpmForGroove(true,  groove, 4) 
                        };

                        if (MathF.Abs(50.3f - bpms[0]) < 0.1f)
                        {
                            Debug.WriteLine("");
                        }

                        var idx0 = tempos[0].FindIndex(t => Math.Abs(t.bpm - bpms[0]) < BpmThreshold);
                        var idx1 = tempos[1].FindIndex(t => Math.Abs(t.bpm - bpms[1]) < BpmThreshold);
                        
                        if (idx0 >= 0 && idx1 >= 0)
                        {
                            foundSimilar = true;
                        }
                    }

                    if (!foundSimilar)
                    {
                        tempos[0].Add(new TempoInfo(groove, false, 4));
                        tempos[1].Add(new TempoInfo(groove, true,  4));
                    }
                }
            }

            var finalList = pal ? tempos[1] : tempos[0];

            // Recompute correct BPM.
            foreach (var tempo in finalList)
                tempo.bpm = ComputeBpmForGroove(pal, tempo.groove, notesPerBeat);

            // Sort.
            finalList.Sort((t1, t2) => t1.bpm.CompareTo(t2.bpm));

#if false
            Debug.WriteLine($"{(pal ? "PAL" : "NTSC")} tempo list ({finalList.Count} entries):");
            foreach (var tempo in finalList)
            {
                Debug.WriteLine($"  * {tempo.bpm.ToString("n1")} = {string.Join("-", tempo.groove)}");
            }
#endif

            return finalList.ToArray();
        }
        
        public static int FindTempoFromGroove(TempoInfo[] tempoList, int[] groove)
        {
            var sortedGroove = groove.Clone() as int[];
            Array.Sort(sortedGroove);

            for (int i = 0; i < tempoList.Length; i++)
            {
                var sortedTempoGroove = tempoList[i].groove.Clone() as int[];
                Array.Sort(sortedTempoGroove);

                if (Utils.CompareArrays(sortedGroove, sortedTempoGroove) == 0)
                    return i;
            }

            return -1;
        }

        public static float ComputeBpmForGroove(bool pal, int[] groove, int notesPerBeat)
        {
            var grooveNumFrames = 0;
            var grooveLength = 0;

            do
            {
                grooveNumFrames += Utils.Sum(groove);
                grooveLength += groove.Length;
            }
            while ((grooveLength % notesPerBeat) != 0);

            float numer = pal ? 3000.0f : 3600.0f;
            float denom = grooveNumFrames / (float)grooveLength * notesPerBeat;

            return numer / denom;
        }

        // Give a groove, like 7-6-6-6, returns all possible permutations.
        //  - 7-6-6-6
        //  - 6-7-6-6
        //  - 6-6-7-6
        //  - 6-6-6-7

        // MATTT : Cache those, this can be like 1/2 seconde on mobile. (ex: 654.5 BPM)
        public static int[][] GetAvailableGrooves(int[] groove)
        {
            ValidateGroove(groove);

            // Get all permutations
            var permutations = new List<int[]>();
            Utils.Permutations(groove, permutations);

            return permutations.ToArray();
        }

        // Make sure the groove has been generated by us.
        public static bool ValidateGroove(int[] groove)
        {
            var min = Utils.Min(groove);
            var max = Utils.Max(groove);

            bool valid = false;
            if (min == max)
            {
                valid =
                    groove.Length == 1 &&
                    min >= MinNoteLength &&
                    min <= MaxNoteLength;
            }
            else
            {
                valid =
                    groove.Length <= 8 &&
                    (max - min) <= 1 &&
                    min >= MinNoteLength &&
                    min <= MaxNoteLength &&
                    max >= MinNoteLength &&
                    min <= MaxNoteLength;
            }

            Debug.Assert(valid);

            return valid;
        }

        private class CacheGrooveLengthKey
        {
            public int[] groove;
            public int   groovePadMode;

            public override int GetHashCode()
            {
                var hash = groove[0];
                if (groove.Length > 1) hash |= groove[1] << 8;
                if (groove.Length > 2) hash |= groove[2] << 16;
                if (groove.Length > 3) hash |= groove[3] << 24;
                hash = Utils.HashCombine(hash, groovePadMode);
                return hash;
            }

            public override bool Equals(object obj)
            {
                var other = obj as CacheGrooveLengthKey;

                if (other == null)
                    return false;

                return Utils.CompareArrays(groove, other.groove) == 0 &&
                    groovePadMode == other.groovePadMode;
            }
        }

        static ThreadLocal<Dictionary<CacheGrooveLengthKey, int[]>> cachedGrooveLengths = new ThreadLocal<Dictionary<CacheGrooveLengthKey, int[]>>(() => new Dictionary<CacheGrooveLengthKey, int[]>());

        public static int ComputeNumberOfFrameForGroove(int length, int[] groove, int groovePadMode)
        {
            // Look in the cache first.
            var key = new CacheGrooveLengthKey() { groove = groove, groovePadMode = groovePadMode};

            if (cachedGrooveLengths.Value.TryGetValue(key, out var grooveLengthArray))
                return grooveLengthArray[length];

            grooveLengthArray = new int[Pattern.MaxLength + 1];

            // Add to cache if not found.
            var idx = 0;
            var grooveIterator = new GrooveIterator(groove, groovePadMode);

            // This is really not optimal, not need to run the full iterator until the very end.
            for (int i = 0; i < grooveLengthArray.Length; i++)
            {
                if (grooveIterator.IsPadFrame)
                    grooveIterator.Advance();

                grooveLengthArray[idx++] = grooveIterator.FrameIndex;

                grooveIterator.Advance();
            }

            cachedGrooveLengths.Value.Add(key, grooveLengthArray);

            return grooveLengthArray[length];
        }

        private class CachedTempoEnvelopeKey
        {
            public int[] groove;
            public int   groovePadMode;
            public bool  palSource;

            public override int GetHashCode()
            {
                var hash = groove[0];
                if (groove.Length > 1) hash |= groove[1] << 8;
                if (groove.Length > 2) hash |= groove[2] << 16;
                if (groove.Length > 3) hash |= groove[3] << 24;

                hash = Utils.HashCombine(hash, groovePadMode);
                hash = Utils.HashCombine(hash, palSource ? 1 : 0);

                return hash;
            }

            public override bool Equals(object obj)
            {
                var other = obj as CachedTempoEnvelopeKey;

                if (other == null)
                    return false;

                return Utils.CompareArrays(groove, other.groove) == 0 &&
                    groovePadMode == other.groovePadMode &&
                    palSource == other.palSource;
            }
        };

        private static ThreadLocal<Dictionary<CachedTempoEnvelopeKey, byte[]>> cachedTempoEnvelopes = new ThreadLocal<Dictionary<CachedTempoEnvelopeKey, byte[]>>(() => new Dictionary<CachedTempoEnvelopeKey, byte[]>());

        public static byte[] GetTempoEnvelope(int[] groove, int groovePadMode, bool palSource)
        {
            // Look in the cache first.
            var key = new CachedTempoEnvelopeKey() { groove = groove, groovePadMode = groovePadMode, palSource = palSource };

            if (cachedTempoEnvelopes.Value.TryGetValue(key, out var env))
                return env;

            // Otherwise build.
            var dstFactor = palSource ? 6 : 5;
            var srcFactor = palSource ? 5 : 6;
            var noteLength = Utils.Min(groove);
            var grooveNumFrames = Utils.Sum(groove);
            var grooveRepeatCount = 1;

            // Repeat the groove until we have something perfectly divisible by 6 (5 on PAL).
            while ((grooveNumFrames % srcFactor) != 0)
            {
                grooveNumFrames += Utils.Sum(groove);
                grooveRepeatCount++;
            }

            // Figure out how many frames that is on the playback machine.
            var adaptedNumFrames = grooveNumFrames / srcFactor * dstFactor;

            // Mark some frames as "important", this will typically be the first 
            // and last frame of the note. This will preserve the attack and 
            // 1-frame silence between notes.
            var importantFrames = new bool[grooveNumFrames];
            var frameIndex = 0;

            for (int i = 0; i < grooveRepeatCount; i++)
            {
                for (int j = 0; j < groove.Length; j++)
                {
                    if (groove[j] == noteLength)
                    {
                        importantFrames[frameIndex] = true;
                        importantFrames[frameIndex + noteLength - 1] = true;
                    }
                    else
                    {
                        if (groovePadMode != GroovePaddingType.Beginning || noteLength == 1)
                            importantFrames[frameIndex] = true;
                        else
                            importantFrames[frameIndex + 1] = true;

                        if (groovePadMode != GroovePaddingType.End || noteLength == 1)
                            importantFrames[frameIndex + noteLength] = true;
                        else
                            importantFrames[frameIndex + noteLength - 1] = true;
                    }

                    frameIndex += groove[j];
                }
            }

#if FALSE
        var numSkipFrames = palSource ? adaptedNumFrames - grooveNumFrames : grooveNumFrames - adaptedNumFrames;
        var bestScore  = int.MaxValue;
        var bestOffset = -1;

        for (int i = 0; i < srcFactor; i++)
        {
            var score = 0;

            frameIndex = i;
            for (int j = 0; j < numSkipFrames; j++)
            {
                if (importantFrames[frameIndex])
                    score++;
                frameIndex += srcFactor;
            }

            if (score < bestScore)
            {
                bestScore  = score;
                bestOffset = i;
            }
        }
#else
            // Start by distributing the skip (or double) frames evenly.
            var numSkipFrames = palSource ? adaptedNumFrames - grooveNumFrames : grooveNumFrames - adaptedNumFrames;
            var skipFrames = new bool[grooveNumFrames];

            frameIndex = srcFactor / 2;
            for (int i = 0; i < numSkipFrames; i++)
            {
                skipFrames[frameIndex] = true;
                frameIndex += srcFactor;
            }

            int GetFrameCost(int idx)
            {
                if (!skipFrames[idx])
                    return 0;

                var cost = 0;

                // Penalize important frames
                if (importantFrames[idx])
                    cost += srcFactor;

                // Look right for another skipped frame.
                for (int i = 1; i < srcFactor; i++)
                {
                    var nextIdx = idx + i;
                    if (nextIdx >= skipFrames.Length)
                        nextIdx -= skipFrames.Length;
                    if (skipFrames[nextIdx])
                    {
                        // The closer we are, the higher the cost.
                        cost += (srcFactor - i);
                        break;
                    }
                }

                // Look left for another skipped frame.
                for (int i = 1; i < srcFactor; i++)
                {
                    var prevIdx = idx - i;
                    if (prevIdx < 0)
                        prevIdx += skipFrames.Length;
                    // The closer we are, the higher the cost.
                    if (skipFrames[prevIdx])
                    {
                        cost += (srcFactor - i);
                        break;
                    }
                }

                return cost;
            }

            var frameCosts = new int[grooveNumFrames];

            // Optimize.
            for (int i = 0; i < 100; i++)
            {
                // Update costs.
                var maxCost = -10;
                var maxCostIndex = -1;
                var totalCost = 0;

                for (int j = 0; j < frameCosts.Length; j++)
                {
                    var cost = GetFrameCost(j);

                    frameCosts[j] = cost;
                    totalCost += cost;

                    if (cost > maxCost)
                    {
                        maxCost = cost;
                        maxCostIndex = j;
                    }
                }

                if (maxCost == 0)
                    break;

                var currentFrameCost = GetFrameCost(maxCostIndex);

                // Try to optimize the most expensive frame by moving it to the left.
                if (maxCostIndex > 0 && !skipFrames[maxCostIndex - 1] && !importantFrames[maxCostIndex - 1])
                {
                    Utils.Swap(ref skipFrames[maxCostIndex], ref skipFrames[maxCostIndex - 1]);
                    if (GetFrameCost(maxCostIndex - 1) < currentFrameCost)
                        continue;
                    Utils.Swap(ref skipFrames[maxCostIndex], ref skipFrames[maxCostIndex - 1]);
                }

                // Try to optimize the most expensive frame by moving it to the right.
                if (maxCostIndex < skipFrames.Length - 1 && !skipFrames[maxCostIndex + 1] && !importantFrames[maxCostIndex + 1])
                {
                    Utils.Swap(ref skipFrames[maxCostIndex], ref skipFrames[maxCostIndex + 1]);
                    if (GetFrameCost(maxCostIndex + 1) < currentFrameCost)
                        continue;
                    Utils.Swap(ref skipFrames[maxCostIndex], ref skipFrames[maxCostIndex + 1]);
                }

                break;
            }
#endif

            // Build the actual envelope.
            var lastFrameIndex = -1;
            var firstFrameIndex = -1;
            var envelope = new List<byte>();
            var sum = 0;

            for (int i = 0; i < skipFrames.Length; i++)
            {
                if (skipFrames[i])
                {
                    var frameDelta = i - lastFrameIndex;
                    envelope.Add((byte)(frameDelta + (palSource ? 1 : -1)));
                    sum += frameDelta;
                    lastFrameIndex = i;
                    if (firstFrameIndex < 0)
                        firstFrameIndex = i;
                }
            }

            if (palSource)
                envelope[0]--;

            var remainingFrames = skipFrames.Length - sum;
            if (remainingFrames != 0)
                envelope.Add((byte)(remainingFrames + firstFrameIndex + 1 + (palSource ? 1 : -1)));
            envelope.Add(0x80);

            env = envelope.ToArray();
            cachedTempoEnvelopes.Value[key] = env;

            return env;
        }
    }

    public class TempoInfo
    {
        public TempoInfo(int[] groove, bool pal, int notesPerBeat)
        {
            this.bpm = FamiStudioTempoUtils.ComputeBpmForGroove(pal, groove, notesPerBeat);
            this.groove = groove;
        }

        public float bpm;
        public int[] groove;
    }

    public class GrooveIterator
    {
        int[] groove;
        int   padMode;
        int   noteLength;
        int   grooveFrameIndex;
        int   grooveArrayIndex;
        int   frameIndex;

        public GrooveIterator(int[] groove, int padMode)
        {
            this.groove  = groove;
            this.padMode = padMode;
            this.noteLength = Utils.Min(groove);

            Reset();
        }

        public void Reset()
        {
            frameIndex = 0;
            grooveFrameIndex = 0;
            grooveArrayIndex = 0;
        }

        public void Advance()
        {
            frameIndex++;

            if (++grooveFrameIndex == groove[grooveArrayIndex])
            {
                grooveFrameIndex = 0;
                if (++grooveArrayIndex == groove.Length)
                    grooveArrayIndex = 0;
            }
        }

        public int FrameIndex => frameIndex;

        public bool IsPadFrame => (groove[grooveArrayIndex] != noteLength) && 
                                  ((padMode == GroovePaddingType.Beginning && grooveFrameIndex == 0)              ||
                                   (padMode == GroovePaddingType.Middle    && grooveFrameIndex == noteLength / 2) ||
                                   (padMode == GroovePaddingType.End       && grooveFrameIndex == noteLength));
    } 
}

