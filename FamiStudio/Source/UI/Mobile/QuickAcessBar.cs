using System;
using System.Diagnostics;
using System.Windows.Forms;

using Rectangle = System.Drawing.Rectangle;

using RenderBitmapAtlas = FamiStudio.GLBitmapAtlas;
using RenderControl     = FamiStudio.GLControl;
using RenderGraphics    = FamiStudio.GLGraphics;
using RenderBrush       = FamiStudio.GLBrush;
using RenderFont        = FamiStudio.GLFont;
using RenderTheme       = FamiStudio.ThemeRenderResources;

namespace FamiStudio
{
    public partial class QuickAcessBar : RenderControl
    {
        const float ButtonMargin = 0.1f;

        private delegate ButtonImageIndices RenderInfoDelegate(RenderGraphics g, out string text, out RenderBrush background);
        private delegate bool EnabledDelegate();

        private class Button
        {
            public Rectangle Hitbox;
            public int IconX;
            public int IconY;
            public int TextY;
            public bool IsNavButton = false;
            public RenderBrush BackgroundBrush;
            public RenderInfoDelegate GetRenderInfo;
            public EmptyDelegate Click;
        }

        private enum ButtonType
        {
            Sequencer,
            PianoRoll,
            Project,
            Tool,
            Snap,
            Channel,
            Instrument,
            Arpeggio,
            Count
        }

        private enum ButtonImageIndices
        { 
            Sequencer,
            PianoRoll,
            ProjectExplorer,
            MobileSnapOn,
            MobileSnapOff,
            MobileChannelDPCM,
            MobileChannelFM,
            MobileChannelNoise,
            MobileChannelSaw,
            MobileChannelSquare,
            MobileChannelTriangle,
            MobileChannelWaveTable,
            MobileInstrument,
            MobileInstrumentFds,
            MobileInstrumentNamco,
            MobileInstrumentSunsoft,
            MobileInstrumentVRC6,
            MobileInstrumentVRC7,
            MobileArpeggio,
            Count
        };

        private readonly string[] ButtonImageNames = new string[]
        {
            "Sequencer",
            "PianoRoll",
            "ProjectExplorer",
            "MobileSnapOn",
            "MobileSnapOff",
            "MobileChannelDPCM",
            "MobileChannelFM",
            "MobileChannelNoise",
            "MobileChannelSaw",
            "MobileChannelSquare",
            "MobileChannelTriangle",
            "MobileChannelWaveTable",
            "MobileInstrument",
            "MobileInstrumentFds",
            "MobileInstrumentNamco",
            "MobileInstrumentSunsoft",
            "MobileInstrumentVRC6",
            "MobileInstrumentVRC7",
            "MobileArpeggio",
        };

        public delegate void EmptyDelegate();

        public event EmptyDelegate SequencerClicked;
        public event EmptyDelegate PianoRollClicked;
        public event EmptyDelegate ProjectExplorerClicked;

        RenderFont font;
        RenderBitmapAtlas bmpButtonAtlas;
        Button[] buttons = new Button[(int)ButtonType.Count];

        int   buttonSizeNav;
        int   buttonSizeExpand;
        int   buttonMargin;
        int   buttonBitmapSize;
        float buttonBitmapScaleFloat = 1.0f;
        int   textOffset;

        public int LayoutSize => buttonSizeExpand;

        protected override void OnRenderInitialized(RenderGraphics g)
        {
            Debug.Assert((int)ButtonImageIndices.Count == ButtonImageNames.Length);

            bmpButtonAtlas = g.CreateBitmapAtlasFromResources(ButtonImageNames);

            buttons[(int)ButtonType.Sequencer]  = new Button { GetRenderInfo = GetSequencerRenderInfo, Click = OnSequencer, IsNavButton = true };
            buttons[(int)ButtonType.PianoRoll]  = new Button { GetRenderInfo = GetPianoRollRenderInfo, Click = OnPianoRoll, IsNavButton = true };
            buttons[(int)ButtonType.Project]    = new Button { GetRenderInfo = GetProjectExplorerInfo, Click = OnProjectExplorer, IsNavButton = true };
            buttons[(int)ButtonType.Tool]       = new Button { GetRenderInfo = GetToolRenderInfo, Click = OnProjectExplorer };
            buttons[(int)ButtonType.Snap]       = new Button { GetRenderInfo = GetSnapRenderInfo, Click = OnProjectExplorer };
            buttons[(int)ButtonType.Channel]    = new Button { GetRenderInfo = GetChannelRenderInfo, Click = OnProjectExplorer };
            buttons[(int)ButtonType.Instrument] = new Button { GetRenderInfo = GetInstrumentRenderingInfo, Click = OnProjectExplorer };
            buttons[(int)ButtonType.Arpeggio]   = new Button { GetRenderInfo = GetArpeggioRenderInfo, Click = OnProjectExplorer };

            var minSize = Math.Min(ParentFormSize.Width, ParentFormSize.Height);

            buttonSizeNav = minSize / 9;
            buttonSizeExpand = (minSize - (buttonSizeNav * 3)) / 5;
            buttonBitmapSize = bmpButtonAtlas.GetElementSize(0).Width;
            buttonMargin = (int)(buttonSizeNav * ButtonMargin);
            buttonBitmapScaleFloat = (buttonSizeNav - buttonMargin * 2) / (float)buttonBitmapSize; // MATTT : Bitmap scaling.

            // MATTT : Font scaling.
            var fontSize = (buttonSizeExpand - buttonMargin * 2) - (int)(buttonBitmapSize * buttonBitmapScaleFloat);
            font = ThemeResources.GetBestMatchingFont(g, fontSize, true);
            textOffset = buttonSizeExpand - buttonMargin - fontSize;
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

        //private Rectangle FlipRectangle(Rectangle rect)
        //{
        //    return new Rectangle(rect.Y, rect.X, rect.Height, rect.Width);
        //}

        private void UpdateButtonLayout()
        {
            if (!IsRenderInitialized)
                return;

            var landscape = IsLandscape;
            var x = 0;

            for (int i = 0; i < (int)ButtonType.Count; i++)
            {
                var btn = buttons[i];
                var size = btn.IsNavButton ? buttonSizeNav : buttonSizeExpand;

                if (landscape)
                {
                    btn.Hitbox = new Rectangle(0, x, buttonSizeExpand, size);
                    btn.IconX = (int)(buttonSizeExpand / 2 - buttonBitmapSize * buttonBitmapScaleFloat / 2);
                    btn.IconY = x + buttonMargin;
                    btn.TextY = x + textOffset;
                }
                else
                {
                    btn.Hitbox = new Rectangle(x, 0, size, buttonSizeExpand);
                    btn.IconX = x + (int)(buttonSizeExpand / 2 - buttonBitmapSize * buttonBitmapScaleFloat / 2);
                    btn.IconY = buttonMargin;
                    btn.TextY = textOffset;
                }

                x += size;
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

        private ButtonImageIndices GetSequencerRenderInfo(RenderGraphics g, out string text, out RenderBrush background)
        {
            text = null;
            background = ThemeResources.DarkGreyLineBrush1;
            return ButtonImageIndices.Sequencer;
        }

        private ButtonImageIndices GetPianoRollRenderInfo(RenderGraphics g, out string text, out RenderBrush background)
        {
            text = null;
            background = ThemeResources.DarkGreyLineBrush1;
            return ButtonImageIndices.PianoRoll;
        }

        private ButtonImageIndices GetProjectExplorerInfo(RenderGraphics g, out string text, out RenderBrush background)
        {
            text = null;
            background = ThemeResources.DarkGreyLineBrush1;
            return ButtonImageIndices.ProjectExplorer;
        }

        private ButtonImageIndices GetToolRenderInfo(RenderGraphics g, out string text, out RenderBrush background)
        {
            text = "Edit";
            background = g.GetSolidBrush(Theme.CustomColors[0, 0]);
            return ButtonImageIndices.MobileSnapOff;
        }

        private ButtonImageIndices GetSnapRenderInfo(RenderGraphics g, out string text, out RenderBrush background)
        {
            text = "1/2";
            background = g.GetSolidBrush(Theme.CustomColors[1, 0]);
            return ButtonImageIndices.MobileSnapOn;
        }

        private ButtonImageIndices GetChannelRenderInfo(RenderGraphics g, out string text, out RenderBrush background)
        {
            text = "Square 1";
            background = g.GetSolidBrush(Theme.CustomColors[2, 0]);
            return ButtonImageIndices.MobileChannelNoise;
        }

        private ButtonImageIndices GetInstrumentRenderingInfo(RenderGraphics g, out string text, out RenderBrush background)
        {
            text = "Instrument1";
            background = g.GetSolidBrush(Theme.CustomColors[3, 0]);
            return ButtonImageIndices.MobileInstrumentNamco;
        }

        private ButtonImageIndices GetArpeggioRenderInfo(RenderGraphics g, out string text, out RenderBrush background)
        {
            text = "1/2";
            background = g.GetSolidBrush(Theme.CustomColors[4, 0]);
            return ButtonImageIndices.MobileArpeggio;
        }

        protected override void OnRender(RenderGraphics g)
        {
            var c = g.CreateCommandList(); 

            // Buttons
            for (int i = 0; i < (int)ButtonType.Count; i++)
            {
                var btn = buttons[i];

                // DROIDTODO : Make the active control slightly brighter.
                //var opacity = status == ButtonStatus.Enabled ? 1.0f : 0.25f;

                var image = btn.GetRenderInfo(g, out var text, out var brush);

                c.FillRectangle(btn.Hitbox.Left, btn.Hitbox.Top, btn.Hitbox.Right, btn.Hitbox.Bottom, brush);

                if (!btn.IsNavButton)
                    c.DrawRectangle(btn.Hitbox.Left, btn.Hitbox.Top, btn.Hitbox.Right, btn.Hitbox.Bottom, ThemeResources.BlackBrush);

                c.DrawBitmapAtlas(bmpButtonAtlas, (int)image, btn.IconX, btn.IconY, 1.0f, buttonBitmapScaleFloat);

                if (!string.IsNullOrEmpty(text))
                    c.DrawText(text, font, btn.Hitbox.Left, btn.TextY, ThemeResources.BlackBrush, RenderTextAlignment.Center, buttonSizeExpand, 0, false, true);
            }

            //c.DrawLine(Width, 0, Width, Height, ThemeResources.BlackBrush, 5.0f); // DROIDTODO : Line width!
            g.DrawCommandList(c);
        }

        // MATTT Temporary
        protected override void OnTouch(int x, int y)
        {
            foreach (var btn in buttons)
            {
                if (btn.Hitbox.Contains(x, y))
                {
                    btn.Click();
                    break;
                }
            }
        }
    }
}
