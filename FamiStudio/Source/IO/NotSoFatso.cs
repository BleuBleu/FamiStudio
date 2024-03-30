using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace FamiStudio
{
    public static class NotSoFatso
    {
        private const string NotSoFatsoDll = Platform.DllPrefix + "NotSoFatso" + Platform.DllExtension;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void WriteRegisterDelegate(int addr, int data);

        [DllImport(NotSoFatsoDll, CallingConvention = CallingConvention.StdCall)]
        public extern static IntPtr NsfOpen(string file);

        [DllImport(NotSoFatsoDll, CallingConvention = CallingConvention.StdCall)]
        public extern static int NsfGetTrackCount(IntPtr nsf);

        [DllImport(NotSoFatsoDll, CallingConvention = CallingConvention.StdCall)]
        public extern static int NsfIsPal(IntPtr nsf);

        [DllImport(NotSoFatsoDll, CallingConvention = CallingConvention.StdCall)]
        public extern static int NsfGetClockSpeed(IntPtr nsf);

        [DllImport(NotSoFatsoDll, CallingConvention = CallingConvention.StdCall)]
        public extern static int NsfGetExpansion(IntPtr nsf);

        [DllImport(NotSoFatsoDll, CallingConvention = CallingConvention.StdCall)]
        public extern static IntPtr NsfGetTitle(IntPtr nsf);

        [DllImport(NotSoFatsoDll, CallingConvention = CallingConvention.StdCall)]
        public extern static IntPtr NsfGetArtist(IntPtr nsf);

        [DllImport(NotSoFatsoDll, CallingConvention = CallingConvention.StdCall)]
        public extern static IntPtr NsfGetCopyright(IntPtr nsf);

        [DllImport(NotSoFatsoDll, CallingConvention = CallingConvention.StdCall)]
        public extern static IntPtr NsfGetTrackName(IntPtr nsf, int track);

        [DllImport(NotSoFatsoDll, CallingConvention = CallingConvention.StdCall)]
        public extern static int NsfGetTrackDuration(IntPtr nsf, int track);

        [DllImport(NotSoFatsoDll, CallingConvention = CallingConvention.StdCall)]
        public extern static void NsfClose(IntPtr nsf);

        [DllImport(NotSoFatsoDll, CallingConvention = CallingConvention.StdCall)]
        public extern static void NsfSetTrack(IntPtr nsf, int track);

        [DllImport(NotSoFatsoDll, CallingConvention = CallingConvention.StdCall)]
        public extern static int NsfRunFrame(IntPtr nsf);

        [DllImport(NotSoFatsoDll, CallingConvention = CallingConvention.StdCall)]
        public extern static int NsfGetState(IntPtr nsf, int channel, int state, int sub);

        [DllImport(NotSoFatsoDll, CallingConvention = CallingConvention.StdCall)]
        public extern static int NsfSetApuWriteCallback(IntPtr nsf, [MarshalAs(UnmanagedType.FunctionPtr)] WriteRegisterDelegate cb);

        public const int EXTSOUND_VRC6  = 0x01;
        public const int EXTSOUND_VRC7  = 0x02;
        public const int EXTSOUND_FDS   = 0x04;
        public const int EXTSOUND_MMC5  = 0x08;
        public const int EXTSOUND_N163  = 0x10;
        public const int EXTSOUND_S5B   = 0x20;
        public const int EXTSOUND_EPSM  = 0x80;

        public const int STATE_VOLUME             = 0;
        public const int STATE_PERIOD             = 1;
        public const int STATE_DUTYCYCLE          = 2;
        public const int STATE_DPCMSAMPLELENGTH   = 3;
        public const int STATE_DPCMSAMPLEADDR     = 4;
        public const int STATE_DPCMSAMPLEDATA     = 5;
        public const int STATE_DPCMLOOP           = 6;
        public const int STATE_DPCMPITCH          = 7;
        public const int STATE_DPCMCOUNTER        = 8;
        public const int STATE_DPCMACTIVE         = 9;
        public const int STATE_FDSWAVETABLE       = 10;
        public const int STATE_FDSMODULATIONTABLE = 11;
        public const int STATE_FDSMODULATIONDEPTH = 12;
        public const int STATE_FDSMODULATIONSPEED = 13;
        public const int STATE_FDSMASTERVOLUME    = 14;
        public const int STATE_VRC7PATCH          = 15;
        public const int STATE_FMPATCHREG         = 16;
        public const int STATE_FMOCTAVE           = 17;
        public const int STATE_FMTRIGGER          = 18;
        public const int STATE_FMTRIGGERCHANGE    = 19;
        public const int STATE_FMSUSTAIN          = 20;
        public const int STATE_N163WAVEPOS        = 21;
        public const int STATE_N163WAVESIZE       = 22;
        public const int STATE_N163WAVE           = 23;
        public const int STATE_N163NUMCHANNELS    = 24;
        public const int STATE_S5BMIXER           = 25;
        public const int STATE_S5BNOISEFREQUENCY  = 26;
        public const int STATE_S5BENVFREQUENCY    = 27;
        public const int STATE_S5BENVSHAPE        = 28;
        public const int STATE_S5BENVTRIGGER      = 29;
        public const int STATE_S5BENVENABLED      = 30;
        public const int STATE_STEREO             = 31;
    }
}
