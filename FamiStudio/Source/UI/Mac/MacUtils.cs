using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace FamiStudio
{
    static class MacUtils
    {
        static IntPtr appKitLib;
        static IntPtr foundationLib;

        static IntPtr nsWindow;
        static IntPtr nsApplication;

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "sel_registerName")]
        public extern static IntPtr SelRegisterName(string name);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_getClass")]
        public extern static IntPtr GetClass(string name);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        public static extern bool SendBool(IntPtr receiver, IntPtr selector);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        public extern static void SendVoid(IntPtr receiver, IntPtr selector);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        public extern static void SendVoid(IntPtr receiver, IntPtr selector, IntPtr intPtr1);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        public extern static void SendVoid(IntPtr receiver, IntPtr selector, IntPtr intPtr1, IntPtr intPtr2);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        public extern static void SendVoid(IntPtr receiver, IntPtr selector, IntPtr intPtr1, IntPtr intPtr2, IntPtr intPtr3, IntPtr intPtr4);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        public extern static void SendVoid(IntPtr receiver, IntPtr selector, NSRect rect1, IntPtr intPtr1);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        public extern static int SendInt(IntPtr receiver, IntPtr selector);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        public extern static double SendFloat(IntPtr receiver, IntPtr selector);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        public extern static IntPtr SendIntPtr(IntPtr receiver, IntPtr selector);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        public extern static IntPtr SendIntPtr(IntPtr receiver, IntPtr selector, bool bool1);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        public extern static IntPtr SendIntPtr(IntPtr receiver, IntPtr selector, int int1);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        public extern static IntPtr SendIntPtr(IntPtr receiver, IntPtr selector, IntPtr intPtr1);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        public extern static IntPtr SendIntPtr(IntPtr receiver, IntPtr selector, IntPtr intPtr1, IntPtr intPtr2);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        public extern static IntPtr SendIntPtr(IntPtr receiver, IntPtr selector, IntPtr intPtr1, IntPtr intPtr2, IntPtr intPtr3);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        public extern static IntPtr SendIntPtr(IntPtr receiver, IntPtr selector, IntPtr intPtr1, int int1);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        public extern static IntPtr SendIntPtr(IntPtr receiver, IntPtr selector, IntPtr intPtr1, NSPoint point1);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        public extern static IntPtr SendIntPtr(IntPtr receiver, IntPtr selector, uint uint1, IntPtr intPtr1);

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

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "class_addMethod")]
        private static extern bool ClassAddMethod(IntPtr classHandle, IntPtr selector, IntPtr method, string types);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_lookUpClass")]
        public static extern IntPtr ClassLookup(string name);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_allocateClassPair")]     
        public static extern IntPtr AllocateClass(IntPtr parentClass, string name, int extraBytes);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_registerClassPair")]
        public static extern void RegisterClass(IntPtr classToRegister);

        [DllImport("/System/Library/Frameworks/AppKit.framework/AppKit")]
        public static extern void NSBeep();

        [DllImport("/System/Library/Frameworks/ApplicationServices.framework/Frameworks/CoreText.framework/CoreText")]
        public static extern bool CTFontManagerRegisterFontsForURL(IntPtr fontUrl, int scope, IntPtr error);

        [DllImport("libgdk-quartz-2.0.0.dylib", EntryPoint = "gdk_quartz_window_get_nswindow")]
        public static extern IntPtr NSWindowFromGdkWindow(IntPtr window);

        [DllImport("libdl.dylib")]
        internal static extern IntPtr dlopen(String fileName, int flags);

        [DllImport("libdl.dylib")]
        internal static extern IntPtr dlsym(IntPtr handle, string symbol);

        [DllImport("/usr/lib/system/libsystem_c.dylib")]
        internal static extern int usleep(uint microseconds);

        const int NSOKButton = 1;
        const int NSWindowZoomButton = 2;
        const int NSAlertFirstButtonReturn  = 1000;
        const int NSAlertSecondButtonReturn = 1001;

        static IntPtr clsNSURL = GetClass("NSURL");
        static IntPtr clsNSString = GetClass("NSString");
        static IntPtr clsNSArray = GetClass("NSArray");
        static IntPtr clsNSEvent;
        static IntPtr clsNSOpenPanel;
        static IntPtr clsNSSavePanel;
        static IntPtr clsNSAlert;
        static IntPtr clsNSCursor;
        static IntPtr clsNSImage;
        static IntPtr clsNSPasteboard;
        static IntPtr clsNSData;
        static IntPtr clsNSNotificationCenter;
        static IntPtr clsNSApplication;
        static IntPtr clsNSMenu;
        static IntPtr clsNSMenuItem;

        static IntPtr selAlloc = SelRegisterName("alloc");
        static IntPtr selLength = SelRegisterName("length");
        static IntPtr selBytes = SelRegisterName("bytes");
        static IntPtr selCount = SelRegisterName("count");
        static IntPtr selGetObjectAtIndex = SelRegisterName("objectAtIndex:");
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
        static IntPtr selInitWithImageHotSpot = SelRegisterName("initWithImage:hotSpot:");
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
        static IntPtr selInitWithData = SelRegisterName("initWithData:");
        static IntPtr selSetDirectoryURL = SelRegisterName("setDirectoryURL:");
        static IntPtr selAllowsMultipleSelection = SelRegisterName("setAllowsMultipleSelection:");
        static IntPtr selSetAllowedFileTypes = SelRegisterName("setAllowedFileTypes:");
        static IntPtr selSetCanChooseDirectories = SelRegisterName("setCanChooseDirectories:");
        static IntPtr selSetCanCreateirectories = SelRegisterName("setCanCreateDirectories:");
        static IntPtr selRunModal = SelRegisterName("runModal");
        static IntPtr selURL = SelRegisterName("URL");
        static IntPtr selURLs = SelRegisterName("URLs");
        static IntPtr selInit = SelRegisterName("init");
        static IntPtr selClassName = SelRegisterName("className");
        static IntPtr selSetMessageText = SelRegisterName("setMessageText:");
        static IntPtr selSetInformativeText = SelRegisterName("setInformativeText:");
        static IntPtr selSetAlertStyle = SelRegisterName("setAlertStyle:");
        static IntPtr selAddButtonWithTitle = SelRegisterName("addButtonWithTitle:");
        static IntPtr selInvalidateCursorRectsForView = SelRegisterName("invalidateCursorRectsForView:");
        static IntPtr selBounds = SelRegisterName("bounds");
        static IntPtr selRelease = SelRegisterName("release");
        static IntPtr selDefaultCenter = SelRegisterName("defaultCenter");
        static IntPtr selAddObserver = SelRegisterName("addObserver:selector:name:object:");
        static IntPtr selAddCursorRectCursor = SelRegisterName("addCursorRect:cursor:");
        static IntPtr selSharedApplication = SelRegisterName("sharedApplication");
        static IntPtr selInitWithTitleActionKeyEquivalent = SelRegisterName("initWithTitle:action:keyEquivalent:");
        static IntPtr selInitWithTitle = SelRegisterName("initWithTitle:");
        static IntPtr selSetTarget = SelRegisterName("setTarget:");
        static IntPtr selNew = SelRegisterName("new");
        static IntPtr selAddItem = SelRegisterName("addItem:");
        static IntPtr selSetSubmenu = SelRegisterName("setSubmenu:");
        static IntPtr selSetMainMenu = SelRegisterName("setMainMenu:");

        static IntPtr generalPasteboard;
        static IntPtr famiStudioPasteboard;

        static float mainWindowScaling = 1.0f;
        static float dialogScaling = 1.0f;

        public static IntPtr FoundationLibrary => foundationLib;
        public static IntPtr NSApplication => nsApplication;
        public static IntPtr NSWindow => nsWindow;

        public static float MainWindowScaling => mainWindowScaling;
        public static float DialogScaling => dialogScaling;

        public static void Initialize(IntPtr nsWin)
        {
            nsWindow = nsWin;
            appKitLib = LoadLibrary("/System/Library/Frameworks/AppKit.framework/AppKit");
            foundationLib = LoadLibrary("/System/Library/Frameworks/Foundation.framework/Foundation");

            clsNSEvent = GetClass("NSEvent");
            clsNSOpenPanel = GetClass("NSOpenPanel");
            clsNSSavePanel = GetClass("NSSavePanel");
            clsNSAlert = GetClass("NSAlert");
            clsNSCursor = GetClass("NSCursor");
            clsNSImage = GetClass("NSImage");
            clsNSPasteboard = GetClass("NSPasteboard");
            clsNSData = GetClass("NSData");
            clsNSNotificationCenter = GetClass("NSNotificationCenter");
            clsNSApplication = GetClass("NSApplication");
            clsNSMenu = GetClass("NSMenu");
            clsNSMenuItem = GetClass("NSMenuItem");

            dialogScaling = (float)SendFloat(nsWin, selBackingScaleFactor);

            if (Settings.DpiScaling != 0)
                mainWindowScaling = Settings.DpiScaling / 100.0f;
            else
                mainWindowScaling = dialogScaling;

            generalPasteboard = SendIntPtr(clsNSPasteboard, selGeneralPasteboard);
            famiStudioPasteboard = SendIntPtr(clsNSPasteboard, selPasteboardWithName, ToNSString("FamiStudio"));
            nsApplication = SendIntPtr(clsNSApplication, selSharedApplication);

            CreateMenu();
        }

        public static void CreateMenu()
        {
            var quitMenuItem = SendIntPtr(clsNSMenuItem, selAlloc);

            SendIntPtr(quitMenuItem, selInitWithTitleActionKeyEquivalent, ToNSString("Quit"), SelRegisterName("windowShouldClose:"), ToNSString(""));
            SendVoid(quitMenuItem, selSetTarget, nsWindow);

            var appMenu = SendIntPtr(clsNSMenu, selNew);
            SendIntPtr(appMenu, selAddItem, quitMenuItem);

            var appMenuItem = SendIntPtr(clsNSMenuItem, selNew);
            SendIntPtr(appMenuItem, selSetSubmenu, appMenu);

            var mainMenu = SendIntPtr(clsNSMenu, selNew);
            SendIntPtr(mainMenu, selAddItem, appMenuItem);

            SendVoid(nsApplication, selSetMainMenu, mainMenu);
        }

        public static void AddNotificationCenterObserver(IntPtr observer, string selector, string notificationName, IntPtr obj)
        {
            var notificationCenter = SendIntPtr(clsNSNotificationCenter, selDefaultCenter);
            SendVoid(notificationCenter, selAddObserver, observer, SelRegisterName(selector), ToNSString(notificationName), obj);
        }

        public static IntPtr GetStringConstant(IntPtr handle, string symbol)
        {
            var indirect = dlsym(handle, symbol);
            if (indirect == IntPtr.Zero)
            {
                return IntPtr.Zero;
            }

            var actual = Marshal.ReadIntPtr(indirect);
            if (actual == IntPtr.Zero)
            {
                return IntPtr.Zero;
            }

            return actual;
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

        public static string GetClassName(IntPtr obj)
        {
            return FromNSString(SendIntPtr(obj, selClassName));
        }

        public static void RegisterMethod(IntPtr handle, Delegate d, string selector, string typeString)
        {
            // TypeString info:
            // https://developer.apple.com/library/mac/documentation/Cocoa/Conceptual/ObjCRuntimeGuide/Articles/ocrtTypeEncodings.html

            var p = Marshal.GetFunctionPointerForDelegate(d);
            var r = ClassAddMethod(handle, SelRegisterName(selector), p, typeString);

            if (!r)
            {
                throw new ArgumentException("Could not register method " + d + " in class " + GetClassName(handle));
            }
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

        static unsafe public string[] FromNSArray(IntPtr nsArray)
        {
            var count = SendInt(nsArray, selCount);

            if (count == 0)
            {
                return new string[0];
            }

            var items = new string[count];

            for (int i = 0; i < count; i++)
            {
                var obj = SendIntPtr(nsArray, selGetObjectAtIndex, i);
                items[i] = FromNSURL(obj);
            }

            return items;
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

        public static System.Drawing.PointF GetWindowSize(IntPtr nsWin)
        {
            var nsView = SendIntPtr(nsWin, selContentView);
            var viewRect = SendRect(nsView, selFrame);

            return new System.Drawing.PointF(viewRect.Width, viewRect.Height);
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

        public static IntPtr GetCursorByName(string name)
        {
            var sel = MacUtils.SelRegisterName(name);
            return SendIntPtr(clsNSCursor, sel);
        }

        public static void SetWindowCursor(IntPtr nsWin, IntPtr target, IntPtr cursor)
        {
            var nsView = SendIntPtr(nsWin, selContentView);
            var rect = SendRect(nsView, selBounds);
            SendVoid(target, selAddCursorRectCursor, rect, cursor);
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
            SetNSWindowFocus(nsWindow);
        }

        public static void RemoveMaximizeButton(IntPtr nsWin)
        {
            var btn = SendIntPtr(nsWin, selStandardWindowButton, NSWindowZoomButton);
            SendIntPtr(btn, selSetHidden, 1);
        }

        public unsafe static IntPtr CreateCursorFromImage(byte[] imageData, int hotX, int hotY)
        {
            fixed (byte* ptr = &imageData[0])
            {
                var nsData = SendIntPtr(clsNSData, selDataWithBytesLength, new IntPtr(ptr), imageData.Length);

                var nsImage = SendIntPtr(clsNSImage, selAlloc);
                nsImage = SendIntPtr(nsImage, selInitWithData, nsData);

                var nsPoint = new NSPoint();
                nsPoint.X = hotX;
                nsPoint.Y = hotY;

				var nsCursor = SendIntPtr(clsNSCursor, selAlloc);
                nsCursor = SendIntPtr(nsCursor, selInitWithImageHotSpot, nsImage, nsPoint);

                SendVoid(nsData,  selRelease);
                SendVoid(nsImage, selRelease);
                
                return nsCursor;
            }
        }

        public static string[] ShowOpenDialog(string title, string[] extensions, bool multiselect = false, string path = null)
        {
            var openPanel = SendIntPtr(clsNSOpenPanel, selOpenPanel);
            SendIntPtr(openPanel, selSetTitle, ToNSString(title));

            if (path != null)
            {
                var url = ToNSURL(path);
                SendIntPtr(openPanel, selSetDirectoryURL, url);
            }

            if (extensions != null && extensions.Length > 0)
            {
                var fileTypesArray = ToNSArray(extensions);
                SendIntPtr(openPanel, selSetAllowedFileTypes, fileTypesArray);
            }

            if (multiselect)
            {
                SendIntPtr(openPanel, selAllowsMultipleSelection, true);
            }

            var status = SendInt(openPanel, selRunModal);

            SetNSWindowFocus(nsWindow);

            if (status == NSOKButton)
            {
                if (multiselect)
                {
                    var urls = SendIntPtr(openPanel, selURLs);
                    return FromNSArray(urls);
                }
                else
                {
                    var url = SendIntPtr(openPanel, selURL);
                    return new[] { FromNSURL(url) };
                }
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

            SetNSWindowFocus(nsWindow);

            if (status == NSOKButton)
            {
                var url = SendIntPtr(savePanel, selURL);
                return FromNSURL(url);
            }

            return null;
        }

        public static string ShowBrowseFolderDialog(string title, ref string path)
        {
            var openPanel = SendIntPtr(clsNSOpenPanel, selOpenPanel);
            SendIntPtr(openPanel, selSetTitle, ToNSString(title));

            if (path != null)
            {
                var url = ToNSURL(path);
                SendIntPtr(openPanel, selSetDirectoryURL, url);
            }

            //var fileTypesArray = ToNSArray(extensions);
            //SendIntPtr(openPanel, selSetAllowedFileTypes, fileTypesArray);
            SendIntPtr(openPanel, selSetCanChooseDirectories, 1);
            SendIntPtr(openPanel, selSetCanCreateirectories, 1);

            var status = SendInt(openPanel, selRunModal);

            SetNSWindowFocus(nsWindow);

            if (status == NSOKButton)
            {
                var url = SendIntPtr(openPanel, selURL);
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

            SetNSWindowFocus(nsWindow);

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
