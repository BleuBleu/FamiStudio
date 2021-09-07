using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace FamiStudio
{
    class VideoEncoderFFmpeg
    {
        private Process process;
        private BinaryWriter stream;

        private VideoEncoderFFmpeg()
        {

        }

        public static VideoEncoderFFmpeg CreateInstance()
        {
            if (DetectFFmpeg(Settings.FFmpegExecutablePath))
            {
                return new VideoEncoderFFmpeg();
            }
            else
            {
                return null;
            }
        }

        public bool BeginEncoding(int resX, int resY, int frameRateNumer, int frameRateDenom, int videoBitRate, int audioBitRate, string audioFile, string outputFile)
        {
            process = LaunchFFmpeg(Settings.FFmpegExecutablePath, $"-y -f rawvideo -pix_fmt argb -s {resX}x{resY} -r {frameRateNumer}/{frameRateDenom} -i - -i \"{audioFile}\" -c:v h264 -pix_fmt yuv420p -b:v {videoBitRate}K -c:a aac -b:a {audioBitRate}k \"{outputFile}\"", true, false);
            stream = new BinaryWriter(process.StandardInput.BaseStream);

            if (PlatformUtils.IsWindows)
            {
                // Cant raise the process priority without being admin on Linux/MacOS.
                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.BelowNormal;
            }

            return true;
        }

        public void AddFrame(byte[] image)
        {
            stream.Write(image);
        }

        public void EndEncoding()
        {
            stream.Dispose();
            stream = null;

            process.WaitForExit();
            process.Dispose();
            process = null;

            if (PlatformUtils.IsWindows)
            {
                // Cant raise the process priority without being admin on Linux/MacOS.
                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Normal;
            }
        }

        private static Process LaunchFFmpeg(string ffmpegExecutable, string commandLine, bool redirectStdIn, bool redirectStdOut)
        {
            var psi = new ProcessStartInfo(ffmpegExecutable, commandLine);

            psi.UseShellExecute = false;
            psi.WorkingDirectory = Path.GetDirectoryName(ffmpegExecutable);
            psi.CreateNoWindow = true;
            psi.WindowStyle = ProcessWindowStyle.Hidden;

            if (redirectStdIn)
            {
                psi.RedirectStandardInput = true;
            }

            if (redirectStdOut)
            {
                psi.RedirectStandardOutput = true;
            }

            var process = Process.Start(psi);
            process.PriorityClass = ProcessPriorityClass.BelowNormal;
            return process;
        }

        private static bool DetectFFmpeg(string ffmpegExecutable)
        {
            try
            {
                var process = LaunchFFmpeg(ffmpegExecutable, $"-version", false, true);
                var output = process.StandardOutput.ReadToEnd();

                var ret = true;
                if (!output.Contains("--enable-libx264"))
                {
                    Log.LogMessage(LogSeverity.Error, "ffmpeg does not seem to be compiled with x264 support. Make sure you have the GPL version.");
                    ret = false;
                }

                process.WaitForExit();
                process.Dispose();

                return ret;
            }
            catch
            {
                Log.LogMessage(LogSeverity.Error, "Error launching ffmpeg. Make sure the path is correct.");
                return false;
            }
        }
    }
}