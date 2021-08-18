using System;
using System.Drawing;
using System.Collections.Generic;

using RenderFont     = FamiStudio.GLFont;
using RenderBrush    = FamiStudio.GLBrush;
using RenderGraphics = FamiStudio.GLGraphics;

namespace FamiStudio
{
    public class ThemeRenderResources : IDisposable
    {
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

        public RenderBrush WhiteBrush           => whiteBrush;
        public RenderBrush BlackBrush           => blackBrush;
        public RenderBrush LightGreyFillBrush1  => lightGreyFillBrush1; 
        public RenderBrush LightGreyFillBrush2  => lightGreyFillBrush2; 
        public RenderBrush MediumGreyFillBrush1 => mediumGreyFillBrush1;
        public RenderBrush DarkGreyLineBrush1   => darkGreyLineBrush1;  
        public RenderBrush DarkGreyLineBrush2   => darkGreyLineBrush2;  
        public RenderBrush DarkGreyLineBrush3   => darkGreyLineBrush3;  
        public RenderBrush DarkGreyFillBrush1   => darkGreyFillBrush1;  
        public RenderBrush DarkGreyFillBrush2   => darkGreyFillBrush2;  
        public RenderBrush LightRedFillBrush    => lightRedFillBrush;   
        public RenderBrush DarkRedFillBrush     => darkRedFillBrush;

        private Dictionary<Color, RenderBrush> customColorBrushes = new Dictionary<Color, RenderBrush>();
        public  Dictionary<Color, RenderBrush> CustomColorBrushes => customColorBrushes;

        protected enum RenderFontStyle
        {
            VerySmall,
            Small,
            SmallBold,
            Medium,
            MediumBold,
            MediumBig,
            Big,
            BigBold,
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
            new FontDefinition() { Name = "QuickSand", Size =  8 },
            new FontDefinition() { Name = "QuickSand", Size = 10 },
            new FontDefinition() { Name = "QuickSand", Size = 10, Bold = true },
            new FontDefinition() { Name = "QuickSand", Size = 12 },
            new FontDefinition() { Name = "QuickSand", Size = 12, Bold = true },
            new FontDefinition() { Name = "QuickSand", Size = 16 },
            new FontDefinition() { Name = "QuickSand", Size = 20 },
            new FontDefinition() { Name = "QuickSand", Size = 20, Bold = true },
            new FontDefinition() { Name = "QuickSand", Size = 28 } 
        };

        protected RenderFont[] fonts = new RenderFont[(int)RenderFontStyle.Max];

        public RenderFont FontVerySmall  => fonts[0];
        public RenderFont FontSmall      => fonts[1];
        public RenderFont FontSmallBold  => fonts[2];
        public RenderFont FontMedium     => fonts[3];
        public RenderFont FontMediumBold => fonts[4];
        public RenderFont FontMediumBig  => fonts[5];
        public RenderFont FontBig        => fonts[6];
        public RenderFont FontBigBold    => fonts[7];
        public RenderFont FontHuge       => fonts[8];

        public ThemeRenderResources(RenderGraphics g)
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
                    FontDefinitions[i].Size);
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
            Utils.DisposeAndNullify(ref lightRedFillBrush);
            Utils.DisposeAndNullify(ref darkRedFillBrush);

            foreach (var kv in customColorBrushes)
                kv.Value.Dispose();
            foreach (var font in fonts)
                font.Dispose();

            customColorBrushes.Clear();
        }

        /*
        private GLFont GetOrCreateFont(int idx)
        {
            if (fonts[idx] == null)
            {
                fonts[idx] = g.CreateFontFromResource(
                       FontDefinitions[idx].Name,
                       FontDefinitions[idx].Bold,
                       FontDefinitions[idx].Size);
            }

            return fonts[idx];
        }
        */
    }
}
