using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Media;
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
        const int ButtonFollow    = 15;
        const int ButtonMachine   = 16;
        const int ButtonHelp      = 17;
        const int ButtonCount     = 18; //should always be last

        const int DefaultTimecodePosX            = 404;
        const int DefaultTimecodePosY            = 4;
        const int DefaultTimecodeSizeX           = 160;
        const int DefaultTooltipSingleLinePosY   = 12;
        const int DefaultTooltipMultiLinePosY    = 4;
        const int DefaultTooltipLineSizeY        = 17;
        const int DefaultTooltipSpecialCharSizeX = 16;
        const int DefaultTooltipSpecialCharSizeY = 14;

        int timecodePosX;
        int timecodePosY;
        int timecodeSizeX;
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
            public bool RightAligned;
            public int Size = 32;
            public string ToolTip;
            public RenderBitmap Bmp;
            public BoolDelegate Enabled;
            public EmptyDelegate Click;
            public EmptyDelegate RightClick;
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


        string tooltip = "";
        RenderTheme theme;
        RenderBrush toolbarBrush;
        RenderBrush warningBrush;
        RenderBitmap bmpLoopNone;
        RenderBitmap bmpLoopSong;
        RenderBitmap bmpLoopPattern;
        RenderBitmap bmpPlay;
        RenderBitmap bmpPause;
        RenderBitmap bmpNtsc;
        RenderBitmap bmpPal;
        RenderBitmap bmpNtscToPal;
        RenderBitmap bmpPalToNtsc;
        RenderBitmap bmpRec;
        RenderBitmap bmpRecRed;
        RenderBitmap[] bmpFollowNone;
        RenderBitmap[] bmpFollowJump;
        RenderBitmap[] bmpFollowContinuous;
        Button[] buttons = new Button[ButtonCount];
        Dictionary<string, TooltipSpecialCharacter> specialCharacters = new Dictionary<string, TooltipSpecialCharacter>();

        protected override void OnRenderInitialized(RenderGraphics g)
        {
            theme = RenderTheme.CreateResourcesForGraphics(g);

            toolbarBrush = g.CreateHorizontalGradientBrush(0, 81, ThemeBase.LightGreyFillColor1, ThemeBase.LightGreyFillColor2);
            warningBrush = g.CreateSolidBrush(ThemeBase.Darken(ThemeBase.CustomColors[0, 0]));

            bmpLoopNone    = g.CreateBitmapFromResource("LoopNone");
            bmpLoopSong    = g.CreateBitmapFromResource("Loop");
            bmpLoopPattern = g.CreateBitmapFromResource("LoopPattern");
            bmpPlay        = g.CreateBitmapFromResource("Play");
            bmpPause       = g.CreateBitmapFromResource("Pause");
            bmpNtsc        = g.CreateBitmapFromResource("NTSC");
            bmpPal         = g.CreateBitmapFromResource("PAL");
            bmpNtscToPal   = g.CreateBitmapFromResource("NTSCToPAL");
            bmpPalToNtsc   = g.CreateBitmapFromResource("PALToNTSC");
            bmpRec         = g.CreateBitmapFromResource("Rec");
            bmpRecRed      = g.CreateBitmapFromResource("RecRed");

            string[] followModes = { "Seq", "Piano", "Both" };
            bmpFollowNone = new RenderBitmap[followModes.Length];
            bmpFollowJump = new RenderBitmap[followModes.Length];
            bmpFollowContinuous = new RenderBitmap[followModes.Length];
            for (int i = 0; i < followModes.Length; i++)
            {
                bmpFollowNone[i] = g.CreateBitmapFromResource("FollowNone" + followModes[i]);
                bmpFollowJump[i] = g.CreateBitmapFromResource("FollowJump" + followModes[i]);
                bmpFollowContinuous[i] = g.CreateBitmapFromResource("FollowContinuous" + followModes[i]);
            }

            buttons[ButtonNew]       = new Button { X = 4,   Y = 4, Bmp = g.CreateBitmapFromResource("File"), Click = OnNew };
            buttons[ButtonOpen]      = new Button { X = 40,  Y = 4, Bmp = g.CreateBitmapFromResource("Open"), Click = OnOpen };
            buttons[ButtonSave]      = new Button { X = 76,  Y = 4, Bmp = g.CreateBitmapFromResource("Save"), Click = OnSave, RightClick = OnSaveAs };
            buttons[ButtonExport]    = new Button { X = 112, Y = 4, Bmp = g.CreateBitmapFromResource("Export"), Click = OnExport };
            buttons[ButtonCopy]      = new Button { X = 148, Y = 4, Bmp = g.CreateBitmapFromResource("Copy"), Click = OnCopy, Enabled = OnCopyEnabled };
            buttons[ButtonCut]       = new Button { X = 184, Y = 4, Bmp = g.CreateBitmapFromResource("Cut"), Click = OnCut, Enabled = OnCutEnabled };
            buttons[ButtonPaste]     = new Button { X = 220, Y = 4, Bmp = g.CreateBitmapFromResource("Paste"), Click = OnPaste, RightClick = OnPasteSpecial, Enabled = OnPasteEnabled };
            buttons[ButtonUndo]      = new Button { X = 256, Y = 4, Bmp = g.CreateBitmapFromResource("Undo"), Click = OnUndo, Enabled = OnUndoEnabled };
            buttons[ButtonRedo]      = new Button { X = 292, Y = 4, Bmp = g.CreateBitmapFromResource("Redo"), Click = OnRedo, Enabled = OnRedoEnabled };
            buttons[ButtonTransform] = new Button { X = 328, Y = 4, Bmp = g.CreateBitmapFromResource("Transform"), Click = OnTransform };
            buttons[ButtonConfig]    = new Button { X = 364, Y = 4, Bmp = g.CreateBitmapFromResource("Config"), Click = OnConfig };
            buttons[ButtonPlay]      = new Button { X = 572, Y = 4, Click = OnPlay, GetBitmap = OnPlayGetBitmap };
            buttons[ButtonRec]       = new Button { X = 608, Y = 4, GetBitmap = OnRecordGetBitmap, Click = OnRecord };
            buttons[ButtonRewind]    = new Button { X = 644, Y = 4, Bmp = g.CreateBitmapFromResource("Rewind"), Click = OnRewind };
            buttons[ButtonLoop]      = new Button { X = 680, Y = 4, Click = OnLoop, GetBitmap = OnLoopGetBitmap };
            buttons[ButtonFollow]    = new Button { X = 716, Y = 4, Click = OnFollowMode, RightClick = OnFollowSync, GetBitmap = OnFollowGetBitmap };
            buttons[ButtonMachine]   = new Button { X = 752, Y = 4, Click = OnMachine, GetBitmap = OnMachineGetBitmap, Enabled = OnMachineEnabled };
            buttons[ButtonHelp]      = new Button { X = 36,  Y = 4, Bmp = g.CreateBitmapFromResource("Help"), RightAligned = true, Click = OnHelp };

            buttons[ButtonNew].ToolTip       = "{MouseLeft} New Project {Ctrl} {N}";
            buttons[ButtonOpen].ToolTip      = "{MouseLeft} Open Project {Ctrl} {O}";
            buttons[ButtonSave].ToolTip      = "{MouseLeft} Save Project {Ctrl} {S} - {MouseRight} Save As...";
            buttons[ButtonExport].ToolTip    = "{MouseLeft} Export to various formats {Ctrl} {E}";
            buttons[ButtonCopy].ToolTip      = "{MouseLeft} Copy selection {Ctrl} {C}";
            buttons[ButtonCut].ToolTip       = "{MouseLeft} Cut selection {Ctrl} {X}";
            buttons[ButtonPaste].ToolTip     = "{MouseLeft} Paste {Ctrl} {V}\n{MouseRight} Paste Special... {Ctrl} {Shift} {V}";
            buttons[ButtonUndo].ToolTip      = "{MouseLeft} Undo {Ctrl} {Z}";
            buttons[ButtonRedo].ToolTip      = "{MouseLeft} Redo {Ctrl} {Y}";
            buttons[ButtonTransform].ToolTip = "{MouseLeft} Perform cleanup and various operations";
            buttons[ButtonConfig].ToolTip    = "{MouseLeft} Edit Application Settings";
            buttons[ButtonPlay].ToolTip      = "{MouseLeft} Play/Pause {Space} - Play from start of pattern {Ctrl} {Space}\nPlay from start of song {Shift} {Space}";
            buttons[ButtonRewind].ToolTip    = "{MouseLeft} Rewind {Home}\nRewind to beginning of current pattern {Ctrl} {Home}";
            buttons[ButtonRec].ToolTip       = "{MouseLeft} Toggles recording mode {Enter}\nAbort recording {Esc}";
            buttons[ButtonLoop].ToolTip      = "{MouseLeft} Toggle Loop Mode";
            buttons[ButtonFollow].ToolTip    = "{MouseLeft} Toggle Follow Mode - {MouseRight} Toggle Window Following";
            buttons[ButtonMachine].ToolTip   = "{MouseLeft} Toggle between NTSC/PAL playback mode";
            buttons[ButtonHelp].ToolTip      = "{MouseLeft} Online documentation";

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
            specialCharacters["Enter"]      = new TooltipSpecialCharacter { Width = (int)(38 * scaling) };
            specialCharacters["Esc"]        = new TooltipSpecialCharacter { Width = (int)(24 * scaling) };
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
            Utils.DisposeAndNullify(ref bmpLoopNone);
            Utils.DisposeAndNullify(ref bmpLoopSong);
            Utils.DisposeAndNullify(ref bmpLoopPattern);
            Utils.DisposeAndNullify(ref bmpPlay);
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
            App.LoopMode = (LoopMode)(((int)App.LoopMode + 1) % 3);
        }

        private RenderBitmap OnLoopGetBitmap()
        {
            switch (App.LoopMode)
            {
                case LoopMode.None:
                case LoopMode.LoopPoint: return bmpLoopNone;
                case LoopMode.Song: return bmpLoopSong;
                case LoopMode.Pattern: return bmpLoopPattern;
            }

            return null;
        }

        private void OnFollowMode()
        {
            Settings.FollowMode = (Settings.FollowMode + 1) % 3;
        }

        private void OnFollowSync()
        {
            Settings.FollowSync = (Settings.FollowSync + 1) % 3;
        }

        private RenderBitmap OnFollowGetBitmap()
        {
            RenderBitmap[] arr;
            switch (Settings.FollowMode)
            {
                case 0: arr = bmpFollowNone; break;
                case 1: arr = bmpFollowJump; break;
                case 2: arr = bmpFollowContinuous; break;
                default: return null;
            }
            return arr[Settings.FollowSync % arr.Length];
        }

        private void OnMachine()
        {
            App.PalPlayback = !App.PalPlayback;
        }

        private bool OnMachineEnabled()
        {
            return App.Project != null && App.Project.ExpansionAudio == Project.ExpansionNone;
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
                bool hover = btn.IsPointIn(pt.X, pt.Y, Width);
                var bmp = btn.GetBitmap != null ? btn.GetBitmap() : btn.Bmp;
                if (bmp == null)
                    bmp = btn.Bmp;
                bool enabled = btn.Enabled != null ? btn.Enabled() : true;
                int x = btn.RightAligned ? Width - btn.X : btn.X;
                g.DrawBitmap(bmp, x, btn.Y, enabled ? (hover ? 0.75f : 1.0f) : 0.25f);
            }
        }

        private void RenderTimecode(RenderGraphics g)
        {
            var frame = App.CurrentFrame;
            var famitrackerTempo = App.Project != null && App.Project.UsesFamiTrackerTempo;

            var zeroSizeX  = g.MeasureString("0", ThemeBase.FontHuge);
            var colonSizeX = g.MeasureString(":", ThemeBase.FontHuge);

            var timeCodeColor = App.IsRecording ? theme.DarkRedFillBrush2 : theme.DarkGreyFillBrush1;
            var textColor = App.IsRecording ? theme.BlackBrush : theme.LightGreyFillBrush1;

            g.FillAndDrawRectangle(timecodePosX, timecodePosY, timecodePosX + timecodeSizeX, Height - timecodePosY, timeCodeColor, theme.BlackBrush);

            if (Settings.TimeFormat == 0 || famitrackerTempo) // MM:SS:mmm cant be used with FamiTracker tempo.
            {
                int patternIdx = App.Song.FindPatternInstanceIndex(frame, out int noteIdx);

                var numPatternDigits = Utils.NumDecimalDigits(App.Song.Length - 1);
                var numNoteDigits = Utils.NumDecimalDigits(App.Song.GetPatternLength(patternIdx) - 1);

                var patternString = patternIdx.ToString("D" + numPatternDigits);
                var noteString = noteIdx.ToString("D" + numNoteDigits);

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
                TimeSpan time = App.Project != null ? 
                    TimeSpan.FromMilliseconds(frame * 1000.0 / (App.Project.PalMode ? NesApu.FpsPAL : NesApu.FpsNTSC)) :
                    TimeSpan.Zero;

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

        private void RenderWarningAndTooltip(RenderGraphics g)
        {
            var scaling = RenderTheme.MainWindowScaling;
            var message = tooltip;
            var messageBrush = theme.BlackBrush;
            var messageFont = ThemeBase.FontMedium;
            var messageFontCenter = ThemeBase.FontMediumCenter;

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
        }

        protected override void OnRender(RenderGraphics g)
        {
            RenderButtons(g);
            RenderTimecode(g);
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
                if (btn.IsPointIn(e.X, e.Y, Width))
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
                        if (btn != null && btn.IsPointIn(e.X, e.Y, Width) && (btn.Enabled == null || btn.Enabled()))
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
