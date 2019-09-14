using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FamiStudio
{
    class Cursors
    {
        public static OpenTK.MouseCursor Default    = OpenTK.MouseCursor.Default;
        public static OpenTK.MouseCursor SizeWE     = OpenTK.MouseCursor.Default;
        public static OpenTK.MouseCursor SizeNS     = OpenTK.MouseCursor.Default;
        public static OpenTK.MouseCursor DragCursor = OpenTK.MouseCursor.Default;
        public static OpenTK.MouseCursor CopyCursor = OpenTK.MouseCursor.Default;

        private static OpenTK.MouseCursor BitmapToMouseCursor(string file, int hotx = 0, int hoty = 0)
        {
            var bmp = Image.FromStream(typeof(Cursors).Assembly.GetManifestResourceStream("FamiStudio.Resources." + file)) as Bitmap;

            System.Drawing.Imaging.BitmapData bmpData =
                bmp.LockBits(
                    new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height),
                    System.Drawing.Imaging.ImageLockMode.ReadWrite,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            var cursor = new OpenTK.MouseCursor(hotx, hoty, bmp.Width, bmp.Height, bmpData.Scan0);

            bmp.UnlockBits(bmpData);

            return cursor;
        }

        public static void Initialize()
        {
            SizeWE = BitmapToMouseCursor("MacSizeWE32px.png", 16, 16);
            SizeNS = BitmapToMouseCursor("MacSizeNS32px.png", 16, 16);
            DragCursor = BitmapToMouseCursor("MacDrag32px.png", 7, 0);
            CopyCursor = BitmapToMouseCursor("MacCopy32px.png", 7, 0);
        }
    }
}
