using System;
using System.Drawing; // MATTT : See the usings below.
using System.Collections.Generic;
using System.Diagnostics;

// MATTT : Needed?
//using Color     = System.Drawing.Color;
//using Point     = System.Drawing.Point;
//using Rectangle = System.Drawing.Rectangle;

namespace FamiStudio
{
    public class Toolbar : Control
    {
        private enum ButtonType
        {
            New,
            Open,
            Save,
            Export,
            Copy,
            Cut,
            Paste,
            Delete,
            Undo,
            Redo,
            Transform,
            Config,
            Play,
            Rec,
            Rewind,
            Loop,
            Qwerty,
            Metronome,
            Machine,
            Follow,
            Help,
            More,
            Piano,
            Count
        }

        private enum ButtonStatus
        {
            Enabled,
            Disabled,
            Dimmed
        }

        private enum ButtonImageIndices
        { 
            LoopNone,
            Loop,
            LoopPattern,
            LoopSelection,
            Play,
            PlayHalf,
            PlayQuarter,
            Pause,
            NTSC,
            PAL,
            NTSCToPAL,
            PALToNTSC,
            Rec,
            Metronome,
            File,
            Open,
            Save,
            Export,
            Copy,
            Cut,
            Paste,
            Delete,
            Undo,
            Redo,
            Transform,
            Config,
            Rewind,
            QwertyPiano,
            Follow,
            Help,
            More,
            Piano,
            Count
        };

        private readonly string[] ButtonImageNames = new string[]
        {
            "LoopNone",
            "Loop",
            "LoopPattern",
            "LoopSelection",
            "Play",
            "PlayHalf",
            "PlayQuarter",
            "Pause",
            "NTSC",
            "PAL",
            "NTSCToPAL",
            "PALToNTSC",
            "Rec",
            "Metronome",
            "File",
            "Open",
            "Save",
            "Export",
            "Copy",
            "Cut",
            "Paste",
            "Delete",
            "Undo",
            "Redo",
            "Transform",
            "Config",
            "Rewind",
            "QwertyPiano",
            "Follow",
            "Help",
            "More",
            "Piano"
        };

        enum SpecialCharImageIndices
        {
            Drag,
            MouseLeft,
            MouseRight,
            MouseWheel,
            Warning,
            Count
        };

        readonly string[] SpecialCharImageNames = new string[]
        {
            "Drag",
            "MouseLeft",
            "MouseRight",
            "MouseWheel",
            "Warning"
        };
        
        // Mobile-only layout.
        private struct MobileButtonLayoutItem
        {
            public MobileButtonLayoutItem(int r, int c, ButtonType b)
            {
                row = r;
                col = c;
                btn = b;
            }
            public int row;
            public int col;
            public ButtonType btn;
        };

        private struct MobileOscTimeLayoutItem
        {
            public MobileOscTimeLayoutItem(int r, int c, int nc)
            {
                row = r;
                col = c;
                numCols = nc;
            }
            public int row;
            public int col;
            public int numCols;
        };

        private readonly MobileButtonLayoutItem[] ButtonLayout = new MobileButtonLayoutItem[]
        {
            new MobileButtonLayoutItem(0,  0, ButtonType.Open),
            new MobileButtonLayoutItem(0,  1, ButtonType.Copy),
            new MobileButtonLayoutItem(0,  2, ButtonType.Cut),
            new MobileButtonLayoutItem(0,  3, ButtonType.Undo),
            new MobileButtonLayoutItem(0,  6, ButtonType.Play),
            new MobileButtonLayoutItem(0,  7, ButtonType.Rec),
            new MobileButtonLayoutItem(0,  8, ButtonType.Help),

            new MobileButtonLayoutItem(1,  0, ButtonType.Save),
            new MobileButtonLayoutItem(1,  1, ButtonType.Paste),
            new MobileButtonLayoutItem(1,  2, ButtonType.Delete),
            new MobileButtonLayoutItem(1,  3, ButtonType.Redo),
            new MobileButtonLayoutItem(1,  6, ButtonType.Rewind),
            new MobileButtonLayoutItem(1,  7, ButtonType.Piano),
            new MobileButtonLayoutItem(1,  8, ButtonType.More),

            new MobileButtonLayoutItem(2,  0, ButtonType.New),
            new MobileButtonLayoutItem(2,  1, ButtonType.Export),
            new MobileButtonLayoutItem(2,  2, ButtonType.Config),
            new MobileButtonLayoutItem(2,  3, ButtonType.Transform),
            new MobileButtonLayoutItem(2,  4, ButtonType.Machine),
            new MobileButtonLayoutItem(2,  5, ButtonType.Follow),
            new MobileButtonLayoutItem(2,  6, ButtonType.Loop),
            new MobileButtonLayoutItem(2,  7, ButtonType.Metronome),
            new MobileButtonLayoutItem(2,  8, ButtonType.Count),
        };

        // [portrait/landscape, timecode/oscilloscope]
        private readonly MobileOscTimeLayoutItem[,] OscTimeLayout = new MobileOscTimeLayoutItem[,]
        {
            {
                new MobileOscTimeLayoutItem(0, 4, 2),
                new MobileOscTimeLayoutItem(1, 4, 2),
            },
            {
                new MobileOscTimeLayoutItem(0, 4, 2),
                new MobileOscTimeLayoutItem(0, 5, 2),
            }
        };

        // Most of those are for desktop.
        const int DefaultTimecodePosY            = 4;
        const int DefaultTimecodeSizeX           = 140;
        const int DefaultTooltipSingleLinePosY   = 12;
        const int DefaultTooltipMultiLinePosY    = 4;
        const int DefaultTooltipLineSizeY        = 17;
        const int DefaultTooltipSpecialCharSizeX = 16;
        const int DefaultTooltipSpecialCharSizeY = 14;
        const int DefaultButtonIconPosX          = Platform.IsMobile ?  12 : 2;
        const int DefaultButtonIconPosY          = Platform.IsMobile ?  12 : 4;
        const int DefaultButtonSize              = Platform.IsMobile ? 120 : 36;
        const int DefaultIconSize                = Platform.IsMobile ?  96 : 32; 
        const float ShowExtraButtonsThreshold    = 0.8f;

        int tooltipSingleLinePosY;
        int tooltipMultiLinePosY;
        int tooltipLineSizeY;
        int tooltipSpecialCharSizeX;
        int tooltipSpecialCharSizeY;
        int buttonIconPosX;
        int buttonIconPosY;
        int buttonSize;
        int iconSize;

        class TooltipSpecialCharacter
        {
            public SpecialCharImageIndices BmpIndex = SpecialCharImageIndices.Count;
            public int Width;
            public int Height;
            public float OffsetY;
        };

        DateTime notificationTime;
        string notification = "";
        bool notificationWarning;

        int lastButtonX = 500;
        bool redTooltip = false;
        new string tooltip = "";
        Font timeCodeFont;
        BitmapAtlasRef[] bmpSpecialCharacters;
        Dictionary<string, TooltipSpecialCharacter> specialCharacters = new Dictionary<string, TooltipSpecialCharacter>();

        private delegate void MouseWheelDelegate(float delta);
        private delegate void MouseClickDelegate(int x, int y);
        private delegate ButtonStatus ButtonStatusDelegate();
        private delegate ButtonImageIndices BitmapDelegate(ref Color tint);

        private class Button
        {
            public Rectangle Rect;
            public Point IconPos;
            public bool Visible = true;
            public bool CloseOnClick = true;
            public bool VibrateOnLongPress = true;
            public string ToolTip;
            public ButtonImageIndices BmpAtlasIndex;
            public ButtonStatusDelegate Enabled;
            public MouseClickDelegate Click;
            public MouseClickDelegate RightClick;
            public MouseWheelDelegate MouseWheel;
            public BitmapDelegate GetBitmap;
        };

        private int timecodePosX;
        private int timecodePosY;
        private int oscilloscopePosX;
        private int oscilloscopePosY;
        private int timecodeOscSizeX;
        private int timecodeOscSizeY;

        private Brush toolbarBrush;
        private Brush warningBrush;
        private BitmapAtlasRef[] bmpButtons;
        private Button[] buttons = new Button[(int)ButtonType.Count];

        private bool oscilloscopeVisible = true;
        private bool lastOscilloscopeHadNonZeroSample = false;

        // Mobile-only stuff
        private float expandRatio = 0.0f;
        private bool  expanding = false; 
        private bool  closing   = false; 

        public int   LayoutSize  => buttonSize * 2;
        public int   RenderSize  => (int)Math.Round(LayoutSize * (1.0f + Utils.SmootherStep(expandRatio) * 0.5f));
        public float ExpandRatio => expandRatio;
        public bool  IsExpanded  => expandRatio > 0.0f;

        public override bool WantsFullScreenViewport => Platform.IsMobile;

        private float iconScaleFloat = 1.0f;

        protected override void OnRenderInitialized(Graphics g)
        {
            Debug.Assert((int)ButtonImageIndices.Count == ButtonImageNames.Length);
            Debug.Assert((int)SpecialCharImageIndices.Count == SpecialCharImageNames.Length);

            if (Platform.IsMobile)
                toolbarBrush = g.CreateSolidBrush(Theme.DarkGreyFillColor1);
            else
                toolbarBrush = g.CreateVerticalGradientBrush(0, Height, Theme.DarkGreyFillColor2, Theme.DarkGreyFillColor1);

            warningBrush = g.CreateSolidBrush(System.Drawing.Color.FromArgb(205, 77, 64));
            bmpButtons = g.GetBitmapAtlasRefs(ButtonImageNames);
            timeCodeFont = ThemeResources.FontHuge;

            buttons[(int)ButtonType.New]       = new Button { BmpAtlasIndex = ButtonImageIndices.File, Click = OnNew };
            buttons[(int)ButtonType.Open]      = new Button { BmpAtlasIndex = ButtonImageIndices.Open, Click = OnOpen, RightClick = Platform.IsDesktop ? OnOpenRecent : (MouseClickDelegate)null };
            buttons[(int)ButtonType.Save]      = new Button { BmpAtlasIndex = ButtonImageIndices.Save, Click = OnSave, RightClick = OnSaveAs };
            buttons[(int)ButtonType.Export]    = new Button { BmpAtlasIndex = ButtonImageIndices.Export, Click = OnExport, RightClick = Platform.IsDesktop ? OnRepeatLastExport : (MouseClickDelegate)null };
            buttons[(int)ButtonType.Copy]      = new Button { BmpAtlasIndex = ButtonImageIndices.Copy, Click = OnCopy, Enabled = OnCopyEnabled };
            buttons[(int)ButtonType.Cut]       = new Button { BmpAtlasIndex = ButtonImageIndices.Cut, Click = OnCut, Enabled = OnCutEnabled };
            buttons[(int)ButtonType.Paste]     = new Button { BmpAtlasIndex = ButtonImageIndices.Paste, Click = OnPaste, RightClick = OnPasteSpecial, Enabled = OnPasteEnabled };
            buttons[(int)ButtonType.Undo]      = new Button { BmpAtlasIndex = ButtonImageIndices.Undo, Click = OnUndo, Enabled = OnUndoEnabled };
            buttons[(int)ButtonType.Redo]      = new Button { BmpAtlasIndex = ButtonImageIndices.Redo, Click = OnRedo, Enabled = OnRedoEnabled };
            buttons[(int)ButtonType.Transform] = new Button { BmpAtlasIndex = ButtonImageIndices.Transform, Click = OnTransform };
            buttons[(int)ButtonType.Config]    = new Button { BmpAtlasIndex = ButtonImageIndices.Config, Click = OnConfig };
            buttons[(int)ButtonType.Play]      = new Button { Click = OnPlay, RightClick = OnPlayWithRate, GetBitmap = OnPlayGetBitmap, VibrateOnLongPress = false };
            buttons[(int)ButtonType.Rec]       = new Button { GetBitmap = OnRecordGetBitmap, Click = OnRecord };
            buttons[(int)ButtonType.Rewind]    = new Button { BmpAtlasIndex = ButtonImageIndices.Rewind, Click = OnRewind };
            buttons[(int)ButtonType.Loop]      = new Button { Click = OnLoop, GetBitmap = OnLoopGetBitmap, CloseOnClick = false };
            buttons[(int)ButtonType.Metronome] = new Button { BmpAtlasIndex = ButtonImageIndices.Metronome, Click = OnMetronome, Enabled = OnMetronomeEnabled, CloseOnClick = false };
            buttons[(int)ButtonType.Machine]   = new Button { Click = OnMachine, GetBitmap = OnMachineGetBitmap, Enabled = OnMachineEnabled, CloseOnClick = false };
            buttons[(int)ButtonType.Follow]    = new Button { BmpAtlasIndex = ButtonImageIndices.Follow, Click = OnFollow, Enabled = OnFollowEnabled, CloseOnClick = false };
            buttons[(int)ButtonType.Help]      = new Button { BmpAtlasIndex = ButtonImageIndices.Help, Click = OnHelp };

            if (Platform.IsMobile)
            {
                buttons[(int)ButtonType.Delete] = new Button { BmpAtlasIndex = ButtonImageIndices.Delete, Click = OnDelete, RightClick = OnDeleteSpecial, Enabled = OnDeleteEnabled };
                buttons[(int)ButtonType.More]   = new Button { BmpAtlasIndex = ButtonImageIndices.More, Click = OnMore };
                buttons[(int)ButtonType.Piano]  = new Button { BmpAtlasIndex = ButtonImageIndices.Piano, Click = OnMobilePiano, Enabled = OnMobilePianoEnabled };

                // On mobile, everything will scale from 1080p.
                var screenSize = Platform.GetScreenResolution();
                var scale = Math.Min(screenSize.Width, screenSize.Height) / 1080.0f;
                var bitmapSize = bmpButtons[0].ElementSize;

                buttonIconPosX = ScaleCustom(DefaultButtonIconPosX, scale);
                buttonIconPosY = ScaleCustom(DefaultButtonIconPosY, scale);
                buttonSize     = ScaleCustom(DefaultButtonSize, scale);
                iconSize       = ScaleCustom(DefaultIconSize, scale);
                iconScaleFloat = iconSize / (float)(bitmapSize.Width);
            }
            else
            {
                buttons[(int)ButtonType.Qwerty] = new Button { BmpAtlasIndex = ButtonImageIndices.QwertyPiano, Click = OnQwerty, Enabled = OnQwertyEnabled };

                timecodePosY            = ScaleForMainWindow(DefaultTimecodePosY);
                oscilloscopePosY        = ScaleForMainWindow(DefaultTimecodePosY);
                timecodeOscSizeX        = ScaleForMainWindow(DefaultTimecodeSizeX);
                tooltipSingleLinePosY   = ScaleForMainWindow(DefaultTooltipSingleLinePosY);
                tooltipMultiLinePosY    = ScaleForMainWindow(DefaultTooltipMultiLinePosY);
                tooltipLineSizeY        = ScaleForMainWindow(DefaultTooltipLineSizeY);
                tooltipSpecialCharSizeX = ScaleForMainWindow(DefaultTooltipSpecialCharSizeX);
                tooltipSpecialCharSizeY = ScaleForMainWindow(DefaultTooltipSpecialCharSizeY);
                buttonIconPosX          = ScaleForMainWindow(DefaultButtonIconPosX);
                buttonIconPosY          = ScaleForMainWindow(DefaultButtonIconPosY);
                buttonSize              = ScaleForMainWindow(DefaultButtonSize);

                bmpSpecialCharacters = g.GetBitmapAtlasRefs(SpecialCharImageNames);

                buttons[(int)ButtonType.New].ToolTip       = "{MouseLeft} New Project {Ctrl} {N}";
                buttons[(int)ButtonType.Open].ToolTip      = "{MouseLeft} Open Project {Ctrl} {O}";
                buttons[(int)ButtonType.Save].ToolTip      = "{MouseLeft} Save Project {Ctrl} {S}\n{MouseRight} More Options...";
                buttons[(int)ButtonType.Export].ToolTip    = "{MouseLeft} Export to various formats {Ctrl} {E}\n{MouseRight} More Options...";
                buttons[(int)ButtonType.Copy].ToolTip      = "{MouseLeft} Copy selection {Ctrl} {C}";
                buttons[(int)ButtonType.Cut].ToolTip       = "{MouseLeft} Cut selection {Ctrl} {X}";
                buttons[(int)ButtonType.Paste].ToolTip     = "{MouseLeft} Paste {Ctrl} {V}\n{MouseRight} More Options...";
                buttons[(int)ButtonType.Undo].ToolTip      = "{MouseLeft} Undo {Ctrl} {Z}";
                buttons[(int)ButtonType.Redo].ToolTip      = "{MouseLeft} Redo {Ctrl} {Y}";
                buttons[(int)ButtonType.Transform].ToolTip = "{MouseLeft} Perform cleanup and various operations";
                buttons[(int)ButtonType.Config].ToolTip    = "{MouseLeft} Edit Application Settings";
                buttons[(int)ButtonType.Play].ToolTip      = "{MouseLeft} Play/Pause {Space} - {MouseRight} More Options... - Play from start of pattern {ForceCtrl} {Space}\nPlay from start of song {Shift} {Space} - Play from loop point {Ctrl} {Shift} {Space}";
                buttons[(int)ButtonType.Rewind].ToolTip    = "{MouseLeft} Rewind {Home}\nRewind to beginning of current pattern {Ctrl} {Home}";
                buttons[(int)ButtonType.Rec].ToolTip       = "{MouseLeft} Toggles recording mode {Enter}\nAbort recording {Esc}";
                buttons[(int)ButtonType.Loop].ToolTip      = "{MouseLeft} Toggle Loop Mode (Song, Pattern/Selection)";
                buttons[(int)ButtonType.Qwerty].ToolTip    = "{MouseLeft} Toggle QWERTY keyboard piano input {Shift} {Q}";
                buttons[(int)ButtonType.Metronome].ToolTip = "{MouseLeft} Toggle metronome while song is playing";
                buttons[(int)ButtonType.Machine].ToolTip   = "{MouseLeft} Toggle between NTSC/PAL playback mode";
                buttons[(int)ButtonType.Follow].ToolTip    = "{MouseLeft} Toggle follow mode {Shift} {F}";
                buttons[(int)ButtonType.Help].ToolTip      = "{MouseLeft} Online documentation";

                specialCharacters["Shift"]      = new TooltipSpecialCharacter { Width = ScaleForMainWindow(32) };
                specialCharacters["Space"]      = new TooltipSpecialCharacter { Width = ScaleForMainWindow(38) };
                specialCharacters["Home"]       = new TooltipSpecialCharacter { Width = ScaleForMainWindow(38) };
                specialCharacters["Ctrl"]       = new TooltipSpecialCharacter { Width = ScaleForMainWindow(28) };
                specialCharacters["ForceCtrl"]  = new TooltipSpecialCharacter { Width = ScaleForMainWindow(28) };
                specialCharacters["Alt"]        = new TooltipSpecialCharacter { Width = ScaleForMainWindow(24) };
                specialCharacters["Tab"]        = new TooltipSpecialCharacter { Width = ScaleForMainWindow(24) };
                specialCharacters["Enter"]      = new TooltipSpecialCharacter { Width = ScaleForMainWindow(38) };
                specialCharacters["Esc"]        = new TooltipSpecialCharacter { Width = ScaleForMainWindow(24) };
                specialCharacters["Del"]        = new TooltipSpecialCharacter { Width = ScaleForMainWindow(24) };
                specialCharacters["F1"]         = new TooltipSpecialCharacter { Width = ScaleForMainWindow(18) };
                specialCharacters["F2"]         = new TooltipSpecialCharacter { Width = ScaleForMainWindow(18) };
                specialCharacters["F3"]         = new TooltipSpecialCharacter { Width = ScaleForMainWindow(18) };
                specialCharacters["F4"]         = new TooltipSpecialCharacter { Width = ScaleForMainWindow(18) };
                specialCharacters["F5"]         = new TooltipSpecialCharacter { Width = ScaleForMainWindow(18) };
                specialCharacters["F6"]         = new TooltipSpecialCharacter { Width = ScaleForMainWindow(18) };
                specialCharacters["F7"]         = new TooltipSpecialCharacter { Width = ScaleForMainWindow(18) };
                specialCharacters["F8"]         = new TooltipSpecialCharacter { Width = ScaleForMainWindow(18) };
                specialCharacters["F9"]         = new TooltipSpecialCharacter { Width = ScaleForMainWindow(18) };
                specialCharacters["F10"]        = new TooltipSpecialCharacter { Width = ScaleForMainWindow(24) };
                specialCharacters["F11"]        = new TooltipSpecialCharacter { Width = ScaleForMainWindow(24) };
                specialCharacters["F12"]        = new TooltipSpecialCharacter { Width = ScaleForMainWindow(24) };
                specialCharacters["Drag"]       = new TooltipSpecialCharacter { BmpIndex = SpecialCharImageIndices.Drag,       OffsetY = ScaleForMainWindow(2) };
                specialCharacters["MouseLeft"]  = new TooltipSpecialCharacter { BmpIndex = SpecialCharImageIndices.MouseLeft,  OffsetY = ScaleForMainWindow(2) };
                specialCharacters["MouseRight"] = new TooltipSpecialCharacter { BmpIndex = SpecialCharImageIndices.MouseRight, OffsetY = ScaleForMainWindow(2) };
                specialCharacters["MouseWheel"] = new TooltipSpecialCharacter { BmpIndex = SpecialCharImageIndices.MouseWheel, OffsetY = ScaleForMainWindow(2) };
                specialCharacters["Warning"]    = new TooltipSpecialCharacter { BmpIndex = SpecialCharImageIndices.Warning };

                for (char i = 'A'; i <= 'Z'; i++)
                    specialCharacters[i.ToString()] = new TooltipSpecialCharacter { Width = tooltipSpecialCharSizeX };
                for (char i = '0'; i <= '9'; i++)
                    specialCharacters[i.ToString()] = new TooltipSpecialCharacter { Width = tooltipSpecialCharSizeX };

                specialCharacters["~"] = new TooltipSpecialCharacter { Width = tooltipSpecialCharSizeX };

                foreach (var c in specialCharacters.Values)
                {
                    if (c.BmpIndex != SpecialCharImageIndices.Count)
                        c.Width = bmpSpecialCharacters[(int)c.BmpIndex].ElementSize.Width;
                    c.Height = tooltipSpecialCharSizeY;
                }
            }

            UpdateButtonLayout();
        }

        protected override void OnRenderTerminated()
        {
            Utils.DisposeAndNullify(ref toolbarBrush);
            Utils.DisposeAndNullify(ref warningBrush);

            specialCharacters.Clear();
        }

        private void UpdateButtonLayout()
        {
            if (!IsRenderInitialized)
                return;

            if (Platform.IsDesktop)
            {
                // Hide a few buttons if the window is too small (out min "usable" resolution is ~1280x720).
                var hideLessImportantButtons = Width < 1420 * MainWindowScaling;
                var hideOscilloscope         = Width < 1250 * MainWindowScaling;

                var x = 0;

                for (int i = 0; i < (int)ButtonType.Count; i++)
                {
                    var btn = buttons[i];

                    if (btn == null)
                        continue;

                    if (i == (int)ButtonType.Help)
                    {
                        btn.Rect = new Rectangle(Width - buttonSize, 0, buttonSize, buttonSize);
                    }
                    else
                    {
                        btn.Rect = new Rectangle(x, 0, buttonSize, Height);
                        lastButtonX = btn.Rect.Right;
                    }

                    btn.IconPos = new Point(btn.Rect.X + buttonIconPosX, btn.Rect.Y + buttonIconPosY);
                    btn.Visible = !hideLessImportantButtons || i < (int)ButtonType.Copy || i > (int)ButtonType.Redo;

                    if (i == (int)ButtonType.Config)
                    {
                        x += buttonSize + timecodeOscSizeX + buttonIconPosX;

                        oscilloscopeVisible = Settings.ShowOscilloscope && !hideOscilloscope;
                        if (oscilloscopeVisible)
                            x += timecodeOscSizeX + buttonIconPosX * 4;
                    }
                    else if (btn.Visible)
                    {
                        x += buttonSize;
                    }
                }

                timecodePosX = buttons[(int)ButtonType.Config].Rect.Right + buttonIconPosX;
                oscilloscopePosX = timecodePosX + timecodeOscSizeX + buttonIconPosX * 4;
                timecodeOscSizeY = Height - timecodePosY * 2;
            }
            else
            {
                var landscape = IsLandscape;

                foreach (var btn in buttons)
                {
                    if (btn != null)
                        btn.Visible = false;
                }

                var numRows = expandRatio >= ShowExtraButtonsThreshold ? 3 : 2;

                foreach (var bl in ButtonLayout)
                {
                    if (bl.btn == ButtonType.Count)
                        continue;

                    var btn = buttons[(int)bl.btn];
                
                    var col = bl.col;
                    var row = bl.row;

                    if (row >= numRows)
                        continue;

                    if (landscape)
                        Utils.Swap(ref col, ref row);

                    btn.Rect = new Rectangle(buttonSize * col, buttonSize * row, buttonSize, buttonSize);
                    btn.IconPos = new Point(btn.Rect.X + buttonIconPosX, btn.Rect.Y + buttonIconPosY);
                    btn.Visible = true;
                }

                var timeLayout = OscTimeLayout[landscape ? 1 : 0, 0];
                var oscLayout  = OscTimeLayout[landscape ? 1 : 0, 1];

                Debug.Assert(timeLayout.numCols == oscLayout.numCols);

                var timeCol = timeLayout.col;
                var timeRow = timeLayout.row;
                var oscCol = oscLayout.col;
                var oscRow = oscLayout.row;

                if (landscape)
                {
                    Utils.Swap(ref timeCol, ref timeRow);
                    Utils.Swap(ref oscCol, ref oscRow);
                }

                timecodeOscSizeX = timeLayout.numCols * buttonSize - buttonIconPosX * 2;
                timecodeOscSizeY = buttonSize - buttonIconPosX * 2;
                timecodePosX = buttonIconPosX + timeCol * buttonSize;
                timecodePosY = buttonIconPosX + timeRow * buttonSize;
                oscilloscopePosX = buttonIconPosX + oscCol * buttonSize;
                oscilloscopePosY = buttonIconPosX + oscRow * buttonSize;

                timeCodeFont = ThemeResources.GetBestMatchingFontByWidth("00:00:000", timecodeOscSizeX, false);
            }
        }

        protected override void OnResize(EventArgs e)
        {
            expandRatio = 0.0f;
            expanding = false;
            closing = false;
            UpdateButtonLayout();
        }

        public void LayoutChanged()
        {
            UpdateButtonLayout();
            MarkDirty();
        }

        public void SetToolTip(string msg, bool red = false)
        {
            if (tooltip != msg || red != redTooltip)
            {
                tooltip = msg;
                redTooltip = red;
                MarkDirty();
            }
        }

        public void DisplayNotification(string msg, bool warning, bool beep)
        {
            notificationTime = DateTime.Now;
            notification = (warning ? "{Warning} " : "") + msg;
            notificationWarning = warning;
            if (beep)
                Platform.Beep();
        }

        public override void Tick(float delta)
        {
            if (Platform.IsDesktop)
            {
                if (!string.IsNullOrEmpty(notification))
                    MarkDirty();
            }
            else
            {
                var prevRatio = expandRatio;

                if (expanding)
                {
                    delta *= 6.0f;
                    expandRatio = Math.Min(1.0f, expandRatio + delta);
                    if (prevRatio < ShowExtraButtonsThreshold && expandRatio >= ShowExtraButtonsThreshold)
                        UpdateButtonLayout();
                    if (expandRatio == 1.0f)
                        expanding = false;
                    MarkDirty();
                }
                else if (closing)
                {
                    delta *= 10.0f;
                    expandRatio = Math.Max(0.0f, expandRatio - delta);
                    if (prevRatio >= ShowExtraButtonsThreshold && expandRatio < ShowExtraButtonsThreshold)
                        UpdateButtonLayout();
                    if (expandRatio == 0.0f)
                        closing = false;
                    MarkDirty();
                }
            }
        }

        public void Reset()
        {
            tooltip = "";
            redTooltip = false;
        }

        private void OnNew(int x, int y)
        {
            App.NewProject();
        }

        private void OnOpen(int x, int y)
        {
            App.OpenProject();
        }

        private void OnOpenRecent(int x, int y)
        {
            if (Settings.RecentFiles.Count > 0)
            {
                var options = new ContextMenuOption[Settings.RecentFiles.Count];

                for (int i = 0; i < Settings.RecentFiles.Count; i++)
                {
                    var j = i; // Important, copy for lambda below.
                    options[i] = new ContextMenuOption("MenuFile", Settings.RecentFiles[i], () => App.OpenProject(Settings.RecentFiles[j]));
                }

                App.ShowContextMenu(left + x, top + y, options);
            }
        }

        private void OnSave(int x, int y)
        {
            App.SaveProjectAsync();
        }

        private void OnSaveAs(int x, int y)
        {
            App.ShowContextMenu(left + x, top + y, new[]
            {
                new ContextMenuOption("MenuSave", "Save As...", () => { App.SaveProjectAsync(true); }),
            });
        }

        private void OnExport(int x, int y)
        {
            App.Export();
        }

        private void OnRepeatLastExport(int x, int y)
        {
            App.ShowContextMenu(left + x, top + y, new[]
            {
                new ContextMenuOption("MenuExport", "Repeast Last Export", "Repeats the previous export {Ctrl} {Shift} {E}", () => { App.RepeatLastExport(); }),
            });
        }

        private void OnCut(int x, int y)
        {
            App.Cut();
        }

        private ButtonStatus OnCutEnabled()
        {
            return App.CanCopy ? ButtonStatus.Enabled : ButtonStatus.Disabled;
        }

        private void OnCopy(int x, int y)
        {
            App.Copy();
        }

        private ButtonStatus OnCopyEnabled()
        {
            return App.CanCopy ? ButtonStatus.Enabled : ButtonStatus.Disabled;
        }

        private void OnPaste(int x, int y)
        {
            App.Paste();
        }

        private void OnPasteSpecial(int x, int y)
        {
            App.ShowContextMenu(left + x, top + y, new[]
            {
                new ContextMenuOption("MenuStar", "Paste Special...", "Paste with advanced options {Ctrl} {Shift} {V}", () => { App.PasteSpecial(); }),
            });
        }

        private ButtonStatus OnPasteEnabled()
        {
            return App.CanPaste ? ButtonStatus.Enabled : ButtonStatus.Disabled;
        }

        private void OnDelete(int x, int y)
        {
            App.Delete();
        }

        private void OnDeleteSpecial(int x, int y)
        {
            App.ShowContextMenu(left + x, top + y, new[]
            {
                new ContextMenuOption("MenuStar", "Delete Special...", () => { App.DeleteSpecial(); }),
            });
        }

        private ButtonStatus OnDeleteEnabled()
        {
            return App.CanDelete ? ButtonStatus.Enabled : ButtonStatus.Disabled;
        }

        private void OnUndo(int x, int y)
        {
            App.UndoRedoManager.Undo();
        }

        private ButtonStatus OnUndoEnabled()
        {
            return App.UndoRedoManager != null && App.UndoRedoManager.UndoScope != TransactionScope.Max ? ButtonStatus.Enabled : ButtonStatus.Disabled;
        }

        private void OnRedo(int x, int y)
        {
            App.UndoRedoManager.Redo();
        }

        private ButtonStatus OnRedoEnabled()
        {
            return App.UndoRedoManager != null && App.UndoRedoManager.RedoScope != TransactionScope.Max ? ButtonStatus.Enabled : ButtonStatus.Disabled;
        }

        private void OnTransform(int x, int y)
        {
            App.OpenTransformDialog();
        }

        private void OnConfig(int x, int y)
        {
            App.OpenConfigDialog();
        }

        private void OnPlay(int x, int y)
        {
            if (App.IsPlaying)
                App.StopSong();
            else
                App.PlaySong();
        }

        private void OnPlayWithRate(int x, int y)
        {
            App.ShowContextMenu(left + x, top + y, new[]
            {
                new ContextMenuOption("MenuPlay", "Play From Beginning of Song", "Plays from the start of the song {ForceCtrl} {Space}", () => { App.StopSong(); App.PlaySongFromBeginning(); } ),
                new ContextMenuOption("MenuPlay", "Play From Beginning of Current Pattern", "Plays from the start of the current pattern {Shift} {Space}", () => { App.StopSong(); App.PlaySongFromStartOfPattern(); } ),
                new ContextMenuOption("MenuPlay", "Play From Loop Point", "Plays from the loop point {Ctrl} {Shift} {Space}", () => { App.StopSong(); App.PlaySongFromLoopPoint(); } ),
                new ContextMenuOption("Regular Speed",  "Sets the play rate to 100%", () => { App.PlayRate = 1; }, () => App.PlayRate == 1 ? ContextMenuCheckState.Radio : ContextMenuCheckState.None, true ),
                new ContextMenuOption("Half Speed",     "Sets the play rate to 50%",  () => { App.PlayRate = 2; }, () => App.PlayRate == 2 ? ContextMenuCheckState.Radio : ContextMenuCheckState.None ),
                new ContextMenuOption("Quarter Speed",  "Sets the play rate to 25%",  () => { App.PlayRate = 4; }, () => App.PlayRate == 4 ? ContextMenuCheckState.Radio : ContextMenuCheckState.None ),
            });
        }

        private ButtonImageIndices OnPlayGetBitmap(ref Color tint)
        {
            if (App.IsPlaying)
            {
                return ButtonImageIndices.Pause;
            }
            else
            {
                switch (App.PlayRate)
                {
                    case 2:  return ButtonImageIndices.PlayHalf;
                    case 4:  return ButtonImageIndices.PlayQuarter;
                    default: return ButtonImageIndices.Play;
                }
            }
        }

        private void OnRewind(int x, int y)
        {
            App.StopSong();
            App.SeekSong(0);
        }

        private ButtonImageIndices OnRecordGetBitmap(ref Color tint)
        {
            if (App.IsRecording)
                tint = Theme.DarkRedFillColor;
            return ButtonImageIndices.Rec; 
        }

        private void OnRecord(int x, int y)
        {
            App.ToggleRecording();
        }

        private void OnLoop(int x, int y)
        {
            App.LoopMode = App.LoopMode == LoopMode.LoopPoint ? LoopMode.Pattern : LoopMode.LoopPoint;
        }

        private void OnQwerty(int x, int y)
        {
            App.ToggleQwertyPiano();
        }

        private ButtonStatus OnQwertyEnabled()
        {
            return App.IsQwertyPianoEnabled ? ButtonStatus.Enabled : ButtonStatus.Dimmed;
        }

        private void OnMetronome(int x, int y)
        {
            App.ToggleMetronome();
        }

        private ButtonStatus OnMetronomeEnabled()
        {
            return App.IsMetronomeEnabled ? ButtonStatus.Enabled : ButtonStatus.Dimmed;
        }

        private ButtonImageIndices OnLoopGetBitmap(ref Color tint)
        {
            switch (App.LoopMode)
            {
                case LoopMode.Pattern:
                    return App.SequencerHasSelection ? ButtonImageIndices.LoopSelection : ButtonImageIndices.LoopPattern;
                default:
                    return App.SelectedSong.LoopPoint < 0 ? ButtonImageIndices.LoopNone : ButtonImageIndices.Loop;
            }
        }

        private void OnMachine(int x, int y)
        {
            App.PalPlayback = !App.PalPlayback;
        }

        private void OnFollow(int x, int y)
        {
            App.FollowModeEnabled = !App.FollowModeEnabled;
        }

        private ButtonStatus OnFollowEnabled()
        {
            return App.FollowModeEnabled ? ButtonStatus.Enabled : ButtonStatus.Dimmed;
        }

        private ButtonStatus OnMachineEnabled()
        {
            return App.Project != null && !App.Project.UsesAnyExpansionAudio ? ButtonStatus.Enabled : ButtonStatus.Disabled;
        }

        private ButtonImageIndices OnMachineGetBitmap(ref Color tint)
        {
            if (App.Project == null)
            {
                return ButtonImageIndices.NTSC;
            }
            else if (App.Project.UsesFamiTrackerTempo)
            {
                return App.PalPlayback ? ButtonImageIndices.PAL : ButtonImageIndices.NTSC;
            }
            else
            {
                if (App.Project.PalMode)
                    return App.PalPlayback ? ButtonImageIndices.PAL : ButtonImageIndices.PALToNTSC;
                else
                    return App.PalPlayback ? ButtonImageIndices.NTSCToPAL : ButtonImageIndices.NTSC;
            }
        }

        private void OnHelp(int x, int y)
        {
            App.ShowHelp();
        }

        private void StartClosing()
        {
            expanding = false;
            closing   = expandRatio > 0.0f;
        }

        private void OnMore(int x, int y)
        {
            if (expanding || closing)
            {
                expanding = !expanding;
                closing   = !closing;
            }
            else
            {
                expanding = expandRatio == 0.0f;
                closing   = expandRatio == 1.0f;
            }

            MarkDirty();
        }

        private void OnMobilePiano(int x, int y)
        {
            App.MobilePianoVisible = !App.MobilePianoVisible;
        }

        private ButtonStatus OnMobilePianoEnabled()
        {
            return App.MobilePianoVisible ? ButtonStatus.Enabled : ButtonStatus.Dimmed;
        }

        private void RenderButtons(CommandList c)
        {
            var pt = PointToClient(Cursor.Position);

            // Buttons
            foreach (var btn in buttons)
            {
                if (btn == null || !btn.Visible)
                    continue;

                var hover = btn.Rect.Contains(pt) && !Platform.IsMobile;
                var tint = Theme.LightGreyFillColor1;
                var bmpIndex = btn.GetBitmap != null ? btn.GetBitmap(ref tint) : btn.BmpAtlasIndex;
                var status = btn.Enabled == null ? ButtonStatus.Enabled : btn.Enabled();
                var opacity = status == ButtonStatus.Enabled ? 1.0f : 0.25f;

                if (status != ButtonStatus.Disabled && hover)
                    opacity *= 0.75f;
                
                c.DrawBitmapAtlas(bmpButtons[(int)bmpIndex], btn.IconPos.X, btn.IconPos.Y, opacity, iconScaleFloat, tint);
            }
        }

        private void RenderTimecode(CommandList c, int x, int y, int sx, int sy)
        {
            var frame = App.CurrentFrame;
            var famitrackerTempo = App.Project != null && App.Project.UsesFamiTrackerTempo;

            var zeroSizeX  = c.Graphics.MeasureString("0", timeCodeFont);
            var colonSizeX = c.Graphics.MeasureString(":", timeCodeFont);

            var timeCodeSizeY = Height - timecodePosY * 2;
            var textColor = App.IsRecording ? ThemeResources.DarkRedFillBrush : ThemeResources.LightGreyFillBrush2;

            c.PushTranslation(x, y);
            c.FillAndDrawRectangle(0, 0, sx, sy, ThemeResources.BlackBrush, ThemeResources.LightGreyFillBrush2);

            if (Settings.TimeFormat == 0 || famitrackerTempo) // MM:SS:mmm cant be used with FamiTracker tempo.
            {
                var location = NoteLocation.FromAbsoluteNoteIndex(App.SelectedSong, frame);

                var numPatternDigits = Utils.NumDecimalDigits(App.SelectedSong.Length - 1);
                var numNoteDigits = Utils.NumDecimalDigits(App.SelectedSong.GetPatternLength(location.PatternIndex) - 1);

                var patternString = (location.PatternIndex + 1).ToString("D" + numPatternDigits);
                var noteString = location.NoteIndex.ToString("D" + numNoteDigits);

                var charPosX = sx / 2 - ((numPatternDigits + numNoteDigits) * zeroSizeX + colonSizeX) / 2;

                for (int i = 0; i < numPatternDigits; i++, charPosX += zeroSizeX)
                    c.DrawText(patternString[i].ToString(), timeCodeFont, charPosX, 0, textColor, TextFlags.MiddleCenter, zeroSizeX, sy);
                c.DrawText(":", timeCodeFont, charPosX, 0, textColor, TextFlags.MiddleCenter, colonSizeX, sy);
                charPosX += colonSizeX;
                for (int i = 0; i < numNoteDigits; i++, charPosX += zeroSizeX)
                    c.DrawText(noteString[i].ToString(), timeCodeFont, charPosX, 0, textColor, TextFlags.MiddleCenter, zeroSizeX, sy);
            }
            else
            {
                TimeSpan time = App.CurrentTime;

                var minutesString = time.Minutes.ToString("D2");
                var secondsString = time.Seconds.ToString("D2");
                var millisecondsString = time.Milliseconds.ToString("D3");

                // 00:00:000
                var charPosX = sx / 2 - (7 * zeroSizeX + 2 * colonSizeX) / 2;

                for (int i = 0; i < 2; i++, charPosX += zeroSizeX)
                    c.DrawText(minutesString[i].ToString(), timeCodeFont, charPosX, 0, textColor, TextFlags.MiddleCenter, zeroSizeX, sy);
                c.DrawText(":", timeCodeFont, charPosX, 0, textColor, TextFlags.MiddleCenter, colonSizeX, sy);
                charPosX += colonSizeX;
                for (int i = 0; i < 2; i++, charPosX += zeroSizeX)
                    c.DrawText(secondsString[i].ToString(), timeCodeFont, charPosX, 0, textColor, TextFlags.MiddleCenter, zeroSizeX, sy);
                c.DrawText(":", timeCodeFont, charPosX, 0, textColor, TextFlags.MiddleCenter, colonSizeX, sy);
                charPosX += colonSizeX;
                for (int i = 0; i < 3; i++, charPosX += zeroSizeX)
                    c.DrawText(millisecondsString[i].ToString(), timeCodeFont, charPosX, 0, textColor, TextFlags.MiddleCenter, zeroSizeX, sy);
            }

            c.PopTransform();
        }

        private void RenderWarningAndTooltip(CommandList c)
        {
            var scaling = MainWindowScaling;
            var message = tooltip;
            var messageBrush = redTooltip ? warningBrush : ThemeResources.LightGreyFillBrush2;
            var messageFont = ThemeResources.FontMedium;

            if (!string.IsNullOrEmpty(notification))
            {
                var span = DateTime.Now - notificationTime;

                if (span.TotalMilliseconds >= 2000)
                {
                    notification = "";
                }
                else
                {
                    message = (((((long)span.TotalMilliseconds) / 250) & 1) != 0) ? notification : "";
                    messageBrush = notificationWarning ? warningBrush : ThemeResources.LightGreyFillBrush2;
                    messageFont = notificationWarning ? ThemeResources.FontMediumBold : ThemeResources.FontMedium;
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

                            if (specialCharacter.BmpIndex != SpecialCharImageIndices.Count)
                            {
                                c.DrawBitmapAtlas(bmpSpecialCharacters[(int)specialCharacter.BmpIndex], posX, posY + specialCharacter.OffsetY, 1.0f, 1.0f, Theme.LightGreyFillColor1);
                            }
                            else
                            {
                                // Solution used here is an easy workaround for macOS Spotlight being attached to Cmd+Space by default.
                                //
                                // We use `Keys2.Control` flag detection to determine whether Ctrl was pressed or not. On macOS that
                                // flag is present for both Ctrl *and* Cmd keys, which means Ctrl+Space and Cmd+Space are equivalent. 
                                // In other words user can use Ctrl+Space instead of Cmd+Space to avoid Spotlight shortcut clash.
                                //
                                // Then why do we introduce "{ForceCtrl}" monikers in tooltips?
                                //
                                // The problem is we render "{Ctrl}" moniker as "Cmd" button on macOS. It would be more accurate to
                                // render it as "Ctrl/Cmd" button, but we don't have UI space to spare for such longer button being
                                // used in multiple places in tooltips. We could also render "{Ctrl}" moniker as "⌃ or ⌘" which is
                                // short, but introduces a risk of users not being aware what those symbols stand for.
                                //
                                // In such case we decided to distinguish those cases where we want to give user a hint to use Ctrl
                                // instead of Cmd (even if Cmd would still work, if not for a Spotlight shortcut clash). And this is
                                // what `{ForceCtrl}` stands for – it "forces" tooltip rendering to render `{Ctrl}` on macOS as "Ctrl"
                                // instead of "Cmd". 
                                //
                                if (Platform.IsMacOS && str == "Ctrl") str = "Cmd";
                                if (str == "ForceCtrl") str = "Ctrl";
                                
                                posX -= (int)scaling; // HACK: The way we handle fonts in OpenGL is so different, i cant be bothered to debug this.
                                c.DrawRectangle(posX, posY + specialCharacter.OffsetY, posX + specialCharacter.Width, posY + specialCharacter.Height + specialCharacter.OffsetY, messageBrush);
                                c.DrawText(str, messageFont, posX, posY, messageBrush, TextFlags.Center, specialCharacter.Width);
                                posX -= (int)scaling; // HACK: The way we handle fonts in OpenGL is so different, i cant be bothered to debug this.
                            }
                        }
                        else
                        {
                            posX -= c.Graphics.MeasureString(splits[i], messageFont);
                            c.DrawText(str, messageFont, posX, posY, messageBrush);
                        }
                    }

                    posY += tooltipLineSizeY;
                }
            }
        }

        private void RenderShadow(CommandList c)
        {
            if (Platform.IsMobile && IsExpanded)
            {
                c.Transform.GetOrigin(out var ox, out var oy);
                var fullscreenRect = new Rectangle(0, 0, ParentFormSize.Width, ParentFormSize.Height);
                fullscreenRect.Offset(-(int)ox, -(int)oy);
                c.FillRectangle(fullscreenRect, c.Graphics.GetSolidBrush(Color.Black, 1.0f, expandRatio * 0.6f));
            }
        }

        private void RenderBackground(CommandList c)
        {
            if (Platform.IsDesktop)
            {
                c.FillRectangle(0, 0, Width, Height, toolbarBrush);
            }
            else
            {
                var renderSize = RenderSize;

                if (IsLandscape)
                {
                    c.FillRectangle(0, 0, renderSize, Height, toolbarBrush);
                    c.DrawLine(renderSize - 1, 0, renderSize - 1, Height, ThemeResources.BlackBrush);
                }
                else
                {
                    var brush = c.Graphics.GetVerticalGradientBrush(Theme.DarkGreyFillColor2, Theme.DarkGreyFillColor1, LayoutSize);
                    c.FillRectangle(0, 0, Width, RenderSize, toolbarBrush);
                    c.DrawLine(0, renderSize - 1, Width, renderSize - 1, ThemeResources.BlackBrush);
                }
            }
        }

        protected override void OnRender(Graphics g)
        {
            var c = g.CreateCommandList(); // Main

            RenderShadow(c);
            RenderBackground(c);
            RenderButtons(c);
            RenderTimecode(c, timecodePosX, timecodePosY, timecodeOscSizeX, timecodeOscSizeY);
            RenderOscilloscope(c, oscilloscopePosX, oscilloscopePosY, timecodeOscSizeX, timecodeOscSizeY);

            g.DrawCommandList(c);

            if (Platform.IsDesktop)
            {
                var ct = g.CreateCommandList(); // Tooltip (clipped)
                RenderWarningAndTooltip(ct);
                g.DrawCommandList(ct, new Rectangle(lastButtonX, 0, Width, Height));
            }
            else
            {
                if (IsLandscape)
                    c.DrawLine(Width - 1, 0, Width - 1, Height, ThemeResources.BlackBrush);
                else
                    c.DrawLine(0, Height - 1, Width, Height - 1, ThemeResources.BlackBrush);
            }
        }

        public bool ShouldRefreshOscilloscope(bool hasNonZeroSample)
        {
            return oscilloscopeVisible && lastOscilloscopeHadNonZeroSample != hasNonZeroSample;
        }

        private void RenderOscilloscope(CommandList c, int x, int y, int sx, int sy)
        {
            if (!oscilloscopeVisible)
                return;

            c.FillRectangle(x, y, x + sx, y + sy, ThemeResources.BlackBrush);

            var oscilloscopeGeometry = App.GetOscilloscopeGeometry(out lastOscilloscopeHadNonZeroSample);

            if (oscilloscopeGeometry != null && lastOscilloscopeHadNonZeroSample)
            {
                float scaleX = sx;
                float scaleY = sy / -2; // D3D is upside down compared to how we display waves typically.

                c.PushTransform(x, y + sy / 2, scaleX, scaleY);
                c.DrawGeometry(oscilloscopeGeometry, ThemeResources.LightGreyFillBrush2, 1, true);
                c.PopTransform();
            }
            else
            {
                c.PushTranslation(x, y + sy / 2);
                c.DrawLine(0, 0, sx, 0, ThemeResources.LightGreyFillBrush2);
                c.PopTransform();
            }

            if (Platform.IsMobile)
            {
                Utils.SplitVersionNumber(Platform.ApplicationVersion, out var betaNumber);

                if (betaNumber > 0)
                    c.DrawText($"BETA {betaNumber}", ThemeResources.FontSmall, x + 4, y + 4, ThemeResources.LightRedFillBrush);
            }

            c.DrawRectangle(x, y, x + sx, y + sy, ThemeResources.LightGreyFillBrush2);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            MarkDirty();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            foreach (var btn in buttons)
            {
                if (btn != null && btn.Visible && btn.Rect.Contains(e.X, e.Y))
                {
                    SetToolTip(btn.ToolTip);
                    return;
                }
            }

            MarkDirty();
            SetToolTip("");
        }

        private Button GetButtonAtCoord(int x, int y)
        {
            foreach (var btn in buttons)
            {
                if (btn != null && btn.Visible && btn.Rect.Contains(x, y) && (btn.Enabled == null || btn.Enabled() != ButtonStatus.Disabled))
                    return btn;
            }

            return null;
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            GetButtonAtCoord(e.X, e.Y)?.MouseWheel?.Invoke(e.ScrollY);
            base.OnMouseWheel(e);
        }

        protected bool IsPointInTimeCode(int x, int y)
        {
            return x > timecodePosX && x < timecodePosX + timecodeOscSizeX &&
                   y > timecodePosY && y < Height - timecodePosY;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            bool left  = e.Left;

            if (left)
            {
                if (IsPointInTimeCode(e.X, e.Y))
                {
                    Settings.TimeFormat = Settings.TimeFormat == 0 ? 1 : 0;
                    MarkDirty();
                }
                else
                {
                    var btn = GetButtonAtCoord(e.X, e.Y);

                    if (btn != null)
                    {
                        btn.Click?.Invoke(e.X, e.Y);
                        MarkDirty();
                    }
                }
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            bool right = e.Right;

            if (right)
            {
                var btn = GetButtonAtCoord(e.X, e.Y);

                if (btn != null)
                {
                    btn.RightClick?.Invoke(e.X, e.Y);
                    MarkDirty();
                }
            }
        }

        protected override void OnTouchLongPress(int x, int y)
        {
            var btn = GetButtonAtCoord(x, y);

            if (btn != null && btn.RightClick != null)
            {
                if (btn.VibrateOnLongPress)
                    Platform.VibrateClick();
                btn.RightClick(x, y);
                MarkDirty();
                if (btn.CloseOnClick && IsExpanded)
                    StartClosing();
            }
        }

        protected override void OnTouchClick(int x, int y)
        {
            var btn = GetButtonAtCoord(x, y);
            if (btn != null)
            {
                Platform.VibrateTick();
                btn.Click?.Invoke(x, y);
                MarkDirty();
                if (!btn.CloseOnClick)
                    return;
            }

            if (IsPointInTimeCode(x, y))
            {
                Settings.TimeFormat = Settings.TimeFormat == 0 ? 1 : 0;
                Platform.VibrateTick();
                MarkDirty();
                return;
            }

            if (IsExpanded)
            {
                if (btn == null)
                    Platform.VibrateTick();
                StartClosing();
            }
        }
    }
}
