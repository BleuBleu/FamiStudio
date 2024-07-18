using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace FamiStudio
{
    public class Project
    {
        // Version 1  = FamiStudio 1.0.0
        // Version 2  = FamiStudio 1.1.0 (Project properties)
        // Version 3  = FamiStudio 1.2.0 (Volume tracks, extended notes, release envelopes)
        // Version 4  = FamiStudio 1.4.0 (VRC6, slide notes, vibrato, no-attack notes)
        // Version 5  = FamiStudio 2.0.0 (All expansions, fine pitch track, duty cycle envelope, advanced tempo, note refactor)
        // Version 6  = FamiStudio 2.1.0 (PAL authoring machine)
        // Version 7  = FamiStudio 2.2.0 (Arpeggios)
        // Version 8  = FamiStudio 2.3.0 (FamiTracker compatibility improvements)
        // Version 9  = FamiStudio 2.4.0 (DPCM sample editor)
        // Version 10 = FamiStudio 3.0.0 (VRC6 saw master volume, groove, song sorting)
        // Version 11 = FamiStudio 3.1.0 (Volume slides, DPCM fine pitch)
        // Version 12 = FamiStudio 3.2.0 (Multiple expansions, overclocking)
        // Version 13 = FamiStudio 3.3.0 (EPSM, Delta counter)
        // Version 14 = FamiStudio 4.0.0 (Unicode text).
        // Version 15 = FamiStudio 4.1.0 (DPCM bankswitching)
        // Version 16 = FamiStudio 4.2.0 (Folders, sound engine options, project mixer settings)
        // Version 17 = FamiStudio 4.3.0 (Tuning)
        public const int Version = 17;
        public const int MaxMappedSampleSize = 0x40000;
        public const int MaxDPCMBanks = 64; 
        public const int MaxSampleAddress = 255 * 64;

        private List<DPCMSample> samples = new List<DPCMSample>();
        private List<Instrument> instruments = new List<Instrument>();
        private List<Arpeggio> arpeggios = new List<Arpeggio>();
        private List<Song> songs = new List<Song>();
        private List<Folder> folders = new List<Folder>();
        private int nextUniqueId = 100;
        private string filename = "";
        private string name = "Untitled";
        private string author = "Unknown";
        private string copyright = "";
        private int tempoMode = TempoType.FamiStudio;
        private int expansionMask = ExpansionType.NoneMask;
        private int expansionNumN163Channels = 1; // For N163 only.
        private int tuning = 440;
        private bool sortSongs = true;
        private bool sortInstruments = true;
        private bool sortSamples = true;
        private bool sortArpeggios = true;

        // Project mixer overrides;
        private bool allowMixerOverride = true;
        private bool overrideBassCutoffHz = false;
        private int bassCutoffHz = 16; // in Hz (matches Settings.DefaultBassCutoffHz, need to move constant somewhere)
        private ExpansionMixer[] mixerSettings = new ExpansionMixer[ExpansionType.Count];

        // Sound engine options.
        private bool soundEngineUsesExtendedInstruments = false;
        private bool soundEngineUsesExtendedDpcm = false;
        private bool soundEngineUsesBankSwitching = false;

        // This flag has different meaning depending on the tempo mode:
        //  - In FamiStudio  mode, it means the source data is authored on PAL
        //  - In FamiTracker mode, it means the last playback mode was PAL
        private bool pal = false;

        public List<DPCMSample> Samples => samples;
        public List<Instrument> Instruments => instruments;
        public List<Song> Songs => songs;
        public List<Arpeggio> Arpeggios => arpeggios;

        public bool UsesFamiStudioTempo => tempoMode == TempoType.FamiStudio;
        public bool UsesFamiTrackerTempo => tempoMode == TempoType.FamiTracker;

        public int ExpansionAudioMask => expansionMask;
        public int ExpansionNumN163Channels => expansionNumN163Channels;
        public int N163WaveRAMSize => 128 - 8 * expansionNumN163Channels;

        public int Tuning { get => tuning; set => tuning = value; }

        public bool UsesAnyExpansionAudio       => (expansionMask != ExpansionType.NoneMask);
        public bool UsesSingleExpansionAudio    => (Utils.NumberOfSetBits(expansionMask) == 1);
        public bool UsesMultipleExpansionAudios => (Utils.NumberOfSetBits(expansionMask) > 1);

        public bool UsesFdsExpansion   => (expansionMask & ExpansionType.FdsMask) != 0;
        public bool UsesN163Expansion  => (expansionMask & ExpansionType.N163Mask) != 0;
        public bool UsesVrc6Expansion  => (expansionMask & ExpansionType.Vrc6Mask) != 0;
        public bool UsesVrc7Expansion  => (expansionMask & ExpansionType.Vrc7Mask) != 0;
        public bool UsesMmc5Expansion  => (expansionMask & ExpansionType.Mmc5Mask) != 0;
        public bool UsesS5BExpansion   => (expansionMask & ExpansionType.S5BMask) != 0;
        public bool UsesEPSMExpansion  => (expansionMask & ExpansionType.EPSMMask) != 0;
        
        public bool OutputsStereoAudio => UsesEPSMExpansion;
        public bool HasAnyFolders => folders.Count > 0;

        public string Filename  { get => filename;  set => filename = value; }
        public string Name      { get => name;      set => name = value; }
        public string Author    { get => author;    set => author = value; }
        public string Copyright { get => copyright; set => copyright = value; }

        public bool SoundEngineUsesExtendedInstruments { get => soundEngineUsesExtendedInstruments; set => soundEngineUsesExtendedInstruments = value; }
        public bool SoundEngineUsesExtendedDpcm        { get => soundEngineUsesExtendedDpcm || soundEngineUsesBankSwitching; set => soundEngineUsesExtendedDpcm = value; }
        public bool SoundEngineUsesDpcmBankSwitching   { get => soundEngineUsesBankSwitching; set => soundEngineUsesBankSwitching = value; }

        public bool AllowMixerOverride   { get => allowMixerOverride;   set => allowMixerOverride   = value; } // TODO : This has no business being here. More to FamiStudio class.
        public bool OverrideBassCutoffHz { get => overrideBassCutoffHz; set => overrideBassCutoffHz = value; }
        public int  BassCutoffHz         { get => bassCutoffHz;         set => bassCutoffHz         = value; }

        public ExpansionMixer[] ExpansionMixerSettings => mixerSettings;

        public Project(bool createSongAndInstrument = false)
        {
            Array.Copy(ExpansionMixer.DefaultExpansionMixerSettings, mixerSettings, mixerSettings.Length);

            if (createSongAndInstrument)
            {
                CreateSong();
                CreateInstrument(ExpansionType.None);
            }
        }

        public int GenerateUniqueId()
        {
            return nextUniqueId++;
        }

        public bool PalMode
        {
            get
            {
                return pal;
            }
            set
            {
                pal = value;
            }
        }

        public int SingleExpansion
        {
            get
            {
                Debug.Assert(UsesSingleExpansionAudio);

                for (int i = ExpansionType.Start; i <= ExpansionType.End; i++)
                {
                    if (UsesExpansionAudio(i))
                        return i;
                }

                return -1;
            }
        }

        public bool AutoSortSongs
        {
            get { return sortSongs; }
            set
            {
                sortSongs = value;
                ConditionalSortSongs();
            }
        }

        public bool AutoSortInstruments
        {
            get { return sortInstruments; }
            set
            {
                sortInstruments = value;
                ConditionalSortInstruments();
            }
        }

        public bool AutoSortSamples
        {
            get { return sortSamples; }
            set
            {
                sortSamples = value;
                ConditionalSortSamples();
            }
        }

        public bool AutoSortArpeggios
        {
            get { return sortArpeggios; }
            set
            {
                sortArpeggios = value;
                ConditionalSortArpeggios();
            }
        }

        public bool UsesExpansionAudio(int type)
        {
            if (type == ExpansionType.None)
                return true;

            return (expansionMask & ExpansionType.GetMaskFromValue(type)) != 0;
        }

        public bool UsesMultipleDPCMBanks
        {
            get
            {
                foreach (var s in samples)
                {
                    if (s.Bank != 0)
                        return true;
                }

                return false;
            }
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

        public Arpeggio GetArpeggio(int id)
        {
            return arpeggios.Find(a => a.Id == id);
        }

        public Arpeggio GetArpeggio(string name)
        {
            return arpeggios.Find(a => a.Name == name);
        }

        public bool SongExists(Song song)
        {
            return songs.Contains(song);
        }

        public bool InstrumentExists(Instrument inst)
        {
            return instruments.Contains(inst);
        }

        public bool ArpeggioExists(Arpeggio arp)
        {
            return arpeggios.Contains(arp);
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

        public bool EnsureSongAssemblyNamesAreUnique()
        {
            foreach (var song in songs)
            {
                var asmName = Utils.MakeNiceAsmName(song.Name);
                if (songs.FindAll(s => Utils.MakeNiceAsmName(s.Name) == asmName).Count > 1)
                {
                    Log.LogMessage(LogSeverity.Error, $"Song names are not unique when exported to assemly, avoid using only special characters to differentiate songs.");
                    return false;
                }
            }

            return true;
        }

        public DPCMSample CreateDPCMSample(string name)
        {
            // Already exist, this should not happen.
            if (samples.Find(s => s.Name == name) != null)
            {
                Debug.Assert(false);
                return null;
            }

            var sample = new DPCMSample(this, GenerateUniqueId(), name);
            samples.Add(sample);
            ConditionalSortSamples();
            return sample;
        }

        public DPCMSample CreateDPCMSampleFromDmcData(string name, byte[] data, string filename = null)
        {
            var sample = CreateDPCMSample(name);

            if (sample == null)
                return null;

            sample.SetDmcSourceData(data, filename, true);
            sample.Process();

            return sample;
        }

        public DPCMSample CreateDPCMSampleFromWavData(string name, short[] data, int sampleRate, string filename = null)
        {
            var sample = CreateDPCMSample(name);

            if (sample == null)
                return null;

            sample.SetWavSourceData(data, sampleRate, filename, true);
            sample.Process();

            return sample;
        }

        public void TransposeDPCMMapping(int oldNote, int newNote, Instrument instrument)
        {
            foreach (var song in songs)
            {
                var channel = song.Channels[ChannelType.Dpcm];

                foreach (var pattern in channel.Patterns)
                {
                    bool dirty = false;
                    foreach (var note in pattern.Notes.Values)
                    {
                        if (note.Instrument == instrument && note.Value == oldNote)
                        {
                            note.Value = (byte)newNote;
                            dirty = true;
                        }
                    }
                    if (dirty)
                        pattern.InvalidateCumulativeCache();
                }
            }
        }

        public DPCMSample FindMatchingSample(byte[] data)
        {
            foreach (var sample in samples)
            {
                if (sample.ProcessedData.Length == data.Length && sample.ProcessedData.SequenceEqual(data))
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
            song.Serialize(saveSerializer);
            var newSong = CreateSong();
            var loadSerializer = new ProjectLoadBuffer(this, saveSerializer.GetBuffer(), Project.Version);

            // Remap the ID of the song + all patterns.
            loadSerializer.RemapId(song.Id, newSong.Id);
            foreach (var channels in song.Channels)
            {
                foreach (var pattern in channels.Patterns)
                    loadSerializer.RemapId(pattern.Id, GenerateUniqueId());
            }

            newSong.Serialize(loadSerializer);
            newSong.Name = GenerateUniqueSongName(newSong.Name.TrimEnd(new[] { ' ', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' }));

            MoveSong(newSong, song);
            ConditionalSortSongs();
            ValidateIntegrity();
            return newSong;
        }

        public bool IsInstrumentNameUnique(string name)
        {
            return instruments.Find(inst => inst.Name == name) == null;
        }

        public Instrument CreateInstrument(int expansion, string name = null)
        {
            Debug.Assert(expansion >= ExpansionType.None && expansion < ExpansionType.Count);

            if (expansion != ExpansionType.None && !UsesExpansionAudio(expansion))
                return null;

            if (name == null)
                name = GenerateUniqueInstrumentName();
            else if (instruments.Find(inst => inst.Name == name) != null)
                return null;

            var instrument = new Instrument(this, GenerateUniqueId(), expansion, name);
            instruments.Add(instrument);
            ConditionalSortInstruments();
            return instrument;
        }

        public Arpeggio CreateArpeggio(string name = null)
        {
            if (name == null)
                name = GenerateUniqueArpeggioName();
            else if (arpeggios.Find(arp => arp.Name == name) != null)
                return null;

            var arpeggio = new Arpeggio(this, GenerateUniqueId(), name);
            arpeggios.Add(arpeggio);
            ConditionalSortArpeggios();
            return arpeggio;
        }

        public Arpeggio DuplicateArpeggio(Arpeggio arpeggio)
        {
            var saveSerializer = new ProjectSaveBuffer(this);
            arpeggio.Serialize(saveSerializer);
            var newArpeggio = CreateArpeggio();
            var loadSerializer = new ProjectLoadBuffer(this, saveSerializer.GetBuffer(), Project.Version);
            loadSerializer.RemapId(arpeggio.Id, newArpeggio.Id);
            newArpeggio.Serialize(loadSerializer);
            newArpeggio.Name = GenerateUniqueArpeggioName(newArpeggio.Name.TrimEnd(new[] { ' ', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' }));
            MoveArpeggio(newArpeggio, arpeggio);
            ConditionalSortArpeggios();
            ValidateIntegrity();
            return newArpeggio;
        }

        public void InvalidateCumulativePatternCache()
        {
            foreach (var song in songs)
            {
                foreach (var channel in song.Channels)
                    channel.InvalidateCumulativePatternCache();
            }
        }

        public void ReplaceInstrument(Instrument instrumentOld, Instrument instrumentNew)
        {
            Debug.Assert(instrumentNew == null || instrumentOld.Expansion == instrumentNew.Expansion);

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
                                {
                                    note.HasRelease = false;
                                    note.Value = Note.NoteInvalid;
                                    note.Duration = 0;
                                }

                                note.Instrument = instrumentNew;
                            }
                        }
                    }
                }
            }

            InvalidateCumulativePatternCache();
        }

        public void DeleteInstrument(Instrument instrument)
        {
            instruments.Remove(instrument);
            ReplaceInstrument(instrument, null);
        }

        public Instrument DuplicateInstrument(Instrument instrument)
        {
            var saveSerializer = new ProjectSaveBuffer(this);
            instrument.Serialize(saveSerializer);
            var newInstrument = CreateInstrument(instrument.Expansion);
            var loadSerializer = new ProjectLoadBuffer(this, saveSerializer.GetBuffer(), Project.Version);
            loadSerializer.RemapId(instrument.Id, newInstrument.Id);
            newInstrument.Serialize(loadSerializer);
            newInstrument.Name = GenerateUniqueInstrumentName(newInstrument.Name.TrimEnd(new[] { ' ', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' }));
            MoveInstrument(newInstrument, instrument);
            ConditionalSortInstruments();
            ValidateIntegrity();
            return newInstrument;
        }

        public Instrument DuplicateConvertInstrument(Instrument oldInst, int newExp)
        {
            var oldName = oldInst.Name;
            var newName = GenerateUniqueInstrumentName(oldName.TrimEnd(new[] { ' ', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' }));
            var newInst = CreateInstrument(newExp, newName);

            InstrumentConverter.Convert(oldInst, newInst);

            ConditionalSortInstruments();
            ValidateIntegrity();
            return newInst;
        }

        public void DeleteAllInstruments()
        {
            foreach (var inst in instruments)
                ReplaceInstrument(inst, null);
            instruments.Clear();
        }

        public void DeleteAllInstrumentBut(int[] instrumentIds)
        {
            for (int i = 0; i < instruments.Count;)
            {
                if (Array.IndexOf(instrumentIds, instruments[i].Id) < 0)
                {
                    ReplaceInstrument(instruments[i], null);
                    instruments.RemoveAt(i);
                }
                else
                {
                    i++;
                }
            }
        }

        public void ReplaceArpeggio(Arpeggio arpeggioOld, Arpeggio arpeggioNew)
        {
            foreach (var song in songs)
            {
                foreach (var channel in song.Channels)
                {
                    foreach (var pattern in channel.Patterns)
                    {
                        foreach (var note in pattern.Notes.Values)
                        {
                            if (note.Arpeggio == arpeggioOld)
                            {
                                note.Arpeggio = arpeggioNew;
                            }
                        }
                    }
                }
            }
        }

        public void DeleteArpeggio(Arpeggio arpeggio)
        {
            arpeggios.Remove(arpeggio);
            ReplaceArpeggio(arpeggio, null);
        }

        public void ReplaceSampleInAllMappings(DPCMSample oldSample, DPCMSample newSample)
        {
            foreach (var inst in instruments)
            {
                if (inst.HasAnyMappedSamples)
                {
                    inst.ReplaceSampleInAllMappings(oldSample, newSample);
                }
            }
        }

        public void DeleteSample(DPCMSample sample)
        {
            samples.Remove(sample);
            ReplaceSampleInAllMappings(sample, null);
        }

        public void DeleteAllSamples()
        {
            foreach (var inst in instruments)
            {
                if (inst.HasAnyMappedSamples)
                {
                    inst.DeleteAllMappings();
                }
            }

            samples.Clear();
        }

        public void DeleteAllSamplesBut(int[] sampleIds)
        {
            for (int i = 0; i < samples.Count;)
            {
                if (Array.IndexOf(sampleIds, samples[i].Id) < 0)
                    DeleteSample(samples[i]);
                else
                    i++;
            }
        }

        public void DeleteAllSongs()
        {
            songs.Clear();
        }

        public void DeleteAllArpeggios()
        {
            arpeggios.Clear();
        }

        public bool IsInstrumentUsedByOtherChannelThanDPCM(Instrument instrument)
        {
            foreach (var song in songs)
            {
                foreach (var channel in song.Channels)
                {
                    if (!channel.IsDpcmChannel)
                    {
                        foreach (var pattern in channel.Patterns)
                        {
                            foreach (var note in pattern.Notes.Values)
                            {
                                if (note.Instrument == instrument)
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }

            return false;
        }

        public string GenerateUniqueSongName(string baseName = "Song")
        {
            if (string.IsNullOrEmpty(baseName))
                baseName = "Song";

            for (int i = 1; ; i++)
            {
                var name = $"{baseName} {i}";
                if (songs.Find(song => song.Name == name) == null)
                    return name;
            }
        }

        public string GenerateUniqueInstrumentName(string baseName = "Instrument")
        {
            if (string.IsNullOrEmpty(baseName))
                baseName = "Instrument";

            for (int i = 1; ; i++)
            {
                var name = $"{baseName} {i}";
                if (instruments.Find(inst => inst.Name == name) == null)
                    return name;
            }
        }

        public string GenerateUniqueArpeggioName(string baseName = "Arpeggio")
        {
            if (string.IsNullOrEmpty(baseName))
                baseName = "Arpeggio";

            for (int i = 1; ; i++)
            {
                var name = $"{baseName} {i}";
                if (arpeggios.Find(arp => arp.Name == name) == null)
                    return name;
            }
        }

        public string GenerateUniqueDPCMSampleName(string baseName)
        {
            if (string.IsNullOrEmpty(baseName))
                baseName = "Sample";

            if (samples.Find(s => s.Name == baseName) == null)
                return baseName;

            for (int i = 1; ; i++)
            {
                var name = $"{baseName} {i}";
                if (samples.Find(s => s.Name == name) == null)
                    return name;
            }
        }

        public string GenerateUniqueFolderName(int type, string baseName = "Folder")
        {
            if (string.IsNullOrEmpty(baseName))
                baseName = "Folder";

            if (!FolderExists(type, baseName))
                return baseName;

            for (int i = 1; ; i++)
            {
                var name = $"{baseName} {i}";
                if (!FolderExists(type, name))
                    return name;
            }
        }

        public bool RenameInstrument(Instrument instrument, string name)
        {
            if (instrument.Name == name)
                return true;
            if (string.IsNullOrEmpty(name))
                return false;

            if (instruments.Find(inst => inst.Name == name) == null)
            {
                instrument.Name = name;
                ConditionalSortInstruments();
                return true;
            }

            return false;
        }

        public void SortInstruments()
        {
            instruments.Sort((i1, i2) => 
            {
                var expComp = i1.Expansion.CompareTo(i2.Expansion);

                if (expComp != 0)
                    return expComp;
                else
                    return AlphaNumericComparer.CompareStatic(i1.Name, i2.Name);
            });
            SortFolders(FolderType.Instrument);
        }

        public void ConditionalSortInstruments()
        {
            if (sortInstruments)
                SortInstruments();
        }

        public void MoveInstrument(Instrument inst, Instrument instBefore)
        {
            Debug.Assert(instruments.Contains(inst));
            instruments.Remove(inst);

            if (instBefore != null)
                instruments.Insert(instruments.IndexOf(instBefore) + 1, inst);
            else
                instruments.Insert(0, inst);
        }

        public bool RenameArpeggio(Arpeggio arpeggio, string name)
        {
            if (arpeggio.Name == name)
                return true;
            if (string.IsNullOrEmpty(name))
                return false;

            if (arpeggios.Find(arp => arp.Name == name) == null)
            {
                arpeggio.Name = name;
                ConditionalSortArpeggios();
                return true;
            }

            return false;
        }

        public void SortArpeggios()
        {
            arpeggios.Sort((a1, a2) =>
            {
                return AlphaNumericComparer.CompareStatic(a1.Name, a2.Name);
            });
            SortFolders(FolderType.Arpeggio);
        }

        public void ConditionalSortArpeggios()
        {
            if (sortArpeggios)
                SortArpeggios();
        }

        public void MoveArpeggio(Arpeggio arp, Arpeggio arpBefore)
        {
            Debug.Assert(arpeggios.Contains(arp));
            arpeggios.Remove(arp);

            if (arpBefore != null)
                arpeggios.Insert(arpeggios.IndexOf(arpBefore) + 1, arp);
            else
                arpeggios.Insert(0, arp);
        }

        public bool RenameSample(DPCMSample sample, string name)
        {
            if (sample.Name == name)
                return true;
            if (string.IsNullOrEmpty(name))
                return false;

            if (samples.Find(s => s.Name == name) == null)
            {
                sample.Name = name;
                ConditionalSortSamples();
                return true;
            }

            return false;
        }

        public void SortSamples()
        {
            samples.Sort((s1, s2) =>
            {
                return AlphaNumericComparer.CompareStatic(s1.Name, s2.Name);
            });
            SortFolders(FolderType.Sample);
        }

        public void ConditionalSortSamples()
        {
            if (sortSamples)
                SortSamples();
        }

        public void MoveSample(DPCMSample sample, DPCMSample sampleBefore)
        {
            Debug.Assert(samples.Contains(sample));
            samples.Remove(sample);

            if (sampleBefore != null)
                samples.Insert(samples.IndexOf(sampleBefore) + 1, sample);
            else
                samples.Insert(0, sample);
        }

        public bool RenameSong(Song song, string name)
        {
            if (song.Name == name)
                return true;
            if (string.IsNullOrEmpty(name))
                return false;

            if (songs.Find(s => s.Name == name) == null)
            {
                song.Name = name;
                return true;
            }

            return false;
        }

        public void SortSongs()
        {
            songs.Sort((s1, s2) =>
            {
                return AlphaNumericComparer.CompareStatic(s1.Name, s2.Name);
            });
            SortFolders(FolderType.Song);
        }

        public void ConditionalSortSongs()
        {
            if (sortSongs)
                SortSongs();
        }

        public void MoveSong(Song song, Song songBefore)
        {
            Debug.Assert(songs.Contains(song));
            songs.Remove(song);

            if (songBefore != null)
                songs.Insert(songs.IndexOf(songBefore) + 1, song);
            else
                songs.Insert(0, song);
        }

        public void SortFolders(int type)
        {
            var foldersForType    = new List<Folder>();
            var foldersOtherTypes = new List<Folder>();

            foreach (var f in folders)
            {
                if (f.Type == type)
                    foldersForType.Add(f);
                else
                    foldersOtherTypes.Add(f);
            }

            foldersForType.Sort((f1, f2) =>
            {
                return AlphaNumericComparer.CompareStatic(f1.Name, f2.Name);
            });

            folders.Clear();
            folders.AddRange(foldersOtherTypes);
            folders.AddRange(foldersForType);
        }

        public void SetExpansionAudioMask(int newExpansionMask, int numChannels = 1, bool resizeN163RAM = true)
        {
            if ((newExpansionMask & ExpansionType.N163Mask) != 0 && numChannels == 0)
                newExpansionMask &= ~ExpansionType.N163Mask;

            var oldExpansionMask   = expansionMask;
            var oldNumN163Channels = expansionNumN163Channels;

            expansionMask = newExpansionMask;
            expansionNumN163Channels = (newExpansionMask & ExpansionType.N163Mask) != 0 ? numChannels : 1;

            foreach (var song in songs)
            {
                song.CreateChannels(true, oldExpansionMask, oldNumN163Channels);
            }

            if (oldExpansionMask != newExpansionMask)
            {
                for (int i = instruments.Count - 1; i >= 0; i--)
                {
                    var inst = instruments[i];
                    if (!UsesExpansionAudio(inst.Expansion))
                        DeleteInstrument(inst);
                }
            }

            if (oldNumN163Channels != expansionNumN163Channels && resizeN163RAM)
            {
                foreach (var inst in instruments)
                {
                    inst.NotifyN163RAMSizeChanged();
                }
            }
        }

        public int GetActiveChannelCount()
        {
            int channelCount = 0;
            for (int i = 0; i < ChannelType.Count; i++)
                if (IsChannelActive(i)) channelCount++;
            return channelCount;
        }

        public int[] GetActiveChannelList()
        {
            var activeChannels = new List<int>();
            for (int i = 0; i < ChannelType.Count; i++)
                if (IsChannelActive(i)) activeChannels.Add(i);
            return activeChannels.ToArray();
        }

        public bool IsChannelActive(int channelType)
        {
            return IsChannelActive(channelType, expansionMask, expansionNumN163Channels);
        }

        public int[] GetActiveExpansions()
        {
            var idx = 1;
            var expansions = new int[Utils.NumberOfSetBits(expansionMask) + 1];

            expansions[0] = ExpansionType.None;

            for (int i = ExpansionType.Start; i <= ExpansionType.End; i++)
            {
                if (UsesExpansionAudio(i))
                    expansions[idx++] = i;
            }

            return expansions;
        }

        public static bool IsChannelActive(int channelType, int expansionMask, int numN163Channels)
        {
            if (channelType < ChannelType.ExpansionAudioStart)
                return true;

            if (ChannelType.GetExpansionTypeForChannelType(channelType) == ExpansionType.Vrc6)
                return (expansionMask & ExpansionType.Vrc6Mask) != 0;

            if (ChannelType.GetExpansionTypeForChannelType(channelType) == ExpansionType.Vrc7)
                return (expansionMask & ExpansionType.Vrc7Mask) != 0;

            if (ChannelType.GetExpansionTypeForChannelType(channelType) == ExpansionType.Fds)
                return (expansionMask & ExpansionType.FdsMask) != 0;

            if (ChannelType.GetExpansionTypeForChannelType(channelType) == ExpansionType.Mmc5)
                return (expansionMask & ExpansionType.Mmc5Mask) != 0 && channelType != ChannelType.Mmc5Dpcm;

            if (ChannelType.GetExpansionTypeForChannelType(channelType) == ExpansionType.N163)
                return (expansionMask & ExpansionType.N163Mask) != 0 && ChannelType.GetExpansionChannelIndexForChannelType(channelType) < numN163Channels;

            if (ChannelType.GetExpansionTypeForChannelType(channelType) == ExpansionType.S5B)
                return (expansionMask & ExpansionType.S5BMask) != 0;

            if (ChannelType.GetExpansionTypeForChannelType(channelType) == ExpansionType.EPSM)
                return (expansionMask & ExpansionType.EPSMMask) != 0;
				
            Debug.Assert(false);

            return false;
        }

        public bool NeedsExpansionInstruments
        {
            get
            {
                for (int i = ExpansionType.Start; i <= ExpansionType.End; i++)
                {
                    if (UsesExpansionAudio(i) && ExpansionType.NeedsExpansionInstrument(i))
                        return true;
                }

                return false;
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

        public Folder CreateFolder(int type, string name = null)
        {
            if (name == null)
                name = GenerateUniqueFolderName(type);
            else if (FolderExists(type, name))
                return GetFolder(type, name);

            var folder = new Folder(type, name);
            folders.Add(folder);
            ConditionalSortFolders(type);
            return folder;
        }

        public void DeleteFolder(int type, string name)
        {
            Debug.Assert(
                (type == FolderType.Song && songs.Find(s => s.FolderName == name) == null) ||
                (type == FolderType.Instrument && instruments.Find(i => i.FolderName == name) == null) ||
                (type == FolderType.Arpeggio && arpeggios.Find(a => a.FolderName == name) == null) ||
                (type == FolderType.Sample && samples.Find(s => s.FolderName == name) == null));

            folders.RemoveAll(f => f.Type == type && f.Name == name);
        }

        public bool RenameFolder(int type, Folder folder, string name)
        {
            if (folder.Name == name)
                return true;
            if (string.IsNullOrEmpty(name))
                return false;

            if (!FolderExists(type, name))
            {
                var oldName = folder.Name;
                folder.Name = name;

                switch (type)
                {
                    case FolderType.Song:
                        songs.ForEach(s => { if (s.FolderName == oldName) s.FolderName = name; });
                        ConditionalSortSongs();
                        break;
                    case FolderType.Instrument:
                        instruments.ForEach(i => { if (i.FolderName == oldName) i.FolderName = name; });
                        ConditionalSortInstruments();
                        break;
                    case FolderType.Arpeggio:
                        arpeggios.ForEach(a => { if (a.FolderName == oldName) a.FolderName = name; });
                        ConditionalSortArpeggios();
                        break;
                    case FolderType.Sample:
                        samples.ForEach(s => { if (s.FolderName == oldName) s.FolderName = name; });
                        ConditionalSortSamples();
                        break;
                }

                return true;
            }

            return false;
        }

        private void EnsureFolderExist(int type, string name)
        {
            if (string.IsNullOrEmpty(name)) 
                return;
            CreateFolder(type, name);
        }

        public void EnsureAllFoldersExist(int type)
        {
            // Here we copy the lists since creating folders may re-sort stuff.
            switch (type)
            {
                case FolderType.Song:
                    var songsCopy = songs.ToArray();
                    foreach (var s in songsCopy)
                        EnsureFolderExist(FolderType.Song, s.FolderName);
                    break;
                case FolderType.Instrument:
                    var instrumentsCopy = instruments.ToArray();
                    foreach (var i in instrumentsCopy)
                        EnsureFolderExist(FolderType.Instrument, i.FolderName);
                    break;
                case FolderType.Arpeggio:
                    var arpeggiosCopy = arpeggios.ToArray();
                    foreach (var a in arpeggiosCopy)
                        EnsureFolderExist(FolderType.Arpeggio, a.FolderName);
                    break;
                case FolderType.Sample:
                    var samplesCopy = samples.ToArray();
                    foreach (var s in samplesCopy)
                        EnsureFolderExist(FolderType.Sample, s.FolderName);
                    break;
            }

        }

        public void EnsureAllFoldersExist()
        {
            EnsureAllFoldersExist(FolderType.Song);
            EnsureAllFoldersExist(FolderType.Instrument);
            EnsureAllFoldersExist(FolderType.Arpeggio);
            EnsureAllFoldersExist(FolderType.Sample);
        }

        public void MoveFolder(Folder folder, Folder folderBefore)
        {
            Debug.Assert(folders.Contains(folder));
            folders.Remove(folder);

            if (folderBefore != null)
                folders.Insert(folders.IndexOf(folderBefore) + 1, folder);
            else
                folders.Insert(0, folder);
        }

        private void ConditionalSortFolders(int type)
        {
            switch (type)
            {
                case FolderType.Song:
                    ConditionalSortSongs(); 
                    break;
                case FolderType.Instrument: 
                    ConditionalSortInstruments(); 
                    break;
                case FolderType.Sample:     
                    ConditionalSortSamples(); 
                    break;
                case FolderType.Arpeggio:  
                    ConditionalSortArpeggios(); 
                    break;
            }
        }

        public void DeleteUnmappedSamples()
        {
            var usedSamples = new List<DPCMSample>();

            foreach (var inst in instruments)
            {
                inst.GetTotalMappedSampleSize(usedSamples);
            }

            // Preserve ordering in case sorting isnt enabled.
            for (var i = samples.Count - 1; i >= 0; i--)
            {
                if (!usedSamples.Contains(samples[i]))
                    samples.RemoveAt(i);
            }

            ConditionalSortSamples();
        }

        public int GetTotalSampleSize()
        {
            lock (DPCMSample.ProcessedDataLock)
            {
                int size = 0;
                foreach (var sample in samples)
                    size += sample.ProcessedData.Length;
                return Math.Min(MaxMappedSampleSize, size);
            }
        }

        public int GetTotalMappedSampleSize()
        {
            lock (DPCMSample.ProcessedDataLock)
            {
                var size = 0;
                var visitedSamples = new List<DPCMSample>(samples.Count);

                foreach (var inst in instruments)
                {
                    size += inst.GetTotalMappedSampleSize(visitedSamples);
                }

                return size;
            }
        }

        private int AutoAssignSamplesBanksRandom(int seed, int bankSize, out bool overflow)
        {
            var rnd = new Random(seed);
            var randomizedSamples = new List<DPCMSample>();
            randomizedSamples.AddRange(samples);

            for (int i = 0; i < randomizedSamples.Count; i++)
            {
                var i0 = rnd.Next(randomizedSamples.Count);
                var i1 = rnd.Next(randomizedSamples.Count);

                Utils.Swap(randomizedSamples, i0, i1);
            }

            foreach (var s in randomizedSamples)
                s.Bank = -1;

            overflow = false;

            var numBanks = 0;
            var bankSizes = new int[MaxDPCMBanks];

            for (var i = 0; i < randomizedSamples.Count; i++)
            {
                var sample = randomizedSamples[i];
                var sampleSize = Utils.AlignSampleOffset(sample.ProcessedData.Length);
                var foundBank = false;

                // Try to insert in existing bank.
                for (var j = 0; j < numBanks; j++)
                {
                    var bankRemainingSize = bankSize - bankSizes[j];
                    if (bankRemainingSize >= sampleSize)
                    {
                        sample.Bank = j;
                        bankSizes[j] += sampleSize;
                        foundBank = true;
                        break;
                    }
                }

                // Start a new bank otherwise
                if (!foundBank)
                {
                    if (numBanks == MaxDPCMBanks - 1)
                    {
                        sample.Bank = 0;
                        overflow = true;
                    }
                    else
                    {
                        sample.Bank = numBanks;
                        bankSizes[numBanks] += sampleSize;
                        numBanks++;
                    }
                }
            }

            return numBanks;
        }

        public int AutoAssignSamplesBanks(int bankSize, out bool overflow)
        {
            const int NumAttempts = 32;

            var optimalNumberOfBanks = Utils.DivideAndRoundUp(GetTotalSampleSize(), bankSize);
            var bestSeed = -1;
            var bestNumBanks = MaxDPCMBanks + 1;
            var bestSizeFirstBanks = 0;

            // This is basically a bin-packing problem. Do a bunch of a attempts
            // inserting samples in random order and keep the best packing.
            for (int k = 0; k < NumAttempts; k++)
            {
                var seedNumBanks = AutoAssignSamplesBanksRandom(k, bankSize, out _);

                if (seedNumBanks < bestNumBanks)
                {
                    bestNumBanks = seedNumBanks;
                    bestSeed = k;
                }
                
                if (seedNumBanks == optimalNumberOfBanks)
                {
                    // If optimal size, favor tries that put more samples in first banks.
                    var sizeFirstBanks = 0;

                    for (int i = 0; i < seedNumBanks - 1; i++)
                        sizeFirstBanks += GetBankSize(i);
                    
                    if (sizeFirstBanks > bestSizeFirstBanks)
                    {
                        bestSizeFirstBanks = sizeFirstBanks;
                        bestNumBanks = seedNumBanks;
                        bestSeed = k;
                    }
                }
            }

            var numBanks = AutoAssignSamplesBanksRandom(bestSeed, bankSize, out overflow);

            if (overflow)
            {
                // Can't pack within budget, assign to bank zero, will get truncated.
                Log.LogMessage(LogSeverity.Warning, $"Unable to pack DPCM samples to stay within {MaxMappedSampleSize} bytes limit. Some samples will not play correctly.");
            }

            return numBanks;
        }

        public List<DPCMSample> GetUsedSamplesInBank(int bank)
        {
            var samplesInBank = new List<DPCMSample>(samples.Count);

            foreach (var inst in instruments)
            {
                if (inst.HasAnyMappedSamples)
                {
                    foreach (var kv in inst.SamplesMapping)
                    {
                        var s = kv.Value.Sample;
                        if (s != null && s.Bank == bank && !samplesInBank.Contains(s))
                            samplesInBank.Add(s);
                    }
                }
            }

            // Keep things a bit more deterministic.
            samplesInBank.Sort((s1, s2) => s1.Id.CompareTo(s2.Id));

            return samplesInBank;
        }

        public int GetSampleBankOffset(DPCMSample sample)
        {
            var samplesInSameBank = GetUsedSamplesInBank(sample.Bank);
            var offset = 0;
            
            for (int i = 0; i < samplesInSameBank.Count; i++)
            {
                var s = samplesInSameBank[i];
                if (s == sample)
                    return offset;
                offset += Utils.AlignSampleOffset(s.ProcessedData.Length);
            }

            return -1; // Sample isnt used.
        }

        public int GetBankSize(int bank)
        {
            var samplesInBank = GetUsedSamplesInBank(bank);
            var size = 0;

            for (int i = 0; i < samplesInBank.Count; i++)
            {
                size += Utils.AlignSampleOffset(samplesInBank[i].ProcessedData.Length);
            }

            return size;
        }

        public byte[] GetPackedSampleData(int bank = 0, int maxBankSize = -1)
        {
            var samplesInBank = GetUsedSamplesInBank(bank);
            var sampleData = new List<byte>();

            foreach (var sample in samplesInBank)
            {
                sampleData.AddRange(sample.ProcessedData);
                var paddedSize = Utils.AlignSampleOffset(sampleData.Count) - sampleData.Count;
                for (int i = 0; i < paddedSize; i++)
                    sampleData.Add(0x55);
            }

            if (maxBankSize > 0 && sampleData.Count > maxBankSize)
                sampleData.RemoveRange(maxBankSize, sampleData.Count - maxBankSize);

            return sampleData.ToArray();
        }

        public void DeleteAllSongsBut(int[] songIds, bool deleteUnusedData = true)
        {
            if (songIds != null)
            {
                for (int i = songs.Count - 1; i >= 0; i--)
                {
                    var song = songs[i];

                    if (Array.IndexOf(songIds, song.Id) < 0)
                    {
                        DeleteSong(song);
                    }
                }
            }

            if (deleteUnusedData)
            {
                Cleanup();
            }
        }

        public void Cleanup()
        {
            DeleteUnusedInstruments();
            UnmapUnusedSamples();
            DeleteUnmappedSamples();
            DeleteUnusedArpeggios();
        }

        public bool MergeProject(Project otherProject)
        {
            // These validations only make sense when merging songs.
            if (otherProject.Songs.Count > 0)
            {
                if (!ExpansionType.IsMaskIncludedInTheOther(expansionMask, otherProject.expansionMask))
                {
                    Log.LogMessage(LogSeverity.Error, $"Cannot import from a project that uses audio expansions ({ExpansionType.GetStringForMask(otherProject.expansionMask, true)}) that are not enabled in the current project.");
                    return false;
                }

                if (otherProject.tempoMode != tempoMode)
                {
                    Log.LogMessage(LogSeverity.Error, $"Cannot import from a project that uses a different tempo mode.");
                    return false;
                }

                otherProject.SetExpansionAudioMask(expansionMask, expansionNumN163Channels);
            }

            // Change all the IDs in the source project.
            List<int> allOtherIds = new List<int>();
            foreach (var inst in otherProject.Instruments)
                inst.ChangeId(GenerateUniqueId());
            foreach (var arp in otherProject.Arpeggios)
                arp.ChangeId(GenerateUniqueId());
            foreach (var sample in otherProject.Samples)
                sample.ChangeId(GenerateUniqueId());
            foreach (var song in otherProject.Songs)
            {
                song.ChangeId(GenerateUniqueId());
                foreach (var channels in song.Channels)
                {
                    foreach (var pattern in channels.Patterns)
                        pattern.ChangeId(GenerateUniqueId());
                }
            }

            // Purely to pass validation.
            otherProject.EnsureNextIdIsLargeEnough(); 
            otherProject.ValidateIntegrity();

            if (otherProject.Songs.Count > 0)
            {
                // Ignore songs that have name conflicts.
                for (int i = 0; i < otherProject.songs.Count;)
                {
                    var otherSong = otherProject.songs[i];
                    if (GetSong(otherSong.Name) != null)
                    {
                        Log.LogMessage(LogSeverity.Warning, $"Project already contains a song named '{otherSong.Name}', ignoring.");
                        otherProject.DeleteSong(otherSong);
                    }
                    else
                    {
                        i++;
                    }
                }

                if (otherProject.Songs.Count == 0)
                {
                    Log.LogMessage(LogSeverity.Warning, "No songs to import. Aborting.");
                    return false;
                }

                otherProject.Cleanup();
                otherProject.ValidateIntegrity();
            }

            // Match existing samples by name.
            if (otherProject.Samples.Count > 0)
            {
                for (int i = 0; i < otherProject.samples.Count; i++)
                {
                    var otherSample = otherProject.samples[i];
                    var existingSample = GetSample(otherSample.Name);
                    if (existingSample != null)
                    {
                        Log.LogMessage(LogSeverity.Warning, $"Project already contains a DPCM sample named '{existingSample.Name}', assuming it is the same.");

                        otherProject.ReplaceSampleInAllMappings(otherSample, existingSample);
                        otherProject.samples.Insert(otherProject.samples.IndexOf(otherSample), existingSample); // To pass validation.
                        otherProject.DeleteSample(otherSample);
                        otherSample.SetProject(this);
                    }
                    else
                    {
                        samples.Add(otherSample);
                        otherSample.SetProject(this);
                    }

                    EnsureFolderExist(FolderType.Sample, otherSample.FolderName);
                }

                ValidateIntegrity();
            }

            // Match existing instruments by name.
            if (otherProject.instruments.Count > 0)
            {
                for (int i = 0; i < otherProject.instruments.Count; i++)
                {
                    var otherInstrument = otherProject.instruments[i];
                    if (otherInstrument.Expansion == ExpansionType.None || (expansionMask & ExpansionType.GetMaskFromValue(otherInstrument.Expansion)) != 0)
                    { 
                        var existingInstrument = GetInstrument(otherInstrument.Name);
                        if (existingInstrument != null)
                        {
                            Log.LogMessage(LogSeverity.Warning, $"Project already contains an instrument named '{existingInstrument.Name}', assuming it is the same.");

                            if (existingInstrument.Expansion == otherInstrument.Expansion)
                            {
                                otherProject.ReplaceInstrument(otherInstrument, existingInstrument);
                                otherProject.instruments.Insert(otherProject.instruments.IndexOf(otherInstrument), existingInstrument); // To pass validation.
                                otherProject.DeleteInstrument(otherInstrument);
                                otherInstrument.SetProject(this);
                            }
                            else
                            {
                                Log.LogMessage(LogSeverity.Warning, $"Instrument '{otherInstrument.Name}' already exists but uses a different expansion, renaming.");

                                // Generate a new name that is unique in both projects.
                                for (int j = 2; ; j++)
                                {
                                    var newName = otherInstrument.Name += $" ({j})";
                                    if (GetInstrument(newName) == null && instruments.FindIndex(k => k.Name == newName) < 0)
                                    {
                                        otherInstrument.Name = newName;
                                        break;
                                    }
                                }

                                otherInstrument.SetProject(this);
                                instruments.Add(otherInstrument);
                            }
                        }
                        else 
                        {
                            otherInstrument.SetProject(this);
                            instruments.Add(otherInstrument);
                        }

                        EnsureFolderExist(FolderType.Instrument, otherInstrument.FolderName);
                    }
                    else
                    {
                        Log.LogMessage(LogSeverity.Warning, $"Instrument '{otherInstrument.Name}' uses an inactive audio expansion, ignoring.");
                    }

                    otherInstrument.PerformPostLoadActions();
                }

                ValidateIntegrity();
            }

            // Match existing arpeggios by name.
            if (otherProject.arpeggios.Count > 0)
            {
                for (int i = 0; i < otherProject.arpeggios.Count; i++)
                {
                    var otherArpeggio = otherProject.arpeggios[i];
                    var existingArpeggio = GetArpeggio(otherArpeggio.Name);
                    if (existingArpeggio != null)
                    {
                        Log.LogMessage(LogSeverity.Warning, $"Project already contains an arpeggio named '{existingArpeggio.Name}', assuming it is the same.");

                        otherProject.ReplaceArpeggio(otherArpeggio, existingArpeggio);
                        otherProject.arpeggios.Insert(otherProject.arpeggios.IndexOf(otherArpeggio), existingArpeggio); // To pass validation.
                        otherProject.DeleteArpeggio(otherArpeggio);
                        otherArpeggio.SetProject(this);
                    }
                    else
                    {
                        arpeggios.Add(otherArpeggio);
                        otherArpeggio.SetProject(this);
                    }

                    EnsureFolderExist(FolderType.Arpeggio, otherArpeggio.FolderName);
                }

                ValidateIntegrity();
            }

            // Finally add the songs.
            foreach (var song in otherProject.Songs)
            {
                song.SetProject(this);
                songs.Add(song);

                EnsureFolderExist(FolderType.Song, song.FolderName);
            }

            ConditionalSortEverything();
            ValidateIntegrity();

            return true;
        }

        private void SortEverything(bool songs)
        {
            SortInstruments();
            SortArpeggios();
            SortSamples();
            if (songs)
                SortSongs();
        }

        public void ConditionalSortEverything()
        {
            ConditionalSortSongs();
            ConditionalSortInstruments();
            ConditionalSortSamples();
            ConditionalSortArpeggios();
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

        public void RemoveDpcmNotesWithoutMapping()
        {
            foreach (var song in songs)
                song.RemoveDpcmNotesWithoutMapping();
        }

        public void PermanentlyApplyGrooves()
        {
            foreach (var song in songs)
                song.PermanentlyApplyGrooves();
        }

        public void ConvertToFamiStudioTempo()
        {
            Debug.Assert(UsesFamiTrackerTempo);

            tempoMode = TempoType.FamiStudio;

            foreach (var song in songs)
                song.ConvertToFamiStudioTempo();
        }

        public void ConvertToFamiTrackerTempo(bool setDefaults)
        {
            Debug.Assert(UsesFamiStudioTempo);

            if (setDefaults)
            {
                foreach (var song in songs)
                {
                    song.SetDefaultsForTempoMode(TempoType.FamiTracker);
                    song.UpdatePatternStartNotes();
                }
            }
            else
            {
                foreach (var song in songs)
                {
                    song.PermanentlyApplyGrooves();
                    song.FamitrackerTempo = Song.NativeTempoNTSC;
                    song.FamitrackerSpeed = 1;
                }
            }

            tempoMode = TempoType.FamiTracker;

            foreach (var song in songs)
                song.ClearCustomPatternSettingsForFamitrackerTempo();
        }

        public void ConvertToCompoundNotes()
        {
            foreach (var song in songs)
                song.ConvertToCompoundNotes();
        }

        public void ConvertToSimpleNotes()
        {
            foreach (var song in songs)
                song.ConvertToSimpleNotes();
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

            // Preserve ordering in case sorting isnt enabled.
            for (var i = instruments.Count - 1; i >= 0; i--)
            {
                if (!usedInstruments.Contains(instruments[i]))
                    instruments.RemoveAt(i);
            }

            ConditionalSortInstruments();
        }

        public void UnmapUnusedSamples()
        {
            foreach (var inst in instruments)
            {
                if (inst.HasAnyMappedSamples)
                {
                    var unusedNotes = new HashSet<int>();

                    foreach (var kv in inst.SamplesMapping)
                    {
                        unusedNotes.Add(kv.Key);
                    }

                    foreach (var song in songs)
                    {
                        var channel = song.Channels[ChannelType.Dpcm];
                        for (int p = 0; p < song.Length; p++)
                        {
                            var pattern = channel.PatternInstances[p];
                            if (pattern != null)
                            {
                                foreach (var note in pattern.Notes.Values)
                                {
                                    if (note.IsMusical && note.Instrument == inst)
                                    {
                                        var mapping = inst.GetDPCMMapping(note.Value);
                                        if (mapping != null && unusedNotes.Contains(note.Value))
                                        {
                                            unusedNotes.Remove(note.Value);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    foreach (var note in unusedNotes)
                    {
                        inst.UnmapDPCMSample(note);
                    }
                }
            }
        }

        public void PermanentlyApplyAllSamplesProcessing()
        {
            foreach (var sample in samples)
            {
                sample.PermanentlyApplyAllProcessing();
            }
        }

        public void DeleteAllN163FdsResampleWavData()
        {
            foreach (var inst in instruments)
            {
                inst.DeleteFdsResampleWavData();
                inst.DeleteN163ResampleWavData();
            }
        }

        public void DeleteUnusedArpeggios()
        {
            var usedArpeggios = new HashSet<Arpeggio>();

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
                                if (note.IsArpeggio)
                                {
                                    //Debug.Assert(note.IsMusical);
                                    usedArpeggios.Add(note.Arpeggio);
                                }
                            }
                        }
                    }
                }
            }

            // Preserve ordering in case sorting isnt enabled.
            for (var i = arpeggios.Count - 1; i >= 0; i--)
            {
                if (!usedArpeggios.Contains(arpeggios[i]))
                    arpeggios.RemoveAt(i);
            }

            ConditionalSortArpeggios();
        }

        class N163Mask
        {
            public ulong[] Bits;
            public int MinIndex = int.MaxValue;
            public int MaxIndex = int.MinValue;

            public N163Mask(int numLongs)
            {
                Bits = new ulong[numLongs];
            }
        };

        public unsafe bool AutoAssignN163WavePositions(out Dictionary<int, int> wavePositions)
        {
            if (!UsesN163Expansion)
            {
                wavePositions = null;
                return true;
            }

            var numN163Instruments = instruments.Count(i => i.IsN163);
            var numN163AutoPos = instruments.Count(i => i.IsN163 && i.N163WaveAutoPos);

            if (numN163AutoPos == 0)
            {
                wavePositions = null;
                return true;
            }

            //var A = Platform.TimeSeconds();

            const int numBitsPerLong = sizeof(ulong) * 8;

            var instrumentOverlaps = new Dictionary<Instrument, HashSet<Instrument>>(numN163AutoPos);
            
            foreach (var song in songs)
            {
                var numFrames = song.EndLocation.ToAbsoluteNoteIndex(song);
                var numLongs = Utils.DivideAndRoundUp(numFrames, numBitsPerLong);
                var instrumentMasks = new Dictionary<Instrument, N163Mask>(numN163Instruments);

                // Buld a huge bitmask that tells us which frame each instrument is used.
                // This is a bit overkill, but is very simple.
                for (var i = 0; i < instruments.Count; i++)
                {
                    var inst = instruments[i];
                    if (inst.IsN163)
                    {
                        instrumentMasks.Add(inst, new N163Mask(numLongs));
                    }
                }

                for (var i = 0; i < expansionNumN163Channels; i++)
                {
                    var channel = song.GetChannelByType(ChannelType.N163Wave1 + i);

                    // TODO : This ignores FamiTracker delayed cuts, so it may generate sub-optimal results when they are used.
                    for (var it = channel.GetSparseNoteIterator(song.StartLocation, song.EndLocation, NoteFilter.Musical); !it.Done; it.Next())
                    {
                        var note = it.Note;

                        if (note.Instrument != null)
                        {
                            var mask = instrumentMasks[note.Instrument];
                            
                            // Start + end of this note, in frame.
                            var idx1 = it.Location.ToAbsoluteNoteIndex(song);
                            var idx2 = Math.Min(idx1 + Math.Min(note.Duration, it.DistanceToNextNote), numFrames - 1);

                            var long1 = idx1 / numBitsPerLong;
                            var long2 = idx2 / numBitsPerLong;
                            var bit1  = idx1 % numBitsPerLong;
                            var bit2  = idx2 % numBitsPerLong;

                            var mask1 = ~((1ul << bit1) - 1ul);
                            var mask2 =  ((1ul << bit2) - 1ul);

                            mask.MinIndex = Math.Min(long1, mask.MinIndex);
                            mask.MaxIndex = Math.Max(long2, mask.MaxIndex);

                            if (long1 == long2)
                            {
                                mask.Bits[long1] |= (mask1 & mask2);
                            }
                            else
                            {
                                mask.Bits[long1] |= mask1;
                                mask.Bits[long2] |= mask2;

                                for (var j = long1 + 1; j < long2; j++)
                                    mask.Bits[j] = ulong.MaxValue;
                            }
                        }
                    }
                }

                // Then "AND" these masks to figure out which instruments overlap, update the map of overlaps.
                // We create overlaps both ways, so this is O(N^2/2) i guess.
                var it1 = instrumentMasks.GetEnumerator();
                while (it1.MoveNext())
                {
                    var inst1 = it1.Current.Key;
                    var mask1 = it1.Current.Value;

                    var it2 = it1;
                    while (it2.MoveNext())
                    {
                        var inst2 = it2.Current.Key;
                        var mask2 = it2.Current.Value;

                        if ((inst1.N163WaveAutoPos || inst2.N163WaveAutoPos) &&
                            (mask1.MaxIndex >= mask1.MinIndex) &&
                            (mask2.MaxIndex >= mask2.MinIndex))
                        {
                            var idx1 = Math.Max(mask1.MinIndex, mask2.MinIndex);
                            var idx2 = Math.Min(mask1.MaxIndex, mask2.MaxIndex);

                            for (var i = idx1; i <= idx2; i++)
                            {
                                if ((mask1.Bits[i] & mask2.Bits[i]) != 0)
                                {
                                    if (inst1.N163WaveAutoPos)
                                    {
                                        if (!instrumentOverlaps.TryGetValue(inst1, out var set1))
                                        {
                                            set1 = new HashSet<Instrument>(numN163Instruments);
                                            instrumentOverlaps.Add(inst1, set1);
                                        }
                                        set1.Add(inst2);
                                    }
                                    if (inst2.N163WaveAutoPos)
                                    {
                                        if (!instrumentOverlaps.TryGetValue(inst2, out var set2))
                                        {
                                            set2 = new HashSet<Instrument>(numN163Instruments);
                                            instrumentOverlaps.Add(inst2, set2);
                                        }
                                        set2.Add(inst1);
                                    }
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            // Finally, try to assign the wave positions without generating overlap.
            // TODO : Sort instrument by descending wav size + how much they are used maybe?
            wavePositions = new Dictionary<int, int>(numN163AutoPos);

            var maxPosByte = N163WaveRAMSize * 2;
            var overlapDetected = false;

            foreach (var kv in instrumentOverlaps)
            {
                var inst1 = kv.Key;
                var pos1 = 0;
                var overlapInstruments = kv.Value;

                foreach (var inst2 in overlapInstruments)
                {
                    var pos2 = (int)inst2.N163WavePos;

                    // No position assigned yet, will be done later.
                    if (inst2.N163WaveAutoPos && !wavePositions.TryGetValue(inst2.Id, out pos2))
                    {
                        continue;
                    }

                    // If overlap, try again right after this instrument.
                    if (pos1 + inst1.N163WaveSize > pos2 &&
                        pos2 + inst2.N163WaveSize > pos1)
                    {
                        pos1 = pos2 + inst2.N163WaveSize;

                        // Was not able to allocate.
                        if (pos1 + inst1.N163WaveSize > maxPosByte)
                        {
                            Log.LogMessage(LogSeverity.Warning, $"Not able to assign a N163 wave position to instrument '{inst1.Name}', reduce wave size of overlap with other instruments. Setting to 0.");
                            pos1 = 0;
                            overlapDetected = true;
                            break;
                        }
                    }
                }

                wavePositions.Add(inst1.Id, pos1);
            }

            //var B = Platform.TimeSeconds();
            //Trace.WriteLine($"ASSIGN TIME! {(B - A) * 1000} ms");

            return !overlapDetected;
        }

        public int FindLargestUniqueId()
        {
            var largestUniqueId = 0;

            foreach (var inst in instruments)
                largestUniqueId = Math.Max(largestUniqueId, inst.Id);
            foreach (var arp in arpeggios)
                largestUniqueId = Math.Max(largestUniqueId, arp.Id);
            foreach (var sample in samples)
                largestUniqueId = Math.Max(largestUniqueId, sample.Id);
            foreach (var song in songs)
            {
                largestUniqueId = Math.Max(largestUniqueId, song.Id);
                foreach (var channels in song.Channels)
                {
                    foreach (var pattern in channels.Patterns)
                        largestUniqueId = Math.Max(largestUniqueId, pattern.Id);
                }
            }

            return largestUniqueId;
        }

        // This is to fix issues with older versions where ids go corrupted somehow,
        // likely using the old import instrument function.
        public void EnsureNextIdIsLargeEnough(int largestUniqueId)
        {
            if (largestUniqueId >= nextUniqueId)
            {
                nextUniqueId = largestUniqueId + 1;
            }
        }

        public void EnsureNextIdIsLargeEnough()
        {
            EnsureNextIdIsLargeEnough(FindLargestUniqueId());
        }

#if DEBUG
        private void ValidateDPCMSamples(Dictionary<int, object> idMap)
        {
            foreach (var sample in samples)
            {
                sample.ValidateIntegrity(this, idMap);
            }
        }

        private void ValidateInstruments(Dictionary<int, object> idMap)
        {
            foreach (var inst in instruments)
            {
                inst.ValidateIntegrity(this, idMap);
            }
        }

        private void ValidateArpeggios(Dictionary<int, object> idMap)
        {
            foreach (var arp in arpeggios)
            {
                arp.ValidateIntegrity(this, idMap);
            }
        }

        public void ValidateId(int id)
        {
            Debug.Assert(id < nextUniqueId);
        }
#endif

        public void ValidateIntegrity()
        {
#if DEBUG
            var idMap = new Dictionary<int, object>(); ;

            ValidateDPCMSamples(idMap);
            ValidateInstruments(idMap);
            ValidateArpeggios(idMap);

            foreach (var song in Songs)
                song.ValidateIntegrity(this, idMap);

            Debug.Assert(Note.EmptyNote.IsEmpty);
#endif
        }

        public bool FolderExists(int type, string name)
        {
            return folders.Exists(f => f.Type == type && f.Name == name);
        }

        public Folder GetFolder(int type, string name)
        {
            return folders.Find(f => f.Type == type && f.Name == name);
        }

        public List<Folder> GetFoldersForType(int type)
        {
            return folders.FindAll(f => f.Type == type);
        }

        public List<Song> GetSongsInFolder(string name)
        {
            return songs.FindAll(s => (name ?? "") == (s.FolderName ?? ""));
        }

        public List<Instrument> GetInstrumentsInFolder(string name)
        {
            return instruments.FindAll(i => (name ?? "") == (i.FolderName ?? ""));
        }

        public List<Arpeggio> GetArpeggiosInFolder(string name)
        {
            return arpeggios.FindAll(a => (name ?? "") == (a.FolderName ?? ""));
        }

        public List<DPCMSample> GetSamplesInFolder(string name)
        {
            return samples.FindAll(s => (name ?? "") == (s.FolderName ?? ""));
        }
        
        public void ExpandAllFolders(int type, bool expand)
        {
            folders.ForEach(f => { if (f.Type == type) f.Expanded = expand; });
        }

        public void SerializeDPCMSamples(ProjectBuffer buffer)
        {
            // Samples
            int sampleCount = samples.Count;
            buffer.Serialize(ref sampleCount);
            buffer.InitializeList(ref samples, sampleCount);

            foreach (var sample in samples)
                sample.Serialize(buffer);
        }

        // This is only used for pre-4.1.0 files where we had just 1 global "DPCM instrument".
        public void SerializeOldDPCMSamplesMapping(ProjectBuffer buffer, out Dictionary<int, DPCMSampleMapping> legacyMappings)
        {
            ulong mappingMask = 0;
            buffer.Serialize(ref mappingMask);

            if (mappingMask != 0)
            {
                legacyMappings = new Dictionary<int, DPCMSampleMapping>();

                for (int i = 0; i < 64; i++)
                {
                    if ((mappingMask & (((ulong)1) << i)) != 0)
                    {
                        const int OldDPCMNoteMin = 0x0c;

                        var mapping = new DPCMSampleMapping();
                        mapping.Serialize(buffer);
                        legacyMappings.Add(OldDPCMNoteMin + i, mapping);
                    }
                }
            }
            else
            {
                legacyMappings = null;
            }
        }

        public void SerializeDPCMState(ProjectBuffer buffer, out Dictionary<int, DPCMSampleMapping> legacyMappings)
        {
            SerializeDPCMSamples(buffer);

            // At version 15 (FamiStudio 4.1.0) we moved DPCM samples mapping to instruments, a-la Famitracker.
            if (buffer.Version < 15)
            {
                SerializeOldDPCMSamplesMapping(buffer, out legacyMappings);
            }
            else
            {
                legacyMappings = null;
            }
        }

        private void CreateLegacyDPCMInstrument(Dictionary<int, DPCMSampleMapping> legacyMappings)
        {
            if (legacyMappings != null)
            {
                // Create the old "DPCM Instrument".
                var dpcmInstrumentName = GetInstrument("DPCM Instrument") == null ? "DPCM Instrument" : GenerateUniqueInstrumentName("DPCM Instrument");
                var dpcmInstrument = CreateInstrument(ExpansionType.None, dpcmInstrumentName);

                foreach (var kv in legacyMappings)
                {
                    dpcmInstrument.MapDPCMSample(kv.Key, kv.Value.Sample, kv.Value.Pitch, kv.Value.Loop);
                }

                // Assign this instrument to all the notes of the DPCM channel (which had a "null" instrument before)
                foreach (var song in songs)
                {
                    foreach (var pattern in song.Channels[ChannelType.Dpcm].Patterns)
                    {
                        foreach (var note in pattern.Notes.Values)
                        {
                            note.Instrument = dpcmInstrument;
                        }
                    }
                }
            }
        }

        public void SerializeInstrumentState(ProjectBuffer buffer)
        {
            int instrumentCount = instruments.Count;
            buffer.Serialize(ref instrumentCount);
            buffer.InitializeList(ref instruments, instrumentCount);
            foreach (var instrument in instruments)
                instrument.Serialize(buffer);
        }

        public void SerializeArpeggioState(ProjectBuffer buffer)
        {
            int arpeggioCount = arpeggios.Count;
            buffer.Serialize(ref arpeggioCount);
            buffer.InitializeList(ref arpeggios, arpeggioCount);
            foreach (var arp in arpeggios)
                arp.Serialize(buffer);
        }


        public void Serialize(ProjectBuffer buffer, bool includeSamples = true)
        {
            if (!buffer.IsForUndoRedo)
            {
                buffer.Serialize(ref nextUniqueId);
            }

            if (buffer.Version >= 15)
            {
                buffer.Serialize(ref sortSongs);
                buffer.Serialize(ref sortInstruments);
                buffer.Serialize(ref sortSamples);
                buffer.Serialize(ref sortArpeggios);
            }
            else if (buffer.Version >= 10)
            {
                // At version 10 (FamiStudio 3.0.0) we allowed song re-ordering, do not assume sorting.
                sortSongs = false; 
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
                buffer.Serialize(ref expansionMask);

                // At version 11 (FamiStudio 3.1.0) we added support for multiple audio expansions.
                if (buffer.Version < 12)
                    expansionMask = ExpansionType.GetMaskFromValue(expansionMask);
            }

            // At version 5 (FamiStudio 2.0.0) we added support for Namco 163 and advanced tempo mode.
            if (buffer.Version >= 5)
            {
                buffer.Serialize(ref expansionNumN163Channels);
                buffer.Serialize(ref tempoMode);
            }
            else
            {
                tempoMode = TempoType.FamiTracker;
            }

            if (buffer.Version >= 6)
            {
                buffer.Serialize(ref pal);
            }

            // At version 17 (FamiStudio 4.3.0) we added support for tuning frequency (ex: A = 440Hz).
            if (buffer.Version >= 17)
            {
                buffer.Serialize(ref tuning);
            }

            // At version 16 (FamiStudio 4.2.0) we added little folders in the project explorer and
            // some sound-engine specific options.
            if (buffer.Version >= 16)
            {
                var folderCount = folders.Count;
                buffer.Serialize(ref folderCount);
                buffer.InitializeList(ref folders, folderCount);
                foreach (var folder in folders)
                {
                    folder.Serialize(buffer);
                }

                buffer.Serialize(ref soundEngineUsesExtendedInstruments);
                buffer.Serialize(ref soundEngineUsesExtendedDpcm);
                buffer.Serialize(ref soundEngineUsesBankSwitching);
            }

            // At version 16 (FamiStudio 4.2.0) we added project-specific mixer settings that can
            // override the app's settings.
            if (buffer.Version >= 16)
            {
                var overrideMask = 0;

                if (buffer.IsWriting)
                {
                    // Only save the settings if they are overriden AND we are really using this
                    // expansion. This is in case we want to change the default settings later.
                    // We wont end up with a bunch of files with outdated settings. Note that
                    // bit 0 is 2A03, so its shifted by 1 compared to regular expansion mask.
                    for (var i = 0; i < mixerSettings.Length; i++)
                    {
                        var bit = 1 << i;
                        var expEnabled = i == 0 || (bit & (expansionMask << 1)) != 0;
                        if (mixerSettings[i].Override && expEnabled)
                            overrideMask |= bit;
                    }
                }

                if (buffer.IsForUndoRedo)
                    buffer.Serialize(ref allowMixerOverride);
                buffer.Serialize(ref overrideBassCutoffHz);
                if (overrideBassCutoffHz)
                    buffer.Serialize(ref bassCutoffHz);
                buffer.Serialize(ref overrideMask);

                if (buffer.IsReading)
                {
                    Array.Copy(ExpansionMixer.DefaultExpansionMixerSettings, mixerSettings, mixerSettings.Length);
                }

                for (var i = 0; i < mixerSettings.Length; i++)
                {
                    if ((overrideMask & (1 << i)) != 0)
                    {
                        mixerSettings[i].Serialize(buffer);
                        mixerSettings[i].Override = true;
                    }
                }
            }

            // DPCM samples
            var legacyMappings = (Dictionary<int, DPCMSampleMapping>)null;
            if (includeSamples)
            {
                SerializeDPCMState(buffer, out legacyMappings);
            }

            // Instruments
            SerializeInstrumentState(buffer);

            // At version 7 (FamiStudio 2.2.0) we added support for arpeggios.
            if (buffer.Version >= 7)
            {
                // Arpeggios
                SerializeArpeggioState(buffer);
            }

            // Songs
            int songCount = songs.Count;
            buffer.Serialize(ref songCount);
            buffer.InitializeList(ref songs, songCount);
            foreach (var song in songs)
            {
                song.Serialize(buffer);
            }

            CreateLegacyDPCMInstrument(legacyMappings);

            if (buffer.IsReading && !buffer.IsForUndoRedo)
            {
                EnsureNextIdIsLargeEnough();

                // At version 10 (FamiStudio 3.0.0) we allow users to re-order songs.
                // At version 15 (FamiStudio 4.1.0) we adding sorting options on everything.
                if (buffer.Version < 15)
                {
                    SortEverything(buffer.Version < 10);
                }

                EnsureAllFoldersExist();
            }
        }

        public Project DeepClone()
        {
            var saveSerializer = new ProjectSaveBuffer(this);
            Serialize(saveSerializer);
            var newProject = new Project();
            var loadSerializer = new ProjectLoadBuffer(newProject, saveSerializer.GetBuffer(), Version);
            newProject.Serialize(loadSerializer);
            newProject.ValidateIntegrity();
            return newProject;
        }
    }

    public static class FolderType
    {
        public const int Song       = 0;
        public const int Instrument = 1;
        public const int Arpeggio   = 2;
        public const int Sample     = 3;
    }

    public class Folder
    {
        private int    type;
        private string name;
        private bool   expanded = true;

        public int    Type     { get => type;     set => type     = value; }
        public string Name     { get => name;     set => name     = value; }
        public bool   Expanded { get => expanded; set => expanded = value; }

        public Folder()
        {
        }

        public Folder(int t, string n)
        {
            type = t;
            name = n;
        }

        public void Serialize(ProjectBuffer buffer)
        {
            buffer.Serialize(ref type);
            buffer.Serialize(ref name);
            buffer.Serialize(ref expanded);
        }
    }

    public static class ExpansionType
    {
        public const int None  = 0;
        public const int Vrc6  = 1;
        public const int Vrc7  = 2;
        public const int Fds   = 3;
        public const int Mmc5  = 4;
        public const int N163  = 5;
        public const int S5B   = 6;
        public const int EPSM  = 7;
        public const int Start = 1;
        public const int End   = 7;
        public const int Count = 8;

        public const int NoneMask = 0;
        public const int Vrc6Mask = (1 << 0);
        public const int Vrc7Mask = (1 << 1);
        public const int FdsMask  = (1 << 2);
        public const int Mmc5Mask = (1 << 3);
        public const int N163Mask = (1 << 4);
        public const int S5BMask  = (1 << 5);
        public const int EPSMMask = (1 << 6);

        public const int AllMask  = Vrc6Mask | Vrc7Mask | FdsMask | Mmc5Mask | N163Mask | S5BMask | EPSMMask;

        // Use these to display to user
        public static LocalizedString[] LocalizedNames = new LocalizedString[Count];
        public static LocalizedString[] LocalizedChipNames = new LocalizedString[0];

        // Use these to save in files, etc.
        public static readonly string[] InternalNames =
        {
            "",
            "VRC6",
            "VRC7",
            "FDS",
            "MMC5",
            "N163",
            "S5B",
            "EPSM"
        };

        // TODO: This is really UI specific, move somewhere else...
        public static readonly string[] Icons =
        {
            "Instrument",
            "InstrumentVRC6",
            "InstrumentVRC7",
            "InstrumentFds",
            "Instrument",
            "InstrumentNamco",
            "InstrumentSunsoft",
            "InstrumentEPSM"
        };

        public static LocalizedString NoneName;
        public static LocalizedString NoneInstrumentName;

        static ExpansionType()
        {
            Localization.LocalizeStatic(typeof(ExpansionType));
            LocalizedChipNames = (LocalizedString[])LocalizedNames.Clone();
            LocalizedChipNames[0] = NoneName;
        }

        public static bool NeedsExpansionInstrument(int value)
        {
            return value == Fds || value == N163 || value == Vrc6 || value == Vrc7 || value == S5B || value == EPSM;
        }

        // Makes sure all the bits set in "sub" are also set in "reference".
        public static bool IsMaskIncludedInTheOther(int referenceMask, int subMask)
        {
            for (int i = ExpansionType.Start; i <= ExpansionType.End; i++)
            {
                var mask = GetMaskFromValue(i);
                if ((mask & subMask) != 0 && (mask & referenceMask) == 0)
                    return false;
            }

            return true;
        }

        public static int GetValueFromMask(int mask)
        {
            Debug.Assert(Utils.NumberOfSetBits(mask) <= 1);

            if ((mask & ExpansionType.Vrc6Mask) != 0) return Vrc6;
            if ((mask & ExpansionType.Vrc7Mask) != 0) return Vrc7;
            if ((mask & ExpansionType.FdsMask)  != 0) return Fds;
            if ((mask & ExpansionType.Mmc5Mask) != 0) return Mmc5;
            if ((mask & ExpansionType.N163Mask) != 0) return N163;
            if ((mask & ExpansionType.S5BMask)  != 0) return S5B;
            if ((mask & ExpansionType.EPSMMask) != 0) return EPSM;

            return None;
        }

        public static int GetMaskFromValue(int exp)
        {
            return exp == None ? NoneMask : 1 << (exp - 1);
        }

        public enum LocalizationMode
        {
            Default,
            Instrument,
            ChipName
        };

        public static string GetLocalizedName(int idx, LocalizationMode mode = LocalizationMode.Default)
        {
            if (idx == 0 && mode == LocalizationMode.Instrument)
                return NoneInstrumentName;
            else if (idx == 0 && mode == LocalizationMode.ChipName)
                return NoneName;
            else 
                return LocalizedNames[idx];
        }

        public static int GetValueForInternalName(string str)
        {
            return Array.IndexOf(InternalNames, str);
        }

        public static int GetValueForLocalizedName(string str)
        {
            return Array.FindIndex(LocalizedNames, n => n.Value == str);
        }

        public static string GetStringForMask(int mask, bool internalNames = false)
        {
            var names = new List<string>();

            for (int i = ExpansionType.Start; i <= ExpansionType.End; i++)
            {
                var bit = GetMaskFromValue(i);
                if ((bit & mask) != 0)
                    names.Add(internalNames ? InternalNames[i] : LocalizedNames[i].Value);
            }

            return string.Join(", ", names);
        }
    }

    public static class MachineType
    {
        public const int NTSC = 0;
        public const int PAL = 1;
        public const int Dual = 2;
        public const int Count = 3;
        public const int CountNoDual = 2;

        public static LocalizedString[] LocalizedNames = new LocalizedString[Count];

        static MachineType()
        {
            Localization.LocalizeStatic(typeof(MachineType));
        }

        public static int GetValueForName(string str)
        {
            return Array.FindIndex(LocalizedNames, n => n.Value == str);
        }
    }

    public static class TempoType
    {
        public const int FamiStudio  = 0;
        public const int FamiTracker = 1;

        public static readonly string[] Names =
        {
            "FamiStudio",
            "FamiTracker"
        };

        public static int GetValueForName(string str)
        {
            return Array.IndexOf(Names, str);
        }
    }

    public struct ExpansionMixer
    {
        public ExpansionMixer(float v, float t, int rf)
        {
            VolumeDb = v;
            TrebleDb = t;
            TrebleRolloffHz = rf;
        }

        public bool Override;
        public float VolumeDb;
        public float TrebleDb;
        public int TrebleRolloffHz;

        public void Serialize(ProjectBuffer buffer)
        {
            // Override flag will be serialized as a bit mask before.
            buffer.Serialize(ref VolumeDb);
            buffer.Serialize(ref TrebleDb);
            buffer.Serialize(ref TrebleRolloffHz);
        }

        public static readonly ExpansionMixer[] DefaultExpansionMixerSettings = new ExpansionMixer[ExpansionType.Count]
        {
            new ExpansionMixer(0.0f,  -5.0f, 12000), // None
            new ExpansionMixer(0.0f,  -5.0f, 12000), // Vrc6
            new ExpansionMixer(0.0f, -15.0f, 12000), // Vrc7
            new ExpansionMixer(0.0f,   0.0f,  2000), // Fds
            new ExpansionMixer(0.0f,  -5.0f, 12000), // Mmc5
            new ExpansionMixer(0.0f, -15.0f, 12000), // N163
            new ExpansionMixer(0.0f,  -5.0f, 12000), // S5B
            new ExpansionMixer(0.0f,  -5.0f, 12000)  // EPSM
        };
    }
}
