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

        public static Color DarkGreyLineColor1    = Color.FromArgb(  0,   0,   0);
        public static Color DarkGreyLineColor2    = Color.FromArgb( 33,  37,  41);
        public static Color DarkGreyLineColor3    = Color.FromArgb( 38,  42,  46);
        public static Color DarkGreyFillColor1    = Color.FromArgb( 42,  48,  51);
        public static Color DarkGreyFillColor2    = Color.FromArgb( 49,  55,  61);
        public static Color MediumGreyFillColor1  = Color.FromArgb( 86,  91, 105);
        public static Color LightGreyFillColor1   = Color.FromArgb(178, 185, 198);
        public static Color LightGreyFillColor2   = Color.FromArgb(198, 205, 218);
        public static Color SeekBarColor          = Color.FromArgb(225, 170,   0);
        public static Color DarkRedFillColor1     = Color.FromArgb( 92,   0,  16);
        public static Color DarkRedFillColor2     = Color.FromArgb(210,  16,  48);

        public static Color BlackColor = Color.FromArgb(0, 0,   0);
        public static Color GreenColor = Color.FromArgb(0, 0, 255);

        private RenderBrush blackBrush;
        private RenderBrush lightGreyFillBrush1;
        private RenderBrush lightGreyFillBrush2;
        private RenderBrush mediumGreyFillBrush1;
        private RenderBrush darkGreyLineBrush1;
        private RenderBrush darkGreyLineBrush2;
        private RenderBrush darkGreyLineBrush3;
        private RenderBrush darkGreyFillBrush1;
        private RenderBrush darkGreyFillBrush2;
        private RenderBrush darkRedFillBrush1;
        private RenderBrush darkRedFillBrush2;

        public RenderBrush BlackBrush           { get => blackBrush;           protected set => blackBrush           = value; }
        public RenderBrush LightGreyFillBrush1  { get => lightGreyFillBrush1;  protected set => lightGreyFillBrush1  = value; }
        public RenderBrush LightGreyFillBrush2  { get => lightGreyFillBrush2;  protected set => lightGreyFillBrush2  = value; }
        public RenderBrush MediumGreyFillBrush1 { get => mediumGreyFillBrush1; protected set => mediumGreyFillBrush1 = value; }
        public RenderBrush DarkGreyLineBrush1   { get => darkGreyLineBrush1;   protected set => darkGreyLineBrush1   = value; }
        public RenderBrush DarkGreyLineBrush2   { get => darkGreyLineBrush2;   protected set => darkGreyLineBrush2   = value; }
        public RenderBrush DarkGreyLineBrush3   { get => darkGreyLineBrush3;   protected set => darkGreyLineBrush3   = value; }
        public RenderBrush DarkGreyFillBrush1   { get => darkGreyFillBrush1;   protected set => darkGreyFillBrush1   = value; }
        public RenderBrush DarkGreyFillBrush2   { get => darkGreyFillBrush2;   protected set => darkGreyFillBrush2   = value; }
        public RenderBrush DarkRedFillBrush1    { get => darkRedFillBrush1;    protected set => darkRedFillBrush1    = value; }
        public RenderBrush DarkRedFillBrush2    { get => darkRedFillBrush2;    protected set => darkRedFillBrush2    = value; }

        private static int nextColorIdx = 0;
        public static Color[,] CustomColors = new Color[5, 4]
        {
            {
                Color.FromArgb(unchecked((int)0xfff44336)),
                Color.FromArgb(unchecked((int)0xffe91e63)),
                Color.FromArgb(unchecked((int)0xff9c27b0)),
                Color.FromArgb(unchecked((int)0xff673ab7))
            },
            {
                Color.FromArgb(unchecked((int)0xff3f51b5)),
                Color.FromArgb(unchecked((int)0xff2196f3)),
                Color.FromArgb(unchecked((int)0xff03a9f4)),
                Color.FromArgb(unchecked((int)0xff00bcd4))
            },
            {
                Color.FromArgb(unchecked((int)0xff009688)),
                Color.FromArgb(unchecked((int)0xff4caf50)),
                Color.FromArgb(unchecked((int)0xff8bc34a)),
                Color.FromArgb(unchecked((int)0xffcddc39))
            },
            {
                Color.FromArgb(unchecked((int)0xffffeb3b)),
                Color.FromArgb(unchecked((int)0xffffc107)),
                Color.FromArgb(unchecked((int)0xffff9800)),
                Color.FromArgb(unchecked((int)0xffff5722))
            },
            {
                Color.FromArgb(unchecked((int)0xff795548)),
                Color.FromArgb(unchecked((int)0xff607d8b)),
                Color.FromArgb(unchecked((int)0xff767d8a)), // LightGreyFillColor1
                Color.FromArgb(unchecked((int)0xffc6cdda))  // LightGreyFillColor2
            }
        };

        private Dictionary<Color, RenderBrush> customColorBrushes = new Dictionary<Color, RenderBrush>();
        public  Dictionary<Color, RenderBrush> CustomColorBrushes => customColorBrushes;

        public static void InitializeBase()
        {
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
                    CustomColors[i, j] = Color.FromArgb(
                       Math.Min(255, color.R + 60),
                       Math.Min(255, color.G + 60),
                       Math.Min(255, color.B + 60));
                }
            }
        }

        public virtual void InitializeForGraphics(RenderGraphics g)
        {
            blackBrush = g.CreateSolidBrush(BlackColor);
            lightGreyFillBrush1 = g.CreateSolidBrush(LightGreyFillColor1);
            lightGreyFillBrush2 = g.CreateSolidBrush(LightGreyFillColor2);
            mediumGreyFillBrush1 = g.CreateSolidBrush(MediumGreyFillColor1);
            darkGreyLineBrush1 = g.CreateSolidBrush(DarkGreyLineColor1);
            darkGreyLineBrush2 = g.CreateSolidBrush(DarkGreyLineColor2);
            darkGreyLineBrush3 = g.CreateSolidBrush(DarkGreyLineColor3);
            darkGreyFillBrush1 = g.CreateSolidBrush(DarkGreyFillColor1);
            darkGreyFillBrush2 = g.CreateSolidBrush(DarkGreyFillColor2);
            darkRedFillBrush1 = g.CreateSolidBrush(DarkRedFillColor1);
            darkRedFillBrush2 = g.CreateSolidBrush(DarkRedFillColor2);

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
            Utils.DisposeAndNullify(ref blackBrush);
            Utils.DisposeAndNullify(ref lightGreyFillBrush1);
            Utils.DisposeAndNullify(ref lightGreyFillBrush2);
            Utils.DisposeAndNullify(ref mediumGreyFillBrush1);
            Utils.DisposeAndNullify(ref darkGreyLineBrush1);
            Utils.DisposeAndNullify(ref darkGreyLineBrush2);
            Utils.DisposeAndNullify(ref darkGreyLineBrush3);
            Utils.DisposeAndNullify(ref darkGreyFillBrush1);
            Utils.DisposeAndNullify(ref darkGreyFillBrush2);
            Utils.DisposeAndNullify(ref darkRedFillBrush1);
            Utils.DisposeAndNullify(ref darkRedFillBrush2);

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

            nextColorIdx = (nextColorIdx + 1) % (si * sj);

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
