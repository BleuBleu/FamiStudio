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
            Channel.Square1,        // CHANID_SQUARE1
            Channel.Square2,        // CHANID_SQUARE2
            Channel.Triangle,       // CHANID_TRIANGLE
            Channel.Noise,          // CHANID_NOISE
            Channel.Dpcm,           // CHANID_DPCM
            Channel.Vrc6Square1,    // CHANID_VRC6_PULSE1
            Channel.Vrc6Square2,    // CHANID_VRC6_PULSE2
            Channel.Vrc6Saw,        // CHANID_VRC6_SAWTOOTH
            Channel.Mmc5Square1,    // CHANID_MMC5_SQUARE1
            Channel.Mmc5Square2,    // CHANID_MMC5_SQUARE2
            Channel.Mmc5Dpcm,       // CHANID_MMC5_VOICE
            Channel.N163Wave1,      // CHANID_N163_CHAN1
            Channel.N163Wave2,      // CHANID_N163_CHAN2
            Channel.N163Wave3,      // CHANID_N163_CHAN3
            Channel.N163Wave4,      // CHANID_N163_CHAN4
            Channel.N163Wave5,      // CHANID_N163_CHAN5
            Channel.N163Wave6,      // CHANID_N163_CHAN6
            Channel.N163Wave7,      // CHANID_N163_CHAN7
            Channel.N163Wave8,      // CHANID_N163_CHAN8
            Channel.FdsWave,        // CHANID_FDS
            Channel.Vrc7Fm1,        // CHANID_VRC7_CH1
            Channel.Vrc7Fm2,        // CHANID_VRC7_CH2
            Channel.Vrc7Fm3,        // CHANID_VRC7_CH3
            Channel.Vrc7Fm4,        // CHANID_VRC7_CH4
            Channel.Vrc7Fm5,        // CHANID_VRC7_CH5
            Channel.Vrc7Fm6,        // CHANID_VRC7_CH6
            Channel.S5BSquare1,     // CHANID_S5B_CH1
            Channel.S5BSquare2,     // CHANID_S5B_CH2
            Channel.S5BSquare3      // CHANID_S5B_CH3
        };

        protected static int[] InstrumentTypeLookup =
        {
            Project.ExpansionCount,  // INST_NONE: Should never happen.
            Project.ExpansionNone,   // INST_2A03
            Project.ExpansionVrc6,   // INST_VRC6
            Project.ExpansionVrc7,   // INST_VRC7
            Project.ExpansionFds,    // INST_FDS
            Project.ExpansionN163,   // INST_N163
            Project.ExpansionS5B     // INST_S5B
        };

        // FamiTracker -> FamiStudio
        protected static int[] EnvelopeTypeLookup =
        {
            Envelope.Volume,   // SEQ_VOLUME
            Envelope.Arpeggio, // SEQ_ARPEGGIO
            Envelope.Pitch,    // SEQ_PITCH
            Envelope.Count,    // SEQ_HIPITCH
            Envelope.DutyCycle // SEQ_DUTYCYCLE
        };

        // FamiStudio -> FamiTracker
        protected static int[] ReverseEnvelopeTypeLookup =
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

        protected Project project;
        protected Dictionary<Pattern, RowFxData[,]> patternFxData = new Dictionary<Pattern, RowFxData[,]>();
        protected Dictionary<Pattern, byte> patternLengths = new Dictionary<Pattern, byte>();
        protected int barLength = -1;

        protected int ConvertExpansionAudio(int exp)
        {
            switch (exp)
            {
                case SndChip_NONE : return Project.ExpansionNone;
                case SndChip_VRC6 : return Project.ExpansionVrc6;
                case SndChip_VRC7 : return Project.ExpansionVrc7;
                case SndChip_FDS  : return Project.ExpansionFds;
                case SndChip_MMC5 : return Project.ExpansionMmc5;
                case SndChip_N163 : return Project.ExpansionN163;
                case SndChip_S5B  : return Project.ExpansionS5B;
            }

            Log.LogMessage(LogSeverity.Error, "Unsupported audio expansion.");
            return -1; // We dont support exotic combinations.
        }

        protected Instrument CreateUniquelyNamedInstrument(int type, string baseName)
        {
            string name = baseName;
            var j = 2;

            while (!project.IsInstrumentNameUnique(name))
                name = baseName + "-" + j++;

            return project.CreateInstrument(type, name);
        }

        protected void RenameInstrumentEnsureUnique(Instrument instrument, string baseName)
        {
            string name = baseName;
            var j = 2;

            while (!project.IsInstrumentNameUnique(name))
                name = baseName + "-" + j++;

            project.RenameInstrument(instrument, name);
        }

        protected DPCMSample CreateUniquelyNamedSample(string baseName, byte[] data)
        {
            string name = baseName;
            var j = 2;

            while (!project.IsDPCMSampleNameUnique(name))
                name = baseName + "-" + j++;

            return project.CreateDPCMSample(name, data);
        }

        protected Song CreateUniquelyNamedSong(string baseName)
        {
            string name = baseName;
            var j = 2;

            while (!project.IsSongNameUnique(name))
                name = baseName + "-" + j++;

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

        protected void ApplySimpleEffects(RowFxData fx, Pattern pattern, int n, Dictionary<Pattern, byte> patternLengths, bool allowSongEffects)
        {
            Note note = null;

            switch (fx.fx)
            {
                case Effect_None:
                    return;
                case Effect_Jump:
                    if (allowSongEffects)
                        pattern.Song.SetLoopPoint(fx.param);
                    return;
                case Effect_Skip:
                    patternLengths[pattern] = (byte)(n + 1);
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
                        pattern.GetOrCreateNoteAt(n).DutyCycle = fx.param;
                    }
                    return;
                case Effect_Delay:
                    if (pattern.Channel.SupportsEffect(Note.EffectNoteDelay))
                    {
                        pattern.GetOrCreateNoteAt(n).NoteDelay = Math.Min((byte)31, fx.param);
                    }
                    return;
                case Effect_NoteCut:
                    if (pattern.Channel.SupportsEffect(Note.EffectCutDelay))
                    {
                        pattern.GetOrCreateNoteAt(n).CutDelay = Math.Min((byte)31, fx.param);
                    }
                    return;
                case Effect_Halt:
                case Effect_PortaUp:
                case Effect_PortaDown:
                case Effect_Portamento:
                case Effect_SlideUp:
                case Effect_SlideDown:
                case Effect_Arpeggio:
                    // These will be applied later.
                    return;
            }

            if (EffectToTextLookup.ContainsKey(fx.fx))
                Log.LogMessage(LogSeverity.Warning, $"Effect '{EffectToTextLookup[fx.fx]}' is not supported and will be ignored. {GetPatternString(pattern, n)}");
            else
                Log.LogMessage(LogSeverity.Warning, $"Unknown effect code ({fx.fx}) and will be ignored. {GetPatternString(pattern, n)}");
        }

        private string GetPatternString(Pattern pattern, int n)
        {
            return $"(Song={pattern.Song.Name}, Channel={Channel.ChannelNames[pattern.ChannelType]}, Pattern={pattern.Name}, Row={n:X2})";
        }

        private int FindPrevNoteForPortamento(Channel channel, int patternIdx, int noteIdx, Dictionary<Pattern, RowFxData[,]> patternFxData)
        {
            var pattern = channel.PatternInstances[patternIdx];

            for (var it = pattern.GetNoteIterator(0, noteIdx, true); !it.Done; it.Next())
            {
                var note = it.CurrentNote;
                if (note.IsMusical || note.IsStop)
                    return note.Value;
            }

            for (var p = patternIdx - 1; p >= 0; p--)
            {
                pattern = channel.PatternInstances[p];
                if (pattern != null)
                {
                    for (var it = pattern.GetNoteIterator(0, channel.Song.GetPatternLength(p), true); !it.Done; it.Next())
                    {
                        var note = it.CurrentNote;
                        if (note.IsMusical || note.IsStop)
                            return note.Value;
                    }
                }
            }

            return Note.NoteInvalid;
        }

        private bool FindNextSlideEffect(Channel channel, int patternIdx, int noteIdx, out int nextPatternIdx, out int nextNoteIdx, Dictionary<Pattern, RowFxData[,]> patternFxData)
        {
            nextPatternIdx = -1;
            nextNoteIdx    = -1;

            var pattern = channel.PatternInstances[patternIdx];

            if (pattern == null || !patternFxData.ContainsKey(pattern))
                return false;

            var patternLen = channel.Song.GetPatternLength(patternIdx);
            var fxData     = patternFxData[pattern];

            for (var it = pattern.GetNoteIterator(noteIdx + 1, patternLen); !it.Done; it.Next())
            {
                var time = it.CurrentTime;
                var note = it.CurrentNote;

                var fxChanged = false;
                for (int i = 0; i < fxData.GetLength(1); i++)
                {
                    var fx = fxData[time, i];

                    if (fx.fx == Effect_PortaUp    || 
                        fx.fx == Effect_PortaDown  ||
                        fx.fx == Effect_Portamento ||
                        fx.fx == Effect_SlideUp    ||
                        fx.fx == Effect_SlideDown)
                    {
                        fxChanged = true;
                        break;
                    }
                }

                if (note != null && note.IsValid || fxChanged)
                {
                    nextPatternIdx = patternIdx;
                    nextNoteIdx = time;
                    return true;
                }
            }

            for (int p = patternIdx + 1; p < channel.Song.Length; p++)
            {
                pattern    = channel.PatternInstances[p];
                patternLen = channel.Song.GetPatternLength(p);

                if (pattern != null && patternFxData.ContainsKey(pattern))
                {
                    fxData = patternFxData[pattern];

                    for (var it = pattern.GetNoteIterator(0, patternLen); !it.Done; it.Next())
                    {
                        var time = it.CurrentTime;
                        var note = it.CurrentNote;

                        var fxChanged = false;
                        for (int i = 0; i < fxData.GetLength(1); i++)
                        {
                            var fx = fxData[time, i];

                            if (fx.fx == Effect_PortaUp    || 
                                fx.fx == Effect_PortaDown  || 
                                fx.fx == Effect_Portamento || 
                                fx.fx == Effect_SlideUp    ||
                                fx.fx == Effect_SlideDown)
                            {
                                fxChanged = true;
                                break;
                            }
                        }

                        if (note != null && note.IsValid || fxChanged)
                        {
                            nextPatternIdx = p;
                            nextNoteIdx = time;
                            return true;
                        }
                    }
                }
            }

            return false;
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

        private void CreateArpeggios(Song s, Dictionary<Pattern, RowFxData[,]> patternFxData)
        {
            var processedPatterns = new HashSet<Pattern>();

            foreach (var c in s.Channels)
            {
                if (!c.SupportsArpeggios)
                    continue;

                var lastNoteInstrument = (Instrument)null;
                var lastNoteArpeggio = (Arpeggio)null;
                var lastNoteValue = (byte)Note.NoteInvalid;

                for (int p = 0; p < s.Length; p++)
                {
                    var pattern = c.PatternInstances[p];

                    if (pattern == null || !patternFxData.ContainsKey(pattern) || processedPatterns.Contains(pattern))
                        continue;

                    processedPatterns.Add(pattern);

                    var fxData = patternFxData[pattern];
                    var patternLen = s.GetPatternLength(p);

                    for (var it = pattern.GetNoteIterator(0, patternLen); !it.Done; it.Next())
                    {
                        var n    = it.CurrentTime;
                        var note = it.CurrentNote;

                        for (int i = 0; i < fxData.GetLength(1); i++)
                        {
                            var fx = fxData[n, i];

                            if (fx.fx == Effect_Arpeggio)
                            {
                                if (note == null)
                                {
                                    note = pattern.GetOrCreateNoteAt(n);
                                    note.Value = lastNoteValue;
                                    note.Instrument = lastNoteInstrument;
                                    note.HasAttack = false;
                                    it.Resync();
                                }

                                note.Arpeggio = GetOrCreateArpeggio(fx.param);
                            }
                        }

                        if (note != null && note.IsValid)
                        {
                            lastNoteValue      = note.Value;
                            lastNoteInstrument = note.Instrument;
                            lastNoteArpeggio   = note.Arpeggio;
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
                if (!c.SupportsSlideNotes)
                    continue;

                var songSpeed = s.FamitrackerSpeed;
                var lastNoteInstrument = (Instrument)null;
                var lastNoteArpeggio = (Arpeggio)null;
                var lastNoteValue = (byte)Note.NoteInvalid;
                var portamentoSpeed = 0;
                var slideSpeed = 0;
                var slideShift = c.IsN163WaveChannel ? 2 : 0;
                var slideSign  = c.IsN163WaveChannel || c.IsFdsWaveChannel ? -1 : 1; // Inverted channels.

                for (int p = 0; p < s.Length; p++)
                {
                    var pattern = c.PatternInstances[p];

                    if (pattern == null)
                        continue;

                    var patternLen = s.GetPatternLength(p);

                    for (var it = pattern.GetNoteIterator(0, patternLen); !it.Done; it.Next())
                    {
                        var n    = it.CurrentTime;
                        var note = it.CurrentNote;

                        // Look for speed changes.
                        s.ApplySpeedEffectAt(p, n, ref songSpeed);

                        if (!patternFxData.ContainsKey(pattern) || processedPatterns.Contains(pattern))
                            continue;

                        var fxData = patternFxData[pattern];
                        var slideTarget = 0;

                        for (int i = 0; i < fxData.GetLength(1); i++)
                        {
                            var fx = fxData[n, i];

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
                                        note = pattern.GetOrCreateNoteAt(n);
                                        it.Resync();
                                    }

                                    note.Value      = lastNoteValue;
                                    note.Instrument = lastNoteInstrument;
                                    note.Arpeggio   = lastNoteArpeggio;
                                    note.HasAttack  = false;
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
                                    slideSpeed = ( fx.param * slideSign) << slideShift;
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
                        if (note != null && !note.IsSlideNote)
                        {
                            if (note.IsMusical)
                            {
                                var slideSource = note.Value;
                                var noteTable   = NesApu.GetNoteTableForChannelType(c.Type, s.Project.PalMode, s.Project.ExpansionNumChannels);
                                var pitchLimit  = NesApu.GetPitchLimitForChannelType(c.Type);

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
                                    }
                                }

                                // Our implementation of VRC7 pitches is quite different from FamiTracker.
                                // Compensate for larger pitches in higher octaves by shifting. We cant shift by 
                                // a large amount because the period is 9-bit and FamiTracker is restricted to 
                                // this for slides (octave never changes).
                                var octaveSlideShift = c.IsVrc7FmChannel && note.Value >= 12 ? 1 : 0;

                                // 3xx/Qxy/Rxy : We know which note we are sliding to and the speed, but we 
                                //               don't know how many frames it will take to get there.
                                if (slideTarget != 0)
                                {
                                    // Advance in the song until we have the correct number of frames.
                                    var numFrames = Math.Max(1, Math.Abs((noteTable[slideSource] - noteTable[slideTarget]) / (slideSpeed << octaveSlideShift)));
                                    note.SlideNoteTarget = (byte)slideTarget;

                                    // TODO: Here we consider if the start note has a delay, but ignore the end note. It might have one too.
                                    var np = p;
                                    var nn = n;
                                    s.AdvanceNumberOfFrames(numFrames, note.HasNoteDelay ? -note.NoteDelay : 0, songSpeed, s.Project.PalMode, ref np, ref nn);

                                    // Still to see if there is a note between the current one and the 
                                    // next note, this could append if you add a note before the slide 
                                    // is supposed to finish.
                                    if (FindNextSlideEffect(c, p, n, out var np2, out var nn2, patternFxData))
                                    {
                                        if (np2 < np)
                                        {
                                            np = np2;
                                            nn = nn2;
                                        }
                                        else if (np2 == np)
                                        {
                                            nn = Math.Min(nn, nn2);
                                        }

                                        // If the slide is interrupted by another slide effect, we will not reach 
                                        // the final target, but rather some intermediate note. Let's do our best
                                        // to interpolate and figure out the best note.
                                        var numFramesUntilNextSlide = s.CountFramesBetween(p, n, np, nn, songSpeed, s.Project.PalMode);
                                        var ratio = Utils.Clamp(numFramesUntilNextSlide / numFrames, 0.0f, 1.0f);
                                        var intermediatePitch = (int)Math.Round(Utils.Lerp(noteTable[slideSource], noteTable[slideTarget], ratio));

                                        slideTarget = FindBestMatchingNote(noteTable, intermediatePitch, Math.Sign(slideSpeed));
                                        note.SlideNoteTarget = (byte)slideTarget;
                                    }

                                    if (np < s.Length)
                                    {
                                        // Add an extra note with no attack to stop the slide.
                                        var nextPattern = c.PatternInstances[np];
                                        if (!nextPattern.Notes.TryGetValue(nn, out var nextNote) || !nextNote.IsValid)
                                        {
                                            nextNote = nextPattern.GetOrCreateNoteAt(nn);
                                            nextNote.Instrument = note.Instrument;
                                            nextNote.Value = (byte)slideTarget;
                                            nextNote.HasAttack = false;
                                            it.Resync();
                                        }
                                        else if (nextNote != null && nextNote.IsRelease)
                                        {
                                            Log.LogMessage(LogSeverity.Warning, $"A slide note ends on a release note. This is currently unsupported and will require manual correction. {GetPatternString(nextPattern, nn)}");
                                        }
                                    }

                                    // 3xx, Qxx and Rxx stops when its done.
                                    slideSpeed = 0;
                                }

                                // 1xx/2xy : We know the speed at which we are sliding, but need to figure out what makes it stop.
                                else if (slideSpeed != 0 && FindNextSlideEffect(c, p, n, out var np, out var nn, patternFxData))
                                {
                                    // See how many frames until the slide stops.
                                    var numFrames = (int)Math.Round(s.CountFramesBetween(p, n, np, nn, songSpeed, s.Project.PalMode));

                                    // TODO: Here we consider if the start note has a delay, but ignore the end note. It might have one too.
                                    numFrames = Math.Max(1, numFrames - (note.HasNoteDelay ? note.NoteDelay : 0));

                                    // Compute the pitch delta and find the closest target note.
                                    var newNotePitch = Utils.Clamp(noteTable[slideSource] + numFrames * (slideSpeed << octaveSlideShift), 0, pitchLimit);
                                    var newNote = FindBestMatchingNote(noteTable, newNotePitch, Math.Sign(slideSpeed));

                                    note.SlideNoteTarget = (byte)newNote;

                                    // If the FX was turned off, we need to add an extra note.
                                    var nextPattern = c.PatternInstances[np];
                                    if (!nextPattern.Notes.TryGetValue(nn, out var nextNote) || !nextNote.IsValid)
                                    {
                                        nextNote = nextPattern.GetOrCreateNoteAt(nn);
                                        nextNote.Instrument = note.Instrument;
                                        nextNote.Value = (byte)newNote;
                                        nextNote.HasAttack = false;
                                        it.Resync();
                                    }
                                    else if (nextNote != null && nextNote.IsRelease)
                                    {
                                        Log.LogMessage(LogSeverity.Warning, $"A slide note ends on a release note. This is currently unsupported and will require manual correction. {GetPatternString(nextPattern, nn)}");
                                    }
                                }
                            }
                        }

                        if (note != null && (note.IsMusical || note.IsStop))
                        {
                            lastNoteValue      = note.IsSlideNote ? note.SlideNoteTarget : note.Value;
                            lastNoteInstrument = note.Instrument;
                            lastNoteArpeggio   = note.Arpeggio;
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
                            for (var it = pattern.GetNoteIterator(0, s.GetPatternLength(p)); !it.Done; it.Next())
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

            if (project.ExpansionAudio == Project.ExpansionVrc6)
            {
                Log.LogMessage(LogSeverity.Warning, $"VRC6 Saw volumes in FamiStudio uses the full volume range and ignores the duty cycle, they will need to the adjusted manually to sound the same. In most cases, this mean reducing the volume by half using either the volume track or volume envelopes.");
            }
        }

        protected bool FinishImport()
        {
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

                s.DeleteNotesPastMaxInstanceLength();
                s.UpdatePatternStartNotes();

                // FamiTracker always assumes 4 rows per beat for BPM calculation, but let's assume
                // the artists properly set first row highlight to that.
                if (barLength == -1)
                    s.SetSensibleBeatLength();
                else
                    s.SetBeatLength(barLength);

                ApplyHaltEffect(s, patternFxData);
                CreateArpeggios(s, patternFxData);
                CreateSlideNotes(s, patternFxData);

                s.DeleteEmptyPatterns();
            }

            project.UpdateAllLastValidNotesAndVolume();
            project.Validate();

            PrintAdditionalWarnings();

            return true;
        }
    };
}
