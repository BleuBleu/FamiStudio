using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace FamiStudio
{
    public class Container : Control
    {
        private List<Control> controls = new List<Control>();
        public IReadOnlyCollection<Control> Controls => controls.AsReadOnly();

        public Container()
        {
        }

        public void AddControl(Control ctrl)
        {
            if (!controls.Contains(ctrl))
            {
                controls.Add(ctrl);
                ctrl.SetParentContainer(this);
                ctrl.AddedToContainer();
            }
        }

        public void RemoveControl(Control ctrl)
        {
            if (ctrl != null)
            {
                controls.Remove(ctrl);
            }
        }

        public Control GetControlAt(int winX, int winY, out int ctrlX, out int ctrlY)
        {
            // First look for containers. Last containers are considered to have higher Z-order.
            for (int i = controls.Count - 1; i >= 0; i--)
            {
                if (controls[i] is Container c)
                {
                    if (c.WindowRectangle.Contains(winX, winY))
                    {
                        var ctrl = c.GetControlAt(winX, winY, out ctrlX, out ctrlY);
                        
                        // We are in the container, but not in a specific child.
                        if (ctrl == null)
                        {
                            var winPos = c.WindowPosition;
                            ctrlX = winX - winPos.X;
                            ctrlY = winY - winPos.Y;
                            return c;
                        }
                    }
                }
            }

            // No better container found, look though our controls.
            foreach (var c in controls)
            {
                if (!(c is Container))
                {
                    if (c.WindowRectangle.Contains(winX, winY))
                    {
                        var winPos = c.WindowPosition;
                        ctrlX = winX - winPos.X;
                        ctrlY = winY - winPos.Y;
                        return c;
                    }
                }
            }

            ctrlX = 0;
            ctrlY = 0;
            return null;
        }

        //protected override void OnKeyDown(KeyEventArgs e)
        //{
        //    DialogKeyDown?.Invoke(this, e);

        //    if (focusedControl != null && focusedControl.Visible)
        //    {
        //        focusedControl.KeyDown(e);
        //    }
        //}

        //protected override void OnKeyUp(KeyEventArgs e)
        //{
        //    if (focusedControl != null && focusedControl.Visible)
        //    {
        //        focusedControl.KeyUp(e);
        //    }
        //}

        //protected override void OnChar(CharEventArgs e)
        //{
        //    if (focusedControl != null && focusedControl.Visible)
        //    {
        //        focusedControl.Char(e);
        //    }
        //}

        protected void RenderChildControlsAndContainers(Graphics g)
        {
            // First render our controls, then other containers.
            RenderChildControls(g, false);
            RenderChildControls(g, true);
        }

        protected void RenderChildControls(Graphics g, bool container)
        {
            foreach (var c in controls)
            {
                if ((c is Container) == container && c.Visible)
                {
                    g.Transform.PushTranslation(c.Left, c.Top);
                    c.Render(g);
                    g.Transform.PopTransform();
                }
            }
        }

        protected override void OnRender(Graphics g)
        {
            RenderChildControlsAndContainers(g);
        }

        public override void Render(Graphics g)
        {
            g.PushClipRegion(WindowPosition, Size);
            OnRender(g);
            g.PopClipRegion();
        }
    }
}
