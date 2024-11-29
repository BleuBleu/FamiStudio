using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace FamiStudio
{
    class VideoEncoderFFmpeg : IVideoEncoder
    {
        private Process process;
        private BinaryWriter stream;
        private byte[] videoImage;

        public VideoEncoderFFmpeg()
        {
        }

        public bool BeginEncoding(int resX, int resY, int frameRateNumer, int frameRateDenom, int videoBitRate, int audioBitRate, bool stereo, string audioFile, string outputFile)
        {
            process = LaunchFFmpeg(Settings.FFmpegExecutablePath, $"-y -f rawvideo -pix_fmt argb -s {resX}x{resY} -r {frameRateNumer}/{frameRateDenom} -i - -i \"{audioFile}\" -c:v h264 -pix_fmt yuv420p -b:v {videoBitRate}K -c:a aac -aac_is disable -b:a {audioBitRate}k \"{outputFile}\"", true, false, true);
            stream = new BinaryWriter(process.StandardInput.BaseStream);
            videoImage = new byte[resX * resY * 4];

            if (Platform.IsWindows)
            {
                // Cant raise the process priority without being admin on Linux/MacOS.
                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.BelowNormal;
            }

            return true;
        }

        public bool AddFrame(OffscreenGraphics graphics)
        {
            graphics.GetBitmap(videoImage);
            stream.Write(videoImage);
            return true;
        }

        public void EndEncoding(bool abort)
        {
            stream.Dispose();
            stream = null;

            process.WaitForExit();
            process.Dispose();
            process = null;

            if (Platform.IsWindows)
            {
                // Cant raise the process priority without being admin on Linux/MacOS.
                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Normal;
            }
        }

        private static Process LaunchFFmpeg(string ffmpegExecutable, string commandLine, bool redirectStdIn, bool redirectStdOut, bool boostPriority)
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

            if (boostPriority)
                process.PriorityClass = ProcessPriorityClass.BelowNormal;

            return process;
        }

        public static bool DetectFFmpeg()
        {
            try
            {
                var process = LaunchFFmpeg(Settings.FFmpegExecutablePath, $"-version", false, true, false);
                var output = process.StandardOutput.ReadToEnd();

                var ret = true;
                if (!output.Contains("--enable-libx264") && !output.Contains("--enable-libopenh264"))
                {
                    Log.LogMessage(LogSeverity.Error, "ffmpeg does not seem to be compiled with H.264 support. Make sure you have the GPL version.");

                    if (Platform.IsLinux)
                    {
                        Log.LogMessage(LogSeverity.Info,
                            "If you are running the Flatpak build, please make sure that ffmpeg-full is installed. " +
                            "You can do this via the following command:\n\n" +
                            "flatpak install org.freedesktop.Platform.ffmpeg-full\n\n" +
                            "Then select the appropriate version.");
                    }

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
