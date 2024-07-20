using System;
using System.Runtime.InteropServices;
using static Android.Icu.Text.CaseMap;

namespace FamiStudio
{
    public class TopBarDialog : Dialog
    {
        // MATTT : We should have function to simply add buttons as we wish.
        protected Container topBarContainer;
        protected Button buttonCancel;
        protected Button buttonAccept;
        protected Label titleLabel;
        protected Rectangle dialogRect;

        public TopBarDialog(FamiStudioWindow win, string t = "") : base(win, t)
        {
            InitTopBar(t);
        }

        public bool AcceptButtonVisible
        {
            get => buttonAccept.Visible;
            set => buttonAccept.Visible = value;
        }

        public string AcceptButtonImage
        {
            get => buttonAccept.ImageName;
            set => buttonAccept.ImageName = value;
        }

        public bool CancelButtonVisible
        {
            get => buttonCancel.Visible;
            set => buttonCancel.Visible = value;
        }

        public string CancelButtonImage
        {
            get => buttonCancel.ImageName;
            set => buttonCancel.ImageName = value;
        }

        private void InitTopBar(string title)
        {
            var topBarHeight = DpiScaling.ScaleForWindow(24);
            var margin = DpiScaling.ScaleForWindow(2);
            var buttonSize = topBarHeight - margin * 2;

            topBarContainer = new GradientPanel(Theme.DarkGreyColor2);
            topBarContainer.Move(0, 0, ParentWindow.Width, DpiScaling.ScaleForWindow(24));
            AddControl(topBarContainer);

            buttonCancel = new Button("No");
            buttonCancel.Transparent = true;
            buttonCancel.Click += ButtonCancel_Click;
            buttonCancel.Move(margin, margin, buttonSize, buttonSize);
            topBarContainer.AddControl(buttonCancel);

            buttonAccept = new Button("Yes");
            buttonAccept.Transparent = true;
            buttonAccept.Click += ButtonAccept_Click;
            buttonAccept.Move(topBarContainer.Width - margin - buttonSize, margin, buttonSize, buttonSize);
            topBarContainer.AddControl(buttonAccept);

            titleLabel = new Label(title);
            titleLabel.Move(buttonCancel.Right + margin, 0, buttonAccept.Left - buttonCancel.Right - margin * 2, topBarHeight);
            titleLabel.Font = fonts.FontMediumBold;
            topBarContainer.AddControl(titleLabel);

            dialogRect = new Rectangle(0, topBarContainer.Height, ParentWindow.Width, ParentWindow.Height - topBarContainer.Height);
        }

        protected virtual void ButtonAccept_Click(Control sender)
        {
            Close(DialogResult.OK);
        }

        protected void ButtonCancel_Click(Control sender)
        {
            Close(DialogResult.Cancel);
        }
    }
}
