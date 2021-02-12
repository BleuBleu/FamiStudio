﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace FamiStudio
{
    public static class WaveFile
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        unsafe struct WaveHeader
        {
            // Riff Wave Header
            public fixed byte chunkId[4];
            public int chunkSize;
            public fixed byte format[4];
            
            // Format Subchunk
            public fixed byte subChunk1Id[4];
            public int subChunk1Size;
            public short audioFormat;
            public short numChannels;
            public int sampleRate;
            public int byteRate;
            public short blockAlign;
            public short bitsPerSample;
            //short int extraParamSize;
            
            // Data Subchunk
            public fixed byte subChunk2Id[4];
            public int subChunk2Size;
        };

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        unsafe struct FormatSubChunk
        {
            // Format Subchunk
            public short audioFormat;
            public short numChannels;
            public int sampleRate;
            public int byteRate;
            public short blockAlign;
            public short bitsPerSample;
        };

        const int FormatSubChunkSize = 16;

        public unsafe static void Save(Song song, string filename, int sampleRate, int loopCount, int duration, int channelMask)
        {
            var project = song.Project;
            var player = new WavPlayer(sampleRate, loopCount, channelMask);
            var samples = player.GetSongSamples(song, project.PalMode, duration);

            Save(samples, filename, sampleRate);
        }

        public unsafe static void Save(short[] samples, string filename, int sampleRate)
        {
            using (var file = new FileStream(filename, FileMode.Create))
            {
                var header = new WaveHeader();

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
                header.audioFormat = 1; // FOR PCM
                header.numChannels = 1; // 1 for MONO, 2 for stereo
                header.sampleRate = sampleRate; // ie 44100 hertz, cd quality audio
                header.bitsPerSample = 16; // 
                header.byteRate = header.sampleRate * header.numChannels * header.bitsPerSample / 8;
                header.blockAlign = (short)(header.numChannels * header.bitsPerSample / 8);

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
                header.subChunk2Size = samples.Length * sizeof(short);
                header.chunkSize = 4 + (8 + header.subChunk1Size) + (8 + header.subChunk2Size);

                var headerBytes = new byte[sizeof(WaveHeader)];
                Marshal.Copy(new IntPtr(&header), headerBytes, 0, headerBytes.Length);
                file.Write(headerBytes, 0, headerBytes.Length);

                // So lame.
                foreach (var s in samples)
                    file.Write(BitConverter.GetBytes(s), 0, sizeof(short));
            }
        }

        public unsafe static short[] Load(string filename, out int sampleRate)
        {
            try
            {
                var bytes = File.ReadAllBytes(filename);

                if (bytes != null && bytes.Length >= 12)
                {
                    if (bytes[0]  == (byte)'R' &&
                        bytes[1]  == (byte)'I' &&
                        bytes[2]  == (byte)'F' &&
                        bytes[3]  == (byte)'F' &&
                        bytes[8]  == (byte)'W' &&
                        bytes[9]  == (byte)'A' &&
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

                            if (fmt.audioFormat == 1 && fmt.bitsPerSample == 16 && fmt.numChannels <= 2)
                            {
                                short[] wavData = new short[dataSize / sizeof(short)];
                                fixed (short* p = &wavData[0])
                                    Marshal.Copy(bytes, dataOffset, new IntPtr(p), dataSize);

                                if (fmt.numChannels == 2)
                                {
                                    var stereoData = wavData;
                                    wavData = new short[wavData.Length / 2];

                                    for (int i = 0; i < stereoData.Length; i += 2)
                                        wavData[i / 2] = (short)((stereoData[i + 0] + stereoData[i + 1]) / 2);

                                    Log.LogMessage(LogSeverity.Warning, "Wave file is stereo and has been downmixed to mono.");
                                }

                                sampleRate = fmt.sampleRate;
                                return wavData;
                            }
                            else
                            {
                                Log.LogMessage(LogSeverity.Error, "Incompatible wave format. Only 16-bit, uncompressed, mono and stereo wave files are supported.");
                            }
                        }
                    }
                }
            }
            catch
            {
            }

            sampleRate = 0;
            return null;
        }
    }
}
