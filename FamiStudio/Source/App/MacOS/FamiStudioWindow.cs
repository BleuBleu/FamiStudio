using System;
using System.Diagnostics;
using static GLFWDotNet.GLFW;

namespace FamiStudio
{
    public partial class FamiStudioWindow
    {
        private float magnificationAccum = 0;

        private IntPtr selType = MacUtils.SelRegisterName("type");
        private IntPtr selMagnification = MacUtils.SelRegisterName("magnification");
        private IntPtr selNextEventMatchingMask = MacUtils.SelRegisterName("nextEventMatchingMask:untilDate:inMode:dequeue:");
        private IntPtr nsLoopMode;

        private void PlatformWindowInitialize()
        {
            MacUtils.InitializeWindow(glfwGetCocoaWindow(window));
            MacUtils.FileOpen += MacUtils_FileOpen;

            nsLoopMode = MacUtils.GetStringConstant(MacUtils.FoundationLibrary, "NSDefaultRunLoopMode");
        }

        private void PlatformWindowShudown()
        {
        }

        private void MacUtils_FileOpen(string filename)
        {
            famistudio.OpenProject(filename);
        }

        private void ProcessPlatformEvents()
        {
            if (!Settings.TrackPadControls)
                return;

            while (true)
            {
                const int NSEventTypeMagnify = 30;

                var e = MacUtils.SendIntPtr(MacUtils.NSApplication, selNextEventMatchingMask, 1 << NSEventTypeMagnify, IntPtr.Zero, nsLoopMode, true);

                if (e == IntPtr.Zero)
                    return;

                var type = MacUtils.SendInt(e, selType);

                if (type == NSEventTypeMagnify)
                {
                    var magnification = (float)MacUtils.SendFloat(e, selMagnification);

                    if (Math.Sign(magnification) != Math.Sign(magnificationAccum))
                        magnificationAccum = 0;

                    magnificationAccum += magnification;
                    
                    float threshold = 1.0f / (float)Utils.Clamp(Settings.TrackPadZoomSensitity, 1, 16);

                    if (Math.Abs(magnificationAccum) > threshold)
                    {
                        var pt = GetClientCursorPosInternal();
                        var sz = GetWindowSizeInternal();

                        // We get notified for clicks in the title back and stuff.
                        if (pt.X < 0 || pt.Y < 0 || pt.X >= sz.Width || pt.Y >= sz.Height)
                            continue;

                        Debug.WriteLine($"PINCH ZOOM! {magnificationAccum} {pt.X} {pt.Y}");

                        var ctrl = container.GetControlAt(pt.X, pt.Y, out int x, out int y);

                        var origModifiers = modifiers.Value;
                        modifiers.Set(origModifiers | GLFW_MOD_CONTROL);
                        ctrl.SendMouseWheel(new PointerEventArgs(0, pt.X, pt.Y, false, magnificationAccum));
                        modifiers.Set(origModifiers);

                        magnificationAccum = 0;
                    }
                }
            }
        }
    }
}
