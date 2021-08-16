using System;
using System.Diagnostics;
using Android.Opengl;
using Javax.Microedition.Khronos.Opengles;

namespace FamiStudio
{
    public class FamiStudioControls
    {
        private int width;
        private int height;
        private GLGraphics gfx;
        private GLControl[] controls = new GLControl[4];
        private GLControl activeControl;

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

            controls[0] = toolbar;
            controls[1] = sequencer;
            controls[2] = pianoRoll;
            controls[3] = projectExplorer;

            activeControl = pianoRoll;

            foreach (var ctrl in controls)
                ctrl.ParentForm = parent;
        }

        public void Resize(int w, int h)
        {
            width  = w;
            height = h;

            var landscape = w > h;

            if (landscape)
            {
                activeControl.Move(0, 0, width - 539, height);
                toolbar.Move(width - 539, 0, 539, height);
            }
            else
            {
                activeControl.Move(0, 0, width, height - 539);
                toolbar.Move(0, height - 539, width, 539);
            }

            /*
            int toolBarHeight = (int)(40 * GLTheme.MainWindowScaling);
            int projectExplorerWidth = (int)(280 * GLTheme.MainWindowScaling);
            int sequencerHeight = pianoRoll.IsMaximized ? 1 : (int)(sequencer.ComputeDesiredSizeY() * GLTheme.MainWindowScaling);

            toolbar.Move(0, 0, width, toolBarHeight);
            projectExplorer.Move(width - projectExplorerWidth, toolBarHeight, projectExplorerWidth, height - toolBarHeight);
            sequencer.Move(0, toolBarHeight, width - projectExplorerWidth, sequencerHeight);
            pianoRoll.Move(0, toolBarHeight + sequencerHeight, width - projectExplorerWidth, height - toolBarHeight - sequencerHeight);
            */
        }

        public GLControl GetControlAtCoord(int formX, int formY, out int ctrlX, out int ctrlY)
        {
            // DROIDTODO : Only allow picking active control for piano roll / seq / project explorer.
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

        GLBrush debugBrush;

        private void RedrawControl(GLControl ctrl)
        {
            if (debugBrush == null)
                debugBrush = new GLBrush(System.Drawing.Color.SpringGreen);

            gfx.BeginDrawControl(new System.Drawing.Rectangle(ctrl.Left, ctrl.Top, ctrl.Width, ctrl.Height), height);

            var t0 = DateTime.Now;
            ctrl.Render(gfx);
            ctrl.Validate();

            var cmd = gfx.CreateCommandList();
            var t1 = DateTime.Now;
            cmd.DrawText($"Render time : {(t1 - t0).TotalMilliseconds} ms", ThemeBase.FontBigBold, 10, 10, debugBrush);
            gfx.DrawCommandList(cmd);

            gfx.EndDrawControl();
        }

        public bool Redraw()
        {
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

                gfx.BeginDrawFrame();
                RedrawControl(activeControl);
                RedrawControl(toolbar);
                gfx.EndDrawFrame();

                return true;
            }

            return false;
        }

        public void InitializeGL(IGL10 gl)
        {
            gfx = new GLGraphics(gl);
            foreach (var ctrl in controls)
                ctrl.RenderInitialized(gfx);
        }
    }
}
