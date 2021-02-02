using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace FamiStudio
{
    public class FamiStudio
    {
        // TODO: Get rid of this!
        public static Project StaticProject { get; set; }

        private FamiStudioForm mainForm;
        private Project project;
        private Song song;
        private SongPlayer songPlayer;
        private InstrumentPlayer instrumentPlayer;
        private UndoRedoManager undoRedoManager;
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
        private Keys lastRecordingKeyDown = Keys.None; 
        private bool[] keyStates = new bool[256];
#if FAMISTUDIO_WINDOWS
        private MultiMediaNotificationListener mmNoticiations;
#endif

        private bool newReleaseAvailable = false;
        private string newReleaseString = null;
        private string newReleaseUrl = null;

        public bool RealTimeUpdate => songPlayer != null && songPlayer.IsPlaying || PianoRoll.IsEditingInstrument || PianoRoll.IsEditingArpeggio || PianoRoll.IsEditingDPCMSample || pianoRollScrollChanged;
        public bool IsPlaying => songPlayer != null && songPlayer.IsPlaying;
        public bool IsRecording => recordingMode;
        public bool IsQwertyPianoEnabled => qwertyPiano;
        public bool FollowModeEnabled { get => followMode; set => followMode = value; }
        public int BaseRecordingOctave => baseRecordingOctave;
        public int CurrentFrame => lastTickCurrentFrame >= 0 ? lastTickCurrentFrame : (songPlayer != null ? songPlayer.CurrentFrame : 0);
        public int ChannelMask { get => songPlayer != null ? songPlayer.ChannelMask : 0xffff; set => songPlayer.ChannelMask = value; }
        public string ToolTip { get => ToolBar.ToolTip; set => ToolBar.ToolTip = value; }
        public Project Project => project;
        public Song Song => song;
        public UndoRedoManager UndoRedoManager => undoRedoManager;
        public LoopMode LoopMode { get => songPlayer != null ? songPlayer.Loop : LoopMode.LoopPoint; set => songPlayer.Loop = value; }

        public int  PreviewDPCMWavPosition => instrumentPlayer != null ? instrumentPlayer.RawPcmSamplePlayPosition : 0;
        public int  PreviewDPCMSampleId    => previewDPCMSampleId;
        public bool PreviewDPCMIsSource    => previewDPCMIsSource;
        public int  PreviewDPCMSampleRate  => previewDPCMSampleRate;

        public Toolbar ToolBar => mainForm.ToolBar;
        public Sequencer Sequencer => mainForm.Sequencer;
        public PianoRoll PianoRoll => mainForm.PianoRoll;
        public ProjectExplorer ProjectExplorer => mainForm.ProjectExplorer;
        public Rectangle MainWindowBounds => mainForm.Bounds;

        static readonly Dictionary<Keys, int> RecordingKeyToNoteMap = new Dictionary<Keys, int>
        {
            { Keys.D1,        -1 }, // Special: Stop note.
            { Keys.Z,          0 },
            { Keys.S,          1 },
            { Keys.X,          2 },
            { Keys.D,          3 },
            { Keys.C,          4 },
            { Keys.V,          5 },
            { Keys.G,          6 },
            { Keys.B,          7 },
            { Keys.H,          8 },
            { Keys.N,          9 },
            { Keys.J,         10 },
            { Keys.M,         11 },

            { Keys.Oemcomma,  12 },
            { Keys.L,         13 },
            { Keys.OemPeriod, 14 },
            { Keys.Oem1,      15 }, // ;
            { Keys.Oem2,      16 }, // /

            { Keys.Q,         12 },
            { Keys.D2,        13 },
            { Keys.W,         14 },
            { Keys.D3,        15 },
            { Keys.E,         16 },
            { Keys.R,         17 },
            { Keys.D5,        18 },
            { Keys.T,         19 },
            { Keys.D6,        20 },
            { Keys.Y,         21 },
            { Keys.D7,        22 },
            { Keys.U,         23 },

            { Keys.I,         24 },
            { Keys.D9,        25 },
            { Keys.O,         26 },
            { Keys.D0,        27 },
            { Keys.P,         28 },
            { Keys.Oem4,      29 }, // [
            { Keys.Oemplus,   30 }, // +/=
            { Keys.Oem6,      31 }, // ]
        };

        public FamiStudio(string filename)
        {
            mainForm = new FamiStudioForm(this);

            Sequencer.PatternClicked += sequencer_PatternClicked;
            Sequencer.SelectedChannelChanged += sequencer_SelectedChannelChanged;
            Sequencer.ControlActivated += Sequencer_ControlActivated;
            Sequencer.PatternModified += Sequencer_PatternModified;
            Sequencer.PatternsPasted += PianoRoll_NotesPasted;
            PianoRoll.PatternChanged += pianoRoll_PatternChanged;
            PianoRoll.ManyPatternChanged += PianoRoll_ManyPatternChanged;
            PianoRoll.DPCMSampleChanged += PianoRoll_DPCMSampleChanged;
            PianoRoll.EnvelopeChanged += pianoRoll_EnvelopeChanged;
            PianoRoll.ControlActivated += PianoRoll_ControlActivated;
            PianoRoll.NotesPasted += PianoRoll_NotesPasted;
            PianoRoll.ScrollChanged += PianoRoll_ScrollChanged;
            ProjectExplorer.InstrumentEdited += projectExplorer_InstrumentEdited;
            ProjectExplorer.InstrumentSelected += projectExplorer_InstrumentSelected;
            ProjectExplorer.InstrumentColorChanged += projectExplorer_InstrumentColorChanged;
            ProjectExplorer.InstrumentReplaced += projectExplorer_InstrumentReplaced;
            ProjectExplorer.InstrumentDeleted += ProjectExplorer_InstrumentDeleted;
            ProjectExplorer.InstrumentDraggedOutside += ProjectExplorer_InstrumentDraggedOutside;
            ProjectExplorer.SongModified += projectExplorer_SongModified;
            ProjectExplorer.SongSelected += projectExplorer_SongSelected;
            ProjectExplorer.ProjectModified += ProjectExplorer_ProjectModified;
            ProjectExplorer.ArpeggioSelected += ProjectExplorer_ArpeggioSelected;
            ProjectExplorer.ArpeggioEdited += ProjectExplorer_ArpeggioEdited;
            ProjectExplorer.ArpeggioColorChanged += ProjectExplorer_ArpeggioColorChanged;
            ProjectExplorer.ArpeggioDeleted += ProjectExplorer_ArpeggioDeleted;
            ProjectExplorer.ArpeggioDraggedOutside += ProjectExplorer_ArpeggioDraggedOutside;
            ProjectExplorer.DPCMSampleEdited += ProjectExplorer_DPCMSampleEdited;
            ProjectExplorer.DPCMSampleColorChanged += ProjectExplorer_DPCMSampleColorChanged;
            ProjectExplorer.DPCMSampleDeleted += ProjectExplorer_DPCMSampleDeleted;

            InitializeSongPlayer();
            InitializeMidi();
            InitializeMultiMediaNotifications();

            if (!string.IsNullOrEmpty(filename))
            {
                OpenProject(filename);
            }
            else
            {
                NewProject();
            }

#if !DEBUG
            if (Settings.CheckUpdates)
            {
                Task.Factory.StartNew(CheckForNewRelease);
            }
#endif
        }

        private void PianoRoll_ScrollChanged()
        {
            if (Settings.ShowPianoRollViewRange)
            {
#if FAMISTUDIO_WINDOWS
                pianoRollScrollChanged = true;
#else
                Sequencer.ConditionalInvalidate();
#endif
            }
        }

        private void ProjectExplorer_ProjectModified()
        {
            RefreshSequencerLayout();
            Sequencer.Reset();
            PianoRoll.Reset();
            PianoRoll.CurrentInstrument = ProjectExplorer.SelectedInstrument;
            PianoRoll.CurrentArpeggio   = ProjectExplorer.SelectedArpeggio;
        }

        private void ProjectExplorer_InstrumentDeleted(Instrument instrument)
        {
            PianoRoll.Reset();
            PianoRoll.CurrentInstrument = ProjectExplorer.SelectedInstrument;
            PianoRoll.CurrentArpeggio   = ProjectExplorer.SelectedArpeggio;
        }

        private void ProjectExplorer_ArpeggioDeleted(Arpeggio arpeggio)
        {
            PianoRoll.Reset();
            PianoRoll.CurrentInstrument = ProjectExplorer.SelectedInstrument;
            PianoRoll.CurrentArpeggio   = ProjectExplorer.SelectedArpeggio;
        }

        private void ProjectExplorer_DPCMSampleDeleted(DPCMSample sample)
        {
            PianoRoll.Reset();
        }

        private void PianoRoll_NotesPasted()
        {
            ProjectExplorer.RefreshButtons();
            ProjectExplorer.ConditionalInvalidate();
        }

        private void ProjectExplorer_InstrumentDraggedOutside(Instrument instrument, Point pos)
        {
            var pianoRollClientPos = PianoRoll.PointToClient(pos);

            if (PianoRoll.ClientRectangle.Contains(pianoRollClientPos))
            {
                PianoRoll.ReplaceSelectionInstrument(instrument);
                PianoRoll.Focus();
            }
        }

        private void ProjectExplorer_ArpeggioDraggedOutside(Arpeggio arpeggio, Point pos)
        {
            var pianoRollClientPos = PianoRoll.PointToClient(pos);

            if (PianoRoll.ClientRectangle.Contains(pianoRollClientPos))
            {
                PianoRoll.ReplaceSelectionArpeggio(arpeggio);
                PianoRoll.Focus();
            }
        }

        private void Sequencer_PatternModified()
        {
            PianoRoll.ConditionalInvalidate();
        }

        private void Sequencer_ControlActivated()
        {
            PianoRoll.ShowSelection = false;
            Sequencer.ShowSelection = true;
            ToolBar.Invalidate();
        }

        private void PianoRoll_ControlActivated()
        {
            PianoRoll.ShowSelection = true;
            Sequencer.ShowSelection = false;
            ToolBar.Invalidate();
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

        public void InitializeMultiMediaNotifications()
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

        private void MmNoticiations_DefaultDeviceChanged()
        {
            audioDeviceChanged = true;
        }

        public void InitializeMidi()
        {
            Midi.Initialize();

            Midi.Close();
            Midi.NotePlayed -= Midi_NotePlayed;

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
            }
        }

        private void UndoRedoManager_PostUndoRedo(TransactionScope scope, TransactionFlags flags)
        {
            if (flags.HasFlag(TransactionFlags.ReinitializeAudio))
            {
                palPlayback = project.PalMode;
                InitializeInstrumentPlayer();
                InitializeSongPlayer();
                InvalidateEverything();
            }

            if (flags.HasFlag(TransactionFlags.StopAudio))
            {
                InvalidateEverything();
            }
        }

        private void UndoRedoManager_Updated()
        {
            ToolBar.Invalidate();
        }

        public bool CheckUnloadProject()
        {
            if (undoRedoManager != null)
            {
                if (undoRedoManager.UndoScope != TransactionScope.Max)
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

                undoRedoManager.PreUndoRedo      -= UndoRedoManager_PreUndoRedo;
                undoRedoManager.PostUndoRedo     -= UndoRedoManager_PostUndoRedo;
                undoRedoManager.TransactionBegan -= UndoRedoManager_PreUndoRedo;
                undoRedoManager.TransactionEnded -= UndoRedoManager_PostUndoRedo;
                undoRedoManager.Updated -= UndoRedoManager_Updated;
                undoRedoManager = null;
                project = null;

                StopEverything();
            }

            return true;
        }

        public void NewProject()
        {
            if (!CheckUnloadProject())
            {
                return;
            }

            project = new Project(true);
            InitProject();
        }

        private void InitProject()
        {
            StopRecording();
            ShutdownSongPlayer();
            ShutdownInstrumentPlayer();

            StaticProject = project;
            song = project.Songs[0];
			palPlayback = project.PalMode;

            InitializeSongPlayer();
            InitializeInstrumentPlayer();

            undoRedoManager = new UndoRedoManager(project, this);
            undoRedoManager.PreUndoRedo      += UndoRedoManager_PreUndoRedo;
            undoRedoManager.PostUndoRedo     += UndoRedoManager_PostUndoRedo;
            undoRedoManager.TransactionBegan += UndoRedoManager_PreUndoRedo;
            undoRedoManager.TransactionEnded += UndoRedoManager_PostUndoRedo;
            undoRedoManager.Updated += UndoRedoManager_Updated;

            ToolBar.Reset();
            ProjectExplorer.Reset();
            PianoRoll.Reset();
            Sequencer.Reset();

            PianoRoll.CurrentInstrument = ProjectExplorer.SelectedInstrument;
            PianoRoll.CurrentArpeggio   = ProjectExplorer.SelectedArpeggio;

            InvalidateEverything();
            UpdateTitle();
            RefreshSequencerLayout();
        }

        public void OpenProject(string filename)
        {
            StopEverything();

            if (!CheckUnloadProject())
            {
                return;
            }

            var dlgLog = new LogDialog(mainForm);
            using (var scopedLog = new ScopedLogOutput(dlgLog, LogSeverity.Warning))
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
                dlgLog.ShowDialogIfMessages();
            }
        }

        public void OpenProject()
        {
            var filename = PlatformUtils.ShowOpenFileDialog("Open File", "All Supported Files (*.fms;*.txt;*.nsf;*.nsfe;*.ftm)|*.fms;*.txt;*.nsf;*.nsfe;*.ftm|FamiStudio Files (*.fms)|*.fms|FamiTracker Files (*.ftm)|*.ftm|FamiTracker Text Export (*.txt)|*.txt|FamiStudio Text Export (*.txt)|*.txt|NES Sound Format (*.nsf;*.nsfe)|*.nsf;*.nsfe", ref Settings.LastFileFolder);
            if (filename != null)
            {
                OpenProject(filename);
            }
        }

        public Project OpenProjectFile(string filename, bool allowNsf = true)
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
            else if (allowNsf && (filename.ToLower().EndsWith("nsf") || filename.ToLower().EndsWith("nsfe")))
            {
                NsfImportDialog dlg = new NsfImportDialog(filename);

                if (dlg.ShowDialog(mainForm) == DialogResult.OK)
                {
                    project = new NsfFile().Load(filename, dlg.SongIndex, dlg.Duration, dlg.PatternLength, dlg.StartFrame, dlg.RemoveIntroSilence, dlg.ReverseDpcmBits);
                }
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
                    }
                }
            }
            else
            {
                success = new ProjectFile().Save(project, project.Filename);
            }

            if (success)
            {
                undoRedoManager.Clear();
            }
            else
            {
                PlatformUtils.MessageBox("An error happened while saving.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            InvalidateEverything();

            return true;
        }

        public void Export()
        {
            var dlgExp = new ExportDialog(project);
            dlgExp.ShowDialog(mainForm);
        }

        public void OpenConfigDialog()
        {
            var dlg = new ConfigDialog();
            var oldNumBufferedFrames = Settings.NumBufferedAudioFrames;

            // MATTT: Stop/restart audio players here (audio buffers might have changed).
            if (dlg.ShowDialog(mainForm) == DialogResult.OK)
            {
                if (oldNumBufferedFrames != Settings.NumBufferedAudioFrames)
                    RecreateAudioPlayers();

                InitializeMidi();
                InvalidateEverything(true);
            }
        }

        public bool TryClosing()
        {
            if (!CheckUnloadProject())
            {
                return false;
            }

            // Dont bother exiting cleanly on Linux.
            Midi.NotePlayed -= Midi_NotePlayed;
            Midi.Shutdown();

            StopEverything();
            ShutdownInstrumentPlayer();
            ShutdownSongPlayer();

            return true;
        }

        private void RefreshSequencerLayout()
        {
            mainForm.RefreshSequencerLayout();
            Sequencer.SequencerLayoutChanged();
        }

        private void CheckForNewRelease()
        {
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

                        // Assume > alphabetical order means newer version.
                        if (string.Compare(newReleaseString, Application.ProductVersion, StringComparison.OrdinalIgnoreCase) > 0)
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
            var dlg = new TransformDialog(this);

            if (dlg.ShowDialog(mainForm) == DialogResult.OK)
            {
                Sequencer.Reset();
                PianoRoll.Reset();
                ProjectExplorer.RefreshButtons();
                InvalidateEverything(true);
            }
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

            var version = Application.ProductVersion.Substring(0, Application.ProductVersion.LastIndexOf('.'));

            string title = $"FamiStudio {version} - {projectFile}";

#if FALSE
            title += " DEVELOPMENT VERSION DO NOT DISTRIBUTE!";
#endif

            mainForm.Text = title;
        }

        public void PlayInstrumentNote(int n, bool showWarning, bool allowRecording, bool custom = false, Instrument customInstrument = null, Arpeggio customArpeggio = null)
        {
            Note note = new Note(n);
            note.Volume = Note.VolumeMax;

            var instrument = custom ? customInstrument : ProjectExplorer.SelectedInstrument;
            var arpeggio   = custom ? customArpeggio   : ProjectExplorer.SelectedArpeggio;

            int channel = Sequencer.SelectedChannel;

            if (instrument == null)
            {
                channel = Channel.Dpcm;
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
                (ProjectExplorer.SelectedInstrument.HasReleaseEnvelope || ProjectExplorer.SelectedInstrument.ExpansionType == Project.ExpansionVrc7) &&
                song.Channels[Sequencer.SelectedChannel].SupportsInstrument(ProjectExplorer.SelectedInstrument))
            {
                instrumentPlayer.ReleaseNote(Sequencer.SelectedChannel);
            }
            else
            {
                instrumentPlayer.StopAllNotes();
            }

            if (allowRecording && recordingMode)
                PianoRoll.RecordNote(new Note(Note.NoteStop));
        }

        public void StopInstrument()
        {
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
            songPlayer = new SongPlayer(palPlayback);
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
            }
        }

        public void PreviewDPCMSample(DPCMSample sample, bool source)
        {
            previewDPCMSampleId = sample.Id;
            previewDPCMIsSource = source;

            byte[] dmcData = null;
            int    dmcRateIndex = 15;

            if (source)
            {
                if (sample.SourceDataIsWav)
                {
                    previewDPCMSampleRate = sample.SourceWavData.SampleRate;
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
                dmcData = sample.ProcessedData;
                dmcRateIndex = sample.PreviewRate;
            }

            previewDPCMSampleRate = (int)Math.Round(DPCMSample.DpcmSampleRatesNtsc[DPCMSample.DpcmSampleRatesNtsc.Length - 1]); // DPCMTODO : What about PAL?

            var playRate = (int)Math.Round(DPCMSample.DpcmSampleRatesNtsc[dmcRateIndex]); // DPCMTODO : What about PAL?
            WaveUtils.DpcmToWave(dmcData, 31, out short[] wave); // DPCMTODO Hardcoded 31 here.
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
                }

				palPlayback = value;
				if (project.UsesFamiTrackerTempo)
	                project.PalMode = value;

                if (playersWereValid)
                {
                    InitializeSongPlayer();
                    InitializeInstrumentPlayer();
                }

                InvalidateEverything();
            }
        }
        
        private bool PreventKeyRepeat(KeyEventArgs e, bool keyDown)
        {
            var keyCode = (int)e.KeyCode;

            if (keyCode < keyStates.Length && keyDown != keyStates[keyCode])
            {
                keyStates[keyCode] = keyDown;
                return true;
            }

            return false;
        }

        protected bool HandleRecordingKey(KeyEventArgs e, bool keyDown)
        {
            if (RecordingKeyToNoteMap.TryGetValue(e.KeyCode, out var noteValue))
            {
                if (!PreventKeyRepeat(e, keyDown))
                    return true;

                if (keyDown)
                {
                    if (noteValue < 0)
                    {
                        lastRecordingKeyDown = Keys.None;
                        StopOrReleaseIntrumentNote(true);
                    }
                    else
                    {
                        noteValue += Note.FromFriendlyName("C0") + (baseRecordingOctave * 12);
                        noteValue = Utils.Clamp(noteValue, Note.MusicalNoteMin, Note.MusicalNoteMax);

                        lastRecordingKeyDown = e.KeyCode;

                        PlayInstrumentNote(noteValue, true, true);
                    }
                }
                else if (e.KeyCode == lastRecordingKeyDown)
                {
                    lastRecordingKeyDown = Keys.None;
                    StopOrReleaseIntrumentNote(false);
                }

                return true;
            }

            return false;
        }

        public void KeyDown(KeyEventArgs e)
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

            if ((recordingMode || qwertyPiano) && !ctrl && !shift && HandleRecordingKey(e, true))
            {
                if (recordingMode)
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
                PianoRoll.ConditionalInvalidate();
            }
            else if (e.KeyCode == Keys.PageDown)
            {
                baseRecordingOctave = Math.Max(0, baseRecordingOctave - 1);
                PianoRoll.ConditionalInvalidate();
            }
            else if (e.KeyCode == Keys.Enter)
            {
                ToggleRecording();
            }
            else if (shift && e.KeyCode == Keys.F)
            {
                followMode = !followMode;
                ToolBar.Invalidate();
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
                        SeekSong(song.LoopPoint >= 0 && song.LoopPoint < song.Length ? song.GetPatternStartNote(song.LoopPoint) : 0);
                    if (ctrl)
                        SeekSong(song.GetPatternStartNote(song.FindPatternInstanceIndex(songPlayer.CurrentFrame, out _)));
                    else if (shift)
                        SeekSong(0);

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
            if (!recordingMode && e.KeyCode >= Keys.D1 && e.KeyCode <= Keys.D9)
            {
                if (ctrl)
                    GhostChannelMask ^= (1 << (int)(e.KeyCode - Keys.D1));
                else
                    Sequencer.SelectedChannel = (int)(e.KeyCode - Keys.D1);
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
#if FAMISTUDIO_WINDOWS
            else if (e.KeyData == Keys.Up    ||
                     e.KeyData == Keys.Down  ||
                     e.KeyData == Keys.Left  ||
                     e.KeyData == Keys.Right ||
                     e.KeyData == Keys.Escape)
            {
                PianoRoll.UnfocusedKeyDown(e);
                Sequencer.UnfocusedKeyDown(e);
            }
#endif
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
                Sequencer.Paste();
        }

        public void KeyUp(KeyEventArgs e)
        {
            bool ctrl  = e.Modifiers.HasFlag(Keys.Control);
            bool shift = e.Modifiers.HasFlag(Keys.Shift);

            if ((recordingMode || qwertyPiano) && !ctrl && !shift && HandleRecordingKey(e, false))
            {
                if (recordingMode)
                    return;
            }

#if FAMISTUDIO_WINDOWS
            if (!Sequencer.Focused) Sequencer.UnfocusedKeyUp(e);
#endif
        }

        private void Midi_NotePlayed(int n, bool on)
        {
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

        public void PlaySong()
        {
            StopRecording();

            if (!songPlayer.IsPlaying)
            {
                songPlayer.Play(song, songPlayer.CurrentFrame, palPlayback);
            }
        }

        public void StopSong()
        {
            if (songPlayer != null && songPlayer.IsPlaying)
            {
                songPlayer.Stop();

                // HACK: Update continuous follow mode only last time so it catches up to the 
                // real final player position.
                lastTickCurrentFrame = songPlayer.CurrentFrame;
                Sequencer.UpdateFollowMode(true);
                PianoRoll.UpdateFollowMode(true);
                lastTickCurrentFrame = -1;

                InvalidateEverything();
            }
        }

        public void StartRecording()
        {
            Debug.Assert(!recordingMode);
            StopSong();
            recordingMode = true;
            qwertyPiano = true;
            InvalidateEverything();
        }

        public void StopRecording()
        {
            if (recordingMode)
            {
                recordingMode = false;
                lastRecordingKeyDown = Keys.None;
                StopInstrument();
                InvalidateEverything();
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
                ToolBar.Invalidate();
                PianoRoll.Invalidate();
            }
        }

        public void SeekSong(int frame)
        {
            if (songPlayer != null)
            {
                bool wasPlaying = songPlayer.IsPlaying;
                if (wasPlaying) StopSong();
                songPlayer.CurrentFrame = Utils.Clamp(frame, 0, song.GetPatternStartNote(song.Length) - 1);
                if (wasPlaying) PlaySong();
                InvalidateEverything();
            }
        }

        public void SeekCurrentPattern()
        {
            if (songPlayer != null)
            {
                bool wasPlaying = songPlayer.IsPlaying;
                if (wasPlaying) StopSong();
                songPlayer.CurrentFrame = song.GetPatternStartNote(song.FindPatternInstanceIndex(songPlayer.CurrentFrame, out _));
                if (wasPlaying) PlaySong();
                InvalidateEverything();
            }
        }

        public int GhostChannelMask
        {
            get { return ghostChannelMask; }
            set
            {
                ghostChannelMask = value;
                Sequencer.ConditionalInvalidate();
                PianoRoll.ConditionalInvalidate();
            }
        }

        public int GetEnvelopeFrame(Instrument instrument, int envelopeIdx, bool force = false)
        {
            if (ProjectExplorer.SelectedInstrument == instrument || force)
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

        private void InvalidateEverything(bool projectExplorer = false)
        {
            ToolBar.Invalidate();
            Sequencer.Invalidate();
            PianoRoll.Invalidate();
            if (projectExplorer)
                ProjectExplorer.Invalidate();
        }

        private void RecreateAudioPlayers()
        {
            ShutdownSongPlayer();
            ShutdownInstrumentPlayer();
            InitializeSongPlayer();
            InitializeInstrumentPlayer();
        }

        private void ConditionalShowTutorial()
        {
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
                            Settings.ShowTutorial = false;
                    }
                }
            }
        }

        public void Tick()
        {
            lastTickCurrentFrame = IsPlaying ? songPlayer.CurrentFrame : -1;

            if (audioDeviceChanged)
            {
                RecreateAudioPlayers();
                audioDeviceChanged = false;
            }

            ToolBar.Tick();
            PianoRoll.Tick();
            Sequencer.Tick();

            if (RealTimeUpdate)
                InvalidateEverything();

            pianoRollScrollChanged = false;

            ConditionalShowTutorial();
            CheckNewReleaseDone();
        }

        private void sequencer_PatternClicked(int trackIndex, int patternIndex)
        {
            PianoRoll.StartEditPattern(trackIndex, patternIndex);
        }

        private void pianoRoll_PatternChanged(Pattern pattern)
        {
            Sequencer.NotifyPatternChange(pattern);
            Sequencer.Invalidate();
        }

        private void PianoRoll_ManyPatternChanged()
        {
            Sequencer.InvalidatePatternCache();
            Sequencer.Invalidate();
        }

        private void PianoRoll_DPCMSampleChanged()
        {
            ProjectExplorer.ConditionalInvalidate();
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

        private void ProjectExplorer_DPCMSampleEdited(DPCMSample sample)
        {
            PianoRoll.StartEditDPCMSample(sample);
        }

        private void pianoRoll_EnvelopeChanged()
        {
            ProjectExplorer.Invalidate();
        }

        public void SerializeState(ProjectBuffer buffer)
        {
            buffer.Serialize(ref ghostChannelMask);
            buffer.Serialize(ref song);

            var currentFrame = CurrentFrame;
            buffer.Serialize(ref currentFrame);

            ProjectExplorer.SerializeState(buffer);
            Sequencer.SerializeState(buffer);
            PianoRoll.SerializeState(buffer);

            if (buffer.IsReading)
            {
                // Move seek bar on undo/redo when recording.
                if (recordingMode)
                    SeekSong(currentFrame);

                RefreshSequencerLayout();
                mainForm.Invalidate();
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
            PianoRoll.ConditionalInvalidate();
        }

        private void ProjectExplorer_ArpeggioColorChanged(Arpeggio arpeggio)
        {
            PianoRoll.ConditionalInvalidate();
        }

        private void ProjectExplorer_DPCMSampleColorChanged(DPCMSample sample)
        {
            Sequencer.InvalidatePatternCache();
            PianoRoll.ConditionalInvalidate();
        }

        private void projectExplorer_SongModified(Song song)
        {
            Sequencer.SongModified();
            PianoRoll.SongModified();
            InvalidateEverything();
        }

        private void projectExplorer_SongSelected(Song song)
        {
            StopSong();
            SeekSong(0);
            this.song = song;

            PianoRoll.SongChanged();
            Sequencer.Reset();
            ToolBar.Reset();

            InvalidateEverything();
        }

        private void projectExplorer_InstrumentReplaced(Instrument instrument)
        {
            Sequencer.InvalidatePatternCache();
            InvalidateEverything();
        }
    }
}
