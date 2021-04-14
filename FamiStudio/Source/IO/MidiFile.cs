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

        private Dictionary<int, Instrument> instrumentMap = new Dictionary<int, Instrument>();
        private Dictionary<int, Instrument> instrumentMapExp = new Dictionary<int, Instrument>();

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
            "B#3 (Crash Cymbal 1)",
            "B3 (High Tom)",
            "B#3 (Ride Cymbal 1)",
            "B3 (Chinese Cymbal)",
            "B3 (Ride Bell)",
            "B#3 (Tambourine)",
            "B3 (Splash Cymbal)",
            "B#3 (Cowbell)",
            "B3 (Crash Cymbal 2)",
            "B#3 (Vibraslap)",
            "B3 (Ride Cymbal 2)",
            "C4 (Hi Bongo)",
            "B#4 (Low Bongo)",
            "B4 (Mute Hi Conga)",
            "B#4 (Open Hi Conga)",
            "B4 (Low Conga)",
            "B4 (High Timbale)",
            "B#4 (Low Timbale)",
            "B4 (High Agogo)",
            "B#4 (Low Agogo)",
            "B4 (Cabasa)",
            "B#4 (Maracas)",
            "B4 (Short Whistle)",
            "C5 (Long Whistle)",
            "B5 (Short Guiro)",
            "B#5 (Long Guiro)",
            "B5 (Claves)",
            "B#5 (Hi Wood Block)",
            "B5 (Low Wood Block)",
            "B5 (Mute Cuica)",
            "B#5 (Open Cuica)",
            "B5 (Mute Triangle)",
            "B#5 (Open Triangle)"
        };

        public const ulong AllDrumKeysMask = 0x3ffffffffff;

        public class MidiSource
        {
            public int   type = MidiSourceType.Channel; // True if idx is a track, otherwise channel.
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

        private void ReadMetaEvent(int track, int tick)
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
                    textEvent.tick = tick;
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
                    songDuration = Math.Max(songDuration, tick);
                    break;
                }

                // Tempo change.
                case 0x51:
                {
                    Debug.Assert(bytes[idx] == 0x03); // Not sure why this is needed.
                    idx++;
                    var tempo = ReadInt24();
                    Debug.WriteLine($"At time {tick} tempo is now {tempo}.");

                    var tempoEvent = new TempoEvent();
                    tempoEvent.tick = tick;
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
                    Debug.WriteLine($"At time {tick} time signature is now {numer} / {denom}.");

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

                Debug.WriteLine($"Program change to {MidiInstrumentNames[prg]} ({prg}) on channel {channel} at time {tick}");

                var prgChange = new ProgramChangeEvent();
                prgChange.tick = tick;
                prgChange.channel = channel;
                prgChange.prg = prg;
                programChangeEvents.Add(prgChange);
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
                    ReadMetaEvent(track, tick);
                }
                else
                {
                    ReadMidiMessage(track, tick, ref status);
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
            programChangeEvents.Sort((p1, p2) => p1.tick.CompareTo(p2.tick));
            noteEvents.Sort((n1, n2) => n1.tick.CompareTo(n2.tick));

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

        private void CreateProjectAndSong(int expansion)
        {
            project = new Project();
            project.SetExpansionAudio(expansion);
            song = project.CreateSong();

            // MIDITODO : Tempo mode here.
        }

        private void CreateInstruments()
        {
            // Create a default instrument in case there isnt any program change.
            instrumentMap[0] = project.CreateInstrument(ExpansionType.None, MidiInstrumentNames[0]);

            if (project.UsesExpansionAudio)
                instrumentMapExp[0] = project.CreateInstrument(project.ExpansionAudio, MidiInstrumentNames[0] + $" {project.ExpansionAudioShortName}");

            foreach (var prgChange in programChangeEvents)
            {
                if (!instrumentMap.ContainsKey(prgChange.prg))
                {
                    instrumentMap[prgChange.prg] = project.CreateInstrument(ExpansionType.None, MidiInstrumentNames[prgChange.prg]);

                    if (project.UsesExpansionAudio)
                        instrumentMapExp[prgChange.prg] = project.CreateInstrument(project.ExpansionAudio, MidiInstrumentNames[prgChange.prg] + $" {project.ExpansionAudioShortName}");
                }
            }
        }

        private void CreatePatterns(out List<MidiPatternInfo> patternInfos)
        {
            Debug.Assert(songDuration >= 0);

            // MIDITODO: Scan song to find most common time signature / tempo. Use that for the default.

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
                        if (evt.note >= MinDrumKey &&
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

            return false;
        }

        private void CreateNotes(List<MidiPatternInfo> patternInfos, int channelIdx, MidiSource source, bool velocityAsVolume)
        {
            var channelInstruments = new int[16];
            var prgChangeIndex = 0;
            var prevVolume = (byte)15;

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

                    var channel = song.Channels[channelIdx];
                    var pattern = channel.PatternInstances[patternIdx];
                    if (pattern == null)
                    {
                        pattern = channel.CreatePattern();
                        channel.PatternInstances[patternIdx] = pattern;
                    }

                    var patternInfo   = patternInfos[patternIdx];
                    var tickInPattern = evt.tick - patternInfo.tick;
                    var noteIndex     = tickInPattern / (double)ticksPerQuarterNote * (4.0 / patternInfo.denom);
                    var noteLength    = song.GetPatternNoteLength(patternIdx);
                    var beatLength    = song.GetPatternBeatLength(patternIdx);

                    var note = new Note(evt.on ? evt.note : Note.NoteStop);

                    if (note.IsMusical)
                    {
                        var instrumentIdx = channelInstruments[evt.channel];
                        note.Instrument = channel.IsExpansionChannel ? instrumentMapExp[instrumentIdx] : instrumentMap[instrumentIdx];

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

                    pattern.Notes[(int)Math.Round(beatLength * noteIndex)] = note;
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

        public Project Load(string filename, int expansion, MidiSource[] channelSources, bool velocityAsVolume)
        {
#if !DEBUG
            try
#endif
            {
                Debug.Assert(Channel.GetChannelCountForExpansion(expansion) == channelSources.Length);

                idx = 0;
                bytes = File.ReadAllBytes(filename);

                if (!ReadHeaderChunk())
                    return null;

                ReadAllTracks();

                CreateProjectAndSong(expansion);
                CreateInstruments();
                CreatePatterns(out var patternInfos);

                for (int channelIdx = 0; channelIdx < song.Channels.Length; channelIdx++)
                    CreateNotes(patternInfos, channelIdx, channelSources[channelIdx], velocityAsVolume);

                project.Cleanup();

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
}
