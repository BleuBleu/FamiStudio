using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Channels;

namespace FamiStudio
{
    // This and the Sequencer are the only 2 "Uber-Control" left, where everything is in one class with 
    // no sub-widgets whatsoever. Both of these need a full rewrite. This is for historical reason, when
    // the app started, we didnt have a proper widget system, we sort-of do now. Also, all the touch and
    // mouse input would need to be unified as much as possible.
    //
    // The piano roll would need to be broken down into a timeline, a note area, a piano and things like
    // the envelope editor/DPCM editor need to be pulled out and made into their own editors.
    public class PianoRoll : Container
    {
        const float MinZoomFamiStudio       = 1.0f / 32.0f;
        const float MinZoomOther            = 1.0f / 8.0f;
        const float MaxZoom                 = 16.0f;
        const float MinZoomY                = 0.25f;
        const float MaxZoomY                = 4.0f;
        const float MaxWaveZoom             = 256.0f;
        const float DefaultChannelZoom      = MinZoomOther;
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
        const int DefaultPianoSizeXMobile          = 44;
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
        const int DefaultEffectPanelTextPosX       = 10;
        const int DefaultEffectPanelTextPosY       = 10;
        const int DefaultDPCMTextPosX              = 2;
        const int DefaultRecordingKeyOffsetY       = 12;
        const int DefaultAttackIconPosX            = 1;
        const int DefaultWaveGeometrySampleSize    = 2;
        const int DefaultWaveDisplayPaddingY       = 8;
        const int DefaultScrollBarThickness1       = 10;
        const int DefaultScrollBarThickness2       = 16;
        const int DefaultMinScrollBarLength        = 64;
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
        int effectPanelTextPosX;
        int effectPanelTextPosY;
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
            Envelope,
            DPCM,
            DPCMMapping,
            Arpeggio,
            VideoRecording
        };

        Color whiteKeyPressedColor         = Color.FromArgb( 70, Theme.BlackColor);
        Color blackKeyPressedColor         = Color.FromArgb( 90, Theme.WhiteColor);
        Color whiteKeyHoverColor           = Color.FromArgb( 40, Theme.BlackColor);
        Color blackKeyHoverColor           = Color.FromArgb( 60, Theme.WhiteColor);
        Color selectionBgVisibleColor      = Color.FromArgb( 64, Theme.LightGreyColor1);
        Color selectionBgInvisibleColor    = Color.FromArgb( 16, Theme.LightGreyColor1);
        Color attackColor                  = Color.FromArgb(128, Theme.BlackColor);
        Color attackBrushForceDisplayColor = Color.FromArgb( 64, Theme.BlackColor);
        Color invalidDpcmMappingColor      = Color.FromArgb( 64, Theme.BlackColor);
        Color volumeSlideBarFillColor      = Color.FromArgb( 64, Theme.LightGreyColor1);
        Color loopSectionColor             = Color.FromArgb( 64, Theme.BlackColor);

        TextureAtlasRef bmpLoopSmallFill;
        TextureAtlasRef bmpReleaseSmallFill;
        TextureAtlasRef bmpEnvResize;
        TextureAtlasRef bmpExpandedSmall;
        TextureAtlasRef bmpCollapsedSmall;
        TextureAtlasRef bmpMaximize;
        TextureAtlasRef bmpSnap;
        TextureAtlasRef bmpSnapOff;
        TextureAtlasRef bmpGizmoResizeLeftRight;
        TextureAtlasRef bmpGizmoResizeUpDown;
        TextureAtlasRef bmpGizmoResizeFill;
        TextureAtlasRef bmpEffectFrame;
        TextureAtlasRef bmpEffectRepeat;
        TextureAtlasRef[] bmpEffects;
        float[][] stopNoteGeometry        = new float[2][]; // [1] is used to draw arps.
        float[][] stopReleaseNoteGeometry = new float[2][]; // [1] is used to draw arps.
        float[][] releaseNoteGeometry     = new float[2][]; // [1] is used to draw arps.
        float[]   slideNoteGeometry;
        float[]   seekGeometry;
        float[]   sampleGeometry;
        float[]   mobileEraseGeometry;

        enum CaptureOperation
        {
            None,
            PlayPiano,
            ResizeEnvelope,
            DragLoop,
            DragRelease,
            ChangeEffectValue,
            ChangeSelectionEffectValue,
            ChangeEnvelopeRepeatValue,
            DrawEnvelope,
            Select,
            SelectWave,
            CreateNote,
            CreateSlideNote,
            DeleteNotes,
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
            MoveSelectionNoteRelease,
            ChangeEnvelopeValue,
            MobileZoom,
            MobileZoomVertical,
            MobilePan
        }

        const int ThesholdNormal           = Platform.IsDesktop ? 5 : 50;
        const int ThesholdNormalMobileOnly = Platform.IsDesktop ? 0 : ThesholdNormal;
        const int ThesholdSmall            = Platform.IsDesktop ? 2 : 20;
        const int ThesholdSmallMobileOnly  = Platform.IsDesktop ? 0 : ThesholdSmall;

        static readonly int[] captureThresholds = new[]
        {
            0,                        // None
            0,                        // PlayPiano
            0,                        // ResizeEnvelope
            0,                        // DragLoop
            0,                        // DragRelease
            0,                        // ChangeEffectValue
            0,                        // ChangeSelectionEffectValue
            0,                        // ChangeEnvelopeRepeatValue
            0,                        // DrawEnvelope
            ThesholdNormalMobileOnly, // Select
            ThesholdNormalMobileOnly, // SelectWave
            0,                        // CreateNote
            ThesholdNormal,           // CreateSlideNote
            ThesholdNormal,           // DeleteNotes
            ThesholdNormal,           // DragSlideNoteTarget
            ThesholdNormal,           // DragSlideNoteTargetGizmo
            0,                        // DragVolumeSlideTarget
            0,                        // DragVolumeSlideTargetGizmo
            ThesholdSmall,            // DragNote
            ThesholdSmall,            // DragSelection
            0,                        // AltZoom
            ThesholdNormal,           // DragSample
            0,                        // DragSeekBar
            0,                        // DragWaveVolumeEnvelope
            0,                        // ScrollBarX
            0,                        // ScrollBarY
            ThesholdSmall,            // ResizeNoteStart
            ThesholdSmall,            // ResizeSelectionNoteStart
            ThesholdSmall,            // ResizeNoteEnd
            ThesholdSmall,            // ResizeSelectionNoteEnd
            ThesholdSmallMobileOnly,  // MoveNoteRelease
            ThesholdSmallMobileOnly,  // MoveSelectionNoteRelease
            0,                        // ChangeEnvelopeValue
            0,                        // MobileZoom
            0,                        // MobileZoomVertical
            0,                        // MobilePan
        };

        static readonly bool[] captureWantsRealTimeUpdate = new[]
        {
            false,             // None
            false,             // PlayPiano
            true,              // ResizeEnvelope
            false,             // DragLoop
            false,             // DragRelease
            false,             // ChangeEffectValue
            false,             // ChangeSelectionEffectValue
            false,             // ChangeEnvelopeRepeatValue
            true,              // DrawEnvelope
            true,              // Select
            true,              // SelectWave
            true,              // CreateNote
            true,              // CreateSlideNote
            Platform.IsMobile, // DeleteNotes
            true,              // DragSlideNoteTarget
            true,              // DragSlideNoteTargetGizmo
            false,             // DragVolumeSlideTarget
            false,             // DragVolumeSlideTargetGizmo
            true,              // DragNote
            true,              // DragSelection
            false,             // AltZoom
            true,              // DragSample
            true,              // DragSeekBar
            false,             // DragWaveVolumeEnvelope
            false,             // ScrollBarX
            false,             // ScrollBarY
            true,              // ResizeNoteStart 
            true,              // ResizeSelectionNoteStart
            true,              // ResizeNoteEnd
            true,              // ResizeSelectionNoteEnd
            false,             // MoveNoteRelease
            false,             // MoveSelectionNoteRelease
            false,             // ChangeEnvelopeValue
            false,             // MobileZoom
            false,             // MobileZoomVertical
            false,             // MobilePan
        };

        enum NoteAttackState
        {
            Attack,
            NoAttack,
            NoAttackError
        }

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
        int captureOffsetX = 0;
        int captureOffsetY = 0;
        int playLastNote = -1;
        int playHighlightNote = Note.NoteInvalid;
        int selectionMin = -1;
        int selectionMax = -1;
        int dragSeekPosition = -1;
        int snapResolution = Settings.DefaultSnapResolution;
        int scrollX = 0;
        int scrollY = 0;
        int lastChannelScrollX = -1;
        int lastChannelScrollY = -1;
        float lastChannelZoom = -1;
        int selectedEffectIdx = Platform.IsMobile ? -1 : 0;
        int[] supportedEffects;
        double captureTime;
        bool captureThresholdMet = false;
        bool captureRealTimeUpdate = false;
        bool panning = false;
        bool continuouslyFollowing = false;
        bool maximized = false;
        bool showEffectsPanel = false;
        bool snap = true;
        bool snapEffects = true;
        bool pianoVisible = true;
        bool canFling = false;
        bool relativeEffectScaling = false;
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
        bool highlightRepeatEnvelope = false;
        int highlightNoteAbsIndex = -1;
        int highlightDPCMSample = -1;
        NoteLocation captureMouseLocation;
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
        ValueTuple<int,Color>[] videoHighlightKeys;
        int[] videoChannelTranspose;
        long videoForceDisplayChannelMask;

        // Hover
        int hoverPianoNote  = -1;
        int hoverNoteIndex = -1;
        int hoverNoteCount = 1;
        int hoverEffectIndex = -1;
        int hoverTopLeftButton = -1;

        enum GizmoAction
        {
            ResizeNote,
            MoveRelease,
            MoveSlide,
            ChangeEnvValue,
            ChangeEnvEffectValue,
            ChangeEffectValue,
            MoveVolumeSlideValue,
        };

        private class Gizmo
        {
            public Rectangle Rect;
            public TextureAtlasRef FillImage = null;
            public TextureAtlasRef Image;
            public GizmoAction Action;
            public string GizmoText;
            public int OffsetX;
        };

        public bool SnapAllowed       { get => editMode == EditionMode.Channel; }
        public bool SnapEnabled       { get => SnapAllowed && snap; set { if (SnapAllowed) snap = value; MarkDirty(); } }
        public bool SnapEffectEnabled { get => SnapAllowed && snapEffects; set { if (SnapAllowed) snapEffects = value; MarkDirty(); } }
        public bool SnapTemporarelyDisabled => ModifierKeys.IsAltDown && !Settings.AltLeftForMiddle;

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
        public bool IsEditingInstrument        => editMode == EditionMode.Envelope; 
        public bool IsEditingArpeggio          => editMode == EditionMode.Arpeggio;
        public bool IsEditingDPCMSample        => editMode == EditionMode.DPCM;
        public bool IsEditingDPCMSampleMapping => editMode == EditionMode.DPCMMapping;
        
        public bool CanCopy       => IsActiveControl && IsSelectionValid() && (editMode == EditionMode.Channel || editMode == EditionMode.Envelope || editMode == EditionMode.Arpeggio);
        public bool CanCopyAsText => IsActiveControl && IsSelectionValid() && (editMode == EditionMode.Envelope || editMode == EditionMode.Arpeggio);
        public bool CanPaste      => IsActiveControl && IsSelectionValid() && (editMode == EditionMode.Channel && ClipboardUtils.ContainsNotes || (editMode == EditionMode.Envelope || editMode == EditionMode.Arpeggio) && ClipboardUtils.ContainsEnvelope);
        public bool CanDelete     => IsActiveControl && IsSelectionValid() && (editMode == EditionMode.Channel || editMode == EditionMode.Envelope || editMode == EditionMode.Arpeggio || editMode == EditionMode.DPCM);
        public bool IsActiveControl => App != null && App.ActiveControl == this;

        public static int DefaultPianoKeyWidth => DefaultNoteSizeY;

        public Instrument EditInstrument   => editInstrument;
        public Arpeggio   EditArpeggio     => editArpeggio;
        public DPCMSample EditSample       => editSample;
        public int        EditEnvelopeType => editEnvelope;

        public delegate void EmptyDelegate();
        public delegate void InstrumentEnvDelegate(Instrument instrument, int env);
        public delegate void PatternDelegate(Pattern pattern);
        public delegate void NoteDelegate(Note note);
        public delegate void DPCMMappingDelegate(int note);

        public event PatternDelegate       PatternChanged;
        public event EmptyDelegate         MaximizedChanged;
        public event EmptyDelegate         ManyPatternChanged;
        public event EmptyDelegate         DPCMSampleChanged;
        public event EmptyDelegate         NotesPasted;
        public event EmptyDelegate         ScrollChanged;
        public event NoteDelegate          NoteEyedropped;
        public event InstrumentEnvDelegate EnvelopeChanged;
        public event DPCMMappingDelegate   DPCMSampleMapped;
        public event DPCMMappingDelegate   DPCMSampleUnmapped;

        #region Localization

        // Piano Roll
        LocalizedString EditingChannelLabel;
        LocalizedString EditingDPCMSampleLabel;
        LocalizedString EditingArpeggioLabel;
        LocalizedString EditingInstrumentEnvelopeLabel;
        LocalizedString EditingInstrumentDPCMLabel;
        LocalizedString DPCMSourceDataLabel;
        LocalizedString DPCMProcessedDataLabel;
        LocalizedString DPCMPreviewPlaybackLabel;
        LocalizedString DPCMInstrumentUsageLabel;
        LocalizedString InstrumentNotSelectedLabel;
        LocalizedString ArpeggioOverriddenLabel;
        LocalizedString ArpeggioNotSelectedLabel;
        LocalizedString SelectedArpeggioWillBeHeardLabel;
        LocalizedString DPCMBankUsageLabel;
        LocalizedString RelativeEffectScalingLabel;
        LocalizedString EnvelopeRelativeLabel;
        LocalizedString EnvelopeAbsoluteLabel;
        LocalizedString HoldFingersToDrawMessage;
        LocalizedString HoldFingersToEraseMessage;


        // DPCM mapping editor
        LocalizedString PitchLabel;
        LocalizedString LoopingLabel;
        LocalizedString DMCInitialValueLabel;
        LocalizedString TransposeSampleMessage;
        LocalizedString TransposeSampleTitle;
        LocalizedString NoDPCMSampleMessage;
        LocalizedString NoDPCMSampleTitle;

        // DPCM mapping assignment
        LocalizedString AssignDPCMSampleTitle;
        LocalizedString SelectSampleToAssignLabel;
        LocalizedString LoopLabel;

        // DPCM mapping properties
        LocalizedString SampleMappingTitle;
        LocalizedString OverrideDMCInitialValueLabel;
        LocalizedString DMCInitialValueDiv2Label;

        // Paste messages
        LocalizedString PasteTitle;
        LocalizedString PasteMissingInstrumentsMessage;
        LocalizedString PasteMissingArpeggiosMessage;
        LocalizedString PasteMissingSamplesMessage;

        // Context menus
        LocalizedString DeleteSelectedNotesContext;
        LocalizedString DeleteNoteContext;
        LocalizedString ToggleNoteAttackContext;
        LocalizedString ToggleSelectedNoteAttackContext;
        LocalizedString ToggleSlideNoteContext;
        LocalizedString ToggleSelectedSlideNoteContext;
        LocalizedString ToggleReleaseContext;
        LocalizedString ToggleSelectedReleaseContext;
        LocalizedString MakeStopNoteContext;
        LocalizedString ReplaceAllInstrumentContext;
        LocalizedString ReplaceSpecificInstrumentContext;
        LocalizedString MakeInstrumentCurrentContext;
        LocalizedString SetSnapContext;
        LocalizedString SelectNoteRangeContext;
        LocalizedString ClearSelectionContext;
        LocalizedString SelectPatternContext;
        LocalizedString SelectAllContext;
        LocalizedString CopyEffectValuesAsEnvValuesContext;
        LocalizedString CopyEffectValuesAsTextContext;
        LocalizedString EnterEffectValueContext;
        LocalizedString ClearEffectValueContext;
        LocalizedString ClearSelectEffectValuesContext;
        LocalizedString ToggleVolumeSlideContext;
        LocalizedString AbsoluteEffectScalingContext;
        LocalizedString RelativeEffectScalingContext;
        LocalizedString AbsoluteValueScalingContext;
        LocalizedString AbsoluteValueScalingContextTooltip;
        LocalizedString RelativeValueScalingContext;
        LocalizedString RelativeValueScalingContextTooltip;
        LocalizedString SetLoopPointContext;
        LocalizedString ClearLoopPointContext;
        LocalizedString SetReleasePointContext;
        LocalizedString ClearReleasePointContext;
        LocalizedString FlattenSelectionContext;
        LocalizedString CopySelectedValuesAsTextContext;
        LocalizedString ResetVertexContext;
        LocalizedString ResetVolumeEnvelopeContext;
        LocalizedString DeleteSelectedSamplesContext;
        LocalizedString RemoveDPCMSampleContext;
        LocalizedString DPCMSamplePropertiesContext;
        LocalizedString SnapEnableContext;
        LocalizedString SnapEnableContextTooltip;
        LocalizedString SnapEffectsContext;
        LocalizedString SnapEffectsContextTooltip;
        LocalizedString SnapToBeatContext;
        LocalizedString SnapToBeatsContext;
        LocalizedString SnapToBeatContextTooltip;
        LocalizedString SnapToBeatsContextTooltip;

        // tooltips
        LocalizedString SeekTooltip;
        LocalizedString SelectTooltip;
        LocalizedString MoreOptionsTooltip;
        LocalizedString ResizeEnvelopeTooltip;
        LocalizedString SetLoopPointTooltip;
        LocalizedString SetReleasePointTooltip;
        LocalizedString MustHaveLoopPointTooltip;
        LocalizedString PlayPianoTooltip;
        LocalizedString PanTooltip;
        LocalizedString ToggleSnappingTooltip;
        LocalizedString ChangeSnapPrecisionTooltip;
        LocalizedString MaximizePianoRollTooltip;
        LocalizedString ShowHideEffectPanelTooltip;
        LocalizedString SelectEffectToEditTooltip;
        LocalizedString SetEffectValueTooltip;
        LocalizedString SetEffectValueFineTooltip;
        LocalizedString ClearEffectValueTooltip;
        LocalizedString OrTooltip;
        LocalizedString MoveVolEnvVertexTooltip;
        LocalizedString SelectSamplesFromSourceTooltip;
        LocalizedString DeleteSelectedSampleTooltip;
        LocalizedString ResizeNotesTooltip;
        LocalizedString MoveReleasePointTooltip;
        LocalizedString MoveNotesTooltip;
        LocalizedString CreateNoteTooltip;
        LocalizedString SlideNoteTooltip;
        LocalizedString ToggleAttackTooltip;
        LocalizedString InstrumentEyedropTooltip;
        LocalizedString SetNoteInstrumentTooltip;
        LocalizedString DeleteNoteTooltip;
        LocalizedString AddStopNoteTooltip;
        LocalizedString SetEnvelopeValueTooltip;
        LocalizedString AssignDPCMSampleTooltip;
        LocalizedString SamplePropertiesTooltip;

        // Bottom-right tooltips
        LocalizedString SamplesSelectedTooltip;
        LocalizedString ValuesSelectedTooltip;
        LocalizedString FramesSelectedTooltip;
        LocalizedString ArpeggioTooltip;

        #endregion

        public PianoRoll()
        {
            Localization.Localize(this);
            SetTickEnabled(true);
            supportsLongPress = true;
            supportsDoubleClick = true;
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

            headerSizeY               = DpiScaling.ScaleForWindow(DefaultHeaderSizeY * headerScale);
            effectButtonSizeY         = DpiScaling.ScaleForWindow(DefaultEffectButtonSizeY * effectIconsScale);
            noteSizeX                 = DpiScaling.ScaleForWindowFloat(DefaultNoteSizeX * zoom);
            noteSizeY                 = DpiScaling.ScaleForWindow(DefaultNoteSizeY * zoomY);
            noteAttackSizeX           = DpiScaling.ScaleForWindow(DefaultNoteAttackSizeX);
            releaseNoteSizeY          = DpiScaling.ScaleForWindow(DefaultReleaseNoteSizeY * zoomY) & 0xfe; // Keep even
            pianoSizeX                = DpiScaling.ScaleForWindow((videoMode || Platform.IsDesktop ? DefaultPianoSizeX    : DefaultPianoSizeXMobile)    * pianoScaleX);
            blackKeySizeX             = DpiScaling.ScaleForWindow((videoMode || Platform.IsDesktop ? DefaultBlackKeySizeX : DefaultBlackKeySizeXMobile) * pianoScaleX);
            whiteKeySizeY             = DpiScaling.ScaleForWindow(DefaultWhiteKeySizeY * zoomY);
            blackKeySizeY             = DpiScaling.ScaleForWindow(DefaultBlackKeySizeY * zoomY);
            effectIconPosX            = DpiScaling.ScaleForWindow(DefaultEffectIconPosX * effectIconsScale);
            effectIconPosY            = DpiScaling.ScaleForWindow(DefaultEffectIconPosY * effectIconsScale);
            headerIconsPosX           = DpiScaling.ScaleForWindow(headerScale == 1 ? DefaultSnapIconDpcmPosX : DefaultSnapIconPosX);
            headerIconsPosY           = DpiScaling.ScaleForWindow(headerScale == 1 ? DefaultSnapIconDpcmPosY : DefaultSnapIconPosY);
            effectNamePosX            = DpiScaling.ScaleForWindow(DefaultEffectNamePosX * effectIconsScale);
            beatTextPosX              = DpiScaling.ScaleForWindow(DefaultBeatTextPosX);
            effectValuePosTextOffsetY = DpiScaling.ScaleForFont(DefaultEffectValuePosTextOffsetY);
            effectValueNegTextOffsetY = DpiScaling.ScaleForFont(DefaultEffectValueNegTextOffsetY);
            bigTextPosX               = DpiScaling.ScaleForFont(DefaultBigTextPosX);
            bigTextPosY               = DpiScaling.ScaleForFont(DefaultBigTextPosY);
            tooltipTextPosX           = DpiScaling.ScaleForFont(DefaultTooltipTextPosX);
            tooltipTextPosY           = DpiScaling.ScaleForFont(DefaultTooltipTextPosY);
            effectPanelTextPosX       = DpiScaling.ScaleForFont(DefaultEffectPanelTextPosX);
            effectPanelTextPosY       = DpiScaling.ScaleForFont(DefaultEffectPanelTextPosY);
            dpcmTextPosX              = DpiScaling.ScaleForFont(DefaultDPCMTextPosX);
            recordingKeyOffsetY       = DpiScaling.ScaleForWindow(DefaultRecordingKeyOffsetY);
            attackIconPosX            = DpiScaling.ScaleForWindow(DefaultAttackIconPosX);
            waveGeometrySampleSize    = DpiScaling.ScaleForWindow(DefaultWaveGeometrySampleSize);
            waveDisplayPaddingY       = DpiScaling.ScaleForWindow(DefaultWaveDisplayPaddingY);
            scrollBarThickness        = DpiScaling.ScaleForWindow(scrollBarSize);
            minScrollBarLength        = DpiScaling.ScaleForWindow(DefaultMinScrollBarLength);
            noteResizeMargin          = DpiScaling.ScaleForWindow(DefaultNoteResizeMargin);
            minPixelDistForLines      = DpiScaling.ScaleForWindow(DefaultMinPixelDistForLines);
            envelopeValueSizeY        = DpiScaling.ScaleForWindowFloat(DefaultEnvelopeSizeY * envelopeValueZoom);
            gizmoSize                 = DpiScaling.ScaleForWindow(DefaultGizmoSize);
            scrollMargin              = (width - pianoSizeX) / 8;

            // Make sure the effect panel actually fit on screen on mobile.
            if (Platform.IsMobile && ParentWindow != null)
                effectPanelSizeY = height / 2 - headerSizeY;
            else
                effectPanelSizeY = DpiScaling.ScaleForWindow(DefaultEffectPanelSizeY);

            octaveSizeY = 12 * noteSizeY;
            headerAndEffectSizeY = headerSizeY + (showEffectsPanel ? effectPanelSizeY : 0);
            virtualSizeY = NumNotes * noteSizeY;
            pianoVisible = editMode == EditionMode.Channel || editMode == EditionMode.DPCMMapping || editMode == EditionMode.VideoRecording || Platform.IsDesktop;

            if (Platform.IsMobile && (editMode == EditionMode.Arpeggio || editMode == EditionMode.Envelope))
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

            editMode = EditionMode.Envelope;
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

        public void StartEditDPCMMapping(Instrument instrument)
        {
            SaveChannelScroll();

            editMode = EditionMode.DPCMMapping;
            editInstrument = instrument;
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

        public void StartVideoRecording(Song song, float videoZoom, float pianoRollScaleX, float pianoRollScaleY, int[] transpose)
        {
            Debug.Assert(transpose == null || transpose.Length == song.Channels.Length);

            editChannel = 0;
            editMode = EditionMode.VideoRecording;
            videoSong = song;
            zoom = videoZoom;
            pianoScaleX = pianoRollScaleX;
            zoomY = pianoRollScaleY;
            videoChannelTranspose = transpose;

            UpdateRenderCoords();
        }

        public void EndVideoRecording()
        {
            videoForceDisplayChannelMask = 0;
            videoChannelTranspose = null;
        }

        public void ApplySettings()
        {
            snapResolution = Settings.SnapResolution;
            snap = Settings.SnapEnabled;
            snapEffects = Settings.SnapEnabled;
        }

        public void SaveSettings()
        {
            Settings.SnapResolution = snapResolution;
            Settings.SnapEnabled = snap;
            Settings.SnapEffects = snapEffects;
        }

        public void SaveChannelScroll()
        {
            if (editMode == EditionMode.Channel)
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
            else if (Platform.IsDesktop && lastChannelZoom > 0)
            {
                // Intentionally not returning true so that we still center the scroll.
                zoom = lastChannelZoom;
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
            UpdateRenderCoords(); // To update noteSizeX
        }

        private void CenterEnvelopeScroll()
        {
            if (editMode == EditionMode.Arpeggio)
                CenterEnvelopeScroll(editArpeggio.Envelope, EnvelopeType.Arpeggio);
            else if (editMode == EditionMode.Envelope)
                CenterEnvelopeScroll(editInstrument.Envelopes[editEnvelope], editEnvelope, editInstrument);
        }

        private void CenterEnvelopeScroll(Envelope envelope, int envelopeType, Instrument instrument = null)
        {
            var baseNoteSizeX = DpiScaling.ScaleForWindow(DefaultNoteSizeX);
            var envelopeLength = Math.Max(4, envelope.Length);

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
            var midIdx = Note.MusicalNoteC4;

            if (editInstrument.GetMinMaxMappedSampleIndex(out var minIdx, out var maxIdx))
                midIdx = (minIdx + maxIdx) / 2;

            var noteY = virtualSizeY - midIdx * noteSizeY;
            scrollY = noteY - (Height - headerAndEffectSizeY) / 2;
        }

        private int GetPixelXForAbsoluteNoteIndex(int n, bool scroll = true)
        {
            // On PC, all math noteSizeX are always integer, but on mobile, they 
            // can be float. We need to cast into double since at the maximum zoom,
            // in a *very* long song, we are hitting the precision limit of floats.
            var x = (int)(n * (double)noteSizeX);
            if (scroll)
                x -= scrollX;
            return x;
        }

        private int GetAbsoluteNoteIndexForPixelX(int x, bool scroll = true)
        {
            if (scroll)
                x += scrollX;
            return (int)(x / (double)noteSizeX);
        }

        private int GetPixelYForNoteValue(int note)
        {
            Debug.Assert(Note.IsMusicalNote(note) || editMode == EditionMode.VideoRecording);
            return virtualSizeY - note * noteSizeY - scrollY;
        }

        private void CenterScroll(int patternIdx = 0)
        {
            var maxScrollY = Math.Max(virtualSizeY + headerAndEffectSizeY - Height, 0);

            scrollX = GetPixelXForAbsoluteNoteIndex(Song.GetPatternStartAbsoluteNoteIndex(patternIdx), false);
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
                if (editMode == EditionMode.Envelope)
                    return editInstrument?.Envelopes[(int)editEnvelope];
                else if (editMode == EditionMode.Arpeggio)
                    return editArpeggio.Envelope;
                else
                    return null;
            }
        }

        private Envelope EditRepeatEnvelope
        {
            get
            {
                if (editMode == EditionMode.Envelope && editInstrument != null && Instrument.EnvelopeHasRepeat(editEnvelope))
                    return editInstrument.Envelopes[EnvelopeType.WaveformRepeat];
                return null;
            }
        }

        private bool HasRepeatEnvelope()
        {
            return EditRepeatEnvelope != null;
        }

        public void HighlightPianoNote(int note)
        {
            SetAndMarkDirty(ref playHighlightNote, note);
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
                MarkDirty();
            }
        }

        protected override void OnAddedToContainer()
        {
            UpdateRenderCoords();

            var g = graphics;
            fontSmallCharSizeX = ParentWindow.Fonts.FontSmall.MeasureString("0", false);
            bmpLoopSmallFill = g.GetTextureAtlasRef("LoopSmallFill");
            bmpReleaseSmallFill = g.GetTextureAtlasRef("ReleaseSmallFill");
            bmpEnvResize = g.GetTextureAtlasRef("EnvResize");
            bmpExpandedSmall = g.GetTextureAtlasRef("ExpandedSmall");
            bmpCollapsedSmall = g.GetTextureAtlasRef("CollapsedSmall");
            bmpMaximize = g.GetTextureAtlasRef("Maximize");
            bmpSnap = g.GetTextureAtlasRef("Snap");
            bmpSnapOff = g.GetTextureAtlasRef("SnapOff");
            bmpGizmoResizeLeftRight = g.GetTextureAtlasRef("GizmoResizeLeftRight");
            bmpGizmoResizeUpDown = g.GetTextureAtlasRef("GizmoResizeUpDown");
            bmpGizmoResizeFill = g.GetTextureAtlasRef("GizmoResizeFill");
            bmpEffectFrame = g.GetTextureAtlasRef("EffectFrame");
            bmpEffectRepeat = g.GetTextureAtlasRef("EffectRepeat");
            bmpEffects = g.GetTextureAtlasRefs(EffectType.Icons);

            if (Platform.IsMobile)
            {
                bitmapScale = DpiScaling.ScaleForWindowFloat(0.5f);
                effectBitmapScale = DpiScaling.ScaleForWindowFloat(0.25f);
            }

            seekGeometry = new float[]
            {
                -headerSizeY / 4, 1,
                0, headerSizeY / 2 - 2,
                headerSizeY / 4, 1
            };

            sampleGeometry = new float[]
            {
                -waveGeometrySampleSize, -waveGeometrySampleSize,
                 waveGeometrySampleSize, -waveGeometrySampleSize,
                 waveGeometrySampleSize,  waveGeometrySampleSize,
                -waveGeometrySampleSize,  waveGeometrySampleSize
            };

            ConditionalUpdateNoteGeometries(g);
        }

        private void ConditionalUpdateNoteGeometries(Graphics g)
        {
            if (geometryNoteSizeY == noteSizeY)
                return;

            geometryNoteSizeY = noteSizeY;

            stopNoteGeometry[0] = new float[]
            {
                0.0f, 0,
                0.0f, noteSizeY,
                1.0f, noteSizeY / 2
            };

            stopNoteGeometry[1] = new float[]
            {
                0.0f, 1,
                0.0f, noteSizeY,
                1.0f, noteSizeY / 2
            };

            releaseNoteGeometry[0] = new float[]
            {
                0.0f, 0,
                0.0f, noteSizeY,
                1.0f, noteSizeY - noteSizeY / 2 + releaseNoteSizeY / 2,
                1.0f, noteSizeY / 2 - releaseNoteSizeY / 2
            };

            releaseNoteGeometry[1] = new float[]
            {
                0.0f, 1,
                0.0f, noteSizeY,
                1.0f, noteSizeY - noteSizeY / 2 + releaseNoteSizeY / 2,
                1.0f, noteSizeY / 2 - releaseNoteSizeY / 2 + 1
            };

            stopReleaseNoteGeometry[0] = new float[]
            {
                0.0f, noteSizeY / 2 - releaseNoteSizeY / 2,
                0.0f, noteSizeY / 2 + releaseNoteSizeY / 2,
                1.0f, noteSizeY / 2
            };

            stopReleaseNoteGeometry[1] = new float[]
            {
                0.0f, noteSizeY / 2 - releaseNoteSizeY / 2 + 1,
                0.0f, noteSizeY / 2 + releaseNoteSizeY / 2,
                1.0f, noteSizeY / 2
            };

            slideNoteGeometry = new float[]
            {
                0.0f, 0,
                1.0f, 0,
                1.0f, noteSizeY
            };

            if (Platform.IsMobile)
            {
                mobileEraseGeometry = new float[2 * 64];
                for (var i = 0; i < mobileEraseGeometry.Length / 2; i++)
                {
                    var angle = i / 64.0f * MathF.PI * 2.0f;
                    mobileEraseGeometry[i * 2 + 0] = MathF.Cos(angle) * noteSizeY * 1.5f;
                    mobileEraseGeometry[i * 2 + 1] = MathF.Sin(angle) * noteSizeY * 1.5f;
                }
            }
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
                minNoteIdx = Math.Max(GetAbsoluteNoteIndexForPixelX(0), 0);
                maxNoteIdx = Math.Min(GetAbsoluteNoteIndexForPixelX(Width - pianoSizeX) + 1, Song.GetPatternStartAbsoluteNoteIndex(Song.Length));
                channelIndex = editChannel;

                return true;
            }
            else
            {
                return false;
            }
        }

        private Color GetSeekBarColor()
        {
            if (editMode == EditionMode.Channel)
            {
                if (App.IsRecording)
                {
                    return Theme.DarkRedColor;
                }
                else if (App.IsSeeking)
                {
                    return Theme.Lighten(Theme.YellowColor, (int)(Math.Abs(Math.Sin(Platform.TimeSeconds() * 12.0)) * 75));
                }
            }

            return Theme.YellowColor;
        }

        private void ForEachWaveTimecode(RenderInfo r, Action<float, float, int, int> function)
        {
            var textSize  = r.g.MeasureString("99.999", r.fonts.FontMedium);
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

        public bool CanEnvelopeDisplayFrame()
        {
            return editEnvelope != EnvelopeType.FdsModulation && editEnvelope != EnvelopeType.WaveformRepeat;
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
            public Fonts fonts;
            public CommandList b;
            public CommandList c;
            public CommandList f;
        }

        private void RenderHeader(RenderInfo r)
        {
            r.c.PushTranslation(pianoSizeX, 0);
            r.c.PushClipRegion(0, 0, width - pianoSizeX, headerSizeY);

            if (Platform.IsDesktop && maximized)
                r.c.DrawLine(0, 0, width - pianoSizeX, 0, Color.Black);

            if ((editMode == EditionMode.Envelope || editMode == EditionMode.Arpeggio) && EditEnvelope != null)
            {
                var env = EditEnvelope;
                var rep = EditRepeatEnvelope;
                var iconPos = (headerSizeY / 2 - DpiScaling.ScaleCustom(bmpLoopSmallFill.ElementSize.Width, bitmapScale)) / 2;

                r.c.PushTranslation(0, headerSizeY / 2);

                if (env.ChunkLength > 1)
                    r.c.FillRectangle(0, 0, GetPixelXForAbsoluteNoteIndex(env.Length), headerSizeY / 2, editInstrument.Color);

                if (env.Loop >= 0)
                {
                    r.c.PushTranslation(GetPixelXForAbsoluteNoteIndex(env.Loop), 0);
                    r.b.FillRectangle(0, 0, GetPixelXForAbsoluteNoteIndex(((env.Release >= 0 ? env.Release : env.Length) - env.Loop), false), headerAndEffectSizeY, rep != null ? loopSectionColor : Theme.DarkGreyColor5);
                    r.b.DrawLine(0, 0, 0, headerAndEffectSizeY, Theme.BlackColor);
                    r.c.DrawTextureAtlas(bmpLoopSmallFill, iconPos + 1, iconPos, bitmapScale, rep != null ? Theme.BlackColor : Theme.LightGreyColor1);
                    r.c.PopTransform();
                }
                if (env.Release >= 0)
                {
                    r.c.PushTranslation(GetPixelXForAbsoluteNoteIndex(env.Release), 0);
                    r.b.DrawLine(0, 0, 0, headerAndEffectSizeY, Theme.BlackColor);
                    r.c.DrawTextureAtlas(bmpReleaseSmallFill, iconPos + 1, iconPos, bitmapScale, rep != null ? Theme.BlackColor : Theme.LightGreyColor1);
                    r.c.PopTransform();
                }
                if (env.Length > 0)
                {
                    r.c.PushTranslation(GetPixelXForAbsoluteNoteIndex(env.Length), 0);
                    r.b.DrawLine(0, 0, 0, headerAndEffectSizeY, Theme.BlackColor);
                    r.c.PopTransform();
                }

                r.c.PopTransform();

                if (env.CanResize)
                {
                    r.c.PushTranslation(GetPixelXForAbsoluteNoteIndex(env.Length), 0);
                    r.c.DrawTextureAtlas(bmpEnvResize, iconPos + 1, iconPos, bitmapScale, Theme.LightGreyColor1);
                    r.c.PopTransform();
                }

                if (hoverNoteIndex >= 0 && hoverNoteIndex < env.Length)
                {
                    var x0 = GetPixelXForAbsoluteNoteIndex(hoverNoteIndex + 0);
                    var x1 = GetPixelXForAbsoluteNoteIndex(hoverNoteIndex + 1);
                    r.c.PushTranslation(x0, 0);
                    r.c.FillRectangle(0, 0, x1 - x0, headerSizeY / 2, Theme.MediumGreyColor1);
                    r.c.PopTransform();
                }

                DrawSelectionRect(r.c, headerSizeY);

                // Draw the header bars
                for (int n = 0; n <= env.Length; n++)
                {
                    int x = GetPixelXForAbsoluteNoteIndex(n);
                    if (x != 0)
                    {
                        r.b.DrawLine(x, 0, x, headerSizeY / 2, Theme.BlackColor, env.ChunkLength > 1 && n % env.ChunkLength == 0 && n != env.Length ? 3 : 1);
                    }
                    if (n != env.Length)
                    {
                        if (env.ChunkLength > 1 && n % env.ChunkLength == 0)
                        {
                            if (x != 0)
                                r.c.DrawLine(x, headerSizeY / 2, x, headerSizeY, Theme.BlackColor, 3);
                            int x1 = GetPixelXForAbsoluteNoteIndex(n + env.ChunkLength);
                            r.c.DrawText((n / env.ChunkLength).ToString(), r.fonts.FontMedium, x, headerSizeY / 2 - 1, Theme.BlackColor, TextFlags.MiddleCenter, x1 - x, headerSizeY / 2);
                        }

                        var label = (editEnvelope == EnvelopeType.N163Waveform ? editInstrument.N163WavePos : 0) + (env.ChunkLength > 1 ? n % env.ChunkLength : n);
                        var labelString = label.ToString();
                        if (labelString.Length * fontSmallCharSizeX + 2 < noteSizeX)
                            r.c.DrawText(labelString, r.fonts.FontMedium, x, 0, Theme.LightGreyColor1, TextFlags.MiddleCenter, noteSizeX, headerSizeY / 2 - 1);
                    }
                }

                r.c.DrawLine(0, headerSizeY / 2 - 1, Width, headerSizeY / 2 - 1, Theme.BlackColor);
            }
            else if (editMode == EditionMode.Channel)
            {
                // Draw colored header
                for (int p = r.minVisiblePattern; p < r.maxVisiblePattern; p++)
                {
                    var pattern = Song.Channels[editChannel].PatternInstances[p];
                    if (pattern != null)
                    {
                        int sx = GetPixelXForAbsoluteNoteIndex(Song.GetPatternLength(p), false);
                        int px = GetPixelXForAbsoluteNoteIndex(Song.GetPatternStartAbsoluteNoteIndex(p), true);
                        r.c.FillRectangle(px, headerSizeY / 2, px + sx, headerSizeY, pattern.Color);
                    }
                }

                // Hover
                if (hoverNoteIndex >= 0 && hoverNoteIndex < Song.GetPatternStartAbsoluteNoteIndex(Song.Length))
                {
                    int x0 = GetPixelXForAbsoluteNoteIndex(hoverNoteIndex, true);
                    int x1 = GetPixelXForAbsoluteNoteIndex(hoverNoteIndex + hoverNoteCount, true);
                    r.c.FillRectangle(x0, 0, x1, headerSizeY / 2 - 1, Theme.MediumGreyColor1);
                }

                // Selection
                DrawSelectionRect(r.c, headerSizeY);

                var beatLabelSizeX = r.g.MeasureString("88.88", r.fonts.FontMedium);

                // Draw the header bars
                for (int p = r.minVisiblePattern; p < r.maxVisiblePattern; p++)
                {
                    var patternLen = Song.GetPatternLength(p);

                    var sx = GetPixelXForAbsoluteNoteIndex(patternLen, false);
                    var px = GetPixelXForAbsoluteNoteIndex(Song.GetPatternStartAbsoluteNoteIndex(p), true);
                    if (p != 0)
                        r.c.DrawLine(px, 0, px, headerSizeY, Theme.BlackColor, 3);

                    var pattern = Song.Channels[editChannel].PatternInstances[p];
                    var beatLen = Song.GetPatternBeatLength(p);
                    var beatSizeX = GetPixelXForAbsoluteNoteIndex(beatLen, false);

                    // Is there enough room to draw beat labels?
                    if ((beatSizeX + beatTextPosX) > beatLabelSizeX)
                    {
                        var numBeats = (int)Math.Ceiling(patternLen / (float)beatLen);
                        for (int i = 0; i < numBeats; i++)
                            r.c.DrawText($"{p + 1}.{i + 1}", r.fonts.FontMedium, px + beatTextPosX + beatSizeX * i, 0, Theme.LightGreyColor1, TextFlags.Middle, 0, headerSizeY / 2 - 1);
                    }
                    else
                    {
                        r.c.DrawText((p + 1).ToString(), r.fonts.FontMedium, px, 0, Theme.LightGreyColor1, TextFlags.MiddleCenter, sx, headerSizeY / 2 - 1);
                    }

                    if (pattern != null)
                        r.c.DrawText(pattern.Name, r.fonts.FontMedium, px, headerSizeY / 2, Theme.BlackColor, TextFlags.MiddleCenter | TextFlags.Clip, sx, headerSizeY / 2 - 1);
                }

                int maxX = GetPixelXForAbsoluteNoteIndex(Song.GetPatternStartAbsoluteNoteIndex(r.maxVisiblePattern));
                r.c.DrawLine(maxX, 0, maxX, Height, Theme.BlackColor, 3);
                r.c.DrawLine(0, headerSizeY / 2 - 1, Width, headerSizeY / 2 - 1, Theme.BlackColor);
            }
            else if (editMode == EditionMode.DPCM)
            {
                // Selection rectangle
                if (IsSelectionValid())
                {
                    r.c.FillRectangle(
                        GetPixelForWaveTime(GetWaveTimeForSample(selectionMin, true),  scrollX), 0,
                        GetPixelForWaveTime(GetWaveTimeForSample(selectionMax, false), scrollX), headerSizeY, selectionBgVisibleColor);
                }

                ForEachWaveTimecode(r, (time, x, level, idx) =>
                {
                    if (time != 0.0f)
                        r.c.DrawText(time.ToString($"F{level + 1}"), r.fonts.FontMedium, x - 100, 0, Theme.LightGreyColor1, TextFlags.MiddleCenter, 200, headerSizeY - 1);
                });

                // Processed Range
                r.c.FillRectangle(
                    GetPixelForWaveTime(editSample.ProcessedStartTime, scrollX), 0,
                    GetPixelForWaveTime(editSample.ProcessedEndTime,   scrollX), headerSizeY, Color.FromArgb(64, editSample.Color));
            }

            r.c.DrawLine(0, headerSizeY - 1, Width, headerSizeY - 1, Theme.BlackColor);

            if (((editMode == EditionMode.Envelope || editMode == EditionMode.Arpeggio) && CanEnvelopeDisplayFrame()) || (editMode == EditionMode.Channel))
            {
                var seekFrame = editMode == EditionMode.Envelope || editMode == EditionMode.Arpeggio ? App.GetEnvelopeFrame(editInstrument, editArpeggio, editEnvelope, editMode == EditionMode.Arpeggio) : GetSeekFrameToDraw();
                if (seekFrame >= 0)
                {
                    r.c.PushTranslation(GetPixelXForAbsoluteNoteIndex(seekFrame), 0);
                    r.c.FillAndDrawGeometry(seekGeometry, GetSeekBarColor(), Theme.BlackColor, 1, true);
                    r.c.DrawLine(0, headerSizeY / 2, 0, headerSizeY, GetSeekBarColor(), 3);
                    r.c.PopTransform();
                }
            }

            r.c.PopClipRegion();
            r.c.PopTransform();
        }

        private void RenderEffectList(RenderInfo r)
        {
            r.c.FillRectangle(0, 0, pianoSizeX, headerAndEffectSizeY, Theme.DarkGreyColor4);
            r.c.DrawLine(pianoSizeX - 1, 0, pianoSizeX - 1, headerAndEffectSizeY, Theme.BlackColor);

            if (Platform.IsDesktop && maximized)
                r.c.DrawLine(0, 0, pianoSizeX, 0, Color.Black);

            if (!Platform.IsMobile && editMode != EditionMode.VideoRecording)
            {
                var maxRect = GetMaximizeButtonRect();
                var maxOpacity = (hoverTopLeftButton & 4) != 0 ? 192 : 255;
                r.c.DrawTextureAtlas(bmpMaximize, maxRect.X, maxRect.Y, 1.0f, maximized ? Theme.LightGreyColor1 : Theme.MediumGreyColor1.Transparent(maxOpacity));
            }

            // Effect icons
            if (editMode == EditionMode.Channel)
            {
                var toggleRect = GetToggleEffectPanelButtonRect();
                var toggleOpacity = (hoverTopLeftButton & 1) != 0 ? 192 : 255;
                r.c.DrawTextureAtlas(showEffectsPanel ? bmpExpandedSmall : bmpCollapsedSmall, toggleRect.X, toggleRect.Y, bitmapScale, Theme.LightGreyColor1.Transparent(toggleOpacity));

                if (SnapAllowed && !Platform.IsMobile)
                {
                    var snapBtnRect = GetSnapButtonRect();
                    var snapResRect = GetSnapResolutionRect();
                    var snapOpacity = (hoverTopLeftButton & 2) != 0 ? 192 : 255;
                    var snapColor = App.IsRecording ? Theme.DarkRedColor : (SnapEnabled ? Theme.LightGreyColor1 : Theme.MediumGreyColor1);

                    r.c.DrawTextureAtlas(SnapEnabled || App.IsRecording ? bmpSnap : bmpSnapOff, snapBtnRect.X, snapBtnRect.Y, 1.0f, snapColor.Transparent(snapOpacity));
                    r.c.DrawText(SnapResolutionType.Names[snapResolution], r.fonts.FontSmall, snapResRect.X, snapResRect.Y, App.IsRecording ? Theme.DarkRedColor : (SnapEnabled ? Theme.LightGreyColor2 : Theme.MediumGreyColor1), TextFlags.Right | TextFlags.Middle, snapResRect.Width, snapResRect.Height);
                }

                if (showEffectsPanel)
                {
                    r.c.PushTranslation(0, headerSizeY);

                    int effectButtonY = 0;

                    for (int i = 0; i < supportedEffects.Length; i++)
                    {
                        var effectIdx = supportedEffects[i];

                        if (Platform.IsMobile && effectIdx != selectedEffectIdx)
                            continue;

                        r.c.PushTranslation(0, effectButtonY);
                        if (hoverEffectIndex == i)
                            r.c.FillRectangle(0, 0, pianoSizeX, effectButtonSizeY, Theme.MediumGreyColor1);
                        r.c.DrawLine(0, -1, pianoSizeX, -1, Theme.BlackColor);
                        r.c.DrawTextureAtlas(bmpEffects[effectIdx], effectIconPosX, effectIconPosY, effectBitmapScale, Theme.LightGreyColor1);
                        r.c.DrawText(EffectType.LocalizedNames[effectIdx], selectedEffectIdx == effectIdx ? r.fonts.FontSmallBold : r.fonts.FontSmall, effectNamePosX, 0, Theme.LightGreyColor2, TextFlags.Middle, 0, effectButtonSizeY);
                        r.c.PopTransform();

                        effectButtonY += effectButtonSizeY;
                    }

                    r.c.PushTranslation(0, effectButtonY);
                    r.c.DrawLine(0, -1, pianoSizeX, -1, Theme.BlackColor);
                    r.c.PopTransform();
                    r.c.PopTransform();
                }
            }
            else if (editMode == EditionMode.DPCM || editMode == EditionMode.Envelope && editInstrument.Envelopes[EnvelopeType.WaveformRepeat] != null)
            {
                r.c.DrawTextureAtlas(showEffectsPanel ? bmpExpandedSmall : bmpCollapsedSmall, 0, 0, bitmapScale, Theme.LightGreyColor1);

                if (showEffectsPanel)
                {
                    r.c.PushTranslation(0, headerSizeY);
                    r.c.DrawLine(0, -1, pianoSizeX, -1, Theme.BlackColor);

                    var bmp  = editMode == EditionMode.DPCM ? bmpEffects[Note.EffectVolume] : bmpEffectRepeat;
                    var text = editMode == EditionMode.DPCM ? EffectType.LocalizedNames[Note.EffectVolume] : EnvelopeType.LocalizedNames[EnvelopeType.WaveformRepeat];

                    r.c.DrawTextureAtlas(bmp, effectIconPosX, effectIconPosY, effectBitmapScale, Theme.LightGreyColor1);
                    r.c.DrawText(text, r.fonts.FontSmallBold, effectNamePosX, 0, Theme.LightGreyColor2, TextFlags.Middle, 0, effectButtonSizeY);
                    r.c.PushTranslation(0, effectButtonSizeY);
                    r.c.DrawLine(0, -1, pianoSizeX, -1, Theme.BlackColor);
                    r.c.PopTransform();
                    r.c.PopTransform();
                }
            }

            r.c.DrawLine(0, headerAndEffectSizeY - 1, pianoSizeX, headerAndEffectSizeY - 1, Theme.BlackColor);
        }

        private bool GetDPCMKeyColor(int note, out Color color)
        {
            if (editMode != EditionMode.VideoRecording && App.SelectedChannel.Type == ChannelType.Dpcm && App.SelectedInstrument != null)
            {
                var mapping = App.SelectedInstrument.GetDPCMMapping(note);
                if (mapping != null && mapping.Sample != null)
                {
                    color = Settings.DpcmColorMode == Settings.ColorModeSample ?
                        mapping.Sample.Color : App.SelectedInstrument.Color;
                    return true;
                }
            }

            color = Color.Invisible;
            return false;
        }

        private bool HighlightPianoNote(CommandList c, int note, Color color, bool whiteKey)
        {
            if (Note.IsMusicalNote(note) || editMode == EditionMode.VideoRecording)
            {
                Note.GetOctaveAndNote(note, out var octave, out var octaveNote);

                if (whiteKey == !IsBlackKey(octaveNote))
                    c.FillRectangle(GetKeyRectangle(octave, octaveNote), color);

                return true;
            }

            return false;
        }

        private void RenderPiano(RenderInfo r)
        {
            if (!pianoVisible)
                return;

            r.c.PushTranslation(0, headerAndEffectSizeY);
            r.c.PushClipRegion(0, 0, pianoSizeX, height - headerAndEffectSizeY - scrollBarThickness + 1);
            r.c.FillRectangleGradient(0, 0, pianoSizeX, Height, Theme.LightGreyColor1, Theme.LightGreyColor2, false, pianoSizeX);

            var drawDpcmColorKeys = (editMode == EditionMode.Channel && Song.Channels[editChannel].Type == ChannelType.Dpcm) || editMode == EditionMode.DPCMMapping;

            // Early pass for DPCM white keys.
            if (drawDpcmColorKeys)
            {
                for (int i = r.minVisibleOctave; i < r.maxVisibleOctave; i++)
                {
                    for (int j = 0; j < 12; j++)
                    {
                        if (!IsBlackKey(j) && GetDPCMKeyColor(i * 12 + j + 1, out var color))
                            r.c.FillRectangleGradient(GetKeyRectangle(i, j), Theme.Darken(color, 20), color, false, pianoSizeX);
                    }
                }
            }

            // Highlight play/hover note (white keys)
            if (videoHighlightKeys != null)
            {
                foreach (var pair in videoHighlightKeys)
                    HighlightPianoNote(r.c, pair.Item1, pair.Item2, true);
            }
            else
            {
                if (!HighlightPianoNote(r.c, playHighlightNote, whiteKeyPressedColor, true))
                    HighlightPianoNote(r.c, hoverPianoNote, whiteKeyHoverColor, true);
            }

            // Draw the piano
            for (int i = r.minVisibleOctave; i < r.maxVisibleOctave; i++)
            {
                var octaveBaseY = (virtualSizeY - octaveSizeY * i) - scrollY;

                for (int j = 0; j < 12; j++)
                {
                    var noteIdx = i * 12 + j;
                    if (noteIdx >= NumNotes && editMode != EditionMode.VideoRecording)
                        break;

                    if (IsBlackKey(j))
                    {
                        if (drawDpcmColorKeys && GetDPCMKeyColor(noteIdx + 1, out var color))
                            r.c.FillAndDrawRectangleGradient(GetKeyRectangle(i, j), Theme.Darken(color, 40), Theme.Darken(color, 20), Theme.BlackColor, false, blackKeySizeX);
                        else
                            r.c.FillRectangleGradient(GetKeyRectangle(i, j), Theme.DarkGreyColor4, Theme.DarkGreyColor5, false, blackKeySizeX);
                    }

                    int y = octaveBaseY - j * noteSizeY;
                    if (j == 0)
                        r.c.DrawLine(0, y, pianoSizeX, y, Theme.BlackColor);
                    else if (j == 5)
                        r.c.DrawLine(0, y, pianoSizeX, y, Theme.BlackColor);
                }

                if ((editMode == EditionMode.Channel || editMode == EditionMode.DPCMMapping) && r.fonts.FontSmall.Size < noteSizeY)
                    r.c.DrawText("C" + i, r.fonts.FontSmall, DpiScaling.Window, octaveBaseY - noteSizeY + 1, Theme.BlackColor, TextFlags.Middle, pianoSizeX - DpiScaling.Window * 2, noteSizeY);
            }

            // Highlight play/hover note (black keys)
            if (videoHighlightKeys != null)
            {
                foreach (var pair in videoHighlightKeys)
                    HighlightPianoNote(r.c, pair.Item1, pair.Item2, false);
            }
            else
            {
                if (!HighlightPianoNote(r.c, playHighlightNote, blackKeyPressedColor, false))
                    HighlightPianoNote(r.c, hoverPianoNote, blackKeyHoverColor, false);
            }

            // QWERTY key labels.
            if (App != null && (App.IsRecording || App.IsQwertyPianoEnabled) && Platform.IsDesktop)
            {
                for (int i = r.minVisibleOctave; i < r.maxVisibleOctave; i++)
                {
                    var octaveBaseY = (virtualSizeY - octaveSizeY * i) - scrollY;

                    for (int j = 0; j < 12; j++)
                    {
                        var noteIdx = i * 12 + j + 1;
                        var keyString = App.GetRecordingKeyString(noteIdx);

                        if (keyString == null)
                            continue;

                        int y = octaveBaseY - j * noteSizeY;

                        Color color;
                        if (App.IsRecording)
                            color = IsBlackKey(j) ? Theme.LightRedColor : Theme.DarkRedColor;
                        else
                            color = IsBlackKey(j) ? Theme.LightGreyColor2 : Theme.BlackColor;

                        r.c.DrawText(keyString, r.fonts.FontVerySmall, 0, y - recordingKeyOffsetY + 1, color, TextFlags.MiddleCenter, blackKeySizeX, noteSizeY - 1);
                    }
                }
            }

            r.c.DrawLine(pianoSizeX - 1, 0, pianoSizeX - 1, Height, Theme.BlackColor);
            r.c.DrawLine(0, height - headerAndEffectSizeY - scrollBarThickness, pianoSizeX, height - headerAndEffectSizeY - scrollBarThickness, Theme.BlackColor);
            r.c.PopClipRegion();
            r.c.PopTransform();
        }

        private int GetEffectValueForPixelY(int y, int min, int max, float exp = 1.0f)
        {
            var alpha = MathF.Pow(Utils.Saturate(y / (float)effectPanelSizeY), 1.0f / exp);
            return (int)MathF.Round(Utils.Lerp(min, max, alpha));
        }

        public int GetPixelYForEffectValue(int val, int min, int max, float exp = 1.0f)
        {
            return (max == min) ? effectPanelSizeY : (int)MathF.Floor(MathF.Pow((val - min) / (float)(max - min), exp) * effectPanelSizeY);
        }

        private int GetPixelYForEffectValue(int effect, int value)
        {
            var channel = Song.Channels[editChannel];
            var minValue = Note.GetEffectMinValue(Song, channel, effect);
            var maxValue = Note.GetEffectMaxValue(Song, channel, effect);
            return GetPixelYForEffectValue(value, minValue, maxValue);
        }

        private void RenderEffectPanel(RenderInfo r)
        {
            if ((editMode == EditionMode.Channel || editMode == EditionMode.DPCM || editMode == EditionMode.Envelope && HasRepeatEnvelope()) && showEffectsPanel)
            {
                r.c.PushTranslation(pianoSizeX, headerSizeY);
                r.c.PushClipRegion(0, 0, width - pianoSizeX - scrollBarThickness, effectPanelSizeY);

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
                        var exp = GetEffectValueExponent(maxValue);

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
                                    r.c.PushTranslation(GetPixelXForAbsoluteNoteIndex(location.ToAbsoluteNoteIndex(song)), 0);

                                    var frame = location.ToAbsoluteNoteIndex(song);

                                    if (lastSlide >= 0)
                                    {
                                        var X0 = GetPixelXForAbsoluteNoteIndex(lastFrame < 0 ? -1000000 : lastFrame - frame, false);
                                        var X1 = GetPixelXForAbsoluteNoteIndex(-frame + lastFrame + lastSlideDuration, false);
                                        var sizeY0 = GetPixelYForEffectValue(lastValue, 0, Note.VolumeMax);
                                        var sizeY1 = GetPixelYForEffectValue(lastSlide, 0, Note.VolumeMax);

                                        var points = new float[4 * 2]
                                        {
                                            X0, effectPanelSizeY - sizeY0,
                                            X0, effectPanelSizeY,
                                            X1, effectPanelSizeY,
                                            X1, effectPanelSizeY - sizeY1
                                        };

                                        r.c.FillGeometry(points, Theme.DarkGreyColor5);

                                        if ((frame - lastFrame) == 1 && lastSlide < lastValue)
                                            singleFrameSlides.Add(NoteLocation.FromAbsoluteNoteIndex(song, lastFrame));

                                        if ((frame - lastFrame) > lastSlideDuration)
                                            r.c.FillRectangle(X1, effectPanelSizeY - sizeY1, 0, effectPanelSizeY, Theme.DarkGreyColor5);
                                    }
                                    else
                                    {
                                        var sizeY = GetPixelYForEffectValue(lastValue, 0, Note.VolumeMax);
                                        r.c.FillRectangle(GetPixelXForAbsoluteNoteIndex(lastFrame < 0 ? -1000000 : lastFrame - frame, false), effectPanelSizeY - sizeY, 0, effectPanelSizeY, Theme.DarkGreyColor5);
                                    }

                                    lastSlide = note.HasVolumeSlide ? note.VolumeSlideTarget : -1;
                                    lastValue = note.Volume;
                                    lastFrame = frame;

                                    if (lastSlide >= 0)
                                        lastSlideDuration = channel.GetVolumeSlideDuration(location);

                                    r.c.PopTransform();
                                }
                            }

                            r.c.PushTranslation(GetPixelXForAbsoluteNoteIndex(Math.Max(lastFrame, 0)), 0);

                            if (lastSlide >= 0)
                            {
                                var location = NoteLocation.FromAbsoluteNoteIndex(song, lastFrame);

                                var X0 = 0;
                                var X1 = GetPixelXForAbsoluteNoteIndex(lastSlideDuration, false);
                                var sizeY0 = GetPixelYForEffectValue(lastValue, 0, Note.VolumeMax);
                                var sizeY1 = GetPixelYForEffectValue(lastSlide, 0, Note.VolumeMax);

                                var points = new float[4 * 2]
                                {
                                    X0, effectPanelSizeY - sizeY0,
                                    X0, effectPanelSizeY,
                                    X1, effectPanelSizeY,
                                    X1, effectPanelSizeY - sizeY1
                                };

                                r.c.FillGeometry(points, Theme.DarkGreyColor5);

                                if (lastSlideDuration == 1 && lastSlide < lastValue)
                                    singleFrameSlides.Add(location);

                                var endLocation = location.Advance(song, lastSlideDuration);
                                if (endLocation.IsInSong(song))
                                {
                                    var lastNote = channel.GetNoteAt(endLocation);
                                    if (lastNote == null || !lastNote.HasVolume)
                                        r.c.FillRectangle(X1, effectPanelSizeY - sizeY1, GetPixelXForAbsoluteNoteIndex(1000000, false), effectPanelSizeY, Theme.DarkGreyColor5);
                                }
                            }
                            else
                            {
                                var lastSizeY = GetPixelYForEffectValue(lastValue, 0, Note.VolumeMax);
                                r.c.FillRectangle(0, effectPanelSizeY - lastSizeY, GetPixelXForAbsoluteNoteIndex(1000000, false), effectPanelSizeY, Theme.DarkGreyColor5);
                            }

                            r.c.PopTransform();
                        }
                        else
                        {
                            for (var it = channel.GetSparseNoteIterator(minLocation, maxLocation, Note.GetFilterForEffect(selectedEffectIdx)); !it.Done; it.Next())
                            {
                                var note = it.Note;
                                var location = it.Location;

                                if (note.HasValidEffectValue(selectedEffectIdx))
                                {
                                    r.c.PushTranslation(GetPixelXForAbsoluteNoteIndex(location.ToAbsoluteNoteIndex(song)), 0);

                                    var frame = location.ToAbsoluteNoteIndex(song);
                                    var sizeY = GetPixelYForEffectValue(lastValue, minValue, maxValue, exp);
                                    r.c.FillRectangle(GetPixelXForAbsoluteNoteIndex(lastFrame < 0 ? -1000000 : lastFrame - frame, false), effectPanelSizeY - sizeY, 0, effectPanelSizeY, Theme.DarkGreyColor5);
                                    lastValue = note.GetEffectValue(selectedEffectIdx);
                                    lastFrame = frame;

                                    r.c.PopTransform();
                                }
                            }

                            var lastSizeY = GetPixelYForEffectValue(lastValue, minValue, maxValue, exp);
                            r.c.PushTranslation(Math.Max(0, GetPixelXForAbsoluteNoteIndex(lastFrame)), 0);
                            r.c.FillRectangle(0, effectPanelSizeY - lastSizeY, Width, effectPanelSizeY, Theme.DarkGreyColor5);
                            r.c.PopTransform();
                        }
                    }

                    DrawSelectionRect(r.c, effectPanelSizeY);

                    var highlightLocation = NoteLocation.Invalid;

                    if (Platform.IsMobile || highlightNoteAbsIndex >= 0 && captureOperation == CaptureOperation.ChangeEffectValue)
                    {
                        highlightLocation = NoteLocation.FromAbsoluteNoteIndex(song, SnapEnabled && SnapEffectEnabled ? SnapNote(highlightNoteAbsIndex) : highlightNoteAbsIndex);
                    }
                    else if (Platform.IsDesktop && captureOperation == CaptureOperation.None)
                    {
                        var pt = ScreenToControl(CursorPosition);
                        GetEffectNoteForCoord(pt.X, pt.Y, out highlightLocation);
                    }

                    // Draw the actual effect bars.
                    for (var it = channel.GetSparseNoteIterator(minLocation, maxLocation, NoteFilter.All); !it.Done; it.Next())
                    {
                        var note = it.Note;
                        var location = it.Location;

                        if (selectedEffectIdx >= 0 && note.HasValidEffectValue(selectedEffectIdx))
                        {
                            var effectValue = note.GetEffectValue(selectedEffectIdx);
                            var minValue = Note.GetEffectMinValue(song, channel, selectedEffectIdx);
                            var maxValue = Note.GetEffectMaxValue(song, channel, selectedEffectIdx);
                            var exp = GetEffectValueExponent(maxValue);
                            var sizeY = GetPixelYForEffectValue(effectValue, minValue, maxValue, exp);

                            r.c.PushTranslation(GetPixelXForAbsoluteNoteIndex(location.ToAbsoluteNoteIndex(song)), 0);

                            if (!Note.EffectWantsPreviousValue(selectedEffectIdx))
                                r.c.FillRectangle(0, 0, noteSizeX, effectPanelSizeY, Theme.DarkGreyColor5);

                            var highlighted = location == highlightLocation;
                            var selected = IsNoteSelected(location);
                            
                            r.c.FillRectangle(0, effectPanelSizeY - sizeY, noteSizeX, effectPanelSizeY, singleFrameSlides.Contains(location) ? volumeSlideBarFillColor : Theme.LightGreyColor1);

                            if (highlighted || selected)
                                r.c.DrawRectangle(0, effectPanelSizeY - sizeY, noteSizeX, effectPanelSizeY, highlighted ? Theme.WhiteColor : Theme.BlackColor, 3, true, true);
                            else
                                r.c.DrawRectangle(0, effectPanelSizeY - sizeY, noteSizeX, effectPanelSizeY, Theme.BlackColor);

                            var text = effectValue.ToString();
                            if (text.Length * fontSmallCharSizeX + 2 < noteSizeX)
                            {
                                if (sizeY < effectPanelSizeY / 2)
                                    r.c.DrawText(text, r.fonts.FontSmall, 0, effectPanelSizeY - sizeY - effectValuePosTextOffsetY, Theme.LightGreyColor1, TextFlags.Center, noteSizeX);
                                else
                                    r.c.DrawText(text, r.fonts.FontSmall, 0, effectPanelSizeY - sizeY + effectValueNegTextOffsetY, Theme.BlackColor, TextFlags.Center, noteSizeX);
                            }

                            r.c.PopTransform();
                        }
                    }

                    // Thick vertical bars
                    for (int p = r.minVisiblePattern; p < r.maxVisiblePattern; p++)
                    {
                        int x = GetPixelXForAbsoluteNoteIndex(Song.GetPatternStartAbsoluteNoteIndex(p));
                        if (p != 0) r.c.DrawLine(x, 0, x, Height, Theme.BlackColor, 3);
                    }

                    int maxX = GetPixelXForAbsoluteNoteIndex(Song.GetPatternStartAbsoluteNoteIndex(r.maxVisiblePattern));
                    r.c.DrawLine(maxX, 0, maxX, Height, Theme.BlackColor, 3);

                    int seekX = GetPixelXForAbsoluteNoteIndex(GetSeekFrameToDraw());
                    r.c.DrawLine(seekX, 0, seekX, effectPanelSizeY, GetSeekBarColor(), 3);

                    var gizmos = GetEffectGizmos(out _, out _);
                    if (gizmos != null)
                    {
                        foreach (var g in gizmos)
                        {
                            var lineColor = IsGizmoHighlighted(g, headerSizeY) ? Color.White : Color.Black;

                            if (g.FillImage != null)
                                r.c.DrawTextureAtlas(g.FillImage, g.Rect.X, g.Rect.Y, g.Rect.Width / (float)g.Image.ElementSize.Width, Theme.LightGreyColor1);
                            r.c.DrawTextureAtlas(g.Image, g.Rect.X, g.Rect.Y, g.Rect.Width / (float)g.Image.ElementSize.Width, lineColor);
                        }
                    }

                    if (relativeEffectScaling && IsSelectionValid())
                    {
                        r.c.DrawText(RelativeEffectScalingLabel, fonts.FontSmall, effectPanelTextPosX, effectPanelTextPosY, Theme.LightRedColor);
                    }
                }
                else if (editMode == EditionMode.Envelope && HasRepeatEnvelope())
                {
                    var env = EditEnvelope;
                    var rep = EditRepeatEnvelope;

                    if (IsSelectionValid())
                    {
                        r.c.FillRectangle(
                            GetPixelXForAbsoluteNoteIndex(selectionMin + 0) + 1, 0,
                            GetPixelXForAbsoluteNoteIndex(selectionMax + 1), height, IsActiveControl ? selectionBgVisibleColor : selectionBgInvisibleColor);
                    }

                    var highlightIndex = -1;

                    if ((Platform.IsMobile && highlightRepeatEnvelope) || captureOperation == CaptureOperation.ChangeEnvelopeRepeatValue)
                    {
                        highlightIndex = highlightNoteAbsIndex;
                    }
                    else if (Platform.IsDesktop && captureOperation == CaptureOperation.None)
                    {
                        var pt = ScreenToControl(CursorPosition);
                        if (IsPointInEffectPanel(pt.X, pt.Y))
                        {
                            GetEnvelopeValueForCoord(pt.X, pt.Y, out highlightIndex, out _);
                            if (highlightIndex >= 0)
                                highlightIndex /= env.ChunkLength;
                        }
                    }

                    Debug.Assert(env.Length % rep.Length == 0);
                    Debug.Assert(env.ChunkCount == rep.Length);

                    var ratio = env.Length / rep.Length;
                    
                    Envelope.GetMinMaxValueForType(editInstrument, EnvelopeType.WaveformRepeat, out var minRepeat, out var maxRepeat);

                    for (var i = 0; i < rep.Length; i++)
                    {
                        var x0 = GetPixelXForAbsoluteNoteIndex((i + 0) * ratio);
                        var x1 = GetPixelXForAbsoluteNoteIndex((i + 1) * ratio);
                        var sizeX = x1 - x0;
                        var val = rep.Values[i];
                        var sizeY = GetPixelYForEffectValue(val, minRepeat, maxRepeat);

                        r.c.PushTranslation(x0, 0);

                        var selected = IsEnvelopeRepeatValueSelected(i);
                        var highlighted = i == highlightIndex;

                        r.c.FillRectangle(0, effectPanelSizeY - sizeY, sizeX, effectPanelSizeY, editInstrument.Color);

                        if (highlighted || selected)
                            r.c.DrawRectangle(0, effectPanelSizeY - sizeY, sizeX, effectPanelSizeY, selected ? Theme.LightGreyColor1 : Theme.WhiteColor, 3, true, true);
                        else
                            r.c.DrawRectangle(0, effectPanelSizeY - sizeY, sizeX, effectPanelSizeY, Theme.BlackColor);

                        var text = val.ToString();
                        if (text.Length * fontSmallCharSizeX + 2 < sizeX)
                        {
                            if (sizeY < effectPanelSizeY / 2)
                                r.c.DrawText(text, r.fonts.FontSmall, 0, effectPanelSizeY - sizeY - effectValuePosTextOffsetY, Theme.LightGreyColor1, TextFlags.Center, sizeX);
                            else
                                r.c.DrawText(text, r.fonts.FontSmall, 0, effectPanelSizeY - sizeY + effectValueNegTextOffsetY, Theme.BlackColor, TextFlags.Center, sizeX);
                        }

                        r.c.PopTransform();
                        r.c.DrawLine(x1, 0, x1, effectPanelSizeY, Theme.BlackColor, 3);
                    }

                    var gizmos = GetEnvelopeEffectsGizmos();
                    if (gizmos != null)
                    {
                        foreach (var g in gizmos)
                        {
                            if (g.FillImage != null)
                                r.c.DrawTextureAtlas(g.FillImage, g.Rect.X, g.Rect.Y, g.Rect.Width / (float)g.Image.ElementSize.Width, EditInstrument.Color);
                            r.c.DrawTextureAtlas(g.Image, g.Rect.X, g.Rect.Y, g.Rect.Width / (float)g.Image.ElementSize.Width, Color.White);
                        }
                    }

                    // TODO : Create a function for this, this is a mess.
                    var seekFrame = App.GetEnvelopeFrame(editInstrument, editArpeggio, editEnvelope);
                    if (seekFrame >= 0)
                    {
                        var seekX = GetPixelXForAbsoluteNoteIndex(seekFrame);
                        r.c.DrawLine(seekX, 0, seekX, Height, GetSeekBarColor(), 3);
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
                        var points = new float[4 * 2]
                        {
                            envelopePoints[i + 1].X, envelopePoints[i + 1].Y,
                            envelopePoints[i + 0].X, envelopePoints[i + 0].Y,
                            envelopePoints[i + 0].X, effectPanelSizeY,
                            envelopePoints[i + 1].X, effectPanelSizeY
                        };

                        r.c.FillGeometry(points, Theme.DarkGreyColor4);
                    }

                    // Horizontal center line
                    r.c.DrawLine(0, halfPanelSizeY, Width, halfPanelSizeY, Theme.BlackColor);

                    // Top/bottom dash lines (limits);
                    var topY    = waveDisplayPaddingY;
                    var bottomY = effectPanelSizeY - waveDisplayPaddingY;
                    r.c.DrawLine(0, topY,    Width, topY, Theme.DarkGreyColor1, 1, false, true); 
                    r.c.DrawLine(0, bottomY, Width, bottomY, Theme.DarkGreyColor1, 1, false, true);

                    // Envelope line
                    for (int i = 0; i < 3; i++)
                    {
                        r.c.DrawLine(
                            envelopePoints[i + 0].X, 
                            envelopePoints[i + 0].Y,
                            envelopePoints[i + 1].X, 
                            envelopePoints[i + 1].Y, 
                            Theme.LightGreyColor1, 1, true);
                    }

                    // Envelope vertices.
                    for (int i = 0; i < 4; i++)
                    {
                        r.c.PushTransform(
                            envelopePoints[i + 0].X,
                            envelopePoints[i + 0].Y, 
                            1.0f, 1.0f);
                        r.c.FillGeometry(sampleGeometry, Theme.LightGreyColor1);
                        r.c.PopTransform();
                    }

                    // Selection rectangle
                    if (IsSelectionValid())
                    {
                        r.c.FillRectangle(
                            GetPixelForWaveTime(GetWaveTimeForSample(selectionMin, true),  scrollX), 0,
                            GetPixelForWaveTime(GetWaveTimeForSample(selectionMax, false), scrollX), Height, selectionBgVisibleColor);
                    }
                }

                r.c.DrawLine(0, effectPanelSizeY - 1, Width, effectPanelSizeY - 1, Theme.BlackColor);
                r.c.PopClipRegion();
                r.c.PopTransform();
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
            ClipboardUtils.SaveNotes(App.Project, GetSelectedNotes(false));
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
                            note.Instrument = newNote.Instrument != null && channel.SupportsInstrument(newNote.Instrument) ? newNote.Instrument : null;
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
                        else if (!note.IsValid)
                        {
                            // This will ensure the note is considered "Useless" and may therefore be deleted.
                            note.Instrument = null;
                            note.Duration = 0;
                            note.Release = 0;
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

        private void PasteNotes(bool pasteNotes, int pasteFxMask, bool mix, int repeat, ClipboardImportFlags instImportFlags, ClipboardImportFlags arpImportFlags, ClipboardImportFlags sampleImportFlags)
        {
            if (!IsSelectionValid())
                return;

            var createAnythingMissing =
                instImportFlags.HasFlag(ClipboardImportFlags.CreateMissing) ||
                arpImportFlags.HasFlag(ClipboardImportFlags.CreateMissing)  ||
                sampleImportFlags.HasFlag(ClipboardImportFlags.CreateMissing);

            App.UndoRedoManager.BeginTransaction(createAnythingMissing ? TransactionScope.Project : TransactionScope.Channel, Song.Id, editChannel);

            for (int i = 0; i < repeat && IsSelectionValid(); i++)
            {
                var notes = ClipboardUtils.LoadNotes(App.Project, instImportFlags, arpImportFlags, sampleImportFlags);

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
        
        private void PasteNotesWithConflictDialog(bool pasteNotes = true, int pasteFxMask = Note.EffectAllMask, bool mix = false, int repeat = 1)
        {
            if (!ClipboardUtils.GetClipboardContentFlags(Song, true, out var instFlags, out var arpFlags, out var sampleFlags, out var patternFlags))
            {
                return;
            }

            var anyConflicts =
                instFlags    != ClipboardContentFlags.None ||
                arpFlags     != ClipboardContentFlags.None ||
                sampleFlags  != ClipboardContentFlags.None ||
                patternFlags != ClipboardContentFlags.None;

            if (pasteNotes && anyConflicts)
            {
                var dlg = new PasteConflictDialog(window, instFlags, arpFlags, sampleFlags);
                dlg.ShowDialogAsync((r) =>
                {
                    if (r == DialogResult.OK)
                    {
                        PasteNotes(pasteNotes, pasteFxMask, mix, repeat, dlg.InstrumentFlags, dlg.ArpeggioFlags, dlg.DPCMSampleFlags);
                    }
                });
            }
            else
            {
                PasteNotes(pasteNotes, pasteFxMask, mix, repeat, ClipboardImportFlags.MatchByName, ClipboardImportFlags.MatchByName, ClipboardImportFlags.MatchByName);
            }
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

        private void CopyEffectValues(bool text)
        {
            if (editMode == EditionMode.Channel && selectedEffectIdx >= 0 && IsSelectionValid())
            {
                var channel = Song.Channels[editChannel];
                var location = NoteLocation.FromAbsoluteNoteIndex(Song, selectionMin);
                var wantsPreviousValue = Note.EffectWantsPreviousValue(selectedEffectIdx);
                var prevVal = wantsPreviousValue ? channel.GetLastEffectValue(location, selectedEffectIdx) : Note.GetEffectDefaultValue(Song, selectedEffectIdx);
                var selectedNotes = GetSelectedNotes(false);
                var values = new int[selectedNotes.Length];

                for (int i = 0; i < selectedNotes.Length; i++)
                {
                    var note = selectedNotes[i];
                    if (note != null && note.HasValidEffectValue(selectedEffectIdx))
                    {
                        var val = note.GetEffectValue(selectedEffectIdx);
                        values[i] = val;
                        if (wantsPreviousValue)
                            prevVal = val;
                    }
                    else
                    {
                        values[i] = prevVal;
                    }
                }

                if (text)
                {
                    Platform.SetClipboardString(string.Join(" ", values));
                }
                else
                {
                    ClipboardUtils.SaveEnvelopeValues(values.Select(v => (sbyte)Utils.Clamp(v, sbyte.MinValue, sbyte.MaxValue)).ToArray());
                }
            }
        }

        private void CopyEnvelopeValues(bool text)
        {
            var values = GetSelectedEnvelopeValues();

            if (text)
                Platform.SetClipboardString(string.Join(" ", values));
            else
                ClipboardUtils.SaveEnvelopeValues(values);
        }

        private void CutEnvelopeValues()
        {
            CopyEnvelopeValues(false);
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
            else if (editMode == EditionMode.Envelope || editMode == EditionMode.Arpeggio)
                CopyEnvelopeValues(false);
        }

        public void CopyAsText()
        {
            if (editMode == EditionMode.Envelope || editMode == EditionMode.Arpeggio)
                CopyEnvelopeValues(true);
        }

        public void Cut()
        {
            if (editMode == EditionMode.Channel)
                CutNotes();
            else if (editMode == EditionMode.Envelope || editMode == EditionMode.Arpeggio)
                CutEnvelopeValues();
        }

        public void Paste()
        {
            AbortCaptureOperation();

            if (editMode == EditionMode.Channel)
                PasteNotesWithConflictDialog();
            else if (editMode == EditionMode.Envelope || editMode == EditionMode.Arpeggio)
                PasteEnvelopeValues();
        }

        public void Delete()
        {
            if (editMode == EditionMode.Channel)
                DeleteSelectedNotes();
            else if (editMode == EditionMode.Envelope || editMode == EditionMode.Arpeggio)
                DeleteSelectedEnvelopeValues();
            else if (editMode == EditionMode.DPCM)
                DeleteSelectedWaveSection();
        }

        public void PasteSpecial()
        {
            if (editMode == EditionMode.Channel)
            {
                AbortCaptureOperation();

                var dlg = new PasteSpecialDialog(ParentWindow, Song.Channels[editChannel], lastPasteSpecialPasteMix, lastPasteSpecialPasteNotes, lastPasteSpecialPasteEffectMask);

                dlg.ShowDialogAsync((r) =>
                {
                    if (r == DialogResult.OK)
                    {
                        var effectMask = dlg.PasteEffectMask;

                        // Include volume slides if volume is selected.
                        if ((effectMask & Note.EffectVolumeMask) != 0 && Song.Channels[editChannel].SupportsEffect(Note.EffectVolumeSlide))
                            effectMask |= Note.EffectVolumeAndSlideMask;

                        PasteNotesWithConflictDialog(dlg.PasteNotes, effectMask, dlg.PasteMix, dlg.PasteRepeat);

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
            else if (editMode == EditionMode.Envelope || editMode == EditionMode.Arpeggio)
                DeleteSelectedEnvelopeValues();
        }

        public void DeleteSpecial()
        {
            if (editMode == EditionMode.Channel)
            {
                AbortCaptureOperation();

                var dlg = new DeleteSpecialDialog(ParentWindow, Song.Channels[editChannel]);

                dlg.ShowDialogAsync((r) =>
                {
                    if (r == DialogResult.OK)
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

        private bool GetRepeatEnvelopeSelectionMinMax(out int min, out int max)
        {
            min = -1;
            max = -1;

            var rep = EditRepeatEnvelope;
            var env = EditEnvelope;

            if (IsSelectionValid() && rep != null)
            {
                min = selectionMin / env.ChunkLength;
                max = selectionMax / env.ChunkLength;

                return true;
            }

            return false;
        }

        private bool IsEnvelopeRepeatValueSelected(int idx)
        {
            if (GetRepeatEnvelopeSelectionMinMax(out var min, out var max))
            {
                return idx >= min && idx <= max;
            }

            return false;
        }

        private void DrawSelectionRect(CommandList c, int height)
        {
            if (IsSelectionValid())
            {
                c.FillRectangle(
                    GetPixelXForAbsoluteNoteIndex(selectionMin + 0) + 1, 0,
                    GetPixelXForAbsoluteNoteIndex(selectionMax + 1), height, IsActiveControl ? selectionBgVisibleColor : selectionBgInvisibleColor);
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
                var pt = ScreenToControl(CursorPosition);

                if (g.Rect.Contains(pt.X - pianoSizeX, pt.Y - offsetY))
                    return true;

                if (g.Action == GizmoAction.MoveSlide && captureOperation == CaptureOperation.DragSlideNoteTargetGizmo ||
                    g.Action == GizmoAction.MoveVolumeSlideValue && captureOperation == CaptureOperation.DragVolumeSlideTargetGizmo)
                    return true;
            }
            return false;
        }

        private Color GetNoteColor(Channel channel, int noteValue, Instrument instrument, int alphaDim = 255)
        {
            var color = Theme.LightGreyColor1;

            if (channel.Type == ChannelType.Dpcm && Settings.DpcmColorMode == Settings.ColorModeSample && instrument != null)
            {
                var mapping = instrument.GetDPCMMapping(noteValue);
                if (mapping != null && mapping.Sample != null)
                    color = mapping.Sample.Color;
            }
            else if (instrument != null)
            {
                color = instrument.Color;
            }

            return Color.FromArgb(alphaDim, color);
        }

        private bool ShouldDrawLines(Song song, int patternIdx, int numNotes)
        {
            var x0 = GetPixelXForAbsoluteNoteIndex(song.GetPatternStartAbsoluteNoteIndex(patternIdx));
            var x1 = GetPixelXForAbsoluteNoteIndex(song.GetPatternStartAbsoluteNoteIndex(patternIdx) + numNotes);

            return (x1 - x0) >= minPixelDistForLines;
        }

        private void RenderNotes(RenderInfo r)
        {
            var song = Song;
            var maxX = editMode == EditionMode.Channel ? GetPixelXForAbsoluteNoteIndex(song.GetPatternStartAbsoluteNoteIndex(r.maxVisiblePattern)) : Width;
                                                           
            // Draw the note backgrounds
            for (int i = r.minVisibleOctave; i < r.maxVisibleOctave; i++)
            {
                int octaveBaseY = (virtualSizeY - octaveSizeY * i) - scrollY;
                for (int j = 0; j < 12; j++)
                {
                    int y = octaveBaseY - j * noteSizeY;
                    if (!IsBlackKey(j))
                        r.b.FillRectangle(0, y - noteSizeY, maxX, y, Theme.DarkGreyColor4);
                }
            }

            DrawSelectionRect(r.b, Height); 

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

                        // Needed to get dashed lines to scroll properly.
                        for (int i = p == 0 ? 1 : 0; i < patternLen; i++)
                        {
                            int x = GetPixelXForAbsoluteNoteIndex(song.GetPatternStartAbsoluteNoteIndex(p) + i);

                            if (i % beatLength == 0)
                                r.b.DrawLine(x, 0, x, Height, Theme.BlackColor, i == 0 ? 3 : 1);
                            else if (drawNotes && i % noteLength == 0)
                                r.b.DrawLine(x, 0, x, Height, Theme.DarkGreyColor1);
                            else if (drawFrames && editMode != EditionMode.VideoRecording)
                                r.b.DrawLine(x, -scrollY, x, virtualSizeY - scrollY, Theme.DarkGreyColor1, 1, false, true);
                        }
                    }
                    else
                    {
                        var drawNotes = ShouldDrawLines(song, p, 1);

                        for (int i = p == 0 ? 1 : 0; i < patternLen; i++)
                        {
                            int x = GetPixelXForAbsoluteNoteIndex(song.GetPatternStartAbsoluteNoteIndex(p) + i);

                            if (i % beatLength == 0)
                                r.b.DrawLine(x, 0, x, Height, Theme.BlackColor, i == 0 ? 3 : 1);
                            else if (drawNotes)
                                r.b.DrawLine(x, 0, x, Height, Theme.DarkGreyColor2);
                        }
                    }
                }

                // Horizontal black lines.
                for (int i = r.minVisibleOctave; i < r.maxVisibleOctave; i++)
                {
                    int octaveBaseY = (virtualSizeY - octaveSizeY * i) - scrollY;
                    for (int j = 0; j < 12; j++)
                    {
                        int y = octaveBaseY - j * noteSizeY;
                        if (i * 12 + j != NumNotes || editMode == EditionMode.VideoRecording)
                            r.b.DrawLine(0, y, maxX, y, Theme.BlackColor);
                    }
                }

                r.b.DrawLine(maxX, 0, maxX, Height, Theme.BlackColor, 3);

                if (editMode != EditionMode.VideoRecording)
                {
                    int seekX = GetPixelXForAbsoluteNoteIndex(GetSeekFrameToDraw());
                    r.c.DrawLine(seekX, 0, seekX, Height, GetSeekBarColor(), 3);
                }

                // Highlight note under mouse.
                var highlightNote = (Note)null;
                var highlightLocation = NoteLocation.Invalid;
                var highlightReleased = false;
                var highlightLastNoteValue = Note.NoteInvalid;
                var highlightLastInstrument = (Instrument)null;
                var highlightAttackState = NoteAttackState.Attack;

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
                    else if (!ParentWindow.IsAsyncDialogInProgress)
                    {
                        if (HasHighlightedNote() && CaptureOperationRequiresNoteHighlight(captureOperation))
                        {
                            highlightLocation = NoteLocation.FromAbsoluteNoteIndex(song, highlightNoteAbsIndex);
                            highlightNote = song.Channels[editChannel].GetNoteAt(highlightLocation);
                        }
                        else if (captureOperation == CaptureOperation.None)
                        {
                            var pt = ScreenToControl(CursorPosition);
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
                    var channelBitMask = 1L << c;
                    var isActiveChannel = c == editChannel || (videoForceDisplayChannelMask & channelBitMask) != 0;

                    if (isActiveChannel || (ghostChannelMask & channelBitMask) != 0)
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
                            var noteAttackState = NoteAttackState.Attack;

                            // Release notes are no longer supported in the piano roll. 
                            Debug.Assert(!note.IsRelease);

                            if (note.IsMusical)
                            {
                                // We'll display an empty attack when the user tries to disable in a 
                                // situation where its not supported.
                                noteAttackState = note.HasAttack ? 
                                    NoteAttackState.Attack : 
                                    Channel.CanDisableAttack(channel.Type, lastInstrument, note.Instrument) ?
                                        NoteAttackState.NoAttack :
                                        NoteAttackState.NoAttackError;

                                if (noteAttackState != NoteAttackState.NoAttack)
                                    released = false;

                                lastNoteValue  = note.Value;
                                lastInstrument = note.Instrument;
                            }

                            if (isActiveChannel && it.Location == highlightLocation)
                            {
                                highlightReleased = released;
                                highlightLastNoteValue = lastNoteValue;
                                highlightLastInstrument = lastInstrument;
                                highlightAttackState = noteAttackState;
                            }

                            if (note.IsMusical)
                            {
                                RenderNote(r, it.Location, note, song, c, it.DistanceToNextCut, drawImplicitStopNotes, isActiveChannel, false, released, noteAttackState);
                            }
                            else if (note.IsStop)
                            {
                                RenderNoteReleaseOrStop(r, note, GetNoteColor(channel, lastNoteValue, lastInstrument, isActiveChannel ? 255 : 50), it.Location.ToAbsoluteNoteIndex(Song), lastNoteValue, false, IsNoteSelected(it.Location, 1), isActiveChannel, true, released);
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
                                RenderNote(r, highlightLocation, highlightNote, song, c, channel.GetDistanceToNextNote(highlightLocation), drawImplicitStopNotes, true, true, highlightReleased, highlightAttackState);
                            }
                            else if (highlightNote.IsStop)
                            {
                                RenderNoteReleaseOrStop(r, highlightNote, GetNoteColor(channel, highlightLastNoteValue, highlightLastInstrument, isActiveChannel ? 255 : 50), highlightLocation.ToAbsoluteNoteIndex(Song), highlightLastNoteValue, true, false, true, true, highlightReleased);
                            }
                        }
                    }
                }

                // Draw effect icons at the top.
                if (editMode != EditionMode.VideoRecording)
                {
                    var effectIconSizeX = DpiScaling.ScaleCustom(bmpEffects[0].ElementSize.Width, effectBitmapScale);

                    var channel = song.Channels[editChannel];
                    for (int p = r.minVisiblePattern; p < r.maxVisiblePattern; p++)
                    {
                        var pattern = channel.PatternInstances[p];
                        
                        if (pattern == null)
                            continue;

                        var patternLen = song.GetPatternLength(p);

                        foreach (var kv in pattern.Notes)
                        {
                            var time = kv.Key;
                            var note = kv.Value;

                            if (time >= patternLen)
                                break;

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

                                        var iconX = GetPixelXForAbsoluteNoteIndex(channel.Song.GetPatternStartAbsoluteNoteIndex(p, time)) + (int)(noteSizeX / 2) - effectIconSizeX / 2;
                                        var iconY = effectPosY + effectIconPosY;

                                        r.f.DrawTextureAtlas(bmpEffectFrame, iconX, iconY, effectBitmapScale, drawOpaque ? Theme.LightGreyColor1 : Theme.MediumGreyColor1);
                                        r.f.DrawTextureAtlas(bmpEffects[fx], iconX, iconY, effectBitmapScale, Theme.LightGreyColor1.Transparent(drawOpaque ? 255 : 100));
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
                            var highlighted = IsGizmoHighlighted(g, headerAndEffectSizeY);
                            var fillColor = GetNoteColor(Song.Channels[editChannel], gizmoNote.Value, gizmoNote.Instrument);
                            var lineColor = highlighted ? Color.White : Color.Black;

                            if (g.FillImage != null)
                                r.f.DrawTextureAtlas(g.FillImage, g.Rect.X, g.Rect.Y, g.Rect.Width / (float)g.FillImage.ElementSize.Width, fillColor);
                            r.f.DrawTextureAtlas(g.Image, g.Rect.X, g.Rect.Y, g.Rect.Width / (float)g.FillImage.ElementSize.Width, lineColor);

                            if (highlighted && !string.IsNullOrEmpty(g.GizmoText))
                                r.f.DrawText(g.GizmoText, r.fonts.FontSmall, g.Rect.X - g.Rect.Width / 8, g.Rect.Y, Theme.WhiteColor, TextFlags.MiddleRight, 0, g.Rect.Height);
                        }
                    }

                    if (Platform.IsMobile && captureOperation == CaptureOperation.DeleteNotes)
                    {
                        var touchX = mouseLastX - pianoSizeX;
                        var touchY = mouseLastY - headerAndEffectSizeY;
                        r.f.PushTranslation(touchX, touchY);
                        r.f.FillGeometry(mobileEraseGeometry, new Color(Theme.LightGreyColor1.R, Theme.LightGreyColor1.G, Theme.LightGreyColor1.B, 128), false);
                        r.f.PopTransform();
                    }

                    var channelType = song.Channels[editChannel].Type;
                    var channelName = song.Channels[editChannel].NameWithExpansion;

                    r.f.DrawText(EditingChannelLabel.Format(channelName), r.fonts.FontVeryLarge, bigTextPosX, maxEffectPosY > 0 ? maxEffectPosY : bigTextPosY, Theme.LightGreyColor1);
                }
            }
            else if (App.Project != null) // Happens if DPCM panel is open and importing an NSF.
            {
                // Horizontal black lines.
                for (int i = r.minVisibleOctave; i < r.maxVisibleOctave; i++)
                {
                    int octaveBaseY = (virtualSizeY - octaveSizeY * i) - scrollY;
                    for (int j = 0; j < 12; j++)
                    {
                        int y = octaveBaseY - j * noteSizeY;
                        if (i * 12 + j != NumNotes)
                            r.b.DrawLine(0, y, maxX, y, Theme.BlackColor);
                    }
                }

                foreach (var kv in editInstrument.SamplesMapping)
                {
                    var note = kv.Key;
                    var mapping = kv.Value;
                    if (mapping != null && mapping.Sample != null)
                    {
                        var y = virtualSizeY - note * noteSizeY - scrollY;
                        var highlighted = note == highlightDPCMSample;

                        r.c.PushTranslation(0, y);
                        r.c.FillAndDrawRectangleGradient(0, 0, Width - pianoSizeX, noteSizeY, mapping.Sample.Color, mapping.Sample.Color.Scaled(0.8f), highlighted ? Theme.WhiteColor : Theme.BlackColor, true, noteSizeY, highlighted ? 3 : 1, highlighted, highlighted);

                        string text = $"{mapping.Sample.Name} - {PitchLabel}: {DPCMSampleRate.GetString(true, FamiStudio.StaticInstance.PalPlayback, true, true, mapping.Pitch)}";
                        if (mapping.Loop) text += $", {LoopingLabel}";
                        if (mapping.OverrideDmcInitialValue) text += $" , {DMCInitialValueLabel} = {mapping.DmcInitialValueDiv2}";

                        r.c.DrawText(text, r.fonts.FontSmall, dpcmTextPosX, 0, Theme.BlackColor, TextFlags.MiddleLeft, 0, noteSizeY);
                        r.c.PopTransform();
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
                    var pt = Platform.IsDesktop ? ScreenToControl(CursorPosition) : new Point(mouseLastX, mouseLastY);

                    if (GetNoteValueForCoord(pt.X, pt.Y, out var noteValue))
                    {
                        var y = virtualSizeY - noteValue * noteSizeY - scrollY;
                        r.c.PushTranslation(0, y);
                        r.c.FillAndDrawRectangleGradient(0, 0, Width - pianoSizeX, noteSizeY, dragSample.Color, dragSample.Color.Scaled(0.8f), Theme.WhiteColor, true, noteSizeY, 3, true, true);
                        r.c.PopTransform();
                    }
                }
                else if (Platform.IsDesktop && captureOperation == CaptureOperation.None)
                {
                    var pt = ScreenToControl(CursorPosition);

                    if (GetLocationForCoord(pt.X, pt.Y, out _, out var highlightNoteValue))
                    {
                        var mapping = editInstrument.GetDPCMMapping(highlightNoteValue);
                        if (mapping != null)
                        {
                            var y = virtualSizeY - highlightNoteValue * noteSizeY - scrollY;

                            r.c.PushTranslation(0, y);
                            r.c.DrawRectangle(0, 0, Width - pianoSizeX, noteSizeY, Theme.WhiteColor, 3, true, true);
                            r.c.PopTransform();
                        }
                    }
                }

                var textY = bigTextPosY;
                r.f.DrawText(EditingInstrumentDPCMLabel.Format(editInstrument.Name), r.fonts.FontVeryLarge, bigTextPosX, bigTextPosY, Theme.LightGreyColor1);
                textY += r.fonts.FontVeryLarge.LineHeight;
                r.f.DrawText(DPCMInstrumentUsageLabel.Format(editInstrument.GetTotalMappedSampleSize(), Project.MaxMappedSampleSize), r.fonts.FontMedium, bigTextPosX, textY, Theme.LightGreyColor1);
                textY += r.fonts.FontMedium.LineHeight;
                
                for (int i = 0; i < Project.MaxDPCMBanks; i++)
                {
                    var bankSize = App.Project.GetBankSize(i);
                    if (bankSize > 0)
                    {
                        r.f.DrawText(DPCMBankUsageLabel.Format(bankSize, i), r.fonts.FontMedium, bigTextPosX, textY, Theme.LightGreyColor1);
                        textY += r.fonts.FontMedium.LineHeight;
                    }
                }
            }
        }

        private void RenderEnvelopeValues(RenderInfo r)
        {
            var env = EditEnvelope;
            var resampled = editMode == EditionMode.Envelope && 
                           (editInstrument.IsN163 && editEnvelope == EnvelopeType.N163Waveform && editInstrument.N163ResampleWaveData != null && editInstrument.N163WavePreset == WavePresetType.Resample ||
                            editInstrument.IsFds  && editEnvelope == EnvelopeType.FdsWaveform  && editInstrument.FdsResampleWaveData  != null && editInstrument.FdsWavePreset  == WavePresetType.Resample);
            var spacing = editEnvelope == EnvelopeType.DutyCycle || editEnvelope == EnvelopeType.S5BMixer ? 4 : (editEnvelope == EnvelopeType.Arpeggio ? 12 : 16);
            var color = editMode == EditionMode.Envelope ? editInstrument.Color : editArpeggio.Color;
            var brush = Color.FromArgb(resampled ? 100 : 255, color);

            Envelope.GetMinMaxValueForType(editInstrument, editEnvelope, out int envTypeMinValue, out int envTypeMaxValue);

            // Draw the envelope value backgrounds.
            int maxValue = 128 / (int)envelopeValueZoom;
            int midValue =  64 / (int)envelopeValueZoom;

            var lastRectangleValue = int.MinValue;
            var lastRectangleY     = -1.0f;
            var oddRectangle       = false;

            var maxX = GetPixelXForAbsoluteNoteIndex(env.Length);
            var maxi = (Platform.IsDesktop ? maxValue : envTypeMaxValue - envTypeMinValue) + 1;

            // Background rectangles + labels
            for (int i = 0; i <= maxi; i++)
            {
                var value = Platform.IsMobile ? i + envTypeMinValue : i - midValue;
                var y = (virtualSizeY - envelopeValueSizeY * i) - scrollY;
                var drawLabel = i == maxi - 1;

                if ((value % spacing) == 0 || i == 0 || i == maxi)
                {
                    if (lastRectangleValue >= envTypeMinValue && lastRectangleValue <= envTypeMaxValue)
                    {
                        r.b.FillRectangle(0, lastRectangleY, maxX, y, oddRectangle ? Theme.DarkGreyColor5 : Theme.DarkGreyColor4);
                        oddRectangle = !oddRectangle;
                    }

                    lastRectangleValue = value;
                    lastRectangleY = y;
                    drawLabel |= value >= envTypeMinValue - 1 && value <= envTypeMaxValue + 1;
                }

                if (drawLabel)
                    r.b.DrawText(value.ToString(), r.fonts.FontSmall, maxX + 4 * DpiScaling.Window, y - envelopeValueSizeY, Theme.LightGreyColor1, TextFlags.MiddleLeft, 0, envelopeValueSizeY);
            }
            
            // Horizontal lines
            for (int i = 0; i <= maxi; i++)
            {
                var value = Platform.IsMobile ? i + envTypeMinValue : i - midValue;
                var y = (virtualSizeY - envelopeValueSizeY * i) - scrollY;

                if (i != maxi)
                    r.b.DrawLine(0, y, GetPixelXForAbsoluteNoteIndex(env.Length), y, Theme.DarkGreyColor1, (value % spacing) == 0 ? 3 : 1);
            }

            DrawSelectionRect(r.b, Height);

            // Draw the vertical bars.
            for (int b = 0; b < env.Length; b++)
            {
                int x = GetPixelXForAbsoluteNoteIndex(b);
                if (b != 0) r.b.DrawLine(x, 0, x, Height, Theme.DarkGreyColor1, env.ChunkLength > 1 && b % env.ChunkLength == 0 ? 3 : 1);
            }

            if (env.Loop >= 0)
                r.b.DrawLine(GetPixelXForAbsoluteNoteIndex(env.Loop), 0, GetPixelXForAbsoluteNoteIndex(env.Loop), Height, Theme.BlackColor);
            if (env.Release >= 0)
                r.b.DrawLine(GetPixelXForAbsoluteNoteIndex(env.Release), 0, GetPixelXForAbsoluteNoteIndex(env.Release), Height, Theme.BlackColor);
            if (env.Length > 0)
                r.b.DrawLine(GetPixelXForAbsoluteNoteIndex(env.Length), 0, GetPixelXForAbsoluteNoteIndex(env.Length), Height, Theme.BlackColor);

            if ((editMode == EditionMode.Envelope || editMode == EditionMode.Arpeggio) && CanEnvelopeDisplayFrame())
            {
                var seekFrame = App.GetEnvelopeFrame(editInstrument, editArpeggio, editEnvelope, editMode == EditionMode.Arpeggio);
                if (seekFrame >= 0)
                {
                    var seekX = GetPixelXForAbsoluteNoteIndex(seekFrame);
                    r.c.DrawLine(seekX, 0, seekX, Height, GetSeekBarColor(), 3);
                }
            }

            var highlightRect = RectangleF.Empty;
            var center = editEnvelope == EnvelopeType.FdsWaveform ? 32 : 0;
            var bias = Platform.IsMobile ? -envTypeMinValue : midValue;

            if (editEnvelope == EnvelopeType.Arpeggio)
            {
                for (int i = 0; i < env.Length; i++)
                {
                    var selected = IsEnvelopeValueSelected(i);
                    var highlighted = Platform.IsMobile && highlightNoteAbsIndex == i;

                    float x0 = GetPixelXForAbsoluteNoteIndex(i + 0);
                    float x1 = GetPixelXForAbsoluteNoteIndex(i + 1);
                    float y = (virtualSizeY - envelopeValueSizeY * (env.Values[i] + bias)) - scrollY;

                    r.c.FillRectangle(x0, y - envelopeValueSizeY, x1, y, brush);

                    if (!highlighted)
                        r.c.DrawRectangle(x0, y - envelopeValueSizeY, x1, y, selected ? Theme.LightGreyColor1 : Theme.BlackColor, selected ? 3 : 1, selected, selected);
                    else
                        highlightRect = new RectangleF(x0,y - envelopeValueSizeY, x1 - x0, envelopeValueSizeY);

                    var label = Envelope.GetDisplayValue(editInstrument, editEnvelope, env.Values[i]);
                    if (label.Length * fontSmallCharSizeX + 2 < noteSizeX)
                        r.f.DrawText(label, r.fonts.FontSmall, x0, y - envelopeValueSizeY - effectValuePosTextOffsetY, Theme.LightGreyColor1, TextFlags.Center, noteSizeX);
                }
            }
            else
            {
                for (int i = 0; i < env.Length; i++)
                {
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

                    var x0 = GetPixelXForAbsoluteNoteIndex(i + 0);
                    var x1 = GetPixelXForAbsoluteNoteIndex(i + 1);
                    var selected = IsEnvelopeValueSelected(i);
                    var highlighted = Platform.IsMobile && highlightNoteAbsIndex == i;

                    r.c.FillRectangle(x0, y0, x1, y1, brush);

                    if (!highlighted)
                        r.c.DrawRectangle(x0, y0, x1, y1, selected ? Theme.LightGreyColor1 : Theme.BlackColor, selected ? 3 : 1, selected, selected);
                    else
                        highlightRect = new RectangleF(x0, y0, x1 - x0, y1 - y0);

                    var label = Envelope.GetDisplayValue(editInstrument, editEnvelope, val);
                    if (label.Length * fontSmallCharSizeX + 2 < noteSizeX)
                    {
                        var drawOutside = Math.Abs(y1 - y0) < (DefaultEnvelopeSizeY * DpiScaling.Window * 2);
                        var textBrush = drawOutside ? Theme.LightGreyColor1 : Theme.BlackColor;
                        var offset = drawOutside != val < center ? -effectValuePosTextOffsetY : effectValueNegTextOffsetY;

                        r.f.DrawText(label, r.fonts.FontSmall, x0, ty + offset, textBrush, TextFlags.Center, noteSizeX);
                    }
                }
            }

            if (!highlightRect.IsEmpty)
                r.c.DrawRectangle(highlightRect, Theme.WhiteColor, 3, true, true);

            // Drawing the N163/FDS waveform on top. 
            if (resampled)
            {
                var isN163     = editInstrument.IsN163;

                var waveSize   = isN163 ? editInstrument.N163WaveSize : 64;
                var wavePeriod = isN163 ? editInstrument.N163ResampleWavePeriod : editInstrument.FdsResampleWavePeriod;
                var waveOffset = isN163 ? editInstrument.N163ResampleWaveOffset : editInstrument.FdsResampleWaveOffset;
                var waveData   = isN163 ? editInstrument.N163ResampleWaveData   : editInstrument.FdsResampleWaveData;

                var numSamplesPerEnvelopeValue  = wavePeriod / (float)waveSize;
                var numVerticesPerColumn = (int)(noteSizeX * 0.5f);

                Debug.Assert(numVerticesPerColumn >= 1);

                var line = new List<float>(width);
                var prevSampleIndex = -1;
                var prevX = 0.0f;
                var prevY = 0.0f;

                // Start at -1 to always draw the first little bit in the first 1/2 of the first value.
                for (var i = -1; i < env.Length; i++)
                {
                    var x0 = GetPixelXForAbsoluteNoteIndex(i + 0);
                    var x1 = GetPixelXForAbsoluteNoteIndex(i + 1);

                    for (var j = 0; j < numVerticesPerColumn; j++)
                    {
                        var sampleIndex = (int)Math.Floor(waveOffset + i * numSamplesPerEnvelopeValue + (j * numSamplesPerEnvelopeValue / numVerticesPerColumn));
                        if (sampleIndex >= 0 && sampleIndex != prevSampleIndex)
                        {
                            if (sampleIndex >= waveData.Length)
                            {
                                i = env.Length;
                                break;
                            }

                            var sample = waveData[sampleIndex];
                            var val = Utils.Lerp(envTypeMinValue, envTypeMaxValue + 1, (sample + 32768.0f) / 65535.0f);

                            var x = Utils.Lerp(x0, x1, j / (float)numVerticesPerColumn) + noteSizeX * 0.5f;
                            var y = (virtualSizeY - envelopeValueSizeY * (val + bias)) - scrollY;

                            // Clip line at end.
                            if (x >= maxX)
                            {
                                var ratio = (maxX - prevX) / (x - prevX);
                                x = Utils.Lerp(prevX, x, ratio);
                                y = Utils.Lerp(prevY, y, ratio);
                                i = env.Length;
                            }

                            line.Add(x);
                            line.Add(y);

                            prevSampleIndex = sampleIndex;
                            prevX = x;
                            prevY = y;
                        }
                    }
                }

                r.c.DrawLine(line, Theme.LightGreyColor2, 1, true);
            }

            if (editMode == EditionMode.Envelope)
            {
                string envelopeString = EnvelopeType.LocalizedNames[editEnvelope];

                if (editEnvelope == EnvelopeType.Pitch)
                    envelopeString = (editInstrument.Envelopes[editEnvelope].Relative ? EnvelopeRelativeLabel : EnvelopeAbsoluteLabel) + " " + envelopeString;

                r.f.DrawText(EditingInstrumentEnvelopeLabel.Format(editInstrument.Name, envelopeString), r.fonts.FontVeryLarge, bigTextPosX, bigTextPosY, Theme.LightGreyColor1);

                var textY = bigTextPosY + r.fonts.FontVeryLarge.LineHeight;

                if (App.SelectedInstrument != null && App.SelectedInstrument != editInstrument)
                { 
                    r.f.DrawText(InstrumentNotSelectedLabel.Format(App.SelectedInstrument.Name), r.fonts.FontMedium, bigTextPosX, textY, Theme.LightRedColor);
                    textY += r.fonts.FontMedium.LineHeight;
                }
                else if (editEnvelope == EnvelopeType.Arpeggio && App.SelectedArpeggio != null)
                { 
                    r.f.DrawText(ArpeggioOverriddenLabel.Format(App.SelectedArpeggio.Name), r.fonts.FontMedium, bigTextPosX, textY, Theme.LightRedColor);
                    textY += r.fonts.FontMedium.LineHeight;
                }

                if (relativeEffectScaling && IsSelectionValid())
                { 
                    r.c.DrawText(RelativeEffectScalingLabel, fonts.FontMedium, bigTextPosX, textY, Theme.LightRedColor);
                }
            }
            else
            {
                r.f.DrawText(EditingArpeggioLabel.Format(editArpeggio.Name), r.fonts.FontVeryLarge, bigTextPosX, bigTextPosY, Theme.LightGreyColor1);

                if (App.SelectedArpeggio != editArpeggio)
                {
                    r.f.DrawText(App.SelectedArpeggio == null ?
                        $"{ArpeggioNotSelectedLabel}" :
                        $"{ArpeggioNotSelectedLabel} {SelectedArpeggioWillBeHeardLabel.Format(App.SelectedArpeggio.Name)}", r.fonts.FontMedium, bigTextPosX, bigTextPosY + r.fonts.FontVeryLarge.LineHeight, Theme.LightRedColor);
                }
            }

            var gizmos = GetEnvelopeGizmos();
            if (gizmos != null)
            {
                foreach (var g in gizmos)
                {
                    var lineColor = IsGizmoHighlighted(g, 0) ? Color.White : Color.Black;

                    if (g.FillImage != null)
                        r.f.DrawTextureAtlas(g.FillImage, g.Rect.X, g.Rect.Y, g.Rect.Width / (float)g.Image.ElementSize.Width, color);
                    r.f.DrawTextureAtlas(g.Image, g.Rect.X, g.Rect.Y, g.Rect.Width / (float)g.Image.ElementSize.Width, lineColor);
                }
            }
        }

        private void RenderNoteArea(RenderInfo r)
        {
            r.c.PushTranslation(pianoSizeX, headerAndEffectSizeY);
            r.c.PushClipRegion(0, 0, width - pianoSizeX - scrollBarThickness, height - headerAndEffectSizeY - scrollBarThickness);

            if (editMode == EditionMode.Channel ||
                editMode == EditionMode.VideoRecording ||
                editMode == EditionMode.DPCMMapping)
            {
                RenderNotes(r);
            }
            else if (editMode == EditionMode.Envelope || editMode == EditionMode.Arpeggio)
            {
                RenderEnvelopeValues(r);
            }

            r.c.PopTransform();

            if (!string.IsNullOrEmpty(noteTooltip) && editMode != EditionMode.DPCM)
            {
                var textWidth = Width - tooltipTextPosX - scrollBarThickness;
                if (textWidth > 0)
                    r.f.DrawText(noteTooltip, r.fonts.FontLarge, 0, Height - tooltipTextPosY - scrollBarThickness, Theme.LightGreyColor1, TextFlags.Right, textWidth);
            }

            r.c.PopClipRegion();
        }

        private void RenderNoteBody(RenderInfo r, Note note, Color color, int time, int noteLen, int transpose, bool outline, bool selected, bool activeChannel, bool released, bool isFirstPart, int slideDuration, NoteAttackState attackState)
        {
            var noteValue = note.Value + transpose;
            var x = GetPixelXForAbsoluteNoteIndex(time);
            var y = GetPixelYForNoteValue(noteValue);
            var sy = released ? releaseNoteSizeY : noteSizeY;
            var activeChannelInt = activeChannel ? 0 : 1;

            if (!outline && isFirstPart && slideDuration >= 0)
            {
                // We will get zero for notes that start a slide and have an immediate delayed cut.
                var duration = Math.Max(1, slideDuration);
                var slideSizeX = duration;
                var slideSizeY = note.SlideNoteTarget + transpose - noteValue;

                r.c.PushTransform(x, y + (slideSizeY > 0 ? 0 : noteSizeY), GetPixelXForAbsoluteNoteIndex(slideSizeX, false), -slideSizeY);
                r.c.FillGeometry(slideNoteGeometry, Color.FromArgb(50, color), true);
                r.c.PopTransform();
            }

            if (released)
                y += noteSizeY / 2 - releaseNoteSizeY / 2;

            r.c.PushTranslation(x, y);

            int sx = GetPixelXForAbsoluteNoteIndex(noteLen, false);
            int noteTextPosX = attackIconPosX + 1;

            if (!outline)
                r.c.FillRectangleGradient(0, activeChannelInt, sx, sy, color, color.Scaled(0.8f), true, sy);
            
            if (activeChannel)
                r.c.DrawRectangle(0, 0, sx, sy, outline ? Theme.WhiteColor : (selected ? Theme.LightGreyColor1 : Theme.BlackColor), selected || outline ? 3 : 1, selected || outline, selected || outline);

            if (!outline)
            {
                if (isFirstPart && attackState != NoteAttackState.NoAttack && sx > noteAttackSizeX + attackIconPosX * 2 + 2)
                {
                    if (attackState == NoteAttackState.NoAttackError)
                    {
                        r.c.DrawRectangle(attackIconPosX + 1, attackIconPosX + 1, attackIconPosX + noteAttackSizeX, sy - attackIconPosX - 1, activeChannel ? attackColor : attackBrushForceDisplayColor);
                    }
                    else
                    {
                        r.c.FillRectangle(attackIconPosX + 1, attackIconPosX + 1, attackIconPosX + noteAttackSizeX + 1, sy - attackIconPosX, activeChannel ? attackColor : attackBrushForceDisplayColor);
                    }
                    noteTextPosX += noteAttackSizeX + attackIconPosX + 2;
                }

                if (activeChannel && !released && editMode == EditionMode.Channel && note.IsMusical && r.fonts.FontSmall.Size < noteSizeY)
                {
                    var label = note.FriendlyName;
                    if ((sx - noteTextPosX) > (label.Length + 1) * fontSmallCharSizeX)
                        r.c.DrawText(note.FriendlyName, r.fonts.FontSmall, noteTextPosX, 1, Theme.BlackColor, TextFlags.Middle, 0, noteSizeY);
                }

                if (note.Arpeggio != null)
                {
                    var offsets = note.Arpeggio.GetChordOffsets();
                    foreach (var offset in offsets)
                    {
                        r.c.PushTranslation(0, offset * -noteSizeY);
                        r.c.FillRectangle(0, 1, sx, sy, Color.FromArgb(activeChannel ? 50 : 25, note.Arpeggio.Color));
                        r.c.PopTransform();
                    }
                }
            }

            r.c.PopTransform();
        }

        private void RenderNoteReleaseOrStop(RenderInfo r, Note note, Color color, int time, int value, bool outline, bool selected, bool activeChannel, bool stop, bool released)
        {
            int x = GetPixelXForAbsoluteNoteIndex(time);
            int y = GetPixelYForNoteValue(value);
            var geo = stop ? (released ? stopReleaseNoteGeometry : stopNoteGeometry) : releaseNoteGeometry;

            r.c.PushTransform(x, y, noteSizeX, 1);
            if (!outline)
                r.c.FillGeometryGradient(geo[activeChannel ? 0 : 1], color, color.Scaled(0.8f), noteSizeY);
            if (activeChannel)
                r.c.DrawGeometry(geo[0], outline ? Theme.WhiteColor : (selected ? Theme.LightGreyColor1 : Theme.BlackColor), outline || selected ? 3 : 1, true);
            r.c.PopTransform();

            r.c.PushTranslation(x, y);
            if (!outline && note.Arpeggio != null)
            {
                var offsets = note.Arpeggio.GetChordOffsets();
                foreach (var offset in offsets)
                {
                    r.c.PushTransform(0, offset * -noteSizeY, noteSizeX, 1);
                    r.c.FillGeometry(geo[1], Color.FromArgb(activeChannel ? 50 : 25, note.Arpeggio.Color), true);
                    r.c.PopTransform();
                }
            }
            r.c.PopTransform();
        }

        private void RenderNote(RenderInfo r, NoteLocation location, Note note, Song song, int channelIndex, int distanceToNextNote, bool drawImplicityStopNotes, bool isActiveChannel, bool highlighted, bool released, NoteAttackState attackState)
        {
            Debug.Assert(note.IsMusical);

            if (distanceToNextNote < 0)
                distanceToNextNote = (int)ushort.MaxValue;

            var channel = song.Channels[channelIndex];
            var absoluteIndex = location.ToAbsoluteNoteIndex(Song);
            var nextAbsoluteIndex = absoluteIndex + distanceToNextNote;
            var duration = Math.Min(distanceToNextNote, note.Duration);
            var slideDuration = note.IsSlideNote ? channel.GetSlideNoteDuration(location) : -1;
            var color = GetNoteColor(channel, note.Value, note.Instrument, isActiveChannel ? 255 : 50);
            var selected = isActiveChannel && IsNoteSelected(location, duration);
            var transpose = videoChannelTranspose != null ? videoChannelTranspose[channelIndex] : 0;

            // Draw first part, from start to release point.
            if (note.HasRelease)
            {
                RenderNoteBody(r, note, color, absoluteIndex, Math.Min(note.Release, duration), transpose, highlighted, selected, isActiveChannel, released, true, slideDuration, attackState);
                absoluteIndex += note.Release;
                duration -= note.Release;

                if (duration > 0)
                {
                    RenderNoteReleaseOrStop(r, note, color, absoluteIndex, note.Value + transpose, highlighted, selected, isActiveChannel, false, released);
                    absoluteIndex++;
                    duration--;
                }

                released = true;
            }

            // Then second part, after release to stop note.
            if (duration > 0)
            {
                RenderNoteBody(r, note, color, absoluteIndex, duration, transpose, highlighted, selected, isActiveChannel, released, !note.HasRelease, slideDuration, attackState);
                absoluteIndex += duration;

                if (drawImplicityStopNotes && absoluteIndex < nextAbsoluteIndex && !highlighted)
                {
                    RenderNoteReleaseOrStop(r, note, Color.FromArgb(128, color), absoluteIndex, note.Value + transpose, highlighted, selected, isActiveChannel, true, released);
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

        private void RenderWave(RenderInfo r, short[] data, float rate, Color color, bool isSource, bool drawSamples)
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
                    var points = new float[numVisibleSample * 2];
                    var indices = isSource && drawSamples ? new int[numVisibleSample] : null;
                    var scaleX = 1.0f / (rate * viewTime) * viewWidth;
                    var biasX = (float)-scrollX;

                    for (int i = minVisibleSample, j = 0; i < maxVisibleSample; i += sampleSkip, j++)
                    {
                        points[j * 2 + 0] = i * scaleX + biasX;
                        points[j * 2 + 1] = halfHeight + data[i] / (float)short.MinValue * halfHeightPad;
                        if (indices != null) indices[j] = i;
                    }

                    r.c.DrawGeometry(points, color, 1, true, false);

                    if (drawSamples)
                    {
                        var selectionValid = IsSelectionValid();

                        for (int i = 0; i < points.Length / 2; i++)
                        {
                            var selected = isSource && selectionValid && indices[i] >= selectionMin && indices[i] <= selectionMax;
                            var sampleScale = selected ? 1.5f : 1.0f;

                            r.c.PushTransform(points[i * 2 + 0], points[i * 2 + 1], sampleScale, sampleScale);
                            r.c.FillGeometry(sampleGeometry, selected ? Theme.WhiteColor : color);
                            r.c.PopTransform();
                        }
                    }
                }
            }
        }

        private void RenderDmc(RenderInfo r, byte[] data, float rate, float baseTime, Color color, bool isSource, bool drawSamples, int dmcInitialValue)
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
                    var points = new float[numVisibleSample * 2];
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
                            points[j * 2 + 0] = i * scaleX + biasX;
                            points[j * 2 + 1] = (-(dpcmCounter - 32) / 64.0f) * 2.0f * halfHeightPad + halfHeight; // DPCMTODO : Is that centered correctly? Also negative value?
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

                    r.c.DrawGeometry(points, color, 1, true, false);

                    if (drawSamples)
                    {
                        var selectionValid = IsSelectionValid();

                        for (int i = 0; i < points.GetLength(0) / 2; i++)
                        {
                            var selected = isSource && selectionValid && indices[i] >= selectionMin && indices[i] <= selectionMax;
                            var sampleScale = selected ? 1.5f : 1.0f;

                            r.c.PushTransform(points[i * 2 + 0], points[i * 2 + 1], sampleScale, sampleScale);
                            r.c.FillGeometry(sampleGeometry, selected ? Theme.WhiteColor : color);
                            r.c.PopTransform();
                        }
                    }
                }
            }
        }

        private void RenderWaveform(RenderInfo r)
        {
            if (editMode != EditionMode.DPCM || Width < pianoSizeX)
                return;

            r.b.PushTranslation(pianoSizeX, headerAndEffectSizeY);
            r.b.PushClipRegion(0, 0, width - pianoSizeX, height - headerAndEffectSizeY - scrollBarThickness);

            // Source data range.
            r.b.FillRectangle(
                GetPixelForWaveTime(0, scrollX), 0,
                GetPixelForWaveTime(editSample.SourceDuration, scrollX), Height, Theme.DarkGreyColor4);

            // Horizontal center line
            var actualHeight = Height - scrollBarThickness;
            var sizeY        = actualHeight - headerAndEffectSizeY;
            var centerY      = sizeY * 0.5f;
            r.b.DrawLine(0, centerY, Width, centerY, Theme.BlackColor);

            // Top/bottom dash lines (limits);
            var topY    = waveDisplayPaddingY;
            var bottomY = (actualHeight - headerAndEffectSizeY) - waveDisplayPaddingY;
            r.b.DrawLine(0, topY,    Width, topY,    Theme.DarkGreyColor1, 1, false, true);
            r.b.DrawLine(0, bottomY, Width, bottomY, Theme.DarkGreyColor1, 1, false, true);

            // Vertical lines (1.0, 0.1, 0.01 seconds)
            ForEachWaveTimecode(r, (time, x, level, idx) =>
            {
                var modSeconds = Utils.IntegerPow(10, level + 1);
                var modTenths  = Utils.IntegerPow(10, level);

                var brush = Theme.DarkGreyColor1;
                var dash = true;

                if ((idx % modSeconds) == 0)
                {
                    dash = false;
                    brush = Theme.BlackColor;
                }
                else if ((idx % modTenths) == 0)
                {
                    dash = false;
                    brush = Theme.DarkGreyColor1;
                }

                r.b.DrawLine(x, 0, x, Height, brush, 1, false, dash);
            });

            // Selection rectangle
            if (IsSelectionValid())
            {
                r.b.FillRectangle(
                    GetPixelForWaveTime(GetWaveTimeForSample(selectionMin, true),  scrollX), 0, 
                    GetPixelForWaveTime(GetWaveTimeForSample(selectionMax, false), scrollX), Height, selectionBgVisibleColor);
            }

            // TODO: Make this a constants.
            bool showSamples = zoom > 32.0f;

            // Source waveform
            if (editSample.SourceDataIsWav)
            {
                RenderWave(r, editSample.SourceWavData.Samples, editSample.SourceSampleRate, Theme.LightGreyColor1, true, showSamples);
            }
            else
            {
                RenderDmc(r, editSample.SourceDmcData.Data, editSample.SourceSampleRate, 0.0f, Theme.LightGreyColor1, true, showSamples, editSample.DmcInitialValueDiv2); 
            }

            // Processed waveform
            RenderDmc(r, editSample.ProcessedData, editSample.ProcessedSampleRate, editSample.ProcessedStartTime, editSample.Color, false, showSamples, editSample.GetVolumeScaleDmcInitialValueDiv2());

            // Play position
            var playPosition = App.PreviewDPCMWavPosition;

            if (playPosition >= 0 && App.PreviewDPCMSampleId == editSample.Id)
            {
                var playTime = playPosition / (float)App.PreviewDPCMSampleRate;
                if (!App.PreviewDPCMIsSource)
                    playTime += editSample.ProcessedStartTime;
                var seekX = GetPixelForWaveTime(playTime, scrollX);
                r.c.DrawLine(seekX, 0, seekX, Height, App.PreviewDPCMIsSource ? Theme.LightGreyColor1 : editSample.Color, 3);
            }

            // Title + source/processed info.
            var textY = bigTextPosY;
            r.f.DrawText(EditingDPCMSampleLabel.Format(editSample.Name), r.fonts.FontVeryLarge, bigTextPosX, textY, Theme.LightGreyColor1);
            textY += r.fonts.FontVeryLarge.LineHeight;
            r.f.DrawText(DPCMSourceDataLabel.Format(editSample.SourceDataIsWav ? "WAV" : "DMC", editSample.SourceSampleRate, editSample.SourceDataSize, (int)(editSample.SourceDuration * 1000)), r.fonts.FontMedium, bigTextPosX, textY, Theme.LightGreyColor1);
            textY += r.fonts.FontMedium.LineHeight;
            r.f.DrawText(DPCMProcessedDataLabel.Format(DPCMSampleRate.GetString(false, App.PalPlayback, true, true, editSample.SampleRate), editSample.ProcessedData.Length, (int)(editSample.ProcessedDuration * 1000)), r.fonts.FontMedium, bigTextPosX, textY, Theme.LightGreyColor1);
            textY += r.fonts.FontMedium.LineHeight;
            r.f.DrawText(DPCMPreviewPlaybackLabel.Format(DPCMSampleRate.GetString(false, App.PalPlayback, true, true, editSample.PreviewRate), (int)(editSample.GetPlaybackDuration(App.PalPlayback) * 1000)), r.fonts.FontMedium, bigTextPosX, textY, Theme.LightGreyColor1);;

            r.b.PopTransform();

            if (!string.IsNullOrEmpty(noteTooltip))
            {
                r.f.DrawText(noteTooltip, r.fonts.FontLarge, 0, actualHeight - tooltipTextPosY, Theme.LightGreyColor1, TextFlags.Right, Width - tooltipTextPosX);
            }

            r.b.PopClipRegion();
        }

        public void RenderVideoFrame(Graphics g, int channel, long forceDisplayMask, int patternIndex, float noteIndex, float centerNote, (int, Color)[] highlightKeys)
        {
            Debug.Assert(editMode == EditionMode.VideoRecording);

            int noteY = (int)Math.Round(virtualSizeY - centerNote * noteSizeY + noteSizeY / 2);

            editChannel = channel;
            scrollX = (int)Math.Round((Song.GetPatternStartAbsoluteNoteIndex(patternIndex) + noteIndex) * (double)noteSizeX);
            scrollY = noteY - (Height - headerAndEffectSizeY) / 2;
            videoHighlightKeys = highlightKeys;
            videoForceDisplayChannelMask = forceDisplayMask;

            OnRender(g);
        }

        private void RenderScrollBars(RenderInfo r)
        {
            if (Settings.ScrollBars != Settings.ScrollBarsNone && editMode != EditionMode.VideoRecording)
            {
                if (GetScrollBarParams(true, out var scrollBarThumbPosX, out var scrollBarThumbSizeX, out var scrollBarSizeX))
                {
                    r.c.PushTranslation(pianoSizeX - 1, 0);
                    r.c.FillAndDrawRectangle(0, Height - scrollBarThickness, scrollBarSizeX, Height, Theme.DarkGreyColor4, Theme.BlackColor);
                    r.c.FillAndDrawRectangle(scrollBarThumbPosX, Height - scrollBarThickness, scrollBarThumbPosX + scrollBarThumbSizeX, Height, Theme.MediumGreyColor1, Theme.BlackColor);
                    r.c.PopTransform();
                }

                if (GetScrollBarParams(false, out var scrollBarThumbPosY, out var scrollBarThumbSizeY, out var scrollBarSizeY))
                {
                    r.c.PushTranslation(0, headerAndEffectSizeY - 1);
                    r.c.FillAndDrawRectangle(Width - scrollBarThickness, 0, Width, scrollBarSizeY, Theme.DarkGreyColor4, Theme.BlackColor);
                    r.c.FillAndDrawRectangle(Width - scrollBarThickness, scrollBarThumbPosY, Width, scrollBarThumbPosY + scrollBarThumbSizeY, Theme.MediumGreyColor1, Theme.BlackColor);
                    r.c.PopTransform();
                }
            }
        }

        private void RenderDebug(RenderInfo r)
        {
#if DEBUG
            if (Platform.IsMobile)
            {
                r.g.OverlayCommandList.FillRectangle(mouseLastX - 30, mouseLastY - 30, mouseLastX + 30, mouseLastY + 30, Theme.WhiteColor);
            }
#endif
        }

#if DEBUG
        // OpenGL line rasterization tests.
        private void RenderLineDebug(Graphics g)
        {
#if false
            for (int i = 1; i <= 8; i++)
            {
                g.Transform.PushTranslation(i * 110, 100);
                for (int j = 0; j < 16; j++)
                {
                    var cos = (float)Math.Cos(j / 16.0 * Math.PI * 2.0);
                    var sin = (float)Math.Sin(j / 16.0 * Math.PI * 2.0);

                    g.DefaultCommandList.DrawLine(0, 0, cos * 50, sin * 50, new Color(255, 0, 255), i, true);
                    g.ForegroundCommandList.DrawLine(0, 0, cos * 50, sin * 50, new Color(0, 255, 0), 1, false);
                }

                g.DefaultCommandList.DrawRectangle(-50, 100, 50, 110, new Color(255, 0, 255), i, true);
                g.ForegroundCommandList.DrawRectangle(-50, 100, 50, 110, new Color(0, 255, 0), 1, false);
                g.Transform.PopTransform();
            }
#elif false
            releaseNoteGeometry[0][4] = 10.0f;
            releaseNoteGeometry[0][6] = 10.0f;
            for (int i = 1; i <= 8; i++)
            {
                g.Transform.PushTranslation(i * 200, 100);
                g.OverlayCommandList.DrawLine(0, 0, 100, 100, new Color(255, 0, 255), i, true);
                g.OverlayCommandList.DrawLine(0, 0, 100, 0, new Color(255, 0, 255), i, true);
                g.OverlayCommandList.DrawLine(0, 0, 0, 100, new Color(255, 0, 255), i, true);
                g.OverlayCommandList.FillAndDrawRectangle(0, 200, 100, 210, new Color(255, 0, 255), new Color(0, 255, 0), i, true, i > 1);
                g.Transform.PushTranslation(0, 110);
                g.OverlayCommandList.FillGeometry(releaseNoteGeometry[0], Color.Pink, true);
                g.Transform.PushTranslation(0, 20);
                g.OverlayCommandList.DrawGeometry(releaseNoteGeometry[0], Color.SpringGreen, i, true, true);
                g.Transform.PopTransform();
                g.Transform.PopTransform();
                g.Transform.PopTransform();
                g.Transform.PushTranslation(i * 200, 500);
                g.OverlayCommandList.DrawLine(0, 0, 100, 100, new Color(255, 0, 255), i, false);
                g.OverlayCommandList.DrawLine(0, 0, 100, 0, new Color(255, 0, 255), i, false);
                g.OverlayCommandList.DrawLine(0, 0, 0, 100, new Color(255, 0, 255), i, false);
                g.OverlayCommandList.DrawLine(0, 0, 100, 100, new Color(0, 255, 0), 1, false);
                g.OverlayCommandList.DrawLine(0, 0, 100, 0, new Color(0, 255, 0), 1, false);
                g.OverlayCommandList.DrawLine(0, 0, 0, 100, new Color(0, 255, 0), 1, false);
                g.OverlayCommandList.FillAndDrawRectangle(0, 200, 100, 210, new Color(255, 0, 255), new Color(0, 255, 0), i, false);
                g.Transform.PushTranslation(0, 110);
                g.OverlayCommandList.FillGeometry(releaseNoteGeometry[0], Color.Pink, false);
                g.Transform.PushTranslation(0, 20);
                g.OverlayCommandList.DrawGeometry(releaseNoteGeometry[0], Color.SpringGreen, i, false, true);
                g.Transform.PopTransform();
                g.Transform.PopTransform();
                g.Transform.PopTransform();
            }
#else

            var rnd = new Random(1003);

            for (int j = 0; j < 2; j++)
            { 
                var points = new float[50 * 2];
                var x = 30;

                for (int i = 0; i < points.Length / 2; i++)
                {
                    points[i * 2 + 0] = x;
                    points[i * 2 + 1] = rnd.Next(100, 400) + j * 300;

                    x += rnd.Next(10, 50);
                }

                //var points = new float [] { 100, 100, 200, 200, 200, 100 };

                //g.OverlayCommandList.DrawGeometry(points, Color.Pink, 16, true, false);
                g.OverlayCommandList.DrawNiceSmoothLine(points, Color.Pink, 6);
            }
#endif
        }

        private void RenderTextDebug(Graphics g)
        {
            var strings = new[]
            {
                "Noise",
                "Triangle",
                "DPCM",
                "Square"
            };

            for (int i = 0; i < strings.Length; i++)
            {
                g.DefaultCommandList.DrawText(strings[i], Fonts.FontMedium, 20, 20 + i * Fonts.FontMedium.LineHeight, Color.White);
            }
        }
#endif

        protected override void OnRender(Graphics g)
        {
            // Init
            var r = new RenderInfo();

            var minVisibleNoteIdx = Math.Max(GetAbsoluteNoteIndexForPixelX(0), 0);
            var maxVisibleNoteIdx = Math.Min(GetAbsoluteNoteIndexForPixelX(Width) + 1, Song.GetPatternStartAbsoluteNoteIndex(Song.Length));

            r.g = g;
            r.fonts = Fonts;
            r.b = g.BackgroundCommandList;
            r.c = g.DefaultCommandList;
            r.f = g.ForegroundCommandList;

            var minNote = editMode == EditionMode.VideoRecording ? -10000 : 0;
            var maxNote = editMode == EditionMode.VideoRecording ?  10000 : NumNotes;

            r.maxVisibleNote = NumNotes - Utils.Clamp((int)Math.Floor(scrollY / (float)noteSizeY), minNote, maxNote);
            r.minVisibleNote = NumNotes - Utils.Clamp((int)Math.Ceiling((scrollY + Height - headerAndEffectSizeY) / (float)noteSizeY), minNote, maxNote);
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

            //RenderLineDebug(g); return;
            //RenderTextDebug(g); return;

            // Prepare command list.
            RenderHeader(r);
            RenderEffectList(r);
            RenderEffectPanel(r);
            RenderPiano(r);
            RenderNoteArea(r);
            RenderWaveform(r);
            RenderScrollBars(r);
            RenderDebug(r);
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
                    GetMinMaxScroll(out var minScrollX, out _, out var maxScrollX, out _);

                    if (minScrollX == maxScrollX)
                        return false;

                    scrollSize = Width - pianoSizeX + 1;
                    thumbSize = Math.Max(minScrollBarLength, (int)Math.Round(scrollSize * Math.Min(1.0f, scrollSize / (float)(maxScrollX + scrollSize))));
                    thumbPos  = (int)Math.Round((scrollSize - thumbSize) * (scrollX / (float)maxScrollX));
                    return true;
                }
                else
                {
                    GetMinMaxScroll(out _, out var minScrollY, out _, out var maxScrollY);

                    if (minScrollY == maxScrollY)
                        return false;

                    scrollSize = Height - headerAndEffectSizeY - scrollBarThickness + 1;
                    thumbSize = Math.Max(minScrollBarLength, (int)Math.Round(scrollSize * Math.Min(1.0f, scrollSize / (float)(maxScrollY + scrollSize))));
                    thumbPos  = (int)Math.Round((scrollSize - thumbSize) * (scrollY / (float)maxScrollY));
                    return true;
                }
            }

            return false;
        }

        void ResizeEnvelope(int x, int y, bool final)
        {
            var env = EditEnvelope;
            var length = Utils.RoundDown(GetAbsoluteNoteIndexForPixelX(x - pianoSizeX), env.ChunkLength);

            ScrollIfNearEdge(x, y);

            switch (captureOperation)
            {
                case CaptureOperation.ResizeEnvelope:
                    if (env.Length != length)
                    {
                        env.Length = length;
                        editInstrument?.NotifyEnvelopeChanged(editEnvelope, false); // Instrument is null when editing arps.
                        if (IsSelectionValid())
                            SetSelection(selectionMin, selectionMax);
                    }
                    break;
                case CaptureOperation.DragRelease:
                    if (env.Release != length && length > 0)
                    {
                        env.Release = length;
                        editInstrument?.NotifyEnvelopeChanged(editEnvelope, false); // Instrument is null when editing arps.
                    }
                    break;
                case CaptureOperation.DragLoop:
                    if (env.Loop != length)
                    {
                        env.Loop = length;
                        editInstrument?.NotifyEnvelopeChanged(editEnvelope, false); // Instrument is null when editing arps.
                    }
                    break;
            }

            ClampScroll();
            MarkDirty();

            if (final)
            {
                EnvelopeChanged?.Invoke(editInstrument, editEnvelope);
                App.UndoRedoManager.EndTransaction();
            }
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

        float GetEffectValueExponent(int maxValue)
        {
            return 1.0f / (maxValue >= 4095 ? 4 : 1);
        }

        void UpdateChangeEffectValue(int x, int y)
        {
            Debug.Assert(selectedEffectIdx >= 0);

            App.UndoRedoManager.RestoreTransaction(false);

            var channel = Song.Channels[editChannel];
            var pattern = channel.PatternInstances[captureNoteLocation.PatternIndex];
            var location = captureNoteLocation;
            var captureEffectValue = 0;

            var min = Note.GetEffectMinValue(Song, channel, selectedEffectIdx);
            var max = Note.GetEffectMaxValue(Song, channel, selectedEffectIdx);
            var exp = GetEffectValueExponent(max);

            if (pattern == null || !pattern.TryGetNoteWithEffectAt(location.NoteIndex, selectedEffectIdx, out var note))
            {
                if (SnapEnabled && SnapEffectEnabled)
                    location = SnapNote(location);

                if (pattern == null)
                    pattern = channel.CreatePatternAndInstance(location.PatternIndex);

                captureEffectValue = GetEffectValueForPixelY(effectPanelSizeY - (captureMouseY - headerSizeY), min, max, exp);
                note = pattern.GetOrCreateNoteAt(location.NoteIndex);
                note.SetEffectValue(selectedEffectIdx, captureEffectValue);
            }
            else
            {
                captureEffectValue = note.GetEffectValue(selectedEffectIdx);
            }

            var delta = 0;
            var originalValue = note.GetEffectValue(selectedEffectIdx);

            // On mobile, we use gizmos, so let's pretend the drag is happening at the effect value instead of at the gizmo position.
            if (Platform.IsMobile)
                y += headerAndEffectSizeY - GetPixelYForEffectValue(captureEffectValue, min, max, exp) - captureMouseY;

            if (ModifierKeys.IsControlDown)
            {
                delta = (captureMouseY - y) / 4;
            }
            else 
            {
                var ratio = 1.0f - Utils.Saturate(((y - headerSizeY) / (float)effectPanelSizeY));
                var newValue = (int)Math.Round(Utils.Lerp(min, max, MathF.Pow(ratio, 1.0f / exp)));

                delta = newValue - originalValue;
            }

            if (captureOperation == CaptureOperation.ChangeSelectionEffectValue)
            {
                var scaling = (Utils.Clamp(originalValue + delta, min, max) - min) / (float)(originalValue - min);
                var minLocation = NoteLocation.FromAbsoluteNoteIndex(Song, selectionMin);
                var maxLocation = NoteLocation.FromAbsoluteNoteIndex(Song, selectionMax);
                var processedNotes = new HashSet<Note>();

                for (var it = channel.GetSparseNoteIterator(minLocation, maxLocation, Note.GetFilterForEffect(selectedEffectIdx)); !it.Done; it.Next())
                {
                    if (!processedNotes.Contains(it.Note))
                    {
                        var value = it.Note.GetEffectValue(selectedEffectIdx);

                        if (relativeEffectScaling)
                        {
                            it.Note.SetEffectValue(selectedEffectIdx, Utils.Clamp((int)Math.Round((value - min) * scaling + min), min, max));
                        }
                        else
                        {
                            it.Note.SetEffectValue(selectedEffectIdx, Utils.Clamp(value + delta, min, max));
                        }

                        processedNotes.Add(it.Note);
                    }
                }

                channel.InvalidateCumulativePatternCache(minLocation.PatternIndex, maxLocation.PatternIndex);
            }
            else
            {
                var value = note.GetEffectValue(selectedEffectIdx);
                note.SetEffectValue(selectedEffectIdx, Utils.Clamp(value + delta, min, max));

                channel.InvalidateCumulativePatternCache(pattern);
            }

            MarkDirty();
        }

        void StartChangeEnvelopeRepeatValue(int x, int y)
        {
            StartCaptureOperation(x, y, CaptureOperation.ChangeEnvelopeRepeatValue, false, GetAbsoluteNoteIndexForPixelX(x - pianoSizeX) / EditEnvelope.ChunkLength);
            App.UndoRedoManager.BeginTransaction(TransactionScope.Instrument, editInstrument.Id);
            UpdateChangeEnvelopeRepeatValue(x, y);
        }

        void UpdateChangeEnvelopeRepeatValue(int x, int y)
        {
            App.UndoRedoManager.RestoreTransaction(false);

            var env = EditEnvelope;
            var rep = EditRepeatEnvelope;
            var idx = Utils.Clamp(captureMouseAbsoluteIdx, 0, env.Length);

            idx /= env.ChunkLength;

            Envelope.GetMinMaxValueForType(editInstrument, EnvelopeType.WaveformRepeat, out var minRepeat, out var maxRepeat);

            var originalValue = rep.Values[idx];
            var delta = 0;

            // On mobile, we use gizmos, so let's pretend the drag is happening at the effect value instead of at the gizmo position.
            if (Platform.IsMobile)
                y += headerAndEffectSizeY - GetPixelYForEffectValue(originalValue, minRepeat, maxRepeat) - captureMouseY;

            var ratio = (y - headerSizeY) / (float)effectPanelSizeY;
            var newValue = (int)Math.Round(Utils.Lerp(maxRepeat, minRepeat, ratio));

            delta = newValue - originalValue;

            if (IsSelectionValid())
            {
                if (GetRepeatEnvelopeSelectionMinMax(out var min, out var max))
                {
                    for (int i = min; i <= max; i++)
                        rep.Values[i] = (sbyte)Utils.Clamp(rep.Values[i] + delta, minRepeat, maxRepeat);
                }
            }
            else
            {
                rep.Values[idx] = (sbyte)Utils.Clamp(rep.Values[idx] + delta, minRepeat, maxRepeat);
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
            var offsetY = headerSizeY + effectPanelSizeY - GetPixelYForEffectValue(Note.EffectVolumeSlide, note.VolumeSlideTarget) - y;
           
            StartCaptureOperation(x, y, CaptureOperation.DragVolumeSlideTargetGizmo, false, location.ToAbsoluteNoteIndex(Song), 0, offsetY);
            App.UndoRedoManager.BeginTransaction(TransactionScope.Pattern, pattern.Id);
        }

        void UpdateDragVolumeSlide(int x, int y, bool final)
        {
            if (Platform.IsMobile)
                App.UndoRedoManager.RestoreTransaction(false);

            var channel = Song.Channels[editChannel];
            var pattern = channel.PatternInstances[captureNoteLocation.PatternIndex];
            var note    = pattern.Notes[captureNoteLocation.NoteIndex];

            // On mobile, we use gizmos, so let's pretend the drag is happening at the effect value instead of at the gizmo position.
            if (Platform.IsMobile)
                y += headerAndEffectSizeY - GetPixelYForEffectValue(note.VolumeSlideTarget, 0, Note.VolumeMax) - captureMouseY;

            var ratio = Utils.Clamp(1.0f - (y - headerSizeY) / (float)effectPanelSizeY, 0.0f, 1.0f);
            note.VolumeSlideTarget = (byte)Math.Round(ratio * Note.VolumeMax);

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

        void DrawEnvelope(int x, int y, bool first = false, bool final = false)
        {
            ScrollIfNearEdge(x, y);

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

            if (final)
            {
                editInstrument?.NotifyEnvelopeChanged(editEnvelope, true);
                EnvelopeChanged?.Invoke(editInstrument, editEnvelope);
                App.UndoRedoManager.EndTransaction();
            }
        }

        void DrawSingleEnvelopeValue(int x, int y)
        {
            if (GetEnvelopeValueForCoord(x, y, out int idx, out sbyte val))
            {
                var env = EditEnvelope;
                idx = Utils.Clamp(idx, 0, env.Length - 1);

                Envelope.GetMinMaxValueForType(editInstrument, editEnvelope, out int min, out int max);

                if (editMode == EditionMode.Envelope)
                    App.UndoRedoManager.BeginTransaction(TransactionScope.Instrument, editInstrument.Id);
                else
                    App.UndoRedoManager.BeginTransaction(TransactionScope.Arpeggio, editArpeggio.Id);

                env.Values[idx] = (sbyte)Utils.Clamp(val, min, max);
                highlightNoteAbsIndex = idx;

                App.UndoRedoManager.EndTransaction();
            }
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
            var note = GetPianoNote(Utils.Clamp(x, 0, pianoSizeX - 1), y);
            if (note >= 0)
            {
                if (note != playLastNote)
                {
                    playLastNote = note;
                    App.PlayInstrumentNote(playLastNote, true, true);
                    MarkDirty();
                }
            }
        }

        private void EditDPCMSampleMappingProperties(Point pt, DPCMSampleMapping mapping)
        {
            var strings = DPCMSampleRate.GetStringList(true, FamiStudio.StaticInstance.PalPlayback, true, true);

            var dlg = new PropertyDialog(ParentWindow, SampleMappingTitle, new Point(left + pt.X, top + pt.Y), 400, false, pt.Y > Height / 2);
            dlg.Properties.AddDropDownList(PitchLabel.Colon, strings, strings[mapping.Pitch]); // 0
            dlg.Properties.AddCheckBox(LoopLabel.Colon, mapping.Loop); // 1
            dlg.Properties.AddCheckBox(OverrideDMCInitialValueLabel.Colon, mapping.OverrideDmcInitialValue); // 2
            dlg.Properties.AddNumericUpDown(DMCInitialValueDiv2Label.Colon, mapping.DmcInitialValueDiv2, 0, 63, 1); // 3
            dlg.Properties.Build();
            dlg.Properties.SetPropertyEnabled(3, mapping.OverrideDmcInitialValue);
            dlg.Properties.PropertyChanged += DPCMSampleMapping_PropertyChanged;

            dlg.ShowDialogAsync((r) =>
            {
                if (r == DialogResult.OK)
                {
                    App.UndoRedoManager.BeginTransaction(TransactionScope.Instrument, editInstrument.Id);
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

        private bool HandleDoubleClickChannelNote(PointerEventArgs e)
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
                    StartCaptureOperation(e.X, e.Y, CaptureOperation.DeleteNotes);
                }

                return true;
            }

            return false;
        }

        private bool HandleDoubleClickEffectPanel(PointerEventArgs e)
        {
            if (e.Left && selectedEffectIdx >= 0 && IsPointInEffectPanel(e.X, e.Y) && GetEffectNoteForCoord(e.X, e.Y, out var location))
            {
                ClearEffectValue(location);
                return true;
            }

            return false;

        }

        private bool HandleDoubleClickDPCMMapping(PointerEventArgs e)
        {
            if (GetLocationForCoord(e.X, e.Y, out _, out var noteValue))
            {
                var mapping = editInstrument.GetDPCMMapping(noteValue);
                if (mapping != null)
                    ClearDPCMSampleMapping(noteValue);
                return true;
            }

            return true;
        }

        protected override void OnMouseDoubleClick(PointerEventArgs e)
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
            CapturePointer();
        }

        private void StartCaptureOperation(int x, int y, CaptureOperation op, bool allowSnap = false, int noteIdx = -1, int offsetX = 0, int offsetY = 0)
        {
#if DEBUG
            Debug.Assert(captureOperation == CaptureOperation.None);
#else
            if (captureOperation != CaptureOperation.None)
                AbortCaptureOperation();
#endif

            CaptureMouse(x, y);
            captureOperation = op;
            captureThresholdMet = captureThresholds[(int)op] == 0;
            captureRealTimeUpdate = captureWantsRealTimeUpdate[(int)op];
            captureWaveTime = editMode == EditionMode.DPCM ? GetWaveTimeForPixel(x - pianoSizeX) : 0.0f;
            captureNoteValue = NumNotes - Utils.Clamp((y + scrollY - headerAndEffectSizeY) / noteSizeY, 0, NumNotes);
            captureSelectionMin = selectionMin;
            captureSelectionMax = selectionMax;
            captureOffsetX = offsetX;
            captureOffsetY = offsetY;
            canFling = false;
            captureTime = Platform.TimeSeconds();

            if (editMode == EditionMode.Envelope || editMode == EditionMode.Arpeggio)
                GetEnvelopeValueForCoord(x, y, out _, out captureEnvelopeValue);

            captureMouseAbsoluteIdx = GetAbsoluteNoteIndexForPixelX(x - pianoSizeX);
            captureMouseLocation = Song.AbsoluteNoteIndexToNoteLocation(captureMouseAbsoluteIdx);
            captureNoteAbsoluteIdx = noteIdx >= 0 ? noteIdx : captureMouseAbsoluteIdx;
            captureNoteLocation = Song.AbsoluteNoteIndexToNoteLocation(captureNoteAbsoluteIdx);

            if (noteIdx >= 0)
                highlightNoteAbsIndex = captureNoteAbsoluteIdx;
        }

        private void UpdateScrollBarX(int x, int y)
        {
            GetScrollBarParams(true, out _, out var scrollBarThumbSizeX, out var scrollAreaSizeX);
            GetMinMaxScroll(out _, out _, out var maxScrollX, out _);

            scrollX = (int)Math.Round(captureScrollX + ((x - captureMouseX) / (float)(scrollAreaSizeX - scrollBarThumbSizeX) * maxScrollX));

            ClampScroll();
            MarkDirty();
        }

        private void UpdateScrollBarY(int x, int y)
        {
            GetScrollBarParams(false, out _, out var scrollBarThumbSizeY, out var scrollAreaSizeY);
            GetMinMaxScroll(out _, out _, out _, out var maxScrollY);

            scrollY = (int)Math.Round(captureScrollY + ((y - captureMouseY) / (float)(scrollAreaSizeY - scrollBarThumbSizeY) * maxScrollY));

            ClampScroll();
            MarkDirty();
        }

        private void UpdateCaptureOperation(int x, int y, float scale = 1.0f, bool realTime = false)
        {  
            if (captureOperation != CaptureOperation.None && !captureThresholdMet)
            {
                var threshold = captureThresholds[(int)captureOperation];

                if (Math.Abs(x - captureMouseX) >= threshold ||
                    Math.Abs(y - captureMouseY) >= threshold)
                {
                    captureThresholdMet = true;
                }
            }

            if (captureOperation != CaptureOperation.None && captureThresholdMet && (captureRealTimeUpdate || !realTime))
            {
                x += captureOffsetX;
                y += captureOffsetY;

                switch (captureOperation)
                {
                    case CaptureOperation.DragLoop:
                    case CaptureOperation.DragRelease:
                    case CaptureOperation.ResizeEnvelope:
                        ResizeEnvelope(x, y, false);
                        break;
                    case CaptureOperation.PlayPiano:
                        PlayPiano(x, y);
                        break;
                    case CaptureOperation.ChangeEffectValue:
                    case CaptureOperation.ChangeSelectionEffectValue:
                        UpdateChangeEffectValue(x, y);
                        break;
                    case CaptureOperation.ChangeEnvelopeRepeatValue:
                        UpdateChangeEnvelopeRepeatValue(x, y);
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
                    case CaptureOperation.MoveSelectionNoteRelease:
                        UpdateMoveNoteRelease(x, y, false);
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
                    case CaptureOperation.DeleteNotes:
                        UpdateDeleteNotes(x, y);
                        break;
                }
            }
        }
        private void UpdateDragDPCMSampleMapping(int x, int y)
        {
            if (draggedSample == null)
            {
                App.UndoRedoManager.BeginTransaction(TransactionScope.Instrument, editInstrument.Id);
                draggedSample = editInstrument.GetDPCMMapping(captureNoteValue);
                editInstrument.UnmapDPCMSample(captureNoteValue);
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
                if (GetNoteValueForCoord(x, y, out var noteValue) && noteValue != captureNoteValue && draggedSample != null)
                {
                    var sample = draggedSample;

                    // Map the sample right away so that it renders correctly as the message box pops.
                    editInstrument.UnmapDPCMSample(noteValue);
                    editInstrument.MapDPCMSample(noteValue, sample.Sample, sample.Pitch, sample.Loop);

                    draggedSample = null;

                    Platform.MessageBoxAsync(ParentWindow, TransposeSampleMessage, TransposeSampleTitle, MessageBoxButtons.YesNo, (r) =>
                    {
                        if (r == DialogResult.Yes)
                        {
                            // Need to promote the transaction to project level since we are going to be transposing 
                            // potentially in multiple songs.
                            App.UndoRedoManager.RestoreTransaction(false);
                            App.UndoRedoManager.AbortTransaction();
                            App.UndoRedoManager.BeginTransaction(TransactionScope.Project);

                            // Need to redo everything + transpose.
                            editInstrument.UnmapDPCMSample(captureNoteValue);
                            editInstrument.UnmapDPCMSample(noteValue);
                            editInstrument.MapDPCMSample(noteValue, sample.Sample, sample.Pitch, sample.Loop);
                            App.Project.TransposeDPCMMapping(captureNoteValue, noteValue, editInstrument);
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
                x += captureOffsetX;
                y += captureOffsetY;

                switch (captureOperation)
                {
                    case CaptureOperation.PlayPiano:
                        EndPlayPiano();
                        break;
                    case CaptureOperation.ResizeEnvelope:
                        ResizeEnvelope(x, y, true);
                        break;
                    case CaptureOperation.DrawEnvelope:
                        DrawEnvelope(x, y, false, true);
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
                    case CaptureOperation.MoveNoteRelease:
                    case CaptureOperation.MoveSelectionNoteRelease:
                        UpdateMoveNoteRelease(x, y, true);
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
                    case CaptureOperation.DeleteNotes:
                        EndDeleteNotes();
                        break;
                    case CaptureOperation.ChangeEnvelopeValue:
                        UpdateChangeEnvelopeValue(x, y, true);
                        break;
                    case CaptureOperation.DragLoop:
                    case CaptureOperation.DragRelease:
                    case CaptureOperation.ChangeEffectValue:
                    case CaptureOperation.ChangeSelectionEffectValue:
                    case CaptureOperation.ChangeEnvelopeRepeatValue:
                        App.UndoRedoManager.EndTransaction();
                        break;
                }

                draggedSample = null;
                captureOperation = CaptureOperation.None;
                panning = false;
                if (!Platform.IsMobile)
                    highlightNoteAbsIndex = -1;

                ReleasePointer();
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
                ReleasePointer();
                App.StopInstrument();

                captureOperation = CaptureOperation.None;
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

            editInstrument?.NotifyEnvelopeChanged(editEnvelope, true);
            EnvelopeChanged?.Invoke(editInstrument, editEnvelope);
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
                var pt = ScreenToControl(CursorPosition);
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
                var pt = ScreenToControl(CursorPosition);
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

        private bool AllowGizmos()
        {
            return  window != null && !window.IsAsyncDialogInProgress;
        }

        private List<Gizmo> GetNoteGizmos(out Note note, out NoteLocation location)
        {
            note = Platform.IsDesktop ? 
                GetNoteForDesktopNoteGizmos(out location) :
                GetHighlightedNoteAndLocation(out location);

            if (note == null || !note.IsMusical || !AllowGizmos())
                return null;

            var locationAbsIndex = location.ToAbsoluteNoteIndex(Song);
            var visualDuration = GetVisualNoteDuration(locationAbsIndex, note);
            var list = new List<Gizmo>();

            // Resize gizmo
            if (Platform.IsMobile)
            {
                var x = GetPixelXForAbsoluteNoteIndex(locationAbsIndex + visualDuration) + gizmoSize / 4;
                var y = virtualSizeY - note.Value * noteSizeY - scrollY - gizmoSize / 4;

                if ((captureOperation == CaptureOperation.ResizeNoteEnd ||
                     captureOperation == CaptureOperation.ResizeSelectionNoteEnd) && captureThresholdMet)
                {
                    x = mouseLastX - pianoSizeX - gizmoSize / 2;
                }

                Gizmo resizeGizmo = new Gizmo();
                resizeGizmo.Image = bmpGizmoResizeLeftRight;
                resizeGizmo.FillImage = bmpGizmoResizeFill;
                resizeGizmo.Action = GizmoAction.ResizeNote;
                resizeGizmo.Rect = new Rectangle(x, y, gizmoSize, gizmoSize);
                resizeGizmo.OffsetX = -gizmoSize * 3 / 4;
                list.Add(resizeGizmo);
            }

            // Release gizmo
            if (Platform.IsMobile && note.HasRelease && note.Release < visualDuration)
            {
                var x = GetPixelXForAbsoluteNoteIndex(locationAbsIndex + note.Release) - gizmoSize / 2;
                var y = virtualSizeY - note.Value * noteSizeY - scrollY + gizmoSize * 3 / 4;

                if ((captureOperation == CaptureOperation.MoveNoteRelease ||
                     captureOperation == CaptureOperation.MoveSelectionNoteRelease) && captureThresholdMet)
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
                var x = GetPixelXForAbsoluteNoteIndex(locationAbsIndex + visualDuration) + (Platform.IsMobile ? gizmoSize / 4 : -5 * gizmoSize / 4);
                var y = 0;

                if (Platform.IsMobile)
                {
                    if (Platform.IsMobile && captureOperation == CaptureOperation.DragSlideNoteTargetGizmo && captureThresholdMet)
                        y = mouseLastY - headerAndEffectSizeY - gizmoSize / 2;
                    else
                        y = virtualSizeY  - Utils.Clamp((note.SlideNoteTarget + side) * noteSizeY + gizmoSize / 4, gizmoSize, virtualSizeY) - scrollY;
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
                slideGizmo.GizmoText = Note.GetFriendlyName(note.SlideNoteTarget);
                list.Add(slideGizmo);
            }

            return list;
        }

        private List<Gizmo> GetEffectGizmos(out Note note, out NoteLocation location)
        {
            note = Platform.IsDesktop ?
                GetNoteForDesktopEffectGizmos(out location) :
                GetHighlightedNoteAndLocation(out location);

            if (!showEffectsPanel || selectedEffectIdx < 0 || note == null || !note.HasValidEffectValue(selectedEffectIdx) || !AllowGizmos())
                return null;

            var list = new List<Gizmo>();

            var locationAbsIndex = location.ToAbsoluteNoteIndex(Song);
            var channel  = Song.Channels[editChannel];
            var minValue = Note.GetEffectMinValue(Song, channel, selectedEffectIdx);
            var maxValue = Note.GetEffectMaxValue(Song, channel, selectedEffectIdx);
            var exp = GetEffectValueExponent(maxValue);
            var midValue = (int)MathF.Round(Utils.Lerp(minValue, maxValue, MathF.Pow(0.5f, 1.0f / exp)));
            var value    = note.GetEffectValue(selectedEffectIdx);

            // Effect values
            if (Platform.IsMobile)
            {
                var effectPosY = effectPanelSizeY - GetPixelYForEffectValue(value, minValue, maxValue, exp);
                var x = GetPixelXForAbsoluteNoteIndex(locationAbsIndex + 1) + gizmoSize / 4;
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
                var effectPosY = effectPanelSizeY - GetPixelYForEffectValue(note.VolumeSlideTarget, minValue, maxValue, exp);

                var x = GetPixelXForAbsoluteNoteIndex(locationAbsIndex + duration) - gizmoSize * 5 / 4;
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

            if (highlightNoteAbsIndex < 0 || highlightNoteAbsIndex >= env.Length || highlightRepeatEnvelope || !AllowGizmos())
                return null;

            Envelope.GetMinMaxValueForType(editInstrument, editEnvelope, out int min, out int max);
            var midValue = (max + min) / 2;
            var value = env.Values[highlightNoteAbsIndex];

            var x = GetPixelXForAbsoluteNoteIndex(highlightNoteAbsIndex + 1) + gizmoSize / 4;
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

        private List<Gizmo> GetEnvelopeEffectsGizmos()
        {
            if (Platform.IsDesktop)
                return null;

            var env = EditEnvelope;
            var rep = EditRepeatEnvelope;

            if (highlightNoteAbsIndex < 0 || highlightNoteAbsIndex >= env.Length || !highlightRepeatEnvelope || rep == null || !AllowGizmos())
                return null;

            Envelope.GetMinMaxValueForType(editInstrument, EnvelopeType.WaveformRepeat, out int min, out int max);
            var val = rep.Values[highlightNoteAbsIndex];
            var mid = (min + max) / 2;
            var effectPosY = GetPixelYForEffectValue(val, min, max);

            var x = GetPixelXForAbsoluteNoteIndex(highlightNoteAbsIndex * env.ChunkLength + env.ChunkLength / 2) - gizmoSize / 2;
            var y = effectPanelSizeY - effectPosY + (val >= mid ? gizmoSize / 4 : -gizmoSize * 5 / 4);

            Gizmo repeatGizmo = new Gizmo();
            repeatGizmo.Image = bmpGizmoResizeUpDown;
            repeatGizmo.FillImage = bmpGizmoResizeFill;
            repeatGizmo.Action = GizmoAction.ChangeEnvEffectValue;
            repeatGizmo.Rect = new Rectangle(x, y, gizmoSize, gizmoSize);

            var list = new List<Gizmo>();
            list.Add(repeatGizmo);
            return list;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            UpdateCursor();

            if (captureOperation != CaptureOperation.None)
                return;

            if (e.Key == Keys.Escape)
            {
                ClearSelection();
                MarkDirty();
            }
            else if (IsActiveControl && Settings.SelectAllShortcut.Matches(e))
            {
                SelectAll();
            }
            else if (Settings.EffectPanelShortcut.Matches(e))
            {
                ToggleEffectPanel();
            }
            else if (Settings.MaximizePianoRollShortcut.Matches(e))
            {
                ToggleMaximize();
            }
            else if (e.Key >= Keys.D1 && e.Key <= Keys.D9 && e.Alt)
            {
                for (int i = SnapResolutionType.Min; i <= SnapResolutionType.Max; i++)
                {
                    if (SnapResolutionType.KeyboardShortcuts[i] == e.Key)
                    {
                        SetAndMarkDirty(ref snapResolution, i);
                        break;
                    }
                }
            }
            else if (Settings.SnapToggleShortcut.Matches(e))
            {
                if (SnapAllowed)
                {
                    snap = !snap;
                    MarkDirty();
                }
            }
            else if (IsActiveControl && IsSelectionValid())
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
                else if (Settings.DeleteShortcut.Matches(e))
                {
                    Delete();
                }
                else if (Settings.DeleteSpecialShortcut.Matches(e))
                {
                    DeleteSpecial();
                }

                if (editMode == EditionMode.Channel)
                {
                    switch (e.Key)
                    {
                        case Keys.Up:
                            TransposeNotes(e.Control ? 12 : 1);
                            break;
                        case Keys.Down:
                            TransposeNotes(e.Control ? -12 : -1);
                            break;
                        case Keys.Right:
                            MoveNotes(e.Control ? (Song.Project.UsesFamiTrackerTempo ? Song.BeatLength : Song.NoteLength) : 1);
                            break;
                        case Keys.Left:
                            MoveNotes(e.Control ? -(Song.Project.UsesFamiTrackerTempo ? Song.BeatLength : Song.NoteLength) : -1);
                            break;
                    }
                }
                else if (editMode == EditionMode.Envelope || editMode == EditionMode.Arpeggio)
                {
                    switch (e.Key)
                    {
                        case Keys.Up:
                            IncrementEnvelopeValues(e.Control ? 4 : 1);
                            break;
                        case Keys.Down:
                            IncrementEnvelopeValues(e.Control ? -4 : -1);
                            break;
                        case Keys.Right:
                            MoveEnvelopeValues(e.Control ? 4 : 1);
                            break;
                        case Keys.Left:
                            MoveEnvelopeValues(e.Control ? -4 : -1);
                            break;
                    }
                }
            }
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            UpdateCursor();
        }

        public int GetDPCMSampleMappingNoteAtPos(Point pos, out Instrument instrument)
        {
            if (editMode == EditionMode.DPCMMapping && GetNoteValueForCoord(pos.X, pos.Y, out var noteValue))
            {
                instrument = editInstrument;
                return noteValue;
            }

            instrument = null;
            return Note.NoteInvalid;
        }

        private bool EnsureSeekBarVisible(float percent = float.MinValue)
        {
            if (percent == float.MinValue)
                percent = Settings.FollowPercent;

            var seekX = GetPixelXForAbsoluteNoteIndex(App.CurrentFrame);
            var minX = 0;
            var maxX = (int)((Width - pianoSizeX) * percent);

            // Keep everything visible 
            if (seekX < minX)
                scrollX -= (minX - seekX);
            else if (seekX > maxX)
                scrollX += (seekX - maxX);

            ClampScroll();

            seekX = GetPixelXForAbsoluteNoteIndex(App.CurrentFrame);
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

        public void ToggleEffectPanel()
        {
            if (editMode == EditionMode.Channel || editMode == EditionMode.DPCM || editMode == EditionMode.Envelope && HasRepeatEnvelope())
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

        private bool HandleMouseDownPan(PointerEventArgs e)
        {
            bool middle = e.Middle || (e.Left && ModifierKeys.IsAltDown && Settings.AltLeftForMiddle);

            if (middle && e.Y > headerSizeY && e.X > pianoSizeX)
            {
                panning = true;
                CaptureMouse(e.X, e.Y);
                return true;
            }

            return false;
        }

        private bool HandleMouseDownScrollbar(PointerEventArgs e)
        {
            if (e.Left && scrollBarThickness > 0 && e.X > pianoSizeX && e.Y > headerAndEffectSizeY)
            {
                if (e.Y >= (Height - scrollBarThickness) && GetScrollBarParams(true, out var scrollBarThumbPosX, out var scrollBarThumbSizeX, out _))
                {
                    var x = e.X - pianoSizeX;
                    if (x < scrollBarThumbPosX)
                    {
                        scrollX -= (Width - pianoSizeX);
                        ClampScroll();
                        MarkDirty();
                    }
                    else if (x > (scrollBarThumbPosX + scrollBarThumbSizeX))
                    {
                        scrollX += (Width - pianoSizeX);
                        ClampScroll();
                        MarkDirty();
                    }
                    else if (x >= scrollBarThumbPosX && x <= (scrollBarThumbPosX + scrollBarThumbSizeX))
                    {
                        StartCaptureOperation(e.X, e.Y, CaptureOperation.ScrollBarX);
                    }
                    return true;
                }
                if (e.X >= (Width - scrollBarThickness) && GetScrollBarParams(false, out var scrollBarThumbPosY, out var scrollBarThumbSizeY, out _))
                {
                    var y = e.Y - headerAndEffectSizeY;
                    if (y < scrollBarThumbPosY)
                    {
                        scrollY -= (Height - headerAndEffectSizeY);
                        ClampScroll();
                        MarkDirty();
                    }
                    else if (y > (scrollBarThumbPosY + scrollBarThumbSizeY))
                    {
                        scrollX += (Height - headerAndEffectSizeY);
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

        private bool HandleMouseDownPiano(PointerEventArgs e)
        {
            if (e.Left && IsPointInPiano(e.X, e.Y))
            {
                StartPlayPiano(e.X, e.Y);
                return true;
            }

            return false;
        }

        private bool HandleMouseDownSeekBar(PointerEventArgs e)
        {
            if (e.Left && IsPointInHeader(e.X, e.Y))
            {
                StartCaptureOperation(e.X, e.Y, CaptureOperation.DragSeekBar);
                UpdateSeekDrag(e.X, e.Y, false);
                return true;
            }

            return false;
        }

        private bool HandleMouseDownHeaderSelection(PointerEventArgs e)
        {
            if (e.Right && IsPointInHeader(e.X, e.Y))
            {
                e.DelayRightClick(); // Need to wait and see if its a context menu click or not.
                return true;
            }

            return false;
        }

        private bool HandleMouseDownEnvelopeEffectPanel(PointerEventArgs e)
        {
            if (e.Left && HasRepeatEnvelope() && IsPointInEffectPanel(e.X, e.Y))
            {
                StartChangeEnvelopeRepeatValue(e.X, e.Y);
                return true;
            }

            return false;
        }

        private bool HandleMouseDownEnvelopeSelection(PointerEventArgs e)
        {
            if (e.Right && (IsPointInHeaderTopPart(e.X, e.Y) || IsPointInEffectPanel(e.X, e.Y) || IsPointInNoteArea(e.X, e.Y)))
            {
                e.DelayRightClick(); // Need to wait and see if its a context menu click or not.
                return true;
            }

            return false;
        }

        private int GetEffectIndexForPosition(int x, int y, int maxEffects)
        {
            if (IsPointInEffectList(x, y))
            {
                int effectIdx = (y - headerSizeY) / effectButtonSizeY;
                if (effectIdx >= 0 && effectIdx < maxEffects)
                {
                    return effectIdx;
                }
            }

            return -1;
        }

        private bool HandleMouseDownEffectList(PointerEventArgs e)
        {
            if (e.Left)
            {
                int effectIdx = GetEffectIndexForPosition(e.X, e.Y, supportedEffects.Length);
                if (effectIdx >= 0)
                {
                    selectedEffectIdx = supportedEffects[effectIdx];
                    MarkDirty();
                    return true;
                }
            }

            return false;
        }

        private bool HandleMouseDownAltZoom(PointerEventArgs e)
        {
            if (e.Right && ModifierKeys.IsAltDown && Settings.AltZoomAllowed)
            {
                StartCaptureOperation(e.X, e.Y, CaptureOperation.AltZoom);
                return true;
            }

            return false;
        }

        private void StartResizeEnvelope(int x, int y)
        {
            StartCaptureOperation(x, y, CaptureOperation.ResizeEnvelope);

            if (editMode == EditionMode.Envelope)
                App.UndoRedoManager.BeginTransaction(TransactionScope.Instrument, editInstrument.Id);
            else
                App.UndoRedoManager.BeginTransaction(TransactionScope.Arpeggio, editArpeggio.Id);

            ResizeEnvelope(x, y, false);
        }

        private bool HandleMouseDownEnvelopeResize(PointerEventArgs e)
        {
            if (e.Left && IsPointWhereCanResizeEnvelope(e.X, e.Y) && EditEnvelope.CanResize)
            {
                StartResizeEnvelope(e.X, e.Y);
                return true;
            }

            return false;
        }

        private bool HandleMouseDownEnvelopeLoopRelease(PointerEventArgs e)
        {
            var env = EditEnvelope;
            var rep = EditRepeatEnvelope;

            var canLoop    = env.CanLoop    || (rep != null && rep.CanLoop);
            var canRelease = env.CanRelease || (rep != null && rep.CanRelease);

            if (((e.Left && canLoop) || (e.Right && canRelease && EditEnvelope.Loop >= 0)) && IsPointInHeaderBottomPart(e.X, e.Y))
            {
                CaptureOperation op = e.Left ? CaptureOperation.DragLoop : CaptureOperation.DragRelease;
                StartCaptureOperation(e.X, e.Y, op);

                if (editMode == EditionMode.Envelope)
                    App.UndoRedoManager.BeginTransaction(TransactionScope.Instrument, editInstrument.Id);
                else
                    App.UndoRedoManager.BeginTransaction(TransactionScope.Arpeggio, editArpeggio.Id);

                ResizeEnvelope(e.X, e.Y, false);
                return true;
            }

            return false;
        }

        private void StartDrawEnvelope(int x, int y)
        {
            StartCaptureOperation(x, y, CaptureOperation.DrawEnvelope);

            if (editMode == EditionMode.Envelope)
                App.UndoRedoManager.BeginTransaction(TransactionScope.Instrument, editInstrument.Id);
            else
                App.UndoRedoManager.BeginTransaction(TransactionScope.Arpeggio, editArpeggio.Id);

            DrawEnvelope(x, y, true);
        }

        private bool HandleMouseDownDrawEnvelope(PointerEventArgs e)
        {
            if (e.Left && IsPointInNoteArea(e.X, e.Y) && EditEnvelope.Length > 0)
            {
                var noteIdx = GetAbsoluteNoteIndexForPixelX(e.X - pianoSizeX);

                if (IsNoteSelected(noteIdx))
                    StartChangeEnvelopeValue(e.X, e.Y);
                else
                    StartDrawEnvelope(e.X, e.Y);

                return true;
            }

            return false;
        }

        private bool HandleMouseDownEffectPanel(PointerEventArgs e)
        {
            if (selectedEffectIdx >= 0 && IsPointInEffectPanel(e.X, e.Y) && GetEffectNoteForCoord(e.X, e.Y, out var location))
            {
                if (e.Left)
                {
                    var slide = Settings.SlideNoteShortcut.IsKeyDown(ParentWindow);

                    if (slide && selectedEffectIdx == Note.EffectVolume)
                    {
                        StartDragVolumeSlide(e.X, e.Y, location);
                    }
                    else if (ModifierKeys.IsShiftDown)
                    {
                        ClearEffectValue(location);
                    }
                    else
                    {
                        StartChangeEffectValue(e.X, e.Y, location);
                    }

                    return true;
                }
                else if (e.Right)
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

        private bool HandleMouseDownDPCMVolumeEnvelope(PointerEventArgs e)
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

        private bool HandleMouseDownSnapButton(PointerEventArgs e)
        {
            if (e.Left && (IsPointOnSnapButton(e.X, e.Y) || IsPointOnSnapResolution(e.X, e.Y)))
            {
                snap = !snap;
                MarkDirty();
                return true;
            }

            return false;
        }

        private bool HandleMouseDownMaximizeButton(PointerEventArgs e)
        {
            if (e.Left && IsPointOnMaximizeButton(e.X, e.Y))
            {
                ToggleMaximize();
                MarkDirty();
                return true;
            }

            return false;
        }

        private bool HandleMouseDownToggleEffectPanelButton(PointerEventArgs e)
        {
            if (e.Left && IsPointOnToggleEffectPanelButton(e.X, e.Y))
            {
                ToggleEffectPanel();
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

        private bool HandleMouseDownWaveSelection(PointerEventArgs e)
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

        private bool HandleMouseDownChannelNote(PointerEventArgs e)
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
                    var delete  = ModifierKeys.IsShiftDown;
                    var release = Settings.ReleaseNoteShortcut.IsKeyDown(ParentWindow);
                    var stop    = Settings.StopNoteShortcut.IsKeyDown(ParentWindow);
                    var slide   = Settings.SlideNoteShortcut.IsKeyDown(ParentWindow);
                    var attack  = Settings.AttackShortcut.IsKeyDown(ParentWindow);
                    var eyedrop = Settings.EyeDropNoteShortcut.IsKeyDown(ParentWindow);
                    var setInst = Settings.SetNoteInstrumentShortcut.IsKeyDown(ParentWindow);

                    if (delete)
                    {
                        if (note != null)
                            DeleteSingleNote(noteLocation, mouseLocation, note);
                        StartCaptureOperation(e.X, e.Y, CaptureOperation.DeleteNotes);
                    }
                    else if (slide)
                    {
                        StartSlideNoteCreation(e.X, e.Y, noteLocation, note, noteValue);
                    }
                    else if (attack && note != null)
                    {
                        ToggleNoteAttack(noteLocation, note);
                    }
                    else if (setInst && note != null)
                    {
                        SetNoteInstrument(noteLocation, note, App.SelectedInstrument);
                    }
                    else if (eyedrop && note != null)
                    {
                        Eyedrop(note);
                    }
                    else if (release && note != null)
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
                            else if (
                                captureOp == CaptureOperation.MoveNoteRelease ||
                                captureOp == CaptureOperation.MoveSelectionNoteRelease)
                            {
                                StartMoveNoteRelease(e.X, e.Y, captureOp, noteLocation);
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

        private bool HandleMouseDownDPCMMapping(PointerEventArgs e)
        {
            if (e.Left && GetLocationForCoord(e.X, e.Y, out var location, out var noteValue))
            {
                var mapping = editInstrument.GetDPCMMapping(noteValue);

                if (mapping == null)
                {
                    MapDPCMSample(noteValue);
                }
                else if (ModifierKeys.IsShiftDown)
                {
                    ClearDPCMSampleMapping(noteValue);
                }
                else
                {
                    StartDragDPCMSampleMapping(e.X, e.Y, noteValue);
                }

                return true;
            }

            return false;
        }

        private bool HandleMouseDownNoteGizmos(PointerEventArgs e)
        {
            return e.Left && HandleNoteGizmos(e.X, e.Y);
        }

        private bool HandleMouseDownEffectGizmos(PointerEventArgs e)
        {
            return e.Left && HandleEffectsGizmos(e.X, e.Y);
        }

        protected override void OnPointerDown(PointerEventArgs e)
        {
            if (e.IsTouchEvent)
            {
                OnTouchDown(e);
                return;
            }

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

            if (editMode == EditionMode.Envelope || 
                editMode == EditionMode.Arpeggio)
            {
                if (HandleMouseDownEnvelopeEffectPanel(e)) goto Handled;
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
                editMode == EditionMode.DPCM    ||
                editMode == EditionMode.Envelope && HasRepeatEnvelope())
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

        private bool HandleMouseDownDelayedChannelNotes(PointerEventArgs e)
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

        private bool HandleMouseDownDelayedHeaderSelection(PointerEventArgs e)
        {
            if (e.Right && IsPointInHeader(e.X, e.Y))
            {
                StartSelection(e.X, e.Y);
                return true;
            }

            return false;
        }

        private bool HandleMouseDownDelayedEffectPanel(PointerEventArgs e)
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

        private bool HandleMouseDownDelayedEnvelopeSelection(PointerEventArgs e)
        {
            if (e.Right && (IsPointInHeaderTopPart(e.X, e.Y) || IsPointInEffectPanel(e.X, e.Y) || IsPointInNoteArea(e.X, e.Y)))
            {
                StartSelection(e.X, e.Y);
                return true;
            }

            return false;
        }

        private bool HandleMouseDownDelayedWaveSelection(PointerEventArgs e)
        {
            if (e.Right && (IsPointInNoteArea(e.X, e.Y) || IsPointInHeader(e.X, e.Y)))
            {
                StartSelectWave(e.X, e.Y);
                return true;
            }

            return false;
        }

        protected override void OnPointerDownDelayed(PointerEventArgs e)
        {
            if (editMode == EditionMode.Channel)
            {
                if (HandleMouseDownDelayedChannelNotes(e)) goto Handled;
                if (HandleMouseDownDelayedHeaderSelection(e)) goto Handled;
                if (HandleMouseDownDelayedEffectPanel(e)) goto Handled;
            }

            if (editMode == EditionMode.Envelope ||
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

            if (!channel.SupportsInstrument(App.SelectedInstrument, false))
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
            note.Instrument = App.SelectedInstrument;
            note.Arpeggio = channel.SupportsArpeggios ? App.SelectedArpeggio : null;

            SetMobileHighlightedNote(abs);
            MarkPatternDirty(pattern);
            App.UndoRedoManager.EndTransaction();

            return note;
        }

        private bool HandleTouchDownPan(int x, int y)
        {
            if (IsPointInNoteArea(x, y) || IsPointInEffectPanel(x, y))
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
            if (IsPointInHeader(x, y) && x < GetPixelXForAbsoluteNoteIndex(EditEnvelope.Length))
            {
                StartSelection(x, y);
                return true;
            }

            return false;
        }

        private bool HandleTouchDownEnvelopeResize(int x, int y)
        {
            if (IsPointInHeader(x, y) && EditEnvelope.CanResize && x > GetPixelXForAbsoluteNoteIndex(EditEnvelope.Length))
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
                                    StartNoteResizeEnd(x, y, IsNoteSelected(absNoteLocation) ? CaptureOperation.ResizeSelectionNoteEnd : CaptureOperation.ResizeNoteEnd, gizmoNoteLocation, g.OffsetX);
                                    break;
                                case GizmoAction.MoveRelease:
                                    StartMoveNoteRelease(x, y, IsNoteSelected(absNoteLocation) ? CaptureOperation.MoveSelectionNoteRelease : CaptureOperation.MoveNoteRelease, gizmoNoteLocation);
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


        private bool HandleEffectsGizmos(int x, int y)
        {
            if (IsPointInNoteArea(x, y) || IsPointInEffectPanel(x, y))
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
                var seekX = GetPixelXForAbsoluteNoteIndex(App.CurrentFrame) + pianoSizeX;

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

        private void UpdateChangeEnvelopeValue(int x, int y, bool final = false)
        {
            App.UndoRedoManager.RestoreTransaction(false);

            GetEnvelopeValueForCoord(x, y, out _, out var value);
            Envelope.GetMinMaxValueForType(editInstrument, editEnvelope, out int min, out int max);

            var env = EditEnvelope;
            var idx = Platform.IsMobile ? highlightNoteAbsIndex : captureNoteAbsoluteIdx;
            var originalValue = env.Values[idx];
            var delta = 0;

            if (Platform.IsDesktop)
            {
                delta = value - originalValue;
            }
            else // On mobile we drag using gizmos
            {
                delta = value - captureEnvelopeValue;
            }

            if (IsEnvelopeValueSelected(idx))
            {
                var scaling = originalValue == 0 ? 0.0f : Utils.Clamp(originalValue + delta, min, max) / MathF.Abs(originalValue);

                for (int i = selectionMin; i <= selectionMax; i++)
                {
                    if (relativeEffectScaling)
                        env.Values[i] = (sbyte)Utils.Clamp((int)Math.Round(env.Values[i] * scaling), min, max);
                    else
                        env.Values[i] = (sbyte)Utils.Clamp(env.Values[i] + delta, min, max);
                }
            }
            else
            {
                env.Values[idx] = (sbyte)Utils.Clamp(env.Values[idx] + delta, min, max);
            }

            if (final)
            {
                // Arps will have null instruments.
                if (editInstrument != null)
                {
                    editInstrument.NotifyEnvelopeChanged(editEnvelope, true);
                }

                EnvelopeChanged?.Invoke(editInstrument, editEnvelope);
                App.UndoRedoManager.EndTransaction();
            }

            MarkDirty();
        }

        private void StartChangeEnvelopeValue(int x, int y)
        {
            StartCaptureOperation(x, y, CaptureOperation.ChangeEnvelopeValue);

            if (editMode == EditionMode.Envelope)
                App.UndoRedoManager.BeginTransaction(TransactionScope.Instrument, editInstrument.Id);
            else
                App.UndoRedoManager.BeginTransaction(TransactionScope.Arpeggio, editArpeggio.Id);

            UpdateChangeEnvelopeValue(x, y);
        }

        private bool HandleTouchDownEnvelopeEffectsGizmos(int x, int y)
        {
            if (HasHighlightedNote() && highlightRepeatEnvelope && IsPointInEffectPanel(x, y))
            {
                var gizmos = GetEnvelopeEffectsGizmos();
                if (gizmos != null)
                {
                    foreach (var g in gizmos)
                    {
                        if (g.Rect.Contains(x - pianoSizeX, y - headerSizeY))
                        {
                            switch (g.Action)
                            {
                                case GizmoAction.ChangeEnvEffectValue:
                                    StartChangeEnvelopeRepeatValue(x, y);
                                    break;
                            }

                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private bool HandleTouchDownEnvelopeGizmos(int x, int y)
        {
            if (HasHighlightedNote() && !highlightRepeatEnvelope && IsPointInNoteArea(x, y))
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
                var mapping = editInstrument.GetDPCMMapping(noteValue);

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
            if ((IsPointInHeader(x, y) || IsPointInNoteArea(x, y)) && x < GetPixelXForAbsoluteNoteIndex(EditEnvelope.Length))
            {
                var absIdx = Utils.Clamp(GetAbsoluteNoteIndexForPixelX(x - pianoSizeX), 0, EditEnvelope.Length - 1);
                highlightNoteAbsIndex = absIdx == highlightNoteAbsIndex ? -1 : absIdx;
                highlightRepeatEnvelope = false;
                return true;
            }

            return false;
        }

        private bool HandleTouchClickEnvelopeEffectPanel(int x, int y)
        {
            if (HasRepeatEnvelope() && IsPointInEffectPanel(x, y))
            {
                var env = EditEnvelope;
                var absIdx = Utils.Clamp(GetAbsoluteNoteIndexForPixelX(x - pianoSizeX), 0, env.Length - 1) / env.ChunkLength;
                highlightNoteAbsIndex   = absIdx == highlightNoteAbsIndex && highlightRepeatEnvelope ? -1 : absIdx;
                highlightRepeatEnvelope = true;
                return true;
            }

            return false;
        }

        private bool HandleTouchClickToggleEffectPanelButton(int x, int y)
        {
            if (IsPointInTopLeftCorner(x, y))
            {
                ToggleEffectPanel();
                return true;
            }

            return false;
        }

        private bool HandleTouchClickDPCMMapping(int x, int y)
        {
            if (GetLocationForCoord(x, y, out _, out var noteValue))
            {
                var mapping = editInstrument.GetDPCMMapping(noteValue);

                if (mapping == null)
                {
                    MapDPCMSample(noteValue);
                    highlightDPCMSample = noteValue;
                }
                else
                {
                    highlightDPCMSample = highlightDPCMSample == noteValue ? -1 : noteValue;
                }

                return true;
            }

            return false;
        }
            
        private bool HandleTouchClickHeaderSeek(int x, int y)
        {
            if (IsPointInHeader(x, y))
            {
                var absNoteIndex = GetAbsoluteNoteIndexForPixelX(x - pianoSizeX);
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
            if (GetLocationForCoord(x, y, out var mouseLocation, out byte noteValue))
            {
                if (mouseLocation.PatternIndex >= Song.Length)
                    return true;

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
                    if (SnapEnabled && SnapEffectEnabled)
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

            Song.Channels[editChannel].SetNoteDurationToMaximumLength(
                NoteLocation.FromAbsoluteNoteIndex(Song, selectionMin),
                NoteLocation.FromAbsoluteNoteIndex(Song, selectionMax));

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
            note.Release = 0;
            note.Duration = 1;
            note.IsStop = true;
            note.Instrument = null;
            note.Arpeggio = null;
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

                if (IsNoteSelected(mouseLocation))
                {
                    menu.Add(new ContextMenuOption("MenuDeleteSelection", DeleteSelectedNotesContext, () => { DeleteSelectedNotes(); }));
                }

                if (note != null)
                {
                    menu.Insert(0, new ContextMenuOption("MenuDelete", DeleteNoteContext, () => { DeleteSingleNote(noteLocation, mouseLocation, note); }));

                    if (note.IsMusical)
                    {
                        if (channel.SupportsNoAttackNotes)
                            menu.Add(new ContextMenuOption("MenuToggleAttack", selection ? ToggleSelectedNoteAttackContext : ToggleNoteAttackContext, () => { ToggleNoteAttack(noteLocation, note); }, ContextMenuSeparator.Before));
                        if (channel.SupportsSlideNotes)
                            menu.Add(new ContextMenuOption("MenuToggleSlide", selection ? ToggleSelectedSlideNoteContext : ToggleSlideNoteContext, () => { ToggleSlideNote(noteLocation, note); }));
                        if (channel.SupportsReleaseNotes)
                            menu.Add(new ContextMenuOption("MenuToggleRelease", selection ? ToggleSelectedReleaseContext : ToggleReleaseContext, () => { ToggleNoteRelease(noteLocation, note); }));
                        if (channel.SupportsStopNotes)
                            menu.Add(new ContextMenuOption("MenuStopNote", MakeStopNoteContext, () => { ConvertToStopNote(noteLocation, note); }));
                        if (App.SelectedInstrument != null && Song.Channels[editChannel].SupportsInstrument(App.SelectedInstrument))
                            menu.Add(new ContextMenuOption("MenuReplaceSelection", ReplaceAllInstrumentContext.Format(App.SelectedInstrument), () => { ReplaceSelectionInstrument(App.SelectedInstrument, new Point(x, y), null); }));
                        if (App.SelectedInstrument != null && Song.Channels[editChannel].SupportsInstrument(App.SelectedInstrument) && note.Instrument != null)
                            menu.Add(new ContextMenuOption("MenuReplaceSelection", ReplaceSpecificInstrumentContext.Format(note.Instrument, App.SelectedInstrument), () => { ReplaceSelectionInstrument(App.SelectedInstrument, new Point(x, y), note.Instrument); }));
                        if (note.Instrument != null)
                            menu.Add(new ContextMenuOption("MenuEyedropper", MakeInstrumentCurrentContext, () => { Eyedrop(note); }));

                        var factor = GetBestSnapFactorForNote(noteLocation, note);
                        if (factor >= 0)
                            menu.Add(new ContextMenuOption("MenuSnap", SetSnapContext.Format(SnapResolutionType.Names[factor]), () => { snapResolution = factor; snap = true; MarkDirty(); }));
                    }

                    menu.Add(new ContextMenuOption("MenuSelectNote", SelectNoteRangeContext, () => { SelectSingleNote(noteLocation, mouseLocation, note); }, ContextMenuSeparator.Before));
                }
                else
                {
                    note = channel.FindMusicalNoteAtLocation(ref noteLocation, -1);

                    if (note != null)
                        menu.Add(new ContextMenuOption("MenuSelectNote", SelectNoteRangeContext, () => { SelectSingleNote(noteLocation, mouseLocation, note); }, ContextMenuSeparator.Before));
                }

                if (IsSelectionValid())
                {
                    menu.Add(new ContextMenuOption("MenuClearSelection", ClearSelectionContext, () => { ClearSelection(); ClearHighlightedNote(); }));
                }

                if (menu.Count > 0)
                    App.ShowContextMenuAsync(menu.ToArray());

                return true;
            }

            return false;
        }

        private bool HandleDoubleTapLongPressChannelNote(int x, int y)
        {
            HandleTouchDoubleClickChannelNote(x, y);
            StartCaptureOperation(x, y, CaptureOperation.DeleteNotes);
            Platform.VibrateClick();
            Platform.ShowToast(window, HoldFingersToEraseMessage);
            return true;
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
                    App.ShowContextMenuAsync(new[]
                    {
                        new ContextMenuOption("MenuSelectPattern", SelectPatternContext, () => { SelectPattern(location.PatternIndex); }),
                        new ContextMenuOption("MenuSelectAll", SelectAllContext, () => { SelectAll(); }),
                    });
                }
            }

            return false;
        }

        private bool HandleTouchLongPressChannelHeader(int x, int y)
        {
            return HandleContextMenuChannelHeader(x, y);
        }

        private void SetRelativeEffectScaling(bool rel)
        {
            if (rel != relativeEffectScaling)
            { 
                App.UndoRedoManager.BeginTransaction(TransactionScope.Application);
                relativeEffectScaling = rel;
                App.UndoRedoManager.EndTransaction();
                MarkDirty();
            }
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

                if (hasValue)
                {
                    menu.Add(new ContextMenuOption("Type", EnterEffectValueContext, () => { EnterEffectValue(x, y, location, note); }));
                    menu.Add(new ContextMenuOption("MenuDelete", ClearEffectValueContext, () => { ClearEffectValue(location, false); }, ContextMenuSeparator.After));
                }

                if (IsNoteSelected(location))
                {
                    menu.Add(new ContextMenuOption("MenuDeleteSelection", ClearSelectEffectValuesContext, () => { ClearEffectValue(location, true); }));
                    menu.Add(new ContextMenuOption("MenuDeleteSelection", DeleteSelectedNotesContext, () => { DeleteSelectedNotes(); }, ContextMenuSeparator.After));
                }

                if (IsSelectionValid())
                {
                    menu.Add(new ContextMenuOption("MenuClearSelection", ClearSelectionContext, () => { ClearSelection(); ClearHighlightedNote(); }, ContextMenuSeparator.After));
                }

                if (hasValue && selectedEffectIdx == Note.EffectVolume && channel.SupportsEffect(Note.EffectVolumeSlide))
                {
                    menu.Add(new ContextMenuOption("MenuToggleSlide", ToggleVolumeSlideContext, () => { ToggleVolumeSlide(location, note); }, ContextMenuSeparator.After));
                }

                if (IsSelectionValid())
                {
                    menu.Add(new ContextMenuOption("MenuCopy", CopyEffectValuesAsEnvValuesContext, () => { CopyEffectValues(false); }));

                    if (Platform.IsDesktop)
                    {
                        menu.Add(new ContextMenuOption("MenuCopy", CopyEffectValuesAsTextContext, () => { CopyEffectValues(true); }, ContextMenuSeparator.After));
                    }
                }

                menu.Add(new ContextMenuOption(AbsoluteEffectScalingContext, AbsoluteValueScalingContextTooltip, () => { SetRelativeEffectScaling(false); }, () => !relativeEffectScaling ? ContextMenuCheckState.Radio : ContextMenuCheckState.None, ContextMenuSeparator.MobileBefore));
                menu.Add(new ContextMenuOption(RelativeEffectScalingContext, RelativeValueScalingContextTooltip, () => { SetRelativeEffectScaling(true); },  () =>  relativeEffectScaling ? ContextMenuCheckState.Radio : ContextMenuCheckState.None, ContextMenuSeparator.After));

                if (menu.Count > 0)
                    App.ShowContextMenuAsync(menu.ToArray());

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
                Platform.ShowToast(window, HoldFingersToDrawMessage);
                StartDrawEnvelope(x, y);
                return true;
            }

            return false;
        }

        private void SetEnvelopeLoopRelease(int x, int y, bool release)
        {
            var env = EditEnvelope;
            var idx = Utils.RoundDown(GetAbsoluteNoteIndexForPixelX(x - pianoSizeX), env.ChunkLength);

            if (editMode == EditionMode.Envelope)
                App.UndoRedoManager.BeginTransaction(TransactionScope.Instrument, editInstrument.Id);
            else
                App.UndoRedoManager.BeginTransaction(TransactionScope.Arpeggio, editArpeggio.Id);

            if (release)
            {
                if (idx > 0)
                {
                    if (env.Loop < 0 || env.Loop >= idx)
                        env.Loop = idx - env.ChunkLength;
                    env.Release = idx;
                }
            }
            else
            {
                if (env.Release > 0)
                    env.Release = idx + env.ChunkLength;
                env.Loop = idx;
            }

            editInstrument?.NotifyEnvelopeChanged(editEnvelope, false);
            App.UndoRedoManager.EndTransaction();
        }

        private void ClearEnvelopeLoopRelease(bool release)
        {
            if (editMode == EditionMode.Envelope)
                App.UndoRedoManager.BeginTransaction(TransactionScope.Instrument, editInstrument.Id);
            else
                App.UndoRedoManager.BeginTransaction(TransactionScope.Arpeggio, editArpeggio.Id);

            var env = EditEnvelope;

            if (release)
                env.Release = -1;
            else
                env.Loop = -1;

            editInstrument?.NotifyEnvelopeChanged(editEnvelope, false);
            App.UndoRedoManager.EndTransaction();
        }

        private bool HandleContextMenuEnvelope(int x, int y)
        {
            if (Platform.IsMobile && IsPointInHeader(x, y) ||
                Platform.IsDesktop && (IsPointInHeaderTopPart(x, y) || IsPointInNoteArea(x,y)))
            {
                var env = EditEnvelope;
                var rep = EditRepeatEnvelope;
                var lastPixel = GetPixelXForAbsoluteNoteIndex(env.Length);
                var menu = new List<ContextMenuOption>();
                var absIdx = Utils.Clamp(GetAbsoluteNoteIndexForPixelX(x - pianoSizeX), 0, EditEnvelope.Length - 1);

                if ((editMode == EditionMode.Envelope || editMode == EditionMode.Arpeggio) && x < lastPixel)
                {
                    if (env.CanLoop || (rep != null && rep.CanLoop))
                    {
                        menu.Add(new ContextMenuOption("MenuLoopPoint", SetLoopPointContext, () => { SetEnvelopeLoopRelease(x, y, false); }));
                        if (env.Loop >= 0)
                            menu.Add(new ContextMenuOption("MenuClearLoopPoint", ClearLoopPointContext, () => { ClearEnvelopeLoopRelease(false); }));
                    }
                    if (env.CanRelease || (rep != null && rep.CanRelease))
                    {
                        if (absIdx > 0)
                            menu.Add(new ContextMenuOption("MenuRelease", SetReleasePointContext, () => { SetEnvelopeLoopRelease(x, y, true); }));
                        if (env.Release >= 0)
                            menu.Add(new ContextMenuOption("MenuClearRelease", ClearReleasePointContext, () => { ClearEnvelopeLoopRelease(true); }));
                    }
                }

                if (IsSelectionValid())
                {
                    if (GetEnvelopeValueForCoord(x, y, out int idx, out _) && idx < EditEnvelope.Length)
                        menu.Insert(0, new ContextMenuOption("MenuClearEnvelope", FlattenSelectionContext, () => { FlattenEnvelopeValues(idx); }, ContextMenuSeparator.After));

                    menu.Add(new ContextMenuOption("MenuClearSelection", ClearSelectionContext, () => { ClearSelection(); ClearHighlightedNote(); }, ContextMenuSeparator.Before));
                }

                if (Platform.IsDesktop && IsSelectionValid())
                {
                    menu.Add(new ContextMenuOption("MenuCopy", CopySelectedValuesAsTextContext, () => { CopyAsText(); }, ContextMenuSeparator.Before));
                }

                menu.Add(new ContextMenuOption(AbsoluteValueScalingContext, AbsoluteValueScalingContextTooltip, () => { SetRelativeEffectScaling(false); }, () => !relativeEffectScaling ? ContextMenuCheckState.Radio : ContextMenuCheckState.None, ContextMenuSeparator.MobileBefore));
                menu.Add(new ContextMenuOption(RelativeValueScalingContext, RelativeValueScalingContextTooltip, () => { SetRelativeEffectScaling(true); }, () => relativeEffectScaling ? ContextMenuCheckState.Radio : ContextMenuCheckState.None));

                if (menu.Count > 0)
                    App.ShowContextMenuAsync(menu.ToArray());

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
                    menu.Add(new ContextMenuOption("MenuClearEnvelope", ResetVertexContext, () => { ResetDPCMVolumeEnvelopeVertex(vertexIdx); }));
                menu.Add(new ContextMenuOption("MenuClearEnvelope", ResetVolumeEnvelopeContext, () => { ResetVolumeEnvelope(); }));
            }

            if (IsPointInNoteArea(x, y) && IsSelectionValid())
            {
                menu.Add(new ContextMenuOption("MenuDeleteSelection", DeleteSelectedSamplesContext, () => { DeleteSelectedWaveSection(); }, ContextMenuSeparator.Before));
                menu.Add(new ContextMenuOption("MenuClearSelection", ClearSelectionContext, () => { ClearSelection(); ClearHighlightedNote(); }, ContextMenuSeparator.Before));
            }

            if (menu.Count > 0)
                App.ShowContextMenuAsync(menu.ToArray());

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
                var mapping = editInstrument.GetDPCMMapping(noteValue);

                if (mapping != null)
                {
                    if (Platform.IsMobile)
                        highlightDPCMSample = noteValue;

                    App.ShowContextMenuAsync(new[]
                    {
                        new ContextMenuOption("MenuDelete", RemoveDPCMSampleContext, () => { ClearDPCMSampleMapping(noteValue); }),
                        new ContextMenuOption("MenuProperties", DPCMSamplePropertiesContext, () => { EditDPCMSampleMappingProperties(new Point(x, y), mapping); }),
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

        protected void OnTouchDown(PointerEventArgs e)
        {
            var x = e.X;
            var y = e.Y;

            Debug.WriteLine($"OnTouchDown {x} {y}");

            SetFlingVelocity(0, 0);
            SetMouseLastPos(x, y);
            
            // Special case, this operation is triggered on a double-tap, and a "TouchDown" is emitted
            // immediately after, so ignore the tap here.
            if (captureOperation == CaptureOperation.DeleteNotes)
            {
                return;
            }

            if (editMode == EditionMode.Channel)
            {
                if (HandleTouchDownDragSeekBar(x, y)) goto Handled;
                if (HandleTouchDownHeaderSelection(x, y)) goto Handled;
                if (HandleTouchDownNoteGizmos(x, y)) goto Handled;
                if (HandleTouchDownNoteEffectsGizmos(x, y)) goto Handled;
                if (HandleTouchDownDragNote(x, y)) goto Handled;
            }

            if (editMode == EditionMode.Envelope ||
                editMode == EditionMode.Arpeggio)
            {
                if (HandleTouchDownEnvelopeSelection(x, y)) goto Handled;
                if (HandleTouchDownEnvelopeResize(x, y)) goto Handled;
                if (HandleTouchDownEnvelopeEffectsGizmos(x, y)) goto Handled;
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

        protected void OnTouchMove(PointerEventArgs e)
        {
            var x = e.X;
            var y = e.Y;

            Debug.WriteLine($"OnTouchMove {x} {y} {captureOperation}");

            UpdateCaptureOperation(x, y);
            SetMouseLastPos(x, y);
        }

        protected void OnTouchUp(PointerEventArgs e)
        {
            var x = e.X;
            var y = e.Y;

            Debug.WriteLine($"OnTouchUp {x} {y} {captureOperation}");

            EndCaptureOperation(x, y);
            SetMouseLastPos(x, y);
        }

        protected override void OnTouchFling(PointerEventArgs e)
        {
            if (canFling)
            {
                EndCaptureOperation(e.X, e.Y);
                SetFlingVelocity(e.FlingVelocityX, e.FlingVelocityY);
            }
        }

        protected override void OnTouchScaleBegin(PointerEventArgs e)
        {
            if (captureOperation != CaptureOperation.None)
            {
                Debug.Assert(captureOperation != CaptureOperation.MobileZoomVertical && captureOperation != CaptureOperation.MobileZoom);
                AbortCaptureOperation();
            }

            var x = e.X;
            var y = e.Y;

            StartCaptureOperation(x, y, IsPointInPiano(x, y) ? CaptureOperation.MobileZoomVertical : CaptureOperation.MobileZoom);
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

            SetMouseLastPos(x, y);

            if (editMode == EditionMode.Channel)
            {
                if (HandleTouchClickHeaderSeek(x, y))  goto Handled;
                if (HandleTouchClickChannelNote(x, y)) goto Handled;
                if (HandleTouchClickEffectPanel(x, y)) goto Handled;
            }

            if (editMode == EditionMode.Envelope ||
                editMode == EditionMode.Arpeggio)
            {
                if (HandleTouchClickEnvelope(x, y)) goto Handled;
                if (HandleTouchClickEnvelopeEffectPanel(x, y)) goto Handled;
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

        protected override void OnTouchDoubleClick(PointerEventArgs e)
        {
            var x = e.X;
            var y = e.Y;

            Debug.WriteLine($"OnTouchDoubleClick {x} {y}");

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

        protected override void OnTouchLongPress(PointerEventArgs e)
        {
            var x = e.X;
            var y = e.Y;

            if (captureOperation == CaptureOperation.DeleteNotes                ||
                captureOperation == CaptureOperation.ChangeEnvelopeValue        ||
                captureOperation == CaptureOperation.ChangeEffectValue          ||
                captureOperation == CaptureOperation.ChangeSelectionEffectValue ||
                captureOperation == CaptureOperation.ChangeEnvelopeRepeatValue  ||
                captureOperation == CaptureOperation.PlayPiano                  ||
                captureOperation == CaptureOperation.ResizeNoteStart            ||
                captureOperation == CaptureOperation.ResizeSelectionNoteStart   ||
                captureOperation == CaptureOperation.ResizeNoteEnd              ||
                captureOperation == CaptureOperation.ResizeSelectionNoteEnd     ||
                captureOperation == CaptureOperation.DragSlideNoteTargetGizmo   ||
                captureOperation == CaptureOperation.DragVolumeSlideTargetGizmo ||
                captureOperation == CaptureOperation.MoveNoteRelease            ||
                captureOperation == CaptureOperation.MoveSelectionNoteRelease) 
            {
                return;
            }

            AbortCaptureOperation();

            if (e.IsDoubleTapLongPress)
            {
                if (editMode == EditionMode.Channel)
                {
                    if (HandleDoubleTapLongPressChannelNote(x, y)) goto Handled;
                }

                return;
            }

            if (editMode == EditionMode.Channel)
            {
                if (HandleTouchLongPressChannelNote(x, y)) goto Handled;
                if (HandleTouchLongPressEffectPanel(x, y)) goto Handled;
                if (HandleTouchLongPressChannelHeader(x, y)) goto Handled;
            }

            if (editMode == EditionMode.Envelope ||
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

            if (Platform.IsMobile && (editMode == EditionMode.Arpeggio || editMode == EditionMode.Envelope))
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
                maxScrollX = Math.Max(GetPixelXForAbsoluteNoteIndex(Song.GetPatternStartAbsoluteNoteIndex(Song.Length), false) - scrollMargin, 0);
            }
            else if (editMode == EditionMode.Envelope ||
                     editMode == EditionMode.Arpeggio)
            {
                maxScrollX = Math.Max(GetPixelXForAbsoluteNoteIndex(EditEnvelope.Length, false) - scrollMargin, 0);
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
                editMode == EditionMode.Envelope ||
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
            highlightRepeatEnvelope = false;
            highlightNoteAbsIndex = -1;
            highlightDPCMSample = -1;
        }

        private bool HasHighlightedNote()
        {
            return highlightNoteAbsIndex >= 0;
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
                int marginMinX = Platform.IsDesktop ? pianoSizeX : headerSizeY; 
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
            playLastNote = -1;
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

            int noteIdx = GetAbsoluteNoteIndexForPixelX(x - pianoSizeX);

            int minSelectionIdx = Math.Min(noteIdx, captureMouseAbsoluteIdx);
            int maxSelectionIdx = Math.Max(noteIdx, captureMouseAbsoluteIdx);
            int pad = SnapEnabled && !SnapTemporarelyDisabled ? -1 : 0;

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
            dragSeekPosition = GetAbsoluteNoteIndexForPixelX(x - pianoSizeX);
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
                    if (channel.SupportsInstrument(App.SelectedInstrument, false))
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
                        note.Instrument = App.SelectedInstrument;
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
                StartCaptureOperation(x, y, CaptureOperation.DragSlideNoteTargetGizmo, false, location.ToAbsoluteNoteIndex(Song), 0, offsetY);
            }
        }

        private void EnterEffectValue(int x, int y, NoteLocation location, Note note)
        {
            var channel = Song.Channels[editChannel];
            var pattern = channel.PatternInstances[location.PatternIndex];

            var val = note.GetEffectValue(selectedEffectIdx);
            var min = Note.GetEffectMinValue(Song, channel, selectedEffectIdx);
            var max = Note.GetEffectMaxValue(Song, channel, selectedEffectIdx);
            var def = Note.GetEffectDefaultValue(Song, selectedEffectIdx);
            var dlg = new ValueInputDialog(ParentWindow, new Point(left + x, top + y), EffectType.LocalizedNames[selectedEffectIdx], val, min, max, false);

            dlg.ShowDialogAsync((r) =>
            {
                if (r == DialogResult.OK)
                {
                    var noteSelected = IsNoteSelected(location);
                    var newVal = (int)dlg.Value;

                    if (noteSelected && SelectionCoversMultiplePatterns())
                        App.UndoRedoManager.BeginTransaction(TransactionScope.Channel, Song.Id, editChannel);
                    else
                        App.UndoRedoManager.BeginTransaction(TransactionScope.Pattern, pattern.Id);

                    if (noteSelected)
                    {
                        TransformNotes(selectionMin, selectionMax, false, true, false, (n, idx) =>
                        {
                            if (n != null && n.HasValidEffectValue(selectedEffectIdx))
                                n.SetEffectValue(selectedEffectIdx, newVal);
                            return n;
                        });
                    }
                    else
                    {
                        note.SetEffectValue(selectedEffectIdx, newVal);
                    }

                    App.UndoRedoManager.EndTransaction();
                    MarkDirty();
                }
            });
        }

        private void ClearEffectValue(NoteLocation location, bool allowSelection = false)
        {
            var pattern = Song.Channels[editChannel].PatternInstances[location.PatternIndex];
            var note = (Note)null;

            if (pattern != null && (allowSelection || pattern.TryGetNoteWithEffectAt(location.NoteIndex, selectedEffectIdx, out note)))
            {
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
                else if (note != null)
                {
                    note.ClearEffectValue(selectedEffectIdx);
                    MarkPatternDirty(location.PatternIndex);
                }

                App.UndoRedoManager.EndTransaction();
            }
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
                Platform.MessageBoxAsync(ParentWindow, NoDPCMSampleMessage, NoDPCMSampleTitle, MessageBoxButtons.OK);
            }
            else
            {
                var sampleNames = new List<string>();
                foreach (var sample in App.Project.Samples)
                    sampleNames.Add(sample.Name);

                var pitchStrings = DPCMSampleRate.GetStringList(true, FamiStudio.StaticInstance.PalPlayback, true, true);

                var dlg = new PropertyDialog(ParentWindow, AssignDPCMSampleTitle, 300);
                dlg.Properties.AddDropDownList(SelectSampleToAssignLabel.Colon, sampleNames.ToArray(), sampleNames[0], null, PropertyFlags.ForceFullWidth); // 0
                dlg.Properties.AddDropDownList(PitchLabel.Colon, pitchStrings, pitchStrings[pitchStrings.Length - 1]); // 1
                dlg.Properties.AddCheckBox(LoopLabel.Colon, false); // 2
                dlg.Properties.Build();

                dlg.ShowDialogAsync((r) =>
                {
                    if (r == DialogResult.OK)
                    {
                        App.UndoRedoManager.BeginTransaction(TransactionScope.Instrument, editInstrument.Id, -1, TransactionFlags.StopAudio);
                        var sampleName = dlg.Properties.GetPropertyValue<string>(0);
                        var mapping = editInstrument.MapDPCMSample(noteValue, App.Project.GetSample(sampleName));
                        mapping.Pitch = dlg.Properties.GetSelectedIndex(1);
                        mapping.Loop = dlg.Properties.GetPropertyValue<bool>(2);
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
            App.UndoRedoManager.BeginTransaction(TransactionScope.Instrument, editInstrument.Id, -1, TransactionFlags.StopAudio);
            editInstrument.UnmapDPCMSample(noteValue);
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
            MarkDirty();
        }

        private void SelectPattern(int p)
        {
            SetSelection(Song.GetPatternStartAbsoluteNoteIndex(p),
                         Song.GetPatternStartAbsoluteNoteIndex(p + 1) - 1);
            MarkDirty();
        }

        private void SelectAll()
        {
            if (editMode == EditionMode.Arpeggio ||
                editMode == EditionMode.Envelope)
            {
                SetSelection(0, EditEnvelope.Length - 1);
            }
            else if (editMode == EditionMode.Channel)
            {
                SetSelection(0, Song.GetPatternStartAbsoluteNoteIndex(Song.Length));
            }

            MarkDirty();
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
                // Preserve most effect values when deleting single notes.
                note.Clear();
                note.HasNoteDelay = false;
                note.HasCutDelay = false;
            }

            if (note.IsEmpty)
                pattern.Notes.Remove(noteLocation.NoteIndex);

            MarkPatternDirty(noteLocation.PatternIndex);
            App.UndoRedoManager.EndTransaction();
        }

        private void UpdateDeleteNotes(int x, int y)
        {
            if (GetLocationForCoord(x, y, out var mouseLocation, out byte noteValue) && mouseLocation.IsInSong(Song))
            {
                var channel = Song.Channels[editChannel];
                var noteLocation = mouseLocation;
                var note = channel.FindMusicalNoteAtLocation(ref noteLocation, noteValue);

                if (note != null)
                {
                    if (!App.UndoRedoManager.HasTransactionInProgress)
                        App.UndoRedoManager.BeginTransaction(TransactionScope.Channel, Song.Id, editChannel);

                    var pattern = Song.Channels[editChannel].PatternInstances[noteLocation.PatternIndex];

                    // Preserve most effect values when deleting single notes.
                    note.Clear();
                    note.HasNoteDelay = false;
                    note.HasCutDelay = false;

                    if (note.IsEmpty)
                        pattern.Notes.Remove(noteLocation.NoteIndex);

                    MarkPatternDirty(noteLocation.PatternIndex);
                }
            }

            if (Platform.IsMobile)
                MarkDirty();
        }

        private void EndDeleteNotes()
        {
            if (App.UndoRedoManager.HasTransactionInProgress)
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

        public void SetNoteInstrument(NoteLocation location, Note note, Instrument instrument)
        {
            var channel = Song.Channels[editChannel];

            if (channel.SupportsInstrument(instrument))
            {
                var pattern = channel.PatternInstances[location.PatternIndex];
                App.UndoRedoManager.BeginTransaction(TransactionScope.Pattern, pattern.Id);
                note.Instrument = instrument;
                MarkPatternDirty(pattern);
                App.UndoRedoManager.EndTransaction();
                MarkDirty();
            }
            else
            {
                App.ShowInstrumentError(channel, true);
            }
        }

        public void ReplaceSelectionInstrument(Instrument instrument, Point pos, Instrument matchInstrument = null, bool forceInSelection = false)
        {
            if (editMode == EditionMode.Channel)
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
                            if (note != null && note.IsMusical && (matchInstrument == null || note.Instrument == matchInstrument))
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

            var threshold = DpiScaling.ScaleForWindow(Platform.IsDesktop ? 10 : 20);

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
            return (editMode == EditionMode.Envelope || editMode == EditionMode.Arpeggio) && x > pianoSizeX && y > 0 && y < headerSizeY / 2;
        }

        private bool IsPointInHeaderBottomPart(int x, int y)
        {
            return (editMode == EditionMode.Envelope || editMode == EditionMode.Arpeggio) && x > pianoSizeX && y >= headerSizeY / 2 && y < headerSizeY;
        }

        private bool IsPointWhereCanResizeEnvelope(int x, int y)
        {
            var pixel0 = GetPixelXForAbsoluteNoteIndex(EditEnvelope.Length) + pianoSizeX;
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
            return showEffectsPanel && (editMode == EditionMode.Channel || editMode == EditionMode.DPCM || editMode == EditionMode.Envelope && HasRepeatEnvelope()) && x > pianoSizeX && y > headerSizeY && y < headerAndEffectSizeY;
        }

        private bool IsPointInNoteArea(int x, int y)
        {
            return y > headerAndEffectSizeY && x > pianoSizeX;
        }

        private bool IsPointInTopLeftCorner(int x, int y)
        {
            return (editMode == EditionMode.Channel || editMode == EditionMode.DPCM) && y < headerSizeY && x < pianoSizeX;
        }

        private Rectangle GetToggleEffectPanelButtonRect()
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
            var toggleRect = GetToggleEffectPanelButtonRect();
            var snapRect   = GetSnapButtonRect();
            return new Rectangle(toggleRect.Right, toggleRect.Top + 1, snapRect.Left - toggleRect.Right - (int)DpiScaling.Window, snapRect.Height);
        }

        private bool IsPointOnToggleEffectPanelButton(int x, int y)
        {
            return GetToggleEffectPanelButtonRect().Contains(x, y);
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

        private void UpdateToolTip(PointerEventArgs e)
        {
            var tooltip = "";
            var newNoteTooltip = "";

            if (IsPointInHeader(e.X, e.Y) && editMode == EditionMode.Channel)
            {
                tooltip = $"<MouseLeft> {SeekTooltip} - <MouseRight><Drag> {SelectTooltip} - <MouseRight> {MoreOptionsTooltip}";
            }
            else if (IsPointInHeaderTopPart(e.X, e.Y) && (editMode == EditionMode.Envelope || editMode == EditionMode.Arpeggio))
            {
                if (IsPointWhereCanResizeEnvelope(e.X, e.Y))
                    tooltip = $"<MouseLeft> {ResizeEnvelopeTooltip}\n";
                else
                    tooltip = $"<MouseRight><Drag> {SelectTooltip}";
            }
            else if (IsPointInHeaderBottomPart(e.X, e.Y) && ((editMode == EditionMode.Envelope && EditEnvelope.CanLoop) || editMode == EditionMode.Arpeggio))
            {
                tooltip = $"<MouseLeft> {SetLoopPointTooltip}" + ((editMode != EditionMode.Arpeggio && EditEnvelope.CanRelease) ? $"\n<MouseRight> {SetReleasePointTooltip} {MustHaveLoopPointTooltip}" : "");
            }
            else if (IsPointInPiano(e.X, e.Y))
            {
                tooltip = $"<MouseLeft> {PlayPianoTooltip} - <MouseWheel> {PanTooltip}";
            }
            else if (IsPointOnSnapResolution(e.X, e.Y) || IsPointOnSnapButton(e.X, e.Y))
            {
                tooltip = $"<MouseLeft> {ToggleSnappingTooltip} {Settings.SnapToggleShortcut.TooltipString} - <MouseWheel> {ChangeSnapPrecisionTooltip}\n<MouseRight> {MoreOptionsTooltip}";
            }
            else if (IsPointOnMaximizeButton(e.X, e.Y))
            {
                tooltip = $"<MouseLeft> {MaximizePianoRollTooltip} {Settings.MaximizePianoRollShortcut.TooltipString}";
            }
            else if (IsPointInTopLeftCorner(e.X, e.Y))
            {
                tooltip = $"<MouseLeft> {ShowHideEffectPanelTooltip} {Settings.EffectPanelShortcut.TooltipString}";
            }
            else if (IsPointInEffectList(e.X, e.Y))
            {
                tooltip = $"<MouseLeft> {SelectEffectToEditTooltip}";
            }
            else if (IsPointInEffectPanel(e.X, e.Y))
            {
                if (editMode == EditionMode.Channel)
                {
                    tooltip = $"<MouseLeft> {SetEffectValueTooltip} - <MouseWheel> {PanTooltip}\n<Ctrl><MouseLeft> {SetEffectValueFineTooltip} - <MouseLeft><MouseLeft> {OrTooltip} <Shift><MouseLeft> {ClearEffectValueTooltip}";
                }
                else if (editMode == EditionMode.DPCM)
                {
                    if (GetWaveVolumeEnvelopeVertexIndex(e.X, e.Y) >= 0)
                    {
                        tooltip = $"<MouseLeft><Drag> {MoveVolEnvVertexTooltip}\n<MouseRight> {MoreOptionsTooltip}";
                    }
                }
                else if (editMode == EditionMode.Envelope)
                {
                    tooltip = $"<MouseLeft> {SetEffectValueTooltip} - <MouseWheel> {PanTooltip}\n<MouseRight> {MoreOptionsTooltip}";
                }
            }
            else if ((IsPointInNoteArea(e.X, e.Y) || IsPointInHeader(e.X, e.Y)) && editMode == EditionMode.DPCM)
            {
                tooltip = $"<MouseLeft><Drag> {OrTooltip} <MouseRight><Drag> {SelectSamplesFromSourceTooltip}";

                if (IsSelectionValid())
                {
                    tooltip += $"\n{Settings.DeleteShortcut.TooltipString} {DeleteSelectedSampleTooltip}";
                    newNoteTooltip = SamplesSelectedTooltip.Format(selectionMax - selectionMin + 1);
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
                                newNoteTooltip += $" ({ArpeggioTooltip}: {note.Arpeggio.Name})";
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
                                tooltipList.Add($"<MouseLeft><Drag> {ResizeNotesTooltip}");
                                break;
                            case CaptureOperation.MoveNoteRelease:
                                tooltipList.Add($"<MouseLeft><Drag> {MoveReleasePointTooltip}");
                                break;
                            case CaptureOperation.DragNote:
                            case CaptureOperation.DragSelection:
                                tooltipList.Add($"<MouseLeft><Drag> {MoveNotesTooltip}");
                                break;
                            default:
                                tooltipList.Add($"<MouseLeft><Drag> {CreateNoteTooltip}");
                                break;
                        }

                        if (note != null)
                        {
                            if (channel.SupportsReleaseNotes && captureOp != CaptureOperation.MoveNoteRelease && Settings.ReleaseNoteShortcut.IsShortcutValid(0))
                                tooltipList.Add($"{Settings.ReleaseNoteShortcut.TooltipString}<MouseLeft> {SetReleasePointTooltip}");
                            if (channel.SupportsSlideNotes && Settings.SlideNoteShortcut.IsShortcutValid(0))
                                tooltipList.Add($"{Settings.SlideNoteShortcut.TooltipString}<MouseLeft><Drag> {SlideNoteTooltip}");
                            if (note.IsMusical)
                            {
                                if (Settings.AttackShortcut.IsShortcutValid(0))
                                    tooltipList.Add($"{Settings.AttackShortcut.TooltipString}<MouseLeft> {ToggleAttackTooltip}");
                                if (Settings.EyeDropNoteShortcut.IsShortcutValid(0))
                                    tooltipList.Add($"{Settings.EyeDropNoteShortcut.TooltipString}<MouseLeft> {InstrumentEyedropTooltip}");
                                if (Settings.SetNoteInstrumentShortcut.IsShortcutValid(0))
                                    tooltipList.Add($"{Settings.SetNoteInstrumentShortcut.TooltipString}<MouseLeft> {SetNoteInstrumentTooltip}");
                            }
                            tooltipList.Add($"<MouseLeft><MouseLeft> {OrTooltip} <Shift><MouseLeft> {DeleteNoteTooltip}");
                        }
                        else 
                        {
                            if (channel.SupportsStopNotes && Settings.StopNoteShortcut.IsShortcutValid(0))
                                tooltipList.Add($"{Settings.StopNoteShortcut.TooltipString}<MouseLeft> {AddStopNoteTooltip}");
                        }

                        tooltipList.Add($"<MouseWheel> {PanTooltip}");

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

                        newNoteTooltip += $"{(selectionMax - selectionMin + 1)}{(Song.Project.UsesFamiTrackerTempo ? " note" : " frame")}" + ((selectionMax - selectionMin) == 0 ? "" : "s") + " selected";
                    }
                }
                else if (editMode == EditionMode.Envelope || editMode == EditionMode.Arpeggio)
                {
                    tooltip = $"<MouseLeft> {SetEnvelopeValueTooltip} - <MouseWheel> {PanTooltip}\n<MouseRight> {MoreOptionsTooltip}";

                    if (GetEnvelopeValueForCoord(e.X, e.Y, out int idx, out sbyte value))
                    {
                        newNoteTooltip = $"{idx:D3} : {value}";

                        if (IsSelectionValid())
                        {
                            var numValuesSelected = selectionMax - selectionMin + 1;

                            switch (editEnvelope)
                            {
                                case EnvelopeType.FdsWaveform:
                                case EnvelopeType.N163Waveform:
                                    newNoteTooltip += $" ({SamplesSelectedTooltip.Format(numValuesSelected)})";
                                    break;
                                case EnvelopeType.FdsModulation:
                                    newNoteTooltip += $" ({ValuesSelectedTooltip.Format(numValuesSelected)})";
                                    break;
                                default:
                                    newNoteTooltip += $" ({FramesSelectedTooltip.Format(numValuesSelected)})";
                                    break;

                            }
                        }
                    }
                }

                else if (editMode == EditionMode.DPCMMapping)
                {
                    if (GetNoteValueForCoord(e.X, e.Y, out byte noteValue))
                    {
                        newNoteTooltip = $"{Note.GetFriendlyName(noteValue)}";

                        var mapping = editInstrument.GetDPCMMapping(noteValue);
                        if (mapping == null)
                        {
                            tooltip = $"<MouseLeft> {AssignDPCMSampleTooltip} - <MouseWheel> {PanTooltip}";
                        }
                        else
                        {
                            tooltip = $"<MouseLeft><MouseLeft> {SamplePropertiesTooltip} - <MouseWheel> {PanTooltip}\n<MouseRight> {MoreOptionsTooltip}";

                            if (mapping.Sample != null)
                                newNoteTooltip += $" ({mapping.Sample.Name})";
                        }
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
            if (SnapEnabled && !SnapTemporarelyDisabled || forceSnap)
            {
                var negativeOffset = 0;
                var firstPatternLen = Song.GetPatternLength(0);

                // If we get a negative note, we will assume that the non-existant "negative"
                // patterns are infinite copies of the first pattern.
                while (absoluteNoteIndex < 0)
                {
                    absoluteNoteIndex += firstPatternLen;
                    negativeOffset    += firstPatternLen;
                }

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

                snappedNoteIndex -= negativeOffset;

                if (!roundUp)
                    snappedNoteIndex = Math.Min(Song.GetPatternLength(location.PatternIndex) - 1, snappedNoteIndex);

                return Song.GetPatternStartAbsoluteNoteIndex(location.PatternIndex, snappedNoteIndex);
            }
            else
            {
                return absoluteNoteIndex;
            }
        }

        private NoteLocation SnapNote(NoteLocation location, bool roundUp = false, bool forceSnap = false)
        {
            return NoteLocation.FromAbsoluteNoteIndex(Song, SnapNote(location.ToAbsoluteNoteIndex(Song), roundUp, forceSnap));
        }

        private int GetBestSnapFactorForNote(NoteLocation location, Note note)
        {
            if (note.IsMusical)
            {
                var beatLength = Song.GetPatternBeatLength(location.PatternIndex);
                var noteDuration = GetVisualNoteDuration(location, note);
                var factor = noteDuration / (float)beatLength;

                for (var i = SnapResolutionType.Min; i <= SnapResolutionType.Max; i++)
                {
                    var testFactor = (float)SnapResolutionType.Factors[i];

                    if (testFactor >= factor || Utils.IsNearlyEqual(testFactor, factor))
                        return i;
                }

                return SnapResolutionType.Max;
            }

            return -1;
        }

        private void SetSnappingFromNoteDuration(NoteLocation location, Note note)
        {
            var bestSnap = GetBestSnapFactorForNote(location, note);
            
            if (bestSnap >= SnapResolutionType.Min &&
                bestSnap <= SnapResolutionType.Max)
            {
                SetAndMarkDirty(ref snapResolution, bestSnap);
            }
        }

        private void StartNoteCreation(PointerEventArgs e, NoteLocation location, byte noteValue)
        { 
            var channel = Song.Channels[editChannel];

            if (channel.SupportsInstrument(App.SelectedInstrument, false))
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
            GetLocationForCoord(x, y, out var location, out _);

            if (!first)
            {
                // Need to cancel the transaction every time since the start pattern may change.
                App.UndoRedoManager.RestoreTransaction(false);
                App.UndoRedoManager.AbortTransaction();
            }

            var minLocation = SnapNote(NoteLocation.Min(location, captureNoteLocation), false);
            var maxLocation = SnapNote(NoteLocation.Max(location, captureNoteLocation), true);

            var channel = Song.Channels[editChannel];
            var pattern = channel.PatternInstances[minLocation.PatternIndex];
            var minAbsoluteNoteIndex = minLocation.ToAbsoluteNoteIndex(Song);
            var maxAbsoluteNoteIndex = maxLocation.ToAbsoluteNoteIndex(Song);

            highlightNoteAbsIndex = minAbsoluteNoteIndex;

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
            note.Instrument = App.SelectedInstrument;
            note.Arpeggio = Song.Channels[editChannel].SupportsArpeggios ? App.SelectedArpeggio : null;
            note.Duration = Math.Max(1, maxAbsoluteNoteIndex) - minAbsoluteNoteIndex;

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
            GetLocationForCoord(x, y, out var location, out var noteValue);

            var deltaPosX = x - captureMouseX;

            var resizeStart = captureOperation == CaptureOperation.ResizeNoteStart || captureOperation == CaptureOperation.ResizeSelectionNoteStart;
            var resizeNote = channel.GetNoteAt(captureNoteLocation);
            var deltaNoteIdx = 0;

            if (!resizeStart)
            {
                // Apply raw delta to note position, then snap that to the grid.
                var newCaptureNoteAbsNoteIndex = captureNoteAbsoluteIdx + location.ToAbsoluteNoteIndex(Song) - captureMouseAbsoluteIdx;

                // Compute snapping by rounding up/down, and use the one that is closest to the original position.
                // This fixes the issue where notes are always attracted "left" since we always round down.
                var deltaNoteIdxSnapRoundDown = SnapNote(newCaptureNoteAbsNoteIndex, false) - captureNoteAbsoluteIdx;
                var deltaNoteIdxSnapRoundUp   = SnapNote(newCaptureNoteAbsNoteIndex, true)  - captureNoteAbsoluteIdx;

                if (deltaPosX < 0)
                    deltaNoteIdx = Math.Max(deltaNoteIdxSnapRoundDown, deltaNoteIdxSnapRoundUp);
                else
                    deltaNoteIdx = Math.Min(deltaNoteIdxSnapRoundDown, deltaNoteIdxSnapRoundUp);
            }
            else 
            {
                // The snapped position of the mouse is the new note start.
                var snappedLocation = NoteLocation.Min(SnapNote(captureNoteLocation.Advance(Song, resizeNote.Duration - 1)), SnapNote(location));
                deltaNoteIdx = snappedLocation.ToAbsoluteNoteIndex(Song) - captureNoteAbsoluteIdx;
            }

            // Don't allow snapping to move stuff in the opposite side of the mouse movement. Feels janky.
            if (Math.Sign(deltaPosX) != Math.Sign(deltaNoteIdx))
            {
                deltaNoteIdx = 0;
            }

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

                var copy = ModifierKeys.IsControlDown;
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

        private void StartNoteResizeEnd(int x, int y, CaptureOperation captureOp, NoteLocation location, int offsetX = 0)
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

            StartCaptureOperation(x, y, captureOp, true, location.ToAbsoluteNoteIndex(Song), offsetX);
        }

        private void UpdateNoteResizeEnd(int x, int y, bool final)
        {
            var channel = Song.Channels[editChannel];

            // HACK : When tapping quickly on a resize gizmo, ignore the 
            // change since we are likely trying to draw a note after.
            if (Platform.IsMobile && !captureThresholdMet)
            {
                App.UndoRedoManager.AbortTransaction();
                return;
            }

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
            GetLocationForCoord(x, y, out var location, out var noteValue);

            // We are resizing the highlighted note, but apply the delta to all other notes.
            var resizeNote = channel.GetNoteAt(captureNoteLocation);
            var resizeNoteEnd = captureNoteAbsoluteIdx + resizeNote.Duration;
            var snappedLocation = NoteLocation.Max(SnapNote(location, Platform.IsDesktop), SnapNote(captureNoteLocation, true));
            var deltaNoteIdx = snappedLocation.ToAbsoluteNoteIndex(Song) - resizeNoteEnd;
            var processedNotes = new HashSet<Note>();

            TransformNotes(min, max, false, final, false, (note, idx) =>
            {
                if (note != null && note.IsMusical && !processedNotes.Contains(note))
                {
                    // HACK : Try to preserve releases.
                    var hadRelease = note.HasRelease;
                    note.Duration = (ushort)Math.Max(1, note.Duration + deltaNoteIdx);
                    if (hadRelease && !note.HasRelease && note.Duration > 1)
                        note.Release = note.Duration - 1;
                }

                processedNotes.Add(note);
                return note;
            });

            if (final)
            {
                App.UndoRedoManager.EndTransaction();
            }
        }

        private void StartMoveNoteRelease(int x, int y, CaptureOperation op, NoteLocation location)
        {
            var minPatternIdx = Song.PatternIndexFromAbsoluteNoteIndex(selectionMin);
            var maxPatternIdx = Song.PatternIndexFromAbsoluteNoteIndex(selectionMax);
            var pattern = Song.Channels[editChannel].PatternInstances[location.PatternIndex];

            if (minPatternIdx != maxPatternIdx)
            {
                App.UndoRedoManager.BeginTransaction(TransactionScope.Channel, Song.Id, editChannel);
            }
            else
            {
                App.UndoRedoManager.BeginTransaction(TransactionScope.Pattern, pattern.Id);
            }

            StartCaptureOperation(x, y, op, false, location.ToAbsoluteNoteIndex(Song));
        }

        private void UpdateMoveNoteRelease(int x, int y, bool final)
        {
            GetLocationForCoord(x, y, out var location, out var noteValue, true);

            var selection = captureOperation == CaptureOperation.MoveSelectionNoteRelease;

            if (selection)
            {
                App.UndoRedoManager.RestoreTransaction(false);
            }

            var channel = Song.Channels[editChannel];
            var pattern = channel.PatternInstances[captureNoteLocation.PatternIndex];

            // Move the release for the highlighted note.
            var highlightedNote = pattern.Notes[captureNoteLocation.NoteIndex];
            var newRelease = Song.CountNotesBetween(captureNoteLocation, location);
            var delta = newRelease - highlightedNote.Release;
            highlightedNote.Release = (ushort)Utils.Clamp(newRelease, 1, highlightedNote.Duration - 1);
            channel.InvalidateCumulativePatternCache(pattern);

            // Then apply same delta to every other selected note.
            if (selection)
            { 
                var min = selection ? selectionMin : captureNoteAbsoluteIdx;
                var max = selection ? selectionMax : captureNoteAbsoluteIdx;
                var processedNotes = new HashSet<Note>();

                TransformNotes(min, max, false, final, false, (note, idx) =>
                {
                    if (note != null && note != highlightedNote && note.IsMusical && note.HasRelease && !processedNotes.Contains(note))
                        note.Release = Utils.Clamp(note.Release + delta, 1, note.Duration - 1);

                    processedNotes.Add(note);
                    return note;
                });
            }

            if (final)
            {
                App.UndoRedoManager.EndTransaction();
            }

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

                    var minNoteCoordX = GetPixelXForAbsoluteNoteIndex(minAbsoluteNoteIdx);
                    var maxNoteCoordX = GetPixelXForAbsoluteNoteIndex(maxAbsoluteNoteIdx);

                    x -= pianoSizeX;

                    if (x > maxNoteCoordX - noteResizeMargin)
                        return IsNoteSelected(noteLocation) ? CaptureOperation.ResizeSelectionNoteEnd : CaptureOperation.ResizeNoteEnd;
                    if (x < minNoteCoordX + noteResizeMargin)
                        return IsNoteSelected(noteLocation) ? CaptureOperation.ResizeSelectionNoteStart : CaptureOperation.ResizeNoteStart;

                    if (note.HasRelease && Song.CountNotesBetween(noteLocation, mouseLocation) == note.Release)
                        return IsNoteSelected(noteLocation) ? CaptureOperation.MoveSelectionNoteRelease : CaptureOperation.MoveNoteRelease;
                }

                return IsNoteSelected(noteLocation) ? CaptureOperation.DragSelection : CaptureOperation.DragNote;
            }

            return CaptureOperation.None;
        }

        private void UpdateCursor()
        {
            var pt = ScreenToControl(CursorPosition);
            var noteIdx = GetAbsoluteNoteIndexForPixelX(pt.X - pianoSizeX);

            if (EditEnvelope != null && EditEnvelope.CanResize && IsPointWhereCanResizeEnvelope(pt.X, pt.Y) && captureOperation != CaptureOperation.Select || captureOperation == CaptureOperation.ResizeEnvelope)
            {
                Cursor = Cursors.SizeWE;
            }
            else if (captureOperation == CaptureOperation.ChangeEffectValue ||
                     captureOperation == CaptureOperation.ChangeEnvelopeRepeatValue ||
                     HasRepeatEnvelope() && IsPointInEffectPanel(pt.X, pt.Y))
            {
                Cursor = Cursors.SizeNS;
            }
            else if ((EditEnvelope != null && IsNoteSelected(noteIdx)) || captureOperation == CaptureOperation.ChangeEnvelopeValue)
            {
                Cursor = Cursors.SizeNS;
            }
            else if (ModifierKeys.IsControlDown && (captureOperation == CaptureOperation.DragNote || captureOperation == CaptureOperation.DragSelection))
            {
                Cursor = Cursors.CopyCursor;
            }
            else if (captureOperation == CaptureOperation.DeleteNotes)
            {
                Cursor = Cursors.Eraser;
            }
            else if (editMode == EditionMode.Channel && Settings.EyeDropNoteShortcut.IsKeyDown(ParentWindow))
            {
                Cursor = Cursors.Eyedrop;
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
                                Cursor = Cursors.SizeNS;
                                break;
                            default:
                                Cursor = Cursors.Default;
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
                                    Cursor = Cursors.SizeNS;
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
                            case CaptureOperation.MoveSelectionNoteRelease:
                                Cursor = Cursors.SizeWE;
                                break;
                            case CaptureOperation.DragNote:
                            case CaptureOperation.DragSelection:
                                Cursor = Cursors.Move;
                                break;
                            default:
                                Cursor = Cursors.Default;
                                break;
                        }
                    }
                }
                else
                {
                    Cursor = Cursors.Default;
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
            UpdateHover(e);

            if (middle)
                DoScroll(e.X - mouseLastX, e.Y - mouseLastY);

            UpdateToolTip(e);
            SetMouseLastPos(e.X, e.Y);
            MarkDirty(); // TODO : This is bad.

            App.SequencerShowExpansionIcons = false;
        }

        protected override void OnPointerLeave(EventArgs e)
        {
            ClearHover();
        }

        private void UpdateHover(PointerEventArgs e)
        {
            if (Platform.IsDesktop)
            {
                var newHoverNote  = -1;
                var newHoverNoteIndex = GetAbsoluteNoteIndexForPixelX(e.X - pianoSizeX);
                var newHoverNoteCount = 1;
                var newHoverEffectIndex = -1;
                var newHoverTopLeftButton = 0;

                if (editMode == EditionMode.Channel)
                {
                    GetLocationForCoord(e.X, e.Y, out var location, out var noteValue, true);
                    newHoverNote  = noteValue;
                    newHoverNoteIndex = location.ToAbsoluteNoteIndex(Song);

                    // This is super lame, advance until we find the next snapping boundary.
                    // We cant just advance by (beat length) * (snap precision) because we have
                    // a bunch of crazy rules in there.
                    if (SnapEnabled)
                    {
                        var newHoverNoteIndex2 = newHoverNoteIndex + 1;

                        for (int i = 1; ; i++)
                        {
                            var newAbsIndex = SnapNote(newHoverNoteIndex + i);
                            if (newAbsIndex != newHoverNoteIndex)
                            {
                                newHoverNoteIndex2 = newAbsIndex;
                                break;
                            }
                        }

                        newHoverNoteCount = newHoverNoteIndex2 - newHoverNoteIndex;
                    }

                    newHoverEffectIndex = showEffectsPanel ? GetEffectIndexForPosition(e.X, e.Y, supportedEffects.Length) : -1;
                }

                if (editMode == EditionMode.Channel ||
                    editMode == EditionMode.DPCM    ||
                    editMode == EditionMode.Envelope && HasRepeatEnvelope())
                {
                    newHoverTopLeftButton |= IsPointOnToggleEffectPanelButton(e.X, e.Y) ? 1 : 0;
                }

                if (SnapAllowed)
                {
                    newHoverTopLeftButton |= IsPointOnSnapButton(e.X, e.Y) || IsPointOnSnapResolution(e.X, e.Y) ? 2 : 0;
                }

                newHoverTopLeftButton |= IsPointOnMaximizeButton(e.X, e.Y) ? 4 : 0;

                SetAndMarkDirty(ref hoverPianoNote,     newHoverNote);
                SetAndMarkDirty(ref hoverNoteIndex,     newHoverNoteIndex);
                SetAndMarkDirty(ref hoverNoteCount,     newHoverNoteCount);
                SetAndMarkDirty(ref hoverEffectIndex,   newHoverEffectIndex);
                SetAndMarkDirty(ref hoverTopLeftButton, newHoverTopLeftButton);
            }
        }

        private void ClearHover()
        {
            if (Platform.IsDesktop)
            {
                SetAndMarkDirty(ref hoverPianoNote, -1);
                SetAndMarkDirty(ref hoverNoteIndex, -1);
                SetAndMarkDirty(ref hoverEffectIndex, -1);
                SetAndMarkDirty(ref hoverTopLeftButton, -1);
            }
        }

        private bool HandleMouseUpSnapResolution(PointerEventArgs e)
        {
            if (e.Right && (IsPointOnSnapResolution(e.X, e.Y) || IsPointOnSnapButton(e.X, e.Y)))
            { 
                var options = new ContextMenuOption[SnapResolutionType.Max - SnapResolutionType.Min + 3];

                options[0] = new ContextMenuOption(SnapEnableContext, $"{SnapEnableContextTooltip} <Shift><S>", () => { snap = !snap; }, () => snap ? ContextMenuCheckState.Checked : ContextMenuCheckState.Unchecked );
                options[1] = new ContextMenuOption(SnapEffectsContext, SnapEffectsContextTooltip, () => { snapEffects = !snapEffects; }, () => snapEffects ? ContextMenuCheckState.Checked : ContextMenuCheckState.Unchecked);

                for (var i = SnapResolutionType.Min; i <= SnapResolutionType.Max; i++)
                {
                    var j = i; // Important, copy for lamdba.
                    var name = SnapResolutionType.Names[i];
                    var plural = SnapResolutionType.Factors[i] > 1.0;
                    var text = (plural ? SnapToBeatsContext : SnapToBeatContext).Format(name);
                    var tooltip = (plural ? SnapToBeatsContextTooltip : SnapToBeatContextTooltip).Format(name);

                    if (SnapResolutionType.KeyboardShortcuts[i] != Keys.Unknown)
                        tooltip += $" <Alt>{SnapResolutionType.KeyboardShortcuts[i] - Keys.D0}";

                    options[i + 2] = new ContextMenuOption(text, tooltip, () => { snapResolution = j; }, () => snapResolution == j ? ContextMenuCheckState.Radio : ContextMenuCheckState.None, i == 0 ? ContextMenuSeparator.Before : ContextMenuSeparator.None);
                }

                App.ShowContextMenuAsync(options);
                return true;
            }

            return false;
        }

        private bool HandleMouseUpChannelNote(PointerEventArgs e)
        {
            return e.Right && HandleContextMenuChannelNote(e.X, e.Y);
        }

        private bool HandleMouseUpChannelHeader(PointerEventArgs e)
        {
            return e.Right && HandleContextMenuChannelHeader(e.X, e.Y);
        }

        private bool HandleMouseUpEffectPanel(PointerEventArgs e)
        {
            return e.Right && HandleContextMenuEffectPanel(e.X, e.Y);
        }

        private bool HandleMouseUpEnvelope(PointerEventArgs e)
        {
            return e.Right && HandleContextMenuEnvelope(e.X, e.Y);
        }

        private bool HandleMouseUpDPCMMapping(PointerEventArgs e)
        {
            return e.Right && HandleContextMenuDPCMMapping(e.X, e.Y);
        }

        private bool HandleMouseUpDPCMVolumeEnvelope(PointerEventArgs e)
        {
            return e.Right && HandleContextMenuWave(e.X, e.Y);
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
                if (editMode == EditionMode.Channel)
                {
                    if (HandleMouseUpSnapResolution(e)) goto Handled;
                    if (HandleMouseUpChannelNote(e)) goto Handled;
                    if (HandleMouseUpEffectPanel(e)) goto Handled;
                    if (HandleMouseUpChannelHeader(e)) goto Handled;
                }

                if (editMode == EditionMode.Envelope ||
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
                x = (int)(Width * Settings.FollowPercent);

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

        private bool HandleMouseWheelZoom(PointerEventArgs e)
        {
            if (e.X > pianoSizeX)
            {
                if (Settings.TrackPadControls && !ModifierKeys.IsControlDown && !ModifierKeys.IsAltDown)
                {
                    if (ModifierKeys.IsShiftDown)
                        scrollX -= Utils.SignedCeil(e.ScrollY);
                    else
                        scrollY -= Utils.SignedCeil(e.ScrollY);

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

        private bool HandleMouseWheelSnapResolution(PointerEventArgs e)
        {
            if (editMode == EditionMode.Channel && (IsPointOnSnapResolution(e.X, e.Y) || IsPointOnSnapButton(e.X, e.Y)))
            {
                snapResolution = Utils.Clamp(snapResolution + (e.ScrollY > 0 ? 1 : -1), SnapResolutionType.Min, SnapResolutionType.Max);
                return true;
            }

            return false;
        }

        protected override void OnMouseWheel(PointerEventArgs e)
        {
            if (HandleMouseWheelZoom(e)) goto Handled;
            if (HandleMouseWheelSnapResolution(e)) goto Handled;

            return;

        Handled:
            MarkDirty();
        }

        protected override void OnMouseHorizontalWheel(PointerEventArgs e)
        {
            scrollX += Utils.SignedCeil(e.ScrollX);
            ClampScroll();
            MarkDirty();
        }

        public void UpdateFollowMode(bool force = false)
        {
            continuouslyFollowing = false;

            if ((App.IsPlaying || force) && App.FollowModeEnabled && Settings.FollowSync != Settings.FollowSyncSequencer && !panning && 
                captureOperation == CaptureOperation.None && editMode == EditionMode.Channel && !window.IsAsyncDialogInProgress)
            {
                var frame = App.CurrentFrame;
                var seekX = GetPixelXForAbsoluteNoteIndex(frame);

                if (Settings.FollowMode == Settings.FollowModeJump)
                {
                    var maxX = Width - pianoSizeX;
                    if (seekX < 0 || seekX > maxX)
                        scrollX = GetPixelXForAbsoluteNoteIndex(frame, false);
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

            Debug.Assert(!window.IsAsyncDialogInProgress || captureOperation == CaptureOperation.None);

            UpdateCaptureOperation(mouseLastX, mouseLastY, 1.0f, true);
            UpdateFollowMode();
            TickFling(delta);
        }

        private bool GetEffectNoteForCoord(int x, int y, out NoteLocation location)
        {
            if (x > pianoSizeX && y > headerSizeY && y < headerAndEffectSizeY)
            {
                var absoluteNoteIndex = GetAbsoluteNoteIndexForPixelX(x - pianoSizeX);
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
            noteValue = (byte)(NumNotes - Utils.Clamp(rawNoteValue, 0, NumNotes - 1));

            // Allow to go outside the window when a capture is in progress.
            var captureInProgress = captureOperation != CaptureOperation.None;
            return x > pianoSizeX && x < width && ((y > headerAndEffectSizeY && !captureInProgress) || (rawNoteValue >= 0 && captureInProgress));
        }

        private bool GetLocationForCoord(int x, int y, out NoteLocation location, out byte noteValue, bool allowSnap = false)
        {
            var absoluteNoteIndex = Utils.Clamp(GetAbsoluteNoteIndexForPixelX(x - pianoSizeX), 0, Song.GetPatternStartAbsoluteNoteIndex(Song.Length));

            if (allowSnap)
                absoluteNoteIndex = SnapNote(absoluteNoteIndex);

            location = Song.AbsoluteNoteIndexToNoteLocation(absoluteNoteIndex);
            noteValue = (byte)(NumNotes - Utils.Clamp((y + scrollY - headerAndEffectSizeY) / noteSizeY, 0, NumNotes));

            return (x > pianoSizeX && x < width && y > headerAndEffectSizeY && location.PatternIndex < Song.Length);
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

            idx = GetAbsoluteNoteIndexForPixelX(x - pianoSizeX);

            return x > pianoSizeX;
        }

#if DEBUG
        public void ValidateIntegrity()
        {
            Debug.Assert(editMode != EditionMode.Channel || editChannel == App.SelectedChannelIndex);
        }
#endif

        public void Serialize(ProjectBuffer buffer)
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

            if (Settings.RestoreViewOnUndoRedo || buffer.IsWriting)
            {
                buffer.Serialize(ref scrollX);
                buffer.Serialize(ref scrollY);
                buffer.Serialize(ref zoom);
            }
            else
            {
                var dummyScroll = 0;
                var dummyZoom = 0.0f;
                buffer.Serialize(ref dummyScroll);
                buffer.Serialize(ref dummyScroll);
                buffer.Serialize(ref dummyZoom);
            }

            buffer.Serialize(ref selectedEffectIdx);
            buffer.Serialize(ref showEffectsPanel);
            buffer.Serialize(ref maximized);
            buffer.Serialize(ref selectionMin);
            buffer.Serialize(ref selectionMax);
            buffer.Serialize(ref relativeEffectScaling);

            if (Platform.IsMobile)
            {
                buffer.Serialize(ref highlightRepeatEnvelope);
                buffer.Serialize(ref highlightNoteAbsIndex);
                buffer.Serialize(ref highlightDPCMSample);
            }

            if (buffer.IsReading)
            {
                BuildSupportEffectList();
                UpdateRenderCoords();
                ClampScroll();
                MarkDirty();
                ReleasePointer();

                captureOperation = CaptureOperation.None;
                panning = false;
            }
        }
    }

    public class SnapResolutionType
    {
        public const int Min         = 0;
        public const int Max         = 10;
        public const int Beat        = 7;
        public const int QuarterBeat = 4;

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

        public static readonly Keys[] KeyboardShortcuts = new Keys[]
        {
            Keys.Unknown,
            Keys.Unknown,
            Keys.D4,
            Keys.Unknown,
            Keys.D3,
            Keys.Unknown,
            Keys.D2,
            Keys.D1,
            Keys.Unknown,
            Keys.Unknown,
            Keys.Unknown
        };
    }
}
