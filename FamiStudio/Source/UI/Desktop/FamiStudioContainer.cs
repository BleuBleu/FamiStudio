using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace FamiStudio
{
    public class FamiStudioContainer : Container
    {
        private List<Dialog> dialogs = new List<Dialog>(); // CTRLTODO : Do we need this?
        private float dialogDimming = 0.0f;
        private bool contextMenuVisible;

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
        public bool IsContextMenuActive => contextMenuVisible;
        public bool IsDialogActive => dialogs.Count > 0;
        public Dialog TopDialog => dialogs.Count > 0 ? dialogs[dialogs.Count - 1] : null;

        public FamiStudioContainer(FamiStudioWindow parent)
        {
            window = parent;
            toolbar = new Toolbar(parent);
            sequencer = new Sequencer(parent);
            pianoRoll = new PianoRoll(parent);
            projectExplorer = new ProjectExplorer(parent);
            quickAccessBar = new QuickAccessBar(parent);
            mobilePiano = new MobilePiano(parent);
            contextMenu = new ContextMenu(parent);
            toast = new Toast(parent);

            AddControl(toolbar);
            AddControl(sequencer);
            AddControl(pianoRoll);
            AddControl(projectExplorer);
        }

        public void Resize(int w, int h)
        {
            width  = Math.Max(1, w);
            height = Math.Max(1, h);

            int toolBarHeight        = DpiScaling.ScaleForWindow(40);
            int projectExplorerWidth = DpiScaling.ScaleForWindow(300);
            int sequencerHeight      = pianoRoll.IsMaximized ? 1 : DpiScaling.ScaleForWindow(sequencer.ComputeDesiredSizeY(out _, out _));

            toolbar.Move(0, 0, width, toolBarHeight);
            projectExplorer.Move(width - projectExplorerWidth, toolBarHeight, projectExplorerWidth, height - toolBarHeight);
            sequencer.Move(0, toolBarHeight, width - projectExplorerWidth, sequencerHeight);
            pianoRoll.Move(0, toolBarHeight + sequencerHeight, width - projectExplorerWidth, height - toolBarHeight - sequencerHeight);

            foreach (var dlg in dialogs)
                dlg.CenterToWindow();

            toast.Reposition();
        }

        // CTRLTODO : Re-test this.
        public override bool CanInteractWithContainer(Container c)
        {
            // HACK : Dont allow any interaction with the main controls if there is no current song
            // since all the code assume this is non-null. This happens when event processing runs
            // during file loading (ex: when calling Thread.Join).
            if (FamiStudio.StaticInstance.SelectedSong == null)
            {
                return false;
            }

            // Only top dialog can be interacted with.
            if (dialogs.Count > 0 && c != dialogs.Last())
            {
                return false;
            }

            // CTRLTODO : What about context menus, etc.
            return true;
        }

        public void ShowContextMenu(int x, int y, ContextMenuOption[] options)
        {
            // CTRLTODO : Bring this back.
            /*
            contextMenu.Initialize(gfx, options);

            // Keep the menu inside the bounds of the window.
            var alignX = x + contextMenu.Width > width;
            var alignY = y + contextMenu.Height > height;

            contextMenu.Move(
                alignX ? x - contextMenu.Width - 1 : x + 1,
                alignY ? y - contextMenu.Height - 1 : y + 1);

            contextMenuVisible = true;
            MarkDirty();
            */
        }

        public void HideContextMenu()
        {
            if (contextMenuVisible)
            {
                contextMenuVisible = false;
                MarkDirty();
            }
        }

        public override void Tick(float delta)
        {
            base.Tick(delta);

            var newDialogDimming = dialogDimming;

            if (dialogs.Count > 0)
            {
                TopDialog.Tick(delta);
                newDialogDimming = Math.Min(1.0f, dialogDimming + delta * 6.0f);
            }
            else
            {
                newDialogDimming = Math.Max(0.0f, dialogDimming - delta * 12.0f);
            }

            if (newDialogDimming != dialogDimming)
            {
                dialogDimming = newDialogDimming;
                MarkDirty();
            }

            toast.Tick(delta);
        }

        protected override void OnRender(Graphics g)
        {
            base.OnRender(g);

            if (dialogDimming != 0.0f)
            {
                var c = g.OverlayCommandList;
                var color = Color.FromArgb(Utils.Lerp(0.0f, 0.4f, dialogDimming), Color.Black);

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
            toast.Initialize(text, longDuration, click);
        }
    }
}
