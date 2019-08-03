using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using FamiStudio.Properties;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;

namespace FamiStudio
{
    public class PianoRoll : Direct2DUserControl
    {
        const int HeaderDefaultSizeY = 17;
        const int EffectPanelSizeY = 256;
        const int EffectButtonSizeY = 17;

        const int NoteDefaultSizeX = 16;
        const int NoteSizeY = 14;

        const int EnvelopeSizeY = 7;
        const int EnvelopeBias = 64;
        const int EnvelopeMax = 127;

        const int WhiteKeySizeY = 24;
        const int WhiteKeySizeX = 81;
        const int BlackKeySizeY = 14;
        const int BlackKeySizeX = 56;

        const int NumNotes = 63;
        const int NumOctaves = 6;
        const int BaseOctave = 1;
        const int OctaveSizeY = 12 * NoteSizeY;
        const int VirtualSizeY = NumNotes * NoteSizeY;

        const int MinZoomLevel = -3;
        const int MaxZoomLevel = 4;

        // TODO: Compute these when zooming, instead of on-the-fly.
        int NoteSizeX => ScaleForZoom(NoteDefaultSizeX);
        int BarSizeX => NoteSizeX * Song.BarLength;
        int PatternSizeX => NoteSizeX * Song.PatternLength;
        int HeaderSizeY => HeaderDefaultSizeY + (showEffectsPanel ? EffectPanelSizeY : 0);

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

        Theme theme;
        Brush whiteKeyBrush;
        Brush blackKeyBrush;
        Brush whiteKeyPressedBrush;
        Brush blackKeyPressedBrush;
        Brush debugBrush;
        Brush playPositionBrush;
        Bitmap bmpLoop;
        Bitmap bmpEffectExpanded;
        Bitmap bmpEffectCollapsed;
        Bitmap[] bmpEffects = new Bitmap[3];
        Bitmap[] bmpEffectsFilled = new Bitmap[3];

        PathGeometry[] stopNoteGeometry = new PathGeometry[MaxZoomLevel - MinZoomLevel + 1];

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
        int selectedEffectIdx = 0;

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

            int maxScrollY = Math.Max(VirtualSizeY + HeaderSizeY - Height, 0);

            scrollX = patternIdx * PatternSizeX;
            scrollY = maxScrollY / 2;

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

            ClampScroll();
            ConditionalInvalidate();
        }

        public void StartEditDPCMSamples()
        {
            editMode = EditionMode.DPCM;
            showEffectsPanel = false;
            zoomLevel = 0;

            ClampScroll();
            ConditionalInvalidate();
        }

        private FamiStudioForm App
        {
            get { return ParentForm as FamiStudioForm; }
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
        }

        protected override void OnDirect2DInitialized(Direct2DGraphics g)
        {
            theme = Theme.CreateResourcesForGraphics(g);
            whiteKeyBrush = g.CreateHorizontalGradientBrush(0, WhiteKeySizeX, Theme.LightGreyFillColor1, Theme.LightGreyFillColor2);
            blackKeyBrush = g.CreateHorizontalGradientBrush(0, BlackKeySizeX, Theme.DarkGreyFillColor1,  Theme.DarkGreyFillColor2);
            whiteKeyPressedBrush = g.CreateHorizontalGradientBrush(0, WhiteKeySizeX, Theme.Darken(Theme.LightGreyFillColor1), Theme.Darken(Theme.LightGreyFillColor2));
            blackKeyPressedBrush = g.CreateHorizontalGradientBrush(0, BlackKeySizeX, Theme.Lighten(Theme.DarkGreyFillColor1), Theme.Lighten(Theme.DarkGreyFillColor2));
            debugBrush = g.CreateSolidBrush(Theme.GreenColor);
            playPositionBrush = g.CreateSolidBrush(new RawColor4(Theme.LightGreyFillColor1.R, Theme.LightGreyFillColor1.G, Theme.LightGreyFillColor1.B, 0.5f));
            bmpLoop = g.ConvertBitmap(Resources.LoopSmall);

            bmpEffects[0] = g.ConvertBitmap(Resources.LoopSmall);
            bmpEffects[1] = g.ConvertBitmap(Resources.JumpSmall);
            bmpEffects[2] = g.ConvertBitmap(Resources.SpeedSmall);

            bmpEffectsFilled[0] = g.ConvertBitmap(Resources.LoopSmallFill);
            bmpEffectsFilled[1] = g.ConvertBitmap(Resources.JumpSmallFill);
            bmpEffectsFilled[2] = g.ConvertBitmap(Resources.SpeedSmallFill);

            bmpEffectExpanded = g.ConvertBitmap(Resources.ExpandedSmall);
            bmpEffectCollapsed = g.ConvertBitmap(Resources.CollapsedSmall);

            for (int z = MinZoomLevel; z <= MaxZoomLevel; z++)
            {
                int idx = z - MinZoomLevel;
                stopNoteGeometry[idx] = new PathGeometry(g.factory);
                var sink = stopNoteGeometry[idx].Open();
                sink.SetFillMode(FillMode.Winding);
                sink.BeginFigure(new RawVector2(0, 0), FigureBegin.Filled);
                sink.AddLine(new RawVector2(0, NoteSizeY));
                sink.AddLine(new RawVector2(NoteSizeX * (float)Math.Pow(2.0, z) - 1, NoteSizeY / 2));
                sink.AddLine(new RawVector2(0, 0));
                sink.EndFigure(FigureEnd.Closed);
                sink.Close();
                sink.Dispose();
            }
        }
        
        private bool IsBlackKey(int key)
        {
            return key == 1 || key == 3 || key == 6 || key == 8 || key == 10;
        }

        private RawRectangleF GetKeyRectangle(int octave, int key)
        {
            RawRectangleF rc = new RawRectangleF();

            if (IsBlackKey(key))
            {
                rc.Top = VirtualSizeY - OctaveSizeY * octave - (key + 1) * NoteSizeY - scrollY;
                rc.Right = rc.Left + BlackKeySizeX;
                rc.Bottom = rc.Top + BlackKeySizeY;
            }
            else
            {
                rc.Top = VirtualSizeY - OctaveSizeY * octave - (key / 2 + (key <= 4 ? 1 : 2)) * WhiteKeySizeY - scrollY;
                rc.Right = rc.Left + WhiteKeySizeX;
                rc.Bottom = rc.Top + WhiteKeySizeY;

                if (key == 4)
                    rc.Top += 2;
                else if (key == 5)
                    rc.Bottom += 2;
            }

            return rc;
        }
        
        protected override void OnRender(Direct2DGraphics g)
        {
            g.Clear(Theme.DarkGreyFillColor2);
            g.FillRectangle(0, 0, WhiteKeySizeX, Height, whiteKeyBrush);

            int maxVisibleNote = NumNotes - Utils.Clamp((int)Math.Floor(scrollY / (float)NoteSizeY), 0, NumNotes);
            int minVisibleNote = NumNotes - Utils.Clamp((int)Math.Ceiling((scrollY + Height - HeaderSizeY) / (float)NoteSizeY), 0, NumNotes);
            int maxVisibleOctave = (int)Math.Ceiling(maxVisibleNote / 12.0f);
            int minVisibleOctave = (int)Math.Floor(minVisibleNote / 12.0f);

            int minVisiblePattern = Math.Max((int)Math.Floor(scrollX / (float)PatternSizeX), 0);
            int maxVisiblePattern = Math.Min((int)Math.Ceiling((scrollX + Width) / (float)PatternSizeX), Song.Length);

            // Header.
            g.FillRectangle(WhiteKeySizeX, 0, Width, HeaderDefaultSizeY, theme.DarkGreyLineBrush2);
            if (showEffectsPanel)
                g.FillRectangle(WhiteKeySizeX, HeaderDefaultSizeY, Width, HeaderSizeY, theme.DarkGreyFillBrush1);

            // Ghost notes icons
            if (editMode == EditionMode.Channel)
                g.DrawBitmap(showEffectsPanel ? bmpEffectExpanded : bmpEffectCollapsed, 2, 2);

            g.PushTranslation(WhiteKeySizeX, 0);
            g.PushClipHalfPixel(0, 0, Width, HeaderSizeY);

            if (editMode == EditionMode.Enveloppe)
            {
                var env = EditEnvelope;
                if (env != null)
                {
                    if (env.Loop >= 0)
                    {
                        g.FillRectangle(env.Loop * NoteSizeX, 0, env.Length * NoteSizeX, HeaderSizeY, theme.DarkGreyFillBrush2);
                        g.DrawLineHalfPixel(env.Loop * NoteSizeX, 0, env.Loop * NoteSizeX, HeaderSizeY, theme.DarkGreyLineBrush1);
                        g.DrawBitmap(bmpLoop, env.Loop * NoteSizeX + 3, 2);
                    }
                    if (env.Length > 0)
                    {
                        g.DrawLineHalfPixel(env.Length * NoteSizeX, 0, env.Length * NoteSizeX, HeaderSizeY, theme.DarkGreyLineBrush1);
                    }
                }
            }
            else if (editMode == EditionMode.Channel)
            {
                // Draw the header bars
                for (int p = minVisiblePattern; p < maxVisiblePattern; p++)
                {
                    int x = p * PatternSizeX - scrollX;
                    if (p != 0)
                        g.DrawLineHalfPixel(x, 0, x, HeaderDefaultSizeY, theme.DarkGreyLineBrush1, 3.0f);
                    g.DrawText(p.ToString(), Theme.FontMediumCenter, x, 2, theme.LightGreyFillBrush1, PatternSizeX);
                }

                // Draw the effect icons.
                if (editMode == EditionMode.Channel && zoomLevel >= 0)
                {
                    for (int p = minVisiblePattern; p < maxVisiblePattern; p++)
                    {
                        var pattern = Song.Channels[editChannel].PatternInstances[p];
                        if (pattern != null)
                        {
                            int patternX = p * PatternSizeX - scrollX;
                            for (int i = 0; i < Song.PatternLength; i++)
                            {
                                var note = pattern.Notes[i];
                                if (note.HasEffect)
                                {
                                    g.DrawBitmap(bmpEffectsFilled[note.Effect - 1], patternX + i * NoteSizeX + (NoteSizeX / 2 - 8) + 2, 2);
                                }
                            }
                        }
                    }
                }
            }

            g.PopClip();
            g.PopTransform();
            g.DrawLineHalfPixel(0, HeaderDefaultSizeY - 1, Width, HeaderDefaultSizeY - 1, theme.DarkGreyLineBrush1);
            g.DrawLineHalfPixel(WhiteKeySizeX - 1, 0, WhiteKeySizeX - 1, HeaderSizeY, theme.DarkGreyLineBrush1);

            // Draw effect panel
            if (editMode == EditionMode.Channel && showEffectsPanel)
            {
                string[] EffectNames =
                {
                    "Jump",
                    "Skip",
                    "Speed"
                };

                g.PushTranslation(0, HeaderDefaultSizeY);
                g.PushClipHalfPixel(0, 0, WhiteKeySizeX, EffectPanelSizeY);

                for (int i = 0; i < 3; i++)
                {
                    g.PushTranslation(-1, i * EffectButtonSizeY - 1);
                    g.DrawRectangleHalfPixel(0, 0, WhiteKeySizeX, EffectButtonSizeY, theme.BlackBrush);
                    g.DrawBitmap(bmpEffects[i], 3, 3);
                    g.DrawText(EffectNames[i], selectedEffectIdx == i ? Theme.FontSmallBold : Theme.FontSmall, 18, 3, theme.BlackBrush);
                    g.PopTransform();
                }

                g.PopClip();
                g.PopTransform();

                for (int p = minVisiblePattern; p < maxVisiblePattern; p++)
                {
                    var pattern = Song.Channels[editChannel].PatternInstances[p];
                    if (pattern != null)
                    {
                        int x = p * PatternSizeX;

                        g.PushTranslation(WhiteKeySizeX, HeaderDefaultSizeY);
                        g.PushClipHalfPixel(0, 0, Width, EffectPanelSizeY);

                        for (int i = 0; i < Song.PatternLength; i++)
                        {
                            var note = pattern.Notes[Math.Min(i, Song.PatternLength - 1)];

                            if (note.HasEffect)
                            {
                                float effectMaxValue = Note.GetEffectMaxValue(Song, note.Effect);
                                float sizeY = (float)Math.Floor(note.EffectParam / effectMaxValue * EffectPanelSizeY);

                                g.PushTranslation(x + i * NoteSizeX - scrollX, 0);

                                g.FillRectangle(0, 0, NoteSizeX, EffectPanelSizeY, theme.DarkGreyFillBrush2);
                                g.FillRectangle(0, EffectPanelSizeY - sizeY, NoteSizeX, EffectPanelSizeY, theme.LightGreyFillBrush1);
                                g.DrawRectangleHalfPixel(0, EffectPanelSizeY - sizeY, NoteSizeX, EffectPanelSizeY, theme.BlackBrush);

                                var text = note.EffectParam.ToString();
                                if ((text.Length <= 2 && zoomLevel >= 0) || zoomLevel > 0)
                                    g.DrawText(text, Theme.FontSmallCenter, 0, EffectPanelSizeY - 12, theme.BlackBrush, NoteSizeX);

                                g.PopTransform();
                            }
                        }

                        g.DrawLineHalfPixel(x - scrollX, 0, x - scrollX, HeaderSizeY, theme.DarkGreyLineBrush1);

                        g.PopClip();
                        g.PopTransform();
                        g.DrawLineHalfPixel(0, HeaderSizeY - 1, Width, HeaderSizeY - 1, theme.DarkGreyLineBrush1);
                    }

                }
            }

            if (editMode == EditionMode.Channel)
            {
                int seekX = App.CurrentFrame * NoteSizeX + WhiteKeySizeX - scrollX;
                g.FillRectangle(seekX + 1, 0, seekX + NoteSizeX, HeaderSizeY, playPositionBrush);
            }
            else if (editMode == EditionMode.Enveloppe)
            {
                int seekX = App.GetEnvelopeFrame(editEnvelope) * NoteSizeX + WhiteKeySizeX - scrollX;
                g.FillRectangle(seekX + 1, 0, seekX + NoteSizeX, HeaderSizeY, playPositionBrush);
            }

            g.PushTranslation(0, HeaderSizeY);
            g.PushClipHalfPixel(0, 0, WhiteKeySizeX, Height);

            bool showSampleNames = editMode == EditionMode.Channel && editChannel == 4;
            bool showNoiseValues = editMode == EditionMode.Channel && editChannel == 3;
            
            int playOctave = -1;
            int playNote = -1;

            if (playingNote > 0)
            {
                playOctave = (playingNote - 1) / 12;
                playNote   = (playingNote - 1) - playOctave * 12;

                if (!IsBlackKey(playNote))
                    g.FillRectangle(GetKeyRectangle(playOctave, playNote), whiteKeyPressedBrush);
            }

            // Draw the piano
            for (int i = minVisibleOctave; i < maxVisibleOctave; i++)
            {
                int octaveBaseY = (VirtualSizeY - OctaveSizeY * i) - scrollY;

                for (int j = 0; j < 12; j++)
                {
                    if (i * 12 + j >= NumNotes)
                        break;

                    if (IsBlackKey(j))
                    {
                        g.FillRectangle(GetKeyRectangle(i, j), blackKeyBrush);

                        if (i == playOctave && j == playNote)
                            g.FillRectangle(GetKeyRectangle(playOctave, playNote), blackKeyPressedBrush);
                    }

                    int y = octaveBaseY - j * NoteSizeY;
                    if (j == 0)
                        g.DrawLineHalfPixel(0, y, WhiteKeySizeX, y, theme.DarkGreyLineBrush1);
                    else if (j == 5)
                        g.DrawLineHalfPixel(0, y, WhiteKeySizeX, y, theme.DarkGreyLineBrush2);

                    //if (showSampleNames)
                    //{
                    //    var mapping = App.Project.GetDPCMMapping(i * 12 + j);
                    //    if (mapping != null && mapping.Sample != null)
                    //    {
                    //        g.DrawText(mapping.Sample.Name, Theme.FontSmallRight, -4, y + 2, theme.BlackBrush, WhiteKeySizeX);
                    //    }
                    //}
                    //else if (showNoiseValues)
                    //{
                    //    if (!IsBlackKey(j))
                    //    {
                    //        int noiseValue = (i * 12 + j) & 0x0f;
                    //        g.DrawText(noiseValue.ToString(), Theme.FontSmallRight, -4, y + 2, theme.BlackBrush, WhiteKeySizeX);
                    //    }
                    //}
                }

                if (!showSampleNames)
                {
                    g.DrawText("C" + (i + BaseOctave), Theme.FontSmall, 2, octaveBaseY - 13, theme.BlackBrush);
                }
            }

            g.PopClip();
            g.DrawLineHalfPixel(WhiteKeySizeX - 1, 0, WhiteKeySizeX - 1, Height, theme.DarkGreyLineBrush1);

            g.PushClipHalfPixel(WhiteKeySizeX, 0, Width, Height);
            g.PushTranslation(WhiteKeySizeX, 0);

            if (editMode == EditionMode.Channel ||
                editMode == EditionMode.DPCM)
            {
                int maxX = editMode == EditionMode.Channel ? PatternSizeX * maxVisiblePattern - scrollX : Width;

                // Draw the note backgrounds
                for (int i = minVisibleOctave; i < maxVisibleOctave; i++)
                {
                    int octaveBaseY = (VirtualSizeY - OctaveSizeY * i) - scrollY;

                    for (int j = 0; j < 12; j++)
                    {
                        int y = octaveBaseY - j * NoteSizeY;
                        if (IsBlackKey(j))
                            g.FillRectangle(0, y - NoteSizeY, maxX, y, theme.DarkGreyFillBrush1);
                        if (i * 12 + j != NumNotes)
                            g.DrawLineHalfPixel(0, y, maxX, y, theme.DarkGreyLineBrush2);
                    }
                }

                if (editMode == EditionMode.Channel)
                {
                    // Seek
                    int seekX = App.CurrentFrame * NoteSizeX - scrollX;
                    g.FillRectangle(seekX + 1, 0, seekX + NoteSizeX, Height, playPositionBrush);

                    int barCount = Song.PatternLength / Song.BarLength;

                    // Draw the vertical bars.
                    for (int p = minVisiblePattern; p < maxVisiblePattern; p++)
                    {
                        for (int b = 0; b < barCount; b++)
                        {
                            int barMinX = p * PatternSizeX + b * BarSizeX - scrollX;

                            for (int t = 0; t < Song.BarLength; t++)
                            {
                                int x = barMinX + t * NoteSizeX;
                                if (zoomLevel < -1 && t != 0) continue;
                                if (p != 0 || b != 0 || t != 0) g.DrawLineHalfPixel(x, 0, x, Height, t == 0 ? theme.DarkGreyLineBrush1 : theme.DarkGreyLineBrush2, b == 0 && t == 0 ? 3.0f : 1.0f);
                            }
                        }
                    }

                    g.DrawLineHalfPixel(maxX, 0, maxX, Height, theme.DarkGreyLineBrush1);

                    // Pattern drawing.
                    for (int c = 0; c < Song.Channels.Length; c++)
                    {
                        if (c == editChannel || (App.GhostChannelMask & (1 << c)) != 0)
                        {
                            var lastTime = int.MinValue;
                            var lastNote = new Note() { Value = Note.NoteInvalid };
                            var dimmed = c != editChannel;

                            // Find previous valid note.
                            for (int p = minVisiblePattern - 1; p >= 0 && lastTime == int.MinValue; p--)
                            {
                                var pattern = Song.Channels[c].PatternInstances[p];
                                if (pattern != null)
                                {
                                    for (int i = Song.PatternLength - 1; i >= 0; i--)
                                    {
                                        var note = pattern.Notes[i];
                                        if (note.IsValid)
                                        {
                                            lastTime = -(minVisiblePattern - p - 1) * Song.PatternLength - (Song.PatternLength - i);
                                            lastNote = note;
                                            break;
                                        }
                                    }
                                }
                            }

                            for (int p = minVisiblePattern; p < maxVisiblePattern; p++)
                            {
                                var pattern = Song.Channels[c].PatternInstances[p];
                                if (pattern != null)
                                {
                                    int startX = p * PatternSizeX;

                                    for (int i = 0; i < Song.PatternLength; i++)
                                    {
                                        var note = pattern.Notes[i];
                                        var instrument = lastNote.Instrument;
                                        var color = instrument == null ? Theme.LightGreyFillColor1 : Direct2DGraphics.ToRawColor4(instrument.Color);
                                        if (dimmed) color.A *= 0.2f;

                                        if (lastNote.IsValid && lastTime != int.MinValue && note.IsValid)
                                        {
                                            if (lastNote.Value >= minVisibleNote && lastNote.Value <= maxVisibleNote)
                                            {
                                                int x = startX + lastTime * NoteSizeX - scrollX;
                                                int y = VirtualSizeY - lastNote.Value * NoteSizeY - scrollY;

                                                g.PushTranslation(x, y);
                                                g.FillRectangle(0, 0, (i - lastTime) * NoteSizeX, NoteSizeY, g.GetVerticalGradientBrush(color, NoteSizeY, 0.8f));
                                                g.DrawRectangleHalfPixel(0, 0, (i - lastTime) * NoteSizeX, NoteSizeY, theme.BlackBrush);
                                                g.PopTransform();
                                            }
                                        }

                                        if (note.IsStop)
                                        {
                                            int value = lastNote.IsValid ? lastNote.Value : 37; // C4 by default.

                                            if (value >= minVisibleNote && value <= maxVisibleNote)
                                            {
                                                int x = startX + i * NoteSizeX - scrollX;
                                                int y = VirtualSizeY - value * NoteSizeY - scrollY;

                                                g.AntiAliasing = true;
                                                g.PushTranslation(x, y);
                                                g.FillGeometry(stopNoteGeometry[zoomLevel - MinZoomLevel], g.GetVerticalGradientBrush(color, NoteSizeY, 0.8f));
                                                g.DrawGeometryHalfPixel(stopNoteGeometry[zoomLevel - MinZoomLevel], theme.BlackBrush);
                                                g.PopTransform();
                                                g.AntiAliasing = false;
                                            }

                                            lastTime = int.MinValue;
                                        }
                                        else if (note.IsValid)
                                        {
                                            lastNote = note;
                                            lastTime = i;
                                        }
                                    }

                                    if (c == editChannel)
                                        g.DrawText(pattern.Name, Theme.FontBig, startX + 10 - scrollX, 10, whiteKeyBrush);
                                }

                                if (lastTime != int.MinValue && p != (maxVisiblePattern - 1))
                                {
                                    lastTime -= Song.PatternLength;
                                }
                            }

                            // Last note
                            if (lastNote.IsValid && lastTime != int.MinValue)
                            {
                                int startX = (maxVisiblePattern - 1) * PatternSizeX;
                                if (lastNote.Value >= minVisibleNote && lastNote.Value <= maxVisibleNote)
                                {
                                    int i = Song.PatternLength;
                                    int x = startX + lastTime * NoteSizeX - scrollX;
                                    int y = VirtualSizeY - lastNote.Value * NoteSizeY - scrollY;
                                    var instrument = lastNote.Instrument;
                                    var color = instrument == null ? Theme.LightGreyFillColor1 : Direct2DGraphics.ToRawColor4(instrument.Color);
                                    if (dimmed) color.A *= 0.2f;

                                    g.PushTranslation(x, y);
                                    g.FillRectangle(0, 0, (i - lastTime) * NoteSizeX, NoteSizeY, g.GetVerticalGradientBrush(color, NoteSizeY, 0.8f));
                                    g.DrawRectangleHalfPixel(0, 0, (i - lastTime) * NoteSizeX, NoteSizeY, theme.BlackBrush);
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
                            var y = VirtualSizeY - i * NoteSizeY - scrollY;

                            g.PushTranslation(0, y);
                            g.FillAndDrawRectangleHalfPixel(0, 0, Width - WhiteKeySizeX, NoteSizeY, g.GetVerticalGradientBrush(Theme.LightGreyFillColor1, NoteSizeY, 0.8f), theme.BlackBrush);
                            if (mapping.Sample != null)
                            {
                                string text = $"{mapping.Sample.Name} (Pitch: {mapping.Pitch}, Loop: {mapping.Loop})";
                                g.DrawText(text, Theme.FontSmall, 4, 1, theme.BlackBrush);
                            }
                            g.PopTransform();
                        }
                    }

                    g.DrawText($"Editing DPCM Samples ({App.Project.GetTotalSampleSize()} / {Project.MaxSampleSize} Bytes)", Theme.FontBig, 10, 10, whiteKeyBrush);
                }
            }
            else if (editMode == EditionMode.Enveloppe)
            {
                // Draw the enveloppe value backgrounds
                // TODO: Get rid of that 126.
                const int maxValues = 126;
                int maxVisibleValue = maxValues - Math.Min((int)Math.Floor(scrollY / (float)EnvelopeSizeY), maxValues);
                int minVisibleValue = maxValues - Math.Max((int)Math.Ceiling((scrollY + Height) / (float)EnvelopeSizeY), 0);

                var env = EditEnvelope;
                var spacing = editEnvelope == Envelope.Arpeggio ? 12 : 16;

                for (int i = minVisibleValue; i < maxVisibleValue; i++)
                {
                    int value = i - 64;
                    int y = (VirtualSizeY - EnvelopeSizeY * i) - scrollY;
                    if ((value % spacing) == 0)
                        g.FillRectangle(0, y - EnvelopeSizeY, env.Length * NoteSizeX, y, value == 0 ? theme.DarkGreyLineBrush2 : theme.DarkGreyFillBrush1);

                    g.DrawLineHalfPixel(0, y, env.Length * NoteSizeX, y, theme.DarkGreyLineBrush2);
                }

                // Draw the vertical bars.
                for (int b = 0; b < env.Length; b++)
                {
                    int x = b * NoteSizeX - scrollX;
                    if (b != 0) g.DrawLineHalfPixel(x, 0, x, Height, theme.DarkGreyLineBrush2);
                }

                if (env.Loop >= 0)
                    g.DrawLineHalfPixel(env.Loop * NoteSizeX, 0, env.Loop * NoteSizeX, Height, theme.DarkGreyLineBrush1);
                if (env.Length > 0)
                    g.DrawLineHalfPixel(env.Length * NoteSizeX, 0, env.Length * NoteSizeX, Height, theme.DarkGreyLineBrush1);

                int seekX = App.GetEnvelopeFrame(editEnvelope) * NoteSizeX - scrollX;
                g.FillRectangle(seekX + 1, 0, seekX + NoteSizeX, Height, playPositionBrush);

                if (editEnvelope == Envelope.Arpeggio)
                {
                    for (int i = 0; i < env.Length; i++)
                    {
                        int x = i * NoteSizeX - scrollX;
                        int y = (VirtualSizeY - EnvelopeSizeY * (env.Values[i] + EnvelopeBias)) - scrollY;
                        g.FillRectangle(x, y - EnvelopeSizeY, x + NoteSizeX, y, g.GetVerticalGradientBrush(Theme.LightGreyFillColor1, EnvelopeSizeY, 0.8f));
                        g.DrawRectangleHalfPixel(x, y - EnvelopeSizeY, x + NoteSizeX, y, theme.BlackBrush);
                    }
                }
                else
                {
                    for (int i = 0; i < env.Length; i++)
                    {
                        int val = env.Values[i];

                        int x = i * NoteSizeX - scrollX;
                        int y0, y1;

                        if (val >= 0)
                        {
                            y0 = (VirtualSizeY - EnvelopeSizeY * (val + EnvelopeBias + 1)) - scrollY;
                            y1 = (VirtualSizeY - EnvelopeSizeY * (EnvelopeBias) - scrollY);
                        }
                        else
                        {
                            y1 = (VirtualSizeY - EnvelopeSizeY * (val + EnvelopeBias)) - scrollY;
                            y0 = (VirtualSizeY - EnvelopeSizeY * (EnvelopeBias + 1) - scrollY);
                        }

                        g.FillRectangle(x, y0, x + NoteSizeX, y1, g.GetVerticalGradientBrush(Theme.LightGreyFillColor1, EnvelopeSizeY, 0.8f));
                        g.DrawRectangleHalfPixel(x, y0, x + NoteSizeX, y1, theme.BlackBrush);
                    }
                }

                g.DrawText($"Editing Instrument {editInstrument.Name} ({Envelope.EnvelopeStrings[editEnvelope]})", Theme.FontBig, 10, 10, whiteKeyBrush);
            }

            g.PopTransform();
            g.PopClip();
            g.PopTransform();
        }

        void ResizeEnvelope(MouseEventArgs e)
        {
            bool left  = e.Button.HasFlag(MouseButtons.Left);
            bool right = e.Button.HasFlag(MouseButtons.Right);

            var env = EditEnvelope;
            int length = (int)Math.Round((e.X - WhiteKeySizeX) / (double)NoteSizeX);

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
            float ratio = 1.0f - (e.Y - HeaderDefaultSizeY) / (float)EffectPanelSizeY;
            if (ratio < 0) ratio = 0;
            if (ratio > 1) ratio = 1;
            var pattern = Song.Channels[editChannel].PatternInstances[effectPatternIdx];
            if (pattern.Notes[effectNoteIdx].Effect == Note.EffectNone)
                pattern.Notes[effectNoteIdx].Effect = (byte)(selectedEffectIdx + 1);
            byte val = (byte)Math.Round(ratio * Note.GetEffectMaxValue(Song, pattern.Notes[effectNoteIdx].Effect));
            pattern.Notes[effectNoteIdx].EffectParam = val;
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

        protected bool PointInRectangle(RawRectangleF rect, int x, int y)
        {
            return (x >= rect.Left && x <= rect.Right &&
                    y >= rect.Top  && y <= rect.Bottom);
        }

        protected void PlayPiano(int x, int y)
        {
            y -= HeaderSizeY;

            for (int i = 0; i < NumOctaves; i++)
            {
                for (int j = 0; j < 12 && i * 12 + j < NumNotes; j++)
                {
                    if (IsBlackKey(j) && PointInRectangle(GetKeyRectangle(i, j), x, y))
                    {
                        int note = i * 12 + j + 1;
                        if (note != playingNote)
                        {
                            playingNote = note;
                            (ParentForm as FamiStudioForm)?.PlayInstrumentNote(playingNote, true);
                            ConditionalInvalidate();
                        }
                        return;
                    }
                }
                for (int j = 0; j < 12 && i * 12 + j < NumNotes; j++)
                {
                    if (!IsBlackKey(j) && PointInRectangle(GetKeyRectangle(i, j), x, y))
                    {
                        int note = i * 12 + j + 1;
                        if (note != playingNote)
                        {
                            playingNote = note;
                            (ParentForm as FamiStudioForm)?.PlayInstrumentNote(playingNote, true);
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
                    var dlg = new EditDPCMDialog(mapping.Sample.Name, mapping.Pitch, mapping.Loop);
                    dlg.Location = PointToScreen(new System.Drawing.Point(e.X, e.Y));

                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        App.Stop();
                        App.UndoRedoManager.BeginTransaction(TransactionScope.DCPMSamples);
                        if (App.Project.RenameSample(mapping.Sample, dlg.NewName))
                        {
                            mapping.Pitch = dlg.NewPitch;
                            mapping.Loop = dlg.NewLoop;
                        }
                        App.UndoRedoManager.EndTransaction();
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

            if (left && e.Y > HeaderSizeY && e.X < WhiteKeySizeX)
            {
                playingPiano = true;
                PlayPiano(e.X, e.Y);
                Capture = true;
            }
            else if (left && editMode == EditionMode.Channel && e.Y < HeaderDefaultSizeY && e.X > WhiteKeySizeX)
            {
                int frame = (int)Math.Floor((e.X - WhiteKeySizeX + scrollX) / (float)NoteSizeX);
                App.Seek(frame);
            }
            else if (left && showEffectsPanel && editMode == EditionMode.Channel && e.X < WhiteKeySizeX && e.X > HeaderDefaultSizeY && e.Y < HeaderSizeY)
            {
                int effectIdx = (e.Y - HeaderDefaultSizeY) / EffectButtonSizeY;
                if (effectIdx >= 0 && effectIdx < 3)
                {
                    selectedEffectIdx = effectIdx;
                    ConditionalInvalidate();
                }
            }
            else if (middle && e.Y > HeaderDefaultSizeY && e.X > WhiteKeySizeX)
            {
                mouseLastX = e.X;
                mouseLastY = e.Y;
                scrolling = true;
                Capture = true;
            }
            else if (!resizingEnvelope && (left || right) && editMode == EditionMode.Enveloppe && e.X > WhiteKeySizeX && e.Y < HeaderSizeY)
            {
                mouseLastX = e.X;
                mouseLastY = e.Y;
                resizingEnvelope = true;
                Capture = true;
                App.UndoRedoManager.BeginTransaction(TransactionScope.Instrument, editInstrument.Id);
                ResizeEnvelope(e);
            }
            else if (!drawingEnvelope && (left || right) && editMode == EditionMode.Enveloppe && e.X > WhiteKeySizeX && e.Y > HeaderSizeY)
            {
                mouseLastX = e.X;
                mouseLastY = e.Y;
                drawingEnvelope = true;
                Capture = true;
                App.UndoRedoManager.BeginTransaction(TransactionScope.Instrument, editInstrument.Id);
                DrawEnvelope(e); 
            }
            else if (!changingEffectValue && left && editMode == EditionMode.Channel && e.X > WhiteKeySizeX && e.Y > HeaderDefaultSizeY && e.Y < HeaderSizeY)
            {
                if (GetEffectNoteForCoord(e.X, e.Y, out effectPatternIdx, out effectNoteIdx))
                {
                    mouseLastX = e.X;
                    mouseLastY = e.Y;
                    changingEffectValue = true;
                    Capture = true;
                    var pattern = Song.Channels[editChannel].PatternInstances[effectPatternIdx];
                    App.UndoRedoManager.BeginTransaction(TransactionScope.Pattern, pattern.Id);
                    ChangeEffectValue(e);
                }
            }
            else if (editMode == EditionMode.Channel && e.X < WhiteKeySizeX && e.Y < HeaderDefaultSizeY)
            {
                showEffectsPanel = !showEffectsPanel;
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
                        pattern.Notes[noteIdx].Value = (byte)0;
                        pattern.Notes[noteIdx].Instrument = null;
                        App.UndoRedoManager.EndTransaction();
                        changed = true;
                    }
                    else if (currentInstrument != null || editChannel == Channel.DPCM)
                    {
                        App.UndoRedoManager.BeginTransaction(TransactionScope.Pattern, pattern.Id);
                        pattern.Notes[noteIdx].Value = noteValue;
                        pattern.Notes[noteIdx].Instrument = editChannel == Channel.DPCM ? null : currentInstrument;
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
                        App.UndoRedoManager.EndTransaction();
                        changed = true;
                    }
                    else
                    {
                        var foundPatternIdx = patternIdx;
                        var foundNoteIdx = noteIdx;
                        if (FindPreviousValidNode(noteValue, ref foundPatternIdx, ref foundNoteIdx))
                        {
                            var foundPattern = Song.Channels[editChannel].PatternInstances[foundPatternIdx];
                            App.UndoRedoManager.BeginTransaction(TransactionScope.Pattern, foundPattern.Id);
                            foundPattern.Notes[foundNoteIdx].IsValid = false;
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
                pattern.Notes[noteIdx].Effect = 0;
                PatternChanged?.Invoke(pattern);
                ConditionalInvalidate();
            }
            else if (editMode == EditionMode.DPCM && (left || right) && GetNoteForCoord(e.X, e.Y, out patternIdx, out noteIdx, out noteValue))
            {
                var mapping = App.Project.SamplesMapping[noteValue];
                if (left && mapping == null)
                {
                    var ofd = new OpenFileDialog()
                    {
                        Filter = "DPCM Samples (*.dmc)|*.dmc",
                        Title = "Open File"
                    };

                    if (ofd.ShowDialog() == DialogResult.OK)
                    {
                        App.UndoRedoManager.BeginTransaction(TransactionScope.DCPMSamples);
                        var name = Path.GetFileNameWithoutExtension(ofd.FileName);
                        var sample = App.Project.CreateDPCMSample(name, File.ReadAllBytes(ofd.FileName));
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
            ClampScroll();
        }

        public void ClampScroll()
        {
            if (Song != null)
            {
                int minScrollX = 0;
                int minScrollY = 0;
                int maxScrollX = 0;
                int maxScrollY = Math.Max(VirtualSizeY + HeaderSizeY - Height, 0);

                if (editMode == EditionMode.Channel)
                    maxScrollX = Math.Max(Song.Length * PatternSizeX + WhiteKeySizeX - Width, 0);
                else if (editMode == EditionMode.Enveloppe)
                    maxScrollX = Math.Max(EditEnvelope.Length * NoteSizeX + WhiteKeySizeX - Width, 0);

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
            
            if (editMode == EditionMode.Enveloppe && (e.X > WhiteKeySizeX && e.Y < HeaderSizeY) || resizingEnvelope)
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
                    if (FindPreviousValidNode(noteValue, ref patternIdx, ref noteIdx))
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
                (ParentForm as FamiStudioForm)?.StopIntrumentNote();
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
                int pixelX = e.X - WhiteKeySizeX;
                int absoluteX = pixelX + scrollX;
                if (e.Delta < 0 && zoomLevel > MinZoomLevel) { zoomLevel--; absoluteX /= 2; }
                if (e.Delta > 0 && zoomLevel < MaxZoomLevel) { zoomLevel++; absoluteX *= 2; }
                scrollX = absoluteX - pixelX;

                ClampScroll();
                ConditionalInvalidate();
            }
        }

        private bool GetEffectNoteForCoord(int x, int y, out int patternIdx, out int noteIdx)
        {
            if (x > WhiteKeySizeX && y > HeaderDefaultSizeY && y < HeaderSizeY)
            { 
                noteIdx = (x - WhiteKeySizeX + scrollX) / NoteSizeX;
                patternIdx = noteIdx / Song.PatternLength;
                noteIdx %= Song.PatternLength;

                return true;
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
            noteIdx    = (x - WhiteKeySizeX + scrollX) / NoteSizeX;
            patternIdx = noteIdx / Song.PatternLength;
            noteIdx   %= Song.PatternLength;
            noteValue  = (byte)(NumNotes - Math.Min((y + scrollY - HeaderSizeY) / NoteSizeY, NumNotes));

            return (x > WhiteKeySizeX && y > HeaderSizeY);
        }

        private bool GetEnvelopeValueForCoord(int x, int y, out int idx, out sbyte value)
        {
            idx = (x - WhiteKeySizeX + scrollX) / NoteSizeX;
            value = (sbyte)(61 - Math.Min((y + scrollY - HeaderSizeY - 1) / EnvelopeSizeY, 126)); // TODO: Why the 61 again???

            return (x > WhiteKeySizeX && y > HeaderSizeY);
        }

        protected bool FindPreviousValidNode(int noteValue, ref int patternIdx, ref int noteIdx)
        {
            while (patternIdx >= 0)
            {
                var pattern = Song.Channels[editChannel].PatternInstances[patternIdx];

                if (pattern != null)
                {
                    while (noteIdx >= 0 && !pattern.Notes[noteIdx].IsValid) noteIdx--;

                    if (noteIdx >= 0 && pattern.Notes[noteIdx].Value == noteValue)
                    {
                        return true;
                    }
                }

                noteIdx = Song.PatternLength - 1;
                patternIdx--;
            }

            return false;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            //if (e.KeyCode == Keys.F && editMode == EditionMode.Channel)
            //{
            //    showEffectsPanel = !showEffectsPanel;
            //    ClampScroll();
            //    ConditionalInvalidate();
            //}
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
                ClampScroll();
                ConditionalInvalidate();
            }
        }
    }
}
