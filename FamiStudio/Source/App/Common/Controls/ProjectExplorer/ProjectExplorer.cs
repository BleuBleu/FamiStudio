using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Threading.Tasks;

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
        private const int   DefaultParamSizeX       = Platform.IsMobile ? 84 : 104; // MATTT : Review.
        private const float ScrollSpeedFactor       = Platform.IsMobile ? 2.0f : 1.0f;

        private int expandSizeX;
        private int spacingX; // Spacing between envelope buttons.
        private int marginX; // Spacing between other stuff.
        private int iconSizeX;
        private int panelSizeY;
        private int paramSizeX;
        private int virtualSizeY; // MATTT : Move this functionality to container directly.
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

        private int mouseLastX = 0; // MATTT : Make this a pOint
        private int mouseLastY = 0;
        private int captureMouseX = -1; // MATTT : Make this a pOint
        private int captureMouseY = -1;
        private int captureButtonRelX = -1;
        private int captureButtonRelY = -1;
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
        private GradientPanel tabPanel;
        private Container mainContainer;
        private ScrollBar scrollBar;
        private GradientPanel noneArpPanel;

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
            insertionPoints[t][mainContainer.FindLastControlOfType<GradientPanel>().Bottom] = (o, f);
        }

        private void CreateFolderInsertionPoint(int t, Folder f)
        {
            folderInsertionPoints[t][mainContainer.FindLastControlOfType<GradientPanel>().Bottom] = f;
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

        private Label CreateLabel(GradientPanel panel, string text, bool black, int x, int y, int width, bool ellipsis = false)
        {
            var label = new Label(text, false);
            label.Color = black ? Theme.BlackColor : label.Color;
            label.Ellipsis = ellipsis;
            //label.SendTouchInputAsMouse = true;
            label.Move(x, y, width, panel.Height);
            panel.AddControl(label);
            return label;
        }

        private Button CreateExpandButton(GradientPanel panel, bool black, bool expanded)
        {
            Debug.Assert(
                panel.UserData is Folder     ||
                panel.UserData is Instrument || 
                panel.UserData is DPCMSample);

            var expandButton = CreateImageButton(panel, marginX, expanded ? "InstrumentExpanded" : "InstrumentExpand", black);
            //expandButton.SendTouchInputAsMouse = true;
            return expandButton;
        }

        private ImageBox CreateImageBox(GradientPanel panel, int x, string image, bool black = false)
        {
            var imageBox = new ImageBox(image);
            panel.AddControl(imageBox);
            imageBox.Tint = black ? Color.Black : Theme.LightGreyColor2;
            imageBox.ImageScale = iconImageScale;
            imageBox.AutoSizeToImage();
            imageBox.Move(x, Utils.DivideAndRoundUp(panel.Height - imageBox.Height, 2));
            return imageBox;
        }

        private Button CreateImageButton(GradientPanel panel, int x, string image, bool black = true)
        {
            var button = new Button(image, null);
            button.Transparent = true;
            //button.SendTouchInputAsMouse = true;
            button.ImageScale = iconImageScale;
            if (black)
            {
                button.ForegroundColorEnabled  = Color.Black;
                button.ForegroundColorDisabled = Color.Black;
            }
            panel.AddControl(button);
            button.AutoSizeToImage();
            button.Move(x, Utils.DivideAndRoundUp(panel.Height - button.Height, 2));

            return button;
        }

        private void CreateFolderControls(Folder folder)
        {
            var panel = CreateGradientPanel(Theme.DarkGreyColor5, folder);
            panel.ToolTip = $"<MouseRight> {MoreOptionsTooltip}";
            panel.MouseUp += (s, e) => Folder_MouseUp(e, folder);
            panel.ContainerMouseUpNotify += (s, e) => Folder_MouseUp(e, folder);

            var expand = CreateExpandButton(panel, false, folder.Expanded);
            expand.ToolTip = $"<MouseLeft> {ExpandTooltip} - <MouseRight> {MoreOptionsTooltip}";
            expand.Click += (s) => ToggleExpandFolder(folder);

            var icon = CreateImageBox(panel, expand.Right + spacingX, folder.Expanded ? "FolderOpen" : "Folder");
            var propsButton = CreateImageButton(panel, panel.Width - iconSizeX - marginX, "Properties", false);
            propsButton.ToolTip = $"<MouseLeft> {PropertiesFolderTooltip}";
            propsButton.Click += (s) => EditFolderProperties(folder);

            CreateLabel(panel, folder.Name, false, icon.Right + marginX, 0, propsButton.Left - icon.Right - marginX * 2, true);
        }

        private void Folder_MouseUp(MouseEventArgs e, Folder folder)
        {
            if (!e.Handled && e.Right)
            {
                App.ShowContextMenu(new[]
                {
                    new ContextMenuOption("MenuDelete", DeleteFolderContext, () => { AskDeleteFolder(folder); }, ContextMenuSeparator.After),
                    new ContextMenuOption("Folder", CollapseAllContext, () => { ExpandAllFolders(folder.Type, false); }),
                    new ContextMenuOption("FolderOpen", ExpandAllContext, () => { ExpandAllFolders(folder.Type, true); }),
                    new ContextMenuOption("MenuProperties", PropertiesFolderContext, () => { EditFolderProperties(folder, true); }, ContextMenuSeparator.Before)
                });
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
                    var button = new Button(null, TabNames[i]);
                    button.Border = true;
                    if ((int)selectedTab == i)
                    {
                        button.BoldFont = true;
                        button.Transparent = true;
                    }
                    else
                    {
                        button.BackgroundColor        = new Color(0, 0, 0, 128);
                        button.BackgroundColorHover   = new Color(0, 0, 0, 64);
                        button.BackgroundColorPressed = new Color(0, 0, 0, 32);
                    }
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
            mainContainer.ContainerMouseDownNotify += MainContainer_ContainerMouseDownNotify;
            mainContainer.ContainerMouseUpNotify += MainContainer_ContainerMouseUpNotify;
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

        private void MainContainer_ContainerMouseDownNotify(Control sender, MouseEventArgs e)
        {
            if (Platform.IsMobile && !e.Handled && e.Left)
            {
                StartCaptureOperation(sender, e.Position, MobilePan);
            }
        }

        private void MainContainer_ContainerMouseUpNotify(Control sender, MouseEventArgs e)
        {
            if (Platform.IsMobile && !e.Handled && e.Left)
            {
                EndCaptureOperation(e.Position);
            }
        }

        private void MainContainer_ContainerTouchFlingNotify(Control sender, MouseEventArgs e)
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
            panel.MouseUp += (s, e) => ProjectHeader_MouseUp(e);
            panel.ContainerMouseUpNotify += (s, e) => ProjectHeader_MouseUp(e);

            var propsButton = CreateImageButton(panel, panel.Width - iconSizeX - marginX, "Properties", false);
            propsButton.ToolTip = $"<MouseLeft> {PropertiesProjectTooltip}";
            propsButton.Click += (s) => EditProjectProperties();

            var mixerButton = CreateImageButton(panel, propsButton.Left -spacingX - iconSizeX, "Mixer", false);
            mixerButton.ToolTip = $"<MouseLeft> {AllowProjectMixerSettings}"; 
            mixerButton.Dimmed = !project.AllowMixerOverride;
            mixerButton.Click += (s) => mixerButton.Dimmed = !ToggleAllowProjectMixer();

            CreateCenteredLabel(panel, projectText, 2 * mixerButton.Left - panel.Width, true);
        }

        private void ProjectHeader_MouseUp(MouseEventArgs e)
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
            panel.MouseUp += (s, e) => Song_MouseUp(e, song);
            panel.ContainerMouseUpNotify += (s, e) => Song_MouseUp(e, song);
            panel.MouseDown += (s, e) => Song_MouseDown(s, e, song);
            panel.ContainerMouseDownNotify += (s, e) => Song_MouseDown(s, e, song);

            var icon = CreateImageBox(panel, marginX + expandSizeX, "Music", true);
            var props = CreateImageButton(panel, panel.Width - marginX - iconSizeX, "Properties");
            props.ToolTip = $"<MouseLeft> {PropertiesSongTooltip}";
            props.Click += (s) => EditSongProperties(song);

            var label = CreateLabel(panel, song.Name, true, icon.Right + marginX, 0, props.Left - icon.Right - marginX * 2, true);
            label.Bold = song == App.SelectedSong;
        }

        private void Song_MouseDown(Control sender, MouseEventArgs e, Song song)
        {
            if (!e.Handled && e.Left)
            {
                App.SelectedSong = song;
                draggedSong = song;
                StartCaptureOperation(sender, e.Position, DragSong);
                e.MarkHandled();
            }
        }

        private void Song_MouseUp(MouseEventArgs e, Song song)
        {
            if (!e.Handled && e.Right)
            {
                var menu = new List<ContextMenuOption>();
                if (App.Project.Songs.Count > 1)
                    menu.Add(new ContextMenuOption("MenuDelete", DeleteSongContext, () => { AskDeleteSong(song); }, ContextMenuSeparator.After));
                menu.Add(new ContextMenuOption("MenuDuplicate", DuplicateContext, () => { DuplicateSong(song); }));
                menu.Add(new ContextMenuOption("MenuProperties", PropertiesSongContext, () => { EditSongProperties(song, true); }, ContextMenuSeparator.Before));
                App.ShowContextMenu(menu.ToArray());
                e.MarkHandled();
            }
        }

        private void CreateInstrumentControls(Instrument instrument)
        {
            var panel = CreateGradientPanel(instrument.Color, instrument);
            panel.ToolTip = $"<MouseLeft> {SelectInstrumentTooltip} - <MouseLeft><Drag> {CopyReplaceInstrumentTooltip}\n<MouseRight> {MoreOptionsTooltip}";
            panel.MouseUp += (s, e) => Instrument_MouseUp(e, instrument);
            panel.ContainerMouseUpNotify += (s, e) => Instrument_MouseUp(e, instrument);
            panel.MouseDown += (s, e) => Instrument_MouseDown(s, e, instrument);
            panel.ContainerMouseDownNotify += (s, e) => Instrument_MouseDown(s, e, instrument);
            panel.ContainerTouchClickNotify += (s, e) => Instrument_TouchClick(s, e, instrument);

            var expand = CreateExpandButton(panel, true, expandedInstrument == instrument);
            expand.ToolTip = $"<MouseLeft> {ExpandTooltip} - <MouseRight> {MoreOptionsTooltip}";
            expand.Click += (s) => ToggleExpandInstrument(instrument);

            var icon = CreateImageBox(panel, expand.Right + spacingX, ExpansionType.Icons[instrument.Expansion], true);
            icon.MouseDown += (s, e) => Instrument_MouseDown(s, e, instrument); // MATTT : Test on desktop, shouldnt have any impact.
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
                dpcm.MouseDown += (s, e) => InstrumentDpcm_MouseDown(s, instrument, e, dpcm.Image);
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
                    env.UserData = instrument.Envelopes[idx];
                    env.ToolTip = $"<MouseLeft> {EditEnvelopeTooltip.Format(EnvelopeType.LocalizedNames[idx].Value.ToLower())} - <MouseLeft><Drag> {CopyEnvelopeTooltip} - <MouseRight> {MoreOptionsTooltip}";
                    env.Click += (s) => InstrumentEnvelope_Click(instrument, idx);
                    env.MouseDown += (s, e) => Instrument_MouseDown(s, e, instrument, idx, env.Image);
                    env.MouseUp += (s, e) => Instrument_MouseUp(e, instrument, idx);
                    env.MarkHandledOnClick = Platform.IsMobile;
                    lastEnv = env;
                }
            }

            var label = CreateLabel(panel, instrument.Name, true, icon.Right + marginX, 0, lastEnv.Left - icon.Right - marginX * 2, true);
            label.Bold = App.SelectedInstrument == instrument;
        }

        private void Icon_MouseDown(Control sender, MouseEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void InstrumentDpcm_MouseDown(Control sender, Instrument instrument, MouseEventArgs e, TextureAtlasRef image)
        {
            draggedInstrument = instrument;
            envelopeDragTexture = image;
            StartCaptureOperation(sender, e.Position, DragInstrumentSampleMappings);
            e.MarkHandled();
        }

        private void InstrumentEnvelope_Click(Instrument instrument, int envelopeType)
        {
            App.SelectedInstrument = instrument;
            App.StartEditInstrument(instrument, envelopeType);
        }

        private void Instrument_MouseDown(Control sender, MouseEventArgs e, Instrument inst, int envelopeType = -1, TextureAtlasRef image = null)
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

        private void Instrument_MouseUp(MouseEventArgs e, Instrument inst, int envelopeType = -1)
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

                App.ShowContextMenu(menu.ToArray());
                e.MarkHandled();
            }
        }

        private void Instrument_TouchClick(Control sender, MouseEventArgs e, Instrument instrument)
        {
            if (!e.Handled)
            {
                App.SelectedInstrument = instrument;
                UpdateHighlightedItem(typeof(Instrument), instrument);
            }
        }

        private void CreateDpcmSampleControls(DPCMSample sample)
        {
            var panel = CreateGradientPanel(sample.Color, sample);
            panel.ToolTip = $"<MouseRight> {MoreOptionsTooltip}";
            panel.MouseUp += (s, e) => DpcmSample_MouseUp(e, sample);
            panel.ContainerMouseUpNotify += (s, e) => DpcmSample_MouseUp(e, sample);
            panel.MouseDown += (s, e) => DpcmSample_MouseDown(s, e, sample);
            panel.ContainerMouseDownNotify += (s, e) => DpcmSample_MouseDown(s, e, sample);

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
            play.RightClick += (s) => App.PreviewDPCMSample(sample, true);

            CreateLabel(panel, sample.Name, true, icon.Right + marginX, 0, play.Left - icon.Right - marginX * 2, true);
        }

        private void DpcmSample_MouseDown(Control sender, MouseEventArgs e, DPCMSample sample)
        {
            if (!e.Handled && e.Left)
            {
                draggedSample = sample;
                StartCaptureOperation(sender, e.Position, DragSample);
                e.MarkHandled();
            }
        }

        private void DpcmSample_MouseUp(MouseEventArgs e, DPCMSample sample)
        {
            if (!e.Handled && e.Right)
            {
                var menu = new List<ContextMenuOption>();

                menu.Add(new ContextMenuOption("MenuDelete", DeleteSampleContext, () => { AskDeleteDPCMSample(sample); }, ContextMenuSeparator.After));

                if (Platform.IsDesktop)
                {
                    menu.Add(new ContextMenuOption("MenuSave", ExportProcessedDmcDataContext, () => { ExportDPCMSampleProcessedData(sample); }));
                    menu.Add(new ContextMenuOption("MenuSave", ExportSourceDataContext, () => { ExportDPCMSampleSourceData(sample); }));
                }

                if (sample.SourceDataIsWav)
                {
                    menu.Add(new ContextMenuOption("MenuTrash", DiscardSourceWavDataContext, DiscardSourceWavDataTooltip, () => { DeleteDpcmSourceWavData(sample); }));
                }

                menu.Add(new ContextMenuOption("MenuBankAssign", AutoAssignBanksContext, () => { AutoAssignSampleBanks(); }, ContextMenuSeparator.Before));
                menu.Add(new ContextMenuOption("MenuProperties", PropertiesSamplesContext, () => { EditDPCMSampleProperties(sample, true); }, ContextMenuSeparator.Before));

                App.ShowContextMenu(menu.ToArray());
                e.MarkHandled();
            }
        }

        private void CreateNoneArpeggioControls()
        {
            noneArpPanel = CreateGradientPanel(Theme.LightGreyColor1);
            noneArpPanel.ToolTip = $"<MouseLeft> {SelectArpeggioTooltip}";
            noneArpPanel.MouseUp += (s, e) => Arpeggio_MouseUp(e, null);
            noneArpPanel.ContainerMouseUpNotify += (s, e) => Arpeggio_MouseUp(e, null);

            var icon = CreateImageBox(noneArpPanel, marginX + expandSizeX, EnvelopeType.Icons[EnvelopeType.Arpeggio], true);
            var label = CreateLabel(noneArpPanel, ArpeggioNoneLabel, true, icon.Right + marginX, 0, noneArpPanel.Width - icon.Right - marginX);
            label.Bold = App.SelectedArpeggio == null;
        }

        private void CreateArpeggioControls(Arpeggio arp)
        {
            var panel = CreateGradientPanel(arp.Color, arp);
            panel.ToolTip = $"<MouseLeft> {SelectArpeggioTooltip} - <MouseLeft><Drag> {ReplaceArpeggioTooltip}\n<MouseRight> {MoreOptionsTooltip}";
            panel.MouseUp += (s, e) => Arpeggio_MouseUp(e, arp);
            panel.ContainerMouseUpNotify += (s, e) => Arpeggio_MouseUp(e, arp);
            panel.MouseDown += (s, e) => Arpeggio_MouseDown(s, e, arp);
            panel.ContainerMouseDownNotify += (s, e) => Arpeggio_MouseDown(s, e, arp);

            var icon = CreateImageBox(panel, marginX + expandSizeX, EnvelopeType.Icons[EnvelopeType.Arpeggio], true);
            var props = CreateImageButton(panel, panel.Width - iconSizeX - marginX, "Properties");
            props.ToolTip = $"<MouseLeft> {PropertiesArpeggioTooltip}";
            props.Click += (s) => EditArpeggioProperties(arp);

            var edit = CreateImageButton(panel, props.Left - spacingX - iconSizeX, "EnvelopeArpeggio");
            edit.ToolTip = $"<MouseLeft> {EditArpeggioTooltip}";
            edit.UserData = "Arpeggio";
            edit.Click += (s) => App.StartEditArpeggio(arp);
            edit.MouseDown += (s, e) => Arpeggio_MouseDown(s, e, arp, true, edit.Image);
            edit.MouseUp += (s, e) => Arpeggio_MouseUp(e, arp);
            edit.MarkHandledOnClick = false;

            var label = CreateLabel(panel, arp.Name, true, icon.Right + marginX, 0, edit.Left - icon.Right - marginX * 2);
            label.Bold = App.SelectedArpeggio == arp;
        }

        private void Arpeggio_MouseDown(Control sender, MouseEventArgs e, Arpeggio arp, bool values = false, TextureAtlasRef image = null)
        {
            if (!e.Handled && e.Left)
            {
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

        private void Arpeggio_MouseUp(MouseEventArgs e, Arpeggio arp)
        {
            if (!e.Handled && e.Right && arp != null)
            {
                App.ShowContextMenu(new[]
                {
                    new ContextMenuOption("MenuDelete", DeleteArpeggioContext, () => { AskDeleteArpeggio(arp); }, ContextMenuSeparator.After),
                    new ContextMenuOption("MenuDuplicate", DuplicateContext, () => { DuplicateArpeggio(arp); }),
                    new ContextMenuOption("MenuReplace", ReplaceWithContext, () => { AskReplaceArpeggio(arp); }),
                    new ContextMenuOption("MenuProperties", PropertiesArpeggioContext, () => { EditArpeggioProperties(arp, true); }, ContextMenuSeparator.Before)
                });
                e.MarkHandled();
            }
        }

        private void CreateParamTabs(GradientPanel panel, int x, int y, int width, int height, string[] tabNames, string selelectedTabName)
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

        private ParamSlider CreateParamSlider(GradientPanel panel, ParamInfo p, int y, int width)
        {
            var slider = new ParamSlider(p);
            panel.AddControl(slider);
            slider.Move(panel.Width - paramSizeX - marginX, y + Utils.DivideAndRoundUp(panelSizeY - slider.Height, 2), paramSizeX, slider.Height);
            return slider;
        }

        private ParamList CreateParamList(GradientPanel panel, ParamInfo p, int y, int height)
        {
            var list = new ParamList(p);
            panel.AddControl(list);
            list.Move(panel.Width - paramSizeX - marginX, y + Utils.DivideAndRoundUp(panelSizeY - list.Height, 2), paramSizeX, list.Height);
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
                            label.MouseUp += ParamLabel_MouseUp;

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

        private void ParamLabel_MouseUp(Control sender, MouseEventArgs e)
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
            var lastSong = (Song)null;
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
                    CreateInsertionPoint(FolderType.Song, lastSong, f);
                }

                if (folderExpanded)
                {
                    foreach (var song in songs)
                    {
                        CreateSongControls(song);
                        CreateInsertionPoint(FolderType.Song, song, f);
                        lastSong = song;
                    }
                }

                CreateFolderInsertionPoint(FolderType.Song, f);
            }
        }

        private void CreateAllInstrumentsControls()
        {
            var project = App.Project;
            var folders = project.GetFoldersForType(FolderType.Instrument);
            var lastIntrument = (Instrument)null;
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
                    CreateInsertionPoint(FolderType.Instrument, lastIntrument, f);
                }

                if (folderExpanded)
                {
                    foreach (var instrument in instruments)
                    {
                        CreateInstrumentControls(instrument);
                        if (instrument == expandedInstrument)
                            CreateParamsControls(instrument.Color, instrument, InstrumentParamProvider.GetParams(instrument), TransactionScope.Instrument, instrument.Id, selectedInstrumentTab);
                        CreateInsertionPoint(FolderType.Instrument, instrument, f);
                        lastIntrument = instrument;
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
            var lastSample = (DPCMSample)null;
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
                    CreateInsertionPoint(FolderType.Sample, lastSample, f);
                }

                if (folderExpanded)
                {
                    foreach (var sample in samples)
                    {
                        CreateDpcmSampleControls(sample);
                        if (sample == expandedSample)
                            CreateParamsControls(sample.Color, sample, DPCMSampleParamProvider.GetParams(sample), TransactionScope.DPCMSample, sample.Id);
                        CreateInsertionPoint(FolderType.Sample, sample, f);
                        lastSample = sample;
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
            var lastArpeggio = (Arpeggio)null;
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
                    CreateInsertionPoint(FolderType.Arpeggio, lastArpeggio, f);
                }

                if (folderExpanded)
                {
                    foreach (var arp in arpeggios)
                    {
                        CreateArpeggioControls(arp);
                        CreateInsertionPoint(FolderType.Arpeggio, arp, f);
                        lastArpeggio = arp;
                    }
                }

                CreateFolderInsertionPoint(FolderType.Arpeggio, f);
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
                            var chanIcon = CreateImageBox(chanHeader, marginX, RegisterViewer.Icons[expRegs.InterpreterIcons[i]]);
                            CreateLabel(chanHeader, expRegs.InterpreterLabels[i], false, chanIcon.Right + marginX, 0, width);
                            lastControl = CreateRegisterViewerPanel(chanRegs, e);
                        }
                    }
                }
            }
        }

        public void RecreateAllControls()
        {
            UpdateRenderCoords();

            if (ParentWindow == null || App.Project == null)
                return;

            ValidateIntegrity();
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
            Capture = false;
            if (scrollBar != null)
                scrollBar.VirtualSize = virtualSizeY;
            ClampScroll();
            SyncScrollBarToContainer();
            ValidateIntegrity();
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

        private void UpdateHighlightedItem(Type type, object obj)
        {
            if (Platform.IsMobile)
            {
                highlightedObject = highlightedObject == obj ? null : obj;

                foreach (var ctrl in mainContainer.Controls)
                {
                    if (ctrl is GradientPanel panel)
                    {
                        if (panel.UserData != null && panel.UserData.GetType() == type)
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

        private void ConditionalResetHighlightedObject()
        {

            //if (highlightedObject != null || App.Project == null)
            //{
            //    if (highlightedObject is Song song && !App.Project.SongExists(song))
            //        UpdateHighlightedItem(typeof(Song), null);
            //    else if (highlightedObject is Instrument inst && !App.Project.InstrumentExists(inst))
            //        UpdateHighlightedItem(typeof(Instrument), null);
            //    else if (highlightedObject is DPCMSample sample && !App.Project.SampleExists(sample))
            //        UpdateHighlightedItem(typeof(DPCMSample), null);
            //    else if (highlightedObject is Arpeggio arp && !App.Project.ArpeggioExists(arp))
            //        UpdateHighlightedItem(typeof(Arpeggio), null);
            //}
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

        public void InstrumentEnvelopeChanged(Instrument instrument, int envType)
        {
            var env = instrument.Envelopes[envType];
            var button = FindInstrumentEnvelopeButton(instrument, env);
            button.Dimmed = env.IsEmpty(envType);
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
            if (selectedTab == TabType.Registers)
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
                    mainContainer.WindowToControl(ControlToWindow(new Point(mouseLastX, mouseLastY)));

                if ((captureOperation == DragInstrumentEnvelope || 
                     captureOperation == DragArpeggioValues) && envelopeDragIdx >= 0 ||
                    (captureOperation == DragInstrumentSampleMappings))
                {
                    if (mainContainer.ClientRectangle.Contains(mainContainerPos.X, mainContainerPos.Y))
                    {
                        Debug.Assert(envelopeDragTexture != null);
                        
                        var bx = mainContainerPos.X - captureButtonRelX;
                        var by = mainContainerPos.Y - captureButtonRelY;

                        c.DrawTextureAtlas(envelopeDragTexture, bx, by, iconImageScale, Color.Black.Transparent(0.5f));

                        if (Platform.IsMobile)
                        {
                            var iconSizeX = envelopeDragTexture.ElementSize.Width  * iconImageScale;
                            var iconSizeY = envelopeDragTexture.ElementSize.Height * iconImageScale;
                            c.DrawRectangle(bx, by, bx + iconSizeX, by + iconSizeY, Theme.WhiteColor, 3, true, true);
                        }
                    }
                }
                else
                {
                    var lineColor = Theme.LightGreyColor2;

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
                    else if (captureOperation == DragArpeggio && draggedArpeggio != null)
                    {
                        lineColor = draggedArpeggio.Color;
                    }

                    GetDragInsertLocation(mainContainerPos, out var draggedInFolder, out var insertY);
                    insertY -= mainContainer.ScrollY;

                    var margin = draggedInFolder != null ? expandSizeX : 0;
                    c.DrawLine(margin, insertY, mainContainer.Width - margin, insertY, lineColor, dragLineSizeY);
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
                    if (songBefore != draggedSong)
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
                    if (instrumentBefore != draggedInstrument)
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
                    InstrumentDroppedOutside(draggedInstrument, ControlToScreen(p));
                }
            }
            else if (captureOperation == DragArpeggio)
            {
                if (inside && draggedArpeggio != null)
                {
                    var arpBefore = GetDragInsertLocation(p, out var draggedInFolder, out _) as Arpeggio;
                    if (arpBefore != draggedArpeggio)
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
                    ArpeggioDroppedOutside(draggedArpeggio, ControlToScreen(p));
                }
            }
            else if (captureOperation == DragSample)
            {
                if (inside)
                {
                    var sampleBefore = GetDragInsertLocation(p, out var draggedInFolder, out _) as DPCMSample;
                    if (sampleBefore != draggedSample)
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
                    var mappingNote = App.GetDPCMSampleMappingNoteAtPos(ControlToScreen(p), out var instrument);
                    if (instrument != null)
                    {
                        App.UndoRedoManager.BeginTransaction(TransactionScope.Instrument, instrument.Id, -1, TransactionFlags.StopAudio);
                        instrument.UnmapDPCMSample(mappingNote);
                        instrument.MapDPCMSample(mappingNote, draggedSample);
                        App.UndoRedoManager.EndTransaction();
                        DPCMSampleMapped?.Invoke(draggedSample, ControlToScreen(p));
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
            var panel = mainContainer.FindControlOfTypeAt<GradientPanel>(windowPoint.X, windowPoint.Y);

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
                                var button = FindInstrumentEnvelopeButton(instrumentDst, oldEnvelopeDst);
                                button.Dimmed = newEnvelopeDst.IsEmpty(envelopeDragIdx);
                                button.UserData = newEnvelopeDst;

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
            var panel = mainContainer.FindControlOfTypeAt<GradientPanel>(windowPoint.X, windowPoint.Y);

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
            var panel = mainContainer.FindControlOfTypeAt<GradientPanel>(windowPoint.X, windowPoint.Y);

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
                if (Math.Abs(p.X - captureMouseX) >= CaptureThreshold ||
                    Math.Abs(p.Y - captureMouseY) >= CaptureThreshold)
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
                    DoScroll(p.Y - mouseLastY);
                }
                else
                {
                    MarkDirty();
                }
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            bool middle = e.Middle || (e.Left && ModifierKeys.IsAltDown && Settings.AltLeftForMiddle);

            Debug.WriteLine($"MOVE {e.X} {e.Y}");

            UpdateCursor();
            //UpdateCaptureOperation(e.X, e.Y); // MATTT : Was this active?

            // MATTT : This should be event on the "mainContainer".
            if (middle)
                DoScroll(e.Y - mouseLastY);

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

            //if (!middle)
            //{
            //    doMouseUp = captureOperation == CaptureOperation.None;
            //    EndCaptureOperation(e.X, e.Y);
            //}

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

        private void StartCaptureOperation(Control control, Point p, CaptureOperation op)
        {
            Debug.Assert(captureOperation == null);
            var ctrlPos = WindowToControl(control.ControlToWindow(p));
            mouseLastX = ctrlPos.X;
            mouseLastY = ctrlPos.Y;
            captureMouseX = ctrlPos.X;
            captureMouseY = ctrlPos.Y;
            captureButtonRelX = p.X;
            captureButtonRelY = p.Y;
            captureScrollY = mainContainer.ScrollY;
            control.Capture = true;
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
            Capture = false;
            MarkDirty();
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
            envelopeDragTexture = null;
            captureOperation = null;
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

        public override void OnContainerMouseWheelNotify(Control control, MouseEventArgs e)
        {
            OnMouseWheel(e);
        }

        public override void OnContainerMouseMoveNotify(Control control, MouseEventArgs e)
        {
            var winPos = control.ControlToWindow(new Point(e.X, e.Y));
            var ctrlPos = WindowToControl(winPos);
            var ctrl = GetControlAt(winPos.X, winPos.Y, out _, out _);

            Debug.WriteLine($"MOVE NOTIFY {e.X} {e.Y}");

            if (ctrl != null)
            {
                var tooltip = ctrl.ToolTip;
                if (string.IsNullOrEmpty(tooltip))
                    tooltip = ctrl.ParentContainer.ToolTip;
                App.SetToolTip(tooltip);
                App.SequencerShowExpansionIcons = (ctrl.UserData is Instrument) || (ctrl.ParentContainer.UserData is Instrument);
            }

            UpdateCursor();
            UpdateCaptureOperation(ctrlPos);

            mouseLastX = ctrlPos.X;
            mouseLastY = ctrlPos.Y;
        }

        public override void OnContainerMouseDownNotify(Control control, MouseEventArgs e)
        {
            //ConditionalRecreateAllControls();
        }

        public override void OnContainerMouseUpNotify(Control control, MouseEventArgs e)
        {
            if (!e.Middle)
            {
                var ctrlPos = WindowToControl(control.ControlToWindow(e.Position));
                EndCaptureOperation(ctrlPos);
            }
            //ConditionalRecreateAllControls();
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
            EnsureFolderExpanded(FolderType.Song, folder);
            RecreateAllControls();
            BlinkButton(App.SelectedSong);
            App.UndoRedoManager.EndTransaction();
        }

        private void AskAddSong()
        {
            App.ShowContextMenu(new[]
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

            App.ShowContextMenu(options.ToArray());
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

        private GradientPanel FindInstrumentPanel(Instrument inst)
        {
            return mainContainer.FindControlByUserData(inst) as GradientPanel;
        }

        private Button FindInstrumentEnvelopeButton(Instrument inst, Envelope env)
        {
            return FindInstrumentPanel(inst).FindControlByUserData(env) as Button;
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
            FindInstrumentEnvelopeButton(inst, env).Dimmed = env.IsEmpty(envelopeType);
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
            App.ShowContextMenu(new[]
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
            App.ShowContextMenu(new[]
            {
                new ContextMenuOption("Folder", AddFolderContext, () => { AddFolder(FolderType.Sample); }, ContextMenuSeparator.Before)
            });
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

        protected override void OnTouchMove(MouseEventArgs e)
        {
            UpdateCursor();
            UpdateCaptureOperation(e.Position);

            mouseLastX = e.X;
            mouseLastY = e.Y;
        }

        protected override void OnTouchUp(MouseEventArgs e)
        {
            EndCaptureOperation(e.Position);
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
            ValidateIntegrity();
            TickFling(delta);
            UpdateCaptureOperation(new Point(mouseLastX, mouseLastY), true, delta);
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

                    if (changedAuthoringMachine || changedNumChannels || changedExpMixer || changedTuning)
                        transFlags = TransactionFlags.RecreatePlayers;
                    else if (changedExpansion)
                        transFlags = TransactionFlags.RecreatePlayers | TransactionFlags.RecreateStreams; // Toggling EPSM will change mono/stereo and requires new audiostreams.
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
            var dlg = new PropertyDialog(ParentWindow, InstrumentPropertiesTitle, pt, 240, true, pt.Y > ParentWindowSize.Height / 2);
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
            var dlg = new PropertyDialog(ParentWindow, FolderPropertiesTitle, pt, 240, true, pt.Y > ParentWindowSize.Height / 2);
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
            var dlg = new PropertyDialog(ParentWindow, ArpeggioPropertiesTitle, pt, 240, true, pt.Y > ParentWindowSize.Height / 2);
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
            var dlg = new PropertyDialog(ParentWindow, SamplePropertiesTitle, pt, 240, true, pt.Y > ParentWindowSize.Height / 2);
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
                Capture = false;
                flingVelY = 0.0f;
                mainContainer.ScrollY = scrollY;
                highlightedObject = null;

                ClampScroll();
                RecreateAllControls();
                BlinkButton(null);
            }
        }
    }
}
