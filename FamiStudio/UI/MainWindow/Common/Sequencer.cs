using System;
using System.Drawing;
using System.Collections.Generic;
using System.Windows.Forms;
using FamiStudio.Properties;
using Color = System.Drawing.Color;
using System.Media;
using System.Diagnostics;

#if FAMISTUDIO_WINDOWS
    using RenderBitmap   = SharpDX.Direct2D1.Bitmap;
    using RenderBrush    = SharpDX.Direct2D1.Brush;
    using RenderPath     = SharpDX.Direct2D1.PathGeometry;
    using RenderControl  = FamiStudio.Direct2DControl;
    using RenderGraphics = FamiStudio.Direct2DGraphics;
    using RenderTheme    = FamiStudio.Direct2DTheme;
#else
    using RenderBitmap   = FamiStudio.GLBitmap;
    using RenderBrush    = FamiStudio.GLBrush;
    using RenderPath     = FamiStudio.GLConvexPath;
    using RenderControl  = FamiStudio.GLControl;
    using RenderGraphics = FamiStudio.GLGraphics;
    using RenderTheme    = FamiStudio.GLTheme;
#endif

namespace FamiStudio
{
    public class Sequencer : RenderControl
    {
        const int DefaultTrackNameSizeX     = 81;
        const int DefaultHeaderSizeY        = 17;
        const int DefaultPatternHeaderSizeY = 16;
        const int DefaultNoteSizeY          = 4;
        const int DefaultScrollMargin       = 128;
        const int DefaultBarTextPosY        = 2;
        const int DefaultTrackIconPosX      = 4;
        const int DefaultTrackIconPosY      = 4;
        const int DefaultTrackNamePosX      = 24;
        const int DefaultTrackNamePosY      = 4;
        const int DefaultGhostNoteOffsetX   = 16;
        const int DefaultGhostNoteOffsetY   = 16;
        const int DefaultPatternNamePosX    = 2;
        const int DefaultPatternNamePosY    = 3;

        const int MinZoomLevel = -2;
        const int MaxZoomLevel = 4;

        int trackNameSizeX;
        int headerSizeY;
        int trackSizeY;
        int patternHeaderSizeY;
        int noteSizeY;
        int scrollMargin;
        int barTextPosY;  
        int trackIconPosX;   
        int trackIconPosY;   
        int trackNamePosX;   
        int trackNamePosY;   
        int ghostNoteOffsetX;
        int ghostNoteOffsetY;
        int patternNamePosX;
        int patternNamePosY;
        int patternSizeX;
        
        int scrollX = 0;
        int zoomLevel = 1;
        int mouseLastX = 0;
        int mouseLastY = 0;
        int selectedChannel = 0;

        enum CaptureOperation
        {
            None,
            Select,
            ClickPattern,
            DragSelection
        }

        bool showSelection = true;
        int captureStartX = -1;
        int captureStartY = -1;
        int selectionDragAnchorX = -64;
        int minSelectedChannelIdx = -1;
        int maxSelectedChannelIdx = -1;
        int minSelectedPatternIdx = -1;
        int maxSelectedPatternIdx = -1;
        CaptureOperation captureOperation = CaptureOperation.None;
        
        int ScaleForZoom(int value)
        {
            return zoomLevel < 0 ? value / (1 << (-zoomLevel)) : value * (1 << zoomLevel);
        }

        Dictionary<int, RenderBitmap> patternBitmapCache = new Dictionary<int, RenderBitmap>();

        RenderTheme theme;
        RenderBrush seekBarBrush;
        RenderBrush whiteKeyBrush;
        RenderBrush patternHeaderBrush;
        RenderBrush selectedPatternVisibleBrush;
        RenderBrush selectedPatternInvisibleBrush;
        RenderPath seekGeometry;

        RenderBitmap[] bmpTracks = new RenderBitmap[Channel.Count];
        RenderBitmap bmpGhostNote;

        public delegate void TrackBarDelegate(int trackIdx, int barIdx);
        public delegate void ChannelDelegate(int channelIdx);
        public delegate void EmptyDelegate();

        public event TrackBarDelegate PatternClicked;
        public event ChannelDelegate SelectedChannelChanged;
        public event EmptyDelegate ControlActivated;
        public event EmptyDelegate PatternModified;
        public event EmptyDelegate PatternsPasted;

        public Sequencer()
        {
            UpdateRenderCoords();
        }

        private Song Song
        {
            get { return App?.Song; }
        }

        public bool ShowSelection
        {
            get { return showSelection; }
            set { showSelection = value; ConditionalInvalidate(); }
        }

        public int SelectedChannel => selectedChannel;

        private void ConditionalInvalidate()
        {
            if (!App.RealTimeUpdate)
                Invalidate();
        }

        private void UpdateRenderCoords()
        {
            var scaling = RenderTheme.MainWindowScaling;

            trackNameSizeX     = (int)(DefaultTrackNameSizeX * scaling);
            headerSizeY        = (int)(DefaultHeaderSizeY * scaling);
            trackSizeY         = (int)(ComputeDesiredTrackSizeY() * scaling);
            patternHeaderSizeY = (int)(DefaultPatternHeaderSizeY * scaling);
            noteSizeY          = (int)(DefaultNoteSizeY * scaling);
            scrollMargin       = (int)(DefaultScrollMargin * scaling);
            barTextPosY        = (int)(DefaultBarTextPosY * scaling);
            trackIconPosX      = (int)(DefaultTrackIconPosX * scaling);
            trackIconPosY      = (int)(DefaultTrackIconPosY * scaling);
            trackNamePosX      = (int)(DefaultTrackNamePosX * scaling);
            trackNamePosY      = (int)(DefaultTrackNamePosY * scaling);
            ghostNoteOffsetX   = (int)(DefaultGhostNoteOffsetX * scaling);
            ghostNoteOffsetY   = (int)(DefaultGhostNoteOffsetY * scaling);
            patternNamePosX    = (int)(DefaultPatternNamePosX * scaling);
            patternNamePosY    = (int)(DefaultPatternNamePosY * scaling);
            patternSizeX       = (int)(ScaleForZoom(Song == null ? 256 : Song.PatternLength) * scaling);
        }

        private int GetChannelCount()
        {
            return App?.Project != null ? App.Project.Songs[0].Channels.Length : 5;
        }

        private int ComputeDesiredTrackSizeY()
        {
            return Math.Max(280 / GetChannelCount(), 40);
        }

        public int ComputeDesiredSizeY()
        {
            // Does not include scaling.
            return ComputeDesiredTrackSizeY() * GetChannelCount() + DefaultHeaderSizeY + 1;
        }

        public void SequencerLayoutChanged()
        {
            UpdateRenderCoords();
            InvalidatePatternCache();
            ConditionalInvalidate();
        }

        public void Reset()
        {
            scrollX = 0;
            zoomLevel = 1;
            selectedChannel = 0;
            ClearSelection();
            UpdateRenderCoords();
            InvalidatePatternCache();
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

        protected override void OnRenderInitialized(RenderGraphics g)
        {
            theme = RenderTheme.CreateResourcesForGraphics(g);

            bmpTracks[Channel.Square1] = g.CreateBitmapFromResource("Square");
            bmpTracks[Channel.Square2] = g.CreateBitmapFromResource("Square");
            bmpTracks[Channel.Triangle] = g.CreateBitmapFromResource("Triangle");
            bmpTracks[Channel.Noise] = g.CreateBitmapFromResource("Noise");
            bmpTracks[Channel.DPCM] = g.CreateBitmapFromResource("DPCM");
            bmpTracks[Channel.VRC6Square1] = g.CreateBitmapFromResource("SquareExp");
            bmpTracks[Channel.VRC6Square2] = g.CreateBitmapFromResource("SquareExp");
            bmpTracks[Channel.VRC6Saw] = g.CreateBitmapFromResource("Saw");

            bmpGhostNote = g.CreateBitmapFromResource("GhostSmall");

            seekBarBrush = g.CreateSolidBrush(ThemeBase.SeekBarColor);
            whiteKeyBrush = g.CreateHorizontalGradientBrush(0, trackNameSizeX, ThemeBase.LightGreyFillColor1, ThemeBase.LightGreyFillColor2);
            patternHeaderBrush = g.CreateVerticalGradientBrush(0, patternHeaderSizeY, ThemeBase.LightGreyFillColor1, ThemeBase.LightGreyFillColor2);
            selectedPatternVisibleBrush   = g.CreateSolidBrush(Color.FromArgb(64, ThemeBase.LightGreyFillColor1));
            selectedPatternInvisibleBrush = g.CreateSolidBrush(Color.FromArgb(32, ThemeBase.LightGreyFillColor1));

            seekGeometry = g.CreateConvexPath(new[]
            {
                new Point(-headerSizeY / 2, 1),
                new Point(0, headerSizeY - 2),
                new Point( headerSizeY / 2, 1)
            });
        }

        protected override void OnResize(EventArgs e)
        {
            UpdateRenderCoords();
            base.OnResize(e);
        }

        private bool IsSelectionValid()
        {
            return minSelectedPatternIdx >= 0 &&
                   maxSelectedPatternIdx >= 0 &&
                   minSelectedChannelIdx >= 0 &&
                   maxSelectedChannelIdx >= 0;
        }

        protected override void OnRender(RenderGraphics g)
        {
            g.Clear(ThemeBase.DarkGreyFillColor1);

            int seekX = ScaleForZoom(App.CurrentFrame) - scrollX;
            int minVisiblePattern = Math.Max((int)Math.Floor(scrollX / (float)patternSizeX), 0);
            int maxVisiblePattern = Math.Min((int)Math.Ceiling((scrollX + Width) / (float)patternSizeX), Song.Length);

            // Track name background
            g.FillRectangle(0, 0, trackNameSizeX, Height, whiteKeyBrush);

            if (IsSelectionValid())
            {
                g.PushClip(trackNameSizeX, 0, Width, headerSizeY);
                g.PushTranslation(trackNameSizeX, 0);
                g.FillRectangle(
                    (minSelectedPatternIdx + 0) * patternSizeX - scrollX, 0,
                    (maxSelectedPatternIdx + 1) * patternSizeX - scrollX, headerSizeY,
                    showSelection ? selectedPatternVisibleBrush : selectedPatternInvisibleBrush);
                g.PopTransform();
                g.PopClip();
            }

            // Header
            g.DrawLine(0, 0, Width, 0, theme.BlackBrush);
            g.DrawLine(trackNameSizeX - 1, 0, trackNameSizeX - 1, headerSizeY, theme.DarkGreyLineBrush1);
            g.PushTranslation(trackNameSizeX, 0);
            g.PushClip(0, 0, Width, Height);

            for (int i = minVisiblePattern, x = minVisiblePattern * patternSizeX - scrollX; i <= maxVisiblePattern; i++, x += patternSizeX)
                if (i != 0) g.DrawLine(x, 0, x, Height, theme.DarkGreyLineBrush1);

            for (int i = minVisiblePattern, x = minVisiblePattern * patternSizeX - scrollX; i < maxVisiblePattern; i++, x += patternSizeX)
            {
                g.PushTranslation(x, 0);
                g.DrawText(i.ToString(), ThemeBase.FontMediumCenter, 0, barTextPosY, theme.LightGreyFillBrush1, patternSizeX);
                g.PopTransform();
            }

            g.PushTranslation(seekX, 0);
            g.FillAndDrawConvexPath(seekGeometry, seekBarBrush, theme.BlackBrush);
            g.PopTransform();

            g.PopClip();
            g.PopTransform();

            g.PushTranslation(0, headerSizeY);

            // Icons
            for (int i = 0, y = 0; i < Song.Channels.Length; i++, y += trackSizeY)
                g.DrawBitmap(bmpTracks[(int)Song.Channels[i].Type], trackIconPosX, y + trackIconPosY, (App.ChannelMask & (1 << i)) != 0 ? 1.0f : 0.2f);

            // Track names
            for (int i = 0, y = 0; i < Song.Channels.Length; i++, y += trackSizeY)
                g.DrawText(Song.Channels[i].Name, i == selectedChannel ? ThemeBase.FontMediumBold : ThemeBase.FontMedium, trackNamePosX, y + trackNamePosY, theme.BlackBrush);

            // Ghost note icons
            for (int i = 0, y = 0; i < Song.Channels.Length; i++, y += trackSizeY)
                g.DrawBitmap(bmpGhostNote, trackNameSizeX - ghostNoteOffsetX, y + trackSizeY - ghostNoteOffsetY - 1, (App.GhostChannelMask & (1 << i)) != 0 ? 1.0f : 0.2f);

            // Vertical line seperating the track labels.
            g.DrawLine(trackNameSizeX - 1, 0, trackNameSizeX - 1, Height, theme.DarkGreyLineBrush1);

            // Grey background rectangles ever other pattern + vertical lines 
            g.PushClip(trackNameSizeX, 0, Width, Height);
            g.PushTranslation(trackNameSizeX, 0);
            for (int i = minVisiblePattern, x = minVisiblePattern * patternSizeX - scrollX; i < maxVisiblePattern; i++, x += patternSizeX)
                if ((i & 1) == 0) g.FillRectangle(x, 0, x + patternSizeX, Height, theme.DarkGreyFillBrush2);
            for (int i = minVisiblePattern, x = minVisiblePattern * patternSizeX - scrollX; i <= maxVisiblePattern; i++, x += patternSizeX)
                if (i != 0) g.DrawLine(x, 0, x, Height, theme.DarkGreyLineBrush1);

            if (IsSelectionValid())
            {
                g.FillRectangle(
                    (minSelectedPatternIdx + 0) * patternSizeX - scrollX, trackSizeY * (minSelectedChannelIdx + 0),
                    (maxSelectedPatternIdx + 1) * patternSizeX - scrollX, trackSizeY * (maxSelectedChannelIdx + 1), 
                    showSelection ? selectedPatternVisibleBrush : selectedPatternInvisibleBrush);
            }

            g.PopTransform();
            g.PopClip();

            // Horizontal lines
            for (int i = 0, y = 0; i < Song.Channels.Length; i++, y += trackSizeY)
                g.DrawLine(0, y, Width, y, theme.DarkGreyLineBrush1);

            g.PushClip(trackNameSizeX, 0, Width, Height);

            // Seek
            g.DrawLine(seekX + trackNameSizeX, 1, seekX + trackNameSizeX, Height, seekBarBrush, 3);

            // Patterns
            for (int t = 0, y = 0; t < Song.Channels.Length; t++, y += trackSizeY)
            {
                for (int i = minVisiblePattern, x = minVisiblePattern * patternSizeX + trackNameSizeX - scrollX; i < maxVisiblePattern; i++, x += patternSizeX)
                {
                    var pattern = Song.Channels[t].PatternInstances[i];

                    if (pattern != null)
                    {
                        //var selected = IsPatternSelected(t, i);
                        var bmp = GetPatternBitmapFromCache(g, pattern);

                        g.PushTranslation(x, y);
                        g.FillRectangle(1, 1, patternSizeX, patternHeaderSizeY, g.GetVerticalGradientBrush(pattern.Color, patternHeaderSizeY - 1, 0.9f));
                        g.DrawLine(0, patternHeaderSizeY, patternSizeX, patternHeaderSizeY, theme.DarkGreyLineBrush1);
                        //if (selected)
                        //    g.FillRectangle(1, 1 + patternHeaderSizeY, patternSizeX, patternHeaderSizeY + bmp.Size.Height + 1, showSelection ? selectedPatternVisibleBrush : selectedPatternInvisibleBrush);

                        g.DrawBitmap(bmp, 1, 1 + patternHeaderSizeY, patternSizeX - 1, bmp.Size.Height, 1.0f);
                        g.PushClip(0, 0, patternSizeX, trackSizeY);
                        g.DrawText(pattern.Name, ThemeBase.FontSmall, patternNamePosX, patternNamePosY, theme.BlackBrush);
                        g.PopClip();
                        g.PopTransform();
                    }
                }
            }

            // Dragging selection
            if (captureOperation == CaptureOperation.DragSelection)
            {
                var pt = this.PointToClient(Cursor.Position);

                //pt.X -= TrackNameSizeX;
                pt.Y -= headerSizeY;

                var drawX = pt.X - selectionDragAnchorX;
                var drawY = minSelectedChannelIdx * trackSizeY;
                var sizeX = (maxSelectedPatternIdx - minSelectedPatternIdx + 1) * patternSizeX;
                var sizeY = (maxSelectedChannelIdx - minSelectedChannelIdx + 1) * trackSizeY;

                g.FillRectangle(drawX, drawY, drawX + sizeX, drawY + sizeY, showSelection ? selectedPatternVisibleBrush : selectedPatternInvisibleBrush);
            }

            g.PopClip();
            g.PopTransform();

            g.DrawLine(0, Height - 1, Width, Height - 1, theme.DarkGreyLineBrush1);
        }

        public void NotifyPatternChange(Pattern pattern)
        {
            patternBitmapCache.Remove(pattern.Id);
        }

        private unsafe RenderBitmap GetPatternBitmapFromCache(RenderGraphics g, Pattern p)
        {
            int patternSizeX = Song.PatternLength - 1;
            int patternSizeY = trackSizeY - patternHeaderSizeY - 1;

            RenderBitmap bmp;

            if (patternBitmapCache.TryGetValue(p.Id, out bmp))
            {
                if (bmp.Size.Width == patternSizeX)
                {
                    return bmp;
                }
                else
                {
                    patternBitmapCache.Remove(p.Id);
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
                        float scaleY = (patternSizeY - noteSizeY) / (float)patternSizeY;

                        int x = i;
                        int y = Math.Min((int)Math.Round((lastValid.Value - minNote.Value) / (float)(maxNote.Value - minNote.Value) * scaleY * patternSizeY), patternSizeY - noteSizeY);

                        var instrument = lastValid.Instrument;
                        var color = instrument == null ? ThemeBase.LightGreyFillColor1 : instrument.Color;

                        for (int j = 0; j < noteSizeY; j++)
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

            bmp = g.CreateBitmap(patternSizeX, patternSizeY, data);
            patternBitmapCache[p.Id] = bmp;

            return bmp;
        }

        private void ClampScroll()
        {
            int minScrollX = 0;
            int maxScrollX = Math.Max(Song.Length * patternSizeX - scrollMargin, 0);

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
            pattern = (x - trackNameSizeX + scrollX) / patternSizeX;
            track = (y - headerSizeY) / trackSizeY;

            return (x > trackNameSizeX && y > headerSizeY && pattern >= 0 && pattern < Song.Length && track >= 0 && track < Song.Channels.Length);
        }

        Rectangle GetTrackIconRect(int idx)
        {
            return new Rectangle(
                trackIconPosX,
                trackIconPosY + headerSizeY + idx * trackSizeY, 
                (int)(16 * RenderTheme.MainWindowScaling),
                (int)(16 * RenderTheme.MainWindowScaling));
        }

        Rectangle GetTrackGhostRect(int idx)
        {
            return new Rectangle(
                trackNameSizeX - ghostNoteOffsetX, 
                headerSizeY + (idx + 1) * trackSizeY - ghostNoteOffsetY - 1, 
                (int)(12 * RenderTheme.MainWindowScaling), 
                (int)(12 * RenderTheme.MainWindowScaling));
        }

        private void StartCaptureOperation(MouseEventArgs e, CaptureOperation op)
        {
            Debug.Assert(captureOperation == CaptureOperation.None);
            mouseLastX = e.X;
            mouseLastY = e.Y;
            captureStartX = e.X;
            captureStartY = e.Y;
            captureOperation = op;
            Capture = true;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            ControlActivated?.Invoke();

            bool left   = e.Button.HasFlag(MouseButtons.Left);
            bool middle = e.Button.HasFlag(MouseButtons.Middle) || (e.Button.HasFlag(MouseButtons.Left) && ModifierKeys.HasFlag(Keys.Alt));
            bool right  = e.Button.HasFlag(MouseButtons.Right);

            bool canCapture = captureOperation == CaptureOperation.None;

            CancelDragSelection();
            UpdateCursor();

            if (middle)
            {
                mouseLastX = e.X;
                mouseLastY = e.Y;
                Capture = true;
                return;
            }

            // Track muting, soloing.
            else if ((left || right) && e.X < trackNameSizeX)
            {
                var trackIcon = GetTrackIconForPos(e);
                var ghostIcon = GetTrackGhostForPos(e);

                if (trackIcon >= 0)
                {
                    int bit = (1 << trackIcon);

                    if (left)
                    {
                        // Toggle muted
                        App.ChannelMask ^= bit;
                    }
                    else
                    {
                        // Toggle Solo
                        if (App.ChannelMask == bit)
                            App.ChannelMask = 0xff;
                        else
                            App.ChannelMask = bit;
                    }

                    ConditionalInvalidate();
                    return;
                }
                else if (ghostIcon >= 0)
                {
                    App.GhostChannelMask ^= (1 << ghostIcon);
                    ConditionalInvalidate();
                    return;
                }
            }

            if (IsMouseInHeader(e))
            {
                if (left)
                {
                    int frame = (int)Math.Round((e.X - trackNameSizeX + scrollX) / (float)patternSizeX * Song.PatternLength);
                    App.Seek(frame);
                }
                else if (right && canCapture)
                {
                    StartCaptureOperation(e, CaptureOperation.Select);
                    UpdateSelection(e.X, true);
                }
            }
            else if (e.Y > headerSizeY && (left || right))
            {
                if (e.Y > headerSizeY)
                {
                    var newChannel = Utils.Clamp((e.Y - headerSizeY) / trackSizeY, 0, Song.Channels.Length - 1);
                    if (newChannel != selectedChannel)
                    {
                        selectedChannel = newChannel;
                        SelectedChannelChanged?.Invoke(selectedChannel);
                        ConditionalInvalidate();
                    }
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
                        PatternClicked?.Invoke(channelIdx, patternIdx);
                        App.UndoRedoManager.EndTransaction();
                        ClearSelection();
                        ConditionalInvalidate();
                    }
                    else if (canCapture)
                    {
                        if (pattern != null)
                        {
                            PatternClicked?.Invoke(channelIdx, patternIdx);
                        }

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

                            return;
                        }
                        else if (!IsPatternSelected(channelIdx, patternIdx) && pattern != null)
                        {
                            minSelectedChannelIdx = channelIdx;
                            maxSelectedChannelIdx = channelIdx;
                            minSelectedPatternIdx = patternIdx;
                            maxSelectedPatternIdx = patternIdx;
                        }

                        selectionDragAnchorX = e.X - trackNameSizeX + scrollX - minSelectedPatternIdx * patternSizeX;
                        StartCaptureOperation(e, CaptureOperation.ClickPattern);

                        ConditionalInvalidate();
                    }
                }
                else if (right && pattern != null)
                {
                    App.UndoRedoManager.BeginTransaction(TransactionScope.Song, Song.Id);
                    channel.PatternInstances[patternIdx] = null;
                    App.UndoRedoManager.EndTransaction();
                    ClearSelection();
                    ConditionalInvalidate();
                }
            }
        }

        private Pattern[,] GetSelectedPatterns()
        {
            if (!IsSelectionValid())
                return null;

            var patterns = new Pattern[maxSelectedPatternIdx - minSelectedPatternIdx + 1, maxSelectedChannelIdx - minSelectedChannelIdx + 1];

            for (int i = 0; i < patterns.GetLength(0); i++)
            {
                for (int j = 0; j < patterns.GetLength(1); j++)
                {
                    patterns[i, j] = Song.Channels[minSelectedChannelIdx + j].PatternInstances[minSelectedPatternIdx + i];
                }
            }

            return patterns;
        }

        public bool CanCopy  => showSelection && IsSelectionValid();
        public bool CanPaste => showSelection && IsSelectionValid() && ClipboardUtils.ConstainsPatterns;

        public void Copy()
        {
            if (IsSelectionValid())
            {
                ClipboardUtils.SavePatterns(App.Project, GetSelectedPatterns());
            }
        }

        public void Cut()
        {
            if (IsSelectionValid())
            {
                ClipboardUtils.SavePatterns(App.Project, GetSelectedPatterns());
                DeleteSelection();
            }
        }

        public void Paste()
        {
            if (!IsSelectionValid())
                return;

            var mergeInstruments = ClipboardUtils.ContainsMissingInstruments(App.Project, false);

            bool createMissingInstrument = false;
            if (mergeInstruments)
            {
                createMissingInstrument = PlatformUtils.MessageBox($"You are pasting notes referring to unknown instruments. Do you want to create the missing instrument?", "Paste", MessageBoxButtons.YesNo) == DialogResult.Yes;
            }

            App.UndoRedoManager.BeginTransaction(createMissingInstrument ? TransactionScope.Project : TransactionScope.Song, Song.Id);

            var patterns = ClipboardUtils.LoadPatterns(App.Project, Song, createMissingInstrument);

            if (patterns == null)
            {
                App.UndoRedoManager.AbortTransaction();
                return;
            }

            for (int i = 0; i < patterns.GetLength(0); i++)
            {
                for (int j = 0; j < patterns.GetLength(1); j++)
                {
                    var pattern = patterns[i, j];

                    if (pattern != null && (i + minSelectedPatternIdx) < Song.Length &&
                        pattern.ChannelType  < Song.Channels.Length &&
                        pattern.ChannelType == Song.Channels[pattern.ChannelType].Type)
                    {
                        Song.Channels[pattern.ChannelType].PatternInstances[i + minSelectedPatternIdx] = pattern;
                    }
                }
            }

            App.UndoRedoManager.EndTransaction();
            PatternsPasted?.Invoke();
            ConditionalInvalidate();
        }

        protected void UpdateCursor()
        {
            if (captureOperation == CaptureOperation.DragSelection)
            {
#if !FAMISTUDIO_LINUX
                // TODO LINUX: Cursors
                Cursor.Current = ModifierKeys.HasFlag(Keys.Control) ? Cursors.CopyCursor : Cursors.DragCursor;
#endif
            }
            else
            {
                Cursor.Current = Cursors.Default;
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            if (captureOperation != CaptureOperation.None)
            {
                if (captureOperation == CaptureOperation.ClickPattern)
                {
                    if (GetPatternForCoord(e.X, e.Y, out int channelIdx, out int patternIdx))
                    {
                        minSelectedChannelIdx = channelIdx;
                        maxSelectedChannelIdx = channelIdx;
                        minSelectedPatternIdx = patternIdx;
                        maxSelectedPatternIdx = patternIdx;
                        ConditionalInvalidate();
                    }
                    else
                    {
                        ClearSelection();
                    }
                }
                else if (captureOperation == CaptureOperation.DragSelection)
                {
                    bool copy = ModifierKeys.HasFlag(Keys.Control);

                    int centerX = e.X - selectionDragAnchorX + patternSizeX / 2;
                    int basePatternIdx = (centerX - trackNameSizeX + scrollX) / patternSizeX;

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

                Capture = false;
                captureOperation = CaptureOperation.None;
            }

            CancelDragSelection();
            UpdateCursor();
        }

        protected void CancelDragSelection()
        {
            if (captureOperation == CaptureOperation.DragSelection)
            {
                selectionDragAnchorX = -64;
                captureOperation = CaptureOperation.None;
            }
        }

        private void DeleteSelection()
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

#if FAMISTUDIO_WINDOWS
        public void UnfocusedKeyDown(KeyEventArgs e)
        {
            OnKeyDown(e);
        }

        public void UnfocusedKeyUp(KeyEventArgs e)
        {
            OnKeyUp(e);
        }
#endif

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                CancelDragSelection();
                UpdateCursor();
                ClearSelection();
                ConditionalInvalidate();
            }
            else if (showSelection)
            {
                bool ctrl = ModifierKeys.HasFlag(Keys.Control);

                if (ctrl)
                {
                    if (e.KeyCode == Keys.C)
                        Copy();
                    else if (e.KeyCode == Keys.X)
                        Cut();
                    else if (e.KeyCode == Keys.V)
                        Paste();
                }

                if (e.KeyCode == Keys.Delete && IsSelectionValid())
                {
                    DeleteSelection();
                }
            }

            if (captureOperation == CaptureOperation.DragSelection)
            {
                UpdateCursor();
            }
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            if (captureOperation == CaptureOperation.DragSelection)
            {
                UpdateCursor();
            }
        }

        private void UpdateSelection(int mouseX, bool first = false)
        {
            if ((mouseX - trackNameSizeX) < 100)
            {
                scrollX -= 16;
                ClampScroll();
            }
            else if ((Width - mouseX) < 100)
            {
                scrollX += 16;
                ClampScroll();
            }

            int patternIdx = Utils.Clamp((mouseX - trackNameSizeX + scrollX) / patternSizeX, 0, Song.Length - 1);

            if (first)
            {
                minSelectedPatternIdx = patternIdx;
                maxSelectedPatternIdx = patternIdx;
                minSelectedChannelIdx = 0;
                maxSelectedChannelIdx = Song.Channels.Length - 1;
            }
            else
            {
                if (mouseX > captureStartX)
                    maxSelectedPatternIdx = patternIdx;
                else
                    minSelectedPatternIdx = patternIdx;
            }

            ConditionalInvalidate();
        }

        private bool IsMouseInPatternZone(MouseEventArgs e)
        {
            return e.Y > headerSizeY && e.X > trackNameSizeX;
        }

        private bool IsMouseInHeader(MouseEventArgs e)
        {
            return e.Y < headerSizeY && e.X > trackNameSizeX;
        }

        private bool IsMouseInTrackName(MouseEventArgs e)
        {
            return e.Y > headerSizeY && e.X < trackNameSizeX;
        }

        private int GetTrackIconForPos(MouseEventArgs e)
        {
            for (int i = 0; i < Song.Channels.Length; i++)
            {
                if (GetTrackIconRect(i).Contains(e.X, e.Y))
                    return i;
            }

            return -1;
        }

        private int GetTrackGhostForPos(MouseEventArgs e)
        {
            for (int i = 0; i < Song.Channels.Length; i++)
            {
                if (GetTrackGhostRect(i).Contains(e.X, e.Y))
                    return i;
            }

            return -1;
        }

        private void UpdateToolTip(MouseEventArgs e)
        {
            string tooltip = "";

            bool inPatternZone = GetPatternForCoord(e.X, e.Y, out int channelIdx, out int patternIdx);

            if (inPatternZone)
            {
                var pattern = Song.Channels[channelIdx].PatternInstances[patternIdx];

                if (pattern == null)
                {
                    tooltip = "{MouseLeft} Add Pattern - {MouseWheel} Pan";
                }
                else
                {
                    if (IsPatternSelected(channelIdx, patternIdx))
                        tooltip = "{Drag} Move Pattern - {Ctrl} {Drag} Clone pattern {MouseLeft}{MouseLeft} Pattern properties - {MouseRight} Delete Pattern - {MouseWheel} Pan";
                    else
                        tooltip = "{MouseLeft} Select Pattern - {MouseLeft}{MouseLeft} Pattern properties - {MouseRight} Delete Pattern - {MouseWheel} Pan";
                }
            }
            else if (IsMouseInHeader(e))
            {
                tooltip = "{MouseLeft} Seek - {MouseRight} Select Colume - {MouseWheel} Pan";
            }
            else if (IsMouseInTrackName(e))
            {
                if (GetTrackIconForPos(e) >= 0)
                    tooltip = "{MouseLeft} Mute Channel - {MouseRight} Solo Channel";
                else if (GetTrackGhostForPos(e) >= 0)
                    tooltip = "{MouseLeft} Toggle channel for display";
                else
                    tooltip = "{MouseLeft} Make channel active";
            }

            App.ToolTip = tooltip;
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

            UpdateToolTip(e);

            if (captureOperation == CaptureOperation.ClickPattern && captureStartX > 0 && Math.Abs(e.X - captureStartX) > 5)
            {
                captureOperation = CaptureOperation.DragSelection;
                ConditionalInvalidate();
            }
            else if (captureOperation == CaptureOperation.Select)
            {
                UpdateSelection(e.X);
            }
            else if (captureOperation == CaptureOperation.DragSelection)
            {
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

                    var dlg = new PropertyDialog(PointToScreen(new Point(e.X, e.Y)), 160);
                    dlg.Properties.AddColoredString(pattern.Name, pattern.Color);
                    dlg.Properties.AddColor(pattern.Color);
                    dlg.Properties.Build();

                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        App.UndoRedoManager.BeginTransaction(TransactionScope.Song, Song.Id);

                        var newName  = dlg.Properties.GetPropertyValue<string>(0);
                        var newColor = dlg.Properties.GetPropertyValue<Color>(1);

                        if (multiplePatternSelected)
                        {
                            for (int i = minSelectedChannelIdx; i <= maxSelectedChannelIdx; i++)
                            {
                                for (int j = minSelectedPatternIdx; j <= maxSelectedPatternIdx; j++)
                                {
                                    Song.Channels[i].PatternInstances[j].Color = newColor;
                                }
                            }
                            App.UndoRedoManager.EndTransaction();
                        }
                        else if (Song.Channels[selectedChannel].RenamePattern(pattern, newName))
                        {
                            pattern.Color = newColor;
                            App.UndoRedoManager.EndTransaction();
                        }
                        else
                        {
                            App.UndoRedoManager.AbortTransaction();
                            SystemSounds.Beep.Play();
                        }

                        ConditionalInvalidate();
                        PatternModified?.Invoke();
                    }
                }
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);

            int pixelX = e.X - trackNameSizeX;
            int absoluteX = pixelX + scrollX;
            if (e.Delta < 0 && zoomLevel > MinZoomLevel) { zoomLevel--; absoluteX /= 2; selectionDragAnchorX /= 2; }
            if (e.Delta > 0 && zoomLevel < MaxZoomLevel) { zoomLevel++; absoluteX *= 2; selectionDragAnchorX *= 2; }
            scrollX = absoluteX - pixelX;

            UpdateRenderCoords();
            ClampScroll();
            ConditionalInvalidate();
        }

        public void Tick()
        {
            if (captureOperation == CaptureOperation.Select)
            {
                var pt = PointToClient(Cursor.Position);
                UpdateSelection(pt.X, false);
            }
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
                UpdateRenderCoords();
                CancelDragSelection();
                ConditionalInvalidate();
            }
        }
    }
}
