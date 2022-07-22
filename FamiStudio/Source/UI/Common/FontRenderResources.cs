using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace FamiStudio
{
    public class FontRenderResources : IDisposable
    {
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

        public FontRenderResources(Graphics g)
        {
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
            foreach (var font in fonts)
                font.Dispose();
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
