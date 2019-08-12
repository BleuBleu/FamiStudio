using FamiStudio.Properties;
using SharpDX.Direct2D1;
using System;
using System.Windows.Forms;

namespace FamiStudio
{
    class Toolbar : Direct2DUserControl
    {
        const int ButtonNew = 0;
        const int ButtonOpen = 1;
        const int ButtonSave = 2;
        const int ButtonExport = 3;
        const int ButtonUndo = 4;
        const int ButtonRedo = 5;
        const int ButtonPlay = 6;
        const int ButtonRewind = 7;
        const int ButtonLoop = 8;
        const int ButtonCount = 9;

        const int TimecodePosX = 260;
        const int TimecodeSizeX = 160;

        private delegate void EmptyDelegate();
        private delegate bool BoolDelegate();
        private delegate Bitmap BitmapDelegate();

        class Button
        {
            public int X;
            public int Y;
            public string ToolTip;
            public Bitmap Bmp;
            public BoolDelegate Enabled;
            public EmptyDelegate Click;
            public EmptyDelegate RightClick;
            public BitmapDelegate GetBitmap;
            public bool IsPointIn(int px, int py) => px >= X && (px - X) < 32 && py >= Y && (py - Y) < 32;
        };

        string tooltip = "";
        Theme theme;
        Brush toolbarBrush;
        Bitmap bmpLoopNone;
        Bitmap bmpLoopSong;
        Bitmap bmpLoopPattern;
        Bitmap bmpPlay;
        Bitmap bmpPause;
        Button[] buttons = new Button[ButtonCount];

        protected override void OnDirect2DInitialized(Direct2DGraphics g)
        {
            theme = Theme.CreateResourcesForGraphics(g);
            toolbarBrush = g.CreateHorizontalGradientBrush(0, 81, Theme.LightGreyFillColor1, Theme.LightGreyFillColor2);

            bmpLoopNone = g.ConvertBitmap(Resources.LoopNone);
            bmpLoopSong = g.ConvertBitmap(Resources.Loop);
            bmpLoopPattern = g.ConvertBitmap(Resources.LoopPattern);
            bmpPlay = g.ConvertBitmap(Resources.Play);
            bmpPause = g.ConvertBitmap(Resources.Pause);

            buttons[ButtonNew] = new Button { X = 4, Y = 4, Bmp = g.ConvertBitmap(Resources.File), Click = OnNew };
            buttons[ButtonOpen] = new Button { X = 44, Y = 4, Bmp = g.ConvertBitmap(Resources.Open), Click = OnOpen };
            buttons[ButtonSave] = new Button { X = 84, Y = 4, Bmp = g.ConvertBitmap(Resources.Save), Click = OnSave, RightClick = OnSaveAs };
            buttons[ButtonExport] = new Button { X = 124, Y = 4, Bmp = g.ConvertBitmap(Resources.ExportMusic), Click = OnExportFamitone, Enabled = OnExportFamitoneEnabled };
            buttons[ButtonUndo] = new Button { X = 164, Y = 4, Bmp = g.ConvertBitmap(Resources.Undo), Click = OnUndo, Enabled = OnUndoEnabled };
            buttons[ButtonRedo] = new Button { X = 204, Y = 4, Bmp = g.ConvertBitmap(Resources.Redo), Click = OnRedo, Enabled = OnRedoEnabled };
            buttons[ButtonPlay] = new Button { X = 436, Y = 4, Click = OnPlay, GetBitmap = OnPlayGetBitmap };
            buttons[ButtonRewind] = new Button { X = 476, Y = 4, Bmp = g.ConvertBitmap(Resources.Rewind), Click = OnRewind };
            buttons[ButtonLoop] = new Button { X = 516, Y = 4, Click = OnLoop, GetBitmap = OnLoopGetBitmap };

            buttons[ButtonNew].ToolTip = "New Project (Ctrl-N)";
            buttons[ButtonOpen].ToolTip = "Open Project (Ctrl-O)";
            buttons[ButtonSave].ToolTip = "Save Project (Ctrl-S) [Right-Click: Save As...]";
            buttons[ButtonExport].ToolTip = "Export to various formats (Ctrl+E)";
            buttons[ButtonUndo].ToolTip = "Undo (Ctrl+Z)";
            buttons[ButtonRedo].ToolTip = "Redo (Ctrl+Y)";
            buttons[ButtonPlay].ToolTip = "Play/Pause (Space) [Ctrl+Space: Play pattern loop, Shift-Space: Play song loop]";
            buttons[ButtonRewind].ToolTip = "Rewind (Home) [Ctrl+Home: Rewind to beginning of current pattern]";
            buttons[ButtonLoop].ToolTip = "Toggle Loop Mode";
        }

        public string ToolTip
        {
            get { return tooltip; }
            set { tooltip = value; ConditionalInvalidate(); }
        }

        private FamiStudioForm App => ParentForm as FamiStudioForm;

        public void Reset()
        {
            tooltip = "";
        }

        private void ConditionalInvalidate()
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

        private void OnExportFamitone()
        {
            App.ExportFamitone();
        }

        private bool OnExportFamitoneEnabled()
        {
            return !string.IsNullOrEmpty(App.Project.Filename);
        }

        private void OnUndo()
        {
            App.UndoRedoManager.Undo();
        }

        private bool OnUndoEnabled()
        {
            return App.UndoRedoManager.UndoScope != TransactionScope.Max;
        }

        private void OnRedo()
        {
            App.UndoRedoManager.Redo();
        }

        private bool OnRedoEnabled()
        {
            return App.UndoRedoManager.RedoScope != TransactionScope.Max;
        }

        private void OnPlay()
        {
            if (App.IsPlaying)
                App.Stop();
            else
                App.Play();
        }

        private Bitmap OnPlayGetBitmap()
        {
            return App.IsPlaying ? bmpPause : bmpPlay;
        }

        private void OnRewind()
        {
            App.Stop();
            App.Seek(0);
        }

        private void OnLoop()
        {
            App.LoopMode = (LoopMode)(((int)App.LoopMode + 1) % (int)LoopMode.Max);
        }

        private Bitmap OnLoopGetBitmap()
        {
            switch (App.LoopMode)
            {
                case LoopMode.None: return bmpLoopNone;
                case LoopMode.Song: return bmpLoopSong;
                case LoopMode.Pattern: return bmpLoopPattern;
            }

            return null;
        }

        protected override void OnRender(Direct2DGraphics g)
        {
            g.FillRectangle(0, 0, Width, Height, toolbarBrush);

            var pt = this.PointToClient(Cursor.Position);

            // Buttons
            foreach (var btn in buttons)
            {
                bool hover = btn.IsPointIn(pt.X, pt.Y);
                var bmp = btn.GetBitmap != null ? btn.GetBitmap() : btn.Bmp;
                if (bmp == null)
                    bmp = btn.Bmp;
                bool enabled = btn.Enabled != null ? btn.Enabled() : true;
                g.DrawBitmap(bmp, btn.X, btn.Y, enabled ? (hover ? 0.75f : 1.0f) : 0.25f);
            }

            // Timecode
            int frame = App.CurrentFrame;
            int patternIdx = frame / App.Song.PatternLength;
            int noteIdx = frame % App.Song.PatternLength;

            g.FillAndDrawRectangleHalfPixel(TimecodePosX, 4, TimecodePosX + TimecodeSizeX, Height - 4, theme.DarkGreyFillBrush1, theme.BlackBrush);
            g.DrawText($"{patternIdx:D3}:{noteIdx:D3}", Theme.FontHuge, TimecodePosX + 30, 2, theme.LightGreyFillBrush1, TimecodeSizeX);

            // Tooltip
            if (!string.IsNullOrEmpty(tooltip))
            {
                g.DrawText(tooltip, Theme.FontMediumRight, Width - 314, 12, theme.BlackBrush, 300);
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            ConditionalInvalidate();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            ConditionalInvalidate();

            foreach (var btn in buttons)
            {
                if (btn.IsPointIn(e.X, e.Y))
                {
                    ToolTip = btn.ToolTip;
                    return;
                }
            }

            ToolTip = "";
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            bool left = e.Button.HasFlag(MouseButtons.Left);
            bool right = e.Button.HasFlag(MouseButtons.Right);

            if (left || right)
            {
                foreach (var btn in buttons)
                {
                    if (btn.IsPointIn(e.X, e.Y) && (btn.Enabled == null || btn.Enabled()))
                    {
                        if (left)
                            btn?.Click();
                        else
                            btn?.RightClick();
                        break;
                    }
                }
            }
        }
    }
}
