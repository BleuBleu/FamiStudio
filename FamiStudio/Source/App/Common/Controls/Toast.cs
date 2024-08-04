using System;
using System.Diagnostics;
using System.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace FamiStudio
{
    public class Toast : Container
    {
        private const int   DefaultPad = Platform.IsDesktop ? 8 : 3;
        private const int   DefaultPositionFromBottom = 32;
        private const float DefaultFadeTime = 0.25f;

        private int pad;
        private int posFromBottom;

        private string[] lines;
        private float duration;
        private float timer;
        private int alpha;
        private Font font;
        private Action action;

        public bool IsClickable => alpha > 0 && action != null;

        public Toast()
        {
            clipRegion = false;
        }

        public void Initialize(string text, bool longDuration, Action click = null)
        {
            lines = text.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            duration = longDuration ? 6.0f : 3.0f;
            timer = duration;
            action = click;
            alpha = 0;
            Visible = true;

            Reposition();
            MarkDirty();
            SetTickEnabled(true);
        }

        public void Reposition()
        {
            if (lines != null)
            {
                var sizeX = lines.Max(l => font.MeasureString(l, false)) + pad * 2;

                // Failsafe for very low resolution devices.
                if (Platform.IsMobile && sizeX > ParentWindowSize.Width)
                {
                    var text = string.Join('\n', lines);
                    var numLines = 0;
                    text = font.SplitLongString(text, ParentWindowSize.Width * 9 / 10, Localization.IsChinese, out numLines);
                    lines = text.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    sizeX = lines.Max(l => font.MeasureString(l, false)) + pad * 2;
                }

                var sizeY = font.LineHeight * lines.Length + pad * 2;
                var posX = (ParentWindowSize.Width  - sizeX) / 2;
                var posY = (ParentWindowSize.Height - sizeY - posFromBottom);

                Move(posX, posY, sizeX, sizeY);
            }
        }

        protected override void OnAddedToContainer()
        {
            font = Platform.IsDesktop ? fonts.FontMedium : fonts.FontSmall;
            pad = DpiScaling.ScaleForWindow(DefaultPad);
            posFromBottom = DpiScaling.ScaleForWindow(DefaultPositionFromBottom);
        }

        protected override void OnPointerDown(PointerEventArgs e)
        {
            action?.Invoke();
        }

        private int CalculateAlpha()
        {
            var alphaIn  = Utils.Clamp((duration - timer) / DefaultFadeTime, 0.0f, 1.0f);
            var alphaOut = Utils.Clamp(timer / DefaultFadeTime, 0.0f, 1.0f);
            return (int)(Math.Min(alphaIn, alphaOut) * 255);
        }

        public override void Tick(float delta)
        {
            if (timer > 0.0f)
            {
                timer = Math.Max(0.0f, timer - delta);

                if (timer == 0.0f)
                {
                    Dismiss();
                }

                SetAndMarkDirty(ref alpha, CalculateAlpha());
            }
        }

        public void Dismiss()
        {
            lines = null;
            action = null;
            duration = 0.0f;
            Visible = false;
            SetTickEnabled(false);
        }

        protected override void OnRender(Graphics g)
        {
            var o = g.TopMostOverlayCommandList;

            o.FillAndDrawRectangle(0, 0, width - 1, height - 1, Color.FromArgb(alpha, Theme.DarkGreyColor1), Color.FromArgb(alpha, Theme.BlackColor));

            for (int i = 0; i < lines.Length; i++)
                o.DrawText(lines[i], font, 0, pad + i * font.LineHeight, Color.FromArgb(alpha, Theme.LightGreyColor1), TextFlags.MiddleCenter, width, font.LineHeight);
        }
    }
}
