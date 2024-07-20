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

        /*
        private string title = "";
        private string verb = "Apply";
        private bool canAccept = true;
        private bool canCancel = true;

        public string Title => title;
        public string Verb  => verb;
        public bool CanAccept => canAccept;
        public bool CanCancel => canCancel;
        */

        public PropertyDialog(FamiStudioWindow win, string text, int width, bool canAccept = true, bool canCancel = true) : base(win, text)
        {
            Init(text);
        }

        public PropertyDialog(FamiStudioWindow win, string text, Point pt, int w, bool leftAlign = false, bool topAlign = false, bool mobileFullscreen = true) : base(win, text)
        {
            fullscreen = mobileFullscreen;
            if (!fullscreen)
            {
                var size = Math.Min(window.Width, window.Height) * 9 / 10;
                width  = size;
                height = size;
            }
            //title = text;
            Init(text);
        }

        public void SetVerb(string text)
        {
            //verb = text;
        }

        private void Init(string title)
        {
            SetTickEnabled(true);

            if (fullscreen)
            {
                Move(0, 0, ParentWindow.Width, ParentWindow.Height);
            }
            else
            {
                Move((window.Width - width) / 2, (window.Height - height) / 2, width, height);
            }

            // MATTT : Switch to Panel.cs when merging code from Sequencer proto.
            // MATTT : Move this to Dialog.cs if we end up using the "top bar" thing often.
            // MATTT : Align nicely with desktop.

            // MATTT : This is wrong, the dialogRect isnt updated when we do the "Move" above".
            propertyContainer = new TouchScrollContainer();
            propertyContainer.Move(dialogRect.Left, dialogRect.Top, dialogRect.Width, dialogRect.Height);
            AddControl(propertyContainer);

            propertyPage = new PropertyPage(propertyContainer, Width);
            propertyPage.LayoutChanged += PropertyPage_LayoutChanged;
        }

        private void PropertyPage_LayoutChanged(PropertyPage props)
        {
            propertyContainer.VirtualSizeY = props.LayoutHeight;
            propertyContainer.ClampScroll();
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