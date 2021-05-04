using System;
using System.Diagnostics;

namespace FamiStudio
{
    public static class GtkUtils
    {
        public static int ScaleGtkWidget(int x)
        {
#if FAMISTUDIO_LINUX
            return (int)(x * GLTheme.DialogScaling);
#else
            return x;
#endif
        }

        public static System.Windows.Forms.MouseEventArgs ToWinFormArgs(Gdk.EventButton e, int x, int y)
        {
            System.Windows.Forms.MouseButtons buttons = System.Windows.Forms.MouseButtons.None;

            if (e.Button == 1)
                buttons = System.Windows.Forms.MouseButtons.Left;
            else if (e.Button == 2)
                buttons = System.Windows.Forms.MouseButtons.Middle;
            else if (e.Button == 3)
                buttons = System.Windows.Forms.MouseButtons.Right;

            return new System.Windows.Forms.MouseEventArgs(buttons, 1, x, y, 0);
        }

        public static System.Windows.Forms.MouseEventArgs ToWinFormArgs(Gdk.EventMotion e, int x, int y)
        {
            System.Windows.Forms.MouseButtons buttons = System.Windows.Forms.MouseButtons.None;

            if ((e.State & Gdk.ModifierType.Button1Mask) != 0)
                buttons |= System.Windows.Forms.MouseButtons.Left;
            else if ((e.State & Gdk.ModifierType.Button2Mask) != 0)
                buttons |= System.Windows.Forms.MouseButtons.Middle;
            else if ((e.State & Gdk.ModifierType.Button3Mask) != 0)
                buttons |= System.Windows.Forms.MouseButtons.Right;

            return new System.Windows.Forms.MouseEventArgs(buttons, 1, x, y, 0);
        }

        public static System.Windows.Forms.Keys ToWinFormKey(Gdk.Key k)
        {
            if (k >= Gdk.Key.A && k <= Gdk.Key.Z)
                return System.Windows.Forms.Keys.A + (k - Gdk.Key.A);
            else if (k >= Gdk.Key.a && k <= Gdk.Key.z)
                return System.Windows.Forms.Keys.A + (k - Gdk.Key.a);
            else if (k >= Gdk.Key.Key_0 && k <= Gdk.Key.Key_9)
                return System.Windows.Forms.Keys.D0 + (k - Gdk.Key.Key_0);
            else if (k >= Gdk.Key.F1 && k <= Gdk.Key.F12)
                return System.Windows.Forms.Keys.F1 + (k - Gdk.Key.F1);
            else if (k == Gdk.Key.Control_R || k == Gdk.Key.Control_L)
                return System.Windows.Forms.Keys.Control;
            else if (k == Gdk.Key.Alt_R || k == Gdk.Key.Alt_L)
                return System.Windows.Forms.Keys.Alt;
            else if (k == Gdk.Key.Shift_R || k == Gdk.Key.Shift_L)
                return System.Windows.Forms.Keys.Shift;
            else if (k == Gdk.Key.Up)
                return System.Windows.Forms.Keys.Up;
            else if (k == Gdk.Key.Down)
                return System.Windows.Forms.Keys.Down;
            else if (k == Gdk.Key.Left)
                return System.Windows.Forms.Keys.Left;
            else if (k == Gdk.Key.Right)
                return System.Windows.Forms.Keys.Right;
            else if (k == Gdk.Key.space)
                return System.Windows.Forms.Keys.Space;
            else if (k == Gdk.Key.Return)
                return System.Windows.Forms.Keys.Enter;
            else if (k == Gdk.Key.Home)
                return System.Windows.Forms.Keys.Home;
            else if (k == Gdk.Key.Delete)
                return System.Windows.Forms.Keys.Delete;
            else if (k == Gdk.Key.Escape)
                return System.Windows.Forms.Keys.Escape;
            else if (k == Gdk.Key.quoteleft)
                return System.Windows.Forms.Keys.Oem3;
            else if (k == Gdk.Key.Tab)
                return System.Windows.Forms.Keys.Tab;
            else if (k == Gdk.Key.minus)
                return System.Windows.Forms.Keys.OemMinus;
            else if (k == Gdk.Key.equal)
                return System.Windows.Forms.Keys.Oemplus;
            else if (k == Gdk.Key.bracketleft)
                return System.Windows.Forms.Keys.Oem4;
            else if (k == Gdk.Key.bracketright)
                return System.Windows.Forms.Keys.Oem6;
            else if (k == Gdk.Key.comma)
                return System.Windows.Forms.Keys.Oemcomma;
            else if (k == Gdk.Key.period)
                return System.Windows.Forms.Keys.OemPeriod;
            else if (k == Gdk.Key.semicolon)
                return System.Windows.Forms.Keys.Oem1;
            else if (k == Gdk.Key.slash)
                return System.Windows.Forms.Keys.Oem2;
            else if (k == Gdk.Key.BackSpace)
                return System.Windows.Forms.Keys.Back;
            else if (k == Gdk.Key.Prior)
                return System.Windows.Forms.Keys.PageUp;
            else if (k == Gdk.Key.Next)
                return System.Windows.Forms.Keys.PageDown;
            else if (k == Gdk.Key.apostrophe)
                return System.Windows.Forms.Keys.OemQuotes;
            else if (k == Gdk.Key.backslash)
                return System.Windows.Forms.Keys.OemBackslash;

            Trace.WriteLine($"Unknown key pressed {k}");

            return System.Windows.Forms.Keys.None;
        }

        public static System.Windows.Forms.Keys ToWinFormKey(Gdk.ModifierType m)
        {
            var mod = System.Windows.Forms.Keys.None;

            if ((m & Gdk.ModifierType.ControlMask) != 0)
                mod |= System.Windows.Forms.Keys.Control;
            if ((m & Gdk.ModifierType.MetaMask) != 0)
                mod |= System.Windows.Forms.Keys.Control;
            if ((m & Gdk.ModifierType.ShiftMask) != 0)
                mod |= System.Windows.Forms.Keys.Shift;
            if ((m & Gdk.ModifierType.Mod1Mask) != 0)
                mod |= System.Windows.Forms.Keys.Alt;

            return mod;
        }
    }
}
