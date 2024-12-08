using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace FamiStudio
{
    public class Container : Control
    {
        // These are only used by scroll container.
        protected int containerScrollX;
        protected int containerScrollY;
        protected List<Control> controls = new List<Control>();
        protected bool clipRegion = true;
        protected bool clipRegionToParent = true;
        protected int numControlsTickEnabled;

        public int ScrollX { get => containerScrollX; set { if (SetAndMarkDirty(ref containerScrollX, value)) Scrolled?.Invoke(this); } }
        public int ScrollY { get => containerScrollY; set { if (SetAndMarkDirty(ref containerScrollY, value)) Scrolled?.Invoke(this); } }
        public IReadOnlyCollection<Control> Controls => controls.AsReadOnly();

        public delegate void RenderingDelegate(Graphics g);
        public event RenderingDelegate Rendering;
        public delegate void ScrolledDelegate(Container sender);
        public event ScrolledDelegate Scrolled;

        public Container()
        {
            canFocus = false;
        }

        public virtual void OnContainerMouseWheelNotify(Control control, PointerEventArgs e)
        {
        }

        public virtual void OnContainerPointerMoveNotify(Control control, PointerEventArgs e)
        {
        }

        public virtual void OnContainerPointerDownNotify(Control control, PointerEventArgs e)
        {
        }

        public virtual void OnContainerPointerUpNotify(Control control, PointerEventArgs e)
        {
        }

        public virtual void OnContainerTouchClickNotify(Control control, PointerEventArgs e)
        {
        }

        public virtual void OnContainerTouchFlingNotify(Control control, PointerEventArgs e)
        {
        }

        public void SetupClipRegion(bool clip, bool clipParents = true)
        {
            clipRegion = clip;
            clipRegionToParent = clipParents;
        }

        public void IncrementControlTickEnabled(int delta)
        {
            numControlsTickEnabled += delta;
            Debug.Assert(numControlsTickEnabled >= 0);
            if (container != null)
                container.IncrementControlTickEnabled(delta);
        }

        public void AddControl(Control ctrl)
        {
            if (!controls.Contains(ctrl))
            {
                controls.Add(ctrl);
                ctrl.SetParentContainer(this);
                ctrl.SendAddedToContainer();

                if (ctrl.TickEnabled)
                    IncrementControlTickEnabled(1);
                if (ctrl is Container cont)
                    IncrementControlTickEnabled(cont.numControlsTickEnabled);

                MarkDirty();
            }
        }

        public void RemoveControl(Control ctrl)
        {
            if (ctrl != null && controls.Contains(ctrl))
            {
                if (ctrl.TickEnabled)
                    IncrementControlTickEnabled(-1);
                if (ctrl is Container cont)
                    IncrementControlTickEnabled(-cont.numControlsTickEnabled);

                ctrl.SetParentContainer(null);
                controls.Remove(ctrl);

                MarkDirty();
            }
        }

        public void RemoveAllControls()
        {
            for (int i = controls.Count - 1; i >= 0; i--) 
            {
                controls[i].SetParentContainer(null);
                controls.RemoveAt(i);
            }

            MarkDirty();
        }

        public T FindControlOfType<T>() where T : Control
        {
            for (int i = 0; i < controls.Count; i++)
            {
                if (controls[i].GetType() == typeof(T))
                    return controls[i] as T;
            }
            return null;
        }

        public T FindControlOfTypeAt<T>(int winX, int winY) where T : Control
        {
            for (int i = 0; i < controls.Count; i++)
            {
                if (controls[i].GetType() == typeof(T) && controls[i].HitTest(winX, winY))
                    return controls[i] as T;
            }
            return null;
        }

        public T FindLastControlOfType<T>() where T : Control
        {
            for (int i = controls.Count - 1; i >= 0; i--)
            {
                if (controls[i].GetType() == typeof(T))
                    return controls[i] as T;
            }
            return null;
        }

        public Control FindControlByUserData(object o)
        {
            foreach (var c in controls)
            {
                if (c.UserData == o)
                    return c;
            }
            return null;
        }

        public Control FindControlByUserData(IComparable o) // Mostly to ensure strings are compared by value.
        {
            foreach (var c in controls)
            {
                if (c.UserData != null && ((IComparable)c.UserData).CompareTo(o) == 0)
                    return c;
            }
            return null;
        }

        public Rectangle GetControlsRect()
        {
            var rc = new Rectangle();
            foreach (var c in controls)
                rc = Rectangle.Union(rc, c.ClientRectangle.Offsetted(c.Left, c.Top));
            return rc;
        }

        public virtual bool CanInteractWithContainer(Container c)
        {
            return true;
        }

        public Control GetControl(int index)
        {
            return controls[index]; 
        }

        public virtual Control GetControlAt(int winX, int winY, out int ctrlX, out int ctrlY)
        {
            // First look for containers. Last containers are considered to have higher Z-order.
            for (int i = controls.Count - 1; i >= 0; i--)
            {
                if (controls[i] is Container c && CanInteractWithContainer(c))
                {
                    if (c.Visible && c.HitTest(winX, winY))
                    {
                        var ctrl = c.GetControlAt(winX, winY, out ctrlX, out ctrlY);
                        
                        if (ctrl != null)
                        {
                            return ctrl;
                        }
                        else // We are in the container, but not in a specific child.
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
            for (int i = controls.Count - 1; i >= 0; i--)
            {
                if (!(controls[i] is Container))
                {
                    var c = controls[i];
                    if (c.Visible && c.HitTest(winX, winY))
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

        public override Point ControlToWindow(Point p)
        {
            if (container == null)
            {
                return p;
            }
            else
            {
                return base.ControlToWindow(p);
            }
        }

        public override Point WindowToControl(Point p)
        {
            p.X -= left;
            p.Y -= top;

            if (container == null)
            {
                return p;
            }
            else
            {
                return base.WindowToControl(p);
            }
        }

        public virtual void GatherChildrenToTick(float delta, ref List<Control> controlsToTick)
        {
            if (numControlsTickEnabled > 0)
            {
                foreach (var ctrl in controls)
                {
                    if (ctrl.Visible)
                    {
                        if (ctrl.TickEnabled)
                            controlsToTick.Add(ctrl);
                        if (ctrl is Container cont)
                            cont.GatherChildrenToTick(delta, ref controlsToTick);
                    }
                }
            }
        }

        public void TickWithChildren(float delta)
        {
            // Build a temporary list and tick them afterwards. This allow
            // adding/removing controls during tick, otherwise the iterator
            // will give us a "collection was modified" error.
            var controlsToTick = new List<Control>();

            if (TickEnabled)
            {
                controlsToTick.Add(this);
            }

            GatherChildrenToTick(delta, ref controlsToTick);

            foreach (var ctrl in controlsToTick)
            {
                //Debug.WriteLine($"Ticking {ctrl}");
                ctrl.Tick(delta);
            }
        }

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
                    var tx = c.Left - containerScrollX;
                    var ty = c.Top  - containerScrollY;

                    // Simple culling for containers, mostly just for the project explorer.
                    if (container)
                    {
                        var containerRect = c.ClientRectangle.Offsetted(tx, ty);
                        if (!containerRect.Intersects(ClientRectangle))
                            continue;
                    }

                    g.Transform.PushTranslation(tx, ty);
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
            if (clipRegion) g.PushClipRegion(0, 0, width, height, clipRegionToParent);
            OnRender(g);
            Rendering?.Invoke(g);
            if (clipRegion) g.PopClipRegion();
        }
    }
}
