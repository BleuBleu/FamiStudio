using System;
using System.Drawing;
using System.Diagnostics;

namespace FamiStudio
{
    public class Control
    {
        private ThemeRenderResources themeRes;
        private IntPtr cursor = Cursors.Default;
        protected FamiStudioWindow parentWindow;
        protected Dialog parentDialog;
        protected int left = 0;
        protected int top = 0;
        protected int width = 100;
        protected int height = 100;
        protected float windowScaling = 1.0f;
        protected float fontScaling = 1.0f;
        protected bool dirty = true;
        protected bool visible = true;
        protected bool enabled = true;
        protected string tooltip;

        protected Control(FamiStudioWindow win)
        {
            parentWindow = win;
        }

        protected Control(Dialog dlg) 
        {
            parentDialog = dlg;
            parentWindow = dlg.parentWindow;
            parentDialog.InitControl(this);
        }

        protected virtual void OnRenderInitialized(Graphics g) { }
        protected virtual void OnRenderTerminated() { }
        protected virtual void OnRender(Graphics g) { }
        protected virtual void OnMouseDown(MouseEventArgs e) { }
        protected virtual void OnMouseDownDelayed(MouseEventArgs e) { }
        protected virtual void OnMouseUp(MouseEventArgs e) { }
        protected virtual void OnMouseDoubleClick(MouseEventArgs e) { }
        protected virtual void OnResize(EventArgs e) { }
        protected virtual void OnMouseMove(MouseEventArgs e) { }
        protected virtual void OnMouseLeave(EventArgs e) { }
        protected virtual void OnMouseWheel(MouseEventArgs e) { }
        protected virtual void OnMouseHorizontalWheel(MouseEventArgs e) { }
        protected virtual void OnKeyDown(KeyEventArgs e) { }
        protected virtual void OnKeyUp(KeyEventArgs e) { }
        protected virtual void OnChar(CharEventArgs e) { }
        protected virtual void OnTouchDown(int x, int y) { }
        protected virtual void OnTouchUp(int x, int y) { }
        protected virtual void OnTouchMove(int x, int y) { }
        protected virtual void OnTouchClick(int x, int y) { }
        protected virtual void OnTouchDoubleClick(int x, int y) { }
        protected virtual void OnTouchLongPress(int x, int y) { }
        protected virtual void OnTouchScaleBegin(int x, int y) { }
        protected virtual void OnTouchScale(int x, int y, float scale) { }
        protected virtual void OnTouchScaleEnd(int x, int y) { }
        protected virtual void OnTouchFling(int x, int y, float velX, float velY) { }
        protected virtual void OnLostDialogFocus() { }
        protected virtual void OnVisibleChanged() { }
        protected virtual void OnAddedToDialog() { }

        public virtual bool WantsFullScreenViewport => false;
        public virtual void Tick(float delta) { }

        public void RenderInitialized(Graphics g) { OnRenderInitialized(g); }
        public void RenderTerminated() { OnRenderTerminated(); }
        public void Render(Graphics g) { OnRender(g); }
        public void MouseDown(MouseEventArgs e) { OnMouseDown(e); DialogMouseDownNotify(e); }
        public void MouseDownDelayed(MouseEventArgs e) { OnMouseDownDelayed(e); }
        public void MouseUp(MouseEventArgs e) { OnMouseUp(e); }
        public void MouseDoubleClick(MouseEventArgs e) { OnMouseDoubleClick(e); }
        public void MouseMove(MouseEventArgs e) { OnMouseMove(e); DialogMouseDownNotify(e); }
        public void MouseLeave(EventArgs e) { OnMouseLeave(e); }
        public void MouseWheel(MouseEventArgs e) { OnMouseWheel(e); }
        public void MouseHorizontalWheel(MouseEventArgs e) { OnMouseHorizontalWheel(e); }
        public void KeyDown(KeyEventArgs e) { OnKeyDown(e); }
        public void KeyUp(KeyEventArgs e) { OnKeyUp(e); }
        public void Char(CharEventArgs e) { OnChar(e); }
        public void TouchDown(int x, int y) { OnTouchDown(x, y); }
        public void TouchUp(int x, int y) { OnTouchUp(x, y); }
        public void TouchMove(int x, int y) { OnTouchMove(x, y); }
        public void TouchClick(int x, int y) { OnTouchClick(x, y); }
        public void TouchDoubleClick(int x, int y) { OnTouchDoubleClick(x, y); }
        public void TouchLongPress(int x, int y) { OnTouchLongPress(x, y); }
        public void TouchScaleBegin(int x, int y) { OnTouchScaleBegin(x, y); }
        public void TouchScale(int x, int y, float scale) { OnTouchScale(x, y, scale); }
        public void TouchScaleEnd(int x, int y) { OnTouchScaleEnd(x, y); }
        public void TouchFling(int x, int y, float velX, float velY) { OnTouchFling(x, y, velX, velY); }
        public void LostDialogFocus() { OnLostDialogFocus(); }
        public void AddedToDialog() { OnAddedToDialog(); }
        public void DialogMouseDownNotify(MouseEventArgs e) { if (parentDialog != null) parentDialog.DialogMouseDownNotify(this, e); }
        public void DialogMouseMoveNotify(MouseEventArgs e) { if (parentDialog != null) parentDialog.DialogMouseMoveNotify(this, e); }

        public Point PointToClient(Point p) { return parentWindow.PointToClient(this, p); }
        public Point PointToScreen(Point p) { return parentWindow.PointToScreen(this, p); }
        public Rectangle ClientRectangle => new Rectangle(0, 0, width, height);
        public Rectangle WindowRectangle => new Rectangle(WindowLeft, WindowTop, Width, Height);
        public Size ParentWindowSize => parentWindow.Size;
        public bool IsLandscape => parentWindow.IsLandscape;
        public int WindowLeft => parentDialog != null ? left + parentDialog.left : left;
        public int WindowTop => parentDialog != null ? top + parentDialog.top : top;
        public int WindowRight => WindowLeft + width;
        public int WindowBottom => WindowTop + height;
        public int Width => width;
        public int Height => height;
        public bool Capture { set { if (value) parentWindow.CaptureMouse(this); else parentWindow.ReleaseMouse(); } }
        public bool NeedsRedraw => dirty;
        public bool IsRenderInitialized => themeRes != null;
        public bool HasDialogFocus => parentDialog != null && parentDialog.FocusedControl == this;
        public void GrabDialogFocus() { if (parentDialog != null) parentDialog.FocusedControl = this; }
        public void ClearDialogFocus() { if (parentDialog != null) parentDialog.FocusedControl = null; }
        public bool Visible { get => visible; set { if (value != visible) { visible = value; OnVisibleChanged(); MarkDirty(); } } }
        public bool Enabled { get => enabled; set => SetAndMarkDirty(ref enabled, value); }
        public string ToolTip { get => tooltip; set { tooltip = value; MarkDirty(); } }
        public float WindowScaling => windowScaling;
        public float FontScaling => fontScaling;
        public ThemeRenderResources ThemeResources => themeRes;
        public void MarkDirty() { dirty = true; if (parentDialog != null) parentDialog.MarkDirty(); }
        public void ClearDirtyFlag() { dirty = false; }
        public void SetDpiScales(float main, float font) { windowScaling = main; fontScaling = font; }
        public void SetThemeRenderResource(ThemeRenderResources res) { themeRes = res; }

        public Point CursorPosition => parentWindow.GetCursorPosition();
        public ModifierKeys ModifierKeys => parentWindow.GetModifierKeys();
        public FamiStudio App => parentWindow?.FamiStudio;
        public IntPtr Cursor { get => cursor; set { cursor = value; parentWindow.RefreshCursor(); } }
        public FamiStudioWindow ParentWindow => parentWindow; 
        public Dialog ParentDialog => parentDialog; 

        public int ScaleForWindow(float val) { return (int)Math.Round(val * windowScaling); }
        public float ScaleForWindowFloat(float val) { return (val * windowScaling); }
        public int ScaleForFont(float val) { return (int)Math.Round(val * fontScaling); }
        public float ScaleForFontFloat(float val) { return (val * fontScaling); }
        public int ScaleCustom(float val, float scale) { return (int)Math.Round(val * scale); }
        public float ScaleCustomFloat(float val, float scale) { return (val * scale); }
        public int ScaleLineForWindow(int width) { return width == 1 ? 1 : (int)Math.Round(width * windowScaling) | 1; }

        public void Move(int x, int y, bool fireResizeEvent = true)
        {
            left = x;
            top = y;

            if (fireResizeEvent)
                OnResize(EventArgs.Empty);
        }

        public void Move(int x, int y, int w, int h, bool fireResizeEvent = true)
        {
            left = x;
            top = y;
            width = Math.Max(1, w);
            height = Math.Max(1, h);

            if (fireResizeEvent)
                OnResize(EventArgs.Empty);
        }

        public void Resize(int w, int h, bool fireResizeEvent = true)
        {
            width = Math.Max(1, w);
            height = Math.Max(1, h);

            if (fireResizeEvent)
                OnResize(EventArgs.Empty);
        }

        public void CenterToWindow()
        {
            Move((parentWindow.Width  - width) / 2,
                 (parentWindow.Height - height) / 2,
                 width, height);
        }

        protected bool SetAndMarkDirty<T>(ref T target, T current) where T : IComparable
        {
            if (target.CompareTo(current) != 0)
            {
                target = current;
                MarkDirty();
                return true;
            }

            return false;
        }
    }

    public class ModifierKeys
    { 
        // Matches GLFW
        private const int ModifierShift   = 1;
        private const int ModifierControl = Platform.IsMacOS ? 10 : 2;
        private const int ModifierAlt     = 4;
        private const int ModifierSuper   = 8;

        private int modifiers;
        private int forcedModifiers;

        public bool Shift     => (Modifiers & ModifierShift)   != 0;
        public bool Control   => (Modifiers & ModifierControl) != 0;
        public bool Alt       => (Modifiers & ModifierAlt)     != 0;
        public bool Super     => (Modifiers & ModifierSuper)   != 0;
        public int  Modifiers => (modifiers | forcedModifiers);

        public void Set(int mods)
        {
            modifiers = mods;
        }

        public void SetForced(int mods)
        {
            forcedModifiers = mods;
        }
    }

    public class MouseEventArgs
    {
        // Matches GLFW (1 << button)
        public const int ButtonLeft   = 1;
        public const int ButtonRight  = 2;
        public const int ButtonMiddle = 4;

        private int buttons;
        private int posX;
        private int posY;
        private float scrollX;
        private float scrollY;
        private bool delay;

        public bool Left   => (buttons & ButtonLeft)   != 0;
        public bool Right  => (buttons & ButtonRight)  != 0;
        public bool Middle => (buttons & ButtonMiddle) != 0;

        public int X => posX;
        public int Y => posY;
        public float ScrollX => scrollX;
        public float ScrollY => scrollY;
        public bool IsRightClickDelayed => delay;

        public MouseEventArgs(int btns, int x, int y, float sx = 0.0f, float sy = 0.0f)
        {
            buttons = btns;
            posX = x;
            posY = y;
            scrollX = sx;
            scrollY = sy;
        }

        public void DelayRightClick()
        {
            Debug.Assert(Right);
            delay = true;
        }
    }

    public class KeyEventArgs
    {   
        // Matches GLFW
        private const int ModifierShift   = 1;
        private const int ModifierControl = Platform.IsMacOS ? 10 : 2;
        private const int ModifierAlt     = 4;
        private const int ModifierSuper   = 8;

        private Keys key;
        private int  modifiers;
        private int  scancode;
        private bool repeat;
        private bool handled;

        public Keys Key      => key;
        public int  Scancode => scancode;
        public bool Shift    => (modifiers & ModifierShift)   != 0;
        public bool Control  => (modifiers & ModifierControl) != 0;
        public bool Alt      => (modifiers & ModifierAlt)     != 0;
        public bool Super    => (modifiers & ModifierSuper)   != 0;
        public bool IsRepeat => repeat;
        public bool Handled { get => handled; set => handled = value; }

        public KeyEventArgs(Keys k, int mods, bool rep, int scan)
        {
            key = k;
            modifiers = mods;
            scancode = scan;
            repeat = rep;
        }
    }

    public class CharEventArgs
    {
        // Matches GLFW
        private const int ModifierShift   = 1;
        private const int ModifierControl = Platform.IsMacOS ? 10 : 2;
        private const int ModifierAlt     = 4;
        private const int ModifierSuper   = 8;

        private char chr;
        private int  modifiers;

        public char Char    => chr;
        public bool Shift   => (modifiers & ModifierShift)   != 0;
        public bool Control => (modifiers & ModifierControl) != 0;
        public bool Alt     => (modifiers & ModifierAlt)     != 0;
        public bool Super   => (modifiers & ModifierSuper)   != 0;

        public CharEventArgs(char c, int mods)
        {
            chr = c;
            modifiers = mods;
        }
    }

    // Matches GLFW
    public enum Keys
    {
        Unknown = -1,
        Space = 32,
        Apostrophe = 39,
        Comma = 44,
        Minus = 45,
        Period = 46,
        Slash = 47,
        D0 = 48,
        D1 = 49,
        D2 = 50,
        D3 = 51,
        D4 = 52,
        D5 = 53,
        D6 = 54,
        D7 = 55,
        D8 = 56,
        D9 = 57,
        SemiColon = 59,
        Equal = 61,
        A = 65,
        B = 66,
        C = 67,
        D = 68,
        E = 69,
        F = 70,
        G = 71,
        H = 72,
        I = 73,
        J = 74,
        K = 75,
        L = 76,
        M = 77,
        N = 78,
        O = 79,
        P = 80,
        Q = 81,
        R = 82,
        S = 83,
        T = 84,
        U = 85,
        V = 86,
        W = 87,
        X = 88,
        Y = 89,
        Z = 90,
        LeftBracket = 91,
        BackSlash = 92,
        RightBracket = 93,
        Escape = 256,
        Enter = 257,
        Tab = 258,
        Backspace = 259,
        Insert = 260,
        Delete = 261,
        Right = 262,
        Left = 263,
        Down = 264,
        Up = 265,
        PageUp = 266,
        PageDown = 267,
        Home = 268,
        End = 269,
        CapsLock = 280,
        ScrollLock = 281,
        NumLock = 282,
        PrintScreen = 283,
        Pause = 284,
        F1 = 290,
        F2 = 291,
        F3 = 292,
        F4 = 293,
        F5 = 294,
        F6 = 295,
        F7 = 296,
        F8 = 297,
        F9 = 298,
        F10 = 299,
        F11 = 300,
        F12 = 301,
        F13 = 302,
        F14 = 303,
        F15 = 304,
        F16 = 305,
        F17 = 306,
        F18 = 307,
        F19 = 308,
        F20 = 309,
        F21 = 310,
        F22 = 311,
        F23 = 312,
        F24 = 313,
        F25 = 314,
        KeypadD0 = 320,
        KeypadD1 = 321,
        KeypadD2 = 322,
        KeypadD3 = 323,
        KeypadD4 = 324,
        KeypadD5 = 325,
        KeypadD6 = 326,
        KeypadD7 = 327,
        KeypadD8 = 328,
        KeypadD9 = 329,
        KeypadDecimal = 330,
        KeypadDivide = 331,
        KeypadMultiply = 332,
        KeypadSubtract = 333,
        KeypadAdd = 334,
        KeypadEnter = 335,
        KeypadEqual = 336,
        LeftShift = 340,
        LeftControl = 341,
        LeftAlt = 342,
        LeftSuper = 343,
        RightShift = 344,
        RightControl = 345,
        RightAlt = 346,
        RightSuper = 347,
        Menu = 348
    };

    // Matches Windows Forms
    public enum DialogResult
    {
        None,
        OK = 1,
        Cancel = 2,
        Yes = 6,
        No = 7
    }

    // Matches Windows Forms
    public enum MessageBoxButtons
    {
        OK = 0,
        YesNoCancel = 3,
        YesNo = 4,
    }
}
