using System;
using System.Linq;
using System.Drawing;
using System.Media;
using System.Windows.Forms;
using System.Collections.Generic;
using FamiStudio.Properties;
using Color = System.Drawing.Color;
using System.Diagnostics;

#if FAMISTUDIO_WINDOWS
    using RenderBrush    = SharpDX.Direct2D1.Brush;
    using RenderBitmap   = SharpDX.Direct2D1.Bitmap;
    using RenderFont     = SharpDX.DirectWrite.TextFormat;
    using RenderControl  = FamiStudio.Direct2DControl;
    using RenderGraphics = FamiStudio.Direct2DGraphics;
    using RenderTheme    = FamiStudio.Direct2DTheme;
#else
    using RenderBrush    = FamiStudio.GLBrush;
    using RenderBitmap   = FamiStudio.GLBitmap;
    using RenderFont     = FamiStudio.GLFont;
    using RenderControl  = FamiStudio.GLControl;
    using RenderGraphics = FamiStudio.GLGraphics;
    using RenderTheme    = FamiStudio.GLTheme;
#endif

namespace FamiStudio
{
    public class ProjectExplorer : RenderControl
    {
        const int DefaultExpandButtonSizeX    = 8;
        const int DefaultButtonIconPosX       = 4;
        const int DefaultExpandButtonPosX     = 3;
        const int DefaultExpandButtonPosY     = 8;
        const int DefaultButtonIconPosY       = 4;
        const int DefaultButtonTextPosX       = 24;
        const int DefaultButtonTextPosY       = 5;
        const int DefaultButtonTextNoIconPosX = 5;
        const int DefaultSubButtonSpacingX    = 20;
        const int DefaultSubButtonPosY        = 4;
        const int DefaultScrollBarSizeX       = 8;
        const int DefaultButtonSizeY          = 23;
        const int DefaultSliderPosX           = 100;
        const int DefaultSliderPosY           = 4;
        const int DefaultSliderSizeX          = 96;
        const int DefaultSliderSizeY          = 16;
        const int DefaultSliderThumbSizeX     = 4;
        const int DefaultSliderTextPosX       = 110;
        const int DefaultCheckBoxPosX         = 20;
        const int DefaultCheckBoxPosY         = 4;

        int expandButtonSizeX;
        int buttonIconPosX;
        int buttonIconPosY;
        int buttonTextPosX;
        int buttonTextPosY;
        int buttonTextNoIconPosX;
        int expandButtonPosX;
        int expandButtonPosY;
        int subButtonSpacingX;
        int subButtonPosY;
        int buttonSizeY;
        int sliderPosX;
        int sliderPosY;
        int sliderSizeX;
        int sliderSizeY;
        int sliderThumbSizeX;
        int sliderTextPosX;
        int checkBoxPosX;
        int checkBoxPosY;
        int virtualSizeY;
        int scrollBarSizeX;
        bool needsScrollBar;

        enum ButtonType
        {
            ProjectSettings,
            SongHeader,
            Song,
            InstrumentHeader,
            Instrument,
            ArpeggioHeader,
            Arpeggio,
            ParamCheckbox,
            ParamSlider,
            ParamList,
            Max
        };

        enum SubButtonType
        {
            // Let's keep this enum and Envelope.XXX values in sync for convenience.
            VolumeEnvelope = Envelope.Volume,
            ArpeggioEnvelope = Envelope.Arpeggio,
            PitchEnvelope = Envelope.Pitch,
            DutyCycle = Envelope.DutyCycle,
            FdsWaveformEnvelope = Envelope.FdsWaveform,
            FdsModulationEnvelope = Envelope.FdsModulation,
            N163WaveformEnvelope = Envelope.N163Waveform,
            EnvelopeMax = Envelope.Count,

            // Other buttons
            Add,
            DPCM,
            LoadInstrument,
            LoadSong,
            ExpandInstrument,
            Max
        }

        // From right to left. Looks more visually pleasing than the enum order.
        static readonly int[] EnvelopeDisplayOrder =
        {
            Envelope.Arpeggio,
            Envelope.Pitch,
            Envelope.Volume,
            Envelope.DutyCycle,
            Envelope.FdsModulation,
            Envelope.FdsWaveform,
            Envelope.N163Waveform
        };

        class Button
        {
            public ButtonType type;
            public int instrumentParam = -1;
            public Song song;
            public Instrument instrument;
            public Arpeggio arpeggio;
            public ProjectExplorer projectExplorer;

            public Button(ProjectExplorer pe)
            {
                projectExplorer = pe;
            }

            private bool IsEnvelopeEmpty(Envelope env, int type)
            {
                // HACK: Volume envelope have 15 by default. 
                if (type == Envelope.Volume)
                {
                    return env.AllValuesEqual(Note.VolumeMax);
                }
                else
                {
                    return env.IsEmpty;
                }
            }

            public SubButtonType[] GetSubButtons(out bool[] active)
            {
                switch (type)
                {
                    case ButtonType.SongHeader:
                        active = new[] { true, true };
                        return new[] { SubButtonType.Add,
                                       SubButtonType.LoadSong };
                    case ButtonType.InstrumentHeader:
                        active = new[] { true ,true };
                        return new[] { SubButtonType.Add,
                                       SubButtonType.LoadInstrument };
                    case ButtonType.ArpeggioHeader:
                        active = new[] { true };
                        return new[] { SubButtonType.Add };
                    case ButtonType.Instrument:
                        if (instrument == null)
                        {
                            active = new[] { true };
                            return new[] { SubButtonType.DPCM };
                        }
                        else
                        {
                            var expandButton = projectExplorer.ShowExpandButtons() && instrument.IsExpansionInstrument;
                            var numSubButtons = instrument.NumActiveEnvelopes + (expandButton ? 1 : 0);
                            var buttons = new SubButtonType[numSubButtons];
                            active = new bool[numSubButtons];

                            for (int i = 0, j = 0; i < Envelope.Count; i++)
                            {
                                int idx = EnvelopeDisplayOrder[i];
                                if (instrument.Envelopes[idx] != null)
                                {
                                    buttons[j] = (SubButtonType)idx;
                                    active[j] = !IsEnvelopeEmpty(instrument.Envelopes[idx], idx);
                                    j++;
                                }
                            }

                            if (expandButton)
                            {
                                buttons[numSubButtons - 1] = SubButtonType.ExpandInstrument;
                                active[numSubButtons - 1]  = true;
                            }

                            return buttons;
                        }
                    case ButtonType.Arpeggio:
                        if (arpeggio != null)
                        {
                            active = new[] { true };
                            return new[] { SubButtonType.ArpeggioEnvelope };
                        }
                        break;

                }

                active = null;
                return null;
            }

            public string GetText(Project project)
            {
                if (project != null)
                {
                    switch (type)
                    {
                        case ButtonType.ProjectSettings: return string.IsNullOrEmpty(project.Author) ? $"{project.Name}" : $"{project.Name} ({project.Author})";
                        case ButtonType.SongHeader: return "Songs";
                        case ButtonType.Song: return song.Name;
                        case ButtonType.InstrumentHeader: return "Instruments";
                        case ButtonType.Instrument: return instrument == null ? "DPCM Samples" : instrument.Name;
                        case ButtonType.ArpeggioHeader: return "Arpeggios";
                        case ButtonType.Arpeggio: return arpeggio == null ? "None" : arpeggio.Name;
                        case ButtonType.ParamCheckbox:
                        case ButtonType.ParamSlider:
                        case ButtonType.ParamList: return Instrument.GetRealTimeParamName(instrumentParam);
                    }
                }

                return "";
            }

            public Color GetColor()
            {
                switch (type)
                {
                    case ButtonType.ProjectSettings:
                    case ButtonType.SongHeader:
                    case ButtonType.ArpeggioHeader:
                    case ButtonType.InstrumentHeader: return ThemeBase.DarkGreyFillColor2;
                    case ButtonType.Song: return song.Color;
                    case ButtonType.Arpeggio: return arpeggio == null ? ThemeBase.LightGreyFillColor1 : arpeggio.Color;
                    case ButtonType.ParamCheckbox:
                    case ButtonType.ParamSlider:
                    case ButtonType.ParamList:
                    case ButtonType.Instrument: return instrument == null ? ThemeBase.LightGreyFillColor1 : instrument.Color;
                }

                return ThemeBase.LightGreyFillColor1;
            }

            public RenderFont GetFont()
            {
                if (type == ButtonType.ProjectSettings)
                {
                    return ThemeBase.FontMediumBoldCenterEllipsis;
                }
                {
                    return ThemeBase.FontMediumBoldCenter;
                }
                else if ((type == ButtonType.Song       && song       == projectExplorer.selectedSong)       ||
                         (type == ButtonType.Instrument && instrument == projectExplorer.selectedInstrument) ||
                         (type == ButtonType.Arpeggio   && arpeggio   == projectExplorer.selectedArpeggio))
                {
                    return ThemeBase.FontMediumBold;
                }
                else
                {
                    return ThemeBase.FontMedium;
                }
            }

            public RenderBrush GetTextColor(RenderTheme theme)
            {
                if (type == ButtonType.ProjectSettings ||
                    type == ButtonType.SongHeader || 
                    type == ButtonType.InstrumentHeader ||
                    type == ButtonType.ArpeggioHeader)
                {
                    return theme.LightGreyFillBrush2;
                }
                else
                {
                    return theme.BlackBrush;
                }
            }

            public RenderBitmap GetIcon()
            {
                if (type == ButtonType.Song)
                {
                    return projectExplorer.bmpSong;
                }
                else if (type == ButtonType.Arpeggio)
                {
                    return projectExplorer.bmpEnvelopes[Envelope.Arpeggio];
                }
                else if (type == ButtonType.Instrument)
                {
                    var expType = instrument != null ? instrument.ExpansionType : Project.ExpansionNone;
                    return projectExplorer.bmpInstrument[expType];
                }
                return null;
            }

            public RenderBitmap GetIcon(SubButtonType sub)
            {
                switch (sub)
                {
                    case SubButtonType.Add:
                        return projectExplorer.bmpAdd;
                    case SubButtonType.DPCM:
                        return projectExplorer.bmpDPCM;
                    case SubButtonType.LoadSong:
                    case SubButtonType.LoadInstrument:
                        return projectExplorer.bmpLoadInstrument;
                    case SubButtonType.ExpandInstrument:
                        return projectExplorer.expandedInstrument == instrument ? projectExplorer.bmpExpanded : projectExplorer.bmpExpand;
                }

                return projectExplorer.bmpEnvelopes[(int)sub];
            }
        }

        enum CaptureOperation
        {
            None,
            DragInstrument,
            DragArpeggio,
            MoveSlider
        };

        static readonly bool[] captureNeedsThreshold = new[]
        {
            false,
            true,
            true,
            false
        };

        int scrollY = 0;
        int mouseLastX = 0;
        int mouseLastY = 0;
        int captureMouseX = -1;
        int captureMouseY = -1;
        int envelopeDragIdx = -1;
        bool captureThresholdMet = false;
        Button sliderDragButton = null;
        CaptureOperation captureOperation = CaptureOperation.None;
        Song selectedSong = null;
        Instrument draggedInstrument = null;
        Instrument selectedInstrument = null; // null = DPCM
        Instrument expandedInstrument = null;
        Arpeggio draggedArpeggio = null;
        Arpeggio selectedArpeggio = null;
        List<Button> buttons = new List<Button>();

        RenderTheme theme;

        RenderBrush    sliderFillBrush;
        RenderBitmap   bmpSong;
        RenderBitmap   bmpAdd;
        RenderBitmap   bmpDPCM;
        RenderBitmap   bmpLoadInstrument;
        RenderBitmap   bmpExpand;
        RenderBitmap   bmpExpanded;
        RenderBitmap   bmpCheckBoxYes;
        RenderBitmap   bmpCheckBoxNo;
        RenderBitmap   bmpButtonLeft;
        RenderBitmap   bmpButtonRight;
        RenderBitmap[] bmpInstrument = new RenderBitmap[Project.ExpansionCount];
        RenderBitmap[] bmpEnvelopes = new RenderBitmap[Envelope.Count];

        public Song SelectedSong => selectedSong;
        public Instrument SelectedInstrument => selectedInstrument;
        public Arpeggio SelectedArpeggio => selectedArpeggio;

        public delegate void EmptyDelegate();
        public delegate void InstrumentEnvelopeDelegate(Instrument instrument, int envelope);
        public delegate void InstrumentDelegate(Instrument instrument);
        public delegate void InstrumentPointDelegate(Instrument instrument, Point pos);
        public delegate void SongDelegate(Song song);
        public delegate void ArpeggioDelegate(Arpeggio arpeggio);
        public delegate void ArpeggioPointDelegate(Arpeggio arpeggio, Point pos);

        public event InstrumentEnvelopeDelegate InstrumentEdited;
        public event InstrumentDelegate InstrumentSelected;
        public event InstrumentDelegate InstrumentColorChanged;
        public event InstrumentDelegate InstrumentReplaced;
        public event InstrumentDelegate InstrumentDeleted;
        public event InstrumentPointDelegate InstrumentDraggedOutside;
        public event SongDelegate SongModified;
        public event SongDelegate SongSelected;
        public event ArpeggioDelegate ArpeggioSelected;
        public event ArpeggioDelegate ArpeggioEdited;
        public event ArpeggioDelegate ArpeggioColorChanged;
        public event ArpeggioDelegate ArpeggioDeleted;
        public event ArpeggioPointDelegate ArpeggioDraggedOutside;
        public event EmptyDelegate ProjectModified;

        public ProjectExplorer()
        {
            UpdateRenderCoords();
        }

        private void UpdateRenderCoords()
        {
            var scaling = RenderTheme.MainWindowScaling;

            expandButtonSizeX    = (int)(DefaultExpandButtonSizeX * scaling);
            buttonIconPosX       = (int)(DefaultButtonIconPosX * scaling);      
            buttonIconPosY       = (int)(DefaultButtonIconPosY * scaling);      
            buttonTextPosX       = (int)(DefaultButtonTextPosX * scaling);      
            buttonTextPosY       = (int)(DefaultButtonTextPosY * scaling);
            buttonTextNoIconPosX = (int)(DefaultButtonTextNoIconPosX * scaling);
            expandButtonPosX     = (int)(DefaultExpandButtonPosX * scaling);
            expandButtonPosY     = (int)(DefaultExpandButtonPosY * scaling);
            subButtonSpacingX    = (int)(DefaultSubButtonSpacingX * scaling);   
            subButtonPosY        = (int)(DefaultSubButtonPosY * scaling);       
            buttonSizeY          = (int)(DefaultButtonSizeY * scaling);
            sliderPosX           = (int)(DefaultSliderPosX * scaling);
            sliderPosY           = (int)(DefaultSliderPosY * scaling);
            sliderSizeX          = (int)(DefaultSliderSizeX * scaling);
            sliderSizeY          = (int)(DefaultSliderSizeY * scaling);
            sliderThumbSizeX     = (int)(DefaultSliderThumbSizeX * scaling);
            sliderTextPosX       = (int)(DefaultSliderTextPosX * scaling);
            checkBoxPosX         = (int)(DefaultCheckBoxPosX * scaling);
            checkBoxPosY         = (int)(DefaultCheckBoxPosY * scaling);
            virtualSizeY         = App?.Project == null ? Height : buttons.Count * buttonSizeY;
            needsScrollBar       = virtualSizeY > Height; 
            scrollBarSizeX       = needsScrollBar ? (int)(DefaultScrollBarSizeX * scaling) : 0;      
        }

        public void Reset()
        {
            scrollY = 0;
            selectedSong = App.Project.Songs[0];
            selectedInstrument = App.Project.Instruments.Count > 0 ? App.Project.Instruments[0] : null;
            expandedInstrument = null;
            selectedArpeggio = null;
            SongSelected?.Invoke(selectedSong);
            RefreshButtons();
            ConditionalInvalidate();
        }

        public void RefreshButtons()
        {
            Debug.Assert(captureOperation != CaptureOperation.MoveSlider);

            buttons.Clear();
            buttons.Add(new Button(this) { type = ButtonType.ProjectSettings });
            buttons.Add(new Button(this) { type = ButtonType.SongHeader });

            foreach (var song in App.Project.Songs)
                buttons.Add(new Button(this) { type = ButtonType.Song, song = song });

            buttons.Add(new Button(this) { type = ButtonType.InstrumentHeader });
            buttons.Add(new Button(this) { type = ButtonType.Instrument }); // null instrument = DPCM

            foreach (var instrument in App.Project.Instruments)
            {
                buttons.Add(new Button(this) { type = ButtonType.Instrument, instrument = instrument });

                if (instrument != null && instrument == expandedInstrument)
                {
                    var instrumentParams = instrument.GetRealTimeParams();

                    if (instrumentParams != null)
                    {
                        foreach (var param in instrumentParams)
                        {
                            var widgetType = ButtonType.ParamSlider;

                            if (Instrument.IsRealTimeParamList(param))
                                widgetType = ButtonType.ParamList;
                            else if (Instrument.GetRealTimeParamMaxValue(param) == 1)
                                widgetType = ButtonType.ParamCheckbox;

                            buttons.Add(new Button(this) { type = widgetType, instrumentParam = param, instrument = instrument });
                        }
                    }
                }
            }

            buttons.Add(new Button(this) { type = ButtonType.ArpeggioHeader });
            buttons.Add(new Button(this) { type = ButtonType.Arpeggio }); // null arpeggio = none.

            foreach (var arpeggio in App.Project.Arpeggios)
            {
                buttons.Add(new Button(this) { type = ButtonType.Arpeggio, arpeggio = arpeggio });
            }

            UpdateRenderCoords();
        }

        protected override void OnRenderInitialized(RenderGraphics g)
        {
            theme = RenderTheme.CreateResourcesForGraphics(g);

            bmpInstrument[Project.ExpansionNone]    = g.CreateBitmapFromResource("Instrument");
            bmpInstrument[Project.ExpansionVrc6]    = g.CreateBitmapFromResource("InstrumentKonami");
            bmpInstrument[Project.ExpansionVrc7]    = g.CreateBitmapFromResource("InstrumentKonami");
            bmpInstrument[Project.ExpansionFds]     = g.CreateBitmapFromResource("InstrumentFds");
            bmpInstrument[Project.ExpansionMmc5]    = g.CreateBitmapFromResource("Instrument");
            bmpInstrument[Project.ExpansionN163]    = g.CreateBitmapFromResource("InstrumentNamco");
            bmpInstrument[Project.ExpansionS5B]     = g.CreateBitmapFromResource("InstrumentSunsoft");

            bmpEnvelopes[Envelope.Volume]        = g.CreateBitmapFromResource("Volume");
            bmpEnvelopes[Envelope.Arpeggio]      = g.CreateBitmapFromResource("Arpeggio");
            bmpEnvelopes[Envelope.Pitch]         = g.CreateBitmapFromResource("Pitch");
            bmpEnvelopes[Envelope.DutyCycle]     = g.CreateBitmapFromResource("Duty");
            bmpEnvelopes[Envelope.FdsWaveform]   = g.CreateBitmapFromResource("Wave");
            bmpEnvelopes[Envelope.FdsModulation] = g.CreateBitmapFromResource("Mod");
            bmpEnvelopes[Envelope.N163Waveform]  = g.CreateBitmapFromResource("Wave");

            bmpExpand = g.CreateBitmapFromResource("InstrumentExpand");
            bmpExpanded = g.CreateBitmapFromResource("InstrumentExpanded");
            bmpCheckBoxYes = g.CreateBitmapFromResource("CheckBoxYes");
            bmpCheckBoxNo = g.CreateBitmapFromResource("CheckBoxNo");
            bmpButtonLeft = g.CreateBitmapFromResource("ButtonLeft");
            bmpButtonRight = g.CreateBitmapFromResource("ButtonRight");
            bmpSong = g.CreateBitmapFromResource("Music");
            bmpAdd = g.CreateBitmapFromResource("Add");
            bmpDPCM = g.CreateBitmapFromResource("DPCMBlack");
            bmpLoadInstrument = g.CreateBitmapFromResource("InstrumentOpen");
            sliderFillBrush = g.CreateSolidBrush(Color.FromArgb(64, Color.Black));
        }

        protected override void OnRenderTerminated()
        {
            theme.Terminate();

            for (int i = 0; i < Project.ExpansionCount; i++)
                Utils.DisposeAndNullify(ref bmpInstrument[i]);
            for (int i = 0; i < Envelope.Count; i++)
                Utils.DisposeAndNullify(ref bmpEnvelopes[i]);

            Utils.DisposeAndNullify(ref bmpExpand);
            Utils.DisposeAndNullify(ref bmpExpanded);
            Utils.DisposeAndNullify(ref bmpCheckBoxYes);
            Utils.DisposeAndNullify(ref bmpCheckBoxNo);
            Utils.DisposeAndNullify(ref bmpButtonLeft);
            Utils.DisposeAndNullify(ref bmpButtonRight);
            Utils.DisposeAndNullify(ref bmpSong);
            Utils.DisposeAndNullify(ref bmpAdd);
            Utils.DisposeAndNullify(ref bmpDPCM);
            Utils.DisposeAndNullify(ref bmpLoadInstrument);
            Utils.DisposeAndNullify(ref sliderFillBrush);
        }

        public void ConditionalInvalidate()
        {
            Invalidate();
        }

        protected bool ShowExpandButtons()
        {
            if (App.Project != null && App.Project.ExpansionAudio != Project.ExpansionNone)
                return App.Project.Instruments.Find(i => i.GetRealTimeParams() != null) != null;

            return false;
        }

        protected override void OnRender(RenderGraphics g)
        {
            g.Clear(ThemeBase.DarkGreyFillColor1);
            g.DrawLine(0, 0, 0, Height, theme.BlackBrush);

            var showExpandButton = ShowExpandButtons();
            var actualWidth = Width - scrollBarSizeX;
            var firstParam = true;
            var y = -scrollY;

            for (int i = 0; i < buttons.Count; i++)
            {
                var button = buttons[i];
                var icon = button.GetIcon();

                g.PushTranslation(0, y);

                if (button.type == ButtonType.ParamCheckbox || 
                    button.type == ButtonType.ParamSlider   || 
                    button.type == ButtonType.ParamList)
                {
                    if (firstParam)
                    {
                        var numParamButtons = 1;

                        for (int j = i + 1; j < buttons.Count; j++, numParamButtons++)
                        {
                            if (buttons[j].type != ButtonType.ParamCheckbox &&
                                buttons[j].type != ButtonType.ParamSlider &&
                                buttons[j].type != ButtonType.ParamList)
                            { 
                                break;
                            }
                        }

                        g.FillAndDrawRectangle(0, 0, actualWidth, numParamButtons * buttonSizeY, g.GetVerticalGradientBrush(button.GetColor(), numParamButtons * buttonSizeY, 0.8f), theme.BlackBrush);
                        firstParam = false;
                    }
                }
                else
                {
                    g.FillAndDrawRectangle(0, 0, actualWidth, buttonSizeY, g.GetVerticalGradientBrush(button.GetColor(), buttonSizeY, 0.8f), theme.BlackBrush);
                }

                var leftPadding = 0;
                var leftAligned = button.type == ButtonType.Instrument || button.type == ButtonType.Song || button.type == ButtonType.ParamSlider || button.type == ButtonType.ParamCheckbox || button.type == ButtonType.ParamList;

                if (showExpandButton && leftAligned)
                {
                    g.PushTranslation(1 + expandButtonSizeX, 0);
                    leftPadding = expandButtonSizeX;
                }

                g.DrawText(button.GetText(App.Project), button.GetFont(), icon == null ? buttonTextNoIconPosX : buttonTextPosX, buttonTextPosY, button.GetTextColor(theme), actualWidth - buttonTextNoIconPosX * 2);

                if (icon != null)
                    g.DrawBitmap(icon, buttonIconPosX, buttonIconPosY);

                if (leftPadding != 0)
                    g.PopTransform();

                if (button.instrumentParam >= 0)
                {
                    var paramMin = Instrument.GetRealTimeParamMinValue(button.instrumentParam);
                    var paramMax = Instrument.GetRealTimeParamMaxValue(button.instrumentParam);
                    var paramVal = button.instrument.GetRealTimeParamValue(button.instrumentParam);
                    var paramStr = button.instrument.GetRealTimeParamString(button.instrumentParam);

                    if (button.type == ButtonType.ParamSlider)
                    {
                        var valSizeX = (int)Math.Round((paramVal - paramMin) / (float)(paramMax - paramMin) * sliderSizeX);

                        g.PushTranslation(actualWidth - sliderPosX, sliderPosY);
                        g.FillRectangle(0, 0, valSizeX, sliderSizeY, sliderFillBrush);
                        g.DrawRectangle(0, 0, sliderSizeX, sliderSizeY, theme.BlackBrush);
                        g.DrawText(paramStr, ThemeBase.FontMediumCenter, 0, buttonTextPosY - sliderPosY, theme.BlackBrush, sliderSizeX);
                        g.PopTransform();
                    }
                    else if (button.type == ButtonType.ParamCheckbox)
                    {
                        g.DrawBitmap(paramVal == 0 ? bmpCheckBoxNo : bmpCheckBoxYes, actualWidth - checkBoxPosX, checkBoxPosY);
                    }
                    else if (button.type == ButtonType.ParamList)
                    {
                        var paramPrev = button.instrument.GetRealTimeParamPrevValue(button.instrumentParam, paramVal);
                        var paramNext = button.instrument.GetRealTimeParamNextValue(button.instrumentParam, paramVal);

                        g.PushTranslation(actualWidth - sliderPosX, sliderPosY);
                        g.DrawBitmap(bmpButtonLeft, 0, 0, paramVal == paramPrev ? 0.25f : 1.0f);
                        g.DrawBitmap(bmpButtonRight, sliderSizeX - bmpButtonRight.Size.Width, 0, paramVal == paramNext ? 0.25f : 1.0f);
                        g.DrawText(paramStr, ThemeBase.FontMediumCenter, 0, buttonTextPosY - sliderPosY, theme.BlackBrush, sliderSizeX);
                        g.PopTransform();
                    }
                }
                else
                {
                    var subButtons = button.GetSubButtons(out var active);
                    if (subButtons != null)
                    {
                        for (int j = 0, x = actualWidth - subButtonSpacingX; j < subButtons.Length; j++, x -= subButtonSpacingX)
                        {
                            if (subButtons[j] == SubButtonType.ExpandInstrument)
                                g.DrawBitmap(button.GetIcon(subButtons[j]), expandButtonPosX, expandButtonPosY);
                            else
                                g.DrawBitmap(button.GetIcon(subButtons[j]), x, subButtonPosY, active[j] ? 1.0f : 0.2f);
                        }
                    }
                }

                g.PopTransform();
                y += buttonSizeY;
            }

            if (needsScrollBar)
            {
                int virtualSizeY   = this.virtualSizeY;
                int scrollBarSizeY = (int)Math.Round(Height * (Height  / (float)virtualSizeY));
                int scrollBarPosY  = (int)Math.Round(Height * (scrollY / (float)virtualSizeY));

                g.FillAndDrawRectangle(actualWidth, 0, Width - 1, Height, theme.DarkGreyFillBrush1, theme.BlackBrush);
                g.FillAndDrawRectangle(actualWidth, scrollBarPosY, Width - 1, scrollBarPosY + scrollBarSizeY, theme.LightGreyFillBrush1, theme.BlackBrush);
            }
        }

        private void ClampScroll()
        {
            int minScrollY = 0;
            int maxScrollY = Math.Max(virtualSizeY - Height, 0);

            if (scrollY < minScrollY) scrollY = minScrollY;
            if (scrollY > maxScrollY) scrollY = maxScrollY;
        }

        private void DoScroll(int deltaY)
        {
            scrollY -= deltaY;
            ClampScroll();
            ConditionalInvalidate();
        }

        protected void UpdateCursor()
        {
            if (captureOperation == CaptureOperation.DragInstrument && captureThresholdMet)
            {
                Cursor.Current = envelopeDragIdx == -1 ? Cursors.DragCursor : Cursors.CopyCursor;
            }
            else if (captureOperation == CaptureOperation.DragArpeggio && captureThresholdMet)
            {
                Cursor.Current = Cursors.DragCursor;
            }
            else
            {
                Cursor.Current = Cursors.Default;
            }
        }

        private int GetButtonAtCoord(int x, int y, out SubButtonType sub)
        {
            var buttonIndex = (y + scrollY) / buttonSizeY;
            sub = SubButtonType.Max;

            if (buttonIndex >= 0 && buttonIndex < buttons.Count)
            {
                var button = buttons[buttonIndex];

                if (ShowExpandButtons() && button.instrument != null && button.instrument.IsExpansionInstrument && x < (expandButtonPosX + expandButtonSizeX))
                {
                    sub = SubButtonType.ExpandInstrument;
                    return buttonIndex;
                }

                var subButtons = button.GetSubButtons(out _);
                if (subButtons != null)
                {
                    y -= (buttonIndex * buttonSizeY - scrollY);

                    for (int i = 0; i < subButtons.Length; i++)
                    {
                        int sx = Width - scrollBarSizeX - subButtonSpacingX * (i + 1);
                        int sy = subButtonPosY;
                        int dx = x - sx;
                        int dy = y - sy;

                        if (dx >= 0 && dx < 16 * RenderTheme.MainWindowScaling &&
                            dy >= 0 && dy < 16 * RenderTheme.MainWindowScaling)
                        {
                            sub = subButtons[i];
                            break;
                        }
                        
                    }
                }

                return buttonIndex;
            }
            else
            {
                return -1;
            }
        }

        private void UpdateToolTip(MouseEventArgs e)
        {
            var tooltip = "";
            var buttonIdx = GetButtonAtCoord(e.X, e.Y, out var subButtonType);

            if (buttonIdx >= 0)
            {
                var buttonType = buttons[buttonIdx].type;

                if (buttonType == ButtonType.SongHeader)
                {
                    if (subButtonType == SubButtonType.Add)
                    {
                        tooltip = "{MouseLeft} Add new song";
                    }
                    else if (subButtonType == SubButtonType.LoadSong)
                    {
                        tooltip = "{MouseLeft} Import/merge song from another project";
                    }
                }
                else if (buttonType == ButtonType.Song)
                {
                    tooltip = "{MouseLeft} Make song current - {MouseLeft}{MouseLeft} Song properties - {MouseRight} Delete song";
                }
                else if (buttonType == ButtonType.InstrumentHeader)
                {
                    if (subButtonType == SubButtonType.Add)
                    {
                        tooltip = "{MouseLeft} Add new instrument";
                    }
                    else if (subButtonType == SubButtonType.LoadInstrument)
                    {
                        tooltip = "{MouseLeft} Import/merge instrument from another project";
                    }
                }
                else if (buttonType == ButtonType.ProjectSettings)
                {
                    tooltip = "{MouseLeft}{MouseLeft} Project properties";
                }
                else if (buttonType == ButtonType.ParamCheckbox && e.X >= Width - scrollBarSizeX - checkBoxPosX)
                {
                    tooltip = "{MouseLeft} Toggle value";
                }
                else if (buttonType == ButtonType.ParamSlider && e.X >= Width - scrollBarSizeX - sliderPosX)
                {
                    tooltip = "{MouseLeft} {Drag} Change value - {Shift} {MouseLeft} {Drag} Change value (fine)";
                    
                }
                else if (buttonType == ButtonType.ParamList && e.X >= Width - scrollBarSizeX - sliderPosX)
                {
                    tooltip = "{MouseLeft} Change value";
                }
                else if (buttonType == ButtonType.Instrument)
                {
                    if (subButtonType == SubButtonType.Max)
                    {
                        if (buttons[buttonIdx].instrument == null)
                            tooltip = "{MouseLeft} Select instrument";
                        else
                            tooltip = "{MouseLeft} Select instrument - {MouseLeft}{MouseLeft} Instrument properties\n{MouseRight} Delete instrument - {MouseLeft} {Drag} Replace instrument";
                    }
                    else
                    {
                        if (subButtonType == SubButtonType.DPCM)
                            tooltip = "{MouseLeft} Edit DPCM samples";
                        else if (subButtonType < SubButtonType.EnvelopeMax)
                            tooltip = $"{{MouseLeft}} Edit {Envelope.EnvelopeNames[(int)subButtonType].ToLower()} envelope - {{MouseRight}} Delete envelope - {{MouseLeft}} {{Drag}} Copy envelope";
                    }
                }
            }

            App.ToolTip = tooltip;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            bool left   = e.Button.HasFlag(MouseButtons.Left);
            bool middle = e.Button.HasFlag(MouseButtons.Middle) || (e.Button.HasFlag(MouseButtons.Left) && ModifierKeys.HasFlag(Keys.Alt));

            UpdateToolTip(e);

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
                if (captureOperation == CaptureOperation.MoveSlider)
                {
                    UpdateSliderValue(sliderDragButton, e, false);
                    ConditionalInvalidate();
                }
            }

            if (middle)
            {
                int deltaY = e.Y - mouseLastY;
                DoScroll(deltaY);
            }

            mouseLastX = e.X;
            mouseLastY = e.Y;

            UpdateCursor();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            if (captureOperation == CaptureOperation.DragInstrument)
            {
                if (ClientRectangle.Contains(e.X, e.Y))
                {
                    var buttonIdx = GetButtonAtCoord(e.X, e.Y, out var subButtonType);

                    var instrumentSrc = draggedInstrument;
                    var instrumentDst = buttonIdx >= 0 && buttons[buttonIdx].type == ButtonType.Instrument ? buttons[buttonIdx].instrument : null;

                    if (instrumentSrc != instrumentDst && instrumentSrc != null && instrumentDst != null)
                    {
                        if (envelopeDragIdx == -1)
                        {
                            if (instrumentSrc.ExpansionType == instrumentDst.ExpansionType)
                            {
                                if (PlatformUtils.MessageBox($"Are you sure you want to replace all notes of instrument '{instrumentDst.Name}' with '{instrumentSrc.Name}'?", "Replace intrument", MessageBoxButtons.YesNo) == DialogResult.Yes)
                                {
                                    App.UndoRedoManager.BeginTransaction(TransactionScope.Project);
                                    App.Project.ReplaceInstrument(instrumentDst, instrumentSrc);
                                    App.UndoRedoManager.EndTransaction();

                                    InstrumentReplaced?.Invoke(instrumentDst);
                                }
                            }
                        }
                        else
                        {
                            if (PlatformUtils.MessageBox($"Are you sure you want to copy the {Envelope.EnvelopeNames[envelopeDragIdx]} envelope of instrument '{instrumentSrc.Name}' to '{instrumentDst.Name}'?", "Copy Envelope", MessageBoxButtons.YesNo) == DialogResult.Yes)
                            {
                                App.UndoRedoManager.BeginTransaction(TransactionScope.Instrument, instrumentDst.Id);
                                instrumentDst.Envelopes[envelopeDragIdx] = instrumentSrc.Envelopes[envelopeDragIdx].ShallowClone();
                                App.UndoRedoManager.EndTransaction();

                                InstrumentEdited?.Invoke(instrumentDst, envelopeDragIdx);
                                Invalidate();
                            }
                        }
                    }
                }
                else
                {
                    InstrumentDraggedOutside(draggedInstrument, PointToScreen(new Point(e.X, e.Y)));
                }
            }
            else if (captureOperation == CaptureOperation.DragArpeggio)
            {
                if (ClientRectangle.Contains(e.X, e.Y))
                {
                    var buttonIdx = GetButtonAtCoord(e.X, e.Y, out var subButtonType);

                    var arpeggioSrc = draggedArpeggio;
                    var arpeggioDst = buttonIdx >= 0 && buttons[buttonIdx].type == ButtonType.Arpeggio ? buttons[buttonIdx].arpeggio : null;

                    if (arpeggioSrc != arpeggioDst && arpeggioSrc != null && arpeggioDst != null)
                    {
                        if (envelopeDragIdx == -1)
                        {
                            if (PlatformUtils.MessageBox($"Are you sure you want to replace all notes using arpeggio '{arpeggioDst.Name}' with '{arpeggioSrc.Name}'?", "Replace arpeggio?", MessageBoxButtons.YesNo) == DialogResult.Yes)
                            {
                                App.UndoRedoManager.BeginTransaction(TransactionScope.Project);
                                App.Project.ReplaceArpeggio(arpeggioDst, arpeggioSrc);
                                App.UndoRedoManager.EndTransaction();
                            }
                        }
                        else
                        {
                            if (PlatformUtils.MessageBox($"Are you sure you want to copy the arpeggio values from '{arpeggioSrc.Name}' to '{arpeggioDst.Name}'?", "Copy Arpeggio", MessageBoxButtons.YesNo) == DialogResult.Yes)
                            {
                                App.UndoRedoManager.BeginTransaction(TransactionScope.Arpeggio, arpeggioDst.Id);
                                arpeggioDst.Envelope.Length = arpeggioSrc.Envelope.Length;
                                arpeggioDst.Envelope.Loop = arpeggioSrc.Envelope.Loop;
                                Array.Copy(arpeggioSrc.Envelope.Values, arpeggioDst.Envelope.Values, arpeggioDst.Envelope.Values.Length);
                                App.UndoRedoManager.EndTransaction();

                                ArpeggioEdited?.Invoke(arpeggioDst);
                                Invalidate();
                            }
                        }
                    }
                }
                else
                {
                    ArpeggioDraggedOutside(draggedArpeggio, PointToScreen(new Point(e.X, e.Y)));
                }
            }
            else if (captureOperation == CaptureOperation.MoveSlider)
            {
                App.UndoRedoManager.EndTransaction();
            }

            draggedArpeggio = null;
            draggedInstrument = null;
            sliderDragButton = null;
            captureOperation = CaptureOperation.None;
            Capture = false;
        }

        private void StartCaptureOperation(MouseEventArgs e, CaptureOperation op)
        {
            Debug.Assert(captureOperation == CaptureOperation.None);
            captureMouseX = e.X;
            captureMouseY = e.Y;
            Capture = true;
            captureOperation = op;
            captureThresholdMet = !captureNeedsThreshold[(int)op];
        }

        private void AbortCaptureOperation()
        {
            Capture = false;
            captureOperation = CaptureOperation.None;

            if (App.UndoRedoManager.HasTransactionInProgress)
                App.UndoRedoManager.AbortTransaction();
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            DoScroll(e.Delta > 0 ? buttonSizeY * 3 : -buttonSizeY * 3);
        }

        protected override void OnResize(EventArgs e)
        {
            UpdateRenderCoords();
            ClampScroll();
            base.OnResize(e);
        }

        bool UpdateSliderValue(Button button, MouseEventArgs e, bool mustBeInside)
        {
            var buttonIdx = buttons.IndexOf(button);
            Debug.Assert(buttonIdx >= 0);

            bool shift = ModifierKeys.HasFlag(Keys.Shift);

            var actualWidth = Width - scrollBarSizeX;
            var buttonX = e.X;
            var buttonY = e.Y + scrollY - buttonIdx * buttonSizeY;

            bool insideSlider = (buttonX > (actualWidth - sliderPosX) &&
                                 buttonX < (actualWidth - sliderPosX + sliderSizeX) &&
                                 buttonY > (sliderPosY) &&
                                 buttonY < (sliderPosY + sliderSizeY));

            if (mustBeInside && !insideSlider)
                return false;

            var paramVal = button.instrument.GetRealTimeParamValue(button.instrumentParam);
            var paramMin = Instrument.GetRealTimeParamMinValue(button.instrumentParam);
            var paramMax = Instrument.GetRealTimeParamMaxValue(button.instrumentParam);
            var paramSnap = Instrument.GetRealTimeParamSnapValue(button.instrumentParam);

            if (shift)
            {
                paramVal = Utils.Clamp(paramVal + (e.X - mouseLastX) * paramSnap, paramMin, paramMax);
            }
            else
            {
                paramVal = (int)Math.Round(Utils.Lerp(paramMin, paramMax, Utils.Clamp((buttonX - (actualWidth - sliderPosX)) / (float)sliderSizeX, 0.0f, 1.0f)));
            }

            paramVal = (paramVal / paramSnap) * paramSnap;

            button.instrument.SetRealTimeParamValue(button.instrumentParam, paramVal);

            return insideSlider;
        }

        private void ImportSong()
        {
            var filename = PlatformUtils.ShowOpenFileDialog("Open File", "All Song Files (*.fms;*.txt;*.ftm)|*.fms;*.txt;*.ftm|FamiStudio Files (*.fms)|*.fms|FamiTracker Files (*.ftm)|*.ftm|FamiTracker Text Export (*.txt)|*.txt|FamiStudio Text Export (*.txt)|*.txt", ref Settings.LastInstrumentFolder);

            if (filename != null)
            {
                var dlgLog = new LogDialog(ParentForm);
                using (var scopedLog = new ScopedLogOutput(dlgLog, LogSeverity.Warning))
                {
                    Project otherProject = App.OpenProjectFile(filename, false);

                    if (otherProject == null)
                    {
                        return;
                    }

                    var songNames = new List<string>();
                    foreach (var song in otherProject.Songs)
                        songNames.Add(song.Name);

                    var dlg = new PropertyDialog(300);
                    dlg.Properties.AddLabel(null, "Select songs to import:");
                    dlg.Properties.AddStringListMulti(null, songNames.ToArray(), null);
                    dlg.Properties.Build();

                    if (dlg.ShowDialog(ParentForm) == DialogResult.OK)
                    {
                        App.UndoRedoManager.BeginTransaction(TransactionScope.Project);

                        var selected = dlg.Properties.GetPropertyValue<bool[]>(1);
                        var songIds = new List<int>();

                        for (int i = 0; i < selected.Length; i++)
                        {
                            if (selected[i])
                                songIds.Add(otherProject.Songs[i].Id);
                        }

                        bool success = false;
                        if (songIds.Count > 0)
                        {
                            otherProject.RemoveAllSongsBut(songIds.ToArray());
                            success = App.Project.MergeSongs(otherProject);
                        }

                        if (success)
                            App.UndoRedoManager.EndTransaction();
                        else
                            App.UndoRedoManager.AbortTransaction();
                    }

                    dlgLog.ShowDialogIfMessages();
                }
            }

            RefreshButtons();
            ConditionalInvalidate();
        }

        private void ImportInstruments()
        {
            var filename = PlatformUtils.ShowOpenFileDialog("Open File", "All Instrument Files (*.fti;*.fms;*.txt;*.ftm)|*.fti;*.fms;*.txt;*.ftm|FamiTracker Instrument File (*.fti)|*.fti|FamiStudio Files (*.fms)|*.fms|FamiTracker Files (*.ftm)|*.ftm|FamiTracker Text Export (*.txt)|*.txt|FamiStudio Text Export (*.txt)|*.txt", ref Settings.LastInstrumentFolder);

            if (filename != null)
            {
                var dlgLog = new LogDialog(ParentForm);
                using (var scopedLog = new ScopedLogOutput(dlgLog, LogSeverity.Warning))
                {
                    App.UndoRedoManager.BeginTransaction(TransactionScope.Project);

                    var success = false;

                    if (filename.ToLower().EndsWith("fti"))
                    {
                        success = new FamitrackerInstrumentFile().CreateFromFile(App.Project, filename) != null;
                    }
                    else
                    {
                        Project instrumentProject = App.OpenProjectFile(filename, false);

                        if (instrumentProject != null)
                        {
                            var instrumentNames = new List<string>();

                            foreach (var instrument in instrumentProject.Instruments)
                            {
                                var instName = instrument.Name;

                                if (instrument.ExpansionType != Project.ExpansionNone)
                                    instName += $" ({Project.ExpansionShortNames[instrument.ExpansionType]})";

                                instrumentNames.Add(instName);
                            }

                            var dlg = new PropertyDialog(300);
                            dlg.Properties.AddLabel(null, "Select instruments to import:");
                            dlg.Properties.AddStringListMulti(null, instrumentNames.ToArray(), null);
                            dlg.Properties.Build();

                            if (dlg.ShowDialog(ParentForm) == DialogResult.OK)
                            {
                                var selected = dlg.Properties.GetPropertyValue<bool[]>(1);
                                var instrumentsToMerge = new List<Instrument>();

                                for (int i = 0; i < selected.Length; i++)
                                {
                                    if (selected[i])
                                        instrumentsToMerge.Add(instrumentProject.Instruments[i]);
                                }

                                success = App.Project.MergeOtherProjectInstruments(instrumentsToMerge);
                            }
                        }
                    }

                    if (!success)
                        App.UndoRedoManager.AbortTransaction();
                    else
                        App.UndoRedoManager.EndTransaction();

                    dlgLog.ShowDialogIfMessages();
                }
            }

            RefreshButtons();
            ConditionalInvalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (captureOperation != CaptureOperation.None)
                return;

            bool left   = e.Button.HasFlag(MouseButtons.Left);
            bool middle = e.Button.HasFlag(MouseButtons.Middle) || (e.Button.HasFlag(MouseButtons.Left) && ModifierKeys.HasFlag(Keys.Alt));
            bool right  = e.Button.HasFlag(MouseButtons.Right);

            if (middle)
            {
                mouseLastY = e.Y;
                return;
            }

            var buttonIdx = GetButtonAtCoord(e.X, e.Y, out var subButtonType);

            if (buttonIdx >= 0)
            {
                var button = buttons[buttonIdx];

                if (left)
                {
                    if (button.type == ButtonType.SongHeader)
                    {
                        if (subButtonType == SubButtonType.Add)
                        {
                            App.UndoRedoManager.BeginTransaction(TransactionScope.ProjectProperties);
                            App.Project.CreateSong();
                            App.UndoRedoManager.EndTransaction();
                            RefreshButtons();
                            ConditionalInvalidate();
                        }
                        else if (subButtonType == SubButtonType.LoadSong)
                        {
                            ImportSong();
                        }
                    }
                    else if (button.type == ButtonType.Song)
                    {
                        if (button.song != selectedSong)
                        {
                            selectedSong = button.song;
                            SongSelected?.Invoke(selectedSong);
                            ConditionalInvalidate();
                        }
                    }
                    else if (button.type == ButtonType.InstrumentHeader)
                    {
                        if (subButtonType == SubButtonType.Add)
                        {
                            var instrumentType = Project.ExpansionNone;

                            if (App.Project.NeedsExpansionInstruments)
                            {
                                var expNames = new[] { Project.ExpansionNames[Project.ExpansionNone], App.Project.ExpansionAudioName };
                                var dlg = new PropertyDialog(PointToScreen(new Point(e.X, e.Y)), 260, true);
                                dlg.Properties.AddStringList("Expansion:", expNames, Project.ExpansionNames[Project.ExpansionNone] ); // 0
                                dlg.Properties.Build();

                                if (dlg.ShowDialog(ParentForm) == DialogResult.OK)
                                    instrumentType = dlg.Properties.GetPropertyValue<string>(0) == Project.ExpansionNames[Project.ExpansionNone] ? Project.ExpansionNone : App.Project.ExpansionAudio;
                                else
                                    return;
                            }

                            App.UndoRedoManager.BeginTransaction(TransactionScope.Project);
                            App.Project.CreateInstrument(instrumentType);
                            App.UndoRedoManager.EndTransaction();
                            RefreshButtons();
                            ConditionalInvalidate();
                        }
                        if (subButtonType == SubButtonType.LoadInstrument)
                        {
                            ImportInstruments();
                        }
                    }
                    else if (button.type == ButtonType.Instrument)
                    {
                        selectedInstrument = button.instrument;

                        if (selectedInstrument != null)
                        {
                            envelopeDragIdx = -1;
                            draggedInstrument = selectedInstrument;
                            StartCaptureOperation(e, CaptureOperation.DragInstrument);
                        }

                        if (subButtonType == SubButtonType.ExpandInstrument)
                        {                         
                            expandedInstrument = expandedInstrument == selectedInstrument ? null : selectedInstrument;
                            RefreshButtons();
                        }
                        if (subButtonType == SubButtonType.DPCM)
                        {
                            InstrumentEdited?.Invoke(selectedInstrument, Envelope.Count);
                        }
                        else if (subButtonType < SubButtonType.EnvelopeMax)
                        {
                            InstrumentEdited?.Invoke(selectedInstrument, (int)subButtonType);
                            envelopeDragIdx = (int)subButtonType;
                        }

                        InstrumentSelected?.Invoke(selectedInstrument);
                        ConditionalInvalidate();
                    }
                    else if (button.type == ButtonType.ParamSlider)
                    {
                        App.UndoRedoManager.BeginTransaction(TransactionScope.Instrument, selectedInstrument.Id);

                        if (UpdateSliderValue(button, e, true))
                        {
                            sliderDragButton = button;
                            StartCaptureOperation(e, CaptureOperation.MoveSlider);
                            ConditionalInvalidate();
                        }
                        else
                        {
                            App.UndoRedoManager.AbortTransaction();
                        }
                    }
                    else if (button.type == ButtonType.ParamCheckbox)
                    {
                        var actualWidth = Width - scrollBarSizeX;

                        if (e.X >= actualWidth - checkBoxPosX)
                        {
                            App.UndoRedoManager.BeginTransaction(TransactionScope.Instrument, selectedInstrument.Id);
                            var val = button.instrument.GetRealTimeParamValue(button.instrumentParam);
                            button.instrument.SetRealTimeParamValue(button.instrumentParam, val == 0 ? 1 : 0);
                            App.UndoRedoManager.EndTransaction();
                            ConditionalInvalidate();
                        }
                    }
                    else if (button.type == ButtonType.ParamList)
                    {
                        var actualWidth = Width - scrollBarSizeX;
                        var buttonX = e.X;
                        var leftButton  = buttonX > (actualWidth - sliderPosX) && buttonX < (actualWidth - sliderPosX + bmpButtonLeft.Size.Width);
                        var rightButton = buttonX > (actualWidth - sliderPosX + sliderSizeX - bmpButtonRight.Size.Width) && buttonX < (actualWidth - sliderPosX + sliderSizeX);
                        var delta = leftButton ? -1 : (rightButton ? 1 : 0);

                        if (leftButton || rightButton)
                        {
                            App.UndoRedoManager.BeginTransaction(TransactionScope.Instrument, selectedInstrument.Id);

                            var val = button.instrument.GetRealTimeParamValue(button.instrumentParam);

                            if (rightButton)
                                val = button.instrument.GetRealTimeParamNextValue(button.instrumentParam, val);
                            else
                                val = button.instrument.GetRealTimeParamPrevValue(button.instrumentParam, val);

                            button.instrument.SetRealTimeParamValue(button.instrumentParam, val);

                            App.UndoRedoManager.EndTransaction();
                            ConditionalInvalidate();
                        }
                    }
                    if (button.type == ButtonType.ArpeggioHeader)
                    {
                        if (subButtonType == SubButtonType.Add)
                        {
                            App.UndoRedoManager.BeginTransaction(TransactionScope.Project);
                            App.Project.CreateArpeggio();
                            App.UndoRedoManager.EndTransaction();
                            RefreshButtons();
                            ConditionalInvalidate();
                        }
                    }
                    else if (button.type == ButtonType.Arpeggio)
                    {
                        selectedArpeggio = button.arpeggio;

                        envelopeDragIdx = -1;
                        draggedArpeggio = selectedArpeggio;
                        StartCaptureOperation(e, CaptureOperation.DragArpeggio);

                        if (subButtonType < SubButtonType.EnvelopeMax)
                        {
                            envelopeDragIdx = (int)subButtonType;
                            ArpeggioEdited?.Invoke(selectedArpeggio);
                        }

                        ArpeggioSelected?.Invoke(selectedArpeggio);
                        ConditionalInvalidate();
                    }
                }
                else if (right)
                {
                    if (button.type == ButtonType.Song && App.Project.Songs.Count > 1)
                    {
                        var song = button.song;
                        if (PlatformUtils.MessageBox($"Are you sure you want to delete '{song.Name}' ?", "Delete song", MessageBoxButtons.YesNo) == DialogResult.Yes)
                        {
                            bool selectNewSong = song == selectedSong;
                            App.Stop();
                            App.UndoRedoManager.BeginTransaction(TransactionScope.ProjectProperties);
                            App.Project.DeleteSong(song);
                            if (selectNewSong)
                                selectedSong = App.Project.Songs[0];
                            SongSelected?.Invoke(selectedSong);
                            App.UndoRedoManager.EndTransaction();
                            RefreshButtons();
                            ConditionalInvalidate();
                        }
                    }
                    else if (button.type == ButtonType.Instrument && button.instrument != null)
                    {
                        var instrument = button.instrument;

                        if (subButtonType < SubButtonType.EnvelopeMax)
                        {
                            App.UndoRedoManager.BeginTransaction(TransactionScope.Instrument, instrument.Id);
                            instrument.Envelopes[(int)subButtonType].ClearToDefault((int)subButtonType);
                            App.UndoRedoManager.EndTransaction();
                            ConditionalInvalidate();
                        }
                        else if (subButtonType == SubButtonType.Max)
                        {
                            if (PlatformUtils.MessageBox($"Are you sure you want to delete '{instrument.Name}' ? All notes using this instrument will be deleted.", "Delete intrument", MessageBoxButtons.YesNo) == DialogResult.Yes)
                            {
                                bool selectNewInstrument = instrument == selectedInstrument;
                                App.StopEverything();
                                App.UndoRedoManager.BeginTransaction(TransactionScope.Project);
                                App.Project.DeleteInstrument(instrument);
                                if (selectNewInstrument)
                                    selectedInstrument = App.Project.Instruments.Count > 0 ? App.Project.Instruments[0] : null;
                                SongSelected?.Invoke(selectedSong);
                                InstrumentDeleted?.Invoke(instrument);
                                App.UndoRedoManager.EndTransaction();
                                App.StartInstrumentPlayer();
                                RefreshButtons();
                                ConditionalInvalidate();
                            }
                        }
                    }
                    else if (button.type == ButtonType.Arpeggio)
                    {
                        var arpeggio = button.arpeggio;

                        if (PlatformUtils.MessageBox($"Are you sure you want to delete '{arpeggio.Name}' ? All notes using this arpeggio will be no longer be arpeggiated.", "Delete arpeggio", MessageBoxButtons.YesNo) == DialogResult.Yes)
                        {
                            bool selectNewArpeggio = arpeggio == selectedArpeggio;
                            App.StopEverything();
                            App.UndoRedoManager.BeginTransaction(TransactionScope.Project);
                            App.Project.DeleteArpeggio(arpeggio);
                            if (selectNewArpeggio)
                                selectedArpeggio = App.Project.Arpeggios.Count > 0 ? App.Project.Arpeggios[0] : null;
                            SongSelected?.Invoke(selectedSong);
                            ArpeggioDeleted?.Invoke(arpeggio);
                            App.UndoRedoManager.EndTransaction();
                            App.StartInstrumentPlayer();
                            RefreshButtons();
                            ConditionalInvalidate();
                        }
                    }
                }
            }
        }

        private void EditProjectProperties(Point pt)
        {
            var project = App.Project;

            var dlg = new PropertyDialog(PointToScreen(pt), 320, true);
            dlg.Properties.AddString("Title :", project.Name, 31); // 0
            dlg.Properties.AddString("Author :", project.Author, 31); // 1
            dlg.Properties.AddString("Copyright :", project.Copyright, 31); // 2
            dlg.Properties.AddStringList("Expansion Audio :", Project.ExpansionNames, project.ExpansionAudioName, CommonTooltips.ExpansionAudio); // 3
            dlg.Properties.AddIntegerRange("Channels :", project.ExpansionNumChannels, 1, 8, CommonTooltips.ExpansionNumChannels); // 4 (Namco)
            dlg.Properties.AddStringList("Tempo Mode :", Project.TempoModeNames, Project.TempoModeNames[project.TempoMode], CommonTooltips.TempoMode); // 5
            dlg.Properties.AddStringList("Authoring Machine :", Project.MachineNames, Project.MachineNames[project.PalMode ? 1 : 0], CommonTooltips.AuthoringMachine); // 6
            dlg.Properties.SetPropertyEnabled(4, project.ExpansionAudio == Project.ExpansionN163);
            dlg.Properties.SetPropertyEnabled(6, project.UsesFamiStudioTempo);
            dlg.Properties.PropertyChanged += ProjectProperties_PropertyChanged;
            dlg.Properties.Build();

            if (dlg.ShowDialog(ParentForm) == DialogResult.OK)
            {
                App.UndoRedoManager.BeginTransaction(TransactionScope.ProjectProperties);

                project.Name = dlg.Properties.GetPropertyValue<string>(0);
                project.Author = dlg.Properties.GetPropertyValue<string>(1);
                project.Copyright = dlg.Properties.GetPropertyValue<string>(2);

                var tempoMode    = Array.IndexOf(Project.TempoModeNames, dlg.Properties.GetPropertyValue<string>(5));
                var expansion    = Array.IndexOf(Project.ExpansionNames, dlg.Properties.GetPropertyValue<string>(3));
                var palAuthoring = Array.IndexOf(Project.MachineNames,   dlg.Properties.GetPropertyValue<string>(6)) == 1;
                var numChannels  = dlg.Properties.GetPropertyValue<int>(4);

                var changedTempoMode        = tempoMode    != project.TempoMode;
                var changedExpansion        = expansion    != project.ExpansionAudio;
                var changedNumChannels      = numChannels  != project.ExpansionNumChannels;
                var changedAuthoringMachine = palAuthoring != project.PalMode;

                if (changedExpansion || changedNumChannels)
                {
                    if (project.ExpansionAudio == Project.ExpansionNone ||
                        (!changedExpansion && changedNumChannels) ||
                        PlatformUtils.MessageBox($"Switching expansion audio will delete all instruments and channels using the old expansion?", "Change expansion audio", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        selectedInstrument = project.Instruments.Count > 0 ? project.Instruments[0] : null;
                        App.StopEverything();
                        project.SetExpansionAudio(expansion, numChannels);
                        ProjectModified?.Invoke();
                        App.StartInstrumentPlayer();
                        Reset();
                    }
                }

                if (changedTempoMode)
                {
                    App.StopEverything();
                    if (tempoMode == Project.TempoFamiStudio)
                    {
                        if (!project.AreSongsEmpty)
                            PlatformUtils.MessageBox($"Converting from FamiTracker to FamiStudio tempo is extremely crude right now. It will ignore all speed changes and assume a tempo of 150. It is very likely that the songs will need a lot of manual corrections after.", "Change tempo mode", MessageBoxButtons.OK);
                        project.ConvertToFamiStudioTempo();
                    }
                    else if (tempoMode == Project.TempoFamiTracker)
                    {
                        if (!project.AreSongsEmpty)
                            PlatformUtils.MessageBox($"Converting from FamiStudio to FamiTracker tempo will simply set the speed to 1 and tempo to 150. It will not try to merge notes or do anything sophisticated.", "Change tempo mode", MessageBoxButtons.OK);
                        project.ConvertToFamiTrackerTempo(project.AreSongsEmpty);
                    }

                    ProjectModified?.Invoke();
                    App.StartInstrumentPlayer();
                    Reset();
                }

                if (changedAuthoringMachine && project.UsesFamiStudioTempo)
                {
                    project.PalMode = palAuthoring;
                    App.PalPlayback = palAuthoring;
                }

                App.UndoRedoManager.EndTransaction();
                ConditionalInvalidate();
            }
        }

        private void ProjectProperties_PropertyChanged(PropertyPage props, int idx, object value)
        {
            var noExpansion = props.GetPropertyValue<string>(3) == Project.ExpansionNames[Project.ExpansionNone];

            if (idx == 3) // Expansion
            {
                props.SetPropertyEnabled(4, (string)value == Project.ExpansionNames[Project.ExpansionN163]);
                props.SetPropertyEnabled(6, props.GetPropertyValue<string>(5) == "FamiStudio" && noExpansion);

                if (noExpansion)
                    props.SetStringListIndex(6, App.Project.PalMode ? 1 : 0);
                else
                    props.SetStringListIndex(6, 0);
            }
            else if (idx == 5) // Tempo Mode
            {
                props.SetPropertyEnabled(6, (string)value == Project.TempoModeNames[Project.TempoFamiStudio]);
            }
        }

        private void EditSongProperties(Point pt, Song song)
        {
            var dlg = new PropertyDialog(PointToScreen(pt), 220, true);

            dlg.Properties.UserData = song;
            dlg.Properties.AddColoredString(song.Name, song.Color); // 0
            dlg.Properties.AddColor(song.Color); // 1
            dlg.Properties.AddIntegerRange("Song Length :", song.Length, 1, Song.MaxLength, CommonTooltips.SongLength); // 2

            if (song.UsesFamiTrackerTempo)
            {
                dlg.Properties.AddIntegerRange("Tempo :", song.FamitrackerTempo, 32, 255, CommonTooltips.Tempo); // 3
                dlg.Properties.AddIntegerRange("Speed :", song.FamitrackerSpeed, 1, 31, CommonTooltips.Speed); // 4
                dlg.Properties.AddIntegerRange("Notes per Beat :", song.BeatLength, 1, 256, CommonTooltips.NotesPerBar); // 5
                dlg.Properties.AddIntegerRange("Notes per Pattern :", song.PatternLength, 1, 256, CommonTooltips.NotesPerPattern); // 6
                dlg.Properties.AddLabel("BPM :", song.BPM.ToString("n1"), CommonTooltips.BPM); // 7
            }
            else
            {
                dlg.Properties.AddIntegerRange("Frames per Note : ", song.NoteLength, Song.MinNoteLength, Song.MaxNoteLength, CommonTooltips.FramesPerNote); // 3
                dlg.Properties.AddIntegerRange("Notes per Beat : ", song.BeatLength / song.NoteLength, 1, 256, CommonTooltips.NotesPerBar); // 4
                dlg.Properties.AddIntegerRange("Notes per Pattern : ", song.PatternLength / song.NoteLength, 1, Pattern.MaxLength / song.NoteLength, CommonTooltips.NotesPerPattern); // 5
                dlg.Properties.AddLabel("BPM :", song.BPM.ToString("n1"), CommonTooltips.BPM); // 6
            }

            dlg.Properties.Build();
            dlg.Properties.PropertyChanged += SongProperties_PropertyChanged;

            if (dlg.ShowDialog(ParentForm) == DialogResult.OK)
            {
                App.UndoRedoManager.BeginTransaction(TransactionScope.ProjectProperties);

                App.Stop();
                App.Seek(0);

                var newName = dlg.Properties.GetPropertyValue<string>(0);

                if (App.Project.RenameSong(song, newName))
                {
                    song.Color = dlg.Properties.GetPropertyValue<System.Drawing.Color>(1);

                    if (song.UsesFamiTrackerTempo)
                    {
                        song.FamitrackerTempo = dlg.Properties.GetPropertyValue<int>(3);
                        song.FamitrackerSpeed = dlg.Properties.GetPropertyValue<int>(4);
                        song.SetBeatLength(dlg.Properties.GetPropertyValue<int>(5));
                        song.SetDefaultPatternLength(dlg.Properties.GetPropertyValue<int>(6));
                    }
                    else
                    {
                        var newNoteLength = dlg.Properties.GetPropertyValue<int>(3);

                        if (newNoteLength != song.NoteLength)
                        {
                            var convertTempo = PlatformUtils.MessageBox($"You changed the note length, do you want FamiStudio to attempt convert the tempo by resizing notes?", "Tempo Change", MessageBoxButtons.YesNo) == DialogResult.Yes;
                            song.ResizeNotes(newNoteLength, convertTempo);
                        }

                        song.SetBeatLength(dlg.Properties.GetPropertyValue<int>(4) * song.NoteLength);
                        song.SetDefaultPatternLength(dlg.Properties.GetPropertyValue<int>(5) * song.NoteLength);
                    }

                    song.SetLength(dlg.Properties.GetPropertyValue<int>(2));
                    SongModified?.Invoke(song);
                    App.UndoRedoManager.EndTransaction();
                    RefreshButtons();
                }
                else
                {
                    App.UndoRedoManager.AbortTransaction();
                    SystemSounds.Beep.Play();
                }

                ConditionalInvalidate();
            }
        }

        private void SongProperties_PropertyChanged(PropertyPage props, int idx, object value)
        {
            var song = props.UserData as Song;

            if (selectedSong.UsesFamiTrackerTempo && (idx == 3 || idx == 4 || idx == 5)) // 3/4 = Tempo/Speed, 5 = beat length
            {
                var tempo = props.GetPropertyValue<int>(3);
                var speed = props.GetPropertyValue<int>(4);
                var beatLength = props.GetPropertyValue<int>(5);

                props.SetLabelText(7, Song.ComputeFamiTrackerBPM(selectedSong.Project.PalMode, speed, tempo, beatLength).ToString("n1"));
            }
            else if (idx == 3 || idx == 4) // 3 = note length, 4 = beat length.
            {
                var noteLength = props.GetPropertyValue<int>(3);
                var beatLength = props.GetPropertyValue<int>(4);

                props.UpdateIntegerRange(5, 1, Pattern.MaxLength / noteLength);
                props.SetLabelText(6, Song.ComputeFamiStudioBPM(selectedSong.Project.PalMode, noteLength, beatLength * noteLength).ToString("n1"));
            }
        }

        private void EditInstrumentProperties(Point pt, Instrument instrument)
        {
            var dlg = new PropertyDialog(PointToScreen(pt), 160, true, pt.Y > Height / 2);
            dlg.Properties.AddColoredString(instrument.Name, instrument.Color); // 0
            dlg.Properties.AddColor(instrument.Color); // 1
            if (instrument.IsEnvelopeActive(Envelope.Pitch))
                dlg.Properties.AddBoolean("Relative pitch:", instrument.Envelopes[Envelope.Pitch].Relative); // 2
            dlg.Properties.Build();

            if (dlg.ShowDialog(ParentForm) == DialogResult.OK)
            {
                var newName = dlg.Properties.GetPropertyValue<string>(0);

                App.UndoRedoManager.BeginTransaction(TransactionScope.Project);

                if (App.Project.RenameInstrument(instrument, newName))
                {
                    instrument.Color = dlg.Properties.GetPropertyValue<System.Drawing.Color>(1);
                    if (instrument.IsEnvelopeActive(Envelope.Pitch))
                    {
                        var newRelative = dlg.Properties.GetPropertyValue<bool>(2);
                        if (instrument.Envelopes[Envelope.Pitch].Relative != newRelative)
                        {
                            if (!instrument.Envelopes[Envelope.Pitch].IsEmpty)
                            {
                                if (newRelative)
                                {
                                    if (PlatformUtils.MessageBox("Do you want to try to convert the pitch envelope from absolute to relative?", "Pitch Envelope", MessageBoxButtons.YesNo) == DialogResult.Yes)
                                        instrument.Envelopes[Envelope.Pitch].ConvertToRelative();
                                }
                                else
                                {
                                    if (PlatformUtils.MessageBox("Do you want to try to convert the pitch envelope from relative to absolute?", "Pitch Envelope", MessageBoxButtons.YesNo) == DialogResult.Yes)
                                        instrument.Envelopes[Envelope.Pitch].ConvertToAbsolute();
                                }
                            }

                            instrument.Envelopes[Envelope.Pitch].Relative = newRelative;
                        }
                    }
                    InstrumentColorChanged?.Invoke(instrument);
                    RefreshButtons();
                    ConditionalInvalidate();
                    App.UndoRedoManager.EndTransaction();
                }
                else
                {
                    App.UndoRedoManager.AbortTransaction();
                    SystemSounds.Beep.Play();
                }
            }
        }

        private void EditArpeggioProperties(Point pt, Arpeggio arpeggio)
        {
            var dlg = new PropertyDialog(PointToScreen(pt), 160, true, pt.Y > Height / 2);
            dlg.Properties.AddColoredString(arpeggio.Name, arpeggio.Color); // 0
            dlg.Properties.AddColor(arpeggio.Color); // 1
            dlg.Properties.Build();

            if (dlg.ShowDialog(ParentForm) == DialogResult.OK)
            {
                var newName = dlg.Properties.GetPropertyValue<string>(0);

                App.UndoRedoManager.BeginTransaction(TransactionScope.Project);

                if (App.Project.RenameArpeggio(arpeggio, newName))
                {
                    arpeggio.Color = dlg.Properties.GetPropertyValue<System.Drawing.Color>(1);
                    ArpeggioColorChanged?.Invoke(arpeggio);
                    RefreshButtons();
                    ConditionalInvalidate();
                    App.UndoRedoManager.EndTransaction();
                }
                else
                {
                    App.UndoRedoManager.AbortTransaction();
                    SystemSounds.Beep.Play();
                }
            }
        }
        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);

            var buttonIdx = GetButtonAtCoord(e.X, e.Y, out var subButtonType);

            if (buttonIdx >= 0)
            {
                if (captureOperation != CaptureOperation.None)
                    AbortCaptureOperation();

                var button = buttons[buttonIdx];
                var pt = new Point(e.X, e.Y);   

                if (button.type == ButtonType.ProjectSettings)
                {
                    EditProjectProperties(pt);
                }
                else if (button.type == ButtonType.Song)
                {
                    EditSongProperties(pt, button.song);
                }
                else if (button.type == ButtonType.Instrument && button.instrument != null && subButtonType == SubButtonType.Max)
                {
                    EditInstrumentProperties(pt, button.instrument);
                }
                else if (button.type == ButtonType.Arpeggio && button.arpeggio != null && subButtonType == SubButtonType.Max)
                {
                    EditArpeggioProperties(pt, button.arpeggio);
                }
#if !FAMISTUDIO_WINDOWS
                else
                {
                    // When pressing multiple times on mac, it creates click -> dbl click -> click -> dbl click sequences which
                    // makes the project explorer feel very sluggish. Interpret dbl click as clicks helps a lot.
                    OnMouseDown(e);
                }
#endif
            }
        }

        public void SerializeState(ProjectBuffer buffer)
        {
            buffer.Serialize(ref selectedSong);
            buffer.Serialize(ref selectedInstrument);
            buffer.Serialize(ref expandedInstrument);
            buffer.Serialize(ref scrollY);

            if (buffer.IsReading)
            {
                captureOperation = CaptureOperation.None;
                Capture = false;

                ClampScroll();
                RefreshButtons();
                ConditionalInvalidate();
            }
        }
    }
}
