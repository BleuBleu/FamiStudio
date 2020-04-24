using System;
using System.Linq;
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
        const int DefaultTrackNameSizeX      = 85;
        const int DefaultHeaderSizeY         = 17;
        const int DefaultPatternHeaderSizeY  = 16;
        const int DefaultNoteSizeY           = 4;
        const int DefaultScrollMargin        = 128;
        const int DefaultBarTextPosY         = 2;
        const int DefaultTrackIconPosX       = 4;
        const int DefaultTrackIconPosY       = 4;
        const int DefaultTrackNamePosX       = 24;
        const int DefaultTrackNamePosY       = 4;
        const int DefaultGhostNoteOffsetX    = 16;
        const int DefaultGhostNoteOffsetY    = 16;
        const int DefaultPatternNamePosX     = 2;
        const int DefaultPatternNamePosY     = 3;
        const int DefaultHeaderIconPosX      = 3;
        const int DefaultHeaderIconPosY      = 3;
        const int DefaultHeaderIconSizeX     = 12;

        const int MinZoomLevel = -2;
        const int MaxZoomLevel =  4;

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
        int headerIconPosX;
        int headerIconPosY;
        int headerIconSizeX;
        float noteSizeX;

        int scrollX = 0;
        int zoomBias = 0;
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
        bool fullColumnSelection = false;
        int firstSelectedPatternIdx = -1;
        int minSelectedChannelIdx = -1;
        int maxSelectedChannelIdx = -1;
        int minSelectedPatternIdx = -1;
        int maxSelectedPatternIdx = -1;
        int   selectionDragAnchorPatternIdx = -1;
        float selectionDragAnchorPatternFraction = -1.0f;
        CaptureOperation captureOperation = CaptureOperation.None;
        
        int ScaleForZoom(int value)
        {
            var actualZoom = zoomLevel + zoomBias;
            return actualZoom < 0 ? value / (1 << (-actualZoom)) : value * (1 << actualZoom);
        }

        float ScaleForZoom(float value)
        {
            var actualZoom = zoomLevel + zoomBias;
            return actualZoom < 0 ? value / (1 << (-actualZoom)) : value * (1 << actualZoom);
        }

        Dictionary<int, List<RenderBitmap>> patternBitmapCache = new Dictionary<int, List<RenderBitmap>>();

        RenderTheme theme;
        RenderBrush seekBarBrush;
        RenderBrush whiteKeyBrush;
        RenderBrush patternHeaderBrush;
        RenderBrush selectedPatternVisibleBrush;
        RenderBrush selectedPatternInvisibleBrush;
        RenderPath seekGeometry;

        RenderBitmap[] bmpTracks = new RenderBitmap[Channel.Count];
        RenderBitmap bmpGhostNote;
        RenderBitmap bmpLoopPoint;
        RenderBitmap bmpCustomLength;

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

        public int SelectedChannel
        {
            get { return selectedChannel; }
            set
            {
                if (value >= 0 && value < Song.Channels.Length)
                {
                    selectedChannel = value;
                    SelectedChannelChanged?.Invoke(selectedChannel);
                    ConditionalInvalidate();
                }
            }
        }

        public void ConditionalInvalidate()
        {
            if (!App.RealTimeUpdate)
                Invalidate();
        }

        private void UpdateRenderCoords()
        {
            var scaling = RenderTheme.MainWindowScaling;

            zoomBias = Song != null ? 6 - (int)Math.Log(Song.PatternLength, 2.0) : 0;

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
            headerIconPosX     = (int)(DefaultHeaderIconPosX * scaling);
            headerIconPosY     = (int)(DefaultHeaderIconPosY * scaling);
            headerIconSizeX    = (int)(DefaultHeaderIconSizeX * scaling);
            noteSizeX          = ScaleForZoom(1.0f) * scaling;
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
            fullColumnSelection = false;
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
            bmpTracks[Channel.Dpcm] = g.CreateBitmapFromResource("DPCM");
            bmpTracks[Channel.Vrc6Square1] = g.CreateBitmapFromResource("Square");
            bmpTracks[Channel.Vrc6Square2] = g.CreateBitmapFromResource("Square");
            bmpTracks[Channel.Vrc7Fm1] = g.CreateBitmapFromResource("DPCM");
            bmpTracks[Channel.Vrc7Fm2] = g.CreateBitmapFromResource("DPCM");
            bmpTracks[Channel.Vrc7Fm3] = g.CreateBitmapFromResource("DPCM");
            bmpTracks[Channel.Vrc7Fm4] = g.CreateBitmapFromResource("DPCM");
            bmpTracks[Channel.Vrc7Fm5] = g.CreateBitmapFromResource("DPCM");
            bmpTracks[Channel.Vrc7Fm6] = g.CreateBitmapFromResource("DPCM");
            bmpTracks[Channel.Vrc6Saw] = g.CreateBitmapFromResource("Saw");
            bmpTracks[Channel.FdsWave] = g.CreateBitmapFromResource("DPCM");
            bmpTracks[Channel.Mmc5Square1] = g.CreateBitmapFromResource("Square");
            bmpTracks[Channel.Mmc5Square2] = g.CreateBitmapFromResource("Square");
            bmpTracks[Channel.N163Wave1] = g.CreateBitmapFromResource("DPCM");
            bmpTracks[Channel.N163Wave2] = g.CreateBitmapFromResource("DPCM");
            bmpTracks[Channel.N163Wave3] = g.CreateBitmapFromResource("DPCM");
            bmpTracks[Channel.N163Wave4] = g.CreateBitmapFromResource("DPCM");
            bmpTracks[Channel.N163Wave5] = g.CreateBitmapFromResource("DPCM");
            bmpTracks[Channel.N163Wave6] = g.CreateBitmapFromResource("DPCM");
            bmpTracks[Channel.N163Wave7] = g.CreateBitmapFromResource("DPCM");
            bmpTracks[Channel.N163Wave8] = g.CreateBitmapFromResource("DPCM");
            bmpTracks[Channel.S5BSquare1] = g.CreateBitmapFromResource("Square");
            bmpTracks[Channel.S5BSquare2] = g.CreateBitmapFromResource("Square");
            bmpTracks[Channel.S5BSquare3] = g.CreateBitmapFromResource("Square");

            bmpGhostNote = g.CreateBitmapFromResource("GhostSmall");
            bmpLoopPoint = g.CreateBitmapFromResource("LoopSmallFill");
            bmpCustomLength = g.CreateBitmapFromResource("CustomPattern");

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

            var seekX = ScaleForZoom(App.CurrentFrame) - scrollX;
            var minVisibleNoteIdx = Math.Max((int)Math.Floor(scrollX / noteSizeX), 0);
            var maxVisibleNoteIdx = Math.Min((int)Math.Ceiling((scrollX + Width) / noteSizeX), Song.GetPatternStartNote(Song.Length));
            var minVisiblePattern = Utils.Clamp(Song.FindPatternInstanceIndex(minVisibleNoteIdx, out _) + 0, 0, Song.Length);
            var maxVisiblePattern = Utils.Clamp(Song.FindPatternInstanceIndex(maxVisibleNoteIdx, out _) + 1, 0, Song.Length);

            // Track name background
            g.FillRectangle(0, 0, trackNameSizeX, Height, whiteKeyBrush);

            if (IsSelectionValid() && fullColumnSelection)
            {
                g.PushClip(trackNameSizeX, 0, Width, headerSizeY);
                g.PushTranslation(trackNameSizeX, 0);
                g.FillRectangle(
                    (int)(Song.GetPatternStartNote(minSelectedPatternIdx + 0) * noteSizeX) - scrollX, 0,
                    (int)(Song.GetPatternStartNote(maxSelectedPatternIdx + 1) * noteSizeX) - scrollX, headerSizeY,
                    showSelection ? selectedPatternVisibleBrush : selectedPatternInvisibleBrush);
                g.PopTransform();
                g.PopClip();
            }

            // Header
            g.DrawLine(0, 0, Width, 0, theme.BlackBrush);
            g.DrawLine(trackNameSizeX - 1, 0, trackNameSizeX - 1, headerSizeY, theme.DarkGreyLineBrush1);
            g.PushTranslation(trackNameSizeX, 0);
            g.PushClip(0, 0, Width, Height);

            for (int i = minVisiblePattern; i <= maxVisiblePattern; i++)
            {
                if (i != 0)
                {
                    var px = (int)(Song.GetPatternStartNote(i) * noteSizeX) - scrollX;
                    g.DrawLine(px, 0, px, Height, theme.DarkGreyLineBrush1);
                }
            }

            for (int i = minVisiblePattern; i < maxVisiblePattern; i++)
            {
                var px = (int)(Song.GetPatternStartNote(i) * noteSizeX) - scrollX;
                var sx = (int)(Song.GetPatternLength(i) * noteSizeX);

                g.PushTranslation(px, 0);

                g.PushClip(1, 0, sx, Height);
                var text = i.ToString();
                if (Song.PatternHasCustomSettings(i))
                    text += "*";
                g.DrawText(text, ThemeBase.FontMediumCenter, 0, barTextPosY, theme.LightGreyFillBrush1, sx);

                //if (Song.PatternHasCustomLength(i))
                //    g.DrawBitmap(bmpCustomLength, sx - headerIconSizeX - headerIconPosX, headerIconPosY);

                g.PopClip();

                if (i == Song.LoopPoint)
                    g.DrawBitmap(bmpLoopPoint, headerIconPosX, headerIconPosY);

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
            for (int i = minVisiblePattern; i < maxVisiblePattern; i++)
            {
                if ((i & 1) == 0)
                {
                    var px = (int)(Song.GetPatternStartNote(i) * noteSizeX) - scrollX;
                    var sx = (int)(Song.GetPatternLength(i) * noteSizeX);
                    g.FillRectangle(px, 0, px + sx, Height, theme.DarkGreyFillBrush2);
                }
            }
            for (int i = minVisiblePattern; i <= maxVisiblePattern; i++)
            {
                if (i != 0)
                {
                    var px = (int)(Song.GetPatternStartNote(i) * noteSizeX) - scrollX;
                    g.DrawLine(px, 0, px, Height, theme.DarkGreyLineBrush1);
                }
            }

            if (IsSelectionValid())
            {
                g.FillRectangle(
                    (int)(Song.GetPatternStartNote(minSelectedPatternIdx + 0) * noteSizeX) - scrollX, trackSizeY * (minSelectedChannelIdx + 0),
                    (int)(Song.GetPatternStartNote(maxSelectedPatternIdx + 1) * noteSizeX) - scrollX, trackSizeY * (maxSelectedChannelIdx + 1), 
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
            for (int t = 0, py = 0; t < Song.Channels.Length; t++, py += trackSizeY)
            {
                for (int i = minVisiblePattern; i < maxVisiblePattern; i++)
                {
                    var patternLen = Song.GetPatternLength(i);
                    var px = (int)(Song.GetPatternStartNote(i) * noteSizeX) + trackNameSizeX - scrollX;
                    var sx = (int)(patternLen * noteSizeX);
                    var pattern = Song.Channels[t].PatternInstances[i];

                    if (pattern != null)
                    {
                        var bmp = GetPatternBitmapFromCache(g, pattern, patternLen);

                        g.PushTranslation(px, py);
                        g.FillRectangle(1, 1, sx, patternHeaderSizeY, g.GetVerticalGradientBrush(pattern.Color, patternHeaderSizeY - 1, 0.9f));
                        g.DrawLine(0, patternHeaderSizeY, sx, patternHeaderSizeY, theme.DarkGreyLineBrush1);
                        g.PushClip(0, 0, sx, trackSizeY);
                        g.DrawBitmap(bmp, 1.5f, 1.5f + patternHeaderSizeY, sx - 1, bmp.Size.Height, 1.0f);
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
                var noteIdx = (int)((pt.X - trackNameSizeX + scrollX) / noteSizeX);

                if (noteIdx >= 0 && noteIdx < Song.GetPatternStartNote(Song.Length))
                {
                    var patternIdx = Song.FindPatternInstanceIndex(noteIdx, out _);
                    var patternIdxDelta = patternIdx - selectionDragAnchorPatternIdx;

                    pt.Y -= headerSizeY;

                    for (int j = minSelectedChannelIdx; j <= maxSelectedChannelIdx; j++)
                    {
                        var y = j * trackSizeY;

                        // Center.
                        var patternSizeX = Song.GetPatternLength(patternIdx) * noteSizeX;
                        var anchorOffsetLeftX = patternSizeX * selectionDragAnchorPatternFraction;
                        var anchorOffsetRightX = patternSizeX * (1.0f - selectionDragAnchorPatternFraction);

                        g.FillAndDrawRectangle(pt.X - anchorOffsetLeftX, y, pt.X - anchorOffsetLeftX + patternSizeX, y + trackSizeY, selectedPatternVisibleBrush, theme.BlackBrush);

                        // Left side
                        for (int p = patternIdx - 1; p >= minSelectedPatternIdx + patternIdxDelta && p >= 0; p--)
                        {
                            patternSizeX = Song.GetPatternLength(p) * noteSizeX;
                            anchorOffsetLeftX += patternSizeX;
                            g.FillAndDrawRectangle(pt.X - anchorOffsetLeftX, y, pt.X - anchorOffsetLeftX + patternSizeX, y + trackSizeY, selectedPatternVisibleBrush, theme.BlackBrush);
                        }

                        // Right side
                        for (int p = patternIdx + 1; p <= maxSelectedPatternIdx + patternIdxDelta && p < Song.Length; p++)
                        {
                            patternSizeX = Song.GetPatternLength(p) * noteSizeX;
                            g.FillAndDrawRectangle(pt.X + anchorOffsetRightX, y, pt.X + anchorOffsetRightX + patternSizeX, y + trackSizeY, selectedPatternVisibleBrush, theme.BlackBrush);
                            anchorOffsetRightX += patternSizeX;
                        }
                    }
                }
            }

            g.PopClip();
            g.PopTransform();

            g.DrawLine(0, Height - 1, Width, Height - 1, theme.DarkGreyLineBrush1);
        }

        public void NotifyPatternChange(Pattern pattern)
        {
            if (pattern != null)
                patternBitmapCache.Remove(pattern.Id);
        }

        private void DrawPatternBitmapNote(int t0, int t1, Note note, int patternSizeX, int patternSizeY, int minNote, int maxNote, float scaleY, uint[] data)
        {
            var y = Math.Min((int)Math.Round((note.Value - minNote) / (float)(maxNote - minNote) * scaleY * patternSizeY), patternSizeY - noteSizeY);
            var instrument = note.Instrument;
            var color = instrument == null ? ThemeBase.LightGreyFillColor1 : instrument.Color;

            for (int j = 0; j < noteSizeY; j++)
                for (int x = t0; x < t1; x++)
                    data[(patternSizeY - 1 - (y + j)) * patternSizeX + x] = (uint)color.ToArgb();
        }

        private RenderBitmap GetPatternBitmapFromCache(RenderGraphics g, Pattern p, int patternLen)
        { 
            int patternSizeX = Math.Max(1, patternLen);
            int patternSizeY = trackSizeY - patternHeaderSizeY - 1;

            var scaleY = (patternSizeY - noteSizeY) / (float)patternSizeY;

            RenderBitmap bmp;

            if (patternBitmapCache.TryGetValue(p.Id, out var list))
            {
                bmp = list.FirstOrDefault(b => b.Size.Width == patternSizeX);

                if (bmp != null)
                    return bmp;
            }

            uint[] data = new uint[patternSizeX * patternSizeY];

            int minNote;
            int maxNote;

            if (p.GetMinMaxNote(out minNote, out maxNote))
            {
                if (maxNote == minNote)
                {
                    minNote = (byte)(minNote - 5);
                    maxNote = (byte)(maxNote + 5);
                }
                else
                {
                    minNote = (byte)(minNote - 2);
                    maxNote = (byte)(maxNote + 2);
                }

                int lastTime = 0;
                Note lastValid = null;

                foreach (var kv in p.Notes)
                {
                    var note = kv.Value;

                    if (kv.Key >= patternLen)
                        break;

                    if (note.IsMusical || note.IsStop)
                    {
                        if (lastValid != null && lastValid.IsValid)
                            DrawPatternBitmapNote(lastTime, kv.Key, lastValid, patternSizeX, patternSizeY, minNote, maxNote, scaleY, data);

                        lastTime  = kv.Key;
                        lastValid = note.IsStop ? null : note;
                    }
                }

                if (lastValid != null && lastValid.IsValid)
                    DrawPatternBitmapNote(lastTime, patternLen, lastValid, patternSizeX, patternSizeY, minNote, maxNote, scaleY, data);
            }

            bmp = g.CreateBitmap(patternSizeX, patternSizeY, data);

            if (!patternBitmapCache.TryGetValue(p.Id, out list))
            {
                list = new List<RenderBitmap>();
                patternBitmapCache[p.Id] = list;
            }

            list.Add(bmp);

            return bmp;
        }

        private void ClampScroll()
        {
            int minScrollX = 0;
            int maxScrollX = Math.Max((int)(Song.GetPatternStartNote(Song.Length) * noteSizeX) - scrollMargin, 0);

            if (scrollX < minScrollX) scrollX = minScrollX;
            if (scrollX > maxScrollX) scrollX = maxScrollX;
        }

        private void DoScroll(int deltaX, int deltaY)
        {
            scrollX -= deltaX;
            ClampScroll();
            ConditionalInvalidate();
        }

        private bool GetPatternForCoord(int x, int y, out int track, out int patternIdx)
        {
            var noteIdx = (int)((x - trackNameSizeX + scrollX) / noteSizeX);

            if (noteIdx < 0 || noteIdx >= Song.GetPatternStartNote(Song.Length))
            {
                track = -1;
                patternIdx = -1;
                return false;
            }

            patternIdx = Song.FindPatternInstanceIndex(noteIdx, out _);
            track = (y - headerSizeY) / trackSizeY;

            return (x > trackNameSizeX && y > headerSizeY && track >= 0 && track < Song.Channels.Length);
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

            bool left    = e.Button.HasFlag(MouseButtons.Left);
            bool middle  = e.Button.HasFlag(MouseButtons.Middle) || (e.Button.HasFlag(MouseButtons.Left) && ModifierKeys.HasFlag(Keys.Alt));
            bool right   = e.Button.HasFlag(MouseButtons.Right);
            bool setLoop = FamiStudioForm.IsKeyDown(Keys.L);

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
                            App.ChannelMask = 0xffff;
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

            bool inPatternZone = GetPatternForCoord(e.X, e.Y, out int channelIdx, out int patternIdx);

            if (IsMouseInHeader(e))
            {
                if (left)
                {
                    if (setLoop)
                    {
                        App.UndoRedoManager.BeginTransaction(TransactionScope.Song, Song.Id);
                        Song.SetLoopPoint(Song.LoopPoint == patternIdx ? -1 : patternIdx);
                        App.UndoRedoManager.EndTransaction();
                        ConditionalInvalidate();
                    }
                    else
                    {
                        int frame = (int)Math.Round((e.X - trackNameSizeX + scrollX) / (float)noteSizeX);
                        App.Seek(frame);
                    }
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

            if (inPatternZone)
            {
                var channel = Song.Channels[channelIdx];
                var pattern = channel.PatternInstances[patternIdx];

                if (left)
                {
                    bool shift = ModifierKeys.HasFlag(Keys.Shift);

                    if (left && setLoop)
                    {
                        App.UndoRedoManager.BeginTransaction(TransactionScope.Song, Song.Id);
                        Song.SetLoopPoint(Song.LoopPoint == patternIdx ? -1 : patternIdx);
                        App.UndoRedoManager.EndTransaction();
                        ConditionalInvalidate();
                    }
                    else if (pattern == null && !shift)
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

                            fullColumnSelection = false;
                            return;
                        }
                        else if (!IsPatternSelected(channelIdx, patternIdx) && pattern != null)
                        {
                            minSelectedChannelIdx = channelIdx;
                            maxSelectedChannelIdx = channelIdx;
                            minSelectedPatternIdx = patternIdx;
                            maxSelectedPatternIdx = patternIdx;
                            fullColumnSelection = false;
                        }

                        selectionDragAnchorPatternIdx = patternIdx;
                        selectionDragAnchorPatternFraction = (e.X - trackNameSizeX + scrollX - (int)(Song.GetPatternStartNote(patternIdx) * noteSizeX)) / (Song.GetPatternLength(patternIdx) * noteSizeX);

                        StartCaptureOperation(e, CaptureOperation.ClickPattern);

                        ConditionalInvalidate();
                    }
                }
                else if (right && pattern != null)
                {
                    var delete = ((e.Y - headerSizeY) / (float)trackSizeY < (Song.Channels.Length - 0.25f)) ||
                        PlatformUtils.MessageBox("Are you sure you want to delete this pattern?", "Delete pattern", MessageBoxButtons.YesNo) == DialogResult.Yes;

                    if (delete)
                    {
                        App.UndoRedoManager.BeginTransaction(TransactionScope.Song, Song.Id);
                        channel.PatternInstances[patternIdx] = null;
                        App.UndoRedoManager.EndTransaction();
                        ClearSelection();
                        ConditionalInvalidate();
                        PatternModified?.Invoke();
                    }
                }
            }
        }

        private Pattern[,] GetSelectedPatterns(out Song.PatternCustomSetting[] customSettings)
        {
            customSettings = null;

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

            if (fullColumnSelection)
            {
                customSettings = new Song.PatternCustomSetting[patterns.GetLength(0)];

                for (int i = 0; i < patterns.GetLength(0); i++)
                    customSettings[i] = Song.GetPatternCustomSettings(minSelectedPatternIdx + i).Clone();
            }

            return patterns;
        }

        public bool CanCopy  => showSelection && IsSelectionValid();
        public bool CanPaste => showSelection && IsSelectionValid() && ClipboardUtils.ConstainsPatterns;

        public void Copy()
        {
            if (IsSelectionValid())
            {
                var selPatterns = GetSelectedPatterns(out var customSettings);
                ClipboardUtils.SavePatterns(App.Project, selPatterns, customSettings);
            }
        }

        public void Cut()
        {
            if (IsSelectionValid())
            {
                var selPatterns = GetSelectedPatterns(out var customSettings);
                ClipboardUtils.SavePatterns(App.Project, selPatterns, customSettings);
                DeleteSelection(true, customSettings != null);
            }
        }

        public void Paste()
        {
            if (!IsSelectionValid())
                return;

            var missingInstruments = ClipboardUtils.ContainsMissingInstrumentsOrSamples(App.Project, false, out var missingSamples);

            bool createMissingInstrument = false;
            if (missingInstruments)
                createMissingInstrument = PlatformUtils.MessageBox($"You are pasting notes referring to unknown instruments. Do you want to create the missing instrument?", "Paste", MessageBoxButtons.YesNo) == DialogResult.Yes;

            bool createMissingSamples = false;
            if (missingSamples)
                createMissingSamples = PlatformUtils.MessageBox($"You are pasting notes referring to unmapped DPCM samples. Do you want to create the missing samples?", "Paste", MessageBoxButtons.YesNo) == DialogResult.Yes;

            App.UndoRedoManager.BeginTransaction(createMissingInstrument || createMissingSamples ? TransactionScope.Project : TransactionScope.Song, Song.Id);

            var patterns = ClipboardUtils.LoadPatterns(App.Project, Song, createMissingInstrument, createMissingSamples, out var customSettings);

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

                    if (pattern != null && (i + minSelectedPatternIdx) < Song.Length && Song.Project.IsChannelActive(pattern.ChannelType))
                    {
                        var channelIdx = Channel.ChannelTypeToIndex(pattern.ChannelType);
                        Song.Channels[channelIdx].PatternInstances[i + minSelectedPatternIdx] = pattern;
                    }
                }
            }

            if (customSettings != null)
            {
                for (int i = 0; i < patterns.GetLength(0); i++)
                {
                    if (customSettings[i].useCustomSettings)
                    {
                        Song.SetPatternCustomSettings(
                            i + minSelectedPatternIdx, 
                            customSettings[i].patternLength,
                            customSettings[i].noteLength,
                            customSettings[i].barLength);
                    }
                    else
                    {
                        Song.ClearPatternCustomSettings(i + minSelectedPatternIdx);
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
                    var copy = ModifierKeys.HasFlag(Keys.Control);
                    var noteIdx = (int)((e.X - trackNameSizeX + scrollX) / noteSizeX);

                    if (noteIdx >= 0 && noteIdx < Song.GetPatternStartNote(Song.Length))
                    {
                        var patternIdx = Song.FindPatternInstanceIndex((int)((e.X - trackNameSizeX + scrollX) / noteSizeX), out _);
                        var patternIdxDelta = patternIdx - selectionDragAnchorPatternIdx;
                        var tmpPatterns = GetSelectedPatterns(out var customSettings);

                        App.UndoRedoManager.BeginTransaction(TransactionScope.Song, Song.Id);

                        if (!copy)
                            DeleteSelection(false, customSettings != null && !copy);

                        for (int i = minSelectedChannelIdx; i <= maxSelectedChannelIdx; i++)
                        {
                            for (int j = minSelectedPatternIdx; j <= maxSelectedPatternIdx; j++)
                            {
                                var nj = j + patternIdxDelta;
                                if (nj >= 0 && nj < Song.Length)
                                    Song.Channels[i].PatternInstances[nj] = tmpPatterns[j - minSelectedPatternIdx, i - minSelectedChannelIdx];
                            }
                        }

                        if (customSettings != null)
                        {
                            for (int j = minSelectedPatternIdx; j <= maxSelectedPatternIdx; j++)
                            {
                                var settings = customSettings[j - minSelectedPatternIdx];

                                var nj = j + patternIdxDelta;
                                if (nj >= 0 && nj < Song.Length)
                                {
                                    if (settings.useCustomSettings)
                                    {
                                        Song.SetPatternCustomSettings(
                                            nj,
                                            customSettings[j - minSelectedPatternIdx].patternLength,
                                            customSettings[j - minSelectedPatternIdx].noteLength,
                                            customSettings[j - minSelectedPatternIdx].barLength);
                                    }
                                    else
                                    {
                                        Song.ClearPatternCustomSettings(nj);
                                    }
                                }
                            }
                        }

                        App.UndoRedoManager.EndTransaction();

                        minSelectedPatternIdx = Utils.Clamp(minSelectedPatternIdx + patternIdxDelta, 0, Song.Length - 1);
                        maxSelectedPatternIdx = Utils.Clamp(maxSelectedPatternIdx + patternIdxDelta, 0, Song.Length - 1);

                        ConditionalInvalidate();
                        PatternModified?.Invoke();
                    }
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
                selectionDragAnchorPatternIdx = -1;
                selectionDragAnchorPatternFraction = -1.0f;
                captureOperation = CaptureOperation.None;
            }
        }

        private void DeleteSelection(bool trans = true, bool clearCustomSettings = false)
        {
            if (trans)
            {
                App.UndoRedoManager.BeginTransaction(TransactionScope.Song, Song.Id);
            }

            for (int i = minSelectedChannelIdx; i <= maxSelectedChannelIdx; i++)
            {
                for (int j = minSelectedPatternIdx; j <= maxSelectedPatternIdx; j++)
                {
                    Song.Channels[i].PatternInstances[j] = null;
                }
            }

            if (clearCustomSettings)
            {
                for (int j = minSelectedPatternIdx; j <= maxSelectedPatternIdx; j++)
                    Song.ClearPatternCustomSettings(j);
            }

            if (trans)
            {
                ClearSelection();
                App.UndoRedoManager.EndTransaction();
                ConditionalInvalidate();
                PatternModified?.Invoke();
            }
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

            int noteIdx = (int)((mouseX - trackNameSizeX + scrollX) / noteSizeX);
            int patternIdx = Song.FindPatternInstanceIndex(noteIdx, out _);

            if (first)
            {
                fullColumnSelection = true;
                firstSelectedPatternIdx = patternIdx;
                minSelectedPatternIdx = patternIdx;
                maxSelectedPatternIdx = patternIdx;
                minSelectedChannelIdx = 0;
                maxSelectedChannelIdx = Song.Channels.Length - 1;
            }
            else
            {
                if (mouseX > captureStartX)
                {
                    minSelectedPatternIdx = firstSelectedPatternIdx;
                    maxSelectedPatternIdx = patternIdx;
                }
                else
                {
                    minSelectedPatternIdx = patternIdx;
                    maxSelectedPatternIdx = firstSelectedPatternIdx;
                }
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
                    tooltip = "{MouseLeft} Add Pattern - {L} {MouseLeft} Set Loop Point - {MouseWheel} Pan";
                }
                else
                {
                    if (IsPatternSelected(channelIdx, patternIdx))
                        tooltip = "{Drag} Move Pattern - {Ctrl} {Drag} Clone pattern {MouseLeft}{MouseLeft} Pattern properties - {MouseWheel} Pan\n{MouseRight} Delete Pattern - {L} {MouseLeft} Set Loop Point";
                    else
                        tooltip = "{MouseLeft} Select Pattern - {MouseLeft}{MouseLeft} Pattern properties - {MouseWheel} Pan\n{MouseRight} Delete Pattern - {L} {MouseLeft} Set Loop Point";
                }
            }
            else if (IsMouseInHeader(e))
            {
                tooltip = "{MouseLeft} Seek - {MouseLeft}{MouseLeft} Customize Pattern - {MouseRight} Select Colume - {L} {MouseLeft} Set Loop Point - {MouseWheel} Pan";
            }
            else if (IsMouseInTrackName(e))
            {
                if (GetTrackIconForPos(e) >= 0)
                {
                    tooltip = "{MouseLeft} Mute Channel - {MouseRight} Solo Channel";
                }
                else if (GetTrackGhostForPos(e) >= 0)
                {
                    tooltip = "{MouseLeft} Toggle channel display";
                    int idx = (e.Y - headerSizeY) / trackSizeY + 1;
                    if (idx >= 1 && idx <= 9)
                        tooltip += $" {{Ctrl}} {{{idx}}}";
                }
                else
                {
                    tooltip = "{MouseLeft} Make channel active";
                    int idx = (e.Y - headerSizeY) / trackSizeY + 1;
                    if (idx >= 1 && idx <= 9)
                        tooltip += $" {{{idx}}}";
                }
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

        private void EditPatternCustomSettings(Point pt, int patternIdx)
        {
            var dlg = new PropertyDialog(PointToScreen(pt), 240);
            var song = Song;
            var enabled = song.PatternHasCustomSettings(patternIdx);

            dlg.Properties.UserData = song;
            dlg.Properties.AddBoolean("Custom Pattern :", song.PatternHasCustomSettings(patternIdx), CommonTooltips.CustomPattern); // 0

            if (song.UsesFamiTrackerTempo)
            {
                dlg.Properties.AddIntegerRange("Notes Per Pattern :", song.GetPatternLength(patternIdx), 1, Pattern.MaxLength, CommonTooltips.NotesPerPattern); // 1
            }
            else
            {
                var noteLength = song.GetPatternNoteLength(patternIdx);

                dlg.Properties.AddIntegerRange("Frames Per Notes:", noteLength, Song.MinNoteLength, Song.MaxNoteLength, CommonTooltips.FramesPerNote); // 1
                dlg.Properties.AddIntegerRange("Notes Per Pattern :", song.GetPatternLength(patternIdx) / noteLength, 1, Pattern.MaxLength / noteLength, CommonTooltips.NotesPerPattern); // 2
                dlg.Properties.AddIntegerRange("Notes Per Bar :", song.GetPatternBarLength(patternIdx) / noteLength, 1, Pattern.MaxLength, CommonTooltips.NotesPerBar); // 3
                dlg.Properties.AddLabel("BPM :", Song.ComputeFamiStudioBPM(noteLength).ToString(), CommonTooltips.BPM); // 4
            }

            for (var i = 1; i < dlg.Properties.PropertyCount && i < 6; i++)
                dlg.Properties.SetPropertyEnabled(i, enabled);

            dlg.Properties.PropertyChanged += PatternCustomSettings_PropertyChanged;
            dlg.Properties.Build();

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                App.UndoRedoManager.BeginTransaction(TransactionScope.Song, song.Id);

                var custom = dlg.Properties.GetPropertyValue<bool>(0);

                if (song.UsesFamiTrackerTempo)
                {
                    var patternLength = dlg.Properties.GetPropertyValue<int>(1);

                    if (custom)
                        song.SetPatternCustomSettings(patternIdx, patternLength);
                    else
                        song.ClearPatternCustomSettings(patternIdx);
                }
                else
                {
                    var noteLength     = song.NoteLength;
                    var patternLength  = song.PatternLength;
                    var barLength      = song.BarLength;

                    if (custom)
                    { 
                        noteLength       = dlg.Properties.GetPropertyValue<int>(1);
                        patternLength    = dlg.Properties.GetPropertyValue<int>(2) * noteLength;
                        barLength        = dlg.Properties.GetPropertyValue<int>(3) * noteLength;
                    }

                    if (noteLength != song.GetPatternNoteLength(patternIdx))
                    {
                        var convertTempo = PlatformUtils.MessageBox($"You changed the note length for this pattern, do you want FamiStudio to attempt convert the tempo by resizing notes?", "Tempo Change", MessageBoxButtons.YesNo) == DialogResult.Yes;
                        if (convertTempo)
                            song.ResizePatternNotes(patternIdx, noteLength);
                    }

                    if (custom)
                        song.SetPatternCustomSettings(patternIdx, patternLength, noteLength, barLength);
                    else
                        song.ClearPatternCustomSettings(patternIdx);
                }

                App.UndoRedoManager.EndTransaction();
                ConditionalInvalidate();
                PatternModified?.Invoke();
            }
        }

        private void PatternCustomSettings_PropertyChanged(PropertyPage props, int idx, object value)
        {
            var song = props.UserData as Song;

            if (idx == 0)
            {
                for (var i = 1; i < props.PropertyCount && i < 6; i++)
                    props.SetPropertyEnabled(i, (bool)value);
            }
            else if (Song.UsesFamiStudioTempo && idx == 1)
            {
                var noteLength = (int)value;

                props.UpdateIntegerRange(2, 1, Pattern.MaxLength / noteLength);
                props.SetLabelText(4, Song.ComputeFamiStudioBPM(noteLength).ToString());
            }
        }

        private void EditPatternProperties(Point pt, Pattern pattern)
        {
            bool multiplePatternSelected = (maxSelectedChannelIdx != minSelectedChannelIdx) || (minSelectedPatternIdx != maxSelectedPatternIdx);

            var dlg = new PropertyDialog(PointToScreen(pt), 160);
            dlg.Properties.AddColoredString(pattern.Name, pattern.Color);
            dlg.Properties.SetPropertyEnabled(0, !multiplePatternSelected);
            dlg.Properties.AddColor(pattern.Color);
            dlg.Properties.Build();

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                App.UndoRedoManager.BeginTransaction(TransactionScope.Song, Song.Id);

                var newName = dlg.Properties.GetPropertyValue<string>(0);
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

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);

            bool left = (e.Button & MouseButtons.Left) != 0;
            bool inPatternZone = GetPatternForCoord(e.X, e.Y, out int channelIdx, out int patternIdx);

            if (left)
            {
                var pt = new Point(e.X, e.Y);

                if (IsMouseInHeader(e) && patternIdx >= 0)
                {
                    EditPatternCustomSettings(pt, patternIdx);
                }
                else if (inPatternZone)
                {
                    var pattern = Song.Channels[channelIdx].PatternInstances[patternIdx];

                    if (pattern != null)
                        EditPatternProperties(pt, pattern);
                }
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);

            int pixelX = e.X - trackNameSizeX;
            int absoluteX = pixelX + scrollX;
            if (e.Delta < 0 && zoomLevel > MinZoomLevel) { zoomLevel--; absoluteX /= 2; }
            if (e.Delta > 0 && zoomLevel < MaxZoomLevel) { zoomLevel++; absoluteX *= 2; }
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

        public void SongModified()
        {
            UpdateRenderCoords();
            InvalidatePatternCache();
            ClearSelection();
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
            buffer.Serialize(ref fullColumnSelection);
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
