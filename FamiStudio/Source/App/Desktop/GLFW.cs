// MIT License
// Copyright (c) 2016 - 2019 Zachary Snow
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

//#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;

namespace GLFWDotNet
{
	// FamiStudio: We still use poor old .NET Framework. Emulate RuntimeInformation
	// with the information we have.
	public enum OSPlatform
    {
		Windows,
		Linux,
		OSX
	};

	public static class RuntimeInformation
    {
		public static bool IsOSPlatform(OSPlatform platform)
        {
			switch (platform)
            {
				case OSPlatform.Windows: return FamiStudio.Platform.IsWindows;
				case OSPlatform.Linux:   return FamiStudio.Platform.IsLinux;
				case OSPlatform.OSX:     return FamiStudio.Platform.IsMacOS;
			}

			return false;
		}
	}

	public static partial class GLFW
	{
		public const int GLFW_VERSION_MAJOR = 3;
		public const int GLFW_VERSION_MINOR = 4;
		public const int GLFW_VERSION_REVISION = 0;
		public const int GLFW_TRUE = 1;
		public const int GLFW_FALSE = 0;
		public const int GLFW_RELEASE = 0;
		public const int GLFW_PRESS = 1;
		public const int GLFW_REPEAT = 2;
		public const int GLFW_HAT_CENTERED = 0;
		public const int GLFW_HAT_UP = 1;
		public const int GLFW_HAT_RIGHT = 2;
		public const int GLFW_HAT_DOWN = 4;
		public const int GLFW_HAT_LEFT = 8;
		public const int GLFW_KEY_UNKNOWN = -1;
		public const int GLFW_KEY_SPACE = 32;
		public const int GLFW_KEY_0 = 48;
		public const int GLFW_KEY_1 = 49;
		public const int GLFW_KEY_2 = 50;
		public const int GLFW_KEY_3 = 51;
		public const int GLFW_KEY_4 = 52;
		public const int GLFW_KEY_5 = 53;
		public const int GLFW_KEY_6 = 54;
		public const int GLFW_KEY_7 = 55;
		public const int GLFW_KEY_8 = 56;
		public const int GLFW_KEY_9 = 57;
		public const int GLFW_KEY_A = 65;
		public const int GLFW_KEY_B = 66;
		public const int GLFW_KEY_C = 67;
		public const int GLFW_KEY_D = 68;
		public const int GLFW_KEY_E = 69;
		public const int GLFW_KEY_F = 70;
		public const int GLFW_KEY_G = 71;
		public const int GLFW_KEY_H = 72;
		public const int GLFW_KEY_I = 73;
		public const int GLFW_KEY_J = 74;
		public const int GLFW_KEY_K = 75;
		public const int GLFW_KEY_L = 76;
		public const int GLFW_KEY_M = 77;
		public const int GLFW_KEY_N = 78;
		public const int GLFW_KEY_O = 79;
		public const int GLFW_KEY_P = 80;
		public const int GLFW_KEY_Q = 81;
		public const int GLFW_KEY_R = 82;
		public const int GLFW_KEY_S = 83;
		public const int GLFW_KEY_T = 84;
		public const int GLFW_KEY_U = 85;
		public const int GLFW_KEY_V = 86;
		public const int GLFW_KEY_W = 87;
		public const int GLFW_KEY_X = 88;
		public const int GLFW_KEY_Y = 89;
		public const int GLFW_KEY_Z = 90;
		public const int GLFW_KEY_ESCAPE = 256;
		public const int GLFW_KEY_ENTER = 257;
		public const int GLFW_KEY_TAB = 258;
		public const int GLFW_KEY_BACKSPACE = 259;
		public const int GLFW_KEY_INSERT = 260;
		public const int GLFW_KEY_DELETE = 261;
		public const int GLFW_KEY_RIGHT = 262;
		public const int GLFW_KEY_LEFT = 263;
		public const int GLFW_KEY_DOWN = 264;
		public const int GLFW_KEY_UP = 265;
		public const int GLFW_KEY_PAGE_UP = 266;
		public const int GLFW_KEY_PAGE_DOWN = 267;
		public const int GLFW_KEY_HOME = 268;
		public const int GLFW_KEY_END = 269;
		public const int GLFW_KEY_CAPS_LOCK = 280;
		public const int GLFW_KEY_SCROLL_LOCK = 281;
		public const int GLFW_KEY_NUM_LOCK = 282;
		public const int GLFW_KEY_PRINT_SCREEN = 283;
		public const int GLFW_KEY_PAUSE = 284;
		public const int GLFW_KEY_F1 = 290;
		public const int GLFW_KEY_F2 = 291;
		public const int GLFW_KEY_F3 = 292;
		public const int GLFW_KEY_F4 = 293;
		public const int GLFW_KEY_F5 = 294;
		public const int GLFW_KEY_F6 = 295;
		public const int GLFW_KEY_F7 = 296;
		public const int GLFW_KEY_F8 = 297;
		public const int GLFW_KEY_F9 = 298;
		public const int GLFW_KEY_F10 = 299;
		public const int GLFW_KEY_F11 = 300;
		public const int GLFW_KEY_F12 = 301;
		public const int GLFW_KEY_F13 = 302;
		public const int GLFW_KEY_F14 = 303;
		public const int GLFW_KEY_F15 = 304;
		public const int GLFW_KEY_F16 = 305;
		public const int GLFW_KEY_F17 = 306;
		public const int GLFW_KEY_F18 = 307;
		public const int GLFW_KEY_F19 = 308;
		public const int GLFW_KEY_F20 = 309;
		public const int GLFW_KEY_F21 = 310;
		public const int GLFW_KEY_F22 = 311;
		public const int GLFW_KEY_F23 = 312;
		public const int GLFW_KEY_F24 = 313;
		public const int GLFW_KEY_F25 = 314;
		public const int GLFW_KEY_KP_0 = 320;
		public const int GLFW_KEY_KP_1 = 321;
		public const int GLFW_KEY_KP_2 = 322;
		public const int GLFW_KEY_KP_3 = 323;
		public const int GLFW_KEY_KP_4 = 324;
		public const int GLFW_KEY_KP_5 = 325;
		public const int GLFW_KEY_KP_6 = 326;
		public const int GLFW_KEY_KP_7 = 327;
		public const int GLFW_KEY_KP_8 = 328;
		public const int GLFW_KEY_KP_9 = 329;
		public const int GLFW_KEY_KP_DECIMAL = 330;
		public const int GLFW_KEY_KP_DIVIDE = 331;
		public const int GLFW_KEY_KP_MULTIPLY = 332;
		public const int GLFW_KEY_KP_SUBTRACT = 333;
		public const int GLFW_KEY_KP_ADD = 334;
		public const int GLFW_KEY_KP_ENTER = 335;
		public const int GLFW_KEY_KP_EQUAL = 336;
		public const int GLFW_KEY_LEFT_SHIFT = 340;
		public const int GLFW_KEY_LEFT_CONTROL = 341;
		public const int GLFW_KEY_LEFT_ALT = 342;
		public const int GLFW_KEY_LEFT_SUPER = 343;
		public const int GLFW_KEY_RIGHT_SHIFT = 344;
		public const int GLFW_KEY_RIGHT_CONTROL = 345;
		public const int GLFW_KEY_RIGHT_ALT = 346;
		public const int GLFW_KEY_RIGHT_SUPER = 347;
		public const int GLFW_KEY_MENU = 348;
		public const int GLFW_KEY_LAST = GLFW_KEY_MENU;
		public const int GLFW_MOD_SHIFT = 0x0001;
		public const int GLFW_MOD_CONTROL = 0x0002;
		public const int GLFW_MOD_ALT = 0x0004;
		public const int GLFW_MOD_SUPER = 0x0008;
		public const int GLFW_MOD_CAPS_LOCK = 0x0010;
		public const int GLFW_MOD_NUM_LOCK = 0x0020;
		public const int GLFW_MOUSE_BUTTON_1 = 0;
		public const int GLFW_MOUSE_BUTTON_2 = 1;
		public const int GLFW_MOUSE_BUTTON_3 = 2;
		public const int GLFW_MOUSE_BUTTON_4 = 3;
		public const int GLFW_MOUSE_BUTTON_5 = 4;
		public const int GLFW_MOUSE_BUTTON_6 = 5;
		public const int GLFW_MOUSE_BUTTON_7 = 6;
		public const int GLFW_MOUSE_BUTTON_8 = 7;
		public const int GLFW_MOUSE_BUTTON_LAST = GLFW_MOUSE_BUTTON_8;
		public const int GLFW_MOUSE_BUTTON_LEFT = GLFW_MOUSE_BUTTON_1;
		public const int GLFW_MOUSE_BUTTON_RIGHT = GLFW_MOUSE_BUTTON_2;
		public const int GLFW_MOUSE_BUTTON_MIDDLE = GLFW_MOUSE_BUTTON_3;
		public const int GLFW_JOYSTICK_1 = 0;
		public const int GLFW_JOYSTICK_2 = 1;
		public const int GLFW_JOYSTICK_3 = 2;
		public const int GLFW_JOYSTICK_4 = 3;
		public const int GLFW_JOYSTICK_5 = 4;
		public const int GLFW_JOYSTICK_6 = 5;
		public const int GLFW_JOYSTICK_7 = 6;
		public const int GLFW_JOYSTICK_8 = 7;
		public const int GLFW_JOYSTICK_9 = 8;
		public const int GLFW_JOYSTICK_10 = 9;
		public const int GLFW_JOYSTICK_11 = 10;
		public const int GLFW_JOYSTICK_12 = 11;
		public const int GLFW_JOYSTICK_13 = 12;
		public const int GLFW_JOYSTICK_14 = 13;
		public const int GLFW_JOYSTICK_15 = 14;
		public const int GLFW_JOYSTICK_16 = 15;
		public const int GLFW_JOYSTICK_LAST = GLFW_JOYSTICK_16;
		public const int GLFW_GAMEPAD_BUTTON_A = 0;
		public const int GLFW_GAMEPAD_BUTTON_B = 1;
		public const int GLFW_GAMEPAD_BUTTON_X = 2;
		public const int GLFW_GAMEPAD_BUTTON_Y = 3;
		public const int GLFW_GAMEPAD_BUTTON_LEFT_BUMPER = 4;
		public const int GLFW_GAMEPAD_BUTTON_RIGHT_BUMPER = 5;
		public const int GLFW_GAMEPAD_BUTTON_BACK = 6;
		public const int GLFW_GAMEPAD_BUTTON_START = 7;
		public const int GLFW_GAMEPAD_BUTTON_GUIDE = 8;
		public const int GLFW_GAMEPAD_BUTTON_LEFT_THUMB = 9;
		public const int GLFW_GAMEPAD_BUTTON_RIGHT_THUMB = 10;
		public const int GLFW_GAMEPAD_BUTTON_DPAD_UP = 11;
		public const int GLFW_GAMEPAD_BUTTON_DPAD_RIGHT = 12;
		public const int GLFW_GAMEPAD_BUTTON_DPAD_DOWN = 13;
		public const int GLFW_GAMEPAD_BUTTON_DPAD_LEFT = 14;
		public const int GLFW_GAMEPAD_BUTTON_LAST = GLFW_GAMEPAD_BUTTON_DPAD_LEFT;
		public const int GLFW_GAMEPAD_BUTTON_CROSS = GLFW_GAMEPAD_BUTTON_A;
		public const int GLFW_GAMEPAD_BUTTON_CIRCLE = GLFW_GAMEPAD_BUTTON_B;
		public const int GLFW_GAMEPAD_BUTTON_SQUARE = GLFW_GAMEPAD_BUTTON_X;
		public const int GLFW_GAMEPAD_BUTTON_TRIANGLE = GLFW_GAMEPAD_BUTTON_Y;
		public const int GLFW_GAMEPAD_AXIS_LEFT_X = 0;
		public const int GLFW_GAMEPAD_AXIS_LEFT_Y = 1;
		public const int GLFW_GAMEPAD_AXIS_RIGHT_X = 2;
		public const int GLFW_GAMEPAD_AXIS_RIGHT_Y = 3;
		public const int GLFW_GAMEPAD_AXIS_LEFT_TRIGGER = 4;
		public const int GLFW_GAMEPAD_AXIS_RIGHT_TRIGGER = 5;
		public const int GLFW_GAMEPAD_AXIS_LAST = GLFW_GAMEPAD_AXIS_RIGHT_TRIGGER;
		public const int GLFW_NO_ERROR = 0;
		public const int GLFW_NOT_INITIALIZED = 0x00010001;
		public const int GLFW_NO_CURRENT_CONTEXT = 0x00010002;
		public const int GLFW_INVALID_ENUM = 0x00010003;
		public const int GLFW_INVALID_VALUE = 0x00010004;
		public const int GLFW_OUT_OF_MEMORY = 0x00010005;
		public const int GLFW_API_UNAVAILABLE = 0x00010006;
		public const int GLFW_VERSION_UNAVAILABLE = 0x00010007;
		public const int GLFW_PLATFORM_ERROR = 0x00010008;
		public const int GLFW_FORMAT_UNAVAILABLE = 0x00010009;
		public const int GLFW_NO_WINDOW_CONTEXT = 0x0001000A;
		public const int GLFW_FOCUSED = 0x00020001;
		public const int GLFW_ICONIFIED = 0x00020002;
		public const int GLFW_RESIZABLE = 0x00020003;
		public const int GLFW_VISIBLE = 0x00020004;
		public const int GLFW_DECORATED = 0x00020005;
		public const int GLFW_AUTO_ICONIFY = 0x00020006;
		public const int GLFW_FLOATING = 0x00020007;
		public const int GLFW_MAXIMIZED = 0x00020008;
		public const int GLFW_CENTER_CURSOR = 0x00020009;
		public const int GLFW_TRANSPARENT_FRAMEBUFFER = 0x0002000A;
		public const int GLFW_HOVERED = 0x0002000B;
		public const int GLFW_FOCUS_ON_SHOW = 0x0002000C;
		public const int GLFW_RED_BITS = 0x00021001;
		public const int GLFW_GREEN_BITS = 0x00021002;
		public const int GLFW_BLUE_BITS = 0x00021003;
		public const int GLFW_ALPHA_BITS = 0x00021004;
		public const int GLFW_DEPTH_BITS = 0x00021005;
		public const int GLFW_STENCIL_BITS = 0x00021006;
		public const int GLFW_ACCUM_RED_BITS = 0x00021007;
		public const int GLFW_ACCUM_GREEN_BITS = 0x00021008;
		public const int GLFW_ACCUM_BLUE_BITS = 0x00021009;
		public const int GLFW_ACCUM_ALPHA_BITS = 0x0002100A;
		public const int GLFW_AUX_BUFFERS = 0x0002100B;
		public const int GLFW_STEREO = 0x0002100C;
		public const int GLFW_SAMPLES = 0x0002100D;
		public const int GLFW_SRGB_CAPABLE = 0x0002100E;
		public const int GLFW_REFRESH_RATE = 0x0002100F;
		public const int GLFW_DOUBLEBUFFER = 0x00021010;
		public const int GLFW_CLIENT_API = 0x00022001;
		public const int GLFW_CONTEXT_VERSION_MAJOR = 0x00022002;
		public const int GLFW_CONTEXT_VERSION_MINOR = 0x00022003;
		public const int GLFW_CONTEXT_REVISION = 0x00022004;
		public const int GLFW_CONTEXT_ROBUSTNESS = 0x00022005;
		public const int GLFW_OPENGL_FORWARD_COMPAT = 0x00022006;
		public const int GLFW_OPENGL_DEBUG_CONTEXT = 0x00022007;
		public const int GLFW_OPENGL_PROFILE = 0x00022008;
		public const int GLFW_CONTEXT_RELEASE_BEHAVIOR = 0x00022009;
		public const int GLFW_CONTEXT_NO_ERROR = 0x0002200A;
		public const int GLFW_CONTEXT_CREATION_API = 0x0002200B;
		public const int GLFW_SCALE_TO_MONITOR = 0x0002200C;
		public const int GLFW_COCOA_RETINA_FRAMEBUFFER = 0x00023001;
		public const int GLFW_COCOA_FRAME_NAME = 0x00023002;
		public const int GLFW_COCOA_GRAPHICS_SWITCHING = 0x00023003;
		public const int GLFW_X11_CLASS_NAME = 0x00024001;
		public const int GLFW_X11_INSTANCE_NAME = 0x00024002;
		public const int GLFW_WAYLAND_APP_ID = 0x00026001;
		public const int GLFW_NO_API = 0;
		public const int GLFW_OPENGL_API = 0x00030001;
		public const int GLFW_OPENGL_ES_API = 0x00030002;
		public const int GLFW_NO_ROBUSTNESS = 0;
		public const int GLFW_NO_RESET_NOTIFICATION = 0x00031001;
		public const int GLFW_LOSE_CONTEXT_ON_RESET = 0x00031002;
		public const int GLFW_OPENGL_ANY_PROFILE = 0;
		public const int GLFW_OPENGL_CORE_PROFILE = 0x00032001;
		public const int GLFW_OPENGL_COMPAT_PROFILE = 0x00032002;
		public const int GLFW_CURSOR = 0x00033001;
		public const int GLFW_STICKY_KEYS = 0x00033002;
		public const int GLFW_STICKY_MOUSE_BUTTONS = 0x00033003;
		public const int GLFW_LOCK_KEY_MODS = 0x00033004;
		public const int GLFW_RAW_MOUSE_MOTION = 0x00033005;
		public const int GLFW_CURSOR_NORMAL = 0x00034001;
		public const int GLFW_CURSOR_HIDDEN = 0x00034002;
		public const int GLFW_CURSOR_DISABLED = 0x00034003;
		public const int GLFW_ANY_RELEASE_BEHAVIOR = 0;
		public const int GLFW_RELEASE_BEHAVIOR_FLUSH = 0x00035001;
		public const int GLFW_RELEASE_BEHAVIOR_NONE = 0x00035002;
		public const int GLFW_NATIVE_CONTEXT_API = 0x00036001;
		public const int GLFW_EGL_CONTEXT_API = 0x00036002;
		public const int GLFW_OSMESA_CONTEXT_API = 0x00036003;
		public const int GLFW_ARROW_CURSOR = 0x00036001;
		public const int GLFW_IBEAM_CURSOR = 0x00036002;
		public const int GLFW_CROSSHAIR_CURSOR = 0x00036003;
		public const int GLFW_POINTING_HAND_CURSOR = 0x00036004;
		public const int GLFW_RESIZE_EW_CURSOR = 0x00036005;
		public const int GLFW_RESIZE_NS_CURSOR = 0x00036006;
		public const int GLFW_RESIZE_NWSE_CURSOR = 0x00036007;
		public const int GLFW_RESIZE_NESW_CURSOR = 0x00036008;
		public const int GLFW_RESIZE_ALL_CURSOR = 0x00036009;
		public const int GLFW_CONNECTED = 0x00040001;
		public const int GLFW_DISCONNECTED = 0x00040002;
		public const int GLFW_JOYSTICK_HAT_BUTTONS = 0x00050001;
		public const int GLFW_COCOA_CHDIR_RESOURCES = 0x00051001;
		public const int GLFW_COCOA_MENUBAR = 0x00051002;
		public const int GLFW_PLATFORM = 0x00050003;
		public const int GLFW_ANY_PLATFORM = 0x00060000;
		public const int GLFW_PLATFORM_WIN32 = 0x00060001;
		public const int GLFW_PLATFORM_COCOA = 0x00060002;
		public const int GLFW_PLATFORM_WAYLAND = 0x00060003;
		public const int GLFW_PLATFORM_X11 = 0x00060004;
		public const int GLFW_PLATFORM_NULL = 0x00060005;
		public const int GLFW_DONT_CARE = -1;

		public struct GLFWvidmode
		{
			public int width;
			public int height;
			public int redBits;
			public int greenBits;
			public int blueBits;
			public int refreshRate;
		}

		public struct GLFWgammaramp
		{
			public ushort[] red;
			public ushort[] green;
			public ushort[] blue;
			public uint size;
		}

		public struct GLFWimage
		{
			public int width;
			public int height;
			public IntPtr pixels;
		}

		public struct GLFWgamepadstate
		{
			public IntPtr buttons;
			public float axes;
		}

		[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		public delegate void GLFWerrorfun(int error, string description);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		public delegate void GLFWwindowposfun(IntPtr window, int xpos, int ypos);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		public delegate void GLFWwindowsizefun(IntPtr window, int width, int height);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		public delegate void GLFWwindowclosefun(IntPtr window);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		public delegate void GLFWwindowrefreshfun(IntPtr window);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		public delegate void GLFWwindowfocusfun(IntPtr window, int focused);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		public delegate void GLFWwindowiconifyfun(IntPtr window, int iconified);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		public delegate void GLFWwindowmaximizefun(IntPtr window, int iconified);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		public delegate void GLFWframebuffersizefun(IntPtr window, int width, int height);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		public delegate void GLFWwindowcontentscalefun(IntPtr window, float xscale, float yscale);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		public delegate void GLFWmousebuttonfun(IntPtr window, int button, int action, int mods);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		public delegate void GLFWcursorposfun(IntPtr window, double xpos, double ypos);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		public delegate void GLFWcursorenterfun(IntPtr window, int entered);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		public delegate void GLFWscrollfun(IntPtr window, double xoffset, double yoffset);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		public delegate void GLFWkeyfun(IntPtr window, int key, int scancode, int action, int mods);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		public delegate void GLFWcharfun(IntPtr window, uint codepoint);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		public delegate void GLFWcharmodsfun(IntPtr window, uint codepoint, int mods);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		public delegate void GLFWdropfun(IntPtr window, int count, IntPtr paths);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		public delegate void GLFWmonitorfun(IntPtr monitor, int @event);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		public delegate void GLFWjoystickfun(int jid, int @event);

		private static class Delegates
		{
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate int glfwInit();

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwTerminate();

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwInitHint(int hint, int value);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwGetVersion(out int major, out int minor, out int rev);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate IntPtr glfwGetVersionString();

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate int glfwGetError(IntPtr description);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate GLFWerrorfun glfwSetErrorCallback(GLFWerrorfun cbfun);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate IntPtr glfwGetMonitors(out int count);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate IntPtr glfwGetPrimaryMonitor();

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwGetMonitorPos(IntPtr monitor, out int xpos, out int ypos);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwGetMonitorWorkarea(IntPtr monitor, out int xpos, out int ypos, out int width, out int height);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwGetMonitorPhysicalSize(IntPtr monitor, out int widthMM, out int heightMM);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwGetMonitorContentScale(IntPtr monitor, out IntPtr xscale, out IntPtr yscale);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate IntPtr glfwGetMonitorName(IntPtr monitor);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwSetMonitorUserPointer(IntPtr monitor, IntPtr pointer);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate IntPtr glfwGetMonitorUserPointer(IntPtr monitor);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate GLFWmonitorfun glfwSetMonitorCallback(GLFWmonitorfun cbfun);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate IntPtr glfwGetVideoModes(IntPtr monitor, out int count);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate IntPtr glfwGetVideoMode(IntPtr monitor);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwSetGamma(IntPtr monitor, float gamma);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate IntPtr glfwGetGammaRamp(IntPtr monitor);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwSetGammaRamp(IntPtr monitor, IntPtr ramp);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwDefaultWindowHints();

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwWindowHint(int hint, int value);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwWindowHintString(int hint, string value);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate IntPtr glfwCreateWindow(int width, int height, IntPtr title, IntPtr monitor, IntPtr share);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwDestroyWindow(IntPtr window);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate int glfwWindowShouldClose(IntPtr window);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwSetWindowShouldClose(IntPtr window, int value);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwSetWindowTitle(IntPtr window, IntPtr title);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwSetWindowIcon(IntPtr window, int count, IntPtr images);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwGetWindowPos(IntPtr window, out int xpos, out int ypos);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwSetWindowPos(IntPtr window, int xpos, int ypos);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwGetWindowSize(IntPtr window, out int width, out int height);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwSetWindowSizeLimits(IntPtr window, int minwidth, int minheight, int maxwidth, int maxheight);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwSetWindowAspectRatio(IntPtr window, int numer, int denom);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwSetWindowSize(IntPtr window, int width, int height);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwGetFramebufferSize(IntPtr window, out int width, out int height);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwGetWindowFrameSize(IntPtr window, out int left, out int top, out int right, out int bottom);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwGetWindowContentScale(IntPtr window, out float xscale, out float yscale);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate float glfwGetWindowOpacity(IntPtr window);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwSetWindowOpacity(IntPtr window, float opacity);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwIconifyWindow(IntPtr window);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwRestoreWindow(IntPtr window);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwMaximizeWindow(IntPtr window);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwShowWindow(IntPtr window);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwHideWindow(IntPtr window);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwFocusWindow(IntPtr window);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwRequestWindowAttention(IntPtr window);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate IntPtr glfwGetWindowMonitor(IntPtr window);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwSetWindowMonitor(IntPtr window, IntPtr monitor, int xpos, int ypos, int width, int height, int refreshRate);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate int glfwGetWindowAttrib(IntPtr window, int attrib);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwSetWindowAttrib(IntPtr window, int attrib, int value);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwSetWindowUserPointer(IntPtr window, IntPtr pointer);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate IntPtr glfwGetWindowUserPointer(IntPtr window);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate GLFWwindowposfun glfwSetWindowPosCallback(IntPtr window, GLFWwindowposfun cbfun);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate GLFWwindowsizefun glfwSetWindowSizeCallback(IntPtr window, GLFWwindowsizefun cbfun);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate GLFWwindowclosefun glfwSetWindowCloseCallback(IntPtr window, GLFWwindowclosefun cbfun);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate GLFWwindowrefreshfun glfwSetWindowRefreshCallback(IntPtr window, GLFWwindowrefreshfun cbfun);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate GLFWwindowfocusfun glfwSetWindowFocusCallback(IntPtr window, GLFWwindowfocusfun cbfun);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate GLFWwindowiconifyfun glfwSetWindowIconifyCallback(IntPtr window, GLFWwindowiconifyfun cbfun);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate GLFWwindowmaximizefun glfwSetWindowMaximizeCallback(IntPtr window, GLFWwindowmaximizefun cbfun);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate GLFWframebuffersizefun glfwSetFramebufferSizeCallback(IntPtr window, GLFWframebuffersizefun cbfun);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate GLFWwindowcontentscalefun glfwSetWindowContentScaleCallback(IntPtr window, GLFWwindowcontentscalefun cbfun);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwPollEvents();

			[UnmanagedFunctionPointer(CallingConvention.Cdecl),	 SuppressUnmanagedCodeSecurity]
			public delegate void glfwWaitEvents();

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwWaitEventsTimeout(double timeout);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwPostEmptyEvent();

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate int glfwGetInputMode(IntPtr window, int mode);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwSetInputMode(IntPtr window, int mode, int value);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate int glfwRawMouseMotionSupported();

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate IntPtr glfwGetKeyName(int key, int scancode);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate int glfwGetKeyScancode(int key);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate int glfwGetKey(IntPtr window, int key);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate int glfwGetMouseButton(IntPtr window, int button);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwGetCursorPos(IntPtr window, out double xpos, out double ypos);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwSetCursorPos(IntPtr window, double xpos, double ypos);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate IntPtr glfwCreateCursor(IntPtr image, int xhot, int yhot);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate IntPtr glfwCreateStandardCursor(int shape);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwDestroyCursor(IntPtr cursor);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwSetCursor(IntPtr window, IntPtr cursor);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate GLFWkeyfun glfwSetKeyCallback(IntPtr window, GLFWkeyfun cbfun);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate GLFWcharfun glfwSetCharCallback(IntPtr window, GLFWcharfun cbfun);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate GLFWcharmodsfun glfwSetCharModsCallback(IntPtr window, GLFWcharmodsfun cbfun);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate GLFWmousebuttonfun glfwSetMouseButtonCallback(IntPtr window, GLFWmousebuttonfun cbfun);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate GLFWcursorposfun glfwSetCursorPosCallback(IntPtr window, GLFWcursorposfun cbfun);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate GLFWcursorenterfun glfwSetCursorEnterCallback(IntPtr window, GLFWcursorenterfun cbfun);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate GLFWscrollfun glfwSetScrollCallback(IntPtr window, GLFWscrollfun cbfun);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate GLFWdropfun glfwSetDropCallback(IntPtr window, GLFWdropfun cbfun);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate int glfwJoystickPresent(int jid);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate IntPtr glfwGetJoystickAxes(int jid, out int count);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate IntPtr glfwGetJoystickButtons(int jid, out int count);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate IntPtr glfwGetJoystickHats(int jid, out int count);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate IntPtr glfwGetJoystickName(int jid);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate IntPtr glfwGetJoystickGUID(int jid);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwSetJoystickUserPointer(int jid, IntPtr pointer);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate IntPtr glfwGetJoystickUserPointer(int jid);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate int glfwJoystickIsGamepad(int jid);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate GLFWjoystickfun glfwSetJoystickCallback(GLFWjoystickfun cbfun);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate int glfwUpdateGamepadMappings(string @string);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate IntPtr glfwGetGamepadName(int jid);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate int glfwGetGamepadState(int jid, out IntPtr state);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwSetClipboardString(IntPtr window, IntPtr s);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate IntPtr glfwGetClipboardString(IntPtr window);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate double glfwGetTime();

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwSetTime(double time);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate ulong glfwGetTimerValue();

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate ulong glfwGetTimerFrequency();

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwMakeContextCurrent(IntPtr window);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate IntPtr glfwGetCurrentContext();

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwSwapBuffers(IntPtr window);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwSwapInterval(int interval);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate int glfwExtensionSupported(string extension);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate IntPtr glfwGetProcAddress(string procname);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate int glfwVulkanSupported();

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate IntPtr glfwGetRequiredInstanceExtensions(out uint count);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate IntPtr glfwGetInstanceProcAddress(IntPtr instance, string procname);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate int glfwGetPhysicalDevicePresentationSupport(IntPtr instance, IntPtr device, uint queuefamily);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate int glfwCreateWindowSurface(IntPtr instance, IntPtr window, IntPtr allocator, out IntPtr surface);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate IntPtr glfwGetWin32Window(IntPtr window);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate IntPtr glfwGetX11Window(IntPtr window);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            public delegate IntPtr glfwGetX11Display();

            [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate IntPtr glfwGetCocoaWindow(IntPtr window);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate IntPtr glfwGetPlatform();


		}

		private static Delegates.glfwInit _glfwInit;

		private static Delegates.glfwTerminate _glfwTerminate;

		private static Delegates.glfwInitHint _glfwInitHint;

		private static Delegates.glfwGetVersion _glfwGetVersion;

		private static Delegates.glfwGetVersionString _glfwGetVersionString;

		private static Delegates.glfwGetError _glfwGetError;

		private static Delegates.glfwSetErrorCallback _glfwSetErrorCallback;

		private static Delegates.glfwGetMonitors _glfwGetMonitors;

		private static Delegates.glfwGetPrimaryMonitor _glfwGetPrimaryMonitor;

		private static Delegates.glfwGetMonitorPos _glfwGetMonitorPos;

		private static Delegates.glfwGetMonitorWorkarea _glfwGetMonitorWorkarea;

		private static Delegates.glfwGetMonitorPhysicalSize _glfwGetMonitorPhysicalSize;

		private static Delegates.glfwGetMonitorContentScale _glfwGetMonitorContentScale;

		private static Delegates.glfwGetMonitorName _glfwGetMonitorName;

		private static Delegates.glfwSetMonitorUserPointer _glfwSetMonitorUserPointer;

		private static Delegates.glfwGetMonitorUserPointer _glfwGetMonitorUserPointer;

		private static Delegates.glfwSetMonitorCallback _glfwSetMonitorCallback;

		private static Delegates.glfwGetVideoModes _glfwGetVideoModes;

		private static Delegates.glfwGetVideoMode _glfwGetVideoMode;

		private static Delegates.glfwSetGamma _glfwSetGamma;

		private static Delegates.glfwGetGammaRamp _glfwGetGammaRamp;

		private static Delegates.glfwSetGammaRamp _glfwSetGammaRamp;

		private static Delegates.glfwDefaultWindowHints _glfwDefaultWindowHints;

		private static Delegates.glfwWindowHint _glfwWindowHint;

		private static Delegates.glfwWindowHintString _glfwWindowHintString;

		private static Delegates.glfwCreateWindow _glfwCreateWindow;

		private static Delegates.glfwDestroyWindow _glfwDestroyWindow;

		private static Delegates.glfwWindowShouldClose _glfwWindowShouldClose;

		private static Delegates.glfwSetWindowShouldClose _glfwSetWindowShouldClose;

		private static Delegates.glfwSetWindowTitle _glfwSetWindowTitle;

		private static Delegates.glfwSetWindowIcon _glfwSetWindowIcon;

		private static Delegates.glfwGetWindowPos _glfwGetWindowPos;

		private static Delegates.glfwSetWindowPos _glfwSetWindowPos;

		private static Delegates.glfwGetWindowSize _glfwGetWindowSize;

		private static Delegates.glfwSetWindowSizeLimits _glfwSetWindowSizeLimits;

		private static Delegates.glfwSetWindowAspectRatio _glfwSetWindowAspectRatio;

		private static Delegates.glfwSetWindowSize _glfwSetWindowSize;

		private static Delegates.glfwGetFramebufferSize _glfwGetFramebufferSize;

		private static Delegates.glfwGetWindowFrameSize _glfwGetWindowFrameSize;

		private static Delegates.glfwGetWindowContentScale _glfwGetWindowContentScale;

		private static Delegates.glfwGetWindowOpacity _glfwGetWindowOpacity;

		private static Delegates.glfwSetWindowOpacity _glfwSetWindowOpacity;

		private static Delegates.glfwIconifyWindow _glfwIconifyWindow;

		private static Delegates.glfwRestoreWindow _glfwRestoreWindow;

		private static Delegates.glfwMaximizeWindow _glfwMaximizeWindow;

		private static Delegates.glfwShowWindow _glfwShowWindow;

		private static Delegates.glfwHideWindow _glfwHideWindow;

		private static Delegates.glfwFocusWindow _glfwFocusWindow;

		private static Delegates.glfwRequestWindowAttention _glfwRequestWindowAttention;

		private static Delegates.glfwGetWindowMonitor _glfwGetWindowMonitor;

		private static Delegates.glfwSetWindowMonitor _glfwSetWindowMonitor;

		private static Delegates.glfwGetWindowAttrib _glfwGetWindowAttrib;

		private static Delegates.glfwSetWindowAttrib _glfwSetWindowAttrib;

		private static Delegates.glfwSetWindowUserPointer _glfwSetWindowUserPointer;

		private static Delegates.glfwGetWindowUserPointer _glfwGetWindowUserPointer;

		private static Delegates.glfwSetWindowPosCallback _glfwSetWindowPosCallback;

		private static Delegates.glfwSetWindowSizeCallback _glfwSetWindowSizeCallback;

		private static Delegates.glfwSetWindowCloseCallback _glfwSetWindowCloseCallback;

		private static Delegates.glfwSetWindowRefreshCallback _glfwSetWindowRefreshCallback;

		private static Delegates.glfwSetWindowFocusCallback _glfwSetWindowFocusCallback;

		private static Delegates.glfwSetWindowIconifyCallback _glfwSetWindowIconifyCallback;

		private static Delegates.glfwSetWindowMaximizeCallback _glfwSetWindowMaximizeCallback;

		private static Delegates.glfwSetFramebufferSizeCallback _glfwSetFramebufferSizeCallback;

		private static Delegates.glfwSetWindowContentScaleCallback _glfwSetWindowContentScaleCallback;

		private static Delegates.glfwPollEvents _glfwPollEvents;

		private static Delegates.glfwWaitEvents _glfwWaitEvents;

		private static Delegates.glfwWaitEventsTimeout _glfwWaitEventsTimeout;

		private static Delegates.glfwPostEmptyEvent _glfwPostEmptyEvent;

		private static Delegates.glfwGetInputMode _glfwGetInputMode;

		private static Delegates.glfwSetInputMode _glfwSetInputMode;

		private static Delegates.glfwRawMouseMotionSupported _glfwRawMouseMotionSupported;

		private static Delegates.glfwGetKeyName _glfwGetKeyName;

		private static Delegates.glfwGetKeyScancode _glfwGetKeyScancode;

		private static Delegates.glfwGetKey _glfwGetKey;

		private static Delegates.glfwGetMouseButton _glfwGetMouseButton;

		private static Delegates.glfwGetCursorPos _glfwGetCursorPos;

		private static Delegates.glfwSetCursorPos _glfwSetCursorPos;

		private static Delegates.glfwCreateCursor _glfwCreateCursor;

		private static Delegates.glfwCreateStandardCursor _glfwCreateStandardCursor;

		private static Delegates.glfwDestroyCursor _glfwDestroyCursor;

		private static Delegates.glfwSetCursor _glfwSetCursor;

		private static Delegates.glfwSetKeyCallback _glfwSetKeyCallback;

		private static Delegates.glfwSetCharCallback _glfwSetCharCallback;

		private static Delegates.glfwSetCharModsCallback _glfwSetCharModsCallback;

		private static Delegates.glfwSetMouseButtonCallback _glfwSetMouseButtonCallback;

		private static Delegates.glfwSetCursorPosCallback _glfwSetCursorPosCallback;

		private static Delegates.glfwSetCursorEnterCallback _glfwSetCursorEnterCallback;

		private static Delegates.glfwSetScrollCallback _glfwSetScrollCallback;

		private static Delegates.glfwSetDropCallback _glfwSetDropCallback;

		private static Delegates.glfwJoystickPresent _glfwJoystickPresent;

		private static Delegates.glfwGetJoystickAxes _glfwGetJoystickAxes;

		private static Delegates.glfwGetJoystickButtons _glfwGetJoystickButtons;

		private static Delegates.glfwGetJoystickHats _glfwGetJoystickHats;

		private static Delegates.glfwGetJoystickName _glfwGetJoystickName;

		private static Delegates.glfwGetJoystickGUID _glfwGetJoystickGUID;

		private static Delegates.glfwSetJoystickUserPointer _glfwSetJoystickUserPointer;

		private static Delegates.glfwGetJoystickUserPointer _glfwGetJoystickUserPointer;

		private static Delegates.glfwJoystickIsGamepad _glfwJoystickIsGamepad;

		private static Delegates.glfwSetJoystickCallback _glfwSetJoystickCallback;

		private static Delegates.glfwUpdateGamepadMappings _glfwUpdateGamepadMappings;

		private static Delegates.glfwGetGamepadName _glfwGetGamepadName;

		private static Delegates.glfwGetGamepadState _glfwGetGamepadState;

		private static Delegates.glfwSetClipboardString _glfwSetClipboardString;

		private static Delegates.glfwGetClipboardString _glfwGetClipboardString;

		private static Delegates.glfwGetTime _glfwGetTime;

		private static Delegates.glfwSetTime _glfwSetTime;

		private static Delegates.glfwGetTimerValue _glfwGetTimerValue;

		private static Delegates.glfwGetTimerFrequency _glfwGetTimerFrequency;

		private static Delegates.glfwMakeContextCurrent _glfwMakeContextCurrent;

		private static Delegates.glfwGetCurrentContext _glfwGetCurrentContext;

		private static Delegates.glfwSwapBuffers _glfwSwapBuffers;

		private static Delegates.glfwSwapInterval _glfwSwapInterval;

		private static Delegates.glfwExtensionSupported _glfwExtensionSupported;

		private static Delegates.glfwGetProcAddress _glfwGetProcAddress;

		private static Delegates.glfwVulkanSupported _glfwVulkanSupported;

		private static Delegates.glfwGetRequiredInstanceExtensions _glfwGetRequiredInstanceExtensions;

		private static Delegates.glfwGetInstanceProcAddress _glfwGetInstanceProcAddress;

		private static Delegates.glfwGetPhysicalDevicePresentationSupport _glfwGetPhysicalDevicePresentationSupport;

		private static Delegates.glfwCreateWindowSurface _glfwCreateWindowSurface;

		private static Delegates.glfwGetWin32Window _glfwGetWin32Window;

		private static Delegates.glfwGetX11Window _glfwGetX11Window;

        private static Delegates.glfwGetX11Display _glfwGetX11Display;        

        private static Delegates.glfwGetCocoaWindow _glfwGetCocoaWindow;

        private static Delegates.glfwGetPlatform _glfwGetPlatform;

		/// <summary>
        /// Initializes the GLFW library.
        /// </summary>
        /// <remarks>
		/// This function initializes the GLFW library.  Before most GLFW functions can
		/// be used, GLFW must be initialized, and before an application terminates GLFW
		/// should be terminated in order to free any resources allocated during or
		/// after initialization.
		/// </remarks>
		/// <returns>
		/// `GLFW_TRUE` if successful, or `GLFW_FALSE` if an
		/// [error](@ref error_handling) occurred.
		/// </returns>
        public static int glfwInit()
        {
            LoadFunctions(LoadAssembly());
            return _glfwInit();
        }

		private static void LoadFunctions(Func<string, IntPtr> getProcAddress)
		{
			_glfwInit = Marshal.GetDelegateForFunctionPointer<Delegates.glfwInit>(getProcAddress("glfwInit"));
			_glfwTerminate = Marshal.GetDelegateForFunctionPointer<Delegates.glfwTerminate>(getProcAddress("glfwTerminate"));
			_glfwInitHint = Marshal.GetDelegateForFunctionPointer<Delegates.glfwInitHint>(getProcAddress("glfwInitHint"));
			_glfwGetVersion = Marshal.GetDelegateForFunctionPointer<Delegates.glfwGetVersion>(getProcAddress("glfwGetVersion"));
			_glfwGetVersionString = Marshal.GetDelegateForFunctionPointer<Delegates.glfwGetVersionString>(getProcAddress("glfwGetVersionString"));
			_glfwGetError = Marshal.GetDelegateForFunctionPointer<Delegates.glfwGetError>(getProcAddress("glfwGetError"));
			_glfwSetErrorCallback = Marshal.GetDelegateForFunctionPointer<Delegates.glfwSetErrorCallback>(getProcAddress("glfwSetErrorCallback"));
			_glfwGetMonitors = Marshal.GetDelegateForFunctionPointer<Delegates.glfwGetMonitors>(getProcAddress("glfwGetMonitors"));
			_glfwGetPrimaryMonitor = Marshal.GetDelegateForFunctionPointer<Delegates.glfwGetPrimaryMonitor>(getProcAddress("glfwGetPrimaryMonitor"));
			_glfwGetMonitorPos = Marshal.GetDelegateForFunctionPointer<Delegates.glfwGetMonitorPos>(getProcAddress("glfwGetMonitorPos"));
			_glfwGetMonitorWorkarea = Marshal.GetDelegateForFunctionPointer<Delegates.glfwGetMonitorWorkarea>(getProcAddress("glfwGetMonitorWorkarea"));
			_glfwGetMonitorPhysicalSize = Marshal.GetDelegateForFunctionPointer<Delegates.glfwGetMonitorPhysicalSize>(getProcAddress("glfwGetMonitorPhysicalSize"));
			_glfwGetMonitorContentScale = Marshal.GetDelegateForFunctionPointer<Delegates.glfwGetMonitorContentScale>(getProcAddress("glfwGetMonitorContentScale"));
			_glfwGetMonitorName = Marshal.GetDelegateForFunctionPointer<Delegates.glfwGetMonitorName>(getProcAddress("glfwGetMonitorName"));
			_glfwSetMonitorUserPointer = Marshal.GetDelegateForFunctionPointer<Delegates.glfwSetMonitorUserPointer>(getProcAddress("glfwSetMonitorUserPointer"));
			_glfwGetMonitorUserPointer = Marshal.GetDelegateForFunctionPointer<Delegates.glfwGetMonitorUserPointer>(getProcAddress("glfwGetMonitorUserPointer"));
			_glfwSetMonitorCallback = Marshal.GetDelegateForFunctionPointer<Delegates.glfwSetMonitorCallback>(getProcAddress("glfwSetMonitorCallback"));
			_glfwGetVideoModes = Marshal.GetDelegateForFunctionPointer<Delegates.glfwGetVideoModes>(getProcAddress("glfwGetVideoModes"));
			_glfwGetVideoMode = Marshal.GetDelegateForFunctionPointer<Delegates.glfwGetVideoMode>(getProcAddress("glfwGetVideoMode"));
			_glfwSetGamma = Marshal.GetDelegateForFunctionPointer<Delegates.glfwSetGamma>(getProcAddress("glfwSetGamma"));
			_glfwGetGammaRamp = Marshal.GetDelegateForFunctionPointer<Delegates.glfwGetGammaRamp>(getProcAddress("glfwGetGammaRamp"));
			_glfwSetGammaRamp = Marshal.GetDelegateForFunctionPointer<Delegates.glfwSetGammaRamp>(getProcAddress("glfwSetGammaRamp"));
			_glfwDefaultWindowHints = Marshal.GetDelegateForFunctionPointer<Delegates.glfwDefaultWindowHints>(getProcAddress("glfwDefaultWindowHints"));
			_glfwWindowHint = Marshal.GetDelegateForFunctionPointer<Delegates.glfwWindowHint>(getProcAddress("glfwWindowHint"));
			_glfwWindowHintString = Marshal.GetDelegateForFunctionPointer<Delegates.glfwWindowHintString>(getProcAddress("glfwWindowHintString"));
			_glfwCreateWindow = Marshal.GetDelegateForFunctionPointer<Delegates.glfwCreateWindow>(getProcAddress("glfwCreateWindow"));
			_glfwDestroyWindow = Marshal.GetDelegateForFunctionPointer<Delegates.glfwDestroyWindow>(getProcAddress("glfwDestroyWindow"));
			_glfwWindowShouldClose = Marshal.GetDelegateForFunctionPointer<Delegates.glfwWindowShouldClose>(getProcAddress("glfwWindowShouldClose"));
			_glfwSetWindowShouldClose = Marshal.GetDelegateForFunctionPointer<Delegates.glfwSetWindowShouldClose>(getProcAddress("glfwSetWindowShouldClose"));
			_glfwSetWindowTitle = Marshal.GetDelegateForFunctionPointer<Delegates.glfwSetWindowTitle>(getProcAddress("glfwSetWindowTitle"));
			_glfwSetWindowIcon = Marshal.GetDelegateForFunctionPointer<Delegates.glfwSetWindowIcon>(getProcAddress("glfwSetWindowIcon"));
			_glfwGetWindowPos = Marshal.GetDelegateForFunctionPointer<Delegates.glfwGetWindowPos>(getProcAddress("glfwGetWindowPos"));
			_glfwSetWindowPos = Marshal.GetDelegateForFunctionPointer<Delegates.glfwSetWindowPos>(getProcAddress("glfwSetWindowPos"));
			_glfwGetWindowSize = Marshal.GetDelegateForFunctionPointer<Delegates.glfwGetWindowSize>(getProcAddress("glfwGetWindowSize"));
			_glfwSetWindowSizeLimits = Marshal.GetDelegateForFunctionPointer<Delegates.glfwSetWindowSizeLimits>(getProcAddress("glfwSetWindowSizeLimits"));
			_glfwSetWindowAspectRatio = Marshal.GetDelegateForFunctionPointer<Delegates.glfwSetWindowAspectRatio>(getProcAddress("glfwSetWindowAspectRatio"));
			_glfwSetWindowSize = Marshal.GetDelegateForFunctionPointer<Delegates.glfwSetWindowSize>(getProcAddress("glfwSetWindowSize"));
			_glfwGetFramebufferSize = Marshal.GetDelegateForFunctionPointer<Delegates.glfwGetFramebufferSize>(getProcAddress("glfwGetFramebufferSize"));
			_glfwGetWindowFrameSize = Marshal.GetDelegateForFunctionPointer<Delegates.glfwGetWindowFrameSize>(getProcAddress("glfwGetWindowFrameSize"));
			_glfwGetWindowContentScale = Marshal.GetDelegateForFunctionPointer<Delegates.glfwGetWindowContentScale>(getProcAddress("glfwGetWindowContentScale"));
			_glfwGetWindowOpacity = Marshal.GetDelegateForFunctionPointer<Delegates.glfwGetWindowOpacity>(getProcAddress("glfwGetWindowOpacity"));
			_glfwSetWindowOpacity = Marshal.GetDelegateForFunctionPointer<Delegates.glfwSetWindowOpacity>(getProcAddress("glfwSetWindowOpacity"));
			_glfwIconifyWindow = Marshal.GetDelegateForFunctionPointer<Delegates.glfwIconifyWindow>(getProcAddress("glfwIconifyWindow"));
			_glfwRestoreWindow = Marshal.GetDelegateForFunctionPointer<Delegates.glfwRestoreWindow>(getProcAddress("glfwRestoreWindow"));
			_glfwMaximizeWindow = Marshal.GetDelegateForFunctionPointer<Delegates.glfwMaximizeWindow>(getProcAddress("glfwMaximizeWindow"));
			_glfwShowWindow = Marshal.GetDelegateForFunctionPointer<Delegates.glfwShowWindow>(getProcAddress("glfwShowWindow"));
			_glfwHideWindow = Marshal.GetDelegateForFunctionPointer<Delegates.glfwHideWindow>(getProcAddress("glfwHideWindow"));
			_glfwFocusWindow = Marshal.GetDelegateForFunctionPointer<Delegates.glfwFocusWindow>(getProcAddress("glfwFocusWindow"));
			_glfwRequestWindowAttention = Marshal.GetDelegateForFunctionPointer<Delegates.glfwRequestWindowAttention>(getProcAddress("glfwRequestWindowAttention"));
			_glfwGetWindowMonitor = Marshal.GetDelegateForFunctionPointer<Delegates.glfwGetWindowMonitor>(getProcAddress("glfwGetWindowMonitor"));
			_glfwSetWindowMonitor = Marshal.GetDelegateForFunctionPointer<Delegates.glfwSetWindowMonitor>(getProcAddress("glfwSetWindowMonitor"));
			_glfwGetWindowAttrib = Marshal.GetDelegateForFunctionPointer<Delegates.glfwGetWindowAttrib>(getProcAddress("glfwGetWindowAttrib"));
			_glfwSetWindowAttrib = Marshal.GetDelegateForFunctionPointer<Delegates.glfwSetWindowAttrib>(getProcAddress("glfwSetWindowAttrib"));
			_glfwSetWindowUserPointer = Marshal.GetDelegateForFunctionPointer<Delegates.glfwSetWindowUserPointer>(getProcAddress("glfwSetWindowUserPointer"));
			_glfwGetWindowUserPointer = Marshal.GetDelegateForFunctionPointer<Delegates.glfwGetWindowUserPointer>(getProcAddress("glfwGetWindowUserPointer"));
			_glfwSetWindowPosCallback = Marshal.GetDelegateForFunctionPointer<Delegates.glfwSetWindowPosCallback>(getProcAddress("glfwSetWindowPosCallback"));
			_glfwSetWindowSizeCallback = Marshal.GetDelegateForFunctionPointer<Delegates.glfwSetWindowSizeCallback>(getProcAddress("glfwSetWindowSizeCallback"));
			_glfwSetWindowCloseCallback = Marshal.GetDelegateForFunctionPointer<Delegates.glfwSetWindowCloseCallback>(getProcAddress("glfwSetWindowCloseCallback"));
			_glfwSetWindowRefreshCallback = Marshal.GetDelegateForFunctionPointer<Delegates.glfwSetWindowRefreshCallback>(getProcAddress("glfwSetWindowRefreshCallback"));
			_glfwSetWindowFocusCallback = Marshal.GetDelegateForFunctionPointer<Delegates.glfwSetWindowFocusCallback>(getProcAddress("glfwSetWindowFocusCallback"));
			_glfwSetWindowIconifyCallback = Marshal.GetDelegateForFunctionPointer<Delegates.glfwSetWindowIconifyCallback>(getProcAddress("glfwSetWindowIconifyCallback"));
			_glfwSetWindowMaximizeCallback = Marshal.GetDelegateForFunctionPointer<Delegates.glfwSetWindowMaximizeCallback>(getProcAddress("glfwSetWindowMaximizeCallback"));
			_glfwSetFramebufferSizeCallback = Marshal.GetDelegateForFunctionPointer<Delegates.glfwSetFramebufferSizeCallback>(getProcAddress("glfwSetFramebufferSizeCallback"));
			_glfwSetWindowContentScaleCallback = Marshal.GetDelegateForFunctionPointer<Delegates.glfwSetWindowContentScaleCallback>(getProcAddress("glfwSetWindowContentScaleCallback"));
			_glfwPollEvents = Marshal.GetDelegateForFunctionPointer<Delegates.glfwPollEvents>(getProcAddress("glfwPollEvents"));
			_glfwWaitEvents = Marshal.GetDelegateForFunctionPointer<Delegates.glfwWaitEvents>(getProcAddress("glfwWaitEvents"));
			_glfwWaitEventsTimeout = Marshal.GetDelegateForFunctionPointer<Delegates.glfwWaitEventsTimeout>(getProcAddress("glfwWaitEventsTimeout"));
			_glfwPostEmptyEvent = Marshal.GetDelegateForFunctionPointer<Delegates.glfwPostEmptyEvent>(getProcAddress("glfwPostEmptyEvent"));
			_glfwGetInputMode = Marshal.GetDelegateForFunctionPointer<Delegates.glfwGetInputMode>(getProcAddress("glfwGetInputMode"));
			_glfwSetInputMode = Marshal.GetDelegateForFunctionPointer<Delegates.glfwSetInputMode>(getProcAddress("glfwSetInputMode"));
			_glfwRawMouseMotionSupported = Marshal.GetDelegateForFunctionPointer<Delegates.glfwRawMouseMotionSupported>(getProcAddress("glfwRawMouseMotionSupported"));
			_glfwGetKeyName = Marshal.GetDelegateForFunctionPointer<Delegates.glfwGetKeyName>(getProcAddress("glfwGetKeyName"));
			_glfwGetKeyScancode = Marshal.GetDelegateForFunctionPointer<Delegates.glfwGetKeyScancode>(getProcAddress("glfwGetKeyScancode"));
			_glfwGetKey = Marshal.GetDelegateForFunctionPointer<Delegates.glfwGetKey>(getProcAddress("glfwGetKey"));
			_glfwGetMouseButton = Marshal.GetDelegateForFunctionPointer<Delegates.glfwGetMouseButton>(getProcAddress("glfwGetMouseButton"));
			_glfwGetCursorPos = Marshal.GetDelegateForFunctionPointer<Delegates.glfwGetCursorPos>(getProcAddress("glfwGetCursorPos"));
			_glfwSetCursorPos = Marshal.GetDelegateForFunctionPointer<Delegates.glfwSetCursorPos>(getProcAddress("glfwSetCursorPos"));
			_glfwCreateCursor = Marshal.GetDelegateForFunctionPointer<Delegates.glfwCreateCursor>(getProcAddress("glfwCreateCursor"));
			_glfwCreateStandardCursor = Marshal.GetDelegateForFunctionPointer<Delegates.glfwCreateStandardCursor>(getProcAddress("glfwCreateStandardCursor"));
			_glfwDestroyCursor = Marshal.GetDelegateForFunctionPointer<Delegates.glfwDestroyCursor>(getProcAddress("glfwDestroyCursor"));
			_glfwSetCursor = Marshal.GetDelegateForFunctionPointer<Delegates.glfwSetCursor>(getProcAddress("glfwSetCursor"));
			_glfwSetKeyCallback = Marshal.GetDelegateForFunctionPointer<Delegates.glfwSetKeyCallback>(getProcAddress("glfwSetKeyCallback"));
			_glfwSetCharCallback = Marshal.GetDelegateForFunctionPointer<Delegates.glfwSetCharCallback>(getProcAddress("glfwSetCharCallback"));
			_glfwSetCharModsCallback = Marshal.GetDelegateForFunctionPointer<Delegates.glfwSetCharModsCallback>(getProcAddress("glfwSetCharModsCallback"));
			_glfwSetMouseButtonCallback = Marshal.GetDelegateForFunctionPointer<Delegates.glfwSetMouseButtonCallback>(getProcAddress("glfwSetMouseButtonCallback"));
			_glfwSetCursorPosCallback = Marshal.GetDelegateForFunctionPointer<Delegates.glfwSetCursorPosCallback>(getProcAddress("glfwSetCursorPosCallback"));
			_glfwSetCursorEnterCallback = Marshal.GetDelegateForFunctionPointer<Delegates.glfwSetCursorEnterCallback>(getProcAddress("glfwSetCursorEnterCallback"));
			_glfwSetScrollCallback = Marshal.GetDelegateForFunctionPointer<Delegates.glfwSetScrollCallback>(getProcAddress("glfwSetScrollCallback"));
			_glfwSetDropCallback = Marshal.GetDelegateForFunctionPointer<Delegates.glfwSetDropCallback>(getProcAddress("glfwSetDropCallback"));
			_glfwJoystickPresent = Marshal.GetDelegateForFunctionPointer<Delegates.glfwJoystickPresent>(getProcAddress("glfwJoystickPresent"));
			_glfwGetJoystickAxes = Marshal.GetDelegateForFunctionPointer<Delegates.glfwGetJoystickAxes>(getProcAddress("glfwGetJoystickAxes"));
			_glfwGetJoystickButtons = Marshal.GetDelegateForFunctionPointer<Delegates.glfwGetJoystickButtons>(getProcAddress("glfwGetJoystickButtons"));
			_glfwGetJoystickHats = Marshal.GetDelegateForFunctionPointer<Delegates.glfwGetJoystickHats>(getProcAddress("glfwGetJoystickHats"));
			_glfwGetJoystickName = Marshal.GetDelegateForFunctionPointer<Delegates.glfwGetJoystickName>(getProcAddress("glfwGetJoystickName"));
			_glfwGetJoystickGUID = Marshal.GetDelegateForFunctionPointer<Delegates.glfwGetJoystickGUID>(getProcAddress("glfwGetJoystickGUID"));
			_glfwSetJoystickUserPointer = Marshal.GetDelegateForFunctionPointer<Delegates.glfwSetJoystickUserPointer>(getProcAddress("glfwSetJoystickUserPointer"));
			_glfwGetJoystickUserPointer = Marshal.GetDelegateForFunctionPointer<Delegates.glfwGetJoystickUserPointer>(getProcAddress("glfwGetJoystickUserPointer"));
			_glfwJoystickIsGamepad = Marshal.GetDelegateForFunctionPointer<Delegates.glfwJoystickIsGamepad>(getProcAddress("glfwJoystickIsGamepad"));
			_glfwSetJoystickCallback = Marshal.GetDelegateForFunctionPointer<Delegates.glfwSetJoystickCallback>(getProcAddress("glfwSetJoystickCallback"));
			_glfwUpdateGamepadMappings = Marshal.GetDelegateForFunctionPointer<Delegates.glfwUpdateGamepadMappings>(getProcAddress("glfwUpdateGamepadMappings"));
			_glfwGetGamepadName = Marshal.GetDelegateForFunctionPointer<Delegates.glfwGetGamepadName>(getProcAddress("glfwGetGamepadName"));
			_glfwGetGamepadState = Marshal.GetDelegateForFunctionPointer<Delegates.glfwGetGamepadState>(getProcAddress("glfwGetGamepadState"));
			_glfwSetClipboardString = Marshal.GetDelegateForFunctionPointer<Delegates.glfwSetClipboardString>(getProcAddress("glfwSetClipboardString"));
			_glfwGetClipboardString = Marshal.GetDelegateForFunctionPointer<Delegates.glfwGetClipboardString>(getProcAddress("glfwGetClipboardString"));
			_glfwGetTime = Marshal.GetDelegateForFunctionPointer<Delegates.glfwGetTime>(getProcAddress("glfwGetTime"));
			_glfwSetTime = Marshal.GetDelegateForFunctionPointer<Delegates.glfwSetTime>(getProcAddress("glfwSetTime"));
			_glfwGetTimerValue = Marshal.GetDelegateForFunctionPointer<Delegates.glfwGetTimerValue>(getProcAddress("glfwGetTimerValue"));
			_glfwGetTimerFrequency = Marshal.GetDelegateForFunctionPointer<Delegates.glfwGetTimerFrequency>(getProcAddress("glfwGetTimerFrequency"));
			_glfwMakeContextCurrent = Marshal.GetDelegateForFunctionPointer<Delegates.glfwMakeContextCurrent>(getProcAddress("glfwMakeContextCurrent"));
			_glfwGetCurrentContext = Marshal.GetDelegateForFunctionPointer<Delegates.glfwGetCurrentContext>(getProcAddress("glfwGetCurrentContext"));
			_glfwSwapBuffers = Marshal.GetDelegateForFunctionPointer<Delegates.glfwSwapBuffers>(getProcAddress("glfwSwapBuffers"));
			_glfwSwapInterval = Marshal.GetDelegateForFunctionPointer<Delegates.glfwSwapInterval>(getProcAddress("glfwSwapInterval"));
			_glfwExtensionSupported = Marshal.GetDelegateForFunctionPointer<Delegates.glfwExtensionSupported>(getProcAddress("glfwExtensionSupported"));
			_glfwGetProcAddress = Marshal.GetDelegateForFunctionPointer<Delegates.glfwGetProcAddress>(getProcAddress("glfwGetProcAddress"));
			_glfwVulkanSupported = Marshal.GetDelegateForFunctionPointer<Delegates.glfwVulkanSupported>(getProcAddress("glfwVulkanSupported"));
			_glfwGetRequiredInstanceExtensions = Marshal.GetDelegateForFunctionPointer<Delegates.glfwGetRequiredInstanceExtensions>(getProcAddress("glfwGetRequiredInstanceExtensions"));
			_glfwGetInstanceProcAddress = Marshal.GetDelegateForFunctionPointer<Delegates.glfwGetInstanceProcAddress>(getProcAddress("glfwGetInstanceProcAddress"));
			_glfwGetPhysicalDevicePresentationSupport = Marshal.GetDelegateForFunctionPointer<Delegates.glfwGetPhysicalDevicePresentationSupport>(getProcAddress("glfwGetPhysicalDevicePresentationSupport"));
			_glfwCreateWindowSurface = Marshal.GetDelegateForFunctionPointer<Delegates.glfwCreateWindowSurface>(getProcAddress("glfwCreateWindowSurface"));
			_glfwGetPlatform = Marshal.GetDelegateForFunctionPointer<Delegates.glfwGetPlatform>(getProcAddress("glfwGetPlatform"));
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) _glfwGetWin32Window = Marshal.GetDelegateForFunctionPointer<Delegates.glfwGetWin32Window>(getProcAddress("glfwGetWin32Window"));
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) _glfwGetX11Window = Marshal.GetDelegateForFunctionPointer<Delegates.glfwGetX11Window>(getProcAddress("glfwGetX11Window"));
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) _glfwGetX11Display = Marshal.GetDelegateForFunctionPointer<Delegates.glfwGetX11Display>(getProcAddress("glfwGetX11Display"));
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) _glfwGetCocoaWindow = Marshal.GetDelegateForFunctionPointer<Delegates.glfwGetCocoaWindow>(getProcAddress("glfwGetCocoaWindow"));
		}

		/// <summary>
		/// Terminates the GLFW library.
		/// </summary>
		/// <remarks>
		/// This function destroys all remaining windows and cursors, restores any
		/// modified gamma ramps and frees any other allocated resources.  Once this
		/// function is called, you must again call @ref glfwInit successfully before
		/// you will be able to use most GLFW functions.
		/// </remarks>
		public static void glfwTerminate()
		{
			_glfwTerminate();
		}

		/// <summary>
		/// Sets the specified init hint to the desired value.
		/// </summary>
		/// <remarks>
		/// This function sets hints for the next initialization of GLFW.
		/// </remarks>
		/// <param name="hint">
		/// The [init hint](@ref init_hints) to set.
		/// </param>
		/// <param name="value">
		/// The new value of the init hint.
		/// </param>
		public static void glfwInitHint(int hint, int value)
		{
			_glfwInitHint(hint, value);
		}

		/// <summary>
		/// Retrieves the version of the GLFW library.
		/// </summary>
		/// <remarks>
		/// This function retrieves the major, minor and revision numbers of the GLFW
		/// library.  It is intended for when you are using GLFW as a shared library and
		/// want to ensure that you are using the minimum required version.
		/// </remarks>
		/// <param name="major">
		/// Where to store the major version number, or `NULL`.
		/// </param>
		/// <param name="minor">
		/// Where to store the minor version number, or `NULL`.
		/// </param>
		/// <param name="rev">
		/// Where to store the revision number, or `NULL`.
		/// </param>
		public static void glfwGetVersion(out int major, out int minor, out int rev)
		{
			_glfwGetVersion(out major, out minor, out rev);
		}

		/// <summary>
		/// Returns a string describing the compile-time configuration.
		/// </summary>
		/// <remarks>
		/// This function returns the compile-time generated
		/// [version string](@ref intro_version_string) of the GLFW library binary.  It
		/// describes the version, platform, compiler and any platform-specific
		/// compile-time options.  It should not be confused with the OpenGL or OpenGL
		/// ES version string, queried with `glGetString`.
		/// </remarks>
		/// <returns>
		/// The ASCII encoded GLFW version string.
		/// </returns>
		public static string glfwGetVersionString()
		{
			var versionStringPtr = _glfwGetVersionString();
			return Marshal.PtrToStringAnsi(versionStringPtr);
		}

		/// <summary>
		/// Returns and clears the last error for the calling thread.
		/// </summary>
		/// <remarks>
		/// This function returns and clears the [error code](@ref errors) of the last
		/// error that occurred on the calling thread, and optionally a UTF-8 encoded
		/// human-readable description of it.  If no error has occurred since the last
		/// call, it returns @ref GLFW_NO_ERROR (zero) and the description pointer is
		/// set to `NULL`.
		/// </remarks>
		/// <param name="description">
		/// Where to store the error description pointer, or `NULL`.
		/// </param>
		/// <returns>
		/// The last error code for the calling thread, or @ref GLFW_NO_ERROR
		/// (zero).
		/// </returns>
		public static int glfwGetError(IntPtr description)
		{
			return _glfwGetError(description);
		}

		/// <summary>
		/// Sets the error callback.
		/// </summary>
		/// <remarks>
		/// This function sets the error callback, which is called with an error code
		/// and a human-readable description each time a GLFW error occurs.
		/// </remarks>
		/// <param name="cbfun">
		/// The new callback, or `NULL` to remove the currently set
		/// callback.
		/// </param>
		/// <returns>
		/// The previously set callback, or `NULL` if no callback was set.
		/// </returns>
		public static GLFWerrorfun glfwSetErrorCallback(GLFWerrorfun cbfun)
		{
			return _glfwSetErrorCallback(cbfun);
		}

		/// <summary>
		/// Returns the currently connected monitors.
		/// </summary>
		/// <remarks>
		/// This function returns an array of handles for all currently connected
		/// monitors.  The primary monitor is always first in the returned array.  If no
		/// monitors were found, this function returns `NULL`.
		/// </remarks>
		/// <param name="count">
		/// Where to store the number of monitors in the returned
		/// array.  This is set to zero if an error occurred.
		/// </param>
		/// <returns>
		/// An array of monitor handles, or `NULL` if no monitors were found or
		/// if an [error](@ref error_handling) occurred.
		/// </returns>
		public static IntPtr[] glfwGetMonitors()
		{
            var arrayPtr = _glfwGetMonitors(out int count);

            if (arrayPtr == IntPtr.Zero)
				return null;

            var result = new IntPtr[count];

            for (int i = 0; i < count; i++)
            {
                var ptr = Marshal.ReadIntPtr(arrayPtr, i * IntPtr.Size);
                result[i] = ptr;
            }

            return result;
		}

		/// <summary>
		/// Returns the primary monitor.
		/// </summary>
		/// <remarks>
		/// This function returns the primary monitor.  This is usually the monitor
		/// where elements like the task bar or global menu bar are located.
		/// </remarks>
		/// <returns>
		/// The primary monitor, or `NULL` if no monitors were found or if an
		/// [error](@ref error_handling) occurred.
		/// </returns>
		public static IntPtr glfwGetPrimaryMonitor()
		{
			return _glfwGetPrimaryMonitor();
		}

		/// <summary>
		/// Returns the position of the monitor's viewport on the virtual screen.
		/// </summary>
		/// <remarks>
		/// This function returns the position, in screen coordinates, of the upper-left
		/// corner of the specified monitor.
		/// </remarks>
		/// <param name="monitor">
		/// The monitor to query.
		/// </param>
		/// <param name="xpos">
		/// Where to store the monitor x-coordinate, or `NULL`.
		/// </param>
		/// <param name="ypos">
		/// Where to store the monitor y-coordinate, or `NULL`.
		/// </param>
		public static void glfwGetMonitorPos(IntPtr monitor, out int xpos, out int ypos)
		{
			_glfwGetMonitorPos(monitor, out xpos, out ypos);
		}

		/// <summary>
		/// Retrives the work area of the monitor.
		/// </summary>
		/// <remarks>
		/// This function returns the position, in screen coordinates, of the upper-left
		/// corner of the work area of the specified monitor along with the work area
		/// size in screen coordinates. The work area is defined as the area of the
		/// monitor not occluded by the operating system task bar where present. If no
		/// task bar exists then the work area is the monitor resolution in screen
		/// coordinates.
		/// </remarks>
		/// <param name="monitor">
		/// The monitor to query.
		/// </param>
		/// <param name="xpos">
		/// Where to store the monitor x-coordinate, or `NULL`.
		/// </param>
		/// <param name="ypos">
		/// Where to store the monitor y-coordinate, or `NULL`.
		/// </param>
		/// <param name="width">
		/// Where to store the monitor width, or `NULL`.
		/// </param>
		/// <param name="height">
		/// Where to store the monitor height, or `NULL`.
		/// </param>
		public static void glfwGetMonitorWorkarea(IntPtr monitor, out int xpos, out int ypos, out int width, out int height)
		{
			_glfwGetMonitorWorkarea(monitor, out xpos, out ypos, out width, out height);
		}

		/// <summary>
		/// Returns the physical size of the monitor.
		/// </summary>
		/// <remarks>
		/// This function returns the size, in millimetres, of the display area of the
		/// specified monitor.
		/// </remarks>
		/// <param name="monitor">
		/// The monitor to query.
		/// </param>
		/// <param name="widthMM">
		/// Where to store the width, in millimetres, of the
		/// monitor's display area, or `NULL`.
		/// </param>
		/// <param name="heightMM">
		/// Where to store the height, in millimetres, of the
		/// monitor's display area, or `NULL`.
		/// </param>
		public static void glfwGetMonitorPhysicalSize(IntPtr monitor, out int widthMM, out int heightMM)
		{
			_glfwGetMonitorPhysicalSize(monitor, out widthMM, out heightMM);
		}

		/// <summary>
		/// Retrieves the content scale for the specified monitor.
		/// </summary>
		/// <remarks>
		/// This function retrieves the content scale for the specified monitor.  The
		/// content scale is the ratio between the current DPI and the platform's
		/// default DPI.  This is especially important for text and any UI elements.  If
		/// the pixel dimensions of your UI scaled by this look appropriate on your
		/// machine then it should appear at a reasonable size on other machines
		/// regardless of their DPI and scaling settings.  This relies on the system DPI
		/// and scaling settings being somewhat correct.
		/// </remarks>
		/// <param name="monitor">
		/// The monitor to query.
		/// </param>
		/// <param name="xscale">
		/// Where to store the x-axis content scale, or `NULL`.
		/// </param>
		/// <param name="yscale">
		/// Where to store the y-axis content scale, or `NULL`.
		/// </param>
		public static void glfwGetMonitorContentScale(IntPtr monitor, out IntPtr xscale, out IntPtr yscale)
		{
			_glfwGetMonitorContentScale(monitor, out xscale, out yscale);
		}

		/// <summary>
		/// Returns the name of the specified monitor.
		/// </summary>
		/// <remarks>
		/// This function returns a human-readable name, encoded as UTF-8, of the
		/// specified monitor.  The name typically reflects the make and model of the
		/// monitor and is not guaranteed to be unique among the connected monitors.
		/// </remarks>
		/// <param name="monitor">
		/// The monitor to query.
		/// </param>
		/// <returns>
		/// The UTF-8 encoded name of the monitor, or `NULL` if an
		/// [error](@ref error_handling) occurred.
		/// </returns>
		public static IntPtr glfwGetMonitorName(IntPtr monitor)
		{
			return _glfwGetMonitorName(monitor);
		}

		/// <summary>
		/// Sets the user pointer of the specified monitor.
		/// </summary>
		/// <remarks>
		/// This function sets the user-defined pointer of the specified monitor.  The
		/// current value is retained until the monitor is disconnected.  The initial
		/// value is `NULL`.
		/// </remarks>
		/// <param name="monitor">
		/// The monitor whose pointer to set.
		/// </param>
		/// <param name="pointer">
		/// The new value.
		/// </param>
		public static void glfwSetMonitorUserPointer(IntPtr monitor, IntPtr pointer)
		{
			_glfwSetMonitorUserPointer(monitor, pointer);
		}

		/// <summary>
		/// Returns the user pointer of the specified monitor.
		/// </summary>
		/// <remarks>
		/// This function returns the current value of the user-defined pointer of the
		/// specified monitor.  The initial value is `NULL`.
		/// </remarks>
		/// <param name="monitor">
		/// The monitor whose pointer to return.
		/// </param>
		public static IntPtr glfwGetMonitorUserPointer(IntPtr monitor)
		{
			return _glfwGetMonitorUserPointer(monitor);
		}

		/// <summary>
		/// Sets the monitor configuration callback.
		/// </summary>
		/// <remarks>
		/// This function sets the monitor configuration callback, or removes the
		/// currently set callback.  This is called when a monitor is connected to or
		/// disconnected from the system.
		/// </remarks>
		/// <param name="cbfun">
		/// The new callback, or `NULL` to remove the currently set
		/// callback.
		/// </param>
		/// <returns>
		/// The previously set callback, or `NULL` if no callback was set or the
		/// library had not been [initialized](@ref intro_init).
		/// </returns>
		public static GLFWmonitorfun glfwSetMonitorCallback(GLFWmonitorfun cbfun)
		{
			return _glfwSetMonitorCallback(cbfun);
		}

		/// <summary>
		/// Returns the available video modes for the specified monitor.
		/// </summary>
		/// <remarks>
		/// This function returns an array of all video modes supported by the specified
		/// monitor.  The returned array is sorted in ascending order, first by color
		/// bit depth (the sum of all channel depths) and then by resolution area (the
		/// product of width and height).
		/// </remarks>
		/// <param name="monitor">
		/// The monitor to query.
		/// </param>
		/// <param name="count">
		/// Where to store the number of video modes in the returned
		/// array.  This is set to zero if an error occurred.
		/// </param>
		/// <returns>
		/// An array of video modes, or `NULL` if an
		/// [error](@ref error_handling) occurred.
		/// </returns>
		public static IntPtr glfwGetVideoModes(IntPtr monitor, out int count)
		{
			return _glfwGetVideoModes(monitor, out count);
		}

		/// <summary>
		/// Returns the current mode of the specified monitor.
		/// </summary>
		/// <remarks>
		/// This function returns the current video mode of the specified monitor.  If
		/// you have created a full screen window for that monitor, the return value
		/// will depend on whether that window is iconified.
		/// </remarks>
		/// <param name="monitor">
		/// The monitor to query.
		/// </param>
		/// <returns>
		/// The current mode of the monitor, or `NULL` if an
		/// [error](@ref error_handling) occurred.
		/// </returns>
		public static GLFWvidmode glfwGetVideoMode(IntPtr monitor)
		{
            var ptr = _glfwGetVideoMode(monitor);
			return Marshal.PtrToStructure<GLFWvidmode>(ptr);
		}

		/// <summary>
		/// Generates a gamma ramp and sets it for the specified monitor.
		/// </summary>
		/// <remarks>
		/// This function generates an appropriately sized gamma ramp from the specified
		/// exponent and then calls @ref glfwSetGammaRamp with it.  The value must be
		/// a finite number greater than zero.
		/// </remarks>
		/// <param name="monitor">
		/// The monitor whose gamma ramp to set.
		/// </param>
		/// <param name="gamma">
		/// The desired exponent.
		/// </param>
		public static void glfwSetGamma(IntPtr monitor, float gamma)
		{
			_glfwSetGamma(monitor, gamma);
		}

		/// <summary>
		/// Returns the current gamma ramp for the specified monitor.
		/// </summary>
		/// <remarks>
		/// This function returns the current gamma ramp of the specified monitor.
		/// </remarks>
		/// <param name="monitor">
		/// The monitor to query.
		/// </param>
		/// <returns>
		/// The current gamma ramp, or `NULL` if an
		/// [error](@ref error_handling) occurred.
		/// </returns>
		public static GLFWgammaramp glfwGetGammaRamp(IntPtr monitor)
		{
			var structPtr = _glfwGetGammaRamp(monitor);

			var redArrayPtr = Marshal.ReadIntPtr(structPtr);
			var blueArrayPtr = Marshal.ReadIntPtr(IntPtr.Add(structPtr, IntPtr.Size));
			var greenArrayPtr = Marshal.ReadIntPtr(IntPtr.Add(structPtr, IntPtr.Size * 2));
			var size = (uint)Marshal.ReadInt32(IntPtr.Add(structPtr, IntPtr.Size * 3));
			
			var result = new GLFWgammaramp()
			{
				size = size,
				red = new ushort[size],
				green = new ushort[size],
				blue = new ushort[size],
			};

			int uintSize = Marshal.SizeOf(typeof(uint));

			for (int i = 0; i < size; i++)
			{
				result.red[i] = (ushort)Marshal.ReadInt16(IntPtr.Add(redArrayPtr, uintSize * i));
				result.blue[i] = (ushort)Marshal.ReadInt16(IntPtr.Add(blueArrayPtr, uintSize * i));
				result.green[i] = (ushort)Marshal.ReadInt16(IntPtr.Add(greenArrayPtr, uintSize * i));
			}

			return result;
		}

		/// <summary>
		/// Sets the current gamma ramp for the specified monitor.
		/// </summary>
		/// <remarks>
		/// This function sets the current gamma ramp for the specified monitor.  The
		/// original gamma ramp for that monitor is saved by GLFW the first time this
		/// function is called and is restored by @ref glfwTerminate.
		/// </remarks>
		/// <param name="monitor">
		/// The monitor whose gamma ramp to set.
		/// </param>
		/// <param name="ramp">
		/// The gamma ramp to use.
		/// </param>
		public static void glfwSetGammaRamp(IntPtr monitor, IntPtr ramp)
		{
			_glfwSetGammaRamp(monitor, ramp);
		}

		/// <summary>
		/// Resets all window hints to their default values.
		/// </summary>
		/// <remarks>
		/// This function resets all window hints to their
		/// [default values](@ref window_hints_values).
		/// </remarks>
		public static void glfwDefaultWindowHints()
		{
			_glfwDefaultWindowHints();
		}

		/// <summary>
		/// Sets the specified window hint to the desired value.
		/// </summary>
		/// <remarks>
		/// This function sets hints for the next call to @ref glfwCreateWindow.  The
		/// hints, once set, retain their values until changed by a call to this
		/// function or @ref glfwDefaultWindowHints, or until the library is terminated.
		/// </remarks>
		/// <param name="hint">
		/// The [window hint](@ref window_hints) to set.
		/// </param>
		/// <param name="value">
		/// The new value of the window hint.
		/// </param>
		public static void glfwWindowHint(int hint, int value)
		{
			_glfwWindowHint(hint, value);
		}

		/// <summary>
		/// Sets the specified window hint to the desired value.
		/// </summary>
		/// <remarks>
		/// This function sets hints for the next call to @ref glfwCreateWindow.  The
		/// hints, once set, retain their values until changed by a call to this
		/// function or @ref glfwDefaultWindowHints, or until the library is terminated.
		/// </remarks>
		/// <param name="hint">
		/// The [window hint](@ref window_hints) to set.
		/// </param>
		/// <param name="value">
		/// The new value of the window hint.
		/// </param>
		public static void glfwWindowHintString(int hint, string value)
		{
			_glfwWindowHintString(hint, value);
		}

		/// <summary>
		/// Creates a window and its associated context.
		/// </summary>
		/// <remarks>
		/// This function creates a window and its associated OpenGL or OpenGL ES
		/// context.  Most of the options controlling how the window and its context
		/// should be created are specified with [window hints](@ref window_hints).
		/// </remarks>
		/// <param name="width">
		/// The desired width, in screen coordinates, of the window.
		/// This must be greater than zero.
		/// </param>
		/// <param name="height">
		/// The desired height, in screen coordinates, of the window.
		/// This must be greater than zero.
		/// </param>
		/// <param name="title">
		/// The initial, UTF-8 encoded window title.
		/// </param>
		/// <param name="monitor">
		/// The monitor to use for full screen mode, or `NULL` for
		/// windowed mode.
		/// </param>
		/// <param name="share">
		/// The window whose context to share resources with, or `NULL`
		/// to not share resources.
		/// </param>
		/// <returns>
		/// The handle of the created window, or `NULL` if an
		/// [error](@ref error_handling) occurred.
		/// </returns>
		public unsafe static IntPtr glfwCreateWindow(int width, int height, string title, IntPtr monitor, IntPtr share)
		{
            var bytes = System.Text.Encoding.UTF8.GetBytes(title + "\0");
            fixed (byte* p = &bytes[0])
				return _glfwCreateWindow(width, height, (IntPtr)p, monitor, share);
		}

		/// <summary>
		/// Destroys the specified window and its context.
		/// </summary>
		/// <remarks>
		/// This function destroys the specified window and its context.  On calling
		/// this function, no further callbacks will be called for that window.
		/// </remarks>
		/// <param name="window">
		/// The window to destroy.
		/// </param>
		public static void glfwDestroyWindow(IntPtr window)
		{
			_glfwDestroyWindow(window);
		}

		/// <summary>
		/// Checks the close flag of the specified window.
		/// </summary>
		/// <remarks>
		/// This function returns the value of the close flag of the specified window.
		/// </remarks>
		/// <param name="window">
		/// The window to query.
		/// </param>
		/// <returns>
		/// The value of the close flag.
		/// </returns>
		public static int glfwWindowShouldClose(IntPtr window)
		{
			return _glfwWindowShouldClose(window);
		}

		/// <summary>
		/// Sets the close flag of the specified window.
		/// </summary>
		/// <remarks>
		/// This function sets the value of the close flag of the specified window.
		/// This can be used to override the user's attempt to close the window, or
		/// to signal that it should be closed.
		/// </remarks>
		/// <param name="window">
		/// The window whose flag to change.
		/// </param>
		/// <param name="value">
		/// The new value.
		/// </param>
		public static void glfwSetWindowShouldClose(IntPtr window, int value)
		{
			_glfwSetWindowShouldClose(window, value);
		}

		/// <summary>
		/// Sets the title of the specified window.
		/// </summary>
		/// <remarks>
		/// This function sets the window title, encoded as UTF-8, of the specified
		/// window.
		/// </remarks>
		/// <param name="window">
		/// The window whose title to change.
		/// </param>
		/// <param name="title">
		/// The UTF-8 encoded window title.
		/// </param>
		public unsafe static void glfwSetWindowTitle(IntPtr window, string title)
		{
            var bytes = System.Text.Encoding.UTF8.GetBytes(title + "\0");
			fixed (byte* p = &bytes[0])
                _glfwSetWindowTitle(window, (IntPtr)p);
		}

		/// <summary>
		/// Sets the icon for the specified window.
		/// </summary>
		/// <remarks>
		/// This function sets the icon of the specified window.  If passed an array of
		/// candidate images, those of or closest to the sizes desired by the system are
		/// selected.  If no images are specified, the window reverts to its default
		/// icon.
		/// </remarks>
		/// <param name="window">
		/// The window whose icon to set.
		/// </param>
		/// <param name="count">
		/// The number of images in the specified array, or zero to
		/// revert to the default window icon.
		/// </param>
		/// <param name="images">
		/// The images to create the icon from.  This is ignored if
		/// count is zero.
		/// </param>
		public static void glfwSetWindowIcon(IntPtr window, int count, IntPtr images)
		{
			_glfwSetWindowIcon(window, count, images);
		}

		/// <summary>
		/// Retrieves the position of the content area of the specified window.
		/// </summary>
		/// <remarks>
		/// This function retrieves the position, in screen coordinates, of the
		/// upper-left corner of the content area of the specified window.
		/// </remarks>
		/// <param name="window">
		/// The window to query.
		/// </param>
		/// <param name="xpos">
		/// Where to store the x-coordinate of the upper-left corner of
		/// the content area, or `NULL`.
		/// </param>
		/// <param name="ypos">
		/// Where to store the y-coordinate of the upper-left corner of
		/// the content area, or `NULL`.
		/// </param>
		public static void glfwGetWindowPos(IntPtr window, out int xpos, out int ypos)
		{
			_glfwGetWindowPos(window, out xpos, out ypos);
		}

		/// <summary>
		/// Sets the position of the content area of the specified window.
		/// </summary>
		/// <remarks>
		/// This function sets the position, in screen coordinates, of the upper-left
		/// corner of the content area of the specified windowed mode window.  If the
		/// window is a full screen window, this function does nothing.
		/// </remarks>
		/// <param name="window">
		/// The window to query.
		/// </param>
		/// <param name="xpos">
		/// The x-coordinate of the upper-left corner of the content area.
		/// </param>
		/// <param name="ypos">
		/// The y-coordinate of the upper-left corner of the content area.
		/// </param>
		public static void glfwSetWindowPos(IntPtr window, int xpos, int ypos)
		{
			_glfwSetWindowPos(window, xpos, ypos);
		}

		/// <summary>
		/// Retrieves the size of the content area of the specified window.
		/// </summary>
		/// <remarks>
		/// This function retrieves the size, in screen coordinates, of the content area
		/// of the specified window.  If you wish to retrieve the size of the
		/// framebuffer of the window in pixels, see @ref glfwGetFramebufferSize.
		/// </remarks>
		/// <param name="window">
		/// The window whose size to retrieve.
		/// </param>
		/// <param name="width">
		/// Where to store the width, in screen coordinates, of the
		/// content area, or `NULL`.
		/// </param>
		/// <param name="height">
		/// Where to store the height, in screen coordinates, of the
		/// content area, or `NULL`.
		/// </param>
		public static void glfwGetWindowSize(IntPtr window, out int width, out int height)
		{
			_glfwGetWindowSize(window, out width, out height);
		}

		/// <summary>
		/// Sets the size limits of the specified window.
		/// </summary>
		/// <remarks>
		/// This function sets the size limits of the content area of the specified
		/// window.  If the window is full screen, the size limits only take effect
		/// once it is made windowed.  If the window is not resizable, this function
		/// does nothing.
		/// </remarks>
		/// <param name="window">
		/// The window to set limits for.
		/// </param>
		/// <param name="minwidth">
		/// The minimum width, in screen coordinates, of the content
		/// area, or `GLFW_DONT_CARE`.
		/// </param>
		/// <param name="minheight">
		/// The minimum height, in screen coordinates, of the
		/// content area, or `GLFW_DONT_CARE`.
		/// </param>
		/// <param name="maxwidth">
		/// The maximum width, in screen coordinates, of the content
		/// area, or `GLFW_DONT_CARE`.
		/// </param>
		/// <param name="maxheight">
		/// The maximum height, in screen coordinates, of the
		/// content area, or `GLFW_DONT_CARE`.
		/// </param>
		public static void glfwSetWindowSizeLimits(IntPtr window, int minwidth, int minheight, int maxwidth, int maxheight)
		{
			_glfwSetWindowSizeLimits(window, minwidth, minheight, maxwidth, maxheight);
		}

		/// <summary>
		/// Sets the aspect ratio of the specified window.
		/// </summary>
		/// <remarks>
		/// This function sets the required aspect ratio of the content area of the
		/// specified window.  If the window is full screen, the aspect ratio only takes
		/// effect once it is made windowed.  If the window is not resizable, this
		/// function does nothing.
		/// </remarks>
		/// <param name="window">
		/// The window to set limits for.
		/// </param>
		/// <param name="numer">
		/// The numerator of the desired aspect ratio, or
		/// `GLFW_DONT_CARE`.
		/// </param>
		/// <param name="denom">
		/// The denominator of the desired aspect ratio, or
		/// `GLFW_DONT_CARE`.
		/// </param>
		public static void glfwSetWindowAspectRatio(IntPtr window, int numer, int denom)
		{
			_glfwSetWindowAspectRatio(window, numer, denom);
		}

		/// <summary>
		/// Sets the size of the content area of the specified window.
		/// </summary>
		/// <remarks>
		/// This function sets the size, in screen coordinates, of the content area of
		/// the specified window.
		/// </remarks>
		/// <param name="window">
		/// The window to resize.
		/// </param>
		/// <param name="width">
		/// The desired width, in screen coordinates, of the window
		/// content area.
		/// </param>
		/// <param name="height">
		/// The desired height, in screen coordinates, of the window
		/// content area.
		/// </param>
		public static void glfwSetWindowSize(IntPtr window, int width, int height)
		{
			_glfwSetWindowSize(window, width, height);
		}

		/// <summary>
		/// Retrieves the size of the framebuffer of the specified window.
		/// </summary>
		/// <remarks>
		/// This function retrieves the size, in pixels, of the framebuffer of the
		/// specified window.  If you wish to retrieve the size of the window in screen
		/// coordinates, see @ref glfwGetWindowSize.
		/// </remarks>
		/// <param name="window">
		/// The window whose framebuffer to query.
		/// </param>
		/// <param name="width">
		/// Where to store the width, in pixels, of the framebuffer,
		/// or `NULL`.
		/// </param>
		/// <param name="height">
		/// Where to store the height, in pixels, of the framebuffer,
		/// or `NULL`.
		/// </param>
		public static void glfwGetFramebufferSize(IntPtr window, out int width, out int height)
		{
			_glfwGetFramebufferSize(window, out width, out height);
		}

		/// <summary>
		/// Retrieves the size of the frame of the window.
		/// </summary>
		/// <remarks>
		/// This function retrieves the size, in screen coordinates, of each edge of the
		/// frame of the specified window.  This size includes the title bar, if the
		/// window has one.  The size of the frame may vary depending on the
		/// [window-related hints](@ref window_hints_wnd) used to create it.
		/// </remarks>
		/// <param name="window">
		/// The window whose frame size to query.
		/// </param>
		/// <param name="left">
		/// Where to store the size, in screen coordinates, of the left
		/// edge of the window frame, or `NULL`.
		/// </param>
		/// <param name="top">
		/// Where to store the size, in screen coordinates, of the top
		/// edge of the window frame, or `NULL`.
		/// </param>
		/// <param name="right">
		/// Where to store the size, in screen coordinates, of the
		/// right edge of the window frame, or `NULL`.
		/// </param>
		/// <param name="bottom">
		/// Where to store the size, in screen coordinates, of the
		/// bottom edge of the window frame, or `NULL`.
		/// </param>
		public static void glfwGetWindowFrameSize(IntPtr window, out int left, out int top, out int right, out int bottom)
		{
			_glfwGetWindowFrameSize(window, out left, out top, out right, out bottom);
		}

		/// <summary>
		/// Retrieves the content scale for the specified window.
		/// </summary>
		/// <remarks>
		/// This function retrieves the content scale for the specified window.  The
		/// content scale is the ratio between the current DPI and the platform's
		/// default DPI.  This is especially important for text and any UI elements.  If
		/// the pixel dimensions of your UI scaled by this look appropriate on your
		/// machine then it should appear at a reasonable size on other machines
		/// regardless of their DPI and scaling settings.  This relies on the system DPI
		/// and scaling settings being somewhat correct.
		/// </remarks>
		/// <param name="window">
		/// The window to query.
		/// </param>
		/// <param name="xscale">
		/// Where to store the x-axis content scale, or `NULL`.
		/// </param>
		/// <param name="yscale">
		/// Where to store the y-axis content scale, or `NULL`.
		/// </param>
		public static void glfwGetWindowContentScale(IntPtr window, out float xscale, out float yscale)
		{
			_glfwGetWindowContentScale(window, out xscale, out yscale);
		}

		/// <summary>
		/// Returns the opacity of the whole window.
		/// </summary>
		/// <remarks>
		/// This function returns the opacity of the window, including any decorations.
		/// </remarks>
		/// <param name="window">
		/// The window to query.
		/// </param>
		/// <returns>
		/// The opacity value of the specified window.
		/// </returns>
		public static float glfwGetWindowOpacity(IntPtr window)
		{
			return _glfwGetWindowOpacity(window);
		}

		/// <summary>
		/// Sets the opacity of the whole window.
		/// </summary>
		/// <remarks>
		/// This function sets the opacity of the window, including any decorations.
		/// </remarks>
		/// <param name="window">
		/// The window to set the opacity for.
		/// </param>
		/// <param name="opacity">
		/// The desired opacity of the specified window.
		/// </param>
		public static void glfwSetWindowOpacity(IntPtr window, float opacity)
		{
			_glfwSetWindowOpacity(window, opacity);
		}

		/// <summary>
		/// Iconifies the specified window.
		/// </summary>
		/// <remarks>
		/// This function iconifies (minimizes) the specified window if it was
		/// previously restored.  If the window is already iconified, this function does
		/// nothing.
		/// </remarks>
		/// <param name="window">
		/// The window to iconify.
		/// </param>
		public static void glfwIconifyWindow(IntPtr window)
		{
			_glfwIconifyWindow(window);
		}

		/// <summary>
		/// Restores the specified window.
		/// </summary>
		/// <remarks>
		/// This function restores the specified window if it was previously iconified
		/// (minimized) or maximized.  If the window is already restored, this function
		/// does nothing.
		/// </remarks>
		/// <param name="window">
		/// The window to restore.
		/// </param>
		public static void glfwRestoreWindow(IntPtr window)
		{
			_glfwRestoreWindow(window);
		}

		/// <summary>
		/// Maximizes the specified window.
		/// </summary>
		/// <remarks>
		/// This function maximizes the specified window if it was previously not
		/// maximized.  If the window is already maximized, this function does nothing.
		/// </remarks>
		/// <param name="window">
		/// The window to maximize.
		/// </param>
		public static void glfwMaximizeWindow(IntPtr window)
		{
			_glfwMaximizeWindow(window);
		}

		/// <summary>
		/// Makes the specified window visible.
		/// </summary>
		/// <remarks>
		/// This function makes the specified window visible if it was previously
		/// hidden.  If the window is already visible or is in full screen mode, this
		/// function does nothing.
		/// </remarks>
		/// <param name="window">
		/// The window to make visible.
		/// </param>
		public static void glfwShowWindow(IntPtr window)
		{
			_glfwShowWindow(window);
		}

		/// <summary>
		/// Hides the specified window.
		/// </summary>
		/// <remarks>
		/// This function hides the specified window if it was previously visible.  If
		/// the window is already hidden or is in full screen mode, this function does
		/// nothing.
		/// </remarks>
		/// <param name="window">
		/// The window to hide.
		/// </param>
		public static void glfwHideWindow(IntPtr window)
		{
			_glfwHideWindow(window);
		}

		/// <summary>
		/// Brings the specified window to front and sets input focus.
		/// </summary>
		/// <remarks>
		/// This function brings the specified window to front and sets input focus.
		/// The window should already be visible and not iconified.
		/// </remarks>
		/// <param name="window">
		/// The window to give input focus.
		/// </param>
		public static void glfwFocusWindow(IntPtr window)
		{
			_glfwFocusWindow(window);
		}

		/// <summary>
		/// Requests user attention to the specified window.
		/// </summary>
		/// <remarks>
		/// This function requests user attention to the specified window.  On
		/// platforms where this is not supported, attention is requested to the
		/// application as a whole.
		/// </remarks>
		/// <param name="window">
		/// The window to request attention to.
		/// </param>
		public static void glfwRequestWindowAttention(IntPtr window)
		{
			_glfwRequestWindowAttention(window);
		}

		/// <summary>
		/// Returns the monitor that the window uses for full screen mode.
		/// </summary>
		/// <remarks>
		/// This function returns the handle of the monitor that the specified window is
		/// in full screen on.
		/// </remarks>
		/// <param name="window">
		/// The window to query.
		/// </param>
		/// <returns>
		/// The monitor, or `NULL` if the window is in windowed mode or an
		/// [error](@ref error_handling) occurred.
		/// </returns>
		public static IntPtr glfwGetWindowMonitor(IntPtr window)
		{
			return _glfwGetWindowMonitor(window);
		}

		/// <summary>
		/// Sets the mode, monitor, video mode and placement of a window.
		/// </summary>
		/// <remarks>
		/// This function sets the monitor that the window uses for full screen mode or,
		/// if the monitor is `NULL`, makes it windowed mode.
		/// </remarks>
		/// <param name="window">
		/// The window whose monitor, size or video mode to set.
		/// </param>
		/// <param name="monitor">
		/// The desired monitor, or `NULL` to set windowed mode.
		/// </param>
		/// <param name="xpos">
		/// The desired x-coordinate of the upper-left corner of the
		/// content area.
		/// </param>
		/// <param name="ypos">
		/// The desired y-coordinate of the upper-left corner of the
		/// content area.
		/// </param>
		/// <param name="width">
		/// The desired with, in screen coordinates, of the content
		/// area or video mode.
		/// </param>
		/// <param name="height">
		/// The desired height, in screen coordinates, of the content
		/// area or video mode.
		/// </param>
		/// <param name="refreshRate">
		/// The desired refresh rate, in Hz, of the video mode,
		/// or `GLFW_DONT_CARE`.
		/// </param>
		public static void glfwSetWindowMonitor(IntPtr window, IntPtr monitor, int xpos, int ypos, int width, int height, int refreshRate)
		{
			_glfwSetWindowMonitor(window, monitor, xpos, ypos, width, height, refreshRate);
		}

		/// <summary>
		/// Returns an attribute of the specified window.
		/// </summary>
		/// <remarks>
		/// This function returns the value of an attribute of the specified window or
		/// its OpenGL or OpenGL ES context.
		/// </remarks>
		/// <param name="window">
		/// The window to query.
		/// </param>
		/// <param name="attrib">
		/// The [window attribute](@ref window_attribs) whose value to
		/// return.
		/// </param>
		/// <returns>
		/// The value of the attribute, or zero if an
		/// [error](@ref error_handling) occurred.
		/// </returns>
		public static int glfwGetWindowAttrib(IntPtr window, int attrib)
		{
			return _glfwGetWindowAttrib(window, attrib);
		}

		/// <summary>
		/// Sets an attribute of the specified window.
		/// </summary>
		/// <remarks>
		/// This function sets the value of an attribute of the specified window.
		/// </remarks>
		/// <param name="window">
		/// The window to set the attribute for.
		/// </param>
		/// <param name="attrib">
		/// A supported window attribute.
		/// </param>
		/// <param name="value">
		/// `GLFW_TRUE` or `GLFW_FALSE`.
		/// </param>
		public static void glfwSetWindowAttrib(IntPtr window, int attrib, int value)
		{
			_glfwSetWindowAttrib(window, attrib, value);
		}

		/// <summary>
		/// Sets the user pointer of the specified window.
		/// </summary>
		/// <remarks>
		/// This function sets the user-defined pointer of the specified window.  The
		/// current value is retained until the window is destroyed.  The initial value
		/// is `NULL`.
		/// </remarks>
		/// <param name="window">
		/// The window whose pointer to set.
		/// </param>
		/// <param name="pointer">
		/// The new value.
		/// </param>
		public static void glfwSetWindowUserPointer(IntPtr window, IntPtr pointer)
		{
			_glfwSetWindowUserPointer(window, pointer);
		}

		/// <summary>
		/// Returns the user pointer of the specified window.
		/// </summary>
		/// <remarks>
		/// This function returns the current value of the user-defined pointer of the
		/// specified window.  The initial value is `NULL`.
		/// </remarks>
		/// <param name="window">
		/// The window whose pointer to return.
		/// </param>
		public static IntPtr glfwGetWindowUserPointer(IntPtr window)
		{
			return _glfwGetWindowUserPointer(window);
		}

		/// <summary>
		/// Sets the position callback for the specified window.
		/// </summary>
		/// <remarks>
		/// This function sets the position callback of the specified window, which is
		/// called when the window is moved.  The callback is provided with the
		/// position, in screen coordinates, of the upper-left corner of the content
		/// area of the window.
		/// </remarks>
		/// <param name="window">
		/// The window whose callback to set.
		/// </param>
		/// <param name="cbfun">
		/// The new callback, or `NULL` to remove the currently set
		/// callback.
		/// </param>
		/// <returns>
		/// The previously set callback, or `NULL` if no callback was set or the
		/// library had not been [initialized](@ref intro_init).
		/// </returns>
		public static GLFWwindowposfun glfwSetWindowPosCallback(IntPtr window, GLFWwindowposfun cbfun)
		{
			return _glfwSetWindowPosCallback(window, cbfun);
		}

		/// <summary>
		/// Sets the size callback for the specified window.
		/// </summary>
		/// <remarks>
		/// This function sets the size callback of the specified window, which is
		/// called when the window is resized.  The callback is provided with the size,
		/// in screen coordinates, of the content area of the window.
		/// </remarks>
		/// <param name="window">
		/// The window whose callback to set.
		/// </param>
		/// <param name="cbfun">
		/// The new callback, or `NULL` to remove the currently set
		/// callback.
		/// </param>
		/// <returns>
		/// The previously set callback, or `NULL` if no callback was set or the
		/// library had not been [initialized](@ref intro_init).
		/// </returns>
		public static GLFWwindowsizefun glfwSetWindowSizeCallback(IntPtr window, GLFWwindowsizefun cbfun)
		{
			return _glfwSetWindowSizeCallback(window, cbfun);
		}

		/// <summary>
		/// Sets the close callback for the specified window.
		/// </summary>
		/// <remarks>
		/// This function sets the close callback of the specified window, which is
		/// called when the user attempts to close the window, for example by clicking
		/// the close widget in the title bar.
		/// </remarks>
		/// <param name="window">
		/// The window whose callback to set.
		/// </param>
		/// <param name="cbfun">
		/// The new callback, or `NULL` to remove the currently set
		/// callback.
		/// </param>
		/// <returns>
		/// The previously set callback, or `NULL` if no callback was set or the
		/// library had not been [initialized](@ref intro_init).
		/// </returns>
		public static GLFWwindowclosefun glfwSetWindowCloseCallback(IntPtr window, GLFWwindowclosefun cbfun)
		{
			return _glfwSetWindowCloseCallback(window, cbfun);
		}

		/// <summary>
		/// Sets the refresh callback for the specified window.
		/// </summary>
		/// <remarks>
		/// This function sets the refresh callback of the specified window, which is
		/// called when the content area of the window needs to be redrawn, for example
		/// if the window has been exposed after having been covered by another window.
		/// </remarks>
		/// <param name="window">
		/// The window whose callback to set.
		/// </param>
		/// <param name="cbfun">
		/// The new callback, or `NULL` to remove the currently set
		/// callback.
		/// </param>
		/// <returns>
		/// The previously set callback, or `NULL` if no callback was set or the
		/// library had not been [initialized](@ref intro_init).
		/// </returns>
		public static GLFWwindowrefreshfun glfwSetWindowRefreshCallback(IntPtr window, GLFWwindowrefreshfun cbfun)
		{
			return _glfwSetWindowRefreshCallback(window, cbfun);
		}

		/// <summary>
		/// Sets the focus callback for the specified window.
		/// </summary>
		/// <remarks>
		/// This function sets the focus callback of the specified window, which is
		/// called when the window gains or loses input focus.
		/// </remarks>
		/// <param name="window">
		/// The window whose callback to set.
		/// </param>
		/// <param name="cbfun">
		/// The new callback, or `NULL` to remove the currently set
		/// callback.
		/// </param>
		/// <returns>
		/// The previously set callback, or `NULL` if no callback was set or the
		/// library had not been [initialized](@ref intro_init).
		/// </returns>
		public static GLFWwindowfocusfun glfwSetWindowFocusCallback(IntPtr window, GLFWwindowfocusfun cbfun)
		{
			return _glfwSetWindowFocusCallback(window, cbfun);
		}

		/// <summary>
		/// Sets the iconify callback for the specified window.
		/// </summary>
		/// <remarks>
		/// This function sets the iconification callback of the specified window, which
		/// is called when the window is iconified or restored.
		/// </remarks>
		/// <param name="window">
		/// The window whose callback to set.
		/// </param>
		/// <param name="cbfun">
		/// The new callback, or `NULL` to remove the currently set
		/// callback.
		/// </param>
		/// <returns>
		/// The previously set callback, or `NULL` if no callback was set or the
		/// library had not been [initialized](@ref intro_init).
		/// </returns>
		public static GLFWwindowiconifyfun glfwSetWindowIconifyCallback(IntPtr window, GLFWwindowiconifyfun cbfun)
		{
			return _glfwSetWindowIconifyCallback(window, cbfun);
		}

		/// <summary>
		/// Sets the maximize callback for the specified window.
		/// </summary>
		/// <remarks>
		/// This function sets the maximization callback of the specified window, which
		/// is called when the window is maximized or restored.
		/// </remarks>
		/// <param name="window">
		/// The window whose callback to set.
		/// </param>
		/// <param name="cbfun">
		/// The new callback, or `NULL` to remove the currently set
		/// callback.
		/// </param>
		/// <returns>
		/// The previously set callback, or `NULL` if no callback was set or the
		/// library had not been [initialized](@ref intro_init).
		/// </returns>
		public static GLFWwindowmaximizefun glfwSetWindowMaximizeCallback(IntPtr window, GLFWwindowmaximizefun cbfun)
		{
			return _glfwSetWindowMaximizeCallback(window, cbfun);
		}

		/// <summary>
		/// Sets the framebuffer resize callback for the specified window.
		/// </summary>
		/// <remarks>
		/// This function sets the framebuffer resize callback of the specified window,
		/// which is called when the framebuffer of the specified window is resized.
		/// </remarks>
		/// <param name="window">
		/// The window whose callback to set.
		/// </param>
		/// <param name="cbfun">
		/// The new callback, or `NULL` to remove the currently set
		/// callback.
		/// </param>
		/// <returns>
		/// The previously set callback, or `NULL` if no callback was set or the
		/// library had not been [initialized](@ref intro_init).
		/// </returns>
		public static GLFWframebuffersizefun glfwSetFramebufferSizeCallback(IntPtr window, GLFWframebuffersizefun cbfun)
		{
			return _glfwSetFramebufferSizeCallback(window, cbfun);
		}

		/// <summary>
		/// Sets the window content scale callback for the specified window.
		/// </summary>
		/// <remarks>
		/// This function sets the window content scale callback of the specified window,
		/// which is called when the content scale of the specified window changes.
		/// </remarks>
		/// <param name="window">
		/// The window whose callback to set.
		/// </param>
		/// <param name="cbfun">
		/// The new callback, or `NULL` to remove the currently set
		/// callback.
		/// </param>
		/// <returns>
		/// The previously set callback, or `NULL` if no callback was set or the
		/// library had not been [initialized](@ref intro_init).
		/// </returns>
		public static GLFWwindowcontentscalefun glfwSetWindowContentScaleCallback(IntPtr window, GLFWwindowcontentscalefun cbfun)
		{
			return _glfwSetWindowContentScaleCallback(window, cbfun);
		}

		/// <summary>
		/// Processes all pending events.
		/// </summary>
		/// <remarks>
		/// This function processes only those events that are already in the event
		/// queue and then returns immediately.  Processing events will cause the window
		/// and input callbacks associated with those events to be called.
		/// </remarks>
		public static void glfwPollEvents()
		{
			_glfwPollEvents();
		}

		/// <summary>
		/// Waits until events are queued and processes them.
		/// </summary>
		/// <remarks>
		/// This function puts the calling thread to sleep until at least one event is
		/// available in the event queue.  Once one or more events are available,
		/// it behaves exactly like @ref glfwPollEvents, i.e. the events in the queue
		/// are processed and the function then returns immediately.  Processing events
		/// will cause the window and input callbacks associated with those events to be
		/// called.
		/// </remarks>
		public static void glfwWaitEvents()
		{
			_glfwWaitEvents();
		}

		/// <summary>
		/// Waits with timeout until events are queued and processes them.
		/// </summary>
		/// <remarks>
		/// This function puts the calling thread to sleep until at least one event is
		/// available in the event queue, or until the specified timeout is reached.  If
		/// one or more events are available, it behaves exactly like @ref
		/// glfwPollEvents, i.e. the events in the queue are processed and the function
		/// then returns immediately.  Processing events will cause the window and input
		/// callbacks associated with those events to be called.
		/// </remarks>
		/// <param name="timeout">
		/// The maximum amount of time, in seconds, to wait.
		/// </param>
		public static void glfwWaitEventsTimeout(double timeout)
		{
			_glfwWaitEventsTimeout(timeout);
		}

		/// <summary>
		/// Posts an empty event to the event queue.
		/// </summary>
		/// <remarks>
		/// This function posts an empty event from the current thread to the event
		/// queue, causing @ref glfwWaitEvents or @ref glfwWaitEventsTimeout to return.
		/// </remarks>
		public static void glfwPostEmptyEvent()
		{
			_glfwPostEmptyEvent();
		}

		/// <summary>
		/// Returns the value of an input option for the specified window.
		/// </summary>
		/// <remarks>
		/// This function returns the value of an input option for the specified window.
		/// The mode must be one of @ref GLFW_CURSOR, @ref GLFW_STICKY_KEYS,
		/// </remarks>
		/// <param name="window">
		/// The window to query.
		/// </param>
		/// <param name="mode">
		/// One of `GLFW_CURSOR`, `GLFW_STICKY_KEYS`,
		/// `GLFW_STICKY_MOUSE_BUTTONS`, `GLFW_LOCK_KEY_MODS` or
		/// `GLFW_RAW_MOUSE_MOTION`.
		/// </param>
		public static int glfwGetInputMode(IntPtr window, int mode)
		{
			return _glfwGetInputMode(window, mode);
		}

		/// <summary>
		/// Sets an input option for the specified window.
		/// </summary>
		/// <remarks>
		/// This function sets an input mode option for the specified window.  The mode
		/// must be one of @ref GLFW_CURSOR, @ref GLFW_STICKY_KEYS,
		/// </remarks>
		/// <param name="window">
		/// The window whose input mode to set.
		/// </param>
		/// <param name="mode">
		/// One of `GLFW_CURSOR`, `GLFW_STICKY_KEYS`,
		/// `GLFW_STICKY_MOUSE_BUTTONS`, `GLFW_LOCK_KEY_MODS` or
		/// `GLFW_RAW_MOUSE_MOTION`.
		/// </param>
		/// <param name="value">
		/// The new value of the specified input mode.
		/// </param>
		public static void glfwSetInputMode(IntPtr window, int mode, int value)
		{
			_glfwSetInputMode(window, mode, value);
		}

		/// <summary>
		/// Returns whether raw mouse motion is supported.
		/// </summary>
		/// <remarks>
		/// This function returns whether raw mouse motion is supported on the current
		/// system.  This status does not change after GLFW has been initialized so you
		/// only need to check this once.  If you attempt to enable raw motion on
		/// a system that does not support it, @ref GLFW_PLATFORM_ERROR will be emitted.
		/// </remarks>
		/// <returns>
		/// `GLFW_TRUE` if raw mouse motion is supported on the current machine,
		/// or `GLFW_FALSE` otherwise.
		/// </returns>
		public static int glfwRawMouseMotionSupported()
		{
			return _glfwRawMouseMotionSupported();
		}

		private static unsafe string PtrToStringUTF8(IntPtr ptr)
        {
			if (ptr == IntPtr.Zero)
				return null;

			var p = (byte*)ptr.ToPointer();
			var n = 0;
			for (; p[n] != 0; n++) ;

			return System.Text.Encoding.UTF8.GetString(p, n);
		}


		/// <summary>
		/// Returns the layout-specific name of the specified printable key.
		/// </summary>
		/// <remarks>
		/// This function returns the name of the specified printable key, encoded as
		/// UTF-8.  This is typically the character that key would produce without any
		/// modifier keys, intended for displaying key bindings to the user.  For dead
		/// keys, it is typically the diacritic it would add to a character.
		/// </remarks>
		/// <param name="key">
		/// The key to query, or `GLFW_KEY_UNKNOWN`.
		/// </param>
		/// <param name="scancode">
		/// The scancode of the key to query.
		/// </param>
		/// <returns>
		/// The UTF-8 encoded, layout-specific name of the key, or `NULL`.
		/// </returns>
		public static unsafe string glfwGetKeyName(int key, int scancode)
		{
			return PtrToStringUTF8(_glfwGetKeyName(key, scancode));
		}

		/// <summary>
		/// Returns the platform-specific scancode of the specified key.
		/// </summary>
		/// <remarks>
		/// This function returns the platform-specific scancode of the specified key.
		/// </remarks>
		/// <param name="key">
		/// Any [named key](@ref keys).
		/// </param>
		/// <returns>
		/// The platform-specific scancode for the key, or `-1` if an
		/// [error](@ref error_handling) occurred.
		/// </returns>
		public static int glfwGetKeyScancode(int key)
		{
			return _glfwGetKeyScancode(key);
		}

		/// <summary>
		/// Returns the last reported state of a keyboard key for the specified
		/// window.
		/// </summary>
		/// <remarks>
		/// This function returns the last state reported for the specified key to the
		/// specified window.  The returned state is one of `GLFW_PRESS` or
		/// `GLFW_RELEASE`.  The higher-level action `GLFW_REPEAT` is only reported to
		/// the key callback.
		/// </remarks>
		/// <param name="window">
		/// The desired window.
		/// </param>
		/// <param name="key">
		/// The desired [keyboard key](@ref keys).  `GLFW_KEY_UNKNOWN` is
		/// not a valid key for this function.
		/// </param>
		/// <returns>
		/// One of `GLFW_PRESS` or `GLFW_RELEASE`.
		/// </returns>
		public static int glfwGetKey(IntPtr window, int key)
		{
			return _glfwGetKey(window, key);
		}

		/// <summary>
		/// Returns the last reported state of a mouse button for the specified
		/// window.
		/// </summary>
		/// <remarks>
		/// This function returns the last state reported for the specified mouse button
		/// to the specified window.  The returned state is one of `GLFW_PRESS` or
		/// `GLFW_RELEASE`.
		/// </remarks>
		/// <param name="window">
		/// The desired window.
		/// </param>
		/// <param name="button">
		/// The desired [mouse button](@ref buttons).
		/// </param>
		/// <returns>
		/// One of `GLFW_PRESS` or `GLFW_RELEASE`.
		/// </returns>
		public static int glfwGetMouseButton(IntPtr window, int button)
		{
			return _glfwGetMouseButton(window, button);
		}

		/// <summary>
		/// Retrieves the position of the cursor relative to the content area of
		/// the window.
		/// </summary>
		/// <remarks>
		/// This function returns the position of the cursor, in screen coordinates,
		/// relative to the upper-left corner of the content area of the specified
		/// window.
		/// </remarks>
		/// <param name="window">
		/// The desired window.
		/// </param>
		/// <param name="xpos">
		/// Where to store the cursor x-coordinate, relative to the
		/// left edge of the content area, or `NULL`.
		/// </param>
		/// <param name="ypos">
		/// Where to store the cursor y-coordinate, relative to the to
		/// top edge of the content area, or `NULL`.
		/// </param>
		public static void glfwGetCursorPos(IntPtr window, out double xpos, out double ypos)
		{
			_glfwGetCursorPos(window, out xpos, out ypos);
		}

		/// <summary>
		/// Sets the position of the cursor, relative to the content area of the
		/// window.
		/// </summary>
		/// <remarks>
		/// This function sets the position, in screen coordinates, of the cursor
		/// relative to the upper-left corner of the content area of the specified
		/// window.  The window must have input focus.  If the window does not have
		/// input focus when this function is called, it fails silently.
		/// </remarks>
		/// <param name="window">
		/// The desired window.
		/// </param>
		/// <param name="xpos">
		/// The desired x-coordinate, relative to the left edge of the
		/// content area.
		/// </param>
		/// <param name="ypos">
		/// The desired y-coordinate, relative to the top edge of the
		/// content area.
		/// </param>
		public static void glfwSetCursorPos(IntPtr window, double xpos, double ypos)
		{
			_glfwSetCursorPos(window, xpos, ypos);
		}

		/// <summary>
		/// Creates a custom cursor.
		/// </summary>
		/// <remarks>
		/// Creates a new custom cursor image that can be set for a window with @ref
		/// glfwSetCursor.  The cursor can be destroyed with @ref glfwDestroyCursor.
		/// Any remaining cursors are destroyed by @ref glfwTerminate.
		/// </remarks>
		/// <param name="image">
		/// The desired cursor image.
		/// </param>
		/// <param name="xhot">
		/// The desired x-coordinate, in pixels, of the cursor hotspot.
		/// </param>
		/// <param name="yhot">
		/// The desired y-coordinate, in pixels, of the cursor hotspot.
		/// </param>
		/// <returns>
		/// The handle of the created cursor, or `NULL` if an
		/// [error](@ref error_handling) occurred.
		/// </returns>
		public static IntPtr glfwCreateCursor(IntPtr image, int xhot, int yhot)
		{
			return _glfwCreateCursor(image, xhot, yhot);
		}

		/// <summary>
		/// Creates a cursor with a standard shape.
		/// </summary>
		/// <remarks>
		/// Returns a cursor with a [standard shape](@ref shapes), that can be set for
		/// a window with @ref glfwSetCursor.
		/// </remarks>
		/// <param name="shape">
		/// One of the [standard shapes](@ref shapes).
		/// </param>
		/// <returns>
		/// A new cursor ready to use or `NULL` if an
		/// [error](@ref error_handling) occurred.
		/// </returns>
		public static IntPtr glfwCreateStandardCursor(int shape)
		{
			return _glfwCreateStandardCursor(shape);
		}

		/// <summary>
		/// Destroys a cursor.
		/// </summary>
		/// <remarks>
		/// This function destroys a cursor previously created with @ref
		/// glfwCreateCursor.  Any remaining cursors will be destroyed by @ref
		/// glfwTerminate.
		/// </remarks>
		/// <param name="cursor">
		/// The cursor object to destroy.
		/// </param>
		public static void glfwDestroyCursor(IntPtr cursor)
		{
			_glfwDestroyCursor(cursor);
		}

		/// <summary>
		/// Sets the cursor for the window.
		/// </summary>
		/// <remarks>
		/// This function sets the cursor image to be used when the cursor is over the
		/// content area of the specified window.  The set cursor will only be visible
		/// when the [cursor mode](@ref cursor_mode) of the window is
		/// `GLFW_CURSOR_NORMAL`.
		/// </remarks>
		/// <param name="window">
		/// The window to set the cursor for.
		/// </param>
		/// <param name="cursor">
		/// The cursor to set, or `NULL` to switch back to the default
		/// arrow cursor.
		/// </param>
		public static void glfwSetCursor(IntPtr window, IntPtr cursor)
		{
			_glfwSetCursor(window, cursor);
		}

		/// <summary>
		/// Sets the key callback.
		/// </summary>
		/// <remarks>
		/// This function sets the key callback of the specified window, which is called
		/// when a key is pressed, repeated or released.
		/// </remarks>
		/// <param name="window">
		/// The window whose callback to set.
		/// </param>
		/// <param name="cbfun">
		/// The new key callback, or `NULL` to remove the currently
		/// set callback.
		/// </param>
		/// <returns>
		/// The previously set callback, or `NULL` if no callback was set or the
		/// library had not been [initialized](@ref intro_init).
		/// </returns>
		public static GLFWkeyfun glfwSetKeyCallback(IntPtr window, GLFWkeyfun cbfun)
		{
			return _glfwSetKeyCallback(window, cbfun);
		}

		/// <summary>
		/// Sets the Unicode character callback.
		/// </summary>
		/// <remarks>
		/// This function sets the character callback of the specified window, which is
		/// called when a Unicode character is input.
		/// </remarks>
		/// <param name="window">
		/// The window whose callback to set.
		/// </param>
		/// <param name="cbfun">
		/// The new callback, or `NULL` to remove the currently set
		/// callback.
		/// </param>
		/// <returns>
		/// The previously set callback, or `NULL` if no callback was set or the
		/// library had not been [initialized](@ref intro_init).
		/// </returns>
		public static GLFWcharfun glfwSetCharCallback(IntPtr window, GLFWcharfun cbfun)
		{
			return _glfwSetCharCallback(window, cbfun);
		}

		/// <summary>
		/// Sets the Unicode character with modifiers callback.
		/// </summary>
		/// <remarks>
		/// This function sets the character with modifiers callback of the specified
		/// window, which is called when a Unicode character is input regardless of what
		/// modifier keys are used.
		/// </remarks>
		/// <param name="window">
		/// The window whose callback to set.
		/// </param>
		/// <param name="cbfun">
		/// The new callback, or `NULL` to remove the currently set
		/// callback.
		/// </param>
		/// <returns>
		/// The previously set callback, or `NULL` if no callback was set or an
		/// [error](@ref error_handling) occurred.
		/// </returns>
		public static GLFWcharmodsfun glfwSetCharModsCallback(IntPtr window, GLFWcharmodsfun cbfun)
		{
			return _glfwSetCharModsCallback(window, cbfun);
		}

		/// <summary>
		/// Sets the mouse button callback.
		/// </summary>
		/// <remarks>
		/// This function sets the mouse button callback of the specified window, which
		/// is called when a mouse button is pressed or released.
		/// </remarks>
		/// <param name="window">
		/// The window whose callback to set.
		/// </param>
		/// <param name="cbfun">
		/// The new callback, or `NULL` to remove the currently set
		/// callback.
		/// </param>
		/// <returns>
		/// The previously set callback, or `NULL` if no callback was set or the
		/// library had not been [initialized](@ref intro_init).
		/// </returns>
		public static GLFWmousebuttonfun glfwSetMouseButtonCallback(IntPtr window, GLFWmousebuttonfun cbfun)
		{
			return _glfwSetMouseButtonCallback(window, cbfun);
		}

		/// <summary>
		/// Sets the cursor position callback.
		/// </summary>
		/// <remarks>
		/// This function sets the cursor position callback of the specified window,
		/// which is called when the cursor is moved.  The callback is provided with the
		/// position, in screen coordinates, relative to the upper-left corner of the
		/// content area of the window.
		/// </remarks>
		/// <param name="window">
		/// The window whose callback to set.
		/// </param>
		/// <param name="cbfun">
		/// The new callback, or `NULL` to remove the currently set
		/// callback.
		/// </param>
		/// <returns>
		/// The previously set callback, or `NULL` if no callback was set or the
		/// library had not been [initialized](@ref intro_init).
		/// </returns>
		public static GLFWcursorposfun glfwSetCursorPosCallback(IntPtr window, GLFWcursorposfun cbfun)
		{
			return _glfwSetCursorPosCallback(window, cbfun);
		}

		/// <summary>
		/// Sets the cursor enter/exit callback.
		/// </summary>
		/// <remarks>
		/// This function sets the cursor boundary crossing callback of the specified
		/// window, which is called when the cursor enters or leaves the content area of
		/// the window.
		/// </remarks>
		/// <param name="window">
		/// The window whose callback to set.
		/// </param>
		/// <param name="cbfun">
		/// The new callback, or `NULL` to remove the currently set
		/// callback.
		/// </param>
		/// <returns>
		/// The previously set callback, or `NULL` if no callback was set or the
		/// library had not been [initialized](@ref intro_init).
		/// </returns>
		public static GLFWcursorenterfun glfwSetCursorEnterCallback(IntPtr window, GLFWcursorenterfun cbfun)
		{
			return _glfwSetCursorEnterCallback(window, cbfun);
		}

		/// <summary>
		/// Sets the scroll callback.
		/// </summary>
		/// <remarks>
		/// This function sets the scroll callback of the specified window, which is
		/// called when a scrolling device is used, such as a mouse wheel or scrolling
		/// area of a touchpad.
		/// </remarks>
		/// <param name="window">
		/// The window whose callback to set.
		/// </param>
		/// <param name="cbfun">
		/// The new scroll callback, or `NULL` to remove the currently
		/// set callback.
		/// </param>
		/// <returns>
		/// The previously set callback, or `NULL` if no callback was set or the
		/// library had not been [initialized](@ref intro_init).
		/// </returns>
		public static GLFWscrollfun glfwSetScrollCallback(IntPtr window, GLFWscrollfun cbfun)
		{
			return _glfwSetScrollCallback(window, cbfun);
		}

		/// <summary>
		/// Sets the file drop callback.
		/// </summary>
		/// <remarks>
		/// This function sets the file drop callback of the specified window, which is
		/// called when one or more dragged files are dropped on the window.
		/// </remarks>
		/// <param name="window">
		/// The window whose callback to set.
		/// </param>
		/// <param name="cbfun">
		/// The new file drop callback, or `NULL` to remove the
		/// currently set callback.
		/// </param>
		/// <returns>
		/// The previously set callback, or `NULL` if no callback was set or the
		/// library had not been [initialized](@ref intro_init).
		/// </returns>
		public static GLFWdropfun glfwSetDropCallback(IntPtr window, GLFWdropfun cbfun)
		{
			return _glfwSetDropCallback(window, cbfun);
		}

		/// <summary>
		/// Returns whether the specified joystick is present.
		/// </summary>
		/// <remarks>
		/// This function returns whether the specified joystick is present.
		/// </remarks>
		/// <param name="jid">
		/// The [joystick](@ref joysticks) to query.
		/// </param>
		/// <returns>
		/// `GLFW_TRUE` if the joystick is present, or `GLFW_FALSE` otherwise.
		/// </returns>
		public static int glfwJoystickPresent(int jid)
		{
			return _glfwJoystickPresent(jid);
		}

		/// <summary>
		/// Returns the values of all axes of the specified joystick.
		/// </summary>
		/// <remarks>
		/// This function returns the values of all axes of the specified joystick.
		/// Each element in the array is a value between -1.0 and 1.0.
		/// </remarks>
		/// <param name="jid">
		/// The [joystick](@ref joysticks) to query.
		/// </param>
		/// <param name="count">
		/// Where to store the number of axis values in the returned
		/// array.  This is set to zero if the joystick is not present or an error
		/// occurred.
		/// </param>
		/// <returns>
		/// An array of axis values, or `NULL` if the joystick is not present or
		/// an [error](@ref error_handling) occurred.
		/// </returns>
		public static float[] glfwGetJoystickAxes(int joy)
		{
            var arrayPtr = _glfwGetJoystickAxes(joy, out int count);

            if (arrayPtr == IntPtr.Zero)
                return null;

            var result = new float[count];
            Marshal.Copy(arrayPtr, result, 0, count);

            return result;
		}

		/// <summary>
		/// Returns the state of all buttons of the specified joystick.
		/// </summary>
		/// <remarks>
		/// This function returns the state of all buttons of the specified joystick.
		/// Each element in the array is either `GLFW_PRESS` or `GLFW_RELEASE`.
		/// </remarks>
		/// <param name="jid">
		/// The [joystick](@ref joysticks) to query.
		/// </param>
		/// <param name="count">
		/// Where to store the number of button states in the returned
		/// array.  This is set to zero if the joystick is not present or an error
		/// occurred.
		/// </param>
		/// <returns>
		/// An array of button states, or `NULL` if the joystick is not present
		/// or an [error](@ref error_handling) occurred.
		/// </returns>
		public static byte[] glfwGetJoystickButtons(int joy)
		{
			var arrayPtr = _glfwGetJoystickButtons(joy, out int count);

            var result = new byte[count];
            Marshal.Copy(arrayPtr, result, 0, count);

            return result;
		}

		/// <summary>
		/// Returns the state of all hats of the specified joystick.
		/// </summary>
		/// <remarks>
		/// This function returns the state of all hats of the specified joystick.
		/// Each element in the array is one of the following values:
		/// </remarks>
		/// <param name="jid">
		/// The [joystick](@ref joysticks) to query.
		/// </param>
		/// <param name="count">
		/// Where to store the number of hat states in the returned
		/// array.  This is set to zero if the joystick is not present or an error
		/// occurred.
		/// </param>
		/// <returns>
		/// An array of hat states, or `NULL` if the joystick is not present
		/// or an [error](@ref error_handling) occurred.
		/// </returns>
		public static IntPtr glfwGetJoystickHats(int jid, out int count)
		{
			return _glfwGetJoystickHats(jid, out count);
		}

		/// <summary>
		/// Returns the name of the specified joystick.
		/// </summary>
		/// <remarks>
		/// This function returns the name, encoded as UTF-8, of the specified joystick.
		/// The returned string is allocated and freed by GLFW.  You should not free it
		/// yourself.
		/// </remarks>
		/// <param name="jid">
		/// The [joystick](@ref joysticks) to query.
		/// </param>
		/// <returns>
		/// The UTF-8 encoded name of the joystick, or `NULL` if the joystick
		/// is not present or an [error](@ref error_handling) occurred.
		/// </returns>
		public static IntPtr glfwGetJoystickName(int jid)
		{
			return _glfwGetJoystickName(jid);
		}

		/// <summary>
		/// Returns the SDL comaptible GUID of the specified joystick.
		/// </summary>
		/// <remarks>
		/// This function returns the SDL compatible GUID, as a UTF-8 encoded
		/// hexadecimal string, of the specified joystick.  The returned string is
		/// allocated and freed by GLFW.  You should not free it yourself.
		/// </remarks>
		/// <param name="jid">
		/// The [joystick](@ref joysticks) to query.
		/// </param>
		/// <returns>
		/// The UTF-8 encoded GUID of the joystick, or `NULL` if the joystick
		/// is not present or an [error](@ref error_handling) occurred.
		/// </returns>
		public static IntPtr glfwGetJoystickGUID(int jid)
		{
			return _glfwGetJoystickGUID(jid);
		}

		/// <summary>
		/// Sets the user pointer of the specified joystick.
		/// </summary>
		/// <remarks>
		/// This function sets the user-defined pointer of the specified joystick.  The
		/// current value is retained until the joystick is disconnected.  The initial
		/// value is `NULL`.
		/// </remarks>
		/// <param name="jid">
		/// The joystick whose pointer to set.
		/// </param>
		/// <param name="pointer">
		/// The new value.
		/// </param>
		public static void glfwSetJoystickUserPointer(int jid, IntPtr pointer)
		{
			_glfwSetJoystickUserPointer(jid, pointer);
		}

		/// <summary>
		/// Returns the user pointer of the specified joystick.
		/// </summary>
		/// <remarks>
		/// This function returns the current value of the user-defined pointer of the
		/// specified joystick.  The initial value is `NULL`.
		/// </remarks>
		/// <param name="jid">
		/// The joystick whose pointer to return.
		/// </param>
		public static IntPtr glfwGetJoystickUserPointer(int jid)
		{
			return _glfwGetJoystickUserPointer(jid);
		}

		/// <summary>
		/// Returns whether the specified joystick has a gamepad mapping.
		/// </summary>
		/// <remarks>
		/// This function returns whether the specified joystick is both present and has
		/// a gamepad mapping.
		/// </remarks>
		/// <param name="jid">
		/// The [joystick](@ref joysticks) to query.
		/// </param>
		/// <returns>
		/// `GLFW_TRUE` if a joystick is both present and has a gamepad mapping,
		/// or `GLFW_FALSE` otherwise.
		/// </returns>
		public static int glfwJoystickIsGamepad(int jid)
		{
			return _glfwJoystickIsGamepad(jid);
		}

		/// <summary>
		/// Sets the joystick configuration callback.
		/// </summary>
		/// <remarks>
		/// This function sets the joystick configuration callback, or removes the
		/// currently set callback.  This is called when a joystick is connected to or
		/// disconnected from the system.
		/// </remarks>
		/// <param name="cbfun">
		/// The new callback, or `NULL` to remove the currently set
		/// callback.
		/// </param>
		/// <returns>
		/// The previously set callback, or `NULL` if no callback was set or the
		/// library had not been [initialized](@ref intro_init).
		/// </returns>
		public static GLFWjoystickfun glfwSetJoystickCallback(GLFWjoystickfun cbfun)
		{
			return _glfwSetJoystickCallback(cbfun);
		}

		/// <summary>
		/// Adds the specified SDL_GameControllerDB gamepad mappings.
		/// </summary>
		/// <remarks>
		/// This function parses the specified ASCII encoded string and updates the
		/// internal list with any gamepad mappings it finds.  This string may
		/// contain either a single gamepad mapping or many mappings separated by
		/// newlines.  The parser supports the full format of the `gamecontrollerdb.txt`
		/// source file including empty lines and comments.
		/// </remarks>
		/// <param name="string">
		/// The string containing the gamepad mappings.
		/// </param>
		/// <returns>
		/// `GLFW_TRUE` if successful, or `GLFW_FALSE` if an
		/// [error](@ref error_handling) occurred.
		/// </returns>
		public static int glfwUpdateGamepadMappings(string @string)
		{
			return _glfwUpdateGamepadMappings(@string);
		}

		/// <summary>
		/// Returns the human-readable gamepad name for the specified joystick.
		/// </summary>
		/// <remarks>
		/// This function returns the human-readable name of the gamepad from the
		/// gamepad mapping assigned to the specified joystick.
		/// </remarks>
		/// <param name="jid">
		/// The [joystick](@ref joysticks) to query.
		/// </param>
		/// <returns>
		/// The UTF-8 encoded name of the gamepad, or `NULL` if the
		/// joystick is not present, does not have a mapping or an
		/// [error](@ref error_handling) occurred.
		/// </returns>
		public static IntPtr glfwGetGamepadName(int jid)
		{
			return _glfwGetGamepadName(jid);
		}

		/// <summary>
		/// Retrieves the state of the specified joystick remapped as a gamepad.
		/// </summary>
		/// <remarks>
		/// This function retrives the state of the specified joystick remapped to
		/// an Xbox-like gamepad.
		/// </remarks>
		/// <param name="jid">
		/// The [joystick](@ref joysticks) to query.
		/// </param>
		/// <param name="state">
		/// The gamepad input state of the joystick.
		/// </param>
		/// <returns>
		/// `GLFW_TRUE` if successful, or `GLFW_FALSE` if no joystick is
		/// connected, it has no gamepad mapping or an [error](@ref error_handling)
		/// occurred.
		/// </returns>
		public static int glfwGetGamepadState(int jid, out IntPtr state)
		{
			return _glfwGetGamepadState(jid, out state);
		}

		/// <summary>
		/// Sets the clipboard to the specified string.
		/// </summary>
		/// <remarks>
		/// This function sets the system clipboard to the specified, UTF-8 encoded
		/// string.
		/// </remarks>
		/// <param name="window">
		/// Deprecated.  Any valid window or `NULL`.
		/// </param>
		/// <param name="string">
		/// A UTF-8 encoded string.
		/// </param>
		public static unsafe void glfwSetClipboardString(IntPtr window, string s)
		{
			var bytes = System.Text.Encoding.UTF8.GetBytes(s + "\0");
			fixed (byte* p = &bytes[0])
				_glfwSetClipboardString(window, (IntPtr)p);
		}

		/// <summary>
		/// Returns the contents of the clipboard as a string.
		/// </summary>
		/// <remarks>
		/// This function returns the contents of the system clipboard, if it contains
		/// or is convertible to a UTF-8 encoded string.  If the clipboard is empty or
		/// if its contents cannot be converted, `NULL` is returned and a @ref
		/// GLFW_FORMAT_UNAVAILABLE error is generated.
		/// </remarks>
		/// <param name="window">
		/// Deprecated.  Any valid window or `NULL`.
		/// </param>
		/// <returns>
		/// The contents of the clipboard as a UTF-8 encoded string, or `NULL`
		/// if an [error](@ref error_handling) occurred.
		/// </returns>
		public static unsafe string glfwGetClipboardString(IntPtr window)
		{
			return PtrToStringUTF8(_glfwGetClipboardString(window));
		}

		/// <summary>
		/// Returns the value of the GLFW timer.
		/// </summary>
		/// <remarks>
		/// This function returns the value of the GLFW timer.  Unless the timer has
		/// been set using @ref glfwSetTime, the timer measures time elapsed since GLFW
		/// was initialized.
		/// </remarks>
		/// <returns>
		/// The current value, in seconds, or zero if an
		/// [error](@ref error_handling) occurred.
		/// </returns>
		public static double glfwGetTime()
		{
			return _glfwGetTime();
		}

		/// <summary>
		/// Sets the GLFW timer.
		/// </summary>
		/// <remarks>
		/// This function sets the value of the GLFW timer.  It then continues to count
		/// up from that value.  The value must be a positive finite number less than
		/// or equal to 18446744073.0, which is approximately 584.5 years.
		/// </remarks>
		/// <param name="time">
		/// The new value, in seconds.
		/// </param>
		public static void glfwSetTime(double time)
		{
			_glfwSetTime(time);
		}

		/// <summary>
		/// Returns the current value of the raw timer.
		/// </summary>
		/// <remarks>
		/// This function returns the current value of the raw timer, measured in
		/// 1&nbsp;/&nbsp;frequency seconds.  To get the frequency, call @ref
		/// glfwGetTimerFrequency.
		/// </remarks>
		/// <returns>
		/// The value of the timer, or zero if an
		/// [error](@ref error_handling) occurred.
		/// </returns>
		public static ulong glfwGetTimerValue()
		{
			return _glfwGetTimerValue();
		}

		/// <summary>
		/// Returns the frequency, in Hz, of the raw timer.
		/// </summary>
		/// <remarks>
		/// This function returns the frequency, in Hz, of the raw timer.
		/// </remarks>
		/// <returns>
		/// The frequency of the timer, in Hz, or zero if an
		/// [error](@ref error_handling) occurred.
		/// </returns>
		public static ulong glfwGetTimerFrequency()
		{
			return _glfwGetTimerFrequency();
		}

		/// <summary>
		/// Makes the context of the specified window current for the calling
		/// thread.
		/// </summary>
		/// <remarks>
		/// This function makes the OpenGL or OpenGL ES context of the specified window
		/// current on the calling thread.  A context must only be made current on
		/// a single thread at a time and each thread can have only a single current
		/// context at a time.
		/// </remarks>
		/// <param name="window">
		/// The window whose context to make current, or `NULL` to
		/// detach the current context.
		/// </param>
		public static void glfwMakeContextCurrent(IntPtr window)
		{
			_glfwMakeContextCurrent(window);
		}

		/// <summary>
		/// Returns the window whose context is current on the calling thread.
		/// </summary>
		/// <remarks>
		/// This function returns the window whose OpenGL or OpenGL ES context is
		/// current on the calling thread.
		/// </remarks>
		/// <returns>
		/// The window whose context is current, or `NULL` if no window's
		/// context is current.
		/// </returns>
		public static IntPtr glfwGetCurrentContext()
		{
			return _glfwGetCurrentContext();
		}

		/// <summary>
		/// Swaps the front and back buffers of the specified window.
		/// </summary>
		/// <remarks>
		/// This function swaps the front and back buffers of the specified window when
		/// rendering with OpenGL or OpenGL ES.  If the swap interval is greater than
		/// zero, the GPU driver waits the specified number of screen updates before
		/// swapping the buffers.
		/// </remarks>
		/// <param name="window">
		/// The window whose buffers to swap.
		/// </param>
		public static void glfwSwapBuffers(IntPtr window)
		{
			_glfwSwapBuffers(window);
		}

		/// <summary>
		/// Sets the swap interval for the current context.
		/// </summary>
		/// <remarks>
		/// This function sets the swap interval for the current OpenGL or OpenGL ES
		/// context, i.e. the number of screen updates to wait from the time @ref
		/// glfwSwapBuffers was called before swapping the buffers and returning.  This
		/// is sometimes called _vertical synchronization_, _vertical retrace
		/// synchronization_ or just _vsync_.
		/// </remarks>
		/// <param name="interval">
		/// The minimum number of screen updates to wait for
		/// until the buffers are swapped by @ref glfwSwapBuffers.
		/// </param>
		public static void glfwSwapInterval(int interval)
		{
			_glfwSwapInterval(interval);
		}

		/// <summary>
		/// Returns whether the specified extension is available.
		/// </summary>
		/// <remarks>
		/// This function returns whether the specified
		/// [API extension](@ref context_glext) is supported by the current OpenGL or
		/// OpenGL ES context.  It searches both for client API extension and context
		/// creation API extensions.
		/// </remarks>
		/// <param name="extension">
		/// The ASCII encoded name of the extension.
		/// </param>
		/// <returns>
		/// `GLFW_TRUE` if the extension is available, or `GLFW_FALSE`
		/// otherwise.
		/// </returns>
		public static int glfwExtensionSupported(string extension)
		{
			return _glfwExtensionSupported(extension);
		}

		/// <summary>
		/// Returns the address of the specified function for the current
		/// context.
		/// </summary>
		/// <remarks>
		/// This function returns the address of the specified OpenGL or OpenGL ES
		/// [core or extension function](@ref context_glext), if it is supported
		/// by the current context.
		/// </remarks>
		/// <param name="procname">
		/// The ASCII encoded name of the function.
		/// </param>
		/// <returns>
		/// The address of the function, or `NULL` if an
		/// [error](@ref error_handling) occurred.
		/// </returns>
		public static IntPtr glfwGetProcAddress(string procname)
		{
			return _glfwGetProcAddress(procname);
		}

		/// <summary>
		/// Returns whether the Vulkan loader and an ICD have been found.
		/// </summary>
		/// <remarks>
		/// This function returns whether the Vulkan loader and any minimally functional
		/// ICD have been found.
		/// </remarks>
		/// <returns>
		/// `GLFW_TRUE` if Vulkan is minimally available, or `GLFW_FALSE`
		/// otherwise.
		/// </returns>
		public static int glfwVulkanSupported()
		{
			return _glfwVulkanSupported();
		}

		/// <summary>
		/// Returns the Vulkan instance extensions required by GLFW.
		/// </summary>
		/// <remarks>
		/// This function returns an array of names of Vulkan instance extensions required
		/// by GLFW for creating Vulkan surfaces for GLFW windows.  If successful, the
		/// list will always contains `VK_KHR_surface`, so if you don't require any
		/// additional extensions you can pass this list directly to the
		/// `VkInstanceCreateInfo` struct.
		/// </remarks>
		/// <param name="count">
		/// Where to store the number of extensions in the returned
		/// array.  This is set to zero if an error occurred.
		/// </param>
		/// <returns>
		/// An array of ASCII encoded extension names, or `NULL` if an
		/// [error](@ref error_handling) occurred.
		/// </returns>
		public static string[] glfwGetRequiredInstanceExtensions()
		{
			var arrayPtr = _glfwGetRequiredInstanceExtensions(out uint count);

			if (arrayPtr == IntPtr.Zero)
				return null;

			var result = new string[count];

            for (int i = 0; i < count; i++)
			{
                IntPtr stringPtr = Marshal.ReadIntPtr(arrayPtr, i * IntPtr.Size);
                result[i] = Marshal.PtrToStringAnsi(stringPtr);
			}

			return result;
		}

		/// <summary>
		/// Returns the address of the specified Vulkan instance function.
		/// </summary>
		/// <remarks>
		/// This function returns the address of the specified Vulkan core or extension
		/// function for the specified instance.  If instance is set to `NULL` it can
		/// return any function exported from the Vulkan loader, including at least the
		/// following functions:
		/// </remarks>
		/// <param name="instance">
		/// The Vulkan instance to query, or `NULL` to retrieve
		/// functions related to instance creation.
		/// </param>
		/// <param name="procname">
		/// The ASCII encoded name of the function.
		/// </param>
		/// <returns>
		/// The address of the function, or `NULL` if an
		/// [error](@ref error_handling) occurred.
		/// </returns>
		public static IntPtr glfwGetInstanceProcAddress(IntPtr instance, string procname)
		{
			return _glfwGetInstanceProcAddress(instance, procname);
		}

		/// <summary>
		/// Returns whether the specified queue family can present images.
		/// </summary>
		/// <remarks>
		/// This function returns whether the specified queue family of the specified
		/// physical device supports presentation to the platform GLFW was built for.
		/// </remarks>
		/// <param name="instance">
		/// The instance that the physical device belongs to.
		/// </param>
		/// <param name="device">
		/// The physical device that the queue family belongs to.
		/// </param>
		/// <param name="queuefamily">
		/// The index of the queue family to query.
		/// </param>
		/// <returns>
		/// `GLFW_TRUE` if the queue family supports presentation, or
		/// `GLFW_FALSE` otherwise.
		/// </returns>
		public static int glfwGetPhysicalDevicePresentationSupport(IntPtr instance, IntPtr device, uint queuefamily)
		{
			return _glfwGetPhysicalDevicePresentationSupport(instance, device, queuefamily);
		}

		/// <summary>
		/// Creates a Vulkan surface for the specified window.
		/// </summary>
		/// <remarks>
		/// This function creates a Vulkan surface for the specified window.
		/// </remarks>
		/// <param name="instance">
		/// The Vulkan instance to create the surface in.
		/// </param>
		/// <param name="window">
		/// The window to create the surface for.
		/// </param>
		/// <param name="allocator">
		/// The allocator to use, or `NULL` to use the default
		/// allocator.
		/// </param>
		/// <param name="surface">
		/// Where to store the handle of the surface.  This is set
		/// to `VK_NULL_HANDLE` if an error occurred.
		/// </param>
		/// <returns>
		/// `VK_SUCCESS` if successful, or a Vulkan error code if an
		/// [error](@ref error_handling) occurred.
		/// </returns>
		public static int glfwCreateWindowSurface(IntPtr instance, IntPtr window, IntPtr allocator, out IntPtr surface)
		{
			return _glfwCreateWindowSurface(instance, window, allocator, out surface);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <remarks>
		/// 
		/// </remarks>
		/// <param name="window">
		/// 
		/// </param>
		public static IntPtr glfwGetWin32Window(IntPtr window)
		{
			return _glfwGetWin32Window(window);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <remarks>
		/// 
		/// </remarks>
		/// <param name="window">
		/// 
		/// </param>
		public static IntPtr glfwGetX11Window(IntPtr window)
		{
			return _glfwGetX11Window(window);
		}

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// 
        /// </remarks>
        /// <param name="window">
        /// 
        /// </param>
        public static IntPtr glfwGetX11Display()
        {
            return _glfwGetX11Display();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// 
        /// </remarks>
        /// <param name="window">
        /// 
        /// </param>
        public static IntPtr glfwGetCocoaWindow(IntPtr window)
		{
			return _glfwGetCocoaWindow(window);
		}

		/*! @brief Returns the currently selected platform.
		*
		*  This function returns the platform that was selected during initialization.  The
		*  returned value will be one of `GLFW_PLATFORM_WIN32`, `GLFW_PLATFORM_COCOA`,
		*  `GLFW_PLATFORM_WAYLAND`, `GLFW_PLATFORM_X11` or `GLFW_PLATFORM_NULL`.
		*
		*  @return The currently selected platform, or zero if an error occurred.
		*
		*  @errors Possible errors include @ref GLFW_NOT_INITIALIZED.
		*
		*  @thread_safety This function may be called from any thread.
		*
		*  @sa @ref platform
		*  @sa @ref glfwPlatformSupported
		*
		*  @since Added in version 3.4.
		*
		*  @ingroup init
		*/
		public static IntPtr glfwGetPlatform()
		{
			return _glfwGetPlatform();
		}

	}
}
