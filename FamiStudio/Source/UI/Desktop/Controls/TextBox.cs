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
        private Color backColor = Color.White;

        private int margin = DpiScaling.ScaleForDialog(4);

        public Color BackColor { get => backColor; set { backColor = value; MarkDirty(); } }

        public TextBox2(string txt)
        {
            height = DpiScaling.ScaleForDialog(24);
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

            c.FillAndDrawRectangle(0, 0, width - 1, height - 1, g.GetSolidBrush(backColor), ThemeResources.BlackBrush);
            c.DrawText("Hello", ThemeResources.FontMedium, margin, 0, ThemeResources.BlackBrush, RenderTextFlags.MiddleLeft | RenderTextFlags.Clip, width - margin * 2, height);
        }
    }
}
