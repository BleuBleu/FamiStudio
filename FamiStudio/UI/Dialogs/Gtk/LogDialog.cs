using Gtk;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Resources;

namespace FamiStudio
{
    public class LogDialog : Window
    {
        public LogDialog(List<string> messages) : base(WindowType.Toplevel)
        {
            Init();
            WidthRequest = 756;

#if FAMISTUDIO_LINUX
            TransientFor = FamiStudioForm.Instance;
            SetPosition(WindowPosition.CenterOnParent);
#endif
        }

        private void Init()
        {

            //Add(vbox);

            BorderWidth = 10;
            Resizable = false;
            Decorated = false;
            KeepAbove = true;
            Modal = true;
            SkipTaskbarHint = true;
        }

        public System.Windows.Forms.DialogResult ShowDialog(FamiStudioForm parent = null)
        {
            //            Show();

            //#if FAMISTUDIO_MACOS
            //            int x = parent.Bounds.Left + (parent.Bounds.Width  - Allocation.Width)  / 2;
            //            int y = parent.Bounds.Top  + (parent.Bounds.Height - Allocation.Height) / 2;
            //            Move(x, y);
            //            MacUtils.SetNSWindowAlwayOnTop(MacUtils.NSWindowFromGdkWindow(GdkWindow.Handle));
            //#endif

            //            while (result == System.Windows.Forms.DialogResult.None)
            //                Application.RunIteration();

            //            Hide();

            //#if FAMISTUDIO_MACOS
            //            MacUtils.RestoreMainNSWindowFocus();
            //#endif

            //return result;

            return System.Windows.Forms.DialogResult.OK;
        }
}
}
