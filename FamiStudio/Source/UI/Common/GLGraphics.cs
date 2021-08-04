using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using OpenTK;
using OpenTK.Graphics.OpenGL;

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
            GL.DeleteTexture(Texture);
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
        public float[,] Points { get; private set; }

        public Dictionary<float, float[,]> miterPoints = new Dictionary<float, float[,]>();

        public GLGeometry(float[,] points, bool closed)
        {
            var numPoints = points.GetLength(0);

            var closedPoints   = new float[(numPoints + (closed ? 1 : 0)) * 1, points.GetLength(1)];

            for (int i = 0; i < closedPoints.GetLength(0); i++)
            {
                closedPoints[i, 0] = points[i % points.GetLength(0), 0];
                closedPoints[i, 1] = points[i % points.GetLength(0), 1];
            }

            Points = closedPoints;
        }

        public float[,] GetMiterPoints(float lineWidth)
        {
            if (miterPoints.TryGetValue(lineWidth, out var points))
                return points;

            points = new float[Points.GetLength(0) * 2, Points.GetLength(1)];

            for (int i = 0; i < Points.GetLength(0) - 1; i++)
            {
                var x0 = Points[i + 0, 0];
                var x1 = Points[i + 1, 0];
                var y0 = Points[i + 0, 1];
                var y1 = Points[i + 1, 1];

                var dx = x1 - x0;
                var dy = y1 - y0;
                var len = (float)Math.Sqrt(dx * dx + dy * dy);

                var nx = dx / len * lineWidth * 0.5f;
                var ny = dy / len * lineWidth * 0.5f;

                points[2 * i + 0, 0] = x0 - nx;
                points[2 * i + 0, 1] = y0 - ny;
                points[2 * i + 1, 0] = x1 + nx;
                points[2 * i + 1, 1] = y1 + ny;
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

            GL.BindTexture(TextureTarget.Texture2D, bmp.Id);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)(tileX ? All.Repeat : All.ClampToEdge));
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)(tileY ? All.Repeat : All.ClampToEdge));
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
        public int Id { get; private set; }
        public Size Size { get; private set; }
        private bool dispose = true;

        public GLBitmap(int id, int width, int height, bool disp = true)
        {
            Id = id;
            Size = new Size(width, height);
            dispose = disp;
        }

        public void Dispose()
        {
            if (dispose)
                GL.DeleteTexture(Id);
        }
    }

    public class GLGraphics : IDisposable
    {
#if FAMISTUDIO_LINUX
        protected bool supportsLineWidth = false;
#endif
        protected bool antialiasing = false;
        protected float windowScaling = 1.0f;
        protected int windowSizeY;
        protected Rectangle scissor;
        protected Rectangle baseScissorRect;
        protected Vector4 transform = new Vector4(1, 1, 0, 0); // xy = scale, zw = translation
        protected Stack<Rectangle> clipStack = new Stack<Rectangle>();
        protected Stack<Vector4> transformStack = new Stack<Vector4>();
        protected Dictionary<Tuple<Color, int>, GLBrush> verticalGradientCache = new Dictionary<Tuple<Color, int>, GLBrush>();
        public float WindowScaling => windowScaling;

        public GLGraphics()
        {
            windowScaling = GLTheme.MainWindowScaling;
        }

        public virtual void BeginDraw(Rectangle unflippedControlRect, int windowSizeY)
        {
#if FAMISTUDIO_LINUX
            var lineWidths = new float[2];
            GL.GetFloat(GetPName.LineWidthRange, lineWidths);
            supportsLineWidth = lineWidths[1] > 1.0f;
#endif

            this.windowSizeY = windowSizeY;

            var controlRect = FlipRectangleY(unflippedControlRect);
            baseScissorRect = unflippedControlRect;
            GL.Viewport(controlRect.Left, controlRect.Top, controlRect.Width, controlRect.Height);

            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            GL.Ortho(0, unflippedControlRect.Width, unflippedControlRect.Height, 0, -1, 1);
            GL.Disable(EnableCap.CullFace);
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            GL.Enable(EnableCap.Blend);

            transform = new Vector4(1, 1, 0, 0);
            scissor = controlRect;
            GL.Enable(EnableCap.ScissorTest);
            GL.Scissor(scissor.Left, scissor.Top, scissor.Width, scissor.Height);
        }

        public virtual void EndDraw()
        {
        }

        protected Rectangle FlipRectangleY(Rectangle rc)
        {
            return new Rectangle(
                rc.Left,
                windowSizeY - rc.Top - rc.Height,
                rc.Width,
                rc.Height);
        }

        protected void AddHalfPixelOffset(float x = 0.5f, float y = 0.5f)
        {
            GL.GetFloat(GetPName.ModelviewMatrix, out Matrix4 matrix);
            matrix.Row3.X += x;
            matrix.Row3.Y += y;
            GL.LoadMatrix(ref matrix);
        }

        public bool AntiAliasing
        {
            get { return antialiasing; }
            set { antialiasing = value; }
        }

        public void PushTranslation(float x, float y)
        {
            transformStack.Push(transform);
            transform.Z += x;
            transform.W += y;

            GL.PushMatrix();
            GL.Translate(x, y, 0);
        }
 
        public void PushTransform(float tx, float ty, float sx, float sy)
        {
            transformStack.Push(transform);

            transform.X *= sx;
            transform.Y *= sy;
            transform.Z += tx;
            transform.W += ty;

            GL.PushMatrix();
            GL.Translate(tx, ty, 0);
            GL.Scale(sx, sy, 0);
        }

        public void PopTransform()
        {
            GL.PopMatrix();

            transform = transformStack.Pop();
        }

        public void PushClip(int x0, int y0, int x1, int y1)
        {
            // OpenGL 1.1 doesnt support multiple scissor rects, but for
            // our purpose, simply intersecting the rects does the job.
            clipStack.Push(scissor);
            scissor = new Rectangle(
                (int)(transform.Z + baseScissorRect.Left + x0),
                (int)(transform.W + baseScissorRect.Top  + y0),
                x1 - x0,
                y1 - y0);
            scissor = FlipRectangleY(scissor);
            scissor.Intersect(clipStack.Peek());
            GL.Scissor(scissor.Left, scissor.Top, scissor.Width, scissor.Height);
        }

        public void PopClip()
        {
            scissor = clipStack.Pop();
            GL.Scissor(scissor.Left, scissor.Top, scissor.Width, scissor.Height);
        }

        public void Clear(Color color)
        {
            GL.ClearColor(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f, color.A / 255.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit);
        }

        public void DrawBitmap(GLBitmap bmp, float x, float y, float opacity = 1.0f)
        {
            DrawBitmap(bmp, x, y, bmp.Size.Width, bmp.Size.Height, opacity);
        }

        public void DrawBitmap(GLBitmap bmp, float x, float y, float width, float height, float opacity)
        {
            int x0 = (int)x;
            int y0 = (int)y;
            int x1 = (int)(x + width);
            int y1 = (int)(y + height);

            GL.Enable(EnableCap.Texture2D);
            GL.BindTexture(TextureTarget.Texture2D, bmp.Id);
            GL.Color4(1.0f, 1.0f, 1.0f, opacity);

            GL.Begin(PrimitiveType.Quads);
            GL.TexCoord2(0, 0); GL.Vertex2(x0, y0);
            GL.TexCoord2(1, 0); GL.Vertex2(x1, y0);
            GL.TexCoord2(1, 1); GL.Vertex2(x1, y1);
            GL.TexCoord2(0, 1); GL.Vertex2(x0, y1);
            GL.End();

            GL.Disable(EnableCap.Texture2D);
        }

        // HACK : Very specific call only used by video rendering, too lazy to do the proper transforms.
        public void DrawRotatedFlippedBitmap(GLBitmap bmp, float x, float y, float width, float height)
        {
            GL.Enable(EnableCap.Texture2D);
            GL.BindTexture(TextureTarget.Texture2D, bmp.Id);
            GL.Color4(1.0f, 1.0f, 1.0f, 1.0f);

            GL.Begin(PrimitiveType.Quads);
            GL.TexCoord2(0, 0); GL.Vertex2(x - height, y);
            GL.TexCoord2(1, 0); GL.Vertex2(x - height, y - width);
            GL.TexCoord2(1, 1); GL.Vertex2(x, y - width);
            GL.TexCoord2(0, 1); GL.Vertex2(x, y );
            GL.End();

            GL.Disable(EnableCap.Texture2D);
        }
        
        public void DrawText(string text, GLFont font, float startX, float startY, GLBrush brush, float width = 1000)
        {
            if (string.IsNullOrEmpty(text))
                return;

            int alignmentOffsetX = 0;
            if (font.Alignment != 0)
            {
                font.MeasureString(text, out int minX, out int maxX);

                if (font.Alignment == 1)
                {
                    alignmentOffsetX -= minX;
                    alignmentOffsetX += ((int)width - maxX - minX) / 2;
                }
                else
                {
                    alignmentOffsetX -= minX;
                    alignmentOffsetX += ((int)width - maxX - minX);
                }
            }

            var vertices  = new float[text.Length * 4, 2];
            var texCoords = new float[text.Length * 4, 2];

            int x = (int)(startX + alignmentOffsetX);
            int y = (int)(startY + font.OffsetY);

            for (int i = 0; i < text.Length; i++)
            {
                var c0 = text[i];
                var info = font.GetCharInfo(c0);

                int x0 = x + info.xoffset;
                int y0 = y + info.yoffset;
                int x1 = x0 + info.width;
                int y1 = y0 + info.height;

                vertices[i * 4 + 0, 0] = x0; vertices[i * 4 + 0, 1] = y0;
                vertices[i * 4 + 1, 0] = x1; vertices[i * 4 + 1, 1] = y0;
                vertices[i * 4 + 2, 0] = x1; vertices[i * 4 + 2, 1] = y1;
                vertices[i * 4 + 3, 0] = x0; vertices[i * 4 + 3, 1] = y1;

                texCoords[i * 4 + 0, 0] = info.u0; texCoords[i * 4 + 0, 1] = info.v0;
                texCoords[i * 4 + 1, 0] = info.u1; texCoords[i * 4 + 1, 1] = info.v0;
                texCoords[i * 4 + 2, 0] = info.u1; texCoords[i * 4 + 2, 1] = info.v1;
                texCoords[i * 4 + 3, 0] = info.u0; texCoords[i * 4 + 3, 1] = info.v1;

                x += info.xadvance;
                if (i != text.Length - 1)
                {
                    char c1 = text[i + 1];
                    x += font.GetKerning(c0, c1);
                }
            }

            GL.Enable(EnableCap.Texture2D);
            GL.BindTexture(TextureTarget.Texture2D, font.Texture);
            GL.Color4(brush.Color0.R, brush.Color0.G, brush.Color0.B, (byte)255);
            GL.EnableClientState(ArrayCap.VertexArray);
            GL.EnableClientState(ArrayCap.TextureCoordArray);
            GL.VertexPointer(2, VertexPointerType.Float, 0, vertices);
            GL.TexCoordPointer(2, TexCoordPointerType.Float, 0, texCoords);
            GL.DrawArrays(PrimitiveType.Quads, 0, vertices.GetLength(0));
            GL.DisableClientState(ArrayCap.TextureCoordArray);
            GL.DisableClientState(ArrayCap.VertexArray);
            GL.Disable(EnableCap.Texture2D);
        }

        public float MeasureString(string text, GLFont font)
        {
            font.MeasureString(text, out int minX, out int maxX);
            return maxX - minX;
        }

        public void DrawLine(float x0, float y0, float x1, float y1, GLBrush brush, float width = 1.0f)
        {
            GL.PushMatrix();
            AddHalfPixelOffset();
            GL.Color4(brush.Color0);
            if (antialiasing)
                GL.Enable(EnableCap.LineSmooth);
#if FAMISTUDIO_LINUX
            if (!supportsLineWidth && width > 1)
            {
                DrawThickLineAsPolygon(new[] {
                    x0, y0,
                    x1, y1 }, brush, width);
            }
            else
#endif
            {
                if (brush.IsBitmap)
                {
                    GL.Enable(EnableCap.Texture2D);
                    GL.BindTexture(TextureTarget.Texture2D, brush.Bitmap.Id);
                    GL.LineWidth(width);
                    GL.Begin(PrimitiveType.Lines);

                    var size = brush.Bitmap.Size;
                    GL.TexCoord2((x0 + 0.5f) / size.Width, (y0 + 0.5f) / size.Height);
                    GL.Vertex2(x0, y0);
                    GL.TexCoord2((x1 + 0.5f) / size.Width, (y1 + 0.5f) / size.Height);
                    GL.Vertex2(x1, y1);

                    GL.End();
                    GL.Disable(EnableCap.Texture2D);
                }
                else
                {
                    GL.LineWidth(width);
                    GL.Begin(PrimitiveType.Lines);
                    GL.Vertex2(x0, y0);
                    GL.Vertex2(x1, y1);
                    GL.End();
                }
            }
            if (antialiasing)
                GL.Disable(EnableCap.LineSmooth);
            GL.PopMatrix();
        }

        public void DrawLine(float[,] points, GLBrush brush)
        {
            for (int i = 0; i < points.GetLength(0) - 1; i++)
            {
                DrawLine(points[i + 0, 0], points[i + 0, 1], points[i + 1, 0], points[i + 1, 1], brush);
            }
        }

        public void DrawRectangle(RectangleF rect, GLBrush brush, float width = 1.0f)
        {
            DrawRectangle(rect.Left, rect.Top, rect.Right, rect.Bottom, brush, width);
        }

        public void DrawRectangle(float x0, float y0, float x1, float y1, GLBrush brush, float width = 1.0f, bool miter = false)
        {
            GL.PushMatrix();

            AddHalfPixelOffset();

            if (width > 1)
                GL.Enable(EnableCap.LineSmooth);

            GL.Color4(brush.Color0);
#if FAMISTUDIO_LINUX
            if (!supportsLineWidth && width > 1)
            {
                DrawThickLineAsPolygon(new[] {
                    x0, y0,
                    x1, y0,
                    x1, y1,
                    x0, y1,
                    x0, y0 }, brush, width);
            }
            else
#endif
            {
                if (brush.IsBitmap)
                {
                    GL.Enable(EnableCap.Texture2D);
                    GL.BindTexture(TextureTarget.Texture2D, brush.Bitmap.Id);
                    GL.LineWidth(width);
                    GL.Begin(PrimitiveType.LineLoop);

                    var size = brush.Bitmap.Size;
                    GL.TexCoord2((x0 + 0.5f) / size.Width, (y0 + 0.5f) / size.Height);
                    GL.Vertex2(x0, y0);
                    GL.TexCoord2((x1 + 0.5f) / size.Width, (y0 + 0.5f) / size.Height);
                    GL.Vertex2(x1, y0);
                    GL.TexCoord2((x1 + 0.5f) / size.Width, (y1 + 0.5f) / size.Height);
                    GL.Vertex2(x1, y1);
                    GL.TexCoord2((x0 + 0.5f) / size.Width, (y1 + 0.5f) / size.Height);
                    GL.Vertex2(x0, y1);

                    GL.End();
                    GL.Disable(EnableCap.Texture2D);
                }
                else if (miter)
                {
                    var pad = width * 0.5f;

                    GL.LineWidth(width);
                    GL.Begin(PrimitiveType.Lines);
                    GL.Vertex2(x0 - pad, y0); GL.Vertex2(x1 + pad, y0);
                    GL.Vertex2(x1, y0 - pad); GL.Vertex2(x1, y1 + pad);
                    GL.Vertex2(x1 + pad, y1); GL.Vertex2(x0 - pad, y1);
                    GL.Vertex2(x0, y1 + pad); GL.Vertex2(x0, y0 - pad);
                    GL.End();
                }
                else
                { 
                    GL.LineWidth(width);
                    GL.Begin(PrimitiveType.LineLoop);
                    GL.Vertex2(x0, y0);
                    GL.Vertex2(x1, y0);
                    GL.Vertex2(x1, y1);
                    GL.Vertex2(x0, y1);
                    GL.End();
                }
            }

            if (width > 1)
                GL.Disable(EnableCap.LineSmooth);

            GL.PopMatrix();
        }

        public void FillRectangle(RectangleF rect, GLBrush brush)
        {
            FillRectangle(rect.Left, rect.Top, rect.Right, rect.Bottom, brush);
        }

        public void FillRectangle(float x0, float y0, float x1, float y1, GLBrush brush)
        {
            if (!brush.IsGradient)
            {
                GL.Color4(brush.Color0);
                GL.Begin(PrimitiveType.Quads);
                GL.Vertex2(x0, y0);
                GL.Vertex2(x1, y0);
                GL.Vertex2(x1, y1);
                GL.Vertex2(x0, y1);
                GL.End();
            }
            else if (brush.GradientSizeX == (x1 - x0))
            {
                GL.Begin(PrimitiveType.Quads);
                GL.Color4(brush.Color0); GL.Vertex2(x0, y0);
                GL.Color4(brush.Color1); GL.Vertex2(x1, y0);
                GL.Color4(brush.Color1); GL.Vertex2(x1, y1);
                GL.Color4(brush.Color0); GL.Vertex2(x0, y1);
                GL.End();
            }
            else if (brush.GradientSizeY == (y1 - y0))
            {
                GL.Begin(PrimitiveType.Quads);
                GL.Color4(brush.Color0); GL.Vertex2(x0, y0);
                GL.Color4(brush.Color0); GL.Vertex2(x1, y0);
                GL.Color4(brush.Color1); GL.Vertex2(x1, y1);
                GL.Color4(brush.Color1); GL.Vertex2(x0, y1);
                GL.End();
            }
            else if (brush.GradientSizeY == 0.0f)
            {
                float xm = x0 + brush.GradientSizeX;

                GL.Begin(PrimitiveType.Quads);
                GL.Color4(brush.Color0); GL.Vertex2(x0, y0);
                GL.Color4(brush.Color1); GL.Vertex2(xm, y0);
                GL.Color4(brush.Color1); GL.Vertex2(xm, y1);
                GL.Color4(brush.Color0); GL.Vertex2(x0, y1);
                GL.Color4(brush.Color1); GL.Vertex2(xm, y0);
                GL.Color4(brush.Color1); GL.Vertex2(x1, y0);
                GL.Color4(brush.Color1); GL.Vertex2(x1, y1);
                GL.Color4(brush.Color1); GL.Vertex2(xm, y1);
                GL.End();
            }
            else if (brush.GradientSizeX == 0.0f)
            {
                float ym = y0 + brush.GradientSizeY;

                GL.Begin(PrimitiveType.Quads);
                GL.Color4(brush.Color0); GL.Vertex2(x0, y0);
                GL.Color4(brush.Color1); GL.Vertex2(x0, ym);
                GL.Color4(brush.Color1); GL.Vertex2(x1, ym);
                GL.Color4(brush.Color0); GL.Vertex2(x1, y0);
                GL.Color4(brush.Color1); GL.Vertex2(x0, ym);
                GL.Color4(brush.Color1); GL.Vertex2(x0, y1);
                GL.Color4(brush.Color1); GL.Vertex2(x1, y1);
                GL.Color4(brush.Color1); GL.Vertex2(x1, ym);
                GL.End();
            }
        }

        public void FillAndDrawRectangle(float x0, float y0, float x1, float y1, GLBrush fillBrush, GLBrush lineBrush, float width = 1.0f, bool miter = false)
        {
            FillRectangle(x0, y0, x1, y1, fillBrush);
            DrawRectangle(x0, y0, x1, y1, lineBrush, width, miter);
        }

        public GLGeometry CreateGeometry(float[,] points, bool closed = true)
        {
            return new GLGeometry(points, closed);
        }

        public void FillGeometry(GLGeometry geo, GLBrush brush, bool smooth = false)
        {
            if (!brush.IsGradient)
            {
                if (smooth)
                    GL.Enable(EnableCap.PolygonSmooth);
                GL.Color4(brush.Color0);
                GL.EnableClientState(ArrayCap.VertexArray);
                GL.VertexPointer(2, VertexPointerType.Float, 0, geo.Points);
                GL.DrawArrays(PrimitiveType.TriangleFan, 0, geo.Points.GetLength(0));
                GL.DisableClientState(ArrayCap.VertexArray);
                if (smooth)
                    GL.Disable(EnableCap.PolygonSmooth);
            }
            else
            {
                Debug.Assert(brush.GradientSizeX == 0.0f);

                GL.Begin(PrimitiveType.TriangleFan);
                for (int i = 0; i < geo.Points.GetLength(0); i++)
                {
                    float lerp = geo.Points[i, 1] / (float)brush.GradientSizeY;
                    byte r = (byte)(brush.Color0.R * (1.0f - lerp) + (brush.Color1.R * lerp));
                    byte g = (byte)(brush.Color0.G * (1.0f - lerp) + (brush.Color1.G * lerp));
                    byte b = (byte)(brush.Color0.B * (1.0f - lerp) + (brush.Color1.B * lerp));
                    byte a = (byte)(brush.Color0.A * (1.0f - lerp) + (brush.Color1.A * lerp));

                    GL.Color4(r, g, b, a);
                    GL.Vertex2(geo.Points[i, 0], geo.Points[i, 1]);
                }
                GL.End();
            }
        }

        public void DrawGeometry(GLGeometry geo, GLBrush brush, float lineWidth = 1.0f, bool miter = false)
        {
            GL.PushMatrix();

            AddHalfPixelOffset();

            GL.Enable(EnableCap.LineSmooth);
            GL.Color4(brush.Color0);
#if FAMISTUDIO_LINUX
            if (!supportsLineWidth && lineWidth > 1)
            {
                var pts = new float[geo.Points.Length * 2];
                for (int i = 0; i < geo.Points.GetLength(0); i++)
                {
                    pts[i * 2 + 0] = geo.Points[i, 0];
                    pts[i * 2 + 1] = geo.Points[i, 1];
                }

                DrawThickLineAsPolygon(pts, brush, lineWidth);
            }
            else
#endif
            {
                GL.LineWidth(lineWidth);
                GL.EnableClientState(ArrayCap.VertexArray);
                if (miter)
                {
                    var points = geo.GetMiterPoints(lineWidth);
                    GL.VertexPointer(2, VertexPointerType.Float, 0, points);
                    GL.DrawArrays(PrimitiveType.LineStrip, 0, points.GetLength(0));
                }
                else
                {
                    GL.VertexPointer(2, VertexPointerType.Float, 0, geo.Points);
                    GL.DrawArrays(PrimitiveType.LineStrip, 0, geo.Points.GetLength(0));
                }
                GL.DisableClientState(ArrayCap.VertexArray);
            }
            GL.Disable(EnableCap.LineSmooth);
            GL.PopMatrix();
        }

        protected void DrawThickLineAsPolygon(float[] points, GLBrush brush, float width)
        {
            GL.Begin(PrimitiveType.Quads);
            for (int i = 0; i < points.Length / 2 - 1; i++)
            {
                float x0 = points[(i + 0) * 2 + 0];
                float y0 = points[(i + 0) * 2 + 1];
                float x1 = points[(i + 1) * 2 + 0];
                float y1 = points[(i + 1) * 2 + 1];

                float dx = x1 - x0;
                float dy = y1 - y0;
                float invHalfWidth = (width * 0.5f) / (float)Math.Sqrt(dx * dx + dy * dy);
                dx *= invHalfWidth;
                dy *= invHalfWidth;

                GL.Vertex2(x0 + dy, y0 + dx);
                GL.Vertex2(x1 + dy, y1 + dx);
                GL.Vertex2(x1 - dy, y1 - dx);
                GL.Vertex2(x0 - dy, y0 - dx);
            }
            GL.End();
        }

        public void FillAndDrawGeometry(GLGeometry geo, GLBrush fillBrush, GLBrush lineBrush, float lineWidth = 1.0f, bool miter = true)
        {
            FillGeometry(geo, fillBrush);
            DrawGeometry(geo, lineBrush, lineWidth, miter);
        }

        public unsafe GLBitmap CreateBitmap(int width, int height, uint[] data)
        {
            fixed (uint* ptr = &data[0])
            {
                int id = GL.GenTexture();
                GL.BindTexture(TextureTarget.Texture2D, id);
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, width, height, 0, PixelFormat.Bgra, PixelType.UnsignedByte, new IntPtr(ptr));
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)All.Nearest);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)All.Nearest);
                return new GLBitmap(id, width, height);
            }
        }

        public GLBrush CreateSolidBrush(Color color)
        {
            return new GLBrush(color);
        }

        public GLBrush CreateBitmapBrush(GLBitmap bmp, bool tileX, bool tileY)
        {
            return new GLBrush(bmp, tileX, tileY);
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

#if FAMISTUDIO_WINDOWS
        public int CreateGLTexture(System.Drawing.Bitmap bmp)
        {
            var bmpData =
                bmp.LockBits(
                    new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height),
                    System.Drawing.Imaging.ImageLockMode.ReadOnly,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            Debug.Assert(bmpData.Stride == bmp.Width * 4);

            int id = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, id);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, bmp.Width, bmp.Height, 0, PixelFormat.Bgra, PixelType.UnsignedByte, bmpData.Scan0);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)All.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)All.Nearest);

            bmp.UnlockBits(bmpData);

            return id;
        }
#else
        public int CreateGLTexture(Gdk.Pixbuf pixbuf)
        {
            Debug.Assert(pixbuf.Rowstride == pixbuf.Width * 4);

            int id = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, id);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, pixbuf.Width, pixbuf.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, pixbuf.Pixels);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)All.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)All.Nearest);

            return id;
        }
#endif

#if FAMISTUDIO_WINDOWS
        public GLBitmap CreateBitmapFromResource(string name)
        {
            var assembly = Assembly.GetExecutingAssembly();

            bool needsScaling = false;
            System.Drawing.Bitmap bmp;

            if (windowScaling == 1.5f && assembly.GetManifestResourceInfo($"FamiStudio.Resources.{name}@15x.png") != null)
            {
                bmp = System.Drawing.Image.FromStream(assembly.GetManifestResourceStream($"FamiStudio.Resources.{name}@15x.png")) as System.Drawing.Bitmap;
            }
            else if (windowScaling > 1.0f && assembly.GetManifestResourceInfo($"FamiStudio.Resources.{name}@2x.png") != null)
            {
                bmp = System.Drawing.Image.FromStream(assembly.GetManifestResourceStream($"FamiStudio.Resources.{name}@2x.png")) as System.Drawing.Bitmap;
                needsScaling = windowScaling != 2.0f;
            }
            else
            {
                bmp = System.Drawing.Image.FromStream(assembly.GetManifestResourceStream($"FamiStudio.Resources.{name}.png")) as System.Drawing.Bitmap;
            }

            // Pre-resize all images so we dont have to deal with scaling later.
            if (needsScaling)
            {
                var newWidth = Math.Max(1, (int)(bmp.Width * (windowScaling / 2.0f)));
                var newHeight = Math.Max(1, (int)(bmp.Height * (windowScaling / 2.0f)));

                bmp = new System.Drawing.Bitmap(bmp, newWidth, newHeight);
            }

            return new GLBitmap(CreateGLTexture(bmp), bmp.Width, bmp.Height);
        }
#else
        public GLBitmap CreateBitmapFromResource(string name)
        {
            var assembly = Assembly.GetExecutingAssembly();

            bool needsScaling = false;
            Gdk.Pixbuf pixbuf = null;

            if (GLTheme.MainWindowScaling == 1.5f && assembly.GetManifestResourceInfo($"FamiStudio.Resources.{name}@15x.png") != null)
            {
                pixbuf = Gdk.Pixbuf.LoadFromResource($"FamiStudio.Resources.{name}@15x.png");
            }
            else if (GLTheme.MainWindowScaling > 1.0f && assembly.GetManifestResourceInfo($"FamiStudio.Resources.{name}@2x.png") != null)
            {
                pixbuf = Gdk.Pixbuf.LoadFromResource($"FamiStudio.Resources.{name}@2x.png");
                needsScaling = GLTheme.MainWindowScaling != 2.0f;
            }
            else
            {
                pixbuf = Gdk.Pixbuf.LoadFromResource($"FamiStudio.Resources.{name}.png");
            }

            // Pre-resize all images so we dont have to deal with scaling later.
            if (needsScaling)
            {
                var newWidth  = Math.Max(1, (int)(pixbuf.Width  * (windowScaling / 2.0f)));
                var newHeight = Math.Max(1, (int)(pixbuf.Height * (windowScaling / 2.0f)));

                pixbuf = pixbuf.ScaleSimple(newWidth, newHeight, Gdk.InterpType.Bilinear);
            }

            return new GLBitmap(CreateGLTexture(pixbuf), pixbuf.Width, pixbuf.Height);
        }
#endif

        public GLBitmap CreateBitmapFromOffscreenGraphics(GLOffscreenGraphics g)
        {
            return new GLBitmap(g.Texture, g.SizeX, g.SizeY, false);
        }

        public float GetBitmapWidth(GLBitmap bmp)
        {
            return bmp.Size.Width;
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

#if FAMISTUDIO_WINDOWS
        public GLFont CreateFont(System.Drawing.Bitmap bmp, string[] def, int size, int alignment, bool ellipsis, int existingTexture = -1)
#else
        public GLFont CreateFont(Gdk.Pixbuf bmp, string[] def, int size, int alignment, bool ellipsis, int existingTexture = -1)
#endif
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
                                glTex = CreateGLTexture(bmp);

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
    };

    public class GLOffscreenGraphics : GLGraphics
    {
        protected int fbo;
        protected int texture;
        protected int resX;
        protected int resY;

        public int Texture => texture;
        public int SizeX => resX;
        public int SizeY => resY;

        private GLOffscreenGraphics(int imageSizeX, int imageSizeY, bool allowReadback)
        {
            resX = imageSizeX;
            resY = imageSizeY;

            texture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, texture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, imageSizeX, imageSizeY, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToBorder);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToBorder);

            fbo = GL.Ext.GenFramebuffer();
            GL.Ext.BindFramebuffer(FramebufferTarget.FramebufferExt, fbo);
            GL.Ext.FramebufferTexture2D(FramebufferTarget.FramebufferExt, FramebufferAttachment.ColorAttachment0Ext, TextureTarget.Texture2D, texture, 0);
            GL.Ext.BindFramebuffer(FramebufferTarget.FramebufferExt, 0);
        }

        public static GLOffscreenGraphics Create(int imageSizeX, int imageSizeY, bool allowReadback)
        {
            return new GLOffscreenGraphics(imageSizeX, imageSizeY, allowReadback);
        }

        public override void BeginDraw(Rectangle unflippedControlRect, int windowSizeY)
        {
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, fbo);
            GL.DrawBuffer(DrawBufferMode.ColorAttachment0);

            base.BeginDraw(unflippedControlRect, windowSizeY);
        }

        public override void EndDraw()
        {
            base.EndDraw();

            GL.Ext.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);
        }

        public unsafe void GetBitmap(byte[] data)
        {
            byte[] tmp = new byte[data.Length];

            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, fbo);
            fixed (byte* tmpPtr = &tmp[0])
            {
                GL.ReadPixels(0, 0, resX, resY, PixelFormat.Bgra, PixelType.UnsignedByte, new IntPtr(tmpPtr));
                GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, 0);

                // Flip image vertically to match D3D. 
                for (int y = 0; y < resY; y++)
                {
                    int y0 = y;
                    int y1 = resY - y - 1;

                    y0 *= resX * 4;
                    y1 *= resX * 4;

                    // ABGR -> RGBA
                    byte* p = tmpPtr + y0; 
                    for (int x = 0; x < resX * 4; x += 4)
                    {
                        data[y1 + x + 3] = *p++;
                        data[y1 + x + 2] = *p++;
                        data[y1 + x + 1] = *p++;
                        data[y1 + x + 0] = *p++;
                    }
                }
            }
        }

        public override void Dispose()
        {
            if (texture != 0) GL.DeleteTextures(1, ref texture);
            if (fbo != 0) GL.Ext.DeleteFramebuffers(1, ref fbo);

            base.Dispose();
        }
    };
}