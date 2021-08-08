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

        Random rnd = new Random();
        GLBitmap bmp;
        GLBrush brush;

        public unsafe bool Redraw()
        {

            if (bmp == null)
            {
                bmp = gfx.CreateBitmapFromResource("Noise@2x");
                brush = new GLBrush(System.Drawing.Color.Blue);
            }

#if FALSE //FAMISTUDIO_ANDROID

            gfx.BeginDraw(new System.Drawing.Rectangle(sequencer.Left, sequencer.Top, sequencer.Width, sequencer.Height), height);
            sequencer.Render(gfx);
            sequencer.Validate();
            gfx.Clear(System.Drawing.Color.DarkGray);
            gfx.DrawRectangle(10, 10, 200, 200, brush);
            gfx.FillRectangle(250, 250, 500, 500, brush);
            gfx.DrawText("Hello World!", ThemeBase.FontHuge, 100, 800, brush);
            gfx.DrawBitmap(bmp, 100, 850, 100, 100, 1.0f);
            gfx.EndDraw();

            return true;
#else

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
                    gfx.BeginDraw(new System.Drawing.Rectangle(control.Left, control.Top, control.Width, control.Height), height);

                    var t0 = DateTime.Now;
                    for (int i = 0; i < 100; i++)
                        gfx.DrawText("The brown fox jumps over the lazy dog", ThemeBase.FontSmall, 0, 0, brush);
                    var t1 = DateTime.Now;
                    var str = $"Render time : {(t1 - t0).TotalMilliseconds} ms";

                    control.Render(gfx);
                    control.Validate();
                    gfx.DrawText(str, ThemeBase.FontHuge, 50, 50, brush);
                    gfx.EndDraw();
                }

                return true;
            }

            return false;
#endif
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
