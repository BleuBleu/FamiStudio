using System;
using System.Collections.Generic;
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

        //[DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        //public extern static IntPtr SendIntPtr(IntPtr receiver, IntPtr selector, IntPtr intPtr1, NSPoint point1);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        public extern static IntPtr SendIntPtr(IntPtr receiver, IntPtr selector, uint uint1, IntPtr intPtr1);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        public extern static IntPtr SendIntPtr(IntPtr receiver, IntPtr selector, uint uint1, IntPtr intPtr1, IntPtr intPtr2, bool bool1);

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

        [DllImport("libgdk-quartz-2.0.0.dylib", EntryPoint = "gdk_quartz_window_get_nswindow")]
        public static extern IntPtr NSWindowFromGdkWindow(IntPtr window);

        [DllImport("libdl.dylib")]
        internal static extern IntPtr dlopen(String fileName, int flags);

        [DllImport("libdl.dylib")]
        internal static extern IntPtr dlsym(IntPtr handle, string symbol);

        [DllImport("/usr/lib/system/libsystem_c.dylib")]
        internal static extern int usleep(uint microseconds);

        // The follow code (most AppleEvent/CoreFoundattion interop) was taken from
        // MonoDevelop. https://github.com/mono/monodevelop
        internal delegate int EventDelegate(IntPtr callRef, IntPtr eventRef, IntPtr userData);

        [DllImport("/System/Library/Frameworks/Carbon.framework/Versions/Current/Carbon")]
        static extern IntPtr GetApplicationEventTarget();

        [DllImport("/System/Library/Frameworks/Carbon.framework/Versions/Current/Carbon")]
        static extern int InstallEventHandler(IntPtr target, EventDelegate handler, uint count, CarbonEventTypeSpec[] types, IntPtr user_data, out IntPtr handlerRef);

        [DllImport("/System/Library/Frameworks/Carbon.framework/Versions/Current/Carbon")]
        static extern int GetEventParameter(IntPtr eventRef, uint name, uint desiredType, uint zero, uint size, uint zero2, IntPtr dataBuffer);

        [DllImport("/System/Library/Frameworks/Carbon.framework/Versions/Current/Carbon")]
        static extern int AEGetNthPtr(ref AEDesc descList, int index, int desiredType, uint keyword, uint zero, IntPtr buffer, int bufferSize, int zero2);

        [DllImport("/System/Library/Frameworks/Carbon.framework/Versions/Current/Carbon")]
        static extern int AECountItems(ref AEDesc descList, out int count); //return an OSErr

        [DllImport("/System/Library/Frameworks/Carbon.framework/Versions/Current/Carbon")]
        static extern int AEDisposeDesc(ref AEDesc desc);

        [DllImport("/System/Library/Frameworks/Carbon.framework/Versions/Current/Carbon")]
        extern static IntPtr CFURLCreateFromFSRef(IntPtr allocator, ref FSRef fsref);

        [DllImport("/System/Library/Frameworks/CoreFoundation.framework/Versions/A/CoreFoundation")]
        extern static IntPtr CFURLCopyFileSystemPath(IntPtr urlRef, int pathStyle);

        [DllImport("/System/Library/Frameworks/CoreFoundation.framework/Versions/A/CoreFoundation")]
        extern static int CFStringGetLength(IntPtr handle);

        [DllImport("/System/Library/Frameworks/CoreFoundation.framework/Versions/A/CoreFoundation")]
        public static extern void CFRelease(IntPtr cfRef);

        [DllImport("/System/Library/Frameworks/CoreFoundation.framework/Versions/A/CoreFoundation", CharSet = CharSet.Unicode)]
        extern static IntPtr CFStringGetCharactersPtr(IntPtr handle);

        [DllImport("/System/Library/Frameworks/CoreFoundation.framework/Versions/A/CoreFoundation", CharSet = CharSet.Unicode)]
        extern static IntPtr CFStringGetCharacters(IntPtr handle, CFRange range, IntPtr buffer);

        [DllImport("/System/Library/Frameworks/CoreAudio.framework/CoreAudio", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        extern static unsafe int AudioObjectAddPropertyListener(uint inObjectID, AudioObjectPropertyAddress* inAddress, AudioObjectPropertyListenerProc inListener, IntPtr inClientData);

        unsafe delegate int AudioObjectPropertyListenerProc(uint inObjectID, uint inNumberAddresses, AudioObjectPropertyAddress* inAddresses, IntPtr inClientData);

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        struct AudioObjectPropertyAddress
        {
            public int element;
            public int scope;
            public int selector;
        }

        const int kAudioHardwarePropertyDefaultOutputDevice = 1682929012; // 'dOut'
        const int kAudioObjectPropertyScopeGlobal = 1735159650; // 'glob'
        const int kAudioObjectSystemObject = 1;

        const int EventParamDirectObject = 757935405; // '----'
        const int EventParamAEPosition = 1802530675; // 'kpos'
        const int EventParamTypeAEList = 1818850164; // 'list'
        const int EventParamTypeChar = 1413830740; // 'TEXT'
        const int EventParamTypeFSRef = 1718841958; // 'fsrf' 
        const int EventClassAppleEvent = 1634039412; // 'aevt'
        const int EventOpenDocuments = 1868853091; // 'odoc'
        const int EventHandled = 0;
        const int EventNotHandled = -9874;
        // End MonoDevelop code.

        public delegate void FileOpenDelegate(string filename);
        public static event FileOpenDelegate FileOpen;

        public delegate void AudioDeviceChangedDelegate();
        public static event AudioDeviceChangedDelegate AudioDeviceChanged;

        const int NSOKButton = 1;
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
        static IntPtr clsNSPasteboard;
        static IntPtr clsNSData;
        static IntPtr clsNSNotificationCenter;
        static IntPtr clsNSApplication;
        static IntPtr clsNSMenu;
        static IntPtr clsNSMenuItem;
        static IntPtr clsNSSound;

        static IntPtr selAlloc = SelRegisterName("alloc");
        static IntPtr selLength = SelRegisterName("length");
        static IntPtr selBytes = SelRegisterName("bytes");
        static IntPtr selCount = SelRegisterName("count");
        static IntPtr selGetObjectAtIndex = SelRegisterName("objectAtIndex:");
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
        static IntPtr selMakeKeyAndOrderFront = SelRegisterName("makeKeyAndOrderFront:");
        static IntPtr selOpenPanel = SelRegisterName("openPanel");
        static IntPtr selSavePanel = SelRegisterName("savePanel");
        static IntPtr selSetTitle = SelRegisterName("setTitle:");
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
        static IntPtr selDefaultCenter = SelRegisterName("defaultCenter");
        static IntPtr selAddObserver = SelRegisterName("addObserver:selector:name:object:");
        static IntPtr selSharedApplication = SelRegisterName("sharedApplication");
        static IntPtr selInitWithTitleActionKeyEquivalent = SelRegisterName("initWithTitle:action:keyEquivalent:");
        static IntPtr selDelegate = SelRegisterName("delegate");
        static IntPtr selSetTarget = SelRegisterName("setTarget:");
        static IntPtr selNew = SelRegisterName("new");
        static IntPtr selAddItem = SelRegisterName("addItem:");
        static IntPtr selSetSubmenu = SelRegisterName("setSubmenu:");
        static IntPtr selSetMainMenu = SelRegisterName("setMainMenu:");
        static IntPtr selDoubleClickInterval = SelRegisterName("doubleClickInterval");
        static IntPtr selBeep = SelRegisterName("beep");

        static IntPtr famiStudioPasteboard;
        static float  doubleClickInterval = 0.25f;
        static EventDelegate eventDelegate;
        static string lastOpenedDocument;
        static AudioObjectPropertyListenerProc audioDeviceChangeDelegate;

        public static IntPtr FoundationLibrary => foundationLib;
        public static IntPtr NSApplication => nsApplication;
        public static IntPtr NSWindow => nsWindow;
        public static float  DoubleClickInterval => doubleClickInterval;

        public static unsafe void Initialize()
        {
            appKitLib = LoadLibrary("/System/Library/Frameworks/AppKit.framework/AppKit");
            foundationLib = LoadLibrary("/System/Library/Frameworks/Foundation.framework/Foundation");

            clsNSEvent = GetClass("NSEvent");
            clsNSOpenPanel = GetClass("NSOpenPanel");
            clsNSSavePanel = GetClass("NSSavePanel");
            clsNSAlert = GetClass("NSAlert");
            clsNSCursor = GetClass("NSCursor");
            clsNSPasteboard = GetClass("NSPasteboard");
            clsNSData = GetClass("NSData");
            clsNSNotificationCenter = GetClass("NSNotificationCenter");
            clsNSApplication = GetClass("NSApplication");
            clsNSMenu = GetClass("NSMenu");
            clsNSMenuItem = GetClass("NSMenuItem");
            clsNSSound = GetClass("NSSound");

            CarbonEventTypeSpec eventType;
            eventType.EventClass = EventClassAppleEvent;
            eventType.EventKind = EventOpenDocuments;

            eventDelegate = new EventDelegate(HandleOpenDocuments);
            InstallEventHandler(GetApplicationEventTarget(), eventDelegate, 1, new CarbonEventTypeSpec[] { eventType }, IntPtr.Zero, out _);

            var outputDeviceAddress = new AudioObjectPropertyAddress()
            {
                element = kAudioHardwarePropertyDefaultOutputDevice,
                scope = kAudioObjectPropertyScopeGlobal,
                selector = 0
            };
            
            audioDeviceChangeDelegate = new AudioObjectPropertyListenerProc(AudioDeviceChangedCallback);
            AudioObjectAddPropertyListener(kAudioObjectSystemObject, &outputDeviceAddress, audioDeviceChangeDelegate, IntPtr.Zero);

            doubleClickInterval = (float)SendFloat(clsNSEvent, selDoubleClickInterval);
            famiStudioPasteboard = SendIntPtr(clsNSPasteboard, selPasteboardWithName, ToNSString("FamiStudio"));
        }

        public static void InitializeWindow(IntPtr nsWin)
        {
            nsWindow = nsWin;

            // Calling this before creating the window creates a ton of issues. It prevents
            // the app from restoring once minimized, and may also break our ability to debug 
            // correctly.
            nsApplication = SendIntPtr(clsNSApplication, selSharedApplication); 
        }
        
        [StructLayout(LayoutKind.Sequential, Pack = 2)]
        struct CarbonEventTypeSpec
        {
            public uint EventClass;
            public uint EventKind;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 2)]
        struct AEDesc
        {
            public uint descriptorType;
            public IntPtr dataHandle;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct SelectionRange
        {
            public short unused1; // 0 (not used)
            public short lineNum; // line to select (<0 to specify range)
            public int startRange; // start of selection range (if line < 0)
            public int endRange; // end of selection range (if line < 0)
            public int unused2; // 0 (not used)
            public int theDate; // modification date/time
        }

        struct CFRange
        {
            public IntPtr Location, Length;
            public CFRange(int l, int len)
            {
                Location = (IntPtr)l;
                Length = (IntPtr)len;
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 2, Size = 80)]
        struct FSRef
        {
            //this is an 80-char opaque byte array
            #pragma warning disable 0169
            private byte hidden;
            #pragma warning restore 0169
        }

        public static void CreateMenu()
        {
            var quitMenuItem = SendIntPtr(clsNSMenuItem, selAlloc);

            SendIntPtr(quitMenuItem, selInitWithTitleActionKeyEquivalent, ToNSString("Quit"), SelRegisterName("windowShouldClose:"), ToNSString(""));

            // GLFW uses a delegate.
            var del = SendIntPtr(nsWindow, selDelegate);
            SendVoid(quitMenuItem, selSetTarget, del);

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

        public static IntPtr GetCursorByName(string name)
        {
            var sel = MacUtils.SelRegisterName(name);
            return SendIntPtr(clsNSCursor, sel);
        }

        public static IntPtr LoadLibrary(string fileName)
        {
            const int RTLD_NOW = 2;
            return dlopen(fileName, RTLD_NOW);
        }

        public static void SetNSWindowFocus(IntPtr nsWin)
        {
            SendIntPtr(nsWin, selMakeKeyAndOrderFront, nsWin);
        }

        public static void RestoreMainNSWindowFocus()
        {
            SetNSWindowFocus(nsWindow);
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

        public static DialogResult ShowAlert(string text, string title, MessageBoxButtons buttons)
        {
            var alert = SendIntPtr(SendIntPtr(clsNSAlert, selAlloc), selInit);

            SendIntPtr(alert, selSetMessageText, ToNSString(title));
            SendIntPtr(alert, selSetInformativeText, ToNSString(text));
            SendIntPtr(alert, selSetAlertStyle, 2);

            if (buttons == MessageBoxButtons.YesNo ||
                buttons == MessageBoxButtons.YesNoCancel)
            {
                SendIntPtr(alert, selAddButtonWithTitle, ToNSString("Yes"));
                SendIntPtr(alert, selAddButtonWithTitle, ToNSString("No"));

                if (buttons == MessageBoxButtons.YesNoCancel)
                {
                    SendIntPtr(alert, selAddButtonWithTitle, ToNSString("Cancel"));
                }
            }

            var ret = SendInt(alert, selRunModal);

            SetNSWindowFocus(nsWindow);

            if (buttons == MessageBoxButtons.YesNo ||
                buttons == MessageBoxButtons.YesNoCancel)
            {
                if (ret == NSAlertFirstButtonReturn)
                    return DialogResult.Yes;
                if (ret == NSAlertSecondButtonReturn)
                    return DialogResult.No;

                return DialogResult.Cancel;
            }
            else
            {
                return DialogResult.OK;
            }
        }

        public static void Beep()
        {
            NSBeep();
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

        // The follow code (most AppleEvent/CoreFoundattion interop) was taken from
        // MonoDevelop. https://github.com/mono/monodevelop
        static T GetEventParameter<T>(IntPtr eventRef, uint name, uint desiredType) where T : struct
        {
            int len = Marshal.SizeOf(typeof(T));
            IntPtr bufferPtr = Marshal.AllocHGlobal(len);
            GetEventParameter(eventRef, name, desiredType, 0, (uint)len, 0, bufferPtr);
            T val = (T)Marshal.PtrToStructure(bufferPtr, typeof(T));
            Marshal.FreeHGlobal(bufferPtr);
            return val;
        }

        static T AEGetNthPtr<T>(ref AEDesc descList, int index, int desiredType) where T : struct
        {
            int len = Marshal.SizeOf(typeof(T));
            IntPtr bufferPtr = Marshal.AllocHGlobal(len);
            try
            {
                AEGetNthPtr(ref descList, index, desiredType, 0, 0, bufferPtr, len, 0);
                T val = (T)Marshal.PtrToStructure(bufferPtr, typeof(T));
                return val;
            }
            finally
            {
                Marshal.FreeHGlobal(bufferPtr);
            }
        }

        public static string FetchString(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
                return null;

            string str;

            int l = CFStringGetLength(handle);
            IntPtr u = CFStringGetCharactersPtr(handle);
            IntPtr buffer = IntPtr.Zero;
            if (u == IntPtr.Zero)
            {
                CFRange r = new CFRange(0, l);
                buffer = Marshal.AllocCoTaskMem(l * 2);
                CFStringGetCharacters(handle, r, buffer);
                u = buffer;
            }
            unsafe
            {
                str = new string((char*)u, 0, l);
            }

            if (buffer != IntPtr.Zero)
                Marshal.FreeCoTaskMem(buffer);

            return str;
        }

        static string FSRefToString(ref FSRef fsref)
        {
            IntPtr url = IntPtr.Zero;
            IntPtr str = IntPtr.Zero;
            try
            {
                url = CFURLCreateFromFSRef(IntPtr.Zero, ref fsref);
                if (url == IntPtr.Zero)
                    return null;
                str = CFURLCopyFileSystemPath(url, 0);
                if (str == IntPtr.Zero)
                    return null;
                return FetchString(str);
            }
            finally
            {
                if (url != IntPtr.Zero)
                    CFRelease(url);
                if (str != IntPtr.Zero)
                    CFRelease(str);
            }
        }

        delegate T AEDescValueSelector<TRef, T>(ref TRef desc);

        static T[] GetListFromAEDesc<T, TRef>(ref AEDesc list, AEDescValueSelector<TRef, T> sel, int type) where TRef : struct
        {
            AECountItems(ref list, out var count);
            T[] arr = new T[count];
            for (int i = 1; i <= count; i++)
            {
                TRef r = AEGetNthPtr<TRef>(ref list, i, type);
                arr[i - 1] = sel(ref r);
            }
            return arr;
        }

        static Dictionary<string, int> GetFileListFromEventRef(IntPtr eventRef)
        {
            AEDesc list = GetEventParameter<AEDesc>(eventRef, EventParamDirectObject, EventParamTypeAEList);

            try
            {
                int line;
                try
                {
                    SelectionRange range = GetEventParameter<SelectionRange>(eventRef, EventParamAEPosition, EventParamTypeChar);
                    line = range.lineNum + 1;
                }
                catch (Exception)
                {
                    line = 0;
                }

                var arr = GetListFromAEDesc<string, FSRef>(ref list, FSRefToString, EventParamTypeFSRef);
                var files = new Dictionary<string, int>();
                foreach (var s in arr)
                {
                    if (!string.IsNullOrEmpty(s))
                        files[s] = line;
                }
                return files;
            }
            finally
            {
                AEDisposeDesc(ref list);
            }

            return null;
        }

        public static string GetInitialOpenDocument()
        {
            return lastOpenedDocument;
        }

        static int HandleOpenDocuments(IntPtr callRef, IntPtr eventRef, IntPtr user_data)
        {
            try
            {
                var docs = GetFileListFromEventRef(eventRef);

                if (docs != null)
                {
                    foreach (var kv in docs)
                    {
                        lastOpenedDocument = kv.Key;
                        FileOpen?.Invoke(kv.Key);
                        break;
                    }
                }

                return EventHandled;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex);
            }

            return EventNotHandled;
        }
        // End MonoDevelop code.

        static unsafe int AudioDeviceChangedCallback(uint inObjectID, uint inNumberAddresses, AudioObjectPropertyAddress* inAddresses, IntPtr inClientData)
        {
            AudioDeviceChanged?.Invoke();
            return 0;
        }
    };
}
