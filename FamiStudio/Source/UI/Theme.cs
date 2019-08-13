using System;
using System.Drawing.Text;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using SharpDX.Mathematics.Interop;

namespace FamiStudio
{
    class Theme
    {
        public static SharpDX.DirectWrite.Factory directWriteFactory;
        private static ResourceFontLoader resourceFontLoader;
        private static SharpDX.DirectWrite.FontCollection fontCollection;

        public static TextFormat FontSmall;
        public static TextFormat FontSmallCenter;
        public static TextFormat FontSmallRight;
        public static TextFormat FontMedium;
        public static TextFormat FontMediumCenter;
        public static TextFormat FontMediumRight;
        public static TextFormat FontBig;

        public static TextFormat FontSmallBold;
        public static TextFormat FontMediumBold;
        public static TextFormat FontMediumBoldCenter;
        public static TextFormat FontMediumBoldCenterEllipsis;
        public static TextFormat FontBigBold;
        public static TextFormat FontHuge;

        public static PrivateFontCollection PrivateFontCollection;

        public static RawColor4 DarkGreyLineColor1  = MakeRawColor4(  0,   0,   0);
        public static RawColor4 DarkGreyLineColor2  = MakeRawColor4( 33,  37,  41);
        public static RawColor4 DarkGreyFillColor1  = MakeRawColor4( 42,  48,  51);
        public static RawColor4 DarkGreyFillColor2  = MakeRawColor4( 49,  55,  61);
        public static RawColor4 LightGreyFillColor1 = MakeRawColor4(178, 185, 198);
        public static RawColor4 LightGreyFillColor2 = MakeRawColor4(198, 205, 218);

        public static RawColor4 BlackColor = MakeRawColor4(0, 0,   0);
        public static RawColor4 GreenColor = MakeRawColor4(0, 0, 255);

        public Brush BlackBrush          { get; private set; }
        public Brush LightGreyFillBrush1 { get; private set; }
        public Brush LightGreyFillBrush2 { get; private set; }
        public Brush DarkGreyLineBrush1  { get; private set; }
        public Brush DarkGreyLineBrush2  { get; private set; }
        public Brush DarkGreyFillBrush1  { get; private set; }
        public Brush DarkGreyFillBrush2  { get; private set; }

        [DllImport("kernel32.dll")]
        private static extern IntPtr LoadLibrary(string dllToLoad);
        [DllImport("user32.dll")]
        private static extern IntPtr LoadCursor(IntPtr hInstance, UInt16 lpCursorName);

        private static IntPtr OleLibrary;
        private static IntPtr DragCursorHandle;
        private static IntPtr CopyCursorHandle;
        public static Cursor DragCursor;
        public static Cursor CopyCursor;

        public static System.Drawing.Color[,] CustomColors = new System.Drawing.Color[5, 4]
        {
            {
                System.Drawing.Color.FromArgb(unchecked((int)0xfff44336)),
                System.Drawing.Color.FromArgb(unchecked((int)0xffe91e63)),
                System.Drawing.Color.FromArgb(unchecked((int)0xff9c27b0)),
                System.Drawing.Color.FromArgb(unchecked((int)0xff673ab7))
            },
            {
                System.Drawing.Color.FromArgb(unchecked((int)0xff3f51b5)),
                System.Drawing.Color.FromArgb(unchecked((int)0xff2196f3)),
                System.Drawing.Color.FromArgb(unchecked((int)0xff03a9f4)),
                System.Drawing.Color.FromArgb(unchecked((int)0xff00bcd4))
            },
            {
                System.Drawing.Color.FromArgb(unchecked((int)0xff009688)),
                System.Drawing.Color.FromArgb(unchecked((int)0xff4caf50)),
                System.Drawing.Color.FromArgb(unchecked((int)0xff8bc34a)),
                System.Drawing.Color.FromArgb(unchecked((int)0xffcddc39))
            },
            {
                System.Drawing.Color.FromArgb(unchecked((int)0xffffeb3b)),
                System.Drawing.Color.FromArgb(unchecked((int)0xffffc107)),
                System.Drawing.Color.FromArgb(unchecked((int)0xffff9800)),
                System.Drawing.Color.FromArgb(unchecked((int)0xffff5722))
            },
            {
                System.Drawing.Color.FromArgb(unchecked((int)0xff795548)),
                System.Drawing.Color.FromArgb(unchecked((int)0xff607d8b)),
                System.Drawing.Color.FromArgb(unchecked((int)0xff767d8a)), // LightGreyFillColor1
                System.Drawing.Color.FromArgb(unchecked((int)0xffc6cdda))  // LightGreyFillColor2
                
            }
        };

        private static int nextColorIdx = 0;
        //public static Random random = new Random(123);

        public static System.Drawing.Color RandomCustomColor()
        {
            var si = CustomColors.GetLength(0);
            var sj = CustomColors.GetLength(1);

            var i = nextColorIdx % si;
            var j = nextColorIdx / si;

            nextColorIdx = (nextColorIdx + 1) % (si * sj);

            return CustomColors[i, j];
        }

        [DllImport("gdi32.dll")]
        private static extern IntPtr AddFontMemResourceEx(IntPtr pbFont, uint cbFont, IntPtr pdv, [In] ref uint pcFonts);

        public static void Initialize()
        {
            directWriteFactory = new SharpDX.DirectWrite.Factory();
            resourceFontLoader = new ResourceFontLoader(directWriteFactory);
            fontCollection     = new SharpDX.DirectWrite.FontCollection(directWriteFactory, resourceFontLoader, resourceFontLoader.Key);

            FontSmall  = new TextFormat(directWriteFactory, "Quicksand", fontCollection, FontWeight.Regular, FontStyle.Normal, FontStretch.Normal, 10.0f);
            FontMedium = new TextFormat(directWriteFactory, "Quicksand", fontCollection, FontWeight.Regular, FontStyle.Normal, FontStretch.Normal, 12.0f);
            FontBig   = new TextFormat(directWriteFactory, "Quicksand", fontCollection, FontWeight.Regular, FontStyle.Normal, FontStretch.Normal, 20.0f);
            FontHuge = new TextFormat(directWriteFactory, "Quicksand", fontCollection, FontWeight.Regular, FontStyle.Normal, FontStretch.Normal, 28.0f);

            FontSmallBold = new TextFormat(directWriteFactory, "Quicksand", fontCollection, FontWeight.Bold, FontStyle.Normal, FontStretch.Normal, 10.0f);
            FontMediumBold = new TextFormat(directWriteFactory, "Quicksand", fontCollection, FontWeight.Bold, FontStyle.Normal, FontStretch.Normal, 12.0f);
            FontBigBold = new TextFormat(directWriteFactory, "Quicksand", fontCollection, FontWeight.Bold, FontStyle.Normal, FontStretch.Normal, 20.0f);

            FontSmallCenter = new TextFormat(directWriteFactory, "Quicksand", fontCollection, FontWeight.Regular, FontStyle.Normal, FontStretch.Normal, 10.0f);
            FontSmallCenter.TextAlignment = TextAlignment.Center;
            FontSmallCenter.WordWrapping = WordWrapping.NoWrap;

            FontSmallRight = new TextFormat(directWriteFactory, "Quicksand", fontCollection, FontWeight.Regular, FontStyle.Normal, FontStretch.Normal, 10.0f);
            FontSmallRight.TextAlignment = TextAlignment.Trailing;
            FontSmallRight.WordWrapping = WordWrapping.NoWrap;

            FontMediumRight = new TextFormat(directWriteFactory, "Quicksand", fontCollection, FontWeight.Regular, FontStyle.Normal, FontStretch.Normal, 12.0f);
            FontMediumRight.TextAlignment = TextAlignment.Trailing;
            FontMediumRight.WordWrapping = WordWrapping.NoWrap;

            FontMediumCenter = new TextFormat(directWriteFactory, "Quicksand", fontCollection, FontWeight.Regular, FontStyle.Normal, FontStretch.Normal, 12.0f);
            FontMediumCenter.TextAlignment = TextAlignment.Center;
            FontMediumCenter.WordWrapping = WordWrapping.NoWrap;

            FontMediumBoldCenter = new TextFormat(directWriteFactory, "Quicksand", fontCollection, FontWeight.Bold, FontStyle.Normal, FontStretch.Normal, 12.0f);
            FontMediumBoldCenter.TextAlignment = TextAlignment.Center;
            FontMediumBoldCenter.WordWrapping = WordWrapping.NoWrap;

            FontMediumBoldCenterEllipsis = new TextFormat(directWriteFactory, "Quicksand", fontCollection, FontWeight.Bold, FontStyle.Normal, FontStretch.Normal, 12.0f);
            FontMediumBoldCenterEllipsis.TextAlignment = TextAlignment.Center;
            FontMediumBoldCenterEllipsis.WordWrapping = WordWrapping.NoWrap;
            var trimmingSign = new EllipsisTrimming(directWriteFactory, FontMediumBoldCenterEllipsis);
            FontMediumBoldCenterEllipsis.SetTrimming(new Trimming() { Delimiter = (int)')', Granularity = TrimmingGranularity.Character, DelimiterCount = 1 }, trimmingSign);



            PrivateFontCollection = new PrivateFontCollection();
            AddFontFromMemory(PrivateFontCollection, "FamiStudio.Resources.Quicksand-Regular.ttf");
            AddFontFromMemory(PrivateFontCollection, "FamiStudio.Resources.Quicksand-Bold.ttf");

            for (int j = 0; j < CustomColors.GetLength(1); j++)
            {
                for (int i = 0; i < CustomColors.GetLength(0); i++)
                {
                    if (i == CustomColors.GetLength(0) - 1 &&
                        j == CustomColors.GetLength(1) - 1)
                    {
                        continue;
                    }

                    var color = CustomColors[i, j];
                    CustomColors[i, j] = System.Drawing.Color.FromArgb(
                       Math.Min(255, color.R + 60),
                       Math.Min(255, color.G + 60),
                       Math.Min(255, color.B + 60));
                }
            }

            OleLibrary = LoadLibrary("ole32.dll");
            DragCursorHandle = LoadCursor(OleLibrary, 2);
            CopyCursorHandle = LoadCursor(OleLibrary, 3);
            DragCursor = new Cursor(DragCursorHandle);
            CopyCursor = new Cursor(CopyCursorHandle);
        }

        private static void AddFontFromMemory(PrivateFontCollection pfc, string name)
        {
            var fontStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name);

            byte[] fontdata = new byte[fontStream.Length];
            fontStream.Read(fontdata, 0, (int)fontStream.Length);
            fontStream.Close();

            uint c = 0;
            var p = Marshal.AllocCoTaskMem(fontdata.Length);
            Marshal.Copy(fontdata, 0, p, fontdata.Length);
            AddFontMemResourceEx(p, (uint)fontdata.Length, IntPtr.Zero, ref c);
            pfc.AddMemoryFont(p, fontdata.Length);
            Marshal.FreeCoTaskMem(p);
        }

        public static Theme CreateResourcesForGraphics(Direct2DGraphics g)
        {
            var theme = new Theme();

            theme.BlackBrush          = g.CreateSolidBrush(BlackColor);
            theme.LightGreyFillBrush1 = g.CreateSolidBrush(LightGreyFillColor1);
            theme.LightGreyFillBrush2 = g.CreateSolidBrush(LightGreyFillColor2);
            theme.DarkGreyLineBrush1  = g.CreateSolidBrush(DarkGreyLineColor1);
            theme.DarkGreyLineBrush2  = g.CreateSolidBrush(DarkGreyLineColor2);
            theme.DarkGreyFillBrush1  = g.CreateSolidBrush(DarkGreyFillColor1);
            theme.DarkGreyFillBrush2  = g.CreateSolidBrush(DarkGreyFillColor2);
            
            return theme;
        }

        public static RawColor4 Darken(RawColor4 color)
        {
            color.R = Math.Max(0.0f, color.R - 0.2f);
            color.G = Math.Max(0.0f, color.G - 0.2f);
            color.B = Math.Max(0.0f, color.B - 0.2f);
            return color;
        }

        public static RawColor4 Lighten(RawColor4 color)
        {
            color.R = Math.Min(1.0f, color.R + 0.2f);
            color.G = Math.Min(1.0f, color.G + 0.2f);
            color.B = Math.Min(1.0f, color.B + 0.2f);
            return color;
        }

        private static RawColor4 MakeRawColor4(int r, int g, int b)
        {
            return new RawColor4(r / 255.0f, g / 255.0f, b / 255.0f, 1.0f);
        }
    }
}
