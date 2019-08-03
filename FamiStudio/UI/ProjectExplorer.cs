using System;
using System.Diagnostics;
using System.Windows.Forms;
using FamiStudio.Properties;
using SharpDX.Direct2D1;

namespace FamiStudio
{
    public class ProjectExplorer : Direct2DUserControl
    {
        const int ScrollBarDefaultSizeX = 8;
        const int ButtonSizeY           = 24;

        int VirtualSizeY => (App?.Project == null ? Height : App.Project.Songs.Count + App.Project.Instruments.Count + 2) * ButtonSizeY;
        int ScrollBarSizeX => NeedsScrollBar ? ScrollBarDefaultSizeX : 0;
        bool NeedsScrollBar => VirtualSizeY > Height;

        int scrollY = 0;
        int mouseLastY = 0;
        int mouseDragY = -1;
        int instrumentDragIdx = -1;
        int envelopeDragIdx = -1;
        bool isDraggingInstrument = false;
        Song selectedSong = null;
        Instrument selectedInstrument = null; // null = DPCM

        Theme theme;

        Bitmap bmpVolume;
        Bitmap bmpPitch;
        Bitmap bmpArpeggio;
        Bitmap bmpDPCM;
        Bitmap bmpSong;
        Bitmap bmpInstrument;
        Bitmap bmpAdd;
        Bitmap[] bmpDuty = new Bitmap[4];

        public Song SelectedSong => selectedSong;
        public Instrument SelectedInstrument => selectedInstrument;

        public delegate void EmptyDelegate();
        public delegate void InstrumentEnvelopeDelegate(Instrument instrument, int envelope);
        public delegate void InstrumentDelegate(Instrument instrument);
        public delegate void SongDelegate(Song song);

        public event InstrumentEnvelopeDelegate InstrumentEdited;
        public event InstrumentDelegate InstrumentSelected;
        public event InstrumentDelegate InstrumentColorChanged;
        public event InstrumentDelegate InstrumentReplaced;
        public event SongDelegate SongModified;
        public event SongDelegate SongSelected;

        public ProjectExplorer()
        {
        }

        private FamiStudioForm App
        {
            get { return ParentForm as FamiStudioForm; }
        }

        public void Reset()
        {
            selectedSong = App.Project.Songs[0];
            selectedInstrument = App.Project.Instruments.Count > 0 ? App.Project.Instruments[0] : null;
            ConditionalInvalidate();
        }

        protected override void OnDirect2DInitialized(Direct2DGraphics g)
        {
            theme = Theme.CreateResourcesForGraphics(g);

            bmpVolume = g.ConvertBitmap(Resources.Volume);
            bmpPitch = g.ConvertBitmap(Resources.Pitch);
            bmpArpeggio = g.ConvertBitmap(Resources.Arpeggio);
            bmpDuty[0] = g.ConvertBitmap(Resources.Duty0);
            bmpDuty[1] = g.ConvertBitmap(Resources.Duty1);
            bmpDuty[2] = g.ConvertBitmap(Resources.Duty2);
            bmpDuty[3] = g.ConvertBitmap(Resources.Duty3);
            bmpDPCM = g.ConvertBitmap(Resources.DPCM);
            bmpSong = g.ConvertBitmap(Resources.Music);
            bmpAdd = g.ConvertBitmap(Resources.Add);
            bmpInstrument = g.ConvertBitmap(Resources.Pattern);
        }

        private void ConditionalInvalidate()
        {
            //if (!(ParentForm as FamitoneStudio).IsPlaying)
                Invalidate();
        }

        System.Drawing.Rectangle GetButtonRect(int buttonIdx, int shiftY = 0)
        {
            return new System.Drawing.Rectangle(Width - ScrollBarSizeX - 20 * buttonIdx, 4 + shiftY, 16, 16);
        }

        protected override void OnRender(Direct2DGraphics g)
        {
            g.Clear(Theme.DarkGreyFillColor1);
            g.DrawLineHalfPixel(0, 0, 0, Height, theme.BlackBrush);

            int actualWidth = Width - ScrollBarSizeX;

            int y = -scrollY;
            g.PushTranslation(0, y);
            g.FillAndDrawRectangleHalfPixel(0, 0, actualWidth, ButtonSizeY, g.GetVerticalGradientBrush(Theme.LightGreyFillColor2, ButtonSizeY, 0.8f), theme.BlackBrush);
            g.DrawText("Songs", Theme.FontMediumBoldCenter, 0, 5, theme.BlackBrush, actualWidth);
            g.DrawBitmap(bmpAdd, actualWidth - 20, 4);
            g.PopTransform();
            y += ButtonSizeY;

            for (int i = 0; i < App.Project.Songs.Count; i++, y += ButtonSizeY)
            {
                var song = App.Project.Songs[i];

                g.PushTranslation(0, y);
                g.FillAndDrawRectangleHalfPixel(0, 0, actualWidth, ButtonSizeY, g.GetVerticalGradientBrush(Direct2DGraphics.ToRawColor4(song.Color), ButtonSizeY, 0.8f), theme.BlackBrush);
                g.DrawText(song.Name, song == selectedSong ? Theme.FontMediumBold : Theme.FontMedium, 24, 5, theme.BlackBrush);
                g.DrawBitmap(bmpSong, 4, 4);

                //var rcVolume = GetVolumeRect();
                //g.DrawBitmap(bmpVolume, rcVolume.X, rcVolume.Y, instrument.Envelopes[Envelope.Volume].Length > 0 ? 1.0f : 0.2f);

                g.PopTransform();
            }

            g.PushTranslation(0, y);
            g.FillAndDrawRectangleHalfPixel(0, 0, actualWidth, ButtonSizeY, g.GetVerticalGradientBrush(Theme.LightGreyFillColor2, ButtonSizeY, 0.8f), theme.BlackBrush);
            g.DrawText("Instruments", Theme.FontMediumBoldCenter, 0, 5, theme.BlackBrush, actualWidth);
            g.DrawBitmap(bmpAdd, actualWidth - 20, 4);
            g.PopTransform();
            y += ButtonSizeY;

            g.PushTranslation(0, y);
            g.FillAndDrawRectangleHalfPixel(0, 0, actualWidth, ButtonSizeY, g.GetVerticalGradientBrush(Theme.LightGreyFillColor1, ButtonSizeY, 0.8f), theme.BlackBrush);
            g.DrawText("DPCM Samples", SelectedInstrument == null ? Theme.FontMediumBold : Theme.FontMedium, 24, 5, theme.BlackBrush);
            g.DrawBitmap(bmpInstrument, 4, 4);
            g.DrawBitmap(bmpDPCM, actualWidth - 20, 4);
            g.PopTransform();
            y += ButtonSizeY;

            for (int i = 0; i < App.Project.Instruments.Count; i++, y += ButtonSizeY)
            {
                var instrument = App.Project.Instruments[i];

                g.PushTranslation(0, y);
                g.FillAndDrawRectangleHalfPixel(0, 0, actualWidth, ButtonSizeY, g.GetVerticalGradientBrush(instrument.Color, ButtonSizeY, 0.8f), theme.BlackBrush);
                g.DrawText(instrument.Name, instrument == SelectedInstrument ? Theme.FontMediumBold : Theme.FontMedium, 24, 5, theme.BlackBrush);

                var rcVolume   = GetButtonRect(4);
                var rcPitch    = GetButtonRect(3);
                var rcArpeggio = GetButtonRect(2);
                var rcDuty     = GetButtonRect(1);

                g.DrawBitmap(bmpInstrument, 4, 4);
                g.DrawBitmap(bmpVolume, rcVolume.X, rcVolume.Y, instrument.Envelopes[Envelope.Volume].Length > 0 ? 1.0f : 0.2f);
                g.DrawBitmap(bmpPitch, rcPitch.X, rcPitch.Y, instrument.Envelopes[Envelope.Pitch].Length > 0 ? 1.0f : 0.2f);
                g.DrawBitmap(bmpArpeggio, rcArpeggio.X, rcArpeggio.Y, instrument.Envelopes[Envelope.Arpeggio].Length > 0 ? 1.0f : 0.2f);
                g.DrawBitmap(bmpDuty[instrument.DutyCycle], rcDuty.X, rcDuty.Y, 1.0f);

                g.PopTransform();
            }

            if (NeedsScrollBar)
            {
                int virtualSizeY   = VirtualSizeY;
                int scrollBarSizeY = (int)Math.Round(Height * (Height  / (float)virtualSizeY));
                int scrollBarPosY  = (int)Math.Round(Height * (scrollY / (float)virtualSizeY));

                g.FillAndDrawRectangleHalfPixel(actualWidth, 0, Width - 1, Height, theme.DarkGreyFillBrush1, theme.BlackBrush);
                g.FillAndDrawRectangleHalfPixel(actualWidth, scrollBarPosY, Width - 1, scrollBarPosY + scrollBarSizeY, theme.LightGreyFillBrush1, theme.BlackBrush);
            }
        }

        private void ClampScroll()
        {
            int minScrollY = 0;
            int maxScrollY = Math.Max(VirtualSizeY - Height, 0);

            if (scrollY < minScrollY) scrollY = minScrollY;
            if (scrollY > maxScrollY) scrollY = maxScrollY;
        }

        private void DoScroll(int deltaY)
        {
            scrollY -= deltaY;
            ClampScroll();
            ConditionalInvalidate();
        }

        protected void UpdateCursor()
        {
            if (isDraggingInstrument)
            {
                Cursor = envelopeDragIdx == -1 ? Theme.DragCursor : Theme.CopyCursor;
            }
            else
            {
                Cursor = Cursors.Default;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            bool left   = e.Button.HasFlag(MouseButtons.Left);
            bool middle = e.Button.HasFlag(MouseButtons.Middle) || (e.Button.HasFlag(MouseButtons.Left) && ModifierKeys.HasFlag(Keys.Alt));

            if (left && mouseDragY > 0 && !isDraggingInstrument && Math.Abs(e.Y - mouseDragY) > 5)
            {
                isDraggingInstrument = true;
            }
            if (middle)
            {
                int deltaY = e.Y - mouseLastY;
                DoScroll(deltaY);
                mouseLastY = e.Y;
            }

            UpdateCursor();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            if (isDraggingInstrument)
            {
                var instrumentSrcIdx = instrumentDragIdx;
                var instrumentDstIdx = ((e.Y + scrollY) / ButtonSizeY) - (App.Project.Songs.Count + 2) - 1;

                if (instrumentSrcIdx != instrumentDstIdx && instrumentDstIdx >= 0 && instrumentDstIdx < App.Project.Instruments.Count)
                {
                    var instrumentSrc = App.Project.Instruments[instrumentSrcIdx];
                    var instrumentDst = App.Project.Instruments[instrumentDstIdx];

                    if (envelopeDragIdx == -1)
                    {
                        if (MessageBox.Show($"Are you sure you want to replace all notes of instrument '{instrumentDst.Name}' with '{instrumentSrc.Name}'?", "Replace intrument", MessageBoxButtons.YesNo) == DialogResult.Yes)
                        {
                            App.UndoRedoManager.BeginTransaction(TransactionScope.Project);
                            App.Project.ReplaceInstrument(instrumentDst, instrumentSrc);
                            App.UndoRedoManager.EndTransaction();

                            InstrumentReplaced?.Invoke(instrumentDst);
                        }
                    }
                    else
                    {
                        if (MessageBox.Show($"Are you sure you want to copy the {Envelope.EnvelopeStrings[envelopeDragIdx]} envelope of instrument '{instrumentSrc.Name}' to '{instrumentDst.Name}'?", "Copy Envelope", MessageBoxButtons.YesNo) == DialogResult.Yes)
                        {
                            App.UndoRedoManager.BeginTransaction(TransactionScope.Instrument, instrumentDst.Id);
                            instrumentDst.Envelopes[envelopeDragIdx] = instrumentSrc.Envelopes[envelopeDragIdx].Clone();
                            App.Project.ReplaceInstrument(instrumentDst, instrumentSrc);
                            App.UndoRedoManager.EndTransaction();

                            InstrumentEdited?.Invoke(instrumentDst, envelopeDragIdx);
                            Invalidate();
                        }
                    }
                }
            }

            CancelDrag();
        }

        protected void CancelDrag()
        {
            isDraggingInstrument = false;
            mouseDragY = -1;
            instrumentDragIdx = -1;
            envelopeDragIdx = -1;
            UpdateCursor();
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            DoScroll(e.Delta > 0 ? ButtonSizeY * 3 : -ButtonSizeY * 3);
        }

        protected override void OnResize(EventArgs e)
        {
            ClampScroll();
            base.OnResize(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            bool left   = e.Button.HasFlag(MouseButtons.Left);
            bool middle = e.Button.HasFlag(MouseButtons.Middle) || (e.Button.HasFlag(MouseButtons.Left) && ModifierKeys.HasFlag(Keys.Alt));
            bool right  = e.Button.HasFlag(MouseButtons.Right);

            var buttonIndex = (e.Y + scrollY) / ButtonSizeY;
            var songIndex = buttonIndex - 1;
            var instrumentIndex = buttonIndex - (App.Project.Songs.Count + 2);
            var shiftY = (buttonIndex * ButtonSizeY - scrollY);

            if (left)
            {
                if (songIndex == -1)
                {
                    if (GetButtonRect(1, shiftY).Contains(e.X, e.Y))
                    {
                        App.UndoRedoManager.BeginTransaction(TransactionScope.Project);
                        App.Project.CreateSong();
                        App.UndoRedoManager.EndTransaction();
                        ConditionalInvalidate();
                    }
                }
                else if (instrumentIndex == -1)
                {
                    if (GetButtonRect(1, shiftY).Contains(e.X, e.Y))
                    {
                        App.UndoRedoManager.BeginTransaction(TransactionScope.Project);
                        App.Project.CreateInstrument();
                        App.UndoRedoManager.EndTransaction();
                        ConditionalInvalidate();
                    }
                }
                else if (songIndex >= 0 && songIndex < App.Project.Songs.Count)
                {
                    selectedSong = App.Project.Songs[songIndex];
                    SongSelected?.Invoke(selectedSong);
                    ConditionalInvalidate();
                    return;
                }
                else if (instrumentIndex >= 0 && instrumentIndex <= App.Project.Instruments.Count)
                {
                    selectedInstrument = instrumentIndex == 0 ? null : App.Project.Instruments[instrumentIndex - 1];

                    if (selectedInstrument != null)
                    {
                        instrumentDragIdx = instrumentIndex - 1;
                        mouseDragY = e.Y;
                    }

                    if (selectedInstrument != null && GetButtonRect(4, shiftY).Contains(e.X, e.Y))
                    {
                        InstrumentEdited?.Invoke(selectedInstrument, Envelope.Volume);
                        envelopeDragIdx = Envelope.Volume;
                    }
                    else if (selectedInstrument != null && GetButtonRect(3, shiftY).Contains(e.X, e.Y))
                    {
                        InstrumentEdited?.Invoke(selectedInstrument, Envelope.Pitch);
                        envelopeDragIdx = Envelope.Pitch;
                    }
                    else if (selectedInstrument != null && GetButtonRect(2, shiftY).Contains(e.X, e.Y))
                    {
                        InstrumentEdited?.Invoke(selectedInstrument, Envelope.Arpeggio);
                        envelopeDragIdx = Envelope.Arpeggio;
                    }
                    else if (GetButtonRect(1, shiftY).Contains(e.X, e.Y))
                    {
                        if (selectedInstrument == null)
                        {
                            InstrumentEdited?.Invoke(selectedInstrument, Envelope.Max);
                        }
                        else
                        {
                            selectedInstrument.DutyCycle = (selectedInstrument.DutyCycle + 1) % 4;
                            ConditionalInvalidate();
                        }
                    }

                    InstrumentSelected?.Invoke(selectedInstrument);
                    ConditionalInvalidate();
                }
            }
            else if (right)
            {
                instrumentIndex--;

                if (songIndex >= 0 && songIndex < App.Project.Songs.Count && App.Project.Songs.Count > 1)
                {
                    var song = App.Project.Songs[songIndex];
                    if (MessageBox.Show($"Are you sure you want to delete '{song.Name}' ?", "Delete song", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        bool selectNewSong = song == selectedSong;
                        App.Stop();
                        App.UndoRedoManager.BeginTransaction(TransactionScope.Project);
                        App.Project.DeleteSong(song);
                        App.UndoRedoManager.EndTransaction();
                        if (selectNewSong)
                            selectedSong = App.Project.Songs[0];
                        SongSelected?.Invoke(selectedSong);
                        ConditionalInvalidate();
                        return;
                    }
                }
                else if (instrumentIndex >= 0 && instrumentIndex < App.Project.Instruments.Count)
                {
                    var instrument = App.Project.Instruments[instrumentIndex];
                    if (MessageBox.Show($"Are you sure you want to delete '{instrument.Name}' ? All notes using this instrument will be deleted.", "Delete intrument", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        bool selectNewInstrument = instrument == selectedInstrument;
                        App.StopInstrumentNoteAndWait();
                        App.UndoRedoManager.BeginTransaction(TransactionScope.Project);
                        App.Project.DeleteInstrument(instrument);
                        App.UndoRedoManager.EndTransaction();
                        if (selectNewInstrument)
                            selectedInstrument = App.Project.Instruments.Count > 0 ? App.Project.Instruments[0] : null;
                        SongSelected?.Invoke(selectedSong);
                        ConditionalInvalidate();
                        return;
                    }
                }
            }

            if (middle)
            {
                mouseLastY = e.Y;
            }
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);

            var buttonIndex = (e.Y + scrollY) / ButtonSizeY;
            var songIndex = buttonIndex - 1;
            var instrumentIndex = buttonIndex - (App.Project.Songs.Count + 2);

            Debug.WriteLine($"Instrument {instrumentIndex}");

            if (songIndex >= 0 && songIndex < App.Project.Songs.Count)
            {
                var song = App.Project.Songs[songIndex];
                var dlg = new SongEditDialog(song)
                {
                    StartPosition = FormStartPosition.Manual,
                    Location = PointToScreen(new System.Drawing.Point(e.X - 210, e.Y))
                };

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    App.UndoRedoManager.BeginTransaction(TransactionScope.Project);

                    App.Stop();
                    App.Seek(0);

                    if (App.Project.RenameSong(song, dlg.NewName))
                    {
                        song.Color = dlg.NewColor;
                        song.Tempo = dlg.NewTempo;
                        song.Speed = dlg.NewSpeed;
                        song.Length = dlg.NewSongLength;
                        song.PatternLength = dlg.NewPatternLength;
                        song.BarLength = dlg.NewBarLength;
                        ConditionalInvalidate();
                        SongModified?.Invoke(song);
                    }

                    App.UndoRedoManager.EndTransaction();
                }
            }
            else if (instrumentIndex > 0 && instrumentIndex <= App.Project.Instruments.Count)
            {
                var instrument = App.Project.Instruments[instrumentIndex - 1];
                var shiftY = (buttonIndex * ButtonSizeY - scrollY);
                var volRect = GetButtonRect(4, shiftY);

                if (e.X < volRect.Left)
                {
                    var dlg = new RenameColorDialog(instrument.Name, instrument.Color)
                    {
                        StartPosition = FormStartPosition.Manual,
                        Location = PointToScreen(new System.Drawing.Point(e.X - 160, e.Y))
                    };

                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        App.UndoRedoManager.BeginTransaction(TransactionScope.Project);

                        if (App.Project.RenameInstrument(instrument, dlg.NewName))
                        {
                            instrument.Color = dlg.NewColor;
                            InstrumentColorChanged?.Invoke(instrument);
                            ConditionalInvalidate();
                        }

                        App.UndoRedoManager.EndTransaction();
                    }
                }
            }
        }
        
        public void SerializeState(ProjectBuffer buffer)
        {
            buffer.Serialize(ref selectedSong);
            buffer.Serialize(ref selectedInstrument);
            buffer.Serialize(ref scrollY);

            if (buffer.IsReading)
            {
                CancelDrag();
                ClampScroll();
                ConditionalInvalidate();
            }
        }
    }
}
