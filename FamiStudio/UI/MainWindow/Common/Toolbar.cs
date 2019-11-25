using System;
using System.Collections.Generic;
using System.Windows.Forms;
using FamiStudio.Properties;

#if FAMISTUDIO_WINDOWS
    using RenderBitmap   = SharpDX.Direct2D1.Bitmap;
    using RenderBrush    = SharpDX.Direct2D1.Brush;
    using RenderControl  = FamiStudio.Direct2DControl;
    using RenderGraphics = FamiStudio.Direct2DGraphics;
    using RenderTheme    = FamiStudio.Direct2DTheme;
#else
    using RenderBitmap   = FamiStudio.GLBitmap;
    using RenderBrush    = FamiStudio.GLBrush;
    using RenderControl  = FamiStudio.GLControl;
    using RenderGraphics = FamiStudio.GLGraphics;
    using RenderTheme    = FamiStudio.GLTheme;
#endif

namespace FamiStudio
{
    public class Toolbar : RenderControl
    {
        const int ButtonNew    = 0;
        const int ButtonOpen   = 1;
        const int ButtonSave   = 2;
        const int ButtonExport = 3;
        const int ButtonCopy   = 4;
        const int ButtonCut    = 5;
        const int ButtonPaste  = 6;
        const int ButtonUndo   = 7;
        const int ButtonRedo   = 8;
        const int ButtonConfig = 9;
        const int ButtonPlay   = 10;
        const int ButtonRewind = 11;
        const int ButtonLoop   = 12;
        const int ButtonCount  = 13;

        const int DefaultTimecodePosX            = 415;
        const int DefaultTimecodePosY            = 4;
        const int DefaultTimecodeSizeX           = 160;
        const int DefaultTimecodeTextPosX        = 30;
        const int DefaultTooltipSingleLinePosY   = 12;
        const int DefaultTooltipMultiLinePosY    = 4;
        const int DefaultTooltipLineSizeY        = 17;
        const int DefaultTooltipSpecialCharSizeX = 16;
        const int DefaultTooltipSpecialCharSizeY = 14;

        int timecodePosX;
        int timecodePosY;
        int timecodeSizeX;
        int timecodeTextPosX;
        int tooltipSingleLinePosY;
        int tooltipMultiLinePosY;
        int tooltipLineSizeY;
        int tooltipSpecialCharSizeX;
        int tooltipSpecialCharSizeY;

        private delegate void EmptyDelegate();
        private delegate bool BoolDelegate();
        private delegate RenderBitmap BitmapDelegate();

        class Button
        {
            public int X;
            public int Y;
            public int Size = 32;
            public string ToolTip;
            public RenderBitmap Bmp;
            public BoolDelegate Enabled;
            public EmptyDelegate Click;
            public EmptyDelegate RightClick;
            public BitmapDelegate GetBitmap;
            public bool IsPointIn(int px, int py) => px >= X && (px - X) < Size && py >= Y && (py - Y) < Size;
        };

        class TooltipSpecialCharacter
        {
            public RenderBitmap Bmp;
            public int Width;
            public int Height;
            public float OffsetY;
        };

        string tooltip = "";
        RenderTheme theme;
        RenderBrush toolbarBrush;
        RenderBitmap bmpLoopNone;
        RenderBitmap bmpLoopSong;
        RenderBitmap bmpLoopPattern;
        RenderBitmap bmpPlay;
        RenderBitmap bmpPause;
        Button[] buttons = new Button[ButtonCount];
        Dictionary<string, TooltipSpecialCharacter> specialCharacters = new Dictionary<string, TooltipSpecialCharacter>();

        protected override void OnRenderInitialized(RenderGraphics g)
        {
            theme = RenderTheme.CreateResourcesForGraphics(g);
            toolbarBrush = g.CreateHorizontalGradientBrush(0, 81, ThemeBase.LightGreyFillColor1, ThemeBase.LightGreyFillColor2);

            bmpLoopNone = g.CreateBitmapFromResource("LoopNone");
            bmpLoopSong = g.CreateBitmapFromResource("Loop");
            bmpLoopPattern = g.CreateBitmapFromResource("LoopPattern");
            bmpPlay = g.CreateBitmapFromResource("Play");
            bmpPause = g.CreateBitmapFromResource("Pause");

            buttons[ButtonNew]    = new Button { X = 4,   Y = 4, Bmp = g.CreateBitmapFromResource("File"), Click = OnNew };
            buttons[ButtonOpen]   = new Button { X = 44,  Y = 4, Bmp = g.CreateBitmapFromResource("Open"), Click = OnOpen };
            buttons[ButtonSave]   = new Button { X = 84,  Y = 4, Bmp = g.CreateBitmapFromResource("Save"), Click = OnSave, RightClick = OnSaveAs };
            buttons[ButtonExport] = new Button { X = 124, Y = 4, Bmp = g.CreateBitmapFromResource("Export"), Click = OnExport };
            buttons[ButtonCopy]   = new Button { X = 164, Y = 4, Bmp = g.CreateBitmapFromResource("Copy"), Click = OnCopy, Enabled = OnCopyEnabled };
            buttons[ButtonCut]    = new Button { X = 204, Y = 4, Bmp = g.CreateBitmapFromResource("Cut"), Click = OnCut, Enabled = OnCutEnabled };
            buttons[ButtonPaste]  = new Button { X = 244, Y = 4, Bmp = g.CreateBitmapFromResource("Paste"), Click = OnPaste, RightClick = OnPasteSpecial, Enabled = OnPasteEnabled };
            buttons[ButtonUndo]   = new Button { X = 284, Y = 4, Bmp = g.CreateBitmapFromResource("Undo"), Click = OnUndo, Enabled = OnUndoEnabled };
            buttons[ButtonRedo]   = new Button { X = 324, Y = 4, Bmp = g.CreateBitmapFromResource("Redo"), Click = OnRedo, Enabled = OnRedoEnabled };
            buttons[ButtonConfig] = new Button { X = 364, Y = 4, Bmp = g.CreateBitmapFromResource("Config"), Click = OnConfig };
            buttons[ButtonPlay]   = new Button { X = 594, Y = 4, Click = OnPlay, GetBitmap = OnPlayGetBitmap };
            buttons[ButtonRewind] = new Button { X = 634, Y = 4, Bmp = g.CreateBitmapFromResource("Rewind"), Click = OnRewind };
            buttons[ButtonLoop]   = new Button { X = 674, Y = 4, Click = OnLoop, GetBitmap = OnLoopGetBitmap };

            buttons[ButtonNew].ToolTip    = "{MouseLeft} New Project {Ctrl} {N}";
            buttons[ButtonOpen].ToolTip   = "{MouseLeft} Open Project {Ctrl} {O}";
            buttons[ButtonSave].ToolTip   = "{MouseLeft} Save Project {Ctrl} {S}  - {MouseRight} Save As...";
            buttons[ButtonExport].ToolTip = "{MouseLeft} Export to various formats {Ctrl} {E}";
            buttons[ButtonCopy].ToolTip   = "{MouseLeft} Copy selection {Ctrl} {C}";
            buttons[ButtonCut].ToolTip    = "{MouseLeft} Cut selection {Ctrl} {X}";
            buttons[ButtonPaste].ToolTip  = "{MouseLeft} Paste {Ctrl} {V}\n{MouseRight} Paste Special... {Ctrl} {Shift} {V}";
            buttons[ButtonUndo].ToolTip   = "{MouseLeft} Undo {Ctrl} {Z}";
            buttons[ButtonRedo].ToolTip   = "{MouseLeft} Redo {Ctrl} {Y}";
            buttons[ButtonConfig].ToolTip = "{MouseLeft} Edit Application Settings";
            buttons[ButtonPlay].ToolTip   = "{MouseLeft} Play/Pause {Space}\nPlay pattern loop {Ctrl} {Space}  - Play song loop {Shift} {Space}";
            buttons[ButtonRewind].ToolTip = "{MouseLeft} Rewind {Home}\nRewind to beginning of current pattern {Ctrl} {Home}";
            buttons[ButtonLoop].ToolTip   = "{MouseLeft} Toggle Loop Mode";

            var scaling = RenderTheme.MainWindowScaling;

            for (int i = 0; i < ButtonCount; i++)
            {
                var btn = buttons[i];
                btn.X = (int)(btn.X * scaling);
                btn.Y = (int)(btn.Y * scaling);
                btn.Size = (int)(btn.Size * scaling);
            }

            timecodePosX            = (int)(DefaultTimecodePosX            * scaling);
            timecodePosY            = (int)(DefaultTimecodePosY            * scaling);
            timecodeSizeX           = (int)(DefaultTimecodeSizeX           * scaling);
            timecodeTextPosX        = (int)(DefaultTimecodeTextPosX        * scaling);
            tooltipSingleLinePosY   = (int)(DefaultTooltipSingleLinePosY   * scaling);
            tooltipMultiLinePosY    = (int)(DefaultTooltipMultiLinePosY    * scaling);
            tooltipLineSizeY        = (int)(DefaultTooltipLineSizeY        * scaling);
            tooltipSpecialCharSizeX = (int)(DefaultTooltipSpecialCharSizeX * scaling);
            tooltipSpecialCharSizeY = (int)(DefaultTooltipSpecialCharSizeY * scaling);

            specialCharacters["Shift"]      = new TooltipSpecialCharacter { Width = (int)(32 * scaling) };
            specialCharacters["Space"]      = new TooltipSpecialCharacter { Width = (int)(38 * scaling) };
            specialCharacters["Home"]       = new TooltipSpecialCharacter { Width = (int)(38 * scaling) };
            specialCharacters["Ctrl"]       = new TooltipSpecialCharacter { Width = (int)(28 * scaling) };
            specialCharacters["Alt"]        = new TooltipSpecialCharacter { Width = (int)(24 * scaling) };
            specialCharacters["Drag"]       = new TooltipSpecialCharacter { Bmp = g.CreateBitmapFromResource("Drag"),       OffsetY = 2 * scaling };
            specialCharacters["MouseLeft"]  = new TooltipSpecialCharacter { Bmp = g.CreateBitmapFromResource("MouseLeft"),  OffsetY = 2 * scaling };
            specialCharacters["MouseRight"] = new TooltipSpecialCharacter { Bmp = g.CreateBitmapFromResource("MouseRight"), OffsetY = 2 * scaling };
            specialCharacters["MouseWheel"] = new TooltipSpecialCharacter { Bmp = g.CreateBitmapFromResource("MouseWheel"), OffsetY = 2 * scaling };

            for (char i = 'A'; i <= 'Z'; i++)
                specialCharacters[i.ToString()] = new TooltipSpecialCharacter { Width = tooltipSpecialCharSizeX };

            foreach (var specialChar in specialCharacters.Values)
            {
                if (specialChar.Bmp != null)
                    specialChar.Width = (int)g.GetBitmapWidth(specialChar.Bmp);
                specialChar.Height = tooltipSpecialCharSizeY;
            }
        }

        public string ToolTip
        {
            get { return tooltip; }
            set
            {
                if (tooltip != value)
                {
                    tooltip = value;
                    ConditionalInvalidate();
                }
            }
        }

        public void Reset()
        {
            tooltip = "";
        }

        private void ConditionalInvalidate()
        {
            if (App != null && !App.RealTimeUpdate)
                Invalidate();
        }

        private void OnNew()
        {
            App.NewProject();
        }

        private void OnOpen()
        {
            App.OpenProject();
        }

        private void OnSave()
        {
            App.SaveProject();
        }

        private void OnSaveAs()
        {
            App.SaveProject(true);
        }

        private void OnExport()
        {
            App.Export();
        }

        private void OnCut()
        {
            App.Cut();
        }

        private bool OnCutEnabled()
        {
            return App.CanCopy;
        }

        private void OnCopy()
        {
            App.Copy();
        }

        private bool OnCopyEnabled()
        {
            return App.CanCopy;
        }

        private void OnPaste()
        {
            App.Paste();
        }

        private void OnPasteSpecial()
        {
            App.PasteSpecial();
        }

        private bool OnPasteEnabled()
        {
            return App.CanPaste;
        }

        private void OnUndo()
        {
            App.UndoRedoManager.Undo();
        }

        private bool OnUndoEnabled()
        {
            return App.UndoRedoManager != null && App.UndoRedoManager.UndoScope != TransactionScope.Max;
        }

        private void OnRedo()
        {
            App.UndoRedoManager.Redo();
        }

        private bool OnRedoEnabled()
        {
            return App.UndoRedoManager != null && App.UndoRedoManager.RedoScope != TransactionScope.Max;
        }

        private void OnConfig()
        {
            App.OpenConfigDialog();
        }

        private void OnPlay()
        {
            if (App.IsPlaying)
                App.Stop();
            else
                App.Play();
        }

        private RenderBitmap OnPlayGetBitmap()
        {
            return App.IsPlaying ? bmpPause : bmpPlay;
        }

        private void OnRewind()
        {
            App.Stop();
            App.Seek(0);
        }

        private void OnLoop()
        {
            App.LoopMode = (LoopMode)(((int)App.LoopMode + 1) % (int)LoopMode.Max);
        }

        private RenderBitmap OnLoopGetBitmap()
        {
            switch (App.LoopMode)
            {
                case LoopMode.None: return bmpLoopNone;
                case LoopMode.Song: return bmpLoopSong;
                case LoopMode.Pattern: return bmpLoopPattern;
            }

            return null;
        }

        protected override void OnRender(RenderGraphics g)
        {
            g.FillRectangle(0, 0, Width, Height, toolbarBrush);

            var scaling = RenderTheme.MainWindowScaling;
            var pt = this.PointToClient(Cursor.Position);

            // Buttons
            foreach (var btn in buttons)
            {
                bool hover = btn.IsPointIn(pt.X, pt.Y);
                var bmp = btn.GetBitmap != null ? btn.GetBitmap() : btn.Bmp;
                if (bmp == null)
                    bmp = btn.Bmp;
                bool enabled = btn.Enabled != null ? btn.Enabled() : true;
                g.DrawBitmap(bmp, btn.X, btn.Y, enabled ? (hover ? 0.75f : 1.0f) : 0.25f);
            }

            // Timecode
            int frame = App.CurrentFrame;
            int patternIdx = frame / App.Song.PatternLength;
            int noteIdx = frame % App.Song.PatternLength;

            g.FillAndDrawRectangle(timecodePosX, timecodePosY, timecodePosX + timecodeSizeX, Height - timecodePosY, theme.DarkGreyFillBrush1, theme.BlackBrush);
            g.DrawText($"{patternIdx:D3}:{noteIdx:D3}", ThemeBase.FontHuge, timecodePosX + timecodeTextPosX, 2, theme.LightGreyFillBrush1, timecodeSizeX);

            // Tooltip
            if (!string.IsNullOrEmpty(tooltip))
            {
                var lines = tooltip.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                var posY = lines.Length == 1 ? tooltipSingleLinePosY : tooltipMultiLinePosY;

                for (int j = 0; j < lines.Length; j++)
                {
                    var splits = lines[j].Split(new char[] { '{', '}' }, StringSplitOptions.RemoveEmptyEntries);
                    var posX = Width - 8 * RenderTheme.MainWindowScaling;

                    for (int i = splits.Length - 1; i >= 0; i--)
                    {
                        var str = splits[i];

                        if (specialCharacters.TryGetValue(str, out var specialCharacter))
                        {
                            posX -= specialCharacter.Width;

                            if (specialCharacter.Bmp != null)
                            {
                                g.DrawBitmap(specialCharacter.Bmp, posX, posY + specialCharacter.OffsetY);
                            }
                            else
                            {
                                g.DrawRectangle(posX, posY + specialCharacter.OffsetY, posX + specialCharacter.Width, posY + specialCharacter.Height + specialCharacter.OffsetY, theme.BlackBrush, RenderTheme.MainWindowScaling * 2 - 1);
                                g.DrawText(str, ThemeBase.FontMediumCenter, posX, posY, theme.BlackBrush, specialCharacter.Width);
                            }
                        }
                        else
                        {
                            posX -= g.MeasureString(splits[i], ThemeBase.FontMedium);
                            g.DrawText(str, ThemeBase.FontMedium, posX, posY, theme.BlackBrush);
                        }
                    }

                    posY += tooltipLineSizeY;
                }
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            ConditionalInvalidate();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            ConditionalInvalidate();

            foreach (var btn in buttons)
            {
                if (btn.IsPointIn(e.X, e.Y))
                {
                    ToolTip = btn.ToolTip;
                    return;
                }
            }

            ToolTip = "";
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            bool left  = e.Button.HasFlag(MouseButtons.Left);
            bool right = e.Button.HasFlag(MouseButtons.Right);

            if (left || right)
            {
                foreach (var btn in buttons)
                {
                    if (btn != null && btn.IsPointIn(e.X, e.Y) && (btn.Enabled == null || btn.Enabled()))
                    {
                        if (left)
                            btn.Click?.Invoke();
                        else
                            btn.RightClick?.Invoke();
                        break;
                    }
                }
            }
        }
    }
}
