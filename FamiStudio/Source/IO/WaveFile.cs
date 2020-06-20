using System;
using System.Collections.Generic;
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

        public unsafe static void Save(Song song, string filename, int sampleRate, int duration, int channelMask)
        {
            var project = song.Project;
            var player = new WavPlayer(sampleRate);
            player.Loop = duration <= 0 ? LoopMode.None : LoopMode.Song;
            player.ChannelMask = channelMask;

            var samples = player.GetSongSamples(song, project.PalMode, duration);

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
    }
}
