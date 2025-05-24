using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;

namespace FamiStudio
{
    public class TextBox : Control
    {
        public delegate void TextChangedDelegate(Control sender);
        public event TextChangedDelegate TextChanged;

        protected class TextBoxState
        {
            public int selStart;
            public int selLen;
            public int caretIndex;
            public string text;
        };

        protected string text = "";
        protected string prompt;
        protected int scrollX;
        protected int maxScrollX;
        protected int selectionStart = -1;
        protected int selectionLength = 0;
        protected int caretIndex;
        protected int textAreaWidth;
        protected int textAreaWidthNoMargin;
        protected int maxLength;
        protected int mouseSelectionChar;
        protected float numberMin;
        protected float numberMax;
        protected float numberInc;
        protected bool mouseSelecting;
        protected bool caretBlink = true;
        protected bool numeric;
        protected bool allowMobileEdit = true;
        protected float caretBlinkTime;

        protected int undoRedoIndex = 0;
        protected List<TextBoxState> undoRedo = new List<TextBoxState>();

        protected Color foreColor     = Theme.LightGreyColor1;
        protected Color disabledColor = Theme.MediumGreyColor1;
        protected Color backColor     = Theme.DarkGreyColor1;
        protected Color selColor      = Theme.MediumGreyColor1;

        protected int topMargin        = DpiScaling.ScaleForWindow(3);
        protected int outerMarginLeft  = DpiScaling.ScaleForWindow(0);
        protected int outerMarginRight = DpiScaling.ScaleForWindow(0);
        protected int innerMargin      = DpiScaling.ScaleForWindow(4);
        protected int scrollAmount     = DpiScaling.ScaleForWindow(20);

        public Color ForeColor      { get => foreColor;     set { SetAndMarkDirty(ref foreColor, value);     } }
        public Color DisabledColor  { get => disabledColor; set { SetAndMarkDirty(ref disabledColor, value); } }
        public Color BackColor      { get => backColor;     set { SetAndMarkDirty(ref backColor, value);     } }
        public Color SelectionColor { get => selColor;      set { SetAndMarkDirty(ref selColor, value);      } }

        public string Prompt { get => prompt; set => prompt = value; }

        #region Localization
        
        protected LocalizedString CopyName;
        protected LocalizedString CutName;
        protected LocalizedString PasteName;

        #endregion

        public TextBox(string txt, int maxLen = 0)
        {
            Debug.Assert(txt != null);
            Localization.Localize(this);
            height = DpiScaling.ScaleForWindow(Platform.IsMobile ? 16 : 24);
            text = txt;
            maxLength = maxLen;
            supportsDoubleClick = true;
        }

        public TextBox(float value, float minVal, float maxVal, float increment)
        {
            Localization.Localize(this);
            height = DpiScaling.ScaleForWindow(24);
            text = value.ToString(CultureInfo.InvariantCulture);
            numeric = true;
            numberMin = minVal;
            numberMax = maxVal;
            numberInc = increment;
        }

        public string Text
        {
            get { return text; }
            set 
            {
                Debug.Assert(value != null);
                text = FilterString(maxLength > 0 ? value.Substring(0, maxLength) : value);
                scrollX = 0;
                caretIndex = 0;
                selectionStart = 0;
                selectionLength = 0;
                OnTextChanged();
                UpdateScrollParams();
                MarkDirty(); 
            }
        }

        protected string FilterString(string s)
        {
            return numeric ? new string(s.Where(c => char.IsDigit(c) || c == '-' || c == '.' || c == ',').ToArray()) : s;
        }

        protected void ClampNumber()
        {
            if (numeric)
            {
                var val = text.Contains('.') || text.Contains(',')
                        ? Utils.ParseFloatWithTrailingGarbage(text) 
                        : Utils.ParseIntWithTrailingGarbage(text);

                var snap   = Utils.RoundDownFloat(val, numberInc);;
                var clamp  = Utils.Clamp(snap, numberMin, numberMax);

                if (SetAndMarkDirty(ref text, clamp.ToString(CultureInfo.InvariantCulture)))
                {
                    caretIndex = Utils.Clamp(caretIndex, 0, text.Length);
                    selectionStart = -1;
                    selectionLength = 0;
                }
            }
        }

        protected void SetAndFormatTextForScaleValue(double s)
        {
            text = s.ToString(CultureInfo.InvariantCulture);
            caretIndex = Math.Min(caretIndex, text.Length);
        }

        protected virtual void OnTextChanged()
        {
            TextChanged?.Invoke(this);
            mouseSelecting = false;
        }

        protected void Cut()
        {
            if (selectionLength > 0)
            {
                Platform.SetClipboardString(text.Substring(selectionStart, selectionLength));
                DeleteSelection();
            }
        }

        protected void Copy()
        {
            if (selectionLength > 0)
                Platform.SetClipboardString(text.Substring(selectionStart, selectionLength));
        }

        protected void Paste()
        {
            InsertText(Platform.GetClipboardString());
        }

        protected void UpdateScrollParams()
        {
            maxScrollX = Math.Max(0, Fonts.FontMedium.MeasureString(text, false) - (textAreaWidth - innerMargin * 2));
            scrollX = Utils.Clamp(scrollX, 0, maxScrollX);
        }

        protected override void OnPointerMove(PointerEventArgs e)
        {
            Cursor = enabled && e.X > outerMarginLeft && e.X < (width - outerMarginRight) ? Cursors.IBeam : Cursors.Default;

            if (mouseSelecting && !e.IsTouchEvent)
            {
                var c = PixelToChar(e.X - outerMarginLeft);
                var selMin = Math.Min(mouseSelectionChar, c);
                var selMax = Math.Max(mouseSelectionChar, c);

                SetAndMarkDirty(ref caretIndex, c);
                SetAndMarkDirty(ref selectionStart, selMin);
                SetAndMarkDirty(ref selectionLength, selMax - selMin);
                Debug.Assert(selectionLength <= text.Length);
                EnsureCaretVisible();
            }
        }

        protected override void OnPointerDown(PointerEventArgs e)
        {
            if (enabled && !e.IsTouchEvent)
            {
                if (e.Left)
                {
                    var c = PixelToChar(e.X - outerMarginLeft);
                    SetAndMarkDirty(ref caretIndex, c);
                    SetAndMarkDirty(ref selectionStart, c);
                    SetAndMarkDirty(ref selectionLength, 0);
                    ClearSelection();
                    ResetCaretBlink();
                    CapturePointer();

                    mouseSelectionChar = c;
                    mouseSelecting = true;
                }
            }
        }

        protected override void OnPointerUp(PointerEventArgs e)
        {
            if (enabled && !e.IsTouchEvent)
            {
                if (e.Left)
                {
                    mouseSelecting = false;
                    ReleasePointer();
                }
                else if (e.Right)
                {
                    GrabDialogFocus();
                    App.ShowContextMenuAsync(new[]
                    {
                        new ContextMenuOption("MenuCut",   CutName,   () => Cut()),
                        new ContextMenuOption("MenuCopy",  CopyName,  () => Copy()),
                        new ContextMenuOption("MenuPaste", PasteName, () => Paste()),
                    });
                }
            }
        }

        protected override void OnMouseDoubleClick(PointerEventArgs e)
        {
            if (e.Left && enabled)
            { 
                var c0 = PixelToChar(e.X - outerMarginLeft);
                var c1 = c0;

                c0 = FindWordStart(c0, -1);
                c1 = FindWordStart(c1,  1);

                selectionStart  = c0;
                selectionLength = c1 - c0;
                caretIndex      = c1;
                Debug.Assert(selectionLength <= text.Length);

                MarkDirty();
            }
        }

        protected int FindWordStart(int c, int dir)
        {
            if (string.IsNullOrEmpty(text))
                return 0;

            c = Math.Clamp(c, 0, text.Length - 1); 
            
            var isWhiteSpace = char.IsWhiteSpace(text[c]);

            if (dir > 0)
            {
                while (c < text.Length && char.IsWhiteSpace(text[c]))
                    c++;

                if (!isWhiteSpace)
                {
                    while (c < text.Length && !char.IsWhiteSpace(text[c]))
                        c++;
                }
            }
            else
            {
                if (c < text.Length && char.IsWhiteSpace(text[c]))
                {
                    while (c >= 1 && char.IsWhiteSpace(text[c - 1]))
                        c--;
                }

                if (!isWhiteSpace)
                {
                    while (c >= 1 && !char.IsWhiteSpace(text[c - 1]))
                        c--;
                }
            }

            Debug.Assert(c >= 0 && c <= text.Length);
            return c;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (!enabled)
                return;

            if (e.Key == Keys.Left || e.Key == Keys.Right || e.Key == Keys.Home || e.Key == Keys.End)
            {
                var sign = e.Key == Keys.Left ? -1 : 1;
                var prevCaretIndex = caretIndex;

                if (e.Key == Keys.Left || e.Key == Keys.Right)
                { 
                    caretIndex = Utils.Clamp(caretIndex + sign, 0, text.Length);

                    if (e.Control || e.Alt)
                        caretIndex = FindWordStart(caretIndex, sign);
                }
                else if (e.Key == Keys.Home)
                { 
                    caretIndex = 0;
                }
                else
                { 
                    caretIndex = text.Length;
                }

                if (e.Shift)
                {
                    var minCaret = Math.Min(prevCaretIndex, caretIndex);
                    var maxCaret = Math.Max(prevCaretIndex, caretIndex);

                    if (selectionLength == 0)
                    {
                        SetSelection(minCaret, maxCaret - minCaret);
                    }
                    else 
                    {
                        var selMin = selectionStart;
                        var selMax = selectionStart + selectionLength;

                        // This seem WAYYYY over complicated.
                        if (caretIndex < selMax && prevCaretIndex < selMax)
                            SetSelection(caretIndex, selMax - caretIndex);
                        else if (caretIndex >= selMin && prevCaretIndex > selMin)
                            SetSelection(selMin, caretIndex - selMin);
                        else if (caretIndex < selMin && prevCaretIndex > selMin)
                            SetSelection(caretIndex, selMin - caretIndex);
                        else if (caretIndex >= selMax && prevCaretIndex < selMax)
                            SetSelection(selMax, caretIndex - selMax);
                    }
                }
                else
                {
                    ClearSelection();
                }

                ResetCaretBlink();
                EnsureCaretVisible();
                MarkDirty();
            }
            else if (e.Key == Keys.A && e.Control)
            {
                SelectAll();
            }
            else if (e.Key == Keys.Backspace)
            {
                if (!DeleteSelection() && caretIndex > 0)
                {
                    caretIndex--;
                    SaveUndoRedoState();
                    text = RemoveStringRange(caretIndex, 1);
                    OnTextChanged();
                    UpdateScrollParams();
                    MarkDirty();
                }
            }
            else if (e.Key == Keys.Delete)
            {
                if (!DeleteSelection() && caretIndex < text.Length)
                {
                    SaveUndoRedoState();
                    text = RemoveStringRange(caretIndex, 1);
                    OnTextChanged();
                    UpdateScrollParams();
                    MarkDirty();
                }
            }
            else if (e.Key == Keys.Escape)
            {
                ClearDialogFocus();
                e.Handled = true;
            }
            else if (e.Key == Keys.V && e.Control)
            {
                Paste();
            }
            else if (e.Key == Keys.C && e.Control)
            {
                Copy();
            }
            else if (e.Key == Keys.X && e.Control)
            {
                Cut();
            }
            else if (e.Key == Keys.Z && e.Control)
            {
                Undo();
            }
            else if (e.Key == Keys.Y && e.Control)
            {
                Redo();
            }
            else if (e.Key == Keys.Tab)
            {
                SendFocusToNextTextBox();
            }
        }

        private void SendFocusToNextTextBox()
        {
            // This is super hacky, we dont have any proper focus management,
            // so we just look for the next text box in the list. Also, we 
            // assume controls are in an order that makes sense.
            var ctrls = container.Controls.ToArray();
            var idx = Array.IndexOf(ctrls, this);
            var nextIdx = -1;

            for (var i = 1; i < ctrls.Length; i++)
            {
                var j = (i + idx) % ctrls.Length;

                if (ctrls[j] is TextBox && ctrls[j].Visible && ctrls[j].Enabled)
                {
                    nextIdx = j;
                    break;
                }
            }

            if (nextIdx >= 0)
            {
                ParentDialog.FocusedControl = ctrls[nextIdx];
            }
            else
            {
                ClearDialogFocus();
                GrabDialogFocus();
            }
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
        }

        protected override void OnChar(CharEventArgs e)
        {
            if (enabled)
            {
                InsertText(e.Char.ToString());
            }
        }

        protected override void OnTouchClick(PointerEventArgs e)
        {
            if (allowMobileEdit && enabled)
            {
                Platform.EditTextAsync(prompt, text, (s) => SetAndMarkDirty(ref text, s));
            }
        }

        protected void InsertText(string str)
        {
            str = FilterString(str);

            if (!string.IsNullOrEmpty(str))
            {
                // Deleting selection saves the state, we don't need to save it twice.
                if (selectionLength > 0)
                    DeleteSelection();
                else
                    SaveUndoRedoState();

                if (maxLength > 0 && text.Length + str.Length > maxLength)
                    str = str.Substring(0, Math.Min(str.Length, Math.Max(0, maxLength - text.Length)));
                text = text.Insert(caretIndex, str);
                caretIndex += str.Length;
                UpdateScrollParams();
                EnsureCaretVisible();
                OnTextChanged();
                ClearSelection();
                MarkDirty();
            }
        }

        public override void Tick(float delta)
        {
            caretBlinkTime += delta;
            SetAndMarkDirty(ref caretBlink, Utils.Frac(caretBlinkTime) < 0.5f);

            if (!HasDialogFocus)
                SetTickEnabled(false);
        }

        protected void UpdateCaretBlink()
        {
            SetAndMarkDirty(ref caretBlink, Utils.Frac(caretBlinkTime) < 0.5f);
        }

        protected void ResetCaretBlink()
        {
            caretBlinkTime = 0;
            UpdateCaretBlink();
        }

        protected void EnsureCaretVisible()
        {
            var px = CharToPixel(caretIndex, false);
            if (px < 0)
                SetAndMarkDirty(ref scrollX, Utils.Clamp(scrollX + px - scrollAmount, 0, maxScrollX));
            else if (px > textAreaWidthNoMargin)
                SetAndMarkDirty(ref scrollX, Utils.Clamp(scrollX + px - textAreaWidthNoMargin + scrollAmount, 0, maxScrollX));
        }

        protected int PixelToChar(int x, bool margin = true)
        {
            return Fonts.FontMedium.GetNumCharactersForSize(text, x - (margin ? innerMargin : 0) + scrollX, true);
        }

        protected int CharToPixel(int c, bool margin = true)
        {
            var px = (margin ? innerMargin : 0) - scrollX;
            if (c > 0)
                px += Fonts.FontMedium.MeasureString(text.Substring(0, c), false);
            return px;
        }

        public void SetSelection(int start, int len)
        {
            caretIndex = Math.Clamp(caretIndex, start, start + len);
            SetAndMarkDirty(ref selectionStart, start);
            SetAndMarkDirty(ref selectionLength, Math.Max(0, len));
        }

        public void ClearSelection()
        {
            SetAndMarkDirty(ref selectionStart, 0);
            SetAndMarkDirty(ref selectionLength, 0);
        }

        public void SelectAll()
        {
            selectionStart = 0;
            selectionLength = text.Length;
            caretIndex = text.Length;
            Debug.Assert(selectionLength <= text.Length);
            MarkDirty();
        }

        protected string RemoveStringRange(int start, int len)
        {
            var left  = text.Substring(0, start);
            var right = text.Substring(start + len);

            return left + right;
        }

        protected bool DeleteSelection()
        {
            if (selectionLength > 0)
            {
                SaveUndoRedoState();

                text = RemoveStringRange(selectionStart, selectionLength);

                if (caretIndex > selectionStart)
                    caretIndex -= selectionLength;

                OnTextChanged();
                ClearSelection();
                UpdateScrollParams();

                return true;
            }

            return false;
        }

        protected void SaveUndoRedoState()
        {
            if (undoRedoIndex != undoRedo.Count)
                undoRedo.RemoveRange(undoRedoIndex, undoRedo.Count - undoRedoIndex);

            var state = new TextBoxState();
            state.caretIndex = caretIndex;
            state.selStart = selectionStart;
            state.selLen = selectionLength;
            state.text = text;
            undoRedo.Add(state);
            undoRedoIndex++;
        }

        protected void RestoreUndoRedoState(TextBoxState state)
        {
            text = state.text;
            caretIndex = state.caretIndex;
            selectionStart = state.selStart;
            selectionLength = state.selLen;
            OnTextChanged();
            EnsureCaretVisible();
            MarkDirty();
        }

        protected void Undo()
        {
            if (undoRedoIndex > 0)
            {
                RestoreUndoRedoState(undoRedo[--undoRedoIndex]);
            }
        }

        protected void Redo()
        {
            if (undoRedoIndex < undoRedo.Count - 1)
            {
                RestoreUndoRedoState(undoRedo[++undoRedoIndex]);
            }
        }

        protected void UpdateTextAreaSize()
        {
            textAreaWidth = width - (outerMarginLeft + outerMarginRight);
            textAreaWidthNoMargin = textAreaWidth - innerMargin * 2;
            UpdateScrollParams();
            MarkDirty();
        }

        protected override void OnAddedToContainer()
        {
            UpdateTextAreaSize();
        }

        protected override void OnAcquiredDialogFocus()
        {
            SetTickEnabled(true);
        }

        protected override void OnResize(EventArgs e)
        {
            if (window != null)
            {
                UpdateTextAreaSize();
            }
        }

        protected override void OnRender(Graphics g)
        {
            var c = g.GetCommandList();

            c.PushTranslation(outerMarginLeft, 0);
            c.FillAndDrawRectangle(0, 0, textAreaWidth - 1, height - 1, backColor, enabled ? foreColor : disabledColor);
            
            if (selectionLength > 0 && HasDialogFocus && enabled)
            {
                var sx0 = Utils.Clamp(CharToPixel(selectionStart), innerMargin, textAreaWidth - innerMargin);
                var sx1 = selectionLength > 0 ? Utils.Clamp(CharToPixel(selectionStart + selectionLength), innerMargin, textAreaWidth - innerMargin) : sx0;

                if (sx0 != sx1)
                    c.FillRectangle(sx0, topMargin, sx1, height - topMargin, selColor);
            }

            c.DrawText(text, Fonts.FontMedium, innerMargin - scrollX, 0, enabled ? foreColor : disabledColor, TextFlags.MiddleLeft | TextFlags.Clip, 0, height, innerMargin, textAreaWidth - innerMargin);

            if (caretBlink && HasDialogFocus && enabled)
            {
                var cx = CharToPixel(caretIndex);
                c.DrawLine(cx, topMargin, cx, height - topMargin, foreColor);
            }

            c.PopTransform();
        }
    }
}
