using System;
using System.Diagnostics;

namespace FamiStudio
{
    public class FamiStudioContainer : Container
    {
        private const bool ShowRenderingTimes = false;

        private Control transitionControl;
        private Control activeControl;
        private float   transitionTimer;

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

        public bool MobilePianoVisible
        {
            get { return mobilePiano.Visible; }
            set
            {
                mobilePiano.Visible = value;
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
            AddControl(mobilePiano);
            AddControl(toolbar); // Toolbar + quickaccess need to be last, draws on top of everything.
            AddControl(quickAccessBar);

            SetTickEnabled(true);
        }

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

        protected override void OnResize(EventArgs e)
        {
            UpdateLayout(false);
        }

        public void UpdateLayout(bool activeControlOnly = false)
        {
            var landscape          = IsLandscape;
            var quickAccessBarSize = quickAccessBar.LayoutSize;
            var toolbarLayoutSize  = toolbar.LayoutSize;
            var pianoLayoutSize    = mobilePiano.Visible ? mobilePiano.LayoutSize : 0;

            if (landscape)
            {
                if (!activeControlOnly)
                {
                    toolbar.Move(0, 0, toolbarLayoutSize, height);
                    quickAccessBar.Move(width - quickAccessBarSize, 0, quickAccessBarSize, height);
                    mobilePiano.Move(toolbarLayoutSize, height - pianoLayoutSize, width - toolbarLayoutSize - quickAccessBarSize, pianoLayoutSize);
                }

                activeControl.Move(toolbarLayoutSize, 0, width - toolbarLayoutSize - quickAccessBarSize, height - pianoLayoutSize);

                // Always update the piano roll since we draw the view range in the sequencer and 
                // it requires valid size to do that.
                if (activeControl != pianoRoll)
                    pianoRoll.Move(toolbarLayoutSize, 0, width - toolbarLayoutSize - quickAccessBarSize, height - pianoLayoutSize);
            }
            else
            {
                if (!activeControlOnly)
                {
                    toolbar.Move(0, 0, width, toolbarLayoutSize);
                    quickAccessBar.Move(0, height - quickAccessBarSize - pianoLayoutSize, width, quickAccessBarSize);
                    mobilePiano.Move(0, height - pianoLayoutSize, width, pianoLayoutSize);
                }

                activeControl.Move(0, toolbarLayoutSize, width, height - toolbarLayoutSize - quickAccessBarSize - pianoLayoutSize);

                // Always update the piano roll since we draw the view range in the sequencer and 
                // it requires valid size to do that.
                if (activeControl != pianoRoll)
                    pianoRoll.Move(0, toolbarLayoutSize, width, height - toolbarLayoutSize - quickAccessBarSize - pianoLayoutSize);
            }

            sequencer.Visible = activeControl == sequencer;
            pianoRoll.Visible = activeControl == pianoRoll;
            projectExplorer.Visible = activeControl == projectExplorer;
        }

        public bool CanAcceptInput
        {
            get
            {
                return transitionTimer == 0.0f && transitionControl == null;
            }
        }

        public override bool CanInteractWithContainer(Container c)
        {
            if (!CanAcceptInput)
            {
                return false;
            }

            // HACK: If you expand the toolbar, you can actually click on the quick access bar.
            if (toolbar.IsExpanded && c != toolbar)
            {
                return false;
            }

            return base.CanInteractWithContainer(c);
        }

        private void RenderTransitionOverlay(Graphics g)
        {
            if (transitionTimer > 0.0f)
            {
                var alpha = (byte)((1.0f - Math.Abs(transitionTimer - 0.5f) * 2) * 255);
                var color = Color.FromArgb(alpha, Theme.DarkGreyColor4);

                g.OverlayCommandList.FillRectangle(activeControl.WindowRectangle, color);
            }
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

        protected override void OnRender(Graphics g)
        {
            RenderTransitionOverlay(g);
            base.OnRender(g);
        }
    }
}
