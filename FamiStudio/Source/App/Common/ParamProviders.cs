using System;
using System.Collections.Generic;

namespace FamiStudio
{
    public class ParamInfo
    {
        public string Name;
        public string ToolTip;
        private int MinValue;
        private int MaxValue;
        public int DefaultValue;
        public bool CanInputValue = true;
        public int SnapValue;
        public int CustomHeight;
        public bool IsList;
        public bool IsEmpty;
        public bool Logarithmic;
        public string TabName;
        public object CustomUserData1;
        public object CustomUserData2;
        public TransactionFlags TransactionFlags;

        public delegate void GetMinMaxValueDelegate(out int min, out int max);
        public delegate int GetValueDelegate();
        public delegate bool EnabledDelegate();
        public delegate void SetValueDelegate(int value);
        public delegate string GetValueStringDelegate();
        public delegate void CustomDrawDelegate(CommandList c, Fonts res, Rectangle rect, object userData1, object userData2);

        public EnabledDelegate IsEnabled;
        public GetValueDelegate GetValue;
        public SetValueDelegate SetValue;
        public GetValueStringDelegate GetValueString;
        public CustomDrawDelegate CustomDraw;
        public GetValueDelegate GetMinValue;
        public GetValueDelegate GetMaxValue;
        public GetValueDelegate GetDefaultValue;
        public EnabledDelegate GetCanInputValue;

        public bool HasTab => !string.IsNullOrEmpty(TabName);

        public int SnapAndClampValue(int value)
        {
            if (SnapValue > 1)
            {
                value = (value / SnapValue) * SnapValue;
            }

            return Utils.Clamp(value, GetMinValue(), GetMaxValue());
        }

        protected ParamInfo(string name, int minVal, int maxVal, int defaultVal, string tooltip, bool list = false, int snap = 1, bool input = true)
        {
            Name = name;
            ToolTip = tooltip;
            MinValue = minVal;
            MaxValue = maxVal;
            DefaultValue = defaultVal;
            CanInputValue = input;
            IsList = list;
            SnapValue = snap;
            GetValueString = () => GetValue().ToString();
            GetMinValue = () => MinValue;
            GetMaxValue = () => MaxValue;
            GetDefaultValue = () => DefaultValue;
            IsEnabled = () => true;
        }

        protected ParamInfo()
        {
            IsEmpty = true;
        }
    };

    public class InstrumentParamInfo : ParamInfo
    {
        public InstrumentParamInfo(Instrument inst, string name, int minVal, int maxVal, int defaultVal, string tooltip = null, bool list = false, int snap = 1) :
            base(name, minVal, maxVal, defaultVal, tooltip, list, snap)
        {
        }

        public InstrumentParamInfo() : base() 
        {
        }
    }

    public static class InstrumentParamProvider
    {
        #region Localization

        // Common labels
        static LocalizedString PitchEnvelopeLabel;
        static LocalizedString AbsoluteLabel;
        static LocalizedString RelativeLabel;
        static LocalizedString OffLabel;

        // FDS/N163 labels
        static LocalizedString MasterVolumeLabel;
        static LocalizedString WavePresetLabel;
        static LocalizedString WaveAutoPosLabel;
        static LocalizedString WavePositionLabel;
        static LocalizedString WaveSizeLabel;
        static LocalizedString WaveCountLabel;
        static LocalizedString ModPresetLabel;
        static LocalizedString ModSpeedLabel;
        static LocalizedString ModDepthLabel;
        static LocalizedString ModDelayLabel;
        static LocalizedString AutoModLabel;
        static LocalizedString AutoModNumerLabel;
        static LocalizedString AutoModDenomLabel;
        static LocalizedString ResamplePeriodLabel;
        static LocalizedString ResampleOffsetLabel;
        static LocalizedString ResampleNormalize;

        // S5B labels
        static LocalizedString EnvelopeShapeLabel;
        static LocalizedString EnvelopeAutoLabel;
        static LocalizedString EnvelopeAutoOctaveLabel;
        static LocalizedString EnvelopeManualFreqLabel;

        // VRC6 labels
        static LocalizedString SawMasterVolumeLabel;

        // VRC7 labels
        static LocalizedString PatchLabel;
        static LocalizedString TremoloLabel;
        static LocalizedString VibratoLabel;
        static LocalizedString SustainedLabel;
        static LocalizedString HalfSineLabel;
        static LocalizedString KeyScalingRateLabel;
        static LocalizedString KeyScalingLevelLabel;
        static LocalizedString FreqMultiplierLabel;
        static LocalizedString AttackLabel;
        static LocalizedString DecayLabel;
        static LocalizedString SustainLabel;
        static LocalizedString ReleaseLabel;
        static LocalizedString LevelLabel;
        static LocalizedString FeedbackLabel;

        // EPSM Labels
        static LocalizedString GeneralTab;
        static LocalizedString AlgorithmLabel;
        static LocalizedString LeftLabel;
        static LocalizedString RightLabel;
        static LocalizedString AMSLabel;
        static LocalizedString PMSLabel;
        static LocalizedString OscillatorEnLabel;
        static LocalizedString OscillatorLabel;
        static LocalizedString DetuneLabel;
        static LocalizedString FrequencyRatioLabel;
        static LocalizedString VolumeLabel;
        static LocalizedString KeyScaleLabel;
        static LocalizedString AttackRateLabel;
        static LocalizedString AmplitudeModulationLabel;
        static LocalizedString DecayRateLabel;
        static LocalizedString SustainRateLabel;
        static LocalizedString SustainLevelLabel;
        static LocalizedString ReleaseRateLabel;
        static LocalizedString SsgEnvEnLabel;
        static LocalizedString SsgEnvLabel;

        // Common tooltips
        static LocalizedString PitchEnvelopeTooltip;

        #endregion

        static InstrumentParamProvider()
        {
            Localization.LocalizeStatic(typeof(InstrumentParamProvider));
        }

        static public bool HasParams(Instrument instrument)
        {
            return
                instrument.IsEnvelopeActive(EnvelopeType.Pitch) ||
                instrument.IsFds  ||
                instrument.IsN163 ||
                instrument.IsVrc6 ||
                instrument.IsEpsm ||
                instrument.IsVrc7;
        }

        static public ParamInfo[] GetParams(Instrument instrument)
        {
            var paramInfos = new List<ParamInfo>();
            var relativePitchParam = (InstrumentParamInfo)null;

            if (instrument.IsEnvelopeActive(EnvelopeType.Pitch))
            {
                relativePitchParam = new InstrumentParamInfo(instrument, PitchEnvelopeLabel, 0, 1, 0, PitchEnvelopeTooltip, true)
                {
                    GetValue = () => { return instrument.Envelopes[EnvelopeType.Pitch].Relative ? 1 : 0; },
                    GetValueString = () => { return instrument.Envelopes[EnvelopeType.Pitch].Relative ? RelativeLabel : AbsoluteLabel; },
                    SetValue = (v) =>
                    {
                        var newRelative = v != 0;

                        /*
                         * Intentially not doing this, this is more confusing/frustrating than anything.
                        if (instrument.Envelopes[EnvelopeType.Pitch].Relative != newRelative)
                        {
                            if (newRelative)
                                instrument.Envelopes[EnvelopeType.Pitch].ConvertToRelative();
                            else
                                instrument.Envelopes[EnvelopeType.Pitch].ConvertToAbsolute();
                        }
                        */

                        instrument.Envelopes[EnvelopeType.Pitch].Relative = newRelative;
                    }
                };
            }

            // TODO : All the bit manipulation should be inside properties on the instruments Really dumb to have all of this here.
            switch (instrument.Expansion)
            {
                case ExpansionType.Fds:
                    paramInfos.Add(new InstrumentParamInfo(instrument, MasterVolumeLabel, 0, 3, 0, null, true)
                        { GetValue = () => { return instrument.FdsMasterVolume; }, GetValueString = () => { return FdsMasterVolumeType.Names[instrument.FdsMasterVolume]; }, SetValue = (v) => { instrument.FdsMasterVolume = (byte)v; } });
                    paramInfos.Add(new InstrumentParamInfo(instrument, WavePresetLabel, 0, WavePresetType.Count - 1, WavePresetType.Sine, null, true)
                        { GetValue = () => { return instrument.FdsWavePreset; }, GetValueString = () => { return WavePresetType.LocalizedNames[instrument.FdsWavePreset]; }, SetValue = (v) => { instrument.FdsWavePreset = (byte)v; } });
                    paramInfos.Add(new InstrumentParamInfo(instrument, WaveCountLabel, 1, instrument.Envelopes[EnvelopeType.FdsWaveform].Values.Length / 64, 1)
                        { GetValue = () => { return instrument.FdsWaveCount; }, SetValue = (v) => { instrument.FdsWaveCount = (byte)v;}, GetMaxValue = () => { return instrument.FdsMaxWaveCount; } });
                    paramInfos.Add(new InstrumentParamInfo(instrument, ModPresetLabel, 0, WavePresetType.CountNoResample - 1, WavePresetType.Flat, null, true )
                        { GetValue = () => { return instrument.FdsModPreset; }, GetValueString = () => { return WavePresetType.LocalizedNames[instrument.FdsModPreset]; }, SetValue = (v) => { instrument.FdsModPreset = (byte)v; } });
                    paramInfos.Add(new InstrumentParamInfo(instrument, ModSpeedLabel, 0, 4095, 0)
                        { GetValue = () => { return instrument.FdsModSpeed; }, SetValue = (v) => { instrument.FdsModSpeed = (ushort)v; }, Logarithmic = true });
                    paramInfos.Add(new InstrumentParamInfo(instrument, ModDepthLabel, 0, 63, 0)
                        { GetValue = () => { return instrument.FdsModDepth; }, SetValue = (v) => { instrument.FdsModDepth = (byte)v; } });
                    paramInfos.Add(new InstrumentParamInfo(instrument, ModDelayLabel, 0, 255, 0)
                        { GetValue = () => { return instrument.FdsModDelay; }, SetValue = (v) => { instrument.FdsModDelay = (byte)v; } });
                    paramInfos.Add(new InstrumentParamInfo(instrument, AutoModLabel, 0, 1, 0)
                        { GetValue = () => { return instrument.FdsAutoMod ? 1 : 0; }, SetValue = (v) => { instrument.FdsAutoMod = v != 0; } });
                    paramInfos.Add(new InstrumentParamInfo(instrument, AutoModNumerLabel, 1, 32, 0)
                        { GetValue = () => { return instrument.FdsAutoModNumer; }, SetValue = (v) => { instrument.FdsAutoModNumer = (byte)v; }, IsEnabled = () => instrument.FdsAutoMod });
                    paramInfos.Add(new InstrumentParamInfo(instrument, AutoModDenomLabel, 1, 32, 0)
                        { GetValue = () => { return instrument.FdsAutoModDenom; }, SetValue = (v) => { instrument.FdsAutoModDenom = (byte)v; }, IsEnabled = () => instrument.FdsAutoMod });
                    paramInfos.Add(new InstrumentParamInfo(instrument, ResamplePeriodLabel, 2, 1024, 128)
                        { GetValue = () => { return instrument.FdsResampleWavePeriod; }, SetValue = (v) => { instrument.FdsResampleWavePeriod = v; }, IsEnabled = () => { return instrument.FdsResampleWaveData != null && instrument.FdsWavePreset == WavePresetType.Resample; } });
                    paramInfos.Add(new InstrumentParamInfo(instrument, ResampleOffsetLabel, 0, 0, 0)
                        { GetValue = () => { return instrument.FdsResampleWaveOffset; }, SetValue = (v) => { instrument.FdsResampleWaveOffset = v; }, IsEnabled = () => { return instrument.FdsResampleWaveData != null && instrument.FdsWavePreset == WavePresetType.Resample; }, GetMaxValue = () => { return instrument.FdsResampleWaveData != null ? instrument.FdsResampleWaveData.Length - 1 : 100; } });
                    paramInfos.Add(new InstrumentParamInfo(instrument, ResampleNormalize, 0, 1, 1)
                        { GetValue = () => { return instrument.FdsResampleWavNormalize ? 1 : 0; }, SetValue = (v) => { instrument.FdsResampleWavNormalize = v != 0; }, IsEnabled = () => { return instrument.FdsResampleWaveData != null && instrument.FdsWavePreset == WavePresetType.Resample; } });
                    break;

                case ExpansionType.N163:
                    paramInfos.Add(new InstrumentParamInfo(instrument, WavePresetLabel, 0, WavePresetType.Count - 1, WavePresetType.Sine, null, true)
                        { GetValue = () => { return instrument.N163WavePreset; }, GetValueString = () => { return WavePresetType.LocalizedNames[instrument.N163WavePreset]; }, SetValue = (v) => { instrument.N163WavePreset = (byte)v;} });
                    paramInfos.Add(new InstrumentParamInfo(instrument, WaveAutoPosLabel, 0, 1, 0)
                        { GetValue = () => { return instrument.N163WaveAutoPos ? 1 : 0; }, SetValue = (v) => { instrument.N163WaveAutoPos = v != 0;}, TransactionFlags = TransactionFlags.StopAudio });
                    paramInfos.Add(new InstrumentParamInfo(instrument, WavePositionLabel, 0, 0, 0, null, false, 2)
                        { GetValue = () => { return instrument.N163WavePos; }, SetValue = (v) => { instrument.N163WavePos = (byte)v;}, GetMaxValue = () => { return instrument.N163MaxWavePos; }, IsEnabled = () => { return !instrument.N163WaveAutoPos; } });
                    paramInfos.Add(new InstrumentParamInfo(instrument, WaveSizeLabel, 4, 4, 16, null, false, 4)
                        { GetValue = () => { return instrument.N163WaveSize; }, SetValue = (v) => { instrument.N163WaveSize = (byte)v;}, GetMaxValue = () => { return instrument.N163MaxWaveSize; } });
                    paramInfos.Add(new InstrumentParamInfo(instrument, WaveCountLabel, 1, 1, 1) 
                        { GetValue = () => { return instrument.N163WaveCount; }, SetValue = (v) => { instrument.N163WaveCount = (byte)v;}, GetMaxValue = () => { return instrument.N163MaxWaveCount; } });
                    paramInfos.Add(new InstrumentParamInfo(instrument, ResamplePeriodLabel, 1, 1024, 128) 
                        { GetValue = () => { return instrument.N163ResampleWavePeriod; }, SetValue = (v) => { instrument.N163ResampleWavePeriod = v;}, IsEnabled = () => { return instrument.N163ResampleWaveData != null && instrument.N163WavePreset == WavePresetType.Resample; } });
                    paramInfos.Add(new InstrumentParamInfo(instrument, ResampleOffsetLabel, 0, 0, 0) 
                        { GetValue = () => { return instrument.N163ResampleWaveOffset; }, SetValue = (v) => { instrument.N163ResampleWaveOffset = v;}, IsEnabled = () => { return instrument.N163ResampleWaveData != null && instrument.N163WavePreset == WavePresetType.Resample; }, GetMaxValue = () => { return instrument.N163ResampleWaveData != null ? instrument.N163ResampleWaveData.Length - 1 : 100; } });
                    paramInfos.Add(new InstrumentParamInfo(instrument, ResampleNormalize, 0, 1, 1) 
                        { GetValue = () => { return instrument.N163ResampleWavNormalize ? 1 : 0; }, SetValue = (v) => { instrument.N163ResampleWavNormalize = v != 0;}, IsEnabled = () => { return instrument.N163ResampleWaveData != null && instrument.N163WavePreset == WavePresetType.Resample; } });
                    break;

                case ExpansionType.S5B:
                    paramInfos.Add(new InstrumentParamInfo(instrument, EnvelopeShapeLabel, 0, 8, 0, null, true)
                        { GetValue = ()  => { return instrument.S5BEnvelopeShape; }, GetValueString = () => { return instrument.S5BEnvelopeShape == 0 ? OffLabel : $"img:S5BEnvelope{instrument.S5BEnvelopeShape + 7:X1}"; }, SetValue = (v) => { instrument.S5BEnvelopeShape = (byte)v; } });
                    paramInfos.Add(new InstrumentParamInfo(instrument, EnvelopeAutoLabel, 0, 1, 1)
                        { GetValue = () => { return instrument.S5BEnvAutoPitch ? 1 : 0; }, SetValue = (v) => { instrument.S5BEnvAutoPitch = v != 0; }, IsEnabled = () => instrument.S5BEnvelopeShape != 0 });
                    paramInfos.Add(new InstrumentParamInfo(instrument, EnvelopeAutoOctaveLabel, -8, 8, 0)
                        { GetValue = () => { return instrument.S5BEnvAutoPitchOctave; }, SetValue = (v) => { instrument.S5BEnvAutoPitchOctave = (sbyte)v; }, IsEnabled = () => instrument.S5BEnvelopeShape != 0 && instrument.S5BEnvAutoPitch });
                    paramInfos.Add(new InstrumentParamInfo(instrument, EnvelopeManualFreqLabel, 0, 65535, 1000)
                        { GetValue = () => { return instrument.S5BEnvelopePitch; }, SetValue = (v) => { instrument.S5BEnvelopePitch = (ushort)v; }, IsEnabled = () => instrument.S5BEnvelopeShape != 0 && !instrument.S5BEnvAutoPitch, Logarithmic = true });
                    break;

                case ExpansionType.Vrc6:
                    paramInfos.Add(new InstrumentParamInfo(instrument, SawMasterVolumeLabel, 0, 2, 0, null, true)
                        { GetValue = ()  => { return instrument.Vrc6SawMasterVolume; }, GetValueString = () => { return Vrc6SawMasterVolumeType.Names[instrument.Vrc6SawMasterVolume]; }, SetValue = (v) => { instrument.Vrc6SawMasterVolume = (byte)v; } });
                    break;

                case ExpansionType.Vrc7:
                    paramInfos.Add(new InstrumentParamInfo(instrument, PatchLabel, 0, 15, 1, null, true)
                        { GetValue = () => { return instrument.Vrc7Patch; }, GetValueString = () => { return Instrument.GetVrc7PatchName(instrument.Vrc7Patch); }, SetValue = (v) => { instrument.Vrc7Patch = (byte)v; } });
                    paramInfos.Add(new InstrumentParamInfo(instrument, "", 0, 0, 0)
                        { GetValue = () => { return 0; }, GetValueString = () => { return ""; }, CustomDraw = CustomDrawAdsrGraph, CustomHeight = 4, CustomUserData1 = instrument, CustomUserData2 = 1, TabName = "Carrier" });
                    paramInfos.Add(new InstrumentParamInfo(instrument, AttackLabel, 0, 15, (Vrc7InstrumentPatch.Infos[1].data[5] & 0xf0) >> 4)
                        { GetValue = () => { return (instrument.Vrc7PatchRegs[5] & 0xf0) >> 4; }, SetValue = (v) => { instrument.Vrc7PatchRegs[5] = (byte)((instrument.Vrc7PatchRegs[5] & (~0xf0)) | ((v << 4) & 0xf0)); instrument.Vrc7Patch = 0; }, TabName = "Carrier" });
                    paramInfos.Add(new InstrumentParamInfo(instrument, DecayLabel, 0, 15, (Vrc7InstrumentPatch.Infos[1].data[5] & 0x0f) >> 0)
                        { GetValue = () => { return (instrument.Vrc7PatchRegs[5] & 0x0f) >> 0; }, SetValue = (v) => { instrument.Vrc7PatchRegs[5] = (byte)((instrument.Vrc7PatchRegs[5] & (~0x0f)) | ((v << 0) & 0x0f)); instrument.Vrc7Patch = 0; }, TabName = "Carrier" });
                    paramInfos.Add(new InstrumentParamInfo(instrument, SustainLabel, 0, 15, (Vrc7InstrumentPatch.Infos[1].data[7] & 0xf0) >> 4)
                        { GetValue = () => { return (instrument.Vrc7PatchRegs[7] & 0xf0) >> 4; }, SetValue = (v) => { instrument.Vrc7PatchRegs[7] = (byte)((instrument.Vrc7PatchRegs[7] & (~0xf0)) | ((v << 4) & 0xf0)); instrument.Vrc7Patch = 0; }, TabName = "Carrier" });
                    paramInfos.Add(new InstrumentParamInfo(instrument, ReleaseLabel, 0, 15, (Vrc7InstrumentPatch.Infos[1].data[7] & 0x0f) >> 0)
                        { GetValue = () => { return (instrument.Vrc7PatchRegs[7] & 0x0f) >> 0; }, SetValue = (v) => { instrument.Vrc7PatchRegs[7] = (byte)((instrument.Vrc7PatchRegs[7] & (~0x0f)) | ((v << 0) & 0x0f)); instrument.Vrc7Patch = 0; }, TabName = "Carrier" });
                    paramInfos.Add(new InstrumentParamInfo(instrument, TremoloLabel, 0, 1, (Vrc7InstrumentPatch.Infos[1].data[1] & 0x80) >> 7)
                        { GetValue = () => { return (instrument.Vrc7PatchRegs[1] & 0x80) >> 7; }, SetValue = (v) => { instrument.Vrc7PatchRegs[1] = (byte)((instrument.Vrc7PatchRegs[1] & (~0x80)) | ((v << 7) & 0x80)); instrument.Vrc7Patch = 0; }, TabName = "Carrier" });
                    paramInfos.Add(new InstrumentParamInfo(instrument, VibratoLabel, 0, 1, (Vrc7InstrumentPatch.Infos[1].data[1] & 0x40) >> 6)
                        { GetValue = () => { return (instrument.Vrc7PatchRegs[1] & 0x40) >> 6; }, SetValue = (v) => { instrument.Vrc7PatchRegs[1] = (byte)((instrument.Vrc7PatchRegs[1] & (~0x40)) | ((v << 6) & 0x40)); instrument.Vrc7Patch = 0; }, TabName = "Carrier" });
                    paramInfos.Add(new InstrumentParamInfo(instrument, SustainedLabel, 0, 1, (Vrc7InstrumentPatch.Infos[1].data[1] & 0x20) >> 5)
                        { GetValue = () => { return (instrument.Vrc7PatchRegs[1] & 0x20) >> 5; }, SetValue = (v) => { instrument.Vrc7PatchRegs[1] = (byte)((instrument.Vrc7PatchRegs[1] & (~0x20)) | ((v << 5) & 0x20)); instrument.Vrc7Patch = 0; }, TabName = "Carrier" });
                    paramInfos.Add(new InstrumentParamInfo(instrument, HalfSineLabel, 0, 1, (Vrc7InstrumentPatch.Infos[1].data[3] & 0x10) >> 4)
                        { GetValue = () => { return (instrument.Vrc7PatchRegs[3] & 0x10) >> 4; }, SetValue = (v) => { instrument.Vrc7PatchRegs[3] = (byte)((instrument.Vrc7PatchRegs[3] & (~0x10)) | ((v << 4) & 0x10)); instrument.Vrc7Patch = 0; }, TabName = "Carrier" });
                    paramInfos.Add(new InstrumentParamInfo(instrument, KeyScalingRateLabel, 0, 1, (Vrc7InstrumentPatch.Infos[1].data[1] & 0x10) >> 4)
                        { GetValue = () => { return (instrument.Vrc7PatchRegs[1] & 0x10) >> 4; }, SetValue = (v) => { instrument.Vrc7PatchRegs[1] = (byte)((instrument.Vrc7PatchRegs[1] & (~0x10)) | ((v << 4) & 0x10)); instrument.Vrc7Patch = 0; }, TabName = "Carrier" });
                    paramInfos.Add(new InstrumentParamInfo(instrument, KeyScalingLevelLabel, 0, 3, (Vrc7InstrumentPatch.Infos[1].data[3] & 0xc0) >> 6)
                        { GetValue = () => { return (instrument.Vrc7PatchRegs[3] & 0xc0) >> 6; }, SetValue = (v) => { instrument.Vrc7PatchRegs[3] = (byte)((instrument.Vrc7PatchRegs[3] & (~0xc0)) | ((v << 6) & 0xc0)); instrument.Vrc7Patch = 0; }, TabName = "Carrier" });
                    paramInfos.Add(new InstrumentParamInfo(instrument, FreqMultiplierLabel, 0, 15, (Vrc7InstrumentPatch.Infos[1].data[1] & 0x0f) >> 0)
                        { GetValue = () => { return (instrument.Vrc7PatchRegs[1] & 0x0f) >> 0; }, SetValue = (v) => { instrument.Vrc7PatchRegs[1] = (byte)((instrument.Vrc7PatchRegs[1] & (~0x0f)) | ((v << 0) & 0x0f)); instrument.Vrc7Patch = 0; }, TabName = "Carrier" });
                    paramInfos.Add(new InstrumentParamInfo() { TabName = "Carrier" });
                    paramInfos.Add(new InstrumentParamInfo() { TabName = "Carrier" });
                    paramInfos.Add(new InstrumentParamInfo(instrument, "", 0, 0, 0)
                        { GetValue = () => { return 0; }, GetValueString = () => { return ""; }, CustomDraw = CustomDrawAdsrGraph, CustomHeight = 4, CustomUserData1 = instrument, CustomUserData2 = 0, TabName = "Modulator" });
                    paramInfos.Add(new InstrumentParamInfo(instrument, AttackLabel, 0, 15, (Vrc7InstrumentPatch.Infos[1].data[4] & 0xf0) >> 4)
                        { GetValue = () => { return (instrument.Vrc7PatchRegs[4] & 0xf0) >> 4; }, SetValue = (v) => { instrument.Vrc7PatchRegs[4] = (byte)((instrument.Vrc7PatchRegs[4] & (~0xf0)) | ((v << 4) & 0xf0)); instrument.Vrc7Patch = 0; }, TabName = "Modulator" });
                    paramInfos.Add(new InstrumentParamInfo(instrument, DecayLabel, 0, 15, (Vrc7InstrumentPatch.Infos[1].data[4] & 0x0f) >> 0)
                        { GetValue = () => { return (instrument.Vrc7PatchRegs[4] & 0x0f) >> 0; }, SetValue = (v) => { instrument.Vrc7PatchRegs[4] = (byte)((instrument.Vrc7PatchRegs[4] & (~0x0f)) | ((v << 0) & 0x0f)); instrument.Vrc7Patch = 0; }, TabName = "Modulator" });
                    paramInfos.Add(new InstrumentParamInfo(instrument, SustainLabel, 0, 15, (Vrc7InstrumentPatch.Infos[1].data[6] & 0xf0) >> 4)
                        { GetValue = () => { return (instrument.Vrc7PatchRegs[6] & 0xf0) >> 4; }, SetValue = (v) => { instrument.Vrc7PatchRegs[6] = (byte)((instrument.Vrc7PatchRegs[6] & (~0xf0)) | ((v << 4) & 0xf0)); instrument.Vrc7Patch = 0; }, TabName = "Modulator" });
                    paramInfos.Add(new InstrumentParamInfo(instrument, ReleaseLabel, 0, 15, (Vrc7InstrumentPatch.Infos[1].data[6] & 0x0f) >> 0)
                        { GetValue = () => { return (instrument.Vrc7PatchRegs[6] & 0x0f) >> 0; }, SetValue = (v) => { instrument.Vrc7PatchRegs[6] = (byte)((instrument.Vrc7PatchRegs[6] & (~0x0f)) | ((v << 0) & 0x0f)); instrument.Vrc7Patch = 0; }, TabName = "Modulator" });
                    paramInfos.Add(new InstrumentParamInfo(instrument, TremoloLabel, 0, 1, (Vrc7InstrumentPatch.Infos[1].data[0] & 0x80) >> 7)
                        { GetValue = () => { return (instrument.Vrc7PatchRegs[0] & 0x80) >> 7; }, SetValue = (v) => { instrument.Vrc7PatchRegs[0] = (byte)((instrument.Vrc7PatchRegs[0] & (~0x80)) | ((v << 7) & 0x80)); instrument.Vrc7Patch = 0; }, TabName = "Modulator" });
                    paramInfos.Add(new InstrumentParamInfo(instrument, VibratoLabel, 0, 1, (Vrc7InstrumentPatch.Infos[1].data[0] & 0x40) >> 6)
                        { GetValue = () => { return (instrument.Vrc7PatchRegs[0] & 0x40) >> 6; }, SetValue = (v) => { instrument.Vrc7PatchRegs[0] = (byte)((instrument.Vrc7PatchRegs[0] & (~0x40)) | ((v << 6) & 0x40)); instrument.Vrc7Patch = 0; }, TabName = "Modulator" });
                    paramInfos.Add(new InstrumentParamInfo(instrument, SustainedLabel, 0, 1, (Vrc7InstrumentPatch.Infos[1].data[0] & 0x20) >> 5)
                        { GetValue = () => { return (instrument.Vrc7PatchRegs[0] & 0x20) >> 5; }, SetValue = (v) => { instrument.Vrc7PatchRegs[0] = (byte)((instrument.Vrc7PatchRegs[0] & (~0x20)) | ((v << 5) & 0x20)); instrument.Vrc7Patch = 0; }, TabName = "Modulator" });
                    paramInfos.Add(new InstrumentParamInfo(instrument, HalfSineLabel, 0, 1, (Vrc7InstrumentPatch.Infos[1].data[3] & 0x08) >> 3)
                        { GetValue = () => { return (instrument.Vrc7PatchRegs[3] & 0x08) >> 3; }, SetValue = (v) => { instrument.Vrc7PatchRegs[3] = (byte)((instrument.Vrc7PatchRegs[3] & (~0x08)) | ((v << 3) & 0x08)); instrument.Vrc7Patch = 0; }, TabName = "Modulator" });
                    paramInfos.Add(new InstrumentParamInfo(instrument, KeyScalingRateLabel, 0, 1, (Vrc7InstrumentPatch.Infos[1].data[0] & 0x10) >> 4)
                        { GetValue = () => { return (instrument.Vrc7PatchRegs[0] & 0x10) >> 4; }, SetValue = (v) => { instrument.Vrc7PatchRegs[0] = (byte)((instrument.Vrc7PatchRegs[0] & (~0x10)) | ((v << 4) & 0x10)); instrument.Vrc7Patch = 0; }, TabName = "Modulator" });
                    paramInfos.Add(new InstrumentParamInfo(instrument, KeyScalingLevelLabel, 0, 3, (Vrc7InstrumentPatch.Infos[1].data[2] & 0xc0) >> 6)
                        { GetValue = () => { return (instrument.Vrc7PatchRegs[2] & 0xc0) >> 6; }, SetValue = (v) => { instrument.Vrc7PatchRegs[2] = (byte)((instrument.Vrc7PatchRegs[2] & (~0xc0)) | ((v << 6) & 0xc0)); instrument.Vrc7Patch = 0; }, TabName = "Modulator" });
                    paramInfos.Add(new InstrumentParamInfo(instrument, FreqMultiplierLabel, 0, 15, (Vrc7InstrumentPatch.Infos[1].data[0] & 0x0f) >> 0)
                        { GetValue = () => { return (instrument.Vrc7PatchRegs[0] & 0x0f) >> 0; }, SetValue = (v) => { instrument.Vrc7PatchRegs[0] = (byte)((instrument.Vrc7PatchRegs[0] & (~0x0f)) | ((v << 0) & 0x0f)); instrument.Vrc7Patch = 0; }, TabName = "Modulator" });
                    paramInfos.Add(new InstrumentParamInfo(instrument, LevelLabel, 0, 63, (Vrc7InstrumentPatch.Infos[1].data[2] & 0x3f) >> 0)
                        { GetValue = () => { return (instrument.Vrc7PatchRegs[2] & 0x3f) >> 0; }, SetValue = (v) => { instrument.Vrc7PatchRegs[2] = (byte)((instrument.Vrc7PatchRegs[2] & (~0x3f)) | ((v << 0) & 0x3f)); instrument.Vrc7Patch = 0; }, TabName = "Modulator" });
                    paramInfos.Add(new InstrumentParamInfo(instrument, FeedbackLabel, 0, 7, (Vrc7InstrumentPatch.Infos[1].data[3] & 0x07) >> 0)
                        { GetValue = () => { return (instrument.Vrc7PatchRegs[3] & 0x07) >> 0; }, SetValue = (v) => { instrument.Vrc7PatchRegs[3] = (byte)((instrument.Vrc7PatchRegs[3] & (~0x07)) | ((v << 0) & 0x07)); instrument.Vrc7Patch = 0; }, TabName = "Modulator" });
                    break;

                case ExpansionType.EPSM:
                    paramInfos.Add(new InstrumentParamInfo(instrument, "", 0, 0, 0)
                        { GetValue = () => { return 0; }, GetValueString = () => { return ""; }, CustomDraw = CustomDrawEpsmAlgorithm, CustomHeight = 4, CustomUserData1 = instrument, TabName = GeneralTab });
                    paramInfos.Add(new InstrumentParamInfo(instrument, AlgorithmLabel, 0, 7, (EpsmInstrumentPatch.Infos[1].data[0] & 0x07) >> 0)
                        { GetValue = () => { return (instrument.EpsmPatchRegs[0] & 0x07) >> 0; }, SetValue = (v) => { instrument.EpsmPatchRegs[0] = (byte)((instrument.EpsmPatchRegs[0] & (~0x07)) | ((v << 0) & 0x07)); instrument.EpsmPatch = 0; }, TabName = GeneralTab });
                    relativePitchParam.TabName = GeneralTab;
                    paramInfos.Add(relativePitchParam);
                    paramInfos.Add(new InstrumentParamInfo(instrument, PatchLabel, 0, 1, 1, null, true)//set number of patches
                        { GetValue = () => { return instrument.EpsmPatch; }, GetValueString = () => { return Instrument.GetEpsmPatchName(instrument.EpsmPatch); }, SetValue = (v) => { instrument.EpsmPatch = (byte)v; }, TabName = GeneralTab });
                    paramInfos.Add(new InstrumentParamInfo(instrument, LeftLabel, 0, 1, (EpsmInstrumentPatch.Infos[1].data[1] & 0x80) >> 7)
                        { GetValue = () => { return (instrument.EpsmPatchRegs[1] & 0x80) >> 7; }, SetValue = (v) => { instrument.EpsmPatchRegs[1] = (byte)((instrument.EpsmPatchRegs[1] & (~0x80)) | ((v << 7) & 0x80)); instrument.EpsmPatch = 0; }, TabName = GeneralTab });
                    paramInfos.Add(new InstrumentParamInfo(instrument, RightLabel, 0, 1, (EpsmInstrumentPatch.Infos[1].data[1] & 0x40) >> 6)
                        { GetValue = () => { return (instrument.EpsmPatchRegs[1] & 0x40) >> 6; }, SetValue = (v) => { instrument.EpsmPatchRegs[1] = (byte)((instrument.EpsmPatchRegs[1] & (~0x40)) | ((v << 6) & 0x40)); instrument.EpsmPatch = 0; }, TabName = GeneralTab });
                    paramInfos.Add(new InstrumentParamInfo(instrument, AMSLabel, 0, 3, (EpsmInstrumentPatch.Infos[1].data[1] & 0x30) >> 4)
                        { GetValue = () => { return (instrument.EpsmPatchRegs[1] & 0x30) >> 4; }, SetValue = (v) => { instrument.EpsmPatchRegs[1] = (byte)((instrument.EpsmPatchRegs[1] & (~0x30)) | ((v << 4) & 0x30)); instrument.EpsmPatch = 0; }, TabName = GeneralTab });
                    paramInfos.Add(new InstrumentParamInfo(instrument, PMSLabel, 0, 7, (EpsmInstrumentPatch.Infos[1].data[1] & 0x07) >> 0)
                        { GetValue = () => { return (instrument.EpsmPatchRegs[1] & 0x07) >> 0; }, SetValue = (v) => { instrument.EpsmPatchRegs[1] = (byte)((instrument.EpsmPatchRegs[1] & (~0x07)) | ((v << 0) & 0x07)); instrument.EpsmPatch = 0; }, TabName = GeneralTab });
                    paramInfos.Add(new InstrumentParamInfo(instrument, OscillatorEnLabel, 0, 1, (EpsmInstrumentPatch.Infos[1].data[30] & 0x08) >> 3, "Low Frequency Oscillator (Vibrato)\nThis setting applies to all channels, Last channel instrument to load dictates the setting")
                        { GetValue = () => { return (instrument.EpsmPatchRegs[30] & 0x08) >> 3; }, SetValue = (v) => { instrument.EpsmPatchRegs[30] = (byte)((instrument.EpsmPatchRegs[30] & (~0x08)) | ((v << 3) & 0x08)); instrument.EpsmPatch = 0; }, TabName = GeneralTab });
                    paramInfos.Add(new InstrumentParamInfo(instrument, OscillatorLabel, 0, 7, (EpsmInstrumentPatch.Infos[1].data[30] & 0x07) >> 0, "freq(Hz) 3.98 5.56 6.02 6.37 6.88 9.63 48.1 72.2")
                        { GetValue = () => { return (instrument.EpsmPatchRegs[30] & 0x07) >> 0; }, SetValue = (v) => { instrument.EpsmPatchRegs[30] = (byte)((instrument.EpsmPatchRegs[30] & (~0x07)) | ((v << 0) & 0x07)); instrument.EpsmPatch = 0; }, TabName = GeneralTab });
                    paramInfos.Add(new InstrumentParamInfo(instrument, EnvelopeShapeLabel, 0, 8, 0, null, true)
                        { GetValue = ()  => { return instrument.EPSMSquareEnvelopeShape; }, GetValueString = () => { return instrument.EPSMSquareEnvelopeShape == 0 ? OffLabel : $"img:S5BEnvelope{instrument.EPSMSquareEnvelopeShape + 7:X1}"; }, SetValue = (v) => { instrument.EPSMSquareEnvelopeShape = (byte)v; }, TabName = GeneralTab });
                    paramInfos.Add(new InstrumentParamInfo(instrument, EnvelopeAutoLabel, 0, 1, 1)
                        { GetValue = () => { return instrument.EPSMSquareEnvAutoPitch ? 1 : 0; }, SetValue = (v) => { instrument.EPSMSquareEnvAutoPitch = v != 0; }, IsEnabled = () => instrument.EPSMSquareEnvelopeShape != 0, TabName = GeneralTab });
                    paramInfos.Add(new InstrumentParamInfo(instrument, EnvelopeAutoOctaveLabel, -8, 8, 0)
                        { GetValue = () => { return instrument.EPSMSquareEnvAutoPitchOctave; }, SetValue = (v) => { instrument.EPSMSquareEnvAutoPitchOctave = (sbyte)v; }, IsEnabled = () => instrument.EPSMSquareEnvelopeShape != 0 && instrument.EPSMSquareEnvAutoPitch, TabName = GeneralTab });
                    paramInfos.Add(new InstrumentParamInfo(instrument, EnvelopeManualFreqLabel, 0, 65535, 4000)
                        { GetValue = () => { return instrument.EPSMSquareEnvelopePitch; }, SetValue = (v) => { instrument.EPSMSquareEnvelopePitch = (ushort)v; }, IsEnabled = () => instrument.EPSMSquareEnvelopeShape != 0 && !instrument.EPSMSquareEnvAutoPitch, Logarithmic = true, TabName = GeneralTab });

                    for (int i = 0; i < 4; i++)
                    {
                        var tabName = $"OP{i + 1}";
                        int i2 = 7 * i;

                        paramInfos.Add(new InstrumentParamInfo(instrument, "", 0, 0, 0)
                            { GetValue = () => { return 0; }, GetValueString = () => { return ""; }, CustomDraw = CustomDrawAdsrGraph, CustomHeight = 4, CustomUserData1 = instrument, CustomUserData2 = i, TabName = tabName });
                        paramInfos.Add(new InstrumentParamInfo(instrument, AttackRateLabel, 0, 31, (EpsmInstrumentPatch.Infos[1].data[(4 + i2)] & 0x1f) >> 0)
                            { GetValue = () => { return (instrument.EpsmPatchRegs[(4 + i2)] & 0x1f) >> 0; }, SetValue = (v) => { instrument.EpsmPatchRegs[(4 + i2)] = (byte)((instrument.EpsmPatchRegs[(4 + i2)] & (~0x1f)) | ((v << 0) & 0x1f)); instrument.EpsmPatch = 0; }, TabName = tabName });
                        paramInfos.Add(new InstrumentParamInfo(instrument, DecayRateLabel, 0, 31, (EpsmInstrumentPatch.Infos[1].data[(5 + i2)] & 0x1f) >> 0)
                            { GetValue = () => { return (instrument.EpsmPatchRegs[(5 + i2)] & 0x1f) >> 0; }, SetValue = (v) => { instrument.EpsmPatchRegs[(5 + i2)] = (byte)((instrument.EpsmPatchRegs[(5 + i2)] & (~0x1f)) | ((v << 0) & 0x1f)); instrument.EpsmPatch = 0; }, TabName = tabName });
                        paramInfos.Add(new InstrumentParamInfo(instrument, SustainRateLabel, 0, 31, (EpsmInstrumentPatch.Infos[1].data[(6 + i2)] & 0x1f) >> 0)
                            { GetValue = () => { return (instrument.EpsmPatchRegs[(6 + i2)] & 0x1f) >> 0; }, SetValue = (v) => { instrument.EpsmPatchRegs[(6 + i2)] = (byte)((instrument.EpsmPatchRegs[(6 + i2)] & (~0x1f)) | ((v << 0) & 0x1f)); instrument.EpsmPatch = 0; }, TabName = tabName });
                        paramInfos.Add(new InstrumentParamInfo(instrument, SustainLevelLabel, 0, 15, (EpsmInstrumentPatch.Infos[1].data[(7 + i2)] & 0xf0) >> 4)
                            { GetValue = () => { return (instrument.EpsmPatchRegs[(7 + i2)] & 0xf0) >> 4; }, SetValue = (v) => { instrument.EpsmPatchRegs[(7 + i2)] = (byte)((instrument.EpsmPatchRegs[(7 + i2)] & (~0xf0)) | ((v << 4) & 0xf0)); instrument.EpsmPatch = 0; }, TabName = tabName });
                        paramInfos.Add(new InstrumentParamInfo(instrument, ReleaseRateLabel, 0, 15, (EpsmInstrumentPatch.Infos[1].data[(7 + i2)] & 0x0f) >> 0)
                            { GetValue = () => { return (instrument.EpsmPatchRegs[(7 + i2)] & 0x0f) >> 0; }, SetValue = (v) => { instrument.EpsmPatchRegs[(7 + i2)] = (byte)((instrument.EpsmPatchRegs[(7 + i2)] & (~0x0f)) | ((v << 0) & 0x0f)); instrument.EpsmPatch = 0; }, TabName = tabName });
                        paramInfos.Add(new InstrumentParamInfo(instrument, DetuneLabel, 0, 7, (EpsmInstrumentPatch.Infos[1].data[2 + 6 * i] & 0x70) >> 4)
                            { GetValue = () => { return (instrument.EpsmPatchRegs[(2 + i2)] & 0x70) >> 4; }, SetValue = (v) => { instrument.EpsmPatchRegs[(2 + i2)] = (byte)((instrument.EpsmPatchRegs[(2 + i2)] & (~0x70)) | ((v << 4) & 0x70)); instrument.EpsmPatch = 0; }, TabName = tabName });
                        paramInfos.Add(new InstrumentParamInfo(instrument, FrequencyRatioLabel, 0, 15, (EpsmInstrumentPatch.Infos[1].data[(2 + i2)] & 0x0f) >> 0)
                            { GetValue = () => { return (instrument.EpsmPatchRegs[(2 + i2)] & 0x0f) >> 0; }, SetValue = (v) => { instrument.EpsmPatchRegs[(2 + i2)] = (byte)((instrument.EpsmPatchRegs[(2 + i2)] & (~0x0f)) | ((v << 0) & 0x0f)); instrument.EpsmPatch = 0; }, TabName = tabName });
                        paramInfos.Add(new InstrumentParamInfo(instrument, VolumeLabel, 0, 127, (EpsmInstrumentPatch.Infos[1].data[(3 + i2)] & 0x7f) >> 0)
                            { GetValue = () => { return (instrument.EpsmPatchRegs[(3 + i2)] & 0x7f) >> 0; }, SetValue = (v) => { instrument.EpsmPatchRegs[(3 + i2)] = (byte)((instrument.EpsmPatchRegs[(3 + i2)] & (~0x7f)) | ((v << 0) & 0x7f)); instrument.EpsmPatch = 0; }, TabName = tabName });
                        paramInfos.Add(new InstrumentParamInfo(instrument, KeyScaleLabel, 0, 3, (EpsmInstrumentPatch.Infos[1].data[(4 + i2)] & 0xc0) >> 6)
                            { GetValue = () => { return (instrument.EpsmPatchRegs[(4 + i2)] & 0xc0) >> 6; }, SetValue = (v) => { instrument.EpsmPatchRegs[(4 + i2)] = (byte)((instrument.EpsmPatchRegs[(4 + i2)] & (~0xc0)) | ((v << 6) & 0xc0)); instrument.EpsmPatch = 0; }, TabName = tabName });
                        paramInfos.Add(new InstrumentParamInfo(instrument, AmplitudeModulationLabel, 0, 1, (EpsmInstrumentPatch.Infos[1].data[(5 + i2)] & 0x80) >> 7)
                            { GetValue = () => { return (instrument.EpsmPatchRegs[(5 + i2)] & 0x80) >> 7; }, SetValue = (v) => { instrument.EpsmPatchRegs[(5 + i2)] = (byte)((instrument.EpsmPatchRegs[(5 + i2)] & (~0x80)) | ((v << 7) & 0x80)); instrument.EpsmPatch = 0; }, TabName = tabName });
                        paramInfos.Add(new InstrumentParamInfo(instrument, SsgEnvEnLabel, 0, 1, (EpsmInstrumentPatch.Infos[1].data[(8 + i2)] & 0x08) >> 3)
                            { GetValue = () => { return (instrument.EpsmPatchRegs[(8 + i2)] & 0x08) >> 3; }, SetValue = (v) => { instrument.EpsmPatchRegs[(8 + i2)] = (byte)((instrument.EpsmPatchRegs[(8 + i2)] & (~0x08)) | ((v << 3) & 0x08)); instrument.EpsmPatch = 0; }, TabName = tabName });
                        paramInfos.Add(new InstrumentParamInfo(instrument, SsgEnvLabel, 0, 7, (EpsmInstrumentPatch.Infos[1].data[(8 + i2)] & 0x07) >> 0, null, true)
                            { GetValue = ()  => { return (instrument.EpsmPatchRegs[(8 + i2)] & 0x07) >> 0; }, GetValueString = () => { return $"img:S5BEnvelope{((instrument.EpsmPatchRegs[(8 + i2)] & 0x07) >> 0) + 8:X1}"; }, SetValue = (v) => { instrument.EpsmPatchRegs[(8 + i2)] = (byte)((instrument.EpsmPatchRegs[(8 + i2)] & (~0x07)) | ((v << 0) & 0x07)); instrument.EpsmPatch = 0; }, TabName = tabName });
                        if (i == 0)
                        {
                            paramInfos.Add(new InstrumentParamInfo(instrument, FeedbackLabel, 0, 7, (EpsmInstrumentPatch.Infos[1].data[0] & 0x38) >> 3)
                                { GetValue = () => { return (instrument.EpsmPatchRegs[0] & 0x38) >> 3; }, SetValue = (v) => { instrument.EpsmPatchRegs[0] = (byte)((instrument.EpsmPatchRegs[0] & (~0x38)) | ((v << 3) & 0x38)); instrument.EpsmPatch = 0; }, TabName = tabName });
                        }
                        else
                        {
                            paramInfos.Add(new InstrumentParamInfo() { TabName = tabName });
                        }
                    }

                    break;
            }

            if (relativePitchParam != null && !paramInfos.Contains(relativePitchParam))
            {
                paramInfos.Insert(0, relativePitchParam);
            }

            return paramInfos.Count == 0 ? null : paramInfos.ToArray();
        }

        public static void CustomDrawAdsrGraph(CommandList c, Fonts res, Rectangle rect, object userData1, object userData2)
        {
            var g = c.Graphics;
            var instrument = userData1 as Instrument;
            var op = (int)userData2;

            var graphWidth  = rect.Width;
            var graphHeight = rect.Height;
            var graphPaddingTop = DpiScaling.ScaleForWindow(3); 
            var graphPaddingCurve = rect.Height / 10;
            var graphHeightMinusPadding = graphHeight - graphPaddingTop - graphPaddingCurve;

            var decayStartX   = 0;
            var decayStartY   = 0;
            var sustainStartX = 0;
            var sustainStartY = 0;
            var releaseStartX = 0;
            var releaseStartY = 0;
            var releaseEndX   = 0;
            var releaseEndY   = 0;

            if (instrument.IsVrc7)
            {
                var opAttackRate   = (instrument.Vrc7PatchRegs[op + 4] & 0xf0) >> 4; // "Carrier Attack"
                var opDecayRate    = (instrument.Vrc7PatchRegs[op + 4] & 0x0f) >> 0; // "Carrier Decay"
                var opSustainRate  = (instrument.Vrc7PatchRegs[op + 0] & 0x20) >> 5; // "Carrier Sustained"
                var opSustainLevel = (instrument.Vrc7PatchRegs[op + 6] & 0xf0) >> 4; // "Carrier Sustain"
                var opReleaseRate  = (instrument.Vrc7PatchRegs[op + 6] & 0x0f) >> 0; // "Carrier Release"
                var opVolume       = (op == 0)?(instrument.Vrc7PatchRegs[op + 2] & 0x3f) >> 0: 0; // "Level"/"Volume" ???
                var volumeScale    = (63 - opVolume) / 63.0f;

                decayStartX = (int)((15.0f - opAttackRate) / 15.0f * 0.25f * graphWidth * volumeScale);
                decayStartY = (int)(((63.0f - opVolume) / 63.0f) * graphHeightMinusPadding);

                sustainStartX = (int)((15.0f - opDecayRate) / 15.0f * 0.25f * graphWidth * volumeScale) + decayStartX;
                sustainStartY = (int)(decayStartY / 15.0f * (15.0f - opSustainLevel));

                if (opDecayRate == 0)
                {
                    sustainStartY = decayStartY;
                }

                releaseStartX = (int)(graphWidth * 0.25f * volumeScale) + sustainStartX;
                releaseStartY = sustainStartY / 2;

                if (opSustainRate == 1)
                {
                    releaseStartY = sustainStartY;
                }

                releaseEndX = (int)((15.0f - opReleaseRate) / 15.0f * 0.25 * graphWidth * volumeScale) + releaseStartX;
                releaseEndY = 0;

                if (opReleaseRate == 0)
                {
                    releaseEndY = releaseStartY;
                    releaseEndX = (graphWidth);
                }
            }
            else if (instrument.IsEpsm)
            {
                int opAttackRate   = (instrument.EpsmPatchRegs[7*op + 4] & 0x1f); //31
                int opDecayRate    = (instrument.EpsmPatchRegs[7*op + 5] & 0x1f); //31
                int opSustainRate  = (instrument.EpsmPatchRegs[7*op + 6] & 0x1f); //31
                int opSustainLevel = (instrument.EpsmPatchRegs[7*op + 7] & 0xf0) >> 4; //15
                int opReleaseRate  = (instrument.EpsmPatchRegs[7*op + 7] & 0x0f); //15
                int opVolume       = (instrument.EpsmPatchRegs[7*op + 3] & 0x7f); //127
                var volumeScale    = (float)(127 - opVolume) / 127;

                decayStartX = (int)((31.0f - opAttackRate) / 31.0f * 0.25f * graphWidth * volumeScale);
                decayStartY = (int)(((127.0f - opVolume) / 127.0f) * graphHeightMinusPadding);
                sustainStartX = (int)((31.0f - opDecayRate) / 31.0f * 0.25f * graphWidth * volumeScale) + decayStartX;
                sustainStartY = (int)(decayStartY / 15.0f * (15 - opSustainLevel));

                if (opDecayRate == 0)
                {
                    sustainStartY = decayStartY;
                }

                releaseStartX = (int)((31.0f - opSustainRate) / 31.0f * 0.25f * graphWidth * volumeScale) + sustainStartX;
                releaseStartY = sustainStartY / 2;

                if (opSustainRate == 0)
                {
                    releaseStartY = sustainStartY;
                    releaseStartX = (int)(graphWidth * 0.25f * volumeScale) + sustainStartX;
                }

                releaseEndX = (int)((15.0f - opReleaseRate) / 15.0f * 0.25f * graphWidth * volumeScale) + releaseStartX;
                releaseEndY = 0;
                
                if (opReleaseRate == 0)
                {
                    releaseEndY = releaseStartY;
                    releaseEndX = (graphWidth);
                }
            }

            // Flip.
            decayStartY   = graphHeight - decayStartY;
            sustainStartY = graphHeight - sustainStartY;
            releaseStartY = graphHeight - releaseStartY;
            releaseEndY   = graphHeight - releaseEndY;

            var attackGeo  = new float[4 * 2] { 0, graphHeight, decayStartX, graphHeight, decayStartX, decayStartY,  0, graphHeight };
            var decayGeo   = new float[4 * 2] { decayStartX, graphHeight,  sustainStartX, graphHeight, sustainStartX, sustainStartY, decayStartX, decayStartY }; 
            var sustainGeo = new float[4 * 2] { sustainStartX, graphHeight, releaseStartX, graphHeight, releaseStartX, releaseStartY, sustainStartX, sustainStartY }; 
            var releaseGeo = new float[4 * 2] { releaseStartX, graphHeight, releaseEndX, graphHeight, releaseEndX, releaseEndY,  releaseStartX, releaseStartY }; 
            var line       = new float[5 * 2] { 0, graphHeight,  decayStartX, decayStartY, sustainStartX, sustainStartY, releaseStartX, releaseStartY, releaseEndX, releaseEndY };

            var fillColor = Color.FromArgb(75, Color.Black);
            c.FillAndDrawRectangle(0, graphPaddingTop, graphWidth, graphHeight, fillColor, Theme.BlackColor);
            c.FillGeometry(attackGeo,  fillColor);
            c.FillGeometry(decayGeo,   fillColor);
            c.FillGeometry(sustainGeo, fillColor);
            c.FillGeometry(releaseGeo, fillColor);
            c.DrawLine(line, Theme.BlackColor, 1, true);
        }

        public static void CustomDrawEpsmAlgorithm(CommandList c, Fonts res, Rectangle rect, object userData1, object userData2)
        {
            var instrument = userData1 as Instrument;
            var algo = instrument.EpsmPatchRegs[0] & 0x07;
            var bmp = c.Graphics.GetTextureAtlasRef($"Algorithm{algo}");
            var bmpSize = bmp.ElementSize;

            var paddingTop = DpiScaling.ScaleForWindow(3);
            rect.Y += paddingTop;
            rect.Height -= paddingTop;

            var posX = (rect.Left + rect.Right)  / 2 - bmpSize.Width  / 2;
            var posY = (rect.Top  + rect.Bottom) / 2 - bmpSize.Height / 2;

            c.FillAndDrawRectangle(rect, Color.FromArgb(128, Color.Black), Theme.BlackColor);
            c.DrawTextureAtlas(bmp, posX, posY);
        }
    }

    public class DPCMSampleParamInfo : ParamInfo
    {
        public DPCMSampleParamInfo(DPCMSample sample, string name, int minVal, int maxVal, int defaultVal, string tooltip, bool list = false, bool canInput = true) :
            base(name, minVal, maxVal, defaultVal, tooltip, list, 1, canInput)
        {
        }
    }

    public static class DPCMSampleParamProvider
    {
        #region Localization

        // Labels
        static LocalizedString PreviewRateLabel;
        static LocalizedString SampleRateLabel;
        static LocalizedString PaddingModeLabel;
        static LocalizedString DmcInitialValueDiv2Label;
        static LocalizedString VolumeAdjustLabel;
        static LocalizedString FineTuningLabel;
        static LocalizedString ProcessPalLabel;
        static LocalizedString TrimZeroVolumeLabel;
        static LocalizedString ReverseBitsLabel;
        static LocalizedString BankNumberLabel;

        // tooltips
        static LocalizedString PreviewRateTooltip;
        static LocalizedString SampleRateTooltip;
        static LocalizedString PaddingModeTooltip;
        static LocalizedString DmcInitialValueDiv2Tooltip;
        static LocalizedString VolumeAdjustTooltip;
        static LocalizedString FineTuningTooltip;
        static LocalizedString ProcessPalTooltip;
        static LocalizedString TrimZeroVolumeTooltip;
        static LocalizedString ReverseBitsTooltip;
        static LocalizedString BankNumberTooltip;

        #endregion

        static DPCMSampleParamProvider()
        {
            Localization.LocalizeStatic(typeof(DPCMSampleParamProvider));
        }

        static public ParamInfo[] GetParams(DPCMSample sample)
        {
            return new[]
            {
                new DPCMSampleParamInfo(sample, PreviewRateLabel, 0, 15, 15, PreviewRateTooltip, true)
                    { GetValue = () => { return sample.PreviewRate; }, GetValueString = () => { return DPCMSampleRate.GetString(true, FamiStudio.StaticInstance.PalPlayback, true, false, sample.PreviewRate); }, SetValue = (v) => { sample.PreviewRate = (byte)v; } },
                new DPCMSampleParamInfo(sample, SampleRateLabel, 0, 15, 15, SampleRateTooltip, true)
                    { GetValue = () => { return sample.SampleRate; }, GetValueString = () => { return DPCMSampleRate.GetString(true, FamiStudio.StaticInstance.PalPlayback, true, false, sample.SampleRate); }, SetValue = (v) => { sample.SampleRate = (byte)v; sample.Process(); } },
                new DPCMSampleParamInfo(sample, PaddingModeLabel, 0, 6, DPCMPaddingType.PadTo16Bytes, PaddingModeTooltip, true)
                    { GetValue = () => { return sample.PaddingMode; }, GetValueString = () => { return DPCMPaddingType.Names[sample.PaddingMode]; }, SetValue = (v) => { sample.PaddingMode = v; sample.Process(); } },
                new DPCMSampleParamInfo(sample, DmcInitialValueDiv2Label, 0, 63, NesApu.DACDefaultValueDiv2, DmcInitialValueDiv2Tooltip)
                    { GetValue = () => { return sample.DmcInitialValueDiv2; }, SetValue = (v) => { sample.DmcInitialValueDiv2 = v; sample.Process(); } },
                new DPCMSampleParamInfo(sample, VolumeAdjustLabel, 0, 200, 100, VolumeAdjustTooltip)
                    { GetValue = () => { return sample.VolumeAdjust; }, SetValue = (v) => { sample.VolumeAdjust = v; sample.Process(); } },
                new DPCMSampleParamInfo(sample, FineTuningLabel, 0, 200, 100, FineTuningTooltip, false, false)
                    { GetValue = () => { return (int)Math.Round((sample.FinePitch - 0.95f) * 2000); }, SetValue = (v) => { sample.FinePitch = (v / 2000.0f) + 0.95f; sample.Process(); }, GetValueString = () => { return (sample.FinePitch * 100.0f).ToString("N2") + "%"; } },
                new DPCMSampleParamInfo(sample, ProcessPalLabel, 0, 1, 0, ProcessPalTooltip)
                    { GetValue = () => { return  sample.PalProcessing ? 1 : 0; }, SetValue = (v) => { sample.PalProcessing = v != 0; sample.Process(); } },
                new DPCMSampleParamInfo(sample, TrimZeroVolumeLabel, 0, 1, 0, TrimZeroVolumeTooltip)
                    { GetValue = () => { return sample.TrimZeroVolume ? 1 : 0; }, SetValue = (v) => { sample.TrimZeroVolume = v != 0; sample.Process(); } },
                new DPCMSampleParamInfo(sample, ReverseBitsLabel, 0, 1, 0, ReverseBitsTooltip)
                    { GetValue = () => { return !sample.SourceDataIsWav && sample.ReverseBits ? 1 : 0; }, SetValue = (v) => { if (!sample.SourceDataIsWav) { sample.ReverseBits = v != 0; sample.Process(); } }, IsEnabled = () => { return !sample.SourceDataIsWav; } },
                new DPCMSampleParamInfo(sample, BankNumberLabel, 0, Project.MaxDPCMBanks - 1, 0, BankNumberTooltip)
                    { GetValue = () => { return sample.Bank; }, SetValue = (v) => { sample.Bank = v; } },
            };
        }
    }
}
