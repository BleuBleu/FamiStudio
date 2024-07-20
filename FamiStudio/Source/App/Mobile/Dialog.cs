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

        protected bool fullscreen = true;

        public DialogResult Result => result;
        public bool Fullscreen => fullscreen;

        public Control FocusedControl
        {
            get => null;
            set { }
        }

        public Dialog(FamiStudioWindow win, string t = "")
        {
            visible = false;
            win.InitDialog(this);
        }

        public void Close(DialogResult res)
        {
            result = res;
            callback?.Invoke(result);

            // Pop dialog after the callback, otherwise sometimes the callback takes some time
            // and this counts in the transition time, making the fade look abrupt.
            var numDialogsToPop = 1;
            if (fullscreen)
            {
                DialogClosing?.Invoke(this, result, ref numDialogsToPop);
            }
            window.PopDialog(this, numDialogsToPop);
        }

        protected virtual void OnShowDialog()
        {
        }

        public void ShowDialogAsync(Action<DialogResult> cb = null)
        {
            callback = cb;
            result = DialogResult.None;
            OnShowDialog();
            window.PushDialog(this);
        }

        // MATTT : Recenter non-fullscreen dialogs on resize.
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
        }

        protected override void OnTouchClick(PointerEventArgs e)
        {
            if (!fullscreen && !ClientRectangle.Contains(e.Position))
            {
                Close(DialogResult.Cancel);
            }
            else
            {
                base.OnTouchClick(e);
            }
        }

        public override bool HitTest(int winX, int winY)
        {
            // We eat all the inputs so we can close when clicking outside.
            return true;
        }

        protected override void OnRender(Graphics g)
        {
            if (!fullscreen)
            {
                g.OverlayCommandList.DrawRectangle(ClientRectangle, Theme.BlackColor, 1);
            }

            base.OnRender(g);
        }
    }
}
