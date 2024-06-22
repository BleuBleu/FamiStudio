using System;
using System.Diagnostics;
using System.Globalization;

namespace FamiStudio
{
    public class Label : Control
    {
        protected int labelOffsetX;
        protected string text;
        protected bool multiline;
        protected bool bold;
        protected bool centered;
        protected bool ellipsis;
        protected Color color = Theme.LightGreyColor1;
        protected Color disabledColor = Theme.MediumGreyColor1;

        public Label(string txt, bool multi = false)
        {
            text = txt;
            height = DpiScaling.ScaleForWindow(24);
            multiline = multi;
        }

        public bool Multiline
        {
            get { return multiline; }
            set { SetAndMarkDirty(ref multiline, value); }
        }

        public bool Bold
        {
            get { return bold; }
            set { SetAndMarkDirty(ref bold, value); }
        }

        public bool Centered
        {
            get { return centered; }
            set { SetAndMarkDirty(ref centered, value); }
        }

        public bool Ellipsis
        {
            get { return ellipsis; }
            set { SetAndMarkDirty(ref ellipsis, value); }
        }

        public Color Color
        {
            get { return color; }
            set { color = value; MarkDirty(); }
        }

        public Color DisabledColor
        {
            get { return disabledColor; }
            set { disabledColor = value; MarkDirty(); }
        }

        private Font GetFont()
        {
            return bold ? Fonts.FontMediumBold : Fonts.FontMedium;
        }

        public void AutosizeWidth()
        {
            Debug.Assert(!multiline);
            width = GetFont().MeasureString(text, false);
        }

        public void AdjustHeightForMultiline()
        {
            if (multiline)
            {
                var actualWidth = width - labelOffsetX;
                var input = text;
                var output = "";
                var numLines = 0;
                var font = GetFont();

                while (true)
                {
                    var numCharsWeCanFit = font.GetNumCharactersForSize(input, actualWidth);
                    var minimunCharsPerLine = Math.Max((int)(numCharsWeCanFit * 0.62), numCharsWeCanFit - 20);
                    var n = numCharsWeCanFit;
                    var done = n == input.Length;
                    
                    if (!done)
                    {
                        while (!char.IsWhiteSpace(input[n]) && input[n] != '\u201C' && char.GetUnicodeCategory(input[n]) != UnicodeCategory.OpenPunctuation)
                        {
                            n--;
                            // No whitespace or punctuation found, let's chop in the middle of a word.
                            if (n <= minimunCharsPerLine)
                            {
                                n = numCharsWeCanFit;
                                if (char.IsPunctuation(input[n]))
                                    n--;
                                break;
                            }
                        }
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

                Resize(width, font.LineHeight * numLines);
            }
        }

        public string Text
        {
            get { return text; }
            set { text = value; MarkDirty(); }
        }

        public int MeasureWidth()
        {
            return GetFont().MeasureString(text, false);
        }

        protected override void OnAddedToContainer()
        {
            AdjustHeightForMultiline();
        }

        protected override void OnRender(Graphics g)
        {
            var c = g.GetCommandList();
            var brush = enabled ? color : disabledColor;
            var font = GetFont();

            if (multiline)
            {
                var lines = text.Split('\n');

                for (int i = 0; i < lines.Length; i++)
                {
                    c.DrawText(lines[i], font, labelOffsetX, i * font.LineHeight, brush, TextFlags.TopLeft, 0, height);
                }
            }
            else
            {
                var flags = TextFlags.Middle;
                if (centered) flags |= TextFlags.Center;
                if (ellipsis) flags |= TextFlags.Ellipsis;
                c.DrawText(text, font, labelOffsetX, 0, brush, flags, centered || ellipsis ? width : 0, height);
            }
        }
    }
}
