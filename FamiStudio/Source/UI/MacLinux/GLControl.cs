using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CursorType = Gdk.Cursor;

namespace FamiStudio
{
    public class GLControl
    {
        private CursorInfo cursorInfo;
        private FamiStudioForm parentForm;
        private int left = 0;
        private int top = 0;
        private int width = 100;
        private int height = 100;
        private bool invalid = true;

        protected GLControl() { cursorInfo = new CursorInfo(this); }
        protected virtual void OnRenderInitialized(GLGraphics g) { }
        protected virtual void OnRenderTerminated() { }
        protected virtual void OnRender(GLGraphics g) { }
        protected virtual void OnMouseDown(System.Windows.Forms.MouseEventArgs e) { }
        protected virtual void OnMouseUp(System.Windows.Forms.MouseEventArgs e) { }
        protected virtual void OnMouseDoubleClick(System.Windows.Forms.MouseEventArgs e) { }
        protected virtual void OnResize(EventArgs e) { }
        protected virtual void OnMouseMove(System.Windows.Forms.MouseEventArgs e) { }
        protected virtual void OnMouseLeave(EventArgs e) { }
        protected virtual void OnMouseWheel(System.Windows.Forms.MouseEventArgs e) { }
        protected virtual void OnMouseHorizontalWheel(System.Windows.Forms.MouseEventArgs e) { }
        protected virtual void OnKeyDown(System.Windows.Forms.KeyEventArgs e) { }
        protected virtual void OnKeyUp(System.Windows.Forms.KeyEventArgs e) { }

        public void RenderInitialized(GLGraphics g) { OnRenderInitialized(g); }
        public void Render(GLGraphics g) { OnRender(g); }
        public void MouseDown(System.Windows.Forms.MouseEventArgs e) { OnMouseDown(e); }
        public void MouseUp(System.Windows.Forms.MouseEventArgs e) { OnMouseUp(e); }
        public void MouseDoubleClick(System.Windows.Forms.MouseEventArgs e) { OnMouseDoubleClick(e); }
        public void MouseMove(System.Windows.Forms.MouseEventArgs e) { OnMouseMove(e); }
        public void MouseLeave(EventArgs e) { OnMouseLeave(e); }
        public void MouseWheel(System.Windows.Forms.MouseEventArgs e) { OnMouseWheel(e); }
        public void MouseHorizontalWheel(System.Windows.Forms.MouseEventArgs e) { OnMouseHorizontalWheel(e); }
        public void KeyDown(System.Windows.Forms.KeyEventArgs e) { OnKeyDown(e); }
        public void KeyUp(System.Windows.Forms.KeyEventArgs e) { OnKeyUp(e); }
        public void Focus() { }

        public System.Drawing.Point PointToClient(System.Drawing.Point p) { return parentForm.PointToClient(this, p); }
        public System.Drawing.Point PointToScreen(System.Drawing.Point p) { return parentForm.PointToScreen(this, p); }
        public System.Drawing.Rectangle ClientRectangle => new System.Drawing.Rectangle(0, 0, width, height);
        public void Validate() { invalid = false; }
        public void Invalidate() { invalid = true; }
        public int Left => left;
        public int Top => top;
        public int Width => width;
        public int Height => height;
        public bool Capture { set { if (value) parentForm.CaptureMouse(this); else parentForm.ReleaseMouse(); } }
        public bool NeedsRedraw => invalid;

        public void Move(int x, int y, int w, int h)
        {
            left = x;
            top = y;
            width = w;
            height = h;

            OnResize(EventArgs.Empty);
        }

        public System.Windows.Forms.Keys ModifierKeys => parentForm.GetModifierKeys(); 
        public FamiStudio App => parentForm?.FamiStudio; 
        public CursorInfo Cursor => cursorInfo; 
        public FamiStudioForm ParentForm { get => parentForm; set => parentForm = value; }
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
}
