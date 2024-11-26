using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace FamiStudio
{
    public class TopBarDialog : Dialog
    {
        protected Container topBarContainer;
        protected Button buttonCancel;
        protected Button buttonAccept;
        protected Button buttonCustom;
        protected Label titleLabel;

        protected Rectangle clientRect;
        protected Rectangle dialogRect;

        protected int topBarHeight = DpiScaling.ScaleForWindow(24);
        protected int topBarMargin = DpiScaling.ScaleForWindow(2);

        public delegate void CustomActionActivatedDelegate();
        public event CustomActionActivatedDelegate CustomActionActivated;

        public TopBarDialog(FamiStudioWindow win, string t = "", bool fs = true) : base(win, t, fs)
        {
            Init(t);
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

        private void Init(string title)
        {
            topBarContainer = new PanelContainer(Theme.DarkGreyColor2);
            AddControl(topBarContainer);

            buttonCancel = new Button("No");
            buttonCancel.Transparent = true;
            buttonCancel.Click += ButtonCancel_Click;
            topBarContainer.AddControl(buttonCancel);

            buttonAccept = new Button("Yes");
            buttonAccept.Transparent = true;
            buttonAccept.Click += ButtonAccept_Click;
            topBarContainer.AddControl(buttonAccept);

            titleLabel = new Label(title);
            titleLabel.Font = fonts.FontMediumBold;
            topBarContainer.AddControl(titleLabel);

            UpdateLayoutRects();
            Resize(dialogRect.Width, dialogRect.Height, false);
            PositionTopBarControls();
        }

        private void UpdateLayoutRects()
        {
            var dialogWidth  = ParentContainer.Width;
            var dialogHeight = ParentContainer.Height;

            if (!Fullscreen)
            {
                var maxHeight = Math.Min(window.Width, window.Height) * 9 / 10;

                dialogWidth  = Math.Min(window.Width, window.Height) * 9 / 10;
                dialogHeight = dialogWidth - topBarHeight;

            }

            clientRect = new Rectangle(0, topBarHeight, dialogWidth, dialogHeight - topBarHeight);
            dialogRect = new Rectangle(0, 0, dialogWidth, dialogHeight);
        }

        public void SetCustomAction(string label)
        {
            Debug.Assert(buttonCustom == null);
            buttonCustom = new Button(null, label);
            buttonCustom.Transparent = true;
            buttonCustom.Click += CustomButton_Click;
            topBarContainer.AddControl(buttonCustom);
            buttonCustom.AutosizeWidth();
            PositionTopBarControls();
        }

        private void CustomButton_Click(Control sender)
        {
            CustomActionActivated?.Invoke();
        }

        private void PositionTopBarControls()
        {
            var buttonSize = topBarHeight - topBarMargin * 2;

            topBarContainer.Resize(dialogRect.Width, topBarHeight);
            buttonCancel.Move(topBarMargin, topBarMargin, buttonSize, buttonSize);
            buttonAccept.Move(topBarContainer.Width - topBarMargin - buttonSize, topBarMargin, buttonSize, buttonSize);
            titleLabel.Move(buttonCancel.Right + topBarMargin, 0, buttonAccept.Left - buttonCancel.Right - topBarMargin * 2, topBarHeight);

            if (buttonCustom != null)
            {
                buttonCustom.Move(buttonAccept.Left - topBarMargin - buttonCustom.Width, topBarMargin, buttonCustom.Width, topBarHeight - topBarMargin * 2);
            }
        }

        protected virtual void ButtonAccept_Click(Control sender)
        {
            Close(DialogResult.OK);
        }

        protected virtual void ButtonCancel_Click(Control sender)
        {
            Close(DialogResult.Cancel);
        }

        public override void OnWindowResize(EventArgs e)
        {
            UpdateLayoutRects();
            PositionTopBarControls();
            CenterDialog(false);
        }

        protected void CenterDialog(bool force = true)
        {
            if (Fullscreen)
            {
                Move(0, 0, ParentWindow.Width, ParentWindow.Height);
            }
            else
            {
                var px = (window.Width  - width)  / 2;
                var py = (window.Height - height) / 2;
                var rx = (px - left) / (float)ParentWindowSize.Width;
                var ry = (py - top)  / (float)ParentWindowSize.Height;
                
                var wr = new Rectangle(0, 0, ParentWindowSize.Width, ParentWindowSize.Height);  
                var dr = new Rectangle(px, py, width, height);
                
                // Dont recenter if small difference and still fits. Happens when popping the keyboard.
                if (force || rx > 0.1f | ry > 0.1f || !wr.Contains(dr))
                {
                    Move((window.Width - width) / 2, (window.Height - height) / 2, width, height);
                }
            }
        }

        protected override void OnShowDialog()
        {
            CenterDialog();
        }
    }
}
