using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace FamiStudio
{
    public class Control
    {
        private IntPtr cursor = Cursors.Default;
        protected Container container;
        protected FamiStudioWindow window; // Caching for efficiency.
        protected Graphics graphics;
        protected Fonts fonts;
        protected int left = 0;
        protected int top = 0;
        protected int width = 100;
        protected int height = 100;
        protected bool visible = true;
        protected bool enabled = true;
        protected bool canFocus = true;
        protected string tooltip;
        protected object userData;
        private bool tickEnabled;

        public delegate void ControlDelegate(Control sender);
        public delegate void MouseEventDelegate(Control sender, PointerEventArgs e);

        public event MouseEventDelegate PointerDown;
        public event MouseEventDelegate PointerUp;
        public event MouseEventDelegate PointerMove;
        public event MouseEventDelegate TouchClick;
        public event MouseEventDelegate TouchFling;
        public event MouseEventDelegate ContainerPointerDownNotify;
        public event MouseEventDelegate ContainerPointerUpNotify;
        public event MouseEventDelegate ContainerPointerMoveNotify;
        public event MouseEventDelegate ContainerTouchClickNotify;
        public event MouseEventDelegate ContainerTouchFlingNotify;

        protected Control()
        {
        }

        protected FamiStudioContainer ParentTopContainer 
        {
            get
            {
                if (this is FamiStudioContainer a)
                    return a;

                Container c = container;
                if (c == null)
                    return null;
                while (c.container != null)
                    c = c.container;

                return c as FamiStudioContainer;
            }
        }

        protected virtual void OnRender(Graphics g) { } 
        protected virtual void OnPointerDown(PointerEventArgs e) { }
        protected virtual void OnPointerDownDelayed(PointerEventArgs e) { }
        protected virtual void OnPointerUp(PointerEventArgs e) { }
        protected virtual void OnMouseDoubleClick(PointerEventArgs e) { }
        protected virtual void OnResize(EventArgs e) { }
        protected virtual void OnPointerMove(PointerEventArgs e) { }
        protected virtual void OnPointerEnter(EventArgs e) { }
        protected virtual void OnPointerLeave(EventArgs e) { }
        protected virtual void OnMouseWheel(PointerEventArgs e) { }
        protected virtual void OnMouseHorizontalWheel(PointerEventArgs e) { }
        protected virtual void OnKeyDown(KeyEventArgs e) { }
        protected virtual void OnKeyUp(KeyEventArgs e) { }
        protected virtual void OnChar(CharEventArgs e) { }
        protected virtual void OnTouchClick(PointerEventArgs e) { }
        protected virtual void OnTouchDoubleClick(PointerEventArgs e) { }
        protected virtual void OnTouchLongPress(PointerEventArgs e) { }
        protected virtual void OnTouchScaleBegin(PointerEventArgs e) { }
        protected virtual void OnTouchScale(PointerEventArgs e) { }
        protected virtual void OnTouchScaleEnd(PointerEventArgs e) { }
        protected virtual void OnTouchFling(PointerEventArgs e) { }
        protected virtual void OnLostDialogFocus() { }
        protected virtual void OnAcquiredDialogFocus() { }
        protected virtual void OnVisibleChanged() { }
        protected virtual void OnAddedToContainer() { }

        public virtual bool WantsFullScreenViewport => false;
        public virtual void Tick(float delta) { }

        public virtual void Render(Graphics g) { OnRender(g); }
        public void SendPointerDown(PointerEventArgs e) { OnPointerDown(e); PointerDown?.Invoke(this, e); SendContainerPointerDownNotify(e); }
        public void SendPointerDownDelayed(PointerEventArgs e) { OnPointerDownDelayed(e); }
        public void SendPointerUp(PointerEventArgs e) { OnPointerUp(e); PointerUp?.Invoke(this, e); SendContainerPointerUpNotify(e); }
        public void SendPointerMove(PointerEventArgs e) { OnPointerMove(e); PointerMove?.Invoke(this, e); SendContainerPointerMoveNotify(e); }
        public void SendPointerEnter(EventArgs e) { if (IsContainedByMainWindow) OnPointerEnter(e); }
        public void SendPointerLeave(EventArgs e) { if (IsContainedByMainWindow) OnPointerLeave(e); }
        public void SendMouseWheel(PointerEventArgs e) { OnMouseWheel(e); SendContainerMouseWheelNotify(e); }
        public void SendMouseHorizontalWheel(PointerEventArgs e) { OnMouseHorizontalWheel(e); }
        public void SendKeyDown(KeyEventArgs e) { OnKeyDown(e); }
        public void SendKeyUp(KeyEventArgs e) { OnKeyUp(e); }
        public void SendChar(CharEventArgs e) { OnChar(e); }
        public void SendLostDialogFocus() { OnLostDialogFocus(); }
        public void SendAcquiredDialogFocus() { OnAcquiredDialogFocus(); }
        public void SendAddedToContainer() { OnAddedToContainer(); }
        public void SendTouchClick(PointerEventArgs e) { OnTouchClick(e); TouchClick?.Invoke(this, e); SendContainerTouchClickNotify(e); }
        public void SendTouchScaleBegin(PointerEventArgs e) { OnTouchScaleBegin(e); }
        public void SendTouchScale(PointerEventArgs e) { OnTouchScale(e); }
        public void SendTouchScaleEnd(PointerEventArgs e) { OnTouchScaleEnd(e); }
        public void SendTouchFling(PointerEventArgs e) { OnTouchFling(e); TouchFling?.Invoke(this, e); SendContainerTouchFlingNotify(e); }

        // MATTT : What about this one?
        public void SendMouseDoubleClick(PointerEventArgs e)
        {
            if (SupportsDoubleClick)
            {
                OnMouseDoubleClick(e);
            }
            else
            {
                OnPointerDown(e);
                OnPointerUp(e);
            }
        }

        // MATTT : What about this one?
        public void SendTouchDoubleClick(PointerEventArgs e)
        {
            //if (SupportsDoubleClick)
                OnTouchDoubleClick(e);
            //else
            //    OnTouchClick(e);
        }

        public void SendTouchLongPress(PointerEventArgs e)
        {
            if (SupportsLongPress)
            {
                OnTouchLongPress(e);
            }
            else
            {
                var e2 = e.CloneWithNewButton(PointerEventArgs.ButtonRight); // Pretend right-click.
                SendPointerDown(e2);
                SendPointerUp(e2);
            }
        }

        public void SendContainerMouseWheelNotify(PointerEventArgs e) 
        { 
            var c = ParentContainer; 
            while (c != null) 
            { 
                c.OnContainerMouseWheelNotify(this, e);
                if (c is Dialog)
                    break;
                c = c.ParentContainer;
            }
        } 

        public void SendContainerPointerDownNotify(PointerEventArgs e) 
        {
            var c = ParentContainer;
            while (c != null)
            {
                c.OnContainerPointerDownNotify(this, e);
                c.ContainerPointerDownNotify?.Invoke(this, e);
                if (c is Dialog)
                    break;
                c = c.ParentContainer;
            }
        }

        public void SendContainerPointerUpNotify(PointerEventArgs e)
        {
            var c = ParentContainer;
            while (c != null)
            {
                c.OnContainerPointerUpNotify(this, e);
                c.ContainerPointerUpNotify?.Invoke(this, e);
                if (c is Dialog)
                    break;
                c = c.ParentContainer;
            }
        }

        public void SendContainerPointerMoveNotify(PointerEventArgs e)
        {
            var c = ParentContainer;
            while (c != null)
            {
                c.OnContainerPointerMoveNotify(this, e);
                c.ContainerPointerMoveNotify?.Invoke(this, e);
                if (c is Dialog)
                    break;
                c = c.ParentContainer;
            }
        }

        public void SendContainerTouchClickNotify(PointerEventArgs e)
        {
            var c = ParentContainer;
            while (c != null)
            {
                c.OnContainerTouchClickNotify(this, e);
                c.ContainerTouchClickNotify?.Invoke(this, e);
                if (c is Dialog)
                    break;
                c = c.ParentContainer;
            }
        }

        public void SendContainerTouchFlingNotify(PointerEventArgs e)
        {
            var c = ParentContainer;
            while (c != null)
            {
                c.OnContainerTouchFlingNotify(this, e);
                c.ContainerTouchFlingNotify?.Invoke(this, e);
                if (c is Dialog)
                    break;
                c = c.ParentContainer;
            }
        }

        public Rectangle Rectangle => new Rectangle(left, top, width, height);
        public Rectangle ClientRectangle => new Rectangle(0, 0, width, height);
        public Rectangle WindowRectangle => new Rectangle(WindowPosition, Size);
        public Size ParentWindowSize => ParentWindow.Size;
        public bool IsLandscape => ParentWindow.IsLandscape;
        public int Left => left;
        public int Top => top;
        public int Right => left + width;
        public int Bottom => top + height;
        public int Width => width;
        public int Height => height;
        public Size Size => new Size(width, height);
        public bool Capture { set { if (value) ParentWindow.CaptureMouse(this); else ParentWindow.ReleaseMouse(); } }
        public bool HasDialogFocus => ParentDialog?.FocusedControl == this;
        public void GrabDialogFocus()  { if (ParentDialog != null) ParentDialog.FocusedControl = this; }
        public void ClearDialogFocus() { if (ParentDialog != null) ParentDialog.FocusedControl = null; }
        public bool Visible { get => visible; set { if (value != visible) { visible = value; OnVisibleChanged(); MarkDirty(); } } }
        public virtual bool Enabled { get => enabled; set => SetAndMarkDirty(ref enabled, value); }
        public bool CanFocus { get => canFocus; }
        public bool IsContainedByMainWindow => ParentTopContainer != null;
        public string ToolTip { get => tooltip; set { SetAndMarkDirty(ref tooltip, value); } }
        public object UserData { get => userData; set => userData = value; }
        public virtual bool SupportsLongPress => true;
        public virtual bool SupportsDoubleClick => true;
        public bool TickEnabled => tickEnabled;
        public void MarkDirty() { window?.MarkDirty(); }

        public bool HasParent => container != null;
        public Point CursorPosition => ParentWindow.GetCursorPosition();
        public ModifierKeys ModifierKeys => ParentWindow.GetModifierKeys();
        public FamiStudio App => ParentWindow?.FamiStudio;
        public IntPtr Cursor { get => cursor; set { cursor = value; ParentWindow.RefreshCursor(); } }
        public FamiStudioWindow ParentWindow => window;
        public Container ParentContainer => container;
        public Graphics Graphics => graphics;
        public Fonts Fonts => fonts;
        public Point WindowPosition => ControlToWindow(Point.Empty);

        public Dialog ParentDialog
        {
            get
            {
                var c = container;
                while (c != null && !(c is Dialog))
                    c = c.container;
                return c as Dialog;
            }
        }

        public virtual Point ControlToWindow(Point p)
        {
            Container c = container;

            p.X += left;
            p.Y += top;

            while (c != null)
            {
                p.X += c.Left;
                p.Y += c.Top;
                p.X -= c.ScrollX;
                p.Y -= c.ScrollY;
                c = c.ParentContainer;
            }

            return p;
        }

        public virtual Point WindowToControl(Point p)
        {
            Container c = container;

            while (c != null)
            {
                p.X -= c.Left;
                p.Y -= c.Top;
                p.X += c.ScrollX;
                p.Y += c.ScrollY;
                c = c.ParentContainer;
            }

            return p;
        }

        public Point ControlToWindow(int x, int y)
        {
            return ControlToWindow(new Point(x, y));
        }

        public Point WindowToControl(int x, int y)
        {
            return WindowToControl(new Point(x, y));
        }

        protected void SetTickEnabled(bool enabled)
        {
            if (tickEnabled != enabled)
            {
                if (container != null)
                    container.IncrementControlTickEnabled(enabled ? 1 : -1);
                tickEnabled = enabled;
            }
        }

        public virtual bool HitTest(int winX, int winY)
        {
            return WindowRectangle.Contains(winX, winY);
        }

        public Point ScreenToControl(Point p) 
        {
            return WindowToControl(ParentWindow.ScreenToWindow(p));
        }

        public Point ControlToScreen(Point p) 
        {
            return ControlToWindow(ParentWindow.WindowToScreen(p)); 
        }

        public void SetParentContainer(Container c)
        {
            Debug.Assert((container == null) != (c == null));
            Debug.Assert(c == null || c.ParentTopContainer != null);
            Debug.Assert(c == null || c.ParentWindow != null);

            container = c;
            window = c?.ParentWindow;
            fonts = window?.Fonts;
            graphics = window?.Graphics;
        }

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
            width  = Math.Max(1, w);
            height = Math.Max(1, h);

            if (fireResizeEvent)
                OnResize(EventArgs.Empty);
        }

        public void CenterToWindow()
        {
            Move((ParentWindow.Width  - width) / 2,
                 (ParentWindow.Height - height) / 2,
                 width, height);
        }

        protected bool SetAndMarkDirty<T>(ref T target, T current) where T : IEquatable<T>
        {
            if (((target == null) != (current == null)) || (target != null && !target.Equals(current)))
            {
                target = current;
                MarkDirty();
                return true;
            }

            return false;
        }

        public void OverrideGraphics(Graphics g, Fonts f)
        {
            graphics = g;
            fonts = f;
        }
    }

    public struct ModifierKeys
    { 
        // Matches GLFW
        private const int ShiftMask   = 1;
        private const int ControlMask = Platform.IsMacOS ? 10 : 2;
        private const int AltMask     = 4;
        private const int SuperMask   = 8;

        private int value;

        public bool IsShiftDown   => (value & ShiftMask)   != 0;
        public bool IsControlDown => (value & ControlMask) != 0;
        public bool IsAltDown     => (value & AltMask)     != 0;
        public bool IsSuperDown   => (value & SuperMask)   != 0;
        public int  Value         => value;

        public static readonly ModifierKeys Control      = new ModifierKeys(ControlMask);
        public static readonly ModifierKeys Shift        = new ModifierKeys(ShiftMask);
        public static readonly ModifierKeys Alt          = new ModifierKeys(AltMask);
        public static readonly ModifierKeys ControlShift = new ModifierKeys(ControlMask | ShiftMask);

        public ModifierKeys(int val)
        {
            value = FixMods(val);
        }

        public void Set(int mods)
        {
            value = FixMods(mods);
        }

        private static int FixMods(int val)
        {
            if (Platform.IsMacOS)
            {
                // On MacOS, we dont differentiate between command and control.
                // Both need to be set to match shortcuts correctly.
                if ((val & ControlMask) != 0)
                    val |= ControlMask;
            }

            return val;
        }

        public override bool Equals(object obj)
        {
            var other = (ModifierKeys)obj;
            return value == other.value;
        }

        public override int GetHashCode()
        {
            return value;
        }

        public static ModifierKeys operator |(ModifierKeys m0, ModifierKeys m1)
        {
            return new ModifierKeys(m0.value | m1.Value);
        }

        public static bool operator ==(ModifierKeys m0, ModifierKeys m1)
        {
            return m0.value == m1.value;
        }

        public static bool operator !=(ModifierKeys m0, ModifierKeys m1)
        {
            return m0.value != m1.value;
        }

        public string ToTooltipString()
        {
            var str = "";
            if (IsControlDown) str += "<Ctrl>";
            if (IsShiftDown)   str += "<Shift>";
            if (IsAltDown)     str += "<Alt>";
            return str;
        }

        public string ToDisplayString()
        {
            var str = "";
            if (IsControlDown) str += "Ctrl+";
            if (IsShiftDown)   str += "Shift+";
            if (IsAltDown)     str += "Alt+";
            return str;
        }

        public void FromConfigString(Span<string> splits)
        {
            value = 0;

            foreach (var s in splits)
            {
                switch (s)
                {
                    case "Ctrl"  : value |= ControlMask; break;
                    case "Shift" : value |= ShiftMask;   break;
                    case "Alt"   : value |= AltMask;     break;
                }
            }
        }

        public string ToConfigString()
        {
            var str = "";
            if (IsControlDown) str += "Ctrl+";
            if (IsShiftDown)   str += "Shift+";
            if (IsAltDown)     str += "Alt+";
            return str;
        }

        public override string ToString()
        {
            return ToDisplayString();
        }
    }

    public class PointerEventArgs
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
        private bool handled; // Only use for mousewheel right now. Not fully implemented.
        private bool touch;

        private float touchScale;
        private float flingVelX;
        private float flingVelY;

        public bool Left   => (buttons & ButtonLeft)   != 0;
        public bool Right  => (buttons & ButtonRight)  != 0;
        public bool Middle => (buttons & ButtonMiddle) != 0;

        public int X => posX;
        public int Y => posY;
        public Point Position => new Point(posX, posY);
        public float ScrollX => scrollX;
        public float ScrollY => scrollY;
        public float FlingVelocityX => flingVelX;
        public float FlingVelocityY => flingVelY;
        public float TouchScale => touchScale;
        public bool IsTouchEvent => touch;
        public bool IsRightClickDelayed => delay;
        public bool Handled => handled;

        public PointerEventArgs(int btns, int x, int y, bool t = false, float sx = 0.0f, float sy = 0.0f)
        {
            buttons = btns;
            posX = x;
            posY = y;
            scrollX = sx;
            scrollY = sy;
            touch = t;
        }

        public PointerEventArgs(int x, int y, float scale = 1.0f)
        {
            buttons = ButtonLeft;
            posX = x;
            posY = y;
            touchScale = scale;
            touch = true;
        }

        public PointerEventArgs(int x, int y, float velX, float velY)
        {
            buttons = ButtonLeft;
            posX = x;
            posY = y;
            flingVelX = velX;
            flingVelY = velY;
            touch = true;
        }

        public PointerEventArgs CloneWithNewButton(int button)
        {
            var copy = MemberwiseClone() as PointerEventArgs;
            copy.buttons = button;
            return copy;
        }

        public void MarkHandled()
        {
            handled = true;
        }

        public void DelayRightClick()
        {
            Debug.Assert(Right);
            delay = true;
        }
    }

    public class KeyEventArgs
    {   
        private Keys key;
        private ModifierKeys modifiers;
        private int  scancode;
        private bool repeat;
        private bool handled;

        public Keys Key => key;
        public ModifierKeys Modifiers => modifiers;
        
        public int  Scancode  => scancode;
        public bool Shift     => modifiers.IsShiftDown;
        public bool Control   => modifiers.IsControlDown;
        public bool Alt       => modifiers.IsAltDown;
        public bool Super     => modifiers.IsSuperDown;
        public bool IsRepeat  => repeat;
        public bool Handled { get => handled; set => handled = value; }

        public KeyEventArgs(Keys k, ModifierKeys mods, bool rep, int scan)
        {
            key = k;
            modifiers = mods;
            scancode = scan;
            repeat = rep;
        }
    }

    public class CharEventArgs
    {
        private char chr;
        private ModifierKeys modifiers;

        public char Char      => chr;
        public bool Shift     => modifiers.IsShiftDown;
        public bool Control   => modifiers.IsControlDown;
        public bool Alt       => modifiers.IsAltDown;
        public bool Super     => modifiers.IsSuperDown;

        public ModifierKeys Modifiers => modifiers;

        public CharEventArgs(char c, ModifierKeys mods)
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

    public class Shortcut
    {
        public string DisplayName    { get; private set; }
        public string ConfigName     { get; private set; }
        public bool   AllowModifiers { get; private set; } = true;
        public readonly bool AllowTwoShortcuts = true; // Always allow... { get; private set; }

        //public int[] ScanCodes = new int[2];
        public Keys[] KeyValues         { get; private set; } = new Keys[2];
        public ModifierKeys[] Modifiers { get; private set; } = new ModifierKeys[2];

        private Shortcut()
        {
        }

        public Shortcut(string displayName, string configName, Keys k, bool allowModifiers)
        {
            DisplayName = displayName;
            ConfigName = configName;
            AllowModifiers = allowModifiers;
            KeyValues[0] = k;
            KeyValues[1] = Keys.Unknown;
            Settings.AllShortcuts.Add(this);
        }

        public Shortcut(string displayName, string configName, Keys k, ModifierKeys m = default(ModifierKeys))
        {
            DisplayName = displayName;
            ConfigName = configName;
            Modifiers[0] = m;
            KeyValues[0] = k;
            KeyValues[1] = Keys.Unknown;
            Settings.AllShortcuts.Add(this);
        }

        public Shortcut(string displayName, string configName, Keys k1, Keys k2)
        {
            DisplayName = displayName;
            ConfigName = configName;
            KeyValues[0] = k1;
            KeyValues[1] = k2;
            //AllowTwoShortcuts = true;
            Settings.AllShortcuts.Add(this);
        }

        public Shortcut(string displayName, string configName, Keys k1, ModifierKeys m1, Keys k2, ModifierKeys m2)
        {
            DisplayName = displayName;
            ConfigName = configName;
            Modifiers[0] = m1;
            KeyValues[0] = k1;
            Modifiers[1] = m2;
            KeyValues[1] = k2;
            //AllowTwoShortcuts = true;
            Settings.AllShortcuts.Add(this);
        }

        private Shortcut Clone()
        {
            var clone = new Shortcut();
            clone.DisplayName = DisplayName;
            clone.ConfigName = ConfigName;
            //clone.AllowTwoShortcuts = AllowTwoShortcuts;
            clone.AllowModifiers = AllowModifiers;
            clone.KeyValues = KeyValues.Clone() as Keys[];
            clone.Modifiers = Modifiers.Clone() as ModifierKeys[];
            return clone;
        }

        public static List<Shortcut> CloneList(List<Shortcut> list)
        {
            var clone = new List<Shortcut>();
            foreach (var c in list)
                clone.Add(c.Clone());
            return clone;
        }

        public void Clear(int idx)
        {
            KeyValues[idx] = Keys.Unknown;
            Modifiers[idx].Set(0);
        }

        public bool IsShortcutValid(int idx)
        {
            return KeyValues[idx] != Keys.Unknown;
        }

        public bool Matches(KeyEventArgs e, int idx)
        {
            return IsShortcutValid(idx) && e.Modifiers == Modifiers[idx] && e.Key == KeyValues[idx];
        }

        public bool Matches(KeyEventArgs e)
        {
            return Matches(e, 0) || Matches(e, 1);
        }

        public bool IsKeyDown(FamiStudioWindow win, int idx)
        {
            Debug.Assert(!AllowModifiers);
            return IsShortcutValid(idx) && win.IsKeyDown(KeyValues[idx]);
        }

        public bool IsKeyDown(FamiStudioWindow win)
        {
            return IsKeyDown(win, 0) || IsKeyDown(win, 1);
        }

        private string GetShortcutKeyString(int idx)
        {
            var str = Platform.KeyToString(KeyValues[idx]);
            if (string.IsNullOrEmpty(str))
                str = KeyValues[idx].ToString();
            else
                str = str.ToUpper();

            // Fixup a few edge cases.
            switch (str)
            {
                case "\t": str = "Tab";   break;
                case "\r": str = "Enter"; break;
                case null: str = "???";   break;
            }

            return str;
        }

        public string TooltipString
        {
            get
            {
                var str = "";

                if (Platform.IsDesktop)
                {
                    // TODO : Some stuff like "Redo" have 2 shortcuts AND tooltips.
                    if (IsShortcutValid(0))
                    {
                        if (Modifiers[0].Value != 0)
                            str = $"{Modifiers[0].ToTooltipString()}";
                        str += $"<{GetShortcutKeyString(0)}>";
                    }

                    // HACK : 'Ctrl' gets converted to 'Cmd' in the tooltip, but for some
                    // commands on MacOS we will really want to display Ctrl since they
                    // conflict with some built-in OS shortcuts.
                    if (Platform.IsMacOS)
                    {
                        switch (str)
                        {
                            case "<Ctrl>+<Space>":
                                str = "<ForceCtrl>+<Space>";
                                break;
                        }
                    }
                }

                return str;
            }
        }

        public void FromConfigString(string s, int idx)
        {
            var splits = s.Split('+', StringSplitOptions.RemoveEmptyEntries);
            
            if (splits.Length > 0)
            {
                Modifiers[idx].FromConfigString(splits.AsSpan(0, splits.Length - 1));
                KeyValues[idx] = (Keys)Enum.Parse(typeof(Keys), splits[splits.Length - 1]);
            }
            else
            {
                KeyValues[idx] = Keys.Unknown;
            }
        }

        public string ToConfigString(int idx)
        {
            Debug.Assert(idx == 0 || AllowTwoShortcuts);
            return Modifiers[idx].ToConfigString() + KeyValues[idx].ToString();
        }

        public string ToDisplayString(int idx)
        {
            var str = "";

            if (IsShortcutValid(idx))
            {
                if (Modifiers[idx].Value != 0)
                    str += $"{Modifiers[idx]}";
                str += (GetShortcutKeyString(idx) ?? "Unknown");
            }

            return str;
        }

        public override string ToString()
        {
            var str = DisplayName + " " + ToDisplayString(0);
            if (AllowTwoShortcuts)
                str += " " + ToDisplayString(1);
            return str.Trim();
        }
    }

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
