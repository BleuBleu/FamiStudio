using System;
using System.Collections.Generic;
using System.Media;
using System.Windows.Forms;
using System.Diagnostics;

using RenderBitmap      = FamiStudio.GLBitmap;
using RenderBitmapAtlas = FamiStudio.GLBitmapAtlas;
using RenderBrush       = FamiStudio.GLBrush;
using RenderGeometry    = FamiStudio.GLGeometry;
using RenderControl     = FamiStudio.GLControl;
using RenderGraphics    = FamiStudio.GLGraphics;
using RenderTheme       = FamiStudio.GLTheme;
using RenderCommandList = FamiStudio.GLCommandList;
using RenderTransform   = FamiStudio.GLTransform;

namespace FamiStudio
{
    public partial class Toolbar : RenderControl
    {
        enum ButtonType
        {
            New,
            Open,
            Save,
            Export,
            Copy,
            Cut,
            Paste,
            Undo,
            Redo,
            Transform,
            Config,
            Play,
            Rec,
            Rewind,
            Loop,
            Qwerty,
            Metronome,
            Machine,
            Follow,
            Help,
            Sequencer, // Mobile only
            PianoRoll, // Mobile only
            Project, // Mobile only
            Count
        }

        enum ButtonStatus
        {
            Enabled,
            Disabled,
            Dimmed
        }

        enum ButtonCategory
        {
            Files,
            UndoRedo,
            CopyPaste,
            Playback,
            Misc,
            RowCount,
            Navigation
        }

        enum ButtonImageIndices
        { 
            LoopNone,
            Loop,
            LoopPattern,
            LoopSelection,
            Play,
            PlayHalf,
            PlayQuarter,
            Pause,
            NTSC,
            PAL,
            NTSCToPAL,
            PALToNTSC,
            Rec,
            RecRed,
            Metronome,
            File,
            Open,
            Save,
            Export,
            Copy,
            Cut,
            Paste,
            Undo,
            Redo,
            Transform,
            Config,
            Rewind,
            QwertyPiano,
            Follow,
            Help,
            Sequencer,
            PianoRoll,
            ProjectExplorer,
            ExpandToolbar,
            Count
        };

        readonly string[] ButtonImageNames = new string[]
        {
            "LoopNone",
            "Loop",
            "LoopPattern",
            "LoopSelection",
            "Play",
            "PlayHalf",
            "PlayQuarter",
            "Pause",
            "NTSC",
            "PAL",
            "NTSCToPAL",
            "PALToNTSC",
            "Rec",
            "RecRed",
            "Metronome",
            "File",
            "Open",
            "Save",
            "Export",
            "Copy",
            "Cut",
            "Paste",
            "Undo",
            "Redo",
            "Transform",
            "Config",
            "Rewind",
            "QwertyPiano",
            "Follow",
            "Help",
            "Sequencer",
            "PianoRoll",
            "ProjectExplorer",
            "ExpandToolbar"
        };

        private delegate void MouseWheelDelegate(int delta);
        private delegate void EmptyDelegate();
        private delegate ButtonStatus ButtonStatusDelegate();
        private delegate ButtonImageIndices BitmapDelegate();

        // DROIDTODO : Have a separate position + hitbox.
        class Button
        {
            public int X;
            public int Y;
            public bool RightAligned;
            public bool Visible = true;
            public bool Important = false; // For mobile, visible when toolbar is compact.
            public int Size;
            public ButtonCategory Category;
            public string ToolTip;
            public ButtonImageIndices BmpAtlasIndex;
            public ButtonStatusDelegate Enabled;
            public EmptyDelegate Click;
            public EmptyDelegate RightClick;
            public MouseWheelDelegate MouseWheel;
            public BitmapDelegate GetBitmap;
            public bool IsPointIn(int px, int py, int width)
            {
                int x = RightAligned ? width - X : X;
                return px >= x && (px - x) < Size && py >= Y && (py - Y) < Size;
            }
        };

        RenderTheme theme;
        RenderBrush toolbarBrush;
        RenderBrush warningBrush;
        RenderBitmapAtlas bmpButtonAtlas;
        Button[] buttons = new Button[(int)ButtonType.Count];

        bool oscilloscopeVisible = true;
        bool lastOscilloscopeHadNonZeroSample = false;

        float buttonBitmapScaleFloat = 1.0f;

        protected void OnRenderInitializedCommon(RenderGraphics g)
        {
            Debug.Assert((int)ButtonImageIndices.Count == ButtonImageNames.Length);

            theme = RenderTheme.CreateResourcesForGraphics(g);
            toolbarBrush = g.CreateVerticalGradientBrush(0, Height, ThemeBase.DarkGreyFillColor2, ThemeBase.DarkGreyFillColor1); // DROIDTODO : Makes no sense on mobile.
            warningBrush = g.CreateSolidBrush(System.Drawing.Color.FromArgb(205, 77, 64));

            bmpButtonAtlas = g.CreateBitmapAtlasFromResources(ButtonImageNames);

            buttons[(int)ButtonType.New]       = new Button { Category = ButtonCategory.Files, BmpAtlasIndex = ButtonImageIndices.File, Click = OnNew };
            buttons[(int)ButtonType.Open]      = new Button { Category = ButtonCategory.Files, BmpAtlasIndex = ButtonImageIndices.Open, Click = OnOpen, Important = true };
            buttons[(int)ButtonType.Save]      = new Button { Category = ButtonCategory.Files, BmpAtlasIndex = ButtonImageIndices.Save, Click = OnSave, RightClick = OnSaveAs, Important = true };
            buttons[(int)ButtonType.Export]    = new Button { Category = ButtonCategory.Files, BmpAtlasIndex = ButtonImageIndices.Export, Click = OnExport, RightClick = OnRepeatLastExport };
            buttons[(int)ButtonType.Copy]      = new Button { Category = ButtonCategory.CopyPaste, BmpAtlasIndex = ButtonImageIndices.Copy, Click = OnCopy, Enabled = OnCopyEnabled, Important = true };
            buttons[(int)ButtonType.Cut]       = new Button { Category = ButtonCategory.CopyPaste, BmpAtlasIndex = ButtonImageIndices.Cut, Click = OnCut, Enabled = OnCutEnabled };
            buttons[(int)ButtonType.Paste]     = new Button { Category = ButtonCategory.CopyPaste, BmpAtlasIndex = ButtonImageIndices.Paste, Click = OnPaste, RightClick = OnPasteSpecial, Enabled = OnPasteEnabled, Important = true };
            buttons[(int)ButtonType.Undo]      = new Button { Category = ButtonCategory.UndoRedo, BmpAtlasIndex = ButtonImageIndices.Undo, Click = OnUndo, Enabled = OnUndoEnabled, Important = true };
            buttons[(int)ButtonType.Redo]      = new Button { Category = ButtonCategory.UndoRedo, BmpAtlasIndex = ButtonImageIndices.Redo, Click = OnRedo, Enabled = OnRedoEnabled, Important = true };
            buttons[(int)ButtonType.Transform] = new Button { Category = ButtonCategory.Misc, BmpAtlasIndex = ButtonImageIndices.Transform, Click = OnTransform };
            buttons[(int)ButtonType.Config]    = new Button { Category = ButtonCategory.Misc, BmpAtlasIndex = ButtonImageIndices.Config, Click = OnConfig, Important = true };
            buttons[(int)ButtonType.Play]      = new Button { Category = ButtonCategory.Playback, Click = OnPlay, MouseWheel = OnPlayMouseWheel, GetBitmap = OnPlayGetBitmap, Important = true };
            buttons[(int)ButtonType.Rec]       = new Button { Category = ButtonCategory.Playback, GetBitmap = OnRecordGetBitmap, Click = OnRecord };
            buttons[(int)ButtonType.Rewind]    = new Button { Category = ButtonCategory.Playback, BmpAtlasIndex = ButtonImageIndices.Rewind, Click = OnRewind, Important = true };
            buttons[(int)ButtonType.Loop]      = new Button { Category = ButtonCategory.Playback, Click = OnLoop, GetBitmap = OnLoopGetBitmap };
            buttons[(int)ButtonType.Qwerty]    = new Button { Category = ButtonCategory.Playback, BmpAtlasIndex = ButtonImageIndices.QwertyPiano, Click = OnQwerty, Enabled = OnQwertyEnabled };
            buttons[(int)ButtonType.Metronome] = new Button { Category = ButtonCategory.Playback, BmpAtlasIndex = ButtonImageIndices.Metronome, Click = OnMetronome, Enabled = OnMetronomeEnabled };
            buttons[(int)ButtonType.Machine]   = new Button { Category = ButtonCategory.Playback, Click = OnMachine, GetBitmap = OnMachineGetBitmap, Enabled = OnMachineEnabled };
            buttons[(int)ButtonType.Follow]    = new Button { Category = ButtonCategory.Playback, BmpAtlasIndex = ButtonImageIndices.Follow, Click = OnFollow, Enabled = OnFollowEnabled };
            buttons[(int)ButtonType.Help]      = new Button { Category = ButtonCategory.Misc, BmpAtlasIndex = ButtonImageIndices.Help, RightAligned = true, Click = OnHelp, Important = true };
            buttons[(int)ButtonType.Sequencer] = new Button { Category = ButtonCategory.Navigation, BmpAtlasIndex = ButtonImageIndices.Sequencer, /*Click = OnHelp, Important = true*/ };
            buttons[(int)ButtonType.PianoRoll] = new Button { Category = ButtonCategory.Navigation, BmpAtlasIndex = ButtonImageIndices.PianoRoll, /*Click = OnHelp, Important = true*/ };
            buttons[(int)ButtonType.Project]   = new Button { Category = ButtonCategory.Navigation, BmpAtlasIndex = ButtonImageIndices.ProjectExplorer, /*Click = OnHelp, Important = true*/ };

            buttons[(int)ButtonType.New].ToolTip       = "{MouseLeft} New Project {Ctrl} {N}";
            buttons[(int)ButtonType.Open].ToolTip      = "{MouseLeft} Open Project {Ctrl} {O}";
            buttons[(int)ButtonType.Save].ToolTip      = "{MouseLeft} Save Project {Ctrl} {S}\n{MouseRight} Save As...";
            buttons[(int)ButtonType.Export].ToolTip    = "{MouseLeft} Export to various formats {Ctrl} {E}\n{MouseRight} Repeat last export {Ctrl} {Shift} {E}";
            buttons[(int)ButtonType.Copy].ToolTip      = "{MouseLeft} Copy selection {Ctrl} {C}";
            buttons[(int)ButtonType.Cut].ToolTip       = "{MouseLeft} Cut selection {Ctrl} {X}";
            buttons[(int)ButtonType.Paste].ToolTip     = "{MouseLeft} Paste {Ctrl} {V}\n{MouseRight} Paste Special... {Ctrl} {Shift} {V}";
            buttons[(int)ButtonType.Undo].ToolTip      = "{MouseLeft} Undo {Ctrl} {Z}";
            buttons[(int)ButtonType.Redo].ToolTip      = "{MouseLeft} Redo {Ctrl} {Y}";
            buttons[(int)ButtonType.Transform].ToolTip = "{MouseLeft} Perform cleanup and various operations";
            buttons[(int)ButtonType.Config].ToolTip    = "{MouseLeft} Edit Application Settings";
            buttons[(int)ButtonType.Play].ToolTip      = "{MouseLeft} Play/Pause {Space} - {MouseWheel} Change play rate - Play from start of pattern {Ctrl} {Space}\nPlay from start of song {Shift} {Space} - Play from loop point {Ctrl} {Shift} {Space}";
            buttons[(int)ButtonType.Rewind].ToolTip    = "{MouseLeft} Rewind {Home}\nRewind to beginning of current pattern {Ctrl} {Home}";
            buttons[(int)ButtonType.Rec].ToolTip       = "{MouseLeft} Toggles recording mode {Enter}\nAbort recording {Esc}";
            buttons[(int)ButtonType.Loop].ToolTip      = "{MouseLeft} Toggle Loop Mode (Song, Pattern/Selection)";
            buttons[(int)ButtonType.Qwerty].ToolTip    = "{MouseLeft} Toggle QWERTY keyboard piano input {Shift} {Q}";
            buttons[(int)ButtonType.Metronome].ToolTip = "{MouseLeft} Toggle metronome while song is playing";
            buttons[(int)ButtonType.Machine].ToolTip   = "{MouseLeft} Toggle between NTSC/PAL playback mode";
            buttons[(int)ButtonType.Follow].ToolTip    = "{MouseLeft} Toggle follow mode {Shift} {F}";
            buttons[(int)ButtonType.Help].ToolTip      = "{MouseLeft} Online documentation";
        }

        protected void OnRenderTerminatedCommon()
        {
            theme.Terminate();
            Utils.DisposeAndNullify(ref bmpButtonAtlas);
        }

        protected override void OnResize(EventArgs e)
        {
            UpdateButtonLayout();
            base.OnResize(e);
        }

        public void LayoutChanged()
        {
            UpdateButtonLayout();
            ConditionalInvalidate();
        }

        public void ConditionalInvalidate()
        {
            if (App != null && !App.RealTimeUpdate)
                Invalidate();
        }

        private void OnNew()
        {
            App.NewProject();
        }

        private void OnOpen()
        {
            App.OpenProject();
        }

        private void OnSave()
        {
            App.SaveProject();
        }

        private void OnSaveAs()
        {
            App.SaveProject(true);
        }

        private void OnExport()
        {
            App.Export();
        }

        private void OnRepeatLastExport()
        {
            App.RepeatLastExport();
        }

        private void OnCut()
        {
            App.Cut();
        }

        private ButtonStatus OnCutEnabled()
        {
            return App.CanCopy ? ButtonStatus.Enabled : ButtonStatus.Disabled;
        }

        private void OnCopy()
        {
            App.Copy();
        }

        private ButtonStatus OnCopyEnabled()
        {
            return App.CanCopy ? ButtonStatus.Enabled : ButtonStatus.Disabled;
        }

        private void OnPaste()
        {
            App.Paste();
        }

        private void OnPasteSpecial()
        {
            App.PasteSpecial();
        }

        private ButtonStatus OnPasteEnabled()
        {
            return App.CanPaste ? ButtonStatus.Enabled : ButtonStatus.Disabled;
        }

        private void OnUndo()
        {
            App.UndoRedoManager.Undo();
        }

        private ButtonStatus OnUndoEnabled()
        {
            return App.UndoRedoManager != null && App.UndoRedoManager.UndoScope != TransactionScope.Max ? ButtonStatus.Enabled : ButtonStatus.Disabled;
        }

        private void OnRedo()
        {
            App.UndoRedoManager.Redo();
        }

        private ButtonStatus OnRedoEnabled()
        {
            return App.UndoRedoManager != null && App.UndoRedoManager.RedoScope != TransactionScope.Max ? ButtonStatus.Enabled : ButtonStatus.Disabled;
        }

        private void OnTransform()
        {
            App.OpenTransformDialog();
        }

        private void OnConfig()
        {
            App.OpenConfigDialog();
        }

        private void OnPlay()
        {
            if (App.IsPlaying)
                App.StopSong();
            else
                App.PlaySong();
        }

        private void OnPlayMouseWheel(int delta)
        {
            int rate = App.PlayRate;

            if (delta < 0)
                App.PlayRate = Utils.Clamp(rate * 2, 1, 4);
            else
                App.PlayRate = Utils.Clamp(rate / 2, 1, 4);

            ConditionalInvalidate();
        }

        private ButtonImageIndices OnPlayGetBitmap()
        {
            if (App.IsPlaying)
            {
                return ButtonImageIndices.Pause;
            }
            else
            {
                switch (App.PlayRate)
                {
                    case 2:  return ButtonImageIndices.PlayHalf;
                    case 4:  return ButtonImageIndices.PlayQuarter;
                    default: return ButtonImageIndices.Play;
                }
            }
        }

        private void OnRewind()
        {
            App.StopSong();
            App.SeekSong(0);
        }

        private ButtonImageIndices OnRecordGetBitmap()
        {
            return App.IsRecording ? ButtonImageIndices.RecRed : ButtonImageIndices.Rec; 
        }

        private void OnRecord()
        {
            App.ToggleRecording();
        }

        private void OnLoop()
        {
            App.LoopMode = App.LoopMode == LoopMode.LoopPoint ? LoopMode.Pattern : LoopMode.LoopPoint;
        }

        private void OnQwerty()
        {
            App.ToggleQwertyPiano();
        }

        private ButtonStatus OnQwertyEnabled()
        {
            return App.IsQwertyPianoEnabled ? ButtonStatus.Enabled : ButtonStatus.Dimmed;
        }

        private void OnMetronome()
        {
            App.ToggleMetronome();
        }

        private ButtonStatus OnMetronomeEnabled()
        {
            return App.IsMetronomeEnabled ? ButtonStatus.Enabled : ButtonStatus.Dimmed;
        }

        private ButtonImageIndices OnLoopGetBitmap()
        {
            switch (App.LoopMode)
            {
                case LoopMode.Pattern:
                    return App.SequencerHasSelection ? ButtonImageIndices.LoopSelection : ButtonImageIndices.LoopPattern;
                default:
                    return App.Song.LoopPoint < 0 ? ButtonImageIndices.LoopNone : ButtonImageIndices.Loop;
            }
        }

        private void OnMachine()
        {
            App.PalPlayback = !App.PalPlayback;
        }

        private void OnFollow()
        {
            App.FollowModeEnabled = !App.FollowModeEnabled;
        }

        private ButtonStatus OnFollowEnabled()
        {
            return App.FollowModeEnabled ? ButtonStatus.Enabled : ButtonStatus.Dimmed;
        }

        private ButtonStatus OnMachineEnabled()
        {
            return App.Project != null && !App.Project.UsesAnyExpansionAudio ? ButtonStatus.Enabled : ButtonStatus.Disabled;
        }

        private ButtonImageIndices OnMachineGetBitmap()
        {
            if (App.Project == null)
            {
                return ButtonImageIndices.NTSC;
            }
            else if (App.Project.UsesFamiTrackerTempo)
            {
                return App.PalPlayback ? ButtonImageIndices.PAL : ButtonImageIndices.NTSC;
            }
            else
            {
                if (App.Project.PalMode)
                    return App.PalPlayback ? ButtonImageIndices.PAL : ButtonImageIndices.PALToNTSC;
                else
                    return App.PalPlayback ? ButtonImageIndices.NTSCToPAL : ButtonImageIndices.NTSC;
            }
        }

        private void OnHelp()
        {
            App.ShowHelp();
        }

        private void RenderButtons(RenderGraphics g, RenderCommandList c)
        {
            var pt = PointToClient(Cursor.Position);

            // Clear
            c.FillRectangle(0, 0, Width, Height, toolbarBrush);

            // Buttons
            foreach (var btn in buttons)
            {
                if (!btn.Visible)
                    continue;

                var hover = btn.IsPointIn(pt.X, pt.Y, Width);
                var bmpIndex = btn.GetBitmap != null ? btn.GetBitmap() : btn.BmpAtlasIndex;
                var status = btn.Enabled == null ? ButtonStatus.Enabled : btn.Enabled();
                var opacity = status == ButtonStatus.Enabled ? 1.0f : 0.25f;

                if (status != ButtonStatus.Disabled && hover)
                    opacity *= 0.75f;

                int x = btn.RightAligned ? Width - btn.X : btn.X;
                c.DrawBitmapAtlas(bmpButtonAtlas, (int)bmpIndex, x, btn.Y, opacity, buttonBitmapScaleFloat);
            }
        }

        private void RenderTimecode(RenderGraphics g, RenderCommandList c)
        {
            var frame = App.CurrentFrame;
            var famitrackerTempo = App.Project != null && App.Project.UsesFamiTrackerTempo;

            var zeroSizeX  = g.MeasureString("0", ThemeBase.FontHuge);
            var colonSizeX = g.MeasureString(":", ThemeBase.FontHuge);

            var timeCodeSizeY = Height - timecodePosY * 2;
            var textColor = App.IsRecording ? theme.DarkRedFillBrush : theme.LightGreyFillBrush2;

            c.FillAndDrawRectangle(timecodePosX, timecodePosY, timecodePosX + timecodeSizeX, Height - timecodePosY, theme.BlackBrush, theme.LightGreyFillBrush2);

            if (Settings.TimeFormat == 0 || famitrackerTempo) // MM:SS:mmm cant be used with FamiTracker tempo.
            {
                var location = NoteLocation.FromAbsoluteNoteIndex(App.Song, frame);

                var numPatternDigits = Utils.NumDecimalDigits(App.Song.Length - 1);
                var numNoteDigits = Utils.NumDecimalDigits(App.Song.GetPatternLength(location.PatternIndex) - 1);

                var patternString = (location.PatternIndex + 1).ToString("D" + numPatternDigits);
                var noteString = location.NoteIndex.ToString("D" + numNoteDigits);

                var charPosX = timecodePosX + timecodeSizeX / 2 - ((numPatternDigits + numNoteDigits) * zeroSizeX + colonSizeX) / 2;

                for (int i = 0; i < numPatternDigits; i++, charPosX += zeroSizeX)
                    c.DrawText(patternString[i].ToString(), ThemeBase.FontHuge, charPosX, 2, textColor, zeroSizeX);

                c.DrawText(":", ThemeBase.FontHuge, charPosX, 2, textColor, colonSizeX);
                charPosX += colonSizeX;

                for (int i = 0; i < numNoteDigits; i++, charPosX += zeroSizeX)
                    c.DrawText(noteString[i].ToString(), ThemeBase.FontHuge, charPosX, 2, textColor, zeroSizeX);
            }
            else
            {
                TimeSpan time = App.CurrentTime;

                var minutesString      = time.Minutes.ToString("D2");
                var secondsString      = time.Seconds.ToString("D2");
                var millisecondsString = time.Milliseconds.ToString("D3");

                // 00:00:000
                var charPosX = timecodePosX + timecodeSizeX / 2 - (7 * zeroSizeX + 2 * colonSizeX) / 2;

                for (int i = 0; i < 2; i++, charPosX += zeroSizeX)
                    c.DrawText(minutesString[i].ToString(), ThemeBase.FontHuge, charPosX, 2, textColor, zeroSizeX);
                c.DrawText(":", ThemeBase.FontHuge, charPosX, 2, textColor, colonSizeX);
                charPosX += colonSizeX;
                for (int i = 0; i < 2; i++, charPosX += zeroSizeX)
                    c.DrawText(secondsString[i].ToString(), ThemeBase.FontHuge, charPosX, 2, textColor, zeroSizeX);
                c.DrawText(":", ThemeBase.FontHuge, charPosX, 2, textColor, colonSizeX);
                charPosX += colonSizeX;
                for (int i = 0; i < 3; i++, charPosX += zeroSizeX)
                    c.DrawText(millisecondsString[i].ToString(), ThemeBase.FontHuge, charPosX, 2, textColor, zeroSizeX);
            }
        }

        public bool ShouldRefreshOscilloscope(bool hasNonZeroSample)
        {
            return oscilloscopeVisible && lastOscilloscopeHadNonZeroSample != hasNonZeroSample;
        }

        private void RenderOscilloscope(RenderGraphics g, RenderCommandList c, int x, int y, int sx, int sy)
        {
            if (!oscilloscopeVisible)
                return;

            c.FillRectangle(x, y, x + sx, y + sy, theme.BlackBrush);

            var oscilloscopeGeometry = App.GetOscilloscopeGeometry(out lastOscilloscopeHadNonZeroSample);

            if (oscilloscopeGeometry != null && lastOscilloscopeHadNonZeroSample)
            {
                float scaleX = sx;
                float scaleY = sy / -2; // D3D is upside down compared to how we display waves typically.

                c.PushTransform(x, y + sy / 2, scaleX, scaleY);
                c.DrawGeometry(oscilloscopeGeometry, theme.LightGreyFillBrush2, 1.0f, true);
                c.PopTransform();
            }
            else
            {
                c.PushTranslation(x, y + sy / 2);
                c.DrawLine(0, 0, sx, 0, theme.LightGreyFillBrush2);
                c.PopTransform();
            }

            c.DrawRectangle(x, y, x + sx, y + sy, theme.LightGreyFillBrush2);
        }
    }
}
