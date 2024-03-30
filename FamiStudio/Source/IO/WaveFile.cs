using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Linq;

namespace FamiStudio
{
    public static class WaveFile
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        unsafe struct PCMWaveHeader
        {
            // Riff Wave Header
            public fixed byte chunkId[4];
            public uint chunkSize;
            public fixed byte format[4];
            
            // Format Subchunk
            public fixed byte subChunk1Id[4];
            public uint subChunk1Size;
            public ushort audioFormat;
            public ushort numChannels;
            public uint sampleRate;
            public uint byteRate;
            public ushort blockAlign;
            public ushort bitsPerSample;
            //short int extraParamSize;
            
            // Data Subchunk
            public fixed byte subChunk2Id[4];
            public uint subChunk2Size;
        };

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        unsafe struct FormatSubChunk
        {
            // Format Subchunk with an extension size specifier (non PCM formats)
            public ushort audioFormat;
            public ushort numChannels;
            public uint sampleRate;
            public uint byteRate;
            public ushort blockAlign;
            public ushort bitsPerSample;
            public ushort extraParamSize;
        };
        /* The technically correct way to define GUIDs, but we do not care about 
         * 3 incredibly obscure types of wav encodings that use all of them
         * For more info check libsndfile source code, specifically wavlike.c around line 64 
        unsafe struct GUID
        {
            public uint field1;
            public ushort field2;
            public ushort field3;
            public fixed byte field4[8];
        };
        */

        unsafe struct FormatSubChunkExtension
        {
            // The Format SubChunk is extended according to WAVE_FORMAT_EXTENSIBLE standard
            public ushort validBitsPerSample;
            public ushort speakerPositionMaskLo;    // Doesn't work as a uint
            public ushort speakerPositionMaskHi;    // or an int for some reason
            public ushort audioFormat;
            public fixed byte restOfGUID[14];
            // public GUID audioFormatGUID;
        }

        const ushort WAVE_FORMAT_PCM = 0x0001;
        const ushort WAVE_FORMAT_IEEE_FLOAT = 0x0003;
        const ushort WAVE_FORMAT_EXTENSIBLE = 0xFFFE;

        static readonly byte[] defaultRestOfGUID = 
            { 0x00, 0x00, 0x00, 0x00, 0x10, 0x00, 0x80, 0x00, 0x00, 0xAA, 0x00, 0x38, 0x9B, 0x71 };
        const int FormatSubChunkSize = 18;

        public unsafe static void Save(short[] samples, string filename, int sampleRate, int numChannels)
        {
            using (var file = new FileStream(filename, FileMode.Create))
            {
                var header = new PCMWaveHeader();

                // RIFF WAVE Header
                header.chunkId[0] = (byte)'R';
                header.chunkId[1] = (byte)'I';
                header.chunkId[2] = (byte)'F';
                header.chunkId[3] = (byte)'F';
                header.format[0] = (byte)'W';
                header.format[1] = (byte)'A';
                header.format[2] = (byte)'V';
                header.format[3] = (byte)'E';

                // Format subchunk
                header.subChunk1Id[0] = (byte)'f';
                header.subChunk1Id[1] = (byte)'m';
                header.subChunk1Id[2] = (byte)'t';
                header.subChunk1Id[3] = (byte)' ';
                header.audioFormat = WAVE_FORMAT_PCM; // FOR PCM
                header.numChannels = (ushort)numChannels; // 1 for MONO, 2 for stereo
                header.sampleRate = (uint)sampleRate; // ie 44100 hertz, cd quality audio
                header.bitsPerSample = 16; // 
                header.byteRate = header.sampleRate * header.numChannels * header.bitsPerSample / 8;
                header.blockAlign = (ushort)(header.numChannels * header.bitsPerSample / 8);

                // Data subchunk
                header.subChunk2Id[0] = (byte)'d';
                header.subChunk2Id[1] = (byte)'a';
                header.subChunk2Id[2] = (byte)'t';
                header.subChunk2Id[3] = (byte)'a';

                // All sizes for later:
                // chuckSize = 4 + (8 + subChunk1Size) + (8 + subChubk2Size)
                // subChunk1Size is constanst, i'm using 16 and staying with PCM
                // subChunk2Size = nSamples * nChannels * bitsPerSample/8
                // Whenever a sample is added:
                //    chunkSize += (nChannels * bitsPerSample/8)
                //    subChunk2Size += (nChannels * bitsPerSample/8)
                header.subChunk1Size = 16;
                header.subChunk2Size = (uint)(samples.Length * sizeof(short));
                header.chunkSize = 4 + (8 + header.subChunk1Size) + (8 + header.subChunk2Size);

                var headerBytes = new byte[sizeof(PCMWaveHeader)];
                Marshal.Copy(new IntPtr(&header), headerBytes, 0, headerBytes.Length);
                file.Write(headerBytes);

                var sampleBytes = new byte[samples.Length*2];
                Buffer.BlockCopy(samples, 0, sampleBytes, 0, sampleBytes.Length);
                file.Write(sampleBytes);

            }
        }

        private unsafe static short[] LoadInternal(byte[] bytes, out int sampleRate)
        {
            sampleRate = 0;

            if (bytes != null && bytes.Length >= 12)
            {
                if (bytes[0] == (byte)'R' &&
                    bytes[1] == (byte)'I' &&
                    bytes[2] == (byte)'F' &&
                    bytes[3] == (byte)'F' &&
                    bytes[8] == (byte)'W' &&
                    bytes[9] == (byte)'A' &&
                    bytes[10] == (byte)'V' &&
                    bytes[11] == (byte)'E')
                {
                    // Look for format and data chunks, ignore everything else.
                    var subChunkOffset = 12;
                    var fmtOffset = -1;
                    var dataOffset = -1;
                    var dataSize = -1;

                    while (subChunkOffset + 8 < bytes.Length && (fmtOffset < 0 || dataOffset < 0))
                    {
                        var subChunkSize = BitConverter.ToInt32(bytes, subChunkOffset + 4);

                        if (bytes[subChunkOffset + 0] == (byte)'f' &&
                            bytes[subChunkOffset + 1] == (byte)'m' &&
                            bytes[subChunkOffset + 2] == (byte)'t' &&
                            bytes[subChunkOffset + 3] == (byte)' ')
                        {
                            fmtOffset = subChunkOffset + 8;
                        }
                        else if (bytes[subChunkOffset + 0] == (byte)'d' &&
                                 bytes[subChunkOffset + 1] == (byte)'a' &&
                                 bytes[subChunkOffset + 2] == (byte)'t' &&
                                 bytes[subChunkOffset + 3] == (byte)'a')
                        {
                            dataOffset = subChunkOffset + 8;
                            dataSize = subChunkSize;
                        }

                        subChunkOffset += subChunkSize + 8;
                    }
                    if (fmtOffset >= 0 && dataOffset >= 0)
                    {
                        var fmt = new FormatSubChunk();
                        Marshal.Copy(bytes, fmtOffset, new IntPtr(&fmt), FormatSubChunkSize);
                        // The last 2 bytes don't matter if it's PCM

                        if (fmt.audioFormat == WAVE_FORMAT_EXTENSIBLE)
                        {   //Copy UUID
                            if (fmt.extraParamSize == 22)
                            {   // The only other officially supported option is 0
                                var fmtExt = new FormatSubChunkExtension();
                                Marshal.Copy(bytes, fmtOffset + sizeof(FormatSubChunk), new IntPtr(&fmtExt), fmt.extraParamSize);
                                var GUID = new byte[14];
                                for (int i = 0; i < 14; i++) { GUID[i] = fmtExt.restOfGUID[i]; }
                                if (Enumerable.SequenceEqual(GUID, defaultRestOfGUID)) //Prevents 2 obscure audio formats
                                    fmt.audioFormat = (ushort)fmtExt.audioFormat;   // from passing off as normal PCM/Float
                            }
                        }

                        if ((fmt.audioFormat == WAVE_FORMAT_PCM || fmt.audioFormat == WAVE_FORMAT_IEEE_FLOAT) && fmt.numChannels <= 2)
                        { // Uncompressed PCM or float
                            short[] wavData = null;
                            if (fmt.audioFormat == WAVE_FORMAT_PCM)
                            {
                                fmt.bitsPerSample = (ushort)((fmt.bitsPerSample + 7) & ~0x07);  // Round up to the nearest 8
                                switch (fmt.bitsPerSample)
                                {
                                    case 8:
                                        wavData = new short[dataSize];
                                        for (int i = 0; i < dataSize; i++)
                                            wavData[i] = (short)((bytes[dataOffset + i] << 8) + short.MinValue + bytes[dataOffset + i]);
                                        break;
                                    case 16:
                                        wavData = new short[dataSize >> 1];
                                        fixed (short* p = &wavData[0])
                                            Marshal.Copy(bytes, dataOffset, new IntPtr(p), dataSize);
                                        break;
                                    default:
                                        short divisor = (short)(fmt.bitsPerSample >> 3);
                                        wavData = new short[dataSize / divisor];
                                        for (int i = 0; i < dataSize; i += divisor)
                                            wavData[i / divisor] = (short)((bytes[dataOffset + i + (divisor - 1)] << 8) | bytes[dataOffset + i + (divisor - 2)]);
                                        break;
                                }
                            } else if (fmt.audioFormat == WAVE_FORMAT_IEEE_FLOAT)
                            {
                                switch (fmt.bitsPerSample)
                                {
                                    case 16:    // apparently nonexistent, but idc
                                        wavData = new short[dataSize >> 1];
                                        for (int i = 0; i < dataSize; i += 2)
                                            wavData[i >> 1] = (short)((float)BitConverter.ToHalf(bytes, dataOffset + i) * 32767);
                                        break;
                                    case 32:
                                        wavData = new short[dataSize >> 2];
                                        for (int i = 0; i < dataSize; i += 4)
                                            wavData[i >> 2] = (short)(BitConverter.ToSingle(bytes, dataOffset + i) * 32767);
                                        break;
                                    case 64:
                                        wavData = new short[dataSize >> 3];
                                        for (int i = 0; i < dataSize; i += 8)
                                            wavData[i >> 3] = (short)(BitConverter.ToDouble(bytes, dataOffset + i) * 32767);
                                        break;
                                };
                            }
                            if (fmt.numChannels == 2)
                            {
                                var stereoData = wavData;
                                wavData = new short[wavData.Length / 2];

                                for (int i = 0; i < stereoData.Length; i += 2)
                                    wavData[i / 2] = (short)((stereoData[i + 0] + stereoData[i + 1]) / 2);

                                Log.LogMessage(LogSeverity.Warning, "Wave file is stereo and has been downmixed to mono.");
                            }

                            sampleRate = (int)fmt.sampleRate;
                            return wavData;
                        }
                        else
                        {
                            Log.LogMessage(LogSeverity.Error, "Incompatible wave format. Only uncompressed PCM and 16/32/64-bit float mono and stereo wave files are supported.");
                            return null;
                        }
                    }
                    else
                    {
                        Log.LogMessage(LogSeverity.Error, "Cannot find format and data chunks. Make sure the file is a valid WAV file and is not corrupted.");
                        return null;
                    }
                }
            }

            Log.LogMessage(LogSeverity.Error, "Invalid WAV file header. Make sure the file is a valid WAV file and is not corrupted.");
            return null;
        }

        public unsafe static short[] Load(string filename, out int sampleRate)
        {
            sampleRate = 0;

            try
            {
                return LoadInternal(File.ReadAllBytes(filename), out sampleRate);
            }
            catch
            {
            }

            return null;
        }

        public unsafe static short[] LoadFromResource(string resourceName, out int sampleRate)
        {
            sampleRate = 0;

            try
            {
                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
                {
                    var bytes = new byte[stream.Length];
                    stream.Read(bytes);
                    return LoadInternal(bytes, out sampleRate);
                }
            }
            catch
            {
            }

            return null;
        }
    }
}
