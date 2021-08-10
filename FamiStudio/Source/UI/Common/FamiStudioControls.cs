using System;
using System.Diagnostics;

#if FAMISTUDIO_ANDROID
using Android.Opengl;
using Javax.Microedition.Khronos.Opengles;
#else
using OpenTK.Graphics.OpenGL;
#endif

namespace FamiStudio
{
    public class FamiStudioControls
    {
        const int ControlToolbar = 0;
        const int ControlSequencer = 1;
        const int ControlPianoRoll = 2;
        const int ControlProjectExplorer = 3;

        private int width;
        private int height;
        private GLGraphics gfx;
        private GLControl[] controls = new GLControl[4];

        private Toolbar toolbar;
        private Sequencer sequencer;
        private PianoRoll pianoRoll;
        private ProjectExplorer projectExplorer;

        public Toolbar ToolBar => toolbar;
        public Sequencer Sequencer => sequencer;
        public PianoRoll PianoRoll => pianoRoll;
        public ProjectExplorer ProjectExplorer => projectExplorer;
        public GLControl[] Controls => controls;

        public FamiStudioControls(FamiStudioForm parent)
        {
            toolbar = new Toolbar();
            sequencer = new Sequencer();
            pianoRoll = new PianoRoll();
            projectExplorer = new ProjectExplorer();

            controls[ControlToolbar] = toolbar;
            controls[ControlSequencer] = sequencer;
            controls[ControlPianoRoll] = pianoRoll;
            controls[ControlProjectExplorer] = projectExplorer;

            foreach (var ctrl in controls)
                ctrl.ParentForm = parent;
        }

        public void Resize(int w, int h)
        {
            width  = w;
            height = h;

            int toolBarHeight = (int)(40 * GLTheme.MainWindowScaling);
            int projectExplorerWidth = (int)(280 * GLTheme.MainWindowScaling);
            int sequencerHeight = pianoRoll.IsMaximized ? 1 : (int)(sequencer.ComputeDesiredSizeY() * GLTheme.MainWindowScaling);

            toolbar.Move(0, 0, width, toolBarHeight);
            projectExplorer.Move(width - projectExplorerWidth, toolBarHeight, projectExplorerWidth, height - toolBarHeight);
            sequencer.Move(0, toolBarHeight, width - projectExplorerWidth, sequencerHeight);
            pianoRoll.Move(0, toolBarHeight + sequencerHeight, width - projectExplorerWidth, height - toolBarHeight - sequencerHeight);
        }

        public GLControl GetControlAtCoord(int formX, int formY, out int ctrlX, out int ctrlY)
        {
            foreach (var ctrl in controls)
            {
                ctrlX = formX - ctrl.Left;
                ctrlY = formY - ctrl.Top;

                if (ctrlX >= 0 &&
                    ctrlY >= 0 &&
                    ctrlX <  ctrl.Width &&
                    ctrlY <  ctrl.Height)
                {
                    return ctrl;
                }
            }

            ctrlX = 0;
            ctrlY = 0;
            return null;
        }

        public void Invalidate()
        {
            foreach (var ctrl in controls)
                ctrl.Invalidate();
        }

        GLBrush brush;

        public unsafe bool Redraw()
        {
            if (brush == null)
                brush = new GLBrush(System.Drawing.Color.SpringGreen);

            bool anyNeedsRedraw = false;
            foreach (var control in controls)
            {
                anyNeedsRedraw |= control.NeedsRedraw;
            }

            anyNeedsRedraw = true; // MATTT DROIDTODO!

            if (anyNeedsRedraw)
            {
                // Tentative fix for a bug when NSF dialog is open that I can no longer repro.
                if (controls[0].App.Project == null)
                    return true;


                foreach (var control in controls)
                {
#if FAMISTUDIO_WINDOWS
                    var t0 = PerformanceCounter.TimeSeconds();
#else
                    var t0 = DateTime.Now;
#endif
                    gfx.BeginDraw(new System.Drawing.Rectangle(control.Left, control.Top, control.Width, control.Height), height);
                    control.Render(gfx);
                    control.Validate();

                    var cmd = gfx.CreateCommandList();
#if FAMISTUDIO_WINDOWS
                    var t1 = PerformanceCounter.TimeSeconds();
                    cmd.DrawText($"Render time : {(t1 - t0) * 1000} ms", ThemeBase.FontBigBold, 10, 10, brush);
#else
                    var t1 = DateTime.Now;
                    cmd.DrawText($"Render time : {(t1 - t0).TotalMilliseconds} ms", ThemeBase.FontBigBold, 10, 10, brush);
#endif
                    gfx.DrawCommandList(cmd);
                    gfx.EndDraw();
                }

                return true;
            }

            return false;
        }

#if FAMISTUDIO_ANDROID
        public void InitializeGL(IGL10 gl)
        {
            gfx = new GLGraphics(gl);
            foreach (var ctrl in controls)
                ctrl.RenderInitialized(gfx);
        }
#else
        public void InitializeGL()
        {
            gfx = new GLGraphics();
            foreach (var ctrl in controls)
                ctrl.RenderInitialized(gfx);
        }
#endif
    }
}
