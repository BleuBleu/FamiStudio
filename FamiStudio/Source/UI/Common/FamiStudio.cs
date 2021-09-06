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

using RenderTheme = FamiStudio.ThemeRenderResources;

namespace FamiStudio
{
    public class FamiStudio
    {
        private const int MaxAutosaves = 30;

        private FamiStudioForm mainForm;
        private Project project;
        private Song song;
        private SongPlayer songPlayer;
        private InstrumentPlayer instrumentPlayer;
        private Oscilloscope oscilloscope;
        private UndoRedoManager undoRedoManager;
        private ExportDialog exportDialog;
        private int ghostChannelMask = 0;
        private int lastMidiNote = -1;
        private bool palPlayback = false;
        private bool audioDeviceChanged = false;
        private bool pianoRollScrollChanged = false;
        private bool recordingMode = false;
        private bool qwertyPiano = false;
        private bool followMode = false;
        private int tutorialCounter = 3;
        private int baseRecordingOctave = 3;
        private int lastTickCurrentFrame = -1;
        private int previewDPCMSampleId = -1;
        private int previewDPCMSampleRate = 44100;
        private bool previewDPCMIsSource = false;
        private bool metronome = false;
        private short[] metronomeSound;
        private int lastRecordingKeyDown = -1;
        private BitArray keyStates = new BitArray(65536);
        private ConcurrentQueue<Tuple<int, bool>> midiNoteQueue = new ConcurrentQueue<Tuple<int, bool>>();
#if FAMISTUDIO_WINDOWS
        private MultiMediaNotificationListener mmNoticiations;
#endif
        private DateTime lastTickTime = DateTime.Now;
        private float averageTickRateMs = 8.0f;
        private int autoSaveIndex = 0;
        private DateTime lastAutoSave;

        private bool newReleaseAvailable = false;
        private string newReleaseString = null;
        private string newReleaseUrl = null;

        public bool IsPlaying => songPlayer != null && songPlayer.IsPlaying;
        public bool IsRecording => recordingMode;
        public bool IsQwertyPianoEnabled => qwertyPiano;
        public bool IsMetronomeEnabled => metronome;
        public bool FollowModeEnabled { get => followMode; set => followMode = value; }
        public bool SequencerHasSelection => Sequencer.GetPatternTimeSelectionRange(out _, out _);
        public int BaseRecordingOctave => baseRecordingOctave;
        public int CurrentFrame => lastTickCurrentFrame >= 0 ? lastTickCurrentFrame : (songPlayer != null ? songPlayer.PlayPosition : 0);
        public int ChannelMask { get => songPlayer != null ? songPlayer.ChannelMask : -1; set => songPlayer.ChannelMask = value; }
        public int PlayRate { get => songPlayer != null ? songPlayer.PlayRate : 1; set { if (!IsPlaying) songPlayer.PlayRate = value; } }
        public float AverageTickRate => averageTickRateMs;

        public Project Project => project;
        public Song Song => song;
        public UndoRedoManager UndoRedoManager => undoRedoManager;
        public LoopMode LoopMode { get => songPlayer != null ? songPlayer.Loop : LoopMode.LoopPoint; set => songPlayer.Loop = value; }
        public DPCMSample DraggedSample => ProjectExplorer.DraggedSample;

        public int PreviewDPCMWavPosition => instrumentPlayer != null ? instrumentPlayer.RawPcmSamplePlayPosition : 0;
        public int PreviewDPCMSampleId => previewDPCMSampleId;
        public bool PreviewDPCMIsSource => previewDPCMIsSource;
        public int PreviewDPCMSampleRate => previewDPCMSampleRate;

        public Toolbar ToolBar => mainForm.ToolBar;
        public Sequencer Sequencer => mainForm.Sequencer;
        public PianoRoll PianoRoll => mainForm.PianoRoll;
        public ProjectExplorer ProjectExplorer => mainForm.ProjectExplorer;

        public static Project StaticProject { get; set; }
        public static FamiStudio StaticInstance { get; private set; }

        public void Initialize(string filename)
        {
            StaticInstance = this;

#if FAMISTUDIO_ANDROID
            mainForm = FamiStudioForm.Instance;
#else
            mainForm = new FamiStudioForm(this);
#endif

            Sequencer.PatternClicked += sequencer_PatternClicked;
            Sequencer.SelectedChannelChanged += sequencer_SelectedChannelChanged;
            Sequencer.ControlActivated += Sequencer_ControlActivated;
            Sequencer.PatternModified += Sequencer_PatternModified;
            Sequencer.PatternsPasted += PianoRoll_NotesPasted;
            Sequencer.SelectionChanged += Sequencer_SelectionChanged;
            PianoRoll.PatternChanged += pianoRoll_PatternChanged;
            PianoRoll.ManyPatternChanged += PianoRoll_ManyPatternChanged;
            PianoRoll.DPCMSampleChanged += PianoRoll_DPCMSampleChanged;
            PianoRoll.EnvelopeChanged += pianoRoll_EnvelopeChanged;
            PianoRoll.ControlActivated += PianoRoll_ControlActivated;
            PianoRoll.NotesPasted += PianoRoll_NotesPasted;
            PianoRoll.ScrollChanged += PianoRoll_ScrollChanged;
            PianoRoll.NoteEyedropped += PianoRoll_NoteEyedropped;
            PianoRoll.DPCMSampleMapped += PianoRoll_DPCMSampleMapped;
            PianoRoll.DPCMSampleUnmapped += PianoRoll_DPCMSampleMapped;
            PianoRoll.MaximizedChanged += PianoRoll_MaximizedChanged;
            ProjectExplorer.InstrumentEdited += projectExplorer_InstrumentEdited;
            ProjectExplorer.InstrumentSelected += projectExplorer_InstrumentSelected;
            ProjectExplorer.InstrumentColorChanged += projectExplorer_InstrumentColorChanged;
            ProjectExplorer.InstrumentReplaced += projectExplorer_InstrumentReplaced;
            ProjectExplorer.InstrumentDeleted += ProjectExplorer_InstrumentDeleted;
            ProjectExplorer.InstrumentDroppedOutside += ProjectExplorer_InstrumentDroppedOutside;
            ProjectExplorer.SongModified += projectExplorer_SongModified;
            ProjectExplorer.SongSelected += projectExplorer_SongSelected;
            ProjectExplorer.ProjectModified += ProjectExplorer_ProjectModified;
            ProjectExplorer.ArpeggioSelected += ProjectExplorer_ArpeggioSelected;
            ProjectExplorer.ArpeggioEdited += ProjectExplorer_ArpeggioEdited;
            ProjectExplorer.ArpeggioColorChanged += ProjectExplorer_ArpeggioColorChanged;
            ProjectExplorer.ArpeggioDeleted += ProjectExplorer_ArpeggioDeleted;
            ProjectExplorer.ArpeggioDroppedOutside += ProjectExplorer_ArpeggioDroppedOutside;
            ProjectExplorer.DPCMSampleReloaded += ProjectExplorer_DPCMSampleReloaded;
            ProjectExplorer.DPCMSampleEdited += ProjectExplorer_DPCMSampleEdited;
            ProjectExplorer.DPCMSampleColorChanged += ProjectExplorer_DPCMSampleColorChanged;
            ProjectExplorer.DPCMSampleDeleted += ProjectExplorer_DPCMSampleDeleted;
            ProjectExplorer.DPCMSampleDraggedOutside += ProjectExplorer_DPCMSampleDraggedOutside;
            ProjectExplorer.DPCMSampleMapped += ProjectExplorer_DPCMSampleMapped;
            ProjectExplorer.InstrumentsHovered += ProjectExplorer_InstrumentsHovered;

            InitializeMetronome();
            InitializeSongPlayer();
            InitializeMidi();
            InitializeMultiMediaNotifications();

            if (string.IsNullOrEmpty(filename) && Settings.OpenLastProjectOnStart && !string.IsNullOrEmpty(Settings.LastProjectFile) && File.Exists(Settings.LastProjectFile))
            {
                filename = Settings.LastProjectFile;
            }

            if (!string.IsNullOrEmpty(filename))
            {
                OpenProject(filename);
            }
            else
            {
                NewProject(true);
            }

#if !DEBUG
            if (Settings.CheckUpdates)
            {
                Task.Factory.StartNew(CheckForNewRelease);
            }
#endif
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
                ProjectExplorer.SelectedInstrument = note.Instrument;
                ProjectExplorer.SelectedArpeggio = note.Arpeggio;
                PianoRoll.CurrentInstrument = note.Instrument;
                PianoRoll.CurrentArpeggio = note.Arpeggio;
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

        private void ProjectExplorer_ProjectModified()
        {
            RefreshLayout();
            Sequencer.Reset();
            PianoRoll.Reset();
            PianoRoll.CurrentInstrument = ProjectExplorer.SelectedInstrument;
            PianoRoll.CurrentArpeggio = ProjectExplorer.SelectedArpeggio;
        }

        private void ProjectExplorer_InstrumentDeleted(Instrument instrument)
        {
            PianoRoll.Reset();
            PianoRoll.CurrentInstrument = ProjectExplorer.SelectedInstrument;
            PianoRoll.CurrentArpeggio = ProjectExplorer.SelectedArpeggio;
        }

        private void ProjectExplorer_ArpeggioDeleted(Arpeggio arpeggio)
        {
            PianoRoll.Reset();
            PianoRoll.CurrentInstrument = ProjectExplorer.SelectedInstrument;
            PianoRoll.CurrentArpeggio = ProjectExplorer.SelectedArpeggio;
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

        private void Sequencer_ControlActivated()
        {
            PianoRoll.ShowSelection = false;
            Sequencer.ShowSelection = true;
            ToolBar.MarkDirty();
        }

        private void PianoRoll_ControlActivated()
        {
            PianoRoll.ShowSelection = true;
            Sequencer.ShowSelection = false;
            ToolBar.MarkDirty();
        }

        private void sequencer_SelectedChannelChanged(int channelIdx)
        {
            StopInstrument();
            PianoRoll.ChangeChannel(channelIdx);
        }

        public void Run()
        {
            mainForm.Run();
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
            {
                project.SongExists(song);
                Debug.Assert(song == ProjectExplorer.SelectedSong);
            }

            Sequencer.ValidateIntegrity();
#endif
        }

        public bool CheckUnloadProject()
        {
            if (undoRedoManager != null)
            {
                if (undoRedoManager.NeedsSaving)
                {
                    var result = PlatformUtils.MessageBox("Save changes?", "FamiStudio", MessageBoxButtons.YesNoCancel);
                    if (result == DialogResult.Cancel)
                    {
                        return false;
                    }

                    if (result == DialogResult.Yes)
                    {
                        if (!SaveProject())
                        {
                            return false;
                        }
                    }
                }

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

            return true;
        }

        public void NewProject(bool isDefault = false)
        {
            if (!CheckUnloadProject())
            {
                return;
            }

            project = new Project(true);
            InitProject();

            if (!isDefault)
                Settings.LastProjectFile = "";
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

            PianoRoll.CurrentInstrument = ProjectExplorer.SelectedInstrument;
            PianoRoll.CurrentArpeggio = ProjectExplorer.SelectedArpeggio;

            MarkEverythingDirty();
            UpdateTitle();
            RefreshLayout();
        }

        public void OpenProject(string filename)
        {
            StopEverything();

            if (!CheckUnloadProject())
            {
                return;
            }

#if !FAMISTUDIO_ANDROID // DROIDTODO
            var dlgLog = new LogDialog(mainForm);
            using (var scopedLog = new ScopedLogOutput(dlgLog, LogSeverity.Warning))
            {
#endif
                project = OpenProjectFile(filename);

                if (project != null)
                {
                    InitProject();

                    if (Path.GetExtension(filename).ToLower() == ".fms")
                        Settings.LastProjectFile = filename;
                }
                else
                {
                    NewProject();
                }

                mainForm.Refresh();
#if !FAMISTUDIO_ANDROID // DROIDTODO
                dlgLog.ShowDialogIfMessages();
            }
#endif
        }

        public void OpenProject()
        {
            var filename = PlatformUtils.ShowOpenFileDialog("Open File", "All Supported Files (*.fms;*.txt;*.nsf;*.nsfe;*.ftm;*.mid)|*.fms;*.txt;*.nsf;*.nsfe;*.ftm;*.mid|FamiStudio Files (*.fms)|*.fms|FamiTracker Files (*.ftm)|*.ftm|FamiTracker Text Export (*.txt)|*.txt|FamiStudio Text Export (*.txt)|*.txt|NES Sound Format (*.nsf;*.nsfe)|*.nsf;*.nsfe|MIDI files (*.mid)|*.mid", ref Settings.LastFileFolder);
            if (filename != null)
            {
                OpenProject(filename);
            }
        }

        public Project OpenProjectFile(string filename, bool allowComplexFormats = true)
        {
            var project = (Project)null;

            if (filename.ToLower().EndsWith("fms"))
            {
                project = new ProjectFile().Load(filename);
            }
            else if (filename.ToLower().EndsWith("ftm"))
            {
                project = new FamitrackerBinaryFile().Load(filename);
            }
            else if (filename.ToLower().EndsWith("txt"))
            {
                if (FamistudioTextFile.LooksLikeFamiStudioText(filename))
                    project = new FamistudioTextFile().Load(filename);
                else
                    project = new FamitrackerTextFile().Load(filename);
            }
            else if (allowComplexFormats && filename.ToLower().EndsWith("mid"))
            {
#if !FAMISTUDIO_ANDROID // DROIDTODO
                var dlg = new MidiImportDialog(filename);
                project = dlg.ShowDialog(mainForm);
#endif
            }
            else if (allowComplexFormats && (filename.ToLower().EndsWith("nsf") || filename.ToLower().EndsWith("nsfe")))
            {
#if !FAMISTUDIO_ANDROID // DROIDTODO
                var dlg = new NsfImportDialog(filename);

                if (dlg.ShowDialog(mainForm) == DialogResult.OK)
                {
                    project = new NsfFile().Load(filename, dlg.SongIndex, dlg.Duration, dlg.PatternLength, dlg.StartFrame, dlg.RemoveIntroSilence, dlg.ReverseDpcmBits, dlg.PreserveDpcmPadding);
                }
#endif
            }

            return project;
        }

        public bool SaveProject(bool forceSaveAs = false)
        {
            bool success = true;

            if (forceSaveAs || string.IsNullOrEmpty(project.Filename))
            {
                string filename = PlatformUtils.ShowSaveFileDialog("Save File", "FamiStudio Files (*.fms)|*.fms", ref Settings.LastFileFolder);
                if (filename != null)
                {
                    success = new ProjectFile().Save(project, filename);
                    if (success)
                    {
                        UpdateTitle();
                        Settings.LastProjectFile = project.Filename;
                    }
                }
                else
                {
                    return false;
                }
            }
            else
            {
                success = new ProjectFile().Save(project, project.Filename);
            }

            if (success)
            {
                if (Settings.ClearUndoRedoOnSave)
                    undoRedoManager.Clear();

                undoRedoManager.NotifySaved();
            }
            else
            {
                PlatformUtils.MessageBox("An error happened while saving.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            MarkEverythingDirty();

            return true;
        }

        public void Export()
        {
            FreeExportDialog();

            exportDialog = new ExportDialog(project);
            exportDialog.Exporting += ExportDialog_Exporting;
            exportDialog.ShowDialog(mainForm);
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
                exportDialog.Export(mainForm, true);
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

        public void OpenConfigDialog()
        {
            var dlg = new ConfigDialog();

            dlg.ShowDialog(mainForm, (r) =>
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
            if (!CheckUnloadProject())
            {
                return false;
            }

            Midi.NotePlayed -= Midi_NotePlayed;
            Midi.Shutdown();

            StopEverything();
            ShutdownInstrumentPlayer();
            ShutdownSongPlayer();

            return true;
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
                    var path = Settings.GetAutoSaveFilePath();
                    var filename = Path.Combine(path, $"AutoSave{autoSaveIndex:D2}.fms");

                    var oldFilename = project.Filename;
                    new ProjectFile().Save(project, filename);
                    project.Filename = oldFilename;

                    autoSaveIndex = (autoSaveIndex + 1) % MaxAutosaves;
                    lastAutoSave = now;
                }
            }
        }

        private void CheckNewReleaseDone()
        {
            if (newReleaseAvailable)
            {
                newReleaseAvailable = false;

                if (PlatformUtils.MessageBox($"A new version ({newReleaseString}) is available. Do you want to download it?", "New Version", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    Utils.OpenUrl("http://www.famistudio.org");
                }
            }
        }

        public void OpenTransformDialog()
        {
#if !FAMISTUDIO_ANDROID // DROIDTODO
            var dlg = new TransformDialog(this);
            dlg.CleaningUp += TransformDialog_CleaningUp;
            dlg.ShowDialog(mainForm, (r) =>
            {
                if (r == DialogResult.OK)
                {
                    Sequencer.Reset();
                    PianoRoll.Reset();
                    ProjectExplorer.RefreshButtons();
                    MarkEverythingDirty();
                }
            });
#endif
        }

        private void TransformDialog_CleaningUp()
        {
            StopEverything();

            // We might be deleting data like entire songs/envelopes/samples that
            // are being edited right now.
            Sequencer.Reset();
            PianoRoll.Reset();
        }

        public void ShowHelp()
        {
            Utils.OpenUrl("http://www.famistudio.org/doc/index.html");
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

        public void PlayInstrumentNote(int n, bool showWarning, bool allowRecording, bool custom = false, Instrument customInstrument = null, Arpeggio customArpeggio = null)
        {
            Note note = new Note(n);
            note.Volume = Note.VolumeMax;

            var instrument = custom ? customInstrument : ProjectExplorer.SelectedInstrument;
            var arpeggio = custom ? customArpeggio : ProjectExplorer.SelectedArpeggio;

            int channel = Sequencer.SelectedChannel;

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

            if (PianoRoll.IsEditingArpeggio && song.Channels[channel].SupportsArpeggios)
            {
                note.Arpeggio = PianoRoll.CurrentArpeggio;
            }

            instrumentPlayer.PlayNote(channel, note);

            if (allowRecording && recordingMode)
                PianoRoll.RecordNote(note);
        }

        public void StopOrReleaseIntrumentNote(bool allowRecording = false)
        {
            if (ProjectExplorer.SelectedInstrument != null &&
                (ProjectExplorer.SelectedInstrument.HasReleaseEnvelope || ProjectExplorer.SelectedInstrument.IsVrc7Instrument) &&
                song.Channels[Sequencer.SelectedChannel].SupportsInstrument(ProjectExplorer.SelectedInstrument))
            {
                instrumentPlayer.ReleaseNote(Sequencer.SelectedChannel);
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
                    Sequencer.SelectedChannel = (e.KeyCode - Keys.F1);
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
                SaveProject();
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

        public bool CanCopy  => PianoRoll.CanCopy  || Sequencer.CanCopy;
        public bool CanPaste => PianoRoll.CanPaste || Sequencer.CanPaste;

        public void Copy()
        {
            if (PianoRoll.ShowSelection)
                PianoRoll.Copy();
            else
                Sequencer.Copy();
        }

        public void Cut()
        {
            if (PianoRoll.ShowSelection)
                PianoRoll.Cut();
            else
                Sequencer.Cut();
        }

        public void Paste()
        {
            if (PianoRoll.ShowSelection)
                PianoRoll.Paste();
            else
                Sequencer.Paste();
        }

        public void PasteSpecial()
        {
            if (PianoRoll.ShowSelection)
                PianoRoll.PasteSpecial();
            else
                Sequencer.PasteSpecial();
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
            if (instrumentPlayer != null && (ProjectExplorer.SelectedInstrument == instrument || force))
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

        public void Tick(float deltaTime = -1.0f)
        {
            lastTickCurrentFrame = IsPlaying ? songPlayer.PlayPosition : -1;

            ProcessAudioDeviceChanges();
            ProcessQueuedMidiNotes();
            TickControls(deltaTime);
            ConditionalMarkControlsDirty();
            ConditionalShowTutorial();
            CheckNewReleaseDone();
            HighlightPlayingInstrumentNote();
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

            lastTickTime = tickTime;
        }

        private void sequencer_PatternClicked(int trackIndex, int patternIndex)
        {
            PianoRoll.StartEditPattern(trackIndex, patternIndex);
        }

        private void pianoRoll_PatternChanged(Pattern pattern)
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

        private void projectExplorer_InstrumentEdited(Instrument instrument, int envelope)
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
            if (PianoRoll.IsEditingDPCMSample && PianoRoll.CurrentEditSample == sample)
            {
                PianoRoll.StartEditDPCMSample(sample);
            }
        }

        private void ProjectExplorer_DPCMSampleEdited(DPCMSample sample)
        {
            PianoRoll.StartEditDPCMSample(sample);
        }

        private void pianoRoll_EnvelopeChanged()
        {
            ProjectExplorer.MarkDirty();
        }

        public void SerializeState(ProjectBuffer buffer)
        {
            var oldSong = song;
            var currentFrame = CurrentFrame;

            buffer.Serialize(ref ghostChannelMask);
            buffer.Serialize(ref song);
            buffer.Serialize(ref currentFrame);

            ProjectExplorer.SerializeState(buffer);
            Sequencer.SerializeState(buffer);
            PianoRoll.SerializeState(buffer);

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
                    Debug.Assert(ProjectExplorer.SelectedSong == song);
                    projectExplorer_SongSelected(song);
                }
            }
        }

        private void projectExplorer_InstrumentSelected(Instrument instrument)
        {
            PianoRoll.CurrentInstrument = instrument;
        }

        private void ProjectExplorer_ArpeggioSelected(Arpeggio arpeggio)
        {
            PianoRoll.CurrentArpeggio = arpeggio;
        }

        private void projectExplorer_InstrumentColorChanged(Instrument instrument)
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

        private void projectExplorer_SongModified(Song song)
        {
            Sequencer.SongModified();
            PianoRoll.SongModified();
            MarkEverythingDirty();
        }

        private void projectExplorer_SongSelected(Song song)
        {
            StopSong();
            SeekSong(0);
            this.song = song;

            PianoRoll.SongChanged();
            Sequencer.Reset();
            ToolBar.Reset();

            MarkEverythingDirty();
        }

        private void projectExplorer_InstrumentReplaced(Instrument instrument)
        {
            Sequencer.InvalidatePatternCache();
            MarkEverythingDirty();
        }
    }
}
