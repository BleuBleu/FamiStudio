using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace FamiStudio
{
    // This and the Piano Roll are the only 2 "Uber-Control" left, where everything is in one class with 
    // no sub-widgets whatsoever. Both of these need a full rewrite. This is for historical reason, when
    // the app started, we didnt have a proper widget system, we sort-of do now. Also, all the touch and
    // mouse input would need to be unified as much as possible.
    //
    // The sequencer would need to be broken down into a timeline, a pattern area, the channel names and
    // associated icons needs to be made into real buttons, etc.
    public class Sequencer : Container
    {
        const int DefaultChannelNameSizeX    = Platform.IsMobile ? 64 : 94;
        const int DefaultHeaderSizeY         = 17;
        const int DefaultPatternHeaderSizeY  = 13;
        const int DefaultBarTextPosY         = 2;
        const int DefaultChannelIconPosX     = 2;
        const int DefaultChannelIconPosY     = 3;
        const int DefaultChannelNamePosX     = 21;
        const int DefaultGhostNoteOffsetX    = Platform.IsMobile ? 12 : 16;
        const int DefaultGhostNoteOffsetY    = Platform.IsMobile ? 16 : 15;
        const int DefaultPatternNamePosX     = 2;
        const int DefaultHeaderIconPosX      = 3;
        const int DefaultHeaderIconPosY      = 3;
        const int DefaultScrollBarThickness1 = 10;
        const int DefaultScrollBarThickness2 = 16;
        const int DefaultMinScrollBarLength  = 64;
        const float ScrollSpeedFactor        = Platform.IsMobile ? 2.0f : 1.0f;
        const float DefaultZoom              = Platform.IsMobile ? 0.5f : 2.0f;

        const float MinZoom = 0.25f;
        const float MaxZoom = 16.0f;

        int channelNameSizeX;
        int headerSizeY;
        int channelSizeY;
        int patternHeaderSizeY;
        int scrollMargin;
        int barTextPosY;  
        int channelIconPosX;   
        int channelIconPosY;   
        int channelNamePosX;   
        int ghostNoteOffsetX;
        int ghostNoteOffsetY;
        int patternNamePosX;
        int headerIconPosX;
        int headerIconPosY;
        int scrollBarThickness;
        int minScrollBarLength;
        int virtualSizeY;
        int numRows;
        float noteSizeX;
        bool allowVerticalScrolling;

        int scrollX = 0;
        int scrollY = 0;
        int mouseLastX = 0;
        int mouseLastY = 0;
        int captureMouseX = -1;
        int captureMouseY = -1;
        int captureScrollX = -1;
        int captureScrollY = -1;
        int captureChannelIdx = -1;
        int captureRowIdx = -1;
        int capturePatternIdx = -1;
        int dragSeekPosition = -1;
        int selectionDragAnchorPatternIdx = -1;
        float zoom = DefaultZoom;
        float bitmapScale = 1.0f;
        float channelBitmapScale = 1.0f;
        float flingVelX;
        float flingVelY;
        float selectionDragAnchorPatternXFraction = -1.0f;
        CaptureOperation captureOperation = CaptureOperation.None;
        bool panning = false;
        bool canFling = false;
        bool continuouslyFollowing = false;
        bool captureThresholdMet = false;
        bool mouseMovedDuringCapture = false;
        bool captureRealTimeUpdate = false;
        bool showExpansionIcons = false;
        bool timeOnlySelection = false;
        bool hideEmptyChannels = false;
        bool forceShyOff;
        bool[] channelVisible;
        int[] channelToRow;
        int[] rowToChannel;
        PatternLocation selectionMin = PatternLocation.Invalid;
        PatternLocation selectionMax = PatternLocation.Invalid;
        PatternLocation highlightLocation = PatternLocation.Invalid;

        // Hover tracking
        int hoverRow = -1;
        int hoverPattern = -1;
        int hoverIconMask = 0;
        bool hoverShy = false;

        PatternBitmapCache patternCache;

        Color selectedPatternVisibleColor   = Color.FromArgb(64, Theme.LightGreyColor1);
        Color selectedPatternInvisibleColor = Color.FromArgb(16, Theme.LightGreyColor1);

        float[] seekGeometry;
        TextureAtlasRef[] bmpExpansions;
        TextureAtlasRef[] bmpChannels;
        TextureAtlasRef bmpForceDisplay;
        TextureAtlasRef bmpLoopPoint;
        TextureAtlasRef bmpInstantiate;
        TextureAtlasRef bmpDuplicate;
        TextureAtlasRef bmpDuplicateMove;
        TextureAtlasRef bmpShyOn;
        TextureAtlasRef bmpShyOff;

        enum CaptureOperation
        {
            None,
            SelectColumn,
            SelectRectangle,
            DragSelection,
            AltZoom,
            DragSeekBar,
            ScrollBarX,
            ScrollBarY,
            MobileZoom,
            MobilePan,
        }

        static readonly bool[] captureNeedsThreshold = new[]
        {
            false, // None
            Platform.IsMobile,  // SelectColumn
            false, // SelectRectangle
            true,  // DragSelection
            false, // AltZoom
            false, // DragSeekBar
            false, // ScrollBarX
            false, // ScrollBarY
            false, // MobileZoom,
            false, // MobilePan,
        };

        static readonly bool[] captureWantsRealTimeUpdate = new[]
        {
            false, // None
            true,  // SelectColumn
            true,  // SelectRectangle
            true,  // DragSelection
            false, // AltZoom
            true,  // DragSeekBar
            false, // ScrollBarX
            false, // ScrollBarY
            false, // MobileZoom,
            false, // MobilePan,
        };

        public delegate void PatternClickedDelegate(int channelIdx, int patternIdx, bool setActive);
        public delegate void ChannelDelegate(int channelIdx);
        public delegate void EmptyDelegate();

        public event PatternClickedDelegate PatternClicked;
        public event EmptyDelegate PatternModified;
        public event EmptyDelegate PatternsPasted;
        public event EmptyDelegate SelectionChanged;
        public event EmptyDelegate ShyChanged;

        #region Localization

        // Tooltips
        LocalizedString AddPatternTooltip;
        LocalizedString SetLoopPointTooltip;
        LocalizedString PanTooltip;
        LocalizedString SelectRectangleTooltip;
        LocalizedString OrTooltip;
        LocalizedString DeletePatternTooltip;
        LocalizedString MoreOptionsTooltip;
        LocalizedString MovePatternTooltip;
        LocalizedString ClonePatternTooltip;
        LocalizedString ShyModeTooltip;
        LocalizedString SeekTooptip;
        LocalizedString SelectColumnTooltip;
        LocalizedString MuteChannelTooltip;
        LocalizedString SoloChannelTooltip;
        LocalizedString ForceDisplayTooltip;
        LocalizedString ForceDisplayAllChannelsTooltip;
        LocalizedString MakeActiveTooltip;

        // Context menus
        LocalizedString ToggleMuteLabel;
        LocalizedString ToggleSoloLabel;
        LocalizedString ForceDisplayLabel;
        LocalizedString ClearLoopPointLabel;
        LocalizedString SetLoopPointLabel;
        LocalizedString CustomPatternSettingsLabel;
        LocalizedString GoToPianoRollLabel;
        LocalizedString ExpandSelectionLabel;
        LocalizedString InstanciateHereLabel;
        LocalizedString DuplicateHereLabel;
        LocalizedString ClearSelectionLabel;
        LocalizedString DeleteSelectionLabel;
        LocalizedString SelectedPatternPropertiesLabel;
        LocalizedString PatternPropertiesLabel;
        LocalizedString DeletePatternLabel;

        // Dialogs
        LocalizedString PasteTitle;
        LocalizedString PasteMissingInstrumentsMessage;
        LocalizedString PasteMissingArpeggiosMessage;
        LocalizedString PasteMissingSamplesMessage;

        // Paste special dialog
        LocalizedString PasteSpecialTitle;
        LocalizedString InsertLabel;
        LocalizedString InsertTooltip;
        LocalizedString ExtendSongLabel;
        LocalizedString ExtendSongTooltip;
        LocalizedString RepeatLabel;
        LocalizedString RepeatTooltip;

        //Custom pattern settings dialog
        LocalizedString CustomPatternLabel;
        LocalizedString CustomPatternTitle;
        LocalizedString CustomPatternTooltip;

        // Pattern properties dialog
        LocalizedString PatternPropertiesTitle;
        LocalizedString MultiplePatternsSelectedLabel;
        LocalizedString ErrorRenamingPattern;

        #endregion

        public Sequencer()
        {
            Localization.Localize(this);
            SetTickEnabled(true);
            supportsLongPress = true;
            supportsDoubleClick = true;
        }

        private Song Song
        {
            get { return App?.SelectedSong; }
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

            RebuildChannelMap();
            ComputeDesiredSizeY(out var unscaledChannelSizeY, out allowVerticalScrolling);

            channelNameSizeX   = DpiScaling.ScaleForWindow(DefaultChannelNameSizeX);
            headerSizeY        = DpiScaling.ScaleForWindow(DefaultHeaderSizeY);
            channelSizeY       = DpiScaling.ScaleForWindow(unscaledChannelSizeY);
            barTextPosY        = DpiScaling.ScaleForWindow(DefaultBarTextPosY);
            channelIconPosX    = DpiScaling.ScaleForWindow(DefaultChannelIconPosX);
            channelIconPosY    = DpiScaling.ScaleForWindow(DefaultChannelIconPosY);
            channelNamePosX    = DpiScaling.ScaleForWindow(DefaultChannelNamePosX);
            ghostNoteOffsetX   = DpiScaling.ScaleForWindow(DefaultGhostNoteOffsetX);
            ghostNoteOffsetY   = DpiScaling.ScaleForWindow(DefaultGhostNoteOffsetY);
            patternNamePosX    = DpiScaling.ScaleForWindow(DefaultPatternNamePosX);
            headerIconPosX     = DpiScaling.ScaleForWindow(DefaultHeaderIconPosX);
            headerIconPosY     = DpiScaling.ScaleForWindow(DefaultHeaderIconPosY);
            scrollBarThickness = DpiScaling.ScaleForWindow(scrollBarSize);
            minScrollBarLength = DpiScaling.ScaleForWindow(DefaultMinScrollBarLength);
            noteSizeX          = DpiScaling.ScaleForWindowFloat(zoom * patternZoom);
            virtualSizeY       = rowToChannel != null ? channelSizeY * rowToChannel.Length : 0;
            scrollMargin       = (width - channelNameSizeX) / 8;

            // Shave a couple pixels when the size is getting too small.
            if (unscaledChannelSizeY < 24)
                patternHeaderSizeY = DpiScaling.ScaleForFont(DefaultPatternHeaderSizeY - 2);
            else
                patternHeaderSizeY = DpiScaling.ScaleForFont(DefaultPatternHeaderSizeY);
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

        private int GetNonEmptyChannelCount()
        {
            var count = 0;

            foreach (var c in App.SelectedSong.Channels)
            {
                if (c.HasAnyPatternInstances)
                    count++;
            }

            return count;
        }

        private int GetChannelCount(bool allowHideEmptyChannel = true)
        {
            if (App != null && App.Project != null && App.SelectedSong != null)
            {
                if (hideEmptyChannels && allowHideEmptyChannel)
                    return GetNonEmptyChannelCount();

                return App.SelectedSong.Channels.Length;
            }
            else
            {
                return 5;
            }
        }

        public int ComputeDesiredSizeY(out int channelSizeY, out bool verticalScoll)
        {
            var channelCount = GetChannelCount(false);
            var visibleChannelCount = GetChannelCount(true);
            var scrollBarSize = Settings.ScrollBars == 1 ? DefaultScrollBarThickness1 : (Settings.ScrollBars == 2 ? DefaultScrollBarThickness2 : 0);
            var constantSize = DefaultHeaderSizeY + scrollBarSize + 1;

            if (Platform.IsMobile)
            {
                verticalScoll = true;
                channelSizeY = visibleChannelCount > 0 ? Math.Clamp(((int)Math.Ceiling(height / DpiScaling.Window) - DefaultHeaderSizeY) / visibleChannelCount, 21, 80) : 21;
                return channelSizeY * channelCount + constantSize;
            }
            else
            {
                // Keep size a multiple of 2 at 150%, multiple of 4 at 125%/175%, etc.
                var frac = Utils.Frac(DpiScaling.Window);
                var divider = (frac == 0.25f || frac == 0.75f) ? 4 : (frac == 0.5f) ? 2 : 1;
                var minChannelSize = Utils.RoundUp(21, divider);
                var idealSequencerHeight = ParentWindow.Height * Settings.IdealSequencerSize / 100;
                
                channelSizeY = visibleChannelCount > 0 ? Math.Max(Utils.DivideAndRoundDown(idealSequencerHeight / visibleChannelCount, divider), minChannelSize) : minChannelSize;

                var actualSequencerHeight = channelSizeY * visibleChannelCount;

                if (Settings.AllowSequencerVerticalScroll && actualSequencerHeight > idealSequencerHeight)
                {
                    verticalScoll = true;
                    return idealSequencerHeight + constantSize;
                }
                else
                {
                    verticalScoll = false;
                    return actualSequencerHeight + constantSize;
                }
            }
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

        private void RebuildChannelMap()
        {
            if (Song == null)
            {
                return;
            }

            channelVisible = new bool[Song.Channels.Length];
            channelToRow = new int[Song.Channels.Length];

            if (hideEmptyChannels)
            {
                numRows = GetChannelCount(true);
                rowToChannel = new int[numRows];

                for (int i = 0, j = 0; i < Song.Channels.Length; i++)
                {
                    if (Song.Channels[i].HasAnyPatternInstances)
                    {
                        channelVisible[i] = true;
                        channelToRow[i] = j;
                        rowToChannel[j] = i;
                        j++;
                    }
                    else
                    {
                        channelToRow[i] = -1;
                    }
                }
            }
            else
            {
                numRows = channelVisible.Length;
                rowToChannel = channelToRow;

                for (int i = 0; i < channelVisible.Length; i++)
                {
                    channelVisible[i] = true;
                    channelToRow[i] = i;
                    rowToChannel[i] = i;
                }
            }
        }

        public void SetHideEmptyChannels(bool hide)
        {
            hideEmptyChannels = hide;
            RebuildChannelMap();
            MarkDirty();
        }

        private void ClearSelection()
        {
            selectionMin = PatternLocation.Invalid;
            selectionMax = PatternLocation.Invalid;
            SelectionChanged?.Invoke();
        }

        private void SetSelection(PatternLocation min, PatternLocation max, bool timeOnly = false)
        {
            selectionMin = min; 
            selectionMax = max;
            timeOnlySelection = timeOnly;
            SelectionChanged?.Invoke();
        }

        private void EnsureSelectionInclude(PatternLocation location)
        {
            if (!IsSelectionValid())
            {
                SetSelection(location, location, false);
            }
            else
            {
                SetSelection(PatternLocation.Min(selectionMin, location), PatternLocation.Max(selectionMax, location));
            }
        }

        private bool IsPatternSelected(PatternLocation location)
        {
            return location.ChannelIndex >= selectionMin.ChannelIndex && location.ChannelIndex <= selectionMax.ChannelIndex &&
                   location.PatternIndex >= selectionMin.PatternIndex && location.PatternIndex <= selectionMax.PatternIndex;
        }

        private bool IsPatternColumnSelected(int patternIdx)
        {
            return IsValidTimeOnlySelection() && patternIdx >= selectionMin.PatternIndex && patternIdx <= selectionMax.PatternIndex;
        }

        private bool SelectionContainsMultiplePatterns()
        {
            return IsSelectionValid() && (selectionMax.ChannelIndex - selectionMin.ChannelIndex + 1) * (selectionMax.PatternIndex - selectionMin.PatternIndex + 1) > 1;
        }

        public bool GetPatternTimeSelectionRange(out int minPatternIdx, out int maxPatternIdx)
        {
            if (IsSelectionValid())
            {
                minPatternIdx = selectionMin.PatternIndex;
                maxPatternIdx = selectionMax.PatternIndex;
                return true;
            }
            else
            {
                minPatternIdx = -1;
                maxPatternIdx = -1;
                return false;
            }
        }

        private void SetMouseLastPos(int x, int y)
        {
            mouseLastX = x;
            mouseLastY = y;
        }

        private void SetFlingVelocity(float x, float y)
        {
            flingVelX = x;
            flingVelY = y;
        }

        protected override void OnAddedToContainer()
        {
            UpdateRenderCoords();

            var g = ParentWindow.Graphics;
            patternCache = new PatternBitmapCache(g);
            bmpExpansions = g.GetTextureAtlasRefs(ExpansionType.Icons);
            bmpChannels = g.GetTextureAtlasRefs(ChannelType.Icons);
            bmpForceDisplay = g.GetTextureAtlasRef("GhostSmall");
            bmpLoopPoint = g.GetTextureAtlasRef("LoopSmallFill");
            bmpInstantiate = g.GetTextureAtlasRef("Instance");
            bmpDuplicate = g.GetTextureAtlasRef("Duplicate");
            bmpDuplicateMove = g.GetTextureAtlasRef("DuplicateMove");

            if (Platform.IsMobile)
            {
                bitmapScale = DpiScaling.ScaleForWindowFloat(0.5f);
                channelBitmapScale = DpiScaling.ScaleForWindowFloat(0.25f);
            }

            bmpShyOn = g.GetTextureAtlasRef("ShyOn");
            bmpShyOff = g.GetTextureAtlasRef("ShyOff");
            
            seekGeometry = new float[]
            {
                -headerSizeY / 2, 1,
                0, headerSizeY - 2,
                headerSizeY / 2, 1 
            };
        }

        protected override void OnResize(EventArgs e)
        {
            UpdateRenderCoords();
            ClampScroll();
        }

        protected bool IsSelectionValid()
        {
            return selectionMin.IsValid && selectionMax.IsValid;
        }

        private bool IsValidTimeOnlySelection()
        {
            return IsSelectionValid() && timeOnlySelection;
        }

        private void SetHighlightedPattern(PatternLocation location)
        {
            highlightLocation = location;
        }

        private void ClearHighlightedPatern()
        {
            highlightLocation = PatternLocation.Invalid;
        }

        private bool HasHighlightedPattern()
        {
            return highlightLocation.IsValid;
        }

        private Color GetSeekBarColor()
        {
            if (App.IsRecording)
            {
                return Theme.DarkRedColor;
            }
            else
            {
                if (App.IsSeeking)
                {
                    return Theme.Lighten(Theme.YellowColor, (int)(Math.Abs(Math.Sin(Platform.TimeSeconds() * 12.0)) * 75));
                }
                else
                {
                    return Theme.YellowColor;
                }
            }
        }

        public int GetSeekFrameToDraw()
        {
            return captureOperation == CaptureOperation.DragSeekBar ? dragSeekPosition : App.CurrentFrame;
        }

        private bool GetMinMaxSelectedRow(out int minSelRow, out int maxSelRow)
        {
            var minSelChannel = selectionMin.ChannelIndex;
            var maxSelChannel = selectionMax.ChannelIndex;
            
            while (minSelChannel < Song.Channels.Length && channelToRow[minSelChannel] < 0) minSelChannel++;
            while (maxSelChannel >= 0 && channelToRow[maxSelChannel] < 0) maxSelChannel--;

            if (minSelChannel < Song.Channels.Length && maxSelChannel >= 0)
            {
                minSelRow = channelToRow[minSelChannel];
                maxSelRow = channelToRow[maxSelChannel];
                return true;
            }
            else
            {
                minSelRow = -1;
                maxSelRow = -1;
                return false;
            }
        }

        protected void RenderChannelNames(Graphics g)
        {
            var c = g.DefaultCommandList;

            // Track name background
            c.FillRectangle(0, 0, channelNameSizeX, height, Theme.DarkGreyColor2);
            c.DrawLine(channelNameSizeX - 1, 0, channelNameSizeX - 1, height, Theme.BlackColor);
            c.DrawLine(0, 0, channelNameSizeX, 0, Theme.BlackColor);
            c.DrawLine(0, height - scrollBarThickness, channelNameSizeX, height - scrollBarThickness, Theme.BlackColor);
            c.DrawLine(0, height - 1, channelNameSizeX, height - 1, Theme.BlackColor);
            c.DrawLine(0, headerSizeY, channelNameSizeX, headerSizeY, Theme.BlackColor);

            // Shy
            c.DrawTextureAtlasCentered(hideEmptyChannels && !forceShyOff ? bmpShyOn : bmpShyOff, GetShyButtonRect(), bitmapScale, hoverShy ? Theme.LightGreyColor1 : Theme.LightGreyColor2);

            // Vertical line seperating with the toolbar
            if (Platform.IsMobile && IsLandscape)
                c.DrawLine(0, 0, 0, height, Theme.BlackColor);

            // Scrollable area.
            c.PushClipRegion(0, headerSizeY + 1, channelNameSizeX, height - scrollBarThickness - headerSizeY - 1);
            c.FillClipRegion(Theme.DarkGreyColor2);
            c.DrawLine(channelNameSizeX - 1, 0, channelNameSizeX - 1, height, Theme.BlackColor);
            c.DrawLine(0, height - 1, channelNameSizeX, height - 1, Theme.BlackColor);
            c.PushTranslation(0, headerSizeY - scrollY);

            // Horizontal lines seperating patterns.
            for (int i = 0, y = 0; i <= rowToChannel.Length; i++, y += channelSizeY)
                c.DrawLine(0, y, channelNameSizeX, y, Theme.BlackColor);

            var showExpIcons = showExpansionIcons && Song.Project.UsesAnyExpansionAudio;
            var atlas = showExpIcons ? bmpExpansions : bmpChannels;
            var selectedChannelIndex = App.SelectedChannelIndex;

            for (int i = 0, y = 0; i < Song.Channels.Length; i++)
            {
                if (channelVisible[i])
                {
                    // Icon
                    var isHoverRow = hoverRow == channelToRow[i];
                    var channel = Song.Channels[i];
                    var bitmapIndex = showExpIcons ? channel.Expansion : channel.Type;
                    var iconHoverOpacity = isHoverRow && (hoverIconMask & 1) != 0 ? 192 : 255;
                    var iconFinalOpacity = Utils.ColorMultiply((App.ChannelMask & (1L << i)) != 0 ? 255 : 50, iconHoverOpacity);
                    c.DrawTextureAtlas(atlas[bitmapIndex], channelIconPosX, y + channelIconPosY, channelBitmapScale, Theme.LightGreyColor1.Transparent(iconFinalOpacity));

                    // Name
                    var font = i == selectedChannelIndex ? Fonts.FontMediumBold : Fonts.FontMedium;
                    var iconHeight = bmpChannels[0].ElementSize.Height * channelBitmapScale;
                    c.DrawText(Song.Channels[i].LocalizedName, font, channelNamePosX, y + channelIconPosY, Theme.LightGreyColor2, TextFlags.MiddleLeft, 0, iconHeight);

                    // Force display icon.
                    var ghostHoverOpacity = isHoverRow && (hoverIconMask & 2) != 0 ? 192 : 255;
                    var ghostFinalOpacity = Utils.ColorMultiply((App.ForceDisplayChannelMask & (1L << i)) != 0 ? 255 : 50, ghostHoverOpacity);
                    c.DrawTextureAtlasCentered(bmpForceDisplay, GetRowGhostRect(channelToRow[i]).Offsetted(0, -headerSizeY + scrollY), channelBitmapScale, Theme.LightGreyColor1.Transparent(ghostFinalOpacity));

                    // Hover
                    if (isHoverRow)
                        c.FillRectangle(0, y, channelNameSizeX, y + channelSizeY, Theme.MediumGreyColor1);

                    y += channelSizeY;
                }
            }

            c.PopTransform();
            c.PopClipRegion();
        }

        protected void RenderPatternArea(Graphics g)
        {
            var c = g.DefaultCommandList;
            var b = g.BackgroundCommandList;

            var seekX = GetPixelForNote(GetSeekFrameToDraw());
            var minVisibleNoteIdx = Math.Max(GetNoteForPixel(0), 0);
            var maxVisibleNoteIdx = Math.Min(GetNoteForPixel(width) + 1, Song.GetPatternStartAbsoluteNoteIndex(Song.Length));
            var minVisiblePattern = Utils.Clamp(Song.PatternIndexFromAbsoluteNoteIndex(minVisibleNoteIdx) + 0, 0, Song.Length);
            var maxVisiblePattern = Utils.Clamp(Song.PatternIndexFromAbsoluteNoteIndex(maxVisibleNoteIdx) + 1, 0, Song.Length);
            var actualSizeY = height - scrollBarThickness;
            var patternAreaSizeX = width - channelNameSizeX - (allowVerticalScrolling ? scrollBarThickness : 0);

            // Header
            c.PushTranslation(channelNameSizeX, 0);
            c.PushClipRegion(0, 0, width - channelNameSizeX, headerSizeY + 1);
            c.DrawLine(0, headerSizeY, width, headerSizeY, Theme.BlackColor);

            // Header Background
            for (int i = minVisiblePattern; i < maxVisiblePattern; i++)
            {
                var px = GetPixelForNote(Song.GetPatternStartAbsoluteNoteIndex(i));
                var sx = GetPixelForNote(Song.GetPatternLength(i), false);
                var color = i == hoverPattern ? Theme.MediumGreyColor1 : ((i & 1) == 0 ? Theme.DarkGreyColor4 : Theme.DarkGreyColor2);
                b.FillRectangle(px, 0, px + sx, headerSizeY, color);
            }

            // Header selection
            if (IsValidTimeOnlySelection())
            {
                c.FillRectangle(
                    GetPixelForNote(Song.GetPatternStartAbsoluteNoteIndex(Math.Min(selectionMin.PatternIndex + 0, Song.Length))), 0,
                    GetPixelForNote(Song.GetPatternStartAbsoluteNoteIndex(Math.Min(selectionMax.PatternIndex + 1, Song.Length))), headerSizeY,
                    IsActiveControl ? selectedPatternVisibleColor : selectedPatternInvisibleColor);
            }

            // Header
            c.DrawLine(0, 0, width, 0, Theme.BlackColor);
            c.DrawLine(0, height - 1, width, height - 1, Theme.BlackColor);

            // Vertical lines 
            for (int i = Math.Max(1, minVisiblePattern); i <= maxVisiblePattern; i++)
            {
                var px = GetPixelForNote(Song.GetPatternStartAbsoluteNoteIndex(i));
                b.DrawLine(px, 0, px, headerSizeY, Theme.BlackColor);
            }

            // Pattern names.
            for (int i = minVisiblePattern; i < maxVisiblePattern; i++)
            {
                var patternLen = Song.GetPatternLength(i);
                var px = GetPixelForNote(Song.GetPatternStartAbsoluteNoteIndex(i));
                var sx = GetPixelForNote(patternLen, false);

                c.PushTranslation(px, 0);

                var text = (i + 1).ToString();
                if (Song.PatternHasCustomSettings(i))
                    text += "*";
                c.DrawText(text, Fonts.FontMedium, 0, barTextPosY, Theme.LightGreyColor1, TextFlags.Center | TextFlags.Clip, sx);

                if (i == Song.LoopPoint)
                    c.DrawTextureAtlas(bmpLoopPoint, headerIconPosX, headerIconPosY, bitmapScale, Theme.LightGreyColor1);

                c.PopTransform();
            }

            // Seek bar
            c.PushTranslation(seekX, 0);
            c.FillAndDrawGeometry(seekGeometry, GetSeekBarColor(), Theme.BlackColor, 1, true);
            c.PopTransform();

            // Scrollable pattern area
            c.PopClipRegion();
            c.PushTranslation(0, headerSizeY);
            c.PushClipRegion(0, 1, patternAreaSizeX, height - headerSizeY - scrollBarThickness - 1);
            c.DrawLine(0, height - headerSizeY - 1, patternAreaSizeX, height - headerSizeY - 1, Theme.BlackColor);
            c.DrawLine(0, 0, patternAreaSizeX, 0, Theme.BlackColor);

            for (int i = minVisiblePattern; i < maxVisiblePattern; i++)
            {
                var px = GetPixelForNote(Song.GetPatternStartAbsoluteNoteIndex(i));
                var sx = GetPixelForNote(Song.GetPatternLength(i), false);
                var color = (i & 1) == 0 ? Theme.DarkGreyColor4 : Theme.DarkGreyColor2;
                b.FillRectangle(px, 0, px + sx, height - headerSizeY, color);
            }

            // Selection
            if (IsSelectionValid())
            {
                if (GetMinMaxSelectedRow(out var minSelRow, out var maxSelRow))
                {
                    c.FillRectangle(
                        GetPixelForNote(Song.GetPatternStartAbsoluteNoteIndex(Math.Min(selectionMin.PatternIndex + 0, Song.Length))), channelSizeY * (minSelRow + 0) - scrollY,
                        GetPixelForNote(Song.GetPatternStartAbsoluteNoteIndex(Math.Min(selectionMax.PatternIndex + 1, Song.Length))), channelSizeY * (maxSelRow + 1) - scrollY,
                        IsActiveControl ? selectedPatternVisibleColor : selectedPatternInvisibleColor);
                }
            }

            // Vertical lines
            for (int i = Math.Max(1, minVisiblePattern); i <= maxVisiblePattern; i++)
            {
                var px = GetPixelForNote(Song.GetPatternStartAbsoluteNoteIndex(i));
                c.DrawLine(px, 0, px, height - scrollBarThickness, Theme.BlackColor);
            }

            c.PushTranslation(0, -scrollY);

            // Horizontal lines
            for (int i = 0, y = 0; i <= rowToChannel.Length; i++, y += channelSizeY)
                c.DrawLine(0, y, width, y, Theme.BlackColor);

            // TODO : This is really bad, since all the logic is in the rendering code. Make
            // this more like any other capture op eventually.
            if (captureOperation == CaptureOperation.DragSelection)
            {
                var pt = new Point(mouseLastX, mouseLastY);
                var noteIdx = GetNoteForPixel(pt.X - channelNameSizeX);

                if (noteIdx >= 0 && noteIdx < Song.GetPatternStartAbsoluteNoteIndex(Song.Length) && GetMinMaxSelectedRow(out var minSelRow, out var maxSelRow))
                {
                    var patternIdx = Song.PatternIndexFromAbsoluteNoteIndex(noteIdx);
                    var patternIdxDelta = patternIdx - selectionDragAnchorPatternIdx;

                    var dragRowIdxStart = captureRowIdx;
                    var dragRowIdxCurrent = GetRowIndexForCoord(pt.Y);
                    var rowIdxDelta = dragRowIdxCurrent - dragRowIdxStart;

                    for (int j = minSelRow + rowIdxDelta; j <= maxSelRow + rowIdxDelta; j++)
                    {
                        if (j < 0 || j >= rowToChannel.Length)
                            continue;

                        var y = j * channelSizeY;

                        // Center.
                        var patternSizeX = GetPixelForNote(Song.GetPatternLength(patternIdx), false);
                        var anchorOffsetLeftX = (int)(patternSizeX * selectionDragAnchorPatternXFraction);
                        var anchorOffsetRightX = (int)(patternSizeX * (1.0f - selectionDragAnchorPatternXFraction));

                        var instance = ModifierKeys.IsControlDown;
                        var duplicate = instance && ModifierKeys.IsShiftDown;

                        var bmpCopy = (TextureAtlasRef)null;
                        var bmpSize = DpiScaling.ScaleCustom(bmpDuplicate.ElementSize.Width, bitmapScale);

                        if (rowIdxDelta != 0)
                            bmpCopy = (duplicate || instance) ? bmpDuplicate : bmpDuplicateMove;
                        else
                            bmpCopy = duplicate ? bmpDuplicate : (instance ? bmpInstantiate : null);

                        c.PushTranslation(pt.X - channelNameSizeX, y);
                        c.FillAndDrawRectangle(-anchorOffsetLeftX, 0, -anchorOffsetLeftX + patternSizeX, channelSizeY, selectedPatternVisibleColor, Theme.BlackColor);

                        if (bmpCopy != null)
                            c.DrawTextureAtlas(bmpCopy, -anchorOffsetLeftX + patternSizeX / 2 - bmpSize / 2, channelSizeY / 2 - bmpSize / 2, bitmapScale, Theme.LightGreyColor1);

                        // Left side
                        for (int p = patternIdx - 1; p >= selectionMin.PatternIndex + patternIdxDelta && p >= 0; p--)
                        {
                            patternSizeX = GetPixelForNote(Song.GetPatternLength(p), false);
                            anchorOffsetLeftX += patternSizeX;

                            c.FillAndDrawRectangle(-anchorOffsetLeftX, 0, -anchorOffsetLeftX + patternSizeX, channelSizeY, selectedPatternVisibleColor, Theme.BlackColor);

                            if (bmpCopy != null)
                                c.DrawTextureAtlas(bmpCopy, -anchorOffsetLeftX + patternSizeX / 2 - bmpSize / 2, channelSizeY / 2 - bmpSize / 2, bitmapScale, Theme.LightGreyColor1);
                        }

                        // Right side
                        for (int p = patternIdx + 1; p <= selectionMax.PatternIndex + patternIdxDelta && p < Song.Length; p++)
                        {
                            patternSizeX = GetPixelForNote(Song.GetPatternLength(p), false);

                            c.FillAndDrawRectangle(anchorOffsetRightX, 0, anchorOffsetRightX + patternSizeX, channelSizeY, selectedPatternVisibleColor, Theme.BlackColor);

                            if (bmpCopy != null)
                                c.DrawTextureAtlas(bmpCopy, anchorOffsetRightX + patternSizeX / 2 - bmpSize / 2, channelSizeY / 2 - bmpSize / 2, bitmapScale, Theme.LightGreyColor1);

                            anchorOffsetRightX += patternSizeX;
                        }

                        c.PopTransform();
                    }
                }
            }

            // Patterns
            var patternCacheSizeY = channelSizeY - patternHeaderSizeY - 1;
            patternCache.Update(patternCacheSizeY);

            for (int pi = minVisiblePattern; pi < maxVisiblePattern; pi++)
            {
                var patternLen = Song.GetPatternLength(pi);
                var noteLen = Song.UsesFamiTrackerTempo ? 1 : Song.GetPatternNoteLength(pi);
                var px = GetPixelForNote(Song.GetPatternStartAbsoluteNoteIndex(pi));
                var sx = GetPixelForNote(patternLen, false);

                c.PushTranslation(px, 0);

                // TODO : Dont draw channels that are not visible!
                for (int ci = 0, py = 0; ci < Song.Channels.Length; ci++)
                {
                    if (channelVisible[ci])
                    {
                        var location = new PatternLocation(ci, pi);
                        var pattern = Song.GetPatternInstance(location);

                        if (pattern != null)
                        {
                            var bmp = patternCache.GetOrAddPattern(pattern, patternLen, noteLen, out var u0, out var v0, out var u1, out var v1);

                            c.PushTranslation(0, py);
                            c.FillRectangleGradient(1, 1, sx, patternHeaderSizeY, pattern.Color, pattern.Color.Scaled(0.8f), true, patternHeaderSizeY);
                            c.FillRectangle(1, patternHeaderSizeY, sx, channelSizeY, Color.FromArgb(75, pattern.Color));
                            c.DrawLine(0, patternHeaderSizeY, sx, patternHeaderSizeY, Theme.BlackColor);
                            c.DrawTexture(bmp, 1.0f, 1.0f + patternHeaderSizeY, sx - 1, patternCacheSizeY, u0, v0, u1, v1);
                            c.DrawText(pattern.Name, Fonts.FontSmall, patternNamePosX, 0, Theme.BlackColor, TextFlags.Left | TextFlags.Middle | TextFlags.Clip, sx - patternNamePosX, patternHeaderSizeY + 1);
                            if (IsPatternSelected(location))
                                c.DrawRectangle(0, 0, sx, channelSizeY, Theme.LightGreyColor1, 3, true, true);
                            c.PopTransform();
                        }

                        if (Platform.IsMobile && highlightLocation == location)
                        {
                            c.DrawRectangle(0, py, sx, py + channelSizeY, Theme.WhiteColor, 3, true, true);
                        }

                        py += channelSizeY;
                    }
                }

                c.PopTransform();
            }

            // Piano roll view rect
            if (App.GetPianoRollViewRange(out var pianoRollMinNoteIdx, out var pianoRollMaxNoteIdx, out var pianoRollChannelIndex) && channelToRow[pianoRollChannelIndex] >= 0)
            {
                c.PushTranslation(GetPixelForNote(pianoRollMinNoteIdx), channelToRow[pianoRollChannelIndex] * channelSizeY);
                c.DrawRectangle(1, patternHeaderSizeY + 1, GetPixelForNote(pianoRollMaxNoteIdx - pianoRollMinNoteIdx, false) - 1, channelSizeY - 1, Theme.LightGreyColor2);
                c.PopTransform();
            }

            // Seek bar
            b.DrawLine(seekX, 0, seekX, virtualSizeY, GetSeekBarColor(), 3);

            c.PopTransform();
            c.PopClipRegion();

            c.PopTransform();
            c.PopTransform();

            // Scroll bar (optional)
            if (GetScrollBarParams(true, out var scrollBarThumbPosX, out var scrollBarSizeThumbX, out var scrollBarSizeX))
            {
                c.PushTranslation(channelNameSizeX - 1, 0);
                c.FillAndDrawRectangle(0, actualSizeY, scrollBarSizeX, height - 1, Theme.DarkGreyColor4, Theme.BlackColor);
                c.FillAndDrawRectangle(scrollBarThumbPosX, actualSizeY, scrollBarThumbPosX + scrollBarSizeThumbX, height, Theme.MediumGreyColor1, Theme.BlackColor);
                c.PopTransform();
            }

            if (GetScrollBarParams(false, out var scrollBarThumbPosY, out var scrollBarThumbSizeY, out var scrollBarSizeY))
            {
                c.PushTranslation(0, headerSizeY);
                c.FillAndDrawRectangle(width - scrollBarThickness, 0, width, scrollBarSizeY, Theme.DarkGreyColor4, Theme.BlackColor);
                c.FillAndDrawRectangle(width - scrollBarThickness, scrollBarThumbPosY, width, scrollBarThumbPosY + scrollBarThumbSizeY, Theme.MediumGreyColor1, Theme.BlackColor);
                c.PopTransform();
            }
        }

        protected void RenderDebug(Graphics g)
        {
#if DEBUG
            if (Platform.IsMobile)
            {
                g.OverlayCommandList.FillRectangle(mouseLastX - 30, mouseLastY - 30, mouseLastX + 30, mouseLastY + 30, Theme.WhiteColor);
            }
#endif
        }

        protected override void OnRender(Graphics g)
        {
            // Happens when piano roll is maximized.
            if (height <= 1)
                return;

            RenderChannelNames(g);
            RenderPatternArea(g);
            RenderDebug(g);
        }

        private void ReplaceSelectionUtil(Point pos, bool forceInSelection, Func<Channel, bool> channelValid, Action<Pattern> action)
        {
            Debug.Assert(!forceInSelection || IsSelectionValid());

            if (GetPatternForCoord(pos.X, pos.Y, out var location))
            {
                // If we drag on selection, we process the whole selection, otherwise
                // just the pattern under the mouse.
                if (IsPatternSelected(location))
                {
                    App.UndoRedoManager.BeginTransaction(TransactionScope.Song, Song.Id);
                    var replacedAnything = false;

                    for (int i = selectionMin.ChannelIndex; i <= selectionMax.ChannelIndex; i++)
                    {
                        var channel = Song.Channels[i];
                        if (channelValid(channel))
                        {
                            for (int j = selectionMin.PatternIndex; j <= selectionMax.PatternIndex; j++)
                            {
                                var pattern = channel.PatternInstances[j];
                                if (pattern != null)
                                {
                                    action(pattern);
                                    NotifyPatternChange(pattern);
                                    replacedAnything = true;
                                }
                            }

                            channel.InvalidateCumulativePatternCache();
                        }
                    }

                    App.UndoRedoManager.AbortOrEndTransaction(replacedAnything);
                    MarkDirty();
                }
                else
                {
                    var channel = Song.Channels[location.ChannelIndex];
                    if (channelValid(channel))
                    {
                        var pattern = channel.PatternInstances[location.PatternIndex];
                        if (pattern != null)
                        {
                            App.UndoRedoManager.BeginTransaction(TransactionScope.Pattern, pattern.Id);
                            action(pattern);
                            NotifyPatternChange(pattern);
                            channel.InvalidateCumulativePatternCache(pattern);
                            App.UndoRedoManager.EndTransaction();
                            MarkDirty();
                        }
                    }
                }
            }
        }

        public void ReplaceSelectionInstrument(Instrument instrument, Point pos, bool forceInSelection = false)
        {
            ReplaceSelectionUtil(
                pos, forceInSelection,
                (channel) => channel.SupportsInstrument(instrument),
                (pattern) =>
                {
                    foreach (var n in pattern.Notes.Values)
                    {
                        if (n.IsMusical)
                            n.Instrument = instrument;
                    }
                });
        }

        public void ReplaceSelectionArpeggio(Arpeggio arpeggio, Point pos, bool forceInSelection = false)
        {
            ReplaceSelectionUtil(
                pos, forceInSelection,
                (channel) => channel.SupportsArpeggios,
                (pattern) =>
                {
                    foreach (var n in pattern.Notes.Values)
                    {
                        if (n.IsMusical)
                            n.Arpeggio = arpeggio;
                    }
                });
        }

        private bool GetScrollBarParams(bool horizontal, out int thumbPos, out int thumbSize, out int scrollSize)
        {
            thumbPos   = 0;
            thumbSize  = 0;
            scrollSize = 0;

            if (scrollBarThickness > 0)
            {
                if (horizontal)
                {
                    GetMinMaxScroll(out _, out var maxScrollX, out _, out _);

                    scrollSize = width - channelNameSizeX + 1;
                    thumbSize = Math.Max(minScrollBarLength, (int)Math.Round(scrollSize * Math.Min(1.0f, scrollSize / (float)(maxScrollX + scrollSize))));
                    thumbPos = (int)Math.Round((scrollSize - thumbSize) * (scrollX / (float)maxScrollX));
                    return true;
                }
                else if (allowVerticalScrolling)
                {
                    GetMinMaxScroll(out _, out _, out _, out var maxScrollY);

                    scrollSize = height - headerSizeY - scrollBarThickness;
                    thumbSize = Math.Max(minScrollBarLength, (int)Math.Round(scrollSize * Math.Min(1.0f, scrollSize / (float)(maxScrollY + scrollSize))));
                    thumbPos = (int)Math.Round((scrollSize - thumbSize) * (scrollY / (float)maxScrollY));
                    return true;
                }
            }

            return false;
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
            maxScrollX = Song != null ? Math.Max(GetPixelForNote(Song.GetPatternStartAbsoluteNoteIndex(Song.Length), false) - scrollMargin, 0) : 0;
            minScrollY = 0;
            maxScrollY = allowVerticalScrolling ? Math.Max(virtualSizeY + headerSizeY - height + scrollBarThickness, 0) : 0;
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
            var noteIdx = GetNoteForPixel(x - channelNameSizeX);

            if (noteIdx < 0 || noteIdx >= Song.GetPatternStartAbsoluteNoteIndex(Song.Length))
                return -1;

            return Song.PatternIndexFromAbsoluteNoteIndex(noteIdx);
        }

        private bool GetPatternForCoord(int x, int y, out PatternLocation location)
        {
            var noteIdx = GetNoteForPixel(x - channelNameSizeX);

            if (noteIdx < 0 || noteIdx >= Song.GetPatternStartAbsoluteNoteIndex(Song.Length))
            {
                location = PatternLocation.Invalid;
                return false;
            }

            location = new PatternLocation(GetChannelIndexForCoord(y), Song.PatternIndexFromAbsoluteNoteIndex(noteIdx));

            return x > channelNameSizeX && y > headerSizeY && location.IsChannelInSong(Song);
        }

        private int GetRowIndexForCoord(int y)
        {
            return Utils.Clamp(((y - headerSizeY) + scrollY) / channelSizeY, 0, rowToChannel.Length - 1);
        }

        private int GetChannelIndexForCoord(int y)
        {
            return rowToChannel.Length == 0 ? -1 : rowToChannel[GetRowIndexForCoord(y)];
        }

        private void GetClampedPatternForCoord(int x, int y, out int channelIdx, out int patternIdx)
        {
            patternIdx = Song.PatternIndexFromAbsoluteNoteIndex(Utils.Clamp(GetNoteForPixel(x - channelNameSizeX), 0, Song.GetPatternStartAbsoluteNoteIndex(Song.Length) - 1));
            channelIdx = GetChannelIndexForCoord(y);
        }

        Rectangle GetRowIconRect(int rowIdx)
        {
            return new Rectangle(
                channelIconPosX,
                channelIconPosY + headerSizeY + rowIdx * channelSizeY - scrollY, 
                DpiScaling.ScaleForWindow(16),
                DpiScaling.ScaleForWindow(16));
        }

        Rectangle GetRowGhostRect(int rowIdx)
        {
            return new Rectangle(
                channelNameSizeX - ghostNoteOffsetX, 
                headerSizeY + (rowIdx + 1) * channelSizeY - ghostNoteOffsetY - scrollY - 1,
                DpiScaling.ScaleForWindow(12),
                DpiScaling.ScaleForWindow(12));
        }

        Rectangle GetShyButtonRect()
        {
            var sx = DpiScaling.ScaleCustom(bmpShyOn.ElementSize.Width, bitmapScale);
            return new Rectangle(channelNameSizeX - ghostNoteOffsetX, 0, sx, headerSizeY);
        }

        bool IsPointInShyButton(int x, int y)
        {
            return GetShyButtonRect().Contains(x, y);
        }

        bool IsPointInHeader(int x, int y)
        {
            return y < headerSizeY;
        }

        bool IsPointInChannelArea(int x, int y)
        {
            return x < channelNameSizeX && y > headerSizeY;
        }

        bool IsPointInPatternArea(int x, int y)
        {
            return x > channelNameSizeX && y > headerSizeY;
        }

        private void CaptureMouse(int x, int y)
        {
            SetMouseLastPos(x, y);
            captureMouseX = x;
            captureMouseY = y;
            captureScrollX = scrollX;
            captureScrollY = scrollY;
            CapturePointer();
        }

        private void StartCaptureOperation(int x, int y, CaptureOperation op)
        {
            Debug.Assert(captureOperation == CaptureOperation.None);
            CaptureMouse(x, y);
            canFling = false;
            captureOperation = op;
            captureThresholdMet = !captureNeedsThreshold[(int)op];
            mouseMovedDuringCapture = false;
            captureRealTimeUpdate = captureWantsRealTimeUpdate[(int)op];
            captureRowIdx = GetRowIndexForCoord(y);
            GetClampedPatternForCoord(x, y, out captureChannelIdx, out capturePatternIdx);
        }

        private void SetSelectedChannel(int idx)
        {
            if (idx != App.SelectedChannelIndex)
                App.SelectedChannelIndex = idx;
        }

        private void ChangeChannelForCoord(int y)
        {
            SetSelectedChannel(GetChannelIndexForCoord(y));
        }

        private void SetLoopPoint(int patternIdx)
        {
            App.UndoRedoManager.BeginTransaction(TransactionScope.Song, Song.Id);
            Song.SetLoopPoint(Song.LoopPoint == patternIdx ? -1 : patternIdx);
            App.UndoRedoManager.EndTransaction();
            MarkDirty();
        }

        private void CreateNewPattern(PatternLocation location)
        {
            var channel = Song.Channels[location.ChannelIndex];

            App.UndoRedoManager.BeginTransaction(TransactionScope.Channel, Song.Id, location.ChannelIndex);
            channel.PatternInstances[location.PatternIndex] = channel.CreatePattern();
            channel.InvalidateCumulativePatternCache();
            PatternClicked?.Invoke(location.ChannelIndex, location.PatternIndex, false);
            App.UndoRedoManager.EndTransaction();

            ClearSelection();
            MarkDirty();
        }

        private void DeletePattern(PatternLocation location)
        {
            var channel = Song.Channels[location.ChannelIndex];
            var pattern = channel.PatternInstances[location.PatternIndex];

            App.UndoRedoManager.BeginTransaction(TransactionScope.Channel, Song.Id, location.ChannelIndex);
            patternCache.Remove(pattern);
            channel.PatternInstances[location.PatternIndex] = null;
            channel.InvalidateCumulativePatternCache();
            PatternModified?.Invoke();
            App.UndoRedoManager.EndTransaction();

            ClearSelection();
            MarkDirty();
        }

        private bool HandleMouseDownPan(PointerEventArgs e)
        {
            bool middle = e.Middle || (e.Left && ModifierKeys.IsAltDown && Settings.AltLeftForMiddle);

            if (middle)
            {
                panning = true;
                CaptureMouse(e.X, e.Y);
                return true;
            }

            return false;
        }

        private bool HandleMouseDownScrollbar(PointerEventArgs e)
        {
            if (e.Left && scrollBarThickness > 0 && e.X > channelNameSizeX && e.Y > headerSizeY)
            {
                if (e.Y >= (height - scrollBarThickness) && GetScrollBarParams(true, out var scrollBarPosX, out var scrollBarSizeX, out _))
                {
                    var x = e.X - channelNameSizeX;
                    if (x < scrollBarPosX)
                    {
                        scrollX -= (width - channelNameSizeX);
                        ClampScroll();
                    }
                    else if (x > (scrollBarPosX + scrollBarSizeX))
                    {
                        scrollX += (width - channelNameSizeX);
                        ClampScroll();
                    }
                    else if (x >= scrollBarPosX && x <= (scrollBarPosX + scrollBarSizeX))
                    {
                        StartCaptureOperation(e.X, e.Y, CaptureOperation.ScrollBarX);
                    }
                    return true;
                }
                if (e.X >= (width - scrollBarThickness) && GetScrollBarParams(false, out var scrollBarThumbPosY, out var scrollBarThumbSizeY, out _))
                {
                    var y = e.Y - headerSizeY;
                    if (y < scrollBarThumbPosY)
                    {
                        scrollY -= (height - headerSizeY);
                        ClampScroll();
                        MarkDirty();
                    }
                    else if (y > (scrollBarThumbPosY + scrollBarThumbSizeY))
                    {
                        scrollX += (height - headerSizeY);
                        ClampScroll();
                        MarkDirty();
                    }
                    else if (y >= scrollBarThumbPosY && y <= (scrollBarThumbPosY + scrollBarThumbSizeY))
                    {
                        StartCaptureOperation(e.X, e.Y, CaptureOperation.ScrollBarY);
                    }
                    return true;
                }
            }

            return false;
        }

        private bool HandleMouseDownChannelName(PointerEventArgs e)
        {
            if (e.Left && IsMouseInTrackName(e.X, e.Y))
            { 
                var chanIdx = GetChannelIndexFromIconPos(e.X, e.Y);
                if (chanIdx >= 0)
                {
                    App.ToggleChannelActive(chanIdx);
                    return true;
                }

                chanIdx = GetChannelIndexFromGhostIconPos(e.X, e.Y);
                if (chanIdx >= 0)
                {
                    App.ToggleChannelForceDisplay(chanIdx);
                    return true;
                }
            }

            return false;
        }

        private bool HandleMouseDownShy(PointerEventArgs e)
        {
            return e.Left && HandleTouchClickShy(e.X, e.Y);
        }

        private bool HandleMouseUpChannelName(PointerEventArgs e)
        {
            return e.Right && HandleContextMenuChannelName(e.X, e.Y);
        }

        private bool HandleMouseUpHeader(PointerEventArgs e)
        {
            return e.Right && HandleContextMenuHeader(e.X, e.Y);
        }

        private bool HandleMouseUpPatternArea(PointerEventArgs e)
        {
            return e.Right && HandleContextMenuPatternArea(e.X, e.Y);
        }

        private bool HandleMouseDownSetLoopPoint(PointerEventArgs e)
        {
            bool setLoop = Settings.SetLoopPointShortcut.IsKeyDown(ParentWindow);

            if (setLoop && e.X > channelNameSizeX && e.Left)
            {
                var patternIdx = GetPatternIndexForCoord(e.X);
                if (patternIdx >= 0)
                    SetLoopPoint(patternIdx);
                return true;
            }

            return false;
        }

        private bool HandleMouseDownSeekBar(PointerEventArgs e)
        {
            if (IsMouseInHeader(e) && e.Left)
            {
                StartCaptureOperation(e.X, e.Y, CaptureOperation.DragSeekBar);
                UpdateSeekDrag(e.X, e.Y, false);
                return true;
            }

            return false;
        }

        private bool HandleMouseDownHeaderSelection(PointerEventArgs e)
        {
            if (IsMouseInHeader(e) && e.Right)
            {
                e.DelayRightClick(); // Can have selection or context menu, need to wait.
                return true;
            }

            return false;
        }

        private bool HandleMouseDownChannelChange(PointerEventArgs e)
        {
            if (e.Y > headerSizeY && e.Y < height - scrollBarThickness && e.Left)
                ChangeChannelForCoord(e.Y);

            // Does not prevent from processing other events.
            return false;
        }

        private bool HandleMouseDownAltZoom(PointerEventArgs e)
        {
            if (e.Right && ModifierKeys.IsAltDown && Settings.AltZoomAllowed && GetPatternForCoord(e.X, e.Y, out _))
            {
                StartCaptureOperation(e.X, e.Y, CaptureOperation.AltZoom);
                return true;
            }

            return false;
        }
        
        private void StartDragSelection(int x, int y, int patternIdx)
        {
            selectionDragAnchorPatternIdx = patternIdx;
            selectionDragAnchorPatternXFraction = (x - channelNameSizeX + scrollX - GetPixelForNote(Song.GetPatternStartAbsoluteNoteIndex(patternIdx), false)) / (float)GetPixelForNote(Song.GetPatternLength(patternIdx), false);
            StartCaptureOperation(x, y, CaptureOperation.DragSelection);
        }

        private bool HandleMouseDownPatternArea(PointerEventArgs e)
        {
            bool inPatternZone = GetPatternForCoord(e.X, e.Y, out var location);

            if (inPatternZone)
            {
                var pattern = Song.GetPatternInstance(location);

                if (e.Left)
                {
                     if (pattern == null)
                    {
                        CreateNewPattern(location);
                    }
                    else
                    {
                        if (pattern != null)
                        {
                            if (ModifierKeys.IsShiftDown)
                            {
                                DeletePattern(location);
                                return true;
                            }

                            PatternClicked?.Invoke(location.ChannelIndex, location.PatternIndex, false);
                        }

                        if (!IsPatternSelected(location))
                        {
                            SetSelection(location, location);
                            timeOnlySelection = false;
                            SelectionChanged?.Invoke();
                        }

                        StartDragSelection(e.X, e.Y, location.PatternIndex);
                    }

                    return true;
                }
                else if (e.Right)
                {
                    e.DelayRightClick(); // Need to wait to see difference between context menu or selection.
                    return true;
                }
            }

            return false;
        }

        protected override void OnPointerDown(PointerEventArgs e)
        {
            if (e.IsTouchEvent)
            {
                OnTouchDown(e);
                return;
            }

            if (captureOperation != CaptureOperation.None && (e.Left || e.Right))
                return;

            UpdateCursor();

            if (HandleMouseDownPan(e)) goto Handled;
            if (HandleMouseDownScrollbar(e)) goto Handled;
            if (HandleMouseDownChannelName(e)) goto Handled;
            if (HandleMouseDownShy(e)) goto Handled;
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

        private bool HandleMouseDownDelayedHeaderSelection(PointerEventArgs e)
        {
            if (e.Right && IsMouseInHeader(e))
            {
                StartCaptureOperation(e.X, e.Y, CaptureOperation.SelectColumn);
                return true;
            }

            return false;
        }

        private bool HandleMouseDownDelayedRectangleSelection(PointerEventArgs e)
        {
            if (e.Right && IsMouseInPatternZone(e))
            {
                StartCaptureOperation(e.X, e.Y, CaptureOperation.SelectRectangle);
                return true;
            }

            return false;
        }

        protected override void OnPointerDownDelayed(PointerEventArgs e)
        {
            if (HandleMouseDownDelayedHeaderSelection(e)) goto Handled;
            if (HandleMouseDownDelayedRectangleSelection(e)) goto Handled;

            return;

        Handled:
            MarkDirty();
        }

        private bool HandleTouchDownDragSeekBar(int x, int y)
        {
            if (IsPointInHeader(x, y) && !IsPointInShyButton(x, y))
            {
                var seekX = GetPixelForNote(App.CurrentFrame) + channelNameSizeX;

                // See if we are close enough to the yellow triangle.
                if (Math.Abs(seekX - x) < headerSizeY)
                {
                    StartCaptureOperation(x, y, CaptureOperation.DragSeekBar);
                    UpdateSeekDrag(x, y, false);
                    return true;
                }
            }

            return false;
        }

        private bool HandleTouchDownHeaderSelection(int x, int y)
        {
            if (IsPointInHeader(x, y))
            {
                // If pattern column is already select, dont start another selection
                // since we will want the longpress to be processed.
                var patternIdx = GetPatternIndexForCoord(x);

                if (patternIdx >= 0)
                {
                    StartCaptureOperation(x, y, CaptureOperation.SelectColumn);
                    return true;
                }
            }

            return false;
        }

        private bool HandleTouchDownPan(int x, int y)
        {
            if (IsPointInPatternArea(x, y))
            {
                StartCaptureOperation(x, y, CaptureOperation.MobilePan);
                return true;
            }

            return false;
        }

        private bool HandleTouchDownPatternArea(int x, int y)
        {
            if (IsPointInPatternArea(x, y))
            {
                bool inPatternZone = GetPatternForCoord(x, y, out var location);

                if (inPatternZone && highlightLocation == location)
                {
                    if (!IsPatternSelected(highlightLocation))
                        SetSelection(highlightLocation, highlightLocation);

                    StartDragSelection(x, y, highlightLocation.PatternIndex);
                    return true;
                }
            }

            return false;
        }

        private bool HandleTouchClickChannelChange(int x, int y)
        {
            if (IsPointInChannelArea(x, y))
            {
                ChangeChannelForCoord(y);
                return true;
            }

            return false;
        }

        private bool HandleTouchClickPatternHeader(int x, int y)
        {
            if (IsPointInHeader(x, y))
            {
                App.SeekSong(GetNoteForPixel(x - channelNameSizeX));
                return true;
            }

            return false;
        }

        private bool HandleTouchClickPatternArea(int x, int y)
        {
            bool inPatternZone = GetPatternForCoord(x, y, out var location);

            if (inPatternZone)
            {
                var pattern = Song.GetPatternInstance(location);

                if (pattern == null)
                {
                    CreateNewPattern(location);
                    SetHighlightedPattern(location);
                }
                else 
                {
                    if (highlightLocation == location)
                        ClearHighlightedPatern();
                    else
                        SetHighlightedPattern(location);
                }

                // CreateNewPattern clears the selection. Ugh, call after.
                SetSelection(location, location);

                return true;
            }

            return false;
        }

        private bool HandleTouchClickShy(int x, int y)
        {
            if (IsPointInShyButton(x, y))
            {
                SetHideEmptyChannels(!hideEmptyChannels);
                ShyChanged?.Invoke();
                return true;
            }

            return false;
        }

        private bool HandleTouchClickChannelName(int x, int y, bool doubleClick = false)
        {
            if (IsMouseInTrackName(x, y))
            {
                var chanIdx = GetChannelIndexFromIconPos(x, y);
                if (chanIdx >= 0)
                {
                    if (doubleClick)
                    {
                        App.ToggleChannelSolo(chanIdx, true);
                    }
                    else
                    {
                        App.ToggleChannelActive(chanIdx);
                    }
                    return true;
                }

                chanIdx = GetChannelIndexFromGhostIconPos(x, y);
                if (chanIdx >= 0)
                {
                    if (doubleClick) 
                    { 
                        App.ToggleChannelForceDisplayAll(chanIdx, true);
                    }
                    else
                    {
                        App.ToggleChannelForceDisplay(chanIdx);
                    }
                    return true;
                }
            }

            return false;
        }

        private bool HandleTouchDoubleClickPatternArea(int x, int y)
        {
            bool inPatternZone = GetPatternForCoord(x, y, out var location);

            if (inPatternZone)
            {
                var pattern = Song.GetPatternInstance(location);

                if (pattern != null)
                {
                    DeletePattern(location);
                    ClearHighlightedPatern();
                }

                return true;
            }

            return false;
        }

        private bool HandleContextMenuChannelName(int x, int y)
        {
            if (IsPointInChannelArea(x, y))
            {
                var channelIdx = GetChannelIndexForCoord(y);
               
                App.ShowContextMenuAsync(new[]
                {
                    new ContextMenuOption("MenuMute", ToggleMuteLabel, () => { App.ToggleChannelActive(channelIdx); }),
                    new ContextMenuOption("MenuSolo", ToggleSoloLabel, () => { App.ToggleChannelSolo(channelIdx); }),
                    new ContextMenuOption("MenuForceDisplay", ForceDisplayLabel, () => { App.ToggleChannelForceDisplay(channelIdx); })
                });

                return true;
            }

            return false;
        }

        private bool HandleTouchLongPressChannelName(int x, int y)
        {
            return HandleContextMenuChannelName(x, y);
        }

        private bool HandleContextMenuHeader(int x, int y)
        {
            if (IsPointInHeader(x, y))
            {
                var patternIdx = GetPatternIndexForCoord(x);

                if (patternIdx >= 0)
                {
                    var isLoopPoint = Song.LoopPoint == patternIdx;

                    App.ShowContextMenuAsync(new[]
                    {
                        new ContextMenuOption(isLoopPoint ? "MenuClearLoopPoint" :  "MenuLoopPoint", isLoopPoint ?  ClearLoopPointLabel : SetLoopPointLabel, () => { SetLoopPoint(patternIdx); } ),
                        new ContextMenuOption("MenuCustomPatternSettings", CustomPatternSettingsLabel, () => { EditPatternCustomSettings(new Point(x, y), patternIdx); } )
                    }); ;
                }

                return true;
            }

            return false;
        }

        private bool HandleTouchLongPressHeader(int x, int y)
        {
            return HandleContextMenuHeader(x, y);
        }

        private void GotoPianoRoll(PatternLocation location)
        {
            PatternClicked?.Invoke(location.ChannelIndex, location.PatternIndex, true);
        }

        private void CopySelectionToCursor(bool copy)
        {
            var channelDeltaIdx = highlightLocation.ChannelIndex - selectionMin.ChannelIndex;
            var patternDeltaIdx = highlightLocation.PatternIndex - selectionMin.PatternIndex;

            MoveCopyOrDuplicateSelection(channelDeltaIdx, patternDeltaIdx, true, copy);
        }

        private bool HandleContextMenuPatternArea(int x, int y)
        {
            bool inPatternZone = GetPatternForCoord(x, y, out var location);

            if (inPatternZone)
            {
                var pattern = Song.GetPatternInstance(location);

                SetHighlightedPattern(location);

                var menu = new List<ContextMenuOption>(); ;

                if (Platform.IsMobile)
                {
                    menu.Add(new ContextMenuOption("MenuPiano", GoToPianoRollLabel, () => { GotoPianoRoll(location); }));
                }

                if (IsSelectionValid() && !IsPatternSelected(location))
                {
                    if (Platform.IsMobile)
                        menu.Add(new ContextMenuOption("MenuExpandSelection", ExpandSelectionLabel, () => { EnsureSelectionInclude(location); }));
                    if (selectionMin.ChannelIndex == location.ChannelIndex)
                        menu.Add(new ContextMenuOption("MenuInstance", InstanciateHereLabel, () => { CopySelectionToCursor(false); }));
                    menu.Add(new ContextMenuOption("MenuDuplicate", DuplicateHereLabel, () => { CopySelectionToCursor(true); }));
                }

                if (IsSelectionValid())
                {
                    menu.Add(new ContextMenuOption("MenuClearSelection", ClearSelectionLabel, () => { ClearSelection(); ClearHighlightedPatern(); }, ContextMenuSeparator.Before));
                }

                if (pattern != null)
                {
                    if (IsPatternSelected(location) && SelectionContainsMultiplePatterns())
                    {
                        menu.Insert(0, new ContextMenuOption("MenuDeleteSelection", DeleteSelectionLabel, () => { DeleteSelection(true); }));
                        menu.Add(new ContextMenuOption("MenuProperties", SelectedPatternPropertiesLabel, () => { EditPatternProperties(new Point(x, y), pattern, location, true); }, ContextMenuSeparator.Before));
                    }
                    else
                    {
                        if (Platform.IsDesktop)
                            SetSelection(location, location);

                        menu.Add(new ContextMenuOption("MenuProperties", PatternPropertiesLabel, () => { EditPatternProperties(new Point(x, y), pattern, location, false); }, ContextMenuSeparator.Before));
                    }

                    menu.Insert(0, new ContextMenuOption("MenuDelete", DeletePatternLabel, () => { DeletePattern(location); }));
                }

                if (menu.Count > 0)
                    App.ShowContextMenuAsync(menu.ToArray());
                
                return true;
            }

            return false;
        }

        private bool HandleTouchLongPressPatternArea(int x, int y)
        {
            return HandleContextMenuPatternArea(x, y);
        }

        protected void OnTouchDown(PointerEventArgs e)
        {
            var x = e.X;
            var y = e.Y;

            SetFlingVelocity(0, 0);
            SetMouseLastPos(x, y);

            if (HandleTouchDownPatternArea(x, y)) goto Handled;
            if (HandleTouchDownDragSeekBar(x, y)) goto Handled;
            if (HandleTouchDownHeaderSelection(x, y)) goto Handled;
            if (HandleTouchDownPan(x, y)) goto Handled;
            return;

        Handled:
            MarkDirty();
        }

        protected void OnTouchMove(PointerEventArgs e)
        {
            var x = e.X;
            var y = e.Y;

            UpdateCaptureOperation(x, y);
            SetMouseLastPos(x, y);
        }

        protected void OnTouchUp(PointerEventArgs e)
        {
            var x = e.X;
            var y = e.Y;

            EndCaptureOperation(x, y);
            SetMouseLastPos(x, y);
        }

        protected override void OnTouchFling(PointerEventArgs e)
        {
            var x = e.X;
            var y = e.Y;

            if (canFling)
            {
                EndCaptureOperation(x, y);
                SetFlingVelocity(e.FlingVelocityX, e.FlingVelocityY);
            }
        }

        protected override void OnTouchScaleBegin(PointerEventArgs e)
        {
            var x = e.X;
            var y = e.Y;

            if (captureOperation != CaptureOperation.None)
            {
                Debug.Assert(captureOperation != CaptureOperation.MobileZoom);
                AbortCaptureOperation();
            }

            StartCaptureOperation(x, y, CaptureOperation.MobileZoom);
            SetMouseLastPos(x, y);
        }

        protected override void OnTouchScale(PointerEventArgs e)
        {
            var x = e.X;
            var y = e.Y;

            UpdateCaptureOperation(x, y, e.TouchScale);
            SetMouseLastPos(x, y);
        }

        protected override void OnTouchScaleEnd(PointerEventArgs e)
        {
            var x = e.X;
            var y = e.Y;

            EndCaptureOperation(x, y);
            SetMouseLastPos(x, y);
        }

        protected override void OnTouchClick(PointerEventArgs e)
        {
            var x = e.X;
            var y = e.Y;

            if (HandleTouchClickShy(x, y)) goto Handled;
            if (HandleTouchClickChannelName(x, y)) goto Handled;
            if (HandleTouchClickChannelChange(x, y)) goto Handled;
            if (HandleTouchClickPatternHeader(x, y)) goto Handled;
            if (HandleTouchClickPatternArea(x, y)) goto Handled;

            return;

        Handled:
            MarkDirty();
        }

        protected override void OnTouchDoubleClick(PointerEventArgs e)
        {
            var x = e.X;
            var y = e.Y;

            SetMouseLastPos(x, y);

            // Ignore double tap if we handled a single tap recently.
            if (captureOperation != CaptureOperation.None/* || (DateTime.Now - lastPatternCreateTime).TotalMilliseconds < 500*/)
            {
                return;
            }

            if (HandleTouchDoubleClickPatternArea(x, y)) goto Handled;
            if (HandleTouchClickShy(x, y)) goto Handled;
            if (HandleTouchClickChannelName(x, y, true)) goto Handled;

            return;

        Handled:
            MarkDirty();
        }

        protected override void OnTouchLongPress(PointerEventArgs e)
        {
            var x = e.X;
            var y = e.Y;

            if (e.IsDoubleTapLongPress)
            {
                return;
            }

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

            var patterns = new Pattern[selectionMax.PatternIndex - selectionMin.PatternIndex + 1, selectionMax.ChannelIndex - selectionMin.ChannelIndex + 1];

            for (int i = 0; i < patterns.GetLength(0); i++)
            {
                for (int j = 0; j < patterns.GetLength(1); j++)
                {
                    patterns[i, j] = Song.Channels[selectionMin.ChannelIndex + j].PatternInstances[selectionMin.PatternIndex + i];
                }
            }

            if (IsValidTimeOnlySelection())
            {
                customSettings = new Song.PatternCustomSetting[patterns.GetLength(0)];

                for (int i = 0; i < patterns.GetLength(0); i++)
                    customSettings[i] = Song.GetPatternCustomSettings(selectionMin.PatternIndex + i).Clone();
            }

            return patterns;
        }

        public bool CanCopy   => IsActiveControl && IsSelectionValid();
        public bool CanPaste  => IsActiveControl && IsSelectionValid() && ClipboardUtils.ContainsPatterns;
        public bool CanDelete => CanCopy;
        public bool IsActiveControl => App != null && App.ActiveControl == this;

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
                createMissingInstrument = Platform.MessageBox(ParentWindow, PasteMissingInstrumentsMessage, PasteTitle, MessageBoxButtons.YesNo) == DialogResult.Yes;

            bool createMissingArpeggios = false;
            if (missingArpeggios)
                createMissingArpeggios = Platform.MessageBox(ParentWindow, PasteMissingArpeggiosMessage, PasteTitle, MessageBoxButtons.YesNo) == DialogResult.Yes;

            bool createMissingSamples = false;
            if (missingSamples)
                createMissingSamples = Platform.MessageBox(ParentWindow, PasteMissingSamplesMessage, PasteTitle, MessageBoxButtons.YesNo) == DialogResult.Yes;

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
                for (int i = song.Length - 1; i >= selectionMin.PatternIndex + numColumnsToPaste; i--)
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
                for (int i = selectionMin.PatternIndex; i < selectionMax.PatternIndex + numColumnsToPaste; i++)
                {
                    song.ClearPatternCustomSettings(i);

                    for (int j = 0; j < song.Channels.Length; j++)
                    {
                        song.Channels[j].PatternInstances[i] = null;
                    }
                }
            }
            
            // Then do the actual paste.
            var startPatternIndex = selectionMin.PatternIndex;

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
                            Song.ClearPatternCustomSettings(i + selectionMin.PatternIndex);
                        }
                    }
                }

                startPatternIndex += patterns.GetLength(0);
            }

            selectionMax.PatternIndex = selectionMin.PatternIndex + numColumnsToPaste - 1;
            selectionMin.ChannelIndex = 0;
            selectionMax.ChannelIndex = Song.Channels.Length - 1;

            song.InvalidateCumulativePatternCache();
            song.DeleteNotesPastMaxInstanceLength();

            App.UndoRedoManager.EndTransaction();
            PatternsPasted?.Invoke();
            SelectionChanged?.Invoke();
            RebuildChannelMap();
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

            var dialog = new PropertyDialog(ParentWindow, PasteSpecialTitle, 200);
            dialog.Properties.AddLabelCheckBox(InsertLabel, false, 0, InsertTooltip); // 0
            dialog.Properties.AddLabelCheckBox(ExtendSongLabel, false, 0, ExtendSongTooltip); // 1
            dialog.Properties.AddNumericUpDown(RepeatLabel.Colon, 1, 1, 32, 1, RepeatTooltip); // 2
            dialog.Properties.SetPropertyEnabled(1, false);
            dialog.Properties.PropertyChanged += PasteSpecialDialog_PropertyChanged;
            dialog.Properties.Build();

            dialog.ShowDialogAsync((r) =>
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
                Cursor = Cursors.DragCursor;
            }
            else
            {
                Cursor = Cursors.Default;
            }
        }

        private int IncrementChannelIndex(int channelIdx, int rowDelta, bool clamp)
        {
            if (hideEmptyChannels)
            {
                var idx = channelToRow[channelIdx] + rowDelta;

                if (clamp)
                    idx = Utils.Clamp(idx, 0, rowToChannel.Length - 1);
                else if (idx < 0 || idx >= rowToChannel.Length)
                    return -1;

                return rowToChannel[idx];
            }
            else
            {
                var idx = channelIdx + rowDelta;

                if (clamp)
                    idx = Utils.Clamp(idx, 0, Song.Channels.Length - 1);

                return idx;
            }
        }

        private int IncrementPatternIndex(int patternIndex, int delta, bool clamp)
        {
            patternIndex += delta;

            if (clamp)
                patternIndex = Utils.Clamp(patternIndex, 0, Song.Length - 1);

            return patternIndex;
        }

        private void MoveCopyOrDuplicateSelection(int rowIdxDelta, int patternIdxDelta, bool copy, bool duplicate)
        {
            App.UndoRedoManager.BeginTransaction(TransactionScope.Song, Song.Id);

            var tmpPatterns = GetSelectedPatterns(out var customSettings);

            if (!copy)
                DeleteSelection(false, customSettings != null && !copy);

            var duplicatePatternMap = new Dictionary<Pattern, Pattern>();

            for (int i = selectionMin.ChannelIndex; i <= selectionMax.ChannelIndex; i++)
            {
                for (int j = selectionMin.PatternIndex; j <= selectionMax.PatternIndex; j++)
                {
                    var ni = IncrementChannelIndex(i, rowIdxDelta, false);
                    var nj = IncrementPatternIndex(j, patternIdxDelta, false);

                    if (nj >= 0 && nj < Song.Length && ni >= 0 && ni < Song.Channels.Length)
                    {
                        var sourcePattern = tmpPatterns[j - selectionMin.PatternIndex, i - selectionMin.ChannelIndex];

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
                for (int j = selectionMin.PatternIndex; j <= selectionMax.PatternIndex; j++)
                {
                    var settings = customSettings[j - selectionMin.PatternIndex];

                    var nj = j + patternIdxDelta;
                    if (nj >= 0 && nj < Song.Length)
                    {
                        if (settings.useCustomSettings)
                        {
                            Song.SetPatternCustomSettings(
                                nj,
                                customSettings[j - selectionMin.PatternIndex].patternLength,
                                customSettings[j - selectionMin.PatternIndex].beatLength,
                                customSettings[j - selectionMin.PatternIndex].groove,
                                customSettings[j - selectionMin.PatternIndex].groovePaddingMode);
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
                    var noteIdx = GetNoteForPixel(x - channelNameSizeX);

                    if (noteIdx >= 0 && noteIdx < Song.GetPatternStartAbsoluteNoteIndex(Song.Length))
                    {
                        var patternIdx = Song.PatternIndexFromAbsoluteNoteIndex(GetNoteForPixel(x - channelNameSizeX));
                        var patternIdxDelta = patternIdx - selectionDragAnchorPatternIdx;

                        var dragRowIdxStart   = captureRowIdx;
                        var dragRowIdxCurrent = GetRowIndexForCoord(y);
                        var rowIdxDelta = dragRowIdxCurrent - dragRowIdxStart;

                        var copy = ModifierKeys.IsControlDown;
                        var duplicate = copy && ModifierKeys.IsShiftDown || rowIdxDelta != 0;

                        MoveCopyOrDuplicateSelection(rowIdxDelta, patternIdxDelta, copy, duplicate);

                        var timeOnly = IsValidTimeOnlySelection();

                        var newSelectionMin = new PatternLocation(
                            IncrementChannelIndex(selectionMin.ChannelIndex, rowIdxDelta, true),
                            IncrementPatternIndex(selectionMin.PatternIndex, patternIdxDelta, true));
                        var newSelectionMax = new PatternLocation(
                            IncrementChannelIndex(selectionMax.ChannelIndex, rowIdxDelta, true),
                            IncrementPatternIndex(selectionMax.PatternIndex, patternIdxDelta, true));

                        if (timeOnly)
                        {
                            newSelectionMin.ChannelIndex = 0;
                            newSelectionMax.ChannelIndex = Song.Channels.Length - 1;
                        }

                        SetSelection(newSelectionMin, newSelectionMax, timeOnly);

                        if (HasHighlightedPattern())
                        {

                            highlightLocation = new PatternLocation(
                                IncrementChannelIndex(highlightLocation.ChannelIndex, rowIdxDelta, true),
                                IncrementPatternIndex(highlightLocation.PatternIndex, patternIdxDelta, true));
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
                        UpdateSeekDrag(x, y, true);
                        break;
                    case CaptureOperation.MobilePan:
                    case CaptureOperation.MobileZoom:
                        canFling = true;
                        break;

                }

                panning = false;
                captureOperation = CaptureOperation.None;
                ReleasePointer();
                MarkDirty();
            }
        }

        protected override void OnPointerUp(PointerEventArgs e)
        {
            if (e.IsTouchEvent)
            {
                OnTouchUp(e);
                return;
            }

            bool middle = e.Middle;
            bool doMouseUp = false;

            if (middle)
            {
                panning = false;
            }
            else
            {
                doMouseUp = captureOperation == CaptureOperation.None;
                EndCaptureOperation(e.X, e.Y);
            }

            UpdateCursor();

            if (doMouseUp)
            {
                if (HandleMouseUpChannelName(e)) goto Handled;
                if (HandleMouseUpHeader(e)) goto Handled;
                if (HandleMouseUpPatternArea(e)) goto Handled;
                return;
                Handled:
                    MarkDirty();
            }
        }

        private void AbortCaptureOperation()
        {
            if (captureOperation != CaptureOperation.None)
            {
                if (App.UndoRedoManager.HasTransactionInProgress)
                    App.UndoRedoManager.AbortTransaction();

                panning = false;
                canFling = false;
                captureOperation = CaptureOperation.None;

                ReleasePointer();
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

        public void DeleteSelection()
        {
            DeleteSelection(true, IsValidTimeOnlySelection());
        }

        private void DeleteSelection(bool trans = true, bool clearCustomSettings = false)
        {
            if (trans)
            {
                App.UndoRedoManager.BeginTransaction(TransactionScope.Song, Song.Id);
            }

            for (int i = selectionMin.ChannelIndex; i <= selectionMax.ChannelIndex; i++)
            {
                for (int j = selectionMin.PatternIndex; j <= selectionMax.PatternIndex; j++)
                {
                    var pattern = Song.Channels[i].PatternInstances[j];
                    if (pattern != null)
                        patternCache.Remove(pattern);
                    Song.Channels[i].PatternInstances[j] = null;
                }
            }

            if (clearCustomSettings)
            {
                for (int j = selectionMin.PatternIndex; j <= selectionMax.PatternIndex; j++)
                    Song.ClearPatternCustomSettings(j);
            }

            Song.InvalidateCumulativePatternCache();
            Song.DeleteNotesPastMaxInstanceLength();

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
            if (e.Key == Keys.Escape)
            {
                CancelDragSelection();
                UpdateCursor();
                ClearSelection();
                MarkDirty();
            }
            else if (IsActiveControl)
            {
                if (Settings.CopyShortcut.Matches(e))
                { 
                    Copy();
                }
                else if (Settings.CutShortcut.Matches(e))
                { 
                    Cut();
                }
                else if (Settings.PasteShortcut.Matches(e))
                { 
                    Paste();
                }
                else if (Settings.PasteSpecialShortcut.Matches(e))
                { 
                    PasteSpecial();
                }
                else if (Settings.DeleteShortcut.Matches(e) && IsSelectionValid())
                {
                    CancelDragSelection();
                    DeleteSelection();
                }
                else if (IsActiveControl && Settings.SelectAllShortcut.Matches(e))
                {
                    SetSelection(new PatternLocation(0, 0), new PatternLocation(Song.Channels.Length - 1, Song.Length - 1), true);
                }
            }

            if (captureOperation == CaptureOperation.DragSelection)
            {
                UpdateCursor();
                MarkDirty();
            }
            
            if (IsActiveControl)
                UpdateToolTip(null);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            if (captureOperation == CaptureOperation.DragSelection)
            {
                UpdateCursor();
                MarkDirty();
            }

            if (IsActiveControl)
                UpdateToolTip(null);
        }

        private void UpdateSeekDrag(int mouseX, int mouseY, bool final)
        {
            ScrollIfNearEdge(mouseX, mouseY);

            dragSeekPosition = GetNoteForPixel(mouseX - channelNameSizeX);

            if (final)
                App.SeekSong(dragSeekPosition);

            MarkDirty();
        }

        private void ScrollIfNearEdge(int x, int y, bool scrollHorizontal = true, bool scrollVertical = false)
        {
            if (scrollHorizontal)
            {
                int posMinX = 0;
                int posMaxX = Platform.IsDesktop ? width + channelNameSizeX : (IsLandscape ? width + headerSizeY : width);
                int marginMinX = channelNameSizeX;
                int marginMaxX = Platform.IsDesktop ? channelNameSizeX : headerSizeY;

                scrollX += Utils.ComputeScrollAmount(x, posMinX, marginMinX, App.AverageTickRate * ScrollSpeedFactor, true);
                scrollX += Utils.ComputeScrollAmount(x, posMaxX, marginMaxX, App.AverageTickRate * ScrollSpeedFactor, false);
                ClampScroll();
            }

            if (scrollVertical)
            {
                int posMinY = 0;
                int posMaxY = Platform.IsMobile && !IsLandscape || Platform.IsDesktop ? height + headerSizeY : height;
                int marginMinY = headerSizeY;
                int marginMaxY = headerSizeY;

                scrollY += Utils.ComputeScrollAmount(y, posMinY, marginMinY, App.AverageTickRate * ScrollSpeedFactor, true);
                scrollY += Utils.ComputeScrollAmount(y, posMaxY, marginMaxY, App.AverageTickRate * ScrollSpeedFactor, false);
                ClampScroll();
            }
        }

        private void UpdateSelection(int x, int y, bool timeOnly, bool first = false)
        {
            ScrollIfNearEdge(x, y, true, !timeOnly && CanScrollVertically());

            if (first)
            {
                Debug.Assert(captureChannelIdx >= 0 && capturePatternIdx >= 0);

                selectionMin.PatternIndex = capturePatternIdx;
                selectionMax.PatternIndex = capturePatternIdx;

                if (timeOnly)
                {
                    selectionMin.ChannelIndex = 0;
                    selectionMax.ChannelIndex = Song.Channels.Length - 1;
                    timeOnlySelection = true;
                }
                else
                {
                    selectionMin.ChannelIndex = captureChannelIdx;
                    selectionMax.ChannelIndex = captureChannelIdx;
                    timeOnlySelection = false;
                }
            }
            else
            {
                GetClampedPatternForCoord(x, y, out var channelIdx, out var patternIdx);

                selectionMin.PatternIndex = Math.Min(patternIdx, capturePatternIdx);
                selectionMax.PatternIndex = Math.Max(patternIdx, capturePatternIdx);

                if (timeOnly)
                {
                    selectionMin.ChannelIndex = 0;
                    selectionMax.ChannelIndex = Song.Channels.Length - 1;
                    timeOnlySelection = true;
                }
                else
                {
                    selectionMin.ChannelIndex = Math.Min(channelIdx, captureChannelIdx);
                    selectionMax.ChannelIndex = Math.Max(channelIdx, captureChannelIdx);
                    timeOnlySelection = false;
                }
            }

            MarkDirty();
            SelectionChanged?.Invoke();
        }

        private void UpdateDragSelection(int x, int y)
        {
            ScrollIfNearEdge(x, y, true, Platform.IsMobile || CanScrollVertically());
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

        private bool IsMouseInPatternZone(PointerEventArgs e)
        {
            return e.Y > headerSizeY && e.X > channelNameSizeX;
        }

        private bool IsMouseInHeader(PointerEventArgs e)
        {
            return e.Y < headerSizeY && e.X > channelNameSizeX;
        }

        private bool IsMouseInTrackName(int x, int y)
        {
            return y > headerSizeY && x < channelNameSizeX;
        }

        private int GetChannelIndexFromIconPos(int x, int y)
        {
            for (int i = 0; i < rowToChannel.Length; i++)
            {
                if (GetRowIconRect(i).Contains(x, y))
                    return rowToChannel[i];
            }

            return -1;
        }

        private int GetChannelIndexFromGhostIconPos(int x, int y)
        {
            for (int i = 0; i < rowToChannel.Length; i++)
            {
                if (GetRowGhostRect(i).Contains(x, y))
                    return rowToChannel[i];
            }

            return -1;
        }

        private void UpdateToolTip(PointerEventArgs e)
        {
            if (e == null)
            {
                var pt = ScreenToControl(CursorPosition);
                e = new PointerEventArgs(0, pt.X, pt.Y);
            }

            string tooltip = "";

            bool inPatternZone = GetPatternForCoord(e.X, e.Y, out var location);

            if (inPatternZone)
            {
                var pattern = Song.GetPatternInstance(location);

                var tooltipList = new List<string>();

                if (pattern == null)
                    tooltipList.Add($"<MouseLeft> {AddPatternTooltip}");

                if (Settings.SetLoopPointShortcut.IsShortcutValid(0))
                    tooltipList.Add($"{Settings.SetLoopPointShortcut.TooltipString}<MouseLeft> {SetLoopPointTooltip}");

                tooltipList.Add($"<MouseWheel><Drag> {PanTooltip}");
                tooltipList.Add($"<MouseRight><Drag> {SelectRectangleTooltip}");

                if (pattern != null)
                {
                    tooltipList.Add($"<MouseLeft><MouseLeft> {OrTooltip} <Shift><MouseLeft> {DeletePatternTooltip}");
                    tooltipList.Add($"<MouseRight> {MoreOptionsTooltip}");
                }

                if (IsPatternSelected(location))
                {
                    tooltipList.Add($"<Drag> {MovePatternTooltip}");
                    tooltipList.Add($"<Ctrl><Drag> {ClonePatternTooltip}");
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
            else if (IsPointInShyButton(e.X, e.Y))
            {
                tooltip = $"<MouseLeft> {ShyModeTooltip}";
            }
            else if (IsMouseInHeader(e))
            {
                tooltip = $"<MouseLeft> {SeekTooptip} - <MouseRight> {MoreOptionsTooltip} - <MouseRight><Drag> {SelectColumnTooltip}\n<L><MouseLeft> {SetLoopPointTooltip} - <MouseWheel><Drag> {PanTooltip}";
            }
            else if (IsMouseInTrackName(e.X, e.Y))
            {
                if (GetChannelIndexFromIconPos(e.X, e.Y) >= 0)
                {
                    tooltip = $"<MouseLeft> {MuteChannelTooltip} - <MouseLeft><MouseLeft> {SoloChannelTooltip}";
                }
                else if (GetChannelIndexFromGhostIconPos(e.X, e.Y) >= 0)
                {
                    tooltip = $"<MouseLeft> {ForceDisplayTooltip}\n<MouseLeft><MouseLeft> {ForceDisplayAllChannelsTooltip}";
                    int idx = GetChannelIndexForCoord(e.Y);
                    if (idx >= 0 && idx < Settings.DisplayChannelShortcuts.Length)
                        tooltip += $" {Settings.DisplayChannelShortcuts[idx].TooltipString}";
                }
                else
                {
                    tooltip = $"<MouseLeft> {MakeActiveTooltip}";
                    int idx = GetChannelIndexForCoord(e.Y);
                    if (idx >= 0 && idx < Settings.ActiveChannelShortcuts.Length)
                        tooltip += $" {Settings.ActiveChannelShortcuts[idx].TooltipString}";
                }

                tooltip += $" - <MouseRight> {MoreOptionsTooltip}";
            }

            App.SetToolTip(tooltip);
        }

        private void UpdateScrollBarX(int x, int y)
        {
            GetScrollBarParams(true, out _, out var scrollBarSizeX, out _);
            GetMinMaxScroll(out _, out var maxScrollX, out _, out _);
            int scrollAreaSizeX = width - channelNameSizeX;
            scrollX = (int)Math.Round(captureScrollX + ((x - captureMouseX) / (float)(scrollAreaSizeX - scrollBarSizeX) * maxScrollX));
            ClampScroll();
            MarkDirty();
        }

        private void UpdateScrollBarY(int x, int y)
        {
            GetScrollBarParams(false, out _, out var scrollBarSizeY, out _);
            GetMinMaxScroll(out _, out _, out _, out var maxScrollY);
            int scrollAreaSizeY = height - headerSizeY - scrollBarThickness;
            scrollY = (int)Math.Round(captureScrollY + ((y - captureMouseY) / (float)(scrollAreaSizeY - scrollBarSizeY) * maxScrollY));
            ClampScroll();
            MarkDirty();
        }

        private bool CanScrollVertically()
        {
            GetMinMaxScroll(out _, out _, out var minScrollY, out var maxScrollY);
            return minScrollY != maxScrollY;
        }

        private void UpdateCaptureOperation(int x, int y, float scale = 1.0f, bool realTime = false)
        {
            const int CaptureThreshold = Platform.IsDesktop ? 5 : 50;

            var captureThresholdJustMet = false;

            if (captureOperation != CaptureOperation.None)
            {
                if (!captureThresholdMet)
                {
                    if (Math.Abs(x - captureMouseX) >= CaptureThreshold ||
                        Math.Abs(y - captureMouseY) >= CaptureThreshold)
                    {
                        captureThresholdMet = true;
                        captureThresholdJustMet = true;
                    }
                }

                mouseMovedDuringCapture |= (x != captureMouseX || y != captureMouseY);
            }

            if (captureOperation != CaptureOperation.None && captureThresholdMet && (captureRealTimeUpdate || !realTime))
            {
                switch (captureOperation)
                {
                    case CaptureOperation.SelectColumn:
                        UpdateSelection(x, y, true, captureThresholdJustMet);
                        break;
                    case CaptureOperation.SelectRectangle:
                        UpdateSelection(x, y, false, captureThresholdJustMet);
                        break;
                    case CaptureOperation.AltZoom:
                        UpdateAltZoom(x, y);
                        break;
                    case CaptureOperation.DragSeekBar:
                        UpdateSeekDrag(x, y, false);
                        break;
                    case CaptureOperation.ScrollBarX:
                        UpdateScrollBarX(x, y);
                        break;
                    case CaptureOperation.ScrollBarY:
                        UpdateScrollBarY(x, y);
                        break;
                    case CaptureOperation.MobilePan:
                        DoScroll(x - mouseLastX, y - mouseLastY);
                        break;
                    case CaptureOperation.DragSelection:
                        UpdateDragSelection(x, y);
                        break;
                    case CaptureOperation.MobileZoom:
                        ZoomAtLocation(x, scale);
                        DoScroll(x - mouseLastX, y - mouseLastY);
                        break;
                    default:
                        MarkDirty();
                        break;
                }
            }
        }

        protected override void OnPointerMove(PointerEventArgs e)
        {
            if (e.IsTouchEvent)
            {
                OnTouchMove(e);
                return;
            }

            bool middle = e.Middle || (e.Left && ModifierKeys.IsAltDown && Settings.AltLeftForMiddle);

            UpdateCursor();
            UpdateCaptureOperation(e.X, e.Y);

            if (middle)
                DoScroll(e.X - mouseLastX, e.Y - mouseLastY);

            UpdateHover(e);
            UpdateToolTip(e);
            SetMouseLastPos(e.X, e.Y);
            ShowExpansionIcons = false;
        }

        private void UpdateHover(PointerEventArgs e)
        {
            if (Platform.IsDesktop)
            {
                SetAndMarkDirty(ref hoverRow, e.Y > headerSizeY ? GetRowIndexForCoord(e.Y) : -1);
                SetAndMarkDirty(ref hoverPattern, GetPatternIndexForCoord(e.X));
                SetAndMarkDirty(ref hoverShy, GetShyButtonRect().Contains(e.X, e.Y));

                var newHoverIconMask = 0;
                if (hoverRow >= 0)
                {
                    if (GetRowIconRect(hoverRow).Contains(e.X, e.Y)) newHoverIconMask |= 1;
                    if (GetRowGhostRect(hoverRow).Contains(e.X, e.Y)) newHoverIconMask |= 2;
                }
                SetAndMarkDirty(ref hoverIconMask, newHoverIconMask);
            }
        }

        private void ClearHover()
        {
            if (Platform.IsDesktop)
            {
                SetAndMarkDirty(ref hoverRow, -1);
                SetAndMarkDirty(ref hoverPattern, -1);
                SetAndMarkDirty(ref hoverShy, false);
                SetAndMarkDirty(ref hoverIconMask, 0);
            }
        }

        protected override void OnPointerLeave(EventArgs e)
        {
            ClearHover();
        }

        // Custom pattern.
        private void EditPatternCustomSettings(Point pt, int patternIdx)
        {
            var dlg = new PropertyDialog(ParentWindow, CustomPatternTitle, new Point(left + pt.X, top + pt.Y), 300);
            var song = Song;
            var enabled = song.PatternHasCustomSettings(patternIdx);

            var minPattern = patternIdx;
            var maxPattern = patternIdx;

            if (IsValidTimeOnlySelection())
            {
                minPattern = selectionMin.PatternIndex;
                maxPattern = selectionMax.PatternIndex;
            }

            var tempoProperties = new TempoProperties(dlg.Properties, song, patternIdx, minPattern, maxPattern);

            dlg.Properties.AddCheckBox(CustomPatternLabel.Colon, song.PatternHasCustomSettings(patternIdx), CustomPatternTooltip); // 0
            tempoProperties.AddProperties();
            tempoProperties.EnableProperties(enabled);
            dlg.Properties.PropertyChanged += PatternCustomSettings_PropertyChanged;
            dlg.Properties.PropertiesUserData = tempoProperties;
            dlg.Properties.Build();

            dlg.ShowDialogAsync((r) =>
            {
                if (r == DialogResult.OK)
                {
                    App.UndoRedoManager.BeginTransaction(TransactionScope.Song, song.Id);
                    tempoProperties.ApplyAsync(ParentWindow, dlg.Properties.GetPropertyValue<bool>(0), () =>
                    {
                        App.UndoRedoManager.EndTransaction();
                        MarkDirty();
                        PatternModified?.Invoke();
                    });
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

        private void EditPatternProperties(Point pt, Pattern pattern, PatternLocation location, bool selection = true)
        {
            bool multipleChannelsSelected = selection && IsSelectionValid() && (selectionMax.ChannelIndex != selectionMin.ChannelIndex);
            bool multiplePatternsSelected = selection && IsSelectionValid() && ((selectionMax.ChannelIndex != selectionMin.ChannelIndex) || (selectionMin.PatternIndex != selectionMax.PatternIndex));

            var dlg = new PropertyDialog(ParentWindow, PatternPropertiesTitle, new Point(left + pt.X, top + pt.Y), 240, false, false, false);
            dlg.Properties.AddColoredTextBox(multiplePatternsSelected ? MultiplePatternsSelectedLabel : pattern.Name, pattern.Color);
            dlg.Properties.SetPropertyEnabled(0, !multiplePatternsSelected);
            dlg.Properties.AddColorPicker(pattern.Color);
            dlg.Properties.Build();

            dlg.ShowDialogAsync((r) =>
            {
                if (r == DialogResult.OK)
                {
                    if (!multipleChannelsSelected)
                        App.UndoRedoManager.BeginTransaction(TransactionScope.Channel, Song.Id, location.ChannelIndex);
                    else
                        App.UndoRedoManager.BeginTransaction(TransactionScope.Song, Song.Id);

                    var newName  = dlg.Properties.GetPropertyValue<string>(0).Trim();
                    var newColor = dlg.Properties.GetPropertyValue<Color>(1);

                    if (multiplePatternsSelected)
                    {
                        for (int i = selectionMin.ChannelIndex; i <= selectionMax.ChannelIndex; i++)
                        {
                            for (int j = selectionMin.PatternIndex; j <= selectionMax.PatternIndex; j++)
                            {
                                var pat = Song.Channels[i].PatternInstances[j];
                                if (pat != null)
                                    pat.Color = newColor;
                            }
                        }
                        App.UndoRedoManager.EndTransaction();
                    }
                    else if (Song.Channels[location.ChannelIndex].RenamePattern(pattern, newName))
                    {
                        pattern.Color = newColor;
                        App.UndoRedoManager.EndTransaction();
                    }
                    else
                    {
                        App.UndoRedoManager.AbortTransaction();
                        App.DisplayNotification(ErrorRenamingPattern, true);
                    }

                    MarkDirty();
                    PatternModified?.Invoke();
                }
            });
        }

        protected bool HandleMouseDoubleClickPatternArea(PointerEventArgs e)
        {
            if (e.Left && IsMouseInPatternZone(e) && GetPatternForCoord(e.X, e.Y, out var location))
            {
                var pattern = Song.GetPatternInstance(location);
                if (pattern != null)
                    DeletePattern(location);
                return true;
            }

            return false;
        }

        protected bool HandleMouseDoubleClickChannelName(PointerEventArgs e)
        {
            return e.Left && HandleTouchClickChannelName(e.X, e.Y, true);
        }

        protected override void OnMouseDoubleClick(PointerEventArgs e)
        {
            if (HandleMouseDoubleClickChannelName(e)) goto Handled;
            if (HandleMouseDoubleClickPatternArea(e)) goto Handled;

            return;

        Handled:
            MarkDirty();
        }

        private void ZoomAtLocation(int x, float scale)
        {
            if (scale == 1.0f)
                return;

            // When continuously following, zoom at the seek bar location.
            if (continuouslyFollowing)
                x = (int)(Width * Settings.FollowPercent);

            Debug.Assert(Platform.IsMobile || scale == 0.5f || scale == 2.0f);

            var pixelX = x - channelNameSizeX;
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

        protected override void OnMouseWheel(PointerEventArgs e)
        {
            if (Settings.TrackPadControls && !ModifierKeys.IsControlDown && !ModifierKeys.IsAltDown)
            {
                if (CanScrollVertically() && !ModifierKeys.IsShiftDown)
                    scrollY -= Utils.SignedCeil(e.ScrollY);
                else
                    scrollX -= Utils.SignedCeil(e.ScrollY);

                ClampScroll();
                MarkDirty();
            }
            else
            {
                ZoomAtLocation(e.X, e.ScrollY < 0.0f ? 0.5f : 2.0f);
            }
        }

        protected override void OnMouseHorizontalWheel(PointerEventArgs e)
        {
            scrollX += Utils.SignedCeil(e.ScrollX);
            ClampScroll();
            MarkDirty();
        }

        protected bool EnsureSeekBarVisible(float percent = float.MinValue)
        {
            if (percent == float.MinValue)
                percent = Settings.FollowPercent;

            var seekX = GetPixelForNote(App.CurrentFrame);
            var minX = 0;
            var maxX = (int)((width - channelNameSizeX) * percent);

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
                    var maxX = width - channelNameSizeX;
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

        private void UpdateShyIcon()
        {
            if (hideEmptyChannels && numRows == 0)
            {
                SetAndMarkDirty(ref forceShyOff, Utils.Frac(Platform.TimeSeconds()) < 0.25f);
            }
            else
            {
                forceShyOff = false;
            }
        }

        public override void Tick(float delta)
        {
            if (App == null)
                return;

            UpdateCaptureOperation(mouseLastX, mouseLastY, 1.0f, true);
            UpdateFollowMode();
            UpdateShyIcon();
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

        public void Serialize(ProjectBuffer buffer)
        {
            buffer.Serialize(ref scrollX);
            buffer.Serialize(ref zoom);
            buffer.Serialize(ref selectionMin.ChannelIndex);
            buffer.Serialize(ref selectionMax.ChannelIndex);
            buffer.Serialize(ref selectionMin.PatternIndex);
            buffer.Serialize(ref selectionMax.PatternIndex);
            buffer.Serialize(ref timeOnlySelection);
            buffer.Serialize(ref hideEmptyChannels);

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
