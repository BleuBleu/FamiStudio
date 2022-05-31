using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace FamiStudio
{
    public class FamiStudioControls
    {
        private int width;
        private int height;
        private Graphics gfx;
        private Control[] controls = new Control[4];
        private List<Dialog> dialogs = new List<Dialog>();
        private ThemeRenderResources res;
        private float dialogDimming = 0.0f;
        private DateTime lastRender = DateTime.Now;
        private bool contextMenuVisible;

        private Toolbar toolbar;
        private Sequencer sequencer;
        private PianoRoll pianoRoll;
        private ProjectExplorer projectExplorer;
        private QuickAccessBar quickAccessBar;
        private MobilePiano mobilePiano;
        private ContextMenu contextMenu;

        public Toolbar ToolBar => toolbar;
        public Sequencer Sequencer => sequencer;
        public PianoRoll PianoRoll => pianoRoll;
        public ProjectExplorer ProjectExplorer => projectExplorer;
        public QuickAccessBar QuickAccessBar => quickAccessBar;
        public MobilePiano MobilePiano => mobilePiano;
        public ContextMenu ContextMenu => contextMenu;
        public Graphics Graphics => gfx;
        public bool IsContextMenuActive => contextMenuVisible;
        public bool IsDialogActive => dialogs.Count > 0;
        public Dialog TopDialog => dialogs.Count > 0 ? dialogs[dialogs.Count - 1] : null;

        public Control[] Controls => controls;

        public FamiStudioControls(FamiStudioWindow parent)
        {
            toolbar = new Toolbar();
            sequencer = new Sequencer();
            pianoRoll = new PianoRoll();
            projectExplorer = new ProjectExplorer();
            quickAccessBar = new QuickAccessBar();
            mobilePiano = new MobilePiano();
            contextMenu = new ContextMenu();

            controls[0] = toolbar;
            controls[1] = sequencer;
            controls[2] = pianoRoll;
            controls[3] = projectExplorer;

            foreach (var ctrl in controls)
                ctrl.ParentWindow = parent;

            contextMenu.ParentWindow = parent;
        }

        public void Resize(int w, int h)
        {
            width  = Math.Max(1, w);
            height = Math.Max(1, h);

            int toolBarHeight        = DpiScaling.ScaleForMainWindow(40);
            int projectExplorerWidth = DpiScaling.ScaleForMainWindow(300);
            int sequencerHeight      = pianoRoll.IsMaximized ? 1 : DpiScaling.ScaleForMainWindow(sequencer.ComputeDesiredSizeY());

            toolbar.Move(0, 0, width, toolBarHeight);
            projectExplorer.Move(width - projectExplorerWidth, toolBarHeight, projectExplorerWidth, height - toolBarHeight);
            sequencer.Move(0, toolBarHeight, width - projectExplorerWidth, sequencerHeight);
            pianoRoll.Move(0, toolBarHeight + sequencerHeight, width - projectExplorerWidth, height - toolBarHeight - sequencerHeight);

            foreach (var dlg in dialogs)
                dlg.CenterToWindow();
        }

        public Control GetControlAtCoord(int formX, int formY, out int ctrlX, out int ctrlY)
        {
            // Don't send any events if the context menu is visible.
            if (contextMenuVisible)
            {
                ctrlX = formX - contextMenu.Left;
                ctrlY = formY - contextMenu.Top;

                if (ctrlX >= 0 &&
                    ctrlY >= 0 &&
                    ctrlX < contextMenu.Width &&
                    ctrlY < contextMenu.Height)
                {
                    return contextMenu;
                }
            }

            // If there is an active dialog, it also eats all of the input.
            if (dialogs.Count > 0)
            {
                return TopDialog.GetControlAt(formX, formY, out ctrlX, out ctrlY);
            }

            // Finally, send to one of the main controls.
            foreach (var ctrl in controls)
            {
                ctrlX = formX - ctrl.Left;
                ctrlY = formY - ctrl.Top;

                if (ctrlX >= 0 &&
                    ctrlY >= 0 &&
                    ctrlX <  ctrl.Width &&
                    ctrlY <  ctrl.Height)
                {
                    return ctrl;
                }
            }

            ctrlX = 0;
            ctrlY = 0;
            return null;
        }

        public void ShowContextMenu(int x, int y, ContextMenuOption[] options)
        {
            contextMenu.Initialize(gfx, options);

            // Keep the menu inside the bounds of the window.
            var alignX = x + contextMenu.Width  > width;
            var alignY = y + contextMenu.Height > height;

            contextMenu.Move(
                alignX ? x - contextMenu.Width  - 1 : x + 1,
                alignY ? y - contextMenu.Height - 1 : y + 1);

            contextMenuVisible = true;
            MarkDirty();
        }

        public void HideContextMenu()
        {
            contextMenuVisible = false;
            MarkDirty();
        }

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
        }

        public void MarkDirty()
        {
            foreach (var ctrl in controls)
                ctrl.MarkDirty();
        }

        public bool AnyControlNeedsRedraw()
        {
            bool anyNeedsRedraw = false;
            foreach (var control in controls)
                anyNeedsRedraw |= control.NeedsRedraw;
            foreach (var dlg in dialogs)
                anyNeedsRedraw |= dlg.NeedsRedraw;
            if (contextMenuVisible)
                anyNeedsRedraw |= contextMenu.NeedsRedraw;
            return anyNeedsRedraw;
        }

        public unsafe bool Redraw()
        {
            var now = DateTime.Now;
            var deltaTime = (float)(now - lastRender).TotalSeconds;
            lastRender = now;

            if (AnyControlNeedsRedraw())
            {
                // HACK : This happens when we have a dialog open, like the NSF dialog.
                if (controls[0].App.Project == null)
                    return true;

                var fullWindowRect = new System.Drawing.Rectangle(0, 0, width, height);

                gfx.BeginDrawFrame();

                foreach (var control in controls)
                {
                    gfx.BeginDrawControl(control.Rectangle, height);
                    control.Render(gfx);
                    control.ClearDirtyFlag();
                }

                if (dialogDimming != 0.0f)
                {
                    gfx.BeginDrawControl(fullWindowRect, height);
                    var c = gfx.CreateCommandList();
                    c.FillRectangle(fullWindowRect, gfx.GetSolidBrush(System.Drawing.Color.Black, 1, Utils.Lerp(0.0f, 0.4f, dialogDimming)));
                    gfx.DrawCommandList(c);
                }

                // MATTT : Dirty flag management for dialogs.
                foreach (var dlg in dialogs)
                {
                    gfx.BeginDrawControl(fullWindowRect, height);
                    gfx.Transform.PushTranslation(dlg.Left, dlg.Top);
                    dlg.Render(gfx);
                    dlg.ClearDirtyFlag();
                    gfx.Transform.PopTransform();
                }

                if (contextMenuVisible)
                {
                    gfx.BeginDrawControl(contextMenu.Rectangle, height);
                    contextMenu.Render(gfx);
                }

                gfx.EndDrawFrame();

                return true;
            }

            return false;
        }

        public void InitDialog(Dialog dialog)
        {
            dialog.SetDpiScales(DpiScaling.Window, DpiScaling.Font);
            dialog.SetThemeRenderResource(res);
            dialog.RenderInitialized(gfx);
            dialog.ParentWindow = contextMenu.ParentWindow; // HACK
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

        public void InitializeGL()
        {
            gfx = new Graphics(DpiScaling.Window, DpiScaling.Font);
            res = new ThemeRenderResources(gfx);

            foreach (var ctrl in controls)
            {
                ctrl.SetDpiScales(DpiScaling.Window, DpiScaling.Font);
                ctrl.SetThemeRenderResource(res);
                ctrl.RenderInitialized(gfx);
            }

            contextMenu.SetDpiScales(DpiScaling.Window, DpiScaling.Font);
            contextMenu.SetThemeRenderResource(res);
            contextMenu.RenderInitialized(gfx);

            //// MATTT : Testing.
            //var dlg = new Dialog();
            //dlg.SetDpiScales(DpiScaling.MainWindow, DpiScaling.Font);
            //dlg.SetThemeRenderResource(res);
            //dlg.RenderInitialized(gfx);
            //dlg.Move(100, 100, 400, 300);
            //dlg.ParentForm = contextMenu.ParentForm; // MATTT
            //dialogs.Push(dlg);

            //dlg.AddLabel(10, 10, 100, 10, "Poop!");
            //dlg.AddButton(120, 10, 32, 32, "");
            //dlg.AddTextBox(10, 50, 100, 16, "Hey");
            //dlg.AddColorPicker(10, 80, 170, 60);
        }
    }
}
