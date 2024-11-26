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

        // Mobile-only layout.
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

        private static readonly Dictionary<string, (int, int)> MobileButtonLayout = new Dictionary<string, (int, int)>
        {
            { "Open",      (0, 0) },
            { "Copy",      (0, 1) },
            { "Cut",       (0, 2) },
            { "Undo",      (0, 3) },
            { "Play",      (0, 6) },
            { "Rec",       (0, 7) },
            { "Help",      (0, 8) },
            { "Save",      (1, 0) },
            { "Paste",     (1, 1) },
            { "Delete",    (1, 2) },
            { "Redo",      (1, 3) },
            { "Rewind",    (1, 6) },
            { "Piano",     (1, 7) },
            { "More",      (1, 8) },
            { "New",       (2, 0) },
            { "Export",    (2, 1) },
            { "Config",    (2, 2) },
            { "Transform", (2, 3) },
            { "Machine",   (2, 4) },
            { "Follow",    (2, 5) },
            { "Loop",      (2, 6) },
            { "Metronome", (2, 7) }
        };

        // [portrait/landscape, timecode/oscilloscope]
        private static readonly MobileOscTimeLayoutItem[,] OscTimeLayout = new MobileOscTimeLayoutItem[,]
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

        const float ShowExtraButtonsThreshold = 0.8f;

        private int buttonSize;
        private float iconScaleFloat = 1.0f;

        // Mobile-only stuff
        private float expandRatio = 0.0f;
        private bool  expanding = false; 
        private bool  closing   = false; 
        private bool  ticking   = false;

        public int   LayoutSize  => buttonSize * 2;
        public int   RenderSize  => (int)Math.Round(LayoutSize * (1.0f + Utils.SmootherStep(expandRatio) * 0.5f));
        public float ExpandRatio => expandRatio;
        public bool  IsExpanded  => Platform.IsMobile && expandRatio > 0.0f;

        public override bool WantsFullScreenViewport => Platform.IsMobile;

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

        // Help dialog
        private LocalizedString ShowHelpTitle;
        private LocalizedString ShowHelpLabel;

        #endregion

        public Toolbar()
        {
            Localization.Localize(this);
            Settings.KeyboardShortcutsChanged += Settings_KeyboardShortcutsChanged;
            SetTickEnabled(Platform.IsMobile);
            clipRegion = Platform.IsDesktop;
            supportsDoubleClick = false;
        }

        private Button CreateToolbarButton(string image, string userData)
        {
            var button = new Button(image);
            button.UserData = userData;
            button.Visible = false;
            button.ImageScale = iconScaleFloat;
            button.Transparent = true;
            button.VibrateOnClick = true;
            button.VibrateOnRightClick = true;
            button.Resize(buttonSize, buttonSize);
            allButtons.Add(button);
            AddControl(button);
            return button;
        }

        protected override void OnAddedToContainer()
        {
            var g = ParentWindow.Graphics;
            
            if (Platform.IsMobile)
            {
                // On mobile, everything will scale from 1080p.
                var screenSize = Platform.GetScreenResolution();
                var scale = Math.Min(screenSize.Width, screenSize.Height) / 1080.0f;

                buttonSize = DpiScaling.ScaleCustom(120, scale);
                iconScaleFloat = 1.5f * scale;
            }
            else
            {
                buttonSize = DpiScaling.ScaleForWindow(36);
            }

            buttonNew = CreateToolbarButton("File", "New");
            buttonNew.Click += ButtonNew_Click;

            buttonOpen = CreateToolbarButton("Open", "Open");
            buttonOpen.Click += ButtonOpen_Click;
            buttonOpen.PointerUp += ButtonOpen_PointerUpEvent;

            buttonSave = CreateToolbarButton("Save", "Save");
            buttonSave.Click += ButtonSave_Click;
            buttonSave.PointerUp += ButtonSave_PointerUpEvent;

            buttonExport = CreateToolbarButton("Export", "Export");
            buttonExport.Click += ButtonExport_Click;
            buttonExport.PointerUp += ButtonExport_PointerUpEvent;

            buttonCopy = CreateToolbarButton("Copy", "Copy");
            buttonCopy.Click += ButtonCopy_Click;
            buttonCopy.EnabledEvent += ButtonCopy_EnabledEvent;

            buttonCut = CreateToolbarButton("Cut", "Cut");
            buttonCut.Click += ButtonCut_Click;
            buttonCut.EnabledEvent += ButtonCut_EnabledEvent;

            buttonPaste = CreateToolbarButton("Paste", "Paste");
            buttonPaste.Click += ButtonPaste_Click;
            buttonPaste.PointerUp += ButtonPaste_PointerUpEvent;
            buttonPaste.EnabledEvent += ButtonPaste_EnabledEvent;

            buttonUndo = CreateToolbarButton("Undo", "Undo");
            buttonUndo.Click += ButtonUndo_Click;
            buttonUndo.EnabledEvent += ButtonUndo_EnabledEvent;

            buttonRedo = CreateToolbarButton("Redo", "Redo");
            buttonRedo.Click += ButtonRedo_Click;
            buttonRedo.EnabledEvent += ButtonRedo_EnabledEvent;

            buttonTransform = CreateToolbarButton("Transform", "Transform");
            buttonTransform.Click += ButtonTransform_Click;

            buttonConfig = CreateToolbarButton("Config", "Config");
            buttonConfig.Click += ButtonConfig_Click;

            buttonPlay = CreateToolbarButton("Play", "Play");
            buttonPlay.Click += ButtonPlay_Click;
            buttonPlay.PointerUp += ButtonPlay_PointerUp;
            buttonPlay.ImageEvent += ButtonPlay_ImageEvent;

            buttonRec = CreateToolbarButton("Rec", "Rec");
            buttonRec.Click += ButtonRec_Click;
            buttonRec.ImageEvent += ButtonRec_ImageEvent;

            buttonRewind = CreateToolbarButton("Rewind", "Rewind");
            buttonRewind.Click += ButtonRewind_Click;

            buttonLoop = CreateToolbarButton("Loop", "Loop");
            buttonLoop.Click += ButtonLoop_Click;
            buttonLoop.ImageEvent += ButtonLoop_ImageEvent;

            buttonQwerty = CreateToolbarButton("QwertyPiano", "Qwerty");
            buttonQwerty.Visible = Platform.IsDesktop;
            buttonQwerty.Click += ButtonQwerty_Click;
            buttonQwerty.DimmedEvent += ButtonQwerty_DimmedEvent;

            buttonMetronome = CreateToolbarButton("Metronome", "Metronome");
            buttonMetronome.Click += ButtonMetronome_Click;
            buttonMetronome.DimmedEvent += ButtonMetronome_DimmedEvent;

            buttonMachine = CreateToolbarButton("NTSC", "Machine");
            buttonMachine.Click += ButtonMachine_Click;
            buttonMachine.ImageEvent += ButtonMachine_ImageEvent;
            buttonMachine.EnabledEvent += ButtonMachine_EnabledEvent;

            buttonFollow = CreateToolbarButton("Follow", "Follow");
            buttonFollow.Click += ButtonFollow_Click;
            buttonFollow.DimmedEvent += ButtonFollow_DimmedEvent;

            buttonHelp = CreateToolbarButton("Help", "Help");
            buttonHelp.Click += ButtonHelp_Click;

            if (Platform.IsMobile)
            {
                buttonDelete = CreateToolbarButton("Delete", "Delete");
                buttonDelete.Click += ButtonDelete_Click;
                buttonDelete.RightClick += ButtonDelete_RightClick;
                buttonDelete.EnabledEvent += ButtonDelete_EnabledEvent;

                buttonMore = CreateToolbarButton("More", "More");
                buttonMore.Click += ButtonMore_Click;

                buttonPiano = CreateToolbarButton("Piano", "Piano");
                buttonPiano.Click += ButtonPiano_Click;
                buttonPiano.DimmedEvent += ButtonPiano_DimmedEvent;
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
            if (Platform.IsDesktop)
                AddControl(tooltipLabel);

            UpdateButtonLayout();
        }


        private void ButtonDelete_Click(Control sender)
        {
            App.Delete();
        }
        
        private void ButtonDelete_RightClick(Control sender)
        {
            App.ShowContextMenuAsync(new[]
            {
                new ContextMenuOption("MenuStar", DeleteSpecialLabel, () => { App.DeleteSpecial(); }),
            });
        }

        private bool ButtonDelete_EnabledEvent(Control sender)
        {
            return App.CanDelete;
        }

        private void ButtonMore_Click(Control sender)
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

        private void ButtonPiano_Click(Control sender)
        {
            App.MobilePianoVisible = !App.MobilePianoVisible;
        }

        private bool ButtonPiano_DimmedEvent(Control sender, ref int dimming)
        {
            return !App.MobilePianoVisible;
        }

        private void ButtonNew_Click(Control sender)
        {
            App.NewProject();
            StartClosing();
        }

        private void ButtonOpen_Click(Control sender)
        {
            App.OpenProject();
            StartClosing();
        }

        private void ButtonOpen_PointerUpEvent(Control sender, PointerEventArgs e)
        {
            if (Platform.IsDesktop && !e.Handled && e.Right && Settings.RecentFiles.Count > 0)
            {
                var options = new ContextMenuOption[Settings.RecentFiles.Count];

                for (int i = 0; i < Settings.RecentFiles.Count; i++)
                {
                    var j = i; // Important, copy for lambda below.
                    options[i] = new ContextMenuOption("MenuFile", Settings.RecentFiles[i], () => App.OpenProject(Settings.RecentFiles[j]));
                }

                App.ShowContextMenuAsync(options);
            }
        }

        private void ButtonSave_Click(Control sender)
        {
            App.SaveProjectAsync();
            StartClosing();
        }

        private void ButtonSave_PointerUpEvent(Control sender, PointerEventArgs e)
        {
            if (!e.Handled && e.Right)
            {
                App.ShowContextMenuAsync(new[]
                {
                    new ContextMenuOption("MenuSave", SaveAsLabel, $"{SaveAsTooltip} {Settings.FileSaveAsShortcut.TooltipString}", () => { App.SaveProjectAsync(true); }),
                });
            }
        }

        private void ButtonExport_Click(Control sender)
        {
            App.Export();
            StartClosing();
        }

        private void ButtonExport_PointerUpEvent(Control sender, PointerEventArgs e)
        {
            if (Platform.IsDesktop && !e.Handled && e.Right)
            {
                App.ShowContextMenuAsync(new[]
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

        private void ButtonPaste_PointerUpEvent(Control sender, PointerEventArgs e)
        {
            if (!e.Handled && e.Right)
            {
                App.ShowContextMenuAsync(new[]
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
            StartClosing();
        }

        private void ButtonConfig_Click(Control sender)
        {
            App.OpenConfigDialog();
            StartClosing();
        }

        private void ButtonPlay_Click(Control sender)
        {
            if (App.IsPlaying)
                App.StopSong();
            else
                App.PlaySong();
        }
        
        private void ButtonPlay_PointerUp(Control sender, PointerEventArgs e)
        {
            if (!e.Handled && e.Right)
            { 
                App.ShowContextMenuAsync(new[]
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

        private string ButtonPlay_ImageEvent(Control sender, ref Color tint)
        {
            if (App.IsPlaying)
            {
                if (App.IsSeeking)
                {
                    tint = Theme.Darken(tint, (int)(Math.Abs(Math.Sin(Platform.TimeSeconds() * 12.0)) * 64));
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

        private string ButtonRec_ImageEvent(Control sender, ref Color tint)
        {
            if (App.IsRecording)
                tint = Theme.DarkRedColor;
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

        private string ButtonLoop_ImageEvent(Control sender, ref Color tint)
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

        private bool ButtonQwerty_DimmedEvent(Control sender, ref int dimming)
        {
            return !App.IsQwertyPianoEnabled;
        }

        private void ButtonMetronome_Click(Control sender)
        {
            App.ToggleMetronome();
        }

        private bool ButtonMetronome_DimmedEvent(Control sender, ref int dimming)
        {
            return !App.IsMetronomeEnabled;
        }

        private void ButtonMachine_Click(Control sender)
        {
            App.PalPlayback = !App.PalPlayback;
        }

        private bool ButtonMachine_EnabledEvent(Control sender)
        {
            return App.Project != null;
        }

        private string ButtonMachine_ImageEvent(Control sender, ref Color tint)
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

        private bool ButtonFollow_DimmedEvent(Control sender, ref int dimming)
        {
            return !App.FollowModeEnabled;
        }

        private void ButtonHelp_Click(Control sender)
        {
            if (Platform.IsMobile)
            {
                Platform.MessageBoxAsync(window, ShowHelpLabel, ShowHelpTitle, MessageBoxButtons.YesNo, (r) =>
                {
                    if (r == DialogResult.Yes)
                    {
                        App.ShowHelp();
                    }
                });
            }
            else
            {
                App.ShowHelp();
            }
        }

        private void Settings_KeyboardShortcutsChanged()
        {
            UpdateTooltips();
        }

        public override void OnContainerPointerMoveNotify(Control control, PointerEventArgs e)
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
                var lastVisibleButton = (Button)null;

                foreach (var btn in allButtons)
                {
                    if (btn == buttonHelp)
                    {
                        btn.Move(Width - btn.Width, 0);
                    }
                    else
                    {
                        btn.Move(x, 0, btn.Width, Height);
                    }

                    var isLessImportant = btn == buttonCopy   ||
                                          btn == buttonCut    ||
                                          btn == buttonPaste  ||
                                          btn == buttonDelete ||
                                          btn == buttonUndo   ||
                                          btn == buttonRedo;

                    btn.Visible = !(hideLessImportantButtons && isLessImportant);

                    if (btn.Visible)
                    {
                        x += btn.Width;

                        if (btn != buttonHelp)
                        {
                            lastVisibleButton = btn;
                        }
                    }

                    if (btn == buttonConfig)
                    {
                        var timecodeOscSizeX  = DpiScaling.ScaleForWindow(140);

                        oscilloscope.Visible = !hideOscilloscope;
                        
                        if (oscilloscope.Visible)
                        {
                            x += margin;
                            timecode.Move(x, margin, timecodeOscSizeX, Height - margin * 2);
                            x += timecodeOscSizeX + margin;
                        }

                        x += margin;
                        oscilloscope.Move(x, margin, timecodeOscSizeX, Height - margin * 2);
                        x += timecodeOscSizeX + margin;
                    }
                }

                tooltipLabel.Move(lastVisibleButton.Right + margin, 0, buttonHelp.Left - lastVisibleButton.Right - margin * 2, Height);
            }
            else
            {
                var landscape = IsLandscape;

                foreach (var btn in allButtons)
                {
                    btn.Visible = false;
                }

                var numRows = expandRatio >= ShowExtraButtonsThreshold ? 3 : 2;

                foreach (var btn in allButtons)
                {
                    if (!MobileButtonLayout.TryGetValue((string)btn.UserData, out var layout))
                        continue;

                    var row = layout.Item1;
                    var col = layout.Item2;

                    if (row >= numRows)
                        continue;

                    if (landscape)
                        Utils.Swap(ref col, ref row);

                    btn.Move(buttonSize * col, buttonSize * row);
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
                    Utils.Swap(ref oscCol,  ref oscRow);
                }

                var margin = buttonSize / 10;
                var timecodeOscSizeX = timeLayout.numCols * buttonSize - margin * 2;
                var timecodeOscSizeY = buttonSize - margin * 2;

                timecode.Move(timeCol * buttonSize + margin, timeRow * buttonSize + margin, timecodeOscSizeX, timecodeOscSizeY);
                oscilloscope.Move(oscCol * buttonSize + margin, oscRow * buttonSize + margin, timecodeOscSizeX, timecodeOscSizeY);
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
            tooltipLabel.ToolTip = "";
        }

        private void StartClosing()
        {
            expanding = false;
            closing = expandRatio > 0.0f;
        }

        //private void OnMore(int x, int y)
        //{

        //}

        //private void OnMobilePiano(int x, int y)
        //{
        //    App.MobilePianoVisible = !App.MobilePianoVisible;
        //}

        //private ButtonStatus OnMobilePianoEnabled()
        //{
        //    return App.MobilePianoVisible ? ButtonStatus.Enabled : ButtonStatus.Dimmed;
        //}

        private Rectangle GetMobileShadowRect()
        {
            if (IsExpanded)
            {
                if (IsLandscape)
                    return new Rectangle(RenderSize, 0, ParentWindowSize.Width - RenderSize, ParentWindowSize.Height);
                else
                    return new Rectangle(0, RenderSize, ParentWindowSize.Width, ParentWindowSize.Height - RenderSize);
            }
            else
            {
                return Rectangle.Empty;
            }
        }

        private void RenderShadow(CommandList c)
        {
            if (Platform.IsMobile && IsExpanded)
            {
                c.FillRectangle(GetMobileShadowRect(), Color.FromArgb((int)Utils.Clamp(expandRatio * 0.6f * 255.0f, 0, 255), Color.Black));
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

            base.OnRender(g);

            if (Platform.IsMobile)
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
            return oscilloscope.Visible && oscilloscope.LastOscilloscopeHadNonZeroSample != hasNonZeroSample;
        }

        protected override void OnPointerUp(PointerEventArgs e)
        {
            if (!e.Handled && Platform.IsMobile && IsExpanded && GetMobileShadowRect().Contains(e.Position))
            {
                Platform.VibrateTick();
                StartClosing();
            }
        }
    }
}
