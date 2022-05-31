using System.Diagnostics;
using System.Drawing;

// MATTT Move to \Desktop
namespace FamiStudio
{
    public partial class PropertyDialog : Dialog
    {
        public delegate bool ValidateDelegate(PropertyPage props);
        public event ValidateDelegate ValidateProperties;

        public PropertyPage Properties => propertyPage;
        private bool topAlign = false;
        private bool center = false;
        private bool advancedPropertiesVisible = false;
        private int margin = DpiScaling.ScaleForMainWindow(8);

        private Button buttonNo;
        private Button buttonYes;
        private Button buttonAdvanced;
        private PropertyPage propertyPage;

        public PropertyDialog(string title, int width, bool canAccept = true, bool canCancel = true)
        {
            width = DpiScaling.ScaleForMainWindow(width);
            Move(0, 0, width, width);
            Init();

            center = true;
            buttonYes.Visible = canAccept;
            buttonNo.Visible  = canCancel;
        }

        public PropertyDialog(string title, Point pt, int width, bool leftAlign = false, bool topAlign = false)
        {
            this.width = DpiScaling.ScaleForMainWindow(width);
            this.topAlign = topAlign;
            if (leftAlign)
                pt.X -= width;
            Move(pt.X, pt.Y, width, width);
            Init();
        }

        private void Init()
        {
            propertyPage = new PropertyPage(this, margin, margin, Width - margin * 2);
            propertyPage.PropertyWantsClose += PropertyPage_PropertyWantsClose;

            buttonYes = new Button("Yes", null);
            buttonYes.Click += ButtonYes_Click;
            buttonYes.Resize(DpiScaling.ScaleForMainWindow(36), DpiScaling.ScaleForMainWindow(36));
            buttonYes.ToolTip = "Accept";

            buttonNo = new Button("No", null);
            buttonNo.Click += ButtonNo_Click;
            buttonNo.Resize(DpiScaling.ScaleForMainWindow(36), DpiScaling.ScaleForMainWindow(36));
            buttonNo.ToolTip = "Cancel";

            buttonAdvanced = new Button("PlusSmall", null);
            buttonAdvanced.Click += ButtonAdvanced_Click;
            buttonAdvanced.Resize(DpiScaling.ScaleForMainWindow(36), DpiScaling.ScaleForMainWindow(36));
            buttonAdvanced.Visible = false;
            buttonAdvanced.ToolTip = "Toggle Advanced Options";

            AddControl(buttonYes);
            AddControl(buttonNo);
            AddControl(buttonAdvanced);
        }

        private void PropertyPage_PropertyWantsClose(int idx)
        {
            Close(DialogResult2.OK);
        }

        private void ButtonYes_Click(Control sender)
        {
            if (ValidateProperties == null || ValidateProperties.Invoke(propertyPage))
            {
                Close(DialogResult2.OK);
            }
        }

        private void ButtonNo_Click(Control sender)
        {
            Close(DialogResult2.Cancel);
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
                    Close(DialogResult2.OK);
                }
                else if (e.Key == Keys.Escape)
                {
                    Close(DialogResult2.Cancel);
                }
            }
        }
    }
}
