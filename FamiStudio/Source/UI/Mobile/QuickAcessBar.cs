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
        // All of these were calibrated at 1080p and will scale up/down from there.
        const int DefaultNavButtonSize     = 120;
        const int DefaultButtonSize        = 144;
        const int DefaultIconSize          = 96;
        const int DefaultIconPos1          = 12;
        const int DefaultIconPos2          = 24;
        const int DefaultTextSize          = 24;
        const int DefaultTextPosTop        = 108;
        const int DefaultExpandIconPosTop  = 0;
        const int DefaultExpandIconPosLeft = 56;
        const int DefaultExpandIconSize    = 32;

        private delegate ButtonImageIndices RenderInfoDelegate(RenderGraphics g, out string text, out RenderBrush background);
        private delegate bool EnabledDelegate();

        private class Button
        {
            public Rectangle Hitbox;
            public int IconX;
            public int IconY;
            public int ExpandIconX;
            public int ExpandIconY;
            public int TextX;
            public int TextY;
            public int TextWidth;
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
            ExpandUp,
            ExpandDown,
            ExpandLeft,
            ExpandRight,
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
            "ExpandUp",
            "ExpandDown",
            "ExpandLeft",
            "ExpandRight",
        };

        public delegate void EmptyDelegate();

        public event EmptyDelegate SequencerClicked;
        public event EmptyDelegate PianoRollClicked;
        public event EmptyDelegate ProjectExplorerClicked;

        RenderFont font;
        RenderBitmapAtlas bmpButtonAtlas;
        Button[] buttons = new Button[(int)ButtonType.Count];

        int buttonSize;
        int buttonSizeNav;
        int buttonIconSize;
        int buttonIconPos1;
        int buttonIconPos2;
        int textPosTop;
        int expandIconPosTop;
        int expandIconPosLeft;

        float iconScaleFloat = 1.0f;
        float iconScaleExpFloat = 1.0f;

        public int LayoutSize => buttonSize;

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

            // MATTT : Font scaling?
            var scale = Math.Min(ParentFormSize.Width, ParentFormSize.Height) / 1080.0f;

            font = ThemeResources.GetBestMatchingFont(g, ScaleCustom(DefaultTextSize, scale), true);

            buttonSize        = ScaleCustom(DefaultButtonSize, scale);
            buttonSizeNav     = ScaleCustom(DefaultNavButtonSize, scale);
            buttonIconSize    = ScaleCustom(DefaultIconSize, scale);
            buttonIconPos1    = ScaleCustom(DefaultIconPos1, scale);
            buttonIconPos2    = ScaleCustom(DefaultIconPos2, scale);
            textPosTop        = ScaleCustom(DefaultTextPosTop, scale);
            expandIconPosTop  = ScaleCustom(DefaultExpandIconPosTop, scale);
            expandIconPosLeft = ScaleCustom(DefaultExpandIconPosLeft, scale);

            iconScaleFloat = ScaleCustomFloat(DefaultIconSize / (float)bmpButtonAtlas.GetElementSize(0).Width, scale);
            iconScaleExpFloat = ScaleCustomFloat(DefaultExpandIconSize / (float)bmpButtonAtlas.GetElementSize((int)ButtonImageIndices.ExpandUp).Width, scale);
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
                var size = btn.IsNavButton ? buttonSizeNav : buttonSize;

                if (landscape)
                {
                    btn.Hitbox = new Rectangle(0, x, buttonSize, size);
                    btn.IconX = buttonIconPos2;
                    btn.IconY = x + buttonIconPos1;
                    btn.ExpandIconX = 0;
                    btn.ExpandIconY = x + expandIconPosLeft;
                    btn.TextX = 0;
                    btn.TextY = x + textPosTop;
                    btn.TextWidth = buttonSize;
                }
                else
                {
                    btn.Hitbox = new Rectangle(x, 0, size, buttonSize);
                    btn.IconX = x + buttonIconPos2;
                    btn.IconY = buttonIconPos2;
                    btn.ExpandIconX = x + expandIconPosLeft;
                    btn.ExpandIconY = expandIconPosTop;
                    btn.TextX = x;
                    btn.TextY = textPosTop;
                    btn.TextWidth = buttonSize;
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
                //var expImage = (int)ButtonImageIndices.ExpandLeft;
                var expImage = IsLandscape ? (int)ButtonImageIndices.ExpandLeft : (int)ButtonImageIndices.ExpandUp;

                c.FillRectangle(btn.Hitbox.Left, btn.Hitbox.Top, btn.Hitbox.Right, btn.Hitbox.Bottom, brush);

                if (!btn.IsNavButton)
                    c.DrawRectangle(btn.Hitbox.Left, btn.Hitbox.Top, btn.Hitbox.Right, btn.Hitbox.Bottom, ThemeResources.BlackBrush);

                c.DrawBitmapAtlas(bmpButtonAtlas, (int)image, btn.IconX, btn.IconY, 1.0f, iconScaleFloat);
                c.DrawBitmapAtlas(bmpButtonAtlas, (int)expImage, btn.ExpandIconX, btn.ExpandIconY, 1.0f, iconScaleExpFloat);

                if (!string.IsNullOrEmpty(text))
                    c.DrawText(text, font, btn.TextX, btn.TextY, ThemeResources.BlackBrush, RenderTextFlags.Center | RenderTextFlags.Ellipsis, btn.TextWidth, 0);
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
