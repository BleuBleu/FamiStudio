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
        public static IntPtr Default    = MacUtils.SelRegisterName("arrowCursor");
        public static IntPtr SizeWE     = MacUtils.SelRegisterName("resizeLeftRightCursor");
        public static IntPtr SizeNS     = MacUtils.SelRegisterName("resizeUpDownCursor");
        public static IntPtr DragCursor = MacUtils.SelRegisterName("closedHandCursor");
        public static IntPtr CopyCursor = MacUtils.SelRegisterName("dragCopyCursor");

        //private static OpenTK.MouseCursor BitmapToMouseCursor(string file, int hotx = 0, int hoty = 0)
        //{
        //    var bmp = Image.FromStream(typeof(Cursors).Assembly.GetManifestResourceStream("FamiStudio.Resources." + file)) as Bitmap;

        //    System.Drawing.Imaging.BitmapData bmpData =
        //        bmp.LockBits(
        //            new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height),
        //            System.Drawing.Imaging.ImageLockMode.ReadWrite,
        //            System.Drawing.Imaging.PixelFormat.Format32bppArgb);

        //    var cursor = new OpenTK.MouseCursor(hotx, hoty, bmp.Width, bmp.Height, bmpData.Scan0);

        //    bmp.UnlockBits(bmpData);

        //    return cursor;
        //}

        public static void Initialize()
        {
            //SizeWE = BitmapToMouseCursor("MacSizeWE32px.png", 16, 16);
            //SizeNS = BitmapToMouseCursor("MacSizeNS32px.png", 16, 16);
            //DragCursor = BitmapToMouseCursor("MacDrag32px.png", 7, 0);
            //CopyCursor = BitmapToMouseCursor("MacCopy32px.png", 7, 0);
        }
    }
}
