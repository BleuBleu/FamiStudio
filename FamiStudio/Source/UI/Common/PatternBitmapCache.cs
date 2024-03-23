using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace FamiStudio
{
    public class PatternBitmapCache
    {
        private const int MaxPatternCacheSizeX = 64;
        private const int MaxPatternCacheSizeY = 32;

        private const int CleanupFrameInterval = 240;
        private const int StaleEntryFrameCount = 480;

        private const int   PatternCacheTextureSize    = 512;
        private const float InvPatternCacheTextureSize = 1.0f / PatternCacheTextureSize;

        class CacheEntry
        {
            public int startX;
            public int sizeX;
        }

        class CacheRow
        {
            public List<CacheEntry> usedEntries = new List<CacheEntry>();
            public List<CacheEntry> freeEntries = new List<CacheEntry>();
        }

        class CacheTexture
        {
            public Texture bmp;
            public CacheRow[] rows;
        }

        class PatternCacheData
        {
            public int patternLen;
            public int framesPerNote;
            public int textureIdx;
            public int lastUsedFrame;
            public Rectangle rect;
        }

        private int frameIndex;
        private int clampedPatternCacheSizeY;
        private int desiredPatternCacheSizeY;
        private float scaleFactorV;
        private Graphics graphics;
        private List<CacheTexture> cacheTextures = new List<CacheTexture>();
        private Dictionary<int, List<PatternCacheData>> patternCache = new Dictionary<int, List<PatternCacheData>>();

        public PatternBitmapCache(Graphics g)
        {
            graphics = g;
        }

        public void Remove(Pattern pattern)
        {
            if (patternCache.TryGetValue(pattern.Id, out var list))
            {
                foreach (var data in list)
                    Free(data.textureIdx, data.rect.X, data.rect.Y, data.rect.Width);

                patternCache.Remove(pattern.Id);
            }
        }

        private void ComputeUVs(Rectangle rect, out float u0, out float v0, out float u1, out float v1)
        {
            u0 = rect.Left   * InvPatternCacheTextureSize;
            v0 = rect.Top    * InvPatternCacheTextureSize;
            u1 = u0 + rect.Width  * InvPatternCacheTextureSize;
            v1 = v0 + rect.Height * InvPatternCacheTextureSize * scaleFactorV;
        }

        public Texture GetOrAddPattern(Pattern pattern, int patternLen, int framesPerNote, out float u0, out float v0, out float u1, out float v1)
        {
            // Look in cache first.
            if (patternCache.TryGetValue(pattern.Id, out var list))
            {
                foreach (var d in list)
                {
                    if (d.patternLen == patternLen && d.framesPerNote == framesPerNote)
                    {
                        ComputeUVs(d.rect, out u0, out v0, out u1, out v1);
                        d.lastUsedFrame = frameIndex;
                        return cacheTextures[d.textureIdx].bmp;
                    }
                }
            }

            // Rasterize pattern and add to cache.
            var noteSizeY = (int)Math.Max(Math.Ceiling(clampedPatternCacheSizeY * 0.1f), 2);
            var patternCacheSizeX = ComputePatternSizeX(patternLen, framesPerNote);
            var scaleX = patternCacheSizeX / (float)patternLen;
            var scaleY = (clampedPatternCacheSizeY - noteSizeY) / (float)clampedPatternCacheSizeY;
            var data = new int[patternCacheSizeX * clampedPatternCacheSizeY];
            var project = pattern.Song.Project;

            if (pattern.GetMinMaxNote(out var minNote, out var maxNote))
            {
                if (maxNote == minNote)
                {
                    minNote = (byte)Math.Max(minNote - 5, Note.MusicalNoteMin);
                    maxNote = (byte)Math.Min(maxNote + 5, Note.MusicalNoteMax);
                }
                else
                {
                    minNote = (byte)Math.Max(minNote - 2, Note.MusicalNoteMin);
                    maxNote = (byte)Math.Min(maxNote + 2, Note.MusicalNoteMax);
                }

                var musicalNotes = new List<Tuple<int, Note>>();

                foreach (var kv in pattern.Notes)
                {
                    var time = kv.Key;
                    if (time >= patternLen)
                        break;

                    var note = kv.Value;
                    if (note.IsMusical)
                    {
                        var quantizedTime = Math.Min(patternCacheSizeX - 1, (int)(time * scaleX));
                        if (musicalNotes.Count == 0 || musicalNotes[musicalNotes.Count - 1].Item1 != quantizedTime)
                            musicalNotes.Add(new Tuple<int, Note>(quantizedTime, note));
                    }
                }

                for (int i = 0; i < musicalNotes.Count; i++)
                {
                    var note  = musicalNotes[i].Item2;
                    var time1 = musicalNotes[i].Item1;
                    var time2 = (int)Math.Min(time1 + Math.Ceiling(note.Duration * scaleX), i < musicalNotes.Count - 1 ? musicalNotes[i + 1].Item1 : patternCacheSizeX);

                    DrawPatternBitmapNote(project, time1, time2, note, patternCacheSizeX, clampedPatternCacheSizeY, noteSizeY, minNote, maxNote, scaleY, pattern.ChannelType == ChannelType.Dpcm, data);
                }
            }

            // Update texture.
            Allocate(patternCacheSizeX, out var textureIdx, out var x, out var y);
            var texture = cacheTextures[textureIdx];
            graphics.UpdateTexture(texture.bmp, x, y, patternCacheSizeX, clampedPatternCacheSizeY, data);

            if (!patternCache.TryGetValue(pattern.Id, out list))
            {
                list = new List<PatternCacheData>();
                patternCache[pattern.Id] = list;
            }

            var cacheData = new PatternCacheData();
            cacheData.patternLen = patternLen;
            cacheData.framesPerNote = framesPerNote;
            cacheData.textureIdx = textureIdx;
            cacheData.rect = new Rectangle(x, y, patternCacheSizeX, clampedPatternCacheSizeY);
            cacheData.lastUsedFrame = frameIndex;

            list.Add(cacheData);

            ComputeUVs(cacheData.rect, out u0, out v0, out u1, out v1);

            return texture.bmp;
        }

        private void CleanupStaleEntries()
        {
            if ((frameIndex % CleanupFrameInterval) == 0)
            {
                var hasEmptyList = false;

                foreach (var list in patternCache.Values)
                {
                    for (int i = list.Count - 1; i >= 0; i--)
                    {
                        var data = list[i];
                        if ((frameIndex - data.lastUsedFrame) > StaleEntryFrameCount)
                        {
                            list.RemoveAt(i);
                            Free(data.textureIdx, data.rect.X, data.rect.Y, data.rect.Width);
                        }
                    }

                    if (list.Count == 0)
                        hasEmptyList = true;
                }

                if (hasEmptyList)
                {
                    var newPatternCache = new Dictionary<int, List<PatternCacheData>>(patternCache.Count);

                    foreach (var kv in patternCache)
                    {
                        if (kv.Value.Count > 0)
                            newPatternCache.Add(kv.Key, kv.Value);
                    }

                    patternCache = newPatternCache;
                }

                PrintCacheStats();
            }
        }

        public void Update(int patternSizeY)
        {
            frameIndex++;

            if (desiredPatternCacheSizeY != patternSizeY)
            {
                desiredPatternCacheSizeY = patternSizeY;
                clampedPatternCacheSizeY = patternSizeY;

                var factor = 1;
                while (clampedPatternCacheSizeY > MaxPatternCacheSizeY)
                {
                    clampedPatternCacheSizeY = clampedPatternCacheSizeY / 2;
                    factor *= 2;
                }

                scaleFactorV = (clampedPatternCacheSizeY * factor) / (float)desiredPatternCacheSizeY;
                Clear();
            }
            else
            {
                CleanupStaleEntries();
            }
        }

        private void PrintCacheStats()
        {
#if DEBUG
            Debug.WriteLine($"Pattern Bitmap Cache Stats: {cacheTextures.Count} textures");

            for (int i = 0; i < cacheTextures.Count; i++)
            { 
                var tex = cacheTextures[i];
                var pixelCount = 0;

                foreach (var row in tex.rows)
                {
                    foreach (var entry in row.usedEntries)
                        pixelCount += entry.sizeX * clampedPatternCacheSizeY;
                }

                var percent = pixelCount * 100 / (float)(PatternCacheTextureSize * PatternCacheTextureSize);

                Debug.WriteLine($"  Texture {i} is {percent}% full.");
            }
#endif
        }

        public void Clear()
        {
            patternCache.Clear();
            foreach (var tex in cacheTextures)
                InitCacheRows(tex);
        }

        private void DrawPatternBitmapNote(Project project, int t0, int t1, Note note, int patternSizeX, int patternSizeY, int noteSizeY, int minNote, int maxNote, float scaleY, bool dpcm, int[] data)
        {
            var y = Math.Min((int)Math.Round((note.Value - minNote) / (float)(maxNote - minNote) * scaleY * patternSizeY), patternSizeY - noteSizeY);
            var instrument = note.Instrument;

            var color = Theme.LightGreyColor1;

            if (instrument != null)
            {
                if (dpcm && Settings.DpcmColorMode == Settings.ColorModeSample)
                {
                    var mapping = instrument.GetDPCMMapping(note.Value);
                    if (mapping != null && mapping.Sample != null)
                        color = mapping.Sample.Color;
                }
                else if (instrument != null)
                {
                    color = instrument.Color;
                }
            }

            for (int j = 0; j < noteSizeY; j++)
                for (int x = t0; x < t1; x++)
                    data[(patternSizeY - 1 - (y + j)) * patternSizeX + x] = color.ToAbgr();
        }

        private int ComputePatternSizeX(int patternLen, int framesPerNote)
        {
            return Math.Min(patternLen / framesPerNote, MaxPatternCacheSizeX);
        }

        private void Allocate(int sizeX, out int textureIdx, out int x, out int y)
        {
            // Look for space in existing textures.
            for (int i = 0; i < cacheTextures.Count; i++)
            {
                if (TryAllocateFromTexture(cacheTextures[i], sizeX, out x, out y))
                {
                    textureIdx = i;
                    return;
                }
            }

            // Create new texture.
            var texture = new CacheTexture();
            texture.bmp  = graphics.CreateEmptyTexture(PatternCacheTextureSize, PatternCacheTextureSize, TextureFormat.Rgba, false);
            InitCacheRows(texture);
            textureIdx = cacheTextures.Count;
            cacheTextures.Add(texture);

            // Allocation cannot fail here.
            var allocated = TryAllocateFromTexture(texture, sizeX, out x, out y);
            Debug.Assert(allocated);
        }

        private void InitCacheRows(CacheTexture texture)
        {
            var numRows = PatternCacheTextureSize / clampedPatternCacheSizeY;
            
            texture.rows = new CacheRow[numRows];

            for (int i = 0; i < numRows; i++)
            {
                texture.rows[i] = new CacheRow();
                texture.rows[i].freeEntries.Add(new CacheEntry() { startX = 0, sizeX = PatternCacheTextureSize });
            }
        }

        private bool TryAllocateFromTexture(CacheTexture texture, int sizeX, out int x, out int y)
        {
            Debug.Assert(texture.rows.Length == PatternCacheTextureSize / clampedPatternCacheSizeY);

            for (int j = 0; j < texture.rows.Length; j++)
            {
                var row = texture.rows[j];
                if (row.freeEntries.Count > 0)
                {
                    for (int k = 0; k < row.freeEntries.Count; k++)
                    {
                        var free = row.freeEntries[k];
                        if (free.sizeX >= sizeX)
                        {
                            CacheEntry node;

                            if (free.sizeX > sizeX)
                            {
                                // Create new node.
                                node = new CacheEntry();
                                node.startX = free.startX;
                                node.sizeX = sizeX;

                                // Shrink free node.
                                free.startX += sizeX;
                                free.sizeX -= sizeX;
                            }
                            else // Perfect fit, use as is.
                            {
                                node = free;
                                row.freeEntries.RemoveAt(k);
                            }

                            x = node.startX;
                            y = j * clampedPatternCacheSizeY;

                            InsertAndMerge(row.usedEntries, node);

                            return true;
                        }
                    }
                }
            }

            x = -1;
            y = -1;

            return false;
        }

        private void InsertAndMerge(List<CacheEntry> list, CacheEntry entry)
        {
            var idx = -1;

            // Add back to the list, keep everything sorted by start X.
            if (list.Count == 0 || list[list.Count - 1].startX < entry.sizeX)
            {
                idx = list.Count;
                list.Add(entry);
            }
            else
            {
                list.Add(entry);
                list.Sort((e1, e2) => e1.startX.CompareTo(e2.startX));
            }

            // Look left for potential merge.
            if (idx > 0)
            {
                var leftEntry = list[idx - 1];
                if (leftEntry.startX + leftEntry.sizeX == entry.startX)
                {
                    list.RemoveAt(idx);
                    leftEntry.sizeX += entry.sizeX;
                    idx--;
                    entry = leftEntry;
                }
            }

            // Look right for potential merge.
            if (idx + 1 < list.Count)
            {
                var rightEntry = list[idx + 1];
                if (entry.startX + entry.sizeX == rightEntry.startX)
                {
                    list.RemoveAt(idx + 1);
                    entry.sizeX += rightEntry.sizeX;
                }
            }
        }

        private void Free(int textureIdx, int x, int y, int sx)
        {
            var rowIdx = y / clampedPatternCacheSizeY;
            var row = cacheTextures[textureIdx].rows[rowIdx];

            for (int k = 0; k < row.usedEntries.Count; k++)
            {
                var used = row.usedEntries[k];

                if (x >= used.startX && x + sx <= used.startX + used.sizeX)
                {
                    if (x == used.startX && x + sx == used.startX + used.sizeX)
                    {
                        row.usedEntries.RemoveAt(k);
                        InsertAndMerge(row.freeEntries, used);
                    }
                    else if (x == used.startX)
                    {
                        InsertAndMerge(row.freeEntries, new CacheEntry() { startX = x, sizeX = sx });
                        used.startX += sx;
                        used.sizeX  -= sx;
                    }
                    else if (x + sx == used.startX + used.sizeX)
                    {
                        used.sizeX -= sx;
                        InsertAndMerge(row.freeEntries, new CacheEntry() { startX = used.startX + used.sizeX, sizeX = sx });
                    }
                    else
                    {
                        var oldSizeX = used.sizeX;
                        used.sizeX = x - used.startX;
                        InsertAndMerge(row.freeEntries, new CacheEntry() { startX = used.startX + used.sizeX, sizeX = sx });
                        InsertAndMerge(row.usedEntries, new CacheEntry() { startX = used.startX + used.sizeX + sx, sizeX = oldSizeX - used.sizeX - sx });
                    }
                    return;
                }
            }

            Debug.Assert(false);
        }

        public void ValidateIntegrity()
        {
            for (int i = 0; i < cacheTextures.Count; i++)
            {
                var texture = cacheTextures[i];

                for (int j = 0; j < texture.rows.Length; j++)
                {
                    var row = texture.rows[j];
                    var pixels = 0;

                    for (int k = 0; k < row.freeEntries.Count; k++)
                        pixels += row.freeEntries[k].sizeX;
                    for (int k = 0; k < row.usedEntries.Count; k++)
                        pixels += row.usedEntries[k].sizeX;

                    Debug.Assert(pixels == PatternCacheTextureSize);
                }
            }
        }
    }
}