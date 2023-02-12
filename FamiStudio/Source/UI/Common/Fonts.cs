using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace FamiStudio
{
    public class Fonts : IDisposable
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
            new FontDefinition() { Name = "Quicksand-Regular", Size =  9              }, // VerySmall
            new FontDefinition() { Name = "Quicksand-Bold",    Size =  9, Bold = true }, // VerySmall (Bold)
            new FontDefinition() { Name = "Quicksand-Regular", Size = 11              }, // Small
            new FontDefinition() { Name = "Quicksand-Bold",    Size = 11, Bold = true }, // Small (Bold)
            new FontDefinition() { Name = "Quicksand-Regular", Size = 13              }, // Medium
            new FontDefinition() { Name = "Quicksand-Bold",    Size = 13, Bold = true }, // Medium (Bold)
            new FontDefinition() { Name = "Quicksand-Regular", Size = 17              }, // Large
            new FontDefinition() { Name = "Quicksand-Regular", Size = 21              }, // VeryLarge
            new FontDefinition() { Name = "Quicksand-Bold",    Size = 21, Bold = true }, // VeryLarge (Bold)
            new FontDefinition() { Name = "Quicksand-Regular", Size = 29              }  // Huge
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

        public Fonts(Graphics g)
        {
            for (int i = 0; i < FontDefinitions.Length; i++)
            {
                fonts[i] = g.CreateFontFromResource(FontDefinitions[i].Name, (int)DpiScaling.ScaleForFontFloat(FontDefinitions[i].Size));
            }
        }

        public void ClearGlyphCache(Graphics g)
        {
            foreach (var font in fonts)
            {
                font.ClearCachedData();
            }

            g.ClearGlyphCache();
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
