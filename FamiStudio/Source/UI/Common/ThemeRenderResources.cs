using System;
using System.Drawing;
using System.Diagnostics;
using System.Collections.Generic;

namespace FamiStudio
{
    public class ThemeRenderResources : IDisposable
    {
        private Brush whiteBrush;
        private Brush blackBrush;
        private Brush lightGreyBrush1;
        private Brush lightGreyBrush2;
        private Brush mediumGreyBrush1;
        private Brush darkGreyBrush1;
        private Brush darkGreyBrush2;
        private Brush darkGreyBrush3;
        private Brush darkGreyBrush4;
        private Brush darkGreyBrush5;
        private Brush darkGreyBrush6;
        private Brush lightRedBrush;
        private Brush darkRedBrush;

        public Brush WhiteBrush       => whiteBrush;
        public Brush BlackBrush       => blackBrush;
        public Brush LightGreyBrush1  => lightGreyBrush1; 
        public Brush LightGreyBrush2  => lightGreyBrush2; 
        public Brush MediumGreyBrush1 => mediumGreyBrush1;
        public Brush DarkGreyBrush1   => darkGreyBrush1;  
        public Brush DarkGreyBrush2   => darkGreyBrush2;  
        public Brush DarkGreyBrush3   => darkGreyBrush3;  
        public Brush DarkGreyBrush4   => darkGreyBrush4;  
        public Brush DarkGreyBrush5   => darkGreyBrush5;  
        public Brush DarkGreyBrush6   => darkGreyBrush6;  
        public Brush LightRedBrush    => lightRedBrush;   
        public Brush DarkRedBrush     => darkRedBrush;

        private Dictionary<Color, Brush> customColorBrushes = new Dictionary<Color, Brush>();
        public  Dictionary<Color, Brush> CustomColorBrushes => customColorBrushes;

        protected enum RenderFontStyle
        {
            VerySmall,
            VerySmallBold,
            Small,
            SmallBold,
            Medium,
            MediumBold,
            Large,
            VeryLarge,
            VeryLargeBold,
            Huge,
            Max
        };

        protected struct FontDefinition
        {
            public string Name;
            public bool Bold;
            public int Size;
        };

        // These are the bitmap fonts we have available in the resources, we also have 2x and 4x sizes.
        protected static readonly FontDefinition[] FontDefinitions = new FontDefinition[]
        {
            new FontDefinition() { Name = "QuickSand", Size =  8 }, // VerySmall
            new FontDefinition() { Name = "QuickSand", Size =  8, Bold = true },
            new FontDefinition() { Name = "QuickSand", Size = 10 }, // Small
            new FontDefinition() { Name = "QuickSand", Size = 10, Bold = true },
            new FontDefinition() { Name = "QuickSand", Size = 12 }, // Medium
            new FontDefinition() { Name = "QuickSand", Size = 12, Bold = true },
            new FontDefinition() { Name = "QuickSand", Size = 16 }, // Large
            new FontDefinition() { Name = "QuickSand", Size = 20 }, // VeryLarge
            new FontDefinition() { Name = "QuickSand", Size = 20, Bold = true },
            new FontDefinition() { Name = "QuickSand", Size = 28 }  // Huge
        };

        protected Font[] fonts = new Font[(int)RenderFontStyle.Max];

        public Font FontVerySmall     => fonts[0];
        public Font FontVerySmallBold => fonts[1];
        public Font FontSmall         => fonts[2];
        public Font FontSmallBold     => fonts[3];
        public Font FontMedium        => fonts[4];
        public Font FontMediumBold    => fonts[5];
        public Font FontLarge         => fonts[6];
        public Font FontVeryLarge     => fonts[7];
        public Font FontVeryLargeBold => fonts[8];
        public Font FontHuge          => fonts[9];

        public ThemeRenderResources(Graphics g)
        {
            whiteBrush       = g.CreateSolidBrush(Theme.WhiteColor);
            blackBrush       = g.CreateSolidBrush(Theme.BlackColor);
            lightGreyBrush1  = g.CreateSolidBrush(Theme.LightGreyColor1);
            lightGreyBrush2  = g.CreateSolidBrush(Theme.LightGreyColor2);
            mediumGreyBrush1 = g.CreateSolidBrush(Theme.MediumGreyColor1);
            darkGreyBrush1   = g.CreateSolidBrush(Theme.DarkGreyColor1);
            darkGreyBrush2   = g.CreateSolidBrush(Theme.DarkGreyColor2);
            darkGreyBrush3   = g.CreateSolidBrush(Theme.DarkGreyColor3);
            darkGreyBrush4   = g.CreateSolidBrush(Theme.DarkGreyColor4);
            darkGreyBrush5   = g.CreateSolidBrush(Theme.DarkGreyColor5);
            darkGreyBrush6   = g.CreateSolidBrush(Theme.DarkGreyColor6);
            lightRedBrush    = g.CreateSolidBrush(Theme.LightRedColor);
            darkRedBrush     = g.CreateSolidBrush(Theme.DarkRedColor);

            for (int j = 0; j < Theme.CustomColors.GetLength(1); j++)
            {
                for (int i = 0; i < Theme.CustomColors.GetLength(0); i++)
                {
                    customColorBrushes[Theme.CustomColors[i, j]] = g.CreateSolidBrush(Theme.CustomColors[i, j]);
                }
            }

            for (int i = 0; i < FontDefinitions.Length; i++)
            {
                fonts[i] = g.CreateFontFromResource(
                    FontDefinitions[i].Name,
                    FontDefinitions[i].Bold,
                    (int)(FontDefinitions[i].Size * g.FontScaling));
            }
        }

        public void Dispose()
        {
            Utils.DisposeAndNullify(ref whiteBrush);
            Utils.DisposeAndNullify(ref blackBrush);
            Utils.DisposeAndNullify(ref lightGreyBrush1);
            Utils.DisposeAndNullify(ref lightGreyBrush2);
            Utils.DisposeAndNullify(ref mediumGreyBrush1);
            Utils.DisposeAndNullify(ref darkGreyBrush1);
            Utils.DisposeAndNullify(ref darkGreyBrush2);
            Utils.DisposeAndNullify(ref darkGreyBrush3);
            Utils.DisposeAndNullify(ref darkGreyBrush4);
            Utils.DisposeAndNullify(ref darkGreyBrush5);
            Utils.DisposeAndNullify(ref darkGreyBrush6);
            Utils.DisposeAndNullify(ref lightRedBrush);
            Utils.DisposeAndNullify(ref darkRedBrush);

            foreach (var kv in customColorBrushes)
                kv.Value.Dispose();
            foreach (var font in fonts)
                font.Dispose();

            customColorBrushes.Clear();
        }

        public Font GetBestMatchingFontByWidth(string text, int desiredWidth, bool bold)
        {
            // Get largest font that will fit.
            for (int i = 0; i < FontDefinitions.Length; i++)
            {
                var def = FontDefinitions[i];

                if (def.Bold == bold)
                {
                    var width = fonts[i].MeasureString(text, false);
                    if (width > desiredWidth)
                        return fonts[Math.Max(0, i - 1)];
                }
            }

            // Found nothing, return the largest font we have.
            for (int i = FontDefinitions.Length - 1; i >= 0 ; i--)
            {
                var def = FontDefinitions[i];
                if (def.Bold == bold)
                    return fonts[i];
            }

            // We never get here.
            Debug.Assert(false);
            return fonts[0];
        }

        public Font GetBestMatchingFontByHeight(Graphics g, int desiredHeight, bool bold)
        {
            var lastIdx = 0;
            var foundIdx = 0;

            for (int i = 0; i < FontDefinitions.Length; i++)
            {
                var def = FontDefinitions[i];

                if (def.Bold == bold)
                {
                    if (def.Size * DpiScaling.Font > desiredHeight)
                    {
                        foundIdx = lastIdx;
                        break;
                    }

                    lastIdx = i;
                }
            }

            return fonts[Math.Max(0, foundIdx)];
        }
    }
}
