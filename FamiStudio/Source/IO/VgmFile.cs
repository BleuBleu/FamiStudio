using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Diagnostics;

namespace FamiStudio
{
    public class VgmFile
    {
        unsafe struct VgmHeader
        {

            public fixed byte Vgm[4];
            public int eofOffset;
            public int version;
            public int sn76489clock;
            public uint ym2413clock;
            public int gd3Offset;
            public int totalSamples;
            public int loopOffset;
            public int loopSamples;
            public int rate;
            public short sn76489Feedback;
            public byte sn76489RegWidth;
            public byte sn76489Flags;
            public int ym2612clock;
            public int ym2151clock;
            public int vgmDataOffset;
            public int segaPCMClock;
            public int segaPCMReg;
            public int RF5C68clock;
            public int YM2203clock;
            public int YM2608clock;
            public int YM2610clock;
            public int YM3812clock;
            public int YM3526clock;
            public int Y8950clock;
            public int YMF262clock;
            public int YMF278Bclock;
            public int YMF271clock;
            public int YMZ280Bclock;
            public int RF5C164clock;
            public int PWMclock;
            public int AY8910clock;
            public byte AY8910ChipType;
            public byte AY8910Flags;
            public byte YM2203_AY8910Flags;
            public byte YM2608_AY8910Flags;
            public byte volumeMod;
            public byte reserved1;
            public byte loopBase;
            public byte loopModifier;
            public int GameBoyDMGclock;
            public uint NESAPUclock;
            public int MultiPCMclock;
            public int uPD7759clock;
            public int OKIM6258clock;
            public byte OKIM6258Flags;
            public byte K054539Flags;
            public byte C140ChipType;
            public byte reserved2;
            public int OKIM6295clock;
            public int K051649clock;
            public int K054539clock;
            public int HuC6280clock;
            public int C140clock;
            public int K053260clock;
            public int Pokeyclock;
            public int QSoundclock;
            public int SCSPclock;
            public int ExtraHeaderOffset;
        };
        public unsafe static void Save(Song song, string filename, string trackTitle, string gameName, string system, string composer, string releaseDate, string VGMby, string notes, bool smoothLoop)
        {   //This is a mess, works somehow tho
            var project = song.Project.DeepClone();
            song = project.GetSong(song.Id);
            var regPlayer = new RegisterPlayer(project.PalMode, project.OutputsStereoAudio);
            int OGSongLength;
            int numDPCMBanks = (!project.UsesMultipleDPCMBanks &&
            project.GetPackedSampleData(0).Length <= 16384) ?
            1 : project.AutoAssignSamplesBanks(16384, out _);
            var sampleBanks = new Dictionary<int, int>();
            foreach (var sample in project.Samples) sampleBanks.Add(sample.Id, sample.Bank);
            var writes = regPlayer.GetRegisterValues(song, out OGSongLength);
            int TotalLength = 0, IntroLength = 0;
            if (smoothLoop)
            {
                song.ExtendForLooping(2);
                writes = regPlayer.GetRegisterValues(song, out TotalLength);
            }
            bool loopsTwice = false;
            Debug.WriteLine("Writes got, length: " + writes.Length);
            writes = RegisterWriteOptimizer.RemoveExpansionWritesBut(writes, 0x8000
            | ExpansionType.Vrc7Mask | ExpansionType.FdsMask
            | ExpansionType.S5BMask | ExpansionType.EPSMMask);
            if (song.LoopPoint >= 0)
            {
                if (song.LoopPoint > 0)
                {
                    var IntroLengthSong = project.DeepClone().GetSong(song.Id);
                    IntroLengthSong.SetLength(IntroLengthSong.LoopPoint);
                    regPlayer.GetRegisterValues(IntroLengthSong, out IntroLength);
                }
                if (smoothLoop)
                {
                    writes = OptimizeLooping(writes, IntroLength, OGSongLength, out loopsTwice);
                    if (loopsTwice) IntroLength = OGSongLength;
                    else TotalLength = OGSongLength;
                }
                else TotalLength = OGSongLength;
            }
            else TotalLength = OGSongLength;
            Debug.WriteLine("Optimized looping, length: " + writes.Length);

            writes = RegisterWriteOptimizer.OptimizeRegisterWrites(writes);
            Debug.WriteLine("Writes optimized, length: " + writes.Length);
             
            {
                var newWrites = new RegisterWrite[writes.Length+1];
                Array.Copy(writes, newWrites, writes.Length);
                // HACK: Fake register write to insert the wait values at the end
                // Better than adding more of the same code
                newWrites[writes.Length] = new RegisterWrite{Register=0x10000, FrameNumber=TotalLength, Value=0};
                writes = newWrites;
            }

            using (var file = new FileStream(filename, FileMode.Create))
            {
                var header = new VgmHeader();
                header.Vgm[0] = (byte)'V';
                header.Vgm[1] = (byte)'g';
                header.Vgm[2] = (byte)'m';
                header.Vgm[3] = (byte)' ';
                header.version = 0x170;
                uint NESClock;
                byte waitCommand;
                int samplesPerFrame;


                if (project.PalMode)
                {
                    header.rate = 50;
                    NESClock = 1662607;
                    waitCommand = 0x63;
                    samplesPerFrame = 882;
                }
                else
                {
                    header.rate = 60;
                    NESClock = 1789772;
                    waitCommand = 0x62;
                    samplesPerFrame = 735;
                }

                int maxFramesPerWaitCommand = 65535 / samplesPerFrame;

                header.totalSamples = TotalLength * samplesPerFrame;
                // The clock values are purely theoretical for expansions in PAL.
                if (project.UsesVrc7Expansion) header.ym2413clock = 3579545 | 0x80000000;

                if (project.UsesFdsExpansion) header.NESAPUclock = NESClock | 0x80000000;
                else header.NESAPUclock = NESClock;


                if (project.UsesS5BExpansion)
                {
                    header.AY8910clock = (int)NESClock;
                    header.AY8910ChipType = 0x10;           // = YM2149
                    header.AY8910Flags = 0x11;  //Bit 5 indicates clock divider on pin 26 enabled
                }

                if (project.UsesEPSMExpansion) header.YM2608clock = 8000000;

                int extraHeaderSize = (project.UsesVrc7Expansion | project.UsesS5BExpansion | project.UsesEPSMExpansion ? 13 : 0) +
                    (project.UsesVrc7Expansion ? 4 : 0) +
                    (project.UsesS5BExpansion ? 4 : 0) +
                    (project.UsesEPSMExpansion ? 8 : 0);

                int initSize = 24 + (project.UsesEPSMExpansion ? 9 : 0);

                header.vgmDataOffset = 0x8C + extraHeaderSize;
                header.ExtraHeaderOffset = extraHeaderSize != 0 ? 4 : 0;

                string gd3 = "Gd3 ";
                string gd3Data = trackTitle + "\0\0";  //Track Name, Skip Track name (in original (non-English) game language characters)
                gd3Data += gameName + "\0\0"; //Game Name, Skip Game name (in original (non-English) game language characters)
                gd3Data += system + "\0\0"; //System Name, Skip System name (in original (non-English) game language characters)
                gd3Data += composer + "\0\0";//Author Name, Skip Name of Original Track Author (in original (non-English) game characters)
                gd3Data += releaseDate + "\0"; //Date of game's release
                gd3Data += VGMby + "\0";   //VGM Convert Person
                gd3Data += notes + "\0";   //Notes
                int gd3Length = (gd3Data.Length * 2);
                int loopPointFrame = song.LoopPoint;
                var sampleBankPointers = new int[numDPCMBanks];
                var DPCMDataList = new List<byte>();
                for (int i = 0; i < numDPCMBanks; i++)
                {
                    sampleBankPointers[i] = DPCMDataList.Count;
                    DPCMDataList.AddRange(project.GetPackedSampleData(i).ToList());
                }   
                var sampleData = DPCMDataList.ToArray();
                var DPCMUsed = sampleData.Length != 0;
                var writer = new BinaryWriter(file);
                var fileLength = sizeof(VgmHeader) + initSize + extraHeaderSize 
                + 1 - 4 + (DPCMUsed ? (sampleData.Length + ( project.UsesMultipleDPCMBanks ? 7 + GetAmountOfBankswitching(sampleBanks, writes) * 12 : 9 )) : 0); 
                //headerbytes + init bytes - offset (4bytes)  + Extra header + audio stop 1byte
                int frameNumber = 0;
                if (IntroLength == 0) { header.loopOffset = fileLength - 25; }  // Relative pointer difference is 24, and the 1 is the data stop command at the end
                foreach (var reg in writes)
                {
                    while (frameNumber < reg.FrameNumber)
                    {
                        if (reg.FrameNumber - frameNumber >= 3)
                        {
                            fileLength += 3;
                            if (IntroLength <= frameNumber || IntroLength >= reg.FrameNumber || IntroLength >= frameNumber + maxFramesPerWaitCommand)
                            {
                                frameNumber += Utils.Clamp(reg.FrameNumber - frameNumber, 3, maxFramesPerWaitCommand);
                                if (IntroLength == frameNumber)
                                {
                                    header.loopOffset = fileLength - 25;    // Relative pointer difference is 24, the 1 is the data stop command at the end
                                }
                            }
                            else
                            {    // IntroLength sandwiched between frameNumber and reg.FrameNumber
                                frameNumber += Utils.Clamp(IntroLength - frameNumber, 3, maxFramesPerWaitCommand);
                                header.loopOffset = fileLength - 25;    // Relative pointer difference is 24, the 1 is the data stop command at the end
                            }
                        }
                        else
                        {
                            frameNumber++;
                            fileLength++;
                            if (frameNumber == IntroLength) { header.loopOffset = fileLength - 25; }  // Relative pointer difference is 24, and the 1 is the data stop command at the end
                        }
                    }
                    if (reg.Register == NesApu.VRC7_REG_WRITE ||               //If VRC7 write, or
                    reg.Register == NesApu.S5B_DATA ||                         //S5B write, or
                    reg.Register >= NesApu.APU_PL1_VOL && reg.Register < 0x409F &&//2A03/FDS/EPSM write,
                    (reg.Register & 0xFD) != 0x1C) fileLength += 3;        //but not EPSM address
                }
                if (song.LoopPoint >= 0)
                {
                    header.loopBase = loopsTwice ? (byte)1 : (byte)0;
                    header.loopModifier = 0x10;
                    header.loopSamples = (TotalLength - IntroLength) * samplesPerFrame;
                }
                else
                {
                    header.loopBase = 0;
                    header.loopSamples = 0;
                    header.loopOffset = 0;
                    header.loopModifier = 0;
                }
                header.gd3Offset = fileLength - 16;
                fileLength += gd3Length + 12;   //12 bytes for Gd3 header, version and length
                header.eofOffset = fileLength;
                var headerBytes = new byte[sizeof(VgmHeader)];

                Marshal.Copy(new IntPtr(&header), headerBytes, 0, headerBytes.Length);
                writer.Write(headerBytes);

                //Extra header
                if (project.UsesVrc7Expansion | project.UsesS5BExpansion | project.UsesEPSMExpansion)
                {
                    writer.Write(12); //extra header size 12 bytes
                    writer.Write(0); //extra clock offset (0 because there are no extra clocks)
                    writer.Write(4); //extra volume offset

                    writer.Write((byte)((project.UsesVrc7Expansion ? 1 : 0) + (project.UsesS5BExpansion ? 1 : 0) + (project.UsesEPSMExpansion ? 2 : 0)));
                    //Chip amount for volume list

                    if (project.UsesVrc7Expansion)
                    {
                        writer.Write(new byte[] { 0x01, 0x80 }); //chip id YM2413, flags VRC7
                        writer.Write((ushort)0x0800); //volume bit 7 for absolute 8.8 fixed point
                    }
                    if (project.UsesS5BExpansion)
                    {
                        writer.Write(new byte[] { 0x12, 0x00 }); //chip id YM2149, flags
                        writer.Write((ushort)0x8200); //volume bit 7 for absolute 8.8 fixed point
                    }

                    if (project.UsesEPSMExpansion)
                    {
                        writer.Write(new byte[] { 0x07, 0x00 }); //chip id YM2608, flags
                        writer.Write((ushort)0x0140); //volume bit 7 for absolute 8.8 fixed point
                        writer.Write(new byte[] { 0x87, 0x00 }); //chip id YM2608 SSG, flags
                        writer.Write((ushort)0x8140); //volume bit 7 for absolute 8.8 fixed point
                    }
                }
                if (DPCMUsed)
                {
                    //Sample data
                    if (project.UsesMultipleDPCMBanks)
                    {
                        writer.Write(new byte[] { 0x67, 0x66, 0x07 });  //Data block, compat command for older players, type - NES APU DPCM data for further writes
                        writer.Write(sampleData.Length);  //Length of sample data
                    }
                    else
                    {
                        writer.Write(new byte[] { 0x67, 0x66, 0xC2 });  //Data block, compat command for older players, type - NES APU RAM Write
                        writer.Write(sampleData.Length + 2);  //Length of sample data + address
                        writer.Write((ushort)0xC000);   //Address $C000 - the minimum for DPCM data
                    }
                    writer.Write(sampleData);   //Write the sample data
                }
                // Not as lame now
                int addressEPSMA0 = -1;
                int addressEPSMA1 = -1;
                int addressVRC7 = -1;
                int address5B = -1;
                int DPCMBank = -1;

                frameNumber = 0;
                //Inits
                //2A03

                writer.Write(new byte[]
                    {0xB4, 0x15, 0x0F,   //Enable everything but DPCM
                    0xB4, 0x08, 0x80,   //Halt triangle length counter (aka silence triangle)
                    0xB4, 0x0F, 0x00,   //Load noise counter with 0 (aka silence noise)
                    0xB4, 0x00, 0x30,   //Halt pulse 1, pulse 2 and noise length counter and disable hardware envelopes
                    0xB4, 0x04, 0x30,
                    0xB4, 0x0C, 0x30,
                    0xB4, 0x01, 0x08,   //Disable pulse 1 and 2 sweep units 
                    0xB4, 0x05, 0x08});

                //EPSM
                if (project.UsesEPSMExpansion)
                {
                    writer.Write(new byte[]{
                            0x56, 0x29, 0x80,  //Disable IRQs, use 6 channel mode
                            0x56, 0x27, 0x00,  //Disable timers, normal channel 3 mode
                            0x56, 0x11, 0x37});//Max rhythm total volume
                }
                foreach (var reg in writes)
                {
                    while (frameNumber < reg.FrameNumber)
                    {
                        if (reg.FrameNumber - frameNumber >= 3)
                        {
                            if (IntroLength <= frameNumber || IntroLength >= reg.FrameNumber || IntroLength >= frameNumber + maxFramesPerWaitCommand)
                            {

                                writer.Write((byte)0x61);
                                writer.Write((short)(Utils.Clamp(reg.FrameNumber - frameNumber, 3, maxFramesPerWaitCommand) * samplesPerFrame));
                                frameNumber += Utils.Clamp(reg.FrameNumber - frameNumber, 3, maxFramesPerWaitCommand);
                            }
                            else
                            {    // IntroLength sandwiched between frameNumber and reg.FrameNumber
                                writer.Write((byte)0x61);
                                writer.Write((short)(Utils.Clamp(IntroLength - frameNumber, 3, maxFramesPerWaitCommand) * samplesPerFrame));
                                frameNumber += Utils.Clamp(IntroLength - frameNumber, 3, maxFramesPerWaitCommand);
                            }
                        }
                        else
                        {
                            frameNumber++;
                            writer.Write(waitCommand);
                        }
                    }
                    if (reg.Register == NesApu.EPSM_ADDR0)          //EPSM A0 Address
                    {
                        addressEPSMA0 = reg.Value;
                    }
                    else if (reg.Register == NesApu.EPSM_ADDR1)     //EPSM A1 Address
                    {
                        addressEPSMA1 = reg.Value;
                    }
                    else if (reg.Register == NesApu.VRC7_REG_SEL)   //VRC7 Address
                    {
                        addressVRC7 = reg.Value;
                    }
                    else if (reg.Register == NesApu.S5B_ADDR)       //5B Address
                    {
                        address5B = reg.Value;
                    }
                    else if (reg.Register == NesApu.EPSM_DATA0)     //EPSM A0 Data
                    {
                        writer.Write(new byte[] { 0x56, (byte)addressEPSMA0, (byte)reg.Value });
                    }
                    else if (reg.Register == NesApu.EPSM_DATA1)     //EPSM A1 Data
                    {
                        writer.Write(new byte[] { 0x57, (byte)addressEPSMA1, (byte)reg.Value });
                    }
                    else if (reg.Register == NesApu.VRC7_REG_WRITE) //VRC7 Data
                    {
                        writer.Write(new byte[] { 0x51, (byte)addressVRC7, (byte)reg.Value });
                    }
                    else if (reg.Register == NesApu.S5B_DATA)       //5B Data
                    {
                        writer.Write(new byte[] { 0xA0, (byte)address5B, (byte)reg.Value });
                    }
                    else if (reg.Register == NesApu.APU_DMC_START)
                    {
                        if (sampleBanks[reg.Metadata] != DPCMBank && project.UsesMultipleDPCMBanks)
                        {
                            writer.Write(new byte[] { 0x68, 0x66, 0x07 });    //Transfer data block type NES APU RAM write
                            writer.Write(Utils.IntToBytes24Bit(sampleBankPointers[sampleBanks[reg.Metadata]]));
                            writer.Write(new byte[] { 0x00, 0xC0, 0x00, 0x00, 0x40, 0x00 }); //Write 4000 bytes to address C000
                        }
                        DPCMBank = sampleBanks[reg.Metadata];
                        writer.Write(new byte[] { 0xB4, NesApu.APU_DMC_START & 0xFF });
                        writer.Write((byte)(project.GetSampleBankOffset(project.GetSample(reg.Metadata)) >> 6));
                    }
                    else if ((reg.Register < 0x401c) || (reg.Register < 0x409f && reg.Register > 0x401F))   //2A03 & FDS
                    {
                        writer.Write((byte)0xb4);
                        if ((reg.Register <= 0x401F) || (reg.Register <= 0x407f && reg.Register >= 0x4040))
                            writer.Write((byte)(reg.Register & 0xFF));
                        else if (reg.Register >= 0x4080)
                            writer.Write((byte)((reg.Register - 0x60) & 0xFF));
                        else if (reg.Register == 0x4023)
                            writer.Write((byte)0x3F);
                        writer.Write((byte)reg.Value);
                    }
                }
                writer.Write((byte)0x66);   //End data
                writer.Write(gd3.ToCharArray());
                writer.Write(0x100); //version
                writer.Write(gd3Length); //gd3Length
                writer.Write(Encoding.Unicode.GetBytes(gd3Data));
                writer.Flush();
                writer.Close();
            }
        }
        private static int GetAmountOfBankswitching(Dictionary<int, int> sampleBanks, RegisterWrite[] writes)
        {
            int bankInUse = -1;
            int bankswitches = 0;
            foreach (var write in writes)
            {
                if (write.Register == NesApu.APU_DMC_START && sampleBanks[write.Metadata] != bankInUse)
                {
                    bankswitches++;
                    bankInUse = sampleBanks[write.Metadata];
                }
            }
            return bankswitches;
        }

        private static RegisterWrite[] OptimizeLooping(RegisterWrite[] writes, int IntroLength, int LoopFrame, out bool loopsTwice)
        {
            //TODO: determine states at frames instead of comparing writes
            int pointer = 0;
            while (writes[pointer].FrameNumber < IntroLength) pointer++;
            Debug.WriteLine(" === LoopOpt: Loop pointer " + pointer + " at frame " + writes[pointer].FrameNumber);
            int afterLoopPointer = pointer;
            while (writes[afterLoopPointer].FrameNumber < LoopFrame) afterLoopPointer++;
            int finalLoopPointer = afterLoopPointer;
            Debug.WriteLine(" === LoopOpt: Second loop pointer " + afterLoopPointer + " at frame " + writes[afterLoopPointer].FrameNumber);
            int LoopLength = LoopFrame - IntroLength;   //reused a bunch of times
            int frame = IntroLength;
            for (; frame < LoopFrame; frame++)
            {
                while (writes[afterLoopPointer].FrameNumber < frame + LoopLength && afterLoopPointer < writes.Length) afterLoopPointer++;    //Pad afterLoopPointer 
                while (writes[pointer].FrameNumber < frame) pointer++;  //Pad pointer 
                for (; afterLoopPointer < writes.Length && writes[pointer].FrameNumber <= frame && writes[afterLoopPointer].FrameNumber <= frame + LoopFrame; pointer++, afterLoopPointer++)
                {
                    if (!(writes[pointer].Register == writes[afterLoopPointer].Register &&
                    writes[pointer].Value == writes[afterLoopPointer].Value &&
                    ((writes[pointer].Register == NesApu.APU_DMC_START &&
                    writes[pointer].Metadata == writes[afterLoopPointer].Metadata) ||
                    writes[pointer].Register != NesApu.APU_DMC_START)))
                    {
                        Debug.WriteLine($"Dumbass broke at frame {writes[pointer].FrameNumber} at pointer {pointer}/{afterLoopPointer} because \'{writes[pointer].Value:x2} => ${writes[pointer].Register:x4}\' isn't equal to \'{writes[afterLoopPointer].Value:x2} => ${writes[afterLoopPointer].Register:x4}\'");
                        loopsTwice = true;
                        return writes;
                    }
                }
                //Debug.WriteLine(frame.ToString() + " " + pointer.ToString() + " " + afterLoopPointer.ToString());
            }
            loopsTwice = false;
            return writes.Take(finalLoopPointer).ToArray();
        }

        private Song song;
        private Project project;
        private ChannelState[] channelStates;
        private bool preserveDpcmPadding;
        private readonly int[] DPCMOctaveOrder = new[] { 4, 5, 3, 6, 2, 7, 1, 0 };

        int[] apuRegister = new int[0x100];
        int[] apuDecayVolume = new int[0x4];
        int[] apuDecayCounter = new int[0x4];
        float[] apuSubTickCounter = new float[0x4];
        int[] apuSweepPitchOffset = new int[0x2];
        bool isFiveStep;
        int[] vrc7Register = new int[0x100];
        int[] vrc7Trigger = new int[0x6];
        int[] epsmRegisterLo = new int[0x100];
        int[] epsmRegisterHi = new int[0x100];
        int[] epsmFmTrigger = new int[0x6];
        int[] epsmEnvTrigger = new int[0x3];
        int[] epsmFmEnabled = new int[0x6];
        int[] epsmFmKey = new int[0x6];
        int[] epsmFmRegisterOrder = new[] { 0xB0, 0xB4, 0x30, 0x40, 0x50, 0x60, 0x70, 0x80, 0x90, 0x38, 0x48, 0x58, 0x68, 0x78, 0x88, 0x98, 0x34, 0x44, 0x54, 0x64, 0x74, 0x84, 0x94, 0x3c, 0x4c, 0x5c, 0x6c, 0x7c, 0x8c, 0x9c, 0x22 };
        int[] s5bRegister = new int[0x100];
        int[] s5bEnvTrigger = new int[0x3];
        bool dpcmTrigger = false;
        byte[] dpcmData = new byte[0xffff];
        byte[] pcmRAMData = new byte[0xffffff];
        bool ym2149AsEPSM;
        int[] fdsModulationTable = new int[0x40];
        int[] NOISE_FREQ_TABLE = new[] {0x004,0x008,0x010,0x020,0x040,0x060,0x080,0x0A0,0x0CA,0x0FE,0x17C,0x1FC,0x2FA,0x3F8,0x7F2,0xFE4 };
        float[] clockMultiplier = new float[ExpansionType.Count];

        class ChannelState
        {
            public const int Triggered = 1;
            public const int Released = 2;
            public const int Stopped = 0;

            public int period = -1;
            public int note = 0;
            public int pitch = 0;
            public int volume = 15;
            public int octave = -1;
            public int state = Stopped;
            public int dmc = 0;

            public int fdsModDepth = 0;
            public int fdsModSpeed = 0;

            public int s5bEnvFreq = 0;
            public int epsmEnvFreq = 0;

            public bool fmTrigger = false;
            public bool fmSustain = false;

            public Instrument instrument = null;
        };

        public int GetBestMatchingNote(int period, ushort[] noteTable, out int finePitch)
        {
            int bestNote = -1;
            int minDiff = 9999999;

            for (int i = 0; i < noteTable.Length; i++)
            {
                var diff = Math.Abs(noteTable[i] - period);
                if (diff < minDiff)
                {
                    minDiff = diff;
                    bestNote = i;
                }
            }

            finePitch = period - noteTable[bestNote];

            return bestNote;
        }

        private Instrument GetDutyInstrument(Channel channel, int duty)
        {
            var expansion = channel.Expansion;
            var expPrefix = expansion == ExpansionType.None || expansion == ExpansionType.Mmc5 ? "" : ExpansionType.InternalNames[expansion] + " ";
            var name = $"{expPrefix}Duty {duty}";

            var instrument = project.GetInstrument(name);
            if (instrument == null)
            {
                instrument = project.CreateInstrument(expansion, name);
                instrument.Envelopes[EnvelopeType.DutyCycle].Length = 1;
                instrument.Envelopes[EnvelopeType.DutyCycle].Values[0] = (sbyte)duty;
            }

            return instrument;
        }

        private Instrument GetFdsInstrument(sbyte[] wavEnv, sbyte[] modEnv, byte masterVolume)
        {
            foreach (var inst in project.Instruments)
            {
                if (inst.IsFds)
                {
                    if (inst.FdsMasterVolume == masterVolume &&
                        wavEnv.SequenceEqual(inst.Envelopes[EnvelopeType.FdsWaveform].Values.Take(64)) &&
                        modEnv.SequenceEqual(inst.Envelopes[EnvelopeType.FdsModulation].Values.Take(32)))
                    {
                        return inst;
                    }
                }
            }

            for (int i = 1; ; i++)
            {
                var name = $"FDS {i}";
                if (project.IsInstrumentNameUnique(name))
                {
                    var instrument = project.CreateInstrument(ExpansionType.Fds, name);

                    Array.Copy(wavEnv, instrument.Envelopes[EnvelopeType.FdsWaveform].Values, 64);
                    Array.Copy(modEnv, instrument.Envelopes[EnvelopeType.FdsModulation].Values, 32);

                    instrument.FdsMasterVolume = masterVolume;
                    instrument.FdsWavePreset = WavePresetType.Custom;
                    instrument.FdsModPreset = WavePresetType.Custom;

                    return instrument;
                }
            }
        }

        private Instrument GetVrc7Instrument(byte patch, byte[] patchRegs)
        {
            if (patch == Vrc7InstrumentPatch.Custom)
            {
                // Custom instrument, look for a match.
                foreach (var inst in project.Instruments)
                {
                    if (inst.IsVrc7)
                    {
                        if (inst.Vrc7Patch == 0 && inst.Vrc7PatchRegs.SequenceEqual(patchRegs))
                            return inst;
                    }
                }

                for (int i = 1; ; i++)
                {
                    var name = $"VRC7 Custom {i}";
                    if (project.IsInstrumentNameUnique(name))
                    {
                        var instrument = project.CreateInstrument(ExpansionType.Vrc7, name);
                        instrument.Vrc7Patch = patch;
                        Array.Copy(patchRegs, instrument.Vrc7PatchRegs, 8);
                        return instrument;
                    }
                }
            }
            else
            {
                // Built-in patch, simply find by name.
                var name = $"VRC7 {Instrument.GetVrc7PatchName(patch)}";
                var instrument = project.GetInstrument(name);

                if (instrument == null)
                {
                    instrument = project.CreateInstrument(ExpansionType.Vrc7, name);
                    instrument.Vrc7Patch = patch;
                }

                return instrument;
            }
        }

        private Instrument GetS5BInstrument(int noise, int mixer, bool envEnabled, int envShape)
        {
            if (envEnabled)
            {
                if (envShape < 0x4) envShape = 0x9;
                else
                if (envShape < 0x8) envShape = 0xf;

                envShape -= 7;
            }
            else
            {
                envShape = 0;
            }

            var toneEnabled = (mixer & 1) == 0;
            var noiseEnabled = (mixer & 2) == 0;

            var name = "S5B";
            if (toneEnabled)
                name += $" Tone";
            if (noiseEnabled)
                name += $" Noise {noise}";
            if (envShape != 0)
                name += $" Env {envShape + 7:X1}";

            var instrument = project.GetInstrument(name);
            if (instrument == null)
            {
                instrument = project.CreateInstrument(ExpansionType.S5B, name);
                instrument.S5BEnvAutoPitch = false;
                instrument.S5BEnvelopeShape = (byte)envShape;
                instrument.EpsmPatch = 1;
                instrument.Envelopes[EnvelopeType.S5BNoiseFreq].Length = 1;
                instrument.Envelopes[EnvelopeType.S5BNoiseFreq].Values[0] = (sbyte)noise;
                instrument.Envelopes[EnvelopeType.S5BMixer].Length = 1;
                instrument.Envelopes[EnvelopeType.S5BMixer].Values[0] = (sbyte)mixer;
            }

            return instrument;

        }

        private Instrument GetDPCMInstrument()
        {
            var inst = project.GetInstrument($"DPCM Instrument");

            if (inst == null)
                return project.CreateInstrument(ExpansionType.None, "DPCM Instrument");
            if (inst.SamplesMapping.Count < (Note.MusicalNoteMax - Note.MusicalNoteMin + 1))
                return inst;

            for (int i = 1; ; i++)
            {
                inst = project.GetInstrument($"DPCM Instrument {i}");

                if (inst == null)
                    return project.CreateInstrument(ExpansionType.None, $"DPCM Instrument {i}");
                if (inst.SamplesMapping.Count < (Note.MusicalNoteMax - Note.MusicalNoteMin + 1))
                    return inst;
            }
        }

        private Instrument GetEPSMInstrument(byte chanType, byte[] patchRegs, int noise, int mixer, bool envEnabled, int envShape)
        {
            var name = $"EPSM {Instrument.GetEpsmPatchName(1)}";
            var instrument = project.GetInstrument(name);
            var stereo = "";
            if ((patchRegs[1] & 0xC0) == 0x80)
                stereo = " Left";
            if ((patchRegs[1] & 0xC0) == 0x40)
                stereo = " Right";
            if ((patchRegs[1] & 0xC0) == 0x00 && chanType == 2)
                stereo = " Stop";
            if ((patchRegs[1] & 0xC0) == 0x00 && chanType != 2)
                patchRegs[1] = 0xC0;

            if (chanType == 0)
            {
                if (envEnabled)
                {
                    if (envShape < 0x4) envShape = 0x9;
                    else
                    if (envShape < 0x8) envShape = 0xf;

                    envShape -= 7;
                }
                else
                {
                    envShape = 0;
                }

                var toneEnabled = (mixer & 1) == 0;
                var noiseEnabled = (mixer & 2) == 0;

                name = "EPSM";
                if (noiseEnabled && noise == 0)
                    noise = 1;
                if (toneEnabled)
                    name += $" Tone";
                if (noiseEnabled)
                    name += $" Noise {noise}";
                if (envShape != 0)
                    name += $" Env {envShape + 7:X1}";

                instrument = project.GetInstrument(name);
                if (instrument == null)
                {
                    instrument = project.CreateInstrument(ExpansionType.EPSM, name);
                    instrument.EPSMSquareEnvAutoPitch = false;
                    instrument.EPSMSquareEnvelopeShape = (byte)envShape;
                    instrument.EpsmPatch = 1;
                    instrument.Envelopes[EnvelopeType.S5BNoiseFreq].Length = 1;
                    instrument.Envelopes[EnvelopeType.S5BNoiseFreq].Values[0] = (sbyte)noise;
                    instrument.Envelopes[EnvelopeType.S5BMixer].Length = 1;
                    instrument.Envelopes[EnvelopeType.S5BMixer].Values[0] = (sbyte)mixer;
                }

                return instrument;
            }

            if (chanType == 2)
            {
                name = $"EPSM Drum{stereo}";
                instrument = project.GetInstrument(name);
                if (instrument == null)
                {
                    instrument = project.CreateInstrument(ExpansionType.EPSM, name);

                    instrument.EpsmPatch = 0;
                    Array.Copy(EpsmInstrumentPatch.Infos[EpsmInstrumentPatch.Default].data, instrument.EpsmPatchRegs, 31);
                    instrument.EpsmPatchRegs[1] = patchRegs[1];
                }
                return instrument;
            }

            if (instrument == null)
            {
                instrument = project.CreateInstrument(ExpansionType.EPSM, name);

                instrument.EpsmPatch = 1;
                if (instrument.EpsmPatchRegs.SequenceEqual(patchRegs))
                    return instrument;
            }

            foreach (var inst in project.Instruments)
            {
                if (inst.IsEpsm)
                {
                    if (inst.EpsmPatchRegs.SequenceEqual(patchRegs))
                        return inst;
                }
            }

            for (int i = 1; ; i++)
            {
                name = $"EPSM Custom{stereo} {i}";
                if (project.IsInstrumentNameUnique(name))
                {
                    instrument = project.CreateInstrument(ExpansionType.EPSM, name);
                    instrument.EpsmPatch = 0;
                    Array.Copy(patchRegs, instrument.EpsmPatchRegs, 31);
                    return instrument;
                }
            }
        }


        private int GetState(int channel, int state, int sub)
        {
            switch (channel)
            {
                case ChannelType.Square1:
                case ChannelType.Square2:
                    {
                        switch (state)
                        {
                            case NotSoFatso.STATE_PERIOD: return (int)apuRegister[2 + (channel * 4)] + (int)((apuRegister[3 + (channel * 4)] & 0x7) << 8);
                            case NotSoFatso.STATE_DUTYCYCLE: return (int)(apuRegister[(channel * 4)] & 0xc0) >> 6;
                            //case NotSoFatso.STATE_VOLUME: return mWave_Squares.nLengthCount[channel] && mWave_Squares.bChannelEnabled[channel] ? mWave_Squares.nVolume[channel] : 0;
                            case NotSoFatso.STATE_VOLUME: return (apuRegister[(channel * 4)] & 0xf);
                        }
                        break;
                    }
                case ChannelType.Triangle:
                    {
                        switch (state)
                        {
                            case NotSoFatso.STATE_PERIOD: return (int)apuRegister[2 + (channel * 4)] + (int)((apuRegister[3 + (channel * 4)] & 0x7) << 8);
                            //case NotSoFatso.STATE_VOLUME: return mWave_TND.nTriLengthCount && mWave_TND.bTriChannelEnabled ? mWave_TND.nTriLinearCount : 0;
                            case NotSoFatso.STATE_VOLUME: return (apuRegister[(channel * 4)] & 0xf);
                        }
                        break;
                    }
                case ChannelType.Noise:
                    {
                        switch (state)
                        {
                            case NotSoFatso.STATE_VOLUME:
                                {
                                    var reg400C  = apuRegister[0x0C];
                                    var constant = (reg400C & 0x10) != 0;
                                    var loop     = (reg400C & 0x20) != 0;
                                    var volume   =  reg400C & 0x0F;
                                    var stepSize = isFiveStep ? 3.2f : 4.0f;

                                    if (!constant)
                                    {
                                        // Multiple steps to simulate 192Hz or 240Hz.
                                        apuSubTickCounter[channel] += stepSize;

                                        while (apuSubTickCounter[channel] >= stepSize)
                                        {
                                            apuSubTickCounter[channel] -= 1.0f;

                                            if (apuDecayCounter[channel] > 0)
                                                apuDecayCounter[channel]--;
                                            else
                                            {
                                                apuDecayCounter[channel] = volume;

                                                if (apuDecayVolume[channel] > 0)
                                                    apuDecayVolume[channel]--;
                                                else if (loop)
                                                    apuDecayVolume[channel] = 15;
                                            }
                                        }

                                        return apuDecayVolume[channel];
                                    }
                                    else
                                    {
                                        return volume;
                                    }
                                }

                            //case NotSoFatso.STATE_VOLUME: return mWave_TND.nNoiseLengthCount && mWave_TND.bNoiseChannelEnabled ? mWave_TND.nNoiseVolume : 0;
                            case NotSoFatso.STATE_DUTYCYCLE: return (apuRegister[0x0e] & 0x80) >> 8;
                            case NotSoFatso.STATE_PERIOD: return apuRegister[0x0e] & 0xf;
                        }
                        break;
                    }
                case ChannelType.Dpcm:
                    {
                        switch (state)
                        {
                            case NotSoFatso.STATE_DPCMSAMPLELENGTH:
                                {
                                    if (dpcmTrigger)
                                    {
#if DEBUG
                                        Log.LogMessage(LogSeverity.Info, "samplelength: " + (apuRegister[0x13] << 4) + " sampladdr: " + (apuRegister[0x12] << 6));
#endif
                                        dpcmTrigger = false;

                                        return (apuRegister[0x13] << 4) + 1;
                                    }
                                    else
                                    {
                                        return 0;
                                    }
                                }
                            case NotSoFatso.STATE_DPCMSAMPLEADDR:
                                {
                                    return apuRegister[0x12];
                                }
                            case NotSoFatso.STATE_DPCMLOOP:
                                {
                                    return apuRegister[0x10] & 0x40;
                                }
                            case NotSoFatso.STATE_DPCMPITCH:
                                {
                                    return apuRegister[0x10] & 0x0f;
                                }
                            case NotSoFatso.STATE_DPCMSAMPLEDATA:
                                {
                                    return dpcmData[((apuRegister[0x12] << 6)) + sub];
                                }
                            case NotSoFatso.STATE_DPCMCOUNTER:
                                {
                                    return 0;// mWave_TND.bDMCLastDeltaWrite;
                                }
                            case NotSoFatso.STATE_DPCMACTIVE:
                                {
                                    return apuRegister[0x15] & 0x10;
                                }
                        }
                        break;
                    }

                case ChannelType.FdsWave:
                {
                    switch (state)
                    {
                        case NotSoFatso.STATE_PERIOD: return apuRegister[0x22] + ((apuRegister[0x23] & 0x0F) << 8);
                        case NotSoFatso.STATE_VOLUME: return apuRegister[0x20] & 0x3F;
                        case NotSoFatso.STATE_FDSWAVETABLE: return apuRegister[0x40 + sub];
                        case NotSoFatso.STATE_FDSMODULATIONTABLE: return fdsModulationTable[sub * 2];
                        case NotSoFatso.STATE_FDSMODULATIONDEPTH: return apuRegister[0x24] & 0x3F;
                        case NotSoFatso.STATE_FDSMODULATIONSPEED: return apuRegister[0x26] + ((apuRegister[0x27] & 0x0F) << 8);
                        case NotSoFatso.STATE_FDSMASTERVOLUME: return apuRegister[0x29] & 0x03;
                    }
                    break;
                }

                case ChannelType.Vrc7Fm1:
                case ChannelType.Vrc7Fm2:
                case ChannelType.Vrc7Fm3:
                case ChannelType.Vrc7Fm4:
                case ChannelType.Vrc7Fm5:
                case ChannelType.Vrc7Fm6:
                    {
                        int idx = channel - ChannelType.Vrc7Fm1;
                        switch (state)
                        {
                            case NotSoFatso.STATE_PERIOD: return ((vrc7Register[0x20 + idx] & 1) << 8) | (vrc7Register[0x10 + idx]);
                            case NotSoFatso.STATE_VOLUME: return (vrc7Register[0x30 + idx] >> 0) & 0xF;
                            case NotSoFatso.STATE_VRC7PATCH: return (vrc7Register[0x30 + idx] >> 4) & 0xF;
                            case NotSoFatso.STATE_FMPATCHREG: return (vrc7Register[sub]);
                            case NotSoFatso.STATE_FMOCTAVE: return (vrc7Register[0x20 + idx] >> 1) & 0x07;
                            case NotSoFatso.STATE_FMTRIGGER: return (vrc7Register[0x20 + idx] >> 4) & 0x01;
                            case NotSoFatso.STATE_FMTRIGGERCHANGE:
                                int trigger = vrc7Trigger[idx];
                                vrc7Trigger[idx] = 0;
                                return trigger;
                            case NotSoFatso.STATE_FMSUSTAIN: return (vrc7Register[0x20 + idx] >> 5) & 0x01;
                        }
                        break;
                    }
                //###############################################################
                //
                // MMC5 could be used for square channels for dual apu VGM's
                //
                //###############################################################
                /*
            case ChannelType.Mmc5Square1:
            case ChannelType.Mmc5Square2:
                {
                    int idx = channel - ChannelType.Mmc5Square1;
                    switch (state)
                    {
                        case NotSoFatso.STATE_PERIOD: return mWave_MMC5Square[idx].nFreqTimer.W;
                        case NotSoFatso.STATE_DUTYCYCLE: return IndexOf(DUTY_CYCLE_TABLE, 4, mWave_MMC5Square[idx].nDutyCycle);
                        case NotSoFatso.STATE_VOLUME: return mWave_MMC5Square[idx].nLengthCount && mWave_MMC5Square[idx].bChannelEnabled ? mWave_MMC5Square[idx].nVolume : 0;
                    }
                    break;
                }*/
                case ChannelType.S5BSquare1:
                case ChannelType.S5BSquare2:
                case ChannelType.S5BSquare3:
                    {
                        int idx = channel - ChannelType.S5BSquare1;
                        switch (state)
                        {
                            case NotSoFatso.STATE_PERIOD: return s5bRegister[0 + idx * 2] | (s5bRegister[1 + idx * 2] << 8);
                            case NotSoFatso.STATE_VOLUME: return (((s5bRegister[7] >> idx) & 9) != 9) || ((s5bRegister[0x8 + idx] & 0x10) != 0) ? s5bRegister[8 + idx] : 0;
                            case NotSoFatso.STATE_S5BMIXER: return ((s5bRegister[7] >> idx) & 9);
                            case NotSoFatso.STATE_S5BNOISEFREQUENCY: return s5bRegister[6];
                            case NotSoFatso.STATE_S5BENVFREQUENCY: return s5bRegister[0xB] | (s5bRegister[0xC] << 8);
                            case NotSoFatso.STATE_S5BENVSHAPE: return s5bRegister[0xD] & 0xF;
                            case NotSoFatso.STATE_S5BENVTRIGGER:
                                int trigger = s5bEnvTrigger[idx];
                                s5bEnvTrigger[idx] = 0;
                                return trigger;
                            case NotSoFatso.STATE_S5BENVENABLED: return (s5bRegister[0x8 + idx] & 0x10);
                        }
                        break;
                    }

                case ChannelType.EPSMrythm1:
                case ChannelType.EPSMrythm2:
                case ChannelType.EPSMrythm3:
                case ChannelType.EPSMrythm4:
                case ChannelType.EPSMrythm5:
                case ChannelType.EPSMrythm6:
                    {
                        int idx = channel - ChannelType.EPSMrythm1;
                        switch (state)
                        {
                            case NotSoFatso.STATE_STEREO: return (epsmRegisterLo[0x18 + idx] & 0xc0);
                            case NotSoFatso.STATE_PERIOD: return 0xc20;
                            case NotSoFatso.STATE_VOLUME:
                                int returnval = (epsmRegisterLo[0x10] & (1 << idx)) != 0 ? ((epsmRegisterLo[0x18 + idx] & 0x1f) >> 1) : 0;
                                epsmRegisterLo[0x10] = ~(~epsmRegisterLo[0x10] | 1 << idx);
                                return returnval;
                        }
                        break;
                    }
                case ChannelType.EPSMSquare1:
                case ChannelType.EPSMSquare2:
                case ChannelType.EPSMSquare3:
                    {
                        int idx = channel - ChannelType.EPSMSquare1;
                        switch (state)
                        {
                            case NotSoFatso.STATE_PERIOD: return epsmRegisterLo[0 + idx * 2] | (epsmRegisterLo[1 + idx * 2] << 8);
                            case NotSoFatso.STATE_VOLUME: return (((epsmRegisterLo[7] >> idx) & 9) != 9) || ((epsmRegisterLo[0x8 + idx] & 0x10) != 0) ? epsmRegisterLo[8 + idx] : 0;
                            case NotSoFatso.STATE_S5BMIXER: return ((epsmRegisterLo[7] >> idx) & 9);
                            case NotSoFatso.STATE_S5BNOISEFREQUENCY: return epsmRegisterLo[6];
                            case NotSoFatso.STATE_S5BENVFREQUENCY: return epsmRegisterLo[0xB] | (epsmRegisterLo[0xC] << 8);
                            case NotSoFatso.STATE_S5BENVSHAPE: return epsmRegisterLo[0xD] & 0xF;
                            case NotSoFatso.STATE_S5BENVTRIGGER:
                                int trigger = epsmEnvTrigger[idx];
                                epsmEnvTrigger[idx] = 0;
                                return trigger;
                            case NotSoFatso.STATE_S5BENVENABLED: return (epsmRegisterLo[0x8 + idx] & 0x10);
                        }
                        break;
                    }
                case ChannelType.EPSMFm1:
                case ChannelType.EPSMFm2:
                case ChannelType.EPSMFm3:
                case ChannelType.EPSMFm4:
                case ChannelType.EPSMFm5:
                case ChannelType.EPSMFm6:
                    {
                        int idx = channel - ChannelType.EPSMFm1;
                        switch (state)
                        {
                            case NotSoFatso.STATE_FMTRIGGER:
                                {
                                    int trigger = epsmFmTrigger[idx];
                                    epsmFmTrigger[idx] = 0;
                                    return trigger;
                                }
                            case NotSoFatso.STATE_FMOCTAVE:
                                if (idx < 3)
                                    return (epsmRegisterLo[0xa4 + idx] >> 3) & 0x07;
                                else
                                    return (epsmRegisterHi[0xa4 + idx - 3] >> 3) & 0x07;
                            case NotSoFatso.STATE_PERIOD:
                                if (idx < 3)
                                    return (epsmRegisterLo[0xa0 + idx] + ((epsmRegisterLo[0xa4 + idx] & 7) << 8)) / 4;
                                else
                                    return (epsmRegisterHi[0xa0 + idx - 3] + ((epsmRegisterHi[0xa4 + idx - 3] & 7) << 8)) / 4;
                            case NotSoFatso.STATE_VOLUME:
                                if (idx < 3)
                                    return (epsmRegisterLo[0xb4 + idx] & 0xc0) > 0 ? 15 : 0;
                                else
                                    return (epsmRegisterHi[0xb4 + idx - 3] & 0xc0) > 0 ? 15 : 0;
                            case NotSoFatso.STATE_FMSUSTAIN: return epsmFmEnabled[idx] > 0 ? 1 : 0;
                            case NotSoFatso.STATE_FMPATCHREG:
                                int returnval = idx < 3 ? epsmRegisterLo[epsmFmRegisterOrder[sub] + idx] : epsmRegisterHi[epsmFmRegisterOrder[sub] + idx - 3];
                                if (sub == 3 && (epsmFmKey[idx] & 0x10) == 0)
                                {
                                    returnval = 0x7f;
                                }
                                if (sub == 10 && (epsmFmKey[idx] & 0x20) == 0)
                                {
                                    returnval = 0x7f;
                                }
                                if (sub == 17 && (epsmFmKey[idx] & 0x40) == 0)
                                {
                                    returnval = 0x7f;
                                }
                                if (sub == 24 && (epsmFmKey[idx] & 0x80) == 0)
                                {
                                    returnval = 0x7f;
                                }
                                return returnval;
                        }
                        break;
                    }
            }

            return 0;
        }



        private bool UpdateChannel(int p, int n, Channel channel, ChannelState state)
        {
            var project = channel.Song.Project;
            var hasNote = false;

            if (channel.Type == ChannelType.Dpcm)
            {
                var dmc = GetState(channel.Type, NotSoFatso.STATE_DPCMCOUNTER, 0);
                var len = GetState(channel.Type, NotSoFatso.STATE_DPCMSAMPLELENGTH, 0);
                var dmcActive = GetState(channel.Type, NotSoFatso.STATE_DPCMACTIVE, 0);

                if (len > 0)
                {
                    // Subtracting one here is not correct. But it is a fact that a lot of games
                    // seemed to favor tight sample packing and did not care about playing one
                    // extra sample of garbage.
                    if (!preserveDpcmPadding)
                    {
                        Debug.Assert((len & 0xf) == 1);
                        len--;
                        Debug.Assert((len & 0xf) == 0);
                    }
                    var sampleData = new byte[len];
                    for (int i = 0; i < len; i++)
                        sampleData[i] = (byte)GetState(channel.Type, NotSoFatso.STATE_DPCMSAMPLEDATA, i);

                    var sample = project.FindMatchingSample(sampleData);
                    if (sample == null)
                        sample = project.CreateDPCMSampleFromDmcData($"Sample {project.Samples.Count + 1}", sampleData);

                    var loop = GetState(channel.Type, NotSoFatso.STATE_DPCMLOOP, 0) != 0;
                    var pitch = GetState(channel.Type, NotSoFatso.STATE_DPCMPITCH, 0);
                    var noteValue = -1;
                    var dpcmInst = (Instrument)null;

                    foreach (var inst in project.Instruments)
                    {
                        if (inst.HasAnyMappedSamples)
                        {
                            noteValue = inst.FindDPCMSampleMapping(sample, pitch, loop);
                            if (noteValue >= 0)
                            {
                                dpcmInst = inst;
                                break;
                            }
                        }
                    }

                    if (noteValue < 0)
                    {
                        dpcmInst = GetDPCMInstrument();

                        var found = false;
                        foreach (var o in DPCMOctaveOrder)
                        {
                            for (var i = 0; i < 12; i++)
                            {
                                noteValue = o * 12 + i + 1;
                                if (dpcmInst.GetDPCMMapping(noteValue) == null)
                                {
                                    found = true;
                                    break;
                                }
                            }

                            if (found)
                                break;
                        }

                        Debug.Assert(found);
                        dpcmInst.MapDPCMSample(noteValue, sample, pitch, loop);
                    }

                    if (Note.IsMusicalNote(noteValue))
                    {
                        var note = channel.GetOrCreatePattern(p).GetOrCreateNoteAt(n);
                        note.Value = (byte)noteValue;
                        note.Instrument = dpcmInst;
                        if (state.dmc != dmc)
                        {
                            note.DeltaCounter = (byte)dmc;
                            state.dmc = dmc;
                        }
                        hasNote = true;
                        state.state = ChannelState.Triggered;
                    }
                }
                else if (dmc != state.dmc)
                {
                    channel.GetOrCreatePattern(p).GetOrCreateNoteAt(n).DeltaCounter = (byte)dmc;
                    state.dmc = dmc;
                }

                if (dmcActive == 0 && state.state == ChannelState.Triggered)
                {
                    channel.GetOrCreatePattern(p).GetOrCreateNoteAt(n).IsStop = true;
                    state.state = ChannelState.Stopped;
                }
            }
            else if (channel.Type != ChannelType.Dpcm)
            {
                var period = GetState(channel.Type, NotSoFatso.STATE_PERIOD, 0);
                var volume = GetState(channel.Type, NotSoFatso.STATE_VOLUME, 0);
                var duty = GetState(channel.Type, NotSoFatso.STATE_DUTYCYCLE, 0);
                var force = false;
                var stop = false;
                var release = false;
                var attack = true;
                var octave = -1;

                if (channel.Type == ChannelType.FdsWave)
                {
                    volume = Math.Min(Note.VolumeMax, volume >> 1);
                }
                else if (channel.Type >= ChannelType.Vrc7Fm1 && channel.Type <= ChannelType.Vrc7Fm6)
                {
                    volume = 15 - volume;
                }

                var hasOctave = channel.IsVrc7Channel || channel.IsEPSMFmChannel;
                var hasVolume = channel.Type != ChannelType.Triangle;
                var hasPitch = channel.Type != ChannelType.Noise && !channel.IsEPSMRythmChannel;
                var hasDuty = channel.Type == ChannelType.Square1 || channel.Type == ChannelType.Square2 || channel.Type == ChannelType.Noise ||  channel.Type == ChannelType.Mmc5Square1 || channel.Type == ChannelType.Mmc5Square2;
                var hasTrigger = channel.IsVrc7Channel;

                if (channel.Type >= ChannelType.Vrc7Fm1 && channel.Type <= ChannelType.Vrc7Fm6)
                {
                    var trigger = GetState(channel.Type, NotSoFatso.STATE_FMTRIGGER, 0) != 0;
                    var sustain = GetState(channel.Type, NotSoFatso.STATE_FMSUSTAIN, 0) != 0;
                    var triggerChange = GetState(channel.Type, NotSoFatso.STATE_FMTRIGGERCHANGE, 0);

                    var newState = state.state;

                    if (!state.fmTrigger && trigger)
                        newState = ChannelState.Triggered;
                    else if (state.fmTrigger && !trigger && sustain)
                        newState = ChannelState.Released;
                    else if (!trigger && !sustain)
                        newState = ChannelState.Stopped;

                    if (newState != state.state || triggerChange > 0)
                    {
                        stop = newState == ChannelState.Stopped;
                        release = newState == ChannelState.Released;
                        state.state = newState;
                        force |= true;
                    }
                    else
                    {
                        attack = false;
                    }

                    octave = GetState(channel.Type, NotSoFatso.STATE_FMOCTAVE, 0);

                    state.fmTrigger = trigger;
                    state.fmSustain = sustain;
                }
                else if (channel.Type >= ChannelType.EPSMFm1 && channel.Type <= ChannelType.EPSMFm6)
                {
                    var trigger = GetState(channel.Type, NotSoFatso.STATE_FMTRIGGER, 0) != 0;
                    var sustain = GetState(channel.Type, NotSoFatso.STATE_FMSUSTAIN, 0) > 0;
                    var stopped = GetState(channel.Type, NotSoFatso.STATE_VOLUME, 0) == 0;

                    var newState = state.state;

                    if (!trigger)
                        attack = false;

                    if (!state.fmTrigger && trigger)
                        newState = ChannelState.Triggered;
                    else
                        newState = sustain ? ChannelState.Triggered : (stopped ? ChannelState.Stopped : ChannelState.Released);

                    if (newState != state.state || trigger)
                    {
                        stop = newState == ChannelState.Stopped;
                        if (!trigger)
                            release = newState == ChannelState.Released;
                        state.state = newState;
                        force |= true;
                    }

                    octave = GetState(channel.Type, NotSoFatso.STATE_FMOCTAVE, 0);

                    state.fmTrigger = trigger;
                    state.fmSustain = sustain;
                }
                else
                {
                    var newState = volume != 0 && (channel.Type == ChannelType.Noise || period != 0) ? ChannelState.Triggered : ChannelState.Stopped;

                    if (newState != state.state)
                    {
                        stop = newState == ChannelState.Stopped;
                        force |= true;
                        state.state = newState;
                    }
                }

                if (hasVolume)
                {
                    if (state.volume != volume && (volume != 0 || hasTrigger))
                    {
                        var pattern = channel.GetOrCreatePattern(p).GetOrCreateNoteAt(n).Volume = (byte)volume;
                        state.volume = volume;
                    }
                }

                Instrument instrument = null;

                if (hasDuty)
                {
                    instrument = GetDutyInstrument(channel, duty);
                }
                else if (channel.Type == ChannelType.FdsWave)
                {
                    var wavEnv = new sbyte[64];
                    var modEnv = new sbyte[32];

                    for (int i = 0; i < 64; i++)
                        wavEnv[i] = (sbyte)(GetState(channel.Type, NotSoFatso.STATE_FDSWAVETABLE, i) & 0x3f);
                    for (int i = 0; i < 32; i++)
                        modEnv[i] = (sbyte)(GetState(channel.Type, NotSoFatso.STATE_FDSMODULATIONTABLE, i));

                    Envelope.ConvertFdsModulationToAbsolute(modEnv);

                    var masterVolume = (byte)GetState(channel.Type, NotSoFatso.STATE_FDSMASTERVOLUME, 0);

                    instrument = GetFdsInstrument(wavEnv, modEnv, masterVolume);

                    int modDepth = GetState(channel.Type, NotSoFatso.STATE_FDSMODULATIONDEPTH, 0);
                    int modSpeed = GetState(channel.Type, NotSoFatso.STATE_FDSMODULATIONSPEED, 0);

                    if (state.fdsModDepth != modDepth)
                    {
                        var pattern = channel.GetOrCreatePattern(p).GetOrCreateNoteAt(n).FdsModDepth = (byte)modDepth;
                        state.fdsModDepth = modDepth;
                    }

                    if (state.fdsModSpeed != modSpeed)
                    {
                        var pattern = channel.GetOrCreatePattern(p).GetOrCreateNoteAt(n).FdsModSpeed = (ushort)modSpeed;
                        state.fdsModSpeed = modSpeed;
                    }
                }
                else if (channel.Type >= ChannelType.Vrc7Fm1 &&
                         channel.Type <= ChannelType.Vrc7Fm6)
                {
                    var patch = (byte)GetState(channel.Type, NotSoFatso.STATE_VRC7PATCH, 0);
                    var regs = new byte[8];

                    if (patch == 0)
                    {
                        for (int i = 0; i < 8; i++)
                            regs[i] = (byte)GetState(channel.Type, NotSoFatso.STATE_FMPATCHREG, i);
                    }

                    instrument = GetVrc7Instrument(patch, regs);
                }
                else if (channel.Type >= ChannelType.S5BSquare1 && channel.Type <= ChannelType.S5BSquare3)
                {
                    var mixer = (int)GetState(channel.Type, NotSoFatso.STATE_S5BMIXER, 0);
                    var noiseFreq = (byte)Utils.Clamp((GetState(channel.Type, NotSoFatso.STATE_S5BNOISEFREQUENCY, 0) & 0x1f) / clockMultiplier[channel.Expansion], 1, 31);
                    var envEnabled = (int)GetState(channel.Type, NotSoFatso.STATE_S5BENVENABLED, 0) != 0;
                    var envShape = (int)GetState(channel.Type, NotSoFatso.STATE_S5BENVSHAPE, 0);
                    var envTrigger = (int)GetState(channel.Type, NotSoFatso.STATE_S5BENVTRIGGER, 0);
                    mixer = (mixer & 0x1) + ((mixer & 0x8) >> 2);
                    instrument = GetS5BInstrument(noiseFreq, mixer, envEnabled, envShape);
                    if (envEnabled)
                    {
                        if (envTrigger != 0)
                            force = true;
                        else
                            attack = false;
                    }
                }
                else if (channel.Type >= ChannelType.EPSMSquare1 && channel.Type <= ChannelType.EPSMrythm6)
                {
                    var regs = new byte[31];
                    Array.Clear(regs, 0, regs.Length);
                    if (channel.Type >= ChannelType.EPSMFm1 && channel.Type <= ChannelType.EPSMFm6)
                    {
                        for (int i = 0; i < 31; i++)
                            regs[i] = (byte)GetState(channel.Type, NotSoFatso.STATE_FMPATCHREG, i);

                        instrument = GetEPSMInstrument(1, regs, 0, 0, false, 0);
                    }
                    else if (channel.Type >= ChannelType.EPSMrythm1 && channel.Type <= ChannelType.EPSMrythm6)
                    {
                        regs[1] = (byte)GetState(channel.Type, NotSoFatso.STATE_STEREO, 0);
                        instrument = GetEPSMInstrument(2, regs, 0, 0, false, 0);
                    }
                    else
                    {
                        var mixer = (int)GetState(channel.Type, NotSoFatso.STATE_S5BMIXER, 0);
                        int noiseFreq;
                        if (ym2149AsEPSM && channel.IsEPSMSquareChannel)
                            noiseFreq = (byte)Utils.Clamp((GetState(channel.Type, NotSoFatso.STATE_S5BNOISEFREQUENCY, 0) & 0x1f) / clockMultiplier[ExpansionType.S5B], 1, 31);
                        else
                            noiseFreq = (byte)Utils.Clamp((GetState(channel.Type, NotSoFatso.STATE_S5BNOISEFREQUENCY, 0) & 0x1f) / clockMultiplier[channel.Expansion], 1, 31);
                        var envEnabled = (int)GetState(channel.Type, NotSoFatso.STATE_S5BENVENABLED, 0) != 0;
                        var envShape = (int)GetState(channel.Type, NotSoFatso.STATE_S5BENVSHAPE, 0);
                        var envTrigger = (int)GetState(channel.Type, NotSoFatso.STATE_S5BENVTRIGGER, 0);
                        mixer = (mixer & 0x1) + ((mixer & 0x8) >> 2);
                        instrument = GetEPSMInstrument(0, regs, noiseFreq, mixer, envEnabled, envShape);
                        if (envEnabled)
                        {
                            if (envTrigger != 0)
                                force = true;
                            else
                                attack = false;
                        }
                    }

                }
                else
                {
                    instrument = GetDutyInstrument(channel, 0);
                }

                if(channel.IsEPSMFmChannel || channel.IsVrc7Channel)
                    period = (int)(period * clockMultiplier[channel.Expansion]);
                else if(!channel.IsEPSMRythmChannel)
                    period = (int)(period / clockMultiplier[channel.Expansion]);
                if(ym2149AsEPSM && channel.IsEPSMSquareChannel)
                    period = (int)(period / clockMultiplier[ExpansionType.S5B]);

                var hasNoteWithAttack = false;

                // 2A03 sweep.
                if (channel.Type == ChannelType.Square1 || channel.Type == ChannelType.Square2)
                {
                    var ch = channel.Index;
                    var offset = ch == 0 ? 0x01 : 0x05;
                    var sweepReg = apuRegister[offset];

                    var sweepEnable = (sweepReg & 0x80) != 0;
                    var sweepShift  =  sweepReg & 0x07;
                    var sweepTimer  = (sweepReg & 0x70) >> 4;
                    var sweepNegate = (sweepReg & 0x08) != 0;
                    var stepSize    = (sweepTimer + 1) * (isFiveStep ? 1.6f : 2.0f);

                    if (sweepEnable && sweepShift != 0)
                    {
                        apuSubTickCounter[ch] += stepSize;

                        while (apuSubTickCounter[ch] >= 1.0f)
                        {
                            apuSubTickCounter[ch] -= 1.0f;

                            var sweepDelta = (period + apuSweepPitchOffset[ch]) >> sweepShift;

                            if (sweepNegate)
                                sweepDelta = (ch == 0) ? -sweepDelta - 1 : -sweepDelta;

                            apuSweepPitchOffset[ch] += sweepDelta;
                        }

                        var target = period + apuSweepPitchOffset[ch];
                        if (target >= 8 && target <= 0x7FF)
                            period = target;
                        else
                            state.volume = 0;
                    }
                    else
                    {
                        apuSweepPitchOffset[ch] = 0;
                    }
                }

                if ((state.period != period) || (hasOctave && state.octave != octave) || (instrument != state.instrument) || force)
                {
                    var noteTable = NesApu.GetNoteTableForChannelType(channel.Type, project.PalMode, project.ExpansionNumN163Channels, project.Tuning);
                    var note = release ? Note.NoteRelease : (stop ? Note.NoteStop : state.note);
                    var finePitch = 0;

                    if (!stop && !release && state.state != ChannelState.Stopped)
                    {
                        if (channel.Type == ChannelType.Noise)
                            note = (period ^ 0x0f) + 32;
                        else
                        {
                            // S5B is offset by -1 vs 2A03/2A07 tables.
                            if (channel.IsS5BChannel)
                                period -= 1;
                                
                            note = (byte)GetBestMatchingNote(period, noteTable, out finePitch);
                        }

                        if (hasOctave)
                        {
                            period *= (1 << octave);
                            while (note > 12)
                            {
                                note -= 12;
                                octave++;
                            }
                            note += octave * 12;
                            note = Math.Min(note, noteTable.Length - 1);
                            finePitch = period - noteTable[note];
                        }
                    }

                    if (note < Note.MusicalNoteMin || note > Note.MusicalNoteMax)
                    {
                        if (note > Note.MusicalNoteMax && note != Note.NoteRelease)
                            note = Note.MusicalNoteMax;
                        instrument = null;
                    }

                    if ((state.note != note) || (state.instrument != instrument && instrument != null) || force)
                    {
                        var pattern = channel.GetOrCreatePattern(p);
                        var newNote = pattern.GetOrCreateNoteAt(n);
                        newNote.Value = (byte)note;
                        newNote.Instrument = instrument;
                        state.note = note;
                        state.octave = octave;
                        if (instrument != null)
                            state.instrument = instrument;
                        if (!attack)
                            newNote.HasAttack = false;
                        hasNote = note != 0;
                        hasNoteWithAttack = newNote.IsMusical && newNote.HasAttack;
                    }

                    if (hasPitch && !stop)
                    {
                        Channel.GetShiftsForType(channel.Type, project.ExpansionNumN163Channels, out int pitchShift, out _);

                        // We scale all pitches changes (slides, fine pitch, pitch envelopes) for
                        // some channels with HUGE pitch values (N163, VRC7).
                        finePitch >>= pitchShift;

                        var pitch = (sbyte)Utils.Clamp(finePitch, Note.FinePitchMin, Note.FinePitchMax);

                        if (pitch != state.pitch)
                        {
                            var pattern = channel.GetOrCreatePattern(p).GetOrCreateNoteAt(n).FinePitch = pitch;
                            state.pitch = pitch;
                        }
                    }

                    state.period = period;
                }

                // Same rule applies for S5B and EPSM with manual envelope period, the only difference with FDS is that 
                // envelope period effects apply regardless of which channel they are on.
                if (channel.IsS5BChannel || channel.IsEPSMSquareChannel)
                {
                    var envFreq = (int)GetState(channel.Type, NotSoFatso.STATE_S5BENVFREQUENCY, 0);
                    var envEnabled = (int)GetState(channel.Type, NotSoFatso.STATE_S5BENVENABLED, 0) != 0;

                    // All envelope frequency will be on square 1.
                    if (ym2149AsEPSM && channel.IsEPSMSquareChannel)
                        envFreq = (int)(envFreq / clockMultiplier[ExpansionType.S5B]);
                    else
                        envFreq = (int)(envFreq / clockMultiplier[channel.Expansion]);

                    // All envelope frequency will be on square 1.
                    if (state.s5bEnvFreq != envFreq || hasNoteWithAttack && envEnabled)
                    {
                        var firstChannelType = channel.IsS5BChannel ? ChannelType.S5BSquare1 : ChannelType.EPSMSquare1;
                        var firstChannel = song.GetChannelByType(firstChannelType);
                        firstChannel.GetOrCreatePattern(p).GetOrCreateNoteAt(n).EnvelopePeriod = (ushort)envFreq;
                        state.s5bEnvFreq = envFreq;
                    }
                }
            }

            return hasNote;
        }

        public static float BcdToDecimal(ReadOnlySpan<byte> bcd)
        {
            Debug.Assert(bcd != null || bcd.Length != 0);

            // Assume reversed.
            var result = 0;
            for (int i = bcd.Length - 1; i >= 0; i--)
            {
                byte item = bcd[i];
                Debug.Assert((item >> 4) < 10);
                Debug.Assert((item % 16) < 10);
                result *= 100;
                result = result + (((item >> 4) * 10) + item % 16);
            }

            return result;
        }

        public static byte[] Decompress(byte[] compressed_data)
        {
            var outputStream = new MemoryStream();
            using (var compressedStream = new MemoryStream(compressed_data))
            using (System.IO.Compression.GZipStream sr = new System.IO.Compression.GZipStream(
                compressedStream, System.IO.Compression.CompressionMode.Decompress))
            {
                sr.CopyTo(outputStream);
                outputStream.Position = 0;
                return outputStream.ToArray();
            }
        }

        /*
         * 
         * Todo:
         * Add 2A03 Sweep Support (blarrg smooth vibrato uses separate workaround)
         * Add Possibility to import second 2A03 Squares as MMC5
         * 
         */
        public Project Load(string filename, int patternLength, int frameSkip, bool adjustClock, bool reverseDpcm, bool preserveDpcmPad, bool ym2149AsEpsm, int tuning = 440)
        {
            var vgmFile = System.IO.File.ReadAllBytes(filename);
            if (filename.EndsWith(".vgz"))
                vgmFile = Decompress(vgmFile);

            if (!vgmFile.Take(4).SequenceEqual(Encoding.ASCII.GetBytes("Vgm ")))
            {
                Log.LogMessage(LogSeverity.Error, "Incompatible file.");
                return null;
            }

            preserveDpcmPadding = preserveDpcmPad;
            Array.Fill(clockMultiplier, 1);
            bool pal = false;
            project = new Project();
            project.Name = "VGM Import";
            project.Author = "unknown";
            project.Copyright = "";
            project.PalMode = false;
            project.Tuning = tuning;
            var songName = "VGM Import";
            project.SetExpansionAudioMask(0xff, 0);
            song = project.CreateSong(songName);
            song.SetDefaultPatternLength(patternLength);
            var p = 0;
            var n = 0;
            channelStates = new ChannelState[50];
            for (int i = 0; i < song.Channels.Length; i++)
                channelStates[i] = new ChannelState();


            var vgmDataOffset = BitConverter.ToInt32(vgmFile.AsSpan(0x34, 4))+0x34;
#if DEBUG
            Log.LogMessage(LogSeverity.Info, "Version : " + (BcdToDecimal(vgmFile.AsSpan(8, 4)) / 100).ToString("F2"));
            Log.LogMessage(LogSeverity.Info, "VGM Data Startoffset: " + vgmDataOffset);
#endif
            var vgmData = new ReadOnlySpan<byte>();
            var vgmCommand = vgmFile[vgmDataOffset];
            if (adjustClock)
            {
                if (BitConverter.ToInt32(vgmFile.AsSpan(0x74, 4)) > 0)
                    clockMultiplier[ExpansionType.S5B] = (float)BitConverter.ToInt32(vgmFile.AsSpan(0x74, 4)) / (((vgmFile[0x78] & vgmFile[0x79] & 0x10) == 0x10) ? 1789773 : (float)894886.5);
                if (BitConverter.ToUInt32(vgmFile.AsSpan(0x44, 4)) > 0)
                    clockMultiplier[ExpansionType.EPSM] = (float)(BitConverter.ToInt32(vgmFile.AsSpan(0x44, 4)) & 0xFFFFFFF) / 4000000;
                if (BitConverter.ToUInt32(vgmFile.AsSpan(0x48, 4)) > 0)
                    clockMultiplier[ExpansionType.EPSM] = (float)(BitConverter.ToInt32(vgmFile.AsSpan(0x48, 4)) & 0xFFFFFFF) / 8000000;
                if (BitConverter.ToUInt32(vgmFile.AsSpan(0x4C, 4)) > 0)
                    clockMultiplier[ExpansionType.EPSM] = (float)(BitConverter.ToInt32(vgmFile.AsSpan(0x4C, 4)) & 0xFFFFFFF) / 8000000;
                if (BitConverter.ToUInt32(vgmFile.AsSpan(0x2c, 4)) > 0)
                    clockMultiplier[ExpansionType.EPSM] = (float)(BitConverter.ToInt32(vgmFile.AsSpan(0x2C, 4)) & 0xFFFFFFF) / 8000000;
                if (BitConverter.ToUInt32(vgmFile.AsSpan(0x10, 4)) > 0)
                    clockMultiplier[ExpansionType.Vrc7] = (float)BitConverter.ToInt32(vgmFile.AsSpan(0x10, 4)) / 3579545;

                if (ym2149AsEpsm)
                {
                    if (BitConverter.ToInt32(vgmFile.AsSpan(0x74, 4)) > 0)
                        clockMultiplier[ExpansionType.S5B] = (float)BitConverter.ToInt32(vgmFile.AsSpan(0x74, 4)) / (((vgmFile[0x78] & vgmFile[0x79] & 0x10) == 0x10) ? 4000000 : 2000000);
                    ym2149AsEPSM = ym2149AsEpsm;
                }
            }
            var chipCommands = 0;
            var unknownChipCommands = 0;
            var samples = 0;
            var frame = 0;
            int expansionMask = 0;
            var samplesPerFrame = 735;

            while (vgmDataOffset < vgmFile.Length) {
                if(expansionMask != project.ExpansionAudioMask)
                    project.SetExpansionAudioMask(expansionMask, 0);

                if (vgmCommand == 0x67)  //DataBlock
                {
                    var dataSize = BitConverter.ToInt32(vgmFile.AsSpan(vgmDataOffset + 3, 4));
                    var dataType = vgmFile[vgmDataOffset + 2];
                    var dataAddr = BitConverter.ToUInt16(vgmFile.AsSpan(vgmDataOffset + 7, 2));

#if DEBUG
                    Log.LogMessage(LogSeverity.Info, $"DataBlock Size: {dataSize:x8}");
                    Log.LogMessage(LogSeverity.Info, $"DataBlock Type: {dataType:x2}");
                    Log.LogMessage(LogSeverity.Info, $"DataBlock Addr: {dataAddr:x4}");
#endif

                    if (vgmFile.Length < (vgmDataOffset + 3 + 4))
                        break;

                    if (vgmFile.Length < dataSize - 2)
                        break;

                    if (vgmFile[vgmDataOffset + 2] == 0xC2) //DPCM Data
                    {
                        var data = vgmFile.AsSpan(vgmDataOffset + 9, dataSize - 2);
                        for (int i = 0; i < data.Length; i++)
                        {
                            if ((i + dataAddr - 0xc000) >= 0)
                                dpcmData[i + dataAddr - 0xc000] = data[i];
                        }
                    }
                    else if (vgmFile[vgmDataOffset + 2] == 0x07) //PCM RAM Data
                    {
                        pcmRAMData = vgmFile.AsSpan(vgmDataOffset + 9, dataSize - 2).ToArray();
                    }
                    else
                    {
                        dpcmData = vgmFile.AsSpan(vgmDataOffset + 9, dataSize - 2).ToArray();
                    }
                    vgmDataOffset = vgmDataOffset + dataSize + 7;
                }
                else if (vgmCommand == 0x68)  //PCM Data Copy
                {
                    if (vgmFile.Length < (vgmDataOffset + 12))
                        break;

                    var readOffset  = Utils.Bytes24BitToInt(vgmFile.AsSpan(vgmDataOffset + 3, 3));
                    var writeOffset = Utils.Bytes24BitToInt(vgmFile.AsSpan(vgmDataOffset + 6, 3));
                    var copySize    = Utils.Bytes24BitToInt(vgmFile.AsSpan(vgmDataOffset + 9, 3));

#if DEBUG
                    Log.LogMessage(LogSeverity.Info, $"PCM RAM Copy Read Offset: {readOffset:x6}");
                    Log.LogMessage(LogSeverity.Info, $"PCM RAM Copy Write Offset: {writeOffset:x6}");
                    Log.LogMessage(LogSeverity.Info, $"PCM RAM Copy: {vgmFile[vgmDataOffset + 2]:x2}");
                    Log.LogMessage(LogSeverity.Info, $"PCM RAM COPY Size: {copySize:x6}");
#endif

                    if (vgmFile.Length < (copySize + vgmDataOffset))
                        break;

                    if (vgmFile[vgmDataOffset + 2] == 0x07)
                    {
                        // PERKKA/ALEX TODO : There is ia OOB access here with some files. Either a bug in the way we export
                        // data or some miscalculation when we import.
                        copySize = Math.Min(copySize, pcmRAMData.Length - readOffset);
                        var data = pcmRAMData.AsSpan(readOffset, copySize).ToArray();
                        for (int i = 0; i < data.Length; i++)
                        {
                            dpcmData[i + writeOffset - 0xc000] = data[i];
                        }

                    }
                    vgmDataOffset = vgmDataOffset + 12;
                }
                else if (vgmCommand == 0x66)
                {
                    vgmDataOffset = vgmDataOffset + 1;
                    //Log.LogMessage(LogSeverity.Info, "VGM Data End");
                    break;
                }

                else if (vgmCommand == 0x61 || vgmCommand == 0x63 || vgmCommand == 0x62 || (vgmCommand >= 0x70 && vgmCommand <= 0x8f))
                {
                    if (vgmCommand == 0x63)
                    {
                        vgmDataOffset = vgmDataOffset + 1;
                        samplesPerFrame = 882;
                        samples = samples + samplesPerFrame;
                        pal = true;
                        project.PalMode = pal;
                    }
                    else if (vgmCommand == 0x62)
                    {
                        vgmDataOffset = vgmDataOffset + 1;
                        samples = samples + samplesPerFrame;
                    }
                    else if (vgmCommand == 0x61)
                    {
                        if (vgmFile.Length < (vgmDataOffset + 3))
                            break;
                        samples = samples + BitConverter.ToUInt16(vgmFile.AsSpan(vgmDataOffset + 1, 2));
                        vgmDataOffset = vgmDataOffset + 3;
                    }
                    else if (vgmCommand >= 0x80)
                    {
                        samples = samples + vgmCommand - 0x80;
                        vgmDataOffset = vgmDataOffset + 1;
                    }
                    else
                    {
                        samples = samples + vgmCommand - 0x6F;
                        vgmDataOffset = vgmDataOffset + 1;
                    }
                    while (samples >= samplesPerFrame)
                    {
                        p = (frame - frameSkip) / song.PatternLength;
                        n = (frame - frameSkip) % song.PatternLength;
                        song.SetLength(p + 1);
                        frame++;
                        samples = samples - samplesPerFrame;
                        if (frameSkip < frame)
                            for (int c = 0; c < song.Channels.Length; c++)
                                UpdateChannel(p, n, song.Channels[c], channelStates[c]);
                    }
                }
                else if (vgmCommand == 0x4F || vgmCommand == 0x50 || vgmCommand == 0x31)
                    vgmDataOffset = vgmDataOffset + 2;
                else if (vgmCommand >= 0xC0 && vgmCommand <= 0xDF)
                    vgmDataOffset = vgmDataOffset + 4;
                else if (vgmCommand == 0xE0)
                    vgmDataOffset = vgmDataOffset + 5;
                else if (vgmCommand >= 0x90 && vgmCommand <= 0x92)
                    vgmDataOffset = vgmDataOffset + 6;
                else if (vgmCommand == 0x93)
                    vgmDataOffset = vgmDataOffset + 11;
                else if (vgmCommand == 0x94)
                    vgmDataOffset = vgmDataOffset + 2;
                else if (vgmCommand == 0x95)
                    vgmDataOffset = vgmDataOffset + 5;
                else
                {
                    if (vgmFile.Length < (vgmDataOffset + 3))
                        break;

                    vgmData = vgmFile.AsSpan(vgmDataOffset, 3);
                    if (vgmCommand == 0xB4)
                    {
                        if (vgmData[1] == 0x17)
                        {
                            if (vgmData[2] == 0xc0)
                            {
                                if (apuRegister[1] == 0x87 && apuRegister[2] == 0xff)
                                    apuRegister[0x03]++;
                                if (apuRegister[1] == 0x8f && apuRegister[2] == 0x00)
                                    apuRegister[0x03]--;
                                if (apuRegister[5] == 0x87 && apuRegister[6] == 0xff)
                                    apuRegister[0x07]++;
                                if (apuRegister[5] == 0x8f && apuRegister[6] == 0x00)
                                    apuRegister[0x07]--;
                            }

                            if (((vgmData[2] & 0x80) >> 7) != 0)
                                isFiveStep = true;
                        }

                        if (vgmData[1] == 0x15 && (vgmData[2] & 0x10) > 0)
                            dpcmTrigger = true;

                        apuRegister[vgmData[1]] = vgmData[2];

                        // Noise decay.
                        if (vgmData[1] == 0x0C || vgmData[1] == 0x0E || vgmData[1] == 0x0F)
                            apuDecayVolume[3] = 15;

                        // FDS.
                        if ((vgmData[1] >= 0x40 && vgmData[1] <= 0x7F) || (vgmData[1] >= 0x20 && vgmData[1] <= 0x3E))
                        {
                            // Mod table.
                            if (vgmData[1] == 0x28)
                            {
                                for (int i = 0; i < 62; i++)
                                {
                                    fdsModulationTable[i] = fdsModulationTable[i + 2];
                                }

                                fdsModulationTable[62] = fdsModulationTable[63] = vgmData[2] & 0x07;
                            }

                            expansionMask = expansionMask | ExpansionType.FdsMask;
                        }
                    }
                    else if (vgmCommand == 0x51)
                    {
                        if (vgmData[1] >= 0x20 && vgmData[1] <= 0x28)
                        {
                            int channel = vgmData[1] - 0x20;
                            if (((vgmData[2] & 0x10) > 0) && ((vrc7Register[vgmData[1]] & 0x10) != (vgmData[2] & 0x10)))
                                if(channel < 6)
                                    vrc7Trigger[channel] = 1;
                        }
                        vrc7Register[vgmData[1]] = vgmData[2];
                        expansionMask = expansionMask | ExpansionType.Vrc7Mask;
                    }
                    else if (vgmCommand == 0x56 || vgmCommand == 0x52 || vgmCommand == 0x58 || vgmCommand == 0x55)
                    {
                        if (vgmData[1] == 0x10)
                            epsmRegisterLo[vgmData[1]] = epsmRegisterLo[vgmData[1]] | vgmData[2];
                        else if (vgmData[1] == 0x0D)
                        {
                            epsmEnvTrigger[0] = 1;
                            epsmEnvTrigger[1] = 1;
                            epsmEnvTrigger[2] = 1;
                            epsmRegisterLo[vgmData[1]] = vgmData[2];
                        }
                        else if (vgmData[1] == 0x28)
                        {
                            int channel = ((((vgmData[2] & 4) >> 2) + 1) * ((vgmData[2] & 3) + 1)) - 1;
                            if ((vgmData[2] & 0x7) == 0)
                            {
                                if ((vgmData[2] & 0xf0) > 0 && epsmFmEnabled[0] == 0)
                                {
                                    epsmFmTrigger[0] = 1;
                                    epsmFmKey[0] = vgmData[2];
                                }
                                epsmFmEnabled[0] = (vgmData[2] & 0xf0) > 0 ? 1 : 0;
                            }
                            if ((vgmData[2] & 0x7) == 1)
                            {
                                if ((vgmData[2] & 0xf0) > 0 && epsmFmEnabled[1] == 0)
                                {
                                    epsmFmTrigger[1] = 1;
                                    epsmFmKey[1] = vgmData[2];
                                }
                                epsmFmEnabled[1] = (vgmData[2] & 0xf0) > 0 ? 1 : 0;
                            }
                            if ((vgmData[2] & 0x7) == 2)
                            {
                                if ((vgmData[2] & 0xf0) > 0 && epsmFmEnabled[2] == 0)
                                {
                                    epsmFmTrigger[2] = 1;
                                    epsmFmKey[2] = vgmData[2];
                                }
                                epsmFmEnabled[2] = (vgmData[2] & 0xf0) > 0 ? 1 : 0;
                            }
                            if ((vgmData[2] & 0x7) == 4)
                            {
                                if ((vgmData[2] & 0xf0) > 0 && epsmFmEnabled[3] == 0)
                                {
                                    epsmFmTrigger[3] = 1;
                                    epsmFmKey[3] = vgmData[2];
                                }
                                epsmFmEnabled[3] = (vgmData[2] & 0xf0) > 0 ? 1 : 0;
                            }
                            if ((vgmData[2] & 0x7) == 5)
                            {
                                if ((vgmData[2] & 0xf0) > 0 && epsmFmEnabled[4] == 0)
                                {
                                    epsmFmTrigger[4] = 1;
                                    epsmFmKey[4] = vgmData[2];
                                }
                                epsmFmEnabled[4] = (vgmData[2] & 0xf0) > 0 ? 1 : 0;
                            }
                            if ((vgmData[2] & 0x7) == 6)
                            {
                                if ((vgmData[2] & 0xf0) > 0 && epsmFmEnabled[5] == 0)
                                {
                                    epsmFmTrigger[5] = 1;
                                    epsmFmKey[5] = vgmData[2];
                                }
                                epsmFmEnabled[5] = (vgmData[2] & 0xf0) > 0 ? 1 : 0;
                            }
                        }
                        else if (vgmData[1] >= 0x30 && vgmData[1] <= 0x4F)
                            epsmRegisterLo[vgmData[1]] = vgmData[2] & 0x7f;
                        else if (vgmData[1] >= 0x50 && vgmData[1] <= 0x5F)
                            epsmRegisterLo[vgmData[1]] = vgmData[2] & 0xdf;
                        else if (vgmData[1] >= 0x60 && vgmData[1] <= 0x6F)
                            epsmRegisterLo[vgmData[1]] = vgmData[2] & 0x9f;
                        else if (vgmData[1] >= 0x70 && vgmData[1] <= 0x7F)
                            epsmRegisterLo[vgmData[1]] = vgmData[2] & 0x1f;
                        else if (vgmData[1] >= 0x90 && vgmData[1] <= 0x9F)
                            epsmRegisterLo[vgmData[1]] = vgmData[2] & 0x0f;
                        else if (vgmData[1] >= 0xB0 && vgmData[1] <= 0xB2)
                            epsmRegisterLo[vgmData[1]] = vgmData[2] & 0x3f;
                        else if (vgmData[1] >= 0xB4 && vgmData[1] <= 0xB6)
                            epsmRegisterLo[vgmData[1]] = vgmData[2] & 0xf7;
                        else
                            epsmRegisterLo[vgmData[1]] = vgmData[2];
                        expansionMask = expansionMask | ExpansionType.EPSMMask;
                    }
                    else if (vgmCommand == 0x57 || vgmCommand == 0x53 || vgmCommand == 0x59)
                    {
                        if (vgmData[1] >= 0x30 && vgmData[1] <= 0x4F)
                            epsmRegisterHi[vgmData[1]] = vgmData[2] & 0x7f;
                        else if (vgmData[1] >= 0x50 && vgmData[1] <= 0x5F)
                            epsmRegisterHi[vgmData[1]] = vgmData[2] & 0xdf;
                        else if (vgmData[1] >= 0x60 && vgmData[1] <= 0x6F)
                            epsmRegisterHi[vgmData[1]] = vgmData[2] & 0x9f;
                        else if (vgmData[1] >= 0x70 && vgmData[1] <= 0x7F)
                            epsmRegisterHi[vgmData[1]] = vgmData[2] & 0x1f;
                        else if (vgmData[1] >= 0x90 && vgmData[1] <= 0x9F)
                            epsmRegisterHi[vgmData[1]] = vgmData[2] & 0x0f;
                        else if (vgmData[1] >= 0xB0 && vgmData[1] <= 0xB2)
                            epsmRegisterHi[vgmData[1]] = vgmData[2] & 0x3f;
                        else if (vgmData[1] >= 0xB4 && vgmData[1] <= 0xB6)
                            epsmRegisterHi[vgmData[1]] = vgmData[2] & 0xf7;
                        else
                            epsmRegisterHi[vgmData[1]] = vgmData[2];
                        expansionMask = expansionMask | ExpansionType.EPSMMask;
                    }
                    else if (vgmCommand == 0xA0)
                    {
                        if (ym2149AsEpsm)
                        {
                            if (vgmData[1] == 0x0D)
                            {
                                epsmEnvTrigger[0] = 1;
                                epsmEnvTrigger[1] = 1;
                                epsmEnvTrigger[2] = 1;
                            }
                            epsmRegisterLo[vgmData[1]] = vgmData[2];
                            expansionMask = expansionMask | ExpansionType.EPSMMask;
                        }
                        else
                        {
                            if (vgmData[1] == 0x0D)
                            {
                                s5bEnvTrigger[0] = 1;
                                s5bEnvTrigger[1] = 1;
                                s5bEnvTrigger[2] = 1;
                            }
                            s5bRegister[vgmData[1]] = vgmData[2];
                            expansionMask = expansionMask | ExpansionType.S5BMask;
                        }
                    }
                    else
                    {
                        if(unknownChipCommands < 100)
                            Log.LogMessage(LogSeverity.Info, "Unknown VGM Chip Data: " + BitConverter.ToString(vgmData.ToArray()).Replace("-", "") + " offset: " + vgmDataOffset + " command " + vgmCommand);
                        unknownChipCommands++;
                    }
                    chipCommands++;
                    vgmDataOffset = vgmDataOffset + 3;
                }
                if (vgmFile.Length > vgmDataOffset)
                    vgmCommand = vgmFile[vgmDataOffset];
                else
                    break;
            }
            if(pal)
                Log.LogMessage(LogSeverity.Info, "VGM is PAL");
            else
                Log.LogMessage(LogSeverity.Info, "VGM is NTSC");
#if DEBUG
            Log.LogMessage(LogSeverity.Info, "VGM Chip Commands: " + chipCommands);
            Log.LogMessage(LogSeverity.Info, "S5b Clock Multiplier: " + clockMultiplier[ExpansionType.S5B]);
            Log.LogMessage(LogSeverity.Info, "EPSM Clock Multiplier: " + clockMultiplier[ExpansionType.EPSM]);
            Log.LogMessage(LogSeverity.Info, "VRC7 Clock Multiplier: " + clockMultiplier[ExpansionType.Vrc7]);
#endif
            Log.LogMessage(LogSeverity.Info, "Frames: " + frame + " time: " + (frame/60) + "s");

            if (vgmFile.Length > (vgmDataOffset + 4))
            {
                if (vgmFile.AsSpan(vgmDataOffset, 4).SequenceEqual(Encoding.ASCII.GetBytes("Gd3 ")))
                {
                    vgmDataOffset = vgmDataOffset + 4 + 4 + 4; // "Gd3 " + version + gd3 length data
                    var gd3Data = vgmFile.AsSpan(vgmDataOffset, vgmFile.Length - vgmDataOffset);
                    var gd3DataArray = System.Text.Encoding.Unicode.GetString(gd3Data).Split("\0");
#if DEBUG
                    Log.LogMessage(LogSeverity.Info, "Gd3 Data: " + System.Text.Encoding.Unicode.GetString(gd3Data));
#endif
                    Log.LogMessage(LogSeverity.Info, "Track Name: " + gd3DataArray[0]);
                    songName = gd3DataArray[0];
                    Log.LogMessage(LogSeverity.Info, "Game Name: " + gd3DataArray[2]);
                    project.Name = gd3DataArray[2] + gd3DataArray[4];
                    Log.LogMessage(LogSeverity.Info, "System Name: " + gd3DataArray[4]);
                    Log.LogMessage(LogSeverity.Info, "Original Author Name: " + gd3DataArray[6]);
                    project.Copyright = gd3DataArray[6];
                    Log.LogMessage(LogSeverity.Info, "Release Date: " + gd3DataArray[7]);
                    Log.LogMessage(LogSeverity.Info, "Converted by: " + gd3DataArray[8]);
                    project.Author = gd3DataArray[8];
                    Log.LogMessage(LogSeverity.Info, "Notes: " + gd3DataArray[9]);
                }
            }


            frame++;
            p = (frame - frameSkip) / song.PatternLength;
            n = (frame - frameSkip) % song.PatternLength;
            for (int c = 0; c < song.Channels.Length; c++)
            {
                if (channelStates[c].state != ChannelState.Stopped)
                    song.Channels[c].GetOrCreatePattern(p).GetOrCreateNoteAt(n).IsStop = true;
            }
            song.Name = string.IsNullOrEmpty(songName) ? "VGM Import" : songName; // Song name should never be blank
            var factors = Utils.GetFactors(song.PatternLength, FamiStudioTempoUtils.MaxNoteLength);
            if (factors.Length > 0)
            {
                var noteLen = factors[0];

                // Look for a factor that generates a note length < 10 and gives a pattern length that is a multiple of 16.
                foreach (var factor in factors)
                {
                    if (factor <= 10)
                    {
                        noteLen = factor;
                        if (((song.PatternLength / noteLen) % 16) == 0)
                            break;
                    }
                }

                song.ChangeFamiStudioTempoGroove(new[] { noteLen }, false);
            }
            else
            {
                song.ChangeFamiStudioTempoGroove(new[] { 1 }, false);
            }
            song.SetSensibleBeatLength();
            song.ConvertToCompoundNotes();
            song.DeleteEmptyPatterns();
            song.UpdatePatternStartNotes();
            song.InvalidateCumulativePatternCache();
            project.DeleteUnusedInstruments();

            if (reverseDpcm)
            {
                foreach (var sample in project.Samples)
                {
                    sample.ReverseBits = true;            
                    sample.Process();
                }
            }
            
            return project;
        }
    }


}
