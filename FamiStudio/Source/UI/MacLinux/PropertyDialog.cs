using Gtk;
using System;
using System.Diagnostics;
using System.Drawing;
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
        private FlatButton buttonYes;
        private FlatButton buttonNo;

        private PropertyPage propertyPage = new PropertyPage();
        private System.Windows.Forms.DialogResult result = System.Windows.Forms.DialogResult.None;

        public  PropertyPage Properties => propertyPage;

        public PropertyDialog(int width, bool canAccept = true) : base(WindowType.Toplevel)
        {
            Init();
            WidthRequest = width;
            
            if (!canAccept)
                buttonYes.Hide();

            TransientFor = FamiStudioForm.Instance;
            SetPosition(WindowPosition.CenterOnParent);
        }

        public PropertyDialog(System.Drawing.Point pt, int width, bool leftAlign = false, bool topAlign = false) : base(WindowType.Toplevel)
        {
            Init();
            WidthRequest = width;
            initialLocation = pt;

            this.leftAlign = leftAlign;
            this.topAlign  = topAlign;

            Move(pt.X, pt.Y);
        }

        private void Init()
        {
            var hbox = new HBox(false, 0);

            var suffix = GLTheme.DialogScaling >= 2.0f ? "@2x" : "";

            buttonYes = new FlatButton(Gdk.Pixbuf.LoadFromResource($"FamiStudio.Resources.Yes{suffix}.png"));
            buttonNo  = new FlatButton(Gdk.Pixbuf.LoadFromResource($"FamiStudio.Resources.No{suffix}.png"));

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
            Modal = true;
            SkipTaskbarHint = true;
            TransientFor = FamiStudioForm.Instance;
        }

        private bool RunValidation()
        {
            if (ValidateProperties == null)
                return true;

            // Validation might display messages boxes, need to work around z-ordering issues.
            bool valid = ValidateProperties.Invoke(propertyPage);

            return valid;
        }

        private void ButtonNo_ButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            result = System.Windows.Forms.DialogResult.Cancel;
        }

        private void ButtonYes_ButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            propertyPage.NotifyClosing();
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
                propertyPage.NotifyClosing();
                if (RunValidation())
                    result = System.Windows.Forms.DialogResult.OK;
            }
            else if (evnt.Key == Gdk.Key.Escape)
            {
                result = System.Windows.Forms.DialogResult.Cancel;
            }

            return base.OnKeyPressEvent(evnt);
        }

        public System.Windows.Forms.DialogResult ShowDialog(FamiStudioForm parent)
        {
            Show();

            if (topAlign || leftAlign)
            {
                Debug.Assert(WindowPosition == WindowPosition.None);

                var pt = initialLocation;
                if (leftAlign) pt.X -= Allocation.Width;
                if (topAlign)  pt.Y -= Allocation.Height;
                Move(pt.X, pt.Y);
            }

            while (result == System.Windows.Forms.DialogResult.None)
                Application.RunIteration();

            Hide();

            return result;
        }

        public void ShowModal(FamiStudioForm parent = null)
        {
            Show();
        }

        public void UpdateModalEvents()
        {
            if (result != System.Windows.Forms.DialogResult.None)
            {
                Hide();
            }

            Application.RunIteration(false);
        }

        public void StayModalUntilClosed()
        {
            if (Visible)
            {
                while (result == System.Windows.Forms.DialogResult.None)
                    Application.RunIteration();

                Hide();
            }
        }

        public System.Windows.Forms.DialogResult DialogResult => result;
    }
}
