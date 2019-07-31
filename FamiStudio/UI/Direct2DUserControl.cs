using System;
using System.Windows.Forms;

using SharpDX.Direct2D1;
using SharpDX;
using SharpDX.Mathematics.Interop;
using SharpDX.DXGI;

using Factory = SharpDX.Direct2D1.Factory;
using DirectWriteFactory = SharpDX.DirectWrite.Factory;
using AlphaMode = SharpDX.Direct2D1.AlphaMode;
using Bitmap = SharpDX.Direct2D1.Bitmap;
using PixelFormat = SharpDX.Direct2D1.PixelFormat;
using SharpDX.DirectWrite;
using System.Collections.Generic;

namespace FamiStudio
{
    public class Direct2DUserControl : UserControl
    {
        protected Direct2DGraphics d2dGraphics;

        public Direct2DUserControl()
        {
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            InitDirect2D();
        }

        private void InitDirect2D()
        {
            if (!DesignMode)
            {
                d2dGraphics = new Direct2DGraphics();
                d2dGraphics.factory = new SharpDX.Direct2D1.Factory();
                d2dGraphics.directWriteFactory = new SharpDX.DirectWrite.Factory();

                HwndRenderTargetProperties properties = new HwndRenderTargetProperties();
                properties.Hwnd = Handle;
                properties.PixelSize = new SharpDX.Size2(ClientSize.Width, ClientSize.Height);
                properties.PresentOptions = PresentOptions.None;

                d2dGraphics.renderTarget = new WindowRenderTarget(d2dGraphics.factory, new RenderTargetProperties(new SharpDX.Direct2D1.PixelFormat(Format.B8G8R8A8_UNorm, AlphaMode.Premultiplied)), properties);
                d2dGraphics.renderTarget.TextAntialiasMode = SharpDX.Direct2D1.TextAntialiasMode.Grayscale;
                //d2dGraphics.renderTarget.TextAntialiasMode = SharpDX.Direct2D1.TextAntialiasMode.Aliased;
                d2dGraphics.renderTarget.AntialiasMode = AntialiasMode.Aliased;

                DoubleBuffered = false;
                ResizeRedraw = true;

                OnDirect2DInitialized(d2dGraphics);
            }
        }

        protected virtual void OnDirect2DInitialized(Direct2DGraphics g)
        {
        }

        protected virtual void OnRender(Direct2DGraphics g)
        {
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            if (d2dGraphics != null)
            {
                d2dGraphics.renderTarget.Resize(new Size2(ClientSize.Width, ClientSize.Height));
            }
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (DesignMode)
            {
                e.Graphics.Clear(System.Drawing.Color.Black);
                e.Graphics.DrawString("Direct2D cannot be used at design time.", this.Font, System.Drawing.Brushes.White, 10, 10);
            }
            else
            {
                d2dGraphics.renderTarget.BeginDraw();
                OnRender(d2dGraphics);
                d2dGraphics.renderTarget.EndDraw();
            }
        }
    }

    public class Direct2DGraphics
    {
        public Factory factory;
        public DirectWriteFactory directWriteFactory;
        public WindowRenderTarget renderTarget;
        private Stack<RawMatrix3x2> matrixStack = new Stack<RawMatrix3x2>();

        public Direct2DGraphics()
        {

        }

        public bool AntiAliasing
        {
            get { return renderTarget.AntialiasMode == AntialiasMode.PerPrimitive; }
            set { renderTarget.AntialiasMode = value ? AntialiasMode.PerPrimitive : AntialiasMode.Aliased; }
        }

        public void PushTranslation(float x, float y)
        {
            var mat = renderTarget.Transform;
            matrixStack.Push(mat);
            mat.M31 += x;
            mat.M32 += y;
            renderTarget.Transform = mat;
        }

        public void PushScale(float x, float y)
        {
            var mat = renderTarget.Transform;
            matrixStack.Push(mat);
            mat.M11 *= x;
            mat.M12 *= y;
            mat.M21 *= x;
            mat.M22 *= y;
            renderTarget.Transform = mat;
        }

        public void PopTransform()
        {
            renderTarget.Transform = matrixStack.Pop();
        }

        public void PushClip(float x0, float y0, float x1, float y1)
        {
            renderTarget.PushAxisAlignedClip(new RawRectangleF(x0, y0, x1, y1), AntialiasMode.Aliased);
        }

        public void PushClipHalfPixel(float x0, float y0, float x1, float y1)
        {
            renderTarget.PushAxisAlignedClip(new RawRectangleF(x0 + 0.5f, y0 + 0.5f, x1 + 0.5f, y1 + 0.5f), AntialiasMode.Aliased);
        }

        public void PopClip()
        {
            renderTarget.PopAxisAlignedClip();
        }

        public void Clear(float r, float g, float b)
        {
            renderTarget.Clear(new RawColor4(r, g, b, 1));
        }

        public void Clear(RawColor4 color)
        {
            renderTarget.Clear(color);
        }

        public void DrawBitmap(Bitmap bmp, float x, float y, float opacity = 1.0f)
        {
            renderTarget.DrawBitmap(bmp, new RawRectangleF(x, y, x + bmp.Size.Width, y + bmp.Size.Height), opacity, BitmapInterpolationMode.NearestNeighbor);
        }

        public void DrawBitmap(Bitmap bmp, float x, float y, float width, float height, float opacity = 1.0f)
        {
            renderTarget.DrawBitmap(bmp, new RawRectangleF(x, y, x + width, y + height), opacity, BitmapInterpolationMode.NearestNeighbor);
        }

        public void DrawText(string text, TextFormat font, float x, float y, Brush brush, float width = 1000)
        {
            renderTarget.DrawText(text, font, new RawRectangleF(x, y, x + width, y + 1000), brush);
        }

        public void DrawLine(float x0, float y0, float x1, float y1, Brush brush)
        {
            renderTarget.DrawLine(new RawVector2(x0, y0), new RawVector2(x1, y1), brush);
        }

        public void DrawLineHalfPixel(float x0, float y0, float x1, float y1, Brush brush, float width = 1.0f)
        {
            renderTarget.DrawLine(new RawVector2(x0 + 0.5f, y0 + 0.5f), new RawVector2(x1 + 0.5f, y1 + 0.5f), brush, width);
        }

        public void DrawRectangleHalfPixel(RawRectangleF rect, Brush brush)
        {
            rect.Left += 0.5f;
            rect.Top += 0.5f;
            rect.Right += 0.5f;
            rect.Bottom += 0.5f;
            renderTarget.DrawRectangle(rect, brush);
        }

        public void DrawRectangleHalfPixel(float x0, float y0, float x1, float y1, Brush brush)
        {
            DrawRectangleHalfPixel(new RawRectangleF(x0, y0, x1, y1), brush);
        }

        public void FillRectangle(RawRectangleF rect, Brush brush)
        {
            renderTarget.FillRectangle(rect, brush);
        }

        public void FillRectangle(float x0, float y0, float x1, float y1, Brush brush)
        {
            renderTarget.FillRectangle(new RawRectangleF(x0, y0, x1, y1), brush);
        }

        public void FillAndDrawRectangleHalfPixel(float x0, float y0, float x1, float y1, Brush fillBrush, Brush lineBrush)
        {
            var rect = new RawRectangleF(x0, y0, x1, y1);
            renderTarget.FillRectangle(rect, fillBrush);
            DrawRectangleHalfPixel(rect, lineBrush);
        }

        public void FillGeometry(Geometry geo, Brush brush)
        {
            renderTarget.FillGeometry(geo, brush);
        }

        public void DrawGeometryHalfPixel(Geometry geo, Brush brush)
        {
            PushTranslation(0.5f, 0.5f);
            renderTarget.DrawGeometry(geo, brush);
            PopTransform();
        }

        public static System.Drawing.Color ToDrawingColor4(RawColor4 color)
        {
            return System.Drawing.Color.FromArgb(
                (int)(color.A * 255.0f),
                (int)(color.R * 255.0f),
                (int)(color.G * 255.0f),
                (int)(color.B * 255.0f));
        }

        public static RawColor4 ToRawColor4(System.Drawing.Color color)
        {
            return new RawColor4(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f, color.A / 255.0f);
        }

        public Brush CreateSolidBrush(RawColor4 color)
        {
            return new SolidColorBrush(renderTarget, color);
        }

        public Brush CreateHorizontalGradientBrush(float x0, float x1, RawColor4 color0, RawColor4 color1)
        {
            return new LinearGradientBrush(
                renderTarget,
                new LinearGradientBrushProperties() { StartPoint = new RawVector2(x0, 0), EndPoint = new RawVector2(x1, 0) },
                null,
                new GradientStopCollection(
                    renderTarget,
                    new GradientStop[]
                    {
                        new GradientStop() { Position = 0.0f, Color = color0 },
                        new GradientStop() { Position = 1.0f, Color = color1 }
                    }));
        }

        public Brush CreateVerticalGradientBrush(float y0, float y1, RawColor4 color0, RawColor4 color1)
        {
            return new LinearGradientBrush(
                renderTarget,
                new LinearGradientBrushProperties() { StartPoint = new RawVector2(0, y0), EndPoint = new RawVector2(0, y1) },
                null,
                new GradientStopCollection(
                    renderTarget,
                    new GradientStop[]
                    {
                        new GradientStop() { Position = 0.0f, Color = color0 },
                        new GradientStop() { Position = 1.0f, Color = color1 }
                    }));
        }

        public Bitmap ConvertBitmap(System.Drawing.Bitmap bmp)
        {
            System.Drawing.Imaging.BitmapData bmpData =
                bmp.LockBits(
                    new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height),
                    System.Drawing.Imaging.ImageLockMode.ReadOnly,
                    System.Drawing.Imaging.PixelFormat.Format32bppPArgb);

            DataStream stream = new DataStream(bmpData.Scan0, bmpData.Stride * bmpData.Height, true, false);
            PixelFormat pFormat = new PixelFormat(Format.B8G8R8A8_UNorm, AlphaMode.Premultiplied);
            BitmapProperties bmpProps = new BitmapProperties(pFormat);

            Bitmap result =
                new Bitmap(
                    renderTarget,
                    new Size2(bmp.Width, bmp.Height),
                    stream,
                    bmpData.Stride,
                    bmpProps);

            bmp.UnlockBits(bmpData);

            stream.Dispose();
            bmp.Dispose();

            return result;
        }

        private Dictionary<RawColor4, Brush> verticalGradientCache = new Dictionary<RawColor4, Brush>();

        public Brush GetVerticalGradientBrush(RawColor4 color1, int sizeY, float dimming)
        {
            Brush brush;
            if (verticalGradientCache.TryGetValue(color1, out brush))
                return brush;

            RawColor4 color2;
            color2.R = color1.R * dimming;
            color2.G = color1.G * dimming;
            color2.B = color1.B * dimming;
            color2.A = color1.A;

            brush = CreateVerticalGradientBrush(0, sizeY, color1, color2);
            verticalGradientCache[color1] = brush;

            return brush;
        }

        public Brush GetVerticalGradientBrush(System.Drawing.Color color1, int sizeY, float dimming)
        {
            return GetVerticalGradientBrush(ToRawColor4(color1), sizeY, dimming);
        }
    }
}
