using System;
using System.Diagnostics;
using System.Globalization;

namespace FamiStudio
{
    public class Oscilloscope : Control
    {
        private bool lastOscilloscopeHadNonZeroSample;

        public bool LastOscilloscopeHadNonZeroSample => lastOscilloscopeHadNonZeroSample;

        public Oscilloscope()
        {
        }

        protected override void OnRender(Graphics g)
        {
            var c = g.DefaultCommandList;

            var sx = width;
            var sy = height;

            c.PushClipRegion(1, 1, sx - 1, sy - 1);
            c.FillRectangle(0, 0, sx, sy, Theme.BlackColor);

            var oscilloscopeGeometry = App.GetOscilloscopeGeometry(out lastOscilloscopeHadNonZeroSample);

            if (oscilloscopeGeometry != null && lastOscilloscopeHadNonZeroSample)
            {
                float scaleX = sx;
                float scaleY = sy / -2; // D3D is upside down compared to how we display waves typically.

                c.PushTransform(0, sy / 2, scaleX, scaleY);
                c.DrawNiceSmoothLine(oscilloscopeGeometry, Theme.LightGreyColor2);
                c.PopTransform();
            }
            else
            {
                c.PushTranslation(0, sy / 2);
                c.DrawLine(0, 0, sx, 0, Theme.LightGreyColor2);
                c.PopTransform();
            }

            if (Platform.IsMobile)
            {
                Utils.SplitVersionNumber(Platform.ApplicationVersion, out var betaNumber);

                if (betaNumber > 0)
                    c.DrawText($"BETA {betaNumber}", Fonts.FontSmall, 4, 4, Theme.LightRedColor);
            }

            c.PopClipRegion();
            c.DrawRectangle(0, 0, sx, sy, Theme.LightGreyColor2);
        }
    }
}
