using System;
using System.Drawing;
using System.Collections.Generic;
using System.Diagnostics;

#if FAMISTUDIO_ANDROID
using Android.Opengl;
using Javax.Microedition.Khronos.Opengles;
#else
using OpenTK;
using OpenTK.Graphics.OpenGL;
#endif

namespace FamiStudio
{
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
        public GLBitmap Bitmap;

        public GLBrush(Color color)
        {
            Color0 = color;
        }

        public GLBrush(GLBitmap bmp, bool tileX, bool tileY)
        {
            Bitmap = bmp;
            Color0 = Color.FromArgb(255, 255, 255, 255);

#if FAMISTUDIO_ANDROID // DROIDTODO
            
#else
            GL.BindTexture(TextureTarget.Texture2D, bmp.Id);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)(tileX ? All.Repeat : All.ClampToEdge));
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)(tileY ? All.Repeat : All.ClampToEdge));
#endif
        }

        public GLBrush(Color color0, Color color1, float sizeX, float sizeY)
        {
            Color0 = color0;
            Color1 = color1;
            GradientSizeX = sizeX;
            GradientSizeY = sizeY;
        }

        public bool IsGradient => GradientSizeX > 0 || GradientSizeY > 0;
        public bool IsBitmap => Bitmap != null;

        public void Dispose()
        {
            Utils.DisposeAndNullify(ref Bitmap);
        }
    }

    public class GLBitmap : IDisposable
    {
        int  id;
        Size size;

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
    }
}
