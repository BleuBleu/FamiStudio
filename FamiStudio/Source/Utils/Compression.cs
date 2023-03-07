using System;
using System.IO;
using System.IO.Compression;

namespace FamiStudio
{
    public static class Compression
    {
        public static byte[] CompressBytes(byte[] buffer, CompressionLevel level)
        {
            var stream = new MemoryStream(buffer.Length);
            stream.Write(BitConverter.GetBytes(buffer.Length), 0, 4);
            DeflateStream compStream = new DeflateStream(stream, level, true);
            compStream.Write(buffer, 0, buffer.Length);
            compStream.Close();
            var compressedBuffer = new byte[stream.Length];
            Array.Copy(stream.GetBuffer(), compressedBuffer, compressedBuffer.Length);
            stream.Close();
            return compressedBuffer;
        }

        public static byte[] DecompressBytes(byte[] compressedBuffer, int offset = 0)
        {
            var inputStream = new MemoryStream(compressedBuffer);
            inputStream.Seek(offset, SeekOrigin.Begin);
            var bytes = new byte[4];
            inputStream.Read(bytes, 0, 4);
            var decompressedSize = BitConverter.ToInt32(bytes, 0);
            DeflateStream compStream = new DeflateStream(inputStream, CompressionMode.Decompress);
            var buffer = new byte[decompressedSize];
            var totalRead = 0;
            while (totalRead < buffer.Length)
            {
                var read = compStream.Read(buffer, totalRead, buffer.Length - totalRead);
                if (read == 0) break;
                totalRead += read;
            }
            compStream.Close();
            return buffer;
        }
    }
}
