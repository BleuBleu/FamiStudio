using Android.Content;
using Android.Util;
using AndroidX.Core.Content;
using Xamarin.Essentials;

namespace FamiStudio
{
    public static class DroidUtils
    {
        public static string GetAttributeValue(Android.Content.Res.Resources.Theme theme, int resId)
        {
            TypedValue typedValue = new TypedValue();
            theme.ResolveAttribute(resId, typedValue, true);
            return typedValue.CoerceToString();
        }

        public static int GetSizeAttributeInPixel(Context context, int resId)
        {
            var att = context.ObtainStyledAttributes(new[] { resId });
            var dimension = att.GetDimensionPixelSize(0, 0);
            att.Recycle();
            return dimension;
        }

        public static Android.Graphics.Color ToAndroidColor(System.Drawing.Color color)
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
    }
}