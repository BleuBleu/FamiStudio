using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace FamiStudio
{
    public class ThemeRenderResources : IDisposable
    {
        private Brush whiteBrush;
        private Brush blackBrush;
        private Brush lightGreyFillBrush1;
        private Brush lightGreyFillBrush2;
        private Brush mediumGreyFillBrush1;
        private Brush darkGreyLineBrush1;
        private Brush darkGreyLineBrush2;
        private Brush darkGreyLineBrush3;
        private Brush darkGreyFillBrush1;
        private Brush darkGreyFillBrush2;
        private Brush darkGreyFillBrush3;
        private Brush lightRedFillBrush;
        private Brush darkRedFillBrush;

        public Brush WhiteBrush           => whiteBrush;
        public Brush BlackBrush           => blackBrush;
        public Brush LightGreyFillBrush1  => lightGreyFillBrush1; 
        public Brush LightGreyFillBrush2  => lightGreyFillBrush2; 
        public Brush MediumGreyFillBrush1 => mediumGreyFillBrush1;
        public Brush DarkGreyLineBrush1   => darkGreyLineBrush1;  
        public Brush DarkGreyLineBrush2   => darkGreyLineBrush2;  
        public Brush DarkGreyLineBrush3   => darkGreyLineBrush3;  
        public Brush DarkGreyFillBrush1   => darkGreyFillBrush1;  
        public Brush DarkGreyFillBrush2   => darkGreyFillBrush2;  
        public Brush DarkGreyFillBrush3   => darkGreyFillBrush3;  
        public Brush LightRedFillBrush    => lightRedFillBrush;   
        public Brush DarkRedFillBrush     => darkRedFillBrush;

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
            whiteBrush           = g.CreateSolidBrush(Theme.WhiteColor);
            blackBrush           = g.CreateSolidBrush(Theme.BlackColor);
            lightGreyFillBrush1  = g.CreateSolidBrush(Theme.LightGreyFillColor1);
            lightGreyFillBrush2  = g.CreateSolidBrush(Theme.LightGreyFillColor2);
            mediumGreyFillBrush1 = g.CreateSolidBrush(Theme.MediumGreyFillColor1);
            darkGreyLineBrush1   = g.CreateSolidBrush(Theme.DarkGreyLineColor1);
            darkGreyLineBrush2   = g.CreateSolidBrush(Theme.DarkGreyLineColor2);
            darkGreyLineBrush3   = g.CreateSolidBrush(Theme.DarkGreyLineColor3);
            darkGreyFillBrush1   = g.CreateSolidBrush(Theme.DarkGreyFillColor1);
            darkGreyFillBrush2   = g.CreateSolidBrush(Theme.DarkGreyFillColor2);
            darkGreyFillBrush3   = g.CreateSolidBrush(Theme.DarkGreyFillColor3);
            lightRedFillBrush    = g.CreateSolidBrush(Theme.LightRedFillColor);
            darkRedFillBrush     = g.CreateSolidBrush(Theme.DarkRedFillColor);

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
            Utils.DisposeAndNullify(ref lightGreyFillBrush1);
            Utils.DisposeAndNullify(ref lightGreyFillBrush2);
            Utils.DisposeAndNullify(ref mediumGreyFillBrush1);
            Utils.DisposeAndNullify(ref darkGreyLineBrush1);
            Utils.DisposeAndNullify(ref darkGreyLineBrush2);
            Utils.DisposeAndNullify(ref darkGreyLineBrush3);
            Utils.DisposeAndNullify(ref darkGreyFillBrush1);
            Utils.DisposeAndNullify(ref darkGreyFillBrush2);
            Utils.DisposeAndNullify(ref darkGreyFillBrush3);
            Utils.DisposeAndNullify(ref lightRedFillBrush);
            Utils.DisposeAndNullify(ref darkRedFillBrush);

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
