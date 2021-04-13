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
        private int songDuration;
        private int numTracks;

        class TextEvent
        {
            public int track;
            public int tick;
            public int type;
            public string text;
        }

        class TimeSignatureEvent
        {
            public int tick;
            public int numer;
            public int denom;
        }

        class TempoEvent
        {
            public int tick;
            public int tempo;
        }

        class NoteEvent
        {
            public int tick;
            public int channel;
            public int note;
            public bool on;
        };

        List<TextEvent>          textEvents          = new List<TextEvent>();
        List<TimeSignatureEvent> timeSignatureEvents = new List<TimeSignatureEvent>();
        List<TempoEvent>         tempoEvents         = new List<TempoEvent>();
        List<NoteEvent>          noteEvents          = new List<NoteEvent>();

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

            Debug.Assert(type != 2);

            // TODO!
            Debug.Assert((ticks & 0x8000) == 0);

            ticksPerQuarterNote = ticks;

            Debug.WriteLine($"Number of ticks per quarter note {ticks}.");

            return true;
        }

        private void ReadMetaEvent(int track, int time)
        {
            var metaType = bytes[idx++];

            switch (metaType)
            {
                // Various text messages.
                // MIDITODO: Store Read 01/02.
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

                    var textEvent = new TextEvent();
                    textEvent.track = track;
                    textEvent.tick = time;
                    textEvent.text = name;
                    textEvent.type = metaType;
                    textEvents.Add(textEvent);
                    break;
                }
                // MIDI Channel Prefix
                case 0x20:
                {
                    Debug.Assert(bytes[idx] == 0x01); // Not sure why this is needed.
                    idx += 2;
                    break;
                }

                // MIDI port
                case 0x21:
                {
                    Debug.Assert(bytes[idx] == 0x01); // Not sure why this is needed.
                        idx += 2;
                        break;
                }

                // Track end
                case 0x2f:
                {
                    Debug.Assert(bytes[idx] == 0x00); // Not sure why this is needed.
                    idx++;
                    songDuration = Math.Max(songDuration, time);
                    break;
                }

                // Tempo change.
                case 0x51:
                {
                    Debug.Assert(bytes[idx] == 0x03); // Not sure why this is needed.
                    idx++;
                    var tempo = ReadInt24();
                    Debug.WriteLine($"At time {time} tempo is now {tempo}.");

                    var tempoEvent = new TempoEvent();
                    tempoEvent.tick = time;
                    tempoEvent.tempo = tempo;
                    tempoEvents.Add(tempoEvent);
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
                    idx += 2; // WTF is that.
                    Debug.WriteLine($"At time {time} time signature is now {numer} / {denom}.");

                    var timeSignature = new TimeSignatureEvent();
                    timeSignature.tick = time;
                    timeSignature.numer = numer;
                    timeSignature.denom = denom;
                    timeSignatureEvents.Add(timeSignature);

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

            // Note ON / OFF
            if (statusHiByte == 0b1001 ||
                statusHiByte == 0b1000)
            {
                var key = bytes[idx++];
                var vel = bytes[idx++];

                //Debug.WriteLine($"At time {time} : NOTE ON! {Note.GetFriendlyName(key - 11)} vel {vel}.");

                var noteEvent = new NoteEvent();
                noteEvent.tick = time;
                noteEvent.channel = status & 0x0f;
                noteEvent.note = key - 11;
                noteEvent.on = statusHiByte == 0b1001;
                noteEvents.Add(noteEvent);
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

        private bool ReadTrackChunk(int track, int chunkLen)
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
                    ReadMetaEvent(track, time);
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
            var track = 0;

            while (idx < bytes.Length)
            {
                var chunkType = Encoding.ASCII.GetString(bytes, idx, 4); idx += 4;
                var chunkLen = ReadInt32();

                switch (chunkType)
                {
                    case "MTrk":
                        ReadTrackChunk(track, chunkLen);
                        track++;
                        break;
                    default:
                        Debug.WriteLine($"Skipping unknown chunk type {chunkType} or length {chunkLen}");
                        idx += chunkLen;
                        break;
                }
            }

            timeSignatureEvents.Sort((s1, s2) => s1.tick.CompareTo(s2.tick));
            tempoEvents.Sort((t1, t2) => t1.tick.CompareTo(t2.tick));

            numTracks = track;

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

        private void CreatePatterns(out List<MidiPatternInfo> patternInfos)
        {
            Debug.Assert(songDuration >= 0);

            // 4/4 by default.
            if (timeSignatureEvents.Count == 0 || 
                timeSignatureEvents[0].tick != 0)
            {
                timeSignatureEvents.Insert(0, new TimeSignatureEvent() { numer = 4, denom = 4 });
            }

            // 120 BPM by default.
            if (tempoEvents.Count == 0 || 
                tempoEvents[0].tick != 0)
            {
                tempoEvents.Insert(0, new TempoEvent() { tempo = 500000 });
            }

            // Setup default song settings.
            var initialBpm   = MicroSecondsToBPM(tempoEvents[0].tempo);
            var initialTempo = GetClosestMatchingTempo(initialBpm, 4);

            song.ChangeFamiStudioTempoGroove(initialTempo.groove, false);
            song.SetBeatLength(song.NoteLength * 4);
            song.SetDefaultPatternLength(song.BeatLength * timeSignatureEvents[0].numer);

            var defaultNumer = timeSignatureEvents[0].numer;
            var defaultDenom = timeSignatureEvents[0].denom;
            var defaultTempo = tempoEvents[0].tempo;

            var time = 0;
            var patternIdx = 0;
            var tempo = defaultTempo;
            var numer = defaultNumer;
            var denom = defaultDenom;

            patternInfos = new List<MidiPatternInfo>();

            while (time < songDuration)
            {
                var ratio = (4.0 / denom);

                if (numer != defaultNumer ||
                    denom != defaultDenom ||
                    tempo != defaultTempo)
                {
                    var patternBpm   = MicroSecondsToBPM(tempo);
                    var patternTempo = GetClosestMatchingTempo(patternBpm, 4);
                    var noteLength   = Utils.Min(patternTempo.groove);

                    song.SetPatternCustomSettings(patternIdx, (int)Math.Round(noteLength * 4 * numer * ratio), noteLength * 4, patternTempo.groove);
                }

                var patternInfo = new MidiPatternInfo();
                patternInfo.tick = time;
                patternInfo.denom = denom;
                patternInfo.numer = numer;
                patternInfos.Add(patternInfo);

                var lastTime = time;

                // Advance by one bar.
                time = (int)(time + ticksPerQuarterNote * numer * ratio);
                patternIdx++;

                // Look for any tempo change between the last pattern and now.
                // We will only allow tempo changes on pattern boundaries for now.
                var tempoChangeIdx = -1;
                for (int i = tempoEvents.Count - 1; i >= 0; i--)
                {
                    if (tempoEvents[i].tick > lastTime &&
                        tempoEvents[i].tick <= time)
                    {
                        tempoChangeIdx = i;
                        break;
                    }
                }

                if (tempoChangeIdx >= 0)
                    tempo = tempoEvents[tempoChangeIdx].tempo;

                // Look for another time signature change.
                var timeSignatureIdx = timeSignatureEvents.FindIndex(s => s.tick == time);
                if (timeSignatureIdx >= 0)
                {
                    numer = timeSignatureEvents[timeSignatureIdx].numer;
                    denom = timeSignatureEvents[timeSignatureIdx].denom;
                }

                if (patternIdx == 256)
                {
                    // MIDITODO : Error message here.
                    break;
                }
            }

            song.SetLength(patternIdx);
        }

        private void CreateNotes(List<MidiPatternInfo> patternInfos)
        {
            for (int i = 0; i < noteEvents.Count; i++)
            {
                var evt = noteEvents[i];

                // MIDITODO : Show warning if note isnt supported.
                if (//evt.on &&
                    //evt.channel == 3 && // MATTT
                    evt.note >= Note.MusicalNoteMin &&
                    evt.note <= Note.MusicalNoteMax)
                {
                    var patternIdx = -1;
                    for (int j = 0; j < patternInfos.Count; j++)
                    {
                        if (evt.tick >= patternInfos[j].tick && (j == patternInfos.Count - 1 || evt.tick < patternInfos[j + 1].tick))
                        {
                            patternIdx = j;
                            break;
                        }
                    }
                    Debug.Assert(patternIdx >= 0);

                    var pattern = song.Channels[0].PatternInstances[patternIdx];
                    if (pattern == null)
                    {
                        pattern = song.Channels[0].CreatePattern();
                        song.Channels[0].PatternInstances[patternIdx] = pattern;
                    }

                    var patternInfo   = patternInfos[patternIdx];
                    var tickInPattern = evt.tick - patternInfo.tick;
                    var noteIndex     = tickInPattern / (double)ticksPerQuarterNote * (4.0 / patternInfo.denom);
                    var noteLength    = song.GetPatternNoteLength(patternIdx);
                    var beatLength    = song.GetPatternBeatLength(patternIdx);

                    pattern.Notes[(int)Math.Round(beatLength * noteIndex)] = new Note(evt.on ? evt.note : Note.NoteStop);
                }
            }
        }

        public string[] GetTrackNames(string filename)
        {
            idx = 0;
            bytes = File.ReadAllBytes(filename);

            if (!ReadHeaderChunk())
                return null;

            ReadAllTracks();

            var trackNames = new Dictionary<int, string>();

            foreach (var textEvent in textEvents)
            {
                if (textEvent.type == 3)
                    trackNames[textEvent.track] = textEvent.text;
            }

            var names = new string[numTracks];
            
            foreach (var kv in trackNames)
                names[kv.Key] = kv.Value;

            return names;
        }

        public Project Load(string filename)
        {
#if !DEBUG
            try
#endif
            {
                idx = 0;
                bytes = File.ReadAllBytes(filename);

                if (!ReadHeaderChunk())
                    return null;

                project = new Project();
                song = project.CreateSong();

                ReadAllTracks();

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
