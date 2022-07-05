using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Reflection;

namespace FamiStudio
{
    public static class TgaFile
    {
        // From http://www.paulbourke.net/dataformats/tga/
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct TgaHeader
        {
            public byte idlength;
            public byte colourmaptype;
            public byte datatypecode;
            public short colourmaporigin;
            public short colourmaplength;
            public byte colourmapdepth;
            public short x_origin;
            public short y_origin;
            public short width;
            public short height;
            public byte bitsperpixel;
            public byte imagedescriptor;
        }

        public static unsafe void GetResourceImageSize(string name, out int width, out int height)
        {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name))
            {
                byte[] headerBytes = new byte[Marshal.SizeOf(typeof(TgaHeader))];
                stream.Read(headerBytes, 0, (int)headerBytes.Length);
                stream.Close();

                fixed (byte* p = &headerBytes[0])
                {
                    var ph = (TgaHeader*)p;

                    width = ph->width;
                    height = ph->height;
                }
            }
        }

        private static int PackColor(byte r, byte g, byte b, byte a)
        {
            return a << 24 | b << 16 | g << 8 | r;
        }

        private static unsafe SimpleBitmap LoadInternal(byte[] bytes, bool swap = false)
        {
            fixed (byte* p = &bytes[0])
            {
                var ph = (TgaHeader*)p;

                Debug.Assert(ph->datatypecode == 2  || ph->datatypecode == 10); // RGB compressed or uncompressed only.
                Debug.Assert(ph->bitsperpixel == 24 || ph->bitsperpixel == 32); // 24/32bpp only
                Debug.Assert(ph->colourmaptype == 0);

                var compressed = ph->datatypecode == 10;
                var bpp = ph->bitsperpixel / 8;
                var flip = (ph->imagedescriptor & 0x20) == 0;

                var bmp = new SimpleBitmap(ph->width, ph->height);
                var i = Marshal.SizeOf(typeof(TgaHeader)) + ph->idlength;

                var ri = swap ? 2 : 0;
                var gi = 1;
                var bi = swap ? 0 : 2;
                var ai = 3;

                var x = 0;
                var y = flip ? ph->height - 1 : 0;
                var yinc = flip ? -1 : 1;

                while (y >= 0 && y < ph->height)
                {
                    if (!compressed)
                    {
                        bmp.SetPixel(x, y, PackColor(
                            bytes[i + ri],
                            bytes[i + gi],
                            bytes[i + bi],
                            bpp == 4 ? bytes[i + ai] : (byte)255));

                        i += bpp;
                        if (++x == bmp.Width) { x = 0; y += yinc; }
                    }
                    else
                    {
                        var code = bytes[i];
                        i++;

                        bmp.SetPixel(x, y, PackColor(
                            bytes[i + ri],
                            bytes[i + gi],
                            bytes[i + bi],
                            bpp == 4 ? bytes[i + ai] : (byte)255));
                        if (++x == bmp.Width) { x = 0; y += yinc; }

                        var count = code & 0x7f;

                        if ((code & 0x80) != 0) // RLE
                        {
                            for (var j = 0; j < count; j++)
                            {
                                bmp.SetPixel(x, y, PackColor(
                                    bytes[i + ri],
                                    bytes[i + gi],
                                    bytes[i + bi],
                                    bpp == 4 ? bytes[i + ai] : (byte)255));
                                if (++x == bmp.Width) { x = 0; y += yinc; }
                            }

                            i += bpp;
                        }
                        else
                        {
                            i += bpp;

                            for (var j = 0; j < count; j++)
                            {
                                bmp.SetPixel(x, y, PackColor(
                                    bytes[i + ri],
                                    bytes[i + gi],
                                    bytes[i + bi],
                                    bpp == 4 ? bytes[i + ai] : (byte)255));
                                i += bpp;
                                if (++x == bmp.Width) { x = 0; y += yinc; }
                            }
                        }
                    }
                }

                return bmp;
            }
        }

        public static SimpleBitmap LoadFromResource(string name, bool swap = false)
        {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name))
            {
                byte[] bytes = new byte[stream.Length];
                stream.Read(bytes, 0, (int)stream.Length);
                stream.Close();

                return LoadInternal(bytes, swap);
            }
        }

        public static unsafe SimpleBitmap LoadFromFile(string filename, bool swap = false)
        {
            return LoadInternal(System.IO.File.ReadAllBytes(filename), swap);
        }
    }
}
