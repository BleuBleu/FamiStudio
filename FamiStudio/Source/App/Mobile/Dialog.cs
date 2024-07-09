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

        protected bool fullscreen;
        protected bool closing;
        protected float fade;

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

            if (fullscreen)
            {
                // Pop dialog after the callback, otherwise sometimes the callback takes some time
                // and this counts in the transition time, making the fade look abrupt.
                var numDialogsToPop = 1;
                DialogClosing?.Invoke(this, result, ref numDialogsToPop);
                window.PopDialog(this, numDialogsToPop);
            }
            else
            {
                SetTickEnabled(true);
                closing = true;
            }
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

            if (!fullscreen)
            {
                SetTickEnabled(true);
                closing = false;
            }
        }

        // MATTT : Recenter non-fullscreen dialogs on resize.
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
        }

        protected override void OnPointerDown(PointerEventArgs e)
        {
            if (!ClientRectangle.Contains(e.Position))
            {
                Close(DialogResult.Cancel);
            }
            else
            {
                base.OnPointerDown(e);
            }
        }

        public override bool HitTest(int winX, int winY)
        {
            // We eat all the inputs. 
            return fullscreen || fade == 1.0f;
        }

        public override void Tick(float delta)
        {
            base.Tick(delta);

            if (!fullscreen)
            {
                MarkDirty(); // Must call before PopDialog otherwise wont reach window.

                // MATTT : Move this to FamiStudio container. Dialog shouldnt know how they transition.
                if (!closing)
                {
                    fade = Math.Min(fade + delta * 6, 1.0f);
                    if (fade == 1.0f)
                    {
                        SetTickEnabled(false);
                    }
                }
                else
                {
                    fade = Math.Max(fade - delta * 10, 0.0f);
                    if (fade == 0.0f)
                    {
                        window.PopDialog(this);
                        SetTickEnabled(false);
                        closing = false;
                    }
                }
            }
        }

        protected override void OnRender(Graphics g)
        {
            if (!fullscreen)
            {
                var o = g.OverlayCommandList;

                o.DrawRectangle(ClientRectangle, Theme.BlackColor, 1);

                if (fade != 0.0f)
                {
                    g.Transform.GetOrigin(out var ox, out var oy);
                    var dialogRect = ClientRectangle;
                    var screenRect = new Rectangle(Point.Empty, ParentWindow.Size);
                    screenRect.Offset(-(int)ox, -(int)oy);

                    var shadowColor = Color.FromArgb((int)Utils.Clamp(fade * 0.6f * 255.0f, 0, 255), Color.Black);
                    o.FillRectangle(screenRect.Left, screenRect.Top, dialogRect.Left, screenRect.Bottom, shadowColor);
                    o.FillRectangle(dialogRect.Left, screenRect.Top, dialogRect.Right, dialogRect.Top, shadowColor);
                    o.FillRectangle(dialogRect.Left, dialogRect.Bottom, dialogRect.Right, screenRect.Bottom, shadowColor);
                    o.FillRectangle(dialogRect.Right, screenRect.Top, screenRect.Right, screenRect.Bottom, shadowColor);
                }
            }

            base.OnRender(g);
        }
    }
}
