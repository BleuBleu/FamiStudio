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
    public abstract class ToolbarBase : RenderControl
    {
        protected enum ButtonType
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
            More,
            Count
        }

        protected enum ButtonStatus
        {
            Enabled,
            Disabled,
            Dimmed
        }

        protected enum ButtonImageIndices
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
            More,
            Count
        };

        protected readonly string[] ButtonImageNames = new string[]
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
            "More"
        };

        protected delegate void MouseWheelDelegate(int delta);
        protected delegate void EmptyDelegate();
        protected delegate ButtonStatus ButtonStatusDelegate();
        protected delegate ButtonImageIndices BitmapDelegate();

        // DROIDTODO : Have a separate position + hitbox.
        protected class Button
        {
            public int X;
            public int Y;
            public bool RightAligned;
            public bool Visible = true;
            public int Size;
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

        protected int timecodePosX;
        protected int timecodePosY;
        protected int oscilloscopePosX;
        protected int oscilloscopePosY;
        protected int timecodeOscSizeX;
        protected int timecodeOscSizeY;

        protected RenderTheme theme;
        protected RenderBrush toolbarBrush;
        protected RenderBrush warningBrush;
        protected RenderBitmapAtlas bmpButtonAtlas;
        protected Button[] buttons = new Button[(int)ButtonType.Count];

        protected bool oscilloscopeVisible = true;
        protected bool lastOscilloscopeHadNonZeroSample = false;

        protected float buttonBitmapScaleFloat = 1.0f;

        protected override void OnRenderInitialized(RenderGraphics g)
        {
            Debug.Assert((int)ButtonImageIndices.Count == ButtonImageNames.Length);

            theme = RenderTheme.CreateResourcesForGraphics(g);
            toolbarBrush = g.CreateVerticalGradientBrush(0, Height, ThemeBase.DarkGreyFillColor2, ThemeBase.DarkGreyFillColor1); // DROIDTODO : Makes no sense on mobile.
            warningBrush = g.CreateSolidBrush(System.Drawing.Color.FromArgb(205, 77, 64));

            bmpButtonAtlas = g.CreateBitmapAtlasFromResources(ButtonImageNames);

            buttons[(int)ButtonType.New]       = new Button { BmpAtlasIndex = ButtonImageIndices.File, Click = OnNew };
            buttons[(int)ButtonType.Open]      = new Button { BmpAtlasIndex = ButtonImageIndices.Open, Click = OnOpen };
            buttons[(int)ButtonType.Save]      = new Button { BmpAtlasIndex = ButtonImageIndices.Save, Click = OnSave, RightClick = OnSaveAs };
            buttons[(int)ButtonType.Export]    = new Button { BmpAtlasIndex = ButtonImageIndices.Export, Click = OnExport, RightClick = OnRepeatLastExport };
            buttons[(int)ButtonType.Copy]      = new Button { BmpAtlasIndex = ButtonImageIndices.Copy, Click = OnCopy, Enabled = OnCopyEnabled };
            buttons[(int)ButtonType.Cut]       = new Button { BmpAtlasIndex = ButtonImageIndices.Cut, Click = OnCut, Enabled = OnCutEnabled };
            buttons[(int)ButtonType.Paste]     = new Button { BmpAtlasIndex = ButtonImageIndices.Paste, Click = OnPaste, RightClick = OnPasteSpecial, Enabled = OnPasteEnabled };
            buttons[(int)ButtonType.Undo]      = new Button { BmpAtlasIndex = ButtonImageIndices.Undo, Click = OnUndo, Enabled = OnUndoEnabled };
            buttons[(int)ButtonType.Redo]      = new Button { BmpAtlasIndex = ButtonImageIndices.Redo, Click = OnRedo, Enabled = OnRedoEnabled };
            buttons[(int)ButtonType.Transform] = new Button { BmpAtlasIndex = ButtonImageIndices.Transform, Click = OnTransform };
            buttons[(int)ButtonType.Config]    = new Button { BmpAtlasIndex = ButtonImageIndices.Config, Click = OnConfig };
            buttons[(int)ButtonType.Play]      = new Button { Click = OnPlay, MouseWheel = OnPlayMouseWheel, GetBitmap = OnPlayGetBitmap };
            buttons[(int)ButtonType.Rec]       = new Button { GetBitmap = OnRecordGetBitmap, Click = OnRecord };
            buttons[(int)ButtonType.Rewind]    = new Button { BmpAtlasIndex = ButtonImageIndices.Rewind, Click = OnRewind };
            buttons[(int)ButtonType.Loop]      = new Button { Click = OnLoop, GetBitmap = OnLoopGetBitmap };
            buttons[(int)ButtonType.Qwerty]    = new Button { BmpAtlasIndex = ButtonImageIndices.QwertyPiano, Click = OnQwerty, Enabled = OnQwertyEnabled };
            buttons[(int)ButtonType.Metronome] = new Button { BmpAtlasIndex = ButtonImageIndices.Metronome, Click = OnMetronome, Enabled = OnMetronomeEnabled };
            buttons[(int)ButtonType.Machine]   = new Button { Click = OnMachine, GetBitmap = OnMachineGetBitmap, Enabled = OnMachineEnabled };
            buttons[(int)ButtonType.Follow]    = new Button { BmpAtlasIndex = ButtonImageIndices.Follow, Click = OnFollow, Enabled = OnFollowEnabled };
            buttons[(int)ButtonType.Help]      = new Button { BmpAtlasIndex = ButtonImageIndices.Help, RightAligned = true, Click = OnHelp };
            buttons[(int)ButtonType.More]      = new Button { BmpAtlasIndex = ButtonImageIndices.More, Click = OnMore, Visible = false };
        }

        protected override void OnRenderTerminated()
        {
            theme.Terminate();
            Utils.DisposeAndNullify(ref bmpButtonAtlas);
        }

        protected abstract void UpdateButtonLayout();

        protected override void OnResize(EventArgs e)
        {
            UpdateButtonLayout();
        }

        // DROIDTODO : This makes no sense on mobile, move elsewhere.
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

        public virtual void SetToolTip(string msg, bool red = false)
        {
        }

        public virtual void DisplayWarning(string msg, bool beep)
        {
        }

        protected void OnNew()
        {
            App.NewProject();
        }

        protected void OnOpen()
        {
            App.OpenProject();
        }

        protected void OnSave()
        {
            App.SaveProject();
        }

        protected void OnSaveAs()
        {
            App.SaveProject(true);
        }

        protected void OnExport()
        {
            App.Export();
        }

        protected void OnRepeatLastExport()
        {
            App.RepeatLastExport();
        }

        protected void OnCut()
        {
            App.Cut();
        }

        protected ButtonStatus OnCutEnabled()
        {
            return App.CanCopy ? ButtonStatus.Enabled : ButtonStatus.Disabled;
        }

        protected void OnCopy()
        {
            App.Copy();
        }

        protected ButtonStatus OnCopyEnabled()
        {
            return App.CanCopy ? ButtonStatus.Enabled : ButtonStatus.Disabled;
        }

        protected void OnPaste()
        {
            App.Paste();
        }

        protected void OnPasteSpecial()
        {
            App.PasteSpecial();
        }

        protected ButtonStatus OnPasteEnabled()
        {
            return App.CanPaste ? ButtonStatus.Enabled : ButtonStatus.Disabled;
        }

        protected void OnUndo()
        {
            App.UndoRedoManager.Undo();
        }

        protected ButtonStatus OnUndoEnabled()
        {
            return App.UndoRedoManager != null && App.UndoRedoManager.UndoScope != TransactionScope.Max ? ButtonStatus.Enabled : ButtonStatus.Disabled;
        }

        protected void OnRedo()
        {
            App.UndoRedoManager.Redo();
        }

        protected ButtonStatus OnRedoEnabled()
        {
            return App.UndoRedoManager != null && App.UndoRedoManager.RedoScope != TransactionScope.Max ? ButtonStatus.Enabled : ButtonStatus.Disabled;
        }

        protected void OnTransform()
        {
            App.OpenTransformDialog();
        }

        protected void OnConfig()
        {
            App.OpenConfigDialog();
        }

        protected void OnPlay()
        {
            if (App.IsPlaying)
                App.StopSong();
            else
                App.PlaySong();
        }

        protected void OnPlayMouseWheel(int delta)
        {
            int rate = App.PlayRate;

            if (delta < 0)
                App.PlayRate = Utils.Clamp(rate * 2, 1, 4);
            else
                App.PlayRate = Utils.Clamp(rate / 2, 1, 4);

            ConditionalInvalidate();
        }

        protected ButtonImageIndices OnPlayGetBitmap()
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

        protected void OnRewind()
        {
            App.StopSong();
            App.SeekSong(0);
        }

        protected ButtonImageIndices OnRecordGetBitmap()
        {
            return App.IsRecording ? ButtonImageIndices.RecRed : ButtonImageIndices.Rec; 
        }

        protected void OnRecord()
        {
            App.ToggleRecording();
        }

        protected void OnLoop()
        {
            App.LoopMode = App.LoopMode == LoopMode.LoopPoint ? LoopMode.Pattern : LoopMode.LoopPoint;
        }

        protected void OnQwerty()
        {
            App.ToggleQwertyPiano();
        }

        protected ButtonStatus OnQwertyEnabled()
        {
            return App.IsQwertyPianoEnabled ? ButtonStatus.Enabled : ButtonStatus.Dimmed;
        }

        protected void OnMetronome()
        {
            App.ToggleMetronome();
        }

        protected ButtonStatus OnMetronomeEnabled()
        {
            return App.IsMetronomeEnabled ? ButtonStatus.Enabled : ButtonStatus.Dimmed;
        }

        protected ButtonImageIndices OnLoopGetBitmap()
        {
            switch (App.LoopMode)
            {
                case LoopMode.Pattern:
                    return App.SequencerHasSelection ? ButtonImageIndices.LoopSelection : ButtonImageIndices.LoopPattern;
                default:
                    return App.Song.LoopPoint < 0 ? ButtonImageIndices.LoopNone : ButtonImageIndices.Loop;
            }
        }

        protected void OnMachine()
        {
            App.PalPlayback = !App.PalPlayback;
        }

        protected void OnFollow()
        {
            App.FollowModeEnabled = !App.FollowModeEnabled;
        }

        protected ButtonStatus OnFollowEnabled()
        {
            return App.FollowModeEnabled ? ButtonStatus.Enabled : ButtonStatus.Dimmed;
        }

        protected ButtonStatus OnMachineEnabled()
        {
            return App.Project != null && !App.Project.UsesAnyExpansionAudio ? ButtonStatus.Enabled : ButtonStatus.Disabled;
        }

        protected ButtonImageIndices OnMachineGetBitmap()
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

        protected void OnHelp()
        {
            App.ShowHelp();
        }

        protected virtual void OnMore()
        {
        }

        protected void RenderButtons(RenderGraphics g, RenderCommandList c)
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

        protected void RenderTimecode(RenderGraphics g, RenderCommandList c, int x, int y, int sx, int sy)
        {
            var frame = App.CurrentFrame;
            var famitrackerTempo = App.Project != null && App.Project.UsesFamiTrackerTempo;

            var zeroSizeX  = g.MeasureString("0", ThemeBase.FontHuge);
            var colonSizeX = g.MeasureString(":", ThemeBase.FontHuge);

            var timeCodeSizeY = Height - timecodePosY * 2;
            var textColor = App.IsRecording ? theme.DarkRedFillBrush : theme.LightGreyFillBrush2;

            c.FillAndDrawRectangle(x, y, x + sx, y + sy, theme.BlackBrush, theme.LightGreyFillBrush2);

            if (Settings.TimeFormat == 0 || famitrackerTempo) // MM:SS:mmm cant be used with FamiTracker tempo.
            {
                var location = NoteLocation.FromAbsoluteNoteIndex(App.Song, frame);

                var numPatternDigits = Utils.NumDecimalDigits(App.Song.Length - 1);
                var numNoteDigits = Utils.NumDecimalDigits(App.Song.GetPatternLength(location.PatternIndex) - 1);

                var patternString = (location.PatternIndex + 1).ToString("D" + numPatternDigits);
                var noteString = location.NoteIndex.ToString("D" + numNoteDigits);

                var charPosX = x + sx / 2 - ((numPatternDigits + numNoteDigits) * zeroSizeX + colonSizeX) / 2;

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

                var minutesString = time.Minutes.ToString("D2");
                var secondsString = time.Seconds.ToString("D2");
                var millisecondsString = time.Milliseconds.ToString("D3");

                // 00:00:000
                var charPosX = x + sx / 2 - (7 * zeroSizeX + 2 * colonSizeX) / 2;

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

        // DROIDTODO : We can use the default command list on mobile here.
        protected override void OnRender(RenderGraphics g)
        {
            var cm = g.CreateCommandList(); // Main

            // Prepare the batches.
            RenderButtons(g, cm);
            RenderTimecode(g, cm, timecodePosX, timecodePosY, timecodeOscSizeX, timecodeOscSizeY);
            RenderOscilloscope(g, cm, oscilloscopePosX, oscilloscopePosY, timecodeOscSizeX, timecodeOscSizeY);

            // Draw everything.
            g.DrawCommandList(cm);
        }

        public bool ShouldRefreshOscilloscope(bool hasNonZeroSample)
        {
            return oscilloscopeVisible && lastOscilloscopeHadNonZeroSample != hasNonZeroSample;
        }

        protected void RenderOscilloscope(RenderGraphics g, RenderCommandList c, int x, int y, int sx, int sy)
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
