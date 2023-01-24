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

        public const int STATE_VOLUME             = 0;
        public const int STATE_PERIOD             = 1;
        public const int STATE_DUTYCYCLE          = 2;
        public const int STATE_DPCMSAMPLELENGTH   = 3;
        public const int STATE_DPCMSAMPLEADDR     = 4;
        public const int STATE_DPCMSAMPLEDATA     = 5;
        public const int STATE_DPCMLOOP           = 6;
        public const int STATE_DPCMPITCH          = 7;
        public const int STATE_DPCMCOUNTER        = 8;
        public const int STATE_FDSWAVETABLE       = 9;
        public const int STATE_FDSMODULATIONTABLE = 10;
        public const int STATE_FDSMODULATIONDEPTH = 11;
        public const int STATE_FDSMODULATIONSPEED = 12;
        public const int STATE_FDSMASTERVOLUME    = 13;
        public const int STATE_VRC7PATCH          = 14;
        public const int STATE_VRC7PATCHREG       = 15;
        public const int STATE_VRC7OCTAVE         = 16;
        public const int STATE_VRC7TRIGGER        = 17;
        public const int STATE_VRC7SUSTAIN        = 18;
        public const int STATE_N163WAVEPOS        = 19;
        public const int STATE_N163WAVESIZE       = 20;
        public const int STATE_N163WAVE           = 21;
        public const int STATE_N163NUMCHANNELS    = 22;
        public const int STATE_S5BMIXER           = 23;
        public const int STATE_S5BNOISEFREQUENCY  = 24;
    }
}
