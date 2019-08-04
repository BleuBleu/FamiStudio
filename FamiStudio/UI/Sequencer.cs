using System;
using System.Collections.Generic;
using System.Windows.Forms;
using FamiStudio.Properties;

using SharpDX;
using SharpDX.DXGI;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;
using AlphaMode = SharpDX.Direct2D1.AlphaMode;
using Bitmap = SharpDX.Direct2D1.Bitmap;
using PixelFormat = SharpDX.Direct2D1.PixelFormat;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace FamiStudio
{
    public class Sequencer : Direct2DUserControl
    {
        const int TrackNameSizeX     = 81;
        const int HeaderSizeY        = 17;
        const int TrackSizeY         = 56;
        const int PatternHeaderSizeY = 16;
        const int NoteSizeY          = 4;

        const int MinZoomLevel = -2;
        const int MaxZoomLevel = 4;
        const int ScrollMargin = 128;

        int PatternSizeX => ScaleForZoom(Song.PatternLength);

        int ScaleForZoom(int value)
        {
            return zoomLevel < 0 ? value / (1 << (-zoomLevel)) : value * (1 << zoomLevel);
        }

        int UnscaleForZoom(int value)
        {
            return zoomLevel < 0 ? value * (1 << (-zoomLevel)) : value / (1 << zoomLevel);
        }

        int scrollX = 0;
        int zoomLevel = 1;

        int mouseLastX = 0;
        int mouseLastY = 0;

        int selectedChannel = 0;

        bool isDraggingSelection = false;
        int selectionDragStartX = -1;
        int selectionDragAnchorX = -64;
        int minSelectedChannelIdx = -1;
        int maxSelectedChannelIdx = -1;
        int minSelectedPatternIdx = -1;
        int maxSelectedPatternIdx = -1;

        Dictionary<int, Bitmap> patternBitmapCache = new Dictionary<int, Bitmap>();

        Theme theme;
        Brush playPositionBrush;
        Brush whiteKeyBrush;
        Brush patternHeaderBrush;
        Brush selectedPatternBrush;

        Bitmap[] bmpTracks = new Bitmap[Channel.Count];
        Bitmap bmpEdit;
        Bitmap bmpGhostNote;

        public delegate void PatternDoubleClick(int trackIdx, int barIdx);
        public event PatternDoubleClick PatternClicked;

        public Sequencer()
        {
        }

        private Song Song
        {
            get { return (ParentForm as FamiStudioForm)?.Song; }
        }

        public int SelectedChannel => selectedChannel;
        public FamiStudioForm App => ParentForm as FamiStudioForm;

        private void ConditionalInvalidate()
        {
            if (!App.RealTimeUpdate)
                Invalidate();
        }

        public void Reset()
        {
            scrollX = 0;
            zoomLevel = 1;
            selectedChannel = 0;
            ClearSelection();
        }

        private void ClearSelection()
        {
            minSelectedChannelIdx = -1;
            maxSelectedChannelIdx = -1;
            minSelectedPatternIdx = -1;
            maxSelectedPatternIdx = -1;
        }
        
        private bool IsPatternSelected(int channelIdx, int patternIdx)
        {
            return channelIdx >= minSelectedChannelIdx && channelIdx <= maxSelectedChannelIdx &&
                   patternIdx >= minSelectedPatternIdx && patternIdx <= maxSelectedPatternIdx;
        }

        protected override void OnDirect2DInitialized(Direct2DGraphics g)
        {
            theme = Theme.CreateResourcesForGraphics(g);
            
            bmpTracks[Channel.Square1]  = g.ConvertBitmap(Resources.Square);
            bmpTracks[Channel.Square2]  = g.ConvertBitmap(Resources.Square);
            bmpTracks[Channel.Triangle] = g.ConvertBitmap(Resources.Triangle);
            bmpTracks[Channel.Noise]    = g.ConvertBitmap(Resources.Noise);
            bmpTracks[Channel.DPCM]     = g.ConvertBitmap(Resources.DPCM);

            bmpEdit = g.ConvertBitmap(Resources.EditSmall);
            bmpGhostNote = g.ConvertBitmap(Resources.GhostSmall);

            playPositionBrush = g.CreateSolidBrush(new RawColor4(Theme.LightGreyFillColor1.R, Theme.LightGreyFillColor1.G, Theme.LightGreyFillColor1.B, 0.75f));

            whiteKeyBrush = g.CreateHorizontalGradientBrush(0, TrackNameSizeX, Theme.LightGreyFillColor1, Theme.LightGreyFillColor2);
            patternHeaderBrush = g.CreateVerticalGradientBrush(0, PatternHeaderSizeY, Theme.LightGreyFillColor1, Theme.LightGreyFillColor2);
            selectedPatternBrush = g.CreateSolidBrush(new RawColor4(Theme.LightGreyFillColor1.R, Theme.LightGreyFillColor1.G, Theme.LightGreyFillColor1.B, 0.5f));
        }

        protected override void OnRender(Direct2DGraphics g)
        {
            g.Clear(Theme.DarkGreyFillColor1);

            int patternSizeX = PatternSizeX;
            int minVisiblePattern = Math.Max((int)Math.Floor(scrollX / (float)patternSizeX), 0);
            int maxVisiblePattern = Math.Min((int)Math.Ceiling((scrollX + Width) / (float)patternSizeX), Song.Length);

            // Track name background
            g.FillRectangle(0, 0, TrackNameSizeX, Height, whiteKeyBrush);

            // Header
            g.DrawLineHalfPixel(0, 0, Width, 0, theme.BlackBrush);
            g.DrawLineHalfPixel(TrackNameSizeX - 1, 0, TrackNameSizeX - 1, HeaderSizeY, theme.DarkGreyLineBrush1);
            g.PushTranslation(TrackNameSizeX, 0);
            g.PushClipHalfPixel(0, 0, Width, Height);

            for (int i = minVisiblePattern, x = minVisiblePattern * patternSizeX - scrollX; i <= maxVisiblePattern; i++, x += patternSizeX)
                if (i != 0) g.DrawLineHalfPixel(x, 0, x, Height, theme.DarkGreyLineBrush1);

            for (int i = minVisiblePattern, x = minVisiblePattern * patternSizeX - scrollX; i < maxVisiblePattern; i++, x += patternSizeX)
            {
                g.PushTranslation(x, 0);
                g.DrawText(i.ToString(), Theme.FontMediumCenter, 0, 2, theme.LightGreyFillBrush1, patternSizeX);
                g.PopTransform();
            }

            g.PopClip();
            g.PopTransform();

            g.PushTranslation(0, HeaderSizeY);

            // Icons
            for (int i = 0, y = 0; i < Song.Channels.Length; i++, y += TrackSizeY)
                g.DrawBitmap(bmpTracks[(int)Song.Channels[i].Type], 4, y + 4, (App.ChannelMask & (1 << i)) != 0 ? 1.0f : 0.2f);

            // Track names
            for (int i = 0, y = 0; i < Song.Channels.Length; i++, y += TrackSizeY)
                g.DrawText(Song.Channels[i].Name, i == selectedChannel ? Theme.FontMediumBold : Theme.FontMedium, 24, y + 4, theme.BlackBrush);

            // Ghost note icons
            for (int i = 0, y = 0; i < Song.Channels.Length; i++, y += TrackSizeY)
                g.DrawBitmap(bmpGhostNote, TrackNameSizeX - 16, y + TrackSizeY - 15, (App.GhostChannelMask & (1 << i)) != 0 ? 1.0f : 0.2f);

            // Vertical line seperating the track labels.
            g.DrawLineHalfPixel(TrackNameSizeX - 1, 0, TrackNameSizeX - 1, Height, theme.DarkGreyLineBrush1);

            // Grey background rectangles ever other pattern + vertical lines 
            g.PushClipHalfPixel(TrackNameSizeX, 0, Width, Height);
            g.PushTranslation(TrackNameSizeX, 0);
            for (int i = minVisiblePattern, x = minVisiblePattern * patternSizeX - scrollX; i < maxVisiblePattern; i++, x += patternSizeX)
                if ((i & 1) == 0) g.FillRectangle(x, 0, x + patternSizeX, Height, theme.DarkGreyFillBrush2);
            for (int i = minVisiblePattern, x = minVisiblePattern * patternSizeX - scrollX; i <= maxVisiblePattern; i++, x += patternSizeX)
                if (i != 0) g.DrawLineHalfPixel(x, 0, x, Height, theme.DarkGreyLineBrush1);
            g.PopTransform();
            g.PopClip();

            // Horizontal lines
            for (int i = 0, y = 0; i < Song.Channels.Length; i++, y += TrackSizeY)
                g.DrawLineHalfPixel(0, y, Width, y, theme.DarkGreyLineBrush1);

            g.PushClipHalfPixel(TrackNameSizeX, 0, Width, Height);

            // Seek
            int xxx = ScaleForZoom(App.CurrentFrame) + TrackNameSizeX - scrollX;
            g.DrawLineHalfPixel(xxx, -HeaderSizeY, xxx, Height, playPositionBrush);

            // Patterns
            for (int t = 0, y = 0; t < Song.Channels.Length; t++, y += TrackSizeY)
            {
                for (int i = minVisiblePattern, x = minVisiblePattern * patternSizeX + TrackNameSizeX - scrollX; i < maxVisiblePattern; i++, x += patternSizeX)
                {
                    var pattern = Song.Channels[t].PatternInstances[i];

                    if (pattern != null)
                    {
                        var bmp = GetPatternBitmapFromCache(pattern);

                        g.PushTranslation(x, y);
                        g.FillRectangle(1, 1, patternSizeX, PatternHeaderSizeY, g.GetVerticalGradientBrush(pattern.Color, PatternHeaderSizeY, 0.9f));
                        g.DrawLineHalfPixel(0, PatternHeaderSizeY, patternSizeX, PatternHeaderSizeY, theme.DarkGreyLineBrush1);
                        if (IsPatternSelected(t, i))
                            g.FillRectangle(1, 1 + PatternHeaderSizeY, PatternSizeX, PatternHeaderSizeY + bmp.Size.Height + 1, selectedPatternBrush);

                        g.DrawBitmap(bmp, 1, 1 + PatternHeaderSizeY, PatternSizeX - 1, bmp.Size.Height);
                        g.PushClipHalfPixel(0, 0, PatternSizeX, Height);
                        g.DrawText(pattern.Name, Theme.FontSmall, 2, 3, theme.BlackBrush);
                        g.PopClip();
                        g.PopTransform();
                    }
                }
            }

            // Dragging selection
            if (isDraggingSelection)
            {
                var pt = this.PointToClient(Cursor.Position);

                //pt.X -= TrackNameSizeX;
                pt.Y -= HeaderSizeY;

                var drawX = pt.X - selectionDragAnchorX;
                var drawY = minSelectedChannelIdx * TrackSizeY;
                var sizeX = (maxSelectedPatternIdx - minSelectedPatternIdx + 1) * patternSizeX;
                var sizeY = (maxSelectedChannelIdx - minSelectedChannelIdx + 1) * TrackSizeY;

                g.FillRectangle(drawX, drawY, drawX + sizeX, drawY + sizeY, selectedPatternBrush);
            }

            g.PopClip();
            g.PopTransform();

            g.DrawLineHalfPixel(0, Height - 1, Width, Height - 1, theme.DarkGreyLineBrush1);
        }

        public void NotifyPatternChange(Pattern pattern)
        {
            patternBitmapCache.Remove(pattern.Id);
        }

        private unsafe Bitmap GetPatternBitmapFromCache(Pattern p)
        {
            int patternSizeX = Song.PatternLength - 1;
            int patternSizeY = TrackSizeY - PatternHeaderSizeY - 1;

            Bitmap bmp;

            if (patternBitmapCache.TryGetValue(p.Id, out bmp))
            {
                if (bmp.Size.Width == patternSizeX)
                {
                    return bmp;
                }
                else
                {
                    bmp.Dispose();
                    bmp = null;
                }
            }

            uint[] data = new uint[patternSizeX * patternSizeY];

            Note minNote;
            Note maxNote;

            if (p.GetMinMaxNote(out minNote, out maxNote))
            {
                if (maxNote.Value == minNote.Value)
                {
                    minNote.Value = (byte)(minNote.Value - 5);
                    maxNote.Value = (byte)(maxNote.Value + 5);
                }
                else
                {
                    minNote.Value = (byte)(minNote.Value - 2);
                    maxNote.Value = (byte)(maxNote.Value + 2);
                }

                Note lastValid = new Note { Value = Note.NoteInvalid };

                for (int i = 0; i < Song.PatternLength - 1; i++) // TODO: We always skip the last note.
                {
                    var n = p.Notes[i];

                    if (n.IsValid && !n.IsStop)
                        lastValid = p.Notes[i];

                    if (lastValid.IsValid)
                    {
                        float scaleY = (patternSizeY - NoteSizeY) / (float)patternSizeY;

                        int x = i;
                        int y = Math.Min((int)Math.Round((lastValid.Value - minNote.Value) / (float)(maxNote.Value - minNote.Value) * scaleY * patternSizeY), patternSizeY - NoteSizeY);

                        var instrument = lastValid.Instrument;
                        var color = instrument == null ? Direct2DGraphics.ToDrawingColor4(Theme.LightGreyFillColor1) : instrument.Color;

                        for (int j = 0; j < NoteSizeY; j++)
                        {
                            data[(patternSizeY - 1 - (y + j)) * patternSizeX + x] = (uint)color.ToArgb();
                        }
                    }

                    //if (n.HasEffect)
                    //{
                    //    for (int y = 0; y < patternSizeY; y++)
                    //    {
                    //        data[y * patternSizeX + i] = 0xff000000;
                    //    }
                    //}
                }
            }

            fixed (uint* ptr = &data[0])
            {
                DataStream stream = new DataStream(new IntPtr(ptr), data.Length * sizeof(uint), true, false);
                BitmapProperties bmpProps = new BitmapProperties(new PixelFormat(Format.B8G8R8A8_UNorm, AlphaMode.Premultiplied));
                bmp = new Bitmap(d2dGraphics.RenderTarget, new Size2(patternSizeX, patternSizeY), stream, patternSizeX * sizeof(uint), bmpProps);
                patternBitmapCache[p.Id] = bmp;
                stream.Dispose();
            }

            return bmp;
        }

        private void ClampScroll()
        {
            int minScrollX = 0;
            int maxScrollX = Math.Max(Song.Length * PatternSizeX - ScrollMargin, 0);

            if (scrollX < minScrollX) scrollX = minScrollX;
            if (scrollX > maxScrollX) scrollX = maxScrollX;
        }

        private void DoScroll(int deltaX, int deltaY)
        {
            scrollX -= deltaX;
            ClampScroll();
            ConditionalInvalidate();
        }

        private bool GetPatternForCoord(int x, int y, out int track, out int pattern)
        {
            int patternSizeX = PatternSizeX;

            pattern = (x - TrackNameSizeX + scrollX) / patternSizeX;
            track = (y - HeaderSizeY) / TrackSizeY;

            return (x > TrackNameSizeX && y > HeaderSizeY && pattern >= 0 && pattern < Song.Length && track >= 0 && track < Song.Channels.Length);
        }

        System.Drawing.Rectangle GetTrackIconRect(int idx)
        {
            return new System.Drawing.Rectangle(4, HeaderSizeY + idx * TrackSizeY + 4, 16, 16);
        }

        System.Drawing.Rectangle GetTrackGhostRect(int idx)
        {
            return new System.Drawing.Rectangle(TrackNameSizeX - 16, HeaderSizeY + (idx + 1) * TrackSizeY - 15, 12, 12);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            bool left   = e.Button.HasFlag(MouseButtons.Left);
            bool middle = e.Button.HasFlag(MouseButtons.Middle) || (e.Button.HasFlag(MouseButtons.Left) && ModifierKeys.HasFlag(Keys.Alt));
            bool right  = e.Button.HasFlag(MouseButtons.Right);

            CancelDragSelection();
            UpdateCursor();

            if (middle)
            {
                mouseLastX = e.X;
                mouseLastY = e.Y;
                return;
            }
            // Track muting, soloing.
            else if ((left || right) && e.X < TrackNameSizeX)
            {
                for (int i = 0; i < 5; i++)
                {
                    int bit = (1 << i);

                    if (GetTrackIconRect(i).Contains(e.X, e.Y))
                    {
                        if (left)
                        {
                            // Toggle muted
                            App.ChannelMask ^= bit; 
                        }
                        else
                        {
                            // Toggle Solo
                            if (App.ChannelMask == (1 << i))
                                App.ChannelMask = 0x1f;
                            else
                                App.ChannelMask = (1 << i); 
                        }
                        ConditionalInvalidate();
                        break;
                    }
                    if (GetTrackGhostRect(i).Contains(e.X, e.Y))
                    {
                        App.GhostChannelMask ^= bit;
                        ConditionalInvalidate();
                        break;
                    }
                }
            }

            if (left && e.X > TrackNameSizeX && e.Y < HeaderSizeY)
            {
                int frame = (int)Math.Round((e.X - TrackNameSizeX + scrollX) / (float)PatternSizeX * Song.PatternLength);
                App.Seek(frame);
                return;
            }

            if (left || right)
            {
                if (e.Y > HeaderSizeY)
                {
                    selectedChannel = (e.Y - HeaderSizeY) / TrackSizeY;
                    ConditionalInvalidate();
                }
            }

            bool inPatternZone = GetPatternForCoord(e.X, e.Y, out int channelIdx, out int patternIdx);

            if (inPatternZone)
            {
                var channel = Song.Channels[channelIdx];
                var pattern = channel.PatternInstances[patternIdx];

                if (left)
                {
                    bool shift = ModifierKeys.HasFlag(Keys.Shift);

                    if (pattern == null && !shift)
                    {
                        App.UndoRedoManager.BeginTransaction(TransactionScope.Song, Song.Id);
                        channel.PatternInstances[patternIdx] = channel.CreatePattern();
                        App.UndoRedoManager.EndTransaction();
                        ConditionalInvalidate();
                    }
                    else
                    {
                        if (shift && minSelectedChannelIdx >= 0 && minSelectedPatternIdx >= 0)
                        {
                            if (channelIdx < minSelectedChannelIdx)
                            {
                                maxSelectedChannelIdx = minSelectedChannelIdx;
                                minSelectedChannelIdx = channelIdx;
                            }
                            else
                            {
                                maxSelectedChannelIdx = channelIdx;
                            }
                            if (patternIdx < minSelectedPatternIdx)
                            {
                                maxSelectedPatternIdx = minSelectedPatternIdx;
                                minSelectedPatternIdx = patternIdx;
                            }
                            else
                            {
                                maxSelectedPatternIdx = patternIdx;
                            }
                        }
                        else if (!IsPatternSelected(channelIdx, patternIdx) && pattern != null)
                        {
                            minSelectedChannelIdx = channelIdx;
                            maxSelectedChannelIdx = channelIdx;
                            minSelectedPatternIdx = patternIdx;
                            maxSelectedPatternIdx = patternIdx;
                        }

                        selectionDragAnchorX = e.X - TrackNameSizeX + scrollX - minSelectedPatternIdx * PatternSizeX;
                        selectionDragStartX = e.X;

                        if (pattern != null)
                        {
                            PatternClicked?.Invoke(channelIdx, patternIdx);
                        }

                        ConditionalInvalidate();
                    }
                }
                else if (right && pattern != null)
                {
                    App.UndoRedoManager.BeginTransaction(TransactionScope.Song, Song.Id);
                    channel.PatternInstances[patternIdx] = null;
                    App.UndoRedoManager.EndTransaction();
                    ConditionalInvalidate();
                }
            }
        }

        protected void UpdateCursor()
        {
            if (isDraggingSelection)
            {
                Cursor = ModifierKeys.HasFlag(Keys.Control) ? Theme.CopyCursor : Theme.DragCursor;
            }
            else
            {
                Cursor = Cursors.Default;
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            if (isDraggingSelection)
            {
                bool copy = ModifierKeys.HasFlag(Keys.Control);

                int centerX = e.X - selectionDragAnchorX + PatternSizeX / 2;
                int basePatternIdx = (centerX - TrackNameSizeX + scrollX) / PatternSizeX;

                Pattern[,] tmpPatterns = new Pattern[maxSelectedChannelIdx - minSelectedChannelIdx + 1, maxSelectedPatternIdx - minSelectedPatternIdx + 1];

                App.UndoRedoManager.BeginTransaction(TransactionScope.Song, Song.Id);
                
                for (int i = minSelectedChannelIdx; i <= maxSelectedChannelIdx; i++)
                {
                    for (int j = minSelectedPatternIdx; j <= maxSelectedPatternIdx; j++)
                    {
                        tmpPatterns[i - minSelectedChannelIdx, j - minSelectedPatternIdx] = Song.Channels[i].PatternInstances[j];
                        if (!copy)
                        {
                            Song.Channels[i].PatternInstances[j] = null;
                        }
                    }
                }

                for (int i = minSelectedChannelIdx; i <= maxSelectedChannelIdx; i++)
                {
                    for (int j = minSelectedPatternIdx; j <= maxSelectedPatternIdx; j++)
                    {
                        Song.Channels[i].PatternInstances[j + basePatternIdx - minSelectedPatternIdx] = tmpPatterns[i - minSelectedChannelIdx, j - minSelectedPatternIdx];
                    }
                }

                App.UndoRedoManager.EndTransaction();

                ClearSelection();
                ConditionalInvalidate();
            }

            CancelDragSelection();
            UpdateCursor();
        }

        protected void CancelDragSelection()
        {
            selectionDragAnchorX = -64;
            selectionDragStartX = -1;
            isDraggingSelection = false;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.KeyCode == Keys.Escape)
            {
                CancelDragSelection();
                UpdateCursor();
                ClearSelection();
                ConditionalInvalidate();
            }

            if (e.KeyCode == Keys.Delete)
            {
                App.UndoRedoManager.BeginTransaction(TransactionScope.Song, Song.Id);
                for (int i = minSelectedChannelIdx; i <= maxSelectedChannelIdx; i++)
                {
                    for (int j = minSelectedPatternIdx; j <= maxSelectedPatternIdx; j++)
                    {
                        Song.Channels[i].PatternInstances[j] = null;
                    }
                }
                App.UndoRedoManager.EndTransaction();

                ConditionalInvalidate();
            }

            if (isDraggingSelection)
            {
                UpdateCursor();
            }
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);

            if (isDraggingSelection)
            {
                UpdateCursor();
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            bool left   = e.Button.HasFlag(MouseButtons.Left);
            bool middle = e.Button.HasFlag(MouseButtons.Middle) || (e.Button.HasFlag(MouseButtons.Left) && ModifierKeys.HasFlag(Keys.Alt));

            bool inPatternZone = GetPatternForCoord(e.X, e.Y, out int channelIdx, out int patternIdx);

            if (middle)
            {
                int deltaX = e.X - mouseLastX;
                int deltaY = e.Y - mouseLastY;

                DoScroll(deltaX, deltaY);

                mouseLastX = e.X;
                mouseLastY = e.Y;
            }
            else if (left && inPatternZone)
            {
                if (!isDraggingSelection && selectionDragStartX > 0 && Math.Abs(e.X - selectionDragStartX) > 5)
                {
                    isDraggingSelection = true;
                }

                //if (IsPatternSelected(channelIdx, patternIdx))
                //{
                //    //selectionDragPointX = patternIdx;
                //}

                ConditionalInvalidate();
            }

            UpdateCursor();
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);

            bool left = (e.Button & MouseButtons.Left) != 0;
            bool inPatternZone = GetPatternForCoord(e.X, e.Y, out int channelIdx, out int barIdx);

            if (left && inPatternZone)
            {
                var channel = Song.Channels[channelIdx];
                var pattern = channel.PatternInstances[barIdx];

                if (pattern != null)
                {
                    bool multiplePatternSelected = (maxSelectedChannelIdx != minSelectedChannelIdx) || (minSelectedPatternIdx != maxSelectedPatternIdx);

                    var dlg = new RenameColorDialog(pattern.Name, pattern.Color, !multiplePatternSelected)
                    {
                        StartPosition = FormStartPosition.Manual,
                        Location = PointToScreen(new System.Drawing.Point(e.X, e.Y))
                    };

                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        App.UndoRedoManager.BeginTransaction(TransactionScope.Song, Song.Id);

                        if (multiplePatternSelected)
                        {
                            for (int i = minSelectedChannelIdx; i <= maxSelectedChannelIdx; i++)
                            {
                                for (int j = minSelectedPatternIdx; j <= maxSelectedPatternIdx; j++)
                                {
                                    Song.Channels[i].PatternInstances[j].Color = dlg.NewColor;
                                }
                            }
                        }
                        else if (Song.Channels[selectedChannel].RenamePattern(pattern, dlg.NewName))
                        {
                            pattern.Color = dlg.NewColor;
                        }

                        ConditionalInvalidate();
                        App.UndoRedoManager.EndTransaction();
                    }
                }
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);

            int pixelX = e.X - TrackNameSizeX;
            int absoluteX = pixelX + scrollX;
            if (e.Delta < 0 && zoomLevel > MinZoomLevel) { zoomLevel--; absoluteX /= 2; selectionDragAnchorX /= 2; }
            if (e.Delta > 0 && zoomLevel < MaxZoomLevel) { zoomLevel++; absoluteX *= 2; selectionDragAnchorX *= 2; }
            scrollX = absoluteX - pixelX;

            ClampScroll();
            ConditionalInvalidate();
        }

        public void InvalidatePatternCache()
        {
            patternBitmapCache.Clear();
            ConditionalInvalidate();
        }

        public void SerializeState(ProjectBuffer buffer)
        {
            buffer.Serialize(ref selectedChannel);
            buffer.Serialize(ref scrollX);
            buffer.Serialize(ref zoomLevel);
            buffer.Serialize(ref minSelectedChannelIdx);
            buffer.Serialize(ref maxSelectedChannelIdx);
            buffer.Serialize(ref minSelectedPatternIdx);
            buffer.Serialize(ref maxSelectedPatternIdx);

            if (buffer.IsReading)
            {
                // TODO: This is overly aggressive. We should have the 
                // scope on the transaction on the buffer and filter by that.
                InvalidatePatternCache();
                CancelDragSelection();
                ConditionalInvalidate();
            }
        }
    }
}
