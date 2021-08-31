using System;
using System.Diagnostics;
using System.Windows.Forms;

using Color     = System.Drawing.Color;
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
            public Rectangle Rect;
            public Rectangle ExpandedRect;
            public int IconX;
            public int IconY;
            public int ExpandIconX;
            public int ExpandIconY;
            public int TextX;
            public int TextY;
            public bool IsNavButton = false;
            public Color Color;
            public RenderInfoDelegate GetRenderInfo;
            public EmptyDelegate Click;
        }

        private class ExpandButton
        {
            public Rectangle Hitbox;
            public int IconX;
            public int IconY;
            public int TextX;
            public int TextY;
            public Color Color;
            public int imageIndex;
            public string text;
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

        // These are only use for expandable button.
        private int            expandedButtonIdx = -1;
        private float          expandedRatio = 0.0f;
        private bool           expanding;
        private bool           closing;
        private ExpandButton[] expandButtons;
        
        private int   maxExpandSize = 1500;

        // Scaled layout variables.
        private int buttonSize;
        private int buttonSizeNav;
        private int buttonIconSize;
        private int buttonIconPos1;
        private int buttonIconPos2;
        private int textPosTop;
        private int expandIconPosTop;
        private int expandIconPosLeft;

        private float iconScaleFloat = 1.0f;
        private float iconScaleExpFloat = 1.0f;

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
            buttons[(int)ButtonType.Channel]    = new Button { GetRenderInfo = GetChannelRenderInfo, Click = OnChannel };
            buttons[(int)ButtonType.Instrument] = new Button { GetRenderInfo = GetInstrumentRenderingInfo, Click = OnInstrument };
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

        public void Tick(float delta)
        {
            delta *= 6.0f;

            if (expandedButtonIdx >= 0)
            {
                if (expanding && expandedRatio != 1.0f)
                {
                    expandedRatio = Math.Min(expandedRatio + delta, 1.0f);
                    if (expandedRatio == 1.0f)
                    {
                        expanding = false;
                    }
                }
                else if (closing && expandedRatio != 0.0f)
                {
                    expandedRatio = Math.Max(expandedRatio - delta, 0.0f);
                    if (expandedRatio == 0.0f)
                    {
                        expandButtons = null;
                        expandedButtonIdx = -1;
                        closing = false;
                    }
                }
            }
        }

        private Rectangle GetExpandableButtonRectangle()
        {
            var totalSize = Math.Min(expandButtons.Length * buttonSize, 1500); // MATTT
            var interpSize = (int)Math.Round(totalSize * expandedRatio);

            if (IsLandscape)
            {
                var rect = buttons[expandedButtonIdx].Rect;
                return new Rectangle(-interpSize, rect.Y, interpSize, rect.Height);
            }
            else
            {
                return new Rectangle();
            }
        }

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
                    btn.Rect = new Rectangle(0, x, buttonSize, size);
                    btn.IconX = buttonIconPos2;
                    btn.IconY = x + buttonIconPos1;
                    btn.ExpandIconX = 0;
                    btn.ExpandIconY = x + expandIconPosLeft;
                    btn.TextX = 0;
                    btn.TextY = x + textPosTop;
                }
                else
                {
                    btn.Rect = new Rectangle(x, 0, size, buttonSize);
                    btn.IconX = x + buttonIconPos2;
                    btn.IconY = buttonIconPos2;
                    btn.ExpandIconX = x + expandIconPosLeft;
                    btn.ExpandIconY = expandIconPosTop;
                    btn.TextX = x;
                    btn.TextY = textPosTop;
                }

                x += size;
            }
        }

        public void ConditionalInvalidate()
        {
            if (App != null && !App.RealTimeUpdate)
                Invalidate();
        }

        private void StartExpandButton(int idx, ExpandButton[] buttons)
        {
            var landscape = IsLandscape;
            var x = 0;

            for (int i = 0; i < buttons.Length; i++)
            {
                var btn = buttons[i];

                if (landscape)
                {
                    btn.Hitbox = new Rectangle(x, 0, buttonSize, buttonSize);
                    btn.IconX = x + buttonIconPos2;
                    btn.IconY = buttonIconPos1;
                    btn.TextX = x;
                    btn.TextY = textPosTop;
                }
                else
                {

                }

                x += buttonSize;
            }

            expandedButtonIdx = idx;
            expandedRatio = 0.0f;
            expanding = true;
            closing = false;
            expandButtons = buttons;
        }
        
        private void StartClosingButton(int idx)
        {
            expandedButtonIdx = idx;
            expanding = false;
            closing = true;
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

        private void OnChannel()
        {
            if (expandedButtonIdx == (int)ButtonType.Channel)
            {
                StartClosingButton(expandedButtonIdx);
                return;
            }

            var channelTypes = App.Project.GetActiveChannelList();
            var buttons = new ExpandButton[channelTypes.Length];

            for (int i = 0; i < channelTypes.Length; i++)
            {
                var btn = new ExpandButton();
                btn.Color = Theme.LightGreyFillColor1;
                btn.imageIndex = Array.IndexOf(ButtonImageNames, "Mobile" + ChannelType.Icons[i]);
                btn.text = ChannelType.GetNameWithExpansion(i);
                buttons[i] = btn;
            }

            StartExpandButton((int)ButtonType.Channel, buttons);
        }

        private void OnInstrument()
        {
            if (expandedButtonIdx == (int)ButtonType.Instrument)
            {
                StartClosingButton(expandedButtonIdx);
                return;
            }

            var project = App.Project;
            var buttons = new ExpandButton[project.Instruments.Count];

            for (int i = 0; i < project.Instruments.Count; i++)
            {
                var inst = project.Instruments[i];
                var btn = new ExpandButton();
                btn.Color = inst.Color;
                btn.imageIndex = Array.IndexOf(ButtonImageNames, "Mobile" + ExpansionType.Icons[inst.Expansion]);
                btn.text = inst.Name;
                buttons[i] = btn;
            }

            StartExpandButton((int)ButtonType.Instrument, buttons);
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

                var image = btn.GetRenderInfo(g, out var text, out var brush); // MATTT Return a color here, not brush.
                //var expImage = (int)ButtonImageIndices.ExpandLeft;
                var expImage = IsLandscape ? (int)ButtonImageIndices.ExpandLeft : (int)ButtonImageIndices.ExpandUp;

                c.FillRectangle(btn.Rect.Left, btn.Rect.Top, btn.Rect.Right, btn.Rect.Bottom, brush); // MATTT Color + gradient.

                if (!btn.IsNavButton)
                    c.DrawRectangle(btn.Rect.Left, btn.Rect.Top, btn.Rect.Right, btn.Rect.Bottom, ThemeResources.BlackBrush);

                c.DrawBitmapAtlas(bmpButtonAtlas, (int)image, btn.IconX, btn.IconY, 1.0f, iconScaleFloat);
                c.DrawBitmapAtlas(bmpButtonAtlas, (int)expImage, btn.ExpandIconX, btn.ExpandIconY, 1.0f, iconScaleExpFloat);

                if (!string.IsNullOrEmpty(text))
                    c.DrawText(text, font, btn.TextX, btn.TextY, ThemeResources.BlackBrush, RenderTextFlags.Center | RenderTextFlags.Ellipsis, buttonSize, 0);
            }

            g.DrawCommandList(c);

            if (expandedButtonIdx >= 0)
            {
                c = g.CreateCommandList();

                var rect = GetExpandableButtonRectangle();
                c.FillAndDrawRectangle(rect.Left, rect.Top, rect.Right, rect.Bottom, ThemeResources.LightRedFillBrush, ThemeResources.BlackBrush); // MATTT Color + gradient.
                c.PushTranslation(rect.Left, rect.Top);

                for (int i = 0; i < expandButtons.Length; i++)
                {
                    var btn = expandButtons[i];
                    c.DrawBitmapAtlas(bmpButtonAtlas, btn.imageIndex, btn.IconX, btn.IconY, 1.0f, iconScaleFloat);
                    c.DrawText(btn.text, font, btn.TextX, btn.TextY, ThemeResources.BlackBrush, RenderTextFlags.Center | RenderTextFlags.Ellipsis, buttonSize, 0);
                }

                c.PopTransform();

                c.Transform.GetOrigin(out var ox, out var oy);
                rect.Offset((int)Math.Round(ox), (int)Math.Round(oy));
                g.DrawCommandList(c, rect);
            }
        }

        // MATTT Temporary
        protected override void OnTouch(int x, int y)
        {
            foreach (var btn in buttons)
            {
                if (btn.Rect.Contains(x, y))
                {
                    btn.Click();
                    break;
                }
            }
        }
    }
}
