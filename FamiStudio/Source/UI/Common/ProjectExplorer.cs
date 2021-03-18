using System;
using System.IO;
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
        const int DefaultExpandButtonPosX     = 3;
        const int DefaultExpandButtonPosY     = 8;
        const int DefaultButtonIconPosX       = 3;
        const int DefaultButtonIconPosY       = 3;
        const int DefaultButtonTextPosX       = 21;
        const int DefaultButtonTextPosY       = 3;
        const int DefaultButtonTextNoIconPosX = 4;
        const int DefaultSubButtonSpacingX    = 18;
        const int DefaultSubButtonPosY        = 3;
        const int DefaultScrollBarSizeX       = 8;
        const int DefaultButtonSizeY          = 21;
        const int DefaultSliderPosX           = 100;
        const int DefaultSliderPosY           = 3;
        const int DefaultSliderSizeX          = 96;
        const int DefaultSliderSizeY          = 15;
        const int DefaultSliderThumbSizeX     = 3;
        const int DefaultSliderTextPosX       = 110;
        const int DefaultCheckBoxPosX         = 20;
        const int DefaultCheckBoxPosY         = 3;

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
            DpcmHeader,
            Dpcm,
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
            VolumeEnvelope        = EnvelopeType.Volume,
            ArpeggioEnvelope      = EnvelopeType.Arpeggio,
            PitchEnvelope         = EnvelopeType.Pitch,
            DutyCycle             = EnvelopeType.DutyCycle,
            FdsWaveformEnvelope   = EnvelopeType.FdsWaveform,
            FdsModulationEnvelope = EnvelopeType.FdsModulation,
            N163WaveformEnvelope  = EnvelopeType.N163Waveform,
            EnvelopeMax           = EnvelopeType.Count,

            // Other buttons
            Add,
            DPCM,
            Load,
            Save,
            EditWave,
            Play,
            Expand,
            Overflow,
            Max
        }

        // From right to left. Looks more visually pleasing than the enum order.
        static readonly int[] EnvelopeDisplayOrder =
        {
            EnvelopeType.Arpeggio,
            EnvelopeType.Pitch,
            EnvelopeType.Volume,
            EnvelopeType.DutyCycle,
            EnvelopeType.FdsModulation,
            EnvelopeType.FdsWaveform,
            EnvelopeType.N163Waveform
        };

        class Button
        {
            public string text;
            public Color color = ThemeBase.DarkGreyFillColor2;
            public RenderFont font = ThemeBase.FontMedium;
            public RenderBrush textBrush;
            public RenderBrush textDisabledBrush;
            public RenderBitmap icon;

            public ButtonType type;
            public Song song;
            public Instrument instrument;
            public Arpeggio arpeggio;
            public DPCMSample sample;
            public ProjectExplorer projectExplorer;

            public ParamInfo param;
            public TransactionScope paramScope;
            public int paramObjectId;

            public delegate string StringGetDelegate();
            public delegate int IntGetDelegate();
            public delegate int IntGetSetDelegate(int value);
            public delegate void IntSetDelegate(int value);

            public Button(ProjectExplorer pe)
            {
                projectExplorer = pe;

                if (pe.theme != null)
                    textBrush = pe.theme.LightGreyFillBrush2;

                textDisabledBrush = pe.disabledBrush;
            }

            private bool IsEnvelopeEmpty(Envelope env, int type)
            {
                return env.IsEmpty(type);
            }

            public SubButtonType[] GetSubButtons(out bool[] active)
            {
                switch (type)
                {
                    case ButtonType.SongHeader:
                        active = new[] { true, true };
                        return new[] { SubButtonType.Add,
                                       SubButtonType.Load };
                    case ButtonType.InstrumentHeader:
                        active = new[] { true ,true };
                        return new[] { SubButtonType.Add,
                                       SubButtonType.Load };
                    case ButtonType.ArpeggioHeader:
                        active = new[] { true };
                        return new[] { SubButtonType.Add };
                    case ButtonType.DpcmHeader:
                        active = new[] { true };
                        return new[] { SubButtonType.Load };
                    case ButtonType.Instrument:
                        if (instrument == null)
                        {
                            var project = projectExplorer.App.Project;
                            if (project != null && project.GetTotalMappedSampleSize() > Project.MaxMappedSampleSize)
                            {
                                active = new[] { true, true };
                                return new[] { SubButtonType.DPCM, SubButtonType.Overflow };
                            }
                            else
                            {
                                active = new[] { true };
                                return new[] { SubButtonType.DPCM };
                            }
                        }
                        else
                        {
                            var expandButton = projectExplorer.ShowExpandButtons() && InstrumentParamProvider.HasParams(instrument);
                            var numSubButtons = instrument.NumActiveEnvelopes + (expandButton ? 1 : 0);
                            var buttons = new SubButtonType[numSubButtons];
                            active = new bool[numSubButtons];

                            for (int i = 0, j = 0; i < EnvelopeType.Count; i++)
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
                                buttons[numSubButtons - 1] = SubButtonType.Expand;
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
                    case ButtonType.Dpcm:
                        active = new[] { true, true, true, true };
                        return new[] { SubButtonType.EditWave, SubButtonType.Save, SubButtonType.Play, SubButtonType.Expand };
                }

                active = null;
                return null;
            }

            public string Text
            {
                get
                {
                    if (text != null)
                    {
                        return text;
                    }
                    else if (type == ButtonType.Instrument && instrument == null)
                    {
                        var label = "DPCM Instrument";
                        if (projectExplorer.App.Project != null)
                        {
                            var mappedSamplesSize = projectExplorer.App.Project.GetTotalMappedSampleSize();
                            if (mappedSamplesSize > 0)
                                label += $" ({mappedSamplesSize} Bytes)";
                        }
                        return label;
                    }
                    else if (type == ButtonType.Dpcm)
                    {
                        return $"{sample.Name} ({sample.ProcessedData.Length} Bytes)"; 
                    }
                    else if (type == ButtonType.DpcmHeader)
                    {
                        var label = "DPCM Samples";
                        // Not useful info.
                        //if (projectExplorer.App.Project != null)
                        //{
                        //    var samplesSize = projectExplorer.App.Project.GetTotalSampleSize();
                        //    if (samplesSize > 0)
                        //        label += $" ({samplesSize} Bytes)";
                        //}
                        return label;
                    }

                    return "";
                }
            }

            public RenderFont Font
            {
                get
                {
                    if ((type == ButtonType.Song       && song       == projectExplorer.selectedSong)       ||
                        (type == ButtonType.Instrument && instrument == projectExplorer.selectedInstrument) ||
                        (type == ButtonType.Arpeggio   && arpeggio   == projectExplorer.selectedArpeggio))
                    {
                        return ThemeBase.FontMediumBold;
                    }
                    else
                    {
                        return font;
                    }
                }
            }
            
            // MATT: Store in the button
            public RenderBitmap GetIcon(SubButtonType sub)
            {
                switch (sub)
                {
                    case SubButtonType.Add:
                        return projectExplorer.bmpAdd;
                    case SubButtonType.Play:
                        return projectExplorer.bmpPlay;
                    case SubButtonType.Save:
                        return projectExplorer.bmpSave;
                    case SubButtonType.DPCM:
                        return projectExplorer.bmpDPCM;
                    case SubButtonType.EditWave:
                        return projectExplorer.bmpWaveEdit;
                    case SubButtonType.Load:
                        return projectExplorer.bmpLoad;
                    case SubButtonType.Overflow:
                        return projectExplorer.bmpOverflow;
                    case SubButtonType.Expand:
                    {
                        if (instrument != null)
                            return projectExplorer.expandedInstrument == instrument ? projectExplorer.bmpExpanded : projectExplorer.bmpExpand;
                        else 
                            return projectExplorer.expandedSample == sample ? projectExplorer.bmpExpanded : projectExplorer.bmpExpand;
                    }
                }

                return projectExplorer.bmpEnvelopes[(int)sub];
            }
        }

        enum CaptureOperation
        {
            None,
            DragInstrument,
            DragArpeggio,
            DragSample,
            MoveSlider,
            ScrollBar
        };

        static readonly bool[] captureNeedsThreshold = new[]
        {
            false,
            true,
            true,
            false,
            false,
            false
        };

        int scrollY = 0;
        int mouseLastX = 0;
        int mouseLastY = 0;
        int captureMouseX = -1;
        int captureMouseY = -1;
        int captureScrollY = -1;
        int envelopeDragIdx = -1;
        bool captureThresholdMet = false;
        Button sliderDragButton = null;
        CaptureOperation captureOperation = CaptureOperation.None;
        Song selectedSong = null;
        Instrument draggedInstrument = null;
        Instrument selectedInstrument = null; // null = DPCM
        Instrument expandedInstrument = null;
        DPCMSample expandedSample = null;
        Arpeggio draggedArpeggio = null;
        Arpeggio selectedArpeggio = null;
        DPCMSample draggedSample = null;
        List<Button> buttons = new List<Button>();

        RenderTheme theme;

        RenderBrush    sliderFillBrush;
        RenderBrush    disabledBrush;
        RenderBitmap   bmpSong;
        RenderBitmap   bmpAdd;
        RenderBitmap   bmpDPCM;
        RenderBitmap   bmpLoad;
        RenderBitmap   bmpPlay;
        RenderBitmap   bmpSave;
        RenderBitmap   bmpWaveEdit;
        RenderBitmap   bmpExpand;
        RenderBitmap   bmpExpanded;
        RenderBitmap   bmpOverflow;
        RenderBitmap   bmpCheckBoxYes;
        RenderBitmap   bmpCheckBoxNo;
        RenderBitmap   bmpButtonLeft;
        RenderBitmap   bmpButtonRight;
        RenderBitmap[] bmpInstrument = new RenderBitmap[ExpansionType.Count];
        RenderBitmap[] bmpEnvelopes = new RenderBitmap[EnvelopeType.Count];

        public Song SelectedSong => selectedSong;
        public DPCMSample DraggedSample => captureOperation == CaptureOperation.DragSample ? draggedSample : null;

        public Arpeggio SelectedArpeggio
        {
            get
            {
                return selectedArpeggio;
            }
            set
            {
                selectedArpeggio = value;
                ConditionalInvalidate();
            }
        }

        public Instrument SelectedInstrument
        {
            get
            {
                return selectedInstrument;
            }
            set
            {
                selectedInstrument = value;
                ConditionalInvalidate();
            }
        }

        public delegate void EmptyDelegate();
        public delegate void InstrumentEnvelopeDelegate(Instrument instrument, int envelope);
        public delegate void InstrumentDelegate(Instrument instrument);
        public delegate void InstrumentPointDelegate(Instrument instrument, Point pos);
        public delegate void SongDelegate(Song song);
        public delegate void ArpeggioDelegate(Arpeggio arpeggio);
        public delegate void ArpeggioPointDelegate(Arpeggio arpeggio, Point pos);
        public delegate void DPCMSamplePointDelegate(DPCMSample instrument, Point pos);
        public delegate void DPCMSampleDelegate(DPCMSample sample);

        public event InstrumentEnvelopeDelegate InstrumentEdited;
        public event InstrumentDelegate InstrumentSelected;
        public event InstrumentDelegate InstrumentColorChanged;
        public event InstrumentDelegate InstrumentReplaced;
        public event InstrumentDelegate InstrumentDeleted;
        public event InstrumentPointDelegate InstrumentDroppedOutside;
        public event SongDelegate SongModified;
        public event SongDelegate SongSelected;
        public event ArpeggioDelegate ArpeggioSelected;
        public event ArpeggioDelegate ArpeggioEdited;
        public event ArpeggioDelegate ArpeggioColorChanged;
        public event ArpeggioDelegate ArpeggioDeleted;
        public event ArpeggioPointDelegate ArpeggioDroppedOutside;
        public event DPCMSampleDelegate DPCMSampleEdited;
        public event DPCMSampleDelegate DPCMSampleColorChanged;
        public event DPCMSampleDelegate DPCMSampleDeleted;
        public event DPCMSamplePointDelegate DPCMSampleDraggedOutside;
        public event DPCMSamplePointDelegate DPCMSampleMapped;
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
            expandedSample = null;
            selectedArpeggio = null;
            SongSelected?.Invoke(selectedSong);
            RefreshButtons();
            ConditionalInvalidate();
        }

        private ButtonType GetButtonTypeForParam(ParamInfo param)
        {
            var widgetType = ButtonType.ParamSlider;

            if (param.IsList)
                widgetType = ButtonType.ParamList;
            else if (param.MaxValue == 1)
                widgetType = ButtonType.ParamCheckbox;

            return widgetType;
        }

        public void RefreshButtons(bool invalidate = true)
        {
            Debug.Assert(captureOperation != CaptureOperation.MoveSlider);

            buttons.Clear();
            var project = App.Project;

            if (theme == null || project == null)
                return;

            var projectText = string.IsNullOrEmpty(project.Author) ? $"{project.Name}" : $"{project.Name} ({project.Author})";

            buttons.Add(new Button(this) { type = ButtonType.ProjectSettings, text = projectText, font = ThemeBase.FontMediumBoldCenterEllipsis });
            buttons.Add(new Button(this) { type = ButtonType.SongHeader, text = "Songs", font = ThemeBase.FontMediumBoldCenter });

            foreach (var song in project.Songs)
                buttons.Add(new Button(this) { type = ButtonType.Song, song = song, text = song.Name, color = song.Color, icon = bmpSong, textBrush = theme.BlackBrush });

            buttons.Add(new Button(this) { type = ButtonType.InstrumentHeader, text = "Instruments", font = ThemeBase.FontMediumBoldCenter });
            buttons.Add(new Button(this) { type = ButtonType.Instrument, color = ThemeBase.LightGreyFillColor1, textBrush = theme.BlackBrush, icon = bmpInstrument[ExpansionType.None] });

            foreach (var instrument in project.Instruments)
            {
                buttons.Add(new Button(this) { type = ButtonType.Instrument, instrument = instrument, text = instrument.Name, color = instrument.Color, textBrush = theme.BlackBrush, icon = bmpInstrument[instrument.ExpansionType] });

                if (instrument != null && instrument == expandedInstrument)
                {
                    var instrumentParams = InstrumentParamProvider.GetParams(instrument);

                    if (instrumentParams != null)
                    {
                        foreach (var param in instrumentParams)
                        {
                            buttons.Add(new Button(this) { type = GetButtonTypeForParam(param), param = param, instrument = instrument, color = instrument.Color, text = param.Name, textBrush = theme.BlackBrush, paramScope = TransactionScope.Instrument, paramObjectId = instrument.Id });
                        }
                    }
                }
            }

            buttons.Add(new Button(this) { type = ButtonType.DpcmHeader, font = ThemeBase.FontMediumBoldCenter });
            foreach (var sample in project.Samples)
            {
                buttons.Add(new Button(this) { type = ButtonType.Dpcm, sample = sample, color = sample.Color, textBrush = theme.BlackBrush, icon = bmpDPCM });

                if (sample == expandedSample)
                {
                    var sampleParams = DPCMSampleParamProvider.GetParams(sample);

                    foreach (var param in sampleParams)
                    {
                        buttons.Add(new Button(this) { type = GetButtonTypeForParam(param), param = param, sample = sample, color = sample.Color, text = param.Name, textBrush = theme.BlackBrush, paramScope = TransactionScope.DPCMSample, paramObjectId = sample.Id });
                    }
                }
            }

            buttons.Add(new Button(this) { type = ButtonType.ArpeggioHeader, text = "Arpeggios", font = ThemeBase.FontMediumBoldCenter });
            buttons.Add(new Button(this) { type = ButtonType.Arpeggio, text = "None", color = ThemeBase.LightGreyFillColor1, textBrush = theme.BlackBrush });

            foreach (var arpeggio in project.Arpeggios)
            {
                buttons.Add(new Button(this) { type = ButtonType.Arpeggio, arpeggio = arpeggio, text = arpeggio.Name, color = arpeggio.Color, textBrush = theme.BlackBrush, icon = bmpEnvelopes[EnvelopeType.Arpeggio] });
            }

            UpdateRenderCoords();

            if (invalidate)
                ConditionalInvalidate();
        }

        protected override void OnRenderInitialized(RenderGraphics g)
        {
            theme = RenderTheme.CreateResourcesForGraphics(g);

            bmpInstrument[ExpansionType.None] = g.CreateBitmapFromResource("Instrument");
            bmpInstrument[ExpansionType.Vrc6] = g.CreateBitmapFromResource("InstrumentKonami");
            bmpInstrument[ExpansionType.Vrc7] = g.CreateBitmapFromResource("InstrumentKonami");
            bmpInstrument[ExpansionType.Fds]  = g.CreateBitmapFromResource("InstrumentFds");
            bmpInstrument[ExpansionType.Mmc5] = g.CreateBitmapFromResource("Instrument");
            bmpInstrument[ExpansionType.N163] = g.CreateBitmapFromResource("InstrumentNamco");
            bmpInstrument[ExpansionType.S5B]  = g.CreateBitmapFromResource("InstrumentSunsoft");
            
            bmpEnvelopes[EnvelopeType.Volume]        = g.CreateBitmapFromResource("Volume");
            bmpEnvelopes[EnvelopeType.Arpeggio]      = g.CreateBitmapFromResource("Arpeggio");
            bmpEnvelopes[EnvelopeType.Pitch]         = g.CreateBitmapFromResource("Pitch");
            bmpEnvelopes[EnvelopeType.DutyCycle]     = g.CreateBitmapFromResource("Duty");
            bmpEnvelopes[EnvelopeType.FdsWaveform]   = g.CreateBitmapFromResource("Wave");
            bmpEnvelopes[EnvelopeType.FdsModulation] = g.CreateBitmapFromResource("Mod");
            bmpEnvelopes[EnvelopeType.N163Waveform]  = g.CreateBitmapFromResource("Wave");

            bmpExpand = g.CreateBitmapFromResource("InstrumentExpand");
            bmpExpanded = g.CreateBitmapFromResource("InstrumentExpanded");
            bmpOverflow = g.CreateBitmapFromResource("Warning");
            bmpCheckBoxYes = g.CreateBitmapFromResource("CheckBoxYes");
            bmpCheckBoxNo = g.CreateBitmapFromResource("CheckBoxNo");
            bmpButtonLeft = g.CreateBitmapFromResource("ButtonLeft");
            bmpButtonRight = g.CreateBitmapFromResource("ButtonRight");
            bmpSong = g.CreateBitmapFromResource("Music");
            bmpAdd = g.CreateBitmapFromResource("Add");
            bmpPlay = g.CreateBitmapFromResource("PlaySource");
            bmpDPCM = g.CreateBitmapFromResource("DPCMBlack");
            bmpLoad = g.CreateBitmapFromResource("InstrumentOpen");
            bmpWaveEdit = g.CreateBitmapFromResource("WaveEdit");
            bmpSave = g.CreateBitmapFromResource("SaveSmall");
            sliderFillBrush = g.CreateSolidBrush(Color.FromArgb(64, Color.Black));
            disabledBrush = g.CreateSolidBrush(Color.FromArgb(64, Color.Black));

            RefreshButtons();
        }

        protected override void OnRenderTerminated()
        {
            theme.Terminate();

            for (int i = 0; i < ExpansionType.Count; i++)
                Utils.DisposeAndNullify(ref bmpInstrument[i]);
            for (int i = 0; i < EnvelopeType.Count; i++)
                Utils.DisposeAndNullify(ref bmpEnvelopes[i]);

            Utils.DisposeAndNullify(ref bmpExpand);
            Utils.DisposeAndNullify(ref bmpExpanded);
            Utils.DisposeAndNullify(ref bmpOverflow);
            Utils.DisposeAndNullify(ref bmpCheckBoxYes);
            Utils.DisposeAndNullify(ref bmpCheckBoxNo);
            Utils.DisposeAndNullify(ref bmpButtonLeft);
            Utils.DisposeAndNullify(ref bmpButtonRight);
            Utils.DisposeAndNullify(ref bmpSong);
            Utils.DisposeAndNullify(ref bmpAdd);
            Utils.DisposeAndNullify(ref bmpPlay);
            Utils.DisposeAndNullify(ref bmpDPCM);
            Utils.DisposeAndNullify(ref bmpLoad);
            Utils.DisposeAndNullify(ref bmpWaveEdit);
            Utils.DisposeAndNullify(ref bmpSave);
            Utils.DisposeAndNullify(ref sliderFillBrush);
            Utils.DisposeAndNullify(ref disabledBrush);
        }

        public void ConditionalInvalidate()
        {
            if (!App.RealTimeUpdate || !App.RealTimeUpdateUpdatesProjectExplorer)
                Invalidate();
        }

        protected bool ShowExpandButtons()
        {
            if (App.Project != null)
            {
                if (App.Project.ExpansionAudio != ExpansionType.None && App.Project.Instruments.Find(i => InstrumentParamProvider.HasParams(i)) != null)
                    return true;

                if (App.Project.Samples.Count > 0)
                    return true;
            }

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
                var icon = button.icon;

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

                        g.FillAndDrawRectangle(0, 0, actualWidth, numParamButtons * buttonSizeY, g.GetVerticalGradientBrush(button.color, numParamButtons * buttonSizeY, 0.8f), theme.BlackBrush);
                        firstParam = false;
                    }
                }
                else
                {
                    g.FillAndDrawRectangle(0, 0, actualWidth, buttonSizeY, g.GetVerticalGradientBrush(button.color, buttonSizeY, 0.8f), theme.BlackBrush);
                }

                var leftPadding = 0;
                var leftAligned = button.type == ButtonType.Instrument || button.type == ButtonType.Song || button.type == ButtonType.ParamSlider || button.type == ButtonType.ParamCheckbox || button.type == ButtonType.ParamList || button.type == ButtonType.Arpeggio || button.type == ButtonType.Dpcm;

                if (showExpandButton && leftAligned)
                {
                    g.PushTranslation(1 + expandButtonSizeX, 0);
                    leftPadding = expandButtonSizeX;
                }

                var enabled = button.param == null || button.param.IsEnabled == null || button.param.IsEnabled();

                g.DrawText(button.Text, button.Font, icon == null ? buttonTextNoIconPosX : buttonTextPosX, buttonTextPosY, enabled ? button.textBrush : disabledBrush, actualWidth - buttonTextNoIconPosX * 2);

                if (icon != null)
                    g.DrawBitmap(icon, buttonIconPosX, buttonIconPosY);

                if (leftPadding != 0)
                    g.PopTransform();

                if (button.param != null)
                {
                    var paramVal = button.param.GetValue();
                    var paramStr = button.param.GetValueString();

                    if (button.type == ButtonType.ParamSlider)
                    {
                        var valSizeX = (int)Math.Round((paramVal - button.param.MinValue) / (float)(button.param.MaxValue - button.param.MinValue) * sliderSizeX);

                        g.PushTranslation(actualWidth - sliderPosX, sliderPosY);
                        g.FillRectangle(0, 0, valSizeX, sliderSizeY, sliderFillBrush);
                        g.DrawRectangle(0, 0, sliderSizeX, sliderSizeY, enabled ? theme.BlackBrush : disabledBrush);
                        g.DrawText(paramStr, ThemeBase.FontMediumCenter, 0, buttonTextPosY - sliderPosY, theme.BlackBrush, sliderSizeX);
                        g.PopTransform();
                    }
                    else if (button.type == ButtonType.ParamCheckbox)
                    {
                        g.DrawBitmap(paramVal == 0 ? bmpCheckBoxNo : bmpCheckBoxYes, actualWidth - checkBoxPosX, checkBoxPosY, enabled ? 1.0f : 0.25f);
                    }
                    else if (button.type == ButtonType.ParamList)
                    {
                        var paramPrev = button.param.SnapAndClampValue(paramVal - 1);
                        var paramNext = button.param.SnapAndClampValue(paramVal + 1);

                        g.PushTranslation(actualWidth - sliderPosX, sliderPosY);
                        g.DrawBitmap(bmpButtonLeft, 0, 0, paramVal == paramPrev || !enabled ? 0.25f : 1.0f);
                        g.DrawBitmap(bmpButtonRight, sliderSizeX - bmpButtonRight.Size.Width, 0, paramVal == paramNext || !enabled ? 0.25f : 1.0f);
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
                            if (subButtons[j] == SubButtonType.Expand)
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
            else if ((captureOperation == CaptureOperation.DragArpeggio || captureOperation == CaptureOperation.DragSample) && captureThresholdMet)
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
            sub = SubButtonType.Max;

            if (needsScrollBar && x >= Width - scrollBarSizeX)
                return -1;

            var buttonIndex = (y + scrollY) / buttonSizeY;

            if (buttonIndex >= 0 && buttonIndex < buttons.Count)
            {
                var button = buttons[buttonIndex];

                if (ShowExpandButtons() && x < (expandButtonPosX + expandButtonSizeX) && ((button.instrument != null && button.instrument.IsExpansionInstrument) || (button.sample != null))) 
                {
                    sub = SubButtonType.Expand;
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
            var redTooltip = false;
            var tooltip = "";
            var buttonIdx = GetButtonAtCoord(e.X, e.Y, out var subButtonType);

            // TODO: Store this in the button itself... this is stupid.
            if (buttonIdx >= 0)
            {
                var button = buttons[buttonIdx];
                var buttonType = button.type;

                if (buttonType == ButtonType.SongHeader)
                {
                    if (subButtonType == SubButtonType.Add)
                    {
                        tooltip = "{MouseLeft} Add new song";
                    }
                    else if (subButtonType == SubButtonType.Load)
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
                    else if (subButtonType == SubButtonType.Load)
                    {
                        tooltip = "{MouseLeft} Import/merge instrument from another project";
                    }
                }
                else if (buttonType == ButtonType.ArpeggioHeader)
                {
                    if (subButtonType == SubButtonType.Add)
                    {
                        tooltip = "{MouseLeft} Add new arpeggio";
                    }
                }
                else if (buttonType == ButtonType.ProjectSettings)
                {
                    tooltip = "{MouseLeft}{MouseLeft} Project properties";
                }
                else if (buttonType == ButtonType.ParamCheckbox)
                {
                    if (e.X >= Width - scrollBarSizeX - checkBoxPosX)
                    {
                        tooltip = "{MouseLeft} Toggle value\n{MouseRight} Reset to default value";
                    }
                    else if (button.param.ToolTip != null)
                    {
                        tooltip = button.param.ToolTip;
                    }
                }
                else if (buttonType == ButtonType.ParamSlider)
                {
                    if (e.X >= Width - scrollBarSizeX - sliderPosX)
                    {
                        tooltip = "{MouseLeft} {Drag} Change value - {Shift} {MouseLeft} {Drag} Change value (fine)\n{MouseRight} Reset to default value";
                    }
                    else if (button.param.ToolTip != null)
                    {
                        tooltip = button.param.ToolTip;
                    }
                }
                else if (buttonType == ButtonType.ParamList)
                {
                    if (e.X >= Width - scrollBarSizeX - sliderPosX)
                    {
                        tooltip = "{MouseLeft} Change value\n{MouseRight} Reset to default value";
                    }
                    else if (button.param.ToolTip != null)
                    {
                        tooltip = button.param.ToolTip;
                    }
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
                        {
                            tooltip = "{MouseLeft} Edit DPCM samples";
                        }
                        else if (subButtonType < SubButtonType.EnvelopeMax)
                        {
                            tooltip = $"{{MouseLeft}} Edit {EnvelopeType.Names[(int)subButtonType].ToLower()} envelope - {{MouseRight}} Delete envelope - {{MouseLeft}} {{Drag}} Copy envelope";
                        }
                        else if (subButtonType == SubButtonType.Overflow)
                        {
                            tooltip = "DPCM sample limit size limit is 16384 bytes. Some samples will not play correctly.";
                            redTooltip = true;
                        }
                    }
                }
                else if (buttonType == ButtonType.Dpcm)
                {
                    if (subButtonType == SubButtonType.Play)
                    {
                        tooltip = "{MouseLeft} Preview processed DPCM sample\n{MouseRight} Play source sample";
                    }
                    else if (subButtonType == SubButtonType.EditWave)
                    {
                        tooltip = "{MouseLeft} Edit waveform";
                    }
                    else if (subButtonType == SubButtonType.Save)
                    {
                        tooltip = "{MouseLeft} Export processed DMC file\n{MouseRight} Export source data (DMC or WAV)";
                    }
                    else if (subButtonType == SubButtonType.Max)
                    {
                        tooltip = "{MouseLeft}{MouseLeft} Edit properties\n{MouseRight} Delete sample";
                    }
                }
                else if (buttonType == ButtonType.DpcmHeader)
                {
                    if (subButtonType == SubButtonType.Load)
                    {
                        tooltip = "{MouseLeft} Load DPCM sample from WAV or DMC file";
                    }
                }
            }
            else if (needsScrollBar && e.X > Width - scrollBarSizeX)
            {
                tooltip = "{MouseLeft} {Drag} Scroll";
            }

            App.SetToolTip(tooltip, redTooltip);
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
                else if (captureOperation == CaptureOperation.ScrollBar)
                {
                    scrollY = captureScrollY + ((e.Y - captureMouseY) * virtualSizeY / Height);
                    ClampScroll();
                    ConditionalInvalidate();
                }
                else if (captureOperation == CaptureOperation.DragSample)
                {
                    if (!ClientRectangle.Contains(e.X, e.Y))
                    {
                        DPCMSampleDraggedOutside?.Invoke(draggedSample, PointToScreen(new Point(e.X, e.Y)));
                    }
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
                                    App.UndoRedoManager.BeginTransaction(TransactionScope.ProjectNoDPCMSamples);
                                    App.Project.ReplaceInstrument(instrumentDst, instrumentSrc);
                                    App.UndoRedoManager.EndTransaction();

                                    InstrumentReplaced?.Invoke(instrumentDst);
                                }
                            }
                        }
                        else
                        {
                            if (PlatformUtils.MessageBox($"Are you sure you want to copy the {EnvelopeType.Names[envelopeDragIdx]} envelope of instrument '{instrumentSrc.Name}' to '{instrumentDst.Name}'?", "Copy Envelope", MessageBoxButtons.YesNo) == DialogResult.Yes)
                            {
                                App.UndoRedoManager.BeginTransaction(TransactionScope.Instrument, instrumentDst.Id);
                                instrumentDst.Envelopes[envelopeDragIdx] = instrumentSrc.Envelopes[envelopeDragIdx].ShallowClone();
                                App.UndoRedoManager.EndTransaction();

                                InstrumentEdited?.Invoke(instrumentDst, envelopeDragIdx);
                                ConditionalInvalidate();
                            }
                        }
                    }
                }
                else
                {
                    InstrumentDroppedOutside(draggedInstrument, PointToScreen(new Point(e.X, e.Y)));
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
                                App.UndoRedoManager.BeginTransaction(TransactionScope.ProjectNoDPCMSamples);
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
                                ConditionalInvalidate();
                            }
                        }
                    }
                }
                else
                {
                    ArpeggioDroppedOutside?.Invoke(draggedArpeggio, PointToScreen(new Point(e.X, e.Y)));
                }
            }
            else if (captureOperation == CaptureOperation.DragSample)
            {
                if (!ClientRectangle.Contains(e.X, e.Y))
                {
                    var mappingNote = App.GetDPCMSampleMappingNoteAtPos(PointToScreen(new Point(e.X, e.Y)));
                    if (App.Project.NoteSupportsDPCM(mappingNote))
                    {
                        App.UndoRedoManager.BeginTransaction(TransactionScope.DPCMSamplesMapping, TransactionFlags.StopAudio);
                        App.Project.UnmapDPCMSample(mappingNote);
                        App.Project.MapDPCMSample(mappingNote, draggedSample);
                        App.UndoRedoManager.EndTransaction();

                        DPCMSampleMapped?.Invoke(draggedSample, PointToScreen(new Point(e.X, e.Y)));
                        ConditionalInvalidate();
                    }
                }
            }
            else if (captureOperation == CaptureOperation.MoveSlider)
            {
                App.UndoRedoManager.EndTransaction();
            }

            draggedArpeggio = null;
            draggedInstrument = null;
            draggedSample = null;
            sliderDragButton = null;
            captureOperation = CaptureOperation.None;
            Capture = false;
        }

        private void StartCaptureOperation(MouseEventArgs e, CaptureOperation op)
        {
            Debug.Assert(captureOperation == CaptureOperation.None);
            captureMouseX = e.X;
            captureMouseY = e.Y;
            captureScrollY = scrollY;
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

            var paramVal = button.param.GetValue();

            if (shift)
            {
                paramVal = Utils.Clamp(paramVal + (e.X - mouseLastX) * button.param.SnapValue, button.param.MinValue, button.param.MaxValue);
            }
            else
            {
                paramVal = (int)Math.Round(Utils.Lerp(button.param.MinValue, button.param.MaxValue, Utils.Clamp((buttonX - (actualWidth - sliderPosX)) / (float)sliderSizeX, 0.0f, 1.0f)));
            }

            paramVal = button.param.SnapAndClampValue(paramVal);
            button.param.SetValue(paramVal);

            App.Project.GetPackedSampleData();

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
                    dlg.Properties.AddCheckBoxList(null, songNames.ToArray(), null);
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

                                if (instrument.ExpansionType != ExpansionType.None)
                                    instName += $" ({ExpansionType.ShortNames[instrument.ExpansionType]})";

                                instrumentNames.Add(instName);
                            }

                            var dlg = new PropertyDialog(300);
                            dlg.Properties.AddLabel(null, "Select instruments to import:");
                            dlg.Properties.AddCheckBoxList(null, instrumentNames.ToArray(), null);
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
        }

        private void LoadDPCMSample()
        {
            var filename = PlatformUtils.ShowOpenFileDialog("Open File", "All Sample Files (*.wav;*.dmc)|*.wav;*.dmc|Wav Files (*.wav)|*.wav|DPCM Sample Files (*.dmc)|*.dmc", ref Settings.LastSampleFolder);

            if (filename != null)
            {
                var sampleName = Path.GetFileNameWithoutExtension(filename);
                if (sampleName.Length > 16)
                    sampleName = sampleName.Substring(0, 16);
                sampleName = App.Project.GenerateUniqueDPCMSampleName(sampleName);

                var dlgLog = new LogDialog(ParentForm);
                using (var scopedLog = new ScopedLogOutput(dlgLog, LogSeverity.Warning))
                {
                    if (Path.GetExtension(filename).ToLower() == ".wav")
                    {
                        var wavData = WaveFile.Load(filename, out var sampleRate);
                        if (wavData != null)
                        {
                            var maximumSamples = sampleRate * 2;
                            if (wavData.Length > maximumSamples)
                            {
                                Array.Resize(ref wavData, maximumSamples);
                                Log.LogMessage(LogSeverity.Warning, "The maximum supported length for a WAV file is 2.0 seconds. Truncating.");
                            }

                            App.UndoRedoManager.BeginTransaction(TransactionScope.DPCMSamples);
                            App.Project.CreateDPCMSampleFromWavData(sampleName, wavData, sampleRate);
                            App.UndoRedoManager.EndTransaction();
                        }
                    }
                    else if (Path.GetExtension(filename).ToLower() == ".dmc")
                    {
                        var dmcData = File.ReadAllBytes(filename);
                        if (dmcData.Length > DPCMSample.MaxSampleSize)
                        {
                            Array.Resize(ref dmcData, DPCMSample.MaxSampleSize);
                            Log.LogMessage(LogSeverity.Warning, $"The maximum supported size for a DMC is {DPCMSample.MaxSampleSize} bytes. Truncating.");
                        }
                        App.UndoRedoManager.BeginTransaction(TransactionScope.DPCMSamples);
                        App.Project.CreateDPCMSampleFromDmcData(sampleName, dmcData);
                        App.UndoRedoManager.EndTransaction();
                    }

                    RefreshButtons();

                    dlgLog.ShowDialogIfMessages();
                }
            }
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

                if (left && button.type == ButtonType.SongHeader)
                {
                    if (subButtonType == SubButtonType.Add)
                    {
                        App.UndoRedoManager.BeginTransaction(TransactionScope.ProjectNoDPCMSamples, TransactionFlags.StopAudio);
                        selectedSong = App.Project.CreateSong();
                        SongSelected?.Invoke(selectedSong);
                        App.UndoRedoManager.EndTransaction();
                        RefreshButtons();
                    }
                    else if (subButtonType == SubButtonType.Load)
                    {
                        ImportSong();
                    }
                }
                else if (left && button.type == ButtonType.Song)
                {
                    if (button.song != selectedSong)
                    {
                        selectedSong = button.song;
                        SongSelected?.Invoke(selectedSong);
                        ConditionalInvalidate();
                    }
                }
                else if (left && button.type == ButtonType.InstrumentHeader)
                {
                    if (subButtonType == SubButtonType.Add)
                    {
                        var instrumentType = ExpansionType.None;

                        if (App.Project.NeedsExpansionInstruments)
                        {
                            var expNames = new[] { ExpansionType.Names[ExpansionType.None], App.Project.ExpansionAudioName };
                            var dlg = new PropertyDialog(PointToScreen(new Point(e.X, e.Y)), 260, true);
                            dlg.Properties.AddDropDownList("Expansion:", expNames, ExpansionType.Names[ExpansionType.None] ); // 0
                            dlg.Properties.Build();

                            if (dlg.ShowDialog(ParentForm) == DialogResult.OK)
                                instrumentType = dlg.Properties.GetPropertyValue<string>(0) == ExpansionType.Names[ExpansionType.None] ? ExpansionType.None : App.Project.ExpansionAudio;
                            else
                                return;
                        }

                        App.UndoRedoManager.BeginTransaction(TransactionScope.ProjectNoDPCMSamples);
                        selectedInstrument = App.Project.CreateInstrument(instrumentType);
                        InstrumentSelected?.Invoke(selectedInstrument);
                        App.UndoRedoManager.EndTransaction();
                        RefreshButtons();
                    }
                    if (subButtonType == SubButtonType.Load)
                    {
                        ImportInstruments();
                    }
                }
                else if (left && button.type == ButtonType.Instrument)
                {
                    selectedInstrument = button.instrument;

                    if (selectedInstrument != null)
                    {
                        envelopeDragIdx = -1;
                        draggedInstrument = selectedInstrument;
                        StartCaptureOperation(e, CaptureOperation.DragInstrument);
                    }

                    if (subButtonType == SubButtonType.Expand)
                    {                         
                        expandedInstrument = expandedInstrument == selectedInstrument ? null : selectedInstrument;
                        expandedSample = null;
                        RefreshButtons(false);
                    }
                    else if (subButtonType == SubButtonType.DPCM)
                    {
                        InstrumentEdited?.Invoke(selectedInstrument, EnvelopeType.Count);
                    }
                    else if (subButtonType < SubButtonType.EnvelopeMax)
                    {
                        InstrumentEdited?.Invoke(selectedInstrument, (int)subButtonType);
                        envelopeDragIdx = (int)subButtonType;
                    }

                    InstrumentSelected?.Invoke(selectedInstrument);
                    ConditionalInvalidate();
                }
                else if ((left || right) && button.type == ButtonType.ParamSlider)
                {
                    App.UndoRedoManager.BeginTransaction(button.paramScope, button.paramObjectId);

                    if (left)
                    {
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
                    else
                    {
                        button.param.SetValue(button.param.DefaultValue);
                        App.UndoRedoManager.EndTransaction();
                        ConditionalInvalidate();
                    }
                }
                else if ((left || right) && button.type == ButtonType.ParamCheckbox)
                {
                    var actualWidth = Width - scrollBarSizeX;

                    if (e.X >= actualWidth - checkBoxPosX)
                    {
                        App.UndoRedoManager.BeginTransaction(button.paramScope, button.paramObjectId);

                        if (left)
                        {
                            var val = button.param.GetValue();
                            button.param.SetValue(val == 0 ? 1 : 0);
                        }
                        else
                        {
                            button.param.SetValue(button.param.DefaultValue);
                        }
                        App.UndoRedoManager.EndTransaction();
                        ConditionalInvalidate();
                    }
                }
                else if ((left || right) && button.type == ButtonType.ParamList)
                {
                    var actualWidth = Width - scrollBarSizeX;
                    var buttonX = e.X;
                    var leftButton  = buttonX > (actualWidth - sliderPosX) && buttonX < (actualWidth - sliderPosX + bmpButtonLeft.Size.Width);
                    var rightButton = buttonX > (actualWidth - sliderPosX + sliderSizeX - bmpButtonRight.Size.Width) && buttonX < (actualWidth - sliderPosX + sliderSizeX);
                    var delta = leftButton ? -1 : (rightButton ? 1 : 0);

                    if (left && (leftButton || rightButton))
                    {
                        App.UndoRedoManager.BeginTransaction(button.paramScope, button.paramObjectId);

                        var val = button.param.GetValue();

                        if (rightButton)
                            val = button.param.SnapAndClampValue(button.param.GetValue() + 1);
                        else
                            val = button.param.SnapAndClampValue(button.param.GetValue() - 1);

                        button.param.SetValue(val);

                        App.UndoRedoManager.EndTransaction();
                        ConditionalInvalidate();
                    }
                    else if (right && buttonX > (actualWidth - sliderPosX))
                    {
                        App.UndoRedoManager.BeginTransaction(button.paramScope, button.paramObjectId);
                        button.param.SetValue(button.param.DefaultValue);
                        App.UndoRedoManager.EndTransaction();
                        ConditionalInvalidate();
                    }
                }
                else if (left && button.type == ButtonType.ArpeggioHeader)
                {
                    if (subButtonType == SubButtonType.Add)
                    {
                        App.UndoRedoManager.BeginTransaction(TransactionScope.ProjectNoDPCMSamples);
                        selectedArpeggio = App.Project.CreateArpeggio();
                        ArpeggioSelected?.Invoke(selectedArpeggio);
                        App.UndoRedoManager.EndTransaction();
                        RefreshButtons();
                    }
                }
                else if (left && button.type == ButtonType.Arpeggio)
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
                else if (left && button.type == ButtonType.DpcmHeader)
                {
                    if (subButtonType == SubButtonType.Load)
                    {
                        LoadDPCMSample();
                    }
                }
                else if (left && button.type == ButtonType.Dpcm)
                {
                    if (subButtonType == SubButtonType.EditWave)
                    {
                        DPCMSampleEdited?.Invoke(button.sample);
                    }
                    else if (subButtonType == SubButtonType.Save)
                    {
                        var filename = PlatformUtils.ShowSaveFileDialog("Save File", "DPCM Samples (*.dmc)|*.dmc", ref Settings.LastSampleFolder);
                        if (filename != null)
                            File.WriteAllBytes(filename, button.sample.ProcessedData);
                    }
                    else if (subButtonType == SubButtonType.Play)
                    {
                        App.PreviewDPCMSample(button.sample, false);
                    }
                    else if (subButtonType == SubButtonType.Expand)
                    {
                        expandedSample = expandedSample == button.sample ? null : button.sample;
                        expandedInstrument = null;
                        RefreshButtons();
                    }
                    else if (subButtonType == SubButtonType.Max)
                    {
                        draggedSample = button.sample;
                        StartCaptureOperation(e, CaptureOperation.DragSample);
                        ConditionalInvalidate();
                    }
                }
                else if (right && button.type == ButtonType.Song && App.Project.Songs.Count > 1)
                {
                    var song = button.song;
                    if (PlatformUtils.MessageBox($"Are you sure you want to delete '{song.Name}' ?", "Delete song", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        bool selectNewSong = song == selectedSong;
                        App.UndoRedoManager.BeginTransaction(TransactionScope.ProjectNoDPCMSamples, TransactionFlags.StopAudio);
                        App.Project.DeleteSong(song);
                        if (selectNewSong)
                            selectedSong = App.Project.Songs[0];
                        SongSelected?.Invoke(selectedSong);
                        App.UndoRedoManager.EndTransaction();
                        RefreshButtons();
                    }
                }
                else if (right && button.type == ButtonType.Instrument && button.instrument != null)
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
                        if (PlatformUtils.MessageBox($"Are you sure you want to delete '{instrument.Name}' ? All notes using this instrument will be deleted.", "Delete instrument", MessageBoxButtons.YesNo) == DialogResult.Yes)
                        {
                            bool selectNewInstrument = instrument == selectedInstrument;
                            App.UndoRedoManager.BeginTransaction(TransactionScope.ProjectNoDPCMSamples, TransactionFlags.StopAudio);
                            App.Project.DeleteInstrument(instrument);
                            if (selectNewInstrument)
                                selectedInstrument = App.Project.Instruments.Count > 0 ? App.Project.Instruments[0] : null;
                            SongSelected?.Invoke(selectedSong);
                            InstrumentDeleted?.Invoke(instrument);
                            App.UndoRedoManager.EndTransaction();
                            RefreshButtons();
                        }
                    }
                }
                else if (right && button.type == ButtonType.Arpeggio && button.arpeggio != null)
                {
                    var arpeggio = button.arpeggio;

                    if (PlatformUtils.MessageBox($"Are you sure you want to delete '{arpeggio.Name}' ? All notes using this arpeggio will be no longer be arpeggiated.", "Delete arpeggio", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        bool selectNewArpeggio = arpeggio == selectedArpeggio;
                        App.UndoRedoManager.BeginTransaction(TransactionScope.ProjectNoDPCMSamples, TransactionFlags.StopAudio);
                        App.Project.DeleteArpeggio(arpeggio);
                        if (selectNewArpeggio)
                            selectedArpeggio = App.Project.Arpeggios.Count > 0 ? App.Project.Arpeggios[0] : null;
                        SongSelected?.Invoke(selectedSong);
                        ArpeggioDeleted?.Invoke(arpeggio);
                        App.UndoRedoManager.EndTransaction();
                        RefreshButtons();
                    }
                }
                else if (right && button.type == ButtonType.Dpcm)
                {
                    if (subButtonType == SubButtonType.Play)
                    {
                        App.PreviewDPCMSample(button.sample, true);
                    }
                    else if (subButtonType == SubButtonType.Save)
                    {
                        if (button.sample.SourceDataIsWav)
                        {
                            var filename = PlatformUtils.ShowSaveFileDialog("Save File", "Wav file (*.wav)|*.wav", ref Settings.LastSampleFolder);
                            if (filename != null)
                                WaveFile.Save(button.sample.SourceWavData.Samples, filename, button.sample.SourceWavData.SampleRate);
                        }
                        else
                        {
                            var filename = PlatformUtils.ShowSaveFileDialog("Save File", "DPCM Samples (*.dmc)|*.dmc", ref Settings.LastSampleFolder);
                            if (filename != null)
                                File.WriteAllBytes(filename, button.sample.SourceDmcData.Data);
                        }
                    }
                    else if (subButtonType == SubButtonType.Max)
                    {
                        if (PlatformUtils.MessageBox($"Are you sure you want to delete DPCM Sample '{button.sample.Name}' ? It will be removed from the DPCM Instrument and every note using it will be silent.", "Delete DPCM Sample", MessageBoxButtons.YesNo) == DialogResult.Yes)
                        {
                            App.UndoRedoManager.BeginTransaction(TransactionScope.DPCMSamples, TransactionFlags.StopAudio);
                            App.Project.DeleteSample(button.sample);
                            DPCMSampleDeleted?.Invoke(button.sample);
                            App.UndoRedoManager.EndTransaction();
                            RefreshButtons();
                        }
                    }
                }
            }
            else if (left && needsScrollBar && e.X > Width - scrollBarSizeX)
            {
                StartCaptureOperation(e, CaptureOperation.ScrollBar);
            }
        }

        private void EditProjectProperties(Point pt)
        {
            var project = App.Project;

            var dlg = new PropertyDialog(PointToScreen(pt), 320, true);
            dlg.Properties.AddString("Title :", project.Name, 31); // 0
            dlg.Properties.AddString("Author :", project.Author, 31); // 1
            dlg.Properties.AddString("Copyright :", project.Copyright, 31); // 2
            dlg.Properties.AddDropDownList("Expansion Audio :", ExpansionType.Names, project.ExpansionAudioName, CommonTooltips.ExpansionAudio); // 3
            dlg.Properties.AddIntegerRange("N163 Channels :", project.ExpansionNumChannels, 1, 8, CommonTooltips.ExpansionNumChannels); // 4 (Namco)
            dlg.Properties.AddDropDownList("Tempo Mode :", TempoType.Names, TempoType.Names[project.TempoMode], CommonTooltips.TempoMode); // 5
            dlg.Properties.AddDropDownList("Authoring Machine :", MachineType.NamesNoDual, MachineType.NamesNoDual[project.PalMode ? MachineType.PAL : MachineType.NTSC], CommonTooltips.AuthoringMachine); // 6
            dlg.Properties.SetPropertyEnabled(4, project.ExpansionAudio == ExpansionType.N163);
            dlg.Properties.SetPropertyEnabled(6, project.UsesFamiStudioTempo && !project.UsesExpansionAudio);
            dlg.Properties.PropertyChanged += ProjectProperties_PropertyChanged;
            dlg.Properties.Build();

            if (dlg.ShowDialog(ParentForm) == DialogResult.OK)
            {
                project.Name = dlg.Properties.GetPropertyValue<string>(0);
                project.Author = dlg.Properties.GetPropertyValue<string>(1);
                project.Copyright = dlg.Properties.GetPropertyValue<string>(2);

                var tempoMode    = TempoType.GetValueForName    (dlg.Properties.GetPropertyValue<string>(5));
                var expansion    = ExpansionType.GetValueForName(dlg.Properties.GetPropertyValue<string>(3));
                var palAuthoring = MachineType.GetValueForName  (dlg.Properties.GetPropertyValue<string>(6)) == 1;
                var numChannels  = dlg.Properties.GetPropertyValue<int>(4);

                var changedTempoMode        = tempoMode    != project.TempoMode;
                var changedExpansion        = expansion    != project.ExpansionAudio;
                var changedNumChannels      = numChannels  != project.ExpansionNumChannels;
                var changedAuthoringMachine = palAuthoring != project.PalMode;

                var transFlags = TransactionFlags.None;

                if (changedAuthoringMachine || changedExpansion || changedNumChannels)
                    transFlags = TransactionFlags.ReinitializeAudio;
                else if (changedTempoMode)
                    transFlags = TransactionFlags.StopAudio;

                App.UndoRedoManager.BeginTransaction(TransactionScope.ProjectNoDPCMSamples, transFlags);

                if (changedExpansion || changedNumChannels)
                {
                    if (project.ExpansionAudio == ExpansionType.None ||
                        (!changedExpansion && changedNumChannels) ||
                        PlatformUtils.MessageBox($"Switching expansion audio will delete all instruments and channels using the old expansion?", "Change expansion audio", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        selectedInstrument = project.Instruments.Count > 0 ? project.Instruments[0] : null;
                        project.SetExpansionAudio(expansion, numChannels);
                        ProjectModified?.Invoke();
                        Reset();
                    }
                }

                if (changedTempoMode)
                {
                    if (tempoMode == TempoType.FamiStudio)
                    {
                        if (!project.AreSongsEmpty)
                            PlatformUtils.MessageBox($"Converting from FamiTracker to FamiStudio tempo is extremely crude right now. It will ignore all speed changes and assume a tempo of 150. It is very likely that the songs will need a lot of manual corrections after.", "Change tempo mode", MessageBoxButtons.OK);
                        project.ConvertToFamiStudioTempo();
                    }
                    else if (tempoMode == TempoType.FamiTracker)
                    {
                        if (!project.AreSongsEmpty)
                            PlatformUtils.MessageBox($"Converting from FamiStudio to FamiTracker tempo will simply set the speed to 1 and tempo to 150. It will not try to merge notes or do anything sophisticated.", "Change tempo mode", MessageBoxButtons.OK);
                        project.ConvertToFamiTrackerTempo(project.AreSongsEmpty);
                    }

                    ProjectModified?.Invoke();
                    Reset();
                }

                if (changedAuthoringMachine && project.UsesFamiStudioTempo)
                {
                    project.PalMode = palAuthoring;
                    App.PalPlayback = palAuthoring;
                }

                App.UndoRedoManager.EndTransaction();
                RefreshButtons();
            }
        }

        private void ProjectProperties_PropertyChanged(PropertyPage props, int idx, object value)
        {
            var noExpansion = props.GetPropertyValue<string>(3) == ExpansionType.Names[ExpansionType.None];

            if (idx == 3) // Expansion
            {
                props.SetPropertyEnabled(4, (string)value == ExpansionType.Names[ExpansionType.N163]);
                props.SetPropertyEnabled(6, props.GetPropertyValue<string>(5) == "FamiStudio" && noExpansion);

                if (noExpansion)
                    props.SetDropDownListIndex(6, App.Project.PalMode ? 1 : 0);
                else
                    props.SetDropDownListIndex(6, 0);
            }
            else if (idx == 5) // Tempo Mode
            {
                props.SetPropertyEnabled(6, (string)value == TempoType.Names[TempoType.FamiStudio]);
            }
        }

        private void EditSongProperties(Point pt, Song song)
        {
            var dlg = new PropertyDialog(PointToScreen(pt), 240, true);

            dlg.Properties.UserData = song;
            dlg.Properties.AddColoredString(song.Name, song.Color); // 0
            dlg.Properties.AddColorPicker(song.Color); // 1
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
                App.UndoRedoManager.BeginTransaction(TransactionScope.ProjectNoDPCMSamples, TransactionFlags.StopAudio);
                App.SeekSong(0);

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
                    RefreshButtons(false);
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
            var dlg = new PropertyDialog(PointToScreen(pt), 240, true, pt.Y > Height / 2);
            dlg.Properties.AddColoredString(instrument.Name, instrument.Color); // 0
            dlg.Properties.AddColorPicker(instrument.Color); // 1
            if (instrument.IsEnvelopeActive(EnvelopeType.Pitch))
                dlg.Properties.AddCheckBox("Relative pitch:", instrument.Envelopes[EnvelopeType.Pitch].Relative); // 2
            dlg.Properties.Build();

            if (dlg.ShowDialog(ParentForm) == DialogResult.OK)
            {
                var newName = dlg.Properties.GetPropertyValue<string>(0);

                App.UndoRedoManager.BeginTransaction(TransactionScope.ProjectNoDPCMSamples);

                if (App.Project.RenameInstrument(instrument, newName))
                {
                    instrument.Color = dlg.Properties.GetPropertyValue<System.Drawing.Color>(1);
                    if (instrument.IsEnvelopeActive(EnvelopeType.Pitch))
                    {
                        var newRelative = dlg.Properties.GetPropertyValue<bool>(2);
                        if (instrument.Envelopes[EnvelopeType.Pitch].Relative != newRelative)
                        {
                            if (!instrument.Envelopes[EnvelopeType.Pitch].IsEmpty(EnvelopeType.Pitch))
                            {
                                if (newRelative)
                                {
                                    if (PlatformUtils.MessageBox("Do you want to try to convert the pitch envelope from absolute to relative?", "Pitch Envelope", MessageBoxButtons.YesNo) == DialogResult.Yes)
                                        instrument.Envelopes[EnvelopeType.Pitch].ConvertToRelative();
                                }
                                else
                                {
                                    if (PlatformUtils.MessageBox("Do you want to try to convert the pitch envelope from relative to absolute?", "Pitch Envelope", MessageBoxButtons.YesNo) == DialogResult.Yes)
                                        instrument.Envelopes[EnvelopeType.Pitch].ConvertToAbsolute();
                                }
                            }

                            instrument.Envelopes[EnvelopeType.Pitch].Relative = newRelative;
                        }
                    }
                    InstrumentColorChanged?.Invoke(instrument);
                    RefreshButtons();
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
            var dlg = new PropertyDialog(PointToScreen(pt), 240, true, pt.Y > Height / 2);
            dlg.Properties.AddColoredString(arpeggio.Name, arpeggio.Color); // 0
            dlg.Properties.AddColorPicker(arpeggio.Color); // 1
            dlg.Properties.Build();

            if (dlg.ShowDialog(ParentForm) == DialogResult.OK)
            {
                var newName = dlg.Properties.GetPropertyValue<string>(0);

                App.UndoRedoManager.BeginTransaction(TransactionScope.ProjectNoDPCMSamples);

                if (App.Project.RenameArpeggio(arpeggio, newName))
                {
                    arpeggio.Color = dlg.Properties.GetPropertyValue<System.Drawing.Color>(1);
                    ArpeggioColorChanged?.Invoke(arpeggio);
                    RefreshButtons();
                    App.UndoRedoManager.EndTransaction();
                }
                else
                {
                    App.UndoRedoManager.AbortTransaction();
                    SystemSounds.Beep.Play();
                }
            }
        }

        private void EditDPCMSampleProperties(Point pt, DPCMSample sample)
        {
            var dlg = new PropertyDialog(PointToScreen(pt), 240, true, pt.Y > Height / 2);
            dlg.Properties.AddColoredString(sample.Name, sample.Color); // 0
            dlg.Properties.AddColorPicker(sample.Color); // 1
            dlg.Properties.Build();

            if (dlg.ShowDialog(ParentForm) == DialogResult.OK)
            {
                var newName = dlg.Properties.GetPropertyValue<string>(0);

                App.UndoRedoManager.BeginTransaction(TransactionScope.DPCMSample, sample.Id);

                if (App.Project.RenameSample(sample, newName))
                {
                    sample.Color = dlg.Properties.GetPropertyValue<System.Drawing.Color>(1);
                    DPCMSampleColorChanged?.Invoke(sample);
                    RefreshButtons();
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
                else if (button.type == ButtonType.Dpcm && subButtonType == SubButtonType.Max)
                {
                    EditDPCMSampleProperties(pt, button.sample);
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
            buffer.Serialize(ref selectedArpeggio);
            buffer.Serialize(ref expandedInstrument);
            buffer.Serialize(ref expandedSample);
            buffer.Serialize(ref scrollY);

            if (buffer.IsReading)
            {
                captureOperation = CaptureOperation.None;
                Capture = false;

                ClampScroll();
                RefreshButtons();
            }
        }
    }
}
