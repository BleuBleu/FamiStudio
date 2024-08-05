using Android.Media.Audiofx;
using Android.Widget;
using System;

namespace FamiStudio
{
    public class PropertyDialog : TopBarDialog
    {
        public delegate bool ValidateDelegate(PropertyPage props);
        public event ValidateDelegate ValidateProperties;

        public PropertyPage Properties => propertyPage;

        private PropertyPage propertyPage;
        private TouchScrollContainer propertyContainer;

        public PropertyDialog(FamiStudioWindow win, string text, int width, bool canAccept = true, bool canCancel = true) : base(win, text)
        {
            Init(text);
        }

        public PropertyDialog(FamiStudioWindow win, string text, Point pt, int w, bool leftAlign = false, bool topAlign = false, bool mobileFullscreen = true) : base(win, text, mobileFullscreen)
        {
            Init(text);
        }

        private void Init(string title)
        {
            SetTickEnabled(true);

            propertyContainer = new TouchScrollContainer();
            propertyContainer.Move(clientRect.Left, clientRect.Top, clientRect.Width, clientRect.Height);
            AddControl(propertyContainer);

            propertyPage = new PropertyPage(propertyContainer, clientRect.Width);
            propertyPage.LayoutChanged += PropertyPage_LayoutChanged;
        }

        private void PropertyPage_LayoutChanged(PropertyPage props)
        {
            propertyContainer.VirtualSizeY = props.LayoutHeight;
            propertyContainer.ClampScroll();
        }

        public override void OnWindowResize(EventArgs e)
        {
            base.OnWindowResize(e);

            propertyPage.Build(clientRect.Width);
            SetFinalSize();
            CenterDialog(false);
        }

        protected void SetFinalSize()
        {
            if (!Fullscreen)
            {
                propertyContainer.Resize(width, Math.Min(height, propertyPage.LayoutHeight));
                Resize(width, topBarHeight + propertyContainer.Height, false);
            }
            else
            {
                propertyContainer.Resize(clientRect.Size);
            }
        }

        protected override void OnShowDialog()
        {
            SetFinalSize();
            base.OnShowDialog();
        }

        protected override void ButtonAccept_Click(Control sender)
        {
            if (ValidateProperties == null || ValidateProperties.Invoke(propertyPage))
            {
                base.ButtonAccept_Click(sender);
            }
        }
    }
}