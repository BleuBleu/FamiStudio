using System;
using System.Diagnostics;
using System.Drawing;
using System.Collections.Generic;

namespace FamiStudio
{
    public class PatternBitmapCache
    {
        const int PatterCacheTextureSize = 1024;

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
            public GLBitmap bmp;
            public CacheRow[] rows;
        }

        class PatternCacheData
        {
            public int patternLen;
            public int framesPerNote;
            public int textureIdx;
            public Rectangle rect;
        }

        private int patternCacheSizeY;
        private GLGraphics graphics;
        private List<CacheTexture> cacheTextures = new List<CacheTexture>();
        private Dictionary<int, List<PatternCacheData>> patternCache = new Dictionary<int, List<PatternCacheData>>();

        public PatternBitmapCache(GLGraphics g)
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

        public GLBitmap GetOrAddPattern(Pattern pattern, int patternLen, int framesPerNote, out float u0, out float v0, out float u1, out float v1)
        {
            // Look in cache first.
            if (patternCache.TryGetValue(pattern.Id, out var list))
            {
                foreach (var d in list)
                {
                    if (d.patternLen == patternLen && d.framesPerNote == framesPerNote)
                    {
                        u0 = (d.rect.Left   + 0.5f) / PatterCacheTextureSize;
                        v0 = (d.rect.Top    + 0.5f) / PatterCacheTextureSize;
                        u1 = (d.rect.Right  + 0.5f) / PatterCacheTextureSize;
                        v1 = (d.rect.Bottom + 0.5f) / PatterCacheTextureSize;
                        return cacheTextures[d.textureIdx].bmp;
                    }
                }
            }

            // Rasterize pattern and add to cache.
            var noteSizeY = (int)Math.Max(Math.Ceiling(patternCacheSizeY * 0.1f), 2);
            var patternCacheSizeX = ComputePatternSizeX(patternLen, framesPerNote);
            var scaleX = patternCacheSizeX / (float)patternLen;
            var scaleY = (patternCacheSizeY - noteSizeY) / (float)patternCacheSizeY;
            var data = new int[patternCacheSizeX * patternCacheSizeY];
            var project = pattern.Song.Project;

            if (pattern.GetMinMaxNote(out var minNote, out var maxNote))
            {
                if (maxNote == minNote)
                {
                    minNote = (byte)(minNote - 5);
                    maxNote = (byte)(maxNote + 5);
                }
                else
                {
                    minNote = (byte)(minNote - 2);
                    maxNote = (byte)(maxNote + 2);
                }

                var musicalNotes = new List<Tuple<int, Note>>();

                foreach (var kv in pattern.Notes)
                {
                    var time = kv.Key;
                    if (time >= patternLen)
                        break;

                    var note = kv.Value;
                    if (note.IsMusical)
                        musicalNotes.Add(new Tuple<int, Note>(time, note));
                }

                for (int i = 0; i < musicalNotes.Count; i++)
                {
                    var note  = musicalNotes[i].Item2;
                    var time1 = musicalNotes[i].Item1;
                    var time2 = i < musicalNotes.Count - 1 ? musicalNotes[i + 1].Item1 : (int)ushort.MaxValue;

                    var scaledTime1 = (int)(time1 * scaleX);
                    var scaledTime2 = Math.Min((int)(time2 * scaleX), patternCacheSizeX - 1);

                    DrawPatternBitmapNote(project, scaledTime1, scaledTime2, note, patternCacheSizeX, patternCacheSizeY, noteSizeY, minNote, maxNote, scaleY, pattern.ChannelType == ChannelType.Dpcm, data);
                }
            }

            // Update texture.
            Allocate(patternCacheSizeX, out var textureIdx, out var x, out var y);
            var texture = cacheTextures[textureIdx];
            graphics.UpdateBitmap(texture.bmp, x, y, patternCacheSizeX, patternCacheSizeY, data);

            if (!patternCache.TryGetValue(pattern.Id, out list))
            {
                list = new List<PatternCacheData>();
                patternCache[pattern.Id] = list;
            }

            var cacheData = new PatternCacheData();
            cacheData.patternLen = patternLen;
            cacheData.framesPerNote = framesPerNote;
            cacheData.textureIdx = textureIdx;
            cacheData.rect = new Rectangle(x, y, patternCacheSizeX, patternCacheSizeY);

            list.Add(cacheData);

            u0 = (cacheData.rect.X + 0.5f) / PatterCacheTextureSize;
            v0 = (cacheData.rect.Y + 0.5f) / PatterCacheTextureSize;
            u1 = (cacheData.rect.Right + 0.5f)  / PatterCacheTextureSize;
            v1 = (cacheData.rect.Bottom + 0.5f) / PatterCacheTextureSize;

            return texture.bmp;
        }

        public void Update(int patternSizeY)
        {
            if (patternCacheSizeY != patternSizeY)
            {
                patternCacheSizeY = patternSizeY;
                Clear();
            }
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

            var color = ThemeBase.LightGreyFillColor1;
            if (dpcm)
            {
                var mapping = project.GetDPCMMapping(note.Value);
                if (mapping != null)
                    color = mapping.Sample.Color;
            }
            else if (instrument != null)
            {
                color = instrument.Color;
            }

            for (int j = 0; j < noteSizeY; j++)
                for (int x = t0; x < t1; x++)
                    data[(patternSizeY - 1 - (y + j)) * patternSizeX + x] = color.ToArgb();
        }

        private int ComputePatternSizeX(int patternLen, int framesPerNote)
        {
            return Math.Min(patternLen / framesPerNote, 64);
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
            texture.bmp  = graphics.CreateEmptyBitmap(PatterCacheTextureSize, PatterCacheTextureSize);
            InitCacheRows(texture);
            textureIdx = cacheTextures.Count;
            cacheTextures.Add(texture);

            // Allocation cannot fail here.
            var allocated = TryAllocateFromTexture(texture, sizeX, out x, out y);
            Debug.Assert(allocated);
        }

        private void InitCacheRows(CacheTexture texture)
        {
            var numRows = PatterCacheTextureSize / patternCacheSizeY;
            
            texture.rows = new CacheRow[numRows];

            for (int i = 0; i < numRows; i++)
            {
                texture.rows[i] = new CacheRow();
                texture.rows[i].freeEntries.Add(new CacheEntry() { startX = 0, sizeX = PatterCacheTextureSize });
            }
        }

        private bool TryAllocateFromTexture(CacheTexture texture, int sizeX, out int x, out int y)
        {
            Debug.Assert(texture.rows.Length == PatterCacheTextureSize / patternCacheSizeY);

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
                            y = j * patternCacheSizeY;

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
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    if (entry.startX < list[i].startX)
                    {
                        idx = i;
                        list.Insert(i, entry);
                        break;
                    }
                }

                Debug.Assert(idx >= 0);
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

        private void Free(int textureIdx, int x, int y, int sizeX)
        {
            var rowIdx = y / patternCacheSizeY;
            var row = cacheTextures[textureIdx].rows[rowIdx];

            for (int k = 0; k < row.usedEntries.Count; k++)
            {
                var used = row.usedEntries[k];
                if (used.startX == x)
                {
                    Debug.Assert(used.sizeX == sizeX);
                    row.usedEntries.RemoveAt(k);
                    InsertAndMerge(row.freeEntries, used);
                    return;
                }
            }

            Debug.Assert(false);
        }

        public void ValidateIntegrity()
        {

        }
    }
}