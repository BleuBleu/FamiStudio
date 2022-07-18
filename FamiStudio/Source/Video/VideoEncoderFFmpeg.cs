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
        private bool mp3Audio = false;
        private Process process;
        private BinaryWriter stream;

        private VideoEncoderFFmpeg(bool mp3)
        {
            mp3Audio = mp3;
        }

        public static VideoEncoderFFmpeg CreateInstance()
        {
            if (DetectFFmpeg(Settings.FFmpegExecutablePath, out var mp3))
            {
                return new VideoEncoderFFmpeg(mp3);
            }
            else
            {
                return null;
            }
        }

        public bool BeginEncoding(int resX, int resY, int frameRateNumer, int frameRateDenom, int videoBitRate, int audioBitRate, bool stereo, string audioFile, string outputFile)
        {
            var audioCodec = mp3Audio ? "libmp3lame" : "aac";

            process = LaunchFFmpeg(Settings.FFmpegExecutablePath, $"-y -f rawvideo -pix_fmt argb -s {resX}x{resY} -r {frameRateNumer}/{frameRateDenom} -i - -i \"{audioFile}\" -c:v h264 -pix_fmt yuv420p -b:v {videoBitRate}K -c:a {audioCodec} -b:a {audioBitRate}k \"{outputFile}\"", true, false);
            stream = new BinaryWriter(process.StandardInput.BaseStream);

            if (Platform.IsWindows)
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

        private static bool DetectFFmpeg(string ffmpegExecutable, out bool mp3Support)
        {
            try
            {
                var process = LaunchFFmpeg(ffmpegExecutable, $"-version", false, true);
                var output = process.StandardOutput.ReadToEnd();

                var ret = true;
                if (!output.Contains("--enable-libx264"))
                {
                    Log.LogMessage(LogSeverity.Error, "ffmpeg does not seem to be compiled with x264 support. Make sure you have the GPL version.");

                    if (Platform.IsLinux)
                    {
                        Log.LogMessage(LogSeverity.Info, 
                            "If you are running the FlatPak build, then video export simply cannot work since the " +
                            "ffmpeg version they distribute has none of the codecs FamiStudio requires " +
                            "(rawvideo, x264 and MP4 are all missing). Please raise an issue with freedesktop or " +
                            "flatpak directly and ask them to distribute a more complete version in the future.");
                    }

                    ret = false;
                }

                // Favor MP3 if available. According to the ffmpeg doc (and our internal testing), the ffmpeg codecs
                // are, in order of quality:
                //
                //   libopus > libvorbis >= libfdk_aac > libmp3lame >= eac3/ac3 > aac > libtwolame > vorbis > mp2 > wmav2/wmav1
                //
                // https://trac.ffmpeg.org/wiki/Encode/HighQualityAudio

                mp3Support = output.Contains("--enable-libmp3lame");

                if (mp3Support)
                    Log.LogMessage(LogSeverity.Info, "FFmpeg MP3 support detected, will be used for audio compression.");
                else
                    Log.LogMessage(LogSeverity.Info, "FFmpeg MP3 support not detected, will fall back on AAC.");

                process.WaitForExit();
                process.Dispose();

                return ret;
            }
            catch
            {
                mp3Support = false;
                Log.LogMessage(LogSeverity.Error, "Error launching ffmpeg. Make sure the path is correct.");
                return false;
            }
        }
    }
}