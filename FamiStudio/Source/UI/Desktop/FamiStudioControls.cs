using System;
using System.Diagnostics;
using OpenTK.Graphics.OpenGL;

namespace FamiStudio
{
    public class FamiStudioControls
    {
        private int width;
        private int height;
        private GLGraphics gfx;
        private GLControl[] controls = new GLControl[4];
        private ThemeRenderResources res;

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
        public bool IsContextMenuActive => contextMenuVisible;

        DateTime lastRender = DateTime.Now;
        bool  contextMenuVisible;

        public GLControl[] Controls => controls;

        public FamiStudioControls(FamiStudioForm parent)
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
                ctrl.ParentForm = parent;

            contextMenu.ParentForm = parent;
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
        }

        public GLControl GetControlAtCoord(int formX, int formY, out int ctrlX, out int ctrlY)
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
                alignX ? x - contextMenu.Width  : x,
                alignY ? y - contextMenu.Height : y);

            contextMenuVisible = true;
            MarkDirty();
        }

        public void HideContextMenu()
        {
            contextMenuVisible = false;
            MarkDirty();
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

                gfx.BeginDrawFrame();

                foreach (var control in controls)
                {
                    gfx.BeginDrawControl(control.Rectangle, height);
                    control.Render(gfx);
                    control.ClearDirtyFlag();
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

            contextMenu.SetDpiScales(DpiScaling.MainWindow, DpiScaling.Font);
            contextMenu.SetThemeRenderResource(res);
            contextMenu.RenderInitialized(gfx);
        }
    }
}
