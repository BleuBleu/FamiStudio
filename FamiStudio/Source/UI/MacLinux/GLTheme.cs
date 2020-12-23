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
        }

#if FAMISTUDIO_MACOS
        public static float MainWindowScaling => MacUtils.MainWindowScaling;
        public static float DialogScaling     => MacUtils.DialogScaling;
#else
        public const float MainWindowScaling = 1;
        public const float DialogScaling     = 1;
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

                    var pixbuf = Gdk.Pixbuf.LoadFromResource(imgfile);
                    var lines = str.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                    fontTextureMap.TryGetValue(imgfile, out int texture);
                    var font = g.CreateFont(pixbuf, lines, def.Size, def.Alignment, def.Ellipsis, texture);

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
