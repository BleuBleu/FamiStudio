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
        private SongPlayer songPlayer = new SongPlayer();
        private InstrumentPlayer instrumentPlayer = new InstrumentPlayer();
        private UndoRedoManager undoRedoManager;
        private int ghostChannelMask = 0;
        private int lastMidiNote = -1;
        private bool palMode = false;
        private bool audioDeviceChanged = false;
#if FAMISTUDIO_WINDOWS
        private MultiMediaNotificationListener mmNoticiations;
#endif

        private bool newReleaseAvailable = false;
        private string newReleaseString = null;
        private string newReleaseUrl = null;

        public bool RealTimeUpdate => songPlayer.IsPlaying || PianoRoll.IsEditingInstrument;
        public bool IsPlaying => songPlayer.IsPlaying;
        public int CurrentFrame => songPlayer.CurrentFrame;
        public int ChannelMask { get => songPlayer.ChannelMask; set => songPlayer.ChannelMask = value; }
        public string ToolTip { get => ToolBar.ToolTip; set => ToolBar.ToolTip = value; }
        public Project Project => project;
        public Song Song => song;
        public UndoRedoManager UndoRedoManager => undoRedoManager;
        public LoopMode LoopMode { get => songPlayer.Loop; set => songPlayer.Loop = value; }

        public Toolbar ToolBar => mainForm.ToolBar;
        public Sequencer Sequencer => mainForm.Sequencer;
        public PianoRoll PianoRoll => mainForm.PianoRoll;
        public ProjectExplorer ProjectExplorer => mainForm.ProjectExplorer;
        public Rectangle MainWindowBounds => mainForm.Bounds;

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
            PianoRoll.EnvelopeChanged += pianoRoll_EnvelopeChanged;
            PianoRoll.ControlActivated += PianoRoll_ControlActivated;
            PianoRoll.NotesPasted += PianoRoll_NotesPasted;
            ProjectExplorer.InstrumentEdited += projectExplorer_InstrumentEdited;
            ProjectExplorer.InstrumentSelected += projectExplorer_InstrumentSelected;
            ProjectExplorer.InstrumentColorChanged += projectExplorer_InstrumentColorChanged;
            ProjectExplorer.InstrumentReplaced += projectExplorer_InstrumentReplaced;
            ProjectExplorer.InstrumentDeleted += ProjectExplorer_InstrumentDeleted;
            ProjectExplorer.InstrumentDraggedOutside += ProjectExplorer_InstrumentDraggedOutside;
            ProjectExplorer.SongModified += projectExplorer_SongModified;
            ProjectExplorer.SongSelected += projectExplorer_SongSelected;
            ProjectExplorer.ProjectModified += ProjectExplorer_ProjectModified;

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
        
        private void ProjectExplorer_ProjectModified()
        {
            if (Project.ExpansionAudio != Project.ExpansionNone)
                palMode = false;

            RefreshSequencerLayout();
            Sequencer.Reset();
            PianoRoll.Reset();
            PianoRoll.CurrentInstrument = ProjectExplorer.SelectedInstrument;
        }

        private void ProjectExplorer_InstrumentDeleted(Instrument instrument)
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
            StopIntrumentNote();
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
            instrumentPlayer.Stop();

            StaticProject = project;
            song = project.Songs[0];
            palMode = false;

            undoRedoManager = new UndoRedoManager(project, this);
            undoRedoManager.Updated += UndoRedoManager_Updated;

            songPlayer.CurrentFrame = 0;
            ToolBar.Reset();
            ProjectExplorer.Reset();
            PianoRoll.Reset();
            Sequencer.Reset();
            PianoRoll.CurrentInstrument = ProjectExplorer.SelectedInstrument;
            InvalidateEverything();
            UpdateTitle();
            RefreshSequencerLayout();

            instrumentPlayer.Start(project, palMode);
        }

        public void OpenProject(string filename)
        {
            StopEverything();

            if (!CheckUnloadProject())
            {
                return;
            }

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
                project = new FamistudioTextFile().Load(filename);

                if (project == null)
                    project = new FamitrackerTextFile().Load(filename);
            }
            else if (filename.ToLower().EndsWith("nsf") || filename.ToLower().EndsWith("nsfe"))
            {
                NsfImportDialog dlg = new NsfImportDialog(filename, mainForm.Bounds);

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    project = new NsfFile().Load(filename, dlg.SongIndex, dlg.Duration, dlg.PatternLength, dlg.StartFrame, dlg.RemoveIntroSilence);
                }
            }

            if (project != null)
            {
                InitProject();

                //FamistudioTextFile.Save(project, "d:\\toto.txt");
            }
            else
            {
                NewProject();
            }
        }

        public void OpenProject()
        {
            var filename = PlatformUtils.ShowOpenFileDialog("Open File", "All Supported Files (*.fms;*.txt;*.nsf;*.nsfe;*.ftm)|*.fms;*.txt;*.nsf;*.nsfe;*.ftm|FamiStudio Files (*.fms)|*.fms|FamiTracker Files (*.ftm)|*.ftm|FamiTracker Text Export (*.txt)|*.txt|FamiStudio Text Export (*.txt)|*.txt|NES Sound Format (*.nsf;*.nsfe)|*.nsf;*.nsfe");
            if (filename != null)
            {
                OpenProject(filename);
            }
        }

        public bool SaveProject(bool forceSaveAs = false)
        {
            bool success = true;

            if (forceSaveAs || string.IsNullOrEmpty(project.Filename))
            {
                string filename = PlatformUtils.ShowSaveFileDialog("Save File", "FamiStudio Files (*.fms)|*.fms");
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
            var dlg = new ExportDialog(mainForm.Bounds, project);
            dlg.ShowDialog();
        }

        public void OpenConfigDialog()
        {
            var dlg = new ConfigDialog(mainForm.Bounds);

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                InitializeMidi();
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
            songPlayer.Shutdown();
            instrumentPlayer.Shutdown();

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

        private void OpenUrl(string url)
        {
            try
            {
#if FAMISTUDIO_LINUX
                Process.Start("xdg-open", url);
#elif FAMISTUDIO_MACOS
                Process.Start("open", url);
#else
                Process.Start(url);
#endif
            }
            catch { }
        }

        private void CheckNewReleaseDone()
        {
            if (newReleaseAvailable)
            {
                newReleaseAvailable = false;

                if (PlatformUtils.MessageBox($"A new version ({newReleaseString}) is available. Do you want to download it?", "New Version", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    OpenUrl("http://www.famistudio.org");
                }
            }
        }

        public void OpenTransformDialog()
        {
            var dlg = new TransformDialog(mainForm.Bounds, this);

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                Sequencer.Reset();
                PianoRoll.Reset();
                ProjectExplorer.RefreshButtons();
                InvalidateEverything(true);
            }
        }

        public void ShowHelp()
        {
            OpenUrl("http://www.famistudio.org/doc/index.html");
        }

        private void UpdateTitle()
        {
            string projectFile = "New Project";

            if (!string.IsNullOrEmpty(project.Filename))
                projectFile = System.IO.Path.GetFileName(project.Filename);

            var version = Application.ProductVersion.Substring(0, Application.ProductVersion.LastIndexOf('.'));

            string title = $"FamiStudio {version} - {projectFile}";

#if TRUE
            title += " DEVELOPMENT VERSION DO NOT DISTRIBUTE!";
#endif

            mainForm.Text = title;
        }

        public void PlayInstrumentNote(int n)
        {
            Note note = new Note(n);
            note.Volume = Note.VolumeMax;

            int channel = Sequencer.SelectedChannel;
            if (ProjectExplorer.SelectedInstrument == null)
            {
                channel = Channel.Dpcm;
            }
            else
            {
                if (song.Channels[channel].SupportsInstrument(ProjectExplorer.SelectedInstrument))
                {
                    note.Instrument = ProjectExplorer.SelectedInstrument;
                }
                else
                {
                    DisplayWarning("Selected instrument is incompatible with channel!", false);
                    return;
                }
            }

            instrumentPlayer.PlayNote(channel, note);
        }

        public void StopOrReleaseIntrumentNote()
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
        }

        public void ReleaseInstrumentNote()
        {
            instrumentPlayer.ReleaseNote(Sequencer.SelectedChannel);
        }

        public void StopIntrumentNote()
        {
            instrumentPlayer.StopAllNotes();
        }

        public void StopEverything(bool stopNotes = true)
        {
            Stop();
            StopInstrumentPlayer(stopNotes);
        }

        public void StopInstrumentPlayer(bool stopNotes = true)
        {
            instrumentPlayer.Stop(stopNotes);
        }

        public void StartInstrumentPlayer()
        {
            instrumentPlayer.Start(project, palMode);
        }

        public bool PalMode
        {
            get
            {
                return palMode;
            }
            set
            {
                Stop();
                StopInstrumentPlayer();
                palMode = value;
                StartInstrumentPlayer();
            }
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
                StopIntrumentNote();
            }

            if (e.KeyCode == Keys.Space)
            {
                if (IsPlaying)
                {
                    Stop();
                }
                else
                {
                    if (ctrl)
                        Seek(song.GetPatternStartNote(song.FindPatternInstanceIndex(songPlayer.CurrentFrame, out _)));
                    else if (shift)
                        Seek(0);

                    Play();
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
                    Seek(0);
                }
            }
            if (e.KeyCode >= Keys.D1 && e.KeyCode <= Keys.D9)
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
#if FAMISTUDIO_WINDOWS
            if (!Sequencer.Focused) Sequencer.UnfocusedKeyUp(e);
#endif
        }

        private void Midi_NotePlayed(int n, bool on)
        {
            if (on)
            {
                PlayInstrumentNote(Utils.Clamp(n - 11, Note.MusicalNoteMin, Note.MusicalNoteMax));
                lastMidiNote = n;
            }
            else if (n == lastMidiNote)
            {
                StopOrReleaseIntrumentNote();
                lastMidiNote = -1;
            }
        }

        public void Play()
        {
            if (!songPlayer.IsPlaying)
            {
                songPlayer.Play(song, songPlayer.CurrentFrame, palMode);
            }
        }

        public void Stop()
        {
            if (songPlayer.IsPlaying)
            {
                songPlayer.Stop();
                InvalidateEverything();
            }
        }

        public void Seek(int frame)
        {
            bool wasPlaying = songPlayer.IsPlaying;
            if (wasPlaying) Stop();
            songPlayer.CurrentFrame = Math.Min(frame, song.GetPatternStartNote(song.Length) - 1);
            if (wasPlaying) Play();
            InvalidateEverything();
        }

        public void SeekCurrentPattern()
        {
            bool wasPlaying = songPlayer.IsPlaying;
            if (wasPlaying) Stop();
            songPlayer.CurrentFrame = song.GetPatternStartNote(song.FindPatternInstanceIndex(songPlayer.CurrentFrame, out _));
            if (wasPlaying) Play();
            InvalidateEverything();
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

        public int GetEnvelopeFrame(Instrument instrument, int envelopeIdx)
        {
            if (ProjectExplorer.SelectedInstrument == instrument)
                return instrumentPlayer.GetEnvelopeFrame(envelopeIdx);
            else
                return -1;
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
            StopEverything(false);

            songPlayer.Shutdown();
            songPlayer = new SongPlayer();
            instrumentPlayer.Shutdown();
            instrumentPlayer = new InstrumentPlayer();

            StartInstrumentPlayer();
        }

        public void Tick()
        {
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

        private void projectExplorer_InstrumentEdited(Instrument instrument, int envelope)
        {
            if (instrument == null)
            {
                PianoRoll.StartEditDPCMSamples();
            }
            else
            {
                PianoRoll.StartEditEnveloppe(instrument, envelope);
            }
        }

        private void pianoRoll_EnvelopeChanged()
        {
            ProjectExplorer.Invalidate();
        }

        public void SerializeState(ProjectBuffer buffer)
        {
            buffer.Serialize(ref ghostChannelMask);
            buffer.Serialize(ref song);

            ProjectExplorer.SerializeState(buffer);
            Sequencer.SerializeState(buffer);
            PianoRoll.SerializeState(buffer);

            if (buffer.IsReading)
            {
                RefreshSequencerLayout();
                mainForm.Invalidate();
            }
        }

        private void projectExplorer_InstrumentSelected(Instrument instrument)
        {
            PianoRoll.CurrentInstrument = instrument;
        }

        private void projectExplorer_InstrumentColorChanged(Instrument instrument)
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
            Stop();
            Seek(0);
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
