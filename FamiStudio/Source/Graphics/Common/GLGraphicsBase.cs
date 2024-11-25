using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

using static FamiStudio.Font;

namespace FamiStudio
{
    public abstract class GraphicsBase : IDisposable
    {
        protected bool builtAtlases;
        protected bool offscreen;
        protected int lineWidthBias;
        protected int dashSize = 2; // Must be power-of-two, max 64.
        protected float[] viewportScaleBias = new float[4];
        protected Rectangle screenRect;
        protected Rectangle screenRectFlip;
        protected TransformStack transform = new TransformStack();
        protected Dictionary<int, TextureAtlas> atlases = new Dictionary<int, TextureAtlas>();
        protected List<ClipRegion> clipRegions = new List<ClipRegion>();
        protected Stack<ClipRegion> clipStack = new Stack<ClipRegion>();
        protected CommandList[] layerCommandLists = new CommandList[(int)GraphicsLayer.Count];
        protected byte curDepthValue = 0x80; // -128
        protected byte maxDepthValue = 0x80; // -128
        protected int maxTextureSize;
        protected int drawFrameCounter;
        protected int ownerThreadId;

        protected struct ClipRegion
        {
            public RectangleF rect;
            public byte depthValue;
        }

        public byte DepthValue => curDepthValue;
        public int LineWidthBias => lineWidthBias;
        public int DashSize => dashSize;
        public TransformStack Transform => transform;
        public RectangleF CurrentClipRegion => clipStack.Peek().rect;
        public bool IsOffscreen => offscreen;
        public int ScreenWidth => screenRect.Width;
        public int ScreenHeight => screenRect.Height;

        public const int MaxAtlasResolution = 1024;
        public const int MaxVertexCount = 64 * 1024;
        public const int MaxIndexCount = MaxVertexCount / 4 * 6;

        // These are only used temporarily during depth pre-pass, clears, blur, etc.
        // TODO : Reduce size dramatically.
        protected float[] vtxArray = new float[MaxVertexCount * 2];
        protected float[] texArray = new float[MaxVertexCount * 3];
        protected int[]   colArray = new int[MaxVertexCount];
        protected byte[]  depArray = new byte[MaxVertexCount];
        protected short[] idxArray = new short[MaxIndexCount];

        protected static short[] quadIdxArray;

        protected List<float[]> freeVertexArrays   = new List<float[]>();
        protected List<float[]> freeTexCoordArrays = new List<float[]>();
        protected List<byte[]>  freeByteArrays     = new List<byte[]>();
        protected List<int[]>   freeColorArrays    = new List<int[]>();
        protected List<short[]> freeIndexArrays    = new List<short[]>();

        protected List<GlyphCache> glyphCaches = new List<GlyphCache>();

        public abstract int CreateTexture(int width, int height, TextureFormat format, bool filter);
        public abstract void DeleteTexture(int id);
        public abstract void UpdateTexture(int id, int x, int y, int width, int height, byte[] data);
        protected abstract int CreateTexture(SimpleBitmap bmp, bool filter);
        protected abstract void Initialize(bool clear, Color clearColor);
        protected abstract void DrawCommandList(CommandList list, bool depthTest);
        protected abstract bool DrawDepthPrepass();
        protected abstract string GetScaledFilename(string name, out bool needsScaling);
        protected abstract TextureAtlas CreateTextureAtlasFromResources(string[] names);
        protected abstract void ClearAlpha();

        protected const string AtlasPrefix = "FamiStudio.Resources.Atlas.";

        public CommandList BackgroundCommandList     => GetCommandList(GraphicsLayer.Background);
        public CommandList DefaultCommandList        => GetCommandList(GraphicsLayer.Default);
        public CommandList ForegroundCommandList     => GetCommandList(GraphicsLayer.Foreground);
        public CommandList OverlayCommandList        => GetCommandList(GraphicsLayer.Overlay); // No depth test
        public CommandList TopMostCommandList        => GetCommandList(GraphicsLayer.TopMost);
        public CommandList TopMostOverlayCommandList => GetCommandList(GraphicsLayer.TopMostOverlay); // No depth test

        protected GraphicsBase(bool offscreen)
        {
            ownerThreadId = Thread.CurrentThread.ManagedThreadId;

            // Quad index buffer.
            if (quadIdxArray == null)
            {
                quadIdxArray = new short[MaxIndexCount];

                for (int i = 0, j = 0; i < MaxVertexCount; i += 4)
                {
                    var i0 = (short)(i + 0);
                    var i1 = (short)(i + 1);
                    var i2 = (short)(i + 2);
                    var i3 = (short)(i + 3);

                    quadIdxArray[j++] = i0;
                    quadIdxArray[j++] = i1;
                    quadIdxArray[j++] = i2;
                    quadIdxArray[j++] = i0;
                    quadIdxArray[j++] = i2;
                    quadIdxArray[j++] = i3;
                }
            }

            this.offscreen = offscreen;

            // We dont need the atlases when rendering videos.
            if (!offscreen)
                BuildTextureAtlases();
        }

        public virtual void BeginDrawFrame(Rectangle rect, bool clear, Color clearColor)
        {
            Debug.Assert(drawFrameCounter++ == 0);
            Debug.Assert(transform.IsEmpty);
            Debug.Assert(clipStack.Count == 0);

            clipRegions.Clear();

            screenRect = rect;
            screenRectFlip = FlipRectangleY(rect, rect.Height);
            transform.SetIdentity();
            curDepthValue = 0x80;
            maxDepthValue = 0x80;

            viewportScaleBias[0] =  2.0f / screenRect.Width;
            viewportScaleBias[1] = -2.0f / screenRect.Height;
            viewportScaleBias[2] = -1.0f;
            viewportScaleBias[3] =  1.0f;

            Initialize(clear, clearColor);
        }

        public virtual void EndDrawFrame(bool clearAlpha = false)
        {
            Debug.Assert(--drawFrameCounter == 0);
            Debug.Assert(transform.IsEmpty);
            Debug.Assert(clipStack.Count == 0);

            var useDepth = DrawDepthPrepass();

            for (int i = 0; i < layerCommandLists.Length; i++)
            {
                if (layerCommandLists[i] != null)
                { 
                    DrawCommandList(layerCommandLists[i], i != (int)GraphicsLayer.Overlay && i != (int)GraphicsLayer.TopMostOverlay && useDepth);
                }
            }

            for (int i = 0; i < layerCommandLists.Length; i++)
            {
                if (layerCommandLists[i] != null)
                {
                    layerCommandLists[i].Release();
                    layerCommandLists[i] = null;
                }
            }

            if (clearAlpha)
            {
                ClearAlpha();
            }
        }

        protected void MakeFullScreenTriangle()
        {
            // Full screen triangle.
            colArray[0] = -1;
            colArray[1] = -1;
            colArray[2] = -1;
            depArray[0] = 0;
            depArray[1] = 0;
            depArray[2] = 0;
            idxArray[0] = 0;
            idxArray[1] = 1;
            idxArray[2] = 2;
            vtxArray[0] = -screenRect.Width;
            vtxArray[1] = 0;
            vtxArray[2] = screenRect.Width * 2;
            vtxArray[3] = 0;
            vtxArray[4] = screenRect.Width * 2;
            vtxArray[5] = screenRect.Height * 3;
        }

        protected void MakeQuad(int x, int y, int width, int height, bool proj = false, bool color = false, bool depth = false)
        {
            var vtxIdx = 0;
            var texIdx = 0;

            var x0 = x;
            var y0 = y;
            var x1 = x + width;
            var y1 = y + height;

            vtxArray[vtxIdx++] = x0;
            vtxArray[vtxIdx++] = y0;
            vtxArray[vtxIdx++] = x1;
            vtxArray[vtxIdx++] = y0;
            vtxArray[vtxIdx++] = x1;
            vtxArray[vtxIdx++] = y1;
            vtxArray[vtxIdx++] = x0;
            vtxArray[vtxIdx++] = y1;

            texArray[texIdx++] = 0.0f;
            texArray[texIdx++] = 1.0f;
            if (proj) texArray[texIdx++] = 1.0f;
            texArray[texIdx++] = 1.0f;
            texArray[texIdx++] = 1.0f;
            if (proj) texArray[texIdx++] = 1.0f;
            texArray[texIdx++] = 1.0f;
            texArray[texIdx++] = 0.0f;
            if (proj) texArray[texIdx++] = 1.0f;
            texArray[texIdx++] = 0.0f;
            texArray[texIdx++] = 0.0f;
            if (proj) texArray[texIdx++] = 1.0f;

            if (color)
            {
                var colIdx = 0;
                colArray[colIdx++] = -1;
                colArray[colIdx++] = -1;
                colArray[colIdx++] = -1;
                colArray[colIdx++] = -1;
            }

            if (depth)
            {
                var depIdx = 0;
                depArray[depIdx++] = 0;
                depArray[depIdx++] = 0;
                depArray[depIdx++] = 0;
                depArray[depIdx++] = 0;
            }
        }

        protected float[] GetBlurKernel(int width, int height, float scale, int numRings = 5)
        {
            var kernel = new List<float>();

            // Center tap is implicit.
            //kernel.Add(0);
            //kernel.Add(0);
            //kernel.Add(0);
            //kernel.Add(0);

            for (var r = 1; r < numRings; r++)
            {
                for (var i = 0; i < r * 6; i++)
                {
                    var angle = i * 2 * MathF.PI / (r * 6);
                    Utils.ToCartesian(angle, r, out var sx, out var sy);
                    kernel.Add(sx / width  * scale);
                    kernel.Add(sy / height * scale);
                }
            }

            Debug.Assert(kernel.Count % 4 == 0);

            return kernel.ToArray();
        }

        public virtual void PushClipRegion(Point p, Size s, bool clipParents = true)
        {
            PushClipRegion(p.X, p.Y, s.Width, s.Height, clipParents);
        }

        public virtual void PushClipRegion(float x, float y, float width, float height, bool clipParents = true)
        {
            var ox = x;
            var oy = y;

            transform.TransformPoint(ref ox, ref oy);

            maxDepthValue = (byte)((maxDepthValue + 1) & 0xff);
            curDepthValue = maxDepthValue;

            var clip = new ClipRegion();

            clip.rect = new RectangleF(ox, oy, width, height);
            clip.depthValue = curDepthValue;

            if (clipParents && clipStack.Count > 0)
                clip.rect = RectangleF.Intersection(clip.rect, clipStack.Peek().rect);

            clipRegions.Add(clip);
            clipStack.Push(clip);
        }

        public virtual void PopClipRegion()
        {
            clipStack.Pop();
            curDepthValue = clipStack.Count > 0 ? clipStack.Peek().depthValue : (byte)0x80;
        }

        public CommandList GetCommandList(GraphicsLayer layer = GraphicsLayer.Default)
        {
            var idx = (int)layer;

            if (layerCommandLists[idx] == null)
            {
                layerCommandLists[idx] = new CommandList(this);
            }

            return layerCommandLists[idx];
        }

        protected SimpleBitmap LoadBitmapFromResourceWithScaling(string name)
        {
            var scaledFilename = GetScaledFilename(name, out var needsScaling);
            var bmp = TgaFile.LoadFromResource(scaledFilename, true);

            // Pre-resize all images so we dont have to deal with scaling later.
            if (needsScaling)
            {
                var newWidth  = Math.Max(1, (int)(bmp.Width  * (DpiScaling.Window / 2.0f)));
                var newHeight = Math.Max(1, (int)(bmp.Height * (DpiScaling.Window / 2.0f)));

                bmp = bmp.Resize(newWidth, newHeight);
            }

            return bmp;
        }

        private void BuildTextureAtlases()
        {
            // Build atlases.
            var assembly = Assembly.GetExecutingAssembly();
            var resourceNames = assembly.GetManifestResourceNames();
            var atlasImages = new Dictionary<int, List<string>>();
            var filteredImages = new HashSet<string>();

            foreach (var res in resourceNames)
            {
                if (res.StartsWith(AtlasPrefix))
                {
                    // Remove any scaling from the name.
                    var at = res.IndexOf('@');
                    var cleanedFilename = res.Substring(AtlasPrefix.Length, at >= 0 ? at - AtlasPrefix.Length : res.Length - AtlasPrefix.Length - 4);
                    filteredImages.Add(cleanedFilename);
                }
            }

            // Keep 1 atlas per power-of-two size. 
            var minWidth = DpiScaling.ScaleForWindow(16);

            foreach (var res in filteredImages)
            {
                var scaledFilename = GetScaledFilename(AtlasPrefix + res, out var needsScaling);
                TgaFile.GetResourceImageSize(scaledFilename, out var width, out var height);

                if (needsScaling)
                {
                    width  = Math.Max(1, (int)(width  * (DpiScaling.Window / 2.0f)));
                    height = Math.Max(1, (int)(height * (DpiScaling.Window / 2.0f)));
                }

                width  = Math.Max(minWidth, width);
                height = Math.Max(minWidth, height);

                var maxSize = Math.Max(width, height);
                var maxSizePow2 = Utils.NextPowerOfTwo(maxSize);

                if (!atlasImages.TryGetValue(maxSizePow2, out var atlas))
                {
                    atlas = new List<string>();
                    atlasImages.Add(maxSizePow2, atlas);
                }

                atlas.Add(res);
            }

            // Build the textures.
            foreach (var kv in atlasImages)
            {
                var bmp = CreateTextureAtlasFromResources(kv.Value.ToArray());
                atlases.Add(kv.Key, bmp);
            }

            builtAtlases = true;
        }

        public TextureAtlasRef GetTextureAtlasRef(string name)
        {
            // Look in all atlases
            foreach (var a in atlases.Values)
            {
                var idx = a.GetElementIndex(name);
                if (idx >= 0)
                    return new TextureAtlasRef(a, idx);
            }

            Debug.Assert(false, $"Error loading texture {name}!"); // Not found!
            return null;
        }

        public TextureAtlasRef[] GetTextureAtlasRefs(string[] name, string prefix = null)
        {
            var refs = new TextureAtlasRef[name.Length];
            for (int i = 0; i < refs.Length; i++)
                refs[i] = GetTextureAtlasRef(prefix != null ? prefix + name[i] : name[i]);
            return refs;
        }

        public void SetLineBias(int bias)
        {
            lineWidthBias = bias;
        }

        protected Rectangle FlipRectangleY(Rectangle rc, int sizeY)
        {
            return new Rectangle(rc.Left, sizeY - rc.Top - rc.Height, rc.Width, rc.Height);
        }

        public float MeasureString(string text, Font font, bool mono = false)
        {
            return font.MeasureString(text, mono);
        }

        public Texture CreateEmptyTexture(int width, int height, TextureFormat format, bool filter)
        {
            return new Texture(this, CreateTexture(width, height, format, filter), width, height, true, filter);
        }

        protected T ReadFontParam<T>(string[] values, string key)
        {
            for (int i = 1; i < values.Length; i += 2)
            {
                if (values[i] == key)
                {
                    return (T)Convert.ChangeType(values[i + 1], typeof(T));
                }
            }

            Debug.Assert(false);
            return default(T);
        }

        public FontCollection CreateFontCollectionFromResource(string[] fontNames)
        {
            return new FontCollection(fontNames);
        }

        public Font CreateFont(FontCollection collection, int size)
        {
            return new Font(this, collection, size);
        }

        public int CacheGlyph(byte[] data, int w, int h, out float u0, out float v0, out float u1, out float v1)
        {
            Debug.Assert(data.Length == (w * h));

            foreach (var cache in glyphCaches)
            {
                if (cache.Allocate(data, w, h, out u0, out v0, out u1, out v1))
                {
                    return cache.TextureId;
                }
            }

            const int BaseCacheSize = 512;
            const int NumNodes = 4096;

            Debug.Assert(maxTextureSize != 0);

            var textureSize = Math.Min(maxTextureSize, Utils.NextPowerOfTwo(DpiScaling.ScaleForFont(BaseCacheSize)));
            var newCache = new GlyphCache(this, textureSize, NumNodes); 
            var allocated = newCache.Allocate(data, w, h, out u0, out v0, out u1, out v1);
            Debug.Assert(allocated);

            glyphCaches.Add(newCache);

            return newCache.TextureId;
        }

        public void ClearGlyphCache()
        {
            foreach (var cache in glyphCaches)
                cache.Dispose();
            glyphCaches.Clear();
        }

        private void ClearAtlases()
        {
            foreach (var a in atlases.Values)
                a.Dispose();
            atlases.Clear();
        }

        public virtual void Dispose()
        {
            ClearGlyphCache();
            ClearAtlases();
        }

        public float[] GetVertexArray()
        {
            if (freeVertexArrays.Count > 0)
            {
                var lastIdx = freeVertexArrays.Count - 1;
                var arr = freeVertexArrays[lastIdx];
                freeVertexArrays.RemoveAt(lastIdx);
                return arr;
            }
            else
            {
                return new float[MaxVertexCount * 2];
            }
        }

        public float[] GetTexCoordArray()
        {
            if (freeTexCoordArrays.Count > 0)
            {
                var lastIdx = freeTexCoordArrays.Count - 1;
                var arr = freeTexCoordArrays[lastIdx];
                freeTexCoordArrays.RemoveAt(lastIdx);
                return arr;
            }
            else
            {
                return new float[MaxVertexCount * 3];
            }
        }

        public byte[] GetByteArray()
        {
            if (freeByteArrays.Count > 0)
            {
                var lastIdx = freeByteArrays.Count - 1;
                var arr = freeByteArrays[lastIdx];
                freeByteArrays.RemoveAt(lastIdx);
                return arr;
            }
            else
            {
                return new byte[MaxVertexCount];
            }
        }

        public int[] GetColorArray()
        {
            if (freeColorArrays.Count > 0)
            {
                var lastIdx = freeColorArrays.Count - 1;
                var arr = freeColorArrays[lastIdx];
                freeColorArrays.RemoveAt(lastIdx);
                return arr;
            }
            else
            {
                return new int[MaxVertexCount];
            }
        }

        public short[] GetIndexArray()
        {
            if (freeIndexArrays.Count > 0)
            {
                var lastIdx = freeIndexArrays.Count - 1;
                var arr = freeIndexArrays[lastIdx];
                freeIndexArrays.RemoveAt(lastIdx);
                return arr;
            }
            else
            {
                return new short[MaxIndexCount];
            }
        }

        public void ReleaseVertexArray(float[] a)
        {
            freeVertexArrays.Add(a);
        }

        public void ReleaseTexCoordArray(float[] a)
        {
            freeTexCoordArrays.Add(a);
        }

        public void ReleaseByteArray(byte[] a)
        {
            freeByteArrays.Add(a);
        }

        public void ReleaseColorArray(int[] a)
        {
            freeColorArrays.Add(a);
        }

        public void ReleaseIndexArray(short[] a)
        {
            freeIndexArrays.Add(a);
        }

        public bool OwnedByCurrentThread()
        {
            return ownerThreadId == Thread.CurrentThread.ManagedThreadId;
        }
    };

    public enum TextureFormat
    {
        R,
        Rgb,
        Rgba,
        Depth
    };

    public enum GraphicsLayer
    {
        Background,
        Default,
        Foreground,
        Overlay, // No depth test
        TopMost,  
        TopMostOverlay, // No depth test
        Count
    };

    public class GlyphCache : IDisposable
    {
        private const string StbDll = Platform.DllStaticLib ? "__Internal" : Platform.DllPrefix + "Stb" + Platform.DllExtension;

        [DllImport(StbDll, CallingConvention = CallingConvention.StdCall)]
        extern static IntPtr StbInitPackRect(int width, int height, int numNodes);

        [DllImport(StbDll, CallingConvention = CallingConvention.StdCall)]
        extern static void StbFreePackRect(IntPtr pack);

        [DllImport(StbDll, CallingConvention = CallingConvention.StdCall)]
        extern static unsafe int StbPackRects(IntPtr pack, ref int widths, ref int heights, ref int x, ref int y, int num);

        private IntPtr pack;
        private int texture;
        private int textureSize;
        private int numFailedAllocations;
        private GraphicsBase graphics;

        public int TextureId => texture;

        public GlyphCache(GraphicsBase g, int size, int numSlots)
        {
            graphics = g;
            texture = g.CreateTexture(size, size, TextureFormat.R, false);
            textureSize = size;
            pack = StbInitPackRect(size, size, numSlots);
        }

        public bool Allocate(byte[] data, int w, int h, out float u0, out float v0, out float u1, out float v1)
        {
            Debug.Assert(data.Length == (w * h));

            u0 = 0.0f;
            v0 = 0.0f;
            u1 = 0.0f;
            v1 = 0.0f;

            if (numFailedAllocations > 10)
            {
                return false;
            }

            var x = 0;
            var y = 0;
            var allocated = StbPackRects(pack, ref w, ref h, ref x, ref y, 1);

            if (allocated == 0)
            {
                numFailedAllocations++;
                return false; 
            }

            graphics.UpdateTexture(texture, x, y, w, h, data);

            u0 = ((x + 0) / (float)textureSize);
            v0 = ((y + 0) / (float)textureSize);
            u1 = ((x + w) / (float)textureSize);
            v1 = ((y + h) / (float)textureSize);

            return true;
        }

        public void Dispose()
        {
            graphics.DeleteTexture(texture);
            texture = 0;
            StbFreePackRect(pack);
        }
    }

    public class FontCollection : IDisposable
    {
        private const string StbDll = Platform.DllStaticLib ? "__Internal" : Platform.DllPrefix + "Stb" + Platform.DllExtension;

        [DllImport(StbDll, CallingConvention = CallingConvention.StdCall)]
        extern static int StbGetNumberOfFonts(IntPtr data);

        [DllImport(StbDll, CallingConvention = CallingConvention.StdCall)]
        extern static int StbGetFontOffsetForIndex(IntPtr data, int index);

        [DllImport(StbDll, CallingConvention = CallingConvention.StdCall)]
        extern static IntPtr StbInitFont(IntPtr data, int offset);

        [DllImport(StbDll, CallingConvention = CallingConvention.StdCall)]
        extern static void StbFreeFont(IntPtr font);

        [DllImport(StbDll, CallingConvention = CallingConvention.StdCall)]
        extern static float StbScaleForPixelHeight(IntPtr info, float pixels);

        [DllImport(StbDll, CallingConvention = CallingConvention.StdCall)]
        extern static float StbScaleForMappingEmToPixels(IntPtr info, float pixels);

        [DllImport(StbDll, CallingConvention = CallingConvention.StdCall)]
        extern static void StbGetGlyphBitmapBox(IntPtr info, int glyph, float scale, out int x0, out int y0, out int x1, out int y1);

        [DllImport(StbDll, CallingConvention = CallingConvention.StdCall)]
        extern static void StbMakeGlyphBitmap(IntPtr info, IntPtr output, int width, int height, int stride, int glyph, float scale);

        [DllImport(StbDll, CallingConvention = CallingConvention.StdCall)]
        extern static void StbMakeGlyphBitmapSubpixel(IntPtr info, IntPtr output, int width, int height, int stride, int glyph, float scale, float subx, float suby);

        [DllImport(StbDll, CallingConvention = CallingConvention.StdCall)]
        extern static void StbGetGlyphHMetrics(IntPtr info, int glyph, out int advanceWidth, out int leftSideBearing);

        [DllImport(StbDll, CallingConvention = CallingConvention.StdCall)]
        extern static int StbGetGlyphKernAdvance(IntPtr info, int ch1, int ch2);

        [DllImport(StbDll, CallingConvention = CallingConvention.StdCall)]
        extern static void StbGetFontVMetrics(IntPtr info, out int ascent, out int descent, out int lineGap);

        [DllImport(StbDll, CallingConvention = CallingConvention.StdCall)]
        extern static int StbGetFontVMetricsOS2(IntPtr info, out int typoAscent, out int typoDescent, out int typoLineGap, out int winAscent, out int winDescent);

        [DllImport(StbDll, CallingConvention = CallingConvention.StdCall)]
        extern static void StbGetFontBoundingBox(IntPtr info, out int x0, out int y0, out int x1, out int y1);

        [DllImport(StbDll, CallingConvention = CallingConvention.StdCall)]
        extern static int StbFindGlyphIndex(IntPtr info, int codepoint);

        class FontData
        {
            public string name;
            public GCHandle handle;
            public IntPtr info;
        };

        private FontData[] fontData;
        private Dictionary<int, float> kerningPairs = new Dictionary<int, float>(); // Key is a pair of chars.
        private static Dictionary<char, (byte, ushort)> glyphDictionary;

        public FontCollection(string[] fontNames)
        {
            ConditionalLoadGlyphDictionary();

            fontData = new FontData[fontNames.Length];
            for (var i = 0; i < fontNames.Length; i++)
            {
                fontData[i] = new FontData() { name = fontNames[i] };
            }
        }

        private void ConditionalLoadGlyphDictionary()
        {
            if (glyphDictionary == null)
            {
                var stream = typeof(Font).Assembly.GetManifestResourceStream($"FamiStudio.Resources.Fonts.GlyphDictionary.bin");
                var buffer = new byte[stream.Length];
                stream.Read(buffer, 0, buffer.Length);
                stream.Dispose();

                glyphDictionary = new Dictionary<char, (byte, ushort)>(buffer.Length / 5);

                for (int i = 0; i < buffer.Length; i += 5)
                {
                    var c = BitConverter.ToChar(buffer, i + 0);
                    var f = buffer[i + 2];
                    var g = BitConverter.ToUInt16(buffer, i + 3);

                    glyphDictionary.Add(c, (f, g));
                }
            }
        }

        private IntPtr GetFontData(int idx)
        {
            if (fontData[idx].info == IntPtr.Zero)
            {
                var name = fontData[idx].name;
                var stream = typeof(Font).Assembly.GetManifestResourceStream($"FamiStudio.Resources.Fonts.{name}.ttf");
                var buffer = new byte[stream.Length];
                stream.Read(buffer, 0, buffer.Length);
                stream.Dispose();

                var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);

                IntPtr pttf = handle.AddrOfPinnedObject();
                var offset = StbGetFontOffsetForIndex(pttf, 0);

                fontData[idx].handle = handle;
                fontData[idx].info = StbInitFont(pttf, offset);
            }

            return fontData[idx].info;
        }

        public void ReleaseFontData(int startIndex = 0)
        {
            for (var i = startIndex; i < fontData.Length; i++)
            {
                var data = fontData[i];

                if (data.info != IntPtr.Zero)
                {
                    StbFreeFont(data.info);

                    data.handle.Free();
                    data.info = IntPtr.Zero;
                }
            }
        }

        public bool EnsureCharValid(ref char c)
        {
            if (!glyphDictionary.ContainsKey(c))
            {
                c = '?';
                Debug.Assert(glyphDictionary.ContainsKey(c));
                return false;
            }

            return true;
        }

        public float GetFontScale(int size)
        {
            return StbScaleForMappingEmToPixels(GetFontData(0), size);
        }

        private int GetGlyphIndex(char c, out int fontIndex)
        {
            if (glyphDictionary.TryGetValue(c, out var pair))
            {
                fontIndex = pair.Item1;
                return pair.Item2;
            }

            fontIndex = -1;
            return -1;
        }

        public void GetCharacterVerticalMetrics(char c, float scale, out int baseValue, out int lineHeight)
        {
            if (GetGlyphIndex(c, out var fontIndex) >= 0)
            {
                StbGetFontVMetrics(GetFontData(fontIndex), out var ascent, out var descent, out var lineGap);

                baseValue  = (int)(ascent * scale);
                lineHeight = (int)((ascent - descent + lineGap) * scale);
            }
            else
            {
                Debug.Assert(false);
                baseValue = -1;
                lineHeight = -1;
            }
        }

        public void GetCharacterMetrics(char c, float scale, out float xadvance, out int x0, out int y0, out int x1, out int y1)
        {
            var glyphIndex = GetGlyphIndex(c, out var fontIndex);
            var fontData = GetFontData(fontIndex);

            StbGetGlyphHMetrics(fontData, glyphIndex, out var advance, out _);
            xadvance = advance * scale;
            StbGetGlyphBitmapBox(fontData, glyphIndex, scale, out x0, out y0, out x1, out y1);
        }

        public unsafe byte[] RasterizeCharacter(char c, float scale, int width = -1, int height = -1)
        {
            Debug.Assert(EnsureCharValid(ref c));

            var glyphIndex = GetGlyphIndex(c, out var fontIndex);
            var fontData = GetFontData(fontIndex);

            if (width < 0 || height < 0)
            {
                StbGetGlyphBitmapBox(fontData, glyphIndex, scale, out var x0, out var y0, out var x1, out var y1);
                width  = x1 - x0;
                height = y1 - y0;
            }

            if (width == 0 || height == 0)
            {
                return null;
            }

            var glyphImage = new byte[width * height];

            fixed (byte* p = &glyphImage[0])
            {
                StbMakeGlyphBitmap(fontData, (IntPtr)p, width, height, width, glyphIndex, scale);
            }

            return glyphImage;
        }

        public float GetKerning(char c0, char c1, float scale)
        {
            EnsureCharValid(ref c0);
            EnsureCharValid(ref c1);

            var key = c0 | (c1 << 16);

            if (kerningPairs.TryGetValue(key, out float amount))
            {
                return amount * scale;
            }
            else
            {
                GetGlyphIndex(c0, out var f0);
                GetGlyphIndex(c1, out var f1);

                // Get the max of f0/f1. The logic here is that a Chinese font includes
                // the latin characters, but not the other way around. 
                var kern = StbGetGlyphKernAdvance(GetFontData(Math.Max(f0, f1)), c0, c1);

                kerningPairs.Add(key, kern);
                return kern * scale;
            }
        }

        public void Dispose()
        {
            ReleaseFontData(0);
        }

#if DEBUG
        public static unsafe void DumpGlyphDictionary(string[] fontList)
        {
            var dict = new SortedDictionary<int, (int, int)>();

            for (var f = 0; f < fontList.Length; f++)
            {
                var name = fontList[f];
                var stream = typeof(Font).Assembly.GetManifestResourceStream($"FamiStudio.Resources.Fonts.{name}.ttf");
                var buffer = new byte[stream.Length];
                stream.Read(buffer, 0, buffer.Length);

                fixed (byte* pttf = buffer)
                {
                    var data = new IntPtr(pttf);
                    var offset = StbGetFontOffsetForIndex(data, 0);
                    var font = StbInitFont(data, offset);

                    for (int c = char.MinValue; c <= char.MaxValue; c++)
                    {
                        if (!dict.ContainsKey(c))
                        {
                            var i = StbFindGlyphIndex(font, c);
                            if (i != 0)
                            {
                                Debug.Assert(i <= ushort.MaxValue);
                                dict.Add(c, (f, i));
                            }
                        }
                    }
                }
            }

            var bytes = new List<byte>(dict.Count * 5);

            foreach (var kv in dict)
            {
                bytes.AddRange(BitConverter.GetBytes((char)kv.Key));
                bytes.Add((byte)kv.Value.Item1);
                bytes.AddRange(BitConverter.GetBytes((ushort)kv.Value.Item2));
            }

            File.WriteAllBytes("C:\\Temp\\GlyphDictionary.bin", bytes.ToArray());
        }
#endif
    }

    public class Font
    {
        public class CharInfo
        {
            public int width;
            public int height;
            public int xoffset;
            public int yoffset;
            public float xadvance;
            public int texture;
            public float u0;
            public float v0;
            public float u1;
            public float v1;
            public bool rasterized;
        }

        private Dictionary<char, CharInfo> glyphInfos = new Dictionary<char, CharInfo>(); 

        private GraphicsBase graphics;
        private FontCollection fontCollection;
        private float scale;
        private int size;
        private int baseValue;
        private int lineHeight;

        // HACK : We seem to have slight font calculation errors. Add a param until I debug this.
        private int globalOffsetY = -1;

        public int Size => size;
        public int OffsetY => size - baseValue; 
        public int LineHeight => lineHeight;

        public Font(GraphicsBase g, FontCollection font, int sz)
        {
            graphics = g;
            fontCollection = font;
            size = sz;
            scale = fontCollection.GetFontScale(sz);

            // Everything will be based off the font #0 (latin characters). Somehow the other
            // fonts seem to look great even though they have different ascent/decent values.
            fontCollection.GetCharacterVerticalMetrics('0', scale, out baseValue, out lineHeight);
        }

    #if DEBUG
        static void DumpGlyph(byte[] glyph, int w, int h)
        {
            var lines = new List<string>();

            lines.Add("P2");
            lines.Add($"{w} {h}");
            lines.Add("255");

            for (int y = 0; y < h; y++)
            {
                lines.Add(string.Join(' ', glyph.AsSpan(w * y, w).ToArray()));
            }

            File.WriteAllLines("C:\\Dump\\glyph.pgm", lines);
        }
    #endif

        public void ClearCachedData()
        {
            glyphInfos.Clear();
        }

        public float GetKerning(char c0, char c1)
        {
            return fontCollection.GetKerning(c0, c1, scale);
        }

        public CharInfo GetCharInfo(char c)
        {
            fontCollection.EnsureCharValid(ref c);

            if (glyphInfos.TryGetValue(c, out CharInfo glyphInfo))
            {
                if (!glyphInfo.rasterized && graphics.OwnedByCurrentThread())
                    RasterizeAndCacheCharacter(c, glyphInfo);

                return glyphInfo;
            }
            else
            {
                glyphInfo = new CharInfo();

                fontCollection.GetCharacterMetrics(
                    c, scale,
                    out glyphInfo.xadvance,
                    out var x0, 
                    out var y0,
                    out var x1, 
                    out var y1);

                glyphInfo.width   = x1 - x0;
                glyphInfo.height  = y1 - y0;
                glyphInfo.xoffset = x0;
                glyphInfo.yoffset = baseValue + y0 + globalOffsetY;

                if (graphics.OwnedByCurrentThread())
                    RasterizeAndCacheCharacter(c, glyphInfo);

                glyphInfos.Add(c, glyphInfo);

                return glyphInfo;
            }
        }

        public void RasterizeAndCacheCharacter(char c, CharInfo glyphInfo)
        {
            Debug.Assert(fontCollection.EnsureCharValid(ref c));
            Debug.Assert(!glyphInfo.rasterized);

           var glyphImage = fontCollection.RasterizeCharacter(c, scale, glyphInfo.width, glyphInfo.height);

            if (glyphImage != null)
            {
                glyphInfo.texture =
                    graphics.CacheGlyph(
                        glyphImage,
                        glyphInfo.width,
                        glyphInfo.height,
                        out glyphInfo.u0,
                        out glyphInfo.v0,
                        out glyphInfo.u1,
                        out glyphInfo.v1);
            }

            glyphInfo.rasterized = true;
        }

        public bool TruncateString(ref string text, int maxSizeX)
        {
            int x = 0;

            for (int i = 0; i < text.Length; i++)
            {
                var c0 = text[i];
                var info = GetCharInfo(c0);

                int x0 = x + info.xoffset;
                int x1 = x0 + info.width;

                if (x1 >= maxSizeX)
                {
                    // Dont end on whitespace, looks silly.
                    while (i > 0 && char.IsWhiteSpace(text[i - 1]))
                        i--;
                    text = text.Substring(0, i);
                    return true;
                }

                var advance = info.xadvance;
                if (i != text.Length - 1)
                {
                    char c1 = text[i + 1];
                    advance += GetKerning(c0, c1);
                }
                x += (int)advance;
            }

            return false;
        }

        public int GetNumCharactersForSize(string text, int sizeX, bool canRoundUp = false)
        {
            var x = 0;
            var maxX = 0;

            for (int i = 0; i < text.Length; i++)
            {
                var c0 = text[i];
                var info = GetCharInfo(c0);

                int x0 = x + info.xoffset;
                int x1 = x0 + info.width;

                maxX = Math.Max(maxX, x1);

                if (maxX > sizeX)
                {
                    if (canRoundUp && sizeX >= (x1 + x0) / 2)
                        return i + 1;
                    else
                        return i;
                }

                var advance = info.xadvance;
                if (i != text.Length - 1)
                {
                    char c1 = text[i + 1];
                    advance += GetKerning(c0, c1);
                }
                x += (int)advance;
            }

            return text.Length;
        }

        public string SplitLongString(string text, int maxWidth, bool chinese, out int numLines)
        {
            var input = text;
            var output = "";
            
            numLines = 0;

            while (true)
            {
                var numCharsWeCanFit = GetNumCharactersForSize(input, maxWidth);
                var n = numCharsWeCanFit;
                var done = n == input.Length;
                var newLineIndex = input.IndexOf('\n');
                var newLine = false;

                if (newLineIndex >= 0 && newLineIndex < numCharsWeCanFit)
                {
                    n = newLineIndex;
                    done = n == input.Length;
                    newLine = true;
                }
                else
                {
                    if (!done)
                    {
                        // This bizarre code was added by the chinese translators. Not touching it.
                        if (chinese)
                        {
                            var minimumCharsPerLine = Math.Max((int)(numCharsWeCanFit * 0.62), numCharsWeCanFit - 20);
                            while (!char.IsWhiteSpace(input[n]) && input[n] != '“' && char.GetUnicodeCategory(input[n]) != UnicodeCategory.OpenPunctuation)
                            {
                                n--;
                                // No whitespace or punctuation found, let's chop in the middle of a word.
                                if (n <= minimumCharsPerLine)
                                {
                                    n = numCharsWeCanFit;
                                    if (char.IsPunctuation(input[n]))
                                        n--;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            while (n >= 0 && !char.IsWhiteSpace(input[n]))
                            {
                                n--;
                            }

                            // No whitespace found, let's chop in the middle of a word.
                            if (n < 0)
                            {
                                n = numCharsWeCanFit;
                            }
                        }
                    }
                }

                output += input.Substring(0, n);
                output += "\n";
                numLines++;

                if (!done)
                {
                    // After an intentional new line (one that had a \n right in the string), preserve
                    // any white space since it may be used for indentation.
                    if (newLine)
                    {
                        n++;
                    }
                    else
                    {
                        while (char.IsWhiteSpace(input[n]))
                            n++;
                    }
                }

                input = input.Substring(n);

                if (done)
                {
                    break;
                }
            }

            return output;
        }

        public static bool IsMonospaceChar(char c)
        {
            var cat = char.GetUnicodeCategory(c);

            return
                (c == '$') ||
                (c == '-') ||
                (c == '+') ||
                (c == '#') ||
                (c == '(') ||
                (c == ')') ||
                (c == '.') ||
                (c == ' ') ||
                (c >= '0' && c <= '9') ||
                (cat == System.Globalization.UnicodeCategory.LowercaseLetter) || // Chinese/korean/japanese characters return "OtherLetter"
                (cat == System.Globalization.UnicodeCategory.UppercaseLetter);
        }

        public int MeasureString(string text, bool mono)
        {
            int x = 0;

            for (int i = 0; i < text.Length; i++)
            {
                var c0 = text[i];
                var isMonoChar = mono && IsMonospaceChar(c0);
                var info = GetCharInfo(isMonoChar ? '0' :c0);

                var advance = info.xadvance;
                if (i != text.Length - 1 && !isMonoChar)
                {
                    char c1 = text[i + 1];
                    advance += GetKerning(c0, c1);
                }
                x += (int)advance;
            }

            return x;
        }
    }

    public class Texture : IDisposable
    {
        protected int id;
        protected Size size;
        protected bool dispose = true;
        protected bool filter = false;
        protected bool atlas = false;
        protected GraphicsBase graphics;

        public int Id => id;
        public Size Size => size;
        public bool Filtering => filter;
        public bool IsAtlas => atlas;

        public Texture(GraphicsBase g, int id, int width, int height, bool disp = true, bool filter = false)
        {
            this.graphics = g;
            this.id = id;
            this.size = new Size(width, height);
            this.dispose = disp;
            this.filter = filter;
        }

        public void Dispose()
        {
            if (dispose)
                graphics.DeleteTexture(id);
            id = -1;
        }

        public override int GetHashCode()
        {
            return id;
        }
    }

    public class TextureAtlas : Texture
    {
        private string[] elementNames;
        private Rectangle[] elementRects;

        public Size GetElementSize(int index) => elementRects[index].Size;

        public TextureAtlas(GraphicsBase g, int id, int atlasSizeX, int atlasSizeY, string[] names, Rectangle[] rects, bool filter = false) :
            base(g, id, atlasSizeX, atlasSizeY, true, filter)
        {
            elementNames = names;
            elementRects = rects;
            atlas = true;
        }

        public int GetElementIndex(string name)
        {
            // By the way we build the atlases, elements are sorted by name
            return Array.BinarySearch(elementNames, name);
        }

        public void GetElementUVs(int elementIndex, out float u0, out float v0, out float u1, out float v1)
        {
            var rect = elementRects[elementIndex];

            u0 = rect.Left   / (float)size.Width;
            u1 = rect.Right  / (float)size.Width;
            v0 = rect.Top    / (float)size.Height;
            v1 = rect.Bottom / (float)size.Height;
        }
    }

    public class TextureAtlasRef
    {
        private TextureAtlas atlas;
        private int index;

        public TextureAtlas Atlas => atlas;
        public int ElementIndex => index;
        public Size ElementSize => atlas.GetElementSize(index);

        public TextureAtlasRef(TextureAtlas a, int idx)
        {
            atlas = a;
            index = idx;
        }

        public void GetElementUVs(out float u0, out float v0, out float u1, out float v1)
        {
            atlas.GetElementUVs(index, out u0, out v0, out u1, out v1);
        }
    }

    public struct Transform
    {
        public float ScaleX;
        public float ScaleY;
        public float TranslationX;
        public float TranslationY;

        public bool HasScaling => ScaleX != 1.0f || ScaleY != 1.0f;

        public Transform(float sx, float sy, float tx, float ty)
        {
            ScaleX = sx;
            ScaleY = sy;
            TranslationX = tx;
            TranslationY = ty;
        }
    }

    public class TransformStack
    {
        private Transform transform = new Transform(1, 1, 0, 0); // xy = scale, zw = translation
        private Stack<Transform> transformStack = new Stack<Transform>();

        public bool HasScaling => transform.HasScaling;
        public bool IsEmpty => transformStack.Count == 0;

        public void SetIdentity()
        {
            transform.ScaleX = 1;
            transform.ScaleY = 1;
            transform.TranslationX = 0;
            transform.TranslationY = 0;
        }

        public void PushTranslation(float x, float y)
        {
            transformStack.Push(transform);
            transform.TranslationX += x;
            transform.TranslationY += y;
        }

        public void PushTransform(float tx, float ty, float sx, float sy)
        {
            transformStack.Push(transform);

            transform.ScaleX *= sx;
            transform.ScaleY *= sy;
            transform.TranslationX += tx;
            transform.TranslationY += ty;
        }

        public void PopTransform()
        {
            transform = transformStack.Pop();
        }

        public void TransformPoint(ref float x, ref float y)
        {
            x = x * transform.ScaleX + transform.TranslationX;
            y = y * transform.ScaleY + transform.TranslationY;
        }

        public void ReverseTransformPoint(ref float x, ref float y)
        {
            x = (x - transform.TranslationX) / transform.ScaleX;
            y = (y - transform.TranslationY) / transform.ScaleY;
        }

        public void ScaleSize(ref float width, ref float height)
        {
            width  *= transform.ScaleX;
            height *= transform.ScaleY;
        }

        public void GetOrigin(out float x, out float y)
        {
            x = 0;
            y = 0;
            TransformPoint(ref x, ref y);
        }
    }

    [Flags]
    public enum TextFlags
    {
        None = 0,

        HorizontalAlignMask = 0x3,
        VerticalAlignMask   = 0xc,

        Left   = 0 << 0,
        Center = 1 << 0,
        Right  = 2 << 0,

        Top    = 0 << 2,
        Middle = 1 << 2,
        Bottom = 2 << 2,

        TopLeft      = Top    | Left,
        TopCenter    = Top    | Center,
        TopRight     = Top    | Right,
        MiddleLeft   = Middle | Left,
        MiddleCenter = Middle | Center,
        MiddleRight  = Middle | Right,
        BottomLeft   = Bottom | Left,
        BottomCenter = Bottom | Center,
        BottomRight  = Bottom | Right,

        Clip       = 1 << 7,
        Ellipsis   = 1 << 8,
        Monospace  = 1 << 9,
        DropShadow = 1 << 10
    }

    [Flags]
    public enum TextureFlags
    {
        Default     = 0,
        Rotated90   = 1 << 0,
        Perspective = 1 << 1,
    }

    // This is common to both OGL, it only does data packing, no GL calls.
    public class CommandList
    {
        private class PolyBatch
        {
            public float[] vtxArray;
            public int[]   colArray;
            public byte[]  dshArray;
            public short[] idxArray;
            public byte[]  depArray;

            public int vtxIdx = 0;
            public int colIdx = 0;
            public int dshIdx = 0;
            public int idxIdx = 0;
            public int depIdx = 0;
        };

        private class LineBatch
        {
            public float[] vtxArray;
            public byte[]  dshArray;
            public int[]   colArray;
            public byte[]  depArray;

            public int vtxIdx = 0;
            public int dshIdx = 0;
            public int colIdx = 0;
            public int depIdx = 0;
        };
        
        private class LineSmoothBatch
        {
            public float[] vtxArray;
            public byte[]  dstArray;
            public int[]   colArray;
            public short[] idxArray;
            public byte[]  depArray;

            public int vtxIdx = 0;
            public int dstIdx = 0;
            public int colIdx = 0;
            public int idxIdx = 0;
            public int depIdx = 0;
        };

        private class TextInstance
        {
            public RectangleF layoutRect;
            public RectangleF clipRect;
            public TextFlags flags;
            public Font font;
            public string text;
            public Color color;
            public byte depth;
        };

        private class TextureInstance
        {
            public float x;
            public float y;
            public float sx;
            public float sy;
            public float u0;
            public float v0;
            public float u1;
            public float v1;
            public Color tint;
            public TextureFlags flags;
            public byte depth;
        }

        public class PolyDrawData
        {
            public float[] vtxArray;
            public int[]   colArray;
            public byte[]  dshArray;
            public short[] idxArray;
            public byte[]  depArray;

            public int vtxArraySize;
            public int colArraySize;
            public int dshArraySize;
            public int idxArraySize;
            public int depArraySize;

            public bool smooth;
            public int numIndices;
        };

        public class LineDrawData
        {
            public float[] vtxArray;
            public byte[]  dshArray;
            public int[]   colArray;
            public byte[]  depArray;

            public int vtxArraySize;
            public int dshArraySize;
            public int colArraySize;
            public int depArraySize;

            public int numVertices;
            public bool smooth;
            public float lineWidth;
        };

        public class LineSmoothDrawData
        {
            public float[] vtxArray;
            public byte[]  dstArray;
            public int[]   colArray;
            public short[] idxArray;
            public byte[]  depArray;

            public int vtxArraySize;
            public int dstArraySize;
            public int colArraySize;
            public int idxArraySize;
            public int depArraySize;

            public int numIndices;
        };
        
        public class TextDrawData
        {
            public float[] vtxArray;
            public float[] texArray;
            public int[]   colArray;
            public byte[]  depArray;

            public int vtxArraySize;
            public int texArraySize;
            public int colArraySize;
            public int depArraySize;

            public List<DrawData> drawCalls = new List<DrawData>();

            public void Release(GraphicsBase g)
            {
                g.ReleaseVertexArray(vtxArray);
                g.ReleaseTexCoordArray(texArray);
                g.ReleaseByteArray(depArray);
                g.ReleaseColorArray(colArray);
            }
        }

        public class TextureDrawData
        {
            public float[] vtxArray;
            public float[] texArray;
            public int[]   colArray;
            public byte[]  depArray;

            public int vtxArraySize;
            public int texArraySize;
            public int colArraySize;
            public int depArraySize;

            public List<DrawData> drawCalls = new List<DrawData>();

            public void Release(GraphicsBase g)
            {
                g.ReleaseVertexArray(vtxArray);
                g.ReleaseTexCoordArray(texArray);
                g.ReleaseByteArray(depArray);
                g.ReleaseColorArray(colArray);
            }
        }

        public class DrawData
        {
            public int textureId;
            public int start;
            public int count;
        };

        private List<LineBatch> lineBatches;
        private List<PolyBatch> polyBatches;
        private List<LineSmoothBatch> lineSmoothBatches;
        private List<TextInstance> texts;
        private Dictionary<Texture, List<TextureInstance>> textures = new Dictionary<Texture, List<TextureInstance>>();

        private GraphicsBase graphics;
        private TransformStack xform;

        public TransformStack Transform => xform;
        public GraphicsBase Graphics => graphics;

        public bool HasAnyPolygons       => polyBatches != null;
        public bool HasAnyLines          => lineBatches != null;
        public bool HasAnySmoothLines    => lineSmoothBatches != null;
        public bool HasAnyTexts          => texts != null;
        public bool HasAnyTextures       => textures.Count > 0;
        public bool HasAnything          => HasAnyPolygons || HasAnyLines || HasAnySmoothLines || HasAnyTexts || HasAnyTextures;

        public CommandList(GraphicsBase g)
        {
            graphics = g;
            xform = g.Transform;
        }

        public void PushTranslation(float x, float y)
        {
            xform.PushTranslation(x, y);
        }

        public void PushTransform(float tx, float ty, float sx, float sy)
        {
            xform.PushTransform(tx, ty, sx, sy);
        }

        public void PopTransform()
        {
            xform.PopTransform();
        }

        public void PushClipRegion(float x, float y, float width, float height, bool clipParents = true)
        {
            graphics.PushClipRegion(x, y, width, height, clipParents);
        }

        public void PopClipRegion()
        {
            graphics.PopClipRegion();
        }

        public void Release()
        {
            if (polyBatches != null)
            {
                foreach (var batch in polyBatches)
                { 
                    graphics.ReleaseVertexArray(batch.vtxArray);
                    graphics.ReleaseColorArray(batch.colArray);
                    graphics.ReleaseByteArray(batch.dshArray);
                    graphics.ReleaseIndexArray(batch.idxArray);
                    graphics.ReleaseByteArray(batch.depArray);
                }
            }

            if (lineBatches != null)
            {
                foreach (var batch in lineBatches)
                {
                    graphics.ReleaseVertexArray(batch.vtxArray);
                    graphics.ReleaseByteArray(batch.dshArray);
                    graphics.ReleaseColorArray(batch.colArray);
                    graphics.ReleaseByteArray(batch.depArray);
                }
            }

            if (lineSmoothBatches != null)
            {
                foreach (var batch in lineSmoothBatches)
                {
                    graphics.ReleaseVertexArray(batch.vtxArray);
                    graphics.ReleaseByteArray(batch.dstArray);
                    graphics.ReleaseColorArray(batch.colArray);
                    graphics.ReleaseIndexArray(batch.idxArray);
                    graphics.ReleaseByteArray(batch.depArray);
                }
            }

            polyBatches = null;
            lineBatches = null;
            lineSmoothBatches = null;
            textures.Clear();
            textures = null;
            texts = null;
        }

        private byte EncodeDashPattern(float x0, float x1, float y0, float y1)
        {
            // This unorm value will be divided by 4.0 in the shader.
            // Integer part: Offset to apply to glFragCoord.
            // Fractional part:
            //   0.00 : No dash
            //   0.50 : Horizontal dash
            //   0.25 : Vertical Dash.

            return x0 == x1 ?
                (byte)(((int)y0 & (graphics.DashSize * 2 - 1)) * 4 + 1) :
                (byte)(((int)x0 & (graphics.DashSize * 2 - 1)) * 4 + 2);
        }

        private PolyBatch GetPolygonBatch(int numVtxNeeded, int numIdxNeeded)
        {
            if (polyBatches == null)
            {
                polyBatches = new List<PolyBatch>();
            }

            var batch = polyBatches.Count > 0 ? polyBatches[polyBatches.Count - 1] : null;

            if (batch == null ||
                batch.depIdx + numVtxNeeded >= batch.depArray.Length ||
                batch.idxIdx + numIdxNeeded >= batch.idxArray.Length)
            {
                batch = new PolyBatch();
                batch.vtxArray = graphics.GetVertexArray();
                batch.colArray = graphics.GetColorArray();
                batch.dshArray = graphics.GetByteArray();
                batch.idxArray = graphics.GetIndexArray();
                batch.depArray = graphics.GetByteArray();
                polyBatches.Add(batch);
            }

            return batch;
        }

        private LineBatch GetLineBatch(int numVtxNeeded)
        {
            if (lineBatches == null)
            {
                lineBatches = new List<LineBatch>();
            }

            var batch = lineBatches.Count > 0 ? lineBatches[lineBatches.Count - 1] : null;

            if (batch == null || batch.depIdx + numVtxNeeded >= batch.depArray.Length)
            {
                batch = new LineBatch();
                batch.vtxArray = graphics.GetVertexArray();
                batch.dshArray = graphics.GetByteArray();
                batch.colArray = graphics.GetColorArray();
                batch.depArray = graphics.GetByteArray();
                lineBatches.Add(batch);
            }

            return batch;

        }

        private void DrawLineInternal(float x0, float y0, float x1, float y1, Color color, bool dash)
        {
            var batch = GetLineBatch(4);
            var depth = graphics.DepthValue;
            var dashPattern = dash ? EncodeDashPattern(x0, x1, y0, y1) : (byte)0;

            //if (dashPattern != 0)
            //    Debug.WriteLine(y0.ToString());

            batch.vtxArray[batch.vtxIdx++] = x0;
            batch.vtxArray[batch.vtxIdx++] = y0;
            batch.vtxArray[batch.vtxIdx++] = x1;
            batch.vtxArray[batch.vtxIdx++] = y1;

            batch.dshArray[batch.dshIdx++] = dashPattern;
            batch.dshArray[batch.dshIdx++] = dashPattern;

            batch.colArray[batch.colIdx++] = color.ToAbgr();
            batch.colArray[batch.colIdx++] = color.ToAbgr();

            batch.depArray[batch.depIdx++] = depth;
            batch.depArray[batch.depIdx++] = depth;
        }

        private void DrawThickLineInternal(float x0, float y0, float x1, float y1, Color color, int width, bool miter, bool dash)
        {
            Debug.Assert(width > 1 && width < 100);
            
            // This properly centers line, while keeping geometries symmetric and nice.
            if ((width & 1) != 0)
            {
                x0 += 0.02f;
                y0 += 0.02f;
                x1 += 0.02f;
                y1 += 0.02f;
            }

            var batch = GetPolygonBatch(4, 6);
            var depth = graphics.DepthValue;
            var dashPattern = dash ? EncodeDashPattern(x0, x1, y0, y1) : (byte)0;

            var dx = x1 - x0;
            var dy = y1 - y0;
            var invHalfWidth = (width * 0.5f) / (float)MathF.Sqrt(dx * dx + dy * dy);
            dx *= invHalfWidth;
            dy *= invHalfWidth;

            if (miter)
            {
                x0 -= dx;
                y0 -= dy;
                x1 += dx;
                y1 += dy;
            }

            var i0 = (short)(batch.vtxIdx / 2 + 0);
            var i1 = (short)(batch.vtxIdx / 2 + 1);
            var i2 = (short)(batch.vtxIdx / 2 + 2);
            var i3 = (short)(batch.vtxIdx / 2 + 3);
 
            batch.idxArray[batch.idxIdx++] = i0;
            batch.idxArray[batch.idxIdx++] = i1;
            batch.idxArray[batch.idxIdx++] = i2;
            batch.idxArray[batch.idxIdx++] = i0;
            batch.idxArray[batch.idxIdx++] = i2;
            batch.idxArray[batch.idxIdx++] = i3;

            batch.vtxArray[batch.vtxIdx++] = x0 - dy;
            batch.vtxArray[batch.vtxIdx++] = y0 + dx;
            batch.vtxArray[batch.vtxIdx++] = x1 - dy;
            batch.vtxArray[batch.vtxIdx++] = y1 + dx;
            batch.vtxArray[batch.vtxIdx++] = x1 + dy;
            batch.vtxArray[batch.vtxIdx++] = y1 - dx;
            batch.vtxArray[batch.vtxIdx++] = x0 + dy;
            batch.vtxArray[batch.vtxIdx++] = y0 - dx;

            batch.colArray[batch.colIdx++] = color.ToAbgr();
            batch.colArray[batch.colIdx++] = color.ToAbgr();
            batch.colArray[batch.colIdx++] = color.ToAbgr();
            batch.colArray[batch.colIdx++] = color.ToAbgr();

            batch.dshArray[batch.dshIdx++] = dashPattern;
            batch.dshArray[batch.dshIdx++] = dashPattern;
            batch.dshArray[batch.dshIdx++] = dashPattern;
            batch.dshArray[batch.dshIdx++] = dashPattern;

            batch.depArray[batch.depIdx++] = depth;
            batch.depArray[batch.depIdx++] = depth;
            batch.depArray[batch.depIdx++] = depth;
            batch.depArray[batch.depIdx++] = depth;
        }

        private LineSmoothBatch GetLineSmoothBatch(int numVtxNeeded, int numIdxNeeded)
        {
            if (lineSmoothBatches == null)
            {
                lineSmoothBatches = new List<LineSmoothBatch>();
            }

            var batch = lineSmoothBatches.Count > 0 ? lineSmoothBatches[lineSmoothBatches.Count - 1] : null;

            if (batch == null ||
                batch.depIdx + numVtxNeeded >= batch.depArray.Length ||
                batch.idxIdx + numIdxNeeded >= batch.idxArray.Length)
            {
                batch = new LineSmoothBatch();
                batch.vtxArray = graphics.GetVertexArray();
                batch.dstArray = graphics.GetByteArray();
                batch.colArray = graphics.GetColorArray();
                batch.idxArray = graphics.GetIndexArray();
                batch.depArray = graphics.GetByteArray();
                lineSmoothBatches.Add(batch);
            }

            return batch;
        }

        private void DrawThickSmoothLineInternal(float x0, float y0, float x1, float y1, Color color, int width, bool miter)
        {
            Debug.Assert(width < 100);

            // Cant draw nice AA line that are 1 pixel wide.
            if (width == 1)
            {
                width = 2;
                x0 += 0.5f;
                y0 += 0.5f;
                x1 += 0.5f;
                y1 += 0.5f;
            }

            if (miter && width < 4)
            {
                miter = false;
            }

            var numVtxNeeded = (width & 1) != 0 ?  8 :  6;
            var numIdxNeeded = (width & 1) != 0 ? 18 : 12;
            var batch = GetLineSmoothBatch(numVtxNeeded, numIdxNeeded);
            var depth = graphics.DepthValue;

            // Odd values require extra vertices to work well with rasterization rules.
            if ((width & 1) != 0)
            {
                x0 += 0.49f;
                y0 += 0.49f;
                x1 += 0.49f;
                y1 += 0.49f; 

                if (y1 < y0)
                {
                    Utils.Swap(ref x0, ref x1);  
                    Utils.Swap(ref y0, ref y1);
                }

                var dx = x1 - x0;
                var dy = y1 - y0;
                var il = 1.0f / MathF.Sqrt(dx * dx + dy * dy);
                var ihw = 0.5f * il;
                var ohw = width * 0.5f * il;
                var idx = dx * ihw;
                var idy = dy * ihw;
                var odx = dx * ohw;
                var ody = dy * ohw;

                var miterAmount = miter ? ohw + 0.1f : 0.1f;

                dx *= il;
                dy *= il;
                x0 -= dx * miterAmount;
                y0 -= dy * miterAmount;
                x1 += dx * miterAmount;  
                y1 += dy * miterAmount;

                var i0 = (short)(batch.vtxIdx / 2 + 0);
                var i1 = (short)(i0 + 1);
                var i2 = (short)(i0 + 2);
                var i3 = (short)(i0 + 3);
                var i4 = (short)(i0 + 4);
                var i5 = (short)(i0 + 5);
                var i6 = (short)(i0 + 6);
                var i7 = (short)(i0 + 7);

                batch.idxArray[batch.idxIdx++] = i0;
                batch.idxArray[batch.idxIdx++] = i1;
                batch.idxArray[batch.idxIdx++] = i2;
                batch.idxArray[batch.idxIdx++] = i1;
                batch.idxArray[batch.idxIdx++] = i3;
                batch.idxArray[batch.idxIdx++] = i2;
                batch.idxArray[batch.idxIdx++] = i2;
                batch.idxArray[batch.idxIdx++] = i3;
                batch.idxArray[batch.idxIdx++] = i4;
                batch.idxArray[batch.idxIdx++] = i3;
                batch.idxArray[batch.idxIdx++] = i5;
                batch.idxArray[batch.idxIdx++] = i4;
                batch.idxArray[batch.idxIdx++] = i4;
                batch.idxArray[batch.idxIdx++] = i5;
                batch.idxArray[batch.idxIdx++] = i6;
                batch.idxArray[batch.idxIdx++] = i5;
                batch.idxArray[batch.idxIdx++] = i7;
                batch.idxArray[batch.idxIdx++] = i6;

                batch.vtxArray[batch.vtxIdx++] = x0 - ody;
                batch.vtxArray[batch.vtxIdx++] = y0 + odx;
                batch.vtxArray[batch.vtxIdx++] = x1 - ody;
                batch.vtxArray[batch.vtxIdx++] = y1 + odx;
                batch.vtxArray[batch.vtxIdx++] = x0 - idy;
                batch.vtxArray[batch.vtxIdx++] = y0 + idx;
                batch.vtxArray[batch.vtxIdx++] = x1 - idy;
                batch.vtxArray[batch.vtxIdx++] = y1 + idx;
                batch.vtxArray[batch.vtxIdx++] = x0 + idy;
                batch.vtxArray[batch.vtxIdx++] = y0 - idx;
                batch.vtxArray[batch.vtxIdx++] = x1 + idy;
                batch.vtxArray[batch.vtxIdx++] = y1 - idx;
                batch.vtxArray[batch.vtxIdx++] = x0 + ody;
                batch.vtxArray[batch.vtxIdx++] = y0 - odx;
                batch.vtxArray[batch.vtxIdx++] = x1 + ody;
                batch.vtxArray[batch.vtxIdx++] = y1 - odx;

                batch.colArray[batch.colIdx++] = color.ToAbgr();
                batch.colArray[batch.colIdx++] = color.ToAbgr();
                batch.colArray[batch.colIdx++] = color.ToAbgr();
                batch.colArray[batch.colIdx++] = color.ToAbgr();
                batch.colArray[batch.colIdx++] = color.ToAbgr();
                batch.colArray[batch.colIdx++] = color.ToAbgr();
                batch.colArray[batch.colIdx++] = color.ToAbgr();
                batch.colArray[batch.colIdx++] = color.ToAbgr();

                batch.dstArray[batch.dstIdx++] = 0;
                batch.dstArray[batch.dstIdx++] = 0;
                batch.dstArray[batch.dstIdx++] = (byte)(width - 1); 
                batch.dstArray[batch.dstIdx++] = (byte)(width - 1);
                batch.dstArray[batch.dstIdx++] = (byte)(width - 1);
                batch.dstArray[batch.dstIdx++] = (byte)(width - 1);
                batch.dstArray[batch.dstIdx++] = 0;
                batch.dstArray[batch.dstIdx++] = 0;

                batch.depArray[batch.depIdx++] = depth;
                batch.depArray[batch.depIdx++] = depth;
                batch.depArray[batch.depIdx++] = depth;
                batch.depArray[batch.depIdx++] = depth;
                batch.depArray[batch.depIdx++] = depth;
                batch.depArray[batch.depIdx++] = depth;
                batch.depArray[batch.depIdx++] = depth;
                batch.depArray[batch.depIdx++] = depth;
            }
            else
            {
                var dx = x1 - x0;
                var dy = y1 - y0;
                var il = 1.0f / (float)MathF.Sqrt(dx * dx + dy * dy);
                var hw = (width * 0.5f) * il;
                dx *= hw;
                dy *= hw;

                if (miter)
                {
                    x0 -= dx * 0.49f;
                    y0 -= dy * 0.49f;
                    x1 += dx * 0.49f;
                    y1 += dy * 0.49f;
                }

                var i0 = (short)(batch.vtxIdx / 2 + 0);
                var i1 = (short)(i0 + 1);
                var i2 = (short)(i0 + 2);
                var i3 = (short)(i0 + 3);
                var i4 = (short)(i0 + 4);
                var i5 = (short)(i0 + 5);

                batch.idxArray[batch.idxIdx++] = i0;
                batch.idxArray[batch.idxIdx++] = i1;
                batch.idxArray[batch.idxIdx++] = i2;
                batch.idxArray[batch.idxIdx++] = i1;
                batch.idxArray[batch.idxIdx++] = i3;
                batch.idxArray[batch.idxIdx++] = i2;
                batch.idxArray[batch.idxIdx++] = i2;
                batch.idxArray[batch.idxIdx++] = i3;
                batch.idxArray[batch.idxIdx++] = i4;
                batch.idxArray[batch.idxIdx++] = i2;
                batch.idxArray[batch.idxIdx++] = i5;
                batch.idxArray[batch.idxIdx++] = i4;

                batch.vtxArray[batch.vtxIdx++] = x0 - dy;
                batch.vtxArray[batch.vtxIdx++] = y0 + dx;
                batch.vtxArray[batch.vtxIdx++] = x1 - dy;
                batch.vtxArray[batch.vtxIdx++] = y1 + dx;
                batch.vtxArray[batch.vtxIdx++] = x0;
                batch.vtxArray[batch.vtxIdx++] = y0;
                batch.vtxArray[batch.vtxIdx++] = x1;
                batch.vtxArray[batch.vtxIdx++] = y1;
                batch.vtxArray[batch.vtxIdx++] = x1 + dy;
                batch.vtxArray[batch.vtxIdx++] = y1 - dx;
                batch.vtxArray[batch.vtxIdx++] = x0 + dy;
                batch.vtxArray[batch.vtxIdx++] = y0 - dx;

                batch.colArray[batch.colIdx++] = color.ToAbgr();
                batch.colArray[batch.colIdx++] = color.ToAbgr();
                batch.colArray[batch.colIdx++] = color.ToAbgr();
                batch.colArray[batch.colIdx++] = color.ToAbgr();
                batch.colArray[batch.colIdx++] = color.ToAbgr();
                batch.colArray[batch.colIdx++] = color.ToAbgr();

                batch.dstArray[batch.dstIdx++] = 0;
                batch.dstArray[batch.dstIdx++] = 0;
                batch.dstArray[batch.dstIdx++] = (byte)width;
                batch.dstArray[batch.dstIdx++] = (byte)width;
                batch.dstArray[batch.dstIdx++] = 0;
                batch.dstArray[batch.dstIdx++] = 0;

                batch.depArray[batch.depIdx++] = depth;
                batch.depArray[batch.depIdx++] = depth;
                batch.depArray[batch.depIdx++] = depth;
                batch.depArray[batch.depIdx++] = depth;
                batch.depArray[batch.depIdx++] = depth;
                batch.depArray[batch.depIdx++] = depth;
            }
        }

        public void DrawLine(float x0, float y0, float x1, float y1, Color color, int width = 1, bool smooth = false, bool dash = false)
        {
            width += graphics.LineWidthBias;

            xform.TransformPoint(ref x0, ref y0);
            xform.TransformPoint(ref x1, ref y1);

            if (smooth)
            { 
                DrawThickSmoothLineInternal(x0, y0, x1, y1, color, width, false);
            }
            else if (width > 1)
            {
                DrawThickLineInternal(x0, y0, x1, y1, color, width, false, dash);
            }
            else
            {
                DrawLineInternal(x0, y0, x1, y1, color, dash);
            }
        }

        public void DrawLine(List<float> points, Color color, int width = 1, bool smooth = false, bool miter = false)
        {
            DrawLine(CollectionsMarshal.AsSpan(points), color, width, smooth, miter);
        }

        public void DrawLine(Span<float> points, Color color, int width = 1, bool smooth = false, bool miter = false)
        {
            Debug.Assert(width > 1.0f || !miter);

            if (points.Length == 0)
                return;

            width += graphics.LineWidthBias;
            smooth |= width > 1.0f;

            var x0 = points[0];
            var y0 = points[1];

            xform.TransformPoint(ref x0, ref y0);

            for (int i = 2; i < points.Length; i += 2)
            {
                var x1 = points[i + 0];
                var y1 = points[i + 1];
                
                xform.TransformPoint(ref x1, ref y1);

                if (smooth)
                {
                    DrawThickSmoothLineInternal(x0, y0, x1, y1, color, width, false);
                }
                else if (width > 1)
                {
                    DrawThickLineInternal(x0, y0, x1, y1, color, width, false, false);
                }
                else
                {
                    DrawLineInternal(x0, y0, x1, y1, color, false);
                }

                x0 = x1;
                y0 = y1;
            }
        }

        public void DrawGeometry(Span<float> points, Color color, int width = 1, bool smooth = false, bool close = true, bool miter = false)
        {
            width += graphics.LineWidthBias;
            
            var x0 = points[0];
            var y0 = points[1];
            xform.TransformPoint(ref x0, ref y0);

            var numVerts = points.Length / 2;
            var numLines = numVerts - (close ? 0 : 1);

            for (int i = 0; i < numLines; i++)
            {
                var x1 = points[((i + 1) % numVerts) * 2 + 0];
                var y1 = points[((i + 1) % numVerts) * 2 + 1];
                xform.TransformPoint(ref x1, ref y1);

                if (smooth)
                {
                    DrawThickSmoothLineInternal(x0, y0, x1, y1, color, width, miter);
                }
                else if (width > 1)
                {
                    DrawThickLineInternal(x0, y0, x1, y1, color, width, miter, false);
                }
                else
                {
                    DrawLineInternal(x0, y0, x1, y1, color, false);
                }

                x0 = x1;
                y0 = y1;
            }
        }

        // Only used by oscilloscope video for now.
        struct CornerInfo
        {
            public int pi;
            public int ci;
            public float x;
            public float y;
            public float cos;
            public int side;
        };

        public unsafe void DrawNiceSmoothLine(Span<float> points, Color color, int width = 2)
        {
            // Only implemented for odd widths for now.
            width = Utils.RoundUp(width + graphics.LineWidthBias, 2);

            var cornerCount = 0;
            var cornerInfos = stackalloc CornerInfo[points.Length];

            var close = false; // Supported, but not exposed.
            var depth = graphics.DepthValue;
            var numVerts = points.Length / 2;
            var numLines = numVerts - (close ? 0 : 1);

            var numVtxNeeded = numVerts * 7;
            var numIdxNeeded = numVerts * 18;
            var batch = GetLineSmoothBatch(numVtxNeeded, numIdxNeeded);
            var startIndex = batch.vtxIdx / 2;

            var px = close ? points[points.Length - 2] : 2 * points[0] - points[2];
            var py = close ? points[points.Length - 1] : 2 * points[1] - points[3];
            xform.TransformPoint(ref px, ref py);

            var cx = points[0];
            var cy = points[1];
            xform.TransformPoint(ref cx, ref cy);

            var dpx = cx - px;
            var dpy = cy - py;
            Utils.Normalize(ref dpx, ref dpy);

            for (int i = 0; i < numLines; i++)
            {
                var ni = (i + 1) % numVerts;

                var nx = points[ni * 2 + 0];
                var ny = points[ni * 2 + 1];
                xform.TransformPoint(ref nx, ref ny);

                var dnx = nx - cx;
                var dny = ny - cy;
                Utils.Normalize(ref dnx, ref dny);

                var sx = (width * 0.5f) * dnx;
                var sy = (width * 0.5f) * dny;

                // Main line body.
                var i0 = (short)(batch.vtxIdx / 2);
                var i1 = (short)(i0 + 1);
                var i2 = (short)(i0 + 2);
                var i3 = (short)(i0 + 3);
                var i4 = (short)(i0 + 4);
                var i5 = (short)(i0 + 5);

                batch.idxArray[batch.idxIdx++] = i0;
                batch.idxArray[batch.idxIdx++] = i3;
                batch.idxArray[batch.idxIdx++] = i1;
                batch.idxArray[batch.idxIdx++] = i1;
                batch.idxArray[batch.idxIdx++] = i3;
                batch.idxArray[batch.idxIdx++] = i4;
                batch.idxArray[batch.idxIdx++] = i1;
                batch.idxArray[batch.idxIdx++] = i4;
                batch.idxArray[batch.idxIdx++] = i2;
                batch.idxArray[batch.idxIdx++] = i2;
                batch.idxArray[batch.idxIdx++] = i4;
                batch.idxArray[batch.idxIdx++] = i5;

                batch.vtxArray[batch.vtxIdx++] = cx - sy;
                batch.vtxArray[batch.vtxIdx++] = cy + sx;
                batch.vtxArray[batch.vtxIdx++] = cx;
                batch.vtxArray[batch.vtxIdx++] = cy;
                batch.vtxArray[batch.vtxIdx++] = cx + sy;
                batch.vtxArray[batch.vtxIdx++] = cy - sx;
                batch.vtxArray[batch.vtxIdx++] = nx - sy;
                batch.vtxArray[batch.vtxIdx++] = ny + sx;
                batch.vtxArray[batch.vtxIdx++] = nx;
                batch.vtxArray[batch.vtxIdx++] = ny;
                batch.vtxArray[batch.vtxIdx++] = nx + sy;
                batch.vtxArray[batch.vtxIdx++] = ny - sx;

                batch.colArray[batch.colIdx++] = color.ToAbgr();
                batch.colArray[batch.colIdx++] = color.ToAbgr();
                batch.colArray[batch.colIdx++] = color.ToAbgr();
                batch.colArray[batch.colIdx++] = color.ToAbgr();
                batch.colArray[batch.colIdx++] = color.ToAbgr();
                batch.colArray[batch.colIdx++] = color.ToAbgr();

                batch.dstArray[batch.dstIdx++] = 0;
                batch.dstArray[batch.dstIdx++] = (byte)width;
                batch.dstArray[batch.dstIdx++] = 0;
                batch.dstArray[batch.dstIdx++] = 0;
                batch.dstArray[batch.dstIdx++] = (byte)width;
                batch.dstArray[batch.dstIdx++] = 0;

                batch.depArray[batch.depIdx++] = depth;
                batch.depArray[batch.depIdx++] = depth;
                batch.depArray[batch.depIdx++] = depth;
                batch.depArray[batch.depIdx++] = depth;
                batch.depArray[batch.depIdx++] = depth;
                batch.depArray[batch.depIdx++] = depth;

                var cos = Utils.Dot(dnx, dny, -dpx, -dpy);

                // Optional outer corner, will be processed after.
                if (cos > -0.999999f)
                {
                    var dx = dnx - dpx;
                    var dy = dny - dpy;
                    Utils.Normalize(ref dx, ref dy);

                    cornerInfos[cornerCount].pi = i == 0 ? numVerts - 1 : i - 1;
                    cornerInfos[cornerCount].ci = i;
                    cornerInfos[cornerCount].x = cx - dx * (width * 0.5f);
                    cornerInfos[cornerCount].y = cy - dy * (width * 0.5f);
                    cornerInfos[cornerCount].cos = cos;
                    cornerInfos[cornerCount].side = Utils.Cross(dpx, dpy, dnx, dny) >= 0 ? 1 : -1;
                    cornerCount++;
                }

                cx = nx;
                cy = ny;
                dpx = dnx;
                dpy = dny;
            }

            for (int i = 0; i < cornerCount; i++)
            {
                var corner = cornerInfos[i];

                if (corner.cos > 0)
                {
                    var i0 = (short)(batch.vtxIdx / 2);

                    // Extra corner vertex.
                    batch.vtxArray[batch.vtxIdx++] = corner.x;
                    batch.vtxArray[batch.vtxIdx++] = corner.y;
                    batch.colArray[batch.colIdx++] = color.ToAbgr();
                    batch.dstArray[batch.dstIdx++] = 0;
                    batch.depArray[batch.depIdx++] = depth;

                    // 2 triangles if angle is large.
                    if (corner.side > 0)
                    { 
                        batch.idxArray[batch.idxIdx++] = (short)(startIndex + corner.ci * 6 + 1);
                        batch.idxArray[batch.idxIdx++] = (short)(startIndex + corner.ci * 6 + 2);
                        batch.idxArray[batch.idxIdx++] = (short)(i0);
                        batch.idxArray[batch.idxIdx++] = (short)(startIndex + corner.ci * 6 + 1);
                        batch.idxArray[batch.idxIdx++] = (short)(i0);
                        batch.idxArray[batch.idxIdx++] = (short)(startIndex + corner.pi * 6 + 5);
                    }
                    else
                    {
                        batch.idxArray[batch.idxIdx++] = (short)(startIndex + corner.ci * 6 + 1);
                        batch.idxArray[batch.idxIdx++] = (short)(startIndex + corner.pi * 6 + 3);
                        batch.idxArray[batch.idxIdx++] = (short)(i0);
                        batch.idxArray[batch.idxIdx++] = (short)(startIndex + corner.ci * 6 + 1);
                        batch.idxArray[batch.idxIdx++] = (short)(i0);
                        batch.idxArray[batch.idxIdx++] = (short)(startIndex + corner.ci * 6 + 0);
                    }
                }
                else
                {
                    // 1 triangle if angle is small
                    if (corner.side > 0)
                    { 
                        batch.idxArray[batch.idxIdx++] = (short)(startIndex + corner.ci * 6 + 1);
                        batch.idxArray[batch.idxIdx++] = (short)(startIndex + corner.ci * 6 + 2);
                        batch.idxArray[batch.idxIdx++] = (short)(startIndex + corner.pi * 6 + 5);
                    }
                    else
                    {
                        batch.idxArray[batch.idxIdx++] = (short)(startIndex + corner.ci * 6 + 1);
                        batch.idxArray[batch.idxIdx++] = (short)(startIndex + corner.ci * 6 + 0);
                        batch.idxArray[batch.idxIdx++] = (short)(startIndex + corner.pi * 6 + 3);
                    }
                }
            }
        }

        public void DrawRectangle(Rectangle rect, Color color, int width = 1, bool smooth = false, bool miter = false)
        {
            DrawRectangle(rect.Left, rect.Top, rect.Right, rect.Bottom, color, width, smooth);
        }

        public void DrawRectangle(RectangleF rect, Color color, int width = 1, bool smooth = false, bool miter = false)
        {
            DrawRectangle(rect.Left, rect.Top, rect.Right, rect.Bottom, color, width, smooth);
        }

        public void DrawRectangle(float x0, float y0, float x1, float y1, Color color, int width = 1, bool smooth = false, bool miter = false)
        {
            width += graphics.LineWidthBias;

            xform.TransformPoint(ref x0, ref y0);
            xform.TransformPoint(ref x1, ref y1);

            if (smooth)
            {
                var halfWidth = miter && width >= 4 ? width * 0.35f : 0.0f;
                DrawThickSmoothLineInternal(x0 - halfWidth, y0, x1 + halfWidth, y0, color, width, false);
                DrawThickSmoothLineInternal(x1, y0 - halfWidth, x1, y1 + halfWidth, color, width, false);
                DrawThickSmoothLineInternal(x0 - halfWidth, y1, x1 + halfWidth, y1, color, width, false);
                DrawThickSmoothLineInternal(x0, y0 - halfWidth, x0, y1 + halfWidth, color, width, false);
            }
            else if (width > 1)
            {
                var halfWidth = width * 0.5f;
                DrawThickLineInternal(x0 - halfWidth, y0, x1 + halfWidth, y0, color, width, false, false);
                DrawThickLineInternal(x1, y0 - halfWidth, x1, y1 + halfWidth, color, width, false, false);
                DrawThickLineInternal(x0 - halfWidth, y1, x1 + halfWidth, y1, color, width, false, false);
                DrawThickLineInternal(x0, y0 - halfWidth, x0, y1 + halfWidth, color, width, false, false);
            }
            else
            {
                // Line rasterization rules makes is so that the last pixel is missing. So +1.
                DrawLineInternal(x0, y0, x1 + 1, y0, color, false);
                DrawLineInternal(x1, y0, x1, y1 + 1, color, false);
                DrawLineInternal(x0, y1, x1 + 1, y1, color, false);
                DrawLineInternal(x0, y0, x0, y1 + 1, color, false);
            }
        }
        public void FillGeometry(Span<float> geo, Color color, bool smooth = false)
        {
            FillGeometryInternal(geo, color, color, 0, smooth);
        }

        public void FillGeometryGradient(Span<float> geo, Color color0, Color color1, int gradientSize, bool smooth = false)
        {
            FillGeometryInternal(geo, color0, color1, gradientSize, smooth);
        }
        
        public void FillAndDrawGeometry(Span<float> geo, Color fillColor, Color lineColor, int lineWidth = 1, bool smooth = false)
        {
        	FillGeometry(geo, fillColor, smooth);
			DrawGeometry(geo, lineColor, lineWidth, smooth, true);
        }
		
        public void FillRectangle(float x0, float y0, float x1, float y1, Color color)
        {
            var batch = GetPolygonBatch(4, 6);
            var depth = graphics.DepthValue;

            xform.TransformPoint(ref x0, ref y0);
            xform.TransformPoint(ref x1, ref y1);

            var i0 = (short)(batch.vtxIdx / 2 + 0);
            var i1 = (short)(batch.vtxIdx / 2 + 1);
            var i2 = (short)(batch.vtxIdx / 2 + 2);
            var i3 = (short)(batch.vtxIdx / 2 + 3);

            batch.idxArray[batch.idxIdx++] = i0;
            batch.idxArray[batch.idxIdx++] = i1;
            batch.idxArray[batch.idxIdx++] = i2;
            batch.idxArray[batch.idxIdx++] = i0;
            batch.idxArray[batch.idxIdx++] = i2;
            batch.idxArray[batch.idxIdx++] = i3;

            batch.vtxArray[batch.vtxIdx++] = x0;
            batch.vtxArray[batch.vtxIdx++] = y0;
            batch.vtxArray[batch.vtxIdx++] = x1;
            batch.vtxArray[batch.vtxIdx++] = y0;
            batch.vtxArray[batch.vtxIdx++] = x1;
            batch.vtxArray[batch.vtxIdx++] = y1;
            batch.vtxArray[batch.vtxIdx++] = x0;
            batch.vtxArray[batch.vtxIdx++] = y1;

            batch.colArray[batch.colIdx++] = color.ToAbgr();
            batch.colArray[batch.colIdx++] = color.ToAbgr();
            batch.colArray[batch.colIdx++] = color.ToAbgr();
            batch.colArray[batch.colIdx++] = color.ToAbgr();

            batch.dshArray[batch.dshIdx++] = 0;
            batch.dshArray[batch.dshIdx++] = 0;
            batch.dshArray[batch.dshIdx++] = 0;
            batch.dshArray[batch.dshIdx++] = 0;

            batch.depArray[batch.depIdx++] = depth;
            batch.depArray[batch.depIdx++] = depth;
            batch.depArray[batch.depIdx++] = depth;
            batch.depArray[batch.depIdx++] = depth;

            Debug.Assert(batch.colIdx * 2 == batch.vtxIdx);
        }

        public void FillRectangleGradient(float x0, float y0, float x1, float y1, Color color0, Color color1, bool vertical, float gradientSize)
        {
            var depth = graphics.DepthValue;

            xform.TransformPoint(ref x0, ref y0);
            xform.TransformPoint(ref x1, ref y1);
                
            bool fullHorizontalGradient = !vertical && MathF.Abs(gradientSize) >= MathF.Abs(x1 - x0);
            bool fullVerticalGradient   =  vertical && MathF.Abs(gradientSize) >= MathF.Abs(y1 - y0);

            if (fullHorizontalGradient || fullVerticalGradient)
            {
                var batch = GetPolygonBatch(4, 6);

                var i0 = (short)(batch.vtxIdx / 2 + 0);
                var i1 = (short)(batch.vtxIdx / 2 + 1);
                var i2 = (short)(batch.vtxIdx / 2 + 2);
                var i3 = (short)(batch.vtxIdx / 2 + 3);

                batch.idxArray[batch.idxIdx++] = i0;
                batch.idxArray[batch.idxIdx++] = i1;
                batch.idxArray[batch.idxIdx++] = i2;
                batch.idxArray[batch.idxIdx++] = i0;
                batch.idxArray[batch.idxIdx++] = i2;
                batch.idxArray[batch.idxIdx++] = i3;

                batch.vtxArray[batch.vtxIdx++] = x0;
                batch.vtxArray[batch.vtxIdx++] = y0;
                batch.vtxArray[batch.vtxIdx++] = x1;
                batch.vtxArray[batch.vtxIdx++] = y0;
                batch.vtxArray[batch.vtxIdx++] = x1;
                batch.vtxArray[batch.vtxIdx++] = y1;
                batch.vtxArray[batch.vtxIdx++] = x0;
                batch.vtxArray[batch.vtxIdx++] = y1;

                if (fullHorizontalGradient)
                {
                    batch.colArray[batch.colIdx++] = color0.ToAbgr();
                    batch.colArray[batch.colIdx++] = color1.ToAbgr();
                    batch.colArray[batch.colIdx++] = color1.ToAbgr();
                    batch.colArray[batch.colIdx++] = color0.ToAbgr();
                }
                else
                {
                    batch.colArray[batch.colIdx++] = color0.ToAbgr();
                    batch.colArray[batch.colIdx++] = color0.ToAbgr();
                    batch.colArray[batch.colIdx++] = color1.ToAbgr();
                    batch.colArray[batch.colIdx++] = color1.ToAbgr();
                }

                batch.dshArray[batch.dshIdx++] = 0;
                batch.dshArray[batch.dshIdx++] = 0;
                batch.dshArray[batch.dshIdx++] = 0;
                batch.dshArray[batch.dshIdx++] = 0;

                batch.depArray[batch.depIdx++] = depth;
                batch.depArray[batch.depIdx++] = depth;
                batch.depArray[batch.depIdx++] = depth;
                batch.depArray[batch.depIdx++] = depth;

                Debug.Assert(batch.colIdx * 2 == batch.vtxIdx);
            }
            else
            {
                var batch = GetPolygonBatch(8, 12);

                var i0 = (short)(batch.vtxIdx / 2 + 0);
                var i1 = (short)(batch.vtxIdx / 2 + 1);
                var i2 = (short)(batch.vtxIdx / 2 + 2);
                var i3 = (short)(batch.vtxIdx / 2 + 3);
                var i4 = (short)(batch.vtxIdx / 2 + 4);
                var i5 = (short)(batch.vtxIdx / 2 + 5);
                var i6 = (short)(batch.vtxIdx / 2 + 6);
                var i7 = (short)(batch.vtxIdx / 2 + 7);

                batch.idxArray[batch.idxIdx++] = i0;
                batch.idxArray[batch.idxIdx++] = i1;
                batch.idxArray[batch.idxIdx++] = i2;
                batch.idxArray[batch.idxIdx++] = i0;
                batch.idxArray[batch.idxIdx++] = i2;
                batch.idxArray[batch.idxIdx++] = i3;
                batch.idxArray[batch.idxIdx++] = i4;
                batch.idxArray[batch.idxIdx++] = i5;
                batch.idxArray[batch.idxIdx++] = i6;
                batch.idxArray[batch.idxIdx++] = i4;
                batch.idxArray[batch.idxIdx++] = i6;
                batch.idxArray[batch.idxIdx++] = i7;

                if (!vertical)
                {
                    float xm = x0 + gradientSize;

                    batch.vtxArray[batch.vtxIdx++] = x0;
                    batch.vtxArray[batch.vtxIdx++] = y0;
                    batch.vtxArray[batch.vtxIdx++] = xm;
                    batch.vtxArray[batch.vtxIdx++] = y0;
                    batch.vtxArray[batch.vtxIdx++] = xm;
                    batch.vtxArray[batch.vtxIdx++] = y1;
                    batch.vtxArray[batch.vtxIdx++] = x0;
                    batch.vtxArray[batch.vtxIdx++] = y1;
                    batch.vtxArray[batch.vtxIdx++] = xm;
                    batch.vtxArray[batch.vtxIdx++] = y0;
                    batch.vtxArray[batch.vtxIdx++] = x1;
                    batch.vtxArray[batch.vtxIdx++] = y0;
                    batch.vtxArray[batch.vtxIdx++] = x1;
                    batch.vtxArray[batch.vtxIdx++] = y1;
                    batch.vtxArray[batch.vtxIdx++] = xm;
                    batch.vtxArray[batch.vtxIdx++] = y1;
                }
                else
                {
                    float ym = y0 + gradientSize;

                    batch.vtxArray[batch.vtxIdx++] = x0;
                    batch.vtxArray[batch.vtxIdx++] = y0;
                    batch.vtxArray[batch.vtxIdx++] = x0;
                    batch.vtxArray[batch.vtxIdx++] = ym;
                    batch.vtxArray[batch.vtxIdx++] = x1;
                    batch.vtxArray[batch.vtxIdx++] = ym;
                    batch.vtxArray[batch.vtxIdx++] = x1;
                    batch.vtxArray[batch.vtxIdx++] = y0;
                    batch.vtxArray[batch.vtxIdx++] = x0;
                    batch.vtxArray[batch.vtxIdx++] = ym;
                    batch.vtxArray[batch.vtxIdx++] = x0;
                    batch.vtxArray[batch.vtxIdx++] = y1;
                    batch.vtxArray[batch.vtxIdx++] = x1;
                    batch.vtxArray[batch.vtxIdx++] = y1;
                    batch.vtxArray[batch.vtxIdx++] = x1;
                    batch.vtxArray[batch.vtxIdx++] = ym;
                }

                batch.colArray[batch.colIdx++] = color0.ToAbgr();
                batch.colArray[batch.colIdx++] = color1.ToAbgr();
                batch.colArray[batch.colIdx++] = color1.ToAbgr();
                batch.colArray[batch.colIdx++] = color0.ToAbgr();
                batch.colArray[batch.colIdx++] = color1.ToAbgr();
                batch.colArray[batch.colIdx++] = color1.ToAbgr();
                batch.colArray[batch.colIdx++] = color1.ToAbgr();
                batch.colArray[batch.colIdx++] = color1.ToAbgr();

                batch.dshArray[batch.dshIdx++] = 0;
                batch.dshArray[batch.dshIdx++] = 0;
                batch.dshArray[batch.dshIdx++] = 0;
                batch.dshArray[batch.dshIdx++] = 0;
                batch.dshArray[batch.dshIdx++] = 0;
                batch.dshArray[batch.dshIdx++] = 0;
                batch.dshArray[batch.dshIdx++] = 0;
                batch.dshArray[batch.dshIdx++] = 0;

                batch.depArray[batch.depIdx++] = depth;
                batch.depArray[batch.depIdx++] = depth;
                batch.depArray[batch.depIdx++] = depth;
                batch.depArray[batch.depIdx++] = depth;
                batch.depArray[batch.depIdx++] = depth;
                batch.depArray[batch.depIdx++] = depth;
                batch.depArray[batch.depIdx++] = depth;
                batch.depArray[batch.depIdx++] = depth;

                Debug.Assert(batch.colIdx * 2 == batch.vtxIdx);
            }
        }

        public void FillClipRegion(Color color)
        {
            Debug.Assert(!xform.HasScaling);

            var clipRect = graphics.CurrentClipRegion;
            var x = clipRect.X;
            var y = clipRect.Y;
            xform.ReverseTransformPoint(ref x, ref y);

            FillRectangle(x, y, x + clipRect.Width, y + clipRect.Height, color);
        }

        public void FillRectangle(Rectangle rect, Color color)
        {
            FillRectangle(rect.Left, rect.Top, rect.Right, rect.Bottom, color);
        }

        public void FillRectangleGradient(Rectangle rect, Color color0, Color color1, bool vertical, int gradientSize)
        {
            FillRectangleGradient(rect.Left, rect.Top, rect.Right, rect.Bottom, color0, color1, vertical, gradientSize);
        }

        public void FillRectangle(RectangleF rect, Color color)
        {
            FillRectangle(rect.Left, rect.Top, rect.Right, rect.Bottom, color);
        }

        public void FillAndDrawRectangle(float x0, float y0, float x1, float y1, Color fillColor, Color lineColor, int width = 1, bool smooth = false, bool miter = false)
        {
            FillRectangle(x0, y0, x1, y1, fillColor);
            DrawRectangle(x0, y0, x1, y1, lineColor, width, smooth, miter);
        }

        public void FillAndDrawRectangleGradient(float x0, float y0, float x1, float y1, Color fillColor0, Color fillColor1, Color lineColor, bool vertical, int gradientSize, int width = 1, bool smooth = false, bool miter = false)
        {
            FillRectangleGradient(x0, y0, x1, y1, fillColor0, fillColor1, vertical, gradientSize);
            DrawRectangle(x0, y0, x1, y1, lineColor, width, smooth, miter);
        }

        public void FillAndDrawRectangle(Rectangle rect, Color fillColor, Color lineColor, int width = 1, bool smooth = false, bool miter = false)
        {
            FillRectangle(rect, fillColor);
            DrawRectangle(rect, lineColor, width, smooth, miter);
        }

        public void FillAndDrawRectangleGradient(Rectangle rect, Color fillColor0, Color fillColor1, Color lineColor, bool vertical, int gradientSize, int width = 1, bool smooth = false)
        {
            FillRectangleGradient(rect, fillColor0, fillColor1, vertical, gradientSize);
            DrawRectangle(rect, lineColor, width, smooth);
        }

        private void FillGeometryInternal(Span<float> points, Color color0, Color color1, int gradientSize, bool smooth = false)
        {
            var gradient = gradientSize > 0;
            var numVerts = points.Length / 2;
            var batch = GetPolygonBatch(numVerts * 2, (numVerts - 2) * 3 + (smooth ? (numVerts) * 6 : 0));
            var depth = graphics.DepthValue;
            var i0 = (short)(batch.vtxIdx / 2);

            if (smooth)
            { 
                var px = points[points.Length - 2];
                var py = points[points.Length - 1];
                xform.TransformPoint(ref px, ref py);

                float cx = points[0];
                float cy = points[1];
                xform.TransformPoint(ref cx, ref cy);

                var dpx = cx - px;
                var dpy = cy - py;
                Utils.Normalize(ref dpx, ref dpy);

                for (int i = 0; i < numVerts; i++)
                {
                    var ni = (i + 1) % numVerts;

                    float nx = points[ni * 2 + 0];
                    float ny = points[ni * 2 + 1];

                    Color gradientColor;

                    if (gradient)
                    {
                        float lerp = ny / gradientSize;
                        byte r = (byte)(color0.R * (1.0f - lerp) + (color1.R * lerp));
                        byte g = (byte)(color0.G * (1.0f - lerp) + (color1.G * lerp));
                        byte b = (byte)(color0.B * (1.0f - lerp) + (color1.B * lerp));
                        byte a = (byte)(color0.A * (1.0f - lerp) + (color1.A * lerp));
                        gradientColor = new Color(r, g, b, a);
                    }
                    else
                    {
                        gradientColor = color0;
                    }

                    xform.TransformPoint(ref nx, ref ny);

                    var dnx = nx - cx;
                    var dny = ny - cy;
                    Utils.Normalize(ref dnx, ref dny);

                    var dx = (dnx - dpx) * 0.5f;
                    var dy = (dny - dpy) * 0.5f;
                    Utils.Normalize(ref dx, ref dy);

                    // Cos -> Csc
                    var d = 0.7071f / MathF.Sqrt(1.0f - Utils.Saturate(Utils.Dot(dnx, dny, -dpx, -dpy)));
                    var ix = cx + dx * d;
                    var iy = cy + dy * d;
                    var ox = cx - dx * d;
                    var oy = cy - dy * d;

                    batch.vtxArray[batch.vtxIdx++] = ix;
                    batch.vtxArray[batch.vtxIdx++] = iy;
                    batch.vtxArray[batch.vtxIdx++] = ox;
                    batch.vtxArray[batch.vtxIdx++] = oy;

                    batch.colArray[batch.colIdx++] = gradientColor.ToAbgr();
                    batch.colArray[batch.colIdx++] = Color.FromArgb(0, gradientColor).ToAbgr();

                    batch.depArray[batch.depIdx++] = depth;
                    batch.depArray[batch.depIdx++] = depth;

                    batch.dshArray[batch.dshIdx++] = 0;
                    batch.dshArray[batch.dshIdx++] = 0;

                    cx = nx;
                    cy = ny;
                    dpx = dnx;
                    dpy = dny;
                }

                // Simple fan for the inside
                for (int i = 0; i < numVerts - 2; i++)
                {
                    batch.idxArray[batch.idxIdx++] = i0;
                    batch.idxArray[batch.idxIdx++] = (short)(i0 + i * 2 + 2);
                    batch.idxArray[batch.idxIdx++] = (short)(i0 + i * 2 + 4);
                }

                // A few more quads for the anti-aliased section.
                for (int i = 0; i < numVerts; i++)
                {
                    var ni = (i + 1) % numVerts;

                    var qi0 = (short)(i0 + i  * 2 + 0);
                    var qi1 = (short)(i0 + i  * 2 + 1);
                    var qi2 = (short)(i0 + ni * 2 + 0);
                    var qi3 = (short)(i0 + ni * 2 + 1);

                    batch.idxArray[batch.idxIdx++] = qi0;
                    batch.idxArray[batch.idxIdx++] = qi1;
                    batch.idxArray[batch.idxIdx++] = qi2;
                    batch.idxArray[batch.idxIdx++] = qi1;
                    batch.idxArray[batch.idxIdx++] = qi3;
                    batch.idxArray[batch.idxIdx++] = qi2;
                }
            }
            else
            {
                for (int i = 0; i < numVerts; i++)
                {
                    var ni = (i + 1) % numVerts;

                    float nx = points[ni * 2 + 0];
                    float ny = points[ni * 2 + 1];

                    Color gradientColor;

                    if (gradient)
                    {
                        float lerp = ny / gradientSize;
                        byte r = (byte)(color0.R * (1.0f - lerp) + (color1.R * lerp));
                        byte g = (byte)(color0.G * (1.0f - lerp) + (color1.G * lerp));
                        byte b = (byte)(color0.B * (1.0f - lerp) + (color1.B * lerp));
                        byte a = (byte)(color0.A * (1.0f - lerp) + (color1.A * lerp));
                        gradientColor = new Color(r, g, b, a);
                    }
                    else
                    {
                        gradientColor = color0;
                    }

                    xform.TransformPoint(ref nx, ref ny);

                    batch.vtxArray[batch.vtxIdx++] = nx;
                    batch.vtxArray[batch.vtxIdx++] = ny;
                    batch.colArray[batch.colIdx++] = gradientColor.ToAbgr();
                    batch.dshArray[batch.dshIdx++] = 0;
                    batch.depArray[batch.depIdx++] = depth;
                }

                // Simple fan
                for (int i = 0; i < numVerts - 2; i++)
                {
                    batch.idxArray[batch.idxIdx++] = i0;
                    batch.idxArray[batch.idxIdx++] = (short)(i0 + i + 1);
                    batch.idxArray[batch.idxIdx++] = (short)(i0 + i + 2);
                }
            }
        }

        public void DrawText(string text, Font font, float x, float y, Color color, TextFlags flags = TextFlags.None, float width = 0, float height = 0, float clipMinX = 0, float clipMaxX = 0)
        {
            if (string.IsNullOrEmpty(text))
                return;

            Debug.Assert(!flags.HasFlag(TextFlags.Clip) || !flags.HasFlag(TextFlags.Ellipsis));
            Debug.Assert(!flags.HasFlag(TextFlags.Monospace) || !flags.HasFlag(TextFlags.Ellipsis));
            Debug.Assert(!flags.HasFlag(TextFlags.Monospace) || !flags.HasFlag(TextFlags.Clip));
            Debug.Assert(!flags.HasFlag(TextFlags.DropShadow) || !flags.HasFlag(TextFlags.Clip));
            Debug.Assert(!flags.HasFlag(TextFlags.Ellipsis) || width > 0);
            Debug.Assert((flags & TextFlags.HorizontalAlignMask) != TextFlags.Center || width  > 0);
            Debug.Assert((flags & TextFlags.VerticalAlignMask)   == TextFlags.Top    || height > 0);

            if (texts == null)
            {
                texts = new List<TextInstance>();
            }

            xform.TransformPoint(ref x, ref y);

            if ((flags & TextFlags.DropShadow) != 0)
            {
                Debug.Assert(clipMinX == 0 && clipMaxX == 0);

                var shadowInst = new TextInstance();
                shadowInst.layoutRect = new RectangleF(x + 1, y + 1, width, height);
                shadowInst.clipRect = shadowInst.layoutRect;
                shadowInst.flags = flags;
                shadowInst.text = text;
                shadowInst.font = font;
                shadowInst.color = Color.Black;
                shadowInst.depth = graphics.DepthValue;

                texts.Add(shadowInst);
            }

            var inst = new TextInstance();
            inst.layoutRect = new RectangleF(x, y, width, height);
            inst.flags = flags;
            inst.text = text;
            inst.font = font;
            inst.color = color;
            inst.depth = graphics.DepthValue;

            if (clipMaxX > clipMinX)
            {
                var dummy = 0.0f;
                xform.TransformPoint(ref clipMinX, ref dummy);
                xform.TransformPoint(ref clipMaxX, ref dummy);
                inst.clipRect = new RectangleF(clipMinX, y, clipMaxX - clipMinX, height); 
            }
            else
            {
                inst.clipRect =  inst.layoutRect;
            }

            texts.Add(inst);
        }

        public void DrawTexture(Texture bmp, float x, float y, Color tint = new Color())
        {
            Debug.Assert(Utils.Frac(x) == 0.0f && Utils.Frac(y) == 0.0f);
            DrawTexture(bmp, x, y, bmp.Size.Width, bmp.Size.Height, 0, 0, 1, 1, TextureFlags.Default, tint);
        }

        public void DrawTextureScaled(Texture bmp, float x, float y, float sx, float sy, bool flip)
        {
            if (flip)
            {
                DrawTexture(bmp, x, y, sx, sy, 0, 1, 1, 0);
            }
            else
            {
                DrawTexture(bmp, x, y, sx, sy, 0, 0, 1, 1);
            }
        }

        public void DrawTextureCentered(Texture bmp, float x, float y, float width, float height, Color tint = new Color())
        {
            x += (width  - bmp.Size.Width)  / 2;
            y += (height - bmp.Size.Height) / 2;
            DrawTexture(bmp, x, y, tint);
        }

        public void DrawTextureAtlas(TextureAtlasRef bmp, float x, float y, float scale = 1.0f, Color tint = new Color())
        {
            Debug.Assert(Utils.Frac(x) == 0.0f && Utils.Frac(y) == 0.0f);
            var atlas = bmp.Atlas;
            var elementIndex = bmp.ElementIndex;
            var elementSize = bmp.ElementSize;
            atlas.GetElementUVs(elementIndex, out var u0, out var v0, out var u1, out var v1);
            DrawTexture(atlas, x, y, elementSize.Width * scale, elementSize.Height * scale, u0, v0, u1, v1, TextureFlags.Default, tint);
        }

        public void DrawTextureAtlasCentered(TextureAtlasRef bmp, float x, float y, float width, float height, float scale = 1.0f, Color tint = new Color())
        {
            x += float.Floor((width  - bmp.ElementSize.Width  * scale) * 0.5f);
            y += float.Floor((height - bmp.ElementSize.Height * scale) * 0.5f);
            DrawTextureAtlas(bmp, x, y, scale, tint);
        }

        public void DrawTextureAtlasCentered(TextureAtlasRef bmp, Rectangle rect, float scale = 1.0f, Color tint = new Color())
        {
            float x = float.Floor(rect.Left + (rect.Width - bmp.ElementSize.Width * scale) * 0.5f);
            float y = float.Floor(rect.Top + (rect.Height - bmp.ElementSize.Height * scale) * 0.5f);
            DrawTextureAtlas(bmp, x, y, scale, tint);
        }

        public void DrawTexture(Texture bmp, float x, float y, float width, float height, float u0 = 0, float v0 = 0, float u1 = 1, float v1 = 1, TextureFlags flags = TextureFlags.Default, Color tint = new Color())
        {
            Debug.Assert(Utils.Frac(x) == 0.0f && Utils.Frac(y) == 0.0f);
            if (!textures.TryGetValue(bmp, out var list))
            {
                list = new List<TextureInstance>();
                textures.Add(bmp, list);
            }

            xform.TransformPoint(ref x, ref y);
            xform.ScaleSize(ref width, ref height);

            var inst = new TextureInstance();
            inst.x = x;
            inst.y = y;
            inst.sx = width;
            inst.sy = height;
            inst.tint = tint;
            inst.depth = graphics.DepthValue;

            if (bmp.IsAtlas && bmp.Filtering) 
            {
                // Prevent leaking from other images in the atlas.
                var halfPixelX = 0.5f / bmp.Size.Width;
                var halfPixelY = 0.5f / bmp.Size.Height;

                inst.u0 = u0 + halfPixelX;
                inst.v0 = v0 + halfPixelY;
                inst.u1 = u1 - halfPixelX;
                inst.v1 = v1 - halfPixelY;
            }
            else
            {
                inst.u0 = u0;
                inst.v0 = v0;
                inst.u1 = u1;
                inst.v1 = v1;
            }

            inst.flags = flags;

            list.Add(inst);
        }

        public List<PolyDrawData> GetPolygonDrawData()
        {
            var draws = (List<PolyDrawData>)null;

            if (polyBatches != null)
            {
                draws = new List<PolyDrawData>();

                foreach (var batch in polyBatches)
                {
                    var draw = new PolyDrawData();
                    draw.vtxArray = batch.vtxArray;
                    draw.colArray = batch.colArray;
                    draw.dshArray = batch.dshArray;
                    draw.idxArray = batch.idxArray;
                    draw.depArray = batch.depArray;
                    draw.numIndices = batch.idxIdx;
                    draw.vtxArraySize = batch.vtxIdx;
                    draw.colArraySize = batch.colIdx;
                    draw.dshArraySize = batch.dshIdx;
                    draw.idxArraySize = batch.idxIdx;
                    draw.depArraySize = batch.depIdx;
                    draws.Add(draw);
                }
            }

            return draws;
        }

        public List<LineSmoothDrawData> GetSmoothLineDrawData()
        {
            var draws = (List<LineSmoothDrawData>)null;

            if (lineSmoothBatches != null)
            {
                draws = new List<LineSmoothDrawData>();

                foreach (var batch in lineSmoothBatches)
                {
                    var draw = new LineSmoothDrawData();
                    draw.vtxArray = batch.vtxArray;
                    draw.dstArray = batch.dstArray;
                    draw.colArray = batch.colArray;
                    draw.idxArray = batch.idxArray;
                    draw.depArray = batch.depArray;
                    draw.numIndices = batch.idxIdx;
                    draw.vtxArraySize = batch.vtxIdx;
                    draw.dstArraySize = batch.dstIdx;
                    draw.colArraySize = batch.colIdx;
                    draw.idxArraySize = batch.idxIdx;
                    draw.depArraySize = batch.depIdx;
                    draws.Add(draw);
                }
            }

            return draws;
        }

        public List<LineDrawData> GetLineDrawData()
        {
            var draws = (List<LineDrawData>)null;

            if (lineBatches != null)
            {
                draws = new List<LineDrawData>();
                foreach (var batch in lineBatches)
                {
                    var draw = new LineDrawData();
                    draw.vtxArray = batch.vtxArray;
                    draw.dshArray = batch.dshArray;
                    draw.colArray = batch.colArray;
                    draw.depArray = batch.depArray;
                    draw.numVertices = batch.vtxIdx / 2;
                    draw.vtxArraySize = batch.vtxIdx;
                    draw.dshArraySize = batch.dshIdx;
                    draw.colArraySize = batch.colIdx;
                    draw.depArraySize = batch.depIdx;
                    draws.Add(draw);
                }
            }

            return draws;
        }

        public List<TextDrawData> GetTextDrawData()
        {
            var drawDatas = new List<TextDrawData>();
            var textEndIndex = 0;

            while (textEndIndex != texts.Count)
            {
                var textStartIndex = textEndIndex;
                var charCount = 0;
                var drawCallsPerTexture = new Dictionary<int, DrawData>();

                var drawData = new TextDrawData();
                drawData.vtxArray = graphics.GetVertexArray();
                drawData.texArray = graphics.GetTexCoordArray();
                drawData.colArray = graphics.GetColorArray();
                drawData.depArray = graphics.GetByteArray();
                drawDatas.Add(drawData);

                // First, count how many characters well need for each texture/drawdata.
                for (textEndIndex = textStartIndex; textEndIndex < texts.Count; textEndIndex++)
                {
                    var inst = texts[textEndIndex];

                    // We may have applied the ellipsis on last iteration.
                    if (inst.flags.HasFlag(TextFlags.Ellipsis) && (textEndIndex == 0 || textEndIndex != textStartIndex))
                    {
                        var mono = inst.flags.HasFlag(TextFlags.Monospace);
                        var ellipsisSizeX = inst.font.MeasureString("...", mono) * 4 / 3; // Leave some padding.
                        if (inst.font.TruncateString(ref inst.text, (int)(inst.layoutRect.Width - ellipsisSizeX)))
                            inst.text += "...";
                    }

                    // If we add this string, we will run out of vertices. Need to stop.
                    // This assumes the worst case, clipping may actually render less characters.
                    if (charCount + inst.text.Length > GraphicsBase.MaxVertexCount / 4)
                    {
                        break;
                    }

                    foreach (var c in inst.text)
                    {
                        var tex = inst.font.GetCharInfo(c).texture;
                        if (tex != 0)
                        {
                            if (!drawCallsPerTexture.TryGetValue(tex, out var d))
                            {
                                d = new DrawData() { textureId = tex };
                                drawCallsPerTexture.Add(tex, d);
                                drawData.drawCalls.Add(d);
                            }
                            d.count++;
                            charCount++;
                        }
                    }
                }

                // Setup the offset for each draw call. Temporarely, start/count are in # of char, not indices.
                // Again, this assumes the worst case, clipping may discard characters so they may be holes between draw calls.
                var orderedDrawData = drawCallsPerTexture.Values;
                var start = 0;

                foreach (var d in orderedDrawData)
                {
                    d.start = start;
                    start += d.count;
                    d.count = 0;
                }

                var draw = (DrawData)null;

                for (var textIndex = textStartIndex; textIndex < textEndIndex; textIndex++)
                {
                    var inst = texts[textIndex];
                    var font = inst.font;
                    var alignmentOffsetX = 0;
                    var alignmentOffsetY = font.OffsetY;
                    var mono = inst.flags.HasFlag(TextFlags.Monospace);
                    var halign = inst.flags & TextFlags.HorizontalAlignMask;
                    var valign = inst.flags & TextFlags.VerticalAlignMask;

                    if (halign != TextFlags.Left)
                    {
                        var minX = 0;
                        var maxX = font.MeasureString(inst.text, mono);

                        if (halign == TextFlags.Center)
                        {
                            alignmentOffsetX -= minX;
                            alignmentOffsetX += ((int)inst.layoutRect.Width - maxX - minX) / 2;
                        }
                        else if (halign == TextFlags.Right)
                        {
                            alignmentOffsetX -= minX;
                            alignmentOffsetX += ((int)inst.layoutRect.Width - maxX - minX);
                        }
                    }

                    if (valign != TextFlags.Top)
                    {
                        // Use a tall character with no descender as reference.
                        var charA = font.GetCharInfo('0');

                        // When aligning middle or center, ignore the y offset since it just
                        // adds extra padding and messes up calculations.
                        alignmentOffsetY = -charA.yoffset;

                        if (valign == TextFlags.Middle)
                        {
                            alignmentOffsetY += ((int)inst.layoutRect.Height - charA.height + 1) / 2;
                        }
                        else if (valign == TextFlags.Bottom)
                        {
                            alignmentOffsetY += ((int)inst.layoutRect.Height - charA.height);
                        }
                    }

                    var packedColor = inst.color.ToAbgr();
                    var numVertices = inst.text.Length * 4;

                    int x = (int)(inst.layoutRect.X + alignmentOffsetX);
                    int y = (int)(inst.layoutRect.Y + alignmentOffsetY);

                    if (inst.flags.HasFlag(TextFlags.Clip)) // Slow path when there is clipping.
                    {
                        var clipMinX = (int)(inst.clipRect.X);
                        var clipMaxX = (int)(inst.clipRect.X + inst.clipRect.Width);

                        for (int i = 0; i < inst.text.Length; i++)
                        {
                            var c0 = inst.text[i];
                            var info = font.GetCharInfo(c0);

                            if (info.texture != 0)
                            {
                                if (draw == null || draw.textureId != info.texture)
                                    draw = drawCallsPerTexture[info.texture];

                                var x0 = x + info.xoffset;
                                var y0 = y + info.yoffset;
                                var x1 = x0 + info.width;
                                var y1 = y0 + info.height;

                                if (x1 > clipMinX && x0 < clipMaxX)
                                {
                                    var u0 = info.u0;
                                    var v0 = info.v0;
                                    var u1 = info.u1;
                                    var v1 = info.v1;

                                    var newu0 = u0;
                                    var newu1 = u1;
                                    var newx0 = x0;
                                    var newx1 = x1;

                                    // Left clipping.
                                    if (x0 < clipMinX && x1 > clipMinX)
                                    {
                                        newu0 = Utils.Lerp(info.u0, info.u1, ((clipMinX - x0) / (float)(x1 - x0)));
                                        newx0 = clipMinX;
                                    }

                                    // Right clipping
                                    if (x0 < clipMaxX && x1 > clipMaxX)
                                    {
                                        newu1 = Utils.Lerp(info.u0, info.u1, ((clipMaxX - x0) / (float)(x1 - x0)));
                                        newx1 = clipMaxX;
                                    }

                                    u0 = newu0;
                                    u1 = newu1;
                                    x0 = newx0;
                                    x1 = newx1;

                                    var vtxIdx = (draw.start + draw.count) * 8;
                                    var texIdx = (draw.start + draw.count) * 8;
                                    var colIdx = (draw.start + draw.count) * 4;
                                    var depIdx = (draw.start + draw.count) * 4;

                                    drawData.vtxArray[vtxIdx++] = x0;
                                    drawData.vtxArray[vtxIdx++] = y0;
                                    drawData.vtxArray[vtxIdx++] = x1;
                                    drawData.vtxArray[vtxIdx++] = y0;
                                    drawData.vtxArray[vtxIdx++] = x1;
                                    drawData.vtxArray[vtxIdx++] = y1;
                                    drawData.vtxArray[vtxIdx++] = x0;
                                    drawData.vtxArray[vtxIdx++] = y1;

                                    drawData.texArray[texIdx++] = u0;
                                    drawData.texArray[texIdx++] = v0;
                                    drawData.texArray[texIdx++] = u1;
                                    drawData.texArray[texIdx++] = v0;
                                    drawData.texArray[texIdx++] = u1;
                                    drawData.texArray[texIdx++] = v1;
                                    drawData.texArray[texIdx++] = u0;
                                    drawData.texArray[texIdx++] = v1;

                                    drawData.colArray[colIdx++] = packedColor;
                                    drawData.colArray[colIdx++] = packedColor;
                                    drawData.colArray[colIdx++] = packedColor;
                                    drawData.colArray[colIdx++] = packedColor;

                                    drawData.depArray[depIdx++] = inst.depth;
                                    drawData.depArray[depIdx++] = inst.depth;
                                    drawData.depArray[depIdx++] = inst.depth;
                                    drawData.depArray[depIdx++] = inst.depth;

                                    draw.count++;
                                }
                            }

                            var advance = info.xadvance;
                            if (i != inst.text.Length - 1)
                            {
                                char c1 = inst.text[i + 1];
                                advance += font.GetKerning(c0, c1);
                            }
                            x += (int)advance;
                        }
                    }
                    else
                    {
                        for (int i = 0; i < inst.text.Length; i++)
                        {
                            var c0 = inst.text[i];
                            var isMonoChar = mono && Font.IsMonospaceChar(c0);
                            var info = font.GetCharInfo(c0);
                            var advanceInfo = isMonoChar ? font.GetCharInfo('0') : info;

                            if (info.texture != 0)
                            {
                                if (draw == null || draw.textureId != info.texture)
                                    draw = drawCallsPerTexture[info.texture];

                                var monoAjustX = isMonoChar ? (advanceInfo.width - info.width + 1) / 2 : 0;

                                var x0 = x + info.xoffset + monoAjustX;
                                var y0 = y + info.yoffset;
                                var x1 = x0 + info.width;
                                var y1 = y0 + info.height;

                                var vtxIdx = (draw.start + draw.count) * 8;
                                var texIdx = (draw.start + draw.count) * 8;
                                var colIdx = (draw.start + draw.count) * 4;
                                var depIdx = (draw.start + draw.count) * 4;

                                drawData.vtxArray[vtxIdx++] = x0;
                                drawData.vtxArray[vtxIdx++] = y0;
                                drawData.vtxArray[vtxIdx++] = x1;
                                drawData.vtxArray[vtxIdx++] = y0;
                                drawData.vtxArray[vtxIdx++] = x1;
                                drawData.vtxArray[vtxIdx++] = y1;
                                drawData.vtxArray[vtxIdx++] = x0;
                                drawData.vtxArray[vtxIdx++] = y1;

                                drawData.texArray[texIdx++] = info.u0;
                                drawData.texArray[texIdx++] = info.v0;
                                drawData.texArray[texIdx++] = info.u1;
                                drawData.texArray[texIdx++] = info.v0;
                                drawData.texArray[texIdx++] = info.u1;
                                drawData.texArray[texIdx++] = info.v1;
                                drawData.texArray[texIdx++] = info.u0;
                                drawData.texArray[texIdx++] = info.v1;

                                drawData.colArray[colIdx++] = packedColor;
                                drawData.colArray[colIdx++] = packedColor;
                                drawData.colArray[colIdx++] = packedColor;
                                drawData.colArray[colIdx++] = packedColor;

                                drawData.depArray[depIdx++] = inst.depth;
                                drawData.depArray[depIdx++] = inst.depth;
                                drawData.depArray[depIdx++] = inst.depth;
                                drawData.depArray[depIdx++] = inst.depth;

                                draw.count++;
                            }

                            var advance = advanceInfo.xadvance;
                            if (i != inst.text.Length - 1 && !isMonoChar)
                            {
                                char c1 = inst.text[i + 1];
                                advance += font.GetKerning(c0, c1);
                            }
                            x += (int)advance;
                        }
                    }
                }

                // # of chars -> # of indices
                var maxCount = 0;
                foreach (var d in orderedDrawData)
                {
                    maxCount = Math.Max(maxCount, d.start + d.count);
                    d.count *= 6;
                    d.start *= 6;
                }

                drawData.vtxArraySize = maxCount * 8;
                drawData.texArraySize = maxCount * 8;
                drawData.colArraySize = maxCount * 4;
                drawData.depArraySize = maxCount * 4;
            }

            return drawDatas;
        }

        public List<TextureDrawData> GetTextureDrawData()
        {
            var drawDatas = new List<TextureDrawData>();
            var drawData = new TextureDrawData();

            drawData.vtxArray = graphics.GetVertexArray();
            drawData.texArray = graphics.GetTexCoordArray();
            drawData.colArray = graphics.GetColorArray();
            drawData.depArray = graphics.GetByteArray();

            var idx = 0;

            foreach (var kv in textures)
            {
                var bmp = kv.Key;
                var list = kv.Value;
                var draw = new DrawData();

                draw.textureId = bmp.Id;
                draw.start = idx;
                
                foreach (var inst in list)
                {
                    // We are about to overflow, start a new batch.
                    if (drawData.texArraySize + 12 >= drawData.texArray.Length)
                    {
                        if (draw.count > 0)
                            drawData.drawCalls.Add(draw);

                        drawDatas.Add(drawData);
                        drawData = new TextureDrawData();
                        drawData.vtxArray = graphics.GetVertexArray();
                        drawData.texArray = graphics.GetTexCoordArray();
                        drawData.colArray = graphics.GetColorArray();
                        drawData.depArray = graphics.GetByteArray();
                        idx = 0;
                        draw = new DrawData();
                        draw.textureId = bmp.Id;
                        draw.start = 0;

                    }

                    var x0 = inst.x;
                    var y0 = inst.y;
                    var x1 = inst.x + inst.sx;
                    var y1 = inst.y + inst.sy;
                    var tint = inst.tint != Color.Empty ? inst.tint : Color.White;
                    var rotated = inst.flags.HasFlag(TextureFlags.Rotated90);
                    var perspective = inst.flags.HasFlag(TextureFlags.Perspective);

                    if (!perspective)
                    {
                        drawData.vtxArray[drawData.vtxArraySize++] = x0;
                        drawData.vtxArray[drawData.vtxArraySize++] = y0;
                        drawData.vtxArray[drawData.vtxArraySize++] = x1;
                        drawData.vtxArray[drawData.vtxArraySize++] = y0;
                        drawData.vtxArray[drawData.vtxArraySize++] = x1;
                        drawData.vtxArray[drawData.vtxArraySize++] = y1;
                        drawData.vtxArray[drawData.vtxArraySize++] = x0;
                        drawData.vtxArray[drawData.vtxArraySize++] = y1;

                        if (!rotated)
                        {
                            drawData.texArray[drawData.texArraySize++] = inst.u0;
                            drawData.texArray[drawData.texArraySize++] = inst.v0;
                            drawData.texArray[drawData.texArraySize++] = 1.0f;
                            drawData.texArray[drawData.texArraySize++] = inst.u1;
                            drawData.texArray[drawData.texArraySize++] = inst.v0;
                            drawData.texArray[drawData.texArraySize++] = 1.0f;
                            drawData.texArray[drawData.texArraySize++] = inst.u1;
                            drawData.texArray[drawData.texArraySize++] = inst.v1;
                            drawData.texArray[drawData.texArraySize++] = 1.0f;
                            drawData.texArray[drawData.texArraySize++] = inst.u0;
                            drawData.texArray[drawData.texArraySize++] = inst.v1;
                            drawData.texArray[drawData.texArraySize++] = 1.0f;
                        }
                        else
                        {
                            // 90 degrees UV rotation.
                            drawData.texArray[drawData.texArraySize++] = inst.u1;
                            drawData.texArray[drawData.texArraySize++] = inst.v0;
                            drawData.texArray[drawData.texArraySize++] = 1.0f;
                            drawData.texArray[drawData.texArraySize++] = inst.u1;
                            drawData.texArray[drawData.texArraySize++] = inst.v1;
                            drawData.texArray[drawData.texArraySize++] = 1.0f;
                            drawData.texArray[drawData.texArraySize++] = inst.u0;
                            drawData.texArray[drawData.texArraySize++] = inst.v1;
                            drawData.texArray[drawData.texArraySize++] = 1.0f;
                            drawData.texArray[drawData.texArraySize++] = inst.u0;
                            drawData.texArray[drawData.texArraySize++] = inst.v0;
                            drawData.texArray[drawData.texArraySize++] = 1.0f;
                        }
                    }
                    else 
                    {
                        var tx = graphics.ScreenWidth / (float)inst.sx;

                        // Perspective mode basically ignores the UVs and assumes (0,0) ... (1,1).
                        if (!rotated)
                        {
                            drawData.texArray[drawData.texArraySize++] = 0.0f;
                            drawData.texArray[drawData.texArraySize++] = 0.0f;
                            drawData.texArray[drawData.texArraySize++] = tx;
                            drawData.texArray[drawData.texArraySize++] = tx;
                            drawData.texArray[drawData.texArraySize++] = 0.0f;
                            drawData.texArray[drawData.texArraySize++] = tx;
                            drawData.texArray[drawData.texArraySize++] = 1.0f;
                            drawData.texArray[drawData.texArraySize++] = 1.0f;
                            drawData.texArray[drawData.texArraySize++] = 1.0f;
                            drawData.texArray[drawData.texArraySize++] = 0.0f;
                            drawData.texArray[drawData.texArraySize++] = 1.0f;
                            drawData.texArray[drawData.texArraySize++] = 1.0f;
                        }
                        else
                        {
                            drawData.texArray[drawData.texArraySize++] = tx;
                            drawData.texArray[drawData.texArraySize++] = 0.0f;
                            drawData.texArray[drawData.texArraySize++] = tx;
                            drawData.texArray[drawData.texArraySize++] = tx;
                            drawData.texArray[drawData.texArraySize++] = tx;
                            drawData.texArray[drawData.texArraySize++] = tx;
                            drawData.texArray[drawData.texArraySize++] = 0.0f;
                            drawData.texArray[drawData.texArraySize++] = 1.0f;
                            drawData.texArray[drawData.texArraySize++] = 1.0f;
                            drawData.texArray[drawData.texArraySize++] = 0.0f;
                            drawData.texArray[drawData.texArraySize++] = 0.0f;
                            drawData.texArray[drawData.texArraySize++] = 1.0f;
                        }

                        // Asumes everything is fullscreen.
                        drawData.vtxArray[drawData.vtxArraySize++] = 0;
                        drawData.vtxArray[drawData.vtxArraySize++] = 0;
                        drawData.vtxArray[drawData.vtxArraySize++] = graphics.ScreenWidth;
                        drawData.vtxArray[drawData.vtxArraySize++] = 0;
                        drawData.vtxArray[drawData.vtxArraySize++] = x1;
                        drawData.vtxArray[drawData.vtxArraySize++] = y1;
                        drawData.vtxArray[drawData.vtxArraySize++] = x0;
                        drawData.vtxArray[drawData.vtxArraySize++] = y1;
                    }
                    
                    drawData.colArray[drawData.colArraySize++] = tint.ToAbgr();
                    drawData.colArray[drawData.colArraySize++] = tint.ToAbgr();
                    drawData.colArray[drawData.colArraySize++] = tint.ToAbgr();
                    drawData.colArray[drawData.colArraySize++] = tint.ToAbgr();

                    drawData.depArray[drawData.depArraySize++] = inst.depth;
                    drawData.depArray[drawData.depArraySize++] = inst.depth;
                    drawData.depArray[drawData.depArraySize++] = inst.depth;
                    drawData.depArray[drawData.depArraySize++] = inst.depth;

                    draw.count += 6;
                    idx += 6;
                }

                drawData.drawCalls.Add(draw);
            }

            drawDatas.Add(drawData);

            return drawDatas;
        }
    }
}
