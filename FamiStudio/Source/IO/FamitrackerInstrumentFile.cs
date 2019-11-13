using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace FamiStudio
{
    class FamitrackerInstrumentFile
    {
        // FTI instruments files
        static readonly string INST_HEADER = "FTI";
        static readonly string INST_VERSION = "2.4";
        public static readonly int MAX_SEQUENCE_ITEMS = /*128*/ 253;
        public static readonly int MAX_SEQUENCES = 128;
        public static readonly int OCTAVE_RANGE = 8;
        enum Inst_Type_t
        {
            INST_NONE = 0,
            INST_2A03 = 1,
            INST_VRC6,
            INST_VRC7,
            INST_FDS,
            INST_N163,
            INST_S5B
        };
        public enum Sequence_t
        {
            SEQ_VOLUME,
            SEQ_ARPEGGIO,
            SEQ_PITCH,
            SEQ_HIPITCH,        // TODO: remove this eventually
            SEQ_DUTYCYCLE,

            SEQ_COUNT
        };
        public static readonly Dictionary<Sequence_t, int> SequenceToEnvelope = new Dictionary<Sequence_t, int>()
        {
            {Sequence_t.SEQ_VOLUME ,Envelope.Volume},
            {Sequence_t.SEQ_ARPEGGIO ,Envelope.Arpeggio},
            {Sequence_t.SEQ_PITCH ,Envelope.Pitch},

        };
        public static Instrument Load(int uniqueId, string filename)
        {
            var bytes = System.IO.File.ReadAllBytes(filename);
            var version = -1;
            if (!CheckFormat(bytes,out version))
            {
               
                return null;
            }
            var instType = GetInstrumentType(bytes,out var offsetIndex);
            if (instType == Inst_Type_t.INST_NONE)
            {
                instType = Inst_Type_t.INST_2A03;
            }
            switch (instType)
            {
                case Inst_Type_t.INST_NONE:
                    break;
                case Inst_Type_t.INST_2A03:
                    return new ConvertInstrument2A03().Convert(uniqueId, version, bytes, offsetIndex);//ConvertInstrument2A03(uniqueId, bytes);
                case Inst_Type_t.INST_VRC6:
                    break;
                case Inst_Type_t.INST_VRC7:
                    break;
                case Inst_Type_t.INST_FDS:
                    break;
                case Inst_Type_t.INST_N163:
                    break;
                case Inst_Type_t.INST_S5B:
                    break;
            }
            return null;
        }
        private static bool CheckFormat( byte[] inputdata ,out int iVersion)
        {
            //outdata = new string[0];
            //if(inputdata.Length != 1)
            //{//invalid format

            //    return false;
            //}
            ////str length == 1
            ///
            iVersion = -1;
            var targetHeaderByte = Encoding.ASCII.GetBytes(INST_HEADER);
            var currentVersionByte = Encoding.ASCII.GetBytes(INST_VERSION);
            if (inputdata.Length < targetHeaderByte.Length + currentVersionByte.Length + 1)
            {
                return false;
            }
            var headerByte = new byte[targetHeaderByte.Length];
            var versionByte = new byte[currentVersionByte.Length];
            Array.Copy(inputdata, headerByte, headerByte.Length);
            Array.Copy(inputdata, headerByte.Length , versionByte, 0 , currentVersionByte.Length);
            //check header
            if (!CompareByteArray(headerByte,targetHeaderByte))
            {
                return false;
            }

            //check version
            float currentVersion = float.Parse(INST_VERSION);
            var fileVersionString = Encoding.ASCII.GetString(versionByte);
            float fileVersion = float.MaxValue;
            try
            {
                fileVersion = float.Parse(fileVersionString);
            }
            catch
            {//error handle

            }
            
            if (fileVersion > currentVersion)
            {//version not supported
                return false;
            }
            iVersion = (int)(fileVersion * 10);
            return true;

        }
        private static Inst_Type_t GetInstrumentType( byte[] inputdata,out int offset)
        {
            var targetHeaderByte = Encoding.ASCII.GetBytes(INST_HEADER);
            var currentVersionByte = Encoding.ASCII.GetBytes(INST_VERSION);
            var instTypeByte = inputdata[targetHeaderByte.Length + currentVersionByte.Length];
            offset = targetHeaderByte.Length + currentVersionByte.Length + 1;
            if (inputdata.Length < targetHeaderByte.Length + currentVersionByte.Length + 1)
            {
                return Inst_Type_t.INST_NONE;
            }
            return (Inst_Type_t)instTypeByte;
        }


        private static bool CompareByteArray(byte[] a1,byte[] a2)
        {
            if(a1.Length != a2.Length)
            {
                return false;
            }
            var length = a1.Length;
            for(int i = 0; i < length; i++)
            {
                if(a1[i] != a2[i])
                {
                    return false;
                }
            }
            return true;

        }
    }
    abstract class ConvertInstrument
    {
        protected int idx = 0;
        protected byte[] data;
        protected int[] m_iSeqEnable = new int[(int)FamitrackerInstrumentFile.Sequence_t.SEQ_COUNT];
        protected int[] m_iSeqIndex = new int[(int)FamitrackerInstrumentFile.Sequence_t.SEQ_COUNT];
        protected sbyte[,] m_cSamples = new sbyte[FamitrackerInstrumentFile.OCTAVE_RANGE,12];   // Samples
        protected sbyte[,] m_cSamplePitch = new sbyte[FamitrackerInstrumentFile.OCTAVE_RANGE, 12];// Play pitch/loop
        protected sbyte[,] m_cSampleLoopOffset = new sbyte[FamitrackerInstrumentFile.OCTAVE_RANGE, 12];// Loop offset
        protected sbyte[,] m_cSampleDelta = new sbyte[FamitrackerInstrumentFile.OCTAVE_RANGE, 12];// Delta setting

        public Instrument Convert(int uniqueId, int iVersion ,byte[] data, int idx)
        {
            this.idx = idx;
            this.data = data;
            return Convert(uniqueId, iVersion);
        }
        protected abstract Instrument Convert(int uniqueId, int iVersion);

        protected string GetName()
        {
            byte[] temp;
            if (!ReadByte(sizeof(int), out temp))
            {
                return "";
            }
            
            var nameLength = BitConverter.ToInt32(temp, 0);
            if (nameLength >= 256)
            {
                return "";
            }
            if (!ReadByte(nameLength, out temp))
            {
                return "";
            }
            return Encoding.ASCII.GetString(temp);

        }

        protected bool ReadByte(int length,out byte[] result)
        {
            result = new byte[length];
            var beforeIdx = idx;

            if (!AddIndex(length))
            {
                return false;
            }
            Array.Copy(data, beforeIdx, result, 0, length);
            return true;
        }
        bool AddIndex(int additional)
        {
            idx += additional;
            if (data.Length < idx)
            {
                return false;
            }
            return true;
        }
        protected sbyte ToSByte(byte val)
        {
            if(val >= 128)
            {
                return (sbyte)(val - 256);
            }
            return (sbyte)val;
        }
        ////this is GetFreeSequence of FamiTracker
        //protected int GetEnvelopeExist(int type)
        //{
        //    var seq = (FamitrackerInstrumentFile.Sequence_t)type;
        //    if (FamitrackerInstrumentFile.SequenceToEnvelope.ContainsKey(seq))
        //    {
        //    }
        //}
        //this is GetSequence of FamiTracker
        protected int GetEnvelopeIndex(int type)
        {
            var seq = (FamitrackerInstrumentFile.Sequence_t)type;
            if (FamitrackerInstrumentFile.SequenceToEnvelope.ContainsKey(seq))
            {
                var idx = FamitrackerInstrumentFile.SequenceToEnvelope[seq];
                return idx;
            }
            return -1;
        }
    }
    class ConvertInstrument2A03 : ConvertInstrument
    {
        const int SEQUENCE_COUNT = 5;
        public ConvertInstrument2A03()
        {
	        for (int i = 0; i<SEQUENCE_COUNT; ++i) {
		        m_iSeqEnable[i] = 0;
		        m_iSeqIndex[i] = 0;
	        }

	        for (int i = 0; i< FamitrackerInstrumentFile.OCTAVE_RANGE; ++i) {
		        for (int j = 0; j< 12; ++j) {
		        	m_cSamples[i,j] = 0;
		        	m_cSamplePitch[i,j] = 0;
		        	m_cSampleLoopOffset[i,j] = 0;
		        	m_cSampleDelta[i,j] = -1;
		        }
	        }
        }
        protected override Instrument Convert(int uniqueId,int iVersion)
        {
            var name = GetName();
            var instrument = new Instrument(uniqueId, name);
            byte[] temp;
            var intSize = sizeof(int);
            var byteSize = 1;
            if (!ReadByte(byteSize, out temp))
                return null;
            
            byte seqCount = temp[0];
            for (int i = 0; i < seqCount; i++)
            {
                if (!ReadByte(byteSize, out temp))
                    return null;
                var enabled = temp[0];
                if (enabled == 1)
                {
                    if (!ReadByte(intSize, out temp))
                        return null;
                    
                    var count = BitConverter.ToInt32(temp, 0);
                    if(count < 0 || FamitrackerInstrumentFile.MAX_SEQUENCE_ITEMS < count)
                        return null;
                    var idx = GetEnvelopeIndex(i);
                    if(idx != -1)
                    {
                        if(iVersion < 20)
                        {
                            //todo support old version
                            return null;
                            var length = new sbyte[count];
                            var value = new sbyte[count];
                            for (int j = 0; j < count; ++j)
                            {
                                ReadByte(byteSize, out temp);
                                length[j] = System.Convert.ToSByte(temp[0]);
                                ReadByte(byteSize, out temp);
                                value[j] = System.Convert.ToSByte(temp[0]);
                            }
                        }
                        else
                        {
                            //length
                            instrument.Envelopes[idx].Length = count;
                            //loop setting
                            if (!ReadByte(intSize, out temp))
                                return null;
                            instrument.Envelopes[idx].Loop = BitConverter.ToInt32(temp, 0);
                            if (iVersion > 20)
                            {
                                //release
                                if (!ReadByte(intSize, out temp))
                                    return null;
                                instrument.Envelopes[idx].Release = BitConverter.ToInt32(temp, 0);
                            }
                            if (iVersion >= 23)
                            {
                                //arp_setting_t
                                //maybe famistudio not supported ( always absolute )
                                if (!ReadByte(intSize, out temp))
                                    return null;

                            }
                            for (int j = 0; j < count; ++j)
                            {
                                if (!ReadByte(byteSize, out temp))
                                    return null;
                                instrument.Envelopes[idx].Values[j] = ToSByte(temp[0]);


                            }
                            
                        }
                    }
                }
                else
                {

                }
            }

            return instrument;
        }
    }
}
