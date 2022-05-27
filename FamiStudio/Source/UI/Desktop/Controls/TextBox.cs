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
    //  / Draw cursor if has focus
    //  x Make cursor blink (will require Tick())
    //  - Selection
    //  - Copy/paste
    //  - Scroll if past length.
    //  - Arrow navigation, selelection if SHIFT is held.

    public class TextBox2 : RenderControl
    {
        private string text;
        private int scrollX;
        private int selectionStart = -1;
        private int selectionLength = 0;
        private int caretIndex = 0;
        private bool caretBlink = true;
        private float caretBlinkTime;

        private Color foreColor = Theme.LightGreyFillColor1;
        private Color backColor = Theme.DarkGreyLineColor1;
        private Color selColor  = Theme.DarkGreyFillColor3;

        private int topMargin  = DpiScaling.ScaleForMainWindow(3);
        private int sideMargin = DpiScaling.ScaleForMainWindow(/*4*/ 10); // MATTT

        public Color ForeColor { get => foreColor; set { foreColor = value; MarkDirty(); } }
        public Color BackColor { get => backColor; set { backColor = value; MarkDirty(); } }

        public TextBox2(string txt)
        {
            height = DpiScaling.ScaleForMainWindow(24);
            text = "Hello this is a very    long text bla bla bla toto titi tata tutu"; // MATTT txt;
            //text = "test lol"; // MATTT txt;
        }

        public string Text
        {
            get { return text; }
            set 
            { 
                text = value;
                scrollX = 0;
                caretIndex = 0;
                selectionStart = 0;
                selectionLength = 0;
                MarkDirty(); 
            }
        }

        protected override void OnMouseDown(MouseEventArgsEx e)
        {
            var c = PixelToChar(e.X);
            SetAndMarkDirty(ref caretIndex, c);
            SetAndMarkDirty(ref selectionStart, c);
            SetAndMarkDirty(ref selectionLength, 0);
            ResetCaretBlink();
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            var c0 = PixelToChar(e.X);
            var c1 = c0;

            while (c1 < text.Length && !char.IsWhiteSpace(text[c1]))
                c1++;
            while (c1 < text.Length && char.IsWhiteSpace(text[c1]))
                c1++;
            while (c0 >= 1 && !char.IsWhiteSpace(text[c0 - 1]))
                c0--;

            selectionStart  = c0;
            selectionLength = c1 - c0;
            caretIndex      = c1;

            MarkDirty();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            // MATTT : CTRL, SHIFT, etc.
            // MATTT : Copy/paste.
            if (e.KeyCode == Keys.Left)
            {
                
            }
            else if (e.KeyCode == Keys.Right)
            {

            }
            else if (e.KeyCode == Keys.Escape)
            {
                ClearDialogFocus();
                e.Handled = true;
            }
        }

        private void EnsureCaretVisible()
        {

        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
        }

        public override void Tick(float delta)
        {
            caretBlinkTime += delta;
            SetAndMarkDirty(ref caretBlink, Utils.Frac(caretBlinkTime) < 0.5f);
        }

        private void UpdateCaretBlink()
        {
            SetAndMarkDirty(ref caretBlink, Utils.Frac(caretBlinkTime) < 0.5f);
        }

        private void ResetCaretBlink()
        {
            caretBlinkTime = 0;
            UpdateCaretBlink();
        }

        private int PixelToChar(int x)
        {
            return ThemeResources.FontMedium.GetNumCharactersForSize(text, x - sideMargin + scrollX, true);
        }

        private int CharToPixel(int c)
        {
            var px = sideMargin - scrollX;
            if (c > 0)
                px += ThemeResources.FontMedium.MeasureString(text.Substring(0, c), false);
            return px;
        }

        public void SelectAll()
        {
            selectionStart = 0;
            selectionLength = text.Length;
            caretIndex = text.Length;
            MarkDirty();
        }

        protected override void OnAddedToDialog()
        {
            Cursor.Current = Cursors.IBeam;
        }

        protected override void OnRender(RenderGraphics g)
        {
            var c = parentDialog.CommandList;

            // MATTT : Cache those.
            var foreBrush = g.GetSolidBrush(foreColor);
            var backBrush = g.GetSolidBrush(backColor);
            var selBrush  = g.GetSolidBrush(selColor);

            c.FillAndDrawRectangle(0, 0, width - 1, height - 1, backBrush, foreBrush);
            
            if (selectionLength > 0 && HasDialogFocus)
            {
                var sx0 = CharToPixel(selectionStart);
                var sx1 = selectionLength > 0 ? CharToPixel(selectionStart + selectionLength) : sx0;

                c.FillRectangle(sx0, topMargin, sx1, height - topMargin, selBrush);
            }

            c.DrawText(text, ThemeResources.FontMedium, sideMargin - scrollX, 0, foreBrush, RenderTextFlags.MiddleLeft | RenderTextFlags.Clip, 0, height, sideMargin, width - sideMargin);

            if (caretBlink && HasDialogFocus)
            {
                var cx = CharToPixel(caretIndex);
                c.DrawLine(cx, topMargin, cx, height - topMargin, ThemeResources.LightGreyFillBrush1);
            }
        }
    }
}
