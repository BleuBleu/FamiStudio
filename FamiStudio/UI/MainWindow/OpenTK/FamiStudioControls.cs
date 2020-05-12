using System;
using System.Diagnostics;
using OpenTK.Graphics.OpenGL;

namespace FamiStudio
{
    public class FamiStudioControls
    {
        const int ControlToolbar = 0;
        const int ControlSequener = 1;
        const int ControlPianoRoll = 2;
        const int ControlProjectExplorer = 3;

        private GLGraphics gfx = new GLGraphics();
        private GLControl[] controls = new GLControl[4];
        private GLControl captureControl;
        private System.Windows.Forms.MouseButtons captureButtons;

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
            controls[ControlSequener] = sequencer;
            controls[ControlPianoRoll] = pianoRoll;
            controls[ControlProjectExplorer] = projectExplorer;

            foreach (var ctrl in controls)
                ctrl.ParentForm = parent;
        }

        public void Resize(int width, int height)
        {
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

        public bool Redraw(int width, int height)
        {
            bool anyNeedsRedraw = false;
            foreach (var control in controls)
            {
                anyNeedsRedraw |= control.NeedsRedraw;
            }

            if (anyNeedsRedraw)
            {
                Debug.WriteLine("REDRAW!");

#if FAMISTUDIO_LINUX
                GL.Viewport(0, 0, width, height);
                GL.Clear(ClearBufferMask.ColorBufferBit);
#endif

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
            foreach (var ctrl in controls)
                ctrl.RenderInitialized(gfx);
        }
    }
}
