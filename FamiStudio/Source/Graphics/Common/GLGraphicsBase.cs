using System;
using System.Drawing;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace FamiStudio
{
    public abstract class GraphicsBase : IDisposable
    {
        protected struct GradientCacheKey
        {
            public Color color0;
            public Color color1;
            public int size;

            public override int GetHashCode()
            {
                return Utils.HashCombine(Utils.HashCombine(color0.ToArgb(), color1.ToArgb()), size);
            }
        }

        protected float windowScaling = 1.0f;
        protected float fontScaling = 1.0f;
        protected int windowSizeY;
        protected int lineWidthBias;
        protected int maxSmoothLineWidth = int.MaxValue;
        protected Rectangle controlRect;
        protected Rectangle controlRectFlip;
        protected TransformStack transform = new TransformStack();
        protected Bitmap dashedBitmap;
        protected Dictionary<int, BitmapAtlas> atlases = new Dictionary<int, BitmapAtlas>();

        protected Dictionary<GradientCacheKey, Brush> verticalGradientCache = new Dictionary<GradientCacheKey, Brush>();
        protected Dictionary<GradientCacheKey, Brush> horizontalGradientCache = new Dictionary<GradientCacheKey, Brush>();
        protected Dictionary<Color, Brush> solidGradientCache = new Dictionary<Color, Brush>();

        public float FontScaling => fontScaling;
        public float WindowScaling => windowScaling;
        public int DashTextureSize => dashedBitmap.Size.Width;
        public int WindowSizeY => windowSizeY;
        public TransformStack Transform => transform;

        protected const int MaxAtlasResolution = 1024;
        protected const int MaxVertexCount = 128 * 1024;
        protected const int MaxIndexCount = MaxVertexCount / 4 * 6;

        protected float[] vtxArray = new float[MaxVertexCount * 2];
        protected float[] texArray = new float[MaxVertexCount * 2];
        protected int[]   colArray = new int[MaxVertexCount];
        protected short[] quadIdxArray = new short[MaxIndexCount];

        protected List<float[]> freeVtxArrays = new List<float[]>();
        protected List<int[]>   freeColArrays = new List<int[]>();
        protected List<short[]> freeIdxArrays = new List<short[]>();

        protected abstract int CreateEmptyTexture(int width, int height, bool alpha = true, bool filter = false);
        protected abstract int CreateTexture(int[,] bmpData, bool filter);
        public abstract void DeleteTexture(int id);
        public abstract void DrawCommandList(CommandList list, Rectangle scissor);
        public abstract BitmapAtlas CreateBitmapAtlasFromResources(string[] names);

        protected GraphicsBase(float mainScale, float fontScale)
        {
            windowScaling = mainScale;
            fontScaling = fontScale;

            // Quad index buffer.
            // TODO : On PC, we have GL_QUADS, we could get rid of this.
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

            BuildBitmapAtlases();
        }

        public virtual void BeginDrawFrame()
        {
        }

        public virtual void EndDrawFrame()
        {
        }

        public virtual void BeginDrawControl(Rectangle unflippedControlRect, int windowSizeY)
        {
            this.windowSizeY = windowSizeY;

            lineWidthBias = 0;
            controlRect = unflippedControlRect;
            controlRectFlip = FlipRectangleY(unflippedControlRect);
            transform.SetIdentity();
        }

        public virtual void EndDrawControl()
        {
        }

        protected string GetScaledFilename(string name, out bool needsScaling)
        {
            var assembly = Assembly.GetExecutingAssembly();

            if (windowScaling == 1.5f && assembly.GetManifestResourceInfo($"FamiStudio.Resources.{name}@15x.tga") != null)
            {
                needsScaling = false;
                return $"FamiStudio.Resources.{name}@15x.tga";
            }
            else if (windowScaling > 1.0f && assembly.GetManifestResourceInfo($"FamiStudio.Resources.{name}@2x.tga") != null)
            {
                needsScaling = windowScaling != 2.0f;
                return $"FamiStudio.Resources.{name}@2x.tga";
            }
            else
            {
                needsScaling = false;
                return $"FamiStudio.Resources.{name}.tga";
            }
        }

        protected int[,] LoadBitmapFromResourceWithScaling(string name)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var scaledFilename = GetScaledFilename(name, out var needsScaling);
            var bmpData = TgaFile.LoadFromResource(scaledFilename);

            // Pre-resize all images so we dont have to deal with scaling later.
            if (needsScaling)
            {
                var newWidth  = Math.Max(1, (int)(bmpData.GetLength(1) * (windowScaling / 2.0f)));
                var newHeight = Math.Max(1, (int)(bmpData.GetLength(0) * (windowScaling / 2.0f)));

                bmpData = Utils.ResizeBitmap(bmpData, newWidth, newHeight);
            }

            return bmpData;
        }

        private void BuildBitmapAtlases()
        {
            // Build atlases.
            var assembly = Assembly.GetExecutingAssembly();
            var resourceNames = assembly.GetManifestResourceNames();
            var atlasImages = new Dictionary<int, List<string>>();
            var filteredImages = new HashSet<string>();

            foreach (var res in resourceNames)
            {
                // Ignore fonts (which have a '_' in their name).
                if (res.StartsWith("FamiStudio.Resources.") && res.EndsWith(".tga") && res.IndexOf('_') < 0)
                {
                    // Remove any scaling from the name.
                    var at = res.IndexOf('@');
                    var cleanedFilename = res.Substring(21, at >= 0 ? at - 21 : res.Length - 25);
                    filteredImages.Add(cleanedFilename);
                }
            }

            // Keep 1 atlas per power-of-two size. 
            var minWidth = (int)(16 * windowScaling);

            foreach (var res in filteredImages)
            {
                var scaledFilename = GetScaledFilename(res, out var needsScaling);
                TgaFile.GetResourceImageSize(scaledFilename, out var width, out var height);

                if (needsScaling)
                {
                    width  = Math.Max(1, (int)(width  * (windowScaling / 2.0f)));
                    height = Math.Max(1, (int)(height * (windowScaling / 2.0f)));
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
                var bmp = CreateBitmapAtlasFromResources(kv.Value.ToArray());
                atlases.Add(kv.Key, bmp);
            }
        }

        public BitmapAtlasRef GetBitmapAtlasRef(string name)
        {
            // Look in all atlases
            foreach (var a in atlases.Values)
            {
                var idx = a.GetElementIndex(name);
                if (idx >= 0)
                    return new BitmapAtlasRef(a, idx);
            }

            Debug.Assert(false);
            return null;
        }

        public BitmapAtlasRef[] GetBitmapAtlasRefs(string[] name)
        {
            var refs = new BitmapAtlasRef[name.Length];
            for (int i = 0; i < refs.Length; i++)
                refs[i] = GetBitmapAtlasRef(name[i]);
            return refs;
        }

        public void SetLineBias(int bias)
        {
            lineWidthBias = bias;
        }

        public void DrawCommandList(CommandList list)
        {
            DrawCommandList(list, Rectangle.Empty);
        }

        public virtual CommandList CreateCommandList()
        {
            return new CommandList(this, dashedBitmap.Size.Width, lineWidthBias, true, maxSmoothLineWidth);
        }

        protected Rectangle FlipRectangleY(Rectangle rc)
        {
            return new Rectangle(rc.Left, windowSizeY - rc.Top - rc.Height, rc.Width, rc.Height);
        }

        public float MeasureString(string text, Font font, bool mono = false)
        {
            return font.MeasureString(text, mono);
        }

        public Geometry CreateGeometry(float[,] points, bool closed = true)
        {
            return new Geometry(points, closed);
        }

        public Bitmap CreateEmptyBitmap(int width, int height, bool alpha = true, bool filter = false)
        {
            return new Bitmap(this, CreateEmptyTexture(width, height, alpha, filter), width, height, true, filter);
        }

        public Brush CreateSolidBrush(Color color)
        {
            return new Brush(color);
        }

        public Brush CreateHorizontalGradientBrush(float x0, float x1, Color color0, Color color1)
        {
            Debug.Assert(x0 == 0.0f);
            return new Brush(color0, color1, x1 - x0, 0.0f);
        }

        public Brush CreateVerticalGradientBrush(float y0, float y1, Color color0, Color color1)
        {
            Debug.Assert(y0 == 0.0f);
            return new Brush(color0, color1, 0.0f, y1 - y0);
        }

        public Brush GetSolidBrush(Color color, float dimming = 1.0f, float alphaDimming = 1.0f)
        {
            if (dimming != 1.0f || alphaDimming != 1.0f)
            {
                color = Color.FromArgb(
                    (int)(color.A * alphaDimming),
                    (int)(color.R * dimming),
                    (int)(color.G * dimming),
                    (int)(color.B * dimming));
            }

            if (solidGradientCache.TryGetValue(color, out var brush))
                return brush;

            brush = new Brush(color);
            solidGradientCache[color] = brush;

            return brush;
        }

        public Brush GetVerticalGradientBrush(Color color0, int sizeY, float dimming)
        {
            Color color1 = Color.FromArgb(
                (int)(color0.A),
                (int)(color0.R * dimming),
                (int)(color0.G * dimming),
                (int)(color0.B * dimming));

            return GetVerticalGradientBrush(color0, color1, sizeY);
        }

        public Brush GetVerticalGradientBrush(Color color0, Color color1, int sizeY)
        {
            var key = new GradientCacheKey() { color0 = color0, color1 = color1, size = sizeY };

            if (verticalGradientCache.TryGetValue(key, out var brush))
                return brush;

            brush = CreateVerticalGradientBrush(0, sizeY, color0, color1);
            verticalGradientCache[key] = brush;

            return brush;
        }

        public Brush GetHorizontalGradientBrush(Color color0, int sizeY, float dimming)
        {
            Color color1 = Color.FromArgb(
                (int)(color0.A),
                (int)(color0.R * dimming),
                (int)(color0.G * dimming),
                (int)(color0.B * dimming));

            return GetHorizontalGradientBrush(color0, color1, sizeY);
        }

        public Brush GetHorizontalGradientBrush(Color color0, Color color1, int sizeY)
        {
            var key = new GradientCacheKey() { color0 = color0, color1 = color1, size = sizeY };

            if (horizontalGradientCache.TryGetValue(key, out var brush))
                return brush;

            brush = CreateHorizontalGradientBrush(0, sizeY, color0, color1);
            horizontalGradientCache[key] = brush;

            return brush;
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

        public Font CreateScaledFont(Font source, int desiredHeight)
        {
            return null;
        }

        public Font CreateFontFromResource(string name, bool bold, int size)
        {
            var suffix = bold ? "Bold" : "";
            var basename = $"{name}{size}{suffix}";
            var fntfile = $"FamiStudio.Resources.{basename}.fnt";
            var imgfile = $"FamiStudio.Resources.{basename}_0.tga";

            var str = "";
            using (Stream stream = typeof(GraphicsBase).Assembly.GetManifestResourceStream(fntfile))
            using (StreamReader reader = new StreamReader(stream))
            {
                str = reader.ReadToEnd();
            }

            var lines = str.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var bmpData = TgaFile.LoadFromResource(imgfile);

            var font = (Font)null;

            int baseValue = 0;
            int lineHeight = 0;
            int texSizeX = 256;
            int texSizeY = 256;

            foreach (var line in lines)
            {
                var splits = line.Split(new[] { ' ', '=', '\"' }, StringSplitOptions.RemoveEmptyEntries);

                switch (splits[0])
                {
                    case "common":
                    {
                        baseValue = ReadFontParam<int>(splits, "base");
                        lineHeight = ReadFontParam<int>(splits, "lineHeight");
                        texSizeX = ReadFontParam<int>(splits, "scaleW");
                        texSizeY = ReadFontParam<int>(splits, "scaleH");
                        font = new Font(this, CreateTexture(bmpData, false), size, baseValue, lineHeight);
                        break;
                    }
                    case "char":
                    {
                        var charInfo = new Font.CharInfo();

                        int c = ReadFontParam<int>(splits, "id");
                        int x = ReadFontParam<int>(splits, "x");
                        int y = ReadFontParam<int>(splits, "y");

                        charInfo.width = ReadFontParam<int>(splits, "width");
                        charInfo.height = ReadFontParam<int>(splits, "height");
                        charInfo.xoffset = ReadFontParam<int>(splits, "xoffset");
                        charInfo.yoffset = ReadFontParam<int>(splits, "yoffset");
                        charInfo.xadvance = ReadFontParam<int>(splits, "xadvance");
                        charInfo.u0 = (x + 0.0f) / (float)texSizeX;
                        charInfo.v0 = (y + 0.0f) / (float)texSizeY;
                        charInfo.u1 = (x + 0.0f + charInfo.width) / (float)texSizeX;
                        charInfo.v1 = (y + 0.0f + charInfo.height) / (float)texSizeY;

                        font.AddChar((char)c, charInfo);

                        break;
                    }
                    case "kerning":
                    {
                        int c0 = ReadFontParam<int>(splits, "first");
                        int c1 = ReadFontParam<int>(splits, "second");
                        int amount = ReadFontParam<int>(splits, "amount");
                        font.AddKerningPair(c0, c1, amount);
                        break;
                    }
                }
            }

            return font;
        }

        public virtual void Dispose()
        {
            foreach (var b in verticalGradientCache.Values)
                b.Dispose();
            foreach (var b in horizontalGradientCache.Values)
                b.Dispose();
            foreach (var b in solidGradientCache.Values)
                b.Dispose();
            foreach (var a in atlases.Values)
                a.Dispose();

            verticalGradientCache.Clear();
            horizontalGradientCache.Clear();
            solidGradientCache.Clear();
            atlases.Clear();
        }

        public float[] GetVertexArray()
        {
            if (freeVtxArrays.Count > 0)
            {
                var lastIdx = freeVtxArrays.Count - 1;
                var arr = freeVtxArrays[lastIdx];
                freeVtxArrays.RemoveAt(lastIdx);
                return arr;
            }
            else
            {
                return new float[MaxVertexCount * 2];
            }
        }

        public int[] GetColorArray()
        {
            if (freeColArrays.Count > 0)
            {
                var lastIdx = freeColArrays.Count - 1;
                var arr = freeColArrays[lastIdx];
                freeColArrays.RemoveAt(lastIdx);
                return arr;
            }
            else
            {
                return new int[MaxVertexCount];
            }
        }

        public short[] GetIndexArray()
        {
            if (freeIdxArrays.Count > 0)
            {
                var lastIdx = freeIdxArrays.Count - 1;
                var arr = freeIdxArrays[lastIdx];
                freeIdxArrays.RemoveAt(lastIdx);
                return arr;
            }
            else
            {
                return new short[MaxVertexCount];
            }
        }

        public void ReleaseVertexArray(float[] a)
        {
            freeVtxArrays.Add(a);
        }

        public void ReleaseColorArray(int[] a)
        {
            freeColArrays.Add(a);
        }

        public void ReleaseIndexArray(short[] a)
        {
            freeIdxArrays.Add(a);
        }
    };

    public class Font : IDisposable
    {
        public class CharInfo
        {
            public int width;
            public int height;
            public int xoffset;
            public int yoffset;
            public int xadvance;
            public float u0;
            public float v0;
            public float u1;
            public float v1;
        }

        Dictionary<char, CharInfo> charMap = new Dictionary<char, CharInfo>();
        Dictionary<int, int> kerningPairs = new Dictionary<int, int>();

        private int texture;
        private int size;
        private int baseValue;
        private int lineHeight;
        private GraphicsBase graphics;

        public int Texture => texture;
        public int Size => size;
        public int LineHeight => lineHeight;
        public int OffsetY => size - baseValue;

        public Font(GraphicsBase g, int tex, int sz, int b, int l)
        {
            graphics = g;
            texture = tex;
            size = sz;
            baseValue = b;
            lineHeight = l;
        }

        public void Dispose()
        {
            graphics.DeleteTexture(texture);
            texture = -1;
        }

        public void AddChar(char c, CharInfo info)
        {
            charMap[c] = info;
        }

        public void AddKerningPair(int c0, int c1, int amount)
        {
            kerningPairs[c0 | (c1 << 8)] = amount;
        }

        public CharInfo GetCharInfo(char c, bool fallback = true)
        {
            if (charMap.TryGetValue(c, out CharInfo info))
            {
                return info;
            }
            else if (fallback)
            {
                return charMap['?'];
            }
            else
            {
                return null;
            }
        }

        public int GetKerning(char c0, char c1)
        {
            int key = (int)c0 | ((int)c1 << 8);
            return kerningPairs.TryGetValue(key, out int amount) ? amount : 0;
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
                    text = text.Substring(0, i);
                    return true;
                }

                x += info.xadvance;
                if (i != text.Length - 1)
                {
                    char c1 = text[i + 1];
                    x += GetKerning(c0, c1);
                }
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

                x += info.xadvance;
                if (i != text.Length - 1)
                {
                    char c1 = text[i + 1];
                    x += GetKerning(c0, c1);
                }
            }

            return text.Length;
        }


        public int MeasureString(string text, bool mono)
        {
            int x = 0;

            if (mono)
            {
                var info = GetCharInfo('0');

                for (int i = 0; i < text.Length; i++)
                {
                    x += info.xadvance;
                }
            }
            else
            {
                for (int i = 0; i < text.Length; i++)
                {
                    var c0 = text[i];
                    var info = GetCharInfo(c0);

                    x += info.xadvance;
                    if (i != text.Length - 1)
                    {
                        char c1 = text[i + 1];
                        x += GetKerning(c0, c1);
                    }
                }
            }

            return x;
        }

        /*
        public void MeasureString(string text, bool mono, out int minX, out int maxX)
        {
            minX = 0;
            maxX = 0;

            int x = 0;

            if (mono)
            {
                var info = GetCharInfo('0');

                for (int i = 0; i < text.Length; i++)
                {
                    var c0 = text[i];
                    int x0 = x + info.xoffset;
                    int x1 = x0 + info.width;

                    minX = Math.Min(minX, x0);
                    maxX = Math.Max(maxX, x1);

                    x += info.xadvance;
                }
            }
            else
            {
                for (int i = 0; i < text.Length; i++)
                {
                    var c0 = text[i];
                    var info = GetCharInfo(c0);

                    int x0 = x + info.xoffset;
                    int x1 = x0 + info.width;

                    minX = Math.Min(minX, x0);
                    maxX = Math.Max(maxX, x1);

                    x += info.xadvance;
                    if (i != text.Length - 1)
                    {
                        char c1 = text[i + 1];
                        x += GetKerning(c0, c1);
                    }
                }
            }
        }

        public int MeasureString(string text, bool mono)
        {
            MeasureString(text, mono, out var minX, out var maxX);
            return maxX - minX;
        }
        */
    }

    public class Geometry : IDisposable
    {
        public float[] Points { get; private set; }

        public Dictionary<float, float[]> miterPoints = new Dictionary<float, float[]>();

        public Geometry(float[,] points, bool closed)
        {
            var numPoints = points.GetLength(0);
            var closedPoints = new float[(numPoints + (closed ? 1 : 0)) * 2];

            for (int i = 0; i < closedPoints.Length / 2; i++)
            {
                closedPoints[i * 2 + 0] = points[i % points.GetLength(0), 0];
                closedPoints[i * 2 + 1] = points[i % points.GetLength(0), 1];
            }

            Points = closedPoints;
        }

        public float[] GetMiterPoints(float lineWidth)
        {
            if (miterPoints.TryGetValue(lineWidth, out var points))
                return points;

            points = new float[Points.Length * 2];

            for (int i = 0; i < Points.Length / 2 - 1; i++)
            {
                var x0 = Points[(i + 0) * 2 + 0];
                var x1 = Points[(i + 1) * 2 + 0];
                var y0 = Points[(i + 0) * 2 + 1];
                var y1 = Points[(i + 1) * 2 + 1];

                var dx = x1 - x0;
                var dy = y1 - y0;
                var len = (float)Math.Sqrt(dx * dx + dy * dy);

                var nx = dx / len * lineWidth * 0.5f;
                var ny = dy / len * lineWidth * 0.5f;

                points[(2 * i + 0) * 2 + 0] = x0 - nx;
                points[(2 * i + 0) * 2 + 1] = y0 - ny;
                points[(2 * i + 1) * 2 + 0] = x1 + nx;
                points[(2 * i + 1) * 2 + 1] = y1 + ny;
            }

            miterPoints.Add(lineWidth, points);

            return points;
        }

        public void Dispose()
        {
        }
    }

    public class Brush : IDisposable
    {
        public float GradientSizeX = 0.0f;
        public float GradientSizeY = 0.0f;
        public Color Color0;
        public Color Color1;
        public int PackedColor0;
        public int PackedColor1;

        public Brush(Color color)
        {
            Color0 = color;
            PackedColor0 = ColorUtils.PackColor(color);
        }

        public Brush(Color color0, Color color1, float sizeX, float sizeY)
        {
            Color0 = color0;
            Color1 = color1;
            PackedColor0 = ColorUtils.PackColor(color0);
            PackedColor1 = ColorUtils.PackColor(color1);
            GradientSizeX = sizeX;
            GradientSizeY = sizeY;
        }

        public bool IsGradient => GradientSizeX != 0 || GradientSizeY != 0;

        public void Dispose()
        {
        }
    }

    public class Bitmap : IDisposable
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

        public Bitmap(GraphicsBase g, int id, int width, int height, bool disp = true, bool filter = false)
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

    public class BitmapAtlas : Bitmap
    {
        private string[] elementNames;
        private Rectangle[] elementRects;

        public Size GetElementSize(int index) => elementRects[index].Size;

        public BitmapAtlas(GraphicsBase g, int id, int atlasSizeX, int atlasSizeY, string[] names, Rectangle[] rects, bool filter = false) :
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

    public class BitmapAtlasRef
    {
        private BitmapAtlas atlas;
        private int index;

        public BitmapAtlas Atlas => atlas;
        public int ElementIndex => index;
        public Size ElementSize => atlas.GetElementSize(index);

        public BitmapAtlasRef(BitmapAtlas a, int idx)
        {
            atlas = a;
            index = idx;
        }

        public void GetElementUVs(out float u0, out float v0, out float u1, out float v1)
        {
            atlas.GetElementUVs(index, out u0, out v0, out u1, out v1);
        }
    }

    public static class ColorUtils
    {
        public static int PackColor(Color c)
        {
            return (c.A << 24) | (c.B << 16) | (c.G << 8) | c.R;
        }

        public static int PackColor(int r, int g, int b, int a)
        {
            return (a << 24) | (b << 16) | (g << 8) | r;
        }

        public static int PackColorForTexture(Color c)
        {
#if FAMISTUDIO_ANDROID
            return (c.A << 24) | (c.B << 16) | (c.G << 8) | c.R;
#else
            return c.ToArgb();
#endif
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
        private Transform transform = new Transform(); // xy = scale, zw = translation
        private Stack<Transform> transformStack = new Stack<Transform>();

        public bool HasScaling => transform.HasScaling;

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

        Clip      = 1 << 7,
        Ellipsis  = 1 << 8,
        Monospace = 1 << 9
    }

    // This is common to both OGL, it only does data packing, no GL calls.
    public class CommandList
    {
        private class MeshBatch
        {
            public float[] vtxArray;
            public int[]   colArray;
            public short[] idxArray;

            public int vtxIdx = 0;
            public int colIdx = 0;
            public int idxIdx = 0;
        };

        private class LineBatch
        {
            public bool smooth;
            public float lineWidth;

            public float[] vtxArray;
            public float[] texArray;
            public int[]   colArray;

            public int vtxIdx = 0;
            public int texIdx = 0;
            public int colIdx = 0;
        };

        private class TextInstance
        {
            public RectangleF layoutRect;
            public RectangleF clipRect;
            public TextFlags flags;
            public string text;
            public Brush brush;
        };

        private class BitmapInstance
        {
            public float x;
            public float y;
            public float sx;
            public float sy;
            public float u0;
            public float v0;
            public float u1;
            public float v1;
            public float opacity;
            public Color tint;
            public bool rotated;
        }

        public class MeshDrawData
        {
            public float[] vtxArray;
            public int[]   colArray;
            public short[] idxArray;

            public int vtxArraySize;
            public int colArraySize;
            public int idxArraySize;

            public bool smooth;
            public int numIndices;
        };

        public class LineDrawData
        {
            public float[] vtxArray;
            public float[] texArray;
            public int[]   colArray;

            public int vtxArraySize;
            public int texArraySize;
            public int colArraySize;

            public int numVertices;
            public bool smooth;
            public float lineWidth;
        };

        public class DrawData
        {
            public int textureId;
            public int start;
            public int count;
        };

#if FAMISTUDIO_LINUX
        private bool drawThickLineAsPolygon;
        private MeshBatch thickLineBatch; // Linux only
        public bool HasAnyTickLineMeshes => thickLineBatch != null;
#else
        public bool HasAnyTickLineMeshes => false;
#endif

        private int maxSmoothLineWidth = int.MaxValue;
        private int lineWidthBias;
        private float invDashTextureSize;
        private MeshBatch meshBatch;
        private MeshBatch meshSmoothBatch;
        private LineBatch currentLineBatch;
        private List<LineBatch> lineBatches = new List<LineBatch>();
        private Dictionary<Font,   List<TextInstance>>   texts   = new Dictionary<Font,   List<TextInstance>>();
        private Dictionary<Bitmap, List<BitmapInstance>> bitmaps = new Dictionary<Bitmap, List<BitmapInstance>>();

        private GraphicsBase graphics;
        private TransformStack xform;

        public TransformStack Transform => xform;
        public GraphicsBase Graphics => graphics;

        public bool HasAnyMeshes  => meshBatch != null || meshSmoothBatch != null;
        public bool HasAnyLines   => lineBatches.Count > 0;
        public bool HasAnyTexts   => texts.Count > 0;
        public bool HasAnyBitmaps => bitmaps.Count > 0;
        public bool HasAnything   => HasAnyMeshes || HasAnyLines || HasAnyTexts || HasAnyBitmaps || HasAnyTickLineMeshes;

        public CommandList(GraphicsBase g, int dashTextureSize, int lineBias = 0, bool supportsLineWidth = true, int maxSmoothWidth = int.MaxValue)
        {
            graphics = g;
            xform = g.Transform;
            invDashTextureSize = 1.0f / dashTextureSize;
            lineWidthBias = lineBias;
            maxSmoothLineWidth = maxSmoothWidth;
#if FAMISTUDIO_LINUX
            drawThickLineAsPolygon = !supportsLineWidth;
#endif
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

        public void Release()
        {
            if (meshBatch != null)
            {
                graphics.ReleaseVertexArray(meshBatch.vtxArray);
                graphics.ReleaseColorArray(meshBatch.colArray);
                graphics.ReleaseIndexArray(meshBatch.idxArray);
            }

            if (meshSmoothBatch != null)
            {
                graphics.ReleaseVertexArray(meshSmoothBatch.vtxArray);
                graphics.ReleaseColorArray(meshSmoothBatch.colArray);
                graphics.ReleaseIndexArray(meshSmoothBatch.idxArray);
            }

            foreach (var batch in lineBatches)
            {
                graphics.ReleaseVertexArray(batch.vtxArray);
                graphics.ReleaseVertexArray(batch.texArray);
                graphics.ReleaseColorArray(batch.colArray);
            }

            meshBatch = null;
            meshSmoothBatch = null;
            lineBatches.Clear();
        }

        private MeshBatch GetMeshBatch(bool smooth)
        {
            MeshBatch batch;

            if (smooth)
            {
                if (meshSmoothBatch == null)
                {
                    meshSmoothBatch = new MeshBatch();
                    meshSmoothBatch.vtxArray = graphics.GetVertexArray();
                    meshSmoothBatch.colArray = graphics.GetColorArray();
                    meshSmoothBatch.idxArray = graphics.GetIndexArray();
                }

                batch = meshSmoothBatch;
            }
            else
            {
                if (meshBatch == null)
                {
                    meshBatch = new MeshBatch();
                    meshBatch.vtxArray = graphics.GetVertexArray();
                    meshBatch.colArray = graphics.GetColorArray();
                    meshBatch.idxArray = graphics.GetIndexArray();
                }

                batch = meshBatch;
            }

            return batch;
        }

        private LineBatch GetLineBatch(float width, bool smooth)
        {
            if (currentLineBatch == null ||
                currentLineBatch.lineWidth != width ||
                currentLineBatch.smooth != smooth)
            {
                currentLineBatch = null;

                foreach (var batch in lineBatches)
                {
                    if (batch.lineWidth == width && batch.smooth == smooth)
                    {
                        currentLineBatch = batch;
                        break;
                    }
                }

                if (currentLineBatch == null)
                {
                    currentLineBatch = new LineBatch();
                    currentLineBatch.smooth = smooth;
                    currentLineBatch.lineWidth = width;
                    currentLineBatch.vtxArray = graphics.GetVertexArray();
                    currentLineBatch.texArray = graphics.GetVertexArray();
                    currentLineBatch.colArray = graphics.GetColorArray();
                    lineBatches.Add(currentLineBatch);
                }

                return currentLineBatch;
            }

            return currentLineBatch;
        }

        private void DrawLineInternal(float x0, float y0, float x1, float y1, Brush brush, int width, bool smooth, bool dash)
        {
#if FAMISTUDIO_LINUX
            if (width > 1.0f && drawThickLineAsPolygon)
            {
                DrawThickLineAsPolygonInternal(x0, y0, x1, y1, brush, width);
                return;
            }
#endif

            var batch = GetLineBatch(width, smooth);

            batch.vtxArray[batch.vtxIdx++] = x0;
            batch.vtxArray[batch.vtxIdx++] = y0;
            batch.vtxArray[batch.vtxIdx++] = x1;
            batch.vtxArray[batch.vtxIdx++] = y1;

            if (dash)
            {
                if (x0 == x1)
                {
                    batch.texArray[batch.texIdx++] = 0.5f;
                    batch.texArray[batch.texIdx++] = (y0 + 0.5f) * invDashTextureSize;
                    batch.texArray[batch.texIdx++] = 0.5f;
                    batch.texArray[batch.texIdx++] = (y1 + 0.5f) * invDashTextureSize;
                }
                else
                {
                    batch.texArray[batch.texIdx++] = (x0 + 0.5f) * invDashTextureSize;
                    batch.texArray[batch.texIdx++] = 0.5f;
                    batch.texArray[batch.texIdx++] = (x1 + 0.5f) * invDashTextureSize;
                    batch.texArray[batch.texIdx++] = 0.5f;
                }
            }
            else
            {
                batch.texArray[batch.texIdx++] = 0.5f;
                batch.texArray[batch.texIdx++] = 0.5f;
                batch.texArray[batch.texIdx++] = 0.5f;
                batch.texArray[batch.texIdx++] = 0.5f;
            }

            batch.colArray[batch.colIdx++] = brush.PackedColor0;
            batch.colArray[batch.colIdx++] = brush.PackedColor0;
        }

#if FAMISTUDIO_LINUX
        private void DrawThickLineAsPolygonInternal(float x0, float y0, float x1, float y1, GLBrush brush, float width)
        {
            if (thickLineBatch == null)
            {
                thickLineBatch = new MeshBatch();
                thickLineBatch.vtxArray = graphics.GetVertexArray();
                thickLineBatch.colArray = graphics.GetColorArray();
                thickLineBatch.idxArray = graphics.GetIndexArray();
            }

            var batch = thickLineBatch;

            var dx = x1 - x0;
            var dy = y1 - y0;
            var invHalfWidth = (width * 0.5f) / (float)Math.Sqrt(dx * dx + dy * dy);
            dx *= invHalfWidth;
            dy *= invHalfWidth;

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

            batch.vtxArray[batch.vtxIdx++] = x0 + dy;
            batch.vtxArray[batch.vtxIdx++] = y0 + dx;
            batch.vtxArray[batch.vtxIdx++] = x1 + dy;
            batch.vtxArray[batch.vtxIdx++] = y1 + dx;
            batch.vtxArray[batch.vtxIdx++] = x1 - dy;
            batch.vtxArray[batch.vtxIdx++] = y1 - dx;
            batch.vtxArray[batch.vtxIdx++] = x0 - dy;
            batch.vtxArray[batch.vtxIdx++] = y0 - dx;

            batch.colArray[batch.colIdx++] = brush.PackedColor0;
            batch.colArray[batch.colIdx++] = brush.PackedColor0;
            batch.colArray[batch.colIdx++] = brush.PackedColor0;
            batch.colArray[batch.colIdx++] = brush.PackedColor0;
        }
#endif

        public void DrawLine(float x0, float y0, float x1, float y1, Brush brush, int width = 1, bool smooth = false, bool dash = false)
        {
            width += lineWidthBias;

            xform.TransformPoint(ref x0, ref y0);
            xform.TransformPoint(ref x1, ref y1);

            DrawLineInternal(x0, y0, x1, y1, brush, width, smooth, dash);
        }

        public void DrawLine(float[] points, Brush brush, int width = 1, bool smooth = false)
        {
            width += lineWidthBias;

            var x0 = points[0];
            var y0 = points[1];

            xform.TransformPoint(ref x0, ref y0);

            for (int i = 2; i < points.Length; i += 2)
            {
                var x1 = points[i + 0];
                var y1 = points[i + 1];
                
                xform.TransformPoint(ref x1, ref y1);
                DrawLineInternal(x0, y0, x1, y1, brush, width, smooth, false);

                x0 = x1;
                y0 = y1;
            }
        }

        public void DrawLine(float[,] points, Brush brush, int width = 1, bool smooth = false)
        {
            width += lineWidthBias;

            var x0 = points[0, 0];
            var y0 = points[0, 1];

            xform.TransformPoint(ref x0, ref y0);

            for (int i = 1; i < points.GetLength(0); i++)
            {
                var x1 = points[i, 0];
                var y1 = points[i, 1];

                xform.TransformPoint(ref x1, ref y1);
                DrawLineInternal(x0, y0, x1, y1, brush, width, smooth, false);

                x0 = x1;
                y0 = y1;
            }
        }

        public void DrawGeometry(float[,] points, Brush brush, int width = 1, bool smooth = false)
        {
            width += lineWidthBias;

            var x0 = points[0, 0];
            var y0 = points[0, 1];

            xform.TransformPoint(ref x0, ref y0);

            for (int i = 1; i < points.GetLength(0); i++)
            {
                var x1 = points[i, 0];
                var y1 = points[i, 1];

                xform.TransformPoint(ref x1, ref y1);
                DrawLineInternal(x0, y0, x1, y1, brush, width, smooth, false);

                x0 = x1;
                y0 = y1;
            }
        }

        public void DrawRectangle(Rectangle rect, Brush brush, int width = 1, bool smooth = false)
        {
            DrawRectangle(rect.Left, rect.Top, rect.Right, rect.Bottom, brush, width, smooth);
        }

        public void DrawRectangle(RectangleF rect, Brush brush, int width = 1, bool smooth = false)
        {
            DrawRectangle(rect.Left, rect.Top, rect.Right, rect.Bottom, brush, width, smooth);
        }

        public void DrawRectangle(float x0, float y0, float x1, float y1, Brush brush, int width = 1, bool smooth = false)
        {
            width += lineWidthBias;

            xform.TransformPoint(ref x0, ref y0);
            xform.TransformPoint(ref x1, ref y1);

            var halfWidth = 0.0f;
            var extraPixel = smooth ? 0 : 1;

#if FAMISTUDIO_ANDROID
            if (width > maxSmoothLineWidth)
            {
                smooth = false;
                halfWidth = (float)Math.Floor(width * 0.5f);
                extraPixel = 0;
            }
#endif

            DrawLineInternal(x0 - halfWidth, y0, x1 + extraPixel + halfWidth, y0, brush, width, smooth, false);
            DrawLineInternal(x1, y0 - halfWidth, x1, y1 + extraPixel + halfWidth, brush, width, smooth, false);
            DrawLineInternal(x0 - halfWidth, y1, x1 + extraPixel + halfWidth, y1, brush, width, smooth, false);
            DrawLineInternal(x0, y0 - halfWidth, x0, y1 + extraPixel + halfWidth, brush, width, smooth, false);
        }

        public void DrawGeometry(Geometry geo, Brush brush, int width, bool smooth = false, bool miter = false)
        {
            width += lineWidthBias;

#if FAMISTUDIO_ANDROID
            if (width > maxSmoothLineWidth)
            {
                smooth = false;
                miter = !xform.HasScaling; // Miter doesnt work with scaling atm.
            }
#endif

            var points = miter ? geo.GetMiterPoints(width) : geo.Points;

            var x0 = points[0];
            var y0 = points[1];

            xform.TransformPoint(ref x0, ref y0);

            for (int i = 0; i < points.Length / 2 - 1; i++)
            {
                var x1 = points[(i + 1) * 2 + 0];
                var y1 = points[(i + 1) * 2 + 1];

                xform.TransformPoint(ref x1, ref y1);

                DrawLineInternal(x0, y0, x1, y1, brush, width, smooth, false);

                x0 = x1;
                y0 = y1;
            }
        }

        public void FillAndDrawGeometry(Geometry geo, Brush fillBrush, Brush lineBrush, int lineWidth = 1, bool smooth = false, bool miter = false)
        {
            FillGeometry(geo, fillBrush, smooth);
            DrawGeometry(geo, lineBrush, lineWidth, smooth, miter);
        }

        public void FillRectangle(float x0, float y0, float x1, float y1, Brush brush)
        {
            var batch = GetMeshBatch(false);

            xform.TransformPoint(ref x0, ref y0);
            xform.TransformPoint(ref x1, ref y1);

            bool fullHorizontalGradient = brush.IsGradient && Math.Abs(brush.GradientSizeX) >= Math.Abs(x1 - x0);
            bool fullVerticalGradient   = brush.IsGradient && Math.Abs(brush.GradientSizeY) >= Math.Abs(y1 - y0);

            if (!brush.IsGradient || fullHorizontalGradient || fullVerticalGradient)
            {
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
                    batch.colArray[batch.colIdx++] = brush.PackedColor0;
                    batch.colArray[batch.colIdx++] = brush.PackedColor1;
                    batch.colArray[batch.colIdx++] = brush.PackedColor1;
                    batch.colArray[batch.colIdx++] = brush.PackedColor0;
                }
                else if (fullVerticalGradient)
                {
                    batch.colArray[batch.colIdx++] = brush.PackedColor0;
                    batch.colArray[batch.colIdx++] = brush.PackedColor0;
                    batch.colArray[batch.colIdx++] = brush.PackedColor1;
                    batch.colArray[batch.colIdx++] = brush.PackedColor1;
                }
                else
                {
                    batch.colArray[batch.colIdx++] = brush.PackedColor0;
                    batch.colArray[batch.colIdx++] = brush.PackedColor0;
                    batch.colArray[batch.colIdx++] = brush.PackedColor0;
                    batch.colArray[batch.colIdx++] = brush.PackedColor0;
                }
            }
            else if (
                brush.GradientSizeY == 0.0f ||
                brush.GradientSizeX == 0.0f) // More complex gradients.
            {
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

                if (brush.GradientSizeY == 0.0f)
                {
                    float xm = x0 + brush.GradientSizeX;

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
                    float ym = y0 + brush.GradientSizeY;

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

                batch.colArray[batch.colIdx++] = brush.PackedColor0;
                batch.colArray[batch.colIdx++] = brush.PackedColor1;
                batch.colArray[batch.colIdx++] = brush.PackedColor1;
                batch.colArray[batch.colIdx++] = brush.PackedColor0;
                batch.colArray[batch.colIdx++] = brush.PackedColor1;
                batch.colArray[batch.colIdx++] = brush.PackedColor1;
                batch.colArray[batch.colIdx++] = brush.PackedColor1;
                batch.colArray[batch.colIdx++] = brush.PackedColor1;
            }

            Debug.Assert(batch.colIdx * 2 == batch.vtxIdx);
        }

        public void FillRectangle(Rectangle rect, Brush brush)
        {
            FillRectangle(rect.Left, rect.Top, rect.Right, rect.Bottom, brush);
        }

        public void FillRectangle(RectangleF rect, Brush brush)
        {
            FillRectangle(rect.Left, rect.Top, rect.Right, rect.Bottom, brush);
        }

        public void FillAndDrawRectangle(float x0, float y0, float x1, float y1, Brush fillBrush, Brush lineBrush, int width = 1, bool smooth = false)
        {
            FillRectangle(x0, y0, x1, y1, fillBrush);
            DrawRectangle(x0, y0, x1, y1, lineBrush, width, smooth);
        }

        public void FillAndDrawRectangle(Rectangle rect, Brush fillBrush, Brush lineBrush, int width = 1, bool smooth = false)
        {
            FillRectangle(rect, fillBrush);
            DrawRectangle(rect, lineBrush, width, smooth);
        }

        public void FillGeometry(Geometry geo, Brush brush, bool smooth = false)
        {
            var batch = GetMeshBatch(smooth);
            var i0 = (short)(batch.vtxIdx / 2);

            if (!brush.IsGradient)
            {
                // All our geometries are closed, so no need for the last vert.
                for (int i = 0; i < geo.Points.Length - 2; i += 2)
                {
                    float x = geo.Points[i + 0];
                    float y = geo.Points[i + 1];

                    xform.TransformPoint(ref x, ref y);

                    batch.vtxArray[batch.vtxIdx++] = x;
                    batch.vtxArray[batch.vtxIdx++] = y;
                    batch.colArray[batch.colIdx++] = brush.PackedColor0;
                }
            }
            else
            {
                Debug.Assert(brush.GradientSizeX == 0.0f);

                // All our geometries are closed, so no need for the last vert.
                for (int i = 0; i < geo.Points.Length; i += 2)
                {
                    float x = geo.Points[i + 0];
                    float y = geo.Points[i + 1];

                    float lerp = y / brush.GradientSizeY;
                    byte r = (byte)(brush.Color0.R * (1.0f - lerp) + (brush.Color1.R * lerp));
                    byte g = (byte)(brush.Color0.G * (1.0f - lerp) + (brush.Color1.G * lerp));
                    byte b = (byte)(brush.Color0.B * (1.0f - lerp) + (brush.Color1.B * lerp));
                    byte a = (byte)(brush.Color0.A * (1.0f - lerp) + (brush.Color1.A * lerp));

                    xform.TransformPoint(ref x, ref y);

                    batch.vtxArray[batch.vtxIdx++] = x;
                    batch.vtxArray[batch.vtxIdx++] = y;
                    batch.colArray[batch.colIdx++] = ColorUtils.PackColor(r, g, b, a);
                }
            }

            // Simple fan
            var numVertices = geo.Points.Length / 2 - 1;
            for (int i = 0; i < numVertices - 2; i++)
            {
                batch.idxArray[batch.idxIdx++] = i0;
                batch.idxArray[batch.idxIdx++] = (short)(i0 + i + 1);
                batch.idxArray[batch.idxIdx++] = (short)(i0 + i + 2);
            }

            Debug.Assert(batch.colIdx * 2 == batch.vtxIdx);
        }

        public void FillGeometry(float[,] points, Brush brush, bool smooth = false)
        {
            var batch = GetMeshBatch(smooth);
            var i0 = (short)(batch.vtxIdx / 2);

            for (int i = 0; i < points.GetLength(0); i++)
            {
                float x = points[i, 0];
                float y = points[i, 1];

                xform.TransformPoint(ref x, ref y);

                batch.vtxArray[batch.vtxIdx++] = x;
                batch.vtxArray[batch.vtxIdx++] = y;
                batch.colArray[batch.colIdx++] = brush.PackedColor0;
            }

            // Simple fan.
            var numVertices = points.Length / 2;
            for (int i = 0; i < numVertices - 2; i++)
            {
                batch.idxArray[batch.idxIdx++] = i0;
                batch.idxArray[batch.idxIdx++] = (short)(i0 + i + 1);
                batch.idxArray[batch.idxIdx++] = (short)(i0 + i + 2);
            }
        }

        public void DrawText(string text, Font font, float x, float y, Brush brush, TextFlags flags = TextFlags.None, float width = 0, float height = 0, float clipMinX = 0, float clipMaxX = 0)
        {
            if (string.IsNullOrEmpty(text))
                return;

            Debug.Assert(!flags.HasFlag(TextFlags.Clip) || !flags.HasFlag(TextFlags.Ellipsis));
            Debug.Assert(!flags.HasFlag(TextFlags.Monospace) || !flags.HasFlag(TextFlags.Ellipsis));
            Debug.Assert(!flags.HasFlag(TextFlags.Monospace) || !flags.HasFlag(TextFlags.Clip));
            Debug.Assert(!flags.HasFlag(TextFlags.Ellipsis) || width > 0);
            Debug.Assert((flags & TextFlags.HorizontalAlignMask) == TextFlags.Left || width  > 0);
            Debug.Assert((flags & TextFlags.VerticalAlignMask)   == TextFlags.Top  || height > 0);

            if (!texts.TryGetValue(font, out var list))
            {
                list = new List<TextInstance>();
                texts.Add(font, list);
            }

            xform.TransformPoint(ref x, ref y);

            var inst = new TextInstance();
            inst.layoutRect = new RectangleF(x, y, width, height);
            inst.flags = flags;
            inst.text = text;
            inst.brush = brush;

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

            list.Add(inst);
        }

        public void DrawBitmap(Bitmap bmp, float x, float y, float opacity = 1.0f, Color tint = new Color())
        {
            Debug.Assert(Utils.Frac(x) == 0.0f && Utils.Frac(y) == 0.0f);
            DrawBitmap(bmp, x, y, bmp.Size.Width, bmp.Size.Height, opacity, 0, 0, 1, 1, false, tint);
        }

        public void DrawBitmapCentered(Bitmap bmp, float x, float y, float width, float height, float opacity = 1.0f, Color tint = new Color())
        {
            x += (width  - bmp.Size.Width)  / 2;
            y += (height - bmp.Size.Height) / 2;
            DrawBitmap(bmp, x, y, opacity, tint);
        }

        public void DrawBitmapAtlas(BitmapAtlasRef bmp, float x, float y, float opacity = 1.0f, float scale = 1.0f, Color tint = new Color())
        {
            Debug.Assert(Utils.Frac(x) == 0.0f && Utils.Frac(y) == 0.0f);
            var atlas = bmp.Atlas;
            var elementIndex = bmp.ElementIndex;
            var elementSize = bmp.ElementSize;
            atlas.GetElementUVs(elementIndex, out var u0, out var v0, out var u1, out var v1);
            DrawBitmap(atlas, x, y, elementSize.Width * scale, elementSize.Height * scale, opacity, u0, v0, u1, v1, false, tint);
        }

        public void DrawBitmapAtlasCentered(BitmapAtlasRef bmp, float x, float y, float width, float height, float opacity = 1.0f, float scale = 1.0f, Color tint = new Color())
        {
            x += (width  - bmp.ElementSize.Width)  / 2;
            y += (height - bmp.ElementSize.Height) / 2;
            DrawBitmapAtlas(bmp, x, y, opacity, scale, tint);
        }

        public void DrawBitmapAtlasCentered(BitmapAtlasRef bmp, Rectangle rect, float opacity = 1.0f, float scale = 1.0f, Color tint = new Color())
        {
            float x = rect.Left + (rect.Width  - bmp.ElementSize.Width)  / 2;
            float y = rect.Top  + (rect.Height - bmp.ElementSize.Height) / 2;
            DrawBitmapAtlas(bmp, x, y, opacity, scale, tint);
        }

        public void DrawBitmap(Bitmap bmp, float x, float y, float width, float height, float opacity, float u0 = 0, float v0 = 0, float u1 = 1, float v1 = 1, bool rotated = false, Color tint = new Color())
        {
            Debug.Assert(Utils.Frac(x) == 0.0f && Utils.Frac(y) == 0.0f);
            if (!bitmaps.TryGetValue(bmp, out var list))
            {
                list = new List<BitmapInstance>();
                bitmaps.Add(bmp, list);
            }

            xform.TransformPoint(ref x, ref y);
            xform.ScaleSize(ref width, ref height);

            var inst = new BitmapInstance();
            inst.x = x;
            inst.y = y;
            inst.sx = width;
            inst.sy = height;
            inst.tint = tint;

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

            inst.opacity = opacity;
            inst.rotated = rotated;

            list.Add(inst);
        }

        public List<MeshDrawData> GetMeshDrawData()
        {
            var drawData = new List<MeshDrawData>();

            if (meshBatch != null)
            {
                var draw = new MeshDrawData();
                draw.vtxArray = meshBatch.vtxArray;
                draw.colArray = meshBatch.colArray;
                draw.idxArray = meshBatch.idxArray;
                draw.numIndices = meshBatch.idxIdx;
                draw.vtxArraySize = meshBatch.vtxIdx;
                draw.colArraySize = meshBatch.colIdx;
                draw.idxArraySize = meshBatch.idxIdx;
                drawData.Add(draw);
            }

            if (meshSmoothBatch != null)
            {
                var draw = new MeshDrawData();
                draw.vtxArray = meshSmoothBatch.vtxArray;
                draw.colArray = meshSmoothBatch.colArray;
                draw.idxArray = meshSmoothBatch.idxArray;
                draw.numIndices = meshSmoothBatch.idxIdx;
                draw.vtxArraySize = meshSmoothBatch.vtxIdx;
                draw.colArraySize = meshSmoothBatch.colIdx;
                draw.idxArraySize = meshSmoothBatch.idxIdx;
                drawData.Add(draw);
            }

            return drawData;
        }

#if FAMISTUDIO_LINUX
        public MeshDrawData GetThickLineAsPolygonDrawData()
        {
            var draw = (MeshDrawData)null;

            if (thickLineBatch != null)
            {
                draw = new MeshDrawData();
                draw.vtxArray = thickLineBatch.vtxArray;
                draw.colArray = thickLineBatch.colArray;
                draw.idxArray = thickLineBatch.idxArray;
                draw.numIndices = thickLineBatch.idxIdx;
                draw.vtxArraySize = thickLineBatch.vtxIdx;
                draw.colArraySize = thickLineBatch.colIdx;
                draw.idxArraySize = thickLineBatch.idxIdx;
            }

            return draw;
        }
#endif

        public List<LineDrawData> GetLineDrawData()
        {
            var drawData = new List<LineDrawData>();

            foreach (var batch in lineBatches)
            {
                var draw = new LineDrawData();
                draw.vtxArray = batch.vtxArray;
                draw.texArray = batch.texArray;
                draw.colArray = batch.colArray;
                draw.numVertices = batch.vtxIdx / 2;
                draw.smooth = batch.smooth;
                draw.lineWidth = batch.lineWidth;
                draw.vtxArraySize = batch.vtxIdx;
                draw.texArraySize = batch.texIdx;
                draw.colArraySize = batch.colIdx;
                drawData.Add(draw);
            }

            drawData.Sort((d1, d2) => d1.lineWidth == d2.lineWidth ? d1.smooth.CompareTo(d2.smooth) : d1.lineWidth.CompareTo(d2.lineWidth));

            return drawData;
        }

        public List<DrawData> GetTextDrawData(float[] vtxArray, float[] texArray, int[] colArray, out int vtxArraySize, out int texArraySize, out int colArraySize, out int idxArraySize)
        {
            var drawData = new List<DrawData>();

            var vtxIdx = 0;
            var texIdx = 0;
            var colIdx = 0;
            var idxIdx = 0;

            foreach (var kv in texts)
            {
                var font = kv.Key;
                var list = kv.Value;
                var draw = new DrawData();

                draw.textureId = font.Texture;
                draw.start = idxIdx;

                foreach (var inst in list)
                {
                    var alignmentOffsetX = 0;
                    var alignmentOffsetY = font.OffsetY;
                    var mono = inst.flags.HasFlag(TextFlags.Monospace);

                    if (inst.flags.HasFlag(TextFlags.Ellipsis))
                    {
                        var ellipsisSizeX = font.MeasureString("...", mono) * 2; // Leave some padding.
                        if (font.TruncateString(ref inst.text, (int)(inst.layoutRect.Width - ellipsisSizeX)))
                            inst.text += "...";
                    }

                    if (inst.flags != TextFlags.TopLeft)
                    {
                        // MATTT : We no longer have minX in Measure string, is it a problem?
                        var minX = 0;
                        var maxX = font.MeasureString(inst.text, mono);

                        var halign = inst.flags & TextFlags.HorizontalAlignMask;
                        var valign = inst.flags & TextFlags.VerticalAlignMask;

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

                        if (valign != TextFlags.Top)
                        {
                            // Use a tall character with no descender as reference.
                            var charA = font.GetCharInfo('A');

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
                    }

                    var packedColor = inst.brush.PackedColor0;
                    var numVertices = inst.text.Length * 4;

                    int x = (int)(inst.layoutRect.X + alignmentOffsetX);
                    int y = (int)(inst.layoutRect.Y + alignmentOffsetY);
                    
                    if (mono)
                    {
                        var infoMono = font.GetCharInfo('0');

                        for (int i = 0; i < inst.text.Length; i++)
                        {
                            var c0 = inst.text[i];
                            var info = font.GetCharInfo(c0);

                            var monoAjustX = (infoMono.width  - info.width  + 1) / 2;
                            var monoAjustY = (infoMono.height - info.height + 1) / 2;

                            var x0 = x + info.xoffset + monoAjustX;
                            var y0 = y + info.yoffset;
                            var x1 = x0 + info.width;
                            var y1 = y0 + info.height;

                            vtxArray[vtxIdx++] = x0;
                            vtxArray[vtxIdx++] = y0;
                            vtxArray[vtxIdx++] = x1;
                            vtxArray[vtxIdx++] = y0;
                            vtxArray[vtxIdx++] = x1;
                            vtxArray[vtxIdx++] = y1;
                            vtxArray[vtxIdx++] = x0;
                            vtxArray[vtxIdx++] = y1;

                            texArray[texIdx++] = info.u0;
                            texArray[texIdx++] = info.v0;
                            texArray[texIdx++] = info.u1;
                            texArray[texIdx++] = info.v0;
                            texArray[texIdx++] = info.u1;
                            texArray[texIdx++] = info.v1;
                            texArray[texIdx++] = info.u0;
                            texArray[texIdx++] = info.v1;

                            colArray[colIdx++] = packedColor;
                            colArray[colIdx++] = packedColor;
                            colArray[colIdx++] = packedColor;
                            colArray[colIdx++] = packedColor;

                            x += infoMono.xadvance;
                        }

                        idxIdx += inst.text.Length * 6;
                        draw.count += inst.text.Length * 6;
                    }
                    else if (inst.flags.HasFlag(TextFlags.Clip)) // Slow path when there is clipping.
                    {
                        var clipMinX = (int)(inst.clipRect.X);
                        var clipMaxX = (int)(inst.clipRect.X + inst.clipRect.Width);

                        for (int i = 0; i < inst.text.Length; i++)
                        {
                            var c0 = inst.text[i];
                            var info = font.GetCharInfo(c0);

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

                                vtxArray[vtxIdx++] = x0;
                                vtxArray[vtxIdx++] = y0;
                                vtxArray[vtxIdx++] = x1;
                                vtxArray[vtxIdx++] = y0;
                                vtxArray[vtxIdx++] = x1;
                                vtxArray[vtxIdx++] = y1;
                                vtxArray[vtxIdx++] = x0;
                                vtxArray[vtxIdx++] = y1;

                                texArray[texIdx++] = u0;
                                texArray[texIdx++] = v0;
                                texArray[texIdx++] = u1;
                                texArray[texIdx++] = v0;
                                texArray[texIdx++] = u1;
                                texArray[texIdx++] = v1;
                                texArray[texIdx++] = u0;
                                texArray[texIdx++] = v1;

                                colArray[colIdx++] = packedColor;
                                colArray[colIdx++] = packedColor;
                                colArray[colIdx++] = packedColor;
                                colArray[colIdx++] = packedColor;

                                idxIdx += 6;
                                draw.count += 6;
                            }

                            x += info.xadvance;
                            if (i != inst.text.Length - 1)
                            {
                                char c1 = inst.text[i + 1];
                                x += font.GetKerning(c0, c1);
                            }
                        }
                    }
                    else
                    {
                        for (int i = 0; i < inst.text.Length; i++)
                        {
                            var c0 = inst.text[i];
                            var info = font.GetCharInfo(c0);

                            var x0 = x + info.xoffset;
                            var y0 = y + info.yoffset;
                            var x1 = x0 + info.width;
                            var y1 = y0 + info.height;

                            vtxArray[vtxIdx++] = x0;
                            vtxArray[vtxIdx++] = y0;
                            vtxArray[vtxIdx++] = x1;
                            vtxArray[vtxIdx++] = y0;
                            vtxArray[vtxIdx++] = x1;
                            vtxArray[vtxIdx++] = y1;
                            vtxArray[vtxIdx++] = x0;
                            vtxArray[vtxIdx++] = y1;

                            texArray[texIdx++] = info.u0;
                            texArray[texIdx++] = info.v0;
                            texArray[texIdx++] = info.u1;
                            texArray[texIdx++] = info.v0;
                            texArray[texIdx++] = info.u1;
                            texArray[texIdx++] = info.v1;
                            texArray[texIdx++] = info.u0;
                            texArray[texIdx++] = info.v1;

                            colArray[colIdx++] = packedColor;
                            colArray[colIdx++] = packedColor;
                            colArray[colIdx++] = packedColor;
                            colArray[colIdx++] = packedColor;

                            x += info.xadvance;
                            if (i != inst.text.Length - 1)
                            {
                                char c1 = inst.text[i + 1];
                                x += font.GetKerning(c0, c1);
                            }
                        }

                        idxIdx += inst.text.Length * 6;
                        draw.count += inst.text.Length * 6;
                    }
                }

                drawData.Add(draw);
            }

            vtxArraySize = vtxIdx;
            texArraySize = texIdx;
            colArraySize = colIdx;
            idxArraySize = idxIdx;

            return drawData;
        }

        public List<DrawData> GetBitmapDrawData(float[] vtxArray, float[] texArray, int[] colArray, out int vtxArraySize, out int texArraySize, out int colArraySize, out int idxArraySize)
        {
            var drawData = new List<DrawData>();

            var vtxIdx = 0;
            var texIdx = 0;
            var colIdx = 0;
            var idxIdx = 0;

            foreach (var kv in bitmaps)
            {
                var bmp = kv.Key;
                var list = kv.Value;
                var draw = new DrawData();

                draw.textureId = bmp.Id;
                draw.start = idxIdx;

                foreach (var inst in list)
                {
                    var x0 = inst.x;
                    var y0 = inst.y;
                    var x1 = inst.x + inst.sx;
                    var y1 = inst.y + inst.sy;
                    var tint = inst.tint != Color.Empty ? inst.tint : Color.White;

                    vtxArray[vtxIdx++] = x0;
                    vtxArray[vtxIdx++] = y0;
                    vtxArray[vtxIdx++] = x1;
                    vtxArray[vtxIdx++] = y0;
                    vtxArray[vtxIdx++] = x1;
                    vtxArray[vtxIdx++] = y1;
                    vtxArray[vtxIdx++] = x0;
                    vtxArray[vtxIdx++] = y1;

                    if (inst.rotated)
                    {
                        texArray[texIdx++] = inst.u1;
                        texArray[texIdx++] = inst.v0;
                        texArray[texIdx++] = inst.u1;
                        texArray[texIdx++] = inst.v1;
                        texArray[texIdx++] = inst.u0;
                        texArray[texIdx++] = inst.v1;
                        texArray[texIdx++] = inst.u0;
                        texArray[texIdx++] = inst.v0;
                    }
                    else
                    {
                        texArray[texIdx++] = inst.u0;
                        texArray[texIdx++] = inst.v0;
                        texArray[texIdx++] = inst.u1;
                        texArray[texIdx++] = inst.v0;
                        texArray[texIdx++] = inst.u1;
                        texArray[texIdx++] = inst.v1;
                        texArray[texIdx++] = inst.u0;
                        texArray[texIdx++] = inst.v1;
                    }

                    var packedOpacity = ColorUtils.PackColor(tint.R, tint.G, tint.B, (int)(inst.opacity * 255)); 
                    colArray[colIdx++] = packedOpacity;
                    colArray[colIdx++] = packedOpacity;
                    colArray[colIdx++] = packedOpacity;
                    colArray[colIdx++] = packedOpacity;

                    draw.count += 6;
                    idxIdx += 6;
                }

                drawData.Add(draw);
            }

            vtxArraySize = vtxIdx;
            texArraySize = texIdx;
            colArraySize = colIdx;
            idxArraySize = idxIdx;

            return drawData;
        }
    }
}
