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
        private Project project;
        private Song song;

        private int tempoMode = TempoType.FamiStudio;
        private int ticksPerQuarterNote = 96;
        private int trackStartIdx;

        private delegate void MetaEventTextDelegate(string text);
        private delegate void MetaEventEndTrackDelegate(int time);
        private delegate void MetaEventTempoDelegate(int time, int tempo);
        private delegate void MetaEventTimeSignatureDelegate(int time, int numer, int denom);
        private delegate void MidiMessageNoteDelegate(int time, int note, bool on);

        private MetaEventTextDelegate          textEvent          = null;
        private MetaEventEndTrackDelegate      endTrack           = null;
        private MetaEventTempoDelegate         tempoEvent         = null;
        private MetaEventTimeSignatureDelegate timeSignatureEvent = null;
        private MidiMessageNoteDelegate        noteMessage        = null;

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
            // Big endian.
            Debug.Assert(BitConverter.IsLittleEndian);
            tmp[0] = bytes[idx + 3];
            tmp[1] = bytes[idx + 2];
            tmp[2] = bytes[idx + 1];
            tmp[3] = bytes[idx + 0];
            var i = BitConverter.ToInt32(tmp, 0);
            idx += 4;
            return i;
        }

        private int ReadInt24()
        {
            // Big endian.
            Debug.Assert(BitConverter.IsLittleEndian);
            tmp[0] = bytes[idx + 2];
            tmp[1] = bytes[idx + 1];
            tmp[2] = bytes[idx + 0];
            var i = BitConverter.ToInt32(tmp, 0);
            idx += 3;
            return i;
        }

        private int ReadInt16()
        {
            Debug.Assert(BitConverter.IsLittleEndian);
            tmp[0] = bytes[idx + 1];
            tmp[1] = bytes[idx + 0];
            var i = BitConverter.ToInt16(tmp, 0);
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

            // TODO!
            Debug.Assert((ticks & 0x8000) == 0);

            ticksPerQuarterNote = ticks;
            trackStartIdx = idx;

            Debug.WriteLine($"Number of ticks per quarter note {ticks}.");

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
                    textEvent?.Invoke(name);
                    break;
                }
                
                // Track end
                case 0x2f:
                {
                    Debug.Assert(bytes[idx] == 0x00); // Not sure why this is needed.
                    idx++;
                    endTrack?.Invoke(time);
                    break;
                }

                // Tempo change.
                case 0x51:
                {
                    Debug.Assert(bytes[idx] == 0x03); // Not sure why this is needed.
                    idx++;
                    var tempo = ReadInt24();
                    Debug.WriteLine($"At time {time} tempo is now {tempo}.");
                    tempoEvent?.Invoke(time, tempo);
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
                    timeSignatureEvent?.Invoke(time, numer, denom);
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
                noteMessage?.Invoke(time, key, true);
            }

            // Note OFF
            else if (statusHiByte == 0b1000)
            {
                var key = bytes[idx++];
                var vel = bytes[idx++];

                noteMessage?.Invoke(time, key, false);
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

        private bool ReadAllTracks()
        {
            idx = trackStartIdx;

            while (idx < bytes.Length)
            {
                var chunkType = Encoding.ASCII.GetString(bytes, idx, 4); idx += 4;
                var chunkLen = ReadInt32();

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
            
            textEvent          = null;
            endTrack           = null;
            tempoEvent         = null;
            timeSignatureEvent = null;
            noteMessage        = null;

            return true;
        }

        private double MicroSecondsToBPM(int ms)
        {
            return 60000000.0 / ms;
        }

        private TempoInfo GetClosestMatchingTempo(double bpm, int notesPerBeat)
        {
            var tempos = FamiStudioTempoUtils.GetAvailableTempos(false, notesPerBeat); // MIDITODO : PAL

            var bestTempoDiff = 1000.0;
            var bestTempo = (TempoInfo)null;

            foreach (var t in tempos)
            {
                var diff = Math.Abs(bpm - t.bpm);

                if (diff < bestTempoDiff)
                {
                    bestTempo = t;
                    bestTempoDiff = diff;
                }
            }

            return bestTempo;
        }

        class MidiPatternInfo
        {
            public int tick;
            public int numer;
            public int denom;
        };

        class MidiNoteInfo
        {
            public int tick;
            public int note;
            public bool on;
        };

        private void CreatePatterns(out List<MidiPatternInfo> patternInfos)
        {
            var timeSignatureChanges = new SortedList<int, Tuple<int, int>>();
            var tempoChanges         = new SortedList<int, int>();
            var songDuration         = -1;

            timeSignatureEvent = (t, n, d) => timeSignatureChanges.Add(t, new Tuple<int, int>(n, d));
            tempoEvent         = (t, tp) => tempoChanges.Add(t, tp);
            endTrack           = (t) => songDuration = Math.Max(songDuration, t);

            ReadAllTracks();

            Debug.Assert(songDuration >= 0);

            // 4/4 by default.
            if (!timeSignatureChanges.ContainsKey(0))
                timeSignatureChanges.Add(0, new Tuple<int, int>(4, 4)); 

            // 120 BPM by default.
            if (!tempoChanges.ContainsKey(0))
                tempoChanges.Add(0, 500000);

            // Setup default song settings.
            var initialBpm   = MicroSecondsToBPM(tempoChanges[0]);
            var initialTempo = GetClosestMatchingTempo(initialBpm, timeSignatureChanges[0].Item2);

            song.ChangeFamiStudioTempoGroove(initialTempo.groove, false);
            song.SetBeatLength(song.NoteLength * timeSignatureChanges[0].Item2);
            song.SetDefaultPatternLength(song.BeatLength * timeSignatureChanges[0].Item1);

            var defaultNumer = timeSignatureChanges[0].Item1;
            var defaultDenom = timeSignatureChanges[0].Item2;
            var defaultTempo = tempoChanges[0];

            var time = 0;
            var patternIdx = 0;
            var tempo = defaultTempo;
            var numer = defaultNumer;
            var denom = defaultDenom;

            patternInfos = new List<MidiPatternInfo>();

            while (time < songDuration)
            {
                if (numer != defaultNumer ||
                    denom != defaultDenom ||
                    tempo != defaultTempo)
                {
                    var patternBpm   = MicroSecondsToBPM(tempo);
                    var patternTempo = GetClosestMatchingTempo(patternBpm, denom);
                    var noteLength   = Utils.Min(patternTempo.groove);

                    song.SetPatternCustomSettings(patternIdx, noteLength * denom * numer, noteLength * denom, patternTempo.groove);
                }

                var patternInfo = new MidiPatternInfo();
                patternInfo.tick = time;
                patternInfo.denom = denom;
                patternInfo.numer = numer;
                patternInfos.Add(patternInfo);

                var lastTime = time;

                // Advance by one bar.
                time = (int)(time + ticksPerQuarterNote * numer * (4.0 / denom));
                patternIdx++;

                // Look for any tempo change between the last pattern and now.
                // We will only allow tempo changes on pattern boundaries for now.
                var tempoChangeIdx = -1;
                for (int i = tempoChanges.Keys.Count - 1; i >= 0; i--)
                {
                    if (tempoChanges.Keys[i] > lastTime &&
                        tempoChanges.Keys[i] <= time)
                    {
                        tempoChangeIdx = i;
                        break;
                    }
                }

                if (tempoChangeIdx >= 0)
                    tempo = tempoChanges.Values[tempoChangeIdx];

                // Look for another time signature change.
                if (timeSignatureChanges.TryGetValue(time, out var foundTimeSignature))
                {
                    numer = foundTimeSignature.Item1;
                    denom = foundTimeSignature.Item2;
                }
            }

            song.SetLength(patternIdx);
        }

        private void CreateNotes(List<MidiPatternInfo> patternInfos)
        {
            var notes = new List<MidiNoteInfo>();
            noteMessage = (t, n, o) => notes.Add(new MidiNoteInfo() { tick = t, note = n - 11, on = o });
            ReadAllTracks();

            for (int i = 0; i < notes.Count; i++)
            {
                var noteInfo = notes[i];

                if (noteInfo.on &&
                    noteInfo.note >= Note.MusicalNoteMin &&
                    noteInfo.note <= Note.MusicalNoteMax)
                {
                    var patternIdx = -1;
                    for (int j = 0; j < patternInfos.Count; j++)
                    {
                        if (noteInfo.tick >= patternInfos[j].tick && (j == patternInfos.Count - 1 || noteInfo.tick < patternInfos[j + 1].tick))
                        {
                            patternIdx = j;
                            break;
                        }
                    }
                    Debug.Assert(patternIdx >= 0);

                    var patternInfo   = patternInfos[patternIdx];
                    var tickInPattern = noteInfo.tick - patternInfo.tick;
                    var noteIndex     = tickInPattern / ticksPerQuarterNote * (4.0 / patternInfo.denom);

                    var pattern = song.Channels[0].PatternInstances[patternIdx];
                    if (pattern == null)
                    {
                        pattern = song.Channels[0].CreatePattern();
                        song.Channels[0].PatternInstances[patternIdx] = pattern;
                    }

                    var noteLength = song.GetPatternNoteLength(patternIdx);
                    var beatLength = song.GetPatternBeatLength(patternIdx);

                    pattern.Notes[(int)Math.Round(beatLength * noteIndex)] = new Note(noteInfo.note);
                }
            }
        }

        public Project Load(string filename)
        {
#if !DEBUG
            try
#endif
            {
                bytes = File.ReadAllBytes(filename);

                if (!ReadHeaderChunk())
                    return null;

                project = new Project();
                song = project.CreateSong();

                // First create the pattern based on time signatures/tempos.
                CreatePatterns(out var patternInfos);

                // Then create the notes.
                CreateNotes(patternInfos);

                return project;
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
