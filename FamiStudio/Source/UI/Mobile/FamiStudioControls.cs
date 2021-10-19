using System;
using System.Diagnostics;

namespace FamiStudio
{
    public class FamiStudioControls
    {
        private const bool ShowRenderingTimes = false;

        private int width;
        private int height;

        private GLGraphics  gfx;
        private ThemeRenderResources res;

        private GLControl[] controls = new GLControl[6];
        private GLControl   transitionControl;
        private GLControl   activeControl;
        private float       transitionTimer;
        private bool        mobilePianoVisible = false;

        private Toolbar         toolbar;
        private Sequencer       sequencer;
        private PianoRoll       pianoRoll;
        private ProjectExplorer projectExplorer;
        private QuickAccessBar  quickAccessBar;
        private MobilePiano     mobilePiano;

        public Toolbar         ToolBar         => toolbar;
        public Sequencer       Sequencer       => sequencer;
        public PianoRoll       PianoRoll       => pianoRoll;
        public ProjectExplorer ProjectExplorer => projectExplorer;
        public QuickAccessBar  QuickAccessBar  => quickAccessBar;
        public MobilePiano     MobilePiano     => mobilePiano;
        public GLControl       ActiveControl   => activeControl;

        public GLControl[] Controls => controls;
        public bool IsLandscape => width > height;
        
        public bool MobilePianoVisible
        {
            get { return mobilePianoVisible; }
            set
            {
                mobilePianoVisible = value;
                UpdateLayout(false);
            }
        }

        public FamiStudioControls(FamiStudioForm parent)
        {
            toolbar         = new Toolbar();
            sequencer       = new Sequencer();
            pianoRoll       = new PianoRoll();
            projectExplorer = new ProjectExplorer();
            quickAccessBar  = new QuickAccessBar();
            mobilePiano     = new MobilePiano();

            controls[0] = sequencer;
            controls[1] = pianoRoll;
            controls[2] = projectExplorer;
            controls[3] = quickAccessBar;
            controls[4] = toolbar;
            controls[5] = mobilePiano;

            activeControl = sequencer;

            foreach (var ctrl in controls)
                ctrl.ParentForm = parent;
        }

       public void SetActiveControl(GLControl ctrl, bool animate = true)
        {
            if (activeControl != ctrl)
            {
                Debug.Assert(transitionTimer == 0.0f && transitionControl == null);

                if (animate)
                {
                    transitionControl = ctrl;
                    transitionTimer   = 1.0f;
                }
                else
                {
                    activeControl   = ctrl;
                    transitionTimer = 0.0f;
                }
            }
        }

        public void Resize(int w, int h)
        {
            width  = w;
            height = h;

            UpdateLayout(false);
        }

        private void UpdateLayout(bool activeControlOnly)
        {
            var landscape          = IsLandscape;
            var quickAccessBarSize = quickAccessBar.LayoutSize;
            var toolLayoutSize     = toolbar.LayoutSize;
            var pianoLayoutSize    = mobilePianoVisible ? mobilePiano.LayoutSize : 0;

            if (landscape)
            {
                if (!activeControlOnly)
                {
                    toolbar.Move(0, 0, toolLayoutSize, height);
                    quickAccessBar.Move(width - quickAccessBarSize, 0, quickAccessBarSize, height);
                    mobilePiano.Move(toolLayoutSize, height - pianoLayoutSize, width - toolLayoutSize - quickAccessBarSize, pianoLayoutSize);
                }

                activeControl.Move(toolLayoutSize, 0, width - toolLayoutSize - quickAccessBarSize, height - pianoLayoutSize);
            }
            else
            {
                if (!activeControlOnly)
                {
                    toolbar.Move(0, 0, width, toolLayoutSize);
                    quickAccessBar.Move(0, height - quickAccessBarSize - pianoLayoutSize, width, quickAccessBarSize);
                    mobilePiano.Move(0, height - pianoLayoutSize, width, pianoLayoutSize);
                }
                activeControl.Move(0, toolLayoutSize, width, height - toolLayoutSize - quickAccessBarSize - pianoLayoutSize);
            }
        }

        private bool IsPointInControl(GLControl ctrl, int x, int y, out int ctrlX, out int ctrlY)
        {
            ctrlX = x - ctrl.Left;
            ctrlY = y - ctrl.Top;

            if (ctrlX >= 0 &&
                ctrlY >= 0 &&
                ctrlX < ctrl.Width &&
                ctrlY < ctrl.Height)
            {
                return true;
            }

            return false;
        }

        public bool CanAcceptInput
        {
            get
            {
                return transitionTimer == 0.0f && transitionControl == null;
            }
        }

        public GLControl GetControlAtCoord(int formX, int formY, out int ctrlX, out int ctrlY)
        {
            if (!CanAcceptInput)
            {
                ctrlX = 0;
                ctrlY = 0;
                return null;
            }

            // These 2 are allowed to steal the input when they are expanded.
            if (quickAccessBar.IsExpanded)
            {
                IsPointInControl(quickAccessBar, formX, formY, out ctrlX, out ctrlY);
                return quickAccessBar;
            }

            if (toolbar.IsExpanded)
            {
                IsPointInControl(toolbar, formX, formY, out ctrlX, out ctrlY);
                return toolbar;
            }

            if (mobilePianoVisible)
            {
                if (IsPointInControl(mobilePiano, formX, formY, out ctrlX, out ctrlY))
                    return mobilePiano;
            }

            if (IsPointInControl(activeControl, formX, formY, out ctrlX, out ctrlY))
                return activeControl;
            if (IsPointInControl(quickAccessBar, formX, formY, out ctrlX, out ctrlY))
                return quickAccessBar;
            if (IsPointInControl(toolbar, formX, formY, out ctrlX, out ctrlY))
                return toolbar;

            return null;
        }

        public void MarkDirty()
        {
            foreach (var ctrl in controls)
                ctrl.MarkDirty();
        }

        public bool NeedsRedraw()
        {
            bool anyNeedsRedraw = transitionTimer > 0.0f;

            anyNeedsRedraw |= activeControl.NeedsRedraw;
            anyNeedsRedraw |= quickAccessBar.NeedsRedraw;
            anyNeedsRedraw |= toolbar.NeedsRedraw;
            if (mobilePianoVisible)
                anyNeedsRedraw |= mobilePiano.NeedsRedraw;

            return anyNeedsRedraw;
        }

        private void RenderControl(GLControl ctrl)
        {
            var fullscreenViewport = ctrl.WantsFullScreenViewport;

            if (fullscreenViewport)
                gfx.BeginDrawControl(new System.Drawing.Rectangle(0, 0, width, height), height);
            else
                gfx.BeginDrawControl(new System.Drawing.Rectangle(ctrl.Left, ctrl.Top, ctrl.Width, ctrl.Height), height);

            gfx.SetLineBias(2);

            if (fullscreenViewport)
                gfx.Transform.PushTranslation(ctrl.Left, ctrl.Top);

            var t0 = DateTime.Now;
            ctrl.Render(gfx);
            var t1 = DateTime.Now;

            if (ShowRenderingTimes)
            {
                var cmd = gfx.CreateCommandList();
                cmd.DrawText($"{(t1 - t0).TotalMilliseconds}", res.FontVeryLargeBold, 10, 10, gfx.GetSolidBrush(System.Drawing.Color.SpringGreen));
                gfx.DrawCommandList(cmd);
            }

            if (fullscreenViewport)
                gfx.Transform.PopTransform();

            gfx.EndDrawControl();

            ctrl.ClearDirtyFlag();
        }

        private void RenderTransitionOverlay()
        {
            if (transitionTimer > 0.0f)
            {
                gfx.BeginDrawControl(new System.Drawing.Rectangle(0, 0, width, height), height);

                var cmd = gfx.CreateCommandList();
                var alpha = (byte)((1.0f - Math.Abs(transitionTimer - 0.5f) * 2) * 255);
                var brush = gfx.GetSolidBrush(System.Drawing.Color.FromArgb(alpha, Theme.DarkGreyFillColor1));

                cmd.FillRectangle(activeControl.Left, activeControl.Top, activeControl.Right, activeControl.Bottom, brush);

                gfx.DrawCommandList(cmd);
                gfx.EndDrawControl();
            }
        }

        public void Tick(float timeDelta)
        {
            if (transitionTimer > 0.0f)
            {
                var prevTimer = transitionTimer;
                transitionTimer = Math.Max(0.0f, transitionTimer - timeDelta * 6);

                if (prevTimer > 0.5f && transitionTimer <= 0.5f)
                {
                    activeControl = transitionControl;
                    transitionControl = null;
                    UpdateLayout(true);
                }

                MarkDirty();
            }
        }

        public bool Redraw()
        {
            gfx.BeginDrawFrame();
            {
                RenderControl(activeControl);
                RenderTransitionOverlay();

                if (mobilePianoVisible)
                {
                    RenderControl(mobilePiano);
                }

                if (toolbar.IsExpanded)
                {
                    RenderControl(quickAccessBar);
                    RenderControl(toolbar);
                }
                else
                {
                    RenderControl(toolbar);
                    RenderControl(quickAccessBar);
                }
            }
            gfx.EndDrawFrame();

            return false;
        }

        public void InitializeGL()
        {
            Debug.Assert(gfx == null);

            gfx = new GLGraphics(DpiScaling.MainWindow, DpiScaling.Font);
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
