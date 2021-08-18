using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace FamiStudio
{
    public static class MobileUtils
    {
        public static int ComputeIdealButtonSize(int w, int h)
        {
            var maxAxis = Math.Max(w, h);
            var minAxis = Math.Min(w, h);
            return Math.Min(maxAxis / 16, minAxis / 8);
        }

    }
}
