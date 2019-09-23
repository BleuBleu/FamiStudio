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

        private static float mainWindowScaling = 1;
        private static float dialogScaling = 1.0f;

        public static float MainWindowScaling => mainWindowScaling;
        public static float DialogScaling => dialogScaling;

        public static void Initialize()
        {
            ThemeBase.InitializeBase();

            var graphics = System.Drawing.Graphics.FromHwnd(IntPtr.Zero);

            // For the main window, we only support 1x or 2x.
            dialogScaling = graphics.DpiX / 96.0f;
            mainWindowScaling = dialogScaling >= 2.0f ? 2 : (dialogScaling >= 1.5f ? 1.5f : 1.0f);

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
                def.Size = (int)(def.Size * mainWindowScaling);

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
