using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Media;
using System.Windows.Forms;
using FamiStudio.Properties;

#if FAMISTUDIO_WINDOWS
    using RenderBitmap   = SharpDX.Direct2D1.Bitmap;
    using RenderBrush    = SharpDX.Direct2D1.Brush;
    using RenderGeometry = SharpDX.Direct2D1.PathGeometry;
    using RenderControl  = FamiStudio.Direct2DControl;
    using RenderGraphics = FamiStudio.Direct2DGraphics;
    using RenderTheme    = FamiStudio.Direct2DTheme;
#else
    using RenderBitmap   = FamiStudio.GLBitmap;
    using RenderBrush    = FamiStudio.GLBrush;
    using RenderGeometry = FamiStudio.GLGeometry;
    using RenderControl  = FamiStudio.GLControl;
    using RenderGraphics = FamiStudio.GLGraphics;
    using RenderTheme    = FamiStudio.GLTheme;
#endif

namespace FamiStudio
{
    public class Toolbar : RenderControl
    {
        const int ButtonNew       = 0;
        const int ButtonOpen      = 1;
        const int ButtonSave      = 2;
        const int ButtonExport    = 3;
        const int ButtonCopy      = 4;
        const int ButtonCut       = 5;
        const int ButtonPaste     = 6;
        const int ButtonUndo      = 7;
        const int ButtonRedo      = 8;
        const int ButtonTransform = 9;
        const int ButtonConfig    = 10;
        const int ButtonPlay      = 11;
        const int ButtonRec       = 12;
        const int ButtonRewind    = 13;
        const int ButtonLoop      = 14;
        const int ButtonQwerty    = 15;
        const int ButtonMachine   = 16;
        const int ButtonFollow    = 17;
        const int ButtonHelp      = 18;
        const int ButtonCount     = 19;

        const int DefaultTimecodeOffsetX         = 38; // Offset from config button.
        const int DefaultTimecodePosY            = 4;
        const int DefaultTimecodeSizeX           = 140;
        const int DefaultTooltipSingleLinePosY   = 12;
        const int DefaultTooltipMultiLinePosY    = 4;
        const int DefaultTooltipLineSizeY        = 17;
        const int DefaultTooltipSpecialCharSizeX = 16;
        const int DefaultTooltipSpecialCharSizeY = 14;
        const int DefaultButtonPosX              = 4;
        const int DefaultButtonPosY              = 4;
        const int DefaultButtonSizeX             = 32;
        const int DefaultButtonSpacingX          = 34;
        const int DefaultButtonTimecodeSpacingX  = 4; // Spacing before/after timecode.

        int timecodeOffsetX;
        int timecodePosX;
        int timecodePosY;
        int timecodeSizeX;
        int oscilloscopePosX;
        int tooltipSingleLinePosY;
        int tooltipMultiLinePosY;
        int tooltipLineSizeY;
        int tooltipSpecialCharSizeX;
        int tooltipSpecialCharSizeY;
        int buttonPosX;
        int buttonPosY;
        int buttonSizeX;
        int buttonSpacingX;
        int buttonTimecodeSpacingX;

        enum ButtonStatus
        {
            Enabled,
            Disabled,
            Dimmed
        }

        private delegate void MouseWheelDelegate(int delta);
        private delegate void EmptyDelegate();
        private delegate ButtonStatus ButtonStatusDelegate();
        private delegate RenderBitmap BitmapDelegate();

        class Button
        {
            public int X;
            public int Y;
            public bool RightAligned;
            public bool Visible = true;
            public int Size;
            public string ToolTip;
            public RenderBitmap Bmp;
            public ButtonStatusDelegate Enabled;
            public EmptyDelegate Click;
            public EmptyDelegate RightClick;
            public MouseWheelDelegate MouseWheel;
            public BitmapDelegate GetBitmap;
            public bool IsPointIn(int px, int py, int width)
            {
                int x = RightAligned ? width - X : X;
                return px >= x && (px - x) < Size && py >= Y && (py - Y) < Size;
            }
        };

        class TooltipSpecialCharacter
        {
            public RenderBitmap Bmp;
            public int Width;
            public int Height;
            public float OffsetY;
        };

        DateTime warningTime;
        string warning = "";

        int lastButtonX = 500;
        bool oscilloscopeVisible = false;
        bool lastOscilloscopeHadNonZeroSample = false;
        bool redTooltip = false;
        string tooltip = "";
        RenderTheme theme;
        RenderBrush toolbarBrush;
        RenderBrush warningBrush;
        RenderBrush seekBarBrush;
        RenderBitmap bmpLoopNone;
        RenderBitmap bmpLoopSong;
        RenderBitmap bmpLoopPattern;
        RenderBitmap bmpPlay;
        RenderBitmap bmpPlayHalf;
        RenderBitmap bmpPlayQuarter;
        RenderBitmap bmpPause;
        RenderBitmap bmpNtsc;
        RenderBitmap bmpPal;
        RenderBitmap bmpNtscToPal;
        RenderBitmap bmpPalToNtsc;
        RenderBitmap bmpRec;
        RenderBitmap bmpRecRed;
        Button[] buttons = new Button[ButtonCount];
        Dictionary<string, TooltipSpecialCharacter> specialCharacters = new Dictionary<string, TooltipSpecialCharacter>();

        protected override void OnRenderInitialized(RenderGraphics g)
        {
            theme = RenderTheme.CreateResourcesForGraphics(g);

            toolbarBrush = g.CreateVerticalGradientBrush(0, Height, ThemeBase.DarkGreyFillColor2, ThemeBase.DarkGreyFillColor1);
            warningBrush = g.CreateSolidBrush(System.Drawing.Color.FromArgb(205, 77, 64));
            seekBarBrush = g.CreateSolidBrush(ThemeBase.SeekBarColor);

            bmpLoopNone    = g.CreateBitmapFromResource("LoopNone");
            bmpLoopSong    = g.CreateBitmapFromResource("Loop");
            bmpLoopPattern = g.CreateBitmapFromResource("LoopPattern");
            bmpPlay        = g.CreateBitmapFromResource("Play");
            bmpPlayHalf    = g.CreateBitmapFromResource("PlayHalf");
            bmpPlayQuarter = g.CreateBitmapFromResource("PlayQuarter");
            bmpPause       = g.CreateBitmapFromResource("Pause");
            bmpNtsc        = g.CreateBitmapFromResource("NTSC");
            bmpPal         = g.CreateBitmapFromResource("PAL");
            bmpNtscToPal   = g.CreateBitmapFromResource("NTSCToPAL");
            bmpPalToNtsc   = g.CreateBitmapFromResource("PALToNTSC");
            bmpRec         = g.CreateBitmapFromResource("Rec");
            bmpRecRed      = g.CreateBitmapFromResource("RecRed");

            buttons[ButtonNew]       = new Button { Bmp = g.CreateBitmapFromResource("File"), Click = OnNew };
            buttons[ButtonOpen]      = new Button { Bmp = g.CreateBitmapFromResource("Open"), Click = OnOpen };
            buttons[ButtonSave]      = new Button { Bmp = g.CreateBitmapFromResource("Save"), Click = OnSave, RightClick = OnSaveAs };
            buttons[ButtonExport]    = new Button { Bmp = g.CreateBitmapFromResource("Export"), Click = OnExport, RightClick = OnRepeatLastExport };
            buttons[ButtonCopy]      = new Button { Bmp = g.CreateBitmapFromResource("Copy"), Click = OnCopy, Enabled = OnCopyEnabled };
            buttons[ButtonCut]       = new Button { Bmp = g.CreateBitmapFromResource("Cut"), Click = OnCut, Enabled = OnCutEnabled };
            buttons[ButtonPaste]     = new Button { Bmp = g.CreateBitmapFromResource("Paste"), Click = OnPaste, RightClick = OnPasteSpecial, Enabled = OnPasteEnabled };
            buttons[ButtonUndo]      = new Button { Bmp = g.CreateBitmapFromResource("Undo"), Click = OnUndo, Enabled = OnUndoEnabled };
            buttons[ButtonRedo]      = new Button { Bmp = g.CreateBitmapFromResource("Redo"), Click = OnRedo, Enabled = OnRedoEnabled };
            buttons[ButtonTransform] = new Button { Bmp = g.CreateBitmapFromResource("Transform"), Click = OnTransform };
            buttons[ButtonConfig]    = new Button { Bmp = g.CreateBitmapFromResource("Config"), Click = OnConfig };
            buttons[ButtonPlay]      = new Button { Click = OnPlay, MouseWheel = OnPlayMouseWheel, GetBitmap = OnPlayGetBitmap };
            buttons[ButtonRec]       = new Button { GetBitmap = OnRecordGetBitmap, Click = OnRecord };
            buttons[ButtonRewind]    = new Button { Bmp = g.CreateBitmapFromResource("Rewind"), Click = OnRewind };
            buttons[ButtonLoop]      = new Button { Click = OnLoop, GetBitmap = OnLoopGetBitmap };
            buttons[ButtonQwerty]    = new Button { Bmp = g.CreateBitmapFromResource("QwertyPiano"), Click = OnQwerty, Enabled = OnQwertyEnabled };
            buttons[ButtonMachine]   = new Button { Click = OnMachine, GetBitmap = OnMachineGetBitmap, Enabled = OnMachineEnabled };
            buttons[ButtonFollow]    = new Button { Bmp = g.CreateBitmapFromResource("Follow"), Click = OnFollow, Enabled = OnFollowEnabled };
            buttons[ButtonHelp]      = new Button { Bmp = g.CreateBitmapFromResource("Help"), RightAligned = true, Click = OnHelp };

            buttons[ButtonNew].ToolTip       = "{MouseLeft} New Project {Ctrl} {N}";
            buttons[ButtonOpen].ToolTip      = "{MouseLeft} Open Project {Ctrl} {O}";
            buttons[ButtonSave].ToolTip      = "{MouseLeft} Save Project {Ctrl} {S}\n{MouseRight} Save As...";
            buttons[ButtonExport].ToolTip    = "{MouseLeft} Export to various formats {Ctrl} {E}\n{MouseRight} Repeat last export {Ctrl} {Shift} {E}";
            buttons[ButtonCopy].ToolTip      = "{MouseLeft} Copy selection {Ctrl} {C}";
            buttons[ButtonCut].ToolTip       = "{MouseLeft} Cut selection {Ctrl} {X}";
            buttons[ButtonPaste].ToolTip     = "{MouseLeft} Paste {Ctrl} {V}\n{MouseRight} Paste Special... {Ctrl} {Shift} {V}";
            buttons[ButtonUndo].ToolTip      = "{MouseLeft} Undo {Ctrl} {Z}";
            buttons[ButtonRedo].ToolTip      = "{MouseLeft} Redo {Ctrl} {Y}";
            buttons[ButtonTransform].ToolTip = "{MouseLeft} Perform cleanup and various operations";
            buttons[ButtonConfig].ToolTip    = "{MouseLeft} Edit Application Settings";
            buttons[ButtonPlay].ToolTip      = "{MouseLeft} Play/Pause {Space} - {MouseWheel} Change play rate - Play from start of pattern {Ctrl} {Space}\nPlay from start of song {Shift} {Space} - Play from loop point {Ctrl} {Shift} {Space}";
            buttons[ButtonRewind].ToolTip    = "{MouseLeft} Rewind {Home}\nRewind to beginning of current pattern {Ctrl} {Home}";
            buttons[ButtonRec].ToolTip       = "{MouseLeft} Toggles recording mode {Enter}\nAbort recording {Esc}";
            buttons[ButtonLoop].ToolTip      = "{MouseLeft} Toggle Loop Mode";
            buttons[ButtonQwerty].ToolTip    = "{MouseLeft} Toggle QWERTY keyboard piano input {Shift} {Q}";
            buttons[ButtonMachine].ToolTip   = "{MouseLeft} Toggle between NTSC/PAL playback mode";
            buttons[ButtonFollow].ToolTip    = "{MouseLeft} Toggle follow mode {Shift} {F}";
            buttons[ButtonHelp].ToolTip      = "{MouseLeft} Online documentation";

            var scaling = RenderTheme.MainWindowScaling;

            timecodeOffsetX         = (int)(DefaultTimecodeOffsetX         * scaling);
            timecodePosY            = (int)(DefaultTimecodePosY            * scaling);
            timecodeSizeX           = (int)(DefaultTimecodeSizeX           * scaling);
            tooltipSingleLinePosY   = (int)(DefaultTooltipSingleLinePosY   * scaling);
            tooltipMultiLinePosY    = (int)(DefaultTooltipMultiLinePosY    * scaling);
            tooltipLineSizeY        = (int)(DefaultTooltipLineSizeY        * scaling);
            tooltipSpecialCharSizeX = (int)(DefaultTooltipSpecialCharSizeX * scaling);
            tooltipSpecialCharSizeY = (int)(DefaultTooltipSpecialCharSizeY * scaling);
            buttonPosX              = (int)(DefaultButtonPosX              * scaling);
            buttonPosY              = (int)(DefaultButtonPosY              * scaling);
            buttonSizeX             = (int)(DefaultButtonSizeX             * scaling);
            buttonSpacingX          = (int)(DefaultButtonSpacingX          * scaling);
            buttonTimecodeSpacingX  = (int)(DefaultButtonTimecodeSpacingX  * scaling);

            UpdateButtonLayout();

            specialCharacters["Shift"]      = new TooltipSpecialCharacter { Width = (int)(32 * scaling) };
            specialCharacters["Space"]      = new TooltipSpecialCharacter { Width = (int)(38 * scaling) };
            specialCharacters["Home"]       = new TooltipSpecialCharacter { Width = (int)(38 * scaling) };
            specialCharacters["Ctrl"]       = new TooltipSpecialCharacter { Width = (int)(28 * scaling) };
            specialCharacters["Alt"]        = new TooltipSpecialCharacter { Width = (int)(24 * scaling) };
            specialCharacters["Enter"]      = new TooltipSpecialCharacter { Width = (int)(38 * scaling) };
            specialCharacters["Esc"]        = new TooltipSpecialCharacter { Width = (int)(24 * scaling) };
            specialCharacters["Del"]        = new TooltipSpecialCharacter { Width = (int)(24 * scaling) };
            specialCharacters["Drag"]       = new TooltipSpecialCharacter { Bmp = g.CreateBitmapFromResource("Drag"),       OffsetY = 2 * scaling };
            specialCharacters["MouseLeft"]  = new TooltipSpecialCharacter { Bmp = g.CreateBitmapFromResource("MouseLeft"),  OffsetY = 2 * scaling };
            specialCharacters["MouseRight"] = new TooltipSpecialCharacter { Bmp = g.CreateBitmapFromResource("MouseRight"), OffsetY = 2 * scaling };
            specialCharacters["MouseWheel"] = new TooltipSpecialCharacter { Bmp = g.CreateBitmapFromResource("MouseWheel"), OffsetY = 2 * scaling };
            specialCharacters["Warning"]    = new TooltipSpecialCharacter { Bmp = g.CreateBitmapFromResource("Warning") };

            for (char i = 'A'; i <= 'Z'; i++)
                specialCharacters[i.ToString()] = new TooltipSpecialCharacter { Width = tooltipSpecialCharSizeX };
            for (char i = '0'; i <= '9'; i++)
                specialCharacters[i.ToString()] = new TooltipSpecialCharacter { Width = tooltipSpecialCharSizeX };

            specialCharacters["~"] = new TooltipSpecialCharacter { Width = tooltipSpecialCharSizeX };

            foreach (var specialChar in specialCharacters.Values)
            {
                if (specialChar.Bmp != null)
                    specialChar.Width = (int)g.GetBitmapWidth(specialChar.Bmp);
                specialChar.Height = tooltipSpecialCharSizeY;
            }
        }

        protected override void OnRenderTerminated()
        {
            theme.Terminate();

            Utils.DisposeAndNullify(ref toolbarBrush);
            Utils.DisposeAndNullify(ref warningBrush);
            Utils.DisposeAndNullify(ref seekBarBrush);
            Utils.DisposeAndNullify(ref bmpLoopNone);
            Utils.DisposeAndNullify(ref bmpLoopSong);
            Utils.DisposeAndNullify(ref bmpLoopPattern);
            Utils.DisposeAndNullify(ref bmpPlay);
            Utils.DisposeAndNullify(ref bmpPlayHalf);
            Utils.DisposeAndNullify(ref bmpPlayQuarter);
            Utils.DisposeAndNullify(ref bmpPause);
            Utils.DisposeAndNullify(ref bmpNtsc);
            Utils.DisposeAndNullify(ref bmpPal);
            Utils.DisposeAndNullify(ref bmpNtscToPal);
            Utils.DisposeAndNullify(ref bmpPalToNtsc);
            Utils.DisposeAndNullify(ref bmpRec);
            Utils.DisposeAndNullify(ref bmpRecRed);

            foreach (var b in buttons)
                Utils.DisposeAndNullify(ref b.Bmp);
            foreach (var c in specialCharacters.Values)
                Utils.DisposeAndNullify(ref c.Bmp);

            specialCharacters.Clear();
        }

        protected override void OnResize(EventArgs e)
        {
            UpdateButtonLayout();
            base.OnResize(e);
        }

        public void LayoutChanged()
        {
            UpdateButtonLayout();
            ConditionalInvalidate();
        }

        private void UpdateButtonLayout()
        {
            if (theme == null)
                return;

            // Hide a few buttons if the window is too small (out min "usable" resolution is ~1280x720).
            bool hideLessImportantButtons = Width < 1420 * RenderTheme.MainWindowScaling;
            bool hideOscilloscope = Width < 1250 * RenderTheme.MainWindowScaling;

            var posX = buttonPosX;

            for (int i = 0; i < ButtonCount; i++)
            {
                var btn = buttons[i];

                if (i == ButtonHelp)
                {
                    btn.X = buttonSizeX;
                }
                else
                {
                    btn.X = posX;
                    lastButtonX = posX + buttonSizeX;
                }

                btn.Y = buttonPosY;
                btn.Size = buttonSizeX;
                btn.Visible = !hideLessImportantButtons || i < ButtonCopy || i > ButtonRedo;

                if (i == ButtonConfig)
                {
                    posX += buttonSpacingX + timecodeSizeX + buttonTimecodeSpacingX * 2;

                    oscilloscopeVisible = Settings.ShowOscilloscope && !hideOscilloscope;
                    if (oscilloscopeVisible)
                        posX += timecodeSizeX + buttonTimecodeSpacingX * 2;
                }
                else if (btn.Visible)
                {
                    posX += buttonSpacingX;
                }
            }

            timecodePosX = buttons[ButtonConfig].X + timecodeOffsetX;
            oscilloscopePosX = timecodePosX + timecodeSizeX + buttonTimecodeSpacingX * 2;
        }

        public void SetToolTip(string msg, bool red = false)
        {
            if (tooltip != msg || red != redTooltip)
            {
                tooltip = msg;
                redTooltip = red;
                ConditionalInvalidate();
            }
        }

        public void DisplayWarning(string msg, bool beep)
        {
            warningTime = DateTime.Now;
            warning = "{Warning} " + msg;
            if (beep)
                SystemSounds.Beep.Play();
        }

        public void Tick()
        {
            if (!string.IsNullOrEmpty(warning))
                ConditionalInvalidate();
        }

        public void Reset()
        {
            tooltip = "";
            redTooltip = false;
        }

        public void ConditionalInvalidate()
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

        private void OnRepeatLastExport()
        {
            App.RepeatLastExport();
        }

        private void OnCut()
        {
            App.Cut();
        }

        private ButtonStatus OnCutEnabled()
        {
            return App.CanCopy ? ButtonStatus.Enabled : ButtonStatus.Disabled;
        }

        private void OnCopy()
        {
            App.Copy();
        }

        private ButtonStatus OnCopyEnabled()
        {
            return App.CanCopy ? ButtonStatus.Enabled : ButtonStatus.Disabled;
        }

        private void OnPaste()
        {
            App.Paste();
        }

        private void OnPasteSpecial()
        {
            App.PasteSpecial();
        }

        private ButtonStatus OnPasteEnabled()
        {
            return App.CanPaste ? ButtonStatus.Enabled : ButtonStatus.Disabled;
        }

        private void OnUndo()
        {
            App.UndoRedoManager.Undo();
        }

        private ButtonStatus OnUndoEnabled()
        {
            return App.UndoRedoManager != null && App.UndoRedoManager.UndoScope != TransactionScope.Max ? ButtonStatus.Enabled : ButtonStatus.Disabled;
        }

        private void OnRedo()
        {
            App.UndoRedoManager.Redo();
        }

        private ButtonStatus OnRedoEnabled()
        {
            return App.UndoRedoManager != null && App.UndoRedoManager.RedoScope != TransactionScope.Max ? ButtonStatus.Enabled : ButtonStatus.Disabled;
        }

        private void OnTransform()
        {
            App.OpenTransformDialog();
        }

        private void OnConfig()
        {
            App.OpenConfigDialog();
        }

        private void OnPlay()
        {
            if (App.IsPlaying)
                App.StopSong();
            else
                App.PlaySong();
        }

        private void OnPlayMouseWheel(int delta)
        {
            int rate = App.PlayRate;

            if (delta < 0)
                App.PlayRate = Utils.Clamp(rate * 2, 1, 4);
            else
                App.PlayRate = Utils.Clamp(rate / 2, 1, 4);

            ConditionalInvalidate();
        }

        private RenderBitmap OnPlayGetBitmap()
        {
            if (App.IsPlaying)
            {
                return bmpPause;
            }
            else
            {
                switch (App.PlayRate)
                {
                    case 2:  return bmpPlayHalf;
                    case 4:  return bmpPlayQuarter;
                    default: return bmpPlay;
                }
            }
        }

        private void OnRewind()
        {
            App.StopSong();
            App.SeekSong(0);
        }

        private RenderBitmap OnRecordGetBitmap()
        {
            return App.IsRecording ? bmpRecRed : bmpRec; 
        }

        private void OnRecord()
        {
            App.ToggleRecording();
        }

        private void OnLoop()
        {
            App.LoopMode = App.LoopMode == LoopMode.LoopPoint ? LoopMode.Pattern : LoopMode.LoopPoint;
        }

        private void OnQwerty()
        {
            App.ToggleQwertyPiano();
        }

        private ButtonStatus OnQwertyEnabled()
        {
            return App.IsQwertyPianoEnabled ? ButtonStatus.Enabled : ButtonStatus.Dimmed;
        }

        private RenderBitmap OnLoopGetBitmap()
        {
            switch (App.LoopMode)
            {
                case LoopMode.Pattern:
                    return bmpLoopPattern;
                default:
                    return App.Song.LoopPoint < 0 ? bmpLoopNone : bmpLoopSong;
            }
        }

        private void OnMachine()
        {
            App.PalPlayback = !App.PalPlayback;
        }

        private void OnFollow()
        {
            App.FollowModeEnabled = !App.FollowModeEnabled;
        }

        private ButtonStatus OnFollowEnabled()
        {
            return App.FollowModeEnabled ? ButtonStatus.Enabled : ButtonStatus.Dimmed;
        }

        private ButtonStatus OnMachineEnabled()
        {
            return App.Project != null && App.Project.ExpansionAudio == ExpansionType.None ? ButtonStatus.Enabled : ButtonStatus.Disabled;
        }

        private RenderBitmap OnMachineGetBitmap()
        {
            if (App.Project == null)
            {
                return bmpNtsc;
            }
            else if (App.Project.UsesFamiTrackerTempo)
            {
                return App.PalPlayback ? bmpPal : bmpNtsc;
            }
            else
            {
                if (App.Project.PalMode)
                    return App.PalPlayback ? bmpPal : bmpPalToNtsc;
                else
                    return App.PalPlayback ? bmpNtscToPal : bmpNtsc;
            }
        }

        private void OnHelp()
        {
            App.ShowHelp();
        }

        private void RenderButtons(RenderGraphics g)
        {
            g.FillRectangle(0, 0, Width, Height, toolbarBrush);

            var pt = this.PointToClient(Cursor.Position);

            // Buttons
            foreach (var btn in buttons)
            {
                if (!btn.Visible)
                    continue;

                bool hover = btn.IsPointIn(pt.X, pt.Y, Width);
                var bmp = btn.GetBitmap != null ? btn.GetBitmap() : btn.Bmp;
                if (bmp == null)
                    bmp = btn.Bmp;

                var status  = btn.Enabled == null ? ButtonStatus.Enabled : btn.Enabled();
                var opacity = status == ButtonStatus.Enabled ? 1.0f : 0.25f;

                if (status != ButtonStatus.Disabled && hover)
                    opacity *= 0.75f;

                int x = btn.RightAligned ? Width - btn.X : btn.X;
                g.DrawBitmap(bmp, x, btn.Y, opacity);
            } 
        }

        private void RenderTimecode(RenderGraphics g)
        {
            var frame = App.CurrentFrame;
            var famitrackerTempo = App.Project != null && App.Project.UsesFamiTrackerTempo;

            var zeroSizeX  = g.MeasureString("0", ThemeBase.FontHuge);
            var colonSizeX = g.MeasureString(":", ThemeBase.FontHuge);

            var timeCodeSizeY = Height - timecodePosY * 2;
            var textColor = App.IsRecording ? theme.DarkRedFillBrush : theme.LightGreyFillBrush2;

            g.FillAndDrawRectangle(timecodePosX, timecodePosY, timecodePosX + timecodeSizeX, Height - timecodePosY, theme.BlackBrush, theme.LightGreyFillBrush2);

            if (Settings.TimeFormat == 0 || famitrackerTempo) // MM:SS:mmm cant be used with FamiTracker tempo.
            {
                var location = NoteLocation.FromAbsoluteNoteIndex(App.Song, frame);

                var numPatternDigits = Utils.NumDecimalDigits(App.Song.Length - 1);
                var numNoteDigits = Utils.NumDecimalDigits(App.Song.GetPatternLength(location.PatternIndex) - 1);

                var patternString = location.PatternIndex.ToString("D" + numPatternDigits);
                var noteString = location.NoteIndex.ToString("D" + numNoteDigits);

                var charPosX = timecodePosX + timecodeSizeX / 2 - ((numPatternDigits + numNoteDigits) * zeroSizeX + colonSizeX) / 2;

                for (int i = 0; i < numPatternDigits; i++, charPosX += zeroSizeX)
                    g.DrawText(patternString[i].ToString(), ThemeBase.FontHuge, charPosX, 2, textColor, zeroSizeX);

                g.DrawText(":", ThemeBase.FontHuge, charPosX, 2, textColor, colonSizeX);
                charPosX += colonSizeX;

                for (int i = 0; i < numNoteDigits; i++, charPosX += zeroSizeX)
                    g.DrawText(noteString[i].ToString(), ThemeBase.FontHuge, charPosX, 2, textColor, zeroSizeX);
            }
            else
            {
                TimeSpan time = App.CurrentTime;

                var minutesString      = time.Minutes.ToString("D2");
                var secondsString      = time.Seconds.ToString("D2");
                var millisecondsString = time.Milliseconds.ToString("D3");

                // 00:00:000
                var charPosX = timecodePosX + timecodeSizeX / 2 - (7 * zeroSizeX + 2 * colonSizeX) / 2;

                for (int i = 0; i < 2; i++, charPosX += zeroSizeX)
                    g.DrawText(minutesString[i].ToString(), ThemeBase.FontHuge, charPosX, 2, textColor, zeroSizeX);
                g.DrawText(":", ThemeBase.FontHuge, charPosX, 2, textColor, colonSizeX);
                charPosX += colonSizeX;
                for (int i = 0; i < 2; i++, charPosX += zeroSizeX)
                    g.DrawText(secondsString[i].ToString(), ThemeBase.FontHuge, charPosX, 2, textColor, zeroSizeX);
                g.DrawText(":", ThemeBase.FontHuge, charPosX, 2, textColor, colonSizeX);
                charPosX += colonSizeX;
                for (int i = 0; i < 3; i++, charPosX += zeroSizeX)
                    g.DrawText(millisecondsString[i].ToString(), ThemeBase.FontHuge, charPosX, 2, textColor, zeroSizeX);
            }
        }

        public bool ShouldRefreshOscilloscope(bool hasNonZeroSample)
        {
            return oscilloscopeVisible && lastOscilloscopeHadNonZeroSample != hasNonZeroSample;
        }

        private void RenderOscilloscope(RenderGraphics g)
        {
            if (!oscilloscopeVisible)
                return;

            var oscilloscopeSizeX = timecodeSizeX;
            var oscilloscopeSizeY = Height - timecodePosY * 2;

            g.FillRectangle(oscilloscopePosX, timecodePosY, oscilloscopePosX + oscilloscopeSizeX, Height - timecodePosY, theme.BlackBrush);

            var oscilloscopeGeometry = App.GetOscilloscopeGeometry(out lastOscilloscopeHadNonZeroSample);

            if (oscilloscopeGeometry != null && lastOscilloscopeHadNonZeroSample)
            {
                float scaleX = oscilloscopeSizeX;
                float scaleY = oscilloscopeSizeY / 2;

                RenderGeometry geo = g.CreateGeometry(oscilloscopeGeometry, false);
                g.PushTransform(oscilloscopePosX, timecodePosY + oscilloscopeSizeY / 2, scaleX, scaleY);
                g.AntiAliasing = true;
                g.DrawGeometry(geo, theme.LightGreyFillBrush2);
                g.AntiAliasing = false;
                g.PopTransform();
                geo.Dispose();
            }
            else
            {
                g.PushTranslation(oscilloscopePosX, timecodePosY + oscilloscopeSizeY / 2);
                g.AntiAliasing = true;
                g.DrawLine(0, 0, oscilloscopeSizeX, 0, theme.LightGreyFillBrush2);
                g.AntiAliasing = false;
                g.PopTransform();
            }

            g.DrawRectangle(oscilloscopePosX, timecodePosY, oscilloscopePosX + oscilloscopeSizeX, Height - timecodePosY, theme.LightGreyFillBrush2);
        }

        private void RenderWarningAndTooltip(RenderGraphics g)
        {
            var scaling = RenderTheme.MainWindowScaling;
            var message = tooltip;
            var messageBrush = redTooltip ? warningBrush : theme.LightGreyFillBrush2;
            var messageFont = ThemeBase.FontMedium;
            var messageFontCenter = ThemeBase.FontMediumCenter;

            g.PushClip(lastButtonX, 0, Width, Height);

            if (!string.IsNullOrEmpty(warning))
            {
                var span = DateTime.Now - warningTime;

                if (span.TotalMilliseconds >= 2000)
                {
                    warning = "";
                }
                else
                {
                    message = (((((long)span.TotalMilliseconds) / 250) & 1) != 0) ? warning : "";
                    messageBrush = warningBrush;
                    messageFont = ThemeBase.FontMediumBold;
                    messageFontCenter = ThemeBase.FontMediumBoldCenter;
                }
            }

            // Tooltip
            if (!string.IsNullOrEmpty(message))
            {
                var lines = message.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                var posY = lines.Length == 1 ? tooltipSingleLinePosY : tooltipMultiLinePosY;

                for (int j = 0; j < lines.Length; j++)
                {
                    var splits = lines[j].Split(new char[] { '{', '}' }, StringSplitOptions.RemoveEmptyEntries);
                    var posX = Width - 40 * scaling;

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
#if FAMISTUDIO_MACOS
                                if (str == "Ctrl") str = "Cmd";
#endif

#if !FAMISTUDIO_WINDOWS
                                // HACK: The way we handle fonts in OpenGL is so different, i cant be bothered to debug this.
                                posX -= (int)scaling;
#endif

                                g.DrawRectangle(posX, posY + specialCharacter.OffsetY, posX + specialCharacter.Width, posY + specialCharacter.Height + specialCharacter.OffsetY, messageBrush);
                                g.DrawText(str, messageFontCenter, posX, posY, messageBrush, specialCharacter.Width);

#if !FAMISTUDIO_WINDOWS
                                // HACK: The way we handle fonts in OpenGL is so different, i cant be bothered to debug this.
                                posX -= (int)scaling;
#endif
                            }
                        }
                        else
                        {
                            posX -= g.MeasureString(splits[i], messageFont);
                            g.DrawText(str, messageFont, posX, posY, messageBrush);
                        }
                    }

                    posY += tooltipLineSizeY;
                }
            }

            g.PopClip();
        }

        protected override void OnRender(RenderGraphics g)
        {
            RenderButtons(g);
            RenderTimecode(g);
            RenderOscilloscope(g);
            RenderWarningAndTooltip(g);
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
                if (btn.Visible && btn.IsPointIn(e.X, e.Y, Width))
                {
                    SetToolTip(btn.ToolTip);
                    return;
                }
            }

            SetToolTip("");
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            foreach (var btn in buttons)
            {
                if (btn != null && btn.Visible && btn.IsPointIn(e.X, e.Y, Width) && (btn.Enabled == null || btn.Enabled() != ButtonStatus.Disabled))
                {
                    btn.MouseWheel?.Invoke(e.Delta);
                    break;
                }
            }

            base.OnMouseWheel(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            bool left  = e.Button.HasFlag(MouseButtons.Left);
            bool right = e.Button.HasFlag(MouseButtons.Right);

            if (left || right)
            {
                if (e.X > timecodePosX && e.X < timecodePosX + timecodeSizeX &&
                    e.Y > timecodePosY && e.Y < Height - timecodePosY)
                {
                    Settings.TimeFormat = Settings.TimeFormat == 0 ? 1 : 0;
                    ConditionalInvalidate();
                }
                else
                {
                    foreach (var btn in buttons)
                    {
                        if (btn != null && btn.Visible && btn.IsPointIn(e.X, e.Y, Width) && (btn.Enabled == null || btn.Enabled() != ButtonStatus.Disabled))
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
}
