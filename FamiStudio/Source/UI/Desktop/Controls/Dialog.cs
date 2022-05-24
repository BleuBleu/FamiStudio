using System;
using System.Drawing;
using System.Collections.Generic;
using System.Windows.Forms;

using RenderBitmapAtlas = FamiStudio.GLBitmapAtlas;
using RenderBrush       = FamiStudio.GLBrush;
using RenderGeometry    = FamiStudio.GLGeometry;
using RenderControl     = FamiStudio.GLControl;
using RenderGraphics    = FamiStudio.GLGraphics;
using RenderCommandList = FamiStudio.GLCommandList;

namespace FamiStudio
{
    public class Dialog : RenderControl
    {
        private List<RenderControl> controls = new List<RenderControl>();
        private RenderControl focusedControl;
        private Action<DialogResult> callback;
        
        public RenderControl FocusedControl => focusedControl;
        public IReadOnlyCollection<RenderControl> Controls => controls.AsReadOnly();

        public Dialog()
        {
            // MATTT: Add the form to the constructor eventually.
            FamiStudioForm.Instance.InitDialog(this);
        }

        public void ShowDialogAsync(object parent, Action<DialogResult> cb)
        {
            callback = cb;
            OnShowDialog();
            FamiStudioForm.Instance.PushDialog(this);
        }

        public void Close(DialogResult result)
        {
            FamiStudioForm.Instance.PopDialog(this);
            callback(result);
        }

        protected virtual void OnShowDialog()
        {
        }

        public void AddControl(RenderControl ctrl)
        {
            if (!controls.Contains(ctrl))
            {
                ctrl.SetDpiScales(DpiScaling.MainWindow, DpiScaling.Font);
                ctrl.SetThemeRenderResource(ThemeResources);
                ctrl.RenderInitialized(ParentForm.Graphics);
                ctrl.ParentForm = ParentForm;
                ctrl.ParentDialog = this;
                controls.Add(ctrl);
            }
        }

        public void RemoveControl(RenderControl ctrl)
        {
            controls.Remove(ctrl);
        }

        public override void Tick(float delta)
        {
            foreach (var ctrl in controls)
            {
                if (ctrl.Visible)
                {
                    ctrl.Tick(delta);
                }
            }
        }

        public RenderControl GetControlAtInternal(bool priority, int formX, int formY, out int ctrlX, out int ctrlY)
        {
            ctrlX = 0;
            ctrlY = 0;

            foreach (var ctrl in controls)
            {
                if (ctrl.Visible && ctrl.PriorityInput == priority)
                {
                    var dlgX = ctrl.Left - Left;
                    var dlgY = ctrl.Top - Top;

                    if (ctrl.Rectangle.Contains(formX, formY))
                    {
                        ctrlX = formX - ctrl.Left;
                        ctrlY = formY - ctrl.Top;

                        return ctrl;
                    }
                }
            }

            return null;
        }

        public RenderControl GetControlAt(int formX, int formY, out int ctrlX, out int ctrlY)
        {
            var ctrl = GetControlAtInternal(true, formX, formY, out ctrlX, out ctrlY);
            if (ctrl != null)
                return ctrl;
            return GetControlAtInternal(false, formX, formY, out ctrlX, out ctrlY);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (focusedControl != null && focusedControl.Visible)
            {
                focusedControl.KeyDown(e);
            }
        }

        protected override void OnRender(RenderGraphics g)
        {
            g.BeginDrawDialog();

            // Fill + Border
            var c = g.CreateCommandList(GLGraphicsBase.CommandListUsage.Dialog);
            c.FillAndDrawRectangle(0, 0, width - 1, height - 1, ThemeResources.DarkGreyFillBrush1, ThemeResources.BlackBrush);

            // Render child controls
            foreach (var ctrl in controls)
            {
                if (ctrl.Visible)
                {
                    // MATTT : This is stupid, need DialogLeft too?
                    g.Transform.PushTranslation(ctrl.Left - Left, ctrl.Top - Top);
                    ctrl.Render(g);
                    g.Transform.PopTransform();
                }
            }

            g.EndDrawDialog(Theme.DarkGreyFillColor1, Rectangle);
        }
    }
}
