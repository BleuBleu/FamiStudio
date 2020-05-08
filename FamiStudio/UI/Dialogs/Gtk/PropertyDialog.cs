using Gtk;
using System;
using System.Reflection;
using System.Resources;

namespace FamiStudio
{
    public class PropertyDialog : Window
    {
        public delegate bool ValidateDelegate(PropertyPage props);
        public event ValidateDelegate ValidateProperties;

        private System.Drawing.Point initialLocation;
        private bool leftAlign = false;
        private bool topAlign  = false;

        private PropertyPage propertyPage = new PropertyPage();
        private System.Windows.Forms.DialogResult result = System.Windows.Forms.DialogResult.None;

        public  PropertyPage Properties => propertyPage;

        public PropertyDialog(System.Drawing.Point pt, int width, bool leftAlign = false, bool topAlign = false) : base(WindowType.Toplevel)
        {
            Init();
            WidthRequest = width;
            initialLocation = pt;

            this.leftAlign = leftAlign;
            this.topAlign  = topAlign;

            Move(pt.X, pt.Y);
        }

        public PropertyDialog(int x, int y, int width, int height) : base(WindowType.Toplevel)
        {
            Init();
            WidthRequest = width;
            Move(x, y);
        }

        private void Init()
        {
            var hbox = new HBox(false, 0);

            var suffix = GLTheme.DialogScaling >= 2.0f ? "@2x" : "";
            var buttonYes = new FlatButton(Gdk.Pixbuf.LoadFromResource($"FamiStudio.Resources.Yes{suffix}.png"));
            var buttonNo = new FlatButton(Gdk.Pixbuf.LoadFromResource($"FamiStudio.Resources.No{suffix}.png"));

            buttonYes.Show();
            buttonYes.ButtonPressEvent += ButtonYes_ButtonPressEvent;
            buttonNo.Show();
            buttonNo.ButtonPressEvent += ButtonNo_ButtonPressEvent;

            hbox.PackStart(buttonYes, false, false, 0);
            hbox.PackStart(buttonNo, false, false, 0);
            hbox.Show();

            var align = new Alignment(1.0f, 0.5f, 0.0f, 0.0f);
            align.TopPadding = 5;
            align.Show();
            align.Add(hbox);

            var vbox = new VBox();
            vbox.PackStart(propertyPage, false, false, 0);
            vbox.PackStart(align, false, false, 0);
            vbox.Show();

            Add(vbox);

            propertyPage.PropertyWantsClose += propertyPage_PropertyWantsClose;
            propertyPage.Show();

            BorderWidth = 5;
            Resizable = false;
            Decorated = false;
            KeepAbove = true;
            Modal = true;
            SkipTaskbarHint = true;
        }

        private bool RunValidation()
        {
            if (ValidateProperties == null)
                return true;

#if FAMISTUDIO_MACOS
            MacUtils.RemoveNSWindowAlwaysOnTop(MacUtils.NSWindowFromGdkWindow(GdkWindow.Handle));
#endif
            // Validation might display messages boxes, need to work around z-ordering issues.
            bool valid = ValidateProperties.Invoke(propertyPage);

#if FAMISTUDIO_MACOS
            // This fixes some super weird focus issues.
            if (!valid)
            {
                Hide();
                Show();
            }

            MacUtils.SetNSWindowAlwayOnTop(MacUtils.NSWindowFromGdkWindow(GdkWindow.Handle));
#endif

            return valid;
        }

        private void ButtonNo_ButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            result = System.Windows.Forms.DialogResult.Cancel;
        }

        private void ButtonYes_ButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            if (RunValidation())
                result = System.Windows.Forms.DialogResult.OK;
        }

        private void propertyPage_PropertyWantsClose(int idx)
        {
            if (RunValidation())
                result = System.Windows.Forms.DialogResult.OK;
        }

        protected override bool OnKeyPressEvent(Gdk.EventKey evnt)
        {
            if (evnt.Key == Gdk.Key.Return)
            {
                if (RunValidation())
                    result = System.Windows.Forms.DialogResult.OK;
            }
            else if (evnt.Key == Gdk.Key.Escape)
            {
                result = System.Windows.Forms.DialogResult.Cancel;
            }

            return base.OnKeyPressEvent(evnt);
        }

        public System.Windows.Forms.DialogResult ShowDialog(FamiStudioForm parent = null)
        {
            Show();

#if FAMISTUDIO_LINUX
            if (parent != null)
            {
                // Experiments.
                var display = LinuxUtils.GetWindowDisplay(parent.WindowInfo);
                var parentXId = parent.WindowInfo.Handle;
                var dialogXId = new IntPtr(LinuxUtils.gdk_x11_drawable_get_xid(GdkWindow.Handle));
                LinuxUtils.XReparentWindow(display, dialogXId, parentXId, 10, 10);
            }
#endif

            if (topAlign || leftAlign)
            {
                var pt = initialLocation;
                if (leftAlign) pt.X -= Allocation.Width;
                if (topAlign)  pt.Y -= Allocation.Height;
                Move(pt.X, pt.Y);
            }

#if FAMISTUDIO_MACOS
            MacUtils.SetNSWindowAlwayOnTop(MacUtils.NSWindowFromGdkWindow(GdkWindow.Handle));
#endif

            while (result == System.Windows.Forms.DialogResult.None)
                Application.RunIteration();

            Hide();

#if FAMISTUDIO_MACOS
            MacUtils.RestoreMainNSWindowFocus();
#else
            PlatformUtils.ProcessPendingEvents();
#endif

            return result;
        }
    }
}
