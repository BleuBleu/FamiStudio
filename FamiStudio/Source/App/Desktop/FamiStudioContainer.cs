using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace FamiStudio
{
    public class FamiStudioContainer : Container
    {
        private List<Dialog> dialogs = new List<Dialog>();
        private float dialogDimming = 0.0f;

        private Toolbar toolbar;
        private Sequencer sequencer;
        private PianoRoll pianoRoll;
        private ProjectExplorer projectExplorer;
        private QuickAccessBar quickAccessBar;
        private MobilePiano mobilePiano;
        private ContextMenu contextMenu;
        private Toast toast;

        public Toolbar ToolBar => toolbar;
        public Sequencer Sequencer => sequencer;
        public PianoRoll PianoRoll => pianoRoll;
        public ProjectExplorer ProjectExplorer => projectExplorer;
        public QuickAccessBar QuickAccessBar => quickAccessBar;
        public MobilePiano MobilePiano => mobilePiano;
        public ContextMenu ContextMenu => contextMenu;
        public Toast Toast => toast;
        public bool IsContextMenuActive => contextMenu.Visible;
        public bool IsDialogActive => dialogs.Count > 0;
        public Dialog TopDialog => dialogs.Count > 0 ? dialogs[dialogs.Count - 1] : null;

        public FamiStudioContainer(FamiStudioWindow parent)
        {
            window = parent;
            toolbar = new Toolbar();
            sequencer = new Sequencer();
            pianoRoll = new PianoRoll();
            projectExplorer = new ProjectExplorer();
            quickAccessBar = new QuickAccessBar();
            mobilePiano = new MobilePiano();
            contextMenu = new ContextMenu();
            toast = new Toast();

            AddControl(toolbar);
            AddControl(sequencer);
            AddControl(pianoRoll);
            AddControl(projectExplorer);

            SetTickEnabled(true);
        }

        protected override void OnResize(EventArgs e)
        {
            int toolBarHeight = DpiScaling.ScaleForWindow(40);
            int projectExplorerWidth = DpiScaling.ScaleForWindow(300);
            int sequencerHeight = pianoRoll.IsMaximized ? 0 : DpiScaling.ScaleForWindow(sequencer.ComputeDesiredSizeY(out _, out _));

            toolbar.Move(0, 0, width, toolBarHeight);
            projectExplorer.Move(width - projectExplorerWidth, toolBarHeight, projectExplorerWidth, height - toolBarHeight);
            sequencer.Move(0, toolBarHeight, width - projectExplorerWidth, sequencerHeight);
            pianoRoll.Move(0, toolBarHeight + sequencerHeight, width - projectExplorerWidth, height - toolBarHeight - sequencerHeight);

            foreach (var dlg in dialogs)
                dlg.CenterToWindow();

            // MATTT : Close ctx menu on desktop too.

            toast.Reposition();
        }

        public override bool CanInteractWithContainer(Container c)
        {
            // HACK : Dont allow any interaction with the main controls if there is no current song
            // since all the code assume this is non-null. This happens when event processing runs
            // during file loading (ex: when calling Thread.Join).
            if (FamiStudio.StaticInstance.SelectedSong == null)
            {
                return false;
            }

            // Context menu is always on top of everything.
            if (c == contextMenu)
            {
                return true;
            }

            // Only top dialog can be interacted with.
            if (IsDialogActive && c != dialogs.Last())
            {
                return false;
            }

            return true;
        }

        public List<Control> GetControlsForKeyboard(out bool isMainFamistudioControls)
        {
            var keyControls = new List<Control>();
            
            isMainFamistudioControls = false;

            if (FamiStudio.StaticInstance.SelectedSong == null)
            {
                return keyControls;
            }

            if (IsDialogActive)
            {
                keyControls.Add(TopDialog);
            }
            else if (IsContextMenuActive)
            {
                keyControls.Add(contextMenu);
            }
            else
            {
                foreach (var ctrl in controls)
                {
                    if (ctrl.Visible)
                    {
                        keyControls.Add(ctrl);
                    }
                }

                isMainFamistudioControls = true;
            }

            return keyControls;
        }

        public void ShowContextMenu(int x, int y, ContextMenuOption[] options)
        {
            AddControl(contextMenu);

            contextMenu.Visible = true;
            contextMenu.Initialize(options);

            // Keep the menu inside the bounds of the window.
            var alignX = x + contextMenu.Width > width;
            var alignY = y + contextMenu.Height > height;

            contextMenu.Move(
                alignX ? x - contextMenu.Width - 1 : x + 1,
                alignY ? y - contextMenu.Height - 1 : y + 1);

            MarkDirty();
        }

        public void HideContextMenu()
        {
            if (contextMenu.Visible)
            { 
                contextMenu.Visible = false;
                RemoveControl(contextMenu);
                MarkDirty();
            }
        }

        public void ConditionalHideContextMenu(Control ctrl)
        {
            if (ctrl != contextMenu)
            {
                HideContextMenu();
            }
        }

        public override void Tick(float delta)
        {
            var newDialogDimming = dialogDimming;

            if (IsDialogActive)
                newDialogDimming = Math.Min(1.0f, dialogDimming + delta * 6.0f);
            else
                newDialogDimming = Math.Max(0.0f, dialogDimming - delta * 12.0f);

            SetAndMarkDirty(ref dialogDimming, newDialogDimming);

            if (!toast.Visible)
                RemoveControl(toast);
        }

        protected override void OnRender(Graphics g)
        {
            if (dialogDimming != 0.0f)
            {
                var c = g.OverlayCommandList;
                var color = Color.FromArgb((int)(dialogDimming * 100), Color.Black);

                if (dialogs.Count != 0)
                {
                    // The overlay command list has no depth depth. Draw negative of the rect.
                    var rc = dialogs.Last().WindowRectangle;
                    c.FillRectangle(0, 0, width, rc.Top, color);
                    c.FillRectangle(0, rc.Top, rc.Left, rc.Bottom, color);
                    c.FillRectangle(rc.Right, rc.Top, width, rc.Bottom, color);
                    c.FillRectangle(0, rc.Bottom, width, height, color);
                }
                else
                {
                    c.FillRectangle(0, 0, width, height, color);
                }
            }

            base.OnRender(g);
        }

        public void InitDialog(Dialog dialog)
        {
            AddControl(dialog);
        }

        public void PushDialog(Dialog dialog)
        {
            AddControl(dialog);
            dialogs.Add(dialog);
        }

        public void PopDialog(Dialog dialog)
        {
            Debug.Assert(TopDialog == dialog);
            dialogs.RemoveAt(dialogs.Count - 1);
            RemoveControl(dialog);
        }

        public void ShowToast(string text, bool longDuration = false, Action click = null)
        {
            AddControl(toast);
            toast.Initialize(text, longDuration, click);
        }

        public void UpdateLayout()
        {
            // Only for mobile.
        }
    }
}
