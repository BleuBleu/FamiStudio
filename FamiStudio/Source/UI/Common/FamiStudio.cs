#define DEVELOPMENT_VERSION

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

#if !FAMISTUDIO_ANDROID
using System.Web.Script.Serialization;
#endif

using RenderControl = FamiStudio.GLControl;
using RenderTheme   = FamiStudio.ThemeRenderResources;

namespace FamiStudio
{
    public class FamiStudio
    {
        private const int MaxAutosaves = 30;

        private FamiStudioForm mainForm;
        private Project project;
        private Song song;
        private Instrument selectedInstrument = null; // null = DPCM
        private Arpeggio selectedArpeggio = null;
        private SongPlayer songPlayer;
        private InstrumentPlayer instrumentPlayer;
        private Oscilloscope oscilloscope;
        private UndoRedoManager undoRedoManager;
        private ExportDialog exportDialog;
        private LogDialog logDialog;
        private LogProgressDialog progressLogDialog;

        private int selectedChannelIndex;
        private int ghostChannelMask = 0;
        private int lastMidiNote = -1;
        private int tutorialCounter = 3;
        private int baseRecordingOctave = 3;
        private int lastTickCurrentFrame = -1;
        private int previewDPCMSampleId = -1;
        private int previewDPCMSampleRate = 44100;
        private int lastRecordingKeyDown = -1;
        private bool previewDPCMIsSource = false;
        private bool metronome = false;
        private bool palPlayback = false;
        private bool audioDeviceChanged = false;
        private bool pianoRollScrollChanged = false;
        private bool recordingMode = false;
        private bool qwertyPiano = false;
        private bool followMode = false;
        private bool suspended = false;
        private float stopInstrumentTimer = 0.0f;
        private short[] metronomeSound;
        private BitArray keyStates = new BitArray(65536);
        private ConcurrentQueue<Tuple<int, bool>> midiNoteQueue = new ConcurrentQueue<Tuple<int, bool>>();
#if FAMISTUDIO_WINDOWS
        private MultiMediaNotificationListener mmNoticiations;
#endif
        private int autoSaveIndex = 0;
        private float averageTickRateMs = 8.0f;
        private DateTime lastTickTime = DateTime.Now;
        private DateTime lastAutoSave;

        private bool   newReleaseAvailable = false;
        private string newReleaseString = null;
        private string newReleaseUrl = null;

        public bool  IsPlaying => songPlayer != null && songPlayer.IsPlaying;
        public bool  IsRecording => recordingMode;
        public bool  IsQwertyPianoEnabled => qwertyPiano;
        public bool  IsMetronomeEnabled => metronome;
        public bool  IsSuspended => suspended;
        public bool  FollowModeEnabled { get => followMode; set => followMode = value; }
        public bool  SequencerHasSelection => Sequencer.GetPatternTimeSelectionRange(out _, out _);
        public int   BaseRecordingOctave => baseRecordingOctave;
        public int   CurrentFrame => lastTickCurrentFrame >= 0 ? lastTickCurrentFrame : (songPlayer != null ? songPlayer.PlayPosition : 0);
        public int   ChannelMask { get => songPlayer != null ? songPlayer.ChannelMask : -1; set => songPlayer.ChannelMask = value; }
        public int   PlayRate { get => songPlayer != null ? songPlayer.PlayRate : 1; set { if (!IsPlaying) songPlayer.PlayRate = value; } }
        public float AverageTickRate => averageTickRateMs;

        public bool SnapEnabled    { get => PianoRoll.SnapEnabled;    set => PianoRoll.SnapEnabled    = value; }
        public int  SnapResolution { get => PianoRoll.SnapResolution; set => PianoRoll.SnapResolution = value; }

        public UndoRedoManager UndoRedoManager => undoRedoManager; 
        public DPCMSample      DraggedSample   => ProjectExplorer.DraggedSample;
        public Project         Project         => project;
        public Channel         SelectedChannel => song.Channels[SelectedChannelIndex];
        public Toolbar         ToolBar         => mainForm.ToolBar;
        public Sequencer       Sequencer       => mainForm.Sequencer;
        public PianoRoll       PianoRoll       => mainForm.PianoRoll;
        public ProjectExplorer ProjectExplorer => mainForm.ProjectExplorer;
        public QuickAccessBar  QuickAccessBar  => mainForm.QuickAccessBar;
        public RenderControl   ActiveControl   => mainForm.ActiveControl;
        public FamiStudioForm  MainForm        => mainForm;

        private string WipProject  => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "WIP.fms");
        private string WipSettings => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "WIP.ini");

        public int  PreviewDPCMWavPosition => instrumentPlayer != null ? instrumentPlayer.RawPcmSamplePlayPosition : 0;
        public int  PreviewDPCMSampleId    => previewDPCMSampleId;
        public int  PreviewDPCMSampleRate  => previewDPCMSampleRate;
        public bool PreviewDPCMIsSource    => previewDPCMIsSource;

        public static Project    StaticProject  { get; set; }
        public static FamiStudio StaticInstance { get; private set; }

        public void Initialize(string filename)
        {
            StaticInstance = this;

            SetMainForm(PlatformUtils.IsMobile ? FamiStudioForm.Instance : new FamiStudioForm(this));
            InitializeMetronome();
            InitializeSongPlayer();
            InitializeMidi();
            InitializeMultiMediaNotifications();

            if (string.IsNullOrEmpty(filename) && PlatformUtils.IsDesktop && Settings.OpenLastProjectOnStart && !string.IsNullOrEmpty(Settings.LastProjectFile) && File.Exists(Settings.LastProjectFile))
                filename = Settings.LastProjectFile;

            if (string.IsNullOrEmpty(filename) && PlatformUtils.IsMobile && File.Exists(WipProject))
                filename = WipProject;

            if (!string.IsNullOrEmpty(filename))
                OpenProjectInternal(filename);
            else
                NewProject(true);

#if !DEBUG
            if (Settings.CheckUpdates)
                Task.Factory.StartNew(CheckForNewRelease);
#endif
        }

        public void SetMainForm(FamiStudioForm form)
        {
            mainForm = form;

            SetActiveControl(PianoRoll);

            Sequencer.PatternClicked     += Sequencer_PatternClicked;
            Sequencer.PatternModified    += Sequencer_PatternModified;
            Sequencer.SelectionChanged   += Sequencer_SelectionChanged;
            Sequencer.PatternsPasted     += PianoRoll_NotesPasted;

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

            ProjectExplorer.InstrumentEdited         += ProjectExplorer_InstrumentEdited;
            ProjectExplorer.InstrumentColorChanged   += ProjectExplorer_InstrumentColorChanged;
            ProjectExplorer.InstrumentReplaced       += ProjectExplorer_InstrumentReplaced;
            ProjectExplorer.InstrumentDeleted        += ProjectExplorer_InstrumentDeleted;
            ProjectExplorer.InstrumentDroppedOutside += ProjectExplorer_InstrumentDroppedOutside;
            ProjectExplorer.SongModified             += ProjectExplorer_SongModified;
            ProjectExplorer.ProjectModified          += ProjectExplorer_ProjectModified;
            ProjectExplorer.ArpeggioEdited           += ProjectExplorer_ArpeggioEdited;
            ProjectExplorer.ArpeggioColorChanged     += ProjectExplorer_ArpeggioColorChanged;
            ProjectExplorer.ArpeggioDeleted          += ProjectExplorer_ArpeggioDeleted;
            ProjectExplorer.ArpeggioDroppedOutside   += ProjectExplorer_ArpeggioDroppedOutside;
            ProjectExplorer.DPCMSampleReloaded       += ProjectExplorer_DPCMSampleReloaded;
            ProjectExplorer.DPCMSampleEdited         += ProjectExplorer_DPCMSampleEdited;
            ProjectExplorer.DPCMSampleColorChanged   += ProjectExplorer_DPCMSampleColorChanged;
            ProjectExplorer.DPCMSampleDeleted        += ProjectExplorer_DPCMSampleDeleted;
            ProjectExplorer.DPCMSampleDraggedOutside += ProjectExplorer_DPCMSampleDraggedOutside;
            ProjectExplorer.DPCMSampleMapped         += ProjectExplorer_DPCMSampleMapped;
            ProjectExplorer.InstrumentsHovered       += ProjectExplorer_InstrumentsHovered;
        }

        public LoopMode LoopMode 
        { 
            get => songPlayer != null ? songPlayer.Loop : LoopMode.LoopPoint; 
            set => songPlayer.Loop = value; 
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
                    PianoRoll.SongChanged();
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
                    Sequencer.MarkDirty();
                    PianoRoll.ChangeChannel(selectedChannelIndex);
                }
            }
        }

        public Instrument SelectedInstrument
        {
            get { return selectedInstrument; }
            set
            {
                selectedInstrument = value;
                ProjectExplorer.MarkDirty();
                // MATTT
            }
        }

        public Arpeggio SelectedArpeggio
        {
            get { return selectedArpeggio; }
            set
            {
                selectedArpeggio = value;
                ProjectExplorer.MarkDirty();
                // MATTT
            }
        }

        public bool IsChannelActive(int idx)
        {
            return songPlayer != null && (songPlayer.ChannelMask & (1 << idx)) != 0;
        }

        public void ToggleChannelActive(int idx)
        {
            if (songPlayer != null)
                songPlayer.ChannelMask ^= (1 << idx);
        }

        public void ToggleChannelSolo(int idx)
        {
            if (songPlayer != null)
            {
                var bit = 1 << idx;
                songPlayer.ChannelMask = songPlayer.ChannelMask == bit ? -1 : bit;
            }
        }

        public void SoloChannel(int idx)
        {
            if (songPlayer != null)
                songPlayer.ChannelMask = 1 << idx;
        }
        
        public bool IsChannelSolo(int idx)
        {
            return songPlayer != null && songPlayer.ChannelMask == (1 << idx);
        }

        public void ToggleChannelGhostNotes(int idx)
        {
            GhostChannelMask ^= (1 << idx);
        }

        public void SetActiveControl(RenderControl ctrl)
        {
            Debug.Assert(ctrl == PianoRoll || ctrl == Sequencer || ctrl == ProjectExplorer);
            mainForm.SetActiveControl(ctrl);
        }

        private void ProjectExplorer_InstrumentsHovered(bool showExpansions)
        {
            Sequencer.ShowExpansionIcons = showExpansions;
        }

        private void PianoRoll_MaximizedChanged()
        {
            RefreshLayout();
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

        public void SetToolTip(string msg, bool red = false)
        {
            ToolBar.SetToolTip(msg, red);
        }

        public void BeginLogTask(bool progress = false)
        {
            Debug.Assert(logDialog == null && progressLogDialog == null);

            if (progress)
            {
                progressLogDialog = new LogProgressDialog(mainForm);
                Log.SetLogOutput(progressLogDialog);
            }
            else if (PlatformUtils.IsDesktop)
            {
                logDialog = new LogDialog(mainForm);
                Log.SetLogOutput(logDialog);
            }
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

                if (PlatformUtils.IsDesktop)
                    progressLogDialog.StayModalUntilClosed();
                else
                    progressLogDialog.Close();

                progressLogDialog = null;
            }
            else if (PlatformUtils.IsDesktop)
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
            ProjectExplorer.MarkDirty();
        }

        public int GetDPCMSampleMappingNoteAtPos(Point pos)
        {
            return PianoRoll.GetDPCMSampleMappingNoteAtPos(PianoRoll.PointToClient(pos));
        }

        private void ProjectExplorer_DPCMSampleMapped(DPCMSample instrument, Point pos)
        {
            Sequencer.InvalidatePatternCache();
            PianoRoll.MarkDirty();
        }

        private void ProjectExplorer_DPCMSampleDraggedOutside(DPCMSample instrument, Point pos)
        {
            if (PianoRoll.ClientRectangle.Contains(PianoRoll.PointToClient(pos)))
                PianoRoll.MarkDirty();
        }

        private void PianoRoll_NoteEyedropped(Note note)
        {
            if (note != null)
            {
                selectedInstrument = note.Instrument;
                selectedArpeggio   = note.Arpeggio;
            }
        }

        private void PianoRoll_ScrollChanged()
        {
            if (Settings.ShowPianoRollViewRange && !PianoRoll.IsMaximized)
            {
#if FAMISTUDIO_WINDOWS
                pianoRollScrollChanged = true;
#else
                Sequencer.MarkDirty();
#endif
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
            RefreshLayout();
            ResetSelectedChannel();
            ResetSelectedInstrumentArpeggio();
            ResetSelectedInstrumentArpeggio();
            Sequencer.Reset();
            PianoRoll.Reset();
        }

        private void ProjectExplorer_InstrumentDeleted(Instrument instrument)
        {
            ResetSelectedInstrumentArpeggio();
            PianoRoll.Reset();
        }

        private void ProjectExplorer_ArpeggioDeleted(Arpeggio arpeggio)
        {
            ResetSelectedInstrumentArpeggio();
            PianoRoll.Reset();
        }

        private void ProjectExplorer_DPCMSampleDeleted(DPCMSample sample)
        {
            PianoRoll.Reset();
        }

        private void PianoRoll_NotesPasted()
        {
            ProjectExplorer.RefreshButtons();
            ProjectExplorer.MarkDirty();
        }

        private void ProjectExplorer_InstrumentDroppedOutside(Instrument instrument, Point pos)
        {
            var pianoRollPos = PianoRoll.PointToClient(pos);
            if (PianoRoll.ClientRectangle.Contains(pianoRollPos))
            {
                PianoRoll.ReplaceSelectionInstrument(instrument, pianoRollPos);
                PianoRoll.Focus();
            }
        }

        private void ProjectExplorer_ArpeggioDroppedOutside(Arpeggio arpeggio, Point pos)
        {
            var pianoRollPos = PianoRoll.PointToClient(pos);
            if (PianoRoll.ClientRectangle.Contains(pianoRollPos))
            {
                PianoRoll.ReplaceSelectionArpeggio(arpeggio, pianoRollPos);
                PianoRoll.Focus();
            }
        }

        private void Sequencer_PatternModified()
        {
            PianoRoll.MarkDirty();
        }

        public void Run()
        {
            mainForm.Run();
        }

        public void ShowContextMenu(ContextMenuOption[] options)
        {
            mainForm.ShowContextMenu(options);
        }

        private void InitializeMultiMediaNotifications()
        {
#if FAMISTUDIO_WINDOWS
            // Windows 7 falls back to XAudio 2.7 which does not have 
            // a virtual audio end point, which mean we need to detect 
            // device changes a lot more manually.
            if (Environment.OSVersion.Version.Major == 6 &&
                Environment.OSVersion.Version.Minor <= 1)
            {
                mmNoticiations = new MultiMediaNotificationListener();
                mmNoticiations.DefaultDeviceChanged += MmNoticiations_DefaultDeviceChanged;
            }
#endif
        }

        private void InitializeMetronome()
        {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("FamiStudio.Resources.Metronome.wav"))
            {
                using (var reader = new BinaryReader(stream))
                {
                    // Pad the first part with a bunch of zero samples.
                    metronomeSound = new short[reader.BaseStream.Length / 2];

                    var i = 0;
                    var volume = Settings.MetronomeVolume / 100.0f;

                    while (reader.BaseStream.Position != reader.BaseStream.Length)
                    {
                        var sample = reader.ReadInt16();
                        metronomeSound[i++] = (short)Utils.Clamp((int)(sample * volume), short.MinValue, short.MaxValue);
                    }
                }
            }
        }

        private void MmNoticiations_DefaultDeviceChanged()
        {
            audioDeviceChanged = true;
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

        public void DisplayWarning(string msg, bool beep = true)
        {
            ToolBar.DisplayWarning(msg, beep);
        }

        private void UndoRedoManager_PreUndoRedo(TransactionScope scope, TransactionFlags flags)
        {
            Debug.Assert(!mainForm.IsAsyncDialogInProgress);

            ValidateIntegrity();

            // Special category for stuff that is so important, we should stop the song.
            if (flags.HasFlag(TransactionFlags.StopAudio))
            {
                StopEverything();
                SeekSong(0);
            }

            if (flags.HasFlag(TransactionFlags.ReinitializeAudio))
            {
                // This is overly careful, the only case where this might be needed
                // is when PAL mode changes, or when the number of buffered frames are 
                // changed in the settings.
                ShutdownInstrumentPlayer();
                ShutdownSongPlayer();
                ShutdownOscilloscope();
            }
        }

        private void UndoRedoManager_PostUndoRedo(TransactionScope scope, TransactionFlags flags)
        {
            Debug.Assert(!mainForm.IsAsyncDialogInProgress);

            ValidateIntegrity();

            if (flags.HasFlag(TransactionFlags.ReinitializeAudio))
            {
                palPlayback = project.PalMode;
                InitializeInstrumentPlayer();
                InitializeSongPlayer();
                InitializeOscilloscope();
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
#endif
        }

        public void TrySaveProjectAsync(Action callback)
        {
            if (undoRedoManager != null && undoRedoManager.NeedsSaving)
            {
                PlatformUtils.MessageBoxAsync("Save changes?", "FamiStudio", MessageBoxButtons.YesNoCancel, (r) =>
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

        private void UnloadProject()
        {
            if (undoRedoManager != null)
            {
                FreeExportDialog();

                undoRedoManager.PreUndoRedo -= UndoRedoManager_PreUndoRedo;
                undoRedoManager.PostUndoRedo -= UndoRedoManager_PostUndoRedo;
                undoRedoManager.TransactionBegan -= UndoRedoManager_PreUndoRedo;
                undoRedoManager.TransactionEnded -= UndoRedoManager_PostUndoRedo;
                undoRedoManager.Updated -= UndoRedoManager_Updated;
                undoRedoManager = null;
                project = null;

                StopEverything();
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
                exportDialog = null;
            }
        }

        private void InitProject()
        {
            StopRecording();
            ShutdownSongPlayer();
            ShutdownInstrumentPlayer();
            ShutdownOscilloscope();

            StaticProject = project;
            song = project.Songs[0];
            palPlayback = project.PalMode;

            ResetSelectedChannel();
            ResetSelectedInstrumentArpeggio();
            ResetSelectedSong();

            ToolBar.Reset();
            ProjectExplorer.Reset();
            PianoRoll.Reset();
            Sequencer.Reset();

            InitializeAutoSave();
            InitializeSongPlayer();
            InitializeInstrumentPlayer();
            InitializeOscilloscope();

            FreeExportDialog();

            undoRedoManager = new UndoRedoManager(project, this);
            undoRedoManager.PreUndoRedo += UndoRedoManager_PreUndoRedo;
            undoRedoManager.PostUndoRedo += UndoRedoManager_PostUndoRedo;
            undoRedoManager.TransactionBegan += UndoRedoManager_PreUndoRedo;
            undoRedoManager.TransactionEnded += UndoRedoManager_PostUndoRedo;
            undoRedoManager.Updated += UndoRedoManager_Updated;

            FixMobileProjectFilename();
            SaveLastOpenProjectFile();
            SaveWorkInProgress();
            MarkEverythingDirty();
            UpdateTitle();
            RefreshLayout();
        }

        private void FixMobileProjectFilename()
        {
            if (PlatformUtils.IsMobile)
            {
                if (!string.IsNullOrEmpty(project.Filename))
                {
                    if (project.Filename.ToLower() == WipProject.ToLower())
                    {
                        LoadWipSettings();
                    }
                    else if (!project.Filename.ToLower().StartsWith(PlatformUtils.UserProjectsDirectory.ToLower()))
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
            {
                project = OpenProjectFile(filename);

                if (project != null)
                {
                    InitProject();
                }
                else
                {
                    NewProject();
                }

                mainForm.Refresh();
            }
            EndLogTask();
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
                if (PlatformUtils.IsDesktop)
                {
                    if (filename == null)
                        filename = PlatformUtils.ShowOpenFileDialog("Open File", "All Supported Files (*.fms;*.txt;*.nsf;*.nsfe;*.ftm;*.mid)|*.fms;*.txt;*.nsf;*.nsfe;*.ftm;*.mid|FamiStudio Files (*.fms)|*.fms|FamiTracker Files (*.ftm)|*.ftm|FamiTracker Text Export (*.txt)|*.txt|FamiStudio Text Export (*.txt)|*.txt|NES Sound Format (*.nsf;*.nsfe)|*.nsf;*.nsfe|MIDI files (*.mid)|*.mid", ref Settings.LastFileFolder);

                    if (filename != null)
                        UnloadAndOpenAction(filename);
                }
                else
                {
                    var dlg = new MobileProjectDialog(this, "Open FamiStudio Project", false);
                    dlg.ShowDialogAsync((f) =>
                    {
                        // HACK : We don't support nested activities right now, so return
                        // this special code to signal that we should open from storage.
                        if (f == "///STORAGE///")
                            PlatformUtils.StartMobileLoadFileOperationAsync("*/*", (fs) => { UnloadAndOpenAction(fs); });
                        else
                            UnloadAndOpenAction(f);
                    });
                }
            });
        }

        public Project OpenProjectFile(string filename, bool allowComplexFormats = true)
        {
            var extension = Path.GetExtension(filename.ToLower());

            var fms = extension == ".fms";
            var ftm = extension == ".ftm";
            var txt = extension == ".txt";
            var nsf = extension == ".nsf" || extension == ".nsfe";
            var mid = extension == ".mid";

            var requiresDialog = PlatformUtils.IsDesktop && allowComplexFormats && (nsf || mid);

            var project = (Project)null;

            if (requiresDialog)
            {
                if (mid)
                {
                    var dlg = new MidiImportDialog(filename);
                    project = dlg.ShowDialog(mainForm);
                }
                else if (nsf)
                {
                    var dlg = new NsfImportDialog(filename);
                    project = dlg.ShowDialog(mainForm);
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
                }
                else if (txt)
                {
                    if (FamistudioTextFile.LooksLikeFamiStudioText(filename))
                        project = new FamistudioTextFile().Load(filename);
                    else
                        project = new FamitrackerTextFile().Load(filename);
                }
            }

            return project;
        }

        public void SaveProjectAsync(bool forceSaveAs = false, Action callback = null)
        {
            var filename = project.Filename;

            if (forceSaveAs || string.IsNullOrEmpty(filename))
            {
                if (PlatformUtils.IsDesktop)
                {
                    filename = PlatformUtils.ShowSaveFileDialog("Save File", "FamiStudio Files (*.fms)|*.fms", ref Settings.LastFileFolder);
                    if (filename != null)
                    {
                        SaveProjectInternal(filename);
                        callback?.Invoke();
                    }
                }
                else
                {
                    var dlg = new MobileProjectDialog(this, "Save FamiStudio Project", true);
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

                if (Settings.ClearUndoRedoOnSave)
                    undoRedoManager.Clear();

                undoRedoManager.NotifySaved();
                PlatformUtils.ShowToast("Project Saved!");
            }
            else
            {
                // DROIDTODO : See if we want to unify this under a single, cross-platform call.
                if (PlatformUtils.IsDesktop)
                    PlatformUtils.MessageBox("An error happened while saving.", "Error", MessageBoxButtons.OK);
                else
                    PlatformUtils.ShowToast("Error Saving Project!");
            }

            MarkEverythingDirty();
            SaveWorkInProgress();
        }

        public void Export()
        {
            FreeExportDialog();

            exportDialog = new ExportDialog(this);
            exportDialog.Exporting += ExportDialog_Exporting;
            exportDialog.ShowDialogAsync();
        }

        public void RepeatLastExport()
        {
            if (exportDialog == null || !exportDialog.HasAnyPreviousExport)
            {
                DisplayWarning("No last export to repeat");
            }
            else if (!exportDialog.CanRepeatLastExport(project))
            {
                DisplayWarning("Project has changed too much to repeat last export.");
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

            // Make sure we arent in real-time mode, on Linux/MacOS, this mean we will 
            // be constantly rendering frames as we export.
            if (AppNeedsRealTimeUpdate())
            {
                PianoRoll.Reset();
                Debug.Assert(!AppNeedsRealTimeUpdate());
            }
        }

        public void Suspend()
        {
            if (!suspended)
            {
                StopEverything();
                ShutdownSongPlayer();
                ShutdownInstrumentPlayer();
                ShutdownOscilloscope();
                SaveWorkInProgress();
                suspended = true;
            }
        }

        public void Resume()
        {
            if (suspended)
            {
                InitializeSongPlayer();
                InitializeInstrumentPlayer();
                InitializeOscilloscope();
                MarkEverythingDirty();
                suspended = false;
            }
        }

        public void OpenConfigDialog()
        {
            var dlg = new ConfigDialog();

            dlg.ShowDialogAsync(mainForm, (r) =>
            {
                if (r == DialogResult.OK)
                {
                    RecreateAudioPlayers();
                    RefreshLayout();
                    InitializeMidi();
                    MarkEverythingDirty();
                }
            });
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
                ShutdownInstrumentPlayer();
                ShutdownSongPlayer();

                close = true;
            });

            return close;
        }

        private void RefreshLayout()
        {
            mainForm.RefreshLayout();
            Sequencer.LayoutChanged();
            PianoRoll.LayoutChanged();
            ToolBar.LayoutChanged();
            ProjectExplorer.LayoutChanged();
        }

        private void CheckForNewRelease()
        {
#if !FAMISTUDIO_ANDROID // DROIDTODO
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
                        var jsonSerializer = new JavaScriptSerializer();

                        dynamic release = jsonSerializer.Deserialize<dynamic>(json);

                        newReleaseString = release["tag_name"].ToString();
                        newReleaseUrl = release["html_url"].ToString();

                        var versionComparison = string.Compare(newReleaseString, PlatformUtils.ApplicationVersion, StringComparison.OrdinalIgnoreCase);
                        var newerVersionAvailable = versionComparison > 0;

#if DEVELOPMENT_VERSION
                        // If we were running a development version (BETA, etc.), but an official version of 
                        // the same number appears on GitHub, prompt for update.
                        if (!newerVersionAvailable && versionComparison == 0)
                        {
                            newerVersionAvailable = true;
                        }
#endif

                        // Assume > alphabetical order means newer version.
                        if (newerVersionAvailable)
                        {
                            // Make sure this release applies to our platform (eg. a hotfix for macos should not impact Windows).
                            var assets = release["assets"];
                            foreach (var asset in assets)
                            {
                                var name = (string)asset["name"];
#if FAMISTUDIO_WINDOWS
                                if (name != null && name.ToLower().Contains("win"))
#elif FAMISTUDIO_LINUX
                                if (name != null && name.ToLower().Contains("linux"))
#else
                                if (name != null && name.ToLower().Contains("macos"))
#endif
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
                    if (PlatformUtils.IsDesktop)
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
            if (PlatformUtils.IsMobile)
            {
                Debug.Assert(PlatformUtils.IsInMainThread());

                SaveProjectCopy(WipProject);
                SaveWipSettings();
            }
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
            if (newReleaseAvailable)
            {
                newReleaseAvailable = false;

                if (PlatformUtils.MessageBox($"A new version ({newReleaseString}) is available. Do you want to download it?", "New Version", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    PlatformUtils.OpenUrl("http://www.famistudio.org");
                }
            }
        }

        public void OpenTransformDialog()
        {
            var dlg = new TransformDialog(this);
            dlg.CleaningUp += TransformDialog_CleaningUp;
            dlg.ShowDialogAsync(mainForm, (r) =>
            {
                if (r == DialogResult.OK)
                {
                    ResetSelectedSong();
                    ResetSelectedInstrumentArpeggio();
                    Sequencer.Reset();
                    PianoRoll.Reset();
                    ProjectExplorer.RefreshButtons();
                    MarkEverythingDirty();
                }
            });
        }

        private void TransformDialog_CleaningUp()
        {
            StopEverything();

            // We might be deleting data like entire songs/envelopes/samples that
            // are being edited right now.
            ResetSelectedSong();
            ResetSelectedInstrumentArpeggio();
            Sequencer.Reset();
            PianoRoll.Reset();
        }

        public void ShowHelp()
        {
            PlatformUtils.OpenUrl("http://www.famistudio.org/doc/index.html");
        }

        private void UpdateTitle()
        {
            string projectFile = "New Project";

            if (!string.IsNullOrEmpty(project.Filename))
                projectFile = System.IO.Path.GetFileName(project.Filename);

            var version = PlatformUtils.ApplicationVersion.Substring(0, PlatformUtils.ApplicationVersion.LastIndexOf('.'));

            string title = $"FamiStudio {version} - {projectFile}";

#if DEVELOPMENT_VERSION
            title += " - DEVELOPMENT VERSION DO NOT DISTRIBUTE!";
#endif

            mainForm.Text = title;
        }

        public void PlayInstrumentNote(int n, bool showWarning, bool allowRecording, bool custom = false, Instrument customInstrument = null, Arpeggio customArpeggio = null, float stopDelay = 0.0f)
        {
            Note note = new Note(n);
            note.Volume = Note.VolumeMax;

            var instrument = custom ? customInstrument : selectedInstrument;
            var arpeggio   = custom ? customArpeggio   : selectedArpeggio;

            int channel = selectedChannelIndex;

            // Non-recorded notes are the ones that are playing when creating/dragging notes.
            // We dont want to assume DPCM channel when getting a null intrument.
            if (instrument == null && allowRecording) 
            {
                channel = ChannelType.Dpcm;
            }
            else
            {
                if (song.Channels[channel].SupportsInstrument(instrument))
                {
                    note.Instrument = instrument;

                    if (song.Channels[channel].SupportsArpeggios && arpeggio != null)
                        note.Arpeggio = arpeggio;
                }
                else
                {
                    if (showWarning)
                        DisplayWarning("Selected instrument is incompatible with channel!", false);
                    return;
                }
            }

            // HACK : These should simply be pass as parameters.
            if (PianoRoll.IsEditingInstrument && PianoRoll.EditInstrument != null && song.Channels[channel].SupportsInstrument(PianoRoll.EditInstrument))
                note.Instrument = PianoRoll.EditInstrument;
            if (PianoRoll.IsEditingArpeggio && song.Channels[channel].SupportsArpeggios)
                note.Arpeggio = PianoRoll.EditArpeggio;

            instrumentPlayer.PlayNote(channel, note);

            if (allowRecording && recordingMode)
                PianoRoll.RecordNote(note);

            stopInstrumentTimer = stopDelay;
        }

        public void StopOrReleaseIntrumentNote(bool allowRecording = false)
        {
            if (selectedInstrument != null &&
                (selectedInstrument.HasReleaseEnvelope || selectedInstrument.IsVrc7Instrument) &&
                song.Channels[selectedChannelIndex].SupportsInstrument(selectedInstrument))
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
            songPlayer = new SongPlayer(palPlayback);
            songPlayer.SetMetronomeSound(metronome ? metronomeSound : null);
            songPlayer.SetSelectionRange(min, max);
        }

        private void InitializeInstrumentPlayer()
        {
            Debug.Assert(instrumentPlayer == null);
            instrumentPlayer = new InstrumentPlayer(palPlayback);
            instrumentPlayer.Start(project, palPlayback);
        }

        private void ShutdownSongPlayer()
        {
            if (songPlayer != null)
            {
                songPlayer.Stop();
                songPlayer.Shutdown();
                songPlayer = null;
            }
        }

        private void ShutdownInstrumentPlayer()
        {
            if (instrumentPlayer != null)
            {
                instrumentPlayer.Stop(true);
                instrumentPlayer.Shutdown();
                instrumentPlayer = null;
                PianoRoll.HighlightPianoNote(Note.NoteInvalid);
            }
        }

        public void InitializeOscilloscope()
        {
            Debug.Assert(oscilloscope == null);

            if (Settings.ShowOscilloscope)
            {
                oscilloscope = new Oscilloscope((int)DpiScaling.MainWindow);
                oscilloscope.Start();

                if (instrumentPlayer != null)
                    instrumentPlayer.ConnectOscilloscope(oscilloscope);
            }
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

        public float[,] GetOscilloscopeGeometry(out bool hHasNonZeroSample)
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
                    instrumentPlayer.PlayRawPcmSample(sample.SourceWavData.Samples, sample.SourceWavData.SampleRate, NesApu.DPCMVolume);
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
                instrumentPlayer.PlayRawPcmSample(wave, playRate, NesApu.DPCMVolume);
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
                {
                    ShutdownSongPlayer();
                    ShutdownInstrumentPlayer();
                    ShutdownOscilloscope();
                }

				palPlayback = value;
				if (project.UsesFamiTrackerTempo)
	                project.PalMode = value;

                if (playersWereValid)
                {
                    InitializeSongPlayer();
                    InitializeInstrumentPlayer();
                    InitializeOscilloscope();
                }

                MarkEverythingDirty();
            }
        }
        
        private bool PreventKeyRepeat(int rawKeyCode, bool keyDown)
        {
            var keyCode = rawKeyCode;

            if (keyCode < keyStates.Length && keyDown != keyStates[keyCode])
            {
                keyStates[keyCode] = keyDown;
                return true;
            }

            return false;
        }

        protected bool HandleRecordingKey(int rawKeyCode, bool keyDown)
        {
            if (Settings.KeyCodeToNoteMap.TryGetValue(rawKeyCode, out var noteValue))
            {
                if (!PreventKeyRepeat(rawKeyCode, keyDown))
                    return true;

                if (keyDown)
                {
                    if (noteValue == 0)
                    {
                        lastRecordingKeyDown = -1;
                        StopOrReleaseIntrumentNote(true);
                    }
                    else
                    {
                        noteValue = noteValue - 1 + Note.FromFriendlyName("C0") + (baseRecordingOctave * 12);
                        noteValue = Utils.Clamp(noteValue, Note.MusicalNoteMin, Note.MusicalNoteMax);

                        lastRecordingKeyDown = rawKeyCode;

                        PlayInstrumentNote(noteValue, true, true);
                    }
                }
                else if (rawKeyCode == lastRecordingKeyDown)
                {
                    lastRecordingKeyDown = -1;
                    StopOrReleaseIntrumentNote(false);
                }

                return true;
            }

            return false;
        }

        public void KeyDown(KeyEventArgs e, int rawKeyCode)
        {
            bool ctrl  = e.Modifiers.HasFlag(Keys.Control);
            bool shift = e.Modifiers.HasFlag(Keys.Shift);

            // Prevent loosing focus on Alt.
            if (e.KeyCode == Keys.Menu)
                e.Handled = true;

            if (e.KeyCode == Keys.Escape)
            {
                StopInstrument();
                StopRecording();
            }

            if (e.KeyCode == Keys.Q && shift)
            {
                ToggleQwertyPiano();
                return;
            }

            if ((recordingMode || qwertyPiano) && !ctrl && !shift && HandleRecordingKey(rawKeyCode, true))
            {
                return;
            }

            if (recordingMode && e.KeyCode == Keys.Tab)
            {
                PianoRoll.AdvanceRecording(CurrentFrame, true);
            }
            else if (recordingMode && e.KeyCode == Keys.Back)
            {
                PianoRoll.DeleteRecording(CurrentFrame);
            }
            else if (e.KeyCode == Keys.PageUp)
            {
                baseRecordingOctave = Math.Min(7, baseRecordingOctave + 1);
                PianoRoll.MarkDirty();
            }
            else if (e.KeyCode == Keys.PageDown)
            {
                baseRecordingOctave = Math.Max(0, baseRecordingOctave - 1);
                PianoRoll.MarkDirty();
            }
            else if (e.KeyCode == Keys.Enter)
            {
                ToggleRecording();
            }
            else if (shift && e.KeyCode == Keys.F)
            {
                followMode = !followMode;
                ToolBar.MarkDirty();
            }
            else if (e.KeyCode == Keys.Space)
            {
                if (IsPlaying)
                {
                    StopSong();
                }
                else
                {
                    if (ctrl && shift)
                        SeekSong(song.LoopPoint >= 0 && song.LoopPoint < song.Length ? song.GetPatternStartAbsoluteNoteIndex(song.LoopPoint) : 0);
                    else if (shift)
                        SeekSong(0);
                    else if (ctrl && !PlatformUtils.IsMacOS) // CMD + Space is spotlight search on MacOS :(
                        SeekSong(song.GetPatternStartAbsoluteNoteIndex(song.PatternIndexFromAbsoluteNoteIndex(songPlayer.PlayPosition)));

                    PlaySong();
                }
            }
            else if (e.KeyCode == Keys.Home)
            {
                if (ctrl)
                {
                    SeekCurrentPattern();
                }
                else
                {
                    SeekSong(0);
                }
            }
            if (!recordingMode && e.KeyCode >= Keys.F1 && e.KeyCode <= Keys.F12)
            {
                if (ctrl)
                    GhostChannelMask ^= (1 << (e.KeyCode - Keys.F1));
                else
                    SelectedChannelIndex = (e.KeyCode - Keys.F1);
                Sequencer.MarkDirty();
            }
            else if ((ctrl && e.KeyCode == Keys.Y) || (ctrl && shift && e.KeyCode == Keys.Z))
            {
                undoRedoManager.Redo();
            }
            else if (ctrl && e.KeyCode == Keys.Z)
            {
                undoRedoManager.Undo();
            }
            else if (ctrl && e.KeyCode == Keys.N)
            {
                NewProject();
            }
            else if (ctrl && e.KeyCode == Keys.S)
            {
                SaveProjectAsync();
            }
            else if (ctrl && e.KeyCode == Keys.E)
            {
                if (shift)
                    RepeatLastExport();
                else
                    Export();
            }
            else if (ctrl && e.KeyCode == Keys.O)
            {
                OpenProject();
            }
            else if (shift && e.KeyCode == Keys.K)
            {
                ToggleQwertyPiano();
            }
            else if (e.KeyCode == Keys.Oem3)
            {
                if (ctrl)
                {
                    PianoRoll.ToggleEffectPannel();
                }
                else 
                {
                    PianoRoll.ToggleMaximize();
                }
            }
            else if (PlatformUtils.IsMacOS && ctrl && e.KeyCode == Keys.Q)
            {
                if (TryClosing())
                {
#if FAMISTUDIO_MACOS
                    // MATTT : When merging fixes from main branch, will be able to remove the #ifdef
                    Gtk.Application.Quit();
#endif
                }
            }
        }

        public bool CanCopy  => PianoRoll.IsActiveControl && PianoRoll.CanCopy  || Sequencer.IsActiveControl && Sequencer.CanCopy;
        public bool CanPaste => PianoRoll.IsActiveControl && PianoRoll.CanPaste || Sequencer.IsActiveControl && Sequencer.CanPaste;
        
        public void Copy()
        {
            if (PianoRoll.IsActiveControl)
                PianoRoll.Copy();
            else if (Sequencer.IsActiveControl)
                Sequencer.Copy();
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

        public void KeyUp(KeyEventArgs e, int rawKeyCode)
        {
            bool ctrl  = e.Modifiers.HasFlag(Keys.Control);
            bool shift = e.Modifiers.HasFlag(Keys.Shift);

            if ((recordingMode || qwertyPiano) && !ctrl && !shift && HandleRecordingKey(rawKeyCode, false))
            {
                if (recordingMode)
                    return;
            }

#if FALSE // MATTT FAMISTUDIO_WINDOWS
            if (!Sequencer.Focused) Sequencer.UnfocusedKeyUp(e);
#endif
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

            if (!songPlayer.IsPlaying)
            {
                instrumentPlayer.ConnectOscilloscope(null);
                songPlayer.ConnectOscilloscope(oscilloscope);
                songPlayer.Play(song, songPlayer.PlayPosition, palPlayback);
            }
        }

        public void StopSong()
        {
            if (songPlayer != null && songPlayer.IsPlaying)
            {
                songPlayer.Stop();
                instrumentPlayer.ConnectOscilloscope(oscilloscope);
                songPlayer.ConnectOscilloscope(null);

                // HACK: Update continuous follow mode only last time so it catches up to the 
                // real final player position.
                lastTickCurrentFrame = songPlayer.PlayPosition;
                Sequencer.UpdateFollowMode(true);
                PianoRoll.UpdateFollowMode(true);
                lastTickCurrentFrame = -1;

                MarkEverythingDirty();
            }
        }

        public void StartRecording()
        {
            Debug.Assert(!recordingMode);
            StopSong();
            recordingMode = true;
            qwertyPiano = true;
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

        public int GhostChannelMask
        {
            get { return ghostChannelMask; }
            set
            {
                ghostChannelMask = value;
                Sequencer.MarkDirty();
                PianoRoll.MarkDirty();
            }
        }

        public int GetEnvelopeFrame(Instrument instrument, int envelopeIdx, bool force = false)
        {
            if (instrumentPlayer != null && (selectedInstrument == instrument || force))
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

        private void RecreateAudioPlayers()
        {
            ShutdownSongPlayer();
            ShutdownInstrumentPlayer();
            ShutdownOscilloscope();
            InitializeMetronome();
            InitializeSongPlayer();
            InitializeInstrumentPlayer();
            InitializeOscilloscope();
        }

        private void ConditionalShowTutorial()
        {
#if !FAMISTUDIO_ANDROID // DROIDTODO
#if FAMISTUDIO_WINDOWS
            // Edge case where we open a NSF from the command line and the open dialog is active.
            if (!mainForm.CanFocus)
                return;
#endif
            if (tutorialCounter > 0)
            {
                if (--tutorialCounter == 0)
                {
                    if (Settings.ShowTutorial)
                    {
                        var dlg = new TutorialDialog();
                        if (dlg.ShowDialog(mainForm) == DialogResult.OK)
                        {
                            Settings.ShowTutorial = false;
                            Settings.Save();
                        }
                    }
                }
            }
#endif
        }

        private bool AppNeedsRealTimeUpdate()
        {
            return songPlayer       != null && songPlayer.IsPlaying       || 
                   instrumentPlayer != null && instrumentPlayer.IsPlaying || 
                   PianoRoll.IsEditingInstrument || 
                   PianoRoll.IsEditingArpeggio   || 
                   PianoRoll.IsEditingDPCMSample || 
                   pianoRollScrollChanged;
        }

        private void ProcessAudioDeviceChanges()
        {
            if (audioDeviceChanged)
            {
                RecreateAudioPlayers();
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

            pianoRollScrollChanged = false;
        }

        private void HighlightPlayingInstrumentNote()
        {
            if (instrumentPlayer != null)
                PianoRoll.HighlightPianoNote(instrumentPlayer.PlayingNote);
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

        public void Tick(float deltaTime = -1.0f)
        {
            Debug.Assert(!mainForm.IsAsyncDialogInProgress);

            lastTickCurrentFrame = IsPlaying ? songPlayer.PlayPosition : -1;

            ProcessAudioDeviceChanges();
            ProcessQueuedMidiNotes();
            TickControls(deltaTime);
            ConditionalMarkControlsDirty();
            ConditionalShowTutorial();
            CheckNewReleaseDone();
            HighlightPlayingInstrumentNote();
            CheckStopInstrumentNote(deltaTime);
        }

        private void TickControls(float deltaTime)
        {
            var tickTime = DateTime.Now;

            if (deltaTime < 0.0f)
                deltaTime = (float)(tickTime - lastTickTime).TotalSeconds;

            deltaTime = (float)Math.Min(0.25f, deltaTime);
            averageTickRateMs = Utils.Lerp(averageTickRateMs, deltaTime * 1000.0f, 0.01f);

            ToolBar.Tick(deltaTime);
            PianoRoll.Tick(deltaTime);
            Sequencer.Tick(deltaTime);
            ProjectExplorer.Tick(deltaTime);
            QuickAccessBar.Tick(deltaTime);

            lastTickTime = tickTime;
        }

        private void Sequencer_PatternClicked(int trackIndex, int patternIndex)
        {
            PianoRoll.StartEditPattern(trackIndex, patternIndex);
        }

        private void PianoRoll_PatternChanged(Pattern pattern)
        {
            Sequencer.NotifyPatternChange(pattern);
            Sequencer.MarkDirty();
        }

        private void PianoRoll_ManyPatternChanged()
        {
            Sequencer.InvalidatePatternCache();
            Sequencer.MarkDirty();
        }

        private void PianoRoll_DPCMSampleChanged()
        {
            ProjectExplorer.MarkDirty();
        }

        private void ProjectExplorer_InstrumentEdited(Instrument instrument, int envelope)
        {
            if (instrument == null)
            {
                PianoRoll.StartEditDPCMMapping();
            }
            else
            {
                PianoRoll.StartEditEnveloppe(instrument, envelope);
            }
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

        private void ProjectExplorer_DPCMSampleEdited(DPCMSample sample)
        {
            PianoRoll.StartEditDPCMSample(sample);
        }

        private void PianoRoll_EnvelopeChanged()
        {
            ProjectExplorer.MarkDirty();
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
                    case 0: mainForm.SetActiveControl(Sequencer, false); break;
                    case 1: mainForm.SetActiveControl(PianoRoll, false); break;
                    case 2: mainForm.SetActiveControl(ProjectExplorer, false); break;
                }
            }
        }

        public void SerializeState(ProjectBuffer buffer)
        {
            var oldSong = song;
            var currentFrame = CurrentFrame;

            buffer.Serialize(ref selectedChannelIndex);
            buffer.Serialize(ref selectedInstrument);
            buffer.Serialize(ref selectedArpeggio);
            buffer.Serialize(ref ghostChannelMask);
            buffer.Serialize(ref song);
            buffer.Serialize(ref currentFrame);

            ProjectExplorer.SerializeState(buffer);
            Sequencer.SerializeState(buffer);
            PianoRoll.SerializeState(buffer);

            SerializeActiveControl(buffer);

            if (buffer.IsReading)
            {
                // Move seek bar on undo/redo when recording.
                if (recordingMode)
                    SeekSong(currentFrame);

                RefreshLayout();
                mainForm.MarkDirty();

                // When the song changes between undo/redos, must stop the audio.
                if (oldSong.Id != song.Id)
                {
                    ResetSelectedSong();
                }
            }
        }

        //private void ProjectExplorer_InstrumentSelected(Instrument instrument)
        //{
        //    selectedInstrument = instrument;
        //}

        //private void ProjectExplorer_ArpeggioSelected(Arpeggio arpeggio)
        //{
        //    selectedArpeggio = arpeggio;
        //}

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

    // Move this to a common class
    public class ContextMenuOption
    {
        public string Image { get; private set; }
        public string Text { get; private set; }
        public Action Callback { get; private set; }

        public ContextMenuOption(string img, string text, Action callback)
        {
            Image = img;
            Text = text;
            Callback = callback;
        }
    }
}
