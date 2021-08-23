using Android.Content;
using Android.Util;
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

        public static int DpToPixels(int dp)
        {
            return (int)(dp * Platform.AppContext.Resources.DisplayMetrics.Density);
        }
    }
}