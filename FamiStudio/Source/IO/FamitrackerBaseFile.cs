using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FamiStudio
{
    public class FamitrackerFileBase
    {
        protected static readonly int[] VibratoSpeedImportLookup = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 10, 11, 11, 11, 12 };
        protected static readonly int[] VibratoSpeedExportLookup = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 12, 15 };

        protected const byte SndChip_NONE = 0;
        protected const byte SndChip_VRC6 = 1;  // Konami VRCVI
        protected const byte SndChip_VRC7 = 2;  // Konami VRCVII
        protected const byte SndChip_FDS  = 4;  // Famicom Disk Sound
        protected const byte SndChip_MMC5 = 8;  // Nintendo MMC5
        protected const byte SndChip_N163 = 16; // Namco N-106
        protected const byte SndChip_S5B  = 32; // Sunsoft 5B

        protected const byte Effect_None = 0;
        protected const byte Effect_Speed = 1;
        protected const byte Effect_Jump = 2;
        protected const byte Effect_Skip = 3;
        protected const byte Effect_Halt = 4;
        protected const byte Effect_Volume = 5;
        protected const byte Effect_Portamento = 6;
        protected const byte Effect_Sweepup = 8;
        protected const byte Effect_Sweepdown = 9;
        protected const byte Effect_Arpeggio = 10;
        protected const byte Effect_Vibrato = 11;
        protected const byte Effect_Tremolo = 12;
        protected const byte Effect_Pitch = 13;
        protected const byte Effect_Delay = 14;
        protected const byte Effect_Dac = 15;
        protected const byte Effect_PortaUp = 16;
        protected const byte Effect_PortaDown = 17;
        protected const byte Effect_DutyCycle = 18;
        protected const byte Effect_SampleOffset = 19;
        protected const byte Effect_SlideUp = 20;
        protected const byte Effect_SlideDown = 21;
        protected const byte Effect_VolumeSlide = 22;
        protected const byte Effect_NoteCut = 23;
        protected const byte Effect_Retrigger = 24;
        protected const byte Effect_FdsModDepth = 26;
        protected const byte Effect_FdsModSpeedHi = 27;
        protected const byte Effect_FdsModSpeedLo = 28;
        protected const byte Effect_DpcmPitch = 29;
        protected const byte Effect_SunsoftEnvLo = 30;
        protected const byte Effect_SunsoftEnvHi = 31;
        protected const byte Effect_SunsoftEnvType = 32;
        protected const byte Effect_Count = 33;

        static protected readonly Dictionary<char, byte> TextToEffectLookup = new Dictionary<char, byte>
        {
            { '0', Effect_Arpeggio     },
            { '1', Effect_PortaUp      },
            { '2', Effect_PortaDown    },
            { '3', Effect_Portamento   },
            { '4', Effect_Vibrato      },
            { '7', Effect_Tremolo      },
            { 'A', Effect_VolumeSlide  },
            { 'B', Effect_Jump         },
            { 'C', Effect_Halt         },
            { 'D', Effect_Skip         },
            { 'E', Effect_Volume       },
            { 'F', Effect_Speed        },
            { 'G', Effect_Delay        },
            { 'H', Effect_Sweepup      },
            { 'I', Effect_Sweepdown    },
            { 'P', Effect_Pitch        },
            { 'Q', Effect_SlideUp      },
            { 'R', Effect_SlideDown    },
            { 'S', Effect_NoteCut      },
            { 'V', Effect_DutyCycle    },
            { 'W', Effect_DpcmPitch    },
            { 'X', Effect_Retrigger    },
            { 'Y', Effect_SampleOffset },
            { 'Z', Effect_Dac          },
        };

        static protected readonly Dictionary<char, int> FdsTextToEffectLookup = new Dictionary<char, int>
        {
            { 'H', Effect_FdsModDepth   },
            { 'I', Effect_FdsModSpeedHi },
            { 'J', Effect_FdsModSpeedLo },
        };

        static protected readonly Dictionary<byte, char> EffectToTextLookup = new Dictionary<byte, char>
        {
            { Effect_Arpeggio      , '0' },
            { Effect_PortaUp       , '1' },
            { Effect_PortaDown     , '2' },
            { Effect_Portamento    , '3' },
            { Effect_Vibrato       , '4' },
            { Effect_Tremolo       , '7' },
            { Effect_VolumeSlide   , 'A' },
            { Effect_Jump          , 'B' },
            { Effect_Halt          , 'C' },
            { Effect_Skip          , 'D' },
            { Effect_Volume        , 'E' },
            { Effect_Speed         , 'F' },
            { Effect_Delay         , 'G' },
            { Effect_Sweepup       , 'H' },
            { Effect_Sweepdown     , 'I' },
            { Effect_Pitch         , 'P' },
            { Effect_SlideUp       , 'Q' },
            { Effect_SlideDown     , 'R' },
            { Effect_NoteCut       , 'S' },
            { Effect_DutyCycle     , 'V' },
            { Effect_DpcmPitch     , 'W' },
            { Effect_Retrigger     , 'X' },
            { Effect_SampleOffset  , 'Y' },
            { Effect_Dac           , 'Z' },
            { Effect_FdsModDepth   , 'H' },
            { Effect_FdsModSpeedHi , 'I' },
            { Effect_FdsModSpeedLo , 'J' },
        };

        protected static readonly int[] ChanIdLookup = new[]
        {
            ChannelType.Square1,        // CHANID_SQUARE1
            ChannelType.Square2,        // CHANID_SQUARE2
            ChannelType.Triangle,       // CHANID_TRIANGLE
            ChannelType.Noise,          // CHANID_NOISE
            ChannelType.Dpcm,           // CHANID_DPCM
            ChannelType.Vrc6Square1,    // CHANID_VRC6_PULSE1
            ChannelType.Vrc6Square2,    // CHANID_VRC6_PULSE2
            ChannelType.Vrc6Saw,        // CHANID_VRC6_SAWTOOTH
            ChannelType.Mmc5Square1,    // CHANID_MMC5_SQUARE1
            ChannelType.Mmc5Square2,    // CHANID_MMC5_SQUARE2
            ChannelType.Mmc5Dpcm,       // CHANID_MMC5_VOICE
            ChannelType.N163Wave1,      // CHANID_N163_CHAN1
            ChannelType.N163Wave2,      // CHANID_N163_CHAN2
            ChannelType.N163Wave3,      // CHANID_N163_CHAN3
            ChannelType.N163Wave4,      // CHANID_N163_CHAN4
            ChannelType.N163Wave5,      // CHANID_N163_CHAN5
            ChannelType.N163Wave6,      // CHANID_N163_CHAN6
            ChannelType.N163Wave7,      // CHANID_N163_CHAN7
            ChannelType.N163Wave8,      // CHANID_N163_CHAN8
            ChannelType.FdsWave,        // CHANID_FDS
            ChannelType.Vrc7Fm1,        // CHANID_VRC7_CH1
            ChannelType.Vrc7Fm2,        // CHANID_VRC7_CH2
            ChannelType.Vrc7Fm3,        // CHANID_VRC7_CH3
            ChannelType.Vrc7Fm4,        // CHANID_VRC7_CH4
            ChannelType.Vrc7Fm5,        // CHANID_VRC7_CH5
            ChannelType.Vrc7Fm6,        // CHANID_VRC7_CH6
            ChannelType.S5BSquare1,     // CHANID_S5B_CH1
            ChannelType.S5BSquare2,     // CHANID_S5B_CH2
            ChannelType.S5BSquare3      // CHANID_S5B_CH3
        };

        protected static int[] InstrumentTypeLookup =
        {
            ExpansionType.Count,  // INST_NONE: Should never happen.
            ExpansionType.None,   // INST_2A03
            ExpansionType.Vrc6,   // INST_VRC6
            ExpansionType.Vrc7,   // INST_VRC7
            ExpansionType.Fds,    // INST_FDS
            ExpansionType.N163,   // INST_N163
            ExpansionType.S5B     // INST_S5B
        };

        // FamiTracker -> FamiStudio
        protected static int[] FamiTrackerToFamiStudioEnvelopeLookup =
        {
            EnvelopeType.Volume,   // SEQ_VOLUME
            EnvelopeType.Arpeggio, // SEQ_ARPEGGIO
            EnvelopeType.Pitch,    // SEQ_PITCH
            EnvelopeType.Count,    // SEQ_HIPITCH
            EnvelopeType.DutyCycle // SEQ_DUTYCYCLE
        };

        // FamiStudio -> FamiTracker
        protected static int[] FamiStudioToFamiTrackerEnvelopeLookup =
        {
             0, // Volume
             1, // Arpeggio
             2, // Pitch
             4, // DutyCycle
        };

        protected struct RowFxData
        {
            public byte fx;
            public byte param;
        }

        protected const int MaxSequences = 128;
        protected const int SequenceCount = 5;

        protected Project project;
        protected Dictionary<Pattern, RowFxData[,]> patternFxData = new Dictionary<Pattern, RowFxData[,]>();
        protected Dictionary<Pattern, int> patternLengths = new Dictionary<Pattern, int>();
        protected Dictionary<Song, int> songDurations = new Dictionary<Song, int>();
        protected Dictionary<Instrument, int> n163WaveEnvs = new Dictionary<Instrument, int>();
        protected Envelope[,] envelopes = new Envelope[MaxSequences, SequenceCount];
        protected Envelope[,] envelopesExp = new Envelope[MaxSequences, SequenceCount];
        protected int barLength = -1;

        protected Envelope GetFamiTrackerEnvelope(int exp, int famitrackerType, int idx)
        {
            if (idx < 0)
                return null;

            var list = exp == ExpansionType.None ? envelopes : envelopesExp;
            return list[idx, famitrackerType];
        }

        protected void SetFamiTrackerEnvelope(int exp, int famitrackerType, int idx, Envelope env)
        {
            var list = exp == ExpansionType.None ? envelopes : envelopesExp;
            list[idx, famitrackerType] = env;
        }

        protected int ConvertExpansionAudio(int exp)
        {
            switch (exp)
            {
                case SndChip_NONE : return ExpansionType.None;
                case SndChip_VRC6 : return ExpansionType.Vrc6;
                case SndChip_VRC7 : return ExpansionType.Vrc7;
                case SndChip_FDS  : return ExpansionType.Fds;
                case SndChip_MMC5 : return ExpansionType.Mmc5;
                case SndChip_N163 : return ExpansionType.N163;
                case SndChip_S5B  : return ExpansionType.S5B;
            }

            Log.LogMessage(LogSeverity.Error, "Unsupported audio expansion.");
            return -1; // We dont support exotic combinations.
        }

        protected Instrument CreateUniquelyNamedInstrument(int expansion, string baseName)
        {
            var name = !string.IsNullOrEmpty(baseName) && project.IsInstrumentNameUnique(baseName) ? baseName : project.GenerateUniqueInstrumentName(baseName); 
            return project.CreateInstrument(expansion, name);
        }

        protected void RenameInstrumentEnsureUnique(Instrument instrument, string baseName)
        {
            var name = !string.IsNullOrEmpty(baseName) && project.IsInstrumentNameUnique(baseName) ? baseName : project.GenerateUniqueInstrumentName(baseName);
            project.RenameInstrument(instrument, name);
        }

        protected DPCMSample CreateUniquelyNamedSampleFromDmcData(string baseName, byte[] data)
        {
            var name = !string.IsNullOrEmpty(baseName) && project.IsDPCMSampleNameUnique(baseName) ? baseName : project.GenerateUniqueDPCMSampleName(baseName);
            return project.CreateDPCMSampleFromDmcData(name, data);
        }

        protected Song CreateUniquelyNamedSong(string baseName)
        {
            var name = !string.IsNullOrEmpty(baseName) && project.IsSongNameUnique(baseName) ? baseName : project.GenerateUniqueSongName(baseName);
            return project.CreateSong(name);
        }

        protected Arpeggio GetOrCreateArpeggio(int param)
        {
            if (param == 0)
                return null;

            var name = $"Arp {param:X2}";
            var arp = project.GetArpeggio(name);

            if (arp != null)
                return arp;

            arp = project.CreateArpeggio(name);
            arp.Envelope.Length = 3;
            arp.Envelope.Loop = 0;
            arp.Envelope.Values[0] = 0;
            arp.Envelope.Values[1] = (sbyte)((param & 0xf0) >> 4);
            arp.Envelope.Values[2] = (sbyte)((param & 0x0f) >> 0);

            return arp;
        }

        protected void SetEffectValueWithRangeCheck(Pattern pattern, int n, Note note, byte fxFT, int fxFS, int val)
        {
            var min = Note.GetEffectMinValue(pattern.Song, pattern.Channel, fxFS);
            var max = Note.GetEffectMaxValue(pattern.Song, pattern.Channel, fxFS);

            if (val < min || val > max)
            {
                Log.LogMessage(LogSeverity.Warning, $"Value {val} for effect '{EffectToTextLookup[fxFT]}' is out of range. Clamping. {GetPatternString(pattern, n)}");
                val = Utils.Clamp(val, min, max);
            }

            note.SetEffectValue(fxFS, val);
        }

        protected void ApplySimpleEffects(RowFxData fx, Pattern pattern, int n, bool allowSongEffects)
        {
            Note note = null;

            switch (fx.fx)
            {
                case Effect_None:
                    return;
                case Effect_Jump:
                    // Ignore if there was a Dxx or Bxx before.
                    if (allowSongEffects && !patternLengths.ContainsKey(pattern))
                    {
                        pattern.Song.SetLoopPoint(fx.param);
                        patternLengths[pattern] = n + 1;

                        // This will be used to shorten the songs, removing unreachable patterns.
                        var lastIdx = Array.LastIndexOf(pattern.Channel.PatternInstances, pattern) + 1;
                        Debug.Assert(lastIdx >= 0);

                        if (songDurations.TryGetValue(pattern.Song, out var currentLastIdx))
                            lastIdx = Math.Max(currentLastIdx, lastIdx);

                        songDurations[pattern.Song] = lastIdx;
                    }
                    return;
                case Effect_Skip:
                    // Ignore if there was a Dxx or Bxx before.
                    if (!patternLengths.ContainsKey(pattern))
                        patternLengths[pattern] = n + 1;
                    return;
                case Effect_Speed:
                    if (pattern.Channel.SupportsEffect(Note.EffectSpeed))
                    {
                        if (fx.param <= 0x1f) // We only support speed change for now.
                            pattern.GetOrCreateNoteAt(n).Speed = Math.Max((byte)1, (byte)fx.param);
                        else
                            Log.LogMessage(LogSeverity.Warning, $"Only speed changes are supported, not tempo. Will be ignored. {GetPatternString(pattern, n)}");
                    }
                    return;
                case Effect_Pitch:
                    if (pattern.Channel.SupportsEffect(Note.EffectFinePitch))
                    {
                        pattern.GetOrCreateNoteAt(n).FinePitch = (sbyte)(0x80 - fx.param);
                    }
                    return;
                case Effect_Vibrato:
                    if (pattern.Channel.SupportsEffect(Note.EffectVibratoDepth))
                    {
                        note = pattern.GetOrCreateNoteAt(n);
                        note.VibratoDepth = (byte)(fx.param & 0x0f);
                        note.VibratoSpeed = (byte)VibratoSpeedImportLookup[fx.param >> 4];

                        if (note.VibratoDepth == 0 ||
                            note.VibratoSpeed == 0)
                        {
                            note.RawVibrato = 0;
                        }
                    }
                    return;
                case Effect_Dac:
                    if (pattern.Channel.SupportsEffect(Note.EffectDeltaCounter))
                    {
                        note = pattern.GetOrCreateNoteAt(n);
                        note.DeltaCounter = (byte)(fx.param & 0x7f);
                    }
                    return;
                case Effect_FdsModSpeedHi:
                    if (pattern.Channel.SupportsEffect(Note.EffectFdsModSpeed))
                    {
                        // TODO: If both hi/lo effects arent in a pair, this is likely not going to work.
                        note = pattern.GetOrCreateNoteAt(n);
                        if (!note.HasFdsModSpeed) note.FdsModSpeed = 0;
                        note.FdsModSpeed = (ushort)(((note.FdsModSpeed) & 0x00ff) | (fx.param << 8));
                    }
                    return;
                case Effect_FdsModSpeedLo:
                    if (pattern.Channel.SupportsEffect(Note.EffectFdsModSpeed))
                    {
                        // TODO: If both hi/lo effects arent in a pair, this is likely not going to work.
                        note = pattern.GetOrCreateNoteAt(n);
                        if (!note.HasFdsModSpeed) note.FdsModSpeed = 0;
                        note.FdsModSpeed = (ushort)(((note.FdsModSpeed) & 0xff00) | (fx.param << 0));
                    }
                    return;
                case Effect_FdsModDepth:
                    if (pattern.Channel.SupportsEffect(Note.EffectFdsModDepth))
                    {
                        pattern.GetOrCreateNoteAt(n).FdsModDepth = fx.param;
                    }
                    return;
                case Effect_DutyCycle:
                    if (pattern.Channel.SupportsEffect(Note.EffectDutyCycle))
                    {
                        SetEffectValueWithRangeCheck(pattern, n, pattern.GetOrCreateNoteAt(n), Effect_DutyCycle, Note.EffectDutyCycle, fx.param);
                    }
                    return;
                case Effect_Delay:
                    if (pattern.Channel.SupportsEffect(Note.EffectNoteDelay))
                    {
                        SetEffectValueWithRangeCheck(pattern, n, pattern.GetOrCreateNoteAt(n), Effect_Delay, Note.EffectNoteDelay, fx.param);
                    }
                    return;
                case Effect_NoteCut:
                    if (pattern.Channel.SupportsEffect(Note.EffectCutDelay))
                    {
                        SetEffectValueWithRangeCheck(pattern, n, pattern.GetOrCreateNoteAt(n), Effect_NoteCut, Note.EffectCutDelay, fx.param);
                    }
                    return;
                case Effect_Halt:
                case Effect_PortaUp:
                case Effect_PortaDown:
                case Effect_Portamento:
                case Effect_SlideUp:
                case Effect_SlideDown:
                case Effect_Arpeggio:
                case Effect_VolumeSlide:
                    // These will be applied later.
                    return;
            }

            if (EffectToTextLookup.ContainsKey(fx.fx))
                Log.LogMessage(LogSeverity.Warning, $"Effect '{EffectToTextLookup[fx.fx]}' is not supported and will be ignored. {GetPatternString(pattern, n)}");
            else
                Log.LogMessage(LogSeverity.Warning, $"Unknown effect code ({fx.fx}) and will be ignored. {GetPatternString(pattern, n)}");
        }

        protected string GetPatternString(Pattern pattern, int n)
        {
            return $"(Song={pattern.Song.Name}, Channel={ChannelType.InternalNames[pattern.ChannelType]}, Location={pattern.Name}:{n:X2})";
        }

        private bool IsVolumeSlideEffect(RowFxData fx)
        {
            return fx.fx == Effect_VolumeSlide;
        }

        private bool IsSlideEffect(RowFxData fx)
        {
            return fx.fx == Effect_PortaUp ||
                   fx.fx == Effect_PortaDown ||
                   fx.fx == Effect_Portamento ||
                   fx.fx == Effect_SlideUp ||
                   fx.fx == Effect_SlideDown;
        }

        private bool IsValidNote(Note note)
        {
            return note != null && note.IsValid;
        }

        private bool NoteHasVolume(Note note)
        {
            return note != null && note.HasVolume;
        }

        private bool FindNextEffect(Channel channel, NoteLocation location, out NoteLocation nextLocation, Dictionary<Pattern, RowFxData[,]> patternFxData, Func<Note, bool> filterNoteFunction, Func<RowFxData, bool> filterFxFunction)
        {
            nextLocation = NoteLocation.Invalid;

            var pattern = channel.PatternInstances[location.PatternIndex];

            if (pattern == null || !patternFxData.ContainsKey(pattern))
                return false;

            var patternLen = channel.Song.GetPatternLength(location.PatternIndex);
            var fxData     = patternFxData[pattern];

            for (var it = pattern.GetDenseNoteIterator(location.NoteIndex + 1, patternLen); !it.Done; it.Next())
            {
                var time = it.CurrentTime;
                var note = it.CurrentNote;
                
                var fxChanged = false;
                for (int i = 0; i < fxData.GetLength(1); i++)
                {
                    var fx = fxData[time, i];

                    if (filterFxFunction(fx))
                    {
                        fxChanged = true;
                        break;
                    }
                }

                if (filterNoteFunction(note) || fxChanged)
                {
                    nextLocation.PatternIndex = location.PatternIndex;
                    nextLocation.NoteIndex = time;
                    return true;
                }
            }

            for (int p = location.PatternIndex + 1; p < channel.Song.Length; p++)
            {
                pattern    = channel.PatternInstances[p];
                patternLen = channel.Song.GetPatternLength(p);

                if (pattern != null && patternFxData.ContainsKey(pattern))
                {
                    fxData = patternFxData[pattern];

                    for (var it = pattern.GetDenseNoteIterator(0, patternLen); !it.Done; it.Next())
                    {
                        var time = it.CurrentTime;
                        var note = it.CurrentNote;

                        var fxChanged = false;
                        for (int i = 0; i < fxData.GetLength(1); i++)
                        {
                            var fx = fxData[time, i];

                            if (filterFxFunction(fx))
                            {
                                fxChanged = true;
                                break;
                            }
                        }

                        if (filterNoteFunction(note) || fxChanged)
                        {
                            nextLocation.PatternIndex = p;
                            nextLocation.NoteIndex = time;
                            return true;
                        }
                    }
                }
            }

            nextLocation = channel.Song.EndLocation;
            return true;
        }

        private int FindBestMatchingNote(ushort[] noteTable, int pitch, int sign)
        {
            var bestIdx  = -1;
            var bestDiff = 99999;

            for (int i = 1; i < noteTable.Length; i++)
            {
                var diff = (pitch - noteTable[i]) * sign;
                if (diff >= 0 && diff < bestDiff)
                {
                    bestIdx = i;
                    bestDiff = diff;
                }
            }

            return bestIdx;
        }

        private void AssignNullInstruments(Song s)
        {
            foreach (var c in s.Channels)
            {
                if (c.IsDpcmChannel)
                    continue;

                var lastInstrument = (Instrument)null;

                for (var it = c.GetSparseNoteIterator(s.StartLocation, s.EndLocation, NoteFilter.Musical); !it.Done; it.Next())
                {
                    var note = it.Note;

                    Debug.Assert(note.IsMusical);

                    if (note.Instrument == null)
                    {
                        if (lastInstrument != null)
                        { 
                            note.Instrument = lastInstrument;
                            // Not worth displaying, not an error.
                            // Log.LogMessage(LogSeverity.Warning, $"No instrument assigned, will use previous instrument '{lastInstrument.Name}'. {GetPatternString(it.Pattern, it.Location.NoteIndex)}");
                        }
                        else
                        {
                            Log.LogMessage(LogSeverity.Warning, $"No instrument assigned, note may not be audible. {GetPatternString(it.Pattern, it.Location.NoteIndex)}");
                        }
                    }
                    else
                    {
                        lastInstrument = note.Instrument;
                    }
                }
            }
        }

        private void CreateArpeggios(Song s, Dictionary<Pattern, RowFxData[,]> patternFxData)
        {
            var processedPatterns = new HashSet<Pattern>();

            foreach (var c in s.Channels)
            {
                if (!c.SupportsArpeggios)
                    continue;

                var lastNoteInstrument = (Instrument)null;
                var lastNoteValue = (byte)Note.NoteInvalid;

                var currentArpeggio = (Arpeggio)null;

                for (int p = 0; p < s.Length; p++)
                {
                    var pattern = c.PatternInstances[p];

                    if (pattern == null || !patternFxData.ContainsKey(pattern) || processedPatterns.Contains(pattern))
                        continue;

                    processedPatterns.Add(pattern);

                    var fxData = patternFxData[pattern];
                    var patternLen = s.GetPatternLength(p);

                    for (var it = pattern.GetDenseNoteIterator(0, patternLen); !it.Done; it.Next())
                    {
                        var n    = it.CurrentTime;
                        var note = it.CurrentNote;

                        for (int i = 0; i < fxData.GetLength(1); i++)
                        {
                            var fx = fxData[n, i];

                            if (fx.fx == Effect_Arpeggio)
                            {
                                if (note == null && lastNoteInstrument != null && Note.IsMusicalNote(lastNoteValue))
                                {
                                    note = pattern.GetOrCreateNoteAt(n);
                                    note.Value = lastNoteValue;
                                    note.Instrument = lastNoteInstrument;
                                    note.HasAttack = false;
                                    it.Resync();
                                }

                                currentArpeggio = GetOrCreateArpeggio(fx.param);
                            }
                        }

                        if (note != null && note.IsMusical)
                        {
                            note.Arpeggio = currentArpeggio;
                        }

                        if (note != null && note.IsValid)
                        {
                            lastNoteValue      = note.Value;
                            lastNoteInstrument = note.Instrument;
                        }
                    }
                }
            }
        }

        private void CreateSlideNotes(Song s, Dictionary<Pattern, RowFxData[,]> patternFxData)
        {
            var processedPatterns = new HashSet<Pattern>();

            // Convert slide notes + portamento to our format.
            foreach (var c in s.Channels)
            {
                if (!c.SupportsEffect(Note.EffectVolumeSlide))
                    continue;

                var songSpeed = s.FamitrackerSpeed;
                var lastNoteInstrument = (Instrument)null;
                var lastNoteArpeggio = (Arpeggio)null;
                var lastNoteValue = (byte)Note.NoteInvalid;
                var portamentoSpeed = 0;
                var slideSpeed = 0;
                var slideShift = c.IsN163Channel ? 2 : 0;
                var slideSign = c.IsN163Channel || c.IsFdsChannel || c.IsVrc7Channel ? -1 : 1; // Inverted channels.

                for (int p = 0; p < s.Length; p++)
                {
                    var pattern = c.PatternInstances[p];

                    if (pattern == null)
                        continue;

                    var patternLen = s.GetPatternLength(p);

                    for (var it = pattern.GetDenseNoteIterator(0, patternLen); !it.Done; it.Next())
                    {
                        var location = new NoteLocation(p, it.CurrentTime);
                        var note = it.CurrentNote;

                        // Look for speed changes.
                        s.ApplySpeedEffectAt(location, ref songSpeed);

                        if (!patternFxData.ContainsKey(pattern) || processedPatterns.Contains(pattern))
                            continue;

                        var fxData = patternFxData[pattern];
                        var slideTarget = 0;

                        for (int i = 0; i < fxData.GetLength(1); i++)
                        {
                            var fx = fxData[location.NoteIndex, i];

                            // These seem to have no effect on the noise channel.
                            if (c.IsNoiseChannel && (fx.fx == Effect_SlideUp || fx.fx == Effect_SlideDown || fx.fx == Effect_Portamento))
                            {
                                fx.fx = Effect_None;
                                fx.param = 0;
                            }

                            if (fx.param != 0)
                            {
                                // When the effect it turned on, we need to add a note.
                                if ((fx.fx == Effect_PortaUp   ||
                                     fx.fx == Effect_PortaDown ||
                                     fx.fx == Effect_SlideUp   ||
                                     fx.fx == Effect_SlideDown) &&
                                    lastNoteValue >= Note.MusicalNoteMin &&
                                    lastNoteValue <= Note.MusicalNoteMax && (note == null || !note.IsValid))
                                {
                                    if (note == null)
                                    {
                                        note = pattern.GetOrCreateNoteAt(location.NoteIndex);
                                        it.Resync();
                                    }

                                    note.Value = lastNoteValue;
                                    note.Instrument = lastNoteInstrument;
                                    note.Arpeggio = lastNoteArpeggio;
                                    note.HasAttack = false;
                                }
                            }

                            if (fx.fx == Effect_PortaUp)
                            {
                                // If we have a Qxx/Rxx on the same row as a 1xx/2xx, things get weird.
                                if (slideTarget == 0)
                                    slideSpeed = (-fx.param * slideSign) << slideShift;
                            }
                            if (fx.fx == Effect_PortaDown)
                            {
                                // If we have a Qxx/Rxx on the same row as a 1xx/2xx, things get weird.
                                if (slideTarget == 0)
                                    slideSpeed = (fx.param * slideSign) << slideShift;
                            }
                            if (fx.fx == Effect_Portamento)
                            {
                                portamentoSpeed = fx.param;
                            }
                            if (fx.fx == Effect_SlideUp && note != null && note.IsMusical)
                            {
                                slideTarget = Utils.Clamp(note.Value + (fx.param & 0xf), Note.MusicalNoteMin, Note.MusicalNoteMax);
                                slideSpeed = (-((fx.param >> 4) * 2 + 1)) << slideShift;
                            }
                            if (fx.fx == Effect_SlideDown && note != null && note.IsMusical)
                            {
                                slideTarget = Utils.Clamp(note.Value - (fx.param & 0xf), Note.MusicalNoteMin, Note.MusicalNoteMax);
                                slideSpeed = (((fx.param >> 4) * 2 + 1)) << slideShift;
                            }
                        }

                        // Create a slide note.
                        if (note != null && note.IsMusical && !note.IsSlideNote)
                        {
                            var slideSource = note.Value;
                            var noteTable = NesApu.GetNoteTableForChannelType(c.Type, s.Project.PalMode, s.Project.ExpansionNumN163Channels);
                            var pitchLimit = NesApu.GetPitchLimitForChannelType(c.Type);

                            // If we have a new note with auto-portamento enabled, we need to
                            // swap the notes since our slide notes work backward compared to 
                            // FamiTracker.
                            if (portamentoSpeed != 0)
                            {
                                // Ignore notes with no attack since we created them to handle a previous slide.
                                if (note.HasAttack && lastNoteValue >= Note.MusicalNoteMin && lastNoteValue <= Note.MusicalNoteMax)
                                {
                                    slideSpeed  = portamentoSpeed;
                                    slideTarget = note.Value;
                                    slideSource = lastNoteValue;
                                    note.Value  = lastNoteValue;

                                    // In FamiTracker, 3xx on a VRC7 channel doesnt trigger the attacks.
                                    if (c.IsVrc7Channel)
                                        note.HasAttack = false;
                                }
                            }

                            // Our implementation of VRC7 pitches is quite different from FamiTracker.
                            // Compensate for larger pitches in higher octaves by shifting. We cant shift by 
                            // a large amount because the period is 9-bit and FamiTracker is restricted to 
                            // this for slides (octave never changes).
                            var octaveSlideShift = c.IsVrc7Channel && note.Value >= 12 ? 1 : 0;

                            // 3xx/Qxy/Rxy : We know which note we are sliding to and the speed, but we 
                            //               don't know how many frames it will take to get there.
                            if (slideTarget != 0)
                            {
                                Debug.Assert(!c.IsNoiseChannel);

                                // Advance in the song until we have the correct number of frames.
                                var numFrames = Math.Max(1, Math.Abs((noteTable[slideSource] - noteTable[slideTarget]) / (slideSpeed << octaveSlideShift)));
                                note.SlideNoteTarget = (byte)slideTarget;

                                // TODO: Here we consider if the start note has a delay, but ignore the end note. It might have one too.
                                var nextLocation = location;
                                s.AdvanceNumberOfFrames(ref nextLocation, numFrames, note.HasNoteDelay ? -note.NoteDelay : 0, songSpeed, s.Project.PalMode);

                                // Still to see if there is a note between the current one and the 
                                // next note, this could happen if you add a note before the slide 
                                // is supposed to finish.
                                if (FindNextEffect(c, location, out var nextLocation2, patternFxData, IsValidNote, IsSlideEffect))
                                {
                                    nextLocation = NoteLocation.Min(nextLocation, nextLocation2);

                                    // If the slide is interrupted by another slide effect, we will not reach 
                                    // the final target, but rather some intermediate note. Let's do our best
                                    // to interpolate and figure out the best note.
                                    var numFramesUntilNextSlide = s.CountFramesBetween(location, nextLocation, songSpeed, s.Project.PalMode);
                                    var ratio = Utils.Clamp(numFramesUntilNextSlide / numFrames, 0.0f, 1.0f);
                                    var intermediatePitch = (int)Math.Round(Utils.Lerp(noteTable[slideSource], noteTable[slideTarget], ratio));

                                    slideTarget = FindBestMatchingNote(noteTable, intermediatePitch, Math.Sign(slideSpeed));
                                    note.SlideNoteTarget = (byte)slideTarget;
                                }

                                if (nextLocation.IsInSong(s))
                                {
                                    // Add an extra note with no attack to stop the slide.
                                    var nextPattern = c.PatternInstances[nextLocation.PatternIndex];
                                    if (!nextPattern.Notes.TryGetValue(nextLocation.NoteIndex, out var nextNote) || !nextNote.IsValid)
                                    {
                                        nextNote = nextPattern.GetOrCreateNoteAt(nextLocation.NoteIndex);
                                        nextNote.Instrument = note.Instrument;
                                        nextNote.Value = (byte)slideTarget;
                                        nextNote.HasAttack = false;
                                        it.Resync();
                                    }
                                    else if (nextNote != null && nextNote.IsRelease)
                                    {
                                        Log.LogMessage(LogSeverity.Warning, $"A slide note ends on a release note. This is currently unsupported and will require manual correction. {GetPatternString(nextPattern, nextLocation.NoteIndex)}");
                                    }
                                }

                                // 3xx, Qxx and Rxx stops when its done.
                                slideSpeed = 0;
                            }

                            // 1xx/2xy : We know the speed at which we are sliding, but need to figure out what makes it stop.
                            else if (slideSpeed != 0 && FindNextEffect(c, location, out var nextLocation, patternFxData, IsValidNote, IsSlideEffect))
                            {
                                // See how many frames until the slide stops.
                                var numFrames = (int)Math.Round(s.CountFramesBetween(location, nextLocation, songSpeed, s.Project.PalMode));

                                // TODO: Here we consider if the start note has a delay, but ignore the end note. It might have one too.
                                numFrames = Math.Max(1, numFrames - (note.HasNoteDelay ? note.NoteDelay : 0));

                                var newNote = 0;
                                var clearSlide = false;

                                // Noise is much simpler. Or is it?
                                if (c.IsNoiseChannel)
                                {
                                    // FamiTracker clamps noise channel period between 0 and 2047. There is no 
                                    // way for us to know the state of the current period here, so we will assume
                                    // we are in the [17,31] range and stop the slide when it hits zero.
                                    if (slideSpeed < 0)
                                    {
                                        var famitrackerNoiseValue = (note.Value & 0xf) | 0x10; // See CNoiseChan::HandleNote
                                        for (int i = 1; i < numFrames; i++)
                                        {
                                            famitrackerNoiseValue += slideSpeed;
                                            if (famitrackerNoiseValue <= 0)
                                            {
                                                numFrames = i;
                                                var newNextLocation = location;
                                                s.AdvanceNumberOfFrames(ref newNextLocation, numFrames, note.HasNoteDelay ? -note.NoteDelay : 0, songSpeed, s.Project.PalMode);
                                                nextLocation = NoteLocation.Min(nextLocation, newNextLocation);
                                                Log.LogMessage(LogSeverity.Warning, $"Effect 1xx on noise channel may not song correct due to the massive differences in how both app handle those. {GetPatternString(c.PatternInstances[location.PatternIndex], location.NoteIndex)}");
                                                clearSlide = true;
                                                break;
                                            }
                                        }
                                    }

                                    var newNoteValue = note.Value + slideSpeed * numFrames;

                                    // We dont support wrapping around.
                                    if (newNoteValue < Note.MusicalNoteMin ||
                                        newNoteValue > Note.MusicalNoteMax)
                                    {
                                        newNoteValue = Utils.Clamp(newNoteValue, Note.MusicalNoteMin, Note.MusicalNoteMax);
                                        Log.LogMessage(LogSeverity.Warning, $"Noise slide tries to go outside of the {Note.GetFriendlyName(Note.MusicalNoteMin)} - {Note.GetFriendlyName(Note.MusicalNoteMax)} range and will be clamped. Manual correction will be required. {GetPatternString(c.PatternInstances[location.PatternIndex], location.NoteIndex)}");
                                    }

                                    newNote = newNoteValue;
                                }
                                else
                                {
                                    // Compute the pitch delta and find the closest target note.
                                    var newNotePitch = Utils.Clamp(noteTable[slideSource] + numFrames * (slideSpeed << octaveSlideShift), 0, pitchLimit);
                                    newNote = FindBestMatchingNote(noteTable, newNotePitch, Math.Sign(slideSpeed));
                                }

                                note.SlideNoteTarget = (byte)newNote;

                                if (nextLocation.IsInSong(s))
                                {
                                    // If the FX was turned off, we need to add an extra note.
                                    var nextPattern = c.PatternInstances[nextLocation.PatternIndex];
                                    if (!nextPattern.Notes.TryGetValue(nextLocation.NoteIndex, out var nextNote) || !nextNote.IsValid)
                                    {
                                        nextNote = nextPattern.GetOrCreateNoteAt(nextLocation.NoteIndex);
                                        nextNote.Instrument = note.Instrument;
                                        nextNote.Value = (byte)newNote;
                                        nextNote.HasAttack = false;
                                        it.Resync();
                                    }
                                    else if (nextNote != null && nextNote.IsRelease)
                                    {
                                        Log.LogMessage(LogSeverity.Warning, $"A slide note ends on a release note. This is currently unsupported and will require manual correction. {GetPatternString(nextPattern, nextLocation.NoteIndex)}");
                                    }
                                }

                                if (clearSlide)
                                    slideSpeed = 0;
                            }
                        }

                        if (note != null && (note.IsMusical || note.IsStop))
                        {
                            lastNoteValue = note.IsSlideNote ? note.SlideNoteTarget : note.Value;
                            lastNoteInstrument = note.Instrument;
                            lastNoteArpeggio = note.Arpeggio;
                        }
                    }

                    processedPatterns.Add(pattern);
                }
            }
        }

        private void CreateVolumeSlides(Song s, Dictionary<Pattern, RowFxData[,]> patternFxData)
        {
            var processedPatterns = new HashSet<Pattern>();

            // Convert slide notes + portamento to our format.
            foreach (var c in s.Channels)
            {
                if (!c.SupportsSlideNotes)
                    continue;

                var songSpeed = s.FamitrackerSpeed;
                var lastVolume = (float)Note.VolumeMax;
                var slideEndLocation = NoteLocation.Invalid;
                var slideSlope = 0.0f;

                for (int p = 0; p < s.Length; p++)
                {
                    var pattern = c.PatternInstances[p];

                    if (pattern == null)
                        continue;

                    var patternLen = s.GetPatternLength(p);
                    
                    for (var it = pattern.GetDenseNoteIterator(0, patternLen); !it.Done; it.Next())
                    {
                        var location = new NoteLocation(p, it.CurrentTime);
                        var note     = it.CurrentNote;

                        // Look for speed changes.
                        s.ApplySpeedEffectAt(location, ref songSpeed);

                        if (!patternFxData.ContainsKey(pattern) || processedPatterns.Contains(pattern))
                            continue;

                        if (slideEndLocation.IsValid && location < slideEndLocation)
                            continue;
                        slideEndLocation = NoteLocation.Invalid;

                        var fxData = patternFxData[pattern];
                        var hasVolumeSlide = false;

                        for (int i = 0; i < fxData.GetLength(1); i++)
                        {
                            var fx = fxData[location.NoteIndex, i];

                            if (fx.fx == Effect_VolumeSlide)
                            {
                                if (note == null)
                                {
                                    note = pattern.GetOrCreateNoteAt(location.NoteIndex);
                                    it.Resync();
                                }

                                if ((fx.param & 0x0f) != 0) // Slide down
                                    slideSlope = -fx.param / 8.0f;
                                else if ((fx.param & 0xf0) != 0) // Slide up
                                    slideSlope =  (fx.param >> 4) / 8.0f;
                                else
                                    slideSlope = 0.0f;

                                hasVolumeSlide = slideSlope != 0.0f;
                            }
                        }

                        if (note != null && note.HasVolume)
                            lastVolume = note.Volume;

                        // Create a volume slide if needed.
                        if (note != null && slideSlope != 0 &&
                            !(slideSlope < 0.0f && lastVolume <= 0.0f || slideSlope > 0.0f && lastVolume >= Note.VolumeMax) &&
                            FindNextEffect(c, location, out var nextLocation, patternFxData, NoteHasVolume, IsVolumeSlideEffect))
                        {
                            // See how many frames until the volume slide stops.
                            var numFrames = (int)Math.Round(s.CountFramesBetween(location, nextLocation, songSpeed, s.Project.PalMode));

                            // TODO: Here we consider if the start note has a delay, but ignore the end note. It might have one too.
                            numFrames = Math.Max(1, numFrames - (note.HasNoteDelay ? note.NoteDelay : 0));

                            var newVolume = Utils.Clamp(lastVolume + numFrames * slideSlope, 0, Note.VolumeMax);

                            // If the volume reaches zero or the max, it might have ended up earlier, let's figure that out.
                            if ((int)lastVolume != newVolume && (newVolume == 0 || newVolume == Note.VolumeMax))
                            {
                                numFrames = (int)Math.Round(Math.Abs((newVolume - lastVolume) / slideSlope));
                                nextLocation = location;
                                s.AdvanceNumberOfFrames(ref nextLocation, numFrames, note.HasNoteDelay ? -note.NoteDelay : 0, songSpeed, s.Project.PalMode);
                            }

                            if (!note.HasVolume)
                                note.Volume = (byte)lastVolume;
                            note.VolumeSlideTarget = (byte)newVolume;
                            lastVolume = newVolume;

                            if (nextLocation.IsInSong(s))
                            {
                                // If the FX was turned off, we need to add an extra note.
                                var nextPattern = c.PatternInstances[nextLocation.PatternIndex];
                                if (!nextPattern.Notes.TryGetValue(nextLocation.NoteIndex, out var nextNote))
                                {
                                    nextNote = nextPattern.GetOrCreateNoteAt(nextLocation.NoteIndex);
                                    nextNote.Volume = (byte)newVolume;
                                    it.Resync();
                                }
                                else if (!nextNote.HasVolume)
                                {
                                    nextNote.Volume = (byte)newVolume;
                                }
                            }
                            else
                            {
                                break;
                            }

                            slideEndLocation = nextLocation;
                        }
                    }

                    processedPatterns.Add(pattern);
                }
            }
        }

        protected void ApplyHaltEffect(Song s, Dictionary<Pattern, RowFxData[,]> patternFxData)
        {
            // Find the first Cxx effect and truncate the song.
            for (int p = 0; p < s.Length; p++)
            {
                for (int c = 0; c < s.Channels.Length; c++)
                {
                    var pattern = s.Channels[c].PatternInstances[p];
                    var patternLength = s.GetPatternLength(p);

                    if (patternFxData.TryGetValue(pattern, out var fxData))
                    {
                        for (int i = 0; i < fxData.GetLength(0) && i < patternLength; i++)
                        {
                            for (int j = 0; j < fxData.GetLength(1); j++)
                            {
                                var fx = fxData[i, j];

                                if (fx.fx == Effect_Halt)
                                {
                                    if (s.PatternHasCustomSettings(p))
                                        s.GetPatternCustomSettings(p).patternLength = i + 1;
                                    else
                                        s.SetPatternCustomSettings(p, i + 1, s.BeatLength);
                                    s.SetLength(p + 1);
                                    s.SetLoopPoint(-1);
                                    return;
                                }
                            }
                        }
                    }
                }
            }
        }

        protected void PrintAdditionalWarnings()
        {
            foreach (var s in project.Songs)
            {
                for (int p = 0; p < s.Length; p++)
                {
                    for (int c = 0; c < s.Channels.Length; c++)
                    {
                        var pattern = s.Channels[c].PatternInstances[p];
                        if (pattern != null)
                        {
                            for (var it = pattern.GetDenseNoteIterator(0, s.GetPatternLength(p)); !it.Done; it.Next())
                            {
                                if (it.CurrentNote != null && it.CurrentNote.HasNoteDelay && it.CurrentNote.HasSpeed)
                                {
                                    Log.LogMessage(LogSeverity.Warning, $"Speed changes (Fxx) cannot be combined with note delays (Gxx). Speed changes will always be applied immediately and will ignore the delay. {GetPatternString(pattern, it.CurrentTime)}");
                                }
                            }
                        }
                    }
                }
            }

            if (project.UsesVrc6Expansion)
            {
                Log.LogMessage(LogSeverity.Warning, $"VRC6 Saw volumes in FamiStudio is not affected by the duty cycle and is instead controlled by a 'Saw Master Volume' on this instrument. You will likely have to adjust this to get the correct volume.");
            }

            var mappedSamplesSize = project.GetTotalMappedSampleSize();
            if (mappedSamplesSize > Project.MaxMappedSampleSize)
            {
                Log.LogMessage(LogSeverity.Warning, $"Project uses {mappedSamplesSize} bytes of sample data. The limit is {Project.MaxMappedSampleSize} bytes. Some samples will not play correctly or at all.");
            }
        }

        public static void ConvertN163WaveIndexToRepeatEnvelope(Instrument inst, Envelope waveIndexEnv)
        {
            if (waveIndexEnv == null)
            {
                // When there is no wave index envelope, just truncate to 1 waveform.
                inst.N163WaveCount = 1;
                inst.Envelopes[EnvelopeType.WaveformRepeat].Values[0] = (sbyte)1;
            }
            else
            {
                // Looks for contiguous sequences in the wave index sequence.
                var repeats = new List<int>();
                var indices = new List<int>();
                var prevIdx = 0;
                var prevVal = waveIndexEnv.Values[0];
                var loopIdx = -1;
                var relIdx  = -1;

                for (int i = 1; i < waveIndexEnv.Length; i++)
                {
                    // Must break for loop and releases too.
                    if (waveIndexEnv.Values[i] != prevVal || i == waveIndexEnv.Loop || i == waveIndexEnv.Release)
                    {
                        repeats.Add(i - prevIdx);
                        indices.Add(prevVal);
                        prevVal = waveIndexEnv.Values[i];
                        prevIdx = i;

                        if (i == waveIndexEnv.Loop)
                            loopIdx = indices.Count;
                        if (i == waveIndexEnv.Release)
                            relIdx = indices.Count;
                    }
                }

                repeats.Add(waveIndexEnv.Length - prevIdx);
                indices.Add(waveIndexEnv.Values[waveIndexEnv.Length - 1]);

                // Build the equivalent waveform + repeat envelope.
                var wavEnv = inst.Envelopes[EnvelopeType.N163Waveform];
                var repEnv = inst.Envelopes[EnvelopeType.WaveformRepeat];
                var originalWaveforms = wavEnv.Values.Clone() as sbyte[];

                inst.N163WaveCount = (byte)repeats.Count;

                if (inst.N163WaveCount < repeats.Count)
                {
                    Log.LogMessage(LogSeverity.Warning, $"The total size of the N163 waveforms is larger than the maximum supported, truncating.");
                }

                for (int i = 0; i < inst.N163WaveCount; i++)
                {
                    repEnv.Values[i] = (sbyte)repeats[i];

                    var idx = Utils.Clamp(indices[i], 0, inst.N163WaveCount - 1);

                    if (idx != indices[i])
                        Log.LogMessage(LogSeverity.Warning, $"N163 wave sequence contained invalid wave indices, clamping.");

                    for (int j = 0; j < inst.N163WaveSize; j++)
                        wavEnv.Values[i * inst.N163WaveSize + j] = originalWaveforms[idx * inst.N163WaveSize + j];
                }

                var waveEnv = inst.Envelopes[EnvelopeType.N163Waveform];

                repEnv.Loop    = loopIdx;
                repEnv.Release = relIdx;

                waveEnv.Loop    = repEnv.Loop    >= 0 ? repEnv.Loop    * inst.N163WaveSize : -1;
                waveEnv.Release = repEnv.Release >= 0 ? repEnv.Release * inst.N163WaveSize : -1;
            }
        }

        protected bool FinishImport()
        {
            foreach (var s in project.Samples)
            {
                s.Process();
            }

            foreach (var inst in project.Instruments)
            {
                for (int i = 0; i < EnvelopeType.Count; i++)
                {
                    var env = inst.Envelopes[i];
                    if (env != null && !env.ValuesInValidRange(inst, i))
                    {
                        Log.LogMessage(LogSeverity.Warning, $"Envelope '{EnvelopeType.LocalizedNames[i]}' of instrument '{inst.Name}' have values outside of the supported range, clamping.");
                        env.ClampToValidRange(inst, i);
                    }
                }

                if (inst.IsN163)
                {
                    if (!n163WaveEnvs.TryGetValue(inst, out var waveIndexEnvIdx))
                        waveIndexEnvIdx = -1;

                    var waveIndexEnv = GetFamiTrackerEnvelope(ExpansionType.N163, 4 /* SEQ_DUTYCYCLE */, waveIndexEnvIdx);

                    ConvertN163WaveIndexToRepeatEnvelope(inst, waveIndexEnv);
                }
            }

            foreach (var s in project.Songs)
            {
                foreach (var c in s.Channels)
                {
                    c.ColorizePatterns();

                    for (int p = 0; p < s.Length; p++)
                    {
                        var pattern = c.PatternInstances[p];
                        if (pattern != null && patternLengths.TryGetValue(pattern, out var instLength))
                        {
                            if (instLength < s.GetPatternLength(p))
                                s.SetPatternCustomSettings(p, instLength, s.BeatLength);
                        }
                    }
                }

                // FamiTracker always assumes 4 rows per beat for BPM calculation, but let's assume
                // the artists properly set first row highlight to that.
                if (barLength == -1)
                    s.SetSensibleBeatLength();
                else
                    s.SetBeatLength(barLength);

                ApplyHaltEffect(s, patternFxData);

                // See if we should truncate the sound because of jumps.
                if (songDurations.TryGetValue(s, out var duration) && duration < s.Length)
                {
                    s.SetLength(duration);
                    Log.LogMessage(LogSeverity.Warning, $"Patterns at end of song '{s.Name}' are unreachable due to a previous jump (Bxx), truncating.");
                }

                s.DeleteNotesPastMaxInstanceLength();
                s.UpdatePatternStartNotes();

                AssignNullInstruments(s);
                CreateArpeggios(s, patternFxData);
                CreateVolumeSlides(s, patternFxData);
                CreateSlideNotes(s, patternFxData);

                s.DeleteEmptyPatterns();
                s.RemoveUnsupportedFeatures(); // Extra security.
            }

            project.AutoSortSongs = false;
            project.ConvertToCompoundNotes();
            project.InvalidateCumulativePatternCache();
            project.ConditionalSortEverything();
            project.ValidateIntegrity();

            PrintAdditionalWarnings();

            return true;
        }
    };
}
