using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace FamiStudio
{
    public class VideoPreviewDialog : PropertyDialog, IVideoEncoder
    {
        public VideoPreviewDialog(FamiStudioWindow win, int resX, int resY) : base(win, "", 800, true, false)
        {
            Localization.Localize(this);
            Properties.AddImage(null, resX, resY);
            Properties.Build();
        }

        public void AddFrame(OffscreenGraphics graphics)
        {
            Properties.SetPropertyValue(0, graphics.GetTexture());
        }

        public bool BeginEncoding(int resX, int resY, int rateNumer, int rateDenom, int videoBitRate, int audioBitRate, bool stereo, string audioFile, string outputFile)
        {
            return true;
        }

        public void EndEncoding(bool abort)
        {
        }
    }
}
