using System;
using System.IO;
using System.Drawing;
using System.Diagnostics;
using System.Collections.Generic;

namespace FamiStudio
{
    class GLTheme : ThemeBase
    {
        public static void Initialize()
        {
            InitializeBase();

#if FAMISTUDIO_LINUX
            dialogScaling = (float)Gdk.Display.Default.DefaultScreen.Resolution / 96.0f;

            if (Settings.DpiScaling != 0)
                mainWindowScaling = Utils.Clamp(Settings.DpiScaling / 100.0f, 1, 2);
            else
                mainWindowScaling = Utils.Clamp((int)(dialogScaling * 2.0f) / 2.0f, 1.0f, 2.0f); // Round to 1/2 (so only 100%, 150% and 200%) are supported.
#elif FAMISTUDIO_WINDOWS
            var graphics = System.Drawing.Graphics.FromHwnd(IntPtr.Zero);

            // For the main window, we only support 1x or 2x.
            dialogScaling = graphics.DpiX / 96.0f;

            if (Settings.DpiScaling != 0)
                mainWindowScaling = Settings.DpiScaling / 100.0f;
            else
                mainWindowScaling = Math.Min(2.0f, (int)(dialogScaling * 2.0f) / 2.0f); // Round to 1/2 (so only 100%, 150% and 200%) are supported.
#endif
        }

#if FAMISTUDIO_MACOS
        public static float MainWindowScaling => MacUtils.MainWindowScaling;
        public static float DialogScaling     => MacUtils.DialogScaling;
#else
        private static float dialogScaling     = 1;
        private static float mainWindowScaling = 1;

        public static float MainWindowScaling => mainWindowScaling;
        public static float DialogScaling     => dialogScaling;
#endif

        private void InitializeFonts(GLGraphics g)
        {
            if (Fonts[0] == null)
            {
                var fontTextureMap = new Dictionary<string, int>();

                for (int i = 0; i < FontDefinitions.Length; i++)
                {
                    var def = FontDefinitions[i];

                    if (!def.NoScaling)
                        def.Size = (int)(def.Size * MainWindowScaling);

                    var suffix   = def.Bold ? "Bold" : "";
                    var basename = $"{def.Name}{def.Size}{suffix}";
                    var fntfile  = $"FamiStudio.Resources.{basename}.fnt";
                    var imgfile  = $"FamiStudio.Resources.{basename}_0.png";

                    var str = "";
                    using (Stream stream = typeof(GLTheme).Assembly.GetManifestResourceStream(fntfile))
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        str = reader.ReadToEnd();
                    }

#if FAMISTUDIO_WINDOWS
                    var bmp = System.Drawing.Image.FromStream(typeof(GLTheme).Assembly.GetManifestResourceStream(imgfile)) as System.Drawing.Bitmap;
#else
                    var bmp = Gdk.Pixbuf.LoadFromResource(imgfile);
#endif
                    var lines = str.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                    fontTextureMap.TryGetValue(imgfile, out int texture);
                    var font = g.CreateFont(bmp, lines, def.Size, def.Alignment, def.Ellipsis, texture);

                    fontTextureMap[imgfile] = font.Texture;
                    Fonts[i] = font;
                }
            }
        }

        public override void InitializeForGraphics(GLGraphics g)
        {
            base.InitializeForGraphics(g);
            InitializeFonts(g);
        }

        public static GLTheme CreateResourcesForGraphics(GLGraphics g)
        {
            var theme = new GLTheme();
            theme.InitializeForGraphics(g);
            return theme;
        }
    }
}
