using System;
using System.Collections.Generic;
using System.Media;
using System.Windows.Forms;
using System.Diagnostics;

using RenderBitmap      = FamiStudio.GLBitmap;
using RenderBitmapAtlas = FamiStudio.GLBitmapAtlas;
using RenderBrush       = FamiStudio.GLBrush;
using RenderGeometry    = FamiStudio.GLGeometry;
using RenderControl     = FamiStudio.GLControl;
using RenderGraphics    = FamiStudio.GLGraphics;
using RenderTheme       = FamiStudio.GLTheme;
using RenderCommandList = FamiStudio.GLCommandList;
using RenderTransform   = FamiStudio.GLTransform;

namespace FamiStudio
{
    public partial class Toolbar : RenderControl
    {
        // DROIDTODO : Review once we have figure out scale.
        const int DefaultButtonMargin = 16;

        // These are calculated on render init.
        int buttonMargin;
        int buttonFullSize;
        int buttonSize;
        int buttonInnerSize;
        int expandButtonSize;
        int timecodeSizeX;
        float buttonSizeFloat;

        // These are calculated when the orientation changes.
        int timecodePosX;
        int timecodePosY;
        int oscilloscopePosX;
        int oscilloscopePosY;

        protected override void OnRenderInitialized(RenderGraphics g)
        {
            OnRenderInitializedCommon(g);

            var scaling = RenderTheme.MainWindowScaling;

            var screenSize = ParentFormSize;
            var minSize = Math.Min(screenSize.Width, screenSize.Height);
            var numRows = (int)ButtonCategory.RowCount + 2; // 2 = timecode + oscilloscope.
            var bitmapSize = bmpButtonAtlas.GetElementSize(0);

            buttonSizeFloat = minSize / (float)numRows;
            buttonMargin = DefaultButtonMargin;  // DROIDTODO : Apply some form of scaling.
            buttonFullSize = (int)buttonSizeFloat;
            buttonSize = buttonFullSize - (buttonMargin * 2);
            expandButtonSize = buttonFullSize / 2;
            buttonBitmapScaleFloat = buttonSize / (float)(bitmapSize.Width);
            timecodeSizeX = buttonFullSize * 2 - buttonMargin * 2;

            // HACK : Disable right-alignment.
            buttons[(int)ButtonType.Help].RightAligned = false;

            UpdateButtonLayout();
        }

        protected override void OnRenderTerminated()
        {
            OnRenderTerminatedCommon();
        }

        private void UpdateButtonLayout()
        {
            if (theme == null)
                return;

            var landscape = IsLandscape;

            // Main buttons.
            for (int j = 0; j < (int)ButtonCategory.RowCount; j++)
            {
                var x = buttonMargin + expandButtonSize;
                var y = buttonMargin + (j * buttonFullSize);

                for (int i = 0; i < (int)ButtonType.Count; i++)
                {
                    var btn = buttons[i];

                    if ((int)btn.Category == j)
                    {
                        if (btn.Important)
                        {
                            btn.X = x;
                            btn.Y = y;
                            btn.Size = buttonSize;

                            if (!landscape)
                                Utils.Swap(ref btn.X, ref btn.Y);

                            btn.Visible = true;
                            x += buttonFullSize;
                        }
                        else
                        {
                            btn.Visible = false;
                        }
                    }
                }
            }

            // Special case for the 3 navigation buttons.
            for (int i = 0; i < 3; i++)
            {
                var x = buttonMargin + expandButtonSize + buttonFullSize * 2;
                var y = Height / 4 * (i + 1) - buttonSize / 2; // DROIDTODO : Height here.
                var btn = buttons[(int)ButtonType.Sequencer + i];

                btn.X = x;
                btn.Y = y;
                btn.Size = buttonSize;
                btn.Visible = true;

                if (!landscape)
                    Utils.Swap(ref btn.X, ref btn.Y);
            }

            // Expand button.

            timecodePosX = buttonMargin + expandButtonSize;
            timecodePosY = buttonMargin + (((int)ButtonCategory.RowCount + 0) * buttonFullSize);
            oscilloscopePosX = buttonMargin + expandButtonSize;
            oscilloscopePosY = buttonMargin + (((int)ButtonCategory.RowCount + 1) * buttonFullSize);

            if (!landscape)
            {
                Utils.Swap(ref timecodePosX, ref timecodePosY);
                Utils.Swap(ref oscilloscopePosX, ref oscilloscopePosY);
            }
        }

        public void SetToolTip(string msg, bool red = false)
        {
        }

        public void DisplayWarning(string msg, bool beep)
        {
            // DROIDTODO : Use a toast here!
            // SystemSounds.Beep.Play();
        }

        public void Tick()
        {
        }

        public void Reset()
        {
        }

        private void RenderNavigationBackground(RenderGraphics g, RenderCommandList c)
        {
            c.FillRectangle(Width - buttonFullSize, 0, Width, Height, theme.DarkGreyLineBrush1);
        }

        private void RenderExpandButton(RenderGraphics g, RenderCommandList c)
        {
            // Manually fetching UVs to be able to flip them.
            bmpButtonAtlas.GetElementUVs((int)ButtonImageIndices.ExpandToolbar, out var u0, out var v0, out var u1, out var v1);
            c.DrawBitmap(bmpButtonAtlas, 0, Height / 2 - buttonFullSize / 4, buttonFullSize / 2, buttonFullSize / 2, 1.0f, u0, v0, u1, v1); // DROIDTODO : Height
            c.DrawLine(0, 0, 0, Height, theme.BlackBrush, 5.0f); // DROIDTODO : Height + line size.
        }

        protected override void OnRender(RenderGraphics g)
        {
            var c = g.CreateCommandList(); // Main
            //var ct = g.CreateCommandList(); // Tooltip (clipped)

            // Prepare the batches.
            RenderButtons(g, c);
            RenderNavigationBackground(g, c);
            RenderExpandButton(g, c);
            //RenderTimecode(g, cm);
            RenderOscilloscope(g, c, timecodePosX, timecodePosY, timecodeSizeX, buttonSize); // MATTT
            RenderOscilloscope(g, c, oscilloscopePosX, oscilloscopePosY, timecodeSizeX, buttonSize);

            // Draw everything.
            g.DrawCommandList(c);
            //g.DrawCommandList(ct, new System.Drawing.Rectangle(lastButtonX, 0, Width, Height));
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            ConditionalInvalidate();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            ConditionalInvalidate();

            foreach (var btn in buttons)
            {
                if (btn.Visible && btn.IsPointIn(e.X, e.Y, Width))
                {
                    SetToolTip(btn.ToolTip);
                    return;
                }
            }

            SetToolTip("");
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);

            foreach (var btn in buttons)
            {
                if (btn != null && btn.Visible && btn.IsPointIn(e.X, e.Y, Width) && (btn.Enabled == null || btn.Enabled() != ButtonStatus.Disabled))
                {
                    btn.MouseWheel?.Invoke(e.Delta);
                    break;
                }
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            bool left = e.Button.HasFlag(MouseButtons.Left);
            bool right = e.Button.HasFlag(MouseButtons.Right);

            if (left || right)
            {
                if (e.X > timecodePosX && e.X < timecodePosX + timecodeSizeX &&
                    e.Y > timecodePosY && e.Y < Height - timecodePosY)
                {
                    Settings.TimeFormat = Settings.TimeFormat == 0 ? 1 : 0;
                    ConditionalInvalidate();
                }
                else
                {
                    foreach (var btn in buttons)
                    {
                        if (btn != null && btn.Visible && btn.IsPointIn(e.X, e.Y, Width) && (btn.Enabled == null || btn.Enabled() != ButtonStatus.Disabled))
                        {
                            if (left)
                                btn.Click?.Invoke();
                            else
                                btn.RightClick?.Invoke();
                            break;
                        }
                    }
                }
            }
        }
    }
}
