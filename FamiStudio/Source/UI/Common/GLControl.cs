using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if FAMISTUDIO_WINDOWS
using CursorType = System.Windows.Forms.Cursor;
#elif FAMISTUDIO_ANDROID
using CursorType = System.Object;
#else
using CursorType = Gdk.Cursor;
#endif

namespace FamiStudio
{
    public class GLControl
    {
        private CursorInfo cursorInfo;
        private FamiStudioForm parentForm;
        private ThemeRenderResources themeRes;
        protected Dialog parentDialog;
        protected int left = 0;
        protected int top = 0;
        protected int width = 100;
        protected int height = 100;
        protected float mainWindowScaling = 1.0f;
        protected float fontScaling = 1.0f;
        protected bool dirty = true;
        protected bool visible = true;
        protected string tooltip;

        protected GLControl() { cursorInfo = new CursorInfo(this); }
        protected virtual void OnRenderInitialized(GLGraphics g) { }
        protected virtual void OnRenderTerminated() { }
        protected virtual void OnRender(GLGraphics g) { }
        protected virtual void OnMouseDown(MouseEventArgsEx e) { }
        protected virtual void OnMouseDownDelayed(System.Windows.Forms.MouseEventArgs e) { }
        protected virtual void OnMouseUp(System.Windows.Forms.MouseEventArgs e) { }
        protected virtual void OnMouseDoubleClick(System.Windows.Forms.MouseEventArgs e) { }
        protected virtual void OnResize(EventArgs e) { }
        protected virtual void OnMouseMove(System.Windows.Forms.MouseEventArgs e) { }
        protected virtual void OnMouseLeave(EventArgs e) { }
        protected virtual void OnMouseWheel(System.Windows.Forms.MouseEventArgs e) { }
        protected virtual void OnMouseHorizontalWheel(System.Windows.Forms.MouseEventArgs e) { }
        protected virtual void OnKeyDown(System.Windows.Forms.KeyEventArgs e) { }
        protected virtual void OnKeyUp(System.Windows.Forms.KeyEventArgs e) { }
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

        public void RenderInitialized(GLGraphics g) { OnRenderInitialized(g); }
        public void RenderTerminated() { OnRenderTerminated(); }
        public void Render(GLGraphics g) { OnRender(g); }
        public void MouseDown(MouseEventArgsEx e) { OnMouseDown(e); DialogMouseDownNotify(e); }
        public void MouseDownDelayed(System.Windows.Forms.MouseEventArgs e) { OnMouseDownDelayed(e); }
        public void MouseUp(System.Windows.Forms.MouseEventArgs e) { OnMouseUp(e); }
        public void MouseDoubleClick(System.Windows.Forms.MouseEventArgs e) { OnMouseDoubleClick(e); }
        public void MouseMove(System.Windows.Forms.MouseEventArgs e) { OnMouseMove(e); DialogMouseDownNotify(e); }
        public void MouseLeave(EventArgs e) { OnMouseLeave(e); }
        public void MouseWheel(System.Windows.Forms.MouseEventArgs e) { OnMouseWheel(e); }
        public void MouseHorizontalWheel(System.Windows.Forms.MouseEventArgs e) { OnMouseHorizontalWheel(e); }
        public void KeyDown(System.Windows.Forms.KeyEventArgs e) { OnKeyDown(e); }
        public void KeyUp(System.Windows.Forms.KeyEventArgs e) { OnKeyUp(e); }
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
        public void DialogMouseDownNotify(System.Windows.Forms.MouseEventArgs e) { if (parentDialog != null) parentDialog.DialogMouseDownNotify(this, e); }
        public void DialogMouseMoveNotify(System.Windows.Forms.MouseEventArgs e) { if (parentDialog != null) parentDialog.DialogMouseMoveNotify(this, e); }

        public System.Drawing.Point PointToClient(System.Drawing.Point p) { return parentForm.PointToClient(this, p); }
        public System.Drawing.Point PointToScreen(System.Drawing.Point p) { return parentForm.PointToScreen(this, p); }
        public System.Drawing.Rectangle ClientRectangle => new System.Drawing.Rectangle(0, 0, width, height);
        public System.Drawing.Rectangle Rectangle => new System.Drawing.Rectangle(Left, Top, Width, Height);
        public System.Drawing.Size ParentFormSize => parentForm.Size;
        public bool IsLandscape => parentForm.IsLandscape;
        public int Left => parentDialog != null ? left + parentDialog.left : left;
        public int Top => parentDialog != null ? top + parentDialog.top : top;
        public int Right => Left + width;
        public int Bottom => Top + height;
        public int Width => width;
        public int Height => height;
        public bool Capture { set { if (value) parentForm.CaptureMouse(this); else parentForm.ReleaseMouse(); } }
        public bool NeedsRedraw => dirty;
        public bool IsRenderInitialized => themeRes != null;
        public bool HasDialogFocus => parentDialog != null && parentDialog.FocusedControl == this;
        public void GrabDialogFocus() { if (parentDialog != null) parentDialog.FocusedControl = this; }
        public void ClearDialogFocus() { if (parentDialog != null) parentDialog.FocusedControl = null; }
        public bool Visible { get => visible; set { if (value != visible) { visible = value; OnVisibleChanged(); MarkDirty(); } } }
        public string ToolTip { get => tooltip; set { tooltip = value; MarkDirty(); } }
        public float MainWindowScaling => mainWindowScaling;
        public float FontScaling => fontScaling;
        public ThemeRenderResources ThemeResources => themeRes;
        public void MarkDirty() { dirty = true; if (parentDialog != null) parentDialog.MarkDirty(); }
        public void ClearDirtyFlag() { dirty = false; }
        public void SetDpiScales(float main, float font) { mainWindowScaling = main; fontScaling = font; }
        public void SetThemeRenderResource(ThemeRenderResources res) { themeRes = res; }

        public System.Windows.Forms.Keys ModifierKeys => parentForm.GetModifierKeys();
        public FamiStudio App => parentForm?.FamiStudio;
        public CursorInfo Cursor => cursorInfo;
        public FamiStudioForm ParentForm { get => parentForm; set => parentForm = value; }
        public Dialog ParentDialog { get => parentDialog; set => parentDialog = value; }

        public int ScaleForMainWindow(float val) { return (int)Math.Round(val * mainWindowScaling); }
        public float ScaleForMainWindowFloat(float val) { return (val * mainWindowScaling); }
        public int ScaleForFont(float val) { return (int)Math.Round(val * fontScaling); }
        public float ScaleForFontFloat(float val) { return (val * fontScaling); }
        public int ScaleCustom(float val, float scale) { return (int)Math.Round(val * scale); }
        public float ScaleCustomFloat(float val, float scale) { return (val * scale); }
        public int ScaleLineForMainWindow(int width) { return width == 1 ? 1 : (int)Math.Round(width * mainWindowScaling) | 1; }


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

        public void CenterToForm()
        {
            Move((parentForm.Width - width) / 2,
                 (parentForm.Height - height) / 2,
                 width, height);
        }

        protected void SetAndMarkDirty<T>(ref T target, T current) where T : IComparable
        {
            if (target.CompareTo(current) != 0)
            {
                target = current;
                MarkDirty();
            }
        }
    }

    public class CursorInfo
    {
        private CursorType cursor = Cursors.Default;
        private GLControl parentControl;

        public CursorInfo(GLControl ctrl) { parentControl = ctrl; }
        public System.Drawing.Point Position => parentControl.ParentForm.GetCursorPosition();
        public CursorType Current
        {
            get { return cursor; }
            set { cursor = value; parentControl.ParentForm.RefreshCursor(); }
        }
    }

    public class MouseEventArgsEx : System.Windows.Forms.MouseEventArgs
    {
        private bool delay = false;
        public  bool IsRightClickDelayed => delay;

        public MouseEventArgsEx(System.Windows.Forms.MouseEventArgs e) :
            base(e.Button, e.Clicks, e.X, e.Y, e.Delta)
        {
        }

        public MouseEventArgsEx(System.Windows.Forms.MouseButtons button, int clicks, int x, int y, int delta) :
            base(button, clicks, x, y, delta)
        {
        }

        public void DelayRightClick()
        {
            Debug.Assert(Button.HasFlag(System.Windows.Forms.MouseButtons.Right));
            delay = true;
        }
    }
}
