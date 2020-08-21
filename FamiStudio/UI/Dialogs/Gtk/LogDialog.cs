using Gtk;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Resources;

namespace FamiStudio
{
    public class LogDialog : Window
    {
        private FlatButton buttonOK;
        private TextView   textView;
        private System.Windows.Forms.DialogResult result = System.Windows.Forms.DialogResult.None;

        public LogDialog(List<string> messages) : base(WindowType.Toplevel)
        {
            Init(messages);
            WidthRequest = 800;

#if FAMISTUDIO_LINUX
            TransientFor = FamiStudioForm.Instance;
            SetPosition(WindowPosition.CenterOnParent);
#endif
        }

        private void Init(List<string> messages)
        {
            textView = new TextView();
            textView.Buffer.Text = string.Join("\n", messages);
            textView.Editable = false;
            textView.CursorVisible = false;
            textView.Show();

            var scroll = new ScrolledWindow(null, null);
            scroll.HscrollbarPolicy = PolicyType.Never;
            scroll.VscrollbarPolicy = PolicyType.Automatic;
            scroll.HeightRequest = 400;
            scroll.ShadowType = ShadowType.EtchedIn;
            scroll.Show();
            scroll.Add(textView);

            var suffix = GLTheme.DialogScaling >= 2.0f ? "@2x" : "";
            buttonOK = new FlatButton(Gdk.Pixbuf.LoadFromResource($"FamiStudio.Resources.Yes{suffix}.png"));
            buttonOK.ButtonPressEvent += ButtonOK_ButtonPressEvent;
            buttonOK.Show();

            var buttonsAlign = new Alignment(1.0f, 0.5f, 0.0f, 0.0f);
            buttonsAlign.TopPadding = 5;
            buttonsAlign.Show();
            buttonsAlign.Add(buttonOK);

            var vbox = new VBox();
            vbox.PackStart(scroll, false, false, 0);
            vbox.PackStart(buttonsAlign, false, false, 0);
            vbox.Show();

            Add(vbox);

            BorderWidth = 10;
            Resizable = false;
            Decorated = false;
            KeepAbove = true;
            Modal = true;
            SkipTaskbarHint = true;
        }

        void ButtonOK_ButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            result = System.Windows.Forms.DialogResult.OK;
        }

        protected override bool OnKeyPressEvent(Gdk.EventKey evnt)
        {
            if (evnt.Key == Gdk.Key.Return ||
                evnt.Key == Gdk.Key.Escape)
            {
                result = System.Windows.Forms.DialogResult.OK;
            }

            return base.OnKeyPressEvent(evnt);
        }

        public System.Windows.Forms.DialogResult ShowDialog(FamiStudioForm parent = null)
        {
            Show();

            #if FAMISTUDIO_MACOS
                int x = parent.Bounds.Left + (parent.Bounds.Width  - Allocation.Width)  / 2;
                int y = parent.Bounds.Top  + (parent.Bounds.Height - Allocation.Height) / 2;
                Move(x, y);
                MacUtils.SetNSWindowAlwayOnTop(MacUtils.NSWindowFromGdkWindow(GdkWindow.Handle));
            #endif

            while (result == System.Windows.Forms.DialogResult.None)
                Application.RunIteration();

            Hide();

            #if FAMISTUDIO_MACOS
                MacUtils.RestoreMainNSWindowFocus();
            #endif

            return result;
        }
    }
}
