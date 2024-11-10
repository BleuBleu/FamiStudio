using System;
using System.Diagnostics;
using System.Globalization;

namespace FamiStudio
{
    public class Timecode : Control
    {
        private int  lastWidth;
        private Font font;

        #region Localization

        private LocalizedString FormatMinSecMs;
        private LocalizedString FormatPatternFrame;

        #endregion

        public Timecode()
        {
            supportsDoubleClick = false;
            Localization.Localize(this);
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

        private bool ProjectUsesFamitrackerTempo()
        {
            return App.Project != null && App.Project.UsesFamiTrackerTempo; ;
        }

        private void ToggleTimecodeFormat()
        {
            if (!ProjectUsesFamitrackerTempo())
            {
                Settings.TimeFormat = Settings.TimeFormat == 0 ? 1 : 0;
                Platform.VibrateTick();
                MarkDirty();
            }
        }

        protected override void OnTouchClick(PointerEventArgs e)
        {
            ToggleTimecodeFormat();
        }

        protected override void OnPointerDown(PointerEventArgs e)
        {
            if (e.Left && Platform.IsDesktop)
            {
                ToggleTimecodeFormat();
            }
        }

        protected override void OnPointerUp(PointerEventArgs e)
        {
            if (e.Right && !ProjectUsesFamitrackerTempo())
            {
                App.ShowContextMenuAsync(new[]
                {
                    new ContextMenuOption(FormatMinSecMs,     null, () => { Settings.TimeFormat = 1; MarkDirty(); }, () => Settings.TimeFormat == 1 ? ContextMenuCheckState.Radio : ContextMenuCheckState.None ),
                    new ContextMenuOption(FormatPatternFrame, null, () => { Settings.TimeFormat = 0; MarkDirty(); }, () => Settings.TimeFormat == 0 ? ContextMenuCheckState.Radio : ContextMenuCheckState.None ),
                });
            }
        }

        protected override void OnRender(Graphics g)
        {
            var c = g.GetCommandList();

            var frame = App.CurrentFrame;

            var zeroSizeX  = c.Graphics.MeasureString("0", font);
            var colonSizeX = c.Graphics.MeasureString(":", font);

            var textColor = App.IsRecording ? Theme.DarkRedColor : Theme.LightGreyColor2;

            var sx = width;
            var sy = height;

            c.FillAndDrawRectangle(0, 0, sx, sy, Theme.BlackColor, Theme.LightGreyColor2);

            if (Settings.TimeFormat == 0 || ProjectUsesFamitrackerTempo()) // MM:SS:mmm cant be used with FamiTracker tempo.
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
