using System.Diagnostics;

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

        private int margin     = DpiScaling.ScaleForWindow(8);
        private int buttonSize = DpiScaling.ScaleForWindow(36);

        private Button buttonNo;
        private Button buttonYes;
        private Button buttonAdvanced;
        private Container propertyContainer;
        private PropertyPage propertyPage;

        private LocalizedString AcceptTooltip;
        private LocalizedString CancelTooltip;
        private LocalizedString ToggleAdvancedTooltip;

        public PropertyDialog(FamiStudioWindow win, string title, int width, bool canAccept = true, bool canCancel = true) : base(win, title)
        {
            Localization.Localize(this);

            width = DpiScaling.ScaleForWindow(width);
            Move(0, 0, width, width);
            Init();

            center = true;
            buttonYes.Visible = canAccept;
            buttonNo.Visible  = canCancel;
        }

        public PropertyDialog(FamiStudioWindow win, string title, Point pt, int w, bool leftAlign = false, bool top = false, bool mobileFullscreen = true) : base(win, title)
        {
            Localization.Localize(this);

            width = DpiScaling.ScaleForWindow(w);
            topAlign = top;
            if (leftAlign)
                pt.X -= width;
            Move(pt.X, pt.Y, width, width);
            Init();
        }

        private void Init()
        {
            propertyContainer = new Container();
            propertyContainer.Move(margin, margin + titleBarSizeY, Width - margin * 2, 100); // Height not known yet.
            propertyContainer.SetupClipRegion(false);

            propertyPage = new PropertyPage(propertyContainer, propertyContainer.Width);
            propertyPage.PropertyWantsClose += PropertyPage_PropertyWantsClose;

            buttonYes = new Button("Yes", null);
            buttonYes.Click += ButtonYes_Click;
            buttonYes.Resize(buttonSize, buttonSize);
            buttonYes.ToolTip = AcceptTooltip;

            buttonNo = new Button("No", null);
            buttonNo.Click += ButtonNo_Click;
            buttonNo.Resize(buttonSize, buttonSize);
            buttonNo.ToolTip = CancelTooltip;

            buttonAdvanced = new Button("PlusSmall", null);
            buttonAdvanced.Click += ButtonAdvanced_Click;
            buttonAdvanced.Resize(buttonSize, buttonSize);
            buttonAdvanced.Visible = false;
            buttonAdvanced.ToolTip = ToggleAdvancedTooltip;

            AddControl(buttonYes);
            AddControl(buttonNo);
            AddControl(buttonAdvanced);
            AddControl(propertyContainer);
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
            buttonAdvanced.ImageName = advancedPropertiesVisible ? "MinusSmall" : "PlusSmall";
            UpdateLayout();
        }

        protected override void OnShowDialog()
        {
            UpdateLayout();

            if (topAlign)
            {
                Move(left, base.top - height);
            }
            else if (!center && WindowRectangle.Bottom > ParentContainer.WindowRectangle.Bottom)
            {
                Move(left, ParentContainer.WindowRectangle.Bottom - height - 10);
            }

            if (center)
                CenterToWindow();
        }

        private void UpdateLayout()
        {
            propertyContainer.Resize(propertyContainer.Width, propertyPage.LayoutHeight);
            Resize(width, propertyContainer.Bottom + buttonNo.Height + margin * 2); 

            var buttonY = propertyPage.LayoutHeight + margin * 2 + titleBarSizeY;

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
                if (e.Key == Keys.Enter || e.Key == Keys.KeypadEnter)
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
