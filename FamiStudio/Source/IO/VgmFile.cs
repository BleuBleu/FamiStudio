using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

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
            var regPlayer = new RegisterPlayer();
            var writes = regPlayer.GetRegisterValues(song, project.PalMode);
            var lastWrite = writes.Last();

            using (var file = new FileStream(filename, FileMode.Create))
            {
                var header = new VgmHeader();

                header.Vgm[0] = (byte)'V';
                header.Vgm[1] = (byte)'g';
                header.Vgm[2] = (byte)'m';
                header.Vgm[3] = (byte)' ';
                header.version = 0x00000170;
                header.ym2413clock = 3579545 + 0x80000000;
                header.YM2608clock = 8000000;
                header.NESAPUclock = 1789772 + 0x80000000;
                header.AY8910clock = 1789772;
                header.AY8910ChipType = 0x10;
                header.vgmDataOffset = 0x8C+29;
                header.totalSamples = lastWrite.FrameNumber*735;
                //header.vgmDataOffset = 0xBA;
                header.rate = 60;
                header.ExtraHeaderOffset = 0x4;
                
                string gd3 = "Gd3 ";
                string songName = song.Name + "\0";
                string gameName = song.Project.Name + "\0";
                string systemName = "NES/Famicom FamiStudio Export\0";
                string author = song.Project.Author + "\0";
                int gd3Lenght = gd3.Length + (songName.Length * 2) + (gameName.Length * 2) + (systemName.Length * 2) + (author.Length * 2) + 2 + 2 + 2 + 2 + 2 + 4 + 4 + 4;

                // ntsc wait 0x62 735 samples
                // pal wait 0x63 882 samples
                // 0x67 data block
                // 0x66 end of data
                //header.totalSamples = (waits* 882 or 735)


                var sampleData = project.GetPackedSampleData();



                if (filetype == 1)
                {
                    var fileLenght = sizeof(VgmHeader) + 39 - 4 +29 + 1; //headerbytes + init bytes (39)  - offset (4bytes)  + extraheader (29bytes) + audio stop 1byte
                    int frameNumber = 0;
                    foreach (var reg in writes)
                    {
                        while (frameNumber < reg.FrameNumber)
                        {
                            frameNumber++;
                            fileLenght++;
                        }
                        switch (reg.Register)
                        {
                            case 0x401d:
                            case 0x401f:
                            case 0x9030:
                            case 0xE000:
                            case int expression when (reg.Register < 0x401c) || (reg.Register < 0x409f && reg.Register > 0x401F):
                                fileLenght = fileLenght + 3;
                                break;
                        }
                    }
                    fileLenght = fileLenght + sampleData.Length + 9;
                    header.gd3Offset = fileLenght - 16;
                    fileLenght = fileLenght + gd3Lenght;
                    header.eofOffset = fileLenght; 
                    var headerBytes = new byte[sizeof(VgmHeader)];


                    Marshal.Copy(new IntPtr(&header), headerBytes, 0, headerBytes.Length);
                    file.Write(headerBytes, 0, headerBytes.Length);

                    //ExtraHeader
                    file.Write(BitConverter.GetBytes(0x0000000c), 0, 4); //extra header size 12bit
                    file.Write(BitConverter.GetBytes(0x00000000), 0, 4); //extra clock offset
                    file.Write(BitConverter.GetBytes(0x00000004), 0, 4); //extra volume offset
                    file.Write(BitConverter.GetBytes(0x04), 0, 1); //chip amount

                    file.Write(BitConverter.GetBytes(0x01), 0, 1); //chip id ym2314
                    file.Write(BitConverter.GetBytes(0x80), 0, 1); // flags VRC7
                    file.Write(BitConverter.GetBytes(0x0800), 0, 2); //volume bit 7 for absolute 8.8 fixed point

                    file.Write(BitConverter.GetBytes(0x12), 0, 1); //chip id ym2149
                    file.Write(BitConverter.GetBytes(0x00), 0, 1); // flags
                    file.Write(BitConverter.GetBytes(0x8200), 0, 2); //volume bit 7 for absolute 8.8 fixed point

                    file.Write(BitConverter.GetBytes(0x07), 0, 1); //chip id ym2608
                    file.Write(BitConverter.GetBytes(0x00), 0, 1); // flags
                    file.Write(BitConverter.GetBytes(0x0140), 0, 2); //volume bit 7 for absolute 8.8 fixed point

                    file.Write(BitConverter.GetBytes(0x87), 0, 1); //chip id ym2608ssg
                    file.Write(BitConverter.GetBytes(0x00), 0, 1); // flags
                    file.Write(BitConverter.GetBytes(0x8140), 0, 2); //volume bit 7 for absolute 8.8 fixed point


                    //sampledata
                    file.Write(BitConverter.GetBytes(0x67), 0, sizeof(byte));
                    file.Write(BitConverter.GetBytes(0x66), 0, sizeof(byte));
                    file.Write(BitConverter.GetBytes(0xC2), 0, sizeof(byte));
                    file.Write(BitConverter.GetBytes(sampleData.Length+2), 0, sizeof(int));
                    file.Write(BitConverter.GetBytes(0xc000), 0, sizeof(short));
                    file.Write(sampleData, 0, sampleData.Length);


                    var sr = new StreamWriter(file);
                    // So lame.
                    int chipData = 0;
                    frameNumber = 0;
                    //Inits
                    //2a03
                    file.Write(BitConverter.GetBytes(0xB4), 0, sizeof(byte));
                    file.Write(BitConverter.GetBytes(0x15), 0, sizeof(byte));
                    file.Write(BitConverter.GetBytes(0x0f), 0, sizeof(byte));
                    file.Write(BitConverter.GetBytes(0xB4), 0, sizeof(byte));
                    file.Write(BitConverter.GetBytes(0x08), 0, sizeof(byte));
                    file.Write(BitConverter.GetBytes(0x80), 0, sizeof(byte));
                    file.Write(BitConverter.GetBytes(0xB4), 0, sizeof(byte));
                    file.Write(BitConverter.GetBytes(0x0f), 0, sizeof(byte));
                    file.Write(BitConverter.GetBytes(0x00), 0, sizeof(byte));
                    file.Write(BitConverter.GetBytes(0xB4), 0, sizeof(byte));
                    file.Write(BitConverter.GetBytes(0x00), 0, sizeof(byte));
                    file.Write(BitConverter.GetBytes(0x30), 0, sizeof(byte));
                    file.Write(BitConverter.GetBytes(0xB4), 0, sizeof(byte));
                    file.Write(BitConverter.GetBytes(0x04), 0, sizeof(byte));
                    file.Write(BitConverter.GetBytes(0x30), 0, sizeof(byte));
                    file.Write(BitConverter.GetBytes(0xB4), 0, sizeof(byte));
                    file.Write(BitConverter.GetBytes(0x0c), 0, sizeof(byte));
                    file.Write(BitConverter.GetBytes(0x30), 0, sizeof(byte));
                    file.Write(BitConverter.GetBytes(0xB4), 0, sizeof(byte));
                    file.Write(BitConverter.GetBytes(0x01), 0, sizeof(byte));
                    file.Write(BitConverter.GetBytes(0x08), 0, sizeof(byte));
                    file.Write(BitConverter.GetBytes(0xB4), 0, sizeof(byte));
                    file.Write(BitConverter.GetBytes(0x05), 0, sizeof(byte));
                    file.Write(BitConverter.GetBytes(0x08), 0, sizeof(byte));
                    //s5b
                    file.Write(BitConverter.GetBytes(0xA0), 0, sizeof(byte));
                    file.Write(BitConverter.GetBytes(0x07), 0, sizeof(byte));
                    file.Write(BitConverter.GetBytes(0x38), 0, sizeof(byte));
                    file.Write(BitConverter.GetBytes(0x56), 0, sizeof(byte));
                    //epsm
                    file.Write(BitConverter.GetBytes(0x07), 0, sizeof(byte));
                    file.Write(BitConverter.GetBytes(0x38), 0, sizeof(byte));
                    file.Write(BitConverter.GetBytes(0x56), 0, sizeof(byte));
                    file.Write(BitConverter.GetBytes(0x29), 0, sizeof(byte));
                    file.Write(BitConverter.GetBytes(0x80), 0, sizeof(byte));
                    file.Write(BitConverter.GetBytes(0x56), 0, sizeof(byte));
                    file.Write(BitConverter.GetBytes(0x27), 0, sizeof(byte));
                    file.Write(BitConverter.GetBytes(0x00), 0, sizeof(byte));
                    file.Write(BitConverter.GetBytes(0x56), 0, sizeof(byte));
                    file.Write(BitConverter.GetBytes(0x11), 0, sizeof(byte));
                    file.Write(BitConverter.GetBytes(0x3f), 0, sizeof(byte));
                    foreach (var reg in writes)
                    {
                        while (frameNumber < reg.FrameNumber)
                        {
                            frameNumber++;
                            file.Write(BitConverter.GetBytes(0x62), 0, sizeof(byte));
                        }
                        if (reg.Register == 0x401c)
                        {
                            chipData = reg.Value;
                        }
                        else if (reg.Register == 0x401e)
                        {
                            chipData = reg.Value;
                        }
                        else if (reg.Register == 0x9010)
                        {
                            chipData = reg.Value;
                        }
                        else if (reg.Register == 0xC000)
                        {
                            chipData = reg.Value;
                        }
                        else if (reg.Register == 0x401d)
                        {
                            file.Write(BitConverter.GetBytes(0x56), 0, sizeof(byte));
                            file.Write(BitConverter.GetBytes(chipData), 0, sizeof(byte));
                            file.Write(BitConverter.GetBytes(reg.Value), 0, sizeof(byte));
                        }
                        else if (reg.Register == 0x401f)
                        {
                            file.Write(BitConverter.GetBytes(0x57), 0, sizeof(byte));
                            file.Write(BitConverter.GetBytes(chipData), 0, sizeof(byte));
                            file.Write(BitConverter.GetBytes(reg.Value), 0, sizeof(byte));
                        }
                        else if (reg.Register == 0x9030)
                        {
                            file.Write(BitConverter.GetBytes(0x51), 0, sizeof(byte));
                            file.Write(BitConverter.GetBytes(chipData), 0, sizeof(byte));
                            file.Write(BitConverter.GetBytes(reg.Value), 0, sizeof(byte));
                        }
                        else if (reg.Register == 0xE000)
                        {
                            file.Write(BitConverter.GetBytes(0xA0), 0, sizeof(byte));
                            file.Write(BitConverter.GetBytes(chipData), 0, sizeof(byte));
                            file.Write(BitConverter.GetBytes(reg.Value), 0, sizeof(byte));
                        }
                        else if ((reg.Register < 0x401c) || (reg.Register < 0x409f && reg.Register > 0x401F))
                        {
                            file.Write(BitConverter.GetBytes(0xb4), 0, sizeof(byte));
                            if ((reg.Register <= 0x401F) || (reg.Register <= 0x407f && reg.Register >= 0x4040))
                                file.Write(BitConverter.GetBytes(reg.Register & 0xFF), 0, sizeof(byte));
                            else if (reg.Register >= 0x4080)
                                file.Write(BitConverter.GetBytes((reg.Register - 0x60) & 0xFF), 0, sizeof(byte));
                            else if (reg.Register == 0x4023)
                                file.Write(BitConverter.GetBytes(0x3F), 0, sizeof(byte));
                            file.Write(BitConverter.GetBytes(reg.Value), 0, sizeof(byte));
                        }
                    }
                    file.Write(BitConverter.GetBytes(0x66), 0, sizeof(byte));

                    for (int i = 0; i < gd3.Length; i++)
                    {
                        file.Write(BitConverter.GetBytes(gd3[i]), 0, sizeof(byte));
                    }
                    file.Write(BitConverter.GetBytes(0x00000100), 0, sizeof(uint)); //version

                    file.Write(BitConverter.GetBytes(gd3Lenght), 0, sizeof(uint)); //gd3Lenght
                    for (int i = 0; i < songName.Length; i++)
                    {
                        file.Write(BitConverter.GetBytes(songName[i]), 0, sizeof(byte));
                        file.Write(BitConverter.GetBytes(0), 0, sizeof(byte));
                    }
                    file.Write(BitConverter.GetBytes(0), 0, sizeof(short));
                    for (int i = 0; i < gameName.Length; i++)
                    {
                        file.Write(BitConverter.GetBytes(gameName[i]), 0, sizeof(byte));
                        file.Write(BitConverter.GetBytes(0), 0, sizeof(byte));
                    }
                    file.Write(BitConverter.GetBytes(0), 0, sizeof(short));
                    for (int i = 0; i < systemName.Length; i++)
                    {
                        file.Write(BitConverter.GetBytes(systemName[i]), 0, sizeof(byte));
                        file.Write(BitConverter.GetBytes(0), 0, sizeof(byte));
                    }
                    file.Write(BitConverter.GetBytes(0), 0, sizeof(short));
                    for (int i = 0; i < author.Length; i++)
                    {
                        file.Write(BitConverter.GetBytes(author[i]), 0, sizeof(byte));
                        file.Write(BitConverter.GetBytes(0), 0, sizeof(byte));
                    }
                    file.Write(BitConverter.GetBytes(0), 0, sizeof(short));
                    file.Write(BitConverter.GetBytes(0), 0, sizeof(short));
                    file.Write(BitConverter.GetBytes(0), 0, sizeof(short));
                    file.Write(BitConverter.GetBytes(0), 0, sizeof(short));
                    //var howManyBytes = test.Length * sizeof(Char);
                    //sr.WriteLine($" ;end of file");
                    sr.Flush();
                    sr.Close();
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
                    sr.WriteLine("MMC5_WRITE = 8");
                    sr.WriteLine("LOOP_VGM = 9");
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
                        /*else if ((lastReg != reg.Register && lastReg != 0x2a03) || (((reg.Register <= 0x401F) || (reg.Register <= 0x407f && reg.Register >= 0x4040)) && lastReg != 0x2a03))
                        {
                            if (lastReg != 0)
                            {
                                sr.WriteLine(writeCommandByte + $"${repeatingReg:X2}" + writeByteStream);
                            }
                        }*/
                        if (reg.Register == 0x401d)
                        {
                            if (lastReg == reg.Register && repeatingReg < 255)
                            {
                                writeByteStream = writeByteStream + $", ${chipData:X2}, ${reg.Value:X2}";
                                repeatingReg++;
                            }
                            else
                            {
                                if(lastReg!=0)
                                    sr.WriteLine(writeCommandByte + $"${repeatingReg:X2}" + writeByteStream);
                                writeCommandByte = ".byte EPSM_A0_WRITE,";
                                writeByteStream = $", ${chipData:X2}, ${reg.Value:X2}";
                                lastReg = reg.Register;
                                repeatingReg = 1;
                            }
                            //sr.WriteLine($".byte EPSM_A0_WRITE, $02, ${chipData:X2}, ${reg.Value:X2}");
                            //sr.WriteLine($".byte ${((chipData & 0xF0) | 0x2):x2} ,${((chipData & 0x0F) << 4):x2} ,${((reg.Value & 0xF0) | 0xA):x2} ,${((reg.Value & 0x0F) << 4):x2}");
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
                            //sr.WriteLine($".byte EPSM_A1_WRITE, $02, ${chipData:X2}, ${reg.Value:X2}");
                            //sr.WriteLine($".byte ${((chipData & 0xF0) | 0x6):x2} ,${((chipData & 0x0F) << 4):x2} ,${((reg.Value & 0xF0) | 0xE):x2} ,${((reg.Value & 0x0F) << 4):x2}");
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
                            //sr.WriteLine($".byte VRC7_WRITE, $02, ${chipData:X2}, ${reg.Value:X2}");
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
                            //sr.WriteLine($".byte S5B_WRITE, $02, ${chipData:X2}, ${reg.Value:X2}");
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
                            //sr.WriteLine($".byte S5B_WRITE, $02, ${chipData:X2}, ${reg.Value:X2}");
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
                            //sr.WriteLine($".byte APU_WRITE, $02, ${(reg.Register & 0xff):X2}, ${reg.Value:X2}");
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
                            //sr.WriteLine($".byte APU_WRITE, $02, ${(reg.Register & 0xff):X2}, ${reg.Value:X2}");
                        }
                    }
                    sr.WriteLine(writeCommandByte + $"${repeatingReg:X2}" + writeByteStream);
                    sr.WriteLine($" .segment \"DPCM\"");
                    var i = 0;
                    string dpcmData = "";
                    foreach(var sample in sampleData)
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
