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
        public static Project StaticProject { get; private set; }

        private FamiStudioForm mainForm;
        private Project project;
        private Song song;
        private SongPlayer songPlayer;
        private InstrumentPlayer instrumentPlayer;
        private Midi midi;
        private UndoRedoManager undoRedoManager;
        private int ghostChannelMask = 0;
        private int lastMidiNote = -1;

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
            PianoRoll.PatternChanged += pianoRoll_PatternChanged;
            PianoRoll.EnvelopeResized += pianoRoll_EnvelopeResized;
            PianoRoll.ControlActivated += PianoRoll_ControlActivated;
            ProjectExplorer.InstrumentEdited += projectExplorer_InstrumentEdited;
            ProjectExplorer.InstrumentSelected += projectExplorer_InstrumentSelected;
            ProjectExplorer.InstrumentColorChanged += projectExplorer_InstrumentColorChanged;
            ProjectExplorer.InstrumentReplaced += projectExplorer_InstrumentReplaced;
            ProjectExplorer.InstrumentDraggedOutside += ProjectExplorer_InstrumentDraggedOutside;
            ProjectExplorer.SongModified += projectExplorer_SongModified;
            ProjectExplorer.SongSelected += projectExplorer_SongSelected;

            songPlayer = new SongPlayer();
            songPlayer.Initialize();
            instrumentPlayer = new InstrumentPlayer();
            instrumentPlayer.Initialize();

            InitializeMidi();

            if (!string.IsNullOrEmpty(filename))
            {
                OpenProject(filename);
            }
            else
            {
                NewProject();
            }

            if (Settings.CheckUpdates)
            {
                Task.Factory.StartNew(CheckForNewRelease);
            }
        }

        private void ProjectExplorer_InstrumentDraggedOutside(Instrument instrument, Point pos)
        {
            var pianoRollClientPos = PianoRoll.PointToClient(pos);

            if (PianoRoll.ClientRectangle.Contains(pianoRollClientPos))
            {
                PianoRoll.ReplaceSelectionInstrument(instrument);
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

        public void InitializeMidi()
        {
            if (midi != null)
            {
                midi.Close();
                midi = null;
            }

            if (Midi.InputCount > 0)
            {
                midi = new Midi();

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

                if (midi.Open(midiDeviceIndex) && midi.Start())
                    midi.NotePlayed += Midi_NotePlayed;
                else
                    midi = null;
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

            songPlayer.CurrentFrame = 0;
            ToolBar.Reset();
            ProjectExplorer.Reset();
            PianoRoll.Reset();
            Sequencer.Reset();
            PianoRoll.CurrentInstrument = ProjectExplorer.SelectedInstrument;
            InvalidateEverything();
            UpdateTitle();
            ClipboardUtils.Reset();
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
                            // Make sure this release applies to our platform (eg. a hotfix for macos should not impact Windows).
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

                if (PlatformDialogs.MessageBox($"A new version ({newReleaseString}) is available. Do you want to download it?", "New Version", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    Process.Start("http://www.famistudio.org");
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

        public void PlayInstrumentNote(int n)
        {
            Note note = new Note();
            note.Value = (byte)n;
            note.Volume = Note.VolumeMax;

            int channel = Sequencer.SelectedChannel;
            if (ProjectExplorer.SelectedInstrument == null)
                channel = Channel.DPCM;
            else
                note.Instrument = ProjectExplorer.SelectedInstrument;

            instrumentPlayer.PlayNote(channel, note);
        }

        public void StopOrReleaseIntrumentNote()
        {
            if (ProjectExplorer.SelectedInstrument != null && 
                ProjectExplorer.SelectedInstrument.HasReleaseEnvelope)
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
                    {
                        LoopMode = LoopMode.Pattern;
                        Seek(songPlayer.CurrentFrame / song.PatternLength * song.PatternLength);
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
                // In MIDI: 60 = C4, In Famitone 37 = C4, but in 1.2.0 we extended the range, so C4 is now 49.
                PlayInstrumentNote(Math.Max(1, Math.Min(n - 11, 63)));
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
                songPlayer.Play(song, songPlayer.CurrentFrame);
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
            songPlayer.CurrentFrame = Math.Min(frame, song.Length * song.PatternLength - 1);
            if (wasPlaying) Play();
            InvalidateEverything();
        }

        public void SeekCurrentPattern()
        {
            bool wasPlaying = songPlayer.IsPlaying;
            if (wasPlaying) Stop();
            songPlayer.CurrentFrame = songPlayer.CurrentFrame - (songPlayer.CurrentFrame % song.PatternLength);
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
