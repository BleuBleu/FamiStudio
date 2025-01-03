using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FamiStudio
{
    class MidiFileReader
    {
        private int idx;
        private byte[] bytes;
        private byte[] tmp = new byte[4];
        private Project project;
        private Song song;
        private int polyphonyWarningCount;

        private int ticksPerQuarterNote = 96;
        private int songDuration;
        private int numTracks;

        private Dictionary<int, Instrument>[] instrumentMap  = new Dictionary<int, Instrument>[ExpansionType.Count];

        private const int MinDrumKey = 24;
        private const int MaxDrumKey = 70;

        public static readonly string[] MidiInstrumentNames =
        {
            "Acoustic Grand Piano",
            "Bright Acoustic Piano",
            "Electric Grand Piano",
            "Honky-tonk Piano",
            "Electric Piano 1 (Rhodes Piano)",
            "Electric Piano 2 (Chorused Piano)",
            "Harpsichord",
            "Clavinet",
            "Celesta",
            "Glockenspiel",
            "Music Box",
            "Vibraphone",
            "Marimba",
            "Xylophone",
            "Tubular Bells",
            "Dulcimer (Santur)",
            "Drawbar Organ (Hammond)",
            "Percussive Organ",
            "Rock Organ",
            "Church Organ",
            "Reed Organ",
            "Accordion (French)",
            "Harmonica",
            "Tango Accordion (Band neon)",
            "Acoustic Guitar (nylon)",
            "Acoustic Guitar (steel)",
            "Electric Guitar (jazz)",
            "Electric Guitar (clean)",
            "Electric Guitar (muted)",
            "Overdriven Guitar",
            "Distortion Guitar",
            "Guitar harmonics",
            "Acoustic Bass",
            "Electric Bass (fingered)",
            "Electric Bass (picked)",
            "Fretless Bass",
            "Slap Bass 1",
            "Slap Bass 2",
            "Synth Bass 1",
            "Synth Bass 2",
            "Violin",
            "Viola",
            "Cello",
            "Contrabass",
            "Tremolo Strings",
            "Pizzicato Strings",
            "Orchestral Harp",
            "Timpani",
            "String Ensemble 1 (strings)",
            "String Ensemble 2 (slow strings)",
            "SynthStrings 1",
            "SynthStrings 2",
            "Choir Aahs",
            "Voice Oohs",
            "Synth Voice",
            "Orchestra Hit",
            "Trumpet",
            "Trombone",
            "Tuba",
            "Muted Trumpet",
            "French Horn",
            "Brass Section",
            "SynthBrass 1",
            "SynthBrass 2",
            "Soprano Sax",
            "Alto Sax",
            "Tenor Sax",
            "Baritone Sax",
            "Oboe",
            "English Horn",
            "Bassoon",
            "Clarinet",
            "Piccolo",
            "Flute",
            "Recorder",
            "Pan Flute",
            "Blown Bottle",
            "Shakuhachi",
            "Whistle",
            "Ocarina",
            "Lead 1 (square wave)",
            "Lead 2 (sawtooth wave)",
            "Lead 3 (calliope)",
            "Lead 4 (chiffer)",
            "Lead 5 (charang)",
            "Lead 6 (voice solo)",
            "Lead 7 (fifths)",
            "Lead 8 (bass + lead)",
            "Pad 1 (new age Fantasia)",
            "Pad 2 (warm)",
            "Pad 3 (polysynth)",
            "Pad 4 (choir space voice)",
            "Pad 5 (bowed glass)",
            "Pad 6 (metallic pro)",
            "Pad 7 (halo)",
            "Pad 8 (sweep)",
            "FX 1 (rain)",
            "FX 2 (soundtrack)",
            "FX 3 (crystal)",
            "FX 4 (atmosphere)",
            "FX 5 (brightness)",
            "FX 6 (goblins)",
            "FX 7 (echoes, drops)",
            "FX 8 (sci-fi, star theme)",
            "Sitar",
            "Banjo",
            "Shamisen",
            "Koto",
            "Kalimba",
            "Bag pipe",
            "Fiddle",
            "Shanai",
            "Tinkle Bell",
            "Agogo",
            "Steel Drums",
            "Woodblock",
            "Taiko Drum",
            "Melodic Tom",
            "Synth Drum",
            "Reverse Cymbal",
            "Guitar Fret Noise",
            "Breath Noise",
            "Seashore",
            "Bird Tweet",
            "Telephone Ring",
            "Helicopter",
            "Applause",
            "Gunshot"
        };

        public static readonly string[] MidiDrumKeyNames =
        {
            "B1 (Acoustic Bass Drum)",
            "C2 (Bass Drum 1)",
            "C#2 (Side Stick)",
            "D2 (Acoustic Snare)",
            "D#2 (Hand Clap)",
            "E2 (Electric Snare)",
            "F2 (Low Floor Tom)",
            "F#2 (Closed Hi Hat)",
            "G2 (High Floor Tom)",
            "G#2 (Pedal Hi-Hat)",
            "A2 (Low Tom)",
            "A#2 (Open Hi-Hat)",
            "B2 (Low-Mid Tom)",
            "C3 (Hi Mid Tom)",
            "C#3 (Crash Cymbal 1)",
            "D3 (High Tom)",
            "D#3 (Ride Cymbal 1)",
            "E3 (Chinese Cymbal)",
            "F3 (Ride Bell)",
            "F#3 (Tambourine)",
            "G3 (Splash Cymbal)",
            "G#3 (Cowbell)",
            "A3 (Crash Cymbal 2)",
            "A#3 (Vibraslap)",
            "B3 (Ride Cymbal 2)",
            "C4 (Hi Bongo)",
            "C#4 (Low Bongo)",
            "D4 (Mute Hi Conga)",
            "D#4 (Open Hi Conga)",
            "E4 (Low Conga)",
            "F4 (High Timbale)",
            "F#4 (Low Timbale)",
            "G4 (High Agogo)",
            "G#4 (Low Agogo)",
            "A4 (Cabasa)",
            "A#4 (Maracas)",
            "B4 (Short Whistle)",
            "C5 (Long Whistle)",
            "C#5 (Short Guiro)",
            "D5 (Long Guiro)",
            "D#5 (Claves)",
            "E5 (Hi Wood Block)",
            "F5 (Low Wood Block)",
            "F#5 (Mute Cuica)",
            "G5 (Open Cuica)",
            "G#5 (Mute Triangle)",
            "A5 (Open Triangle)"
        };

        public const ulong AllDrumKeysMask = 0x7fffffffffff;

        public class MidiSource
        {
            public int   type = MidiSourceType.Channel; // Track/channel/None
            public int   index = 0;                     // Index of track/channel.
            public ulong keys  = AllDrumKeysMask;       // Only used for channel 10, can select specific keys.
        }

        private class TextEvent
        {
            public int track;
            public int tick;
            public int type;
            public string text;
        }

        private class TimeSignatureEvent
        {
            public int tick;
            public int numer;
            public int denom;
        }

        private class TempoEvent
        {
            public int tick;
            public int tempo;
        }

        private class NoteEvent
        {
            public int tick;
            public int track;
            public int channel;
            public int note;
            public int vel;
            public bool on;
        };

        private class ProgramChangeEvent
        {
            public int tick;
            public int channel;
            public int prg;
        }

        private class MidiPatternInfo
        {
            public int tick;
            public int numer;
            public int denom;
            public int tempo;
            public int measureCount;
        };

        private List<TextEvent>          textEvents          = new List<TextEvent>();
        private List<TimeSignatureEvent> timeSignatureEvents = new List<TimeSignatureEvent>();
        private List<TempoEvent>         tempoEvents         = new List<TempoEvent>();
        private List<NoteEvent>          noteEvents          = new List<NoteEvent>();
        private List<ProgramChangeEvent> programChangeEvents = new List<ProgramChangeEvent>();

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

        private bool ReadMetaEvent(int track, int tick)
        {
            var metaType = bytes[idx++];

            switch (metaType)
            {
                // Sequence number
                case 0x00:
                {
                    if (bytes[idx] == 0x02)
                    {
                        idx += 3;
                    }
                    else if (bytes[idx] == 0x00)
                    {
                        idx++;
                    }
                    else
                    {
                        Log.LogMessage(LogSeverity.Error, $"Unexpected value in sequence number meta event. Aborting.");
                        return false;
                    }
                    break;
                }

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

                    var textEvent = new TextEvent();
                    textEvent.track = track;
                    textEvent.tick = tick;
                    textEvent.text = name;
                    textEvent.type = metaType;
                    textEvents.Add(textEvent);
                    break;
                }
                // MIDI Channel Prefix
                case 0x20:
                {
                    if (bytes[idx] != 0x01)
                    {
                        Log.LogMessage(LogSeverity.Error, $"Unexpected value in channel prefix event. Aborting.");
                        return false;
                    }

                    idx += 2;
                    break;
                }

                // MIDI port
                case 0x21:
                {
                    if (bytes[idx] != 0x01)
                    {
                        Log.LogMessage(LogSeverity.Error, $"Unexpected value in MIDI port event. Aborting.");
                        return false;
                    }
                    idx += 2;
                    break;
                }

                // Track end
                case 0x2f:
                {
                    if (bytes[idx] != 0x00)
                    {
                       Log.LogMessage(LogSeverity.Error, $"Unexpected value in track end event. Aborting.");
                       return false;
                    }

                    idx++;
                    songDuration = Math.Max(songDuration, tick);
                    break;
                }

                // Tempo change.
                case 0x51:
                {
                    if (bytes[idx] != 0x03)
                    {
                       Log.LogMessage(LogSeverity.Error, $"Unexpected value in tempo change event. Aborting.");
                       return false;
                    }

                    idx++;
                    var tempo = ReadInt24();
                    var tempoEvent = new TempoEvent();
                    tempoEvent.tick = tick;
                    tempoEvent.tempo = tempo;
                    tempoEvents.Add(tempoEvent);
                    break;
                }

                // SMPTE Offset
                case 0x54:
                {
                    if (bytes[idx] != 0x05)
                    {
                       Log.LogMessage(LogSeverity.Error, $"Unexpected value in SMPTE offset event. Aborting.");
                       return false;
                    }

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
                    if (bytes[idx] != 0x04)
                    {
                       Log.LogMessage(LogSeverity.Error, $"Unexpected value in time signature event. Aborting.");
                       return false;
                    }

                    idx++;
                    var numer = bytes[idx++];
                    var denom = 1 << bytes[idx++];
                    idx += 2; // No idea what these 2 bytes mean.

                    var timeSignature = new TimeSignatureEvent();
                    timeSignature.tick = tick;
                    timeSignature.numer = numer;
                    timeSignature.denom = denom;
                    timeSignatureEvents.Add(timeSignature);

                    break;
                }

                // Key signature.
                case 0x59:
                {
                    if (bytes[idx] != 0x02)
                    {
                       Log.LogMessage(LogSeverity.Error, $"Unexpected value in key signature event. Aborting.");
                       return false;
                    }
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
                    Log.LogMessage(LogSeverity.Error, $"Unknown meta event {metaType}. Aborting.");
                    return false;
                }
            }

            return true;
        }

        private bool ReadMidiMessage(int track, int tick, ref byte status)
        {
            // Do we have a status byte?
            if ((bytes[idx] & 0x80) != 0)
            {
                status = bytes[idx++];
            }

            var statusHiByte = status >> 4;
            var channel = status & 0x0f;

            // Note ON / OFF
            if (statusHiByte == 0b1001 ||
                statusHiByte == 0b1000)
            {
                var key = bytes[idx++];
                var vel = bytes[idx++];

                //Debug.WriteLine($"At time {time} : NOTE ON! {Note.GetFriendlyName(key - 11)} vel {vel}.");

                var noteEvent = new NoteEvent();
                noteEvent.tick = tick;
                noteEvent.channel = channel;
                noteEvent.track = track;
                noteEvent.note = key - 11;
                noteEvent.vel = vel;
                noteEvent.on = statusHiByte == 0b1001 && vel > 0;
                noteEvents.Add(noteEvent);
            }

            // Polyphonic Key Pressure
            else if (statusHiByte == 0b1010)
            {
                var key      = bytes[idx++];
                var pressure = bytes[idx++];
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

                if (prg <= 127)
                { 
                    Debug.WriteLine($"Program change to {MidiInstrumentNames[prg]} ({prg}) on channel {channel} at time {tick}");

                    var prgChange = new ProgramChangeEvent();
                    prgChange.tick = tick;
                    prgChange.channel = channel;
                    prgChange.prg = prg;
                    programChangeEvents.Add(prgChange);
                }
                else
                {
                    Log.LogMessage(LogSeverity.Warning, $"Out-of-range program change ({prg}) on channel {channel} at time {tick}. Ignoring.");
                }
            }

            // System exclusive
            else if (status == 0b11110000)
            {
                while (bytes[idx++] != 0b11110111);
            }

            // Unsupported/unknown.
            else
            {
                Log.LogMessage(LogSeverity.Error, $"Unknown status {status}. Aborting.");
                return false;
            }
            
            return true;
        }

        private bool ReadTrackChunk(int track, int chunkLen)
        {
            var endIdx = idx + chunkLen;
            var status = (byte)0;
            var tick = 0;

            while (idx < endIdx)
            {
                var delta = ReadVarLen();
                var evt = bytes[idx];

                tick += delta;

                // Meta event
                if (evt == 0xff)
                {
                    idx++;
                    if (!ReadMetaEvent(track, tick))
                        return false;
                }
                else
                {
                    if (!ReadMidiMessage(track, tick, ref status))
                        return false;
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
                        if (!ReadTrackChunk(track, chunkLen))
                            return false;
                        track++;
                        break;
                    default:
                        Log.LogMessage(LogSeverity.Warning, $"Skipping unknown chunk type {chunkType} or length {chunkLen}");
                        idx += chunkLen;
                        break;
                }
            }

            textEvents.Sort((e1, e2) => e1.tick.CompareTo(e2.tick));
            timeSignatureEvents.Sort((e1, e2) => e1.tick.CompareTo(e2.tick));
            tempoEvents.Sort((e1, e2) => e1.tick.CompareTo(e2.tick));
            programChangeEvents.Sort((e1, e2) => e1.tick.CompareTo(e2.tick));

            // Favor note OFF before note ON to avoid false polyphony warnings.
            noteEvents.Sort((e1, e2) => e1.tick == e2.tick ? e1.on.CompareTo(e2.on) : e1.tick.CompareTo(e2.tick)); 

            numTracks = track;

            return true;
        }

        private double MicroSecondsToBPM(int ms)
        {
            return 60000000.0 / ms;
        }

        private TempoInfo GetClosestMatchingTempo(double bpm, int notesPerBeat)
        {
            var tempos = FamiStudioTempoUtils.GetAvailableTempos(project.PalMode, notesPerBeat); 

            var bestTempoDiff = double.MaxValue;
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

        private void CreateProjectAndSong(int expansionMask, MidiSource[] channelSources, bool pal)
        {
            var num163Channels = (expansionMask & ExpansionType.N163Mask) != 0 ? 8 : 1;

            project = new Project();
            project.SetExpansionAudioMask(expansionMask, num163Channels);
            project.PalMode = pal;

            song = project.CreateSong();

            bool foundName = false;
            bool foundCopyright = false;

            foreach (var textEvent in textEvents)
            {
                if (textEvent.type == 1 && !foundName)
                {
                    project.Name = textEvent.text;
                    foundName = true;
                }
                else if (textEvent.type == 2 && !foundCopyright)
                {
                    project.Copyright = textEvent.text;
                    foundCopyright = true;
                }
            }
        }

        private void CreateInstruments()
        {
            // Create a default instrument in case there isnt any program change.
            instrumentMap[ExpansionType.None] = new Dictionary<int, Instrument>();
            instrumentMap[ExpansionType.None][0] = project.CreateInstrument(ExpansionType.None, MidiInstrumentNames[0]);

            if (project.UsesAnyExpansionAudio)
            {
                for (int i = ExpansionType.Start; i <= ExpansionType.End; i++)
                {
                    if (project.UsesExpansionAudio(i))
                    {
                        instrumentMap[i] = new Dictionary<int, Instrument>();
                        instrumentMap[i][0] = project.CreateInstrument(i, MidiInstrumentNames[0] + $" {ExpansionType.InternalNames[i]}");
                    }
                }
            }

            foreach (var prgChange in programChangeEvents)
            {
                if (!instrumentMap[ExpansionType.None].ContainsKey(prgChange.prg))
                {
                    instrumentMap[ExpansionType.None][prgChange.prg] = project.CreateInstrument(ExpansionType.None, MidiInstrumentNames[prgChange.prg]);

                    if (project.UsesAnyExpansionAudio)
                    {
                        for (int i = ExpansionType.Start; i <= ExpansionType.End; i++)
                        {
                            if (project.UsesExpansionAudio(i))
                                instrumentMap[i][prgChange.prg] = project.CreateInstrument(i, MidiInstrumentNames[prgChange.prg] + $" {ExpansionType.InternalNames[i]}");
                        }
                    }
                }
            }
        }

        private void CreatePatterns(out List<MidiPatternInfo> patternInfos, int measuresPerPattern)
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

            // First figure out where each measure starts. Look for most common tempo / time signature
            var time = 0;
            var patternIdx = 0;
            var numer = timeSignatureEvents[0].numer;
            var denom = timeSignatureEvents[0].denom;
            var tempo = tempoEvents[0].tempo;
            var tempoCounts = new Dictionary<Tuple<int, int, int>, int>();
            var individualPatternInfos = new List<MidiPatternInfo>();

            while (time < songDuration)
            {
                var ratio = (4.0 / denom);

                var key = new Tuple<int, int, int>(numer, denom, tempo);
                if (!tempoCounts.ContainsKey(key))
                    tempoCounts.Add(key, 0);
                tempoCounts[key]++;

                individualPatternInfos.Add(new MidiPatternInfo() { tick = time, denom = denom, numer = numer, tempo = tempo, measureCount = 1 });

                var lastTime = time;

                // Advance by one bar.
                time = (int)(time + ticksPerQuarterNote * numer * ratio);
                patternIdx++;

                // Look for any tempo change between the last pattern and now.
                // We will only allow tempo changes on pattern boundaries for now.
                for (int i = tempoEvents.Count - 1; i >= 0; i--)
                {
                    if (tempoEvents[i].tick > lastTime &&
                        tempoEvents[i].tick <= time)
                    {
                        if (tempoEvents[i].tick != time)
                            Log.LogMessage(LogSeverity.Warning, $"Tempo change from {MicroSecondsToBPM(tempo).ToString("n1")} BPM to {MicroSecondsToBPM(tempoEvents[i].tempo).ToString("n1")} BPM at MIDI tick {tempoEvents[i].tick} is not exactly at the beginning of a measures and will only be applied at the beginning of the next measure.");

                        tempo = tempoEvents[i].tempo;

                        break;
                    }
                }

                // Look for another time signature change.
                var timeSignatureIdx = timeSignatureEvents.FindIndex(s => s.tick == time);
                if (timeSignatureIdx >= 0)
                {
                    numer = timeSignatureEvents[timeSignatureIdx].numer;
                    denom = timeSignatureEvents[timeSignatureIdx].denom;
                }
            }

            // Figure out which tempo/time signature is most common, use that for song default.
            var maxCount = 0;
            foreach (var kv in tempoCounts)
                maxCount = Math.Max(kv.Value, maxCount);

            int defaultNumer = 4;
            int defaultDenom = 4;
            int defaultTempo = 500000;

            foreach (var kv in tempoCounts)
            {
                if (kv.Value == maxCount)
                {
                    defaultNumer = kv.Key.Item1;
                    defaultDenom = kv.Key.Item2;
                    defaultTempo = kv.Key.Item3;

                    // Setup default song settings.
                    var initialBpm = MicroSecondsToBPM(defaultTempo);
                    var initialTempo = GetClosestMatchingTempo(initialBpm, 4);

                    song.ChangeFamiStudioTempoGroove(initialTempo.groove, false);
                    song.SetBeatLength(song.NoteLength * 4);
                    song.SetDefaultPatternLength(song.BeatLength * defaultNumer * measuresPerPattern);

                    break;
                }
            }

            // Merge patterns that have same tempo / time signature, up to measuresPerPattern.
            patternInfos = new List<MidiPatternInfo>();

            for (int p = 0; p < individualPatternInfos.Count;)
            {
                var patternInfo = individualPatternInfos[p];
                var cnt = 1;

                for (int j = p + 1; j < p + measuresPerPattern && j < individualPatternInfos.Count && cnt < measuresPerPattern; j++)
                {
                    var nextPatternInfo = individualPatternInfos[j];

                    if (nextPatternInfo.numer != patternInfo.numer ||
                        nextPatternInfo.denom != patternInfo.denom ||
                        nextPatternInfo.tempo != patternInfo.tempo)
                    {
                        break;
                    }

                    cnt++;
                }

                patternIdx = patternInfos.Count;

                patternInfo.measureCount = cnt;
                patternInfos.Add(patternInfo);

                var ratio = (4.0 / patternInfo.denom);

                // Create actual custom pattern settings.
                if (patternInfo.numer != defaultNumer ||
                    patternInfo.denom != defaultDenom ||
                    patternInfo.tempo != defaultTempo ||
                    patternInfo.measureCount != measuresPerPattern)
                {
                    var patternBpm = MicroSecondsToBPM(patternInfo.tempo);
                    var patternTempo = GetClosestMatchingTempo(patternBpm, 4);
                    var noteLength = Utils.Min(patternTempo.groove);

                    song.SetPatternCustomSettings(patternIdx, (int)Math.Round(noteLength * 4 * patternInfo.numer * ratio * patternInfo.measureCount), noteLength * 4, patternTempo.groove);
                }

                if (patternInfos.Count == 256)
                {
                    Log.LogMessage(LogSeverity.Warning, $"Song is more than 256 measures long, truncating. You could try increasing the 'Measures per pattern' to reduce the number of patterns.");
                    break;
                }

                p += cnt;
            }

            song.SetLength(patternInfos.Count);
        }

        private static string GetMIDINoteFriendlyName(int value)
        {
            int octave = value / 12;
            int note   = value % 12;

            return Note.NoteNames[note] + octave.ToString();
        }

        private bool FilterNoteEvent(NoteEvent evt, MidiSource source)
        {
            if (evt.note >= Note.MusicalNoteMin &&
                evt.note <= Note.MusicalNoteMax)
            {
                if (source.type == MidiSourceType.Track)
                {
                    return evt.track == source.index;
                }
                else if (source.type == MidiSourceType.Channel)
                {
                    if (evt.channel == 9)
                    {
                        if (evt.channel == source.index &&
                            evt.note >= MinDrumKey &&
                            evt.note <= MaxDrumKey)
                        {
                            return ((1ul << (evt.note - MinDrumKey)) & source.keys) != 0;
                        }
                    }
                    else
                    {
                        return evt.channel == source.index;
                    }
                }
            }
            else
            {
                // HACK : We subtracted 11 earlier to conver to "FamiStudio note", add it back.
                Log.LogMessage(LogSeverity.Warning, $"Note {GetMIDINoteFriendlyName(evt.note + 11)} on MIDI channel {evt.channel}, MIDI tick {evt.tick} is outside of range supported by FamiStudio (C0 to B7). Ignoring.");
            }

            return false;
        }

        private void CreateNotes(List<MidiPatternInfo> patternInfos, int channelIdx, MidiSource source, bool velocityAsVolume, int polyphony)
        {
            var channel = song.Channels[channelIdx];
            var channelInstruments = new int[16];
            var prgChangeIndex = 0;
            var prevVolume = (byte)15;
            var activeNote = -1;

            for (int i = 0; i < noteEvents.Count; i++)
            {
                var evt = noteEvents[i];

                // Apply any program change that happened before this event.
                while (prgChangeIndex < programChangeEvents.Count && programChangeEvents[prgChangeIndex].tick <= evt.tick)
                {
                    var prgChange = programChangeEvents[prgChangeIndex];
                    channelInstruments[prgChange.channel] = prgChange.prg;
                    prgChangeIndex++;
                }

                if (FilterNoteEvent(evt, source))
                {
                    if (evt.on)
                    {
                        if (activeNote > 0)
                        {
                            Log.LogMessage(LogSeverity.Warning, $"Polyphony detected on MIDI channel {evt.channel}, at MIDI tick {evt.tick} ({Note.GetFriendlyName(activeNote)} -> {Note.GetFriendlyName(evt.note)}).");
                            polyphonyWarningCount++;

                            if (polyphony == MidiPolyphonyBehavior.KeepOldNote)
                                continue;
                        }

                        activeNote = evt.note;
                    }
                    else 
                    {
                        if (evt.note != activeNote)
                        {
                            continue;
                        }

                        activeNote = -1;
                    }

                    // TODO: Binary search.
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

                    var pattern = channel.PatternInstances[patternIdx];
                    if (pattern == null)
                    {
                        pattern = channel.CreatePattern();
                        channel.PatternInstances[patternIdx] = pattern;
                    }

                    var patternInfo   = patternInfos[patternIdx];
                    var tickInPattern = evt.tick - patternInfo.tick;
                    var noteIndex     = tickInPattern / (double)ticksPerQuarterNote;
                    var noteLength    = song.GetPatternNoteLength(patternIdx);
                    var beatLength    = song.GetPatternBeatLength(patternIdx);

                    var note = new Note(evt.on ? evt.note : Note.NoteStop);

                    if (note.IsMusical)
                    {
                        var instrumentIdx = channelInstruments[evt.channel];
                        note.Instrument = instrumentMap[channel.Expansion][instrumentIdx];

                        if (velocityAsVolume && channel.SupportsEffect(Note.EffectVolume))
                        {
                            var volume = (byte)Math.Round((evt.vel / 127.0) * 15.0);
                            if (volume != prevVolume)
                            {
                                note.Volume = volume;
                                prevVolume = volume;
                            }
                        }
                    }

                    //var quantizedNoteIndex = Utils.Clamp((int)Math.Round(beatLength * noteIndex), 0, song.GetPatternLength(patternIdx) - 1);
                    var quantizedNoteIndex = (int)(beatLength * noteIndex);

                    if (patternIdx == patternInfos.Count - 1 && quantizedNoteIndex >= song.GetPatternLength(patternIdx))
                        break;

                    Debug.Assert(quantizedNoteIndex <= song.GetPatternLength(patternIdx));

                    pattern.Notes[quantizedNoteIndex] = note;
                }
            }
        }

        public string[] GetTrackNames(string filename)
        {
            idx = 0;
            bytes = File.ReadAllBytes(filename);

            if (!ReadHeaderChunk())
                return null;

            if (!ReadAllTracks())
                return null;

            var trackNames = new Dictionary<int, string>();

            foreach (var textEvent in textEvents)
            {
                if (textEvent.type == 3)
                {
                    var trackName = textEvent.text;
                    if (trackName != null)
                    {
                        // Sanitize name a bit.
                        trackName = trackName.Trim();
                        trackName = trackName.Trim(new[] { '\0' });
                        trackNames[textEvent.track] = trackName;
                    }
                }
            }

            var names = new string[numTracks];
            
            foreach (var kv in trackNames)
                names[kv.Key] = kv.Value;

            return names;
        }

        private void Cleanup()
        {
            // Truncate # of N163 channels.
            if (project.UsesN163Expansion)
            {
                var numN163Channels = 0;
                for (int i = ChannelType.N163Wave1; i <= ChannelType.N163Wave8; i++)
                {
                    var channel = song.GetChannelByType(i);
                    if (channel.Patterns.Count > 0)
                        numN163Channels = i - ChannelType.N163Wave1 + 1;
                }

                var expansionMask = project.ExpansionAudioMask;

                if (numN163Channels == 0)
                    expansionMask &= ~(ExpansionType.N163Mask);

                project.SetExpansionAudioMask(expansionMask, numN163Channels);
            }

            song.MergeIdenticalPatterns();
            song.Trim();
            project.ConvertToCompoundNotes();
            project.Cleanup();
            project.ValidateIntegrity();
        }

        public Project Load(string filename, int expansionMask, bool pal, MidiSource[] channelSources, bool velocityAsVolume, int polyphony, int measuresPerPattern, out int polyphonyCount)
        {
#if !DEBUG
            try
#endif
            {
                Debug.Assert(Channel.GetChannelCountForExpansionMask(expansionMask, 8) == channelSources.Length); 

                idx = 0;
                bytes = File.ReadAllBytes(filename);
                polyphonyCount = 0;

                if (!ReadHeaderChunk())
                    return null;

                if (!ReadAllTracks())
                    return null;

                CreateProjectAndSong(expansionMask, channelSources, pal);
                CreateInstruments();
                CreatePatterns(out var patternInfos, measuresPerPattern);

                for (int channelIdx = 0; channelIdx < song.Channels.Length; channelIdx++)
                    CreateNotes(patternInfos, channelIdx, channelSources[channelIdx], velocityAsVolume, polyphony);

                Cleanup();

                polyphonyCount = polyphonyWarningCount;
                return project;
            }
#if !DEBUG
            catch (Exception e)
            {
                Log.LogMessage(LogSeverity.Error, "Please contact the developer on GitHub!");
                Log.LogMessage(LogSeverity.Error, e.Message);
                Log.LogMessage(LogSeverity.Error, e.StackTrace);
                polyphonyCount = 0;
                return null;
            }
#endif
        }
    }

    class MidiFileWriter
    {
        private Project project;
        private Song song;
        private List<byte> bytes = new List<byte>();
        private byte[] tmp = new byte[4];
        private int songDuration;
        private const int TicksPerQuarterNote = 480;

        void WriteVarLen(int value)
{
            var buffer = value & 0x7f;
            while ((value >>= 7) > 0)
            {
                buffer <<= 8;
                buffer |= 0x80;
                buffer += (value & 0x7f);
            }

            while (true)
            {
                bytes.Add((byte)buffer);
                if ((buffer & 0x80) != 0)
                    buffer >>= 8;
                else
                    break;
            }
        }

        private void SetInt32(int i, int index)
        {
            Debug.Assert(BitConverter.IsLittleEndian);
            var b = BitConverter.GetBytes(i);
            // Big endian.
            bytes[index + 0] = b[3];
            bytes[index + 1] = b[2];
            bytes[index + 2] = b[1];
            bytes[index + 3] = b[0];
        }

        private void WriteInt32(int i)
        {
            Debug.Assert(BitConverter.IsLittleEndian);
            var b = BitConverter.GetBytes(i);
            // Big endian.
            bytes.Add(b[3]);
            bytes.Add(b[2]);
            bytes.Add(b[1]);
            bytes.Add(b[0]);
        }

        private void WriteInt24(int i)
        {
            Debug.Assert(BitConverter.IsLittleEndian);
            var b = BitConverter.GetBytes(i);
            // Big endian.
            bytes.Add(b[2]);
            bytes.Add(b[1]);
            bytes.Add(b[0]);
        }

        private void WriteInt16(short i)
        {
            Debug.Assert(BitConverter.IsLittleEndian);
            var b = BitConverter.GetBytes(i);
            // Big endian.
            bytes.Add(b[1]);
            bytes.Add(b[0]);
        }

        private void WriteDeltaTime(int ticks)
        {
            WriteVarLen(ticks);
        }

        private void WriteTextEvent(int type, string text)
        {
            bytes.Add(0xff);
            bytes.Add((byte)type);
            var b = Encoding.ASCII.GetBytes(text);
            WriteVarLen(b.Length);
            bytes.AddRange(b);
        }

        private void WriteTimeSignatureEvent(int numer, int denom)
        {
            // Write time signature
            bytes.Add(0xff);
            bytes.Add(0x58);
            bytes.Add(0x04);
            bytes.Add((byte)numer); // numer
            bytes.Add((byte)Utils.Log2Int(denom)); // denom.
            bytes.Add(0x18); // Just writing the same value as everybody else. No clue what it means.
            bytes.Add(0x08); // Just writing the same value as everybody else. No clue what it means.
        }

        private void WriteTempoEvent(float bpm)
        {
            // Write tempo.
            bytes.Add(0xff);
            bytes.Add(0x51);
            bytes.Add(0x03);
            WriteInt24((int)Math.Round(60000000.0 / bpm));
        }

        private void WriteTrackEndEvent()
        {
            bytes.Add(0xff);
            bytes.Add(0x2f);
            bytes.Add(0x00);
        }

        private void WriteNoteEvent(bool on, int channel, int note, int vel)
        {
            var status = on ? 0b10010000 : 0b10000000;
            status |= channel;

            bytes.Add((byte)status);
            bytes.Add((byte)note);
            bytes.Add((byte)vel);
        }

        private void WriteProgramChangeEvent(int channel, int prg)
        {
            var status = 0b11000000 | channel;

            bytes.Add((byte)status);
            bytes.Add((byte)prg);
        }

        private void WritePitchWheelEvent(int channel, int pitch)
        {
            var status = 0b11100000 | channel;

            bytes.Add((byte)status);
            bytes.Add((byte)(pitch & 0x7f));
            bytes.Add((byte)((pitch >> 7) & 0x7f));
        }

        private void WriteControllerChangeEvent(int channel, int ctrl, int val)
        {
            var status = 0b10110000 | channel;

            bytes.Add((byte)status);
            bytes.Add((byte)ctrl);
            bytes.Add((byte)val);
        }

        private void WriteHeaderChunk()
        {
            bytes.AddRange(Encoding.ASCII.GetBytes("MThd"));
            WriteInt32(6);
            WriteInt16(1);
            WriteInt16((short)(song.Channels.Length + 1));
            WriteInt16(TicksPerQuarterNote);
        }

        private void WriteTimeSignatureAndTempo(int[] groove, int patternLength, int notesPerBeat, int noteLength)
        {
            const int MaxFactor = 1024;

            var factor = 1;
            while (((patternLength * factor) % notesPerBeat) != 0 && factor <= MaxFactor)
                factor *= 2;

            var foundValidFactor = ((patternLength * factor) % notesPerBeat) == 0;

            var numer = 4;
            var denom = 4;

            if (foundValidFactor)
            {
                numer = patternLength * factor / notesPerBeat;
                denom = 4 * factor;
            }
            else
            {
                Log.LogMessage(LogSeverity.Warning, "Error computing valid time signature, defaulting to 4/4. Check your tempo settings!");
            }

            WriteTimeSignatureEvent(numer, denom);

            var bpm = FamiStudioTempoUtils.ComputeBpmForGroove(project.PalMode, groove, notesPerBeat / noteLength);

            WriteDeltaTime(0);
            WriteTempoEvent(bpm);
        }

        private void WritePitchBendRange(int channel, int range)
        {
            WriteControllerChangeEvent(channel, 0x65, 0);
            WriteDeltaTime(0);
            WriteControllerChangeEvent(channel, 0x64, 0);
            WriteDeltaTime(0);
            WriteControllerChangeEvent(channel, 0x06, range);
        }

        private void WriteControlTrack(out List<int> patternStartTick)
        {
            bytes.AddRange(Encoding.ASCII.GetBytes("MTrk"));

            // Write dummy len, will patch after.
            var chunckStartIdx = bytes.Count;
            WriteInt32(0);

            // Name
            WriteDeltaTime(0);
            WriteTextEvent(3, "Control Track");

            // Default tempo / time signature.
            if (!song.PatternHasCustomSettings(0))
            {
                WriteDeltaTime(0);
                WriteTimeSignatureAndTempo(song.Groove, song.PatternLength, song.BeatLength, song.NoteLength);
            }

            patternStartTick = new List<int>();

            var lastEventTick     = 0;
            var lastGroove        = song.Groove;
            var lastPatternLength = song.PatternLength;
            var lastBeatLength    = song.BeatLength;

            var tick = 0;

            // All custom patterns tempo changes.
            for (int i = 0; i < song.Length; i++)
            {
                var patternGroove     = song.GetPatternGroove(i);
                var patternLength     = song.GetPatternLength(i);
                var patternBeatLength = song.GetPatternBeatLength(i);
                var patternNoteLength = song.GetPatternNoteLength(i);

                if (Utils.CompareArrays(patternGroove, lastGroove) != 0 || patternLength != lastPatternLength || patternBeatLength != lastBeatLength)
                {
                    WriteDeltaTime(tick - lastEventTick);
                    WriteTimeSignatureAndTempo(patternGroove, patternLength, patternBeatLength, patternNoteLength);

                    lastEventTick     = tick;
                    lastGroove        = patternGroove;
                    lastPatternLength = patternLength;
                    lastBeatLength    = patternBeatLength;
                }

                patternStartTick.Add(tick);

                tick += TicksPerQuarterNote * patternLength / patternBeatLength;
            }

            songDuration = tick;

            // End of track
            WriteVarLen(songDuration - lastEventTick);
            WriteTrackEndEvent();

            // Patch size.
            SetInt32(bytes.Count - chunckStartIdx - 4, chunckStartIdx);
        }

        private void WriteChannelTrack(int channelIdx, List<int> patternInfos, int instrumentMode, int[] instrumentMapping, bool volumeAsVelocity, bool slideToPitchWheel, int pitchWheelRange)
        {
            bytes.AddRange(Encoding.ASCII.GetBytes("MTrk"));

            // Write dummy len, will patch after.
            var chunckStartIdx = bytes.Count;
            WriteInt32(0);

            WriteVarLen(0);
            WriteTextEvent(3, song.Channels[channelIdx].Name);

            WriteVarLen(0);
            WritePitchBendRange(channelIdx, pitchWheelRange);

            if (instrumentMode == MidiExportInstrumentMode.Channel)
            {
                WriteVarLen(0);
                WriteProgramChangeEvent(channelIdx, instrumentMapping[channelIdx]);
            }

            var tick = 0;
            var channel = song.Channels[channelIdx];
            var volume = 127;

            var slidePitch = 0;
            var slideNumFrames = 0;
            var slideStep = 0;
            var slideNeedReset = false;

            var lastInstrument = (Instrument)null;
            var lastEventTick = 0;
            var lastActiveNote = -1;
            
            // Loop through all the patterns.
            for (int p = 0; p < song.Length; p++)
            {
                var pattern = channel.PatternInstances[p];

                var patternLength     = song.GetPatternLength(p);
                var patternBeatLength = song.GetPatternBeatLength(p);

                if (pattern != null)
                {
                    for (var it = pattern.GetDenseNoteIterator(0, patternLength); !it.Done; it.Next())
                    {
                        var time = it.CurrentTime;
                        var note = it.CurrentNote;

                        var noteIdxFloat = time / (float)patternBeatLength;

                        tick = patternInfos[p] + (int)Math.Round(noteIdxFloat * TicksPerQuarterNote);

                        if (slideNumFrames > 0)
                        {
                            slidePitch += slideStep;
                            slideNumFrames--;

                            WriteVarLen(tick - lastEventTick);
                            WritePitchWheelEvent(channelIdx, slidePitch);
                            lastEventTick = tick;

                            if (slideNumFrames == 0)
                                slideNeedReset = true;
                        }

                        if (note == null)
                            continue;

                        if (note.HasVolume && volumeAsVelocity)
                            volume = (int)Math.Round((note.Volume / 15.0) * 127.0);

                        if (!note.IsMusical && !note.IsStop)
                            continue;

                        // Turn off any previous note.
                        if (lastActiveNote >= 0)
                        {
                            WriteVarLen(tick - lastEventTick);
                            WriteNoteEvent(false, channelIdx, lastActiveNote, 127);
                            lastEventTick = tick;
                            lastActiveNote = -1;
                        }
                        
                        if (note.IsMusical)
                        {
                            if (slideNeedReset)
                            {
                                WriteVarLen(tick - lastEventTick);
                                WritePitchWheelEvent(channelIdx, 8192);
                                lastEventTick = tick;
                                slideNeedReset = false;
                            }

                            // Instrument change => program change.
                            if (instrumentMode == MidiExportInstrumentMode.Instrument &&
                                note.Instrument != null &&
                                note.Instrument != lastInstrument)
                            {
                                WriteVarLen(tick - lastEventTick);
                                WriteProgramChangeEvent(channelIdx, instrumentMapping[project.Instruments.IndexOf(note.Instrument)]);

                                lastEventTick = tick;
                                lastInstrument = note.Instrument;
                            }

                            if (note.IsSlideNote && slideToPitchWheel)
                            {
                                var slideDuration = channel.GetSlideNoteDuration(new NoteLocation(p, time));
                                var semitones = note.SlideNoteTarget - note.Value;

                                if (semitones < 0 && semitones < -pitchWheelRange)
                                    semitones = -pitchWheelRange;
                                else if (semitones > 0 && semitones > pitchWheelRange)
                                    semitones = pitchWheelRange;

                                var stepStepFloat = semitones / (float)pitchWheelRange / slideDuration;

                                slideStep = Utils.Clamp((int)Math.Round(stepStepFloat * 8192), -8192, 8191);
                                slidePitch = 8192 + slideStep;
                                slideNumFrames = slideDuration - 1;

                                WriteVarLen(tick - lastEventTick);
                                WritePitchWheelEvent(channelIdx, slidePitch);
                                lastEventTick = tick;
                            }

                            // Write note.
                            WriteVarLen(tick - lastEventTick);
                            WriteNoteEvent(note.IsMusical, channelIdx, note.Value + 11, volume);
                            lastEventTick = tick;
                            lastActiveNote = note.Value + 11;
                        }
                    }
                }
            }

            tick = songDuration;

            // Turn off last note.
            if (lastActiveNote >= 0)
            {
                WriteVarLen(tick - lastEventTick);
                WriteNoteEvent(false, channelIdx, lastActiveNote, 127);
                lastEventTick = tick;
                lastActiveNote = -1;
            }

            WriteVarLen(tick - lastEventTick);
            WriteTrackEndEvent();

            // Patch size.
            SetInt32(bytes.Count - chunckStartIdx - 4, chunckStartIdx);
        }

        public void Save(Project originalProject, string filename, int songId, int instrumentMode, int[] instrumentMapping, bool volumeAsVelocity, bool slideToPitchWheel, int pitchWheelRange)
        {
            if (originalProject.UsesFamiTrackerTempo)
            {
                Log.LogMessage(LogSeverity.Error, "MIDI export is only available for projects using FamiStudio tempo. Aborting.");
                return;
            }

            Debug.Assert(
                (instrumentMode == MidiExportInstrumentMode.Instrument && instrumentMapping.Length == originalProject.Instruments.Count) ||
                (instrumentMode == MidiExportInstrumentMode.Channel && instrumentMapping.Length == originalProject.GetSong(songId).Channels.Length));

            project = originalProject.DeepClone();
            project.DeleteAllSongsBut(new int[] { songId }, false); // Not deleting unused data to keep all instruments, to match "instrumentMapping" array.
            project.ConvertToSimpleNotes();
            song = project.Songs[0];

            WriteHeaderChunk();
            WriteControlTrack(out var patternInfos);

            for (int i = 0; i < song.Channels.Length; i++)
                WriteChannelTrack(i, patternInfos, instrumentMode, instrumentMapping, volumeAsVelocity, slideToPitchWheel, pitchWheelRange);

            File.WriteAllBytes(filename, bytes.ToArray());
        }
    }

    public static class MidiSourceType
    {
        public const int Channel = 0;
        public const int Track   = 1;
        public const int None    = 2;

        public static readonly string[] Names =
        {
            "Channel",
            "Track",
            "None"
        };

        public static int GetValueForName(string str)
        {
            return Array.IndexOf(Names, str);
        }
    }

    public static class MidiPolyphonyBehavior
    {
        public const int UseNewNote  = 0;
        public const int KeepOldNote = 1;
        public const int Count       = 2;

        public static readonly LocalizedString[] LocalizedNames = new LocalizedString[Count];

        static MidiPolyphonyBehavior()
        {
            Localization.LocalizeStatic(typeof(MidiPolyphonyBehavior));
        }
    }

    public static class MidiExportInstrumentMode
    {
        public const int Instrument = 0;
        public const int Channel    = 1;
        public const int Count      = 2;

        public static readonly LocalizedString[] LocalizedNames = new LocalizedString[Count];

        static MidiExportInstrumentMode()
        {
            Localization.LocalizeStatic(typeof(MidiExportInstrumentMode));
        }
    }
}
