using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using SharpDX.DirectWrite;

namespace FamiStudio
{
    class Direct2DTheme : ThemeBase
    {
        private static Factory directWriteFactory;
        private static Direct2DResourceFontLoader resourceFontLoader;
        private static FontCollection fontCollection;
        
        public static void Initialize()
        {
            ThemeBase.InitializeBase();
            InitializeFonts();
        }

        public static void InitializeFonts()
        {
            directWriteFactory = new Factory();
            resourceFontLoader = new Direct2DResourceFontLoader(directWriteFactory);
            fontCollection = new FontCollection(directWriteFactory, resourceFontLoader, resourceFontLoader.Key);

            for (int i = 0; i < FontDefinitions.Length; i++)
            {
                var def = FontDefinitions[i];

                var format = new TextFormat(directWriteFactory, def.Name, fontCollection, def.Bold ? FontWeight.Bold : FontWeight.Regular, FontStyle.Normal, FontStretch.Normal, def.Size);

                format.WordWrapping = WordWrapping.NoWrap;
                format.TextAlignment = def.Alignment == 1 ? TextAlignment.Center : (def.Alignment == 2 ? TextAlignment.Trailing : TextAlignment.Leading);

                if (def.Ellipsis)
                {
                    var trimmingSign = new EllipsisTrimming(directWriteFactory, format);
                    format.SetTrimming(new Trimming() { Delimiter = (int)')', Granularity = TrimmingGranularity.Character, DelimiterCount = 1 }, trimmingSign);
                }

                Fonts[i] = format;
            }
        }

        public static Direct2DTheme CreateResourcesForGraphics(Direct2DGraphics g)
        {
            var theme = new Direct2DTheme();
            theme.InitializeForGraphics(g);
            return theme;
        }
    }
}
