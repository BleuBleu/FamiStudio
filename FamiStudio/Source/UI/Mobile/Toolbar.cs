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
        // DROIDTODO : Review once we have figure out scale. Use float.
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

        // [portrait/landscape, collapsed/expanded ]
        private readonly ButtonLayoutItem[,][] ButtonLayout = new ButtonLayoutItem[2, 2][]
        { 
            {
                new ButtonLayoutItem[] 
                {
                    new ButtonLayoutItem(0,  0, ButtonType.Open),
                    new ButtonLayoutItem(0,  1, ButtonType.Copy),
                    new ButtonLayoutItem(0,  2, ButtonType.Undo),
                    new ButtonLayoutItem(0,  3, ButtonType.Config),
                    new ButtonLayoutItem(0,  6, ButtonType.Play),
                    new ButtonLayoutItem(0,  7, ButtonType.Help),

                    new ButtonLayoutItem(1,  0, ButtonType.Save),
                    new ButtonLayoutItem(1,  1, ButtonType.Paste),
                    new ButtonLayoutItem(1,  2, ButtonType.Redo),
                    new ButtonLayoutItem(1,  3, ButtonType.Transform),
                    new ButtonLayoutItem(1,  6, ButtonType.Rewind),
                    new ButtonLayoutItem(1,  7, ButtonType.More),
                },
                new ButtonLayoutItem[] 
                {
                },
            }, 
            {
                new ButtonLayoutItem[] 
                {
                    new ButtonLayoutItem(0,  0, ButtonType.Open),
                    new ButtonLayoutItem(0,  1, ButtonType.Save),
                    new ButtonLayoutItem(0,  2, ButtonType.Copy),
                    new ButtonLayoutItem(0,  3, ButtonType.Paste),
                    new ButtonLayoutItem(0,  4, ButtonType.Undo),
                    new ButtonLayoutItem(0,  5, ButtonType.Redo),
                    new ButtonLayoutItem(0,  6, ButtonType.Config),
                    new ButtonLayoutItem(0, 11, ButtonType.Play),
                    new ButtonLayoutItem(0, 12, ButtonType.Rewind),
                    new ButtonLayoutItem(0, 13, ButtonType.Help),
                    new ButtonLayoutItem(0, 14, ButtonType.More)

                },
                new ButtonLayoutItem[] 
                {
                    new ButtonLayoutItem(0,  0, ButtonType.New),
                    new ButtonLayoutItem(0,  1, ButtonType.Open),
                    new ButtonLayoutItem(0,  2, ButtonType.Copy),
                    new ButtonLayoutItem(0,  3, ButtonType.Paste),
                    new ButtonLayoutItem(0,  4, ButtonType.Undo),
                    new ButtonLayoutItem(0,  5, ButtonType.Config),
                    new ButtonLayoutItem(0, 10, ButtonType.Play),
                    new ButtonLayoutItem(0, 11, ButtonType.Rewind),
                    new ButtonLayoutItem(0, 12, ButtonType.Metronome),
                    new ButtonLayoutItem(0, 13, ButtonType.Follow),
                    new ButtonLayoutItem(0, 14, ButtonType.Help),

                    new ButtonLayoutItem(1,  0, ButtonType.Save),
                    new ButtonLayoutItem(1,  1, ButtonType.Export),
                    new ButtonLayoutItem(1,  2, ButtonType.Cut),
                    new ButtonLayoutItem(1,  3, ButtonType.Cut), // DROIDTODO : Delete!
                    new ButtonLayoutItem(1,  4, ButtonType.Redo),
                    new ButtonLayoutItem(1,  5, ButtonType.Transform),
                    new ButtonLayoutItem(1, 10, ButtonType.Rec),
                    new ButtonLayoutItem(1, 11, ButtonType.Loop),
                    new ButtonLayoutItem(1, 12, ButtonType.Machine),
                    new ButtonLayoutItem(1, 14, ButtonType.More),
                },
            }, 
        };

        // [portrait/landscape, collapsed/expanded, timecode/oscilloscope ]
        private readonly OscTimeLayoutItem[,,] OscTimeLayout = new OscTimeLayoutItem[2, 2, 2]
        {
            {
                {
                    new OscTimeLayoutItem(0, 4, 2),
                    new OscTimeLayoutItem(1, 4, 2),
                },
                {
                    new OscTimeLayoutItem(0, 0, 2),
                    new OscTimeLayoutItem(0, 0, 2),
                },
            },
            {
                {
                    new OscTimeLayoutItem(0, 7, 2),
                    new OscTimeLayoutItem(0, 9, 2),
                },
                {
                    new OscTimeLayoutItem(0, 6, 2),
                    new OscTimeLayoutItem(0, 8, 2),
                },
            },
        };

        // These are calculated on render init.
        private int buttonMargin;
        private int buttonSizeFull;
        private int buttonSize;

        private float expandRatio = 0.0f;
        private bool  expanded = false; // MATTT : Testing.

        public int   LayoutSize  => buttonSizeFull * (IsLandscape ? 1 : 2);
        public int   DesiredSize => (int)Math.Round(LayoutSize * (1.0f + expandRatio));
        public float ExpandRatio => expandRatio;

        protected override void OnRenderInitialized(RenderGraphics g)
        {
            base.OnRenderInitialized(g);

            var bitmapSize = bmpButtonAtlas.GetElementSize(0);

            buttonSizeFull = MobileUtils.ComputeIdealButtonSize(ParentFormSize.Width, ParentFormSize.Height);
            buttonMargin = (int)Math.Round(buttonSizeFull * 0.1f);  // DROIDTODO : Apply some form of scaling.
            buttonSize = buttonSizeFull - (buttonMargin * 2);
            buttonBitmapScaleFloat = buttonSize / (float)(bitmapSize.Width);

            UpdateButtonLayout();
        }

        protected override void UpdateButtonLayout()
        {
            if (theme == null)
                return;

            var landscape = IsLandscape;
            var layout = ButtonLayout[landscape ? 1 : 0, expanded ? 1 : 0];

            foreach (var btn in buttons)
            {
                btn.Visible = false;
            }

            foreach (var bl in layout)
            {
                var btn = buttons[(int)bl.btn];

                btn.X = buttonSizeFull * bl.col + buttonMargin;
                btn.Y = buttonSizeFull * bl.row + buttonMargin;
                btn.Size = buttonSize; // DROIDTODO : Hitbox.
                btn.Visible = true;
            }

            var timeLayout = OscTimeLayout[landscape ? 1 : 0, expanded ? 1 : 0, 0];
            var oscLayout  = OscTimeLayout[landscape ? 1 : 0, expanded ? 1 : 0, 1];

            Debug.Assert(timeLayout.numCols == oscLayout.numCols);

            timecodeOscSizeX = timeLayout.numCols * buttonSizeFull - buttonMargin * 2;
            timecodeOscSizeY = buttonSize;
            timecodePosX = buttonMargin + timeLayout.col * buttonSizeFull;
            timecodePosY = buttonMargin + timeLayout.row * buttonSizeFull;
            oscilloscopePosX = buttonMargin + oscLayout.col * buttonSizeFull;
            oscilloscopePosY = buttonMargin + oscLayout.row * buttonSizeFull;
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

        public void Reset()
        {
        }

        // TEMPORARY!
        protected override void OnMouseDown(MouseEventArgs e)
        {
            bool left  = e.Button.HasFlag(MouseButtons.Left);
            bool right = e.Button.HasFlag(MouseButtons.Right);

            if (left || right)
            {
                if (e.X > timecodePosX && e.X < timecodePosX + timecodeOscSizeX &&
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
