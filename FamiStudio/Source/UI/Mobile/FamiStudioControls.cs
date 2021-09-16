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
        private QuickAccessBar  quickAccessBar;

        public Toolbar         ToolBar         => toolbar;
        public Sequencer       Sequencer       => sequencer;
        public PianoRoll       PianoRoll       => pianoRoll;
        public ProjectExplorer ProjectExplorer => projectExplorer;
        public QuickAccessBar  QuickAccessBar  => quickAccessBar;
        public GLControl       ActiveControl   => activeControl;

        public GLControl[] Controls => controls;
        public bool IsLandscape => width > height;

        public FamiStudioControls(FamiStudioForm parent)
        {
            toolbar         = new Toolbar();
            sequencer       = new Sequencer();
            pianoRoll       = new PianoRoll();
            projectExplorer = new ProjectExplorer();
            quickAccessBar  = new QuickAccessBar();

            controls[0] = sequencer;
            controls[1] = pianoRoll;
            controls[2] = projectExplorer;
            controls[3] = quickAccessBar;
            controls[4] = toolbar;

            activeControl = pianoRoll;

            foreach (var ctrl in controls)
                ctrl.ParentForm = parent;
        }

       public void SetActiveControl(GLControl ctrl, bool animate = true)
        {
            // DROIDTODO : Test mashing nav buttons quick.
            Debug.Assert(transitionTimer == 0.0f && transitionControl == null);

            if (activeControl != ctrl)
            {
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
            var landscape = IsLandscape;
            var quickAccessBarSize = quickAccessBar.LayoutSize;
            var toolLayoutSize = toolbar.LayoutSize;

            if (landscape)
            {
                if (!activeControlOnly)
                {
                    toolbar.Move(0, 0, toolLayoutSize, height);
                    quickAccessBar.Move(width - quickAccessBarSize, 0, quickAccessBarSize, height);
                }
                activeControl.Move(toolLayoutSize, 0, width - toolLayoutSize - quickAccessBarSize, height);
            }
            else
            {
                if (!activeControlOnly)
                {
                    toolbar.Move(0, 0, width, toolLayoutSize);
                    quickAccessBar.Move(0, height - quickAccessBarSize, width, quickAccessBarSize);
                }
                activeControl.Move(0, toolLayoutSize, width, height - toolLayoutSize - quickAccessBarSize);
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

            return anyNeedsRedraw;
        }

        GLBrush debugBrush;

        private void RenderControl(GLControl ctrl)
        {
            if (debugBrush == null)
                debugBrush = new GLBrush(System.Drawing.Color.SpringGreen);

            var fullscreenViewport = ctrl.WantsFullScreenViewport;

            if (fullscreenViewport)
                gfx.BeginDrawControl(new System.Drawing.Rectangle(0, 0, width, height), height);
            else
                gfx.BeginDrawControl(new System.Drawing.Rectangle(ctrl.Left, ctrl.Top, ctrl.Width, ctrl.Height), height);

            if (fullscreenViewport)
                gfx.Transform.PushTranslation(ctrl.Left, ctrl.Top);

            var t0 = DateTime.Now;
            ctrl.Render(gfx);
            var t1 = DateTime.Now;

            var cmd = gfx.CreateCommandList();
            cmd.DrawText($"{(t1 - t0).TotalMilliseconds}", res.FontVeryLargeBold, 10, 10, debugBrush);
            gfx.DrawCommandList(cmd);

            if (fullscreenViewport)
                gfx.Transform.PopTransform();

            gfx.EndDrawControl();

            ctrl.ClearDirtyFlag();
        }

        //private GLBrush GetShadowBrush(float alpha, bool horizontal, int sign)
        //{
        //    const int GradientSize = 100; // MATTT

        //    var color0 = System.Drawing.Color.FromArgb((byte)(224 * alpha), 0, 0, 0);
        //    var color1 = System.Drawing.Color.FromArgb((byte)(128 * alpha), 0, 0, 0);

        //    if (horizontal)
        //        return gfx.GetHorizontalGradientBrush(color0, color1, GradientSize * sign);
        //    else
        //        return gfx.GetVerticalGradientBrush(color0, color1, GradientSize * sign);
        //}

        //private void DrawVerticalDropShadow(GLCommandList c, int pos, bool up, float alpha)
        //{
        //    var brush = GetShadowBrush(alpha, false, up ? -1 : 1);

        //    if (up)
        //        c.FillRectangle(0, pos, width, 0, brush);
        //    else
        //        c.FillRectangle(0, pos, width, height, brush);
        //}

        //private void DrawHorizontalDropShadow(GLCommandList c, int pos, bool left, float alpha)
        //{
        //    var brush = GetShadowBrush(alpha, true, left ? -1 : 1);

        //    if (left)
        //        c.FillRectangle(pos, 0, 0, height, brush);
        //    else
        //        c.FillRectangle(pos, 0, width, height, brush);
        //}

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
                    activeControl.MarkDirty();
                    transitionControl = null;
                    UpdateLayout(true);
                }
            }
        }

        public bool Redraw()
        {
            gfx.BeginDrawFrame();
            {
                RenderControl(activeControl);
                RenderTransitionOverlay();

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
