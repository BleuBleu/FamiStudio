using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace FamiStudio
{
    public class Toolbar : Container
    {
        private Button buttonNew;
        private Button buttonOpen;
        private Button buttonSave;
        private Button buttonExport;
        private Button buttonCopy;
        private Button buttonCut;
        private Button buttonPaste;
        private Button buttonDelete;
        private Button buttonUndo;
        private Button buttonRedo;
        private Button buttonTransform;
        private Button buttonConfig;
        private Button buttonPlay;
        private Button buttonRec;
        private Button buttonRewind;
        private Button buttonLoop;
        private Button buttonQwerty;
        private Button buttonMetronome;
        private Button buttonMachine;
        private Button buttonFollow;
        private Button buttonHelp;
        private Button buttonMore;
        private Button buttonPiano;
        private List<Button> allButtons = new List<Button>();

        private Oscilloscope oscilloscope;
        private Timecode timecode;
        private TooltipLabel tooltipLabel;

        //private enum ButtonStatus
        //{
        //    Enabled,
        //    Disabled,
        //    Dimmed
        //}

        //private enum ButtonImageIndices
        //{ 
        //    LoopNone,
        //    Loop,
        //    LoopPattern,
        //    LoopSelection,
        //    Play,
        //    PlayHalf,
        //    PlayQuarter,
        //    Pause,
        //    Wait,
        //    NTSC,
        //    PAL,
        //    NTSCToPAL,
        //    PALToNTSC,
        //    Rec,
        //    Metronome,
        //    File,
        //    Open,
        //    Save,
        //    Export,
        //    Copy,
        //    Cut,
        //    Paste,
        //    Delete,
        //    Undo,
        //    Redo,
        //    Transform,
        //    Config,
        //    Rewind,
        //    QwertyPiano,
        //    Follow,
        //    Help,
        //    More,
        //    Piano,
        //    Count
        //};

        //private readonly string[] ButtonImageNames = new string[]
        //{
        //    "LoopNone",
        //    "Loop",
        //    "LoopPattern",
        //    "LoopSelection",
        //    "Play",
        //    "PlayHalf",
        //    "PlayQuarter",
        //    "Pause",
        //    "Wait",
        //    "NTSC",
        //    "PAL",
        //    "NTSCToPAL",
        //    "PALToNTSC",
        //    "Rec",
        //    "Metronome",
        //    "File",
        //    "Open",
        //    "Save",
        //    "Export",
        //    "Copy",
        //    "Cut",
        //    "Paste",
        //    "Delete",
        //    "Undo",
        //    "Redo",
        //    "Transform",
        //    "Config",
        //    "Rewind",
        //    "QwertyPiano",
        //    "Follow",
        //    "Help",
        //    "More",
        //    "Piano"
        //};

        // Mobile-only layout.
        private struct MobileButtonLayoutItem
        {
            public MobileButtonLayoutItem(int r, int c, string b)
            {
                row = r;
                col = c;
                btn = b;
            }
            public int row;
            public int col;
            public string btn;
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
            new MobileButtonLayoutItem(0, 0, "Open"),
            new MobileButtonLayoutItem(0, 1, "Copy"),
            new MobileButtonLayoutItem(0, 2, "Cut"),
            new MobileButtonLayoutItem(0, 3, "Undo"),
            new MobileButtonLayoutItem(0, 6, "Play"),
            new MobileButtonLayoutItem(0, 7, "Rec"),
            new MobileButtonLayoutItem(0, 8, "Help"),
            new MobileButtonLayoutItem(1, 0, "Save"),
            new MobileButtonLayoutItem(1, 1, "Paste"),
            new MobileButtonLayoutItem(1, 2, "Delete"),
            new MobileButtonLayoutItem(1, 3, "Redo"),
            new MobileButtonLayoutItem(1, 6, "Rewind"),
            new MobileButtonLayoutItem(1, 7, "Piano"),
            new MobileButtonLayoutItem(1, 8, "More"),
            new MobileButtonLayoutItem(2, 0, "New"),
            new MobileButtonLayoutItem(2, 1, "Export"),
            new MobileButtonLayoutItem(2, 2, "Config"),
            new MobileButtonLayoutItem(2, 3, "Transform"),
            new MobileButtonLayoutItem(2, 4, "Machine"),
            new MobileButtonLayoutItem(2, 5, "Follow"),
            new MobileButtonLayoutItem(2, 6, "Loop"),
            new MobileButtonLayoutItem(2, 7, "Metronome"),
            new MobileButtonLayoutItem(2, 8, "Count"),
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
        // MATTT : Can we initialize those immediately like we do for controls now?
        const int DefaultButtonSize              = Platform.IsMobile ? 120 : 36;
        const int DefaultIconSize                = Platform.IsMobile ?  96 : 32; 
        const float ShowExtraButtonsThreshold    = 0.8f;

        int buttonSize;

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

        // MATTT : Disable color too intense. Review dimmed color too.
        private Button CreateToolbarButton(string image, string userData)
        {
            var button = new Button(image);
            button.UserData = userData;
            button.Visible = false;
            button.ImageScale = iconScaleFloat;
            button.Transparent = true;
            button.Resize(buttonSize, buttonSize);
            allButtons.Add(button);
            AddControl(button);
            return button;
        }

        protected override void OnAddedToContainer()
        {
            var g = ParentWindow.Graphics;
            
            // MATTT : Review these calculation on mobile.
            if (Platform.IsMobile)
            {
                // On mobile, everything will scale from 1080p.
                var screenSize = Platform.GetScreenResolution();
                var scale = Math.Min(screenSize.Width, screenSize.Height) / 1080.0f;

                //buttonIconPosX = DpiScaling.ScaleCustom(DefaultButtonIconPosX, scale);
                //buttonIconPosY = DpiScaling.ScaleCustom(DefaultButtonIconPosY, scale);
                buttonSize     = DpiScaling.ScaleCustom(DefaultButtonSize, scale);
                iconScaleFloat = DpiScaling.ScaleCustom(DefaultIconSize, scale) / (float)(DefaultIconSize);

            }
            else
            {
                //timecodePosY     = DpiScaling.ScaleForWindow(DefaultTimecodePosY);
                //oscilloscopePosY = DpiScaling.ScaleForWindow(DefaultTimecodePosY);
                //timecodeOscSizeX = DpiScaling.ScaleForWindow(DefaultTimecodeSizeX);
                //buttonIconPosX   = DpiScaling.ScaleForWindow(DefaultButtonIconPosX);
                //buttonIconPosY   = DpiScaling.ScaleForWindow(DefaultButtonIconPosY);
                buttonSize       = DpiScaling.ScaleForWindow(DefaultButtonSize);
            }

            buttonNew       = CreateToolbarButton("File", "New");
            buttonOpen      = CreateToolbarButton("Open", "Open");
            buttonSave      = CreateToolbarButton("Save", "Save");
            buttonExport    = CreateToolbarButton("Export", "Export");
            buttonCopy      = CreateToolbarButton("Copy", "Copy");
            buttonCut       = CreateToolbarButton("Cut", "Cut");
            buttonPaste     = CreateToolbarButton("Paste", "Paste");
            buttonUndo      = CreateToolbarButton("Undo", "Undo");
            buttonRedo      = CreateToolbarButton("Redo", "Redo");
            buttonTransform = CreateToolbarButton("Transform", "Transform");
            buttonConfig    = CreateToolbarButton("Config", "Config");
            buttonPlay      = CreateToolbarButton("Play", "Play");
            buttonRec       = CreateToolbarButton("Rec", "Rec");
            buttonRewind    = CreateToolbarButton("Rewind", "Rewind");
            buttonLoop      = CreateToolbarButton("Loop", "Loop");
            buttonQwerty    = CreateToolbarButton("QwertyPiano", "Qwerty"); // MATTT : Desktop only.
            buttonMetronome = CreateToolbarButton("Metronome", "Metronome");
            buttonMachine   = CreateToolbarButton("NTSC", "Machine");
            buttonFollow    = CreateToolbarButton("Follow", "Follow");
            buttonHelp      = CreateToolbarButton("Help", "Help");

            buttonNew.Click              += ButtonNew_Click;
            buttonOpen.Click             += ButtonOpen_Click;
            buttonOpen.MouseUpEvent      += ButtonOpen_MouseUpEvent;
            buttonSave.Click             += ButtonSave_Click;
            buttonSave.MouseUpEvent      += ButtonSave_MouseUpEvent;
            buttonExport.Click           += ButtonExport_Click;
            buttonExport.MouseUpEvent    += ButtonExport_MouseUpEvent;
            buttonCopy.Click             += ButtonCopy_Click;
            buttonCopy.EnabledEvent      += ButtonCopy_EnabledEvent;
            buttonCut.Click              += ButtonCut_Click;
            buttonCut.EnabledEvent       += ButtonCut_EnabledEvent;
            buttonPaste.Click            += ButtonPaste_Click;
            buttonPaste.MouseUpEvent     += ButtonPaste_MouseUpEvent;
            buttonPaste.EnabledEvent     += ButtonPaste_EnabledEvent;
            buttonUndo.Click             += ButtonUndo_Click;
            buttonUndo.EnabledEvent      += ButtonUndo_EnabledEvent;
            buttonRedo.Click             += ButtonRedo_Click;
            buttonRedo.EnabledEvent      += ButtonRedo_EnabledEvent;
            buttonTransform.Click        += ButtonTransform_Click;
            buttonConfig.Click           += ButtonConfig_Click;
            buttonPlay.Click             += ButtonPlay_Click; // MATTT : VibrateOnLongPress = false, what was that?
            buttonPlay.MouseUpEvent      += ButtonPlay_MouseUp;
            buttonPlay.ImageEvent        += ButtonPlay_ImageEvent;
            buttonRec.Click              += ButtonRec_Click;
            buttonRec.ImageEvent         += ButtonRec_ImageEvent;
            buttonRewind.Click           += ButtonRewind_Click;
            buttonLoop.Click             += ButtonLoop_Click; // MATTT : CloseOnClick = false, what was that?
            buttonLoop.ImageEvent        += ButtonLoop_ImageEvent;
            buttonQwerty.Click           += ButtonQwerty_Click;
            buttonQwerty.EnabledEvent    += ButtonQwerty_EnabledEvent;
            buttonMetronome.Click        += ButtonMetronome_Click; // MATTT : CloseOnClick = false, what was that?
            buttonMetronome.EnabledEvent += ButtonMetronome_EnabledEvent;
            buttonMachine.Click          += ButtonMachine_Click; // MATTT : CloseOnClick = false, what was that?
            buttonMachine.ImageEvent     += ButtonMachine_ImageEvent;
            buttonMachine.EnabledEvent   += ButtonMachine_EnabledEvent;
            buttonFollow.Click           += ButtonFollow_Click; // MATTT : CloseOnClick = false, what was that?
            buttonFollow.EnabledEvent    += ButtonFollow_EnabledEvent;
            buttonHelp.Click             += ButtonHelp_Click;

            if (Platform.IsMobile)
            {
                buttonDelete = CreateToolbarButton("Delete", "Delete");
                buttonMore   = CreateToolbarButton("More", "More");
                buttonPiano  = CreateToolbarButton("Piano", "Piano");
                buttonQwerty.Visible = false;
            }
            else
            {
                UpdateTooltips();
            }

            oscilloscope = new Oscilloscope();
            timecode     = new Timecode();
            tooltipLabel = new TooltipLabel();

            AddControl(oscilloscope);
            AddControl(timecode);
            AddControl(tooltipLabel);

            UpdateButtonLayout();

            /*
            x buttons[(int)ButtonType.New]       = new Button { BmpAtlasIndex = ButtonImageIndices.File, Click = OnNew };
            x buttons[(int)ButtonType.Open]      = new Button { BmpAtlasIndex = ButtonImageIndices.Open, Click = OnOpen, RightClick = Platform.IsDesktop ? OnOpenRecent : (MouseClickDelegate)null };
            x buttons[(int)ButtonType.Save]      = new Button { BmpAtlasIndex = ButtonImageIndices.Save, Click = OnSave, RightClick = OnSaveAs };
            x buttons[(int)ButtonType.Export]    = new Button { BmpAtlasIndex = ButtonImageIndices.Export, Click = OnExport, RightClick = Platform.IsDesktop ? OnRepeatLastExport : (MouseClickDelegate)null };
            x buttons[(int)ButtonType.Copy]      = new Button { BmpAtlasIndex = ButtonImageIndices.Copy, Click = OnCopy, Enabled = OnCopyEnabled };
            x buttons[(int)ButtonType.Cut]       = new Button { BmpAtlasIndex = ButtonImageIndices.Cut, Click = OnCut, Enabled = OnCutEnabled };
            x buttons[(int)ButtonType.Paste]     = new Button { BmpAtlasIndex = ButtonImageIndices.Paste, Click = OnPaste, RightClick = OnPasteSpecial, Enabled = OnPasteEnabled };
            x buttons[(int)ButtonType.Undo]      = new Button { BmpAtlasIndex = ButtonImageIndices.Undo, Click = OnUndo, Enabled = OnUndoEnabled };
            x buttons[(int)ButtonType.Redo]      = new Button { BmpAtlasIndex = ButtonImageIndices.Redo, Click = OnRedo, Enabled = OnRedoEnabled };
            x buttons[(int)ButtonType.Transform] = new Button { BmpAtlasIndex = ButtonImageIndices.Transform, Click = OnTransform };
            x buttons[(int)ButtonType.Config]    = new Button { BmpAtlasIndex = ButtonImageIndices.Config, Click = OnConfig };
            / buttons[(int)ButtonType.Play]      = new Button { Click = OnPlay, RightClick = OnPlayWithRate, GetBitmap = OnPlayGetBitmap, VibrateOnLongPress = false };
            / buttons[(int)ButtonType.Rec]       = new Button { GetBitmap = OnRecordGetBitmap, Click = OnRecord };
            x buttons[(int)ButtonType.Rewind]    = new Button { BmpAtlasIndex = ButtonImageIndices.Rewind, Click = OnRewind };
            x buttons[(int)ButtonType.Loop]      = new Button { Click = OnLoop, GetBitmap = OnLoopGetBitmap, CloseOnClick = false };
            x buttons[(int)ButtonType.Metronome] = new Button { BmpAtlasIndex = ButtonImageIndices.Metronome, Click = OnMetronome, Enabled = OnMetronomeEnabled, CloseOnClick = false };
            x buttons[(int)ButtonType.Machine]   = new Button { Click = OnMachine, GetBitmap = OnMachineGetBitmap, Enabled = OnMachineEnabled, CloseOnClick = false };
            x buttons[(int)ButtonType.Follow]    = new Button { BmpAtlasIndex = ButtonImageIndices.Follow, Click = OnFollow, Enabled = OnFollowEnabled, CloseOnClick = false };
            x buttons[(int)ButtonType.Help]      = new Button { BmpAtlasIndex = ButtonImageIndices.Help, Click = OnHelp };

            if (Platform.IsMobile)
            {
                buttons[(int)ButtonType.Delete] = new Button { BmpAtlasIndex = ButtonImageIndices.Delete, Click = OnDelete, RightClick = OnDeleteSpecial, Enabled = OnDeleteEnabled };
                buttons[(int)ButtonType.More]   = new Button { BmpAtlasIndex = ButtonImageIndices.More, Click = OnMore };
                buttons[(int)ButtonType.Piano]  = new Button { BmpAtlasIndex = ButtonImageIndices.Piano, Click = OnMobilePiano, Enabled = OnMobilePianoEnabled };
            }
            else
            {
                buttons[(int)ButtonType.Qwerty] = new Button { BmpAtlasIndex = ButtonImageIndices.QwertyPiano, Click = OnQwerty, Enabled = OnQwertyEnabled };

            }
            */

        }

        private void ButtonNew_Click(Control sender)
        {
            App.NewProject();
        }

        private void ButtonOpen_Click(Control sender)
        {
            App.OpenProject();
        }

        private void ButtonOpen_MouseUpEvent(Control sender, MouseEventArgs e)
        {
            if (Platform.IsDesktop && !e.Handled && e.Right && Settings.RecentFiles.Count > 0)
            {
                var options = new ContextMenuOption[Settings.RecentFiles.Count];

                for (int i = 0; i < Settings.RecentFiles.Count; i++)
                {
                    var j = i; // Important, copy for lambda below.
                    options[i] = new ContextMenuOption("MenuFile", Settings.RecentFiles[i], () => App.OpenProject(Settings.RecentFiles[j]));
                }

                App.ShowContextMenu(options);
            }
        }

        private void ButtonSave_Click(Control sender)
        {
            App.SaveProjectAsync();
        }

        private void ButtonSave_MouseUpEvent(Control sender, MouseEventArgs e)
        {
            if (!e.Handled && e.Right)
            {
                App.ShowContextMenu(new[]
                {
                    new ContextMenuOption("MenuSave", SaveAsLabel, $"{SaveAsTooltip} {Settings.FileSaveAsShortcut.TooltipString}", () => { App.SaveProjectAsync(true); }),
                });
            }
        }

        private void ButtonExport_Click(Control sender)
        {
            App.Export();
        }

        private void ButtonExport_MouseUpEvent(Control sender, MouseEventArgs e)
        {
            if (Platform.IsDesktop && !e.Handled && e.Right)
            {
                App.ShowContextMenu(new[]
                {
                    new ContextMenuOption("MenuExport", RepeatExportLabel, $"{RepeatExportTooltip} {Settings.FileExportRepeatShortcut.TooltipString}", () => { App.RepeatLastExport(); }),
                });
            }
        }

        private void ButtonCopy_Click(Control sender)
        {
            App.Copy();
        }

        private bool ButtonCopy_EnabledEvent(Control sender)
        {
            return App.CanCopy;
        }

        private void ButtonCut_Click(Control sender)
        {
            App.Cut();
        }

        private bool ButtonCut_EnabledEvent(Control sender)
        {
            return App.CanCopy;
        }

        private void ButtonPaste_Click(Control sender)
        {
            App.Paste();
        }

        private void ButtonPaste_MouseUpEvent(Control sender, MouseEventArgs e)
        {
            if (!e.Handled && e.Right)
            {
                App.ShowContextMenu(new[]
                {
                    new ContextMenuOption("MenuStar", PasteSpecialLabel, $"{PasteSpecialTooltip} {Settings.PasteSpecialShortcut.TooltipString}", () => { App.PasteSpecial(); }),
                });
            }
        }

        private bool ButtonPaste_EnabledEvent(Control sender)
        {
            return App.CanPaste;
        }

        private void ButtonUndo_Click(Control sender)
        {
            App.UndoRedoManager.Undo();
        }

        private bool ButtonUndo_EnabledEvent(Control sender)
        {
            return App.UndoRedoManager != null && App.UndoRedoManager.UndoScope != TransactionScope.Max;
        }

        private void ButtonRedo_Click(Control sender)
        {
            App.UndoRedoManager.Redo();
        }

        private bool ButtonRedo_EnabledEvent(Control sender)
        {
            return App.UndoRedoManager != null && App.UndoRedoManager.RedoScope != TransactionScope.Max;
        }

        private void ButtonTransform_Click(Control sender)
        {
            App.OpenTransformDialog();
        }

        private void ButtonConfig_Click(Control sender)
        {
            App.OpenConfigDialog();
        }

        private void ButtonPlay_Click(Control sender)
        {
            if (App.IsPlaying)
                App.StopSong();
            else
                App.PlaySong();
        }
        
        private void ButtonPlay_MouseUp(Control sender, MouseEventArgs e)
        {
            if (!e.Handled && e.Right)
            { 
                App.ShowContextMenu(new[]
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
        }

        private string ButtonPlay_ImageEvent(Control sender)
        {
            if (App.IsPlaying)
            {
                if (App.IsSeeking)
                {
                    // MATTT : How do we want to do this?
                    //tint = Theme.Darken(tint, (int)(Math.Abs(Math.Sin(Platform.TimeSeconds() * 12.0)) * 64));
                    return "Wait";
                }
                else
                {
                    return "Pause";
                }
            }
            else
            {
                switch (App.PlayRate)
                {
                    case 2:  return "PlayHalf";
                    case 4:  return "PlayQuarter";
                    default: return "Play";
                }
            }
        }

        private void ButtonRec_Click(Control sender)
        {
            App.ToggleRecording();
        }

        private string ButtonRec_ImageEvent(Control sender)
        {
            // MATTT : Tint!
            //    if (App.IsRecording)
            //        tint = Theme.DarkRedColor;
            return "Rec"; 
        }

        private void ButtonRewind_Click(Control sender)
        {
            App.StopSong();
            App.SeekSong(0);
        }

        private void ButtonLoop_Click(Control sender)
        {
            App.LoopMode = App.LoopMode == LoopMode.LoopPoint ? LoopMode.Pattern : LoopMode.LoopPoint;
        }

        private string ButtonLoop_ImageEvent(Control sender)
        {
            switch (App.LoopMode)
            {
                case LoopMode.Pattern:
                    return App.SequencerHasSelection ? "LoopSelection" : "LoopPattern";
                default:
                    return App.SelectedSong.LoopPoint < 0 ? "LoopNone" : "Loop";
            }
        }

        private void ButtonQwerty_Click(Control sender)
        {
            App.ToggleQwertyPiano();
        }

        private bool ButtonQwerty_EnabledEvent(Control sender)
        {
            return App.IsQwertyPianoEnabled;
        }

        private void ButtonMetronome_Click(Control sender)
        {
            App.ToggleMetronome();
        }

        private bool ButtonMetronome_EnabledEvent(Control sender)
        {
            return App.IsMetronomeEnabled;
        }

        private void ButtonMachine_Click(Control sender)
        {
            App.PalPlayback = !App.PalPlayback;
        }

        private bool ButtonMachine_EnabledEvent(Control sender)
        {
            return App.Project != null && !App.Project.UsesAnyExpansionAudio;
        }

        private string ButtonMachine_ImageEvent(Control sender)
        {
            if (App.Project == null)
            {
                return "NTSC";
            }
            else if (App.Project.UsesFamiTrackerTempo)
            {
                return App.PalPlayback ? "PAL" : "NTSC";
            }
            else
            {
                if (App.Project.PalMode)
                    return App.PalPlayback ? "PAL" : "PALToNTSC";
                else
                    return App.PalPlayback ? "NTSCToPAL" : "NTSC";
            }
        }

        private void ButtonFollow_Click(Control sender)
        {
            App.FollowModeEnabled = !App.FollowModeEnabled;
        }

        private bool ButtonFollow_EnabledEvent(Control sender)
        {
            return App.FollowModeEnabled;
        }

        private void ButtonHelp_Click(Control sender)
        {
            App.ShowHelp();
        }

        private void Settings_KeyboardShortcutsChanged()
        {
            UpdateTooltips();
        }

        public override void ContainerMouseMoveNotify(Control control, MouseEventArgs e)
        {
            var winPos = control.ControlToWindow(e.Position);
            var ctrl = GetControlAt(winPos.X, winPos.Y, out _, out _);

            tooltipLabel.ToolTip = ctrl != null && ctrl is Button ? ctrl.ToolTip : "";
        }

        private void UpdateTooltips()
        {
            if (Platform.IsDesktop)
            {
                buttonNew.ToolTip       = $"<MouseLeft> {NewProjectTooltip} {Settings.FileNewShortcut.TooltipString}";
                buttonOpen.ToolTip      = $"<MouseLeft> {OpenProjectTooltip} {Settings.FileOpenShortcut.TooltipString}\n<MouseRight> {RecentFilesTooltip}";
                buttonSave.ToolTip      = $"<MouseLeft> {SaveProjectTooltip} {Settings.FileSaveShortcut.TooltipString}\n<MouseRight> {MoreOptionsTooltip}";
                buttonExport.ToolTip    = $"<MouseLeft> {ExportTooltip} {Settings.FileExportShortcut.TooltipString}\n<MouseRight> {MoreOptionsTooltip}";
                buttonCopy.ToolTip      = $"<MouseLeft> {CopySelectionTooltip} {Settings.CopyShortcut.TooltipString}";
                buttonCut.ToolTip       = $"<MouseLeft> {CutSelectionTooltip} {Settings.CutShortcut.TooltipString}";
                buttonPaste.ToolTip     = $"<MouseLeft> {PasteTooltip} {Settings.PasteShortcut.TooltipString}\n<MouseRight> {MoreOptionsTooltip}";
                buttonUndo.ToolTip      = $"<MouseLeft> {UndoTooltip} {Settings.UndoShortcut.TooltipString}";
                buttonRedo.ToolTip      = $"<MouseLeft> {RedoTooltip} {Settings.RedoShortcut.TooltipString}";
                buttonTransform.ToolTip = $"<MouseLeft> {CleanupTooltip}";
                buttonConfig.ToolTip    = $"<MouseLeft> {SettingsTooltip}";
                buttonPlay.ToolTip      = $"<MouseLeft> {PlayPauseTooltip} {Settings.PlayShortcut.TooltipString} - <MouseRight> {MoreOptionsTooltip}";
                buttonRewind.ToolTip    = $"<MouseLeft> {RewindTooltip} {Settings.SeekStartShortcut.TooltipString}\n{RewindPatternTooltip} {Settings.SeekStartPatternShortcut.TooltipString}";
                buttonRec.ToolTip       = $"<MouseLeft> {ToggleRecordingTooltip} {Settings.RecordingShortcut.TooltipString}\n{AbortRecordingTooltip} <Esc>";
                buttonLoop.ToolTip      = $"<MouseLeft> {ToggleLoopModeTooltip}";
                buttonQwerty.ToolTip    = $"<MouseLeft> {ToggleQWERTYTooltip} {Settings.QwertyShortcut.TooltipString}";
                buttonMetronome.ToolTip = $"<MouseLeft> {ToggleMetronomeTooltip}";
                buttonMachine.ToolTip   = $"<MouseLeft> {TogglePALTooltip}";
                buttonFollow.ToolTip    = $"<MouseLeft> {ToggleFollowModeTooltip} {Settings.FollowModeShortcut.TooltipString}";
                buttonHelp.ToolTip      = $"<MouseLeft> {DocumentationTooltip}";
            }
        }

        private void UpdateButtonLayout()
        {
            if (ParentContainer == null)
                return;

            if (Platform.IsDesktop)
            {
                var margin = DpiScaling.ScaleForWindow(4);

                // Hide a few buttons if the window is too small (out min "usable" resolution is ~1280x720).
                var hideLessImportantButtons = Width < 1420 * DpiScaling.Window;
                var hideOscilloscope         = Width < 1250 * DpiScaling.Window;

                var x = 0;

                foreach (var btn in allButtons)
                {
                    if ((string)btn.UserData == "Help")
                    {
                        btn.Move(Width - btn.Width, 0);
                    }
                    else
                    {
                        btn.Move(x, 0, btn.Width, Height);
                    }

                    var isLessImportant =
                        (string)btn.UserData == "Copy"   ||
                        (string)btn.UserData == "Cut"    ||
                        (string)btn.UserData == "Paste"  ||
                        (string)btn.UserData == "Delete" ||
                        (string)btn.UserData == "Undo"   ||
                        (string)btn.UserData == "Redo";

                    btn.Visible = !(hideLessImportantButtons && isLessImportant);

                    if (btn.Visible)
                    {
                        x += btn.Width;
                    }

                    if ((string)btn.UserData == "Config")
                    {
                        var timecodeOscSizeX  = DpiScaling.ScaleForWindow(140);

                        oscilloscope.Visible = !hideOscilloscope;
                        
                        if (oscilloscope.Visible)
                        {
                            x += margin;
                            oscilloscope.Move(x, margin, timecodeOscSizeX, Height - margin * 2);
                            x += timecodeOscSizeX + margin;
                        }

                        x += margin;
                        timecode.Move(x, margin, timecodeOscSizeX, Height - margin * 2);
                        x += timecodeOscSizeX + margin;
                    }
                }

                x += margin;
                tooltipLabel.Move(x, 0, buttonHelp.Left - x - margin, Height);
            }
            /* MATTT : Mobile!
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
            }
            */

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

        public void SetToolTip(string msg)
        {
            tooltipLabel.ToolTip = msg;
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
            // MATTT
            //tooltip = "";
            //redTooltip = false;
        }

        //private void OnDelete(int x, int y)
        //{
        //    App.Delete();
        //}

        //private void OnDeleteSpecial(int x, int y)
        //{
        //    App.ShowContextMenu(left + x, top + y, new[]
        //    {
        //        new ContextMenuOption("MenuStar", DeleteSpecialLabel, () => { App.DeleteSpecial(); }),
        //    });
        //}

        //private ButtonStatus OnDeleteEnabled()
        //{
        //    return App.CanDelete ? ButtonStatus.Enabled : ButtonStatus.Disabled;
        //}

        //private void StartClosing()
        //{
        //    expanding = false;
        //    closing   = expandRatio > 0.0f;
        //}

        //private void OnMore(int x, int y)
        //{
        //    if (expanding || closing)
        //    {
        //        expanding = !expanding;
        //        closing   = !closing;
        //    }
        //    else
        //    {
        //        expanding = expandRatio == 0.0f;
        //        closing   = expandRatio == 1.0f;
        //    }

        //    MarkDirty();
        //}

        //private void OnMobilePiano(int x, int y)
        //{
        //    App.MobilePianoVisible = !App.MobilePianoVisible;
        //}

        //private ButtonStatus OnMobilePianoEnabled()
        //{
        //    return App.MobilePianoVisible ? ButtonStatus.Enabled : ButtonStatus.Dimmed;
        //}

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
            base.OnRender(g);
            /*
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
            //RenderButtons(c);

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
            */
        }

        public bool ShouldRefreshOscilloscope(bool hasNonZeroSample)
        {
            return oscilloscope.Visible && oscilloscope.LastOscilloscopeHadNonZeroSample != hasNonZeroSample;
        }

        //protected override void OnMouseMove(MouseEventArgs e)
        //{
        //    var newHoverButtonIdx = -1;
        //    var newTooltip = "";

        //    for (int i = 0; i < buttons.Length; i++)
        //    {
        //        var btn = buttons[i];

        //        if (btn != null && btn.Visible && btn.Rect.Contains(e.X, e.Y))
        //        {
        //            newHoverButtonIdx = i;
        //            newTooltip = btn.ToolTip;
        //            break;
        //        }
        //    }

        //    SetAndMarkDirty(ref hoverButtonIdx, newHoverButtonIdx);
        //    SetToolTip(newTooltip);
        //}

        //protected override void OnMouseDown(MouseEventArgs e)
        //{
        //    bool left  = e.Left;

        //    if (left)
        //    {
        //        if (Platform.IsMobile && !ClientRectangle.Contains(e.X, e.Y))
        //        {
        //            StartClosing();
        //        }
        //        else if (IsPointInTimeCode(e.X, e.Y))
        //        {
        //            Settings.TimeFormat = Settings.TimeFormat == 0 ? 1 : 0;
        //            MarkDirty();
        //        }
        //        else
        //        {
        //            var btn = GetButtonAtCoord(e.X, e.Y);

        //            if (btn != null)
        //            {
        //                btn.Click?.Invoke(e.X, e.Y);
        //                MarkDirty();
        //            }
        //        }
        //    }
        //}

        //protected override void OnTouchLongPress(int x, int y)
        //{
        //    var btn = GetButtonAtCoord(x, y);

        //    if (btn != null && btn.RightClick != null)
        //    {
        //        if (btn.VibrateOnLongPress)
        //            Platform.VibrateClick();
        //        btn.RightClick(x, y);
        //        MarkDirty();
        //        if (btn.CloseOnClick && IsExpanded)
        //            StartClosing();
        //    }
        //}

        //protected override void OnTouchClick(int x, int y)
        //{
        //    var btn = GetButtonAtCoord(x, y);
        //    if (btn != null)
        //    {
        //        Platform.VibrateTick();
        //        btn.Click?.Invoke(x, y);
        //        MarkDirty();
        //        if (!btn.CloseOnClick)
        //            return;
        //    }

        //    if (IsPointInTimeCode(x, y))
        //    {
        //        Settings.TimeFormat = Settings.TimeFormat == 0 ? 1 : 0;
        //        Platform.VibrateTick();
        //        MarkDirty();
        //        return;
        //    }

        //    if (IsExpanded)
        //    {
        //        if (btn == null)
        //            Platform.VibrateTick();
        //        StartClosing();
        //    }
        //}
    }
}
