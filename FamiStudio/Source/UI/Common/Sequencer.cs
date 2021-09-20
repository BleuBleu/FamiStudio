using System;
using System.Linq;
using System.Drawing;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Media;
using System.Diagnostics;

using Color = System.Drawing.Color;

using RenderBitmap      = FamiStudio.GLBitmap;
using RenderBitmapAtlas = FamiStudio.GLBitmapAtlas;
using RenderBrush       = FamiStudio.GLBrush;
using RenderPath        = FamiStudio.GLGeometry;
using RenderControl     = FamiStudio.GLControl;
using RenderGraphics    = FamiStudio.GLGraphics;
using RenderTheme       = FamiStudio.ThemeRenderResources;

namespace FamiStudio
{
    public class Sequencer : RenderControl
    {
        const int DefaultTrackNameSizeX      = PlatformUtils.IsMobile ? 60 : 94;
        const int DefaultHeaderSizeY         = 17;
        const int DefaultPatternHeaderSizeY  = 13;
        const int DefaultScrollMargin        = 128;
        const int DefaultBarTextPosY         = 2;
        const int DefaultTrackIconPosX       = 2;
        const int DefaultTrackIconPosY       = 3;
        const int DefaultTrackNamePosX       = 21;
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
        const float ScrollSpeedFactor        = PlatformUtils.IsMobile ? 2.0f : 1.0f;
        const float DefaultZoom              = PlatformUtils.IsMobile ? 0.5f : 2.0f;

        const float MinZoom = 0.25f;
        const float MaxZoom = 16.0f;

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
        int virtualSizeY;
        float noteSizeX;

        int scrollX = 0;
        int scrollY = 0;
        int mouseLastX = 0;
        int mouseLastY = 0;
        float zoom = DefaultZoom;
        float bitmapScale = 1.0f;
        float channelBitmapScale = 1.0f;
        float flingVelX;
        float flingVelY;

        enum CaptureOperation
        {
            None,
            Select,
            DragSelection,
            AltZoom,
            DragSeekBar,
            ScrollBar,
            MobileZoom,
            MobilePan,
        }

        static readonly bool[] captureNeedsThreshold = new[]
        {
            false, // None
            PlatformUtils.IsMobile, // Select
            true,  // DragSelection
            false, // AltZoom
            false, // DragSeekBar
            false, // ScrollBar
            false, // MobileZoom,
            false, // MobilePan,
        };

        static readonly bool[] captureWantsRealTimeUpdate = new[]
        {
            false, // None
            true,  // Select
            true,  // DragSelection
            false, // AltZoom
            true,  // DragSeekBar
            false, // ScrollBar
            false, // MobileZoom,
            false, // MobilePan,
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
        int hoverChannelIdx = -1;
        int hoverPatternIdx = -1;
        int   selectionDragAnchorPatternIdx = -1;
        float selectionDragAnchorPatternXFraction = -1.0f;
        CaptureOperation captureOperation = CaptureOperation.None;
        bool panning = false; // TODO: Make this a capture operation.
        bool continuouslyFollowing = false;
        bool captureThresholdMet = false;
        bool captureRealTimeUpdate = false;

        PatternBitmapCache patternCache;

        RenderBrush seekBarBrush;
        RenderBrush seekBarRecBrush;
        RenderBrush whiteKeyBrush;
        RenderBrush patternHeaderBrush;
        RenderBrush selectedPatternVisibleBrush;
        RenderBrush selectedPatternInvisibleBrush;
        RenderBrush selectionPatternBrush;
        RenderBrush hoverPatternBrush;
        RenderPath seekGeometry;

        RenderBitmapAtlas bmpAtlasExpansions;
        RenderBitmapAtlas bmpAtlasTracks;
        RenderBitmapAtlas bmpAtlasMisc;

        enum MiscImageIndices
        {
            ForceDisplay,
            LoopPoint,
            Instanciate,
            Duplicate,
            DuplicateMove,
            Count
        };

        readonly string[] MiscImageNames = new string[]
        {
            "GhostSmall",
            "LoopSmallFill",
            "Instance",
            "Duplicate",
            "DuplicateMove"
        };

        public delegate void TrackBarDelegate(int trackIdx, int barIdx);
        public delegate void ChannelDelegate(int channelIdx);
        public delegate void EmptyDelegate();

        public event TrackBarDelegate PatternClicked;
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
            get { return App?.SelectedSong; }
        }

        public bool ShowSelection
        {
            get { return showSelection; }
            set { showSelection = value; MarkDirty(); }
        }

        public bool ShowExpansionIcons
        {
            get { return showExpansionIcons; }
            set
            {
                if (showExpansionIcons != value)
                {
                    showExpansionIcons = value;
                    MarkDirty();
                }
            }
        }

        private void UpdateRenderCoords()
        {
            var scrollBarSize  = Settings.ScrollBars == 1 ? DefaultScrollBarThickness1 : (Settings.ScrollBars == 2 ? DefaultScrollBarThickness2 : 0);
            var patternZoom    = Song != null ? 128.0f / (float)Utils.NextPowerOfTwo(Song.PatternLength) : 1.0f;

            trackNameSizeX     = ScaleForMainWindow(DefaultTrackNameSizeX);
            headerSizeY        = ScaleForMainWindow(DefaultHeaderSizeY);
            trackSizeY         = ScaleForMainWindow(ComputeDesiredTrackSizeY());
            scrollMargin       = ScaleForMainWindow(DefaultScrollMargin);
            barTextPosY        = ScaleForMainWindow(DefaultBarTextPosY);
            trackIconPosX      = ScaleForMainWindow(DefaultTrackIconPosX);
            trackIconPosY      = ScaleForMainWindow(DefaultTrackIconPosY);
            trackNamePosX      = ScaleForMainWindow(DefaultTrackNamePosX);
            trackNamePosY      = ScaleForMainWindow(DefaultTrackNamePosY);
            ghostNoteOffsetX   = ScaleForMainWindow(DefaultGhostNoteOffsetX);
            ghostNoteOffsetY   = ScaleForMainWindow(DefaultGhostNoteOffsetY);
            patternNamePosX    = ScaleForMainWindow(DefaultPatternNamePosX);
            patternNamePosY    = ScaleForMainWindow(DefaultPatternNamePosY);
            headerIconPosX     = ScaleForMainWindow(DefaultHeaderIconPosX);
            headerIconPosY     = ScaleForMainWindow(DefaultHeaderIconPosY);
            headerIconSizeX    = ScaleForMainWindow(DefaultHeaderIconSizeX);
            scrollBarThickness = ScaleForMainWindow(scrollBarSize);
            minScrollBarLength = ScaleForMainWindow(DefaultMinScrollBarLength);
            noteSizeX          = ScaleForMainWindowFloat(zoom * patternZoom);
            virtualSizeY       = Song != null ? trackSizeY * Song.Channels.Length : 0;

            // Shave a couple pixels when the size is getting too small.
            if (TrackSizeIsSmall())
            {
                patternNamePosY    = ScaleForFont(DefaultPatternNamePosY - 1);
                patternHeaderSizeY = ScaleForFont(DefaultPatternHeaderSizeY - 2);
            }
            else
            {
                patternNamePosY    = ScaleForFont(DefaultPatternNamePosY);
                patternHeaderSizeY = ScaleForFont(DefaultPatternHeaderSizeY);
            }
        }

        private int GetPixelForNote(int n, bool scroll = true)
        {
            // On PC, all math noteSizeX are always integer, but on mobile, they 
            // can be float. We need to cast into double since at the maximum zoom,
            // in a *very* long song, we are hitting the precision limit of floats.
            var x = (int)(n * (double)noteSizeX);
            if (scroll)
                x -= scrollX;
            return x;
        }

        private int GetNoteForPixel(int x, bool scroll = true)
        {
            if (scroll)
                x += scrollX;
            return (int)(x / (double)noteSizeX);
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
            if (PlatformUtils.IsMobile)
                return 32; // MATTT Constant or something.
            else
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
            MarkDirty();
        }

        public void Reset()
        {
            scrollX = 0;
            scrollY = 0;
            zoom = DefaultZoom;
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

        private void SetSelection(int minChannelIdx, int maxChannelIdx, int minPatternIdx, int maxPatternIdx, bool timeOnly = false)
        {
            minSelectedChannelIdx = minChannelIdx;
            maxSelectedChannelIdx = maxChannelIdx;
            minSelectedPatternIdx = minPatternIdx;
            maxSelectedPatternIdx = maxPatternIdx;
            timeOnlySelection = timeOnly;
            SelectionChanged?.Invoke();
        }

        private void EnsureSelectionInclude(int channelIdx, int patternIdx)
        {
            if (!IsSelectionValid())
            {
                SetSelection(channelIdx, channelIdx, patternIdx, patternIdx, false);
            }
            else
            {
                SetSelection(Math.Min(minSelectedChannelIdx, channelIdx),
                             Math.Max(maxSelectedChannelIdx, channelIdx),
                             Math.Min(minSelectedPatternIdx, patternIdx),
                             Math.Max(maxSelectedPatternIdx, patternIdx),
                             false);
            }
        }

        private bool IsPatternSelected(int channelIdx, int patternIdx)
        {
            return channelIdx >= minSelectedChannelIdx && channelIdx <= maxSelectedChannelIdx &&
                   patternIdx >= minSelectedPatternIdx && patternIdx <= maxSelectedPatternIdx;
        }

        private bool IsPatternColumnSelected(int patternIdx)
        {
            return IsValidTimeOnlySelection() && patternIdx >= minSelectedPatternIdx && patternIdx <= maxSelectedPatternIdx;
        }

        private bool SelectionContainsMultiplePatterns()
        {
            return IsSelectionValid() && (maxSelectedChannelIdx - minSelectedChannelIdx + 1) * (maxSelectedPatternIdx - maxSelectedPatternIdx + 1) > 1;
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
            Debug.Assert(MiscImageNames.Length == (int)MiscImageIndices.Count);

            UpdateRenderCoords();

            patternCache = new PatternBitmapCache(g);

            bmpAtlasExpansions = g.CreateBitmapAtlasFromResources(ExpansionType.Icons);
            bmpAtlasTracks     = g.CreateBitmapAtlasFromResources(ChannelType.Icons);
            bmpAtlasMisc       = g.CreateBitmapAtlasFromResources(MiscImageNames);

            seekBarBrush = g.CreateSolidBrush(Theme.SeekBarColor);
            seekBarRecBrush = g.CreateSolidBrush(Theme.DarkRedFillColor);
            whiteKeyBrush = g.CreateHorizontalGradientBrush(0, trackNameSizeX, Theme.LightGreyFillColor1, Theme.LightGreyFillColor2);
            patternHeaderBrush = g.CreateVerticalGradientBrush(0, patternHeaderSizeY, Theme.LightGreyFillColor1, Theme.LightGreyFillColor2);
            selectedPatternVisibleBrush   = g.CreateSolidBrush(Color.FromArgb(96, Theme.LightGreyFillColor1));
            selectedPatternInvisibleBrush = g.CreateSolidBrush(Color.FromArgb(48, Theme.LightGreyFillColor1));
            selectionPatternBrush = g.CreateSolidBrush(Theme.LightGreyFillColor1);
            hoverPatternBrush = g.CreateSolidBrush(Theme.WhiteColor);

            if (PlatformUtils.IsMobile)
            {
                bitmapScale = g.WindowScaling * 0.5f;
                channelBitmapScale = g.WindowScaling * 0.25f;
            }
            
            seekGeometry = g.CreateGeometry(new float[,]
            {
                { -headerSizeY / 2, 1 },
                { 0, headerSizeY - 2 },
                { headerSizeY / 2, 1 }
            }, true);
        }

        protected override void OnRenderTerminated()
        {
            Utils.DisposeAndNullify(ref bmpAtlasExpansions);
            Utils.DisposeAndNullify(ref bmpAtlasTracks);
            Utils.DisposeAndNullify(ref bmpAtlasMisc);
            Utils.DisposeAndNullify(ref seekBarBrush);
            Utils.DisposeAndNullify(ref seekBarRecBrush);
            Utils.DisposeAndNullify(ref whiteKeyBrush);
            Utils.DisposeAndNullify(ref patternHeaderBrush);
            Utils.DisposeAndNullify(ref selectedPatternVisibleBrush);
            Utils.DisposeAndNullify(ref selectedPatternInvisibleBrush);
            Utils.DisposeAndNullify(ref selectionPatternBrush);
            Utils.DisposeAndNullify(ref hoverPatternBrush);
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

        private void SetHoverPattern(int c, int p)
        {
            hoverChannelIdx = c;
            hoverPatternIdx = p;
        }

        private void ClearHoverPatern()
        {
            hoverChannelIdx = -1;
            hoverPatternIdx = -1;
        }

        private bool HasHoverPattern()
        {
            return hoverChannelIdx >= 0 && hoverPatternIdx >= 0;
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
            c.DrawRectangle(0, 0, trackNameSizeX, Height, ThemeResources.DarkGreyFillBrush1);

            // Horizontal lines
            c.DrawLine(0, 0, trackNameSizeX, 0, ThemeResources.BlackBrush);
            c.DrawLine(0, Height - 1, trackNameSizeX, Height - 1, ThemeResources.BlackBrush);

            for (int i = 0, y = headerSizeY; i < Song.Channels.Length; i++, y += trackSizeY)
                c.DrawLine(0, y, Width, y, ThemeResources.BlackBrush);

            // Vertical line seperating the track labels.
            c.DrawLine(trackNameSizeX - 1, 0, trackNameSizeX - 1, Height, ThemeResources.BlackBrush);

            c.PushTranslation(0, headerSizeY);

            // Icons
            var showExpIcons = showExpansionIcons && Song.Project.UsesAnyExpansionAudio;
            var atlas = showExpIcons ? bmpAtlasExpansions : bmpAtlasTracks;

            for (int i = 0, y = 0; i < Song.Channels.Length; i++, y += trackSizeY)
            {
                var bitmapIndex = showExpIcons ? Song.Channels[i].Expansion : Song.Channels[i].Type;
                c.DrawBitmapAtlas(atlas, bitmapIndex, trackIconPosX, y + trackIconPosY, (App.ChannelMask & (1 << i)) != 0 ? 1.0f : 0.2f, channelBitmapScale, Theme.LightGreyFillColor1);
            }

            // Track names
            var selectedChannelIndex = App.SelectedChannelIndex;
            for (int i = 0, y = 0; i < Song.Channels.Length; i++, y += trackSizeY)
            {
                var font = i == selectedChannelIndex ? ThemeResources.FontMediumBold : ThemeResources.FontMedium;
                c.DrawText(Song.Channels[i].Name, font, trackNamePosX, y + trackNamePosY, ThemeResources.LightGreyFillBrush2);
            }

            // Ghost note icons
            for (int i = 0, y = 0; i < Song.Channels.Length; i++, y += trackSizeY)
            {
                c.DrawBitmapAtlas(bmpAtlasMisc, (int)MiscImageIndices.ForceDisplay, trackNameSizeX - ghostNoteOffsetX, y + trackSizeY - ghostNoteOffsetY - 1, (App.GhostChannelMask & (1 << i)) != 0 ? 1.0f : 0.2f, bitmapScale, Theme.LightGreyFillColor1);
            }

            c.PopTransform();
            g.DrawCommandList(c, new Rectangle(0, 0, trackNameSizeX, Height));
        }

        protected void RenderPatternArea(RenderGraphics g)
        {
            var ch = g.CreateCommandList(); // Header stuff 
            var cb = g.CreateCommandList(); // Background stuff
            var cp = g.CreateCommandList(); // Pattern stuff
            var cf = g.CreateCommandList(); // Foreground stuff

            var seekX = GetPixelForNote(GetSeekFrameToDraw());
            var minVisibleNoteIdx = Math.Max(GetNoteForPixel(0), 0);
            var maxVisibleNoteIdx = Math.Min(GetNoteForPixel(Width) + 1, Song.GetPatternStartAbsoluteNoteIndex(Song.Length));
            var minVisiblePattern = Utils.Clamp(Song.PatternIndexFromAbsoluteNoteIndex(minVisibleNoteIdx) + 0, 0, Song.Length);
            var maxVisiblePattern = Utils.Clamp(Song.PatternIndexFromAbsoluteNoteIndex(maxVisibleNoteIdx) + 1, 0, Song.Length);
            var actualSizeY = Height - scrollBarThickness;

            // Grey background rectangles ever other pattern + vertical lines 
            ch.PushTranslation(trackNameSizeX, 0);

            for (int i = minVisiblePattern; i < maxVisiblePattern; i++)
            {
                if ((i & 1) == 0)
                {
                    var px = GetPixelForNote(Song.GetPatternStartAbsoluteNoteIndex(i));
                    var sx = GetPixelForNote(Song.GetPatternLength(i), false);
                    ch.FillRectangle(px, 0, px + sx, Height, ThemeResources.DarkGreyFillBrush1);
                }
            }

            if (IsSelectionValid())
            {
                cp.FillRectangle(
                    GetPixelForNote(Song.GetPatternStartAbsoluteNoteIndex(Math.Min(minSelectedPatternIdx + 0, Song.Length))), trackSizeY * (minSelectedChannelIdx + 0) + headerSizeY,
                    GetPixelForNote(Song.GetPatternStartAbsoluteNoteIndex(Math.Min(maxSelectedPatternIdx + 1, Song.Length))), trackSizeY * (maxSelectedChannelIdx + 1) + headerSizeY,
                    showSelection ? selectedPatternVisibleBrush : selectedPatternInvisibleBrush);

                if (IsValidTimeOnlySelection())
                {
                    ch.FillRectangle(
                        GetPixelForNote(Song.GetPatternStartAbsoluteNoteIndex(Math.Min(minSelectedPatternIdx + 0, Song.Length))), 0,
                        GetPixelForNote(Song.GetPatternStartAbsoluteNoteIndex(Math.Min(maxSelectedPatternIdx + 1, Song.Length))), headerSizeY,
                        showSelection ? selectedPatternVisibleBrush : selectedPatternInvisibleBrush);
                }
            }

            // Header
            ch.DrawLine(0, 0, Width, 0, ThemeResources.BlackBrush);
            ch.DrawLine(0, Height - 1, Width, Height - 1, ThemeResources.BlackBrush);

            // Vertical lines
            for (int i = minVisiblePattern; i <= maxVisiblePattern; i++)
            {
                if (i != 0)
                {
                    var px = GetPixelForNote(Song.GetPatternStartAbsoluteNoteIndex(i));
                    ch.DrawLine(px, 0, px, actualSizeY, ThemeResources.BlackBrush);
                }
            }

            for (int i = minVisiblePattern; i < maxVisiblePattern; i++)
            {
                var patternLen = Song.GetPatternLength(i);
                var px = GetPixelForNote(Song.GetPatternStartAbsoluteNoteIndex(i));
                var sx = GetPixelForNote(patternLen, false);

                ch.PushTranslation(px, 0);

                var text = (i + 1).ToString();
                if (Song.PatternHasCustomSettings(i))
                    text += "*";
                ch.DrawText(text, ThemeResources.FontMedium, 0, barTextPosY, ThemeResources.LightGreyFillBrush1, RenderTextFlags.Center | RenderTextFlags.Clip, sx);

                if (i == Song.LoopPoint)
                {
                    var bmpSize = bmpAtlasMisc.GetElementSize((int)MiscImageIndices.LoopPoint);
                    ch.FillRectangle(headerIconPosX, headerIconPosY, headerIconPosX + bmpSize.Width, headerIconPosY + bmpSize.Height, ThemeResources.DarkGreyLineBrush2);
                    ch.DrawBitmapAtlas(bmpAtlasMisc, (int)MiscImageIndices.LoopPoint, headerIconPosX, headerIconPosY, 1.0f, bitmapScale, Theme.LightGreyFillColor1);
                }

                ch.PopTransform();
            }

            cb.PushTranslation(0, -scrollY);

            // Horizontal lines
            for (int i = 0, y = headerSizeY; i < Song.Channels.Length; i++, y += trackSizeY)
                cb.DrawLine(0, y, Width, y, ThemeResources.BlackBrush);

            // Patterns
            int patternCacheSizeY = trackSizeY - patternHeaderSizeY - 1;
            patternCache.Update(patternCacheSizeY);

            for (int i = minVisiblePattern; i < maxVisiblePattern; i++)
            {
                var patternLen = Song.GetPatternLength(i);
                var noteLen = Song.UsesFamiTrackerTempo ? 1 : Song.GetPatternNoteLength(i);
                var px = GetPixelForNote(Song.GetPatternStartAbsoluteNoteIndex(i));
                var sx = GetPixelForNote(patternLen, false);

                cb.PushTranslation(px, 0);

                for (int t = 0, py = headerSizeY; t < Song.Channels.Length; t++, py += trackSizeY)
                {
                    var pattern = Song.Channels[t].PatternInstances[i];

                    if (pattern != null)
                    {
                        var bmp = patternCache.GetOrAddPattern(pattern, patternLen, noteLen, out var u0, out var v0, out var u1, out var v1);

                        // DROIDTODO : The icons arent aligned probably on mobile while moving patterns.
                        cp.PushTranslation(0, py);
                        cp.FillRectangle(1, 1, sx, patternHeaderSizeY, g.GetVerticalGradientBrush(pattern.Color, patternHeaderSizeY, 0.8f));
                        cp.DrawLine(0, patternHeaderSizeY, sx, patternHeaderSizeY, ThemeResources.BlackBrush);
                        cp.DrawBitmap(bmp, 1.0f, 1.0f + patternHeaderSizeY, sx - 1, patternCacheSizeY, 1.0f, u0, v0, u1, v1); // MATTT : We use the bitmap size here.
                        cp.DrawText(pattern.Name, ThemeResources.FontSmall, patternNamePosX, patternNamePosY, ThemeResources.BlackBrush, RenderTextFlags.Left | RenderTextFlags.Clip, sx - patternNamePosX);
                        if (IsPatternSelected(t, i))
                            cf.DrawRectangle(0, 0, sx, trackSizeY, selectionPatternBrush, 2, true);
                        cp.PopTransform();
                    }

                    // DROIDTODO : If this works, bring to desktop?
                    if (PlatformUtils.IsMobile && hoverChannelIdx == t && hoverPatternIdx == i)
                    {
                        cf.PushTranslation(0, py);
                        cf.DrawRectangle(0, 0, sx, trackSizeY, hoverPatternBrush, 2, false);
                        cf.PopTransform();
                    }
                }

                cp.PopTransform();
            }

            // TODO : This is really bad, since all the logic is in the rendering code. Make
            // this more like any other capture op eventually.
            if (captureOperation == CaptureOperation.DragSelection)
            {
                var pt = new Point(mouseLastX, mouseLastY);
                var noteIdx = GetNoteForPixel(pt.X - trackNameSizeX);

                if (noteIdx >= 0 && noteIdx < Song.GetPatternStartAbsoluteNoteIndex(Song.Length))
                {
                    var patternIdx = Song.PatternIndexFromAbsoluteNoteIndex(noteIdx);
                    var patternIdxDelta = patternIdx - selectionDragAnchorPatternIdx;

                    var dragChannelIdxStart = GetChannelIndexForCoord(captureMouseY);
                    var dragChannelIdxCurrent = GetChannelIndexForCoord(pt.Y);
                    var channelIdxDelta = dragChannelIdxCurrent - dragChannelIdxStart;

                    for (int j = minSelectedChannelIdx + channelIdxDelta; j <= maxSelectedChannelIdx + channelIdxDelta; j++)
                    {
                        if (j < 0 || j > Song.Channels.Length)
                            continue;

                        var y = j * trackSizeY;

                        // Center.
                        var patternSizeX = GetPixelForNote(Song.GetPatternLength(patternIdx), false);
                        var anchorOffsetLeftX  = (int)(patternSizeX * selectionDragAnchorPatternXFraction);
                        var anchorOffsetRightX = (int)(patternSizeX * (1.0f - selectionDragAnchorPatternXFraction));

                        var instance  = ModifierKeys.HasFlag(Keys.Control);
                        var duplicate = instance && ModifierKeys.HasFlag(Keys.Shift);

                        var bmpCopy = MiscImageIndices.Count;
                        var bmpSize = bmpAtlasMisc.GetElementSize((int)MiscImageIndices.Duplicate);

                        if (channelIdxDelta != 0)
                            bmpCopy = (duplicate || instance) ? MiscImageIndices.Duplicate : MiscImageIndices.DuplicateMove;
                        else
                            bmpCopy = duplicate ? MiscImageIndices.Duplicate : (instance ? MiscImageIndices.Instanciate : MiscImageIndices.Count);

                        cp.PushTranslation(pt.X - trackNameSizeX, y + headerSizeY);
                        cp.FillAndDrawRectangle(- anchorOffsetLeftX, 0, - anchorOffsetLeftX + patternSizeX, trackSizeY, selectedPatternVisibleBrush, ThemeResources.BlackBrush);

                        if (bmpCopy != MiscImageIndices.Count)
                            cp.DrawBitmapAtlas(bmpAtlasMisc, (int)bmpCopy, -anchorOffsetLeftX + patternSizeX / 2 - bmpSize.Width / 2, trackSizeY / 2 - bmpSize.Height / 2, 1.0f, bitmapScale, Theme.LightGreyFillColor1);

                        // Left side
                        for (int p = patternIdx - 1; p >= minSelectedPatternIdx + patternIdxDelta && p >= 0; p--)
                        {
                            patternSizeX = GetPixelForNote(Song.GetPatternLength(p), false);
                            anchorOffsetLeftX += patternSizeX;

                            cp.FillAndDrawRectangle(-anchorOffsetLeftX, 0, -anchorOffsetLeftX + patternSizeX, trackSizeY, selectedPatternVisibleBrush, ThemeResources.BlackBrush);

                            if (bmpCopy != MiscImageIndices.Count)
                                cp.DrawBitmapAtlas(bmpAtlasMisc, (int)bmpCopy, -anchorOffsetLeftX + patternSizeX / 2 - bmpSize.Width / 2, trackSizeY / 2 - bmpSize.Height / 2, 1.0f, bitmapScale, Theme.LightGreyFillColor1);
                        }

                        // Right side
                        for (int p = patternIdx + 1; p <= maxSelectedPatternIdx + patternIdxDelta && p < Song.Length; p++)
                        {
                            patternSizeX = GetPixelForNote(Song.GetPatternLength(p), false);

                            cp.FillAndDrawRectangle(anchorOffsetRightX, 0, anchorOffsetRightX + patternSizeX, trackSizeY, selectedPatternVisibleBrush, ThemeResources.BlackBrush);

                            if (bmpCopy != MiscImageIndices.Count)
                                cp.DrawBitmapAtlas(bmpAtlasMisc, (int)bmpCopy, anchorOffsetRightX + patternSizeX / 2 - bmpSize.Width / 2, trackSizeY / 2 - bmpSize.Height / 2, 1.0f, bitmapScale, Theme.LightGreyFillColor1);

                            anchorOffsetRightX += patternSizeX;
                        }

                        cp.PopTransform();
                    }
                }
            }

            // Piano roll view rect
            if (Settings.ShowPianoRollViewRange)
            {
                App.GetPianoRollViewRange(out var pianoRollMinNoteIdx, out var pianoRollMaxNoteIdx, out var pianoRollChannelIndex);

                cp.PushTranslation(GetPixelForNote(pianoRollMinNoteIdx), pianoRollChannelIndex * trackSizeY + headerSizeY);
                cp.DrawRectangle(1, patternHeaderSizeY + 1, GetPixelForNote(pianoRollMaxNoteIdx - pianoRollMinNoteIdx, false) - 1, trackSizeY - 1, ThemeResources.LightGreyFillBrush2);
                cp.PopTransform();
            }

            // Scroll bar (optional)
            if (GetScrollBarParams(out var scrollBarPosX, out var scrollBarSizeX))
            {
                cb.FillAndDrawRectangle(-1, actualSizeY, Width, Height, ThemeResources.DarkGreyFillBrush1, ThemeResources.BlackBrush);
                cb.FillAndDrawRectangle(scrollBarPosX - 1, actualSizeY, scrollBarPosX + scrollBarSizeX, Height, ThemeResources.MediumGreyFillBrush1, ThemeResources.BlackBrush);
            }

            // Seek bar
            cb.PushTranslation(seekX, 0);
            cf.FillAndDrawGeometry(seekGeometry, GetSeekBarBrush(), ThemeResources.BlackBrush);
            cb.DrawLine(0, headerSizeY, 0, actualSizeY, GetSeekBarBrush(), 3);
            cb.PopTransform();

            cb.PopTransform();
            cb.PopTransform();

            var headerRect      = new Rectangle(trackNameSizeX, 0, Width, Height);
            var patternOnlyRect = new Rectangle(trackNameSizeX, headerSizeY, Width, Height);
            var patternRect     = new Rectangle(trackNameSizeX, 0, Width, Height);

            g.DrawCommandList(ch, headerRect);
            g.DrawCommandList(cb, patternOnlyRect);
            g.DrawCommandList(cp, patternOnlyRect);
            g.DrawCommandList(cf, patternRect);
        }

        protected void RenderDebug(RenderGraphics g)
        {
#if DEBUG
            if (PlatformUtils.IsMobile)
            {
                var c = g.CreateCommandList();
                c.FillRectangle(mouseLastX - 30, mouseLastY - 30, mouseLastX + 30, mouseLastY + 30, ThemeResources.WhiteBrush);
                g.DrawCommandList(c);
            }
#endif
        }

        protected override void OnRender(RenderGraphics g)
        {
            // Happens when piano roll is maximized.
            if (Height <= 1)
            {
                g.Clear(Theme.BlackColor);
                return;
            }

            g.Clear(Theme.DarkGreyLineColor2);
           
            RenderChannelNames(g);
            RenderPatternArea(g);
            RenderDebug(g);
        }

        private bool GetScrollBarParams(out int posX, out int sizeX)
        {
            if (scrollBarThickness > 0)
            {
                GetMinMaxScroll(out _, out var maxScrollX, out _, out _);

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

        private void GetMinMaxScroll(out int minScrollX, out int maxScrollX, out int minScrollY, out int maxScrollY)
        {
            minScrollX = 0;
            maxScrollX = Math.Max(GetPixelForNote(Song.GetPatternStartAbsoluteNoteIndex(Song.Length), false) - scrollMargin, 0);

            if (PlatformUtils.IsMobile)
            {
                minScrollY = 0;
                maxScrollY = Math.Max(virtualSizeY + headerSizeY - Height, 0);
            }
            else
            {
                // No vertical scrolling on desktop.
                minScrollY = 0;
                maxScrollY = 0;
            }
        }

        private bool ClampScroll()
        {
            GetMinMaxScroll(out var minScrollX, out var maxScrollX, out var minScrollY, out var maxScrollY);

            var scrolledX = true;
            var scrolledY = true;

            if (scrollX < minScrollX) { scrollX = minScrollX; scrolledX = false; }
            if (scrollX > maxScrollX) { scrollX = maxScrollX; scrolledY = false; }
            if (scrollY < minScrollY) { scrollY = minScrollY; scrolledY = false; }
            if (scrollY > maxScrollY) { scrollY = maxScrollY; scrolledY = false; }

            return scrolledX || scrolledY;
        }

        private bool DoScroll(int deltaX, int deltaY)
        {
            scrollX -= deltaX;
            scrollY -= deltaY; 
            MarkDirty();
            return ClampScroll();
        }

        private int GetPatternIndexForCoord(int x)
        {
            var noteIdx = GetNoteForPixel(x - trackNameSizeX);

            if (noteIdx < 0 || noteIdx >= Song.GetPatternStartAbsoluteNoteIndex(Song.Length))
                return -1;

            return Song.PatternIndexFromAbsoluteNoteIndex(noteIdx);
        }

        private bool GetPatternForCoord(int x, int y, out int channelIdx, out int patternIdx, out bool inPatternHeader)
        {
            var noteIdx = GetNoteForPixel(x - trackNameSizeX);

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

        private bool HasPatternAtCoord(int x, int y)
        {
            bool inPatternZone = GetPatternForCoord(x, y, out var channelIdx, out var patternIdx, out var inPatternHeader);
            return inPatternZone && Song.Channels[channelIdx].PatternInstances[patternIdx] != null;
        }

        private int GetChannelIndexForCoord(int y)
        {
            // DROIDTODO : Take scrollY into account!
            return (y - headerSizeY) / trackSizeY;
        }

        private void GetClampedPatternForCoord(int x, int y, out int channelIdx, out int patternIdx)
        {
            patternIdx = Song.PatternIndexFromAbsoluteNoteIndex(Utils.Clamp(GetNoteForPixel(x - trackNameSizeX), 0, Song.GetPatternStartAbsoluteNoteIndex(Song.Length) - 1));
            channelIdx = Utils.Clamp((y - headerSizeY) / trackSizeY, 0, Song.Channels.Length - 1);
        }

        Rectangle GetTrackIconRect(int idx)
        {
            return new Rectangle(
                trackIconPosX,
                trackIconPosY + headerSizeY + idx * trackSizeY, 
                ScaleForMainWindow(16),
                ScaleForMainWindow(16));
        }

        Rectangle GetTrackGhostRect(int idx)
        {
            return new Rectangle(
                trackNameSizeX - ghostNoteOffsetX, 
                headerSizeY + (idx + 1) * trackSizeY - ghostNoteOffsetY - 1,
                ScaleForMainWindow(12),
                ScaleForMainWindow(12));
        }

        bool IsCoordInHeader(int x, int y)
        {
            return y < headerSizeY;
        }

        bool IsCoordIsChannelArea(int x, int y)
        {
            return x < trackNameSizeX && y > headerSizeY;
        }

        bool IsCoordInPatternArea(int x, int y)
        {
            return x > trackNameSizeX && y > headerSizeY;
        }

        private void CaptureMouse(int x, int y)
        {
            mouseLastX = x;
            mouseLastY = y;
            captureMouseX = x;
            captureMouseY = y;
            captureScrollX = scrollX;
            Capture = true;
        }

        private void StartCaptureOperation(int x, int y, CaptureOperation op)
        {
            Debug.Assert(captureOperation == CaptureOperation.None);
            CaptureMouse(x, y);
            captureOperation = op;
            captureThresholdMet = !captureNeedsThreshold[(int)op];
            captureRealTimeUpdate = captureWantsRealTimeUpdate[(int)op];
            GetClampedPatternForCoord(x, y, out captureChannelIdx, out capturePatternIdx);
        }

        private void SetSelectedChannel(int idx)
        {
            if (idx != App.SelectedChannelIndex)
                App.SelectedChannelIndex = idx;
        }

        private void ChangeChannelForCoord(int y)
        {
            SetSelectedChannel(Utils.Clamp((y - headerSizeY) / trackSizeY, 0, Song.Channels.Length - 1));
        }

        private void SetLoopPoint(int patternIdx)
        {
            App.UndoRedoManager.BeginTransaction(TransactionScope.Song, Song.Id);
            Song.SetLoopPoint(Song.LoopPoint == patternIdx ? -1 : patternIdx);
            App.UndoRedoManager.EndTransaction();
            MarkDirty();
        }

        private void CreateNewPattern(int channelIdx, int patternIdx)
        {
            var channel = Song.Channels[channelIdx];

            App.UndoRedoManager.BeginTransaction(TransactionScope.Song, Song.Id);
            channel.PatternInstances[patternIdx] = channel.CreatePattern();
            channel.InvalidateCumulativePatternCache();
            PatternClicked?.Invoke(channelIdx, patternIdx);
            App.UndoRedoManager.EndTransaction();

            ClearSelection();
            MarkDirty();
        }

        private void DeletePattern(int channelIdx, int patternIdx)
        {
            var channel = Song.Channels[channelIdx];
            var pattern = channel.PatternInstances[patternIdx];

            App.UndoRedoManager.BeginTransaction(TransactionScope.Song, Song.Id);
            patternCache.Remove(pattern);
            channel.PatternInstances[patternIdx] = null;
            channel.InvalidateCumulativePatternCache();
            PatternModified?.Invoke();
            App.UndoRedoManager.EndTransaction();

            ClearSelection();
            MarkDirty();
        }

        private bool HandleMouseDownPan(MouseEventArgs e)
        {
            bool middle = e.Button.HasFlag(MouseButtons.Middle) || (e.Button.HasFlag(MouseButtons.Left) && ModifierKeys.HasFlag(Keys.Alt));

            if (middle)
            {
                panning = true;
                CaptureMouse(e.X, e.Y);
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
                }
                else if (x > (scrollBarPosX + scrollBarSizeX))
                {
                    scrollX += (Width - trackNameSizeX);
                    ClampScroll();
                }
                else if (x >= scrollBarPosX && x <= (scrollBarPosX + scrollBarSizeX))
                {
                    StartCaptureOperation(e.X, e.Y, CaptureOperation.ScrollBar);
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
                    if (left)
                        App.ToggleChannelActive(trackIcon);
                    else
                        App.ToggleChannelSolo(trackIcon);

                    return true;
                }
                else if (ghostIcon >= 0)
                {
                    App.ToggleChannelGhostNotes(ghostIcon);
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
                var patternIdx = GetPatternIndexForCoord(e.X);
                if (patternIdx >= 0)
                    SetLoopPoint(patternIdx);
                return true;
            }

            return false;
        }

        private bool HandleMouseDownSeekBar(MouseEventArgs e)
        {
            if (IsMouseInHeader(e) && e.Button.HasFlag(MouseButtons.Left))
            {
                StartCaptureOperation(e.X, e.Y, CaptureOperation.DragSeekBar);
                UpdateSeekDrag(e.X, false);
                return true;
            }

            return false;
        }

        private bool HandleMouseDownHeaderSelection(MouseEventArgs e)
        {
            if (IsMouseInHeader(e) && e.Button.HasFlag(MouseButtons.Right))
            {
                StartCaptureOperation(e.X, e.Y, CaptureOperation.Select);
                UpdateSelection(e.X, e.Y, true);
                return true;
            }

            return false;
        }

        private bool HandleMouseDownChannelChange(MouseEventArgs e)
        {
            if (e.Y > headerSizeY && e.Y < Height - scrollBarThickness && e.Button.HasFlag(MouseButtons.Left))
                ChangeChannelForCoord(e.Y);

            // Does not prevent from processing other events.
            return false;
        }

        private bool HandleMouseDownAltZoom(MouseEventArgs e)
        {
            if (e.Button.HasFlag(MouseButtons.Right) && ModifierKeys.HasFlag(Keys.Alt) && GetPatternForCoord(e.X, e.Y, out _, out _, out _))
            {
                StartCaptureOperation(e.X, e.Y, CaptureOperation.AltZoom);
                return true;
            }

            return false;
        }
        
        private void StartDragSelection(int x, int y, int patternIdx)
        {
            selectionDragAnchorPatternIdx = patternIdx;
            selectionDragAnchorPatternXFraction = (x - trackNameSizeX + scrollX - GetPixelForNote(Song.GetPatternStartAbsoluteNoteIndex(patternIdx), false)) / (float)GetPixelForNote(Song.GetPatternLength(patternIdx), false);
            StartCaptureOperation(x, y, CaptureOperation.DragSelection);
        }

        private bool HandleMouseDownPatternArea(MouseEventArgs e)
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
                        CreateNewPattern(channelIdx, patternIdx);
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

                        StartDragSelection(e.X, e.Y, patternIdx);
                    }

                    return true;
                }
                else if (right && inPatternHeader && pattern != null)
                {
                    DeletePattern(channelIdx, patternIdx);
                    return true;
                }
                else if (right)
                {
                    StartCaptureOperation(e.X, e.Y, CaptureOperation.Select);
                    UpdateSelection(e.X, e.Y, true);
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
            if (HandleMouseDownPatternArea(e)) goto Handled;
            return;

        Handled:
            MarkDirty();
        }

        private bool HandleTouchDownDragSeekBar(int x, int y)
        {
            if (IsCoordInHeader(x, y))
            {
                var seekX = GetPixelForNote(App.CurrentFrame) + trackNameSizeX;

                // See if we are close enough to the yellow triangle.
                if (Math.Abs(seekX - x) < headerSizeY * 2)
                {
                    StartCaptureOperation(x, y, CaptureOperation.DragSeekBar);
                    UpdateSeekDrag(x, false);
                    return true;
                }
            }

            return false;
        }

        private bool HandleTouchDownHeaderSelection(int x, int y)
        {
            if (IsCoordInHeader(x, y))
            {
                // If pattern column is already select, dont start another selection
                // since we will want the longpress to be processed.
                var patternIdx = GetPatternIndexForCoord(x);

                if (patternIdx >= 0 && !IsPatternColumnSelected(patternIdx))
                {
                    StartCaptureOperation(x, y, CaptureOperation.Select);
                    return true;
                }
            }

            return false;
        }

        private bool HandleTouchDownPan(int x, int y)
        {
            if (IsCoordInPatternArea(x, y))
            {
                StartCaptureOperation(x, y, CaptureOperation.MobilePan);
                return true;
            }

            return false;
        }

        private bool HandleTouchDownPatternArea(int x, int y)
        {
            if (IsCoordInPatternArea(x, y))
            {
                bool inPatternZone = GetPatternForCoord(x, y, out var channelIdx, out var patternIdx, out _);

                if (inPatternZone && hoverChannelIdx == channelIdx && hoverPatternIdx == patternIdx)
                {
                    if (!IsPatternSelected(hoverChannelIdx, hoverPatternIdx))
                        SetSelection(hoverChannelIdx, hoverChannelIdx, hoverPatternIdx, hoverPatternIdx);

                    StartDragSelection(x, y, patternIdx);
                    return true;
                }
            }

            return false;
        }

        private bool HandleTouchClickChannelChange(int x, int y)
        {
            if (IsCoordIsChannelArea(x, y))
            {
                ChangeChannelForCoord(y);
                return true;
            }

            return false;
        }

        private bool HandleTouchClickPatternHeader(int x, int y)
        {
            if (IsCoordInHeader(x, y))
            {
                App.SeekSong(GetNoteForPixel(x - trackNameSizeX));
                return true;
            }

            return false;
        }

        private bool HandleTouchClickPatternArea(int x, int y)
        {
            bool inPatternZone = GetPatternForCoord(x, y, out var channelIdx, out var patternIdx, out var inPatternHeader);

            if (inPatternZone)
            {
                var channel = Song.Channels[channelIdx];
                var pattern = channel.PatternInstances[patternIdx];

                SetHoverPattern(channelIdx, patternIdx);

                // DROIDTODO : Check if delete tool.
                if (pattern == null)
                {
                    CreateNewPattern(channelIdx, patternIdx);
                    SetSelection(channelIdx, channelIdx, patternIdx, patternIdx);
                }
                else
                {
                    if (!IsSelectionValid() || 
                        (minSelectedChannelIdx != channelIdx ||
                         maxSelectedChannelIdx != channelIdx ||
                         minSelectedPatternIdx != patternIdx ||
                         maxSelectedPatternIdx != patternIdx))
                    {
                        SetSelection(channelIdx, channelIdx, patternIdx, patternIdx);
                    }
                    else 
                    {
                        ClearSelection();
                    }
                }

                return true;
            }

            return false;
        }

        private bool HandleTouchLongPressChannelName(int x, int y)
        {
            if (IsCoordIsChannelArea(x, y))
            {
                var channelIdx = GetChannelIndexForCoord(y);

                const int ResultToggleMute = 0;
                const int ResultToggleSolo = 1;

                App.ShowContextMenu(new[]
                {
                    new ContextMenuOption("", App.IsChannelActive(channelIdx) ? "Mute Channel"   : "Unmute Channel", ResultToggleMute),
                    new ContextMenuOption("", App.IsChannelSolo(channelIdx)   ? "Unsolo Channel" : "Solo Channel",   ResultToggleSolo)
                },
                (i) =>
                {
                    if (i == ResultToggleMute)
                        App.ToggleChannelActive(channelIdx);
                    else if (i == ResultToggleSolo)
                        App.ToggleChannelSolo(channelIdx);
                });

                return true;
            }

            return false;
        }

        private bool HandleTouchLongPressHeader(int x, int y)
        {
            if (IsCoordInHeader(x, y))
            {
                var patternIdx = GetPatternIndexForCoord(x);

                if (patternIdx >= 0)
                {
                    const int ResultSetLoopPoint   = 0;
                    const int ResultCustumSettings = 1;

                    App.ShowContextMenu(new[]
                    {
                        new ContextMenuOption("", Song.LoopPoint == patternIdx ? "Clear Loop Point" : "Set Loop Point", ResultSetLoopPoint),
                        new ContextMenuOption("", "Custom Pattern Settings...", ResultCustumSettings)
                    }, 
                    (i) =>
                    {
                        if (i == ResultSetLoopPoint)
                            SetLoopPoint(patternIdx);
                        else if (i == ResultCustumSettings)
                            EditPatternCustomSettings(Point.Empty, patternIdx);
                    });
                }

                return true;
            }

            return false;
        }

        private bool HandleTouchLongPressPatternArea(int x, int y)
        {
            bool inPatternZone = GetPatternForCoord(x, y, out var channelIdx, out var patternIdx, out var inPatternHeader);

            if (inPatternZone)
            {
                var channel = Song.Channels[channelIdx];
                var pattern = channel.PatternInstances[patternIdx];

                SetHoverPattern(channelIdx, patternIdx);

                var menu = new List<ContextMenuOption>(); ;

                const int ResultPastePatternInstances = 0;
                const int ResultPastePatternCopies    = 1;
                const int ResultExpandSelection       = 2;
                const int ResultDeleteHover           = 3;
                const int ResultDeleteSelection       = 4;
                const int ResultHoverProperties       = 5;
                const int ResultSelectionProperties   = 6;

                // DROIDTODO : This isnt right for instances, etc.
                if (ClipboardUtils.ConstainsPatterns)
                {
                    menu.Add(new ContextMenuOption("", "Paste Instances", ResultPastePatternInstances));
                    menu.Add(new ContextMenuOption("", "Paste Copies",    ResultPastePatternCopies));
                }

                if (IsSelectionValid() && !IsPatternSelected(channelIdx, patternIdx))
                {
                    menu.Add(new ContextMenuOption("", "Expand Selection", ResultExpandSelection));
                }

                if (pattern != null)
                {
                    if (IsPatternSelected(channelIdx, patternIdx) && SelectionContainsMultiplePatterns())
                    {
                        menu.Add(new ContextMenuOption("", "Delete selection", ResultDeleteSelection));
                        menu.Add(new ContextMenuOption("", "Selected Patterns Properties...", ResultSelectionProperties));
                    }
                    else
                    {
                        menu.Add(new ContextMenuOption("", "Delete pattern", ResultDeleteHover));
                        menu.Add(new ContextMenuOption("", "Pattern Properties...", ResultHoverProperties));
                    }
                }

                if (menu.Count > 0)
                {
                    App.ShowContextMenu(menu.ToArray(), (i) =>
                    {
                        if (i == ResultPastePatternInstances)
                            ; // DROIDTODO : Copy/paste!
                        else if (i == ResultPastePatternCopies)
                            ; // DROIDTODO : Copy/paste!
                        else if (i == ResultExpandSelection)
                            EnsureSelectionInclude(channelIdx, patternIdx);
                        else if (i == ResultDeleteHover)
                            DeletePattern(channelIdx, patternIdx);
                        else if (i == ResultDeleteSelection)
                            DeleteSelection(true);
                        else if (i == ResultHoverProperties)
                            EditPatternProperties(Point.Empty, pattern, false);
                        else if (i == ResultSelectionProperties)
                            EditPatternProperties(Point.Empty, pattern, true);
                    });
                }

                return true;
            }

            return false;
        }

        protected override void OnTouchDown(int x, int y)
        {  
            // Pattern names/area:
            // - Set current channel
            // Header:
            // - Start seek drag (if click on the seek bar)
            // - Start selection (if not click on seek bar) => Will be a bit tricky, how to tell click/down appart? Use threshold?
            // Pattern area:
            // - Nothing if pencil mode
            // - Start erase op if eraser
            // - Start selection if seleciton mode

            flingVelX = 0;
            flingVelY = 0;

            if (HandleTouchDownPatternArea(x, y)) goto Handled;
            if (HandleTouchDownDragSeekBar(x, y)) goto Handled;
            if (HandleTouchDownHeaderSelection(x, y)) goto Handled;
            if (HandleTouchDownPan(x, y)) goto Handled;
            return;

        Handled:
            MarkDirty();
        }

        protected override void OnTouchMove(int x, int y)
        {
            UpdateCaptureOperation(x, y, false);

            mouseLastX = x;
            mouseLastY = y;
        }

        protected override void OnTouchUp(int x, int y)
        {
            EndCaptureOperation(x, y);
        }

        protected override void OnTouchFling(int x, int y, float velX, float velY)
        {
            EndCaptureOperation(x, y);
            flingVelX = velX;
            flingVelY = velY;
        }

        protected override void OnTouchScale(int x, int y, float scale, TouchScalePhase phase)
        {
            // Header/Pattern area:
            // - Scale

            if (captureOperation != CaptureOperation.None)
            {
                AbortCaptureOperation(); // Temporary.
                Debug.WriteLine("Oops");
            }

            // TODO : Capture operation.
            switch (phase)
            {
                case TouchScalePhase.Begin:
                    panning = false; // MATTT This will not be used on mobile.
                    StartCaptureOperation(x, y, CaptureOperation.MobileZoom);
                    break;
                case TouchScalePhase.Scale:
                    ZoomAtLocation(x, scale); // MATTT : Center is stuck at the initial position.
                    break;
                case TouchScalePhase.End:
                    EndCaptureOperation(x, y);
                    break;
            }
        }

        protected override void OnTouchClick(int x, int y)
        {
            // Header: 
            // - Seek
            // Pattern names:
            // x Set current channel (NO WILL DO ON DOWN)
            // - Toggle mute on icon
            // - Force diplay? How small is the icon going to be?
            // Pattern area:
            // - Add pattern if empty (if pencil tool)
            // - Select single pattern (if pencil tool)
            // - 

            if (HandleTouchClickChannelChange(x, y)) goto Handled;
            if (HandleTouchClickPatternHeader(x, y)) goto Handled;
            if (HandleTouchClickPatternArea(x, y)) goto Handled;

            return;

        Handled:
            MarkDirty();
        }

        protected override void OnTouchLongPress(int x, int y)
        {
            // Header:
            // - Context menu : seet loop point, custom settings
            // Pattern names:
            // - Context menu : Mute/Unmute, Toggle force display, etc. (click on icon???)
            // Pattern area:
            // - Context menu : Pattern properties, etc. (if in selection)

            AbortCaptureOperation();

            if (HandleTouchLongPressChannelName(x, y)) goto Handled;
            if (HandleTouchLongPressHeader(x, y)) goto Handled;
            if (HandleTouchLongPressPatternArea(x, y)) goto Handled;

            return;

        Handled:
            MarkDirty();
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
            MarkDirty();
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
            dialog.Properties.AddNumericUpDown("Repeat :", 1, 1, 32); // 2
            dialog.Properties.SetPropertyEnabled(1, false);
            dialog.Properties.PropertyChanged += PasteSpecialDialog_PropertyChanged;
            dialog.Properties.Build();

            dialog.ShowDialog(ParentForm, (r) =>
            {
                if (r == DialogResult.OK)
                {
                    PasteInternal(
                        dialog.Properties.GetPropertyValue<bool>(0),
                        dialog.Properties.GetPropertyValue<bool>(1),
                        dialog.Properties.GetPropertyValue<int> (2));
                }
            });
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

        private void EndDragSelection(int x, int y)
        {
            if (captureThresholdMet)
            {
                if (!IsSelectionValid()) // No clue how we end up here with invalid selection.
                {
                    CancelDragSelection();
                }
                else
                {
                    var noteIdx = GetNoteForPixel(x - trackNameSizeX);

                    if (noteIdx >= 0 && noteIdx < Song.GetPatternStartAbsoluteNoteIndex(Song.Length))
                    {
                        var patternIdx = Song.PatternIndexFromAbsoluteNoteIndex(GetNoteForPixel(x - trackNameSizeX));
                        var patternIdxDelta = patternIdx - selectionDragAnchorPatternIdx;
                        var tmpPatterns = GetSelectedPatterns(out var customSettings);

                        var dragChannelIdxStart = GetChannelIndexForCoord(captureMouseY);
                        var dragChannelIdxCurrent = GetChannelIndexForCoord(y);
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
                                            duplicatedPattern.Color = Theme.RandomCustomColor();
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
                        Song.DeleteNotesPastMaxInstanceLength();
                        Song.InvalidateCumulativePatternCache();

                        App.UndoRedoManager.EndTransaction();

                        minSelectedChannelIdx = Utils.Clamp(minSelectedChannelIdx + channelIdxDelta, 0, Song.Channels.Length - 1);
                        maxSelectedChannelIdx = Utils.Clamp(maxSelectedChannelIdx + channelIdxDelta, 0, Song.Channels.Length - 1);
                        minSelectedPatternIdx = Utils.Clamp(minSelectedPatternIdx + patternIdxDelta, 0, Song.Length - 1);
                        maxSelectedPatternIdx = Utils.Clamp(maxSelectedPatternIdx + patternIdxDelta, 0, Song.Length - 1);

                        if (HasHoverPattern())
                        {
                            hoverChannelIdx = Utils.Clamp(hoverChannelIdx + channelIdxDelta, 0, Song.Channels.Length - 1);
                            hoverPatternIdx = Utils.Clamp(hoverPatternIdx + patternIdxDelta, 0, Song.Length - 1);
                        }

                        MarkDirty();
                        PatternModified?.Invoke();
                        SelectionChanged?.Invoke();
                    }
                }
            }
        }

        private void EndCaptureOperation(int x, int y)
        {
            if (captureOperation != CaptureOperation.None)
            {
                switch (captureOperation)
                {
                    case CaptureOperation.DragSelection:
                        EndDragSelection(x, y);
                        break;
                    case CaptureOperation.DragSeekBar:
                        UpdateSeekDrag(x, true);
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
                EndCaptureOperation(e.X, e.Y);

            UpdateCursor();
        }

        private void AbortCaptureOperation()
        {
            if (captureOperation != CaptureOperation.None)
            {
                if (App.UndoRedoManager.HasTransactionInProgress)
                    App.UndoRedoManager.AbortTransaction();

                Capture = false;
                panning = false;
                captureOperation = CaptureOperation.None;

                MarkDirty();
            }
            else
            {
                Debug.Assert(!App.UndoRedoManager.HasTransactionInProgress);
            }
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
                MarkDirty();
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
                MarkDirty();
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
                MarkDirty();
            }

            UpdateToolTip(null);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            if (captureOperation == CaptureOperation.DragSelection)
            {
                UpdateCursor();
                MarkDirty();
            }

            UpdateToolTip(null);
        }

        private void UpdateSeekDrag(int mouseX, bool final)
        {
            ScrollIfNearEdge(mouseX);

            dragSeekPosition = GetNoteForPixel(mouseX - trackNameSizeX);

            if (final)
                App.SeekSong(dragSeekPosition);

            MarkDirty();
        }

        private void ScrollIfNearEdge(int mouseX)
        {
            if ((mouseX - trackNameSizeX) < 0)
            {
                var scrollAmount = Utils.Clamp((trackNameSizeX - mouseX) / (float)trackNameSizeX, 0.0f, 1.0f);
                scrollX -= (int)(App.AverageTickRate * ScrollSpeedFactor * scrollAmount);
                ClampScroll();
            }
            else if ((Width - mouseX) < trackNameSizeX)
            {
                var scrollAmount = Utils.Clamp((mouseX - (Width - trackNameSizeX)) / (float)trackNameSizeX, 0.0f, 1.0f);
                scrollX += (int)(App.AverageTickRate * ScrollSpeedFactor * scrollAmount);
                ClampScroll();
            }
        }

        private void UpdateSelection(int x, int y, bool first = false)
        {
            ScrollIfNearEdge(x);

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
                GetClampedPatternForCoord(x, y, out var channelIdx, out var patternIdx);

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

            MarkDirty();
            SelectionChanged?.Invoke();
        }

        private void UpdateDragSelection(int x, int y)
        {
            ScrollIfNearEdge(x);
            MarkDirty();
        }

        private void UpdateAltZoom(int x, int y)
        {
            var deltaY = y - captureMouseY;

            if (Math.Abs(deltaY) > 50)
            {
                ZoomAtLocation(x, deltaY < 0.0f ? 2.0f : 0.5f);
                captureMouseY = y;
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
                    int idx = GetChannelIndexForCoord(e.Y) + 1;
                    if (idx >= 1 && idx <= 12)
                        tooltip += $" {{Ctrl}} {{F{idx}}}";
                }
                else
                {
                    tooltip = "{MouseLeft} Make channel active";
                    int idx = GetChannelIndexForCoord(e.Y) + 1;
                    if (idx >= 1 && idx <= 12)
                        tooltip += $" {{F{idx}}}";
                }
            }

            App.SetToolTip(tooltip);
        }

        private void UpdateScrollBarX(int x, int y)
        {
            GetScrollBarParams(out _, out var scrollBarSizeX);
            GetMinMaxScroll(out _, out var maxScrollX, out _, out _);
            int scrollAreaSizeX = Width - trackNameSizeX;
            scrollX = (int)Math.Round(captureScrollX + ((x - captureMouseX) / (float)(scrollAreaSizeX - scrollBarSizeX) * maxScrollX));
            ClampScroll();
            MarkDirty();
        }

        private void UpdateCaptureOperation(int x, int y, bool realTime)
        {
            const int CaptureThreshold = PlatformUtils.IsDesktop ? 5 : 50;

            if (captureOperation != CaptureOperation.None && !captureThresholdMet)
            {
                if (Math.Abs(x - captureMouseX) >= CaptureThreshold ||
                    Math.Abs(y - captureMouseY) >= CaptureThreshold)
                {
                    captureThresholdMet = true;
                }
            }

            if (captureOperation != CaptureOperation.None && captureThresholdMet && (captureRealTimeUpdate || !realTime))
            {
                switch (captureOperation)
                {
                    case CaptureOperation.Select:
                        UpdateSelection(x, y);
                        break;
                    case CaptureOperation.AltZoom:
                        UpdateAltZoom(x, y);
                        break;
                    case CaptureOperation.DragSeekBar:
                        UpdateSeekDrag(x, false);
                        break;
                    case CaptureOperation.ScrollBar:
                        UpdateScrollBarX(x, y);
                        break;
                    case CaptureOperation.MobilePan:
                        DoScroll(x - mouseLastX, y - mouseLastY);
                        break;
                    case CaptureOperation.DragSelection:
                        UpdateDragSelection(x, y);
                        break;
                    default:
                        MarkDirty();
                        break;
                }
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            bool middle = e.Button.HasFlag(MouseButtons.Middle) || (e.Button.HasFlag(MouseButtons.Left) && ModifierKeys.HasFlag(Keys.Alt));

            UpdateCursor();
            UpdateCaptureOperation(e.X, e.Y, false);

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

            dlg.ShowDialog(ParentForm, (r) =>
            {
                if (r == DialogResult.OK)
                {
                    App.UndoRedoManager.BeginTransaction(TransactionScope.Song, song.Id);
                    tempoProperties.Apply(dlg.Properties.GetPropertyValue<bool>(0));
                    App.UndoRedoManager.EndTransaction();
                    MarkDirty();
                    PatternModified?.Invoke();
                }
            });
        }

        private void PatternCustomSettings_PropertyChanged(PropertyPage props, int propIdx, int rowIdx, int colIdx, object value)
        {
            if (propIdx == 0)
            {
                var tempoProperties = props.PropertiesUserData as TempoProperties;
                tempoProperties.EnableProperties((bool)value);
            }
        }

        private void EditPatternProperties(Point pt, Pattern pattern, bool selection = true)
        {
            bool multiplePatternSelected = selection && ((maxSelectedChannelIdx != minSelectedChannelIdx) || (minSelectedPatternIdx != maxSelectedPatternIdx));

            var dlg = new PropertyDialog(PointToScreen(pt), 240);
            dlg.Properties.AddColoredTextBox(pattern.Name, pattern.Color);
            dlg.Properties.SetPropertyEnabled(0, !multiplePatternSelected);
            dlg.Properties.AddColorPicker(pattern.Color);
            dlg.Properties.Build();

            dlg.ShowDialog(ParentForm, (r) =>
            {
                if (r == DialogResult.OK)
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
                    else if (App.SelectedChannel.RenamePattern(pattern, newName))
                    {
                        pattern.Color = newColor;
                        App.UndoRedoManager.EndTransaction();
                    }
                    else
                    {
                        App.UndoRedoManager.AbortTransaction();
                        SystemSounds.Beep.Play();
                    }

                    MarkDirty();
                    PatternModified?.Invoke();
                }
            });
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

        private void ZoomAtLocation(int x, float scale)
        {
            if (scale == 1.0f)
                return;

            // When continuously following, zoom at the seek bar location.
            if (continuouslyFollowing)
                x = (int)(Width * ContinuousFollowPercent);

            Debug.Assert(PlatformUtils.IsMobile || scale == 0.5f || scale == 2.0f);

            var pixelX = x - trackNameSizeX;
            var absoluteX = pixelX + scrollX;
            var prevNoteSizeX = noteSizeX;

            zoom *= scale;
            zoom = Utils.Clamp(zoom, MinZoom, MaxZoom);

            // This will update the noteSizeX.
            UpdateRenderCoords();

            absoluteX = (int)Math.Round(absoluteX * (noteSizeX / (double)prevNoteSizeX));
            scrollX = absoluteX - pixelX;

            ClampScroll();
            MarkDirty();
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
                MarkDirty();
            }
            else
            {
                ZoomAtLocation(e.X, e.Delta < 0.0f ? 0.5f : 2.0f);
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
            MarkDirty();
        }

        protected bool EnsureSeekBarVisible(float percent = ContinuousFollowPercent)
        {
            var seekX = GetPixelForNote(App.CurrentFrame);
            var minX = 0;
            var maxX = (int)((Width * percent) - trackNameSizeX);

            // Keep everything visible 
            if (seekX < minX)
                scrollX -= (minX - seekX);
            else if (seekX > maxX)
                scrollX += (seekX - maxX);

            ClampScroll();

            seekX = GetPixelForNote(App.CurrentFrame);
            return seekX == maxX;
        }

        public void UpdateFollowMode(bool force = false)
        {
            continuouslyFollowing = false;

            if ((App.IsPlaying || force) && App.FollowModeEnabled && Settings.FollowSync != Settings.FollowSyncPianoRoll && !panning && captureOperation == CaptureOperation.None)
            {
                var frame = App.CurrentFrame;
                var seekX = GetPixelForNote(App.CurrentFrame);

                if (Settings.FollowMode == Settings.FollowModeJump)
                {
                    var maxX = Width - trackNameSizeX;
                    if (seekX < 0 || seekX > maxX)
                        scrollX = GetPixelForNote(frame, false);
                }
                else
                {
                    continuouslyFollowing = EnsureSeekBarVisible();
                }

                ClampScroll();
            }
        }

        private void TickFling(float delta)
        {
            if (flingVelX != 0.0f ||
                flingVelY != 0.0f)
            {
                var deltaPixelX = (int)Math.Round(flingVelX * delta);
                var deltaPixelY = (int)Math.Round(flingVelY * delta);

                if ((deltaPixelX != 0 || deltaPixelY != 0) && DoScroll(deltaPixelX, deltaPixelY))
                {
                    flingVelX *= (float)Math.Exp(delta * -4.5f);
                    flingVelY *= (float)Math.Exp(delta * -4.5f);
                }
                else
                {
                    flingVelX = 0.0f;
                    flingVelY = 0.0f;
                }
            }
        }

        public void Tick(float delta)
        {
            if (App == null)
                return;

            UpdateCaptureOperation(mouseLastX, mouseLastY, true);
            UpdateFollowMode();
            TickFling(delta);
        }

        public void SongModified()
        {
            UpdateRenderCoords();
            InvalidatePatternCache();
            ClearSelection();
            ClampScroll();
            MarkDirty();
        }

        public void InvalidatePatternCache()
        {
            if (patternCache != null)
                patternCache.Clear();
            MarkDirty();
        }

        public void SerializeState(ProjectBuffer buffer)
        {
            buffer.Serialize(ref scrollX);
            buffer.Serialize(ref zoom);
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
                MarkDirty();
            }
        }
    }
}
