using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

#if !FAMISTUDIO_ANDROID
    using System.Text.Json;
#endif

namespace FamiStudio
{
    public class FamiStudio
    {
        private const int MaxAutosaves = 99;

        private FamiStudioWindow window;
        private Project project;
        private Song song;
        private Instrument selectedInstrument; // null = DPCM
        private Arpeggio selectedArpeggio;
        private SongPlayer songPlayer;
        private InstrumentPlayer instrumentPlayer;
        private IAudioStream songStream;
        private IAudioStream instrumentStream;
        private OscilloscopeGenerator oscilloscope;
        private UndoRedoManager undoRedoManager;
        private ExportDialog exportDialog;
        private LogDialog logDialog;
        private LogProgressDialog progressLogDialog;

        private int selectedChannelIndex;
        private long forceDisplayChannelMask = 0;
        private int lastMidiNote = -1;
        private int tutorialCounter = 3;
        private int baseRecordingOctave = 3;
        private int lastTickCurrentFrame = -1;
        private int previewDPCMSampleId = -1;
        private int previewDPCMSampleRate = 44100;
        private int lastRecordingKeyDown = -1;
        private int lastPlayPosition = 0;
        private bool previewDPCMIsSource = false;
        private bool metronome = false;
        private bool palPlayback = false;
        private bool audioDeviceChanged = false;
        private bool recordingMode = false;
        private bool qwertyPiano = false;
        private bool followMode = false;
        private bool suspended = false;
        private float stopInstrumentTimer = 0.0f;
        private short[] metronomeSound;
        private ConcurrentQueue<Tuple<int, bool>> midiNoteQueue = new ConcurrentQueue<Tuple<int, bool>>();

        private int autoSaveIndex = 0;
        private float averageTickRateMs = 8.0f;
        private DateTime lastAutoSave;

        private volatile bool   newReleaseCheckDone = false;
        private volatile bool   newReleaseAvailable = false;
        private volatile string newReleaseString = null;
        private volatile string newReleaseUrl = null;

        public bool  IsPlaying => songPlayer != null && songPlayer.IsPlaying;
        public bool  IsSeeking => songPlayer != null && songPlayer.IsSeeking;
        public bool  IsRecording => recordingMode;
        public bool  IsQwertyPianoEnabled => qwertyPiano;
        public bool  IsMetronomeEnabled => metronome;
        public bool  IsSuspended => suspended;
        public bool  FollowModeEnabled { get => followMode; set => followMode = value; }
        public bool  SequencerHasSelection => Sequencer.GetPatternTimeSelectionRange(out _, out _);
        public bool  PianoRollHasSelection => PianoRoll.IsSelectionValid();
        public int   BaseRecordingOctave => baseRecordingOctave;
        public bool  MobilePianoVisible { get => window.MobilePianoVisible; set => window.MobilePianoVisible = value; }
        public int   CurrentFrame => lastTickCurrentFrame >= 0 ? lastTickCurrentFrame : (songPlayer != null ? songPlayer.PlayPosition : 0);
        public long  ChannelMask { get => songPlayer != null ? songPlayer.ChannelMask : -1; set => songPlayer.ChannelMask = value; }
        public int   PlayRate { get => songPlayer != null ? songPlayer.PlayRate : 1; set { songPlayer.PlayRate = value; } }
        public float AverageTickRate => averageTickRateMs;
        public int   EditEnvelopeType { get => PianoRoll.EditEnvelopeType; }

        public bool SnapEnabled                 { get => PianoRoll.SnapEnabled;         set => PianoRoll.SnapEnabled         = value; }
        public bool SnapEffectEnabled           { get => PianoRoll.SnapEffectEnabled;   set => PianoRoll.SnapEffectEnabled   = value; }
        public int  SnapResolution              { get => PianoRoll.SnapResolution;      set => PianoRoll.SnapResolution      = value; }
        public int  SelectedEffect              { get => PianoRoll.SelectedEffect;      set => PianoRoll.SelectedEffect      = value; }
        public bool EffectPanelExpanded         { get => PianoRoll.EffectPanelExpanded; set => PianoRoll.EffectPanelExpanded = value; }
        public bool SequencerShowExpansionIcons { get => Sequencer.ShowExpansionIcons;  set => Sequencer.ShowExpansionIcons  = value; }

        public UndoRedoManager  UndoRedoManager => undoRedoManager; 
        public DPCMSample       DraggedSample   => ProjectExplorer.DraggedSample;
        public DPCMSample       EditSample      => PianoRoll.EditSample;
        public Project          Project         => project;
        public Channel          SelectedChannel => song.Channels[SelectedChannelIndex];
        public Toolbar          ToolBar         => window.ToolBar;
        public Sequencer        Sequencer       => window.Sequencer;
        public PianoRoll        PianoRoll       => window.PianoRoll;
        public ProjectExplorer  ProjectExplorer => window.ProjectExplorer;
        public QuickAccessBar   QuickAccessBar  => window.QuickAccessBar;
        public MobilePiano      MobilePiano     => window.MobilePiano;
        public Control          ActiveControl   => window.ActiveControl;
        public FamiStudioWindow Window          => window;
        public BasePlayer       ActivePlayer    => IsPlaying ? songPlayer : instrumentPlayer;

        public bool IsSequencerActive          => ActiveControl == Sequencer;
        public bool IsPianoRollActive          => ActiveControl == PianoRoll;
        public bool IsProjectExplorerActive    => ActiveControl == ProjectExplorer;
        public bool IsEditingChannel           => PianoRoll.IsEditingChannel;
        public bool IsEditingInstrument        => PianoRoll.IsEditingInstrument;
        public bool IsEditingArpeggio          => PianoRoll.IsEditingArpeggio;
        public bool IsEditingDPCMSample        => PianoRoll.IsEditingDPCMSample;
        public bool IsEditingDPCMSampleMapping => PianoRoll.IsEditingDPCMSampleMapping;

        private string WipProject  => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "WIP.fms");
        private string WipSettings => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "WIP.ini");

        public int  PreviewDPCMWavPosition => instrumentPlayer != null ? instrumentPlayer.RawPcmSamplePlayPosition : 0;
        public int  PreviewDPCMSampleId    => previewDPCMSampleId;
        public int  PreviewDPCMSampleRate  => previewDPCMSampleRate;
        public bool PreviewDPCMIsSource    => previewDPCMIsSource;

        public static Project    StaticProject  { get; set; }
        public static FamiStudio StaticInstance { get; private set; }

        #region Localization

        LocalizedString SaveChangesDialog;
        LocalizedString ConfirmTitle;
        LocalizedString DesktopOpenProjectTitle;
        LocalizedString DesktopSaveProjectTitle;
        LocalizedString MobileOpenProjectTitle;
        LocalizedString MobileSaveProjectTitle;
        LocalizedString MobileFamiTrackerImportWarning;
        LocalizedString WarningTitle;
        LocalizedString ProjectSaveSuccess;
        LocalizedString ProjectSaveError;
        LocalizedString NoLastExportWarning;
        LocalizedString ProjectChangedExportWarning;
        LocalizedString NewVersionToast;
        LocalizedString NewProjectTitle;
        LocalizedString NewVersionWelcome;
        LocalizedString IncompatibleInstrumentError;
        LocalizedString IncompatibleExpRequiredError;
        LocalizedString AudioDeviceChanged;
        LocalizedString AudioStreamError;

        #endregion

        public void Initialize(FamiStudioWindow form, string filename)
        {
            Localization.Localize(this);

            StaticInstance = this;

            SetWindow(form);
            InitializeKeys();
            InitializeMetronome();
            InitializeMidi();
            InitializeDeviceChangeEvent();
            ApplySettings();

            if (string.IsNullOrEmpty(filename) && Platform.IsDesktop && Settings.OpenLastProjectOnStart && !string.IsNullOrEmpty(Settings.LastProjectFile) && File.Exists(Settings.LastProjectFile))
                filename = Settings.LastProjectFile;

            if (string.IsNullOrEmpty(filename) && Platform.IsMobile && File.Exists(WipProject))
                filename = WipProject;

            if (!string.IsNullOrEmpty(filename))
                OpenProjectInternal(filename);
            else
                NewProject(true);

#if !FAMISTUDIO_ANDROID && !DEBUG
            if (Settings.CheckUpdates)
                Task.Factory.StartNew(CheckForNewRelease);
            else if (Platform.IsDesktop)
                newReleaseCheckDone = true;
#endif
        }

        public void SetWindow(FamiStudioWindow win, bool resuming = false)
        {
            window = win;

            SetActiveControl(Platform.IsDesktop ? (Control)PianoRoll : Sequencer, false);

            Sequencer.PatternClicked     += Sequencer_PatternClicked;
            Sequencer.PatternModified    += Sequencer_PatternModified;
            Sequencer.SelectionChanged   += Sequencer_SelectionChanged;
            Sequencer.PatternsPasted     += Sequencer_PatternsPasted;
            Sequencer.ShyChanged         += Sequencer_ShyChanged;

            PianoRoll.PatternChanged     += PianoRoll_PatternChanged;
            PianoRoll.ManyPatternChanged += PianoRoll_ManyPatternChanged;
            PianoRoll.DPCMSampleChanged  += PianoRoll_DPCMSampleChanged;
            PianoRoll.EnvelopeChanged    += PianoRoll_EnvelopeChanged;
            PianoRoll.NotesPasted        += PianoRoll_NotesPasted;
            PianoRoll.ScrollChanged      += PianoRoll_ScrollChanged;
            PianoRoll.NoteEyedropped     += PianoRoll_NoteEyedropped;
            PianoRoll.DPCMSampleMapped   += PianoRoll_DPCMSampleMapped;
            PianoRoll.DPCMSampleUnmapped += PianoRoll_DPCMSampleMapped;
            PianoRoll.MaximizedChanged   += PianoRoll_MaximizedChanged;

            ProjectExplorer.InstrumentColorChanged   += ProjectExplorer_InstrumentColorChanged;
            ProjectExplorer.InstrumentReplaced       += ProjectExplorer_InstrumentReplaced;
            ProjectExplorer.InstrumentDeleted        += ProjectExplorer_InstrumentDeleted;
            ProjectExplorer.InstrumentDroppedOutside += ProjectExplorer_InstrumentDroppedOutside;
            ProjectExplorer.SongModified             += ProjectExplorer_SongModified;
            ProjectExplorer.ProjectModified          += ProjectExplorer_ProjectModified;
            ProjectExplorer.ArpeggioColorChanged     += ProjectExplorer_ArpeggioColorChanged;
            ProjectExplorer.ArpeggioDeleted          += ProjectExplorer_ArpeggioDeleted;
            ProjectExplorer.ArpeggioDroppedOutside   += ProjectExplorer_ArpeggioDroppedOutside;
            ProjectExplorer.DPCMSampleReloaded       += ProjectExplorer_DPCMSampleReloaded;
            ProjectExplorer.DPCMSampleColorChanged   += ProjectExplorer_DPCMSampleColorChanged;
            ProjectExplorer.DPCMSampleDeleted        += ProjectExplorer_DPCMSampleDeleted;
            ProjectExplorer.DPCMSampleDraggedOutside += ProjectExplorer_DPCMSampleDraggedOutside;
            ProjectExplorer.DPCMSampleMapped         += ProjectExplorer_DPCMSampleMapped;

            if (resuming)
            {
                ResetEverything();

                // Extra safety.
                if (UndoRedoManager.HasTransactionInProgress)
                    UndoRedoManager.AbortTransaction();
            }
        }

        public LoopMode LoopMode 
        { 
            get => songPlayer != null ? songPlayer.Loop : LoopMode.LoopPoint; 
            set => songPlayer.Loop = value; 
        }

        public bool AccurateSeek
        {
            get => Settings.AccurateSeek;
            set
            {
                if (Settings.AccurateSeek != value)
                {
                    Settings.AccurateSeek = value;
                    RecreateAudioPlayers();
                }
            }
        }

        public Song SelectedSong
        {
            get { return song; }
            set
            {
                if (song != value)
                {
                    StopSong();
                    SeekSong(0);

                    song = value;

                    ResetSelectedChannel();
                    RefreshLayout();
                    ProjectExplorer.SelectedSongChanged();
                    PianoRoll.SongChanged(selectedChannelIndex);
                    Sequencer.Reset();
                    ToolBar.Reset();

                    MarkEverythingDirty();
                }
            }
        }

        public int SelectedChannelIndex
        {
            get { return selectedChannelIndex; }
            set
            {
                if (value >= 0 && value < song.Channels.Length)
                {
                    StopInstrument();
                    selectedChannelIndex = value;
                    PianoRoll.ChangeChannel(selectedChannelIndex);
                    MarkEverythingDirty();
                }
            }
        }

        public Instrument SelectedInstrument
        {
            get { return selectedInstrument; }
            set
            {
                if (value != selectedInstrument)
                {
                    selectedInstrument = value;

                    if (Platform.IsMobile)
                    {
                        if (PianoRoll.IsEditingInstrument && selectedInstrument != null)
                        {
                            var envType = PianoRoll.EditEnvelopeType;

                            // If new instrument doesnt have this envelope, fallback to volume which is common to all.
                            if (!selectedInstrument.IsEnvelopeActive(envType))
                                envType = EnvelopeType.Volume;

                            PianoRoll.StartEditInstrument(selectedInstrument, envType);
                        }
                        else if (PianoRoll.IsEditingDPCMSampleMapping && selectedInstrument != null && selectedInstrument.Expansion == ExpansionType.None)
                        {
                            PianoRoll.StartEditDPCMMapping(selectedInstrument);
                        }
                    }

                    ProjectExplorer.SelectedInstrumentChanged();
                    MarkEverythingDirty();
                }
            }
        }

        public Arpeggio SelectedArpeggio
        {
            get { return selectedArpeggio; }
            set
            {
                if (value != selectedArpeggio)
                {
                    selectedArpeggio = value;
                    if (Platform.IsMobile && PianoRoll.IsEditingArpeggio && selectedArpeggio != null)
                        PianoRoll.StartEditArpeggio(selectedArpeggio);
                    ProjectExplorer.SelectedArpeggioChanged();
                    MarkEverythingDirty();
                }
            }
        }

        public void StartEditDPCMMapping(Instrument instrument)
        {
            PianoRoll.StartEditDPCMMapping(instrument);
            ConditionalSwitchToPianoRoll();
        }

        public void StartEditInstrument(Instrument instrument, int envelope)
        {
            PianoRoll.StartEditInstrument(instrument, envelope);
            ConditionalSwitchToPianoRoll();
        }

        public void StartEditChannel(int channelIdx, int patternIdx = 0)
        {
            PianoRoll.StartEditChannel(channelIdx, patternIdx);
            ConditionalSwitchToPianoRoll();
        }

        public void StartEditDPCMSample(DPCMSample sample)
        {
            PianoRoll.StartEditDPCMSample(sample);
            ConditionalSwitchToPianoRoll();
        }

        public void StartEditArpeggio(Arpeggio arp)
        {
            PianoRoll.StartEditArpeggio(arp);
            ConditionalSwitchToPianoRoll();
        }

        private void ConditionalSwitchToPianoRoll()
        {
            if (Platform.IsMobile && ActiveControl != PianoRoll)
                SetActiveControl(PianoRoll);
        }

        public bool IsChannelActive(int idx)
        {
            return songPlayer != null && (songPlayer.ChannelMask & (1L << idx)) != 0;
        }

        public void ToggleChannelActive(int idx)
        {
            if (songPlayer != null)
                songPlayer.ChannelMask ^= (1L << idx);
        }

        public void ToggleChannelSolo(int idx, bool toggleAll = false)
        {
            if (songPlayer != null)
            {
                var bit = 1L << idx;
                if (songPlayer.ChannelMask == 0 && toggleAll)
                    songPlayer.ChannelMask = -1;
                else
                    songPlayer.ChannelMask = songPlayer.ChannelMask == bit ? -1 : bit;
            }
        }

        public void SoloChannel(int idx)
        {
            if (songPlayer != null)
                songPlayer.ChannelMask = 1L << idx;
        }
        
        public bool IsChannelSolo(int idx)
        {
            return songPlayer != null && songPlayer.ChannelMask == (1L << idx);
        }

        public bool IsChannelForceDisplay(int idx)
        {
            return (forceDisplayChannelMask & (1L << idx)) != 0;
        }

        public void ToggleChannelForceDisplay(int idx)
        {
            ForceDisplayChannelMask ^= (1L << idx);
        }

        public void ToggleChannelForceDisplayAll(int idx, bool toggleAll = false)
        {
            var bit = 1L << idx;
            if (forceDisplayChannelMask == 0 && toggleAll)
                ForceDisplayChannelMask = -1;
            else
                ForceDisplayChannelMask = forceDisplayChannelMask == bit ? -1 : bit;
        }

        public void SetActiveControl(Control ctrl, bool animate = true)
        {
            Debug.Assert(ctrl == PianoRoll || ctrl == Sequencer || ctrl == ProjectExplorer);
            window.SetActiveControl(ctrl, animate);
        }

        public void ReplacePianoRollSelectionInstrument(Instrument inst)
        {
            PianoRoll.ReplaceSelectionInstrument(inst, Point.Empty, null, true);
        }

        public void ReplacePianoRollSelectionArpeggio(Arpeggio arp)
        {
            PianoRoll.ReplaceSelectionArpeggio(arp, Point.Empty, true);
        }

        private void ProjectExplorer_InstrumentsHovered(bool showExpansions)
        {
            Sequencer.ShowExpansionIcons = showExpansions;
        }

        private void PianoRoll_MaximizedChanged()
        {
            RefreshLayout();
        }

        private void Sequencer_ShyChanged()
        {
            RefreshLayout();
        }

        private void Sequencer_PatternsPasted()
        {
            RefreshLayout();
            RefreshProjectExplorerButtons();
        }

        private void Sequencer_SelectionChanged()
        {
            ToolBar.MarkDirty();

            if (songPlayer != null)
            {
                Sequencer.GetPatternTimeSelectionRange(out var min, out var max);
                songPlayer.SetSelectionRange(min, max);
            }
        }

        public void SetToolTip(string msg)
        {
            ToolBar.SetToolTip(msg);
        }

        public void BeginLogTask(bool progress = false, string title = null, string text = null)
        {
            Debug.Assert(logDialog == null && progressLogDialog == null);

            if (progress)
            {
                progressLogDialog = new LogProgressDialog(window, title, text);
                Log.SetLogOutput(progressLogDialog);
            }
            else 
            {
                if (Platform.IsDesktop)
                {
                    logDialog = new LogDialog(window);
                    Log.SetLogOutput(logDialog);
                }
                else
                {
                    Log.ClearLastMessages();
                }
            }
        }

        public void AbortLogTask()
        {
            logDialog = null;
            Log.ClearLogOutput();
        }

        public void EndLogTask()
        {
            if (progressLogDialog != null)
            {
                if (progressLogDialog.HasMessages)
                {
                    Log.LogMessage(LogSeverity.Info, "Done!");
                    Log.ReportProgress(1.0f);
                }

                if (Platform.IsDesktop)
                    progressLogDialog.StayModalUntilClosed();
                else
                    progressLogDialog.Close();

                progressLogDialog = null;
            }
            else if (Platform.IsDesktop)
            {
                logDialog.ShowDialogIfMessages();
                logDialog = null;
            }

            Log.ClearLogOutput();
        }

        public TimeSpan CurrentTime
        {
            get
            {
                if (song != null && song.UsesFamiStudioTempo)
                {
                    var location = NoteLocation.FromAbsoluteNoteIndex(song, CurrentFrame);
                    var numFrames  = song.CountFramesBetween(song.StartLocation, location, 0, false);

                    return TimeSpan.FromMilliseconds(numFrames * 1000.0 / (song.Project.PalMode ? NesApu.FpsPAL : NesApu.FpsNTSC));
                }
                else
                {
                    return TimeSpan.Zero;
                }
            }
        }

        private void PianoRoll_DPCMSampleMapped(int note)
        {
            Sequencer.InvalidatePatternCache();
            ProjectExplorer.NotifyDPCMSampleMapped();
        }

        public int GetDPCMSampleMappingNoteAtPos(Point pos, out Instrument instrument)
        {
            return PianoRoll.GetDPCMSampleMappingNoteAtPos(PianoRoll.ScreenToControl(pos), out instrument);
        }

        private void ProjectExplorer_DPCMSampleMapped(DPCMSample instrument, Point pos)
        {
            Sequencer.InvalidatePatternCache();
            PianoRoll.MarkDirty();
        }

        private void ProjectExplorer_DPCMSampleDraggedOutside(DPCMSample instrument, Point pos)
        {
            if (PianoRoll.ClientRectangle.Contains(PianoRoll.ScreenToControl(pos)))
                PianoRoll.MarkDirty();
        }

        private void PianoRoll_NoteEyedropped(Note note)
        {
            if (note != null && note.Instrument != null)
            {
                selectedInstrument = note.Instrument;
                selectedArpeggio   = note.Arpeggio;

                ProjectExplorer.BlinkButton(selectedInstrument);
            }
        }

        private void PianoRoll_ScrollChanged()
        {
            if (!PianoRoll.IsMaximized)
            {
                Sequencer.MarkDirty();
            }
        }

        private void ResetSelectedChannel()
        {
            selectedChannelIndex = 0;
        }

        private void ResetSelectedSong()
        {
            if (!project.SongExists(song))
                song = project.Songs[0];
        }

        private void ResetSelectedInstrumentArpeggio()
        {
            if (!project.InstrumentExists(selectedInstrument))
                selectedInstrument = project.Instruments.Count > 0 ? project.Instruments[0] : null;
            if (!project.ArpeggioExists(selectedArpeggio))
                selectedArpeggio = null;
        }

        private void ProjectExplorer_ProjectModified()
        {
            ResetEverything();
            RefreshLayout();
        }

        private void ProjectExplorer_InstrumentDeleted(Instrument instrument)
        {
            ResetSelectedInstrumentArpeggio();
            PianoRoll.Reset(selectedChannelIndex);
            Sequencer.InvalidatePatternCache();
        }

        private void ProjectExplorer_ArpeggioDeleted(Arpeggio arpeggio)
        {
            ResetSelectedInstrumentArpeggio();
            PianoRoll.Reset(selectedChannelIndex);
        }

        private void ProjectExplorer_DPCMSampleDeleted(DPCMSample sample)
        {
            PianoRoll.Reset(selectedChannelIndex);
        }

        private void PianoRoll_NotesPasted()
        {
            RefreshProjectExplorerButtons();
        }

        private void ProjectExplorer_InstrumentDroppedOutside(Instrument instrument, Point pos)
        {
            var pianoRollPos = PianoRoll.ScreenToControl(pos);
            if (PianoRoll.ClientRectangle.Contains(pianoRollPos))
                PianoRoll.ReplaceSelectionInstrument(instrument, pianoRollPos);
            var sequencerPos = Sequencer.ScreenToControl(pos);
            if (Sequencer.ClientRectangle.Contains(sequencerPos))
                Sequencer.ReplaceSelectionInstrument(instrument, sequencerPos);
        }

        private void ProjectExplorer_ArpeggioDroppedOutside(Arpeggio arpeggio, Point pos)
        {
            var pianoRollPos = PianoRoll.ScreenToControl(pos);
            if (PianoRoll.ClientRectangle.Contains(pianoRollPos))
                PianoRoll.ReplaceSelectionArpeggio(arpeggio, pianoRollPos);
            var sequencerPos = Sequencer.ScreenToControl(pos);
            if (Sequencer.ClientRectangle.Contains(sequencerPos))
                Sequencer.ReplaceSelectionArpeggio(arpeggio, sequencerPos);
        }

        private void Sequencer_PatternModified()
        {
            PianoRoll.MarkDirty();
        }

        public bool Run(string[] args)
        {
            var win = FamiStudioWindow.CreateWindow(this);

            if (win == null)
            {
                Platform.MessageBox(null, "Error initializing OpenGL.", "Error", MessageBoxButtons.OK);
                return false;
            }

            #if FAMISTUDIO_MACOS
                var filename = MacUtils.GetInitialOpenDocument();   
            #else
                var filename = Array.Find(args, a => !a.StartsWith("-"));
            #endif

            Initialize(win, filename);
            window.Run();

            return true;
        }

        public void ShowContextMenuAsync(ContextMenuOption[] options)
        {
            if (options != null && options.Length > 0)
            {
                window.ShowContextMenuAsync(options);
            }
        }

        public void HideContextMenu()
        {
            window.HideContextMenu();
        }

        private void InitializeKeys()
        {

        }

        private void InitializeMetronome()
        {
            metronomeSound = WaveFile.LoadFromResource("FamiStudio.Resources.Sounds.Metronome.wav", out _);
            WaveUtils.AdjustVolume(metronomeSound, Settings.MetronomeVolume / 100.0f);
        }

        private void InitializeDeviceChangeEvent()
        {
            Platform.AudioDeviceChanged += Platform_AudioDeviceChanged;
        }

        private void Platform_AudioDeviceChanged()
        {
            if (songStream != null && songStream.RecreateOnDeviceChanged)
            {
                audioDeviceChanged = true;
            }
        }

        private void InitializeMidi()
        {
            Midi.Initialize();

            Midi.Close();
            Midi.NotePlayed -= Midi_NotePlayed;

            while (midiNoteQueue.TryDequeue(out _)) ;

            if (Midi.InputCount > 0)
            {
                int midiDeviceIndex = 0;
                if (Settings.MidiDevice.Length > 0)
                {
                    var numMidiDevices = Midi.InputCount;
                    for (int i = 0; i < numMidiDevices; i++)
                    {
                        if (Midi.GetDeviceName(i) == Settings.MidiDevice)
                        {
                            midiDeviceIndex = i;
                            break;
                        }
                    }
                }

                if (Midi.Open(midiDeviceIndex))
                    Midi.NotePlayed += Midi_NotePlayed;
            }
        }

        public void DisplayNotification(string msg, bool beep = true, bool forceLongDuration = false)
        {
            Platform.ShowToast(window, msg, forceLongDuration || msg.Length > 60);

            if (beep)
                Platform.Beep();
        }

        private void UndoRedoManager_PreUndoRedo(TransactionScope scope, TransactionFlags flags)
        {
            Debug.Assert(!window.IsAsyncDialogInProgress);

            ValidateIntegrity();

            // Special category for stuff that is so important, we should stop the song.
            if (flags.HasFlag(TransactionFlags.StopAudio))
            {
                StopEverything();
                SeekSong(0);
            }

            if (flags.HasFlag(TransactionFlags.RecreateStreams))
            {
                ShutdownAudioPlayers(true);
            }
            else if (flags.HasFlag(TransactionFlags.RecreatePlayers))
            {
                ShutdownAudioPlayers();
            }
        }

        private void UndoRedoManager_PostUndoRedo(TransactionScope scope, TransactionFlags flags)
        {
            Debug.Assert(!window.IsAsyncDialogInProgress);

            ValidateIntegrity();

            if (flags.HasFlag(TransactionFlags.RecreatePlayers))
            {
                palPlayback = project.PalMode;
                InitializeAudioPlayers();
                MarkEverythingDirty();
            }

            if (flags.HasFlag(TransactionFlags.StopAudio))
            {
                MarkEverythingDirty();
            }

            if (scope == TransactionScope.Instrument && songPlayer != null)
            {
                songPlayer.ForceInstrumentsReload();
            }

            ConditionalAutoSave();
        }

        private void UndoRedoManager_Updated()
        {
            ToolBar.MarkDirty();
        }

        private void ValidateIntegrity()
        {
        #if DEBUG
            if (song != null)
                Debug.Assert(project.SongExists(song));
            if (selectedInstrument != null)
                Debug.Assert(project.InstrumentExists(selectedInstrument));
            if (selectedArpeggio != null)
                Debug.Assert(project.ArpeggioExists(selectedArpeggio));

            Sequencer.ValidateIntegrity();
            PianoRoll.ValidateIntegrity();
        #endif
        }

        public void TrySaveProjectAsync(Action callback)
        {
            if (undoRedoManager != null && undoRedoManager.NeedsSaving)
            {
                Platform.MessageBoxAsync(window, SaveChangesDialog, ConfirmTitle, MessageBoxButtons.YesNoCancel, (r) =>
                {
                    if (r == DialogResult.No)
                        callback();
                    else if (r == DialogResult.Yes)
                        SaveProjectAsync(false, callback);
                });
            }
            else
            {
                callback();
            }
        }

        private void InitializeUndoRedoManager()
        {
            undoRedoManager = new UndoRedoManager(project, this);
            undoRedoManager.PreUndoRedo += UndoRedoManager_PreUndoRedo;
            undoRedoManager.PostUndoRedo += UndoRedoManager_PostUndoRedo;
            undoRedoManager.TransactionBegan += UndoRedoManager_PreUndoRedo;
            undoRedoManager.TransactionEnded += UndoRedoManager_PostUndoRedo;
            undoRedoManager.Updated += UndoRedoManager_Updated;
        }

        private void ShutdownUndoRedoManager()
        {
            undoRedoManager.PreUndoRedo -= UndoRedoManager_PreUndoRedo;
            undoRedoManager.PostUndoRedo -= UndoRedoManager_PostUndoRedo;
            undoRedoManager.TransactionBegan -= UndoRedoManager_PreUndoRedo;
            undoRedoManager.TransactionEnded -= UndoRedoManager_PostUndoRedo;
            undoRedoManager.Updated -= UndoRedoManager_Updated;
            undoRedoManager = null;
        }

        private void UnloadProject()
        {
            if (undoRedoManager != null)
            {
                FreeExportDialog();
                ShutdownUndoRedoManager();
                StopEverything();
                project = null;
                song = null;
                selectedInstrument = null;
                selectedArpeggio = null;
            }
            else
            {
                Debug.Assert(project == null);
            }
        }

        public void NewProject(bool isDefault = false)
        {
            TrySaveProjectAsync(() =>
            {
                UnloadProject();

                project = new Project(true);
                InitProject();

                if (!isDefault)
                    Settings.LastProjectFile = "";
            });
        }

        private void FreeExportDialog()
        {
            if (exportDialog != null)
            {
                exportDialog.Exporting -= ExportDialog_Exporting;
                //exportDialog.DestroyControls();
                exportDialog = null;
            }
        }

        private void InitProject()
        {
            StopRecording();
            ShutdownAudioPlayers(songStream != null && songStream.Stereo != project.OutputsStereoAudio);

            StaticProject = project;
            song = project.Songs[0];
            palPlayback = project.PalMode;
            Sequencer.SetHideEmptyChannels(false);

            ResetEverything();
            InitializeAutoSave();
            InitializeAudioPlayers();
            FreeExportDialog();
            InitializeUndoRedoManager();
            FixMobileProjectFilename();
            SaveLastOpenProjectFile();
            SaveWorkInProgress();
            MarkEverythingDirty();
            UpdateTitle();
            RefreshLayout();
            ClearFontCaches();
        }

        private void ClearFontCaches()
        {
            Window.Fonts.ClearGlyphCache(Window.Graphics);
        }

        private void ResetEverything()
        {
            ResetSelectedChannel();
            ResetSelectedInstrumentArpeggio();
            ResetSelectedSong();
            MarkEverythingDirty();

            ToolBar.Reset();
            ProjectExplorer.Reset();
            Sequencer.Reset();
            PianoRoll.Reset(selectedChannelIndex);
        }

        private void FixMobileProjectFilename()
        {
            if (Platform.IsMobile)
            {
                if (!string.IsNullOrEmpty(project.Filename))
                {
                    if (project.Filename.ToLower() == WipProject.ToLower())
                    {
                        LoadWipSettings();
                    }
                    else if (!project.Filename.ToLower().StartsWith(Platform.UserProjectsDirectory.ToLower()))
                    {
                        project.Filename = null;
                    }
                }
            }
        }

        private void SaveLastOpenProjectFile()
        {
            if (project.Filename != null && Path.GetExtension(project.Filename).ToLower() == ".fms")
                Settings.LastProjectFile = project.Filename;
        }

        private void OpenProjectInternal(string filename)
        {
            Debug.Assert(project == null && undoRedoManager == null);

            StopEverything();

            BeginLogTask();
            OpenProjectFileAsync(filename, true, (p) => 
            {
                project = p;

                if (project != null)
                {
                    InitProject();
                }
                else
                {
                    NewProject();
                }

                window.Refresh();
                EndLogTask();
            });
        }

        public void OpenProject(string filename = null)
        {
            Action<string> UnloadAndOpenAction = (f) =>
            {
                UnloadProject();
                OpenProjectInternal(f);
            };

            TrySaveProjectAsync(() =>
            {
                if (Platform.IsDesktop)
                {
                    if (filename == null)
                        filename = Platform.ShowOpenFileDialog(DesktopOpenProjectTitle, "All Supported Files (*.fms;*.txt;*.nsf;*.nsfe;*.ftm;*.mid;*.vgm;*.vgz)|*.fms;*.txt;*.nsf;*.nsfe;*.ftm;*.mid;*.vgm;*.vgz|FamiStudio Files (*.fms)|*.fms|FamiTracker Files (*.ftm)|*.ftm|FamiTracker Text Export (*.txt)|*.txt|FamiStudio Text Export (*.txt)|*.txt|NES Sound Format (*.nsf;*.nsfe)|*.nsf;*.nsfe|MIDI files (*.mid)|*.mid|VGM files (*.vgm;*.vgz)|*.vgm;*.vgz", ref Settings.LastFileFolder);

                    if (filename != null)
                        UnloadAndOpenAction(filename);
                }
                else
                {
                    var dlg = new MobileProjectDialog(this, MobileOpenProjectTitle, false);
                    dlg.ShowDialogAsync((f) =>
                    {
                        // HACK : We don't support nested activities right now, so return
                        // this special code to signal that we should open from storage.
                        if (f == "///STORAGE///")
                            Platform.StartMobileLoadFileOperationAsync("*/*", (fs) => { UnloadAndOpenAction(fs); });
                        else
                            UnloadAndOpenAction(f);
                    });
                }
            });
        }

        private void ConditionalShowFamiTrackerMobileWarning()
        {
            if (Platform.IsMobile && 
                (Log.GetLastMessage(LogSeverity.Warning) != null ||
                 Log.GetLastMessage(LogSeverity.Error)   != null))
            {
                Platform.DelayedMessageBoxAsync(MobileFamiTrackerImportWarning, WarningTitle);
            }
        }

        public void OpenProjectFileAsync(string filename, bool allowComplexFormats, Action<Project> action)
        {
            var extension = Path.GetExtension(filename.ToLower());

            var fms = extension == ".fms";
            var ftm = extension == ".ftm";
            var txt = extension == ".txt";
            var vgm = extension == ".vgm" || extension == ".vgz";
            var nsf = extension == ".nsf" || extension == ".nsfe";
            var mid = extension == ".mid";

            var requiresDialog = allowComplexFormats && (nsf || mid || vgm);

            var project = (Project)null;

            if (requiresDialog)
            {
                // HACK : Our rendering code currently always requires a valid project/song
                // to be present so we create a dummy project while the MIDI/NSF dialog is
                // running.
                Debug.Assert(this.project == null);

                this.project = new Project(true);
                this.song = this.project.Songs[0];
                this.ResetEverything();

                Action<Project> ClearTemporaryProjectAndInvokeAction = (p) =>
                {
                    // Undo the hack mentionned above.
                    this.song = null;
                    this.project = null;
                    action(p);
                };

                if (mid)
                {
                    var dlg = new MidiImportDialog(window, filename);
                    dlg.ShowDialogAsync(window, ClearTemporaryProjectAndInvokeAction);
                }
                else if (nsf)
                {
                    var dlg = new NsfImportDialog(window, filename);
                    dlg.ShowDialogAsync(window, ClearTemporaryProjectAndInvokeAction);
                }
                else if (vgm)
                {
                    var dlg = new VgmImportDialog(window, filename, ClearTemporaryProjectAndInvokeAction);
                    dlg.ShowDialogAsync(window, ClearTemporaryProjectAndInvokeAction);
                }
            }
            else
            {
                if (fms)
                {
                    project = new ProjectFile().Load(filename);
                }
                else if (ftm)
                {
                    project = new FamitrackerBinaryFile().Load(filename);
                    ConditionalShowFamiTrackerMobileWarning();
                }
                else if (txt)
                {
                    if (FamistudioTextFile.LooksLikeFamiStudioText(filename))
                    {
                        project = new FamistudioTextFile().Load(filename);
                    }
                    else
                    {
                        project = new FamitrackerTextFile().Load(filename);
                        ConditionalShowFamiTrackerMobileWarning();
                    }
                }

                action(project);
            }

            if (Platform.IsDesktop && allowComplexFormats)
            {
                Settings.AddRecentFile(filename);
                Settings.Save();
            }
        }

        public void SaveProjectAsync(bool forceSaveAs = false, Action callback = null)
        {
            var filename = project.Filename;

            if (forceSaveAs || string.IsNullOrEmpty(filename))
            {
                if (Platform.IsDesktop)
                {
                    filename = Platform.ShowSaveFileDialog(DesktopSaveProjectTitle, "FamiStudio Files (*.fms)|*.fms", ref Settings.LastFileFolder);
                    if (filename != null)
                    {
                        SaveProjectInternal(filename);
                        callback?.Invoke();
                    }
                }
                else
                {
                    var dlg = new MobileProjectDialog(this, MobileSaveProjectTitle, true);
                    dlg.ShowDialogAsync((f) =>
                    {
                        SaveProjectInternal(f);
                        callback?.Invoke();
                    });
                }
            }
            else
            {
                SaveProjectInternal(filename);
                callback?.Invoke();
            }
        }

        private void SaveProjectInternal(string filename)
        {
            var success = new ProjectFile().Save(project, filename);

            if (success)
            {
                UpdateTitle();
                Settings.LastProjectFile = project.Filename;
                Settings.AddRecentFile(project.Filename);

                if (Settings.ClearUndoRedoOnSave)
                    undoRedoManager.Clear();

                undoRedoManager.NotifySaved();
                DisplayNotification(ProjectSaveSuccess, false);
            }
            else
            {
                Platform.ShowToast(window, ProjectSaveError);
            }

            MarkEverythingDirty();
            SaveWorkInProgress();
        }

        public void Export()
        {
            FreeExportDialog();

            exportDialog = new ExportDialog(window);
            exportDialog.Exporting += ExportDialog_Exporting;
            exportDialog.ShowDialogAsync();
        }

        public void RepeatLastExport()
        {
            if (exportDialog == null || !exportDialog.HasAnyPreviousExport)
            {
                DisplayNotification(NoLastExportWarning);
            }
            else if (!exportDialog.IsProjectStillCompatible(project))
            {
                DisplayNotification(ProjectChangedExportWarning);
                FreeExportDialog();
            }
            else
            {
                exportDialog.Export(true);
            }
        }

        private void ExportDialog_Exporting()
        {
            StopEverything();

            // Make sure we arent in real-time mode, this mean we will 
            // be constantly rendering frames as we export.
            if (AppNeedsRealTimeUpdate())
            {
                PianoRoll.Reset(selectedChannelIndex);
                Debug.Assert(!AppNeedsRealTimeUpdate());
            }
        }

        public void Suspend()
        {
            // Null window means Initialized() was never call. This can happen when starting
            // the app with the phone sleeping. It will resume and suspend on the same frame, 
            // not leaving any time for the GL thread to start.
            if (!suspended && window != null) 
            {
                suspended = true;
                StopEverything();
                ShutdownAudioPlayers(true);
                SaveWorkInProgress();
            }
        }

        public void Resume()
        {
            if (suspended)
            {
                suspended = false;
                InitializeAudioPlayers();
                MarkEverythingDirty();
            }
        }

        public void OpenConfigDialog()
        {
            var dlg = new ConfigDialog(window);

            dlg.ShowDialogAsync((r) =>
            {
                if (r != DialogResult.Cancel) 
                {
                    // Yes = audio settings changed.
                    if (r == DialogResult.Yes)
                        RecreateAudioPlayers(true);

                    RefreshLayout();
                    RefreshProjectExplorerButtons();
                    InvalidatePatternCache();
                    InitializeMidi();
                    MarkEverythingDirty();
                }
            });
        }

        private void RefreshProjectExplorerButtons()
        {
            ProjectExplorer.RefreshButtons();
        }

        private void InvalidatePatternCache()
        {
            Sequencer.InvalidatePatternCache();
        }

        public bool TryClosing()
        {
            var close = false;

            // This only runs on desktop and desktop doesnt run anything async.
            TrySaveProjectAsync(() =>
            {
                UnloadProject();

                Midi.NotePlayed -= Midi_NotePlayed;
                Midi.Shutdown();

                StopEverything();
                ShutdownAudioPlayers(true);
                SaveSettings();

                close = true;
            });

            return close;
        }

        private void ApplySettings()
        {
            PianoRoll.ApplySettings();
        }

        private void SaveSettings()
        {
            PianoRoll.SaveSettings();
            Settings.Save();
        }

        private void RefreshLayout()
        {
            window.RefreshLayout();
            Sequencer.LayoutChanged();
            PianoRoll.LayoutChanged();
            ToolBar.LayoutChanged();
            ProjectExplorer.LayoutChanged();
        }

        private void CheckForNewRelease()
        {
        #if !FAMISTUDIO_ANDROID
            try
            {
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri("https://api.github.com/repos/BleuBleu/FamiStudio/releases/latest");
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("FamiStudio");
                    var response = client.GetAsync("").Result;
                    if (response.IsSuccessStatusCode)
                    {
                        var json = response.Content.ReadAsStringAsync().Result;
                        var release = JsonSerializer.Deserialize<dynamic>(json, new JsonSerializerOptions { Converters = { new DynamicJsonConverter() } });

                        newReleaseString = release["tag_name"].ToString();
                        newReleaseUrl = release["html_url"].ToString();

                        var appVersion = Utils.SplitVersionNumber(Platform.ApplicationVersion, out var betaNumber);
                        var versionComparison = string.Compare(newReleaseString, appVersion, StringComparison.OrdinalIgnoreCase);
                        var newerVersionAvailable = versionComparison > 0;

                        if (betaNumber > 0)
                        {
                            // If we were running a development version, but an official version of 
                            // the same number appears on GitHub, prompt for update.
                            if (!newerVersionAvailable && versionComparison == 0)
                            {
                                newerVersionAvailable = true;
                            }
                        }

                        // Assume > alphabetical order means newer version.
                        if (newerVersionAvailable)
                        {
                            // Make sure this release applies to our platform (eg. a hotfix for macos should not impact Windows).
                            var assets = release["assets"];
                            foreach (var asset in assets)
                            {
                                var name = (string)asset["name"];
                                var token = Platform.IsWindows ? "win" : (Platform.IsMacOS ? "macos" : "linux");

                                if (name != null && name.ToLower().Contains(token))
                                {
                                    newReleaseAvailable = true;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
            }

            newReleaseCheckDone = true;
        #endif
        }

        private void InitializeAutoSave()
        {
            var path = Settings.GetAutoSaveFilePath();
            var maxTime = DateTime.MinValue;
            var maxIdx = -1;

            Directory.CreateDirectory(path);

            for (int i = 0; i < MaxAutosaves; i++)
            {
                var filename = Path.Combine(path, $"AutoSave{i:D2}.fms");

                if (File.Exists(filename))
                {
                    var time = File.GetLastWriteTime(filename);
                    if (time > maxTime)
                    {
                        maxTime = time;
                        maxIdx = i;
                    }
                }
            }

            autoSaveIndex = (maxIdx + 1) % MaxAutosaves;
            lastAutoSave = DateTime.Now;
        }

        private void ConditionalAutoSave()
        {
            if (Settings.AutoSaveCopy)
            {
                var now = DateTime.Now;
                var timespan = now - lastAutoSave;

                if (timespan.TotalMinutes > 2)
                {
                    if (Platform.IsDesktop)
                    {
                        var path = Settings.GetAutoSaveFilePath();
                        var filename = Path.Combine(path, $"AutoSave{autoSaveIndex:D2}.fms");

                        SaveProjectCopy(filename);
                    }
                    else
                    {
                        SaveWorkInProgress();
                    }

                    autoSaveIndex = (autoSaveIndex + 1) % MaxAutosaves;
                    lastAutoSave = now;
                }
            }
        }

        public void SaveProjectCopy(string filename)
        {
            var oldFilename = project.Filename;
            new ProjectFile().Save(project, filename);
            project.Filename = oldFilename;
        }

        public void SaveWorkInProgress()
        {
            if (Platform.IsMobile)
            {
                //Debug.Assert(Platform.IsInMainThread());

                SaveProjectCopy(WipProject);
                SaveWipSettings();
            }

            SaveSettings();
        }

        private void LoadWipSettings()
        {
            var ini = new IniFile();

            if (ini.Load(WipSettings))
            {
                project.Filename = ini.GetString("General", "Filename", null);
                if (ini.GetBool("General", "Dirty", false))
                    undoRedoManager.ForceDirty();
            }
            else
            {
                project.Filename = null;
            }
        }

        private void SaveWipSettings()
        {
            // Save the actual project filename in a text file along with it.
            if (!string.IsNullOrEmpty(project.Filename))
            {
                var ini = new IniFile();
                ini.SetString("General", "Filename", project.Filename);
                ini.SetBool("General", "Dirty", undoRedoManager.NeedsSaving);
                ini.Save(WipSettings);
            }
            else
            {
                File.Delete(WipSettings);
            }
        }

        private void CheckNewReleaseDone()
        {
            if (newReleaseCheckDone)
            {
                if (newReleaseAvailable)
                {
                    newReleaseAvailable = false;
                    Platform.ShowToast(window, NewVersionToast.Format(newReleaseString), true, () => Platform.OpenUrl("http://www.famistudio.org"));
                }
                else if (Settings.NewVersionCounter > 0)
                {
                    var version = Utils.SplitVersionNumber(Platform.ApplicationVersion, out _);
                    Platform.ShowToast(window, NewVersionWelcome.Format(version), true, () =>
                    {
                        Platform.OpenUrl(Utils.GetReleaseUrl(Platform.ApplicationVersion));
                        Settings.NewVersionCounter = 0;
                        Settings.Save();
                    });

                    Settings.NewVersionCounter--;
                    Settings.Save();
                }

                newReleaseCheckDone = false;
            }
        }

        public void OpenTransformDialog()
        {
            var dlg = new TransformDialog(window);
            dlg.CleaningUp += TransformDialog_CleaningUp;
            dlg.ShowDialogAsync((r) =>
            {
                if (r == DialogResult.OK)
                    ResetEverything();
            });
        }

        private void TransformDialog_CleaningUp()
        {
            StopEverything();
            ResetEverything();
        }

        public void ShowHelp()
        {
            Platform.OpenUrl("http://www.famistudio.org/doc/index.html");
        }

        private void UpdateTitle()
        {
            string projectFile = NewProjectTitle;

            if (!string.IsNullOrEmpty(project.Filename))
                projectFile = System.IO.Path.GetFileName(project.Filename);

            var version = Utils.SplitVersionNumber(Platform.ApplicationVersion, out var betaNumber);
            var title = $"FamiStudio {version} - {projectFile}";

            if (betaNumber > 0)
                title += $" - BETA {betaNumber} - DEVELOPMENT VERSION DO NOT DISTRIBUTE!";

            window.Text = title;
        }

        public void ShowInstrumentError(Channel channel, bool beep)
        {
            var message = IncompatibleInstrumentError.Value;
            if (channel != null)
            {
                if (channel.IsExpansionChannel)
                    message += $"\n{IncompatibleExpRequiredError}";
            }
            DisplayNotification(message, beep);
        }

        public void PlayInstrumentNote(int n, bool showWarning, bool allowRecording, bool custom = false, Instrument customInstrument = null, Arpeggio customArpeggio = null, float stopDelay = 0.0f)
        {
            Note note = new Note(n);
            note.Volume = Note.VolumeMax;

            var instrument = custom ? customInstrument : selectedInstrument;
            var arpeggio   = custom ? customArpeggio   : selectedArpeggio;

            int channel = selectedChannelIndex;

            if (instrument == null)
                return;

            if (song.Channels[channel].SupportsInstrument(instrument))
            {
                note.Instrument = instrument;

                if (song.Channels[channel].SupportsArpeggios && arpeggio != null)
                    note.Arpeggio = arpeggio;
            }
            else
            {
                if (showWarning)
                    ShowInstrumentError(song.Channels[channel], false);
                return;
            }

            instrumentPlayer.PlayNote(channel, note);

            if (allowRecording && recordingMode)
                PianoRoll.RecordNote(note);

            stopInstrumentTimer = stopDelay;
        }

        public void StopOrReleaseIntrumentNote(bool allowRecording = false)
        {
            var channel = song.Channels[selectedChannelIndex];

            if (selectedInstrument != null && 
                selectedInstrument.CanRelease(channel) &&
                channel.SupportsInstrument(selectedInstrument))
            {
                instrumentPlayer.ReleaseNote(selectedChannelIndex);
            }
            else
            {
                instrumentPlayer.StopAllNotes();
            }
        }

        public void StopInstrument()
        {
            if (instrumentPlayer != null)
                instrumentPlayer.StopAllNotes();
        }

        public void StopEverything()
        {
            StopSong();
            StopInstrument();
        }

        private void InitializeSongPlayer()
        {
            Debug.Assert(songPlayer == null);
            Sequencer.GetPatternTimeSelectionRange(out var min, out var max);

            if (songStream == null)
            {
                songStream = Platform.CreateAudioStream(Settings.AudioAPI, NesApu.EmulationSampleRate, project.OutputsStereoAudio, Settings.AudioBufferSize);
                if (songStream == null)
                    DisplayNotification(AudioStreamError, true, true);
            }

            songPlayer = new SongPlayer(songStream, palPlayback, NesApu.EmulationSampleRate, project.OutputsStereoAudio, Settings.NumBufferedFrames);
            songPlayer.AccurateSeek = Settings.AccurateSeek;
            songPlayer.SetMetronomeSound(metronome ? metronomeSound : null);
            songPlayer.SetSelectionRange(min, max);
        }

        private void InitializeInstrumentPlayer()
        {
            Debug.Assert(instrumentPlayer == null);

            if (instrumentStream == null)
            { 
                instrumentStream = Platform.CreateAudioStream(Settings.AudioAPI, NesApu.EmulationSampleRate, project.OutputsStereoAudio, Settings.AudioBufferSize);
                if (instrumentStream == null)
                    DisplayNotification(AudioStreamError, true, true);
            }

            instrumentPlayer = new InstrumentPlayer(instrumentStream, palPlayback, NesApu.EmulationSampleRate, project.OutputsStereoAudio, Settings.NumBufferedFrames); 
            instrumentPlayer.Start(project, palPlayback);
        }

        private void ShutdownSongPlayer(bool shutdownStream)
        {
            if (songPlayer != null)
            {
                songPlayer.Stop();
                songPlayer.Shutdown();
                songPlayer = null;
            }

            if (shutdownStream && songStream != null)
            {
                songStream.Stop();
                songStream.Dispose();
                songStream = null;
            }
        }

        private void ShutdownInstrumentPlayer(bool shutdownStream)
        {
            if (instrumentPlayer != null)
            {
                instrumentPlayer.Stop();
                instrumentPlayer.Shutdown();
                instrumentPlayer = null;
                PianoRoll.HighlightPianoNote(Note.NoteInvalid);
            }

            if (shutdownStream && instrumentStream != null)
            {
                instrumentStream.Stop();   
                instrumentStream.Dispose(); 
                instrumentStream = null;    
            }
        }

        public void InitializeOscilloscope()
        {
            Debug.Assert(oscilloscope == null);

            oscilloscope = new OscilloscopeGenerator(project.OutputsStereoAudio);
            oscilloscope.Start();

            if (instrumentPlayer != null)
                instrumentPlayer.ConnectOscilloscope(oscilloscope);
        }

        public void ShutdownOscilloscope()
        {
            if (songPlayer != null)
                songPlayer.ConnectOscilloscope(null);
            if (instrumentPlayer != null)
                instrumentPlayer.ConnectOscilloscope(null);

            if (oscilloscope != null)
            {
                oscilloscope.Stop();
                oscilloscope = null;
            }
        }

        public float[] GetOscilloscopeGeometry(out bool hHasNonZeroSample)
        {
            if (oscilloscope != null)
                return oscilloscope.GetGeometry(out hHasNonZeroSample);

            hHasNonZeroSample = false;
            return null;
        }

        public void PreviewDPCMSample(DPCMSample sample, bool source)
        {
            previewDPCMSampleId = sample.Id;
            previewDPCMIsSource = source;

            byte[] dmcData = null;
            int    dmcRateIndex = 15;

            if (source)
            {
                previewDPCMSampleRate = (int)sample.SourceSampleRate;

                if (sample.SourceDataIsWav)
                {
                    instrumentPlayer.PlayRawPcmSample(sample.SourceWavData.Samples, sample.SourceWavData.SampleRate, NesApu.DPCMVolume * Utils.DbToAmplitude(Settings.GlobalVolumeDb));
                    return;
                }
                else
                {
                    dmcData = sample.SourceDmcData.Data;
                }
            }
            else
            {
                previewDPCMSampleRate = (int)sample.ProcessedSampleRate;
                dmcData = sample.ProcessedData;
                dmcRateIndex = sample.PreviewRate;
            }

            var playRate = (int)Math.Round(DPCMSampleRate.Frequencies[palPlayback ? 1 : 0, dmcRateIndex]);
            WaveUtils.DpcmToWave(dmcData, sample.DmcInitialValueDiv2, out short[] wave);

            if (wave.Length > 0)
                instrumentPlayer.PlayRawPcmSample(wave, playRate, NesApu.DPCMVolume * Utils.DbToAmplitude(Settings.GlobalVolumeDb));
        }

        public void PlayRawPcmSample(short[] data, int sampleRate, float volume = 1.0f, int channel = 0)
        {
            if (instrumentPlayer != null)
            {
                instrumentPlayer.PlayRawPcmSample(data, sampleRate, volume, channel);
            }
        }

        public bool PalPlayback
        {
            get
            {
                return palPlayback;
            }
            set
            {
                // This is needed in case we change the PalPlayack while in the middle 
                // of a transaction with ReinitializeAudio flag.
                var playersWereValid = songPlayer != null;

                if (playersWereValid)
                    ShutdownAudioPlayers(false);

				palPlayback = value;
				if (project.UsesFamiTrackerTempo)
	                project.PalMode = value;

                if (playersWereValid)
                    InitializeAudioPlayers();

                MarkEverythingDirty();
            }
        }
        
        public string GetRecordingKeyString(int noteValue)
        {
            noteValue = (noteValue - baseRecordingOctave * 12) - 1;

            var str = (string)null;

            if (noteValue >= 0 && noteValue < Settings.QwertyNoteShortcuts.Length)
            {
                var shortcut = Settings.QwertyNoteShortcuts[noteValue];
                if (shortcut.IsShortcutValid(0))
                {
                    str += Settings.QwertyNoteShortcuts[noteValue].ToDisplayString(0);
                }

                if (shortcut.IsShortcutValid(1))
                {
                    if (str != null)
                        str += "   ";
                    str += Settings.QwertyNoteShortcuts[noteValue].ToDisplayString(1);
                }
            }

            return str;
        }

        protected bool HandleRecordingKey(KeyEventArgs e, bool keyDown, bool repeat)
        {
            for (int i = 0; i < Settings.QwertyNoteShortcuts.Length; i++)
            {
                if (Settings.QwertyNoteShortcuts[i].Matches(e))
                {
                    if (keyDown && repeat)
                        return true;

                    if (keyDown)
                    {
                        var noteValue = i;
                        noteValue = noteValue + Note.FromFriendlyName("C0") + (baseRecordingOctave * 12);
                        noteValue = Utils.Clamp(noteValue, Note.MusicalNoteMin, Note.MusicalNoteMax);
                        lastRecordingKeyDown = e.Scancode;

                        PlayInstrumentNote(noteValue, true, true);
                    }
                    else if (e.Scancode == lastRecordingKeyDown)
                    {
                        lastRecordingKeyDown = -1;
                        StopOrReleaseIntrumentNote(false);
                    }

                    return true;
                }
            }

            return false;
        }

        public void KeyDown(KeyEventArgs e)
        {
            // Prevent loosing focus on Alt.
            if (e.Key == Keys.Menu)
                e.Handled = true;

            if (e.Key == Keys.Escape)
            {
                StopInstrument();
                StopRecording();
            }

            if (e.Key == Keys.Q && e.Shift)
            {
                ToggleQwertyPiano();
                return;
            }

            if ((recordingMode || qwertyPiano) && HandleRecordingKey(e, true, e.IsRepeat))
            {
                return;
            }

            if (recordingMode && Settings.QwertyStopShortcut.Matches(e))
            {
                StopOrReleaseIntrumentNote(true);
            }
            else if (recordingMode && Settings.QwertySkipShortcut.Matches(e))
            {
                PianoRoll.AdvanceRecording(CurrentFrame, true);
            }
            else if (recordingMode && Settings.QwertyBackShortcut.Matches(e))
            {
                PianoRoll.DeleteRecording(CurrentFrame);
            }
            else if ((recordingMode || qwertyPiano) && Settings.QwertyOctaveUpShortcut.Matches(e))
            {
                baseRecordingOctave = Math.Min(7, baseRecordingOctave + 1);
                PianoRoll.MarkDirty();
            }
            else if ((recordingMode || qwertyPiano) && Settings.QwertyOctaveDownShortcut.Matches(e))
            {
                baseRecordingOctave = Math.Max(0, baseRecordingOctave - 1);
                PianoRoll.MarkDirty();
            }
            else if (Settings.RecordingShortcut.Matches(e))
            {
                ToggleRecording();
            }
            else if (Settings.FollowModeShortcut.Matches(e))
            {
                followMode = !followMode;
                ToolBar.MarkDirty();
            }
            else if (Settings.PlayShortcut.Matches(e))
            {
                TogglePlaySong();
            }
            else if (Settings.PlayFromLoopShortcut.Matches(e))
            {
                TogglePlaySongFromLoopPoint();
            }
            else if (Settings.PlayFromPatternShortcut.Matches(e))
            {
                TogglePlaySongFromStartOfPattern();
            }
            else if (Settings.PlayFromStartShortcut.Matches(e))
            {
                TogglePlaySongFromBeginning();
            }
            else if (Settings.SeekStartShortcut.Matches(e))
            {
                SeekSong(0);
            }
            else if (Settings.SeekStartPatternShortcut.Matches(e))
            {
                SeekCurrentPattern();
            }
            else if (Settings.RedoShortcut.Matches(e))
            {
                undoRedoManager.Redo();
            }
            else if (Settings.UndoShortcut.Matches(e))
            {
                undoRedoManager.Undo();
            }
            else if (Settings.FileNewShortcut.Matches(e))
            {
                NewProject();
            }
            else if (Settings.FileOpenShortcut.Matches(e))
            {
                OpenProject();
            }
            else if (Settings.FileSaveShortcut.Matches(e) && !UndoRedoManager.HasTransactionInProgress)
            {
                SaveProjectAsync(false);
            }
            else if (Settings.FileSaveAsShortcut.Matches(e) && !UndoRedoManager.HasTransactionInProgress)
            {
                SaveProjectAsync(true);
            }
            else if (Settings.FileExportShortcut.Matches(e))
            {
                Export();
            }
            else if (Settings.FileExportRepeatShortcut.Matches(e))
            {
                RepeatLastExport();
            }
            else if (Settings.QwertyShortcut.Matches(e))
            {
                ToggleQwertyPiano();
            }
            else if (Platform.IsMacOS && e.Control && e.Key == Keys.Q)
            {
                if (TryClosing())
                    window.Quit();
            }
            else if (!recordingMode)
            {
                for (int i = 0; i < Settings.ActiveChannelShortcuts.Length; i++)
                {
                    if (Settings.ActiveChannelShortcuts[i].Matches(e))
                    {
                        SelectedChannelIndex = i;
                        Sequencer.MarkDirty();
                        break;
                    }
                }

                for (int i = 0; i < Settings.DisplayChannelShortcuts.Length; i++)
                {
                    if (Settings.DisplayChannelShortcuts[i].Matches(e))
                    {
                        ForceDisplayChannelMask ^= (1L << i);
                        Sequencer.MarkDirty();
                        break;
                    }
                }
            }
        }

        public bool CanCopy       => PianoRoll.IsActiveControl && PianoRoll.CanCopy   || Sequencer.IsActiveControl && Sequencer.CanCopy;
        public bool CanCopyAsText => PianoRoll.IsActiveControl && PianoRoll.CanCopyAsText;
        public bool CanPaste      => PianoRoll.IsActiveControl && PianoRoll.CanPaste  || Sequencer.IsActiveControl && Sequencer.CanPaste;
        public bool CanDelete     => PianoRoll.IsActiveControl && PianoRoll.CanDelete || Sequencer.IsActiveControl && Sequencer.CanDelete;

        public void Copy()
        {
            if (PianoRoll.IsActiveControl)
                PianoRoll.Copy();
            else if (Sequencer.IsActiveControl)
                Sequencer.Copy();
        }

        public void CopyAsText()
        {
            if (PianoRoll.IsActiveControl)
                PianoRoll.CopyAsText();
        }

        public void Cut()
        {
            if (PianoRoll.IsActiveControl)
                PianoRoll.Cut();
            else if (Sequencer.IsActiveControl)
                Sequencer.Cut();
        }

        public void Paste()
        {
            if (PianoRoll.IsActiveControl)
                PianoRoll.Paste();
            else if (Sequencer.IsActiveControl)
                Sequencer.Paste();
        }

        public void PasteSpecial()
        {
            if (PianoRoll.IsActiveControl)
                PianoRoll.PasteSpecial();
            else if (Sequencer.IsActiveControl)
                Sequencer.PasteSpecial();
        }

        public void Delete()
        {
            if (PianoRoll.IsActiveControl)
                PianoRoll.DeleteSelection();
            else if (Sequencer.IsActiveControl)
                Sequencer.DeleteSelection();
        }

        public void DeleteSpecial()
        {
            if (PianoRoll.IsActiveControl)
                PianoRoll.DeleteSpecial();
        }

        public void KeyUp(KeyEventArgs e)
        {
            bool ctrl  = e.Control;
            bool shift = e.Shift;

            if ((recordingMode || qwertyPiano) && !ctrl && !shift && HandleRecordingKey(e, false, e.IsRepeat))
            {
                if (recordingMode)
                    return;
            }
        }

        public void AdvanceRecording()
        {
            PianoRoll.AdvanceRecording(CurrentFrame, true);
        }

        private void Midi_NotePlayed(int n, bool on)
        {
            midiNoteQueue.Enqueue(new Tuple<int, bool>(n, on));
        }

        private void ProcessQueuedMidiNotes()
        {
            while (midiNoteQueue.TryDequeue(out var t))
            {
                var n  = t.Item1;
                var on = t.Item2;

                if (on)
                {
                    PlayInstrumentNote(Utils.Clamp(n - 11, Note.MusicalNoteMin, Note.MusicalNoteMax), true, true);
                    lastMidiNote = n;
                }
                else if (n == lastMidiNote)
                {
                    StopOrReleaseIntrumentNote(false);
                    lastMidiNote = -1;
                }
            }
        }

        public void PlaySong()
        {
            StopRecording();

            if (songPlayer != null && !songPlayer.IsPlaying)
            {
                lastPlayPosition = songPlayer.PlayPosition;
                instrumentPlayer.ConnectOscilloscope(null);
                songPlayer.ConnectOscilloscope(oscilloscope);
                songPlayer.Start(song);
                Platform.ForceScreenOn(true);
            }
        }

        public void PlaySongFromBeginning()
        {
            if (IsPlaying)
                StopSong();
            SeekSong(0);
            PlaySong();
        }

        public void PlaySongFromStartOfPattern()
        {
            if (IsPlaying)
                StopSong();
            SeekSong(song.GetPatternStartAbsoluteNoteIndex(song.PatternIndexFromAbsoluteNoteIndex(songPlayer.PlayPosition)));
            PlaySong();
        }

        public void PlaySongFromLoopPoint()
        {
            if (IsPlaying)
                StopSong();
            SeekSong(song.LoopPoint >= 0 && song.LoopPoint < song.Length ? song.GetPatternStartAbsoluteNoteIndex(song.LoopPoint) : 0);
            PlaySong();
        }

        public void TogglePlaySong()
        {
            if (IsPlaying)
                StopSong();
            else
                PlaySong();
        }

        public void TogglePlaySongFromBeginning()
        {
            if (IsPlaying)
                StopSong();
            else
                PlaySongFromBeginning();
        }

        public void TogglePlaySongFromStartOfPattern()
        {
            if (IsPlaying)
                StopSong();
            else
                PlaySongFromStartOfPattern();
        }

        public void TogglePlaySongFromLoopPoint()
        {
            if (IsPlaying)
                StopSong();
            else
                PlaySongFromLoopPoint();
        }

        public void StopSong()
        {
            if (songPlayer != null && songPlayer.IsPlaying)
            {
                songPlayer.Stop();
                instrumentPlayer.ConnectOscilloscope(oscilloscope);
                songPlayer.ConnectOscilloscope(null);

                if (Settings.RewindAfterPlay)
                {
                    SeekSong(lastPlayPosition);
                }
                else
                {
                    // HACK: Update continuous follow mode only last time so it catches up to the 
                    // real final player position.
                    lastTickCurrentFrame = songPlayer.PlayPosition;
                    Sequencer.UpdateFollowMode(true);
                    PianoRoll.UpdateFollowMode(true);
                    lastTickCurrentFrame = -1;
                }

                Platform.ForceScreenOn(false);
                MarkEverythingDirty();
            }
        }

        public void StartRecording()
        {
            Debug.Assert(!recordingMode);
            StopSong();
            recordingMode = true;
            qwertyPiano = Platform.IsDesktop;
            MobilePianoVisible = Platform.IsMobile;
            MarkEverythingDirty();
        }

        public void StopRecording()
        {
            if (recordingMode)
            {
                recordingMode = false;
                lastRecordingKeyDown = -1;
                StopInstrument();
                MarkEverythingDirty();
            }
        }

        public void ToggleRecording()
        {
            if (recordingMode)
                StopRecording();
            else
                StartRecording();
        }

        public void ToggleQwertyPiano()
        {
            if (!recordingMode)
            {
                qwertyPiano = !qwertyPiano;
                ToolBar.MarkDirty();
                PianoRoll.MarkDirty();
            }
        }

        public void ToggleMetronome()
        {
            metronome = !metronome;
            if (songPlayer != null)
                songPlayer.SetMetronomeSound(metronome ? metronomeSound : null);
        }

        public void SeekSong(int frame)
        {
            if (songPlayer != null)
            {
                bool wasPlaying = songPlayer.IsPlaying;
                if (wasPlaying) StopSong();
                songPlayer.PlayPosition = Utils.Clamp(frame, 0, song.GetPatternStartAbsoluteNoteIndex(song.Length) - 1);
                if (wasPlaying) PlaySong();
                MarkEverythingDirty();
            }
        }

        public void SeekCurrentPattern()
        {
            if (songPlayer != null)
            {
                bool wasPlaying = songPlayer.IsPlaying;
                if (wasPlaying) StopSong();
                songPlayer.PlayPosition = song.GetPatternStartAbsoluteNoteIndex(song.PatternIndexFromAbsoluteNoteIndex(songPlayer.PlayPosition));
                if (wasPlaying) PlaySong();
                MarkEverythingDirty();
            }
        }

        public long ForceDisplayChannelMask
        {
            get { return forceDisplayChannelMask; }
            set
            {
                forceDisplayChannelMask = value;
                Sequencer.MarkDirty();
                PianoRoll.MarkDirty();
            }
        }

        public int GetEnvelopeFrame(Instrument inst, Arpeggio arp, int envelopeIdx, bool arpMode = false)
        {
            bool match = arpMode && (selectedArpeggio == arp) || !arpMode && (selectedInstrument == inst);

            if (instrumentPlayer != null && match)
                return instrumentPlayer.GetEnvelopeFrame(envelopeIdx);
            else
                return -1;
        }

        public bool GetPianoRollViewRange(out int minNoteIdx, out int maxNoteIdx, out int channelIndex)
        {
            minNoteIdx   = -1000000;
            maxNoteIdx   = -1000000;
            channelIndex = -1000000;

            return PianoRoll.GetViewRange(ref minNoteIdx, ref maxNoteIdx, ref channelIndex);
        }

        private void MarkEverythingDirty()
        {
            ToolBar.MarkDirty();
            Sequencer.MarkDirty();
            PianoRoll.MarkDirty();
            ProjectExplorer.MarkDirty();
        }

        private void InitializeAudioPlayers()
        {
            if (!suspended)
            {
                // Initialize the instrument player first. On Android, this
                // has a higher change of getting the low-latency flag accepted.
                InitializeInstrumentPlayer();
                InitializeSongPlayer();
                InitializeOscilloscope();
            }
        }

        private void ShutdownAudioPlayers(bool shutdownStream = false)
        {
            ShutdownSongPlayer(shutdownStream);
            ShutdownInstrumentPlayer(shutdownStream);
            ShutdownOscilloscope();
        }

        private void RecreateAudioPlayers(bool recreateStream = false)
        {
            ShutdownAudioPlayers(recreateStream);
            InitializeMetronome();
            InitializeAudioPlayers();
        }

        private void ConditionalShowTutorial()
        {
            // Edge case where we open a NSF from the command line and the open dialog is active.
            if (window.IsAsyncDialogInProgress)
                return;

            if (tutorialCounter > 0)
            {
                if (--tutorialCounter == 0)
                {
                    if (Settings.ShowTutorial) 
                    {
                        var dlg = new TutorialDialog(window);
                        dlg.ShowDialogAsync((r) =>
                        {
                            if (r == DialogResult.OK)
                            {
                                Settings.ShowTutorial = false;
                                SaveSettings();
                            }
                        });
                    }
                }
            }
        }

        private bool AppNeedsRealTimeUpdate()
        {
            return songPlayer       != null && (songPlayer.IsPlaying || songPlayer.IsSeeking) || 
                   instrumentPlayer != null && (instrumentPlayer.IsPlayingAnyNotes) || 
                   PianoRoll.IsEditingInstrument || 
                   PianoRoll.IsEditingArpeggio   || 
                   PianoRoll.IsEditingDPCMSample;
        }

        private void ConditionalReconnectOscilloscope()
        {
            // This can happen when a song with no loop point naturally ends. We don't get notified of 
            // this (we probably should) and we end up not reconnecting the oscilloscope.
            if (songPlayer != null &&
                !songPlayer.IsPlaying &&
                songPlayer.IsOscilloscopeConnected &&
                instrumentPlayer != null &&
                !instrumentPlayer.IsOscilloscopeConnected)
            {
                songPlayer.ConnectOscilloscope(null);
                instrumentPlayer.ConnectOscilloscope(oscilloscope);

                if (Settings.RewindAfterPlay)
                    SeekSong(lastPlayPosition);
            }
        }

        private void ConditionalEndAccurateSeek()
        {
            if (songPlayer != null && songPlayer.IsSeeking)
            {
                songPlayer.StartIfSeekComplete();
            }
        }

        private void ProcessAudioDeviceChanges()
        {
            if (audioDeviceChanged)
            {
                RecreateAudioPlayers(true);
                DisplayNotification(AudioDeviceChanged);
                audioDeviceChanged = false;
            }
        }

        private void ConditionalMarkControlsDirty()
        {
            if (AppNeedsRealTimeUpdate())
            {
                MarkEverythingDirty();
            }
            else if (oscilloscope != null && ToolBar.ShouldRefreshOscilloscope(oscilloscope.HasNonZeroSample))
            {
                ToolBar.MarkDirty();
            }
        }

        private void HighlightPlayingInstrumentNote()
        {
            if (instrumentPlayer != null)
            {
                PianoRoll.HighlightPianoNote(instrumentPlayer.PlayingNote);
                MobilePiano.HighlightPianoNote(instrumentPlayer.PlayingNote);
            }
        }

        private void CheckStopInstrumentNote(float deltaTime)
        {
            if (stopInstrumentTimer > 0.0f)
            {
                stopInstrumentTimer = Math.Max(0.0f, stopInstrumentTimer - deltaTime);
                if (stopInstrumentTimer == 0.0f)
                {
                    StopOrReleaseIntrumentNote();
                }
            }
        }

        public void Tick(float deltaTime)
        {
            Debug.Assert(!window.IsAsyncDialogInProgress);

            lastTickCurrentFrame = IsPlaying ? songPlayer.PlayPosition : -1;
            averageTickRateMs = Utils.Lerp(averageTickRateMs, deltaTime * 1000.0f, 0.01f);

            ProcessAudioDeviceChanges();
            ProcessQueuedMidiNotes();
            ConditionalMarkControlsDirty();
            ConditionalShowTutorial();
            ConditionalReconnectOscilloscope();
            ConditionalEndAccurateSeek();
            CheckNewReleaseDone();
            HighlightPlayingInstrumentNote();
            CheckStopInstrumentNote(deltaTime);
        }

        private void Sequencer_PatternClicked(int channelIdx, int patternIdx, bool setActive)
        {
            selectedChannelIndex = channelIdx;
            PianoRoll.StartEditChannel(channelIdx, patternIdx);
            if (setActive)
                SetActiveControl(PianoRoll);
        }

        private void PianoRoll_PatternChanged(Pattern pattern)
        {
            Sequencer.NotifyPatternChange(pattern);
            Sequencer.MarkDirty();
        }

        private void PianoRoll_ManyPatternChanged()
        {
            Sequencer.InvalidatePatternCache();
        }

        private void PianoRoll_DPCMSampleChanged()
        {
            ProjectExplorer.MarkDirty();
        }

        private void ProjectExplorer_ArpeggioEdited(Arpeggio arpeggio)
        {
            PianoRoll.StartEditArpeggio(arpeggio);
        }

        private void ProjectExplorer_DPCMSampleReloaded(DPCMSample sample)
        {
            if (PianoRoll.IsEditingDPCMSample && PianoRoll.EditSample == sample)
            {
                PianoRoll.StartEditDPCMSample(sample);
            }
        }

        private void PianoRoll_EnvelopeChanged(Instrument instrument, int env)
        {
            ProjectExplorer.InstrumentEnvelopeChanged(instrument, env);
        }

        private void SerializeActiveControl(ProjectBuffer buffer)
        {
            if (buffer.IsWriting)
            {
                int controlIdx = 0;

                if (ActiveControl == PianoRoll) 
                    controlIdx = 1;
                else if (ActiveControl == ProjectExplorer) 
                    controlIdx = 2;

                buffer.Serialize(ref controlIdx);
            }
            else
            {
                int controlIdx = 0;
                buffer.Serialize(ref controlIdx);

                switch (controlIdx)
                {
                    case 0: window.SetActiveControl(Sequencer, false); break;
                    case 1: window.SetActiveControl(PianoRoll, false); break;
                    case 2: window.SetActiveControl(ProjectExplorer, false); break;
                }
            }
        }

        public void Serialize(ProjectBuffer buffer)
        {
            var oldSong = song;
            var currentFrame = CurrentFrame;

            buffer.Serialize(ref selectedChannelIndex);
            buffer.Serialize(ref selectedInstrument);
            buffer.Serialize(ref selectedArpeggio);
            buffer.Serialize(ref forceDisplayChannelMask);
            buffer.Serialize(ref song);
            buffer.Serialize(ref currentFrame);

            ProjectExplorer.Serialize(buffer);
            Sequencer.Serialize(buffer);
            PianoRoll.Serialize(buffer);

            SerializeActiveControl(buffer);

            if (buffer.IsReading)
            {
                // Move seek bar on undo/redo when recording.
                if (recordingMode)
                    SeekSong(currentFrame);

                RefreshLayout();
                window.MarkDirty();

                // When the song changes between undo/redos, must stop the audio.
                if (oldSong.Id != song.Id)
                {
                    StopEverything();
                    ResetSelectedSong();
                }
            }
        }

        private void ProjectExplorer_InstrumentColorChanged(Instrument instrument)
        {
            Sequencer.InvalidatePatternCache();
            PianoRoll.MarkDirty();
        }

        private void ProjectExplorer_ArpeggioColorChanged(Arpeggio arpeggio)
        {
            PianoRoll.MarkDirty();
        }

        private void ProjectExplorer_DPCMSampleColorChanged(DPCMSample sample)
        {
            Sequencer.InvalidatePatternCache();
            PianoRoll.MarkDirty();
        }

        private void ProjectExplorer_SongModified(Song song)
        {
            Sequencer.SongModified();
            PianoRoll.SongModified();
            MarkEverythingDirty();
        }

        private void ProjectExplorer_InstrumentReplaced(Instrument instrument)
        {
            Sequencer.InvalidatePatternCache();
            MarkEverythingDirty();
        }
    }

    public enum ContextMenuCheckState
    {
        None,
        Unchecked,
        Checked,
        Radio
    }

    [Flags]
    public enum ContextMenuSeparator
    {
        None,
        Before = 1,
        After  = 2,

        // Last minute hack, we typically don't have separators for context menus on mobile,
        // but if we want them, we can use these special values.
        MobileFlag = 0x80,

        MobileBefore = MobileFlag | Before,
        MobileAfter  = MobileFlag | After
    }

    // Move these to a common class
    public class ContextMenuOption
    {
        public string Image;
        public string Text;
        public string ToolTip;
        public Func<ContextMenuCheckState> CheckState;
        public Action Callback;
        public ContextMenuSeparator Separator;

        public ContextMenuOption(string img, string text, Action callback, ContextMenuSeparator separator = ContextMenuSeparator.None)
        {
            Image = img;
            Text = text;
            Callback = callback;
            Separator = separator;
            CheckState = () => ContextMenuCheckState.None;
        }

        public ContextMenuOption(string img, string text, string tooltip, Action callback, ContextMenuSeparator separator = ContextMenuSeparator.None)
        {
            Image = img;
            ToolTip = tooltip;
            Text = text;
            Callback = callback;
            Separator = separator;
            CheckState = () => ContextMenuCheckState.None;
        }

        public ContextMenuOption(string text, string tooltip, Action callback, Func<ContextMenuCheckState> checkState, ContextMenuSeparator separator = ContextMenuSeparator.None)
        {
            ToolTip = tooltip;
            Text = text;
            Callback = callback;
            Separator = separator;
            CheckState = checkState;
        }
    }
}

