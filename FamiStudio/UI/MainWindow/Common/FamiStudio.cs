using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public static Project StaticProject { get; private set; }

        private FamiStudioForm mainForm;
        private Project project;
        private Song song;
        private SongPlayer songPlayer;
        private InstrumentPlayer instrumentPlayer;
        private Midi midi;
        private UndoRedoManager undoRedoManager;
        private int currentFrame = 0;
        private int ghostChannelMask = 0;

        private bool newReleaseAvailable = false;
        private string newReleaseString = null;
        private string newReleaseUrl = null;

        public bool RealTimeUpdate => songPlayer.IsPlaying || PianoRoll.IsEditingInstrument;
        public bool IsPlaying => songPlayer.IsPlaying;
        public int CurrentFrame => currentFrame;
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

        public FamiStudio(string filename)
        {
            mainForm = new FamiStudioForm(this);

            Sequencer.PatternClicked += new Sequencer.PatternDoubleClick(sequencer_PatternClicked);
            PianoRoll.PatternChanged += new PianoRoll.PatternChange(pianoRoll_PatternChanged);
            PianoRoll.EnvelopeResized += new PianoRoll.EnvelopeResize(pianoRoll_EnvelopeResized);
            ProjectExplorer.InstrumentEdited += new ProjectExplorer.InstrumentEnvelopeDelegate(projectExplorer_InstrumentEdited);
            ProjectExplorer.InstrumentSelected += new ProjectExplorer.InstrumentDelegate(projectExplorer_InstrumentSelected);
            ProjectExplorer.InstrumentColorChanged += new ProjectExplorer.InstrumentDelegate(projectExplorer_InstrumentColorChanged);
            ProjectExplorer.InstrumentReplaced += new ProjectExplorer.InstrumentDelegate(projectExplorer_InstrumentReplaced);
            ProjectExplorer.SongModified += new ProjectExplorer.SongDelegate(projectExplorer_SongModified);
            ProjectExplorer.SongSelected += new ProjectExplorer.SongDelegate(projectExplorer_SongSelected);

            songPlayer = new SongPlayer();
            songPlayer.Initialize();
            instrumentPlayer = new InstrumentPlayer();
            instrumentPlayer.Initialize();

            if (Midi.InputCount > 0)
            {
                midi = new Midi();

                if (midi.Open(0) && midi.Start())
                    midi.NotePlayed += Midi_NotePlayed;
                else
                    midi = null;
            }

            if (!string.IsNullOrEmpty(filename))
            {
                OpenProject(filename);
            }
            else
            {
                NewProject();
            }

            Task.Factory.StartNew(CheckForNewRelease);
        }

        public void Run()
        {
            mainForm.Run();
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
                    var result = PlatformDialogs.MessageBox("Save changes?", "FamiStudio", MessageBoxButtons.YesNoCancel);
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
            StaticProject = project;
            song = project.Songs[0];

            undoRedoManager = new UndoRedoManager(project, this);
            undoRedoManager.Updated += UndoRedoManager_Updated;

            currentFrame = 0;
            ToolBar.Reset();
            ProjectExplorer.Reset();
            PianoRoll.Reset();
            Sequencer.Reset();
            PianoRoll.CurrentInstrument = ProjectExplorer.SelectedInstrument;
            InvalidateEverything();
            UpdateTitle();
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
                project = ProjectFile.Load(filename);
            }
            else if (filename.ToLower().EndsWith("txt"))
            {
                project = FamitrackerFile.Load(filename);
            }

            if (project != null)
            {
                InitProject();
            }
            else
            {
                NewProject();
            }
        }

        public void OpenProject()
        {
            var filename = PlatformDialogs.ShowOpenFileDialog("Open File", "All Supported Files (*.fms;*.txt)|*.fms;*.txt|FamiStudio Files (*.fms)|*.fms|Famitracker Text Export (*.txt)|*.txt");
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
                string filename = PlatformDialogs.ShowSaveFileDialog("Save File", "FamiStudio Files (*.fms)|*.fms");
                if (filename != null)
                {
                    success = ProjectFile.Save(project, filename);
                    if (success)
                    {
                        UpdateTitle();
                    }
                }
            }
            else
            {
                success = ProjectFile.Save(project, project.Filename);
            }

            if (success)
            {
                undoRedoManager.Clear();
            }
            else
            {
                PlatformDialogs.MessageBox("An error happened while saving.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            InvalidateEverything();

            return true;
        }

        public void Export()
        {
            var dlg = new ExportDialog(mainForm.Bounds, project);
            dlg.ShowDialog();
        }

        public bool TryClosing()
        {
            if (!CheckUnloadProject())
            {
                return false;
            }

            if (midi != null)
            {
                midi.Stop();
                midi.Close();
                midi = null;
            }

            songPlayer.Shutdown();
            instrumentPlayer.Shutdown();

            return true;
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
                            // Make sure this release applies to our platform (eg. a hotfix for macos should impact Windows).
                            var assets = release["assets"];
                            foreach (var asset in assets)
                            {
                                var name = (string)asset["name"];
#if FAMISTUDIO_WINDOWS
                                if (name != null && name.ToLower().Contains(".msi"))
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

                if (PlatformDialogs.MessageBox($"A new release ({newReleaseString}) is available. Do you want to go to GitHub to download it?", "New Version", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    Process.Start(newReleaseUrl);
                }
            }
        }

        private void UpdateTitle()
        {
            string projectFile = "New Project";

            if (!string.IsNullOrEmpty(project.Filename))
                projectFile = project.Filename;

            var version = Application.ProductVersion.Substring(0, Application.ProductVersion.LastIndexOf('.'));

            mainForm.Text = $"FamiStudio {version} - {projectFile}";
        }

        public void PlayInstrumentNote(int n, bool on)
        {
            Note note = new Note();
            note.Value = (byte)n;

            int channel = Sequencer.SelectedChannel;
            if (ProjectExplorer.SelectedInstrument == null)
                channel = 4;
            else
                note.Instrument = ProjectExplorer.SelectedInstrument;

            instrumentPlayer.PlayNote(channel, note);
        }

        public void StopIntrumentNote()
        {
            instrumentPlayer.StopAllNotes();
        }

        public void StopInstrumentNoteAndWait()
        {
            instrumentPlayer.StopAllNotesAndWait();
        }

        public void StopEverything()
        {
            Stop();
            StopInstrumentNoteAndWait();
        }

        public void KeyDown(KeyEventArgs e)
        {
            bool ctrl  = e.Modifiers.HasFlag(Keys.Control);
            bool shift = e.Modifiers.HasFlag(Keys.Shift);

            if (e.KeyCode == Keys.Space)
            {
                if (IsPlaying)
                {
                    Stop();
                }
                else
                {
                    if (ctrl)
                    {
                        LoopMode = LoopMode.Pattern;
                        Seek(currentFrame / song.PatternLength * song.PatternLength);
                    }
                    else if (shift)
                    {
                        LoopMode = LoopMode.Song;
                    }

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
            else if (ctrl && e.KeyCode == Keys.Z)
            {
                undoRedoManager.Undo();
            }
            else if (ctrl && e.KeyCode == Keys.Y)
            {
                undoRedoManager.Redo();
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
        }

        private void Midi_NotePlayed(int n, bool on)
        {
            if (on)
            {
                // In MIDI: 60 = C4, In Famitone 37 = C4
                PlayInstrumentNote(Math.Max(1, Math.Min(n - 23, 63)), on);
            }
            else
            {
                //StopIntrumentNote();
            }
        }

        public void Play()
        {
            if (!songPlayer.IsPlaying)
            {
                songPlayer.Play(song, currentFrame);
            }
        }

        public void Stop()
        {
            if (songPlayer.IsPlaying)
            {
                songPlayer.Stop();
                currentFrame = songPlayer.CurrentFrame;
                InvalidateEverything();
            }
        }

        public void Seek(int frame)
        {
            bool wasPlaying = songPlayer.IsPlaying;
            if (wasPlaying) Stop();
            currentFrame = Math.Min(frame, song.Length * song.PatternLength - 1);
            if (wasPlaying) Play();
            InvalidateEverything();
        }

        public void SeekCurrentPattern()
        {
            bool wasPlaying = songPlayer.IsPlaying;
            if (wasPlaying) Stop();
            currentFrame = currentFrame - (currentFrame % song.PatternLength);
            if (wasPlaying) Play();
            InvalidateEverything();
        }

        public int GhostChannelMask
        {
            get { return ghostChannelMask; }
            set
            {
                ghostChannelMask = value;
                PianoRoll.ConditionalInvalidate();
            }
        }

        public int GetEnvelopeFrame(int envelopeIdx)
        {
            return instrumentPlayer.GetEnvelopeFrame(envelopeIdx);
        }

        private void InvalidateEverything()
        {
            ToolBar.Invalidate();
            Sequencer.Invalidate();
            PianoRoll.Invalidate();
            //projectExplorer.Invalidate();
        }

        public void Tick()
        {
            if (RealTimeUpdate)
            {
                songPlayer.CheckIfEnded();
                if (songPlayer.IsPlaying)
                    currentFrame = songPlayer.CurrentFrame;
                InvalidateEverything();
            }

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

        private void pianoRoll_EnvelopeResized()
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
                mainForm.Invalidate();
        }

        private void projectExplorer_InstrumentSelected(Instrument instrument)
        {
            PianoRoll.CurrentInstrument = instrument;
        }

        private void projectExplorer_InstrumentColorChanged(Instrument instrument)
        {
            Sequencer.InvalidatePatternCache();
        }

        private void projectExplorer_SongModified(Song song)
        {
            Sequencer.InvalidatePatternCache();
            PianoRoll.ClampScroll();
            InvalidateEverything();
        }

        private void projectExplorer_SongSelected(Song song)
        {
            Stop();
            this.currentFrame = 0;
            this.song = song;
            Sequencer.InvalidatePatternCache();
            PianoRoll.ClampScroll();
            InvalidateEverything();
        }

        private void projectExplorer_InstrumentReplaced(Instrument instrument)
        {
            Sequencer.InvalidatePatternCache();
            InvalidateEverything();
        }
    }
}
