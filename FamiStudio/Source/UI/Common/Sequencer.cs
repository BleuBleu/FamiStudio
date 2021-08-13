using System;
using System.Linq;
using System.Drawing;
using System.Collections.Generic;
using System.Windows.Forms;
using Color = System.Drawing.Color;
using System.Media;
using System.Diagnostics;

using RenderBitmap      = FamiStudio.GLBitmap;
using RenderBitmapAtlas = FamiStudio.GLBitmapAtlas;
using RenderBrush       = FamiStudio.GLBrush;
using RenderPath        = FamiStudio.GLGeometry;
using RenderControl     = FamiStudio.GLControl;
using RenderGraphics    = FamiStudio.GLGraphics;
using RenderTheme       = FamiStudio.GLTheme;

namespace FamiStudio
{
    public class Sequencer : RenderControl
    {
        const int DefaultTrackNameSizeX      = 94;
        const int DefaultHeaderSizeY         = 17;
        const int DefaultPatternHeaderSizeY  = 13;
        const int DefaultScrollMargin        = 128;
        const int DefaultBarTextPosY         = 2;
        const int DefaultTrackIconPosX       = 2;
        const int DefaultTrackIconPosY       = 3;
        const int DefaultTrackNamePosX       = 23;
        const int DefaultTrackNamePosY       = 4;
        const int DefaultGhostNoteOffsetX    = 16;
        const int DefaultGhostNoteOffsetY    = 15;
        const int DefaultPatternNamePosX     = 2;
        const int DefaultPatternNamePosY     = 1;
        const int DefaultHeaderIconPosX      = 3;
        const int DefaultHeaderIconPosY      = 3;
        const int DefaultHeaderIconSizeX     = 12;
        const int DefaultScrollBarThickness1 = 10;
        const int DefaultScrollBarThickness2 = 16;
        const int DefaultMinScrollBarLength  = 128;
        const float ContinuousFollowPercent  = 0.75f;

        const int MinZoomLevel = -2;
        const int MaxZoomLevel =  4;

        int trackNameSizeX;
        int headerSizeY;
        int trackSizeY;
        int patternHeaderSizeY;
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
        int scrollBarThickness;
        int minScrollBarLength;
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
            DragSelection,
            AltZoom,
            DragSeekBar,
            ScrollBar
        }

        static readonly bool[] captureNeedsThreshold = new[]
        {
            false, // None
            false, // Select
            true,  // DragSelection
            false, // AltZoom
            false, // DragSeekBar
            false, // ScrollBar
        };

        bool showSelection = true;
        bool showExpansionIcons = false;
        int captureMouseX = -1;
        int captureMouseY = -1;
        int captureScrollX = -1;
        int captureChannelIdx = -1;
        int capturePatternIdx = -1;
        int dragSeekPosition = -1;
        bool timeOnlySelection = false;
        int minSelectedChannelIdx = -1;
        int maxSelectedChannelIdx = -1;
        int minSelectedPatternIdx = -1;
        int maxSelectedPatternIdx = -1;
        int   selectionDragAnchorPatternIdx = -1;
        float selectionDragAnchorPatternXFraction = -1.0f;
        CaptureOperation captureOperation = CaptureOperation.None;
        bool panning = false; // TODO: Make this a capture operation.
        bool continuouslyFollowing = false;
        bool captureThresholdMet = false;

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

        PatternBitmapCache patternCache;

        RenderTheme theme;
        RenderBrush seekBarBrush;
        RenderBrush seekBarRecBrush;
        RenderBrush whiteKeyBrush;
        RenderBrush patternHeaderBrush;
        RenderBrush selectedPatternVisibleBrush;
        RenderBrush selectedPatternInvisibleBrush;
        RenderBrush selectionPatternBrush;
        RenderPath seekGeometry;

        RenderBitmapAtlas bmpAtlasExpansions;
        RenderBitmapAtlas bmpAtlasTracks;
        RenderBitmap bmpGhostNote;
        RenderBitmap bmpLoopPoint;
        RenderBitmap bmpCustomLength;
        RenderBitmap bmpInstanciate;
        RenderBitmap bmpDuplicate;
        RenderBitmap bmpDuplicateMove;

        public delegate void TrackBarDelegate(int trackIdx, int barIdx);
        public delegate void ChannelDelegate(int channelIdx);
        public delegate void EmptyDelegate();

        public event TrackBarDelegate PatternClicked;
        public event ChannelDelegate SelectedChannelChanged;
        public event EmptyDelegate ControlActivated;
        public event EmptyDelegate PatternModified;
        public event EmptyDelegate PatternsPasted;
        public event EmptyDelegate SelectionChanged;

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

        public bool ShowExpansionIcons
        {
            get { return showExpansionIcons; }
            set
            {
                if (showExpansionIcons != value)
                {
                    showExpansionIcons = value;
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
            scrollBarThickness = (int)((Settings.ScrollBars == 1 ? DefaultScrollBarThickness1 : (Settings.ScrollBars == 2 ? DefaultScrollBarThickness2 : 0)) * scaling);
            minScrollBarLength = (int)(DefaultMinScrollBarLength * scaling);
            noteSizeX          = ScaleForZoom(1.0f) * scaling;

            // Shave a couple pixels when the size is getting too small.
            if (TrackSizeIsSmall())
            {
                patternNamePosY    = (int)((DefaultPatternNamePosY - 1) * scaling);
                patternHeaderSizeY = (int)((DefaultPatternHeaderSizeY - 2) * scaling);
            }
            else
            {
                patternNamePosY    = (int)(DefaultPatternNamePosY * scaling);
                patternHeaderSizeY = (int)(DefaultPatternHeaderSizeY * scaling);
            }
        }

        private int GetChannelCount()
        {
            return App?.Project != null ? App.Project.Songs[0].Channels.Length : 5;
        }

        private bool TrackSizeIsSmall()
        {
            return ComputeDesiredTrackSizeY() < 24;
        }

        private int ComputeDesiredTrackSizeY()
        {
            return Math.Max(Settings.ForceCompactSequencer ? 0 : 280 / GetChannelCount(), 21);
        }

        public int ComputeDesiredSizeY()
        {
            var scrollBarSize = Settings.ScrollBars == 1 ? DefaultScrollBarThickness1 : (Settings.ScrollBars == 2 ? DefaultScrollBarThickness2 : 0);

            // Does not include scaling.
            return ComputeDesiredTrackSizeY() * GetChannelCount() + DefaultHeaderSizeY + scrollBarSize + 1;
        }

        public void LayoutChanged()
        {
            UpdateRenderCoords();
            ClampScroll();
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
            SelectionChanged?.Invoke();
        }

        private bool IsPatternSelected(int channelIdx, int patternIdx)
        {
            return channelIdx >= minSelectedChannelIdx && channelIdx <= maxSelectedChannelIdx &&
                   patternIdx >= minSelectedPatternIdx && patternIdx <= maxSelectedPatternIdx;
        }

        public bool GetPatternTimeSelectionRange(out int minPatternIdx, out int maxPatternIdx)
        {
            if (IsSelectionValid())
            {
                minPatternIdx = minSelectedPatternIdx;
                maxPatternIdx = maxSelectedPatternIdx;
                return true;
            }
            else
            {
                minPatternIdx = -1;
                maxPatternIdx = -1;
                return false;
            }
        }

        protected override void OnRenderInitialized(RenderGraphics g)
        {
            theme = RenderTheme.CreateResourcesForGraphics(g);
            patternCache = new PatternBitmapCache(g);

            var expansionBitmapNames = new string[ExpansionType.Count];
            for (int i = 0; i < ExpansionType.Count; i++)
                expansionBitmapNames[i] = ExpansionType.Icons[i] + "Light";

            bmpAtlasExpansions = g.CreateBitmapAtlasFromResources(expansionBitmapNames);
            bmpAtlasTracks     = g.CreateBitmapAtlasFromResources(ChannelType.Icons);

            bmpGhostNote = g.CreateBitmapFromResource("GhostSmall");
            bmpLoopPoint = g.CreateBitmapFromResource("LoopSmallFill");
            bmpCustomLength = g.CreateBitmapFromResource("CustomPattern");
            bmpInstanciate = g.CreateBitmapFromResource("Instance");
            bmpDuplicate = g.CreateBitmapFromResource("Duplicate");
            bmpDuplicateMove = g.CreateBitmapFromResource("DuplicateMove");

            seekBarBrush = g.CreateSolidBrush(ThemeBase.SeekBarColor);
            seekBarRecBrush = g.CreateSolidBrush(ThemeBase.DarkRedFillColor);
            whiteKeyBrush = g.CreateHorizontalGradientBrush(0, trackNameSizeX, ThemeBase.LightGreyFillColor1, ThemeBase.LightGreyFillColor2);
            patternHeaderBrush = g.CreateVerticalGradientBrush(0, patternHeaderSizeY, ThemeBase.LightGreyFillColor1, ThemeBase.LightGreyFillColor2);
            selectedPatternVisibleBrush   = g.CreateSolidBrush(Color.FromArgb(64, ThemeBase.LightGreyFillColor1));
            selectedPatternInvisibleBrush = g.CreateSolidBrush(Color.FromArgb(32, ThemeBase.LightGreyFillColor1));
            selectionPatternBrush = g.CreateSolidBrush(ThemeBase.LightGreyFillColor1);

            seekGeometry = g.CreateGeometry(new float[,]
            {
                { -headerSizeY / 2, 1 },
                { 0, headerSizeY - 2 },
                { headerSizeY / 2, 1 }
            }, true);
        }

        protected override void OnRenderTerminated()
        {
            theme.Terminate();

            Utils.DisposeAndNullify(ref bmpAtlasExpansions);
            Utils.DisposeAndNullify(ref bmpAtlasTracks);
            Utils.DisposeAndNullify(ref bmpGhostNote);
            Utils.DisposeAndNullify(ref bmpLoopPoint);
            Utils.DisposeAndNullify(ref bmpCustomLength);
            Utils.DisposeAndNullify(ref bmpInstanciate);
            Utils.DisposeAndNullify(ref bmpDuplicate);
            Utils.DisposeAndNullify(ref seekBarBrush);
            Utils.DisposeAndNullify(ref seekBarRecBrush);
            Utils.DisposeAndNullify(ref whiteKeyBrush);
            Utils.DisposeAndNullify(ref patternHeaderBrush);
            Utils.DisposeAndNullify(ref selectedPatternVisibleBrush);
            Utils.DisposeAndNullify(ref selectedPatternInvisibleBrush);
            Utils.DisposeAndNullify(ref selectionPatternBrush);
            Utils.DisposeAndNullify(ref seekGeometry);

            InvalidatePatternCache();
        }

        protected override void OnResize(EventArgs e)
        {
            UpdateRenderCoords();
            base.OnResize(e);
        }

        protected bool IsSelectionValid()
        {
            return minSelectedPatternIdx >= 0 &&
                    maxSelectedPatternIdx >= 0 &&
                    minSelectedChannelIdx >= 0 &&
                    maxSelectedChannelIdx >= 0;
        }

        private bool IsValidTimeOnlySelection()
        {
            return IsSelectionValid() && timeOnlySelection;
        }

        private RenderBrush GetSeekBarBrush()
        {
            return App.IsRecording ? seekBarRecBrush : seekBarBrush;
        }

        public int GetSeekFrameToDraw()
        {
            return captureOperation == CaptureOperation.DragSeekBar ? dragSeekPosition : App.CurrentFrame;
        }

        protected void RenderChannelNames(RenderGraphics g)
        {
            var c = g.CreateCommandList();

            // Track name background
            c.DrawRectangle(0, 0, trackNameSizeX, Height, theme.DarkGreyFillBrush1);

            // Horizontal lines
            c.DrawLine(0, 0, trackNameSizeX, 0, theme.BlackBrush);
            c.DrawLine(0, Height - 1, trackNameSizeX, Height - 1, theme.BlackBrush);

            for (int i = 0, y = headerSizeY; i < Song.Channels.Length; i++, y += trackSizeY)
                c.DrawLine(0, y, Width, y, theme.BlackBrush);

            // Vertical line seperating the track labels.
            c.DrawLine(trackNameSizeX - 1, 0, trackNameSizeX - 1, Height, theme.BlackBrush);

            c.PushTranslation(0, headerSizeY);

            // Icons
            for (int i = 0, y = 0; i < Song.Channels.Length; i++, y += trackSizeY)
            {
                RenderBitmapAtlas atlas;
                int bitmapIndex;

                if (showExpansionIcons && Song.Project.UsesAnyExpansionAudio)
                {
                    atlas = bmpAtlasExpansions;
                    bitmapIndex = Song.Channels[i].Expansion;
                }
                else
                {
                    atlas = bmpAtlasTracks;
                    bitmapIndex = Song.Channels[i].Type;
                }

                c.DrawBitmapAtlas(atlas, bitmapIndex, trackIconPosX, y + trackIconPosY, (App.ChannelMask & (1 << i)) != 0 ? 1.0f : 0.2f);
            }

            // Track names
            for (int i = 0, y = 0; i < Song.Channels.Length; i++, y += trackSizeY)
            {
                var font = i == selectedChannel ? ThemeBase.FontMediumBold : ThemeBase.FontMedium;
                c.DrawText(Song.Channels[i].Name, font, trackNamePosX, y + trackNamePosY, theme.LightGreyFillBrush2);
            }

            // Ghost note icons
            for (int i = 0, y = 0; i < Song.Channels.Length; i++, y += trackSizeY)
            {
                c.DrawBitmap(bmpGhostNote, trackNameSizeX - ghostNoteOffsetX, y + trackSizeY - ghostNoteOffsetY - 1, (App.GhostChannelMask & (1 << i)) != 0 ? 1.0f : 0.2f);
            }

            c.PopTransform();
            g.DrawCommandList(c, new Rectangle(0, 0, trackNameSizeX, Height));
        }

        protected void RenderPatternArea(RenderGraphics g)
        {
            var cb = g.CreateCommandList(); // Background stuff
            var cf = g.CreateCommandList(); // Foreground stuff

            var seekX = noteSizeX * GetSeekFrameToDraw() - scrollX;
            var minVisibleNoteIdx = Math.Max((int)Math.Floor(scrollX / noteSizeX), 0);
            var maxVisibleNoteIdx = Math.Min((int)Math.Ceiling((scrollX + Width) / noteSizeX), Song.GetPatternStartAbsoluteNoteIndex(Song.Length));
            var minVisiblePattern = Utils.Clamp(Song.PatternIndexFromAbsoluteNoteIndex(minVisibleNoteIdx) + 0, 0, Song.Length);
            var maxVisiblePattern = Utils.Clamp(Song.PatternIndexFromAbsoluteNoteIndex(maxVisibleNoteIdx) + 1, 0, Song.Length);
            var actualSizeY = Height - scrollBarThickness;

            // Grey background rectangles ever other pattern + vertical lines 
            cb.PushTranslation(trackNameSizeX, 0);

            for (int i = minVisiblePattern; i < maxVisiblePattern; i++)
            {
                if ((i & 1) == 0)
                {
                    var px = (int)(Song.GetPatternStartAbsoluteNoteIndex(i) * noteSizeX) - scrollX;
                    var sx = (int)(Song.GetPatternLength(i) * noteSizeX);
                    cb.FillRectangle(px, 0, px + sx, Height, theme.DarkGreyFillBrush1);
                }
            }

            if (IsSelectionValid())
            {
                cb.FillRectangle(
                    (int)(Song.GetPatternStartAbsoluteNoteIndex(Math.Min(minSelectedPatternIdx + 0, Song.Length)) * noteSizeX) - scrollX, trackSizeY * (minSelectedChannelIdx + 0) + headerSizeY,
                    (int)(Song.GetPatternStartAbsoluteNoteIndex(Math.Min(maxSelectedPatternIdx + 1, Song.Length)) * noteSizeX) - scrollX, trackSizeY * (maxSelectedChannelIdx + 1) + headerSizeY,
                    showSelection ? selectedPatternVisibleBrush : selectedPatternInvisibleBrush);

                if (IsValidTimeOnlySelection())
                {
                    cb.FillRectangle(
                        (int)(Song.GetPatternStartAbsoluteNoteIndex(Math.Min(minSelectedPatternIdx + 0, Song.Length)) * noteSizeX) - scrollX, 0,
                        (int)(Song.GetPatternStartAbsoluteNoteIndex(Math.Min(maxSelectedPatternIdx + 1, Song.Length)) * noteSizeX) - scrollX, headerSizeY,
                        showSelection ? selectedPatternVisibleBrush : selectedPatternInvisibleBrush);
                }
            }

            // Header
            cb.DrawLine(0, 0, Width, 0, theme.BlackBrush);
            cb.DrawLine(0, Height - 1, Width, Height - 1, theme.BlackBrush);

            // Vertical lines
            for (int i = minVisiblePattern; i <= maxVisiblePattern; i++)
            {
                if (i != 0)
                {
                    var px = (int)(Song.GetPatternStartAbsoluteNoteIndex(i) * noteSizeX) - scrollX;
                    cb.DrawLine(px, 0, px, actualSizeY, theme.BlackBrush);
                }
            }

            // Horizontal lines
            for (int i = 0, y = headerSizeY; i < Song.Channels.Length; i++, y += trackSizeY)
                cb.DrawLine(0, y, Width, y, theme.BlackBrush);

            // Patterns
            int patternCacheSizeY = trackSizeY - patternHeaderSizeY - 1;
            patternCache.Update(patternCacheSizeY);

            for (int i = minVisiblePattern; i < maxVisiblePattern; i++)
            {
                var patternLen = Song.GetPatternLength(i);
                var noteLen = Song.UsesFamiTrackerTempo ? 1 : Song.GetPatternNoteLength(i);
                var px = (int)(Song.GetPatternStartAbsoluteNoteIndex(i) * noteSizeX) - scrollX;
                var sx = (int)(patternLen * noteSizeX);

                cf.PushTranslation(px, 0);

                var text = (i + 1).ToString();
                if (Song.PatternHasCustomSettings(i))
                    text += "*";
                cf.DrawText(text, ThemeBase.FontMediumCenter, 0, barTextPosY, theme.LightGreyFillBrush1, sx, true);

                if (i == Song.LoopPoint)
                {
                    cf.FillRectangle(headerIconPosX, headerIconPosY, headerIconPosX + bmpLoopPoint.Size.Width, headerIconPosY + bmpLoopPoint.Size.Height, theme.DarkGreyLineBrush2);
                    cf.DrawBitmap(bmpLoopPoint, headerIconPosX, headerIconPosY);
                }

                for (int t = 0, py = headerSizeY; t < Song.Channels.Length; t++, py += trackSizeY)
                {
                    var pattern = Song.Channels[t].PatternInstances[i];

                    if (pattern != null)
                    {
                        var bmp = patternCache.GetOrAddPattern(pattern, patternLen, noteLen, out var u0, out var v0, out var u1, out var v1);

                        cf.PushTranslation(0, py);

                        cf.FillRectangle(1, 1, sx, patternHeaderSizeY, g.GetVerticalGradientBrush(pattern.Color, patternHeaderSizeY, 0.8f));
                        cf.DrawLine(0, patternHeaderSizeY, sx, patternHeaderSizeY, theme.BlackBrush);
                        cf.DrawBitmap(bmp, 1.0f, 1.0f + patternHeaderSizeY, sx - 1, patternCacheSizeY, 1.0f, u0, v0, u1, v1); // MATTT : We use the bitmap size here.
                        cf.DrawText(pattern.Name, ThemeBase.FontSmall, patternNamePosX, patternNamePosY, theme.BlackBrush, sx - patternNamePosX, true);

                        if (IsPatternSelected(t, i))
                            cf.DrawRectangle(0, 0, sx, trackSizeY, selectionPatternBrush, 2);

                        cf.PopTransform();
                    }
                }

                cf.PopTransform();
            }

            /*
            // Dragging selection
            // MATTT : This doesnt work with the new rendering code.
            if (captureOperation == CaptureOperation.DragSelection)
            {
                var pt = this.PointToClient(Cursor.Position);
                var noteIdx = (int)((pt.X - trackNameSizeX + scrollX) / noteSizeX);

                if (noteIdx >= 0 && noteIdx < Song.GetPatternStartAbsoluteNoteIndex(Song.Length))
                {
                    var patternIdx = Song.PatternIndexFromAbsoluteNoteIndex(noteIdx);
                    var patternIdxDelta = patternIdx - selectionDragAnchorPatternIdx;

                    pt.Y -= headerSizeY;

                    var dragChannelIdxStart = (captureMouseY - headerSizeY) / trackSizeY;
                    var dragChannelIdxCurrent = pt.Y / trackSizeY;
                    var channelIdxDelta = dragChannelIdxCurrent - dragChannelIdxStart;

                    for (int j = minSelectedChannelIdx + channelIdxDelta; j <= maxSelectedChannelIdx + channelIdxDelta; j++)
                    {
                        if (j < 0 || j > Song.Channels.Length)
                            continue;

                        var y = j * trackSizeY;

                        // Center.
                        var patternSizeX = Song.GetPatternLength(patternIdx) * noteSizeX;
                        var anchorOffsetLeftX = patternSizeX * selectionDragAnchorPatternXFraction;
                        var anchorOffsetRightX = patternSizeX * (1.0f - selectionDragAnchorPatternXFraction);

                        var instance  = ModifierKeys.HasFlag(Keys.Control);
                        var duplicate = instance && ModifierKeys.HasFlag(Keys.Shift);

                        var bmpCopy = (RenderBitmap)null;
                        if (channelIdxDelta != 0)
                            bmpCopy = (duplicate || instance) ? bmpDuplicate : bmpDuplicateMove;
                        else
                            bmpCopy = duplicate ? bmpDuplicate : (instance ? bmpInstanciate : null);

                        xf.PushTranslation(0, y);
                        xf.PushTranslation(pt.X - anchorOffsetLeftX, 0);

                        fillBg.AddRectangle(0, 0, patternSizeX, trackSizeY, selectedPatternVisibleBrush);
                        lines.AddRectangle(0, 0, patternSizeX, trackSizeY, theme.BlackBrush);

                        if (bmpCopy != null)
                            bitmaps.AddBitmap(bmpCopy, patternSizeX / 2 - bmpInstanciate.Size.Width / 2, trackSizeY / 2 - bmpInstanciate.Size.Height / 2);

                        // Left side
                        for (int p = patternIdx - 1; p >= minSelectedPatternIdx + patternIdxDelta && p >= 0; p--)
                        {
                            patternSizeX = Song.GetPatternLength(p) * noteSizeX;
                            anchorOffsetLeftX += patternSizeX;

                            fillBg.AddRectangle(0, 0, patternSizeX, trackSizeY, selectedPatternVisibleBrush);
                            lines.AddRectangle(0, 0, patternSizeX, trackSizeY, theme.BlackBrush);

                            if (bmpCopy != null)
                                bitmaps.AddBitmap(bmpCopy, patternSizeX / 2 - bmpInstanciate.Size.Width / 2, trackSizeY / 2 - bmpInstanciate.Size.Height / 2);
                        }
                        
                        xf.PopTransform();
                        xf.PushTranslation(pt.X + anchorOffsetRightX, 0);

                        // Right side
                        for (int p = patternIdx + 1; p <= maxSelectedPatternIdx + patternIdxDelta && p < Song.Length; p++)
                        {
                            patternSizeX = Song.GetPatternLength(p) * noteSizeX;
                            
                            fillBg.AddRectangle(0, 0, patternSizeX, trackSizeY, selectedPatternVisibleBrush);
                            lines.AddRectangle(0, 0, patternSizeX, trackSizeY, theme.BlackBrush);

                            if (bmpCopy != null)
                                bitmaps.AddBitmap(bmpCopy, patternSizeX / 2 - bmpInstanciate.Size.Width / 2, trackSizeY / 2 - bmpInstanciate.Size.Height / 2);

                            anchorOffsetRightX += patternSizeX;
                        }

                        xf.PopTransform();
                    }
                }
            }
            */

            // Piano roll view rect
            if (Settings.ShowPianoRollViewRange)
            {
                App.GetPianoRollViewRange(out var pianoRollMinNoteIdx, out var pianoRollMaxNoteIdx, out var pianoRollChannelIndex);

                cf.PushTranslation(pianoRollMinNoteIdx * noteSizeX - scrollX + trackNameSizeX, pianoRollChannelIndex * trackSizeY + headerSizeY);
                cf.DrawRectangle(1, patternHeaderSizeY + 1, (pianoRollMaxNoteIdx - pianoRollMinNoteIdx) * noteSizeX - 1, trackSizeY - 1, theme.LightGreyFillBrush2);
                cf.PopTransform();
            }

            // Scroll bar (optional)
            if (GetScrollBarParams(out var scrollBarPosX, out var scrollBarSizeX))
            {
                cb.FillAndDrawRectangle(-1, actualSizeY, Width, Height, theme.DarkGreyFillBrush1, theme.BlackBrush);
                cb.FillAndDrawRectangle(scrollBarPosX - 1, actualSizeY, scrollBarPosX + scrollBarSizeX, Height, theme.MediumGreyFillBrush1, theme.BlackBrush);
            }

            // Seek bar
            cb.PushTranslation(seekX, 0);
            cf.FillAndDrawGeometry(seekGeometry, GetSeekBarBrush(), theme.BlackBrush);
            cb.DrawLine(0, headerSizeY, 0, actualSizeY, GetSeekBarBrush(), 3);
            cb.PopTransform();

            cb.PopTransform();

            var rect = new Rectangle(trackNameSizeX, 0, Width, Height);

            g.DrawCommandList(cb, rect);
            g.DrawCommandList(cf, rect);
        }

        //int clip = 100;
        //int inc = 1;

        protected override void OnRender(RenderGraphics g)
        {
            // Happens when piano roll is maximized.
            if (Height <= 1)
            {
                g.Clear(ThemeBase.BlackColor);
                return;
            }

            g.Clear(ThemeBase.DarkGreyLineColor2);
            /*
            var list = g.CreateCommandList();

            list.DrawLine(20, 0, 20, Height, theme.BlackBrush);
            list.DrawText("Hello this is a long text", ThemeBase.FontMediumCenter, 20, 50, theme.LightRedFillBrush, clip, true);
            list.DrawLine(20 + clip, 0, clip + 20, Height, theme.BlackBrush);

            clip += inc;
            if (clip > 150) inc = -1;
            if (clip < 50) inc = 1;

            g.DrawCommandList(list);*/

            
            RenderChannelNames(g);
            RenderPatternArea(g);
            
        }

        private bool GetScrollBarParams(out int posX, out int sizeX)
        {
            if (scrollBarThickness > 0)
            {
                GetMinMaxScroll(out _, out var maxScrollX);

                int scrollAreaSizeX = Width - trackNameSizeX;
                sizeX = Math.Max(minScrollBarLength, (int)Math.Round(scrollAreaSizeX * Math.Min(1.0f, scrollAreaSizeX / (float)(maxScrollX + scrollAreaSizeX))));
                posX = (int)Math.Round((scrollAreaSizeX - sizeX) * (scrollX / (float)maxScrollX));
                return true;
            }
            else
            {
                posX  = 0;
                sizeX = 0;

                return false;
            }
        }

        public void NotifyPatternChange(Pattern pattern)
        {
            if (pattern != null)
            {
                patternCache.Remove(pattern);
            }
        }

#if DEBUG
        public void ValidateIntegrity()
        {
            if (patternCache != null)
            {
                patternCache.ValidateIntegrity();
            }
        }
#endif

        private void GetMinMaxScroll(out int minScrollX, out int maxScrollX)
        {
            minScrollX = 0;
            maxScrollX = Math.Max((int)(Song.GetPatternStartAbsoluteNoteIndex(Song.Length) * noteSizeX) - scrollMargin, 0);
        }

        private void ClampScroll()
        {
            int minScrollX, maxScrollX;
            GetMinMaxScroll(out minScrollX, out maxScrollX);

            if (scrollX < minScrollX) scrollX = minScrollX;
            if (scrollX > maxScrollX) scrollX = maxScrollX;
        }

        private void DoScroll(int deltaX, int deltaY)
        {
            scrollX -= deltaX;
            ClampScroll();
            ConditionalInvalidate();
        }

        private bool GetPatternForCoord(int x, int y, out int channelIdx, out int patternIdx, out bool inPatternHeader)
        {
            var noteIdx = (int)((x - trackNameSizeX + scrollX) / noteSizeX);

            if (noteIdx < 0 || noteIdx >= Song.GetPatternStartAbsoluteNoteIndex(Song.Length))
            {
                channelIdx = -1;
                patternIdx = -1;
                inPatternHeader = false;
                return false;
            }

            patternIdx = Song.PatternIndexFromAbsoluteNoteIndex(noteIdx);
            channelIdx = (y - headerSizeY) / trackSizeY;
            inPatternHeader = (y - headerSizeY) % trackSizeY < patternHeaderSizeY;

            return (x > trackNameSizeX && y > headerSizeY && channelIdx >= 0 && channelIdx < Song.Channels.Length);
        }

        private void GetClampedPatternForCoord(int x, int y, out int channelIdx, out int patternIdx)
        {
            patternIdx = Song.PatternIndexFromAbsoluteNoteIndex(Utils.Clamp((int)((x - trackNameSizeX + scrollX) / noteSizeX), 0, Song.GetPatternStartAbsoluteNoteIndex(Song.Length) - 1));
            channelIdx = Utils.Clamp((y - headerSizeY) / trackSizeY, 0, Song.Channels.Length - 1);
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

        private void CaptureMouse(MouseEventArgs e)
        {
            mouseLastX = e.X;
            mouseLastY = e.Y;
            captureMouseX = e.X;
            captureMouseY = e.Y;
            captureScrollX = scrollX;
            Capture = true;
        }

        private void StartCaptureOperation(MouseEventArgs e, CaptureOperation op)
        {
            Debug.Assert(captureOperation == CaptureOperation.None);
            CaptureMouse(e);
            captureOperation = op;
            captureThresholdMet = !captureNeedsThreshold[(int)op];
            GetClampedPatternForCoord(e.X, e.Y, out captureChannelIdx, out capturePatternIdx);
        }

        private bool HandleMouseDownPan(MouseEventArgs e)
        {
            bool middle = e.Button.HasFlag(MouseButtons.Middle) || (e.Button.HasFlag(MouseButtons.Left) && ModifierKeys.HasFlag(Keys.Alt));

            if (middle)
            {
                panning = true;
                CaptureMouse(e);
                return true;
            }

            return false;
        }

        private bool HandleMouseDownScrollbar(MouseEventArgs e)
        {
            if (e.X > trackNameSizeX && e.Y > Height - scrollBarThickness && GetScrollBarParams(out var scrollBarPosX, out var scrollBarSizeX))
            {
                var x = e.X - trackNameSizeX;
                if (x < scrollBarPosX)
                {
                    scrollX -= (Width - trackNameSizeX);
                    ClampScroll();
                    ConditionalInvalidate();
                }
                else if (x > (scrollBarPosX + scrollBarSizeX))
                {
                    scrollX += (Width - trackNameSizeX);
                    ClampScroll();
                    ConditionalInvalidate();
                }
                else if (x >= scrollBarPosX && x <= (scrollBarPosX + scrollBarSizeX))
                {
                    StartCaptureOperation(e, CaptureOperation.ScrollBar);
                }
                return true;
            }

            return false;
        }

        private bool HandleMouseDownChannelName(MouseEventArgs e)
        {
            bool left  = e.Button.HasFlag(MouseButtons.Left);
            bool right = e.Button.HasFlag(MouseButtons.Right);

            if ((left || right) && e.X < trackNameSizeX)
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
                            App.ChannelMask = -1;
                        else
                            App.ChannelMask = bit;
                    }

                    ConditionalInvalidate();
                    return true;
                }
                else if (ghostIcon >= 0)
                {
                    App.GhostChannelMask ^= (1 << ghostIcon);
                    ConditionalInvalidate();
                    return true;
                }
            }

            return false;
        }

        private bool HandleMouseDownSetLoopPoint(MouseEventArgs e)
        {
            bool setLoop = FamiStudioForm.IsKeyDown(Keys.L);

            if (setLoop && e.X > trackNameSizeX && e.Button.HasFlag(MouseButtons.Left))
            {
                GetPatternForCoord(e.X, e.Y, out int channelIdx, out int patternIdx, out _);

                if (patternIdx >= 0)
                {
                    App.UndoRedoManager.BeginTransaction(TransactionScope.Song, Song.Id);
                    Song.SetLoopPoint(Song.LoopPoint == patternIdx ? -1 : patternIdx);
                    App.UndoRedoManager.EndTransaction();
                    ConditionalInvalidate();
                }
                return true;
            }

            return false;
        }

        private bool HandleMouseDownSeekBar(MouseEventArgs e)
        {
            if (IsMouseInHeader(e) && e.Button.HasFlag(MouseButtons.Left))
            {
                StartCaptureOperation(e, CaptureOperation.DragSeekBar);
                UpdateSeekDrag(e.X, false);
                return true;
            }

            return false;
        }

        private bool HandleMouseDownHeaderSelection(MouseEventArgs e)
        {
            if (IsMouseInHeader(e) && e.Button.HasFlag(MouseButtons.Right))
            {
                StartCaptureOperation(e, CaptureOperation.Select);
                UpdateSelection(e, true);
                return true;
            }

            return false;
        }

        private bool HandleMouseDownChannelChange(MouseEventArgs e)
        {
            if (e.Y > headerSizeY && e.Y < Height - scrollBarThickness && e.Button.HasFlag(MouseButtons.Left))
            {
                var newChannel = Utils.Clamp((e.Y - headerSizeY) / trackSizeY, 0, Song.Channels.Length - 1);
                if (newChannel != selectedChannel)
                {
                    selectedChannel = newChannel;
                    SelectedChannelChanged?.Invoke(selectedChannel);
                    ConditionalInvalidate();
                }
            }

            // Does not prevent from processing other events.
            return false;
        }

        private bool HandleMouseDownAltZoom(MouseEventArgs e)
        {
            if (e.Button.HasFlag(MouseButtons.Right) && ModifierKeys.HasFlag(Keys.Alt) && GetPatternForCoord(e.X, e.Y, out _, out _, out _))
            {
                StartCaptureOperation(e, CaptureOperation.AltZoom);
                return true;
            }

            return false;
        }

        private bool HandleMouseDownPatterns(MouseEventArgs e)
        {
            bool left  = e.Button.HasFlag(MouseButtons.Left);
            bool right = e.Button.HasFlag(MouseButtons.Right);

            bool inPatternZone = GetPatternForCoord(e.X, e.Y, out var channelIdx, out var patternIdx, out var inPatternHeader);

            if (inPatternZone)
            {
                var channel = Song.Channels[channelIdx];
                var pattern = channel.PatternInstances[patternIdx];

                if (left)
                {
                    var ctrl  = ModifierKeys.HasFlag(Keys.Control);
                    var shift = ModifierKeys.HasFlag(Keys.Shift);

                    if (pattern == null && !shift)
                    {
                        App.UndoRedoManager.BeginTransaction(TransactionScope.Song, Song.Id);
                        channel.PatternInstances[patternIdx] = channel.CreatePattern();
                        channel.InvalidateCumulativePatternCache();
                        PatternClicked?.Invoke(channelIdx, patternIdx);
                        App.UndoRedoManager.EndTransaction();
                        ClearSelection();
                        ConditionalInvalidate();
                    }
                    else
                    {
                        if (pattern != null)
                        {
                            PatternClicked?.Invoke(channelIdx, patternIdx);
                        }

                        if (!IsPatternSelected(channelIdx, patternIdx))
                        {
                            minSelectedChannelIdx = channelIdx;
                            maxSelectedChannelIdx = channelIdx;
                            minSelectedPatternIdx = patternIdx;
                            maxSelectedPatternIdx = patternIdx;
                            timeOnlySelection = false;
                            SelectionChanged?.Invoke();
                        }

                        selectionDragAnchorPatternIdx = patternIdx;
                        selectionDragAnchorPatternXFraction = (e.X - trackNameSizeX + scrollX - (int)(Song.GetPatternStartAbsoluteNoteIndex(patternIdx) * noteSizeX)) / (Song.GetPatternLength(patternIdx) * noteSizeX);
                        StartCaptureOperation(e, CaptureOperation.DragSelection);

                        ConditionalInvalidate();
                    }

                    return true;
                }
                else if (right && inPatternHeader && pattern != null)
                {
                    App.UndoRedoManager.BeginTransaction(TransactionScope.Song, Song.Id);
                    patternCache.Remove(pattern);
                    channel.PatternInstances[patternIdx] = null;
                    channel.InvalidateCumulativePatternCache();
                    App.UndoRedoManager.EndTransaction();
                    ClearSelection();
                    ConditionalInvalidate();
                    PatternModified?.Invoke();

                    return true;
                }
                else if (right)
                {
                    StartCaptureOperation(e, CaptureOperation.Select);
                    UpdateSelection(e, true);
                    return true;
                }
            }

            return false;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            ControlActivated?.Invoke();

            bool left    = e.Button.HasFlag(MouseButtons.Left);
            bool right   = e.Button.HasFlag(MouseButtons.Right);

            if (captureOperation != CaptureOperation.None && (left || right))
                return;

            UpdateCursor();

            if (HandleMouseDownPan(e)) goto Handled;
            if (HandleMouseDownScrollbar(e)) goto Handled;
            if (HandleMouseDownChannelName(e)) goto Handled;
            if (HandleMouseDownSetLoopPoint(e)) goto Handled;
            if (HandleMouseDownSeekBar(e)) goto Handled;
            if (HandleMouseDownHeaderSelection(e)) goto Handled;
            if (HandleMouseDownChannelChange(e)) goto Handled;
            if (HandleMouseDownAltZoom(e)) goto Handled;
            if (HandleMouseDownPatterns(e)) goto Handled;
            return;

        Handled: // Yes, i use a goto, sue me.
            ConditionalInvalidate();
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

            if (IsValidTimeOnlySelection())
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

        private void PasteInternal(bool insert, bool extend, int repeat)
        {
            var missingInstruments = ClipboardUtils.ContainsMissingInstrumentsOrSamples(App.Project, false, out var missingArpeggios, out var missingSamples);

            bool createMissingInstrument = false;
            if (missingInstruments)
                createMissingInstrument = PlatformUtils.MessageBox($"You are pasting notes referring to unknown instruments. Do you want to create the missing instrument?", "Paste", MessageBoxButtons.YesNo) == DialogResult.Yes;

            bool createMissingArpeggios = false;
            if (missingArpeggios)
                createMissingArpeggios = PlatformUtils.MessageBox($"You are pasting notes referring to unknown arpeggios. Do you want to create the missing arpeggios?", "Paste", MessageBoxButtons.YesNo) == DialogResult.Yes;

            bool createMissingSamples = false;
            if (missingSamples)
                createMissingSamples = PlatformUtils.MessageBox($"You are pasting notes referring to unmapped DPCM samples. Do you want to create the missing samples?", "Paste", MessageBoxButtons.YesNo) == DialogResult.Yes;

            App.UndoRedoManager.BeginTransaction(createMissingInstrument || createMissingArpeggios || createMissingSamples ? TransactionScope.Project : TransactionScope.Song, Song.Id);

            var song = Song;
            var patterns = ClipboardUtils.LoadPatterns(App.Project, song, createMissingInstrument, createMissingArpeggios, createMissingSamples, out var customSettings);

            if (patterns == null)
            {
                App.UndoRedoManager.AbortTransaction();
                return;
            }

            var numColumnsToPaste = patterns.GetLength(0) * repeat;

            if (numColumnsToPaste == 0)
            {
                App.UndoRedoManager.AbortTransaction();
                return;
            }

            if (insert)
            {
                if (extend)
                {
                    song.SetLength(Math.Min(numColumnsToPaste + song.Length, Song.MaxLength));
                }

                // Move everything to the right.
                for (int i = song.Length - 1; i >= minSelectedPatternIdx + numColumnsToPaste; i--)
                {
                    var srcIndex = i - numColumnsToPaste;

                    for (int j = 0; j < song.Channels.Length; j++)
                    {
                        song.Channels[j].PatternInstances[i] = song.Channels[j].PatternInstances[srcIndex];
                    }

                    if (song.PatternHasCustomSettings(srcIndex))
                    {
                        var srcCustomSettings = song.GetPatternCustomSettings(srcIndex);
                        song.SetPatternCustomSettings(i, srcCustomSettings.patternLength, srcCustomSettings.beatLength, srcCustomSettings.groove, srcCustomSettings.groovePaddingMode);
                    }
                }

                // Clear everything where we are pasting.
                for (int i = minSelectedPatternIdx; i < maxSelectedPatternIdx + numColumnsToPaste; i++)
                {
                    song.ClearPatternCustomSettings(i);

                    for (int j = 0; j < song.Channels.Length; j++)
                    {
                        song.Channels[j].PatternInstances[i] = null;
                    }
                }
            }
            
            // Then do the actual paste.
            var startPatternIndex = minSelectedPatternIdx;

            for (int r = 0; r < repeat; r++)
            {
                for (int i = 0; i < patterns.GetLength(0); i++)
                {
                    for (int j = 0; j < patterns.GetLength(1); j++)
                    {
                        var pattern = patterns[i, j];

                        if (pattern != null && (i + startPatternIndex) < song.Length && song.Project.IsChannelActive(pattern.ChannelType))
                        {
                            var channelIdx = Channel.ChannelTypeToIndex(pattern.ChannelType, song.Project.ExpansionAudioMask, song.Project.ExpansionNumN163Channels);
                            song.Channels[channelIdx].PatternInstances[i + startPatternIndex] = pattern;
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
                                i + startPatternIndex,
                                customSettings[i].patternLength,
                                customSettings[i].beatLength,
                                customSettings[i].groove,
                                customSettings[i].groovePaddingMode);
                        }
                        else
                        {
                            Song.ClearPatternCustomSettings(i + minSelectedPatternIdx);
                        }
                    }
                }

                startPatternIndex += patterns.GetLength(0);
            }

            maxSelectedPatternIdx = minSelectedPatternIdx + numColumnsToPaste - 1;
            minSelectedChannelIdx = 0;
            maxSelectedChannelIdx = Song.Channels.Length - 1;

            song.InvalidateCumulativePatternCache();
            song.DeleteNotesPastMaxInstanceLength();

            App.UndoRedoManager.EndTransaction();
            PatternsPasted?.Invoke();
            SelectionChanged?.Invoke();
            ConditionalInvalidate();
        }

        public void Paste()
        {
            if (!IsSelectionValid())
                return;

            PasteInternal(false, false, 1);
        }

        public void PasteSpecial()
        {
            if (!IsSelectionValid())
                return;

            var dialog = new PropertyDialog(200);
            dialog.Properties.AddLabelCheckBox("Insert", false); // 0
            dialog.Properties.AddLabelCheckBox("Extend song", false); // 1
            dialog.Properties.AddIntegerRange("Repeat :", 1, 1, 32); // 2
            dialog.Properties.SetPropertyEnabled(1, false);
            dialog.Properties.PropertyChanged += PasteSpecialDialog_PropertyChanged;
            dialog.Properties.Build();

            if (dialog.ShowDialog(ParentForm) == DialogResult.OK)
            {
                PasteInternal(
                    dialog.Properties.GetPropertyValue<bool>(0),
                    dialog.Properties.GetPropertyValue<bool>(1),
                    dialog.Properties.GetPropertyValue<int>(2));
            }
        }

        private void PasteSpecialDialog_PropertyChanged(PropertyPage props, int propIdx, int rowIdx, int colIdx, object value)
        {
            if (propIdx == 0)
                props.SetPropertyEnabled(1, (bool)value);
        }

        protected void UpdateCursor()
        {
            if (captureOperation == CaptureOperation.DragSelection)
            {
                Cursor.Current = Cursors.DragCursor;
            }
            else
            {
                Cursor.Current = Cursors.Default;
            }
        }

        private void EndDragSelection(MouseEventArgs e)
        {
            if (captureThresholdMet)
            {
                if (!IsSelectionValid()) // No clue how we end up here with invalid selection.
                {
                    CancelDragSelection();
                }
                else
                {
                    var noteIdx = (int)((e.X - trackNameSizeX + scrollX) / noteSizeX);

                    if (noteIdx >= 0 && noteIdx < Song.GetPatternStartAbsoluteNoteIndex(Song.Length))
                    {
                        var patternIdx = Song.PatternIndexFromAbsoluteNoteIndex((int)((e.X - trackNameSizeX + scrollX) / noteSizeX));
                        var patternIdxDelta = patternIdx - selectionDragAnchorPatternIdx;
                        var tmpPatterns = GetSelectedPatterns(out var customSettings);

                        var dragChannelIdxStart = (captureMouseY - headerSizeY) / trackSizeY;
                        var dragChannelIdxCurrent = (e.Y - headerSizeY) / trackSizeY;
                        var channelIdxDelta = dragChannelIdxCurrent - dragChannelIdxStart;

                        var copy = ModifierKeys.HasFlag(Keys.Control);
                        var duplicate = copy && ModifierKeys.HasFlag(Keys.Shift) || channelIdxDelta != 0;

                        App.UndoRedoManager.BeginTransaction(TransactionScope.Song, Song.Id);

                        if (!copy)
                            DeleteSelection(false, customSettings != null && !copy);

                        var duplicatePatternMap = new Dictionary<Pattern, Pattern>(); ;

                        for (int i = minSelectedChannelIdx; i <= maxSelectedChannelIdx; i++)
                        {
                            for (int j = minSelectedPatternIdx; j <= maxSelectedPatternIdx; j++)
                            {
                                var ni = i + channelIdxDelta;
                                var nj = j + patternIdxDelta;
                                if (nj >= 0 && nj < Song.Length && ni >= 0 && ni < Song.Channels.Length)
                                {
                                    var sourcePattern = tmpPatterns[j - minSelectedPatternIdx, i - minSelectedChannelIdx];

                                    if (duplicate && sourcePattern != null)
                                    {
                                        Pattern duplicatedPattern = null;
                                        if (!duplicatePatternMap.TryGetValue(sourcePattern, out duplicatedPattern))
                                        {
                                            var destChannel = Song.Channels[ni];

                                            var newName = sourcePattern.Name;
                                            if (!destChannel.IsPatternNameUnique(newName))
                                                newName = destChannel.GenerateUniquePatternNameSmart(sourcePattern.Name);

                                            duplicatedPattern = sourcePattern.ShallowClone(destChannel);
                                            duplicatedPattern.RemoveUnsupportedChannelFeatures();
                                            // Intentionally changing the color so that its clear it a clone.
                                            duplicatedPattern.Color = ThemeBase.RandomCustomColor();
                                            duplicatePatternMap.Add(sourcePattern, duplicatedPattern);
                                            destChannel.RenamePattern(duplicatedPattern, newName);
                                        }
                                        Song.Channels[ni].PatternInstances[nj] = duplicatedPattern;
                                    }
                                    else
                                    {
                                        Song.Channels[ni].PatternInstances[nj] = sourcePattern;
                                    }
                                }
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
                                            customSettings[j - minSelectedPatternIdx].beatLength,
                                            customSettings[j - minSelectedPatternIdx].groove,
                                            customSettings[j - minSelectedPatternIdx].groovePaddingMode);
                                    }
                                    else
                                    {
                                        Song.ClearPatternCustomSettings(nj);
                                    }
                                }
                            }
                        }

                        Song.RemoveUnsupportedEffects();
                        Song.RemoveUnsupportedInstruments();
                        Song.InvalidateCumulativePatternCache();

                        App.UndoRedoManager.EndTransaction();

                        minSelectedChannelIdx = Utils.Clamp(minSelectedChannelIdx + channelIdxDelta, 0, Song.Channels.Length - 1);
                        maxSelectedChannelIdx = Utils.Clamp(maxSelectedChannelIdx + channelIdxDelta, 0, Song.Channels.Length - 1);
                        minSelectedPatternIdx = Utils.Clamp(minSelectedPatternIdx + patternIdxDelta, 0, Song.Length - 1);
                        maxSelectedPatternIdx = Utils.Clamp(maxSelectedPatternIdx + patternIdxDelta, 0, Song.Length - 1);

                        ConditionalInvalidate();
                        PatternModified?.Invoke();
                        SelectionChanged?.Invoke();
                    }
                }
            }
        }

        private void EndCaptureOperation(MouseEventArgs e)
        {
            if (captureOperation != CaptureOperation.None)
            {
                switch (captureOperation)
                {
                    case CaptureOperation.DragSelection:
                        EndDragSelection(e);
                        break;
                    case CaptureOperation.DragSeekBar:
                        UpdateSeekDrag(e.X, true);
                        break;
                }

                Capture = false;
                panning = false;
                captureOperation = CaptureOperation.None;
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            bool middle = e.Button.HasFlag(MouseButtons.Middle);

            if (middle)
                panning = false;
            else
                EndCaptureOperation(e);

            UpdateCursor();
        }

        private void AbortCaptureOperation()
        {
            if (App.UndoRedoManager.HasTransactionInProgress)
                App.UndoRedoManager.AbortTransaction();

            Capture = false;
            panning = false;
            captureOperation = CaptureOperation.None;

            ConditionalInvalidate();
        }

        protected void CancelDragSelection()
        {
            if (captureOperation == CaptureOperation.DragSelection)
            {
                selectionDragAnchorPatternIdx = -1;
                selectionDragAnchorPatternXFraction = -1.0f;
                captureOperation = CaptureOperation.None;
            }

            captureMouseX = -1;
            captureMouseY = -1;
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
                    var pattern = Song.Channels[i].PatternInstances[j];
                    if (pattern != null)
                        patternCache.Remove(pattern);
                    Song.Channels[i].PatternInstances[j] = null;
                }
            }

            if (clearCustomSettings)
            {
                for (int j = minSelectedPatternIdx; j <= maxSelectedPatternIdx; j++)
                    Song.ClearPatternCustomSettings(j);
            }

            Song.InvalidateCumulativePatternCache();

            if (trans)
            {
                ClearSelection();
                App.UndoRedoManager.EndTransaction();
                ConditionalInvalidate();
                PatternModified?.Invoke();
            }
        }

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
                bool ctrl  = e.Modifiers.HasFlag(Keys.Control);
                bool shift = e.Modifiers.HasFlag(Keys.Shift);

                if (ctrl)
                {
                    if (e.KeyCode == Keys.C)
                        Copy();
                    else if (e.KeyCode == Keys.X)
                        Cut();
                    else if (e.KeyCode == Keys.V && !shift)
                        Paste();
                    else if (e.KeyCode == Keys.V && shift)
                        PasteSpecial();
                }

                if (e.KeyCode == Keys.Delete && IsSelectionValid())
                {
                    CancelDragSelection();
                    DeleteSelection();
                }
            }

            if (captureOperation == CaptureOperation.DragSelection)
            {
                UpdateCursor();
                ConditionalInvalidate();
            }

            UpdateToolTip(null);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            if (captureOperation == CaptureOperation.DragSelection)
            {
                UpdateCursor();
                ConditionalInvalidate();
            }

            UpdateToolTip(null);
        }

        private void UpdateSeekDrag(int mouseX, bool final)
        {
            dragSeekPosition = (int)Math.Round((mouseX - trackNameSizeX + scrollX) / (float)noteSizeX);

            if (final)
                App.SeekSong(dragSeekPosition);

            ConditionalInvalidate();
        }

        private void ScrollIfSelectionNearEdge(int mouseX)
        {
            if ((mouseX - trackNameSizeX) < 0)
            {
                var scrollAmount = Utils.Clamp((trackNameSizeX - mouseX) / (float)trackNameSizeX, 0.0f, 1.0f);
                scrollX -= (int)(App.AverageTickRate * scrollAmount);
                ClampScroll();
            }
            else if ((Width - mouseX) < trackNameSizeX)
            {
                var scrollAmount = Utils.Clamp((mouseX - (Width - trackNameSizeX)) / (float)trackNameSizeX, 0.0f, 1.0f);
                scrollX += (int)(App.AverageTickRate * scrollAmount);
                ClampScroll();
            }
        }

        private void UpdateSelection(MouseEventArgs e, bool first = false)
        {
            ScrollIfSelectionNearEdge(e.X);

            var shift = ModifierKeys.HasFlag(Keys.Shift);

            if (first)
            {
                Debug.Assert(captureChannelIdx >= 0 && capturePatternIdx >= 0);

                minSelectedPatternIdx = capturePatternIdx;
                maxSelectedPatternIdx = capturePatternIdx;

                if (shift)
                {
                    minSelectedChannelIdx = captureChannelIdx;
                    maxSelectedChannelIdx = captureChannelIdx;
                    timeOnlySelection = false;
                }
                else
                {
                    minSelectedChannelIdx = 0;
                    maxSelectedChannelIdx = Song.Channels.Length - 1;
                    timeOnlySelection = true;
                }
            }
            else
            {
                GetClampedPatternForCoord(e.X, e.Y, out var channelIdx, out var patternIdx);

                minSelectedPatternIdx = Math.Min(patternIdx, capturePatternIdx);
                maxSelectedPatternIdx = Math.Max(patternIdx, capturePatternIdx);

                if (shift)
                {
                    minSelectedChannelIdx = Math.Min(channelIdx, captureChannelIdx);
                    maxSelectedChannelIdx = Math.Max(channelIdx, captureChannelIdx);
                    timeOnlySelection = false;
                }
                else
                {
                    minSelectedChannelIdx = 0;
                    maxSelectedChannelIdx = Song.Channels.Length - 1;
                    timeOnlySelection = true;
                }
            }

            ConditionalInvalidate();
            SelectionChanged?.Invoke();
        }

        private void UpdateAltZoom(MouseEventArgs e)
        {
            var deltaY = e.Y - captureMouseY;

            if (Math.Abs(deltaY) > 50)
            {
                ZoomAtLocation(e.X, Math.Sign(-deltaY));
                captureMouseY = e.Y;
            }
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
            if (e == null)
            {
                var pt = PointToClient(Cursor.Position);
                e = new MouseEventArgs(MouseButtons.None, 1, pt.X, pt.Y, 0);
            }

            string tooltip = "";

            bool inPatternZone = GetPatternForCoord(e.X, e.Y, out int channelIdx, out int patternIdx, out var inPatternHeader);

            if (inPatternZone)
            {
                var pattern = Song.Channels[channelIdx].PatternInstances[patternIdx];

                var tooltipList = new List<string>();

                if (pattern == null)
                    tooltipList.Add("{MouseLeft} Add Pattern");

                tooltipList.Add("{L} {MouseLeft} Set Loop Point");
                tooltipList.Add("{MouseWheel} Pan");

                if (pattern == null || !inPatternHeader)
                {
                    if (ModifierKeys.HasFlag(Keys.Shift))
                        tooltipList.Add("{MouseRight} Select Rectangle");
                    else
                        tooltipList.Add("{MouseRight} Select Column");
                }

                if (pattern != null)
                {
                    if (inPatternHeader)
                        tooltipList.Add("{MouseRight} Delete Pattern");

                    tooltipList.Add("{MouseLeft}{MouseLeft} Pattern properties");
                }

                if (IsPatternSelected(channelIdx, patternIdx))
                {
                    tooltipList.Add("{Drag} Move Pattern");
                    tooltipList.Add("{Ctrl} {Drag} Clone pattern");
                }

                if (tooltipList.Count >= 3)
                {
                    var array = tooltipList.ToArray();
                    var numFirstLine = array.Length / 2;
                    tooltip = string.Join(" - ", array, 0, numFirstLine) + "\n" + string.Join(" - ", array, numFirstLine, array.Length - numFirstLine);
                }
                else
                {
                    tooltip = string.Join(" - ", tooltipList);
                }
            }
            else if (IsMouseInHeader(e))
            {
                tooltip = "{MouseLeft} Seek - {MouseLeft}{MouseLeft} Customize Pattern\n{MouseRight} Select Column - {L} {MouseLeft} Set Loop Point - {MouseWheel} Pan";
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
                    if (idx >= 1 && idx <= 12)
                        tooltip += $" {{Ctrl}} {{F{idx}}}";
                }
                else
                {
                    tooltip = "{MouseLeft} Make channel active";
                    int idx = (e.Y - headerSizeY) / trackSizeY + 1;
                    if (idx >= 1 && idx <= 12)
                        tooltip += $" {{F{idx}}}";
                }
            }

            App.SetToolTip(tooltip);
        }

        private void UpdateScrollBarX(MouseEventArgs e)
        {
            GetScrollBarParams(out _, out var scrollBarSizeX);
            GetMinMaxScroll(out _, out var maxScrollX);
            int scrollAreaSizeX = Width - trackNameSizeX;
            scrollX = (int)Math.Round(captureScrollX + ((e.X - captureMouseX) / (float)(scrollAreaSizeX - scrollBarSizeX) * maxScrollX));
            ClampScroll();
            ConditionalInvalidate();
        }

        private void UpdateCaptureOperation(MouseEventArgs e)
        {
            if (captureOperation != CaptureOperation.None && !captureThresholdMet)
            {
                if (Math.Abs(e.X - captureMouseX) > 4 ||
                    Math.Abs(e.Y - captureMouseY) > 4)
                {
                    captureThresholdMet = true;
                }
            }

            if (captureOperation != CaptureOperation.None && captureThresholdMet)
            {
                bool inPatternZone = GetPatternForCoord(e.X, e.Y, out int channelIdx, out int patternIdx, out _);

                switch (captureOperation)
                {
                    case CaptureOperation.Select:
                        UpdateSelection(e);
                        break;
                    case CaptureOperation.AltZoom:
                        UpdateAltZoom(e);
                        break;
                    case CaptureOperation.DragSeekBar:
                        UpdateSeekDrag(e.X, false);
                        break;
                    case CaptureOperation.ScrollBar:
                        UpdateScrollBarX(e);
                        break;
                    default:
                        ConditionalInvalidate();
                        break;
                }
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            bool middle = e.Button.HasFlag(MouseButtons.Middle) || (e.Button.HasFlag(MouseButtons.Left) && ModifierKeys.HasFlag(Keys.Alt));

            UpdateCursor();
            UpdateCaptureOperation(e);

            if (middle)
            {
                DoScroll(e.X - mouseLastX, e.Y - mouseLastY);
            }

            UpdateToolTip(e);
            ShowExpansionIcons = false;

            mouseLastX = e.X;
            mouseLastY = e.Y;
        }

        private void EditPatternCustomSettings(Point pt, int patternIdx)
        {
            var dlg = new PropertyDialog(PointToScreen(pt), 240);
            var song = Song;
            var enabled = song.PatternHasCustomSettings(patternIdx);

            var minPattern = patternIdx;
            var maxPattern = patternIdx;

            if (IsValidTimeOnlySelection())
            {
                minPattern = minSelectedPatternIdx;
                maxPattern = maxSelectedPatternIdx;
            }

            var tempoProperties = new TempoProperties(dlg.Properties, song, patternIdx, minPattern, maxPattern);

            dlg.Properties.AddCheckBox("Custom Pattern :", song.PatternHasCustomSettings(patternIdx), CommonTooltips.CustomPattern); // 0
            tempoProperties.AddProperties();
            tempoProperties.EnableProperties(enabled);
            dlg.Properties.PropertyChanged += PatternCustomSettings_PropertyChanged;
            dlg.Properties.PropertiesUserData = tempoProperties;
            dlg.Properties.Build();

            if (dlg.ShowDialog(ParentForm) == DialogResult.OK)
            {
                App.UndoRedoManager.BeginTransaction(TransactionScope.Song, song.Id);
                tempoProperties.Apply(dlg.Properties.GetPropertyValue<bool>(0));
                App.UndoRedoManager.EndTransaction();
                ConditionalInvalidate();
                PatternModified?.Invoke();
            }
        }

        private void PatternCustomSettings_PropertyChanged(PropertyPage props, int propIdx, int rowIdx, int colIdx, object value)
        {
            if (propIdx == 0)
            {
                var tempoProperties = props.PropertiesUserData as TempoProperties;
                tempoProperties.EnableProperties((bool)value);
            }
        }

        private void EditPatternProperties(Point pt, Pattern pattern)
        {
            bool multiplePatternSelected = (maxSelectedChannelIdx != minSelectedChannelIdx) || (minSelectedPatternIdx != maxSelectedPatternIdx);

            var dlg = new PropertyDialog(PointToScreen(pt), 240);
            dlg.Properties.AddColoredString(pattern.Name, pattern.Color);
            dlg.Properties.SetPropertyEnabled(0, !multiplePatternSelected);
            dlg.Properties.AddColorPicker(pattern.Color);
            dlg.Properties.Build();

            if (dlg.ShowDialog(ParentForm) == DialogResult.OK)
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
                            var pat = Song.Channels[i].PatternInstances[j];
                            if (pat != null)
                                pat.Color = newColor;
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
            bool inPatternZone = GetPatternForCoord(e.X, e.Y, out int channelIdx, out int patternIdx, out _);

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

        private void ZoomAtLocation(int x, int delta)
        {
            // When continuously following, zoom at the seek bar location.
            if (continuouslyFollowing)
                x = (int)(Width * ContinuousFollowPercent);

            int pixelX = x - trackNameSizeX;
            int absoluteX = pixelX + scrollX;
            if (delta < 0 && zoomLevel > MinZoomLevel) { zoomLevel--; absoluteX /= 2; }
            if (delta > 0 && zoomLevel < MaxZoomLevel) { zoomLevel++; absoluteX *= 2; }
            scrollX = absoluteX - pixelX;

            UpdateRenderCoords();
            ClampScroll();
            ConditionalInvalidate();
        }

        public override void DoMouseWheel(MouseEventArgs e)
        {
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);

            if (Settings.TrackPadControls && !ModifierKeys.HasFlag(Keys.Control))
            {
                if (ModifierKeys.HasFlag(Keys.Shift))
                    scrollX -= e.Delta;

                ClampScroll();
                ConditionalInvalidate();
            }
            else
            {
                ZoomAtLocation(e.X, e.Delta);
            }
        }

#if FALSE // MATTT FAMISTUDIO_WINDOWS
        protected override void OnMouseCaptureChanged(EventArgs e)
        {
            AbortCaptureOperation();
            base.OnMouseCaptureChanged(e);
        }
#endif

        protected override void OnMouseHorizontalWheel(MouseEventArgs e)
        {
            scrollX += e.Delta;
            ClampScroll();
            ConditionalInvalidate();
        }

        protected bool EnsureSeekBarVisible(float percent = ContinuousFollowPercent)
        {
            var seekX = (int)Math.Round(noteSizeX * App.CurrentFrame - scrollX);
            var minX = 0;
            var maxX = (int)((Width * percent) - trackNameSizeX);

            // Keep everything visible 
            if (seekX < minX)
                scrollX -= (minX - seekX);
            else if (seekX > maxX)
                scrollX += (seekX - maxX);

            ClampScroll();

            seekX = (int)Math.Round(noteSizeX * App.CurrentFrame - scrollX);
            return seekX == maxX;
        }

        public void UpdateFollowMode(bool force = false)
        {
            continuouslyFollowing = false;

            if ((App.IsPlaying || force) && App.FollowModeEnabled && Settings.FollowSync != Settings.FollowSyncPianoRoll && !panning && captureOperation == CaptureOperation.None)
            {
                var frame = App.CurrentFrame;
                var seekX = (int)Math.Round(noteSizeX * App.CurrentFrame - scrollX);

                if (Settings.FollowMode == Settings.FollowModeJump)
                {
                    var maxX = Width - trackNameSizeX;
                    if (seekX < 0 || seekX > maxX)
                        scrollX = (int)Math.Round(frame * noteSizeX);
                }
                else
                {
                    continuouslyFollowing = EnsureSeekBarVisible();
                }

                ClampScroll();
            }
        }

        public void Tick()
        {
            if (App == null)
                return;

            if (captureOperation == CaptureOperation.Select)
            {
                var pt = PointToClient(Cursor.Position);
                UpdateSelection(new MouseEventArgs(MouseButtons.None, 0, pt.X, pt.Y, 0), false);
            }

            UpdateFollowMode();
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
            if (patternCache != null)
                patternCache.Clear();
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
            buffer.Serialize(ref timeOnlySelection);

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
