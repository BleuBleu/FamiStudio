using System;
using System.Diagnostics;

namespace FamiStudio
{
    public class FamiStudioContainer : Container
    {
        private const bool ShowRenderingTimes = false;

        private int width;
        private int height;

        private Control transitionControl;
        private Control activeControl;
        private float   transitionTimer;
        private bool    mobilePianoVisible = false;

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
        public Control         ActiveControl   => activeControl;

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

        public FamiStudioContainer(FamiStudioWindow parent)
        {
            window = parent;
            toolbar = new Toolbar();
            sequencer = new Sequencer();
            pianoRoll = new PianoRoll();
            projectExplorer = new ProjectExplorer();
            quickAccessBar = new QuickAccessBar();
            mobilePiano = new MobilePiano();
            activeControl = sequencer;

            pianoRoll.Visible = false;
            projectExplorer.Visible = false;
            mobilePiano.Visible = false;

            AddControl(sequencer);
            AddControl(pianoRoll);
            AddControl(projectExplorer);
            AddControl(quickAccessBar);
            AddControl(toolbar);
            AddControl(mobilePiano);
        }

        // CTRLTODO : Show/hide controls as needed.
        public void SetActiveControl(Control ctrl, bool animate = true)
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

                // Always update the piano roll since we draw the view range in the sequencer and 
                // it requires valid size to do that.
                if (activeControl != pianoRoll)
                    pianoRoll.Move(toolLayoutSize, 0, width - toolLayoutSize - quickAccessBarSize, height - pianoLayoutSize);
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

                // Always update the piano roll since we draw the view range in the sequencer and 
                // it requires valid size to do that.
                if (activeControl != pianoRoll)
                    pianoRoll.Move(0, toolLayoutSize, width, height - toolLayoutSize - quickAccessBarSize - pianoLayoutSize);
            }
        }

        // CTRLTODO : Change!
        public bool CanAcceptInput
        {
            get
            {
                return transitionTimer == 0.0f && transitionControl == null;
            }
        }

        // CTRLTODO : Change!
        //public Control GetControlAtCoord(int formX, int formY, out int ctrlX, out int ctrlY)
        //{
        //    if (!CanAcceptInput)
        //    {
        //        ctrlX = 0;
        //        ctrlY = 0;
        //        return null;
        //    }

        //    // These 2 are allowed to steal the input when they are expanded.
        //    if (quickAccessBar.IsExpanded)
        //    {
        //        IsPointInControl(quickAccessBar, formX, formY, out ctrlX, out ctrlY);
        //        return quickAccessBar;
        //    }

        //    if (toolbar.IsExpanded)
        //    {
        //        IsPointInControl(toolbar, formX, formY, out ctrlX, out ctrlY);
        //        return toolbar;
        //    }

        //    if (mobilePianoVisible)
        //    {
        //        if (IsPointInControl(mobilePiano, formX, formY, out ctrlX, out ctrlY))
        //            return mobilePiano;
        //    }

        //    if (IsPointInControl(activeControl, formX, formY, out ctrlX, out ctrlY))
        //        return activeControl;
        //    if (IsPointInControl(quickAccessBar, formX, formY, out ctrlX, out ctrlY))
        //        return quickAccessBar;
        //    if (IsPointInControl(toolbar, formX, formY, out ctrlX, out ctrlY))
        //        return toolbar;

        //    return null;
        //}

        // GLTODO : Migrate.
        /*
        private void RenderControl(Control ctrl)
        {
            var fullscreenViewport = ctrl.WantsFullScreenViewport;

            if (fullscreenViewport)
                gfx.BeginDrawControl(new Rectangle(0, 0, width, height), height);
            else
                gfx.BeginDrawControl(new Rectangle(ctrl.WindowLeft, ctrl.WindowTop, ctrl.Width, ctrl.Height), height);

            gfx.SetLineBias(2);

            if (fullscreenViewport)
                gfx.Transform.PushTranslation(ctrl.WindowLeft, ctrl.WindowTop);

            var t0 = DateTime.Now;
            ctrl.Render(gfx);
            var t1 = DateTime.Now;

            if (ShowRenderingTimes)
            {
                var cmd = gfx.CreateCommandList();
                cmd.DrawText($"{(t1 - t0).TotalMilliseconds}", fontRes.FontVeryLargeBold, 10, 10, Color.SpringGreen);
                gfx.DrawCommandList(cmd);
            }

            if (fullscreenViewport)
                gfx.Transform.PopTransform();

            gfx.EndDrawControl();

            ctrl.ClearDirtyFlag();
        }
        */

        private void RenderTransitionOverlay()
        {
            // GLTODO : Migrate.
            /*
            if (transitionTimer > 0.0f)
            {
                gfx.BeginDrawControl(new Rectangle(0, 0, width, height), height);

                var cmd = gfx.CreateCommandList();
                var alpha = (byte)((1.0f - Math.Abs(transitionTimer - 0.5f) * 2) * 255);
                var color = Color.FromArgb(alpha, Theme.DarkGreyColor4);

                cmd.FillRectangle(activeControl.WindowLeft, activeControl.WindowTop, activeControl.WindowRight, activeControl.WindowBottom, color);

                gfx.DrawCommandList(cmd);
                gfx.EndDrawControl();
            }
            */
        }

        public override void Tick(float delta)
        {
            base.Tick(delta);

            if (transitionTimer > 0.0f)
            {
                var prevTimer = transitionTimer;
                transitionTimer = Math.Max(0.0f, transitionTimer - delta * 6);

                if (prevTimer > 0.5f && transitionTimer <= 0.5f)
                {
                    activeControl = transitionControl;
                    transitionControl = null;
                    UpdateLayout(true);
                }

                MarkDirty();
            }
        }

        // GLTODO : This wont be needed.
        //public bool Redraw()
        //{
        //    gfx.BeginDrawFrame();
        //    {
        //        RenderControl(activeControl);
        //        RenderTransitionOverlay();

        //        if (mobilePianoVisible)
        //        {
        //            RenderControl(mobilePiano);
        //        }

        //        if (toolbar.IsExpanded)
        //        {
        //            RenderControl(quickAccessBar);
        //            RenderControl(toolbar);
        //        }
        //        else
        //        {
        //            RenderControl(toolbar);
        //            RenderControl(quickAccessBar);
        //        }
        //    }
        //    gfx.EndDrawFrame();

        //    return false;
        //}
    }
}
