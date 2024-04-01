using System;
using System.Diagnostics;
using System.Linq;

namespace FamiStudio
{
    public class Toast : Container
    {
        private const int   DefaultPad = 8;
        private const int   DefaultPositionFromBottom = 32;
        private const float DefaultFadeTime = 0.25f;

        private int pad;
        private int posFromBottom;

        private string[] lines;
        private float duration;
        private float timer;
        private int alpha;
        private Action action;

        public bool IsClickable => alpha > 0 && action != null;

        public Toast()
        {
            SetTickEnabled(true);
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
        }

        public void Reposition()
        {
            if (lines != null)
            {
                var font = ParentWindow.Fonts.FontMedium;
                var sizeX = lines.Max(l => font.MeasureString(l, false)) + pad * 2;
                var sizeY = font.LineHeight * lines.Length + pad * 2;
                var posX = (ParentWindowSize.Width - sizeX) / 2;
                var posY = (ParentWindowSize.Height - sizeY - posFromBottom);

                Move(posX, posY, sizeX, sizeY);
            }
        }

        protected override void OnAddedToContainer()
        {
            pad = DpiScaling.ScaleForWindow(DefaultPad);
            posFromBottom = DpiScaling.ScaleForWindow(DefaultPositionFromBottom);
        }

        protected override void OnMouseDown(MouseEventArgs e)
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
            base.Tick(delta);

            if (timer > 0.0f)
            {
                timer = Math.Max(0.0f, timer - delta);

                if (timer == 0.0f)
                {
                    lines = null;
                    action = null;
                    duration = 0.0f;
                    Visible = false;
                }

                SetAndMarkDirty(ref alpha, CalculateAlpha());
            }
        }

        protected override void OnRender(Graphics g)
        {
            var c = g.DefaultCommandList;
            var font = Fonts.FontMedium;

            c.FillAndDrawRectangle(0, 0, width - 1, height - 1, Color.FromArgb(alpha, Theme.DarkGreyColor1), Color.FromArgb(alpha, Theme.BlackColor));

            for (int i = 0; i < lines.Length; i++)
                c.DrawText(lines[i], font, 0, pad + i * font.LineHeight, Color.FromArgb(alpha, Theme.LightGreyColor1), TextFlags.MiddleCenter, width, font.LineHeight);
        }
    }
}
