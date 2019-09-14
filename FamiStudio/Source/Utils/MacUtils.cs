using System;
using System.Runtime.InteropServices;

namespace FamiStudio
{
    static class MacUtils
    {
        static IntPtr appKit;
        static IntPtr mainNsWindow;

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "sel_registerName")]
        public extern static IntPtr SelRegisterName(string name);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_getClass")]
        public extern static IntPtr ObjCGetClass(string name);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        public extern static void SendVoid(IntPtr receiver, IntPtr selector);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        public extern static int SendInt(IntPtr receiver, IntPtr selector);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        public extern static IntPtr SendIntPtr(IntPtr receiver, IntPtr selector);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        public extern static IntPtr SendIntPtr(IntPtr receiver, IntPtr selector, int int1);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        public extern static IntPtr SendIntPtr(IntPtr receiver, IntPtr selector, IntPtr intPtr1);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        public extern static IntPtr SendIntPtr(IntPtr receiver, IntPtr selector, IntPtr intPtr1, int int1);

        //[DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        //public extern static NSPoint SendPoint(IntPtr receiver, IntPtr selector);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        public static extern NSPointF SendPointF(IntPtr receiver, IntPtr selector);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        public static extern NSPointD SendPointD(IntPtr receiver, IntPtr selector);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "class_replaceMethod")]
        public static extern void ClassReplaceMethod(IntPtr classHandle, IntPtr selector, IntPtr method, string types);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_lookUpClass")]
        public static extern IntPtr ClassLookup(string name);

        [DllImport("/System/Library/Frameworks/AppKit.framework/AppKit")]
        public static extern void NSBeep();

        [DllImport("/System/Library/Frameworks/ApplicationServices.framework/Frameworks/CoreText.framework/CoreText")]
        public static extern bool CTFontManagerRegisterFontsForURL(IntPtr fontUrl, int scope, IntPtr error);

        [DllImport("libgdk-quartz-2.0.0.dylib", EntryPoint = "gdk_quartz_window_get_nswindow")]
        public static extern IntPtr NSWindowFromGdkWindow(IntPtr window);

        [DllImport("libdl.dylib")]
        internal static extern IntPtr dlopen(String fileName, int flags);

        [DllImport("/usr/lib/system/libsystem_c.dylib")]
        internal static extern int usleep(uint microseconds);

        const int NSOKButton = 1;
        const int NSWindowZoomButton = 2;
        const int NSAlertFirstButtonReturn  = 1000;
        const int NSAlertSecondButtonReturn = 1001;

        public static void Initialize(IntPtr nsWin)
        {
            mainNsWindow = nsWin;
            appKit = LoadLibrary("/System/Library/Frameworks/AppKit.framework/AppKit");
        }

        public static IntPtr ToNSString(string str)
        {
            if (str == null)
                return IntPtr.Zero;

            unsafe
            {
                fixed (char* ptrFirstChar = str)
                {
                    var handle = SendIntPtr(MacUtils.ObjCGetClass("NSString"), SelRegisterName("alloc"));
                    handle = MacUtils.SendIntPtr(handle, MacUtils.SelRegisterName("initWithCharacters:length:"), (IntPtr)ptrFirstChar, str.Length);
                    return handle;
                }
            }
        }

        public static IntPtr ToNSURL(string filepath)
        {
            return SendIntPtr(
                ObjCGetClass("NSURL"),
                SelRegisterName("fileURLWithPath:"),
                ToNSString(filepath));
        }

        public static unsafe string FromNSURL(IntPtr url)
        {
            var str = SendIntPtr(url, SelRegisterName("path"));
            var charPtr = SendIntPtr(str, SelRegisterName("UTF8String"));
            return Marshal.PtrToStringAnsi(charPtr);
        }

        static unsafe public IntPtr ToNSArray(params string[] items)
        {
            IntPtr buf = Marshal.AllocHGlobal((items.Length) * IntPtr.Size);
            for (int i = 0; i < items.Length; i++)
                Marshal.WriteIntPtr(buf, i * IntPtr.Size, ToNSString(items[i]));

            var array = SendIntPtr(
                ObjCGetClass("NSArray"),
                SelRegisterName("arrayWithObjects:count:"),
                buf,
                items.Length);

            Marshal.FreeHGlobal(buf);

            return array;

        }

        public static void CoreTextRegisterFont(string fontfile)
        {
            var url = ToNSURL(fontfile);
            CTFontManagerRegisterFontsForURL(url, 1, IntPtr.Zero);
        }

        internal struct NSFloat
        {
            public IntPtr Value;

            public static implicit operator NSFloat(float v)
            {
                var f = new NSFloat();
                unsafe
                {
                    if (IntPtr.Size == 4)
                    {
                        f.Value = *(IntPtr*)&v;
                    }
                    else
                    {
                        double d = v;
                        f.Value = *(IntPtr*)&d;
                    }
                }

                return f;
            }

            public static implicit operator NSFloat(double v)
            {
                var f = new NSFloat();
                unsafe
                {
                    if (IntPtr.Size == 4)
                    {
                        var fv = (float)v;
                        f.Value = *(IntPtr*)&fv;
                    }
                    else
                    {
                        f.Value = *(IntPtr*)&v;
                    }
                }

                return f;
            }

            public static implicit operator float(NSFloat f)
            {
                unsafe
                {
                    if (IntPtr.Size == 4)
                    {
                        return *(float*)&f.Value;
                    }

                    return (float)*(double*)&f.Value;
                }
            }

            public static implicit operator double(NSFloat f)
            {
                unsafe
                {
                    if (IntPtr.Size == 4)
                    {
                        return *(float*)&f.Value;
                    }

                    return *(double*)&f.Value;
                }
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct NSPoint
        {
            public NSFloat X;
            public NSFloat Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct NSPointF
        {
            public float X;
            public float Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct NSPointD
        {
            public double X;
            public double Y;
        }

        public static NSPoint SendPoint(IntPtr receiver, IntPtr selector)
        {
            var r = new NSPoint();

            unsafe
            {
                if (IntPtr.Size == 4)
                {
                    var pf = SendPointF(receiver, selector);
                    r.X.Value = *(IntPtr*)&pf.X;
                    r.Y.Value = *(IntPtr*)&pf.Y;
                }
                else
                {
                    var pd = SendPointD(receiver, selector);
                    r.X.Value = *(IntPtr*)&pd.X;
                    r.Y.Value = *(IntPtr*)&pd.Y;
                }
            }

            return r;
        }

        public static System.Windows.Forms.MouseButtons GetMouseButtons()
        {
            int macButtons = SendInt(ObjCGetClass("NSEvent"), SelRegisterName("pressedMouseButtons"));

            System.Windows.Forms.MouseButtons buttons = 0;
            if ((macButtons & 1) != 0) buttons |= System.Windows.Forms.MouseButtons.Left;
            if ((macButtons & 2) != 0) buttons |= System.Windows.Forms.MouseButtons.Right;
            if ((macButtons & 4) != 0) buttons |= System.Windows.Forms.MouseButtons.Middle;
            return buttons;
        }

        public static System.Drawing.Point GetMousePosition(int displayHeight)
        {
            NSPoint mouseLoc = SendPoint(ObjCGetClass("NSEvent"), SelRegisterName("mouseLocation"));
            var pt = new System.Drawing.Point();
            pt.X = (int)Math.Round((float)mouseLoc.X);
            pt.Y = (int)Math.Round((float)mouseLoc.Y);
            pt.Y = displayHeight - pt.Y - 1;

            return pt;
        }

        public static IntPtr LoadLibrary(string fileName)
        {
            const int RTLD_NOW = 2;
            return dlopen(fileName, RTLD_NOW);
        }

        public static void SetNSWindowAlwayOnTop(IntPtr nsWin)
        {
            SendIntPtr(nsWin, SelRegisterName("makeKeyAndOrderFront:"), IntPtr.Zero);
            SendIntPtr(nsWin, SelRegisterName("setLevel:"), 25);
        }

        public static void SetNSWindowFocus(IntPtr nsWin)
        {
            SendIntPtr(nsWin, SelRegisterName("makeKeyAndOrderFront:"), nsWin);
        }

        public static void RestoreMainNSWindowFocus()
        {
            SetNSWindowFocus(mainNsWindow);
        }

        public static void RemoveMaximizeButton(IntPtr nsWin)
        {
            var btn = SendIntPtr(nsWin, SelRegisterName("standardWindowButton:"), NSWindowZoomButton);
            SendIntPtr(btn, SelRegisterName("setHidden:"), 1);
        }

        public static string ShowOpenDialog(string title, string[] extensions, string path = null)
        {
            var clsOpenPanel = ObjCGetClass("NSOpenPanel");
            var openPanel = SendIntPtr(clsOpenPanel, SelRegisterName("openPanel"));
            SendIntPtr(openPanel, SelRegisterName("setTitle:"), ToNSString(title));

            if (path != null)
            {
                var url = ToNSURL(path);
                SendIntPtr(openPanel, SelRegisterName("setDirectoryURL:"), url);
            }

            var fileTypesArray = ToNSArray(extensions);
            SendIntPtr(openPanel, SelRegisterName("setAllowedFileTypes:"), fileTypesArray);

            var status = SendInt(openPanel, SelRegisterName("runModal"));

            SetNSWindowFocus(mainNsWindow);

            if (status == NSOKButton)
            {
                var url = SendIntPtr(openPanel, SelRegisterName("URL"));
                return FromNSURL(url);
            }

            return null;
        }

        public static string ShowSaveDialog(string title, string[] extensions, string path = null)
        {
            var clsSavePanel = ObjCGetClass("NSSavePanel");
            var savePanel = SendIntPtr(clsSavePanel, SelRegisterName("savePanel"));
            SendIntPtr(savePanel, SelRegisterName("setTitle:"), ToNSString(title));

            if (path != null)
            {
                var url = ToNSURL(path);
                SendIntPtr(savePanel, SelRegisterName("setDirectoryURL:"), url);
            }

            var fileTypesArray = ToNSArray(extensions);
            SendIntPtr(savePanel, SelRegisterName("setAllowedFileTypes:"), fileTypesArray);

            var status = SendInt(savePanel, SelRegisterName("runModal"));

            SetNSWindowFocus(mainNsWindow);

            if (status == NSOKButton)
            {
                var url = SendIntPtr(savePanel, SelRegisterName("URL"));
                return FromNSURL(url);
            }

            return null;
        }

        public static System.Windows.Forms.DialogResult ShowAlert(string text, string title, System.Windows.Forms.MessageBoxButtons buttons)
        {
            var alert = SendIntPtr(SendIntPtr(ObjCGetClass("NSAlert"), SelRegisterName("alloc")), SelRegisterName("init"));

            SendIntPtr(alert, SelRegisterName("setMessageText:"), ToNSString(title));
            SendIntPtr(alert, SelRegisterName("setInformativeText:"), ToNSString(text));
            SendIntPtr(alert, SelRegisterName("setAlertStyle:"), 2);

            if (buttons == System.Windows.Forms.MessageBoxButtons.YesNo ||
                buttons == System.Windows.Forms.MessageBoxButtons.YesNoCancel)
            {
                SendIntPtr(alert, SelRegisterName("addButtonWithTitle:"), ToNSString("Yes"));
                SendIntPtr(alert, SelRegisterName("addButtonWithTitle:"), ToNSString("No"));

                if (buttons == System.Windows.Forms.MessageBoxButtons.YesNoCancel)
                {
                    SendIntPtr(alert, SelRegisterName("addButtonWithTitle:"), ToNSString("Cancel"));
                }
            }

            var ret = SendInt(alert, SelRegisterName("runModal"));

            SetNSWindowFocus(mainNsWindow);

            if (buttons == System.Windows.Forms.MessageBoxButtons.YesNo ||
                buttons == System.Windows.Forms.MessageBoxButtons.YesNoCancel)
            {
                if (ret == NSAlertFirstButtonReturn)
                    return System.Windows.Forms.DialogResult.Yes;
                if (ret == NSAlertSecondButtonReturn)
                    return System.Windows.Forms.DialogResult.No;

                return System.Windows.Forms.DialogResult.Cancel;
            }
            else
            {
                return System.Windows.Forms.DialogResult.OK;
            }
        }
    };
}
