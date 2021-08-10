using System;
using System.Drawing;
using System.Collections.Generic;
using System.Diagnostics;

#if FAMISTUDIO_ANDROID
using Android.Opengl;
using Javax.Microedition.Khronos.Opengles;
using Bitmap = Android.Graphics.Bitmap;
#else
using OpenTK;
using OpenTK.Graphics.OpenGL;
#if FAMISTUDIO_WINDOWS
using Bitmap = System.Drawing.Bitmap;
#else
using Bitmap = Gdk.Pixbuf;
#endif
#endif

namespace FamiStudio
{
    public abstract class GLGraphicsBase : IDisposable
    {
        protected float windowScaling = 1.0f;
        protected int windowSizeY;
        protected Rectangle controlRect;
        protected Rectangle controlRectFlip;
        protected GLTransform transform = new GLTransform();
        protected Dictionary<Tuple<Color, int>, GLBrush> verticalGradientCache = new Dictionary<Tuple<Color, int>, GLBrush>();
        protected GLBitmap dashedBitmap;

        public float WindowScaling => windowScaling;
        public int DashTextureSize => dashedBitmap.Size.Width;
        public GLTransform Transform => transform;

        protected const int MaxAtlasResolution = 1024;
        protected const int MaxVertexCount = 64 * 1024;
        protected const int MaxTexCoordCount = 64 * 1024;
        protected const int MaxColorCount = 64 * 1024;
        protected const int MaxIndexCount = MaxVertexCount / 4 * 6;

        protected float[] vtxArray = new float[MaxVertexCount * 2];
        protected float[] texArray = new float[MaxVertexCount * 2];
        protected int[]   colArray = new int[MaxVertexCount];
        protected short[] quadIdxArray = new short[MaxIndexCount];

        protected List<float[]> freeVtxArrays = new List<float[]>();
        protected List<int[]>   freeColArrays = new List<int[]>();
        protected List<short[]> freeIdxArrays = new List<short[]>();

        protected GLGraphicsBase()
        {
            windowScaling = GLTheme.MainWindowScaling;

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
        }

        protected abstract int CreateEmptyTexture(int width, int height);
        protected abstract int CreateTexture(Bitmap bmp);

        public virtual void BeginDraw(Rectangle unflippedControlRect, int windowSizeY)
        {
            this.windowSizeY = windowSizeY;

            controlRect = unflippedControlRect;
            controlRectFlip = FlipRectangleY(unflippedControlRect);
            transform.SetIdentity();
        }

        public virtual void EndDraw()
        {
        }

        protected Rectangle FlipRectangleY(Rectangle rc)
        {
            return new Rectangle(rc.Left, windowSizeY - rc.Top - rc.Height, rc.Width, rc.Height);
        }

        public float MeasureString(string text, GLFont font)
        {
            font.MeasureString(text, out int minX, out int maxX);
            return maxX - minX;
        }

        public GLGeometry CreateGeometry(float[,] points, bool closed = true)
        {
            return new GLGeometry(points, closed);
        }

        public GLBitmap CreateEmptyBitmap(int width, int height)
        {
            return new GLBitmap(CreateEmptyTexture(width, height), width, height);
        }

        public GLBrush CreateSolidBrush(Color color)
        {
            return new GLBrush(color);
        }

        public GLBrush CreateHorizontalGradientBrush(float x0, float x1, Color color0, Color color1)
        {
            Debug.Assert(x0 == 0.0f);
            return new GLBrush(color0, color1, x1 - x0, 0.0f);
        }

        public GLBrush CreateVerticalGradientBrush(float y0, float y1, Color color0, Color color1)
        {
            Debug.Assert(y0 == 0.0f);
            return new GLBrush(color0, color1, 0.0f, y1 - y0);
        }

        public GLBrush GetSolidBrush(Color color, float dimming = 1.0f, float alphaDimming = 1.0f)
        {
            Color color2 = Color.FromArgb(
                Utils.Clamp((int)(color.A * alphaDimming), 0, 255),
                Utils.Clamp((int)(color.R * dimming), 0, 255),
                Utils.Clamp((int)(color.G * dimming), 0, 255),
                Utils.Clamp((int)(color.B * dimming), 0, 255));

            return new GLBrush(color2);
        }

        public GLBrush GetVerticalGradientBrush(Color color1, int sizeY, float dimming)
        {
            var key = new Tuple<Color, int>(color1, sizeY);

            GLBrush brush;
            if (verticalGradientCache.TryGetValue(key, out brush))
                return brush;

            Color color2 = Color.FromArgb(
                Utils.Clamp((int)(color1.A), 0, 255),
                Utils.Clamp((int)(color1.R * dimming), 0, 255),
                Utils.Clamp((int)(color1.G * dimming), 0, 255),
                Utils.Clamp((int)(color1.B * dimming), 0, 255));

            brush = CreateVerticalGradientBrush(0, sizeY, color1, color2);
            verticalGradientCache[key] = brush;

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

        public GLFont CreateFont(Bitmap bmp, string[] def, int size, int alignment, bool ellipsis, int existingTexture = -1)
        {
            var font = (GLFont)null;
            var lines = def;

            int baseValue = 0;
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
                            texSizeX = ReadFontParam<int>(splits, "scaleW");
                            texSizeY = ReadFontParam<int>(splits, "scaleH");

                            int glTex = existingTexture;
                            if (glTex == 0)
                                glTex = CreateTexture(bmp);

                            font = new GLFont(glTex, size - baseValue, alignment, ellipsis);
                            break;
                        }
                    case "char":
                        {
                            var charInfo = new GLFont.CharInfo();

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

    public class GLFont : IDisposable
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

        public int Texture { get; private set; }
        public int OffsetY { get; private set; }
        public int Alignment { get; private set; }
        public bool Ellipsis { get; private set; }

        public GLFont(int tex, int offsetY, int alignment, bool ellipsis)
        {
            Texture = tex;
            OffsetY = offsetY;
            Alignment = alignment;
            Ellipsis = ellipsis;
        }

        public void Dispose()
        {
#if FAMISTUDIO_ANDROID // DROIDTODO
            int id = Texture;
            //GL.DeleteTextures(1, ref id);
#else
            GL.DeleteTexture(Texture);
#endif
        }

        public void AddChar(char c, CharInfo info)
        {
            charMap[c] = info;
        }

        public void AddKerningPair(int c0, int c1, int amount)
        {
            kerningPairs[c0 | (c1 << 8)] = amount;
        }

        public CharInfo GetCharInfo(char c)
        {
            if (charMap.TryGetValue(c, out CharInfo info))
            {
                return info;
            }
            else
            {
                return charMap[char.MaxValue];
            }
        }

        public int GetKerning(char c0, char c1)
        {
            int key = (int)c0 | ((int)c1 << 8);
            return kerningPairs.TryGetValue(key, out int amount) ? amount : 0;
        }

        public void MeasureString(string text, out int minX, out int maxX)
        {
            minX = 0;
            maxX = 0;

            int x = 0;

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

    public class GLGeometry : IDisposable
    {
        public float[] Points { get; private set; }

        public Dictionary<float, float[]> miterPoints = new Dictionary<float, float[]>();

        public GLGeometry(float[,] points, bool closed)
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

    public class GLBrush : IDisposable
    {
        public float GradientSizeX = 0.0f;
        public float GradientSizeY = 0.0f;
        public Color Color0;
        public Color Color1;
        public int PackedColor0;
        public int PackedColor1;

        public GLBrush(Color color)
        {
            Color0 = color;
            PackedColor0 = GLColorUtils.PackColor(color);
        }

        public GLBrush(Color color0, Color color1, float sizeX, float sizeY)
        {
            Color0 = color0;
            Color1 = color1;
            PackedColor0 = GLColorUtils.PackColor(color0);
            PackedColor1 = GLColorUtils.PackColor(color1);
            GradientSizeX = sizeX;
            GradientSizeY = sizeY;
        }

        public bool IsGradient => GradientSizeX > 0 || GradientSizeY > 0;

        public void Dispose()
        {
        }
    }

    public class GLBitmap : IDisposable
    {
        protected int  id;
        protected Size size;

        public int Id => id;
        public Size Size => size;
        private bool dispose = true;

        public GLBitmap(int id, int width, int height, bool disp = true)
        {
            this.id = id;
            this.size = new Size(width, height);
            this.dispose = disp;
        }

        public void Dispose()
        {
            /*
            if (dispose)
#if FAMISTUDIO_ANDROID // DROIDTODO
                GL.DeleteTextures(1, ref id);
#else
                GL.DeleteTexture(id);
#endif
            */
        }

        public override int GetHashCode()
        {
            return id;
        }
    }

    public class GLBitmapAtlas : GLBitmap
    {
        Rectangle[] elementRects;

        public Size GetElementSize(int index) => elementRects[index].Size;

        public GLBitmapAtlas(int id, int width, int height, Rectangle[] elementRects, bool disp = true) : 
            base(id, width, height, disp)
        {
            this.elementRects = elementRects;
        }

        public void GetElementUVs(int elementIndex, out float u0, out float v0, out float u1, out float v1)
        {
            var rect = elementRects[elementIndex];

            // DROIDTODO : +0.5?
            u0 = rect.Left   / (float)size.Width;
            u1 = rect.Right  / (float)size.Width;
            v0 = rect.Top    / (float)size.Height;
            v1 = rect.Bottom / (float)size.Height;
        }
    }

    public static class GLColorUtils
    {
        public static int PackColor(Color c)
        {
            // DROIDTODO : Do we need to check the byte ordering here?
            return (c.A << 24) | (c.B << 16) | (c.G << 8) | c.R;
        }

        public static int PackColor(int r, int g, int b, int a)
        {
            // DROIDTODO : Do we need to check the byte ordering here?
            return (a << 24) | (b << 16) | (g << 8) | r;
        }
    }

    public class GLTransform
    {
        public static readonly GLTransform Identity = new GLTransform();

        protected Vector4 transform = new Vector4(1, 1, 0, 0); // xy = scale, zw = translation
        protected Stack<Vector4> transformStack = new Stack<Vector4>();

        public Vector4 Transform => transform;

        public void SetIdentity()
        {
            transform.X = 1;
            transform.Y = 1;
            transform.Z = 0;
            transform.W = 0;
        }

        public void PushTranslation(float x, float y)
        {
            transformStack.Push(transform);
            transform.Z += x;
            transform.W += y;
        }

        public void PushTransform(float tx, float ty, float sx, float sy)
        {
            transformStack.Push(transform);

            transform.X *= sx;
            transform.Y *= sy;
            transform.Z += tx;
            transform.W += ty;
        }

        public void PopTransform()
        {
            transform = transformStack.Pop();
        }

        public void TransformPoint(ref float x, ref float y)
        {
            x = x * transform.X + transform.Z;
            y = y * transform.Y + transform.W;
        }

        public void ScaleSize(ref float width, ref float height)
        {
            width  *= transform.X;
            height *= transform.Y;
        }
    }

    // This is common to both OGL, it only does data packing, no GL calls.
    public class GLCommandList
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
            public int[] colArray;

            public int vtxIdx = 0;
            public int texIdx = 0;
            public int colIdx = 0;
        };

        private class TextInstance
        {
            public float x;
            public float y;
            public float width;
            public string text;
            public bool clip;
            public GLBrush brush;
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

        private float invDashTextureSize;
        private MeshBatch meshBatch;
        private MeshBatch meshSmoothBatch;
        private LineBatch currentLineBatch;
        private List<LineBatch> lineBatches = new List<LineBatch>();
        private Dictionary<GLFont,   List<TextInstance>>   texts   = new Dictionary<GLFont,   List<TextInstance>>();
        private Dictionary<GLBitmap, List<BitmapInstance>> bitmaps = new Dictionary<GLBitmap, List<BitmapInstance>>();

        private GLGraphics  graphics;
        private GLTransform xform;
        public  GLTransform Transform => xform;

        public bool HasAnyMeshes  => meshBatch != null || meshSmoothBatch != null;
        public bool HasAnyLines   => lineBatches.Count > 0;
        public bool HasAnyTexts   => texts.Count > 0;
        public bool HasAnyBitmaps => bitmaps.Count > 0;

        public GLCommandList(GLGraphics g, int dashTextureSize)
        {
            graphics = g;
            xform = g.Transform;
            invDashTextureSize = 1.0f / dashTextureSize;
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

        private void DrawLineInternal(float x0, float y0, float x1, float y1, GLBrush brush, float width, bool smooth, bool dash)
        {
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

        public void DrawLine(float x0, float y0, float x1, float y1, GLBrush brush, float width = 1.0f, bool smooth = false, bool dash = false)
        {
            xform.TransformPoint(ref x0, ref y0);
            xform.TransformPoint(ref x1, ref y1);

            DrawLineInternal(x0, y0, x1, y1, brush, width, smooth, dash);
        }

        public void DrawLine(float[] points, GLBrush brush, float width = 1.0f, bool smooth = false)
        {
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

        public void DrawGeometry(float[,] points, GLBrush brush, float width = 1.0f, bool smooth = false)
        {
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

        public void DrawRectangle(float x0, float y0, float x1, float y1, GLBrush brush, float width = 1.0f)
        {
            xform.TransformPoint(ref x0, ref y0);
            xform.TransformPoint(ref x1, ref y1);

            DrawLineInternal(x0, y0, x1, y0, brush, width, false, false);
            DrawLineInternal(x1, y0, x1, y1, brush, width, false, false);
            DrawLineInternal(x1, y1, x0, y1, brush, width, false, false);
            DrawLineInternal(x0, y1, x0, y0, brush, width, false, false);
        }

        public void DrawGeometry(GLGeometry geo, GLBrush brush, float width, bool smooth = false, bool miter = false)
        {
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

        public void FillAndDrawGeometry(GLGeometry geo, GLBrush fillBrush, GLBrush lineBrush, float lineWidth = 1.0f, bool smooth = false, bool miter = false)
        {
            FillGeometry(geo, fillBrush, smooth);
            DrawGeometry(geo, lineBrush, lineWidth, miter);
        }

        public void FillRectangle(float x0, float y0, float x1, float y1, GLBrush brush)
        {
            var batch = GetMeshBatch(false);

            xform.TransformPoint(ref x0, ref y0);
            xform.TransformPoint(ref x1, ref y1);

            bool fullHorizontalGradient = brush.IsGradient && brush.GradientSizeX == (x1 - x0);
            bool fullVerticalGradient   = brush.IsGradient && brush.GradientSizeY == (y1 - y0);

            // MATTT : Make sure this doesnt add any overhead!
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
                var i4 = (short)(batch.vtxIdx / 2 + 0);
                var i5 = (short)(batch.vtxIdx / 2 + 1);
                var i6 = (short)(batch.vtxIdx / 2 + 2);
                var i7 = (short)(batch.vtxIdx / 2 + 3);

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

        public void FillRectangle(RectangleF rect, GLBrush brush)
        {
            FillRectangle(rect.Left, rect.Top, rect.Right, rect.Bottom, brush);
        }

        public void FillAndDrawRectangle(float x0, float y0, float x1, float y1, GLBrush fillBrush, GLBrush lineBrush, float width = 1.0f, bool miter = false)
        {
            FillRectangle(x0, y0, x1, y1, fillBrush);
            DrawRectangle(x0, y0, x1, y1, lineBrush, width);
        }

        public void FillGeometry(GLGeometry geo, GLBrush brush, bool smooth = false)
        {
            var batch = GetMeshBatch(smooth);
            var i0 = (short)(batch.vtxIdx / 2);

            if (!brush.IsGradient)
            {
                for (int i = 0; i < geo.Points.Length; i += 2)
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
                    batch.colArray[batch.colIdx++] = GLColorUtils.PackColor(r, g, b, a);
                }
            }

            // Simple fan.
            var numVertices = geo.Points.Length / 2;
            for (int i = 0; i < numVertices - 2; i++)
            {
                batch.idxArray[batch.idxIdx++] = i0;
                batch.idxArray[batch.idxIdx++] = (short)(i0 + i + 1);
                batch.idxArray[batch.idxIdx++] = (short)(i0 + i + 2);
            }

            Debug.Assert(batch.colIdx * 2 == batch.vtxIdx);
        }

        public void FillGeometry(float[] points, GLBrush brush, bool smooth = false)
        {
            var batch = GetMeshBatch(smooth);
            var i0 = (short)(batch.vtxIdx / 2);

            for (int i = 0; i < points.Length; i += 2)
            {
                float x = points[i + 0];
                float y = points[i + 1];

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

        public void FillGeometry(float[,] points, GLBrush brush, bool smooth = false)
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

        public void DrawText(string text, GLFont font, float x, float y, GLBrush brush, float width = 1000, bool clip = false)
        {
            if (!texts.TryGetValue(font, out var list))
            {
                list = new List<TextInstance>();
                texts.Add(font, list);
            }

            xform.TransformPoint(ref x, ref y);

            var inst = new TextInstance();
            inst.x = x;
            inst.y = y;
            inst.text = text;
            inst.brush = brush;
            inst.width = width;
            inst.clip = clip;

            list.Add(inst);
        }

        public void DrawBitmap(GLBitmap bmp, float x, float y, float opacity = 1.0f)
        {
            DrawBitmap(bmp, x, y, bmp.Size.Width, bmp.Size.Height, opacity);
        }

        public void DrawBitmapAtlas(GLBitmapAtlas atlas, int bitmapIndex, float x, float y, float opacity = 1.0f)
        {
            atlas.GetElementUVs(bitmapIndex, out var u0, out var v0, out var u1, out var v1);
            var elementSize = atlas.GetElementSize(bitmapIndex);
            DrawBitmap(atlas, x, y, elementSize.Width, elementSize.Height, opacity, u0, v0, u1, v1);
        }

        public void DrawBitmap(GLBitmap bmp, float x, float y, float width, float height, float opacity, float u0 = 0, float v0 = 0, float u1 = 1, float v1 = 1)
        {
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
            inst.u0 = u0;
            inst.v0 = v0;
            inst.u1 = u1;
            inst.v1 = v1;
            inst.opacity = opacity;

            list.Add(inst);
        }

        // HACK : Very specific call only used by video rendering, too lazy to do the proper transforms.
        public void DrawRotatedFlippedBitmap(GLBitmap bmp, float x, float y, float width, float height)
        {
            Debug.Assert(false);

            //GL.Enable(EnableCap.Texture2D);
            //GL.BindTexture(TextureTarget.Texture2D, bmp.Id);
            //GL.Color4(1.0f, 1.0f, 1.0f, 1.0f);

            //GL.Begin(PrimitiveType.Quads);
            //GL.TexCoord2(0, 0); GL.Vertex2(x - height, y);
            //GL.TexCoord2(1, 0); GL.Vertex2(x - height, y - width);
            //GL.TexCoord2(1, 1); GL.Vertex2(x, y - width);
            //GL.TexCoord2(0, 1); GL.Vertex2(x, y);
            //GL.End();

            //GL.Disable(EnableCap.Texture2D);
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
                draw.vtxArraySize = meshBatch.vtxIdx;
                draw.colArraySize = meshBatch.colIdx;
                draw.idxArraySize = meshBatch.idxIdx;
                drawData.Add(draw);
            }

            return drawData;
        }

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

        public List<DrawData> GetTextDrawData(float[] vtxArray, float[] texArray, int[] colArray, out int vtxArraySize, out int texArraySize, out int colArraySize)
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

                foreach (var sub in list)
                {
                    int alignmentOffsetX = 0;
                    if (font.Alignment != 0)
                    {
                        font.MeasureString(sub.text, out int minX, out int maxX);

                        if (font.Alignment == 1)
                        {
                            alignmentOffsetX -= minX;
                            alignmentOffsetX += ((int)sub.width - maxX - minX) / 2;
                        }
                        else
                        {
                            alignmentOffsetX -= minX;
                            alignmentOffsetX += ((int)sub.width - maxX - minX);
                        }
                    }

                    var packedColor = sub.brush.PackedColor0;
                    var numVertices = sub.text.Length * 4;

                    int x = (int)(sub.x + alignmentOffsetX);
                    int y = (int)(sub.y + font.OffsetY);

                    // Slow path when there is clipping.
                    if (sub.clip)
                    {
                        var clipMinX = (int)(sub.x);
                        var clipMaxX = (int)(sub.x + sub.width);

                        for (int i = 0; i < sub.text.Length; i++)
                        {
                            var c0 = sub.text[i];
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
                            if (i != sub.text.Length - 1)
                            {
                                char c1 = sub.text[i + 1];
                                x += font.GetKerning(c0, c1);
                            }
                        }
                    }
                    else
                    {
                        for (int i = 0; i < sub.text.Length; i++)
                        {
                            var c0 = sub.text[i];
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
                            if (i != sub.text.Length - 1)
                            {
                                char c1 = sub.text[i + 1];
                                x += font.GetKerning(c0, c1);
                            }
                        }

                        idxIdx += sub.text.Length * 6;
                        draw.count += sub.text.Length * 6;
                    }
                }

                drawData.Add(draw);
            }

            vtxArraySize = vtxIdx;
            texArraySize = texIdx;
            colArraySize = colIdx;

            return drawData;
        }

        public List<DrawData> GetBitmapDrawData(float[] vtxArray, float[] texArray, int[] colArray, out int vtxArraySize, out int texArraySize, out int colArraySize)
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

                foreach (var sub in list)
                {
                    float x0 = sub.x;
                    float y0 = sub.y;
                    float x1 = sub.x + sub.sx;
                    float y1 = sub.y + sub.sy;

                    vtxArray[vtxIdx++] = x0;
                    vtxArray[vtxIdx++] = y0;
                    vtxArray[vtxIdx++] = x1;
                    vtxArray[vtxIdx++] = y0;
                    vtxArray[vtxIdx++] = x1;
                    vtxArray[vtxIdx++] = y1;
                    vtxArray[vtxIdx++] = x0;
                    vtxArray[vtxIdx++] = y1;

                    texArray[texIdx++] = sub.u0;
                    texArray[texIdx++] = sub.v0;
                    texArray[texIdx++] = sub.u1;
                    texArray[texIdx++] = sub.v0;
                    texArray[texIdx++] = sub.u1;
                    texArray[texIdx++] = sub.v1;
                    texArray[texIdx++] = sub.u0;
                    texArray[texIdx++] = sub.v1;

                    var packedOpacity = GLColorUtils.PackColor(255, 255, 255, (int)(sub.opacity * 255));
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

            return drawData;
        }
    }
}
