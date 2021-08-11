using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using Android.Opengl;
using Java.Nio;
using Javax.Microedition.Khronos.Opengles;
using Bitmap = Android.Graphics.Bitmap;

namespace FamiStudio
{
    public class GLGraphics : GLGraphicsBase
    {
        protected IGL10 gl;

        public GLGraphics(IGL10 g)
        {
            gl = g;
            dashedBitmap = CreateBitmapFromResource("Dash");
            gl.GlTexParameterx(GLES11.GlTexture2d, GLES11.GlTextureWrapS, GLES11.GlRepeat);
            gl.GlTexParameterx(GLES11.GlTexture2d, GLES11.GlTextureWrapT, GLES11.GlRepeat);
        }

        public override void BeginDraw(Rectangle unflippedControlRect, int windowSizeY)
        {
            base.BeginDraw(unflippedControlRect, windowSizeY);

            gl.GlHint(GLES11.GlLineSmoothHint, GLES11.GlNicest);
            gl.GlViewport(controlRectFlip.Left, controlRectFlip.Top, controlRectFlip.Width, controlRectFlip.Height);
            gl.GlMatrixMode(GLES11.GlProjection);
            gl.GlLoadIdentity();
            gl.GlOrthof(0, unflippedControlRect.Width, unflippedControlRect.Height, 0, -1, 1);
            gl.GlDisable((int)2884); // Cull face?
            gl.GlMatrixMode(GLES11.GlModelview);
            gl.GlLoadIdentity();
            gl.GlBlendFunc(GLES11.GlSrcAlpha, GLES11.GlOneMinusSrcAlpha);
            gl.GlEnable(GLES11.GlBlend);
            gl.GlDisable(GLES11.GlDepthTest);
            gl.GlDisable(GLES11.GlStencilTest);
            gl.GlEnable(GLES11.GlScissorTest);
            gl.GlScissor(controlRectFlip.Left, controlRectFlip.Top, controlRectFlip.Width, controlRectFlip.Height);
            gl.GlEnableClientState(GLES11.GlVertexArray);
        }

        private void SetScissorRect(int x0, int y0, int x1, int y1)
        {
            var scissor = new Rectangle(controlRect.X + x0, controlRect.Y + y0, x1 - x0, y1 - y0);
            scissor = FlipRectangleY(scissor);
            gl.GlScissor(scissor.Left, scissor.Top, scissor.Width, scissor.Height);
        }

        private void ClearScissorRect()
        {
            gl.GlScissor(controlRectFlip.Left, controlRectFlip.Top, controlRectFlip.Width, controlRectFlip.Height);
        }

        public void Clear(Color color)
        {
            gl.GlClearColor(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f, color.A / 255.0f);
            gl.GlClear(GLES11.GlColorBufferBit);
        }

        public void UpdateBitmap(GLBitmap bmp, int x, int y, int width, int height, int[] data)
        {
            // DROIDTODO!
            //GL.BindTexture(TextureTarget.Texture2D, bmp.Id);
            //GL.TexSubImage2D(TextureTarget.Texture2D, 0, x, y, width, height, PixelFormat.Bgra, PixelType.UnsignedByte, data);
        }

        protected override int CreateEmptyTexture(int width, int height)
        {
            var id = new int[1];
            gl.GlGenTextures(1, id, 0);
            gl.GlBindTexture(GLES11.GlTexture2d, id[0]);
            gl.GlTexParameterx(GLES11.GlTexture2d, GLES11.GlTextureMinFilter, GLES11.GlNearest);
            gl.GlTexParameterx(GLES11.GlTexture2d, GLES11.GlTextureMagFilter, GLES11.GlNearest);
            gl.GlTexParameterx(GLES11.GlTexture2d, GLES11.GlTextureWrapS, GLES11.GlClampToEdge);
            gl.GlTexParameterx(GLES11.GlTexture2d, GLES11.GlTextureWrapT, GLES11.GlClampToEdge);

            var buffer = ByteBuffer.AllocateDirect(width * height * sizeof(int)).Order(ByteOrder.NativeOrder()).AsIntBuffer();
            buffer.Put(new int[width * height]);
            buffer.Position(0);

            gl.GlTexImage2D(GLES11.GlTexture2d, 0, GLES11.GlRgba, width, height, 0, GLES11.GlRgba, GLES11.GlUnsignedByte, buffer);

            return id[0];
        }

        protected override int CreateTexture(Bitmap bmp)
        {
            var id = new int[1];
            gl.GlGenTextures(1, id, 0);
            gl.GlBindTexture(GLES11.GlTexture2d, id[0]);
            GLUtils.TexImage2D(GLES11.GlTexture2d, 0, bmp, 0);
            gl.GlTexParameterx(GLES11.GlTexture2d, GLES11.GlTextureMinFilter, GLES11.GlNearest);
            gl.GlTexParameterx(GLES11.GlTexture2d, GLES11.GlTextureMagFilter, GLES11.GlNearest);
            gl.GlTexParameterx(GLES11.GlTexture2d, GLES11.GlTextureWrapS, GLES11.GlClampToEdge);
            gl.GlTexParameterx(GLES11.GlTexture2d, GLES11.GlTextureWrapT, GLES11.GlClampToEdge);
            bmp.Recycle();

            return id[0];
        }

        // DROIDTODO : Move to base class?
        protected Bitmap LoadBitmapFromResourceWithScaling(string name)
        {
            var assembly = Assembly.GetExecutingAssembly();

            bool needsScaling = false;
            Bitmap bmp;

            if (windowScaling == 1.5f && assembly.GetManifestResourceInfo($"FamiStudio.Resources.{name}@15x.png") != null)
            {
                bmp = PlatformUtils.LoadBitmapFromResource($"FamiStudio.Resources.{name}@15x.png");
            }
            else if (windowScaling > 1.0f && assembly.GetManifestResourceInfo($"FamiStudio.Resources.{name}@2x.png") != null)
            {
                bmp = PlatformUtils.LoadBitmapFromResource($"FamiStudio.Resources.{name}@2x.png");
                needsScaling = windowScaling != 2.0f;
            }
            else
            {
                bmp = PlatformUtils.LoadBitmapFromResource($"FamiStudio.Resources.{name}.png");
            }

            // DROIDTODO
            /*
            // Pre-resize all images so we dont have to deal with scaling later.
            if (needsScaling)
            {
                var newWidth  = Math.Max(1, (int)(bmp.Width  * (windowScaling / 2.0f)));
                var newHeight = Math.Max(1, (int)(bmp.Height * (windowScaling / 2.0f)));

#if FAMISTUDIO_WINDOWS
                bmp = new System.Drawing.Bitmap(bmp, newWidth, newHeight);
#else
                bmp = bmp.ScaleSimple(newWidth, newHeight, Gdk.InterpType.Bilinear);
#endif
            }
            */

            return bmp;
        }

        public GLBitmap CreateBitmapFromResource(string name)
        {
            var bmp = LoadBitmapFromResourceWithScaling(name);
            return new GLBitmap(CreateTexture(bmp), bmp.Width, bmp.Height);
        }

        private void ChangeBitmapBackground(Bitmap bmp, Color color)
        {
            // DROIDTODO.

            //for (int y = 0; y < bmp.Height; y++)
            //{
            //    for (int x = 0; x < bmp.Width; x++)
            //    {
            //        var pixel = bmp.GetPixel(x, y);

            //        var r = (byte)Utils.Lerp(color.R, pixel.R, pixel.A / 255.0f);
            //        var g = (byte)Utils.Lerp(color.G, pixel.G, pixel.A / 255.0f);
            //        var b = (byte)Utils.Lerp(color.B, pixel.B, pixel.A / 255.0f);

            //        bmp.SetPixel(x, y, new Android.Graphics.Color(r, g, b));
            //    }
            //}
        }

        public GLBitmapAtlas CreateBitmapAtlasFromResources(string[] names)
        {
            return CreateBitmapAtlasFromResources(names, Color.Empty);
        }

        public GLBitmapAtlas CreateBitmapAtlasFromResources(string[] names, Color backgroundOverride)
        {
            var bitmaps = new Bitmap[names.Length];
            var elementSizeX = 0;
            var elementSizeY = 0;

            for (int i = 0; i < names.Length; i++)
            {
                var bmp = LoadBitmapFromResourceWithScaling(names[i]);

                if (backgroundOverride != Color.Empty)
                    ChangeBitmapBackground(bmp, backgroundOverride);

                elementSizeX = Math.Max(elementSizeX, bmp.Width);
                elementSizeY = Math.Max(elementSizeY, bmp.Height);

                bitmaps[i] = bmp;
            }

            var numRows = Utils.DivideAndRoundUp(elementSizeX * names.Length, MaxAtlasResolution);
            var elementsPerRow = names.Length / numRows;
            var atlasSizeX = elementsPerRow * elementSizeX;
            var atlasSizeY = numRows * elementSizeY;
            var textureId = CreateEmptyTexture(atlasSizeX, atlasSizeY);
            var elementRects = new Rectangle[names.Length];

            gl.GlBindTexture(GLES11.GlTexture2d, textureId);

            for (int i = 0; i < names.Length; i++)
            {
                var bmp = bitmaps[i];

                var row = i / elementsPerRow;
                var col = i % elementsPerRow;

                elementRects[i] = new Rectangle(
                    col * elementSizeX,
                    row * elementSizeY,
                    bmp.Width,
                    bmp.Height);

                GLUtils.TexSubImage2D(GLES11.GlTexture2d, 0, elementRects[i].X, elementRects[i].Y, bmp);
                bmp.Recycle();
            }

            return new GLBitmapAtlas(textureId, atlasSizeX, atlasSizeY, elementRects);
        }

        public GLBitmap CreateBitmapFromOffscreenGraphics(GLOffscreenGraphics g)
        {
            return new GLBitmap(g.Texture, g.SizeX, g.SizeY, false);
        }

        public float GetBitmapWidth(GLBitmap bmp)
        {
            return bmp.Size.Width;
        }

        public GLCommandList CreateCommandList()
        {
            return new GLCommandList(this, dashedBitmap.Size.Width);
        }

        public void DrawCommandList(GLCommandList list)
        {
            DrawCommandList(list, Rectangle.Empty);
        }

        private T[] CopyResizeArray<T>(T[] array, int size)
        {
            var newArray = new T[size];
            Array.Copy(array, newArray, size);
            return newArray;
        }

        private FloatBuffer CopyCreateVtxBuffer(float[] array, int size)
        {
            var newArray = new float[size];
            Array.Copy(array, newArray, size);
            var buffer = ByteBuffer.AllocateDirect(sizeof(float) * size).Order(ByteOrder.NativeOrder()).AsFloatBuffer();
            buffer.Put(newArray);
            buffer.Position(0);
            return buffer;
        }

        private IntBuffer CopyCreateColBuffer(int[] array, int size)
        {
            var newArray = new int[size];
            Array.Copy(array, newArray, size);
            var buffer = ByteBuffer.AllocateDirect(sizeof(int) * size).Order(ByteOrder.NativeOrder()).AsIntBuffer();
            buffer.Put(newArray);
            buffer.Position(0);
            return buffer;
        }

        private ShortBuffer CopyCreateIdxBuffer(short[] array, int size)
        {
            var newArray = new short[size];
            Array.Copy(array, newArray, size);
            var buffer = ByteBuffer.AllocateDirect(sizeof(short) * size).Order(ByteOrder.NativeOrder()).AsShortBuffer();
            buffer.Put(newArray);
            buffer.Position(0);
            return buffer;
        }

        public unsafe void DrawCommandList(GLCommandList list, Rectangle scissor)
        {
            if (!scissor.IsEmpty)
                SetScissorRect(scissor.Left, scissor.Top, scissor.Right, scissor.Bottom);

            if (list.HasAnyMeshes)
            {
                var drawData = list.GetMeshDrawData();

                gl.GlEnableClientState(GLES11.GlColorArray);

                foreach (var draw in drawData)
                {
                    var vb = CopyCreateVtxBuffer(draw.vtxArray, draw.vtxArraySize);
                    var cb = CopyCreateColBuffer(draw.colArray, draw.colArraySize);
                    var ib = CopyCreateIdxBuffer(draw.idxArray, draw.idxArraySize);

                    //if (draw.smooth) gl.GlEnable(GLES11.GlPolygonSmooth);
                    gl.GlColorPointer(4, GLES11.GlUnsignedByte, 0, cb);
                    gl.GlVertexPointer(2, GLES11.GlFloat, 0, vb);
                    gl.GlDrawElements(GLES11.GlTriangles, draw.numIndices, GLES11.GlUnsignedShort, ib);
                    //if (draw.smooth) gl.GlDisable(GLES11.GlPolygonSmooth);
                }

                gl.GlDisableClientState(GLES11.GlColorArray);
            }

            if (list.HasAnyLines)
            {
                var drawData = list.GetLineDrawData();

                gl.GlPushMatrix();
                gl.GlTranslatef(0.5f, 0.5f, 0.0f);
                gl.GlEnable(GLES11.GlTexture2d);
                gl.GlBindTexture(GLES11.GlTexture2d, dashedBitmap.Id);
                gl.GlEnableClientState(GLES11.GlColorArray);
                gl.GlEnableClientState(GLES11.GlTextureCoordArray);

                foreach (var draw in drawData)
                {
                    var vb = CopyCreateVtxBuffer(draw.vtxArray, draw.vtxArraySize);
                    var cb = CopyCreateColBuffer(draw.colArray, draw.colArraySize);
                    var tb = CopyCreateVtxBuffer(draw.texArray, draw.texArraySize);

                    if (draw.smooth) gl.GlEnable(GLES11.GlLineSmooth);
                    gl.GlLineWidth(draw.lineWidth);
                    gl.GlTexCoordPointer(2, GLES11.GlFloat, 0, tb);
                    gl.GlColorPointer(4, GLES11.GlUnsignedByte, 0, cb);
                    gl.GlVertexPointer(2, GLES11.GlFloat, 0, vb);
                    gl.GlDrawArrays(GLES11.GlLines, 0, draw.numVertices);
                    if (draw.smooth) gl.GlDisable(GLES11.GlLineSmooth);
                }

                gl.GlDisableClientState(GLES11.GlColorArray);
                gl.GlDisableClientState(GLES11.GlTextureCoordArray);
                gl.GlDisable(GLES11.GlTexture2d);
                gl.GlPopMatrix();
            }

            if (list.HasAnyBitmaps)
            {
                var drawData = list.GetBitmapDrawData(vtxArray, texArray, colArray, out var vtxSize, out var texSize, out var colSize, out var idxSize);

                var vb = CopyCreateVtxBuffer(vtxArray, vtxSize);
                var cb = CopyCreateColBuffer(colArray, colSize);
                var tb = CopyCreateVtxBuffer(texArray, texSize);
                var ib = CopyCreateIdxBuffer(quadIdxArray, idxSize);

                gl.GlEnable(GLES11.GlTexture2d);
                gl.GlEnableClientState(GLES11.GlColorArray);
                gl.GlEnableClientState(GLES11.GlTextureCoordArray);
                gl.GlTexCoordPointer(2, GLES11.GlFloat, 0, tb);
                gl.GlColorPointer(4, GLES11.GlUnsignedByte, 0, cb);
                gl.GlVertexPointer(2, GLES11.GlFloat, 0, vb);

                foreach (var draw in drawData)
                {
                    ib.Position(draw.start);
                    gl.GlBindTexture(GLES11.GlTexture2d, draw.textureId);
                    gl.GlDrawElements(GLES11.GlTriangles, draw.count, GLES11.GlUnsignedShort, ib);
                }

                gl.GlDisableClientState(GLES11.GlColorArray);
                gl.GlDisableClientState(GLES11.GlTextureCoordArray);
                gl.GlDisable(GLES11.GlTexture2d);
            }

            if (list.HasAnyTexts)
            {
                var drawData = list.GetTextDrawData(vtxArray, texArray, colArray, out var vtxSize, out var texSize, out var colSize, out var idxSize);

                var vb = CopyCreateVtxBuffer(vtxArray, vtxSize);
                var cb = CopyCreateColBuffer(colArray, colSize);
                var tb = CopyCreateVtxBuffer(texArray, texSize);
                var ib = CopyCreateIdxBuffer(quadIdxArray, idxSize);

                gl.GlEnable(GLES11.GlTexture2d);
                gl.GlEnableClientState(GLES11.GlColorArray);
                gl.GlEnableClientState(GLES11.GlTextureCoordArray);
                gl.GlTexCoordPointer(2, GLES11.GlFloat, 0, tb);
                gl.GlColorPointer(4, GLES11.GlUnsignedByte, 0, cb);
                gl.GlVertexPointer(2, GLES11.GlFloat, 0, vb);

                foreach (var draw in drawData)
                {
                    ib.Position(draw.start);
                    gl.GlBindTexture(GLES11.GlTexture2d, draw.textureId);
                    gl.GlDrawElements(GLES11.GlTriangles, draw.count, GLES11.GlUnsignedShort, ib);
                }

                gl.GlDisableClientState(GLES11.GlColorArray);
                gl.GlDisableClientState(GLES11.GlTextureCoordArray);
                gl.GlDisable(GLES11.GlTexture2d);
            }

            if (!scissor.IsEmpty)
                ClearScissorRect();

            list.Release();
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