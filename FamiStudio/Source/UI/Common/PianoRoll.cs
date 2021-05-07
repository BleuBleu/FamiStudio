using System;
using System.Diagnostics;
using System.IO;
using System.Media;
using System.Drawing;
using System.Windows.Forms;
using FamiStudio.Properties;
using System.Collections.Generic;
using System.Runtime.InteropServices;

#if FAMISTUDIO_WINDOWS
    using RenderBitmap   = SharpDX.Direct2D1.Bitmap;
    using RenderBrush    = SharpDX.Direct2D1.Brush;
    using RenderGeometry = SharpDX.Direct2D1.PathGeometry;
    using RenderControl  = FamiStudio.Direct2DControl;
    using RenderGraphics = FamiStudio.Direct2DGraphics;
    using RenderTheme    = FamiStudio.Direct2DTheme;
#else
    using RenderBitmap   = FamiStudio.GLBitmap;
    using RenderBrush    = FamiStudio.GLBrush;
    using RenderGeometry = FamiStudio.GLGeometry;
    using RenderControl  = FamiStudio.GLControl;
    using RenderGraphics = FamiStudio.GLGraphics;
    using RenderTheme    = FamiStudio.GLTheme;
#endif

namespace FamiStudio
{
    public class PianoRoll : RenderControl
    {
        const int MinZoomLevel = -3;
        const int MaxZoomLevel =  4;
        const int MaxWaveZoomLevel = 8;
        const int DefaultEnvelopeZoomLevel = 2;
        const int DrawFrameZoomLevel = -1;
        const float ContinuousFollowPercent = 0.75f;
        const float DefaultZoomWaveTime = 0.25f;

        const int DefaultNumOctaves = 8;
        const int DefaultHeaderSizeY = 17;
        const int DefaultDPCMHeaderSizeY = 17;
        const int DefaultEffectPanelSizeY = 176;
        const int DefaultEffectButtonSizeY = 18;
        const int DefaultNoteSizeX = 16;
        const int DefaultNoteSizeY = 12;
        const int DefaultNoteAttackSizeX = 3;
        const int DefaultReleaseNoteSizeY = 8;
        const int DefaultEnvelopeSizeY = 9;
        const int DefaultEnvelopeMax = 127;
        const int DefaultWhiteKeySizeX = 94;
        const int DefaultWhiteKeySizeY = 20;
        const int DefaultBlackKeySizeX = 56;
        const int DefaultBlackKeySizeY = 14;
        const int DefaultEffectExpandIconPosX = 4;
        const int DefaultEffectExpandIconPosY = 3;
        const int DefaultSnapIconPosX = 3;
        const int DefaultSnapIconPosY = 3;
        const int DefaultEffectIconPosX = 2;
        const int DefaultEffectIconPosY = 2;
        const int DefaultEffectNamePosX = 17;
        const int DefaultEffectNamePosY = 2;
        const int DefaultEffectIconSizeX = 12;
        const int DefaultEffectValuePosTextOffsetY = 12;
        const int DefaultEffectValueNegTextOffsetY = 3;
        const int DefaultBigTextPosX = 10;
        const int DefaultBigTextPosY = 10;
        const int DefaultTooltipTextPosX = 10;
        const int DefaultTooltipTextPosY = 30;
        const int DefaultDPCMTextPosX = 2;
        const int DefaultDPCMTextPosY = 0;
        const int DefaultDPCMSourceDataPosX = 10;
        const int DefaultDPCMSourceDataPosY = 38;
        const int DefaultDPCMInfoSpacingY = 16;
        const int DefaultOctaveNameOffsetY = 11;
        const int DefaultRecordingKeyOffsetY = 12;
        const int DefaultAttackIconPosX = 2;
        const int DefaultNoteTextPosY = 1;
        const int DefaultMinNoteSizeForText = 24;
        const int DefaultWaveGeometrySampleSize = 2;
        const int DefaultWaveDisplayPaddingY = 8;
        const int DefaultScrollBarThickness = 10;
        const int DefaultMinScrollBarLength = 128;
        const int DefaultScrollMargin = 128;
        const int DefaultNoteResizeMargin = 8;

        int numNotes;
        int numOctaves;
        int headerSizeY;
        int headerAndEffectSizeY;
        int effectPanelSizeY;
        int effectButtonSizeY;
        int noteSizeX;
        int noteSizeY;
        int noteAttackSizeX;
        int releaseNoteSizeY;
        int envelopeMax;
        int whiteKeySizeY;
        int whiteKeySizeX;
        int recordKeyPosX;
        int blackKeySizeY;
        int blackKeySizeX;
        int effectIconPosX;
        int effectIconPosY;
        int snapIconPosX;
        int snapIconPosY;
        int effectNamePosX;
        int effectNamePosY;
        int effectIconSizeX;
        int effectValuePosTextOffsetY;
        int effectValueNegTextOffsetY;
        int bigTextPosX;
        int bigTextPosY;
        int tooltipTextPosX;
        int tooltipTextPosY;
        int dpcmTextPosX;
        int dpcmTextPosY;
        int dpcmSourceDataPosX;
        int dpcmSourceDataPosY;
        int dpcmInfoSpacingY;
        int octaveNameOffsetY;
        int recordingKeyOffsetY;
        int octaveSizeY;
        int virtualSizeY;
        int scrollBarThickness;
        int minScrollBarLength;
        int barSizeX;
        int attackIconPosX;
        int noteTextPosY;
        int minNoteSizeForText;
        int waveGeometrySampleSize;
        int waveDisplayPaddingY;
        int minZoomLevel;
        int maxZoomLevel;
        int scrollMargin;
        int noteResizeMargin;
        float envelopeSizeY;

        int ScaleForZoom(int value)
        {
            return zoomLevel < 0 ? value / (1 << (-zoomLevel)) : value * (1 << zoomLevel);
        }
        
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

        enum SnapResolution
        {
            OneNote,
            TwoNote,
            ThreeNote,
            FourNote,
            Max
        };

        readonly double[] SnapResolutionFactors = new[]
        {
            1.0,
            2.0,
            3.0,
            4.0
        };

        RenderTheme theme;
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
        RenderBrush dashedVerticalLineBrush;
        RenderBrush dashedHorizontalLineBrush;
        RenderBrush invalidDpcmMappingBrush;
        RenderBitmap bmpLoop;
        RenderBitmap bmpRelease;
        RenderBitmap bmpEffectExpanded;
        RenderBitmap bmpEffectCollapsed;
        RenderBitmap bmpSlide;
        RenderBitmap bmpSnap;
        RenderBitmap bmpSnapRed;
        RenderBitmap[] bmpSnapResolution = new RenderBitmap[(int)SnapResolution.Max];
        RenderBitmap[] bmpEffects = new RenderBitmap[Note.EffectCount];
        RenderGeometry[,] stopNoteGeometry = new RenderGeometry[MaxZoomLevel - MinZoomLevel + 1, 2];
        RenderGeometry[,] stopReleaseNoteGeometry = new RenderGeometry[MaxZoomLevel - MinZoomLevel + 1, 2];
        RenderGeometry[,] releaseNoteGeometry = new RenderGeometry[MaxZoomLevel - MinZoomLevel + 1, 2];
        RenderGeometry[] slideNoteGeometry = new RenderGeometry[MaxZoomLevel - MinZoomLevel + 1];
        RenderGeometry seekGeometry;
        RenderGeometry sampleGeometry;

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
            MoveNoteRelease
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
            true,  // DragNote
            false, // DragSelection
            false, // AltZoom
            false, // DragSample
            false, // DragSeekBar
            false, // DragWaveVolumeEnvelope
            false, // ScrollBarX
            false, // ScrollBarY
            false, // ResizeNoteStart 
            false, // ResizeSelectionNoteStart
            false, // ResizeNoteEnd
            false, // ResizeSelectionNoteEnd
            false, // MoveNoteRelease
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
        };

        NoteLocation hoverNoteLocation = NoteLocation.Invalid;
        NoteLocation captureNoteLocation;
        NoteLocation captureMouseLocation;
        int captureNoteAbsoluteIdx = 0;
        int captureMouseAbsoluteIdx = 0;
        int captureNoteValue = 0;
        int captureEffectValue = 0;
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

        bool showSelection = false;
        bool showEffectsPanel = false;
        bool snap = false;
        SnapResolution snapResolution = SnapResolution.OneNote;
        int scrollX = 0;
        int scrollY = 0;
        int zoomLevel = 0;
        int selectedEffectIdx = 0;
        float videoScaleX = 1.0f;
        float videoScaleY = 1.0f;
        string noteTooltip = "";

        EditionMode editMode = EditionMode.None;

        // Pattern edit mode.
        int editChannel = -1;
        Instrument currentInstrument = null;
        Arpeggio currentArpeggio = null;

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

        public bool IsEditingInstrument        => editMode == EditionMode.Enveloppe; 
        public bool IsEditingArpeggio          => editMode == EditionMode.Arpeggio;
        public bool IsEditingDPCMSample        => editMode == EditionMode.DPCM;
        public bool IsEditingDPCMSampleMapping => editMode == EditionMode.DPCMMapping;
        
        public bool CanCopy  => showSelection && IsSelectionValid() && (editMode == EditionMode.Channel || editMode == EditionMode.Enveloppe || editMode == EditionMode.Arpeggio);
        public bool CanPaste => showSelection && IsSelectionValid() && (editMode == EditionMode.Channel && ClipboardUtils.ConstainsNotes || (editMode == EditionMode.Enveloppe || editMode == EditionMode.Arpeggio) && ClipboardUtils.ConstainsEnvelope);

        public Instrument CurrentInstrument { get => currentInstrument; set => currentInstrument = value; }
        public Arpeggio   CurrentArpeggio { get => currentArpeggio; set => currentArpeggio = value; }
        public DPCMSample CurrentEditSample { get => editSample; }

        public delegate void EmptyDelegate();
        public delegate void PatternDelegate(Pattern pattern);
        public delegate void NoteDelegate(Note note);
        public delegate void DPCMMappingDelegate(int note);

        public event PatternDelegate     PatternChanged;
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

        private void UpdateRenderCoords(float overrideScale = -1.0f)
        {
            var scaling = overrideScale > 0.0f ? overrideScale : RenderTheme.MainWindowScaling;

            minZoomLevel = MinZoomLevel;
            maxZoomLevel = editMode == EditionMode.DPCM ? MaxWaveZoomLevel : MaxZoomLevel;
            zoomLevel = Utils.Clamp(zoomLevel, minZoomLevel, maxZoomLevel);
            numOctaves = DefaultNumOctaves;
            headerSizeY = (int)((editMode == EditionMode.DPCMMapping || editMode == EditionMode.DPCM || editMode == EditionMode.None ? 1 : (editMode == EditionMode.VideoRecording ? 0 : 2)) * DefaultHeaderSizeY * scaling);
            effectPanelSizeY = (int)(DefaultEffectPanelSizeY * scaling);
            effectButtonSizeY = (int)(DefaultEffectButtonSizeY * scaling);
            noteSizeX = (int)(ScaleForZoom(DefaultNoteSizeX) * scaling);
            noteSizeY = (int)(DefaultNoteSizeY * scaling * videoScaleY);
            noteAttackSizeX = (int)(DefaultNoteAttackSizeX * scaling);
            releaseNoteSizeY = (int)(DefaultReleaseNoteSizeY * scaling * videoScaleY) & 0xfe; // Keep even
            envelopeMax = (int)(DefaultEnvelopeMax * scaling);
            whiteKeySizeY = (int)(DefaultWhiteKeySizeY * scaling * videoScaleY);
            whiteKeySizeX = (int)(DefaultWhiteKeySizeX * scaling * videoScaleX);
            recordKeyPosX = (int)((DefaultWhiteKeySizeX - 10) * scaling);
            blackKeySizeY = (int)(DefaultBlackKeySizeY * scaling * videoScaleY);
            blackKeySizeX = (int)(DefaultBlackKeySizeX * scaling * videoScaleX);
            effectIconPosX = (int)(DefaultEffectIconPosX * scaling);
            effectIconPosY = (int)(DefaultEffectIconPosY * scaling);
            snapIconPosX = (int)(DefaultSnapIconPosX * scaling);
            snapIconPosY = (int)(DefaultSnapIconPosY * scaling);
            effectNamePosX = (int)(DefaultEffectNamePosX * scaling);
            effectNamePosY = (int)(DefaultEffectNamePosY * scaling);
            effectIconSizeX = (int)(DefaultEffectIconSizeX * scaling);
            effectValuePosTextOffsetY = (int)(DefaultEffectValuePosTextOffsetY * scaling);
            effectValueNegTextOffsetY = (int)(DefaultEffectValueNegTextOffsetY * scaling);
            bigTextPosX = (int)(DefaultBigTextPosX * scaling);
            bigTextPosY = (int)(DefaultBigTextPosY * scaling);
            tooltipTextPosX = (int)(DefaultTooltipTextPosX * scaling);
            tooltipTextPosY = (int)(DefaultTooltipTextPosY * scaling);
            dpcmTextPosX = (int)(DefaultDPCMTextPosX * scaling);
            dpcmTextPosY = (int)(DefaultDPCMTextPosY * scaling);
            dpcmSourceDataPosX = (int)(DefaultDPCMSourceDataPosX * scaling);
            dpcmSourceDataPosY = (int)(DefaultDPCMSourceDataPosY * scaling);
            dpcmInfoSpacingY = (int)(DefaultDPCMInfoSpacingY * scaling);
            octaveNameOffsetY = (int)(DefaultOctaveNameOffsetY * scaling);
            recordingKeyOffsetY = (int)(DefaultRecordingKeyOffsetY * scaling);
            attackIconPosX = (int)(DefaultAttackIconPosX * scaling);
            minNoteSizeForText = (int)(DefaultMinNoteSizeForText * scaling);
            envelopeSizeY = DefaultEnvelopeSizeY * envelopeValueZoom * scaling;
            waveGeometrySampleSize = (int)(DefaultWaveGeometrySampleSize * scaling);
            waveDisplayPaddingY = (int)(DefaultWaveDisplayPaddingY * scaling);
            octaveSizeY = 12 * noteSizeY;
            numNotes = numOctaves * 12;
            barSizeX = noteSizeX * (Song == null ? 16 : Song.BeatLength);
            headerAndEffectSizeY = headerSizeY + (showEffectsPanel ? effectPanelSizeY : 0);
            noteTextPosY = scaling > 1 ? 0 : 1; // Pretty hacky.
            scrollBarThickness = Settings.ShowScrollBars ? (int)(DefaultScrollBarThickness * scaling) : 0;
            minScrollBarLength = (int)(DefaultMinScrollBarLength * scaling);
            virtualSizeY = numNotes * noteSizeY;
            scrollMargin = (int)(DefaultScrollMargin * scaling);
            noteResizeMargin = (int)(DefaultNoteResizeMargin * scaling);
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
            ConditionalInvalidate();
        }

        public void ChangeChannel(int trackIdx)
        {
            if (editMode == EditionMode.Channel)
            {
                AbortCaptureOperation();
                editChannel = trackIdx;
                noteTooltip = "";
                BuildSupportEffectList();
                ConditionalInvalidate();
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
            ConditionalInvalidate();
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
            ConditionalInvalidate();
        }

        public void StartEditDPCMSample(DPCMSample sample)
        {
            editMode = EditionMode.DPCM;
            editSample = sample;
            zoomLevel = 0;
            noteTooltip = "";
            envelopeValueZoom = 1;
            envelopeValueOffset = 0;

            ClearSelection();
            UpdateRenderCoords();
            CenterWaveScroll();
            ClampScroll();
            ConditionalInvalidate();
        }

        public void StartEditDPCMMapping()
        {
            editMode = EditionMode.DPCMMapping;
            showEffectsPanel = false;
            zoomLevel = 0;
            noteTooltip = "";
            envelopeValueZoom = 1;
            envelopeValueOffset = 0;

            ClearSelection();
            UpdateRenderCoords();
            CenterDPCMMappingScroll();
            ClampScroll();
            ConditionalInvalidate();
        }

        public void StartVideoRecording(RenderGraphics g, Song song, int zoom, float pianoRollScaleX, float pianoRollScaleY, out int outNoteSizeY)
        {
            editChannel = 0;
            editMode = EditionMode.VideoRecording;
            videoSong = song;
            zoomLevel = zoom;
            videoScaleX = pianoRollScaleX;
            videoScaleY = pianoRollScaleY;

            UpdateRenderCoords(1.0f);
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
                    if (Song.Channels[editChannel].SupportsEffect(i))
                        cnt++;
                }

                supportedEffects = new int[cnt];
                for (int i = 0, j = 0; i < Note.EffectCount; i++)
                {
                    if (Song.Channels[editChannel].SupportsEffect(i))
                        supportedEffects[j++] = i;
                }

                if (Array.IndexOf(supportedEffects, selectedEffectIdx) == -1)
                    selectedEffectIdx = supportedEffects.Length > 0 ? supportedEffects[0] : -1;
            }
        }

        private void CenterWaveScroll()
        {
            zoomLevel = maxZoomLevel;

            var duration = Math.Max(editSample.SourceDuration, editSample.ProcessedDuration);
            var viewSize = Width - whiteKeySizeX;
            var width    = (int)GetPixelForWaveTime(duration);

            while (width > viewSize && zoomLevel > minZoomLevel)
            {
                zoomLevel--;
                width /= 2;
            }

            scrollX = 0;
        }

        private void CenterEnvelopeScroll(Envelope envelope, int envelopeType, Instrument instrument = null)
        {
            var maxNumNotes = Width / DefaultNoteSizeX;

            if (envelope.Length == 0)
                zoomLevel = DefaultEnvelopeZoomLevel;
            else
                zoomLevel = Utils.Clamp((int)Math.Floor(Math.Log(maxNumNotes / (float)envelope.Length, 2.0)), minZoomLevel, maxZoomLevel);

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

        private void CenterScroll(int patternIdx = 0)
        {
            var maxScrollY = Math.Max(virtualSizeY + headerAndEffectSizeY - Height, 0);

            scrollX = Song.GetPatternStartAbsoluteNoteIndex(patternIdx) * noteSizeX;
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
            if (App.Project.UsesFamiTrackerTempo)
                snapResolution = (SnapResolution)Math.Max((int)snapResolution, (int)SnapResolution.OneNote);
        }

        private Song Song
        {
            get { return videoSong != null ? videoSong : App?.Song; }
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
            set { showSelection = value; ConditionalInvalidate(); }
        }

        public void ConditionalInvalidate()
        {
            if (!App.RealTimeUpdate)
                Invalidate();
        }

        public void Reset()
        {
            AbortCaptureOperation();
            showEffectsPanel = false;
            scrollX = 0;
            scrollY = 0;
            zoomLevel = 0;
            editMode = EditionMode.None;
            editChannel = -1;
            currentInstrument = null;
            currentArpeggio = null;
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
            ConditionalInvalidate();
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
            theme = RenderTheme.CreateResourcesForGraphics(g);
            whiteKeyBrush = g.CreateHorizontalGradientBrush(0, whiteKeySizeX, ThemeBase.LightGreyFillColor1, ThemeBase.LightGreyFillColor2);
            blackKeyBrush = g.CreateHorizontalGradientBrush(0, blackKeySizeX, ThemeBase.DarkGreyFillColor1, ThemeBase.DarkGreyFillColor2);
            whiteKeyPressedBrush = g.CreateHorizontalGradientBrush(0, whiteKeySizeX, ThemeBase.Darken(ThemeBase.LightGreyFillColor1), ThemeBase.Darken(ThemeBase.LightGreyFillColor2));
            blackKeyPressedBrush = g.CreateHorizontalGradientBrush(0, blackKeySizeX, ThemeBase.Lighten(ThemeBase.DarkGreyFillColor1), ThemeBase.Lighten(ThemeBase.DarkGreyFillColor2));
            frameLineBrush = g.CreateSolidBrush(Color.FromArgb(128, ThemeBase.DarkGreyLineColor2));
            debugBrush = g.CreateSolidBrush(ThemeBase.GreenColor);
            seekBarBrush = g.CreateSolidBrush(ThemeBase.SeekBarColor);
            seekBarRecBrush = g.CreateSolidBrush(ThemeBase.DarkRedFillColor);
            selectionBgVisibleBrush = g.CreateSolidBrush(Color.FromArgb(64, ThemeBase.LightGreyFillColor1));
            selectionBgInvisibleBrush = g.CreateSolidBrush(Color.FromArgb(16, ThemeBase.LightGreyFillColor1));
            selectionNoteBrush = g.CreateSolidBrush(ThemeBase.LightGreyFillColor1);
            hoverNoteBrush = g.CreateSolidBrush(ThemeBase.WhiteColor);
            attackBrush = g.CreateSolidBrush(Color.FromArgb(128, ThemeBase.BlackColor));
            iconTransparentBrush = g.CreateSolidBrush(Color.FromArgb(92, ThemeBase.DarkGreyLineColor2));
            dashedVerticalLineBrush = g.CreateBitmapBrush(g.CreateBitmapFromResource("Dash"), false, true);
            dashedHorizontalLineBrush = g.CreateBitmapBrush(g.CreateBitmapFromResource("DashHorizontal"), true, false);
            invalidDpcmMappingBrush = g.CreateSolidBrush(Color.FromArgb(64, ThemeBase.BlackColor));
            bmpLoop = g.CreateBitmapFromResource("LoopSmallFill");
            bmpRelease = g.CreateBitmapFromResource("ReleaseSmallFill");
            bmpEffects[Note.EffectVolume] = g.CreateBitmapFromResource("VolumeSmall");
            bmpEffects[Note.EffectVibratoSpeed] = g.CreateBitmapFromResource("VibratoSmall");
            bmpEffects[Note.EffectVibratoDepth] = g.CreateBitmapFromResource("VibratoSmall");
            bmpEffects[Note.EffectFinePitch] = g.CreateBitmapFromResource("PitchSmall");
            bmpEffects[Note.EffectSpeed] = g.CreateBitmapFromResource("SpeedSmall");
            bmpEffects[Note.EffectFdsModDepth] = g.CreateBitmapFromResource("ModSmall");
            bmpEffects[Note.EffectFdsModSpeed] = g.CreateBitmapFromResource("ModSmall");
            bmpEffects[Note.EffectDutyCycle] = g.CreateBitmapFromResource("DutyCycleSmall");
            bmpEffects[Note.EffectNoteDelay] = g.CreateBitmapFromResource("NoteDelaySmall");
            bmpEffects[Note.EffectCutDelay] = g.CreateBitmapFromResource("CutDelaySmall");
            bmpEffectExpanded = g.CreateBitmapFromResource("ExpandedSmall");
            bmpEffectCollapsed = g.CreateBitmapFromResource("CollapsedSmall");
            bmpSlide = g.CreateBitmapFromResource("Slide");
            bmpSnap = g.CreateBitmapFromResource("Snap");
            bmpSnapRed = g.CreateBitmapFromResource("SnapRed");
            bmpSnapResolution[(int)SnapResolution.OneNote] = g.CreateBitmapFromResource("Snap1");
            bmpSnapResolution[(int)SnapResolution.TwoNote] = g.CreateBitmapFromResource("Snap2");
            bmpSnapResolution[(int)SnapResolution.ThreeNote] = g.CreateBitmapFromResource("Snap3");
            bmpSnapResolution[(int)SnapResolution.FourNote] = g.CreateBitmapFromResource("Snap4");

            for (int z = MinZoomLevel; z <= MaxZoomLevel; z++)
            {
                int idx = z - MinZoomLevel;
                int x = (int)(DefaultNoteSizeX * g.WindowScaling * (float)Math.Pow(2.0, z) - 1);

                stopNoteGeometry[idx, 0] = g.CreateGeometry(new float[,]
                {
                    { 0, 0 },
                    { 0, noteSizeY },
                    { x, noteSizeY / 2 }
                });

                releaseNoteGeometry[idx, 0] = g.CreateGeometry(new float[,]
                {
                    { 0, 0 },
                    { 0, noteSizeY },
                    { x + 1, noteSizeY - noteSizeY / 2 + releaseNoteSizeY / 2 },
                    { x + 1, noteSizeY / 2 - releaseNoteSizeY / 2 }
                });

                stopReleaseNoteGeometry[idx, 0] = g.CreateGeometry(new float[,]
                {
                    { 0, noteSizeY / 2 - releaseNoteSizeY / 2 },
                    { 0, noteSizeY / 2 + releaseNoteSizeY / 2 },
                    { x, noteSizeY / 2 }
                });

                stopNoteGeometry[idx, 1] = g.CreateGeometry(new float[,]
                {
                    { 0, 1 },
                    { 0, noteSizeY },
                    { x, noteSizeY / 2 }
                });

                releaseNoteGeometry[idx, 1] = g.CreateGeometry(new float[,]
                {
                    { 0, 1 },
                    { 0, noteSizeY },
                    { x + 1, noteSizeY - noteSizeY / 2 + releaseNoteSizeY / 2 },
                    { x + 1, noteSizeY / 2 - releaseNoteSizeY / 2 + 1 }
                });

                stopReleaseNoteGeometry[idx, 1] = g.CreateGeometry(new float[,]
                {
                    { 0, noteSizeY / 2 - releaseNoteSizeY / 2 + 1 },
                    { 0, noteSizeY / 2 + releaseNoteSizeY / 2 },
                    { x, noteSizeY / 2 }
                });

                slideNoteGeometry[idx] = g.CreateGeometry(new float[,]
                {
                    { 0, 0 },
                    { x + 1, 0 },
                    { x + 1,  noteSizeY }
                });
            }

            seekGeometry = g.CreateGeometry(new float[,]
            {
                { -headerSizeY / 2, 1 },
                { 0, headerSizeY - 2 },
                { headerSizeY / 2, 1 }
            });

            sampleGeometry = g.CreateGeometry(new float[,]
            {
                { -waveGeometrySampleSize, -waveGeometrySampleSize },
                {  waveGeometrySampleSize, -waveGeometrySampleSize },
                {  waveGeometrySampleSize,  waveGeometrySampleSize },
                { -waveGeometrySampleSize,  waveGeometrySampleSize }
            });
        }

        protected override void OnRenderTerminated()
        {
            theme.Terminate();

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
            Utils.DisposeAndNullify(ref dashedVerticalLineBrush);
            Utils.DisposeAndNullify(ref dashedHorizontalLineBrush);
            Utils.DisposeAndNullify(ref invalidDpcmMappingBrush);
            Utils.DisposeAndNullify(ref bmpLoop);
            Utils.DisposeAndNullify(ref bmpRelease);
            Utils.DisposeAndNullify(ref bmpEffectExpanded);
            Utils.DisposeAndNullify(ref bmpEffectCollapsed);
            Utils.DisposeAndNullify(ref bmpSlide);
            Utils.DisposeAndNullify(ref bmpSnap);
            Utils.DisposeAndNullify(ref bmpSnapRed);

            for (int i = 0; i < (int)SnapResolution.Max; i++)
            {
                Utils.DisposeAndNullify(ref bmpSnapResolution[i]);
            }

            for (int i = 0; i < Note.EffectCount; i++)
            {
                Utils.DisposeAndNullify(ref bmpEffects[i]);
            }

            for (int z = MinZoomLevel; z <= MaxZoomLevel; z++)
            {
                int idx = z - MinZoomLevel;

                Utils.DisposeAndNullify(ref stopNoteGeometry[idx, 0]);
                Utils.DisposeAndNullify(ref releaseNoteGeometry[idx, 0]);
                Utils.DisposeAndNullify(ref stopReleaseNoteGeometry[idx, 0]);
                Utils.DisposeAndNullify(ref stopNoteGeometry[idx, 1]);
                Utils.DisposeAndNullify(ref releaseNoteGeometry[idx, 1]);
                Utils.DisposeAndNullify(ref stopReleaseNoteGeometry[idx, 1]);
                Utils.DisposeAndNullify(ref slideNoteGeometry[idx]);
            }

            Utils.DisposeAndNullify(ref seekGeometry);
            Utils.DisposeAndNullify(ref sampleGeometry);
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
                minNoteIdx = Math.Max((int)Math.Floor(scrollX / (float)noteSizeX), 0);
                maxNoteIdx = Math.Min((int)Math.Ceiling((scrollX + Width - whiteKeySizeX) / (float)noteSizeX), Song.GetPatternStartAbsoluteNoteIndex(Song.Length));
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

        private void ForEachWaveTimecode(RenderGraphics g, RenderArea a, Action<float, float, int, int> function)
        {
            var textSize  = g.MeasureString("99.999", ThemeBase.FontMediumCenter);
            var waveWidth = Width - whiteKeySizeX;
            var numLabels = Math.Floor(waveWidth / textSize);

            for (int i = 2; i >= 0; i--)
            {
                var divTime = Math.Pow(10.0, -i - 1);

                var minLabel = (int)Math.Floor  (a.minVisibleWaveTime / divTime);
                var maxLabel = (int)Math.Ceiling(a.maxVisibleWaveTime / divTime);

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

        class RenderArea
        {
            public int maxVisibleNote;
            public int minVisibleNote;
            public int maxVisibleOctave;
            public int minVisibleOctave;
            public int minVisiblePattern;
            public int maxVisiblePattern;
            public float minVisibleWaveTime;
            public float maxVisibleWaveTime;
        }

        private void RenderHeader(RenderGraphics g, RenderArea a)
        {
            g.PushTranslation(whiteKeySizeX, 0);
            g.PushClip(0, 0, Width, headerSizeY);
            g.Clear(ThemeBase.DarkGreyLineColor2);

            if ((editMode == EditionMode.Enveloppe || editMode == EditionMode.Arpeggio) && EditEnvelope != null)
            {
                var env = EditEnvelope;

                g.PushTranslation(0, headerSizeY / 2);

                DrawSelectionRect(g, headerSizeY);

                if (env.Loop >= 0)
                {
                    g.PushTranslation(env.Loop * noteSizeX - scrollX, 0);
                    g.FillRectangle(0, 0, ((env.Release >= 0 ? env.Release : env.Length) - env.Loop) * noteSizeX, headerAndEffectSizeY, theme.DarkGreyFillBrush2);
                    g.DrawLine(0, 0, 0, headerAndEffectSizeY, theme.BlackBrush);
                    g.DrawBitmap(bmpLoop, effectIconPosX + 1, effectIconPosY);
                    g.PopTransform();
                }
                if (env.Release >= 0)
                {
                    g.PushTranslation(env.Release * noteSizeX - scrollX, 0);
                    g.DrawLine(0, 0, 0, headerAndEffectSizeY, theme.BlackBrush);
                    g.DrawBitmap(bmpRelease, effectIconPosX + 1, effectIconPosY);
                    g.PopTransform();
                }
                if (env.Length > 0)
                {
                    g.PushTranslation(env.Length * noteSizeX - scrollX, 0);
                    g.DrawLine(0, 0, 0, headerAndEffectSizeY, theme.BlackBrush);
                    g.PopTransform();
                }

                g.DrawLine(0, headerSizeY - 1, Width, headerSizeY - 1, theme.BlackBrush);
                g.PopTransform();

                DrawSelectionRect(g, headerSizeY);

                // Draw the header bars
                for (int n = 0; n <= env.Length; n++)
                {
                    int x = n * noteSizeX - scrollX;
                    if (x != 0)
                        g.DrawLine(x, 0, x, headerSizeY / 2, theme.BlackBrush, 1.0f);
                    if (zoomLevel >= 1 && n != env.Length)
                        g.DrawText(n.ToString(), ThemeBase.FontMediumCenter, x, effectNamePosY, theme.LightGreyFillBrush1, noteSizeX);
                }

                g.DrawLine(0, headerSizeY / 2 - 1, Width, headerSizeY / 2 - 1, theme.BlackBrush);
            }
            else if (editMode == EditionMode.Channel)
            {
                // Draw colored header
                for (int p = a.minVisiblePattern; p < a.maxVisiblePattern; p++)
                {
                    var pattern = Song.Channels[editChannel].PatternInstances[p];
                    if (pattern != null)
                    {
                        int sx = Song.GetPatternLength(p) * noteSizeX;
                        int px = Song.GetPatternStartAbsoluteNoteIndex(p) * noteSizeX - scrollX;
                        g.FillRectangle(px, headerSizeY / 2, px + sx, headerSizeY, theme.CustomColorBrushes[pattern.Color]);
                    }
                }

                DrawSelectionRect(g, headerSizeY);

                // Draw the header bars
                for (int p = a.minVisiblePattern; p < a.maxVisiblePattern; p++)
                {
                    var sx = Song.GetPatternLength(p) * noteSizeX;
                    int px = Song.GetPatternStartAbsoluteNoteIndex(p) * noteSizeX - scrollX;
                    if (p != 0)
                        g.DrawLine(px, 0, px, headerSizeY, theme.BlackBrush, 3.0f);
                    var pattern = Song.Channels[editChannel].PatternInstances[p];
                    g.DrawText(p.ToString(), ThemeBase.FontMediumCenter, px, effectNamePosY, theme.LightGreyFillBrush1, sx);
                    if (pattern != null)
                        g.DrawText(pattern.Name, ThemeBase.FontMediumCenter, px, effectNamePosY + headerSizeY / 2, theme.BlackBrush, sx);
                }

                int maxX = Song.GetPatternStartAbsoluteNoteIndex(a.maxVisiblePattern) * noteSizeX - scrollX;
                g.DrawLine(maxX, 0, maxX, Height, theme.BlackBrush, 3.0f);
                g.DrawLine(0, headerSizeY / 2 - 1, Width, headerSizeY / 2 - 1, theme.BlackBrush);
            }
            else if (editMode == EditionMode.DPCM)
            {
                // Selection rectangle
                if (IsSelectionValid())
                {
                    g.FillRectangle(
                        GetPixelForWaveTime(GetWaveTimeForSample(selectionMin, true),  scrollX), 0,
                        GetPixelForWaveTime(GetWaveTimeForSample(selectionMax, false), scrollX), Height, selectionBgVisibleBrush);
                }

                ForEachWaveTimecode(g, a, (time, x, level, idx) =>
                {
                    if (time != 0.0f)
                        g.DrawText(time.ToString($"F{level + 1}"), ThemeBase.FontMediumCenter, x - 100, effectNamePosY, theme.LightGreyFillBrush1, 200);
                });

                // Processed Range
                var processedBrush = g.GetSolidBrush(editSample.Color, 1.0f, 0.25f);
                g.FillRectangle(
                    GetPixelForWaveTime(editSample.ProcessedStartTime, scrollX), 0,
                    GetPixelForWaveTime(editSample.ProcessedEndTime,   scrollX), Height, processedBrush);
            }

            g.DrawLine(0, headerSizeY - 1, Width, headerSizeY - 1, theme.BlackBrush);

            if (((editMode == EditionMode.Enveloppe || editMode == EditionMode.Arpeggio) && editEnvelope < EnvelopeType.RegularCount) || (editMode == EditionMode.Channel))
            {
                var seekFrame = editMode == EditionMode.Enveloppe || editMode == EditionMode.Arpeggio ? App.GetEnvelopeFrame(editInstrument, editEnvelope, editMode == EditionMode.Arpeggio) : GetSeekFrameToDraw();
                if (seekFrame >= 0)
                {
                    g.PushTranslation(seekFrame * noteSizeX - scrollX, 0);
                    g.FillAndDrawGeometry(seekGeometry, GetSeekBarBrush(), theme.BlackBrush);
                    g.DrawLine(0, headerSizeY / 2, 0, headerSizeY, GetSeekBarBrush(), 3);
                    g.PopTransform();
                }
            }

            g.PopClip();
            g.PopTransform();
        }

        private void RenderEffectList(RenderGraphics g)
        {
            g.PushClip(0, 0, whiteKeySizeX, headerAndEffectSizeY);
            g.FillRectangle(0, 0, whiteKeySizeX, Height, theme.DarkGreyFillBrush1);
            g.DrawLine(whiteKeySizeX - 1, 0, whiteKeySizeX - 1, headerAndEffectSizeY, theme.BlackBrush);

            // Effect icons
            if (editMode == EditionMode.Channel)
            {
                g.DrawBitmap(showEffectsPanel ? bmpEffectExpanded : bmpEffectCollapsed, effectIconPosX, effectIconPosY);

                if (IsSnappingAllowed)
                {
                    g.DrawBitmap(bmpSnapResolution[(int)snapResolution], whiteKeySizeX - (int)bmpSnap.Size.Width * 2 - snapIconPosX - 1, snapIconPosY, IsSnappingEnabled ? 1.0f : 0.3f);
                    g.DrawBitmap(App.IsRecording ? bmpSnapRed : bmpSnap, whiteKeySizeX - (int)bmpSnap.Size.Width * 1 - snapIconPosX * 1 - 1, snapIconPosY, IsSnappingEnabled || App.IsRecording ? 1.0f : 0.3f);
                }

                if (showEffectsPanel)
                {
                    g.PushTranslation(0, headerSizeY);

                    int effectButtonY = 0;

                    for (int i = 0; i < supportedEffects.Length; i++, effectButtonY += effectButtonSizeY)
                    {
                        var effectIdx = supportedEffects[i];

                        g.PushTranslation(0, effectButtonY);
                        g.DrawLine(0, -1, whiteKeySizeX, -1, theme.BlackBrush);
                        g.DrawBitmap(bmpEffects[effectIdx], effectIconPosX, effectIconPosY);
                        g.DrawText(Note.EffectNames[effectIdx], selectedEffectIdx == effectIdx ? ThemeBase.FontSmallBold : ThemeBase.FontSmall, effectNamePosX, effectNamePosY, theme.LightGreyFillBrush2);
                        g.PopTransform();
                    }

                    g.PushTranslation(0, effectButtonY);
                    g.DrawLine(0, -1, whiteKeySizeX, -1, theme.BlackBrush);
                    g.PopTransform();
                    g.PopTransform();
                }
            }
            else if (editMode == EditionMode.DPCM)
            {
                g.DrawBitmap(showEffectsPanel ? bmpEffectExpanded : bmpEffectCollapsed, 0, 0);

                if (showEffectsPanel)
                {
                    g.PushTranslation(0, headerSizeY);
                    g.DrawLine(0, -1, whiteKeySizeX, -1, theme.BlackBrush);
                    g.DrawBitmap(bmpEffects[Note.EffectVolume], effectIconPosX, effectIconPosY);
                    g.DrawText(Note.EffectNames[Note.EffectVolume], ThemeBase.FontSmallBold, effectNamePosX, effectNamePosY, theme.LightGreyFillBrush2);
                    g.PopTransform();

                    g.PushTranslation(0, effectButtonSizeY);
                    g.DrawLine(0, -1, whiteKeySizeX, -1, theme.BlackBrush);
                    g.PopTransform();
                }
            }

            g.DrawLine(0, headerAndEffectSizeY - 1, whiteKeySizeX, headerAndEffectSizeY - 1, theme.BlackBrush);
            g.PopClip();
        }

        private void RenderPiano(RenderGraphics g, RenderArea a)
        {
            g.PushTranslation(0, headerAndEffectSizeY);
            g.PushClip(0, 0, whiteKeySizeX, Height);
            g.FillRectangle(0, 0, whiteKeySizeX, Height, whiteKeyBrush);

            var playOctave = -1;
            var playNote = -1;
            var draggingNote = captureOperation == CaptureOperation.DragNote || captureOperation == CaptureOperation.CreateNote;
            var dragOctave = (dragLastNoteValue - 1) / 12;
            var dragNote = (dragLastNoteValue - 1) % 12;

            if (playingNote > 0)
            {
                playOctave = (playingNote - 1) / 12;
                playNote = (playingNote - 1) - playOctave * 12;

                if (!IsBlackKey(playNote))
                    g.FillRectangle(GetKeyRectangle(playOctave, playNote), whiteKeyPressedBrush);
            }

            if (draggingNote && !IsBlackKey(dragNote))
            {
                g.FillRectangle(GetKeyRectangle(dragOctave, dragNote), whiteKeyPressedBrush);
            }

            // Draw the piano
            for (int i = a.minVisibleOctave; i < a.maxVisibleOctave; i++)
            {
                int octaveBaseY = (virtualSizeY - octaveSizeY * i) - scrollY;

                for (int j = 0; j < 12; j++)
                {
                    if (i * 12 + j >= numNotes)
                        break;

                    if (IsBlackKey(j))
                    {
                        g.FillRectangle(GetKeyRectangle(i, j), blackKeyBrush);

                        if ((i == playOctave && j == playNote) || (draggingNote && (i == dragOctave && j == dragNote)))
                            g.FillRectangle(GetKeyRectangle(i, j), blackKeyPressedBrush);
                    }

                    int y = octaveBaseY - j * noteSizeY;
                    if (j == 0)
                        g.DrawLine(0, y, whiteKeySizeX, y, theme.BlackBrush);
                    else if (j == 5)
                        g.DrawLine(0, y, whiteKeySizeX, y, theme.BlackBrush);
                }

                if (editMode == EditionMode.Channel || editMode == EditionMode.DPCMMapping)
                    g.DrawText("C" + i, ThemeBase.FontSmall, 1, octaveBaseY - octaveNameOffsetY, theme.BlackBrush);
            }

            if (App != null && (App.IsRecording || App.IsQwertyPianoEnabled))
            {
                var showQwerty = App.IsRecording || App.IsQwertyPianoEnabled;

                foreach (var kv in Settings.KeyCodeToNoteMap)
                {
                    var i = kv.Value - 1;
                    var k = kv.Key;

                    if (i < 0)
                        continue;

                    int octaveBaseY = (virtualSizeY - octaveSizeY * ((i / 12) + App.BaseRecordingOctave)) - scrollY;
                    int y = octaveBaseY - (i % 12) * noteSizeY;

                    RenderBrush brush;
                    if (App.IsRecording)
                        brush = IsBlackKey(i % 12) ? theme.LightRedFillBrush : theme.DarkRedFillBrush;
                    else
                        brush = IsBlackKey(i % 12) ? theme.LightGreyFillBrush2 : theme.BlackBrush;

                    g.DrawText(PlatformUtils.KeyCodeToString(k), ThemeBase.FontVerySmallCenter, 0, y - recordingKeyOffsetY + g.WindowScaling * 2, brush, blackKeySizeX);
                }
            }

            g.DrawLine(whiteKeySizeX - 1, 0, whiteKeySizeX - 1, Height, theme.BlackBrush);

            g.PopClip();
            g.PopTransform();
        }

        private void RenderEffectPanel(RenderGraphics g, RenderArea a)
        {
            if ((editMode == EditionMode.Channel || editMode == EditionMode.DPCM) && showEffectsPanel)
            {
                g.PushTranslation(whiteKeySizeX, headerSizeY);
                g.PushClip(0, 0, Width, effectPanelSizeY);
                g.Clear(editMode == EditionMode.Channel ? ThemeBase.DarkGreyFillColor1 : ThemeBase.DarkGreyLineColor2);

                if (editMode == EditionMode.Channel)
                {
                    var song = Song;
                    var channel = song.Channels[editChannel];

                    var minLocation = new NoteLocation(a.minVisiblePattern, 0);
                    var maxLocation = new NoteLocation(a.maxVisiblePattern, 0);

                    // Draw the effects current value rectangles. Not all effects need this.
                    if (selectedEffectIdx >= 0 && Note.EffectWantsPreviousValue(selectedEffectIdx))
                    {
                        var lastFrame = -1;
                        var lastValue = channel.GetCachedLastValidEffectValue(a.minVisiblePattern - 1, selectedEffectIdx);
                        var minValue = Note.GetEffectMinValue(song, channel, selectedEffectIdx);
                        var maxValue = Note.GetEffectMaxValue(song, channel, selectedEffectIdx);

                        for (var it = channel.GetSparseNoteIterator(minLocation, maxLocation, NoteFilter.All); !it.Done; it.Next())
                        {
                            var note = it.Note;
                            var location = it.Location;

                            if (note.HasValidEffectValue(selectedEffectIdx))
                            {
                                g.PushTranslation(location.ToAbsoluteNoteIndex(song) * noteSizeX - scrollX, 0);

                                var frame = location.ToAbsoluteNoteIndex(song);
                                var sizeY = (maxValue == minValue) ? effectPanelSizeY : (float)Math.Floor((lastValue - minValue) / (float)(maxValue - minValue) * effectPanelSizeY);
                                g.FillRectangle(lastFrame < 0 ? -noteSizeX * 100000 : (frame - lastFrame - 1) * -noteSizeX, effectPanelSizeY - sizeY, 0, effectPanelSizeY, theme.DarkGreyFillBrush2);
                                lastValue = note.GetEffectValue(selectedEffectIdx);
                                lastFrame = frame;

                                g.PopTransform();
                            }
                        }

                        g.PushTranslation(Math.Max(0, lastFrame * noteSizeX - scrollX), 0);
                        var lastSizeY = (maxValue == minValue) ? effectPanelSizeY : (float)Math.Floor((lastValue - minValue) / (float)(maxValue - minValue) * effectPanelSizeY);
                        g.FillRectangle(0, effectPanelSizeY - lastSizeY, Width, effectPanelSizeY, theme.DarkGreyFillBrush2);
                        g.PopTransform();
                    }

                    DrawSelectionRect(g, effectPanelSizeY);

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

                            g.PushTranslation(location.ToAbsoluteNoteIndex(song) * noteSizeX - scrollX, 0);

                            if (!Note.EffectWantsPreviousValue(selectedEffectIdx))
                                g.FillRectangle(0, 0, noteSizeX, effectPanelSizeY, theme.DarkGreyFillBrush2);

                            var hover = location == hoverLocation;

                            g.FillRectangle(0, effectPanelSizeY - sizeY, noteSizeX, effectPanelSizeY, theme.LightGreyFillBrush1);
                            g.DrawRectangle(0, effectPanelSizeY - sizeY, noteSizeX, effectPanelSizeY, hover ? theme.WhiteBrush : theme.BlackBrush, hover || IsNoteSelected(location) ? 2 : 1);

                            var text = effectValue.ToString();
                            if ((text.Length <= 2 && zoomLevel >= 0) || zoomLevel > 0)
                            {
                                if (sizeY < effectPanelSizeY / 2)
                                    g.DrawText(text, ThemeBase.FontSmallCenter, 0, effectPanelSizeY - sizeY - effectValuePosTextOffsetY, theme.LightGreyFillBrush1, noteSizeX);
                                else
                                    g.DrawText(text, ThemeBase.FontSmallCenter, 0, effectPanelSizeY - sizeY + effectValueNegTextOffsetY, theme.BlackBrush, noteSizeX);
                            }

                            g.PopTransform();
                        }
                    }

                    // Thick vertical bars
                    for (int p = a.minVisiblePattern; p < a.maxVisiblePattern; p++)
                    {
                        int x = Song.GetPatternStartAbsoluteNoteIndex(p) * noteSizeX - scrollX;
                        if (p != 0) g.DrawLine(x, 0, x, Height, theme.BlackBrush, 3.0f);
                    }

                    int maxX = Song.GetPatternStartAbsoluteNoteIndex(a.maxVisiblePattern) * noteSizeX - scrollX;
                    g.DrawLine(maxX, 0, maxX, Height, theme.BlackBrush, 3.0f);

                    int seekX = GetSeekFrameToDraw() * noteSizeX - scrollX;
                    g.DrawLine(seekX, 0, seekX, effectPanelSizeY, GetSeekBarBrush(), 3);
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

                        RenderGeometry geo = g.CreateGeometry(points, false);
                        g.FillGeometry(geo, theme.DarkGreyFillBrush1);
                        geo.Dispose();
                    }

                    // Horizontal center line
                    g.DrawLine(0, halfPanelSizeY, Width, halfPanelSizeY, theme.BlackBrush);

                    // Top/bottom dash lines (limits);
                    var topY    = waveDisplayPaddingY;
                    var bottomY = effectPanelSizeY - waveDisplayPaddingY;
                    g.DrawLine(0, topY,    Width, topY,    dashedHorizontalLineBrush);
                    g.DrawLine(0, bottomY, Width, bottomY, dashedHorizontalLineBrush);

                    // Envelope line
                    g.AntiAliasing = true;
                    for (int i = 0; i < 3; i++)
                    {
                        g.DrawLine(
                            envelopePoints[i + 0].X, 
                            envelopePoints[i + 0].Y,
                            envelopePoints[i + 1].X, 
                            envelopePoints[i + 1].Y, 
                            theme.LightGreyFillBrush1);
                    }
                    g.AntiAliasing = false;

                    // Envelope vertices.
                    for (int i = 0; i < 4; i++)
                    {
                        g.PushTransform(
                            envelopePoints[i + 0].X,
                            envelopePoints[i + 0].Y, 
                            1.0f, 1.0f);
                        g.FillGeometry(sampleGeometry, theme.LightGreyFillBrush1);
                        g.PopTransform();
                    }

                    // Selection rectangle
                    if (IsSelectionValid())
                    {
                        g.FillRectangle(
                            GetPixelForWaveTime(GetWaveTimeForSample(selectionMin, true),  scrollX), 0,
                            GetPixelForWaveTime(GetWaveTimeForSample(selectionMax, false), scrollX), Height, selectionBgVisibleBrush);
                    }
                }

                g.DrawLine(0, effectPanelSizeY - 1, Width, effectPanelSizeY - 1, theme.BlackBrush);
                g.PopClip();
                g.PopTransform();
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
                            note.Slide = channel.SupportsSlideNotes ? newNote.Slide : (byte)0;
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

                var dlg = new PasteSpecialDialog(Song.Channels[editChannel], lastPasteSpecialPasteMix, lastPasteSpecialPasteNotes, lastPasteSpecialPasteEffectMask);

                if (dlg.ShowDialog(ParentForm) == DialogResult.OK)
                {
                    PasteNotes(dlg.PasteNotes, dlg.PasteEffectMask, dlg.PasteMix, dlg.PasteRepeat);

                    lastPasteSpecialPasteMix = dlg.PasteMix;
                    lastPasteSpecialPasteNotes = dlg.PasteNotes;
                    lastPasteSpecialPasteEffectMask = dlg.PasteEffectMask;
                }
            }
        }

        public void DeleteSpecial()
        {
            if (editMode == EditionMode.Channel)
            {
                AbortCaptureOperation();

                var dlg = new DeleteSpecialDialog(Song.Channels[editChannel]);

                if (dlg.ShowDialog(ParentForm) == DialogResult.OK)
                    DeleteSelectedNotes(true, dlg.DeleteNotes, dlg.DeleteEffectMask);
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

        private void DrawSelectionRect(RenderGraphics g, int height)
        {
            if (IsSelectionValid())
            {
                g.FillRectangle(
                    (selectionMin + 0) * noteSizeX - scrollX, 0,
                    (selectionMax + 1) * noteSizeX - scrollX, height, showSelection ? selectionBgVisibleBrush : selectionBgInvisibleBrush);
            }
        }

        private Color GetNoteColor(Channel channel, int noteValue, Instrument instrument, Project project)
        {
            if (channel.Type == ChannelType.Dpcm)
            {
                var mapping = project.GetDPCMMapping(noteValue);
                if (mapping != null && mapping.Sample != null)
                    return mapping.Sample.Color;
            }
            else if (instrument != null)
            {
                return instrument.Color;
            }

            return ThemeBase.LightGreyFillColor1;
        }

        private void RenderNotes(RenderGraphics g, RenderArea a)
        {
            var song = Song;

            g.PushTranslation(whiteKeySizeX, headerAndEffectSizeY);
            g.PushClip(0, 0, Width, Height);
            g.Clear(ThemeBase.DarkGreyLineColor2);

            if (editMode == EditionMode.Channel ||
                editMode == EditionMode.VideoRecording ||
                editMode == EditionMode.DPCMMapping)
            {
                int maxX = editMode == EditionMode.Channel ? song.GetPatternStartAbsoluteNoteIndex(a.maxVisiblePattern) * noteSizeX - scrollX : Width;

                // Draw the note backgrounds
                for (int i = a.minVisibleOctave; i < a.maxVisibleOctave; i++)
                {
                    int octaveBaseY = (virtualSizeY - octaveSizeY * i) - scrollY;

                    for (int j = 0; j < 12; j++)
                    {
                        int y = octaveBaseY - j * noteSizeY;
                        if (!IsBlackKey(j))
                            g.FillRectangle(0, y - noteSizeY, maxX, y, theme.DarkGreyFillBrush1);
                        if (i * 12 + j != numNotes)
                            g.DrawLine(0, y, maxX, y, theme.BlackBrush);
                    }
                }

                DrawSelectionRect(g, Height);

                if (editMode == EditionMode.Channel ||
                    editMode == EditionMode.VideoRecording)
                {
                    // Draw the vertical bars.
                    for (int p = a.minVisiblePattern; p < a.maxVisiblePattern; p++)
                    {
                        var patternLen = song.GetPatternLength(p);
                        var beatLength = song.GetPatternBeatLength(p);

                        if (song.UsesFamiStudioTempo)
                        {
                            var noteLength = song.GetPatternNoteLength(p);

                            for (int i = p == 0 ? 1 : 0; i < patternLen; i++)
                            {
                                int x = (song.GetPatternStartAbsoluteNoteIndex(p) + i) * noteSizeX - scrollX;

                                if (i % beatLength == 0)
                                    g.DrawLine(x, 0, x, Height, theme.BlackBrush, i == 0 ? 3.0f : 1.0f);
                                else if (i % noteLength == 0)
                                    g.DrawLine(x, 0, x, Height, theme.DarkGreyLineBrush1);
                                else if (zoomLevel >= DrawFrameZoomLevel)
                                    g.DrawLine(x, 0, x, Height, dashedVerticalLineBrush /*theme.DarkGreyLineBrush3*/);
                            }
                        }
                        else
                        {
                            for (int i = p == 0 ? 1 : 0; i < patternLen; i++)
                            {
                                int x = (song.GetPatternStartAbsoluteNoteIndex(p) + i) * noteSizeX - scrollX;

                                if (i % beatLength == 0)
                                    g.DrawLine(x, 0, x, Height, theme.BlackBrush, i == 0 ? 3.0f : 1.0f);
                                else if (zoomLevel >= -1)
                                    g.DrawLine(x, 0, x, Height, theme.DarkGreyLineBrush2);
                            }
                        }
                    }

                    g.DrawLine(maxX, 0, maxX, Height, theme.BlackBrush, 3.0f);

                    if (editMode != EditionMode.VideoRecording)
                    {
                        int seekX = GetSeekFrameToDraw() * noteSizeX - scrollX;
                        g.DrawLine(seekX, 0, seekX, Height, GetSeekBarBrush(), 3);
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
                            hoverNote = song.Channels[editChannel].PatternInstances[hoverLocation.PatternIndex].Notes[hoverLocation.NoteIndex];
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

                            var min = new NoteLocation(a.minVisiblePattern, 0);
                            var max = new NoteLocation(a.maxVisiblePattern, 0);

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

                                if (it.Location == hoverLocation)
                                {
                                    hoverReleased = released;
                                    hoverLastNoteValue = lastNoteValue;
                                    hoverLastInstrument = lastInstrument;
                                }

                                if (note.IsMusical)
                                {
                                    RenderNote(g, it.Location, note, song, channel, it.DistanceToNextCut, drawImplicitStopNotes, isActiveChannel, false, released);
                                }
                                else if (note.IsStop)
                                {
                                    RenderNoteReleaseOrStop(g, note, GetNoteColor(channel, lastNoteValue, lastInstrument, song.Project), it.Location.ToAbsoluteNoteIndex(Song), lastNoteValue, false, IsNoteSelected(it.Location, 1), true, released);
                                }

                                if (note.HasRelease && note.Release < Math.Min(note.Duration, it.DistanceToNextCut))
                                {
                                    released = true;
                                }
                            }

                            if (hoverNote != null)
                            {
                                if (hoverNote.IsMusical)
                                {
                                    RenderNote(g, hoverLocation, hoverNote, song, channel, channel.GetDistanceToNextNote(hoverLocation), drawImplicitStopNotes, true, true, hoverReleased);
                                }
                                else if (hoverNote.IsStop)
                                {
                                    RenderNoteReleaseOrStop(g, hoverNote, GetNoteColor(channel, hoverLastNoteValue, hoverLastInstrument, song.Project), hoverLocation.ToAbsoluteNoteIndex(Song), hoverLastNoteValue, true, false, true, hoverReleased);
                                }
                            }
                        }
                    }

                    // Draw effect icons at the top.
                    if (editMode != EditionMode.VideoRecording)
                    {
                        var channel = song.Channels[editChannel];
                        for (int p = a.minVisiblePattern; p < a.maxVisiblePattern; p++)
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

                                            bool drawOpaque = !showEffectsPanel || fx == selectedEffectIdx || fx == Note.EffectVibratoDepth && selectedEffectIdx == Note.EffectVibratoSpeed || fx == Note.EffectVibratoSpeed && selectedEffectIdx == Note.EffectVibratoDepth;

                                            int iconX = channel.Song.GetPatternStartAbsoluteNoteIndex(p, time) * noteSizeX + noteSizeX / 2 - effectIconSizeX / 2 - scrollX;
                                            int iconY = effectPosY + effectIconPosY;
                                            g.FillRectangle(iconX, iconY, iconX + effectIconSizeX, iconY + effectIconSizeX, theme.DarkGreyLineBrush2);
                                            g.DrawBitmap(bmpEffects[fx], iconX, iconY, drawOpaque ? 1.0f : 0.4f);
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
                        var channelName = song.Channels[editChannel].Name;

                        if (channelType >= ChannelType.ExpansionAudioStart)
                            channelName += $" ({song.Project.ExpansionAudioName})";

                        g.DrawText($"Editing {channelName} Channel", ThemeBase.FontBig, bigTextPosX, maxEffectPosY > 0 ? maxEffectPosY : bigTextPosY, whiteKeyBrush);
                    }
                }
                else if (App.Project != null) // Happens if DPCM panel is open and importing an NSF.
                {
                    // Draw 2 dark rectangle to show invalid range. 
                    g.PushTranslation(0, -scrollY);
                    g.FillRectangle(0, virtualSizeY, Width, virtualSizeY - Note.DPCMNoteMin * noteSizeY, invalidDpcmMappingBrush);
                    g.FillRectangle(0, 0, Width, virtualSizeY - Note.DPCMNoteMax * noteSizeY, invalidDpcmMappingBrush);
                    g.PopTransform();

                    for (int i = 0; i < Note.MusicalNoteMax; i++)
                    {
                        var mapping = App.Project.GetDPCMMapping(i);
                        if (mapping != null && mapping.Sample != null)
                        {
                            var y = virtualSizeY - i * noteSizeY - scrollY;

                            g.PushTranslation(0, y);
                            g.FillAndDrawRectangle(0, 0, Width - whiteKeySizeX, noteSizeY, g.GetVerticalGradientBrush(mapping.Sample.Color, noteSizeY, 0.8f), theme.BlackBrush, 1);
                            if (mapping.Sample != null)
                            {
                                string text = $"{mapping.Sample.Name} - Pitch: {DPCMSampleRate.Strings[App.PalPlayback ? 1 : 0][mapping.Pitch]}";
                                if (mapping.Loop) text += ", Looping";
                                g.DrawText(text, ThemeBase.FontSmall, dpcmTextPosX, dpcmTextPosY, theme.BlackBrush);
                            }
                            g.PopTransform();
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
                            g.PushTranslation(0, y);
                            g.FillAndDrawRectangle(0, 0, Width - whiteKeySizeX, noteSizeY, g.GetVerticalGradientBrush(dragSample.Color, noteSizeY, 0.8f), theme.WhiteBrush, 2);
                            g.PopTransform();
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

                                g.PushTranslation(0, y);
                                g.DrawRectangle(0, 0, Width - whiteKeySizeX, noteSizeY, theme.WhiteBrush, 2);
                                g.PopTransform();
                            }
                        }
                    }

                    g.DrawText($"Editing DPCM Samples Instrument ({App.Project.GetTotalMappedSampleSize()} / {Project.MaxMappedSampleSize} Bytes)", ThemeBase.FontBig, bigTextPosX, bigTextPosY, whiteKeyBrush);
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
                    g.DrawLine(0, y, env.Length * noteSizeX - scrollX, y, theme.DarkGreyLineBrush1, (value % spacing) == 0 ? 3 : 1);
                }

                DrawSelectionRect(g, Height);

                // Draw the vertical bars.
                for (int b = 0; b < env.Length; b++)
                {
                    int x = b * noteSizeX - scrollX;
                    if (b != 0) g.DrawLine(x, 0, x, Height, theme.DarkGreyLineBrush1);
                }

                if (env.Loop >= 0)
                    g.DrawLine(env.Loop * noteSizeX - scrollX, 0, env.Loop * noteSizeX - scrollX, Height, theme.BlackBrush);
                if (env.Release >= 0)
                    g.DrawLine(env.Release * noteSizeX - scrollX, 0, env.Release * noteSizeX - scrollX, Height, theme.BlackBrush);
                if (env.Length > 0)
                    g.DrawLine(env.Length * noteSizeX - scrollX, 0, env.Length * noteSizeX - scrollX, Height, theme.BlackBrush);

                if ((editMode == EditionMode.Enveloppe || editMode == EditionMode.Arpeggio) && editEnvelope < EnvelopeType.RegularCount)
                {
                    var seekFrame = App.GetEnvelopeFrame(editInstrument, editEnvelope, editMode == EditionMode.Arpeggio);
                    if (seekFrame >= 0)
                    {
                        var seekX = seekFrame * noteSizeX - scrollX;
                        g.DrawLine(seekX, 0, seekX, Height, GetSeekBarBrush(), 3);
                    }
                }

                if (editEnvelope == EnvelopeType.Arpeggio)
                {
                    for (int i = 0; i < env.Length; i++)
                    {
                        var x = i * noteSizeX - scrollX;
                        var y = (virtualSizeY - envelopeSizeY * (env.Values[i] + midValue)) - scrollY;
                        var selected = IsEnvelopeValueSelected(i);
                        g.FillRectangle(x, y - envelopeSizeY, x + noteSizeX, y, g.GetVerticalGradientBrush(ThemeBase.LightGreyFillColor1, (int)envelopeSizeY, 0.8f));
                        g.DrawRectangle(x, y - envelopeSizeY, x + noteSizeX, y, theme.BlackBrush, selected ? 2 : 1);
                        if (zoomLevel >= 1)
                            g.DrawText(env.Values[i].ToString("+#;-#;0"), ThemeBase.FontSmallCenter, x, y - envelopeSizeY - effectValuePosTextOffsetY, theme.LightGreyFillBrush1, noteSizeX);
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

                        var x = i * noteSizeX - scrollX;
                        var selected = IsEnvelopeValueSelected(i);

                        g.FillRectangle(x, y0, x + noteSizeX, y1, theme.LightGreyFillBrush1);
                        g.DrawRectangle(x, y0, x + noteSizeX, y1, theme.BlackBrush, selected ? 2 : 1);

                        if (zoomLevel >= 1)
                        {
                            bool drawOutside = Math.Abs(y1 - y0) < (DefaultEnvelopeSizeY * RenderTheme.MainWindowScaling * 2);
                            var brush = drawOutside ? theme.LightGreyFillBrush1 : theme.BlackBrush;
                            var offset = drawOutside != val < center ? -effectValuePosTextOffsetY : effectValueNegTextOffsetY;

                            g.DrawText(val.ToString(), ThemeBase.FontSmallCenter, x, ty + offset, brush, noteSizeX);
                        }
                    }
                }

                if (editMode == EditionMode.Enveloppe)
                {
                    var envelopeString = EnvelopeType.Names[editEnvelope];

                    if (editEnvelope == EnvelopeType.Pitch)
                        envelopeString = (editInstrument.Envelopes[editEnvelope].Relative ? "Relative " : "Absolute ") + envelopeString;

                    g.DrawText($"Editing Instrument {editInstrument.Name} ({envelopeString})", ThemeBase.FontBig, bigTextPosX, bigTextPosY, whiteKeyBrush);
                }
                else
                {
                    g.DrawText($"Editing Arpeggio {editArpeggio.Name}", ThemeBase.FontBig, bigTextPosX, bigTextPosY, whiteKeyBrush);
                }
            }

            g.PopClip();
            g.PopTransform();

            if (!string.IsNullOrEmpty(noteTooltip) && editMode != EditionMode.DPCM)
            {
                g.DrawText(noteTooltip, ThemeBase.FontMediumBigRight, 0, Height - tooltipTextPosY - scrollBarThickness, whiteKeyBrush, Width - tooltipTextPosX);
            }
        }

        private void RenderNoteBody(RenderGraphics g, Note note, Color color, int time, int noteLen, bool outline, bool selected, bool activeChannel, bool released, bool isFirstPart, int slideDuration = -1)
        {
            int x = time * noteSizeX - scrollX;
            int y = virtualSizeY - note.Value * noteSizeY - scrollY;
            int sy = released ? releaseNoteSizeY : noteSizeY;

            if (!outline && isFirstPart && slideDuration >= 0)
            {
                // We will get zero for notes that start a slide and have an immediate delayed cut.
                int duration = Math.Max(1, slideDuration);
                int slideSizeX = duration;
                int slideSizeY = note.SlideNoteTarget - note.Value;

                g.PushTransform(x, y + (slideSizeY > 0 ? 0 : noteSizeY), slideSizeX, -slideSizeY);
                g.FillGeometry(slideNoteGeometry[zoomLevel - MinZoomLevel], g.GetSolidBrush(color, 1.0f, 0.2f), true);
                g.PopTransform();
            }

            if (released)
                y += noteSizeY / 2 - releaseNoteSizeY / 2;

            g.PushTranslation(x, y);

            int sx = noteLen * noteSizeX;
            int noteTextPosX = attackIconPosX;

            if (!outline)
                g.FillRectangle(0, 0, sx, sy, g.GetVerticalGradientBrush(color, sy, 0.8f));

            g.DrawRectangle(0, 0, sx, sy, outline ? hoverNoteBrush : (selected ? selectionNoteBrush : theme.BlackBrush), selected || outline ? 2 : 1);

            if (!outline)
            {
                if (activeChannel)
                {
                    if (isFirstPart && note.HasAttack && sx > noteAttackSizeX + 4)
                    {
                        g.FillRectangle(attackIconPosX, attackIconPosX, attackIconPosX + noteAttackSizeX, sy - attackIconPosX + 1, attackBrush);
                        noteTextPosX += noteAttackSizeX + attackIconPosX;
                    }

                    if (Settings.ShowNoteLabels && !released && editMode == EditionMode.Channel && note.IsMusical && sx > minNoteSizeForText)
                    {
                        g.DrawText(note.FriendlyName, ThemeBase.FontSmall, noteTextPosX, noteTextPosY, theme.BlackBrush);
                    }
                }

                if (note.Arpeggio != null)
                {
                    var offsets = note.Arpeggio.GetChordOffsets();
                    foreach (var offset in offsets)
                    {
                        g.PushTranslation(0, offset * -noteSizeY);
                        g.FillRectangle(0, 1, sx, sy, g.GetSolidBrush(note.Arpeggio.Color, 1.0f, 0.2f));
                        g.PopTransform();
                    }
                }
            }

            g.PopTransform();
        }

        private void RenderNoteReleaseOrStop(RenderGraphics g, Note note, Color color, int time, int value, bool outline, bool selected, bool stop, bool released)
        {
            int x = time * noteSizeX - scrollX;
            int y = virtualSizeY - value * noteSizeY - scrollY;

            var paths = stop ? (released ? stopReleaseNoteGeometry : stopNoteGeometry) : releaseNoteGeometry;

            g.PushTranslation(x, y);
            
            if (!outline)
                g.FillGeometry(paths[zoomLevel - MinZoomLevel, 0], g.GetVerticalGradientBrush(color, noteSizeY, 0.8f));
            g.DrawGeometry(paths[zoomLevel - MinZoomLevel, 0], outline ? hoverNoteBrush : (selected ? selectionNoteBrush : theme.BlackBrush), outline || selected ? 2 : 1);

            if (!outline && note.Arpeggio != null)
            {
                var offsets = note.Arpeggio.GetChordOffsets();
                foreach (var offset in offsets)
                {
                    g.PushTranslation(0, offset * -noteSizeY);
                    g.FillGeometry(paths[zoomLevel - MinZoomLevel, 1], g.GetSolidBrush(note.Arpeggio.Color, 1.0f, 0.2f), true);
                    g.PopTransform();
                }
            }

            g.PopTransform();
        }

        private void RenderNote(RenderGraphics g, NoteLocation location, Note note, Song song, Channel channel, int distanceToNextNote, bool drawImplicityStopNotes, bool isActiveChannel, bool hover, bool released)
        {
            Debug.Assert(note.IsMusical);

            if (distanceToNextNote < 0)
                distanceToNextNote = (int)ushort.MaxValue;

            var absoluteIndex = location.ToAbsoluteNoteIndex(Song);
            var nextAbsoluteIndex = absoluteIndex + distanceToNextNote;
            var duration = Math.Min(distanceToNextNote, note.Duration);
            var slideDuration = note.IsSlideNote ? channel.GetSlideNoteDuration(note, location) : -1;
            var color = GetNoteColor(channel, note.Value, note.Instrument, song.Project);
            var selected = isActiveChannel && IsNoteSelected(location, duration);

            if (!isActiveChannel)
                color = Color.FromArgb((int)(color.A * 0.2f), color);

            // Draw first part, from start to release point.
            if (note.HasRelease)
            {
                RenderNoteBody(g, note, color, absoluteIndex, Math.Min(note.Release, duration), hover, selected, isActiveChannel, released, true, slideDuration);
                absoluteIndex += note.Release;
                duration -= note.Release;

                if (duration > 0)
                {
                    RenderNoteReleaseOrStop(g, note, color, absoluteIndex, note.Value, hover, selected, false, released);
                    absoluteIndex++;
                    duration--;
                }

                released = true;
            }

            // Then second part, after release to stop note.
            if (duration > 0)
            {
                RenderNoteBody(g, note, color, absoluteIndex, duration, hover, selected, isActiveChannel, released, !note.HasRelease, slideDuration);
                absoluteIndex += duration;
                duration -= duration;

                if (drawImplicityStopNotes && absoluteIndex < nextAbsoluteIndex && !hover)
                {
                    RenderNoteReleaseOrStop(g, note, Color.FromArgb(128, color), absoluteIndex, note.Value, hover, selected, true, released);
                }
            }
        }

        private float GetPixelForWaveTime(float time, int scroll = 0)
        {
            var viewSize = Width - whiteKeySizeX;
            var viewTime = DefaultZoomWaveTime * (float)Math.Pow(2, -zoomLevel);

            return time / viewTime * viewSize - scroll;
        }

        private float GetWaveTimeForPixel(int x)
        {
            var viewSize = Width - whiteKeySizeX;
            var viewTime = DefaultZoomWaveTime * (float)Math.Pow(2, -zoomLevel);

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

        private void RenderWave(RenderGraphics g, RenderArea a, short[] data, float rate, RenderBrush brush, bool isSource, bool drawSamples)
        {
            var viewWidth     = Width - whiteKeySizeX;
            var halfHeight    = (Height - headerAndEffectSizeY) / 2;
            var halfHeightPad = halfHeight - waveDisplayPaddingY;
            var viewTime      = DefaultZoomWaveTime * (float)Math.Pow(2, -zoomLevel);

            var unclampedMinVisibleSample = (int)Math.Floor  (a.minVisibleWaveTime * rate);
            var unclampedMaxVisibleSample = (int)Math.Ceiling(a.maxVisibleWaveTime * rate);
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

                    // Direct2D doesn't have a way to drawing lines with more than 2 points. Using a temporary 
                    // geometry is ugly, but still 6x faster than manually drawing each line segment.
                    RenderGeometry geo = g.CreateGeometry(points, false);

                    g.AntiAliasing = true;
                    g.DrawGeometry(geo, brush);
                    g.AntiAliasing = false;
                    geo.Dispose();

                    if (drawSamples)
                    {
                        var selectionValid = IsSelectionValid();

                        for (int i = 0; i < points.GetLength(0); i++)
                        {
                            var selected = isSource && selectionValid && indices[i] >= selectionMin && indices[i] <= selectionMax;
                            var sampleScale = selected ? 1.5f : 1.0f;

                            g.PushTransform(points[i, 0], points[i, 1], sampleScale, sampleScale);
                            g.FillGeometry(sampleGeometry, selected ? theme.WhiteBrush : brush);
                            g.PopTransform();
                        }
                    }
                }
            }
        }

        private void RenderDmc(RenderGraphics g, RenderArea a, byte[] data, float rate, float baseTime, RenderBrush brush, bool isSource, bool drawSamples)
        {
            var viewWidth     = Width - whiteKeySizeX;
            var realHeight    = Height - headerAndEffectSizeY;
            var halfHeight    = realHeight / 2;
            var halfHeightPad = halfHeight - waveDisplayPaddingY;
            var viewTime      = DefaultZoomWaveTime * (float)Math.Pow(2, -zoomLevel);

            var unclampedMinVisibleSample = (int)Math.Floor  ((a.minVisibleWaveTime - baseTime) * rate);
            var unclampedMaxVisibleSample = (int)Math.Ceiling((a.maxVisibleWaveTime - baseTime) * rate);
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

                    var dpcmCounter = NesApu.DACDefaultValueDiv2;

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

                    // Direct2D doesn't have a way to drawing lines with more than 2 points. Using a temporary 
                    // geometry is ugly, but still 6x faster than manually drawing each line segment.
                    RenderGeometry geo = g.CreateGeometry(points, false);

                    g.AntiAliasing = true;
                    g.DrawGeometry(geo, brush);
                    g.AntiAliasing = false;
                    geo.Dispose();

                    if (drawSamples)
                    {
                        var selectionValid = IsSelectionValid();

                        for (int i = 0; i < points.GetLength(0); i++)
                        {
                            var selected = isSource && selectionValid && indices[i] >= selectionMin && indices[i] <= selectionMax;
                            var sampleScale = selected ? 1.5f : 1.0f;

                            g.PushTransform(points[i, 0], points[i, 1], sampleScale, sampleScale);
                            g.FillGeometry(sampleGeometry, selected ? theme.WhiteBrush : brush);
                            g.PopTransform();
                        }
                    }
                }
            }
        }

        private void RenderWaveform(RenderGraphics g, RenderArea a)
        {
            if (editMode != EditionMode.DPCM)
                return;

            g.PushTranslation(whiteKeySizeX, headerAndEffectSizeY);
            g.PushClip(0, 0, Width, Height);

            // Source data range.
            g.FillRectangle(
                GetPixelForWaveTime(0, scrollX), 0,
                GetPixelForWaveTime(editSample.SourceDuration, scrollX), Height, theme.DarkGreyFillBrush1);

            // Horizontal center line
            var sizeY   = Height - headerAndEffectSizeY;
            var centerY = sizeY * 0.5f;
            g.DrawLine(0, centerY, Width, centerY, theme.BlackBrush);

            // Top/bottom dash lines (limits);
            var topY    = waveDisplayPaddingY;
            var bottomY = (Height - headerAndEffectSizeY) - waveDisplayPaddingY;
            g.DrawLine(0, topY,    Width, topY,    dashedHorizontalLineBrush);
            g.DrawLine(0, bottomY, Width, bottomY, dashedHorizontalLineBrush);

            // Vertical lines (1.0, 0.1, 0.01 seconds)
            ForEachWaveTimecode(g, a, (time, x, level, idx) =>
            {
                var modSeconds = Utils.IntegerPow(10, level + 1);
                var modTenths  = Utils.IntegerPow(10, level);

                var brush = dashedVerticalLineBrush;

                if ((idx % modSeconds) == 0)
                    brush = theme.BlackBrush;
                else if ((idx % modTenths) == 0)
                    brush = theme.DarkGreyLineBrush1;

                g.DrawLine(x, 0, x, Height, brush, 1.0f);
            });

            // Selection rectangle
            if (IsSelectionValid())
            {
                g.FillRectangle(
                    GetPixelForWaveTime(GetWaveTimeForSample(selectionMin, true),  scrollX), 0, 
                    GetPixelForWaveTime(GetWaveTimeForSample(selectionMax, false), scrollX), Height, selectionBgVisibleBrush);
            }

            // TODO: Make this a constants.
            bool showSamples = zoomLevel > 5;

            // Source waveform
            if (editSample.SourceDataIsWav)
            {
                RenderWave(g, a, editSample.SourceWavData.Samples, editSample.SourceSampleRate, theme.LightGreyFillBrush1, true, showSamples);
            }
            else
            {
                RenderDmc(g, a, editSample.SourceDmcData.Data, editSample.SourceSampleRate, 0.0f, theme.LightGreyFillBrush1, true, showSamples); 
            }

            // Processed waveform
            var processedBrush = g.GetSolidBrush(editSample.Color);
            RenderDmc(g, a, editSample.ProcessedData, editSample.ProcessedSampleRate, editSample.ProcessedStartTime, processedBrush, false, showSamples);

            // Play position
            var playPosition = App.PreviewDPCMWavPosition;

            if (playPosition >= 0 && App.PreviewDPCMSampleId == editSample.Id)
            {
                var playTime = playPosition / (float)App.PreviewDPCMSampleRate;
                if (!App.PreviewDPCMIsSource)
                    playTime += editSample.ProcessedStartTime;
                var seekX = GetPixelForWaveTime(playTime, scrollX);
                g.DrawLine(seekX, 0, seekX, Height, App.PreviewDPCMIsSource ? theme.LightGreyFillBrush1 : processedBrush, 3);
            }

            // Title + source/processed info.
            g.DrawText($"Editing DPCM Sample {editSample.Name}", ThemeBase.FontBig, bigTextPosX, bigTextPosY, whiteKeyBrush);
            g.DrawText($"Source Data ({(editSample.SourceDataIsWav ? "WAV" : "DMC")}) : {editSample.SourceSampleRate} Hz, {editSample.SourceDataSize} Bytes, {(int)(editSample.SourceDuration * 1000)} ms", ThemeBase.FontMedium, bigTextPosX, dpcmSourceDataPosY, whiteKeyBrush);
            g.DrawText($"Processed Data (DMC) : {editSample.ProcessedSampleRate} Hz, {editSample.ProcessedData.Length} Bytes, {(int)(editSample.ProcessedDuration * 1000)} ms", ThemeBase.FontMedium, bigTextPosX, dpcmSourceDataPosY + dpcmInfoSpacingY, whiteKeyBrush);
            g.DrawText($"Preview Playback : {editSample.GetPlaybackSampleRate(App.PalPlayback)} Hz, {(int)(editSample.GetPlaybackDuration(App.PalPlayback) * 1000)} ms", ThemeBase.FontMedium, bigTextPosX, dpcmSourceDataPosY + dpcmInfoSpacingY * 2, whiteKeyBrush);

            g.PopClip();
            g.PopTransform();

            if (!string.IsNullOrEmpty(noteTooltip))
            {
                g.DrawText(noteTooltip, ThemeBase.FontMediumBigRight, 0, Height - tooltipTextPosY, whiteKeyBrush, Width - tooltipTextPosX);
            }
        }

        public void RenderVideoFrame(RenderGraphics g, int channel, int patternIndex, float noteIndex, float centerNote, int highlightKey, Color highlightColor)
        {
            Debug.Assert(editMode == EditionMode.VideoRecording);

            int noteY = (int)Math.Round(virtualSizeY - centerNote * noteSizeY + noteSizeY / 2);

            editChannel = channel;
            scrollX = (int)Math.Round((Song.GetPatternStartAbsoluteNoteIndex(patternIndex) + noteIndex) * noteSizeX);
            scrollY = noteY - (Height - headerAndEffectSizeY) / 2;
            playingNote = highlightKey;
            videoKeyColor = highlightColor;

            Utils.DisposeAndNullify(ref whiteKeyPressedBrush);
            Utils.DisposeAndNullify(ref blackKeyPressedBrush);
            whiteKeyPressedBrush = g.CreateSolidBrush(highlightColor);
            blackKeyPressedBrush = g.CreateSolidBrush(highlightColor);

            OnRender(g);
        }

        private void RenderScrollBars(RenderGraphics g, RenderArea a)
        {
            if (Settings.ShowScrollBars && editMode != EditionMode.VideoRecording)
            {
                bool h = false;
                bool v = false;

                if (GetScrollBarParams(true, out var scrollBarPosX, out var scrollBarSizeX))
                {
                    g.PushTranslation(whiteKeySizeX - 1, 0);
                    g.FillAndDrawRectangle(0, Height - scrollBarThickness, Width + 1, Height - 1, theme.DarkGreyFillBrush1, theme.BlackBrush);
                    g.FillAndDrawRectangle(scrollBarPosX, Height - scrollBarThickness, scrollBarPosX + scrollBarSizeX + 1, Height - 1, theme.MediumGreyFillBrush1, theme.BlackBrush);
                    g.PopTransform();
                    h = true;
                }

                if (GetScrollBarParams(false, out var scrollBarPosY, out var scrollBarSizeY))
                {
                    g.PushTranslation(0, headerAndEffectSizeY - 1);
                    g.FillAndDrawRectangle(Width - scrollBarThickness + 1, 0, Width, Height, theme.DarkGreyFillBrush1, theme.BlackBrush);
                    g.FillAndDrawRectangle(Width - scrollBarThickness + 1, scrollBarPosY, Width, scrollBarPosY + scrollBarSizeY + 1, theme.MediumGreyFillBrush1, theme.BlackBrush);
                    g.PopTransform();
                    v = true;
                }

                // Hide the glitchy area where both scroll bars intersect with a little square.
                if (h && v)
                {
                    g.FillAndDrawRectangle(Width - scrollBarThickness + 1, Height - scrollBarThickness, Width, Height - 1, theme.DarkGreyFillBrush1, theme.BlackBrush);
                }
            }
        }

        protected override void OnRender(RenderGraphics g)
        {
            var a = new RenderArea();

            var minVisibleNoteIdx = Math.Max((int)Math.Floor(scrollX / (float)noteSizeX), 0);
            var maxVisibleNoteIdx = Math.Min((int)Math.Ceiling((scrollX + Width) / (float)noteSizeX), Song.GetPatternStartAbsoluteNoteIndex(Song.Length));

            a.maxVisibleNote = numNotes - Utils.Clamp((int)Math.Floor(scrollY / (float)noteSizeY), 0, numNotes);
            a.minVisibleNote = numNotes - Utils.Clamp((int)Math.Ceiling((scrollY + Height - headerAndEffectSizeY) / (float)noteSizeY), 0, numNotes);
            a.maxVisibleOctave = (int)Math.Ceiling(a.maxVisibleNote / 12.0f);
            a.minVisibleOctave = (int)Math.Floor(a.minVisibleNote / 12.0f);
            a.minVisiblePattern = Utils.Clamp(Song.PatternIndexFromAbsoluteNoteIndex(minVisibleNoteIdx) + 0, 0, Song.Length);
            a.maxVisiblePattern = Utils.Clamp(Song.PatternIndexFromAbsoluteNoteIndex(maxVisibleNoteIdx) + 1, 0, Song.Length);

            if (editMode == EditionMode.DPCM)
            {
                a.minVisibleWaveTime = GetWaveTimeForPixel(0);
                a.maxVisibleWaveTime = GetWaveTimeForPixel(Width - whiteKeySizeX);
            }

            RenderHeader(g, a);
            RenderEffectList(g);
            RenderEffectPanel(g, a);
            RenderPiano(g, a);
            RenderNotes(g, a);
            RenderWaveform(g, a);
            RenderScrollBars(g, a);
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
            int length = (int)Math.Round((e.X - whiteKeySizeX + scrollX) / (double)noteSizeX);

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
            ConditionalInvalidate();
        }

        void StartChangeEffectValue(MouseEventArgs e, NoteLocation location)
        {
            var channel   = Song.Channels[editChannel];
            var pattern   = channel.PatternInstances[location.PatternIndex];
            var note      = channel.GetNoteAt(location);
            var selection = IsSelectionValid() && IsNoteSelected(location) && note != null && note.HasValidEffectValue(selectedEffectIdx);

            StartCaptureOperation(e, selection ? CaptureOperation.ChangeSelectionEffectValue : CaptureOperation.ChangeEffectValue, false, location.ToAbsoluteNoteIndex(Song));

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

            captureEffectValue = note != null && note.HasValidEffectValue(selectedEffectIdx) ? note.GetEffectValue(selectedEffectIdx) : int.MinValue;

            UpdateChangeEffectValue(e);
        }

        void UpdateChangeEffectValue(MouseEventArgs e)
        {
            Debug.Assert(selectedEffectIdx >= 0);

            if (captureOperation == CaptureOperation.ChangeSelectionEffectValue)
                App.UndoRedoManager.RestoreTransaction(false);

            bool shift = ModifierKeys.HasFlag(Keys.Shift);

            var channel = Song.Channels[editChannel];
            var pattern = channel.PatternInstances[captureNoteLocation.PatternIndex];
            var minValue = Note.GetEffectMinValue(Song, channel, selectedEffectIdx);
            var maxValue = Note.GetEffectMaxValue(Song, channel, selectedEffectIdx);

            var note = pattern.GetOrCreateNoteAt(captureNoteLocation.NoteIndex);

            int newValue;
            if (shift)
            {
                if (!note.HasValidEffectValue(selectedEffectIdx))
                    note.SetEffectValue(selectedEffectIdx, Note.GetEffectDefaultValue(Song, selectedEffectIdx));

                newValue = Utils.Clamp(note.GetEffectValue(selectedEffectIdx) + (mouseLastY - e.Y), minValue, maxValue);
            }
            else
            {
                var ratio = Utils.Clamp(1.0f - (e.Y - headerSizeY) / (float)effectPanelSizeY, 0.0f, 1.0f);
                newValue = (int)Math.Round(ratio * (maxValue - minValue) + minValue);
            }

            if (captureOperation == CaptureOperation.ChangeSelectionEffectValue)
            {
                var effectDelta = newValue - captureEffectValue;
                var minLocation = NoteLocation.FromAbsoluteNoteIndex(Song, selectionMin);
                var maxLocation = NoteLocation.FromAbsoluteNoteIndex(Song, selectionMax);

                for (var it = channel.GetSparseNoteIterator(minLocation, maxLocation, Note.GetFilterForEffect(selectedEffectIdx)); !it.Done; it.Next())
                {
                    var value = it.Note.GetEffectValue(selectedEffectIdx);
                    it.Note.SetEffectValue(selectedEffectIdx, value + effectDelta);
                }

                channel.InvalidateCumulativePatternCache(minLocation.PatternIndex);
            }
            else
            {
                note.SetEffectValue(selectedEffectIdx, newValue);
                channel.InvalidateCumulativePatternCache(captureNoteLocation.PatternIndex);
            }

            ConditionalInvalidate();
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

                ConditionalInvalidate();
            }
        }

        private void UpdateWavePreset(bool final)
        {
            if (editMode == EditionMode.Enveloppe)
            {
                if (editInstrument.ExpansionType == ExpansionType.Fds)
                {
                    if (editEnvelope == EnvelopeType.FdsWaveform)
                        editInstrument.FdsWavePreset = WavePresetType.Custom;
                    if (editEnvelope == EnvelopeType.FdsModulation)
                        editInstrument.FdsModPreset = WavePresetType.Custom;
                }
                else if (editInstrument.ExpansionType == ExpansionType.N163)
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

        protected void PlayPiano(int x, int y)
        {
            y -= headerAndEffectSizeY;

            for (int i = 0; i < numOctaves; i++)
            {
                for (int j = 0; j < 12 && i * 12 + j < numNotes; j++)
                {
                    if (IsBlackKey(j) && PointInRectangle(GetKeyRectangle(i, j), x, y))
                    {
                        int note = i * 12 + j + 1;
                        if (note != playingNote)
                        {
                            playingNote = note;
                            App.PlayInstrumentNote(playingNote, true, true);
                            ConditionalInvalidate();
                        }
                        return;
                    }
                }
                for (int j = 0; j < 12 && i * 12 + j < numNotes; j++)
                {
                    if (!IsBlackKey(j) && PointInRectangle(GetKeyRectangle(i, j), x, y))
                    {
                        int note = i * 12 + j + 1;
                        if (note != playingNote)
                        {
                            playingNote = note;
                            App.PlayInstrumentNote(playingNote, true, true);
                            ConditionalInvalidate();
                        }
                        return;
                    }
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
                        var freqIdx = App.PalPlayback ? 1 : 0;
                        var dlg = new PropertyDialog(PointToScreen(new Point(e.X, e.Y)), 160, false, e.Y > Height / 2);
                        dlg.Properties.AddDropDownList("Pitch :", DPCMSampleRate.Strings[freqIdx], DPCMSampleRate.Strings[freqIdx][mapping.Pitch]); // 0
                        dlg.Properties.AddCheckBox("Loop :", mapping.Loop); // 1
                        dlg.Properties.Build();

                        if (dlg.ShowDialog(ParentForm) == DialogResult.OK)
                        {
                            App.UndoRedoManager.BeginTransaction(TransactionScope.DPCMSamplesMapping);
                            mapping.Pitch = DPCMSampleRate.GetIndexForName(App.PalPlayback, dlg.Properties.GetPropertyValue<string>(0));
                            mapping.Loop = dlg.Properties.GetPropertyValue<bool>(1);
                            App.UndoRedoManager.EndTransaction();
                            ConditionalInvalidate();
                        }
                    }
                }
            }
            else if (right && editMode == EditionMode.Channel)
            {
                if (IsMouseInHeader(e.X, e.Y))
                {
                    int patIdx = Song.PatternIndexFromAbsoluteNoteIndex((int)Math.Floor((e.X - whiteKeySizeX + scrollX) / (float)noteSizeX));
                    if (patIdx >= 0 && patIdx < Song.Length)
                        SetSelection(Song.GetPatternStartAbsoluteNoteIndex(patIdx), Song.GetPatternStartAbsoluteNoteIndex(patIdx + 1) - 1);
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
                ConditionalInvalidate();
            }
        }

        private void CaptureMouse(MouseEventArgs e)
        {
            mouseLastX = e.X;
            mouseLastY = e.Y;
            captureMouseX = e.X;
            captureMouseY = e.Y;
            captureScrollX = scrollX;
            captureScrollY = scrollY;
            Capture = true;
        }

        private void StartCaptureOperation(MouseEventArgs e, CaptureOperation op, bool allowSnap = false, int noteIdx = -1)
        {
#if DEBUG
            Debug.Assert(captureOperation == CaptureOperation.None);
#else
            if (captureOperation != CaptureOperation.None)
                AbortCaptureOperation();
#endif

            CaptureMouse(e);
            captureOperation = op;
            captureThresholdMet = !captureNeedsThreshold[(int)op];
            captureRealTimeUpdate = captureWantsRealTimeUpdate[(int)op];
            captureWaveTime = editMode == EditionMode.DPCM ? GetWaveTimeForPixel(e.X - whiteKeySizeX) : 0.0f;
            captureNoteValue = numNotes - Utils.Clamp((e.Y + scrollY - headerAndEffectSizeY) / noteSizeY, 0, numNotes);
            captureSelectionMin = selectionMin;
            captureSelectionMax = selectionMax;

            captureMouseAbsoluteIdx = (e.X - whiteKeySizeX + scrollX) / noteSizeX;
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
            ConditionalInvalidate();
        }

        private void UpdateScrollBarY(MouseEventArgs e)
        {
            GetScrollBarParams(false, out _, out var scrollBarSizeY);
            GetMinMaxScroll(out _, out _, out _, out var maxScrollY);

            int scrollAreaSizeY = Height - headerAndEffectSizeY;
            scrollY = (int)Math.Round(captureScrollY + ((e.Y - captureMouseY) / (float)(scrollAreaSizeY - scrollBarSizeY) * maxScrollY));

            ClampScroll();
            ConditionalInvalidate();
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

        private void EndDragDPCMSampleMapping(MouseEventArgs e)
        {
            bool success = false;
            if (GetNoteValueForCoord(e.X, e.Y, out var noteValue) && App.Project.NoteSupportsDPCM(noteValue) && noteValue != captureNoteValue && draggedSample != null)
            {
                App.Project.UnmapDPCMSample(noteValue);
                App.Project.MapDPCMSample(noteValue, draggedSample.Sample, draggedSample.Pitch, draggedSample.Loop);
                DPCMSampleMapped?.Invoke(noteValue);
                success = true;
            }

            if (success)
            {
                if (PlatformUtils.MessageBox($"Do you want to transpose all the notes using this sample?", "Remap DPCM Sample", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    App.Project.TransposeDPCMMapping(captureNoteValue, noteValue);
                }

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

                ConditionalInvalidate();
            }
        }

        private void AbortCaptureOperation()
        {
            if (captureOperation != CaptureOperation.None)
            {
                if (App.UndoRedoManager.HasTransactionInProgress)
                    App.UndoRedoManager.AbortTransaction();

                ConditionalInvalidate();
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

            channel.InvalidateCumulativePatternCache(minLocation.PatternIndex);

            if (doTransaction)
                App.UndoRedoManager.EndTransaction();

            ConditionalInvalidate();
        }

        private void TransformEnvelopeValues(int startFrameIdx, int endFrameIdx, Func<sbyte, int, sbyte> function)
        {
            if (editMode == EditionMode.Arpeggio)
                App.UndoRedoManager.BeginTransaction(TransactionScope.Arpeggio, editArpeggio.Id);
            else
                App.UndoRedoManager.BeginTransaction(TransactionScope.Instrument, editInstrument.Id);

            Envelope.GetMinMaxValue(editInstrument, editEnvelope, out int minVal, out int maxVal);

            for (int i = startFrameIdx; i <= endFrameIdx; i++)
                EditEnvelope.Values[i] = (sbyte)Utils.Clamp(function(EditEnvelope.Values[i], i - startFrameIdx), minVal, maxVal);

            UpdateWavePreset(false);
            App.UndoRedoManager.EndTransaction();
            ConditionalInvalidate();
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
                        note.Clear();
                    else
                        note.Value = (byte)value;

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
                ConditionalInvalidate();
            }
        }

#if FAMISTUDIO_WINDOWS
        public void UnfocusedKeyDown(KeyEventArgs e)
        {
            OnKeyDown(e);
        }
#endif

        protected override void OnKeyDown(KeyEventArgs e)
        {
            UpdateCursor();

            if (captureOperation != CaptureOperation.None)
                return;

            bool ctrl  = ModifierKeys.HasFlag(Keys.Control);
            bool shift = ModifierKeys.HasFlag(Keys.Shift);

            if (e.KeyCode == Keys.Escape)
            {
                ClearSelection();
                ConditionalInvalidate();
            }
            else if (e.KeyCode == Keys.Oem3 && (editMode == EditionMode.Channel || editMode == EditionMode.DPCM))
            {
                ToggleEffectPannel();
            }
            else if (e.KeyCode == Keys.S && ModifierKeys.HasFlag(Keys.Shift))
            {
                if (IsSnappingAllowed)
                {
                    snap = !snap;
                    ConditionalInvalidate();
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
            var seekX = App.CurrentFrame * noteSizeX - scrollX;
            var minX = 0;
            var maxX = (int)((Width * percent) - whiteKeySizeX);

            // Keep everything visible 
            if (seekX < minX)
                scrollX -= (minX - seekX);
            else if (seekX > maxX)
                scrollX += (seekX - maxX);

            ClampScroll();

            seekX = App.CurrentFrame * noteSizeX - scrollX;
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

                ConditionalInvalidate();
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

                ConditionalInvalidate();
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
                channel.InvalidateCumulativePatternCache(location.PatternIndex);
                PatternChanged?.Invoke(pattern);

                AdvanceRecording(currentFrame);

                App.UndoRedoManager.EndTransaction();

                ConditionalInvalidate();
            }
        }

        private void ShowInstrumentError()
        {
            App.DisplayWarning("Selected instrument is incompatible with channel!");
        }

        private void ToggleEffectPannel()
        {
            Debug.Assert(editMode == EditionMode.Channel || editMode == EditionMode.DPCM);
            showEffectsPanel = !showEffectsPanel;
            UpdateRenderCoords();
            ClampScroll();
            ConditionalInvalidate();
        }

        private bool HandleMouseDownPan(MouseEventArgs e)
        {
            bool middle = e.Button.HasFlag(MouseButtons.Middle) || (e.Button.HasFlag(MouseButtons.Left) && ModifierKeys.HasFlag(Keys.Alt));

            if (middle && e.Y > headerSizeY && e.X > whiteKeySizeX)
            {
                panning = true;
                CaptureMouse(e);
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
                        ConditionalInvalidate();
                    }
                    else if (x > (scrollBarPosX + scrollBarSizeX))
                    {
                        scrollX += (Width - whiteKeySizeX);
                        ClampScroll();
                        ConditionalInvalidate();
                    }
                    else if (x >= scrollBarPosX && x <= (scrollBarPosX + scrollBarSizeX))
                    {
                        StartCaptureOperation(e, CaptureOperation.ScrollBarX);
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
                        ConditionalInvalidate();
                    }
                    else if (y > (scrollBarPosY + scrollBarSizeY))
                    {
                        scrollX += (Height - headerAndEffectSizeY);
                        ClampScroll();
                        ConditionalInvalidate();
                    }
                    else if (y >= scrollBarPosY && y <= (scrollBarPosY + scrollBarSizeY))
                    {
                        StartCaptureOperation(e, CaptureOperation.ScrollBarY);
                    }
                    return true;
                }
            }

            return false;
        }

        private bool HandleMouseDownPiano(MouseEventArgs e)
        {
            if (e.Button.HasFlag(MouseButtons.Left) && IsMouseInPiano(e.X, e.Y))
            {
                StartPlayPiano(e);
                return true;
            }

            return false;
        }

        private bool HandleMouseDownSeekBar(MouseEventArgs e)
        {
            if (e.Button.HasFlag(MouseButtons.Left) && IsMouseInHeader(e.X, e.Y))
            {
                StartCaptureOperation(e, CaptureOperation.DragSeekBar);
                UpdateSeekDrag(e, false);
                return true;
            }

            return false;
        }

        private bool HandleMouseDownHeaderSelection(MouseEventArgs e)
        {
            if (e.Button.HasFlag(MouseButtons.Right) && IsMouseInHeader(e.X, e.Y))
            {
                StartCaptureOperation(e, CaptureOperation.Select, false);
                UpdateSelection(e);
                return true;
            }

            return false;
        }

        private bool HandleMouseDownEnvelopeSelection(MouseEventArgs e)
        {
            if (e.Button.HasFlag(MouseButtons.Right) && (IsMouseInHeaderTopPart(e.X, e.Y) || IsMouseInNoteArea(e.X, e.Y)))
            {
                StartCaptureOperation(e, CaptureOperation.Select);
                UpdateSelection(e);
                return true;
            }

            return false;
        }

        private bool HandleMouseDownEffectList(MouseEventArgs e)
        {
            if (e.Button.HasFlag(MouseButtons.Left) && IsMouseInEffectList(e.X, e.Y))
            {
                int effectIdx = (e.Y - headerSizeY) / effectButtonSizeY;
                if (effectIdx >= 0 && effectIdx < supportedEffects.Length)
                {
                    selectedEffectIdx = supportedEffects[effectIdx];
                    ConditionalInvalidate();
                }
                return true;
            }

            return false;
        }

        private bool HandleMouseDownAltZoom(MouseEventArgs e)
        {
            if (e.Button.HasFlag(MouseButtons.Right) && ModifierKeys.HasFlag(Keys.Alt))
            {
                StartCaptureOperation(e, CaptureOperation.AltZoom);
                return true;
            }

            return false;
        }

        private bool HandleMouseDownEnvelopeResize(MouseEventArgs e)
        {
            if (e.Button.HasFlag(MouseButtons.Left) && IsMouseInHeaderTopPart(e.X, e.Y) && EditEnvelope.CanResize)
            {
                StartCaptureOperation(e, CaptureOperation.ResizeEnvelope);

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

            if (((left && EditEnvelope.CanLoop) || (right && EditEnvelope.CanRelease && EditEnvelope.Loop >= 0)) && IsMouseInHeaderBottomPart(e.X, e.Y))
            {
                CaptureOperation op = left ? CaptureOperation.DragLoop : CaptureOperation.DragRelease;
                StartCaptureOperation(e, op);

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
            if (e.Button.HasFlag(MouseButtons.Left) && IsMouseInNoteArea(e.X, e.Y) && EditEnvelope.Length > 0)
            {
                StartCaptureOperation(e, CaptureOperation.DrawEnvelope);

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
            if (e.Button.HasFlag(MouseButtons.Left) && IsMouseInEffectPanel(e.X, e.Y) && selectedEffectIdx >= 0)
            {
                if (GetEffectNoteForCoord(e.X, e.Y, out var location))
                {
                    StartChangeEffectValue(e, location);
                }
            }

            return false;
        }

        private bool HandleMouseDownDPCMVolumeEnvelope(MouseEventArgs e)
        {
            bool left  = e.Button.HasFlag(MouseButtons.Left);
            bool right = e.Button.HasFlag(MouseButtons.Right);

            if ((left || right) && IsMouseInEffectPanel(e.X, e.Y))
            {
                var vertexIdx = GetWaveVolumeEnvelopeVertexIndex(e);
                if (vertexIdx >= 0)
                {
                    if (left)
                    {
                        volumeEnvelopeDragVertex = vertexIdx;
                        App.UndoRedoManager.BeginTransaction(TransactionScope.DPCMSample, editSample.Id);
                        StartCaptureOperation(e, CaptureOperation.DragWaveVolumeEnvelope);
                    }
                    else
                    {
                        App.UndoRedoManager.BeginTransaction(TransactionScope.DPCMSample, editSample.Id);
                        editSample.VolumeEnvelope[vertexIdx].volume = 1.0f;
                        editSample.Process();
                        App.UndoRedoManager.EndTransaction();
                        ConditionalInvalidate();
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

            if ((left || right) && IsMouseOnSnapResolutionButton(e.X, e.Y))
            {
                if (left)
                    snapResolution = (SnapResolution)Math.Min((int)snapResolution + 1, (int)SnapResolution.Max - 1);
                else
                    snapResolution = (SnapResolution)Math.Max((int)snapResolution - 1, (int)SnapResolution.OneNote);

                ConditionalInvalidate();
                return true;
            }

            return false;
        }

        private bool HandleMouseDownSnapButton(MouseEventArgs e)
        {
            if (e.Button.HasFlag(MouseButtons.Left) && IsMouseOnSnapButton(e.X, e.Y))
            {
                snap = !snap;
                ConditionalInvalidate();
                return true;
            }

            return false;
        }

        private bool HandleMouseDownToggleEffectPanelButton(MouseEventArgs e)
        {
            if (e.Button.HasFlag(MouseButtons.Left) && IsMouseInTopLeftCorner(e.X, e.Y))
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

            if ((left || right) && (IsMouseInNoteArea(e.X, e.Y) || IsMouseInHeader(e.X, e.Y)))
            {
                StartCaptureOperation(e, CaptureOperation.SelectWave);
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

                ConditionalInvalidate();
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
            ConditionalInvalidate();
        }

        public void LayoutChanged()
        {
            UpdateRenderCoords();
            ClampScroll();
            ConditionalInvalidate();
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
                maxScrollX = Math.Max(Song.GetPatternStartAbsoluteNoteIndex(Song.Length) * noteSizeX - scrollMargin, 0);
            }
            else if (editMode == EditionMode.Enveloppe ||
                     editMode == EditionMode.Arpeggio)
            {
                maxScrollX = Math.Max(EditEnvelope.Length * noteSizeX - scrollMargin, 0);
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
            ConditionalInvalidate();
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
                scrollX -= 32;
                ClampScroll();
            }
            else if ((Width - mouseX) < 100)
            {
                scrollX += 32;
                ClampScroll();
            }
        }

        private void MarkPatternDirty(int patternIdx)
        {
            var channel = Song.Channels[editChannel];
            var pattern = channel.PatternInstances[patternIdx];
            channel.InvalidateCumulativePatternCache(patternIdx);
            PatternChanged?.Invoke(pattern);
        }

        private void MarkPatternDirty(Pattern pattern)
        {
            pattern.InvalidateCumulativeCache();
            PatternChanged?.Invoke(pattern);
        }

        private void StartPlayPiano(MouseEventArgs e)
        {
            StartCaptureOperation(e, CaptureOperation.PlayPiano);
            PlayPiano(e.X, e.Y);
        }

        private void EndPlayPiano()
        {
            App.StopOrReleaseIntrumentNote(false);
            playingNote = -1;
        }

        private void StartSelection(MouseEventArgs e)
        {
            StartCaptureOperation(e, CaptureOperation.Select, false);
            UpdateSelection(e);
        }

        private void UpdateSelection(MouseEventArgs e)
        {
            ScrollIfSelectionNearEdge(e.X);

            int noteIdx = (e.X - whiteKeySizeX + scrollX) / noteSizeX;

            int minSelectionIdx = Math.Min(noteIdx, captureMouseAbsoluteIdx);
            int maxSelectionIdx = Math.Max(noteIdx, captureMouseAbsoluteIdx);
            int pad = IsSnappingEnabled ? -1 : 0;

            SetSelection(SnapNote(minSelectionIdx), SnapNote(maxSelectionIdx, true) + pad);
            ConditionalInvalidate();
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
            ConditionalInvalidate();
        }

        private void UpdateSeekDrag(MouseEventArgs e, bool final)
        {
            dragSeekPosition = (int)Math.Floor((e.X - whiteKeySizeX + scrollX) / (float)noteSizeX);
            dragSeekPosition = SnapNote(dragSeekPosition);

            if (final)
                App.SeekSong(dragSeekPosition);

            ConditionalInvalidate();
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

            ConditionalInvalidate();
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
                    StartCaptureOperation(e, CaptureOperation.DragSlideNoteTarget, false, location.ToAbsoluteNoteIndex(Song));
                }
                else
                {
                    if (channel.SupportsInstrument(currentInstrument))
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
                        note.Instrument = editChannel == ChannelType.Dpcm ? null : currentInstrument;

                        StartCaptureOperation(e, CaptureOperation.CreateSlideNote, true);
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

                ConditionalInvalidate();
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
        }

        private void StartDragDPCMSampleMapping(MouseEventArgs e, byte noteValue)
        {
            App.UndoRedoManager.BeginTransaction(TransactionScope.Project);
            StartCaptureOperation(e, CaptureOperation.DragSample);
            draggedSample = App.Project.GetDPCMMapping(noteValue);
            App.Project.UnmapDPCMSample(noteValue);
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

                if (dx < 10 * RenderTheme.MainWindowScaling &&
                    dy < 10 * RenderTheme.MainWindowScaling)
                {
                    return idx;
                }
            }

            return -1;
        }

        private bool IsMouseInHeader(int x, int y)
        {
            return x > whiteKeySizeX && y < headerSizeY;
        }

        private bool IsMouseInHeaderTopPart(int x, int y)
        {
            return (editMode == EditionMode.Enveloppe || editMode == EditionMode.Arpeggio) && x > whiteKeySizeX && y > 0 && y < headerSizeY / 2;
        }

        private bool IsMouseInHeaderBottomPart(int x, int y)
        {
            return (editMode == EditionMode.Enveloppe || editMode == EditionMode.Arpeggio) && x > whiteKeySizeX && y >= headerSizeY / 2 && y < headerSizeY;
        }

        private bool IsMouseInPiano(int x, int y)
        {
            return x < whiteKeySizeX && y > headerAndEffectSizeY;
        }

        private bool IsMouseInEffectList(int x, int y)
        {
            return showEffectsPanel && editMode == EditionMode.Channel && x < whiteKeySizeX && y > headerSizeY && y < headerAndEffectSizeY;
        }

        private bool IsMouseInEffectPanel(int x, int y)
        {
            return showEffectsPanel && (editMode == EditionMode.Channel || editMode == EditionMode.DPCM) && x > whiteKeySizeX && y > headerSizeY && y < headerAndEffectSizeY;
        }

        private bool IsMouseOnSnapResolutionButton(int x, int y)
        {
            return IsSnappingAllowed &&
                x > whiteKeySizeX - (int)bmpSnap.Size.Width * 2 - snapIconPosX * 2 && x < whiteKeySizeX - (int)bmpSnap.Size.Width - snapIconPosX &&
                y > snapIconPosY && y < snapIconPosY + (int)bmpSnap.Size.Height;
        }
        private bool IsMouseOnSnapButton(int x, int y)
        {
            return IsSnappingAllowed &&
                x > whiteKeySizeX - (int)bmpSnap.Size.Width - snapIconPosX && x < whiteKeySizeX &&
                y > snapIconPosY && y < snapIconPosY + (int)bmpSnap.Size.Height;
        }

        private bool IsMouseInNoteArea(int x, int y)
        {
            return y > headerSizeY && x > whiteKeySizeX;
        }

        private bool IsMouseInTopLeftCorner(int x, int y)
        {
            return (editMode == EditionMode.Channel || editMode == EditionMode.DPCM) && y < headerSizeY && x < whiteKeySizeX;
        }

        private void UpdateToolTip(MouseEventArgs e)
        {
            var tooltip = "";
            var newNoteTooltip = "";

            if (IsMouseInHeader(e.X, e.Y) && editMode == EditionMode.Channel)
            {
                tooltip = "{MouseLeft} Seek - {MouseRight} Select - {MouseRight}{MouseRight} Select entire pattern";
            }
            else if (IsMouseInHeaderTopPart(e.X, e.Y) && (editMode == EditionMode.Enveloppe || editMode == EditionMode.Arpeggio))
            {
                tooltip = "{MouseRight} Select - {MouseLeft} Resize envelope";
            }
            else if (IsMouseInHeaderBottomPart(e.X, e.Y) && (editMode == EditionMode.Enveloppe || editMode == EditionMode.Arpeggio))
            {
                tooltip = "{MouseLeft} Set loop point - {MouseRight} Set release point (volume only, must have loop point)";
            }
            else if (IsMouseInPiano(e.X, e.Y))
            {
                tooltip = "{MouseLeft} Play piano - {MouseWheel} Pan";
            }
            else if (IsMouseOnSnapResolutionButton(e.X, e.Y))
            {
                tooltip = "{MouseLeft} Next snap precision {MouseRight} Previous snap precision {MouseWheel} Change snap precision";
            }
            else if (IsMouseOnSnapButton(e.X, e.Y))
            {
                tooltip = "{MouseLeft} Toggle snapping {Shift} {S} {MouseWheel} Change snap precision";
            }
            else if (IsMouseInTopLeftCorner(e.X, e.Y))
            {
                tooltip = "{MouseLeft} Show/hide effect panel {~}";
            }
            else if (IsMouseInEffectList(e.X, e.Y))
            {
                tooltip = "{MouseLeft} Select effect track to edit";
            }
            else if (IsMouseInEffectPanel(e.X, e.Y))
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
            else if ((IsMouseInNoteArea(e.X, e.Y) || IsMouseInHeader(e.X, e.Y)) && editMode == EditionMode.DPCM)
            {
                tooltip = "{MouseLeft} {Drag} or {MouseRight} {Drag} Select samples from source data";

                if (IsSelectionValid())
                {
                    tooltip += "\n{Del} Delete selected samples.";
                    newNoteTooltip = $"{(selectionMax - selectionMin + 1)} samples selected";
                }
            }
            else if (IsMouseInNoteArea(e.X, e.Y))
            {
                if (editMode == EditionMode.Channel)
                {
                    if (GetLocationForCoord(e.X, e.Y, out var location, out byte noteValue))
                    {
                        newNoteTooltip = $"{Note.GetFriendlyName(noteValue)} [{location.PatternIndex:D3} : {location.NoteIndex:D3}]";

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
                var snapFactor = SnapResolutionFactors[(int)snapResolution];

                Debug.Assert(snapFactor >= 1.0); // Fractional snapping is no longer supported.

                var numNotes = noteLength * (int)snapFactor;
                var snappedNoteIndex = (location.NoteIndex / numNotes + (roundUp ? 1 : 0)) * numNotes;

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
            if (Song.Channels[editChannel].SupportsInstrument(currentInstrument))
            {
                App.PlayInstrumentNote(noteValue, false, false);
                StartCaptureOperation(e, CaptureOperation.CreateNote, true);
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
            note.Instrument = editChannel == ChannelType.Dpcm ? null : currentInstrument;
            note.Arpeggio = Song.Channels[editChannel].SupportsArpeggios ? currentArpeggio : null;
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

            StartCaptureOperation(e, captureOp, true, location.ToAbsoluteNoteIndex(Song));

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
                        newNote.Slide = (byte)(oldNote.IsSlideNote ? Utils.Clamp(oldNote.Slide + deltaNoteValue, Note.MusicalNoteMin, Note.MusicalNoteMax) : 0);
                        newNote.Flags = oldNote.Flags;
                        newNote.Duration = (ushort)Math.Max(1, oldNote.Duration + deltaDuration);
                        newNote.Release = oldNote.Release;
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
                        note.Slide = (byte)(note.IsSlideNote ? Utils.Clamp(note.Slide + deltaNoteValue, Note.MusicalNoteMin, Note.MusicalNoteMax) : 0);
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

                if (channel.SupportsInstrument(currentInstrument))
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
                    note.Instrument = editChannel == ChannelType.Dpcm ? null : currentInstrument;
                    note.Arpeggio = channel.SupportsArpeggios ? currentArpeggio : null;
                    note.Duration = (ushort)(dragNoteOldDuration - dragNote.Duration);

                    channel.InvalidateCumulativePatternCache(dragNoteLocation.PatternIndex);
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
                channel.InvalidateCumulativePatternCache(p0);
                p0 = Song.PatternIndexFromAbsoluteNoteIndex(dragFrameMin + deltaNoteIdx + 0);
                p1 = Song.PatternIndexFromAbsoluteNoteIndex(dragFrameMax + deltaNoteIdx + 1);
                for (int p = p0; p <= p1 && p < Song.Length; p++)
                    PatternChanged?.Invoke(channel.PatternInstances[p]);
                channel.InvalidateCumulativePatternCache(p0);
            }

            if (final)
            {
                App.Project.Validate();
                App.UndoRedoManager.EndTransaction();
                App.StopInstrument();
            }

            ConditionalInvalidate();
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

            StartCaptureOperation(e, captureOp, true, location.ToAbsoluteNoteIndex(Song));
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
                    note.Duration = (ushort)Math.Max(1, note.Duration + deltaNoteIdx);

                return note;
            });

            if (final)
                App.UndoRedoManager.EndTransaction();
        }

        private void StartMoveNoteRelease(MouseEventArgs e, NoteLocation location)
        {
            var pattern = Song.Channels[editChannel].PatternInstances[location.PatternIndex];
            App.UndoRedoManager.BeginTransaction(TransactionScope.Pattern, pattern.Id);
            StartCaptureOperation(e, CaptureOperation.MoveNoteRelease, false, location.ToAbsoluteNoteIndex(Song));
        }

        private void UpdateMoveNoteRelease(MouseEventArgs e)
        {
            GetLocationForCoord(e.X, e.Y, out var location, out var noteValue, false);

            var channel = Song.Channels[editChannel];
            var pattern = channel.PatternInstances[captureNoteLocation.PatternIndex];
            var note = pattern.Notes[captureNoteLocation.NoteIndex];
            note.Release = (ushort)Utils.Clamp(Song.CountNotesBetween(captureNoteLocation, location), 1, note.Duration - 1);
            channel.InvalidateCumulativePatternCache(captureNoteLocation.PatternIndex);
            ConditionalInvalidate();
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

                    var minNoteCoordX = minAbsoluteNoteIdx * noteSizeX - scrollX;
                    var maxNoteCoordX = maxAbsoluteNoteIdx * noteSizeX - scrollX;

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
                    if (IsMouseInEffectPanel(pt.X, pt.Y))
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
                    else if (IsMouseInNoteArea(pt.X, pt.Y))
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
            ConditionalInvalidate();

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

        private void ZoomAtLocation(int x, int delta)
        {
            // When continuously following, zoom at the seek bar location.
            if (continuouslyFollowing)
                x = (int)(Width * ContinuousFollowPercent);

            int pixelX = x - whiteKeySizeX;
            int absoluteX = pixelX + scrollX;
            if (delta < 0 && zoomLevel > minZoomLevel) { zoomLevel--; absoluteX /= 2; }
            if (delta > 0 && zoomLevel < maxZoomLevel) { zoomLevel++; absoluteX *= 2; }
            scrollX = absoluteX - pixelX;

            UpdateRenderCoords();
            ClampScroll();
            ConditionalInvalidate();
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
                    ConditionalInvalidate();
                }
                else if (editMode != EditionMode.DPCMMapping)
                {
                    ZoomAtLocation(e.X, e.Delta);
                }
            }
            else if (IsMouseOnSnapResolutionButton(e.X, e.Y) || IsMouseOnSnapButton(e.X, e.Y))
            {
                if (e.Delta > 0)
                    snapResolution = (SnapResolution)Math.Min((int)snapResolution + 1, (int)SnapResolution.Max - 1);
                else
                    snapResolution = (SnapResolution)Math.Max((int)snapResolution - 1, (int)SnapResolution.OneNote);

                ConditionalInvalidate();
            }
        }

#if FAMISTUDIO_WINDOWS
        protected override void WndProc(ref System.Windows.Forms.Message m)
        {
            base.WndProc(ref m);
            if (m.Msg == 0x020e) // WM_MOUSEHWHEEL
                OnMouseHorizontalWheel(PlatformUtils.ConvertHorizontalMouseWheelMessage(this, m));
        }

        protected void OnMouseHorizontalWheel(MouseEventArgs e)
#else
        protected override void OnMouseHorizontalWheel(MouseEventArgs e)
#endif
        {
            scrollX += e.Delta;
            ClampScroll();
            ConditionalInvalidate();
        }

        public void UpdateFollowMode(bool force = false)
        {
            continuouslyFollowing = false;

            if ((App.IsPlaying || force) && App.FollowModeEnabled && Settings.FollowSync != Settings.FollowSyncSequencer && !panning && captureOperation == CaptureOperation.None && editMode == EditionMode.Channel)
            {
                var frame = App.CurrentFrame;
                var seekX = frame * noteSizeX - scrollX;

                if (Settings.FollowMode == Settings.FollowModeJump)
                {
                    var maxX = Width - whiteKeySizeX;
                    if (seekX < 0 || seekX > maxX)
                        scrollX = frame * noteSizeX;
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

            var pt = this.PointToClient(Cursor.Position);
            var e = new MouseEventArgs(MouseButtons.None, 1, pt.X, pt.Y, 0);

            UpdateCaptureOperation(e, true);
            UpdateFollowMode();
        }

        private bool GetEffectNoteForCoord(int x, int y, out NoteLocation location)
        {
            if (x > whiteKeySizeX && y > headerSizeY && y < headerAndEffectSizeY)
            {
                var absoluteNoteIndex = (x - whiteKeySizeX + scrollX) / noteSizeX;
                location = NoteLocation.FromAbsoluteNoteIndex(Song, absoluteNoteIndex);
                if (location.PatternIndex < Song.Length)
                    return true;
            }

            location = NoteLocation.Invalid;
            return false;
        }

        private bool GetNoteValueForCoord(int x, int y, out byte noteValue)
        {
            noteValue = (byte)(numNotes - Utils.Clamp((y + scrollY - headerAndEffectSizeY) / noteSizeY, 0, numNotes));
            return x > whiteKeySizeX && y > headerAndEffectSizeY;
        }

        private bool GetLocationForCoord(int x, int y, out NoteLocation location, out byte noteValue, bool allowSnap = false)
        {
            var absoluteNoteIndex = Utils.Clamp((x - whiteKeySizeX + scrollX) / noteSizeX, 0, Song.GetPatternStartAbsoluteNoteIndex(Song.Length) - 1);

            if (allowSnap)
                absoluteNoteIndex = SnapNote(absoluteNoteIndex);

            location = Song.AbsoluteNoteIndexToNoteLocation(absoluteNoteIndex);
            noteValue = (byte)(numNotes - Utils.Clamp((y + scrollY - headerAndEffectSizeY) / noteSizeY, 0, numNotes));

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

            idx = (x - whiteKeySizeX + scrollX) / noteSizeX;
            value = (sbyte)(maxValue - (int)Math.Min((y + scrollY - headerAndEffectSizeY - 1) / envelopeSizeY, 128)); 

            return x > whiteKeySizeX;
        }

        public void SerializeState(ProjectBuffer buffer)
        {
            int editModeInt = (int)editMode;
            buffer.Serialize(ref editModeInt);
            editMode = (EditionMode)editModeInt;

            buffer.Serialize(ref editChannel);
            buffer.Serialize(ref currentInstrument);
            buffer.Serialize(ref currentArpeggio);
            buffer.Serialize(ref editInstrument);
            buffer.Serialize(ref editEnvelope);
            buffer.Serialize(ref editArpeggio);
            buffer.Serialize(ref editSample);
            buffer.Serialize(ref envelopeValueZoom);
            buffer.Serialize(ref envelopeValueOffset);
            buffer.Serialize(ref scrollX);
            buffer.Serialize(ref scrollY);
            buffer.Serialize(ref zoomLevel);
            buffer.Serialize(ref selectedEffectIdx);
            buffer.Serialize(ref showEffectsPanel);
            buffer.Serialize(ref selectionMin);
            buffer.Serialize(ref selectionMax);

            if (buffer.IsReading)
            {
                BuildSupportEffectList();
                UpdateRenderCoords();
                ClampScroll();
                ClampMinSnap();
                ConditionalInvalidate();

                captureOperation = CaptureOperation.None;
                Capture = false;
                panning = false;
            }
        }
    }
}
