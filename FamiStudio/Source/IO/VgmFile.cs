using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

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
        public unsafe static void Save(Song song, string filename, int filetype)
        {
            var project = song.Project;
            var regPlayer = new RegisterPlayer(song.Project.OutputsStereoAudio);
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
                if (project.UsesVrc7Expansion) { header.ym2413clock = 3579545 + 0x80000000; }
                    
                if (project.UsesEPSMExpansion) { header.YM2608clock = 8000000; }
                if (project.UsesFdsExpansion) { header.NESAPUclock = 1789772 + 0x80000000; }
                else { header.NESAPUclock = 1789772; }
                if (project.UsesS5BExpansion) {
                    header.AY8910clock = 1789772;
                    header.AY8910ChipType = 0x10;
                }
                header.vgmDataOffset = 0x8C+29;
                header.totalSamples = lastWrite.FrameNumber*735;
                header.rate = 60;
                header.ExtraHeaderOffset = 0x4;
                
                string gd3 = "Gd3 ";
                string songName = song.Name + "\0";
                string gameName = song.Project.Name + "\0";
                string systemName = "NES/Famicom FamiStudio Export\0";
                string author = song.Project.Author + "\0";
                int gd3Lenght = gd3.Length + (songName.Length * 2) + (gameName.Length * 2) + (systemName.Length * 2) + (author.Length * 2) + 2 + 2 + 2 + 2 + 2 + 4 + 4 + 4;

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
                    file.Write(BitConverter.GetBytes(0x37), 0, sizeof(byte));
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
                                if(lastReg!=0)
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

        private Song song;
        private Project project;
        private ChannelState[] channelStates;
        private bool preserveDpcmPadding;
        private readonly int[] DPCMOctaveOrder = new[] { 4, 5, 3, 6, 2, 7, 1, 0 };

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

        private Pattern GetOrCreatePattern(Channel channel, int patternIdx)
        {
            if (channel.PatternInstances[patternIdx] == null)
                channel.PatternInstances[patternIdx] = channel.CreatePattern();
            return channel.PatternInstances[patternIdx];
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

            if (expansion == ExpansionType.Vrc6)
                instrument.Vrc6SawMasterVolume = Vrc6SawMasterVolumeType.Full;

            return instrument;
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

        private Instrument GetS5BInstrument(int noise, int mixer)
        {
            var name = "S5B";
            if (mixer != 2 && noise == 0)
                noise = 1;
            if (mixer != 2)
                name = $"S5B Noise {noise} M {mixer}";

            var instrument = project.GetInstrument(name);
            if (instrument == null)
            {
                instrument = project.CreateInstrument(ExpansionType.S5B, name);
                instrument.Envelopes[EnvelopeType.YMNoiseFreq].Length = 1;
                instrument.Envelopes[EnvelopeType.YMNoiseFreq].Values[0] = (sbyte)noise;
                instrument.Envelopes[EnvelopeType.YMMixerSettings].Length = 1;
                instrument.Envelopes[EnvelopeType.YMMixerSettings].Values[0] = (sbyte)mixer;
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

        private Instrument GetEPSMInstrument(byte chanType, byte[] patchRegs, int noise, int mixer)
        {
            var name = $"EPSM {Instrument.GetEpsmPatchName(1)}";
            var instrument = project.GetInstrument(name);
            var stereo = "";
            if (patchRegs[1] == 0x80)
                stereo = " Left";
            if (patchRegs[1] == 0x40)
                stereo = " Right";

            if (chanType == 0)
            {
                if (mixer != 2 && noise == 0)
                    noise = 1;
                if (mixer != 2)
                    name = $"EPSM Noise {noise} M {mixer}";

                instrument = project.GetInstrument(name);
                if (instrument == null)
                {
                    instrument = project.CreateInstrument(ExpansionType.EPSM, name);

                    instrument.EpsmPatch = 1;
                    instrument.Envelopes[EnvelopeType.YMNoiseFreq].Length = 1;
                    instrument.Envelopes[EnvelopeType.YMNoiseFreq].Values[0] = (sbyte)noise;
                    instrument.Envelopes[EnvelopeType.YMMixerSettings].Length = 1;
                    instrument.Envelopes[EnvelopeType.YMMixerSettings].Values[0] = (sbyte)mixer;
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
            foreach (var inst in project.Instruments)
            {
                if (inst.IsEpsm)
                    return inst;
            }
        }

        public Project Load(string filename, int patternLength)
        {
            var vgmFile = System.IO.File.ReadAllBytes(filename);
            if (!vgmFile.Skip(0).Take(4).SequenceEqual(Encoding.ASCII.GetBytes("Vgm ")))
            {
                Log.LogMessage(LogSeverity.Error, "Incompatible file.");
                return null;
            }
            if (!vgmFile.Skip(8).Take(4).SequenceEqual(BitConverter.GetBytes(0x00000170)))
            {  
                Log.LogMessage(LogSeverity.Error, "Not version 1.70");
                return null;
            }


            project = new Project();
            var instrument = (Instrument)null;
            project.Name = "VGM Import";
            project.Author = "unknown";
            project.Copyright = "";
            project.PalMode = false;
            var songName = "VGM Import";
            song = project.CreateSong(songName);
            instrument = project.CreateInstrument(0, "instrument 2a03");
            var p = 0;
            var n = 0;
            channelStates = new ChannelState[song.Channels.Length];
            for (int i = 0; i < song.Channels.Length; i++)
                channelStates[i] = new ChannelState();


            var vgmDataOffset = BitConverter.ToInt32(vgmFile.Skip(0x34).Take(4).ToArray())+0x34;
            Log.LogMessage(LogSeverity.Info, "VGM Data Startoffset: " + vgmDataOffset);
            var vgmData = vgmFile.Skip(vgmDataOffset).Take(1).ToArray();
            var chipCommands = 0;
            var samples = 0;
            var frame = 0;
            int[] apuRegister = new int[0xff];
            while (vgmDataOffset < vgmFile.Length) {
                if (vgmData[0] == 0x67)  //DataBlock
                {
                    Log.LogMessage(LogSeverity.Info, "DataBlock Size: " + BitConverter.ToInt32(vgmFile.Skip(vgmDataOffset + 3).Take(4).ToArray()));
                    vgmDataOffset = vgmDataOffset + BitConverter.ToInt32(vgmFile.Skip(vgmDataOffset + 3).Take(4).ToArray()) + 3 + 4;
                }
                else if (vgmData[0] == 0x66)
                {
                    vgmDataOffset = vgmDataOffset + 1;
                    Log.LogMessage(LogSeverity.Info, "VGM Data End");
                    break;
                }

                else if (vgmData[0] == 0x62 || vgmData[0] == 0x63)
                {
                    vgmDataOffset = vgmDataOffset + 1;
                    frame++;

                    p = frame / song.PatternLength;
                    n = frame % song.PatternLength;
                    song.SetLength(p + 1);
                    var channel = song.Channels[0];

                    var pattern = GetOrCreatePattern(channel, p);
                    var newNote = pattern.GetOrCreateNoteAt(n);
                    var noteTable = NesApu.GetNoteTableForChannelType(ChannelType.Square1, project.PalMode, project.ExpansionNumN163Channels);
                    int period = (int)apuRegister[2] + (int)((apuRegister[3]& 0x7) << 8);
                    newNote.Value = (byte)GetBestMatchingNote(period, noteTable, out int finePitch);
                    newNote.Instrument = instrument;
                    Log.LogMessage(LogSeverity.Info, "Period high value: " + apuRegister[0] + " " + apuRegister[1] + " " + apuRegister[2].ToString("X") + " " + apuRegister[3].ToString("X") + " " + newNote.Value + " " + period);

                    channel = song.Channels[1];

                    pattern = GetOrCreatePattern(channel, p);
                    newNote = pattern.GetOrCreateNoteAt(n);
                    noteTable = NesApu.GetNoteTableForChannelType(ChannelType.Square2, project.PalMode, project.ExpansionNumN163Channels);
                    period = (int)apuRegister[6] + (int)((apuRegister[7] & 0x7) << 8);
                    newNote.Value = (byte)GetBestMatchingNote(period, noteTable, out finePitch);
                    newNote.Instrument = instrument;
                    channel = song.Channels[2];

                    pattern = GetOrCreatePattern(channel, p);
                    newNote = pattern.GetOrCreateNoteAt(n);
                    noteTable = NesApu.GetNoteTableForChannelType(ChannelType.Triangle, project.PalMode, project.ExpansionNumN163Channels);
                    period = (int)apuRegister[0xa] + (int)((apuRegister[0xb] & 0x7) << 8);
                    newNote.Value = (byte)GetBestMatchingNote(period, noteTable, out finePitch);
                    newNote.Instrument = instrument;
                    Log.LogMessage(LogSeverity.Info, "note: " + newNote);
                    //channelStates[0].period = 1;
                    //channelStates[0].state = 1;


                }
                else if (vgmData[0] == 0x61)
                {
                    samples = samples + BitConverter.ToInt32(vgmFile.Skip(vgmDataOffset + 1).Take(2).ToArray());
                    vgmDataOffset = vgmDataOffset + 3;
                    while (samples >= 735)
                    {
                        frame++;
                        samples = samples - 735;
                    }
                }
                else
                {

                    vgmData = vgmFile.Skip(vgmDataOffset).Take(3).ToArray();
                    if (vgmData[0] == 0xB4)
                    {
                        /*if (vgmData[1] == 0x01 && vgmData[2] == 0x87 && apuRegister[2] == 0xff)
                            apuRegister[0x03]++;
                        if (vgmData[1] == 0x05 && vgmData[2] == 0x87 && apuRegister[2] == 0xff)
                            apuRegister[0x07]++;
                        if (vgmData[1] == 0x09 && vgmData[2] == 0x87 && apuRegister[2] == 0xff)
                            apuRegister[0x0b]++;

                        if (vgmData[1] == 0x01 && vgmData[2] == 0x8f && apuRegister[2] == 0xff)
                            apuRegister[0x03]--;
                        if (vgmData[1] == 0x05 && vgmData[2] == 0x8f && apuRegister[2] == 0xff)
                            apuRegister[0x07]--;
                        if (vgmData[1] == 0x09 && vgmData[2] == 0x8f && apuRegister[2] == 0xff)
                            apuRegister[0x0b]--;*/

                        apuRegister[vgmData[1]] = vgmData[2];
                    }
                    //Log.LogMessage(LogSeverity.Info, "VGM Chip Data: " + Convert.ToHexString(vgmData));
                    chipCommands++;
                    vgmDataOffset = vgmDataOffset + 3;
                }
                vgmData = vgmFile.Skip(vgmDataOffset).Take(1).ToArray();
            }
            Log.LogMessage(LogSeverity.Info, "VGM Chip Commands: " + chipCommands);
            Log.LogMessage(LogSeverity.Info, "Frames: " + frame + " time: " + (frame/60) + "s");

            if (vgmFile.Skip(vgmDataOffset).Take(4).SequenceEqual(Encoding.ASCII.GetBytes("Gd3 ")))
            {
                vgmDataOffset = vgmDataOffset + 4+4+4; // "Gd3 " + version + gd3 length data
                var gd3Data = vgmFile.Skip(vgmDataOffset).Take(vgmFile.Length-vgmDataOffset).ToArray();
                var gd3DataArray = System.Text.Encoding.Unicode.GetString(gd3Data).Split("\0");
                Log.LogMessage(LogSeverity.Info, "Gd3 Data: " + System.Text.Encoding.Unicode.GetString(gd3Data));
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

            song.Name = songName;
            song.SetDefaultPatternLength(patternLength);
            song.SetSensibleBeatLength();
            song.ConvertToCompoundNotes();
            //song.DeleteEmptyPatterns();
            song.UpdatePatternStartNotes();
            song.InvalidateCumulativePatternCache();
            //project.DeleteUnusedInstruments();
            return project;
        }
    }


}
