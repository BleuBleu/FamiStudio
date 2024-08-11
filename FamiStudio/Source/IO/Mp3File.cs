using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FamiStudio
{
    class Mp3File
    {
        private const string ShineMp3Dll = Platform.DllStaticLib ? "__Internal" : Platform.DllPrefix + "ShineMp3" + Platform.DllExtension;

        [DllImport(ShineMp3Dll, CallingConvention = CallingConvention.StdCall, EntryPoint = "ShineMp3Encode")]
        extern static int ShineMp3Encode(int wav_rate, int wav_channels, int wav_num_samples, IntPtr wavData, int mp3_bitrate, int mp3_data_size, IntPtr mp3_data);

        public unsafe static bool Save(short[] wavData, string filename, int sampleRate, int bitRate, int numChannels)
        {
            if (sampleRate < 44100)
            {
                sampleRate = 44100;
                Log.LogMessage(LogSeverity.Warning, $"Sample rate of {sampleRate}Hz is too low for MP3. Forcing 44100Hz.");
            }

            var mp3Data = new byte[wavData.Length * sizeof(short) * 4];
            var mp3Size = 0;

            fixed (short* wavPtr = &wavData[0])
            {
                fixed (byte* mp3Ptr = &mp3Data[0])
                {
                    mp3Size = ShineMp3Encode(sampleRate, numChannels, wavData.Length, new IntPtr(wavPtr), bitRate, mp3Data.Length, new IntPtr(mp3Ptr));
                }
            }

            if (mp3Size <= 0)
                return false;

            Array.Resize(ref mp3Data, mp3Size);
            File.WriteAllBytes(filename, mp3Data);

            return true;
        }
    }
}
