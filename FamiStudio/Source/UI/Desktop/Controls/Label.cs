using System.Drawing;
using System.Collections.Generic;
using System.Diagnostics;

using RenderBitmapAtlas = FamiStudio.GLBitmapAtlas;
using RenderBrush = FamiStudio.GLBrush;
using RenderGeometry = FamiStudio.GLGeometry;
using RenderControl = FamiStudio.GLControl;
using RenderGraphics = FamiStudio.GLGraphics;
using RenderCommandList = FamiStudio.GLCommandList;

namespace FamiStudio
{
    public class Label2 : RenderControl
    {
        protected int labelOffsetX;
        protected string text;
        protected bool multiline;

        public Label2(string txt, bool multi = false)
        {
            text = txt;
            height = DpiScaling.ScaleForMainWindow(24);
            multiline = multi;
        }

        public void ResizeForMultiline()
        {
            if (multiline)
            {
                var actualWidth = width - labelOffsetX;
                var input = text;
                var output = "";
                var numLines = 0;

                while (true)
                {
                    var n = ThemeResources.FontMedium.GetNumCharactersForSize(input, actualWidth);
                    var done = n == input.Length;
                    
                    if (!done)
                    {
                        while (!char.IsWhiteSpace(input[n]))
                            n--;
                    }

                    output += input.Substring(0, n);
                    output += "\n";
                    numLines++;

                    if (!done)
                    {
                        while (char.IsWhiteSpace(input[n]))
                            n++;
                    }

                    input = input.Substring(n);

                    if (done)
                    {
                        break;
                    }
                }

                text = output;

                Resize(width, ThemeResources.FontMedium.LineHeight * numLines);
            }
        }

        public string Text
        {
            get { return text; }
            set { text = value; MarkDirty(); }
        }

        public int MeasureWidth()
        {
            return ThemeResources.FontMedium.MeasureString(text, false);
        }

        protected override void OnAddedToDialog()
        {
            ResizeForMultiline();
        }

        protected override void OnRender(RenderGraphics g)
        {
            var c = parentDialog.CommandList;
            var brush = enabled ? ThemeResources.LightGreyFillBrush1 : ThemeResources.MediumGreyFillBrush1;

            if (multiline)
            {
                var lines = text.Split('\n');

                for (int i = 0; i < lines.Length; i++)
                {
                    c.DrawText(lines[i], ThemeResources.FontMedium, labelOffsetX, i * ThemeResources.FontMedium.LineHeight, brush, RenderTextFlags.TopLeft, 0, height);
                }
            }
            else
            {
                c.DrawText(text, ThemeResources.FontMedium, labelOffsetX, 0, brush, RenderTextFlags.MiddleLeft, 0, height);
            }
        }
    }
}
