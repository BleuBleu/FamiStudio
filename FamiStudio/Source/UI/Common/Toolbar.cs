using System;
using System.Collections.Generic;
using System.Media;
using System.Windows.Forms;
using System.Diagnostics;

using Color     = System.Drawing.Color;
using Point     = System.Drawing.Point;
using Rectangle = System.Drawing.Rectangle;

using RenderBitmapAtlas = FamiStudio.GLBitmapAtlas;
using RenderBrush       = FamiStudio.GLBrush;
using RenderControl     = FamiStudio.GLControl;
using RenderGraphics    = FamiStudio.GLGraphics;
using RenderCommandList = FamiStudio.GLCommandList;

namespace FamiStudio
{
    public class Toolbar : RenderControl
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
            Undo,
            Redo,
            Transform,
            Config,
            Rewind,
            QwertyPiano,
            Follow,
            Help,
            More,
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
            "Undo",
            "Redo",
            "Transform",
            "Config",
            "Rewind",
            "QwertyPiano",
            "Follow",
            "Help",
            "More"
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
            new MobileButtonLayoutItem(0,  2, ButtonType.Undo),
            new MobileButtonLayoutItem(0,  3, ButtonType.Config),
            new MobileButtonLayoutItem(0,  6, ButtonType.Play),
            new MobileButtonLayoutItem(0,  7, ButtonType.Rec),
            new MobileButtonLayoutItem(0,  8, ButtonType.Help),

            new MobileButtonLayoutItem(1,  0, ButtonType.Save),
            new MobileButtonLayoutItem(1,  1, ButtonType.Paste),
            new MobileButtonLayoutItem(1,  2, ButtonType.Redo),
            new MobileButtonLayoutItem(1,  3, ButtonType.Transform),
            new MobileButtonLayoutItem(1,  6, ButtonType.Rewind),
            new MobileButtonLayoutItem(1,  7, ButtonType.Qwerty),
            new MobileButtonLayoutItem(1,  8, ButtonType.More),

            new MobileButtonLayoutItem(2,  0, ButtonType.New),
            new MobileButtonLayoutItem(2,  1, ButtonType.Cut),
            new MobileButtonLayoutItem(2,  2, ButtonType.Count),
            new MobileButtonLayoutItem(2,  3, ButtonType.Count),
            new MobileButtonLayoutItem(2,  6, ButtonType.Loop),
            new MobileButtonLayoutItem(2,  7, ButtonType.Metronome),
            new MobileButtonLayoutItem(2,  8, ButtonType.Count),

            new MobileButtonLayoutItem(3,  0, ButtonType.Export),
            new MobileButtonLayoutItem(3,  1, ButtonType.Count), // MATTT : Delete
            new MobileButtonLayoutItem(3,  2, ButtonType.Count),
            new MobileButtonLayoutItem(3,  3, ButtonType.Count),
            new MobileButtonLayoutItem(3,  6, ButtonType.Machine),
            new MobileButtonLayoutItem(3,  7, ButtonType.Follow),
            new MobileButtonLayoutItem(3,  8, ButtonType.Count),
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
        const int DefaultButtonIconPosX          = PlatformUtils.IsMobile ?  12 : 2;
        const int DefaultButtonIconPosY          = PlatformUtils.IsMobile ?  12 : 4;
        const int DefaultButtonSize              = PlatformUtils.IsMobile ? 120 : 36;
        const int DefaultIconSize                = PlatformUtils.IsMobile ?  96 : 32; 
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

        DateTime warningTime;
        string warning = "";

        int lastButtonX = 500;
        bool redTooltip = false;
        string tooltip = "";
        RenderBitmapAtlas bmpSpecialCharAtlas;
        Dictionary<string, TooltipSpecialCharacter> specialCharacters = new Dictionary<string, TooltipSpecialCharacter>();

        private delegate void MouseWheelDelegate(int delta);
        private delegate void EmptyDelegate();
        private delegate ButtonStatus ButtonStatusDelegate();
        private delegate ButtonImageIndices BitmapDelegate(ref Color tint);

        // DROIDTODO : Have a separate position + hitbox.
        private class Button
        {
            public Rectangle Rect;
            public Point IconPos;
            public bool Visible = true;
            public bool CloseOnClick = true;
            public string ToolTip;
            public ButtonImageIndices BmpAtlasIndex;
            public ButtonStatusDelegate Enabled;
            public EmptyDelegate Click;
            public EmptyDelegate RightClick;
            public MouseWheelDelegate MouseWheel;
            public BitmapDelegate GetBitmap;
        };

        private int timecodePosX;
        private int timecodePosY;
        private int oscilloscopePosX;
        private int oscilloscopePosY;
        private int timecodeOscSizeX;
        private int timecodeOscSizeY;

        private RenderBrush toolbarBrush;
        private RenderBrush warningBrush;
        private RenderBitmapAtlas bmpButtonAtlas;
        private Button[] buttons = new Button[(int)ButtonType.Count];

        private bool oscilloscopeVisible = true;
        private bool lastOscilloscopeHadNonZeroSample = false;

        // Mobile-only stuff
        private float expandRatio = 0.0f;
        private bool  expanding = false; 
        private bool  closing   = false; 

        public int   LayoutSize  => buttonSize * 2;
        public int   RenderSize  => (int)Math.Round(LayoutSize * (1.0f + Utils.SmootherStep(expandRatio)));
        public float ExpandRatio => expandRatio;
        public bool  IsExpanded  => expandRatio > 0.0f;

        public override bool WantsFullScreenViewport => PlatformUtils.IsMobile;

        private float iconScaleFloat = 1.0f;

        protected override void OnRenderInitialized(RenderGraphics g)
        {
            Debug.Assert((int)ButtonImageIndices.Count == ButtonImageNames.Length);
            Debug.Assert((int)SpecialCharImageIndices.Count == SpecialCharImageNames.Length);

            toolbarBrush = g.CreateVerticalGradientBrush(0, Height, Theme.DarkGreyFillColor2, Theme.DarkGreyFillColor1); // DROIDTODO : Makes no sense on mobile.
            warningBrush = g.CreateSolidBrush(System.Drawing.Color.FromArgb(205, 77, 64));
            bmpButtonAtlas = g.CreateBitmapAtlasFromResources(ButtonImageNames);

            buttons[(int)ButtonType.New]       = new Button { BmpAtlasIndex = ButtonImageIndices.File, Click = OnNew };
            buttons[(int)ButtonType.Open]      = new Button { BmpAtlasIndex = ButtonImageIndices.Open, Click = OnOpen };
            buttons[(int)ButtonType.Save]      = new Button { BmpAtlasIndex = ButtonImageIndices.Save, Click = OnSave, RightClick = OnSaveAs };
            buttons[(int)ButtonType.Export]    = new Button { BmpAtlasIndex = ButtonImageIndices.Export, Click = OnExport, RightClick = OnRepeatLastExport };
            buttons[(int)ButtonType.Copy]      = new Button { BmpAtlasIndex = ButtonImageIndices.Copy, Click = OnCopy, Enabled = OnCopyEnabled };
            buttons[(int)ButtonType.Cut]       = new Button { BmpAtlasIndex = ButtonImageIndices.Cut, Click = OnCut, Enabled = OnCutEnabled };
            buttons[(int)ButtonType.Paste]     = new Button { BmpAtlasIndex = ButtonImageIndices.Paste, Click = OnPaste, RightClick = OnPasteSpecial, Enabled = OnPasteEnabled };
            buttons[(int)ButtonType.Undo]      = new Button { BmpAtlasIndex = ButtonImageIndices.Undo, Click = OnUndo, Enabled = OnUndoEnabled };
            buttons[(int)ButtonType.Redo]      = new Button { BmpAtlasIndex = ButtonImageIndices.Redo, Click = OnRedo, Enabled = OnRedoEnabled };
            buttons[(int)ButtonType.Transform] = new Button { BmpAtlasIndex = ButtonImageIndices.Transform, Click = OnTransform };
            buttons[(int)ButtonType.Config]    = new Button { BmpAtlasIndex = ButtonImageIndices.Config, Click = OnConfig };
            buttons[(int)ButtonType.Play]      = new Button { Click = OnPlay, MouseWheel = OnPlayMouseWheel, GetBitmap = OnPlayGetBitmap };
            buttons[(int)ButtonType.Rec]       = new Button { GetBitmap = OnRecordGetBitmap, Click = OnRecord };
            buttons[(int)ButtonType.Rewind]    = new Button { BmpAtlasIndex = ButtonImageIndices.Rewind, Click = OnRewind };
            buttons[(int)ButtonType.Loop]      = new Button { Click = OnLoop, GetBitmap = OnLoopGetBitmap, CloseOnClick = false };
            buttons[(int)ButtonType.Qwerty]    = new Button { BmpAtlasIndex = ButtonImageIndices.QwertyPiano, Click = OnQwerty, Enabled = OnQwertyEnabled };
            buttons[(int)ButtonType.Metronome] = new Button { BmpAtlasIndex = ButtonImageIndices.Metronome, Click = OnMetronome, Enabled = OnMetronomeEnabled, CloseOnClick = false };
            buttons[(int)ButtonType.Machine]   = new Button { Click = OnMachine, GetBitmap = OnMachineGetBitmap, Enabled = OnMachineEnabled, CloseOnClick = false };
            buttons[(int)ButtonType.Follow]    = new Button { BmpAtlasIndex = ButtonImageIndices.Follow, Click = OnFollow, Enabled = OnFollowEnabled, CloseOnClick = false };
            buttons[(int)ButtonType.Help]      = new Button { BmpAtlasIndex = ButtonImageIndices.Help, Click = OnHelp };

            if (PlatformUtils.IsMobile)
            {
                buttons[(int)ButtonType.More]  = new Button { BmpAtlasIndex = ButtonImageIndices.More, Click = OnMore, Visible = false };

                // On mobile, everything will scale from 1080p.
                var scale = Math.Min(ParentFormSize.Width, ParentFormSize.Height) / 1080.0f;
                var bitmapSize = bmpButtonAtlas.GetElementSize(0);

                buttonIconPosX = ScaleCustom(DefaultButtonIconPosX, scale);
                buttonIconPosY = ScaleCustom(DefaultButtonIconPosY, scale);
                buttonSize     = ScaleCustom(DefaultButtonSize, scale);
                iconSize       = ScaleCustom(DefaultIconSize, scale);
                iconScaleFloat = iconSize / (float)(bitmapSize.Width);
            }
            else 
            {
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

                bmpSpecialCharAtlas = g.CreateBitmapAtlasFromResources(SpecialCharImageNames);

                buttons[(int)ButtonType.New].ToolTip       = "{MouseLeft} New Project {Ctrl} {N}";
                buttons[(int)ButtonType.Open].ToolTip      = "{MouseLeft} Open Project {Ctrl} {O}";
                buttons[(int)ButtonType.Save].ToolTip      = "{MouseLeft} Save Project {Ctrl} {S}\n{MouseRight} Save As...";
                buttons[(int)ButtonType.Export].ToolTip    = "{MouseLeft} Export to various formats {Ctrl} {E}\n{MouseRight} Repeat last export {Ctrl} {Shift} {E}";
                buttons[(int)ButtonType.Copy].ToolTip      = "{MouseLeft} Copy selection {Ctrl} {C}";
                buttons[(int)ButtonType.Cut].ToolTip       = "{MouseLeft} Cut selection {Ctrl} {X}";
                buttons[(int)ButtonType.Paste].ToolTip     = "{MouseLeft} Paste {Ctrl} {V}\n{MouseRight} Paste Special... {Ctrl} {Shift} {V}";
                buttons[(int)ButtonType.Undo].ToolTip      = "{MouseLeft} Undo {Ctrl} {Z}";
                buttons[(int)ButtonType.Redo].ToolTip      = "{MouseLeft} Redo {Ctrl} {Y}";
                buttons[(int)ButtonType.Transform].ToolTip = "{MouseLeft} Perform cleanup and various operations";
                buttons[(int)ButtonType.Config].ToolTip    = "{MouseLeft} Edit Application Settings";
                buttons[(int)ButtonType.Play].ToolTip      = "{MouseLeft} Play/Pause {Space} - {MouseWheel} Change play rate - Play from start of pattern {Ctrl} {Space}\nPlay from start of song {Shift} {Space} - Play from loop point {Ctrl} {Shift} {Space}";
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
                specialCharacters["Alt"]        = new TooltipSpecialCharacter { Width = ScaleForMainWindow(24) };
                specialCharacters["Enter"]      = new TooltipSpecialCharacter { Width = ScaleForMainWindow(38) };
                specialCharacters["Esc"]        = new TooltipSpecialCharacter { Width = ScaleForMainWindow(24) };
                specialCharacters["Del"]        = new TooltipSpecialCharacter { Width = ScaleForMainWindow(24) };
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
                        c.Width = bmpSpecialCharAtlas.GetElementSize((int)c.BmpIndex).Width;
                    c.Height = tooltipSpecialCharSizeY;
                }
            }

            UpdateButtonLayout();
        }

        protected override void OnRenderTerminated()
        {
            Utils.DisposeAndNullify(ref toolbarBrush);
            Utils.DisposeAndNullify(ref warningBrush);
            Utils.DisposeAndNullify(ref bmpButtonAtlas);
            Utils.DisposeAndNullify(ref bmpSpecialCharAtlas);

            specialCharacters.Clear();
        }

        private void UpdateButtonLayout()
        {
            if (!IsRenderInitialized)
                return;

            if (PlatformUtils.IsDesktop)
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
                    btn.Visible = false;

                var numRows = expandRatio >= ShowExtraButtonsThreshold ? 4 : 2;

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
            }
        }

        protected override void OnResize(EventArgs e)
        {
            UpdateButtonLayout();
            expandRatio = 0.0f;
            expanding = false;
            closing = false;
        }

        // DROIDTODO : This makes no sense on mobile, move elsewhere.
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

        public void DisplayWarning(string msg, bool beep)
        {
            warningTime = DateTime.Now;
            warning = "{Warning} " + msg;
            if (beep)
                SystemSounds.Beep.Play();
        }

        public void Tick(float delta)
        {
            if (PlatformUtils.IsDesktop)
            {
                if (!string.IsNullOrEmpty(warning))
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
            App.SaveProjectAsync();
        }

        private void OnSaveAs()
        {
            App.SaveProjectAsync(true);
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

            MarkDirty();
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

        private void OnRewind()
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

        private void OnMetronome()
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

        private void OnHelp()
        {
            App.ShowHelp();
        }

        private void StartClosing()
        {
            expanding = false;
            closing   = expandRatio > 0.0f;
        }

        private void OnMore()
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

        private void RenderButtons(RenderCommandList c)
        {
            var pt = PointToClient(Cursor.Position);

            // Buttons
            foreach (var btn in buttons)
            {
                if (btn == null || !btn.Visible)
                    continue;

                var hover = btn.Rect.Contains(pt) && !PlatformUtils.IsMobile;
                var tint = Theme.LightGreyFillColor1;
                var bmpIndex = btn.GetBitmap != null ? btn.GetBitmap(ref tint) : btn.BmpAtlasIndex;
                var status = btn.Enabled == null ? ButtonStatus.Enabled : btn.Enabled();
                var opacity = status == ButtonStatus.Enabled ? 1.0f : 0.25f;

                if (status != ButtonStatus.Disabled && hover)
                    opacity *= 0.75f;

                c.DrawBitmapAtlas(bmpButtonAtlas, (int)bmpIndex, btn.IconPos.X, btn.IconPos.Y, opacity, iconScaleFloat, tint);
            }
        }

        private void RenderTimecode(RenderCommandList c, int x, int y, int sx, int sy)
        {
            var frame = App.CurrentFrame;
            var famitrackerTempo = App.Project != null && App.Project.UsesFamiTrackerTempo;

            var zeroSizeX  = c.Graphics.MeasureString("0", ThemeResources.FontHuge);
            var colonSizeX = c.Graphics.MeasureString(":", ThemeResources.FontHuge);

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
                    c.DrawText(patternString[i].ToString(), ThemeResources.FontHuge, charPosX, 0, textColor, RenderTextFlags.MiddleCenter, zeroSizeX, sy);
                c.DrawText(":", ThemeResources.FontHuge, charPosX, 0, textColor, RenderTextFlags.MiddleCenter, colonSizeX, sy);
                charPosX += colonSizeX;
                for (int i = 0; i < numNoteDigits; i++, charPosX += zeroSizeX)
                    c.DrawText(noteString[i].ToString(), ThemeResources.FontHuge, charPosX, 0, textColor, RenderTextFlags.MiddleCenter, zeroSizeX, sy);
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
                    c.DrawText(minutesString[i].ToString(), ThemeResources.FontHuge, charPosX, 0, textColor, RenderTextFlags.MiddleCenter, zeroSizeX, sy);
                c.DrawText(":", ThemeResources.FontHuge, charPosX, 0, textColor, RenderTextFlags.MiddleCenter, colonSizeX, sy);
                charPosX += colonSizeX;
                for (int i = 0; i < 2; i++, charPosX += zeroSizeX)
                    c.DrawText(secondsString[i].ToString(), ThemeResources.FontHuge, charPosX, 0, textColor, RenderTextFlags.MiddleCenter, zeroSizeX, sy);
                c.DrawText(":", ThemeResources.FontHuge, charPosX, 0, textColor, RenderTextFlags.MiddleCenter, colonSizeX, sy);
                charPosX += colonSizeX;
                for (int i = 0; i < 3; i++, charPosX += zeroSizeX)
                    c.DrawText(millisecondsString[i].ToString(), ThemeResources.FontHuge, charPosX, 0, textColor, RenderTextFlags.MiddleCenter, zeroSizeX, sy);
            }

            c.PopTransform();
        }

        private void RenderWarningAndTooltip(RenderCommandList c)
        {
            var scaling = MainWindowScaling;
            var message = tooltip;
            var messageBrush = redTooltip ? warningBrush : ThemeResources.LightGreyFillBrush2;
            var messageFont = ThemeResources.FontMedium;

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
                    messageFont = ThemeResources.FontMediumBold;
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
                                c.DrawBitmapAtlas(bmpSpecialCharAtlas, (int)specialCharacter.BmpIndex, posX, posY + specialCharacter.OffsetY, 1.0f, 1.0f, Theme.LightGreyFillColor1);
                            }
                            else
                            {
                                if (PlatformUtils.IsMacOS && str == "Ctrl") str = "Cmd";
                                posX -= (int)scaling; // HACK: The way we handle fonts in OpenGL is so different, i cant be bothered to debug this.
                                c.DrawRectangle(posX, posY + specialCharacter.OffsetY, posX + specialCharacter.Width, posY + specialCharacter.Height + specialCharacter.OffsetY, messageBrush);
                                c.DrawText(str, messageFont, posX, posY, messageBrush, RenderTextFlags.Center, specialCharacter.Width);
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

        private void RenderShadow(RenderCommandList c)
        {
            if (PlatformUtils.IsMobile && IsExpanded)
            {
                c.Transform.GetOrigin(out var ox, out var oy);
                var fullscreenRect = new Rectangle(0, 0, ParentFormSize.Width, ParentFormSize.Height);
                fullscreenRect.Offset(-(int)ox, -(int)oy);
                c.FillRectangle(fullscreenRect, c.Graphics.GetSolidBrush(Color.Black, 1.0f, expandRatio * 0.6f));
            }
        }

        private void RenderBackground(RenderCommandList c)
        {
            if (PlatformUtils.IsDesktop)
            {
                c.FillRectangle(0, 0, Width, Height, toolbarBrush);
            }
            else
            {
                // MATTT : Toolbar brush.
                var brush = c.Graphics.GetSolidBrush(Theme.DarkGreyFillColor1);

                if (IsLandscape)
                    c.FillRectangle(0, 0, RenderSize, Height, brush);
                else
                    c.FillRectangle(0, 0, Width, RenderSize, brush);
            }
        }

        protected override void OnRender(RenderGraphics g)
        {
            var c = g.CreateCommandList(); // Main

            RenderShadow(c);
            RenderBackground(c);
            RenderButtons(c);
            RenderTimecode(c, timecodePosX, timecodePosY, timecodeOscSizeX, timecodeOscSizeY);
            RenderOscilloscope(c, oscilloscopePosX, oscilloscopePosY, timecodeOscSizeX, timecodeOscSizeY);

            g.DrawCommandList(c);

            if (PlatformUtils.IsDesktop)
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

        private void RenderOscilloscope(RenderCommandList c, int x, int y, int sx, int sy)
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
                c.DrawGeometry(oscilloscopeGeometry, ThemeResources.LightGreyFillBrush2, 1.0f, true);
                c.PopTransform();
            }
            else
            {
                c.PushTranslation(x, y + sy / 2);
                c.DrawLine(0, 0, sx, 0, ThemeResources.LightGreyFillBrush2);
                c.PopTransform();
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
            MarkDirty();

            foreach (var btn in buttons)
            {
                if (btn != null && btn.Visible && btn.Rect.Contains(e.X, e.Y))
                {
                    SetToolTip(btn.ToolTip);
                    return;
                }
            }

            SetToolTip("");
            base.OnMouseMove(e);
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
            GetButtonAtCoord(e.X, e.Y)?.MouseWheel.Invoke(e.Delta);
            base.OnMouseWheel(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            bool left  = e.Button.HasFlag(MouseButtons.Left);
            bool right = e.Button.HasFlag(MouseButtons.Right);

            if (left || right)
            {
                if (e.X > timecodePosX && e.X < timecodePosX + timecodeOscSizeX &&
                    e.Y > timecodePosY && e.Y < Height - timecodePosY)
                {
                    Settings.TimeFormat = Settings.TimeFormat == 0 ? 1 : 0;
                    MarkDirty();
                }
                else
                {
                    var btn = GetButtonAtCoord(e.X, e.Y);

                    if (btn != null)
                    {
                        if (left)
                            btn.Click?.Invoke();
                        else
                            btn.RightClick?.Invoke();
                        MarkDirty();
                    }
                }
            }

            base.OnMouseDown(e);
        }

        protected override void OnTouchClick(int x, int y)
        {
            var btn = GetButtonAtCoord(x, y);
            if (btn != null)
            {
                PlatformUtils.VibrateTick();
                btn.Click();
                MarkDirty();
                if (!btn.CloseOnClick)
                    return;
            }

            if (x > timecodePosX && x < timecodePosX + timecodeOscSizeX &&
                y > timecodePosY && y < Height - timecodePosY)
            {
                Settings.TimeFormat = Settings.TimeFormat == 0 ? 1 : 0;
                PlatformUtils.VibrateTick();
                MarkDirty();
                return;
            }

            if (IsExpanded)
            {
                PlatformUtils.VibrateTick();
                StartClosing();
            }
        }
    }
}
