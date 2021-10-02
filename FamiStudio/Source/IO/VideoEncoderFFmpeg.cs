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

        private bool usePngPipe;
        private bool useOpenH264;

        private int resX;
        private int resY;

        public bool AlternateByteOrdering => usePngPipe;

        private VideoEncoderFFmpeg(bool png, bool openH264)
        {
            usePngPipe  = png;
            useOpenH264 = openH264;
        }

        public static VideoEncoderFFmpeg CreateInstance()
        {
            if (DetectFFmpeg(Settings.FFmpegExecutablePath, out var hasX264, out var hasOpenH264, out var hasRawVideo, out var hasPngPipe))
            {
                if ((hasRawVideo || hasPngPipe) && (hasX264 || hasOpenH264))
                {
                    return new VideoEncoderFFmpeg(hasRawVideo ? hasRawVideo : hasPngPipe, hasX264 ? hasX264 : hasOpenH264);
                }
                else
                {
                    Log.LogMessage(LogSeverity.Error, "ffmpeg does not seem to be compiled with x264 or OpenH264 support, or is missing rawvideo or png_pipe input. Make sure you have the GPL version if you want x264.");
                    return null;
                }
            }
            else
            {
                Log.LogMessage(LogSeverity.Error, "Error launching ffmpeg. Make sure the path is correct.");
                return null;
            }
        }

        public bool BeginEncoding(int x, int y, int frameRateNumer, int frameRateDenom, int videoBitRate, int audioBitRate, string audioFile, string outputFile)
        {
            resX = x;
            resY = y;
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
            #if FAMISTUDIO_LINUX
            if (usePngPipe)
            {
                // TODO : Move this to PlatformUtils or something.
                var pngData = new Gdk.Pixbuf(image, true, 8, resX, resY, resX * 4).SaveToBuffer("png");
                stream.Write(pngData);
            }
            else
            #endif
            {
                stream.Write(image);
            }
        }

        public void EndEncoding(bool abort)
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

        private static bool DetectFFmpeg(string ffmpegExecutable, out bool hasX264, out bool hasOpenH264, out bool hasRawVideo, out bool hasPngPipe)
        {
            hasX264     = false;
            hasOpenH264 = false;
            hasRawVideo = false;
            hasPngPipe  = false;

            try
            {
                using (var process = LaunchFFmpeg(ffmpegExecutable, $"-version", false, true))
                {
                    var output = process.StandardOutput.ReadToEnd();
                    hasX264     = output.Contains("--enable-libx264");
                    hasOpenH264 = output.Contains("--enable-libopenh264");
                    process.WaitForExit();
                }

                using (var process = LaunchFFmpeg(ffmpegExecutable, $"-formats", false, true))
                {
                    var output = process.StandardOutput.ReadToEnd();
                    hasRawVideo = output.Contains("rawvideo");
                    hasPngPipe  = output.Contains("png_pipe") && PlatformUtils.IsLinux; // We only care to support this for flatpak.
                    process.WaitForExit();
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}