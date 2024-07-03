using Android.Media.Audiofx;
using Android.Widget;
using System;

namespace FamiStudio
{
    public class PropertyDialog : Dialog
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

        public PropertyDialog(FamiStudioWindow win, string text, int width, bool canAccept = true, bool canCancel = true, object parent = null) : base(win, text)
        {
            Init(text);
        }

        public PropertyDialog(FamiStudioWindow win, string text, Point pt, int width, bool leftAlign = false, bool topAlign = false) : base(win, text)
        {
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
            Move(0, 0, ParentWindow.Width, ParentWindow.Height);

            // MATTT : Switch to Panel.cs when merging code from Sequencer proto.
            // MATTT : Move this to Dialog.cs if we end up using the "top bar" thing often.
            // MATTT : Align nicely with desktop.

            propertyContainer = new TouchScrollContainer();
            propertyContainer.Move(dialogRect.Left, dialogRect.Top, dialogRect.Width, dialogRect.Height);
            AddControl(propertyContainer);

            propertyPage = new PropertyPage(propertyContainer, Width);
        }

        protected override void ButtonYes_Click(Control sender)
        {
            if (ValidateProperties == null || ValidateProperties.Invoke(propertyPage))
            {
                base.ButtonYes_Click(sender);
            }
        }

        protected override void OnShowDialog()
        {
            propertyContainer.VirtualSizeY = propertyPage.LayoutHeight;
        }
    }
}