using System;
using System.Diagnostics;
using System.Windows.Forms;

using RenderBitmapAtlas = FamiStudio.GLBitmapAtlas;
using RenderControl     = FamiStudio.GLControl;
using RenderGraphics    = FamiStudio.GLGraphics;
using RenderTheme       = FamiStudio.ThemeRenderResources;

namespace FamiStudio
{
    public partial class NavigationBar : RenderControl
    {
        const int DefaultButtonMargin = 16; // DROIDTODO : Use float.

        enum ButtonType
        {
            Sequencer, // Mobile only
            PianoRoll, // Mobile only
            Project, // Mobile only
            Count
        }

        enum ButtonImageIndices
        { 
            Sequencer,
            PianoRoll,
            ProjectExplorer,
            Count
        };

        readonly string[] ButtonImageNames = new string[]
        {
            "Sequencer",
            "PianoRoll",
            "ProjectExplorer"
        };

        public delegate void EmptyDelegate();

        class Button
        {
            public int x;
            public int y;
            public EmptyDelegate Click;
        };

        public event EmptyDelegate SequencerClicked;
        public event EmptyDelegate PianoRollClicked;
        public event EmptyDelegate ProjectExplorerClicked;

        RenderBitmapAtlas bmpButtonAtlas;
        Button[] buttons = new Button[(int)ButtonType.Count];

        int   buttonSizeFull;
        int   buttonSize;
        int   buttonMargin;
        float buttonBitmapScaleFloat = 1.0f;

        public int DesiredSize => buttonSizeFull;

        protected override void OnRenderInitialized(RenderGraphics g)
        {
            Debug.Assert((int)ButtonImageIndices.Count == ButtonImageNames.Length);

            bmpButtonAtlas = g.CreateBitmapAtlasFromResources(ButtonImageNames);

            buttons[(int)ButtonType.Sequencer] = new Button { Click = OnSequencer };
            buttons[(int)ButtonType.PianoRoll] = new Button { Click = OnPianoRoll };
            buttons[(int)ButtonType.Project]   = new Button { Click = OnProjectExplorer };

            var bitmapSize = bmpButtonAtlas.GetElementSize(0);

            buttonSizeFull = MobileUtils.ComputeIdealButtonSize(ParentFormSize.Width, ParentFormSize.Height);
            buttonMargin = DefaultButtonMargin;// MATTT scale or something.
            buttonSize = buttonSizeFull - buttonMargin * 2; 
            buttonBitmapScaleFloat = buttonSize / (float)(bitmapSize.Width);
        }

        protected override void OnRenderTerminated()
        {
            Utils.DisposeAndNullify(ref bmpButtonAtlas);
        }

        protected override void OnResize(EventArgs e)
        {
            UpdateButtonLayout();
            base.OnResize(e);
        }

        private void UpdateButtonLayout()
        {
            if (!IsRenderInitialized)
                return;

            var landscape = IsLandscape;

            // Special case for the 3 navigation buttons.
            for (int i = 0; i < (int)ButtonType.Count; i++)
            {
                var btn = buttons[i];
                
                if (landscape)
                {
                    btn.x = buttonMargin;
                    btn.y = Height / 4 * (i + 1) - buttonSize / 2;
                }
                else
                {
                    btn.x = Width / 4 * (i + 1) - buttonSize / 2;
                    btn.y = buttonMargin;
                }
            }
        }

        public void ConditionalInvalidate()
        {
            if (App != null && !App.RealTimeUpdate)
                Invalidate();
        }

        private void OnSequencer()
        {
            SequencerClicked?.Invoke();
        }

        private void OnPianoRoll()
        {
            PianoRollClicked?.Invoke();
        }

        private void OnProjectExplorer()
        {
            ProjectExplorerClicked?.Invoke();
        }

        protected override void OnRender(RenderGraphics g)
        {
            var c = g.CreateCommandList(); 

            c.FillRectangle(0, 0, Width, Height, ThemeResources.DarkGreyLineBrush1);

            // Buttons
            for (int i = 0; i < (int)ButtonType.Count; i++)
            {
                var btn = buttons[i];

                // DROIDTODO : Make the active control slightly brighter.
                //var opacity = status == ButtonStatus.Enabled ? 1.0f : 0.25f;

                c.DrawBitmapAtlas(bmpButtonAtlas, i, btn.x, btn.y, 1.0f, buttonBitmapScaleFloat);
            }

            c.DrawLine(Width, 0, Width, Height, ThemeResources.BlackBrush, 5.0f); // DROIDTODO : Line width!
            g.DrawCommandList(c);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            var pos    = IsLandscape ? e.Y    : e.X;
            var size   = IsLandscape ? Height : Width;
            var margin = size / 8;

            if (pos >= margin && pos < Height - margin)
            {
                var idx = (pos - margin) / (size / 4);

                System.Diagnostics.Debug.WriteLine($"BUTTON {idx}");

                if (idx >= 0 && idx <= 2)
                    buttons[idx].Click();
            }
        }
    }
}
