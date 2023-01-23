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
            toolbar = new Toolbar(parent);
            sequencer = new Sequencer(parent);
            pianoRoll = new PianoRoll(parent);
            projectExplorer = new ProjectExplorer(parent);
            quickAccessBar = new QuickAccessBar(parent);
            mobilePiano = new MobilePiano(parent);
            contextMenu = new ContextMenu(parent);
            toast = new Toast(parent);
            window = parent;

            AddControl(toolbar);
            AddControl(sequencer);
            AddControl(pianoRoll);
            AddControl(projectExplorer);
        }

        /*
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
        */

        // CTRLTODO : See where we can fit this? Override the functions from Container to get control?
        public bool CanInteractWithControls()
        {
            // HACK : Dont allow any interaction with the main controls if there is no current song
            // since all the code assume this is non-null. This happens when event processing runs
            // during file loading (ex: when calling Thread.Join).
            return true; // controls[0].App.SelectedSong != null;
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

        // CTRLTODO : Tick.
        /*
        public void Tick(float delta)
        {
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
        */

        public bool AnyControlNeedsRedraw()
        {
            return true;

            // CTRLTODO : Bring back?
            /*
            bool anyNeedsRedraw = false;
            foreach (var control in controls)
                anyNeedsRedraw |= control.NeedsRedraw;
            foreach (var dlg in dialogs)
                anyNeedsRedraw |= dlg.NeedsRedraw;
            if (contextMenuVisible)
                anyNeedsRedraw |= contextMenu.NeedsRedraw;
            if (toast.IsVisible)
                anyNeedsRedraw |= toast.NeedsRedraw;
            return anyNeedsRedraw;
            */
        }

        // CTRLTODO
        /*
        public unsafe bool Redraw()
        {
            if (AnyControlNeedsRedraw())
            {
                Debug.Assert(controls[0].App.Project != null);

                var fullWindowRect = new Rectangle(0, 0, width, height);

                gfx.BeginDrawFrame();

                foreach (var control in controls)
                {
                    gfx.BeginDrawControl(control.WindowRectangle, height);
                    control.Render(gfx);
                    control.ClearDirtyFlag();
                }

                if (dialogDimming != 0.0f)
                {
                    gfx.BeginDrawControl(fullWindowRect, height);
                    var c = gfx.CreateCommandList();
                    c.FillRectangle(fullWindowRect, Color.FromArgb(Utils.Lerp(0.0f, 0.4f, dialogDimming), Color.Black));
                    gfx.DrawCommandList(c);
                }

                foreach (var dlg in dialogs)
                {
                    gfx.BeginDrawControl(fullWindowRect, height);
                    gfx.Transform.PushTranslation(dlg.WindowLeft, dlg.WindowTop);
                    dlg.Render(gfx);
                    dlg.ClearDirtyFlag();
                    gfx.Transform.PopTransform();
                }

                if (contextMenuVisible)
                {
                    gfx.BeginDrawControl(contextMenu.WindowRectangle, height);
                    contextMenu.Render(gfx);
                }

                if (toast.IsVisible)
                {
                    gfx.BeginDrawControl(toast.WindowRectangle, height);
                    toast.Render(gfx);
                }

                gfx.EndDrawFrame();

                return true;
            }

            return false;
        }
        */

        public void InitDialog(Dialog dialog)
        {
            // CTRLTODO : Dialogs.
            /*
            dialog.SetDpiScales(DpiScaling.Window, DpiScaling.Font);
            dialog.SetFontRenderResource(fonts);
            dialog.RenderInitialized(gfx);
            */
        }

        public void PushDialog(Dialog dialog)
        {
            dialogs.Add(dialog);
        }

        public void PopDialog(Dialog dialog)
        {
            Debug.Assert(TopDialog == dialog);
            dialogs.RemoveAt(dialogs.Count - 1);
        }

        public void ShowToast(string text, bool longDuration = false, Action click = null)
        {
            toast.Initialize(text, longDuration, click);
        }

        public void InitializeGL()
        {
            // CTRLTODO : This will be moved to Window.
            /*
            gfx = new Graphics();
            fonts = new Fonts(gfx);

            foreach (var ctrl in controls)
            {
                ctrl.SetDpiScales(DpiScaling.Window, DpiScaling.Font);
                ctrl.SetFontRenderResource(fonts);
                ctrl.RenderInitialized(gfx);
            }

            contextMenu.SetDpiScales(DpiScaling.Window, DpiScaling.Font);
            contextMenu.SetFontRenderResource(fonts);
            contextMenu.RenderInitialized(gfx);

            toast.SetDpiScales(DpiScaling.Window, DpiScaling.Font);
            toast.SetFontRenderResource(fonts);
            toast.RenderInitialized(gfx);
            */
        }

        public void ShutdownGL()
        {
            // CTRLTODO : Moved to Window?
            /*
            foreach (var ctrl in controls)
            {
                ctrl.RenderTerminated();
                ctrl.SetFontRenderResource(null);
            }

            fonts.Dispose();
            */
        }
    }
}
