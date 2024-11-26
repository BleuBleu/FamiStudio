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
            public static IntPtr LoadLibrary(string fileName)
            {
                IntPtr retVal = dlopen(fileName, 2);
                var errPtr = dlerror();
                
                if (errPtr != IntPtr.Zero)
                    throw new InvalidOperationException(Marshal.PtrToStringAnsi (errPtr));

                return retVal;
            }

            const string DlLibName = "libdl";

            [DllImport(DlLibName)]
            private static extern IntPtr dlopen(string fileName, int flags);

            [DllImport(DlLibName)]
            public static extern IntPtr dlsym(IntPtr handle, string symbol);

            [DllImport(DlLibName)]
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

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                string extension = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "so" : "dylib";
                string assemblyPath = Path.Combine(
                    assemblyDirectory, 
                    $"libglfw.{extension}");
                    
                // Try to load system GLFW, falling back on our own binary if necessary
                IntPtr assembly = NativeLibrary.TryLoad($"libglfw.so.3", out var handle) ? handle : Unix.LoadLibrary(assemblyPath);

                if (assembly == IntPtr.Zero)
                    throw new InvalidOperationException($"Failed to load GLFW {extension} from path '{assemblyPath}'.");

                return functionName => Unix.dlsym(assembly, functionName);
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
