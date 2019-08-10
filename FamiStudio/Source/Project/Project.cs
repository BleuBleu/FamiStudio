using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace FamiStudio
{
    public class Project
    {
        public static int Version = 1;
        public static int MaxSampleSize = 0x4000;

        private DPCMSampleMapping[] samplesMapping = new DPCMSampleMapping[64];
        private List<DPCMSample> samples = new List<DPCMSample>();
        private List<Instrument> instruments = new List<Instrument>();
        private List<Song> songs = new List<Song>();
        private int nextUniqueId = 100;
        private string filename = "";

        public List<DPCMSample>    Samples        => samples;
        public DPCMSampleMapping[] SamplesMapping => samplesMapping;
        public List<Instrument>    Instruments    => instruments;
        public List<Song>          Songs          => songs;
        public int                 NextUniqueId   => nextUniqueId;
        public string              Filename { get => filename; set => filename = value; }
        public string              Name     { get => Path.GetFileNameWithoutExtension(filename); }

        public Project(bool createSongAndInstrument = false)
        {
            if (createSongAndInstrument)
            {
                CreateSong();
                CreateInstrument();
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
                if (addr + s.Data.Length > offset)
                {
                    return s.Data[offset - addr];
                }
                addr = (addr + s.Data.Length + 63) & 0xffc0;
            }
            Debug.Assert(false);
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

        public Instrument GetInstrument(int id)
        {
            return instruments.Find(i => i.Id == id);
        }

        public DPCMSample GetSample(int id)
        {
            return samples.Find(s => s.Id == id);
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

        public DPCMSample CreateDPCMSample(string name, byte[] data)
        {
            DPCMSample sample = samples.Find(s => s.Name == name);
            if (sample != null)
            {
                sample.Data = data;
            }
            else
            {
                sample = new DPCMSample(GenerateUniqueId(), name, data);
                samples.Add(sample);
                SortSamples();
            }

            return sample;
        }

        public void MapDPCMSample(int note, DPCMSample sample, int pitch = 15, bool loop = false)
        {
            if (samplesMapping[note] == null)
            {
                samplesMapping[note] = new DPCMSampleMapping();
                samplesMapping[note].Sample = sample;
                samplesMapping[note].Pitch = pitch;
                samplesMapping[note].Loop = loop;
            }
        }

        public DPCMSampleMapping GetDPCMMapping(int note)
        {
            return samplesMapping[note];
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

        public bool IsInstrumentNameUnique(string name)
        {
            return instruments.Find(inst => inst.Name == name) == null;
        }

        public Instrument CreateInstrument(string name = null)
        {
            if (name == null)
                name = GenerateUniqueInstrumentName();
            else if (instruments.Find(inst => inst.Name == name) != null)
                return null;

            var instrument = new Instrument(GenerateUniqueId(), name);
            instruments.Add(instrument);
            SortInstruments();
            return instrument;
        }

        public void ReplaceInstrument(Instrument instrumentOld, Instrument instrumentNew)
        {
            foreach (var song in songs)
            {
                foreach (var channel in song.Channels)
                {
                    foreach (var pattern in channel.Patterns)
                    {
                        for (int i = 0; i < Pattern.MaxLength; i++)
                        {
                            if (pattern.Notes[i].Instrument == instrumentOld)
                            {
                                if (instrumentNew == null)
                                    pattern.Notes[i].Value = Note.NoteInvalid;

                                pattern.Notes[i].Instrument = instrumentNew;
                            }
                        }
                    }
                }
            }
        }

        public void DeleteInstrument(Instrument instrument)
        {
            instruments.Remove(instrument);
            ReplaceInstrument(instrument, null);
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
            instruments.Sort((i1, i2) => i1.Name.CompareTo(i2.Name));
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
            {
                size += (sample.Data.Length + 63) & 0xffc0;
            }
            return size;
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
                            for (int i = 0; i < song.PatternLength; i++)
                            {
                                var note = pattern.Notes[i];
                                if (note.IsValid && !note.IsStop && note.Instrument != null)
                                {
                                    usedInstruments.Add(note.Instrument);
                                }
                            }
                        }
                    }
                }
            }

            instruments = new List<Instrument>(usedInstruments);
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
            for (int i = 0; i < samplesMapping.Length; i++)
            {
                if (samplesMapping[i] != null)
                    mappingMask |= (((ulong)1) << i);
            }
            buffer.Serialize(ref mappingMask);

            for (int i = 0; i < samplesMapping.Length; i++)
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

        public void SerializeState(ProjectBuffer buffer)
        {
            if (!buffer.IsForUndoRedo)
            {
                buffer.Serialize(ref nextUniqueId);
            }

            // DPCM samples
            SerializeDPCMState(buffer);

            // Instruments
            int instrumentCount = instruments.Count;
            buffer.Serialize(ref instrumentCount);
            buffer.InitializeList(ref instruments, instrumentCount);
            foreach (var instrument in instruments)
                instrument.SerializeState(buffer);

            // Songs
            int songCount = songs.Count;
            buffer.Serialize(ref songCount);
            buffer.InitializeList(ref songs, songCount);
            foreach (var song in songs)
                song.SerializeState(buffer);
        }

        public Project Clone()
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
