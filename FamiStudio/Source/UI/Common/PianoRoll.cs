using System;
using System.Diagnostics;
using System.Drawing;
using System.Collections.Generic;

namespace FamiStudio
{
    public class PianoRoll : Control
    {
        const float MinZoomFamiStudio       = 1.0f / 32.0f;
        const float MinZoomOther            = 1.0f / 8.0f;
        const float MaxZoom                 = 16.0f;
        const float MinZoomY                = 0.25f;
        const float MaxZoomY                = 4.0f;
        const float MaxWaveZoom             = 256.0f;
        const float DefaultChannelZoom      = MinZoomOther;
        const float DefaultEnvelopeZoom     = 4.0f;
        const float ContinuousFollowPercent = 0.75f;
        const float DefaultZoomWaveTime     = 0.25f;
        const float ScrollSpeedFactor       = Platform.IsMobile ? 2.0f : 1.0f;

        const int NumOctaves = 8;
        const int NumNotes   = NumOctaves * 12;

        const int DefaultHeaderSizeY               = 17;
        const int DefaultEffectPanelSizeY          = 176;
        const int DefaultEffectButtonSizeY         = 18;
        const int DefaultNoteSizeX                 = 16;
        const int DefaultNoteSizeY                 = 12;
        const int DefaultNoteAttackSizeX           = 3;
        const int DefaultReleaseNoteSizeY          = 8;
        const int DefaultEnvelopeSizeY             = Platform.IsMobile ? 4 : 9;
        const int DefaultPianoSizeX                = 94;
        const int DefaultPianoSizeXMobile          = 40;
        const int DefaultWhiteKeySizeY             = 20;
        const int DefaultBlackKeySizeX             = 56;
        const int DefaultBlackKeySizeXMobile       = 20;
        const int DefaultBlackKeySizeY             = 14;
        const int DefaultSnapIconPosX              = 3;
        const int DefaultSnapIconPosY              = 3;
        const int DefaultSnapIconDpcmPosX          = 0;
        const int DefaultSnapIconDpcmPosY          = 0;
        const int DefaultEffectIconPosX            = 2;
        const int DefaultEffectIconPosY            = 2;
        const int DefaultEffectNamePosX            = 17;
        const int DefaultEffectValuePosTextOffsetY = 13;
        const int DefaultEffectValueNegTextOffsetY = 2;
        const int DefaultBigTextPosX               = 10;
        const int DefaultBigTextPosY               = 10;
        const int DefaultTooltipTextPosX           = 10;
        const int DefaultTooltipTextPosY           = 30;
        const int DefaultDPCMTextPosX              = 2;
        const int DefaultRecordingKeyOffsetY       = 12;
        const int DefaultAttackIconPosX            = 1;
        const int DefaultWaveGeometrySampleSize    = 2;
        const int DefaultWaveDisplayPaddingY       = 8;
        const int DefaultScrollBarThickness1       = 10;
        const int DefaultScrollBarThickness2       = 16;
        const int DefaultMinScrollBarLength        = 128;
        const int DefaultScrollMargin              = 128;
        const int DefaultNoteResizeMargin          = 8;
        const int DefaultBeatTextPosX              = 3;
        const int DefaultMinPixelDistForLines      = 5;
        const int DefaultGizmoSize                 = 20;

        int headerSizeY;
        int headerAndEffectSizeY;
        int effectPanelSizeY;
        int effectButtonSizeY;
        int noteAttackSizeX;
        int releaseNoteSizeY;
        int whiteKeySizeY;
        int pianoSizeX;
        int blackKeySizeY;
        int blackKeySizeX;
        int effectIconPosX;
        int effectIconPosY;
        int headerIconsPosX;
        int headerIconsPosY;
        int effectNamePosX;
        int effectValuePosTextOffsetY;
        int effectValueNegTextOffsetY;
        int bigTextPosX;
        int bigTextPosY;
        int tooltipTextPosX;
        int tooltipTextPosY;
        int dpcmTextPosX;
        int recordingKeyOffsetY;
        int octaveSizeY;
        int virtualSizeY;
        int scrollBarThickness;
        int minScrollBarLength;
        int attackIconPosX;
        int waveGeometrySampleSize;
        int waveDisplayPaddingY;
        int scrollMargin;
        int noteResizeMargin;
        int beatTextPosX;
        int geometryNoteSizeY;
        int fontSmallCharSizeX;
        int minPixelDistForLines;
        int gizmoSize;
        float minZoom;
        float maxZoom;
        float envelopeValueSizeY;
        float noteSizeX;
        int noteSizeY;

        float effectBitmapScale = 1.0f;
        float bitmapScale = 1.0f;

        enum EditionMode
        {
            Channel,
            Enveloppe,
            DPCM,
            DPCMMapping,
            Arpeggio,
            VideoRecording
        };

        Brush whiteKeyBrush;
        Brush blackKeyBrush;
        Brush whiteKeyPressedBrush;
        Brush blackKeyPressedBrush;
        Brush frameLineBrush;
        Brush debugBrush;
        Brush seekBarBrush;
        Brush seekBarRecBrush;
        Brush selectionBgVisibleBrush;
        Brush selectionBgInvisibleBrush;
        Brush selectionNoteBrush;
        Brush highlightNoteBrush;
        Brush attackBrush;
        Brush attackBrushForceDisplay;
        Brush iconTransparentBrush;
        Brush invalidDpcmMappingBrush;
        Brush volumeSlideBarFillBrush;
        BitmapAtlasRef bmpLoopSmallFill;
        BitmapAtlasRef bmpReleaseSmallFill;
        BitmapAtlasRef bmpEnvResize;
        BitmapAtlasRef bmpExpandedSmall;
        BitmapAtlasRef bmpCollapsedSmall;
        BitmapAtlasRef bmpMaximize;
        BitmapAtlasRef bmpSnap;
        BitmapAtlasRef bmpSnapOff;
        BitmapAtlasRef bmpGizmoResizeLeftRight;
        BitmapAtlasRef bmpGizmoResizeUpDown;
        BitmapAtlasRef bmpGizmoResizeFill;
        BitmapAtlasRef[] bmpEffects;
        Geometry[] stopNoteGeometry        = new Geometry[2]; // [1] is used to draw arps.
        Geometry[] stopReleaseNoteGeometry = new Geometry[2]; // [1] is used to draw arps.
        Geometry[] releaseNoteGeometry     = new Geometry[2]; // [1] is used to draw arps.
        Geometry   slideNoteGeometry;
        Geometry   seekGeometry;
        Geometry   sampleGeometry;

        readonly string[] EffectImageNames = new string[]
        {
            "EffectVolume",
            "EffectVibrato",
            "EffectVibrato",
            "EffectPitch",
            "EffectSpeed",
            "EffectMod",
            "EffectMod",
            "EffectDutyCycle",
            "EffectNoteDelay",
            "EffectCutDelay",
            "EffectVolume",
            "EffectDutyCycle",
            "EffectFrame" // Special background rectangle image.
        };

        enum CaptureOperation
        {
            None,
            PlayPiano,
            ResizeEnvelope,
            DragLoop,
            DragRelease,
            ChangeEffectValue,
            ChangeSelectionEffectValue,
            DrawEnvelope,
            Select,
            SelectWave,
            CreateNote,
            CreateSlideNote,
            DragSlideNoteTarget,
            DragSlideNoteTargetGizmo,
            DragVolumeSlideTarget,
            DragVolumeSlideTargetGizmo,
            DragNote,
            DragSelection,
            AltZoom,
            DragSample,
            DragSeekBar,
            DragWaveVolumeEnvelope,
            ScrollBarX,
            ScrollBarY,
            ResizeNoteStart,
            ResizeSelectionNoteStart,
            ResizeNoteEnd,
            ResizeSelectionNoteEnd,
            MoveNoteRelease,
            ChangeEnvelopeValue,
            MobileZoom,
            MobileZoomVertical,
            MobilePan
        }

        static readonly bool[] captureNeedsThreshold = new[]
        {
            false, // None
            false, // PlayPiano
            false, // ResizeEnvelope
            false, // DragLoop
            false, // DragRelease
            false, // ChangeEffectValue
            false, // ChangeSelectionEffectValue
            false, // DrawEnvelope
            Platform.IsMobile, // Select
            Platform.IsMobile, // SelectWave
            false, // CreateNote
            true,  // CreateSlideNote
            true,  // DragSlideNoteTarget
            true,  // DragSlideNoteTargetGizmo
            false, // DragVolumeSlideTarget
            false, // DragVolumeSlideTargetGizmo
            true,  // DragNote
            false, // DragSelection
            false, // AltZoom
            true,  // DragSample
            false, // DragSeekBar
            false, // DragWaveVolumeEnvelope
            false, // ScrollBarX
            false, // ScrollBarY
            false, // ResizeNoteStart 
            false, // ResizeSelectionNoteStart
            false, // ResizeNoteEnd
            false, // ResizeSelectionNoteEnd
            false, // MoveNoteRelease
            false, // ChangeEnvelopeValue
            false, // MobileZoom
            false, // MobileZoomVertical
            false, // MobilePan
        };

        static readonly bool[] captureWantsRealTimeUpdate = new[]
        {
            false, // None
            false, // PlayPiano
            false, // ResizeEnvelope
            false, // DragLoop
            false, // DragRelease
            false, // ChangeEffectValue
            false, // ChangeSelectionEffectValue
            false, // DrawEnvelope
            true,  // Select
            true,  // SelectWave
            true,  // CreateNote
            true,  // CreateSlideNote
            true,  // DragSlideNoteTarget
            true,  // DragSlideNoteTargetGizmo
            false, // DragVolumeSlideTarget
            false, // DragVolumeSlideTargetGizmo
            true,  // DragNote
            true,  // DragSelection
            false, // AltZoom
            true,  // DragSample
            true,  // DragSeekBar
            false, // DragWaveVolumeEnvelope
            false, // ScrollBarX
            false, // ScrollBarY
            true,  // ResizeNoteStart 
            true,  // ResizeSelectionNoteStart
            true,  // ResizeNoteEnd
            true,  // ResizeSelectionNoteEnd
            false, // MoveNoteRelease
            false, // ChangeEnvelopeValue
            false, // MobileZoom
            false, // MobileZoomVertical
            false, // MobilePan
        };

        int captureNoteAbsoluteIdx = 0;
        int captureMouseAbsoluteIdx = 0;
        int captureNoteValue = 0;
        int mouseLastX = 0;
        int mouseLastY = 0;
        int captureMouseX = 0;
        int captureMouseY = 0;
        int captureScrollX = 0;
        int captureScrollY = 0;
        int captureSelectionMin = -1;
        int captureSelectionMax = -1;
        int captureOffsetY = 0;
        int playingNote = -1;
        int highlightNote = Note.NoteInvalid;
        int selectionMin = -1;
        int selectionMax = -1;
        int dragSeekPosition = -1;
        int snapResolution = SnapResolutionType.OneBeat;
        int scrollX = 0;
        int scrollY = 0;
        int lastChannelScrollX = -1;
        int lastChannelScrollY = -1;
        float lastChannelZoom = -1;
        int selectedEffectIdx = Platform.IsMobile ? -1 : 0;
        int[] supportedEffects;
        bool captureThresholdMet = false;
        bool captureRealTimeUpdate = false;
        bool panning = false;
        bool continuouslyFollowing = false;
        bool maximized = false;
        bool showEffectsPanel = false;
        bool snap = true;
        bool pianoVisible = true;
        bool canFling = false;
        sbyte captureEnvelopeValue = 0;
        float flingVelX = 0.0f;
        float flingVelY = 0.0f;
        float zoom = DefaultChannelZoom;
        float zoomY = Platform.IsMobile ? 0.8f : 1.0f;
        float pianoScaleX = 1.0f; // Only used by video export.
        float captureWaveTime = 0.0f;
        string noteTooltip = "";
        CaptureOperation captureOperation = CaptureOperation.None;
        EditionMode editMode = EditionMode.Channel;
        int highlightNoteAbsIndex = -1;
        int highlightDPCMSample = -1;
        NoteLocation captureNoteLocation;
        DateTime lastNoteCreateTime = DateTime.Now;

        // Note dragging support.
        int dragFrameMin = -1;
        int dragFrameMax = -1;
        int dragLastNoteValue = -1;
        SortedList<int, Note> dragNotes = new SortedList<int, Note>();

        // Pattern edit mode.
        int editChannel = -1;

        // Envelope edit mode.
        Instrument editInstrument = null;
        int editEnvelope;
        int envelopeValueOffset = 0;
        float envelopeValueZoom = 1;

        // Arpeggio edit mode
        Arpeggio editArpeggio = null;

        // Remembering last paste-special settings
        bool lastPasteSpecialPasteMix = false;
        bool lastPasteSpecialPasteNotes = true;
        int  lastPasteSpecialPasteEffectMask = Note.EffectAllMask;

        // DPCM editing mode
        int volumeEnvelopeDragVertex = -1;
        DPCMSample editSample = null;

        // When dragging samples
        DPCMSampleMapping draggedSample;

        // Video stuff
        Song videoSong;
        Color videoKeyColor;

        enum GizmoAction
        {
            ResizeNote,
            MoveRelease,
            MoveSlide,
            ChangeEnvValue,
            ChangeEffectValue,
            MoveVolumeSlideValue,
        };

        private class Gizmo
        {
            public Rectangle Rect;
            public BitmapAtlasRef FillImage = null;
            public BitmapAtlasRef Image;
            public GizmoAction Action;
        };

        public bool SnapAllowed { get => editMode == EditionMode.Channel; }
        public bool SnapEnabled { get => SnapAllowed && snap; set { if (SnapAllowed) snap = value; MarkDirty(); } }
        public bool EffectPanelExpanded { get => showEffectsPanel; set => SetShowEffectPanel(value); }
        public int  SnapResolution
        {
            get { Debug.Assert(editMode == EditionMode.Channel); return snapResolution; }
            set { Debug.Assert(editMode == EditionMode.Channel); snapResolution = value; MarkDirty(); }
        }

        public int SelectedEffect
        {
            get { Debug.Assert(editMode == EditionMode.Channel); return supportedEffects.Length > 0 ? selectedEffectIdx : -1; }
            set { Debug.Assert(editMode == EditionMode.Channel); selectedEffectIdx = value; MarkDirty(); }
        }

        public bool IsMaximized                => maximized;
        public bool IsEditingChannel           => editMode == EditionMode.Channel; 
        public bool IsEditingInstrument        => editMode == EditionMode.Enveloppe; 
        public bool IsEditingArpeggio          => editMode == EditionMode.Arpeggio;
        public bool IsEditingDPCMSample        => editMode == EditionMode.DPCM;
        public bool IsEditingDPCMSampleMapping => editMode == EditionMode.DPCMMapping;
        
        public bool CanCopy   => IsActiveControl && IsSelectionValid() && (editMode == EditionMode.Channel || editMode == EditionMode.Enveloppe || editMode == EditionMode.Arpeggio);
        public bool CanPaste  => IsActiveControl && IsSelectionValid() && (editMode == EditionMode.Channel && ClipboardUtils.ContainsNotes || (editMode == EditionMode.Enveloppe || editMode == EditionMode.Arpeggio) && ClipboardUtils.ContainsEnvelope);
        public bool CanDelete => IsActiveControl && IsSelectionValid() && (editMode == EditionMode.Channel || editMode == EditionMode.Enveloppe || editMode == EditionMode.Arpeggio || editMode == EditionMode.DPCM);
        public bool IsActiveControl => App != null && App.ActiveControl == this;

        public Instrument EditInstrument   => editInstrument;
        public Arpeggio   EditArpeggio     => editArpeggio;
        public DPCMSample EditSample       => editSample;
        public int        EditEnvelopeType => editEnvelope;

        public delegate void EmptyDelegate();
        public delegate void PatternDelegate(Pattern pattern);
        public delegate void NoteDelegate(Note note);
        public delegate void DPCMMappingDelegate(int note);

        public event PatternDelegate     PatternChanged;
        public event EmptyDelegate       MaximizedChanged;
        public event EmptyDelegate       ManyPatternChanged;
        public event EmptyDelegate       DPCMSampleChanged;
        public event EmptyDelegate       EnvelopeChanged;
        public event EmptyDelegate       NotesPasted;
        public event EmptyDelegate       ScrollChanged;
        public event NoteDelegate        NoteEyedropped;
        public event DPCMMappingDelegate DPCMSampleMapped;
        public event DPCMMappingDelegate DPCMSampleUnmapped;

        public PianoRoll()
        {
            UpdateRenderCoords();
        }

        private void UpdateRenderCoords()
        {
            var videoMode = editMode == EditionMode.VideoRecording;
            var headerScale = editMode == EditionMode.DPCMMapping || editMode == EditionMode.DPCM ? 1 : (editMode == EditionMode.VideoRecording ? 0 : 2);
            var scrollBarSize = Settings.ScrollBars == 1 ? DefaultScrollBarThickness1 : (Settings.ScrollBars == 2 ? DefaultScrollBarThickness2 : 0);
            var effectIconsScale = Platform.IsMobile ? 0.5f : 1.0f;

            minZoom = editMode == EditionMode.Channel && Song != null && Song.UsesFamiStudioTempo ? MinZoomFamiStudio : MinZoomOther;
            maxZoom = editMode == EditionMode.DPCM ? MaxWaveZoom : MaxZoom;
            zoom    = Utils.Clamp(zoom, minZoom, maxZoom);

            headerSizeY               = ScaleForMainWindow(DefaultHeaderSizeY * headerScale);
            effectButtonSizeY         = ScaleForMainWindow(DefaultEffectButtonSizeY * effectIconsScale);
            noteSizeX                 = ScaleForMainWindowFloat(DefaultNoteSizeX * zoom);
            noteSizeY                 = ScaleForMainWindow(DefaultNoteSizeY * zoomY);
            noteAttackSizeX           = ScaleForMainWindow(DefaultNoteAttackSizeX);
            releaseNoteSizeY          = ScaleForMainWindow(DefaultReleaseNoteSizeY * zoomY) & 0xfe; // Keep even
            pianoSizeX                = ScaleForMainWindow((videoMode || Platform.IsDesktop ? DefaultPianoSizeX    : DefaultPianoSizeXMobile)    * pianoScaleX);
            blackKeySizeX             = ScaleForMainWindow((videoMode || Platform.IsDesktop ? DefaultBlackKeySizeX : DefaultBlackKeySizeXMobile) * pianoScaleX);
            whiteKeySizeY             = ScaleForMainWindow(DefaultWhiteKeySizeY * zoomY);
            blackKeySizeY             = ScaleForMainWindow(DefaultBlackKeySizeY * zoomY);
            effectIconPosX            = ScaleForMainWindow(DefaultEffectIconPosX * effectIconsScale);
            effectIconPosY            = ScaleForMainWindow(DefaultEffectIconPosY * effectIconsScale);
            headerIconsPosX           = ScaleForMainWindow(headerScale == 1 ? DefaultSnapIconDpcmPosX : DefaultSnapIconPosX);
            headerIconsPosY           = ScaleForMainWindow(headerScale == 1 ? DefaultSnapIconDpcmPosY : DefaultSnapIconPosY);
            effectNamePosX            = ScaleForMainWindow(DefaultEffectNamePosX * effectIconsScale);
            beatTextPosX              = ScaleForMainWindow(DefaultBeatTextPosX);
            effectValuePosTextOffsetY = ScaleForFont(DefaultEffectValuePosTextOffsetY);
            effectValueNegTextOffsetY = ScaleForFont(DefaultEffectValueNegTextOffsetY);
            bigTextPosX               = ScaleForFont(DefaultBigTextPosX);
            bigTextPosY               = ScaleForFont(DefaultBigTextPosY);
            tooltipTextPosX           = ScaleForFont(DefaultTooltipTextPosX);
            tooltipTextPosY           = ScaleForFont(DefaultTooltipTextPosY);
            dpcmTextPosX              = ScaleForFont(DefaultDPCMTextPosX);
            recordingKeyOffsetY       = ScaleForMainWindow(DefaultRecordingKeyOffsetY);
            attackIconPosX            = ScaleForMainWindow(DefaultAttackIconPosX);
            waveGeometrySampleSize    = ScaleForMainWindow(DefaultWaveGeometrySampleSize);
            waveDisplayPaddingY       = ScaleForMainWindow(DefaultWaveDisplayPaddingY);
            scrollBarThickness        = ScaleForMainWindow(scrollBarSize);
            minScrollBarLength        = ScaleForMainWindow(DefaultMinScrollBarLength);
            scrollMargin              = ScaleForMainWindow(DefaultScrollMargin);
            noteResizeMargin          = ScaleForMainWindow(DefaultNoteResizeMargin);
            minPixelDistForLines      = ScaleForMainWindow(DefaultMinPixelDistForLines);
            envelopeValueSizeY        = ScaleForMainWindowFloat(DefaultEnvelopeSizeY * envelopeValueZoom);
            gizmoSize                 = ScaleForMainWindow(DefaultGizmoSize);

            // Make sure the effect panel actually fit on screen on mobile.
            if (Platform.IsMobile && ParentForm != null)
                effectPanelSizeY = Math.Min(ParentFormSize.Height / 2, ScaleForMainWindow(DefaultEffectPanelSizeY));
            else
                effectPanelSizeY = ScaleForMainWindow(DefaultEffectPanelSizeY);

            octaveSizeY = 12 * noteSizeY;
            headerAndEffectSizeY = headerSizeY + (showEffectsPanel ? effectPanelSizeY : 0);
            virtualSizeY = NumNotes * noteSizeY;
            pianoVisible = editMode == EditionMode.Channel || editMode == EditionMode.DPCMMapping || editMode == EditionMode.VideoRecording || Platform.IsDesktop;

            if (Platform.IsMobile && (editMode == EditionMode.Arpeggio || editMode == EditionMode.Enveloppe))
            {
                Envelope.GetMinMaxValueForType(editInstrument, editEnvelope, out int min, out int max);
                var maxValuesInScreen = editEnvelope == EnvelopeType.FdsWaveform || editEnvelope == EnvelopeType.FdsModulation ? 64 : 16;
                envelopeValueSizeY = (Height - headerAndEffectSizeY) / Math.Min(maxValuesInScreen, max - min + 1);
                virtualSizeY = (int)((max - min + 1) * envelopeValueSizeY);
            }

            if (!pianoVisible)
                pianoSizeX = 0;
        }

        public void StartEditChannel(int channelIdx, int patternIdx = 0)
        {
            editMode = EditionMode.Channel;
            editChannel = channelIdx;
            noteTooltip = "";

            var restoredScroll = RestoreChannelScroll();

            BuildSupportEffectList();
            ClearSelection();
            UpdateRenderCoords();
            if (!restoredScroll)
                CenterScroll(patternIdx);
            ClampScroll();
            MarkDirty();
        }

        public void ChangeChannel(int channelIdx)
        {
            if (editMode == EditionMode.Channel)
            {
                AbortCaptureOperation();
                editChannel = channelIdx;
                noteTooltip = "";
                BuildSupportEffectList();
                MarkDirty();
            }
        }

        public void StartEditInstrument(Instrument instrument, int envelope)
        {
            SaveChannelScroll();

            editMode = EditionMode.Enveloppe;
            editInstrument = instrument;
            editEnvelope = envelope;
            showEffectsPanel = false;
            noteTooltip = "";
            envelopeValueZoom = envelope == EnvelopeType.Volume || envelope == EnvelopeType.DutyCycle || envelope == EnvelopeType.N163Waveform ? 2 : 1;
            envelopeValueOffset = 0;
            Debug.Assert(editInstrument != null);

            ClearSelection();
            ClearHighlightedNote();
            UpdateRenderCoords();
            CenterEnvelopeScroll();
            ClampScroll();
            MarkDirty();
        }

        public void StartEditArpeggio(Arpeggio arpeggio)
        {
            SaveChannelScroll();

            editMode = EditionMode.Arpeggio;
            editEnvelope = EnvelopeType.Arpeggio;
            editInstrument = null;
            editArpeggio = arpeggio;
            showEffectsPanel = false;
            noteTooltip = "";
            envelopeValueZoom = 1;  
            envelopeValueOffset = 0;

            ClearSelection();
            ClearHighlightedNote();
            UpdateRenderCoords();
            CenterEnvelopeScroll();
            ClampScroll();
            MarkDirty();
        }

        public void StartEditDPCMSample(DPCMSample sample)
        {
            SaveChannelScroll();

            editMode = EditionMode.DPCM;
            editSample = sample;
            zoom = 1.0f;
            noteTooltip = "";
            envelopeValueZoom = 1;
            envelopeValueOffset = 0;

            ClearSelection();
            ClearHighlightedNote();
            UpdateRenderCoords();
            CenterWaveScroll();
            ClampScroll();
            MarkDirty();
        }

        public void StartEditDPCMMapping()
        {
            SaveChannelScroll();

            editMode = EditionMode.DPCMMapping;
            showEffectsPanel = false;
            zoom = 1.0f;
            noteTooltip = "";
            envelopeValueZoom = 1;
            envelopeValueOffset = 0;

            ClearSelection();
            ClearHighlightedNote();
            UpdateRenderCoords();
            CenterDPCMMappingScroll();
            ClampScroll();
            MarkDirty();
        }

        public void StartVideoRecording(Graphics g, Song song, float videoZoom, float pianoRollScaleX, float pianoRollScaleY, out int outNoteSizeY)
        {
            editChannel = 0;
            editMode = EditionMode.VideoRecording;
            videoSong = song;
            zoom = videoZoom;
            pianoScaleX = pianoRollScaleX;
            zoomY = pianoRollScaleY;

            UpdateRenderCoords();
            OnRenderInitialized(g);

            outNoteSizeY = noteSizeY;
        }

        public void EndVideoRecording()
        {
            OnRenderTerminated();
        }

        public void ApplySettings()
        {
            snapResolution = Settings.SnapResolution;
            SnapEnabled = Settings.SnapEnabled;
        }

        public void SaveSettings()
        {
            Settings.SnapResolution = snapResolution;
            Settings.SnapEnabled = SnapEnabled;
        }

        public void SaveChannelScroll()
        {
            if (Platform.IsMobile && editMode == EditionMode.Channel)
            {
                lastChannelScrollX = scrollX;
                lastChannelScrollY = scrollY;
                lastChannelZoom = zoom;
            }
        }

        public bool RestoreChannelScroll()
        {
            if (Platform.IsMobile && lastChannelScrollX >= 0 && lastChannelScrollY >= 0 && lastChannelZoom > 0)
            {
                scrollX = lastChannelScrollX;
                scrollY = lastChannelScrollY;
                zoom = lastChannelZoom;
                return true;
            }

            return false;
        }

        private void BuildSupportEffectList()
        {
            if (editChannel >= 0)
            {
                int cnt = 0;
                for (int i = 0; i < Note.EffectCount; i++)
                {
                    if (Song.Channels[editChannel].ShouldDisplayEffect(i))
                        cnt++;
                }

                supportedEffects = new int[cnt];
                for (int i = 0, j = 0; i < Note.EffectCount; i++)
                {
                    if (Song.Channels[editChannel].ShouldDisplayEffect(i))
                        supportedEffects[j++] = i;
                }

                if (Array.IndexOf(supportedEffects, selectedEffectIdx) == -1)
                    selectedEffectIdx = supportedEffects.Length > 0 ? supportedEffects[0] : -1;

                if (Platform.IsMobile && selectedEffectIdx < 0)
                    showEffectsPanel = false;
            }
        }

        private void CenterWaveScroll()
        {
            zoom = maxZoom;

            var duration = Math.Max(editSample.SourceDuration, editSample.ProcessedDuration);
            var viewSize = Width - pianoSizeX;
            var width    = (int)GetPixelForWaveTime(duration);

            while (width > viewSize && zoom > minZoom)
            {
                zoom /= 2;
                width /= 2;
            }

            scrollX = 0;
        }

        private void CenterEnvelopeScroll()
        {
            if (editMode == EditionMode.Arpeggio)
                CenterEnvelopeScroll(editArpeggio.Envelope, EnvelopeType.Arpeggio);
            else if (editMode == EditionMode.Enveloppe)
                CenterEnvelopeScroll(editInstrument.Envelopes[editEnvelope], editEnvelope, editInstrument);
        }

        private void CenterEnvelopeScroll(Envelope envelope, int envelopeType, Instrument instrument = null)
        {
            var baseNoteSizeX = ScaleForMainWindow(DefaultNoteSizeX);
            var envelopeLength = Math.Max(Platform.IsMobile ? 8 : 4, envelope.Length);

            zoom = minZoom;
            while (zoom < maxZoom && envelopeLength * baseNoteSizeX * (zoom * 2.0f) < (Width - pianoSizeX))
                zoom *= 2.0f;

            if (Platform.IsMobile)
                envelopeValueZoom = 1.0f;

            UpdateRenderCoords();

            Envelope.GetMinMaxValueForType(instrument, envelopeType, out var typeMin, out var typeMax);

            if (Platform.IsDesktop)
            {
                int midY = virtualSizeY - ((typeMin + typeMax) / 2 + 64 / (int)envelopeValueZoom) * (virtualSizeY / (128 / (int)envelopeValueZoom));

                scrollX = 0;
                scrollY = midY - Height / 2;
            }
            else
            {
                if (typeMax - typeMin > 15)
                {
                    envelope.GetMinMaxValue(out var envMin, out var envMax);
                    scrollY = (int)(virtualSizeY - envelopeValueSizeY * (((envMax + envMin) / 2) - typeMin + 1) - (Height - headerAndEffectSizeY) / 2);
                }
                else
                {
                    scrollY = 0;
                }

                scrollX = 0;
            }

            ClampScroll();
        }

        private void CenterDPCMMappingScroll()
        {
            scrollY = Math.Max(virtualSizeY + headerAndEffectSizeY - Height, 0) / 2;
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

        private int GetAbsoluteNoteIndexForPixel(int x, bool scroll = true)
        {
            if (scroll)
                x += scrollX;
            return (int)(x / (double)noteSizeX);
        }

        private void CenterScroll(int patternIdx = 0)
        {
            var maxScrollY = Math.Max(virtualSizeY + headerAndEffectSizeY - Height, 0);

            scrollX = GetPixelForNote(Song.GetPatternStartAbsoluteNoteIndex(patternIdx), false);
            scrollY = maxScrollY / 2;

            var channel = Song.Channels[editChannel];
            var note = channel.FindPatternFirstMusicalNote(patternIdx);

            if (note != null)
            {
                int noteY = virtualSizeY - note.Value * noteSizeY;
                scrollY = noteY - (Height - headerAndEffectSizeY) / 2;
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

        private Song Song
        {
            get { return videoSong != null ? videoSong : App?.SelectedSong; }
        }

        private Envelope EditEnvelope
        {
            get
            {
                if (editMode == EditionMode.Enveloppe)
                    return editInstrument?.Envelopes[(int)editEnvelope];
                else if (editMode == EditionMode.Arpeggio)
                    return editArpeggio.Envelope;
                else
                    return null;
            }
        }

        public void HighlightPianoNote(int note)
        {
            if (note != highlightNote)
            {
                highlightNote = note;
                MarkDirty();
            }
        }

        public void Reset(int channelIdx)
        {
            // At this point, this is just a more agressive StartEditChannel().
            AbortCaptureOperation();
            showEffectsPanel = false;
            editInstrument = null;
            editArpeggio = null;
            zoom = DefaultChannelZoom;
            scrollX = 0;
            scrollY = 0;
            lastChannelScrollX = -1;
            lastChannelScrollY = -1;
            lastChannelZoom = -1;
            StartEditChannel(channelIdx);
        }

        public void SongModified()
        {
            ClearSelection();
            UpdateRenderCoords();
            MarkDirty();
        }

        public void SongChanged(int channelIdx)
        {
            if (editMode == EditionMode.Channel)
            {
                editMode = EditionMode.Channel;
                editChannel = channelIdx;
                editInstrument = null;
                editArpeggio = null;
                showEffectsPanel = false;
                ClearSelection();
                UpdateRenderCoords();
            }
        }

        protected override void OnRenderInitialized(Graphics g)
        {
            UpdateRenderCoords();

            Debug.Assert(EffectImageNames.Length == Note.EffectCount + 1);

            whiteKeyBrush = g.CreateHorizontalGradientBrush(0, pianoSizeX, Theme.LightGreyFillColor1, Theme.LightGreyFillColor2);
            blackKeyBrush = g.CreateHorizontalGradientBrush(0, blackKeySizeX, Theme.DarkGreyFillColor1, Theme.DarkGreyFillColor2);
            whiteKeyPressedBrush = g.CreateHorizontalGradientBrush(0, pianoSizeX, Theme.Darken(Theme.LightGreyFillColor1), Theme.Darken(Theme.LightGreyFillColor2));
            blackKeyPressedBrush = g.CreateHorizontalGradientBrush(0, blackKeySizeX, Theme.Lighten(Theme.DarkGreyFillColor1), Theme.Lighten(Theme.DarkGreyFillColor2));
            frameLineBrush = g.CreateSolidBrush(Color.FromArgb(128, Theme.DarkGreyLineColor2));
            debugBrush = g.CreateSolidBrush(Theme.GreenColor);
            seekBarBrush = g.CreateSolidBrush(Theme.SeekBarColor);
            seekBarRecBrush = g.CreateSolidBrush(Theme.DarkRedFillColor);
            selectionBgVisibleBrush = g.CreateSolidBrush(Color.FromArgb(64, Theme.LightGreyFillColor1));
            selectionBgInvisibleBrush = g.CreateSolidBrush(Color.FromArgb(16, Theme.LightGreyFillColor1));
            selectionNoteBrush = g.CreateSolidBrush(Theme.LightGreyFillColor1);
            highlightNoteBrush = g.CreateSolidBrush(Theme.WhiteColor);
            attackBrush = g.CreateSolidBrush(Color.FromArgb(128, Theme.BlackColor));
            attackBrushForceDisplay = g.CreateSolidBrush(Color.FromArgb(64, Theme.BlackColor));
            iconTransparentBrush = g.CreateSolidBrush(Color.FromArgb(92, Theme.DarkGreyLineColor2));
            invalidDpcmMappingBrush = g.CreateSolidBrush(Color.FromArgb(64, Theme.BlackColor));
            volumeSlideBarFillBrush = g.CreateSolidBrush(Color.FromArgb(64, Theme.LightGreyFillColor1));
            fontSmallCharSizeX = ThemeResources != null ? ThemeResources.FontSmall.MeasureString("0", false) : 1;
            bmpLoopSmallFill = g.GetBitmapAtlasRef("LoopSmallFill");
            bmpReleaseSmallFill = g.GetBitmapAtlasRef("ReleaseSmallFill");
            bmpEnvResize = g.GetBitmapAtlasRef("EnvResize");
            bmpExpandedSmall = g.GetBitmapAtlasRef("ExpandedSmall");
            bmpCollapsedSmall = g.GetBitmapAtlasRef("CollapsedSmall");
            bmpMaximize = g.GetBitmapAtlasRef("Maximize");
            bmpSnap = g.GetBitmapAtlasRef("Snap");
            bmpSnapOff = g.GetBitmapAtlasRef("SnapOff");
            bmpGizmoResizeLeftRight = g.GetBitmapAtlasRef("GizmoResizeLeftRight");
            bmpGizmoResizeUpDown = g.GetBitmapAtlasRef("GizmoResizeUpDown");
            bmpGizmoResizeFill = g.GetBitmapAtlasRef("GizmoResizeFill");
            bmpEffects = g.GetBitmapAtlasRefs(EffectImageNames);

            if (Platform.IsMobile)
            {
                bitmapScale = g.WindowScaling * 0.5f;
                effectBitmapScale = g.WindowScaling * 0.25f;
            }

            seekGeometry = g.CreateGeometry(new float[,]
            {
                { -headerSizeY / 4, 1 },
                { 0, headerSizeY / 2 - 2 },
                { headerSizeY / 4, 1 }
            });

            sampleGeometry = g.CreateGeometry(new float[,]
            {
                { -waveGeometrySampleSize, -waveGeometrySampleSize },
                {  waveGeometrySampleSize, -waveGeometrySampleSize },
                {  waveGeometrySampleSize,  waveGeometrySampleSize },
                { -waveGeometrySampleSize,  waveGeometrySampleSize }
            });

            ConditionalUpdateNoteGeometries(g);
        }

        protected override void OnRenderTerminated()
        {
            Utils.DisposeAndNullify(ref whiteKeyBrush);
            Utils.DisposeAndNullify(ref blackKeyBrush);
            Utils.DisposeAndNullify(ref whiteKeyPressedBrush);
            Utils.DisposeAndNullify(ref blackKeyPressedBrush);
            Utils.DisposeAndNullify(ref frameLineBrush);
            Utils.DisposeAndNullify(ref debugBrush);
            Utils.DisposeAndNullify(ref seekBarBrush);
            Utils.DisposeAndNullify(ref seekBarRecBrush);
            Utils.DisposeAndNullify(ref selectionBgVisibleBrush);
            Utils.DisposeAndNullify(ref selectionBgInvisibleBrush);
            Utils.DisposeAndNullify(ref selectionNoteBrush);
            Utils.DisposeAndNullify(ref highlightNoteBrush);
            Utils.DisposeAndNullify(ref attackBrush);
            Utils.DisposeAndNullify(ref attackBrushForceDisplay);
            Utils.DisposeAndNullify(ref iconTransparentBrush);
            Utils.DisposeAndNullify(ref invalidDpcmMappingBrush);
            Utils.DisposeAndNullify(ref volumeSlideBarFillBrush);
            Utils.DisposeAndNullify(ref stopNoteGeometry[0]);
            Utils.DisposeAndNullify(ref stopNoteGeometry[1]);
            Utils.DisposeAndNullify(ref releaseNoteGeometry[0]);
            Utils.DisposeAndNullify(ref releaseNoteGeometry[1]);
            Utils.DisposeAndNullify(ref stopReleaseNoteGeometry[0]);
            Utils.DisposeAndNullify(ref stopReleaseNoteGeometry[1]);
            Utils.DisposeAndNullify(ref slideNoteGeometry);
            Utils.DisposeAndNullify(ref seekGeometry);
            Utils.DisposeAndNullify(ref sampleGeometry);
        }

        private void ConditionalUpdateNoteGeometries(Graphics g)
        {
            if (geometryNoteSizeY == noteSizeY)
                return;

            geometryNoteSizeY = noteSizeY;

            Utils.DisposeAndNullify(ref stopNoteGeometry[0]);
            Utils.DisposeAndNullify(ref stopNoteGeometry[1]);
            Utils.DisposeAndNullify(ref releaseNoteGeometry[0]);
            Utils.DisposeAndNullify(ref releaseNoteGeometry[1]);
            Utils.DisposeAndNullify(ref stopReleaseNoteGeometry[0]);
            Utils.DisposeAndNullify(ref stopReleaseNoteGeometry[1]);
            Utils.DisposeAndNullify(ref slideNoteGeometry);
            
            stopNoteGeometry[0] = g.CreateGeometry(new float[,]
            {
                { 0.0f, 0 },
                { 0.0f, noteSizeY },
                { 1.0f, noteSizeY / 2 }
            });

            stopNoteGeometry[1] = g.CreateGeometry(new float[,]
            {
                { 0.0f, 1 },
                { 0.0f, noteSizeY },
                { 1.0f, noteSizeY / 2 }
            });

            releaseNoteGeometry[0] = g.CreateGeometry(new float[,]
            {
                { 0.0f, 0 },
                { 0.0f, noteSizeY },
                { 1.0f, noteSizeY - noteSizeY / 2 + releaseNoteSizeY / 2 },
                { 1.0f, noteSizeY / 2 - releaseNoteSizeY / 2 }
            });

            releaseNoteGeometry[1] = g.CreateGeometry(new float[,]
            {
                { 0.0f, 1 },
                { 0.0f, noteSizeY },
                { 1.0f, noteSizeY - noteSizeY / 2 + releaseNoteSizeY / 2 },
                { 1.0f, noteSizeY / 2 - releaseNoteSizeY / 2 + 1 }
            });

            stopReleaseNoteGeometry[0] = g.CreateGeometry(new float[,]
            {
                { 0.0f, noteSizeY / 2 - releaseNoteSizeY / 2 },
                { 0.0f, noteSizeY / 2 + releaseNoteSizeY / 2 },
                { 1.0f, noteSizeY / 2 }
            });

            stopReleaseNoteGeometry[1] = g.CreateGeometry(new float[,]
            {
                { 0.0f, noteSizeY / 2 - releaseNoteSizeY / 2 + 1 },
                { 0.0f, noteSizeY / 2 + releaseNoteSizeY / 2 },
                { 1.0f, noteSizeY / 2 }
            });

            slideNoteGeometry = g.CreateGeometry(new float[,]
            {
                { 0.0f, 0 },
                { 1.0f, 0 },
                { 1.0f, noteSizeY }
            });
        }

        private bool IsBlackKey(int key)
        {
            return key == 1 || key == 3 || key == 6 || key == 8 || key == 10;
        }

        private Rectangle GetKeyRectangle(int octave, int key)
        {
            if (IsBlackKey(key))
            {
                return new Rectangle(
                    editMode == EditionMode.VideoRecording ? pianoSizeX - blackKeySizeX : 0,
                    virtualSizeY - octaveSizeY * octave - (key + 1) * noteSizeY - scrollY,
                    blackKeySizeX,
                    blackKeySizeY);
            }
            else
            {
                int keySizeY = key > 4 ? (noteSizeY * 12 - whiteKeySizeY * 3) / 4 : whiteKeySizeY;

                return new Rectangle(
                    0,
                    virtualSizeY - octaveSizeY * octave - (key <= 4 ? ((key / 2 + 1) * whiteKeySizeY) : ((whiteKeySizeY * 3) + ((key - 4) / 2 + 1) * keySizeY)) - scrollY,
                    pianoSizeX,
                    keySizeY);
            }
        }

        public bool GetViewRange(ref int minNoteIdx, ref int maxNoteIdx, ref int channelIndex)
        {
            if (editMode == EditionMode.Channel && Width > pianoSizeX)
            {
                minNoteIdx = Math.Max(GetAbsoluteNoteIndexForPixel(0), 0);
                maxNoteIdx = Math.Min(GetAbsoluteNoteIndexForPixel(Width - pianoSizeX) + 1, Song.GetPatternStartAbsoluteNoteIndex(Song.Length));
                channelIndex = editChannel;

                return true;
            }
            else
            {
                return false;
            }
        }

        private Brush GetSeekBarBrush()
        {
            return (editMode == EditionMode.Channel && App.IsRecording) ? seekBarRecBrush : seekBarBrush;
        }

        private void ForEachWaveTimecode(RenderInfo r, Action<float, float, int, int> function)
        {
            var textSize  = r.g.MeasureString("99.999", ThemeResources.FontMedium);
            var waveWidth = Width - pianoSizeX;
            var numLabels = Math.Floor(waveWidth / textSize);

            for (int i = 2; i >= 0; i--)
            {
                var divTime = Math.Pow(10.0, -i - 1);

                var minLabel = (int)Math.Floor  (r.minVisibleWaveTime / divTime);
                var maxLabel = (int)Math.Ceiling(r.maxVisibleWaveTime / divTime);

                if (i == 0 || numLabels > (maxLabel - minLabel))
                {
                    for (var t = minLabel; t <= maxLabel; t++)
                    {
                        var time = t * divTime;
                        var x = GetPixelForWaveTime((float)time, scrollX);

                        function((float)time, x, i, t);
                    }

                    break;
                }
            }
        }

        public int GetSeekFrameToDraw()
        {
            return captureOperation == CaptureOperation.DragSeekBar ? dragSeekPosition : App.CurrentFrame;
        }

        class RenderInfo
        {
            public int maxVisibleNote;
            public int minVisibleNote;
            public int maxVisibleOctave;
            public int minVisibleOctave;
            public int minVisiblePattern;
            public int maxVisiblePattern;
            public float minVisibleWaveTime;
            public float maxVisibleWaveTime;

            public Graphics g;
            public CommandList cc; // Top left (corner, header list)
            public CommandList ch; // Top right (header, effect panel)
            public CommandList cz; // Top right (effect panel gizmos)
            public CommandList cp; // Left side (piano area)
            public CommandList cb; // Right side (note area) background
            public CommandList cf; // Right side (note area) foreground
            public CommandList cg; // Right side (note area) mobile gizmos.
            public CommandList cs; // Scroll bars
            public CommandList cd; // Debug
        }

        private void RenderHeader(RenderInfo r)
        {
            r.ch.PushTranslation(pianoSizeX, 0);

            if ((editMode == EditionMode.Enveloppe || editMode == EditionMode.Arpeggio) && EditEnvelope != null)
            {
                var env = EditEnvelope;
                var iconPos = (headerSizeY / 2 - ScaleCustom(bmpLoopSmallFill.ElementSize.Width, bitmapScale)) / 2;

                r.ch.PushTranslation(0, headerSizeY / 2);

                DrawSelectionRect(r.ch, headerSizeY);

                if (env.Loop >= 0)
                {
                    r.ch.PushTranslation(GetPixelForNote(env.Loop), 0);
                    r.ch.FillRectangle(0, 0, GetPixelForNote(((env.Release >= 0 ? env.Release : env.Length) - env.Loop), false), headerAndEffectSizeY, ThemeResources.DarkGreyFillBrush2);
                    r.ch.DrawLine(0, 0, 0, headerAndEffectSizeY, ThemeResources.BlackBrush);
                    r.ch.DrawBitmapAtlas(bmpLoopSmallFill, iconPos + 1, iconPos, 1.0f, bitmapScale, Theme.LightGreyFillColor1);
                    r.ch.PopTransform();
                }
                if (env.Release >= 0)
                {
                    r.ch.PushTranslation(GetPixelForNote(env.Release), 0);
                    r.ch.DrawLine(0, 0, 0, headerAndEffectSizeY, ThemeResources.BlackBrush);
                    r.ch.DrawBitmapAtlas(bmpReleaseSmallFill, iconPos + 1, iconPos, 1.0f, bitmapScale, Theme.LightGreyFillColor1);
                    r.ch.PopTransform();
                }
                if (env.Length > 0)
                {
                    r.ch.PushTranslation(GetPixelForNote(env.Length), 0);
                    r.ch.DrawLine(0, 0, 0, headerAndEffectSizeY, ThemeResources.BlackBrush);
                    r.ch.PopTransform();
                }

                r.ch.DrawLine(0, headerSizeY - 1, Width, headerSizeY - 1, ThemeResources.BlackBrush);
                r.ch.PopTransform();

                if (env.CanResize)
                {
                    r.ch.PushTranslation(GetPixelForNote(env.Length), 0);
                    r.ch.DrawBitmapAtlas(bmpEnvResize, iconPos + 1, iconPos, 1.0f, bitmapScale, Theme.LightGreyFillColor1);
                    r.ch.PopTransform();
                }

                DrawSelectionRect(r.ch, headerSizeY);

                // Draw the header bars
                for (int n = 0; n <= env.Length; n++)
                {
                    int x = GetPixelForNote(n);
                    if (x != 0)
                    {
                        r.ch.DrawLine(x, 0, x, headerSizeY / 2, ThemeResources.BlackBrush, 1);
                    }
                    if (n != env.Length)
                    {
                        var label = editEnvelope == EnvelopeType.N163Waveform ? editInstrument.N163WavePos + n : n;
                        var labelString = label.ToString();
                        if (labelString.Length * fontSmallCharSizeX + 2 < noteSizeX)
                            r.ch.DrawText(labelString, ThemeResources.FontMedium, x, 0, ThemeResources.LightGreyFillBrush1, TextFlags.MiddleCenter, noteSizeX, headerSizeY / 2 - 1);
                    }
                }

                r.ch.DrawLine(0, headerSizeY / 2 - 1, Width, headerSizeY / 2 - 1, ThemeResources.BlackBrush);
            }
            else if (editMode == EditionMode.Channel)
            {
                // Draw colored header
                for (int p = r.minVisiblePattern; p < r.maxVisiblePattern; p++)
                {
                    var pattern = Song.Channels[editChannel].PatternInstances[p];
                    if (pattern != null)
                    {
                        int sx = GetPixelForNote(Song.GetPatternLength(p), false);
                        int px = GetPixelForNote(Song.GetPatternStartAbsoluteNoteIndex(p), true);
                        r.ch.FillRectangle(px, headerSizeY / 2, px + sx, headerSizeY, ThemeResources.CustomColorBrushes[pattern.Color]);
                    }
                }

                DrawSelectionRect(r.ch, headerSizeY);

                var beatLabelSizeX = r.g.MeasureString("88.88", ThemeResources.FontMedium);

                // Draw the header bars
                for (int p = r.minVisiblePattern; p < r.maxVisiblePattern; p++)
                {
                    var patternLen = Song.GetPatternLength(p);

                    var sx = GetPixelForNote(patternLen, false);
                    var px = GetPixelForNote(Song.GetPatternStartAbsoluteNoteIndex(p), true);
                    if (p != 0)
                        r.ch.DrawLine(px, 0, px, headerSizeY, ThemeResources.BlackBrush, 3);

                    var pattern = Song.Channels[editChannel].PatternInstances[p];
                    var beatLen = Song.GetPatternBeatLength(p);
                    var beatSizeX = GetPixelForNote(beatLen, false);

                    // Is there enough room to draw beat labels?
                    if ((beatSizeX + beatTextPosX) > beatLabelSizeX)
                    {
                        var numBeats = (int)Math.Ceiling(patternLen / (float)beatLen);
                        for (int i = 0; i < numBeats; i++)
                            r.ch.DrawText($"{p + 1}.{i + 1}", ThemeResources.FontMedium, px + beatTextPosX + beatSizeX * i, 0, ThemeResources.LightGreyFillBrush1, TextFlags.Middle, 0, headerSizeY / 2 - 1);
                    }
                    else
                    {
                        r.ch.DrawText((p + 1).ToString(), ThemeResources.FontMedium, px, 0, ThemeResources.LightGreyFillBrush1, TextFlags.MiddleCenter, sx, headerSizeY / 2 - 1);
                    }

                    if (pattern != null)
                        r.ch.DrawText(pattern.Name, ThemeResources.FontMedium, px, headerSizeY / 2, ThemeResources.BlackBrush, TextFlags.MiddleCenter | TextFlags.Clip, sx, headerSizeY / 2 - 1);
                }

                int maxX = GetPixelForNote(Song.GetPatternStartAbsoluteNoteIndex(r.maxVisiblePattern));
                r.ch.DrawLine(maxX, 0, maxX, Height, ThemeResources.BlackBrush, 3);
                r.ch.DrawLine(0, headerSizeY / 2 - 1, Width, headerSizeY / 2 - 1, ThemeResources.BlackBrush);
            }
            else if (editMode == EditionMode.DPCM)
            {
                // Selection rectangle
                if (IsSelectionValid())
                {
                    r.ch.FillRectangle(
                        GetPixelForWaveTime(GetWaveTimeForSample(selectionMin, true),  scrollX), 0,
                        GetPixelForWaveTime(GetWaveTimeForSample(selectionMax, false), scrollX), headerSizeY, selectionBgVisibleBrush);
                }

                ForEachWaveTimecode(r, (time, x, level, idx) =>
                {
                    if (time != 0.0f)
                        r.ch.DrawText(time.ToString($"F{level + 1}"), ThemeResources.FontMedium, x - 100, 0, ThemeResources.LightGreyFillBrush1, TextFlags.MiddleCenter, 200, headerSizeY - 1);
                });

                // Processed Range
                var processedBrush = r.g.GetSolidBrush(editSample.Color, 1.0f, 0.25f);
                r.ch.FillRectangle(
                    GetPixelForWaveTime(editSample.ProcessedStartTime, scrollX), 0,
                    GetPixelForWaveTime(editSample.ProcessedEndTime,   scrollX), headerSizeY, processedBrush);
            }

            r.ch.DrawLine(0, headerSizeY - 1, Width, headerSizeY - 1, ThemeResources.BlackBrush);

            if (((editMode == EditionMode.Enveloppe || editMode == EditionMode.Arpeggio) && editEnvelope < EnvelopeType.RegularCount) || (editMode == EditionMode.Channel))
            {
                var seekFrame = editMode == EditionMode.Enveloppe || editMode == EditionMode.Arpeggio ? App.GetEnvelopeFrame(editInstrument, editEnvelope, editMode == EditionMode.Arpeggio) : GetSeekFrameToDraw();
                if (seekFrame >= 0)
                {
                    r.ch.PushTranslation(GetPixelForNote(seekFrame), 0);
                    r.ch.FillAndDrawGeometry(seekGeometry, GetSeekBarBrush(), ThemeResources.BlackBrush, 1, true);
                    r.ch.DrawLine(0, headerSizeY / 2, 0, headerSizeY, GetSeekBarBrush(), 3);
                    r.ch.PopTransform();
                }
            }

            r.ch.PopTransform();
        }

        private void RenderEffectList(RenderInfo r)
        {
            r.cc.FillRectangle(0, 0, pianoSizeX, headerAndEffectSizeY, ThemeResources.DarkGreyFillBrush1);
            r.cc.DrawLine(pianoSizeX - 1, 0, pianoSizeX - 1, headerAndEffectSizeY, ThemeResources.BlackBrush);

            if (!Platform.IsMobile && editMode != EditionMode.VideoRecording)
            {
                var maxRect = GetMaximizeButtonRect();
                r.cc.DrawBitmapAtlas(bmpMaximize, maxRect.X, maxRect.Y, 1.0f, 1.0f, maximized ? Theme.LightGreyFillColor1 : Theme.MediumGreyFillColor1);
            }

            // Effect icons
            if (editMode == EditionMode.Channel)
            {
                var toggleRect = GetToggleEffectPannelButtonRect();
                r.cc.DrawBitmapAtlas(showEffectsPanel ? bmpExpandedSmall : bmpCollapsedSmall, toggleRect.X, toggleRect.Y, 1.0f, bitmapScale, Theme.LightGreyFillColor1);

                if (SnapAllowed && !Platform.IsMobile)
                {
                    var snapBtnRect = GetSnapButtonRect();
                    var snapResRect = GetSnapResolutionRect();

                    r.cc.DrawBitmapAtlas(SnapEnabled || App.IsRecording ? bmpSnap : bmpSnapOff, snapBtnRect.X, snapBtnRect.Y, 1.0f, 1.0f, App.IsRecording ? Theme.DarkRedFillColor : (SnapEnabled ? Theme.LightGreyFillColor1 : Theme.MediumGreyFillColor1));
                    r.cc.DrawText(SnapResolutionType.Names[snapResolution], ThemeResources.FontSmall, snapResRect.X, snapResRect.Y, App.IsRecording ? ThemeResources.DarkRedFillBrush : (SnapEnabled ? ThemeResources.LightGreyFillBrush2 : ThemeResources.MediumGreyFillBrush1), TextFlags.Right | TextFlags.Middle, snapResRect.Width, snapResRect.Height);
                }

                if (showEffectsPanel)
                {
                    r.cc.PushTranslation(0, headerSizeY);

                    int effectButtonY = 0;

                    for (int i = 0; i < supportedEffects.Length; i++)
                    {
                        var effectIdx = supportedEffects[i];

                        if (Platform.IsMobile && effectIdx != selectedEffectIdx)
                            continue;

                        r.cc.PushTranslation(0, effectButtonY);
                        r.cc.DrawLine(0, -1, pianoSizeX, -1, ThemeResources.BlackBrush);
                        
                        r.cc.DrawBitmapAtlas(bmpEffects[effectIdx], effectIconPosX, effectIconPosY, 1.0f, effectBitmapScale, Theme.LightGreyFillColor1);
                        r.cc.DrawText(Note.EffectNames[effectIdx], selectedEffectIdx == effectIdx ? ThemeResources.FontSmallBold : ThemeResources.FontSmall, effectNamePosX, 0, ThemeResources.LightGreyFillBrush2, TextFlags.Middle, 0, effectButtonSizeY);
                        r.cc.PopTransform();

                        effectButtonY += effectButtonSizeY;
                    }

                    r.cc.PushTranslation(0, effectButtonY);
                    r.cc.DrawLine(0, -1, pianoSizeX, -1, ThemeResources.BlackBrush);
                    r.cc.PopTransform();
                    r.cc.PopTransform();
                }
            }
            else if (editMode == EditionMode.DPCM)
            {
                r.cc.DrawBitmapAtlas(showEffectsPanel ? bmpExpandedSmall : bmpCollapsedSmall, 0, 0, 1.0f, bitmapScale, Theme.LightGreyFillColor1);

                if (showEffectsPanel)
                {
                    r.cc.PushTranslation(0, headerSizeY);
                    r.cc.DrawLine(0, -1, pianoSizeX, -1, ThemeResources.BlackBrush);
                    
                    r.cc.DrawBitmapAtlas(bmpEffects[Note.EffectVolume], effectIconPosX, effectIconPosY, 1.0f, effectBitmapScale, Theme.LightGreyFillColor1);
                    r.cc.DrawText(Note.EffectNames[Note.EffectVolume], ThemeResources.FontSmallBold, effectNamePosX, 0, ThemeResources.LightGreyFillBrush2, TextFlags.Middle, 0, effectButtonSizeY);
                    r.cc.PopTransform();

                    r.cc.PushTranslation(0, effectButtonSizeY);
                    r.cc.DrawLine(0, -1, pianoSizeX, -1, ThemeResources.BlackBrush);
                    r.cc.PopTransform();
                }
            }

            r.cc.DrawLine(0, headerAndEffectSizeY - 1, pianoSizeX, headerAndEffectSizeY - 1, ThemeResources.BlackBrush);
        }

        private bool GetDPCMKeyColor(int note, ref Color color)
        {
            if (editMode != EditionMode.VideoRecording && App.SelectedChannel.Type == ChannelType.Dpcm)
            {
                var mapping = Song.Project.GetDPCMMapping(note);
                if (mapping != null)
                {
                    color = mapping.Sample.Color;
                    return true;
                }
            }

            return false;
        }

        private void RenderPiano(RenderInfo r)
        {
            if (!pianoVisible)
                return;

            r.cp.PushTranslation(0, headerAndEffectSizeY);
            r.cp.FillRectangle(0, 0, pianoSizeX, Height, whiteKeyBrush);

            var playOctave = -1;
            var playNote = -1;
            var draggingNote = captureOperation == CaptureOperation.DragNote || captureOperation == CaptureOperation.CreateNote;
            var dragOctave = (dragLastNoteValue - 1) / 12;
            var dragNote = (dragLastNoteValue - 1) % 12;
            
            if (Note.IsMusicalNote(highlightNote))
            {
                playOctave = (highlightNote - 1) / 12;
                playNote   = (highlightNote - 1) - playOctave * 12;

                if (!IsBlackKey(playNote))
                    r.cp.FillRectangle(GetKeyRectangle(playOctave, playNote), whiteKeyPressedBrush);
            }

            if (draggingNote && !IsBlackKey(dragNote))
            {
                r.cp.FillRectangle(GetKeyRectangle(dragOctave, dragNote), whiteKeyPressedBrush);
            }

            var drawDpcmColorKeys = (editMode == EditionMode.Channel && Song.Channels[editChannel].Type == ChannelType.Dpcm) || editMode == EditionMode.DPCMMapping;
            var color = Color.Empty;

            // Early pass for DPCM white keys.
            if (drawDpcmColorKeys)
            {
                for (int i = r.minVisibleOctave; i < r.maxVisibleOctave; i++)
                {
                    for (int j = 0; j < 12; j++)
                    {
                        if (!IsBlackKey(j) && GetDPCMKeyColor(i * 12 + j + 1, ref color))
                            r.cp.FillRectangle(GetKeyRectangle(i, j), r.g.GetHorizontalGradientBrush(Theme.Darken(color, 20), color, pianoSizeX));
                    }
                }
            }

            // Draw the piano
            for (int i = r.minVisibleOctave; i < r.maxVisibleOctave; i++)
            {
                var octaveBaseY = (virtualSizeY - octaveSizeY * i) - scrollY;

                for (int j = 0; j < 12; j++)
                {
                    var noteIdx = i * 12 + j;
                    if (noteIdx >= NumNotes)
                        break;

                    if (IsBlackKey(j))
                    {
                        if (drawDpcmColorKeys && GetDPCMKeyColor(noteIdx + 1, ref color))
                            r.cp.FillAndDrawRectangle(GetKeyRectangle(i, j), r.g.GetHorizontalGradientBrush(Theme.Darken(color, 40), Theme.Darken(color, 20), blackKeySizeX), ThemeResources.BlackBrush);
                        else
                            r.cp.FillRectangle(GetKeyRectangle(i, j), blackKeyBrush);

                        if ((i == playOctave && j == playNote) || (draggingNote && (i == dragOctave && j == dragNote)))
                            r.cp.FillRectangle(GetKeyRectangle(i, j), blackKeyPressedBrush);
                    }

                    int y = octaveBaseY - j * noteSizeY;
                    if (j == 0)
                        r.cp.DrawLine(0, y, pianoSizeX, y, ThemeResources.BlackBrush);
                    else if (j == 5)
                        r.cp.DrawLine(0, y, pianoSizeX, y, ThemeResources.BlackBrush);
                }

                if ((editMode == EditionMode.Channel || editMode == EditionMode.DPCMMapping) && ThemeResources.FontSmall.Size < noteSizeY)
                    r.cp.DrawText("C" + i, ThemeResources.FontSmall, r.g.WindowScaling, octaveBaseY - noteSizeY + 1, ThemeResources.BlackBrush, TextFlags.Middle, pianoSizeX - r.g.WindowScaling * 2, noteSizeY - 1);
            }

            if (App != null && (App.IsRecording || App.IsQwertyPianoEnabled) && Platform.IsDesktop)
            {
                var showQwerty = App.IsRecording || App.IsQwertyPianoEnabled;
                var keyStrings = new string[Note.MusicalNoteMax];

                foreach (var kv in Settings.KeyCodeToNoteMap)
                {
                    var i = kv.Value - 1;
                    var k = kv.Key;

                    if (i < 0 || i >= keyStrings.Length)
                        continue;

                    if (keyStrings[i] == null)
                        keyStrings[i] = Platform.KeyCodeToString(k);
                    else
                        keyStrings[i] += $"   {Platform.KeyCodeToString(k)}";
                }

                for (int i = 0; i < Note.MusicalNoteMax; i++)
                {
                    if (keyStrings[i] == null)
                        continue;

                    int octaveBaseY = (virtualSizeY - octaveSizeY * ((i / 12) + App.BaseRecordingOctave)) - scrollY;
                    int y = octaveBaseY - (i % 12) * noteSizeY;

                    Brush brush;
                    if (App.IsRecording)
                        brush = IsBlackKey(i % 12) ? ThemeResources.LightRedFillBrush : ThemeResources.DarkRedFillBrush;
                    else
                        brush = IsBlackKey(i % 12) ? ThemeResources.LightGreyFillBrush2 : ThemeResources.BlackBrush;

                    r.cp.DrawText(keyStrings[i], ThemeResources.FontVerySmall, 0, y - recordingKeyOffsetY + 1, brush, TextFlags.MiddleCenter, blackKeySizeX, noteSizeY - 1);
                }
            }

            r.cp.DrawLine(pianoSizeX - 1, 0, pianoSizeX - 1, Height, ThemeResources.BlackBrush);
            r.cp.PopTransform();
        }

        private void RenderEffectPanel(RenderInfo r)
        {
            if ((editMode == EditionMode.Channel || editMode == EditionMode.DPCM) && showEffectsPanel)
            {
                r.ch.PushTranslation(pianoSizeX, headerSizeY);

                if (editMode == EditionMode.Channel)
                {
                    var song = Song;
                    var channel = song.Channels[editChannel];

                    var minLocation = new NoteLocation(r.minVisiblePattern, 0);
                    var maxLocation = new NoteLocation(r.maxVisiblePattern, 0);

                    var singleFrameSlides = new HashSet<NoteLocation>();

                    // Draw the effects current value rectangles. Not all effects need this.
                    if (selectedEffectIdx >= 0 && Note.EffectWantsPreviousValue(selectedEffectIdx))
                    {
                        var lastFrame = -1;
                        var lastValue = channel.GetCachedLastValidEffectValue(r.minVisiblePattern - 1, selectedEffectIdx, out var lastValueLocation);
                        var minValue = Note.GetEffectMinValue(song, channel, selectedEffectIdx);
                        var maxValue = Note.GetEffectMaxValue(song, channel, selectedEffectIdx);

                        // Special case for volume, since it can have slides.
                        if (selectedEffectIdx == Note.EffectVolume)
                        {
                            var lastSlide = channel.GetCachedLastValidEffectValue(r.minVisiblePattern - 1, Note.EffectVolumeSlide, out var lastSlideLocation);

                            // If the last slide is before the last volume, ignore.
                            if (lastSlideLocation.IsValid && lastSlideLocation < lastValueLocation || !lastSlideLocation.IsValid)
                                lastSlide = -1;

                            var lastSlideDuration = lastSlide >= 0 ? channel.GetVolumeSlideDuration(lastSlideLocation) : -1;

                            lastFrame = lastValueLocation.IsValid ? lastValueLocation.ToAbsoluteNoteIndex(song) : -1;

                            var filter = Note.GetFilterForEffect(Note.EffectVolume) | Note.GetFilterForEffect(Note.EffectVolumeSlide);

                            for (var it = channel.GetSparseNoteIterator(minLocation, maxLocation, filter); !it.Done; it.Next())
                            {
                                var note = it.Note;
                                var location = it.Location;

                                Debug.Assert(note.HasVolume || note.HasVolumeSlide);

                                if (note.HasValidEffectValue(Note.EffectVolume))
                                {
                                    r.ch.PushTranslation(GetPixelForNote(location.ToAbsoluteNoteIndex(song)), 0);

                                    var frame = location.ToAbsoluteNoteIndex(song);

                                    if (lastSlide >= 0)
                                    {
                                        var X0 = GetPixelForNote(lastFrame < 0 ? -1000000 : lastFrame - frame, false);
                                        var X1 = GetPixelForNote(-frame + lastFrame + lastSlideDuration, false);
                                        var sizeY0 = (maxValue == minValue) ? effectPanelSizeY : (float)Math.Floor(lastValue / (float)Note.VolumeMax * effectPanelSizeY);
                                        var sizeY1 = (maxValue == minValue) ? effectPanelSizeY : (float)Math.Floor(lastSlide / (float)Note.VolumeMax * effectPanelSizeY);

                                        var points = new float[4, 2]
                                        {
                                            { X0, effectPanelSizeY - sizeY0 },
                                            { X0, effectPanelSizeY },
                                            { X1, effectPanelSizeY },
                                            { X1, effectPanelSizeY - sizeY1 }
                                        };

                                        r.ch.FillGeometry(points, ThemeResources.DarkGreyFillBrush2);

                                        if ((frame - lastFrame) == 1 && lastSlide < lastValue)
                                            singleFrameSlides.Add(NoteLocation.FromAbsoluteNoteIndex(song, lastFrame));

                                        if ((frame - lastFrame) > lastSlideDuration)
                                            r.ch.FillRectangle(X1, effectPanelSizeY - sizeY1, 0, effectPanelSizeY, ThemeResources.DarkGreyFillBrush2);
                                    }
                                    else
                                    {
                                        var sizeY = (maxValue == minValue) ? effectPanelSizeY : (float)Math.Floor(lastValue / (float)Note.VolumeMax * effectPanelSizeY);
                                        r.ch.FillRectangle(GetPixelForNote(lastFrame < 0 ? -1000000 : lastFrame - frame, false), effectPanelSizeY - sizeY, 0, effectPanelSizeY, ThemeResources.DarkGreyFillBrush2);
                                    }

                                    lastSlide = note.HasVolumeSlide ? note.VolumeSlideTarget : -1;
                                    lastValue = note.Volume;
                                    lastFrame = frame;

                                    if (lastSlide >= 0)
                                        lastSlideDuration = channel.GetVolumeSlideDuration(location);

                                    r.ch.PopTransform();
                                }
                            }

                            r.ch.PushTranslation(GetPixelForNote(Math.Max(lastFrame, 0)), 0);

                            if (lastSlide >= 0)
                            {
                                var location = NoteLocation.FromAbsoluteNoteIndex(song, lastFrame);

                                var X0 = 0;
                                var X1 = GetPixelForNote(lastSlideDuration, false);
                                var sizeY0 = (maxValue == minValue) ? effectPanelSizeY : (float)Math.Floor(lastValue / (float)Note.VolumeMax * effectPanelSizeY);
                                var sizeY1 = (maxValue == minValue) ? effectPanelSizeY : (float)Math.Floor(lastSlide / (float)Note.VolumeMax * effectPanelSizeY);

                                var points = new float[4, 2]
                                {
                                    { X0, effectPanelSizeY - sizeY0 },
                                    { X0, effectPanelSizeY },
                                    { X1, effectPanelSizeY },
                                    { X1, effectPanelSizeY - sizeY1 }
                                };

                                r.ch.FillGeometry(points, ThemeResources.DarkGreyFillBrush2);

                                if (lastSlideDuration == 1 && lastSlide < lastValue)
                                    singleFrameSlides.Add(location);

                                var endLocation = location.Advance(song, lastSlideDuration);
                                if (endLocation.IsInSong(song))
                                {
                                    var lastNote = channel.GetNoteAt(endLocation);
                                    if (lastNote == null || !lastNote.HasVolume)
                                        r.ch.FillRectangle(X1, effectPanelSizeY - sizeY1, GetPixelForNote(1000000, false), effectPanelSizeY, ThemeResources.DarkGreyFillBrush2);
                                }
                            }
                            else
                            {
                                var lastSizeY = (maxValue == minValue) ? effectPanelSizeY : (float)Math.Floor((lastValue - minValue) / (float)(maxValue - minValue) * effectPanelSizeY);
                                r.ch.FillRectangle(0, effectPanelSizeY - lastSizeY, GetPixelForNote(1000000, false), effectPanelSizeY, ThemeResources.DarkGreyFillBrush2);
                            }

                            r.ch.PopTransform();
                        }
                        else
                        {
                            for (var it = channel.GetSparseNoteIterator(minLocation, maxLocation, Note.GetFilterForEffect(selectedEffectIdx)); !it.Done; it.Next())
                            {
                                var note = it.Note;
                                var location = it.Location;

                                if (note.HasValidEffectValue(selectedEffectIdx))
                                {
                                    r.ch.PushTranslation(GetPixelForNote(location.ToAbsoluteNoteIndex(song)), 0);

                                    var frame = location.ToAbsoluteNoteIndex(song);
                                    var sizeY = (maxValue == minValue) ? effectPanelSizeY : (float)Math.Floor((lastValue - minValue) / (float)(maxValue - minValue) * effectPanelSizeY);
                                    r.ch.FillRectangle(GetPixelForNote(lastFrame < 0 ? -1000000 : lastFrame - frame, false), effectPanelSizeY - sizeY, 0, effectPanelSizeY, ThemeResources.DarkGreyFillBrush2);
                                    lastValue = note.GetEffectValue(selectedEffectIdx);
                                    lastFrame = frame;

                                    r.ch.PopTransform();
                                }
                            }

                            r.ch.PushTranslation(Math.Max(0, GetPixelForNote(lastFrame)), 0);
                            var lastSizeY = (maxValue == minValue) ? effectPanelSizeY : (float)Math.Floor((lastValue - minValue) / (float)(maxValue - minValue) * effectPanelSizeY);
                            r.ch.FillRectangle(0, effectPanelSizeY - lastSizeY, Width, effectPanelSizeY, ThemeResources.DarkGreyFillBrush2);
                            r.ch.PopTransform();
                        }
                    }

                    DrawSelectionRect(r.ch, effectPanelSizeY);

                    var highlightLocation = NoteLocation.Invalid;

                    if (Platform.IsMobile)
                    {
                        highlightLocation = NoteLocation.FromAbsoluteNoteIndex(song, highlightNoteAbsIndex);
                    }
                    else
                    {
                        if (HasHighlightedNote() && CaptureOperationRequiresEffectHighlight(captureOperation))
                        {
                            highlightLocation = NoteLocation.FromAbsoluteNoteIndex(song, highlightNoteAbsIndex);
                        }
                        else if (captureOperation == CaptureOperation.None)
                        {
                            var pt = PointToClient(Cursor.Position);
                            GetEffectNoteForCoord(pt.X, pt.Y, out highlightLocation);
                        }
                    }

                    // Draw the actual effect bars.
                    for (var it = channel.GetSparseNoteIterator(minLocation, maxLocation, NoteFilter.All); !it.Done; it.Next())
                    {
                        var pattern = it.Pattern;
                        var note = it.Note;
                        var location = it.Location;

                        if (selectedEffectIdx >= 0 && note.HasValidEffectValue(selectedEffectIdx))
                        {
                            var effectValue = note.GetEffectValue(selectedEffectIdx);
                            var effectMinValue = Note.GetEffectMinValue(song, channel, selectedEffectIdx);
                            var effectMaxValue = Note.GetEffectMaxValue(song, channel, selectedEffectIdx);
                            var sizeY = (effectMinValue == effectMaxValue) ? effectPanelSizeY : (float)Math.Floor((effectValue - effectMinValue) / (float)(effectMaxValue - effectMinValue) * effectPanelSizeY);

                            r.ch.PushTranslation(GetPixelForNote(location.ToAbsoluteNoteIndex(song)), 0);

                            if (!Note.EffectWantsPreviousValue(selectedEffectIdx))
                                r.ch.FillRectangle(0, 0, noteSizeX, effectPanelSizeY, ThemeResources.DarkGreyFillBrush2);

                            var highlighted = location == highlightLocation;
                            var selected = IsNoteSelected(location);

                            r.ch.FillAndDrawRectangle(
                                0, effectPanelSizeY - sizeY, noteSizeX, effectPanelSizeY,
                                singleFrameSlides.Contains(location) ? volumeSlideBarFillBrush : ThemeResources.LightGreyFillBrush1,
                                highlighted ? ThemeResources.WhiteBrush : ThemeResources.BlackBrush, highlighted || selected ? 2 : 1, highlighted || selected);

                            var text = effectValue.ToString();
                            if (text.Length * fontSmallCharSizeX + 2 < noteSizeX)
                            {
                                if (sizeY < effectPanelSizeY / 2)
                                    r.ch.DrawText(text, ThemeResources.FontSmall, 0, effectPanelSizeY - sizeY - effectValuePosTextOffsetY, ThemeResources.LightGreyFillBrush1, TextFlags.Center, noteSizeX);
                                else
                                    r.ch.DrawText(text, ThemeResources.FontSmall, 0, effectPanelSizeY - sizeY + effectValueNegTextOffsetY, ThemeResources.BlackBrush, TextFlags.Center, noteSizeX);
                            }

                            r.ch.PopTransform();
                        }
                    }

                    // Thick vertical bars
                    for (int p = r.minVisiblePattern; p < r.maxVisiblePattern; p++)
                    {
                        int x = GetPixelForNote(Song.GetPatternStartAbsoluteNoteIndex(p));
                        if (p != 0) r.ch.DrawLine(x, 0, x, Height, ThemeResources.BlackBrush, 3);
                    }

                    int maxX = GetPixelForNote(Song.GetPatternStartAbsoluteNoteIndex(r.maxVisiblePattern));
                    r.ch.DrawLine(maxX, 0, maxX, Height, ThemeResources.BlackBrush, 3);

                    int seekX = GetPixelForNote(GetSeekFrameToDraw());
                    r.ch.DrawLine(seekX, 0, seekX, effectPanelSizeY, GetSeekBarBrush(), 3);

                    var gizmos = GetEffectGizmos(out _, out _);
                    if (gizmos != null)
                    {
                        foreach (var g in gizmos)
                        {
                            var lineColor = IsGizmoHighlighted(g, headerSizeY) ? Color.White : Color.Black;
                            var scaling = r.g.WindowScaling;

                            if (g.FillImage != null)
                                r.cz.DrawBitmapAtlas(g.FillImage, g.Rect.X, g.Rect.Y, 1.0f, g.Rect.Width / (float)g.Image.ElementSize.Width, Theme.LightGreyFillColor1);
                            r.cz.DrawBitmapAtlas(g.Image, g.Rect.X, g.Rect.Y, 1.0f, g.Rect.Width / (float)g.Image.ElementSize.Width, lineColor);
                        }
                    }
                }
                else if (editMode == EditionMode.DPCM)
                {
                    var halfPanelSizeY = effectPanelSizeY * 0.5f;
                    var envelopePoints = new PointF[4];

                    // Volume envelope
                    for (int i = 0; i < 4; i++)
                    {
                        var x = GetPixelForWaveTime(editSample.VolumeEnvelope[i + 0].sample / editSample.SourceSampleRate, scrollX);
                        var y = halfPanelSizeY + (editSample.VolumeEnvelope[i + 0].volume - 1.0f) * -(halfPanelSizeY - waveDisplayPaddingY);

                        envelopePoints[i] = new PointF(x, y);
                    }

                    // Filled part.
                    for (int i = 0; i < 3; i++)
                    {
                        var points = new float[4, 2]
                        {
                            { envelopePoints[i + 1].X, envelopePoints[i + 1].Y },
                            { envelopePoints[i + 0].X, envelopePoints[i + 0].Y },
                            { envelopePoints[i + 0].X, effectPanelSizeY },
                            { envelopePoints[i + 1].X, effectPanelSizeY }
                        };

                        r.ch.FillGeometry(points, ThemeResources.DarkGreyFillBrush1);
                    }

                    // Horizontal center line
                    r.ch.DrawLine(0, halfPanelSizeY, Width, halfPanelSizeY, ThemeResources.BlackBrush);

                    // Top/bottom dash lines (limits);
                    var topY    = waveDisplayPaddingY;
                    var bottomY = effectPanelSizeY - waveDisplayPaddingY;
                    r.ch.DrawLine(0, topY,    Width, topY, ThemeResources.DarkGreyLineBrush1, 1, false, true); 
                    r.ch.DrawLine(0, bottomY, Width, bottomY, ThemeResources.DarkGreyLineBrush1, 1, false, true);

                    // Envelope line
                    for (int i = 0; i < 3; i++)
                    {
                        r.ch.DrawLine(
                            envelopePoints[i + 0].X, 
                            envelopePoints[i + 0].Y,
                            envelopePoints[i + 1].X, 
                            envelopePoints[i + 1].Y, 
                            ThemeResources.LightGreyFillBrush1, 1, true);
                    }

                    // Envelope vertices.
                    for (int i = 0; i < 4; i++)
                    {
                        r.ch.PushTransform(
                            envelopePoints[i + 0].X,
                            envelopePoints[i + 0].Y, 
                            1.0f, 1.0f);
                        r.ch.FillGeometry(sampleGeometry, ThemeResources.LightGreyFillBrush1);
                        r.ch.PopTransform();
                    }

                    // Selection rectangle
                    if (IsSelectionValid())
                    {
                        r.ch.FillRectangle(
                            GetPixelForWaveTime(GetWaveTimeForSample(selectionMin, true),  scrollX), 0,
                            GetPixelForWaveTime(GetWaveTimeForSample(selectionMax, false), scrollX), Height, selectionBgVisibleBrush);
                    }
                }

                r.ch.DrawLine(0, effectPanelSizeY - 1, Width, effectPanelSizeY - 1, ThemeResources.BlackBrush);
                r.ch.PopTransform();
            }
        }

        private Note[] GetSelectedNotes(bool clone = true)
        {
            if (!IsSelectionValid())
                return null;

            var notes = new Note[selectionMax - selectionMin + 1];

            TransformNotes(selectionMin, selectionMax, false, false, false, (note, idx) =>
            {
                if (note != null && clone)
                    notes[idx] = note.Clone();
                else
                    notes[idx] = note;

                return note;
            });

            return notes;
        }

        private SortedList<int, Note> GetSparseSelectedNotes(int offset = 0, bool musicalOnly = false)
        {
            if (!IsSelectionValid())
                return null;

            var notes = new SortedList<int, Note>();

            TransformNotes(selectionMin, selectionMax, false, false, false, (note, idx) =>
            {
                if (note != null && !note.IsEmpty && (note.IsMusical || !musicalOnly))
                    notes[idx + offset] = note.Clone();
                return note;
            });

            return notes;
        }

        private void CopyNotes()
        {
            ClipboardUtils.SaveNotes(App.Project, GetSelectedNotes(false), editChannel == ChannelType.Dpcm);
        }

        private void CutNotes()
        {
            CopyNotes();
            DeleteSelectedNotes();
        }

        private void ReplaceNotes(Note[] notes, int startFrameIdx, bool doTransaction, bool pasteNotes = true, int pasteFxMask = Note.EffectAllMask, bool mix = false)
        {
            TransformNotes(startFrameIdx, startFrameIdx + notes.Length - 1, doTransaction, true, true, (note, idx) =>
            {
                var channel = Song.Channels[editChannel];
                var newNote = notes[idx];

                if (newNote == null)
                    newNote = Note.EmptyNote;

                if (note == null)
                    note = new Note();

                if (pasteNotes)
                {
                    if (!mix || !note.IsMusicalOrStop && newNote.IsMusicalOrStop)
                    {
                        note.Value = newNote.Value;

                        if (note.IsMusical)
                        {
                            note.Instrument = editChannel == ChannelType.Dpcm || !channel.SupportsInstrument(newNote.Instrument) ? null : newNote.Instrument;
                            note.SlideNoteTarget = channel.SupportsSlideNotes ? newNote.SlideNoteTarget : (byte)0;
                            note.Flags = newNote.Flags;
                            note.Duration = newNote.Duration;
                            note.Release = channel.SupportsReleaseNotes ? newNote.Release : 0;
                            note.Arpeggio = channel.SupportsArpeggios ? newNote.Arpeggio : null;
                        }
                        else if (note.IsStop)
                        {
                            if (!channel.SupportsStopNotes)
                                return null;

                            note.Duration = 1;
                        }
                    }
                }

                for (int i = 0; i < Note.EffectCount; i++)
                {
                    if ((pasteFxMask & (1 << i)) != 0 && (!mix || !note.HasValidEffectValue(i) && newNote.HasValidEffectValue(i)))
                    {
                        note.ClearEffectValue(i);
                        if (channel.SupportsEffect(i) && newNote.HasValidEffectValue(i))
                        {
                            int clampedEffectValue = Note.ClampEffectValue(Song, channel, i, newNote.GetEffectValue(i));
                            note.SetEffectValue(i, clampedEffectValue);
                        }
                    }
                }

                return note;
            });

            SetSelection(startFrameIdx, startFrameIdx + notes.Length - 1);
        }

        private void PasteNotes(bool pasteNotes = true, int pasteFxMask = Note.EffectAllMask, bool mix = false, int repeat = 1)
        {
            if (!IsSelectionValid())
                return;

            bool createMissingInstrument = false;
            bool createMissingArpeggios  = false;
            bool createMissingSamples    = false;

            if (pasteNotes)
            {
                var missingInstruments = ClipboardUtils.ContainsMissingInstrumentsOrSamples(App.Project, true, out var missingArpeggios, out var missingSamples);

                if (missingInstruments)
                    createMissingInstrument = Platform.MessageBox($"You are pasting notes referring to unknown instruments. Do you want to create the missing instrument?", "Paste", MessageBoxButtons2.YesNo) == DialogResult2.Yes;

                if (missingArpeggios)
                    createMissingArpeggios = Platform.MessageBox($"You are pasting notes referring to unknown arpeggios. Do you want to create the missing arpeggios?", "Paste", MessageBoxButtons2.YesNo) == DialogResult2.Yes;

                if (missingSamples && editChannel == ChannelType.Dpcm)
                    createMissingSamples = Platform.MessageBox($"You are pasting notes referring to unmapped DPCM samples. Do you want to create the missing samples?", "Paste", MessageBoxButtons2.YesNo) == DialogResult2.Yes;
            }

            App.UndoRedoManager.BeginTransaction(createMissingInstrument || createMissingArpeggios || createMissingSamples ? TransactionScope.Project : TransactionScope.Channel, Song.Id, editChannel);

            for (int i = 0; i < repeat; i++)
            {
                var notes = ClipboardUtils.LoadNotes(App.Project, createMissingInstrument, createMissingArpeggios, createMissingSamples);

                if (notes == null)
                {
                    App.UndoRedoManager.AbortTransaction();
                    return;
                }

                ReplaceNotes(notes, selectionMin, false, pasteNotes, pasteFxMask, mix);

                if (i != repeat - 1)
                {
                    int selectionSize = selectionMax - selectionMin + 1;
                    SetSelection(selectionMin + selectionSize, selectionMax + selectionSize);
                }
            }

            NotesPasted?.Invoke();
            App.UndoRedoManager.EndTransaction();
        }

        private sbyte[] GetSelectedEnvelopeValues()
        {
            if (!IsSelectionValid())
                return null;

            var values = new sbyte[selectionMax - selectionMin + 1];

            for (int i = selectionMin; i <= selectionMax; i++)
                values[i - selectionMin] = EditEnvelope.Values[i];

            return values;
        }

        private void CopyEnvelopeValues()
        {
            ClipboardUtils.SaveEnvelopeValues(GetSelectedEnvelopeValues());
        }

        private void CutEnvelopeValues()
        {
            CopyEnvelopeValues();
            DeleteSelectedEnvelopeValues();
        }

        private void ReplaceEnvelopeValues(sbyte[] values, int startIdx)
        {
            TransformEnvelopeValues(startIdx, startIdx + values.Length - 1, (val, idx) =>
            {
                return values[idx];
            });

            SetSelection(startIdx, startIdx + values.Length - 1);
        }

        private void PasteEnvelopeValues()
        {
            if (!IsSelectionValid())
                return;

            var values = ClipboardUtils.LoadEnvelopeValues();

            if (values == null)
                return;

            ReplaceEnvelopeValues(values, selectionMin);
        }

        public void Copy()
        {
            if (editMode == EditionMode.Channel)
                CopyNotes();
            else if (editMode == EditionMode.Enveloppe || editMode == EditionMode.Arpeggio)
                CopyEnvelopeValues();
        }

        public void Cut()
        {
            if (editMode == EditionMode.Channel)
                CutNotes();
            else if (editMode == EditionMode.Enveloppe || editMode == EditionMode.Arpeggio)
                CutEnvelopeValues();
        }

        public void Paste()
        {
            AbortCaptureOperation();

            if (editMode == EditionMode.Channel)
                PasteNotes();
            else if (editMode == EditionMode.Enveloppe || editMode == EditionMode.Arpeggio)
                PasteEnvelopeValues();
        }

        public void PasteSpecial()
        {
            if (editMode == EditionMode.Channel)
            {
                AbortCaptureOperation();

                var dlg = new PasteSpecialDialog(Song.Channels[editChannel], lastPasteSpecialPasteMix, lastPasteSpecialPasteNotes, lastPasteSpecialPasteEffectMask);

                dlg.ShowDialogAsync(ParentForm, (r) =>
                {
                    if (r == DialogResult2.OK)
                    {
                        var effectMask = dlg.PasteEffectMask;

                        // Include volume slides if volume is selected.
                        if ((effectMask & Note.EffectVolumeMask) != 0 && Song.Channels[editChannel].SupportsEffect(Note.EffectVolumeSlide))
                            effectMask |= Note.EffectVolumeAndSlideMask;

                        PasteNotes(dlg.PasteNotes, effectMask, dlg.PasteMix, dlg.PasteRepeat);

                        lastPasteSpecialPasteMix = dlg.PasteMix;
                        lastPasteSpecialPasteNotes = dlg.PasteNotes;
                        lastPasteSpecialPasteEffectMask = dlg.PasteEffectMask;
                    }
                });
            }
        }

        public void DeleteSelection()
        {
            if (editMode == EditionMode.DPCM)
                DeleteSelectedWaveSection();
            else if (editMode == EditionMode.Channel)
                DeleteSelectedNotes();
            else if (editMode == EditionMode.Enveloppe || editMode == EditionMode.Arpeggio)
                DeleteSelectedEnvelopeValues();
        }

        public void DeleteSpecial()
        {
            if (editMode == EditionMode.Channel)
            {
                AbortCaptureOperation();

                var dlg = new DeleteSpecialDialog(Song.Channels[editChannel]);

                dlg.ShowDialogAsync(ParentForm, (r) =>
                {
                    if (r == DialogResult2.OK)
                        DeleteSelectedNotes(true, dlg.DeleteNotes, dlg.DeleteEffectMask);
                });
            }
        }

        private void PromoteTransaction(TransactionScope scope, int objectId = -1, int subIdx = -1)
        {
            // HACK : When promoting transaction, we end up saving the app state at the 
            // moment when the transaction is promoted. This lead to selections being in
            // the wrong place. 
            var tempSelectionMin = selectionMin;
            var tempSelectionMax = selectionMax;

            selectionMin = captureSelectionMin;
            selectionMax = captureSelectionMax;

            App.UndoRedoManager.AbortTransaction();
            App.UndoRedoManager.BeginTransaction(scope, objectId, subIdx);

            selectionMin = tempSelectionMin;
            selectionMax = tempSelectionMax;
        }

        private bool IsNoteSelected(int absoluteNoteIdx)
        {
            if (IsSelectionValid())
                return absoluteNoteIdx >= selectionMin && absoluteNoteIdx <= selectionMax;
            else
                return false;
        }

        private bool IsNoteSelected(NoteLocation location, int duration = 0)
        {
            return IsNoteSelected(location.ToAbsoluteNoteIndex(Song));
        }

        private bool IsEnvelopeValueSelected(int idx)
        {
            return IsSelectionValid() && idx >= selectionMin && idx <= selectionMax;
        }

        private void DrawSelectionRect(CommandList c, int height)
        {
            if (IsSelectionValid())
            {
                c.FillRectangle(
                    GetPixelForNote(selectionMin + 0), 0,
                    GetPixelForNote(selectionMax + 1), height, IsActiveControl ? selectionBgVisibleBrush : selectionBgInvisibleBrush);
            }
        }

        private bool IsGizmoHighlighted(Gizmo g, int offsetY)
        {
            if (Platform.IsMobile)
            {
                return true;
            }
            else
            {
                var pt = PointToClient(Cursor.Position);

                if (g.Rect.Contains(pt.X - pianoSizeX, pt.Y - offsetY))
                    return true;

                if (g.Action == GizmoAction.MoveSlide && captureOperation == CaptureOperation.DragSlideNoteTargetGizmo ||
                    g.Action == GizmoAction.MoveVolumeSlideValue && captureOperation == CaptureOperation.DragVolumeSlideTargetGizmo)
                    return true;
            }
            return false;
        }

        private Color GetNoteColor(Channel channel, int noteValue, Instrument instrument, float alphaDim = 1.0f)
        {
            var color = Theme.LightGreyFillColor1;

            if (channel.Type == ChannelType.Dpcm)
            {
                var mapping = channel.Song.Project.GetDPCMMapping(noteValue);
                if (mapping != null)
                    color = mapping.Sample.Color;
            }
            else if (instrument != null)
            {
                color = instrument.Color;
            }

            return alphaDim != 1.0f ? Color.FromArgb((int)(color.A * alphaDim), color) : color;
        }

        private bool ShouldDrawLines(Song song, int patternIdx, int numNotes)
        {
            var x0 = GetPixelForNote(song.GetPatternStartAbsoluteNoteIndex(patternIdx));
            var x1 = GetPixelForNote(song.GetPatternStartAbsoluteNoteIndex(patternIdx) + numNotes);

            return (x1 - x0) >= minPixelDistForLines;
        }

        private void RenderNotes(RenderInfo r)
        {
            var song = Song;
            var maxX  = editMode == EditionMode.Channel ? GetPixelForNote(song.GetPatternStartAbsoluteNoteIndex(r.maxVisiblePattern)) : Width;
                                                           
            // Draw the note backgrounds
            for (int i = r.minVisibleOctave; i < r.maxVisibleOctave; i++)
            {
                int octaveBaseY = (virtualSizeY - octaveSizeY * i) - scrollY;

                for (int j = 0; j < 12; j++)
                {
                    int y = octaveBaseY - j * noteSizeY;
                    if (!IsBlackKey(j))
                        r.cb.FillRectangle(0, y - noteSizeY, maxX, y, ThemeResources.DarkGreyFillBrush1);
                    if (i * 12 + j != NumNotes)
                        r.cb.DrawLine(0, y, maxX, y, ThemeResources.BlackBrush);
                }
            }

            DrawSelectionRect(r.cb, Height); 

            if (editMode == EditionMode.Channel ||
                editMode == EditionMode.VideoRecording)
            {
                // Draw the vertical bars.
                for (int p = r.minVisiblePattern; p < r.maxVisiblePattern; p++)
                {
                    var patternLen = song.GetPatternLength(p);
                    var beatLength = song.GetPatternBeatLength(p);

                    if (song.UsesFamiStudioTempo)
                    {
                        var noteLength = song.GetPatternNoteLength(p);
                        var drawNotes = ShouldDrawLines(song, p, noteLength);
                        var drawFrames = drawNotes && ShouldDrawLines(song, p, 1);

                        for (int i = p == 0 ? 1 : 0; i < patternLen; i++)
                        {
                            int x = GetPixelForNote(song.GetPatternStartAbsoluteNoteIndex(p) + i);

                            if (i % beatLength == 0)
                                r.cb.DrawLine(x, 0, x, Height, ThemeResources.BlackBrush, i == 0 ? 3 : 1);
                            else if (drawNotes && i % noteLength == 0)
                                r.cb.DrawLine(x, 0, x, Height, ThemeResources.DarkGreyLineBrush1);
                            else if (drawFrames && editMode != EditionMode.VideoRecording)
                                r.cb.DrawLine(x, 0, x, Height, ThemeResources.DarkGreyLineBrush1, 1, false, true);
                        }
                    }
                    else
                    {
                        var drawNotes = ShouldDrawLines(song, p, 1);

                        for (int i = p == 0 ? 1 : 0; i < patternLen; i++)
                        {
                            int x = GetPixelForNote(song.GetPatternStartAbsoluteNoteIndex(p) + i);

                            if (i % beatLength == 0)
                                r.cb.DrawLine(x, 0, x, Height, ThemeResources.BlackBrush, i == 0 ? 3 : 1);
                            else if (drawNotes)
                                r.cb.DrawLine(x, 0, x, Height, ThemeResources.DarkGreyLineBrush2);
                        }
                    }
                }

                r.cb.DrawLine(maxX, 0, maxX, Height, ThemeResources.BlackBrush, 3);

                if (editMode != EditionMode.VideoRecording)
                {
                    int seekX = GetPixelForNote(GetSeekFrameToDraw());
                    r.cb.DrawLine(seekX, 0, seekX, Height, GetSeekBarBrush(), 3);
                }

                // Highlight note under mouse.
                var highlightNote = (Note)null;
                var highlightLocation = NoteLocation.Invalid;
                var highlightReleased = false;
                var highlightLastNoteValue = Note.NoteInvalid;
                var highlightLastInstrument = (Instrument)null;

                if (editMode != EditionMode.VideoRecording)
                {
                    if (Platform.IsMobile)
                    {
                        if (HasHighlightedNote())
                        {
                            highlightLocation = NoteLocation.FromAbsoluteNoteIndex(song, highlightNoteAbsIndex);
                            highlightNote = song.Channels[editChannel].GetNoteAt(highlightLocation);
                        }
                    }
                    else
                    {
                        if (HasHighlightedNote() && CaptureOperationRequiresNoteHighlight(captureOperation))
                        {
                            highlightLocation = NoteLocation.FromAbsoluteNoteIndex(song, highlightNoteAbsIndex);
                            highlightNote = song.Channels[editChannel].GetNoteAt(highlightLocation);
                        }
                        else if (captureOperation == CaptureOperation.None)
                        {
                            var pt = PointToClient(Cursor.Position);
                            highlightNote = GetNoteForCoord(pt.X, pt.Y, out _, out highlightLocation, out _);
                        }
                    }
                }

                var ghostChannelMask = App != null ? App.ForceDisplayChannelMask : 0;
                var maxEffectPosY = 0;

                // Render the active channel last.
                var channelsToRender = new int[song.Channels.Length];
                for (int c = 0; c < song.Channels.Length; c++)
                    channelsToRender[c] = c;

                Utils.Swap(ref channelsToRender[editChannel], ref channelsToRender[channelsToRender.Length - 1]);

                // Note drawing.
                foreach (var c in channelsToRender)
                {
                    var channel = song.Channels[c];
                    var isActiveChannel = c == editChannel;

                    if (isActiveChannel || (ghostChannelMask & (1 << c)) != 0)
                    {
                        var drawImplicitStopNotes = 
                            isActiveChannel &&
                            editMode == EditionMode.Channel &&
                            (Settings.ShowImplicitStopNotes && Song.UsesFamiTrackerTempo);

                        var min = new NoteLocation(r.minVisiblePattern, 0);
                        var max = new NoteLocation(r.maxVisiblePattern, 0);

                        var lastNoteValue  = Note.MusicalNoteC4;
                        var lastInstrument = (Instrument)null;

                        // Always start rendering from the last note that had an attack.
                        var lastNoteLocation = channel.GetCachedLastMusicalNoteWithAttackLocation(min.PatternIndex - 1);
                        if (lastNoteLocation.IsValid)
                        {
                            min.PatternIndex = Math.Min(min.PatternIndex, lastNoteLocation.PatternIndex);

                            var note = channel.GetNoteAt(lastNoteLocation);
                            lastNoteValue  = note.Value;
                            lastInstrument = note.Instrument;
                        }

                        var released = false;

                        for (var it = channel.GetSparseNoteIterator(min, max); !it.Done; it.Next())
                        {
                            var note = it.Note;

                            // Release notes are no longer supported in the piano roll. 
                            Debug.Assert(!note.IsRelease);

                            if (note.IsMusical)
                            {
                                lastNoteValue = note.Value;
                                lastInstrument = note.Instrument;

                                if (note.HasAttack)
                                    released = false;
                            }

                            if (isActiveChannel && it.Location == highlightLocation)
                            {
                                highlightReleased = released;
                                highlightLastNoteValue = lastNoteValue;
                                highlightLastInstrument = lastInstrument;
                            }

                            if (note.IsMusical)
                            {
                                RenderNote(r, it.Location, note, song, channel, it.DistanceToNextCut, drawImplicitStopNotes, isActiveChannel, false, released);
                            }
                            else if (note.IsStop)
                            {
                                RenderNoteReleaseOrStop(r, note, GetNoteColor(channel, lastNoteValue, lastInstrument, isActiveChannel ? 1.0f : 0.2f), it.Location.ToAbsoluteNoteIndex(Song), lastNoteValue, false, IsNoteSelected(it.Location, 1), isActiveChannel, true, released);
                            }

                            if (note.HasRelease && note.Release < Math.Min(note.Duration, it.DistanceToNextCut))
                            {
                                released = true;
                            }
                        }

                        if (isActiveChannel && highlightNote != null)
                        {
                            if (highlightNote.IsMusical)
                            {
                                RenderNote(r, highlightLocation, highlightNote, song, channel, channel.GetDistanceToNextNote(highlightLocation), drawImplicitStopNotes, true, true, highlightReleased);
                            }
                            else if (highlightNote.IsStop)
                            {
                                RenderNoteReleaseOrStop(r, highlightNote, GetNoteColor(channel, highlightLastNoteValue, highlightLastInstrument, isActiveChannel ? 1.0f : 0.2f), highlightLocation.ToAbsoluteNoteIndex(Song), highlightLastNoteValue, true, false, true, true, highlightReleased);
                            }
                        }
                    }
                }

                // Draw effect icons at the top.
                if (editMode != EditionMode.VideoRecording)
                {
                    var effectIconSizeX = ScaleCustom(bmpEffects[0].ElementSize.Width, effectBitmapScale);

                    var channel = song.Channels[editChannel];
                    for (int p = r.minVisiblePattern; p < r.maxVisiblePattern; p++)
                    {
                        var pattern = channel.PatternInstances[p];

                        if (pattern == null)
                            continue;

                        foreach (var kv in pattern.Notes)
                        {
                            var time = kv.Key;
                            var note = kv.Value;

                            if (note.HasAnyEffect)
                            {
                                // TODO: Iterate on the bits of the effect mask. 
                                var effectPosY = 0;
                                for (int fx = 0; fx < Note.EffectCount; fx++)
                                {
                                    if (note.HasValidEffectValue(fx))
                                    {
                                        // These 2 effects usually come in a pair, so let's draw only 1 icon.
                                        if (fx == Note.EffectVibratoDepth && note.HasValidEffectValue(Note.EffectVibratoSpeed))
                                            continue;
                                        if (fx == Note.EffectVolumeSlide)
                                            continue;

                                        var drawOpaque = !showEffectsPanel || fx == selectedEffectIdx || fx == Note.EffectVibratoDepth && selectedEffectIdx == Note.EffectVibratoSpeed || fx == Note.EffectVibratoSpeed && selectedEffectIdx == Note.EffectVibratoDepth;

                                        var iconX = GetPixelForNote(channel.Song.GetPatternStartAbsoluteNoteIndex(p, time)) + (int)(noteSizeX / 2) - effectIconSizeX / 2;
                                        var iconY = effectPosY + effectIconPosY;

                                        r.cf.DrawBitmapAtlas(bmpEffects[EffectImageNames.Length - 1], iconX, iconY, 1.0f, effectBitmapScale, drawOpaque ? Theme.LightGreyFillColor1 : Theme.MediumGreyFillColor1);
                                        r.cf.DrawBitmapAtlas(bmpEffects[fx], iconX, iconY, drawOpaque ? 1.0f : 0.4f, effectBitmapScale, Theme.LightGreyFillColor1);
                                        effectPosY += effectIconSizeX + effectIconPosY + 1;
                                    }
                                }
                                maxEffectPosY = Math.Max(maxEffectPosY, effectPosY);
                            }
                        }
                    }
                }
                    
                if (editMode == EditionMode.Channel)
                {
                    var gizmos = GetNoteGizmos(out var gizmoNote, out _);
                    if (gizmos != null)
                    {
                        foreach (var g in gizmos)
                        {
                            var fillColor = GetNoteColor(Song.Channels[editChannel], gizmoNote.Value, gizmoNote.Instrument);
                            var lineColor = IsGizmoHighlighted(g, headerAndEffectSizeY) ? Color.White : Color.Black;

                            if (g.FillImage != null)
                                r.cg.DrawBitmapAtlas(g.FillImage, g.Rect.X, g.Rect.Y, 1.0f, g.Rect.Width / (float)g.FillImage.ElementSize.Width, fillColor);
                            r.cg.DrawBitmapAtlas(g.Image, g.Rect.X, g.Rect.Y, 1.0f, g.Rect.Width / (float)g.FillImage.ElementSize.Width, lineColor);
                        }
                    }
                }

                if (editMode == EditionMode.Channel)
                {
                    var channelType = song.Channels[editChannel].Type;
                    var channelName = song.Channels[editChannel].NameWithExpansion;

                    r.cf.DrawText($"Editing {channelName} Channel", ThemeResources.FontVeryLarge, bigTextPosX, maxEffectPosY > 0 ? maxEffectPosY : bigTextPosY, whiteKeyBrush);
                }
            }
            else if (App.Project != null) // Happens if DPCM panel is open and importing an NSF.
            {
                // Draw 2 dark rectangle to show invalid range. 
                r.cb.PushTranslation(0, -scrollY);
                r.cb.FillRectangle(0, virtualSizeY, Width, virtualSizeY - Note.DPCMNoteMin * noteSizeY, invalidDpcmMappingBrush);
                r.cb.FillRectangle(0, 0, Width, virtualSizeY - Note.DPCMNoteMax * noteSizeY, invalidDpcmMappingBrush);
                r.cb.PopTransform();

                for (int i = 0; i < Note.MusicalNoteMax; i++)
                {
                    var mapping = App.Project.GetDPCMMapping(i);
                    if (mapping != null)
                    {
                        var y = virtualSizeY - i * noteSizeY - scrollY;
                        var highlighted = i == highlightDPCMSample;

                        r.cf.PushTranslation(0, y);
                        r.cf.FillAndDrawRectangle(0, 0, Width - pianoSizeX, noteSizeY, r.g.GetVerticalGradientBrush(mapping.Sample.Color, noteSizeY, 0.8f), highlighted ? ThemeResources.WhiteBrush : ThemeResources.BlackBrush, highlighted ? 2 : 1);

                        string text = $"{mapping.Sample.Name} - Pitch: {DPCMSampleRate.GetString(true, FamiStudio.StaticInstance.PalPlayback, true, true, mapping.Pitch)}";
                        if (mapping.Loop) text += ", Looping";
                        if (mapping.OverrideDmcInitialValue) text += $" , DMC Initial value = {mapping.DmcInitialValueDiv2}";

                        r.cf.DrawText(text, ThemeResources.FontSmall, dpcmTextPosX, 0, ThemeResources.BlackBrush, TextFlags.MiddleLeft, 0, noteSizeY);
                        r.cf.PopTransform();
                    }
                }

                DPCMSample dragSample = null;

                if (captureOperation == CaptureOperation.DragSample && draggedSample != null)
                {
                    dragSample = draggedSample.Sample;
                }
                else if (captureOperation == CaptureOperation.None && App.DraggedSample != null)
                {
                    dragSample = App.DraggedSample;
                }

                if (dragSample != null)
                {
                    var pt = Platform.IsDesktop ? PointToClient(Cursor.Position) : new Point(mouseLastX, mouseLastY);

                    if (GetNoteValueForCoord(pt.X, pt.Y, out var noteValue) && App.Project.NoteSupportsDPCM(noteValue))
                    {
                        var y = virtualSizeY - noteValue * noteSizeY - scrollY;
                        r.cf.PushTranslation(0, y);
                        r.cf.FillAndDrawRectangle(0, 0, Width - pianoSizeX, noteSizeY, r.g.GetVerticalGradientBrush(dragSample.Color, noteSizeY, 0.8f), ThemeResources.WhiteBrush, 2, true);
                        r.cf.PopTransform();
                    }
                }
                else if (Platform.IsDesktop && captureOperation == CaptureOperation.None)
                {
                    var pt = PointToClient(Cursor.Position);

                    if (GetLocationForCoord(pt.X, pt.Y, out _, out var highlightNoteValue))
                    {
                        var mapping = App.Project.GetDPCMMapping(highlightNoteValue);
                        if (mapping != null)
                        {
                            var y = virtualSizeY - highlightNoteValue * noteSizeY - scrollY;

                            r.cf.PushTranslation(0, y);
                            r.cf.DrawRectangle(0, 0, Width - pianoSizeX, noteSizeY, ThemeResources.WhiteBrush, 2, true);
                            r.cf.PopTransform();
                        }
                    }
                }

                r.cf.DrawText($"Editing DPCM Samples Instrument ({App.Project.GetTotalMappedSampleSize()} / {Project.MaxMappedSampleSize} Bytes)", ThemeResources.FontVeryLarge, bigTextPosX, bigTextPosY, whiteKeyBrush);
            }
        }

        private void RenderEnvelopeValues(RenderInfo r)
        {
            var env = EditEnvelope;
            var spacing = editEnvelope == EnvelopeType.DutyCycle ? 4 : (editEnvelope == EnvelopeType.Arpeggio ? 12 : 16);
            var color = editMode == EditionMode.Enveloppe ? editInstrument.Color : editArpeggio.Color;

            Envelope.GetMinMaxValueForType(editInstrument, editEnvelope, out int envTypeMinValue, out int envTypeMaxValue);

            // Draw the enveloppe value backgrounds.
            int maxValue = 128 / (int)envelopeValueZoom;
            int midValue =  64 / (int)envelopeValueZoom;

            var lastRectangleValue = int.MinValue;
            var lastRectangleY     = -1.0f;
            var oddRectangle       = false;

            var maxX = GetPixelForNote(env.Length);
            var maxi = (Platform.IsDesktop ? maxValue : envTypeMaxValue - envTypeMinValue) + 1;

            for (int i = 0; i <= maxi; i++)
            {
                var value = Platform.IsMobile ? i + envTypeMinValue : i - midValue;
                var y = (virtualSizeY - envelopeValueSizeY * i) - scrollY;
                
                if (i != maxi)
                    r.cb.DrawLine(0, y, GetPixelForNote(env.Length), y, ThemeResources.DarkGreyLineBrush1, (value % spacing) == 0 ? 3 : 1);

                var drawLabel = i == maxi - 1;

                if ((value % spacing) == 0 || i == 0 || i == maxi)
                {
                    if (lastRectangleValue >= envTypeMinValue && lastRectangleValue <= envTypeMaxValue)
                    {
                        r.cb.FillRectangle(0, lastRectangleY, maxX, y, oddRectangle ? ThemeResources.DarkGreyFillBrush2 : ThemeResources.DarkGreyFillBrush1);
                        oddRectangle = !oddRectangle;
                    }

                    lastRectangleValue = value;
                    lastRectangleY = y;
                    drawLabel |= value >= envTypeMinValue - 1 && value <= envTypeMaxValue + 1;
                }

                if (drawLabel)
                    r.cb.DrawText(value.ToString(), ThemeResources.FontSmall, maxX + 4 * r.g.WindowScaling, y - envelopeValueSizeY, ThemeResources.LightGreyFillBrush1, TextFlags.MiddleLeft, 0, envelopeValueSizeY);
            }

            DrawSelectionRect(r.cb, Height);

            // Draw the vertical bars.
            for (int b = 0; b < env.Length; b++)
            {
                int x = GetPixelForNote(b);
                if (b != 0) r.cb.DrawLine(x, 0, x, Height, ThemeResources.DarkGreyLineBrush1);
            }

            if (env.Loop >= 0)
                r.cb.DrawLine(GetPixelForNote(env.Loop), 0, GetPixelForNote(env.Loop), Height, ThemeResources.BlackBrush);
            if (env.Release >= 0)
                r.cb.DrawLine(GetPixelForNote(env.Release), 0, GetPixelForNote(env.Release), Height, ThemeResources.BlackBrush);
            if (env.Length > 0)
                r.cb.DrawLine(GetPixelForNote(env.Length), 0, GetPixelForNote(env.Length), Height, ThemeResources.BlackBrush);

            if ((editMode == EditionMode.Enveloppe || editMode == EditionMode.Arpeggio) && editEnvelope < EnvelopeType.RegularCount)
            {
                var seekFrame = App.GetEnvelopeFrame(editInstrument, editEnvelope, editMode == EditionMode.Arpeggio);
                if (seekFrame >= 0)
                {
                    var seekX = GetPixelForNote(seekFrame);
                    r.cb.DrawLine(seekX, 0, seekX, Height, GetSeekBarBrush(), 3);
                }
            }

            var highlightRect = RectangleF.Empty;

            if (editEnvelope == EnvelopeType.Arpeggio)
            {
                for (int i = 0; i < env.Length; i++)
                {
                    var selected = IsEnvelopeValueSelected(i);
                    var highlighted = Platform.IsMobile && highlightNoteAbsIndex == i;
                    var bias = Platform.IsMobile ? -envTypeMinValue : midValue;

                    float x0 = GetPixelForNote(i + 0);
                    float x1 = GetPixelForNote(i + 1);
                    float y = (virtualSizeY - envelopeValueSizeY * (env.Values[i] + bias)) - scrollY;

                    r.cf.FillRectangle(x0, y - envelopeValueSizeY, x1, y, r.g.GetVerticalGradientBrush(color, (int)envelopeValueSizeY, 0.8f));

                    if (!highlighted)
                        r.cf.DrawRectangle(x0, y - envelopeValueSizeY, x1, y, selected ? selectionNoteBrush : ThemeResources.BlackBrush, selected ? 2 : 1, selected);
                    else
                        highlightRect = new RectangleF(x0, y - envelopeValueSizeY, x1 - x0, envelopeValueSizeY);

                    var label = env.Values[i].ToString("+#;-#;0");
                    if (label.Length * fontSmallCharSizeX + 2 < noteSizeX)
                        r.cf.DrawText(label, ThemeResources.FontSmall, x0, y - envelopeValueSizeY - effectValuePosTextOffsetY, ThemeResources.LightGreyFillBrush1, TextFlags.Center, noteSizeX);
                }
            }
            else
            {
                for (int i = 0; i < env.Length; i++)
                {
                    var center = editEnvelope == EnvelopeType.FdsWaveform ? 32 : 0;
                    var bias = Platform.IsMobile ? -envTypeMinValue : midValue;
                    int val = env.Values[i];

                    float y0, y1, ty;
                    if (val >= center)
                    {
                        y0 = (virtualSizeY - envelopeValueSizeY * (val + bias + 1)) - scrollY;
                        y1 = (virtualSizeY - envelopeValueSizeY * (bias + center) - scrollY);
                        ty = y0;
                    }
                    else
                    {
                        y1 = (virtualSizeY - envelopeValueSizeY * (val + bias)) - scrollY;
                        y0 = (virtualSizeY - envelopeValueSizeY * (bias + center + 1) - scrollY);
                        ty = y1;
                    }

                    var x0 = GetPixelForNote(i + 0);
                    var x1 = GetPixelForNote(i + 1);
                    var selected = IsEnvelopeValueSelected(i);
                    var highlighted = Platform.IsMobile && highlightNoteAbsIndex == i;

                    r.cf.FillRectangle(x0, y0, x1, y1, r.g.GetSolidBrush(color));

                    if (!highlighted)
                        r.cf.DrawRectangle(x0, y0, x1, y1, selected ? selectionNoteBrush : ThemeResources.BlackBrush, selected ? 2 : 1, selected);
                    else
                        highlightRect = new RectangleF(x0, y0, x1 - x0, y1 - y0);

                    bool drawOutside = Math.Abs(y1 - y0) < (DefaultEnvelopeSizeY * MainWindowScaling * 2);
                    var brush = drawOutside ? ThemeResources.LightGreyFillBrush1 : ThemeResources.BlackBrush;
                    var offset = drawOutside != val < center ? -effectValuePosTextOffsetY : effectValueNegTextOffsetY;

                    var label = val.ToString();
                    if (label.Length * fontSmallCharSizeX + 2 < noteSizeX)
                        r.cf.DrawText(label, ThemeResources.FontSmall, x0, ty + offset, brush, TextFlags.Center, noteSizeX);
                }
            }

            if (!highlightRect.IsEmpty)
                r.cf.DrawRectangle(highlightRect, highlightNoteBrush, 2, true);

            if (editMode == EditionMode.Enveloppe)
            {
                var envelopeString = EnvelopeType.Names[editEnvelope];

                if (editEnvelope == EnvelopeType.Pitch)
                    envelopeString = (editInstrument.Envelopes[editEnvelope].Relative ? "Relative " : "Absolute ") + envelopeString;

                r.cf.DrawText($"Editing Instrument {editInstrument.Name} ({envelopeString})", ThemeResources.FontVeryLarge, bigTextPosX, bigTextPosY, whiteKeyBrush);

                if (App.SelectedInstrument != editInstrument)
                    r.cf.DrawText($"Warning : Instrument is currently not selected. Selected instrument '{App.SelectedInstrument.Name}' will be heard when playing the piano.", ThemeResources.FontMedium, bigTextPosX, bigTextPosY + ThemeResources.FontVeryLarge.LineHeight, ThemeResources.LightRedFillBrush);
                else if (editEnvelope == EnvelopeType.Arpeggio && App.SelectedArpeggio != null)
                    r.cf.DrawText($"Warning : Arpeggio envelope currently overridden by selected arpeggio '{App.SelectedArpeggio.Name}'", ThemeResources.FontMedium, bigTextPosX, bigTextPosY + ThemeResources.FontVeryLarge.LineHeight, ThemeResources.LightRedFillBrush);
            }
            else
            {
                r.cf.DrawText($"Editing Arpeggio {editArpeggio.Name}", ThemeResources.FontVeryLarge, bigTextPosX, bigTextPosY, whiteKeyBrush);
            }

            var gizmos = GetEnvelopeGizmos();
            if (gizmos != null)
            {
                foreach (var g in gizmos)
                {
                    var lineColor = IsGizmoHighlighted(g, 0) ? Color.White : Color.Black;

                    if (g.FillImage != null)
                        r.cg.DrawBitmapAtlas(g.FillImage, g.Rect.X, g.Rect.Y, 1.0f, g.Rect.Width / (float)g.Image.ElementSize.Width, color);
                    r.cg.DrawBitmapAtlas(g.Image, g.Rect.X, g.Rect.Y, 1.0f, g.Rect.Width / (float)g.Image.ElementSize.Width, lineColor);
                }
            }
        }

        private void RenderNoteArea(RenderInfo r)
        {
            r.cb.PushTranslation(pianoSizeX, headerAndEffectSizeY);

            if (editMode == EditionMode.Channel ||
                editMode == EditionMode.VideoRecording ||
                editMode == EditionMode.DPCMMapping)
            {
                RenderNotes(r);
            }
            else if (editMode == EditionMode.Enveloppe || editMode == EditionMode.Arpeggio)
            {
                RenderEnvelopeValues(r);
            }

            r.cb.PopTransform();

            if (!string.IsNullOrEmpty(noteTooltip) && editMode != EditionMode.DPCM)
            {
                var textWidth = Width - tooltipTextPosX - scrollBarThickness;
                if (textWidth > 0)
                    r.cf.DrawText(noteTooltip, ThemeResources.FontLarge, 0, Height - tooltipTextPosY - scrollBarThickness, whiteKeyBrush, TextFlags.Right, textWidth);
            }
        }

        private void RenderNoteBody(RenderInfo r, Note note, Color color, int time, int noteLen, bool outline, bool selected, bool activeChannel, bool released, bool isFirstPart, int slideDuration = -1)
        {
            int x = GetPixelForNote(time);
            int y = virtualSizeY - note.Value * noteSizeY - scrollY;
            int sy = released ? releaseNoteSizeY : noteSizeY;
            int activeChannelInt = activeChannel ? 0 : 1;

            if (!outline && isFirstPart && slideDuration >= 0)
            {
                // We will get zero for notes that start a slide and have an immediate delayed cut.
                int duration = Math.Max(1, slideDuration);
                int slideSizeX = duration;
                int slideSizeY = note.SlideNoteTarget - note.Value;

                r.cf.PushTransform(x, y + (slideSizeY > 0 ? 0 : noteSizeY), GetPixelForNote(slideSizeX, false), -slideSizeY);
                r.cf.FillGeometry(slideNoteGeometry, r.g.GetSolidBrush(color, 1.0f, 0.2f), true);
                r.cf.PopTransform();
            }

            if (released)
                y += noteSizeY / 2 - releaseNoteSizeY / 2;

            r.cf.PushTranslation(x, y);

            int sx = GetPixelForNote(noteLen, false);
            int noteTextPosX = attackIconPosX + 1;

            if (!outline)
                r.cf.FillRectangle(0, activeChannelInt, sx, sy, r.g.GetVerticalGradientBrush(color, sy, 0.8f));
            
            if (activeChannel)
                r.cf.DrawRectangle(0, 0, sx, sy, outline ? highlightNoteBrush : (selected ? selectionNoteBrush : ThemeResources.BlackBrush), selected || outline ? 2 : 1, selected || outline);

            if (!outline)
            {
                if (isFirstPart && note.HasAttack && sx > noteAttackSizeX + attackIconPosX * 2 + 2)
                {
                    r.cf.FillRectangle(attackIconPosX + 1, attackIconPosX + 1, attackIconPosX + noteAttackSizeX + 1, sy - attackIconPosX, activeChannel ? attackBrush : attackBrushForceDisplay);
                    noteTextPosX += noteAttackSizeX + attackIconPosX + 2;
                }

                if (activeChannel && Settings.ShowNoteLabels && !released && editMode == EditionMode.Channel && note.IsMusical && ThemeResources.FontSmall.Size < noteSizeY)
                {
                    var label = note.FriendlyName;
                    if ((sx - noteTextPosX) > (label.Length + 1) * fontSmallCharSizeX)
                        r.cf.DrawText(note.FriendlyName, ThemeResources.FontSmall, noteTextPosX, 1, ThemeResources.BlackBrush, TextFlags.Middle, 0, noteSizeY - 1);
                }

                if (note.Arpeggio != null)
                {
                    var offsets = note.Arpeggio.GetChordOffsets();
                    foreach (var offset in offsets)
                    {
                        r.cf.PushTranslation(0, offset * -noteSizeY);
                        r.cf.FillRectangle(0, 1, sx, sy, r.g.GetSolidBrush(note.Arpeggio.Color, 1.0f, activeChannel ? 0.2f : 0.1f));
                        r.cf.PopTransform();
                    }
                }
            }

            r.cf.PopTransform();
        }

        private void RenderNoteReleaseOrStop(RenderInfo r, Note note, Color color, int time, int value, bool outline, bool selected, bool activeChannel, bool stop, bool released)
        {
            int x = GetPixelForNote(time);
            int y = virtualSizeY - value * noteSizeY - scrollY;

            var geo = stop ? (released ? stopReleaseNoteGeometry : stopNoteGeometry) : releaseNoteGeometry;

            r.cf.PushTransform(x, y, noteSizeX, 1);
            if (!outline)
                r.cf.FillGeometry(geo[activeChannel ? 0 : 1], r.g.GetVerticalGradientBrush(color, noteSizeY, 0.8f));
            if (activeChannel)
                r.cf.DrawGeometry(geo[0], outline ? highlightNoteBrush : (selected ? selectionNoteBrush : ThemeResources.BlackBrush), outline || selected ? 2 : 1, true);
            r.cf.PopTransform();

            r.cf.PushTranslation(x, y);
            if (!outline && note.Arpeggio != null)
            {
                var offsets = note.Arpeggio.GetChordOffsets();
                foreach (var offset in offsets)
                {
                    r.cf.PushTransform(0, offset * -noteSizeY, noteSizeX, 1);
                    r.cf.FillGeometry(geo[1], r.g.GetSolidBrush(note.Arpeggio.Color, 1.0f, activeChannel ? 0.2f : 0.1f), true);
                    r.cf.PopTransform();
                }
            }
            r.cf.PopTransform();
        }

        private void RenderNote(RenderInfo r, NoteLocation location, Note note, Song song, Channel channel, int distanceToNextNote, bool drawImplicityStopNotes, bool isActiveChannel, bool highlighted, bool released)
        {
            Debug.Assert(note.IsMusical);

            if (distanceToNextNote < 0)
                distanceToNextNote = (int)ushort.MaxValue;

            var absoluteIndex = location.ToAbsoluteNoteIndex(Song);
            var nextAbsoluteIndex = absoluteIndex + distanceToNextNote;
            var duration = Math.Min(distanceToNextNote, note.Duration);
            var slideDuration = note.IsSlideNote ? channel.GetSlideNoteDuration(location) : -1;
            var color = GetNoteColor(channel, note.Value, note.Instrument, isActiveChannel ? 1.0f : 0.2f);
            var selected = isActiveChannel && IsNoteSelected(location, duration);

            // Draw first part, from start to release point.
            if (note.HasRelease)
            {
                RenderNoteBody(r, note, color, absoluteIndex, Math.Min(note.Release, duration), highlighted, selected, isActiveChannel, released, true, slideDuration);
                absoluteIndex += note.Release;
                duration -= note.Release;

                if (duration > 0)
                {
                    RenderNoteReleaseOrStop(r, note, color, absoluteIndex, note.Value, highlighted, selected, isActiveChannel, false, released);
                    absoluteIndex++;
                    duration--;
                }

                released = true;
            }

            // Then second part, after release to stop note.
            if (duration > 0)
            {
                RenderNoteBody(r, note, color, absoluteIndex, duration, highlighted, selected, isActiveChannel, released, !note.HasRelease, slideDuration);
                absoluteIndex += duration;
                duration -= duration;

                if (drawImplicityStopNotes && absoluteIndex < nextAbsoluteIndex && !highlighted)
                {
                    RenderNoteReleaseOrStop(r, note, Color.FromArgb(128, color), absoluteIndex, note.Value, highlighted, selected, isActiveChannel, true, released);
                }
            }
        }

        private float GetPixelForWaveTime(float time, int scroll = 0)
        {
            var viewSize = Width - pianoSizeX;
            var viewTime = DefaultZoomWaveTime / zoom;

            return time / viewTime * viewSize - scroll;
        }

        private float GetWaveTimeForPixel(int x)
        {
            var viewSize = Width - pianoSizeX;
            var viewTime = DefaultZoomWaveTime / zoom;

            return (x + scrollX) / (float)viewSize * viewTime;
        }

        public float GetWaveTimeForSample(int sampleIndex, bool min)
        {
            // The first sample in a DMC is the initial DPCM counter, its
            // not really part of the data.
            if (!editSample.SourceDataIsWav)
                sampleIndex++;

            float offset = min ? -0.5f : 0.5f;
            return (sampleIndex + offset) / editSample.SourceSampleRate;
        }

        private void RenderWave(RenderInfo r, short[] data, float rate, Brush brush, bool isSource, bool drawSamples)
        {
            var viewWidth     = Width - pianoSizeX;
            var viewHeight    = Height - headerAndEffectSizeY - scrollBarThickness;
            var halfHeight    = viewHeight / 2;
            var halfHeightPad = halfHeight - waveDisplayPaddingY;
            var viewTime      = DefaultZoomWaveTime / zoom;

            var unclampedMinVisibleSample = (int)Math.Floor  (r.minVisibleWaveTime * rate);
            var unclampedMaxVisibleSample = (int)Math.Ceiling(r.maxVisibleWaveTime * rate);
            var unclampedNumVisibleSample = unclampedMaxVisibleSample - unclampedMinVisibleSample;

            if (unclampedNumVisibleSample > 0 && unclampedMaxVisibleSample > 0 && unclampedMinVisibleSample < data.Length)
            {
                var sampleSkip = 1;
                while (unclampedNumVisibleSample / (sampleSkip * 2) > viewWidth)
                    sampleSkip *= 2;

                var minVisibleSample = Utils.RoundDownAndClamp(unclampedMinVisibleSample,     sampleSkip, 0);
                var maxVisibleSample = Utils.RoundUpAndClamp  (unclampedMaxVisibleSample + 1, sampleSkip, data.Length);
                var numVisibleSample = Utils.DivideAndRoundUp (maxVisibleSample - minVisibleSample, sampleSkip);

                if (numVisibleSample > 0)
                {
                    var points = new float[numVisibleSample, 2];
                    var indices = isSource && drawSamples ? new int[numVisibleSample] : null;
                    var scaleX = 1.0f / (rate * viewTime) * viewWidth;
                    var biasX = (float)-scrollX;

                    for (int i = minVisibleSample, j = 0; i < maxVisibleSample; i += sampleSkip, j++)
                    {
                        points[j, 0] = i * scaleX + biasX;
                        points[j, 1] = halfHeight + data[i] / (float)short.MinValue * halfHeightPad;
                        if (indices != null) indices[j] = i;
                    }

                    r.cf.DrawGeometry(points, brush, 1, true);

                    if (drawSamples)
                    {
                        var selectionValid = IsSelectionValid();

                        for (int i = 0; i < points.GetLength(0); i++)
                        {
                            var selected = isSource && selectionValid && indices[i] >= selectionMin && indices[i] <= selectionMax;
                            var sampleScale = selected ? 1.5f : 1.0f;

                            r.cf.PushTransform(points[i, 0], points[i, 1], sampleScale, sampleScale);
                            r.cf.FillGeometry(sampleGeometry, selected ? ThemeResources.WhiteBrush : brush);
                            r.cf.PopTransform();
                        }
                    }
                }
            }
        }

        private void RenderDmc(RenderInfo r, byte[] data, float rate, float baseTime, Brush brush, bool isSource, bool drawSamples, int dmcInitialValue)
        {
            var viewWidth     = Width - pianoSizeX;
            var viewHeight    = Height - headerAndEffectSizeY - scrollBarThickness;
            var halfHeight    = viewHeight / 2;
            var halfHeightPad = halfHeight - waveDisplayPaddingY;
            var viewTime      = DefaultZoomWaveTime / zoom;

            var unclampedMinVisibleSample = (int)Math.Floor  ((r.minVisibleWaveTime - baseTime) * rate);
            var unclampedMaxVisibleSample = (int)Math.Ceiling((r.maxVisibleWaveTime - baseTime) * rate);
            var unclampedNumVisibleSample = unclampedMaxVisibleSample - unclampedMinVisibleSample;

            if (unclampedNumVisibleSample > 0 && unclampedMaxVisibleSample > 0 && unclampedMinVisibleSample < data.Length * 8)
            {
                var sampleSkip = 1;
                while (unclampedNumVisibleSample / (sampleSkip * 2) > viewWidth)
                    sampleSkip *= 2;

                var minVisibleSample = Utils.RoundDownAndClamp(unclampedMinVisibleSample,     sampleSkip, 0);
                var maxVisibleSample = Utils.RoundUpAndClamp  (unclampedMaxVisibleSample + 1, sampleSkip, data.Length * 8);

                // Align to bytes.
                minVisibleSample = Utils.RoundDownAndClamp(minVisibleSample, 8, 0);
                maxVisibleSample = Utils.RoundUpAndClamp  (maxVisibleSample, 8, data.Length * 8);

                var numVisibleSample = Utils.DivideAndRoundUp(maxVisibleSample - minVisibleSample + 1, sampleSkip);

                if (numVisibleSample > 0)
                {
                    var points = new float[numVisibleSample, 2];
                    var indices = isSource && drawSamples ? new int[numVisibleSample] : null;
                    var scaleX = 1.0f / (rate * viewTime) * viewWidth;
                    var biasX = GetPixelForWaveTime(baseTime, scrollX);

                    var dpcmCounter = dmcInitialValue;

                    for (int i = 0; i < minVisibleSample; i++)
                    {
                        var bit = (i >> 3);
                        var mask = (1 << (i & 7));

                        if ((data[bit] & mask) != 0)
                            dpcmCounter = Math.Min(dpcmCounter + 1, 63);
                        else
                            dpcmCounter = Math.Max(dpcmCounter - 1, 0);
                    }

                    for (int i = minVisibleSample, j = 0; i <= maxVisibleSample; i++)
                    {
                        if ((i & (sampleSkip - 1)) == 0)
                        {
                            points[j, 0] = i * scaleX + biasX;
                            points[j, 1] = (-(dpcmCounter - 32) / 64.0f) * 2.0f * halfHeightPad + halfHeight; // DPCMTODO : Is that centered correctly? Also negative value?
                            if (indices != null) indices[j] = i - 1;
                            j++;
                        }

                        if (i < maxVisibleSample)
                        {
                            var bit = (i >> 3);
                            var mask = (1 << (i & 7));

                            if ((data[bit] & mask) != 0)
                                dpcmCounter = Math.Min(dpcmCounter + 1, 63);
                            else
                                dpcmCounter = Math.Max(dpcmCounter - 1, 0);
                        }
                    }

                    r.cf.DrawGeometry(points, brush, 1, true);

                    if (drawSamples)
                    {
                        var selectionValid = IsSelectionValid();

                        for (int i = 0; i < points.GetLength(0); i++)
                        {
                            var selected = isSource && selectionValid && indices[i] >= selectionMin && indices[i] <= selectionMax;
                            var sampleScale = selected ? 1.5f : 1.0f;

                            r.cf.PushTransform(points[i, 0], points[i, 1], sampleScale, sampleScale);
                            r.cf.FillGeometry(sampleGeometry, selected ? ThemeResources.WhiteBrush : brush);
                            r.cf.PopTransform();
                        }
                    }
                }
            }
        }

        private void RenderWaveform(RenderInfo r)
        {
            if (editMode != EditionMode.DPCM || Width < pianoSizeX)
                return;

            r.cb.PushTranslation(pianoSizeX, headerAndEffectSizeY);

            // Source data range.
            r.cb.FillRectangle(
                GetPixelForWaveTime(0, scrollX), 0,
                GetPixelForWaveTime(editSample.SourceDuration, scrollX), Height, ThemeResources.DarkGreyFillBrush1);

            // Horizontal center line
            var actualHeight = Height - scrollBarThickness;
            var sizeY        = actualHeight - headerAndEffectSizeY;
            var centerY      = sizeY * 0.5f;
            r.cb.DrawLine(0, centerY, Width, centerY, ThemeResources.BlackBrush);

            // Top/bottom dash lines (limits);
            var topY    = waveDisplayPaddingY;
            var bottomY = (actualHeight - headerAndEffectSizeY) - waveDisplayPaddingY;
            r.cb.DrawLine(0, topY,    Width, topY,    ThemeResources.DarkGreyLineBrush1, 1, false, true);
            r.cb.DrawLine(0, bottomY, Width, bottomY, ThemeResources.DarkGreyLineBrush1, 1, false, true);

            // Vertical lines (1.0, 0.1, 0.01 seconds)
            ForEachWaveTimecode(r, (time, x, level, idx) =>
            {
                var modSeconds = Utils.IntegerPow(10, level + 1);
                var modTenths  = Utils.IntegerPow(10, level);

                var brush = ThemeResources.DarkGreyLineBrush1;
                var dash = true;

                if ((idx % modSeconds) == 0)
                {
                    dash = false;
                    brush = ThemeResources.BlackBrush;
                }
                else if ((idx % modTenths) == 0)
                {
                    dash = false;
                    brush = ThemeResources.DarkGreyLineBrush1;
                }

                r.cb.DrawLine(x, 0, x, Height, brush, 1, false, dash);
            });

            // Selection rectangle
            if (IsSelectionValid())
            {
                r.cb.FillRectangle(
                    GetPixelForWaveTime(GetWaveTimeForSample(selectionMin, true),  scrollX), 0, 
                    GetPixelForWaveTime(GetWaveTimeForSample(selectionMax, false), scrollX), Height, selectionBgVisibleBrush);
            }

            // TODO: Make this a constants.
            bool showSamples = zoom > 32.0f;

            // Source waveform
            if (editSample.SourceDataIsWav)
            {
                RenderWave(r, editSample.SourceWavData.Samples, editSample.SourceSampleRate, ThemeResources.LightGreyFillBrush1, true, showSamples);
            }
            else
            {
                RenderDmc(r, editSample.SourceDmcData.Data, editSample.SourceSampleRate, 0.0f, ThemeResources.LightGreyFillBrush1, true, showSamples, editSample.DmcInitialValueDiv2); 
            }

            // Processed waveform
            var processedBrush = r.g.GetSolidBrush(editSample.Color);
            RenderDmc(r, editSample.ProcessedData, editSample.ProcessedSampleRate, editSample.ProcessedStartTime, processedBrush, false, showSamples, editSample.GetVolumeScaleDmcInitialValueDiv2());

            // Play position
            var playPosition = App.PreviewDPCMWavPosition;

            if (playPosition >= 0 && App.PreviewDPCMSampleId == editSample.Id)
            {
                var playTime = playPosition / (float)App.PreviewDPCMSampleRate;
                if (!App.PreviewDPCMIsSource)
                    playTime += editSample.ProcessedStartTime;
                var seekX = GetPixelForWaveTime(playTime, scrollX);
                r.cb.DrawLine(seekX, 0, seekX, Height, App.PreviewDPCMIsSource ? ThemeResources.LightGreyFillBrush1 : processedBrush, 3);
            }

            // Title + source/processed info.
            var textY = bigTextPosY;
            r.cf.DrawText($"Editing DPCM Sample {editSample.Name}", ThemeResources.FontVeryLarge, bigTextPosX, textY, whiteKeyBrush);
            textY += ThemeResources.FontVeryLarge.LineHeight;
            r.cf.DrawText($"Source Data ({(editSample.SourceDataIsWav ? "WAV" : "DMC")}) : {editSample.SourceSampleRate} Hz, {editSample.SourceDataSize} Bytes, {(int)(editSample.SourceDuration * 1000)} ms", ThemeResources.FontMedium, bigTextPosX, textY, whiteKeyBrush);
            textY += ThemeResources.FontMedium.LineHeight;
            r.cf.DrawText($"Processed Data (DMC) : {DPCMSampleRate.GetString(false, App.PalPlayback, true, true, editSample.SampleRate)}, {editSample.ProcessedData.Length} Bytes, {(int)(editSample.ProcessedDuration * 1000)} ms", ThemeResources.FontMedium, bigTextPosX, textY, whiteKeyBrush);
            textY += ThemeResources.FontMedium.LineHeight;
            r.cf.DrawText($"Preview Playback : {DPCMSampleRate.GetString(false, App.PalPlayback, true, true, editSample.PreviewRate)}, {(int)(editSample.GetPlaybackDuration(App.PalPlayback) * 1000)} ms", ThemeResources.FontMedium, bigTextPosX, textY, whiteKeyBrush);

            r.cb.PopTransform();

            if (!string.IsNullOrEmpty(noteTooltip))
            {
                r.cf.DrawText(noteTooltip, ThemeResources.FontLarge, 0, actualHeight - tooltipTextPosY, whiteKeyBrush, TextFlags.Right, Width - tooltipTextPosX);
            }
        }

        public void RenderVideoFrame(Graphics g, int channel, int patternIndex, float noteIndex, float centerNote, int highlightKey, Color highlightColor)
        {
            Debug.Assert(editMode == EditionMode.VideoRecording);

            int noteY = (int)Math.Round(virtualSizeY - centerNote * noteSizeY + noteSizeY / 2);

            editChannel = channel;
            scrollX = (int)Math.Round((Song.GetPatternStartAbsoluteNoteIndex(patternIndex) + noteIndex) * (double)noteSizeX);
            scrollY = noteY - (Height - headerAndEffectSizeY) / 2;
            highlightNote = highlightKey;
            videoKeyColor = highlightColor;

            Utils.DisposeAndNullify(ref whiteKeyPressedBrush);
            Utils.DisposeAndNullify(ref blackKeyPressedBrush);
            whiteKeyPressedBrush = g.CreateSolidBrush(highlightColor);
            blackKeyPressedBrush = g.CreateSolidBrush(highlightColor);

            OnRender(g);
        }

        private void RenderScrollBars(RenderInfo r)
        {
            if (Settings.ScrollBars != Settings.ScrollBarsNone && editMode != EditionMode.VideoRecording)
            {
                bool h = false;
                bool v = false;

                if (GetScrollBarParams(true, out var scrollBarPosX, out var scrollBarSizeX))
                {
                    r.cs.PushTranslation(pianoSizeX - 1, 0);
                    r.cs.FillAndDrawRectangle(0, Height - scrollBarThickness, Width + 1, Height - 1, ThemeResources.DarkGreyFillBrush1, ThemeResources.BlackBrush);
                    r.cs.FillAndDrawRectangle(scrollBarPosX, Height - scrollBarThickness, scrollBarPosX + scrollBarSizeX + 1, Height - 1, ThemeResources.MediumGreyFillBrush1, ThemeResources.BlackBrush);
                    r.cs.PopTransform();
                    h = true;
                }

                if (GetScrollBarParams(false, out var scrollBarPosY, out var scrollBarSizeY))
                {
                    r.cs.PushTranslation(0, headerAndEffectSizeY - 1);
                    r.cs.FillAndDrawRectangle(Width - scrollBarThickness + 1, 0, Width, Height, ThemeResources.DarkGreyFillBrush1, ThemeResources.BlackBrush);
                    r.cs.FillAndDrawRectangle(Width - scrollBarThickness + 1, scrollBarPosY, Width, scrollBarPosY + scrollBarSizeY + 1, ThemeResources.MediumGreyFillBrush1, ThemeResources.BlackBrush);
                    r.cs.PopTransform();
                    v = true;
                }

                // Hide the glitchy area where both scroll bars intersect with a little square.
                if (h && v)
                {
                    r.cs.FillAndDrawRectangle(Width - scrollBarThickness + 1, Height - scrollBarThickness, Width, Height - 1, ThemeResources.DarkGreyFillBrush1, ThemeResources.BlackBrush);
                }
            }
        }

        private void RenderDebug(RenderInfo r)
        {
#if DEBUG
            if (Platform.IsMobile)
            {
                r.cd = r.g.CreateCommandList();
                r.cd.FillRectangle(mouseLastX - 30, mouseLastY - 30, mouseLastX + 30, mouseLastY + 30, ThemeResources.WhiteBrush);
            }
#endif
        }

        protected override void OnRender(Graphics g)
        {
            // Init
            var r = new RenderInfo();

            var minVisibleNoteIdx = Math.Max(GetAbsoluteNoteIndexForPixel(0), 0);
            var maxVisibleNoteIdx = Math.Min(GetAbsoluteNoteIndexForPixel(Width) + 1, Song.GetPatternStartAbsoluteNoteIndex(Song.Length));

            r.g = g;
            r.cc = g.CreateCommandList();
            r.ch = g.CreateCommandList();
            r.cp = g.CreateCommandList();
            r.cb = g.CreateCommandList();
            r.cf = g.CreateCommandList();
            r.cg = g.CreateCommandList();
            r.cz = g.CreateCommandList();

            if (Platform.IsDesktop)
                r.cs = g.CreateCommandList();

            r.maxVisibleNote = NumNotes - Utils.Clamp((int)Math.Floor(scrollY / (float)noteSizeY), 0, NumNotes);
            r.minVisibleNote = NumNotes - Utils.Clamp((int)Math.Ceiling((scrollY + Height - headerAndEffectSizeY) / (float)noteSizeY), 0, NumNotes);
            r.maxVisibleOctave = (int)Math.Ceiling(r.maxVisibleNote / 12.0f);
            r.minVisibleOctave = (int)Math.Floor(r.minVisibleNote / 12.0f);
            r.minVisiblePattern = Utils.Clamp(Song.PatternIndexFromAbsoluteNoteIndex(minVisibleNoteIdx) + 0, 0, Song.Length);
            r.maxVisiblePattern = Utils.Clamp(Song.PatternIndexFromAbsoluteNoteIndex(maxVisibleNoteIdx) + 1, 0, Song.Length);

            if (editMode == EditionMode.DPCM)
            {
                r.minVisibleWaveTime = GetWaveTimeForPixel(0);
                r.maxVisibleWaveTime = GetWaveTimeForPixel(Width - pianoSizeX);
            }

            ConditionalUpdateNoteGeometries(g);

            // Prepare command list.
            RenderHeader(r);
            RenderEffectList(r);
            RenderEffectPanel(r);
            RenderPiano(r);
            RenderNoteArea(r);
            RenderWaveform(r);
            RenderScrollBars(r);
            RenderDebug(r);

            // Submit draw calls.
            var cornerRect = new Rectangle(0, 0, pianoSizeX, headerAndEffectSizeY);
            var headerRect = new Rectangle(pianoSizeX, 0, Width, headerAndEffectSizeY);
            var pianoRect  = new Rectangle(0, headerAndEffectSizeY, pianoSizeX, Height);
            var notesRect  = new Rectangle(pianoSizeX, headerAndEffectSizeY, Width, Height);

            g.Clear(Theme.DarkGreyLineColor2);
            g.DrawCommandList(r.cc, cornerRect);
            g.DrawCommandList(r.ch, headerRect);
            g.DrawCommandList(r.cz, headerRect);
            g.DrawCommandList(r.cp, pianoRect);
            g.DrawCommandList(r.cb, notesRect);
            g.DrawCommandList(r.cf, notesRect);
            g.DrawCommandList(r.cg, notesRect);
            g.DrawCommandList(r.cs, notesRect);
            g.DrawCommandList(r.cd);
        }

        private bool GetScrollBarParams(bool horizontal, out int pos, out int size)
        {
            pos  = 0;
            size = 0;

            if (scrollBarThickness > 0)
            {
                if (horizontal)
                {
                    GetMinMaxScroll(out var minScrollX, out _, out var maxScrollX, out _);

                    if (minScrollX == maxScrollX)
                        return false;

                    int scrollAreaSizeX = Width - pianoSizeX;
                    size = Math.Max(minScrollBarLength, (int)Math.Round(scrollAreaSizeX * Math.Min(1.0f, scrollAreaSizeX / (float)(maxScrollX + scrollAreaSizeX))));
                    pos  = (int)Math.Round((scrollAreaSizeX - size) * (scrollX / (float)maxScrollX));
                    return true;
                }
                else
                {
                    GetMinMaxScroll(out _, out var minScrollY, out _, out var maxScrollY);

                    if (minScrollY == maxScrollY)
                        return false;

                    int scrollAreaSizeY = Height - headerAndEffectSizeY;
                    size = Math.Max(minScrollBarLength, (int)Math.Round(scrollAreaSizeY * Math.Min(1.0f, scrollAreaSizeY / (float)(maxScrollY + scrollAreaSizeY))));
                    pos  = (int)Math.Round((scrollAreaSizeY - size) * (scrollY / (float)maxScrollY));
                    return true;
                }
            }

            return false;
        }

        void ResizeEnvelope(int x, int y)
        {
            var env = EditEnvelope;
            int length = GetAbsoluteNoteIndexForPixel(x - pianoSizeX);

            switch (captureOperation)
            {
                case CaptureOperation.ResizeEnvelope:
                    if (env.Length == length)
                        return;
                    env.Length = length;
                    if (IsSelectionValid())
                        SetSelection(selectionMin, selectionMax);
                    break;
                case CaptureOperation.DragRelease:
                    if (env.Release == length)
                        return;
                    env.Release = length;
                    break;
                case CaptureOperation.DragLoop:
                    if (env.Loop == length)
                        return;
                    env.Loop = length;
                    break;
            }

            ClampScroll();
            MarkDirty();
        }

        void StartChangeEffectValue(int x, int y, NoteLocation location)
        {
            var channel   = Song.Channels[editChannel];
            var pattern   = channel.PatternInstances[location.PatternIndex];
            var note      = channel.GetNoteAt(location);
            var selection = IsSelectionValid() && IsNoteSelected(location) && note != null && note.HasValidEffectValue(selectedEffectIdx);

            StartCaptureOperation(x, y, selection ? CaptureOperation.ChangeSelectionEffectValue : CaptureOperation.ChangeEffectValue, false, location.ToAbsoluteNoteIndex(Song));

            var minPatternIdx = Song.PatternIndexFromAbsoluteNoteIndex(selectionMin);
            var maxPatternIdx = Song.PatternIndexFromAbsoluteNoteIndex(selectionMax);

            if (selection && minPatternIdx != maxPatternIdx || pattern == null)
            {
                App.UndoRedoManager.BeginTransaction(TransactionScope.Channel, Song.Id, editChannel);

                if (pattern == null)
                    pattern = channel.CreatePatternAndInstance(location.PatternIndex);
            }
            else
            {
                App.UndoRedoManager.BeginTransaction(TransactionScope.Pattern, pattern.Id);
            }

            UpdateChangeEffectValue(x, y);
        }

        void UpdateChangeEffectValue(int x, int y)
        {
            Debug.Assert(selectedEffectIdx >= 0);

            App.UndoRedoManager.RestoreTransaction(false);

            var shift = ModifierKeys.Shift;

            var channel = Song.Channels[editChannel];
            var pattern = channel.PatternInstances[captureNoteLocation.PatternIndex];

            if (pattern == null)
                pattern = channel.CreatePatternAndInstance(captureNoteLocation.PatternIndex);

            var minValue = Note.GetEffectMinValue(Song, channel, selectedEffectIdx);
            var maxValue = Note.GetEffectMaxValue(Song, channel, selectedEffectIdx);

            var note = pattern.GetOrCreateNoteAt(captureNoteLocation.NoteIndex);

            if (!note.HasValidEffectValue(selectedEffectIdx))
                note.SetEffectValue(selectedEffectIdx, Note.GetEffectDefaultValue(Song, selectedEffectIdx));

            var delta = 0;

            if (shift)
            {
                delta = (captureMouseY - y) / 4;
            }
            else if (Platform.IsDesktop)
            {
                var ratio = (y - headerSizeY) / (float)effectPanelSizeY;
                var newValue = (int)Math.Round(Utils.Lerp(maxValue, minValue, ratio));

                var originalValue = note.GetEffectValue(selectedEffectIdx);
                delta = newValue - originalValue;
            }
            else // On mobile we drag using gizmos
            {
                var origRatio = (captureMouseY - headerSizeY) / (float)effectPanelSizeY;
                var origValue = (int)Math.Round(Utils.Lerp(maxValue, minValue, origRatio));
                var newRatio  = (y - headerSizeY) / (float)effectPanelSizeY;
                var newValue  = (int)Math.Round(Utils.Lerp(maxValue, minValue, newRatio));

                delta = newValue - origValue;
            }

            if (captureOperation == CaptureOperation.ChangeSelectionEffectValue)
            {
                var minLocation = NoteLocation.FromAbsoluteNoteIndex(Song, selectionMin);
                var maxLocation = NoteLocation.FromAbsoluteNoteIndex(Song, selectionMax);

                for (var it = channel.GetSparseNoteIterator(minLocation, maxLocation, Note.GetFilterForEffect(selectedEffectIdx)); !it.Done; it.Next())
                {
                    var value = it.Note.GetEffectValue(selectedEffectIdx);
                    it.Note.SetEffectValue(selectedEffectIdx, Utils.Clamp(value + delta, minValue, maxValue));
                }

                channel.InvalidateCumulativePatternCache(minLocation.PatternIndex, maxLocation.PatternIndex);
            }
            else
            {
                var value = note.GetEffectValue(selectedEffectIdx);
                note.SetEffectValue(selectedEffectIdx, Utils.Clamp(value + delta, minValue, maxValue));

                channel.InvalidateCumulativePatternCache(pattern);
            }

            MarkDirty();
        }

        void StartDragVolumeSlide(int x, int y, NoteLocation location)
        {
            StartCaptureOperation(x, y, CaptureOperation.DragVolumeSlideTarget, false, location.ToAbsoluteNoteIndex(Song));

            var channel = Song.Channels[editChannel];
            var pattern = channel.PatternInstances[location.PatternIndex];

            if (pattern == null)
            {
                App.UndoRedoManager.BeginTransaction(TransactionScope.Channel, Song.Id, editChannel);
                pattern = channel.CreatePatternAndInstance(location.PatternIndex);
            }
            else
            {
                App.UndoRedoManager.BeginTransaction(TransactionScope.Pattern, pattern.Id);
            }

            var note = pattern.GetOrCreateNoteAt(captureNoteLocation.NoteIndex);

            var ratio = Utils.Clamp(1.0f - (y - headerSizeY) / (float)effectPanelSizeY, 0.0f, 1.0f);
            var volume = (byte)Math.Round(ratio * Note.VolumeMax);

            if (!note.HasVolume)
                note.Volume = volume;
            note.VolumeSlideTarget = volume;

            pattern.InvalidateCumulativeCache();
        }

        void StartDragVolumeSlideGizmo(int x, int y, Note note, NoteLocation location)
        {
            var channel = Song.Channels[editChannel];
            var pattern = channel.PatternInstances[location.PatternIndex];
            var offsetY = GetEffectCoordY(Note.EffectVolumeSlide, note.VolumeSlideTarget) - y;

            StartCaptureOperation(x, y, CaptureOperation.DragVolumeSlideTargetGizmo, false, location.ToAbsoluteNoteIndex(Song), offsetY);
            App.UndoRedoManager.BeginTransaction(TransactionScope.Pattern, pattern.Id);
        }

        void UpdateDragVolumeSlide(int x, int y, bool final)
        {
            if (Platform.IsMobile)
                App.UndoRedoManager.RestoreTransaction(false);

            var channel = Song.Channels[editChannel];
            var pattern = channel.PatternInstances[captureNoteLocation.PatternIndex];
            var note    = pattern.Notes[captureNoteLocation.NoteIndex];

            if (Platform.IsDesktop)
            {
                var ratio = Utils.Clamp(1.0f - (y - headerSizeY) / (float)effectPanelSizeY, 0.0f, 1.0f);
                note.VolumeSlideTarget = (byte)Math.Round(ratio * Note.VolumeMax);
            }
            else // On mobile we drag using gizmos, so apply a delta.
            {
                var origValue = (int)Math.Round(Utils.Lerp(Note.VolumeMax, 0, (captureMouseY - headerSizeY) / (float)effectPanelSizeY));
                var newValue  = (int)Math.Round(Utils.Lerp(Note.VolumeMax, 0, (y - headerSizeY) / (float)effectPanelSizeY));

                var delta = newValue - origValue;
                note.VolumeSlideTarget = (byte)Utils.Clamp(note.VolumeSlideTarget + delta, 0, Note.VolumeMax);
            }

            if (final)
            {
                if (note.VolumeSlideTarget == note.Volume)
                    note.HasVolumeSlide = false;

                pattern.InvalidateCumulativeCache();
                App.UndoRedoManager.EndTransaction();
            }
            else
            {
                pattern.InvalidateCumulativeCache();
            }

            MarkDirty();
        }

        void DrawEnvelope(int x, int y, bool first = false)
        {
            if (GetEnvelopeValueForCoord(x, y, out int idx1, out sbyte val1))
            {
                int idx0;
                sbyte val0;

                if (first || !GetEnvelopeValueForCoord(mouseLastX, mouseLastY, out idx0, out val0))
                {
                    idx0 = idx1;
                    val0 = val1;
                }

                Envelope.GetMinMaxValueForType(editInstrument, editEnvelope, out int min, out int max);

                var env = EditEnvelope;

                idx0 = Utils.Clamp(idx0, 0, env.Length - 1);
                idx1 = Utils.Clamp(idx1, 0, env.Length - 1);

                if (idx0 != idx1)
                {
                    if (idx1 < idx0)
                    {
                        Utils.Swap(ref idx0, ref idx1);
                        Utils.Swap(ref val0, ref val1);
                    }

                    for (int i = idx0; i <= idx1; i++)
                    {
                        int val = (int)Math.Round(Utils.Lerp(val0, val1, (i - idx0) / (float)(idx1 - idx0)));
                        env.Values[i] = (sbyte)Utils.Clamp(val, min, max);
                    }
                }
                else
                {
                    env.Values[idx0] = (sbyte)Utils.Clamp(val0, min, max);
                }

                MarkDirty();
            }
        }

        void DrawSingleEnvelopeValue(int x, int y)
        {
            if (GetEnvelopeValueForCoord(x, y, out int idx, out sbyte val))
            {
                var env = EditEnvelope;
                idx = Utils.Clamp(idx, 0, env.Length - 1);

                Envelope.GetMinMaxValueForType(editInstrument, editEnvelope, out int min, out int max);

                if (editMode == EditionMode.Enveloppe)
                    App.UndoRedoManager.BeginTransaction(TransactionScope.Instrument, editInstrument.Id);
                else
                    App.UndoRedoManager.BeginTransaction(TransactionScope.Arpeggio, editArpeggio.Id);

                env.Values[idx] = (sbyte)Utils.Clamp(val, min, max);
                highlightNoteAbsIndex = idx;

                App.UndoRedoManager.EndTransaction();
            }
        }

        private void UpdateWavePreset(bool final)
        {
            if (editMode == EditionMode.Enveloppe)
            {
                if (editInstrument.IsFdsInstrument)
                {
                    if (editEnvelope == EnvelopeType.FdsWaveform)
                        editInstrument.FdsWavePreset = WavePresetType.Custom;
                    if (editEnvelope == EnvelopeType.FdsModulation)
                        editInstrument.FdsModPreset = WavePresetType.Custom;
                }
                else if (editInstrument.IsN163Instrument)
                {
                    if (editEnvelope == EnvelopeType.N163Waveform)
                        editInstrument.N163WavePreset = WavePresetType.Custom;
                }
            }

            EnvelopeChanged?.Invoke();

            if (final)
                App.UndoRedoManager.EndTransaction();
        }

        protected int GetPianoNote(int x, int y)
        {
            y -= headerAndEffectSizeY;

            for (int i = 0; i < NumOctaves; i++)
            {
                for (int j = 0; j < 12 && i * 12 + j < NumNotes; j++)
                {
                    if (IsBlackKey(j) && GetKeyRectangle(i, j).Contains(x, y))
                        return i * 12 + j + 1;
                }
                for (int j = 0; j < 12 && i * 12 + j < NumNotes; j++)
                {
                    if (!IsBlackKey(j) && GetKeyRectangle(i, j).Contains(x, y))
                        return i * 12 + j + 1;
                }
            }

            return -1;
        }

        protected void PlayPiano(int x, int y)
        {
            var note = GetPianoNote(x, y);
            if (note >= 0)
            {
                if (note != playingNote)
                {
                    playingNote = note;
                    App.PlayInstrumentNote(playingNote, true, true);
                    MarkDirty();
                }
            }
        }

        private void EditDPCMSampleMappingProperties(Point pt, DPCMSampleMapping mapping)
        {
            var strings = DPCMSampleRate.GetStringList(true, FamiStudio.StaticInstance.PalPlayback, true, true);

            var dlg = new PropertyDialog("DPCM Key Properties", new Point(left + pt.X, top + pt.Y), 280, false, pt.Y > Height / 2);
            dlg.Properties.AddDropDownList("Pitch :", strings, strings[mapping.Pitch]); // 0
            dlg.Properties.AddCheckBox("Loop :", mapping.Loop); // 1
            dlg.Properties.AddCheckBox("Override DMC Initial Value :", mapping.OverrideDmcInitialValue); // 2
            dlg.Properties.AddNumericUpDown("DMC Initial Value (÷2) :", mapping.DmcInitialValueDiv2, 0, 63); // 3
            dlg.Properties.Build();
            dlg.Properties.SetPropertyEnabled(3, mapping.OverrideDmcInitialValue);
            dlg.Properties.PropertyChanged += DPCMSampleMapping_PropertyChanged;

            dlg.ShowDialogAsync(ParentForm, (r) =>
            {
                if (r == DialogResult2.OK)
                {
                    App.UndoRedoManager.BeginTransaction(TransactionScope.DPCMSamplesMapping);
                    mapping.Pitch = dlg.Properties.GetSelectedIndex(0);
                    mapping.Loop = dlg.Properties.GetPropertyValue<bool>(1);
                    mapping.OverrideDmcInitialValue = dlg.Properties.GetPropertyValue<bool>(2);
                    mapping.DmcInitialValueDiv2 = dlg.Properties.GetPropertyValue<int>(3);
                    App.UndoRedoManager.EndTransaction();
                    MarkDirty();
                }
            });
        }

        private void DPCMSampleMapping_PropertyChanged(PropertyPage props, int propIdx, int rowIdx, int colIdx, object value)
        {
            if (propIdx == 2)
            {
                props.SetPropertyEnabled(3, (bool)value);
            }
        }

        private bool HandleDoubleClickChannelNote(MouseEventArgs e)
        {
            if (e.Left && GetLocationForCoord(e.X, e.Y, out var mouseLocation, out byte noteValue) && mouseLocation.IsInSong(Song))
            {
                var channel = Song.Channels[editChannel];
                var noteLocation = mouseLocation;
                var note = channel.FindMusicalNoteAtLocation(ref noteLocation, noteValue);

                if (note != null)
                {
                    AbortCaptureOperation(); 
                    DeleteSingleNote(noteLocation, mouseLocation, note);
                }

                return true;
            }

            return false;
        }

        private bool HandleDoubleClickEffectPanel(MouseEventArgs e)
        {
            if (e.Left && selectedEffectIdx >= 0 && IsPointInEffectPanel(e.X, e.Y) && GetEffectNoteForCoord(e.X, e.Y, out var location))
            {
                var channel = Song.Channels[editChannel];
                var pattern = channel.PatternInstances[location.PatternIndex];

                if (pattern != null && pattern.TryGetNoteWithEffectAt(location.NoteIndex, selectedEffectIdx, out var note))
                {
                    AbortCaptureOperation();
                    ClearEffectValue(location, note);
                }

                return true;
            }

            return false;

        }

        private bool HandleDoubleClickDPCMMapping(MouseEventArgs e)
        {
            if (GetLocationForCoord(e.X, e.Y, out _, out var noteValue))
            {
                var mapping = App.Project.GetDPCMMapping(noteValue);
                if (mapping != null)
                    ClearDPCMSampleMapping(noteValue);
                return true;
            }

            return true;
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            if (editMode == EditionMode.Channel)
            {
                if (HandleDoubleClickChannelNote(e)) goto Handled;
                if (HandleDoubleClickEffectPanel(e)) goto Handled;
            }

            if (editMode == EditionMode.DPCMMapping)
            {
                if (HandleDoubleClickDPCMMapping(e)) goto Handled;
            }

            return;

        Handled:
            MarkDirty();
        }

        private void CaptureMouse(int x, int y)
        {
            SetMouseLastPos(x, y);
            captureMouseX = x;
            captureMouseY = y;
            captureScrollX = scrollX;
            captureScrollY = scrollY;
            Capture = true;
        }

        private void StartCaptureOperation(int x, int y, CaptureOperation op, bool allowSnap = false, int noteIdx = -1, int offsetY = 0)
        {
#if DEBUG
            Debug.Assert(captureOperation == CaptureOperation.None);
#else
            if (captureOperation != CaptureOperation.None)
                AbortCaptureOperation();
#endif

            CaptureMouse(x, y);
            captureOperation = op;
            captureThresholdMet = !captureNeedsThreshold[(int)op];
            captureRealTimeUpdate = captureWantsRealTimeUpdate[(int)op];
            captureWaveTime = editMode == EditionMode.DPCM ? GetWaveTimeForPixel(x - pianoSizeX) : 0.0f;
            captureNoteValue = NumNotes - Utils.Clamp((y + scrollY - headerAndEffectSizeY) / noteSizeY, 0, NumNotes);
            captureSelectionMin = selectionMin;
            captureSelectionMax = selectionMax;
            captureOffsetY = offsetY;
            canFling = false;

            if (editMode == EditionMode.Enveloppe || editMode == EditionMode.Arpeggio)
                GetEnvelopeValueForCoord(x, y, out _, out captureEnvelopeValue);

            captureMouseAbsoluteIdx = GetAbsoluteNoteIndexForPixel(x - pianoSizeX);
            if (allowSnap)
                captureMouseAbsoluteIdx = SnapNote(captureMouseAbsoluteIdx);

            captureNoteAbsoluteIdx = noteIdx >= 0 ? noteIdx : captureMouseAbsoluteIdx;
            captureNoteLocation = Song.AbsoluteNoteIndexToNoteLocation(captureNoteAbsoluteIdx);
            
            if (noteIdx >= 0)
                highlightNoteAbsIndex = captureNoteAbsoluteIdx;
        }

        private void UpdateScrollBarX(int x, int y)
        {
            GetScrollBarParams(true, out _, out var scrollBarSizeX);
            GetMinMaxScroll(out _, out _, out var maxScrollX, out _);

            int scrollAreaSizeX = Width - pianoSizeX;
            scrollX = (int)Math.Round(captureScrollX + ((x - captureMouseX) / (float)(scrollAreaSizeX - scrollBarSizeX) * maxScrollX));

            ClampScroll();
            MarkDirty();
        }

        private void UpdateScrollBarY(int x, int y)
        {
            GetScrollBarParams(false, out _, out var scrollBarSizeY);
            GetMinMaxScroll(out _, out _, out _, out var maxScrollY);

            int scrollAreaSizeY = Height - headerAndEffectSizeY;
            scrollY = (int)Math.Round(captureScrollY + ((y - captureMouseY) / (float)(scrollAreaSizeY - scrollBarSizeY) * maxScrollY));

            ClampScroll();
            MarkDirty();
        }

        private void UpdateCaptureOperation(int x, int y, float scale = 1.0f, bool realTime = false)
        {  
            const int CaptureThreshold = Platform.IsDesktop ? 5 : 50;

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
                y += captureOffsetY;

                switch (captureOperation)
                {
                    case CaptureOperation.DragLoop:
                    case CaptureOperation.DragRelease:
                    case CaptureOperation.ResizeEnvelope:
                        ResizeEnvelope(x, y);
                        break;
                    case CaptureOperation.PlayPiano:
                        PlayPiano(x, y);
                        break;
                    case CaptureOperation.ChangeEffectValue:
                    case CaptureOperation.ChangeSelectionEffectValue:
                        UpdateChangeEffectValue(x, y);
                        break;
                    case CaptureOperation.DragSample:
                        UpdateDragDPCMSampleMapping(x, y);
                        break;
                    case CaptureOperation.DrawEnvelope:
                        DrawEnvelope(x, y);
                        break;
                    case CaptureOperation.Select:
                        UpdateSelection(x, y);
                        break;
                    case CaptureOperation.SelectWave:
                        UpdateWaveSelection(x, y);
                        break;
                    case CaptureOperation.CreateSlideNote:
                    case CaptureOperation.DragSlideNoteTarget:
                        UpdateSlideNoteCreation(x, y, false);
                        break;
                    case CaptureOperation.DragSlideNoteTargetGizmo:
                        UpdateSlideNoteCreation(x, y, false, true);
                        break;
                    case CaptureOperation.DragVolumeSlideTarget:
                    case CaptureOperation.DragVolumeSlideTargetGizmo:
                        UpdateDragVolumeSlide(x, y, false);
                        break;
                    case CaptureOperation.CreateNote:
                        UpdateNoteCreation(x, y, false, false);
                        break;
                    case CaptureOperation.ResizeNoteEnd:
                    case CaptureOperation.ResizeSelectionNoteEnd:
                        UpdateNoteResizeEnd(x, y, false);
                        break;
                    case CaptureOperation.MoveNoteRelease:
                        UpdateMoveNoteRelease(x, y);
                        break;
                    case CaptureOperation.DragNote:
                    case CaptureOperation.DragSelection:
                    case CaptureOperation.ResizeNoteStart:
                    case CaptureOperation.ResizeSelectionNoteStart:
                        UpdateNoteDrag(x, y, false);
                        break;
                    case CaptureOperation.AltZoom:
                        UpdateAltZoom(x, y);
                        break;
                    case CaptureOperation.DragSeekBar:
                        UpdateSeekDrag(x, y, false);
                        break;
                    case CaptureOperation.DragWaveVolumeEnvelope:
                        UpdateVolumeEnvelopeDrag(x, y, false);
                        break;
                    case CaptureOperation.ChangeEnvelopeValue:
                        UpdateChangeEnvelopeValue(x, y);
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
                    case CaptureOperation.MobileZoomVertical:
                        ZoomVerticallyAtLocation(y, scale);
                        break;
                    case CaptureOperation.MobileZoom:
                        ZoomAtLocation(x, scale);
                        DoScroll(x - mouseLastX, y - mouseLastY);
                        break;
                }
            }
        }
        private void UpdateDragDPCMSampleMapping(int x, int y)
        {
            if (draggedSample == null)
            {
                App.UndoRedoManager.BeginTransaction(TransactionScope.DPCMSamplesMapping);
                draggedSample = App.Project.GetDPCMMapping(captureNoteValue);
                App.Project.UnmapDPCMSample(captureNoteValue);
            }
            else
            {
                ScrollIfNearEdge(x, y, false, true);
                MarkDirty();
            }
        }

        private void EndDragDPCMSampleMapping(int x, int y)
        {
            if (draggedSample != null)
            {
                if (GetNoteValueForCoord(x, y, out var noteValue) && App.Project.NoteSupportsDPCM(noteValue) && noteValue != captureNoteValue && draggedSample != null)
                {
                    var sample = draggedSample;

                    // Map the sample right away so that it renders correctly as the message box pops.
                    App.Project.UnmapDPCMSample(noteValue);
                    App.Project.MapDPCMSample(noteValue, sample.Sample, sample.Pitch, sample.Loop);

                    draggedSample = null;

                    Platform.MessageBoxAsync($"Do you want to transpose all the notes using this sample?", "Remap DPCM Sample", MessageBoxButtons2.YesNo, (r) =>
                    {
                        if (r == DialogResult2.Yes)
                        {
                            // Need to promote the transaction to project level since we are going to be transposing 
                            // potentially in multiple songs.
                            App.UndoRedoManager.RestoreTransaction(false);
                            App.UndoRedoManager.AbortTransaction();
                            App.UndoRedoManager.BeginTransaction(TransactionScope.Project);

                            // Need to redo everything + transpose.
                            App.Project.UnmapDPCMSample(captureNoteValue);
                            App.Project.UnmapDPCMSample(noteValue);
                            App.Project.MapDPCMSample(noteValue, sample.Sample, sample.Pitch, sample.Loop);
                            App.Project.TransposeDPCMMapping(captureNoteValue, noteValue);
                        }

                        DPCMSampleMapped?.Invoke(noteValue);
                        ManyPatternChanged?.Invoke();

                        App.UndoRedoManager.EndTransaction();
                    });

                    if (Platform.IsMobile)
                        highlightDPCMSample = noteValue;
                }
                else
                {
                    App.UndoRedoManager.RestoreTransaction(false);
                    App.UndoRedoManager.AbortTransaction();

                    if (noteValue != captureNoteValue && draggedSample != null)
                        Platform.Beep();
                }
            }
        }

        private void EndCaptureOperation(int x, int y)
        {
            if (captureOperation != CaptureOperation.None)
            {
                y += captureOffsetY;

                switch (captureOperation)
                {
                    case CaptureOperation.PlayPiano:
                        EndPlayPiano();
                        break;
                    case CaptureOperation.ResizeEnvelope:
                    case CaptureOperation.DrawEnvelope:
                        UpdateWavePreset(true);
                        break;
                    case CaptureOperation.CreateSlideNote:
                    case CaptureOperation.DragSlideNoteTarget:
                        UpdateSlideNoteCreation(x, y, true);
                        break;
                    case CaptureOperation.DragSlideNoteTargetGizmo:
                        UpdateSlideNoteCreation(x, y, true, true);
                        break;
                    case CaptureOperation.DragVolumeSlideTarget:
                    case CaptureOperation.DragVolumeSlideTargetGizmo:
                        UpdateDragVolumeSlide(x, y, true);
                        break;
                    case CaptureOperation.CreateNote:
                        UpdateNoteCreation(x, y, false, true);
                        break;
                    case CaptureOperation.ResizeNoteEnd:
                    case CaptureOperation.ResizeSelectionNoteEnd:
                        UpdateNoteResizeEnd(x, y, true);
                        break;
                    case CaptureOperation.DragNote:
                    case CaptureOperation.DragSelection:
                    case CaptureOperation.ResizeNoteStart:
                    case CaptureOperation.ResizeSelectionNoteStart:
                        UpdateNoteDrag(x, y, true);
                        break;
                    case CaptureOperation.DragSample:
                        EndDragDPCMSampleMapping(x, y);
                        break;
                    case CaptureOperation.DragSeekBar:
                        UpdateSeekDrag(x, y, true);
                        break;
                    case CaptureOperation.DragWaveVolumeEnvelope:
                        UpdateVolumeEnvelopeDrag(x, y, true);
                        break;
                    case CaptureOperation.MobilePan:
                    case CaptureOperation.MobileZoom:
                        canFling = true;
                        break;
                    case CaptureOperation.MobileZoomVertical:
                        break;
                    case CaptureOperation.DragLoop:
                    case CaptureOperation.DragRelease:
                    case CaptureOperation.ChangeEffectValue:
                    case CaptureOperation.ChangeSelectionEffectValue:
                    case CaptureOperation.MoveNoteRelease:
                    case CaptureOperation.ChangeEnvelopeValue:
                        App.UndoRedoManager.EndTransaction();
                        break;
                }

                draggedSample = null;
                captureOperation = CaptureOperation.None;
                Capture = false;
                panning = false;
                if (!Platform.IsMobile)
                    highlightNoteAbsIndex = -1;

                MarkDirty();
            }
        }

        private void AbortCaptureOperation(bool restore = false)
        {
            if (captureOperation != CaptureOperation.None)
            {
                if (App.UndoRedoManager.HasTransactionInProgress)
                {
                    if (restore)
                        App.UndoRedoManager.RestoreTransaction(false);
                    App.UndoRedoManager.AbortTransaction();
                }

                MarkDirty();
                App.StopInstrument();

                captureOperation = CaptureOperation.None;
                Capture = false;
                panning = false;
                canFling = false;
                if (!Platform.IsMobile)
                    highlightNoteAbsIndex = -1;

                ManyPatternChanged?.Invoke();
            }
        }

        public bool IsSelectionValid()
        {
            return selectionMin >= 0 && selectionMax >= 0;
        }

        private bool SelectionCoversMultiplePatterns()
        {
            return IsSelectionValid() && Song.PatternIndexFromAbsoluteNoteIndex(selectionMin) != Song.PatternIndexFromAbsoluteNoteIndex(selectionMax);
        }
        
        private void MoveNotes(int amount)
        {
            if (selectionMin + amount >= 0)
            {
                App.UndoRedoManager.BeginTransaction(TransactionScope.Channel, Song.Id, editChannel);
                var notes = GetSelectedNotes();
                DeleteSelectedNotes(false);
                ReplaceNotes(notes, selectionMin + amount, false);
                App.UndoRedoManager.EndTransaction();
            }
        }

        private void TransformNotes(int minAbsoluteNoteIdx, int maxAbsoluteNoteIdx, bool doTransaction, bool doPatternChangeEvent, bool createMissingPatterns, Func<Note, int, Note> function)
        {
            if (doTransaction)
                App.UndoRedoManager.BeginTransaction(TransactionScope.Channel, Song.Id, editChannel);

            var channel     = Song.Channels[editChannel];
            var minLocation = Song.AbsoluteNoteIndexToNoteLocation(minAbsoluteNoteIdx);
            var maxLocation = Song.AbsoluteNoteIndexToNoteLocation(maxAbsoluteNoteIdx);

            for (var p = minLocation.PatternIndex; p <= maxLocation.PatternIndex; p++)
            {
                var pattern = channel.PatternInstances[p];

                if (pattern == null)
                {
                    if (createMissingPatterns && p < Song.Length)
                    {
                        pattern = channel.CreatePatternAndInstance(p);
                    }
                    else
                    {
                        continue;
                    }
                }

                var patternLen = Song.GetPatternLength(p);
                var n0 = p == minLocation.PatternIndex ? minLocation.NoteIndex : 0;
                var n1 = p == maxLocation.PatternIndex ? maxLocation.NoteIndex : patternLen - 1;
                var newNotes = new SortedList<int, Note>();

                for (var it = pattern.GetDenseNoteIterator(n0, n1 + 1); !it.Done; it.Next())
                {
                    var transformedNote = function(it.CurrentNote, Song.GetPatternStartAbsoluteNoteIndex(p) + it.CurrentTime - minAbsoluteNoteIdx);
                    if (transformedNote != null)
                        newNotes[it.CurrentTime] = transformedNote;
                }

                pattern.DeleteNotesBetween(n0, n1 + 1);

                foreach (var kv in newNotes)
                    pattern.SetNoteAt(kv.Key, kv.Value);

                if (doPatternChangeEvent)
                    PatternChanged?.Invoke(pattern);
            }

            channel.InvalidateCumulativePatternCache(minLocation.PatternIndex, maxLocation.PatternIndex);

            if (doTransaction)
                App.UndoRedoManager.EndTransaction();

            MarkDirty();
        }

        private void TransformEnvelopeValues(int startFrameIdx, int endFrameIdx, Func<sbyte, int, sbyte> function)
        {
            if (editMode == EditionMode.Arpeggio)
                App.UndoRedoManager.BeginTransaction(TransactionScope.Arpeggio, editArpeggio.Id);
            else
                App.UndoRedoManager.BeginTransaction(TransactionScope.Instrument, editInstrument.Id);

            Envelope.GetMinMaxValueForType(editInstrument, editEnvelope, out int minVal, out int maxVal);

            startFrameIdx = Math.Max(startFrameIdx, 0);
            endFrameIdx   = Math.Min(endFrameIdx, EditEnvelope.Length - 1);

            for (int i = startFrameIdx; i <= endFrameIdx; i++)
                EditEnvelope.Values[i] = (sbyte)Utils.Clamp(function(EditEnvelope.Values[i], i - startFrameIdx), minVal, maxVal);

            UpdateWavePreset(false);
            App.UndoRedoManager.EndTransaction();
            MarkDirty();
        }

        private void TransposeNotes(int amount)
        {
            var processedNotes = new HashSet<Note>();

            TransformNotes(selectionMin, selectionMax, true, true, false, (note, idx) =>
            {
                if (note != null && note.IsMusical && !processedNotes.Contains(note))
                {
                    int value = note.Value + amount;
                    if (value < Note.MusicalNoteMin || value > Note.MusicalNoteMax)
                    {
                        note.Clear();
                    }
                    else
                    {
                        note.Value = (byte)value;
                        if (note.IsSlideNote)
                            note.SlideNoteTarget = (byte)Utils.Clamp(note.SlideNoteTarget + amount, Note.MusicalNoteMin, Note.MusicalNoteMax);
                    }

                    processedNotes.Add(note);
                }

                return note;
            });
        }

        private void IncrementEnvelopeValues(int amount)
        {
            TransformEnvelopeValues(selectionMin, selectionMax, (val, idx) =>
            {
                return (sbyte)Utils.Clamp(val + amount, sbyte.MinValue, sbyte.MaxValue);
            });
        }

        private void MoveEnvelopeValues(int amount)
        {
            if (selectionMin + amount >= 0)
                ReplaceEnvelopeValues(GetSelectedEnvelopeValues(), selectionMin + amount);
        }

        private void DeleteSelectedNotes(bool doTransaction = true, bool deleteNotes = true, int deleteEffectsMask = Note.EffectAllMask)
        {
            TransformNotes(selectionMin, selectionMax, doTransaction, true, false, (note, idx) =>
            {
                if (note != null)
                {
                    if (deleteNotes && deleteEffectsMask == Note.EffectAllMask)
                    {
                        note.Clear(false);
                    }
                    else
                    {
                        if (deleteNotes)
                            note.Clear(true);

                        for (int i = 0; i < Note.EffectCount; i++)
                        {
                            if ((deleteEffectsMask & (1 << i)) != 0)
                                note.ClearEffectValue(i);
                        }
                    }
                }
                return note;
            });
        }

        private void DeleteSelectedEnvelopeValues()
        {
            TransformEnvelopeValues(selectionMin, selectionMax, (val, idx) =>
            {
                return 0;
            });
        }

        private void FlattenEnvelopeValues(int refValueIdx)
        {
            var value = EditEnvelope.Values[refValueIdx];

            TransformEnvelopeValues(selectionMin, selectionMax, (val, idx) =>
            {
                return value;
            });
        }

        private void DeleteSelectedWaveSection()
        {
            if (IsSelectionValid())
            {
                App.UndoRedoManager.BeginTransaction(TransactionScope.DPCMSample, editSample.Id);
                if (editSample.TrimSourceSourceData(selectionMin, selectionMax))
                {
                    editSample.Process();
                    App.UndoRedoManager.EndTransaction();
                    DPCMSampleChanged?.Invoke();
                }
                else
                {
                    App.UndoRedoManager.AbortTransaction();
                }

                ClearSelection();
                ClampScroll();
                MarkDirty();
            }
        }

        private Note GetNoteForDesktopNoteGizmos(out NoteLocation location)
        {
            Debug.Assert(Platform.IsDesktop);
            location = NoteLocation.Invalid;

            if (captureOperation == CaptureOperation.DragSlideNoteTargetGizmo)
            {
                location = captureNoteLocation;
            }
            else
            {
                var pt = PointToClient(Cursor.Position);
                if (!GetLocationForCoord(pt.X, pt.Y, out var mouseLocation, out byte noteValue) || !mouseLocation.IsInSong(Song))
                    return null;
                location = mouseLocation;
            }

            return Song.Channels[editChannel].FindMusicalNoteAtLocation(ref location, -1);
        }

        private Note GetNoteForDesktopEffectGizmos(out NoteLocation location)
        {
            Debug.Assert(Platform.IsDesktop);
            location = NoteLocation.Invalid;

            if (captureOperation == CaptureOperation.DragVolumeSlideTargetGizmo) 
            {
                location = captureNoteLocation;
            }
            else
            {
                var pt = PointToClient(Cursor.Position);
                if (!GetEffectNoteForCoord(pt.X, pt.Y, out var mouseLocation) || !mouseLocation.IsInSong(Song))
                    return null;
                location = mouseLocation;
            }

            var channel = Song.Channels[editChannel];
            location = channel.GetLastEffectLocation(location, Note.EffectVolumeSlide);

            if (location.IsValid)
                return channel.GetNoteAt(location);

            return null;
        }

        private List<Gizmo> GetNoteGizmos(out Note note, out NoteLocation location)
        {
            note = Platform.IsDesktop ? 
                GetNoteForDesktopNoteGizmos(out location) :
                GetHighlightedNoteAndLocation(out location);

            if (note == null || !note.IsMusical)
                return null;

            var locationAbsIndex = location.ToAbsoluteNoteIndex(Song);
            var visualDuration = GetVisualNoteDuration(locationAbsIndex, note);
            var list = new List<Gizmo>();

            // Resize gizmo
            if (Platform.IsMobile)
            {
                var x = GetPixelForNote(locationAbsIndex + visualDuration) + gizmoSize / 4;
                var y = virtualSizeY - note.Value * noteSizeY - scrollY - gizmoSize / 4;

                if (captureOperation == CaptureOperation.ResizeNoteEnd ||
                    captureOperation == CaptureOperation.ResizeSelectionNoteEnd)
                {
                    x = mouseLastX - pianoSizeX - gizmoSize / 2;
                }

                Gizmo resizeGizmo = new Gizmo();
                resizeGizmo.Image = bmpGizmoResizeLeftRight;
                resizeGizmo.FillImage = bmpGizmoResizeFill;
                resizeGizmo.Action = GizmoAction.ResizeNote;
                resizeGizmo.Rect = new Rectangle(x, y, gizmoSize, gizmoSize);
                list.Add(resizeGizmo);
            }

            // Release gizmo
            if (Platform.IsMobile && note.HasRelease && note.Release < visualDuration)
            {
                var x = GetPixelForNote(locationAbsIndex + note.Release) - gizmoSize / 2;
                var y = virtualSizeY - note.Value * noteSizeY - scrollY + gizmoSize * 3 / 4;

                if (captureOperation == CaptureOperation.MoveNoteRelease)
                {
                    x = mouseLastX - pianoSizeX - gizmoSize / 2;
                }

                Gizmo releaseGizmo = new Gizmo();
                releaseGizmo.Image = bmpGizmoResizeLeftRight;
                releaseGizmo.FillImage = bmpGizmoResizeFill;
                releaseGizmo.Action = GizmoAction.MoveRelease;
                releaseGizmo.Rect = new Rectangle(x, y, gizmoSize, gizmoSize);
                list.Add(releaseGizmo);
            }

            // Slide note gizmo
            if (note.IsSlideNote)
            {
                var side = note.SlideNoteTarget > note.Value ? 1 : -1;
                var x = GetPixelForNote(locationAbsIndex + visualDuration) + (Platform.IsMobile ? gizmoSize / 4 : -5 * gizmoSize / 4);
                var y = 0;

                if (Platform.IsMobile)
                {
                    if (Platform.IsMobile && captureOperation == CaptureOperation.DragSlideNoteTargetGizmo)
                        y = mouseLastY - headerAndEffectSizeY - gizmoSize / 4 - (side > 0 ? side * noteSizeY : 0);
                    else
                        y = virtualSizeY - (note.SlideNoteTarget + side) * noteSizeY - scrollY - gizmoSize / 4; 
                }
                else
                {
                    y = virtualSizeY - note.SlideNoteTarget * noteSizeY - scrollY - (gizmoSize - noteSizeY) / 2;
                }

                Gizmo slideGizmo = new Gizmo();
                slideGizmo.Image = bmpGizmoResizeUpDown;
                slideGizmo.FillImage = bmpGizmoResizeFill;
                slideGizmo.Action = GizmoAction.MoveSlide;
                slideGizmo.Rect = new Rectangle(x, y, gizmoSize, gizmoSize);
                list.Add(slideGizmo);
            }

            return list;
        }

        private List<Gizmo> GetEffectGizmos(out Note note, out NoteLocation location)
        {
            note = Platform.IsDesktop ?
                GetNoteForDesktopEffectGizmos(out location) :
                GetHighlightedNoteAndLocation(out location);

            if (!showEffectsPanel || selectedEffectIdx < 0 || note == null || !note.HasValidEffectValue(selectedEffectIdx))
                return null;

            var list = new List<Gizmo>();

            var locationAbsIndex = location.ToAbsoluteNoteIndex(Song);
            var channel  = Song.Channels[editChannel];
            var minValue = Note.GetEffectMinValue(Song, channel, selectedEffectIdx);
            var maxValue = Note.GetEffectMaxValue(Song, channel, selectedEffectIdx);
            var midValue = (minValue + maxValue) / 2;
            var value    = note.GetEffectValue(selectedEffectIdx);

            // Effect values
            if (Platform.IsMobile)
            {
                var effectPosY = effectPanelSizeY - ((minValue == maxValue) ? effectPanelSizeY : (float)Math.Floor((value - minValue) / (float)(maxValue - minValue) * effectPanelSizeY));

                var x = GetPixelForNote(locationAbsIndex + 1) + gizmoSize / 4;
                var y = (int)(effectPosY + (value >= midValue ? gizmoSize / 4 : -gizmoSize * 5 / 4));

                Gizmo effectGizmo = new Gizmo();
                effectGizmo.Image = bmpGizmoResizeUpDown;
                effectGizmo.FillImage = bmpGizmoResizeFill;
                effectGizmo.Action = GizmoAction.ChangeEffectValue;
                effectGizmo.Rect = new Rectangle(x, y, gizmoSize, gizmoSize);
                list.Add(effectGizmo);
            }

            // Volume slide.
            if (selectedEffectIdx == Note.EffectVolume && channel.SupportsEffect(Note.EffectVolumeSlide) && note.HasVolumeSlide)
            {
                var duration = channel.GetVolumeSlideDuration(location);
                var effectPosY = effectPanelSizeY - ((minValue == maxValue) ? effectPanelSizeY : (float)Math.Floor((note.VolumeSlideTarget - minValue) / (float)(maxValue - minValue) * effectPanelSizeY));

                var x = GetPixelForNote(locationAbsIndex + duration) - gizmoSize * 5 / 4;
                var y = (int)(effectPosY + (note.VolumeSlideTarget >= midValue ? gizmoSize / 4 : -gizmoSize * 5 / 4));

                Gizmo slideGizmo = new Gizmo();
                slideGizmo.Image = bmpGizmoResizeUpDown;
                slideGizmo.FillImage = bmpGizmoResizeFill;
                slideGizmo.Action = GizmoAction.MoveVolumeSlideValue;
                slideGizmo.Rect = new Rectangle(x, y, gizmoSize, gizmoSize);
                list.Add(slideGizmo);
            }

            return list;
        }

        private List<Gizmo> GetEnvelopeGizmos()
        {
            if (Platform.IsDesktop)
                return null;

            var env = EditEnvelope;

            if (!HasHighlightedNote() || highlightNoteAbsIndex >= env.Length)
                return null;

            Envelope.GetMinMaxValueForType(editInstrument, editEnvelope, out int min, out int max);
            var midValue = (max + min) / 2;
            var value = env.Values[highlightNoteAbsIndex];

            var x = GetPixelForNote(highlightNoteAbsIndex + 1) + gizmoSize / 4;
            var y = 0;
            
            if (editEnvelope == EnvelopeType.Arpeggio)
                y = (int)(virtualSizeY - envelopeValueSizeY * (value - min)) - scrollY - gizmoSize * 3 / 4;
            else
                y = (int)(virtualSizeY - envelopeValueSizeY * (value - min + (value < 0 ? 0 : 1))) + (value >= midValue ? gizmoSize / 4 : -gizmoSize * 5 / 4) - scrollY;

            Gizmo slideGizmo = new Gizmo();
            slideGizmo.Image = bmpGizmoResizeUpDown;
            slideGizmo.FillImage = bmpGizmoResizeFill;
            slideGizmo.Action = GizmoAction.ChangeEnvValue;
            slideGizmo.Rect = new Rectangle(x, y, gizmoSize, gizmoSize);

            var list = new List<Gizmo>();
            list.Add(slideGizmo);
            return list;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            UpdateCursor();

            if (captureOperation != CaptureOperation.None)
                return;

            bool ctrl  = e.Control;
            bool shift = e.Shift;

            if (e.Key == Keys.Escape)
            {
                ClearSelection();
                MarkDirty();
            }
            else if (e.Key == Keys.A && ctrl && IsActiveControl)
            {
                if (editMode == EditionMode.Arpeggio ||
                    editMode == EditionMode.Enveloppe)
                {
                    SetSelection(0, EditEnvelope.Length - 1);
                }
                else if (editMode == EditionMode.Channel)
                {
                    SetSelection(0, Song.GetPatternStartAbsoluteNoteIndex(Song.Length) - 1);
                }
            }
            else if (e.Key == Keys.S && shift)
            {
                if (SnapAllowed)
                {
                    snap = !snap;
                    MarkDirty();
                }
            }
            else if (IsActiveControl && IsSelectionValid())
            {
                if (ctrl)
                {
                    if (e.Key == Keys.C)
                        Copy();
                    else if (e.Key == Keys.X)
                        Cut();
                    else if (e.Key == Keys.V)
                    {
                        if (shift)
                            PasteSpecial();
                        else
                            Paste();
                    }
                }

                if (e.Key == Keys.Delete)
                {
                    if (editMode == EditionMode.Channel)
                    {
                        if (ctrl && shift)
                            DeleteSpecial();
                        else
                            DeleteSelectedNotes();
                    }
                    else if (editMode == EditionMode.Enveloppe || editMode == EditionMode.Arpeggio)
                    {
                        DeleteSelectedEnvelopeValues();
                    }
                    else if (editMode == EditionMode.DPCM)
                    {
                        DeleteSelectedWaveSection();
                    }
                }

                if (editMode == EditionMode.Channel)
                {
                    switch (e.Key)
                    {
                        case Keys.Up:
                            TransposeNotes(ctrl ? 12 : 1);
                            break;
                        case Keys.Down:
                            TransposeNotes(ctrl ? -12 : -1);
                            break;
                        case Keys.Right:
                            MoveNotes(ctrl ? (Song.Project.UsesFamiTrackerTempo ? Song.BeatLength : Song.NoteLength) : 1);
                            break;
                        case Keys.Left:
                            MoveNotes(ctrl ? -(Song.Project.UsesFamiTrackerTempo ? Song.BeatLength : Song.NoteLength) : -1);
                            break;
                    }
                }
                else if (editMode == EditionMode.Enveloppe || editMode == EditionMode.Arpeggio)
                {
                    switch (e.Key)
                    {
                        case Keys.Up:
                            IncrementEnvelopeValues(ctrl ? 4 : 1);
                            break;
                        case Keys.Down:
                            IncrementEnvelopeValues(ctrl ? -4 : -1);
                            break;
                        case Keys.Right:
                            MoveEnvelopeValues(ctrl ? 4 : 1);
                            break;
                        case Keys.Left:
                            MoveEnvelopeValues(ctrl ? -4 : -1);
                            break;
                    }
                }
            }
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            UpdateCursor();
        }

        public int GetDPCMSampleMappingNoteAtPos(Point pos)
        {
            if (editMode == EditionMode.DPCMMapping && GetNoteValueForCoord(pos.X, pos.Y, out var noteValue))
            {
                return noteValue;
            }

            return Note.NoteInvalid;
        }

        private bool EnsureSeekBarVisible(float percent = ContinuousFollowPercent)
        {
            var seekX = GetPixelForNote(App.CurrentFrame);
            var minX = 0;
            var maxX = (int)((Width * percent) - pianoSizeX);

            // Keep everything visible 
            if (seekX < minX)
                scrollX -= (minX - seekX);
            else if (seekX > maxX)
                scrollX += (seekX - maxX);

            ClampScroll();

            seekX = GetPixelForNote(App.CurrentFrame);
            return seekX == maxX;
        }

        public void DeleteRecording(int frame)
        {
            if (App.IsRecording && editMode == EditionMode.Channel)
            {
                var endFrame = frame;
                var startFrame = SnapNote(frame, false, true);

                if (startFrame == 0)
                    return;

                if (startFrame == endFrame)
                    startFrame = SnapNote(startFrame - 1, false, true);

                var startPatternIdx = Song.PatternIndexFromAbsoluteNoteIndex(startFrame);
                var endPatternIdx   = Song.PatternIndexFromAbsoluteNoteIndex(endFrame);

                var channel = Song.Channels[editChannel];

                if (startPatternIdx == endPatternIdx)
                {
                    if (channel.PatternInstances[startPatternIdx] != null)
                        App.UndoRedoManager.BeginTransaction(TransactionScope.Pattern, channel.PatternInstances[startPatternIdx].Id);
                    else
                        App.UndoRedoManager.BeginTransaction(TransactionScope.Application);
                }
                else
                    App.UndoRedoManager.BeginTransaction(TransactionScope.Channel, Song.Id, editChannel);

                channel.DeleteNotesBetween(startFrame, endFrame);
                App.SeekSong(startFrame);
                EnsureSeekBarVisible();
                App.UndoRedoManager.EndTransaction();

                for (int i = startPatternIdx; i <= endPatternIdx; i++)
                    PatternChanged?.Invoke(channel.PatternInstances[startPatternIdx]);

                MarkDirty();
            }
        }

        public void AdvanceRecording(int frame, bool doTransaction = false)
        {
            if (App.IsRecording && editMode == EditionMode.Channel)
            {
                if (doTransaction)
                    App.UndoRedoManager.BeginTransaction(TransactionScope.Application);

                App.SeekSong(SnapNote(frame, true, true));
                EnsureSeekBarVisible();

                if (doTransaction)
                    App.UndoRedoManager.EndTransaction();

                MarkDirty();
            }
        }

        public void RecordNote(Note note)
        {
            if (App.IsRecording && editMode == EditionMode.Channel && note.IsMusical)
            {
                var currentFrame = SnapNote(App.CurrentFrame, false, true);
                var location = NoteLocation.FromAbsoluteNoteIndex(Song, currentFrame);
                var channel = Song.Channels[editChannel];
                var pattern = channel.PatternInstances[location.PatternIndex];

                // Create a pattern if needed.
                if (pattern == null)
                {
                    App.UndoRedoManager.BeginTransaction(TransactionScope.Channel, Song.Id, editChannel);
                    pattern = channel.CreatePattern();
                    channel.PatternInstances[location.PatternIndex] = pattern;
                }
                else
                {
                    App.UndoRedoManager.BeginTransaction(TransactionScope.Pattern, pattern.Id);
                }

                var nextFrame = SnapNote(currentFrame, true, true);

                var newNote = note.Clone();
                newNote.HasVolume = false;
                newNote.Duration = nextFrame - currentFrame;
                pattern.Notes[location.NoteIndex] = newNote;
                channel.InvalidateCumulativePatternCache(pattern);
                PatternChanged?.Invoke(pattern);

                AdvanceRecording(currentFrame);

                App.UndoRedoManager.EndTransaction();

                MarkDirty();
            }
        }

        public void ToggleEffectPannel()
        {
            if (editMode == EditionMode.Channel || editMode == EditionMode.DPCM)
                SetShowEffectPanel(!showEffectsPanel);
        }

        public void SetShowEffectPanel(bool expanded)
        {
            showEffectsPanel = expanded;
            UpdateRenderCoords();
            ClampScroll();
            MarkDirty();
        }

        public void ToggleMaximize()
        {
            maximized = !maximized;
            MaximizedChanged?.Invoke();
            UpdateRenderCoords();
            ClampScroll();
            MarkDirty();
        }

        private bool HandleMouseDownPan(MouseEventArgs e)
        {
            bool middle = e.Middle || (e.Left && ModifierKeys.Alt);

            if (middle && e.Y > headerSizeY && e.X > pianoSizeX)
            {
                panning = true;
                CaptureMouse(e.X, e.Y);
                return true;
            }

            return false;
        }

        private bool HandleMouseDownScrollbar(MouseEventArgs e)
        {
            if (e.Left && scrollBarThickness > 0 && e.X > pianoSizeX && e.Y > headerAndEffectSizeY)
            {
                if (e.Y >= (Height - scrollBarThickness) && GetScrollBarParams(true, out var scrollBarPosX, out var scrollBarSizeX))
                {
                    var x = e.X - pianoSizeX;
                    if (x < scrollBarPosX)
                    {
                        scrollX -= (Width - pianoSizeX);
                        ClampScroll();
                        MarkDirty();
                    }
                    else if (x > (scrollBarPosX + scrollBarSizeX))
                    {
                        scrollX += (Width - pianoSizeX);
                        ClampScroll();
                        MarkDirty();
                    }
                    else if (x >= scrollBarPosX && x <= (scrollBarPosX + scrollBarSizeX))
                    {
                        StartCaptureOperation(e.X, e.Y, CaptureOperation.ScrollBarX);
                    }
                    return true;
                }
                if (e.X >= (Width - scrollBarThickness) && GetScrollBarParams(false, out var scrollBarPosY, out var scrollBarSizeY))
                {
                    var y = e.Y - headerAndEffectSizeY;
                    if (y < scrollBarPosY)
                    {
                        scrollY -= (Height - headerAndEffectSizeY);
                        ClampScroll();
                        MarkDirty();
                    }
                    else if (y > (scrollBarPosY + scrollBarSizeY))
                    {
                        scrollX += (Height - headerAndEffectSizeY);
                        ClampScroll();
                        MarkDirty();
                    }
                    else if (y >= scrollBarPosY && y <= (scrollBarPosY + scrollBarSizeY))
                    {
                        StartCaptureOperation(e.X, e.Y, CaptureOperation.ScrollBarY);
                    }
                    return true;
                }
            }

            return false;
        }

        private bool HandleMouseDownPiano(MouseEventArgs e)
        {
            if (e.Left && IsPointInPiano(e.X, e.Y))
            {
                StartPlayPiano(e.X, e.Y);
                return true;
            }

            return false;
        }

        private bool HandleMouseDownSeekBar(MouseEventArgs e)
        {
            if (e.Left && IsPointInHeader(e.X, e.Y))
            {
                StartCaptureOperation(e.X, e.Y, CaptureOperation.DragSeekBar);
                UpdateSeekDrag(e.X, e.Y, false);
                return true;
            }

            return false;
        }

        private bool HandleMouseDownHeaderSelection(MouseEventArgs e)
        {
            if (e.Right && IsPointInHeader(e.X, e.Y))
            {
                e.DelayRightClick(); // Need to wait and see if its a context menu click or not.
                return true;
            }

            return false;
        }

        private bool HandleMouseDownEnvelopeSelection(MouseEventArgs e)
        {
            if (e.Right && (IsPointInHeaderTopPart(e.X, e.Y) || IsPointInNoteArea(e.X, e.Y)))
            {
                e.DelayRightClick(); // Need to wait and see if its a context menu click or not.
                return true;
            }

            return false;
        }

        private bool HandleMouseDownEffectList(MouseEventArgs e)
        {
            if (e.Left && IsPointInEffectList(e.X, e.Y))
            {
                int effectIdx = (e.Y - headerSizeY) / effectButtonSizeY;
                if (effectIdx >= 0 && effectIdx < supportedEffects.Length)
                {
                    selectedEffectIdx = supportedEffects[effectIdx];
                    MarkDirty();
                }
                return true;
            }

            return false;
        }

        private bool HandleMouseDownAltZoom(MouseEventArgs e)
        {
            if (e.Right && ModifierKeys.Alt)
            {
                StartCaptureOperation(e.X, e.Y, CaptureOperation.AltZoom);
                return true;
            }

            return false;
        }

        private void StartResizeEnvelope(int x, int y)
        {
            StartCaptureOperation(x, y, CaptureOperation.ResizeEnvelope);

            if (editMode == EditionMode.Enveloppe)
                App.UndoRedoManager.BeginTransaction(TransactionScope.Instrument, editInstrument.Id);
            else
                App.UndoRedoManager.BeginTransaction(TransactionScope.Arpeggio, editArpeggio.Id);

            ResizeEnvelope(x, y);
        }

        private bool HandleMouseDownEnvelopeResize(MouseEventArgs e)
        {
            if (e.Left && IsPointWhereCanResizeEnvelope(e.X, e.Y) && EditEnvelope.CanResize)
            {
                StartResizeEnvelope(e.X, e.Y);
                return true;
            }

            return false;
        }

        private bool HandleMouseDownEnvelopeLoopRelease(MouseEventArgs e)
        {
            bool left  = e.Left;
            bool right = e.Right;

            if (((left && EditEnvelope.CanLoop) || (right && EditEnvelope.CanRelease && EditEnvelope.Loop >= 0)) && IsPointInHeaderBottomPart(e.X, e.Y))
            {
                CaptureOperation op = left ? CaptureOperation.DragLoop : CaptureOperation.DragRelease;
                StartCaptureOperation(e.X, e.Y, op);

                if (editMode == EditionMode.Enveloppe)
                    App.UndoRedoManager.BeginTransaction(TransactionScope.Instrument, editInstrument.Id);
                else
                    App.UndoRedoManager.BeginTransaction(TransactionScope.Arpeggio, editArpeggio.Id);

                ResizeEnvelope(e.X, e.Y);
                return true;
            }

            return false;
        }

        private void StartDrawEnvelope(int x, int y)
        {
            StartCaptureOperation(x, y, CaptureOperation.DrawEnvelope);

            if (editMode == EditionMode.Enveloppe)
                App.UndoRedoManager.BeginTransaction(TransactionScope.Instrument, editInstrument.Id);
            else
                App.UndoRedoManager.BeginTransaction(TransactionScope.Arpeggio, editArpeggio.Id);

            DrawEnvelope(x, y, true);
        }

        private bool HandleMouseDownDrawEnvelope(MouseEventArgs e)
        {
            if (e.Left && IsPointInNoteArea(e.X, e.Y) && EditEnvelope.Length > 0)
            {
                StartDrawEnvelope(e.X, e.Y);
                return true;
            }

            return false;
        }

        private bool HandleMouseDownEffectPanel(MouseEventArgs e)
        {
            bool left  = e.Left;
            bool right = e.Right;

            if (selectedEffectIdx >= 0 && IsPointInEffectPanel(e.X, e.Y) && GetEffectNoteForCoord(e.X, e.Y, out var location))
            {
                if (left)
                {
                    var slide = FamiStudioForm.IsKeyDown(Keys.S);

                    if (slide && selectedEffectIdx == Note.EffectVolume)
                    {
                        StartDragVolumeSlide(e.X, e.Y, location);
                    }
                    else
                    {
                        StartChangeEffectValue(e.X, e.Y, location);
                    }

                    return true;
                }
                else if (right)
                {
                    e.DelayRightClick(); // Wait to see if its a context menu or selection.
                    return true;
                }
            }

            return false;
        }

        private void StartDragWaveVolumeEnvelope(int x, int y, int vertexIdx)
        {
            volumeEnvelopeDragVertex = vertexIdx;
            App.UndoRedoManager.BeginTransaction(TransactionScope.DPCMSample, editSample.Id);
            StartCaptureOperation(x, y, CaptureOperation.DragWaveVolumeEnvelope);
        }

        private bool HandleMouseDownDPCMVolumeEnvelope(MouseEventArgs e)
        {
            if (e.Left && IsPointInEffectPanel(e.X, e.Y))
            {
                var vertexIdx = GetWaveVolumeEnvelopeVertexIndex(e.X, e.Y);
                if (vertexIdx >= 0)
                {
                    StartDragWaveVolumeEnvelope(e.X, e.Y, vertexIdx);
                    return true;
                }
            }

            return false;
        }

        private bool HandleMouseDownSnapButton(MouseEventArgs e)
        {
            if (e.Left && (IsPointOnSnapButton(e.X, e.Y) || IsPointOnSnapResolution(e.X, e.Y)))
            {
                snap = !snap;
                MarkDirty();
                return true;
            }

            return false;
        }

        private bool HandleMouseDownMaximizeButton(MouseEventArgs e)
        {
            if (e.Left && IsPointOnMaximizeButton(e.X, e.Y))
            {
                ToggleMaximize();
                MarkDirty();
                return true;
            }

            return false;
        }

        private bool HandleMouseDownToggleEffectPanelButton(MouseEventArgs e)
        {
            if (e.Left && IsPointOnToggleEffectPannelButton(e.X, e.Y))
            {
                ToggleEffectPannel();
                return true;
            }

            return false;
        }

        private void StartSelectWave(int x, int y)
        {
            StartCaptureOperation(x, y, CaptureOperation.SelectWave);
            if (captureThresholdMet)
                UpdateWaveSelection(x, y);
        }

        private bool HandleMouseDownWaveSelection(MouseEventArgs e)
        {
            bool left  = e.Left;
            bool right = e.Right;

            if ((left || right) && (IsPointInNoteArea(e.X, e.Y) || IsPointInHeader(e.X, e.Y)))
            {
                if (left)
                    StartSelectWave(e.X, e.Y);
                else
                    e.DelayRightClick(); // Need to see if we have a context menu or not.
                return true;
            }

            return false;
        }

        private bool HandleMouseDownChannelNote(MouseEventArgs e)
        {
            bool left  = e.Left;
            bool right = e.Right;

            if (GetLocationForCoord(e.X, e.Y, out var mouseLocation, out byte noteValue))
            {
                if (mouseLocation.PatternIndex >= Song.Length)
                    return true;

                var channel = Song.Channels[editChannel];
                var noteLocation = mouseLocation;
                var note = channel.FindMusicalNoteAtLocation(ref noteLocation, noteValue);

                if (left)
                {
                    var shift   = ModifierKeys.Shift;
                    var stop    = FamiStudioForm.IsKeyDown(Keys.T);
                    var slide   = FamiStudioForm.IsKeyDown(Keys.S);
                    var attack  = FamiStudioForm.IsKeyDown(Keys.A);
                    var eyedrop = FamiStudioForm.IsKeyDown(Keys.I);

                    if (slide)
                    {
                        StartSlideNoteCreation(e.X, e.Y, noteLocation, note, noteValue);
                    }
                    else if (attack && note != null)
                    {
                        ToggleNoteAttack(noteLocation, note);
                    }
                    else if (eyedrop && note != null)
                    {
                        Eyedrop(note);
                    }
                    else if (shift && note != null)
                    {
                        ToggleReleaseNote(noteLocation, mouseLocation, note);
                    }
                    else if (stop)
                    {
                        CreateOrphanStopNote(mouseLocation);
                    }
                    else
                    {
                        if (note != null)
                        {
                            var captureOp = GetHighlightedNoteCaptureOperationForCoord(e.X, e.Y);

                            if (captureOp == CaptureOperation.DragSelection ||
                                captureOp == CaptureOperation.DragNote ||
                                captureOp == CaptureOperation.ResizeNoteStart ||
                                captureOp == CaptureOperation.ResizeSelectionNoteStart)
                            {
                                StartNoteDrag(e.X, e.Y, captureOp, noteLocation, note);
                            }
                            else if (captureOp == CaptureOperation.ResizeNoteEnd ||
                                     captureOp == CaptureOperation.ResizeSelectionNoteEnd)
                            {
                                StartNoteResizeEnd(e.X, e.Y, captureOp, noteLocation);
                            }
                            else if (captureOp == CaptureOperation.MoveNoteRelease)
                            {
                                StartMoveNoteRelease(e.X, e.Y, noteLocation);
                            }
                        }
                        else
                        {
                            StartNoteCreation(e, noteLocation, noteValue);
                        }
                    }
                }
                else if (right && note == null)
                {
                    e.DelayRightClick(); // Need to wait to tell if its a context menu or selection.
                }

                MarkDirty();
                return true;
            }

            return false;
        }

        private bool HandleMouseDownDPCMMapping(MouseEventArgs e)
        {
            bool left = e.Left;

            if (left && GetLocationForCoord(e.X, e.Y, out var location, out var noteValue))
            {
                if (App.Project.NoteSupportsDPCM(noteValue))
                {
                    var mapping = App.Project.GetDPCMMapping(noteValue);

                    if (mapping == null)
                    {
                        MapDPCMSample(noteValue);
                    }
                    else 
                    {
                        StartDragDPCMSampleMapping(e.X, e.Y, noteValue);
                    }
                }
                else
                {
                    App.DisplayNotification("DPCM samples are only allowed between C1 and D6");
                }

                return true;
            }

            return false;
        }

        private bool HandleMouseDownNoteGizmos(MouseEventArgs e)
        {
            return e.Left && HandleNoteGizmos(e.X, e.Y);
        }

        private bool HandleMouseDownEffectGizmos(MouseEventArgs e)
        {
            return e.Left && HandleEffectsGizmos(e.X, e.Y);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            bool left  = e.Left;
            bool right = e.Right;

            if (captureOperation != CaptureOperation.None && (left || right))
                return;

            UpdateCursor();

            // General stuff.
            if (HandleMouseDownPan(e)) goto Handled;
            if (HandleMouseDownScrollbar(e)) goto Handled;
            if (HandleMouseDownPiano(e)) goto Handled;
            if (HandleMouseDownAltZoom(e)) goto Handled;
            if (HandleMouseDownMaximizeButton(e)) goto Handled;

            if (editMode == EditionMode.Channel)
            {
                if (HandleMouseDownSeekBar(e)) goto Handled;
                if (HandleMouseDownHeaderSelection(e)) goto Handled;
                if (HandleMouseDownEffectList(e)) goto Handled;
                if (HandleMouseDownEffectGizmos(e)) goto Handled; // Needs to be above "HandleMouseDownEffectPanel".
                if (HandleMouseDownEffectPanel(e)) goto Handled;
                if (HandleMouseDownSnapButton(e)) goto Handled;
                if (HandleMouseDownNoteGizmos(e)) goto Handled;
                if (HandleMouseDownChannelNote(e)) goto Handled;
            }

            if (editMode == EditionMode.Enveloppe || 
                editMode == EditionMode.Arpeggio)
            {
                if (HandleMouseDownEnvelopeSelection(e)) goto Handled;
                if (HandleMouseDownEnvelopeResize(e)) goto Handled;
                if (HandleMouseDownEnvelopeLoopRelease(e)) goto Handled;
                if (HandleMouseDownDrawEnvelope(e)) goto Handled;
            }

            if (editMode == EditionMode.DPCM)
            {
                if (HandleMouseDownDPCMVolumeEnvelope(e)) goto Handled;
                if (HandleMouseDownWaveSelection(e)) goto Handled;
            }

            if (editMode == EditionMode.Channel ||
                editMode == EditionMode.DPCM)
            {
                if (HandleMouseDownToggleEffectPanelButton(e)) goto Handled;
            }

            if (editMode == EditionMode.DPCMMapping)
            {
                if (HandleMouseDownDPCMMapping(e)) goto Handled;
            }
            return;

        Handled: 
            MarkDirty();
        }

        private bool HandleMouseDownDelayedChannelNotes(MouseEventArgs e)
        {
            bool right = e.Right;

            if (right && GetLocationForCoord(e.X, e.Y, out var mouseLocation, out byte noteValue) && mouseLocation.PatternIndex < Song.Length)
            {
                var channel = Song.Channels[editChannel];
                var noteLocation = mouseLocation;
                var note = channel.FindMusicalNoteAtLocation(ref noteLocation, noteValue);

                if (note == null)
                    StartSelection(e.X, e.Y);

                return true;
            }

            return false;
        }

        private bool HandleMouseDownDelayedHeaderSelection(MouseEventArgs e)
        {
            if (e.Right && IsPointInHeader(e.X, e.Y))
            {
                StartSelection(e.X, e.Y);
                return true;
            }

            return false;
        }

        private bool HandleMouseDownDelayedEffectPanel(MouseEventArgs e)
        {
            if (e.Right && selectedEffectIdx >= 0 && IsPointInEffectPanel(e.X, e.Y) && GetEffectNoteForCoord(e.X, e.Y, out var location))
            {
                var pattern = Song.Channels[editChannel].PatternInstances[location.PatternIndex];

                //if (pattern == null || !pattern.Notes.TryGetValue(location.NoteIndex, out var note) || note == null || !note.HasValidEffectValue(selectedEffectIdx))
                    StartSelection(e.X, e.Y);

                return true;
            }

            return false;
        }

        private bool HandleMouseDownDelayedEnvelopeSelection(MouseEventArgs e)
        {
            if (e.Right && (IsPointInHeaderTopPart(e.X, e.Y) || IsPointInNoteArea(e.X, e.Y)))
            {
                StartSelection(e.X, e.Y);
                return true;
            }

            return false;
        }

        private bool HandleMouseDownDelayedWaveSelection(MouseEventArgs e)
        {
            if (e.Right && (IsPointInNoteArea(e.X, e.Y) || IsPointInHeader(e.X, e.Y)))
            {
                StartSelectWave(e.X, e.Y);
                return true;
            }

            return false;
        }

        protected override void OnMouseDownDelayed(MouseEventArgs e)
        {
            if (editMode == EditionMode.Channel)
            {
                if (HandleMouseDownDelayedChannelNotes(e)) goto Handled;
                if (HandleMouseDownDelayedHeaderSelection(e)) goto Handled;
                if (HandleMouseDownDelayedEffectPanel(e)) goto Handled;
            }

            if (editMode == EditionMode.Enveloppe ||
                editMode == EditionMode.Arpeggio)
            {
                if (HandleMouseDownDelayedEnvelopeSelection(e)) goto Handled;
            }

            if (editMode == EditionMode.DPCM)
            {
                if (HandleMouseDownDelayedWaveSelection(e)) goto Handled;
            }

            return;

        Handled:
            MarkDirty();
        }

        private Note CreateSingleNote(int x, int y)
        {
            GetLocationForCoord(x, y, out var location, out var noteValue, true);

            var channel = Song.Channels[editChannel];
            var pattern = channel.PatternInstances[location.PatternIndex];

            if (!channel.SupportsInstrument(App.SelectedInstrument))
            {
                App.ShowInstrumentError(channel, true);
                return null;
            }

            if (pattern != null)
            {
                App.UndoRedoManager.BeginTransaction(TransactionScope.Pattern, pattern.Id);
            }
            else
            {
                App.UndoRedoManager.BeginTransaction(TransactionScope.Channel, Song.Id, editChannel);
                pattern = channel.CreatePatternAndInstance(location.PatternIndex);
            }

            App.PlayInstrumentNote(noteValue, false, false, false, null, null, 0.5f);

            var abs = location.ToAbsoluteNoteIndex(Song);
            var note = pattern.GetOrCreateNoteAt(location.NoteIndex);
            note.Value = noteValue;
            note.Duration = SnapEnabled ? Math.Max(1, SnapNote(abs, true) - abs) : Song.GetPatternBeatLength(location.PatternIndex);
            note.Instrument = editChannel != ChannelType.Dpcm ? App.SelectedInstrument : null;
            note.Arpeggio = channel.SupportsArpeggios ? App.SelectedArpeggio : null;

            SetMobileHighlightedNote(abs);
            MarkPatternDirty(pattern);
            App.UndoRedoManager.EndTransaction();

            return note;
        }

        private bool HandleTouchDownPan(int x, int y)
        {
            if (IsPointInNoteArea(x, y))
            {
                StartCaptureOperation(x, y, CaptureOperation.MobilePan);
                return true;
            }

            return false;
        }

        private bool HandleTouchDownPiano(int x, int y)
        {
            if (IsPointInPiano(x, y))
            {
                StartPlayPiano(x, y);
                return true;
            }

            return false;
        }

        private bool HandleTouchDownEnvelopeSelection(int x, int y)
        {
            if (IsPointInHeader(x, y) && x < GetPixelForNote(EditEnvelope.Length))
            {
                StartSelection(x, y);
                return true;
            }

            return false;
        }

        private bool HandleTouchDownEnvelopeResize(int x, int y)
        {
            if (IsPointInHeader(x, y) && EditEnvelope.CanResize && x > GetPixelForNote(EditEnvelope.Length))
            {
                StartResizeEnvelope(x, y);
                return true;
            }

            return false;
        }

        private bool HandleNoteGizmos(int x, int y)
        {
            if (IsPointInNoteArea(x, y))
            {
                var gizmos = GetNoteGizmos(out var gizmoNote, out var gizmoNoteLocation);
                if (gizmos != null)
                {
                    var absNoteLocation = gizmoNoteLocation.ToAbsoluteNoteIndex(Song);
                    foreach (var g in gizmos)
                    {
                        if (g.Rect.Contains(x - pianoSizeX, y - headerAndEffectSizeY))
                        {
                            switch (g.Action)
                            {
                                case GizmoAction.ResizeNote:
                                    StartNoteResizeEnd(x, y, IsNoteSelected(absNoteLocation) ? CaptureOperation.ResizeSelectionNoteEnd : CaptureOperation.ResizeNoteEnd, gizmoNoteLocation);
                                    break;
                                case GizmoAction.MoveRelease:
                                    StartMoveNoteRelease(x, y, gizmoNoteLocation);
                                    break;
                                case GizmoAction.MoveSlide:
                                    StartDragSlideNoteGizmo(x, y, gizmoNoteLocation, gizmoNote);
                                    break;
                            }

                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private bool HandleTouchDownNoteGizmos(int x, int y)
        {
            return HandleNoteGizmos(x, y);
        }

        private int GetEffectCoordY(int effect, int value)
        {
            var channel = Song.Channels[editChannel];
            var minValue = Note.GetEffectMinValue(Song, channel, effect);
            var maxValue = Note.GetEffectMaxValue(Song, channel, effect);
            return headerSizeY + effectPanelSizeY - ((minValue == maxValue) ? effectPanelSizeY : (int)Math.Floor((value - minValue) / (float)(maxValue - minValue) * effectPanelSizeY));
        }

        private bool HandleEffectsGizmos(int x, int y)
        {
            if (IsPointInNoteArea(x, y))
            {
                var gizmos = GetEffectGizmos(out var gizmoNote, out var gizmoNoteLocation);
                if (gizmos != null)
                {
                    foreach (var g in gizmos)
                    {
                        if (g.Rect.Contains(x - pianoSizeX, y - headerSizeY))
                        {
                            switch (g.Action)
                            {
                                case GizmoAction.ChangeEffectValue:
                                    StartChangeEffectValue(x, y, gizmoNoteLocation);
                                    break;
                                case GizmoAction.MoveVolumeSlideValue:
                                    StartDragVolumeSlideGizmo(x, y, gizmoNote, gizmoNoteLocation);
                                    break;
                            }

                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private bool HandleTouchDownNoteEffectsGizmos(int x, int y)
        {
            return HandleEffectsGizmos(x, y);
        }

        private bool HandleTouchDownDragNote(int x, int y)
        {
            if (HasHighlightedNote() && IsPointInNoteArea(x, y))
            {
                var mouseNote = GetNoteForCoord(x, y, out _, out _, out var duration);
                var highlightNote = GetHighlightedNote();

                if (highlightNote != null && mouseNote == highlightNote)
                {
                    StartNoteDrag(x, y, IsHighlightedNoteSelected() ? CaptureOperation.DragSelection : CaptureOperation.DragNote, NoteLocation.FromAbsoluteNoteIndex(Song, highlightNoteAbsIndex), highlightNote);
                    return true;
                }
            }

            return false;
        }

        private bool HandleTouchDownDragSeekBar(int x, int y)
        {
            if (IsPointInHeader(x, y))
            {
                var seekX = GetPixelForNote(App.CurrentFrame) + pianoSizeX;

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
                StartSelection(x, y);
                return true;
            }

            return false;
        }

        private void UpdateChangeEnvelopeValue(int x, int y)
        {
            App.UndoRedoManager.RestoreTransaction(false);

            GetEnvelopeValueForCoord(x, y, out _, out var value);
            Envelope.GetMinMaxValueForType(editInstrument, editEnvelope, out int min, out int max);

            var env = EditEnvelope;
            var delta = value - captureEnvelopeValue;

            if (IsEnvelopeValueSelected(highlightNoteAbsIndex))
            {
                for (int i = selectionMin; i <= selectionMax; i++)
                    env.Values[i] = (sbyte)Utils.Clamp(env.Values[i] + delta, min, max);
            }
            else
            {
                env.Values[highlightNoteAbsIndex] = (sbyte)Utils.Clamp(env.Values[highlightNoteAbsIndex] + delta, min, max);
            }

            MarkDirty();
        }

        private void StartChangeEnvelopeValue(int x, int y)
        {
            StartCaptureOperation(x, y, CaptureOperation.ChangeEnvelopeValue);

            if (editMode == EditionMode.Enveloppe)
                App.UndoRedoManager.BeginTransaction(TransactionScope.Instrument, editInstrument.Id);
            else
                App.UndoRedoManager.BeginTransaction(TransactionScope.Arpeggio, editArpeggio.Id);

            UpdateChangeEnvelopeValue(x, y);
        }

        private bool HandleTouchDownEnvelopeGizmos(int x, int y)
        {
            if (HasHighlightedNote() && IsPointInNoteArea(x, y))
            {
                var gizmos = GetEnvelopeGizmos();
                if (gizmos != null)
                {
                    foreach (var g in gizmos)
                    {
                        if (g.Rect.Contains(x - pianoSizeX, y - headerAndEffectSizeY))
                        {
                            switch (g.Action)
                            {
                                case GizmoAction.ChangeEnvValue:
                                    StartChangeEnvelopeValue(x, y);
                                    break;
                            }

                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private bool HandleTouchDownDPCMVolumeEnvelope(int x, int y)
        {
            if (showEffectsPanel && IsPointInEffectPanel(x, y))
            {
                var vertexIdx = GetWaveVolumeEnvelopeVertexIndex(x, y);
                if (vertexIdx >= 0)
                {
                    StartDragWaveVolumeEnvelope(x, y, vertexIdx);
                    return true;
                }
            }

            return false;
        }

        private bool HandleTouchDownWaveSelection(int x, int y)
        {
            if (IsPointInHeader(x, y))
            {
                StartSelectWave(x, y);
                return true;
            }

            return false;
        }

        private bool HandleTouchDownDPCMMapping(int x, int y)
        {
            if (GetLocationForCoord(x, y, out _, out var noteValue) && noteValue == highlightDPCMSample)
            {
                var mapping = App.Project.GetDPCMMapping(noteValue);

                if (mapping != null)
                {
                    StartDragDPCMSampleMapping(x, y, noteValue);
                    return true;
                }
            }

            return false;
        }

        private bool HandleTouchClickEnvelope(int x, int y)
        {
            if ((IsPointInHeader(x, y) || IsPointInNoteArea(x, y)) && x < GetPixelForNote(EditEnvelope.Length))
            {
                var absIdx = Utils.Clamp(GetAbsoluteNoteIndexForPixel(x - pianoSizeX), 0, EditEnvelope.Length - 1);
                highlightNoteAbsIndex = absIdx == highlightNoteAbsIndex ? -1 : absIdx;
                return true;
            }

            return false;
        }

        private bool HandleTouchClickToggleEffectPanelButton(int x, int y)
        {
            if (IsPointInTopLeftCorner(x, y))
            {
                ToggleEffectPannel();
                return true;
            }

            return false;
        }

        private bool HandleTouchClickDPCMMapping(int x, int y)
        {
            if (GetLocationForCoord(x, y, out _, out var noteValue))
            {
                if (App.Project.NoteSupportsDPCM(noteValue))
                {
                    var mapping = App.Project.GetDPCMMapping(noteValue);

                    if (mapping == null)
                    {
                        MapDPCMSample(noteValue);
                        highlightDPCMSample = noteValue;
                    }
                    else
                    {
                        highlightDPCMSample = highlightDPCMSample == noteValue ? -1 : noteValue;
                    }
                }
                else
                {
                    App.DisplayNotification("DPCM samples are only allowed between C1 and D6");
                }

                return true;
            }

            return false;
        }
            
        private bool HandleTouchClickHeaderSeek(int x, int y)
        {
            if (IsPointInHeader(x, y))
            {
                var absNoteIndex = GetAbsoluteNoteIndexForPixel(x - pianoSizeX);
                App.SeekSong(SnapNote(absNoteIndex));
                return true;
            }

            return false;
        }

        private bool HandleTouchClickChannelNote(int x, int y)
        {
            if (GetLocationForCoord(x, y, out var mouseLocation, out byte noteValue))
            {
                if (mouseLocation.PatternIndex >= Song.Length)
                    return true;

                var channel = Song.Channels[editChannel];
                var noteLocation = mouseLocation;
                var note = channel.FindMusicalNoteAtLocation(ref noteLocation, noteValue);

                if (note == null)
                {
                    CreateSingleNote(x, y);
                    lastNoteCreateTime = DateTime.Now;
                }
                else
                {
                    var absIdx = noteLocation.ToAbsoluteNoteIndex(Song);
                    highlightNoteAbsIndex = highlightNoteAbsIndex == absIdx ? -1 : absIdx;
                }

                return true;
            }

            return false;
        }

        private bool HandleTouchDoubleClickChannelNote(int x, int y)
        {
            if (Settings.DoubleClickDelete && GetLocationForCoord(x, y, out var mouseLocation, out byte noteValue))
            {
                if (mouseLocation.PatternIndex >= Song.Length)
                    return true;

                var channel = Song.Channels[editChannel];
                var noteLocation = mouseLocation;
                var note = channel.FindMusicalNoteAtLocation(ref noteLocation, noteValue);

                if (note != null)
                {
                    DeleteSingleNote(noteLocation, mouseLocation, note);
                }

                return true;
            }

            return false;
        }

        private void SetSingleEffectValue(int x, int y, NoteLocation location)
        {
            var channel = Song.Channels[editChannel];
            var pattern = channel.PatternInstances[location.PatternIndex];

            if (pattern == null)
            {
                App.UndoRedoManager.BeginTransaction(TransactionScope.Channel, Song.Id, editChannel);
                pattern = channel.CreatePatternAndInstance(location.PatternIndex);
            }
            else
            {
                App.UndoRedoManager.BeginTransaction(TransactionScope.Pattern, pattern.Id);
            }

            var note = pattern.GetOrCreateNoteAt(location.NoteIndex);

            var minValue = Note.GetEffectMinValue(Song, channel, selectedEffectIdx);
            var maxValue = Note.GetEffectMaxValue(Song, channel, selectedEffectIdx);
            var ratio = Utils.Clamp(1.0f - (y - headerSizeY) / (float)effectPanelSizeY, 0.0f, 1.0f);
            var value = (int)Math.Round(ratio * (maxValue - minValue) + minValue);

            note.SetEffectValue(selectedEffectIdx, value);
            MarkPatternDirty(pattern);

            App.UndoRedoManager.EndTransaction();
        }

        private bool HandleTouchClickEffectPanel(int x, int y)
        {
            if (showEffectsPanel && selectedEffectIdx >= 0 && IsPointInEffectPanel(x, y) && GetEffectNoteForCoord(x, y, out var location))
            {
                var channel = Song.Channels[editChannel];
                var note    = channel.GetNoteAt(location);
                var absIdx  = location.ToAbsoluteNoteIndex(Song);

                if (note != null && note.HasValidEffectValue(selectedEffectIdx))
                {
                    highlightNoteAbsIndex = highlightNoteAbsIndex == absIdx ? -1 : absIdx;
                }
                else
                {
                    absIdx = SnapNote(absIdx);
                    location = NoteLocation.FromAbsoluteNoteIndex(Song, absIdx);
                    SetSingleEffectValue(x, y, location);
                    highlightNoteAbsIndex = absIdx;
                }

                return true;
            }

            return false;
        }

        private void ToggleSlideNote(NoteLocation location, Note note)
        {
            var pattern = Song.Channels[editChannel].PatternInstances[location.PatternIndex];
            var selected = IsNoteSelected(location);

            if (selected && SelectionCoversMultiplePatterns())
                App.UndoRedoManager.BeginTransaction(TransactionScope.Channel, Song.Id, editChannel);
            else
                App.UndoRedoManager.BeginTransaction(TransactionScope.Pattern, pattern.Id);

            note.SlideNoteTarget = note.IsSlideNote ? (byte)0 : (byte)(note.Value + (note.Value < Note.MusicalNoteC7 ? 5 : -5));

            if (selected)
            {
                TransformNotes(selectionMin, selectionMax, false, true, false, (n, idx) =>
                {
                    if (n != null && n.IsMusical)
                    {
                        if (!note.IsSlideNote)
                            n.IsSlideNote = false;
                        else if (!n.IsSlideNote)
                            n.SlideNoteTarget = (byte)(n.Value + (n.Value < Note.MusicalNoteC7 ? 5 : -5));
                    }

                    return n;
                });

                MarkSelectedPatternsDirty();
            }
            else
            {
                MarkPatternDirty(location.PatternIndex);
            }

            App.UndoRedoManager.EndTransaction();
        }

        private void ToggleNoteRelease(NoteLocation location, Note note)
        {
            var pattern = Song.Channels[editChannel].PatternInstances[location.PatternIndex];
            var selected = IsNoteSelected(location);

            if (selected && SelectionCoversMultiplePatterns())
                App.UndoRedoManager.BeginTransaction(TransactionScope.Channel, Song.Id, editChannel);
            else
                App.UndoRedoManager.BeginTransaction(TransactionScope.Pattern, pattern.Id);

            note.Release = note.HasRelease ? 0 : Math.Max(1, note.Duration / 2);

            if (selected)
            {
                TransformNotes(selectionMin, selectionMax, false, true, false, (n, idx) =>
                {
                    if (n != null && n.IsMusical)
                    {
                        if (!note.HasRelease)
                            n.HasRelease = false;
                        else if (!n.HasRelease)
                            n.Release = Math.Max(1, n.Duration / 2);
                    }

                    return n;
                });

                MarkSelectedPatternsDirty();
            }
            else
            {
                MarkPatternDirty(location.PatternIndex);
            }

            App.UndoRedoManager.EndTransaction();
        }

        private void ToggleVolumeSlide(NoteLocation location, Note note)
        {
            var pattern = Song.Channels[editChannel].PatternInstances[location.PatternIndex];
            var selected = IsNoteSelected(location);

            App.UndoRedoManager.BeginTransaction(TransactionScope.Pattern, pattern.Id);

            if (note.HasVolumeSlide)
                note.HasVolumeSlide = false;
            else
                note.VolumeSlideTarget = (byte)Utils.Clamp(note.Volume + note.Value >= 8 ? -5 : 5, 0, Note.VolumeMax);

            MarkPatternDirty(location.PatternIndex);

            App.UndoRedoManager.EndTransaction();
        }

        private void ConvertToStopNote(NoteLocation location, Note note)
        {
            var pattern = Song.Channels[editChannel].PatternInstances[location.PatternIndex];
            App.UndoRedoManager.BeginTransaction(TransactionScope.Pattern, pattern.Id);
            note.IsStop = true;
            MarkPatternDirty(pattern);
            App.UndoRedoManager.EndTransaction();
        }

        private bool HandleContextMenuChannelNote(int x, int y)
        {
            if (GetLocationForCoord(x, y, out var mouseLocation, out byte noteValue))
            {
                if (!mouseLocation.IsInSong(Song))
                    return true;

                var channel = Song.Channels[editChannel];
                var noteLocation = mouseLocation;
                var note = channel.FindMusicalNoteAtLocation(ref noteLocation, noteValue);

                SetMobileHighlightedNote(noteLocation.ToAbsoluteNoteIndex(Song));

                var selection = IsHighlightedNoteSelected();
                var menu = new List<ContextMenuOption>();

                if (note != null)
                {
                    if (note.IsMusical)
                    {
                        if (channel.SupportsNoAttackNotes)
                            menu.Add(new ContextMenuOption("MenuToggleAttack", $"Toggle {(selection ? "Selection" : "")} Note Attack", () => { ToggleNoteAttack(noteLocation, note); }));
                        if (channel.SupportsSlideNotes)
                            menu.Add(new ContextMenuOption("MenuToggleSlide", $"Toggle {(selection ? "Selection" : "")} Slide Note", () => { ToggleSlideNote(noteLocation, note); }));
                        if (channel.SupportsReleaseNotes)
                            menu.Add(new ContextMenuOption("MenuToggleRelease", $"Toggle {(selection ? "Selection" : "")} Release", () => { ToggleNoteRelease(noteLocation, note); }));
                        if (channel.Type != ChannelType.Dpcm)
                            menu.Add(new ContextMenuOption("MenuEyedropper", $"Make Instrument Current", () => { Eyedrop(note); }));
                        if (channel.SupportsStopNotes)
                            menu.Add(new ContextMenuOption("MenuStopNote", $"Make Stop Note", () => { ConvertToStopNote(noteLocation, note); }));
                    }

                    menu.Add(new ContextMenuOption("MenuSelectNote", "Select Note Range", () => { SelectSingleNote(noteLocation, mouseLocation, note); }));
                    menu.Add(new ContextMenuOption("MenuDelete", "Delete Note", () => { DeleteSingleNote(noteLocation, mouseLocation, note); }));
                }
                else
                {
                    note = channel.FindMusicalNoteAtLocation(ref noteLocation, -1);

                    if (note != null)
                        menu.Add(new ContextMenuOption("MenuSelectNote", "Select Note Range", () => { SelectSingleNote(noteLocation, mouseLocation, note); }));
                }

                if (IsNoteSelected(mouseLocation))
                {
                    menu.Add(new ContextMenuOption("MenuDeleteSelection", "Delete Selected Notes", () => { DeleteSelectedNotes(); }));
                }

                if (IsSelectionValid())
                {
                    menu.Add(new ContextMenuOption("MenuClearSelection", "Clear Selection", () => { ClearSelection(); ClearHighlightedNote(); }));
                }

                if (menu.Count > 0)
                    App.ShowContextMenu(left + x, top + y, menu.ToArray());

                return true;
            }

            return false;
        }

        private bool HandleTouchLongPressChannelNote(int x, int y)
        {
            return HandleContextMenuChannelNote(x, y);
        }

        private bool HandleContextMenuChannelHeader(int x, int y)
        {
            if (IsPointInHeader(x, y))
            {
                GetLocationForCoord(x, y, out var location, out _);

                if (location.IsInSong(Song))
                {
                    App.ShowContextMenu(left + x, top + y, new[]
                    {
                        new ContextMenuOption("MenuSelectPattern", "Select Pattern", () => { SelectPattern(location.PatternIndex); }),
                        new ContextMenuOption("MenuSelectAll", "Select All", () => { SelectAll(); }),
                    });
                }
            }

            return false;
        }

        private bool HandleTouchLongPressChannelHeader(int x, int y)
        {
            return HandleContextMenuChannelHeader(x, y);
        }

        private bool HandleContextMenuEffectPanel(int x, int y)
        {
            if (showEffectsPanel && selectedEffectIdx >= 0 && IsPointInEffectPanel(x, y) && GetEffectNoteForCoord(x, y, out var location))
            {
                var channel = Song.Channels[editChannel];
                var note = channel.GetNoteAt(location);
                var absIdx = location.ToAbsoluteNoteIndex(Song);
                var hasValue = note != null && note.HasValidEffectValue(selectedEffectIdx);

                SetMobileHighlightedNote(absIdx);

                var menu = new List<ContextMenuOption>();

                if (hasValue || IsNoteSelected(location))
                {
                    menu.Add(new ContextMenuOption("MenuDelete", "Clear Effect Value", () => { ClearEffectValue(location, note, IsNoteSelected(location)); }));
                }

                if (hasValue && selectedEffectIdx == Note.EffectVolume && channel.SupportsEffect(Note.EffectVolumeSlide))
                {
                    menu.Add(new ContextMenuOption("MenuToggleSlide", "Toggle Volume Slide", () => { ToggleVolumeSlide(location, note); }));
                }

                if (IsNoteSelected(location))
                {
                    menu.Add(new ContextMenuOption("MenuDeleteSelection", "Delete Selected Notes", () => { DeleteSelectedNotes(); }));
                }

                if (IsSelectionValid())
                {
                    menu.Add(new ContextMenuOption("MenuClearSelection", "Clear Selection", () => { ClearSelection(); ClearHighlightedNote(); }));
                }

                if (menu.Count > 0)
                    App.ShowContextMenu(left + x, top + y, menu.ToArray());

                return true;
            }

            return false;
        }

        private bool HandleTouchLongPressEffectPanel(int x, int y)
        {
            return HandleContextMenuEffectPanel(x, y);
        }

        private bool HandleTouchLongPressDrawEnvelope(int x, int y)
        {
            if (IsPointInNoteArea(x, y) && EditEnvelope.Length > 0)
            {
                Platform.VibrateClick();
                Platform.ShowToast("Keep holding and move your finger to draw");
                StartDrawEnvelope(x, y);
                return true;
            }

            return false;
        }

        private void SetEnvelopeLoopRelease(int x, int y, bool release)
        {
            int idx = GetAbsoluteNoteIndexForPixel(x - pianoSizeX);
            var env = EditEnvelope;

            if (editMode == EditionMode.Enveloppe)
                App.UndoRedoManager.BeginTransaction(TransactionScope.Instrument, editInstrument.Id);
            else
                App.UndoRedoManager.BeginTransaction(TransactionScope.Arpeggio, editArpeggio.Id);

            if (release)
            {
                Debug.Assert(idx > 0);
                if (env.Loop < 0 || env.Loop >= idx)
                    env.Loop = idx - 1;
                env.Release = idx;
            }
            else
            {
                if (env.Release > 0)
                    env.Release = idx + 1;
                env.Loop = idx;
            }

            App.UndoRedoManager.EndTransaction();
        }

        private void ClearEnvelopeLoopRelease(bool release)
        {
            if (editMode == EditionMode.Enveloppe)
                App.UndoRedoManager.BeginTransaction(TransactionScope.Instrument, editInstrument.Id);
            else
                App.UndoRedoManager.BeginTransaction(TransactionScope.Arpeggio, editArpeggio.Id);

            var env = EditEnvelope;

            if (release)
                env.Release = -1;
            else
                env.Loop = -1;

            App.UndoRedoManager.EndTransaction();
        }

        private bool HandleContextMenuEnvelope(int x, int y)
        {
            if (Platform.IsMobile && IsPointInHeader(x, y) ||
                Platform.IsDesktop && (IsPointInHeaderTopPart(x, y) || IsPointInNoteArea(x,y)))
            {
                var env = EditEnvelope;
                var lastPixel = GetPixelForNote(env.Length);
                var menu = new List<ContextMenuOption>();
                var absIdx = Utils.Clamp(GetAbsoluteNoteIndexForPixel(x - pianoSizeX), 0, EditEnvelope.Length - 1);

                if (editMode == EditionMode.Enveloppe && x < lastPixel)
                {
                    if (env.CanLoop)
                    {
                        menu.Add(new ContextMenuOption("MenuLoopPoint", "Set Loop Point", () => { SetEnvelopeLoopRelease(x, y, false); }));
                        if (env.Loop >= 0)
                            menu.Add(new ContextMenuOption("MenuClearLoopPoint", "Clear Loop Point", () => { ClearEnvelopeLoopRelease(false); }));
                    }
                    if (env.CanRelease)
                    {
                        if (absIdx > 0)
                            menu.Add(new ContextMenuOption("MenuRelease", "Set Release Point", () => { SetEnvelopeLoopRelease(x, y, true); }));
                        if (env.Release >= 0)
                            menu.Add(new ContextMenuOption("MenuClearRelease", "Clear Release Point", () => { ClearEnvelopeLoopRelease(true); }));
                    }
                }

                if (IsSelectionValid())
                {
                    menu.Add(new ContextMenuOption("MenuClearSelection", "Clear Selection", () => { ClearSelection(); ClearHighlightedNote(); }));

                    if (GetEnvelopeValueForCoord(x, y, out int idx, out _) && idx < EditEnvelope.Length)
                        menu.Add(new ContextMenuOption("MenuClearEnvelope", "Flatten Selection", () => { FlattenEnvelopeValues(idx); }));
                }

                if (menu.Count > 0)
                    App.ShowContextMenu(left + x, top + y, menu.ToArray());

                return true;
            }

            return false;
        }

        private bool HandleTouchLongPressEnvelopeHeader(int x, int y)
        {
            return HandleContextMenuEnvelope(x, y);
        }

        private void ResetVolumeEnvelope()
        {
            App.UndoRedoManager.BeginTransaction(TransactionScope.DPCMSample, editSample.Id);
            editSample.ResetVolumeEnvelope();
            App.UndoRedoManager.EndTransaction();
            MarkDirty();
        }

        private void ResetDPCMVolumeEnvelopeVertex(int idx)
        {
            App.UndoRedoManager.BeginTransaction(TransactionScope.DPCMSample, editSample.Id);
            editSample.VolumeEnvelope[idx].volume = 1.0f;
            editSample.Process();
            App.UndoRedoManager.EndTransaction();
            MarkDirty();
        }

        private bool HandleContextMenuWave(int x, int y)
        {
            var menu = new List<ContextMenuOption>();

            if (IsPointInEffectPanel(x, y))
            {
                var vertexIdx = GetWaveVolumeEnvelopeVertexIndex(x, y);
                if (vertexIdx >= 0)
                    menu.Add(new ContextMenuOption("MenuClearEnvelope", "Reset Vertex", () => { ResetDPCMVolumeEnvelopeVertex(vertexIdx); }));
                menu.Add(new ContextMenuOption("MenuClearEnvelope", "Reset Volume Envelope", () => { ResetVolumeEnvelope(); }));
            }

            if (IsSelectionValid())
            {
                menu.Add(new ContextMenuOption("MenuClearSelection", "Clear Selection", () => { ClearSelection(); ClearHighlightedNote(); }, menu.Count > 0));
                menu.Add(new ContextMenuOption("MenuDeleteSelection", "Delete Selected Samples", () => { DeleteSelectedWaveSection(); }));
            }

            if (menu.Count > 0)
                App.ShowContextMenu(left + x, top + y, menu.ToArray());

            return true;
        }

        private bool HandleTouchLongPressWave(int x, int y)
        {
            return HandleContextMenuWave(x, y);
        }

        private bool HandleContextMenuDPCMMapping(int x, int y)
        {
            if (GetLocationForCoord(x, y, out _, out var noteValue))
            {
                var mapping = App.Project.GetDPCMMapping(noteValue);

                if (mapping != null)
                {
                    if (Platform.IsMobile)
                        highlightDPCMSample = noteValue;

                    App.ShowContextMenu(left + x, top + y, new[]
                    {
                        new ContextMenuOption("MenuDelete", "Remove DPCM Sample", () => { ClearDPCMSampleMapping(noteValue); }),
                        new ContextMenuOption("MenuProperties", "DPCM Sample Properties...", () => { EditDPCMSampleMappingProperties(new Point(x, y), mapping); }),
                    });

                    return true;
                }
            }

            return true;
        }

        private bool HandleTouchLongPressDPCMMapping(int x, int y)
        {
            return HandleContextMenuDPCMMapping(x, y);
        }

        protected override void OnTouchDown(int x, int y)
        {
            SetFlingVelocity(0, 0);
            SetMouseLastPos(x, y);
            
            if (editMode == EditionMode.Channel)
            {
                if (HandleTouchDownDragSeekBar(x, y)) goto Handled;
                if (HandleTouchDownHeaderSelection(x, y)) goto Handled;
                if (HandleTouchDownNoteGizmos(x, y)) goto Handled;
                if (HandleTouchDownNoteEffectsGizmos(x, y)) goto Handled;
                if (HandleTouchDownDragNote(x, y)) goto Handled;
            }

            if (editMode == EditionMode.Enveloppe ||
                editMode == EditionMode.Arpeggio)
            {
                if (HandleTouchDownEnvelopeSelection(x, y)) goto Handled;
                if (HandleTouchDownEnvelopeResize(x, y)) goto Handled;
                if (HandleTouchDownEnvelopeGizmos(x, y)) goto Handled;
            }

            if (editMode == EditionMode.DPCM)
            {
                if (HandleTouchDownDPCMVolumeEnvelope(x, y)) goto Handled;
                if (HandleTouchDownWaveSelection(x, y)) goto Handled;
            }

            if (editMode == EditionMode.DPCMMapping)
            {
                if (HandleTouchDownDPCMMapping(x, y)) goto Handled;
            }

            if (pianoVisible)
            {
                if (HandleTouchDownPiano(x, y)) goto Handled;
            }

            if (HandleTouchDownPan(x, y)) goto Handled;

            return;

        Handled:
            MarkDirty();
        }

        protected override void OnTouchMove(int x, int y)
        {
            UpdateCaptureOperation(x, y);
            SetMouseLastPos(x, y);
        }

        protected override void OnTouchUp(int x, int y)
        {
            EndCaptureOperation(x, y);
            SetMouseLastPos(x, y);
        }

        protected override void OnTouchFling(int x, int y, float velX, float velY)
        {
            if (canFling)
            {
                EndCaptureOperation(x, y);
                SetFlingVelocity(velX, velY);
            }
        }

        protected override void OnTouchScaleBegin(int x, int y)
        {
            if (captureOperation != CaptureOperation.None)
            {
                Debug.Assert(captureOperation != CaptureOperation.MobileZoomVertical && captureOperation != CaptureOperation.MobileZoom);
                AbortCaptureOperation();
            }

            StartCaptureOperation(x, y, IsPointInPiano(x, y) ? CaptureOperation.MobileZoomVertical : CaptureOperation.MobileZoom);
            SetMouseLastPos(x, y);
        }

        protected override void OnTouchScale(int x, int y, float scale)
        {
            UpdateCaptureOperation(x, y, scale);
            SetMouseLastPos(x, y);
        }

        protected override void OnTouchScaleEnd(int x, int y)
        {
            EndCaptureOperation(x, y);
            SetMouseLastPos(x, y);
        }

        protected override void OnTouchClick(int x, int y)
        {
            SetMouseLastPos(x, y);

            if (editMode == EditionMode.Channel)
            {
                if (HandleTouchClickHeaderSeek(x, y))  goto Handled;
                if (HandleTouchClickChannelNote(x, y)) goto Handled;
                if (HandleTouchClickEffectPanel(x, y)) goto Handled;
            }

            if (editMode == EditionMode.Enveloppe ||
                editMode == EditionMode.Arpeggio)
            {
                if (HandleTouchClickEnvelope(x, y)) goto Handled;
            }

            if (editMode == EditionMode.Channel ||
                editMode == EditionMode.DPCM)
            {
                if (HandleTouchClickToggleEffectPanelButton(x, y)) goto Handled;
            }

            if (editMode == EditionMode.DPCMMapping)
            {
                if (HandleTouchClickDPCMMapping(x, y)) goto Handled;
            }

            return;

        Handled:
            MarkDirty();
        }

        protected override void OnTouchDoubleClick(int x, int y)
        {
            SetMouseLastPos(x, y);

            // Ignore double tap if we handled a single tap recently.
            if (captureOperation != CaptureOperation.None || (DateTime.Now - lastNoteCreateTime).TotalMilliseconds < 500)
            {
                return;
            }

            if (editMode == EditionMode.Channel)
            {
                if (HandleTouchDoubleClickChannelNote(x, y)) goto Handled;
            }

            return;

        Handled:
            MarkDirty();
        }

        protected override void OnTouchLongPress(int x, int y)
        {
            if (captureOperation == CaptureOperation.ChangeEnvelopeValue)
                return;

            AbortCaptureOperation();

            if (editMode == EditionMode.Channel)
            {
                if (HandleTouchLongPressChannelNote(x, y)) goto Handled;
                if (HandleTouchLongPressEffectPanel(x, y)) goto Handled;
                if (HandleTouchLongPressChannelHeader(x, y)) goto Handled;
            }

            if (editMode == EditionMode.Enveloppe ||
                editMode == EditionMode.Arpeggio)
            {
                if (HandleTouchLongPressDrawEnvelope(x, y)) goto Handled;
                if (HandleTouchLongPressEnvelopeHeader(x, y)) goto Handled;
            }

            if (editMode == EditionMode.DPCM)
            {
                if (HandleTouchLongPressWave(x, y)) goto Handled;
            }

            if (editMode == EditionMode.DPCMMapping)
            {
                if (HandleTouchLongPressDPCMMapping(x, y)) goto Handled;
            }

            return;

        Handled:
            MarkDirty();
        }

        public void LayoutChanged()
        {
            UpdateRenderCoords();
            ClampScroll();
            MarkDirty();
        }

        protected override void OnResize(EventArgs e)
        {
            UpdateRenderCoords();
            ClampScroll();

            if (Platform.IsMobile && (editMode == EditionMode.Arpeggio || editMode == EditionMode.Enveloppe))
                CenterEnvelopeScroll();
        }

        private void GetMinMaxScroll(out int minScrollX, out int minScrollY, out int maxScrollX, out int maxScrollY)
        {
            minScrollX = 0;
            minScrollY = 0;
            maxScrollX = 0;
            maxScrollY = Math.Max(virtualSizeY + headerAndEffectSizeY - Height, 0);

            if (editMode == EditionMode.Channel ||
                editMode == EditionMode.VideoRecording)
            {
                maxScrollX = Math.Max(GetPixelForNote(Song.GetPatternStartAbsoluteNoteIndex(Song.Length), false) - scrollMargin, 0);
            }
            else if (editMode == EditionMode.Enveloppe ||
                     editMode == EditionMode.Arpeggio)
            {
                maxScrollX = Math.Max(GetPixelForNote(EditEnvelope.Length, false) - scrollMargin, 0);
            }
            else if (editMode == EditionMode.DPCM)
            {
                maxScrollX = Math.Max((int)Math.Ceiling(GetPixelForWaveTime(Math.Max(editSample.SourceDuration, editSample.ProcessedDuration))) - scrollMargin, 0);
                minScrollY = Math.Max(virtualSizeY + headerAndEffectSizeY - Height, 0) / 2;
                maxScrollY = minScrollY;
            }
        }

        private bool ClampScroll()
        {
            var scrolledX = true;
            var scrolledY = true;

            if (Song != null)
            {
                GetMinMaxScroll(out var minScrollX, out var minScrollY, out var maxScrollX, out var maxScrollY);

                if (scrollX < minScrollX) { scrollX = minScrollX; scrolledX = false; }
                if (scrollX > maxScrollX) { scrollX = maxScrollX; scrolledY = false; }
                if (scrollY < minScrollY) { scrollY = minScrollY; scrolledY = false; }
                if (scrollY > maxScrollY) { scrollY = maxScrollY; scrolledY = false; }
            }

            ScrollChanged?.Invoke();

            return scrolledX || scrolledY;
        }

        private bool DoScroll(int deltaX, int deltaY)
        {
            scrollX -= deltaX;
            scrollY -= deltaY;
            MarkDirty();
            return ClampScroll();
        }

        private void SetSelection(int min, int max)
        {
            int rangeMax;

            if (editMode == EditionMode.Channel ||
                editMode == EditionMode.Enveloppe ||
                editMode == EditionMode.Arpeggio)
            {
                rangeMax = editMode == EditionMode.Channel ? Song.GetPatternStartAbsoluteNoteIndex(Song.Length) - 1 : EditEnvelope.Length - 1;
            }
            else if (editMode == EditionMode.DPCM)
            {
                rangeMax = editSample.SourceData.NumSamples - 1;

                // DMC file can only select in groups of 8 samples (1 byte)
                if (!editSample.SourceDataIsWav)
                {
                    min = Utils.RoundDown(min, 8);
                    max = Utils.RoundUp  (max, 8);
                    max += (max == min) ? 7 : -1;
                }
            }
            else
            {
                return;
            }

            if (min > rangeMax)
            {
                ClearSelection();
            }
            else
            {
                selectionMin = Utils.Clamp(min, 0, rangeMax);
                selectionMax = Utils.Clamp(max, min, rangeMax);
            }
        }

        private void ClearSelection()
        {
            selectionMin = -1;
            selectionMax = -1;
        }

        private void SetMobileHighlightedNote(int absNoteIndex)
        {
            if (Platform.IsMobile)
                highlightNoteAbsIndex = absNoteIndex;
        }

        private void ClearHighlightedNote()
        {
            highlightNoteAbsIndex = -1;
            highlightDPCMSample = -1;
        }

        private bool HasHighlightedNote()
        {
            return Platform.IsMobile && highlightNoteAbsIndex >= 0;
        }

        private bool IsHighlightedNoteSelected()
        {
            return HasHighlightedNote() && IsNoteSelected(highlightNoteAbsIndex);
        }

        private Note GetHighlightedNote()
        {
            return HasHighlightedNote() ? Song.Channels[editChannel].GetNoteAt(NoteLocation.FromAbsoluteNoteIndex(Song, highlightNoteAbsIndex)) : null; 
        }

        private Note GetHighlightedNoteAndLocation(out NoteLocation location)
        {
            location = NoteLocation.FromAbsoluteNoteIndex(Song, highlightNoteAbsIndex);
            return HasHighlightedNote() ? Song.Channels[editChannel].GetNoteAt(location) : null;
        }

        private void ScrollIfNearEdge(int x, int y, bool scrollHorizontal = true, bool scrollVertical = false)
        {
            if (scrollHorizontal)
            {
                int posMinX = 0;
                int posMaxX = Platform.IsDesktop ? Width + pianoSizeX : (IsLandscape ? Width + headerSizeY : Width);
                int marginMinX = pianoSizeX;
                int marginMaxX = Platform.IsDesktop ? pianoSizeX : headerSizeY;

                scrollX += Utils.ComputeScrollAmount(x, posMinX, marginMinX, App.AverageTickRate * ScrollSpeedFactor, true);
                scrollX += Utils.ComputeScrollAmount(x, posMaxX, marginMaxX, App.AverageTickRate * ScrollSpeedFactor, false);
                ClampScroll();
            }

            if (scrollVertical)
            {
                int posMinY = 0;
                int posMaxY = Platform.IsMobile && !IsLandscape ? Height + headerSizeY : Height;
                int marginMinY = headerSizeY;
                int marginMaxY = headerSizeY;

                scrollY += Utils.ComputeScrollAmount(y, posMinY, marginMinY, App.AverageTickRate * ScrollSpeedFactor, true);
                scrollY += Utils.ComputeScrollAmount(y, posMaxY, marginMaxY, App.AverageTickRate * ScrollSpeedFactor, false);
                ClampScroll();
            }
        }

        private void MarkPatternDirty(int patternIdx)
        {
            var channel = Song.Channels[editChannel];
            var pattern = channel.PatternInstances[patternIdx];
            channel.InvalidateCumulativePatternCache(pattern);
            PatternChanged?.Invoke(pattern);
        }

        private void MarkPatternDirty(Pattern pattern)
        {
            pattern.InvalidateCumulativeCache();
            PatternChanged?.Invoke(pattern);
        }

        private void MarkSelectedPatternsDirty()
        {
            if (IsSelectionValid())
            {
                var channel = Song.Channels[editChannel];
                var patternMin = Song.PatternIndexFromAbsoluteNoteIndex(selectionMin);
                var patternMax = Song.PatternIndexFromAbsoluteNoteIndex(selectionMax);

                channel.InvalidateCumulativePatternCache(patternMin, patternMax);

                for (int i = patternMin; i <= patternMax; i++)
                {
                    var pattern = channel.PatternInstances[i];
                    if (pattern != null)
                        PatternChanged?.Invoke(pattern);
                }
            }
        }

        private void StartPlayPiano(int x, int y)
        {
            StartCaptureOperation(x, y, CaptureOperation.PlayPiano);
            PlayPiano(x, y);
        }

        private void EndPlayPiano()
        {
            App.StopOrReleaseIntrumentNote(false);
            playingNote = -1;
        }

        private void StartSelection(int x, int y)
        {
            StartCaptureOperation(x, y, CaptureOperation.Select, false);
            if (captureThresholdMet)
                UpdateSelection(x, y);
        }

        private void UpdateSelection(int x, int y)
        {
            ScrollIfNearEdge(x, y);

            int noteIdx = GetAbsoluteNoteIndexForPixel(x - pianoSizeX);

            int minSelectionIdx = Math.Min(noteIdx, captureMouseAbsoluteIdx);
            int maxSelectionIdx = Math.Max(noteIdx, captureMouseAbsoluteIdx);
            int pad = SnapEnabled ? -1 : 0;

            SetSelection(SnapNote(minSelectionIdx), SnapNote(maxSelectionIdx, true) + pad);
            MarkDirty();
        }

        private void UpdateWaveSelection(int x, int y)
        {
            ScrollIfNearEdge(x, y);

            float time = Math.Max(0.0f, GetWaveTimeForPixel(x - pianoSizeX));

            float minSelectionTime = Math.Min(time, captureWaveTime);
            float maxSelectionTime = Math.Max(time, captureWaveTime);

            int minSample = (int)Math.Round(minSelectionTime * editSample.SourceSampleRate);
            int maxSample = (int)Math.Round(maxSelectionTime * editSample.SourceSampleRate);

            // The first sample show in the DMC is the initial DMC counter, it doesnt really exist.
            if (!editSample.SourceDataIsWav)
            {
                minSample--;
                maxSample--;
            }

            SetSelection(minSample, maxSample);
            MarkDirty();
        }

        private void UpdateSeekDrag(int x, int y, bool final)
        {
            dragSeekPosition = GetAbsoluteNoteIndexForPixel(x - pianoSizeX);
            dragSeekPosition = SnapNote(dragSeekPosition);

            if (final)
                App.SeekSong(dragSeekPosition);

            MarkDirty();
        }

        private void UpdateVolumeEnvelopeDrag(int x, int y, bool final)
        {
            var halfHeight    = effectPanelSizeY * 0.5f;
            var halfHeightPad = halfHeight - waveDisplayPaddingY;

            var time   = Utils.Clamp((int)Math.Round(GetWaveTimeForPixel(x - pianoSizeX) * editSample.SourceSampleRate), 0, editSample.SourceNumSamples - 1);
            var volume = Utils.Clamp(((y - headerSizeY) - halfHeight) / -halfHeightPad + 1.0f, 0.0f, 2.0f);

            // Cant move 1st and last vertex.
            if (volumeEnvelopeDragVertex != 0 &&
                volumeEnvelopeDragVertex != editSample.VolumeEnvelope.Length - 1)
            {
                editSample.VolumeEnvelope[volumeEnvelopeDragVertex].sample = time;
            }

            editSample.VolumeEnvelope[volumeEnvelopeDragVertex].volume = volume;
            editSample.SortVolumeEnvelope(ref volumeEnvelopeDragVertex);
            editSample.Process();

            if (final)
                App.UndoRedoManager.EndTransaction();

            MarkDirty();
        }

        private void StartSlideNoteCreation(int x, int y, NoteLocation location, Note note, byte noteValue)
        {
            var channel = Song.Channels[editChannel];
            var pattern = channel.PatternInstances[location.PatternIndex];

            if (channel.SupportsSlideNotes)
            {
                if (note != null)
                {
                    App.UndoRedoManager.BeginTransaction(TransactionScope.Pattern, pattern.Id);
                    StartCaptureOperation(x, y, CaptureOperation.DragSlideNoteTarget, false, location.ToAbsoluteNoteIndex(Song));
                }
                else
                {
                    if (channel.SupportsInstrument(App.SelectedInstrument))
                    {
                        if (pattern != null)
                        {
                            App.UndoRedoManager.BeginTransaction(TransactionScope.Pattern, pattern.Id);
                        }
                        else
                        {
                            App.UndoRedoManager.BeginTransaction(TransactionScope.Channel, Song.Id, editChannel);
                            pattern = channel.CreatePatternAndInstance(location.PatternIndex);
                        }

                        StartCaptureOperation(x, y, CaptureOperation.CreateSlideNote, true);

                        note = pattern.GetOrCreateNoteAt(captureNoteLocation.NoteIndex);
                        note.Value = noteValue;
                        note.Duration = (ushort)Song.BeatLength;
                        note.Instrument = editChannel != ChannelType.Dpcm ? App.SelectedInstrument : null;
                        note.Arpeggio = channel.SupportsArpeggios ? App.SelectedArpeggio : null;
                    }
                    else
                    {
                        App.ShowInstrumentError(channel, true);
                        return;
                    }
                }
            }
        }

        private void UpdateSlideNoteCreation(int x, int y, bool final, bool gizmo = false)
        {
            Debug.Assert(captureNoteAbsoluteIdx >= 0);

            ScrollIfNearEdge(x, y, false, true);

            var location = NoteLocation.FromAbsoluteNoteIndex(Song, captureNoteAbsoluteIdx);
            var channel = Song.Channels[editChannel];
            var pattern = channel.PatternInstances[location.PatternIndex];

            if (GetNoteValueForCoord(x, y, out var noteValue))
            {
                var note = pattern.GetOrCreateNoteAt(location.NoteIndex);

                if (noteValue == note.Value)
                    note.SlideNoteTarget = 0;
                else
                    note.SlideNoteTarget = noteValue;

                MarkDirty();
            }

            if (final)
            {
                if (captureOperation == CaptureOperation.CreateSlideNote && !captureThresholdMet)
                    channel.PatternInstances[location.PatternIndex].GetOrCreateNoteAt(location.NoteIndex).IsSlideNote ^= true;
                MarkPatternDirty(location.PatternIndex);
                App.UndoRedoManager.EndTransaction();
            }
        }

        private void StartDragSlideNoteGizmo(int x, int y, NoteLocation location, Note note)
        {
            var channel = Song.Channels[editChannel];
            var pattern = channel.PatternInstances[location.PatternIndex];

            if (note != null && channel.SupportsSlideNotes)
            {
                // -0.5 since out note values have +1 in them (-1 + 0.5 = -0.5)
                var offsetY = headerAndEffectSizeY + virtualSizeY - (int)((note.SlideNoteTarget - 0.5f) * noteSizeY) - scrollY - y;
                App.UndoRedoManager.BeginTransaction(TransactionScope.Pattern, pattern.Id);
                StartCaptureOperation(x, y, CaptureOperation.DragSlideNoteTargetGizmo, false, location.ToAbsoluteNoteIndex(Song), offsetY);
            }
        }

        private void ClearEffectValue(NoteLocation location, Note note, bool allowSelection = false)
        {
            var pattern = Song.Channels[editChannel].PatternInstances[location.PatternIndex];

            if (allowSelection && SelectionCoversMultiplePatterns())
                App.UndoRedoManager.BeginTransaction(TransactionScope.Channel, Song.Id, editChannel);
            else
                App.UndoRedoManager.BeginTransaction(TransactionScope.Pattern, pattern.Id);

            if (allowSelection)
            {
                TransformNotes(selectionMin, selectionMax, false, true, false, (n, idx) =>
                {
                    if (n != null)
                        n.ClearEffectValue(selectedEffectIdx);
                    return n;
                });

                MarkSelectedPatternsDirty();
            }
            else
            {
                note.ClearEffectValue(selectedEffectIdx);
                MarkPatternDirty(location.PatternIndex);
            }

            App.UndoRedoManager.EndTransaction();
        }

        private void ToggleNoteAttack(NoteLocation location, Note note)
        {
            if (note.IsMusical)
            {
                var channel = Song.Channels[editChannel];
                var pattern = channel.PatternInstances[location.PatternIndex];

                if (channel.SupportsNoAttackNotes)
                {
                    var selected = IsNoteSelected(location);

                    if (selected && SelectionCoversMultiplePatterns())
                        App.UndoRedoManager.BeginTransaction(TransactionScope.Channel, Song.Id, editChannel);
                    else
                        App.UndoRedoManager.BeginTransaction(TransactionScope.Pattern, pattern.Id);

                    note.HasAttack ^= true;
                    var attack = note.HasAttack;

                    if (selected)
                    {
                        TransformNotes(selectionMin, selectionMax, false, true, false, (n, idx) =>
                        {
                            if (n != null && n.IsMusical)
                                n.HasAttack = attack;
                            return n;
                        });

                        MarkSelectedPatternsDirty();
                    }
                    else
                    {
                        MarkPatternDirty(location.PatternIndex);
                    }

                    App.UndoRedoManager.EndTransaction();
                }
            }
        }

        private void MapDPCMSample(byte noteValue)
        {
            if (App.Project.Samples.Count == 0)
            {
                Platform.MessageBoxAsync("Before assigning a sample to a piano key, load at least one sample in the 'DPCM Samples' section of the project explorer", "No DPCM sample found", MessageBoxButtons2.OK);
            }
            else
            {
                var sampleNames = new List<string>();
                foreach (var sample in App.Project.Samples)
                    sampleNames.Add(sample.Name);

                var pitchStrings = DPCMSampleRate.GetStringList(true, FamiStudio.StaticInstance.PalPlayback, true, true);

                var dlg = new PropertyDialog("Assign DPCM Sample", 300);
                dlg.Properties.AddLabel(null, "Select sample to assign:"); // 0
                dlg.Properties.AddDropDownList(Platform.IsMobile ? "Select the sample to assign" : null, sampleNames.ToArray(), sampleNames[0]); // 1
                dlg.Properties.AddDropDownList("Pitch :", pitchStrings, pitchStrings[pitchStrings.Length - 1]); // 2
                dlg.Properties.AddCheckBox("Loop :", false); // 3
                dlg.Properties.SetPropertyVisible(0, Platform.IsDesktop);
                dlg.Properties.Build();

                dlg.ShowDialogAsync(ParentForm, (r) =>
                {
                    if (r == DialogResult2.OK)
                    {
                        App.UndoRedoManager.BeginTransaction(TransactionScope.DPCMSamplesMapping, TransactionFlags.StopAudio);
                        var sampleName = dlg.Properties.GetPropertyValue<string>(1);
                        var mapping = App.Project.MapDPCMSample(noteValue, App.Project.GetSample(sampleName));
                        mapping.Pitch = dlg.Properties.GetSelectedIndex(2);
                        mapping.Loop = dlg.Properties.GetPropertyValue<bool>(3);
                        App.UndoRedoManager.EndTransaction();
                        DPCMSampleMapped?.Invoke(noteValue);
                    }
                });
            }
        }

        private void StartDragDPCMSampleMapping(int x, int y, byte noteValue)
        {
            StartCaptureOperation(x, y, CaptureOperation.DragSample);
            draggedSample = null;
        }

        private void ClearDPCMSampleMapping(byte noteValue)
        {
            App.UndoRedoManager.BeginTransaction(TransactionScope.DPCMSamplesMapping, TransactionFlags.StopAudio);
            App.Project.UnmapDPCMSample(noteValue);
            if (noteValue == highlightDPCMSample)
                highlightDPCMSample = -1;
            App.UndoRedoManager.EndTransaction();
            DPCMSampleUnmapped?.Invoke(noteValue);
        }

        private void SelectSingleNote(NoteLocation noteLocation, NoteLocation mouseLocation, Note note)
        {
            var channel = Song.Channels[editChannel];
            var absoluteNoteIndex = noteLocation.ToAbsoluteNoteIndex(Song);
            SetSelection(absoluteNoteIndex, absoluteNoteIndex + Math.Min(note.Duration, channel.GetDistanceToNextNote(noteLocation)) - 1);
        }

        private void SelectPattern(int p)
        {
            SetSelection(Song.GetPatternStartAbsoluteNoteIndex(p),
                         Song.GetPatternStartAbsoluteNoteIndex(p + 1));
        }

        private void SelectAll()
        {
            SetSelection(0, Song.GetPatternStartAbsoluteNoteIndex(Song.Length));
        }

        private void DeleteSingleNote(NoteLocation noteLocation, NoteLocation mouseLocation, Note note)
        {
            var pattern = Song.Channels[editChannel].PatternInstances[noteLocation.PatternIndex];
            var dist = noteLocation.DistanceTo(Song, mouseLocation);

            App.UndoRedoManager.BeginTransaction(TransactionScope.Pattern, pattern.Id);

            // Special case : Remove the release point if user clicks on the release.
            if (note.HasRelease && note.Release == dist)
            {
                note.Release = 0;
            }
            else
            {
                // Preserve mosty effect values when deleting single notes.
                note.Clear();
                note.HasNoteDelay = false;
                note.HasCutDelay = false;
            }

            if (note.IsEmpty)
                pattern.Notes.Remove(noteLocation.NoteIndex);

            MarkPatternDirty(noteLocation.PatternIndex);
            App.UndoRedoManager.EndTransaction();
        }

        private void ToggleReleaseNote(NoteLocation noteLocation, NoteLocation mouseLocation, Note note)
        {
            var channel = Song.Channels[editChannel];
            var pattern = channel.PatternInstances[noteLocation.PatternIndex];

            if (channel.SupportsReleaseNotes)
            {
                App.UndoRedoManager.BeginTransaction(TransactionScope.Pattern, pattern.Id);
                var release = (ushort)Math.Max(1, Song.CountNotesBetween(noteLocation, mouseLocation));
                note.Release = note.Release == release ? 0 : release;
                MarkPatternDirty(noteLocation.PatternIndex);
                App.UndoRedoManager.EndTransaction();
            }
        }

        private void CreateOrphanStopNote(NoteLocation location)
        {
            var channel = Song.Channels[editChannel];
            var pattern = channel.PatternInstances[location.PatternIndex];

            if (channel.SupportsStopNotes)
            {
                if (pattern == null)
                {
                    App.UndoRedoManager.BeginTransaction(TransactionScope.Channel, Song.Id, editChannel);
                    pattern = channel.CreatePatternAndInstance(location.PatternIndex);
                }
                else
                {
                    App.UndoRedoManager.BeginTransaction(TransactionScope.Pattern, pattern.Id);
                }

                var note = pattern.GetOrCreateNoteAt(location.NoteIndex);
                note.Clear();
                note.Value = Note.NoteStop;
                note.Duration = 1;
                MarkPatternDirty(location.PatternIndex);
                App.UndoRedoManager.EndTransaction();
            }
        }

        private void Eyedrop(Note note)
        {
            App.UndoRedoManager.BeginTransaction(TransactionScope.Application);
            NoteEyedropped?.Invoke(note);
            App.UndoRedoManager.EndTransaction();
        }

        public void ReplaceSelectionInstrument(Instrument instrument, Point pos, bool forceInSelection = false)
        {
            if (editMode == EditionMode.Channel && editChannel != ChannelType.Dpcm)
            {
                Debug.Assert(!forceInSelection || IsSelectionValid());

                var channel = Song.Channels[editChannel];

                if (channel.SupportsInstrument(instrument))
                {
                    GetLocationForCoord(pos.X, pos.Y, out var location, out var noteValue);

                    // If dragging inside the selection, replace that.
                    if (IsSelectionValid() && (IsNoteSelected(location) || forceInSelection))
                    {
                        TransformNotes(selectionMin, selectionMax, true, true, false, (note, idx) =>
                        {
                            if (note != null && note.IsMusical)
                                note.Instrument = instrument;
                            return note;
                        });
                    }
                    else 
                    {
                        // Otherwise see if a note is under the cursor.
                        var note = channel.FindMusicalNoteAtLocation(ref location, noteValue);
                        if (note != null)
                        {
                            var pattern = channel.PatternInstances[location.PatternIndex];
                            App.UndoRedoManager.BeginTransaction(TransactionScope.Pattern, pattern.Id);
                            note.Instrument = instrument;
                            MarkPatternDirty(pattern);
                            App.UndoRedoManager.EndTransaction();
                        }
                    }
                }
                else
                {
                    App.ShowInstrumentError(channel, true);
                }
            }
        }

        public void ReplaceSelectionArpeggio(Arpeggio arpeggio, Point pos, bool forceInSelection = false)
        {
            if (editMode == EditionMode.Channel)
            {
                Debug.Assert(!forceInSelection || IsSelectionValid());

                var channel = Song.Channels[editChannel];

                if (channel.SupportsArpeggios)
                {
                    GetLocationForCoord(pos.X, pos.Y, out var location, out var noteValue);

                    // If dragging inside the selection, replace that.
                    if (IsSelectionValid() && (IsNoteSelected(location) || forceInSelection))
                    {
                        TransformNotes(selectionMin, selectionMax, true, true, false, (note, idx) =>
                        {
                            if (note != null && note.IsMusical)
                                note.Arpeggio = arpeggio;
                            return note;
                        });
                    }
                    else
                    {
                        // Otherwise see if a note is under the cursor.
                        var note = channel.FindMusicalNoteAtLocation(ref location, noteValue);
                        if (note != null)
                        {
                            App.UndoRedoManager.BeginTransaction(TransactionScope.Pattern, channel.PatternInstances[location.PatternIndex].Id);
                            note.Arpeggio = arpeggio;
                            App.UndoRedoManager.EndTransaction();
                        }
                    }
                }
            }
        }

        private readonly int[] vertexOrder = new int[] { 1, 2, 0, 3 };

        private int GetWaveVolumeEnvelopeVertexIndex(int x, int y)
        {
            Debug.Assert(editMode == EditionMode.DPCM);
            Debug.Assert(vertexOrder.Length == editSample.VolumeEnvelope.Length);

            var halfHeight    = effectPanelSizeY * 0.5f;
            var halfHeightPad = halfHeight - waveDisplayPaddingY;

            var threshold = ScaleForMainWindow(Platform.IsDesktop ? 10 : 20);

            x -= pianoSizeX;
            y -= headerSizeY;

            for (int i = 0; i < 4; i++)
            {
                var idx = vertexOrder[i];

                var vx = GetPixelForWaveTime(editSample.VolumeEnvelope[idx].sample / editSample.SourceSampleRate, scrollX);
                var vy = (int)Math.Round(halfHeight - (editSample.VolumeEnvelope[idx].volume - 1.0f) * halfHeightPad);

                var dx = Math.Abs(vx - x);
                var dy = Math.Abs(vy - y);

                if (dx < threshold &&
                    dy < threshold)
                {
                    return idx;
                }
            }

            return -1;
        }

        private bool IsPointInHeader(int x, int y)
        {
            return x > pianoSizeX && y < headerSizeY;
        }

        private bool IsPointInHeaderTopPart(int x, int y)
        {
            return (editMode == EditionMode.Enveloppe || editMode == EditionMode.Arpeggio) && x > pianoSizeX && y > 0 && y < headerSizeY / 2;
        }

        private bool IsPointInHeaderBottomPart(int x, int y)
        {
            return (editMode == EditionMode.Enveloppe || editMode == EditionMode.Arpeggio) && x > pianoSizeX && y >= headerSizeY / 2 && y < headerSizeY;
        }

        private bool IsPointWhereCanResizeEnvelope(int x, int y)
        {
            var pixel0 = GetPixelForNote(EditEnvelope.Length) + pianoSizeX;
            var pixel1 = pixel0 + bmpEnvResize.ElementSize.Width;

            return IsPointInHeaderTopPart(x, y) && x > pixel0 && x <= pixel1;
        }

        private bool IsPointInPiano(int x, int y)
        {
            return x < pianoSizeX && y > headerAndEffectSizeY;
        }

        private bool IsPointInEffectList(int x, int y)
        {
            return showEffectsPanel && editMode == EditionMode.Channel && x < pianoSizeX && y > headerSizeY && y < headerAndEffectSizeY;
        }

        private bool IsPointInEffectPanel(int x, int y)
        {
            return showEffectsPanel && (editMode == EditionMode.Channel || editMode == EditionMode.DPCM) && x > pianoSizeX && y > headerSizeY && y < headerAndEffectSizeY;
        }

        private bool IsPointInNoteArea(int x, int y)
        {
            return y > headerSizeY && x > pianoSizeX;
        }

        private bool IsPointInTopLeftCorner(int x, int y)
        {
            return (editMode == EditionMode.Channel || editMode == EditionMode.DPCM) && y < headerSizeY && x < pianoSizeX;
        }

        private Rectangle GetToggleEffectPannelButtonRect()
        {
            var expandButtonSize = bmpExpandedSmall.ElementSize.Width;
            return new Rectangle(effectIconPosX, effectIconPosY, expandButtonSize, expandButtonSize);
        }

        private Rectangle GetSnapButtonRect()
        {
            var snapButtonSize = bmpSnap.ElementSize.Width;
            var posX = pianoSizeX - (snapButtonSize + headerIconsPosX) * 2 - 1;
            return new Rectangle(posX, headerIconsPosY, snapButtonSize, snapButtonSize);
        }

        private Rectangle GetMaximizeButtonRect()
        {
            var snapButtonSize = bmpSnap.ElementSize.Width;
            var posX = pianoSizeX - (snapButtonSize + headerIconsPosX) - 1;
            return new Rectangle(posX, headerIconsPosY, snapButtonSize, snapButtonSize);
        }

        private Rectangle GetSnapResolutionRect()
        {
            var toggleRect = GetToggleEffectPannelButtonRect();
            var snapRect   = GetSnapButtonRect();
            return new Rectangle(toggleRect.Right, toggleRect.Top + 1, snapRect.Left - toggleRect.Right - (int)MainWindowScaling, snapRect.Height);
        }

        private bool IsPointOnToggleEffectPannelButton(int x, int y)
        {
            return GetToggleEffectPannelButtonRect().Contains(x, y);
        }

        private bool IsPointOnSnapButton(int x, int y)
        {
            return GetSnapButtonRect().Contains(x, y);
        }

        private bool IsPointOnSnapResolution(int x, int y)
        {
            return GetSnapResolutionRect().Contains(x, y);
        }

        private bool IsPointOnMaximizeButton(int x, int y)
        {
            return GetMaximizeButtonRect().Contains(x, y);
        }

        private void UpdateToolTip(MouseEventArgs e)
        {
            var tooltip = "";
            var newNoteTooltip = "";

            if (IsPointInHeader(e.X, e.Y) && editMode == EditionMode.Channel)
            {
                tooltip = "{MouseLeft} Seek - {MouseRight} Select - {MouseRight}{MouseRight} Select entire pattern";
            }
            else if (IsPointInHeaderTopPart(e.X, e.Y) && (editMode == EditionMode.Enveloppe || editMode == EditionMode.Arpeggio))
            {
                if (IsPointWhereCanResizeEnvelope(e.X, e.Y))
                    tooltip = "{MouseLeft} Resize envelope\n";
                else
                    tooltip = "{MouseRight} Select";
            }
            else if (IsPointInHeaderBottomPart(e.X, e.Y) && (editMode == EditionMode.Enveloppe || editMode == EditionMode.Arpeggio))
            {
                tooltip = "{MouseLeft} Set loop point\n{MouseRight} Set release point (volume only, must have loop point)";
            }
            else if (IsPointInPiano(e.X, e.Y))
            {
                tooltip = "{MouseLeft} Play piano - {MouseWheel} Pan";
            }
            else if (IsPointOnSnapResolution(e.X, e.Y) || IsPointOnSnapButton(e.X, e.Y))
            {
                tooltip = "{MouseLeft} Toggle snapping {Shift} {S} {MouseWheel} Change snap precision\n{MouseRight} More Options...";
            }
            else if (IsPointOnMaximizeButton(e.X, e.Y))
            {
                tooltip = "{MouseLeft} Maximize/Minimize piano roll {1}";
            }
            else if (IsPointInTopLeftCorner(e.X, e.Y))
            {
                tooltip = "{MouseLeft} Show/hide effect panel {Ctrl} {1}";
            }
            else if (IsPointInEffectList(e.X, e.Y))
            {
                tooltip = "{MouseLeft} Select effect track to edit";
            }
            else if (IsPointInEffectPanel(e.X, e.Y))
            {
                if (editMode == EditionMode.Channel)
                {
                    tooltip = "{MouseLeft} Set effect value - {MouseWheel} Pan\n{Shift} {MouseLeft} Set effect value (fine) - {MouseRight} Clear effect value";
                }
                else if (editMode == EditionMode.DPCM)
                {
                    var vertexIdx = GetWaveVolumeEnvelopeVertexIndex(e.X, e.Y);

                    if (vertexIdx >= 0)
                    {
                        tooltip = "{MouseLeft} {Drag} Move volume envelope vertex\n{MouseRight} Reset volume to 100%";
                    }
                }
            }
            else if ((IsPointInNoteArea(e.X, e.Y) || IsPointInHeader(e.X, e.Y)) && editMode == EditionMode.DPCM)
            {
                tooltip = "{MouseLeft} {Drag} or {MouseRight} {Drag} Select samples from source data";

                if (IsSelectionValid())
                {
                    tooltip += "\n{Del} Delete selected samples.";
                    newNoteTooltip = $"{(selectionMax - selectionMin + 1)} samples selected";
                }
            }
            else if (IsPointInNoteArea(e.X, e.Y))
            {
                if (editMode == EditionMode.Channel)
                {
                    if (GetLocationForCoord(e.X, e.Y, out var location, out byte noteValue))
                    {
                        newNoteTooltip = $"{Note.GetFriendlyName(noteValue)} [{location.PatternIndex+1:D3} : {location.NoteIndex:D3}]";

                        var channel = Song.Channels[editChannel];
                        var note = channel.FindMusicalNoteAtLocation(ref location, noteValue);

                        if (note != null)
                        {
                            if (note.Instrument != null)
                                newNoteTooltip += $" ({note.Instrument.Name})";
                            if (note.IsArpeggio)
                                newNoteTooltip += $" (Arpeggio: {note.Arpeggio.Name})";
                        }

                        // Main click action.
                        var captureOp = GetHighlightedNoteCaptureOperationForCoord(e.X, e.Y);
                        var tooltipList = new List<string>();

                        switch (captureOp)
                        {
                            case CaptureOperation.ResizeNoteStart:
                            case CaptureOperation.ResizeSelectionNoteStart:
                            case CaptureOperation.ResizeNoteEnd:
                            case CaptureOperation.ResizeSelectionNoteEnd:
                                tooltipList.Add("{MouseLeft} {Drag} Resize note(s)");
                                break;
                            case CaptureOperation.MoveNoteRelease:
                                tooltipList.Add("{MouseLeft} {Drag} Move release point");
                                break;
                            case CaptureOperation.DragNote:
                            case CaptureOperation.DragSelection:
                                tooltipList.Add("{MouseLeft} {Drag} Move note(s)");
                                break;
                            default:
                                tooltipList.Add("{MouseLeft} {Drag} Create note");
                                break;
                        }

                        if (note != null)
                        {
                            if (channel.SupportsReleaseNotes && captureOp != CaptureOperation.MoveNoteRelease)
                                tooltipList.Add("{Shift} {MouseLeft} Set release point");
                            if (channel.SupportsSlideNotes)
                                tooltipList.Add("{S} {MouseLeft} {Drag} Slide note");
                            if (note.IsMusical)
                            {
                                tooltipList.Add("{A} {MouseLeft} Toggle note attack");
                                tooltipList.Add("{I} {MouseLeft} Instrument Eyedrop");
                            }
                            tooltipList.Add("{MouseRight} Delete note");
                        }
                        else 
                        {
                            if (channel.SupportsStopNotes)
                                tooltipList.Add("{T} {MouseLeft} Add stop note");
                            tooltipList.Add("{MouseRight} {MouseRight} Select entire note");
                        }

                        tooltipList.Add("{MouseWheel} Pan");

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

                    if (IsSelectionValid())
                    {
                        if (newNoteTooltip.Length > 0)
                            newNoteTooltip += " ";

                        newNoteTooltip += $"{(selectionMax - selectionMin + 1)} {(Song.Project.UsesFamiTrackerTempo ? "notes" : "frames")} selected";
                    }
                }
                else if (editMode == EditionMode.Enveloppe || editMode == EditionMode.Arpeggio)
                {
                    tooltip = "{MouseLeft} Set envelope value - {MouseWheel} Pan";

                    if (GetEnvelopeValueForCoord(e.X, e.Y, out int idx, out sbyte value))
                    {
                        newNoteTooltip = $"{idx:D3} : {value}";

                        if (IsSelectionValid())
                            newNoteTooltip += $" ({selectionMax - selectionMin + 1} frames selected)";
                    }
                }
                else if (editMode == EditionMode.DPCMMapping)
                {
                    if (GetNoteValueForCoord(e.X, e.Y, out byte noteValue))
                    {
                        if (App.Project.NoteSupportsDPCM(noteValue))
                        {
                            newNoteTooltip = $"{Note.GetFriendlyName(noteValue)}";

                            var mapping = App.Project.GetDPCMMapping(noteValue);
                            if (mapping == null)
                            {
                                tooltip = "{MouseLeft} Assign DPCM sample - {MouseWheel} Pan";
                            }
                            else
                            {
                                tooltip = "{MouseLeft}{MouseLeft} Sample properties - {MouseRight} Unassign DPCM sample {MouseWheel} Pan";

                                if (mapping.Sample != null)
                                    newNoteTooltip += $" ({mapping.Sample.Name})";
                            }
                        }
                        else
                            tooltip = "Samples must be between C1 and D6";
                    }
                }
            }

            App.SetToolTip(tooltip);

            if (noteTooltip != newNoteTooltip)
                noteTooltip = newNoteTooltip;
        }

        private double GetEffectiveSnapResolution(NoteLocation location)
        {
            var snapFactor = SnapResolutionType.Factors[snapResolution];
            var beatLength = Song.GetPatternBeatLength(location.PatternIndex);

            if (snapFactor >= 1.0f)
            {
                return snapFactor;
            }
            else
            {
                int invSnapFactor = (int)Math.Round(1.0f / snapFactor);
                // For fractional snapping, make sure the beat length can somewhat divided.
                if (invSnapFactor > beatLength)
                {
                    return 1.0 / beatLength;
                }
                else
                {
                    return snapFactor;
                }
            }
        }

        private int SnapNote(int absoluteNoteIndex, bool roundUp = false, bool forceSnap = false)
        {
            if (SnapEnabled || forceSnap)
            {
                var location = NoteLocation.FromAbsoluteNoteIndex(Song, absoluteNoteIndex);
                
                if (location.PatternIndex >= Song.Length)
                    return absoluteNoteIndex;

                var beatLength = Song.GetPatternBeatLength(location.PatternIndex);
                var snapFactor = GetEffectiveSnapResolution(location);

                var snappedNoteIndex = location.NoteIndex;
                if (snapFactor >= 1.0)
                {
	                var numNotes = beatLength * (int)snapFactor;
                    if (numNotes == 1) 
                        return absoluteNoteIndex;
                    snappedNoteIndex = (location.NoteIndex / numNotes + (roundUp ? 1 : 0)) * numNotes;
          		}
                else
                {
                    // Subtract the base note so that snapping inside a note is always deterministic. 
                    // Otherwise, rounding errors can create a different snapping pattern every note (6-5-5-6 like Gimmick).
                    var baseNoteIdx   = location.NoteIndex / beatLength * beatLength;
                    var beatFrameIdx  = location.NoteIndex % beatLength;
                    var numSnapPoints = (int)Math.Round(1.0 / snapFactor);

                    for (int i = numSnapPoints - 1; i >= 0; i--)
                    {
                        var snapPoint = (int)Math.Round(i / (double)numSnapPoints * beatLength);
                        if (beatFrameIdx >= snapPoint)
                        {
                            if (roundUp)
                                snapPoint = (int)Math.Round((i + 1) / (double)numSnapPoints * beatLength);
                            snappedNoteIndex = baseNoteIdx + snapPoint;
                            break;
                        }
                    }
                }

                if (!roundUp)
                    snappedNoteIndex = Math.Min(Song.GetPatternLength(location.PatternIndex) - 1, snappedNoteIndex);

                return Song.GetPatternStartAbsoluteNoteIndex(location.PatternIndex, snappedNoteIndex);
            }
            else
            {
                return absoluteNoteIndex;
            }
        }

        private void StartNoteCreation(MouseEventArgs e, NoteLocation location, byte noteValue)
        { 
            var channel = Song.Channels[editChannel];

            if (channel.SupportsInstrument(App.SelectedInstrument))
            {
                App.PlayInstrumentNote(noteValue, false, false);
                StartCaptureOperation(e.X, e.Y, CaptureOperation.CreateNote, true);
                UpdateNoteCreation(e.X, e.Y, true, false);
            }
            else
            {
                App.ShowInstrumentError(channel, true);
            }
        }

        private void UpdateNoteCreation(int x, int y, bool first, bool last)
        {
            ScrollIfNearEdge(x, y);
            GetLocationForCoord(x, y, out var location, out var noteValue, true);

            if (!first)
            {
                // Need to cancel the transaction every time since the start pattern may change.
                App.UndoRedoManager.RestoreTransaction(false);
                App.UndoRedoManager.AbortTransaction();
            }

            var minLocation = NoteLocation.Min(location, captureNoteLocation);
            var maxLocation = NoteLocation.Max(location, captureNoteLocation);
            var minAbsoluteNoteIndex = minLocation.ToAbsoluteNoteIndex(Song);
            var maxAbsoluteNoteIndex = maxLocation.ToAbsoluteNoteIndex(Song);

            highlightNoteAbsIndex = minAbsoluteNoteIndex;

            var channel = Song.Channels[editChannel];
            var pattern = channel.PatternInstances[minLocation.PatternIndex];

            if (pattern == null)
            {
                App.UndoRedoManager.BeginTransaction(TransactionScope.Channel, Song.Id, editChannel);
                pattern = channel.CreatePatternAndInstance(minLocation.PatternIndex);
            }
            else
            {
                App.UndoRedoManager.BeginTransaction(TransactionScope.Pattern, pattern.Id);
            }

            var note = pattern.GetOrCreateNoteAt(minLocation.NoteIndex);

            note.Value = (byte)captureNoteValue;
            note.Instrument = editChannel == ChannelType.Dpcm ? null : App.SelectedInstrument;
            note.Arpeggio = Song.Channels[editChannel].SupportsArpeggios ? App.SelectedArpeggio : null;
            note.Duration = (ushort)Math.Max(1, SnapNote(maxAbsoluteNoteIndex, true, false) - minAbsoluteNoteIndex);

            if (last)
            {
                MarkPatternDirty(pattern);
                App.StopOrReleaseIntrumentNote();
                App.UndoRedoManager.EndTransaction();
            }
        }

        private void StartNoteDrag(int x, int y, CaptureOperation captureOp, NoteLocation location, Note note)
        {
            var dragSelection = 
                captureOp == CaptureOperation.DragSelection || 
                captureOp == CaptureOperation.ResizeSelectionNoteStart;

            var minPatternIdx = Song.PatternIndexFromAbsoluteNoteIndex(selectionMin);
            var maxPatternIdx = Song.PatternIndexFromAbsoluteNoteIndex(selectionMax);
            var multiplePatterns = dragSelection && minPatternIdx != maxPatternIdx;

            var channel = Song.Channels[editChannel];
            var pattern = channel.PatternInstances[location.PatternIndex];

            if (multiplePatterns)
                App.UndoRedoManager.BeginTransaction(TransactionScope.Channel, Song.Id, editChannel);
            else
                App.UndoRedoManager.BeginTransaction(TransactionScope.Pattern, pattern.Id);

            StartCaptureOperation(x, y, captureOp, true, location.ToAbsoluteNoteIndex(Song));

            if (dragSelection)
            {
                dragNotes = GetSparseSelectedNotes(selectionMin);
                dragFrameMin = selectionMin;
                dragFrameMax = selectionMax;
            }
            else
            {
                var absPrevNoteIdx = location.ToAbsoluteNoteIndex(Song);

                dragFrameMin = absPrevNoteIdx;
                dragFrameMax = absPrevNoteIdx;

                dragNotes.Clear();
                dragNotes[absPrevNoteIdx] = note.Clone();
            }

            dragLastNoteValue = -1;

            if (captureThresholdMet)
                UpdateNoteDrag(x, y, false);
        }

        private void UpdateNoteDrag(int x, int y, bool final)
        {
            Debug.Assert(
                App.UndoRedoManager.HasTransactionInProgress && (
                    App.UndoRedoManager.UndoScope == TransactionScope.Pattern ||
                    App.UndoRedoManager.UndoScope == TransactionScope.Channel));

            var channel = Song.Channels[editChannel];

            App.UndoRedoManager.RestoreTransaction(false);

            ScrollIfNearEdge(x, y, true, true);
            GetLocationForCoord(x, y, out var location, out var noteValue, true);

            var resizeStart = captureOperation == CaptureOperation.ResizeNoteStart || captureOperation == CaptureOperation.ResizeSelectionNoteStart;
            var deltaNoteIdx = location.ToAbsoluteNoteIndex(Song) - captureMouseAbsoluteIdx;
            var deltaDuration = resizeStart ? -deltaNoteIdx : 0;
            var deltaNoteValue = resizeStart ? 0 : noteValue - captureNoteValue;
            var newDragFrameMin = dragFrameMin + deltaNoteIdx;
            var newDragFrameMax = dragFrameMax + deltaNoteIdx;

            if (final && deltaNoteIdx == 0 && deltaNoteValue == 0)
            {
                App.UndoRedoManager.AbortTransaction();
            }
            else
            {
                highlightNoteAbsIndex = captureNoteLocation.Advance(Song, deltaNoteIdx).ToAbsoluteNoteIndex(Song);

                // When we cross pattern boundaries, we will have to promote the current transaction
                // from pattern to channel.
                if (App.UndoRedoManager.UndoScope == TransactionScope.Pattern)
                {
                    var initialPatternMinIdx = Song.PatternIndexFromAbsoluteNoteIndex(dragFrameMin);
                    var initialPatternMaxIdx = Song.PatternIndexFromAbsoluteNoteIndex(dragFrameMax);
                    Debug.Assert(initialPatternMinIdx == initialPatternMaxIdx);

                    var newPatternMinIdx = Song.PatternIndexFromAbsoluteNoteIndex(newDragFrameMin);
                    var newPatternMaxIdx = Song.PatternIndexFromAbsoluteNoteIndex(newDragFrameMax);

                    bool multiplePatterns = newPatternMinIdx != initialPatternMinIdx ||
                                            newPatternMaxIdx != initialPatternMinIdx;

                    if (multiplePatterns)
                        PromoteTransaction(TransactionScope.Channel, Song.Id, editChannel);
                }

                var copy = ModifierKeys.Control;
                var keepFx = captureOperation != CaptureOperation.DragSelection;

                // If not copying, delete original notes.
                if (!copy)
                {
                    channel.DeleteNotesBetween(dragFrameMin, dragFrameMax + 1, keepFx);
                }

                // Clear where the new notes are going to be.
                channel.DeleteNotesBetween(newDragFrameMin, newDragFrameMax + 1, keepFx);

                foreach (var kv in dragNotes)
                {
                    var frame = kv.Key + deltaNoteIdx;

                    if (frame < 0 || frame >= Song.GetPatternStartAbsoluteNoteIndex(Song.Length))
                        continue;

                    var newLocation = NoteLocation.FromAbsoluteNoteIndex(Song, kv.Key + deltaNoteIdx);
                    var pattern = channel.PatternInstances[newLocation.PatternIndex];

                    if (pattern == null)
                    {
                        // Cant create patterns without having promoted the transaction to channel.
                        Debug.Assert(App.UndoRedoManager.UndoScope == TransactionScope.Channel);
                        pattern = channel.CreatePatternAndInstance(newLocation.PatternIndex);
                    }

                    if (keepFx)
                    {
                        var oldNote = kv.Value;

                        if (oldNote.IsMusical)
                        {
                            var newNote = pattern.GetOrCreateNoteAt(newLocation.NoteIndex);

                            newNote.Value = (byte)Utils.Clamp(oldNote.Value + deltaNoteValue, Note.MusicalNoteMin, Note.MusicalNoteMax);
                            newNote.Instrument = oldNote.Instrument;
                            newNote.Arpeggio = oldNote.Arpeggio;
                            newNote.SlideNoteTarget = (byte)(oldNote.IsSlideNote ? Utils.Clamp(oldNote.SlideNoteTarget + deltaNoteValue, Note.MusicalNoteMin, Note.MusicalNoteMax) : 0);
                            newNote.Flags = oldNote.Flags;
                            newNote.Duration = (ushort)Math.Max(1, oldNote.Duration + deltaDuration);
                            newNote.Release = oldNote.Release;

                            // HACK : Try to preserve releases
                            if (oldNote.HasRelease && !newNote.HasRelease && newNote.Duration > 1)
                                newNote.Release = newNote.Duration - 1;
                        }
                        else if (oldNote.IsStop)
                        {
                            var newNote = pattern.GetOrCreateNoteAt(newLocation.NoteIndex);
                            newNote.Value = Note.NoteStop;
                            newNote.Duration = 1;
                        }
                    }
                    else
                    {
                        var note = kv.Value.Clone();
                        if (note.IsMusical)
                        {
                            note.Value = (byte)Utils.Clamp(note.Value + deltaNoteValue, Note.MusicalNoteMin, Note.MusicalNoteMax);
                            note.SlideNoteTarget = (byte)(note.IsSlideNote ? Utils.Clamp(note.SlideNoteTarget + deltaNoteValue, Note.MusicalNoteMin, Note.MusicalNoteMax) : 0);
                        }
                        pattern.SetNoteAt(newLocation.NoteIndex, note);
                    }
                }

                if (captureOperation == CaptureOperation.DragSelection || captureOperation == CaptureOperation.ResizeSelectionNoteStart)
                {
                    selectionMin = Utils.Clamp(newDragFrameMin, 0, Song.GetPatternStartAbsoluteNoteIndex(Song.Length) - 1);
                    selectionMax = Utils.Clamp(newDragFrameMax, 0, Song.GetPatternStartAbsoluteNoteIndex(Song.Length) - 1);
                }

                if (dragLastNoteValue != noteValue &&
                    (captureOperation == CaptureOperation.DragNote))
                {
                    var dragNote = (Note)null;
                    foreach (var n in dragNotes)
                    {
                        dragNote = n.Value;
                        break;
                    }

                    // Itch.io request.
                    bool disableDragSound = Settings.NoDragSoungWhenPlaying && App.IsPlaying;

                    // No sound feedback on stop/release notes.
                    if (dragNote != null && dragNote.IsMusical && !disableDragSound)
                    {
                        // If we are adding a new note or if the threshold has not been met, we need
                        // to play the selected note from the project explorer, otherwise we need to 
                        // play the instrument from the selected note.
                        if (!captureThresholdMet)
                        {
                            App.PlayInstrumentNote(noteValue, false, false);
                        }
                        else
                        {
                            foreach (var n in dragNotes)
                            {
                                App.PlayInstrumentNote(noteValue, false, false, true, n.Value.Instrument, n.Value.Arpeggio);
                                break;
                            }
                        }
                    }

                    dragLastNoteValue = noteValue;
                }
            }

            if (final)
            {
                int p0, p1;

                p0 = Song.PatternIndexFromAbsoluteNoteIndex(dragFrameMin + 0);
                p1 = Song.PatternIndexFromAbsoluteNoteIndex(dragFrameMax + 1);
                for (int p = p0; p <= p1 && p < Song.Length; p++)
                    PatternChanged?.Invoke(channel.PatternInstances[p]);
                channel.InvalidateCumulativePatternCache(p0, p1);
                p0 = Song.PatternIndexFromAbsoluteNoteIndex(dragFrameMin + deltaNoteIdx + 0);
                p1 = Song.PatternIndexFromAbsoluteNoteIndex(dragFrameMax + deltaNoteIdx + 1);
                for (int p = p0; p <= p1 && p < Song.Length; p++)
                    PatternChanged?.Invoke(channel.PatternInstances[p]);
                channel.InvalidateCumulativePatternCache(p0, p1);

                App.Project.ValidateIntegrity();
                if (App.UndoRedoManager.HasTransactionInProgress)
                    App.UndoRedoManager.EndTransaction();
                App.StopInstrument();
            }

            MarkDirty();
        }

        private void StartNoteResizeEnd(int x, int y, CaptureOperation captureOp, NoteLocation location)
        {
            var pattern = Song.Channels[editChannel].PatternInstances[location.PatternIndex];
            var dragSelection = captureOp == CaptureOperation.ResizeSelectionNoteEnd;
            var multiplePatterns = dragSelection &&
                                   Song.PatternIndexFromAbsoluteNoteIndex(selectionMin) !=
                                   Song.PatternIndexFromAbsoluteNoteIndex(selectionMax);

            if (multiplePatterns)
                App.UndoRedoManager.BeginTransaction(TransactionScope.Channel, Song.Id, editChannel);
            else
                App.UndoRedoManager.BeginTransaction(TransactionScope.Pattern, pattern.Id);

            StartCaptureOperation(x, y, captureOp, true, location.ToAbsoluteNoteIndex(Song));
        }

        private void UpdateNoteResizeEnd(int x, int y, bool final)
        {
            var channel = Song.Channels[editChannel];

            App.UndoRedoManager.RestoreTransaction(false);

            var selection = captureOperation == CaptureOperation.ResizeSelectionNoteEnd;
            var min = selection ? selectionMin : captureNoteAbsoluteIdx;
            var max = selection ? selectionMax : captureNoteAbsoluteIdx;

            // Since we may be be dragging from the "visual" duration which may be shorter than
            // the real duration, we truncate them right away.
            TransformNotes(min, max, false, final, false, (note, idx) =>
            {
                if (note != null && note.IsMusical)
                {
                    var distToNext = channel.GetDistanceToNextNote(NoteLocation.FromAbsoluteNoteIndex(Song, min + idx));
                    if (distToNext >= 0)
                        note.Duration = (ushort)Utils.Clamp(note.Duration, 1, distToNext);
                }

                return note;
            });

            ScrollIfNearEdge(x, y);
            GetLocationForCoord(x, y, out var location, out var noteValue, true);

            var deltaNoteIdx = location.ToAbsoluteNoteIndex(Song) - captureMouseAbsoluteIdx;

            TransformNotes(min, max, false, final, false, (note, idx) =>
            {
                if (note != null && note.IsMusical)
                {
                    // HACK : Try to preserve releases.
                    var hadRelease = note.HasRelease;
                    note.Duration = (ushort)Math.Max(1, note.Duration + deltaNoteIdx);
                    if (hadRelease && !note.HasRelease && note.Duration > 1)
                        note.Release = note.Duration - 1;
                }

                return note;
            });

            if (final)
                App.UndoRedoManager.EndTransaction();
        }

        private void StartMoveNoteRelease(int x, int y, NoteLocation location)
        {
            var pattern = Song.Channels[editChannel].PatternInstances[location.PatternIndex];
            App.UndoRedoManager.BeginTransaction(TransactionScope.Pattern, pattern.Id);
            StartCaptureOperation(x, y, CaptureOperation.MoveNoteRelease, false, location.ToAbsoluteNoteIndex(Song));
        }

        private void UpdateMoveNoteRelease(int x, int y)
        {
            GetLocationForCoord(x, y, out var location, out var noteValue, false);

            var channel = Song.Channels[editChannel];
            var pattern = channel.PatternInstances[captureNoteLocation.PatternIndex];
            var note = pattern.Notes[captureNoteLocation.NoteIndex];
            note.Release = (ushort)Utils.Clamp(Song.CountNotesBetween(captureNoteLocation, location), 1, note.Duration - 1);
            channel.InvalidateCumulativePatternCache(pattern);
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

        private bool CaptureOperationRequiresEffectHighlight(CaptureOperation op)
        {
            return
                op == CaptureOperation.ChangeEffectValue;
        }

        private bool CaptureOperationRequiresNoteHighlight(CaptureOperation op)
        {
            return
                op == CaptureOperation.ResizeSelectionNoteEnd ||
                op == CaptureOperation.ResizeNoteEnd ||
                op == CaptureOperation.ResizeSelectionNoteStart ||
                op == CaptureOperation.ResizeNoteStart ||
                op == CaptureOperation.MoveNoteRelease ||
                op == CaptureOperation.DragSelection ||
                op == CaptureOperation.DragNote;
        }

        private CaptureOperation GetHighlightedEffectCaptureOperationForCoord(int x, int y)
        {
            Debug.Assert(editMode == EditionMode.Channel);

            if (GetEffectNoteForCoord(x, y, out var location))
            {
                var note = Song.Channels[editChannel].GetNoteAt(location);

                if (note != null && note.HasValidEffectValue(selectedEffectIdx))
                    return CaptureOperation.ChangeEffectValue;
            }

            return CaptureOperation.None;
        }

        private CaptureOperation GetHighlightedNoteCaptureOperationForCoord(int x, int y)
        {
            Debug.Assert(editMode == EditionMode.Channel);

            var note = GetNoteForCoord(x, y, out var mouseLocation, out var noteLocation, out var noteDuration);
            if (note != null)
            {
                if (note.IsMusical)
                {
                    var minAbsoluteNoteIdx = noteLocation.ToAbsoluteNoteIndex(Song);
                    var maxAbsoluteNoteIdx = noteLocation.ToAbsoluteNoteIndex(Song) + noteDuration;

                    var minNoteCoordX = GetPixelForNote(minAbsoluteNoteIdx);
                    var maxNoteCoordX = GetPixelForNote(maxAbsoluteNoteIdx);

                    x -= pianoSizeX;

                    if (x > maxNoteCoordX - noteResizeMargin)
                        return IsNoteSelected(noteLocation) ? CaptureOperation.ResizeSelectionNoteEnd : CaptureOperation.ResizeNoteEnd;
                    if (x < minNoteCoordX + noteResizeMargin)
                        return IsNoteSelected(noteLocation) ? CaptureOperation.ResizeSelectionNoteStart : CaptureOperation.ResizeNoteStart;

                    if (note.HasRelease && Song.CountNotesBetween(noteLocation, mouseLocation) == note.Release)
                        return CaptureOperation.MoveNoteRelease;
                }

                return IsNoteSelected(noteLocation) ? CaptureOperation.DragSelection : CaptureOperation.DragNote;
            }

            return CaptureOperation.None;
        }

        private void UpdateCursor()
        {
            var pt = PointToClient(Cursor.Position);

            if (EditEnvelope != null && EditEnvelope.CanResize && IsPointWhereCanResizeEnvelope(pt.X, pt.Y) && captureOperation != CaptureOperation.Select || captureOperation == CaptureOperation.ResizeEnvelope)
            {
                Cursor.Current = Cursors.SizeWE;
            }
            else if (captureOperation == CaptureOperation.ChangeEffectValue)
            {
                Cursor.Current = Cursors.SizeNS;
            }
            else if (ModifierKeys.Control && (captureOperation == CaptureOperation.DragNote || captureOperation == CaptureOperation.DragSelection))
            {
                Cursor.Current = Cursors.CopyCursor;
            }
            else if (editMode == EditionMode.Channel && FamiStudioForm.IsKeyDown(Keys.I))
            {
                Cursor.Current = Cursors.Eyedrop;
            }
            else
            {
                if (editMode == EditionMode.Channel && captureOperation == CaptureOperation.None)
                {
                    if (IsPointInEffectPanel(pt.X, pt.Y))
                    {
                        var captureOp = GetHighlightedEffectCaptureOperationForCoord(pt.X, pt.Y);

                        switch (captureOp)
                        {
                            case CaptureOperation.ChangeEffectValue:
                                Cursor.Current = Cursors.SizeNS;
                                break;
                            default:
                                Cursor.Current = Cursors.Default;
                                break;
                        }
                    }
                    else if (IsPointInNoteArea(pt.X, pt.Y))
                    {
                        var gizmos = GetNoteGizmos(out _, out _);

                        if (gizmos != null)
                        {
                            foreach (var gizmo in gizmos)
                            {
                                if (gizmo.Rect.Contains(pt.X - pianoSizeX, pt.Y - headerAndEffectSizeY))
                                {
                                    Debug.Assert(gizmo.Action == GizmoAction.MoveSlide);
                                    Cursor.Current = Cursors.SizeNS;
                                    return;
                                }
                            }
                        }

                        var captureOp = GetHighlightedNoteCaptureOperationForCoord(pt.X, pt.Y);

                        switch (captureOp)
                        {
                            case CaptureOperation.ResizeNoteStart:
                            case CaptureOperation.ResizeSelectionNoteStart:
                            case CaptureOperation.ResizeNoteEnd:
                            case CaptureOperation.ResizeSelectionNoteEnd:
                            case CaptureOperation.MoveNoteRelease:
                                Cursor.Current = Cursors.SizeWE;
                                break;
                            case CaptureOperation.DragNote:
                            case CaptureOperation.DragSelection:
                                Cursor.Current = Cursors.Move;
                                break;
                            default:
                                Cursor.Current = Cursors.Default;
                                break;
                        }
                    }
                }
                else
                {
                    Cursor.Current = Cursors.Default;
                }
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            bool left   = e.Left;
            bool middle = e.Middle || (e.Left && ModifierKeys.Alt);

            UpdateCursor();
            UpdateCaptureOperation(e.X, e.Y);

            if (middle)
            {
                DoScroll(e.X - mouseLastX, e.Y - mouseLastY);
            }

            UpdateToolTip(e);
            SetMouseLastPos(e.X, e.Y);
            MarkDirty();
            App.SequencerShowExpansionIcons = false;
        }

        private bool HandleMouseUpSnapResolution(MouseEventArgs e)
        {
            if (e.Right && (IsPointOnSnapResolution(e.X, e.Y) || IsPointOnSnapButton(e.X, e.Y)))
            { 
                var options = new ContextMenuOption[SnapResolutionType.Max - SnapResolutionType.Min + 2];

                options[0] = new ContextMenuOption("Enable Snapping", "Enables snapping the specified number of\nbeats in the piano roll", () => { snap = !snap; }, () => snap ? ContextMenuCheckState.Checked : ContextMenuCheckState.Unchecked , false );

                for (var i = SnapResolutionType.Min; i <= SnapResolutionType.Max; i++)
                {
                    var j = i; // Important, copy for lamdba.
                    options[i + 1] = new ContextMenuOption($"Snap To {SnapResolutionType.Names[i]} {(SnapResolutionType.Factors[i] > 1.0 ? "Beats" : "Beat")}", "", () => { snapResolution = j; }, () => snapResolution == j ? ContextMenuCheckState.Radio : ContextMenuCheckState.None, i == 0);
                }

                App.ShowContextMenu(left + e.X, top + e.Y, options);
                return true;
            }

            return false;
        }

        private bool HandleMouseUpChannelNote(MouseEventArgs e)
        {
            return e.Right && HandleContextMenuChannelNote(e.X, e.Y);
        }

        private bool HandleMouseUpChannelHeader(MouseEventArgs e)
        {
            return e.Right && HandleContextMenuChannelHeader(e.X, e.Y);
        }

        private bool HandleMouseUpEffectPanel(MouseEventArgs e)
        {
            return e.Right && HandleContextMenuEffectPanel(e.X, e.Y);
        }

        private bool HandleMouseUpEnvelope(MouseEventArgs e)
        {
            return e.Right && HandleContextMenuEnvelope(e.X, e.Y);
        }

        private bool HandleMouseUpDPCMMapping(MouseEventArgs e)
        {
            return e.Right && HandleContextMenuDPCMMapping(e.X, e.Y);
        }

        private bool HandleMouseUpDPCMVolumeEnvelope(MouseEventArgs e)
        {
            return e.Right && HandleContextMenuWave(e.X, e.Y);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
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
                if (editMode == EditionMode.Channel)
                {
                    if (HandleMouseUpSnapResolution(e)) goto Handled;
                    if (HandleMouseUpChannelNote(e)) goto Handled;
                    if (HandleMouseUpEffectPanel(e)) goto Handled;
                    if (HandleMouseUpChannelHeader(e)) goto Handled;
                }

                if (editMode == EditionMode.Enveloppe ||
                    editMode == EditionMode.Arpeggio)
                {
                    if (HandleMouseUpEnvelope(e)) goto Handled;
                }

                if (editMode == EditionMode.DPCM)
                {
                    if (HandleMouseUpDPCMVolumeEnvelope(e)) goto Handled;
                }

                if (editMode == EditionMode.DPCMMapping)
                {
                    if (HandleMouseUpDPCMMapping(e)) goto Handled;
                }

                return;

            Handled:
                MarkDirty();
            }
        }

        private void ZoomAtLocation(int x, float scale)
        {
            if (scale == 1.0f)
                return;

            // When continuously following, zoom at the seek bar location.
            if (continuouslyFollowing)
                x = (int)(Width * ContinuousFollowPercent);

            Debug.Assert(Platform.IsMobile || scale == 0.5f || scale == 2.0f);

            var pixelX = x - pianoSizeX;
            var absoluteX = pixelX + scrollX;
            var prevNoteSizeX = noteSizeX;

            zoom *= scale;
            zoom = Utils.Clamp(zoom, minZoom, maxZoom);

            Debug.Assert(Platform.IsMobile || Utils.Frac(Math.Log(zoom, 2.0)) == 0.0);

            // This will update the noteSizeX.
            UpdateRenderCoords();

            absoluteX = (int)Math.Round(absoluteX * (noteSizeX / (double)prevNoteSizeX));
            scrollX = absoluteX - pixelX;

            ClampScroll();
            MarkDirty();
        }

        private void ZoomVerticallyAtLocation(int y, float scale)
        {
            if (scale == 1.0f)
                return;

            Debug.Assert(Platform.IsMobile);

            var pixelY = y - headerAndEffectSizeY;
            var absoluteY = pixelY + scrollY;
            var prevNoteSizeY = noteSizeY;

            zoomY *= scale;
            zoomY = Utils.Clamp(zoomY, MinZoomY, MaxZoomY);

            // This will update the noteSizeX.
            UpdateRenderCoords();

            absoluteY = (int)Math.Round(absoluteY * (noteSizeY / (double)prevNoteSizeY));
            scrollY = absoluteY - pixelY;

            ClampScroll();
            MarkDirty();
        }

        private bool HandleMouseWheelZoom(MouseEventArgs e)
        {
            if (e.X > pianoSizeX)
            {
                if (Settings.TrackPadControls && !ModifierKeys.Control)
                {
                    if (ModifierKeys.Shift)
                        scrollX -= (int)e.ScrollY; // MATTT cast
                    else
                        scrollY -= (int)e.ScrollY; // MATTT cast

                    ClampScroll();
                    return true;
                }
                else if (editMode != EditionMode.DPCMMapping)
                {
                    ZoomAtLocation(e.X, e.ScrollY < 0.0f ? 0.5f : 2.0f);
                    return true;
                }
            }

            return false;
        }

        private bool HandleMouseWheelSnapResolution(MouseEventArgs e)
        {
            if (editMode == EditionMode.Channel && (IsPointOnSnapResolution(e.X, e.Y) || IsPointOnSnapButton(e.X, e.Y)))
            {
                snapResolution = Utils.Clamp(snapResolution + (e.ScrollY > 0 ? 1 : -1), SnapResolutionType.Min, SnapResolutionType.Max);
                return true;
            }

            return false;
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            if (HandleMouseWheelZoom(e)) goto Handled;
            if (HandleMouseWheelSnapResolution(e)) goto Handled;

            return;

        Handled:
            MarkDirty();
        }

        protected override void OnMouseHorizontalWheel(MouseEventArgs e)
        {
            scrollX += (int)e.ScrollX; // MATTT cast to int
            ClampScroll();
            MarkDirty();
        }

        public void UpdateFollowMode(bool force = false)
        {
            continuouslyFollowing = false;

            if ((App.IsPlaying || force) && App.FollowModeEnabled && Settings.FollowSync != Settings.FollowSyncSequencer && !panning && captureOperation == CaptureOperation.None && editMode == EditionMode.Channel)
            {
                var frame = App.CurrentFrame;
                var seekX = GetPixelForNote(frame);

                if (Settings.FollowMode == Settings.FollowModeJump)
                {
                    var maxX = Width - pianoSizeX;
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
                    flingVelX *= (float)Math.Exp(delta * -6.0f);
                    flingVelY *= (float)Math.Exp(delta * -6.0f);
                }
                else
                {
                    flingVelX = 0.0f;
                    flingVelY = 0.0f;
                }
            }
        }

        public override void Tick(float delta)
        {
            if (App == null)
                return;

            UpdateCaptureOperation(mouseLastX, mouseLastY, 1.0f, true);
            UpdateFollowMode();
            TickFling(delta);
        }

        private bool GetEffectNoteForCoord(int x, int y, out NoteLocation location)
        {
            if (x > pianoSizeX && y > headerSizeY && y < headerAndEffectSizeY)
            {
                var absoluteNoteIndex = GetAbsoluteNoteIndexForPixel(x - pianoSizeX);
                location = NoteLocation.FromAbsoluteNoteIndex(Song, absoluteNoteIndex);
                if (location.PatternIndex < Song.Length)
                    return true;
            }

            location = NoteLocation.Invalid;
            return false;
        }

        private bool GetNoteValueForCoord(int x, int y, out byte noteValue)
        {
            var rawNoteValue = ((y - headerAndEffectSizeY) + scrollY) / noteSizeY;
            noteValue = (byte)(NumNotes - Utils.Clamp(rawNoteValue, 0, NumNotes));

            // Allow to go outside the window when a capture is in progress.
            var captureInProgress = captureOperation != CaptureOperation.None;
            return x > pianoSizeX && ((y > headerAndEffectSizeY && !captureInProgress) || (rawNoteValue >= 0 && captureInProgress));
        }

        private bool GetLocationForCoord(int x, int y, out NoteLocation location, out byte noteValue, bool allowSnap = false)
        {
            var absoluteNoteIndex = Utils.Clamp(GetAbsoluteNoteIndexForPixel(x - pianoSizeX), 0, Song.GetPatternStartAbsoluteNoteIndex(Song.Length));

            if (allowSnap)
                absoluteNoteIndex = SnapNote(absoluteNoteIndex);

            location = Song.AbsoluteNoteIndexToNoteLocation(absoluteNoteIndex);
            noteValue = (byte)(NumNotes - Utils.Clamp((y + scrollY - headerAndEffectSizeY) / noteSizeY, 0, NumNotes));

            return (x > pianoSizeX && y > headerAndEffectSizeY && location.PatternIndex < Song.Length);
        }

        private int GetVisualNoteDuration(NoteLocation location, Note note)
        {
            var duration = note.Duration;

            var distToNext = Song.Channels[editChannel].GetDistanceToNextNote(location);
            if (distToNext >= 0)
                duration = Math.Min(duration, distToNext);

            return duration;
        }

        private int GetVisualNoteDuration(int absIndex, Note note)
        {
            return GetVisualNoteDuration(NoteLocation.FromAbsoluteNoteIndex(Song, absIndex), note);
        }

        private Note GetNoteForCoord(int x, int y, out NoteLocation mouseLocation, out NoteLocation noteLocation, out int duration)
        {
            Debug.Assert(editMode == EditionMode.Channel);

            if (GetLocationForCoord(x, y, out mouseLocation, out var noteValue))
            {
                noteLocation = mouseLocation;
                var note = Song.Channels[editChannel].FindMusicalNoteAtLocation(ref noteLocation, noteValue);

                if (note != null)
                {
                    duration = (int)note.Duration;

                    var distToNext = Song.Channels[editChannel].GetDistanceToNextNote(noteLocation);
                    if (distToNext >= 0)
                        duration = Math.Min(duration, distToNext);

                    return note;
                }
            }

            mouseLocation = NoteLocation.Invalid;
            noteLocation  = NoteLocation.Invalid;
            duration = -1;
            return null;
        }

        private bool GetEnvelopeValueForCoord(int x, int y, out int idx, out sbyte value)
        {
            if (Platform.IsDesktop)
            {
                var maxValue = 64 / (int)envelopeValueZoom - 1;
                value = (sbyte)(maxValue - (int)Math.Min((y + scrollY - headerAndEffectSizeY - 1) / envelopeValueSizeY, 128));
            }
            else
            {
                Envelope.GetMinMaxValueForType(editInstrument, editEnvelope, out int min, out int max);
                value = (sbyte)Math.Floor((max - min + 1) - ((y - headerAndEffectSizeY) + scrollY) / envelopeValueSizeY + min);
            }

            idx = GetAbsoluteNoteIndexForPixel(x - pianoSizeX);

            return x > pianoSizeX;
        }

#if DEBUG
        public void ValidateIntegrity()
        {
            Debug.Assert(editMode != EditionMode.Channel || editChannel == App.SelectedChannelIndex);
        }
#endif

        public void SerializeState(ProjectBuffer buffer)
        {
            int editModeInt = (int)editMode;
            buffer.Serialize(ref editModeInt);
            editMode = (EditionMode)editModeInt;

            buffer.Serialize(ref editChannel);
            buffer.Serialize(ref editInstrument);
            buffer.Serialize(ref editEnvelope);
            buffer.Serialize(ref editArpeggio);
            buffer.Serialize(ref editSample);
            buffer.Serialize(ref envelopeValueZoom);
            buffer.Serialize(ref envelopeValueOffset);
            buffer.Serialize(ref scrollX);
            buffer.Serialize(ref scrollY);
            buffer.Serialize(ref zoom);
            buffer.Serialize(ref selectedEffectIdx);
            buffer.Serialize(ref showEffectsPanel);
            buffer.Serialize(ref maximized);
            buffer.Serialize(ref selectionMin);
            buffer.Serialize(ref selectionMax);

            if (Platform.IsMobile)
            {
                buffer.Serialize(ref highlightNoteAbsIndex);
                buffer.Serialize(ref highlightDPCMSample);
            }

            if (buffer.IsReading)
            {
                BuildSupportEffectList();
                UpdateRenderCoords();
                ClampScroll();
                MarkDirty();

                captureOperation = CaptureOperation.None;
                Capture = false;
                panning = false;
            }
        }
    }

    public class SnapResolutionType
    {
        public const int Min     = 0;
        public const int Max     = 10;
        public const int OneBeat = 7;

        public static readonly double[] Factors = new[]
        {
            1.0 / 16.0,
            1.0 / 12.0,
            1.0 / 8.0,
            1.0 / 6.0,
            1.0 / 4.0,
            1.0 / 3.0,
            1.0 / 2.0,
            1.0,
            2.0,
            3.0,
            4.0
        };

        public static readonly string[] Names = new string[]
        {
            "1/16",
            "1/12",
            "1/8",
            "1/6",
            "1/4",
            "1/3",
            "1/2",
            "1",
            "2",
            "3",
            "4"
        };
    }
}
