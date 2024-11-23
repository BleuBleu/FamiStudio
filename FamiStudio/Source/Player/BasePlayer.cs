using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace FamiStudio
{
    public enum LoopMode
    {
        LoopPoint,
        Song,
        Pattern,
        None,
        Max
    };

    public interface IPlayerInterface
    {
        void NotifyInstrumentLoaded(Instrument instrument, long channelTypeMask);
        void NotifyRegisterWrite(int apuIndex, int reg, int data, int metadata = 0);
        int GetN163AutoWavePosition(Instrument instrument);
        ChannelState GetChannelByType(int type); // Use sparingly, kind of hacky.
    }

    public class BasePlayer : IPlayerInterface
    {
        protected int apuIndex;
        protected NesApu.DmcReadDelegate dmcCallback;
        protected int sampleRate;
        protected int loopCount = 0;
        protected int maxLoopCount = -1;
        protected int frameNumber = 0;
        protected int playbackRate = 1; // 1 = normal, 2 = 1/2, 4 = 1/4, etc.
        protected int playbackRateCounter = 1;
        protected int minSelectedPattern = -1;
        protected int maxSelectedPattern = -1;
        protected int numPlayedPatterns = 0;
        protected bool famitrackerTempo = true;
        protected bool palPlayback = false;
        protected bool seeking = false;
        protected bool beat = false;
        protected bool stereo = false;
        protected bool accurateSeek = false;
        protected bool forceReadRegisterValues = false;
        protected volatile bool reachedEnd = false;
        protected int  tndMode = NesApu.TND_MODE_SINGLE;
        protected int  beatIndex = -1;
        protected Dictionary<int, int> n163AutoWavPosMap;
        protected Song song;
        protected ChannelState[] channelStates;
        protected LoopMode loopMode = LoopMode.Song;
        protected long channelMask = -1;
        protected volatile int playPosition = 0;
        protected NoteLocation playLocation = new NoteLocation(0, 0);
        protected NesApu.NesRegisterValues registerValues = new NesApu.NesRegisterValues();

        // Only used by FamiTracker tempo.
        protected int famitrackerTempoCounter = 0;
        protected int famitrackerSpeed = 6;

        // Only used by FamiStudio tempo.
        protected GrooveIterator grooveIterator;

        // Only used by FamiStudio tempo when doing adapted playback (NTSC -> PAL or vice-versa).
        protected byte[] tempoEnvelope;
        protected int tempoEnvelopeIndex;
        protected int tempoEnvelopeCounter;

        protected BasePlayer(int apu, bool pal, bool inStereo, int rate = 44100)
        {
            apuIndex = apu;
            sampleRate = rate;
            dmcCallback = new NesApu.DmcReadDelegate(NesApu.DmcReadCallback);
            stereo = inStereo;
            palPlayback = pal;
            registerValues.SetPalMode(pal);
        }

        public virtual void Shutdown()
        {
        }

        public long ChannelMask
        {
            get { return Thread.VolatileRead(ref channelMask); }
            set { Thread.VolatileWrite(ref channelMask, value); }
        }

        public LoopMode Loop
        {
            get { return loopMode; }
            set { loopMode = value; }
        }

        public bool AccurateSeek
        {
            get { return accurateSeek; }
            set { accurateSeek = value; }
        }

        public virtual int PlayPosition
        {
            get { return Math.Max(0, playPosition); }
            set { playPosition = value; }
        }

        public int PlayRate
        {
            get { return playbackRate; }
            set
            {
                Debug.Assert(value == 1 || value == 2 || value == 4);
                playbackRate = value;
            }
        }
        
        public void SetSelectionRange(int min, int max)
        {
            minSelectedPattern = min;
            maxSelectedPattern = max;
        }

        // Returns the number of frames to run (0, 1 or 2)
        protected int UpdateFamiStudioTempo()
        {
            if (!playLocation.IsValid || famitrackerTempo || song.Project.PalMode == palPlayback)
            {
                return 1;
            }
            else
            {
                if (--tempoEnvelopeCounter <= 0)
                {
                    tempoEnvelopeIndex++;

                    if (tempoEnvelope[tempoEnvelopeIndex] == 0x80)
                        tempoEnvelopeIndex = 1;

                    tempoEnvelopeCounter = tempoEnvelope[tempoEnvelopeIndex];

#if FALSE //DEBUG
                    if (song.Project.PalMode)
                        Debug.WriteLine("*** Will do nothing for 1 frame!");
                    else
                        Debug.WriteLine("*** Will run 2 frames!"); 
#endif

                    // A NTSC song playing on PAL will sometimes need to run 2 frames to keep up.
                    // A PAL song playing on NTSC will sometimes need to do nothing for 1 frame to avoid going too fast.
                    return palPlayback ? 2 : 0;
                }
                else
                {
                    return 1;
                }
            }
        }

        private void ResetFamiStudioTempo()
        {
            if (!famitrackerTempo)
            {
                var newGroove = song.GetPatternGroove(playLocation.PatternIndex);
                var newGroovePadMode = song.GetPatternGroovePaddingMode(playLocation.PatternIndex);

                FamiStudioTempoUtils.ValidateGroove(newGroove);

                grooveIterator = new GrooveIterator(newGroove, newGroovePadMode);

                tempoEnvelope = FamiStudioTempoUtils.GetTempoEnvelope(newGroove, newGroovePadMode, song.Project.PalMode);
                tempoEnvelopeCounter = tempoEnvelope[0];
                tempoEnvelopeIndex = 0;
            }
        }

        protected bool ShouldAdvanceSong()
        {
            if (!playLocation.IsValid) // Here we are really "before the first frame"
            {
                return true;
            }
            else
            {
                if (famitrackerTempo)
                {
                    return famitrackerTempoCounter <= 0;
                }
                else
                {
                    return !grooveIterator.IsPadFrame;
                }
            }
        }

        protected void UpdateTempo()
        {
            if (famitrackerTempo)
            {
                // Tempo/speed logic straight from Famitracker.
                var tempoDecrement = (song.FamitrackerTempo * 24) / famitrackerSpeed;
                var tempoRemainder = (song.FamitrackerTempo * 24) % famitrackerSpeed;

                if (famitrackerTempoCounter <= 0)
                {
                    int ticksPerSec = palPlayback ? 50 : 60;
                    famitrackerTempoCounter += (60 * ticksPerSec) - tempoRemainder;
                }

                famitrackerTempoCounter -= tempoDecrement;
            }
            else
            {
                grooveIterator.Advance();
            }
        }

        protected void AdvanceChannels()
        {
            foreach (var channel in channelStates)
            {
                channel.Advance(song, playLocation, ref famitrackerSpeed);
            }
        }

        protected void UpdateChannels()
        {
            // Main channel update, advance song, update APU, etc.
            foreach (var channel in channelStates)
            {
                channel.Update();
            }

            // This will do the phase resets (if any) this attempts to mimic the sound engine a bit more closely.
            foreach (var channel in channelStates)
            {
                channel.PostUpdate();
            }
        }

        protected void EnableChannelType(int channelType, bool enable)
        {
            var exp = ChannelType.GetExpansionTypeForChannelType(channelType);
            var idx = ChannelType.GetExpansionChannelIndexForChannelType(channelType);

            NesApu.EnableChannel(apuIndex, exp, idx, enable ? 1 : 0);
        }

        protected void InitAndResetApu(Project project)
        {
            var expMixerSettings = new ExpansionMixer[ExpansionType.Count];

            // Apply project overrides.
            for (int i = 0; i < expMixerSettings.Length; i++)
            {
                expMixerSettings[i] = project.AllowMixerOverride && project.ExpansionMixerSettings[i].Override ?
                    project.ExpansionMixerSettings[i] :
                    Settings.ExpansionMixerSettings[i];
            }

            NesApu.InitAndReset(
                apuIndex, 
                sampleRate, 
                Settings.GlobalVolumeDb,
                project.OverrideBassCutoffHz ? project.BassCutoffHz : Settings.BassCutoffHz, 
                expMixerSettings,
                palPlayback, 
                Settings.N163Mix,
                tndMode, 
                project.ExpansionAudioMask, 
                project.ExpansionNumN163Channels, 
                dmcCallback);
        }

        protected void UpdateChannelsMuting()
        {
            for (int i = 0; i < channelStates.Length; i++)
            {
                var state = channelStates[i];

                EnableChannelType(state.InnerChannelType, (channelMask & (1L << i)) != 0);
            }
        }

        public void BeginPlaySong(Song s)
        {
            Debug.Assert(s.Project.OutputsStereoAudio == stereo);

            song = s;
            song.Project.AutoAssignN163WavePositions(out n163AutoWavPosMap);
            famitrackerTempo = song.UsesFamiTrackerTempo;
            famitrackerSpeed = song.FamitrackerSpeed;
            playLocation = NoteLocation.Invalid; // This means "before the first frame".
            frameNumber = 0;
            famitrackerTempoCounter = 0;
            channelStates = CreateChannelStates(song.Project, apuIndex, song.Project.Tuning, palPlayback, song.Project.ExpansionNumN163Channels);
            reachedEnd = false;
            playbackRateCounter = 1;
            tempoEnvelopeCounter = 0;

            InitAndResetApu(song.Project);
            UpdateChannelsMuting();
        }

        public bool SeekTo(int startNote)
        {
            Debug.Assert(startNote > 0);

            StartSeeking();

            var stillSeeking = true;

            while (true)
            {
                // We 1 frame before the target so that we "advance" to the correct frame on the first PlaySongFrame().
                var reachedStartNote = playLocation.IsValid && playLocation.ToAbsoluteNoteIndex(song) >= startNote - 1;
                if (reachedStartNote || !PlaySongFrameInternal(true))
                {
                    stillSeeking = false;
                    break;
                }

                // In accurate seek mode, this can be slow, so the caller must call in a loop until we return false.
                if (accurateSeek)
                {
                    EndSeekFrame();
                    break;
                }
            }

            StopSeeking();

            return stillSeeking;
        }

        protected bool PlaybackRateShouldSkipFrame(bool seeking)
        {
            if (seeking)
                return false;

            if (--playbackRateCounter > 0)
                return true;

            Debug.Assert(playbackRateCounter == 0);
            playbackRateCounter = playbackRate;
            return false;
        }

        protected bool PlaySongFrameInternal(bool seeking)
        {
            ClearBeat();

            //Debug.WriteLine($"PlaySongFrameInternal {playPosition}!");
            //Debug.WriteLine($"PlaySongFrameInternal {song.GetPatternStartNote(playPattern) + playNote}!");

            if (PlaybackRateShouldSkipFrame(seeking))
                return true;

            int numFramesToRun = UpdateFamiStudioTempo();

            for (int i = 0; i < numFramesToRun; i++)
            {
                if (ShouldAdvanceSong())
                {
                    //Debug.WriteLine($"  Seeking Frame {song.GetPatternStartNote(playPattern) + playNote}!");

                    if (!AdvanceSong(song.Length, seeking ? LoopMode.None : loopMode))
                    {
                        reachedEnd = true;
                        return false;
                    }

                    AdvanceChannels();
                }

                UpdateChannels();
                UpdateTempo();

#if DEBUG
                if (i > 0)
                {
                    var noteLength = song.GetPatternNoteLength(playLocation.PatternIndex);
                    if ((playLocation.NoteIndex % noteLength) == 0 && noteLength != 1)
                        Debug.WriteLine("*********** INVALID SKIPPED NOTE!");
                }
#endif
            }

            if (!seeking)
                playPosition = playLocation.ToAbsoluteNoteIndex(song);

            frameNumber++;

            return true;
        }

        public bool PlaySongFrame()
        {
            BeginFrame();

            if (!PlaySongFrameInternal(false))
                return false;

            UpdateChannelsMuting();
            EndFrame();

            return true;
        }

        protected void StartSeeking()
        {
            seeking = true;
            if (!accurateSeek)
                NesApu.StartSeeking(apuIndex);
        }

        protected void StopSeeking()
        {
            if (!accurateSeek)
                NesApu.StopSeeking(apuIndex);
            seeking = false;
        }

        protected bool AdvanceSong(int songLength, LoopMode loopMode)
        {
            bool advancedPattern = false;

            // Pretend we "advance" to the first note on the first frame.
            if (!playLocation.IsValid)
            {
                playLocation = new NoteLocation(0, 0);
                advancedPattern = true;
            }
            else
            {
                if (++playLocation.NoteIndex >= song.GetPatternLength(playLocation.PatternIndex))
                {
                    playLocation.NoteIndex = 0;

                    if (loopMode != LoopMode.Pattern)
                    {
                        playLocation.PatternIndex++;
                        advancedPattern = true;
                    }
                    else
                    {
                        // Make sure the selection is valid, updated on another thread, so could be sketchy.
                        var minPatternIdx = minSelectedPattern;
                        var maxPatternIdx = maxSelectedPattern;

                        if (minPatternIdx >= 0 &&
                            maxPatternIdx >= 0 &&
                            maxPatternIdx >= minPatternIdx &&
                            minPatternIdx < song.Length)
                        {
                            if (playLocation.PatternIndex + 1 > maxPatternIdx)
                            {
                                playLocation.PatternIndex = minPatternIdx;
                            }
                            else
                            {
                                playLocation.PatternIndex++;
                                advancedPattern = true;
                            }
                        }
                    }
                }

                if (playLocation.PatternIndex >= songLength)
                {
                    if (maxLoopCount > 0 && ((loopMode == LoopMode.LoopPoint && song.LoopPoint >= 0) || loopMode == LoopMode.Song) && ++loopCount >= maxLoopCount)
                    {
                        return false;
                    }

                    if (loopMode == LoopMode.LoopPoint) // This loop mode is actually unused.
                    {
                        if (song.LoopPoint >= 0)
                        {
                            playLocation.PatternIndex = song.LoopPoint;
                            playLocation.NoteIndex = 0;
                            advancedPattern = true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else if (loopMode == LoopMode.Song)
                    {
                        playLocation.PatternIndex = Math.Max(0, song.LoopPoint);
                        playLocation.NoteIndex = 0;
                        advancedPattern = true;
                    }
                    else if (loopMode == LoopMode.None)
                    {
                        return false;
                    }
                }
            }

            if (advancedPattern)
            {
                numPlayedPatterns++;
                ResetFamiStudioTempo();
            }

            UpdateBeat();

            return true;
        }

        private void ClearBeat()
        {
            beat = false;
        }

        private void UpdateBeat()
        {
            if (!seeking)
            {
                var beatLength = song.GetPatternBeatLength(playLocation.PatternIndex);

                beat = playLocation.NoteIndex % beatLength == 0;
                beatIndex = playLocation.NoteIndex / beatLength;
            }
            else
            {
                beat = false;
                beatIndex = -1;
            }
        }

        private ChannelState CreateChannelState(int apuIdx, int channelType, int expNumChannels, int tuning, bool pal)
        {
            switch (channelType)
            {
                case ChannelType.Square1:
                case ChannelType.Square2:
                    return new ChannelStateSquare(this, apuIdx, channelType, tuning, pal);
                case ChannelType.Triangle:
                    return new ChannelStateTriangle(this, apuIdx, channelType, tuning, pal);
                case ChannelType.Noise:
                    return new ChannelStateNoise(this, apuIdx, channelType, tuning, pal);
                case ChannelType.Dpcm:
                    return new ChannelStateDpcm(this, apuIdx, channelType, tuning, pal);
                case ChannelType.Vrc6Square1:
                case ChannelType.Vrc6Square2:
                    return new ChannelStateVrc6Square(this, apuIdx, channelType, tuning, pal);
                case ChannelType.Vrc6Saw:
                    return new ChannelStateVrc6Saw(this, apuIdx, channelType, tuning, pal);
                case ChannelType.Vrc7Fm1:
                case ChannelType.Vrc7Fm2:
                case ChannelType.Vrc7Fm3:
                case ChannelType.Vrc7Fm4:
                case ChannelType.Vrc7Fm5:
                case ChannelType.Vrc7Fm6:
                    return new ChannelStateVrc7(this, apuIdx, channelType, tuning);
                case ChannelType.FdsWave:
                    return new ChannelStateFds(this, apuIdx, channelType, tuning, pal);
                case ChannelType.Mmc5Square1:
                case ChannelType.Mmc5Square2:
                    return new ChannelStateMmc5Square(this, apuIdx, channelType, tuning, pal);
                case ChannelType.N163Wave1:
                case ChannelType.N163Wave2:
                case ChannelType.N163Wave3:
                case ChannelType.N163Wave4:
                case ChannelType.N163Wave5:
                case ChannelType.N163Wave6:
                case ChannelType.N163Wave7:
                case ChannelType.N163Wave8:
                    return new ChannelStateN163(this, apuIdx, channelType, tuning, pal, expNumChannels);
                case ChannelType.S5BSquare1:
                case ChannelType.S5BSquare2:
                case ChannelType.S5BSquare3:
                    return new ChannelStateS5B(this, apuIdx, channelType, tuning, pal);
                case ChannelType.EPSMSquare1:
                case ChannelType.EPSMSquare2:
                case ChannelType.EPSMSquare3:
                    return new ChannelStateEPSMSquare(this, apuIdx, channelType, tuning, pal);
                case ChannelType.EPSMFm1:
                case ChannelType.EPSMFm2:
                case ChannelType.EPSMFm3:
                case ChannelType.EPSMFm4:
                case ChannelType.EPSMFm5:
                case ChannelType.EPSMFm6:
                    return new ChannelStateEPSMFm(this, apuIdx, channelType, tuning, pal);
                case ChannelType.EPSMrythm1:
                case ChannelType.EPSMrythm2:
                case ChannelType.EPSMrythm3:
                case ChannelType.EPSMrythm4:
                case ChannelType.EPSMrythm5:
                case ChannelType.EPSMrythm6:
                    return new ChannelStateEPSMRythm(this, apuIdx, channelType, tuning, pal);
            }

            Debug.Assert(false);
            return null;
        }

        protected ChannelState[] CreateChannelStates(Project project, int apuIdx, int tuning, bool pal, int expNumChannels)
        {
            var channelCount = project.GetActiveChannelCount();
            var states = new ChannelState[channelCount];

            int idx = 0;
            for (int i = 0; i < ChannelType.Count; i++)
            {
                if (project.IsChannelActive(i))
                {
                    var state = CreateChannelState(apuIdx, i, expNumChannels, tuning, pal);
                    states[idx++] = state;
                }
            }

            return states;
        }

        protected virtual void BeginFrame()
        {
            NesApu.ResetTriggers(apuIndex);
        }

        private unsafe void EndSeekFrame()
        {
            Debug.Assert(accurateSeek);

            NesApu.EndFrame(apuIndex);
            NesApu.ReadSamples(apuIndex, IntPtr.Zero, NesApu.SamplesAvailable(apuIndex));
        }

        protected virtual unsafe short[] EndFrame()
        {
            NesApu.EndFrame(apuIndex);
            
            var numTotalSamples = NesApu.SamplesAvailable(apuIndex);
            var samples = new short[numTotalSamples * (stereo ? 2 : 1)];

            fixed (short* ptr = &samples[0])
            {
                NesApu.ReadSamples(apuIndex, new IntPtr(ptr), numTotalSamples);
            }

            ReadBackRegisterValues();

            return samples;
        }

        public void ForceInstrumentsReload()
        {
            if (channelStates != null)
            {
                foreach (var channelState in channelStates)
                    channelState.ForceInstrumentReload();
            }
        }

        public void NotifyInstrumentLoaded(Instrument instrument, long channelTypeMask)
        {
            foreach (var channelState in channelStates)
            {
                if (((1L << channelState.InnerChannelType) & channelTypeMask) != 0)
                {
                    channelState.IntrumentLoadedNotify(instrument);
                }
            }
        }

        public virtual void NotifyRegisterWrite(int apuIndex, int reg, int data, int metadata = 0)
        {
        }

        public virtual ChannelState GetChannelByType(int type)
        {
            return Array.Find(channelStates, c => c.InnerChannelType == type);
        }

        public int GetN163AutoWavePosition(Instrument instrument)
        {
            Debug.Assert(instrument != null && instrument.IsN163 && instrument.N163WaveAutoPos);

            // Cant be NULL on instrument player thread, its fine, lets all put them at 0.
            var pos = 0;
            n163AutoWavPosMap?.TryGetValue(instrument.Id, out pos);

            return pos;
        }

        protected void ReadBackRegisterValues()
        {
            if (Settings.ShowRegisterViewer || forceReadRegisterValues)
            {
                lock (registerValues)
                {
                    registerValues.ReadRegisterValues(apuIndex);

                    // Read some additionnal information that we may need for the
                    // register viewer, such as instrument colors, etc.
                    foreach (var state in channelStates)
                    {
                        state.AddRegisterValuesExtraData(registerValues);
                    }
                }
            }
        }

        public void GetRegisterValues(NesApu.NesRegisterValues values)
        {
            lock (registerValues)
            {
                registerValues.CopyTo(values);
            }
        }

        protected int GetOscilloscopeTrigger(int channelType)
        {
            var expType = ChannelType.GetExpansionTypeForChannelType(channelType);
            var chanIdx = ChannelType.GetExpansionChannelIndexForChannelType(channelType);

            return NesApu.GetChannelTrigger(apuIndex, expType, chanIdx);
        }
    };
}
