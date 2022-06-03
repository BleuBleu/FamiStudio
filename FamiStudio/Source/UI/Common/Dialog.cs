using System;
using System.Collections.Generic;

namespace FamiStudio
{
    public class Dialog : Control
    {
        public delegate void KeyDownDelegate(Dialog dlg, KeyEventArgs e);
        public event KeyDownDelegate DialogKeyDown;

        const float ToolTipDelay = 0.2f;
        const int   ToolTipMaxCharsPerLine = 64;

        private List<Control> controls = new List<Control>();
        private Control focusedControl;
        private DialogResult result = DialogResult.None;
        private float tooltipTimer;

        private int tooltipTopMargin  = DpiScaling.ScaleForWindow(2);
        private int tooltipSideMargin = DpiScaling.ScaleForWindow(4);
        private int tooltipOffsetY = DpiScaling.ScaleForWindow(24);

        private CommandList commandList;
        private CommandList commandListForeground;

        public CommandList CommandList => commandList;
        public CommandList CommandListForeground => commandListForeground;
        public DialogResult DialogResult => result;

        public IReadOnlyCollection<Control> Controls => controls.AsReadOnly();

        public Control FocusedControl
        {
            get { return focusedControl; }
            set 
            {
                if (value == focusedControl)
                    return;

                if (focusedControl != null)
                    focusedControl.LostDialogFocus();

                focusedControl = value;
                MarkDirty();
            }
        }

        public Dialog()
        {
            visible = false;
            // MATTT: Add the form to the constructor eventually.
            FamiStudioWindow.Instance.InitDialog(this);
        }

        public DialogResult ShowDialog()
        {
            visible = true;
            OnShowDialog();
            FamiStudioWindow.Instance.PushDialog(this);
            
            while (result == DialogResult.None)
                ParentWindow.RunEventLoop(true);

            return result;
        }

        public void ShowDialogAsync(object parent, Action<DialogResult> cb) // MATTT : Remove parent, pass in contructor.
        {
            cb(ShowDialog());
        }

        public void Close(DialogResult res)
        {
            FamiStudioWindow.Instance.PopDialog(this);
            result = res;
            visible = false;
        }

        protected virtual void OnShowDialog()
        {
        }

        public void AddControl(Control ctrl)
        {
            if (!controls.Contains(ctrl))
            {
                controls.Add(ctrl);
                ctrl.ParentWindow = ParentWindow;
                ctrl.ParentDialog = this;
                ctrl.SetDpiScales(DpiScaling.Window, DpiScaling.Font);
                ctrl.SetThemeRenderResource(ThemeResources);
                ctrl.RenderInitialized(ParentWindow.Graphics); 
                ctrl.AddedToDialog();
            }
        }

        public void RemoveControl(Control ctrl)
        {
            if (ctrl != null)
            {
                controls.Remove(ctrl);
                ctrl.RenderTerminated();
            }
        }

        public override void Tick(float delta)
        {
            foreach (var ctrl in controls)
            {
                if (ctrl.Visible)
                {
                    ctrl.Tick(delta);
                }
            }

            tooltipTimer += delta;
        }

        public Control GetControlAtInternal(bool focused, int formX, int formY, out int ctrlX, out int ctrlY)
        {
            ctrlX = 0;
            ctrlY = 0;

            foreach (var ctrl in controls)
            {
                if (ctrl.Visible && ctrl.HasDialogFocus == focused)
                {
                    var dlgX = ctrl.Left - Left;
                    var dlgY = ctrl.Top - Top;

                    if (ctrl.Rectangle.Contains(formX, formY))
                    {
                        ctrlX = formX - ctrl.Left;
                        ctrlY = formY - ctrl.Top;

                        return ctrl;
                    }
                }
            }

            return null;
        }

        public Control GetControlAt(int formX, int formY, out int ctrlX, out int ctrlY)
        {
            var ctrl = GetControlAtInternal(true, formX, formY, out ctrlX, out ctrlY);
            if (ctrl != null)
                return ctrl;
            ctrl = GetControlAtInternal(false, formX, formY, out ctrlX, out ctrlY);
            if (ctrl != null)
                return ctrl;
            return this;
        }

        public void DialogMouseDownNotify(Control control, MouseEventArgs e) 
        {
            ResetToolTip();
        }

        public void DialogMouseMoveNotify(Control control, MouseEventArgs e)
        {
            ResetToolTip();
        }

        private void ResetToolTip()
        {
            if (tooltipTimer > ToolTipDelay)
                MarkDirty();
            tooltipTimer = 0;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            FocusedControl = null;
            ResetToolTip();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            DialogKeyDown?.Invoke(this, e);

            if (focusedControl != null && focusedControl.Visible)
            {
                focusedControl.KeyDown(e);
            }
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            if (focusedControl != null && focusedControl.Visible)
            {
                focusedControl.KeyUp(e);
            }
        }

        protected override void OnChar(CharEventArgs e)
        {
            if (focusedControl != null && focusedControl.Visible)
            {
                focusedControl.Char(e);
            }
        }

        private List<string> SplitLongTooltip(string str)
        {
            var splits = new List<string>();

            if (str.Length > ToolTipMaxCharsPerLine)
            {
                var lastIdx = 0;

                for (var i = ToolTipMaxCharsPerLine - 1; i < str.Length;)
                {
                    if (str[i] == ' ')
                    {
                        splits.Add(str.Substring(lastIdx, i - lastIdx));
                        lastIdx = i + 1;
                        i += ToolTipMaxCharsPerLine;
                    }
                    else
                    {
                        i++;
                    }
                }

                if (lastIdx < str.Length)
                {
                    splits.Add(str.Substring(lastIdx));
                }
            }
            else
            {
                splits.Add(str);
            }

            return splits;
        }

        protected override void OnRender(Graphics g)
        {
            commandList = g.CreateCommandList();
            commandListForeground = g.CreateCommandList();

            // Fill + Border
            commandList.FillAndDrawRectangle(0, 0, width - 1, height - 1, ThemeResources.DarkGreyFillBrush1, ThemeResources.BlackBrush);

            // Render child controls
            foreach (var ctrl in controls)
            {
                if (ctrl.Visible)
                {
                    // MATTT : This is stupid, need DialogLeft too?
                    g.Transform.PushTranslation(ctrl.Left - Left, ctrl.Top - Top);
                    ctrl.Render(g);
                    g.Transform.PopTransform();
                }
            }

            g.DrawCommandList(commandList, Rectangle);
            g.DrawCommandList(commandListForeground);

            commandList = null;
            commandListForeground = null;

            if (tooltipTimer > ToolTipDelay)
            {
                var pt = PointToClient(Cursor.Position);
                var formPt = pt + new Size(left, top);
                var ctrl = GetControlAt(left + pt.X, top + pt.Y, out _, out _);

                if (ctrl != null && !string.IsNullOrEmpty(ctrl.ToolTip))
                {
                    var splits = SplitLongTooltip(ctrl.ToolTip);
                    var sizeX = 0;
                    var sizeY = ThemeResources.FontMedium.LineHeight * splits.Count + tooltipTopMargin;

                    for (int i = 0; i < splits.Count; i++)
                        sizeX = Math.Max(sizeX, ThemeResources.FontMedium.MeasureString(splits[i], false));

                    var totalSizeX = sizeX + tooltipSideMargin * 2;
                    var rightAlign = formPt.X + totalSizeX > ParentWindow.Width;

                    var c = g.CreateCommandList();
                    g.Transform.PushTranslation(pt.X - (rightAlign ? totalSizeX : 0), pt.Y + tooltipOffsetY);

                    for (int i = 0; i < splits.Count; i++)
                        c.DrawText(splits[i], ThemeResources.FontMedium, tooltipSideMargin, i * ThemeResources.FontMedium.LineHeight + tooltipTopMargin, ThemeResources.LightGreyFillBrush1);

                    c.FillAndDrawRectangle(0, 0, totalSizeX, sizeY, ThemeResources.DarkGreyLineBrush1, ThemeResources.LightGreyFillBrush1);
                    g.Transform.PopTransform();
                    g.DrawCommandList(c);
                }
            }
        }
    }
}
