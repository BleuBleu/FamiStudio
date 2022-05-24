using System.Drawing;
using System.Collections.Generic;

using RenderBitmapAtlas = FamiStudio.GLBitmapAtlas;
using RenderBrush = FamiStudio.GLBrush;
using RenderGeometry = FamiStudio.GLGeometry;
using RenderControl = FamiStudio.GLControl;
using RenderGraphics = FamiStudio.GLGraphics;
using RenderCommandList = FamiStudio.GLCommandList;
using System.Windows.Forms;

namespace FamiStudio
{
    // TODO:
    //  - Drag cursor if has focus
    //  - Make cursor blink (will require Tick())
    //  - Selection
    //  - Copy/paste
    //  - Scroll if past length.
    //  - Arrow navigation, selelection if SHIFT is held.

    public class TextBox2 : RenderControl
    {
        private string text;
        private int selectionStart = -1;
        private int selectionEnd = -1;
        private int scrollX;
        private Color foreColor = Theme.LightGreyFillColor1;
        private Color backColor = Theme.DarkGreyLineColor1;

        private int margin = DpiScaling.ScaleForMainWindow(4);

        public Color ForeColor { get => foreColor; set { foreColor = value; MarkDirty(); } }
        public Color BackColor { get => backColor; set { backColor = value; MarkDirty(); } }

        public TextBox2(string txt)
        {
            height = DpiScaling.ScaleForMainWindow(24);
            text = txt;
        }

        public string Text
        {
            get { return text; }
            set { text = value; MarkDirty(); }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
        }

        protected override void OnRender(RenderGraphics g)
        {
            var c = g.CreateCommandList(GLGraphicsBase.CommandListUsage.Dialog);
            var foreBrush = g.GetSolidBrush(foreColor);
            var backBrush = g.GetSolidBrush(backColor);

            c.FillAndDrawRectangle(0, 0, width - 1, height - 1, backBrush, foreBrush);
            c.DrawText("Hello", ThemeResources.FontMedium, margin, 0, foreBrush, RenderTextFlags.MiddleLeft | RenderTextFlags.Clip, width - margin * 2, height);
        }
    }
}
