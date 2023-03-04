using System;
using System.IO;

namespace FamiStudio
{
    class VideoFileOscilloscope : VideoFileBase
    {
        private void BuildChannelColors(Song song, VideoChannelState[] channels, VideoFrameMetadata[] meta, int colorMode)
        {
            Color[,] colors = new Color[meta.Length, meta[0].channelData.Length];

            // Get the note colors.
            for (int i = 0; i < meta.Length; i++)
            {
                var m = meta[i];

                for (int j = 0; j < channels.Length; j++)
                {
                    var note = m.channelData[channels[j].songChannelIndex].note;

                    if (note != null && note.IsMusical)
                    {
                        var color = Theme.LightGreyColor1;

                        if (colorMode == OscilloscopeColorType.Channel)
                        {
                            var channel = song.Channels[channels[j].songChannelIndex];
                            for (int p = 0; p < channel.PatternInstances.Length; p++)
                            {
                                if (channel.PatternInstances[p] != null)
                                {
                                    color = channel.PatternInstances[p].Color;
                                    break;
                                }
                            }
                        }
                        else if (colorMode != OscilloscopeColorType.None)
                        {
                            if (note.Instrument != null && colorMode == OscilloscopeColorType.Instruments)
                            {
                                color = note.Instrument.Color;
                            }
                        }

                        colors[i, j] = color;
                    }
                }
            }

            // Extend any color until we hit another one.
            for (int i = 0; i < colors.GetLength(0) - 1; i++)
            {
                for (int j = 0; j < colors.GetLength(1); j++)
                {
                    if (colors[i, j].A != 0)
                    {
                        if (colors[i + 1, j].A == 0)
                            colors[i + 1, j] = colors[i, j];
                    }
                    else
                    {
                        colors[i, j] = Theme.LightGreyColor1;
                    }
                }
            }

            const int ColorBlendTime = 5;

            // Blend the colors.
            for (int i = 0; i < meta.Length; i++)
            {
                var m = meta[i];

                for (int j = 0; j < m.channelData.Length; j++)
                {
                    int avgR = 0;
                    int avgG = 0;
                    int avgB = 0;
                    int count = 0;

                    for (int k = i; k < i + ColorBlendTime && k < meta.Length; k++)
                    {
                        avgR += colors[k, j].R;
                        avgG += colors[k, j].G;
                        avgB += colors[k, j].B;
                        count++;
                    }

                    m.channelData[j].color = Color.FromArgb(avgR / count, avgG / count, avgB / count);
                }
            }
        }

        public bool Save(Project originalProject, int songId, int loopCount, int colorMode, int numColumns, int lineThickness, int window, string filename, int resX, int resY, bool halfFrameRate, long channelMask, int audioDelay, int audioBitRate, int videoBitRate, bool stereo, float[] pan, bool[] emuTriggers)
        {
            if (!InitializeEncoder(originalProject, songId, loopCount, filename, resX, resY, halfFrameRate, window, channelMask, audioDelay, audioBitRate, videoBitRate, stereo, pan, emuTriggers))
                return false;

            numColumns = Math.Min(numColumns, channelStates.Length);
            var numRows = (int)Math.Ceiling(channelStates.Length / (float)numColumns);

            var channelResXFloat = videoResX / (float)numColumns;
            var channelResYFloat = videoResY / (float)numRows;

            var channelResX = (int)channelResXFloat;
            var channelResY = (int)channelResYFloat;

            // Tweak some cosmetic stuff that depends on resolution.
            var smallChannelText = channelResY < 128;
            var font = lineThickness > 1 ?
                (smallChannelText ? fonts.FontMediumBold : fonts.FontVeryLargeBold) : 
                (smallChannelText ? fonts.FontMedium     : fonts.FontVeryLarge);
            var textOffsetY = smallChannelText ? 1 : 4;
            var channelLineWidth = resY >= 720 ? 5 : 3;

            LoadChannelIcons(!smallChannelText);
            BuildChannelColors(song, channelStates, metadata, colorMode);

            return LaunchEncoderLoop((f) =>
            {
                var frame = metadata[f];
                var c = videoGraphics.DefaultCommandList;

                // Draw gradients.
                for (int i = 0; i < numRows; i++)
                {
                    c.PushTranslation(0, i * channelResY);
                    c.FillRectangleGradient(0, 0, videoResX, channelResY, Color.Black, Color.Transparent, true, channelResY / 2);
                    c.PopTransform();
                }

                // Channel names + oscilloscope
                for (int i = 0; i < channelStates.Length; i++)
                {
                    var s = channelStates[i];

                    var channelX = i % numColumns;
                    var channelY = i / numColumns;

                    var channelPosX0 = (channelX + 0) * channelResX;
                    var channelPosX1 = (channelX + 1) * channelResX;
                    var channelPosY0 = (channelY + 0) * channelResY;
                    var channelPosY1 = (channelY + 1) * channelResY;

                    // Oscilloscope
                    var oscilloscope = UpdateOscilloscope(s, f);

                    c.PushTransform(channelPosX0, channelPosY0 + channelResY / 2, channelPosX1 - channelPosX0, (channelPosY0 - channelPosY1) / 2);
                    c.DrawGeometry(oscilloscope, frame.channelData[i].color, lineThickness, true, false);
                    c.PopTransform();

                    // Icons + text
                    var channelIconPosX = channelPosX0 + s.icon.Size.Width / 2;
                    var channelIconPosY = channelPosY0 + s.icon.Size.Height / 2;

                    c.FillAndDrawRectangle(channelIconPosX, channelIconPosY, channelIconPosX + s.icon.Size.Width - 1, channelIconPosY + s.icon.Size.Height - 1, Theme.DarkGreyColor2, Theme.LightGreyColor1);
                    c.DrawBitmap(s.icon, channelIconPosX, channelIconPosY, 1, Theme.LightGreyColor1);
                    c.DrawText(s.channelText, font, channelIconPosX + s.icon.Size.Width + ChannelIconTextSpacing, channelIconPosY + textOffsetY, Theme.LightGreyColor1);

                    videoGraphics.ConditionalFlushCommandLists();
                }

                // Grid lines
                for (int i = 1; i < numRows; i++)
                    c.DrawLine(0, i * channelResY, videoResX, i * channelResY, Theme.BlackColor, channelLineWidth);
                for (int i = 1; i < numColumns; i++)
                    c.DrawLine(i * channelResX, 0, i * channelResX, videoResY, Theme.BlackColor, channelLineWidth);
            });
        }
    }

    static class OscilloscopeColorType
    {
        public const int None = 0;
        public const int Instruments = 1;
        public const int Channel = 2;

        public static readonly string[] Names =
        {
            "None",
            "Instruments",
            "Channel (First pattern color)"
        };

        public static int GetIndexForName(string str)
        {
            return Array.IndexOf(Names, str);
        }
    }
}
