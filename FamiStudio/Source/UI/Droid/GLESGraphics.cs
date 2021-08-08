using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using Android.Opengl;
using Java.Nio;
using Javax.Microedition.Khronos.Opengles;

namespace FamiStudio
{
    public class GLGraphics : IDisposable
    {
        protected IGL10 gl;
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

        protected FloatBuffer vtxBuffer;
        protected FloatBuffer texBuffer;
        protected IntBuffer   colBuffer;
        protected ShortBuffer idxBuffer;

        const int MaxVertexCount   = 4096;
        const int MaxTexCoordCount = 4096;
        const int MaxColorCount    = 4096;
        const int MaxIndexCount    = MaxVertexCount / 4 * 6;

        protected float[] tmpVtxArray = new float[MaxVertexCount * 2];
        protected float[] tmpTexArray = new float[MaxTexCoordCount * 2];
        protected int[]   tmpColArray = new int[MaxColorCount * 2];

        // DROIDTODO!
        static readonly float[] HalfPixelOffsetMatrix = new[]
        {
            1.0f, 0.0f, 0.0f, 0.0f,
            0.0f, 1.0f, 0.0f, 0.0f,
            0.0f, 0.0f, 1.0f, 0.0f,
            0.5f, 0.5f, 0.0f, 1.0f,
        };

        public GLGraphics(IGL10 gl)
        {
            this.gl = gl;
            this.windowScaling = GLTheme.MainWindowScaling;

            vtxBuffer = ByteBuffer.AllocateDirect(sizeof(float) * MaxVertexCount * 2).Order(ByteOrder.NativeOrder()).AsFloatBuffer();
            texBuffer = ByteBuffer.AllocateDirect(sizeof(float) * MaxTexCoordCount * 2).Order(ByteOrder.NativeOrder()).AsFloatBuffer();
            colBuffer = ByteBuffer.AllocateDirect(sizeof(int) * MaxColorCount).Order(ByteOrder.NativeOrder()).AsIntBuffer();
            idxBuffer = ByteBuffer.AllocateDirect(sizeof(short) * MaxIndexCount).Order(ByteOrder.NativeOrder()).AsShortBuffer();

            for (int i = 0; i < MaxVertexCount; i += 4)
            {
                var i0 = (short)(i + 0);
                var i1 = (short)(i + 1);
                var i2 = (short)(i + 2);
                var i3 = (short)(i + 3);

                // TODO : Pre-bake this.
                idxBuffer.Put(i0);
                idxBuffer.Put(i1);
                idxBuffer.Put(i2);
                idxBuffer.Put(i0);
                idxBuffer.Put(i2);
                idxBuffer.Put(i3);
            }

            idxBuffer.Position(0);
        }

        public virtual void BeginDraw(Rectangle unflippedControlRect, int windowSizeY)
        {
            this.windowSizeY = windowSizeY;

            var controlRect = FlipRectangleY(unflippedControlRect);
            baseScissorRect = unflippedControlRect;
            gl.GlViewport(controlRect.Left, controlRect.Top, controlRect.Width, controlRect.Height);

            gl.GlMatrixMode(GLES11.GlProjection);
            gl.GlLoadIdentity();
            gl.GlOrthof(0, unflippedControlRect.Width, unflippedControlRect.Height, 0, -1, 1);
            gl.GlDisable((int)2884); // Cull face?
            gl.GlDisable(GLES11.GlDepthTest);
            gl.GlDisable(GLES11.GlStencilTest);
            gl.GlMatrixMode(GLES11.GlModelview);
            gl.GlLoadIdentity();
            gl.GlBlendFunc(GLES11.GlSrcAlpha, GLES11.GlOneMinusSrcAlpha);
            gl.GlEnable(GLES11.GlBlend);
            gl.GlEnableClientState(GLES11.GlVertexArray);

            transform = new Vector4(1, 1, 0, 0);
            scissor = controlRect;
            gl.GlEnable(GLES11.GlScissorTest); 
            gl.GlScissor(scissor.Left, scissor.Top, scissor.Width, scissor.Height);
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
            gl.GlTranslatef(x, y, 0.0f);
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

            gl.GlPushMatrix();
            gl.GlTranslatef(x, y, 0);
        }

        public void PushTransform(float tx, float ty, float sx, float sy)
        {
            transformStack.Push(transform);

            transform.X *= sx;
            transform.Y *= sy;
            transform.Z += tx;
            transform.W += ty;

            gl.GlPushMatrix();
            gl.GlTranslatef(tx, ty, 0);
            gl.GlScalef(sx, sy, 0);
        }

        public void PopTransform()
        {
            gl.GlPopMatrix();

            transform = transformStack.Pop();
        }

        public void PushClip(int x0, int y0, int x1, int y1)
        {
            // OpenGL 1.1 doesnt support multiple scissor rects, but for
            // our purpose, simply intersecting the rects does the job.
            clipStack.Push(scissor);
            scissor = new Rectangle(
                (int)(transform.Z + baseScissorRect.Left + x0),
                (int)(transform.W + baseScissorRect.Top + y0),
                x1 - x0,
                y1 - y0);
            scissor = FlipRectangleY(scissor);
            scissor.Intersect(clipStack.Peek());

            gl.GlScissor(scissor.Left, scissor.Top, scissor.Width, scissor.Height);
        }

        public void PopClip()
        {
            scissor = clipStack.Pop();
            gl.GlScissor(scissor.Left, scissor.Top, scissor.Width, scissor.Height);
        }

        public void Clear(Color color)
        {
            gl.GlClearColor(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f, color.A / 255.0f);
            gl.GlClear(GLES11.GlColorBufferBit);
        }

        public void DrawBitmap(GLBitmap bmp, float x, float y, float opacity = 1.0f)
        {
            DrawBitmap(bmp, x, y, bmp.Size.Width, bmp.Size.Height, opacity);
        }

        public unsafe void DrawBitmap(GLBitmap bmp, float x, float y, float width, float height, float opacity)
        {
            int x0 = (int)x;
            int y0 = (int)y;
            int x1 = (int)(x + width);
            int y1 = (int)(y + height);

            tmpVtxArray[0] = x0;
            tmpVtxArray[1] = y0;
            tmpVtxArray[2] = x1;
            tmpVtxArray[3] = y0;
            tmpVtxArray[4] = x1;
            tmpVtxArray[5] = y1;
            tmpVtxArray[6] = x0;
            tmpVtxArray[7] = y1;

            tmpTexArray[0] = 0;
            tmpTexArray[1] = 0;
            tmpTexArray[2] = 1;
            tmpTexArray[3] = 0;
            tmpTexArray[4] = 1;
            tmpTexArray[5] = 1;
            tmpTexArray[6] = 0;
            tmpTexArray[7] = 1;

            vtxBuffer.Put(tmpVtxArray, 0, 4 * 2);
            texBuffer.Put(tmpTexArray, 0, 4 * 2);
            vtxBuffer.Position(0);
            texBuffer.Position(0);

            gl.GlEnable(GLES11.GlTexture2d);
            gl.GlBindTexture(GLES11.GlTexture2d, bmp.Id);
            gl.GlColor4f(1, 1, 1, opacity);
            gl.GlEnableClientState(GLES11.GlTextureCoordArray);
            gl.GlVertexPointer(2, GLES11.GlFloat, 0, vtxBuffer);
            gl.GlTexCoordPointer(2, GLES11.GlFloat, 0, texBuffer);
            gl.GlDrawArrays(GLES11.GlTriangleFan, 0, 4);
            gl.GlDisableClientState(GLES11.GlTextureCoordArray);
            gl.GlDisable(GLES11.GlTexture2d);
        }

        // HACK : Very specific call only used by video rendering, too lazy to do the proper transforms.
        public void DrawRotatedFlippedBitmap(GLBitmap bmp, float x, float y, float width, float height)
        {
            /*
            GL.Enable(EnableCap.Texture2D);
            GL.BindTexture(TextureTarget.Texture2D, bmp.Id);
            GL.Color4(1.0f, 1.0f, 1.0f, 1.0f);

            GL.Begin(PrimitiveType.Quads);
            GL.TexCoord2(0, 0); GL.Vertex2(x - height, y);
            GL.TexCoord2(1, 0); GL.Vertex2(x - height, y - width);
            GL.TexCoord2(1, 1); GL.Vertex2(x, y - width);
            GL.TexCoord2(0, 1); GL.Vertex2(x, y);
            GL.End();

            GL.Disable(EnableCap.Texture2D);
            */
        }

        public unsafe void DrawText(string text, GLFont font, float startX, float startY, GLBrush brush, float width = 1000)
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

                tmpVtxArray[(i * 4 + 0) * 2 + 0] = x0; tmpVtxArray[(i * 4 + 0) * 2 + 1] = y0;
                tmpVtxArray[(i * 4 + 1) * 2 + 0] = x1; tmpVtxArray[(i * 4 + 1) * 2 + 1] = y0;
                tmpVtxArray[(i * 4 + 2) * 2 + 0] = x1; tmpVtxArray[(i * 4 + 2) * 2 + 1] = y1;
                tmpVtxArray[(i * 4 + 3) * 2 + 0] = x0; tmpVtxArray[(i * 4 + 3) * 2 + 1] = y1;

                tmpTexArray[(i * 4 + 0) * 2 + 0] = info.u0; tmpTexArray[(i * 4 + 0) * 2 + 1] = info.v0;
                tmpTexArray[(i * 4 + 1) * 2 + 0] = info.u1; tmpTexArray[(i * 4 + 1) * 2 + 1] = info.v0;
                tmpTexArray[(i * 4 + 2) * 2 + 0] = info.u1; tmpTexArray[(i * 4 + 2) * 2 + 1] = info.v1;
                tmpTexArray[(i * 4 + 3) * 2 + 0] = info.u0; tmpTexArray[(i * 4 + 3) * 2 + 1] = info.v1;

                x += info.xadvance;
                if (i != text.Length - 1)
                {
                    char c1 = text[i + 1];
                    x += font.GetKerning(c0, c1);
                }
            }

            vtxBuffer.Put(tmpVtxArray, 0, text.Length * 4 * 2);
            texBuffer.Put(tmpTexArray, 0, text.Length * 4 * 2);
            vtxBuffer.Position(0);
            texBuffer.Position(0);

            gl.GlEnable(GLES11.GlTexture2d);
            gl.GlBindTexture(GLES11.GlTexture2d, font.Texture);
            gl.GlColor4f(brush.Color0.R / 255.0f, brush.Color0.G / 255.0f, brush.Color0.B / 255.0f, 1.0f);
            gl.GlEnableClientState(GLES11.GlTextureCoordArray);
            gl.GlVertexPointer(2, GLES11.GlFloat, 0, vtxBuffer);
            gl.GlTexCoordPointer(2, GLES11.GlFloat, 0, texBuffer);
            gl.GlDrawElements(GLES11.GlTriangles, text.Length * 6, GLES11.GlUnsignedShort, idxBuffer);
            gl.GlDisableClientState(GLES11.GlTextureCoordArray);
            gl.GlDisable(GLES11.GlTexture2d);
        }

        public float MeasureString(string text, GLFont font)
        {
            font.MeasureString(text, out int minX, out int maxX);
            return maxX - minX;
        }

        public unsafe void DrawLine(float x0, float y0, float x1, float y1, GLBrush brush, float width = 1.0f)
        {
            gl.GlPushMatrix();
            AddHalfPixelOffset();
            
            gl.GlColor4f(brush.Color0.R / 255.0f, brush.Color0.G / 255.0f, brush.Color0.B / 255.0f, brush.Color0.A / 255.0f);

            if (antialiasing)
                gl.GlEnable(GLES11.GlLineSmooth);

            if (brush.IsBitmap)
            {
                var size = brush.Bitmap.Size;

                tmpVtxArray[0] = x0;
                tmpVtxArray[1] = y0;
                tmpVtxArray[2] = x1;
                tmpVtxArray[3] = y1;

                tmpTexArray[0] = (x0 + 0.5f) / size.Width;
                tmpTexArray[1] = (y0 + 0.5f) / size.Height;
                tmpTexArray[2] = (x1 + 0.5f) / size.Width;
                tmpTexArray[3] = (y1 + 0.5f) / size.Height;

                vtxBuffer.Put(tmpVtxArray, 0, 4);
                vtxBuffer.Position(0);
                texBuffer.Put(tmpTexArray, 0, 4);
                texBuffer.Position(0);

                gl.GlEnable(GLES11.GlTexture2d);
                gl.GlBindTexture(GLES11.GlTexture2d, brush.Bitmap.Id);
                gl.GlLineWidth(width);
                gl.GlEnableClientState(GLES11.GlTextureCoordArray);
                gl.GlVertexPointer(2, GLES11.GlFloat, 0, vtxBuffer);
                gl.GlTexCoordPointer(2, GLES11.GlFloat, 0, texBuffer);
                gl.GlDrawArrays(GLES11.GlLines, 0, 2);
                gl.GlDisableClientState(GLES11.GlTextureCoordArray);
                gl.GlDisable(GLES11.GlTexture2d);
            }
            else
            {
                tmpVtxArray[0] = x0;
                tmpVtxArray[1] = y0;
                tmpVtxArray[2] = x1;
                tmpVtxArray[3] = y1;
                vtxBuffer.Put(tmpVtxArray, 0, 4);
                vtxBuffer.Position(0);

                gl.GlLineWidth(width);
                gl.GlVertexPointer(2, GLES11.GlFloat, 0, vtxBuffer);
                gl.GlDrawArrays(GLES11.GlLines, 0, 2);
            }

            if (antialiasing)
                gl.GlDisable(GLES11.GlLineSmooth);
            gl.GlPopMatrix();
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

        public unsafe void DrawRectangle(float x0, float y0, float x1, float y1, GLBrush brush, float width = 1.0f, bool miter = false)
        {
            gl.GlPushMatrix();

            AddHalfPixelOffset();

            if (width > 1)
                gl.GlEnable(GLES11.GlLineSmooth);

            gl.GlColor4f(brush.Color0.R / 255.0f, brush.Color0.G / 255.0f, brush.Color0.B / 255.0f, brush.Color0.A / 255.0f);
            {
                if (brush.IsBitmap)
                {
                    Debug.Assert(false);

                    /*
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
                    GL.Disable(EnableCap.Texture2D);*/
                }
                else if (miter)
                {
                    Debug.Assert(false);

                    var pad = width * 0.5f;

                    /*
                    GL.LineWidth(width);
                    GL.Begin(PrimitiveType.Lines);
                    GL.Vertex2(x0 - pad, y0); GL.Vertex2(x1 + pad, y0);
                    GL.Vertex2(x1, y0 - pad); GL.Vertex2(x1, y1 + pad);
                    GL.Vertex2(x1 + pad, y1); GL.Vertex2(x0 - pad, y1);
                    GL.Vertex2(x0, y1 + pad); GL.Vertex2(x0, y0 - pad);
                    GL.End();*/
                }
                else
                {
                    tmpVtxArray[0] = x0;
                    tmpVtxArray[1] = y0;
                    tmpVtxArray[2] = x1;
                    tmpVtxArray[3] = y0;
                    tmpVtxArray[4] = x1;
                    tmpVtxArray[5] = y1;
                    tmpVtxArray[6] = x0;
                    tmpVtxArray[7] = y1;
                    vtxBuffer.Put(tmpVtxArray, 0, 4 * 2);
                    vtxBuffer.Position(0);

                    gl.GlLineWidth(width);
                    gl.GlVertexPointer(2, GLES11.GlFloat, 0, vtxBuffer);
                    gl.GlDrawArrays(GLES11.GlLineLoop, 0, 4);
                }
            }

            if (width > 1)
                gl.GlDisable(GLES11.GlLineSmooth);

            gl.GlPopMatrix();
        }

        public void FillRectangle(RectangleF rect, GLBrush brush)
        {
            FillRectangle(rect.Left, rect.Top, rect.Right, rect.Bottom, brush);
        }

        protected int PackColor(Color c)
        {
            return (c.A << 24) | (c.B << 16) | (c.G << 8) | c.R;
        }

        protected int PackColor(int r, int g, int b, int a)
        {
            return (a << 24) | (b << 16) | (g << 8) | r;
        }

        public unsafe void FillRectangle(float x0, float y0, float x1, float y1, GLBrush brush)
        {
            if (!brush.IsGradient)
            {
                tmpVtxArray[0] = x0;
                tmpVtxArray[1] = y0;
                tmpVtxArray[2] = x1;
                tmpVtxArray[3] = y0;
                tmpVtxArray[4] = x1;
                tmpVtxArray[5] = y1;
                tmpVtxArray[6] = x0;
                tmpVtxArray[7] = y1;
                vtxBuffer.Put(tmpVtxArray, 0, 4 * 2);
                vtxBuffer.Position(0);

                gl.GlColor4f(brush.Color0.R / 255.0f, brush.Color0.G / 255.0f, brush.Color0.B / 255.0f, brush.Color0.A / 255.0f);
                gl.GlVertexPointer(2, GLES11.GlFloat, 0, vtxBuffer);
                gl.GlDrawArrays(GLES11.GlTriangleFan, 0, 4);
            }
            else if (brush.GradientSizeX == (x1 - x0))
            {
                tmpVtxArray[0] = x0;
                tmpVtxArray[1] = y0;
                tmpVtxArray[2] = x1;
                tmpVtxArray[3] = y0;
                tmpVtxArray[4] = x1;
                tmpVtxArray[5] = y1;
                tmpVtxArray[6] = x0;
                tmpVtxArray[7] = y1;

                tmpColArray[0] = PackColor(brush.Color0);
                tmpColArray[1] = PackColor(brush.Color1);
                tmpColArray[2] = PackColor(brush.Color1);
                tmpColArray[3] = PackColor(brush.Color0);

                vtxBuffer.Put(tmpVtxArray, 0, 8);
                vtxBuffer.Position(0);
                colBuffer.Put(tmpColArray, 0, 4);
                colBuffer.Position(0);

                gl.GlEnableClientState(GLES11.GlColorArray);
                gl.GlVertexPointer(2, GLES11.GlFloat, 0, vtxBuffer);
                gl.GlColorPointer(4, GLES11.GlUnsignedByte, 0, colBuffer);
                gl.GlDrawArrays(GLES11.GlTriangleFan, 0, 4);
                gl.GlDisableClientState(GLES11.GlColorArray);
            }
            else if (brush.GradientSizeY == (y1 - y0))
            {
                tmpVtxArray[0] = x0;
                tmpVtxArray[1] = y0;
                tmpVtxArray[2] = x1;
                tmpVtxArray[3] = y0;
                tmpVtxArray[4] = x1;
                tmpVtxArray[5] = y1;
                tmpVtxArray[6] = x0;
                tmpVtxArray[7] = y1;

                tmpColArray[0] = PackColor(brush.Color0);
                tmpColArray[1] = PackColor(brush.Color0);
                tmpColArray[2] = PackColor(brush.Color1);
                tmpColArray[3] = PackColor(brush.Color1);

                vtxBuffer.Put(tmpVtxArray, 0, 8);
                vtxBuffer.Position(0);
                colBuffer.Put(tmpColArray, 0, 4);
                colBuffer.Position(0);

                gl.GlEnableClientState(GLES11.GlColorArray);
                gl.GlVertexPointer(2, GLES11.GlFloat, 0, vtxBuffer);
                gl.GlColorPointer(4, GLES11.GlUnsignedByte, 0, colBuffer);
                gl.GlDrawArrays(GLES11.GlTriangleFan, 0, 4);
                gl.GlDisableClientState(GLES11.GlColorArray);
            }
            else if (brush.GradientSizeY == 0.0f)
            {
                float xm = x0 + brush.GradientSizeX;

                tmpVtxArray[0]  = x0;
                tmpVtxArray[1]  = y0;
                tmpVtxArray[2]  = xm;
                tmpVtxArray[3]  = y0;
                tmpVtxArray[4]  = xm;
                tmpVtxArray[5]  = y1;
                tmpVtxArray[6]  = x0;
                tmpVtxArray[7]  = y1;
                tmpVtxArray[8]  = xm;
                tmpVtxArray[9]  = y0;
                tmpVtxArray[10] = x1;
                tmpVtxArray[11] = y0;
                tmpVtxArray[12] = x1;
                tmpVtxArray[13] = y1;
                tmpVtxArray[14] = xm;
                tmpVtxArray[15] = y1;

                tmpColArray[0] = PackColor(brush.Color0);
                tmpColArray[1] = PackColor(brush.Color1);
                tmpColArray[2] = PackColor(brush.Color1);
                tmpColArray[3] = PackColor(brush.Color0);
                tmpColArray[4] = PackColor(brush.Color1);
                tmpColArray[5] = PackColor(brush.Color1);
                tmpColArray[6] = PackColor(brush.Color1);
                tmpColArray[7] = PackColor(brush.Color1);

                vtxBuffer.Put(tmpVtxArray, 0, 16);
                vtxBuffer.Position(0);
                colBuffer.Put(tmpColArray, 0, 8);
                colBuffer.Position(0);

                gl.GlEnableClientState(GLES11.GlColorArray);
                gl.GlVertexPointer(2, GLES11.GlFloat, 0, vtxBuffer);
                gl.GlColorPointer(4, GLES11.GlUnsignedByte, 0, colBuffer);
                gl.GlDrawArrays(GLES11.GlTriangleFan, 0, 8);
                gl.GlDisableClientState(GLES11.GlColorArray);
            }
            else if (brush.GradientSizeX == 0.0f)
            {
                float ym = y0 + brush.GradientSizeY;

                tmpVtxArray[0]  = x0;
                tmpVtxArray[1]  = y0;
                tmpVtxArray[2]  = x0;
                tmpVtxArray[3]  = ym;
                tmpVtxArray[4]  = x1;
                tmpVtxArray[5]  = ym;
                tmpVtxArray[6]  = x1;
                tmpVtxArray[7]  = y0;
                tmpVtxArray[8]  = x0;
                tmpVtxArray[9]  = ym;
                tmpVtxArray[10] = x0;
                tmpVtxArray[11] = y1;
                tmpVtxArray[12] = x1;
                tmpVtxArray[13] = y1;
                tmpVtxArray[14] = x1;
                tmpVtxArray[15] = ym;

                tmpColArray[0] = PackColor(brush.Color0);
                tmpColArray[1] = PackColor(brush.Color1);
                tmpColArray[2] = PackColor(brush.Color1);
                tmpColArray[3] = PackColor(brush.Color0);
                tmpColArray[4] = PackColor(brush.Color1);
                tmpColArray[5] = PackColor(brush.Color1);
                tmpColArray[6] = PackColor(brush.Color1);
                tmpColArray[7] = PackColor(brush.Color1);

                vtxBuffer.Put(tmpVtxArray, 0, 16);
                vtxBuffer.Position(0);
                colBuffer.Put(tmpColArray, 0, 8);
                colBuffer.Position(0);

                gl.GlEnableClientState(GLES11.GlColorArray);
                gl.GlVertexPointer(2, GLES11.GlFloat, 0, vtxBuffer);
                gl.GlColorPointer(4, GLES11.GlUnsignedByte, 0, colBuffer);
                gl.GlDrawArrays(GLES11.GlTriangleFan, 0, 8);
                gl.GlDisableClientState(GLES11.GlColorArray);
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
                    gl.GlEnable(GLES11.GlPolygonSmoothHint);

                vtxBuffer.Put(geo.Points, 0, geo.Points.Length);
                vtxBuffer.Position(0);

                gl.GlColor4f(brush.Color0.R / 255.0f, brush.Color0.G / 255.0f, brush.Color0.B / 255.0f, brush.Color0.A / 255.0f);
                gl.GlVertexPointer(2, GLES11.GlFloat, 0, vtxBuffer);
                gl.GlDrawArrays(GLES11.GlTriangleFan, 0, geo.Points.Length / 2);

                if (smooth)
                    gl.GlDisable(GLES11.GlPolygonSmoothHint);
            }
            else
            {
                Debug.Assert(brush.GradientSizeX == 0.0f);

                var vi = 0;
                var ci = 0;

                for (int i = 0; i < geo.Points.Length / 2; i++)
                {
                    var lerp = geo.Points[i * 2 + 1] / (float)brush.GradientSizeY;
                    var r = (int)(brush.Color0.R * (1.0f - lerp) + (brush.Color1.R * lerp));
                    var g = (int)(brush.Color0.G * (1.0f - lerp) + (brush.Color1.G * lerp));
                    var b = (int)(brush.Color0.B * (1.0f - lerp) + (brush.Color1.B * lerp));
                    var a = (int)(brush.Color0.A * (1.0f - lerp) + (brush.Color1.A * lerp));

                    tmpVtxArray[vi++] = geo.Points[i * 2 + 0];
                    tmpVtxArray[vi++] = geo.Points[i * 2 + 1];
                    tmpTexArray[ci++] = PackColor(r, g, b, a);
                }

                vtxBuffer.Put(tmpVtxArray, 0, vi);
                vtxBuffer.Position(0);
                colBuffer.Put(tmpColArray, 0, ci);
                colBuffer.Position(0);

                gl.GlEnableClientState(GLES11.GlColorArray);
                gl.GlVertexPointer(2, GLES11.GlFloat, 0, vtxBuffer);
                gl.GlColorPointer(4, GLES11.GlUnsignedByte, 0, colBuffer);
                gl.GlDrawArrays(GLES11.GlTriangleFan, 0, geo.Points.Length);
                gl.GlDisableClientState(GLES11.GlColorArray);
            }
        }

        public void DrawGeometry(GLGeometry geo, GLBrush brush, float lineWidth = 1.0f, bool miter = false)
        {
            gl.GlPushMatrix();

            AddHalfPixelOffset();

            gl.GlEnable(GLES11.GlLineSmooth);
            gl.GlColor4f(brush.Color0.R / 255.0f, brush.Color0.G / 255.0f, brush.Color0.B / 255.0f, brush.Color0.A / 255.0f);

            {
                gl.GlLineWidth(lineWidth);
                
                if (miter)
                {
                    var points = geo.GetMiterPoints(lineWidth);

                    vtxBuffer.Put(points, 0, points.Length);
                    vtxBuffer.Position(0);

                    gl.GlVertexPointer(2, GLES11.GlFloat, 0, vtxBuffer);
                    gl.GlDrawArrays(GLES11.GlLineStrip, 0, points.Length / 2);
                }
                else
                {
                    vtxBuffer.Put(geo.Points, 0, geo.Points.Length);
                    vtxBuffer.Position(0);

                    gl.GlVertexPointer(2, GLES11.GlFloat, 0, vtxBuffer);
                    gl.GlDrawArrays(GLES11.GlLineStrip, 0, geo.Points.Length / 2);
                }
            }
            gl.GlDisable(GLES11.GlLineSmooth);
            gl.GlPopMatrix();
        }

        public void FillAndDrawGeometry(GLGeometry geo, GLBrush fillBrush, GLBrush lineBrush, float lineWidth = 1.0f, bool miter = false)
        {
            FillGeometry(geo, fillBrush);
            DrawGeometry(geo, lineBrush, lineWidth, miter);
        }

        public unsafe GLBitmap CreateBitmap(int width, int height, uint[] data)
        {
            /*
            fixed (uint* ptr = &data[0])
            {
                int id = GL.GenTexture();
                GL.BindTexture(TextureTarget.Texture2D, id);
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, width, height, 0, PixelFormat.Bgra, PixelType.UnsignedByte, new IntPtr(ptr));
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)All.Nearest);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)All.Nearest);
                return new GLBitmap(id, width, height);
            }
            */
            return null;
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

        public int CreateGLTexture(Android.Graphics.Bitmap bmp)
        {
            var id = new int[1];
            gl.GlGenTextures(1, id, 0);
            gl.GlBindTexture(GLES11.GlTexture2d, id[0]);
            GLUtils.TexImage2D(GLES11.GlTexture2d, 0, bmp, 0);
            gl.GlTexParameterx(GLES11.GlTexture2d, GLES11.GlTextureMinFilter, GLES11.GlNearest);
            gl.GlTexParameterx(GLES11.GlTexture2d, GLES11.GlTextureMagFilter, GLES11.GlNearest);
            bmp.Recycle();

            return id[0];
        }

        public GLBitmap CreateBitmapFromResource(string name)
        {
            Debug.WriteLine(name);

            var assembly = Assembly.GetExecutingAssembly();

            // DROIDTODO
            /*
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
            */

            var bmp = Android.Graphics.BitmapFactory.DecodeStream(
                typeof(GLTheme).Assembly.GetManifestResourceStream($"FamiStudio.Resources.{name}.png"), null,
                new Android.Graphics.BitmapFactory.Options() { InPremultiplied = false });

            return new GLBitmap(CreateGLTexture(bmp), bmp.Width, bmp.Height);
        }

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

        // TODO: Put this code in common somewhere.
        public GLFont CreateFont(Android.Graphics.Bitmap bmp, string[] def, int size, int alignment, bool ellipsis, int existingTexture = -1)
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

        private GLOffscreenGraphics(IGL10 gl, int imageSizeX, int imageSizeY, bool allowReadback) : base(gl)
        {
            resX = imageSizeX;
            resY = imageSizeY;

            /*
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
            */
        }

        public static GLOffscreenGraphics Create(/*IGL10 gl,*/ int imageSizeX, int imageSizeY, bool allowReadback)
        {
            return new GLOffscreenGraphics(null /*gl*/, imageSizeX, imageSizeY, allowReadback);
        }

        public override void BeginDraw(Rectangle unflippedControlRect, int windowSizeY)
        {
            /*
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, fbo);
            GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
            */

            base.BeginDraw(unflippedControlRect, windowSizeY);
        }

        public override void EndDraw()
        {
            base.EndDraw();

            // GL.Ext.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);
        }

        public unsafe void GetBitmap(byte[] data)
        {
            byte[] tmp = new byte[data.Length];

            /*
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
            */
        }

        public override void Dispose()
        {
            //if (texture != 0) GL.DeleteTextures(1, ref texture);
            //if (fbo != 0) GL.Ext.DeleteFramebuffers(1, ref fbo);

            base.Dispose();
        }
    };

    public struct Vector4
    {
        public Vector4(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public float X;
        public float Y;
        public float Z;
        public float W;
    }
}