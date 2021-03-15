using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FamiStudio
{
    public class ParamInfo
    {
        public string Name;
        public string ToolTip;
        public int MinValue;
        public int MaxValue;
        public int DefaultValue;
        public int SnapValue;
        public bool IsList;

        public delegate int GetValueDelegate();
        public delegate bool EnabledDelegate();
        public delegate void SetValueDelegate(int value);
        public delegate string GetValueStringDelegate();

        public EnabledDelegate IsEnabled;
        public GetValueDelegate GetValue;
        public SetValueDelegate SetValue;
        public GetValueStringDelegate GetValueString;

        public int SnapAndClampValue(int value)
        {
            if (SnapValue > 1)
            {
                value = (value / SnapValue) * SnapValue;
            }

            return Utils.Clamp(value, MinValue, MaxValue);
        }

        protected ParamInfo(string name, int minVal, int maxVal, int defaultVal, string tooltip, bool list = false, int snap = 1)
        {
            Name = name;
            ToolTip = tooltip;
            MinValue = minVal;
            MaxValue = maxVal;
            DefaultValue = defaultVal;
            IsList = list;
            SnapValue = snap;
            GetValueString = () => GetValue().ToString();
        }
    };

    public class InstrumentParamInfo : ParamInfo
    {
        public InstrumentParamInfo(Instrument inst, string name, int minVal, int maxVal, int defaultVal, string tooltip = null, bool list = false, int snap = 1) :
            base(name, minVal, maxVal, defaultVal, tooltip, list, snap)
        {
        }
    }

    public static class InstrumentParamProvider
    {
        static public bool HasParams(Instrument instrument)
        {
            return
                instrument.ExpansionType == ExpansionType.Fds  ||
                instrument.ExpansionType == ExpansionType.N163 ||
                instrument.ExpansionType == ExpansionType.Vrc6 ||
                instrument.ExpansionType == ExpansionType.Vrc7;
        }

        static public ParamInfo[] GetParams(Instrument instrument)
        {
            switch (instrument.ExpansionType)
            {
                case ExpansionType.Fds:
                    return new[]
                    {
                        new InstrumentParamInfo(instrument, "Master Volume", 0, 3, 0, null, true)
                            { GetValue = () => { return instrument.FdsMasterVolume; }, GetValueString = () => { return FdsMasterVolumeType.Names[instrument.FdsMasterVolume]; }, SetValue = (v) => { instrument.FdsMasterVolume = (byte)v; } },
                        new InstrumentParamInfo(instrument, "Wave Preset", 0, WavePresetType.Count - 1, WavePresetType.Sine, null, true)
                            { GetValue = () => { return instrument.FdsWavePreset; }, GetValueString = () => { return WavePresetType.Names[instrument.FdsWavePreset]; }, SetValue = (v) => { instrument.FdsWavePreset = (byte)v; instrument.UpdateFdsWaveEnvelope(); } },
                        new InstrumentParamInfo(instrument, "Mod Preset", 0, WavePresetType.Count - 1, WavePresetType.Flat, null, true )
                            { GetValue = () => { return instrument.FdsModPreset; }, GetValueString = () => { return WavePresetType.Names[instrument.FdsModPreset]; }, SetValue = (v) => { instrument.FdsModPreset = (byte)v; instrument.UpdateFdsModulationEnvelope(); } },
                        new InstrumentParamInfo(instrument, "Mod Speed", 0, 4095, 0)
                            { GetValue = () => { return instrument.FdsModSpeed; }, SetValue = (v) => { instrument.FdsModSpeed = (ushort)v; } },
                        new InstrumentParamInfo(instrument, "Mod Depth", 0, 63, 0)
                            { GetValue = () => { return instrument.FdsModDepth; }, SetValue = (v) => { instrument.FdsModDepth = (byte)v; } },
                        new InstrumentParamInfo(instrument, "Mod Delay", 0, 255, 0)
                            { GetValue = () => { return instrument.FdsModDelay; }, SetValue = (v) => { instrument.FdsModDelay = (byte)v; } },
                    };

                case ExpansionType.N163:
                    return new[]
                    {
                        new InstrumentParamInfo(instrument, "Wave Preset", 0, WavePresetType.Count - 1, WavePresetType.Sine, null, true)
                            { GetValue = () => { return instrument.N163WavePreset; }, GetValueString = () => { return WavePresetType.Names[instrument.N163WavePreset]; }, SetValue = (v) => { instrument.N163WavePreset = (byte)v;} },
                        new InstrumentParamInfo(instrument, "Wave Size", 4, 248, 16, null, false, 4)
                            { GetValue = () => { return instrument.N163WaveSize; }, SetValue = (v) => { instrument.N163WaveSize = (byte)v;} },
                        new InstrumentParamInfo(instrument, "Wave Position", 0, 244, 0, null, false, 4)
                            { GetValue = () => { return instrument.N163WavePos; }, SetValue = (v) => { instrument.N163WavePos = (byte)v;} },
                    };

                case ExpansionType.Vrc6:
                    return new[]
                    {
                        new InstrumentParamInfo(instrument, "Saw Master Volume", 0, 2, 0, null, true)
                            { GetValue = ()  => { return instrument.Vrc6SawMasterVolume; }, GetValueString = () => { return Vrc6SawMasterVolumeType.Names[instrument.Vrc6SawMasterVolume]; }, SetValue = (v) => { instrument.Vrc6SawMasterVolume = (byte)v; } },
                    };

                case ExpansionType.Vrc7:
                    return new[]
                    {
                        new InstrumentParamInfo(instrument, "Patch", 0, 15, 1, null, true)
                            { GetValue = ()  => { return instrument.Vrc7Patch; }, GetValueString = () => { return Instrument.GetVrc7PatchName(instrument.Vrc7Patch); }, SetValue = (v) => { instrument.Vrc7Patch = (byte)v; } },
                        new InstrumentParamInfo(instrument, "Carrier Tremolo", 0, 1, (Vrc7InstrumentPatch.Infos[1].data[1] & 0x80) >> 7)
                            { GetValue = ()  => { return (instrument.Vrc7PatchRegs[1] & 0x80) >> 7; }, SetValue = (v) => { instrument.Vrc7PatchRegs[1] = (byte)((instrument.Vrc7PatchRegs[1] & (~0x80)) | ((v << 7) & 0x80)); instrument.Vrc7Patch = 0; } },
                        new InstrumentParamInfo(instrument, "Carrier Vibrato", 0, 1, (Vrc7InstrumentPatch.Infos[1].data[1] & 0x40) >> 6)
                            { GetValue = ()  => { return (instrument.Vrc7PatchRegs[1] & 0x40) >> 6; }, SetValue = (v) => { instrument.Vrc7PatchRegs[1] = (byte)((instrument.Vrc7PatchRegs[1] & (~0x40)) | ((v << 6) & 0x40)); instrument.Vrc7Patch = 0; } },
                        new InstrumentParamInfo(instrument, "Carrier Sustained", 0, 1, (Vrc7InstrumentPatch.Infos[1].data[1] & 0x20) >> 5)
                            { GetValue = ()  => { return (instrument.Vrc7PatchRegs[1] & 0x20) >> 5; }, SetValue = (v) => { instrument.Vrc7PatchRegs[1] = (byte)((instrument.Vrc7PatchRegs[1] & (~0x20)) | ((v << 5) & 0x20)); instrument.Vrc7Patch = 0; } },
                        new InstrumentParamInfo(instrument, "Carrier Wave Rectified", 0, 1, (Vrc7InstrumentPatch.Infos[1].data[3] & 0x10) >> 4)
                            { GetValue = ()  => { return (instrument.Vrc7PatchRegs[3] & 0x10) >> 4; }, SetValue = (v) => { instrument.Vrc7PatchRegs[3] = (byte)((instrument.Vrc7PatchRegs[3] & (~0x10)) | ((v << 4) & 0x10)); instrument.Vrc7Patch = 0; } },
                        new InstrumentParamInfo(instrument, "Carrier KeyScaling", 0, 1, (Vrc7InstrumentPatch.Infos[1].data[1] & 0x10) >> 4)
                            { GetValue = ()  => { return (instrument.Vrc7PatchRegs[1] & 0x10) >> 4; }, SetValue = (v) => { instrument.Vrc7PatchRegs[1] = (byte)((instrument.Vrc7PatchRegs[1] & (~0x10)) | ((v << 4) & 0x10)); instrument.Vrc7Patch = 0; } },
                        new InstrumentParamInfo(instrument, "Carrier KeyScaling Level", 0, 3, (Vrc7InstrumentPatch.Infos[1].data[3] & 0xc0) >> 6)
                            { GetValue = ()  => { return (instrument.Vrc7PatchRegs[3] & 0xc0) >> 6; }, SetValue = (v) => { instrument.Vrc7PatchRegs[3] = (byte)((instrument.Vrc7PatchRegs[3] & (~0xc0)) | ((v << 6) & 0xc0)); instrument.Vrc7Patch = 0; } },
                        new InstrumentParamInfo(instrument, "Carrier FreqMultiplier", 0, 15, (Vrc7InstrumentPatch.Infos[1].data[1] & 0x0f) >> 0)
                            { GetValue = ()  => { return (instrument.Vrc7PatchRegs[1] & 0x0f) >> 0; }, SetValue = (v) => { instrument.Vrc7PatchRegs[1] = (byte)((instrument.Vrc7PatchRegs[1] & (~0x0f)) | ((v << 0) & 0x0f)); instrument.Vrc7Patch = 0; } },
                        new InstrumentParamInfo(instrument, "Carrier Attack", 0, 15, (Vrc7InstrumentPatch.Infos[1].data[5] & 0xf0) >> 4)
                            { GetValue = ()  => { return (instrument.Vrc7PatchRegs[5] & 0xf0) >> 4; }, SetValue = (v) => { instrument.Vrc7PatchRegs[5] = (byte)((instrument.Vrc7PatchRegs[5] & (~0xf0)) | ((v << 4) & 0xf0)); instrument.Vrc7Patch = 0; } },
                        new InstrumentParamInfo(instrument, "Carrier Decay", 0, 15, (Vrc7InstrumentPatch.Infos[1].data[5] & 0x0f) >> 0)
                            { GetValue = ()  => { return (instrument.Vrc7PatchRegs[5] & 0x0f) >> 0; }, SetValue = (v) => { instrument.Vrc7PatchRegs[5] = (byte)((instrument.Vrc7PatchRegs[5] & (~0x0f)) | ((v << 0) & 0x0f)); instrument.Vrc7Patch = 0; } },
                        new InstrumentParamInfo(instrument, "Carrier Sustain", 0, 15, (Vrc7InstrumentPatch.Infos[1].data[7] & 0xf0) >> 4)
                            { GetValue = ()  => { return (instrument.Vrc7PatchRegs[7] & 0xf0) >> 4; }, SetValue = (v) => { instrument.Vrc7PatchRegs[7] = (byte)((instrument.Vrc7PatchRegs[7] & (~0xf0)) | ((v << 4) & 0xf0)); instrument.Vrc7Patch = 0; } },
                        new InstrumentParamInfo(instrument, "Carrier Release", 0, 15, (Vrc7InstrumentPatch.Infos[1].data[7] & 0x0f) >> 0)
                            { GetValue = ()  => { return (instrument.Vrc7PatchRegs[7] & 0x0f) >> 0; }, SetValue = (v) => { instrument.Vrc7PatchRegs[7] = (byte)((instrument.Vrc7PatchRegs[7] & (~0x0f)) | ((v << 0) & 0x0f)); instrument.Vrc7Patch = 0; } },
                        new InstrumentParamInfo(instrument, "Modulator Tremolo", 0, 1, (Vrc7InstrumentPatch.Infos[1].data[0] & 0x80) >> 7)
                            { GetValue = ()  => { return (instrument.Vrc7PatchRegs[0] & 0x80) >> 7; }, SetValue = (v) => { instrument.Vrc7PatchRegs[0] = (byte)((instrument.Vrc7PatchRegs[0] & (~0x80)) | ((v << 7) & 0x80)); instrument.Vrc7Patch = 0; } },
                        new InstrumentParamInfo(instrument, "Modulator Vibrato", 0, 1, (Vrc7InstrumentPatch.Infos[1].data[0] & 0x40) >> 6)
                            { GetValue = ()  => { return (instrument.Vrc7PatchRegs[0] & 0x40) >> 6; }, SetValue = (v) => { instrument.Vrc7PatchRegs[0] = (byte)((instrument.Vrc7PatchRegs[0] & (~0x40)) | ((v << 6) & 0x40)); instrument.Vrc7Patch = 0; } },
                        new InstrumentParamInfo(instrument, "Modulator Sustained", 0, 1, (Vrc7InstrumentPatch.Infos[1].data[0] & 0x20) >> 5)
                            { GetValue = ()  => { return (instrument.Vrc7PatchRegs[0] & 0x20) >> 5; }, SetValue = (v) => { instrument.Vrc7PatchRegs[0] = (byte)((instrument.Vrc7PatchRegs[0] & (~0x20)) | ((v << 5) & 0x20)); instrument.Vrc7Patch = 0; } },
                        new InstrumentParamInfo(instrument, "Modulator Wave Rectified", 0, 1, (Vrc7InstrumentPatch.Infos[1].data[3] & 0x08) >> 3)
                            { GetValue = ()  => { return (instrument.Vrc7PatchRegs[3] & 0x08) >> 3; }, SetValue = (v) => { instrument.Vrc7PatchRegs[3] = (byte)((instrument.Vrc7PatchRegs[3] & (~0x08)) | ((v << 3) & 0x08)); instrument.Vrc7Patch = 0; } },
                        new InstrumentParamInfo(instrument, "Modulator KeyScaling", 0, 1, (Vrc7InstrumentPatch.Infos[1].data[0] & 0x10) >> 4)
                            { GetValue = ()  => { return (instrument.Vrc7PatchRegs[0] & 0x10) >> 4; }, SetValue = (v) => { instrument.Vrc7PatchRegs[0] = (byte)((instrument.Vrc7PatchRegs[0] & (~0x10)) | ((v << 4) & 0x10)); instrument.Vrc7Patch = 0; } },
                        new InstrumentParamInfo(instrument, "Modulator KeyScaling Level", 0, 3, (Vrc7InstrumentPatch.Infos[1].data[2] & 0xc0) >> 6)
                            { GetValue = ()  => { return (instrument.Vrc7PatchRegs[2] & 0xc0) >> 6; }, SetValue = (v) => { instrument.Vrc7PatchRegs[2] = (byte)((instrument.Vrc7PatchRegs[2] & (~0xc0)) | ((v << 6) & 0xc0)); instrument.Vrc7Patch = 0; } },
                        new InstrumentParamInfo(instrument, "Modulator FreqMultiplier", 0, 15, (Vrc7InstrumentPatch.Infos[1].data[0] & 0x0f) >> 0)
                            { GetValue = ()  => { return (instrument.Vrc7PatchRegs[0] & 0x0f) >> 0; }, SetValue = (v) => { instrument.Vrc7PatchRegs[0] = (byte)((instrument.Vrc7PatchRegs[0] & (~0x0f)) | ((v << 0) & 0x0f)); instrument.Vrc7Patch = 0; } },
                        new InstrumentParamInfo(instrument, "Modulator Attack", 0, 15, (Vrc7InstrumentPatch.Infos[1].data[4] & 0xf0) >> 4)
                            { GetValue = ()  => { return (instrument.Vrc7PatchRegs[4] & 0xf0) >> 4; }, SetValue = (v) => { instrument.Vrc7PatchRegs[4] = (byte)((instrument.Vrc7PatchRegs[4] & (~0xf0)) | ((v << 4) & 0xf0)); instrument.Vrc7Patch = 0; } },
                        new InstrumentParamInfo(instrument, "Modulator Decay", 0, 15, (Vrc7InstrumentPatch.Infos[1].data[4] & 0x0f) >> 0)
                            { GetValue = ()  => { return (instrument.Vrc7PatchRegs[4] & 0x0f) >> 0; }, SetValue = (v) => { instrument.Vrc7PatchRegs[4] = (byte)((instrument.Vrc7PatchRegs[4] & (~0x0f)) | ((v << 0) & 0x0f)); instrument.Vrc7Patch = 0; } },
                        new InstrumentParamInfo(instrument, "Modulator Sustain", 0, 15, (Vrc7InstrumentPatch.Infos[1].data[6] & 0xf0) >> 4)
                            { GetValue = ()  => { return (instrument.Vrc7PatchRegs[6] & 0xf0) >> 4; }, SetValue = (v) => { instrument.Vrc7PatchRegs[6] = (byte)((instrument.Vrc7PatchRegs[6] & (~0xf0)) | ((v << 4) & 0xf0)); instrument.Vrc7Patch = 0; } },
                        new InstrumentParamInfo(instrument, "Modulator Release", 0, 15, (Vrc7InstrumentPatch.Infos[1].data[6] & 0x0f) >> 0)
                            { GetValue = ()  => { return (instrument.Vrc7PatchRegs[6] & 0x0f) >> 0; }, SetValue = (v) => { instrument.Vrc7PatchRegs[6] = (byte)((instrument.Vrc7PatchRegs[6] & (~0x0f)) | ((v << 0) & 0x0f)); instrument.Vrc7Patch = 0; } },
                        new InstrumentParamInfo(instrument, "Modulator Level", 0, 63, (Vrc7InstrumentPatch.Infos[1].data[2] & 0x3f) >> 0)
                            { GetValue = ()  => { return (instrument.Vrc7PatchRegs[2] & 0x3f) >> 0; }, SetValue = (v) => { instrument.Vrc7PatchRegs[2] = (byte)((instrument.Vrc7PatchRegs[2] & (~0x3f)) | ((v << 0) & 0x3f)); instrument.Vrc7Patch = 0; } },
                        new InstrumentParamInfo(instrument, "Feedback", 0, 7, (Vrc7InstrumentPatch.Infos[1].data[3] & 0x07) >> 0)
                            { GetValue = ()  => { return (instrument.Vrc7PatchRegs[3] & 0x07) >> 0; }, SetValue = (v) => { instrument.Vrc7PatchRegs[3] = (byte)((instrument.Vrc7PatchRegs[3] & (~0x07)) | ((v << 0) & 0x07)); instrument.Vrc7Patch = 0; } }
                    };
            }

            return null;
        }
    }

    public class DPCMSampleParamInfo : ParamInfo
    {
        public DPCMSampleParamInfo(DPCMSample sample, string name, int minVal, int maxVal, int defaultVal, string tooltip, bool list = false) :
            base(name, minVal, maxVal, defaultVal, tooltip, list)
        {
        }
    }

    public static class DPCMSampleParamProvider
    {
        static public ParamInfo[] GetParams(DPCMSample sample)
        {
            return new[]
            {
                new DPCMSampleParamInfo(sample, "Preview Rate", 0, 15, 15, "Rate to use when previewing the processed\nDMC data with the play button above", true)
                    { GetValue = () => { return sample.PreviewRate; }, GetValueString = () => { return DPCMSampleRate.Strings[FamiStudio.StaticInstance.PalPlayback ? 1 : 0][sample.PreviewRate]; }, SetValue = (v) => { sample.PreviewRate = (byte)v; } },
                new DPCMSampleParamInfo(sample, "Sample Rate", 0, 15, 15, "Rate at which to resample the source data at", true)
                    { GetValue = () => { return sample.SampleRate; }, GetValueString = () => { return DPCMSampleRate.Strings[sample.PalProcessing ? 1 : 0][sample.SampleRate]; }, SetValue = (v) => { sample.SampleRate = (byte)v; sample.Process(); } },
                new DPCMSampleParamInfo(sample, "Padding Mode", 0, 4, DPCMPaddingType.PadTo16Bytes, "Padding method for the processed DMC data", true)
                    { GetValue = () => { return sample.PaddingMode; }, GetValueString = () => { return DPCMPaddingType.Names[sample.PaddingMode]; }, SetValue = (v) => { sample.PaddingMode = v; sample.Process(); } },
                new DPCMSampleParamInfo(sample, "Volume Adjust", 0, 200, 100, "Volume adjustment (%)")
                    { GetValue = () => { return sample.VolumeAdjust; }, SetValue = (v) => { sample.VolumeAdjust = v; sample.Process(); } },
                new DPCMSampleParamInfo(sample, "Process as PAL", 0, 1, 0, "Use PAL sample rates for all processing\nFor DMC source data, assumes PAL sample rate")
                    { GetValue = () => { return  sample.PalProcessing ? 1 : 0; }, SetValue = (v) => { sample.PalProcessing = v != 0; sample.Process(); } },
                new DPCMSampleParamInfo(sample, "Trim Zero Volume", 0, 1, 0, "Trim parts of the source data that is considered too low to be audible")
                    { GetValue = () => { return sample.TrimZeroVolume ? 1 : 0; }, SetValue = (v) => { sample.TrimZeroVolume = v != 0; sample.Process(); } },
                new DPCMSampleParamInfo(sample, "Reverse Bits", 0, 1, 0, "For DMC source data only, reverse the bits to correct errors in some NES games")
                    { GetValue = () => { return !sample.SourceDataIsWav && sample.ReverseBits ? 1 : 0; }, SetValue = (v) => { if (!sample.SourceDataIsWav) { sample.ReverseBits = v != 0; sample.Process(); } }, IsEnabled = () => { return !sample.SourceDataIsWav; } }
            };
        }
    }
}
