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
using RenderTheme       = FamiStudio.ThemeRenderResources;
using RenderCommandList = FamiStudio.GLCommandList;
using RenderTransform   = FamiStudio.GLTransform;

namespace FamiStudio
{
    public class Toolbar : ToolbarBase
    {
        // DROIDTODO : Migrate this to like the quick access bar.
        const float DefaultButtonMargin = 0.1f;

        private struct ButtonLayoutItem
        {
            public ButtonLayoutItem(int r, int c, ButtonType b)
            {
                row = r;
                col = c;
                btn = b;
            }
            public int row;
            public int col;
            public ButtonType btn;
        };

        private struct OscTimeLayoutItem
        {
            public OscTimeLayoutItem(int r, int c, int nc)
            {
                row = r;
                col = c;
                numCols = nc;
            }
            public int row;
            public int col;
            public int numCols;
        };

        private readonly ButtonLayoutItem[] ButtonLayout = new ButtonLayoutItem[]
        { 
            new ButtonLayoutItem(0,  0, ButtonType.Open),
            new ButtonLayoutItem(0,  1, ButtonType.Copy),
            new ButtonLayoutItem(0,  2, ButtonType.Undo),
            new ButtonLayoutItem(0,  3, ButtonType.Config),
            new ButtonLayoutItem(0,  6, ButtonType.Play),
            new ButtonLayoutItem(0,  7, ButtonType.Rec),
            new ButtonLayoutItem(0,  8, ButtonType.Help),

            new ButtonLayoutItem(1,  0, ButtonType.Save),
            new ButtonLayoutItem(1,  1, ButtonType.Paste),
            new ButtonLayoutItem(1,  2, ButtonType.Redo),
            new ButtonLayoutItem(1,  3, ButtonType.Transform),
            new ButtonLayoutItem(1,  6, ButtonType.Rewind),
            new ButtonLayoutItem(1,  7, ButtonType.Qwerty),
            new ButtonLayoutItem(1,  8, ButtonType.More),

            new ButtonLayoutItem(2,  0, ButtonType.New),
            new ButtonLayoutItem(2,  1, ButtonType.Cut),
            new ButtonLayoutItem(2,  2, ButtonType.Count),
            new ButtonLayoutItem(2,  3, ButtonType.Count),
            new ButtonLayoutItem(2,  6, ButtonType.Loop),
            new ButtonLayoutItem(2,  7, ButtonType.Metronome),
            new ButtonLayoutItem(2,  8, ButtonType.Count),

            new ButtonLayoutItem(3,  0, ButtonType.Export),
            new ButtonLayoutItem(3,  1, ButtonType.Count), // MATTT : Delete
            new ButtonLayoutItem(3,  2, ButtonType.Count),
            new ButtonLayoutItem(3,  3, ButtonType.Count),
            new ButtonLayoutItem(3,  6, ButtonType.Machine),
            new ButtonLayoutItem(3,  7, ButtonType.Follow),
            new ButtonLayoutItem(3,  8, ButtonType.Count),
        };

        // [portrait/landscape, timecode/oscilloscope]
        private readonly OscTimeLayoutItem[,] OscTimeLayout = new OscTimeLayoutItem[,]
        {
            {
                new OscTimeLayoutItem(0, 4, 2),
                new OscTimeLayoutItem(1, 4, 2),
            },
            {
                new OscTimeLayoutItem(0, 4, 2),
                new OscTimeLayoutItem(0, 5, 2),
            }
        };

        // These are calculated on render init.
        private int buttonMargin;
        private int buttonSizeFull;
        private int buttonSize;

        private float expandRatio = 0.0f;
        private bool  expanded = false; // MATTT : Testing.

        public int   LayoutSize  => buttonSizeFull * 2;
        public int   RenderSize  => (int)Math.Round(LayoutSize * (1.0f + Utils.SmootherStep(expandRatio)));
        public float ExpandRatio => expandRatio;
        public bool  IsExpanded  => expandRatio > 0.001f;

        protected override void OnRenderInitialized(RenderGraphics g)
        {
            base.OnRenderInitialized(g);

            var bitmapSize = bmpButtonAtlas.GetElementSize(0);

            buttonSizeFull = Math.Min(ParentFormSize.Width, ParentFormSize.Height) / 9;
            buttonMargin = (int)Math.Round(buttonSizeFull * 0.1f);
            buttonSize = buttonSizeFull - (buttonMargin * 2);
            buttonBitmapScaleFloat = buttonSize / (float)(bitmapSize.Width);

            UpdateButtonLayout();
        }

        protected override void UpdateButtonLayout()
        {
            if (!IsRenderInitialized)
                return;

            var landscape = IsLandscape;

            foreach (var btn in buttons)
                btn.Visible = false;

            var numRows = expanded ? 4 : 2;

            foreach (var bl in ButtonLayout)
            {
                if (bl.btn == ButtonType.Count)
                    continue;

                var btn = buttons[(int)bl.btn];
                
                var col = bl.col;
                var row = bl.row;

                if (row >= numRows)
                    continue;

                if (landscape)
                    Utils.Swap(ref col, ref row);

                btn.X = buttonSizeFull * col + buttonMargin;
                btn.Y = buttonSizeFull * row + buttonMargin;
                btn.Size = buttonSize; // DROIDTODO : Hitbox.
                btn.Visible = true;
            }

            var timeLayout = OscTimeLayout[landscape ? 1 : 0, 0];
            var oscLayout  = OscTimeLayout[landscape ? 1 : 0, 1];

            Debug.Assert(timeLayout.numCols == oscLayout.numCols);

            var timeCol = timeLayout.col;
            var timeRow = timeLayout.row;
            var oscCol = oscLayout.col;
            var oscRow = oscLayout.row;

            if (landscape)
            {
                Utils.Swap(ref timeCol, ref timeRow);
                Utils.Swap(ref oscCol, ref oscRow);
            }

            timecodeOscSizeX = timeLayout.numCols * buttonSizeFull - buttonMargin * 2;
            timecodeOscSizeY = buttonSize;
            timecodePosX = buttonMargin + timeCol * buttonSizeFull;
            timecodePosY = buttonMargin + timeRow * buttonSizeFull;
            oscilloscopePosX = buttonMargin + oscCol * buttonSizeFull;
            oscilloscopePosY = buttonMargin + oscRow * buttonSizeFull;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            expandRatio = expanded ? 1.0f : 0.0f;
        }

        protected override void OnMore()
        {
            expanded = !expanded;
            ConditionalInvalidate();
        }

        public void Tick(float delta)
        {
            delta *= 6.0f;

            var prevRatio = expandRatio;

            if (expanded && expandRatio < 1.0f)
            {
                expandRatio = Math.Min(1.0f, expandRatio + delta);
                if (prevRatio < 0.5f && expandRatio >= 0.5f)
                    UpdateButtonLayout();
            }
            else if (!expanded && expandRatio > 0.0f)
            {
                expandRatio = Math.Max(0.0f, expandRatio - delta);
                if (prevRatio > 0.5f && expandRatio <= 0.5f)
                    UpdateButtonLayout();
            }
        }

        protected override void Clear(RenderCommandList c)
        {
            // MATTT : Toolbar brush.
            var brush = c.Graphics.GetSolidBrush(Theme.DarkGreyFillColor1);

            if (IsLandscape)
                c.FillRectangle(0, 0, RenderSize, Height, brush);
            else
                c.FillRectangle(0, 0, Width, RenderSize, brush);
        }

        protected override void PostRender(RenderCommandList c)
        {
            if (IsLandscape)
                c.DrawLine(Width - 1, 0, Width - 1, Height, ThemeResources.BlackBrush);
            else
                c.DrawLine(0, Height - 1, Width, Height - 1, ThemeResources.BlackBrush);
        }

        public void Reset()
        {
        }

        // TEMPORARY!
        protected override void OnTouchDown(int x, int y)
        {
            //bool left  = e.Button.HasFlag(MouseButtons.Left);
            //bool right = e.Button.HasFlag(MouseButtons.Right);

            //if (left || right)
            {
                /*
                if (x > timecodePosX && x < timecodePosX + timecodeOscSizeX &&
                    y > timecodePosY && y < Height - timecodePosY)
                {
                    Settings.TimeFormat = Settings.TimeFormat == 0 ? 1 : 0;
                    ConditionalInvalidate();
                }
                else
                */
                {
                    foreach (var btn in buttons)
                    {
                        if (btn != null && btn.Visible && btn.IsPointIn(x, y, Width) && (btn.Enabled == null || btn.Enabled() != ButtonStatus.Disabled))
                        {
                            //if (left)
                                btn.Click?.Invoke();
                            //else
                            //    btn.RightClick?.Invoke();
                            break;
                        }
                    }
                }
            }
        }
    }
}
