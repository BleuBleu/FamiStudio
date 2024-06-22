using System;

namespace FamiStudio
{
    public class Theme
    {
        public static Color DarkGreyColor1    = Color.FromArgb( 25,  28,  31);
        public static Color DarkGreyColor2    = Color.FromArgb( 33,  37,  41);
        public static Color DarkGreyColor3    = Color.FromArgb( 38,  42,  46);
        public static Color DarkGreyColor4    = Color.FromArgb( 42,  48,  51);
        public static Color DarkGreyColor5    = Color.FromArgb( 49,  55,  61);
        public static Color DarkGreyColor6    = Color.FromArgb( 56,  62,  71);
        public static Color MediumGreyColor1  = Color.FromArgb( 86,  91, 105);
        public static Color LightGreyColor1   = Color.FromArgb(178, 185, 198);
        public static Color LightGreyColor2   = Color.FromArgb(198, 205, 218);
        public static Color YellowColor       = Color.FromArgb(225, 170,   0);
        public static Color LightRedColor     = Color.FromArgb(225, 150, 150);
        public static Color DarkRedColor      = Color.FromArgb(210,  16,  48);

        public static Color BlackColor = Color.FromArgb(  0,   0,   0);
        public static Color GreenColor = Color.FromArgb(  0,   0, 255);
        public static Color WhiteColor = Color.FromArgb(255, 255, 255);

        private static int  nextColorIdx = 39;
        private static bool colorsInitialized = false;

        //
        // These are some of the shades (300 to 800) for most of the Google Material Design colors.
        // Brown and gray were removed.
        //
        // https://material.io/design/color/the-color-system.html
        //

        public static readonly Color[,] CustomColors = new Color[17, 6]
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
            {
                Color.FromArgb(unchecked((int)0xffa1887f)),
                Color.FromArgb(unchecked((int)0xff8d6e63)),
                Color.FromArgb(unchecked((int)0xff795548)),
                Color.FromArgb(unchecked((int)0xff6d4c41)),
                Color.FromArgb(unchecked((int)0xff5d4037)),
                Color.FromArgb(unchecked((int)0xff4e342e)),
            }

        };

        public static void Initialize()
        {
            if (!colorsInitialized)
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

                colorsInitialized = true;
            }
        }

        public static float ColorDistance(Color c0, Color c1)
        {
            float dr = MathF.Abs(c0.R - c1.R);
            float dg = MathF.Abs(c0.G - c1.G);
            float db = MathF.Abs(c0.B - c1.B);

            return (float)MathF.Sqrt(dr * dr + dg * dg + db * db);
        }

        public static Color EnforceThemeColor(Color color)
        {
            float minDist = 1000.0f;
            Color closestColor = CustomColors[0, 0];

            for (int j = 0; j < CustomColors.GetLength(1); j++)
            {
                for (int i = 0; i < CustomColors.GetLength(0); i++)
                {
                    if (CustomColors[i, j] == color)
                        return color;

                    var dist = ColorDistance(color, CustomColors[i, j]);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        closestColor = CustomColors[i, j];
                    }
                }
            }

            return closestColor;
        }

        public static Color RandomCustomColor()
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

        public static Color Darken(Color color, int amount = 50)
        {
            return Color.FromArgb(
                    Math.Max(0, color.R - amount),
                    Math.Max(0, color.G - amount),
                    Math.Max(0, color.B - amount)
                );
        }

        public static Color Lighten(Color color, int amount = 50)
        {
            return Color.FromArgb(
                    Math.Min(255, color.R + amount),
                    Math.Min(255, color.G + amount),
                    Math.Min(255, color.B + amount)
                );
        }

        public static void Serialize(ProjectBuffer buffer)
        {
            buffer.Serialize(ref nextColorIdx);
        }
    }
}
