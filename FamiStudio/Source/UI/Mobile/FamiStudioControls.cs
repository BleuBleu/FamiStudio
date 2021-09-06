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
        }

        private void UpdateLayout()
        {
            var landscape = IsLandscape;
            var quickAccessBarSize = quickAccessBar.LayoutSize;
            var toolLayoutSize = toolbar.LayoutSize;

            // Toolbar will be resized every frame anyway.
            if (landscape)
            {
                toolbar.Move(0, 0, toolLayoutSize, height);
                activeControl.Move(toolLayoutSize, 0, width - toolLayoutSize - quickAccessBarSize, height);
                quickAccessBar.Move(width - quickAccessBarSize, 0, quickAccessBarSize, height);
            }
            else
            {
                toolbar.Move(0, 0, width, toolLayoutSize);
                activeControl.Move(0, toolLayoutSize, width, height - toolLayoutSize - quickAccessBarSize);
                quickAccessBar.Move(0, height - quickAccessBarSize, width, quickAccessBarSize);
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

        public GLControl GetControlAtCoord(int formX, int formY, out int ctrlX, out int ctrlY)
        {
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

        private void RenderControl(GLControl ctrl, bool fullscreenViewport = false)
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

                ctrl.ClearDirtyFlag();
            }
            var t1 = DateTime.Now;

            var cmd = gfx.CreateCommandList();
            cmd.DrawText($"{(t1 - t0).TotalMilliseconds}", res.FontVeryLargeBold, 10, 10, debugBrush);
            gfx.DrawCommandList(cmd);

            gfx.EndDrawControl();
        }

        private GLBrush GetShadowBrush(float alpha, bool horizontal, int sign)
        {
            const int GradientSize = 100; // MATTT

            var color0 = System.Drawing.Color.FromArgb((byte)(224 * alpha), 0, 0, 0);
            var color1 = System.Drawing.Color.FromArgb((byte)(128 * alpha), 0, 0, 0);

            if (horizontal)
                return gfx.GetHorizontalGradientBrush(color0, color1, GradientSize * sign);
            else
                return gfx.GetVerticalGradientBrush(color0, color1, GradientSize * sign);
        }

        private void DrawVerticalDropShadow(GLCommandList c, int pos, bool up, float alpha)
        {
            var brush = GetShadowBrush(alpha, false, up ? -1 : 1);

            if (up)
                c.FillRectangle(0, pos, width, 0, brush);
            else
                c.FillRectangle(0, pos, width, height, brush);
        }

        private void DrawHorizontalDropShadow(GLCommandList c, int pos, bool left, float alpha)
        {
            var brush = GetShadowBrush(alpha, true, left ? -1 : 1);

            if (left)
                c.FillRectangle(pos, 0, 0, height, brush);
            else
                c.FillRectangle(pos, 0, width, height, brush);
        }

        private void RenderOverlay()
        {
            gfx.BeginDrawControl(new System.Drawing.Rectangle(0, 0, width, height), height);

            var cmd = gfx.CreateCommandList();

            if (transitionTimer > 0.0f)
            {
                var alpha = (byte)((1.0f - Math.Abs(transitionTimer - 0.5f) * 2) * 255);
                var brush = gfx.GetSolidBrush(System.Drawing.Color.FromArgb(alpha, Theme.DarkGreyFillColor1));

                cmd.FillRectangle(activeControl.Left, activeControl.Top, activeControl.Right, activeControl.Bottom, brush);
            }

            if (toolbar.IsExpanded)
            {
                if (IsLandscape)
                    DrawHorizontalDropShadow(cmd, toolbar.RenderSize, false, toolbar.ExpandRatio);
                else
                    DrawVerticalDropShadow(cmd, toolbar.RenderSize, false, toolbar.ExpandRatio);
            }

            if (quickAccessBar.IsExpanded)
            {
                if (IsLandscape)
                    DrawHorizontalDropShadow(cmd, width - quickAccessBar.RenderSize, true, quickAccessBar.ExpandRatio);
                else
                    DrawVerticalDropShadow(cmd, height - quickAccessBar.RenderSize, true, quickAccessBar.ExpandRatio);
            }

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
                    activeControl.MarkDirty();
                    transitionControl = null;
                    UpdateLayout();
                }
            }
        }

        public bool Redraw()
        {
            UpdateTimeDelta();
            UpdateTransition();
            UpdateQuickAccess(); // MATTT : Should we tick with the other controls?

            gfx.BeginDrawFrame();
            {
                RenderControl(activeControl);
                RenderControl(quickAccessBar, true);
                RenderControl(toolbar, true);
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
