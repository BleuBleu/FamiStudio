using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;

namespace FamiStudio
{
    static class VgmExport
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
        {
            //This is a fucking mess, please forgive me

            var project = song.Project.DeepClone();
            song = project.GetSong(song.Id);
            var regPlayer = new RegisterPlayer(project.OutputsStereoAudio);
            int OGSongLength;
            int numDPCMBanks = (!project.UsesMultipleDPCMBanks &&
            project.GetPackedSampleData(0).Length <= 16384) ?
            1 : project.AutoAssignSamplesBanks(16384, out _);
            var writes = regPlayer.GetRegisterValues(song, project.PalMode, out OGSongLength);
            Console.WriteLine($"OG SONG LENGTH: {OGSongLength}");
            int TotalLength = 0, IntroLength = 0;
            if (smoothLoop){
                song.ExtendForLooping(2);
                writes = regPlayer.GetRegisterValues(song, project.PalMode, out TotalLength);
            }
            bool loopsTwice = false;
            Console.WriteLine("Writes got, length: " + writes.Length);
            writes = RegisterWriteOptimizer.RemoveExpansionWritesBut(writes, 0x8000 
            | ExpansionType.Vrc7Mask | ExpansionType.FdsMask 
            | ExpansionType.S5BMask | ExpansionType.EPSMMask);
            if (song.LoopPoint >= 0){
                if (song.LoopPoint > 0){
                    var IntroLengthSong = project.DeepClone().GetSong(song.Id);
                    IntroLengthSong.SetLength(IntroLengthSong.LoopPoint);
                    regPlayer.GetRegisterValues(IntroLengthSong, project.PalMode, out IntroLength);
                }
                if (smoothLoop){
                    writes = OptimizeLooping(writes, IntroLength, OGSongLength, out loopsTwice);
                    if (loopsTwice) IntroLength = OGSongLength;
                    else TotalLength = OGSongLength;
                } else TotalLength = OGSongLength;
            } else TotalLength = OGSongLength;        
            Console.WriteLine("Optimized looping, length: " + writes.Length);

            writes = RegisterWriteOptimizer.OptimizeRegisterWrites(writes);
            Console.WriteLine("Writes optimized, length: " + writes.Length);
             
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
                for (int i = 0; i < numDPCMBanks; i++){
                    sampleBankPointers[i] = DPCMDataList.Count;
                    DPCMDataList.AddRange(project.GetPackedSampleData(i).ToList());
                }   
                var sampleData = DPCMDataList.ToArray();
                bool DPCMUsed = sampleData.Length != 0;
                var writer = new BinaryWriter(file);
                var fileLength = sizeof(VgmHeader) + initSize + extraHeaderSize 
                + 1 - 4 + (DPCMUsed ? (sampleData.Length + ( project.UsesMultipleDPCMBanks ? 7 + VgmExport.GetAmountOfBankswitching(writes) * 12 : 9 )) : 0); 
                //headerbytes + init bytes - offset (4bytes)  + Extra header + audio stop 1byte
                int frameNumber = 0;
                if (IntroLength == 0) { header.loopOffset = fileLength - 25; }  // Relative pointer difference is 24, and the 1 is the data stop command at the end
                foreach (var reg in writes)
                {
                    while (frameNumber < reg.FrameNumber)
                    {
                        if (reg.FrameNumber - frameNumber >= 3){
                            fileLength += 3;
                            if (IntroLength <= frameNumber || IntroLength >= reg.FrameNumber || IntroLength >= frameNumber + maxFramesPerWaitCommand){
                                frameNumber += Utils.Clamp(reg.FrameNumber - frameNumber, 3, maxFramesPerWaitCommand);
                                if (IntroLength == frameNumber){
                                    header.loopOffset = fileLength - 25;    // Relative pointer difference is 24, the 1 is the data stop command at the end
                                }
                            } else {    // IntroLength sandwiched between frameNumber and reg.FrameNumber
                                frameNumber += Utils.Clamp(IntroLength - frameNumber, 3, maxFramesPerWaitCommand);
                                header.loopOffset = fileLength - 25;    // Relative pointer difference is 24, the 1 is the data stop command at the end
                            }   
                        } else {
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
                    header.loopBase = loopsTwice ? (byte) 1 : (byte) 0;
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
                if (DPCMUsed){
                    //Sample data
                    if (project.UsesMultipleDPCMBanks){
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
                        if (reg.FrameNumber - frameNumber >= 3){
                            if (IntroLength <= frameNumber || IntroLength >= reg.FrameNumber || IntroLength >= frameNumber + maxFramesPerWaitCommand){
                                
                                writer.Write((byte)0x61);
                                writer.Write((short)(Utils.Clamp(reg.FrameNumber - frameNumber, 3, maxFramesPerWaitCommand)*samplesPerFrame));
                                frameNumber += Utils.Clamp(reg.FrameNumber - frameNumber, 3, maxFramesPerWaitCommand);
                            } else {    // IntroLength sandwiched between frameNumber and reg.FrameNumber
                                writer.Write((byte)0x61);
                                writer.Write((short)(Utils.Clamp(IntroLength - frameNumber, 3, maxFramesPerWaitCommand)*samplesPerFrame));
                                frameNumber += Utils.Clamp(IntroLength - frameNumber, 3, maxFramesPerWaitCommand);
                            }
                        } else {
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
                    else if (reg.Register == NesApu.APU_DMC_START){
                            if (reg.Metadata[0] != DPCMBank && project.UsesMultipleDPCMBanks){
                                writer.Write(new byte[] {0x68, 0x66, 0x07});    //Transfer data block type NES APU RAM write
                                writer.Write(Utils.IntToBytes24Bit(sampleBankPointers[reg.Metadata[0]]));
                                writer.Write(new byte[] {0x00, 0xC0, 0x00, 0x00, 0x40, 0x00}); //Write 4000 bytes to address C000
                            }
                            DPCMBank = reg.Metadata[0];
                            writer.Write (new byte[] {0xB4, NesApu.APU_DMC_START & 0xFF});
                            writer.Write ((byte)(project.GetSampleBankOffset(project.GetSample(reg.Metadata[1]))>>6));
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
        private static int GetAmountOfBankswitching(RegisterWrite[] writes){
            int bankInUse = -1;
            int bankswitches = 0;
            foreach (var write in writes){
                if (write.Register == NesApu.APU_DMC_START && write.Metadata[0] != bankInUse){
                    bankswitches++;
                    bankInUse = write.Metadata[0];
                }
            }
            return bankswitches;
        }

        private static RegisterWrite[] OptimizeLooping(RegisterWrite[] writes, int IntroLength, int LoopFrame, out bool loopsTwice){

            //TODO: determine states at frames instead of comparing writes

            int pointer = 0;
            int afterLoopPointer = 0;
            for (; writes[pointer].FrameNumber != IntroLength; pointer++){}
            Console.WriteLine(" === LoopOpt: Loop pointer " + pointer + " at frame " + writes[pointer].FrameNumber);
            for (afterLoopPointer = pointer; writes[afterLoopPointer].FrameNumber != LoopFrame; afterLoopPointer++){}
            int finalLoopPointer = afterLoopPointer;
            Console.WriteLine(" === LoopOpt: Second loop pointer " + afterLoopPointer + " at frame " + writes[afterLoopPointer].FrameNumber);
            int LoopLength = LoopFrame - IntroLength;   //reused a shitton of times
            int frame = IntroLength;
            for (; frame < LoopFrame; frame++){
                for (; writes[afterLoopPointer].FrameNumber < frame + LoopLength && afterLoopPointer < writes.Length; afterLoopPointer++){}    //To pad afterLoopPointer 
                for (; writes[pointer].FrameNumber < frame; pointer++){}    //To pad pointer 
                for (; afterLoopPointer < writes.Length && writes[pointer].FrameNumber <= frame && writes[afterLoopPointer].FrameNumber <= frame + LoopFrame; pointer++, afterLoopPointer++){
                    if (!(writes[pointer].Register == writes[afterLoopPointer].Register &&
                    writes[pointer].Value == writes[afterLoopPointer].Value &&
                    ((writes[pointer].Register == NesApu.APU_DMC_START && 
                    writes[pointer].Metadata[1] == writes[afterLoopPointer].Metadata[1]) ||
                    writes[pointer].Register != NesApu.APU_DMC_START))){
                        Console.WriteLine($"Dumbass broke at frame {writes[pointer].FrameNumber} at pointer {pointer}/{afterLoopPointer} because \'{writes[pointer].Value:x2} => ${writes[pointer].Register:x4}\' isn't equal to \'{writes[afterLoopPointer].Value:x2} => ${writes[afterLoopPointer].Register:x4}\'");
                        loopsTwice = true;
                        return writes;
                    }
                    }
                

                //Console.WriteLine(frame.ToString() + " " + pointer.ToString() + " " + afterLoopPointer.ToString());
            }
            loopsTwice = false;
            return writes.Take(finalLoopPointer).ToArray();
        }
    }
}
