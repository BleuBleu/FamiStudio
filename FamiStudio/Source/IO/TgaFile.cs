using System;
using System.Drawing;
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

        private static unsafe int[,] LoadInternal(byte[] bytes)
        {
            fixed (byte* p = &bytes[0])
            {
                var ph = (TgaHeader*)p;

                Debug.Assert(ph->datatypecode == 2 || ph->datatypecode == 10); // RGB compressed or uncompressed only.
                Debug.Assert(ph->bitsperpixel == 24 || ph->bitsperpixel == 32); // 24/32bpp only
                Debug.Assert(ph->colourmaptype == 0);

                var imageData = new int[ph->height, ph->width];
                var compressed = ph->datatypecode == 10;

                var dataIdx = Marshal.SizeOf(typeof(TgaHeader)) + ph->idlength;
                var pixelIdx = 0;
                var bpp = ph->bitsperpixel / 8;
                var flip = (ph->imagedescriptor & 0x20) == 0;

                while (pixelIdx < imageData.Length)
                {
                    if (!compressed)
                    {
                        imageData[flip ? ph->height - pixelIdx / ph->width - 1 : pixelIdx / ph->width, pixelIdx % ph->width] = PackColor(
                            bytes[dataIdx++],
                            bytes[dataIdx++],
                            bytes[dataIdx++],
                            bpp == 4 ? bytes[dataIdx++] : (byte)255);

                        pixelIdx++;
                    }
                    else
                    {
                        var code = bytes[dataIdx];

                        imageData[flip ? ph->height - pixelIdx / ph->width - 1 : pixelIdx / ph->width, pixelIdx % ph->width] = PackColor(
                            bytes[dataIdx + 1],
                            bytes[dataIdx + 2],
                            bytes[dataIdx + 3],
                            bpp == 4 ? bytes[dataIdx + 4] : (byte)255);

                        pixelIdx++;

                        var count = code & 0x7f;

                        if ((code & 0x80) != 0) // RLE
                        {
                            for (var i = 0; i < count; i++)
                            {
                                imageData[flip ? ph->height - pixelIdx / ph->width - 1 : pixelIdx / ph->width, pixelIdx % ph->width] = PackColor(
                                    bytes[dataIdx + 1],
                                    bytes[dataIdx + 2],
                                    bytes[dataIdx + 3],
                                    bpp == 4 ? bytes[dataIdx + 4] : (byte)255);
                                pixelIdx++;
                            }

                            dataIdx += bpp + 1;
                        }
                        else
                        {
                            dataIdx += bpp + 1;

                            for (var i = 0; i < count; i++)
                            {
                                imageData[flip ? ph->height - pixelIdx / ph->width - 1 : pixelIdx / ph->width, pixelIdx % ph->width] = PackColor(
                                    bytes[dataIdx++],
                                    bytes[dataIdx++],
                                    bytes[dataIdx++],
                                    bpp == 4 ? bytes[dataIdx++] : (byte)255);
                                pixelIdx++;
                            }
                        }
                    }
                }

                return imageData;
            }
        }

        public static int[,] LoadFromResource(string name)
        {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name))
            {
                byte[] bytes = new byte[stream.Length];
                stream.Read(bytes, 0, (int)stream.Length);
                stream.Close();

                return LoadInternal(bytes);
            }
        }

        public static unsafe int[,] LoadFromFile(string filename)
        {
            return LoadInternal(System.IO.File.ReadAllBytes(filename));
        }
    }
}
