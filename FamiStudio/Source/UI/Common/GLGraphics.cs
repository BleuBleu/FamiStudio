using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using OpenTK;
using OpenTK.Graphics.OpenGL;

#if FAMISTUDIO_WINDOWS
using Bitmap = System.Drawing.Bitmap;
#else
using Bitmap = Gdk.Pixbuf;
#endif

namespace FamiStudio
{
    public class GLGraphics : GLGraphicsBase
    {
        public GLGraphics()
        {
            dashedBitmap = CreateBitmapFromResource("Dash");
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)All.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)All.Repeat);
        }

        public override void BeginDrawControl(Rectangle unflippedControlRect, int windowSizeY)
        {
            base.BeginDrawControl(unflippedControlRect, windowSizeY);

            GL.Viewport(controlRectFlip.Left, controlRectFlip.Top, controlRectFlip.Width, controlRectFlip.Height);
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            GL.Ortho(0, unflippedControlRect.Width, unflippedControlRect.Height, 0, -1, 1);
            GL.Disable(EnableCap.CullFace);
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            GL.Enable(EnableCap.Blend);
            GL.Disable(EnableCap.DepthTest);
            GL.Disable(EnableCap.StencilTest);
            GL.Enable(EnableCap.ScissorTest);
            GL.Scissor(controlRectFlip.Left, controlRectFlip.Top, controlRectFlip.Width, controlRectFlip.Height);
            GL.EnableClientState(ArrayCap.VertexArray);
        }

        private void SetScissorRect(int x0, int y0, int x1, int y1)
        {
            var scissor = new Rectangle(controlRect.X + x0, controlRect.Y + y0, x1 - x0, y1 - y0);
            scissor = FlipRectangleY(scissor);
            GL.Scissor(scissor.Left, scissor.Top, scissor.Width, scissor.Height);
        }

        private void ClearScissorRect()
        {
            GL.Scissor(controlRectFlip.Left, controlRectFlip.Top, controlRectFlip.Width, controlRectFlip.Height);
        }

        public void Clear(Color color)
        {
            GL.ClearColor(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f, color.A / 255.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit);
        }

        public void UpdateBitmap(GLBitmap bmp, int x, int y, int width, int height, int[] data)
        {
            GL.BindTexture(TextureTarget.Texture2D, bmp.Id);
            GL.TexSubImage2D(TextureTarget.Texture2D, 0, x, y, width, height, PixelFormat.Bgra, PixelType.UnsignedByte, data);
        }

        protected override int CreateEmptyTexture(int width, int height)
        {
            int id = GL.GenTexture();

            GL.BindTexture(TextureTarget.Texture2D, id);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)All.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)All.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)All.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)All.ClampToEdge);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, width, height, 0, PixelFormat.Bgra, PixelType.UnsignedByte, new uint[width * height]);

            return id;
        }

        protected override int CreateTexture(Bitmap bmp)
        {
#if FAMISTUDIO_WINDOWS
            var bmpData =
                bmp.LockBits(
                    new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height),
                    System.Drawing.Imaging.ImageLockMode.ReadOnly,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            Debug.Assert(bmpData.Stride == bmp.Width * 4);
            var ptr = bmpData.Scan0;
            var format = PixelFormat.Bgra;
#else
            Debug.Assert(pixbuf.Rowstride == pixbuf.Width * 4);
            var ptr = bmpData.Pixels;
            var format = PixelFormat.Rgba;
#endif

            int id = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, id);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, bmp.Width, bmp.Height, 0, format, PixelType.UnsignedByte, ptr);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)All.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)All.Nearest);

#if FAMISTUDIO_WINDOWS
            bmp.UnlockBits(bmpData);
#endif

            return id;
        }

        protected Bitmap LoadBitmapFromResourceWithScaling(string name)
        {
            var assembly = Assembly.GetExecutingAssembly();

            bool needsScaling = false;
            System.Drawing.Bitmap bmp;

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

            return bmp;
        }

        public GLBitmap CreateBitmapFromResource(string name)
        {
            var bmp = LoadBitmapFromResourceWithScaling(name);
            return new GLBitmap(CreateTexture(bmp), bmp.Width, bmp.Height);
        }

        private void ChangeBitmapBackground(Bitmap bmp, Color color)
        {
            for (int y = 0; y < bmp.Height; y++)
            {
                for (int x = 0; x < bmp.Width; x++)
                {
                    var pixel = bmp.GetPixel(x, y);

                    var r = (byte)Utils.Lerp(color.R, pixel.R, pixel.A / 255.0f);
                    var g = (byte)Utils.Lerp(color.G, pixel.G, pixel.A / 255.0f);
                    var b = (byte)Utils.Lerp(color.B, pixel.B, pixel.A / 255.0f);

                    bmp.SetPixel(x, y, Color.FromArgb(r, g, b));
                }
            }
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

            GL.BindTexture(TextureTarget.Texture2D, textureId);

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

#if FAMISTUDIO_WINDOWS
                var bmpData =
                    bmp.LockBits(
                        new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height),
                        System.Drawing.Imaging.ImageLockMode.ReadOnly,
                        System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                var ptr = bmpData.Scan0;
                var format = PixelFormat.Bgra;
#else
                var ptr = bmpData.Pixels;
                var format = PixelFormat.Rgba;
#endif

                GL.TexSubImage2D(TextureTarget.Texture2D, 0, elementRects[i].X, elementRects[i].Y, bmp.Width, bmp.Height, format, PixelType.UnsignedByte, ptr);
                bmp.UnlockBits(bmpData);
                bmp.Dispose();
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

        public unsafe void DrawCommandList(GLCommandList list, Rectangle scissor)
        {
            if (!scissor.IsEmpty)
                SetScissorRect(scissor.Left, scissor.Top, scissor.Right, scissor.Bottom);

            if (list.HasAnyMeshes)
            {
                var drawData = list.GetMeshDrawData();

                GL.EnableClientState(ArrayCap.ColorArray);

                foreach (var draw in drawData)
                {
                    if (draw.smooth) GL.Enable(EnableCap.PolygonSmooth);
                    GL.ColorPointer(4, ColorPointerType.UnsignedByte, 0, draw.colArray);
                    GL.VertexPointer(2, VertexPointerType.Float, 0, draw.vtxArray);
                    GL.DrawElements(PrimitiveType.Triangles, draw.numIndices, DrawElementsType.UnsignedShort, draw.idxArray);
                    if (draw.smooth) GL.Disable(EnableCap.PolygonSmooth);
                }

                GL.DisableClientState(ArrayCap.ColorArray);
            }

            if (list.HasAnyLines)
            {
                var drawData = list.GetLineDrawData();

                GL.PushMatrix();
                GL.Translate(0.5f, 0.5f, 0.0f);
                GL.Enable(EnableCap.Texture2D);
                GL.BindTexture(TextureTarget.Texture2D, dashedBitmap.Id);
                GL.EnableClientState(ArrayCap.ColorArray);
                GL.EnableClientState(ArrayCap.TextureCoordArray);

                foreach (var draw in drawData)
                {
                    if (draw.smooth) GL.Enable(EnableCap.LineSmooth);
                    GL.LineWidth(draw.lineWidth);
                    GL.TexCoordPointer(2, TexCoordPointerType.Float, 0, draw.texArray);
                    GL.ColorPointer(4, ColorPointerType.UnsignedByte, 0, draw.colArray);
                    GL.VertexPointer(2, VertexPointerType.Float, 0, draw.vtxArray);
                    GL.DrawArrays(PrimitiveType.Lines, 0, draw.numVertices);
                    if (draw.smooth) GL.Disable(EnableCap.LineSmooth);
                }

                GL.DisableClientState(ArrayCap.ColorArray);
                GL.DisableClientState(ArrayCap.TextureCoordArray);
                GL.Disable(EnableCap.Texture2D);
                GL.PopMatrix();
            }

            if (list.HasAnyBitmaps)
            {
                var drawData = list.GetBitmapDrawData(vtxArray, texArray, colArray, out _, out _, out _, out _);

                GL.Enable(EnableCap.Texture2D);
                GL.EnableClientState(ArrayCap.ColorArray);
                GL.EnableClientState(ArrayCap.TextureCoordArray);
                GL.TexCoordPointer(2, TexCoordPointerType.Float, 0, texArray);
                GL.ColorPointer(4, ColorPointerType.UnsignedByte, 0, colArray);
                GL.VertexPointer(2, VertexPointerType.Float, 0, vtxArray);

                fixed (short* ptr = quadIdxArray)
                {
                    foreach (var draw in drawData)
                    {
                        GL.BindTexture(TextureTarget.Texture2D, draw.textureId);
                        GL.DrawElements(PrimitiveType.Triangles, draw.count, DrawElementsType.UnsignedShort, new IntPtr(ptr + draw.start));
                    }
                }

                GL.DisableClientState(ArrayCap.ColorArray);
                GL.DisableClientState(ArrayCap.TextureCoordArray);
                GL.Disable(EnableCap.Texture2D);
            }

            if (list.HasAnyTexts)
            {
                var drawData = list.GetTextDrawData(vtxArray, texArray, colArray, out _, out _, out _, out _);

                GL.Enable(EnableCap.Texture2D);
                GL.EnableClientState(ArrayCap.ColorArray);
                GL.EnableClientState(ArrayCap.TextureCoordArray);
                GL.TexCoordPointer(2, TexCoordPointerType.Float, 0, texArray);
                GL.ColorPointer(4, ColorPointerType.UnsignedByte, 0, colArray);
                GL.VertexPointer(2, VertexPointerType.Float, 0, vtxArray);

                fixed (short* ptr = quadIdxArray)
                {
                    foreach (var draw in drawData)
                    {
                        GL.BindTexture(TextureTarget.Texture2D, draw.textureId);
                        GL.DrawElements(PrimitiveType.Triangles, draw.count, DrawElementsType.UnsignedShort, new IntPtr(ptr + draw.start));
                    }
                }

                GL.DisableClientState(ArrayCap.ColorArray);
                GL.DisableClientState(ArrayCap.TextureCoordArray);
                GL.Disable(EnableCap.Texture2D);
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

        public override void BeginDrawControl(Rectangle unflippedControlRect, int windowSizeY)
        {
            base.BeginDrawControl(unflippedControlRect, windowSizeY);

            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, fbo);
            GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
        }

        public override void EndDrawControl()
        {
            base.EndDrawControl();

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