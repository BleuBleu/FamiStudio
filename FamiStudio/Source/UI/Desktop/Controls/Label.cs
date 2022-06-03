using System.Diagnostics;

namespace FamiStudio
{
    public class Label : Control
    {
        protected int labelOffsetX;
        protected string text;
        protected bool multiline;

        public bool Multiline
        {
            get { return multiline; }
            set { multiline = value; MarkDirty(); }
        }

        public Label(string txt, bool multi = false)
        {
            text = txt;
            height = DpiScaling.ScaleForWindow(24);
            multiline = multi;
        }

        public void AutosizeWidth()
        {
            Debug.Assert(!multiline);
            width = ThemeResources.FontMedium.MeasureString(text, false);
        }

        public void AdjustHeightForMultiline()
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
            AdjustHeightForMultiline();
        }

        protected override void OnRender(Graphics g)
        {
            var c = parentDialog.CommandList;
            var brush = enabled ? ThemeResources.LightGreyFillBrush1 : ThemeResources.MediumGreyFillBrush1;

            if (multiline)
            {
                var lines = text.Split('\n');

                for (int i = 0; i < lines.Length; i++)
                {
                    c.DrawText(lines[i], ThemeResources.FontMedium, labelOffsetX, i * ThemeResources.FontMedium.LineHeight, brush, TextFlags.TopLeft, 0, height);
                }
            }
            else
            {
                c.DrawText(text, ThemeResources.FontMedium, labelOffsetX, 0, brush, TextFlags.MiddleLeft, 0, height);
            }
        }
    }
}
