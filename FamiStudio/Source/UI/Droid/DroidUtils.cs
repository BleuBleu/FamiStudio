using Android.Content;
using Android.Util;
using AndroidX.Core.Content;
using System.Reflection;
using Xamarin.Essentials;

namespace FamiStudio
{
    public static class DroidUtils
    {
        public static int GetSizeAttributeInPixel(Context context, int resId)
        {
            var att = context.ObtainStyledAttributes(new[] { resId });
            var dimension = att.GetDimensionPixelSize(0, 0);
            att.Recycle();
            return dimension;
        }

        public static Android.Graphics.Color ToAndroidColor(Color color)
        {
            return new Android.Graphics.Color(color.R, color.G, color.B, color.A);
        }

        public static int DpToPixels(int dp)
        {
            return (int)(dp * Xamarin.Essentials.Platform.AppContext.Resources.DisplayMetrics.Density);
        }

        public static Android.Graphics.Color GetColorFromResources(Context context, int resId)
        {
            // Seriously google?
            return new Android.Graphics.Color(ContextCompat.GetColor(context, resId));
        }

        public static Android.Graphics.Bitmap LoadTgaBitmapFromResource(string name, bool swap = false)
        {
            var img = TgaFile.LoadFromResource(name, swap);
            return Android.Graphics.Bitmap.CreateBitmap(img.Data, img.Width, img.Height, Android.Graphics.Bitmap.Config.Argb8888);
        }

        public static Android.Graphics.Bitmap LoadPngBitmapFromResource(string name, bool premultiplied = false)
        {
            return Android.Graphics.BitmapFactory.DecodeStream(
                Assembly.GetExecutingAssembly().GetManifestResourceStream(name), null,
                new Android.Graphics.BitmapFactory.Options() { InPremultiplied = premultiplied });
        }

    }
}