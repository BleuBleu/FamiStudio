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
        public int MinValue;
        public int MaxValue;
        public int SnapValue;
        public bool IsList;

        public delegate int GetValueDelegate();
        public delegate void SetValueDelegate(int value);
        public delegate string GetValueStringDelegate();

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

        protected ParamInfo(string name, int minVal, int maxVal, bool list = false, int snap = 1)
        {
            Name = name;
            MinValue = minVal;
            MaxValue = maxVal;
            IsList = list;
            SnapValue = snap;
            GetValueString = () => GetValue().ToString();
        }
    };

    public class InstrumentParamInfo : ParamInfo
    {
        public InstrumentParamInfo(Instrument inst, string name, int minVal, int maxVal, bool list = false, int snap = 1) :
            base(name, minVal, maxVal, list, snap)
        {
        }
    }

    public static class InstrumentParamProvider
    {
        static readonly string[] FdsVolumeStrings =
        {
            "100%", "66%", "50%", "40%"
        };

        static public bool HasParams(Instrument instrument)
        {
            return
                instrument.ExpansionType == Project.ExpansionFds ||
                instrument.ExpansionType == Project.ExpansionN163 ||
                instrument.ExpansionType == Project.ExpansionVrc7;
        }

        static public ParamInfo[] GetParams(Instrument instrument)
        {
            switch (instrument.ExpansionType)
            {
                case Project.ExpansionFds:
                    return new[]
                    {
                        new InstrumentParamInfo(instrument, "Master Volume", 0, 3, true)
                            { GetValue = () => { return instrument.FdsMasterVolume; }, GetValueString = () => { return FdsVolumeStrings[instrument.FdsMasterVolume]; }, SetValue = (v) => { instrument.FdsMasterVolume = (byte)v; } },
                        new InstrumentParamInfo(instrument, "Wave Preset", 0, Envelope.WavePresetMax - 1, true)
                            { GetValue = () => { return instrument.FdsWavePreset; }, GetValueString = () => { return Envelope.PresetNames[instrument.FdsWavePreset]; }, SetValue = (v) => { instrument.FdsWavePreset = (byte)v; instrument.UpdateFdsWaveEnvelope(); } },
                        new InstrumentParamInfo(instrument, "Mod Preset", 0, Envelope.WavePresetMax - 1, true )
                            { GetValue = () => { return instrument.FdsModPreset; }, GetValueString = () => { return Envelope.PresetNames[instrument.FdsModPreset]; }, SetValue = (v) => { instrument.FdsModPreset = (byte)v; instrument.UpdateFdsModulationEnvelope(); } },
                        new InstrumentParamInfo(instrument, "Mod Speed", 0, 4095)
                            { GetValue = () => { return instrument.FdsModSpeed; }, SetValue = (v) => { instrument.FdsModSpeed = (ushort)v; } },
                        new InstrumentParamInfo(instrument, "Mod Depth", 0, 63)
                            { GetValue = () => { return instrument.FdsModDepth; }, SetValue = (v) => { instrument.FdsModDepth = (byte)v; } },
                        new InstrumentParamInfo(instrument, "Mod Delay", 0, 255)
                            { GetValue = () => { return instrument.FdsModDelay; }, SetValue = (v) => { instrument.FdsModDelay = (byte)v; } },
                    };

                case Project.ExpansionN163:
                    return new[]
                    {
                        new InstrumentParamInfo(instrument, "Wave Preset", 0, Envelope.WavePresetMax - 1, true)
                            { GetValue = () => { return instrument.N163WavePreset; }, GetValueString = () => { return Envelope.PresetNames[instrument.N163WavePreset]; }, SetValue = (v) => { instrument.N163WavePreset = (byte)v;} },
                        new InstrumentParamInfo(instrument, "Wave Size", 4, 248, false, 4)
                            { GetValue = () => { return instrument.N163WaveSize; }, SetValue = (v) => { instrument.N163WaveSize = (byte)v;} },
                        new InstrumentParamInfo(instrument, "Wave Position", 0, 244, false, 4)
                            { GetValue = () => { return instrument.N163WavePos; }, SetValue = (v) => { instrument.N163WavePos = (byte)v;} },
                    };

                case Project.ExpansionVrc7:
                    return new[]
                    {
                        new InstrumentParamInfo(instrument, "Patch", 0, 15, true)
                            { GetValue = ()  => { return instrument.Vrc7Patch; }, GetValueString = () => { return Instrument.GetVrc7PatchName(instrument.Vrc7Patch); }, SetValue = (v) => { instrument.Vrc7Patch = (byte)v; } },
                        new InstrumentParamInfo(instrument, "Carrier Tremolo", 0, 1)
                            { GetValue = ()  => { return (instrument.Vrc7PatchRegs[1] & 0x80) >> 7; }, SetValue = (v) => { instrument.Vrc7PatchRegs[1] = (byte)((instrument.Vrc7PatchRegs[1] & (~0x80)) | ((v << 7) & 0x80)); instrument.Vrc7Patch = 0; } },
                        new InstrumentParamInfo(instrument, "Carrier Vibrato", 0, 1)
                            { GetValue = ()  => { return (instrument.Vrc7PatchRegs[1] & 0x40) >> 6; }, SetValue = (v) => { instrument.Vrc7PatchRegs[1] = (byte)((instrument.Vrc7PatchRegs[1] & (~0x40)) | ((v << 6) & 0x40)); instrument.Vrc7Patch = 0; } },
                        new InstrumentParamInfo(instrument, "Carrier Sustained", 0, 1)
                            { GetValue = ()  => { return (instrument.Vrc7PatchRegs[1] & 0x20) >> 5; }, SetValue = (v) => { instrument.Vrc7PatchRegs[1] = (byte)((instrument.Vrc7PatchRegs[1] & (~0x20)) | ((v << 5) & 0x20)); instrument.Vrc7Patch = 0; } },
                        new InstrumentParamInfo(instrument, "Carrier Wave Rectified", 0, 1)
                            { GetValue = ()  => { return (instrument.Vrc7PatchRegs[3] & 0x10) >> 4; }, SetValue = (v) => { instrument.Vrc7PatchRegs[3] = (byte)((instrument.Vrc7PatchRegs[3] & (~0x10)) | ((v << 4) & 0x10)); instrument.Vrc7Patch = 0; } },
                        new InstrumentParamInfo(instrument, "Carrier KeyScaling", 0, 1)
                            { GetValue = ()  => { return (instrument.Vrc7PatchRegs[1] & 0x10) >> 4; }, SetValue = (v) => { instrument.Vrc7PatchRegs[1] = (byte)((instrument.Vrc7PatchRegs[1] & (~0x10)) | ((v << 4) & 0x10)); instrument.Vrc7Patch = 0; } },
                        new InstrumentParamInfo(instrument, "Carrier KeyScaling Level", 0, 3)
                            { GetValue = ()  => { return (instrument.Vrc7PatchRegs[3] & 0xc0) >> 6; }, SetValue = (v) => { instrument.Vrc7PatchRegs[3] = (byte)((instrument.Vrc7PatchRegs[3] & (~0xc0)) | ((v << 6) & 0xc0)); instrument.Vrc7Patch = 0; } },
                        new InstrumentParamInfo(instrument, "Carrier FreqMultiplier", 0, 15)
                            { GetValue = ()  => { return (instrument.Vrc7PatchRegs[1] & 0x0f) >> 0; }, SetValue = (v) => { instrument.Vrc7PatchRegs[1] = (byte)((instrument.Vrc7PatchRegs[1] & (~0x0f)) | ((v << 0) & 0x0f)); instrument.Vrc7Patch = 0; } },
                        new InstrumentParamInfo(instrument, "Carrier Attack", 0, 15)
                            { GetValue = ()  => { return (instrument.Vrc7PatchRegs[5] & 0xf0) >> 4; }, SetValue = (v) => { instrument.Vrc7PatchRegs[5] = (byte)((instrument.Vrc7PatchRegs[5] & (~0xf0)) | ((v << 4) & 0xf0)); instrument.Vrc7Patch = 0; } },
                        new InstrumentParamInfo(instrument, "Carrier Decay", 0, 15)
                            { GetValue = ()  => { return (instrument.Vrc7PatchRegs[5] & 0x0f) >> 0; }, SetValue = (v) => { instrument.Vrc7PatchRegs[5] = (byte)((instrument.Vrc7PatchRegs[5] & (~0x0f)) | ((v << 0) & 0x0f)); instrument.Vrc7Patch = 0; } },
                        new InstrumentParamInfo(instrument, "Carrier Sustain", 0, 15)
                            { GetValue = ()  => { return (instrument.Vrc7PatchRegs[7] & 0xf0) >> 4; }, SetValue = (v) => { instrument.Vrc7PatchRegs[7] = (byte)((instrument.Vrc7PatchRegs[7] & (~0xf0)) | ((v << 4) & 0xf0)); instrument.Vrc7Patch = 0; } },
                        new InstrumentParamInfo(instrument, "Carrier Release", 0, 15)
                            { GetValue = ()  => { return (instrument.Vrc7PatchRegs[7] & 0x0f) >> 0; }, SetValue = (v) => { instrument.Vrc7PatchRegs[7] = (byte)((instrument.Vrc7PatchRegs[7] & (~0x0f)) | ((v << 0) & 0x0f)); instrument.Vrc7Patch = 0; } },
                        new InstrumentParamInfo(instrument, "Modulator Tremolo", 0, 1)
                            { GetValue = ()  => { return (instrument.Vrc7PatchRegs[0] & 0x80) >> 7; }, SetValue = (v) => { instrument.Vrc7PatchRegs[0] = (byte)((instrument.Vrc7PatchRegs[0] & (~0x80)) | ((v << 7) & 0x80)); instrument.Vrc7Patch = 0; } },
                        new InstrumentParamInfo(instrument, "Modulator Vibrato", 0, 1)
                            { GetValue = ()  => { return (instrument.Vrc7PatchRegs[0] & 0x40) >> 6; }, SetValue = (v) => { instrument.Vrc7PatchRegs[0] = (byte)((instrument.Vrc7PatchRegs[0] & (~0x40)) | ((v << 6) & 0x40)); instrument.Vrc7Patch = 0; } },
                        new InstrumentParamInfo(instrument, "Modulator Sustained", 0, 1)
                            { GetValue = ()  => { return (instrument.Vrc7PatchRegs[0] & 0x20) >> 5; }, SetValue = (v) => { instrument.Vrc7PatchRegs[0] = (byte)((instrument.Vrc7PatchRegs[0] & (~0x20)) | ((v << 5) & 0x20)); instrument.Vrc7Patch = 0; } },
                        new InstrumentParamInfo(instrument, "Modulator Wave Rectified", 0, 1)
                            { GetValue = ()  => { return (instrument.Vrc7PatchRegs[3] & 0x08) >> 3; }, SetValue = (v) => { instrument.Vrc7PatchRegs[3] = (byte)((instrument.Vrc7PatchRegs[3] & (~0x08)) | ((v << 3) & 0x08)); instrument.Vrc7Patch = 0; } },
                        new InstrumentParamInfo(instrument, "Modulator KeyScaling", 0, 1)
                            { GetValue = ()  => { return (instrument.Vrc7PatchRegs[0] & 0x10) >> 4; }, SetValue = (v) => { instrument.Vrc7PatchRegs[0] = (byte)((instrument.Vrc7PatchRegs[0] & (~0x10)) | ((v << 4) & 0x10)); instrument.Vrc7Patch = 0; } },
                        new InstrumentParamInfo(instrument, "Modulator KeyScaling Level", 0, 3)
                            { GetValue = ()  => { return (instrument.Vrc7PatchRegs[2] & 0xc0) >> 6; }, SetValue = (v) => { instrument.Vrc7PatchRegs[2] = (byte)((instrument.Vrc7PatchRegs[2] & (~0xc0)) | ((v << 6) & 0xc0)); instrument.Vrc7Patch = 0; } },
                        new InstrumentParamInfo(instrument, "Modulator FreqMultiplier", 0, 15)
                            { GetValue = ()  => { return (instrument.Vrc7PatchRegs[0] & 0x0f) >> 0; }, SetValue = (v) => { instrument.Vrc7PatchRegs[0] = (byte)((instrument.Vrc7PatchRegs[0] & (~0x0f)) | ((v << 0) & 0x0f)); instrument.Vrc7Patch = 0; } },
                        new InstrumentParamInfo(instrument, "Modulator Attack", 0, 15)
                            { GetValue = ()  => { return (instrument.Vrc7PatchRegs[4] & 0xf0) >> 4; }, SetValue = (v) => { instrument.Vrc7PatchRegs[4] = (byte)((instrument.Vrc7PatchRegs[4] & (~0xf0)) | ((v << 4) & 0xf0)); instrument.Vrc7Patch = 0; } },
                        new InstrumentParamInfo(instrument, "Modulator Decay", 0, 15)
                            { GetValue = ()  => { return (instrument.Vrc7PatchRegs[4] & 0x0f) >> 0; }, SetValue = (v) => { instrument.Vrc7PatchRegs[4] = (byte)((instrument.Vrc7PatchRegs[4] & (~0x0f)) | ((v << 0) & 0x0f)); instrument.Vrc7Patch = 0; } },
                        new InstrumentParamInfo(instrument, "Modulator Sustain", 0, 15)
                            { GetValue = ()  => { return (instrument.Vrc7PatchRegs[6] & 0xf0) >> 4; }, SetValue = (v) => { instrument.Vrc7PatchRegs[6] = (byte)((instrument.Vrc7PatchRegs[6] & (~0xf0)) | ((v << 4) & 0xf0)); instrument.Vrc7Patch = 0; } },
                        new InstrumentParamInfo(instrument, "Modulator Release", 0, 15)
                            { GetValue = ()  => { return (instrument.Vrc7PatchRegs[6] & 0x0f) >> 0; }, SetValue = (v) => { instrument.Vrc7PatchRegs[6] = (byte)((instrument.Vrc7PatchRegs[6] & (~0x0f)) | ((v << 0) & 0x0f)); instrument.Vrc7Patch = 0; } },
                        new InstrumentParamInfo(instrument, "Modulator Level", 0, 63)
                            { GetValue = ()  => { return (instrument.Vrc7PatchRegs[2] & 0x3f) >> 0; }, SetValue = (v) => { instrument.Vrc7PatchRegs[2] = (byte)((instrument.Vrc7PatchRegs[2] & (~0x3f)) | ((v << 0) & 0x3f)); instrument.Vrc7Patch = 0; } },
                        new InstrumentParamInfo(instrument, "Feedback", 0, 7)
                            { GetValue = ()  => { return (instrument.Vrc7PatchRegs[3] & 0x07) >> 0; }, SetValue = (v) => { instrument.Vrc7PatchRegs[3] = (byte)((instrument.Vrc7PatchRegs[3] & (~0x07)) | ((v << 0) & 0x07)); instrument.Vrc7Patch = 0; } }
                    };
            }

            return null;
        }
    }

    public class DPCMSampleParamInfo : ParamInfo
    {
        public DPCMSampleParamInfo(DPCMSample sample, string name, int minVal, int maxVal, bool list = false) :
            base(name, minVal, maxVal, list)
        {
        }
    }

    public static class DPCMSampleParamProvider
    {
        // DPCMTODO : How to handle PAL????

        static readonly string[] FrequencyStringsNtsc =
        {
            "0 (4.2 KHz)",
            "1 (4.7 KHz)",
            "2 (5.3 KHz)",
            "3 (5.6 KHz)",
            "4 (6.3 KHz)",
            "5 (7.0 KHz)",
            "6 (8.9 KHz)",
            "7 (8.3 KHz)",
            "8 (9.4 KHz)",
            "9 (11.1 KHz)",
            "10 (12.6 KHz)",
            "11 (14.9 KHz)",
            "12 (16.9 KHz)",
            "13 (21.3 KHz)",
            "14 (24.9 KHz)",
            "15 (33.1 KHz)",
        };

        static public ParamInfo[] GetParams(DPCMSample sample)
        {
            return new[]
            {
                new DPCMSampleParamInfo(sample, "Preview Rate", 0, 15, true)
                    { GetValue = () => { return sample.PreviewRate; }, GetValueString = () => { return FrequencyStringsNtsc[sample.PreviewRate]; }, SetValue = (v) => { sample.PreviewRate = (byte)v; } },
                new DPCMSampleParamInfo(sample, "Sample Rate", 0, 15, true)
                    { GetValue = () => { return sample.SampleRate; }, GetValueString = () => { return FrequencyStringsNtsc[sample.SampleRate]; }, SetValue = (v) => { sample.SampleRate = (byte)v; sample.Process(); } },
                new DPCMSampleParamInfo(sample, "Volume Adjust", 0, 200)
                    { GetValue = () => { return sample.VolumeAdjust; }, SetValue = (v) => { sample.VolumeAdjust = v; sample.Process(); } },
                new DPCMSampleParamInfo(sample, "Trim Zero Volume", 0, 1)
                    { GetValue = () => { return sample.TrimZeroVolume ? 1 : 0; }, SetValue = (v) => { sample.TrimZeroVolume = v != 0; sample.Process(); } },
                new DPCMSampleParamInfo(sample, "Reverse Bits", 0, 1)
                    { GetValue = () => { return sample.ReverseBits ? 1 : 0; }, SetValue = (v) => { sample.ReverseBits = v != 0; sample.Process(); } }
            };
        }
    }
}

