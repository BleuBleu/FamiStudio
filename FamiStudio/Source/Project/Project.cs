using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace FamiStudio
{
    public class Project
    {
        // Version 1 = FamiStudio 1.0.0
        // Version 2 = FamiStudio 1.1.0 (Project properties)
        // Version 3 = FamiStudio 1.2.0 (Volume tracks, extended notes, release envelopes)
        // Version 4 = FamiStudio 1.4.0 (VRC6, slide notes, vibrato, no-attack notes)
        // Version 5 = FamiStudio 2.0.0 (All expansions, fine pitch track, duty cycle envelope, advanced tempo, note refactor)
        public static int Version = 5;
        public static int MaxSampleSize = 0x4000;

        public const int ExpansionNone    = 0;
        public const int ExpansionVrc6    = 1;
        public const int ExpansionVrc7    = 2;
        public const int ExpansionFds     = 3;
        public const int ExpansionMmc5    = 4;
        public const int ExpansionN163    = 5;
        public const int ExpansionS5B     = 6;
        public const int ExpansionCount   = 7;

        public const int TempoFamiStudio  = 0;
        public const int TempoFamiTracker = 1;

        public static string [] TempoModeNames = 
        {
            "FamiStudio",
            "FamiTracker"
        };

        public static string[] ExpansionNames =
        {
            "None",
            "Konami VRC6",
            "Konami VRC7",
            "Famicom Disk System",
            "Nintendo MMC5",
            "Namco 163",
            "Sunsoft 5B"
        };

        public static string[] ExpansionShortNames =
        {
            "",
            "VRC6",
            "VRC7",
            "FDS",
            "MMC5",
            "N163",
            "S5B"
        };

        private DPCMSampleMapping[] samplesMapping = new DPCMSampleMapping[64]; // We only support allow samples from C1...D6 [1...63]. Stock FT2 range.
        private List<DPCMSample> samples = new List<DPCMSample>();
        private List<Instrument> instruments = new List<Instrument>();
        private List<Song> songs = new List<Song>();
        private int nextUniqueId = 100;
        private string filename = "";
        private string name = "Untitled";
        private string author = "Unknown";
        private string copyright = "";
        private int tempoMode = TempoFamiStudio;
        private int expansionAudio = ExpansionNone;
        private int expansionNumChannels = 1;

        public List<DPCMSample>    Samples        => samples;
        public DPCMSampleMapping[] SamplesMapping => samplesMapping;
        public List<Instrument>    Instruments    => instruments;
        public List<Song>          Songs          => songs;
        public int                 ExpansionAudio => expansionAudio;
        public int                 ExpansionNumChannels => expansionNumChannels;
        public string              ExpansionAudioName => ExpansionNames[expansionAudio];
        public string              ExpansionAudioShortName => ExpansionShortNames[expansionAudio];
        public bool                UsesExpansionAudio => expansionAudio != ExpansionNone;
        public bool                UsesFamiStudioTempo  => tempoMode == TempoFamiStudio;
        public bool                UsesFamiTrackerTempo => tempoMode == TempoFamiTracker;

        public string Filename   { get => filename; set => filename = value; }
        public string Name       { get => name; set => name = value; }
        public string Author     { get => author; set => author = value; }
        public string Copyright  { get => copyright; set => copyright = value; }

        public Project(bool createSongAndInstrument = false)
        {
            if (createSongAndInstrument)
            {
                CreateSong();
                CreateInstrument(ExpansionNone);
            }
        }

        public int GenerateUniqueId()
        {
            return nextUniqueId++;
        }

        public int GetSampleForAddress(int offset)
        {
            int addr = 0;
            foreach (var s in samples)
            {
                if (offset >= addr && offset < addr + s.Data.Length)
                    return s.Data[offset - addr];
                addr = (addr + s.Data.Length + 63) & 0xffc0;
            }
            return 0x55;
        }

        public int GetAddressForSample(DPCMSample sample)
        {
            int addr = 0;
            foreach (var s in samples)
            {
                if (s == sample)
                {
                    return addr;
                }
                addr = (addr + s.Data.Length + 63) & 0xffc0;
            }
            return addr;
        }

        public Song GetSong(int id)
        {
            return songs.Find(s => s.Id == id);
        }

        public Song GetSong(string name)
        {
            return songs.Find(s => s.Name == name);
        }

        public Instrument GetInstrument(int id)
        {
            return instruments.Find(i => i.Id == id);
        }

        public Instrument GetInstrument(string name)
        {
            return instruments.Find(i => i.Name == name);
        }

        public bool InstrumentExists(Instrument inst)
        {
            return instruments.Contains(inst);
        }

        public DPCMSample GetSample(int id)
        {
            return samples.Find(s => s.Id == id);
        }

        public DPCMSample GetSample(string name)
        {
            return samples.Find(s => s.Name == name);
        }

        public Pattern GetPattern(int id)
        {
            foreach (var song in songs)
            {
                var pattern = song.GetPattern(id);
                if (pattern != null)
                {
                    return pattern;
                }
            }

            return null;
        }

        public bool IsDPCMSampleNameUnique(string name)
        {
            return samples.Find(s => s.Name == name) == null;
        }

        public bool IsSongNameUnique(string name)
        {
            return songs.Find(s => s.Name == name) == null;
        }

        public DPCMSample CreateDPCMSample(string name, byte[] data)
        {
            var sampleSize = GetTotalSampleSize();
            var sample = samples.Find(s => s.Name == name);

            if (sample != null)
            {
                if (sampleSize - sample.Data.Length + data.Length <= MaxSampleSize)
                    sample.Data = data;
            }
            else if (sampleSize + data.Length <= MaxSampleSize)
            {
                sample = new DPCMSample(GenerateUniqueId(), name, data);
                samples.Add(sample);
                SortSamples();
            }

            return sample;
        }

        public bool NoteSupportsDPCM(int note)
        {
            return note > Note.DPCMNoteMin && note <= Note.DPCMNoteMax;
        }

        public void MapDPCMSample(int note, DPCMSample sample, int pitch = 15, bool loop = false)
        {
            if (sample != null && NoteSupportsDPCM(note))
            {
                note -= Note.DPCMNoteMin;

                if (samplesMapping[note] == null)
                {
                    samplesMapping[note] = new DPCMSampleMapping();
                    samplesMapping[note].Sample = sample;
                    samplesMapping[note].Pitch = pitch;
                    samplesMapping[note].Loop = loop;
                }
            }
        }

        public void UnmapDPCMSample(int note)
        {
            if (NoteSupportsDPCM(note))
            {
                samplesMapping[note - Note.DPCMNoteMin] = null;
            }
        }

        public DPCMSampleMapping GetDPCMMapping(int note)
        {
            if (NoteSupportsDPCM(note))
                return samplesMapping[note - Note.DPCMNoteMin];
            else
                return null;
        }

        public int FindDPCMSampleMapping(DPCMSample sample, int pitch, bool loop)
        {
            for (int i = 0; i < samplesMapping.Length; i++)
            {
                if (samplesMapping[i] != null && 
                    samplesMapping[i].Sample == sample &&
                    samplesMapping[i].Pitch  == pitch &&
                    samplesMapping[i].Loop   == loop)
                { 
                    return i + Note.DPCMNoteMin;
                }
            }
            
            return -1;
        }

        public DPCMSample FindMatchingSample(byte[] data)
        {
            foreach (var sample in samples)
            {
                if (sample.Data.Length == data.Length && sample.Data.SequenceEqual(data))
                    return sample;
            }

            return null;
        }

        public Song CreateSong(string name = null)
        {
            if (name == null)
                name = GenerateUniqueSongName();
            else if (songs.Find(s => s.Name == name) != null)
                return null;

            var song = new Song(this, GenerateUniqueId(), name);
            songs.Add(song);
            return song;
        }

        public void DeleteSong(Song song)
        {
            songs.Remove(song);
        }

        public Song DuplicateSong(Song song)
        {
            var saveSerializer = new ProjectSaveBuffer(this);
            song.SerializeState(saveSerializer);
            var newSong = CreateSong();
            var loadSerializer = new ProjectLoadBuffer(this, saveSerializer.GetBuffer(), Project.Version);
            loadSerializer.RemapId(song.Id, newSong.Id);
            newSong.SerializeState(loadSerializer);
#if DEBUG
            Validate();
#endif
            return newSong;
        }

        public bool IsInstrumentNameUnique(string name)
        {
            return instruments.Find(inst => inst.Name == name) == null;
        }

        public Instrument CreateInstrument(int type, string name = null)
        {
            if (type != ExpansionNone && type != expansionAudio)
                return null;

            if (name == null)
                name = GenerateUniqueInstrumentName();
            else if (instruments.Find(inst => inst.Name == name) != null)
                return null;

            var instrument = new Instrument(GenerateUniqueId(), type, name);
            instruments.Add(instrument);
            SortInstruments();
            return instrument;
        }

        public void UpdateAllLastValidNotesAndVolume()
        {
            foreach (var song in songs)
            {
                foreach (var channel in song.Channels)
                {
                    foreach (var pattern in channel.Patterns)
                        pattern.ClearLastValidNoteCache();
                }
            }
        }

        public void ReplaceInstrument(Instrument instrumentOld, Instrument instrumentNew)
        {
            Debug.Assert(instrumentNew == null || instrumentOld.ExpansionType == instrumentNew.ExpansionType);

            foreach (var song in songs)
            {
                foreach (var channel in song.Channels)
                {
                    foreach (var pattern in channel.Patterns)
                    {
                        foreach (var note in pattern.Notes.Values)
                        {
                            if (note.Instrument == instrumentOld)
                            {
                                if (instrumentNew == null)
                                    note.Value = Note.NoteInvalid;

                                note.Instrument = instrumentNew;
                            }
                        }
                    }
                }
            }

            UpdateAllLastValidNotesAndVolume();
        }

        public void DeleteInstrument(Instrument instrument)
        {
            instruments.Remove(instrument);
            ReplaceInstrument(instrument, null);
        }
        
        public void DeleteAllInstrument()
        {
            foreach (var inst in instruments)
                ReplaceInstrument(inst, null);
            instruments.Clear();
        }

        public void DeleteAllSamples()
        {
            for (int i = 0; i < samplesMapping.Length; i++)
                samplesMapping[i] = null;
            samples.Clear();
        }

        public string GenerateUniqueSongName()
        {
            for (int i = 1; ; i++)
            {
                var name = "Song " + i;
                if (songs.Find(song => song.Name == name) == null)
                    return name;
            }
        }

        public string GenerateUniqueInstrumentName()
        {
            for (int i = 1; ; i++)
            {
                var name = "Instrument " + i;
                if (instruments.Find(inst => inst.Name == name) == null)
                    return name;
            }
        }

        public bool RenameInstrument(Instrument instrument, string name)
        {
            if (instrument.Name == name)
                return true;

            if (instruments.Find(inst => inst.Name == name) == null)
            {
                instrument.Name = name;
                SortInstruments();
                return true;
            }

            return false;
        }

        public void SortInstruments()
        {
            instruments.Sort((i1, i2) => 
            {
                var expComp = i1.ExpansionType.CompareTo(i2.ExpansionType);

                if (expComp != 0)
                    return expComp;
                else
                    return i1.Name.CompareTo(i2.Name);
            });
        }

        public bool RenameSample(DPCMSample sample, string name)
        {
            if (sample.Name == name)
                return true;

            if (samples.Find(s => s.Name == name) == null)
            {
                sample.Name = name;
                SortSamples();
                return true;
            }

            return false;
        }

        public void SortSamples()
        {
            samples.Sort((s1, s2) => s1.Name.CompareTo(s2.Name) );
        }

        public bool RenameSong(Song song, string name)
        {
            if (song.Name == name)
                return true;

            if (songs.Find(s => s.Name == name) == null)
            {
                song.Name = name;
                SortSongs();
                return true;
            }

            return false;
        }

        public void SortSongs()
        {
            songs.Sort((s1, s2) => s1.Name.CompareTo(s2.Name));
        }

        public void SetExpansionAudio(int expansion, int numChannels = 1)
        {
            if (expansion == ExpansionN163 && numChannels == 0)
                expansion = ExpansionNone;

            if (expansion >= 0 && expansion < ExpansionCount)
            {
                var changed = expansionAudio != expansion;
                var oldNumChannels = expansionNumChannels;

                expansionAudio = expansion;
                expansionNumChannels = expansion == ExpansionN163 ? numChannels : 1;

                foreach (var song in songs)
                {
                    song.CreateChannels(true, Channel.ExpansionAudioStart + (!changed && expansion == ExpansionN163 ? oldNumChannels : 0));
                }

                if (changed)
                {
                    for (int i = instruments.Count - 1; i >= 0; i--)
                    {
                        var inst = instruments[i];
                        if (inst.IsExpansionInstrument)
                            DeleteInstrument(inst);
                    }
                }
            }
        }

        public int GetActiveChannelCount()
        {
            int channelCount = 0;
            for (int i = 0; i < Channel.Count; i++)
                if (IsChannelActive(i)) channelCount++;
            return channelCount;
        }

        public int[] GetActiveChannelList()
        {
            var activeChannels = new List<int>();
            for (int i = 0; i < Channel.Count; i++)
                if (IsChannelActive(i)) activeChannels.Add(i);
            return activeChannels.ToArray();
        }

        public bool IsChannelActive(int channelType)
        {
            if (channelType <= Channel.Dpcm)
                return true;

            if (channelType >= Channel.Vrc6Square1 && channelType <= Channel.Vrc6Saw)
                return expansionAudio == ExpansionVrc6;

            if (channelType == Channel.FdsWave)
                return expansionAudio == ExpansionFds;

            if (channelType >= Channel.Mmc5Square1 && channelType <= Channel.Mmc5Square2)
                return expansionAudio == ExpansionMmc5;

            if (channelType == Channel.Mmc5Dpcm)
                return false;

            if (channelType >= Channel.Vrc7Fm1 && channelType <= Channel.Vrc7Fm6)
                return expansionAudio == ExpansionVrc7;

            if (channelType >= Channel.N163Wave1 && channelType <= Channel.N163Wave8)
                return expansionAudio == ExpansionN163 && (channelType - Channel.N163Wave1) < expansionNumChannels;

            if (channelType >= Channel.S5BSquare1 && channelType <= Channel.S5BSquare3)
                return expansionAudio == ExpansionS5B;

            Debug.Assert(false);

            return false;
        }

        public bool NeedsExpansionInstruments
        {
            get
            {
                return expansionAudio != ExpansionNone && expansionAudio != ExpansionMmc5;
            }
        }
        
        public bool UsesSamples
        {
            get
            {
                if (samples.Count == 0)
                    return false;

                foreach (var song in songs)
                {
                    if (song.UsesDpcm)
                        return true;
                }

                return false;
            }
        }

        public int TempoMode
        {
            get
            {
                return tempoMode;
            }
            set
            {
                Debug.Assert(AreSongsEmpty);
                tempoMode = value;
            }
        }

        public bool AreSongsEmpty
        {
            get
            {
                foreach (var song in songs)
                {
                    if (!song.IsEmpty)
                        return false;
                }

                return true;
            }
        }

        public void CleanupUnusedSamples()
        {
            var usedSamples = new HashSet<DPCMSample>();
            foreach (var mapping in samplesMapping)
            {
                if (mapping != null && mapping.Sample != null)
                {
                    usedSamples.Add(mapping.Sample);
                }
            }

            samples.Clear();
            samples.AddRange(usedSamples);
        }

        public int GetTotalSampleSize()
        {
            int size = 0;
            foreach (var sample in samples)
                size += (sample.Data.Length + 63) & 0xffc0;
            return Math.Min(MaxSampleSize, size);
        }

        public byte[] GetPackedSampleData()
        {
            var sampleData = new List<byte>();

            foreach (var sample in samples)
                sampleData.AddRange(sample.Data);

            if (sampleData.Count > MaxSampleSize)
                sampleData.RemoveRange(MaxSampleSize, sampleData.Count - MaxSampleSize);

            return sampleData.ToArray();
        }

        public void RemoveAllSongsBut(int[] songIds)
        {
            for (int i = songs.Count - 1; i >= 0; i--)
            {
                var song = songs[i];

                if (Array.IndexOf(songIds, song.Id) < 0)
                {
                    DeleteSong(song);
                }
            }

            DeleteUnusedInstruments();
            DeleteUnusedSamples();
        }

        public void MergeIdenticalInstruments()
        {
            var instrumentCrcMap = new Dictionary<uint, Instrument>();

            for (int i = 0; i < instruments.Count;)
            {
                var inst = instruments[i];
                var crc = inst.ComputeCRC();

                if (instrumentCrcMap.TryGetValue(crc, out var matchingInstrument))
                {
                    ReplaceInstrument(inst, matchingInstrument);
                    instruments.RemoveAt(i);
                }
                else
                {
                    instrumentCrcMap[crc] = inst;
                    i++;
                }
            }
        }

        public void ConvertToFamiStudioTempo()
        {
            Debug.Assert(UsesFamiTrackerTempo);

            foreach (var song in songs)
                song.ConvertToFamiStudioTempo();

            tempoMode = TempoFamiStudio;
        }

        public void ConvertToFamiTrackerTempo(bool setDefaults)
        {
            Debug.Assert(UsesFamiStudioTempo);

            if (setDefaults)
            {
                foreach (var song in songs)
                {
                    song.SetDefaultsForTempoMode(Project.TempoFamiTracker);
                    song.UpdatePatternStartNotes();
                }
            }
            else
            {
                foreach (var song in songs)
                {
                    song.FamitrackerTempo = Song.NativeTempoNTSC;
                    song.FamitrackerSpeed = 1;
                }
            }

            tempoMode = TempoFamiTracker;
        }

        public void DeleteUnusedInstruments()
        {
            var usedInstruments = new HashSet<Instrument>();

            foreach (var song in songs)
            {
                for (int p = 0; p < song.Length; p++)
                {
                    foreach (var channel in song.Channels)
                    {
                        var pattern = channel.PatternInstances[p];
                        if (pattern != null)
                        {
                            foreach (var note in pattern.Notes.Values)
                            {
                                if (note.Instrument != null)
                                {
                                    //Debug.Assert(note.IsMusical);
                                    usedInstruments.Add(note.Instrument);
                                }
                            }
                        }
                    }
                }
            }

            instruments = new List<Instrument>(usedInstruments);
            SortInstruments();
        }

        public void DeleteUnusedSamples()
        {
            var usedSamples = new HashSet<DPCMSample>();

            foreach (var song in songs)
            {
                var channel = song.Channels[Channel.Dpcm];

                for (int p = 0; p < song.Length; p++)
                {
                    var pattern = channel.PatternInstances[p];
                    if (pattern != null)
                    {
                        foreach (var note in pattern.Notes.Values)
                        {
                            var mapping = GetDPCMMapping(note.Value);
                            if (note.IsValid && !note.IsStop && note.Instrument == null && mapping != null && mapping.Sample != null)
                            {
                                usedSamples.Add(mapping.Sample);
                            }
                        }
                    }
                }
            }

            samples = new List<DPCMSample>(usedSamples);

            for (int i = 0; i < samplesMapping.Length; i++)
            {
                if (samplesMapping[i] != null && !usedSamples.Contains(samplesMapping[i].Sample))
                {
                    samplesMapping[i] = null;
                }
            }
        }

#if DEBUG
        private void ValidateDPCMSamples()
        {
            foreach (var mapping in samplesMapping)
            {
                if (mapping != null && mapping.Sample != null)
                {
                    Debug.Assert(GetSample(mapping.Sample.Id) == mapping.Sample);
                    Debug.Assert(samples.Contains(mapping.Sample));
                }
            }
        }
#endif

        public void Validate()
        {
#if DEBUG
            ValidateDPCMSamples();

            foreach (var song in Songs)
                song.Validate(this);

            Debug.Assert(Note.EmptyNote.IsEmpty);
#endif
        }

        public void SerializeDPCMState(ProjectBuffer buffer)
        {
            // Samples
            int sampleCount = samples.Count;
            buffer.Serialize(ref sampleCount);
            buffer.InitializeList(ref samples, sampleCount);

            foreach (var sample in samples)
                sample.SerializeState(buffer);

            // Mapping
            ulong mappingMask = 0;
            for (int i = 0; i < 64; i++)
            {
                if (samplesMapping[i] != null)
                    mappingMask |= (((ulong)1) << i);
            }
            buffer.Serialize(ref mappingMask);

            for (int i = 0; i < 64; i++)
            {
                if ((mappingMask & (((ulong)1) << i)) != 0)
                {
                    if (buffer.IsReading)
                        samplesMapping[i] = new DPCMSampleMapping();
                    samplesMapping[i].SerializeState(buffer);
                }
                else
                {
                    samplesMapping[i] = null;
                }
            }
        }

        public void SerializeInstrumentState(ProjectBuffer buffer)
        {
            int instrumentCount = instruments.Count;
            buffer.Serialize(ref instrumentCount);
            buffer.InitializeList(ref instruments, instrumentCount);
            foreach (var instrument in instruments)
                instrument.SerializeState(buffer);
        }

        public void SerializeState(ProjectBuffer buffer)
        {
            if (!buffer.IsForUndoRedo)
            {
                buffer.Serialize(ref nextUniqueId);
            }

            // At version 2 (FamiStudio 1.1.0) we added project properties
            if (buffer.Version >= 2)
            {
                buffer.Serialize(ref name);
                buffer.Serialize(ref author);
                buffer.Serialize(ref copyright);

                // Version 2 (FamiStudio 1.1.0) had a typo in the name of the author.
                if (buffer.Version < 3 && author == "Unkown")
                {
                    author = "Unknown";
                }
            }

            // At version 4 (FamiStudio 1.4.0) we added basic expansion audio.
            if (buffer.Version >= 4)
            {
                buffer.Serialize(ref expansionAudio);
            }

            // At version 5 (FamiStudio 2.0.0) we added support for Namco 163 and advanced tempo mode.
            if (buffer.Version >= 5)
            {
                buffer.Serialize(ref expansionNumChannels);
                buffer.Serialize(ref tempoMode);
            }
            else
            {
                tempoMode = TempoFamiTracker;
            }

            // DPCM samples
            SerializeDPCMState(buffer);

            // Instruments
            SerializeInstrumentState(buffer);

            // Songs
            int songCount = songs.Count;
            buffer.Serialize(ref songCount);
            buffer.InitializeList(ref songs, songCount);
            foreach (var song in songs)
                song.SerializeState(buffer);
        }

        public Project DeepClone()
        {
            var saveSerializer = new ProjectSaveBuffer(this);
            SerializeState(saveSerializer);
            var newProject = new Project();
            var loadSerializer = new ProjectLoadBuffer(newProject, saveSerializer.GetBuffer(), Version);
            newProject.SerializeState(loadSerializer);
            return newProject;
        }
    }
}
