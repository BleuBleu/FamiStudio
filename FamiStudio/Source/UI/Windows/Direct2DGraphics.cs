using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Diagnostics;
using SharpDX.Direct2D1;
using SharpDX;
using SharpDX.Mathematics.Interop;
using SharpDX.DXGI;
using SharpDX.DirectWrite;

using Factory = SharpDX.Direct2D1.Factory;
using DirectWriteFactory = SharpDX.DirectWrite.Factory;
using AlphaMode = SharpDX.Direct2D1.AlphaMode;
using Bitmap = SharpDX.Direct2D1.Bitmap;
using PixelFormat = SharpDX.Direct2D1.PixelFormat;
using Color = System.Drawing.Color;
using Rectangle = System.Drawing.Rectangle;
using System.Reflection;

namespace FamiStudio
{
    public class Direct2DGraphics : IDisposable
    {
        protected static SharpDX.Direct2D1.Factory factory;
        protected static int factoryRefCount = 0;

        protected DirectWriteFactory directWriteFactory;
        protected RenderTarget renderTarget;
        protected Stack<RawMatrix3x2> matrixStack = new Stack<RawMatrix3x2>();
        protected Dictionary<Color, Brush> solidGradientCache = new Dictionary<Color, Brush>();
        protected Dictionary<Tuple<Color, int>, Brush> verticalGradientCache = new Dictionary<Tuple<Color, int>, Brush>();
        protected StrokeStyle strokeStyleMiterNoScaling;
        protected float windowScaling = 1.0f;

        public Factory Factory => factory;
        public float WindowScaling => windowScaling;

        public Direct2DGraphics(UserControl control)
        {
            CreateFactory();

            windowScaling = Direct2DTheme.MainWindowScaling;

            HwndRenderTargetProperties properties = new HwndRenderTargetProperties();
            properties.Hwnd = control.Handle;
            properties.PixelSize = new SharpDX.Size2(control.ClientSize.Width, control.ClientSize.Height);
            properties.PresentOptions = PresentOptions.None;

            renderTarget = new WindowRenderTarget(factory, new RenderTargetProperties(new SharpDX.Direct2D1.PixelFormat(Format.B8G8R8A8_UNorm, AlphaMode.Premultiplied)), properties);

            Initialize();
        }

        protected Direct2DGraphics()
        {
        }

        protected void CreateFactory()
        {
            if (factory == null)
            {
	            try
	            {
	                // Some Windows 7 versions dont have version 1 of the factory.
	                factory = new SharpDX.Direct2D1.Factory1();
	            }
	            catch
	            {
	                factory = new SharpDX.Direct2D1.Factory();
	            }
				
                factoryRefCount = 1;
            }
            else
            {
                factoryRefCount++;
            }
        }

        protected void Initialize()
        {
            directWriteFactory = new SharpDX.DirectWrite.Factory();

            renderTarget.TextAntialiasMode = SharpDX.Direct2D1.TextAntialiasMode.Grayscale;
            renderTarget.AntialiasMode = AntialiasMode.Aliased;

            if (factory is SharpDX.Direct2D1.Factory1)
	            strokeStyleMiterNoScaling = new StrokeStyle1(factory as SharpDX.Direct2D1.Factory1, new StrokeStyleProperties1() { MiterLimit = 1, TransformType = StrokeTransformType.Fixed });
			else
	            strokeStyleMiterNoScaling = new StrokeStyle(factory, new StrokeStyleProperties() { MiterLimit = 1 });
        }

        public virtual void Dispose()
        {
            foreach (var grad in verticalGradientCache.Values)
                grad.Dispose();
            verticalGradientCache.Clear();

            foreach (var grad in solidGradientCache.Values)
                grad.Dispose();
            solidGradientCache.Clear();

            Utils.DisposeAndNullify(ref renderTarget);
            Utils.DisposeAndNullify(ref directWriteFactory);

            if (--factoryRefCount == 0)
                Utils.DisposeAndNullify(ref factory);
        }

        public void BeginDraw()
        {
            renderTarget.BeginDraw();
        }

        public void EndDraw()
        {
            renderTarget.EndDraw();
        }

        public void Resize(int width, int height)
        {
            (renderTarget as WindowRenderTarget).Resize(new Size2(width, height));
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

        public void PushTransform(float tx, float ty, float sx, float sy)
        {
            var mat = renderTarget.Transform;
            matrixStack.Push(mat);
            mat.M11 *= sx;
            mat.M12 *= sy;
            mat.M21 *= sx;
            mat.M22 *= sy;
            mat.M31 += tx;
            mat.M32 += ty;
            renderTarget.Transform = mat;
        }

        public void PopTransform()
        {
            renderTarget.Transform = matrixStack.Pop();
        }

        public void PushClip(float x0, float y0, float x1, float y1)
        {
            renderTarget.PushAxisAlignedClip(new RawRectangleF(x0 + 0.5f, y0 + 0.5f, x1 + 0.5f, y1 + 0.5f), AntialiasMode.Aliased);
        }

        public void PopClip()
        {
            renderTarget.PopAxisAlignedClip();
        }
        
        public void Clear(Color color)
        {
            renderTarget.Clear(ToRawColor4(color));
        }

        public void DrawBitmap(Bitmap bmp, float x, float y, float opacity = 1.0f)
        {
            renderTarget.DrawBitmap(bmp, new RawRectangleF(x, y, x + bmp.Size.Width, y + bmp.Size.Height), opacity, BitmapInterpolationMode.NearestNeighbor);
        }

        public void DrawBitmap(Bitmap bmp, float x, float y, float width, float height, float opacity)
        {
            renderTarget.DrawBitmap(bmp, new RawRectangleF(x, y, x + width, y + height), opacity, BitmapInterpolationMode.NearestNeighbor);
        }

        // HACK : Very specific call only used by video rendering, too lazy to do the proper transforms.
        public void DrawRotatedFlippedBitmap(Bitmap bmp, float x, float y, float width, float height)
        {
            var mat = renderTarget.Transform;
            matrixStack.Push(mat);

            mat.M11 =  0;
            mat.M12 = -1;
            mat.M21 = -1;
            mat.M22 =  0;
            mat.M31 += x;
            mat.M32 += y;
            renderTarget.Transform = mat;

            renderTarget.DrawBitmap(bmp, new RawRectangleF(0, 0, width, height), 1.0f, BitmapInterpolationMode.NearestNeighbor);

            PopTransform();
        }

        public void DrawText(string text, TextFormat font, float x, float y, Brush brush, float width = 1000)
        {
            if (!string.IsNullOrEmpty(text))
                renderTarget.DrawText(text, font, new RawRectangleF(x, y, x + width, y + 1000), brush);
        }

        public float MeasureString(string text, TextFormat font)
        {
            using (TextLayout layout = new TextLayout(directWriteFactory, text, font, 1000, 1000))
                return (float)layout.Metrics.WidthIncludingTrailingWhitespace;
        }

        public void DrawLine(float x0, float y0, float x1, float y1, Brush brush)
        {
            PushTranslation(0.5f, 0.5f);
            renderTarget.DrawLine(new RawVector2(x0, y0), new RawVector2(x1, y1), brush);
            PopTransform();
        }

        public void DrawLine(float[,] points, Brush brush, float width = 1.0f)
        {
            PushTranslation(0.5f, 0.5f);
            for (int i = 0; i < points.GetLength(0) - 1; i++)
            {
                renderTarget.DrawLine(
                    new RawVector2(points[i + 0, 0], points[i + 0, 1]),
                    new RawVector2(points[i + 1, 0], points[i + 1, 1]), brush, width, strokeStyleMiterNoScaling);
            }
            PopTransform();
        }

        public void DrawLine(float x0, float y0, float x1, float y1, Brush brush, float width = 1.0f)
        {
            PushTranslation(0.5f, 0.5f);
            renderTarget.DrawLine(new RawVector2(x0, y0), new RawVector2(x1, y1), brush, width);
            PopTransform();
        }

        public void DrawRectangle(RawRectangleF rect, Brush brush, float width = 1.0f)
        {
            PushTranslation(0.5f, 0.5f);
            renderTarget.DrawRectangle(rect, brush, width);
            PopTransform();
        }

        public void DrawRectangle(float x0, float y0, float x1, float y1, Brush brush, float width = 1.0f)
        {
            DrawRectangle(new RawRectangleF(x0, y0, x1, y1), brush, width);
        }

        public PathGeometry CreateGeometry(float[,] points, bool closed = true)
        {
            var geo = new PathGeometry(factory);
            var sink = geo.Open();
            sink.SetFillMode(FillMode.Winding);

            sink.BeginFigure(new RawVector2(points[0, 0], points[0, 1]), FigureBegin.Filled);
            for (int i = 1; i < points.GetLength(0); i++)
                sink.AddLine(new RawVector2(points[i, 0], points[i, 1]));
            if (closed)
                sink.AddLine(new RawVector2(points[0, 0], points[0, 1]));

            sink.EndFigure(closed ? FigureEnd.Closed : FigureEnd.Open);
            sink.Close();
            sink.Dispose();

            return geo;
        }

        public void FillRectangle(Rectangle rect, Brush brush)
        {
            renderTarget.FillRectangle(new RawRectangleF(rect.Left, rect.Top, rect.Right, rect.Bottom), brush);
        }

        public void FillRectangle(float x0, float y0, float x1, float y1, Brush brush)
        {
            renderTarget.FillRectangle(new RawRectangleF(x0, y0, x1, y1), brush);
        }

        public void FillAndDrawRectangle(float x0, float y0, float x1, float y1, Brush fillBrush, Brush lineBrush, float width = 1.0f)
        {
            var rect = new RawRectangleF(x0, y0, x1, y1);
            renderTarget.FillRectangle(rect, fillBrush);
            DrawRectangle(rect, lineBrush, width);
        }

        public void FillGeometry(Geometry geo, Brush brush, bool smooth = false)
        {
            AntiAliasing = true;
            renderTarget.FillGeometry(geo, brush);
            AntiAliasing = false;
        }

        public void DrawGeometry(Geometry geo, Brush brush, float lineWidth = 1.0f)
        {
            bool halfPixelOffset = lineWidth == 1;

            AntiAliasing = true;
            if (halfPixelOffset) PushTranslation(0.5f, 0.5f);
            renderTarget.DrawGeometry(geo, brush, lineWidth, strokeStyleMiterNoScaling);
            if (halfPixelOffset) PopTransform();
            AntiAliasing = false;
        }

        public void FillAndDrawGeometry(Geometry geo, Brush fillBrush, Brush lineBrush, float lineWidth = 1.0f)
        {
            bool halfPixelOffset = lineWidth == 1;

            AntiAliasing = true;
            renderTarget.FillGeometry(geo, fillBrush);
            if (halfPixelOffset) PushTranslation(0.5f, 0.5f);
            renderTarget.DrawGeometry(geo, lineBrush, lineWidth, strokeStyleMiterNoScaling);
            if (halfPixelOffset) PopTransform();
            AntiAliasing = false;
        }

        public static Color ToDrawingColor4(RawColor4 color)
        {
            return Color.FromArgb(
                (int)(color.A * 255.0f),
                (int)(color.R * 255.0f),
                (int)(color.G * 255.0f),
                (int)(color.B * 255.0f));
        }

        public static RawColor4 ToRawColor4(Color color)
        {
            return new RawColor4(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f, color.A / 255.0f);
        }

        public Brush CreateSolidBrush(Color color)
        {
            return new SolidColorBrush(renderTarget, ToRawColor4(color));
        }

        public Brush CreateBitmapBrush(Bitmap bmp, bool wrapX, bool wrapY)
        {
            return new BitmapBrush(renderTarget, bmp, new BitmapBrushProperties
            {
                ExtendModeX = wrapX ? ExtendMode.Wrap : ExtendMode.Clamp,
                ExtendModeY = wrapY ? ExtendMode.Wrap : ExtendMode.Clamp
            });
        }

        public Brush CreateHorizontalGradientBrush(float x0, float x1, Color color0, Color color1)
        {
            return new LinearGradientBrush(
                renderTarget,
                new LinearGradientBrushProperties() { StartPoint = new RawVector2(x0, 0), EndPoint = new RawVector2(x1, 0) },
                null,
                new GradientStopCollection(
                    renderTarget,
                    new GradientStop[]
                    {
                        new GradientStop() { Position = 0.0f, Color = ToRawColor4(color0) },
                        new GradientStop() { Position = 1.0f, Color = ToRawColor4(color1) }
                    }));
        }

        public Brush CreateVerticalGradientBrush(float y0, float y1, Color color0, Color color1)
        {
            return new LinearGradientBrush(
                renderTarget,
                new LinearGradientBrushProperties() { StartPoint = new RawVector2(0, y0), EndPoint = new RawVector2(0, y1) },
                null,
                new GradientStopCollection(
                    renderTarget,
                    new GradientStop[]
                    {
                        new GradientStop() { Position = 0.0f, Color = ToRawColor4(color0) },
                        new GradientStop() { Position = 1.0f, Color = ToRawColor4(color1) }
                    }));
        }

        public unsafe Bitmap CreateBitmap(int width, int height, uint[] data)
        {
            fixed (uint* ptr = &data[0])
            {
                DataStream stream = new DataStream(new IntPtr(ptr), data.Length * sizeof(uint), true, false);
                BitmapProperties bmpProps = new BitmapProperties(new PixelFormat(Format.B8G8R8A8_UNorm, AlphaMode.Premultiplied));
                var bmp = new Bitmap(renderTarget, new Size2(width, height), stream, width * sizeof(uint), bmpProps);
                stream.Dispose();
                return bmp;
            }
        }

        public Bitmap CreateBitmapFromResource(string name)
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
                var newWidth  = Math.Max(1, (int)(bmp.Width  * (windowScaling / 2.0f)));
                var newHeight = Math.Max(1, (int)(bmp.Height * (windowScaling / 2.0f)));

                bmp = new System.Drawing.Bitmap(bmp, newWidth, newHeight);
            }

            var bmpData =
                bmp.LockBits(
                    new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height),
                    System.Drawing.Imaging.ImageLockMode.ReadOnly,
                    System.Drawing.Imaging.PixelFormat.Format32bppPArgb);

            var stream = new DataStream(bmpData.Scan0, bmpData.Stride * bmpData.Height, true, false);
            var pFormat = new PixelFormat(Format.B8G8R8A8_UNorm, AlphaMode.Premultiplied);
            var bmpProps = new BitmapProperties(pFormat);

            var result =
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

        public Bitmap CreateBitmapFromOffscreenGraphics(Direct2DOffscreenGraphics g)
        {
            BitmapProperties bmpProps = new BitmapProperties(new PixelFormat(Format.B8G8R8A8_UNorm, AlphaMode.Premultiplied));
            return new Bitmap(renderTarget, g.Surface, bmpProps);
        }

        public float GetBitmapWidth(Bitmap bmp)
        {
            return bmp.Size.Width;
        }

        public Brush GetSolidBrush(Color color, float dimming = 1.0f, float alphaDimming = 1.0f)
        {
            Brush brush;

            Color color2 = Color.FromArgb(
                Utils.Clamp((int)(color.A * alphaDimming), 0, 255),
                Utils.Clamp((int)(color.R * dimming), 0, 255),
                Utils.Clamp((int)(color.G * dimming), 0, 255),
                Utils.Clamp((int)(color.B * dimming), 0, 255));

            if (solidGradientCache.TryGetValue(color2, out brush))
                return brush;

            brush = CreateSolidBrush(color2);
            solidGradientCache[color2] = brush;

            return brush;
        }

        public Brush GetVerticalGradientBrush(Color color1, int sizeY, float dimming)
        {
            var key = new Tuple<Color, int>(color1, sizeY);

            Brush brush;
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

    }

    public class Direct2DOffscreenGraphics : Direct2DGraphics
    {
        // Only used when doing offline rendering.
        static protected int d3dDeviceRef = 0;
        static protected SharpDX.Direct3D11.Device d3dDevice;

        protected SharpDX.Direct3D11.Texture2D offscreenTexture;
        protected SharpDX.Direct3D11.Texture2D stagingTexture;

        public Surface Surface => offscreenTexture.QueryInterface<SharpDX.DXGI.Surface>();

        private Direct2DOffscreenGraphics(int imageSizeX, int imageSizeY, bool allowReadback)
        {            
			try
            {
	            if (d3dDevice == null)
	            {
	                d3dDevice = new SharpDX.Direct3D11.Device(SharpDX.Direct3D.DriverType.Hardware, SharpDX.Direct3D11.DeviceCreationFlags.BgraSupport);
	                d3dDeviceRef = 1;
	            }
	            else
	            {
	                d3dDeviceRef++;
	            }

	            offscreenTexture = new SharpDX.Direct3D11.Texture2D(d3dDevice, new SharpDX.Direct3D11.Texture2DDescription
	            {
	                BindFlags = SharpDX.Direct3D11.BindFlags.RenderTarget | SharpDX.Direct3D11.BindFlags.ShaderResource,
	                CpuAccessFlags = SharpDX.Direct3D11.CpuAccessFlags.None,
	                Format = Format.B8G8R8A8_UNorm,
	                Width = imageSizeX,
	                Height = imageSizeY,
	                MipLevels = 1,
	                ArraySize = 1,
	                OptionFlags = SharpDX.Direct3D11.ResourceOptionFlags.None,
	                SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
	                Usage = SharpDX.Direct3D11.ResourceUsage.Default
	            });

	            if (allowReadback)
	            {
	                stagingTexture = new SharpDX.Direct3D11.Texture2D(d3dDevice, new SharpDX.Direct3D11.Texture2DDescription
	                {
	                    BindFlags = SharpDX.Direct3D11.BindFlags.None,
	                    CpuAccessFlags = SharpDX.Direct3D11.CpuAccessFlags.Read,
	                    Format = Format.B8G8R8A8_UNorm,
	                    Width = imageSizeX,
	                    Height = imageSizeY,
	                    MipLevels = 1,
	                    ArraySize = 1,
	                    OptionFlags = SharpDX.Direct3D11.ResourceOptionFlags.None,
	                    SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
	                    Usage = SharpDX.Direct3D11.ResourceUsage.Staging
	                });
	            }

	            windowScaling = 1.0f; // No scaling for now in videos.
	            CreateFactory();
	            renderTarget = new RenderTarget(factory, offscreenTexture.QueryInterface<SharpDX.DXGI.Surface>(), new RenderTargetProperties(new SharpDX.Direct2D1.PixelFormat(Format.Unknown, AlphaMode.Premultiplied)));
            }
            catch (Exception e)
            {
                Log.LogMessage(LogSeverity.Error, e.Message);
                Log.LogMessage(LogSeverity.Error, "Error initializing D3D11. This can happen on Windows 7 system that are missing some D3D11 components such as the KB2670838 update.");
                return;
            }
            
            Initialize();
        }

        public static Direct2DOffscreenGraphics Create(int imageSizeX, int imageSizeY, bool allowReadback)
        {
            var gfx = new Direct2DOffscreenGraphics(imageSizeX, imageSizeY, allowReadback);

            if (gfx.renderTarget == null)
            {
                gfx.Dispose();
                return null;
            }

            return gfx;
        }

        public bool InitializedSuccessfully => renderTarget != null;

        public override void Dispose()
        {
            base.Dispose();

            Utils.DisposeAndNullify(ref offscreenTexture);
            Utils.DisposeAndNullify(ref stagingTexture);

            if (--d3dDeviceRef == 0)
                Utils.DisposeAndNullify(ref d3dDevice);
        }

        public unsafe void GetBitmap(byte[] data)
        {
            int textureWidth  = stagingTexture.Description.Width;
            int textureHeight = stagingTexture.Description.Height;

            Debug.Assert(data.Length == offscreenTexture.Description.Width * offscreenTexture.Description.Height * 4);

            d3dDevice.ImmediateContext.CopyResource(offscreenTexture, stagingTexture);
            var mapSource = d3dDevice.ImmediateContext.MapSubresource(stagingTexture, 0, SharpDX.Direct3D11.MapMode.Read, SharpDX.Direct3D11.MapFlags.None);

            byte* row = (byte*)mapSource.DataPointer.ToPointer();

            for (int y = 0; y < textureHeight; y++)
            {
                byte* p = row;
                for (int x = 0; x < textureWidth * 4; x += 4)
                {
                    data[y * textureWidth * 4 + x + 3] = *p++;
                    data[y * textureWidth * 4 + x + 2] = *p++;
                    data[y * textureWidth * 4 + x + 1] = *p++;
                    data[y * textureWidth * 4 + x + 0] = *p++;
                }
                row += mapSource.RowPitch;
            }

            d3dDevice.ImmediateContext.UnmapSubresource(stagingTexture, 0);
        }
    }
}
