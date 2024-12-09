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

        private Control activeControl;
        private List<ControlTransition> transitions = new List<ControlTransition>();

        private Rectangle transitionOverlayRect;
        private Color     transitionOverlayColor;

        private Control transitionShadowControl;
        private float   transitionShadowIntensity;

        private Toolbar         toolbar;
        private Sequencer       sequencer;
        private PianoRoll       pianoRoll;
        private ProjectExplorer projectExplorer;
        private QuickAccessBar  quickAccessBar;
        private MobilePiano     mobilePiano;
        private Toast           toast;

        public Toolbar         ToolBar         => toolbar;
        public Sequencer       Sequencer       => sequencer;
        public PianoRoll       PianoRoll       => pianoRoll;
        public ProjectExplorer ProjectExplorer => projectExplorer;
        public QuickAccessBar  QuickAccessBar  => quickAccessBar;
        public MobilePiano     MobilePiano     => mobilePiano;
        public Control         ActiveControl   => activeControl;

        public Dialog TopDialog => dialogs.Count > 0 ? dialogs[dialogs.Count - 1] : null;
        public bool IsDialogActive => dialogs.Count > 0;
        public bool AnyFullScreenDialogVisible => controls.Find((c) => (c is Dialog d) && d.Fullscreen && d.Visible) != null;

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
            toast = new Toast();
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

        public void SwitchToControl(Control ctrl, bool animate = true)
        {
            Debug.Assert(ctrl == pianoRoll || ctrl == projectExplorer || ctrl == sequencer);
            
            if (activeControl != ctrl)
            {
                FlushPendingTransitions(true);

                var trans = new FullscreenOverlayTransition(this, ctrl);

                if (animate)
                {
                    transitions.Add(trans);
                }
                else
                {
                    trans.Start();
                    trans.Tick(1000.0f);
                }
            }
        }

        public void InitDialog(Dialog dialog)
        {
            AddControl(dialog);
        }

        public void PushDialog(Dialog dialog)
        {
            if (dialog.Fullscreen)
            {
                FlushPendingTransitions(true);
            }

            AddControl(dialog);
            dialogs.Add(dialog);

            if (dialog.Fullscreen)
            {
                transitions.Add(new FullscreenOverlayTransition(this, dialog, false));
            }
            else
            {
                transitions.Add(new DialogShadowTransition(this, dialog, false));
            }
        }

        public void PopDialog(Dialog dialog, int numLevels)
        {
            if (dialog.Fullscreen)
            {
                FlushPendingTransitions(true);
            }

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

            if (dialog.Fullscreen)
            {
                transitions.Add(new FullscreenOverlayTransition(this, dialog, true));
            }
            else
            {
                transitions.Add(new DialogShadowTransition(this, dialog, true));
            }
        }

        private void FlushPendingTransitions(bool overlay)
        {
            for (var i = 0; i < transitions.Count; i++)
            {
                var trans = transitions[i];

                if (trans.IsOverlay == overlay)
                {
                    if (!trans.Started)
                    {
                        trans.Start();
                    }

                    trans.Tick(1000.0f);
                    transitions.RemoveAt(i);
                }
            }
        }

        protected override void OnResize(EventArgs e)
        {
            FlushPendingTransitions(true);
            FlushPendingTransitions(false);
            UpdateLayout(false);

            // Resize any other dialogs, etc.
            foreach (var ctrl in controls)
            {
                if (ctrl is Dialog d)
                {
                    if (ctrl is ContextMenuDialog)
                    {
                        d.Close(DialogResult.Cancel);
                    }
                    else
                    {
                        d.OnWindowResize(e);
                    }
                }
            }

            toast.Dismiss();
            FlushPendingTransitions(false);
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

            var anyFullscreenDialogVisible = AnyFullScreenDialogVisible;

            quickAccessBar.Visible = !anyFullscreenDialogVisible;
            toolbar.Visible = !anyFullscreenDialogVisible;
            sequencer.Visible = activeControl == sequencer && !anyFullscreenDialogVisible;
            pianoRoll.Visible = activeControl == pianoRoll && !anyFullscreenDialogVisible;
            projectExplorer.Visible = activeControl == projectExplorer && !anyFullscreenDialogVisible;
        }

        public void SetActiveControl(Control ctrl)
        {
            activeControl = ctrl;
            UpdateLayout();
        }

        public bool CanAcceptInput
        {
            get
            {
                return transitions.Count == 0;
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
            if (toolbar.Visible && toolbar.IsExpanded && c != toolbar)
            {
                return false;
            }

            return base.CanInteractWithContainer(c);
        }

        public void SetTransitionOverlay(Rectangle rect, Color color)
        {
            transitionOverlayRect  = rect;
            transitionOverlayColor = color;
        }

        public void SetTransitionShadowControl(Control ctrl, float intensity)
        {
            transitionShadowControl = ctrl;
            transitionShadowIntensity = intensity;
        }

        private void RenderTransitionOverlay(Graphics g)
        {
            if (transitionOverlayColor.A != 0)
            {
                g.OverlayCommandList.FillRectangle(transitionOverlayRect, transitionOverlayColor);
            }
        }

        private void RenderTransitionShadowRect(Graphics g)
        {
            if (transitionShadowIntensity != 0.0f)
            {
                var screenRect = ClientRectangle;
                var shadowRect = transitionShadowControl.TopContainerRectangle;
                var shadowColor = Color.FromArgb((int)Utils.Clamp(transitionShadowIntensity * 0.6f * 255.0f, 0, 255), Color.Black);

                var o = g.OverlayCommandList;
                o.FillRectangle(screenRect.Left, screenRect.Top, shadowRect.Left, screenRect.Bottom, shadowColor);
                o.FillRectangle(shadowRect.Left, screenRect.Top, shadowRect.Right, shadowRect.Top, shadowColor);
                o.FillRectangle(shadowRect.Left, shadowRect.Bottom, shadowRect.Right, screenRect.Bottom, shadowColor);
                o.FillRectangle(shadowRect.Right, screenRect.Top, screenRect.Right, screenRect.Bottom, shadowColor);
            }
        }

        public void ShowContextMenuAsync(ContextMenuOption[] options)
        {
            if (options == null || options.Length == 0)
                return;

            var dlg = new ContextMenuDialog(window, options);
            dlg.ShowDialogAsync();

            toast.Dismiss(); // Toast draws on top, remove.

            Platform.VibrateClick();
        }

        public void HideContextMenu()
        {
            Debug.Assert(TopDialogIsContextMenu());
            TopDialog.Close(DialogResult.OK);
            MarkDirty();
        }

        public void ConditionalHideContextMenu(Control ctrl)
        {
            if (TopDialogIsContextMenu())
            {
                HideContextMenu();
            }
        }

        private bool TopDialogIsContextMenu()
        {
            var dlg = TopDialog;
            if (dlg != null)
            {
                var scroll = dlg.FindControlOfType<TouchScrollContainer>();
                return scroll != null && scroll.FindControlOfType<ContextMenu>() != null;
            }
            return false;
        }

        public void ShowToast(string text, bool longDuration = false, Action click = null)
        {
            AddControl(toast);
            toast.Initialize(text, longDuration, click);
        }

        private void TickTransitions(float delta)
        {
            if (transitions.Count > 0)
            {
                MarkDirty();
            }

            var numOverlayTransistions = 0;
            var numShadowTransitions = 0;

            for (var i = 0; i < transitions.Count;)
            {
                var trans = transitions[i];

                if (trans.IsOverlay && numOverlayTransistions > 0)
                {
                    Debug.Assert(false); 
                }

                if (!trans.IsOverlay && numShadowTransitions > 0)
                {
                    i++;
                    continue;
                }

                if (!trans.Started)
                {
                    trans.Start();
                }

                var done = trans.Tick(delta);

                if (done)
                {
                    transitions.RemoveAt(i);
                }
                else
                {
                    if (trans.IsOverlay)
                    {
                        numOverlayTransistions++;
                    }
                    else
                    {
                        numShadowTransitions++;
                    }

                    i++;
                }
            }
        }

        private void TickToast(float delta)
        {
            if (!toast.Visible)
                RemoveControl(toast);
        }

        public override void Tick(float delta)
        {
            TickTransitions(delta);
            TickToast(delta);
        }

        protected override void OnRender(Graphics g)
        {
            base.OnRender(g);
            RenderTransitionOverlay(g);
            RenderTransitionShadowRect(g);
        }

        public override void Render(Graphics g)
        {
            g.Transform.PushTranslation(left, top);
            base.Render(g);
            g.Transform.PopTransform();
        }
    }

    abstract class ControlTransition
    {
        protected bool started = false;
        protected FamiStudioContainer container;

        protected ControlTransition(FamiStudioContainer cont)
        {
            container = cont;
        }

        public bool Started => started;
        public virtual bool IsOverlay => true;
        public virtual void Start() { started = true; }
        public virtual bool Tick(float delta) { Debug.Assert(started); return true; }
    }

    class FullscreenOverlayTransition : ControlTransition
    {
        private Control target;
        private float timer = -1.0f;
        private bool dialog;
        private bool popping;

        public FullscreenOverlayTransition(FamiStudioContainer cont, Control control) : base(cont)
        {
            target = control;
        }

        public FullscreenOverlayTransition(FamiStudioContainer cont, Dialog dlg, bool pop) : base(cont)
        {
            target = dlg;
            dialog = true;
            popping = pop;
            target.Visible = popping;
        }

        public override void Start()
        {
            base.Start();
            timer = 1.0f;
        }

        public override bool Tick(float delta)
        {
            base.Tick(delta);
            var prevTimer = timer;
            timer = Math.Max(0.0f, timer - delta * 6);
            
            if (prevTimer > 0.5f && timer <= 0.5f)
            {
                if (dialog)
                {
                    var dlg = target as Dialog;

                    if (popping)
                    {
                        dlg.Visible = false;
                        container.RemoveControl(dlg);
                    }
                    else
                    {
                        // TODO : Hide the other visible dialogs underneath too.
                        dlg.Visible = true;
                    }
                }
                else
                {
                    container.SetActiveControl(target);
                }

                container.UpdateLayout();
            }

            var alpha = (byte)((1.0f - Math.Abs(timer - 0.5f) * 2) * 255);
            var color = Color.FromArgb(alpha, Theme.DarkGreyColor2);

            if (dialog)
            {
                container.SetTransitionOverlay(container.ClientRectangle, color);
            }
            else
            {
                container.SetTransitionOverlay(container.ActiveControl.TopContainerRectangle, color);
            }

            return timer <= 0.0f;
        }
    }

    class DialogShadowTransition : ControlTransition
    {
        private Dialog dialog;
        private bool popping;
        private bool contextMenu;
        private float timer = -1.0f;

        public override bool IsOverlay => false;

        public DialogShadowTransition(FamiStudioContainer cont, Dialog dlg, bool pop) : base(cont)
        {
            dialog = dlg;
            popping = pop;
            contextMenu = dlg is ContextMenuDialog;
        }

        public override void Start()
        {
            base.Start();
            timer = 1.0f;
            dialog.Visible = true;
        }

        public override bool Tick(float delta)
        {
            base.Tick(delta);
            Debug.Assert(timer >= 0.0f);

            timer = Math.Max(0, timer - delta * 6);

            if (contextMenu)
            {
                dialog.Move((container.Width - dialog.Width) / 2, container.Height - (int)(dialog.Height * (popping ? timer : 1.0f - timer)));
            }

            if (timer == 0.0f && popping)
            {
                container.RemoveControl(dialog);
                dialog.Visible = false;
                dialog = null;
            }

            container.SetTransitionShadowControl(dialog, popping ? timer : 1.0f - timer);

            return timer <= 0.0f;
        }
    }
}
