using System;
using System.Drawing.Text;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Drawing;
using System.Collections.Generic;

#if FAMISTUDIO_WINDOWS
using RenderFont     = SharpDX.DirectWrite.TextFormat;
    using RenderBrush    = SharpDX.Direct2D1.Brush;
    using RenderGraphics = FamiStudio.Direct2DGraphics;
#else
    using RenderFont     = FamiStudio.GLFont;
    using RenderBrush    = FamiStudio.GLBrush;
    using RenderGraphics = FamiStudio.GLGraphics;
#endif

namespace FamiStudio
{
    class ThemeBase
    {
        protected enum RenderFontStyle
        {
            VerySmallCenter,
            Small,
            SmallCenter,
            SmallRight,
            SmallBold,
            Medium,
            MediumCenter,
            MediumRight,
            MediumBold,
            MediumBoldCenter,
            MediumBoldCenterEllipsis,
            MediumBigRight,
            Big,
            BigBold,
            BigCenter,
            BigUnscaled,
            Huge,
            Max
        };

        protected struct RenderFontDefinition
        {
            public string Name;
            public bool   Bold;
            public bool   Ellipsis;
            public bool   NoScaling;
            public int    Alignment; // TODO: Create an enum.
            public int    Size;
        };

        protected static RenderFontDefinition[] FontDefinitions = new RenderFontDefinition[(int)RenderFontStyle.Max]
        {
            new RenderFontDefinition() { Name = "QuickSand", Size =  8, Alignment = 1 }, // VerySmallCenter
            new RenderFontDefinition() { Name = "QuickSand", Size = 10 }, // Small
            new RenderFontDefinition() { Name = "QuickSand", Size = 10, Alignment = 1 }, // SmallCenter
            new RenderFontDefinition() { Name = "QuickSand", Size = 10, Alignment = 2 }, // SmallRight
            new RenderFontDefinition() { Name = "QuickSand", Size = 10, Bold = true }, // SmallBold
            new RenderFontDefinition() { Name = "QuickSand", Size = 12 }, // Medium
            new RenderFontDefinition() { Name = "QuickSand", Size = 12, Alignment = 1 }, // MediumCenter
            new RenderFontDefinition() { Name = "QuickSand", Size = 12, Alignment = 2 }, // MediumRight
            new RenderFontDefinition() { Name = "QuickSand", Size = 12, Bold = true }, // MediumBold
            new RenderFontDefinition() { Name = "QuickSand", Size = 12, Alignment = 1, Bold = true }, // MediumBoldCenter
            new RenderFontDefinition() { Name = "QuickSand", Size = 12, Alignment = 1, Bold = true, Ellipsis = true }, // MediumBoldCenterEllipsis
            new RenderFontDefinition() { Name = "QuickSand", Size = 16, Alignment = 2 }, // MediumBigRight
            new RenderFontDefinition() { Name = "QuickSand", Size = 20 }, // Big
            new RenderFontDefinition() { Name = "QuickSand", Size = 20, Bold = true }, // BigBold
            new RenderFontDefinition() { Name = "QuickSand", Size = 20, Alignment = 1 }, // BigCenter
            new RenderFontDefinition() { Name = "QuickSand", Size = 20, NoScaling = true }, // BigUnscaled
            new RenderFontDefinition() { Name = "QuickSand", Size = 28, Alignment = 1 } // Huge
        };

        protected static RenderFont[] Fonts = new RenderFont[(int)RenderFontStyle.Max];

        public static RenderFont FontVerySmallCenter          => Fonts[(int)RenderFontStyle.VerySmallCenter];
        public static RenderFont FontSmall                    => Fonts[(int)RenderFontStyle.Small];
        public static RenderFont FontSmallCenter              => Fonts[(int)RenderFontStyle.SmallCenter];
        public static RenderFont FontSmallRight               => Fonts[(int)RenderFontStyle.SmallRight];
        public static RenderFont FontSmallBold                => Fonts[(int)RenderFontStyle.SmallBold];
        public static RenderFont FontMedium                   => Fonts[(int)RenderFontStyle.Medium];
        public static RenderFont FontMediumCenter             => Fonts[(int)RenderFontStyle.MediumCenter];
        public static RenderFont FontMediumRight              => Fonts[(int)RenderFontStyle.MediumRight];
        public static RenderFont FontMediumBold               => Fonts[(int)RenderFontStyle.MediumBold];
        public static RenderFont FontMediumBoldCenter         => Fonts[(int)RenderFontStyle.MediumBoldCenter];
        public static RenderFont FontMediumBoldCenterEllipsis => Fonts[(int)RenderFontStyle.MediumBoldCenterEllipsis];
        public static RenderFont FontMediumBigRight           => Fonts[(int)RenderFontStyle.MediumBigRight];
        public static RenderFont FontBig                      => Fonts[(int)RenderFontStyle.Big];
        public static RenderFont FontBigBold                  => Fonts[(int)RenderFontStyle.BigBold];
        public static RenderFont FontBigCenter                => Fonts[(int)RenderFontStyle.BigCenter];
        public static RenderFont FontBigUnscaled              => Fonts[(int)RenderFontStyle.BigUnscaled];
        public static RenderFont FontHuge                     => Fonts[(int)RenderFontStyle.Huge];

        public static Color DarkGreyLineColor1    = Color.FromArgb( 25,  28,  31);
        public static Color DarkGreyLineColor2    = Color.FromArgb( 33,  37,  41);
        public static Color DarkGreyLineColor3    = Color.FromArgb( 38,  42,  46);
        public static Color DarkGreyFillColor1    = Color.FromArgb( 42,  48,  51);
        public static Color DarkGreyFillColor2    = Color.FromArgb( 49,  55,  61);
        public static Color MediumGreyFillColor1  = Color.FromArgb( 86,  91, 105);
        public static Color LightGreyFillColor1   = Color.FromArgb(178, 185, 198);
        public static Color LightGreyFillColor2   = Color.FromArgb(198, 205, 218);
        public static Color SeekBarColor          = Color.FromArgb(225, 170,   0);
        public static Color LightRedFillColor     = Color.FromArgb(225, 150, 150);
        public static Color DarkRedFillColor      = Color.FromArgb(210,  16,  48);

        public static Color BlackColor = Color.FromArgb(  0,   0,   0);
        public static Color GreenColor = Color.FromArgb(  0,   0, 255);
        public static Color WhiteColor = Color.FromArgb(255, 255, 255);

        private RenderBrush whiteBrush;
        private RenderBrush blackBrush;
        private RenderBrush lightGreyFillBrush1;
        private RenderBrush lightGreyFillBrush2;
        private RenderBrush mediumGreyFillBrush1;
        private RenderBrush darkGreyLineBrush1;
        private RenderBrush darkGreyLineBrush2;
        private RenderBrush darkGreyLineBrush3;
        private RenderBrush darkGreyFillBrush1;
        private RenderBrush darkGreyFillBrush2;
        private RenderBrush lightRedFillBrush;
        private RenderBrush darkRedFillBrush;

        public RenderBrush WhiteBrush           { get => whiteBrush;           protected set => whiteBrush           = value; }
        public RenderBrush BlackBrush           { get => blackBrush;           protected set => blackBrush           = value; }
        public RenderBrush LightGreyFillBrush1  { get => lightGreyFillBrush1;  protected set => lightGreyFillBrush1  = value; }
        public RenderBrush LightGreyFillBrush2  { get => lightGreyFillBrush2;  protected set => lightGreyFillBrush2  = value; }
        public RenderBrush MediumGreyFillBrush1 { get => mediumGreyFillBrush1; protected set => mediumGreyFillBrush1 = value; }
        public RenderBrush DarkGreyLineBrush1   { get => darkGreyLineBrush1;   protected set => darkGreyLineBrush1   = value; }
        public RenderBrush DarkGreyLineBrush2   { get => darkGreyLineBrush2;   protected set => darkGreyLineBrush2   = value; }
        public RenderBrush DarkGreyLineBrush3   { get => darkGreyLineBrush3;   protected set => darkGreyLineBrush3   = value; }
        public RenderBrush DarkGreyFillBrush1   { get => darkGreyFillBrush1;   protected set => darkGreyFillBrush1   = value; }
        public RenderBrush DarkGreyFillBrush2   { get => darkGreyFillBrush2;   protected set => darkGreyFillBrush2   = value; }
        public RenderBrush LightRedFillBrush    { get => lightRedFillBrush;    protected set => lightRedFillBrush    = value; }
        public RenderBrush DarkRedFillBrush     { get => darkRedFillBrush;     protected set => darkRedFillBrush     = value; }

        private static int nextColorIdx = 48;

        //
        // These are some of the shades (300 to 800) for most of the Google Material Design colors.
        // Brown and gray were removed.
        //
        // https://material.io/design/color/the-color-system.html
        //

        public static Color[,] CustomColors = new Color[16, 6]
        {
            {
                Color.FromArgb(unchecked((int)0xffe57373)),
                Color.FromArgb(unchecked((int)0xffef5350)),
                Color.FromArgb(unchecked((int)0xfff44336)),
                Color.FromArgb(unchecked((int)0xffe53935)),
                Color.FromArgb(unchecked((int)0xffd32f2f)),
                Color.FromArgb(unchecked((int)0xffc62828)),
            },
            {
                Color.FromArgb(unchecked((int)0xfff06292)),
                Color.FromArgb(unchecked((int)0xffec407a)),
                Color.FromArgb(unchecked((int)0xffe91e63)),
                Color.FromArgb(unchecked((int)0xffd81b60)),
                Color.FromArgb(unchecked((int)0xffc2185b)),
                Color.FromArgb(unchecked((int)0xffad1457)),
            },
            {
                Color.FromArgb(unchecked((int)0xffba68c8)),
                Color.FromArgb(unchecked((int)0xffab47bc)),
                Color.FromArgb(unchecked((int)0xff9c27b0)),
                Color.FromArgb(unchecked((int)0xff8e24aa)),
                Color.FromArgb(unchecked((int)0xff7b1fa2)),
                Color.FromArgb(unchecked((int)0xff6a1b9a)),
            },
            {
                Color.FromArgb(unchecked((int)0xff9575cd)),
                Color.FromArgb(unchecked((int)0xff7e57c2)),
                Color.FromArgb(unchecked((int)0xff673ab7)),
                Color.FromArgb(unchecked((int)0xff5e35b1)),
                Color.FromArgb(unchecked((int)0xff512da8)),
                Color.FromArgb(unchecked((int)0xff4527a0)),
            },
            {
                Color.FromArgb(unchecked((int)0xff7986cb)),
                Color.FromArgb(unchecked((int)0xff5c6bc0)),
                Color.FromArgb(unchecked((int)0xff3f51b5)),
                Color.FromArgb(unchecked((int)0xff3949ab)),
                Color.FromArgb(unchecked((int)0xff303f9f)),
                Color.FromArgb(unchecked((int)0xff283593)),
            },
            {
                Color.FromArgb(unchecked((int)0xff64b5f6)),
                Color.FromArgb(unchecked((int)0xff42a5f5)),
                Color.FromArgb(unchecked((int)0xff2196f3)),
                Color.FromArgb(unchecked((int)0xff1e88e5)),
                Color.FromArgb(unchecked((int)0xff1976d2)),
                Color.FromArgb(unchecked((int)0xff1565c0)),
            },
            {
                Color.FromArgb(unchecked((int)0xff4fc3f7)),
                Color.FromArgb(unchecked((int)0xff29b6f6)),
                Color.FromArgb(unchecked((int)0xff03a9f4)),
                Color.FromArgb(unchecked((int)0xff039be5)),
                Color.FromArgb(unchecked((int)0xff0288d1)),
                Color.FromArgb(unchecked((int)0xff0277bd)),
            },
            {
                Color.FromArgb(unchecked((int)0xff4dd0e1)),
                Color.FromArgb(unchecked((int)0xff26c6da)),
                Color.FromArgb(unchecked((int)0xff00bcd4)),
                Color.FromArgb(unchecked((int)0xff00acc1)),
                Color.FromArgb(unchecked((int)0xff0097a7)),
                Color.FromArgb(unchecked((int)0xff00838f)),
            },
            {
                Color.FromArgb(unchecked((int)0xff4db6ac)),
                Color.FromArgb(unchecked((int)0xff26a69a)),
                Color.FromArgb(unchecked((int)0xff009688)),
                Color.FromArgb(unchecked((int)0xff00897b)),
                Color.FromArgb(unchecked((int)0xff00796b)),
                Color.FromArgb(unchecked((int)0xff00695c)),
            },
            {
                Color.FromArgb(unchecked((int)0xff81c784)),
                Color.FromArgb(unchecked((int)0xff66bb6a)),
                Color.FromArgb(unchecked((int)0xff4caf50)),
                Color.FromArgb(unchecked((int)0xff43a047)),
                Color.FromArgb(unchecked((int)0xff388e3c)),
                Color.FromArgb(unchecked((int)0xff2e7d32)),
            },
            {
                Color.FromArgb(unchecked((int)0xffaed581)),
                Color.FromArgb(unchecked((int)0xff9ccc65)),
                Color.FromArgb(unchecked((int)0xff8bc34a)),
                Color.FromArgb(unchecked((int)0xff7cb342)),
                Color.FromArgb(unchecked((int)0xff689f38)),
                Color.FromArgb(unchecked((int)0xff558b2f)),
            },
            {
                Color.FromArgb(unchecked((int)0xffdce775)),
                Color.FromArgb(unchecked((int)0xffd4e157)),
                Color.FromArgb(unchecked((int)0xffcddc39)),
                Color.FromArgb(unchecked((int)0xffc0ca33)),
                Color.FromArgb(unchecked((int)0xffafb42b)),
                Color.FromArgb(unchecked((int)0xff9e9d24)),
            },
            {
                Color.FromArgb(unchecked((int)0xfffff176)),
                Color.FromArgb(unchecked((int)0xffffee58)),
                Color.FromArgb(unchecked((int)0xffffeb3b)),
                Color.FromArgb(unchecked((int)0xfffdd835)),
                Color.FromArgb(unchecked((int)0xfffbc02d)),
                Color.FromArgb(unchecked((int)0xfff9a825)),
            },
            {
                Color.FromArgb(unchecked((int)0xffffd54f)),
                Color.FromArgb(unchecked((int)0xffffca28)),
                Color.FromArgb(unchecked((int)0xffffc107)),
                Color.FromArgb(unchecked((int)0xffffb300)),
                Color.FromArgb(unchecked((int)0xffffa000)),
                Color.FromArgb(unchecked((int)0xffff8f00)),
            },
            {
                Color.FromArgb(unchecked((int)0xffffb74d)),
                Color.FromArgb(unchecked((int)0xffffa726)),
                Color.FromArgb(unchecked((int)0xffff9800)),
                Color.FromArgb(unchecked((int)0xfffb8c00)),
                Color.FromArgb(unchecked((int)0xfff57c00)),
                Color.FromArgb(unchecked((int)0xffef6c00)),
            },
            {
                Color.FromArgb(unchecked((int)0xffff8a65)),
                Color.FromArgb(unchecked((int)0xffff7043)),
                Color.FromArgb(unchecked((int)0xffff5722)),
                Color.FromArgb(unchecked((int)0xfff4511e)),
                Color.FromArgb(unchecked((int)0xffe64a19)),
                Color.FromArgb(unchecked((int)0xffd84315)),
            },
        };

        private Dictionary<Color, RenderBrush> customColorBrushes = new Dictionary<Color, RenderBrush>();
        public  Dictionary<Color, RenderBrush> CustomColorBrushes => customColorBrushes;

        public static void InitializeBase()
        {
            for (int j = 0; j < CustomColors.GetLength(1); j++)
            {
                for (int i = 0; i < CustomColors.GetLength(0); i++)
                {
                    var color = CustomColors[i, j];

                    // Make everything more pastel.
                    CustomColors[i, j] = Color.FromArgb(
                       Math.Min(255, color.R + 60),
                       Math.Min(255, color.G + 60),
                       Math.Min(255, color.B + 60));
                }
            }
        }

        public static float ColorDistance(Color c0, Color c1)
        {
            float dr = Math.Abs(c0.R - c1.R);
            float dg = Math.Abs(c0.G - c1.G);
            float db = Math.Abs(c0.B - c1.B);

            return (float)Math.Sqrt(dr * dr + dg * dg + db * db);
        }

        public static void EnforceThemeColor(ref Color color)
        {
            float minDist = 1000.0f;
            Color closestColor = CustomColors[0, 0];

            for (int j = 0; j < CustomColors.GetLength(1); j++)
            {
                for (int i = 0; i < CustomColors.GetLength(0); i++)
                {
                    if (CustomColors[i, j] == color)
                        return;

                    var dist = ColorDistance(color, CustomColors[i, j]);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        closestColor = CustomColors[i, j];
                    }
                }
            }

            color = closestColor;
        }

        public virtual void InitializeForGraphics(RenderGraphics g)
        {
            whiteBrush = g.CreateSolidBrush(WhiteColor);
            blackBrush = g.CreateSolidBrush(BlackColor);
            lightGreyFillBrush1 = g.CreateSolidBrush(LightGreyFillColor1);
            lightGreyFillBrush2 = g.CreateSolidBrush(LightGreyFillColor2);
            mediumGreyFillBrush1 = g.CreateSolidBrush(MediumGreyFillColor1);
            darkGreyLineBrush1 = g.CreateSolidBrush(DarkGreyLineColor1);
            darkGreyLineBrush2 = g.CreateSolidBrush(DarkGreyLineColor2);
            darkGreyLineBrush3 = g.CreateSolidBrush(DarkGreyLineColor3);
            darkGreyFillBrush1 = g.CreateSolidBrush(DarkGreyFillColor1);
            darkGreyFillBrush2 = g.CreateSolidBrush(DarkGreyFillColor2);
            lightRedFillBrush = g.CreateSolidBrush(LightRedFillColor);
            darkRedFillBrush = g.CreateSolidBrush(DarkRedFillColor);

            for (int j = 0; j < CustomColors.GetLength(1); j++)
            {
                for (int i = 0; i < CustomColors.GetLength(0); i++)
                {
                    customColorBrushes[CustomColors[i, j]] = g.CreateSolidBrush(CustomColors[i, j]);
                }
            }
        }

        public void Terminate()
        {
            Utils.DisposeAndNullify(ref whiteBrush);
            Utils.DisposeAndNullify(ref blackBrush);
            Utils.DisposeAndNullify(ref lightGreyFillBrush1);
            Utils.DisposeAndNullify(ref lightGreyFillBrush2);
            Utils.DisposeAndNullify(ref mediumGreyFillBrush1);
            Utils.DisposeAndNullify(ref darkGreyLineBrush1);
            Utils.DisposeAndNullify(ref darkGreyLineBrush2);
            Utils.DisposeAndNullify(ref darkGreyLineBrush3);
            Utils.DisposeAndNullify(ref darkGreyFillBrush1);
            Utils.DisposeAndNullify(ref darkGreyFillBrush2);
            Utils.DisposeAndNullify(ref lightRedFillBrush);
            Utils.DisposeAndNullify(ref darkRedFillBrush);

            for (int j = 0; j < CustomColors.GetLength(1); j++)
            {
                for (int i = 0; i < CustomColors.GetLength(0); i++)
                {
                    customColorBrushes[CustomColors[i, j]].Dispose();
                }
            }

            customColorBrushes.Clear();
        }

        public static System.Drawing.Color RandomCustomColor()
        {
            var si = CustomColors.GetLength(0);
            var sj = CustomColors.GetLength(1);

            var i = nextColorIdx % si;
            var j = nextColorIdx / si;

            nextColorIdx = (nextColorIdx + 13) % (si * sj);

            return CustomColors[i, j];
        }

        public static int GetCustomColorIndex(Color color)
        {
            for (int j = 0; j < CustomColors.GetLength(1); j++)
            {
                for (int i = 0; i < CustomColors.GetLength(0); i++)
                {
                    if (CustomColors[i, j] == color) return j * CustomColors.GetLength(0) + i;
                }
            }

            return 0;
        }

        public static Color Darken(Color color)
        {
            return Color.FromArgb(
                    Math.Max(0, color.R - 50),
                    Math.Max(0, color.G - 50),
                    Math.Max(0, color.B - 50)
                );
        }

        public static Color Lighten(Color color)
        {
            return Color.FromArgb(
                    Math.Min(255, color.R + 50),
                    Math.Min(255, color.G + 50),
                    Math.Min(255, color.B + 50)
                );
        }
    }
}
