using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;

namespace FamiStudio
{
    public partial class ProjectExplorer : Container
    {
        private const int   DefaultExpandSizeX      = 12;
        private const int   DefaultIconSizeX        = 16;
        private const int   DefaultMarginX          = Platform.IsMobile ? 1 : 2;
        private const int   DefaultSpacingX         = Platform.IsMobile ? 0 : 2;
        private const int   DefaultPanelSizeY       = 21;
        private const int   DefaultDraggedLineSizeY = 5;
        private const int   DefaultSliderSizeX      = Platform.IsMobile ? 84 : 104; // MATTT : Review.
        private const float ScrollSpeedFactor       = Platform.IsMobile ? 2.0f : 1.0f;

        private int expandSizeX;
        private int spacingX;
        private int marginX;
        private int iconSizeX;
        private int panelSizeY;
        private int sliderSizeX;
        private int virtualSizeY; // MATTT : Move this functionality to container directly.

        private enum TabType
        {
            Project,
            Registers,
            Count
        };

        #region Localization

        LocalizedString[] TabNames = new LocalizedString[(int)TabType.Count];

        // Buttons
        LocalizedString SongsHeaderLabel;
        LocalizedString InstrumentHeaderLabel;
        LocalizedString SamplesHeaderLabel;
        LocalizedString ArpeggiosHeaderLabel;
        LocalizedString ArpeggioNoneLabel;
        LocalizedString RegistersExpansionHeaderLabel;

        // Tooltips
        LocalizedString AddNewArpeggioTooltip;
        LocalizedString AddNewInstrumentTooltip;
        LocalizedString AddNewSongTooltip;
        LocalizedString AutoSortArpeggiosTooltip;
        LocalizedString AutoSortInstrumentsTooltip;
        LocalizedString AutoSortSamplesTooltip;
        LocalizedString AutoSortSongsTooltip;
        LocalizedString ChangeValueFineTooltip;
        LocalizedString ChangeValueTooltip;
        LocalizedString CopyEnvelopeTooltip;
        LocalizedString CopyReplaceInstrumentTooltip;
        LocalizedString EditEnvelopeTooltip;
        LocalizedString EditSamplesTooltip;
        LocalizedString EditWaveformTooltip;
        LocalizedString ImportInstrumentsTooltip;
        LocalizedString ImportSamplesTooltip;
        LocalizedString ImportSongsTooltip;
        LocalizedString MakeSongCurrentTooltip;
        LocalizedString MoreOptionsTooltip;
        LocalizedString PlaySourceSampleTooltip;
        LocalizedString PreviewProcessedSampleTooltip;
        LocalizedString PropertiesArpeggioTooltip;
        LocalizedString PropertiesInstrumentTooltip;
        LocalizedString PropertiesProjectTooltip;
        LocalizedString PropertiesSongTooltip;
        LocalizedString PropertiesFolderTooltip;
        LocalizedString ReloadSourceDataTooltip;
        LocalizedString ReorderSongsTooltip;
        LocalizedString ReplaceArpeggioTooltip;
        LocalizedString SelectArpeggioTooltip;
        LocalizedString SelectInstrumentTooltip;
        LocalizedString ToggleValueTooltip;
        LocalizedString AllowProjectMixerSettings;
        LocalizedString ExpandTooltip;

        // Messages
        LocalizedString CopyArpeggioMessage;
        LocalizedString CopyArpeggioTitle;
        LocalizedString ErrorTitle;
        LocalizedString MaxWavFileWarning;
        LocalizedString MaxDmcSizeWarning;
        LocalizedString MaxWavN163Duration;
        LocalizedString AskDeleteSongMessage;
        LocalizedString AskDeleteSongTitle;
        LocalizedString AskDeleteInstrumentMessage;
        LocalizedString AskDeleteInstrumentTitle;
        LocalizedString AskDeleteArpeggioMessage;
        LocalizedString AskDeleteArpeggioTitle;
        LocalizedString AskDeleteSampleMessage;
        LocalizedString AskDeleteSampleTitle;
        LocalizedString AskDeleteFolderMessage;
        LocalizedString AskDeleteFolderTitle;
        LocalizedString AskReplaceInstrumentMessage;
        LocalizedString AskReplaceInstrumentTitle;
        LocalizedString AskReplaceArpeggioMessage;
        LocalizedString AskReplaceArpeggioTitle;
        LocalizedString ClipboardNoValidTextError;
        LocalizedString ClipboardInvalidNumberRegisters;
        LocalizedString CantFindSourceFileError;

        // Import songs dialog
        LocalizedString ImportSongsTitle;
        LocalizedString ImportSongsLabel;
        LocalizedString SelectAllLabel;
        LocalizedString SelectNoneLabel ;

        // Import instruments dialog
        LocalizedString ImportInstrumentsTitle;
        LocalizedString ImportInstrumentsLabel ;

        // Import DPCM Samples dialog
        LocalizedString ImportSamplesTitle;
        LocalizedString ImportSamplesLabel;

        // Auto-assign banks dialog
        LocalizedString AutoAssignBanksTitle;
        LocalizedString TargetBankSizeLabel;

        // Tempo conversion dialog
        LocalizedString ProjectChangeTempoModeTitle;
        LocalizedString ProjectConvertToFamiTrackerMessage;
        LocalizedString ProjectConvertToFamiStudioMessage;

        //  Expansion change messages
        LocalizedString ProjectExpansionRemovedMessage;
        LocalizedString ProjectChangedN163ChannelMessage;

        // Song properties dialog
        LocalizedString SongPropertiesTitle;
        LocalizedString SongLengthLabel;
        LocalizedString SongLengthTooltip;
        LocalizedString RenameSongError ;

        // Instrument properties dialog
        LocalizedString InstrumentPropertiesTitle;
        LocalizedString RenameInstrumentError;

        // Arpeggio properties dialog
        LocalizedString ArpeggioPropertiesTitle;
        LocalizedString RenameArpeggioError;

        // DPCM sample properties dialog
        LocalizedString SamplePropertiesTitle;
        LocalizedString RenameSampleError;

        // Folder properties dialog
        LocalizedString FolderPropertiesTitle;
        LocalizedString RenameFolderError;

        // Context menus
        LocalizedString AddSongContext;
        LocalizedString AddArpeggioContext;
        LocalizedString AddFolderContext;
        LocalizedString AddExpInstrumentContext;
        LocalizedString AddRegularInstrumentContext;
        LocalizedString AutoAssignBanksContext;
        LocalizedString ClearEnvelopeContext;
        LocalizedString CopyRegisterValueContext;
        LocalizedString DeleteArpeggioContext;
        LocalizedString DeleteInstrumentContext;
        LocalizedString DeleteSampleContext;
        LocalizedString DeleteSongContext;
        LocalizedString DeleteFolderContext;
        LocalizedString DiscardSourceWavDataContext;
        LocalizedString DiscardSourceWavDataTooltip;
        LocalizedString DiscardWavDataContext;
        LocalizedString DuplicateContext;
        LocalizedString DuplicateConvertContext;
        LocalizedString ExportProcessedDmcDataContext;
        LocalizedString ExportSourceDataContext;
        LocalizedString PasteRegisterValueContext;
        LocalizedString PropertiesArpeggioContext;
        LocalizedString PropertiesInstrumentContext;
        LocalizedString PropertiesProjectContext;
        LocalizedString PropertiesSamplesContext;
        LocalizedString PropertiesSongContext;
        LocalizedString ReplaceWithContext;
        LocalizedString ResampleWavContext;
        LocalizedString EnterValueContext;
        LocalizedString ResetDefaultValueContext;
        LocalizedString CollapseAllContext;
        LocalizedString ExpandAllContext;

        // Message boxes
        LocalizedString CopyInstrumentEnvelopeMessage;
        LocalizedString CopyInstrumentEnvelopeTitle;
        LocalizedString CopyInstrumentSamplesMessage;
        LocalizedString CopyInstrumentSamplesTitle;

        #endregion

        // From right to left. Looks more visually pleasing than the enum order.
        private static readonly int[] EnvelopeDisplayOrder =
        {
            EnvelopeType.Arpeggio,
            EnvelopeType.Pitch,
            EnvelopeType.Volume,
            EnvelopeType.DutyCycle,
            EnvelopeType.FdsModulation,
            EnvelopeType.FdsWaveform,
            EnvelopeType.N163Waveform,
            EnvelopeType.S5BMixer,
            EnvelopeType.S5BNoiseFreq
        };

        // MATTT : Remove
        private enum SubButtonType
        {
        }

        // MATTT : Remove
        private class ProjectExplorerButton
        {
        }

        // MATTT : Create a "CaptureOperation" class or struct.
        private enum CaptureOperation
        {
            None,
            DragInstrument,
            DragInstrumentEnvelope,
            DragInstrumentSampleMappings,
            DragArpeggio,
            DragArpeggioValues,
            DragSample,
            DragSong,
            DragFolder
        };

        private static readonly bool[] captureNeedsThreshold = new[]
        {
            false, // None
            true,  // DragInstrument
            true,  // DragInstrumentEnvelope
            true,  // DragInstrumentSampleMappings
            true,  // DragArpeggio
            true,  // DragArpeggioValues
            true,  // DragSample
            true,  // DragSong
            true,  // DragFolder
        };

        private static readonly bool[] captureWantsRealTimeUpdate = new[]
        {
            false, // None
            true,  // DragInstrument
            true,  // DragInstrumentEnvelope
            true,  // DragInstrumentSampleMappings
            true,  // DragArpeggio
            false, // DragArpeggioValues
            true,  // DragSample
            true,  // DragSong
            true,  // DragFolder
        };

        private int mouseLastX = 0;
        private int mouseLastY = 0;
        private int captureMouseX = -1;
        private int captureMouseY = -1;
        private int captureButtonRelX = -1;
        private int captureButtonRelY = -1;
        private int captureButtonIdx = -1;
        private int captureScrollY = -1;
        private int envelopeDragIdx = -1;
        private int highlightedButtonIdx = -1;
        private int captureButtonSign = 0;
        private float flingVelY = 0.0f;
        private float bitmapScale = 1.0f;
        private float captureDuration = 0.0f;
        private bool captureThresholdMet = false;
        private bool captureRealTimeUpdate = false;
        private bool canFling = false;
        private TabType selectedTab = TabType.Project;
        private ProjectExplorerButton sliderDragButton = null;
        private CaptureOperation captureOperation = CaptureOperation.None;
        private CaptureOperation lastCaptureOperation = CaptureOperation.None;
        private Instrument draggedInstrument = null;
        private Instrument expandedInstrument = null;
        private Folder draggedFolder = null;
        private string selectedInstrumentTab = null;
        private DPCMSample expandedSample = null;
        private Arpeggio draggedArpeggio = null;
        private DPCMSample draggedSample = null;
        private Song draggedSong = null;
        private List<ProjectExplorerButton> buttons = new List<ProjectExplorerButton>();

        // Global controls
        private GradientPanel tabPanel;
        private Container mainContainer;
        private ScrollBar scrollBar;
        private GradientPanel noneArpPanel;

        // Register viewer stuff
        private NesApu.NesRegisterValues registerValues;
        private RegisterViewer[] registerViewers = new RegisterViewer[ExpansionType.Count];

        public DPCMSample DraggedSample => captureOperation == CaptureOperation.DragSample ? draggedSample : null;
        public bool IsActiveControl => App != null && App.ActiveControl == this;

        public delegate void EmptyDelegate();
        public delegate void BoolDelegate(bool val);
        public delegate void InstrumentEnvelopeDelegate(Instrument instrument, int envelope);
        public delegate void InstrumentDelegate(Instrument instrument);
        public delegate void InstrumentPointDelegate(Instrument instrument, Point pos);
        public delegate void SongDelegate(Song song);
        public delegate void ArpeggioDelegate(Arpeggio arpeggio);
        public delegate void ArpeggioPointDelegate(Arpeggio arpeggio, Point pos);
        public delegate void DPCMSamplePointDelegate(DPCMSample instrument, Point pos);
        public delegate void DPCMSampleDelegate(DPCMSample sample);

        public event InstrumentDelegate InstrumentColorChanged;
        public event InstrumentDelegate InstrumentReplaced;
        public event InstrumentDelegate InstrumentDeleted;
        public event InstrumentPointDelegate InstrumentDroppedOutside;
        public event SongDelegate SongModified;
        public event ArpeggioDelegate ArpeggioColorChanged;
        public event ArpeggioDelegate ArpeggioDeleted;
        public event ArpeggioPointDelegate ArpeggioDroppedOutside;
        public event DPCMSampleDelegate DPCMSampleReloaded;
        public event DPCMSampleDelegate DPCMSampleColorChanged;
        public event DPCMSampleDelegate DPCMSampleDeleted;
        public event DPCMSamplePointDelegate DPCMSampleDraggedOutside;
        public event DPCMSamplePointDelegate DPCMSampleMapped;
        public event EmptyDelegate ProjectModified;

        public ProjectExplorer()
        {
            Localization.Localize(this);

            registerValues = new NesApu.NesRegisterValues();
            registerViewers[ExpansionType.None] = new ApuRegisterViewer(registerValues);
            registerViewers[ExpansionType.Vrc6] = new Vrc6RegisterViewer(registerValues);
            registerViewers[ExpansionType.Vrc7] = new Vrc7RegisterViewer(registerValues);
            registerViewers[ExpansionType.Fds]  = new FdsRegisterViewer(registerValues);
            registerViewers[ExpansionType.Mmc5] = new Mmc5RegisterViewer(registerValues);
            registerViewers[ExpansionType.N163] = new N163RegisterViewer(registerValues);
            registerViewers[ExpansionType.S5B]  = new S5BRegisterViewer(registerValues);
            registerViewers[ExpansionType.EPSM] = new EpsmRegisterViewer(registerValues);

            SetTickEnabled(true);
        }

        private void UpdateRenderCoords()
        {
            expandSizeX = DpiScaling.ScaleForWindow(DefaultExpandSizeX);
            spacingX    = DpiScaling.ScaleForWindow(DefaultSpacingX);
            marginX     = DpiScaling.ScaleForWindow(DefaultMarginX);
            iconSizeX   = DpiScaling.ScaleForWindow(DefaultIconSizeX);
            panelSizeY  = DpiScaling.ScaleForWindow(DefaultPanelSizeY);
            sliderSizeX = DpiScaling.ScaleForWindow(DefaultSliderSizeX);
        }

        public void Reset()
        {
            expandedInstrument = null;
            expandedSample = null;
            selectedInstrumentTab = null;
            RecreateAllControls();
            MarkDirty();
        }

        public void LayoutChanged()
        {
            UpdateRenderCoords();
            ClampScroll();
            MarkDirty();
        }

        private GradientPanel CreateGradientPanel(Color color, object userData = null, bool scroll = true, Control ctrlBefore = null)
        {
            var actualContainer = scroll ? mainContainer : this;
            var lastControl = ctrlBefore != null ? ctrlBefore : actualContainer.FindLastControlOfType<GradientPanel>();
            var y = lastControl != null ? lastControl.Bottom : 0;
            var panel = new GradientPanel(color);
            panel.Move(0, y, actualContainer.Width, panelSizeY);
            panel.UserData = userData;
            actualContainer.AddControl(panel);
            return panel;
        }

        private Label CreateCenteredLabel(GradientPanel panel, string text, int width, bool ellipsis = false)
        {
            var label = new Label(text, false);
            label.Bold = true;
            label.Centered = true;
            label.Ellipsis = ellipsis;
            label.Move(Utils.DivideAndRoundDown(panel.Width - width, 2), 0, width, panel.Height);
            panel.AddControl(label);
            return label;
        }

        private Label CreateLabel(GradientPanel panel, string text, bool dark, int x, int y, int width, bool ellipsis = false)
        {
            var label = new Label(text, false);
            label.Color = dark ? Theme.BlackColor : label.Color;
            label.Ellipsis = ellipsis;
            label.Move(x, y, width, panel.Height);
            panel.AddControl(label);
            return label;
        }

        private Button CreateExpandButton(GradientPanel panel, bool dark, bool expanded)
        {
            Debug.Assert(
                panel.UserData is Folder     ||
                panel.UserData is Instrument || 
                panel.UserData is DPCMSample);

            var expandButton = CreateImageButton(panel, marginX, expanded ? "InstrumentExpanded" : "InstrumentExpand", dark);
            return expandButton;
        }

        private ImageBox CreateImageBox(GradientPanel panel, int x, string image, bool dark = false)
        {
            var imageBox = new ImageBox(image);
            panel.AddControl(imageBox);
            imageBox.Tint = dark ? Color.Black : Theme.LightGreyColor2;
            imageBox.AutoSizeToImage(); // MATTT : Make this built-in functionality in imagebox.
            imageBox.Move(x, Utils.DivideAndRoundUp(panel.Height - imageBox.Height, 2));
            return imageBox;
        }

        private Button CreateImageButton(GradientPanel panel, int x, string image, bool dark = true)
        {
            var button = new Button(image, null);
            button.Dark = dark;
            button.Transparent = true;
            panel.AddControl(button);
            button.AutoSizeToImage(); // MATTT : Make this built-in functionality in button.
            
            // Negative values are for right aligned buttons.
            // MATTT : Ugly, remove.
            if (x >= 0)
                button.Move(x, Utils.DivideAndRoundUp(panel.Height - button.Height, 2));
            else
                button.Move(panel.Width + x - button.Width, Utils.DivideAndRoundUp(panel.Height - button.Height, 2));

            return button;
        }

        private void CreateFolderControls(Folder folder)
        {
            var panel = CreateGradientPanel(Theme.DarkGreyColor5, folder);
            panel.ToolTip = $"<MouseRight> {MoreOptionsTooltip}";
            panel.ContainerMouseUpNotifyEvent += (s, e) => Folder_MouseUp(e, folder);

            var expand = CreateExpandButton(panel, false, folder.Expanded);
            expand.ToolTip = $"<MouseLeft> {ExpandTooltip} - <MouseRight> {MoreOptionsTooltip}";
            expand.Click += (s) => ToggleExpandFolder(folder); 

            var icon = CreateImageBox(panel, expand.Right + spacingX, folder.Expanded ? "FolderOpen" : "Folder");
            var propsButton = CreateImageButton(panel, -marginX, "Properties", false);
            propsButton.ToolTip = $"<MouseLeft> {PropertiesFolderTooltip}";
            propsButton.Click += (s) => EditFolderProperties(folder);

            CreateLabel(panel, folder.Name, false, icon.Right + spacingX, 0, propsButton.Left - icon.Right - spacingX * 2, true);
        }

        private void Folder_MouseUp(MouseEventArgs e, Folder folder)
        {
            if (!e.Handled && e.Right)
            {
                var menu = new List<ContextMenuOption>();

                menu.Add(new ContextMenuOption("MenuDelete", DeleteFolderContext, () => { AskDeleteFolder(folder); }, ContextMenuSeparator.After));
                menu.Add(new ContextMenuOption("Folder", CollapseAllContext, () => { ExpandAllFolders(folder.Type, false); }));
                menu.Add(new ContextMenuOption("FolderOpen", ExpandAllContext, () => { ExpandAllFolders(folder.Type, true); }));

                App.ShowContextMenu(menu.ToArray());
                e.MarkHandled();
            }
        }

        private void CreateTopTabs()
        {
            if (Settings.ShowRegisterViewer)
            {
                tabPanel = CreateGradientPanel(Theme.DarkGreyColor5, null, false);

                var tabSizeX = width / (int)TabType.Count;
                for (var i = 0; i < (int)TabType.Count; i++)
                {
                    var button = new Button(null, TabNames[0]);
                    button.BoldFont = (int)selectedTab == i;
                    button.Border = true;
                    button.Transparent = true;
                    button.Move(i * tabSizeX, 0, i == (int)TabType.Count - 1 ? width - tabSizeX * i : tabSizeX, panelSizeY);
                    tabPanel.AddControl(button);
                }
            }
            else
            {
                tabPanel = null;
            }
        }

        private void CreateMainContainer()
        {
            var scrollBarWidth = 0;
            var tabHeight = tabPanel != null ? tabPanel.Height : 0;

            mainContainer = new Container();
            AddControl(mainContainer);

            if (Settings.ScrollBars != Settings.ScrollBarsNone)
            {
                scrollBar = new ScrollBar();
                scrollBar.Move(width - scrollBar.ScrollBarThickness - 1, tabHeight, scrollBar.ScrollBarThickness, height - tabHeight);
                AddControl(scrollBar);
                scrollBarWidth = scrollBar.ScrollBarThickness;
            }
            else
            {
                scrollBar = null;
            }

            mainContainer.Move(0, tabHeight, width - scrollBarWidth, height - tabHeight);
        }

        private void CreateProjectHeaderControls()
        {
            var project = App.Project;
            var projectText = string.IsNullOrEmpty(project.Author) ? $"{project.Name}" : $"{project.Name} ({project.Author})";
            var panel = CreateGradientPanel(Theme.DarkGreyColor4, project);
            panel.ToolTip = $"<MouseRight> {MoreOptionsTooltip}";
            panel.ContainerMouseUpNotifyEvent += ProjectHeader_MouseUp;

            var propsButton = CreateImageButton(panel, panel.Width - iconSizeX - marginX, "Properties", false);
            propsButton.ToolTip = $"<MouseLeft> {PropertiesProjectTooltip}";
            propsButton.Click += (s) => EditProjectProperties();

            var mixerButton = CreateImageButton(panel, propsButton.Left -spacingX - iconSizeX, "Mixer", false);
            mixerButton.ToolTip = $"<MouseLeft> {AllowProjectMixerSettings}"; 
            mixerButton.Dimmed = !project.AllowMixerOverride;
            mixerButton.Click += (s) => mixerButton.Dimmed = !ToggleAllowProjectMixer();

            CreateCenteredLabel(panel, projectText, 2 * mixerButton.Left - panel.Width, true);
        }

        private void ProjectHeader_MouseUp(Control sender, MouseEventArgs e)
        {
            if (!e.Handled && e.Right)
            {
                App.ShowContextMenu(new[]
                {
                    new ContextMenuOption("MenuProperties", PropertiesProjectContext, () => { EditProjectProperties(); })
                });
                e.MarkHandled();
            }
        }

        private void CreateSongsHeaderControls()
        {
            var project = App.Project;
            var panel = CreateGradientPanel(Theme.DarkGreyColor4);
            var addButton = CreateImageButton(panel, panel.Width - iconSizeX - marginX, "Add", false);
            addButton.ToolTip = $"<MouseLeft> {AddNewSongTooltip}";
            addButton.Click += (s) => AddSong();

            var importButton = CreateImageButton(panel, addButton.Left - spacingX - iconSizeX, "InstrumentOpen", false);
            importButton.ToolTip = $"<MouseLeft> {ImportSongsTooltip}";
            importButton.Click += (s) => ImportSongs();

            var sortButton = CreateImageButton(panel, importButton.Left - spacingX - iconSizeX, "Sort", false);
            sortButton.ToolTip = $"<MouseLeft> {AutoSortSongsTooltip}";
            sortButton.Dimmed = !project.AutoSortSongs;
            sortButton.Click += (s) => SortSongs();

            CreateCenteredLabel(panel, SongsHeaderLabel, 2 * sortButton.Left - panel.Width, false);
        }

        private void CreateInstrumentsHeaderControls()
        {
            var project = App.Project;
            var panel = CreateGradientPanel(Theme.DarkGreyColor4);
            var addButton = CreateImageButton(panel, panel.Width - iconSizeX - marginX, "Add", false);
            addButton.ToolTip = $"<MouseLeft> {AddNewInstrumentTooltip}";
            addButton.Click += (s) => AskAddInstrument();
            addButton.MouseUpEvent += (s, e) => { if (e.Right) AskAddInstrument(); };

            var importButton = CreateImageButton(panel, addButton.Left - spacingX - iconSizeX, "InstrumentOpen", false);
            importButton.ToolTip = $"<MouseLeft> {ImportInstrumentsTooltip}";
            importButton.Click += (s) => ImportInstruments();

            var sortButton = CreateImageButton
                (panel, importButton.Left - spacingX - iconSizeX, "Sort", false);
            sortButton.ToolTip = $"<MouseLeft> {AutoSortInstrumentsTooltip}";
            sortButton.Dimmed = !project.AutoSortInstruments;
            sortButton.Click += (s) => SortInstruments();

            CreateCenteredLabel(panel, InstrumentHeaderLabel, 2 * sortButton.Left - panel.Width, false);
        }

        private void CreateSongControls(Song song)
        {
            var panel = CreateGradientPanel(song.Color, song);
            panel.ToolTip = $"<MouseLeft> {MakeSongCurrentTooltip} - <MouseLeft><Drag> {ReorderSongsTooltip}\n<MouseRight> {MoreOptionsTooltip}";
            panel.ContainerMouseUpNotifyEvent += (s, e) => Song_MouseUp(e, song);

            var icon = CreateImageBox(panel, marginX + expandSizeX, "Music", true);
            var props = CreateImageButton(panel, -marginX, "Properties");
            props.ToolTip = $"<MouseLeft> {PropertiesSongTooltip}";
            props.Click += (s) => EditSongProperties(song);

            var label = CreateLabel(panel, song.Name, true, icon.Right + spacingX, 0, props.Left - icon.Right - spacingX * 2, true);
            label.Bold = song == App.SelectedSong;
        }

        private void Song_MouseUp(MouseEventArgs e, Song song)
        {
            if (!e.Handled)
            {
                if (e.Left)
                {
                    // MATTT : Do we want to do this on mouse up/down? See when we add the drags.
                    App.SelectedSong = song;
                }
                else if (e.Right)
                {
                    var menu = new List<ContextMenuOption>();
                    if (App.Project.Songs.Count > 1)
                        menu.Add(new ContextMenuOption("MenuDelete", DeleteSongContext, () => { AskDeleteSong(song); }, ContextMenuSeparator.After));
                    menu.Add(new ContextMenuOption("MenuDuplicate", DuplicateContext, () => { DuplicateSong(song); }));
                    menu.Add(new ContextMenuOption("MenuProperties", PropertiesSongContext, () => { EditSongProperties(song); }, ContextMenuSeparator.Before));
                    App.ShowContextMenu(menu.ToArray());
                }
                e.MarkHandled();
            }
        }

        private void CreateInstrumentControls(Instrument instrument)
        {
            var container = CreateGradientPanel(instrument.Color, instrument);
            container.ToolTip = $"<MouseLeft> {SelectInstrumentTooltip} - <MouseLeft><Drag> {CopyReplaceInstrumentTooltip}\n<MouseRight> {MoreOptionsTooltip}";
            container.MouseUpEvent += (s, e) => Instrument_MouseUp(e, instrument); // MATTT : Needed? If so, replicate to all
            container.ContainerMouseUpNotifyEvent += (s, e) => Instrument_MouseUp(e, instrument);

            var expand = CreateExpandButton(container, true, expandedInstrument == instrument);
            expand.ToolTip = $"<MouseLeft> {ExpandTooltip} - <MouseRight> {MoreOptionsTooltip}";
            expand.Click += (s) => InstrumentExpand_Click(instrument);

            var icon = CreateImageBox(container, expand.Right + spacingX, ExpansionType.Icons[instrument.Expansion], true);

            var x = marginX; 
            var props = CreateImageButton(container, -x, "Properties");
            props.ToolTip = $"<MouseLeft> {PropertiesInstrumentTooltip} - <MouseRight> {MoreOptionsTooltip}";
            props.Click += (s) => EditInstrumentProperties(instrument);

            if (instrument.Expansion == ExpansionType.None)
            {
                x += spacingX + props.Width;
                var dpcm = CreateImageButton(container, -x, "ChannelDPCM");
                dpcm.Dimmed = !instrument.HasAnyMappedSamples;
                dpcm.UserData = "DPCM";
                dpcm.ToolTip = $"<MouseLeft> {EditSamplesTooltip} - <MouseRight> {MoreOptionsTooltip}";
                dpcm.Click += (s) => App.StartEditDPCMMapping(instrument);
            }

            var lastEnv = (Button)null;
            for (var i = 0; i < EnvelopeDisplayOrder.Length; i++)
            {
                var idx = EnvelopeDisplayOrder[i];
                if (instrument.Envelopes[idx] != null)
                {
                    x += spacingX + props.Width;
                    var env = CreateImageButton(container, -x, EnvelopeType.Icons[idx]);
                    env.Dimmed = instrument.Envelopes[idx].IsEmpty(idx);
                    env.UserData = instrument.Envelopes[idx];
                    env.ToolTip = $"<MouseLeft> {EditEnvelopeTooltip.Format(EnvelopeType.LocalizedNames[idx].Value.ToLower())} - <MouseLeft><Drag> {CopyEnvelopeTooltip} - <MouseRight> {MoreOptionsTooltip}";
                    env.Click += (s) => App.StartEditInstrument(instrument, idx);
                    env.MouseUpEvent += (s, e) => Instrument_MouseUp(e, instrument, idx);
                    lastEnv = env;
                }
            }

            var label = CreateLabel(container, instrument.Name, true, icon.Right + spacingX, 0, lastEnv.Left - icon.Right - spacingX * 2, true);
            label.Bold = App.SelectedInstrument == instrument;
        }

        private void InstrumentExpand_Click(Instrument instrument)
        {
            ToggleExpandInstrument(instrument);
        }

        private void Instrument_MouseUp(MouseEventArgs e, Instrument inst, int envelopeType = -1)
        {
            if (!e.Handled)
            {
                if (e.Left)
                {
                    App.SelectedInstrument = inst;
                }
                else if (e.Right)
                {
                    Instrument_RightClick(inst, envelopeType);
                }
                e.MarkHandled();
            }
        }

        private void Instrument_RightClick(Instrument inst, int envelopeType = -1)
        {
            var menu = new List<ContextMenuOption>();

            if (inst != null)
            {
                menu.Add(new ContextMenuOption("MenuDelete", DeleteInstrumentContext, () => { AskDeleteInstrument(inst); }, ContextMenuSeparator.After));

                if (envelopeType >= 0)
                {
                    // MATTT : Need to grey out button here.
                    menu.Add(new ContextMenuOption("MenuClearEnvelope", ClearEnvelopeContext, () => { ClearInstrumentEnvelope(inst, envelopeType); }, ContextMenuSeparator.After));
                }
                else
                {
                    if (inst.IsN163 || inst.IsFds)
                    {
                        menu.Add(new ContextMenuOption("MenuWave", ResampleWavContext, () => { LoadN163FdsResampleWavFile(inst); }, ContextMenuSeparator.Before));

                        if (inst.IsN163 && inst.N163ResampleWaveData != null ||
                            inst.IsFds  && inst.FdsResampleWaveData  != null)
                        {
                            menu.Add(new ContextMenuOption("MenuTrash", DiscardWavDataContext, () => { ClearN163FdsResampleWavData(inst); }));
                        }
                    }

                    if (Platform.IsDesktop && (inst.IsVrc7 || inst.IsEpsm))
                    {
                        menu.Add(new ContextMenuOption("MenuCopy", CopyRegisterValueContext, () => { CopyRegisterValues(inst); }, ContextMenuSeparator.Before));
                        menu.Add(new ContextMenuOption("MenuPaste", PasteRegisterValueContext, () => { PasteRegisterValues(inst); }));
                    }

                    menu.Add(new ContextMenuOption("MenuDuplicate", DuplicateContext, () => { DuplicateInstrument(inst); }, ContextMenuSeparator.Before));
                    menu.Add(new ContextMenuOption("MenuReplace", ReplaceWithContext, () => { AskReplaceInstrument(inst); }, ContextMenuSeparator.After));

                    if (App.Project.UsesAnyExpansionAudio)
                    {
                        var activeExpansions = App.Project.GetActiveExpansions();

                        foreach (var exp in activeExpansions)
                        {
                            if (exp != inst.Expansion && (exp == ExpansionType.None || ExpansionType.NeedsExpansionInstrument(exp)))
                            {
                                var e = exp;
                                menu.Add(new ContextMenuOption(ExpansionType.Icons[exp], DuplicateConvertContext.Format(ExpansionType.GetLocalizedName(exp, ExpansionType.LocalizationMode.Instrument)), () => { DuplicateConvertInstrument(inst, e); }));
                            }
                        }
                    }
                }

                menu.Add(new ContextMenuOption("MenuProperties", PropertiesInstrumentContext, () => { EditInstrumentProperties(inst); }, ContextMenuSeparator.Before));
            }

            App.ShowContextMenu(menu.ToArray());
        }

        private void CreateDpcmSampleControls(DPCMSample sample)
        {
            var panel = CreateGradientPanel(sample.Color, sample);
            panel.ToolTip = $"<MouseRight> {MoreOptionsTooltip}";
            panel.ContainerMouseUpNotifyEvent += (s, e) => DpcmSample_MouseUp(e, sample);

            var expand = CreateExpandButton(panel, true, expandedSample == sample);
            expand.ToolTip = $"<MouseLeft> {ExpandTooltip} - <MouseRight> {MoreOptionsTooltip}";
            expand.Click += (s) => ToggleExpandDPCMSample(sample);
            
            var icon = CreateImageBox(panel, expand.Right, "ChannelDPCM", true);
            var props = CreateImageButton(panel, panel.Width - marginX - iconSizeX, "Properties");
            props.ToolTip = $"<MouseLeft> {PropertiesInstrumentTooltip}";
            props.Click += (s) => EditDPCMSampleProperties(sample);

            var editWave = CreateImageButton(panel, props.Left - spacingX - iconSizeX, "WaveEdit");
            editWave.ToolTip = $"<MouseLeft> {EditWaveformTooltip}";
            editWave.Click += (s) => App.StartEditDPCMSample(sample);

            var reload = CreateImageButton(panel, editWave.Left - spacingX - iconSizeX, "Reload");
            reload.ToolTip = $"<MouseLeft> {ReloadSourceDataTooltip}";
            reload.Click += (s) => ReloadDPCMSampleSourceData(sample);

            var play = CreateImageButton(panel, reload.Left - spacingX - iconSizeX, "PlaySource");
            play.ToolTip = $"<MouseLeft> {PreviewProcessedSampleTooltip}\n<MouseRight> {PlaySourceSampleTooltip}";
            play.Click += (s) => App.PreviewDPCMSample(sample, false);
            //play.RightClick += (s) => App.PreviewDPCMSample(sample, true); // MATTT 

            CreateLabel(panel, sample.Name, true, icon.Right + spacingX, 0, play.Left - icon.Right - spacingX * 2, true);
        }

        private void DpcmSample_MouseUp(MouseEventArgs e, DPCMSample sample)
        {
        }

        private void CreateNoneArpeggioControls()
        {
            noneArpPanel = CreateGradientPanel(Theme.LightGreyColor1);
            noneArpPanel.ToolTip = $"<MouseLeft> {SelectArpeggioTooltip}";
            noneArpPanel.ContainerMouseUpNotifyEvent += (s, e) => Arpeggio_MouseUp(e, null);

            var icon = CreateImageBox(noneArpPanel, marginX + expandSizeX, EnvelopeType.Icons[EnvelopeType.Arpeggio], true);
            var label = CreateLabel(noneArpPanel, ArpeggioNoneLabel, true, icon.Right + spacingX, 0, noneArpPanel.Width - icon.Right - spacingX);
            label.Bold = App.SelectedArpeggio == null;
        }

        private void CreateArpeggioControls(Arpeggio arp)
        {
            var panel = CreateGradientPanel(arp.Color, arp);
            panel.ToolTip = $"<MouseLeft> {SelectArpeggioTooltip} - <MouseLeft><Drag> {ReplaceArpeggioTooltip}\n<MouseRight> {MoreOptionsTooltip}";
            panel.ContainerMouseUpNotifyEvent += (s, e) => Arpeggio_MouseUp(e, arp);

            var icon = CreateImageBox(panel, marginX + expandSizeX, EnvelopeType.Icons[EnvelopeType.Arpeggio], true);
            var props = CreateImageButton(panel, -marginX, "Properties");
            props.ToolTip = $"<MouseLeft> {PropertiesArpeggioTooltip}";
            props.Click += (s) => EditArpeggioProperties(arp);

            var label = CreateLabel(panel, arp.Name, true, icon.Right + spacingX, 0, props.Left - icon.Right - spacingX * 2);
            label.Bold = App.SelectedArpeggio == arp;
        }

        private void Arpeggio_MouseUp(MouseEventArgs e, Arpeggio arp)
        {
            if (e.Left)
            {
                // MATTT : Do we want to do this on mouse up/down? See when we add the drags.
                App.SelectedArpeggio = arp;
            }
            else if (e.Right && arp != null)
            {
                var menu = new List<ContextMenuOption>();
                menu.Add(new ContextMenuOption("MenuDelete", DeleteArpeggioContext, () => { AskDeleteArpeggio(arp); }, ContextMenuSeparator.After));
                menu.Add(new ContextMenuOption("MenuDuplicate", DuplicateContext, () => { DuplicateArpeggio(arp); }));
                menu.Add(new ContextMenuOption("MenuReplace", ReplaceWithContext, () => { AskReplaceArpeggio(arp); }));
                menu.Add(new ContextMenuOption("MenuProperties", PropertiesArpeggioContext, () => { EditArpeggioProperties(arp); }, ContextMenuSeparator.Before));
                App.ShowContextMenu(menu.ToArray());
            }
        }

        private void CreateParamTabs(GradientPanel panel, int x, int y, int width, int height, string[] tabNames, string selelectedTabName)
        {
            var tabWidth = width / tabNames.Length;
            for (int i = 0; i < tabNames.Length; i++)
            {
                var name = tabNames[i];
                var tab = new SimpleTab(name, name == selelectedTabName);
                tab.Move(x + i * tabWidth, y, tabWidth, height);
                tab.Click += (s) => { selectedInstrumentTab = name; RecreateAllControls(); }; // HACK : This is only used for instruments right now.
                panel.AddControl(tab);
            }
        }

        private ParamSlider CreateParamSlider(GradientPanel panel, ParamInfo p, int y, int width)
        {
            var slider = new ParamSlider(p);
            panel.AddControl(slider);
            slider.Move(panel.Width - sliderSizeX - marginX, y + Utils.DivideAndRoundUp(panelSizeY - slider.Height, 2), sliderSizeX, slider.Height);
            return slider;
        }

        private ParamList CreateParamList(GradientPanel panel, ParamInfo p, int y, int height)
        {
            var list = new ParamList(p);
            panel.AddControl(list);
            list.Move(panel.Width - sliderSizeX - marginX, y + Utils.DivideAndRoundUp(panelSizeY - list.Height, 2), sliderSizeX, list.Height);
            return list;
        }

        private ParamCheckBox CreateParamCheckBox(GradientPanel panel, ParamInfo p, int y, int height)
        {
            var check = new ParamCheckBox(p);
            panel.AddControl(check);
            check.Move(panel.Width - check.Width - marginX, y + Utils.DivideAndRoundUp(panelSizeY - check.Height, 2));
            return check;
        }

        private void CreateParamCustomDraw(GradientPanel panel, ParamInfo p, int x, int y, int width, int height)
        {
            var custom = new ParamCustomDraw(p);
            panel.AddControl(custom);
            custom.Move(x, y, width, height);
        }

        private void CreateParamsControls(Color color, object userData, ParamInfo[] parameters, TransactionScope transScope, int transObjectId, string selectedTabName = null)
        {
            if (parameters != null)
            {
                List<string> tabNames = null;

                foreach (var param in parameters)
                {
                    if (param.HasTab)
                    {
                        if (tabNames == null)
                            tabNames = new List<string>();

                        if (!tabNames.Contains(param.TabName))
                            tabNames.Add(param.TabName);
                    }
                }

                if (tabNames != null && (string.IsNullOrEmpty(selectedTabName) || !tabNames.Contains(selectedTabName)))
                    selectedTabName = tabNames[0];

                var y = 0;
                var tabCreated = false;
                var panel = CreateGradientPanel(color);
                var indentX = marginX + expandSizeX;

                foreach (var param in parameters)
                {
                    if (!tabCreated && param.HasTab)
                    {
                        CreateParamTabs(panel, indentX, y, panel.Width - indentX - marginX, panelSizeY, tabNames.ToArray(), selectedTabName);
                        y += panelSizeY;
                        tabCreated = true;
                    }

                    if (param.HasTab && selectedTabName != param.TabName)
                    {
                        continue;
                    }

                    if (param.CustomHeight > 0)
                    {
                        Debug.Assert(param.CustomDraw != null);
                        var customHeight = param.CustomHeight * panelSizeY;
                        CreateParamCustomDraw(panel, param, indentX, y, panel.Width - indentX - marginX, customHeight);
                        y += customHeight;
                    }
                    else
                    {
                        if (!param.IsEmpty)
                        {
                            var label = CreateLabel(panel, param.Name, true, indentX, y, panel.Width - indentX);
                            label.ToolTip = param.ToolTip; // TODO : The whole row should display the tooltip.

                            if (param.IsList)
                            {
                                var paramList = CreateParamList(panel, param, y, panelSizeY);
                                paramList.ToolTip = $"<MouseLeft> {ChangeValueTooltip}\n<MouseRight> {MoreOptionsTooltip}";
                                paramList.ValueChangeStart += (s) => App.UndoRedoManager.BeginTransaction(transScope, transObjectId);
                                paramList.ValueChangeEnd   += (s) => App.UndoRedoManager.EndTransaction();
                            }
                            else if (param.GetMaxValue() == 1)
                            {
                                var paramCheck = CreateParamCheckBox(panel, param, y, panelSizeY);
                                paramCheck.ToolTip = $"<MouseLeft> {ToggleValueTooltip}\n<MouseRight> {MoreOptionsTooltip}";
                                paramCheck.ValueChangeStart += (s) => App.UndoRedoManager.BeginTransaction(transScope, transObjectId);
                                paramCheck.ValueChangeEnd   += (s) => App.UndoRedoManager.EndTransaction();
                            }
                            else
                            {
                                var paramSlider = CreateParamSlider(panel, param, y, 100);
                                paramSlider.ToolTip = $"<MouseLeft><Drag> {ChangeValueTooltip} - <Ctrl><MouseLeft><Drag> {ChangeValueFineTooltip}\n<MouseRight> {MoreOptionsTooltip}";
                                paramSlider.ValueChangeStart += (s) => App.UndoRedoManager.BeginTransaction(transScope, transObjectId);
                                paramSlider.ValueChangeEnd   += (s) => App.UndoRedoManager.EndTransaction();
                            }
                        }

                        y += panelSizeY;
                    }
                }

                panel.Resize(panel.Width, y);
            }
        }

        private void CreateAllSongsControls()
        {
            var project = App.Project;
            var folders = project.GetFoldersForType(FolderType.Song);
            folders.Insert(0, null);

            foreach (var f in folders)
            {
                var songs = project.GetSongsInFolder(f == null ? null : f.Name);

                if (f != null)
                {
                    CreateFolderControls(f);
                    if (!f.Expanded)
                        continue;
                }

                foreach (var song in songs)
                {
                    CreateSongControls(song);
                }
            }
        }

        private void CreateAllInstrumentsControls()
        {
            var project = App.Project;
            var folders = project.GetFoldersForType(FolderType.Instrument);
            folders.Insert(0, null);

            foreach (var f in folders)
            {
                var instruments = project.GetInstrumentsInFolder(f == null ? null : f.Name);

                if (f != null)
                {
                    CreateFolderControls(f);
                    if (!f.Expanded)
                        continue;
                }

                foreach (var instrument in instruments)
                {
                    CreateInstrumentControls(instrument);
                    if (instrument == expandedInstrument)
                        CreateParamsControls(instrument.Color, instrument, InstrumentParamProvider.GetParams(instrument), TransactionScope.Instrument, instrument.Id, selectedInstrumentTab);
                }
            }
        }

        private void CreateDpcmSamplesHeaderControls()
        {
            var project = App.Project;
            var panel = CreateGradientPanel(Theme.DarkGreyColor4);
            var importButton = CreateImageButton(panel, panel.Width - iconSizeX - marginX, "InstrumentOpen", false);
            importButton.ToolTip = $"<MouseLeft> {ImportSamplesTooltip}";
            importButton.Click += (s) => LoadDPCMSample();

            var sortButton = CreateImageButton(panel, importButton.Left - spacingX - iconSizeX, "Sort", false);
            sortButton.ToolTip = $"<MouseLeft> {AutoSortSamplesTooltip}";
            sortButton.Dimmed = !project.AutoSortSamples;
            sortButton.Click += (s) => SortSamples();

            CreateCenteredLabel(panel, SamplesHeaderLabel, 2 * sortButton.Left - panel.Width, false);
        }

        private void CreateAllDpcmSamplesControls()
        {
            var project = App.Project;
            var folders = project.GetFoldersForType(FolderType.Sample);
            folders.Insert(0, null);

            foreach (var f in folders)
            {
                var samples = project.GetSamplesInFolder(f == null ? null : f.Name);

                if (f != null)
                {
                    CreateFolderControls(f);
                    if (!f.Expanded)
                        continue;
                }

                foreach (var sample in samples)
                {
                    CreateDpcmSampleControls(sample);
                    if (sample == expandedSample)
                        CreateParamsControls(sample.Color, sample, DPCMSampleParamProvider.GetParams(sample), TransactionScope.DPCMSample, sample.Id);
                }
            }
        }

        private void CreateArpeggioHeaderControls()
        {
            var project = App.Project;
            var panel = CreateGradientPanel(Theme.DarkGreyColor4);
            var addButton = CreateImageButton(panel, panel.Width - iconSizeX - marginX, "Add", false);
            addButton.ToolTip = $"<MouseLeft> {AddNewArpeggioTooltip}";
            addButton.Click += (s) => AddArpeggio();

            var sortButton = CreateImageButton(panel, addButton.Left - spacingX - iconSizeX, "Sort", false);
            sortButton.ToolTip = $"<MouseLeft> {AutoSortArpeggiosTooltip}";
            sortButton.Dimmed = !project.AutoSortArpeggios;
            sortButton.Click += (s) => SortArpeggios();

            CreateCenteredLabel(panel, ArpeggiosHeaderLabel, 2 * sortButton.Left - panel.Width, false);
        }

        private void CreateAllArpeggiosControls()
        {
            CreateNoneArpeggioControls();

            var project = App.Project;
            var folders = project.GetFoldersForType(FolderType.Arpeggio);
            folders.Insert(0, null);

            foreach (var f in folders)
            {
                var arpeggios = project.GetArpeggiosInFolder(f == null ? null : f.Name);

                if (f != null)
                {
                    CreateFolderControls(f);
                    if (!f.Expanded)
                        continue;
                }

                foreach (var arp in arpeggios)
                {
                    CreateArpeggioControls(arp);
                }
            }
        }

        private RegisterViewerPanel CreateRegisterViewerPanel(RegisterViewerRow[] rows, int exp = -1)
        {
            var lastPanel = mainContainer.FindLastControlOfType<GradientPanel>();
            var regViewer = new RegisterViewerPanel(registerValues, rows, exp);
            mainContainer.AddControl(regViewer);
            regViewer.Move(0, lastPanel.Bottom, width, regViewer.Height);
            return regViewer;
        }

        private void CreateAllRegisterViewerControls()
        {
            var project = App.Project;
            var expansions = project.GetActiveExpansions();
            var lastControl = (Control)null;

            foreach (var e in expansions)
            {
                var expRegs = registerViewers[e];

                if (expRegs != null)
                {
                    // Raw register values for each expansions.
                    var expName = ExpansionType.GetLocalizedName(e, ExpansionType.LocalizationMode.ChipName);

                    var regHeader = CreateGradientPanel(Theme.DarkGreyColor5, null, true, lastControl);
                    var regIcon = CreateImageBox(regHeader, marginX, ExpansionType.Icons[e]);
                    CreateLabel(regHeader, RegistersExpansionHeaderLabel.Format(expName), false, regIcon.Right + spacingX, 0, width);
                    lastControl = CreateRegisterViewerPanel(expRegs.RegisterRows, e);

                    // Register interpreters for each channels
                    var numChannels = expRegs.GetNumInterpreterRows(project);
                    for (int i = 0; i < numChannels; i++)
                    {
                        var chanRegs = expRegs.InterpeterRows[i];

                        if (chanRegs != null && chanRegs.Length > 0)
                        {
                            var chanHeader = CreateGradientPanel(Theme.DarkGreyColor5, null, true, lastControl);
                            var chanIcon = CreateImageBox(chanHeader, marginX, RegisterViewer.Icons[expRegs.InterpreterIcons[i]]);
                            CreateLabel(chanHeader, expRegs.InterpreterLabels[i], false, chanIcon.Right + spacingX, 0, width);
                            lastControl = CreateRegisterViewerPanel(chanRegs, e);
                        }
                    }
                }
            }
        }

        public void RecreateAllControls()
        {
            // MATTT
            //Debug.Assert(captureOperation != CaptureOperation.MoveSlider);

            // MATTT
            //if (selectedTab == TabType.Registers && !Settings.ShowRegisterViewer)
            //    selectedTab = TabType.Project;

            UpdateRenderCoords();

            if (ParentWindow == null || App.Project == null)
                return;

            // MATTT : Add the project/register tabs here.

            RemoveAllControls();

            CreateTopTabs();
            CreateMainContainer();

            if (selectedTab == TabType.Project)
            {
                CreateProjectHeaderControls();
                CreateSongsHeaderControls();
                CreateAllSongsControls();
                CreateInstrumentsHeaderControls();
                CreateAllInstrumentsControls();
                CreateDpcmSamplesHeaderControls();
                CreateAllDpcmSamplesControls();
                CreateArpeggioHeaderControls();
                CreateAllArpeggiosControls();
            }
            else
            {
                CreateAllRegisterViewerControls();
            }
            
            flingVelY = 0.0f;
            highlightedButtonIdx = -1;
            virtualSizeY = mainContainer.GetControlsRect().Bottom;
            ClampScroll();
        }

        private void UpdateSelectedItem(Type type, object obj)
        {
            foreach (var ctrl in mainContainer.Controls)
            {
                if (ctrl is GradientPanel panel)
                {
                    if (panel.UserData != null && panel.UserData.GetType() == type)
                    {
                        panel.FindControlOfType<Label>().Bold = panel.UserData == obj;
                    }
                }
            }
        }

        public void SelectedSongChanged()
        {
            UpdateSelectedItem(typeof(Song), App.SelectedSong);
        }

        public void SelectedInstrumentChanged()
        {
            UpdateSelectedItem(typeof(Instrument), App.SelectedInstrument);
        }

        public void SelectedArpeggioChanged()
        {
            // Special case for "None" arpeggio.
            noneArpPanel.FindControlOfType<Label>().Bold = App.SelectedArpeggio == null;
            UpdateSelectedItem(typeof(Arpeggio), App.SelectedArpeggio);
        }

        public void InstrumentEnvelopeChanged()
        {
            // This is a bit overkill. We could restrict to instrument that has changed.
            foreach (var ctrl in mainContainer.Controls)
            {
                if ((ctrl is GradientPanel panel) && (panel.UserData is Instrument inst))
                {
                    for (int i = 0; i < inst.Envelopes.Length; i++)
                    {
                        if (inst.Envelopes[i] != null && inst.IsEnvelopeVisible(i))
                        {
                            var env = inst.Envelopes[i];
                            var button = panel.FindControlByUserData(env) as Button;
                            button.Dimmed = env.IsEmpty(i);
                        }
                    }
                }
            }
        }

        public void NotifyDPCMSampleMapped()
        {
            foreach (var ctrl in mainContainer.Controls)
            {
                if ((ctrl is GradientPanel panel) && (panel.UserData is Instrument inst))
                {
                    var button = panel.FindControlByUserData("DPCM") as Button;
                    if (button != null)
                        button.Dimmed = !inst.HasAnyMappedSamples;
                }
            }
        }

        public void BlinkButton(object obj)
        {
            if (obj != null)
            {
                var c = mainContainer.FindControlByUserData(obj);
                if (c is GradientPanel p)
                {
                    // MATTT : Scroll to the button.
                    //scrollY = buttonY - Height / 2;
                    //ClampScroll();
                    //MarkDirty();
                    p.Blink();
                }
            }
        }

        protected override void OnAddedToContainer()
        {
            RecreateAllControls();
        }

        // Return value is index of the button after which we should insert.
        // If folder != null, we are inserting inside a folder.
        private int GetDragCaptureState(int x, int y, out Folder folder)
        {
            // MATTT
            folder = null;

            //if (ClientRectangle.Contains(x, y))
            //{ 
            //    var buttonIdx = GetButtonAtCoord(x, y - buttonSizeY / 2, out _);

            //    if (buttonIdx >= 0)
            //    {
            //        var button = buttons[buttonIdx];
            //        var nextButton = buttonIdx != buttons.Count - 1 ? buttons[buttonIdx + 1] : null;

            //        if ((captureOperation == CaptureOperation.DragSong       && (button.type == ButtonType.Song       || button.type == ButtonType.SongHeader       || button.type == ButtonType.SongFolder)) ||
            //            (captureOperation == CaptureOperation.DragInstrument && (button.type == ButtonType.Instrument || button.type == ButtonType.InstrumentHeader || button.type == ButtonType.InstrumentFolder)) ||
            //            (captureOperation == CaptureOperation.DragSample     && (button.type == ButtonType.Dpcm       || button.type == ButtonType.DpcmHeader       || button.type == ButtonType.DpcmFolder)) ||
            //            (captureOperation == CaptureOperation.DragArpeggio   && ((button.type == ButtonType.Arpeggio && draggedArpeggio != null) || button.type == ButtonType.ArpeggioFolder)) || // No header to account for "None" arp.
            //            // Folder can be insert : below another folder OR below the last item not in a folder
            //            (captureOperation == CaptureOperation.DragFolder     && ((button.folder != null && button.folder.Type == draggedFolder.Type) || (!button.ItemIsInFolder && nextButton != null && nextButton.folder != null && nextButton.folder.Type == draggedFolder.Type))))
            //        {
            //            if (captureOperation != CaptureOperation.DragFolder && button.folder != null)
            //            {
            //                folder = button.folder;
            //            }
            //            else
            //            {
            //                switch (captureOperation)
            //                {
            //                    case CaptureOperation.DragSong:       folder = button.song?.Folder;       break;
            //                    case CaptureOperation.DragInstrument: folder = button.instrument?.Folder; break;
            //                    case CaptureOperation.DragSample:     folder = button.sample?.Folder;     break;
            //                    case CaptureOperation.DragArpeggio:   folder = button.arpeggio?.Folder;   break;
            //                }   
            //            }

            //            return buttonIdx;
            //        }
            //    }
            //}

            return -1;
        }

        protected override void OnRender(Graphics g)
        {
            if (selectedTab == TabType.Registers)
                App.ActivePlayer.GetRegisterValues(registerValues);

            base.OnRender(g);

            /*
            // HACK : This is gross. We have logic in the rendering code. This should be done elsewhere.
            if (captureOperation != CaptureOperation.None && captureThresholdMet)
            {
                if ((captureOperation == CaptureOperation.DragSong)       ||
                    (captureOperation == CaptureOperation.DragInstrument) ||
                    (captureOperation == CaptureOperation.DragSample) ||
                    (captureOperation == CaptureOperation.DragArpeggio && draggedArpeggio != null) ||
                    (captureOperation == CaptureOperation.DragFolder))
                {
                    var pt = Platform.IsDesktop ? ScreenToControl(CursorPosition) : new Point(mouseLastX, mouseLastY);
                    var beforeButtonIdx = GetDragCaptureState(pt.X, pt.Y, out var draggedInFolder);

                    if (beforeButtonIdx >= 0)
                    {
                        var lineY = (beforeButtonIdx + 1) * buttonSizeY - scrollY;
                        var lineColor = Theme.LightGreyColor2;

                        switch (captureOperation)
                        {
                            case CaptureOperation.DragSong:       lineColor = draggedSong.Color;       break;
                            case CaptureOperation.DragInstrument: lineColor = draggedInstrument.Color; break;
                            case CaptureOperation.DragSample:     lineColor = draggedSample.Color;     break;
                            case CaptureOperation.DragArpeggio:   lineColor = draggedArpeggio.Color;   break;
                        }

                        c.DrawLine(draggedInFolder != null ? expandButtonSizeX : 0, lineY, contentSizeX, lineY, lineColor, draggedLineSizeY);
                    }
                }
                else if ((captureOperation == CaptureOperation.DragInstrumentEnvelope || captureOperation == CaptureOperation.DragArpeggioValues) && envelopeDragIdx >= 0 || 
                         (captureOperation == CaptureOperation.DragInstrumentSampleMappings))
                {
                    var pt = Platform.IsDesktop ? ScreenToControl(CursorPosition) : new Point(mouseLastX, mouseLastY);
                    if (ClientRectangle.Contains(pt))
                    {
                        var bx = pt.X - captureButtonRelX;
                        var by = pt.Y - captureButtonRelY - topTabSizeY;
                        var bmp = captureOperation == CaptureOperation.DragInstrumentSampleMappings ? bmpDPCM : bmpEnvelopes[envelopeDragIdx];
                        
                        c.DrawTextureAtlas(bmp, bx, by, 0.5f, bitmapScale, Color.Black);

                        if (Platform.IsMobile)
                            c.DrawRectangle(bx, by, bx + iconSize - 4, by + iconSize - 4, Theme.WhiteColor, 3, true, true);
                    }
                }
            }
            */
        }

        private bool ClampScroll()
        {
            if (mainContainer != null)
            {
                var minScrollY = 0;
                var maxScrollY = Math.Max(virtualSizeY - mainContainer.Height, 0);

                var scrolled = true;
                if (mainContainer.ScrollY < minScrollY) { mainContainer.ScrollY = minScrollY; scrolled = false; }
                if (mainContainer.ScrollY > maxScrollY) { mainContainer.ScrollY = maxScrollY; scrolled = false; }
                return scrolled;
            }
            else
            {
                return false;
            }
        }

        private bool DoScroll(int deltaY)
        {
            //scrollY -= deltaY;
            mainContainer.ScrollY -= deltaY;
            MarkDirty();
            return ClampScroll();
        }

        protected void UpdateCursor()
        {
            if ((captureOperation == CaptureOperation.DragInstrumentEnvelope || captureOperation == CaptureOperation.DragArpeggioValues) && captureThresholdMet)
            {
                Cursor = Cursors.CopyCursor;
            }
            else if (
                captureOperation == CaptureOperation.DragSong       ||
                captureOperation == CaptureOperation.DragInstrument ||
                captureOperation == CaptureOperation.DragArpeggio   ||
                captureOperation == CaptureOperation.DragSample     ||
                captureOperation == CaptureOperation.DragFolder)
            {
                Cursor = Cursors.DragCursor;
            }
            else
            {
                Cursor = Cursors.Default;
            }
        }

        private void ScrollIfNearEdge(int x, int y)
        {
            // MATTT

            //int minY = Platform.IsMobile && IsLandscape ? 0      : -buttonSizeY;
            //int maxY = Platform.IsMobile && IsLandscape ? Height : Height + buttonSizeY;

            //scrollY += Utils.ComputeScrollAmount(y, minY, buttonSizeY, App.AverageTickRate * ScrollSpeedFactor, true);
            //scrollY += Utils.ComputeScrollAmount(y, maxY, buttonSizeY, App.AverageTickRate * ScrollSpeedFactor, false);

            ClampScroll();
        }

        private void UpdateScrollBar(int x, int y)
        {
            // MATTT
            //scrollY = captureScrollY + ((y - captureMouseY) * virtualSizeY / Height);
            ClampScroll();
            MarkDirty();
        }

        private void UpdateDrag(int x, int y, bool final)
        {
            //if (final)
            //{
            //    var buttonIdx = GetDragCaptureState(x, y, out var draggedInFolder);
            //    var button = buttonIdx >= 0 ? buttons[buttonIdx] : null;
            //    var inside = ClientRectangle.Contains(x, y);

            //    if (captureOperation == CaptureOperation.DragSong)
            //    {
            //        if (inside && button != null)
            //        {
            //            Debug.Assert(button.type == ButtonType.Song || button.type == ButtonType.SongHeader || button.type == ButtonType.SongFolder);

            //            var songBefore = buttons[buttonIdx].song;
            //            if (songBefore != draggedSong)
            //            {
            //                var oldFolder = draggedSong.Folder;
            //                App.UndoRedoManager.BeginTransaction(TransactionScope.ProjectNoDPCMSamples);
            //                App.Project.MoveSong(draggedSong, songBefore);
            //                draggedSong.FolderName = draggedInFolder == null ? null : draggedInFolder.Name;
            //                if (draggedInFolder != null && !draggedInFolder.Expanded)
            //                {
            //                    draggedInFolder.Expanded = true;
            //                    BlinkButton(draggedSong);
            //                }
            //                if (oldFolder == draggedSong.Folder)
            //                {
            //                    App.Project.AutoSortSongs = false;
            //                }
            //                App.Project.ConditionalSortSongs();
            //                App.UndoRedoManager.EndTransaction();
            //            }
            //        }
            //    }
            //    else if (captureOperation == CaptureOperation.DragInstrument)
            //    {
            //        if (inside && button != null)
            //        {
            //            Debug.Assert(button.type == ButtonType.Instrument || button.type == ButtonType.InstrumentHeader || button.type == ButtonType.InstrumentFolder);

            //            var instrumentBefore = buttons[buttonIdx].instrument;
            //            if (instrumentBefore != draggedInstrument)
            //            {
            //                var oldFolder = draggedInstrument.Folder;
            //                App.UndoRedoManager.BeginTransaction(TransactionScope.ProjectNoDPCMSamples);
            //                App.Project.MoveInstrument(draggedInstrument, instrumentBefore);
            //                draggedInstrument.FolderName = draggedInFolder == null ? null : draggedInFolder.Name;
            //                if (draggedInFolder != null && !draggedInFolder.Expanded)
            //                {
            //                    draggedInFolder.Expanded = true;
            //                    BlinkButton(draggedInstrument);
            //                }
            //                if (oldFolder == draggedInstrument.Folder)
            //                {
            //                    App.Project.AutoSortInstruments = false;
            //                }
            //                App.Project.ConditionalSortInstruments();
            //                App.UndoRedoManager.EndTransaction();
            //            }
            //        }
            //        else if (Platform.IsDesktop && !inside)
            //        {
            //            InstrumentDroppedOutside(draggedInstrument, ControlToScreen(new Point(x, y)));
            //        }
            //    }
            //    else if (captureOperation == CaptureOperation.DragArpeggio)
            //    {
            //        if (inside && button != null && draggedArpeggio != null)
            //        {
            //            Debug.Assert(button.type == ButtonType.Arpeggio || button.type == ButtonType.ArpeggioFolder);

            //            var arpBefore = buttons[buttonIdx].arpeggio;
            //            if (arpBefore != draggedArpeggio)
            //            {
            //                var oldFolder = draggedArpeggio.Folder;
            //                App.UndoRedoManager.BeginTransaction(TransactionScope.ProjectNoDPCMSamples);
            //                App.Project.MoveArpeggio(draggedArpeggio, arpBefore);
            //                draggedArpeggio.FolderName = draggedInFolder == null ? null : draggedInFolder.Name;
            //                if (draggedInFolder != null && !draggedInFolder.Expanded)
            //                {
            //                    draggedInFolder.Expanded = true;
            //                    BlinkButton(draggedArpeggio);
            //                }
            //                if (oldFolder == draggedArpeggio.Folder)
            //                {
            //                    App.Project.AutoSortArpeggios = false;
            //                }
            //                App.Project.ConditionalSortArpeggios();
            //                App.UndoRedoManager.EndTransaction();
            //            }
            //        }
            //        else if (Platform.IsDesktop && !inside)
            //        {
            //            ArpeggioDroppedOutside(draggedArpeggio, ControlToScreen(new Point(x, y)));
            //        }
            //    }
            //    else if (captureOperation == CaptureOperation.DragSample)
            //    {
            //        if (inside && button != null)
            //        {
            //            Debug.Assert(button.type == ButtonType.Dpcm || button.type == ButtonType.DpcmHeader || button.type == ButtonType.DpcmFolder);

            //            var sampleBefore = buttons[buttonIdx].sample;
            //            if (sampleBefore != draggedSample)
            //            {
            //                var oldFolder = draggedSample.Folder;
            //                App.UndoRedoManager.BeginTransaction(TransactionScope.Project);
            //                App.Project.MoveSample(draggedSample, sampleBefore);
            //                draggedSample.FolderName = draggedInFolder == null ? null : draggedInFolder.Name;
            //                if (draggedInFolder != null && !draggedInFolder.Expanded)
            //                {
            //                    draggedInFolder.Expanded = true;
            //                    BlinkButton(draggedSample);
            //                }
            //                if (oldFolder == draggedSample.Folder)
            //                {
            //                    App.Project.AutoSortSamples = false;
            //                }
            //                App.Project.ConditionalSortSamples();
            //                App.UndoRedoManager.EndTransaction();
            //            }
            //        }
            //        else if (Platform.IsDesktop && !inside)
            //        {
            //            var mappingNote = App.GetDPCMSampleMappingNoteAtPos(ControlToScreen(new Point(x, y)), out var instrument);
            //            if (instrument != null)
            //            {
            //                App.UndoRedoManager.BeginTransaction(TransactionScope.Instrument, instrument.Id, -1, TransactionFlags.StopAudio);
            //                instrument.UnmapDPCMSample(mappingNote);
            //                instrument.MapDPCMSample(mappingNote, draggedSample);
            //                App.UndoRedoManager.EndTransaction();
            //                DPCMSampleMapped?.Invoke(draggedSample, ControlToScreen(new Point(x, y)));
            //            }
            //        }
            //    }
            //    else if (captureOperation == CaptureOperation.DragFolder)
            //    {
            //        if (inside && button != null)
            //        {
            //            var folderBefore = buttons[buttonIdx].folder;
            //            if (folderBefore != draggedFolder)
            //            {
            //                App.UndoRedoManager.BeginTransaction(TransactionScope.Project);
            //                App.Project.MoveFolder(draggedFolder, folderBefore);
            //                switch (draggedFolder.Type)
            //                {
            //                    case FolderType.Song:       App.Project.AutoSortSongs       = false; break;
            //                    case FolderType.Instrument: App.Project.AutoSortInstruments = false; break;
            //                    case FolderType.Arpeggio:   App.Project.AutoSortArpeggios   = false; break;
            //                    case FolderType.Sample:     App.Project.AutoSortSamples     = false; break;
            //                }
            //                App.UndoRedoManager.EndTransaction();
            //            }
            //        }
            //    }

            //    RecreateControls();
            //}
            //else
            //{
            //    ScrollIfNearEdge(x, y);
            //    MarkDirty();

            //    if (Platform.IsDesktop && captureOperation == CaptureOperation.DragSample && !ClientRectangle.Contains(x, y))
            //    {
            //        DPCMSampleDraggedOutside?.Invoke(draggedSample, ControlToScreen(new Point(x, y)));
            //    }
            //}
        }

        private void UpdateDragInstrumentEnvelope(int x, int y, bool final)
        {
            // MATTT
            //if (final)
            //{
            //    if (ClientRectangle.Contains(x, y))
            //    {
            //        var buttonIdx = GetButtonAtCoord(x, y, out var subButtonType);
            //        var instrumentSrc = draggedInstrument;
            //        var instrumentDst = buttonIdx >= 0 && buttons[buttonIdx].type == ButtonType.Instrument ? buttons[buttonIdx].instrument : null;

            //        if (instrumentSrc != instrumentDst && instrumentSrc != null && instrumentDst != null && envelopeDragIdx != -1)
            //        {
            //            if (instrumentSrc.Expansion == instrumentDst.Expansion)
            //            {
            //                Platform.MessageBoxAsync(ParentWindow, CopyInstrumentEnvelopeMessage.Format(EnvelopeType.LocalizedNames[envelopeDragIdx], instrumentSrc.Name, instrumentDst.Name), CopyInstrumentEnvelopeTitle, MessageBoxButtons.YesNo, (r) =>
            //                {
            //                    if (r == DialogResult.Yes)
            //                    {
            //                        App.UndoRedoManager.BeginTransaction(TransactionScope.Instrument, instrumentDst.Id);
            //                        instrumentDst.Envelopes[envelopeDragIdx] = instrumentSrc.Envelopes[envelopeDragIdx].ShallowClone();
            //                        instrumentDst.Envelopes[envelopeDragIdx].ClampToValidRange(instrumentDst, envelopeDragIdx);

            //                        // HACK : Copy some envelope related stuff. Need to cleanup the envelope code.
            //                        switch (envelopeDragIdx)
            //                        {
            //                            case EnvelopeType.FdsWaveform:
            //                                instrumentDst.FdsWavePreset = instrumentSrc.FdsWavePreset;
            //                                break;
            //                            case EnvelopeType.FdsModulation:
            //                                instrumentDst.FdsModPreset = instrumentSrc.FdsModPreset;
            //                                break;
            //                            case EnvelopeType.N163Waveform:
            //                                instrumentDst.N163WavePreset = instrumentSrc.N163WavePreset;
            //                                instrumentDst.N163WaveSize = instrumentSrc.N163WaveSize;
            //                                break;
            //                        }

            //                        App.UndoRedoManager.EndTransaction();

            //                        if (Platform.IsDesktop)
            //                            App.StartEditInstrument(instrumentDst, envelopeDragIdx);
            //                    }
            //                });
            //            }
            //            else
            //            {
            //                App.DisplayNotification($"Incompatible audio expansion!"); ;
            //            }
            //        }
            //    }
            //}
            //else
            //{
            //    ScrollIfNearEdge(x, y);
            //    MarkDirty();
            //}
        }

        private void UpdateDragInstrumentSampleMappings(int x, int y, bool final)
        {
            // MATTT
            //if (final)
            //{
            //    if (ClientRectangle.Contains(x, y))
            //    {
            //        var buttonIdx = GetButtonAtCoord(x, y, out var subButtonType);
            //        var instrumentSrc = draggedInstrument;
            //        var instrumentDst = buttonIdx >= 0 && buttons[buttonIdx].type == ButtonType.Instrument ? buttons[buttonIdx].instrument : null;

            //        if (instrumentSrc != instrumentDst && instrumentSrc != null && instrumentDst != null && instrumentDst.Expansion == ExpansionType.None)
            //        {
            //            Platform.MessageBoxAsync(ParentWindow, CopyInstrumentSamplesMessage.Format(instrumentSrc.Name, instrumentDst.Name), CopyInstrumentSamplesTitle, MessageBoxButtons.YesNo, (r) =>
            //            {
            //                if (r == DialogResult.Yes)
            //                {
            //                    App.UndoRedoManager.BeginTransaction(TransactionScope.Instrument, instrumentDst.Id);
            //                    instrumentDst.SamplesMapping.Clear();
            //                    foreach (var mapping in instrumentSrc.SamplesMapping)
            //                        instrumentDst.MapDPCMSample(mapping.Key, mapping.Value.Sample, mapping.Value.Pitch, mapping.Value.Loop);
            //                    App.UndoRedoManager.EndTransaction();

            //                    if (Platform.IsDesktop)
            //                        App.StartEditDPCMMapping(instrumentDst);
            //                }
            //            });
            //        }
            //    }
            //}
            //else
            //{
            //    ScrollIfNearEdge(x, y);
            //    MarkDirty();
            //}
        }

        private void UpdateDragArpeggioValues(int x, int y, bool final)
        {
            // MATTT
            //if (final)
            //{
            //    if (ClientRectangle.Contains(x, y))
            //    {
            //        var buttonIdx = GetButtonAtCoord(x, y, out var subButtonType);

            //        var arpeggioSrc = draggedArpeggio;
            //        var arpeggioDst = buttonIdx >= 0 && buttons[buttonIdx].type == ButtonType.Arpeggio ? buttons[buttonIdx].arpeggio : null;

            //        if (arpeggioSrc != arpeggioDst && arpeggioSrc != null && arpeggioDst != null && envelopeDragIdx != -1)
            //        {
            //            Platform.MessageBoxAsync(ParentWindow, CopyArpeggioMessage.Format(arpeggioSrc.Name, arpeggioDst.Name), CopyArpeggioTitle, MessageBoxButtons.YesNo, (r) =>
            //            {
            //                if (r == DialogResult.Yes)
            //                {
            //                    App.UndoRedoManager.BeginTransaction(TransactionScope.Arpeggio, arpeggioDst.Id);
            //                    arpeggioDst.Envelope.Length = arpeggioSrc.Envelope.Length;
            //                    arpeggioDst.Envelope.Loop = arpeggioSrc.Envelope.Loop;
            //                    Array.Copy(arpeggioSrc.Envelope.Values, arpeggioDst.Envelope.Values, arpeggioDst.Envelope.Values.Length);
            //                    App.UndoRedoManager.EndTransaction();
            //                    if (Platform.IsDesktop)
            //                        App.StartEditArpeggio(arpeggioDst);
            //                }
            //            });
            //        }
            //    }
            //}
            //else
            //{
            //    ScrollIfNearEdge(x, y);
            //    MarkDirty();
            //}
        }

        private void UpdateCaptureOperation(int x, int y, bool realTime = false, float delta = 0.0f)
        {
            // MATTT
            /*
            const int CaptureThreshold = Platform.IsDesktop ? 5 : 50;

            if (captureOperation != CaptureOperation.None && !captureThresholdMet)
            {
                if (Math.Abs(x - captureMouseX) >= CaptureThreshold ||
                    Math.Abs(y - captureMouseY) >= CaptureThreshold)
                {
                    captureThresholdMet = true;
                }
            }

            if (captureOperation != CaptureOperation.None && realTime)
            {
                captureDuration += delta;
            }

            if (captureOperation != CaptureOperation.None && captureThresholdMet && (captureRealTimeUpdate || !realTime))
            {
                switch (captureOperation)
                {
                    case CaptureOperation.MoveSlider:
                        UpdateSlider(x, y, false);
                        break;
                    case CaptureOperation.SliderButtons:
                        UpdateSliderButtons(false, false);
                        break;
                    case CaptureOperation.ScrollBar:
                        UpdateScrollBar(x, y);
                        break;
                    case CaptureOperation.DragInstrumentEnvelope:
                        UpdateDragInstrumentEnvelope(x, y, false);
                        break;
                    case CaptureOperation.DragInstrumentSampleMappings:
                        UpdateDragInstrumentSampleMappings(x, y, false);
                        break;
                    case CaptureOperation.DragArpeggioValues:
                        UpdateDragArpeggioValues(x, y, false);
                        break;
                    case CaptureOperation.DragSong:
                    case CaptureOperation.DragInstrument:
                    case CaptureOperation.DragArpeggio:
                    case CaptureOperation.DragSample:
                    case CaptureOperation.DragFolder:
                        UpdateDrag(x, y, false);
                        break;
                    case CaptureOperation.MobilePan:
                        DoScroll(y - mouseLastY);
                        break;
                    default:
                        MarkDirty();
                        break;
                }
            }
            */

            lastCaptureOperation = CaptureOperation.None;
        }

        protected void ConditionalShowExpansionIcons(int x, int y)
        {
            //mainContainer.GetControlAt();
            //var buttonIdx = GetButtonAtCoord(x, y, out _);
            //App.SequencerShowExpansionIcons = buttonIdx >= 0 && (buttons[buttonIdx].type == ButtonType.Instrument || buttons[buttonIdx].type == ButtonType.InstrumentHeader);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            bool middle = e.Middle || (e.Left && ModifierKeys.IsAltDown && Settings.AltLeftForMiddle);

            UpdateCursor();
            UpdateCaptureOperation(e.X, e.Y);

            if (middle)
                DoScroll(e.Y - mouseLastY);

            ConditionalShowExpansionIcons(e.X, e.Y);

            mouseLastX = e.X;
            mouseLastY = e.Y;
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            App.SequencerShowExpansionIcons = false;
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            bool middle = e.Middle;
            bool doMouseUp = false;

            if (!middle)
            {
                doMouseUp = captureOperation == CaptureOperation.None;
                EndCaptureOperation(e.X, e.Y);
            }

            UpdateCursor();

            // MATTT
            //if (doMouseUp)
            //{
            //    if (HandleMouseUpButtons(e)) goto Handled;
            //    return;
            //Handled:
            //    MarkDirty();
            //}
        }

        private void StartCaptureOperation(int x, int y, CaptureOperation op, int buttonIdx = -1, int buttonRelX = 0, int buttonRelY = 0)
        {
            Debug.Assert(captureOperation == CaptureOperation.None);
            mouseLastX = x;
            mouseLastY = y;
            captureMouseX = x;
            captureMouseY = y;
            captureButtonIdx = buttonIdx;
            captureButtonRelX = buttonRelX;
            captureButtonRelY = buttonRelY;
            captureScrollY = mainContainer.ScrollY;
            Capture = true;
            canFling = false;
            captureOperation = op;
            lastCaptureOperation = CaptureOperation.None;
            captureThresholdMet = !captureNeedsThreshold[(int)op];
            captureRealTimeUpdate = captureWantsRealTimeUpdate[(int)op];
            captureDuration = 0.0f;
        }

        private void EndCaptureOperation(int x, int y)
        {
            // MATTT
            /*
            if (captureOperation != CaptureOperation.None && captureThresholdMet)
            {
                switch (captureOperation)
                {
                    case CaptureOperation.DragInstrumentEnvelope:
                        UpdateDragInstrumentEnvelope(x, y, true);
                        break;
                    case CaptureOperation.DragInstrumentSampleMappings:
                        UpdateDragInstrumentSampleMappings(x, y, true);
                        break;
                    case CaptureOperation.DragArpeggioValues:
                        UpdateDragArpeggioValues(x, y, true);
                        break;
                    case CaptureOperation.MoveSlider:
                        UpdateSlider(x, y, true);
                        break;
                    case CaptureOperation.SliderButtons:
                        UpdateSliderButtons(false, true);
                        break;
                    case CaptureOperation.DragSong:
                    case CaptureOperation.DragInstrument:
                    case CaptureOperation.DragArpeggio:
                    case CaptureOperation.DragSample:
                    case CaptureOperation.DragFolder:
                        UpdateDrag(x, y, true);
                        break;
                    case CaptureOperation.MobilePan:
                        canFling = true;
                        break;
                }
            }

            draggedArpeggio = null;
            draggedInstrument = null;
            draggedSample = null;
            draggedSong = null;
            sliderDragButton = null;
            lastCaptureOperation = captureOperation;
            captureOperation = CaptureOperation.None;
            Capture = false;
            MarkDirty();
            */
        }

        private void AbortCaptureOperation()
        {
            if (App.UndoRedoManager.HasTransactionInProgress)
                App.UndoRedoManager.AbortTransaction();

            MarkDirty();

            draggedArpeggio = null;
            draggedInstrument = null;
            draggedSample = null;
            draggedSong = null;
            sliderDragButton = null;
            captureOperation = CaptureOperation.None;
            Capture = false;
            canFling = false;
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            if (!e.Handled)
            {
                DoScroll(e.ScrollY > 0 ? panelSizeY * 3 : -panelSizeY * 3);
                e.MarkHandled();
            }
        }

        public override void ContainerMouseWheelNotify(Control control, MouseEventArgs e)
        {
            OnMouseWheel(e);
        }

        public override void ContainerMouseMoveNotify(Control control, MouseEventArgs e)
        {
            var winPos = control.ControlToWindow(new Point(e.X, e.Y));
            var ctrl = GetControlAt(winPos.X, winPos.Y, out _, out _);

            if (ctrl != null)
            {
                var tooltip = ctrl.ToolTip;
                if (string.IsNullOrEmpty(tooltip))
                    tooltip = ctrl.ParentContainer.ToolTip;
                App.SetToolTip(tooltip, false);
                App.SequencerShowExpansionIcons = (ctrl.UserData is Instrument) || (ctrl.ParentContainer.UserData is Instrument);
            }
        }

        private void ResizeMainContainer()
        {
            if (mainContainer != null)
            {
                mainContainer.Resize(mainContainer.Width, height - tabPanel.Height);
            }
        }

        protected override void OnResize(EventArgs e)
        {
            ResizeMainContainer();
            UpdateRenderCoords();
            ClampScroll();
        }

        private void ImportSongs()
        {
            Action<string> ImportSongsAction = (filename) =>
            {
                if (filename != null)
                {
                    App.BeginLogTask();
                    App.OpenProjectFileAsync(filename, false, (otherProject) =>
                    {
                        if (otherProject != null)
                        {
                            var songs = new List<Song>();
                            var songsNames = new List<string>();

                            otherProject.SortSongs();

                            foreach (var song in otherProject.GetSongsInFolder(null))
                            {
                                songs.Add(song);
                                songsNames.Add(song.NameWithFolder);
                            }

                            foreach (var folder in otherProject.GetFoldersForType(FolderType.Song))
                            {
                                foreach (var song in otherProject.GetSongsInFolder(folder.Name))
                                {
                                    songs.Add(song);
                                    songsNames.Add(song.NameWithFolder);
                                }
                            }

                            var dlg = new PropertyDialog(ParentWindow, ImportSongsTitle, 300);
                            dlg.Properties.AddLabel(null, ImportSongsLabel.Colon); // 0
                            dlg.Properties.AddCheckBoxList(null, songsNames.ToArray(), null, null, 15); // 1
                            dlg.Properties.AddButton(null, SelectAllLabel); // 2
                            dlg.Properties.AddButton(null, SelectNoneLabel); // 3
                            dlg.Properties.PropertyClicked += ImportSongs_PropertyClicked;
                            dlg.Properties.Build();

                            dlg.ShowDialogAsync((r) =>
                            {
                                if (r == DialogResult.OK)
                                {
                                    App.UndoRedoManager.BeginTransaction(TransactionScope.Project, TransactionFlags.StopAudio);

                                    var selected = dlg.Properties.GetPropertyValue<bool[]>(1);
                                    var songIds = new List<int>();

                                    for (int i = 0; i < selected.Length; i++)
                                    {
                                        if (selected[i])
                                            songIds.Add(songs[i].Id);
                                    }

                                    bool success = false;
                                    if (songIds.Count > 0)
                                    {
                                        otherProject.DeleteAllSongsBut(songIds.ToArray());
                                        success = App.Project.MergeProject(otherProject);
                                    }

                                    App.UndoRedoManager.AbortOrEndTransaction(success);
                                    RecreateAllControls();

                                    if (!success && Platform.IsMobile && Log.GetLastMessage(LogSeverity.Error) != null)
                                    {
                                        Platform.DelayedMessageBoxAsync(Log.GetLastMessage(LogSeverity.Error), ErrorTitle);
                                    }

                                    App.EndLogTask();
                                }
                                else
                                {
                                    App.AbortLogTask();
                                }
                            });
                        }
                        else
                        {
                            App.AbortLogTask();
                        }
                    });
                }
            };

            if (Platform.IsMobile)
            {
                MobileProjectDialog dlg = new MobileProjectDialog(App, ImportSongsTitle, false, false);
                dlg.ShowDialogAsync((f) => ImportSongsAction(f));
            }
            else
            {
                var filename = Platform.ShowOpenFileDialog("Open File", "All Song Files (*.fms;*.txt;*.ftm)|*.fms;*.txt;*.ftm|FamiStudio Files (*.fms)|*.fms|FamiTracker Files (*.ftm)|*.ftm|FamiTracker Text Export (*.txt)|*.txt|FamiStudio Text Export (*.txt)|*.txt", ref Settings.LastInstrumentFolder);
                ImportSongsAction(filename);
            }
        }

        private void SortSongs()
        {
            var scope = !App.Project.AutoSortSongs ? TransactionScope.ProjectNoDPCMSamples : TransactionScope.Application;
            App.UndoRedoManager.BeginTransaction(scope);
            App.Project.AutoSortSongs = !App.Project.AutoSortSongs;
            App.UndoRedoManager.EndTransaction();
            RecreateAllControls();
        }

        private void ImportSongs_PropertyClicked(PropertyPage props, ClickType click, int propIdx, int rowIdx, int colIdx)
        {
            if (click == ClickType.Button)
            {
                var newValues = new bool[props.GetPropertyValue<bool[]>(1).Length];

                if (propIdx == 2)
                {
                    for (int i = 0; i < newValues.Length; i++)
                        newValues[i] = true;
                }

                props.UpdateCheckBoxList(1, newValues);
            }
        }

        private void ImportInstruments()
        {
            Action<string> ImportInstrumentsAction = (filename) =>
            {
                if (filename != null)
                {
                    if (filename.ToLower().EndsWith("fti"))
                    {
                        App.BeginLogTask();
                        App.UndoRedoManager.BeginTransaction(TransactionScope.Project);
                        var success = new FamitrackerInstrumentFile().CreateFromFile(App.Project, filename) != null;
                        App.UndoRedoManager.AbortOrEndTransaction(success);
                        RecreateAllControls();
                        App.EndLogTask();
                    }
                    else if (filename.ToLower().EndsWith("bti"))
                    {
                        App.BeginLogTask();
                        App.UndoRedoManager.BeginTransaction(TransactionScope.Project);
                        var success = new BambootrackerInstrumentFile().CreateFromFile(App.Project, filename) != null;
                        App.UndoRedoManager.AbortOrEndTransaction(success);
                        RecreateAllControls();
                        App.EndLogTask();
                    }
                    else if (filename.ToLower().EndsWith("opni"))
                    {
                        App.BeginLogTask();
                        App.UndoRedoManager.BeginTransaction(TransactionScope.Project);
                        var success = new OPNIInstrumentFile().CreateFromFile(App.Project, filename) != null;
                        App.UndoRedoManager.AbortOrEndTransaction(success);
                        RecreateAllControls();
                        App.EndLogTask();
                    }
                    else
                    {
                        App.BeginLogTask();
                        App.OpenProjectFileAsync(filename, false, (instrumentProject) => 
                        {
                            if (instrumentProject != null)
                            {
                                var instruments = new List<Instrument>();
                                var instrumentNames = new List<string>();

                                instrumentProject.SortInstruments();

                                foreach (var instrument in instrumentProject.GetInstrumentsInFolder(null))
                                {
                                    instruments.Add(instrument);
                                    instrumentNames.Add(instrument.NameWithExpansionAndFolder);
                                }

                                foreach (var folder in instrumentProject.GetFoldersForType(FolderType.Instrument))
                                {
                                    foreach (var instrument in instrumentProject.GetInstrumentsInFolder(folder.Name))
                                    {
                                        instruments.Add(instrument);
                                        instrumentNames.Add(instrument.NameWithExpansionAndFolder);
                                    }
                                }

                                var dlg = new PropertyDialog(ParentWindow, ImportInstrumentsTitle, 300);
                                dlg.Properties.AddLabel(null, ImportInstrumentsLabel.Colon); // 0
                                dlg.Properties.AddCheckBoxList(null, instrumentNames.ToArray(), null, null, 15); // 1
                                dlg.Properties.AddButton(null, SelectAllLabel); // 2
                                dlg.Properties.AddButton(null, SelectNoneLabel); // 3
                                dlg.Properties.Build();
                                dlg.Properties.PropertyClicked += ImportInstrument_PropertyClicked;

                                dlg.ShowDialogAsync((r) =>
                                {
                                    if (r == DialogResult.OK)
                                    {
                                        var selected = dlg.Properties.GetPropertyValue<bool[]>(1);
                                        var instrumentsIdsToMerge = new List<int>();

                                        for (int i = 0; i < selected.Length; i++)
                                        {
                                            if (selected[i])
                                                instrumentsIdsToMerge.Add(instruments[i].Id);
                                        }

                                        // Wipe everything but the instruments we want.
                                        instrumentProject.DeleteAllSongs();
                                        instrumentProject.DeleteAllArpeggios();
                                        instrumentProject.DeleteAllInstrumentBut(instrumentsIdsToMerge.ToArray());
                                        instrumentProject.DeleteUnmappedSamples();

                                        App.UndoRedoManager.BeginTransaction(TransactionScope.Project);
                                        var success = App.Project.MergeProject(instrumentProject);
                                        App.UndoRedoManager.AbortOrEndTransaction(success);
                                        RecreateAllControls();

                                        App.EndLogTask();
                                    }
                                    else
                                    {
                                        App.AbortLogTask();
                                    }
                                });
                            }
                            else
                            {
                                App.AbortLogTask();
                            }
                        });
                    }
                }
            };

            if (Platform.IsMobile)
            {
                MobileProjectDialog dlg = new MobileProjectDialog(App, ImportInstrumentsTitle, false, false);
                dlg.ShowDialogAsync((f) => ImportInstrumentsAction(f));
            }
            else
            {
                var filename = Platform.ShowOpenFileDialog("Open File", "All Instrument Files (*.fti;*.fms;*.txt;*.ftm;*.bti;*.opni)|*.fti;*.fms;*.txt;*.ftm;*.bti;*.opni|FamiTracker Instrument File (*.fti)|*.fti|BambooTracker Instrument File (*.bti)|*.bti|OPN Instrument File (*.opni)|*.opni|FamiStudio Files (*.fms)|*.fms|FamiTracker Files (*.ftm)|*.ftm|FamiTracker Text Export (*.txt)|*.txt|FamiStudio Text Export (*.txt)|*.txt", ref Settings.LastInstrumentFolder);
                ImportInstrumentsAction(filename);
            }
        }

        private void ImportInstrument_PropertyClicked(PropertyPage props, ClickType click, int propIdx, int rowIdx, int colIdx)
        {
            if (click == ClickType.Button)
            {
                var newValues = new bool[props.GetPropertyValue<bool[]>(1).Length];

                if (propIdx == 2)
                {
                    for (int i = 0; i < newValues.Length; i++)
                        newValues[i] = true;
                }

                props.UpdateCheckBoxList(1, newValues);
            }
        }

        private void SortInstruments()
        {
            var scope = !App.Project.AutoSortSongs ? TransactionScope.ProjectNoDPCMSamples : TransactionScope.Application;
            App.UndoRedoManager.BeginTransaction(scope);
            App.Project.AutoSortInstruments = !App.Project.AutoSortInstruments;
            App.UndoRedoManager.EndTransaction();
            RecreateAllControls();
        }

        private void LoadDPCMSample()
        {
            Action<string[]> LoadDPCMSampleAction = (filenames) =>
            {
                if (filenames != null && filenames.Length > 0)
                {
                    var numFamiStudioFiles = 0;
                    var numSamplesFiles = 0;
                    foreach (var fn in filenames)
                    {
                        var ext = Path.GetExtension(fn).ToLower();

                        if (ext == ".fms" && Platform.IsDesktop)
                            numFamiStudioFiles++;
                        else if (ext == ".dmc" || ext == ".wav")
                            numSamplesFiles++;
                    }

                    if (numFamiStudioFiles > 1 || (numFamiStudioFiles == 1 && numSamplesFiles != 0))
                    {
                        Platform.MessageBoxAsync(ParentWindow, "You can only select one FamiStudio project to import samples from.", "Error", MessageBoxButtons.OK);
                        return;
                    }
                    else if (numFamiStudioFiles == 1)
                    {
                        App.BeginLogTask();
                        App.OpenProjectFileAsync(filenames[0], false, (samplesProject) => 
                        {
                            if (samplesProject != null)
                            {
                                if (samplesProject.Samples.Count == 0)
                                {
                                    Platform.MessageBox(ParentWindow, "The selected project does not contain any samples.", "Error", MessageBoxButtons.OK);
                                    return;
                                }

                                var samples = new List<DPCMSample>();
                                var samplesNames = new List<string>();

                                samplesProject.SortSamples();

                                foreach (var sample in samplesProject.GetSamplesInFolder(null))
                                {
                                    samples.Add(sample);
                                    samplesNames.Add(sample.NameWithFolder);
                                }

                                foreach (var folder in samplesProject.GetFoldersForType(FolderType.Sample))
                                {
                                    foreach (var sample in samplesProject.GetSamplesInFolder(folder.Name))
                                    {
                                        samples.Add(sample);
                                        samplesNames.Add(sample.NameWithFolder);
                                    }
                                }

                                var dlg = new PropertyDialog(ParentWindow, ImportSamplesTitle, 300);
                                dlg.Properties.AddLabel(null, ImportSamplesLabel.Colon); // 0
                                dlg.Properties.AddCheckBoxList(null, samplesNames.ToArray(), null, null, 15); // 1
                                dlg.Properties.AddButton(null, SelectAllLabel); // 2
                                dlg.Properties.AddButton(null, SelectNoneLabel); // 3
                                dlg.Properties.Build();
                                dlg.Properties.PropertyClicked += ImportInstrument_PropertyClicked;

                                dlg.ShowDialogAsync((r) =>
                                {
                                    if (r == DialogResult.OK)
                                    {
                                        var selected = dlg.Properties.GetPropertyValue<bool[]>(1);
                                        var sampleIdsToMerge = new List<int>();

                                        for (int i = 0; i < selected.Length; i++)
                                        {
                                            if (selected[i])
                                                sampleIdsToMerge.Add(samples[i].Id);
                                        }

                                        // Wipe everything but the instruments we want.
                                        samplesProject.DeleteAllSongs();
                                        samplesProject.DeleteAllArpeggios();
                                        samplesProject.DeleteAllSamplesBut(sampleIdsToMerge.ToArray());
                                        samplesProject.DeleteAllInstruments();

                                        App.UndoRedoManager.BeginTransaction(TransactionScope.Project);
                                        bool success = App.Project.MergeProject(samplesProject);
                                        App.UndoRedoManager.AbortOrEndTransaction(success);

                                        RecreateAllControls();
                                        App.EndLogTask();
                                    }
                                    else
                                    {
                                        App.AbortLogTask();
                                    }
                                });
                            }
                            else
                            {
                                App.AbortLogTask();
                            }
                        });
                    }
                    else if (numSamplesFiles > 0)
                    {
                        App.BeginLogTask();
                        {
                            App.UndoRedoManager.BeginTransaction(TransactionScope.Project);

                            var importedSamples = new List<DPCMSample>();

                            foreach (var filename in filenames)
                            {
                                var sampleName = Path.GetFileNameWithoutExtension(filename);
                                if (sampleName.Length > 16)
                                    sampleName = sampleName.Substring(0, 16);
                                sampleName = App.Project.GenerateUniqueDPCMSampleName(sampleName);

                                if (Path.GetExtension(filename).ToLower() == ".wav")
                                {
                                    var wavData = WaveFile.Load(filename, out var sampleRate);
                                    if (wavData != null)
                                    {
                                        var maximumSamples = sampleRate * 2;
                                        if (wavData.Length > maximumSamples)
                                        {
                                            Array.Resize(ref wavData, maximumSamples);
                                            Log.LogMessage(LogSeverity.Warning, MaxWavFileWarning.Format(2));
                                        }

                                        var sample = App.Project.CreateDPCMSampleFromWavData(sampleName, wavData, sampleRate, filename);
                                        importedSamples.Add(sample);
                                    }
                                }
                                else if (Path.GetExtension(filename).ToLower() == ".dmc")
                                {
                                    var dmcData = File.ReadAllBytes(filename);
                                    if (dmcData.Length > DPCMSample.MaxSampleSize)
                                    {
                                        Array.Resize(ref dmcData, DPCMSample.MaxSampleSize);
                                        Log.LogMessage(LogSeverity.Warning, MaxDmcSizeWarning.Format(DPCMSample.MaxSampleSize));
                                    }
                                    var sample = App.Project.CreateDPCMSampleFromDmcData(sampleName, dmcData, filename);
                                    importedSamples.Add(sample);
                                }
                            }

                            App.UndoRedoManager.EndTransaction();
                            RecreateAllControls();
                            if (importedSamples.Count != 0)
                                BlinkButton(importedSamples[0]);
                        }
                        App.EndLogTask();
                    }
                }
            };

            if (Platform.IsMobile)
            {
                Platform.StartMobileLoadFileOperationAsync("*/*", (f) => LoadDPCMSampleAction(new[] { f }));
            }
            else
            {
                var filenames = Platform.ShowOpenFileDialog("Open File", "All Sample Files (*.wav;*.dmc;*.fms)|*.wav;*.dmc;*.fms|Wav Files (*.wav)|*.wav|DPCM Sample Files (*.dmc)|*.dmc|FamiStudio Files (*.fms)|*.fms", ref Settings.LastSampleFolder, true);
                LoadDPCMSampleAction(filenames);
            }
        }

        private void AddSong()
        {
            var folder = App.SelectedSong != null ? App.SelectedSong.FolderName : null;
            App.UndoRedoManager.BeginTransaction(TransactionScope.ProjectNoDPCMSamples, TransactionFlags.StopAudio);
            App.SelectedSong = App.Project.CreateSong();
            App.SelectedSong.FolderName = folder;
            App.UndoRedoManager.EndTransaction();
            RecreateAllControls();
            BlinkButton(App.SelectedSong);
        }

        private void AskAddSong(int x, int y)
        {
            var options = new List<ContextMenuOption>
            {
                new ContextMenuOption("Music", AddSongContext, () => { AddSong(); }),
                new ContextMenuOption("Folder", AddFolderContext, () => { AddFolder(FolderType.Song); }, ContextMenuSeparator.Before)
            };

            App.ShowContextMenu(left + x, top + y, options.ToArray());
        }

        private void AskDeleteSong(Song song)
        {
            Platform.MessageBoxAsync(ParentWindow, AskDeleteSongMessage.Format(song.Name), AskDeleteSongTitle, MessageBoxButtons.YesNo, (r) =>
            {
                if (r == DialogResult.Yes)
                {
                    bool selectNewSong = song == App.SelectedSong;
                    App.UndoRedoManager.BeginTransaction(TransactionScope.ProjectNoDPCMSamples, TransactionFlags.StopAudio);
                    App.Project.DeleteSong(song);
                    if (selectNewSong)
                        App.SelectedSong = App.Project.Songs[0];
                    App.UndoRedoManager.EndTransaction();
                    RecreateAllControls();
                }
            });
        }

        private void AddInstrument(int expansionType)
        {
            var folder = App.SelectedInstrument != null ? App.SelectedInstrument.FolderName : null;
            App.UndoRedoManager.BeginTransaction(TransactionScope.ProjectNoDPCMSamples);
            App.SelectedInstrument = App.Project.CreateInstrument(expansionType);
            App.SelectedInstrument.FolderName = folder;
            App.UndoRedoManager.EndTransaction();
            RecreateAllControls();
            BlinkButton(App.SelectedInstrument);
        }

        private void AddFolder(int type)
        {
            App.UndoRedoManager.BeginTransaction(type == FolderType.Sample ? TransactionScope.Project : TransactionScope.ProjectNoDPCMSamples);
            var folder = App.Project.CreateFolder(type);
            App.UndoRedoManager.EndTransaction();
            RecreateAllControls();
            BlinkButton(folder);
        }

        private void AskAddInstrument()
        {
            var pt = ParentWindow.LastMousePosition;
            var activeExpansions = App.Project.GetActiveExpansions();
            var options = new List<ContextMenuOption>
            {
                new ContextMenuOption(ExpansionType.Icons[0], AddRegularInstrumentContext, () => { AddInstrument(ExpansionType.None); })
            };

            for (int i = 1; i < activeExpansions.Length; i++)
            {
                if (ExpansionType.NeedsExpansionInstrument(activeExpansions[i]))
                {
                    var j = i; // Important, copy for lambda.
                    var expName = ExpansionType.GetLocalizedName(activeExpansions[i], ExpansionType.LocalizationMode.Instrument);
                    options.Add(new ContextMenuOption(ExpansionType.Icons[activeExpansions[i]], AddExpInstrumentContext.Format(expName), () => { AddInstrument(activeExpansions[j]); }));
                }
            }

            options.Add(new ContextMenuOption("Folder", AddFolderContext, () => { AddFolder(FolderType.Instrument); }, ContextMenuSeparator.Before));

            App.ShowContextMenu(pt.X, pt.Y, options.ToArray());
        }

        private void ToggleExpandInstrument(Instrument inst)
        {
            expandedInstrument = expandedInstrument == inst ? null : inst;
            selectedInstrumentTab = null;
            expandedSample = null;
            RecreateAllControls();
        }

        private void ToggleExpandFolder(Folder folder)
        {
            folder.Expanded = !folder.Expanded;
            RecreateAllControls();
        }

        private void AskDeleteInstrument(Instrument inst)
        {
            Platform.MessageBoxAsync(ParentWindow, AskDeleteInstrumentMessage.Format(inst.Name), AskDeleteInstrumentTitle, MessageBoxButtons.YesNo, (r) =>
            {
                if (r == DialogResult.Yes)
                {
                    bool selectNewInstrument = inst == App.SelectedInstrument;
                    App.UndoRedoManager.BeginTransaction(TransactionScope.ProjectNoDPCMSamples, TransactionFlags.StopAudio);
                    App.Project.DeleteInstrument(inst);
                    if (selectNewInstrument)
                        App.SelectedInstrument = App.Project.Instruments.Count > 0 ? App.Project.Instruments[0] : null;
                    InstrumentDeleted?.Invoke(inst);
                    App.UndoRedoManager.EndTransaction();
                    RecreateAllControls();
                }
            });
        }

        private GradientPanel FindInstrumentPanel(Instrument inst)
        {
            return mainContainer.FindControlByUserData(inst) as GradientPanel;
        }

        private Button FindInstrumentEnvelopeButton(Instrument inst, Envelope env)
        {
            return FindInstrumentPanel(inst).FindControlByUserData(env) as Button;
        }

        private void ClearInstrumentEnvelope(Instrument inst, int envelopeType)
        {
            var env = inst.Envelopes[envelopeType];
            App.UndoRedoManager.BeginTransaction(TransactionScope.Instrument, inst.Id);
            env.ResetToDefault(envelopeType);
            inst.NotifyEnvelopeChanged(envelopeType, true);
            App.UndoRedoManager.EndTransaction();
            FindInstrumentEnvelopeButton(inst, env).Dimmed = env.IsEmpty(envelopeType);
            MarkDirty();
        }

        private void AddArpeggio()
        {
            var folder = App.SelectedArpeggio != null ? App.SelectedArpeggio.FolderName : null;
            App.UndoRedoManager.BeginTransaction(TransactionScope.ProjectNoDPCMSamples);
            App.SelectedArpeggio = App.Project.CreateArpeggio();
            App.SelectedArpeggio.FolderName = folder;
            App.UndoRedoManager.EndTransaction();
            RecreateAllControls();
            BlinkButton(App.SelectedArpeggio);
        }

        private void AskAddArpeggio(int x, int y)
        {
            var options = new List<ContextMenuOption>
            {
                new ContextMenuOption("Music", AddArpeggioContext, () => { AddArpeggio(); }),
                new ContextMenuOption("Folder", AddFolderContext, () => { AddFolder(FolderType.Arpeggio); }, ContextMenuSeparator.Before)
            };

            App.ShowContextMenu(left + x, top + y, options.ToArray());
        }

        private void AskDeleteArpeggio(Arpeggio arpeggio)
        {
            Platform.MessageBoxAsync(ParentWindow, AskDeleteArpeggioMessage.Format(arpeggio.Name), AskDeleteArpeggioTitle, MessageBoxButtons.YesNo, (r) =>
            {
                if (r == DialogResult.Yes)
                {
                    bool selectNewArpeggio = arpeggio == App.SelectedArpeggio;
                    App.UndoRedoManager.BeginTransaction(TransactionScope.ProjectNoDPCMSamples, TransactionFlags.StopAudio);
                    App.Project.DeleteArpeggio(arpeggio);
                    if (selectNewArpeggio)
                        App.SelectedArpeggio = App.Project.Arpeggios.Count > 0 ? App.Project.Arpeggios[0] : null;
                    ArpeggioDeleted?.Invoke(arpeggio);
                    App.UndoRedoManager.EndTransaction();
                    RecreateAllControls();
                }
            });
        }

        private void SortArpeggios()
        {
            var scope = !App.Project.AutoSortSongs ? TransactionScope.ProjectNoDPCMSamples : TransactionScope.Application;
            App.UndoRedoManager.BeginTransaction(TransactionScope.ProjectNoDPCMSamples);
            App.Project.AutoSortArpeggios = !App.Project.AutoSortArpeggios;
            App.UndoRedoManager.EndTransaction();
            RecreateAllControls();
        }

        private void AskAddSampleFolder(int x, int y)
        {
            var options = new List<ContextMenuOption>
            {
                new ContextMenuOption("Folder", AddFolderContext, () => { AddFolder(FolderType.Sample); }, ContextMenuSeparator.Before)
            };

            App.ShowContextMenu(left + x, top + y, options.ToArray());
        }

        private void ReloadDPCMSampleSourceData(DPCMSample sample)
        {
            if (!string.IsNullOrEmpty(sample.SourceFilename))
            {
                if (File.Exists(sample.SourceFilename))
                {
                    if (sample.SourceDataIsWav)
                    {
                        var wavData = WaveFile.Load(sample.SourceFilename, out var sampleRate);
                        if (wavData != null)
                        {
                            var maximumSamples = sampleRate * 2;
                            if (wavData.Length > maximumSamples)
                                Array.Resize(ref wavData, maximumSamples);

                            App.UndoRedoManager.BeginTransaction(TransactionScope.DPCMSample, sample.Id);
                            sample.SetWavSourceData(wavData, sampleRate, sample.SourceFilename, false);
                            sample.Process();
                            App.UndoRedoManager.EndTransaction();
                        }
                    }
                    else
                    {
                        var dmcData = File.ReadAllBytes(sample.SourceFilename);
                        if (dmcData.Length > DPCMSample.MaxSampleSize)
                            Array.Resize(ref dmcData, DPCMSample.MaxSampleSize);

                        App.UndoRedoManager.BeginTransaction(TransactionScope.DPCMSample, sample.Id);
                        sample.SetDmcSourceData(dmcData, sample.SourceFilename, false);
                        sample.Process();
                        App.UndoRedoManager.EndTransaction();
                    }

                    DPCMSampleReloaded?.Invoke(sample);
                }
                else
                {
                    App.DisplayNotification(CantFindSourceFileError.Format(sample.SourceFilename));
                }
            }
        }

        private void ExportDPCMSampleProcessedData(DPCMSample sample)
        {
            var filename = Platform.ShowSaveFileDialog("Save File", "DPCM Samples (*.dmc)|*.dmc", ref Settings.LastSampleFolder);
            if (filename != null)
                File.WriteAllBytes(filename, sample.ProcessedData);
        }

        private void ExportDPCMSampleSourceData(DPCMSample sample)
        {
            if (sample.SourceDataIsWav)
            {
                var filename = Platform.ShowSaveFileDialog("Save File", "Wav file (*.wav)|*.wav", ref Settings.LastSampleFolder);
                if (filename != null)
                    WaveFile.Save(sample.SourceWavData.Samples, filename, sample.SourceWavData.SampleRate, 1);
            }
            else
            {
                var filename = Platform.ShowSaveFileDialog("Save File", "DPCM Samples (*.dmc)|*.dmc", ref Settings.LastSampleFolder);
                if (filename != null)
                    File.WriteAllBytes(filename, sample.SourceDmcData.Data);
            }
        }

        private void SortSamples()
        {
            var scope = !App.Project.AutoSortSongs ? TransactionScope.Project : TransactionScope.Application;
            App.UndoRedoManager.BeginTransaction(scope);
            App.Project.AutoSortSamples = !App.Project.AutoSortSamples;
            App.UndoRedoManager.EndTransaction();
            RecreateAllControls();
        }

        private void AutoAssignSampleBanks()
        {
            var dlg = new PropertyDialog(ParentWindow, AutoAssignBanksTitle, 250, true, true);
            dlg.Properties.AddLabel(null, TargetBankSizeLabel.Colon); // 0
            dlg.Properties.AddDropDownList(null, new[] { "4KB", "8KB", "16KB" }, "4KB", null); // 1
            dlg.Properties.Build();

            dlg.ShowDialogAsync((r) =>
            {
                if (r == DialogResult.OK)
                {
                    App.UndoRedoManager.BeginTransaction(TransactionScope.Project);
                    var bankSize = Utils.ParseIntWithTrailingGarbage(dlg.Properties.GetPropertyValue<string>(1)) * 1024;
                    App.Project.AutoAssignSamplesBanks(bankSize, out _);
                    App.UndoRedoManager.EndTransaction();
                    MarkDirty();
                }
            });
        }

        private void ToggleExpandDPCMSample(DPCMSample sample)
        {
            expandedSample = expandedSample == sample ? null : sample;
            expandedInstrument = null;
            selectedInstrumentTab = null;
            RecreateAllControls();
        }

        private void AskDeleteDPCMSample(DPCMSample sample)
        {
            Platform.MessageBoxAsync(ParentWindow, AskDeleteSampleMessage.Format(sample.Name), AskDeleteSampleTitle, MessageBoxButtons.YesNo, (r) =>
            {
                if (r == DialogResult.Yes)
                {
                    App.UndoRedoManager.BeginTransaction(TransactionScope.Project, TransactionFlags.StopAudio);
                    App.Project.DeleteSample(sample);
                    DPCMSampleDeleted?.Invoke(sample);
                    App.UndoRedoManager.EndTransaction();
                    RecreateAllControls();
                }
            });
        }

        private bool HandleMouseDownPan(MouseEventArgs e)
        {
            bool middle = e.Middle || (e.Left && ModifierKeys.IsAltDown && Settings.AltLeftForMiddle);

            if (middle)
            {
                mouseLastY = e.Y;
                return true;
            }

            return false;
        }

        private bool HandleMouseDownSongButton(MouseEventArgs e, ProjectExplorerButton button, int buttonIdx, SubButtonType subButtonType)
        {
            // MATTT
            //if (e.Left && subButtonType == SubButtonType.Properties)
            //{
            //    EditSongProperties(new Point(e.X, e.Y), button.song);
            //}
            //else if (e.Left && subButtonType == SubButtonType.Max)
            //{
            //    App.SelectedSong = button.song;
            //    StartCaptureOperation(e.X, e.Y, CaptureOperation.DragSong, buttonIdx);
            //    draggedSong = button.song;
            //}

            return true;
        }

        private bool HandleMouseDownInstrumentButton(MouseEventArgs e, ProjectExplorerButton button, SubButtonType subButtonType, int buttonIdx, int buttonRelX, int buttonRelY)
        {
            // MATTT
            //if (e.Left)
            //{
            //    if (subButtonType == SubButtonType.Expand)
            //    {
            //        ToggleExpandInstrument(button.instrument);
            //        return true;
            //    }
            //    else if (subButtonType == SubButtonType.DPCM)
            //    {
            //        draggedInstrument = button.instrument;
            //        StartCaptureOperation(e.X, e.Y, CaptureOperation.DragInstrumentSampleMappings, buttonIdx, buttonRelX, buttonRelY);
            //        App.StartEditDPCMMapping(button.instrument);
            //        return true;
            //    }
            //    else if (subButtonType == SubButtonType.Properties)
            //    {
            //        EditInstrumentProperties(new Point(e.X, e.Y), button.instrument);
            //        return true;
            //    }

            //    App.SelectedInstrument = button.instrument;

            //    if (button.instrument != null)
            //    {
            //        draggedInstrument = button.instrument;

            //        if (subButtonType < SubButtonType.EnvelopeMax)
            //        {
            //            envelopeDragIdx = (int)subButtonType;
            //            StartCaptureOperation(e.X, e.Y, CaptureOperation.DragInstrumentEnvelope, buttonIdx, buttonRelX, buttonRelY);
            //            App.StartEditInstrument(button.instrument, (int)subButtonType);
            //        }
            //        else
            //        {
            //            envelopeDragIdx = -1;
            //            StartCaptureOperation(e.X, e.Y, CaptureOperation.DragInstrument, buttonIdx);
            //        }
            //    }
            //}

            return true;
        }

        private bool HandleMouseDownFolderButton(MouseEventArgs e, ProjectExplorerButton button, SubButtonType subButtonType, int buttonIdx)
        {
            // MATTT
            //if (e.Left)
            //{
            //    if (subButtonType == SubButtonType.Expand)
            //    {
            //        ToggleExpandFolder(button.folder);
            //    }
            //    else if (subButtonType == SubButtonType.Properties)
            //    {
            //        EditFolderProperties(new Point(e.X, e.Y), button.folder);
            //    }
            //    else if (subButtonType == SubButtonType.Max)
            //    {
            //        draggedFolder = button.folder;
            //        StartCaptureOperation(e.X, e.Y, CaptureOperation.DragFolder, buttonIdx);
            //    }
            //}
            
            return true;
        }

        private bool HandleMouseDownArpeggioButton(MouseEventArgs e, ProjectExplorerButton button, SubButtonType subButtonType, int buttonIdx, int buttonRelX, int buttonRelY)
        {
            // MATTT
            //if (e.Left)
            //{
            //    if (subButtonType == SubButtonType.Properties)
            //    {
            //        EditArpeggioProperties(new Point(e.X, e.Y), button.arpeggio);
            //        return true;
            //    }

            //    App.SelectedArpeggio = button.arpeggio;

            //    envelopeDragIdx = -1;
            //    draggedArpeggio = button.arpeggio;

            //    if (subButtonType < SubButtonType.EnvelopeMax)
            //    {
            //        envelopeDragIdx = (int)subButtonType;
            //        StartCaptureOperation(e.X, e.Y, CaptureOperation.DragArpeggioValues, buttonIdx, buttonRelX, buttonRelY);
            //        App.StartEditArpeggio(button.arpeggio);
            //    }
            //    else
            //    {
            //        StartCaptureOperation(e.X, e.Y, CaptureOperation.DragArpeggio, buttonIdx);
            //    }
            //}

            return true;
        }

        private bool HandleMouseDownDpcmButton(MouseEventArgs e, ProjectExplorerButton button, SubButtonType subButtonType, int buttonIdx)
        {
            // MATTT
            //if (e.Left)
            //{
            //    if (subButtonType == SubButtonType.EditWave)
            //    {
            //        App.StartEditDPCMSample(button.sample);
            //    }
            //    else if (subButtonType == SubButtonType.Reload)
            //    {
            //        ReloadDPCMSampleSourceData(button.sample);
            //    }
            //    else if (subButtonType == SubButtonType.Play)
            //    {
            //        App.PreviewDPCMSample(button.sample, false);
            //    }
            //    else if (subButtonType == SubButtonType.Properties)
            //    {
            //        EditDPCMSampleProperties(new Point(e.X, e.Y), button.sample);
            //    }
            //    else if (subButtonType == SubButtonType.Expand)
            //    {
            //        ToggleExpandDPCMSample(button.sample);
            //    }
            //    else if (subButtonType == SubButtonType.Max)
            //    {
            //        draggedSample = button.sample;
            //        StartCaptureOperation(e.X, e.Y, CaptureOperation.DragSample, buttonIdx);
            //        MarkDirty();
            //    }
            //}
            //else if (e.Right)
            //{
            //    if (subButtonType == SubButtonType.Play)
            //    {
            //        App.PreviewDPCMSample(button.sample, true);
            //    }
            //}

            return true;
        }

        private void DuplicateSong(Song s)
        {
            App.UndoRedoManager.BeginTransaction(TransactionScope.ProjectNoDPCMSamples);
            var newSong = s.Project.DuplicateSong(s);
            RecreateAllControls();
            BlinkButton(newSong);
            App.UndoRedoManager.EndTransaction();
        }

        private void DuplicateInstrument(Instrument inst)
        {
            App.UndoRedoManager.BeginTransaction(TransactionScope.ProjectNoDPCMSamples);
            var newInst = App.Project.DuplicateInstrument(inst);
            RecreateAllControls();
            BlinkButton(newInst);
            App.UndoRedoManager.EndTransaction();
        }

        private void AskReplaceInstrument(Instrument inst)
        {
            var instrumentNames  = new List<string>();
            var instrumentColors = new List<Color>();

            foreach (var i in App.Project.Instruments)
            {
                if (i.Expansion == inst.Expansion && i != inst)
                {
                    instrumentNames.Add(i.Name);
                    instrumentColors.Add(i.Color);
                } 
            }

            if (instrumentNames.Count > 0)
            {                               
                var dlg = new PropertyDialog(ParentWindow, AskReplaceInstrumentTitle, 250, true, true);
                dlg.Properties.AddLabel(null, AskReplaceInstrumentMessage.Format(inst.Name), true); // 0
                dlg.Properties.AddRadioButtonList(null, instrumentNames.ToArray(), 0, null, 12); // 1
                dlg.Properties.Build();

                for (int i = 0; i < instrumentColors.Count; i++)
                    dlg.Properties.SetRowColor(1, i, instrumentColors[i]);

                dlg.ShowDialogAsync((r) =>
                {
                    if (r == DialogResult.OK)
                    {
                        App.UndoRedoManager.BeginTransaction(TransactionScope.ProjectNoDPCMSamples);
                        App.Project.ReplaceInstrument(inst, App.Project.GetInstrument(instrumentNames[dlg.Properties.GetSelectedIndex(1)]));
                        App.UndoRedoManager.EndTransaction();
                        RecreateAllControls();
                        InstrumentReplaced?.Invoke(inst);
                    }
                });
            }
        }

        private void DuplicateConvertInstrument(Instrument instrument, int exp)
        {
            App.UndoRedoManager.BeginTransaction(TransactionScope.Project);
            var newInstrument = App.Project.DuplicateConvertInstrument(instrument, exp);
            App.UndoRedoManager.EndTransaction();
            RecreateAllControls();
            BlinkButton(newInstrument);
        }

        private void LoadN163FdsResampleWavFile(Instrument inst)
        {
            Action<string> LoadWavFileAction = (filename) =>
            {
                if (filename != null)
                {
                    var wav = WaveFile.Load(filename, out _);
                    if (wav != null)
                    {
                        App.BeginLogTask();
                        App.UndoRedoManager.BeginTransaction(TransactionScope.Instrument, inst.Id);

                        if (wav.Length > Instrument.MaxResampleWavSamples)
                        {
                            Array.Resize(ref wav, Instrument.MaxResampleWavSamples);
                            Log.LogMessage(LogSeverity.Warning, MaxWavN163Duration.Format(Instrument.MaxResampleWavSamples));
                        }

                        if (inst.IsN163)
                            inst.SetN163ResampleWaveData(wav);
                        else
                            inst.SetFdsResampleWaveData(wav);

                        App.UndoRedoManager.EndTransaction();
                        App.EndLogTask();

                        MarkDirty();
                    }
                }
            };

            if (Platform.IsMobile)
            {
                Platform.StartMobileLoadFileOperationAsync("*/*", (f) => LoadWavFileAction(f));
            }
            else
            {
                var filename = Platform.ShowOpenFileDialog("Open File", "Wav Files (*.wav)|*.wav", ref Settings.LastSampleFolder);
                LoadWavFileAction(filename);
            }
        }

        private void ClearN163FdsResampleWavData(Instrument inst)
        {
            App.UndoRedoManager.BeginTransaction(TransactionScope.Instrument, inst.Id);

            if (inst.IsN163)
                inst.DeleteN163ResampleWavData();
            else
                inst.DeleteFdsResampleWavData();

            App.UndoRedoManager.EndTransaction();
            MarkDirty();
        }

        private void CopyRegisterValues(Instrument inst)
        {
            Debug.Assert(inst.IsVrc7 || inst.IsEpsm);

            var regs = inst.IsVrc7 ? inst.Vrc7PatchRegs : inst.EpsmPatchRegs;            
            var str = $"{regs[0]:x2}";
            for (var i = 1; i < regs.Length; i++)
                str += $" {regs[i]:x2}";

            Platform.SetClipboardString(str);
        }

        private void PasteRegisterValues(Instrument inst)
        {
            var str = Platform.GetClipboardString();

            if (string.IsNullOrEmpty(str))
            {
                App.DisplayNotification(ClipboardNoValidTextError);
                return;
            }

            var splits = str.Split(new[] { ' ' });
            var regs = inst.IsVrc7 ? inst.Vrc7PatchRegs : inst.EpsmPatchRegs;

            if (splits.Length != regs.Length)
            {
                App.DisplayNotification(ClipboardInvalidNumberRegisters);
                return;
            }

            App.UndoRedoManager.BeginTransaction(TransactionScope.Instrument, inst.Id);

            for (int i = 0; i < splits.Length; i++)
            {
                try { regs[i] = (byte)Convert.ToInt32(splits[i], 16); } catch { }
            }

            if (inst.IsVrc7)
                inst.Vrc7Patch = 0;
            else
                inst.EpsmPatch = 0;

            App.UndoRedoManager.EndTransaction();
        }

        private void DuplicateArpeggio(Arpeggio arp)
        {
            App.UndoRedoManager.BeginTransaction(TransactionScope.ProjectNoDPCMSamples);
            var newArp = App.Project.DuplicateArpeggio(arp);
            RecreateAllControls();
            BlinkButton(newArp);
            App.UndoRedoManager.EndTransaction();
        }

        private void AskReplaceArpeggio(Arpeggio arp)
        {
            var arpeggioNames  = new List<string>();
            var arpeggioColors = new List<Color>();

            foreach (var a in App.Project.Arpeggios)
            {
                if (a != arp)
                {
                    arpeggioNames.Add(a.Name);
                    arpeggioColors.Add(a.Color);
                }
            }

            if (arpeggioNames.Count > 0)
            {
                var dlg = new PropertyDialog(ParentWindow, AskReplaceArpeggioTitle, 250, true, true);
                dlg.Properties.AddLabel(null, AskReplaceArpeggioMessage.Format(arp.Name), true); // 0
                dlg.Properties.AddRadioButtonList(null, arpeggioNames.ToArray(), 0, null, 12); // 1
                dlg.Properties.Build();

                for (int i = 0; i < arpeggioColors.Count; i++)
                    dlg.Properties.SetRowColor(1, i, arpeggioColors[i]);

                dlg.ShowDialogAsync((r) =>
                {
                    if (r == DialogResult.OK)
                    {
                        App.UndoRedoManager.BeginTransaction(TransactionScope.ProjectNoDPCMSamples);
                        App.Project.ReplaceArpeggio(arp, App.Project.GetArpeggio(arpeggioNames[dlg.Properties.GetSelectedIndex(1)]));
                        App.UndoRedoManager.EndTransaction();
                        RecreateAllControls();
                        InstrumentReplaced?.Invoke(null);
                    }
                });
            }
        }

        private void DeleteDpcmSourceWavData(DPCMSample sample)
        {
            App.UndoRedoManager.BeginTransaction(TransactionScope.Project);
            sample.PermanentlyApplyAllProcessing();
            App.UndoRedoManager.EndTransaction();
        }

        private bool HandleContextMenuDpcmButton(int x, int y, ProjectExplorerButton button, SubButtonType subButtonType, int buttonIdx)
        {
            // MATTT
            //if (subButtonType != SubButtonType.Max)
            //    return true;

            //var menu = new List<ContextMenuOption>();

            //menu.Add(new ContextMenuOption("MenuDelete", DeleteSampleContext, () => { AskDeleteDPCMSample(button.sample); }, ContextMenuSeparator.After));

            //if (Platform.IsDesktop)
            //{
            //    menu.Add(new ContextMenuOption("MenuSave", ExportProcessedDmcDataContext, () => { ExportDPCMSampleProcessedData(button.sample); }));
            //    menu.Add(new ContextMenuOption("MenuSave", ExportSourceDataContext, () => { ExportDPCMSampleSourceData(button.sample); }));
            //}

            //if (button.sample.SourceDataIsWav)
            //{
            //    menu.Add(new ContextMenuOption("MenuTrash", DiscardSourceWavDataContext, DiscardSourceWavDataTooltip, () => { DeleteDpcmSourceWavData(button.sample); }));
            //}

            //menu.Add(new ContextMenuOption("MenuBankAssign", AutoAssignBanksContext, () => { AutoAssignSampleBanks(); }, ContextMenuSeparator.Before));
            //menu.Add(new ContextMenuOption("MenuProperties", PropertiesSamplesContext, () => { EditDPCMSampleProperties(new Point(x, y), button.sample); }, ContextMenuSeparator.Before));

            //App.ShowContextMenu(left + x, top + y, menu.ToArray());

            return true;
        }

        private void ExpandAllFolders(int type, bool expand)
        {
            App.Project.ExpandAllFolders(type, expand);
            RecreateAllControls();
        }

        private void AskDeleteFolder(Folder folder)
        {
            Platform.MessageBoxAsync(ParentWindow, AskDeleteFolderMessage.Format(folder.Name), AskDeleteFolderTitle, MessageBoxButtons.YesNoCancel, (r) =>
            {
                if (r != DialogResult.Cancel)
                {
                    App.UndoRedoManager.BeginTransaction(folder.Type == FolderType.Sample ? TransactionScope.ProjectNoDPCMSamples : TransactionScope.Project, r == DialogResult.Yes ? TransactionFlags.StopAudio : TransactionFlags.None);

                    if (r == DialogResult.Yes)
                    {
                        switch (folder.Type)
                        {
                            case FolderType.Song:
                                App.Project.GetSongsInFolder(folder.Name).ForEach(s => { if (App.Project.Songs.Count > 1) { App.Project.DeleteSong(s); } });
                                App.Project.GetSongsInFolder(folder.Name).ForEach(s => s.FolderName = null);
                                if (App.SelectedSong == null || !App.Project.SongExists(App.SelectedSong))
                                    App.SelectedSong = App.Project.Songs[0];
                                break;
                            case FolderType.Instrument:
                                App.Project.GetInstrumentsInFolder(folder.Name).ForEach(i => { App.Project.DeleteInstrument(i); InstrumentDeleted?.Invoke(i); });
                                if (App.Project.Instruments.Count > 0 && (App.SelectedInstrument == null || !App.Project.InstrumentExists(App.SelectedInstrument)))
                                    App.SelectedInstrument = App.Project.Instruments[0];
                                else
                                    App.SelectedInstrument = null;
                                break;
                            case FolderType.Arpeggio:
                                App.Project.GetArpeggiosInFolder(folder.Name).ForEach(a => { App.Project.DeleteArpeggio(a); ArpeggioDeleted?.Invoke(a); });
                                if (App.Project.Arpeggios.Count > 0 && (App.SelectedArpeggio == null || !App.Project.ArpeggioExists(App.SelectedArpeggio)))
                                    App.SelectedArpeggio = App.Project.Arpeggios[0];
                                else
                                    App.SelectedArpeggio = null;
                                break;
                            case FolderType.Sample:
                                App.Project.GetSamplesInFolder(folder.Name).ForEach(s => { App.Project.DeleteSample(s); DPCMSampleDeleted?.Invoke(s); });
                                break;
                        }
                    }
                    else if (r == DialogResult.No)
                    {
                        switch (folder.Type)
                        {
                            case FolderType.Song:       
                                App.Project.GetSongsInFolder(folder.Name).ForEach(s => s.FolderName = null); 
                                break;
                            case FolderType.Instrument: 
                                App.Project.GetInstrumentsInFolder(folder.Name).ForEach(i => i.FolderName = null); 
                                break;
                            case FolderType.Arpeggio:   
                                App.Project.GetArpeggiosInFolder(folder.Name).ForEach(a => a.FolderName = null); 
                                break;
                            case FolderType.Sample:     
                                App.Project.GetSamplesInFolder(folder.Name).ForEach(s => s.FolderName = null); 
                                break;
                        }
                    }

                    App.Project.DeleteFolder(folder.Type, folder.Name);
                    App.UndoRedoManager.EndTransaction();
                    RecreateAllControls();
                }
            });
        }

        private bool HandleContextMenuFolderButton(int x, int y, ProjectExplorerButton button)
        {
            // MATTT
            //var menu = new List<ContextMenuOption>();

            //menu.Add(new ContextMenuOption("MenuDelete", DeleteFolderContext, () => { AskDeleteFolder(button.folder); }, ContextMenuSeparator.After));
            //menu.Add(new ContextMenuOption("Folder",     CollapseAllContext,  () => { ExpandAllFolders(button.folder.Type, false); }));
            //menu.Add(new ContextMenuOption("FolderOpen", ExpandAllContext,    () => { ExpandAllFolders(button.folder.Type, true);  }));

            //App.ShowContextMenu(left + x, top + y, menu.ToArray());

            return true;
        }

        private void EnterParamValue(ProjectExplorerButton button, int x, int y)
        {
            // MATTT
            //var dlg = new ValueInputDialog(ParentWindow, new Point(left + x, top + y), button.param.Name, button.param.GetValue(), button.param.GetMinValue(), button.param.GetMaxValue(), true);
            //dlg.ShowDialogAsync((r) => 
            //{
            //    if (r == DialogResult.OK)
            //    {
            //        App.UndoRedoManager.BeginTransaction(button.paramScope, button.paramObjectId);
            //        button.param.SetValue(dlg.Value);
            //        App.UndoRedoManager.EndTransaction();
            //        MarkDirty();
            //    }
            //});
        }

        private void ResetParamButtonDefaultValue(ProjectExplorerButton button)
        {
            // MATTT
            //App.UndoRedoManager.BeginTransaction(button.paramScope, button.paramObjectId);
            //button.param.SetValue(button.param.DefaultValue);
            //App.UndoRedoManager.EndTransaction();
            MarkDirty();
        }

        private bool HandleContextMenuParamButton(int x, int y, ProjectExplorerButton button)
        {
            // MATTT
            //if (button.param.IsEnabled())
            //{
            //    var menu = new List<ContextMenuOption>();

            //    if (button.type == ButtonType.ParamSlider)
            //        menu.Add(new ContextMenuOption("Type", EnterValueContext, () => { EnterParamValue(button, x, y); }));
            //    menu.Add(new ContextMenuOption("MenuReset", ResetDefaultValueContext, () => { ResetParamButtonDefaultValue(button); }));

            //    App.ShowContextMenu(left + x, top + y, menu.ToArray());
            //}

            return true;
        }

        private bool HandleTouchDownDragInstrument(int x, int y)
        {
            // MATTT
            //var buttonIdx = GetButtonAtCoord(x, y, out var subButtonType, out var buttonRelX, out var buttonRelY);

            //if (buttonIdx >= 0)
            //{
            //    var button = buttons[buttonIdx];
            //    if (button.instrument != null && buttonIdx == highlightedButtonIdx)
            //    {
            //        draggedInstrument = button.instrument;

            //        if (subButtonType < SubButtonType.EnvelopeMax)
            //        {
            //            envelopeDragIdx = (int)subButtonType;
            //            StartCaptureOperation(x, y, CaptureOperation.DragInstrumentEnvelope, buttonIdx, buttonRelX, buttonRelY);
            //        }
            //        else if (subButtonType == SubButtonType.Max && IsPointInButtonIcon(button, buttonIdx, buttonRelX, buttonRelY))
            //        {
            //            envelopeDragIdx = -1;
            //            StartCaptureOperation(x, y, CaptureOperation.DragInstrument, buttonIdx);
            //        }

            //        return true;
            //    }
            //}

            return false;
        }

        private bool HandleTouchDownDragSong(int x, int y)
        {
            // MATTT
            //var buttonIdx = GetButtonAtCoord(x, y, out var subButtonType, out var buttonRelX, out var buttonRelY);

            //if (buttonIdx >= 0)
            //{
            //    var button = buttons[buttonIdx];
            //    if (button.song != null && buttonIdx == highlightedButtonIdx && subButtonType == SubButtonType.Max && IsPointInButtonIcon(button, buttonIdx,  buttonRelX, buttonRelY))
            //    {
            //        App.SelectedSong = button.song;
            //        StartCaptureOperation(x, y, CaptureOperation.DragSong, buttonIdx);
            //        draggedSong = button.song;
            //        return true;
            //    }
            //}

            return false;
        }

        private bool HandleTouchDownDragDPCMSample(int x, int y)
        {
            // MATTT
            //var buttonIdx = GetButtonAtCoord(x, y, out var subButtonType, out var buttonRelX, out var buttonRelY);

            //if (buttonIdx >= 0)
            //{
            //    var button = buttons[buttonIdx];
            //    if (button.sample != null && buttonIdx == highlightedButtonIdx && subButtonType == SubButtonType.Max && IsPointInButtonIcon(button, buttonIdx, buttonRelX, buttonRelY))
            //    {
            //        StartCaptureOperation(x, y, CaptureOperation.DragSample, buttonIdx);
            //        draggedSample = button.sample;
            //        return true;
            //    }
            //}

            return false;
        }

        private bool HandleTouchDownDragArpeggio(int x, int y)
        {
            // MATTT
            //var buttonIdx = GetButtonAtCoord(x, y, out var subButtonType, out var buttonRelX, out var buttonRelY);

            //if (buttonIdx >= 0)
            //{
            //    var button = buttons[buttonIdx];
            //    if (button.arpeggio != null && buttonIdx == highlightedButtonIdx && subButtonType == SubButtonType.Max && IsPointInButtonIcon(button, buttonIdx, buttonRelX, buttonRelY))
            //    {
            //        StartCaptureOperation(x, y, CaptureOperation.DragArpeggio, buttonIdx);
            //        draggedArpeggio = button.arpeggio;
            //        return true;
            //    }
            //}

            return false;
        }

        private bool HandleTouchDownDragFolder(int x, int y)
        {
            // MATTT
            //var buttonIdx = GetButtonAtCoord(x, y, out var subButtonType, out var buttonRelX, out var buttonRelY);

            //if (buttonIdx >= 0)
            //{
            //    var button = buttons[buttonIdx];
            //    if (button.folder != null && buttonIdx == highlightedButtonIdx && subButtonType == SubButtonType.Max && IsPointInButtonIcon(button, buttonIdx, buttonRelX, buttonRelY))
            //    {
            //        StartCaptureOperation(x, y, CaptureOperation.DragFolder, buttonIdx);
            //        draggedFolder = button.folder;
            //        return true;
            //    }
            //}

            return false;
        }

        private bool HandleTouchDownPan(int x, int y)
        {
            // MATTT
            //StartCaptureOperation(x, y, CaptureOperation.MobilePan);
            return true;
        }

        protected override void OnTouchMove(int x, int y)
        {
            UpdateCursor();
            UpdateCaptureOperation(x, y);

            mouseLastX = x;
            mouseLastY = y;
        }

        protected override void OnTouchUp(int x, int y)
        {
            EndCaptureOperation(x, y);
        }

        protected override void OnTouchFling(int x, int y, float velX, float velY)
        {
            if (canFling)
            {
                EndCaptureOperation(x, y);
                flingVelY = velY;
            }
        }

        private void TickFling(float delta)
        {
            if (flingVelY != 0.0f)
            {
                var deltaPixel = (int)Math.Round(flingVelY * delta);
                if (deltaPixel != 0 && DoScroll(deltaPixel))
                    flingVelY *= (float)Math.Exp(delta * -4.5f);
                else
                    flingVelY = 0.0f;
            }
        }

        public override void Tick(float delta)
        {
            TickFling(delta);
            UpdateCaptureOperation(mouseLastX, mouseLastY, true, delta);
        }

        private void EditProjectProperties()
        {
            var pt = ParentWindow.LastMousePosition;
            var dlg = new ProjectPropertiesDialog(ParentWindow, App.Project);

            dlg.EditProjectPropertiesAsync((r) =>
            {
                if (r == DialogResult.OK)
                {
                    var project = App.Project;
                    var newExpansionMask = dlg.ExpansionMask;
                    var expansionRemoved = (project.ExpansionAudioMask & newExpansionMask) != project.ExpansionAudioMask;

                    var tempoMode = dlg.TempoMode;
                    var palAuthoring = dlg.Machine == MachineType.PAL;
                    var numN163Channels = dlg.NumN163Channels;

                    var changedTempoMode = tempoMode != project.TempoMode;
                    var changedExpansion = newExpansionMask != project.ExpansionAudioMask;
                    var changedNumChannels = numN163Channels != project.ExpansionNumN163Channels;
                    var changedAuthoringMachine = palAuthoring != project.PalMode;
                    var changedExpMixer = dlg.MixerProperties.Changed;

                    var transFlags = TransactionFlags.None;

                    if (changedAuthoringMachine || changedNumChannels || changedExpMixer)
                        transFlags = TransactionFlags.RecreatePlayers;
                    else if (changedExpansion)
                        transFlags = TransactionFlags.RecreatePlayers | TransactionFlags.RecreateStreams; // Toggling EPSM will change mono/stereo and requires new audiostreams.
                    else if (changedTempoMode)
                        transFlags = TransactionFlags.StopAudio;

                    App.UndoRedoManager.BeginTransaction(TransactionScope.ProjectNoDPCMSamples, transFlags);

                    project.Name = dlg.Title;
                    project.Author = dlg.Author;
                    project.Copyright = dlg.Copyright;

                    project.SoundEngineUsesDpcmBankSwitching = dlg.DPCMBankswitching;
                    project.SoundEngineUsesExtendedDpcm = dlg.DPCMExtendedRange;
                    project.SoundEngineUsesExtendedInstruments = dlg.InstrumentExtendedRange;

                    if (changedExpansion || changedNumChannels)
                    {
                        App.SelectedInstrument = project.Instruments.Count > 0 ? project.Instruments[0] : null;
                        project.SetExpansionAudioMask(newExpansionMask, numN163Channels);
                        ProjectModified?.Invoke();
                        Reset();
                    }

                    if (changedTempoMode)
                    {
                        if (tempoMode == TempoType.FamiStudio)
                        {
                            if (!project.AreSongsEmpty && Platform.IsDesktop)
                                Platform.MessageBox(ParentWindow, ProjectConvertToFamiTrackerMessage, ProjectChangeTempoModeTitle, MessageBoxButtons.OK);
                            project.ConvertToFamiStudioTempo();
                        }
                        else if (tempoMode == TempoType.FamiTracker)
                        {
                            if (!project.AreSongsEmpty && Platform.IsDesktop)
                                Platform.MessageBox(ParentWindow, ProjectConvertToFamiStudioMessage, ProjectChangeTempoModeTitle, MessageBoxButtons.OK);
                            project.ConvertToFamiTrackerTempo(project.AreSongsEmpty);
                        }

                        ProjectModified?.Invoke();
                        Reset();
                    }

                    if (changedAuthoringMachine && project.UsesFamiStudioTempo)
                    {
                        project.PalMode = palAuthoring;
                        App.PalPlayback = palAuthoring;
                    }

                    dlg.MixerProperties.Apply();

                    var toast = (string)null;

                    if (expansionRemoved)
                        toast += ProjectExpansionRemovedMessage + "\n";
                    if (changedNumChannels)
                        toast += ProjectChangedN163ChannelMessage;

                    if (!string.IsNullOrEmpty(toast))
                        Platform.ShowToast(window, toast, true);

                    App.UndoRedoManager.EndTransaction();

                    RecreateAllControls();
                }
            });
        }

        private bool ToggleAllowProjectMixer()
        {
            var project = App.Project;

            App.UndoRedoManager.BeginTransaction(TransactionScope.ProjectNoDPCMSamples, TransactionFlags.RecreatePlayers);
            project.AllowMixerOverride = !project.AllowMixerOverride;
            App.UndoRedoManager.EndTransaction();

            return project.AllowMixerOverride;
        }

        private void EditSongProperties(Song song)
        {
            var pt = ParentWindow.LastMousePosition;
            var dlg = new PropertyDialog(ParentWindow, SongPropertiesTitle, new Point(pt.X, pt.Y), 320, true); 

            var tempoProperties = new TempoProperties(dlg.Properties, song);

            dlg.Properties.AddColoredTextBox(song.Name, song.Color); // 0
            dlg.Properties.AddColorPicker(song.Color); // 1
            dlg.Properties.AddNumericUpDown(SongLengthLabel.Colon, song.Length, 1, Song.MaxLength, 1, SongLengthTooltip); // 2
            tempoProperties.AddProperties();
            dlg.Properties.Build();

            dlg.ShowDialogAsync((r) =>
            {
                if (r == DialogResult.OK)
                {
                    App.UndoRedoManager.BeginTransaction(TransactionScope.ProjectNoDPCMSamples, TransactionFlags.StopAudio);
                    App.SeekSong(0);

                    var newName = dlg.Properties.GetPropertyValue<string>(0).Trim();

                    if (App.Project.RenameSong(song, newName))
                    {
                        song.Color = dlg.Properties.GetPropertyValue<Color>(1);
                        song.SetLength(dlg.Properties.GetPropertyValue<int>(2));

                        tempoProperties.ApplyAsync(ParentWindow, false, () =>
                        {
                            SongModified?.Invoke(song);
                            App.UndoRedoManager.EndTransaction();
                            RecreateAllControls();
                        });
                    }
                    else
                    {
                        App.UndoRedoManager.AbortTransaction();
                        App.DisplayNotification(RenameSongError, true);
                        MarkDirty();
                    }
                }
            });
        }

        private void EditInstrumentProperties(Instrument instrument)
        {
            // MATTT : Make "pt" window-space.
            var pt = ParentWindow.LastMousePosition;
            var dlg = new PropertyDialog(ParentWindow, InstrumentPropertiesTitle, new Point(pt.X, pt.Y), 240, true, pt.Y > ParentWindowSize.Height / 2);
            dlg.Properties.AddColoredTextBox(instrument.Name, instrument.Color); // 0
            dlg.Properties.AddColorPicker(instrument.Color); // 1
            dlg.Properties.Build();

            dlg.ShowDialogAsync((r) =>
            {
                if (r == DialogResult.OK)
                {
                    var newName = dlg.Properties.GetPropertyValue<string>(0).Trim();

                    App.UndoRedoManager.BeginTransaction(TransactionScope.ProjectNoDPCMSamples);

                    if (App.Project.RenameInstrument(instrument, newName))
                    {
                        instrument.Color = dlg.Properties.GetPropertyValue<Color>(1);
                        InstrumentColorChanged?.Invoke(instrument);
                        RecreateAllControls();
                        App.UndoRedoManager.EndTransaction();
                    }
                    else
                    {
                        App.UndoRedoManager.AbortTransaction();
                        App.DisplayNotification(RenameInstrumentError, true);
                    }
                }
            });
        }

        private void EditFolderProperties(Folder folder)
        {
            var pt = ParentWindow.LastMousePosition;
            var dlg = new PropertyDialog(ParentWindow, FolderPropertiesTitle, new Point(pt.X, pt.Y), 240, true, pt.Y > ParentWindowSize.Height / 2);
            dlg.Properties.AddTextBox(null, folder.Name); // 0
            dlg.Properties.Build();

            dlg.ShowDialogAsync((r) =>
            {
                if (r == DialogResult.OK)
                {
                    var newName = dlg.Properties.GetPropertyValue<string>(0).Trim();

                    App.UndoRedoManager.BeginTransaction(TransactionScope.ProjectNoDPCMSamples);

                    if (App.Project.RenameFolder(folder.Type, folder, newName))
                    {
                        RecreateAllControls();
                        App.UndoRedoManager.EndTransaction();
                    }
                    else
                    {
                        App.UndoRedoManager.AbortTransaction();
                        App.DisplayNotification(RenameFolderError, true);
                    }
                }
            });
        }

        private void EditArpeggioProperties(Arpeggio arpeggio)
        {
            var pt = ParentWindow.LastMousePosition;
            var dlg = new PropertyDialog(ParentWindow, ArpeggioPropertiesTitle, new Point(pt.X, pt.Y), 240, true, pt.Y > ParentWindowSize.Height / 2);
            dlg.Properties.AddColoredTextBox(arpeggio.Name, arpeggio.Color); // 0
            dlg.Properties.AddColorPicker(arpeggio.Color); // 1
            dlg.Properties.Build();

            dlg.ShowDialogAsync((r) =>
            {
                if (r == DialogResult.OK)
                {
                    var newName = dlg.Properties.GetPropertyValue<string>(0).Trim();

                    App.UndoRedoManager.BeginTransaction(TransactionScope.ProjectNoDPCMSamples);

                    if (App.Project.RenameArpeggio(arpeggio, newName))
                    {
                        arpeggio.Color = dlg.Properties.GetPropertyValue<Color>(1);
                        ArpeggioColorChanged?.Invoke(arpeggio);
                        RecreateAllControls();
                        App.UndoRedoManager.EndTransaction();
                    }
                    else
                    {
                        App.UndoRedoManager.AbortTransaction();
                        App.DisplayNotification(RenameArpeggioError, true);
                    }
                }
            });
        }

        private void EditDPCMSampleProperties(DPCMSample sample)
        {
            var pt = ParentWindow.LastMousePosition;
            var dlg = new PropertyDialog(ParentWindow, SamplePropertiesTitle, new Point(pt.X, pt.Y), 240, true, pt.Y > ParentWindowSize.Height / 2);
            dlg.Properties.AddColoredTextBox(sample.Name, sample.Color); // 0
            dlg.Properties.AddColorPicker(sample.Color); // 1
            dlg.Properties.Build();

            dlg.ShowDialogAsync((r) =>
            {
                var newName = dlg.Properties.GetPropertyValue<string>(0).Trim();

                App.UndoRedoManager.BeginTransaction(TransactionScope.DPCMSample, sample.Id);

                if (App.Project.RenameSample(sample, newName))
                {
                    sample.Color = dlg.Properties.GetPropertyValue<Color>(1);
                    DPCMSampleColorChanged?.Invoke(sample);
                    RecreateAllControls();
                    App.UndoRedoManager.EndTransaction();
                }
                else
                {
                    App.UndoRedoManager.AbortTransaction();
                    App.DisplayNotification(RenameSampleError, true);
                }
            });
        }

        public void ValidateIntegrity()
        {
        }

        public void Serialize(ProjectBuffer buffer)
        {
            buffer.Serialize(ref expandedInstrument);
            buffer.Serialize(ref selectedInstrumentTab);
            buffer.Serialize(ref expandedSample);
            //buffer.Serialize(ref scrollY); // MATTT

            if (buffer.IsReading)
            {
                captureOperation = CaptureOperation.None;
                lastCaptureOperation = CaptureOperation.None;
                Capture = false;
                flingVelY = 0.0f;

                ClampScroll();
                RecreateAllControls();
                BlinkButton(null);
            }
        }
    }
}
