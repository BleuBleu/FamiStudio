using Android.App;
using Java.Security.Cert;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace FamiStudio
{
    public class FamiStudioContainer : Container
    {
        private const bool ShowRenderingTimes = false;

        private List<Dialog> dialogs = new List<Dialog>();

        private Control transitionControl;
        private Control activeControl;
        private Dialog  dialogToHide;
        private bool    dialogTransition;
        private bool    poppingDialog;
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

        public Dialog TopDialog => dialogs.Count > 0 ? dialogs[dialogs.Count - 1] : null;
        public bool IsDialogActive => dialogs.Count > 0;

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

        public void StartTransition(Control ctrl, bool animate = true, bool dialog = false)
        {
            if (activeControl != ctrl || dialog)
            {
                Debug.Assert(transitionTimer == 0.0f && transitionControl == null);

                if (animate)
                {
                    transitionControl = ctrl;
                    transitionTimer   = 1.0f;
                }
                else
                {
                    Debug.Assert(!dialogTransition);
                    activeControl   = ctrl;
                    transitionTimer = 0.0f;
                }

                dialogTransition = dialog;
            }
        }

        public void InitDialog(Dialog dialog)
        {
            AddControl(dialog);
        }

        public void PushDialog(Dialog dialog)
        {
            dialogToHide = dialogs.Count > 0 ? dialogs[dialogs.Count - 1] : null;
            AddControl(dialog);
            dialogs.Add(dialog);
            dialog.Visible = false;
            StartTransition(dialog, true, true);
            poppingDialog = false;
        }

        public void PopDialog(Dialog dialog, int numLevels)
        {
            Debug.Assert(TopDialog == dialog);

            if (numLevels > 1)
            {
                Debug.Assert(dialogs.Count >= numLevels);
                for (var i = 1; i < numLevels; i++)
                {
                    RemoveControl(dialogs[dialogs.Count - 2]);
                    dialogs.RemoveAt(dialogs.Count - 2);
                }

                Debug.Assert(TopDialog == dialog);
            }

            dialogs.RemoveAt(dialogs.Count - 1);

            if (dialogs.Count > 0)
            {
                StartTransition(TopDialog, true, true);
            }
            else
            {
                StartTransition(activeControl, true, true);
            }

            poppingDialog = true;
            dialogToHide = dialog;
        }

        protected override void OnResize(EventArgs e)
        {
            if (transitionControl != null)
            {
                CommitTransitionControl();
                transitionTimer = 0.0f;
            }

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

            var dialogActive = IsDialogActive;

            sequencer.Visible = activeControl == sequencer && !dialogActive;
            pianoRoll.Visible = activeControl == pianoRoll && !dialogActive;
            projectExplorer.Visible = activeControl == projectExplorer && !dialogActive;
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

            // Only top dialog can be interacted with.
            if (IsDialogActive && c != dialogs.Last())
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
                var color = Color.FromArgb(alpha, Theme.DarkGreyColor2);

                if (dialogTransition)
                {
                    g.OverlayCommandList.FillRectangle(WindowRectangle, color);
                }
                else
                {
                    g.OverlayCommandList.FillRectangle(activeControl.WindowRectangle, color);
                }
            }
        }

        private void CommitTransitionControl()
        {
            if (dialogTransition)
            {
                transitionControl.Visible = true;
            }
            else
            {
                activeControl = transitionControl;
            }

            if (dialogToHide != null)
            {
                // Only remove the dialogs when popping. Even inactive dialogs need to
                // remain attached since some of the code can call ParentWindow from them.
                if (poppingDialog)
                {
                    RemoveControl(dialogToHide);
                    poppingDialog = false;
                }
                dialogToHide.Visible = false;
                dialogToHide = null;
            }

            transitionControl = null;
        }

        public override void Tick(float delta)
        {
            var prevTimer = transitionTimer;
            transitionTimer = Math.Max(0.0f, transitionTimer - delta * 6);

            if (prevTimer > 0.5f && transitionTimer <= 0.5f)
            {
                CommitTransitionControl();
                UpdateLayout(true);
            }

            MarkDirty();
        }

        protected override void OnRender(Graphics g)
        {
            RenderTransitionOverlay(g);
            base.OnRender(g);
        }
    }
}
