using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FamiStudio
{
    class VorbisFile
    {
        private const string VorbisDll = Platform.DllPrefix + "Vorbis" + Platform.DllExtension;

        [DllImport(VorbisDll, CallingConvention = CallingConvention.StdCall, EntryPoint = "VorbisOggEncode")]
        extern static int VorbisOggEncode(int wav_rate, int wav_channels, int wav_num_samples, IntPtr wav_data, int ogg_bitrate, int ogg_data_size, IntPtr ogg_data);

        public unsafe static bool Save(short[] wavData, string filename, int sampleRate, int bitRate, int numChannels)
        {
            var oggData = new byte[wavData.Length * sizeof(short) * 4];
            var oggSize = 0;

            fixed (short* wavPtr = &wavData[0])
            {
                fixed (byte* mp3Ptr = &oggData[0])
                {
                    oggSize = VorbisOggEncode(sampleRate, numChannels, wavData.Length, new IntPtr(wavPtr), bitRate, oggData.Length, new IntPtr(mp3Ptr));
                }
            }

            if (oggSize <= 0)
                return false;

            Array.Resize(ref oggData, oggSize);
            File.WriteAllBytes(filename, oggData);

            return true;
        }
    }
}
