using System;
using System.Diagnostics;
using System.Globalization;

namespace FamiStudio
{
    public class Timecode : Control
    {
        private int  lastWidth;
        private Font font;

        public Timecode()
        {
        }

        protected override void OnAddedToContainer()
        {
            UpdateFont();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateFont();
        }

        private void UpdateFont()
        {
            if (width != lastWidth)
            {
                font = Fonts.GetBestMatchingFontByWidth("00:00:000", width, false);
                lastWidth = width; 
            }
        }

        protected override void OnRender(Graphics g)
        {
            var c = g.GetCommandList();

            var frame = App.CurrentFrame;
            var famitrackerTempo = App.Project != null && App.Project.UsesFamiTrackerTempo;

            var zeroSizeX  = c.Graphics.MeasureString("0", font);
            var colonSizeX = c.Graphics.MeasureString(":", font);

            var textColor = App.IsRecording ? Theme.DarkRedColor : Theme.LightGreyColor2;

            var sx = width;
            var sy = height;

            c.FillAndDrawRectangle(0, 0, sx, sy, Theme.BlackColor, Theme.LightGreyColor2);

            if (Settings.TimeFormat == 0 || famitrackerTempo) // MM:SS:mmm cant be used with FamiTracker tempo.
            {
                var location = NoteLocation.FromAbsoluteNoteIndex(App.SelectedSong, frame);

                var numPatternDigits = Utils.NumDecimalDigits(App.SelectedSong.Length - 1);
                var numNoteDigits = Utils.NumDecimalDigits(App.SelectedSong.GetPatternLength(location.PatternIndex) - 1);

                var patternString = (location.PatternIndex + 1).ToString("D" + numPatternDigits);
                var noteString = location.NoteIndex.ToString("D" + numNoteDigits);

                var charPosX = sx / 2 - ((numPatternDigits + numNoteDigits) * zeroSizeX + colonSizeX) / 2;

                for (int i = 0; i < numPatternDigits; i++, charPosX += zeroSizeX)
                    c.DrawText(patternString[i].ToString(), font, charPosX, 0, textColor, TextFlags.MiddleCenter, zeroSizeX, sy);
                c.DrawText(":", font, charPosX, 0, textColor, TextFlags.MiddleCenter, colonSizeX, sy);
                charPosX += colonSizeX;
                for (int i = 0; i < numNoteDigits; i++, charPosX += zeroSizeX)
                    c.DrawText(noteString[i].ToString(), font, charPosX, 0, textColor, TextFlags.MiddleCenter, zeroSizeX, sy);
            }
            else
            {
                TimeSpan time = App.CurrentTime;

                var minutesString = time.Minutes.ToString("D2");
                var secondsString = time.Seconds.ToString("D2");
                var millisecondsString = time.Milliseconds.ToString("D3");

                // 00:00:000
                var charPosX = sx / 2 - (7 * zeroSizeX + 2 * colonSizeX) / 2;

                for (int i = 0; i < 2; i++, charPosX += zeroSizeX)
                    c.DrawText(minutesString[i].ToString(), font, charPosX, 0, textColor, TextFlags.MiddleCenter, zeroSizeX, sy);
                c.DrawText(":", font, charPosX, 0, textColor, TextFlags.MiddleCenter, colonSizeX, sy);
                charPosX += colonSizeX;
                for (int i = 0; i < 2; i++, charPosX += zeroSizeX)
                    c.DrawText(secondsString[i].ToString(), font, charPosX, 0, textColor, TextFlags.MiddleCenter, zeroSizeX, sy);
                c.DrawText(":", font, charPosX, 0, textColor, TextFlags.MiddleCenter, colonSizeX, sy);
                charPosX += colonSizeX;
                for (int i = 0; i < 3; i++, charPosX += zeroSizeX)
                    c.DrawText(millisecondsString[i].ToString(), font, charPosX, 0, textColor, TextFlags.MiddleCenter, zeroSizeX, sy);
            }
        }
    }
}
