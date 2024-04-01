using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace FamiStudio
{
    public class Toolbar : Container
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
            Wait,
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
            "Wait",
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
        const int DefaultTooltipSpecialCharSizeY = 15;
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

        int lastButtonX = 500;
        int helpButtonX = 500;
        bool redTooltip = false;
        new string tooltip = "";
        Font timeCodeFont;
        TextureAtlasRef[] bmpSpecialCharacters;
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
            //public MouseWheelDelegate MouseWheel;
            public BitmapDelegate GetBitmap;
        };

        private int timecodePosX;
        private int timecodePosY;
        private int oscilloscopePosX;
        private int oscilloscopePosY;
        private int timecodeOscSizeX;
        private int timecodeOscSizeY;

        private Color warningColor = Color.FromArgb(205, 77, 64);
        private TextureAtlasRef[] bmpButtons;
        private Button[] buttons = new Button[(int)ButtonType.Count];

        private bool oscilloscopeVisible = true;
        private bool lastOscilloscopeHadNonZeroSample = false;
        private int  hoverButtonIdx = -1;

        // Mobile-only stuff
        private float expandRatio = 0.0f;
        private bool  expanding = false; 
        private bool  closing   = false; 
        private bool  ticking   = false;

        public int   LayoutSize  => buttonSize * 2;
        public int   RenderSize  => (int)Math.Round(LayoutSize * (1.0f + Utils.SmootherStep(expandRatio) * 0.5f));
        public float ExpandRatio => expandRatio;
        public bool  IsExpanded  => expandRatio > 0.0f;

        public override bool WantsFullScreenViewport => Platform.IsMobile;

        private float iconScaleFloat = 1.0f;

        #region Localization

        // Tooltips
        private LocalizedString NewProjectTooltip;
        private LocalizedString OpenProjectTooltip;
        private LocalizedString RecentFilesTooltip;
        private LocalizedString SaveProjectTooltip;
        private LocalizedString MoreOptionsTooltip;
        private LocalizedString ExportTooltip;
        private LocalizedString CopySelectionTooltip;
        private LocalizedString CutSelectionTooltip;
        private LocalizedString PasteTooltip;
        private LocalizedString UndoTooltip;
        private LocalizedString RedoTooltip;
        private LocalizedString CleanupTooltip;
        private LocalizedString SettingsTooltip;
        private LocalizedString PlayPauseTooltip;
        private LocalizedString RewindTooltip;
        private LocalizedString RewindPatternTooltip;
        private LocalizedString ToggleRecordingTooltip;
        private LocalizedString AbortRecordingTooltip;
        private LocalizedString ToggleLoopModeTooltip;
        private LocalizedString ToggleQWERTYTooltip;
        private LocalizedString ToggleMetronomeTooltip;
        private LocalizedString TogglePALTooltip;
        private LocalizedString ToggleFollowModeTooltip;
        private LocalizedString DocumentationTooltip;

        // Context menus
        private LocalizedString SaveAsLabel;
        private LocalizedString SaveAsTooltip;
        private LocalizedString RepeatExportLabel;
        private LocalizedString RepeatExportTooltip;
        private LocalizedString PasteSpecialLabel;
        private LocalizedString PasteSpecialTooltip;
        private LocalizedString DeleteSpecialLabel;
        private LocalizedString PlayBeginSongLabel;
        private LocalizedString PlayBeginSongTooltip;
        private LocalizedString PlayBeginPatternLabel;
        private LocalizedString PlayBeginPatternTooltip;
        private LocalizedString PlayLoopPointLabel;
        private LocalizedString PlayLoopPointTooltip;
        private LocalizedString RegularSpeedLabel;
        private LocalizedString RegularSpeedTooltip;
        private LocalizedString HalfSpeedLabel;
        private LocalizedString HalfSpeedTooltip;
        private LocalizedString QuarterSpeedLabel;
        private LocalizedString QuarterSpeedTooltip;
        private LocalizedString AccurateSeekLabel;
        private LocalizedString AccurateSeekTooltip;

        #endregion

        public Toolbar()
        {
            Localization.Localize(this);
            Settings.KeyboardShortcutsChanged += Settings_KeyboardShortcutsChanged;
            SetTickEnabled(Platform.IsMobile);
        }

        protected override void OnAddedToContainer()
        {
            Debug.Assert((int)ButtonImageIndices.Count == ButtonImageNames.Length);
            Debug.Assert((int)SpecialCharImageIndices.Count == SpecialCharImageNames.Length);

            var g = ParentWindow.Graphics;
            bmpButtons = g.GetTextureAtlasRefs(ButtonImageNames);
            timeCodeFont = Fonts.FontHuge;

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

                buttonIconPosX = DpiScaling.ScaleCustom(DefaultButtonIconPosX, scale);
                buttonIconPosY = DpiScaling.ScaleCustom(DefaultButtonIconPosY, scale);
                buttonSize     = DpiScaling.ScaleCustom(DefaultButtonSize, scale);
                iconSize       = DpiScaling.ScaleCustom(DefaultIconSize, scale);
                iconScaleFloat = iconSize / (float)(bitmapSize.Width);
            }
            else
            {
                buttons[(int)ButtonType.Qwerty] = new Button { BmpAtlasIndex = ButtonImageIndices.QwertyPiano, Click = OnQwerty, Enabled = OnQwertyEnabled };

                timecodePosY            = DpiScaling.ScaleForWindow(DefaultTimecodePosY);
                oscilloscopePosY        = DpiScaling.ScaleForWindow(DefaultTimecodePosY);
                timecodeOscSizeX        = DpiScaling.ScaleForWindow(DefaultTimecodeSizeX);
                tooltipSingleLinePosY   = DpiScaling.ScaleForWindow(DefaultTooltipSingleLinePosY);
                tooltipMultiLinePosY    = DpiScaling.ScaleForWindow(DefaultTooltipMultiLinePosY);
                tooltipLineSizeY        = DpiScaling.ScaleForWindow(DefaultTooltipLineSizeY);
                tooltipSpecialCharSizeX = DpiScaling.ScaleForWindow(DefaultTooltipSpecialCharSizeX);
                tooltipSpecialCharSizeY = DpiScaling.ScaleForWindow(DefaultTooltipSpecialCharSizeY);
                buttonIconPosX          = DpiScaling.ScaleForWindow(DefaultButtonIconPosX);
                buttonIconPosY          = DpiScaling.ScaleForWindow(DefaultButtonIconPosY);
                buttonSize              = DpiScaling.ScaleForWindow(DefaultButtonSize);

                bmpSpecialCharacters = g.GetTextureAtlasRefs(SpecialCharImageNames);

                specialCharacters["Shift"]      = new TooltipSpecialCharacter { Width = DpiScaling.ScaleForWindow(32) };
                specialCharacters["Space"]      = new TooltipSpecialCharacter { Width = DpiScaling.ScaleForWindow(38) };
                specialCharacters["Home"]       = new TooltipSpecialCharacter { Width = DpiScaling.ScaleForWindow(38) };
                specialCharacters["Ctrl"]       = new TooltipSpecialCharacter { Width = DpiScaling.ScaleForWindow(28) };
                specialCharacters["ForceCtrl"]  = new TooltipSpecialCharacter { Width = DpiScaling.ScaleForWindow(28) };
                specialCharacters["Alt"]        = new TooltipSpecialCharacter { Width = DpiScaling.ScaleForWindow(24) };
                specialCharacters["Tab"]        = new TooltipSpecialCharacter { Width = DpiScaling.ScaleForWindow(24) };
                specialCharacters["Enter"]      = new TooltipSpecialCharacter { Width = DpiScaling.ScaleForWindow(38) };
                specialCharacters["Esc"]        = new TooltipSpecialCharacter { Width = DpiScaling.ScaleForWindow(24) };
                specialCharacters["Del"]        = new TooltipSpecialCharacter { Width = DpiScaling.ScaleForWindow(24) };
                specialCharacters["F1"]         = new TooltipSpecialCharacter { Width = DpiScaling.ScaleForWindow(18) };
                specialCharacters["F2"]         = new TooltipSpecialCharacter { Width = DpiScaling.ScaleForWindow(18) };
                specialCharacters["F3"]         = new TooltipSpecialCharacter { Width = DpiScaling.ScaleForWindow(18) };
                specialCharacters["F4"]         = new TooltipSpecialCharacter { Width = DpiScaling.ScaleForWindow(18) };
                specialCharacters["F5"]         = new TooltipSpecialCharacter { Width = DpiScaling.ScaleForWindow(18) };
                specialCharacters["F6"]         = new TooltipSpecialCharacter { Width = DpiScaling.ScaleForWindow(18) };
                specialCharacters["F7"]         = new TooltipSpecialCharacter { Width = DpiScaling.ScaleForWindow(18) };
                specialCharacters["F8"]         = new TooltipSpecialCharacter { Width = DpiScaling.ScaleForWindow(18) };
                specialCharacters["F9"]         = new TooltipSpecialCharacter { Width = DpiScaling.ScaleForWindow(18) };
                specialCharacters["F10"]        = new TooltipSpecialCharacter { Width = DpiScaling.ScaleForWindow(24) };
                specialCharacters["F11"]        = new TooltipSpecialCharacter { Width = DpiScaling.ScaleForWindow(24) };
                specialCharacters["F12"]        = new TooltipSpecialCharacter { Width = DpiScaling.ScaleForWindow(24) };
                specialCharacters["Drag"]       = new TooltipSpecialCharacter { BmpIndex = SpecialCharImageIndices.Drag,       OffsetY = DpiScaling.ScaleForWindow(2) };
                specialCharacters["MouseLeft"]  = new TooltipSpecialCharacter { BmpIndex = SpecialCharImageIndices.MouseLeft,  OffsetY = DpiScaling.ScaleForWindow(2) };
                specialCharacters["MouseRight"] = new TooltipSpecialCharacter { BmpIndex = SpecialCharImageIndices.MouseRight, OffsetY = DpiScaling.ScaleForWindow(2) };
                specialCharacters["MouseWheel"] = new TooltipSpecialCharacter { BmpIndex = SpecialCharImageIndices.MouseWheel, OffsetY = DpiScaling.ScaleForWindow(2) };
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

                UpdateTooltips();
            }

            UpdateButtonLayout();
        }

        private void Settings_KeyboardShortcutsChanged()
        {
            UpdateTooltips();
        }

        private void UpdateTooltips()
        {
            if (Platform.IsDesktop)
            {
                buttons[(int)ButtonType.New].ToolTip       = $"<MouseLeft> {NewProjectTooltip} {Settings.FileNewShortcut.TooltipString}";
                buttons[(int)ButtonType.Open].ToolTip      = $"<MouseLeft> {OpenProjectTooltip} {Settings.FileOpenShortcut.TooltipString}\n<MouseRight> {RecentFilesTooltip}";
                buttons[(int)ButtonType.Save].ToolTip      = $"<MouseLeft> {SaveProjectTooltip} {Settings.FileSaveShortcut.TooltipString}\n<MouseRight> {MoreOptionsTooltip}";
                buttons[(int)ButtonType.Export].ToolTip    = $"<MouseLeft> {ExportTooltip} {Settings.FileExportShortcut.TooltipString}\n<MouseRight> {MoreOptionsTooltip}";
                buttons[(int)ButtonType.Copy].ToolTip      = $"<MouseLeft> {CopySelectionTooltip} {Settings.CopyShortcut.TooltipString}";
                buttons[(int)ButtonType.Cut].ToolTip       = $"<MouseLeft> {CutSelectionTooltip} {Settings.CutShortcut.TooltipString}";
                buttons[(int)ButtonType.Paste].ToolTip     = $"<MouseLeft> {PasteTooltip} {Settings.PasteShortcut.TooltipString}\n<MouseRight> {MoreOptionsTooltip}";
                buttons[(int)ButtonType.Undo].ToolTip      = $"<MouseLeft> {UndoTooltip} {Settings.UndoShortcut.TooltipString}";
                buttons[(int)ButtonType.Redo].ToolTip      = $"<MouseLeft> {RedoTooltip} {Settings.RedoShortcut.TooltipString}";
                buttons[(int)ButtonType.Transform].ToolTip = $"<MouseLeft> {CleanupTooltip}";
                buttons[(int)ButtonType.Config].ToolTip    = $"<MouseLeft> {SettingsTooltip}";
                buttons[(int)ButtonType.Play].ToolTip      = $"<MouseLeft> {PlayPauseTooltip} {Settings.PlayShortcut.TooltipString} - <MouseRight> {MoreOptionsTooltip}";
                buttons[(int)ButtonType.Rewind].ToolTip    = $"<MouseLeft> {RewindTooltip} {Settings.SeekStartShortcut.TooltipString}\n{RewindPatternTooltip} {Settings.SeekStartPatternShortcut.TooltipString}";
                buttons[(int)ButtonType.Rec].ToolTip       = $"<MouseLeft> {ToggleRecordingTooltip} {Settings.RecordingShortcut.TooltipString}\n{AbortRecordingTooltip} <Esc>";
                buttons[(int)ButtonType.Loop].ToolTip      = $"<MouseLeft> {ToggleLoopModeTooltip}";
                buttons[(int)ButtonType.Qwerty].ToolTip    = $"<MouseLeft> {ToggleQWERTYTooltip} {Settings.QwertyShortcut.TooltipString}";
                buttons[(int)ButtonType.Metronome].ToolTip = $"<MouseLeft> {ToggleMetronomeTooltip}";
                buttons[(int)ButtonType.Machine].ToolTip   = $"<MouseLeft> {TogglePALTooltip}";
                buttons[(int)ButtonType.Follow].ToolTip    = $"<MouseLeft> {ToggleFollowModeTooltip} {Settings.FollowModeShortcut.TooltipString}";
                buttons[(int)ButtonType.Help].ToolTip      = $"<MouseLeft> {DocumentationTooltip}";
            }
        }

        private void UpdateButtonLayout()
        {
            if (ParentContainer == null)
                return;

            if (Platform.IsDesktop)
            {
                // Hide a few buttons if the window is too small (out min "usable" resolution is ~1280x720).
                var hideLessImportantButtons = Width < 1420 * DpiScaling.Window;
                var hideOscilloscope         = Width < 1250 * DpiScaling.Window;

                var x = 0;

                for (int i = 0; i < (int)ButtonType.Count; i++)
                {
                    var btn = buttons[i];

                    if (btn == null)
                        continue;

                    if (i == (int)ButtonType.Help)
                    {
                        btn.Rect = new Rectangle(Width - buttonSize, 0, buttonSize, buttonSize);
                        helpButtonX = btn.Rect.Left;
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

                        oscilloscopeVisible = !hideOscilloscope;
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

                timeCodeFont = Fonts.GetBestMatchingFontByWidth("00:00:000", timecodeOscSizeX, false);
            }
        }

        protected override void OnResize(EventArgs e)
        {
            if (!ticking)
            {
                expandRatio = 0.0f;
                expanding = false;
                closing = false;
                UpdateButtonLayout();
            }
        }

        public void LayoutChanged()
        {
            UpdateButtonLayout();
            MarkDirty();
        }

        public override bool HitTest(int winX, int winY)
        {
            // Eat all the input when expanded.
            return Platform.IsMobile && IsExpanded || base.HitTest(winX, winY);
        }

        public void SetToolTip(string msg, bool red = false)
        {
            if (tooltip != msg || red != redTooltip)
            {
                Debug.Assert(msg == null || (!msg.Contains('{') && !msg.Contains('}'))); // Temporary until i migrated everything.
                tooltip = msg;
                redTooltip = red;
                MarkDirty();
            }
        }

        public override void Tick(float delta)
        {
            if (Platform.IsMobile)
            {
                var prevRatio = expandRatio;

                ticking = true;
                if (expanding)
                {
                    delta *= 6.0f;
                    expandRatio = Math.Min(1.0f, expandRatio + delta);
                    if (prevRatio < ShowExtraButtonsThreshold && expandRatio >= ShowExtraButtonsThreshold)
                        UpdateButtonLayout();
                    if (expandRatio == 1.0f)
                        expanding = false;
                    MarkDirty();
                    ParentTopContainer.UpdateLayout();
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
                    ParentTopContainer.UpdateLayout();
                }
                ticking = false;
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
                new ContextMenuOption("MenuSave", SaveAsLabel, $"{SaveAsTooltip} {Settings.FileSaveAsShortcut.TooltipString}", () => { App.SaveProjectAsync(true); }),
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
                new ContextMenuOption("MenuExport", RepeatExportLabel, $"{RepeatExportTooltip} {Settings.FileExportRepeatShortcut.TooltipString}", () => { App.RepeatLastExport(); }),
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

        // Unused.
        //private void OnCopyAsText(int x, int y)
        //{
        //    if (App.CanCopyAsText)
        //    {
        //        App.ShowContextMenu(left + x, top + y, new[]
        //        {
        //            new ContextMenuOption("MenuCopy", "Copy as Text", "Copy context as human readable text", () => { App.CopyAsText(); }),
        //        });
        //    }
        //}

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
                new ContextMenuOption("MenuStar", PasteSpecialLabel, $"{PasteSpecialTooltip} {Settings.PasteSpecialShortcut.TooltipString}", () => { App.PasteSpecial(); }),
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
                new ContextMenuOption("MenuStar", DeleteSpecialLabel, () => { App.DeleteSpecial(); }),
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
                new ContextMenuOption("MenuPlay", PlayBeginSongLabel, $"{PlayBeginSongTooltip} {Settings.PlayFromStartShortcut.TooltipString}", () => { App.StopSong(); App.PlaySongFromBeginning(); } ),
                new ContextMenuOption("MenuPlay", PlayBeginPatternLabel, $"{PlayBeginPatternTooltip} {Settings.PlayFromPatternShortcut.TooltipString}", () => { App.StopSong(); App.PlaySongFromStartOfPattern(); } ),
                new ContextMenuOption("MenuPlay", PlayLoopPointLabel, $"{PlayLoopPointTooltip} {Settings.PlayFromLoopShortcut.TooltipString}", () => { App.StopSong(); App.PlaySongFromLoopPoint(); } ),
                new ContextMenuOption(RegularSpeedLabel, RegularSpeedTooltip, () => { App.PlayRate = 1; }, () => App.PlayRate == 1 ? ContextMenuCheckState.Radio : ContextMenuCheckState.None, ContextMenuSeparator.MobileBefore ),
                new ContextMenuOption(HalfSpeedLabel,    HalfSpeedTooltip,    () => { App.PlayRate = 2; }, () => App.PlayRate == 2 ? ContextMenuCheckState.Radio : ContextMenuCheckState.None ),
                new ContextMenuOption(QuarterSpeedLabel, QuarterSpeedTooltip, () => { App.PlayRate = 4; }, () => App.PlayRate == 4 ? ContextMenuCheckState.Radio : ContextMenuCheckState.None ),
                new ContextMenuOption(AccurateSeekLabel, AccurateSeekTooltip, () => { App.AccurateSeek = !App.AccurateSeek; }, () => App.AccurateSeek ? ContextMenuCheckState.Checked : ContextMenuCheckState.Unchecked, ContextMenuSeparator.MobileBefore )
            });
        }

        private ButtonImageIndices OnPlayGetBitmap(ref Color tint)
        {
            if (App.IsPlaying)
            {
                if (App.IsSeeking)
                {
                    tint = Theme.Darken(tint, (int)(Math.Abs(Math.Sin(Platform.TimeSeconds() * 12.0)) * 64));
                    return ButtonImageIndices.Wait;
                }
                else
                {
                    return ButtonImageIndices.Pause;
                }
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
                tint = Theme.DarkRedColor;
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
            // Buttons
            for (int i = 0; i < buttons.Length; i++)
            {
                var btn = buttons[i];

                if (btn == null || !btn.Visible)
                    continue;

                var hover = hoverButtonIdx == i;
                var tint = Theme.LightGreyColor1;
                var bmpIndex = btn.GetBitmap != null ? btn.GetBitmap(ref tint) : btn.BmpAtlasIndex;
                var status = btn.Enabled == null ? ButtonStatus.Enabled : btn.Enabled();
                var opacity = status == ButtonStatus.Enabled ? 1.0f : 0.25f;

                if (status != ButtonStatus.Disabled && hover)
                    opacity *= 0.75f;
                
                c.DrawTextureAtlas(bmpButtons[(int)bmpIndex], btn.IconPos.X, btn.IconPos.Y, iconScaleFloat, tint.Transparent(opacity));
            }
        }

        private void RenderTimecode(CommandList c, int x, int y, int sx, int sy)
        {
            var frame = App.CurrentFrame;
            var famitrackerTempo = App.Project != null && App.Project.UsesFamiTrackerTempo;

            var zeroSizeX  = c.Graphics.MeasureString("0", timeCodeFont);
            var colonSizeX = c.Graphics.MeasureString(":", timeCodeFont);

            var textColor = App.IsRecording ? Theme.DarkRedColor : Theme.LightGreyColor2;

            c.PushTranslation(x, y);
            c.FillAndDrawRectangle(0, 0, sx, sy, Theme.BlackColor, Theme.LightGreyColor2);

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
            var scaling = DpiScaling.Window;
            var message = tooltip;
            var messageColor = redTooltip ? warningColor : Theme.LightGreyColor2;
            var messageFont = Fonts.FontMedium;

            // Tooltip
            if (!string.IsNullOrEmpty(message))
            {
                var lines = message.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                var posY = lines.Length == 1 ? tooltipSingleLinePosY : tooltipMultiLinePosY;

                for (int j = 0; j < lines.Length; j++)
                {
                    var splits = lines[j].Split(new char[] { '<', '>' }, StringSplitOptions.RemoveEmptyEntries);
                    var posX = Width - 40 * scaling;

                    for (int i = splits.Length - 1; i >= 0; i--)
                    {
                        var str = splits[i];

                        if (specialCharacters.TryGetValue(str, out var specialCharacter))
                        {
                            posX -= specialCharacter.Width;

                            if (specialCharacter.BmpIndex != SpecialCharImageIndices.Count)
                            {
                                c.DrawTextureAtlas(bmpSpecialCharacters[(int)specialCharacter.BmpIndex], posX, posY + specialCharacter.OffsetY, 1.0f, Theme.LightGreyColor1);
                            }
                            else
                            {
                                if (Platform.IsMacOS && str == "Ctrl") str = "Cmd";
                                
                                c.DrawRectangle(posX, posY + specialCharacter.OffsetY, posX + specialCharacter.Width - (int)scaling, posY + specialCharacter.Height + specialCharacter.OffsetY, messageColor);
                                c.DrawText(str, messageFont, posX, posY, messageColor, TextFlags.Center, specialCharacter.Width);
                            }
                        }
                        else
                        {
                            posX -= c.Graphics.MeasureString(splits[i], messageFont);
                            c.DrawText(str, messageFont, posX, posY, messageColor);
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
                if (IsLandscape)
                    c.FillRectangle(RenderSize, 0, ParentWindowSize.Width, ParentWindowSize.Height, Color.FromArgb(expandRatio * 0.6f, Color.Black));
                else
                    c.FillRectangle(0, RenderSize, ParentWindowSize.Width, ParentWindowSize.Height, Color.FromArgb(expandRatio * 0.6f, Color.Black));
            }
        }

        private void RenderBackground(CommandList c)
        {
            if (Platform.IsDesktop)
            {
                c.FillRectangleGradient(0, 0, Width, Height, Theme.DarkGreyColor5, Theme.DarkGreyColor4, true, Height);
            }
            else
            {
                var renderSize = RenderSize;

                if (IsLandscape)
                {
                    c.FillRectangle(0, 0, renderSize, Height, Theme.DarkGreyColor4);
                    c.DrawLine(renderSize - 1, 0, renderSize - 1, Height, Theme.BlackColor);
                }
                else
                {
                    c.FillRectangle(0, 0, Width, RenderSize, Theme.DarkGreyColor4);
                    c.DrawLine(0, renderSize - 1, Width, renderSize - 1, Theme.BlackColor);
                }
            }
        }

        protected override void OnRender(Graphics g)
        {
            var c = g.DefaultCommandList;
            var o = g.OverlayCommandList;

            if (Platform.IsMobile)
            {
                if (IsLandscape)
                    g.PushClipRegion(0, 0, RenderSize, height, false);
                else
                    g.PushClipRegion(0, 0, width, RenderSize, false);
            }

            RenderShadow(o);
            RenderBackground(c);
            RenderButtons(c);
            RenderTimecode(c, timecodePosX, timecodePosY, timecodeOscSizeX, timecodeOscSizeY);
            RenderOscilloscope(c, oscilloscopePosX, oscilloscopePosY, timecodeOscSizeX, timecodeOscSizeY);

            if (Platform.IsDesktop)
            {
                c.PushClipRegion(lastButtonX, 0, helpButtonX - lastButtonX, Height);
                RenderBackground(c);
                RenderWarningAndTooltip(c);
                c.PopClipRegion();
            }
            else
            {
                if (IsLandscape)
                    c.DrawLine(RenderSize - 1, 0, RenderSize - 1, Height, Theme.BlackColor);
                else
                    c.DrawLine(0, RenderSize - 1, Width, RenderSize - 1, Theme.BlackColor);

                c.PopClipRegion();
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

            c.PushClipRegion(x + 1, y + 1, sx - 1, sy - 1);
            c.FillRectangle(x, y, x + sx, y + sy, Theme.BlackColor);

            var oscilloscopeGeometry = App.GetOscilloscopeGeometry(out lastOscilloscopeHadNonZeroSample);

            if (oscilloscopeGeometry != null && lastOscilloscopeHadNonZeroSample)
            {
                float scaleX = sx;
                float scaleY = sy / -2; // D3D is upside down compared to how we display waves typically.

                c.PushTransform(x, y + sy / 2, scaleX, scaleY);
                c.DrawNiceSmoothLine(oscilloscopeGeometry, Theme.LightGreyColor2);
                c.PopTransform();
            }
            else
            {
                c.PushTranslation(x, y + sy / 2);
                c.DrawLine(0, 0, sx, 0, Theme.LightGreyColor2);
                c.PopTransform();
            }

            if (Platform.IsMobile)
            {
                Utils.SplitVersionNumber(Platform.ApplicationVersion, out var betaNumber);

                if (betaNumber > 0)
                    c.DrawText($"BETA {betaNumber}", Fonts.FontSmall, x + 4, y + 4, Theme.LightRedColor);
            }

            c.PopClipRegion();

            c.DrawRectangle(x, y, x + sx, y + sy, Theme.LightGreyColor2);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            SetAndMarkDirty(ref hoverButtonIdx, -1);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            var newHoverButtonIdx = -1;
            var newTooltip = "";

            for (int i = 0; i < buttons.Length; i++)
            {
                var btn = buttons[i];

                if (btn != null && btn.Visible && btn.Rect.Contains(e.X, e.Y))
                {
                    newHoverButtonIdx = i;
                    newTooltip = btn.ToolTip;
                    break;
                }
            }

            SetAndMarkDirty(ref hoverButtonIdx, newHoverButtonIdx);
            SetToolTip(newTooltip);
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

        //protected override void OnMouseWheel(MouseEventArgs e)
        //{
        //    GetButtonAtCoord(e.X, e.Y)?.MouseWheel?.Invoke(e.ScrollY);
        //    base.OnMouseWheel(e);
        //}

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
                if (Platform.IsMobile && !ClientRectangle.Contains(e.X, e.Y))
                {
                    StartClosing();
                }
                else if (IsPointInTimeCode(e.X, e.Y))
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

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            OnMouseDown(e);
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
