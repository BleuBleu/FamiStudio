using System.Diagnostics;
using System.Drawing;

namespace FamiStudio
{
    public class PropertyDialog : Dialog
    {
        public delegate bool ValidateDelegate(PropertyPage props);
        public event ValidateDelegate ValidateProperties;

        public PropertyPage Properties => propertyPage;
        private bool topAlign = false;
        private bool center = false;
        private bool advancedPropertiesVisible = false;
        private int margin = DpiScaling.ScaleForWindow(8);

        private Button buttonNo;
        private Button buttonYes;
        private Button buttonAdvanced;
        private PropertyPage propertyPage;

        public PropertyDialog(FamiStudioWindow win, string title, int width, bool canAccept = true, bool canCancel = true) : base(win)
        {
            width = DpiScaling.ScaleForWindow(width);
            Move(0, 0, width, width);
            Init();

            center = true;
            buttonYes.Visible = canAccept;
            buttonNo.Visible  = canCancel;
        }

        public PropertyDialog(FamiStudioWindow win, string title, Point pt, int w, bool leftAlign = false, bool top = false) : base(win)
        {
            width = DpiScaling.ScaleForWindow(w);
            topAlign = top;
            if (leftAlign)
                pt.X -= width;
            Move(pt.X, pt.Y, width, width);
            Init();
        }

        private void Init()
        {
            propertyPage = new PropertyPage(this, margin, margin, Width - margin * 2);
            propertyPage.PropertyWantsClose += PropertyPage_PropertyWantsClose;

            buttonYes = new Button(this, "Yes", null);
            buttonYes.Click += ButtonYes_Click;
            buttonYes.Resize(DpiScaling.ScaleForWindow(36), DpiScaling.ScaleForWindow(36));
            buttonYes.ToolTip = "Accept";

            buttonNo = new Button(this, "No", null);
            buttonNo.Click += ButtonNo_Click;
            buttonNo.Resize(DpiScaling.ScaleForWindow(36), DpiScaling.ScaleForWindow(36));
            buttonNo.ToolTip = "Cancel";

            buttonAdvanced = new Button(this, "PlusSmall", null);
            buttonAdvanced.Click += ButtonAdvanced_Click;
            buttonAdvanced.Resize(DpiScaling.ScaleForWindow(36), DpiScaling.ScaleForWindow(36));
            buttonAdvanced.Visible = false;
            buttonAdvanced.ToolTip = "Toggle Advanced Options";

            AddControl(buttonYes);
            AddControl(buttonNo);
            AddControl(buttonAdvanced);
        }

        private void PropertyPage_PropertyWantsClose(int idx)
        {
            Close(DialogResult.OK);
        }

        private void ButtonYes_Click(Control sender)
        {
            if (ValidateProperties == null || ValidateProperties.Invoke(propertyPage))
            {
                Close(DialogResult.OK);
            }
        }

        private void ButtonNo_Click(Control sender)
        {
            Close(DialogResult.Cancel);
        }

        private void ButtonAdvanced_Click(Control sender)
        {
            Debug.Assert(propertyPage.HasAdvancedProperties);

            advancedPropertiesVisible = !advancedPropertiesVisible;
            propertyPage.Build(advancedPropertiesVisible);
            buttonAdvanced.Image = advancedPropertiesVisible ? "MinusSmall" : "PlusSmall";
            UpdateLayout();
        }

        protected override void OnShowDialog()
        {
            UpdateLayout();

            if (topAlign)
                Move(left, base.top - height);

            if (center)
                CenterToWindow();
        }

        private void UpdateLayout()
        {
            Resize(width, propertyPage.LayoutHeight + buttonNo.Height + margin * 3); 

            var buttonY = propertyPage.LayoutHeight + margin * 2;

            if (buttonNo.Visible)
            {
                buttonYes.Move(Width - buttonYes.Width * 2 - margin * 2, buttonY); 
                buttonNo.Move(Width - buttonNo.Width - margin, buttonY); 
            }
            else
            {
                buttonYes.Move(Width - buttonNo.Width - margin, buttonY);
            }

            if (propertyPage.HasAdvancedProperties)
            {
                buttonAdvanced.Move(margin, buttonY);
                buttonAdvanced.Visible = true;
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (!e.Handled)
            {
                if (e.Key == Keys.Enter)
                {
                    Close(DialogResult.OK);
                }
                else if (e.Key == Keys.Escape)
                {
                    Close(DialogResult.Cancel);
                }
            }
        }
    }
}
