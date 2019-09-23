using System;
using System.Windows.Forms;
using FamiStudio.Properties;

#if FAMISTUDIO_WINDOWS
    using RenderBitmap   = SharpDX.Direct2D1.Bitmap;
    using RenderBrush    = SharpDX.Direct2D1.Brush;
    using RenderPath     = SharpDX.Direct2D1.PathGeometry;
    using RenderFont     = SharpDX.DirectWrite.TextFormat;
    using RenderControl  = FamiStudio.Direct2DControl;
    using RenderGraphics = FamiStudio.Direct2DGraphics;
    using RenderTheme    = FamiStudio.Direct2DTheme;
#else
    using RenderBitmap   = FamiStudio.GLBitmap;
    using RenderBrush    = FamiStudio.GLBrush;
    using RenderPath     = FamiStudio.GLConvexPath;
    using RenderFont     = FamiStudio.GLFont;
    using RenderControl  = FamiStudio.GLControl;
    using RenderGraphics = FamiStudio.GLGraphics;
    using RenderTheme    = FamiStudio.GLTheme;
#endif

namespace FamiStudio
{
    public class Toolbar : RenderControl
    {
        const int ButtonNew    = 0;
        const int ButtonOpen   = 1;
        const int ButtonSave   = 2;
        const int ButtonExport = 3;
        const int ButtonUndo   = 4;
        const int ButtonRedo   = 5;
        const int ButtonPlay   = 6;
        const int ButtonRewind = 7;
        const int ButtonLoop   = 8;
        const int ButtonCount  = 9;

        const int DefaultTimecodePosX     = 260;
        const int DefaultTimecodePosY     = 4;
        const int DefaultTimecodeSizeX    = 160;
        const int DefaultTimecodeTextPosX = 30;
        const int DefaultTooltipPosY      = 12;

        int timecodePosX;
        int timecodePosY;
        int timecodeSizeX;
        int timecodeTextPosX;
        int tooltipPosY;
        float buttonScaling = 1.0f;

        private delegate void EmptyDelegate();
        private delegate bool BoolDelegate();
        private delegate RenderBitmap BitmapDelegate();

        class Button
        {
            public int X;
            public int Y;
            public int Size = 32;
            public string ToolTip;
            public RenderBitmap Bmp;
            public BoolDelegate Enabled;
            public EmptyDelegate Click;
            public EmptyDelegate RightClick;
            public BitmapDelegate GetBitmap;
            public bool IsPointIn(int px, int py) => px >= X && (px - X) < Size && py >= Y && (py - Y) < Size;
        };

        string tooltip = "";
        RenderTheme theme;
        RenderBrush toolbarBrush;
        RenderBitmap bmpLoopNone;
        RenderBitmap bmpLoopSong;
        RenderBitmap bmpLoopPattern;
        RenderBitmap bmpPlay;
        RenderBitmap bmpPause;
        Button[] buttons = new Button[ButtonCount];

        protected override void OnRenderInitialized(RenderGraphics g)
        {
            theme = RenderTheme.CreateResourcesForGraphics(g);
            toolbarBrush = g.CreateHorizontalGradientBrush(0, 81, ThemeBase.LightGreyFillColor1, ThemeBase.LightGreyFillColor2);

            bmpLoopNone = g.CreateBitmapFromResource("LoopNone");
            bmpLoopSong = g.CreateBitmapFromResource("Loop");
            bmpLoopPattern = g.CreateBitmapFromResource("LoopPattern");
            bmpPlay = g.CreateBitmapFromResource("Play");
            bmpPause = g.CreateBitmapFromResource("Pause");

            buttons[ButtonNew]    = new Button { X = 4,   Y = 4, Bmp = g.CreateBitmapFromResource("File"), Click = OnNew };
            buttons[ButtonOpen]   = new Button { X = 44,  Y = 4, Bmp = g.CreateBitmapFromResource("Open"), Click = OnOpen };
            buttons[ButtonSave]   = new Button { X = 84,  Y = 4, Bmp = g.CreateBitmapFromResource("Save"), Click = OnSave, RightClick = OnSaveAs };
            buttons[ButtonExport] = new Button { X = 124, Y = 4, Bmp = g.CreateBitmapFromResource("Export"), Click = OnExport, Enabled = OnExportEnabled };
            buttons[ButtonUndo]   = new Button { X = 164, Y = 4, Bmp = g.CreateBitmapFromResource("Undo"), Click = OnUndo, Enabled = OnUndoEnabled };
            buttons[ButtonRedo]   = new Button { X = 204, Y = 4, Bmp = g.CreateBitmapFromResource("Redo"), Click = OnRedo, Enabled = OnRedoEnabled };
            buttons[ButtonPlay]   = new Button { X = 436, Y = 4, Click = OnPlay, GetBitmap = OnPlayGetBitmap };
            buttons[ButtonRewind] = new Button { X = 476, Y = 4, Bmp = g.CreateBitmapFromResource("Rewind"), Click = OnRewind };
            buttons[ButtonLoop]   = new Button { X = 516, Y = 4, Click = OnLoop, GetBitmap = OnLoopGetBitmap };

            buttons[ButtonNew].ToolTip    = "New Project (Ctrl-N)";
            buttons[ButtonOpen].ToolTip   = "Open Project (Ctrl-O)";
            buttons[ButtonSave].ToolTip   = "Save Project (Ctrl-S) [Right-Click: Save As...]";
            buttons[ButtonExport].ToolTip = "Export to various formats (Ctrl+E)";
            buttons[ButtonUndo].ToolTip   = "Undo (Ctrl+Z)";
            buttons[ButtonRedo].ToolTip   = "Redo (Ctrl+Y)";
            buttons[ButtonPlay].ToolTip   = "Play/Pause (Space) [Ctrl+Space: Play pattern loop, Shift-Space: Play song loop]";
            buttons[ButtonRewind].ToolTip = "Rewind (Home) [Ctrl+Home: Rewind to beginning of current pattern]";
            buttons[ButtonLoop].ToolTip   = "Toggle Loop Mode";

            var scaling = RenderTheme.MainWindowScaling;

            // When scaling is > 1, we use the 64x64 icons, but we need to scale them to 48.
            if (scaling == 1.5f)
                buttonScaling = 0.75f;

            for (int i = 0; i < ButtonCount; i++)
            {
                var btn = buttons[i];
                btn.X = (int)(btn.X * scaling);
                btn.Y = (int)(btn.Y * scaling);
                btn.Size = (int)(btn.Size * scaling);
            }

            timecodePosX     = (int)(DefaultTimecodePosX     * scaling);
            timecodePosY     = (int)(DefaultTimecodePosY     * scaling);
            timecodeSizeX    = (int)(DefaultTimecodeSizeX    * scaling);
            timecodeTextPosX = (int)(DefaultTimecodeTextPosX * scaling);
            tooltipPosY      = (int)(DefaultTooltipPosY      * scaling);
        }

        public string ToolTip
        {
            get { return tooltip; }
            set { tooltip = value; ConditionalInvalidate(); }
        }

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

        private void OnExport()
        {
            App.Export();
        }

        private bool OnExportEnabled()
        {
            return App.Project != null && !string.IsNullOrEmpty(App.Project.Filename);
        }

        private void OnUndo()
        {
            App.UndoRedoManager.Undo();
        }

        private bool OnUndoEnabled()
        {
            return App.UndoRedoManager != null && App.UndoRedoManager.UndoScope != TransactionScope.Max;
        }

        private void OnRedo()
        {
            App.UndoRedoManager.Redo();
        }

        private bool OnRedoEnabled()
        {
            return App.UndoRedoManager != null && App.UndoRedoManager.RedoScope != TransactionScope.Max;
        }

        private void OnPlay()
        {
            if (App.IsPlaying)
                App.Stop();
            else
                App.Play();
        }

        private RenderBitmap OnPlayGetBitmap()
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

        private RenderBitmap OnLoopGetBitmap()
        {
            switch (App.LoopMode)
            {
                case LoopMode.None: return bmpLoopNone;
                case LoopMode.Song: return bmpLoopSong;
                case LoopMode.Pattern: return bmpLoopPattern;
            }

            return null;
        }

        protected override void OnRender(RenderGraphics g)
        {
            g.FillRectangle(0, 0, Width, Height, toolbarBrush);

            var scaling = RenderTheme.MainWindowScaling;
            var pt = this.PointToClient(Cursor.Position);

            // Buttons
            foreach (var btn in buttons)
            {
                bool hover = btn.IsPointIn(pt.X, pt.Y);
                var bmp = btn.GetBitmap != null ? btn.GetBitmap() : btn.Bmp;
                if (bmp == null)
                    bmp = btn.Bmp;
                bool enabled = btn.Enabled != null ? btn.Enabled() : true;
                g.DrawBitmap(bmp, btn.X, btn.Y, true, buttonScaling, enabled ? (hover ? 0.75f : 1.0f) : 0.25f);
            }

            // Timecode
            int frame = App.CurrentFrame;
            int patternIdx = frame / App.Song.PatternLength;
            int noteIdx = frame % App.Song.PatternLength;

            g.FillAndDrawRectangle(timecodePosX, timecodePosY, timecodePosX + timecodeSizeX, Height - timecodePosY, theme.DarkGreyFillBrush1, theme.BlackBrush);
            g.DrawText($"{patternIdx:D3}:{noteIdx:D3}", ThemeBase.FontHuge, timecodePosX + timecodeTextPosX, 2, theme.LightGreyFillBrush1, timecodeSizeX);

            // Tooltip
            if (!string.IsNullOrEmpty(tooltip))
            {
                g.DrawText(tooltip, ThemeBase.FontMediumRight, Width - 314, tooltipPosY, theme.BlackBrush, 300);
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
            bool left  = e.Button.HasFlag(MouseButtons.Left);
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
