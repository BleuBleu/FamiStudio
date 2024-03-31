using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace FamiStudio
{
    public class Dialog : Container
    {
        public delegate void KeyDownDelegate(Dialog dlg, KeyEventArgs e);
        public event KeyDownDelegate DialogKeyDown;

        const float ToolTipDelay = 0.2f;
        const int   ToolTipMaxCharsPerLine = 64;

        private DialogResult result = DialogResult.None;
        private float tooltipTimer;
        private string title = "";
        private Control focusedControl;

        private int tooltipTopMargin  = DpiScaling.ScaleForWindow(2);
        private int tooltipSideMargin = DpiScaling.ScaleForWindow(4);
        private int tooltipOffsetY = DpiScaling.ScaleForWindow(24);
        
        protected int titleBarMargin = DpiScaling.ScaleForWindow(8);
        protected int titleBarSizeY;

        public DialogResult DialogResult => result;

        public Control FocusedControl
        {
            get { return focusedControl; }
            set 
            {
                if (value == focusedControl || (value != null && !value.CanFocus))
                    return;

                if (focusedControl != null)
                    focusedControl.LostDialogFocus();

                focusedControl = value;
                
                if (focusedControl != null)
                    focusedControl.AcquiredDialogFocus();

                MarkDirty();
            }
        }

        public string Title
        {
            get { return title; }
            set 
            {
                if (SetAndMarkDirty(ref title, value))
                    titleBarSizeY = string.IsNullOrEmpty(title) ? 0 : DpiScaling.ScaleForWindow(24);
            }
        }

        public Dialog(FamiStudioWindow win, string t = "")
        {
            visible = false;
            Title = t;
            win.InitDialog(this);
        }

        private void ShowDialogInternal()
        {
            result = DialogResult.None;
            visible = true;
            OnShowDialog();
            window.PushDialog(this);
        }

        public void ShowDialogNonModal()
        {
            ShowDialogInternal();
        }

        public DialogResult ShowDialog()
        {
            ShowDialogInternal();

            while (result == DialogResult.None)
                ParentWindow.RunEventLoop(true);

            return result;
        }

        public void ShowDialogAsync(Action<DialogResult> cb)
        {
            // Dialogs are not async on desktop.
            cb(ShowDialog());
        }

        public void Close(DialogResult res)
        {
            FocusedControl = null;
            window.PopDialog(this);
            result = res;
            visible = false;
        }

        protected virtual void OnShowDialog()
        {
        }

        public override void Tick(float delta)
        {
            base.Tick(delta);

            var v1 = ShouldDisplayTooltip();
            tooltipTimer += delta;
            var v2 = ShouldDisplayTooltip();

            if (!v1 && v2)
                MarkDirty();
        }

        private bool ShouldDisplayTooltip()
        {
            return window.TopDialog == this && tooltipTimer > ToolTipDelay;
        }

        public override void ContainerMouseDownNotify(Control control, MouseEventArgs e) 
        {
            ResetToolTip();
        }

        public override void ContainerMouseMoveNotify(Control control, MouseEventArgs e)
        {
            ResetToolTip();
        }

        private void ResetToolTip()
        {
            if (ShouldDisplayTooltip())
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
               focusedControl.KeyDown(e);
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

        public override Control GetControlAt(int winX, int winY, out int ctrlX, out int ctrlY)
        {
            // Focus control gets to check first. This is needed to handle drop downs
            // that extends beyond the bounding of a dialog, for example.
            if (focusedControl != null && focusedControl.HitTest(winX, winY))
            {
                Debug.Assert(focusedControl.Visible);
                var winPos = focusedControl.WindowPosition;
                ctrlX = winX - winPos.X;
                ctrlY = winY - winPos.Y;
                return focusedControl;
            }

            return base.GetControlAt(winX, winY, out ctrlX, out ctrlY);
        }

        public override bool HitTest(int winX, int winY)
        {
            return true; // The top dialog eats all the input.
        }

        private List<string> SplitLongString(string str)
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

        private List<string> SplitLongTooltip(string str)
        {
            var lines = str.Split(new[] { '\n' });
            var splits = new List<string>();

            foreach (var l in lines)
                splits.AddRange(SplitLongString(l));

            return splits;
        }

        protected override void OnRender(Graphics g)
        {
            var c = g.DefaultCommandList;
            var fonts = Fonts;

            // Fill + Border
            c.FillAndDrawRectangle(0, 0, width - 1, height - 1, Theme.DarkGreyColor4, Theme.BlackColor);

            if (titleBarSizeY > 0)
            {
                c.FillAndDrawRectangle(0, 0, width, titleBarSizeY, Theme.DarkGreyColor1, Theme.BlackColor);
                c.DrawText(title, fonts.FontMediumBold, titleBarMargin, 0, Theme.LightGreyColor1, TextFlags.MiddleLeft, 0, titleBarSizeY);
            }

            if (ShouldDisplayTooltip())
            {
                var o = g.Overlay2CommandList;
                var pt = ScreenToControl(CursorPosition);
                var formPt = pt + new Size(left, top);
                var ctrl = GetControlAt(left + pt.X, top + pt.Y, out _, out _);

                if (ctrl != null && !string.IsNullOrEmpty(ctrl.ToolTip))
                {
                    var splits = SplitLongTooltip(ctrl.ToolTip);
                    var sizeX = 0;
                    var sizeY = fonts.FontMedium.LineHeight * splits.Count + tooltipTopMargin * 2;

                    for (int i = 0; i < splits.Count; i++)
                        sizeX = Math.Max(sizeX, fonts.FontMedium.MeasureString(splits[i], false));

                    var totalSizeX = sizeX + tooltipSideMargin * 2;
                    var rightAlign = formPt.X + totalSizeX > ParentWindow.Width;

                    g.Transform.PushTranslation(pt.X - (rightAlign ? totalSizeX : 0), pt.Y + tooltipOffsetY);

                    for (int i = 0; i < splits.Count; i++)
                        o.DrawText(splits[i], Fonts.FontMedium, tooltipSideMargin, i * fonts.FontMedium.LineHeight + tooltipTopMargin, Theme.LightGreyColor1);

                    o.FillAndDrawRectangle(0, 0, totalSizeX, sizeY, Theme.DarkGreyColor1, Theme.LightGreyColor1);
                    g.Transform.PopTransform();
                }
            }

            base.OnRender(g);
        }
    }
}
