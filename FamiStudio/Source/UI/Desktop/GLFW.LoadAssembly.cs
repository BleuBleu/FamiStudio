using System;
using System.IO;
using System.Runtime.InteropServices;

namespace GLFWDotNet
{
    public static partial class GLFW
    {
        private static class Win32
        {
            [DllImport("kernel32")]
            public static extern IntPtr LoadLibrary(string fileName);

            [DllImport("kernel32")]
            public static extern IntPtr GetProcAddress(IntPtr module, string procName);
        }

        private static class Unix
        {
            public delegate IntPtr dlopenDelegate(string fileName, int flags);
            public delegate IntPtr dlsymDelegate(IntPtr handle, string symbol);
            public delegate IntPtr dlerrorDelegate();

            public static dlopenDelegate  dlopen;
            public static dlsymDelegate   dlsym;
            public static dlerrorDelegate dlerror;

            private static void Initialize()
            {
                if (dlopen != null)
                    return;

                // See which name variation works.
                try
                {
                    dlerror1();
                    dlopen  = new dlopenDelegate(dlopen1);
                    dlsym   = new dlsymDelegate(dlsym1);
                    dlerror = new dlerrorDelegate(dlerror1);
                }
                catch
                {
                    try
                    {
                        dlerror2();
                        dlopen = new dlopenDelegate(dlopen2);
                        dlsym = new dlsymDelegate(dlsym2);
                        dlerror = new dlerrorDelegate(dlerror2);
                    }
                    catch
                    {
                        dlerror3();
                        dlopen = new dlopenDelegate(dlopen3);
                        dlsym = new dlsymDelegate(dlsym3);
                        dlerror = new dlerrorDelegate(dlerror3);
                    }
                }
            }

            public static IntPtr LoadLibrary(string fileName)
            {
                Initialize();

                IntPtr retVal = dlopen(fileName, 2);
                var errPtr = dlerror();
                
                if (errPtr != IntPtr.Zero)
                    throw new InvalidOperationException(Marshal.PtrToStringAnsi (errPtr));

                return retVal;
            }

            // HACK : I've had issues on some distros when libdl wasnt detected. So we will
            // try up to 3 different name variations here. 
            [DllImport("libdl.so")]
            private static extern IntPtr dlopen1(string fileName, int flags);
            [DllImport("libdl.so")]
            public static extern IntPtr dlsym1(IntPtr handle, string symbol);
            [DllImport("libdl.so")]
            private static extern IntPtr dlerror1();

            [DllImport("libdl.so.2")]
            private static extern IntPtr dlopen2(string fileName, int flags);
            [DllImport("libdl.so.2")]
            public static extern IntPtr dlsym2(IntPtr handle, string symbol);
            [DllImport("libdl.so.2")]
            private static extern IntPtr dlerror2();

            [DllImport("libdl.so.1")]
            private static extern IntPtr dlopen3(string fileName, int flags);
            [DllImport("libdl.so.1")]
            public static extern IntPtr dlsym3(IntPtr handle, string symbol);
            [DllImport("libdl.so.1")]
            private static extern IntPtr dlerror3();
        }

        private static class MacOS
        {
            public static IntPtr LoadLibrary(string fileName)
            {
                IntPtr retVal = dlopen(fileName, 2);
                var errPtr = dlerror();

                if (errPtr != IntPtr.Zero)
                    throw new InvalidOperationException(Marshal.PtrToStringAnsi(errPtr));

                return retVal;
            }

            [DllImport("libdl.dylib")]
            private static extern IntPtr dlopen(string fileName, int flags);

            [DllImport("libdl.dylib")]
            public static extern IntPtr dlsym(IntPtr handle, string symbol);

            [DllImport("libdl.dylib")]
            private static extern IntPtr dlerror();
        }

        private static Func<string, IntPtr> LoadAssembly()
        {
            var assemblyDirectory = Path.GetDirectoryName(typeof(GLFW).Assembly.Location);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                string assemblyPath = Path.Combine(
                    assemblyDirectory,
                    "glfw3.dll");

                IntPtr assembly = Win32.LoadLibrary(assemblyPath);

                if (assembly == IntPtr.Zero)
                    throw new InvalidOperationException($"Failed to load GLFW dll from path '{assemblyPath}'.");

                return x => Win32.GetProcAddress(assembly, x);
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                string assemblyPath = Path.Combine(
                    assemblyDirectory,
                    "libglfw.so");

                IntPtr assembly = Unix.LoadLibrary(assemblyPath);

                if (assembly == IntPtr.Zero)
                    throw new InvalidOperationException($"Failed to load GLFW dll from path '{assemblyPath}'.");

                return functionName => Unix.dlsym(assembly, functionName);
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                string assemblyPath = Path.Combine(
                    assemblyDirectory,
                    "libglfw.dylib");

                IntPtr assembly = MacOS.LoadLibrary(assemblyPath);

                if (assembly == IntPtr.Zero)
                    throw new InvalidOperationException($"Failed to load GLFW dll from path '{assemblyPath}'.");

                return functionName => MacOS.dlsym(assembly, functionName);
            }

            throw new NotImplementedException("Unsupported platform.");
        }

        public static IntPtr glfwGetNativeWindow(IntPtr window)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return glfwGetWin32Window(window);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return glfwGetX11Window(window);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return glfwGetCocoaWindow(window);
            }

            throw new NotImplementedException("Unsupported platform.");
        }
    }
}
