using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FamiStudio
{
    class MidiFile
    {
        private int idx;
        private byte[] bytes;
        private byte[] tmp = new byte[4];

        private int ReadVarLen()
        {
            int value = bytes[idx++];

            if ((value & 0x80) != 0)
            {
                value &= 0x7f;

                byte c;
                do
                {
                    c = bytes[idx++];
                    value = (value << 7) + (c & 0x7f);
                }
                while ((c & 0x80) != 0);
            }

            return value;
        }

        private int ReadInt32()
        {
            Array.Reverse(bytes, idx, 4); // Big endian.
            var i = BitConverter.ToInt32(bytes, idx);
            idx += 4;
            return i;
        }

        private int ReadInt24()
        {
            tmp[0] = bytes[idx + 2];
            tmp[1] = bytes[idx + 1];
            tmp[2] = bytes[idx + 0];
            var i = BitConverter.ToInt32(tmp, 0);
            idx += 3;
            return i;
        }

        private int ReadInt16()
        {
            Array.Reverse(bytes, idx, 2); // Big endian.
            var i = BitConverter.ToInt16(bytes, idx);
            idx += 2;
            return i;
        }

        private bool ReadHeaderChunk()
        {
            var chunkType = Encoding.ASCII.GetString(bytes, idx, 4); idx += 4;

            if (chunkType != "MThd")
                return false;

            var chunkLen  = ReadInt32();
            var type      = ReadInt16();
            var numTracks = ReadInt16();
            var ticks     = ReadInt16();

            Debug.WriteLine($"Number of ticks per quarter note {ticks}.");

            // TODO!
            Debug.Assert((ticks & 0x8000) == 0);

            return true;
        }

        private void ReadMetaEvent(int time)
        {
            var metaType = bytes[idx++];

            switch (metaType)
            {
                // Various text messages.
                case 0x01:
                case 0x02:
                case 0x03:
                case 0x04:
                case 0x05:
                case 0x06:
                case 0x07:
                case 0x08:
                case 0x09:
                {
                    var len  = ReadVarLen();
                    var name = Encoding.ASCII.GetString(bytes, idx, len); idx += len;
                    break;
                }
                
                // Track end
                case 0x2f:
                {
                    Debug.Assert(bytes[idx] == 0x00); // Not sure why this is needed.
                    idx++;
                    break;
                }

                // Tempo change.
                case 0x51:
                {
                    Debug.Assert(bytes[idx] == 0x03); // Not sure why this is needed.
                    idx++;
                    var tempo = ReadInt24();
                    Debug.WriteLine($"At time {time} tempo is now {tempo}.");
                    break;
                }

                // SMPTE Offset
                case 0x54:
                {
                    Debug.Assert(bytes[idx] == 0x05); // Not sure why this is needed.
                    idx++;
                    var hr = bytes[idx++];
                    var mn = bytes[idx++];
                    var se = bytes[idx++];
                    var fr = bytes[idx++];
                    var ff = bytes[idx++];
                    break;
                }

                // Time signature.
                case 0x58:
                {
                    Debug.Assert(bytes[idx] == 0x04); // Not sure why this is needed.
                    idx++;
                    var numer = bytes[idx++];
                    var denom = 1 << bytes[idx++];
                    Debug.WriteLine($"At time {time} time signature is now {numer} / {denom}.");
                    idx += 2; // WTF is that.
                    break;
                }

                // Key signature.
                case 0x59:
                {
                    Debug.Assert(bytes[idx] == 0x02); // Not sure why this is needed.
                    idx++;
                    var sf = bytes[idx++];
                    var mi = bytes[idx++];
                    break;
                }

                // Special requirement
                case 0x7f:
                {
                    var len = ReadVarLen();
                    idx += len;
                    break;
                }

                default:
                {
                    Debug.Assert(false, $"Unknown meta event {metaType}");
                    break;
                }
            }
        }

        private bool ReadMidiMessage(int time, ref byte status)
        {
            // Do we have a status byte?
            if ((bytes[idx] & 0x80) != 0)
            {
                status = bytes[idx++];
            }

            var statusHiByte = status >> 4;

            // Note ON
            if (statusHiByte == 0b1001)
            {
                var key = bytes[idx++];
                var vel = bytes[idx++];

                Debug.WriteLine($"At time {time} : NOTE ON! {Note.GetFriendlyName(key - 11)} vel {vel}.");
            }

            // Note OFF
            else if (statusHiByte == 0b1000)
            {
                var key = bytes[idx++];
                var vel = bytes[idx++];
            }

            // Channel pressure
            else if (statusHiByte == 0b1101)
            {
                var pressure = bytes[idx++];
            }

            // Pitch wheel
            else if (statusHiByte == 0b1110)
            {
                var lsb = bytes[idx++];
                var msb = bytes[idx++];
            }

            // Control change
            else if (statusHiByte == 0b1011)
            {
                var ctrl = bytes[idx++];
                var val  = bytes[idx++];
            }

            // Program change
            else if (statusHiByte == 0b1100)
            {
                var prg = bytes[idx++];
            }

            // System exclusive
            else if (status == 0b11110000)
            {
                while (bytes[idx++] != 0b11110111);
            }
            else
            {
                Debug.Assert(false, $"Unknown status {status}");
            }
            //Debug.Assert(false, $"Unknown event {evt}");

            return true;
        }

        private bool ReadTrackChunk(int chunkLen)
        {
            var endIdx = idx + chunkLen;
            var status = (byte)0;
            var time = 0;

            while (idx < endIdx)
            {
                var delta = ReadVarLen();
                var evt = bytes[idx];

                time += delta;

                // Meta event
                if (evt == 0xff)
                {
                    idx++;
                    ReadMetaEvent(time);
                }
                else
                {
                    ReadMidiMessage(time, ref status);
                }
            }
            
            return true;
        }

        public Project Load(string filename)
        {
#if !DEBUG
            try
#endif
            {
                bytes = File.ReadAllBytes(filename);

                if (!ReadHeaderChunk())
                {
                    return null;
                }

                while (idx < bytes.Length)
                {
                    var chunkType = Encoding.ASCII.GetString(bytes, idx, 4); idx += 4;
                    var chunkLen  = ReadInt32();

                    switch (chunkType)
                    {
                        case "MTrk":
                            ReadTrackChunk(chunkLen);
                            break;
                        default:
                            Debug.WriteLine($"Skipping unknown chunk type {chunkType} or length {chunkLen}");
                            idx += chunkLen;
                            break;
                    }
                }

                return null;
            }
#if !DEBUG
            catch (Exception e)
            {
                Log.LogMessage(LogSeverity.Error, "Please contact the developer on GitHub!");
                Log.LogMessage(LogSeverity.Error, e.Message);
                Log.LogMessage(LogSeverity.Error, e.StackTrace);
                return false;
            }
#endif
    }
}
}
