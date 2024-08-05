using System;
using System.IO;

namespace FamiStudio
{
    class VideoFileOscilloscope : VideoFileBase
    {
         public bool Save(VideoExportSettings settings)
        {
            if (!InitializeEncoder(settings))
                return false;

            var numCols = Math.Min(settings.OscNumColumns, channelStates.Length);
            var numRows = (int)Math.Ceiling(channelStates.Length / (float)numCols);

            var channelResXFloat = videoResX / (float)numCols;
            var channelResYFloat = videoResY / (float)numRows;

            var channelResX = (int)channelResXFloat;
            var channelResY = (int)channelResYFloat;

            // Tweak some cosmetic stuff that depends on resolution.
            var smallChannelText = channelResY < 128;
            var font = settings.OscLineThickness > 1 ?
                (smallChannelText ? fonts.FontMediumBold : fonts.FontVeryLargeBold) : 
                (smallChannelText ? fonts.FontMedium     : fonts.FontVeryLarge);
            var textOffsetY = smallChannelText ? 1 : 4;
            var channelLineWidth = settings.ResY >= 720 ? 5 : 3;

            LoadChannelIcons(!smallChannelText);

            return LaunchEncoderLoop((f) =>
            {
                var frame = metadata[f];
                var c = videoGraphics.DefaultCommandList;
                var o = videoGraphics.OverlayCommandList;

                videoGraphics.BeginDrawFrame(new Rectangle(0, 0, videoResX, videoResY), true, Theme.DarkGreyColor2);
                c.PushClipRegion(0, 0, videoResX, videoResY);

                // Channel names + oscilloscope
                for (int i = 0; i < channelStates.Length; i++)
                {
                    var s = channelStates[i];

                    var channelX = i % numCols;
                    var channelY = i / numCols;

                    var channelPosX0 = (channelX + 0) * channelResX;
                    var channelPosX1 = (channelX + 1) * channelResX;
                    var channelPosY0 = (channelY + 0) * channelResY;
                    var channelPosY1 = (channelY + 1) * channelResY;

                    c.PushTranslation(channelPosX0, channelPosY0);
                    c.PushClipRegion(0, 0, channelResX, channelResY);
                    
                    // Gradient
                    c.FillRectangleGradient(0, 0, channelResX, channelResY, Color.Black, Color.Invisible, true, channelResY / 2);

                    // Oscilloscope
                    var oscilloscope = UpdateOscilloscope(s, f);

                    c.PushTransform(0, channelResY / 2, channelPosX1 - channelPosX0, (channelPosY0 - channelPosY1) / 2);
                    c.DrawNiceSmoothLine(oscilloscope, frame.channelData[i].color, settings.OscLineThickness);
                    c.PopTransform();

                    // Icons + text
                    var channelIconPosX = s.icon.Size.Width / 2;
                    var channelIconPosY = s.icon.Size.Height / 2;

                    c.FillAndDrawRectangle(channelIconPosX, channelIconPosY, channelIconPosX + s.icon.Size.Width - 1, channelIconPosY + s.icon.Size.Height - 1, Theme.DarkGreyColor2, Theme.LightGreyColor1);
                    c.DrawTexture(s.icon, channelIconPosX, channelIconPosY, Theme.LightGreyColor1);
                    c.DrawText(s.channelText, font, channelIconPosX + s.icon.Size.Width + ChannelIconTextSpacing, channelIconPosY + textOffsetY, Theme.LightGreyColor1);

                    c.PopClipRegion();
                    c.PopTransform();
                }

                // Grid lines
                for (int i = 1; i < numRows; i++)
                    o.DrawLine(0, i * channelResY, videoResX, i * channelResY, Theme.BlackColor, channelLineWidth);
                for (int i = 1; i < numCols; i++)
                    o.DrawLine(i * channelResX, 0, i * channelResX, videoResY, Theme.BlackColor, channelLineWidth);

                c.PopClipRegion();
            });
        }
    }

    static class OscilloscopeColorType
    {
        public const int None = 0;
        public const int Instruments = 1;
        public const int Channel = 2;
        public const int Count = 3;

        public static LocalizedString[] LocalizedNames = new LocalizedString[Count];

        static OscilloscopeColorType()
        {
            Localization.LocalizeStatic(typeof(OscilloscopeColorType));
        }

        public static int GetIndexForName(string str)
        {
            return Array.FindIndex(LocalizedNames, n => n.Value == str);
        }
    }
}
