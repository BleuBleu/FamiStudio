using System;
using System.Diagnostics;
using OpenTK.Graphics.OpenGL;

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
            int sequencerHeight = (int)(sequencer.ComputeDesiredSizeY() * GLTheme.MainWindowScaling);

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

        Random rnd = new Random();

        public bool Redraw()
        {
            bool anyNeedsRedraw = false;
            foreach (var control in controls)
            {
                anyNeedsRedraw |= control.NeedsRedraw;
            }

            if (anyNeedsRedraw)
            {
                //Debug.WriteLine($"REDRAW! {width} {height}");

                GL.Viewport(0, 0, width, height);
                //GL.ClearColor(1.0f, (float)rnd.NextDouble(), 1.0f, 1.0f);
                //GL.Clear(ClearBufferMask.ColorBufferBit);

                // Tentative fix for a bug when NSF dialog is open that I can no longer repro.
                if (controls[0].App.Project == null)
                    return true;

                foreach (var control in controls)
                {
                    gfx.BeginDraw(control, height);
                    control.Render(gfx);
                    control.Validate();
                    gfx.EndDraw();
                }

                return true;
            }

            return false;
        }


        public void InitializeGL(FamiStudioForm form)
        {
            gfx = new GLGraphics();
            foreach (var ctrl in controls)
                ctrl.RenderInitialized(gfx);
        }
    }
}
