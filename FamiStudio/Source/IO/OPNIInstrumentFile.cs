using System;
using System.Linq;
using System.Text;

namespace FamiStudio
{
    class OPNIInstrumentFile
    {
        public Instrument CreateFromFile(Project project, string filename)
        {
            var bytes = System.IO.File.ReadAllBytes(filename);
            var offset = 12;
            if (bytes.Length == 77) //Version 1
            {
                if (!bytes.Skip(0).Take(11).SequenceEqual(Encoding.ASCII.GetBytes("WOPN2-INST\0")))
                {
                    Log.LogMessage(LogSeverity.Error, "Incompatible file.");
                    return null;
                }
            }
            else if (bytes.Length == 79 || bytes.Length == 83) //Version 2
            {
                offset = offset + 2;
                if (!bytes.Skip(0).Take(11).SequenceEqual(Encoding.ASCII.GetBytes("WOPN2-IN2T\0")))
                {
                    Log.LogMessage(LogSeverity.Error, "Incompatible file.");
                    return null;
                }
            }
            else
            {
                Log.LogMessage(LogSeverity.Error, "Incompatible file.");
                return null;
            }

            var instType = ExpansionType.EPSM;

            if (!project.UsesExpansionAudio(instType))
            {
                Log.LogMessage(LogSeverity.Error, "OPNI import only supported by EPSM expansion.");
                return null;
            }


            var instrumentNameData = bytes.Skip(offset).Take(32).ToArray();
            var instrumentNameDataArray = System.Text.Encoding.ASCII.GetString(instrumentNameData).Split("\0");
            string name = string.IsNullOrEmpty(instrumentNameDataArray[0]) ? "EPSM Instrument" : instrumentNameDataArray[0];

            if (project.GetInstrument(name) != null)
            {
                Log.LogMessage(LogSeverity.Error, $"An instrument named '{name}' already exists.");
                return null;
            }
            var instrument = project.CreateInstrument(instType, name);
            //0xB0, 0xB4, 0x30, 0x40, 0x50, 0x60, 0x70, 0x80, 0x90, 0x38, 0x48, 0x58, 0x68, 0x78, 0x88, 0x98, 0x34, 0x44, 0x54, 0x64, 0x74, 0x84, 0x94, 0x3c, 0x4c, 0x5c, 0x6c, 0x7c, 0x8c, 0x9c, 0x22 -- Register order
            offset = offset + 35;
            instrument.EpsmPatchRegs[0] = (byte)(bytes[offset]);
            offset++;
            //instrument.EpsmPatchRegs[30] = (byte)(bytes[offset] & 0xf); //The OPN2 Bank editor seems to use this for AMS/PMS but the OPNI Specification uses it for LFO Frequency
            offset++;

            for (int i = 0; i < 7; ++i)
            {
                instrument.EpsmPatchRegs[i+2] = (byte)(bytes[offset]);
                offset++;
            }
            for (int i = 0; i < 7; ++i)
            {
                instrument.EpsmPatchRegs[i + 16] = (byte)(bytes[offset]);
                offset++;
            }
            for (int i = 0; i < 7; ++i)
            {
                instrument.EpsmPatchRegs[i + 9] = (byte)(bytes[offset]);
                offset++;
            }
            for (int i = 0; i < 7; ++i)
            {
                instrument.EpsmPatchRegs[i + 23] = (byte)(bytes[offset]);
                offset++;
            }
            project.ConditionalSortInstruments();

            return instrument;
        }
    }
}
