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
        const int ScrollMargin = 128;
        const int DrawFrameZoomLevel = -1;
        const float ContinuousFollowPercent = 0.75f;
        const float DefaultZoomWaveTime = 0.25f;
        const float WaveDisplayScaleY = 0.98f;

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
        const int DefaultWhiteKeySizeX = 85;
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
        int barSizeX;
        int attackIconPosX;
        int noteTextPosY;
        int minNoteSizeForText;
        int waveGeometrySampleSize;
        int minZoomLevel;
        int maxZoomLevel;
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
            OneQuarter,
            OneThird,
            OneHalf,
            OneNote,
            TwoNote,
            ThreeNote,
            FourNote,
            Max
        };

        readonly double[] SnapResolutionFactors = new[]
        {
            1.0 / 4.0,
            1.0 / 3.0,
            1.0 / 2.0,
            1.0,
            2.0,
            3.0,
            4.0
        };

        static readonly List<char> RecordingNoteToKeyMap = new List<char>
        {
            'Z', 'S', 'X', 'D', 'C', 'V', 'G', 'B', 'H', 'N', 'J', 'M',
            'Q', '2', 'W', '3', 'E', 'R', '5', 'T', '6', 'Y', '7', 'U', 'I', '9', 'O', '0', 'P', '[', '=', ']'
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
        RenderBrush attackBrush;
        RenderBrush iconTransparentBrush;
        RenderBrush dashedLineBrush;
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
            DrawEnvelope,
            Select,
            SelectWave,
            CreateDragSlideNoteTarget,
            DragSlideNoteTarget,
            DragNote,
            DragNewNote,
            DragSelection,
            AltZoom,
            DragSample,
            DragSeekBar,
            DragWaveVolumeEnvelope
        }

        static readonly bool[] captureNeedsThreshold = new[]
        {
            false, // None
            false, // PlayPiano
            false, // ResizeEnvelope
            false, // DragLoop
            false, // DragRelease
            false, // ChangeEffectValue
            false, // DrawEnvelope
            false, // Select
            false, // SelectWave
            true,  // CreateDragSlideNoteTarget
            true,  // DragSlideNoteTarget
            true,  // DragNote
            false, // DragNewNote
            false, // DragSelection
            false, // AltZoom
            false, // DragSample
            false, // DragSeekBar
            false  // DragWaveVolumeEnvelope
        };

        int captureNoteIdx = 0;
        int captureNoteValue = 0;
        float captureWaveTime = 0.0f;
        int mouseLastX = 0;
        int mouseLastY = 0;
        int captureMouseX = 0;
        int captureMouseY = 0;
        int playingNote = -1;
        int effectPatternIdx;
        int effectNoteIdx;
        int selectionMin = -1;
        int selectionMax = -1;
        int dragSeekPosition = -1;
        int[] supportedEffects;
        bool captureThresholdMet = false;
        bool panning = false; // TODO: Make this a capture operation.
        bool continuouslyFollowing = false;
        CaptureOperation captureOperation = CaptureOperation.None;

        // Note dragging support.
        int dragFrameMin = -1;
        int dragFrameMax = -1;
        int dragLastNoteValue = -1;
        bool dragNewNoteCreatedPattern = false;
        SortedList<int, Note> dragNotes = new SortedList<int, Note>();

        bool showSelection = false;
        bool showEffectsPanel = false;
        bool snap = false;
        SnapResolution snapResolution = SnapResolution.OneNote;
        int scrollX = 0;
        int scrollY = 0;
        int zoomLevel = 0;
        int selectedEffectIdx = 0;
        float noteScaleY = 1.0f;
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

        public Instrument CurrentInstrument { get => currentInstrument; set => currentInstrument = value; }
        public Arpeggio CurrentArpeggio { get => currentArpeggio; set => currentArpeggio = value; }

        public delegate void EmptyDelegate();
        public delegate void PatternDelegate(Pattern pattern);
        public delegate void InstrumentDelegate(Instrument instrument);
        public delegate void DPCMMappingDelegate(int note);

        public event PatternDelegate     PatternChanged;
        public event EmptyDelegate       ManyPatternChanged;
        public event EmptyDelegate       DPCMSampleChanged;
        public event EmptyDelegate       EnvelopeChanged;
        public event EmptyDelegate       ControlActivated;
        public event EmptyDelegate       NotesPasted;
        public event EmptyDelegate       ScrollChanged;
        public event InstrumentDelegate  InstrumentEyedropped;
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
            noteSizeY = (int)(DefaultNoteSizeY * scaling * noteScaleY);
            noteAttackSizeX = (int)(DefaultNoteAttackSizeX * scaling);
            releaseNoteSizeY = (int)(DefaultReleaseNoteSizeY * scaling * noteScaleY) & 0xfe; // Keep even
            envelopeMax = (int)(DefaultEnvelopeMax * scaling);
            whiteKeySizeY = (int)(DefaultWhiteKeySizeY * scaling * noteScaleY);
            whiteKeySizeX = (int)(DefaultWhiteKeySizeX * scaling);
            recordKeyPosX = (int)((DefaultWhiteKeySizeX - 10) * scaling);
            blackKeySizeY = (int)(DefaultBlackKeySizeY * scaling * noteScaleY);
            blackKeySizeX = (int)(DefaultBlackKeySizeX * scaling);
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
            octaveSizeY = 12 * noteSizeY;
            numNotes = numOctaves * 12;
            virtualSizeY = numNotes * noteSizeY;
            barSizeX = noteSizeX * (Song == null ? 16 : Song.BeatLength);
            headerAndEffectSizeY = headerSizeY + (showEffectsPanel ? effectPanelSizeY : 0);
            noteTextPosY = scaling > 1 ? 0 : 1; // Pretty hacky.
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

        public void StartVideoRecording(RenderGraphics g, Song song, int zoom, bool thinNotes, out int outNoteSizeY)
        {
            editChannel = 0;
            editMode = EditionMode.VideoRecording;
            videoSong = song;
            zoomLevel = zoom;
            noteScaleY = thinNotes ? 0.667f : 1.0f;

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

            scrollX = Song.GetPatternStartNote(patternIdx) * noteSizeX;
            scrollY = maxScrollY / 2;

            var pattern = Song.Channels[editChannel].PatternInstances[patternIdx];
            if (pattern != null)
            {
                if (pattern.FirstValidNoteTime >= 0)
                {
                    var firstNote = pattern.FirstValidNote;
                    if (firstNote.IsMusical)
                    {
                        int noteY = virtualSizeY - firstNote.Value * noteSizeY;
                        scrollY = noteY - (Height - headerAndEffectSizeY) / 2;
                    }
                }
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
            attackBrush = g.CreateSolidBrush(Color.FromArgb(128, ThemeBase.BlackColor));
            iconTransparentBrush = g.CreateSolidBrush(Color.FromArgb(92, ThemeBase.DarkGreyLineColor2));
            dashedLineBrush = g.CreateBitmapBrush(g.CreateBitmapFromResource("Dash"), false, true);
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
            bmpSnapResolution[(int)SnapResolution.OneQuarter] = g.CreateBitmapFromResource("Snap1_4");
            bmpSnapResolution[(int)SnapResolution.OneThird] = g.CreateBitmapFromResource("Snap1_3");
            bmpSnapResolution[(int)SnapResolution.OneHalf] = g.CreateBitmapFromResource("Snap1_2");
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
            Utils.DisposeAndNullify(ref attackBrush);
            Utils.DisposeAndNullify(ref iconTransparentBrush);
            Utils.DisposeAndNullify(ref dashedLineBrush);
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
                maxNoteIdx = Math.Min((int)Math.Ceiling((scrollX + Width - whiteKeySizeX) / (float)noteSizeX), Song.GetPatternStartNote(Song.Length));
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
                        int px = Song.GetPatternStartNote(p) * noteSizeX - scrollX;
                        g.FillRectangle(px, headerSizeY / 2, px + sx, headerSizeY, theme.CustomColorBrushes[pattern.Color]);
                    }
                }

                DrawSelectionRect(g, headerSizeY);

                // Draw the header bars
                for (int p = a.minVisiblePattern; p < a.maxVisiblePattern; p++)
                {
                    var sx = Song.GetPatternLength(p) * noteSizeX;
                    int px = Song.GetPatternStartNote(p) * noteSizeX - scrollX;
                    if (p != 0)
                        g.DrawLine(px, 0, px, headerSizeY, theme.BlackBrush, 3.0f);
                    var pattern = Song.Channels[editChannel].PatternInstances[p];
                    g.DrawText(p.ToString(), ThemeBase.FontMediumCenter, px, effectNamePosY, theme.LightGreyFillBrush1, sx);
                    if (pattern != null)
                        g.DrawText(pattern.Name, ThemeBase.FontMediumCenter, px, effectNamePosY + headerSizeY / 2, theme.BlackBrush, sx);
                }

                int maxX = Song.GetPatternStartNote(a.maxVisiblePattern) * noteSizeX - scrollX;
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
            var draggingNote = captureOperation == CaptureOperation.DragNote || captureOperation == CaptureOperation.DragNewNote;
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

                for (int i = 0; i < RecordingNoteToKeyMap.Count; i++)
                {
                    int octaveBaseY = (virtualSizeY - octaveSizeY * ((i / 12) + App.BaseRecordingOctave)) - scrollY;
                    int y = octaveBaseY - (i % 12) * noteSizeY;

                    RenderBrush brush;
                    if (App.IsRecording)
                        brush = IsBlackKey(i % 12) ? theme.LightRedFillBrush : theme.DarkRedFillBrush;
                    else
                        brush = IsBlackKey(i % 12) ? theme.LightGreyFillBrush2 : theme.BlackBrush;

                    g.DrawText(RecordingNoteToKeyMap[i].ToString(), ThemeBase.FontVerySmallCenter, 0, y - recordingKeyOffsetY + g.WindowScaling * 2, brush, blackKeySizeX);
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
                    var channel = Song.Channels[editChannel];

                    // Draw the effects current value rectangles. Not all effects need this.
                    if (selectedEffectIdx >= 0 && Note.EffectWantsPreviousValue(selectedEffectIdx))
                    {
                        var lastFrame = -1;
                        var lastValue = channel.GetLastValidEffectValue(a.minVisiblePattern - 1, selectedEffectIdx);
                        var minValue = Note.GetEffectMinValue(Song, channel, selectedEffectIdx);
                        var maxValue = Note.GetEffectMaxValue(Song, channel, selectedEffectIdx);

                        for (int p = a.minVisiblePattern; p < a.maxVisiblePattern; p++)
                        {
                            var pattern = channel.PatternInstances[p];

                            if (pattern != null)
                            {
                                var patternLen = Song.GetPatternLength(p);
                                var x = Song.GetPatternStartNote(p) * noteSizeX - scrollX;

                                foreach (var kv in pattern.Notes)
                                {
                                    var time = kv.Key;
                                    var note = kv.Value;

                                    if (time >= patternLen)
                                        break;

                                    if (note.HasValidEffectValue(selectedEffectIdx))
                                    {
                                        g.PushTranslation(x + time * noteSizeX, 0);

                                        var frame = Song.GetPatternStartNote(p) + time;
                                        var sizeY = (float)Math.Floor((lastValue - minValue) / (float)(maxValue - minValue) * effectPanelSizeY);
                                        g.FillRectangle(lastFrame < 0 ? -noteSizeX * 100000 : (frame - lastFrame - 1) * -noteSizeX, effectPanelSizeY - sizeY, 0, effectPanelSizeY, theme.DarkGreyFillBrush2);
                                        lastValue = note.GetEffectValue(selectedEffectIdx);
                                        lastFrame = frame;

                                        g.PopTransform();
                                    }
                                }
                            }
                        }

                        g.PushTranslation(Math.Max(0, lastFrame * noteSizeX - scrollX), 0);
                        var lastSizeY = (float)Math.Floor((lastValue - minValue) / (float)(maxValue - minValue) * effectPanelSizeY);
                        g.FillRectangle(0, effectPanelSizeY - lastSizeY, Width, effectPanelSizeY, theme.DarkGreyFillBrush2);
                        g.PopTransform();
                    }

                    DrawSelectionRect(g, effectPanelSizeY);

                    for (int p = a.minVisiblePattern; p < a.maxVisiblePattern; p++)
                    {
                        var pattern = channel.PatternInstances[p];

                        if (pattern != null)
                        {
                            var patternLen = Song.GetPatternLength(p);
                            int x = Song.GetPatternStartNote(p) * noteSizeX - scrollX;

                            foreach (var kv in pattern.Notes)
                            {
                                var time = kv.Key;
                                var note = kv.Value;

                                if (time >= patternLen)
                                    break;

                                if (selectedEffectIdx >= 0 && note.HasValidEffectValue(selectedEffectIdx))
                                {
                                    var effectValue = note.GetEffectValue(selectedEffectIdx);
                                    var effectMinValue = Note.GetEffectMinValue(Song, channel, selectedEffectIdx);
                                    var effectMaxValue = Note.GetEffectMaxValue(Song, channel, selectedEffectIdx);
                                    var sizeY = (float)Math.Floor((effectValue - effectMinValue) / (float)(effectMaxValue - effectMinValue) * effectPanelSizeY);

                                    g.PushTranslation(x + time * noteSizeX, 0);

                                    if (!Note.EffectWantsPreviousValue(selectedEffectIdx))
                                        g.FillRectangle(0, 0, noteSizeX, effectPanelSizeY, theme.DarkGreyFillBrush2);

                                    g.FillRectangle(0, effectPanelSizeY - sizeY, noteSizeX, effectPanelSizeY, theme.LightGreyFillBrush1);
                                    g.DrawRectangle(0, effectPanelSizeY - sizeY, noteSizeX, effectPanelSizeY, theme.BlackBrush, IsNoteSelected(p, time) ? 2 : 1);

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

                            g.DrawLine(x, 0, x, headerAndEffectSizeY, theme.BlackBrush);
                            g.DrawLine(0, headerAndEffectSizeY - 1, Width, headerAndEffectSizeY - 1, theme.BlackBrush);
                        }
                    }

                    // Thick vertical bars
                    for (int p = a.minVisiblePattern; p < a.maxVisiblePattern; p++)
                    {
                        int x = Song.GetPatternStartNote(p) * noteSizeX - scrollX;
                        if (p != 0) g.DrawLine(x, 0, x, Height, theme.BlackBrush, 3.0f);
                    }

                    int maxX = Song.GetPatternStartNote(a.maxVisiblePattern) * noteSizeX - scrollX;
                    g.DrawLine(maxX, 0, maxX, Height, theme.BlackBrush, 3.0f);

                    int seekX = GetSeekFrameToDraw() * noteSizeX - scrollX;
                    g.DrawLine(seekX, 0, seekX, effectPanelSizeY, GetSeekBarBrush(), 3);
                }
                else if (editMode == EditionMode.DPCM)
                {
                    // Horizontal center line
                    var halfPanelSizeY = effectPanelSizeY * 0.5f;
                    g.DrawLine(0, halfPanelSizeY, Width, halfPanelSizeY, theme.BlackBrush);

                    // Volume envelope
                    for (int i = 0; i < 3; i++)
                    {
                        var x0 = GetPixelForWaveTime(editSample.VolumeEnvelope[i + 0].sample / editSample.SourceSampleRate, scrollX);
                        var x1 = GetPixelForWaveTime(editSample.VolumeEnvelope[i + 1].sample / editSample.SourceSampleRate, scrollX);
                        var y0 = effectPanelSizeY - editSample.VolumeEnvelope[i + 0].volume * halfPanelSizeY;
                        var y1 = effectPanelSizeY - editSample.VolumeEnvelope[i + 1].volume * halfPanelSizeY;

                        var points = new float[4, 2]
                        {
                            { x1, y1 },
                            { x0, y0 },
                            { x0, effectPanelSizeY },
                            { x1, effectPanelSizeY }
                        };

                        RenderGeometry geo = g.CreateGeometry(points, false);
                        g.FillGeometry(geo, theme.DarkGreyFillBrush1);
                        geo.Dispose();

                        g.AntiAliasing = true;
                        g.DrawLine(x0, y0, x1, y1, theme.WhiteBrush);
                        g.AntiAliasing = false;

                        g.PushTransform(x0, y0, 1.0f, 1.0f);
                        g.FillGeometry(sampleGeometry, theme.WhiteBrush);
                        g.PopTransform();

                        if (i == 2)
                        {
                            g.PushTransform(x1, y1, 1.0f, 1.0f);
                            g.FillGeometry(sampleGeometry, theme.WhiteBrush);
                            g.PopTransform();
                        }
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

            TransformNotes(selectionMin, selectionMax, false, (note, idx) =>
            {
                if (note != null && clone)
                    notes[idx] = note.Clone();
                else
                    notes[idx] = note;

                return note;
            });

            return notes;
        }

        private SortedList<int, Note> GetSparseSelectedNotes(int offset = 0)
        {
            if (!IsSelectionValid())
                return null;

            var notes = new SortedList<int, Note>();

            TransformNotes(selectionMin, selectionMax, false, (note, idx) =>
            {
                if (note != null && !note.IsEmpty)
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
            TransformNotes(startFrameIdx, startFrameIdx + notes.Length - 1, doTransaction, (note, idx) =>
            {
                var channel = Song.Channels[editChannel];
                var newNote = notes[idx];

                if (newNote == null)
                    newNote = Note.EmptyNote;

                if (note == null)
                    note = new Note();

                if (pasteNotes)
                {
                    if (!mix || !note.IsValid && newNote.IsValid)
                    {
                        note.Value = newNote.Value;
                        note.Instrument = editChannel == ChannelType.Dpcm || !channel.SupportsInstrument(newNote.Instrument) ? null : newNote.Instrument;
                        note.Slide = channel.SupportsSlideNotes ? newNote.Slide : (byte)0;
                        note.Flags = newNote.Flags;
                        note.Arpeggio = channel.SupportsArpeggios ? newNote.Arpeggio : null;
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

            var missingInstruments = ClipboardUtils.ContainsMissingInstrumentsOrSamples(App.Project, true, out var missingArpeggios, out var missingSamples);

            bool createMissingInstrument = false;
            if (missingInstruments)
                createMissingInstrument = PlatformUtils.MessageBox($"You are pasting notes referring to unknown instruments. Do you want to create the missing instrument?", "Paste", MessageBoxButtons.YesNo) == DialogResult.Yes;

            bool createMissingArpeggios = false;
            if (missingArpeggios)
                createMissingArpeggios = PlatformUtils.MessageBox($"You are pasting notes referring to unknown arpeggios. Do you want to create the missing arpeggios?", "Paste", MessageBoxButtons.YesNo) == DialogResult.Yes;

            bool createMissingSamples = false;
            if (missingSamples && editChannel == ChannelType.Dpcm)
                createMissingSamples = PlatformUtils.MessageBox($"You are pasting notes referring to unmapped DPCM samples. Do you want to create the missing samples?", "Paste", MessageBoxButtons.YesNo) == DialogResult.Yes;

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

        public bool CanCopy  => showSelection && IsSelectionValid() && (editMode == EditionMode.Channel || editMode == EditionMode.Enveloppe || editMode == EditionMode.Arpeggio);
        public bool CanPaste => showSelection && IsSelectionValid() && (editMode == EditionMode.Channel && ClipboardUtils.ConstainsNotes || (editMode == EditionMode.Enveloppe || editMode == EditionMode.Arpeggio) && ClipboardUtils.ConstainsEnvelope);

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

        private bool IsNoteSelected(int patternIdx, int noteIdx)
        {
            int absoluteNoteIdx = Song.GetPatternStartNote(patternIdx, noteIdx);
            return IsSelectionValid() && absoluteNoteIdx >= selectionMin && absoluteNoteIdx <= selectionMax;
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

        private Color GetNoteColor(int channel, Note note, Project project)
        {
            if (channel == ChannelType.Dpcm)
            {
                var mapping = project.GetDPCMMapping(note.Value);
                if (mapping != null && mapping.Sample != null)
                    return mapping.Sample.Color;
            }
            else if (note.Instrument != null)
            {
                return note.Instrument.Color;
            }

            return ThemeBase.LightGreyFillColor1;
        }

        private void RenderNote(RenderGraphics g, Channel channel, bool selected, bool activeChannel, Color color, Arpeggio arpeggio, int p0, int i0, Note n0, bool released, int p1, int i1)
        {
            int x = channel.Song.GetPatternStartNote(p0, i0) * noteSizeX - scrollX;
            int y = virtualSizeY - n0.Value * noteSizeY - scrollY;
            int sy = released ? releaseNoteSizeY : noteSizeY;

            if (n0.IsSlideNote && n0.Value != n0.SlideNoteTarget)
            {
                // We will get zero for notes that start a slide and have an immediate delayed cut.
                int duration = Math.Max(1, channel.GetSlideNoteDuration(n0, p0, i0)); 
                int slideSizeX = duration;
                int slideSizeY = n0.SlideNoteTarget - n0.Value;

                g.PushTransform(x, y + (slideSizeY > 0 ? 0 : noteSizeY), slideSizeX, -slideSizeY);
                g.FillGeometry(slideNoteGeometry[zoomLevel - MinZoomLevel], g.GetSolidBrush(color, 1.0f, 0.2f), true);
                g.PopTransform();
            }

            if (released)
                y += noteSizeY / 2 - releaseNoteSizeY / 2;

            g.PushTranslation(x, y);

            int noteLen = channel.Song.GetPatternStartNote(p1, i1) - channel.Song.GetPatternStartNote(p0, i0);
            int sx = noteLen * noteSizeX;
            int noteTextPosX = attackIconPosX;

            g.FillRectangle(0, 0, sx, sy, g.GetVerticalGradientBrush(color, sy, 0.8f));
            g.DrawRectangle(0, 0, sx, sy, selected ? selectionNoteBrush : theme.BlackBrush, selected ? 2 : 1);

            if (n0.HasAttack && sx > noteAttackSizeX + 4)
            if (activeChannel && n0.HasAttack && sx > noteAttackSizeX + 4)
            {
                g.FillRectangle(attackIconPosX, attackIconPosX, attackIconPosX + noteAttackSizeX, sy - attackIconPosX + 1, attackBrush);
                noteTextPosX += noteAttackSizeX + attackIconPosX;
            }

            if (activeChannel && Settings.ShowNoteLabels && !released && editMode == EditionMode.Channel && n0.IsMusical && sx > minNoteSizeForText)
            {
                g.DrawText(n0.FriendlyName, ThemeBase.FontSmall, noteTextPosX, noteTextPosY, theme.BlackBrush);
            }

            if (arpeggio != null)
            {
                var offsets = arpeggio.GetChordOffsets();
                foreach (var offset in offsets)
                {
                    g.PushTranslation(0, offset * -noteSizeY);
                    g.FillRectangle(0, 1, sx, sy, g.GetSolidBrush(arpeggio.Color, 1.0f, 0.2f));
                    g.PopTransform();
                }
            }

            g.PopTransform();
        }

        private void RenderReleaseStopNote(RenderGraphics g, Song song, int value, bool selected, Color color, Arpeggio arpeggio, int p1, int i1, Note n1, bool released)
        {
            int x = song.GetPatternStartNote(p1, i1) * noteSizeX - scrollX;
            int y = virtualSizeY - value * noteSizeY - scrollY;

            var paths = n1.IsStop ? (released ? stopReleaseNoteGeometry : stopNoteGeometry) : releaseNoteGeometry;

            g.PushTranslation(x, y);
            g.FillAndDrawGeometry(paths[zoomLevel - MinZoomLevel, 0], g.GetVerticalGradientBrush(color, noteSizeY, 0.8f), selected ? selectionNoteBrush : theme.BlackBrush, selected ? 2 : 1);

            if (arpeggio != null)
            {
                var offsets = arpeggio.GetChordOffsets();
                foreach (var offset in offsets)
                {
                    g.PushTranslation(0, offset * -noteSizeY);
                    g.FillGeometry(paths[zoomLevel - MinZoomLevel, 1], g.GetSolidBrush(arpeggio.Color, 1.0f, 0.2f), true);
                    g.PopTransform();
                }
            }

            g.PopTransform();

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
                int maxX = editMode == EditionMode.Channel ? song.GetPatternStartNote(a.maxVisiblePattern) * noteSizeX - scrollX : Width;

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
                                int x = (song.GetPatternStartNote(p) + i) * noteSizeX - scrollX;

                                if (i % beatLength == 0)
                                    g.DrawLine(x, 0, x, Height, theme.BlackBrush, i == 0 ? 3.0f : 1.0f);
                                else if (i % noteLength == 0)
                                    g.DrawLine(x, 0, x, Height, theme.DarkGreyLineBrush1);
                                else if (zoomLevel >= DrawFrameZoomLevel)
                                    g.DrawLine(x, 0, x, Height, dashedLineBrush /*theme.DarkGreyLineBrush3*/);
                            }
                        }
                        else
                        {
                            for (int i = p == 0 ? 1 : 0; i < patternLen; i++)
                            {
                                int x = (song.GetPatternStartNote(p) + i) * noteSizeX - scrollX;

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

                    var ghostChannelMask = App != null ? App.GhostChannelMask : 0;
                    var maxEffectPosY = 0;

                    // Render the active channel last.
                    var channelsToRender = new int[song.Channels.Length];
                    for (int c = 0; c < song.Channels.Length; c++)
                        channelsToRender[c] = c;

                    Utils.Swap(ref channelsToRender[editChannel], ref channelsToRender[channelsToRender.Length - 1]);

                    // Pattern drawing.
                    foreach (var c in channelsToRender)
                    {
                        var channel = song.Channels[c];
                        var isActiveChannel = c == editChannel;

                        if (isActiveChannel || (ghostChannelMask & (1 << c)) != 0)
                        {
                            var selected = false;
                            var color = ThemeBase.LightGreyFillColor1;
                            var arpeggio = (Arpeggio)null;

                            var p0 = a.minVisiblePattern - 1;
                            var i0 = 0;
                            var n0 = new Note(Note.NoteInvalid);

                            if (channel.GetLastValidNote(ref p0, ref n0, out i0, out var released))
                            {
                                n0 = n0.Clone();
                                selected = IsNoteSelected(p0, i0) && isActiveChannel;
                                color = GetNoteColor(c, n0, song.Project);
                                arpeggio = n0.Arpeggio;
                                if (!isActiveChannel) color = Color.FromArgb((int)(color.A * 0.2f), color);
                            }

                            for (int p1 = a.minVisiblePattern; p1 < a.maxVisiblePattern; p1++)
                            {
                                var pattern = song.Channels[c].PatternInstances[p1];

                                if (pattern == null)
                                    continue;

                                var patternLen = song.GetPatternLength(p1);

                                foreach (var kv in pattern.Notes)
                                {
                                    var i1 = kv.Key;
                                    var n1 = kv.Value.Clone();

                                    if (i1 >= patternLen)
                                        break;

                                    if (isActiveChannel && editMode != EditionMode.VideoRecording && n1.HasAnyEffect)
                                    {
                                        // Draw the effect icons.
                                        // TODO: Iterate on the bits of the effect mask. 
                                        var effectPosY = 0;
                                        for (int fx = 0; fx < Note.EffectCount; fx++)
                                        {
                                            if (n1.HasValidEffectValue(fx))
                                            {
                                                // These 2 effects usually come in a pair, so let's draw only 1 icon.
                                                if (fx == Note.EffectVibratoDepth && n1.HasValidEffectValue(Note.EffectVibratoSpeed))
                                                    continue;

                                                bool drawOpaque = !showEffectsPanel || fx == selectedEffectIdx || fx == Note.EffectVibratoDepth && selectedEffectIdx == Note.EffectVibratoSpeed || fx == Note.EffectVibratoSpeed && selectedEffectIdx == Note.EffectVibratoDepth;

                                                int iconX = channel.Song.GetPatternStartNote(p1, i1) * noteSizeX + noteSizeX / 2 - effectIconSizeX / 2 - scrollX;
                                                int iconY = effectPosY + effectIconPosY;
                                                g.FillRectangle(iconX, iconY, iconX + effectIconSizeX, iconY + effectIconSizeX, theme.DarkGreyLineBrush2);
                                                g.DrawBitmap(bmpEffects[fx], iconX, iconY, drawOpaque ? 1.0f : 0.4f);
                                                effectPosY += effectIconSizeX + effectIconPosY + 1;
                                            }
                                        }
                                        maxEffectPosY = Math.Max(maxEffectPosY, effectPosY);
                                    }

                                    if (n0.IsValid && n1.IsValid && ((n0.Value >= a.minVisibleNote && n0.Value <= a.maxVisibleNote) || n0.IsSlideNote || n0.IsArpeggio))
                                        RenderNote(g, channel, selected, isActiveChannel, color, arpeggio, p0, i0, n0, released, p1, i1);

                                    if (n1.IsStop || n1.IsRelease)
                                    {
                                        selected = IsNoteSelected(p1, i1) && isActiveChannel;
                                        int value = n0.Value >= Note.MusicalNoteMin && n0.Value <= Note.MusicalNoteMax ? n0.Value : 49; // C4 by default.

                                        if (value >= a.minVisibleNote && value <= a.maxVisibleNote)
                                            RenderReleaseStopNote(g, song, value, selected, color, arpeggio, p1, i1, n1, released);

                                        if (n1.IsRelease)
                                            released = true;

                                        if (n1.IsStop)
                                        {
                                            p0 = -1;
                                            n0.Value = Note.NoteInvalid;
                                            n0.Instrument = null;
                                            arpeggio = null;
                                        }
                                        else
                                        {
                                            i0 = i1 + 1;
                                            if (p0 < 0 || i0 >= song.GetPatternLength(p0))
                                            {
                                                i0 = 0;
                                                p0++;
                                            }
                                            else
                                            {
                                                p0 = p1;
                                            }

                                            // To avoid redrawing slides after a release note.
                                            n0.IsSlideNote = false;
                                            n0.HasAttack = false;
                                        }
                                    }
                                    else if (n1.IsValid)
                                    {
                                        n0 = n1;
                                        p0 = p1;
                                        i0 = i1;
                                        released &= !n1.HasAttack;
                                        selected = IsNoteSelected(p0, i0) && isActiveChannel;
                                        color = GetNoteColor(c, n0, song.Project);
                                        arpeggio = n0.Arpeggio;
                                        if (!isActiveChannel) color = Color.FromArgb((int)(color.A * 0.2f), color);
                                    }
                                }
                            }

                            if (n0.IsValid && ((n0.Value >= a.minVisibleNote && n0.Value <= a.maxVisibleNote) || n0.IsSlideNote || n0.IsArpeggio))
                            {
                                RenderNote(g, channel, selected, isActiveChannel, color, arpeggio, p0, i0, n0, released, Math.Min(Song.Length, a.maxVisiblePattern + 1), 0);
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

                    //int octaveBaseY = (virtualSizeY - octaveSizeY * i) - scrollY;

                    //for (int j = 0; j < 12; j++)
                    //{
                    //    int y = octaveBaseY - j * noteSizeY;
                    //    if (!IsBlackKey(j))
                    //        g.FillRectangle(0, y - noteSizeY, maxX, y, theme.DarkGreyFillBrush1);
                    //    if (i * 12 + j != numNotes)
                    //        g.DrawLine(0, y, maxX, y, theme.BlackBrush);
                    //}


                    for (int i = 0; i < Note.MusicalNoteMax; i++)
                    {
                        var mapping = App.Project.GetDPCMMapping(i);
                        if (mapping != null && mapping.Sample != null)
                        {
                            var y = virtualSizeY - i * noteSizeY - scrollY;

                            g.PushTranslation(0, y);
                            g.FillAndDrawRectangle(0, 0, Width - whiteKeySizeX, noteSizeY, g.GetVerticalGradientBrush(mapping.Sample.Color, noteSizeY, 0.8f), theme.BlackBrush);
                            if (mapping.Sample != null)
                            {
                                string text = $"{mapping.Sample.Name} (Pitch: {mapping.Pitch}";
                                if (mapping.Loop) text += ", Looping";
                                text += ")";
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

                        if (GetNoteForCoord(pt.X, pt.Y, out _, out _, out var noteValue) && App.Project.NoteSupportsDPCM(noteValue))
                        {
                            var y = virtualSizeY - noteValue * noteSizeY - scrollY;
                            g.PushTranslation(0, y);
                            g.FillAndDrawRectangle(0, 0, Width - whiteKeySizeX, noteSizeY, g.GetVerticalGradientBrush(dragSample.Color, noteSizeY, 0.8f), selectionNoteBrush, 2);
                            g.PopTransform();
                        }
                    }
                    else if (App.DraggedSample != null && captureOperation == CaptureOperation.None)
                    {

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

            if (!string.IsNullOrEmpty(noteTooltip))
            {
                g.DrawText(noteTooltip, ThemeBase.FontMediumBigRight, 0, Height - tooltipTextPosY, whiteKeyBrush, Width - tooltipTextPosX);
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
            var viewWidth  = Width - whiteKeySizeX;
            var halfHeight = (Height - headerAndEffectSizeY) / 2;
            var viewTime   = DefaultZoomWaveTime * (float)Math.Pow(2, -zoomLevel);

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
                        points[j, 1] = halfHeight + data[i] / (float)short.MinValue * halfHeight * WaveDisplayScaleY;
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
            var viewWidth  = Width - whiteKeySizeX;
            var realHeight = Height - headerAndEffectSizeY;
            var viewTime   = DefaultZoomWaveTime * (float)Math.Pow(2, -zoomLevel);

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
                            points[j, 1] = (-dpcmCounter / 64.0f + 0.5f) * realHeight * WaveDisplayScaleY + realHeight / 2; // DPCMTODO : Is that centered correctly? Also negative value?
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
                            g.FillGeometry(sampleGeometry, brush);
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
            var centerY = (Height - headerAndEffectSizeY) * 0.5f;
            g.DrawLine(0, centerY, Width, centerY, theme.BlackBrush);

            // Vertical lines (1.0, 0.1, 0.01 seconds)
            ForEachWaveTimecode(g, a, (time, x, level, idx) =>
            {
                var modSeconds = Utils.IntegerPow(10, level + 1);
                var modTenths  = Utils.IntegerPow(10, level);

                var brush = dashedLineBrush;

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
            g.DrawText($"Source Data ({(editSample.SourceDataIsWav ? "WAV" : "DMC")}) : {editSample.SourceSampleRate} Hz, {editSample.SourceDataSize} Bytes, {(int)(editSample.SourceDuration * 1000)} Ms", ThemeBase.FontMedium, bigTextPosX, dpcmSourceDataPosY, whiteKeyBrush);
            g.DrawText($"Processed Data (DMC) : {editSample.ProcessedSampleRate} Hz, {editSample.ProcessedData.Length} Bytes, {(int)(editSample.ProcessedDuration * 1000)} Ms", ThemeBase.FontMedium, bigTextPosX, dpcmSourceDataPosY + dpcmInfoSpacingY, whiteKeyBrush);
            g.DrawText($"Preview Playback : {editSample.GetPlaybackSampleRate(App.PalPlayback)} Hz, {(int)(editSample.GetPlaybackDuration(App.PalPlayback) * 1000)} Ms", ThemeBase.FontMedium, bigTextPosX, dpcmSourceDataPosY + dpcmInfoSpacingY * 2, whiteKeyBrush);

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
            scrollX = (int)Math.Round((Song.GetPatternStartNote(patternIndex) + noteIndex) * noteSizeX);
            scrollY = noteY - (Height - headerAndEffectSizeY) / 2;
            playingNote = highlightKey;
            videoKeyColor = highlightColor;

            Utils.DisposeAndNullify(ref whiteKeyPressedBrush);
            Utils.DisposeAndNullify(ref blackKeyPressedBrush);
            whiteKeyPressedBrush = g.CreateSolidBrush(highlightColor);
            blackKeyPressedBrush = g.CreateSolidBrush(highlightColor);

            OnRender(g);
        }

        protected override void OnRender(RenderGraphics g)
        {
            var a = new RenderArea();

            var minVisibleNoteIdx = Math.Max((int)Math.Floor(scrollX / (float)noteSizeX), 0);
            var maxVisibleNoteIdx = Math.Min((int)Math.Ceiling((scrollX + Width) / (float)noteSizeX), Song.GetPatternStartNote(Song.Length));

            a.maxVisibleNote = numNotes - Utils.Clamp((int)Math.Floor(scrollY / (float)noteSizeY), 0, numNotes);
            a.minVisibleNote = numNotes - Utils.Clamp((int)Math.Ceiling((scrollY + Height - headerAndEffectSizeY) / (float)noteSizeY), 0, numNotes);
            a.maxVisibleOctave = (int)Math.Ceiling(a.maxVisibleNote / 12.0f);
            a.minVisibleOctave = (int)Math.Floor(a.minVisibleNote / 12.0f);
            a.minVisiblePattern = Utils.Clamp(Song.FindPatternInstanceIndex(minVisibleNoteIdx, out _) + 0, 0, Song.Length);
            a.maxVisiblePattern = Utils.Clamp(Song.FindPatternInstanceIndex(maxVisibleNoteIdx, out _) + 1, 0, Song.Length);

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

        void ChangeEffectValue(MouseEventArgs e)
        {
            Debug.Assert(selectedEffectIdx >= 0);

            bool shift = ModifierKeys.HasFlag(Keys.Shift);

            var channel = Song.Channels[editChannel];
            var pattern = channel.PatternInstances[effectPatternIdx];
            var minValue = Note.GetEffectMinValue(Song, channel, selectedEffectIdx);
            var maxValue = Note.GetEffectMaxValue(Song, channel, selectedEffectIdx);

            var note = pattern.GetOrCreateNoteAt(effectNoteIdx);

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

            note.SetEffectValue(selectedEffectIdx, newValue);
            pattern.ClearLastValidNoteCache();

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

        private void UpdateWavePreset()
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
            bool foundNote = GetNoteForCoord(e.X, e.Y, out int patternIdx, out int noteIdx, out byte noteValue);

            if (editMode == EditionMode.DPCMMapping && left && foundNote)
            {
                // In case we were dragging a sample.
                EndCaptureOperation(e);

                var mapping = App.Project.GetDPCMMapping(noteValue);
                if (left && mapping != null)
                {
                    var dlg = new PropertyDialog(PointToScreen(new Point(e.X, e.Y)), 160, false, e.Y > Height / 2);
                    dlg.Properties.AddIntegerRange("Pitch :", mapping.Pitch, 0, 15); // 0
                    dlg.Properties.AddBoolean("Loop :", mapping.Loop); // 1
                    dlg.Properties.Build();

                    if (dlg.ShowDialog(ParentForm) == DialogResult.OK)
                    {
                        App.UndoRedoManager.BeginTransaction(TransactionScope.DPCMSamplesMapping);
                        mapping.Pitch = dlg.Properties.GetPropertyValue<int>(0);
                        mapping.Loop  = dlg.Properties.GetPropertyValue<bool>(1);
                        App.UndoRedoManager.EndTransaction();
                        ConditionalInvalidate();
                    }
                }
            }
            else if (right && editMode == EditionMode.Channel && e.Y < headerSizeY && e.X > whiteKeySizeX)
            {
                int patIdx = Song.FindPatternInstanceIndex((int)Math.Floor((e.X - whiteKeySizeX + scrollX) / (float)noteSizeX), out _);
                if (patIdx >= 0 && patIdx < Song.Length)
                {
                    SetSelection(Song.GetPatternStartNote(patIdx), Song.GetPatternStartNote(patIdx + 1) - 1);
                    ConditionalInvalidate();
                }
            }
        }

        private void CaptureMouse(MouseEventArgs e)
        {
            mouseLastX = e.X;
            mouseLastY = e.Y;
            captureMouseX = e.X;
            captureMouseY = e.Y;
            Capture = true;
        }

        private void StartCaptureOperation(MouseEventArgs e, CaptureOperation op, bool allowSnap = false, int noteIdx = -1)
        {
            Debug.Assert(captureOperation == CaptureOperation.None);
            CaptureMouse(e);
            captureOperation = op;
            captureThresholdMet = !captureNeedsThreshold[(int)op];
            captureNoteValue = numNotes - Utils.Clamp((e.Y + scrollY - headerAndEffectSizeY) / noteSizeY, 0, numNotes);
            captureNoteIdx = noteIdx >= 0 ? noteIdx : (e.X - whiteKeySizeX + scrollX) / noteSizeX;
            captureWaveTime = editMode == EditionMode.DPCM ? GetWaveTimeForPixel(e.X - whiteKeySizeX) : 0.0f;
            if (allowSnap)
                captureNoteIdx = SnapNote(captureNoteIdx);
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
                        ChangeEffectValue(e);
                        break;
                    case CaptureOperation.DrawEnvelope:
                        DrawEnvelope(e);
                        break;
                    case CaptureOperation.Select:
                        UpdateSelection(e.X);
                        break;
                    case CaptureOperation.SelectWave:
                        UpdateWaveSelection(e.X);
                        break;
                    case CaptureOperation.DragSlideNoteTarget:
                    case CaptureOperation.CreateDragSlideNoteTarget:
                        UpdateSlideNoteTarget(e);
                        break;
                    case CaptureOperation.DragNote:
                    case CaptureOperation.DragNewNote:
                    case CaptureOperation.DragSelection:
                        UpdateNoteDrag(e, false);
                        break;
                    case CaptureOperation.AltZoom:
                        UpdateAltZoom(e);
                        break;
                    case CaptureOperation.DragSample:
                        UpdateSampleDrag(e);
                        break;
                    case CaptureOperation.DragSeekBar:
                        UpdateSeekDrag(e.X);
                        break;
                    case CaptureOperation.DragWaveVolumeEnvelope:
                        UpdateVolumeEnvelopeDrag(e);
                        break;
                }
            }
        }

        private void EndSampleDrag(MouseEventArgs e)
        {
            bool success = false;
            if (GetNoteForCoord(e.X, e.Y, out _, out _, out var noteValue) && App.Project.NoteSupportsDPCM(noteValue) && noteValue != captureNoteValue && draggedSample != null)
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
                App.UndoRedoManager.RestoreTransaction();
                App.UndoRedoManager.AbortTransaction();

                if (noteValue != captureNoteValue && draggedSample != null)
                    SystemSounds.Beep.Play();
            }

            ConditionalInvalidate();
        }

        private void EndCaptureOperation(MouseEventArgs e)
        {
            if (captureOperation != CaptureOperation.None)
            {
                var patternIdx = Song.FindPatternInstanceIndex(captureNoteIdx, out var noteIdx);

                switch (captureOperation)
                {
                    case CaptureOperation.DragLoop:
                    case CaptureOperation.DragRelease:
                    case CaptureOperation.ChangeEffectValue:
                    case CaptureOperation.CreateDragSlideNoteTarget:
                        App.UndoRedoManager.EndTransaction();
                        break;
                    case CaptureOperation.PlayPiano:
                        App.StopOrReleaseIntrumentNote(false);
                        playingNote = -1;
                        ConditionalInvalidate();
                        break;
                    case CaptureOperation.ResizeEnvelope:
                    case CaptureOperation.DrawEnvelope:
                        UpdateWavePreset();
                        App.UndoRedoManager.EndTransaction();
                        EnvelopeChanged?.Invoke();
                        break;
                    case CaptureOperation.DragSlideNoteTarget:
                        if (!captureThresholdMet)
                            Song.Channels[editChannel].PatternInstances[patternIdx].GetOrCreateNoteAt(noteIdx).IsSlideNote ^= true;
                        App.UndoRedoManager.EndTransaction();
                        ConditionalInvalidate();
                        break;
                    case CaptureOperation.DragNote:
                    case CaptureOperation.DragNewNote:
                    case CaptureOperation.DragSelection:
                        UpdateNoteDrag(e, true, !captureThresholdMet);
                        App.UndoRedoManager.EndTransaction();
                        ConditionalInvalidate();
                        App.StopInstrument();
                        break;
                    case CaptureOperation.DragSample:
                        EndSampleDrag(e);
                        break;
                    case CaptureOperation.DragSeekBar:
                        UpdateSeekDrag(e.X);
                        App.SeekSong(dragSeekPosition);
                        break;
                    case CaptureOperation.DragWaveVolumeEnvelope:
                        UpdateVolumeEnvelopeDrag(e);
                        App.UndoRedoManager.EndTransaction();

                        break;
                }

                draggedSample = null;
                captureOperation = CaptureOperation.None;
                Capture = false;
                panning = false;
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

                ManyPatternChanged?.Invoke();
            }
        }

        private void GetSelectionRange(int minFrameIdx, int maxFrameIdx, out int minPattern, out int maxPattern, out int minNote, out int maxNote)
        {
            minPattern = Song.FindPatternInstanceIndex(minFrameIdx, out minNote);
            maxPattern = Song.FindPatternInstanceIndex(maxFrameIdx, out maxNote);
        }

        // TODO : Make this a property.
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

        private void TransformNotes(int startFrameIdx, int endFrameIdx, bool doTransaction, Func<Note, int, Note> function)
        {
            if (doTransaction)
                App.UndoRedoManager.BeginTransaction(TransactionScope.Channel, Song.Id, editChannel);

            GetSelectionRange(startFrameIdx, endFrameIdx, out int minPattern, out int maxPattern, out int minNote, out int maxNote);

            for (var p = minPattern; p <= maxPattern; p++)
            {
                var pattern = Song.Channels[editChannel].PatternInstances[p];

                if (pattern != null)
                {
                    var patternLen = Song.GetPatternLength(p);
                    var n0 = p == minPattern ? minNote : 0;
                    var n1 = p == maxPattern ? maxNote : patternLen - 1;
                    var newNotes = new SortedList<int, Note>();

                    for (var it = pattern.GetNoteIterator(n0, n1 + 1); !it.Done; it.Next())
                    {
                        var transformedNote = function(it.CurrentNote, Song.GetPatternStartNote(p) + it.CurrentTime - startFrameIdx);
                        if (transformedNote != null)
                            newNotes[it.CurrentTime] = transformedNote;
                    }

                    pattern.DeleteNotesBetween(n0, n1 + 1);

                    foreach (var kv in newNotes)
                        pattern.SetNoteAt(kv.Key, kv.Value);

                    pattern.ClearLastValidNoteCache();
                    PatternChanged?.Invoke(pattern);
                }
            }

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

            UpdateWavePreset();
            EnvelopeChanged?.Invoke();
            App.UndoRedoManager.EndTransaction();
            ConditionalInvalidate();
        }

        private void TransposeNotes(int amount)
        {
            var processedNotes = new HashSet<Note>();

            TransformNotes(selectionMin, selectionMax, true, (note, idx) =>
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
            TransformNotes(selectionMin, selectionMax, doTransaction, (note, idx) =>
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
            else if (e.KeyCode == Keys.Oem3)
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
            if (editMode == EditionMode.DPCMMapping && GetNoteForCoord(pos.X, pos.Y, out _, out _, out var noteValue))
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

                var startPatternIdx = Song.FindPatternInstanceIndex(startFrame, out _);
                var endPatternIdx = Song.FindPatternInstanceIndex(endFrame, out _);

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
                var snappedFrame = SnapNote(frame, true, true);

                if (doTransaction)
                    App.UndoRedoManager.BeginTransaction(TransactionScope.Application);

                App.SeekSong(snappedFrame);
                EnsureSeekBarVisible();

                if (doTransaction)
                    App.UndoRedoManager.EndTransaction();

                ConditionalInvalidate();
            }
        }

        public void RecordNote(Note note)
        {
            if (App.IsRecording && editMode == EditionMode.Channel && (note.IsMusical || note.IsStop))
            {
                var currentFrame = SnapNote(App.CurrentFrame, false, true);
                var patternIdx = Song.FindPatternInstanceIndex(currentFrame, out var noteIdx);
                var channel = Song.Channels[editChannel];
                var pattern = channel.PatternInstances[patternIdx];

                // Create a pattern if needed.
                if (pattern == null)
                {
                    App.UndoRedoManager.BeginTransaction(TransactionScope.Channel, Song.Id, editChannel);
                    pattern = channel.CreatePattern();
                    channel.PatternInstances[patternIdx] = pattern;
                }
                else
                {
                    App.UndoRedoManager.BeginTransaction(TransactionScope.Pattern, pattern.Id);
                }

                var newNote = note.Clone();
                newNote.HasVolume = false;
                pattern.Notes[noteIdx] = newNote;
                pattern.ClearLastValidNoteCache();
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

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            ControlActivated?.Invoke();

            bool left   = e.Button.HasFlag(MouseButtons.Left);
            bool middle = e.Button.HasFlag(MouseButtons.Middle) || (e.Button.HasFlag(MouseButtons.Left) && ModifierKeys.HasFlag(Keys.Alt));
            bool right  = e.Button.HasFlag(MouseButtons.Right);

            if (captureOperation != CaptureOperation.None)
                return;

            UpdateCursor();

            if (left && IsMouseInPiano(e))
            {
                StartCaptureOperation(e, CaptureOperation.PlayPiano);
                PlayPiano(e.X, e.Y);
            }
            else if (left && editMode == EditionMode.Channel && IsMouseInHeader(e))
            {
                StartCaptureOperation(e, CaptureOperation.DragSeekBar);
                UpdateSeekDrag(e.X);
            }
            else if (right && editMode == EditionMode.Channel && IsMouseInHeader(e))
            {
                StartCaptureOperation(e, CaptureOperation.Select, false);
                UpdateSelection(e.X, true);
            }
            else if (right && (editMode == EditionMode.Enveloppe || editMode == EditionMode.Arpeggio) && (IsMouseInHeaderTopPart(e) || IsMouseInNoteArea(e)))
            {
                StartCaptureOperation(e, CaptureOperation.Select);
                UpdateSelection(e.X, true);
            }
            else if (left && IsMouseInEffectList(e))
            {
                int effectIdx = (e.Y - headerSizeY) / effectButtonSizeY;
                if (effectIdx >= 0 && effectIdx < supportedEffects.Length)
                {
                    selectedEffectIdx = supportedEffects[effectIdx];
                    ConditionalInvalidate();
                }
            }
            else if (middle && e.Y > headerSizeY && e.X > whiteKeySizeX)
            {
                panning = true;
                CaptureMouse(e);
            }
            else if (right && ModifierKeys.HasFlag(Keys.Alt))
            {
                StartCaptureOperation(e, CaptureOperation.AltZoom);
            }
            else if (left && (editMode == EditionMode.Enveloppe || editMode == EditionMode.Arpeggio) && IsMouseInHeaderTopPart(e) && EditEnvelope.CanResize)
            {
                StartCaptureOperation(e, CaptureOperation.ResizeEnvelope);

                if (editMode == EditionMode.Enveloppe)
                    App.UndoRedoManager.BeginTransaction(TransactionScope.Instrument, editInstrument.Id);
                else
                    App.UndoRedoManager.BeginTransaction(TransactionScope.Arpeggio, editArpeggio.Id);

                ResizeEnvelope(e);
            }
            else if ((editMode == EditionMode.Enveloppe || editMode == EditionMode.Arpeggio) && ((left && EditEnvelope.CanLoop) || (right && EditEnvelope.CanRelease && EditEnvelope.Loop >= 0)) && IsMouseInHeaderBottomPart(e))
            {
                CaptureOperation op = left ? CaptureOperation.DragLoop : CaptureOperation.DragRelease;
                StartCaptureOperation(e, op);

                if (editMode == EditionMode.Enveloppe)
                    App.UndoRedoManager.BeginTransaction(TransactionScope.Instrument, editInstrument.Id);
                else
                    App.UndoRedoManager.BeginTransaction(TransactionScope.Arpeggio, editArpeggio.Id);

                ResizeEnvelope(e);
            }
            else if (left && (editMode == EditionMode.Enveloppe || editMode == EditionMode.Arpeggio) && IsMouseInNoteArea(e) && EditEnvelope.Length > 0)
            {
                StartCaptureOperation(e, CaptureOperation.DrawEnvelope);

                if (editMode == EditionMode.Enveloppe)
                    App.UndoRedoManager.BeginTransaction(TransactionScope.Instrument, editInstrument.Id);
                else
                    App.UndoRedoManager.BeginTransaction(TransactionScope.Arpeggio, editArpeggio.Id);

                DrawEnvelope(e, true);
            }
            else if (left && editMode == EditionMode.Channel && IsMouseInEffectPanel(e) && selectedEffectIdx >= 0)
            {
                if (GetEffectNoteForCoord(e.X, e.Y, out effectPatternIdx, out effectNoteIdx))
                {
                    var pattern = Song.Channels[editChannel].PatternInstances[effectPatternIdx];
                    if (pattern != null)
                    {
                        StartCaptureOperation(e, CaptureOperation.ChangeEffectValue);
                        App.UndoRedoManager.BeginTransaction(TransactionScope.Pattern, pattern.Id);
                        ChangeEffectValue(e);
                    }
                }
            }
            else if (left && editMode == EditionMode.DPCM && IsMouseInEffectPanel(e))
            {
                var vertexIdx = GetWaveVolumeEnvelopeVertexIndex(e);
                if (vertexIdx >= 0)
                {
                    volumeEnvelopeDragVertex = vertexIdx;
                    App.UndoRedoManager.BeginTransaction(TransactionScope.DPCMSample, editSample.Id);
                    StartCaptureOperation(e, CaptureOperation.DragWaveVolumeEnvelope);
                }
            }
            else if ((left || right) && IsMouseOnSnapResolutionButton(e))
            {
                if (left)
                    snapResolution = (SnapResolution)Math.Min((int)snapResolution + 1, (int)SnapResolution.Max - 1);
                else
                    snapResolution = (SnapResolution)Math.Max((int)snapResolution - 1, (int)(App.Project.UsesFamiTrackerTempo ? SnapResolution.OneNote : SnapResolution.OneQuarter));

                ConditionalInvalidate();
            }
            else if (left && IsMouseOnSnapButton(e))
            {
                if (left)
                    snap = !snap;

                ConditionalInvalidate();
            }
            else if (left && IsMouseInTopLeftCorner(e) && (editMode == EditionMode.Channel || editMode == EditionMode.DPCM))
            {
                ToggleEffectPannel();
                return;
            }
            else if (editMode == EditionMode.DPCM && (left || right) && (IsMouseInNoteArea(e) || IsMouseInHeader(e)))
            {
                StartCaptureOperation(e, CaptureOperation.SelectWave);
                UpdateWaveSelection(e.X, true);
            }
            else if (editMode == EditionMode.Channel && GetNoteForCoord(e.X, e.Y, out int patternIdx, out int noteIdx, out byte noteValue))
            {
                if (patternIdx >= Song.Length)
                    return;

                var changed = false;
                var channel = Song.Channels[editChannel];
                var pattern = channel.PatternInstances[patternIdx];

                var supportsInstrument = channel.SupportsInstrument(currentInstrument);

                if (left)
                {
                    var ctrl = ModifierKeys.HasFlag(Keys.Control);
                    var shift = ModifierKeys.HasFlag(Keys.Shift);
                    var slide = FamiStudioForm.IsKeyDown(Keys.S);
                    var attack = FamiStudioForm.IsKeyDown(Keys.A);
                    var eyedrop = FamiStudioForm.IsKeyDown(Keys.I);

                    if (slide && channel.FindPreviousMatchingNote(noteValue, ref patternIdx, ref noteIdx))
                    {
                        if (channel.SupportsSlideNotes)
                        {
                            App.UndoRedoManager.BeginTransaction(TransactionScope.Pattern, channel.PatternInstances[patternIdx].Id);
                            var op = CaptureOperation.DragSlideNoteTarget;
                            StartCaptureOperation(e, op, false, Song.GetPatternStartNote(patternIdx, noteIdx));
                            changed = true;
                        }
                    }
                    else if (attack && channel.FindPreviousMatchingNote(noteValue, ref patternIdx, ref noteIdx))
                    {
                        App.UndoRedoManager.BeginTransaction(TransactionScope.Pattern, channel.PatternInstances[patternIdx].Id);
                        channel.PatternInstances[patternIdx].GetOrCreateNoteAt(noteIdx).HasAttack ^= true;
                        channel.PatternInstances[patternIdx].ClearLastValidNoteCache();
                        App.UndoRedoManager.EndTransaction();
                        changed = true;
                    }
                    else if (eyedrop)
                    {
                        if (channel.FindPreviousMatchingNote(noteValue, ref patternIdx, ref noteIdx) &&
                            channel.PatternInstances[patternIdx].Notes.TryGetValue(noteIdx, out var note) && note != null)
                        {
                            App.UndoRedoManager.BeginTransaction(TransactionScope.Application);
                            InstrumentEyedropped?.Invoke(note.Instrument);
                            App.UndoRedoManager.EndTransaction();
                        }
                    }
                    else if (ctrl || shift && channel.SupportsReleaseNotes)
                    {
                        if (pattern != null)
                        {
                            App.UndoRedoManager.BeginTransaction(TransactionScope.Pattern, pattern.Id);
                        }
                        else
                        {
                            App.UndoRedoManager.BeginTransaction(TransactionScope.Channel, Song.Id, editChannel);
                            pattern = channel.CreatePatternAndInstance(patternIdx);
                        }

                        var note = pattern.GetOrCreateNoteAt(noteIdx);
                        note.Value = (byte)(ctrl ? Note.NoteStop : Note.NoteRelease);
                        note.Instrument = null;
                        note.Slide = 0;
                        note.Arpeggio = null;
                        pattern.ClearLastValidNoteCache();
                        App.UndoRedoManager.EndTransaction();
                        changed = true;
                    }
                    else if (slide && channel.SupportsSlideNotes)
                    {
                        if (supportsInstrument)
                        {
                            if (pattern != null)
                            {
                                App.UndoRedoManager.BeginTransaction(TransactionScope.Pattern, pattern.Id);
                            }
                            else
                            {
                                App.UndoRedoManager.BeginTransaction(TransactionScope.Channel, Song.Id, editChannel);
                                pattern = channel.CreatePatternAndInstance(patternIdx);
                            }

                            App.UndoRedoManager.BeginTransaction(TransactionScope.Pattern, pattern.Id);
                            SnapPatternNote(patternIdx, ref noteIdx);
                            var note = pattern.GetOrCreateNoteAt(noteIdx);
                            note.Value = noteValue;
                            note.Instrument = editChannel == ChannelType.Dpcm ? null : currentInstrument;
                            pattern.ClearLastValidNoteCache();
                            StartCaptureOperation(e, CaptureOperation.CreateDragSlideNoteTarget, true);
                            changed = true;
                        }
                        else
                        {
                            ShowInstrumentError();
                        }
                    }
                    else
                    {
                        var prevPatternIdx = patternIdx;
                        var prevNoteIdx = noteIdx;
                        var note = (Note)null;

                        if (pattern != null)
                            channel.PatternInstances[patternIdx].Notes.TryGetValue(noteIdx, out note);

                        var stopOrRelease = note != null && (note.IsStop || note.IsRelease);
                        var musicalNote = note != null && (note.IsMusical);
                        var dragStarted = false;

                        dragNewNoteCreatedPattern = false;

                        if (stopOrRelease || (musicalNote && note.Value == noteValue) || channel.FindPreviousMatchingNote(noteValue, ref prevPatternIdx, ref prevNoteIdx))
                        {
                            if (IsNoteSelected(prevPatternIdx, prevNoteIdx))
                            {
                                bool multiplePatterns = Song.FindPatternInstanceIndex(selectionMin, out _) !=
                                                        Song.FindPatternInstanceIndex(selectionMax, out _);

                                if (multiplePatterns)
                                    App.UndoRedoManager.BeginTransaction(TransactionScope.Channel, Song.Id, editChannel);
                                else
                                    App.UndoRedoManager.BeginTransaction(TransactionScope.Pattern, pattern.Id);

                                StartCaptureOperation(e, CaptureOperation.DragSelection, true);

                                var absPrevNoteIdx = Song.GetPatternStartNote(prevPatternIdx, prevNoteIdx);

                                dragNotes = GetSparseSelectedNotes(selectionMin);

                                dragFrameMin = selectionMin;
                                dragFrameMax = selectionMax;
                                dragStarted = true;
                            }
                            else
                            {
                                var prevPattern = channel.PatternInstances[prevPatternIdx];

                                App.UndoRedoManager.BeginTransaction(TransactionScope.Pattern, prevPattern.Id);
                                StartCaptureOperation(e, CaptureOperation.DragNote, true);

                                var absPrevNoteIdx = Song.GetPatternStartNote(prevPatternIdx, prevNoteIdx);

                                dragFrameMin = absPrevNoteIdx;
                                dragFrameMax = absPrevNoteIdx;

                                dragNotes.Clear();
                                dragNotes[absPrevNoteIdx] = channel.PatternInstances[prevPatternIdx].Notes[prevNoteIdx].Clone();
                                dragStarted = true;
                            }
                        }
                        else
                        {
                            if (supportsInstrument)
                            {
                                if (pattern != null)
                                {
                                    App.UndoRedoManager.BeginTransaction(TransactionScope.Pattern, pattern.Id);
                                }
                                else
                                {
                                    App.UndoRedoManager.BeginTransaction(TransactionScope.Channel, Song.Id, editChannel);
                                    pattern = channel.CreatePatternAndInstance(patternIdx);
                                    dragNewNoteCreatedPattern = true;
                                }

                                StartCaptureOperation(e, CaptureOperation.DragNewNote, true);

                                var newNote = new Note(noteValue);
                                newNote.Instrument = editChannel == ChannelType.Dpcm ? null : currentInstrument;
                                newNote.Arpeggio = Song.Channels[editChannel].SupportsArpeggios ? currentArpeggio : null;

                                dragFrameMin = captureNoteIdx;
                                dragFrameMax = captureNoteIdx;

                                dragNotes.Clear();
                                dragNotes[captureNoteIdx] = newNote;
                                dragStarted = true;
                            }
                            else
                            {
                                ShowInstrumentError();
                            }
                        }

                        if (dragStarted)
                        {
                            changed = true;
                            dragLastNoteValue = -1;
                            UpdateNoteDrag(e, false);
                            ConditionalInvalidate();
                        }
                    }
                }
                else if (right)
                {
                    if (pattern != null && pattern.Notes.TryGetValue(noteIdx, out var note) && (note.IsStop || note.IsRelease))
                    {
                        var select = false;

                        if (note.IsStop || note.IsRelease)
                        {
                            int prevPatternIdx = patternIdx;
                            int prevNoteIdx    = noteIdx;

                            // For stop and release note, we actually don't know their position since it 
                            // depends on the previous note. We need to search for it.
                            if (channel.FindPreviousMatchingNote(-1, ref prevPatternIdx, ref prevNoteIdx))
                            {
                                // See if the previous musical note matches where the user clicked.
                                if (!channel.PatternInstances[prevPatternIdx].Notes.TryGetValue(prevNoteIdx, out var prevMusicalNote) || prevMusicalNote.Value != noteValue)
                                    select = true;
                            }
                            // When stop or releases are "orphans" (no previous note), they are drawn at C4, so check for that too.
                            else if (noteValue != 49)
                            {
                                select = true;
                            }
                        }

                        if (select)
                        {
                            StartCaptureOperation(e, CaptureOperation.Select, false);
                            UpdateSelection(e.X, true);
                        }
                        else
                        {
                            App.UndoRedoManager.BeginTransaction(TransactionScope.Pattern, pattern.Id);
                            pattern.Notes.Remove(noteIdx);
                            pattern.ClearLastValidNoteCache();
                            App.UndoRedoManager.EndTransaction();
                            changed = true;
                        }
                    }
                    else if (channel.FindPreviousMatchingNote(noteValue, ref patternIdx, ref noteIdx))
                    {
                        var foundPattern = channel.PatternInstances[patternIdx];
                        App.UndoRedoManager.BeginTransaction(TransactionScope.Pattern, foundPattern.Id);
                        foundPattern.Notes.Remove(noteIdx);
                        foundPattern.ClearLastValidNoteCache();
                        App.UndoRedoManager.EndTransaction();
                        changed = true;
                    }
                    else
                    {
                        StartCaptureOperation(e, CaptureOperation.Select, false);
                        UpdateSelection(e.X, true);
                    }
                }

                if (changed)
                {
                    PatternChanged?.Invoke(pattern);
                    ConditionalInvalidate();
                }
            }
            else if (editMode == EditionMode.Channel && right && GetEffectNoteForCoord(e.X, e.Y, out patternIdx, out noteIdx) && selectedEffectIdx >= 0)
            {
                var pattern = Song.Channels[editChannel].PatternInstances[patternIdx];
                if (pattern != null)
                {
                    if (pattern.Notes.TryGetValue(noteIdx, out var note) && note != null && note.HasValidEffectValue(selectedEffectIdx))
                    {
                        App.UndoRedoManager.BeginTransaction(TransactionScope.Pattern, pattern.Id);
                        note.ClearEffectValue(selectedEffectIdx);
                        pattern.ClearLastValidNoteCache();
                        PatternChanged?.Invoke(pattern);
                        App.UndoRedoManager.EndTransaction();
                        ConditionalInvalidate();
                    }
                    else
                    {
                        StartCaptureOperation(e, CaptureOperation.Select, false);
                        UpdateSelection(e.X, true);
                    }
                }
            }
            else if (editMode == EditionMode.DPCMMapping && (left || right) && GetNoteForCoord(e.X, e.Y, out patternIdx, out noteIdx, out noteValue))
            {
                if (App.Project.NoteSupportsDPCM(noteValue))
                {
                    var mapping = App.Project.GetDPCMMapping(noteValue);

                    if (left && mapping == null)
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
                            dlg.Properties.AddStringList(null, sampleNames.ToArray(), sampleNames[0]); // 1
                            dlg.Properties.Build();

                            if (dlg.ShowDialog(ParentForm) == DialogResult.OK)
                            {
                                App.UndoRedoManager.BeginTransaction(TransactionScope.DPCMSamplesMapping, TransactionFlags.StopAudio);
                                var sampleName = dlg.Properties.GetPropertyValue<string>(1);
                                App.Project.MapDPCMSample(noteValue, App.Project.GetSample(sampleName));
                                App.UndoRedoManager.EndTransaction();
                                DPCMSampleMapped?.Invoke(noteValue);
                                ConditionalInvalidate();
                            }
                        }
                    }
                    else if (left && mapping != null)
                    {
                        App.UndoRedoManager.BeginTransaction(TransactionScope.Project);
                        StartCaptureOperation(e, CaptureOperation.DragSample);
                    }
                    else if (right && mapping != null)
                    {
                        App.UndoRedoManager.BeginTransaction(TransactionScope.DPCMSamplesMapping, TransactionFlags.StopAudio);
                        App.Project.UnmapDPCMSample(noteValue);
                        App.UndoRedoManager.EndTransaction();
                        DPCMSampleUnmapped?.Invoke(noteValue);
                        ConditionalInvalidate();
                    }
                }
                else
                {
                    App.DisplayWarning("DPCM samples are only allowed between C1 and D6");
                }
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateRenderCoords();
            ClampScroll();
        }

        private void ClampScroll()
        {
            if (Song != null)
            {
                int minScrollX = 0;
                int minScrollY = 0;
                int maxScrollX = 0;
                int maxScrollY = Math.Max(virtualSizeY + headerAndEffectSizeY - Height, 0);

                if (editMode == EditionMode.Channel || 
                    editMode == EditionMode.VideoRecording)
                {
                    maxScrollX = Math.Max(Song.GetPatternStartNote(Song.Length) * noteSizeX - ScrollMargin, 0);
                }
                else if (editMode == EditionMode.Enveloppe || 
                         editMode == EditionMode.Arpeggio)
                {
                    maxScrollX = Math.Max(EditEnvelope.Length * noteSizeX - ScrollMargin, 0);
                }
                else if (editMode == EditionMode.DPCM)
                {
                    maxScrollX = Math.Max((int)Math.Ceiling(GetPixelForWaveTime(Math.Max(editSample.SourceDuration, editSample.ProcessedDuration))) - ScrollMargin, 0);
                    minScrollY = Math.Max(virtualSizeY + headerAndEffectSizeY - Height, 0) / 2;
                    maxScrollY = minScrollY;
                }

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
                rangeMax = editMode == EditionMode.Channel ? Song.GetPatternStartNote(Song.Length) - 1 : EditEnvelope.Length - 1;
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

        private void UpdateSelection(int mouseX, bool first = false)
        {
            ScrollIfSelectionNearEdge(mouseX);

            int noteIdx = (mouseX - whiteKeySizeX + scrollX) / noteSizeX;

            int minSelectionIdx = Math.Min(noteIdx, captureNoteIdx);
            int maxSelectionIdx = Math.Max(noteIdx, captureNoteIdx);
            int pad = IsSnappingEnabled ? -1 : 0;

            SetSelection(SnapNote(minSelectionIdx), SnapNote(maxSelectionIdx, true) + pad);
            ConditionalInvalidate();
        }

        private void UpdateWaveSelection(int mouseX, bool first = false)
        {
            ScrollIfSelectionNearEdge(mouseX);

            float time = Math.Max(0.0f, GetWaveTimeForPixel(mouseX - whiteKeySizeX));

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

        private void UpdateSeekDrag(int mouseX)
        {
            dragSeekPosition = (int)Math.Floor((mouseX - whiteKeySizeX + scrollX) / (float)noteSizeX);
            dragSeekPosition = SnapNote(dragSeekPosition);
            ConditionalInvalidate();
        }

        private void UpdateVolumeEnvelopeDrag(MouseEventArgs e)
        {
            var time   = Utils.Clamp((int)Math.Round(GetWaveTimeForPixel(e.X - whiteKeySizeX) * editSample.SourceSampleRate), 0, editSample.SourceNumSamples - 1);
            var volume = Utils.Clamp((1.0f - (e.Y - headerSizeY) / (float)effectPanelSizeY) * 2.0f, 0.0f, 2.0f);

            // Cant move 1st and last vertex.
            if (volumeEnvelopeDragVertex != 0 &&
                volumeEnvelopeDragVertex != editSample.VolumeEnvelope.Length - 1)
            {
                editSample.VolumeEnvelope[volumeEnvelopeDragVertex].sample = time;
            }

            editSample.VolumeEnvelope[volumeEnvelopeDragVertex].volume = volume;
            editSample.SortVolumeEnvelope(ref volumeEnvelopeDragVertex);
            editSample.Process();

            ConditionalInvalidate();
        }

        private void UpdateSlideNoteTarget(MouseEventArgs e)
        {
            Debug.Assert(captureNoteIdx >= 0);

            var patternIdx = Song.FindPatternInstanceIndex(captureNoteIdx, out var noteIdx);
            var pattern = Song.Channels[editChannel].PatternInstances[patternIdx];

            if (GetNoteForCoord(e.X, e.Y, out _, out _, out byte noteValue))
            {
                var note = pattern.GetOrCreateNoteAt(noteIdx);

                if (noteValue == note.Value)
                    note.SlideNoteTarget = 0;
                else
                    note.SlideNoteTarget = noteValue;

                ConditionalInvalidate();
            }
        }

        public void ReplaceSelectionInstrument(Instrument instrument)
        {
            if (editMode == EditionMode.Channel && editChannel != ChannelType.Dpcm && IsSelectionValid())
            {
                if (Song.Channels[editChannel].SupportsInstrument(instrument))
                {
                    TransformNotes(selectionMin, selectionMax, true, (note, idx) =>
                    {
                        if (note != null && note.IsMusical)
                            note.Instrument = instrument;
                        return note;
                    });
                }
                else
                {
                    ShowInstrumentError();
                }
            }
        }

        public void ReplaceSelectionArpeggio(Arpeggio arpeggio)
        {
            if (editMode == EditionMode.Channel && Song.Channels[editChannel].SupportsArpeggios && IsSelectionValid())
            {
                TransformNotes(selectionMin, selectionMax, true, (note, idx) =>
                {
                    if (note != null && note.IsMusical)
                        note.Arpeggio = arpeggio;
                    return note;
                });
            }
        }

        private readonly int[] vertexOrder = new int[] { 1, 2, 0, 3 };

        private int GetWaveVolumeEnvelopeVertexIndex(MouseEventArgs e)
        {
            Debug.Assert(editMode == EditionMode.DPCM);
            Debug.Assert(vertexOrder.Length == editSample.VolumeEnvelope.Length);

            var x = e.X - whiteKeySizeX;
            var y = e.Y - headerSizeY;

            for (int i = 0; i < 4; i++)
            {
                var idx = vertexOrder[i];

                var vx = GetPixelForWaveTime(editSample.VolumeEnvelope[idx].sample / editSample.SourceSampleRate, scrollX);
                var vy = (int)Math.Round(effectPanelSizeY - editSample.VolumeEnvelope[idx].volume * (effectPanelSizeY * 0.5f));

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

        private bool IsMouseInHeader(MouseEventArgs e)
        {
            return e.X > whiteKeySizeX && e.Y < headerSizeY;
        }

        private bool IsMouseInHeaderTopPart(MouseEventArgs e)
        {
            return (editMode == EditionMode.Enveloppe || editMode == EditionMode.Arpeggio) && e.X > whiteKeySizeX && e.Y > 0 && e.Y < headerSizeY / 2;
        }

        private bool IsMouseInHeaderBottomPart(MouseEventArgs e)
        {
            return (editMode == EditionMode.Enveloppe || editMode == EditionMode.Arpeggio) && e.X > whiteKeySizeX && e.Y >= headerSizeY / 2 && e.Y < headerSizeY;
        }

        private bool IsMouseInPiano(MouseEventArgs e)
        {
            return e.X < whiteKeySizeX && e.Y > headerAndEffectSizeY;
        }

        private bool IsMouseInEffectList(MouseEventArgs e)
        {
            return showEffectsPanel && editMode == EditionMode.Channel && e.X < whiteKeySizeX && e.Y > headerSizeY && e.Y < headerAndEffectSizeY;
        }

        private bool IsMouseInEffectPanel(MouseEventArgs e)
        {
            return showEffectsPanel && (editMode == EditionMode.Channel || editMode == EditionMode.DPCM) && e.X > whiteKeySizeX && e.X > headerSizeY && e.Y < headerAndEffectSizeY;
        }

        private bool IsMouseOnSnapResolutionButton(MouseEventArgs e)
        {
            return IsSnappingAllowed &&
                e.X > whiteKeySizeX - (int)bmpSnap.Size.Width * 2 - snapIconPosX * 2 && e.X < whiteKeySizeX - (int)bmpSnap.Size.Width - snapIconPosX &&
                e.Y > snapIconPosY && e.Y < snapIconPosY + (int)bmpSnap.Size.Height;
        }
        private bool IsMouseOnSnapButton(MouseEventArgs e)
        {
            return IsSnappingAllowed &&
                e.X > whiteKeySizeX - (int)bmpSnap.Size.Width - snapIconPosX && e.X < whiteKeySizeX &&
                e.Y > snapIconPosY && e.Y < snapIconPosY + (int)bmpSnap.Size.Height;
        }

        private bool IsMouseInNoteArea(MouseEventArgs e)
        {
            return e.Y > headerSizeY && e.X > whiteKeySizeX;
        }

        private bool IsMouseInTopLeftCorner(MouseEventArgs e)
        {
            return (editMode == EditionMode.Channel || editMode == EditionMode.DPCM) && e.Y < headerSizeY && e.X < whiteKeySizeX;
        }

        private void UpdateToolTip(MouseEventArgs e)
        {
            var tooltip = "";
            var newNoteTooltip = "";

            if (IsMouseInHeader(e) && editMode == EditionMode.Channel)
            {
                tooltip = "{MouseLeft} Seek - {MouseRight} Select - {MouseRight}{MouseRight} Select entire pattern";
            }
            else if (IsMouseInHeaderTopPart(e) && (editMode == EditionMode.Enveloppe || editMode == EditionMode.Arpeggio))
            {
                tooltip = "{MouseRight} Select - {MouseLeft} Resize envelope";
            }
            else if (IsMouseInHeaderBottomPart(e) && (editMode == EditionMode.Enveloppe || editMode == EditionMode.Arpeggio))
            {
                tooltip = "{MouseLeft} Set loop point - {MouseRight} Set release point (volume only, must have loop point)";
            }
            else if (IsMouseInPiano(e))
            {
                tooltip = "{MouseLeft} Play piano - {MouseWheel} Pan";
            }
            else if (IsMouseOnSnapResolutionButton(e))
            {
                tooltip = "{MouseLeft} Next snap precision {MouseRight} Previous snap precision {MouseWheel} Change snap precision";
            }
            else if (IsMouseOnSnapButton(e))
            {
                tooltip = "{MouseLeft} Toggle snapping {Shift} {S} {MouseWheel} Change snap precision";
            }
            else if (IsMouseInTopLeftCorner(e))
            {
                tooltip = "{MouseLeft} Show/hide effect panel {~}";
            }
            else if (IsMouseInEffectList(e))
            {
                tooltip = "{MouseLeft} Select effect track to edit";
            }
            else if (IsMouseInEffectPanel(e))
            {
                tooltip = "{MouseLeft} Set effect value - {MouseWheel} Pan\n{Shift} {MouseLeft} Set effect value (fine) - {MouseRight} Clear effect value";
            }
            else if ((IsMouseInNoteArea(e) || IsMouseInHeader(e)) && editMode == EditionMode.DPCM)
            {
                tooltip = "{MouseLeft} {Drag} or {MouseRight} {Drag} Select samples from source data";

                if (IsSelectionValid())
                {
                    tooltip += "\n{Del} Delete selected samples.";
                    newNoteTooltip = $"{(selectionMax - selectionMin + 1)} samples selected";
                }
            }
            else if (IsMouseInNoteArea(e))
            {
                if (editMode == EditionMode.Channel)
                {
                    if (GetNoteForCoord(e.X, e.Y, out int patternIdx, out int noteIdx, out byte noteValue))
                    {
                        if (Song.Channels[editChannel].PatternInstances[patternIdx] == null)
                            tooltip = "{MouseWheel} Pan";

                        newNoteTooltip = $"{Note.GetFriendlyName(noteValue)} [{patternIdx:D3} : {noteIdx:D3}]";
                        if (Song.Channels[editChannel].FindPreviousMatchingNote(noteValue, ref patternIdx, ref noteIdx))
                        {
                            var pat = Song.Channels[editChannel].PatternInstances[patternIdx];
                            var note = pat.Notes[noteIdx];
                            if (note.Instrument != null)
                                newNoteTooltip += $" ({note.Instrument.Name})";
                            if (note.IsArpeggio)
                                newNoteTooltip += $" (Arpeggio: {note.Arpeggio.Name})";

                            tooltip = "{MouseLeft} {Drag} Add/drag note - {Ctrl} {MouseLeft} Add stop note - {Shift} {MouseLeft} Add release note - {MouseWheel} Pan\n{S} {MouseLeft} {Drag} Slide note - {A} {MouseLeft} Toggle note attack - {I} {MouseLeft} Instrument Eyedrop - {MouseRight} Delete note";
                        }
                        else
                        {
                            tooltip = "{MouseLeft} Add note - {Ctrl} {MouseLeft} Add stop note - {Shift} {MouseLeft} Add release note - {MouseWheel} Pan\n{MouseRight} Select";
                        }
                    }
                }
                else if (editMode == EditionMode.Enveloppe || editMode == EditionMode.Arpeggio)
                {
                    tooltip = "{MouseLeft} Set envelope value - {MouseWheel} Pan";

                    if (GetEnvelopeValueForCoord(e.X, e.Y, out int idx, out sbyte value))
                        newNoteTooltip = $"{idx:D3} : {value}";
                }
                else if (editMode == EditionMode.DPCMMapping)
                {
                    if (GetNoteForCoord(e.X, e.Y, out int patternIdx, out int noteIdx, out byte noteValue))
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
            {
                noteTooltip = newNoteTooltip;
                ConditionalInvalidate();
            }
        }

        private int PatternLengthForNoteIndex(int noteIdx)
        {
            var patternIdx = Song.FindPatternInstanceIndex(noteIdx, out noteIdx);
            return Song.GetPatternNoteLength(patternIdx);
        }

        private int SnapNote(int noteIdx, bool roundUp = false, bool forceSnap = false)
        {
            if (IsSnappingEnabled || forceSnap)
            {
                var patternIdx = Song.FindPatternInstanceIndex(noteIdx, out noteIdx);
                var noteLength = Song.Project.UsesFamiTrackerTempo ? 1 : Song.GetPatternNoteLength(patternIdx);
                var snapFactor = SnapResolutionFactors[(int)snapResolution];
                var snappedNoteIndex = noteIdx;

                if (snapFactor >= 1.0)
                {
                    var numNotes = noteLength * (int)snapFactor;
                    snappedNoteIndex = (noteIdx / numNotes + (roundUp ? 1 : 0)) * numNotes;
                }
                else
                {
                    // Subtract the base note so that snapping inside a note is always deterministic. 
                    // Otherwise, rounding errors can create a different snapping pattern every note (6-5-5-6 like Gimmick).
                    var baseNodeIdx = noteIdx / noteLength * noteLength;
                    var noteFrameIdx = noteIdx % noteLength;
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
                    snappedNoteIndex = Math.Min(Song.GetPatternLength(patternIdx) - 1, snappedNoteIndex);

                return Song.GetPatternStartNote(patternIdx, snappedNoteIndex);
            }
            else
            {
                return noteIdx;
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

        private void UpdateNoteDrag(MouseEventArgs e, bool final, bool createNote = false)
        {
            Debug.Assert(
                App.UndoRedoManager.HasTransactionInProgress && (
                    App.UndoRedoManager.UndoScope == TransactionScope.Pattern ||
                    App.UndoRedoManager.UndoScope == TransactionScope.Channel));

            var channel = Song.Channels[editChannel];

            App.UndoRedoManager.RestoreTransaction(false);

            GetNoteForCoord(e.X, e.Y, out var patternIdx, out var noteIdx, out var noteValue, true /* captureOperation != CaptureOperation.DragSelection*/);

            if (dragNewNoteCreatedPattern)
            {
                Debug.Assert(App.UndoRedoManager.UndoScope == TransactionScope.Channel);
                
                if (channel.PatternInstances[patternIdx] == null)
                    channel.CreatePatternAndInstance(patternIdx);
            }

            int deltaNoteIdx = Song.GetPatternStartNote(patternIdx, noteIdx) - captureNoteIdx;
            int deltaNoteValue = noteValue - captureNoteValue;
            int newDragFrameMin = dragFrameMin + deltaNoteIdx;
            int newDragFrameMax = dragFrameMax + deltaNoteIdx;

            // When we cross pattern boundaries, we will have to promote the current transaction
            // from pattern to channel.
            if (App.UndoRedoManager.UndoScope == TransactionScope.Pattern)
            {
                var initialPatternMinIdx = Song.FindPatternInstanceIndex(dragFrameMin, out _);
                var initialPatternMaxIdx = Song.FindPatternInstanceIndex(dragFrameMax, out _);
                Debug.Assert(initialPatternMinIdx == initialPatternMaxIdx);

                var newPatternMinIdx = Song.FindPatternInstanceIndex(newDragFrameMin, out _);
                var newPatternMaxIdx = Song.FindPatternInstanceIndex(newDragFrameMax, out _);

                bool multiplePatterns = newPatternMinIdx != initialPatternMinIdx ||
                                        newPatternMaxIdx != initialPatternMinIdx;

                if (multiplePatterns)
                {
                    App.UndoRedoManager.AbortTransaction();
                    App.UndoRedoManager.BeginTransaction(TransactionScope.Channel, Song.Id, editChannel);

                    if (captureOperation == CaptureOperation.DragNewNote)
                    {
                        if (channel.PatternInstances[newPatternMinIdx] == null)
                            channel.CreatePatternAndInstance(newPatternMinIdx);

                        dragNewNoteCreatedPattern = true;
                    }
                }
            }

            var copy = ModifierKeys.HasFlag(Keys.Control) && captureOperation != CaptureOperation.DragNewNote;
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

                if (frame < 0 || frame >= Song.GetPatternStartNote(Song.Length))
                    continue;

                var p = Song.FindPatternInstanceIndex(kv.Key + deltaNoteIdx, out var n);
                var pattern = channel.PatternInstances[p];

                if (pattern != null)
                {
                    if (keepFx)
                    {
                        var oldNote = kv.Value;

                        if (oldNote.IsValid)
                        {
                            var newNote = pattern.GetOrCreateNoteAt(n);

                            if (!oldNote.IsRelease && !oldNote.IsStop)
                            {
                                newNote.Value = (byte)Utils.Clamp(oldNote.Value + deltaNoteValue, Note.MusicalNoteMin, Note.MusicalNoteMax);
                                newNote.Instrument = oldNote.Instrument;
                                newNote.Arpeggio = oldNote.Arpeggio;
                                newNote.Slide = oldNote.Slide;
                                newNote.Flags = oldNote.Flags;
                            }
                            else
                            {
                                newNote.Value = oldNote.Value;
                            }
                        }
                    }
                    else
                    {
                        var note = kv.Value.Clone();
                        if (note.IsMusical)
                            note.Value = (byte)Utils.Clamp(note.Value + deltaNoteValue, Note.MusicalNoteMin, Note.MusicalNoteMax);
                        pattern.SetNoteAt(n, note);
                    }
                }
            }

            if (captureOperation == CaptureOperation.DragSelection)
            {
                selectionMin = Utils.Clamp(newDragFrameMin, 0, Song.GetPatternStartNote(Song.Length) - 1);
                selectionMax = Utils.Clamp(newDragFrameMax, 0, Song.GetPatternStartNote(Song.Length) - 1);
            }

            if (dragLastNoteValue != noteValue &&
                (captureOperation == CaptureOperation.DragNote ||
                 captureOperation == CaptureOperation.DragNewNote))
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
                    if (captureOperation == CaptureOperation.DragNewNote || !captureThresholdMet)
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
                Debug.Assert(dragFrameMin == dragFrameMax);

                // If there is a not between the snapped position and where we clicked, use that one.
                if (dragFrameMin > Song.GetPatternStartNote(patternIdx, noteIdx))
                    patternIdx = Song.FindPatternInstanceIndex(dragFrameMin, out noteIdx);

                var pattern = channel.PatternInstances[patternIdx];
                if (pattern != null)
                {
                    if (channel.SupportsInstrument(currentInstrument))
                    {
                        var note = pattern.GetOrCreateNoteAt(noteIdx);
                        note.Value = noteValue;
                        note.Instrument = editChannel == ChannelType.Dpcm ? null : currentInstrument;
                        note.Arpeggio = channel.SupportsArpeggios ? currentArpeggio : null;
                    }
                    else
                    {
                        ShowInstrumentError();
                    }
                }
            }

            if (final)
            {
                int p0, p1;

                p0 = Song.FindPatternInstanceIndex(dragFrameMin + 0, out _);
                p1 = Song.FindPatternInstanceIndex(dragFrameMax + 1, out _);
                for (int p = p0; p <= p1 && p < Song.Length; p++)
                    PatternChanged?.Invoke(channel.PatternInstances[p]);
                p0 = Song.FindPatternInstanceIndex(dragFrameMin + deltaNoteIdx + 0, out _);
                p1 = Song.FindPatternInstanceIndex(dragFrameMax + deltaNoteIdx + 1, out _);
                for (int p = p0; p <= p1 && p < Song.Length; p++)
                    PatternChanged?.Invoke(channel.PatternInstances[p]);
            }

            App.Project.Validate();
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

        private void UpdateSampleDrag(MouseEventArgs e)
        {
            if (draggedSample == null && GetNoteForCoord(e.X, e.Y, out _, out _, out var noteValue) && noteValue != captureNoteValue)
            {
                draggedSample = App.Project.GetDPCMMapping(captureNoteValue);
                App.Project.UnmapDPCMSample(captureNoteValue);
            }

            ConditionalInvalidate();
        }

        private void UpdateCursor()
        {
            var pt = PointToClient(Cursor.Position);

            if ((editMode == EditionMode.Enveloppe || editMode == EditionMode.Arpeggio) && EditEnvelope.CanResize && (pt.X > whiteKeySizeX && pt.Y < headerSizeY && captureOperation != CaptureOperation.Select) || captureOperation == CaptureOperation.ResizeEnvelope)
                Cursor.Current = Cursors.SizeWE;
            else if (captureOperation == CaptureOperation.ChangeEffectValue)
                Cursor.Current = Cursors.SizeNS;
            else if (ModifierKeys.HasFlag(Keys.Control) && (captureOperation == CaptureOperation.DragNote || captureOperation == CaptureOperation.DragSelection))
                Cursor.Current = Cursors.CopyCursor;
            else if (editMode == EditionMode.Channel && FamiStudioForm.IsKeyDown(Keys.I))
                Cursor.Current = Cursors.Eyedrop;
            else
                Cursor.Current = Cursors.Default;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            bool left   = e.Button.HasFlag(MouseButtons.Left);
            bool middle = e.Button.HasFlag(MouseButtons.Middle) || (e.Button.HasFlag(MouseButtons.Left) && ModifierKeys.HasFlag(Keys.Alt));

            UpdateCursor();
            UpdateCaptureOperation(e);

            if (middle)
            {
                DoScroll(e.X - mouseLastX, e.Y - mouseLastY);
            }

            UpdateToolTip(e);

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
            else if (IsMouseOnSnapResolutionButton(e) || IsMouseOnSnapButton(e))
            {
                if (e.Delta > 0)
                    snapResolution = (SnapResolution)Math.Min((int)snapResolution + 1, (int)SnapResolution.Max - 1);
                else
                    snapResolution = (SnapResolution)Math.Max((int)snapResolution - 1, (int)(App.Project.UsesFamiTrackerTempo ? SnapResolution.OneNote : SnapResolution.OneQuarter));

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

            if (captureOperation == CaptureOperation.Select)
                UpdateSelection(pt.X, false);
            else if (captureOperation == CaptureOperation.SelectWave)
                UpdateWaveSelection(pt.X, false);
            else if (captureOperation == CaptureOperation.DragSeekBar)
                UpdateSeekDrag(pt.X);

            UpdateFollowMode();
        }

        private bool GetEffectNoteForCoord(int x, int y, out int patternIdx, out int noteIdx)
        {
            if (x > whiteKeySizeX && y > headerSizeY && y < headerAndEffectSizeY)
            { 
                noteIdx = (x - whiteKeySizeX + scrollX) / noteSizeX;
                patternIdx = Song.FindPatternInstanceIndex(noteIdx, out noteIdx);
                return patternIdx < Song.Length;
            }
            else
            {
                patternIdx = -1;
                noteIdx = -1;
                return false;
            }
        }

        private bool GetNoteForCoord(int x, int y, out int patternIdx, out int noteIdx, out byte noteValue, bool allowSnap = false)
        {
            noteIdx = Utils.Clamp((x - whiteKeySizeX + scrollX) / noteSizeX, 0, Song.GetPatternStartNote(Song.Length) - 1);

            if (allowSnap)
                noteIdx = SnapNote(noteIdx);

            patternIdx = Song.FindPatternInstanceIndex(noteIdx, out noteIdx);
            noteValue = (byte)(numNotes - Utils.Clamp((y + scrollY - headerAndEffectSizeY) / noteSizeY, 0, numNotes));

            return (x > whiteKeySizeX && y > headerAndEffectSizeY && patternIdx < Song.Length);
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
