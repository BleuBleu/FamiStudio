using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace FamiStudio
{
    class VideoEncoderAndroid
    {
        private Process process;
        private BinaryWriter stream;

        private VideoEncoderAndroid()
        {
        }

        public static VideoEncoderAndroid CreateInstance()
        {
            /*
            if (DetectFFmpeg(Settings.FFmpegExecutablePath))
            {
                return new VideoEncoderFFmpeg();
            }
            else
            {
                return null;
            }
            */

            return null;
        }

        public void BeginEncoding(int resX, int resY, int frameRateNumer, int frameRateDenom, int videoBitRate, int audioBitRate, string audioFile, string outputFile)
        {
        }

        public void AddFrame(byte[] image)
        {
        }

        public void EndEncoding()
        {
        }
    }
}