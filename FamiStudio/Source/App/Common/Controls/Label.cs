using System;
using System.Diagnostics;
using System.Globalization;

namespace FamiStudio
{
    public class Label : Control
    {
        protected int labelOffsetX;
        protected string text;
        protected string multilineSplitText;
        protected bool multiline;
        protected bool centered;
        protected bool ellipsis;
        private Font font;
        protected Color color = Theme.LightGreyColor1;
        protected Color disabledColor = Theme.MediumGreyColor1;

        public Label(string txt, bool multi = false)
        {
            text = txt ?? "";
            height = DpiScaling.ScaleForWindow(Platform.IsMobile ? 16 : 24);
            multiline = multi;
        }

        public bool Multiline
        {
            get { return multiline; }
            set { if (SetAndMarkDirty(ref multiline, value)) AdjustHeightForMultiline(); }
        }

        public Font Font
        {
            get { return font; }
            set { font = value; MarkDirty(); }
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

        public void AutosizeWidth()
        {
            Debug.Assert(!multiline);
            width = font.MeasureString(text, false);
        }

        public void AutoSizeHeight()
        {
            if (!multiline)
            {
                height = font.LineHeight;
            }
        }

        protected override void OnResize(EventArgs e)
        {
            AdjustHeightForMultiline();
        }

        private void AdjustHeightForMultiline()
        {
            if (multiline && fonts != null)
            {
                var actualWidth = width - labelOffsetX;
                var input = text;
                var output = "";
                var numLines = 0;

                while (true)
                {
                    var numCharsWeCanFit = font.GetNumCharactersForSize(input, actualWidth);
                    var n = numCharsWeCanFit;
                    var done = n == input.Length;
                    var newLineIndex = input.IndexOf('\n');
                    var newLine = false;

                    if (newLineIndex >= 0 && newLineIndex < numCharsWeCanFit)
                    {
                        n = newLineIndex;
                        done = n == input.Length;
                        newLine = true;
                    }
                    else
                    {
                        if (!done)
                        {
                            // This bizarre code was added by the chinese translators. Not touching it.
                            if (Localization.LanguageCode == "ZHO")
                            {
                                var minimumCharsPerLine = Math.Max((int)(numCharsWeCanFit * 0.62), numCharsWeCanFit - 20);
                                while (!char.IsWhiteSpace(input[n]) && input[n] != '“' && char.GetUnicodeCategory(input[n]) != UnicodeCategory.OpenPunctuation)
                                {
                                    n--;
                                    // No whitespace or punctuation found, let's chop in the middle of a word.
                                    if (n <= minimumCharsPerLine)
                                    {
                                        n = numCharsWeCanFit;
                                        if (char.IsPunctuation(input[n]))
                                            n--;
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                while (n >= 0 && !char.IsWhiteSpace(input[n]))
                                {
                                    n--;
                                }

                                // No whitespace found, let's chop in the middle of a word.
                                if (n < 0)
                                {
                                    n = numCharsWeCanFit;
                                }
                            }
                        }
                    }

                    output += input.Substring(0, n);
                    output += "\n";
                    numLines++;

                    if (!done)
                    {
                        // After an intentional new line (one that had a \n right in the string), preserve
                        // any white space since it may be used for indentation.
                        if (newLine)
                        {
                            n++;
                        }
                        else
                        {
                            while (char.IsWhiteSpace(input[n]))
                                n++;
                        }
                    }

                    input = input.Substring(n);

                    if (done)
                    {
                        break;
                    }
                }

                multilineSplitText = output;

                Resize(width, font.LineHeight * numLines, false);
            }
        }

        public string Text
        {
            get { return text; }
            set { if (SetAndMarkDirty(ref text, value)) AdjustHeightForMultiline(); }
        }

        public int MeasureWidth()
        {
            return font.MeasureString(text, false);
        }

        protected override void OnAddedToContainer()
        {
            if (font == null)
            {
                font = fonts.FontMedium;
            }
            AdjustHeightForMultiline();
        }

        protected override void OnRender(Graphics g)
        {
            var c = g.GetCommandList();
            var brush = enabled ? color : disabledColor;

            if (multiline)
            {
                var lines = multilineSplitText.Split('\n');

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
