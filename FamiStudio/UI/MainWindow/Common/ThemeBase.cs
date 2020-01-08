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
            Huge,
            Max
        };

        protected struct RenderFontDefinition
        {
            public string Name;
            public bool   Bold;
            public bool   Ellipsis;
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
            new RenderFontDefinition() { Name = "QuickSand", Size = 28 } // Huge
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
        public static RenderFont FontHuge                     => Fonts[(int)RenderFontStyle.Huge];

        public static Color DarkGreyLineColor1    = Color.FromArgb(  0,   0,   0);
        public static Color DarkGreyLineColor2    = Color.FromArgb( 33,  37,  41);
        public static Color DarkGreyFillColor1    = Color.FromArgb( 42,  48,  51);
        public static Color DarkGreyFillColor2    = Color.FromArgb( 49,  55,  61);
        public static Color LightGreyFillColor1   = Color.FromArgb(178, 185, 198);
        public static Color LightGreyFillColor2   = Color.FromArgb(198, 205, 218);
        public static Color SeekBarColor          = Color.FromArgb(225, 170,   0);

        public static Color BlackColor = Color.FromArgb(0, 0,   0);
        public static Color GreenColor = Color.FromArgb(0, 0, 255);

        public RenderBrush BlackBrush          { get; protected set; }
        public RenderBrush LightGreyFillBrush1 { get; protected set; }
        public RenderBrush LightGreyFillBrush2 { get; protected set; }
        public RenderBrush DarkGreyLineBrush1  { get; protected set; }
        public RenderBrush DarkGreyLineBrush2  { get; protected set; }
        public RenderBrush DarkGreyFillBrush1  { get; protected set; }
        public RenderBrush DarkGreyFillBrush2  { get; protected set; }

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

        public Dictionary<Color, RenderBrush> CustomColorBrushes = new Dictionary<Color, RenderBrush>();

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
            BlackBrush = g.CreateSolidBrush(BlackColor);
            LightGreyFillBrush1 = g.CreateSolidBrush(LightGreyFillColor1);
            LightGreyFillBrush2 = g.CreateSolidBrush(LightGreyFillColor2);
            DarkGreyLineBrush1 = g.CreateSolidBrush(DarkGreyLineColor1);
            DarkGreyLineBrush2 = g.CreateSolidBrush(DarkGreyLineColor2);
            DarkGreyFillBrush1 = g.CreateSolidBrush(DarkGreyFillColor1);
            DarkGreyFillBrush2 = g.CreateSolidBrush(DarkGreyFillColor2);

            for (int j = 0; j < CustomColors.GetLength(1); j++)
            {
                for (int i = 0; i < CustomColors.GetLength(0); i++)
                {
                    CustomColorBrushes[CustomColors[i, j]] = g.CreateSolidBrush(CustomColors[i, j]);
                }
            }
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
