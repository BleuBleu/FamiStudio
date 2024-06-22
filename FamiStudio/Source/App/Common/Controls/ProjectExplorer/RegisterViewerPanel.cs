using System;
using System.Diagnostics;

namespace FamiStudio
{
    public class RegisterViewerPanel : Control
    {
        protected NesApu.NesRegisterValues regValues;
        protected RegisterViewerRow[] regRows;
        protected int expansion;

        protected int marginX       = DpiScaling.ScaleForWindow(2);
        protected int registerSizeY = DpiScaling.ScaleForWindow(14);
        protected int labelSizeX    = DpiScaling.ScaleForWindow(58);

        protected static Color[] registerColors;

        public RegisterViewerPanel(NesApu.NesRegisterValues vals, RegisterViewerRow[] rows, int exp = -1)
        {
            regValues = vals;
            regRows = rows;
            expansion = exp; // -1 for interpreter rows.

            if (registerColors== null)
            {
                registerColors = new Color[11];

                var color0 = Theme.LightGreyColor2;    // Grey
                var color1 = Theme.CustomColors[0, 5]; // Red

                for (var i = 0; i < registerColors.Length; i++)
                {
                    var alpha = i / (float)(registerColors.Length - 1);
                    var color = Color.FromArgb(
                        (int)Utils.Lerp(color1.R, color0.R, alpha),
                        (int)Utils.Lerp(color1.G, color0.G, alpha),
                        (int)Utils.Lerp(color1.B, color0.B, alpha));
                    registerColors[i] = color;
                }
            }
        }

        protected int MeasureHeight()
        {
            var h = 0;
            for (int i = 0; i < regRows.Length; i++)
                h += regRows[i].CustomHeight > 0 ? DpiScaling.ScaleForWindow(regRows[i].CustomHeight) : registerSizeY;
            return h;
        }

        protected override void OnAddedToContainer()
        {
            height = MeasureHeight();
        }

        protected override void OnRender(Graphics g)
        {
            var c = g.GetCommandList();
            var y = 0;

            for (int i = 0; i < regRows.Length; i++)
            {
                var row = regRows[i];
                var regSizeY = row.CustomHeight > 0 ? DpiScaling.ScaleForWindow(row.CustomHeight) : registerSizeY;

                c.PushTranslation(0, y);

                if (i != 0)
                    c.DrawLine(0, -1, width, -1, Theme.BlackColor);

                if (row.CustomDraw != null)
                {
                    var label = row.Label;
                    c.DrawText(label, Fonts.FontSmall, marginX, 0, Theme.LightGreyColor2, TextFlags.Middle, 0, regSizeY);

                    c.PushTranslation(labelSizeX + 1, 0);
                    row.CustomDraw(c, Fonts, new Rectangle(0, 0, width - labelSizeX - 1, regSizeY), false);
                    c.PopTransform();
                }
                else if (row.GetValue != null)
                {
                    var label = row.Label;
                    var value = row.GetValue().ToString();
                    var flags = row.Monospace ? TextFlags.Middle | TextFlags.Monospace : TextFlags.Middle;

                    c.DrawText(label, Fonts.FontSmall, marginX, 0, Theme.LightGreyColor2, TextFlags.Middle, 0, regSizeY);
                    c.DrawText(value, Fonts.FontSmall, marginX + labelSizeX, 0, Theme.LightGreyColor2, flags, 0, regSizeY);
                }
                else
                {
                    Debug.Assert(expansion >= 0);

                    c.DrawText(row.Label, Fonts.FontSmall, marginX, 0, Theme.LightGreyColor2, TextFlags.Middle | TextFlags.Monospace, 0, regSizeY);

                    var flags = TextFlags.Monospace | TextFlags.Middle;
                    var x = marginX + labelSizeX;

                    for (var r = row.AddStart; r <= row.AddEnd; r++)
                    {
                        for (var s = row.SubStart; s <= row.SubEnd; s++)
                        {
                            var val = regValues.GetRegisterValue(expansion, r, out var age, s);
                            var str = $"${val:X2} ";
                            var color = registerColors[Math.Min(age, registerColors.Length - 1)];

                            c.DrawText(str, Fonts.FontSmall, x, 0, color, flags, 0, regSizeY);
                            x += (int)c.Graphics.MeasureString(str, Fonts.FontSmall, true);
                        }
                    }
                }

                c.PopTransform();
                y += regSizeY;
            }

            c.DrawLine(labelSizeX, 0, labelSizeX, height, Theme.BlackColor);
        }
    }
}
