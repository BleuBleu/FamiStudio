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
    public class PianoRoll : RenderControl
    {
        const int MinZoomLevel = -3;
        const int MaxZoomLevel = 4;
        const int DefaultEnvelopeZoomLevel = 2;
        const int ScrollMargin = 128;
        const int DrawFrameZoomLevel = -1;

        const int DefaultNumOctaves = 8;
        const int DefaultHeaderSizeY = 17;
        const int DefaultDPCMHeaderSizeY = 17;
        const int DefaultEffectPanelSizeY = 176;
        const int DefaultEffectButtonSizeY = 17;
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
        const int DefaultOctaveNameOffsetY = 11;
        const int DefaultSlideIconPosX = 2;
        const int DefaultSlideIconPosY = 2;

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
        int octaveNameOffsetY;
        int octaveSizeY;
        int virtualSizeY;
        int barSizeX;
        int slideIconPosX;
        int slideIconPosY;
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
            Arpeggio
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

        RenderTheme theme;
        RenderBrush whiteKeyBrush;
        RenderBrush blackKeyBrush;
        RenderBrush whiteKeyPressedBrush;
        RenderBrush blackKeyPressedBrush;
        RenderBrush frameLineBrush;
        RenderBrush debugBrush;
        RenderBrush seekBarBrush;
        RenderBrush selectionBgVisibleBrush;
        RenderBrush selectionBgInvisibleBrush;
        RenderBrush selectionNoteBrush;
        RenderBrush attackBrush;
        RenderBrush iconTransparentBrush;
        RenderBrush dashedLineBrush;
        RenderBitmap bmpLoop;
        RenderBitmap bmpRelease;
        RenderBitmap bmpEffectExpanded;
        RenderBitmap bmpEffectCollapsed;
        RenderBitmap bmpSlide;
        RenderBitmap bmpSlideSmall;
        RenderBitmap bmpSnap;
        RenderBitmap[] bmpSnapResolution = new RenderBitmap[(int)SnapResolution.Max];
        RenderBitmap[] bmpEffects = new RenderBitmap[Note.EffectCount];
        RenderPath[] stopNoteGeometry = new RenderPath[MaxZoomLevel - MinZoomLevel + 1];
        RenderPath[] stopReleaseNoteGeometry = new RenderPath[MaxZoomLevel - MinZoomLevel + 1];
        RenderPath[] releaseNoteGeometry = new RenderPath[MaxZoomLevel - MinZoomLevel + 1];
        RenderPath[] slideNoteGeometry = new RenderPath[MaxZoomLevel - MinZoomLevel + 1];
        RenderPath seekGeometry;

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
            CreateDragSlideNoteTarget,
            DragSlideNoteTarget,
            DragNote,
            DragNewNote,
            DragSelection,
            AltZoom
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
            true,  // CreateDragSlideNoteTarget
            true,  // DragSlideNoteTarget
            true,  // DragNote
            false, // DragNewNote
            false, // DragSelection
            false, // AltZoom
        };

        int captureNoteIdx = 0;
        int captureNoteValue = 0;
        int mouseLastX = 0;
        int mouseLastY = 0;
        int captureMouseX = 0;
        int captureMouseY = 0;
        int playingNote = -1;
        int effectPatternIdx;
        int effectNoteIdx;
        int selectionFrameMin = -1;
        int selectionFrameMax = -1;
        int[] supportedEffects;
        bool captureThresholdMet = false;
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
        string noteTooltip = "";

        EditionMode editMode = EditionMode.None;

        // Pattern edit mode.
        int editChannel = -1;
        Instrument currentInstrument = null;
        Arpeggio currentArpeggio = null;

        // Envelope edit mode.
        Instrument editInstrument = null;
        int editEnvelope;
        int envelopeValueZoom   = 1;
        int envelopeValueOffset = 0;

        // Arpeggio edit mode
        Arpeggio editArpeggio = null;

        private bool IsSnappingAllowed => editMode == EditionMode.Channel;
        private bool IsSnappingEnabled => IsSnappingAllowed && snap;
        
        public Instrument CurrentInstrument { get => currentInstrument; set => currentInstrument = value; }
        public Arpeggio   CurrentArpeggio   { get => currentArpeggio;   set => currentArpeggio   = value; }

        public delegate void ManyPatternChange();
        public event ManyPatternChange ManyPatternChanged;
        public delegate void PatternChange(Pattern pattern);
        public event PatternChange PatternChanged;
        public delegate void EnvelopeResize();
        public event EnvelopeResize EnvelopeChanged;
        public delegate void ControlActivate();
        public event ControlActivate ControlActivated;
        public delegate void NotesPastedDelegate();
        public event NotesPastedDelegate NotesPasted;

        public PianoRoll()
        {
            UpdateRenderCoords();
        }
        
        private void UpdateRenderCoords()
        {
            var scaling = RenderTheme.MainWindowScaling;

            numOctaves                = DefaultNumOctaves;
            headerSizeY               = (int)((editMode == EditionMode.Channel || editMode == EditionMode.Enveloppe || editMode == EditionMode.Arpeggio ? 2 : 1) * DefaultHeaderSizeY * scaling);
            effectPanelSizeY          = (int)(DefaultEffectPanelSizeY * scaling);
            effectButtonSizeY         = (int)(DefaultEffectButtonSizeY * scaling);
            noteSizeX                 = (int)(ScaleForZoom(DefaultNoteSizeX) * scaling);        
            noteSizeY                 = (int)(DefaultNoteSizeY * scaling);
            noteAttackSizeX           = (int)(DefaultNoteAttackSizeX * scaling);
            releaseNoteSizeY          = (int)(DefaultReleaseNoteSizeY * scaling);
            envelopeMax               = (int)(DefaultEnvelopeMax * scaling);      
            whiteKeySizeY             = (int)(DefaultWhiteKeySizeY * scaling);    
            whiteKeySizeX             = (int)(DefaultWhiteKeySizeX * scaling);    
            blackKeySizeY             = (int)(DefaultBlackKeySizeY * scaling);    
            blackKeySizeX             = (int)(DefaultBlackKeySizeX * scaling);
            effectIconPosX            = (int)(DefaultEffectIconPosX * scaling);
            effectIconPosY            = (int)(DefaultEffectIconPosY * scaling);
            snapIconPosX              = (int)(DefaultSnapIconPosX * scaling);
            snapIconPosY              = (int)(DefaultSnapIconPosY * scaling);
            effectNamePosX            = (int)(DefaultEffectNamePosX * scaling);
            effectNamePosY            = (int)(DefaultEffectNamePosY * scaling);
            effectIconSizeX           = (int)(DefaultEffectIconSizeX * scaling);
            effectValuePosTextOffsetY = (int)(DefaultEffectValuePosTextOffsetY * scaling);
            effectValueNegTextOffsetY = (int)(DefaultEffectValueNegTextOffsetY * scaling);
            bigTextPosX               = (int)(DefaultBigTextPosX * scaling);
            bigTextPosY               = (int)(DefaultBigTextPosY * scaling);
            tooltipTextPosX           = (int)(DefaultTooltipTextPosX * scaling);
            tooltipTextPosY           = (int)(DefaultTooltipTextPosY * scaling);
            dpcmTextPosX              = (int)(DefaultDPCMTextPosX * scaling);
            dpcmTextPosY              = (int)(DefaultDPCMTextPosY * scaling);
            octaveNameOffsetY         = (int)(DefaultOctaveNameOffsetY * scaling);
            slideIconPosX             = (int)(DefaultSlideIconPosX * scaling);
            slideIconPosY             = (int)(DefaultSlideIconPosY * scaling);
            envelopeSizeY             = DefaultEnvelopeSizeY * envelopeValueZoom * scaling;    
            octaveSizeY               = 12 * noteSizeY;
            numNotes                  = numOctaves * 12;
            virtualSizeY              = numNotes * noteSizeY;
            barSizeX                  = noteSizeX * (Song == null ? 16  : Song.BarLength);
            headerAndEffectSizeY      = headerSizeY + (showEffectsPanel ? effectPanelSizeY : 0);
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
            envelopeValueZoom = envelope == Envelope.Volume || envelope == Envelope.DutyCycle || envelope == Envelope.N163Waveform ? 2 : 1;
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
            editEnvelope = Envelope.Arpeggio;
            editInstrument = null;
            editArpeggio = arpeggio;
            showEffectsPanel = false;
            noteTooltip = "";
            envelopeValueZoom = 1;
            envelopeValueOffset = 0;

            ClearSelection();
            UpdateRenderCoords();
            CenterEnvelopeScroll(arpeggio.Envelope, Envelope.Arpeggio);
            ClampScroll();
            ConditionalInvalidate();
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

        private void CenterEnvelopeScroll(Envelope envelope, int envelopeType, Instrument instrument = null)
        {
            var maxNumNotes = Width / DefaultNoteSizeX;

            if (envelope.Length == 0)
                zoomLevel = DefaultEnvelopeZoomLevel;
            else
                zoomLevel = Utils.Clamp((int)Math.Floor(Math.Log(maxNumNotes / (float)envelope.Length, 2.0)), MinZoomLevel, MaxZoomLevel);

            UpdateRenderCoords();

            Envelope.GetMinMaxValue(instrument, envelopeType, out int min, out int max);

            int midY = virtualSizeY - ((min + max) / 2 + 64 / envelopeValueZoom) * (virtualSizeY / (128 / envelopeValueZoom));
            
            scrollX = 0;
            scrollY = midY - Height / 2;
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

        public void StartEditDPCMSamples()
        {
            editMode = EditionMode.DPCM;
            showEffectsPanel = false;
            zoomLevel = 0;
            noteTooltip = "";
            envelopeValueZoom = 1;
            envelopeValueOffset = 0;

            ClearSelection();
            UpdateRenderCoords();
            ClampScroll();
            ConditionalInvalidate();
        }

        private void ClampMinSnap()
        {
            if (App.Project.UsesFamiTrackerTempo)
                snapResolution = (SnapResolution)Math.Max((int)snapResolution, (int)SnapResolution.OneNote);
        }

        private Song Song
        {
            get { return App?.Song; }
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

        public bool IsEditingInstrument
        {
            get { return editMode == EditionMode.Enveloppe; }
        }

        public bool IsEditingArpeggio
        {
            get { return editMode == EditionMode.Arpeggio; }
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
            ClampMinSnap();
            ClearSelection();
            UpdateRenderCoords();
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
            selectionBgVisibleBrush   = g.CreateSolidBrush(Color.FromArgb(64, ThemeBase.LightGreyFillColor1));
            selectionBgInvisibleBrush = g.CreateSolidBrush(Color.FromArgb(16, ThemeBase.LightGreyFillColor1));
            selectionNoteBrush = g.CreateSolidBrush(ThemeBase.LightGreyFillColor1);
            attackBrush = g.CreateSolidBrush(Color.FromArgb(128, ThemeBase.BlackColor));
            iconTransparentBrush = g.CreateSolidBrush(Color.FromArgb(92, ThemeBase.LightGreyFillColor2));
            dashedLineBrush = g.CreateBitmapBrush(g.CreateBitmapFromResource("Dash"), false, true);
            bmpLoop = g.CreateBitmapFromResource("LoopSmallFill");
            bmpRelease = g.CreateBitmapFromResource("ReleaseSmallFill");
            bmpEffects[Note.EffectVolume] = g.CreateBitmapFromResource("VolumeSmall");
            bmpEffects[Note.EffectVibratoSpeed] = g.CreateBitmapFromResource("VibratoSmall");
            bmpEffects[Note.EffectVibratoDepth] = g.CreateBitmapFromResource("VibratoSmall");
            bmpEffects[Note.EffectFinePitch] = g.CreateBitmapFromResource("PitchSmall");
            bmpEffects[Note.EffectSpeed] = g.CreateBitmapFromResource("SpeedSmall");
            bmpEffects[Note.EffectFdsModDepth] = g.CreateBitmapFromResource("ModSmall");
            bmpEffects[Note.EffectFdsModSpeed] = g.CreateBitmapFromResource("ModSmall");
            bmpEffectExpanded = g.CreateBitmapFromResource("ExpandedSmall");
            bmpEffectCollapsed = g.CreateBitmapFromResource("CollapsedSmall");
            bmpSlide = g.CreateBitmapFromResource("Slide");
            bmpSlideSmall = g.CreateBitmapFromResource("SlideSmall");
            bmpSnap = g.CreateBitmapFromResource("Snap");
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
                int x = (int)(noteSizeX * (float)Math.Pow(2.0, z) - 1);

                stopNoteGeometry[idx] = g.CreateConvexPath(new[]
                {
                    new Point(0, 0),
                    new Point(0, noteSizeY),
                    new Point(x, noteSizeY / 2)
                });

                releaseNoteGeometry[idx] = g.CreateConvexPath(new[]
                {
                    new Point(0, 0),
                    new Point(0, noteSizeY),
                    new Point(x + 1, noteSizeY - noteSizeY / 2 + releaseNoteSizeY / 2),
                    new Point(x + 1, noteSizeY / 2 - releaseNoteSizeY / 2)
                });

                stopReleaseNoteGeometry[idx] = g.CreateConvexPath(new[]
                {
                    new Point(0, noteSizeY / 2 - releaseNoteSizeY / 2),
                    new Point(0, noteSizeY / 2 + releaseNoteSizeY / 2),
                    new Point(x, noteSizeY / 2)
                });

                slideNoteGeometry[idx] = g.CreateConvexPath(new[]
                {
                    new Point(0, 0),
                    new Point(x + 1, 0),
                    new Point(x + 1,  noteSizeY)
                });
            }

            seekGeometry = g.CreateConvexPath(new[]
            {
                new Point(-headerSizeY / 2, 1),
                new Point(0, headerSizeY - 2),
                new Point( headerSizeY / 2, 1)
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
            Utils.DisposeAndNullify(ref selectionBgVisibleBrush);
            Utils.DisposeAndNullify(ref selectionBgInvisibleBrush);
            Utils.DisposeAndNullify(ref selectionNoteBrush);
            Utils.DisposeAndNullify(ref attackBrush);
            Utils.DisposeAndNullify(ref iconTransparentBrush);
            Utils.DisposeAndNullify(ref dashedLineBrush);
            Utils.DisposeAndNullify(ref bmpLoop);
            Utils.DisposeAndNullify(ref bmpRelease);
            Utils.DisposeAndNullify(ref bmpEffectExpanded);
            Utils.DisposeAndNullify(ref bmpEffectCollapsed);
            Utils.DisposeAndNullify(ref bmpSlide);
            Utils.DisposeAndNullify(ref bmpSlideSmall);
            Utils.DisposeAndNullify(ref bmpSnap);

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

                Utils.DisposeAndNullify(ref stopNoteGeometry[idx]);
                Utils.DisposeAndNullify(ref releaseNoteGeometry[idx]);
                Utils.DisposeAndNullify(ref stopReleaseNoteGeometry[idx]);
                Utils.DisposeAndNullify(ref slideNoteGeometry[idx]);
            }

            Utils.DisposeAndNullify(ref seekGeometry);
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
                    0,
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

        class RenderArea
        {
            public int maxVisibleNote;
            public int minVisibleNote;
            public int maxVisibleOctave;
            public int minVisibleOctave;
            public int minVisiblePattern;
            public int maxVisiblePattern;
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
                    g.DrawLine(0, 0, 0, headerAndEffectSizeY, theme.DarkGreyLineBrush1);
                    g.DrawBitmap(bmpLoop, effectIconPosX + 1, effectIconPosY);
                    g.PopTransform();
                }
                if (env.Release >= 0)
                {
                    g.PushTranslation(env.Release * noteSizeX - scrollX, 0);
                    g.DrawLine(0, 0, 0, headerAndEffectSizeY, theme.DarkGreyLineBrush1);
                    g.DrawBitmap(bmpRelease, effectIconPosX + 1, effectIconPosY);
                    g.PopTransform();
                }
                if (env.Length > 0)
                {
                    g.PushTranslation(env.Length * noteSizeX - scrollX, 0);
                    g.DrawLine(0, 0, 0, headerAndEffectSizeY, theme.DarkGreyLineBrush1);
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
                        g.DrawLine(x, 0, x, headerSizeY / 2, theme.DarkGreyLineBrush1, 1.0f);
                    if (zoomLevel >= 1 && n != env.Length)
                        g.DrawText(n.ToString(), ThemeBase.FontMediumCenter, x, effectNamePosY, theme.LightGreyFillBrush1, noteSizeX);
                }

                g.DrawLine(0, headerSizeY / 2 - 1, Width, headerSizeY / 2 - 1, theme.DarkGreyLineBrush1);
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
                    var sx = Song.GetPatternLength(p)    * noteSizeX;
                    int px = Song.GetPatternStartNote(p) * noteSizeX - scrollX;
                    if (p != 0)
                        g.DrawLine(px, 0, px, headerSizeY, theme.DarkGreyLineBrush1, 3.0f);
                    var pattern = Song.Channels[editChannel].PatternInstances[p];
                    g.DrawText(p.ToString(), ThemeBase.FontMediumCenter, px, effectNamePosY, theme.LightGreyFillBrush1, sx);
                    if (pattern != null)
                        g.DrawText(pattern.Name, ThemeBase.FontMediumCenter, px, effectNamePosY + headerSizeY / 2, theme.BlackBrush, sx);
                }

                int maxX = Song.GetPatternStartNote(a.maxVisiblePattern) * noteSizeX - scrollX;
                g.DrawLine(maxX, 0, maxX, Height, theme.DarkGreyLineBrush1, 3.0f);
                g.DrawLine(0, headerSizeY / 2 - 1, Width, headerSizeY / 2 - 1, theme.DarkGreyLineBrush1);

                // Draw the effect icons.
                for (int p = a.minVisiblePattern; p < a.maxVisiblePattern; p++)
                {
                    var pattern = Song.Channels[editChannel].PatternInstances[p];
                    if (pattern != null)
                    {
                        var patternLen = Song.GetPatternLength(p);
                        var patternX   = Song.GetPatternStartNote(p) * noteSizeX - scrollX;

                        foreach (var kv in pattern.Notes)
                        {
                            var n    = kv.Key;
                            var note = kv.Value;

                            if (n >= patternLen)
                                break;

                            for (int i = Note.EffectCount - 1; i >= 0; i--)
                            {
                                if (showEffectsPanel && i == selectedEffectIdx)
                                    continue;

                                if (note.HasValidEffectValue(i))
                                {
                                    int iconX = patternX + n * noteSizeX + noteSizeX / 2 - effectIconSizeX / 2;
                                    int iconY = headerSizeY / 2 + effectIconPosY;
                                    g.FillRectangle(iconX, iconY, iconX + effectIconSizeX, iconY + effectIconSizeX, showEffectsPanel ? iconTransparentBrush : theme.LightGreyFillBrush2);
                                    g.DrawBitmap(bmpEffects[i], iconX, iconY, showEffectsPanel ? 0.3f : 1.0f);
                                    break;
                                }
                            }
                        }
                    }
                }

                // When showing the effect panel, make sure to draw the selectd effect icons on top of everything.
                if (showEffectsPanel)
                {
                    for (int p = a.minVisiblePattern; p < a.maxVisiblePattern; p++)
                    {
                        var pattern = Song.Channels[editChannel].PatternInstances[p];
                        if (pattern != null)
                        {
                            var patternLen = Song.GetPatternLength(p);
                            var patternX   = Song.GetPatternStartNote(p) * noteSizeX - scrollX;

                            foreach (var kv in pattern.Notes)
                            {
                                if (kv.Key >= patternLen)
                                    break;

                                var note = kv.Value;
                                if (selectedEffectIdx >= 0 && note.HasValidEffectValue(selectedEffectIdx))
                                {
                                    int iconX = patternX + kv.Key * noteSizeX + noteSizeX / 2 - effectIconSizeX / 2;
                                    int iconY = headerSizeY / 2 + effectIconPosY;
                                    g.FillRectangle(iconX, iconY, iconX + effectIconSizeX, iconY + effectIconSizeX, theme.LightGreyFillBrush2);
                                    g.DrawBitmap(bmpEffects[selectedEffectIdx], iconX, iconY);
                                }
                            }
                        }
                    }
                }
            }

            g.DrawLine(0, headerSizeY - 1, Width, headerSizeY - 1, theme.BlackBrush);

            if (((editMode == EditionMode.Enveloppe || editMode == EditionMode.Arpeggio) && editEnvelope < Envelope.RegularCount) || (editMode == EditionMode.Channel))
            {
                var seekFrame = editMode == EditionMode.Enveloppe|| editMode == EditionMode.Arpeggio ? App.GetEnvelopeFrame(editInstrument, editEnvelope, editMode == EditionMode.Arpeggio) : App.CurrentFrame;
                if (seekFrame >= 0)
                {
                    g.PushTranslation(seekFrame * noteSizeX - scrollX, 0);
                    g.FillAndDrawConvexPath(seekGeometry, seekBarBrush, theme.BlackBrush);
                    g.DrawLine(0, headerSizeY / 2, 0, headerSizeY, seekBarBrush, 3);
                    g.PopTransform();
                }
            }

            g.PopClip();
            g.PopTransform();
        }

        private void RenderEffectList(RenderGraphics g)
        {
            g.PushClip(0, 0, whiteKeySizeX, headerAndEffectSizeY);
            g.FillRectangle(0, 0, whiteKeySizeX, Height, whiteKeyBrush);
            g.DrawLine(whiteKeySizeX - 1, 0, whiteKeySizeX - 1, headerAndEffectSizeY, theme.BlackBrush);

            // Effect icons
            if (editMode == EditionMode.Channel)
            { 
                g.DrawBitmap(showEffectsPanel ? bmpEffectExpanded : bmpEffectCollapsed, effectIconPosX, effectIconPosY);

                if (IsSnappingAllowed)
                {
                    g.DrawBitmap(bmpSnapResolution[(int)snapResolution], whiteKeySizeX - (int)bmpSnap.Size.Width * 2 - snapIconPosX - 1, snapIconPosY, snap ? 1.0f : 0.3f);
                    g.DrawBitmap(bmpSnap, whiteKeySizeX - (int)bmpSnap.Size.Width * 1 - snapIconPosX * 1 - 1, snapIconPosY, snap ? 1.0f : 0.3f);
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
                        g.DrawText(Note.EffectNames[effectIdx], selectedEffectIdx == effectIdx ? ThemeBase.FontSmallBold : ThemeBase.FontSmall, effectNamePosX, effectNamePosY, theme.BlackBrush);
                        g.PopTransform();
                    }

                    g.PushTranslation(0, effectButtonY);
                    g.DrawLine(0, -1, whiteKeySizeX, -1, theme.BlackBrush);
                    g.PopTransform();
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

            int playOctave = -1;
            int playNote = -1;
            var draggingNote = captureOperation == CaptureOperation.DragNote || captureOperation == CaptureOperation.DragNewNote;
            var dragOctave = (dragLastNoteValue - 1) / 12;
            var dragNote   = (dragLastNoteValue - 1) % 12;

            if (playingNote > 0)
            {
                playOctave = (playingNote - 1) / 12;
                playNote   = (playingNote - 1) - playOctave * 12;

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
                        g.DrawLine(0, y, whiteKeySizeX, y, theme.DarkGreyLineBrush1);
                    else if (j == 5)
                        g.DrawLine(0, y, whiteKeySizeX, y, theme.DarkGreyLineBrush2);
                }

                g.DrawText("C" + i, ThemeBase.FontSmall, 1, octaveBaseY - octaveNameOffsetY, theme.BlackBrush);
            }

            g.DrawLine(whiteKeySizeX - 1, 0, whiteKeySizeX - 1, Height, theme.DarkGreyLineBrush1);

            g.PopClip();
            g.PopTransform();
        }

        private void RenderEffectPanel(RenderGraphics g, RenderArea a)
        {
            if (editMode == EditionMode.Channel && showEffectsPanel)
            {
                g.PushTranslation(whiteKeySizeX, headerSizeY);
                g.PushClip(0, 0, Width, effectPanelSizeY);
                g.Clear(ThemeBase.DarkGreyFillColor1);

                var channel = Song.Channels[editChannel];

                // Draw the effects current value rectangles. Not all effects need this.
                if (selectedEffectIdx >= 0 && Note.EffectWantsPreviousValue(selectedEffectIdx))
                {
                    var lastFrame = -1;
                    var lastValue = channel.GetLastValidEffectValue(a.minVisiblePattern - 1, selectedEffectIdx);
                    var minValue  = Note.GetEffectMinValue(Song, selectedEffectIdx);
                    var maxValue  = Note.GetEffectMaxValue(Song, selectedEffectIdx);

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
                                var effectMinValue = Note.GetEffectMinValue(Song, selectedEffectIdx);
                                var effectMaxValue = Note.GetEffectMaxValue(Song, selectedEffectIdx);
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

                        g.DrawLine(x, 0, x, headerAndEffectSizeY, theme.DarkGreyLineBrush1);
                        g.DrawLine(0, headerAndEffectSizeY - 1, Width, headerAndEffectSizeY - 1, theme.DarkGreyLineBrush1);
                    }
                }

                // Thick vertical bars
                for (int p = a.minVisiblePattern; p < a.maxVisiblePattern; p++)
                {
                    int x = Song.GetPatternStartNote(p) * noteSizeX - scrollX;
                    if (p != 0) g.DrawLine(x, 0, x, Height, theme.DarkGreyLineBrush1, 3.0f);
                }

                int maxX = Song.GetPatternStartNote(a.maxVisiblePattern) * noteSizeX - scrollX;
                g.DrawLine(maxX, 0, maxX, Height, theme.DarkGreyLineBrush1, 3.0f);

                int seekX = App.CurrentFrame * noteSizeX - scrollX;
                g.DrawLine(seekX, 0, seekX, effectPanelSizeY, seekBarBrush, 3);

                g.DrawLine(0, effectPanelSizeY - 1, Width, effectPanelSizeY - 1, theme.BlackBrush);
                g.PopClip();
                g.PopTransform();
            }
        }

        private Note[] GetSelectedNotes(bool clone = true)
        {
            if (!IsSelectionValid())
                return null;

            var notes = new Note[selectionFrameMax - selectionFrameMin + 1];

            TransformNotes(selectionFrameMin, selectionFrameMax, false, (note, idx) =>
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

            TransformNotes(selectionFrameMin, selectionFrameMax, false, (note, idx) =>
            {
                if (note != null && !note.IsEmpty)
                    notes[idx + offset] = note.Clone();
                return note;
            });

            return notes;
        }

        private void CopyNotes()
        {
            ClipboardUtils.SaveNotes(App.Project, GetSelectedNotes(false), editChannel == Channel.Dpcm);
        }

        private void CutNotes()
        {
            CopyNotes();
            DeleteSelectedNotes();
        }

        private void ReplaceNotes(Note[] notes, int startFrameIdx, bool doTransaction, bool pasteNotes = true, bool pasteVolume = true, bool pasteFx = true, bool mix = false)
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
                        note.Instrument = editChannel == Channel.Dpcm || !Song.Channels[editChannel].SupportsInstrument(newNote.Instrument) ? null : newNote.Instrument;
                        note.Slide = newNote.Slide;
                        note.Flags = newNote.Flags;
                    }
                }
                if (pasteVolume)
                {
                    if (!mix || !note.HasVolume && newNote.HasVolume)
                    {
                        note.HasVolume = false;
                        if (channel.SupportsEffect(Note.EffectVolume) && newNote.HasVolume) note.Volume = newNote.Volume;
                    }
                }
                if (pasteFx)
                {
                    if (!mix || !note.HasSpeed && newNote.HasSpeed)
                    {
                        note.HasSpeed = false;
                        if (channel.SupportsEffect(Note.EffectSpeed) && newNote.HasSpeed) note.Speed = newNote.Speed;
                    }
                    if (!mix || !note.HasFinePitch && newNote.HasFinePitch)
                    {
                        note.HasFinePitch = false;
                        if (channel.SupportsEffect(Note.EffectFinePitch) && newNote.HasFinePitch) note.FinePitch = newNote.FinePitch;
                    }
                    if (!mix || !note.HasVibrato && newNote.HasVibrato)
                    {
                        note.HasVibrato = false;
                        if (channel.SupportsEffect(Note.EffectVibratoDepth) && newNote.HasVibrato) note.RawVibrato = newNote.RawVibrato;
                    }
                    if (!mix || !note.HasFdsModSpeed && newNote.HasFdsModSpeed)
                    {
                        note.HasFdsModSpeed = false;
                        if (channel.SupportsEffect(Note.EffectFdsModSpeed) && newNote.HasFdsModSpeed) note.FdsModSpeed = newNote.FdsModSpeed;
                    }
                    if (!mix || !note.HasFdsModDepth && newNote.HasFdsModDepth)
                    {
                        note.HasFdsModDepth = false;
                        if (channel.SupportsEffect(Note.EffectFdsModDepth) && newNote.HasFdsModDepth) note.FdsModDepth = newNote.FdsModDepth;
                    }
                }

                return note;
            });

            SetSelection(startFrameIdx, startFrameIdx + notes.Length - 1);
        }

        private void PasteNotes(bool pasteNotes = true, bool pasteVolume = true, bool pasteFx = true, bool mix = false)
        {
            if (!IsSelectionValid())
                return;

            var missingInstruments = ClipboardUtils.ContainsMissingInstrumentsOrSamples(App.Project, true, out var missingSamples);

            bool createMissingInstrument = false;
            if (missingInstruments)
                createMissingInstrument = PlatformUtils.MessageBox($"You are pasting notes referring to unknown instruments. Do you want to create the missing instrument?", "Paste", MessageBoxButtons.YesNo) == DialogResult.Yes;

            bool createMissingSamples = false;
            if (missingSamples && editChannel == Channel.Dpcm)
                createMissingSamples = PlatformUtils.MessageBox($"You are pasting notes referring to unmapped DPCM samples. Do you want to create the missing samples?", "Paste", MessageBoxButtons.YesNo) == DialogResult.Yes;

            App.UndoRedoManager.BeginTransaction(createMissingInstrument || createMissingSamples ? TransactionScope.Project : TransactionScope.Channel, Song.Id, editChannel);

            var notes = ClipboardUtils.LoadNotes(App.Project, createMissingInstrument, createMissingSamples);

            if (notes == null)
            {
                App.UndoRedoManager.AbortTransaction();
                return;
            }

            ReplaceNotes(notes, selectionFrameMin, false, pasteNotes, pasteVolume, pasteFx, mix);
            NotesPasted?.Invoke();
            App.UndoRedoManager.EndTransaction();
        }

        private sbyte[] GetSelectedEnvelopeValues()
        {
            if (!IsSelectionValid())
                return null;
            
            var values = new sbyte[selectionFrameMax - selectionFrameMin + 1];

            for (int i = selectionFrameMin; i <= selectionFrameMax; i++)
                values[i - selectionFrameMin] = EditEnvelope.Values[i];

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

            ReplaceEnvelopeValues(values, selectionFrameMin);
        }

        public bool CanCopy  => showSelection && IsSelectionValid();
        public bool CanPaste => showSelection && IsSelectionValid() && (editMode == EditionMode.Channel && ClipboardUtils.ConstainsNotes || editMode == EditionMode.Enveloppe && ClipboardUtils.ConstainsEnvelope);

        public void Copy()
        {
            if (editMode == EditionMode.Channel)
                CopyNotes();
            else if (editMode == EditionMode.Enveloppe) // MATTT: Arpeggion copy/paste
                CopyEnvelopeValues();
        }

        public void Cut()
        {
            if (editMode == EditionMode.Channel)
                CutNotes();
            else if (editMode == EditionMode.Enveloppe) // MATTT: Arpeggion copy/paste
                CutEnvelopeValues();
        }

        public void Paste()
        {
            AbortCaptureOperation();

            if (editMode == EditionMode.Channel)
                PasteNotes();
            else if (editMode == EditionMode.Enveloppe) // MATTT: Arpeggion copy/paste
                PasteEnvelopeValues();
        }

        public void PasteSpecial()
        {
            if (editMode == EditionMode.Channel)
            {
                AbortCaptureOperation();

                var dlg = new PasteSpecialDialog(App.MainWindowBounds);

                if (dlg.ShowDialog() == DialogResult.OK)
                    PasteNotes(dlg.PasteNotes, dlg.PasteVolumes, dlg.PasteEffects, dlg.PasteMix);
            }
        }

        private bool IsNoteSelected(int patternIdx, int noteIdx)
        {
            int absoluteNoteIdx = Song.GetPatternStartNote(patternIdx, noteIdx);
            return IsSelectionValid() && absoluteNoteIdx >= selectionFrameMin && absoluteNoteIdx <= selectionFrameMax;
        }

        private bool IsEnvelopeValueSelected(int idx)
        {
            return IsSelectionValid() && idx >= selectionFrameMin && idx <= selectionFrameMax;
        }

        private void DrawSelectionRect(RenderGraphics g, int height)
        {
            if (IsSelectionValid())
            {
                g.FillRectangle(
                    (selectionFrameMin + 0) * noteSizeX - scrollX, 0,
                    (selectionFrameMax + 1) * noteSizeX - scrollX, height, showSelection ? selectionBgVisibleBrush : selectionBgInvisibleBrush);
            }
        }

        private void RenderNote(RenderGraphics g, Channel channel, bool selected, Color color, Arpeggio arpeggio, int p0, int i0, Note n0, bool released, int p1, int i1)
        {
            int x = Song.GetPatternStartNote(p0, i0) * noteSizeX - scrollX;
            int y = virtualSizeY - n0.Value * noteSizeY - scrollY;
            int sy = released ? releaseNoteSizeY : noteSizeY;

            if (n0.IsSlideNote)
            {
                if (channel.ComputeSlideNoteParams(n0, p0, i0, Song.FamitrackerSpeed, Song.NativeTempoNTSC, null, out _, out _, out int duration))
                {
                    int slideSizeX = duration;
                    int slideSizeY = n0.SlideNoteTarget - n0.Value;

                    g.PushTransform(x, y + (slideSizeY > 0 ? 0 : noteSizeY), slideSizeX, -slideSizeY);
                    g.FillConvexPath(slideNoteGeometry[zoomLevel - MinZoomLevel], g.GetSolidBrush(color, 1.0f, 0.2f), true);
                    g.PopTransform();
                }
            }

            if (released)
                y += noteSizeY / 2 - releaseNoteSizeY / 2;

            g.PushTranslation(x, y);

            int noteLen = Song.GetPatternStartNote(p1, i1) - Song.GetPatternStartNote(p0, i0);
            int sx = noteLen * noteSizeX;
            int iconX = slideIconPosX;

            g.FillRectangle(0, 0, sx, sy, g.GetVerticalGradientBrush(color, sy, 0.8f));
            g.DrawRectangle(0, 0, sx, sy, selected ? selectionNoteBrush : theme.BlackBrush, selected ? 2 : 1);

            if (n0.HasAttack && sx > noteAttackSizeX + 4)
            {
                g.FillRectangle(slideIconPosX, slideIconPosX, slideIconPosX + noteAttackSizeX, sy - slideIconPosX + 1, attackBrush);
                iconX += noteAttackSizeX + slideIconPosX;
            }

            if (n0.Arpeggio != null)
            {
                var offsets = n0.Arpeggio.GetChordOffsets();
                foreach (var offset in offsets)
                {
                    g.PushTranslation(0, offset * -noteSizeY);
                    g.FillRectangle(0, 0, sx, sy, g.GetSolidBrush(n0.Arpeggio.Color, 1.0f, 0.2f));
                    g.PopTransform();
                }
            }

            g.PopTransform();
        }

        private void RenderReleaseStopNote(RenderGraphics g, int value, bool selected, Color color, Arpeggio arpeggio, int p1, int i1, Note n1, bool released)
        {
            int x = Song.GetPatternStartNote(p1, i1) * noteSizeX - scrollX;
            int y = virtualSizeY - value * noteSizeY - scrollY;

            var paths = n1.IsStop ? (released ? stopReleaseNoteGeometry : stopNoteGeometry) : releaseNoteGeometry;

            g.PushTranslation(x, y);
            g.FillAndDrawConvexPath(paths[zoomLevel - MinZoomLevel], g.GetVerticalGradientBrush(color, noteSizeY, 0.8f), selected ? selectionNoteBrush : theme.BlackBrush, selected ? 2 : 1);

            if (arpeggio != null)
            {
                var offsets = arpeggio.GetChordOffsets();
                foreach (var offset in offsets)
                {
                    g.PushTranslation(0, offset * -noteSizeY);
                    g.FillConvexPath(paths[zoomLevel - MinZoomLevel], g.GetSolidBrush(arpeggio.Color, 1.0f, 0.2f), true);
                    g.PopTransform();
                }
            }

            g.PopTransform();

        }

        private void RenderNotes(RenderGraphics g, RenderArea a)
        {
            g.PushTranslation(whiteKeySizeX, headerAndEffectSizeY);
            g.PushClip(0, 0, Width, Height);
            g.Clear(ThemeBase.DarkGreyFillColor2);

            if (editMode == EditionMode.Channel ||
                editMode == EditionMode.DPCM)
            {
                int maxX = editMode == EditionMode.Channel ? Song.GetPatternStartNote(a.maxVisiblePattern) * noteSizeX - scrollX : Width;

                // Draw the note backgrounds
                for (int i = a.minVisibleOctave; i < a.maxVisibleOctave; i++)
                {
                    int octaveBaseY = (virtualSizeY - octaveSizeY * i) - scrollY;

                    for (int j = 0; j < 12; j++)
                    {
                        int y = octaveBaseY - j * noteSizeY;
                        if (IsBlackKey(j))
                            g.FillRectangle(0, y - noteSizeY, maxX, y, theme.DarkGreyFillBrush1);
                        if (i * 12 + j != numNotes)
                            g.DrawLine(0, y, maxX, y, theme.DarkGreyLineBrush2);
                    }
                }

                DrawSelectionRect(g, Height);

                if (editMode == EditionMode.Channel)
                {
                    // Draw the vertical bars.
                    for (int p = a.minVisiblePattern; p < a.maxVisiblePattern; p++)
                    {
                        var patternLen = Song.GetPatternLength(p);
                        var barLength = Song.GetPatternBarLength(p);

                        if (Song.UsesFamiStudioTempo)
                        {
                            var noteLength = Song.GetPatternNoteLength(p);

                            for (int i = p == 0 ? 1 : 0; i < patternLen; i++)
                            {
                                int x = (Song.GetPatternStartNote(p) + i) * noteSizeX - scrollX;

                                if (i % barLength == 0) 
                                    g.DrawLine(x, 0, x, Height, theme.DarkGreyLineBrush1, i == 0 ? 3.0f : 1.0f);
                                else if (i % noteLength == 0)
                                    g.DrawLine(x, 0, x, Height, theme.DarkGreyLineBrush2);
                                else if (zoomLevel >= DrawFrameZoomLevel)
                                    g.DrawLine(x, 0, x, Height, dashedLineBrush /*theme.DarkGreyLineBrush3*/);
                            }
                        }
                        else
                        {
                            for (int i = p == 0 ? 1 : 0; i < patternLen; i++)
                            {
                                int x = (Song.GetPatternStartNote(p) + i) * noteSizeX - scrollX;

                                if (i % barLength == 0)
                                    g.DrawLine(x, 0, x, Height, theme.DarkGreyLineBrush1, i == 0 ? 3.0f : 1.0f);
                                else if (zoomLevel >= -1)
                                    g.DrawLine(x, 0, x, Height, theme.DarkGreyLineBrush2);
                            }
                        }
                    }

                    g.DrawLine(maxX, 0, maxX, Height, theme.DarkGreyLineBrush1, 3.0f);

                    int seekX = App.CurrentFrame * noteSizeX - scrollX;
                    g.DrawLine(seekX, 0, seekX, Height, seekBarBrush, 3);

                    // Pattern drawing.
                    for (int c = 0; c < Song.Channels.Length; c++)
                    {
                        var channel = Song.Channels[c];
                        var isActiveChannel = c == editChannel;

                        if (isActiveChannel || (App.GhostChannelMask & (1 << c)) != 0)
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
                                color = n0.Instrument == null ? ThemeBase.LightGreyFillColor1 : n0.Instrument.Color;
                                arpeggio = n0.Arpeggio;
                                if (!isActiveChannel) color = Color.FromArgb((int)(color.A * 0.2f), color);
                            }

                            for (int p1 = a.minVisiblePattern; p1 < a.maxVisiblePattern; p1++)
                            {
                                var pattern = Song.Channels[c].PatternInstances[p1];

                                if (pattern == null)
                                    continue;

                                var patternLen = Song.GetPatternLength(p1);

                                foreach (var kv in pattern.Notes)
                                {
                                    var i1 = kv.Key;
                                    var n1 = kv.Value.Clone();

                                    if (i1 >= patternLen)
                                        break;

                                    if (n0.IsValid && n1.IsValid && ((n0.Value >= a.minVisibleNote && n0.Value <= a.maxVisibleNote) || n0.IsSlideNote))
                                        RenderNote(g, channel, selected, color, arpeggio, p0, i0, n0, released, p1, i1);

                                    if (n1.IsStop || n1.IsRelease)
                                    {
                                        selected = IsNoteSelected(p1, i1) && isActiveChannel;
                                        int value = n0.Value >= Note.MusicalNoteMin && n0.Value <= Note.MusicalNoteMax ? n0.Value : 49; // C4 by default.

                                        if (value >= a.minVisibleNote && value <= a.maxVisibleNote)
                                            RenderReleaseStopNote(g, value, selected, color, arpeggio, p1, i1, n1, released);

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
                                            if (p0 < 0 || i0 >= Song.GetPatternLength(p0))
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
                                        color = n0.Instrument == null ? ThemeBase.LightGreyFillColor1 : n0.Instrument.Color;
                                        arpeggio = n0.Arpeggio;
                                        if (!isActiveChannel) color = Color.FromArgb((int)(color.A * 0.2f), color);
                                    }
                                }
                            }

                            if (n0.IsValid && ((n0.Value >= a.minVisibleNote && n0.Value <= a.maxVisibleNote) || n0.IsSlideNote))
                            {
                                RenderNote(g, channel, selected, color, arpeggio, p0, i0, n0, released, Math.Min(Song.Length, a.maxVisiblePattern + 1), 0);
                            }
                        }
                    }

                    var channelType = Song.Channels[editChannel].Type;
                    var channelName = Song.Channels[editChannel].Name;

                    if (channelType >= Channel.ExpansionAudioStart)
                        channelName += $" ({Song.Project.ExpansionAudioName})";

                    g.DrawText($"Editing Channel {channelName}", ThemeBase.FontBig, bigTextPosX, bigTextPosY, whiteKeyBrush);
                }
                else
                {
                    for (int i = 0; i < Note.MusicalNoteMax; i++)
                    {
                        var mapping = App.Project.GetDPCMMapping(i);
                        if (mapping != null)
                        {
                            var y = virtualSizeY - i  * noteSizeY - scrollY;

                            g.PushTranslation(0, y);
                            g.FillAndDrawRectangle(0, 0, Width - whiteKeySizeX, noteSizeY, g.GetVerticalGradientBrush(ThemeBase.LightGreyFillColor1, noteSizeY, 0.8f), theme.BlackBrush);
                            if (mapping.Sample != null)
                            {
                                string text = $"{mapping.Sample.Name} (Pitch: {mapping.Pitch}, Loop: {mapping.Loop})";
                                g.DrawText(text, ThemeBase.FontSmall, dpcmTextPosX, dpcmTextPosY, theme.BlackBrush);
                            }
                            g.PopTransform();
                        }
                    }

                    g.DrawText($"Editing DPCM Samples ({App.Project.GetTotalSampleSize()} / {Project.MaxSampleSize} Bytes)", ThemeBase.FontBig, bigTextPosX, bigTextPosY, whiteKeyBrush);
                }
            }
            else if (editMode == EditionMode.Enveloppe || editMode == EditionMode.Arpeggio)
            {
                // Draw the enveloppe value backgrounds
                int maxValue = 128 / envelopeValueZoom;
                int midValue =  64 / envelopeValueZoom;
                int maxVisibleValue = maxValue - Math.Min((int)Math.Floor(scrollY / envelopeSizeY), maxValue);
                int minVisibleValue = maxValue - Math.Max((int)Math.Ceiling((scrollY + Height) / envelopeSizeY), 0);

                var env = EditEnvelope;
                var spacing = editEnvelope == Envelope.DutyCycle ? 4 : (editEnvelope == Envelope.Arpeggio ? 12 : 16); // MATTT: editEnvelope shouldnt be accessed

                for (int i = minVisibleValue; i <= maxVisibleValue; i++)
                {
                    var value = i - 64;
                    var y = (virtualSizeY - envelopeSizeY * i) - scrollY;
                    g.DrawLine(0, y, env.Length * noteSizeX - scrollX, y, theme.DarkGreyLineBrush2, (value % spacing) == 0 ? 3 : 1);
                }

                DrawSelectionRect(g, Height);

                // Draw the vertical bars.
                for (int b = 0; b < env.Length; b++)
                {
                    int x = b * noteSizeX - scrollX;
                    if (b != 0) g.DrawLine(x, 0, x, Height, theme.DarkGreyLineBrush2);
                }

                if (env.Loop >= 0)
                    g.DrawLine(env.Loop * noteSizeX - scrollX, 0, env.Loop * noteSizeX - scrollX, Height, theme.DarkGreyLineBrush1);
                if (env.Release >= 0)
                    g.DrawLine(env.Release * noteSizeX - scrollX, 0, env.Release * noteSizeX - scrollX, Height, theme.DarkGreyLineBrush1);
                if (env.Length > 0)
                    g.DrawLine(env.Length * noteSizeX - scrollX, 0, env.Length * noteSizeX - scrollX, Height, theme.DarkGreyLineBrush1);

                if ((editMode == EditionMode.Enveloppe || editMode == EditionMode.Arpeggio) && editEnvelope < Envelope.RegularCount)
                {
                    var seekFrame = App.GetEnvelopeFrame(editInstrument, editEnvelope, editMode == EditionMode.Arpeggio);
                    if (seekFrame >= 0)
                    {
                        var seekX = seekFrame * noteSizeX - scrollX;
                        g.DrawLine(seekX, 0, seekX, Height, seekBarBrush, 3);
                    }
                }

                if (editEnvelope == Envelope.Arpeggio)
                {
                    for (int i = 0; i < env.Length; i++)
                    {
                        var x = i * noteSizeX - scrollX;
                        var y = (virtualSizeY - envelopeSizeY * (env.Values[i] + midValue)) - scrollY;
                        var selected = IsEnvelopeValueSelected(i);
                        g.FillRectangle(x, y - envelopeSizeY, x + noteSizeX, y, g.GetVerticalGradientBrush(ThemeBase.LightGreyFillColor1, (int)envelopeSizeY, 0.8f));
                        g.DrawRectangle(x, y - envelopeSizeY, x + noteSizeX, y, theme.BlackBrush, selected ? 2 : 1);
                        if (zoomLevel >= 1)
                            g.DrawText(env.Values[i].ToString("+#;-#;0"), ThemeBase.FontSmallCenter, x, y - envelopeSizeY- effectValuePosTextOffsetY, theme.LightGreyFillBrush1, noteSizeX);
                    }
                }
                else
                {
                    for (int i = 0; i < env.Length; i++)
                    {
                        var center = editEnvelope == Envelope.FdsWaveform ? 32 : 0;
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
                            var brush  = drawOutside ? theme.LightGreyFillBrush1 : theme.BlackBrush;
                            var offset = drawOutside != val < center ? -effectValuePosTextOffsetY : effectValueNegTextOffsetY;

                            g.DrawText(val.ToString(), ThemeBase.FontSmallCenter, x, ty + offset, brush, noteSizeX);
                        }
                    }
                }

                if (editMode == EditionMode.Enveloppe)
                {
                    var envelopeString = Envelope.EnvelopeNames[editEnvelope];

                    if (editEnvelope == Envelope.Pitch)
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

            RenderHeader(g, a);
            RenderEffectList(g);
            RenderEffectPanel(g, a);
            RenderPiano(g, a);
            RenderNotes(g, a);
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
                        SetSelection(selectionFrameMin, selectionFrameMax);
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
            var minValue = Note.GetEffectMinValue(Song, selectedEffectIdx);
            var maxValue = Note.GetEffectMaxValue(Song, selectedEffectIdx);

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
                if (editInstrument.ExpansionType == Project.ExpansionFds)
                {
                    if (editEnvelope == Envelope.FdsWaveform)
                        editInstrument.FdsWavePreset = Envelope.WavePresetCustom;
                    if (editEnvelope == Envelope.FdsModulation)
                        editInstrument.FdsModPreset = Envelope.WavePresetCustom;
                }
                else if (editInstrument.ExpansionType == Project.ExpansionN163)
                {
                    if (editEnvelope == Envelope.N163Waveform)
                        editInstrument.N163WavePreset = Envelope.WavePresetCustom;
                }
            }
        }

        protected bool PointInRectangle(Rectangle rect, int x, int y)
        {
            return (x >= rect.Left && x <= rect.Right &&
                    y >= rect.Top  && y <= rect.Bottom);
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
                            App.PlayInstrumentNote(playingNote);
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
                            App.PlayInstrumentNote(playingNote);
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

            bool left  = e.Button.HasFlag(MouseButtons.Left);
            bool right = e.Button.HasFlag(MouseButtons.Right);
            bool foundNote = GetNoteForCoord(e.X, e.Y, out int patternIdx, out int noteIdx, out byte noteValue);

            if (editMode == EditionMode.DPCM && left && foundNote)
            {
                var mapping = App.Project.GetDPCMMapping(noteValue);
                if (left && mapping != null)
                {
                    var dlg = new PropertyDialog(PointToScreen(new Point(e.X, e.Y)), 160);
                    dlg.Properties.AddColoredString(mapping.Sample.Name, ThemeBase.LightGreyFillColor2);
                    dlg.Properties.AddIntegerRange("Pitch :", mapping.Pitch, 0, 15);
                    dlg.Properties.AddBoolean("Loop :", mapping.Loop);
                    dlg.Properties.Build();

                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        var newName = dlg.Properties.GetPropertyValue<string>(0);

                        App.Stop();
                        App.UndoRedoManager.BeginTransaction(TransactionScope.DCPMSamples);
                        if (App.Project.RenameSample(mapping.Sample, newName))
                        {
                            mapping.Pitch = dlg.Properties.GetPropertyValue<int>(1);
                            mapping.Loop = dlg.Properties.GetPropertyValue<bool>(2);
                            App.UndoRedoManager.EndTransaction();
                        }
                        else
                        {
                            App.UndoRedoManager.AbortTransaction();
                            SystemSounds.Beep.Play();
                        }
                        ConditionalInvalidate();
                    }
                }
            }
            else if (right && editMode == EditionMode.Channel && e.Y < headerSizeY && e.X > whiteKeySizeX)
            {
                int patIdx  = Song.FindPatternInstanceIndex((int)Math.Floor((e.X - whiteKeySizeX + scrollX) / (float)noteSizeX), out _);
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
                }
            }
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
                        App.StopOrReleaseIntrumentNote();
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
                        App.StopIntrumentNote();
                        break;
                }

                captureOperation = CaptureOperation.None;
                Capture = false;
            }
        }

        private void AbortCaptureOperation()
        {
            if (captureOperation != CaptureOperation.None)
            {
                if (App.UndoRedoManager.HasTransactionInProgress)
                    App.UndoRedoManager.AbortTransaction();

                ConditionalInvalidate();
                App.StopIntrumentNote();

                captureOperation = CaptureOperation.None;
                Capture = false;

                ManyPatternChanged?.Invoke();
            }
        }

        private void GetSelectionRange(int minFrameIdx, int maxFrameIdx, out int minPattern, out int maxPattern, out int minNote, out int maxNote)
        {
            minPattern = Song.FindPatternInstanceIndex(minFrameIdx, out minNote);
            maxPattern = Song.FindPatternInstanceIndex(maxFrameIdx, out maxNote);
        }

        private bool IsSelectionValid()
        {
            return selectionFrameMin >= 0 && selectionFrameMax >= 0;
        }

        private void MoveNotes(int amount)
        {
            if (selectionFrameMin + amount >= 0)
            {
                App.UndoRedoManager.BeginTransaction(TransactionScope.Channel, Song.Id, editChannel);
                var notes = GetSelectedNotes();
                DeleteSelectedNotes(false);
                ReplaceNotes(notes, selectionFrameMin + amount, false);
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

            TransformNotes(selectionFrameMin, selectionFrameMax, true, (note, idx) =>
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
            TransformEnvelopeValues(selectionFrameMin, selectionFrameMax, (val, idx) =>
            {
                return (sbyte)Utils.Clamp(val + amount, sbyte.MinValue, sbyte.MaxValue);
            });
        }

        private void MoveEnvelopeValues(int amount)
        {
            if (selectionFrameMin + amount >= 0)
                ReplaceEnvelopeValues(GetSelectedEnvelopeValues(), selectionFrameMin + amount);
        }
        
        private void DeleteSelectedNotes(bool doTransaction = true)
        {
            TransformNotes(selectionFrameMin, selectionFrameMax, doTransaction, (note, idx) =>
            {
                if (note != null)
                    note.Clear(false);
                return note;
            });
        }

        private void DeleteSelectedEnvelopeValues()
        {
            TransformEnvelopeValues(selectionFrameMin, selectionFrameMax, (val, idx) =>
            {
                return 0;
            });
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
                bool ctrl  = ModifierKeys.HasFlag(Keys.Control);
                bool shift = ModifierKeys.HasFlag(Keys.Shift);

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
                        DeleteSelectedNotes();
                    else if (editMode == EditionMode.Enveloppe) // MATTT
                        DeleteSelectedEnvelopeValues();
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
                            MoveNotes(ctrl ?  (Song.Project.UsesFamiTrackerTempo ? Song.BarLength : Song.NoteLength) : 1);
                            break;
                        case Keys.Left:
                            MoveNotes(ctrl ? -(Song.Project.UsesFamiTrackerTempo ? Song.BarLength : Song.NoteLength) : -1);
                            break;
                    }
                }
                else if (editMode == EditionMode.Enveloppe) // MATTT
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

        private void ShowInstrumentError()
        {
            App.DisplayWarning("Selected instrument is incompatible with channel!");
        }

        private void ToggleEffectPannel()
        {
            Debug.Assert(editMode == EditionMode.Channel);
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

            if (left && IsMouseInPiano(e))
            {
                StartCaptureOperation(e, CaptureOperation.PlayPiano);
                PlayPiano(e.X, e.Y);
            }
            else if (left && editMode == EditionMode.Channel && IsMouseInHeader(e))
            {
                App.Seek((int)Math.Floor((e.X - whiteKeySizeX + scrollX) / (float)noteSizeX));
            }
            else if (right && editMode == EditionMode.Channel && IsMouseInHeader(e))
            {
                StartCaptureOperation(e, CaptureOperation.Select, false);
                UpdateSelection(e.X, true);
            }
            else if (right && editMode == EditionMode.Enveloppe && IsMouseInHeaderTopPart(e)) // MATTT
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
            else if ((left || right) && (editMode == EditionMode.Enveloppe || editMode == EditionMode.Arpeggio)  && IsMouseInNoteArea(e) && EditEnvelope.Length > 0) // MATTT
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
            else if (left && editMode == EditionMode.Channel && IsMouseInTopLeftCorner(e))
            {
                ToggleEffectPannel();
                return;
            }
            else if (editMode == EditionMode.Channel && GetNoteForCoord(e.X, e.Y, out int patternIdx, out int noteIdx, out byte noteValue))
            {
                var changed = false;
                var channel = Song.Channels[editChannel];
                var pattern = channel.PatternInstances[patternIdx];

                if (pattern == null)
                    return;

                var supportsInstrument = channel.SupportsInstrument(currentInstrument);

                if (left)
                {
                    var ctrl   = ModifierKeys.HasFlag(Keys.Control);
                    var shift  = ModifierKeys.HasFlag(Keys.Shift);
                    var slide  = FamiStudioForm.IsKeyDown(Keys.S);
                    var attack = FamiStudioForm.IsKeyDown(Keys.A);

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
                    else if (ctrl || shift && channel.SupportsReleaseNotes)
                    {
                        App.UndoRedoManager.BeginTransaction(TransactionScope.Pattern, pattern.Id);
                        var note = pattern.GetOrCreateNoteAt(noteIdx);
                        note.Value = (byte)(ctrl ? Note.NoteStop : Note.NoteRelease);
                        note.Instrument = null;
                        note.Slide = 0;
                        pattern.ClearLastValidNoteCache();
                        App.UndoRedoManager.EndTransaction();
                        changed = true;
                    }
                    else if (slide && channel.SupportsSlideNotes)
                    {
                        if (supportsInstrument)
                        {
                            App.UndoRedoManager.BeginTransaction(TransactionScope.Pattern, pattern.Id);
                            SnapPatternNote(patternIdx, ref noteIdx);
                            var note = pattern.GetOrCreateNoteAt(noteIdx);
                            note.Value = noteValue;
                            note.Instrument = editChannel == Channel.Dpcm ? null : currentInstrument;
                            pattern.ClearLastValidNoteCache();
                            StartCaptureOperation(e, CaptureOperation.CreateDragSlideNoteTarget, true);
                        }
                        else
                        {
                            ShowInstrumentError();
                        }
                    }
                    else
                    {
                        var prevPatternIdx = patternIdx;
                        var prevNoteIdx    = noteIdx;

                        channel.PatternInstances[patternIdx].Notes.TryGetValue(noteIdx, out var note);

                        var stopOrRelease = note != null && (note.IsStop || note.IsRelease);
                        var musicalNote   = note != null && (note.IsMusical);
                        var dragStarted = false;

                        if (stopOrRelease || (musicalNote && note.Value == noteValue) || channel.FindPreviousMatchingNote(noteValue, ref prevPatternIdx, ref prevNoteIdx))
                        {
                            if (IsNoteSelected(prevPatternIdx, prevNoteIdx))
                            {
                                bool multiplePatterns = Song.FindPatternInstanceIndex(selectionFrameMin, out _) != 
                                                        Song.FindPatternInstanceIndex(selectionFrameMax, out _);

                                if (multiplePatterns)
                                    App.UndoRedoManager.BeginTransaction(TransactionScope.Channel, Song.Id, editChannel);
                                else
                                    App.UndoRedoManager.BeginTransaction(TransactionScope.Pattern, pattern.Id);

                                StartCaptureOperation(e, CaptureOperation.DragSelection, true);

                                var absPrevNoteIdx = Song.GetPatternStartNote(prevPatternIdx, prevNoteIdx);

                                dragNotes = GetSparseSelectedNotes(selectionFrameMin);

                                dragFrameMin = selectionFrameMin;
                                dragFrameMax = selectionFrameMax;
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
                                App.UndoRedoManager.BeginTransaction(TransactionScope.Pattern, pattern.Id);
                                StartCaptureOperation(e, CaptureOperation.DragNewNote, true);

                                var newNote = new Note(noteValue);
                                newNote.Instrument = editChannel == Channel.Dpcm ? null : currentInstrument;
                                newNote.Arpeggio   = Song.Channels[editChannel].SupportsArpeggios ? currentArpeggio : null;

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
                            dragLastNoteValue = -1;
                            UpdateNoteDrag(e, false);
                            ConditionalInvalidate();
                        }
                    }

                    changed = true;
                }
                else if (right)
                {
                    if (pattern.Notes.TryGetValue(noteIdx, out var note) && (note.IsStop || note.IsRelease))
                    {
                        App.UndoRedoManager.BeginTransaction(TransactionScope.Pattern, pattern.Id);
                        pattern.Notes.Remove(noteIdx);
                        pattern.ClearLastValidNoteCache();
                        App.UndoRedoManager.EndTransaction();
                        changed = true;
                    }
                    else
                    {
                        if (channel.FindPreviousMatchingNote(noteValue, ref patternIdx, ref noteIdx))
                        {
                            var foundPattern = channel.PatternInstances[patternIdx];
                            App.UndoRedoManager.BeginTransaction(TransactionScope.Pattern, foundPattern.Id);
                            foundPattern.Notes.Remove(noteIdx);
                            foundPattern.ClearLastValidNoteCache();
                            App.UndoRedoManager.EndTransaction();
                            changed = true;
                        }
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
                    App.UndoRedoManager.BeginTransaction(TransactionScope.Pattern, pattern.Id);
                    if (pattern.Notes.TryGetValue(noteIdx, out var note))
                        note.ClearEffectValue(selectedEffectIdx);
                    pattern.ClearLastValidNoteCache();
                    PatternChanged?.Invoke(pattern);
                    App.UndoRedoManager.EndTransaction();
                    ConditionalInvalidate();
                }
            }
            else if (editMode == EditionMode.DPCM && (left || right) && GetNoteForCoord(e.X, e.Y, out patternIdx, out noteIdx, out noteValue))
            {
                if (App.Project.NoteSupportsDPCM(noteValue))
                {
                    var save    = FamiStudioForm.IsKeyDown(Keys.S);
                    var mapping = App.Project.GetDPCMMapping(noteValue);

                    if (left && save && mapping != null && mapping.Sample != null)
                    {
                        var filename = PlatformUtils.ShowSaveFileDialog("Save File", "DPCM Samples (*.dmc)|*.dmc", ref Settings.LastSampleFolder);
                        if (filename != null)
                        {
                            File.WriteAllBytes(filename, mapping.Sample.Data);
                        }
                    }
                    else if (left && mapping == null)
                    {
                        var filename = PlatformUtils.ShowOpenFileDialog("Open File", "DPCM Samples (*.dmc)|*.dmc", ref Settings.LastSampleFolder);
                        if (filename != null)
                        {
                            App.UndoRedoManager.BeginTransaction(TransactionScope.DCPMSamples);
                            var name = Path.GetFileNameWithoutExtension(filename);
                            var sample = App.Project.CreateDPCMSample(name, File.ReadAllBytes(filename));
                            App.Project.MapDPCMSample(noteValue, sample);
                            App.UndoRedoManager.EndTransaction();
                            ConditionalInvalidate();
                        }
                    }
                    else if (right && mapping != null)
                    {
                        App.UndoRedoManager.BeginTransaction(TransactionScope.DCPMSamples);
                        App.Project.UnmapDPCMSample(noteValue);
                        App.Project.CleanupUnusedSamples();
                        App.UndoRedoManager.EndTransaction();
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

                if (editMode == EditionMode.Channel)
                    maxScrollX = Math.Max(Song.GetPatternStartNote(Song.Length) * noteSizeX - ScrollMargin, 0);
                else if (editMode == EditionMode.Enveloppe || editMode == EditionMode.Arpeggio)
                    maxScrollX = Math.Max(EditEnvelope.Length * noteSizeX - ScrollMargin, 0);

                if (scrollX < minScrollX) scrollX = minScrollX;
                if (scrollX > maxScrollX) scrollX = maxScrollX;
                if (scrollY < minScrollY) scrollY = minScrollY;
                if (scrollY > maxScrollY) scrollY = maxScrollY;
            }
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
            if (editMode == EditionMode.Channel   || 
                editMode == EditionMode.Enveloppe || 
                editMode == EditionMode.Arpeggio)
            {
                int rangeMax = editMode == EditionMode.Channel ? Song.GetPatternStartNote(Song.Length) - 1 : EditEnvelope.Length - 1;

                if (min > rangeMax)
                {
                    ClearSelection();
                }
                else
                {
                    selectionFrameMin = Utils.Clamp(min, 0, rangeMax);
                    selectionFrameMax = Utils.Clamp(max, min, rangeMax);
                }
            }
        }

        private void ClearSelection()
        {
            selectionFrameMin = -1;
            selectionFrameMax = -1;
        }

        private void UpdateSelection(int mouseX, bool first = false)
        {
            if ((mouseX - whiteKeySizeX) < 100)
            {
                scrollX -= 32;
                ClampScroll();
            }
            else if ((Width - mouseX) < 100)
            {
                scrollX += 32;
                ClampScroll();
            }

            int noteIdx = (mouseX - whiteKeySizeX + scrollX) / noteSizeX;

            int minSelectionIdx = Math.Min(noteIdx, captureNoteIdx);
            int maxSelectionIdx = Math.Max(noteIdx, captureNoteIdx);
            int pad = IsSnappingEnabled ? -1 : 0;

            SetSelection(SnapNote(minSelectionIdx), SnapNote(maxSelectionIdx, true) + pad);

            ConditionalInvalidate();
        }

        private void UpdateSlideNoteTarget(MouseEventArgs e)
        {
            Debug.Assert(captureNoteIdx >= 0);

            var patternIdx = Song.FindPatternInstanceIndex(captureNoteIdx, out var noteIdx);
            var pattern    = Song.Channels[editChannel].PatternInstances[patternIdx];

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
            if (editMode == EditionMode.Channel && editChannel != Channel.Dpcm && IsSelectionValid())
            {
                if (Song.Channels[editChannel].SupportsInstrument(instrument))
                {
                    TransformNotes(selectionFrameMin, selectionFrameMax, true, (note, idx) =>
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
            return showEffectsPanel && editMode == EditionMode.Channel && e.X > whiteKeySizeX && e.X > headerSizeY && e.Y < headerAndEffectSizeY;
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
            return editMode == EditionMode.Channel && e.Y < headerSizeY && e.X < whiteKeySizeX;
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
            else if (IsMouseInNoteArea(e))
            {
                if (editMode == EditionMode.Channel)
                {
                    if (GetNoteForCoord(e.X, e.Y, out int patternIdx, out int noteIdx, out byte noteValue))
                    {
                        if (Song.Channels[editChannel].PatternInstances[patternIdx] == null)
                            tooltip = "{MouseWheel} Pan";
                        else
                            tooltip = "{MouseLeft} {Drag} Add/drag note - {Ctrl} {MouseLeft} Add stop note - {Shift} {MouseLeft} Add release note - {MouseWheel} Pan\n{MouseRight} Delete note - {S} {MouseLeft} {Drag} Create/edit slide note - {A} {MouseLeft} Toggle note attack";

                        newNoteTooltip = $"{Note.GetFriendlyName(noteValue)} [{patternIdx:D3} : {noteIdx:D3}]";
                        if (Song.Channels[editChannel].FindPreviousMatchingNote(noteValue, ref patternIdx, ref noteIdx))
                        {
                            var pat  = Song.Channels[editChannel].PatternInstances[patternIdx];
                            var note = pat.Notes[noteIdx];
                            if (note.Instrument != null)
                                newNoteTooltip += $" ({note.Instrument.Name})";
                        }
                    }
                }
                else if (editMode == EditionMode.Enveloppe || editMode == EditionMode.Arpeggio)
                {
                    tooltip = "{MouseLeft} Set envelope value - {MouseWheel} Pan";

                    if (GetEnvelopeValueForCoord(e.X, e.Y, out int idx, out sbyte value))
                        newNoteTooltip = $"{idx:D3} : {value}";
                }
                else if (editMode == EditionMode.DPCM)
                {
                    if (GetNoteForCoord(e.X, e.Y, out int patternIdx, out int noteIdx, out byte noteValue))
                    {
                        if (App.Project.NoteSupportsDPCM(noteValue))
                        {
                            var mapping = App.Project.GetDPCMMapping(noteValue);
                            if (mapping == null)
                                tooltip = "{MouseLeft} Load DPCM sample - {MouseWheel} Pan";
                            else
                                tooltip = "{S} {MouseLeft} Save sample to file - {MouseLeft}{MouseLeft} Sample properties - {MouseRight} Delete sample {MouseWheel} Pan";
                        }
                        else
                            tooltip = "Samples must be between C1 and D6";
                    }
                }
            }

            App.ToolTip = tooltip;

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

        private int SnapNote(int noteIdx, bool roundUp = false)
        {
            if (IsSnappingEnabled)
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
                    var baseNodeIdx  = noteIdx / noteLength * noteLength;
                    var noteFrameIdx = noteIdx % noteLength;
                    snappedNoteIndex = baseNodeIdx + (int)Math.Round(Math.Floor((noteFrameIdx + 0.001) / (noteLength * snapFactor) + (roundUp ? 1 : 0)) * (noteLength * snapFactor));
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

            App.UndoRedoManager.RestoreTransaction(false);

            GetNoteForCoord(e.X, e.Y, out var patternIdx, out var noteIdx, out var noteValue, true /* captureOperation != CaptureOperation.DragSelection*/);

            int deltaNoteIdx    = Song.GetPatternStartNote(patternIdx, noteIdx) - captureNoteIdx;
            int deltaNoteValue  = noteValue - captureNoteValue;
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
                }
            }

            var copy = ModifierKeys.HasFlag(Keys.Control) && captureOperation != CaptureOperation.DragNewNote;
            var channel = Song.Channels[editChannel];
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
                selectionFrameMin = Utils.Clamp(newDragFrameMin, 0, Song.GetPatternStartNote(Song.Length) - 1);
                selectionFrameMax = Utils.Clamp(newDragFrameMax, 0, Song.GetPatternStartNote(Song.Length) - 1);
            }

            if (dragLastNoteValue != noteValue && 
                (captureOperation == CaptureOperation.DragNote ||
                 captureOperation == CaptureOperation.DragNewNote))
            {
                App.PlayInstrumentNote(noteValue, false);
                dragLastNoteValue = noteValue;
            }

            if (createNote && deltaNoteIdx == 0 && deltaNoteValue == 0)
            {
                Debug.Assert(dragFrameMin == dragFrameMax);

                var pattern = channel.PatternInstances[patternIdx];
                if (pattern != null)
                {
                    if (channel.SupportsInstrument(currentInstrument))
                    {
                        var note = pattern.GetOrCreateNoteAt(noteIdx);
                        note.Value = noteValue;
                        note.Instrument = editChannel == Channel.Dpcm ? null : currentInstrument;
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
                for (int p = p0; p <= p1; p++)
                    PatternChanged?.Invoke(channel.PatternInstances[p]);
                p0 = Song.FindPatternInstanceIndex(dragFrameMin + deltaNoteIdx + 0, out _);
                p1 = Song.FindPatternInstanceIndex(dragFrameMax + deltaNoteIdx + 1, out _);
                for (int p = p0; p <= p1; p++)
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

        private void UpdateCursor()
        {
            var pt = PointToClient(Cursor.Position);

            if ((editMode == EditionMode.Enveloppe || editMode == EditionMode.Arpeggio) && EditEnvelope.CanResize && (pt.X > whiteKeySizeX && pt.Y < headerSizeY && captureOperation != CaptureOperation.Select) || captureOperation == CaptureOperation.ResizeEnvelope)
                Cursor.Current = Cursors.SizeWE;
            else if (captureOperation == CaptureOperation.ChangeEffectValue)
                Cursor.Current = Cursors.SizeNS;
            else if (ModifierKeys.HasFlag(Keys.Control) && (captureOperation == CaptureOperation.DragNote || captureOperation == CaptureOperation.DragSelection))
                Cursor.Current = Cursors.CopyCursor;
            else
                Cursor.Current = Cursors.Default;
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

            mouseLastX = e.X;
            mouseLastY = e.Y;
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            bool middle = e.Button.HasFlag(MouseButtons.Middle);

            if (!middle)
            {
                EndCaptureOperation(e);
            }
        }

        private void ZoomAtLocation(int x, int delta)
        {
            int pixelX = x - whiteKeySizeX;
            int absoluteX = pixelX + scrollX;
            if (delta < 0 && zoomLevel > MinZoomLevel) { zoomLevel--; absoluteX /= 2; }
            if (delta > 0 && zoomLevel < MaxZoomLevel) { zoomLevel++; absoluteX *= 2; }
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
                else if (editMode != EditionMode.DPCM)
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

        public void Tick()
        {
            if (captureOperation == CaptureOperation.Select)
            {
                var pt = this.PointToClient(Cursor.Position);
                UpdateSelection(pt.X, false);
            }
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
            buffer.Serialize(ref selectionFrameMin);
            buffer.Serialize(ref selectionFrameMax);

            if (buffer.IsReading)
            {
                BuildSupportEffectList();
                UpdateRenderCoords();
                ClampScroll();
                ClampMinSnap();
                ConditionalInvalidate();

                captureOperation = CaptureOperation.None;
                Capture = false;
            }
        }
    }
}
