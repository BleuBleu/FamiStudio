using System;
using System.Diagnostics;
using System.Media;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;

using RenderBitmap      = FamiStudio.GLBitmap;
using RenderBitmapAtlas = FamiStudio.GLBitmapAtlas;
using RenderBrush       = FamiStudio.GLBrush;
using RenderGeometry    = FamiStudio.GLGeometry;
using RenderControl     = FamiStudio.GLControl;
using RenderGraphics    = FamiStudio.GLGraphics;
using RenderTheme       = FamiStudio.ThemeRenderResources;
using RenderCommandList = FamiStudio.GLCommandList;

namespace FamiStudio
{
    public class PianoRoll : RenderControl
    {
        const float MinZoomFamiStudio       = 1.0f / 32.0f;
        const float MinZoomOther            = 1.0f / 8.0f;
        const float MaxZoom                 = 16.0f;
        const float MinZoomY                = 0.25f;
        const float MaxZoomY                = 4.0f;
        const float MaxWaveZoom             = 256.0f;
        const float DefaultEnvelopeZoom     = 4.0f;
        const float DrawFrameZoom           = 0.5f;
        const float ContinuousFollowPercent = 0.75f;
        const float DefaultZoomWaveTime     = 0.25f;

        const int NumOctaves = 8;
        const int NumNotes   = NumOctaves * 12;

        const int DefaultHeaderSizeY               = 17;
        const int DefaultEffectPanelSizeY          = 176;
        const int DefaultEffectButtonSizeY         = 18;
        const int DefaultNoteSizeX                 = 16;
        const int DefaultNoteSizeY                 = 12;
        const int DefaultNoteAttackSizeX           = 3;
        const int DefaultReleaseNoteSizeY          = 8;
        const int DefaultEnvelopeSizeY             = 9;
        const int DefaultWhiteKeySizeX             = PlatformUtils.IsMobile ? 40 : 94;
        const int DefaultWhiteKeySizeY             = 20;
        const int DefaultBlackKeySizeX             = PlatformUtils.IsMobile ? 20 : 56;
        const int DefaultBlackKeySizeY             = 14;
        const int DefaultSnapIconPosX              = 3;
        const int DefaultSnapIconPosY              = 3;
        const int DefaultEffectIconPosX            = 2;
        const int DefaultEffectIconPosY            = 2;
        const int DefaultEffectNamePosX            = 17;
        const int DefaultEffectIconSizeX           = 12;
        const int DefaultEffectValuePosTextOffsetY = 12;
        const int DefaultEffectValueNegTextOffsetY = 3;
        const int DefaultBigTextPosX               = 10;
        const int DefaultBigTextPosY               = 10;
        const int DefaultTooltipTextPosX           = 10;
        const int DefaultTooltipTextPosY           = 30;
        const int DefaultDPCMTextPosX              = 2;
        const int DefaultDPCMTextPosY              = 0;
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

        int headerSizeY;
        int headerAndEffectSizeY;
        int effectPanelSizeY;
        int effectButtonSizeY;
        int noteAttackSizeX;
        int releaseNoteSizeY;
        int whiteKeySizeY;
        int whiteKeySizeX;
        int blackKeySizeY;
        int blackKeySizeX;
        int effectIconPosX;
        int effectIconPosY;
        int headerIconsPosX;
        int headerIconsPosY;
        int effectNamePosX;
        int effectIconSizeX;
        int effectValuePosTextOffsetY;
        int effectValueNegTextOffsetY;
        int bigTextPosX;
        int bigTextPosY;
        int tooltipTextPosX;
        int tooltipTextPosY;
        int dpcmTextPosX;
        int dpcmTextPosY;
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
        float minZoom;
        float maxZoom;
        float envelopeSizeY;
        float noteSizeX;
        int noteSizeY;

        float effectBitmapScale = 1.0f;
        float bitmapScale = 1.0f;

        enum EditionMode
        {
            None,
            Channel,
            Enveloppe,
            DPCM,
            DPCMMapping,
            Arpeggio,
            VideoRecording
        };

        RenderBrush whiteKeyBrush;
        RenderBrush blackKeyBrush;
        RenderBrush whiteKeyPressedBrush;
        RenderBrush blackKeyPressedBrush;
        RenderBrush frameLineBrush;
        RenderBrush debugBrush;
        RenderBrush seekBarBrush;
        RenderBrush seekBarRecBrush;
        RenderBrush selectionBgVisibleBrush;
        RenderBrush selectionBgInvisibleBrush;
        RenderBrush selectionNoteBrush;
        RenderBrush hoverNoteBrush;
        RenderBrush attackBrush;
        RenderBrush iconTransparentBrush;
        RenderBrush invalidDpcmMappingBrush;
        RenderBrush volumeSlideBarFillBrush;
        RenderBitmapAtlas bmpMiscAtlas;
        RenderBitmapAtlas bmpSnapResolutionAtlas;
        RenderBitmapAtlas bmpEffectAtlas;
        //RenderBitmapAtlas bmpEffectFillAtlas;
        RenderGeometry[] stopNoteGeometry        = new RenderGeometry[2]; // [1] is used to draw arps.
        RenderGeometry[] stopReleaseNoteGeometry = new RenderGeometry[2]; // [1] is used to draw arps.
        RenderGeometry[] releaseNoteGeometry     = new RenderGeometry[2]; // [1] is used to draw arps.
        RenderGeometry   slideNoteGeometry;
        RenderGeometry   seekGeometry;
        RenderGeometry   sampleGeometry;

        enum MiscImageIndices
        {
            Loop,
            Release,
            EffectExpanded,
            EffectCollapsed,
            Maximize,
            Snap,
            Count
        };

        readonly string[] MiscImageNames = new string[]
        {
            "LoopSmallFill",
            "ReleaseSmallFill",
            "ExpandedSmall",
            "CollapsedSmall",
            "Maximize",
            "Snap",
        };


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
            "EffectDutyCycle", // MATTT : Wrong
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
            DragVolumeSlideTarget,
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
            false, // Select
            false, // SelectWave
            false, // CreateNote
            true,  // CreateDragSlideNoteTarget
            true,  // DragSlideNoteTarget
            false, // DragVolumeSlideTarget
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
            false, // CreateDragSlideNoteTarget
            false, // DragSlideNoteTarget
            false, // DragVolumeSlideTarget
            true,  // DragNote
            true,  // DragSelection
            false, // AltZoom
            false, // DragSample
            true,  // DragSeekBar
            false, // DragWaveVolumeEnvelope
            false, // ScrollBarX
            false, // ScrollBarY
            true,  // ResizeNoteStart 
            true,  // ResizeSelectionNoteStart
            true,  // ResizeNoteEnd
            true,  // ResizeSelectionNoteEnd
            false, // MoveNoteRelease
            false, // MobileZoom
            false, // MobileZoomVertical
            false, // MobilePan
        };

        NoteLocation hoverNoteLocation = NoteLocation.Invalid;
        NoteLocation captureNoteLocation;
        NoteLocation captureMouseLocation;
        int captureNoteAbsoluteIdx = 0;
        int captureMouseAbsoluteIdx = 0;
        int captureNoteValue = 0;
        float captureWaveTime = 0.0f;
        int mouseLastX = 0;
        int mouseLastY = 0;
        int captureMouseX = 0;
        int captureMouseY = 0;
        int captureScrollX = 0;
        int captureScrollY = 0;
        int captureSelectionMin = -1;
        int captureSelectionMax = -1;
        int playingNote = -1;
        int highlightNote = Note.NoteInvalid;
        int selectionMin = -1;
        int selectionMax = -1;
        int dragSeekPosition = -1;
        int[] supportedEffects;
        bool captureThresholdMet = false;
        bool captureRealTimeUpdate = false;
        bool panning = false; // TODO: Make this a capture operation.
        bool continuouslyFollowing = false;
        CaptureOperation captureOperation = CaptureOperation.None;

        // Note dragging support.
        int dragFrameMin = -1;
        int dragFrameMax = -1;
        int dragLastNoteValue = -1;
        SortedList<int, Note> dragNotes = new SortedList<int, Note>();

        bool maximized = false;
        bool showSelection = false;
        bool showEffectsPanel = false;
        bool snap = false;
        int snapResolution = SnapResolution.OneNote;
        int scrollX = 0;
        int scrollY = 0;
        float zoom = 1.0f;
        float zoomY = PlatformUtils.IsMobile ? 0.75f : 1.0f;
        float pianoScaleX = 1.0f; // Only used by video export.
        int selectedEffectIdx = 0;
        string noteTooltip = "";

        EditionMode editMode = EditionMode.None;

        // Pattern edit mode.
        int editChannel = -1;

        // Envelope edit mode.
        Instrument editInstrument = null;
        int editEnvelope;
        int envelopeValueZoom = 1;
        int envelopeValueOffset = 0;

        // Arpeggio edit mode
        Arpeggio editArpeggio = null;

        // Remembering last paste-special settings
        bool lastPasteSpecialPasteMix = false;
        bool lastPasteSpecialPasteNotes = true;
        int lastPasteSpecialPasteEffectMask = Note.EffectAllMask;

        // DPCM editing mode
        int volumeEnvelopeDragVertex = -1;
        DPCMSample editSample = null;

        // When dragging samples
        DPCMSampleMapping draggedSample;

        // Video stuff
        Song videoSong;
        Color videoKeyColor;

        private bool IsSnappingAllowed => editMode == EditionMode.Channel;
        private bool IsSnappingEnabled => IsSnappingAllowed && snap;

        public bool IsMaximized                => maximized;
        public bool IsEditingInstrument        => editMode == EditionMode.Enveloppe; 
        public bool IsEditingArpeggio          => editMode == EditionMode.Arpeggio;
        public bool IsEditingDPCMSample        => editMode == EditionMode.DPCM;
        public bool IsEditingDPCMSampleMapping => editMode == EditionMode.DPCMMapping;
        
        public bool CanCopy  => showSelection && IsSelectionValid() && (editMode == EditionMode.Channel || editMode == EditionMode.Enveloppe || editMode == EditionMode.Arpeggio);
        public bool CanPaste => showSelection && IsSelectionValid() && (editMode == EditionMode.Channel && ClipboardUtils.ConstainsNotes || (editMode == EditionMode.Enveloppe || editMode == EditionMode.Arpeggio) && ClipboardUtils.ConstainsEnvelope);

        public Instrument EditInstrument => editInstrument;
        public Arpeggio   EditArpeggio   => editArpeggio;

        public DPCMSample CurrentEditSample { get => editSample; }

        public delegate void EmptyDelegate();
        public delegate void PatternDelegate(Pattern pattern);
        public delegate void NoteDelegate(Note note);
        public delegate void DPCMMappingDelegate(int note);

        public event PatternDelegate     PatternChanged;
        public event EmptyDelegate       MaximizedChanged;
        public event EmptyDelegate       ManyPatternChanged;
        public event EmptyDelegate       DPCMSampleChanged;
        public event EmptyDelegate       EnvelopeChanged;
        public event EmptyDelegate       ControlActivated;
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
            var headerScale = editMode == EditionMode.DPCMMapping || editMode == EditionMode.DPCM || editMode == EditionMode.None ? 1 : (editMode == EditionMode.VideoRecording ? 0 : 2);
            var scrollBarSize = Settings.ScrollBars == 1 ? DefaultScrollBarThickness1 : (Settings.ScrollBars == 2 ? DefaultScrollBarThickness2 : 0);
            var effectIconsScale = PlatformUtils.IsMobile ? 0.5f : 1.0f;

            minZoom = editMode == EditionMode.Channel && Song != null && Song.UsesFamiStudioTempo ? MinZoomFamiStudio : MinZoomOther;
            maxZoom = editMode == EditionMode.DPCM ? MaxWaveZoom : MaxZoom;
            zoom    = Utils.Clamp(zoom, minZoom, maxZoom);

            headerSizeY               = ScaleForMainWindow(DefaultHeaderSizeY * headerScale);
            effectButtonSizeY         = ScaleForMainWindow(DefaultEffectButtonSizeY * effectIconsScale);
            noteSizeX                 = ScaleForMainWindowFloat(DefaultNoteSizeX * zoom);
            noteSizeY                 = ScaleForMainWindow(DefaultNoteSizeY * zoomY);
            noteAttackSizeX           = ScaleForMainWindow(DefaultNoteAttackSizeX);
            releaseNoteSizeY          = ScaleForMainWindow(DefaultReleaseNoteSizeY * zoomY) & 0xfe; // Keep even
            whiteKeySizeY             = ScaleForMainWindow(DefaultWhiteKeySizeY * zoomY);
            whiteKeySizeX             = ScaleForMainWindow(DefaultWhiteKeySizeX * pianoScaleX);
            blackKeySizeY             = ScaleForMainWindow(DefaultBlackKeySizeY * zoomY);
            blackKeySizeX             = ScaleForMainWindow(DefaultBlackKeySizeX * pianoScaleX);
            effectIconPosX            = ScaleForMainWindow(DefaultEffectIconPosX * effectIconsScale);
            effectIconPosY            = ScaleForMainWindow(DefaultEffectIconPosY * effectIconsScale);
            headerIconsPosX           = ScaleForMainWindow(DefaultSnapIconPosX);
            headerIconsPosY           = ScaleForMainWindow(DefaultSnapIconPosY);
            effectNamePosX            = ScaleForMainWindow(DefaultEffectNamePosX * effectIconsScale);
            beatTextPosX              = ScaleForMainWindow(DefaultBeatTextPosX);
            effectIconSizeX           = ScaleForMainWindow(DefaultEffectIconSizeX);
            effectValuePosTextOffsetY = ScaleForMainWindow(DefaultEffectValuePosTextOffsetY);
            effectValueNegTextOffsetY = ScaleForMainWindow(DefaultEffectValueNegTextOffsetY);
            bigTextPosX               = ScaleForFont(DefaultBigTextPosX);
            bigTextPosY               = ScaleForFont(DefaultBigTextPosY);
            tooltipTextPosX           = ScaleForFont(DefaultTooltipTextPosX);
            tooltipTextPosY           = ScaleForFont(DefaultTooltipTextPosY);
            dpcmTextPosX              = ScaleForFont(DefaultDPCMTextPosX);
            dpcmTextPosY              = ScaleForFont(DefaultDPCMTextPosY);
            recordingKeyOffsetY       = ScaleForMainWindow(DefaultRecordingKeyOffsetY);
            attackIconPosX            = ScaleForMainWindow(DefaultAttackIconPosX);
            waveGeometrySampleSize    = ScaleForMainWindow(DefaultWaveGeometrySampleSize);
            waveDisplayPaddingY       = ScaleForMainWindow(DefaultWaveDisplayPaddingY);
            scrollBarThickness        = ScaleForMainWindow(scrollBarSize);
            minScrollBarLength        = ScaleForMainWindow(DefaultMinScrollBarLength);
            scrollMargin              = ScaleForMainWindow(DefaultScrollMargin);
            noteResizeMargin          = ScaleForMainWindow(DefaultNoteResizeMargin);
            envelopeSizeY             = ScaleForMainWindowFloat(DefaultEnvelopeSizeY * envelopeValueZoom);

            //// Make sure the effect panel actually fit on screen on mobile.
            if (PlatformUtils.IsMobile && ParentForm != null)
                effectPanelSizeY = Math.Min(ParentFormSize.Height / 2, ScaleForMainWindow(DefaultEffectPanelSizeY));
            else
                effectPanelSizeY = ScaleForMainWindow(DefaultEffectPanelSizeY);

            octaveSizeY          = 12 * noteSizeY;
            headerAndEffectSizeY = headerSizeY + (showEffectsPanel ? effectPanelSizeY : 0);
            virtualSizeY         = NumNotes * noteSizeY;
        }

        public void StartEditPattern(int trackIdx, int patternIdx)
        {
            editMode = EditionMode.Channel;
            editChannel = trackIdx;
            noteTooltip = "";

            BuildSupportEffectList();
            ClearSelection();
            UpdateRenderCoords();
            CenterScroll(patternIdx);
            ClampScroll();
            ClampMinSnap();
            MarkDirty();
        }

        public void ChangeChannel(int trackIdx)
        {
            if (editMode == EditionMode.Channel)
            {
                AbortCaptureOperation();
                editChannel = trackIdx;
                noteTooltip = "";
                BuildSupportEffectList();
                MarkDirty();
            }
        }

        public void StartEditEnveloppe(Instrument instrument, int envelope)
        {
            editMode = EditionMode.Enveloppe;
            editInstrument = instrument;
            editEnvelope = envelope;
            showEffectsPanel = false;
            noteTooltip = "";
            envelopeValueZoom = envelope == EnvelopeType.Volume || envelope == EnvelopeType.DutyCycle || envelope == EnvelopeType.N163Waveform ? 2 : 1;
            envelopeValueOffset = 0;
            Debug.Assert(editInstrument != null);

            ClearSelection();
            UpdateRenderCoords();
            CenterEnvelopeScroll(editInstrument.Envelopes[editEnvelope], editEnvelope, editInstrument);
            ClampScroll();
            MarkDirty();
        }

        public void StartEditArpeggio(Arpeggio arpeggio)
        {
            editMode = EditionMode.Arpeggio;
            editEnvelope = EnvelopeType.Arpeggio;
            editInstrument = null;
            editArpeggio = arpeggio;
            showEffectsPanel = false;
            noteTooltip = "";
            envelopeValueZoom = 1;  
            envelopeValueOffset = 0;

            ClearSelection();
            UpdateRenderCoords();
            CenterEnvelopeScroll(arpeggio.Envelope, EnvelopeType.Arpeggio);
            ClampScroll();
            MarkDirty();
        }

        public void StartEditDPCMSample(DPCMSample sample)
        {
            editMode = EditionMode.DPCM;
            editSample = sample;
            zoom = 0;
            noteTooltip = "";
            envelopeValueZoom = 1;
            envelopeValueOffset = 0;

            ClearSelection();
            UpdateRenderCoords();
            CenterWaveScroll();
            ClampScroll();
            MarkDirty();
        }

        public void StartEditDPCMMapping()
        {
            editMode = EditionMode.DPCMMapping;
            showEffectsPanel = false;
            zoom = 0;
            noteTooltip = "";
            envelopeValueZoom = 1;
            envelopeValueOffset = 0;

            ClearSelection();
            UpdateRenderCoords();
            CenterDPCMMappingScroll();
            ClampScroll();
            MarkDirty();
        }

        public void StartVideoRecording(RenderGraphics g, Song song, int videoZoom, float pianoRollScaleX, float pianoRollScaleY, out int outNoteSizeY)
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
            }
        }

        private void CenterWaveScroll()
        {
            zoom = maxZoom;

            var duration = Math.Max(editSample.SourceDuration, editSample.ProcessedDuration);
            var viewSize = Width - whiteKeySizeX;
            var width    = (int)GetPixelForWaveTime(duration);

            while (width > viewSize && zoom > minZoom)
            {
                zoom /= 2;
                width /= 2;
            }

            scrollX = 0;
        }

        private void CenterEnvelopeScroll(Envelope envelope, int envelopeType, Instrument instrument = null)
        {
            var maxNumNotes = Width / DefaultNoteSizeX;

            if (envelope.Length == 0)
                zoom = DefaultEnvelopeZoom;
            else
                zoom = Utils.Clamp(Utils.NextPowerOfTwo((int)Math.Ceiling(maxNumNotes / (float)envelope.Length)) / 2, minZoom, maxZoom); 

            UpdateRenderCoords();

            Envelope.GetMinMaxValue(instrument, envelopeType, out int min, out int max);

            int midY = virtualSizeY - ((min + max) / 2 + 64 / envelopeValueZoom) * (virtualSizeY / (128 / envelopeValueZoom));

            scrollX = 0;
            scrollY = midY - Height / 2;
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

        private int GetNoteForPixel(int x, bool scroll = true)
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

        private void ClampMinSnap()
        {
            snapResolution = SnapResolution.Clamp(snapResolution, App.Project.UsesFamiTrackerTempo);
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

        public bool ShowSelection
        {
            get { return showSelection; }
            set { showSelection = value; MarkDirty(); }
        }

        public void HighlightPianoNote(int note)
        {
            if (note != highlightNote)
            {
                highlightNote = note;
                MarkDirty();
            }
        }

        public void Reset()
        {
            AbortCaptureOperation();
            showEffectsPanel = false;
            scrollX = 0;
            scrollY = 0;
            zoom = 0;
            editMode = EditionMode.None;
            editChannel = -1;
            editInstrument = null;
            editArpeggio = null;
            noteTooltip = "";
            UpdateRenderCoords();
            ClampScroll();
            ClampMinSnap();
            ClearSelection();
        }

        public void SongModified()
        {
            ClearSelection();
            UpdateRenderCoords();
            MarkDirty();
        }

        public void SongChanged()
        {
            if (editMode == EditionMode.Channel)
            {
                editMode = EditionMode.None;
                editChannel = -1;
                editInstrument = null;
                editArpeggio = null;
                showEffectsPanel = false;
                ClearSelection();
                UpdateRenderCoords();
            }
        }

        protected override void OnRenderInitialized(RenderGraphics g)
        {
            UpdateRenderCoords();

            Debug.Assert(MiscImageNames.Length == (int)MiscImageIndices.Count);
            Debug.Assert(SnapResolution.ImageNames.Length == (int)SnapResolution.Max);
            Debug.Assert(EffectImageNames.Length == Note.EffectCount + 1);

            whiteKeyBrush = g.CreateHorizontalGradientBrush(0, whiteKeySizeX, Theme.LightGreyFillColor1, Theme.LightGreyFillColor2);
            blackKeyBrush = g.CreateHorizontalGradientBrush(0, blackKeySizeX, Theme.DarkGreyFillColor1, Theme.DarkGreyFillColor2);
            whiteKeyPressedBrush = g.CreateHorizontalGradientBrush(0, whiteKeySizeX, Theme.Darken(Theme.LightGreyFillColor1), Theme.Darken(Theme.LightGreyFillColor2));
            blackKeyPressedBrush = g.CreateHorizontalGradientBrush(0, blackKeySizeX, Theme.Lighten(Theme.DarkGreyFillColor1), Theme.Lighten(Theme.DarkGreyFillColor2));
            frameLineBrush = g.CreateSolidBrush(Color.FromArgb(128, Theme.DarkGreyLineColor2));
            debugBrush = g.CreateSolidBrush(Theme.GreenColor);
            seekBarBrush = g.CreateSolidBrush(Theme.SeekBarColor);
            seekBarRecBrush = g.CreateSolidBrush(Theme.DarkRedFillColor);
            selectionBgVisibleBrush = g.CreateSolidBrush(Color.FromArgb(64, Theme.LightGreyFillColor1));
            selectionBgInvisibleBrush = g.CreateSolidBrush(Color.FromArgb(16, Theme.LightGreyFillColor1));
            selectionNoteBrush = g.CreateSolidBrush(Theme.LightGreyFillColor1);
            hoverNoteBrush = g.CreateSolidBrush(Theme.WhiteColor);
            attackBrush = g.CreateSolidBrush(Color.FromArgb(128, Theme.BlackColor));
            iconTransparentBrush = g.CreateSolidBrush(Color.FromArgb(92, Theme.DarkGreyLineColor2));
            invalidDpcmMappingBrush = g.CreateSolidBrush(Color.FromArgb(64, Theme.BlackColor));
            volumeSlideBarFillBrush = g.CreateSolidBrush(Color.FromArgb(64, Theme.LightGreyFillColor1));
            bmpMiscAtlas = g.CreateBitmapAtlasFromResources(MiscImageNames);
            bmpSnapResolutionAtlas = g.CreateBitmapAtlasFromResources(SnapResolution.ImageNames);
            bmpEffectAtlas = g.CreateBitmapAtlasFromResources(EffectImageNames);
            //bmpEffectFillAtlas = g.CreateBitmapAtlasFromResources(EffectImageNames, Theme.DarkGreyLineColor2);
            fontSmallCharSizeX = ThemeResources != null ? ThemeResources.FontSmall.MeasureString("0") : 1;

            if (PlatformUtils.IsMobile)
            {
                bitmapScale = g.WindowScaling * 0.5f;
                effectBitmapScale = g.WindowScaling * 0.25f;
            }

            seekGeometry = g.CreateGeometry(new float[,]
            {
                { -headerSizeY / 2, 1 },
                { 0, headerSizeY - 2 },
                { headerSizeY / 2, 1 }
            }, true);

            sampleGeometry = g.CreateGeometry(new float[,]
            {
                { -waveGeometrySampleSize, -waveGeometrySampleSize },
                {  waveGeometrySampleSize, -waveGeometrySampleSize },
                {  waveGeometrySampleSize,  waveGeometrySampleSize },
                { -waveGeometrySampleSize,  waveGeometrySampleSize }
            }, true);

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
            Utils.DisposeAndNullify(ref hoverNoteBrush);
            Utils.DisposeAndNullify(ref attackBrush);
            Utils.DisposeAndNullify(ref iconTransparentBrush);
            Utils.DisposeAndNullify(ref invalidDpcmMappingBrush);
            Utils.DisposeAndNullify(ref volumeSlideBarFillBrush);
            Utils.DisposeAndNullify(ref bmpMiscAtlas);
            Utils.DisposeAndNullify(ref bmpSnapResolutionAtlas);
            Utils.DisposeAndNullify(ref bmpEffectAtlas);
            //Utils.DisposeAndNullify(ref bmpEffectFillAtlas);
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

        private void ConditionalUpdateNoteGeometries(RenderGraphics g)
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
            }, true);

            stopNoteGeometry[1] = g.CreateGeometry(new float[,]
            {
                { 0.0f, 1 },
                { 0.0f, noteSizeY },
                { 1.0f, noteSizeY / 2 }
            }, true);

            releaseNoteGeometry[0] = g.CreateGeometry(new float[,]
            {
                { 0.0f, 0 },
                { 0.0f, noteSizeY },
                { 1.0f, noteSizeY - noteSizeY / 2 + releaseNoteSizeY / 2 },
                { 1.0f, noteSizeY / 2 - releaseNoteSizeY / 2 }
            }, true);

            releaseNoteGeometry[1] = g.CreateGeometry(new float[,]
            {
                { 0.0f, 1 },
                { 0.0f, noteSizeY },
                { 1.0f, noteSizeY - noteSizeY / 2 + releaseNoteSizeY / 2 },
                { 1.0f, noteSizeY / 2 - releaseNoteSizeY / 2 + 1 }
            }, true);

            stopReleaseNoteGeometry[0] = g.CreateGeometry(new float[,]
            {
                { 0.0f, noteSizeY / 2 - releaseNoteSizeY / 2 },
                { 0.0f, noteSizeY / 2 + releaseNoteSizeY / 2 },
                { 1.0f, noteSizeY / 2 }
            }, true);

            stopReleaseNoteGeometry[1] = g.CreateGeometry(new float[,]
            {
                { 0.0f, noteSizeY / 2 - releaseNoteSizeY / 2 + 1 },
                { 0.0f, noteSizeY / 2 + releaseNoteSizeY / 2 },
                { 1.0f, noteSizeY / 2 }
            }, true);

            slideNoteGeometry = g.CreateGeometry(new float[,]
            {
                { 0.0f, 0 },
                { 1.0f, 0 },
                { 1.0f, noteSizeY }
            }, true);
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
                    editMode == EditionMode.VideoRecording ? whiteKeySizeX - blackKeySizeX : 0,
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
                    whiteKeySizeX,
                    keySizeY);
            }
        }

        public bool GetViewRange(ref int minNoteIdx, ref int maxNoteIdx, ref int channelIndex)
        {
            if (editMode == EditionMode.Channel)
            {
                minNoteIdx = Math.Max(GetNoteForPixel(0), 0);
                maxNoteIdx = Math.Min(GetNoteForPixel(Width - whiteKeySizeX) + 1, Song.GetPatternStartAbsoluteNoteIndex(Song.Length));
                channelIndex = editChannel;

                return true;
            }
            else
            {
                return false;
            }
        }

        private RenderBrush GetSeekBarBrush()
        {
            return (editMode == EditionMode.Channel && App.IsRecording) ? seekBarRecBrush : seekBarBrush;
        }

        private void ForEachWaveTimecode(RenderInfo r, Action<float, float, int, int> function)
        {
            var textSize  = r.g.MeasureString("99.999", ThemeResources.FontMedium);
            var waveWidth = Width - whiteKeySizeX;
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

            public RenderGraphics g;
            public RenderCommandList cc; // Top left (corner, header list)
            public RenderCommandList ch; // Top right (header, effect panel)
            public RenderCommandList cp; // Left side (piano area)
            public RenderCommandList cb; // Right side (note area) background
            public RenderCommandList cf; // Right side (note area) foreground
            public RenderCommandList cs; // Scroll bars
        }

        private void RenderHeader(RenderInfo r)
        {
            r.ch.PushTranslation(whiteKeySizeX, 0);

            if ((editMode == EditionMode.Enveloppe || editMode == EditionMode.Arpeggio) && EditEnvelope != null)
            {
                var env = EditEnvelope;

                r.ch.PushTranslation(0, headerSizeY / 2);

                DrawSelectionRect(r.ch, headerSizeY);

                if (env.Loop >= 0)
                {
                    r.ch.PushTranslation(GetPixelForNote(env.Loop), 0);
                    r.ch.FillRectangle(0, 0, GetPixelForNote(((env.Release >= 0 ? env.Release : env.Length) - env.Loop), false), headerAndEffectSizeY, ThemeResources.DarkGreyFillBrush2);
                    r.ch.DrawLine(0, 0, 0, headerAndEffectSizeY, ThemeResources.BlackBrush);
                    r.ch.DrawBitmapAtlas(bmpMiscAtlas, (int)MiscImageIndices.Loop, effectIconPosX + 1, effectIconPosY);
                    r.ch.PopTransform();
                }
                if (env.Release >= 0)
                {
                    r.ch.PushTranslation(GetPixelForNote(env.Release), 0);
                    r.ch.DrawLine(0, 0, 0, headerAndEffectSizeY, ThemeResources.BlackBrush);
                    r.ch.DrawBitmapAtlas(bmpMiscAtlas, (int)MiscImageIndices.Release, effectIconPosX + 1, effectIconPosY);
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

                DrawSelectionRect(r.ch, headerSizeY);

                // Draw the header bars
                for (int n = 0; n <= env.Length; n++)
                {
                    int x = GetPixelForNote(n);
                    if (x != 0)
                        r.ch.DrawLine(x, 0, x, headerSizeY / 2, ThemeResources.BlackBrush, 1.0f);
                    if (zoom >= 2.0f && n != env.Length)
                        r.ch.DrawText(n.ToString(), ThemeResources.FontMedium, x, 0, ThemeResources.LightGreyFillBrush1, RenderTextFlags.MiddleCenter, noteSizeX, headerSizeY / 2 - 1);
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
                        r.ch.DrawLine(px, 0, px, headerSizeY, ThemeResources.BlackBrush, 3.0f);

                    var pattern = Song.Channels[editChannel].PatternInstances[p];
                    var beatLen = Song.GetPatternBeatLength(p);
                    var beatSizeX = GetPixelForNote(beatLen, false);

                    // Is there enough room to draw beat labels?
                    if ((beatSizeX + beatTextPosX) > beatLabelSizeX)
                    {
                        var numBeats = (int)Math.Ceiling(patternLen / (float)beatLen);
                        for (int i = 0; i < numBeats; i++)
                            r.ch.DrawText($"{p + 1}.{i + 1}", ThemeResources.FontMedium, px + beatTextPosX + beatSizeX * i, 0, ThemeResources.LightGreyFillBrush1, RenderTextFlags.Middle, 0, headerSizeY / 2 - 1);
                    }
                    else
                    {
                        r.ch.DrawText((p + 1).ToString(), ThemeResources.FontMedium, px, 0, ThemeResources.LightGreyFillBrush1, RenderTextFlags.MiddleCenter, sx, headerSizeY / 2 - 1);
                    }

                    if (pattern != null)
                        r.ch.DrawText(pattern.Name, ThemeResources.FontMedium, px, headerSizeY / 2, ThemeResources.BlackBrush, RenderTextFlags.MiddleCenter, sx, headerSizeY / 2 - 1);
                }

                int maxX = GetPixelForNote(Song.GetPatternStartAbsoluteNoteIndex(r.maxVisiblePattern));
                r.ch.DrawLine(maxX, 0, maxX, Height, ThemeResources.BlackBrush, 3.0f);
                r.ch.DrawLine(0, headerSizeY / 2 - 1, Width, headerSizeY / 2 - 1, ThemeResources.BlackBrush);
            }
            else if (editMode == EditionMode.DPCM)
            {
                // Selection rectangle
                if (IsSelectionValid())
                {
                    r.ch.FillRectangle(
                        GetPixelForWaveTime(GetWaveTimeForSample(selectionMin, true),  scrollX), 0,
                        GetPixelForWaveTime(GetWaveTimeForSample(selectionMax, false), scrollX), Height, selectionBgVisibleBrush);
                }

                ForEachWaveTimecode(r, (time, x, level, idx) =>
                {
                    if (time != 0.0f)
                        r.ch.DrawText(time.ToString($"F{level + 1}"), ThemeResources.FontMedium, x - 100, 0, ThemeResources.LightGreyFillBrush1, RenderTextFlags.MiddleCenter, 200, headerSizeY - 1);
                });

                // Processed Range
                var processedBrush = r.g.GetSolidBrush(editSample.Color, 1.0f, 0.25f);
                r.ch.FillRectangle(
                    GetPixelForWaveTime(editSample.ProcessedStartTime, scrollX), 0,
                    GetPixelForWaveTime(editSample.ProcessedEndTime,   scrollX), Height, processedBrush);
            }

            r.ch.DrawLine(0, headerSizeY - 1, Width, headerSizeY - 1, ThemeResources.BlackBrush);

            if (((editMode == EditionMode.Enveloppe || editMode == EditionMode.Arpeggio) && editEnvelope < EnvelopeType.RegularCount) || (editMode == EditionMode.Channel))
            {
                var seekFrame = editMode == EditionMode.Enveloppe || editMode == EditionMode.Arpeggio ? App.GetEnvelopeFrame(editInstrument, editEnvelope, editMode == EditionMode.Arpeggio) : GetSeekFrameToDraw();
                if (seekFrame >= 0)
                {
                    r.ch.PushTranslation(GetPixelForNote(seekFrame), 0);
                    r.ch.FillAndDrawGeometry(seekGeometry, GetSeekBarBrush(), ThemeResources.BlackBrush, 1);
                    r.ch.DrawLine(0, headerSizeY / 2, 0, headerSizeY, GetSeekBarBrush(), 3);
                    r.ch.PopTransform();
                }
            }

            r.ch.PopTransform();
        }

        private void RenderEffectList(RenderInfo r)
        {
            r.cc.FillRectangle(0, 0, whiteKeySizeX, headerAndEffectSizeY, ThemeResources.DarkGreyFillBrush1);
            r.cc.DrawLine(whiteKeySizeX - 1, 0, whiteKeySizeX - 1, headerAndEffectSizeY, ThemeResources.BlackBrush);

            // Effect icons
            if (editMode == EditionMode.Channel)
            {
                var snapButtonSize = (int)bmpMiscAtlas.GetElementSize((int)MiscImageIndices.Snap).Width;

                r.cc.DrawBitmapAtlas(bmpMiscAtlas, showEffectsPanel ? (int)MiscImageIndices.EffectExpanded : (int)MiscImageIndices.EffectCollapsed, effectIconPosX, effectIconPosY, 1.0f, bitmapScale, Theme.LightGreyFillColor1);
                if (!PlatformUtils.IsMobile)
                    r.cc.DrawBitmapAtlas(bmpMiscAtlas, (int)MiscImageIndices.Maximize, whiteKeySizeX - (snapButtonSize + headerIconsPosX) * 1 - 1, headerIconsPosY, maximized ? 1.0f : 0.3f, 1.0f, Theme.LightGreyFillColor1);

                if (IsSnappingAllowed && !PlatformUtils.IsMobile)
                {
                    r.cc.DrawBitmapAtlas(bmpMiscAtlas, (int)MiscImageIndices.Snap, whiteKeySizeX - (snapButtonSize + headerIconsPosX) * 2 - 1, headerIconsPosY, IsSnappingEnabled || App.IsRecording ? 1.0f : 0.3f, 1.0f, App.IsRecording ? Theme.DarkRedFillColor : Theme.LightGreyFillColor1);
                    r.cc.DrawBitmapAtlas(bmpSnapResolutionAtlas, (int)snapResolution, whiteKeySizeX - (snapButtonSize + headerIconsPosX) * 3 - 1, headerIconsPosY, IsSnappingEnabled || App.IsRecording ? 1.0f : 0.3f, 1.0f, App.IsRecording ? Theme.DarkRedFillColor : Theme.LightGreyFillColor1);
                }

                if (showEffectsPanel)
                {
                    r.cc.PushTranslation(0, headerSizeY);

                    int effectButtonY = 0;

                    for (int i = 0; i < supportedEffects.Length; i++, effectButtonY += effectButtonSizeY)
                    {
                        var effectIdx = supportedEffects[i];

                        r.cc.PushTranslation(0, effectButtonY);
                        r.cc.DrawLine(0, -1, whiteKeySizeX, -1, ThemeResources.BlackBrush);
                        r.cc.DrawBitmapAtlas(bmpEffectAtlas, effectIdx, effectIconPosX, effectIconPosY, 1.0f, effectBitmapScale, Theme.LightGreyFillColor1);
                        r.cc.DrawText(Note.EffectNames[effectIdx], selectedEffectIdx == effectIdx ? ThemeResources.FontSmallBold : ThemeResources.FontSmall, effectNamePosX, 0, ThemeResources.LightGreyFillBrush2, RenderTextFlags.Middle, 0, effectButtonSizeY);
                        r.cc.PopTransform();
                    }

                    r.cc.PushTranslation(0, effectButtonY);
                    r.cc.DrawLine(0, -1, whiteKeySizeX, -1, ThemeResources.BlackBrush);
                    r.cc.PopTransform();
                    r.cc.PopTransform();
                }
            }
            else if (editMode == EditionMode.DPCM)
            {
                r.cc.DrawBitmapAtlas(bmpMiscAtlas, showEffectsPanel ? (int)MiscImageIndices.EffectExpanded  : (int)MiscImageIndices.EffectCollapsed, 0, 0, 1.0f, bitmapScale);

                if (showEffectsPanel)
                {
                    r.cc.PushTranslation(0, headerSizeY);
                    r.cc.DrawLine(0, -1, whiteKeySizeX, -1, ThemeResources.BlackBrush);
                    r.cc.DrawBitmapAtlas(bmpEffectAtlas, Note.EffectVolume, effectIconPosX, effectIconPosY, 1.0f, 1.0f, Theme.LightGreyFillColor1);
                    r.cc.DrawText(Note.EffectNames[Note.EffectVolume], ThemeResources.FontSmallBold, effectNamePosX, 0, ThemeResources.LightGreyFillBrush2, RenderTextFlags.Middle, 0, effectButtonSizeY);
                    r.cc.PopTransform();

                    r.cc.PushTranslation(0, effectButtonSizeY);
                    r.cc.DrawLine(0, -1, whiteKeySizeX, -1, ThemeResources.BlackBrush);
                    r.cc.PopTransform();
                }
            }

            r.cc.DrawLine(0, headerAndEffectSizeY - 1, whiteKeySizeX, headerAndEffectSizeY - 1, ThemeResources.BlackBrush);
        }

        private void RenderPiano(RenderInfo r)
        {
            r.cp.PushTranslation(0, headerAndEffectSizeY);
            r.cp.FillRectangle(0, 0, whiteKeySizeX, Height, whiteKeyBrush);

            var playOctave = -1;
            var playNote = -1;
            var draggingNote = captureOperation == CaptureOperation.DragNote || captureOperation == CaptureOperation.CreateNote;
            var dragOctave = (dragLastNoteValue - 1) / 12;
            var dragNote = (dragLastNoteValue - 1) % 12;

            if (highlightNote >= Note.MusicalNoteMin && 
                highlightNote <  Note.MusicalNoteMax)
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

            // Draw the piano
            for (int i = r.minVisibleOctave; i < r.maxVisibleOctave; i++)
            {
                int octaveBaseY = (virtualSizeY - octaveSizeY * i) - scrollY;

                for (int j = 0; j < 12; j++)
                {
                    if (i * 12 + j >= NumNotes)
                        break;

                    if (IsBlackKey(j))
                    {
                        r.cp.FillRectangle(GetKeyRectangle(i, j), blackKeyBrush);

                        if ((i == playOctave && j == playNote) || (draggingNote && (i == dragOctave && j == dragNote)))
                            r.cp.FillRectangle(GetKeyRectangle(i, j), blackKeyPressedBrush);
                    }

                    int y = octaveBaseY - j * noteSizeY;
                    if (j == 0)
                        r.cp.DrawLine(0, y, whiteKeySizeX, y, ThemeResources.BlackBrush);
                    else if (j == 5)
                        r.cp.DrawLine(0, y, whiteKeySizeX, y, ThemeResources.BlackBrush);
                }

                if ((editMode == EditionMode.Channel || editMode == EditionMode.DPCMMapping) && ThemeResources.FontSmall.Size < noteSizeY)
                    r.cp.DrawText("C" + i, ThemeResources.FontSmall, r.g.WindowScaling, octaveBaseY - noteSizeY + 1, ThemeResources.BlackBrush, RenderTextFlags.Middle, whiteKeySizeX - r.g.WindowScaling * 2, noteSizeY - 1);
            }

            if (App != null && (App.IsRecording || App.IsQwertyPianoEnabled))
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
                        keyStrings[i] = PlatformUtils.KeyCodeToString(k);
                    else
                        keyStrings[i] += $"   {PlatformUtils.KeyCodeToString(k)}";
                }

                for (int i = 0; i < Note.MusicalNoteMax; i++)
                {
                    if (keyStrings[i] == null)
                        continue;

                    int octaveBaseY = (virtualSizeY - octaveSizeY * ((i / 12) + App.BaseRecordingOctave)) - scrollY;
                    int y = octaveBaseY - (i % 12) * noteSizeY;

                    RenderBrush brush;
                    if (App.IsRecording)
                        brush = IsBlackKey(i % 12) ? ThemeResources.LightRedFillBrush : ThemeResources.DarkRedFillBrush;
                    else
                        brush = IsBlackKey(i % 12) ? ThemeResources.LightGreyFillBrush2 : ThemeResources.BlackBrush;

                    r.cp.DrawText(keyStrings[i], ThemeResources.FontVerySmall, 0, y - recordingKeyOffsetY + 1, brush, RenderTextFlags.MiddleCenter, blackKeySizeX, noteSizeY - 1);
                }
            }

            r.cp.DrawLine(whiteKeySizeX - 1, 0, whiteKeySizeX - 1, Height, ThemeResources.BlackBrush);
            r.cp.PopTransform();
        }

        private void RenderEffectPanel(RenderInfo r)
        {
            if ((editMode == EditionMode.Channel || editMode == EditionMode.DPCM) && showEffectsPanel)
            {
                r.ch.PushTranslation(whiteKeySizeX, headerSizeY);

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

                    var hoverLocation = NoteLocation.Invalid;
                    if (hoverNoteLocation != NoteLocation.Invalid && CaptureOperationRequiresEffectHighlight(captureOperation))
                    {
                        hoverLocation = hoverNoteLocation;
                    }
                    else if (captureOperation == CaptureOperation.None)
                    {
                        var pt = PointToClient(Cursor.Position);
                        GetEffectNoteForCoord(pt.X, pt.Y, out hoverLocation);
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

                            var hover = location == hoverLocation;
                            var selected = IsNoteSelected(location);

                            r.ch.FillAndDrawRectangle(
                                0, effectPanelSizeY - sizeY, noteSizeX, effectPanelSizeY,
                                singleFrameSlides.Contains(location) ? volumeSlideBarFillBrush : ThemeResources.LightGreyFillBrush1,
                                hover ? ThemeResources.WhiteBrush : ThemeResources.BlackBrush, hover || selected ? 2 : 1, hover || selected);

                            var text = effectValue.ToString();
                            if (text.Length * fontSmallCharSizeX + 2 < noteSizeX)
                            {
                                if (sizeY < effectPanelSizeY / 2)
                                    r.ch.DrawText(text, ThemeResources.FontSmall, 0, effectPanelSizeY - sizeY - effectValuePosTextOffsetY, ThemeResources.LightGreyFillBrush1, RenderTextFlags.Center, noteSizeX);
                                else
                                    r.ch.DrawText(text, ThemeResources.FontSmall, 0, effectPanelSizeY - sizeY + effectValueNegTextOffsetY, ThemeResources.BlackBrush, RenderTextFlags.Center, noteSizeX);
                            }

                            r.ch.PopTransform();
                        }
                    }

                    // Thick vertical bars
                    for (int p = r.minVisiblePattern; p < r.maxVisiblePattern; p++)
                    {
                        int x = GetPixelForNote(Song.GetPatternStartAbsoluteNoteIndex(p));
                        if (p != 0) r.ch.DrawLine(x, 0, x, Height, ThemeResources.BlackBrush, 3.0f);
                    }

                    int maxX = GetPixelForNote(Song.GetPatternStartAbsoluteNoteIndex(r.maxVisiblePattern));
                    r.ch.DrawLine(maxX, 0, maxX, Height, ThemeResources.BlackBrush, 3.0f);

                    int seekX = GetPixelForNote(GetSeekFrameToDraw());
                    r.ch.DrawLine(seekX, 0, seekX, effectPanelSizeY, GetSeekBarBrush(), 3);
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
                        var p0 = envelopePoints[i + 0];
                        var p1 = envelopePoints[i + 1];

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
                    createMissingInstrument = PlatformUtils.MessageBox($"You are pasting notes referring to unknown instruments. Do you want to create the missing instrument?", "Paste", MessageBoxButtons.YesNo) == DialogResult.Yes;

                if (missingArpeggios)
                    createMissingArpeggios = PlatformUtils.MessageBox($"You are pasting notes referring to unknown arpeggios. Do you want to create the missing arpeggios?", "Paste", MessageBoxButtons.YesNo) == DialogResult.Yes;

                if (missingSamples && editChannel == ChannelType.Dpcm)
                    createMissingSamples = PlatformUtils.MessageBox($"You are pasting notes referring to unmapped DPCM samples. Do you want to create the missing samples?", "Paste", MessageBoxButtons.YesNo) == DialogResult.Yes;
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

#if !FAMISTUDIO_ANDROID // DROIDTODO
                var dlg = new PasteSpecialDialog(Song.Channels[editChannel], lastPasteSpecialPasteMix, lastPasteSpecialPasteNotes, lastPasteSpecialPasteEffectMask);

                if (dlg.ShowDialog(ParentForm) == DialogResult.OK)
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
#endif
            }
        }

        public void DeleteSpecial()
        {
            if (editMode == EditionMode.Channel)
            {
                AbortCaptureOperation();

                var dlg = new DeleteSpecialDialog(Song.Channels[editChannel]);

                dlg.ShowDialog(ParentForm, (r) =>
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

        private bool IsNoteSelected(NoteLocation location, int duration = 0)
        {
            if (IsSelectionValid())
            {
                int absoluteNoteIdx = location.ToAbsoluteNoteIndex(Song);
                return absoluteNoteIdx >= selectionMin && absoluteNoteIdx <= selectionMax;
            }
            else
            {
                return false;
            }
        }

        private bool IsEnvelopeValueSelected(int idx)
        {
            return IsSelectionValid() && idx >= selectionMin && idx <= selectionMax;
        }

        private void DrawSelectionRect(RenderCommandList c, int height)
        {
            if (IsSelectionValid())
            {
                c.FillRectangle(
                    GetPixelForNote(selectionMin + 0), 0,
                    GetPixelForNote(selectionMax + 1), height, showSelection ? selectionBgVisibleBrush : selectionBgInvisibleBrush);
            }
        }

        private Color GetNoteColor(Channel channel, int noteValue, Instrument instrument, Project project)
        {
            if (channel.Type == ChannelType.Dpcm)
            {
                var mapping = project.GetDPCMMapping(noteValue);
                if (mapping != null)
                    return mapping.Sample.Color;
            }
            else if (instrument != null)
            {
                return instrument.Color;
            }

            return Theme.LightGreyFillColor1;
        }

        private void RenderNotes(RenderInfo r)
        {
            var song = Song;

            r.cb.PushTranslation(whiteKeySizeX, headerAndEffectSizeY);

            if (editMode == EditionMode.Channel ||
                editMode == EditionMode.VideoRecording ||
                editMode == EditionMode.DPCMMapping)
            {
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

                            for (int i = p == 0 ? 1 : 0; i < patternLen; i++)
                            {
                                int x = GetPixelForNote(song.GetPatternStartAbsoluteNoteIndex(p) + i);

                                if (i % beatLength == 0)
                                    r.cb.DrawLine(x, 0, x, Height, ThemeResources.BlackBrush, i == 0 ? 3.0f : 1.0f);
                                else if (i % noteLength == 0)
                                    r.cb.DrawLine(x, 0, x, Height, ThemeResources.DarkGreyLineBrush1);
                                else if (zoom >= DrawFrameZoom && editMode != EditionMode.VideoRecording)
                                    r.cb.DrawLine(x, 0, x, Height, ThemeResources.DarkGreyLineBrush1, 1, false, true);
                            }
                        }
                        else
                        {
                            for (int i = p == 0 ? 1 : 0; i < patternLen; i++)
                            {
                                int x = GetPixelForNote(song.GetPatternStartAbsoluteNoteIndex(p) + i);

                                if (i % beatLength == 0)
                                    r.cb.DrawLine(x, 0, x, Height, ThemeResources.BlackBrush, i == 0 ? 3.0f : 1.0f);
                                else if (zoom >= 0.5f)
                                    r.cb.DrawLine(x, 0, x, Height, ThemeResources.DarkGreyLineBrush2);
                            }
                        }
                    }

                    r.cb.DrawLine(maxX, 0, maxX, Height, ThemeResources.BlackBrush, 3.0f);

                    if (editMode != EditionMode.VideoRecording)
                    {
                        int seekX = GetPixelForNote(GetSeekFrameToDraw());
                        r.cb.DrawLine(seekX, 0, seekX, Height, GetSeekBarBrush(), 3);
                    }

                    // Highlight note under mouse.
                    var hoverNote = (Note)null;
                    var hoverLocation = NoteLocation.Invalid;
                    var hoverReleased = false;
                    var hoverLastNoteValue = Note.NoteInvalid;
                    var hoverLastInstrument = (Instrument)null;

                    if (editMode != EditionMode.VideoRecording)
                    {
                        if (hoverNoteLocation != NoteLocation.Invalid && CaptureOperationRequiresNoteHighlight(captureOperation))
                        {
                            hoverLocation = hoverNoteLocation;
                            hoverNote = song.Channels[editChannel].GetNoteAt(hoverLocation);
                        }
                        else if (captureOperation == CaptureOperation.None)
                        {
                            var pt = PointToClient(Cursor.Position);
                            hoverNote = GetNoteForCoord(pt.X, pt.Y, out _, out hoverLocation, out _);
                        }
                    }

                    var ghostChannelMask = App != null ? App.GhostChannelMask : 0;
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

                                if (isActiveChannel && it.Location == hoverLocation)
                                {
                                    hoverReleased = released;
                                    hoverLastNoteValue = lastNoteValue;
                                    hoverLastInstrument = lastInstrument;
                                }

                                if (note.IsMusical)
                                {
                                    RenderNote(r, it.Location, note, song, channel, it.DistanceToNextCut, drawImplicitStopNotes, isActiveChannel, false, released);
                                }
                                else if (note.IsStop)
                                {
                                    RenderNoteReleaseOrStop(r, note, GetNoteColor(channel, lastNoteValue, lastInstrument, song.Project), it.Location.ToAbsoluteNoteIndex(Song), lastNoteValue, false, IsNoteSelected(it.Location, 1), true, released);
                                }

                                if (note.HasRelease && note.Release < Math.Min(note.Duration, it.DistanceToNextCut))
                                {
                                    released = true;
                                }
                            }

                            if (isActiveChannel && hoverNote != null)
                            {
                                if (hoverNote.IsMusical)
                                {
                                    RenderNote(r, hoverLocation, hoverNote, song, channel, channel.GetDistanceToNextNote(hoverLocation), drawImplicitStopNotes, true, true, hoverReleased);
                                }
                                else if (hoverNote.IsStop)
                                {
                                    RenderNoteReleaseOrStop(r, hoverNote, GetNoteColor(channel, hoverLastNoteValue, hoverLastInstrument, song.Project), hoverLocation.ToAbsoluteNoteIndex(Song), hoverLastNoteValue, true, false, true, hoverReleased);
                                }
                            }
                        }
                    }

                    // Draw effect icons at the top.
                    if (editMode != EditionMode.VideoRecording)
                    {
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

                                            r.cf.DrawBitmapAtlas(bmpEffectAtlas, EffectImageNames.Length - 1, iconX, iconY, 1.0f, 1.0f, drawOpaque ? Theme.LightGreyFillColor1 : Theme.MediumGreyFillColor1);
                                            r.cf.DrawBitmapAtlas(bmpEffectAtlas, fx, iconX, iconY, drawOpaque ? 1.0f : 0.4f, 1.0f, Theme.LightGreyFillColor1);
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
                        var channelType = song.Channels[editChannel].Type;
                        var channelName = song.Channels[editChannel].NameWithExpansion;

                        r.cb.DrawText($"Editing {channelName} Channel", ThemeResources.FontVeryLarge, bigTextPosX, maxEffectPosY > 0 ? maxEffectPosY : bigTextPosY, whiteKeyBrush);
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

                            r.cf.PushTranslation(0, y);
                            r.cf.FillAndDrawRectangle(0, 0, Width - whiteKeySizeX, noteSizeY, r.g.GetVerticalGradientBrush(mapping.Sample.Color, noteSizeY, 0.8f), ThemeResources.BlackBrush, 1);

                            string text = $"{mapping.Sample.Name} - Pitch: {DPCMSampleRate.GetString(true, FamiStudio.StaticInstance.PalPlayback, true, true, mapping.Pitch)}";
                            if (mapping.Loop) text += ", Looping";

                            r.cf.DrawText(text, ThemeResources.FontSmall, dpcmTextPosX, dpcmTextPosY, ThemeResources.BlackBrush);
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
                        var pt = PointToClient(Cursor.Position);

                        if (GetNoteValueForCoord(pt.X, pt.Y, out var noteValue) && App.Project.NoteSupportsDPCM(noteValue))
                        {
                            var y = virtualSizeY - noteValue * noteSizeY - scrollY;
                            r.cf.PushTranslation(0, y);
                            r.cf.FillAndDrawRectangle(0, 0, Width - whiteKeySizeX, noteSizeY, r.g.GetVerticalGradientBrush(dragSample.Color, noteSizeY, 0.8f), ThemeResources.WhiteBrush, 2, true);
                            r.cf.PopTransform();
                        }
                    }
                    else if (captureOperation == CaptureOperation.None)
                    {
                        var pt = PointToClient(Cursor.Position);
                        if (GetLocationForCoord(pt.X, pt.Y, out _, out var hoverNoteValue))
                        {
                            var mapping = App.Project.GetDPCMMapping(hoverNoteValue);
                            if (mapping != null)
                            {
                                var y = virtualSizeY - hoverNoteValue * noteSizeY - scrollY;

                                r.cf.PushTranslation(0, y);
                                r.cf.DrawRectangle(0, 0, Width - whiteKeySizeX, noteSizeY, ThemeResources.WhiteBrush, 2, true);
                                r.cf.PopTransform();
                            }
                        }
                    }

                    r.cb.DrawText($"Editing DPCM Samples Instrument ({App.Project.GetTotalMappedSampleSize()} / {Project.MaxMappedSampleSize} Bytes)", ThemeResources.FontVeryLarge, bigTextPosX, bigTextPosY, whiteKeyBrush);
                }
            }
            else if (editMode == EditionMode.Enveloppe || editMode == EditionMode.Arpeggio)
            {
                // Draw the enveloppe value backgrounds
                int maxValue = 128 / envelopeValueZoom;
                int midValue = 64 / envelopeValueZoom;
                int maxVisibleValue = maxValue - Math.Min((int)Math.Floor(scrollY / envelopeSizeY), maxValue);
                int minVisibleValue = maxValue - Math.Max((int)Math.Ceiling((scrollY + Height) / envelopeSizeY), 0);

                var env = EditEnvelope;
                var spacing = editEnvelope == EnvelopeType.DutyCycle ? 4 : (editEnvelope == EnvelopeType.Arpeggio ? 12 : 16);

                for (int i = minVisibleValue; i <= maxVisibleValue; i++)
                {
                    var value = i - 64;
                    var y = (virtualSizeY - envelopeSizeY * i) - scrollY;
                    r.cb.DrawLine(0, y, GetPixelForNote(env.Length), y, ThemeResources.DarkGreyLineBrush1, (value % spacing) == 0 ? 3 : 1);
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

                if (editEnvelope == EnvelopeType.Arpeggio)
                {
                    for (int i = 0; i < env.Length; i++)
                    {
                        var x = GetPixelForNote(i);
                        var y = (virtualSizeY - envelopeSizeY * (env.Values[i] + midValue)) - scrollY;
                        var selected = IsEnvelopeValueSelected(i);
                        r.cf.FillAndDrawRectangle(x, y - envelopeSizeY, x + noteSizeX, y, r.g.GetVerticalGradientBrush(Theme.LightGreyFillColor1, (int)envelopeSizeY, 0.8f), ThemeResources.BlackBrush, selected ? 2 : 1, selected);
                        var label = env.Values[i].ToString("+#;-#;0");
                        if (label.Length * fontSmallCharSizeX + 2 < noteSizeX)
                            r.cf.DrawText(label, ThemeResources.FontSmall, x, y - envelopeSizeY - effectValuePosTextOffsetY, ThemeResources.LightGreyFillBrush1, RenderTextFlags.Center, noteSizeX);
                    }
                }
                else
                {
                    for (int i = 0; i < env.Length; i++)
                    {
                        var center = editEnvelope == EnvelopeType.FdsWaveform ? 32 : 0;
                        int val = env.Values[i];

                        float y0, y1, ty;
                        if (val >= center)
                        {
                            y0 = (virtualSizeY - envelopeSizeY * (val + midValue + 1)) - scrollY;
                            y1 = (virtualSizeY - envelopeSizeY * (midValue + center) - scrollY);
                            ty = y0;
                        }
                        else
                        {
                            y1 = (virtualSizeY - envelopeSizeY * (val + midValue)) - scrollY;
                            y0 = (virtualSizeY - envelopeSizeY * (midValue + center + 1) - scrollY);
                            ty = y1;
                        }

                        var x = GetPixelForNote(i);
                        var selected = IsEnvelopeValueSelected(i);

                        r.cf.FillAndDrawRectangle(x, y0, x + noteSizeX, y1, ThemeResources.LightGreyFillBrush1, ThemeResources.BlackBrush, selected ? 2 : 1, selected);

                        bool drawOutside = Math.Abs(y1 - y0) < (DefaultEnvelopeSizeY * MainWindowScaling * 2);
                        var brush = drawOutside ? ThemeResources.LightGreyFillBrush1 : ThemeResources.BlackBrush;
                        var offset = drawOutside != val < center ? -effectValuePosTextOffsetY : effectValueNegTextOffsetY;

                        var label = val.ToString();
                        if (label.Length * fontSmallCharSizeX + 2 < noteSizeX)
                            r.cf.DrawText(label, ThemeResources.FontSmall, x, ty + offset, brush, RenderTextFlags.Center, noteSizeX);
                    }
                }

                if (editMode == EditionMode.Enveloppe)
                {
                    var envelopeString = EnvelopeType.Names[editEnvelope];

                    if (editEnvelope == EnvelopeType.Pitch)
                        envelopeString = (editInstrument.Envelopes[editEnvelope].Relative ? "Relative " : "Absolute ") + envelopeString;

                    r.cb.DrawText($"Editing Instrument {editInstrument.Name} ({envelopeString})", ThemeResources.FontVeryLarge, bigTextPosX, bigTextPosY, whiteKeyBrush);
                }
                else
                {
                    r.cb.DrawText($"Editing Arpeggio {editArpeggio.Name}", ThemeResources.FontVeryLarge, bigTextPosX, bigTextPosY, whiteKeyBrush);
                }
            }

            r.cb.PopTransform();

            if (!string.IsNullOrEmpty(noteTooltip) && editMode != EditionMode.DPCM)
            {
                // MATTT : The position of the text is wrong here. Creates asserts and shit when minimizing (or resizing?) the window
                //r.cb.DrawText(noteTooltip, ThemeResources.FontLarge, 0, Height - tooltipTextPosY - scrollBarThickness, whiteKeyBrush, RenderTextFlags.Right, Width - tooltipTextPosX);
            }
        }

        private void RenderNoteBody(RenderInfo r, Note note, Color color, int time, int noteLen, bool outline, bool selected, bool activeChannel, bool released, bool isFirstPart, int slideDuration = -1)
        {
            int x = GetPixelForNote(time);
            int y = virtualSizeY - note.Value * noteSizeY - scrollY;
            int sy = released ? releaseNoteSizeY : noteSizeY;

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
                r.cf.FillRectangle(0, 0, sx, sy, r.g.GetVerticalGradientBrush(color, sy, 0.8f));

            r.cf.DrawRectangle(0, 0, sx, sy, outline ? hoverNoteBrush : (selected ? selectionNoteBrush : ThemeResources.BlackBrush), selected || outline ? 2 : 1, selected || outline);

            if (!outline)
            {
                if (activeChannel)
                {
                    if (isFirstPart && note.HasAttack && sx > noteAttackSizeX + attackIconPosX * 2 + 2)
                    {
                        r.cf.FillRectangle(attackIconPosX + 1, attackIconPosX + 1, attackIconPosX + noteAttackSizeX + 1, sy - attackIconPosX, attackBrush);
                        noteTextPosX += noteAttackSizeX + attackIconPosX + 2;
                    }

                    if (Settings.ShowNoteLabels && !released && editMode == EditionMode.Channel && note.IsMusical && ThemeResources.FontSmall.Size < noteSizeY)
                    {
                        var label = note.FriendlyName;
                        if ((sx - noteTextPosX) > (label.Length + 1) * fontSmallCharSizeX)
                            r.cf.DrawText(note.FriendlyName, ThemeResources.FontSmall, noteTextPosX, 1, ThemeResources.BlackBrush, RenderTextFlags.Middle, 0, noteSizeY - 1);
                    }
                }

                if (note.Arpeggio != null)
                {
                    var offsets = note.Arpeggio.GetChordOffsets();
                    foreach (var offset in offsets)
                    {
                        r.cf.PushTranslation(0, offset * -noteSizeY);
                        r.cf.FillRectangle(0, 1, sx, sy, r.g.GetSolidBrush(note.Arpeggio.Color, 1.0f, 0.2f));
                        r.cf.PopTransform();
                    }
                }
            }

            r.cf.PopTransform();
        }

        private void RenderNoteReleaseOrStop(RenderInfo r, Note note, Color color, int time, int value, bool outline, bool selected, bool stop, bool released)
        {
            int x = GetPixelForNote(time);
            int y = virtualSizeY - value * noteSizeY - scrollY;

            var geo = stop ? (released ? stopReleaseNoteGeometry : stopNoteGeometry) : releaseNoteGeometry;

            r.cf.PushTransform(x, y, noteSizeX, 1);
            if (!outline)
                r.cf.FillGeometry(geo[0], r.g.GetVerticalGradientBrush(color, noteSizeY, 0.8f));
            r.cf.DrawGeometry(geo[0], outline ? hoverNoteBrush : (selected ? selectionNoteBrush : ThemeResources.BlackBrush), outline || selected ? 2 : 1, true);
            r.cf.PopTransform();

            r.cf.PushTranslation(x, y);
            if (!outline && note.Arpeggio != null)
            {
                var offsets = note.Arpeggio.GetChordOffsets();
                foreach (var offset in offsets)
                {
                    r.cf.PushTransform(0, offset * -noteSizeY, noteSizeX, 1);
                    r.cf.FillGeometry(geo[1], r.g.GetSolidBrush(note.Arpeggio.Color, 1.0f, 0.2f), true);
                    r.cf.PopTransform();
                }
            }
            r.cf.PopTransform();
        }

        private void RenderNote(RenderInfo r, NoteLocation location, Note note, Song song, Channel channel, int distanceToNextNote, bool drawImplicityStopNotes, bool isActiveChannel, bool hover, bool released)
        {
            Debug.Assert(note.IsMusical);

            if (distanceToNextNote < 0)
                distanceToNextNote = (int)ushort.MaxValue;

            var absoluteIndex = location.ToAbsoluteNoteIndex(Song);
            var nextAbsoluteIndex = absoluteIndex + distanceToNextNote;
            var duration = Math.Min(distanceToNextNote, note.Duration);
            var slideDuration = note.IsSlideNote ? channel.GetSlideNoteDuration(location) : -1;
            var color = GetNoteColor(channel, note.Value, note.Instrument, song.Project);
            var selected = isActiveChannel && IsNoteSelected(location, duration);

            if (!isActiveChannel)
                color = Color.FromArgb((int)(color.A * 0.2f), color);

            // Draw first part, from start to release point.
            if (note.HasRelease)
            {
                RenderNoteBody(r, note, color, absoluteIndex, Math.Min(note.Release, duration), hover, selected, isActiveChannel, released, true, slideDuration);
                absoluteIndex += note.Release;
                duration -= note.Release;

                if (duration > 0)
                {
                    RenderNoteReleaseOrStop(r, note, color, absoluteIndex, note.Value, hover, selected, false, released);
                    absoluteIndex++;
                    duration--;
                }

                released = true;
            }

            // Then second part, after release to stop note.
            if (duration > 0)
            {
                RenderNoteBody(r, note, color, absoluteIndex, duration, hover, selected, isActiveChannel, released, !note.HasRelease, slideDuration);
                absoluteIndex += duration;
                duration -= duration;

                if (drawImplicityStopNotes && absoluteIndex < nextAbsoluteIndex && !hover)
                {
                    RenderNoteReleaseOrStop(r, note, Color.FromArgb(128, color), absoluteIndex, note.Value, hover, selected, true, released);
                }
            }
        }

        private float GetPixelForWaveTime(float time, int scroll = 0)
        {
            var viewSize = Width - whiteKeySizeX;
            var viewTime = DefaultZoomWaveTime / zoom;

            return time / viewTime * viewSize - scroll;
        }

        private float GetWaveTimeForPixel(int x)
        {
            var viewSize = Width - whiteKeySizeX;
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

        private void RenderWave(RenderInfo r, short[] data, float rate, RenderBrush brush, bool isSource, bool drawSamples)
        {
            var viewWidth     = Width - whiteKeySizeX;
            var halfHeight    = (Height - headerAndEffectSizeY) / 2;
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

        private void RenderDmc(RenderInfo r, byte[] data, float rate, float baseTime, RenderBrush brush, bool isSource, bool drawSamples, int dmcInitialValue)
        {
            var viewWidth     = Width - whiteKeySizeX;
            var realHeight    = Height - headerAndEffectSizeY;
            var halfHeight    = realHeight / 2;
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
            if (editMode != EditionMode.DPCM)
                return;

            r.cb.PushTranslation(whiteKeySizeX, headerAndEffectSizeY);

            // Source data range.
            r.cb.FillRectangle(
                GetPixelForWaveTime(0, scrollX), 0,
                GetPixelForWaveTime(editSample.SourceDuration, scrollX), Height, ThemeResources.DarkGreyFillBrush1);

            // Horizontal center line
            var sizeY   = Height - headerAndEffectSizeY;
            var centerY = sizeY * 0.5f;
            r.cb.DrawLine(0, centerY, Width, centerY, ThemeResources.BlackBrush);

            // Top/bottom dash lines (limits);
            var topY    = waveDisplayPaddingY;
            var bottomY = (Height - headerAndEffectSizeY) - waveDisplayPaddingY;
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

                r.cb.DrawLine(x, 0, x, Height, brush, 1.0f, false, dash);
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
            r.cb.DrawText($"Editing DPCM Sample {editSample.Name}", ThemeResources.FontVeryLarge, bigTextPosX, textY, whiteKeyBrush);
            textY += ThemeResources.FontVeryLarge.LineHeight;
            r.cb.DrawText($"Source Data ({(editSample.SourceDataIsWav ? "WAV" : "DMC")}) : {editSample.SourceSampleRate} Hz, {editSample.SourceDataSize} Bytes, {(int)(editSample.SourceDuration * 1000)} ms", ThemeResources.FontMedium, bigTextPosX, textY, whiteKeyBrush);
            textY += ThemeResources.FontMedium.LineHeight;
            r.cb.DrawText($"Processed Data (DMC) : {DPCMSampleRate.GetString(false, App.PalPlayback, true, true, editSample.SampleRate)}, {editSample.ProcessedData.Length} Bytes, {(int)(editSample.ProcessedDuration * 1000)} ms", ThemeResources.FontMedium, bigTextPosX, textY, whiteKeyBrush);
            textY += ThemeResources.FontMedium.LineHeight;
            r.cb.DrawText($"Preview Playback : {DPCMSampleRate.GetString(false, App.PalPlayback, true, true, editSample.PreviewRate)}, {(int)(editSample.GetPlaybackDuration(App.PalPlayback) * 1000)} ms", ThemeResources.FontMedium, bigTextPosX, textY, whiteKeyBrush);

            r.cb.PopTransform();

            if (!string.IsNullOrEmpty(noteTooltip))
            {
                r.cb.DrawText(noteTooltip, ThemeResources.FontLarge, 0, Height - tooltipTextPosY, whiteKeyBrush, RenderTextFlags.Right, Width - tooltipTextPosX);
            }
        }

        public void RenderVideoFrame(RenderGraphics g, int channel, int patternIndex, float noteIndex, float centerNote, int highlightKey, Color highlightColor)
        {
            Debug.Assert(editMode == EditionMode.VideoRecording);

            int noteY = (int)Math.Round(virtualSizeY - centerNote * noteSizeY + noteSizeY / 2);

            editChannel = channel;
            scrollX = (int)Math.Round((Song.GetPatternStartAbsoluteNoteIndex(patternIndex) + noteIndex) * (double)noteSizeX);
            scrollY = noteY - (Height - headerAndEffectSizeY) / 2;
            playingNote = highlightKey;
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
                    r.cs.PushTranslation(whiteKeySizeX - 1, 0);
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

        protected override void OnRender(RenderGraphics g)
        {
            // Init
            var r = new RenderInfo();

            var minVisibleNoteIdx = Math.Max(GetNoteForPixel(0), 0);
            var maxVisibleNoteIdx = Math.Min(GetNoteForPixel(Width) + 1, Song.GetPatternStartAbsoluteNoteIndex(Song.Length));

            r.g = g;
            r.cc = g.CreateCommandList();
            r.ch = g.CreateCommandList();
            r.cp = g.CreateCommandList();
            r.cb = g.CreateCommandList();
            r.cf = g.CreateCommandList();
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
                r.maxVisibleWaveTime = GetWaveTimeForPixel(Width - whiteKeySizeX);
            }

            ConditionalUpdateNoteGeometries(g);

            // Prepare command list.
            RenderHeader(r);
            RenderEffectList(r);
            RenderEffectPanel(r);
            RenderPiano(r);
            RenderNotes(r);
            RenderWaveform(r);
            RenderScrollBars(r);

            // Submit draw calls.
            var cornerRect = new Rectangle(0, 0, whiteKeySizeX, headerAndEffectSizeY);
            var headerRect = new Rectangle(whiteKeySizeX, 0, Width, headerAndEffectSizeY);
            var pianoRect  = new Rectangle(0, headerAndEffectSizeY, whiteKeySizeX, Height);
            var notesRect  = new Rectangle(whiteKeySizeX, headerAndEffectSizeY, Width, Height);

            g.Clear(Theme.DarkGreyLineColor2);
            g.DrawCommandList(r.cc, cornerRect);
            g.DrawCommandList(r.ch, headerRect);
            g.DrawCommandList(r.cp, pianoRect);
            g.DrawCommandList(r.cb, notesRect);
            g.DrawCommandList(r.cf, notesRect);
            g.DrawCommandList(r.cs, notesRect);
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

                    int scrollAreaSizeX = Width - whiteKeySizeX;
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

        void ResizeEnvelope(MouseEventArgs e)
        {
            var env = EditEnvelope;
            int length = GetNoteForPixel(e.X - whiteKeySizeX);

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

        void StartChangeEffectValue(MouseEventArgs e, NoteLocation location)
        {
            var channel   = Song.Channels[editChannel];
            var pattern   = channel.PatternInstances[location.PatternIndex];
            var note      = channel.GetNoteAt(location);
            var selection = IsSelectionValid() && IsNoteSelected(location) && note != null && note.HasValidEffectValue(selectedEffectIdx);

            StartCaptureOperation(e.X, e.Y, selection ? CaptureOperation.ChangeSelectionEffectValue : CaptureOperation.ChangeEffectValue, false, location.ToAbsoluteNoteIndex(Song));

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

            UpdateChangeEffectValue(e);
        }

        void UpdateChangeEffectValue(MouseEventArgs e)
        {
            Debug.Assert(selectedEffectIdx >= 0);

            App.UndoRedoManager.RestoreTransaction(false);

            var shift = ModifierKeys.HasFlag(Keys.Shift);

            var channel = Song.Channels[editChannel];
            var pattern = channel.PatternInstances[captureNoteLocation.PatternIndex];

            if (pattern == null)
                pattern = channel.CreatePatternAndInstance(captureNoteLocation.PatternIndex);

            var minValue = Note.GetEffectMinValue(Song, channel, selectedEffectIdx);
            var maxValue = Note.GetEffectMaxValue(Song, channel, selectedEffectIdx);

            var note = pattern.GetOrCreateNoteAt(captureNoteLocation.NoteIndex);

            if (!note.HasValidEffectValue(selectedEffectIdx))
                note.SetEffectValue(selectedEffectIdx, Note.GetEffectDefaultValue(Song, selectedEffectIdx));

            var originalValue = note.GetEffectValue(selectedEffectIdx);
            var delta = 0;

            if (shift)
            {
                delta = (captureMouseY - e.Y) / 4;
            }
            else
            {
                var ratio = Utils.Clamp(1.0f - (e.Y - headerSizeY) / (float)effectPanelSizeY, 0.0f, 1.0f);
                var newValue = (int)Math.Round(ratio * (maxValue - minValue) + minValue);
                delta = newValue - originalValue;
            }

            if (captureOperation == CaptureOperation.ChangeSelectionEffectValue)
            {
                var minLocation = NoteLocation.FromAbsoluteNoteIndex(Song, selectionMin);
                var maxLocation = NoteLocation.FromAbsoluteNoteIndex(Song, selectionMax);

                for (var it = channel.GetSparseNoteIterator(minLocation, maxLocation, Note.GetFilterForEffect(selectedEffectIdx)); !it.Done; it.Next())
                {
                    var value = it.Note.GetEffectValue(selectedEffectIdx);
                    it.Note.SetEffectValue(selectedEffectIdx, value + delta);
                }

                channel.InvalidateCumulativePatternCache(minLocation.PatternIndex, maxLocation.PatternIndex);
            }
            else
            {
                var value = note.GetEffectValue(selectedEffectIdx);
                note.SetEffectValue(selectedEffectIdx, value + delta);

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

        void UpdateDragVolumeSlide(MouseEventArgs e, bool final)
        {
            var channel = Song.Channels[editChannel];
            var pattern = channel.PatternInstances[captureNoteLocation.PatternIndex];
            var note    = pattern.Notes[captureNoteLocation.NoteIndex];

            var ratio = Utils.Clamp(1.0f - (e.Y - headerSizeY) / (float)effectPanelSizeY, 0.0f, 1.0f);
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
        }

        void DrawEnvelope(MouseEventArgs e, bool first = false)
        {
            if (GetEnvelopeValueForCoord(e.X, e.Y, out int idx1, out sbyte val1))
            {
                int idx0;
                sbyte val0;

                if (first || !GetEnvelopeValueForCoord(mouseLastX, mouseLastY, out idx0, out val0))
                {
                    idx0 = idx1;
                    val0 = val1;
                }

                Envelope.GetMinMaxValue(editInstrument, editEnvelope, out int min, out int max);

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

        protected bool PointInRectangle(Rectangle rect, int x, int y)
        {
            return (x >= rect.Left && x <= rect.Right &&
                    y >= rect.Top && y <= rect.Bottom);
        }

        protected int GetPianoNote(int x, int y)
        {
            y -= headerAndEffectSizeY;

            for (int i = 0; i < NumOctaves; i++)
            {
                for (int j = 0; j < 12 && i * 12 + j < NumNotes; j++)
                {
                    if (IsBlackKey(j) && PointInRectangle(GetKeyRectangle(i, j), x, y))
                        return i * 12 + j + 1;
                }
                for (int j = 0; j < 12 && i * 12 + j < NumNotes; j++)
                {
                    if (!IsBlackKey(j) && PointInRectangle(GetKeyRectangle(i, j), x, y))
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

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);

            bool left = e.Button.HasFlag(MouseButtons.Left);
            bool right = e.Button.HasFlag(MouseButtons.Right);

            if (editMode == EditionMode.DPCMMapping && left)
            {
                if (GetNoteValueForCoord(e.X, e.Y, out byte noteValue))
                {
                    // In case we were dragging a sample.
                    EndCaptureOperation(e);

                    var mapping = App.Project.GetDPCMMapping(noteValue);
                    if (left && mapping != null)
                    {
                        var strings = DPCMSampleRate.GetStringList(true, FamiStudio.StaticInstance.PalPlayback, true, true);

#if !FAMISTUDIO_ANDROID // DROIDTODO
                        var dlg = new PropertyDialog(PointToScreen(new Point(e.X, e.Y)), 280, false, e.Y > Height / 2);
                        dlg.Properties.AddDropDownList("Pitch :", strings, strings[mapping.Pitch]); // 0
                        dlg.Properties.AddCheckBox("Loop :", mapping.Loop); // 1
                        dlg.Properties.Build();

                        if (dlg.ShowDialog(ParentForm) == DialogResult.OK)
                        {
                            App.UndoRedoManager.BeginTransaction(TransactionScope.DPCMSamplesMapping);
                            mapping.Pitch = dlg.Properties.GetSelectedIndex(0);
                            mapping.Loop = dlg.Properties.GetPropertyValue<bool>(1);
                            App.UndoRedoManager.EndTransaction();
                            MarkDirty();
                        }
#endif
                    }
                }
            }
            else if (editMode == EditionMode.Channel)
            {
                if (right)
                {
                    if (IsPointInHeader(e.X, e.Y))
                    {
                        int patIdx = Song.PatternIndexFromAbsoluteNoteIndex(GetNoteForPixel(e.X - whiteKeySizeX));
                        if (patIdx >= 0 && patIdx < Song.Length)
                            SetSelection(Song.GetPatternStartAbsoluteNoteIndex(patIdx), Song.GetPatternStartAbsoluteNoteIndex(patIdx + 1) - 1);
                    }
                    else if (IsPointInEffectPanel(e.X, e.Y))
                    {
                        if (GetEffectNoteForCoord(e.X, e.Y, out var location))
                            SetSelection(Song.GetPatternStartAbsoluteNoteIndex(location.PatternIndex), Song.GetPatternStartAbsoluteNoteIndex(location.PatternIndex + 1) - 1);
                    }
                    else if (GetLocationForCoord(e.X, e.Y, out var location, out byte noteValue))
                    {
                        var channel = Song.Channels[editChannel];
                        var note = channel.FindMusicalNoteAtLocation(ref location, -1);
                        if (note != null && (note.IsStop || note.IsMusical && note.Value != noteValue))
                        {
                            var absoluteNoteIndex = location.ToAbsoluteNoteIndex(Song);
                            SetSelection(absoluteNoteIndex, absoluteNoteIndex + Math.Min(note.Duration, channel.GetDistanceToNextNote(location)) - 1);
                        }
                    }

                    MarkDirty();
                }
            }
        }

        private void CaptureMouse(int x, int y)
        {
            mouseLastX = x;
            mouseLastY = y;
            captureMouseX = x;
            captureMouseY = y;
            captureScrollX = scrollX;
            captureScrollY = scrollY;
            Capture = true;
        }

        private void StartCaptureOperation(int x, int y, CaptureOperation op, bool allowSnap = false, int noteIdx = -1)
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
            captureWaveTime = editMode == EditionMode.DPCM ? GetWaveTimeForPixel(x - whiteKeySizeX) : 0.0f;
            captureNoteValue = NumNotes - Utils.Clamp((y + scrollY - headerAndEffectSizeY) / noteSizeY, 0, NumNotes);
            captureSelectionMin = selectionMin;
            captureSelectionMax = selectionMax;

            captureMouseAbsoluteIdx = GetNoteForPixel(x - whiteKeySizeX);
            if (allowSnap)
                captureMouseAbsoluteIdx = SnapNote(captureMouseAbsoluteIdx);
            captureMouseLocation = Song.AbsoluteNoteIndexToNoteLocation(captureMouseAbsoluteIdx);

            captureNoteAbsoluteIdx = noteIdx >= 0 ? noteIdx : captureMouseAbsoluteIdx;
            captureNoteLocation = Song.AbsoluteNoteIndexToNoteLocation(captureNoteAbsoluteIdx);

            if (noteIdx >= 0)
                hoverNoteLocation = captureNoteLocation;
        }

        private void UpdateScrollBarX(MouseEventArgs e)
        {
            GetScrollBarParams(true, out _, out var scrollBarSizeX);
            GetMinMaxScroll(out _, out _, out var maxScrollX, out _);

            int scrollAreaSizeX = Width - whiteKeySizeX;
            scrollX = (int)Math.Round(captureScrollX + ((e.X - captureMouseX) / (float)(scrollAreaSizeX - scrollBarSizeX) * maxScrollX));

            ClampScroll();
            MarkDirty();
        }

        private void UpdateScrollBarY(MouseEventArgs e)
        {
            GetScrollBarParams(false, out _, out var scrollBarSizeY);
            GetMinMaxScroll(out _, out _, out _, out var maxScrollY);

            int scrollAreaSizeY = Height - headerAndEffectSizeY;
            scrollY = (int)Math.Round(captureScrollY + ((e.Y - captureMouseY) / (float)(scrollAreaSizeY - scrollBarSizeY) * maxScrollY));

            ClampScroll();
            MarkDirty();
        }

        private void UpdateCaptureOperation(MouseEventArgs e, bool realTime)
        {
            if (captureOperation != CaptureOperation.None && !captureThresholdMet)
            {
                if (Math.Abs(e.X - captureMouseX) > 4 ||
                    Math.Abs(e.Y - captureMouseY) > 4)
                {
                    captureThresholdMet = true;
                }
            }

            if (captureOperation != CaptureOperation.None && captureThresholdMet && (captureRealTimeUpdate || !realTime))
            {
                switch (captureOperation)
                {
                    case CaptureOperation.DragLoop:
                    case CaptureOperation.DragRelease:
                    case CaptureOperation.ResizeEnvelope:
                        ResizeEnvelope(e);
                        break;
                    case CaptureOperation.PlayPiano:
                        PlayPiano(e.X, e.Y);
                        break;
                    case CaptureOperation.ChangeEffectValue:
                    case CaptureOperation.ChangeSelectionEffectValue:
                        UpdateChangeEffectValue(e);
                        break;
                    case CaptureOperation.DragSample:
                        UpdateDragDPCMSampleMapping(e);
                        break;
                    case CaptureOperation.DrawEnvelope:
                        DrawEnvelope(e);
                        break;
                    case CaptureOperation.Select:
                        UpdateSelection(e);
                        break;
                    case CaptureOperation.SelectWave:
                        UpdateWaveSelection(e);
                        break;
                    case CaptureOperation.DragSlideNoteTarget:
                    case CaptureOperation.CreateSlideNote:
                        UpdateSlideNoteCreation(e, false);
                        break;
                    case CaptureOperation.DragVolumeSlideTarget:
                        UpdateDragVolumeSlide(e, false);
                        break;
                    case CaptureOperation.CreateNote:
                        UpdateNoteCreation(e, false, false);
                        break;
                    case CaptureOperation.ResizeNoteEnd:
                    case CaptureOperation.ResizeSelectionNoteEnd:
                        UpdateNoteResizeEnd(e, false);
                        break;
                    case CaptureOperation.MoveNoteRelease:
                        UpdateMoveNoteRelease(e);
                        break;
                    case CaptureOperation.DragNote:
                    case CaptureOperation.DragSelection:
                    case CaptureOperation.ResizeNoteStart:
                    case CaptureOperation.ResizeSelectionNoteStart:
                        UpdateNoteDrag(e, false);
                        break;
                    case CaptureOperation.AltZoom:
                        UpdateAltZoom(e);
                        break;
                    case CaptureOperation.DragSeekBar:
                        UpdateSeekDrag(e, false);
                        break;
                    case CaptureOperation.DragWaveVolumeEnvelope:
                        UpdateVolumeEnvelopeDrag(e, false);
                        break;
                    case CaptureOperation.ScrollBarX:
                        UpdateScrollBarX(e);
                        break;
                    case CaptureOperation.ScrollBarY:
                        UpdateScrollBarY(e);
                        break;
                }
            }
        }
        private void UpdateDragDPCMSampleMapping(MouseEventArgs e)
        {
            if (draggedSample == null)
            {
                App.UndoRedoManager.BeginTransaction(TransactionScope.DPCMSamplesMapping);
                draggedSample = App.Project.GetDPCMMapping(captureNoteValue);
                App.Project.UnmapDPCMSample(captureNoteValue);
            }
        }

        private void EndDragDPCMSampleMapping(MouseEventArgs e)
        {
            if (draggedSample != null)
            {
                if (GetNoteValueForCoord(e.X, e.Y, out var noteValue) && App.Project.NoteSupportsDPCM(noteValue) && noteValue != captureNoteValue && draggedSample != null)
                {
                    var sample = draggedSample;

                    // Map the sample right away so that it renders correctly as the message box pops.
                    App.Project.UnmapDPCMSample(noteValue);
                    App.Project.MapDPCMSample(noteValue, sample.Sample, sample.Pitch, sample.Loop);

                    draggedSample = null;

                    if (PlatformUtils.MessageBox($"Do you want to transpose all the notes using this sample?", "Remap DPCM Sample", MessageBoxButtons.YesNo) == DialogResult.Yes)
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
                }
                else
                {
                    App.UndoRedoManager.RestoreTransaction(false);
                    App.UndoRedoManager.AbortTransaction();

                    if (noteValue != captureNoteValue && draggedSample != null)
                        SystemSounds.Beep.Play();
                }
            }
        }

        private void EndCaptureOperation(MouseEventArgs e)
        {
            if (captureOperation != CaptureOperation.None)
            {
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
                        UpdateSlideNoteCreation(e, true);
                        break;
                    case CaptureOperation.DragVolumeSlideTarget:
                        UpdateDragVolumeSlide(e, true);
                        break;
                    case CaptureOperation.CreateNote:
                        UpdateNoteCreation(e, false, true);
                        break;
                    case CaptureOperation.ResizeNoteEnd:
                    case CaptureOperation.ResizeSelectionNoteEnd:
                        UpdateNoteResizeEnd(e, true);
                        break;
                    case CaptureOperation.DragNote:
                    case CaptureOperation.DragSelection:
                    case CaptureOperation.ResizeNoteStart:
                    case CaptureOperation.ResizeSelectionNoteStart:
                        UpdateNoteDrag(e, true, !captureThresholdMet);
                        break;
                    case CaptureOperation.DragSample:
                        EndDragDPCMSampleMapping(e);
                        break;
                    case CaptureOperation.DragSeekBar:
                        UpdateSeekDrag(e, true);
                        break;
                    case CaptureOperation.DragWaveVolumeEnvelope:
                        UpdateVolumeEnvelopeDrag(e, true);
                        break;
                    case CaptureOperation.DragLoop:
                    case CaptureOperation.DragRelease:
                    case CaptureOperation.ChangeEffectValue:
                    case CaptureOperation.ChangeSelectionEffectValue:
                    case CaptureOperation.MoveNoteRelease:
                        App.UndoRedoManager.EndTransaction();
                        break;
                }

                draggedSample = null;
                captureOperation = CaptureOperation.None;
                hoverNoteLocation = NoteLocation.Invalid;
                Capture = false;
                panning = false;

                MarkDirty();
            }
        }

        private void AbortCaptureOperation()
        {
            if (captureOperation != CaptureOperation.None)
            {
                if (App.UndoRedoManager.HasTransactionInProgress)
                    App.UndoRedoManager.AbortTransaction();

                MarkDirty();
                App.StopInstrument();

                captureOperation = CaptureOperation.None;
                Capture = false;
                panning = false;
                hoverNoteLocation = NoteLocation.Invalid;

                ManyPatternChanged?.Invoke();
            }
        }

        private bool IsSelectionValid()
        {
            return selectionMin >= 0 && selectionMax >= 0;
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

            Envelope.GetMinMaxValue(editInstrument, editEnvelope, out int minVal, out int maxVal);

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

        protected override void OnKeyDown(KeyEventArgs e)
        {
            UpdateCursor();

            if (captureOperation != CaptureOperation.None)
                return;

            bool ctrl  = e.Modifiers.HasFlag(Keys.Control);
            bool shift = e.Modifiers.HasFlag(Keys.Shift);

            if (e.KeyCode == Keys.Escape)
            {
                ClearSelection();
                MarkDirty();
            }
            else if (e.KeyCode == Keys.S && shift)
            {
                if (IsSnappingAllowed)
                {
                    snap = !snap;
                    MarkDirty();
                }
            }
            else if (showSelection && IsSelectionValid())
            {
                if (ctrl)
                {
                    if (e.KeyCode == Keys.C)
                        Copy();
                    else if (e.KeyCode == Keys.X)
                        Cut();
                    else if (e.KeyCode == Keys.V)
                    {
                        if (shift)
                            PasteSpecial();
                        else
                            Paste();
                    }
                }

                if (e.KeyCode == Keys.Delete)
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
                    switch (e.KeyCode)
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
                    switch (e.KeyCode)
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

        protected bool EnsureSeekBarVisible(float percent = ContinuousFollowPercent)
        {
            var seekX = GetPixelForNote(App.CurrentFrame);
            var minX = 0;
            var maxX = (int)((Width * percent) - whiteKeySizeX);

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

        private void ShowInstrumentError()
        {
            App.DisplayWarning("Selected instrument is incompatible with channel!");
        }

        public void ToggleEffectPannel()
        {
            if (editMode == EditionMode.Channel || editMode == EditionMode.DPCM)
            {
                showEffectsPanel = !showEffectsPanel;
                UpdateRenderCoords();
                ClampScroll();
                MarkDirty();
            }
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
            bool middle = e.Button.HasFlag(MouseButtons.Middle) || (e.Button.HasFlag(MouseButtons.Left) && ModifierKeys.HasFlag(Keys.Alt));

            if (middle && e.Y > headerSizeY && e.X > whiteKeySizeX)
            {
                panning = true;
                CaptureMouse(e.X, e.Y);
                return true;
            }

            return false;
        }

        private bool HandleMouseDownScrollbar(MouseEventArgs e)
        {
            if (e.Button.HasFlag(MouseButtons.Left) && scrollBarThickness > 0 && e.X > whiteKeySizeX && e.Y > headerAndEffectSizeY)
            {
                if (e.Y >= (Height - scrollBarThickness) && GetScrollBarParams(true, out var scrollBarPosX, out var scrollBarSizeX))
                {
                    var x = e.X - whiteKeySizeX;
                    if (x < scrollBarPosX)
                    {
                        scrollX -= (Width - whiteKeySizeX);
                        ClampScroll();
                        MarkDirty();
                    }
                    else if (x > (scrollBarPosX + scrollBarSizeX))
                    {
                        scrollX += (Width - whiteKeySizeX);
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
            if (e.Button.HasFlag(MouseButtons.Left) && IsPointInPiano(e.X, e.Y))
            {
                StartPlayPiano(e.X, e.Y);
                return true;
            }

            return false;
        }

        private bool HandleMouseDownSeekBar(MouseEventArgs e)
        {
            if (e.Button.HasFlag(MouseButtons.Left) && IsPointInHeader(e.X, e.Y))
            {
                StartCaptureOperation(e.X, e.Y, CaptureOperation.DragSeekBar);
                UpdateSeekDrag(e, false);
                return true;
            }

            return false;
        }

        private bool HandleMouseDownHeaderSelection(MouseEventArgs e)
        {
            if (e.Button.HasFlag(MouseButtons.Right) && IsPointInHeader(e.X, e.Y))
            {
                StartCaptureOperation(e.X, e.Y, CaptureOperation.Select, false);
                UpdateSelection(e);
                return true;
            }

            return false;
        }

        private bool HandleMouseDownEnvelopeSelection(MouseEventArgs e)
        {
            if (e.Button.HasFlag(MouseButtons.Right) && (IsPointInHeaderTopPart(e.X, e.Y) || IsPointInNoteArea(e.X, e.Y)))
            {
                StartCaptureOperation(e.X, e.Y, CaptureOperation.Select);
                UpdateSelection(e);
                return true;
            }

            return false;
        }

        private bool HandleMouseDownEffectList(MouseEventArgs e)
        {
            if (e.Button.HasFlag(MouseButtons.Left) && IsPointInEffectList(e.X, e.Y))
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
            if (e.Button.HasFlag(MouseButtons.Right) && ModifierKeys.HasFlag(Keys.Alt))
            {
                StartCaptureOperation(e.X, e.Y, CaptureOperation.AltZoom);
                return true;
            }

            return false;
        }

        private bool HandleMouseDownEnvelopeResize(MouseEventArgs e)
        {
            if (e.Button.HasFlag(MouseButtons.Left) && IsPointInHeaderTopPart(e.X, e.Y) && EditEnvelope.CanResize)
            {
                StartCaptureOperation(e.X, e.Y, CaptureOperation.ResizeEnvelope);

                if (editMode == EditionMode.Enveloppe)
                    App.UndoRedoManager.BeginTransaction(TransactionScope.Instrument, editInstrument.Id);
                else
                    App.UndoRedoManager.BeginTransaction(TransactionScope.Arpeggio, editArpeggio.Id);

                ResizeEnvelope(e);
                return true;
            }

            return false;
        }

        private bool HandleMouseDownEnvelopeLoopRelease(MouseEventArgs e)
        {
            bool left  = e.Button.HasFlag(MouseButtons.Left);
            bool right = e.Button.HasFlag(MouseButtons.Right);

            if (((left && EditEnvelope.CanLoop) || (right && EditEnvelope.CanRelease && EditEnvelope.Loop >= 0)) && IsPointInHeaderBottomPart(e.X, e.Y))
            {
                CaptureOperation op = left ? CaptureOperation.DragLoop : CaptureOperation.DragRelease;
                StartCaptureOperation(e.X, e.Y, op);

                if (editMode == EditionMode.Enveloppe)
                    App.UndoRedoManager.BeginTransaction(TransactionScope.Instrument, editInstrument.Id);
                else
                    App.UndoRedoManager.BeginTransaction(TransactionScope.Arpeggio, editArpeggio.Id);

                ResizeEnvelope(e);
                return true;
            }

            return false;
        }

        private bool HandleMouseDownDrawEnvelope(MouseEventArgs e)
        {
            if (e.Button.HasFlag(MouseButtons.Left) && IsPointInNoteArea(e.X, e.Y) && EditEnvelope.Length > 0)
            {
                StartCaptureOperation(e.X, e.Y, CaptureOperation.DrawEnvelope);

                if (editMode == EditionMode.Enveloppe)
                    App.UndoRedoManager.BeginTransaction(TransactionScope.Instrument, editInstrument.Id);
                else
                    App.UndoRedoManager.BeginTransaction(TransactionScope.Arpeggio, editArpeggio.Id);

                DrawEnvelope(e, true);
                return true;
            }

            return false;
        }

        private bool HandleMouseDownChangeEffectValue(MouseEventArgs e)
        {
            if (e.Button.HasFlag(MouseButtons.Left) && IsPointInEffectPanel(e.X, e.Y) && selectedEffectIdx >= 0)
            {
                if (GetEffectNoteForCoord(e.X, e.Y, out var location))
                {
                    var slide = FamiStudioForm.IsKeyDown(Keys.S);

                    if (slide && selectedEffectIdx == Note.EffectVolume)
                    {
                        StartDragVolumeSlide(e.X, e.Y, location);
                    }
                    else
                    {
                        StartChangeEffectValue(e, location);
                    }
                }
            }

            return false;
        }

        private bool HandleMouseDownDPCMVolumeEnvelope(MouseEventArgs e)
        {
            bool left  = e.Button.HasFlag(MouseButtons.Left);
            bool right = e.Button.HasFlag(MouseButtons.Right);

            if ((left || right) && IsPointInEffectPanel(e.X, e.Y))
            {
                var vertexIdx = GetWaveVolumeEnvelopeVertexIndex(e);
                if (vertexIdx >= 0)
                {
                    if (left)
                    {
                        volumeEnvelopeDragVertex = vertexIdx;
                        App.UndoRedoManager.BeginTransaction(TransactionScope.DPCMSample, editSample.Id);
                        StartCaptureOperation(e.X, e.Y, CaptureOperation.DragWaveVolumeEnvelope);
                    }
                    else
                    {
                        App.UndoRedoManager.BeginTransaction(TransactionScope.DPCMSample, editSample.Id);
                        editSample.VolumeEnvelope[vertexIdx].volume = 1.0f;
                        editSample.Process();
                        App.UndoRedoManager.EndTransaction();
                        MarkDirty();
                    }
                }
                return true;
            }

            return false;
        }

        private bool HandleMouseDownSnapResolutionButton(MouseEventArgs e)
        {
            bool left  = e.Button.HasFlag(MouseButtons.Left);
            bool right = e.Button.HasFlag(MouseButtons.Right);

            if ((left || right) && IsPointOnSnapResolutionButton(e.X, e.Y))
            {
                if (left)
                    snapResolution = SnapResolution.Clamp(snapResolution + 1, App.Project.UsesFamiTrackerTempo);
                else
                    snapResolution = SnapResolution.Clamp(snapResolution - 1, App.Project.UsesFamiTrackerTempo);

                MarkDirty();
                return true;
            }

            return false;
        }

        private bool HandleMouseDownSnapButton(MouseEventArgs e)
        {
            if (e.Button.HasFlag(MouseButtons.Left) && IsPointOnSnapButton(e.X, e.Y))
            {
                snap = !snap;
                MarkDirty();
                return true;
            }

            return false;
        }

        private bool HandleMouseDownMaximizeButton(MouseEventArgs e)
        {
            if (e.Button.HasFlag(MouseButtons.Left) && IsPointOnMaximizeButton(e.X, e.Y))
            {
                ToggleMaximize();
                MarkDirty();
                return true;
            }

            return false;
        }

        private bool HandleMouseDownToggleEffectPanelButton(MouseEventArgs e)
        {
            if (e.Button.HasFlag(MouseButtons.Left) && IsPointInTopLeftCorner(e.X, e.Y))
            {
                ToggleEffectPannel();
                return true;
            }

            return false;
        }

        private bool HandleMouseDownWaveSelection(MouseEventArgs e)
        {
            bool left  = e.Button.HasFlag(MouseButtons.Left);
            bool right = e.Button.HasFlag(MouseButtons.Right);

            if ((left || right) && (IsPointInNoteArea(e.X, e.Y) || IsPointInHeader(e.X, e.Y)))
            {
                StartCaptureOperation(e.X, e.Y, CaptureOperation.SelectWave);
                UpdateWaveSelection(e);
                return true;
            }

            return false;
        }

        private bool HandleMouseDownChannelNote(MouseEventArgs e)
        {
            bool left  = e.Button.HasFlag(MouseButtons.Left);
            bool right = e.Button.HasFlag(MouseButtons.Right);

            if (GetLocationForCoord(e.X, e.Y, out var mouseLocation, out byte noteValue))
            {
                if (mouseLocation.PatternIndex >= Song.Length)
                    return true;

                var channel = Song.Channels[editChannel];
                var noteLocation = mouseLocation;
                var note = channel.FindMusicalNoteAtLocation(ref noteLocation, noteValue);

                if (left)
                {
                    var shift   = ModifierKeys.HasFlag(Keys.Shift);
                    var stop    = FamiStudioForm.IsKeyDown(Keys.T);
                    var slide   = FamiStudioForm.IsKeyDown(Keys.S);
                    var attack  = FamiStudioForm.IsKeyDown(Keys.A);
                    var eyedrop = FamiStudioForm.IsKeyDown(Keys.I);

                    if (slide)
                    {
                        StartSlideNoteCreation(e, noteLocation, note, noteValue);
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
                            var captureOp = GetHoverNoteCaptureOperationForCoord(e.X, e.Y);

                            if (captureOp == CaptureOperation.DragSelection ||
                                captureOp == CaptureOperation.DragNote ||
                                captureOp == CaptureOperation.ResizeNoteStart ||
                                captureOp == CaptureOperation.ResizeSelectionNoteStart)
                            {
                                StartNoteDrag(e, captureOp, noteLocation, note);
                            }
                            else if (captureOp == CaptureOperation.ResizeNoteEnd ||
                                     captureOp == CaptureOperation.ResizeSelectionNoteEnd)
                            {
                                StartNoteResizeEnd(e, captureOp, noteLocation);
                            }
                            else if (captureOp == CaptureOperation.MoveNoteRelease)
                            {
                                StartMoveNoteRelease(e, noteLocation);
                            }
                        }
                        else
                        {
                            StartNoteCreation(e, noteLocation, noteValue);
                        }
                    }
                }
                else if (right)
                {
                    if (note != null)
                    {
                        ClickDeleteNote(noteLocation, mouseLocation, note);
                    }
                    else
                    {
                        StartSelection(e);
                    }
                }

                MarkDirty();
                return true;
            }

            return false;
        }

        private bool HandleMouseDownClearEffectValue(MouseEventArgs e)
        {
            if (e.Button.HasFlag(MouseButtons.Right) && GetEffectNoteForCoord(e.X, e.Y, out var location) && selectedEffectIdx >= 0)
            {
                var pattern = Song.Channels[editChannel].PatternInstances[location.PatternIndex];

                if (pattern != null && pattern.Notes.TryGetValue(location.NoteIndex, out var note) && note != null && note.HasValidEffectValue(selectedEffectIdx))
                {
                    ClearEffectValue(location, note);
                }
                else
                {
                    StartSelection(e);
                }

                return true;
            }

            return false;
        }

        private bool HandleMouseDownDPCMMapping(MouseEventArgs e)
        {
            bool left  = e.Button.HasFlag(MouseButtons.Left);
            bool right = e.Button.HasFlag(MouseButtons.Right);

            if ((left || right) && GetLocationForCoord(e.X, e.Y, out var location, out var noteValue))
            {
                if (App.Project.NoteSupportsDPCM(noteValue))
                {
                    var mapping = App.Project.GetDPCMMapping(noteValue);

                    if (left && mapping == null)
                    {
                        MapDPCMSample(noteValue);
                    }
                    else if (left && mapping != null)
                    {
                        StartDragDPCMSampleMapping(e, noteValue);
                    }
                    else if (right && mapping != null)
                    {
                        ClearDPCMSampleMapping(noteValue);
                    }
                }
                else
                {
                    App.DisplayWarning("DPCM samples are only allowed between C1 and D6");
                }

                return true;
            }

            return false;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            ControlActivated?.Invoke();

            bool left  = e.Button.HasFlag(MouseButtons.Left);
            bool right = e.Button.HasFlag(MouseButtons.Right);

            if (captureOperation != CaptureOperation.None && (left || right))
                return;

            UpdateCursor();

            // General stuff.
            if (HandleMouseDownPan(e)) goto Handled;
            if (HandleMouseDownScrollbar(e)) goto Handled;
            if (HandleMouseDownPiano(e)) goto Handled;
            if (HandleMouseDownAltZoom(e)) goto Handled;

            if (editMode == EditionMode.Channel)
            {
                if (HandleMouseDownSeekBar(e)) goto Handled;
                if (HandleMouseDownHeaderSelection(e)) goto Handled;
                if (HandleMouseDownEffectList(e)) goto Handled;
                if (HandleMouseDownChangeEffectValue(e)) goto Handled;
                if (HandleMouseDownClearEffectValue(e)) goto Handled;
                if (HandleMouseDownSnapResolutionButton(e)) goto Handled;
                if (HandleMouseDownSnapButton(e)) goto Handled;
                if (HandleMouseDownMaximizeButton(e)) goto Handled;
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

        Handled: // Yes, i use a goto, sue me.
            MarkDirty();
        }

        //==============================================================================================================
        //==============================================================================================================
        //==============================================================================================================
        //==============================================================================================================
        //==============================================================================================================

        private bool HandleTouchDownPan(int x, int y)
        {
            if (y > headerSizeY && x > whiteKeySizeX)
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

        private bool HandleTouchClickToggleEffectPanelButton(int x, int y)
        {
            if (IsPointInTopLeftCorner(x, y))
            {
                ToggleEffectPannel();
                return true;
            }

            return false;
        }

        protected override void OnTouchDown(int x, int y)
        {
            if (HandleTouchDownPan(x, y)) goto Handled;
            if (HandleTouchDownPiano(x, y)) goto Handled;

            return;

        Handled: // Yes, i use a goto, sue me.
            MarkDirty();
        }

        protected override void OnTouchMove(int x, int y)
        {
            // MATTT : Move to "UpdateCaptureOperation".
            if (captureOperation == CaptureOperation.PlayPiano)
                PlayPiano(x, y);

            //UpdateCaptureOperation(e, false);
            MarkDirty();
            //UpdateCaptureOperation(e, false);

            // MATTT : Move to "UpdateCaptureOperation".
            if (captureOperation == CaptureOperation.MobilePan)
            {
                DoScroll(x - mouseLastX, y - mouseLastY);
            }

            mouseLastX = x;
            mouseLastY = y;
        }

        protected override void OnTouchUp(int x, int y)
        {
            EndCaptureOperation(new MouseEventArgs(MouseButtons.None, 0, x, y, 0));
        }

        protected override void OnTouchScale(int x, int y, float scale, TouchScalePhase phase)
        {
            if (captureOperation != CaptureOperation.None)
            {
                Debug.WriteLine("Oops");
            }

            // TODO : Capture operation.
            switch (phase)
            {
                case TouchScalePhase.Begin:
                    panning = false; // MATTT This will not be used on mobile.
                    StartCaptureOperation(x, y, IsPointInPiano(x, y) ? CaptureOperation.MobileZoomVertical : CaptureOperation.MobileZoom);
                    break;
                case TouchScalePhase.Scale:
                    if (captureOperation == CaptureOperation.MobileZoomVertical)
                        ZoomVerticallyAtLocation(y, scale); // MATTT : Center is stuck at the initial position.
                    else
                        ZoomAtLocation(x, scale); // MATTT : Center is stuck at the initial position.
                    break;
                case TouchScalePhase.End:
                    EndCaptureOperation(new MouseEventArgs(MouseButtons.None, 0, x, y, 0)); // MATTT
                    break;
            }
        }

        protected override void OnTouchClick(int x, int y)
        {
            if (HandleTouchClickToggleEffectPanelButton(x, y)) goto Handled;

        Handled: // Yes, i use a goto, sue me.
            MarkDirty();
        }

        //==============================================================================================================
        //==============================================================================================================
        //==============================================================================================================
        //==============================================================================================================
        //==============================================================================================================

        public void LayoutChanged()
        {
            UpdateRenderCoords();
            ClampScroll();
            MarkDirty();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateRenderCoords();
            ClampScroll();
        }

        private void GetMinMaxScroll(out int minScrollX, out int minScrollY, out int maxScrollX, out int maxScrollY)
        {
            minScrollX = 0;
            minScrollY = 0;
            maxScrollX = 0;
            maxScrollY = editMode == EditionMode.None ? 0 : Math.Max(virtualSizeY + headerAndEffectSizeY - Height, 0);

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

        private void ClampScroll()
        {
            if (Song != null)
            {
                GetMinMaxScroll(out var minScrollX, out var minScrollY, out var maxScrollX, out var maxScrollY);

                scrollX = Utils.Clamp(scrollX, minScrollX, maxScrollX);
                scrollY = Utils.Clamp(scrollY, minScrollY, maxScrollY);
            }

            ScrollChanged?.Invoke();
        }


        private void DoScroll(int deltaX, int deltaY)
        {
            scrollX -= deltaX;
            scrollY -= deltaY;

            ClampScroll();
            MarkDirty();
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

        private void ScrollIfSelectionNearEdge(int mouseX)
        {
            if ((mouseX - whiteKeySizeX) < 0)
            {
                var scrollAmount = Utils.Clamp((whiteKeySizeX - mouseX) / (float)whiteKeySizeX, 0.0f, 1.0f);
                scrollX -= (int)(App.AverageTickRate * scrollAmount);
                ClampScroll();
            }
            else if ((Width - mouseX) < whiteKeySizeX)
            {
                var scrollAmount = Utils.Clamp((mouseX - (Width - whiteKeySizeX)) / (float)whiteKeySizeX, 0.0f, 1.0f);
                scrollX += (int)(App.AverageTickRate * scrollAmount);
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

        private void StartSelection(MouseEventArgs e)
        {
            StartCaptureOperation(e.X, e.Y, CaptureOperation.Select, false);
            UpdateSelection(e);
        }

        private void UpdateSelection(MouseEventArgs e)
        {
            ScrollIfSelectionNearEdge(e.X);

            int noteIdx = GetNoteForPixel(e.X - whiteKeySizeX);

            int minSelectionIdx = Math.Min(noteIdx, captureMouseAbsoluteIdx);
            int maxSelectionIdx = Math.Max(noteIdx, captureMouseAbsoluteIdx);
            int pad = IsSnappingEnabled ? -1 : 0;

            SetSelection(SnapNote(minSelectionIdx), SnapNote(maxSelectionIdx, true) + pad);
            MarkDirty();
        }

        private void UpdateWaveSelection(MouseEventArgs e)
        {
            ScrollIfSelectionNearEdge(e.X);

            float time = Math.Max(0.0f, GetWaveTimeForPixel(e.X - whiteKeySizeX));

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

        private void UpdateSeekDrag(MouseEventArgs e, bool final)
        {
            dragSeekPosition = GetNoteForPixel(e.X - whiteKeySizeX);
            dragSeekPosition = SnapNote(dragSeekPosition);

            if (final)
                App.SeekSong(dragSeekPosition);

            MarkDirty();
        }

        private void UpdateVolumeEnvelopeDrag(MouseEventArgs e, bool final)
        {
            var halfHeight    = effectPanelSizeY * 0.5f;
            var halfHeightPad = halfHeight - waveDisplayPaddingY;

            var time   = Utils.Clamp((int)Math.Round(GetWaveTimeForPixel(e.X - whiteKeySizeX) * editSample.SourceSampleRate), 0, editSample.SourceNumSamples - 1);
            var volume = Utils.Clamp(((e.Y - headerSizeY) - halfHeight) / -halfHeightPad + 1.0f, 0.0f, 2.0f);

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

        private void StartSlideNoteCreation(MouseEventArgs e, NoteLocation location, Note note, byte noteValue)
        {
            var channel = Song.Channels[editChannel];
            var pattern = channel.PatternInstances[location.PatternIndex];

            if (channel.SupportsSlideNotes)
            {
                if (note != null)
                {
                    App.UndoRedoManager.BeginTransaction(TransactionScope.Pattern, pattern.Id);
                    StartCaptureOperation(e.X, e.Y, CaptureOperation.DragSlideNoteTarget, false, location.ToAbsoluteNoteIndex(Song));
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

                        SnapPatternNote(location.PatternIndex, ref location.NoteIndex);

                        note = pattern.GetOrCreateNoteAt(location.NoteIndex);
                        note.Value = noteValue;
                        note.Duration = (ushort)Song.BeatLength;
                        note.Instrument = editChannel == ChannelType.Dpcm ? null : App.SelectedInstrument;

                        StartCaptureOperation(e.X, e.Y, CaptureOperation.CreateSlideNote, true);
                    }
                    else
                    {
                        ShowInstrumentError();
                        return;
                    }
                }
            }
        }

        private void UpdateSlideNoteCreation(MouseEventArgs e, bool final)
        {
            Debug.Assert(captureNoteAbsoluteIdx >= 0);

            var location = NoteLocation.FromAbsoluteNoteIndex(Song, captureNoteAbsoluteIdx);
            var channel = Song.Channels[editChannel];
            var pattern = channel.PatternInstances[location.PatternIndex];

            if (GetNoteValueForCoord(e.X, e.Y, out var noteValue))
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

        private void ClearEffectValue(NoteLocation location, Note note)
        {
            var pattern = Song.Channels[editChannel].PatternInstances[location.PatternIndex];
            App.UndoRedoManager.BeginTransaction(TransactionScope.Pattern, pattern.Id);
            note.ClearEffectValue(selectedEffectIdx);
            MarkPatternDirty(location.PatternIndex);
            App.UndoRedoManager.EndTransaction();
        }

        private void ToggleNoteAttack(NoteLocation location, Note note)
        {
            if (note.IsMusical)
            {
                var pattern = Song.Channels[editChannel].PatternInstances[location.PatternIndex];
                App.UndoRedoManager.BeginTransaction(TransactionScope.Pattern, pattern.Id);
                pattern.Notes[location.NoteIndex].HasAttack ^= true;
                MarkPatternDirty(location.PatternIndex);
                App.UndoRedoManager.EndTransaction();
            }
        }

        private void MapDPCMSample(byte noteValue)
        {
#if !FAMISTUDIO_ANDROID
            if (App.Project.Samples.Count == 0)
            {
                PlatformUtils.MessageBox("Before assigning a sample to a piano key, load at least one sample in the 'DPCM Samples' section of the project explorer", "No DPCM sample found", MessageBoxButtons.OK);
            }
            else
            {
                var sampleNames = new List<string>();
                foreach (var sample in App.Project.Samples)
                    sampleNames.Add(sample.Name);

                var dlg = new PropertyDialog(300);
                dlg.Properties.AddLabel(null, "Select sample to assign:"); // 0
                dlg.Properties.AddDropDownList(null, sampleNames.ToArray(), sampleNames[0]); // 1
                dlg.Properties.Build();

                if (dlg.ShowDialog(ParentForm) == DialogResult.OK)
                {
                    App.UndoRedoManager.BeginTransaction(TransactionScope.DPCMSamplesMapping, TransactionFlags.StopAudio);
                    var sampleName = dlg.Properties.GetPropertyValue<string>(1);
                    App.Project.MapDPCMSample(noteValue, App.Project.GetSample(sampleName));
                    App.UndoRedoManager.EndTransaction();
                    DPCMSampleMapped?.Invoke(noteValue);
                }
            }
#endif
        }

        private void StartDragDPCMSampleMapping(MouseEventArgs e, byte noteValue)
        {
            StartCaptureOperation(e.X, e.Y, CaptureOperation.DragSample);
            draggedSample = null;
        }

        private void ClearDPCMSampleMapping(byte noteValue)
        {
            App.UndoRedoManager.BeginTransaction(TransactionScope.DPCMSamplesMapping, TransactionFlags.StopAudio);
            App.Project.UnmapDPCMSample(noteValue);
            App.UndoRedoManager.EndTransaction();
            DPCMSampleUnmapped?.Invoke(noteValue);
        }

        private void ClickDeleteNote(NoteLocation noteLocation, NoteLocation mouseLocation, Note note)
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
            var pattern = Song.Channels[editChannel].PatternInstances[location.PatternIndex];
            App.UndoRedoManager.BeginTransaction(TransactionScope.Pattern, pattern.Id);
            var note = pattern.GetOrCreateNoteAt(location.NoteIndex);
            note.Clear();
            note.Value = Note.NoteStop;
            note.Duration = 1;
            MarkPatternDirty(location.PatternIndex);
            App.UndoRedoManager.EndTransaction();
        }

        private void Eyedrop(Note note)
        {
            App.UndoRedoManager.BeginTransaction(TransactionScope.Application);
            NoteEyedropped?.Invoke(note);
            App.UndoRedoManager.EndTransaction();
        }

        public void ReplaceSelectionInstrument(Instrument instrument, Point pos)
        {
            if (editMode == EditionMode.Channel && editChannel != ChannelType.Dpcm)
            {
                var channel = Song.Channels[editChannel];

                if (channel.SupportsInstrument(instrument))
                {
                    GetLocationForCoord(pos.X, pos.Y, out var location, out var noteValue);

                    // If dragging inside the selection, replace that.
                    if (IsSelectionValid() && IsNoteSelected(location))
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
                            App.UndoRedoManager.BeginTransaction(TransactionScope.Pattern, channel.PatternInstances[location.PatternIndex].Id);
                            note.Instrument = instrument;
                            App.UndoRedoManager.EndTransaction();
                        }
                    }
                }
                else
                {
                    ShowInstrumentError();
                }
            }
        }

        public void ReplaceSelectionArpeggio(Arpeggio arpeggio, Point pos)
        {
            if (editMode == EditionMode.Channel)
            {
                var channel = Song.Channels[editChannel];

                if (channel.SupportsArpeggios)
                {
                    GetLocationForCoord(pos.X, pos.Y, out var location, out var noteValue);

                    // If dragging inside the selection, replace that.
                    if (IsSelectionValid() && IsNoteSelected(location))
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

        private int GetWaveVolumeEnvelopeVertexIndex(MouseEventArgs e)
        {
            Debug.Assert(editMode == EditionMode.DPCM);
            Debug.Assert(vertexOrder.Length == editSample.VolumeEnvelope.Length);

            var halfHeight    = effectPanelSizeY * 0.5f;
            var halfHeightPad = halfHeight - waveDisplayPaddingY;

            var x = e.X - whiteKeySizeX;
            var y = e.Y - headerSizeY;

            for (int i = 0; i < 4; i++)
            {
                var idx = vertexOrder[i];

                var vx = GetPixelForWaveTime(editSample.VolumeEnvelope[idx].sample / editSample.SourceSampleRate, scrollX);
                var vy = (int)Math.Round(halfHeight - (editSample.VolumeEnvelope[idx].volume - 1.0f) * halfHeightPad);

                var dx = Math.Abs(vx - x);
                var dy = Math.Abs(vy - y);

                if (dx < 10 * MainWindowScaling &&
                    dy < 10 * MainWindowScaling)
                {
                    return idx;
                }
            }

            return -1;
        }

        private bool IsPointInHeader(int x, int y)
        {
            return x > whiteKeySizeX && y < headerSizeY;
        }

        private bool IsPointInHeaderTopPart(int x, int y)
        {
            return (editMode == EditionMode.Enveloppe || editMode == EditionMode.Arpeggio) && x > whiteKeySizeX && y > 0 && y < headerSizeY / 2;
        }

        private bool IsPointInHeaderBottomPart(int x, int y)
        {
            return (editMode == EditionMode.Enveloppe || editMode == EditionMode.Arpeggio) && x > whiteKeySizeX && y >= headerSizeY / 2 && y < headerSizeY;
        }

        private bool IsPointInPiano(int x, int y)
        {
            return x < whiteKeySizeX && y > headerAndEffectSizeY;
        }

        private bool IsPointInEffectList(int x, int y)
        {
            return showEffectsPanel && editMode == EditionMode.Channel && x < whiteKeySizeX && y > headerSizeY && y < headerAndEffectSizeY;
        }

        private bool IsPointInEffectPanel(int x, int y)
        {
            return showEffectsPanel && (editMode == EditionMode.Channel || editMode == EditionMode.DPCM) && x > whiteKeySizeX && y > headerSizeY && y < headerAndEffectSizeY;
        }

        private bool IsPointOnSnapResolutionButton(int x, int y)
        {
            var snapButtonSize = (int)bmpMiscAtlas.GetElementSize((int)MiscImageIndices.Snap).Width;
            var posX = whiteKeySizeX - (snapButtonSize + headerIconsPosX) * 3 - 1;
            return IsSnappingAllowed &&
                x > posX && x < posX + snapButtonSize &&
                y > headerIconsPosY && y < headerIconsPosY + snapButtonSize;
        }

        private bool IsPointOnSnapButton(int x, int y)
        {
            var snapButtonSize = (int)bmpMiscAtlas.GetElementSize((int)MiscImageIndices.Snap).Width;
            var posX = whiteKeySizeX - (snapButtonSize + headerIconsPosX) * 2 - 1;
            return IsSnappingAllowed &&
                x > posX && x < posX + snapButtonSize &&
                y > headerIconsPosY && y < headerIconsPosY + snapButtonSize;
        }

        private bool IsPointOnMaximizeButton(int x, int y)
        {
            var snapButtonSize = (int)bmpMiscAtlas.GetElementSize((int)MiscImageIndices.Snap).Width;
            var posX = whiteKeySizeX - (snapButtonSize + headerIconsPosX) * 1 - 1;
            return 
                x > posX && x < posX + snapButtonSize &&
                y > headerIconsPosY && y < headerIconsPosY + snapButtonSize;
        }

        private bool IsPointInNoteArea(int x, int y)
        {
            return y > headerSizeY && x > whiteKeySizeX;
        }

        private bool IsPointInTopLeftCorner(int x, int y)
        {
            return (editMode == EditionMode.Channel || editMode == EditionMode.DPCM) && y < headerSizeY && x < whiteKeySizeX;
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
                tooltip = "{MouseRight} Select - {MouseLeft} Resize envelope";
            }
            else if (IsPointInHeaderBottomPart(e.X, e.Y) && (editMode == EditionMode.Enveloppe || editMode == EditionMode.Arpeggio))
            {
                tooltip = "{MouseLeft} Set loop point - {MouseRight} Set release point (volume only, must have loop point)";
            }
            else if (IsPointInPiano(e.X, e.Y))
            {
                tooltip = "{MouseLeft} Play piano - {MouseWheel} Pan\n{MouseLeft} {MouseLeft} Configure scales";
            }
            else if (IsPointOnSnapResolutionButton(e.X, e.Y))
            {
                tooltip = "{MouseLeft} Next snap precision {MouseRight} Previous snap precision {MouseWheel} Change snap precision";
            }
            else if (IsPointOnSnapButton(e.X, e.Y))
            {
                tooltip = "{MouseLeft} Toggle snapping {Shift} {S} {MouseWheel} Change snap precision";
            }
            else if (IsPointOnMaximizeButton(e.X, e.Y))
            {
                tooltip = "{MouseLeft} Maximize/Minimize piano roll {~}";
            }
            else if (IsPointInTopLeftCorner(e.X, e.Y))
            {
                tooltip = "{MouseLeft} Show/hide effect panel {Ctrl} {~}";
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
                    var vertexIdx = GetWaveVolumeEnvelopeVertexIndex(e);

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
                        var captureOp = GetHoverNoteCaptureOperationForCoord(e.X, e.Y);
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

        private int SnapNote(int absoluteNoteIndex, bool roundUp = false, bool forceSnap = false)
        {
            if (IsSnappingEnabled || forceSnap)
            {
                var location = NoteLocation.FromAbsoluteNoteIndex(Song, absoluteNoteIndex);
                var noteLength = Song.Project.UsesFamiTrackerTempo ? 1 : Song.GetPatternNoteLength(location.PatternIndex);
                var snapFactor = SnapResolution.Factors[snapResolution];

                Debug.Assert(snapFactor >= 1.0); // Fractional snapping is no longer supported.

                var snappedNoteIndex = location.NoteIndex;
                if (snapFactor >= 1.0)
                {
	                var numNotes = noteLength * (int)snapFactor;
	                snappedNoteIndex = (location.NoteIndex / numNotes + (roundUp ? 1 : 0)) * numNotes;
          		}
                else
                {
                    // Subtract the base note so that snapping inside a note is always deterministic. 
                    // Otherwise, rounding errors can create a different snapping pattern every note (6-5-5-6 like Gimmick).
                    var baseNodeIdx  = location.NoteIndex / noteLength * noteLength;
                    var noteFrameIdx = location.NoteIndex % noteLength;
                    var numSnapPoints = (int)Math.Round(1.0 / snapFactor);

                    // This is terrible...
                    if (roundUp)
                    {
                        for (int i = 0; i <= numSnapPoints; i++)
                        {
                            var snapPoint = (int)Math.Round(i / (double)numSnapPoints * noteLength);
                            if (noteFrameIdx < snapPoint)
                            {
                                snappedNoteIndex = baseNodeIdx + snapPoint;
                                break;
                            }
                        }
                    }
                    else
                    {
                        for (int i = numSnapPoints - 1; i >= 0; i--)
                        {
                            var snapPoint = (int)Math.Round(i / (double)numSnapPoints * noteLength);
                            if (noteFrameIdx >= snapPoint)
                            {
                                snappedNoteIndex = baseNodeIdx + snapPoint;
                                break;
                            }
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

        private void SnapPatternNote(int patternIdx, ref int noteIdx)
        {
            if (IsSnappingEnabled)
            {
                var noteLength = Song.GetPatternNoteLength(patternIdx);
                noteIdx = (noteIdx / noteLength) * noteLength;
            }
        }

        private void StartNoteCreation(MouseEventArgs e, NoteLocation location, byte noteValue)
        {
            if (Song.Channels[editChannel].SupportsInstrument(App.SelectedInstrument))
            {
                App.PlayInstrumentNote(noteValue, false, false);
                StartCaptureOperation(e.X, e.Y, CaptureOperation.CreateNote, true);
                UpdateNoteCreation(e, true, false);
            }
            else
            {
                ShowInstrumentError();
            }
        }

        private void UpdateNoteCreation(MouseEventArgs e, bool first, bool last)
        {
            ScrollIfSelectionNearEdge(e.X);
            GetLocationForCoord(e.X, e.Y, out var location, out var noteValue, true);

            if (!first)
            {
                // Need to cancel the transaction every time since the start pattern may change.
                App.UndoRedoManager.RestoreTransaction(false);
                App.UndoRedoManager.AbortTransaction();
            }

            var minLocation = NoteLocation.Min(location, captureNoteLocation);
            var maxLocation = NoteLocation.Max(location, captureNoteLocation);
            var minAbsoluteNoteIndex = minLocation.ToAbsoluteNoteIndex(Song);
            var maxAbsoluteNoteIndex = maxLocation.ToAbsoluteNoteIndex(Song) + 1;

            hoverNoteLocation = minLocation;

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

        private void StartNoteDrag(MouseEventArgs e, CaptureOperation captureOp, NoteLocation location, Note note)
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

            StartCaptureOperation(e.X, e.Y, captureOp, true, location.ToAbsoluteNoteIndex(Song));

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

            UpdateNoteDrag(e, false);
        }

        private void UpdateNoteDrag(MouseEventArgs e, bool final, bool createNote = false)
        {
            Debug.Assert(
                App.UndoRedoManager.HasTransactionInProgress && (
                    App.UndoRedoManager.UndoScope == TransactionScope.Pattern ||
                    App.UndoRedoManager.UndoScope == TransactionScope.Channel));

            var channel = Song.Channels[editChannel];

            App.UndoRedoManager.RestoreTransaction(false);

            ScrollIfSelectionNearEdge(e.X);
            GetLocationForCoord(e.X, e.Y, out var location, out var noteValue, true);

            var resizeStart = captureOperation == CaptureOperation.ResizeNoteStart || captureOperation == CaptureOperation.ResizeSelectionNoteStart;
            var deltaNoteIdx = location.ToAbsoluteNoteIndex(Song) - captureMouseAbsoluteIdx;
            var deltaDuration = resizeStart ? -deltaNoteIdx : 0;
            var deltaNoteValue = resizeStart ? 0 : noteValue - captureNoteValue;
            var newDragFrameMin = dragFrameMin + deltaNoteIdx;
            var newDragFrameMax = dragFrameMax + deltaNoteIdx;

            hoverNoteLocation = captureNoteLocation.Advance(Song, deltaNoteIdx);

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

            var copy = ModifierKeys.HasFlag(Keys.Control);
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
                        newNote.Value    = Note.NoteStop;
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

            if (createNote && deltaNoteIdx == 0 && deltaNoteValue == 0)
            {
                Debug.Assert(dragNotes.Count == 1);
                Debug.Assert(dragFrameMin == dragFrameMax);

                // If there is a note between the snapped position and where we clicked, use that one.
                if (dragFrameMin > location.ToAbsoluteNoteIndex(Song))
                    location = NoteLocation.FromAbsoluteNoteIndex(Song, dragFrameMin);

                var pattern = channel.PatternInstances[location.PatternIndex];

                if (channel.SupportsInstrument(App.SelectedInstrument))
                {
                    var dragNoteLocation = NoteLocation.FromAbsoluteNoteIndex(Song, dragFrameMin);

                    if (pattern == null || dragNoteLocation.PatternIndex != location.PatternIndex)
                    {
                        // Check if need to promote transaction.
                        if (App.UndoRedoManager.UndoScope == TransactionScope.Pattern)
                            PromoteTransaction(TransactionScope.Channel, Song.Id, editChannel);

                        if (pattern == null)
                        {
                            pattern = channel.CreatePatternAndInstance(location.PatternIndex);
                        }
                    }

                    var dragNote = channel.PatternInstances[dragNoteLocation.PatternIndex].Notes[dragNoteLocation.NoteIndex];
                    var dragNoteNextDistance = channel.GetDistanceToNextNote(dragNoteLocation);
                    var dragNoteOldDuration = dragNoteNextDistance > 0 ? Math.Min(dragNote.Duration, dragNoteNextDistance) : dragNote.Duration;

                    // Shorten the drag notes.
                    dragNote.Duration = (ushort)Song.CountNotesBetween(dragNoteLocation, location);

                    var note = pattern.GetOrCreateNoteAt(location.NoteIndex);

                    note.Value = noteValue;
                    note.Instrument = editChannel == ChannelType.Dpcm ? null : App.SelectedInstrument;
                    note.Arpeggio = channel.SupportsArpeggios ? App.SelectedArpeggio : null;
                    note.Duration = (ushort)(dragNoteOldDuration - dragNote.Duration);

                    channel.InvalidateCumulativePatternCache(pattern);
                }
                else
                {
                    ShowInstrumentError();
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
            }

            if (final)
            {
                App.Project.ValidateIntegrity();
                App.UndoRedoManager.EndTransaction();
                App.StopInstrument();
            }

            MarkDirty();
        }

        private void StartNoteResizeEnd(MouseEventArgs e, CaptureOperation captureOp, NoteLocation location)
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

            StartCaptureOperation(e.X, e.Y, captureOp, true, location.ToAbsoluteNoteIndex(Song));
        }

        private void UpdateNoteResizeEnd(MouseEventArgs e, bool final)
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

            ScrollIfSelectionNearEdge(e.X);
            GetLocationForCoord(e.X, e.Y, out var location, out var noteValue, true);

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

        private void StartMoveNoteRelease(MouseEventArgs e, NoteLocation location)
        {
            var pattern = Song.Channels[editChannel].PatternInstances[location.PatternIndex];
            App.UndoRedoManager.BeginTransaction(TransactionScope.Pattern, pattern.Id);
            StartCaptureOperation(e.X, e.Y, CaptureOperation.MoveNoteRelease, false, location.ToAbsoluteNoteIndex(Song));
        }

        private void UpdateMoveNoteRelease(MouseEventArgs e)
        {
            GetLocationForCoord(e.X, e.Y, out var location, out var noteValue, false);

            var channel = Song.Channels[editChannel];
            var pattern = channel.PatternInstances[captureNoteLocation.PatternIndex];
            var note = pattern.Notes[captureNoteLocation.NoteIndex];
            note.Release = (ushort)Utils.Clamp(Song.CountNotesBetween(captureNoteLocation, location), 1, note.Duration - 1);
            channel.InvalidateCumulativePatternCache(pattern);
            MarkDirty();
        }

        private void UpdateAltZoom(MouseEventArgs e)
        {
            var deltaY = e.Y - captureMouseY;

            if (Math.Abs(deltaY) > 50)
            {
                ZoomAtLocation(e.X, deltaY < 0.0f ? 2.0f : 0.5f);
                captureMouseY = e.Y;
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

        private CaptureOperation GetHoverEffectCaptureOperationForCoord(int x, int y)
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

        private CaptureOperation GetHoverNoteCaptureOperationForCoord(int x, int y)
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

                    x -= whiteKeySizeX;

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

            if ((editMode == EditionMode.Enveloppe || editMode == EditionMode.Arpeggio) && EditEnvelope.CanResize && (pt.X > whiteKeySizeX && pt.Y < headerSizeY && captureOperation != CaptureOperation.Select) || captureOperation == CaptureOperation.ResizeEnvelope)
            {
                Cursor.Current = Cursors.SizeWE;
            }
            else if (captureOperation == CaptureOperation.ChangeEffectValue)
            {
                Cursor.Current = Cursors.SizeNS;
            }
            else if (ModifierKeys.HasFlag(Keys.Control) && (captureOperation == CaptureOperation.DragNote || captureOperation == CaptureOperation.DragSelection))
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
                        var captureOp = GetHoverEffectCaptureOperationForCoord(pt.X, pt.Y);

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
                        var captureOp = GetHoverNoteCaptureOperationForCoord(pt.X, pt.Y);

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
            base.OnMouseMove(e);

            bool left   = e.Button.HasFlag(MouseButtons.Left);
            bool middle = e.Button.HasFlag(MouseButtons.Middle) || (e.Button.HasFlag(MouseButtons.Left) && ModifierKeys.HasFlag(Keys.Alt));

            UpdateCursor();
            UpdateCaptureOperation(e, false);

            if (middle)
            {
                DoScroll(e.X - mouseLastX, e.Y - mouseLastY);
            }

            UpdateToolTip(e);
            MarkDirty();

            mouseLastX = e.X;
            mouseLastY = e.Y;
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            UpdateCursor();

            bool middle = e.Button.HasFlag(MouseButtons.Middle);

            if (middle)
                panning = false;
            else
                EndCaptureOperation(e);
        }

        private void ZoomAtLocation(int x, float scale)
        {
            if (scale == 1.0f)
                return;

            // When continuously following, zoom at the seek bar location.
            if (continuouslyFollowing)
                x = (int)(Width * ContinuousFollowPercent);

            Debug.Assert(PlatformUtils.IsMobile || scale == 0.5f || scale == 2.0f);

            var pixelX = x - whiteKeySizeX;
            var absoluteX = pixelX + scrollX;
            var prevNoteSizeX = noteSizeX;

            zoom *= scale;
            zoom = Utils.Clamp(zoom, minZoom, maxZoom);

            Debug.Assert(PlatformUtils.IsMobile || Utils.Frac(Math.Log(zoom, 2.0)) == 0.0);

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

            Debug.Assert(PlatformUtils.IsMobile);

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

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);

            if (e.X > whiteKeySizeX)
            {
                if (Settings.TrackPadControls && !ModifierKeys.HasFlag(Keys.Control))
                {
                    if (ModifierKeys.HasFlag(Keys.Shift))
                        scrollX -= e.Delta;
                    else
                        scrollY -= e.Delta;

                    ClampScroll();
                    MarkDirty();
                }
                else if (editMode != EditionMode.DPCMMapping)
                {
                    ZoomAtLocation(e.X, e.Delta < 0.0f ? 0.5f : 2.0f);
                }
            }
            else if (IsPointOnSnapResolutionButton(e.X, e.Y) || IsPointOnSnapButton(e.X, e.Y))
            {
                if (e.Delta > 0)
                    snapResolution = SnapResolution.Clamp(snapResolution + 1, App.Project.UsesFamiTrackerTempo);
                else
                    snapResolution = SnapResolution.Clamp(snapResolution - 1, App.Project.UsesFamiTrackerTempo);

                MarkDirty();
            }
        }

        protected override void OnMouseHorizontalWheel(MouseEventArgs e)
        {
            scrollX += e.Delta;
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
                    var maxX = Width - whiteKeySizeX;
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

        public void Tick(float delta)
        {
            if (App == null)
                return;

            var pt = this.PointToClient(Cursor.Position);
            var e = new MouseEventArgs(MouseButtons.None, 1, pt.X, pt.Y, 0);

            UpdateCaptureOperation(e, true);
            UpdateFollowMode();
        }

        private bool GetEffectNoteForCoord(int x, int y, out NoteLocation location)
        {
            if (x > whiteKeySizeX && y > headerSizeY && y < headerAndEffectSizeY)
            {
                var absoluteNoteIndex = GetNoteForPixel(x - whiteKeySizeX);
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
            return x > whiteKeySizeX && ((y > headerAndEffectSizeY && !captureInProgress) || (rawNoteValue >= 0 && captureInProgress));
        }

        private bool GetLocationForCoord(int x, int y, out NoteLocation location, out byte noteValue, bool allowSnap = false)
        {
            var absoluteNoteIndex = Utils.Clamp(GetNoteForPixel(x - whiteKeySizeX), 0, Song.GetPatternStartAbsoluteNoteIndex(Song.Length) - 1);

            if (allowSnap)
                absoluteNoteIndex = SnapNote(absoluteNoteIndex);

            location = Song.AbsoluteNoteIndexToNoteLocation(absoluteNoteIndex);
            noteValue = (byte)(NumNotes - Utils.Clamp((y + scrollY - headerAndEffectSizeY) / noteSizeY, 0, NumNotes));

            return (x > whiteKeySizeX && y > headerAndEffectSizeY && location.PatternIndex < Song.Length);
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
            var maxValue = 64 / envelopeValueZoom - 1; 

            idx = GetNoteForPixel(x - whiteKeySizeX);
            value = (sbyte)(maxValue - (int)Math.Min((y + scrollY - headerAndEffectSizeY - 1) / envelopeSizeY, 128)); 

            return x > whiteKeySizeX;
        }

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

            if (buffer.IsReading)
            {
                BuildSupportEffectList();
                UpdateRenderCoords();
                ClampScroll();
                ClampMinSnap();
                MarkDirty();

                captureOperation = CaptureOperation.None;
                Capture = false;
                panning = false;
            }
        }
    }

    public class SnapResolution
    {
        public const int OneQuarter = 0;
        public const int OneThird   = 1;
        public const int OneHalf    = 2;
        public const int OneNote    = 3;
        public const int TwoNote    = 4;
        public const int ThreeNote  = 5;
        public const int FourNote   = 6;
        public const int Max        = 7;

        public static readonly double[] Factors = new[]
        {
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
            "1/4",
            "1/3",
            "1/2",
            "1",
            "2",
            "3",
            "4"
        };

        public static readonly string[] ImageNames = new string[]
        {
            "Snap1_4",
            "Snap1_3",
            "Snap1_2",
            "Snap1",
            "Snap2",
            "Snap3",
            "Snap4"
        };

        public static int GetMinSnapValue(bool famitrackerTempo)
        {
            return famitrackerTempo ? OneNote : OneQuarter;
        }

        public static int GetMaxSnapValue()
        {
            return FourNote;
        }

        public static int Clamp(int value, bool famitrackerTempo)
        {
            return Utils.Clamp(value, famitrackerTempo ? OneNote : OneQuarter, FourNote);
        }
    }
}
