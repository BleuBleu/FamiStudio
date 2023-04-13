using System;
using System.Diagnostics;
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
        public unsafe static void Save(Song song, string filename, int filetype)
        {
            var project = song.Project;
            var regPlayer = new RegisterPlayer(song.Project.OutputsStereoAudio);
            var writes = regPlayer.GetRegisterValues(song, project.PalMode);
            Console.WriteLine("Writes got, length: " + writes.Length);
            var regOptimizer = new RegisterWriteOptimizer(song.Project);
            writes = regOptimizer.OptimizeRegisterWrites(writes);
            Console.WriteLine("Writes optimized, length: " + writes.Length);
            var lastWrite = writes.Last();

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


                string systemName;  //For Gd3
                if (project.PalMode)
                {
                    header.rate = 50;
                    NESClock = 1662607;
                    waitCommand = 0x63;
                    samplesPerFrame = 882;
                    systemName = "PAL NES";     //PAL Famicom? WTF would that be
                }
                else
                {
                    header.rate = 60;
                    NESClock = 1789772;
                    waitCommand = 0x62;
                    samplesPerFrame = 735;
                    systemName = "NTSC NES / Famicom";
                }
                header.totalSamples = (lastWrite.FrameNumber + 1) * samplesPerFrame;
                // The clock values are purely theoretical for expansions in PAL.
                if (project.UsesVrc7Expansion)
                {
                    header.ym2413clock = 3579545 | 0x80000000;
                    systemName += " + Konami VRC7";
                }

                if (project.UsesFdsExpansion)
                {
                    header.NESAPUclock = NESClock | 0x80000000;
                    systemName += " + Famicom Disk System";
                }
                else { header.NESAPUclock = NESClock; }


                if (project.UsesS5BExpansion)
                {
                    header.AY8910clock = (int)NESClock / 2;    //Divided by 2 because the SEL pin's pulled low
                    header.AY8910ChipType = 0x10;           // = YM2149
                    systemName += " + Sunsoft 5B";
                }

                if (project.UsesEPSMExpansion)
                {
                    header.YM2608clock = 8000000;
                    systemName += " + EPSM";
                }

                int extraHeaderSize = (project.UsesVrc7Expansion | project.UsesS5BExpansion | project.UsesEPSMExpansion ? 13 : 0) +
                    (project.UsesVrc7Expansion ? 4 : 0) +
                    (project.UsesS5BExpansion ? 4 : 0) +
                    (project.UsesEPSMExpansion ? 8 : 0);

                int initSize = 24 + (project.UsesEPSMExpansion ? 9 : 0);

                header.vgmDataOffset = 0x8C + extraHeaderSize;
                header.ExtraHeaderOffset = extraHeaderSize != 0 ? 4 : 0;

                string gd3 = "Gd3 ";
                string gd3Data = song.Name + "\0\0";  //Track Name, Skip Track name (in original (non-English) game language characters)
                gd3Data += song.Project.Name + "\0\0"; //Game Name, Skip Game name (in original (non-English) game language characters)
                gd3Data += systemName + "\0\0"; //System Name, Skip System name (in original (non-English) game language characters)
                gd3Data += song.Project.Author + "\0\0\0";//Author Name, Skip Name of Original Track Author (in original (non-English) game characters), Date of game's release
                gd3Data += "FamiStudio Export\0";   //VGM Convert Person
                gd3Data += song.Project.Copyright + "\0";   //Notes
                int gd3Length = (gd3Data.Length * 2);
                int loopPointFrame = song.LoopPoint;
                var sampleData = project.GetPackedSampleData();
                if (filetype == 1)
                {
                    var writer = new BinaryWriter(file);
                    var fileLength = sizeof(VgmHeader) + initSize + extraHeaderSize + 1 - 4 + sampleData.Length + 9; //headerbytes + init bytes - offset (4bytes)  + Extra header + audio stop 1byte
                    int frameNumber = 0;
                    if (frameNumber == 0) { header.loopOffset = fileLength - 25; }  // Relative pointer difference is 24, and the 1 is the data stop command at the end
                    foreach (var reg in writes)
                    {
                        while (frameNumber < reg.FrameNumber)
                        //TODO: for 3+ frames only increment file length by 3
                        {
                            frameNumber++;
                            fileLength++;
                            if (frameNumber == loopPointFrame) { header.loopOffset = fileLength - 25; }  // Relative pointer difference is 24, and the 1 is the data stop command at the end
                        }
                        switch (reg.Register)
                        {
                            case NesApu.EPSM_DATA0: case NesApu.EPSM_DATA1:
                            case NesApu.VRC7_REG_WRITE:
                            case NesApu.S5B_DATA:
                            case (>= NesApu.APU_PL1_VOL and < NesApu.EPSM_ADDR0):  //2A03/7 registers
                            case (>= 0x4020 and < 0x409f):  //FDS registers
                                fileLength += 3;
                                break;
                        }
                    }
                    header.loopBase = 0;
                    if (loopPointFrame != -1)
                    {
                        header.loopModifier = 0x10;
                        header.loopSamples = (lastWrite.FrameNumber - loopPointFrame + 1) * samplesPerFrame;
                    }
                    else
                    {
                        header.loopSamples = 0;
                        header.loopOffset = 0;
                        header.loopModifier = 0;
                    }
                    header.gd3Offset = fileLength - 16;
                    fileLength = fileLength + gd3Length;
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

                    //Sample data
                    writer.Write(new byte[] { 0x67, 0x66, 0xC2 });  //Data block, compat command for older players, type - NES APU RAM Write
                    writer.Write(sampleData.Length + 2);  //Length of sample data + address
                    writer.Write((ushort)0xC000);   //Address $C000 - the minimum for DPCM data
                    writer.Write(sampleData);   //Write the sample data

                    // Not as lame now
                    byte addressEPSMA0 = 0;
                    byte addressEPSMA1 = 0;
                    byte addressVRC7 = 0;
                    byte address5B = 0;

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
                             0x56, 0x29, 0x80,  //Disable IRQs, bit 7 ???
                             0x56, 0x27, 0x00,  //Disable timers, use 6 channel mode
                             0x56, 0x11, 0x37});//Max rhythm total volume
                    }
                    foreach (var reg in writes)
                    {
                        while (frameNumber < reg.FrameNumber)
                        {
                            frameNumber++;
                            writer.Write(waitCommand);
                        }
                        if (reg.Register == NesApu.EPSM_ADDR0)          //EPSM A0 Address
                        {
                            addressEPSMA0 = (byte)reg.Value;
                        }
                        else if (reg.Register == NesApu.EPSM_ADDR1)     //EPSM A1 Address
                        {
                            addressEPSMA1 = (byte)reg.Value;
                        }
                        else if (reg.Register == NesApu.VRC7_REG_SEL)   //VRC7 Address
                        {
                            addressVRC7 = (byte)reg.Value;
                        }
                        else if (reg.Register == NesApu.S5B_ADDR)       //5B Address
                        {
                            address5B = (byte)reg.Value;
                        }
                        else if (reg.Register == NesApu.EPSM_DATA0)     //EPSM A0 Data
                        {
                            writer.Write(new byte[] { 0x56, addressEPSMA0, (byte)reg.Value });
                        }
                        else if (reg.Register == NesApu.EPSM_DATA1)     //EPSM A1 Data
                        {
                            writer.Write(new byte[] { 0x57, addressEPSMA1, (byte)reg.Value });
                        }
                        else if (reg.Register == NesApu.VRC7_REG_WRITE) //VRC7 Data
                        {
                            writer.Write(new byte[] { 0x51, addressVRC7, (byte)reg.Value });
                        }
                        else if (reg.Register == NesApu.S5B_DATA)       //5B Data
                        {
                            writer.Write(new byte[] { 0xA0, address5B, (byte)reg.Value });
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
                else
                {
                    var sr = new StreamWriter(file);
                    // So lame.
                    int frameNumber = 0;
                    int chipData = 0;
                    sr.WriteLine("WAITFRAME = 1");
                    sr.WriteLine("APU_WRITE = 2");
                    sr.WriteLine("EPSM_A0_WRITE = 3");
                    sr.WriteLine("EPSM_A1_WRITE = 4");
                    sr.WriteLine("S5B_WRITE = 5");
                    sr.WriteLine("VRC7_WRITE = 6");
                    sr.WriteLine("N163_WRITE = 7");
                    sr.WriteLine("LOOP_VGM = 8");
                    sr.WriteLine("STOP_VGM = 9");
                    sr.WriteLine("MMC5_WRITE = 10");
                    sr.WriteLine(".byte APU_WRITE, $08, $15, $0f, $08, $80, $0f, $00, $00, $30, $04, $30, $0c, $30, $01, $08, $05, $08");
                    sr.WriteLine(".byte EPSM_A0_WRITE, $01, $07, $38");
                    sr.WriteLine(".byte EPSM_A0_WRITE, $04, $07, $38, $29, $80, $27, $00, $11, $37");
                    string writeByteStream = "";
                    string writeCommandByte = ";start";
                    int lastReg = 0;
                    int repeatingReg = 0;
                    foreach (var reg in writes)
                    {
                        if (frameNumber < reg.FrameNumber)
                        {
                            if (lastReg != 0)
                            {
                                sr.WriteLine(writeCommandByte + $"${repeatingReg:X2}" + writeByteStream);
                                writeByteStream = "";
                                writeCommandByte = ";start";
                                repeatingReg = 0;
                            }
                            while (frameNumber < reg.FrameNumber)
                            {
                                frameNumber++;
                                repeatingReg++;
                            }
                            sr.WriteLine($".byte WAITFRAME, ${repeatingReg:X2}");
                            lastReg = 0;
                            repeatingReg = 0;
                        }
                        if ((reg.Register == 0x401c) || (reg.Register == 0x401e) || (reg.Register == 0x9010) || (reg.Register == 0xC000) || (reg.Register == 0xF800))
                        {
                            chipData = reg.Value;
                        }
                        if (reg.Register == 0x401d)
                        {
                            if (lastReg == reg.Register && repeatingReg < 255)
                            {
                                writeByteStream = writeByteStream + $", ${chipData:X2}, ${reg.Value:X2}";
                                repeatingReg++;
                            }
                            else
                            {
                                if (lastReg != 0)
                                    sr.WriteLine(writeCommandByte + $"${repeatingReg:X2}" + writeByteStream);
                                writeCommandByte = ".byte EPSM_A0_WRITE,";
                                writeByteStream = $", ${chipData:X2}, ${reg.Value:X2}";
                                lastReg = reg.Register;
                                repeatingReg = 1;
                            }
                        }
                        else if (reg.Register == 0x401f)
                        {
                            if (lastReg == reg.Register && repeatingReg < 255)
                            {
                                writeByteStream = writeByteStream + $", ${chipData:X2}, ${reg.Value:X2}";
                                repeatingReg++;
                            }
                            else
                            {
                                if (lastReg != 0)
                                    sr.WriteLine(writeCommandByte + $"${repeatingReg:X2}" + writeByteStream);
                                writeCommandByte = ".byte EPSM_A1_WRITE,";
                                writeByteStream = $", ${chipData:X2}, ${reg.Value:X2}";
                                lastReg = reg.Register;
                                repeatingReg = 1;
                            }
                        }
                        else if (reg.Register == 0x9030)
                        {
                            if (lastReg == reg.Register && repeatingReg < 255)
                            {
                                writeByteStream = writeByteStream + $", ${chipData:X2}, ${reg.Value:X2}";
                                repeatingReg++;
                            }
                            else
                            {
                                if (lastReg != 0)
                                    sr.WriteLine(writeCommandByte + $"${repeatingReg:X2}" + writeByteStream);
                                writeCommandByte = ".byte VRC7_WRITE,";
                                writeByteStream = $", ${chipData:X2}, ${reg.Value:X2}";
                                lastReg = reg.Register;
                                repeatingReg = 1;
                            }
                        }
                        else if (reg.Register == 0xE000)
                        {
                            if (lastReg == reg.Register && repeatingReg < 255)
                            {
                                writeByteStream = writeByteStream + $", ${chipData:X2}, ${reg.Value:X2}";
                                repeatingReg++;
                            }
                            else
                            {
                                if (lastReg != 0)
                                    sr.WriteLine(writeCommandByte + $"${repeatingReg:X2}" + writeByteStream);
                                writeCommandByte = ".byte S5B_WRITE,";
                                writeByteStream = $", ${chipData:X2}, ${reg.Value:X2}";
                                lastReg = reg.Register;
                                repeatingReg = 1;
                            }
                        }
                        else if (reg.Register == 0x4800)
                        {
                            if (lastReg == reg.Register && repeatingReg < 255)
                            {
                                writeByteStream = writeByteStream + $", ${chipData:X2}, ${reg.Value:X2}";
                                repeatingReg++;
                            }
                            else
                            {
                                if (lastReg != 0)
                                    sr.WriteLine(writeCommandByte + $"${repeatingReg:X2}" + writeByteStream);
                                writeCommandByte = ".byte N163_WRITE,";
                                writeByteStream = $", ${chipData:X2}, ${reg.Value:X2}";
                                lastReg = reg.Register;
                                repeatingReg = 1;
                            }
                        }
                        else if ((reg.Register <= 0x401B) || (reg.Register <= 0x407f && reg.Register >= 0x4040))
                        {
                            if (lastReg == 0x2a03)
                            {
                                writeByteStream = writeByteStream + $", ${(reg.Register & 0xff):X2}, ${reg.Value:X2}";
                                repeatingReg++;
                            }
                            else
                            {
                                if (lastReg != 0)
                                    sr.WriteLine(writeCommandByte + $"${repeatingReg:X2}" + writeByteStream);
                                writeCommandByte = ".byte APU_WRITE,";
                                writeByteStream = $", ${(reg.Register & 0xff):X2}, ${reg.Value:X2}";
                                lastReg = 0x2a03;
                                repeatingReg = 1;
                            }
                        }
                        else if (reg.Register >= 0x5000 && reg.Register <= 0x5020)
                        {
                            if (lastReg == 0x5000)
                            {
                                writeByteStream = writeByteStream + $", ${(reg.Register & 0xff):X2}, ${reg.Value:X2}";
                                repeatingReg++;
                            }
                            else
                            {
                                if (lastReg != 0)
                                    sr.WriteLine(writeCommandByte + $"${repeatingReg:X2}" + writeByteStream);
                                writeCommandByte = ".byte MMC5_WRITE,";
                                writeByteStream = $", ${(reg.Register & 0xff):X2}, ${reg.Value:X2}";
                                lastReg = 0x5000;
                                repeatingReg = 1;
                            }
                        }
                    }
                    sr.WriteLine(writeCommandByte + $"${repeatingReg:X2}" + writeByteStream);
                    sr.WriteLine(".byte STOP_VGM");
                    sr.WriteLine($" .segment \"DPCM\"");
                    var i = 0;
                    string dpcmData = "";
                    foreach (var sample in sampleData)
                    {
                        i++;
                        dpcmData = dpcmData + " $" + $"{sample:X2}";
                        if ((i % 8) != 0)
                            dpcmData = dpcmData + ",";
                        if ((i % 8) == 0)
                        {
                            sr.WriteLine($" .byte " + dpcmData);
                            dpcmData = "";
                        }

                    }
                    if ((i % 8) != 0)
                    {
                        sr.WriteLine($" .db" + dpcmData);
                    }

                    sr.WriteLine($" ;end of file");
                    sr.Flush();
                    sr.Close();
                }
            }
        }
    }
}
