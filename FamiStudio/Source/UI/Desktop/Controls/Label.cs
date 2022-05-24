using System.Drawing;
using System.Collections.Generic;

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
        private string text;
        private bool multiline;

        public Label2(string txt, bool multi = false)
        {
            text = txt;
            height = DpiScaling.ScaleForMainWindow(24);
            multiline = multi;
        }

        public void ResizeForMultiline(int width)
        {
            if (multiline)
            {
                var input = text;
                var output = "";
                var numLines = 0;

                while (true)
                {
                    var n = ThemeResources.FontMedium.GetNumCharactersForSize(input, width);
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

        protected override void OnRender(RenderGraphics g)
        {
            var c = g.CreateCommandList(GLGraphicsBase.CommandListUsage.Dialog);

            if (multiline)
            {
                var lines = text.Split('\n');

                for (int i = 0; i < lines.Length; i++)
                {
                    c.DrawText(lines[i], ThemeResources.FontMedium, 0, i * ThemeResources.FontMedium.LineHeight, ThemeResources.LightGreyFillBrush1, RenderTextFlags.TopLeft, 0, height);
                }
            }
            else
            {
                c.DrawText(text, ThemeResources.FontMedium, 0, 0, ThemeResources.LightGreyFillBrush1, RenderTextFlags.MiddleLeft, 0, height);
            }
        }
    }
}
