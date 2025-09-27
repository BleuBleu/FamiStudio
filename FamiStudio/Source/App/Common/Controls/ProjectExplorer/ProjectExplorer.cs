using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace FamiStudio
{
    public partial class ProjectExplorer : Container
    {
        private const int   DefaultExpandSizeX      = 12;
        private const int   DefaultIconSizeX        = 16;
        private const int   DefaultMarginX          = 2;
        private const int   DefaultSpacingX         = Platform.IsMobile ? 0 : 2;
        private const int   DefaultPanelSizeY       = 21;
        private const int   DefaultDraggedLineSizeY = 5;
        private const int   DefaultParamSizeX       = Platform.IsMobile ? 92 : 104;
        private const float ScrollSpeedFactor       = Platform.IsMobile ? 2.0f : 1.0f;

        private int expandSizeX;
        private int spacingX; // Spacing between envelope buttons.
        private int marginX; // Spacing between other stuff.
        private int iconSizeX;
        private int panelSizeY;
        private int paramSizeX;
        private int virtualSizeY;
        private int dragLineSizeY;
        private int topTabSizeY;

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
        LocalizedString AddNewSampleTooltip;
        LocalizedString AutoSortArpeggiosTooltip;
        LocalizedString AutoSortInstrumentsTooltip;
        LocalizedString AutoSortSamplesTooltip;
        LocalizedString AutoSortSongsTooltip;
        LocalizedString ChangeValueFineTooltip;
        LocalizedString ChangeValueTooltip;
        LocalizedString CheckBoxSelectAllTooltip;
        LocalizedString CopyEnvelopeTooltip;
        LocalizedString CopyReplaceInstrumentTooltip;
        LocalizedString EditEnvelopeTooltip;
        LocalizedString EditSamplesTooltip;
        LocalizedString EditWaveformTooltip;
        LocalizedString EditArpeggioTooltip;
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
        LocalizedString ImportSampleContext;
        LocalizedString PasteRegisterValueContext;
        LocalizedString PropertiesArpeggioContext;
        LocalizedString PropertiesInstrumentContext;
        LocalizedString PropertiesProjectContext;
        LocalizedString PropertiesSamplesContext;
        LocalizedString PropertiesSongContext;
        LocalizedString PropertiesFolderContext;
        LocalizedString ReplaceWithContext;
        LocalizedString ResampleWavContext;
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

        private class CaptureOperation
        {
            private string name;
            private bool needsThreshold;
            private bool realTimeUpdate;

            public string Name => name;
            public bool NeedsThreshold => needsThreshold;
            public bool RealTimeUpdate => realTimeUpdate;

            public CaptureOperation(string desc, bool realTime = true, bool threshold = true)
            {
                name = desc;
                needsThreshold = threshold;
                realTimeUpdate = realTime;
            }

            public override string ToString()
            {
                return name;
            }
        }

        private static readonly CaptureOperation DragInstrument               = new CaptureOperation("DragInstrument");
        private static readonly CaptureOperation DragInstrumentEnvelope       = new CaptureOperation("DragInstrumentEnvelope");
        private static readonly CaptureOperation DragInstrumentSampleMappings = new CaptureOperation("DragInstrumentSampleMappings");
        private static readonly CaptureOperation DragArpeggio                 = new CaptureOperation("DragArpeggio");
        private static readonly CaptureOperation DragArpeggioValues           = new CaptureOperation("DragArpeggioValues", false);
        private static readonly CaptureOperation DragSample                   = new CaptureOperation("DragSample");
        private static readonly CaptureOperation DragSong                     = new CaptureOperation("DragSong");
        private static readonly CaptureOperation DragFolder                   = new CaptureOperation("DragFolder");
        private static readonly CaptureOperation MobilePan                    = new CaptureOperation("MobilePan", true, false);

        private Point mouseLastPos;
        private Point captureMousePos;
        private Point captureButtonRelPos;
        private int captureCookie;
        private int captureScrollY = -1;
        private int envelopeDragIdx = -1;
        private TextureAtlasRef envelopeDragTexture = null;
        private object highlightedObject;
        private int captureButtonSign = 0;
        private float flingVelY = 0.0f;
        private float iconImageScale = Platform.IsMobile ? DpiScaling.Window * 0.25f : 1.0f;
        private float captureDuration = 0.0f;
        private bool captureThresholdMet = false;
        private bool canFling = false;
        private TabType selectedTab = TabType.Project;
        private CaptureOperation captureOperation;
        private Instrument draggedInstrument = null;
        private Instrument expandedInstrument = null;
        private Folder draggedFolder = null;
        private string selectedInstrumentTab = null;
        private DPCMSample expandedSample = null;
        private DPCMSample draggedSample = null;
        private Arpeggio draggedArpeggio = null;
        private Song draggedSong = null;

        // Global controls
        private PanelContainer tabPanel;
        private Container mainContainer;
        private ScrollBar scrollBar;
        private PanelContainer noneArpPanel;

        // Register viewer stuff
        private NesApu.NesRegisterValues registerValues;
        private RegisterViewer[] registerViewers = new RegisterViewer[ExpansionType.Count];

        public DPCMSample DraggedSample => captureOperation == DragSample ? draggedSample : null;
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
            expandSizeX   = DpiScaling.ScaleForWindow(DefaultExpandSizeX);
            spacingX      = DpiScaling.ScaleForWindow(DefaultSpacingX);
            marginX       = DpiScaling.ScaleForWindow(DefaultMarginX);
            iconSizeX     = DpiScaling.ScaleForWindow(DefaultIconSizeX);
            panelSizeY    = DpiScaling.ScaleForWindow(DefaultPanelSizeY);
            paramSizeX    = DpiScaling.ScaleForWindow(DefaultParamSizeX);
            dragLineSizeY = DpiScaling.ScaleForWindow(DefaultDraggedLineSizeY);
        }

        public void Reset()
        {
            expandedInstrument = null;
            expandedSample = null;
            selectedInstrumentTab = null;
            highlightedObject = null;
            RecreateAllControls();
            MarkDirty();
        }

        public void LayoutChanged()
        {
            if (selectedTab == TabType.Registers && !Settings.ShowRegisterViewer)
                selectedTab = TabType.Project;

            UpdateRenderCoords();
            ClampScroll();
            MarkDirty();
        }

        private SortedDictionary<int, (object, Folder)>[] insertionPoints = new[]
        {
            new SortedDictionary<int, (object, Folder)>(),
            new SortedDictionary<int, (object, Folder)>(),
            new SortedDictionary<int, (object, Folder)>(),
            new SortedDictionary<int, (object, Folder)>(),
        };

        private SortedDictionary<int, Folder>[] folderInsertionPoints = new[]
        {
            new SortedDictionary<int, Folder>(),
            new SortedDictionary<int, Folder>(),
            new SortedDictionary<int, Folder>(),
            new SortedDictionary<int, Folder>(),
        };

        private void RemoveAllInsertionPoints()
        {
            foreach (var i in insertionPoints)
                i.Clear();
            foreach (var i in folderInsertionPoints)
                i.Clear();
        }

        private void CreateInsertionPoint(int t, Object o = null, Folder f = null)
        {
            insertionPoints[t][mainContainer.FindLastControlOfType<PanelContainer>().Bottom] = (o, f);
        }

        private void CreateFolderInsertionPoint(int t, Folder f)
        {
            folderInsertionPoints[t][mainContainer.FindLastControlOfType<PanelContainer>().Bottom] = f;
        }

        private PanelContainer CreateGradientPanel(Color color, object userData = null, bool scroll = true, Control ctrlBefore = null)
        {
            var actualContainer = scroll ? mainContainer : this;
            var lastControl = ctrlBefore != null ? ctrlBefore : actualContainer.FindLastControlOfType<PanelContainer>();
            var y = lastControl != null ? lastControl.Bottom : 0;
            var panel = new PanelContainer(color);
            panel.Move(0, y, actualContainer.Width, panelSizeY);
            panel.UserData = userData;
            actualContainer.AddControl(panel);
            return panel;
        }

        private Label CreateCenteredLabel(PanelContainer panel, string text, int width, bool ellipsis = false)
        {
            var label = new Label(text, false);
            label.Font = fonts.FontMediumBold;
            label.Centered = true;
            label.Ellipsis = ellipsis;
            label.Move(Utils.DivideAndRoundDown(panel.Width - width, 2), 0, width, panel.Height);
            panel.AddControl(label);
            return label;
        }

        private Label CreateLabel(PanelContainer panel, string text, bool black, int x, int y, int width, bool ellipsis = false)
        {
            var label = new Label(text, false);
            label.Color = black ? Theme.BlackColor : label.Color;
            label.Ellipsis = ellipsis;
            label.Move(x, y, width, panel.Height);
            panel.AddControl(label);
            return label;
        }

        private Button CreateExpandButton(PanelContainer panel, bool black, bool expanded)
        {
            Debug.Assert(
                panel.UserData is Folder     ||
                panel.UserData is Instrument || 
                panel.UserData is DPCMSample);

            var expandButton = CreateImageButton(panel, marginX, expanded ? "InstrumentExpanded" : "InstrumentExpand", black);
            return expandButton;
        }

        private ImageBox CreateImageBox(PanelContainer panel, int x, string image, bool black = false)
        {
            var imageBox = new ImageBox(image);
            panel.AddControl(imageBox);
            imageBox.Tint = black ? Color.Black : Theme.LightGreyColor2;
            imageBox.ImageScale = iconImageScale;
            imageBox.AutoSizeToImage();
            imageBox.Move(x, Utils.DivideAndRoundUp(panel.Height - imageBox.Height, 2));
            return imageBox;
        }

        private Button CreateImageButton(PanelContainer panel, int x, string image, bool black = true)
        {
            var button = new Button(image, null);
            button.Transparent = true;
            button.ImageScale = iconImageScale;
            button.VibrateOnClick = true;
            if (black)
                button.ForegroundColor = Color.Black;
            panel.AddControl(button);
            button.AutoSizeToImage();
            button.Move(x, Utils.DivideAndRoundUp(panel.Height - button.Height, 2));

            return button;
        }

        private void CreateFolderControls(Folder folder)
        {
            var panel = CreateGradientPanel(Theme.DarkGreyColor5, folder);
            panel.ToolTip = $"<MouseRight> {MoreOptionsTooltip}";
            panel.PointerDown += (s, e) => Folder_PointerDown(s, e, folder);
            panel.PointerUp += (s, e) => Folder_PointerUp(e, folder);
            panel.ContainerPointerDownNotify += (s, e) => Folder_PointerDown(s, e, folder);
            panel.ContainerPointerUpNotify += (s, e) => Folder_PointerUp(e, folder);
            panel.ContainerTouchClickNotify += (s, e) => Folder_TouchClick(s, e, folder);

            var expand = CreateExpandButton(panel, false, folder.Expanded);
            expand.ToolTip = $"<MouseLeft> {ExpandTooltip} - <MouseRight> {MoreOptionsTooltip}";
            expand.Click += (s) => ToggleExpandFolder(folder);

            var icon = CreateImageBox(panel, expand.Right + spacingX, folder.Expanded ? "FolderOpen" : "Folder");
            icon.PointerDown += (s, e) => Folder_PointerDown(s, e, folder);
            icon.WhiteHighlight = highlightedObject == folder;

            var propsButton = CreateImageButton(panel, panel.Width - iconSizeX - marginX, "Properties", false);
            propsButton.ToolTip = $"<MouseLeft> {PropertiesFolderTooltip}";
            propsButton.Click += (s) => EditFolderProperties(folder);

            CreateLabel(panel, folder.Name, false, icon.Right + marginX, 0, propsButton.Left - icon.Right - marginX * 2, true);
        }

        private void Folder_PointerDown(Control sender, PointerEventArgs e, Folder folder)
        {
            var allowDrag = Platform.IsDesktop || (highlightedObject == folder && sender is ImageBox);

            if (!e.Handled && e.Left && allowDrag)
            {
                draggedFolder = folder;
                StartCaptureOperation(sender, e.Position, DragFolder);
                e.MarkHandled();
            }
        }

        private void Folder_PointerUp(PointerEventArgs e, Folder folder)
        {
            if (!e.Handled && e.Right)
            {
                App.ShowContextMenuAsync(new[]
                {
                    new ContextMenuOption("MenuDelete", DeleteFolderContext, () => { AskDeleteFolder(folder); }, ContextMenuSeparator.After),
                    new ContextMenuOption("Folder", CollapseAllContext, () => { ExpandAllFolders(folder.Type, false); }),
                    new ContextMenuOption("FolderOpen", ExpandAllContext, () => { ExpandAllFolders(folder.Type, true); }),
                    new ContextMenuOption("MenuProperties", PropertiesFolderContext, () => { EditFolderProperties(folder, true); }, ContextMenuSeparator.Before)
                });
                e.MarkHandled();
            }
        }

        private void Folder_TouchClick(Control sender, PointerEventArgs e, Folder folder)
        {
            if (!e.Handled)
            {
                UpdateHighlightedItem(folder);
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
                    var button = new Button(null, TabNames[i]);
                    button.Border = true;
                    button.VibrateOnClick = true;
                    if ((int)selectedTab == i)
                        button.Font = fonts.FontMediumBold;
                    else
                        button.BackgroundColor = new Color(0, 0, 0, 160);
                    button.Move(i * tabSizeX, 0, i == (int)TabType.Count - 1 ? width - tabSizeX * i : tabSizeX, panelSizeY);
                    button.Click += TopTabs_Click;
                    button.UserData = (TabType)i;
                    tabPanel.AddControl(button);
                }
            }
            else
            {
                tabPanel = null;
            }

            topTabSizeY = tabPanel != null? tabPanel.Height : 0;   
        }

        private void TopTabs_Click(Control sender)
        {
            selectedTab = (TabType)sender.UserData;
            RecreateAllControls();
        }

        private void CreateMainContainer()
        {
            var scrollBarWidth = 0;
            var oldScrollY = mainContainer != null? mainContainer.ScrollY : 0;

            mainContainer = new Container();
            mainContainer.ContainerPointerDownNotify += MainContainer_ContainerPointerDownNotify;
            mainContainer.ContainerPointerUpNotify += MainContainer_ContainerPointerUpNotify;
            mainContainer.ContainerTouchFlingNotify += MainContainer_ContainerTouchFlingNotify;
            mainContainer.Rendering += MainContainer_Rendering;
            mainContainer.Scrolled += MainContainer_Scrolled;
            mainContainer.ScrollY = oldScrollY;
            AddControl(mainContainer);

            if (Settings.ScrollBars != Settings.ScrollBarsNone)
            {
                scrollBar = new ScrollBar();
                scrollBar.LineColor = Color.Black;
                scrollBar.Move(width - scrollBar.ScrollBarThickness, topTabSizeY, scrollBar.ScrollBarThickness, height - topTabSizeY - 1);
                scrollBar.Scrolled += ScrollBar_Scrolled;
                AddControl(scrollBar);
                scrollBarWidth = scrollBar.ScrollBarThickness;
            }
            else
            {
                scrollBar = null;
            }

            mainContainer.Move(0, topTabSizeY, width - scrollBarWidth, height - topTabSizeY);
        }

        private void MainContainer_ContainerPointerDownNotify(Control sender, PointerEventArgs e)
        {
            flingVelY = 0.0f;

            if (Platform.IsMobile && !e.Handled && e.Left)
            {
                StartCaptureOperation(sender, e.Position, MobilePan);
            }
        }

        private void MainContainer_ContainerPointerUpNotify(Control sender, PointerEventArgs e)
        {
            if (!e.Handled && e.Left)
            {
                var ctrlPos = WindowToControl(sender.ControlToWindow(e.Position));
                EndCaptureOperation(ctrlPos);
            }
        }

        private void MainContainer_ContainerTouchFlingNotify(Control sender, PointerEventArgs e)
        {
            if (canFling)
            {
                EndCaptureOperation(e.Position);
                flingVelY = e.FlingVelocityY;
            }
        }

        private void SyncScrollBarToContainer()
        {
            if (scrollBar != null)
                scrollBar.SetScroll(mainContainer.ScrollY, false);
        }

        private void MainContainer_Scrolled(Container sender)
        {
            SyncScrollBarToContainer();
        }

        private void ScrollBar_Scrolled(ScrollBar sender, int pos)
        {
            mainContainer.ScrollY = pos;
        }

        private void CreateProjectHeaderControls()
        {
            var project = App.Project;
            var projectText = string.IsNullOrEmpty(project.Author) ? $"{project.Name}" : $"{project.Name} ({project.Author})";
            var panel = CreateGradientPanel(Theme.DarkGreyColor4, project);
            panel.ToolTip = $"<MouseRight> {MoreOptionsTooltip}";
            panel.PointerUp += (s, e) => ProjectHeader_PointerUp(e);
            panel.ContainerPointerUpNotify += (s, e) => ProjectHeader_PointerUp(e);

            var propsButton = CreateImageButton(panel, panel.Width - iconSizeX - marginX, "Properties", false);
            propsButton.ToolTip = $"<MouseLeft> {PropertiesProjectTooltip}";
            propsButton.Click += (s) => EditProjectProperties();

            var mixerButton = CreateImageButton(panel, propsButton.Left -spacingX - iconSizeX, "Mixer", false);
            mixerButton.ToolTip = $"<MouseLeft> {AllowProjectMixerSettings}"; 
            mixerButton.Dimmed = !project.AllowMixerOverride;
            mixerButton.Click += (s) => mixerButton.Dimmed = !ToggleAllowProjectMixer();

            CreateCenteredLabel(panel, projectText, 2 * mixerButton.Left - panel.Width, true);
        }

        private void ProjectHeader_PointerUp(PointerEventArgs e)
        {
            if (!e.Handled && e.Right)
            {
                App.ShowContextMenuAsync(new[]
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
            addButton.Click += (s) => AskAddSong();
            addButton.RightClick += (s) => AskAddSong();
            addButton.ClickOnMouseUp = true;

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
            addButton.RightClick += (s) => AskAddInstrument(); 
            addButton.ClickOnMouseUp = true;

            var importButton = CreateImageButton(panel, addButton.Left - spacingX - iconSizeX, "InstrumentOpen", false);
            importButton.ToolTip = $"<MouseLeft> {ImportInstrumentsTooltip}";
            importButton.Click += (s) => ImportInstruments();

            var sortButton = CreateImageButton(panel, importButton.Left - spacingX - iconSizeX, "Sort", false);
            sortButton.ToolTip = $"<MouseLeft> {AutoSortInstrumentsTooltip}";
            sortButton.Dimmed = !project.AutoSortInstruments;
            sortButton.Click += (s) => SortInstruments();

            CreateCenteredLabel(panel, InstrumentHeaderLabel, 2 * sortButton.Left - panel.Width, false);
        }

        private void CreateSongControls(Song song)
        {
            var panel = CreateGradientPanel(song.Color, song);
            panel.ToolTip = $"<MouseLeft> {MakeSongCurrentTooltip} - <MouseLeft><Drag> {ReorderSongsTooltip}\n<MouseRight> {MoreOptionsTooltip}";
            panel.PointerUp += (s, e) => Song_PointerUp(e, song);
            panel.ContainerPointerUpNotify += (s, e) => Song_PointerUp(e, song);
            panel.PointerDown += (s, e) => Song_PointerDown(s, e, song);
            panel.ContainerPointerDownNotify += (s, e) => Song_PointerDown(s, e, song);
            panel.ContainerTouchClickNotify += (s, e) => Song_TouchClick(s, e, song);

            var icon = CreateImageBox(panel, marginX + expandSizeX, "Music", true);
            icon.PointerDown += (s, e) => Song_PointerDown(s, e, song); 
            icon.WhiteHighlight = highlightedObject == song;

            var props = CreateImageButton(panel, panel.Width - marginX - iconSizeX, "Properties");
            props.ToolTip = $"<MouseLeft> {PropertiesSongTooltip}";
            props.Click += (s) => EditSongProperties(song);

            var label = CreateLabel(panel, song.Name, true, icon.Right + marginX, 0, props.Left - icon.Right - marginX * 2, true);
            label.Font = song == App.SelectedSong ? fonts.FontMediumBold : fonts.FontMedium;
        }

        private void Song_PointerDown(Control sender, PointerEventArgs e, Song song)
        {
            var allowDrag = Platform.IsDesktop || (highlightedObject == song && sender is ImageBox);

            if (!e.Handled && e.Left && allowDrag)
            {
                if (Platform.IsDesktop)
                    App.SelectedSong = song;

                draggedSong = song;
                StartCaptureOperation(sender, e.Position, DragSong);
                e.MarkHandled();
            }
        }

        private void Song_PointerUp(PointerEventArgs e, Song song)
        {
            if (!e.Handled && e.Right)
            {
                var menu = new List<ContextMenuOption>();
                if (App.Project.Songs.Count > 1)
                    menu.Add(new ContextMenuOption("MenuDelete", DeleteSongContext, () => { AskDeleteSong(song); }, ContextMenuSeparator.After));
                menu.Add(new ContextMenuOption("MenuDuplicate", DuplicateContext, () => { DuplicateSong(song); }));
                menu.Add(new ContextMenuOption("MenuProperties", PropertiesSongContext, () => { EditSongProperties(song, true); }, ContextMenuSeparator.Before));
                App.ShowContextMenuAsync(menu.ToArray());
                e.MarkHandled();
            }
        }

        private void Song_TouchClick(Control sender, PointerEventArgs e, Song song)
        {
            if (!e.Handled)
            {
                App.SelectedSong = song;
                UpdateHighlightedItem(song);
            }
        }

        private void CreateInstrumentControls(Instrument instrument)
        {
            var panel = CreateGradientPanel(instrument.Color, instrument);
            panel.ToolTip = $"<MouseLeft> {SelectInstrumentTooltip} - <MouseLeft><Drag> {CopyReplaceInstrumentTooltip}\n<MouseRight> {MoreOptionsTooltip}";
            panel.PointerUp += (s, e) => Instrument_PointerUp(e, instrument);
            panel.ContainerPointerUpNotify += (s, e) => Instrument_PointerUp(e, instrument);
            panel.PointerDown += (s, e) => Instrument_PointerDown(s, e, instrument);
            panel.ContainerPointerDownNotify += (s, e) => Instrument_PointerDown(s, e, instrument);
            panel.ContainerTouchClickNotify += (s, e) => Instrument_TouchClick(s, e, instrument);

            var expand = CreateExpandButton(panel, true, expandedInstrument == instrument);
            expand.ToolTip = $"<MouseLeft> {ExpandTooltip} - <MouseRight> {MoreOptionsTooltip}";
            expand.Click += (s) => ToggleExpandInstrument(instrument);

            var icon = CreateImageBox(panel, expand.Right + spacingX, ExpansionType.Icons[instrument.Expansion], true);
            icon.PointerDown += (s, e) => Instrument_PointerDown(s, e, instrument); 
            icon.WhiteHighlight = highlightedObject == instrument;

            var x = panel.Width - marginX - iconSizeX; 
            var props = CreateImageButton(panel, x, "Properties");
            props.ToolTip = $"<MouseLeft> {PropertiesInstrumentTooltip} - <MouseRight> {MoreOptionsTooltip}";
            props.Click += (s) => EditInstrumentProperties(instrument);

            if (instrument.Expansion == ExpansionType.None)
            {
                x -= spacingX + props.Width;
                var dpcm = CreateImageButton(panel, x, "ChannelDPCM");
                dpcm.Dimmed = !instrument.HasAnyMappedSamples;
                dpcm.WhiteHighlight = highlightedObject == instrument;
                dpcm.UserData = "DPCM";
                dpcm.ToolTip = $"<MouseLeft> {EditSamplesTooltip} - <MouseRight> {MoreOptionsTooltip}";
                dpcm.Click += (s) => App.StartEditDPCMMapping(instrument);
                dpcm.PointerDown += (s, e) => InstrumentDpcm_PointerDown(s, instrument, e, dpcm.Image);
                dpcm.MarkHandledOnClick = Platform.IsMobile;
            }

            var lastEnv = (Button)null;
            for (var i = 0; i < EnvelopeDisplayOrder.Length; i++)
            {
                var idx = EnvelopeDisplayOrder[i];
                if (instrument.Envelopes[idx] != null)
                {
                    x -= spacingX + props.Width;
                    var env = CreateImageButton(panel, x, EnvelopeType.Icons[idx]);
                    env.Dimmed = instrument.Envelopes[idx].IsEmpty(idx);
                    env.WhiteHighlight = highlightedObject == instrument;
                    env.UserData = EnvelopeType.InternalNames[idx];
                    env.ToolTip = $"<MouseLeft> {EditEnvelopeTooltip.Format(EnvelopeType.LocalizedNames[idx].Value.ToLower())}\n<MouseLeft><Drag> {CopyEnvelopeTooltip} - <MouseRight> {MoreOptionsTooltip}";
                    env.Click += (s) => InstrumentEnvelope_Click(instrument, idx);
                    env.PointerDown += (s, e) => Instrument_PointerDown(s, e, instrument, idx, env.Image);
                    env.PointerUp += (s, e) => Instrument_PointerUp(e, instrument, idx);
                    env.MarkHandledOnClick = Platform.IsMobile;
                    lastEnv = env;
                }
            }

            var label = CreateLabel(panel, instrument.Name, true, icon.Right + marginX, 0, lastEnv.Left - icon.Right - marginX * 2, true);
            label.Font = App.SelectedInstrument == instrument ? fonts.FontMediumBold : fonts.FontMedium;
        }

        private void InstrumentDpcm_PointerDown(Control sender, Instrument instrument, PointerEventArgs e, TextureAtlasRef image)
        {
            var allowDrag = Platform.IsDesktop || (highlightedObject == instrument);

            if (!e.Handled && e.Left && allowDrag)
            {
                draggedInstrument = instrument;
                envelopeDragTexture = image;
                StartCaptureOperation(sender, e.Position, DragInstrumentSampleMappings);
                e.MarkHandled();
            }
        }

        private void InstrumentEnvelope_Click(Instrument instrument, int envelopeType)
        {
            App.SelectedInstrument = instrument;
            App.StartEditInstrument(instrument, envelopeType);
        }

        private void Instrument_PointerDown(Control sender, PointerEventArgs e, Instrument inst, int envelopeType = -1, TextureAtlasRef image = null)
        {
            var allowDrag = Platform.IsDesktop || (highlightedObject == inst && (envelopeType >= 0 || sender is ImageBox));

            if (!e.Handled && e.Left && allowDrag)
            {
                if (Platform.IsDesktop)
                    App.SelectedInstrument = inst;

                draggedInstrument = inst;
                envelopeDragIdx = envelopeType;
                envelopeDragTexture = image;

                if (envelopeType >= 0)
                    StartCaptureOperation(sender, e.Position, DragInstrumentEnvelope);
                else
                    StartCaptureOperation(sender, e.Position, DragInstrument);

                e.MarkHandled();
            }
        }

        private void Instrument_PointerUp(PointerEventArgs e, Instrument inst, int envelopeType = -1)
        {
            if (!e.Handled && e.Right)
            {
                var menu = new List<ContextMenuOption>();

                if (inst != null)
                {
                    menu.Add(new ContextMenuOption("MenuDelete", DeleteInstrumentContext, () => { AskDeleteInstrument(inst); }, ContextMenuSeparator.After));

                    if (envelopeType >= 0)
                    {
                        menu.Add(new ContextMenuOption("MenuClearEnvelope", ClearEnvelopeContext, () => { ClearInstrumentEnvelope(inst, envelopeType); }, ContextMenuSeparator.After));
                    }
                    else
                    {
                        if (inst.IsN163 || inst.IsFds)
                        {
                            menu.Add(new ContextMenuOption("MenuWave", ResampleWavContext, () => { LoadN163FdsResampleWavFile(inst); }, ContextMenuSeparator.Before));

                            if (inst.IsN163 && inst.N163ResampleWaveData != null ||
                                inst.IsFds && inst.FdsResampleWaveData != null)
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
                                    var localExp = exp;
                                    menu.Add(new ContextMenuOption(ExpansionType.Icons[exp], DuplicateConvertContext.Format(ExpansionType.GetLocalizedName(exp, ExpansionType.LocalizationMode.Instrument)), () => { DuplicateConvertInstrument(inst, localExp); }));
                                }
                            }
                        }
                    }

                    menu.Add(new ContextMenuOption("MenuProperties", PropertiesInstrumentContext, () => { EditInstrumentProperties(inst, true); }, ContextMenuSeparator.Before));
                }

                App.ShowContextMenuAsync(menu.ToArray());
                e.MarkHandled();
            }
        }

        private void Instrument_TouchClick(Control sender, PointerEventArgs e, Instrument instrument)
        {
            if (!e.Handled)
            {
                App.SelectedInstrument = instrument;
                UpdateHighlightedItem(instrument);
            }
        }

        private void CreateDpcmSampleControls(DPCMSample sample)
        {
            var panel = CreateGradientPanel(sample.Color, sample);
            panel.ToolTip = $"<MouseRight> {MoreOptionsTooltip}";
            panel.PointerUp += (s, e) => DpcmSample_PointerUp(e, sample);
            panel.ContainerPointerUpNotify += (s, e) => DpcmSample_PointerUp(e, sample);
            panel.PointerDown += (s, e) => DpcmSample_PointerDown(s, e, sample);
            panel.ContainerPointerDownNotify += (s, e) => DpcmSample_PointerDown(s, e, sample);
            panel.ContainerTouchClickNotify += (s, e) => DpcmSample_TouchClick(s, e, sample);

            var expand = CreateExpandButton(panel, true, expandedSample == sample);
            expand.ToolTip = $"<MouseLeft> {ExpandTooltip} - <MouseRight> {MoreOptionsTooltip}";
            expand.Click += (s) => ToggleExpandDPCMSample(sample);

            var icon = CreateImageBox(panel, expand.Right, "ChannelDPCM", true);
            icon.PointerDown += (s, e) => DpcmSample_PointerDown(s, e, sample); 
            icon.WhiteHighlight = highlightedObject == sample;

            var props = CreateImageButton(panel, panel.Width - marginX - iconSizeX, "Properties");
            props.ToolTip = $"<MouseLeft> {PropertiesInstrumentTooltip}";
            props.Click += (s) => EditDPCMSampleProperties(sample);

            var editWave = CreateImageButton(panel, props.Left - spacingX - iconSizeX, "WaveEdit");
            editWave.ToolTip = $"<MouseLeft> {EditWaveformTooltip}";
            editWave.Click += (s) => App.StartEditDPCMSample(sample);

            var reload = CreateImageButton(panel, editWave.Left - spacingX - iconSizeX, "Reload");
            reload.ToolTip = $"<MouseLeft> {ReloadSourceDataTooltip}";
            reload.Click += (s) => ReloadDPCMSampleSourceData(sample);
            reload.EnabledEvent += (s) => !string.IsNullOrEmpty(sample.SourceFilename);

            var play = CreateImageButton(panel, reload.Left - spacingX - iconSizeX, "PlaySource");
            play.ToolTip = $"<MouseLeft> {PreviewProcessedSampleTooltip}\n<MouseRight> {PlaySourceSampleTooltip}";
            play.Click += (s) => App.PreviewDPCMSample(sample, false);
            play.RightClick += (s) => App.PreviewDPCMSample(sample, true);
            play.ClickOnMouseUp = true;
            play.SetSupportsDoubleClick(true);

            CreateLabel(panel, sample.Name, true, icon.Right + marginX, 0, play.Left - icon.Right - marginX * 2, true);
        }

        private void DpcmSample_PointerDown(Control sender, PointerEventArgs e, DPCMSample sample)
        {
            var allowDrag = Platform.IsDesktop || (highlightedObject == sample && sender is ImageBox);

            if (!e.Handled && e.Left && allowDrag && !(sender is Button))
            {
                draggedSample = sample;
                StartCaptureOperation(sender, e.Position, DragSample);
                e.MarkHandled();
            }
        }

        private void DpcmSample_PointerUp(PointerEventArgs e, DPCMSample sample)
        {
            if (!e.Handled && e.Right)
            {
                var menu = new List<ContextMenuOption>();

                menu.Add(new ContextMenuOption("MenuDelete", DeleteSampleContext, () => { AskDeleteDPCMSample(sample); }, ContextMenuSeparator.After));
                
                if (Platform.IsDesktop)
                {
                    menu.Add(new ContextMenuOption("MenuWave", ImportSampleContext, () => { UpdateDPCMSampleSourceData(sample); }));
                    menu.Add(new ContextMenuOption("MenuSave", ExportProcessedDmcDataContext, () => { ExportDPCMSampleProcessedData(sample); }));
                    menu.Add(new ContextMenuOption("MenuSave", ExportSourceDataContext, () => { ExportDPCMSampleSourceData(sample); }));
                }

                if (sample.SourceDataIsWav)
                {
                    menu.Add(new ContextMenuOption("MenuTrash", DiscardSourceWavDataContext, DiscardSourceWavDataTooltip, () => { DeleteDpcmSourceWavData(sample); }));
                }

                menu.Add(new ContextMenuOption("MenuBankAssign", AutoAssignBanksContext, () => { AutoAssignSampleBanks(); }, ContextMenuSeparator.Before));
                menu.Add(new ContextMenuOption("MenuProperties", PropertiesSamplesContext, () => { EditDPCMSampleProperties(sample, true); }, ContextMenuSeparator.Before));

                App.ShowContextMenuAsync(menu.ToArray());
                e.MarkHandled();
            }
        }

        private void DpcmSample_TouchClick(Control sender, PointerEventArgs e, DPCMSample sample)
        {
            if (!e.Handled)
            {
                UpdateHighlightedItem(sample);
            }
        }

        private void CreateNoneArpeggioControls()
        {
            noneArpPanel = CreateGradientPanel(Theme.LightGreyColor1);
            noneArpPanel.ToolTip = $"<MouseLeft> {SelectArpeggioTooltip}";
            if (Platform.IsDesktop)
            {
                noneArpPanel.ContainerPointerDownNotify += NoneArpeggio_PointerDown;
            }
            else
            {
                noneArpPanel.ContainerTouchClickNotify += NoneArpeggio_PointerDown;
            }

            var icon = CreateImageBox(noneArpPanel, marginX + expandSizeX, EnvelopeType.Icons[EnvelopeType.Arpeggio], true);
            var label = CreateLabel(noneArpPanel, ArpeggioNoneLabel, true, icon.Right + marginX, 0, noneArpPanel.Width - icon.Right - marginX);
            label.Font = App.SelectedArpeggio == null ? fonts.FontMediumBold : fonts.FontMedium;

        }

        private void CreateArpeggioControls(Arpeggio arp)
        {
            var panel = CreateGradientPanel(arp.Color, arp);
            panel.ToolTip = $"<MouseLeft> {SelectArpeggioTooltip} - <MouseLeft><Drag> {ReplaceArpeggioTooltip}\n<MouseRight> {MoreOptionsTooltip}";
            panel.PointerUp += (s, e) => Arpeggio_PointerUp(e, arp);
            panel.ContainerPointerUpNotify += (s, e) => Arpeggio_PointerUp(e, arp);
            panel.PointerDown += (s, e) => Arpeggio_PointerDown(s, e, arp);
            panel.ContainerPointerDownNotify += (s, e) => Arpeggio_PointerDown(s, e, arp);
            panel.ContainerTouchClickNotify += (s, e) => Arpeggio_TouchClick(s, e, arp);

            var icon = CreateImageBox(panel, marginX + expandSizeX, EnvelopeType.Icons[EnvelopeType.Arpeggio], true);
            icon.PointerDown += (s, e) => Arpeggio_PointerDown(s, e, arp); 
            icon.WhiteHighlight = highlightedObject == arp;

            var props = CreateImageButton(panel, panel.Width - iconSizeX - marginX, "Properties");
            props.ToolTip = $"<MouseLeft> {PropertiesArpeggioTooltip}";
            props.Click += (s) => EditArpeggioProperties(arp);

            var edit = CreateImageButton(panel, props.Left - spacingX - iconSizeX, "EnvelopeArpeggio");
            edit.ToolTip = $"<MouseLeft> {EditArpeggioTooltip}";
            edit.UserData = "Arpeggio";
            edit.Click += (s) => App.StartEditArpeggio(arp);
            edit.PointerDown += (s, e) => Arpeggio_PointerDown(s, e, arp, true, edit.Image);
            edit.PointerUp += (s, e) => Arpeggio_PointerUp(e, arp);
            edit.MarkHandledOnClick = Platform.IsMobile;
            edit.WhiteHighlight = highlightedObject == arp;

            var label = CreateLabel(panel, arp.Name, true, icon.Right + marginX, 0, edit.Left - icon.Right - marginX * 2);
            label.Font = App.SelectedArpeggio == arp ? fonts.FontMediumBold : fonts.FontMedium;
        }
        
        private void NoneArpeggio_PointerDown(Control sender, PointerEventArgs e)
        {
            if (!e.Handled && e.Left)
            {
                App.SelectedArpeggio = null;
                draggedArpeggio = null;
                envelopeDragIdx = -1;
                StartCaptureOperation(sender, e.Position, DragArpeggio);
                MarkDirty();
                e.MarkHandled();
            }
        }

        private void Arpeggio_PointerDown(Control sender, PointerEventArgs e, Arpeggio arp, bool values = false, TextureAtlasRef image = null)
        {
            var allowDrag = Platform.IsDesktop || (highlightedObject == arp && (values || sender is ImageBox));

            if (!e.Handled && e.Left && allowDrag)
            {
                if (Platform.IsDesktop)
                    App.SelectedArpeggio = arp;

                draggedArpeggio = arp;
                envelopeDragTexture = image;

                if (values)
                {
                    envelopeDragIdx = EnvelopeType.Arpeggio;
                    StartCaptureOperation(sender, e.Position, DragArpeggioValues);
                }
                else
                {
                    envelopeDragIdx = -1;
                    StartCaptureOperation(sender, e.Position, DragArpeggio);
                }

                e.MarkHandled();
            }
        }

        private void Arpeggio_PointerUp(PointerEventArgs e, Arpeggio arp)
        {
            if (!e.Handled && e.Right && arp != null)
            {
                App.ShowContextMenuAsync(new[]
                {
                    new ContextMenuOption("MenuDelete", DeleteArpeggioContext, () => { AskDeleteArpeggio(arp); }, ContextMenuSeparator.After),
                    new ContextMenuOption("MenuDuplicate", DuplicateContext, () => { DuplicateArpeggio(arp); }),
                    new ContextMenuOption("MenuReplace", ReplaceWithContext, () => { AskReplaceArpeggio(arp); }),
                    new ContextMenuOption("MenuProperties", PropertiesArpeggioContext, () => { EditArpeggioProperties(arp, true); }, ContextMenuSeparator.Before)
                });
                e.MarkHandled();
            }
        }

        private void Arpeggio_TouchClick(Control sender, PointerEventArgs e, Arpeggio arp)
        {
            if (!e.Handled)
            {
                App.SelectedArpeggio = arp;
                UpdateHighlightedItem(arp);
            }
        }

        private void CreateParamTabs(PanelContainer panel, int x, int y, int width, int height, string[] tabNames, string selelectedTabName)
        {
            var tabWidth = width / (float)tabNames.Length;
            for (int i = 0; i < tabNames.Length; i++)
            {
                var name = tabNames[i];
                var tab = new SimpleTab(name, name == selelectedTabName);
                var p0 = (int)Math.Round((i + 0) * tabWidth);
                var p1 = (int)Math.Round((i + 1) * tabWidth);
                tab.Move(x + p0, y, p1 - p0, height);
                tab.Click += (s) => { selectedInstrumentTab = name; RecreateAllControls(); };
                panel.AddControl(tab);
            }
        }

        private ParamSlider CreateParamSlider(PanelContainer panel, ParamInfo p, int y, int width)
        {
            var slider = new ParamSlider(p);
            panel.AddControl(slider);
            slider.Move(panel.Width - paramSizeX - marginX, y + Utils.DivideAndRoundUp(panelSizeY - slider.Height, 2), paramSizeX, slider.Height);
            return slider;
        }

        private ParamList CreateParamList(PanelContainer panel, ParamInfo p, int y, int height)
        {
            var list = new ParamList(p);
            panel.AddControl(list);
            list.Move(panel.Width - paramSizeX - marginX, y + Utils.DivideAndRoundUp(panelSizeY - list.Height, 2), paramSizeX, list.Height);
            return list;
        }

        private ParamCheckBox CreateParamCheckBox(PanelContainer panel, ParamInfo p, int y, int height)
        {
            var check = new ParamCheckBox(p);
            panel.AddControl(check);
            check.Move(panel.Width - check.Width - marginX, y + Utils.DivideAndRoundUp(panelSizeY - check.Height, 2));
            return check;
        }

        private void CreateParamCustomDraw(PanelContainer panel, ParamInfo p, int x, int y, int width, int height)
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
                //var indentX = marginX + expandSizeX;
                var indentX = expandSizeX;

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
                        CreateParamCustomDraw(panel, param, indentX, y, panel.Width - indentX - marginX - 1, customHeight - 1);
                        y += customHeight;
                    }
                    else
                    {
                        if (!param.IsEmpty)
                        {
                            var label = CreateLabel(panel, param.Name, true, indentX, y, panel.Width - indentX);
                            label.ToolTip = param.ToolTip; // TODO : The whole row should display the tooltip.
                            label.PointerUp += ParamLabel_PointerUp;

                            if (param.IsList)
                            {
                                var paramList = CreateParamList(panel, param, y, panelSizeY);
                                paramList.ToolTip = $"<MouseLeft> {ChangeValueTooltip}\n<MouseRight> {MoreOptionsTooltip}";
                                paramList.ValueChangeStart += (s) => App.UndoRedoManager.BeginTransaction(transScope, transObjectId, -1, param.TransactionFlags);
                                paramList.ValueChangeEnd   += (s) => App.UndoRedoManager.EndTransaction();
                                label.UserData = paramList;
                            }
                            else if (param.GetMaxValue() == 1)
                            {
                                var paramCheck = CreateParamCheckBox(panel, param, y, panelSizeY);
                                paramCheck.ToolTip = $"<MouseLeft> {ToggleValueTooltip}\n<MouseRight> {MoreOptionsTooltip}";
                                paramCheck.ValueChangeStart += (s) => App.UndoRedoManager.BeginTransaction(transScope, transObjectId, -1, param.TransactionFlags);
                                paramCheck.ValueChangeEnd   += (s) => App.UndoRedoManager.EndTransaction();
                                label.UserData = paramCheck;
                            }
                            else
                            {
                                var paramSlider = CreateParamSlider(panel, param, y, 100);
                                paramSlider.ToolTip = $"<MouseLeft><Drag> {ChangeValueTooltip} - <Ctrl><MouseLeft><Drag> {ChangeValueFineTooltip}\n<MouseRight> {MoreOptionsTooltip}";
                                paramSlider.ValueChangeStart += (s) => App.UndoRedoManager.BeginTransaction(transScope, transObjectId, -1, param.TransactionFlags);
                                paramSlider.ValueChangeEnd   += (s) => App.UndoRedoManager.EndTransaction();
                                label.UserData = paramSlider;
                            }
                        }

                        y += panelSizeY;
                    }
                }

                panel.Resize(panel.Width, y);
            }
        }

        private void ParamLabel_PointerUp(Control sender, PointerEventArgs e)
        {
            if (e.Right && !e.Handled)
            {
                (sender.UserData as ParamControl).ShowParamContextMenu();
            }
        }

        private void CreateAllSongsControls()
        {
            var project = App.Project;
            var folders = project.GetFoldersForType(FolderType.Song);
            folders.Insert(0, null);
            CreateInsertionPoint(FolderType.Song);

            foreach (var f in folders)
            {
                var songs = project.GetSongsInFolder(f == null ? null : f.Name);
                var folderExpanded = true;

                if (f != null)
                {
                    CreateFolderControls(f);
                    folderExpanded = f.Expanded;
                    CreateInsertionPoint(FolderType.Song, null, f);
                }

                if (folderExpanded)
                {
                    foreach (var song in songs)
                    {
                        CreateSongControls(song);
                        CreateInsertionPoint(FolderType.Song, song, f);
                    }
                }

                CreateFolderInsertionPoint(FolderType.Song, f);
            }
        }

        private void CreateAllInstrumentsControls()
        {
            var project = App.Project;
            var folders = project.GetFoldersForType(FolderType.Instrument);
            folders.Insert(0, null);
            CreateInsertionPoint(FolderType.Instrument);

            foreach (var f in folders)
            {
                var instruments = project.GetInstrumentsInFolder(f == null ? null : f.Name);
                var folderExpanded = true;

                if (f != null)
                {
                    CreateFolderControls(f);
                    folderExpanded = f.Expanded;
                    CreateInsertionPoint(FolderType.Instrument, null, f);
                }

                if (folderExpanded)
                {
                    foreach (var instrument in instruments)
                    {
                        CreateInstrumentControls(instrument);
                        if (instrument == expandedInstrument)
                            CreateParamsControls(instrument.Color, instrument, InstrumentParamProvider.GetParams(instrument), TransactionScope.Instrument, instrument.Id, selectedInstrumentTab);
                        CreateInsertionPoint(FolderType.Instrument, instrument, f);
                    }
                }

                CreateFolderInsertionPoint(FolderType.Instrument, f);
            }
        }

        private void CreateDpcmSamplesHeaderControls()
        {
            var project = App.Project;
            var panel = CreateGradientPanel(Theme.DarkGreyColor4);
            var addButton = CreateImageButton(panel, panel.Width - iconSizeX - marginX, "Add", false);
            addButton.ToolTip = $"<MouseLeft> {AddNewSampleTooltip}";
            addButton.Click += (s) => AskAddSampleFolder();
            addButton.RightClick += (s) => AskAddSampleFolder();
            addButton.ClickOnMouseUp = true;

            var importButton = CreateImageButton(panel, addButton.Left - iconSizeX - marginX, "InstrumentOpen", false);
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
            CreateInsertionPoint(FolderType.Sample);

            foreach (var f in folders)
            {
                var samples = project.GetSamplesInFolder(f == null ? null : f.Name);
                var folderExpanded = true;

                if (f != null)
                {
                    CreateFolderControls(f);
                    folderExpanded = f.Expanded;
                    CreateInsertionPoint(FolderType.Sample, null, f);
                }

                if (folderExpanded)
                {
                    foreach (var sample in samples)
                    {
                        CreateDpcmSampleControls(sample);
                        if (sample == expandedSample)
                            CreateParamsControls(sample.Color, sample, DPCMSampleParamProvider.GetParams(sample), TransactionScope.DPCMSample, sample.Id);
                        CreateInsertionPoint(FolderType.Sample, sample, f);
                    }
                }

                CreateFolderInsertionPoint(FolderType.Sample, f);
            }
        }

        private void CreateArpeggioHeaderControls()
        {
            var project = App.Project;
            var panel = CreateGradientPanel(Theme.DarkGreyColor4);
            var addButton = CreateImageButton(panel, panel.Width - iconSizeX - marginX, "Add", false);
            addButton.ToolTip = $"<MouseLeft> {AddNewArpeggioTooltip}";
            addButton.Click += (s) => AskAddArpeggio();
            addButton.RightClick += (s) => AskAddArpeggio();
            addButton.ClickOnMouseUp = true;

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
            CreateInsertionPoint(FolderType.Arpeggio);

            foreach (var f in folders)
            {
                var arpeggios = project.GetArpeggiosInFolder(f == null ? null : f.Name);
                var folderExpanded = true;

                if (f != null)
                {
                    CreateFolderControls(f);
                    folderExpanded = f.Expanded;
                    CreateInsertionPoint(FolderType.Arpeggio, null, f);
                }

                if (folderExpanded)
                {
                    foreach (var arp in arpeggios)
                    {
                        CreateArpeggioControls(arp);
                        CreateInsertionPoint(FolderType.Arpeggio, arp, f);
                    }
                }

                CreateFolderInsertionPoint(FolderType.Arpeggio, f);
            }
        }

        private RegisterViewerPanel CreateRegisterViewerPanel(RegisterViewerRow[] rows, int exp = -1)
        {
            var lastPanel = mainContainer.FindLastControlOfType<PanelContainer>();
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
                    CreateLabel(regHeader, RegistersExpansionHeaderLabel.Format(expName), false, regIcon.Right + marginX, 0, width);
                    lastControl = CreateRegisterViewerPanel(expRegs.RegisterRows, e);

                    // Register interpreters for each channels
                    var numChannels = expRegs.GetNumInterpreterRows(project);
                    for (int i = 0; i < numChannels; i++)
                    {
                        var chanRegs = expRegs.InterpeterRows[i];

                        if (chanRegs != null && chanRegs.Length > 0)
                        {
                            var chanHeader = CreateGradientPanel(Theme.DarkGreyColor5, null, true, lastControl);
                            var chanIcon = CreateImageBox(chanHeader, marginX, expRegs.InterpreterIcons[i]);
                            CreateLabel(chanHeader, expRegs.InterpreterLabels[i], false, chanIcon.Right + marginX, 0, width);
                            lastControl = CreateRegisterViewerPanel(chanRegs, e);
                        }
                    }
                }
            }
        }

        private void RecreateAllControls()
        {
            UpdateRenderCoords();

            if (ParentWindow == null || App == null || App.Project == null)
                return;

            RemoveAllControls();
            RemoveAllInsertionPoints();

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
            virtualSizeY = mainContainer.GetControlsRect().Bottom;
            if (scrollBar != null)
                scrollBar.VirtualSize = virtualSizeY;
            ReleasePointer();
            ClampScroll();
            SyncScrollBarToContainer();
            ValidateIntegrity();
        }

        public void RefreshButtons()
        {
            RecreateAllControls();
        }

        private void UpdateSelectedItem(Type type, object obj)
        {
            if (mainContainer != null)
            {
                foreach (var ctrl in mainContainer.Controls)
                {
                    if (ctrl is PanelContainer panel)
                    {
                        if (panel.UserData != null && panel.UserData.GetType() == type)
                        {
                            panel.FindControlOfType<Label>().Font = panel.UserData == obj ? fonts.FontMediumBold : fonts.FontMedium;
                        }
                    }
                }
            }
        }

        private void UpdateHighlightedItem(object obj)
        {
            if (Platform.IsMobile)
            {
                highlightedObject = highlightedObject == obj ? null : obj;

                foreach (var ctrl in mainContainer.Controls)
                {
                    if (ctrl is PanelContainer panel)
                    {
                        if (panel.UserData != null)
                        {
                            var highlight = panel.UserData == highlightedObject;

                            foreach (var ctrl2 in panel.Controls)
                            {
                                if (ctrl2 is ImageBox img)
                                {
                                    img.WhiteHighlight = highlight;
                                }
                                else if (ctrl2 is Button btn)
                                {
                                    btn.WhiteHighlight = highlight && btn.UserData != null;
                                }
                            }
                        }
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
            noneArpPanel.FindControlOfType<Label>().Font = App.SelectedArpeggio == null ? fonts.FontMediumBold : fonts.FontMedium;
            UpdateSelectedItem(typeof(Arpeggio), App.SelectedArpeggio);
        }

        public void InstrumentEnvelopeChanged(Instrument instrument, int envType)
        {
            if (selectedTab == TabType.Project)
            {
                // Arpeggios trigger this, but with null instruments.
                if (instrument != null)
                {
                    var env = instrument.Envelopes[envType];
                    var button = FindInstrumentEnvelopeButton(instrument, envType);
                    button.Dimmed = env.IsEmpty(envType);
                }
                else
                {
                    Debug.Assert(envType == EnvelopeType.Arpeggio);
                }
            }
        }

        public void NotifyDPCMSampleMapped()
        {
            foreach (var ctrl in mainContainer.Controls)
            {
                if ((ctrl is PanelContainer panel) && (panel.UserData is Instrument inst))
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
                if (c is PanelContainer p)
                {
                    mainContainer.ScrollY = (c.Top + c.Bottom) / 2 - mainContainer.Height / 2;
                    ClampScroll();
                    MarkDirty();
                    p.Blink();
                }
            }
        }

        protected override void OnAddedToContainer()
        {
            RecreateAllControls();
        }

        private T FindBestInsertionPoint<T>(int y, SortedDictionary<int, T> insertionPoints, out int insertY)
        {
            var minDist = int.MaxValue;
            var minElem = default(T);

            insertY = 0;

            // TODO : Binary search or something smarter.
            foreach (var kv in insertionPoints)
            {
                var dist = Math.Abs(kv.Key - y);
                if (dist < minDist)
                {
                    minDist = dist;
                    minElem = kv.Value;
                    insertY = kv.Key;
                }
            
            }
            return minElem;
        }

        // Return value is index of the button after which we should insert.
        // If folder != null, we are inserting inside a folder.
        private object GetDragInsertLocation(Point mainContainerPos, out Folder folder, out int insertY)
        {
            insertY = int.MinValue;
            folder = null;

            if (mainContainer.ClientRectangle.Contains(mainContainerPos))
            {
                mainContainerPos.Y += mainContainer.ScrollY;
                
                if (captureOperation == DragFolder)
                {
                    return FindBestInsertionPoint(mainContainerPos.Y, folderInsertionPoints[draggedFolder.Type], out insertY);
                }

                var type = -1;

                if (captureOperation == DragSong)
                {
                    type = FolderType.Song;
                }
                else if (captureOperation == DragInstrument)
                {
                    type = FolderType.Instrument;
                }
                else if (captureOperation == DragArpeggio)
                {
                    type = FolderType.Arpeggio;
                }
                else if (captureOperation == DragSample)
                {
                    type = FolderType.Sample;
                }
                else
                {
                    return null;
                }

                var ins = FindBestInsertionPoint(mainContainerPos.Y, insertionPoints[type], out insertY);
                folder = ins.Item2;
                return ins.Item1;
            }

            return null;
        }

        protected override void OnRender(Graphics g)
        {
            if (selectedTab == TabType.Registers && !window.IsAsyncDialogInProgress)
                App.ActivePlayer.GetRegisterValues(registerValues);

            base.OnRender(g);
        }

        private void MainContainer_Rendering(Graphics g)
        {
            if (captureOperation != null && captureThresholdMet)
            {
                var c = g.DefaultCommandList;

                var mainContainerPos = Platform.IsDesktop ?
                    mainContainer.ScreenToControl(CursorPosition) :
                    mainContainer.WindowToControl(ControlToWindow(mouseLastPos));

                if ((captureOperation == DragInstrumentEnvelope || 
                     captureOperation == DragArpeggioValues) && envelopeDragIdx >= 0 ||
                    (captureOperation == DragInstrumentSampleMappings))
                {
                    if (mainContainer.ClientRectangle.Contains(mainContainerPos.X, mainContainerPos.Y))
                    {
                        Debug.Assert(envelopeDragTexture != null);
                        
                        var bp = mainContainerPos - captureButtonRelPos;

                        c.DrawTextureAtlas(envelopeDragTexture, bp.X, bp.Y, iconImageScale, Color.Black.Transparent(128));

                        if (Platform.IsMobile)
                        {
                            var iconSizeX = envelopeDragTexture.ElementSize.Width  * iconImageScale;
                            var iconSizeY = envelopeDragTexture.ElementSize.Height * iconImageScale;
                            c.DrawRectangle(bp.X, bp.Y, bp.X + iconSizeX, bp.Y + iconSizeY, Theme.WhiteColor, 3, true, true);
                        }
                    }
                }
                else
                {
                    var lineColor = Theme.LightGreyColor2;
                    var drawLine = true;

                    if (captureOperation == DragSong)
                    {
                        lineColor = draggedSong.Color;
                    }
                    else if (captureOperation == DragInstrument)
                    {
                        lineColor = draggedInstrument.Color;
                    }
                    else if (captureOperation == DragSample)
                    {
                        lineColor = draggedSample.Color;
                    }
                    else if (captureOperation == DragArpeggio)
                    {
                        if (draggedArpeggio != null)
                        {
                            lineColor = draggedArpeggio.Color;
                        }
                        else
                        {
                            drawLine = false;
                        }
                    }

                    if (drawLine)
                    {
                        GetDragInsertLocation(mainContainerPos, out var draggedInFolder, out var insertY);
                        insertY -= mainContainer.ScrollY;

                        var margin = draggedInFolder != null ? expandSizeX : 0;
                        c.DrawLine(margin, insertY, mainContainer.Width - margin, insertY, lineColor, dragLineSizeY);
                    }
                }
            }
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
            mainContainer.ScrollY -= deltaY;
            MarkDirty();
            return ClampScroll();
        }

        protected void UpdateCursor()
        {
            // TODO : Add a cursor field on the capture operation directly?
            if ((captureOperation == DragInstrumentEnvelope || 
                 captureOperation == DragArpeggioValues) && captureThresholdMet)
            {
                Cursor = Cursors.CopyCursor;
            }
            else if (
                captureOperation == DragSong       ||
                captureOperation == DragInstrument ||
                captureOperation == DragArpeggio   ||
                captureOperation == DragSample     ||
                captureOperation == DragFolder)
            {
                Cursor = Cursors.DragCursor;
            }
            else
            {
                Cursor = Cursors.Default;
            }
        }

        private void ScrollIfNearEdge(Point p)
        {
            var mainContainerPoint = mainContainer.WindowToControl(ControlToWindow(p));

            int minY = Platform.IsMobile && IsLandscape ? 0 : -panelSizeY;
            int maxY = Platform.IsMobile && IsLandscape ? mainContainer.Height : mainContainer.Height + panelSizeY;

            var deltaY = 0;
            deltaY += Utils.ComputeScrollAmount(mainContainerPoint.Y, minY, panelSizeY, App.AverageTickRate * ScrollSpeedFactor, true);
            deltaY += Utils.ComputeScrollAmount(mainContainerPoint.Y, maxY, panelSizeY, App.AverageTickRate * ScrollSpeedFactor, false);
            mainContainer.ScrollY += deltaY;
            ClampScroll();
        }

        private void UpdateDragObjectOrFolder(Point p)
        {
            ScrollIfNearEdge(p);
            MarkDirty();

            if (Platform.IsDesktop && captureOperation == DragSample && !ClientRectangle.Contains(p))
            {
                DPCMSampleDraggedOutside?.Invoke(draggedSample, ControlToScreen(p));
            }
        }

        private void EndDragObjectOrFolder(Point p)
        {
            var inside = ClientRectangle.Contains(p);
            var screenPos = ControlToScreen(p);
            p.Y -= mainContainer.Top;

            if (captureOperation == DragFolder)
            {
                if (inside)
                {
                    var folderBefore = GetDragInsertLocation(p, out _, out _) as Folder;
                    if (folderBefore != draggedFolder)
                    {
                        App.UndoRedoManager.BeginTransaction(TransactionScope.Project);
                        App.Project.MoveFolder(draggedFolder, folderBefore);
                        switch (draggedFolder.Type)
                        {
                            case FolderType.Song:       App.Project.AutoSortSongs       = false; break;
                            case FolderType.Instrument: App.Project.AutoSortInstruments = false; break;
                            case FolderType.Arpeggio:   App.Project.AutoSortArpeggios   = false; break;
                            case FolderType.Sample:     App.Project.AutoSortSamples     = false; break;
                        }
                        App.UndoRedoManager.EndTransaction();
                    }
                }
            }
            else if (captureOperation == DragSong)
            {
                if (inside)
                {
                    var songBefore = GetDragInsertLocation(p, out var draggedInFolder, out _) as Song;
                    if (songBefore != draggedSong || songBefore.Folder != draggedInFolder)
                    {
                        var oldFolder = draggedSong.Folder;
                        App.UndoRedoManager.BeginTransaction(TransactionScope.ProjectNoDPCMSamples);
                        App.Project.MoveSong(draggedSong, songBefore);
                        draggedSong.FolderName = draggedInFolder == null ? null : draggedInFolder.Name;
                        if (draggedInFolder != null && !draggedInFolder.Expanded)
                        {
                            draggedInFolder.Expanded = true;
                            BlinkButton(draggedSong);
                        }
                        if (oldFolder == draggedSong.Folder)
                        {
                            App.Project.AutoSortSongs = false;
                        }
                        App.Project.ConditionalSortSongs();
                        App.UndoRedoManager.EndTransaction();
                    }
                }
            }
            else if (captureOperation == DragInstrument)
            {
                if (inside)
                {
                    var instrumentBefore = GetDragInsertLocation(p, out var draggedInFolder, out _) as Instrument;
                    if (instrumentBefore != draggedInstrument || instrumentBefore.Folder != draggedInFolder)
                    {
                        var oldFolder = draggedInstrument.Folder;
                        App.UndoRedoManager.BeginTransaction(TransactionScope.ProjectNoDPCMSamples);
                        App.Project.MoveInstrument(draggedInstrument, instrumentBefore);
                        draggedInstrument.FolderName = draggedInFolder == null ? null : draggedInFolder.Name;
                        if (draggedInFolder != null && !draggedInFolder.Expanded)
                        {
                            draggedInFolder.Expanded = true;
                            BlinkButton(draggedInstrument);
                        }
                        if (oldFolder == draggedInstrument.Folder)
                        {
                            App.Project.AutoSortInstruments = false;
                        }
                        App.Project.ConditionalSortInstruments();
                        App.UndoRedoManager.EndTransaction();
                    }
                }
                else if (Platform.IsDesktop && !inside)
                {
                    InstrumentDroppedOutside(draggedInstrument, screenPos);
                }
            }
            else if (captureOperation == DragArpeggio)
            {
                if (inside && draggedArpeggio != null)
                {
                    var arpBefore = GetDragInsertLocation(p, out var draggedInFolder, out _) as Arpeggio;
                    if (arpBefore != draggedArpeggio || draggedArpeggio.Folder != draggedInFolder)
                    {
                        var oldFolder = draggedArpeggio.Folder;
                        App.UndoRedoManager.BeginTransaction(TransactionScope.ProjectNoDPCMSamples);
                        App.Project.MoveArpeggio(draggedArpeggio, arpBefore);
                        draggedArpeggio.FolderName = draggedInFolder == null ? null : draggedInFolder.Name;
                        if (draggedInFolder != null && !draggedInFolder.Expanded)
                        {
                            draggedInFolder.Expanded = true;
                            BlinkButton(draggedArpeggio);
                        }
                        if (oldFolder == draggedArpeggio.Folder)
                        {
                            App.Project.AutoSortArpeggios = false;
                        }
                        App.Project.ConditionalSortArpeggios();
                        App.UndoRedoManager.EndTransaction();
                    }
                }
                else if (Platform.IsDesktop && !inside)
                {
                    ArpeggioDroppedOutside(draggedArpeggio, screenPos);
                }
            }
            else if (captureOperation == DragSample)
            {
                if (inside)
                {
                    var sampleBefore = GetDragInsertLocation(p, out var draggedInFolder, out _) as DPCMSample;
                    if (sampleBefore != draggedSample || draggedSample.Folder != draggedInFolder)
                    {
                        var oldFolder = draggedSample.Folder;
                        App.UndoRedoManager.BeginTransaction(TransactionScope.Project);
                        App.Project.MoveSample(draggedSample, sampleBefore);
                        draggedSample.FolderName = draggedInFolder == null ? null : draggedInFolder.Name;
                        if (draggedInFolder != null && !draggedInFolder.Expanded)
                        {
                            draggedInFolder.Expanded = true;
                            BlinkButton(draggedSample);
                        }
                        if (oldFolder == draggedSample.Folder)
                        {
                            App.Project.AutoSortSamples = false;
                        }
                        App.Project.ConditionalSortSamples();
                        App.UndoRedoManager.EndTransaction();
                    }
                }
                else if (Platform.IsDesktop && !inside)
                {
                    var mappingNote = App.GetDPCMSampleMappingNoteAtPos(screenPos, out var instrument);
                    if (instrument != null)
                    {
                        App.UndoRedoManager.BeginTransaction(TransactionScope.Instrument, instrument.Id, -1, TransactionFlags.StopAudio);
                        instrument.UnmapDPCMSample(mappingNote);
                        instrument.MapDPCMSample(mappingNote, draggedSample);
                        App.UndoRedoManager.EndTransaction();
                        DPCMSampleMapped?.Invoke(draggedSample, screenPos);
                    }
                }
            }
                
            RecreateAllControls();
        }

        private void UpdateDragInstrumentEnvelope(Point p)
        {
            ScrollIfNearEdge(p);
            MarkDirty();
        }

        private void EndDragInstrumentEnvelope(Point p)
        {
            var windowPoint = ControlToWindow(p);
            var mainContainerPoint = mainContainer.WindowToControl(windowPoint);
            var panel = mainContainer.FindControlOfTypeAt<PanelContainer>(windowPoint.X, windowPoint.Y);

            if (mainContainer.ClientRectangle.Contains(mainContainerPoint) && panel != null)
            {
                var instrumentSrc = draggedInstrument;
                var instrumentDst = panel.UserData as Instrument;

                if (instrumentSrc != instrumentDst && instrumentSrc != null && instrumentDst != null && envelopeDragIdx != -1)
                {
                    if (instrumentSrc.Expansion == instrumentDst.Expansion)
                    {
                        Platform.MessageBoxAsync(ParentWindow, CopyInstrumentEnvelopeMessage.Format(EnvelopeType.LocalizedNames[envelopeDragIdx], instrumentSrc.Name, instrumentDst.Name), CopyInstrumentEnvelopeTitle, MessageBoxButtons.YesNo, (r) =>
                        {
                            if (r == DialogResult.Yes)
                            {
                                App.UndoRedoManager.BeginTransaction(TransactionScope.Instrument, instrumentDst.Id);
                                var oldEnvelopeDst = instrumentDst.Envelopes[envelopeDragIdx];
                                instrumentDst.Envelopes[envelopeDragIdx] = instrumentSrc.Envelopes[envelopeDragIdx].ShallowClone();
                                instrumentDst.Envelopes[envelopeDragIdx].ClampToValidRange(instrumentDst, envelopeDragIdx);

                                // HACK : Copy some envelope related stuff. Need to cleanup the envelope code.
                                switch (envelopeDragIdx)
                                {
                                    case EnvelopeType.FdsWaveform:
                                        instrumentDst.FdsWavePreset  = instrumentSrc.FdsWavePreset;
                                        break;
                                    case EnvelopeType.FdsModulation:
                                        instrumentDst.FdsModPreset   = instrumentSrc.FdsModPreset;
                                        break;
                                    case EnvelopeType.N163Waveform:
                                        instrumentDst.N163WavePreset = instrumentSrc.N163WavePreset;
                                        instrumentDst.N163WaveSize   = instrumentSrc.N163WaveSize;
                                        break;
                                }

                                App.UndoRedoManager.EndTransaction();

                                // Update envelope button.
                                var newEnvelopeDst = instrumentDst.Envelopes[envelopeDragIdx];
                                var button = FindInstrumentEnvelopeButton(instrumentDst, envelopeDragIdx);
                                button.Dimmed = newEnvelopeDst.IsEmpty(envelopeDragIdx);
                                
                                Debug.Assert((string)button.UserData == EnvelopeType.InternalNames[envelopeDragIdx]);

                                if (Platform.IsDesktop)
                                    App.StartEditInstrument(instrumentDst, envelopeDragIdx);
                            }
                        });
                    }
                    else
                    {
                        App.DisplayNotification($"Incompatible audio expansion!"); ;
                    }
                }
            }
        }

        private void UpdateDragInstrumentSampleMappings(Point p)
        {
            ScrollIfNearEdge(p);
            MarkDirty();
        }

        private void EndDragInstrumentSampleMappings(Point p)
        {
            var windowPoint = ControlToWindow(p);
            var mainContainerPoint = mainContainer.WindowToControl(windowPoint);
            var panel = mainContainer.FindControlOfTypeAt<PanelContainer>(windowPoint.X, windowPoint.Y);

            if (mainContainer.ClientRectangle.Contains(mainContainerPoint) && panel != null)
            {
                var instrumentSrc = draggedInstrument;
                var instrumentDst = panel.UserData as Instrument;

                if (instrumentSrc != instrumentDst && instrumentSrc != null && instrumentDst != null && instrumentDst.Expansion == ExpansionType.None)
                {
                    Platform.MessageBoxAsync(ParentWindow, CopyInstrumentSamplesMessage.Format(instrumentSrc.Name, instrumentDst.Name), CopyInstrumentSamplesTitle, MessageBoxButtons.YesNo, (r) =>
                    {
                        if (r == DialogResult.Yes)
                        {
                            App.UndoRedoManager.BeginTransaction(TransactionScope.Instrument, instrumentDst.Id);
                            instrumentDst.SamplesMapping.Clear();
                            foreach (var mapping in instrumentSrc.SamplesMapping)
                                instrumentDst.MapDPCMSample(mapping.Key, mapping.Value.Sample, mapping.Value.Pitch, mapping.Value.Loop);
                            App.UndoRedoManager.EndTransaction();

                            // Update DPCM button.
                            var button = FindInstrumentDpcmButton(instrumentDst);
                            button.Dimmed = !instrumentDst.HasAnyMappedSamples;

                            if (Platform.IsDesktop)
                                App.StartEditDPCMMapping(instrumentDst);
                        }
                    });
                }
            }
        }

        private void UpdateDragArpeggioValues(Point p)
        {
            ScrollIfNearEdge(p);
            MarkDirty();
        }

        private void EndDragArpeggioValues(Point p)
        {
            var windowPoint = ControlToWindow(p);
            var mainContainerPoint = mainContainer.WindowToControl(windowPoint);
            var panel = mainContainer.FindControlOfTypeAt<PanelContainer>(windowPoint.X, windowPoint.Y);

            if (mainContainer.ClientRectangle.Contains(mainContainerPoint) && panel != null)
            {
                var arpeggioSrc = draggedArpeggio;
                var arpeggioDst = panel.UserData as Arpeggio; 

                if (arpeggioSrc != arpeggioDst && arpeggioSrc != null && arpeggioDst != null && envelopeDragIdx != -1)
                {
                    Platform.MessageBoxAsync(ParentWindow, CopyArpeggioMessage.Format(arpeggioSrc.Name, arpeggioDst.Name), CopyArpeggioTitle, MessageBoxButtons.YesNo, (r) =>
                    {
                        if (r == DialogResult.Yes)
                        {
                            App.UndoRedoManager.BeginTransaction(TransactionScope.Arpeggio, arpeggioDst.Id);
                            arpeggioDst.Envelope.Length = arpeggioSrc.Envelope.Length;
                            arpeggioDst.Envelope.Loop = arpeggioSrc.Envelope.Loop;
                            Array.Copy(arpeggioSrc.Envelope.Values, arpeggioDst.Envelope.Values, arpeggioDst.Envelope.Values.Length);
                            App.UndoRedoManager.EndTransaction();

                            if (Platform.IsDesktop)
                                App.StartEditArpeggio(arpeggioDst);
                        }
                    });
                }
            }
        }

        private void UpdateCaptureOperation(Point p, bool realTime = false, float delta = 0.0f)
        {
            const int CaptureThreshold = Platform.IsDesktop ? 5 : 50;

            if (captureOperation != null && !captureThresholdMet)
            {
                var mouseDelta = p - captureMousePos;

                if (Math.Abs(mouseDelta.X) >= CaptureThreshold ||
                    Math.Abs(mouseDelta.Y) >= CaptureThreshold)
                {
                    captureThresholdMet = true;
                }
            }

            if (captureOperation != null && realTime)
            {
                captureDuration += delta;
            }

            if (captureOperation != null && captureThresholdMet && (captureOperation.RealTimeUpdate || !realTime))
            {
                if (captureOperation == DragInstrumentEnvelope)
                {
                    UpdateDragInstrumentEnvelope(p);
                }
                else if (captureOperation == DragInstrumentSampleMappings)
                {
                    UpdateDragInstrumentSampleMappings(p);
                }
                else if (captureOperation == DragArpeggioValues)
                {
                    UpdateDragArpeggioValues(p);
                }
                else if (
                    captureOperation == DragSong       ||
                    captureOperation == DragInstrument ||
                    captureOperation == DragArpeggio   ||
                    captureOperation == DragSample     ||
                    captureOperation == DragFolder)
                {
                    UpdateDragObjectOrFolder(p);
                }
                else if (captureOperation == MobilePan)
                {
                    // This can happen if a control captures after the initial pointer-down. 
                    // The slider is an example of this, since it has a bit of slop.
                    if (CheckPointerCaptureCookie(captureCookie))
                    {
                        DoScroll(p.Y - mouseLastPos.Y);
                    }
                }
                else
                {
                    MarkDirty();
                }
            }
        }

        /*
        protected override void OnPointerMove(PointerEventArgs e)
        {
            if (e.IsTouchEvent)
            {
                OnTouchMove(e);
                return;
            }

            base.OnPointerMove(e);

            Debug.WriteLine($"MOVE {e.X} {e.Y}");

            UpdateCursor();
            UpdateCaptureOperation(e.Position);

            mouseLastPos = e.Position;

            Debug.WriteLine($"PMY = {e.Position.Y}");
        }
        */

        protected override void OnPointerLeave(EventArgs e)
        {
            App.SequencerShowExpansionIcons = false;
        }

        /*
        protected override void OnPointerUp(PointerEventArgs e)
        {
            if (e.IsTouchEvent)
            {
                OnTouchUp(e);
                return;
            }

            UpdateCursor();
        }
        */

        private void StartCaptureOperation(Control control, Point p, CaptureOperation op)
        {
            Debug.Assert(captureOperation == null);
            var ctrlPos = WindowToControl(control.ControlToWindow(p));
            mouseLastPos = ctrlPos;
            captureMousePos = ctrlPos;
            captureButtonRelPos = p;
            captureScrollY = mainContainer.ScrollY;
            captureCookie = control.CapturePointer();
            canFling = false;
            captureOperation = op;
            captureThresholdMet = !op.NeedsThreshold;
            captureDuration = 0.0f;
        }

        private void EndCaptureOperation(Point p)
        {
            if (captureOperation != null && captureThresholdMet)
            {
                if (captureOperation == DragInstrumentEnvelope)
                {
                    EndDragInstrumentEnvelope(p);
                }
                else if (captureOperation == DragInstrumentSampleMappings)
                {
                    EndDragInstrumentSampleMappings(p);
                }
                else if (captureOperation == DragArpeggioValues)
                {
                    EndDragArpeggioValues(p);
                }
                else if (
                    captureOperation == DragSong ||
                    captureOperation == DragInstrument ||
                    captureOperation == DragArpeggio ||
                    captureOperation == DragSample ||
                    captureOperation == DragFolder)
                {
                    EndDragObjectOrFolder(p);
                }
                else if (captureOperation == MobilePan)
                {
                    canFling = true;
                }
                else
                {
                    MarkDirty();
                }
            }

            draggedArpeggio = null;
            draggedInstrument = null;
            draggedSample = null;
            draggedSong = null;
            envelopeDragTexture = null;
            captureOperation = null;
            ReleasePointer();
            MarkDirty();
        }

        // TODO : This is never called? Suspicious. In 4.2.x, this was called on long press + double click.
        private void AbortCaptureOperation()
        {
            if (App.UndoRedoManager.HasTransactionInProgress)
                App.UndoRedoManager.AbortTransaction();

            MarkDirty();

            draggedArpeggio = null;
            draggedInstrument = null;
            draggedSample = null;
            draggedSong = null;
            envelopeDragTexture = null;
            captureOperation = null;
            canFling = false;
            ReleasePointer();
        }

        //protected override void OnMouseWheel(PointerEventArgs e)
        //{
        //    if (!e.Handled)
        //    {
        //        DoScroll(e.ScrollY > 0 ? panelSizeY * 3 : -panelSizeY * 3);
        //        e.MarkHandled();
        //    }
        //}

        public override void OnContainerMouseWheelNotify(Control control, PointerEventArgs e)
        {
            if (!e.Handled)
            {
                DoScroll(e.ScrollY > 0 ? panelSizeY * 3 : -panelSizeY * 3);
                e.MarkHandled();
            }
        }

        public override void OnContainerPointerMoveNotify(Control control, PointerEventArgs e)
        {
            var winPos = control.ControlToWindow(e.Position);
            var ctrlPos = WindowToControl(winPos);
            var ctrl = GetControlAt(winPos.X, winPos.Y, out _, out _);

            if (ctrl != null)
            {
                var tooltip = ctrl.ToolTip;
                if (string.IsNullOrEmpty(tooltip))
                    tooltip = ctrl.ParentContainer.ToolTip;
                App.SetToolTip(tooltip);

                if (!Platform.IsMobile)
                {
                    App.SequencerShowExpansionIcons = (ctrl.UserData is Instrument) || (ctrl.ParentContainer.UserData is Instrument);
                }
            }

            UpdateCursor();
            UpdateCaptureOperation(ctrlPos);

            // HACK : Do the middle scroll here instead of in the main container to avoid changing the scroll BEFORE
            // we recalculate the position (ControlToWindow/WindowToControl above.
            bool middle = e.Middle || (e.Left && ModifierKeys.IsAltDown && Settings.AltLeftForMiddle);

            if (middle && control.IsInContainer(mainContainer))
            {
                DoScroll(ctrlPos.Y - mouseLastPos.Y);
            }

            mouseLastPos = ctrlPos;
        }

        private void ResizeMainContainer()
        {
            if (mainContainer != null)
            {
                mainContainer.Resize(mainContainer.Width, height - topTabSizeY);
                if (scrollBar != null)
                    scrollBar.Resize(scrollBar.ScrollBarThickness, height - topTabSizeY - 1);
            }
        }

        protected override void OnResize(EventArgs e)
        {
            if (Platform.IsDesktop)
            {
                ResizeMainContainer();
                UpdateRenderCoords();
                ClampScroll();
            }
            else
            {
                RecreateAllControls();
            }
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
                            dlg.Properties.AddCheckBoxList(ImportSongsLabel.Colon, songsNames.ToArray(), null, CheckBoxSelectAllTooltip, 15); // 0
                            dlg.Properties.Build();

                            dlg.ShowDialogAsync((r) =>
                            {
                                if (r == DialogResult.OK)
                                {
                                    App.UndoRedoManager.BeginTransaction(TransactionScope.Project, TransactionFlags.StopAudio);

                                    var selected = dlg.Properties.GetPropertyValue<bool[]>(0);
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
                MobileProjectDialog dlg = new MobileProjectDialog(App, ImportSongsTitle, false, true);
                dlg.ShowDialogAsync((f) =>
                {
                    // HACK : We don't support nested activities right now, so return this special code to signal that we should open from storage.
                    if (f == "///STORAGE///")
                        Platform.StartMobileLoadFileOperationAsync(new[] { "fms", "txt", "ftm" }, (fs) => { ImportSongsAction(fs); });
                    else
                        ImportSongsAction(f);
                });
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

                                var dlg = new PropertyDialog(ParentWindow, ImportInstrumentsTitle, 350);
                                dlg.Properties.AddCheckBoxList(ImportInstrumentsLabel.Colon, instrumentNames.ToArray(), null, CheckBoxSelectAllTooltip, 15); // 0
                                dlg.Properties.Build();

                                dlg.ShowDialogAsync((r) =>
                                {
                                    if (r == DialogResult.OK)
                                    {
                                        var selected = dlg.Properties.GetPropertyValue<bool[]>(0);
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
                MobileProjectDialog dlg = new MobileProjectDialog(App, ImportInstrumentsTitle, false, true);
                dlg.ShowDialogAsync((f) => 
                {
                    // HACK : We don't support nested activities right now, so return this special code to signal that we should open from storage.
                    if (f == "///STORAGE///")
                        Platform.StartMobileLoadFileOperationAsync(new[] { "fms", "fti", "txt", "ftm", "bti", "opni" }, (fs) => { ImportInstrumentsAction(fs); });
                    else
                        ImportInstrumentsAction(f); 
                });
            }
            else
            {
                var filename = Platform.ShowOpenFileDialog("Open File", "All Instrument Files (*.fti;*.fms;*.txt;*.ftm;*.bti;*.opni)|*.fti;*.fms;*.txt;*.ftm;*.bti;*.opni|FamiTracker Instrument File (*.fti)|*.fti|BambooTracker Instrument File (*.bti)|*.bti|OPN Instrument File (*.opni)|*.opni|FamiStudio Files (*.fms)|*.fms|FamiTracker Files (*.ftm)|*.ftm|FamiTracker Text Export (*.txt)|*.txt|FamiStudio Text Export (*.txt)|*.txt", ref Settings.LastInstrumentFolder);
                ImportInstrumentsAction(filename);
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

                        if (ext == ".fms")
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
                                dlg.Properties.AddCheckBoxList(ImportSamplesLabel.Colon, samplesNames.ToArray(), null, CheckBoxSelectAllTooltip, 15); // 0
                                dlg.Properties.Build();

                                dlg.ShowDialogAsync((r) =>
                                {
                                    if (r == DialogResult.OK)
                                    {
                                        var selected = dlg.Properties.GetPropertyValue<bool[]>(0);
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

                            RecreateAllControls();
                            if (importedSamples.Count != 0)
                                BlinkButton(importedSamples[0]);
                            App.UndoRedoManager.EndTransaction();
                        }
                        App.EndLogTask();
                    }
                }
            };

            if (Platform.IsMobile)
            {
                Platform.StartMobileLoadFileOperationAsync(new [] { "dmc", "wav", "fms" }, (f) => LoadDPCMSampleAction(new[] { f }));
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
            EnsureFolderExpanded(FolderType.Song, folder);
            RecreateAllControls();
            BlinkButton(App.SelectedSong);
            App.UndoRedoManager.EndTransaction();
        }

        private void AskAddSong()
        {
            App.ShowContextMenuAsync(new[]
            {
                new ContextMenuOption("Music", AddSongContext, () => { AddSong(); }),
                new ContextMenuOption("Folder", AddFolderContext, () => { AddFolder(FolderType.Song); }, ContextMenuSeparator.Before)
            });
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

        private void EnsureFolderExpanded(int type, string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                App.Project.GetFolder(type, name).Expanded = true;
            }
        }

        private void AddInstrument(int expansionType)
        {
            var folder = App.SelectedInstrument != null ? App.SelectedInstrument.FolderName : null;
            App.UndoRedoManager.BeginTransaction(TransactionScope.ProjectNoDPCMSamples);
            App.SelectedInstrument = App.Project.CreateInstrument(expansionType);
            App.SelectedInstrument.FolderName = folder;
            EnsureFolderExpanded(FolderType.Instrument, folder);
            RecreateAllControls();
            BlinkButton(App.SelectedInstrument);
            App.UndoRedoManager.EndTransaction();
        }

        private void AddFolder(int type)
        {
            App.UndoRedoManager.BeginTransaction(type == FolderType.Sample ? TransactionScope.Project : TransactionScope.ProjectNoDPCMSamples);
            var folder = App.Project.CreateFolder(type);
            RecreateAllControls();
            BlinkButton(folder);
            App.UndoRedoManager.EndTransaction();
        }

        private void AskAddInstrument()
        {
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

            App.ShowContextMenuAsync(options.ToArray());
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
                    if (inst == highlightedObject)
                        highlightedObject = null;
                    InstrumentDeleted?.Invoke(inst);
                    App.UndoRedoManager.EndTransaction();
                    RecreateAllControls();
                }
            });
        }

        private PanelContainer FindInstrumentPanel(Instrument inst)
        {
            return mainContainer.FindControlByUserData(inst) as PanelContainer;
        }

        private Button FindInstrumentEnvelopeButton(Instrument inst, int envType)
        {
            return FindInstrumentPanel(inst).FindControlByUserData(EnvelopeType.InternalNames[envType]) as Button;
        }

        private Button FindInstrumentDpcmButton(Instrument inst)
        {
            return FindInstrumentPanel(inst).FindControlByUserData("DPCM") as Button;
        }

        private void ClearInstrumentEnvelope(Instrument inst, int envelopeType)
        {
            var env = inst.Envelopes[envelopeType];
            App.UndoRedoManager.BeginTransaction(TransactionScope.Instrument, inst.Id);
            env.ResetToDefault(envelopeType);
            inst.NotifyEnvelopeChanged(envelopeType, true);
            App.UndoRedoManager.EndTransaction();
            FindInstrumentEnvelopeButton(inst, envelopeType).Dimmed = env.IsEmpty(envelopeType);
            MarkDirty();
        }

        private void AddArpeggio()
        {
            var folder = App.SelectedArpeggio != null ? App.SelectedArpeggio.FolderName : null;
            App.UndoRedoManager.BeginTransaction(TransactionScope.ProjectNoDPCMSamples);
            App.SelectedArpeggio = App.Project.CreateArpeggio();
            App.SelectedArpeggio.FolderName = folder;
            EnsureFolderExpanded(FolderType.Arpeggio, folder);
            RecreateAllControls();
            BlinkButton(App.SelectedArpeggio);
            App.UndoRedoManager.EndTransaction();
        }

        private void AskAddArpeggio()
        {
            App.ShowContextMenuAsync(new[]
            {
                new ContextMenuOption("Music", AddArpeggioContext, () => { AddArpeggio(); }),
                new ContextMenuOption("Folder", AddFolderContext, () => { AddFolder(FolderType.Arpeggio); }, ContextMenuSeparator.Before)
            });
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

        private void AskAddSampleFolder()
        {
            App.ShowContextMenuAsync(new[]
            {
                new ContextMenuOption("Folder", AddFolderContext, () => { AddFolder(FolderType.Sample); }, ContextMenuSeparator.Before)
            });
        }

        private void UpdateDPCMSampleSourceData(DPCMSample sample)
        {
            var filename = Platform.ShowOpenFileDialog("Open File", "All Sample Files (*.wav;*.dmc)|*.wav;*.dmc|Wav Files (*.wav)|*.wav|DPCM Sample Files (*.dmc)|*.dmc", ref Settings.LastSampleFolder);

            if (filename != null)
            {
                ReloadDPCMSampleSourceData(sample, filename);
            }
        }

        private void ReloadDPCMSampleSourceData(DPCMSample sample, string newFilename = null)
        {
            var filename = string.IsNullOrEmpty(newFilename) ? sample.SourceFilename : newFilename;

            if (File.Exists(filename))
            {
                if (sample.SourceDataIsWav || Path.GetExtension(filename).Equals(".wav", StringComparison.CurrentCultureIgnoreCase))
                {
                    var wavData = WaveFile.Load(filename, out var sampleRate);
                    if (wavData != null)
                    {
                        var maximumSamples = sampleRate * 2;
                        if (wavData.Length > maximumSamples)
                            Array.Resize(ref wavData, maximumSamples);

                        App.UndoRedoManager.BeginTransaction(TransactionScope.DPCMSample, sample.Id);
                        sample.SetWavSourceData(wavData, sampleRate, filename, false);
                        sample.Process();
                        App.UndoRedoManager.EndTransaction();
                    }
                }
                else
                {
                    var dmcData = File.ReadAllBytes(filename);
                    if (dmcData.Length > DPCMSample.MaxSampleSize)
                        Array.Resize(ref dmcData, DPCMSample.MaxSampleSize);

                    App.UndoRedoManager.BeginTransaction(TransactionScope.DPCMSample, sample.Id);
                    sample.SetDmcSourceData(dmcData, filename, false);
                    sample.Process();
                    App.UndoRedoManager.EndTransaction();
                }

                DPCMSampleReloaded?.Invoke(sample);
            }
            else
            {
                App.DisplayNotification(CantFindSourceFileError.Format(filename));
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
            dlg.Properties.AddDropDownList(TargetBankSizeLabel.Colon, new[] { "4KB", "8KB", "16KB" }, "4KB", null, PropertyFlags.ForceFullWidth); // 0
            dlg.Properties.Build();

            dlg.ShowDialogAsync((r) =>
            {
                if (r == DialogResult.OK)
                {
                    App.UndoRedoManager.BeginTransaction(TransactionScope.Project);
                    var bankSize = Utils.ParseIntWithTrailingGarbage(dlg.Properties.GetPropertyValue<string>(0)) * 1024;
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
                dlg.Properties.AddRadioButtonList(AskReplaceInstrumentMessage.Format(inst.Name), instrumentNames.ToArray(), 0, null, 12, PropertyFlags.MultiLineLabel); // 0
                dlg.Properties.Build();

                for (int i = 0; i < instrumentColors.Count; i++)
                    dlg.Properties.SetRowColor(0, i, instrumentColors[i]);

                dlg.ShowDialogAsync((r) =>
                {
                    if (r == DialogResult.OK)
                    {
                        App.UndoRedoManager.BeginTransaction(TransactionScope.ProjectNoDPCMSamples);
                        App.Project.ReplaceInstrument(inst, App.Project.GetInstrument(instrumentNames[dlg.Properties.GetSelectedIndex(0)]));
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
            RecreateAllControls();
            BlinkButton(newInstrument);
            App.UndoRedoManager.EndTransaction();
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
                Platform.StartMobileLoadFileOperationAsync(new[] { "wav" } , (f) => LoadWavFileAction(f));
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
                dlg.Properties.AddRadioButtonList(AskReplaceArpeggioMessage.Format(arp.Name), arpeggioNames.ToArray(), 0, null, 12, PropertyFlags.MultiLineLabel); // 0
                dlg.Properties.Build();

                for (int i = 0; i < arpeggioColors.Count; i++)
                    dlg.Properties.SetRowColor(0, i, arpeggioColors[i]);

                dlg.ShowDialogAsync((r) =>
                {
                    if (r == DialogResult.OK)
                    {
                        App.UndoRedoManager.BeginTransaction(TransactionScope.ProjectNoDPCMSamples);
                        App.Project.ReplaceArpeggio(arp, App.Project.GetArpeggio(arpeggioNames[dlg.Properties.GetSelectedIndex(0)]));
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

        //protected void OnTouchMove(PointerEventArgs e)
        //{
        //    UpdateCursor();
        //    UpdateCaptureOperation(e.Position);
        //    mouseLastPos = e.Position;
        //}

        //protected void OnTouchUp(PointerEventArgs e)
        //{
        //    EndCaptureOperation(e.Position);
        //}

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
            ValidateIntegrity();
            TickFling(delta);
            UpdateCaptureOperation(mouseLastPos, true, delta);
#if DEBUG
            ValidateIntegrity();
#endif
        }

        private void EditProjectProperties()
        {
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
                    var tuning = dlg.Tuning;

                    var changedTempoMode = tempoMode != project.TempoMode;
                    var changedExpansion = newExpansionMask != project.ExpansionAudioMask;
                    var changedNumChannels = numN163Channels != project.ExpansionNumN163Channels;
                    var changedAuthoringMachine = palAuthoring != project.PalMode;
                    var changedTuning = tuning != project.Tuning;
                    var changedExpMixer = dlg.MixerProperties.Changed;

                    var transFlags = TransactionFlags.None;

                    if (changedExpansion)
                        transFlags = TransactionFlags.RecreatePlayers | TransactionFlags.RecreateStreams; // Toggling EPSM will change mono/stereo and requires new audiostreams.
                    else if (changedAuthoringMachine || changedNumChannels || changedExpMixer || changedTuning)
                        transFlags = TransactionFlags.RecreatePlayers;
                    else if (changedTempoMode)
                        transFlags = TransactionFlags.StopAudio;

                    App.UndoRedoManager.BeginTransaction(TransactionScope.ProjectNoDPCMSamples, transFlags);

                    project.Name = dlg.Title;
                    project.Author = dlg.Author;
                    project.Copyright = dlg.Copyright;
                    project.Tuning = tuning;

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

        private Point GetPropertiesDialogPosition(bool ctx)
        {
            return ctx ? ParentWindow.LastContextMenuPosition : ParentWindow.LastMousePosition;
        }

        private void EditSongProperties(Song song, bool ctx = false)
        {
            var dlg = new PropertyDialog(ParentWindow, SongPropertiesTitle, GetPropertiesDialogPosition(ctx), 320, true); 

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

        private void EditInstrumentProperties(Instrument instrument, bool ctx = false)
        {
            var pt = GetPropertiesDialogPosition(ctx);
            var dlg = new PropertyDialog(ParentWindow, InstrumentPropertiesTitle, pt, 240, true, pt.Y > ParentWindowSize.Height / 2, false);
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

        private void EditFolderProperties(Folder folder, bool ctx = false)
        {
            var pt = GetPropertiesDialogPosition(ctx);
            var dlg = new PropertyDialog(ParentWindow, FolderPropertiesTitle, pt, 240, true, pt.Y > ParentWindowSize.Height / 2, false);
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

        private void EditArpeggioProperties(Arpeggio arpeggio, bool ctx = false)
        {
            var pt = GetPropertiesDialogPosition(ctx);
            var dlg = new PropertyDialog(ParentWindow, ArpeggioPropertiesTitle, pt, 240, true, pt.Y > ParentWindowSize.Height / 2, false);
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

        private void EditDPCMSampleProperties(DPCMSample sample, bool ctx =false)
        {
            var pt = GetPropertiesDialogPosition(ctx);
            var dlg = new PropertyDialog(ParentWindow, SamplePropertiesTitle, pt, 240, true, pt.Y > ParentWindowSize.Height / 2, false);
            dlg.Properties.AddColoredTextBox(sample.Name, sample.Color); // 0
            dlg.Properties.AddColorPicker(sample.Color); // 1
            dlg.Properties.Build();

            dlg.ShowDialogAsync((r) =>
            {
                if (r == DialogResult.OK)
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
                }
            });
        }

        private void ValidateControlUserData(Control ctrl)
        {
            if (ctrl.UserData != null)
            {
                if (ctrl.UserData is Instrument inst)
                {
                    Debug.Assert(App.Project.InstrumentExists(inst));
                }
                else if (ctrl.UserData is Folder folder)
                {
                    Debug.Assert(App.Project.FolderExists(folder));
                }
                else if (ctrl.UserData is Arpeggio arp)
                {
                    Debug.Assert(App.Project.ArpeggioExists(arp));
                }
                else if (ctrl.UserData is DPCMSample sample)
                {
                    Debug.Assert(App.Project.SampleExists(sample));
                }
                else if (ctrl.UserData is Song song)
                {
                    Debug.Assert(App.Project.SongExists(song));
                }
                else if (ctrl.UserData is Project project)
                {
                    Debug.Assert(App.Project == project);
                }
            }
        }

        private void ValidateContainerUserData(Container container)
        {
            foreach (var c in container.Controls)
            {
                ValidateControlUserData(c);

                if (c is Container cont)
                {
                    ValidateContainerUserData(cont);
                }
            }
        }

        public void ValidateIntegrity()
        {
#if DEBUG
            if (highlightedObject != null || App.Project == null)
            {
                // TODO : We should properly clear the highlighted object whenever we delete something.
                if (highlightedObject is Song song)
                    Debug.Assert(App.Project.SongExists(song));
                if (highlightedObject is Instrument inst)
                    Debug.Assert(App.Project.InstrumentExists(inst));
                if (highlightedObject is DPCMSample sample)
                    Debug.Assert(App.Project.SampleExists(sample));
                if (highlightedObject is Arpeggio arp)
                    Debug.Assert(App.Project.ArpeggioExists(arp));
            }

            ValidateContainerUserData(this);
#endif
        }

        public void Serialize(ProjectBuffer buffer)
        {
            var scrollY = mainContainer.ScrollY;
            buffer.Serialize(ref scrollY);
            buffer.Serialize(ref expandedInstrument);
            buffer.Serialize(ref selectedInstrumentTab);
            buffer.Serialize(ref expandedSample);

            if (buffer.IsReading)
            {
                captureOperation = null;
                flingVelY = 0.0f;
                mainContainer.ScrollY = scrollY;
                highlightedObject = null;

                ReleasePointer();
                ClampScroll();
                RecreateAllControls();
                BlinkButton(null);
            }
        }
    }
}
