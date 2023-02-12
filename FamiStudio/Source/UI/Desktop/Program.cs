using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using static FamiStudio.Init;
using static GLFWDotNet.GLFW;

namespace FamiStudio
{
    static class Program
    {
        private const string StbDll = Platform.DllPrefix + "Stb" + Platform.DllExtension;

        [DllImport(StbDll, CallingConvention = CallingConvention.StdCall)]
        extern static int StbGetNumberOfFonts(IntPtr data);

        [DllImport(StbDll, CallingConvention = CallingConvention.StdCall)]
        extern static int StbGetFontOffsetForIndex(IntPtr data, int index);

        [DllImport(StbDll, CallingConvention = CallingConvention.StdCall)]
        extern static IntPtr StbInitFont(IntPtr data, int offset);

        [DllImport(StbDll, CallingConvention = CallingConvention.StdCall)]
        extern static float StbScaleForPixelHeight(IntPtr info, float pixels);

        [DllImport(StbDll, CallingConvention = CallingConvention.StdCall)]
        extern static float StbScaleForMappingEmToPixels(IntPtr info, float pixels);

        [DllImport(StbDll, CallingConvention = CallingConvention.StdCall)]
        extern static void StbGetCodepointBitmapBox(IntPtr info, int codepoint, float scale, ref int x0, ref int y0, ref int x1, ref int y1);

        [DllImport(StbDll, CallingConvention = CallingConvention.StdCall)]
        extern static void StbMakeCodepointBitmap(IntPtr info, IntPtr output, int width, int height, int stride, int codepoint, float scale);

        [DllImport(StbDll, CallingConvention = CallingConvention.StdCall)]
        extern static void StbMakeCodepointBitmapSubpixel(IntPtr info, IntPtr output, int width, int height, int stride, int codepoint, float scale, float subx, float suby);

        [DllImport(StbDll, CallingConvention = CallingConvention.StdCall)]
        extern static void StbGetCodepointHMetrics(IntPtr info, int codepoint, ref int advanceWidth, ref int leftSideBearing);

        [DllImport(StbDll, CallingConvention = CallingConvention.StdCall)]
        extern static int StbGetCodepointKernAdvance(IntPtr info, int ch1, int ch2);

        [DllImport(StbDll, CallingConvention = CallingConvention.StdCall)]
        extern static void StbGetFontVMetrics(IntPtr info, ref int ascent, ref int descent, ref int lineGap);

        static void DumpGlyph(byte[] glyph, int w, int h)
        {
            var lines = new List<string>();

            lines.Add("P2");
            lines.Add($"{w} {h}");
            lines.Add("255");

            for (int y = 0; y < h; y++)
            {
                var pixels = new List<string>();
                lines.Add(string.Join(' ', glyph.AsSpan(w * y, w).ToArray()));
            }

            File.WriteAllLines("C:\\Dump\\glyph.pgm", lines);
        }

        static unsafe void FontTest()
        {
            var stream = typeof(Program).Assembly.GetManifestResourceStream("FamiStudio.Resources.Fonts.QuickSand-Regular.ttf");
            var buffer = new byte[stream.Length];
            stream.Read(buffer, 0, buffer.Length);

            fixed (byte* p = &buffer[0])
            {
                var count = StbGetNumberOfFonts((IntPtr)p);
                var offset = StbGetFontOffsetForIndex((IntPtr)p, 0);
                var info = StbInitFont((IntPtr)p, offset);
                //float scale = StbScaleForPixelHeight(info, 12.0f);
                float scale = StbScaleForMappingEmToPixels(info, 8.0f);

                char c = 'M';

                int advance = 0;
                int lsb = 0;
                StbGetCodepointHMetrics(info, c, ref advance, ref lsb);
                var k = StbGetCodepointKernAdvance(info, 'l', 'j');

                var ascent = 0;
                var descent = 0;
                var lineGap = 0;
                StbGetFontVMetrics(info, ref ascent, ref descent, ref lineGap);

                int x0 = 0;
                int y0 = 0;
                int x1 = 0;
                int y1 = 0;
                StbGetCodepointBitmapBox(info, c, scale, ref x0, ref y0, ref x1, ref y1);

                int w = x1 - x0;
                int h = y1 - y0;

#if true
                var glyph = new byte[w * h * 4];
                fixed (byte* pp = &glyph[0])
                {
                    StbMakeCodepointBitmap(info, (IntPtr)pp, w * 2, h * 2, w * 2, c, scale * 2);
                }
#else
                var coords = new int[,]
                {
                    { -2, -6 },
                    {  6, -2 },
                    {  6,  2 },
                    {  2,  6 },
                };

                var glyph = new byte[w * h];
                var temp  = new byte[w * h];

                fixed (byte* pt = &glyph[0])
                {
                    for (int i = 0; i < 4; i++)
                    {
                        //var sx = coords[i, 0] / 16.0f + 0.5f;
                        //var sy = coords[i, 1] / 16.0f + 0.5f;
                        var sx = coords[i, 0] / 16.0f;
                        var sy = coords[i, 1] / 16.0f;

                        StbMakeCodepointBitmapSubpixel(info, (IntPtr)pt, w, h, w, c, scale, sx, sy);

                        for (int j = 0; j < temp.Length; j++)
                        {
                            glyph[j] += (byte)(temp[j] / 4);
                        }
                    }
                }
#endif

                DumpGlyph(glyph, w * 2, h * 2);

                info = info;
            }
        }

        //static void FontTest2()
        //{
        //    var bmp = TgaFile.LoadFromResource("FamiStudio.Resources.Fonts.QuickSand56_0.tga");
        //    var list = new List<byte>();

        //    for (var y = 0; y < bmp.Height; y++)
        //    {
        //        for (var x = 0; x < bmp.Width; x += 2)
        //        {
        //            var p0 = bmp.GetPixel(x + 0, y);
        //            var p1 = bmp.GetPixel(x + 1, y);
        //            var packed = (byte)(((p0 >> 28) & 0xf) | ((p1 >> 24) & 0xf0));
        //            list.Add(packed);
        //        }
        //    }

        //    var comp = Compression.CompressBytes(list.ToArray(), System.IO.Compression.CompressionLevel.Optimal) ;
        //}

        [STAThread]
        static void Main(string[] args)
        {
            var cli = new CommandLineInterface(args);

            if (!InitializeBaseSystems(cli.HasAnythingToDo))
            {
                Environment.Exit(-1);
            }

            //FontTest();
            //FontTest2();

            if (!cli.Run())
            {
                var fs = new FamiStudio();
                if (!fs.Run(args))
                {
                    Environment.Exit(-1);
                }
            }

            ShutdownBaseSystems();
        }
    }
}
