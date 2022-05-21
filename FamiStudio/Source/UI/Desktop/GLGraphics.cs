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
        private bool supportsLineWidth = true;

        private GLCommandList dialogCommandList;
        private GLCommandList dialogCommandListForeground;

        public GLGraphics(float mainScale, float fontScale) : base(mainScale, fontScale)
        {
            dashedBitmap = CreateBitmapFromResource("Dash");
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)All.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)All.Repeat);

#if FAMISTUDIO_LINUX
            var lineWidths = new float[2];
            GL.GetFloat(GetPName.LineWidthRange, lineWidths);
            supportsLineWidth = lineWidths[1] > 1.0f;
#endif
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

        protected override int CreateEmptyTexture(int width, int height, bool filter = false)
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

        // MATTT : Move to base!
        protected unsafe override int CreateTexture(int[,] bmpData, bool filter)
        {
            fixed (int* ptr = &bmpData[0, 0])
            {
                var stride = sizeof(int) * bmpData.GetLength(1);

                // MATTT : Check that!!!
            #if FAMISTUDIO_WINDOWS
                var format = PixelFormat.Bgra;
            #else
                var format = PixelFormat.Rgba;
            #endif

                int id = GL.GenTexture();
                GL.BindTexture(TextureTarget.Texture2D, id);
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, bmpData.GetLength(1), bmpData.GetLength(0), 0, format, PixelType.UnsignedByte, new IntPtr(ptr));
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)All.Nearest);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)All.Nearest);

                return id;
            }
        }

        public GLBitmap CreateBitmapFromResource(string name)
        {
            var bmp = LoadBitmapFromResourceWithScaling(name);
            return new GLBitmap(CreateTexture(bmp, false), bmp.GetLength(0), bmp.GetLength(1));
        }

        public GLBitmap CreateBitmapFromOffscreenGraphics(GLOffscreenGraphics g)
        {
            return new GLBitmap(g.Texture, g.SizeX, g.SizeY, false);
        }

        public float GetBitmapWidth(GLBitmap bmp)
        {
            return bmp.Size.Width;
        }

        public void BeginDrawDialog()
        {
            dialogCommandList = CreateCommandList(CommandListUsage.Default);
            dialogCommandListForeground = CreateCommandList(CommandListUsage.Default);
        }

        public void EndDrawDialog(System.Drawing.Color clearColor)
        {
            DrawCommandList(dialogCommandList);
            DrawCommandList(dialogCommandListForeground);
        }

        public override GLCommandList CreateCommandList(CommandListUsage usage = CommandListUsage.Default)
        {
            switch (usage)
            {
                case CommandListUsage.Dialog:
                    return dialogCommandList;
                case CommandListUsage.DialogForeground:
                    return dialogCommandListForeground;
                default:
                    return new GLCommandList(this, dashedBitmap.Size.Width, lineWidthBias, supportsLineWidth);
            }
        }

        public unsafe override void DrawCommandList(GLCommandList list, Rectangle scissor)
        {
            if (list == null)
                return;

            if (list.HasAnything)
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

#if FAMISTUDIO_LINUX
                if (list.HasAnyTickLineMeshes)
                {
                    var draw = list.GetThickLineAsPolygonDrawData();

                    /*if (draw.smooth)*/ GL.Enable(EnableCap.PolygonSmooth);
                    GL.EnableClientState(ArrayCap.ColorArray);
                    GL.ColorPointer(4, ColorPointerType.UnsignedByte, 0, draw.colArray);
                    GL.VertexPointer(2, VertexPointerType.Float, 0, draw.vtxArray);
                    GL.DrawElements(PrimitiveType.Triangles, draw.numIndices, DrawElementsType.UnsignedShort, draw.idxArray);
                    GL.DisableClientState(ArrayCap.ColorArray);
                    /*if (draw.smooth)*/ GL.Disable(EnableCap.PolygonSmooth);
                }
#endif

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
            }

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

        private GLOffscreenGraphics(int imageSizeX, int imageSizeY, bool allowReadback) : base(1.0f, 1.0f) 
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