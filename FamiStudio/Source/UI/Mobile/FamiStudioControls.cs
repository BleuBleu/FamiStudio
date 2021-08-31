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

        private float timeDelta = 0.0f;
        private DateTime lastTime = DateTime.MinValue;

        private GLGraphics  gfx;
        private ThemeRenderResources res;

        private GLControl[] controls = new GLControl[5];
        private GLControl   transitionControl;
        private GLControl   activeControl;
        private float       transitionTimer;

        private Toolbar         toolbar;
        private Sequencer       sequencer;
        private PianoRoll       pianoRoll;
        private ProjectExplorer projectExplorer;
        private QuickAcessBar   quickAccessBar;

        public Toolbar         ToolBar         => toolbar;
        public Sequencer       Sequencer       => sequencer;
        public PianoRoll       PianoRoll       => pianoRoll;
        public ProjectExplorer ProjectExplorer => projectExplorer;
        //public NavigationBar   NavigationBar   => quickAccessBar;

        public GLControl[] Controls => controls;
        public bool IsLandscape => width > height;

        public FamiStudioControls(FamiStudioForm parent)
        {
            toolbar         = new Toolbar();
            sequencer       = new Sequencer();
            pianoRoll       = new PianoRoll();
            projectExplorer = new ProjectExplorer();
            quickAccessBar  = new QuickAcessBar();

            controls[0] = sequencer;
            controls[1] = pianoRoll;
            controls[2] = projectExplorer;
            controls[3] = quickAccessBar;
            controls[4] = toolbar;

            quickAccessBar.PianoRollClicked += NavigationBar_PianoRollClicked;
            quickAccessBar.SequencerClicked += NavigationBar_SequencerClicked;
            quickAccessBar.ProjectExplorerClicked += NavigationBar_ProjectExplorerClicked;

            activeControl = pianoRoll;

            foreach (var ctrl in controls)
                ctrl.ParentForm = parent;
        }

        private void NavigationBar_SequencerClicked()
        {
            TransitionToControl(sequencer);
        }

        private void NavigationBar_PianoRollClicked()
        {
            TransitionToControl(pianoRoll);
        }

        private void NavigationBar_ProjectExplorerClicked()
        {
            TransitionToControl(projectExplorer);
        }

        private void TransitionToControl(GLControl ctrl)
        {
            if (activeControl != ctrl)
            {
                transitionControl = ctrl;
                transitionTimer = 1.0f;
            }
        }

        public void Resize(int w, int h)
        {
            width  = w;
            height = h;

            UpdateLayout();
            UpdateToolbar(true);
        }

        private void UpdateLayout()
        {
            var landscape = IsLandscape;
            var quickAccessBarSize = quickAccessBar.LayoutSize;
            var toolLayoutSize = toolbar.LayoutSize;

            // Toolbar will be resized every frame anyway.
            if (landscape)
            {
                activeControl.Move(toolLayoutSize, 0, width - toolLayoutSize - quickAccessBarSize, height);
                quickAccessBar.Move(width - quickAccessBarSize, 0, quickAccessBarSize, height);
            }
            else
            {
                activeControl.Move(0, toolLayoutSize, width, height - toolLayoutSize - quickAccessBarSize);
                quickAccessBar.Move(0, height - quickAccessBarSize, width, quickAccessBarSize);
            }
        }

        // MATTT : Use a fullscreen viewport instead.
        private void UpdateToolbar(bool fireEvent = false)
        {
            var toolActualSize = toolbar.DesiredSize;

            if (IsLandscape)
                toolbar.Move(0, 0, toolActualSize, height, fireEvent);
            else
                toolbar.Move(0, 0, width, toolActualSize, fireEvent);
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

        private void RedrawControl(GLControl ctrl, bool fullscreenViewport = false)
        {
            if (debugBrush == null)
                debugBrush = new GLBrush(System.Drawing.Color.SpringGreen);

            if (fullscreenViewport)
                gfx.BeginDrawControl(new System.Drawing.Rectangle(0, 0, width, height), height);
            else
                gfx.BeginDrawControl(new System.Drawing.Rectangle(ctrl.Left, ctrl.Top, ctrl.Width, ctrl.Height), height);

            var t0 = DateTime.Now;
            {
                if (fullscreenViewport)
                    gfx.Transform.PushTranslation(ctrl.Left, ctrl.Top);

                ctrl.Render(gfx);

                if (fullscreenViewport)
                    gfx.Transform.PopTransform();

                ctrl.Validate();
            }
            var t1 = DateTime.Now;

            var cmd = gfx.CreateCommandList();
            cmd.DrawText($"{(t1 - t0).TotalMilliseconds}", res.FontVeryLargeBold, 10, 10, debugBrush);
            gfx.DrawCommandList(cmd);

            gfx.EndDrawControl();
        }

        // MATTT : Move this to a RenderOverlay function on toolbar
        private void RenderOverlay()
        {
            gfx.BeginDrawControl(new System.Drawing.Rectangle(0, 0, width, height), height);

            var cmd = gfx.CreateCommandList();

            if (toolbar.ExpandRatio > 0.001f)
            {
                var size = MobileUtils.ComputeIdealButtonSize(width, height);
                var brush = gfx.CreateVerticalGradientBrush(
                    0, size,
                    System.Drawing.Color.FromArgb((byte)(224 * toolbar.ExpandRatio), 0, 0, 0),
                    System.Drawing.Color.FromArgb((byte)(128 * toolbar.ExpandRatio), 0, 0, 0));

                cmd.FillRectangle(toolbar.Left, toolbar.Bottom, toolbar.Right, height, brush);
            }

            if (transitionTimer > 0.0f)
            {
                var alpha = (byte)((1.0f - Math.Abs(transitionTimer - 0.5f) * 2) * 255);
                var brush = gfx.CreateSolidBrush(System.Drawing.Color.FromArgb(alpha, Theme.DarkGreyFillColor1));

                cmd.FillRectangle(activeControl.Left, activeControl.Top, activeControl.Right, activeControl.Bottom, brush);
            }

            //cmd.DrawLine(toolbar.Left, toolbar.Bottom, toolbar.Right, toolbar.Bottom, res.BlackBrush, 3.0f);

            //if (IsLandscape)
            //    cmd.DrawLine(quickAccessBar.Right, quickAccessBar.Top, quickAccessBar.Right, quickAccessBar.Bottom, res.BlackBrush, 3.0f);
            //else
            //    cmd.DrawLine(quickAccessBar.Right, quickAccessBar.Top, quickAccessBar.Left, quickAccessBar.Top, res.BlackBrush, 3.0f);

            gfx.DrawCommandList(cmd);
            gfx.EndDrawControl();
        }

        private void UpdateTimeDelta()
        {
            if (lastTime == DateTime.MinValue)
                lastTime = DateTime.Now;

            var currTime = DateTime.Now;

            timeDelta = Math.Min(0.25f, (float)(currTime - lastTime).TotalSeconds);
            lastTime = currTime;
        }

        private void UpdateQuickAccess()
        {
            quickAccessBar.Tick(timeDelta);
        }

        private void UpdateTransition()
        {
            if (transitionTimer > 0.0f)
            {
                var prevTimer = transitionTimer;
                transitionTimer = Math.Max(0.0f, transitionTimer - timeDelta * 6);

                if (prevTimer > 0.5f && transitionTimer <= 0.5f)
                {
                    activeControl = transitionControl;
                    transitionControl = null;
                    UpdateLayout();
                }
            }
        }

        public bool Redraw()
        {
            UpdateTimeDelta();
            UpdateTransition();
            UpdateToolbar();
            UpdateQuickAccess(); // MATTT : Should we tick with the other controls?

            gfx.BeginDrawFrame();
            {
                RedrawControl(activeControl);
                RedrawControl(quickAccessBar, true);
                RedrawControl(toolbar);
                RenderOverlay();
            }
            gfx.EndDrawFrame();

            return false;
        }

        public void InitializeGL(IGL10 gl)
        {
            gfx = new GLGraphics(gl, DpiScaling.MainWindow, DpiScaling.Font);
            res = new ThemeRenderResources(gfx);
            
            foreach (var ctrl in controls)
            {
                ctrl.SetDpiScales(DpiScaling.MainWindow, DpiScaling.Font);
                ctrl.SetThemeRenderResource(res);
                ctrl.RenderInitialized(gfx);
            }
        }
    }
}
