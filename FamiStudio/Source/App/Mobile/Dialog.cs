using System;
using System.Runtime.InteropServices;
using static Android.Icu.Text.CaseMap;

namespace FamiStudio
{
    public class Dialog : Container
    {
        public delegate void KeyDownDelegate(Dialog dlg, KeyEventArgs e);
        public delegate void DialogClosingDelegate(Dialog dlg, DialogResult result, ref int numDialogsToPop);

        public event KeyDownDelegate DialogKeyDown;
        public event DialogClosingDelegate DialogClosing;

        protected Action<DialogResult> callback;
        protected DialogResult result = DialogResult.None;

        protected Container topBarContainer;
        protected Button buttonNo;
        protected Button buttonYes;
        protected Label titleLabel;
        protected Rectangle dialogRect;

        public DialogResult Result => result;

        public Control FocusedControl
        {
            get => null;
            set { }
        }

        public Dialog(FamiStudioWindow win, string t = "")
        {
            visible = false;
            win.InitDialog(this);
            InitTopBar(t);
            //Title = t;
            //SetTickEnabled(true);
        }

        private void InitTopBar(string title)
        {
            var topBarHeight = DpiScaling.ScaleForWindow(24);
            var margin = DpiScaling.ScaleForWindow(2);
            var buttonSize = topBarHeight - margin * 2;

            topBarContainer = new GradientPanel(Theme.DarkGreyColor1);
            topBarContainer.Move(0, 0, ParentWindow.Width, DpiScaling.ScaleForWindow(24));
            AddControl(topBarContainer);

            buttonNo = new Button("No");
            buttonNo.Transparent = true;
            buttonNo.Click += ButtonNo_Click;
            buttonNo.Move(margin, margin, buttonSize, buttonSize);
            topBarContainer.AddControl(buttonNo);

            buttonYes = new Button("Yes");
            buttonYes.Transparent = true;
            buttonYes.Click += ButtonYes_Click;
            buttonYes.Move(topBarContainer.Width - margin - buttonSize, margin, buttonSize, buttonSize);
            topBarContainer.AddControl(buttonYes);

            titleLabel = new Label(title);
            titleLabel.Move(buttonNo.Right + margin, 0, buttonYes.Left - buttonNo.Right - margin * 2, topBarHeight);
            titleLabel.Font = fonts.FontMediumBold;
            topBarContainer.AddControl(titleLabel);

            dialogRect = new Rectangle(0, topBarContainer.Height, ParentWindow.Width, ParentWindow.Height - topBarContainer.Height);
        }

        protected virtual void ButtonYes_Click(Control sender)
        {
            Close(DialogResult.OK);
        }

        protected void ButtonNo_Click(Control sender)
        {
            Close(DialogResult.Cancel);
        }

        public void Close(DialogResult res)
        {
            result = res;
            callback?.Invoke(result);

            // Pop dialog after the callback, otherwise sometimes the callback takes some time
            // and this counts in the transition time, making the fade look abrupt.
            var numDialogsToPop = 1;
            DialogClosing?.Invoke(this, result, ref numDialogsToPop);
            window.PopDialog(this, numDialogsToPop);
        }

        protected virtual void OnShowDialog()
        {
        }

        public void ShowDialogAsync(Action<DialogResult> cb)
        {
            callback = cb;
            result = DialogResult.None;
            OnShowDialog();
            window.PushDialog(this);
        }
    }
}
