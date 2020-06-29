using System;
using System.Diagnostics;
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
        public extern static void SendVoid(IntPtr receiver, IntPtr selector, IntPtr intPtr1);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        public extern static void SendVoid(IntPtr receiver, IntPtr selector, IntPtr intPtr1, IntPtr intPtr2);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        public extern static void SendVoid(IntPtr receiver, IntPtr selector, NSRect rect1, IntPtr intPtr1);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        public extern static int SendInt(IntPtr receiver, IntPtr selector);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        public extern static double SendFloat(IntPtr receiver, IntPtr selector);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        public extern static IntPtr SendIntPtr(IntPtr receiver, IntPtr selector);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        public extern static IntPtr SendIntPtr(IntPtr receiver, IntPtr selector, int int1);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        public extern static IntPtr SendIntPtr(IntPtr receiver, IntPtr selector, IntPtr intPtr1);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        public extern static IntPtr SendIntPtr(IntPtr receiver, IntPtr selector, IntPtr intPtr1, IntPtr intPtr2);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        public extern static IntPtr SendIntPtr(IntPtr receiver, IntPtr selector, IntPtr intPtr1, int int1);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        public extern static IntPtr SendIntPtr(IntPtr receiver, IntPtr selector, uint uint1, IntPtr intPtr1, IntPtr intPtr2, bool bool1);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        public static extern NSPointF SendPointF(IntPtr receiver, IntPtr selector);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        public static extern NSPointD SendPointD(IntPtr receiver, IntPtr selector);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend_stret")]
        private static extern void SendRect(out NSRect retval, IntPtr receiver, IntPtr selector);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend_stret")]
        private static extern void SendRect(out NSRect retval, IntPtr receiver, IntPtr selector, NSRect rect1);

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

        static IntPtr clsNSURL = ObjCGetClass("NSURL");
        static IntPtr clsNSString = ObjCGetClass("NSString");
        static IntPtr clsNSArray = ObjCGetClass("NSArray");
        static IntPtr clsNSEvent;
        static IntPtr clsNSOpenPanel;
        static IntPtr clsNSSavePanel;
        static IntPtr clsNSAlert;
        static IntPtr clsNSCursor;
        static IntPtr clsNSPasteboard;
        static IntPtr clsNSData;

        static IntPtr selAlloc = SelRegisterName("alloc");
        static IntPtr selLength = SelRegisterName("length");
        static IntPtr selBytes = SelRegisterName("bytes");
        static IntPtr selClearContents = SelRegisterName("clearContents");
        static IntPtr selStringForType = SelRegisterName("stringForType:");
        static IntPtr selSetStringForType = SelRegisterName("setString:forType:");
        static IntPtr selGeneralPasteboard = SelRegisterName("generalPasteboard");
        static IntPtr selPasteboardWithName = SelRegisterName("pasteboardWithName:");
        static IntPtr selDeclareTypesOwner = SelRegisterName("declareTypes:owner:");
        static IntPtr selSetDataForType = SelRegisterName("setData:forType:");
        static IntPtr selDataForType = SelRegisterName("dataForType:");
        static IntPtr selDataWithBytesLength = SelRegisterName("dataWithBytes:length:");
        static IntPtr selInitWithCharactersLength = SelRegisterName("initWithCharacters:length:");
        static IntPtr selFileURLWithPath = SelRegisterName("fileURLWithPath:");
        static IntPtr selPath = SelRegisterName("path");
        static IntPtr selUTF8String = SelRegisterName("UTF8String");
        static IntPtr selArrayWithObjectsCount = SelRegisterName("arrayWithObjects:count:");
        static IntPtr selPressedMouseButtons = SelRegisterName("pressedMouseButtons");
        static IntPtr selBackingScaleFactor = SelRegisterName("backingScaleFactor");
        static IntPtr selContentView = SelRegisterName("contentView");
        static IntPtr selFrame = SelRegisterName("frame");
        static IntPtr selMouseLocationOutsideOfEventStream = SelRegisterName("mouseLocationOutsideOfEventStream");
        static IntPtr selMakeKeyAndOrderFront = SelRegisterName("makeKeyAndOrderFront:");
        static IntPtr selSetLevel = SelRegisterName("setLevel:");
        static IntPtr selStandardWindowButton = SelRegisterName("standardWindowButton:");
        static IntPtr selSetHidden = SelRegisterName("setHidden:");
        static IntPtr selOpenPanel = SelRegisterName("openPanel");
        static IntPtr selSavePanel = SelRegisterName("savePanel");
        static IntPtr selSetTitle = SelRegisterName("setTitle:");
        static IntPtr selSetDirectoryURL = SelRegisterName("setDirectoryURL:");
        static IntPtr selSetAllowedFileTypes = SelRegisterName("setAllowedFileTypes:");
        static IntPtr selRunModal = SelRegisterName("runModal");
        static IntPtr selURL = SelRegisterName("URL");
        static IntPtr selInit = SelRegisterName("init");
        static IntPtr selSetMessageText = SelRegisterName("setMessageText:");
        static IntPtr selSetInformativeText = SelRegisterName("setInformativeText:");
        static IntPtr selSetAlertStyle = SelRegisterName("setAlertStyle:");
        static IntPtr selAddButtonWithTitle = SelRegisterName("addButtonWithTitle:");
        static IntPtr selInvalidateCursorRectsForView = SelRegisterName("invalidateCursorRectsForView:");
        static IntPtr selBounds = SelRegisterName("bounds");
        static IntPtr selAddCursorRectCursor = SelRegisterName("addCursorRect:cursor:");

        static IntPtr generalPasteboard;
        static IntPtr famiStudioPasteboard;

        static float mainWindowScaling = 1.0f;
        static float dialogScaling = 1.0f;

        public static float MainWindowScaling => mainWindowScaling;
        public static float DialogScaling => dialogScaling;

        public static void Initialize(IntPtr nsWin)
        {
            mainNsWindow = nsWin;
            appKit = LoadLibrary("/System/Library/Frameworks/AppKit.framework/AppKit");

            clsNSEvent = ObjCGetClass("NSEvent");
            clsNSOpenPanel = ObjCGetClass("NSOpenPanel");
            clsNSSavePanel = ObjCGetClass("NSSavePanel");
            clsNSAlert = ObjCGetClass("NSAlert");
            clsNSCursor = ObjCGetClass("NSCursor");
            clsNSPasteboard = ObjCGetClass("NSPasteboard");
            clsNSData = ObjCGetClass("NSData");

            dialogScaling = (float)SendFloat(nsWin, selBackingScaleFactor);

            if (Settings.DpiScaling != 0)
                mainWindowScaling = Settings.DpiScaling / 100.0f;
            else
                mainWindowScaling = dialogScaling;

            generalPasteboard = SendIntPtr(clsNSPasteboard, selGeneralPasteboard);
            famiStudioPasteboard = SendIntPtr(clsNSPasteboard, selPasteboardWithName, ToNSString("FamiStudio"));
        }

        public static IntPtr ToNSString(string str)
        {
            if (str == null)
                return IntPtr.Zero;

            unsafe
            {
                fixed (char* ptrFirstChar = str)
                {
                    var handle = SendIntPtr(clsNSString, selAlloc);
                    handle = SendIntPtr(handle, selInitWithCharactersLength, (IntPtr)ptrFirstChar, str.Length);
                    return handle;
                }
            }
        }

        public static string FromNSString(IntPtr handle)
        {
            return Marshal.PtrToStringAuto(SendIntPtr(handle, selUTF8String));
        }

        public static IntPtr ToNSURL(string filepath)
        {
            return SendIntPtr(
                clsNSURL,
                selFileURLWithPath,
                ToNSString(filepath));
        }

        public static unsafe string FromNSURL(IntPtr url)
        {
            var str = SendIntPtr(url, selPath);
            var charPtr = SendIntPtr(str, selUTF8String);
            return Marshal.PtrToStringAnsi(charPtr);
        }

        static unsafe public IntPtr ToNSArray(params string[] items)
        {
            IntPtr buf = Marshal.AllocHGlobal((items.Length) * IntPtr.Size);
            for (int i = 0; i < items.Length; i++)
                Marshal.WriteIntPtr(buf, i * IntPtr.Size, ToNSString(items[i]));

            var array = SendIntPtr(
                clsNSArray,
                selArrayWithObjectsCount,
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

            public static implicit operator NSPoint(System.Drawing.PointF p)
            {
                return new NSPoint
                {
                    X = p.X,
                    Y = p.Y
                };
            }

            public static implicit operator System.Drawing.PointF(NSPoint s)
            {
                return new System.Drawing.PointF(s.X, s.Y);
            }
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

        [StructLayout(LayoutKind.Sequential)]
        internal struct NSSize
        {
            public NSFloat Width;
            public NSFloat Height;

            public static implicit operator NSSize(System.Drawing.SizeF s)
            {
                return new NSSize
                {
                    Width = s.Width,
                    Height = s.Height
                };
            }

            public static implicit operator System.Drawing.SizeF(NSSize s)
            {
                return new System.Drawing.SizeF(s.Width, s.Height);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct NSRect
        {
            public NSPoint Location;
            public NSSize Size;

            public NSFloat Width => Size.Width;
            public NSFloat Height => Size.Height;
            public NSFloat X => Location.X;
            public NSFloat Y => Location.Y;

            public static implicit operator NSRect(System.Drawing.RectangleF s)
            {
                return new NSRect
                {
                    Location = s.Location,
                    Size = s.Size
                };
            }

            public static implicit operator System.Drawing.RectangleF(NSRect s)
            {
                return new System.Drawing.RectangleF(s.Location, s.Size);
            }
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

        public static NSRect SendRect(IntPtr receiver, IntPtr selector)
        {
            NSRect r;
            SendRect(out r, receiver, selector);
            return r;
        }

        public static NSRect SendRect(IntPtr receiver, IntPtr selector, NSRect rect1)
        {
            NSRect r;
            SendRect(out r, receiver, selector, rect1);
            return r;
        }

        public static System.Windows.Forms.MouseButtons GetMouseButtons()
        {
            int macButtons = SendInt(clsNSEvent, selPressedMouseButtons);

            System.Windows.Forms.MouseButtons buttons = 0;
            if ((macButtons & 1) != 0) buttons |= System.Windows.Forms.MouseButtons.Left;
            if ((macButtons & 2) != 0) buttons |= System.Windows.Forms.MouseButtons.Right;
            if ((macButtons & 4) != 0) buttons |= System.Windows.Forms.MouseButtons.Middle;
            return buttons;
        }

        public static System.Drawing.Point GetWindowMousePosition(IntPtr nsWin)
        {
            var nsView = SendIntPtr(nsWin, selContentView);
            var viewRect = SendRect(nsView, selFrame);
            var winHeight = (float)viewRect.Size.Height * dialogScaling;

            var mouseLoc = SendPoint(nsWin, selMouseLocationOutsideOfEventStream);
            var x = (float)mouseLoc.X * dialogScaling;
            var y = (float)mouseLoc.Y * dialogScaling;
            y = winHeight - y;

            return new System.Drawing.Point((int)Math.Round(x), (int)Math.Round(y));
        }

        public static void SetWindowCursor(IntPtr nsWin, IntPtr target, IntPtr cursor)
        {
            var nsView = SendIntPtr(nsWin, selContentView);
            var rect = SendRect(nsView, selBounds);
            var nsCursor = SendIntPtr(clsNSCursor, cursor);
            SendVoid(target, selAddCursorRectCursor, rect, nsCursor);
        }

        public static void InvalidateCursor(IntPtr nsWin)
        {
            var nsView = SendIntPtr(nsWin, selContentView);
            SendVoid(nsWin, selInvalidateCursorRectsForView, nsView);
        }

        public static IntPtr LoadLibrary(string fileName)
        {
            const int RTLD_NOW = 2;
            return dlopen(fileName, RTLD_NOW);
        }

        public static void SetNSWindowAlwayOnTop(IntPtr nsWin)
        {
            SendIntPtr(nsWin, selMakeKeyAndOrderFront, IntPtr.Zero);
            SendIntPtr(nsWin, selSetLevel, 10);
        }

        public static void RemoveNSWindowAlwaysOnTop(IntPtr nsWin)
        {
            SendIntPtr(nsWin, selSetLevel, 0);
        }

        public static void SetNSWindowFocus(IntPtr nsWin)
        {
            SendIntPtr(nsWin, selMakeKeyAndOrderFront, nsWin);
        }

        public static void RestoreMainNSWindowFocus()
        {
            SetNSWindowFocus(mainNsWindow);
        }

        public static void RemoveMaximizeButton(IntPtr nsWin)
        {
            var btn = SendIntPtr(nsWin, selStandardWindowButton, NSWindowZoomButton);
            SendIntPtr(btn, selSetHidden, 1);
        }

        public static string ShowOpenDialog(string title, string[] extensions, string path = null)
        {
            var openPanel = SendIntPtr(clsNSOpenPanel, selOpenPanel);
            SendIntPtr(openPanel, selSetTitle, ToNSString(title));

            if (path != null)
            {
                var url = ToNSURL(path);
                SendIntPtr(openPanel, selSetDirectoryURL, url);
            }

            var fileTypesArray = ToNSArray(extensions);
            SendIntPtr(openPanel, selSetAllowedFileTypes, fileTypesArray);

            var status = SendInt(openPanel, selRunModal);

            SetNSWindowFocus(mainNsWindow);

            if (status == NSOKButton)
            {
                var url = SendIntPtr(openPanel, selURL);
                return FromNSURL(url);
            }

            return null;
        }

        public static string ShowSaveDialog(string title, string[] extensions, string path = null)
        {
            var savePanel = SendIntPtr(clsNSSavePanel, selSavePanel);
            SendIntPtr(savePanel, selSetTitle, ToNSString(title));

            if (path != null)
            {
                var url = ToNSURL(path);
                SendIntPtr(savePanel, selSetDirectoryURL, url);
            }

            var fileTypesArray = ToNSArray(extensions);
            SendIntPtr(savePanel, selSetAllowedFileTypes, fileTypesArray);

            var status = SendInt(savePanel, selRunModal);

            SetNSWindowFocus(mainNsWindow);

            if (status == NSOKButton)
            {
                var url = SendIntPtr(savePanel, selURL);
                return FromNSURL(url);
            }

            return null;
        }

        public static System.Windows.Forms.DialogResult ShowAlert(string text, string title, System.Windows.Forms.MessageBoxButtons buttons)
        {
            var alert = SendIntPtr(SendIntPtr(clsNSAlert, selAlloc), selInit);

            SendIntPtr(alert, selSetMessageText, ToNSString(title));
            SendIntPtr(alert, selSetInformativeText, ToNSString(text));
            SendIntPtr(alert, selSetAlertStyle, 2);

            if (buttons == System.Windows.Forms.MessageBoxButtons.YesNo ||
                buttons == System.Windows.Forms.MessageBoxButtons.YesNoCancel)
            {
                SendIntPtr(alert, selAddButtonWithTitle, ToNSString("Yes"));
                SendIntPtr(alert, selAddButtonWithTitle, ToNSString("No"));

                if (buttons == System.Windows.Forms.MessageBoxButtons.YesNoCancel)
                {
                    SendIntPtr(alert, selAddButtonWithTitle, ToNSString("Cancel"));
                }
            }

            var ret = SendInt(alert, selRunModal);

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

        public static unsafe void SetPasteboardData(byte[] data)
        {
            var pbTypes = ToNSArray(new[] { "FamiStudioData" });

            if (data == null || data.Length == 0)
            {
                var nsData = SendIntPtr(clsNSData, selDataWithBytesLength, IntPtr.Zero, 0);
                SendIntPtr(famiStudioPasteboard, selDeclareTypesOwner, pbTypes, IntPtr.Zero);
                SendVoid(famiStudioPasteboard, selSetDataForType, nsData, ToNSString("FamiStudioData"));
            }
            else
            {
                fixed (byte* ptr = &data[0])
                {
                    var nsData = SendIntPtr(clsNSData, selDataWithBytesLength, new IntPtr(ptr), data.Length);
                    SendIntPtr(famiStudioPasteboard, selDeclareTypesOwner, pbTypes, IntPtr.Zero);
                    SendVoid(famiStudioPasteboard, selSetDataForType, nsData, ToNSString("FamiStudioData"));
                }
            }
        }

        public static unsafe byte[] GetPasteboardData()
        {   
            var nsData = SendIntPtr(famiStudioPasteboard, selDataForType, ToNSString("FamiStudioData"));

            if (nsData == IntPtr.Zero)
                return null;

            var length = SendInt(nsData, selLength);
            if (length == 0)
                return null;

            var bytesPtr = SendIntPtr(nsData, selBytes);
            if (bytesPtr == IntPtr.Zero)
                return null;

            var bytes = new byte[length];
            fixed (byte* ptrDest = &bytes[0])
            {
                Marshal.Copy(bytesPtr, bytes, 0, length);
            }

            return bytes;
        }

        public static string GetPasteboardString()
        {
            var nsString = SendIntPtr(generalPasteboard, selStringForType, ToNSString("NSStringPboardType"));

            if (nsString == IntPtr.Zero)
                return null;

            return FromNSString(nsString);
        }

        public static void ClearPasteboardString()
        {
            SendVoid(generalPasteboard, selClearContents);
        }
    };
}
