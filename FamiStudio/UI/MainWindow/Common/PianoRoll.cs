using System;
using System.Diagnostics;
using System.IO;
using System.Media;
using System.Drawing;
using System.Windows.Forms;
using FamiStudio.Properties;

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
        const int ScrollMargin = 128;

        const int DefaultNumOctavesChannel      = 8;
        const int DefaultBaseOctaveChannel      = 0;
        const int DefaultNumOctavesEnvelope     = 7;
        const int DefaultBaseOctaveEnvelope     = 1;
        const int DefaultHeaderSizeY            = 17;
        const int DefaultEffectPanelSizeY       = 256;
        const int DefaultEffectButtonSizeY      = 17;
        const int DefaultNoteSizeX              = 16;
        const int DefaultNoteSizeY              = 12;
        const int DefaultEnvelopeSizeY          = 8;
        const int DefaultEnvelopeMax            = 127;
        const int DefaultWhiteKeySizeX          = 81;
        const int DefaultWhiteKeySizeY          = 20;
        const int DefaultBlackKeySizeX          = 56;
        const int DefaultBlackKeySizeY          = 14;
        const int DefaultEffectIconPosX         = 2;
        const int DefaultEffectIconPosY         = 2;
        const int DefaultEffectNamePosX         = 17;
        const int DefaultEffectNamePosY         = 2;
        const int DefaultEffectIconSizeX        = 12;
        const int DefaultEffectValueTextOffsetY = 12;
        const int DefaultBigTextPosX            = 10;
        const int DefaultBigTextPosY            = 10;
        const int DefaultDPCMTextPosX           = 2;
        const int DefaultDPCMTextPosY           = 0;
        const int DefaultOctaveNameOffsetY      = 11;

        int numNotes;
        int numOctaves;
        int baseOctave;
        int headerSizeY;
        int headerAndEffectSizeY;
        int effectPanelSizeY;
        int effectButtonSizeY;
        int noteSizeX;       
        int noteSizeY;
        int envelopeSizeY;
        int envelopeMax;  
        int whiteKeySizeY;
        int whiteKeySizeX;
        int blackKeySizeY;
        int blackKeySizeX;
        int effectIconPosX;
        int effectIconPosY;
        int effectNamePosX;
        int effectNamePosY;
        int effectIconSizeX;
        int effectValueTextOffsetY;
        int bigTextPosX;
        int bigTextPosY;
        int dpcmTextPosX;
        int dpcmTextPosY;
        int octaveNameOffsetY;
        int octaveSizeY;
        int virtualSizeY;
        int patternSizeX;
        int barSizeX;

        int ScaleForZoom(int value)
        {
            return zoomLevel < 0 ? value / (1 << (-zoomLevel)) : value * (1 << zoomLevel);
        }

        enum EditionMode
        {
            None,
            Channel,
            Enveloppe,
            DPCM
        };

        RenderTheme theme;
        RenderBrush whiteKeyBrush;
        RenderBrush blackKeyBrush;
        RenderBrush whiteKeyPressedBrush;
        RenderBrush blackKeyPressedBrush;
        RenderBrush debugBrush;
        RenderBrush playPositionBrush;
        RenderBitmap bmpLoop;
        RenderBitmap bmpEffectExpanded;
        RenderBitmap bmpEffectCollapsed;
        RenderBitmap bmpVolume;
        RenderBitmap[] bmpEffects = new RenderBitmap[3];
        RenderBitmap[] bmpEffectsFilled = new RenderBitmap[3];
        RenderPath[] stopNoteGeometry = new RenderPath[MaxZoomLevel - MinZoomLevel + 1];

        int mouseLastX = 0;
        int mouseLastY = 0;
        int playingNote = -1;
        bool playingPiano = false;
        bool scrolling = false;
        bool resizingEnvelope = false;
        bool changingEffectValue = false;
        bool drawingEnvelope = false;
        int  effectPatternIdx;
        int  effectNoteIdx;

        bool showEffectsPanel = false;
        int scrollX = 0;
        int scrollY = 0;
        int zoomLevel = 0;
        int selectedEffectIdx = -1; // -1 = volume

        EditionMode editMode = EditionMode.None;

        // Pattern edit mode.
        int editChannel = -1;
        Instrument currentInstrument = null;

        // Envelope edit mode.
        Instrument editInstrument = null;
        int editEnvelope;

        public delegate void PatternChange(Pattern pattern);
        public event PatternChange PatternChanged;
        public delegate void EnvelopeResize();
        public event EnvelopeResize EnvelopeResized;

        public PianoRoll()
        {
            UpdateRenderCoords();
        }
        
        private void UpdateRenderCoords()
        {
            var scaling = RenderTheme.MainWindowScaling;

            numOctaves             = editMode == EditionMode.Enveloppe ? DefaultNumOctavesEnvelope : DefaultNumOctavesChannel;
            baseOctave             = editMode == EditionMode.Enveloppe ? DefaultBaseOctaveEnvelope : DefaultBaseOctaveChannel;
            headerSizeY            = (int)(DefaultHeaderSizeY * scaling);
            effectPanelSizeY       = (int)(DefaultEffectPanelSizeY * scaling);
            effectButtonSizeY      = (int)(DefaultEffectButtonSizeY * scaling);
            noteSizeX              = (int)(ScaleForZoom(DefaultNoteSizeX) * scaling);        
            noteSizeY              = (int)(DefaultNoteSizeY * scaling);        
            envelopeSizeY          = (int)(DefaultEnvelopeSizeY * scaling);    
            envelopeMax            = (int)(DefaultEnvelopeMax * scaling);      
            whiteKeySizeY          = (int)(DefaultWhiteKeySizeY * scaling);    
            whiteKeySizeX          = (int)(DefaultWhiteKeySizeX * scaling);    
            blackKeySizeY          = (int)(DefaultBlackKeySizeY * scaling);    
            blackKeySizeX          = (int)(DefaultBlackKeySizeX * scaling);
            effectIconPosX         = (int)(DefaultEffectIconPosX * scaling);
            effectIconPosY         = (int)(DefaultEffectIconPosY * scaling);
            effectNamePosX         = (int)(DefaultEffectNamePosX * scaling);
            effectNamePosY         = (int)(DefaultEffectNamePosY * scaling);
            effectIconSizeX        = (int)(DefaultEffectIconSizeX * scaling);
            effectValueTextOffsetY = (int)(DefaultEffectValueTextOffsetY * scaling);
            bigTextPosX            = (int)(DefaultBigTextPosX * scaling);
            bigTextPosY            = (int)(DefaultBigTextPosY * scaling);
            dpcmTextPosX           = (int)(DefaultDPCMTextPosX * scaling);
            dpcmTextPosY           = (int)(DefaultDPCMTextPosY * scaling);
            octaveNameOffsetY      = (int)(DefaultOctaveNameOffsetY * scaling);
            octaveSizeY            = 12 * noteSizeY;
            numNotes               = numOctaves * 12;
            virtualSizeY           = numNotes * noteSizeY;
            patternSizeX           = noteSizeX * (Song == null ? 256 : Song.PatternLength);
            barSizeX               = noteSizeX * (Song == null ? 16  : Song.BarLength);
            headerAndEffectSizeY   = headerSizeY + (showEffectsPanel ? effectPanelSizeY : 0);
        }

        public Instrument CurrentInstrument
        {
            get { return currentInstrument; }
            set { currentInstrument = value; }
        }

        public void StartEditPattern(int trackIdx, int patternIdx)
        {
            editMode = EditionMode.Channel;
            editChannel = trackIdx;

            UpdateRenderCoords();
            CenterScroll(patternIdx);
            ClampScroll();
            ConditionalInvalidate();
        }

        public void StartEditEnveloppe(Instrument instrument, int envelope)
        {
            editMode = EditionMode.Enveloppe;
            editInstrument = instrument;
            editEnvelope = envelope;
            showEffectsPanel = false;
            Debug.Assert(editInstrument != null);

            UpdateRenderCoords();
            CenterScroll();
            ClampScroll();
            ConditionalInvalidate();
        }

        private void CenterScroll(int patternIdx = 0)
        {
            int maxScrollY = Math.Max(virtualSizeY + headerAndEffectSizeY - Height, 0);
            scrollX = patternIdx * patternSizeX;
            scrollY = maxScrollY / 2;
        }

        public void StartEditDPCMSamples()
        {
            editMode = EditionMode.DPCM;
            showEffectsPanel = false;
            zoomLevel = 0;

            UpdateRenderCoords();
            ClampScroll();
            ConditionalInvalidate();
        }

        private Song Song
        {
            get { return App?.Song; }
        }

        private Envelope EditEnvelope
        {
            get { return editInstrument?.Envelopes[(int)editEnvelope]; }
        }

        public bool IsEditingInstrument
        {
            get { return editMode == EditionMode.Enveloppe; }
        }

        public void ConditionalInvalidate()
        {
            if (!App.RealTimeUpdate)
                Invalidate();
        }

        public void Reset()
        {
            showEffectsPanel = false;
            scrollX = 0;
            scrollY = 0;
            zoomLevel = 0;
            editMode = EditionMode.None;
            editChannel = -1;
            currentInstrument = null;
            editInstrument = null;
            UpdateRenderCoords();
        }

        public void SongChanged()
        {
            if (editMode == EditionMode.Channel)
            {
                editMode = EditionMode.None;
                editChannel = -1;
                editInstrument = null;
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
            debugBrush = g.CreateSolidBrush(ThemeBase.GreenColor);
            playPositionBrush = g.CreateSolidBrush(Color.FromArgb(128, ThemeBase.LightGreyFillColor1));

            bmpLoop = g.CreateBitmapFromResource("LoopSmall");
            bmpVolume = g.CreateBitmapFromResource("VolumeSmall");
            bmpEffects[0] = g.CreateBitmapFromResource("LoopSmall");
            bmpEffects[1] = g.CreateBitmapFromResource("JumpSmall");
            bmpEffects[2] = g.CreateBitmapFromResource("SpeedSmall");
            bmpEffectsFilled[0] = g.CreateBitmapFromResource("LoopSmallFill");
            bmpEffectsFilled[1] = g.CreateBitmapFromResource("JumpSmallFill");
            bmpEffectsFilled[2] = g.CreateBitmapFromResource("SpeedSmallFill");
            bmpEffectExpanded = g.CreateBitmapFromResource("ExpandedSmall");
            bmpEffectCollapsed = g.CreateBitmapFromResource("CollapsedSmall");

            for (int z = MinZoomLevel; z <= MaxZoomLevel; z++)
            {
                int idx = z - MinZoomLevel;

                var points = new Point[3]
                {
                    new Point(0, 0),
                    new Point(0, noteSizeY),
                    new Point((int)(noteSizeX * (float)Math.Pow(2.0, z) - 1), noteSizeY / 2)
                };

                stopNoteGeometry[idx] = g.CreateConvexPath(points);
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

            if (editMode == EditionMode.Enveloppe)
            {
                var env = EditEnvelope;
                if (env != null)
                {
                    if (env.Loop >= 0)
                    {
                        g.FillRectangle(env.Loop * noteSizeX - scrollX, 0, env.Length * noteSizeX - scrollX, headerAndEffectSizeY, theme.DarkGreyFillBrush2);
                        g.DrawLine(env.Loop * noteSizeX - scrollX, 0, env.Loop * noteSizeX - scrollX, headerAndEffectSizeY, theme.DarkGreyLineBrush1);
                        g.DrawBitmap(bmpLoop, env.Loop * noteSizeX - scrollX + effectIconPosX + 1, effectIconPosY, RenderTheme.MainWindowScaling);
                    }
                    if (env.Length > 0)
                    {
                        g.DrawLine(env.Length * noteSizeX - scrollX, 0, env.Length * noteSizeX - scrollX, headerAndEffectSizeY, theme.DarkGreyLineBrush1);
                    }
                }
            }
            else if (editMode == EditionMode.Channel)
            {
                // Draw the header bars
                for (int p = a.minVisiblePattern; p < a.maxVisiblePattern; p++)
                {
                    int x = p * patternSizeX - scrollX;
                    if (p != 0)
                        g.DrawLine(x, 0, x, headerSizeY, theme.DarkGreyLineBrush1, 3.0f);
                    g.DrawText(p.ToString(), ThemeBase.FontMediumCenter, x, effectNamePosY, theme.LightGreyFillBrush1, patternSizeX);
                }

                // Draw the effect icons.
                if (editMode == EditionMode.Channel && zoomLevel >= 0)
                {
                    for (int p = a.minVisiblePattern; p < a.maxVisiblePattern; p++)
                    {
                        var pattern = Song.Channels[editChannel].PatternInstances[p];
                        if (pattern != null)
                        {
                            int patternX = p * patternSizeX - scrollX;
                            for (int i = 0; i < Song.PatternLength; i++)
                            {
                                var note = pattern.Notes[i];
                                if (note.HasEffect)
                                {
                                    g.DrawBitmap(bmpEffectsFilled[note.Effect - 1], patternX + i * noteSizeX + noteSizeX / 2 - effectIconSizeX / 2, effectIconPosY, RenderTheme.MainWindowScaling);
                                }
                            }
                        }
                    }
                }
            }
            
            int seekX = (editMode == EditionMode.Enveloppe ? App.GetEnvelopeFrame(editEnvelope) : App.CurrentFrame) * noteSizeX - scrollX;
            g.FillRectangle(seekX + 1, 0, seekX + noteSizeX, headerSizeY, playPositionBrush);
            g.DrawLine(0, headerSizeY - 1, Width, headerSizeY - 1, theme.BlackBrush);
            
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
                g.DrawBitmap(showEffectsPanel ? bmpEffectExpanded : bmpEffectCollapsed, effectIconPosX, effectIconPosY, RenderTheme.MainWindowScaling);

                if (showEffectsPanel)
                {
                    string[] EffectNames =
                    {
                        "Jump",
                        "Skip",
                        "Speed"
                    };

                    g.PushTranslation(0, headerSizeY);

                    g.DrawLine(0, -1, whiteKeySizeX, -1, theme.BlackBrush);
                    g.DrawBitmap(bmpVolume, effectIconPosX, effectIconPosY, RenderTheme.MainWindowScaling);
                    g.DrawText("Volume", selectedEffectIdx == -1 ? ThemeBase.FontSmallBold : ThemeBase.FontSmall, effectNamePosX, effectNamePosY, theme.BlackBrush);

                    int effectButtonY = 0;
                    for (int i = 0; i < 3; i++)
                    {
                        effectButtonY += effectButtonSizeY;
                        g.PushTranslation(0, effectButtonY);
                        g.DrawLine(0, -1, whiteKeySizeX, -1, theme.BlackBrush);
                        g.DrawBitmap(bmpEffects[i], effectIconPosX, effectIconPosY, RenderTheme.MainWindowScaling);
                        g.DrawText(EffectNames[i], selectedEffectIdx == i ? ThemeBase.FontSmallBold : ThemeBase.FontSmall, effectNamePosX, effectNamePosY, theme.BlackBrush);
                        g.PopTransform();
                    }

                    effectButtonY += effectButtonSizeY;
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

            bool showSampleNames = editMode == EditionMode.Channel && editChannel == 4;
            bool showNoiseValues = editMode == EditionMode.Channel && editChannel == 3;

            int playOctave = -1;
            int playNote = -1;

            if (playingNote > 0)
            {
                int tmpNote = playingNote - (12 * baseOctave);

                playOctave = (tmpNote - 1) / 12;
                playNote   = (tmpNote - 1) - playOctave * 12;

                if (!IsBlackKey(playNote))
                    g.FillRectangle(GetKeyRectangle(playOctave, playNote), whiteKeyPressedBrush);
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

                        if (i == playOctave && j == playNote)
                            g.FillRectangle(GetKeyRectangle(playOctave, playNote), blackKeyPressedBrush);
                    }

                    int y = octaveBaseY - j * noteSizeY;
                    if (j == 0)
                        g.DrawLine(0, y, whiteKeySizeX, y, theme.DarkGreyLineBrush1);
                    else if (j == 5)
                        g.DrawLine(0, y, whiteKeySizeX, y, theme.DarkGreyLineBrush2);
                }

                if (!showSampleNames)
                {
                    g.DrawText("C" + (i + baseOctave), ThemeBase.FontSmall, 1, octaveBaseY - octaveNameOffsetY, theme.BlackBrush);
                }
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

                var lastVolumeFrame = 0;
                var lastVolumeValue = Song.Channels[editChannel].GetLastValidVolume(a.minVisiblePattern - 1);

                // Draw the effects.
                if (selectedEffectIdx == -1)
                {
                    for (int p = a.minVisiblePattern; p < a.maxVisiblePattern; p++)
                    {
                        var pattern = Song.Channels[editChannel].PatternInstances[p];
                        if (pattern != null)
                        {
                            int x = p * patternSizeX;

                            for (int i = 0; i < Song.PatternLength; i++)
                            {
                                var note = pattern.Notes[Math.Min(i, Song.PatternLength - 1)];

                                if (note.HasVolume && selectedEffectIdx == -1)
                                {
                                    g.PushTranslation(x + i * noteSizeX - scrollX, 0);

                                    var frame = p * Song.PatternLength + i;
                                    var sizeY = (float)Math.Floor(lastVolumeValue / (float)Note.VolumeMax * effectPanelSizeY);
                                    g.FillRectangle(lastVolumeFrame < 0 ? -noteSizeX * 1000 : (frame - lastVolumeFrame - 1) * -noteSizeX, effectPanelSizeY - sizeY, 0, effectPanelSizeY, theme.DarkGreyFillBrush2);
                                    lastVolumeValue = note.Volume;
                                    lastVolumeFrame = frame;

                                    g.PopTransform();
                                }
                            }
                        }
                    }

                    g.PushTranslation(Math.Max(0, lastVolumeFrame * noteSizeX - scrollX), 0);
                    var lastSizeY = (float)Math.Floor(lastVolumeValue / (float)Note.VolumeMax * effectPanelSizeY);
                    g.FillRectangle(noteSizeX, effectPanelSizeY - lastSizeY, Width, effectPanelSizeY, theme.DarkGreyFillBrush2);
                    g.PopTransform();
                }

                for (int p = a.minVisiblePattern; p < a.maxVisiblePattern; p++)
                {
                    var pattern = Song.Channels[editChannel].PatternInstances[p];
                    if (pattern != null)
                    {
                        int x = p * patternSizeX;

                        for (int i = 0; i < Song.PatternLength; i++)
                        {
                            var note = pattern.Notes[Math.Min(i, Song.PatternLength - 1)];

                            if ((note.HasEffect && selectedEffectIdx >= 0) ||
                                (note.HasVolume && selectedEffectIdx == -1))
                            {
                                var effectMaxValue = selectedEffectIdx == -1 ? Note.VolumeMax : Note.GetEffectMaxValue(Song, note.Effect);
                                var effectValue = selectedEffectIdx == -1 ? note.Volume : note.EffectParam;
                                var sizeY = (float)Math.Floor(effectValue / (float)effectMaxValue * effectPanelSizeY);

                                g.PushTranslation(x + i * noteSizeX - scrollX, 0);

                                if (selectedEffectIdx != -1)
                                {
                                    g.FillRectangle(0, 0, noteSizeX, effectPanelSizeY, theme.DarkGreyFillBrush2);
                                }

                                g.FillRectangle(0, effectPanelSizeY - sizeY, noteSizeX, effectPanelSizeY, theme.LightGreyFillBrush1);
                                g.DrawRectangle(0, effectPanelSizeY - sizeY, noteSizeX, effectPanelSizeY, theme.BlackBrush);

                                var text = effectValue.ToString();
                                if ((text.Length <= 2 && zoomLevel >= 0) || zoomLevel > 0)
                                    g.DrawText(text, ThemeBase.FontSmallCenter, 0, effectPanelSizeY - effectValueTextOffsetY, theme.BlackBrush, noteSizeX);

                                g.PopTransform();
                            }
                        }

                        g.DrawLine(x - scrollX, 0, x - scrollX, headerAndEffectSizeY, theme.DarkGreyLineBrush1);
                        g.DrawLine(0, headerAndEffectSizeY - 1, Width, headerAndEffectSizeY - 1, theme.DarkGreyLineBrush1);
                    }
                }

                // Thick vertical bars
                for (int p = a.minVisiblePattern; p < a.maxVisiblePattern; p++)
                {
                    int x = p * patternSizeX - scrollX;
                    if (p != 0) g.DrawLine(x, 0, x, Height, theme.DarkGreyLineBrush1, 3.0f);
                }

                int seekX = App.CurrentFrame * noteSizeX - scrollX;
                g.FillRectangle(seekX + 1, 0, seekX + noteSizeX, effectPanelSizeY, playPositionBrush);

                g.DrawLine(0, effectPanelSizeY - 1, Width, effectPanelSizeY - 1, theme.BlackBrush);
                g.PopClip();
                g.PopTransform();
            }
        }

        private void RenderNotes(RenderGraphics g, RenderArea a)
        {
            g.PushTranslation(whiteKeySizeX, headerAndEffectSizeY);
            g.PushClip(0, 0, Width, Height);
            g.Clear(ThemeBase.DarkGreyFillColor2);

            if (editMode == EditionMode.Channel ||
                editMode == EditionMode.DPCM)
            {
                int maxX = editMode == EditionMode.Channel ? patternSizeX * a.maxVisiblePattern - scrollX : Width;

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

                if (editMode == EditionMode.Channel)
                {
                    int seekX = App.CurrentFrame * noteSizeX - scrollX;
                    g.FillRectangle(seekX + 1, 0, seekX + noteSizeX, Height, playPositionBrush);

                    int barCount = Song.PatternLength / Song.BarLength;

                    // Draw the vertical bars.
                    for (int p = a.minVisiblePattern; p < a.maxVisiblePattern; p++)
                    {
                        for (int b = 0; b < barCount; b++)
                        {
                            int barMinX = p * patternSizeX + b * barSizeX - scrollX;

                            for (int t = 0; t < Song.BarLength; t++)
                            {
                                int x = barMinX + t * noteSizeX;
                                if (zoomLevel < -1 && t != 0) continue;
                                if (p != 0 || b != 0 || t != 0) g.DrawLine(x, 0, x, Height, t == 0 ? theme.DarkGreyLineBrush1 : theme.DarkGreyLineBrush2, b == 0 && t == 0 ? 3.0f : 1.0f);
                            }
                        }
                    }

                    g.DrawLine(maxX, 0, maxX, Height, theme.DarkGreyLineBrush1);

                    // Pattern drawing.
                    for (int c = 0; c < Song.Channels.Length; c++)
                    {
                        if (c == editChannel || (App.GhostChannelMask & (1 << c)) != 0)
                        {
                            var dimmed = c != editChannel;

                            var lastNotePatternIdx = a.minVisiblePattern - 1;
                            var lastNoteValue = Song.Channels[c].GetLastValidNote(ref lastNotePatternIdx, out var lastNoteTime, out var lastNoteInstrument);

                            if (lastNoteValue != Note.NoteInvalid)
                            {
                                lastNoteTime = -(a.minVisiblePattern - lastNotePatternIdx - 1) * Song.PatternLength - (Song.PatternLength - lastNoteTime);
                            }

                            for (int p = a.minVisiblePattern; p < a.maxVisiblePattern; p++)
                            {
                                var pattern = Song.Channels[c].PatternInstances[p];
                                if (pattern != null)
                                {
                                    int startX = p * patternSizeX;

                                    for (int i = 0; i < Song.PatternLength; i++)
                                    {
                                        var note = pattern.Notes[i];
                                        var instrument = lastNoteInstrument;
                                        var color = instrument == null ? ThemeBase.LightGreyFillColor1 : instrument.Color;
                                        if (dimmed) color = Color.FromArgb((int)(color.A * 0.2f), color);

                                        if (lastNoteValue != Note.NoteInvalid && lastNoteTime != int.MinValue && note.IsValid)
                                        {
                                            if (lastNoteValue >= a.minVisibleNote && lastNoteValue <= a.maxVisibleNote)
                                            {
                                                int x = startX + lastNoteTime * noteSizeX - scrollX;
                                                int y = virtualSizeY - lastNoteValue * noteSizeY - scrollY;

                                                g.PushTranslation(x, y);
                                                g.FillRectangle(0, 0, (i - lastNoteTime) * noteSizeX, noteSizeY, g.GetVerticalGradientBrush(color, noteSizeY, 0.8f));
                                                g.DrawRectangle(0, 0, (i - lastNoteTime) * noteSizeX, noteSizeY, theme.BlackBrush);
                                                g.PopTransform();
                                            }
                                        }

                                        if (note.IsStop)
                                        {
                                            int value = lastNoteValue != Note.NoteInvalid ? lastNoteValue : 49; // C4 by default.

                                            if (value >= a.minVisibleNote && value <= a.maxVisibleNote)
                                            {
                                                int x = startX + i * noteSizeX - scrollX;
                                                int y = virtualSizeY - value * noteSizeY - scrollY;

                                                g.AntiAliasing = true;
                                                g.PushTranslation(x, y);
                                                g.FillConvexPath(stopNoteGeometry[zoomLevel - MinZoomLevel], g.GetVerticalGradientBrush(color, noteSizeY, 0.8f));
                                                g.DrawConvexPath(stopNoteGeometry[zoomLevel - MinZoomLevel], theme.BlackBrush);
                                                g.PopTransform();
                                                g.AntiAliasing = false;
                                            }

                                            lastNoteTime = int.MinValue;
                                        }
                                        else if (note.IsValid)
                                        {
                                            lastNoteValue = note.Value;
                                            lastNoteInstrument = note.Instrument;
                                            lastNoteTime = i;
                                        }
                                    }

                                    if (c == editChannel)
                                        g.DrawText(pattern.Name, ThemeBase.FontBig, startX + bigTextPosX - scrollX, bigTextPosY, whiteKeyBrush);
                                }

                                if (lastNoteTime != int.MinValue && p != (a.maxVisiblePattern - 1))
                                {
                                    lastNoteTime -= Song.PatternLength;
                                }
                            }

                            // Last note
                            if (lastNoteValue != Note.NoteInvalid && lastNoteTime != int.MinValue)
                            {
                                int startX = (a.maxVisiblePattern - 1) * patternSizeX;
                                if (lastNoteValue >= a.minVisibleNote && lastNoteValue <= a.maxVisibleNote)
                                {
                                    int i = Song.PatternLength;
                                    int x = startX + lastNoteTime * noteSizeX - scrollX;
                                    int y = virtualSizeY - lastNoteValue * noteSizeY - scrollY;
                                    var instrument = lastNoteInstrument;
                                    var color = instrument == null ? ThemeBase.LightGreyFillColor1 : instrument.Color;
                                    if (dimmed) color = Color.FromArgb((int)(color.A * 0.2f), color);

                                    g.PushTranslation(x, y);
                                    g.FillRectangle(0, 0, (i - lastNoteTime) * noteSizeX, noteSizeY, g.GetVerticalGradientBrush(color, noteSizeY, 0.8f));
                                    g.DrawRectangle(0, 0, (i - lastNoteTime) * noteSizeX, noteSizeY, theme.BlackBrush);
                                    g.PopTransform();
                                }
                            }
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < Note.NoteMax; i++)
                    {
                        if (App.Project.SamplesMapping[i] != null)
                        {
                            var mapping = App.Project.SamplesMapping[i];
                            var y = virtualSizeY - i * noteSizeY - scrollY;

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
            else if (editMode == EditionMode.Enveloppe)
            {
                // Draw the enveloppe value backgrounds
                const int maxValues = 126;
                int maxVisibleValue = maxValues - Math.Min((int)Math.Floor(scrollY / (float)envelopeSizeY), maxValues);
                int minVisibleValue = maxValues - Math.Max((int)Math.Ceiling((scrollY + Height) / (float)envelopeSizeY), 0);

                var env = EditEnvelope;
                var spacing = editEnvelope == Envelope.Arpeggio ? 12 : 16;

                for (int i = minVisibleValue; i < maxVisibleValue; i++)
                {
                    int value = i - 64;
                    int y = (virtualSizeY - envelopeSizeY * i) - scrollY;
                    if ((value % spacing) == 0)
                        g.FillRectangle(0, y - envelopeSizeY, env.Length * noteSizeX - scrollX, y, value == 0 ? theme.DarkGreyLineBrush2 : theme.DarkGreyFillBrush1);

                    g.DrawLine(0, y, env.Length * noteSizeX - scrollX, y, theme.DarkGreyLineBrush2);
                }

                // Draw the vertical bars.
                for (int b = 0; b < env.Length; b++)
                {
                    int x = b * noteSizeX - scrollX;
                    if (b != 0) g.DrawLine(x, 0, x, Height, theme.DarkGreyLineBrush2);
                }

                if (env.Loop >= 0)
                    g.DrawLine(env.Loop * noteSizeX - scrollX, 0, env.Loop * noteSizeX - scrollX, Height, theme.DarkGreyLineBrush1);
                if (env.Length > 0)
                    g.DrawLine(env.Length * noteSizeX - scrollX, 0, env.Length * noteSizeX - scrollX, Height, theme.DarkGreyLineBrush1);

                int seekX = App.GetEnvelopeFrame(editEnvelope) * noteSizeX - scrollX;
                g.FillRectangle(seekX + 1, 0, seekX + noteSizeX, Height, playPositionBrush);

                if (editEnvelope == Envelope.Arpeggio)
                {
                    for (int i = 0; i < env.Length; i++)
                    {
                        int x = i * noteSizeX - scrollX;
                        int y = (virtualSizeY - envelopeSizeY * (env.Values[i] + 64)) - scrollY;
                        g.FillRectangle(x, y - envelopeSizeY, x + noteSizeX, y, g.GetVerticalGradientBrush(ThemeBase.LightGreyFillColor1, envelopeSizeY, 0.8f));
                        g.DrawRectangle(x, y - envelopeSizeY, x + noteSizeX, y, theme.BlackBrush);
                    }
                }
                else
                {
                    for (int i = 0; i < env.Length; i++)
                    {
                        int val = env.Values[i];

                        int x = i * noteSizeX - scrollX;
                        int y0, y1;

                        if (val >= 0)
                        {
                            y0 = (virtualSizeY - envelopeSizeY * (val + 64 + 1)) - scrollY;
                            y1 = (virtualSizeY - envelopeSizeY * (64) - scrollY);
                        }
                        else
                        {
                            y1 = (virtualSizeY - envelopeSizeY * (val + 64)) - scrollY;
                            y0 = (virtualSizeY - envelopeSizeY * (64 + 1) - scrollY);
                        }

                        g.FillRectangle(x, y0, x + noteSizeX, y1, theme.LightGreyFillBrush1);
                        g.DrawRectangle(x, y0, x + noteSizeX, y1, theme.BlackBrush);
                    }
                }

                g.DrawText($"Editing Instrument {editInstrument.Name} ({Envelope.EnvelopeStrings[editEnvelope]})", ThemeBase.FontBig, bigTextPosX, bigTextPosY, whiteKeyBrush);
            }

            g.PopClip();
            g.PopTransform();
        }

        protected override void OnRender(RenderGraphics g)
        {
            var a = new RenderArea();

            a.maxVisibleNote = numNotes - Utils.Clamp((int)Math.Floor(scrollY / (float)noteSizeY), 0, numNotes);
            a.minVisibleNote = numNotes - Utils.Clamp((int)Math.Ceiling((scrollY + Height - headerAndEffectSizeY) / (float)noteSizeY), 0, numNotes);
            a.maxVisibleOctave = (int)Math.Ceiling(a.maxVisibleNote / 12.0f);
            a.minVisibleOctave = (int)Math.Floor(a.minVisibleNote / 12.0f);
            a.minVisiblePattern = Math.Max((int)Math.Floor(scrollX / (float)patternSizeX), 0);
            a.maxVisiblePattern = Math.Min((int)Math.Ceiling((scrollX + Width) / (float)patternSizeX), Song.Length);

            RenderHeader(g, a);
            RenderEffectList(g);
            RenderEffectPanel(g, a);
            RenderPiano(g, a);
            RenderNotes(g, a);
        }

        void ResizeEnvelope(MouseEventArgs e)
        {
            bool left  = e.Button.HasFlag(MouseButtons.Left);
            bool right = e.Button.HasFlag(MouseButtons.Right);

            var env = EditEnvelope;
            int length = (int)Math.Round((e.X - whiteKeySizeX + scrollX) / (double)noteSizeX);

            if (left  && env.Length == length ||
                right && env.Loop   == length)
            {
                return;
            }

            if (left)
            {
                env.Length = length;
            }
            else
            {
                env.Loop = length;
            }

            ClampScroll();
            ConditionalInvalidate();
        }

        void ChangeEffectValue(MouseEventArgs e)
        {
            var ratio = Utils.Clamp(1.0f - (e.Y - headerSizeY) / (float)effectPanelSizeY, 0.0f, 1.0f);
            var pattern = Song.Channels[editChannel].PatternInstances[effectPatternIdx];

            if (selectedEffectIdx == -1)
            {
                byte val = (byte)Math.Round(ratio * Note.VolumeMax);
                pattern.Notes[effectNoteIdx].Volume = val;
                pattern.UpdateLastValidNotesAndVolume();
            }
            else
            {
                if (pattern.Notes[effectNoteIdx].Effect == Note.EffectNone)
                    pattern.Notes[effectNoteIdx].Effect = (byte)(selectedEffectIdx + 1);
                byte val = (byte)Math.Round(ratio * Note.GetEffectMaxValue(Song, pattern.Notes[effectNoteIdx].Effect));
                pattern.Notes[effectNoteIdx].EffectParam = val;
            }

            ConditionalInvalidate();
        }

        void DrawEnvelope(MouseEventArgs e)
        {
            if (GetEnvelopeValueForCoord(e.X, e.Y, out int idx, out sbyte value))
            {
                if (idx >= 0 && idx < editInstrument.Envelopes[editEnvelope].Length)
                {
                    Envelope.GetMinValueValue(editEnvelope, out int min, out int max);
                    editInstrument.Envelopes[editEnvelope].Values[idx] = (sbyte)Math.Max(min, Math.Min(max, value));
                    ConditionalInvalidate();
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
                        int note = (baseOctave + i) * 12 + j + 1;
                        if (note != playingNote)
                        {
                            playingNote = note;
                            App.PlayInstrumentNote(playingNote, true);
                            ConditionalInvalidate();
                        }
                        return;
                    }
                }
                for (int j = 0; j < 12 && i * 12 + j < numNotes; j++)
                {
                    if (!IsBlackKey(j) && PointInRectangle(GetKeyRectangle(i, j), x, y))
                    {
                        int note = (baseOctave + i) * 12 + j + 1;
                        if (note != playingNote)
                        {
                            playingNote = note;
                            App.PlayInstrumentNote(playingNote, true);
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

            if (editMode == EditionMode.DPCM && left && GetNoteForCoord(e.X, e.Y, out int patternIdx, out int noteIdx, out byte noteValue))
            {
                var mapping = App.Project.SamplesMapping[noteValue];
                if (left && mapping != null)
                {
                    
                    var dlg = new PropertyDialog(PointToScreen(new Point(e.X, e.Y)), 160);
                    dlg.Properties.AddColoredString(mapping.Sample.Name, ThemeBase.LightGreyFillColor2);
                    dlg.Properties.AddIntegerRange("Pitch :", mapping.Pitch, 0, 15);
                    dlg.Properties.AddBoolean("Loop :", mapping.Loop);
                    dlg.Properties.Build();

                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        var newName  = dlg.Properties.GetPropertyValue<string>(0);

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
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            bool left   = e.Button.HasFlag(MouseButtons.Left);
            bool middle = e.Button.HasFlag(MouseButtons.Middle) || (e.Button.HasFlag(MouseButtons.Left) && ModifierKeys.HasFlag(Keys.Alt));
            bool right  = e.Button.HasFlag(MouseButtons.Right);

            if (left && e.Y > headerAndEffectSizeY && e.X < whiteKeySizeX)
            {
                playingPiano = true;
                PlayPiano(e.X, e.Y);
                Capture = true;
            }
            else if (left && editMode == EditionMode.Channel && e.Y < headerSizeY && e.X > whiteKeySizeX)
            {
                int frame = (int)Math.Floor((e.X - whiteKeySizeX + scrollX) / (float)noteSizeX);
                App.Seek(frame);
            }
            else if (left && showEffectsPanel && editMode == EditionMode.Channel && e.X < whiteKeySizeX && e.X > headerSizeY && e.Y < headerAndEffectSizeY)
            {
                int effectIdx = (e.Y - headerSizeY) / effectButtonSizeY - 1;
                if (effectIdx >= -1 && effectIdx < 3)
                {
                    selectedEffectIdx = effectIdx;
                    ConditionalInvalidate();
                }
            }
            else if (middle && e.Y > headerSizeY && e.X > whiteKeySizeX)
            {
                mouseLastX = e.X;
                mouseLastY = e.Y;
                scrolling = true;
                Capture = true;
            }
            else if (!resizingEnvelope && (left || right) && editMode == EditionMode.Enveloppe && e.X > whiteKeySizeX && e.Y < headerAndEffectSizeY)
            {
                mouseLastX = e.X;
                mouseLastY = e.Y;
                resizingEnvelope = true;
                Capture = true;
                App.UndoRedoManager.BeginTransaction(TransactionScope.Instrument, editInstrument.Id);
                ResizeEnvelope(e);
            }
            else if (!drawingEnvelope && (left || right) && editMode == EditionMode.Enveloppe && e.X > whiteKeySizeX && e.Y > headerAndEffectSizeY)
            {
                mouseLastX = e.X;
                mouseLastY = e.Y;
                drawingEnvelope = true;
                Capture = true;
                App.UndoRedoManager.BeginTransaction(TransactionScope.Instrument, editInstrument.Id);
                DrawEnvelope(e); 
            }
            else if (!changingEffectValue && left && editMode == EditionMode.Channel && e.X > whiteKeySizeX && e.Y > headerSizeY && e.Y < headerAndEffectSizeY)
            {
                if (GetEffectNoteForCoord(e.X, e.Y, out effectPatternIdx, out effectNoteIdx))
                {
                    var pattern = Song.Channels[editChannel].PatternInstances[effectPatternIdx];
                    if (pattern != null)
                    {
                        mouseLastX = e.X;
                        mouseLastY = e.Y;
                        changingEffectValue = true;
                        Capture = true;
                        App.UndoRedoManager.BeginTransaction(TransactionScope.Pattern, pattern.Id);
                        ChangeEffectValue(e);
                    }
                }
            }
            else if (editMode == EditionMode.Channel && e.X < whiteKeySizeX && e.Y < headerSizeY)
            {
                showEffectsPanel = !showEffectsPanel;
                UpdateRenderCoords();
                ClampScroll();
                ConditionalInvalidate();
                return;
            }
            else if (editMode == EditionMode.Channel && GetNoteForCoord(e.X, e.Y, out int patternIdx, out int noteIdx, out byte noteValue))
            {
                var changed = false;
                var pattern = Song.Channels[editChannel].PatternInstances[patternIdx];

                if (pattern == null)
                    return;

                if (left)
                {
                    bool ctrl = ModifierKeys.HasFlag(Keys.Control);
                    if (ctrl)
                    {
                        App.UndoRedoManager.BeginTransaction(TransactionScope.Pattern, pattern.Id);
                        pattern.Notes[noteIdx].Value = Note.NoteStop;
                        pattern.Notes[noteIdx].Instrument = null;
                        pattern.UpdateLastValidNotesAndVolume();
                        App.UndoRedoManager.EndTransaction();
                        changed = true;
                    }
                    else if (currentInstrument != null || editChannel == Channel.DPCM)
                    {
                        App.UndoRedoManager.BeginTransaction(TransactionScope.Pattern, pattern.Id);
                        pattern.Notes[noteIdx].Value = noteValue;
                        pattern.Notes[noteIdx].Instrument = editChannel == Channel.DPCM ? null : currentInstrument;
                        pattern.UpdateLastValidNotesAndVolume();
                        App.UndoRedoManager.EndTransaction();
                        changed = true;
                    }
                }
                else if (right)
                {
                    if (pattern.Notes[noteIdx].IsStop)
                    {
                        App.UndoRedoManager.BeginTransaction(TransactionScope.Pattern, pattern.Id);
                        pattern.Notes[noteIdx].IsValid = false;
                        pattern.UpdateLastValidNotesAndVolume();
                        App.UndoRedoManager.EndTransaction();
                        changed = true;
                    }
                    else
                    {
                        var foundPatternIdx = patternIdx;
                        var foundNoteIdx = noteIdx;
                        if (Song.Channels[editChannel].FindPreviousValidNote(noteValue, ref foundPatternIdx, ref foundNoteIdx))
                        {
                            var foundPattern = Song.Channels[editChannel].PatternInstances[foundPatternIdx];
                            App.UndoRedoManager.BeginTransaction(TransactionScope.Pattern, foundPattern.Id);
                            foundPattern.Notes[foundNoteIdx].IsValid = false;
                            foundPattern.UpdateLastValidNotesAndVolume();
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
            else if (editMode == EditionMode.Channel && right && GetEffectNoteForCoord(e.X, e.Y, out patternIdx, out noteIdx))
            {
                var pattern = Song.Channels[editChannel].PatternInstances[patternIdx];
                if (selectedEffectIdx == -1)
                    pattern.Notes[noteIdx].HasVolume = false;
                else
                    pattern.Notes[noteIdx].HasEffect = false;
                PatternChanged?.Invoke(pattern);
                ConditionalInvalidate();
            }
            else if (editMode == EditionMode.DPCM && (left || right) && GetNoteForCoord(e.X, e.Y, out patternIdx, out noteIdx, out noteValue))
            {
                var mapping = App.Project.SamplesMapping[noteValue];
                if (left && mapping == null)
                {
                    var filename = PlatformDialogs.ShowOpenFileDialog("Open File", "DPCM Samples (*.dmc)|*.dmc");
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
                    App.Project.SamplesMapping[noteValue] = null;
                    App.Project.CleanupUnusedSamples();
                    App.UndoRedoManager.EndTransaction();
                    ConditionalInvalidate();
                }
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateRenderCoords();
            ClampScroll();
        }

        public void ClampScroll()
        {
            if (Song != null)
            {
                int minScrollX = 0;
                int minScrollY = 0;
                int maxScrollX = 0;
                int maxScrollY = Math.Max(virtualSizeY + headerAndEffectSizeY - Height, 0);

                if (editMode == EditionMode.Channel)
                    maxScrollX = Math.Max(Song.Length * patternSizeX - ScrollMargin, 0);
                else if (editMode == EditionMode.Enveloppe)
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
        
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            bool middle = e.Button.HasFlag(MouseButtons.Middle) || (e.Button.HasFlag(MouseButtons.Left) && ModifierKeys.HasFlag(Keys.Alt));

            if (middle && scrolling)
            {
                int deltaX = e.X - mouseLastX;
                int deltaY = e.Y - mouseLastY;

                DoScroll(deltaX, deltaY);

                mouseLastX = e.X;
                mouseLastY = e.Y;
            }
            
            if (editMode == EditionMode.Enveloppe && (e.X > whiteKeySizeX && e.Y < headerAndEffectSizeY) || resizingEnvelope)
                Cursor.Current = Cursors.SizeWE;
            else if (changingEffectValue)
                Cursor.Current = Cursors.SizeNS;
            else
                Cursor.Current = Cursors.Default;

            if (playingPiano)
            {
                PlayPiano(e.X, e.Y);
            }
            else if (resizingEnvelope)
            {
                ResizeEnvelope(e);
            }
            else if (changingEffectValue)
            {
                ChangeEffectValue(e);
            }
            else if (drawingEnvelope)
            {
                DrawEnvelope(e);
            }

            string tooltip = "";
            if (editMode == EditionMode.Channel)
            {
                if (GetNoteForCoord(e.X, e.Y, out int patternIdx, out int noteIdx, out byte noteValue))
                {
                    tooltip = $"{Note.GetFriendlyName(noteValue)} [{patternIdx:D3}:{noteIdx:D3}]";
                    if (Song.Channels[editChannel].FindPreviousValidNote(noteValue, ref patternIdx, ref noteIdx))
                    {
                        var pat = Song.Channels[editChannel].PatternInstances[patternIdx];
                        if (pat != null)
                        {
                            var note = pat.Notes[noteIdx];
                            if (note.Instrument != null)
                                tooltip += $" ({note.Instrument.Name})";
                        }
                    }
                }
            }
            else if (editMode == EditionMode.Enveloppe)
            {
                if (GetEnvelopeValueForCoord(e.X, e.Y, out int idx, out sbyte value))
                {
                    tooltip = $"{idx:D3}:{value}";
                }
            }
            App.ToolTip = tooltip;
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            App.ToolTip = "";
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            if (playingPiano)
            {
                App.StopIntrumentNote();
                playingPiano = false;
                playingNote = -1;
                Capture = false;
                ConditionalInvalidate();
            }
            if (scrolling)
            {
                scrolling = false;
                Capture   = false;
            }
            if (resizingEnvelope)
            {
                App.UndoRedoManager.EndTransaction();
                EnvelopeResized?.Invoke();
                resizingEnvelope = false;
                Capture = false;
            }
            if (drawingEnvelope)
            {
                App.UndoRedoManager.EndTransaction();
                EnvelopeResized?.Invoke();
                drawingEnvelope = false;
                Capture = false;
            }
            if (changingEffectValue)
            {
                App.UndoRedoManager.EndTransaction();
                changingEffectValue = false;
                Capture = false;
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);

            if (editMode != EditionMode.DPCM)
            {
                int pixelX = e.X - whiteKeySizeX;
                int absoluteX = pixelX + scrollX;
                if (e.Delta < 0 && zoomLevel > MinZoomLevel) { zoomLevel--; absoluteX /= 2; }
                if (e.Delta > 0 && zoomLevel < MaxZoomLevel) { zoomLevel++; absoluteX *= 2; }
                scrollX = absoluteX - pixelX;

                UpdateRenderCoords();
                ClampScroll();
                ConditionalInvalidate();
            }
        }

        private bool GetEffectNoteForCoord(int x, int y, out int patternIdx, out int noteIdx)
        {
            if (x > whiteKeySizeX && y > headerSizeY && y < headerAndEffectSizeY)
            { 
                noteIdx = (x - whiteKeySizeX + scrollX) / noteSizeX;
                patternIdx = noteIdx / Song.PatternLength;
                noteIdx %= Song.PatternLength;

                return patternIdx < Song.Length;
            }
            else
            {
                patternIdx = -1;
                noteIdx = -1;
                return false;
            }
        }

        private bool GetNoteForCoord(int x, int y, out int patternIdx, out int noteIdx, out byte noteValue)
        {
            noteIdx    = (x - whiteKeySizeX + scrollX) / noteSizeX;
            patternIdx = noteIdx / Song.PatternLength;
            noteIdx   %= Song.PatternLength;
            noteValue  = (byte)(numNotes - Math.Min((y + scrollY - headerAndEffectSizeY) / noteSizeY, numNotes));

            return (x > whiteKeySizeX && y > headerAndEffectSizeY && patternIdx < Song.Length);
        }

        private bool GetEnvelopeValueForCoord(int x, int y, out int idx, out sbyte value)
        {
            idx = (x - whiteKeySizeX + scrollX) / noteSizeX;
            value = (sbyte)(61 - Math.Min((y + scrollY - headerAndEffectSizeY - 1) / envelopeSizeY, 128)); // TODO: Why the 61 again???

            return (x > whiteKeySizeX && y > headerAndEffectSizeY);
        }

        public void SerializeState(ProjectBuffer buffer)
        {
            int editModeInt = (int)editMode;
            buffer.Serialize(ref editModeInt);
            editMode = (EditionMode)editModeInt;

            buffer.Serialize(ref editChannel);
            buffer.Serialize(ref currentInstrument);
            buffer.Serialize(ref editEnvelope);
            buffer.Serialize(ref scrollX);
            buffer.Serialize(ref scrollY);
            buffer.Serialize(ref zoomLevel);
            buffer.Serialize(ref selectedEffectIdx);

            if (buffer.IsReading)
            {
                UpdateRenderCoords();
                ClampScroll();
                ConditionalInvalidate();
            }
        }
    }
}
