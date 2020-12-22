using System;
using System.Diagnostics;
using OpenTK.Input;

namespace FamiStudio
{
    public static class OpenTkUtils
    {
        public static System.Windows.Forms.MouseEventArgs ToWinFormArgs(MouseMoveEventArgs e, int x, int y)
        {
#if FAMISTUDIO_MACOS
            // The OpenTK mouse state isnt reliable and often has buttons getting "stuck"
            // especially after things like resizing the window. Bypass it.
            var buttons = MacUtils.GetMouseButtons();
#else
            System.Windows.Forms.MouseButtons buttons = System.Windows.Forms.MouseButtons.None;

            if (e.Mouse.LeftButton == ButtonState.Pressed)
                buttons = System.Windows.Forms.MouseButtons.Left;
            else if (e.Mouse.MiddleButton == ButtonState.Pressed)
                buttons = System.Windows.Forms.MouseButtons.Middle;
            else if (e.Mouse.RightButton == ButtonState.Pressed)
                buttons = System.Windows.Forms.MouseButtons.Right;
#endif

            return new System.Windows.Forms.MouseEventArgs(buttons, 1, x, y, 0);
        }

        public static System.Windows.Forms.MouseEventArgs ToWinFormArgs(MouseButtonEventArgs e, int x, int y)
        {
            System.Windows.Forms.MouseButtons buttons = System.Windows.Forms.MouseButtons.None;

            switch (e.Button)
            {
                case MouseButton.Left:   buttons = System.Windows.Forms.MouseButtons.Left; break;
                case MouseButton.Middle: buttons = System.Windows.Forms.MouseButtons.Middle; break;
                case MouseButton.Right:  buttons = System.Windows.Forms.MouseButtons.Right; break;
            }

            return new System.Windows.Forms.MouseEventArgs(buttons, 1, x, y, 0);
        }

        public static System.Windows.Forms.MouseEventArgs ToWinFormArgs(MouseWheelEventArgs e, int x, int y)
        {
            return new System.Windows.Forms.MouseEventArgs(System.Windows.Forms.MouseButtons.None, 0, x, y, (int)Math.Round(e.DeltaPrecise * 1200));
        }

        public static System.Windows.Forms.Keys ToWinFormKey(Key k)
        {
            if (k >= Key.A && k <= Key.Z)
                return System.Windows.Forms.Keys.A + (k - Key.A);
            else if (k >= Key.Number0 && k <= Key.Number9)
                return System.Windows.Forms.Keys.D0 + (k - Key.Number0);
            else if (k == Key.ControlRight || k == Key.ControlLeft || k == Key.Command || k == Key.WinLeft)
                return System.Windows.Forms.Keys.Control;
            else if (k == Key.AltRight || k == Key.AltLeft)
                return System.Windows.Forms.Keys.Alt;
            else if (k == Key.ShiftRight || k == Key.ShiftLeft)
                return System.Windows.Forms.Keys.Shift;
            else if (k == Key.Up)
                return System.Windows.Forms.Keys.Up;
            else if (k == Key.Down)
                return System.Windows.Forms.Keys.Down;
            else if (k == Key.Left)
                return System.Windows.Forms.Keys.Left;
            else if (k == Key.Right)
                return System.Windows.Forms.Keys.Right;
            else if (k == Key.Space)
                return System.Windows.Forms.Keys.Space;
            else if (k == Key.Enter)
                return System.Windows.Forms.Keys.Enter;
            else if (k == Key.Home)
                return System.Windows.Forms.Keys.Home;
            else if (k == Key.Delete)
                return System.Windows.Forms.Keys.Delete;
            else if (k == Key.Escape)
                return System.Windows.Forms.Keys.Escape;
            else if (k == Key.Tilde)
                return System.Windows.Forms.Keys.Oem3;
            else if (k == Key.Tab)
                return System.Windows.Forms.Keys.Tab;
            else if (k == Key.Plus)
                return System.Windows.Forms.Keys.Oemplus;
            else if (k == Key.BracketLeft)
                return System.Windows.Forms.Keys.Oem4;
            else if (k == Key.RBracket)
                return System.Windows.Forms.Keys.Oem6;
            else if (k == Key.Comma)
                return System.Windows.Forms.Keys.Oemcomma;
            else if (k == Key.Period)
                return System.Windows.Forms.Keys.OemPeriod;
            else if (k == Key.Semicolon)
                return System.Windows.Forms.Keys.Oem1;
            else if (k == Key.Slash)
                return System.Windows.Forms.Keys.Oem2;
            else if (k == Key.BackSpace)
                return System.Windows.Forms.Keys.Back;
            else if (k == Key.PageUp)
                return System.Windows.Forms.Keys.PageUp;
            else if (k == Key.PageDown)
                return System.Windows.Forms.Keys.PageDown;

            Trace.WriteLine($"Unknown key pressed {k}");

            return System.Windows.Forms.Keys.None;
        }

        public static Key FromWinFormKey(System.Windows.Forms.Keys k)
        {
            if (k >= System.Windows.Forms.Keys.A && k <= System.Windows.Forms.Keys.Z)
                return Key.A + (k - System.Windows.Forms.Keys.A);

            Debug.Assert(false);

            return Key.Unknown;
        }

        public static System.Windows.Forms.Keys GetModifierKeys()
        {
            System.Windows.Forms.Keys modifiers = System.Windows.Forms.Keys.None;

            if (Keyboard.GetState().IsKeyDown(Key.ControlRight) || Keyboard.GetState().IsKeyDown(Key.ControlLeft) || Keyboard.GetState().IsKeyDown(Key.Command) || Keyboard.GetState().IsKeyDown(Key.WinLeft))
                modifiers |= System.Windows.Forms.Keys.Control;
            if (Keyboard.GetState().IsKeyDown(Key.ShiftRight) || Keyboard.GetState().IsKeyDown(Key.ShiftLeft))
                modifiers |= System.Windows.Forms.Keys.Shift;
            if (Keyboard.GetState().IsKeyDown(Key.AltRight) || Keyboard.GetState().IsKeyDown(Key.AltLeft))
                modifiers |= System.Windows.Forms.Keys.Alt;

            return modifiers;
        }
}
}
