using System;
using System.Drawing;
using System.Collections.Generic;

namespace FamiStudio
{
    public class Dialog : Control
    {
        const float ToolTipDelay = 0.2f;
        const int   ToolTipMaxCharsPerLine = 64;

        private List<Control> controls = new List<Control>();
        private Control focusedControl;
        private Action<DialogResult2> callback;
        private DialogResult2 result = DialogResult2.None;
        private float tooltipTimer;

        private int tooltipTopMargin  = DpiScaling.ScaleForMainWindow(2);
        private int tooltipSideMargin = DpiScaling.ScaleForMainWindow(4);
        private int tooltipOffsetY = DpiScaling.ScaleForMainWindow(24);

        private CommandList commandList;
        private CommandList commandListForeground;

        public CommandList CommandList => commandList;
        public CommandList CommandListForeground => commandListForeground;
        public DialogResult2 DialogResult2 => result;

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
            FamiStudioForm.Instance.InitDialog(this);
        }

        public void ShowDialogAsync(object parent, Action<DialogResult2> cb) // MATTT : Remove parent, pass in contructor.
        {
            visible = true;
            callback = cb;
            OnShowDialog();
            FamiStudioForm.Instance.PushDialog(this);
        }

        public void Close(DialogResult2 res)
        {
            FamiStudioForm.Instance.PopDialog(this);
            result = res;
            visible = false;
            callback(result);
        }

        protected virtual void OnShowDialog()
        {
        }

        public void AddControl(Control ctrl)
        {
            if (!controls.Contains(ctrl))
            {
                controls.Add(ctrl);
                ctrl.ParentForm = ParentForm;
                ctrl.ParentDialog = this;
                ctrl.SetDpiScales(DpiScaling.Window, DpiScaling.Font);
                ctrl.SetThemeRenderResource(ThemeResources);
                ctrl.RenderInitialized(ParentForm.Graphics); 
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
            if (focusedControl != null && focusedControl.Visible)
            {
                focusedControl.KeyDown(e);
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
                    var rightAlign = formPt.X + totalSizeX > ParentForm.Width;

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
