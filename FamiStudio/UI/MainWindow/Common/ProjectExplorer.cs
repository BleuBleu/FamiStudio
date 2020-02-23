using System;
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
            ParamCheckbox,
            ParamSlider,
            ParamList,
            Max
        };

        enum ParamType
        {
            FdsWavePreset,
            FdsModulationPreset,
            FdsModulationSpeed,
            FdsModulationDepth,

            Vrc7CarTremolo,
            Vrc7CarVibrato,
            Vrc7CarSustained,
            Vrc7CarWaveRectified,
            Vrc7CarKeyScaling,
            Vrc7CarFreqMultiplier,
            Vrc7CarAttack,
            Vrc7CarDecay,
            Vrc7CarSustain,
            Vrc7CarRelease,

            Vrc7ModTremolo,
            Vrc7ModVibrato,
            Vrc7ModSustained,
            Vrc7ModWaveRectified,
            Vrc7ModKeyScaling,
            Vrc7ModFreqMultiplier,
            Vrc7ModAttack,
            Vrc7ModDecay,
            Vrc7ModSustain,
            Vrc7ModRelease,

            Max
        };

        static readonly string[] ParamNames = 
        {
            // FDS
            "Wave Preset",
            "Mod Preset",
            "Mod Speed",
            "Mod Depth",

            // VRC7
            "Carrier Tremolo",
            "Carrier Vibrato",
            "Carrier Sustained",
            "Carrier WaveShape",
            "Carrier KeyScaling",
            "Carrier FreqMultiplier",
            "Carrier Attack",
            "Carrier Decay",
            "Carrier Sustain",
            "Carrier Release",

            "Modulator Tremolo",
            "Modulator Vibrato",
            "Modulator Sustained",
            "Modulator WaveShape",
            "Modulator KeyScaling",
            "Modulator FreqMultiplier",
            "Modulator Attack",
            "Modulator Decay",
            "Modulator Sustain",
            "Modulator Release"
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
            NamcoWaveformEnvelope = Envelope.NamcoWaveform,
            EnvelopeMax = Envelope.Max,

            // Other buttons
            Add,
            DPCM,
            LoadInstrument,
            ExpandInstrument,
            Max
        }

        // From right to left. Looks more visually pleasing than the enum order.
        static readonly int[] EnvelopeDisplayOrder = new int[]
        {
            Envelope.Arpeggio,
            Envelope.Pitch,
            Envelope.Volume,
            Envelope.DutyCycle,
            Envelope.FdsModulation,
            Envelope.FdsWaveform,
            Envelope.NamcoWaveform
        };

        class Button
        {
            public ButtonType type;
            public ParamType param = ParamType.Max;
            public Song song;
            public Instrument instrument;
            public ProjectExplorer projectExplorer;

            public Button(ProjectExplorer pe)
            {
                projectExplorer = pe;
            }

            public SubButtonType[] GetSubButtons(out bool[] active)
            {
                switch (type)
                {
                    case ButtonType.SongHeader:
                        active = new[] { true };
                        return new[] { SubButtonType.Add };
                    case ButtonType.InstrumentHeader:
                        active = new[] { true ,true };
                        return new[] { SubButtonType.Add ,
                                       SubButtonType.LoadInstrument };
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

                            for (int i = 0, j = 0; i < Envelope.Max; i++)
                            {
                                int idx = EnvelopeDisplayOrder[i];
                                if (instrument.Envelopes[idx] != null)
                                {
                                    buttons[j] = (SubButtonType)idx;
                                    active[j] = !instrument.Envelopes[idx].IsEmpty; 
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
                }

                active = null;
                return null;
            }

            public string GetText(Project project)
            {
                switch (type)
                {
                    case ButtonType.ProjectSettings: return $"{project.Name} ({project.Author})";
                    case ButtonType.SongHeader: return "Songs";
                    case ButtonType.Song: return song.Name;
                    case ButtonType.InstrumentHeader: return "Instruments";
                    case ButtonType.Instrument: return instrument == null ? "DPCM Samples" : instrument.Name;
                    case ButtonType.ParamCheckbox:
                    case ButtonType.ParamSlider:
                    case ButtonType.ParamList: return ParamNames[(int)param];
                }

                return "";
            }

            public Color GetColor()
            {
                switch (type)
                {
                    case ButtonType.SongHeader:
                    case ButtonType.InstrumentHeader: return ThemeBase.LightGreyFillColor2;
                    case ButtonType.Song: return song.Color;
                    case ButtonType.ParamCheckbox:
                    case ButtonType.ParamSlider:
                    case ButtonType.ParamList:
                    case ButtonType.Instrument: return instrument == null ? ThemeBase.LightGreyFillColor1 : instrument.Color;
                }

                return ThemeBase.LightGreyFillColor1;
            }

            public RenderFont GetFont(Song selectedSong, Instrument selectedInstrument)
            {
                if (type == ButtonType.ProjectSettings)
                {
                    return ThemeBase.FontMediumBoldCenterEllipsis;
                }
                if (type == ButtonType.SongHeader || type == ButtonType.InstrumentHeader)
                {
                    return ThemeBase.FontMediumBoldCenter;
                }
                else if ((type == ButtonType.Song && song == selectedSong) ||
                         (type == ButtonType.Instrument && instrument == selectedInstrument))
                {
                    return ThemeBase.FontMediumBold;
                }
                else
                {
                    return ThemeBase.FontMedium;
                }
            }

            public RenderBitmap GetIcon()
            {
                if (type == ButtonType.Song)
                {
                    return projectExplorer.bmpSong;
                }
                else if (type == ButtonType.ParamCheckbox || type == ButtonType.ParamSlider || type == ButtonType.ParamList)
                {
                    return projectExplorer.bmpParams[(int)param];
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
                    case SubButtonType.LoadInstrument:
                        return projectExplorer.bmpLoadInstrument;
                    case SubButtonType.ExpandInstrument:
                        return projectExplorer.expandedInstrument == instrument ? projectExplorer.bmpExpanded : projectExplorer.bmpExpand;
                }

                return projectExplorer.bmpEnvelopes[(int)sub];
            }

            int testVal = 1000; // MATTT

            public int GetParamMinValue()
            {
                Debug.Assert(param != ParamType.Max);
                return 0;
            }

            public int GetParamMaxValue()
            {
                Debug.Assert(param != ParamType.Max);

                switch (param)
                {
                    case ParamType.FdsModulationSpeed: return 4095;
                    case ParamType.FdsModulationDepth: return 255;
                    case ParamType.Vrc7CarFreqMultiplier:
                    case ParamType.Vrc7CarAttack:
                    case ParamType.Vrc7CarDecay:
                    case ParamType.Vrc7CarSustain:
                    case ParamType.Vrc7CarRelease:
                    case ParamType.Vrc7ModFreqMultiplier:
                    case ParamType.Vrc7ModAttack:
                    case ParamType.Vrc7ModDecay:
                    case ParamType.Vrc7ModSustain:
                    case ParamType.Vrc7ModRelease:
                        return 16;
                }

                return 0;
            }

            public int GetParamValue()
            {
                Debug.Assert(param != ParamType.Max);

                switch (param)
                {
                    case ParamType.FdsModulationSpeed: return testVal; // MATTT
                    case ParamType.FdsModulationDepth: return 20;
                    case ParamType.FdsWavePreset: return 100;
                    case ParamType.FdsModulationPreset: return 200;
                }

                return 0;
            }

            public string GetParamString()
            {
                Debug.Assert(param != ParamType.Max);

                switch (param)
                {
                    case ParamType.FdsWavePreset:       return "Sine";
                    case ParamType.FdsModulationPreset: return "Square";
                }

                return GetParamValue().ToString();
            }

            public void SetParamValue(int val)
            {
                Debug.Assert(param != ParamType.Max);

                testVal = val; // MATTT
            }
        }

        enum CaptureOperation
        {
            None,
            DragInstrument,
            MoveSlider
        };

        static readonly bool[] captureNeedsThreshold = new[]
        {
            false,
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
        RenderBitmap[] bmpEnvelopes = new RenderBitmap[Envelope.Max];
        RenderBitmap[] bmpParams = new RenderBitmap[(int)ParamType.Max];

        public Song SelectedSong => selectedSong;
        public Instrument SelectedInstrument => selectedInstrument;

        public delegate void EmptyDelegate();
        public delegate void InstrumentEnvelopeDelegate(Instrument instrument, int envelope);
        public delegate void InstrumentDelegate(Instrument instrument);
        public delegate void InstrumentPointDelegate(Instrument instrument, Point pos);
        public delegate void SongDelegate(Song song);

        public event InstrumentEnvelopeDelegate InstrumentEdited;
        public event InstrumentDelegate InstrumentSelected;
        public event InstrumentDelegate InstrumentColorChanged;
        public event InstrumentDelegate InstrumentReplaced;
        public event InstrumentDelegate InstrumentDeleted;
        public event InstrumentPointDelegate InstrumentDraggedOutside;
        public event SongDelegate SongModified;
        public event SongDelegate SongSelected;
        public event EmptyDelegate ExpansionAudioChanged;

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
            selectedSong = App.Project.Songs[0];
            selectedInstrument = App.Project.Instruments.Count > 0 ? App.Project.Instruments[0] : null;
            expandedInstrument = null;
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
                    if (instrument.ExpansionType == Project.ExpansionFds)
                    { 
                        buttons.Add(new Button(this) { type = ButtonType.ParamList,     param = ParamType.FdsWavePreset,       instrument = instrument });
                        buttons.Add(new Button(this) { type = ButtonType.ParamList,     param = ParamType.FdsModulationPreset, instrument = instrument });
                        buttons.Add(new Button(this) { type = ButtonType.ParamSlider,   param = ParamType.FdsModulationSpeed,  instrument = instrument });
                        buttons.Add(new Button(this) { type = ButtonType.ParamCheckbox, param = ParamType.FdsModulationDepth,  instrument = instrument });
                    }
                    else if (instrument.ExpansionType == Project.ExpansionVrc7)
                    {
                        buttons.Add(new Button(this) { type = ButtonType.ParamCheckbox, param = ParamType.Vrc7CarTremolo,        instrument = instrument });
                        buttons.Add(new Button(this) { type = ButtonType.ParamCheckbox, param = ParamType.Vrc7CarVibrato,        instrument = instrument });
                        buttons.Add(new Button(this) { type = ButtonType.ParamCheckbox, param = ParamType.Vrc7CarSustained,      instrument = instrument });
                        buttons.Add(new Button(this) { type = ButtonType.ParamCheckbox, param = ParamType.Vrc7CarWaveRectified,  instrument = instrument });
                        buttons.Add(new Button(this) { type = ButtonType.ParamList,     param = ParamType.Vrc7CarKeyScaling,     instrument = instrument });
                        buttons.Add(new Button(this) { type = ButtonType.ParamSlider,   param = ParamType.Vrc7CarFreqMultiplier, instrument = instrument });
                        buttons.Add(new Button(this) { type = ButtonType.ParamSlider,   param = ParamType.Vrc7CarAttack,         instrument = instrument });
                        buttons.Add(new Button(this) { type = ButtonType.ParamSlider,   param = ParamType.Vrc7CarDecay,          instrument = instrument });
                        buttons.Add(new Button(this) { type = ButtonType.ParamSlider,   param = ParamType.Vrc7CarSustain,        instrument = instrument });
                        buttons.Add(new Button(this) { type = ButtonType.ParamSlider,   param = ParamType.Vrc7CarRelease,        instrument = instrument });
                        buttons.Add(new Button(this) { type = ButtonType.ParamCheckbox, param = ParamType.Vrc7ModTremolo,        instrument = instrument });
                        buttons.Add(new Button(this) { type = ButtonType.ParamCheckbox, param = ParamType.Vrc7ModVibrato,        instrument = instrument });
                        buttons.Add(new Button(this) { type = ButtonType.ParamCheckbox, param = ParamType.Vrc7ModSustained,      instrument = instrument });
                        buttons.Add(new Button(this) { type = ButtonType.ParamCheckbox, param = ParamType.Vrc7ModWaveRectified,  instrument = instrument });
                        buttons.Add(new Button(this) { type = ButtonType.ParamList,     param = ParamType.Vrc7ModKeyScaling,     instrument = instrument });
                        buttons.Add(new Button(this) { type = ButtonType.ParamSlider,   param = ParamType.Vrc7ModFreqMultiplier, instrument = instrument });
                        buttons.Add(new Button(this) { type = ButtonType.ParamSlider,   param = ParamType.Vrc7ModAttack,         instrument = instrument });
                        buttons.Add(new Button(this) { type = ButtonType.ParamSlider,   param = ParamType.Vrc7ModDecay,          instrument = instrument });
                        buttons.Add(new Button(this) { type = ButtonType.ParamSlider,   param = ParamType.Vrc7ModSustain,        instrument = instrument });
                        buttons.Add(new Button(this) { type = ButtonType.ParamSlider,   param = ParamType.Vrc7ModRelease,        instrument = instrument });
                    }
                }
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
            bmpInstrument[Project.ExpansionNamco]   = g.CreateBitmapFromResource("InstrumentNamco");
            bmpInstrument[Project.ExpansionSunsoft] = g.CreateBitmapFromResource("InstrumentSunsoft");

            bmpEnvelopes[Envelope.Volume]        = g.CreateBitmapFromResource("Volume");
            bmpEnvelopes[Envelope.Arpeggio]      = g.CreateBitmapFromResource("Arpeggio");
            bmpEnvelopes[Envelope.Pitch]         = g.CreateBitmapFromResource("Pitch");
            bmpEnvelopes[Envelope.DutyCycle]     = g.CreateBitmapFromResource("Duty");
            bmpEnvelopes[Envelope.FdsWaveform]   = g.CreateBitmapFromResource("Wave");
            bmpEnvelopes[Envelope.FdsModulation] = g.CreateBitmapFromResource("Wave");
            bmpEnvelopes[Envelope.NamcoWaveform] = g.CreateBitmapFromResource("Wave");

            bmpParams[(int)ParamType.FdsWavePreset]       = g.CreateBitmapFromResource("Wave");
            bmpParams[(int)ParamType.FdsModulationPreset] = g.CreateBitmapFromResource("Wave");
            bmpParams[(int)ParamType.FdsModulationSpeed]  = g.CreateBitmapFromResource("Wave");
            bmpParams[(int)ParamType.FdsModulationDepth]  = g.CreateBitmapFromResource("Wave");

            bmpExpand = g.CreateBitmapFromResource("InstrumentExpand");
            bmpExpanded = g.CreateBitmapFromResource("InstrumentExpanded");
            bmpCheckBoxYes = g.CreateBitmapFromResource("CheckBoxYes");
            bmpCheckBoxNo = g.CreateBitmapFromResource("CheckBoxNo");
            bmpButtonLeft = g.CreateBitmapFromResource("ButtonLeft");
            bmpButtonRight = g.CreateBitmapFromResource("ButtonRight");
            bmpSong = g.CreateBitmapFromResource("Music");
            bmpAdd = g.CreateBitmapFromResource("Add");
            bmpDPCM = g.CreateBitmapFromResource("DPCM");
            bmpLoadInstrument = g.CreateBitmapFromResource("InstrumentOpen");
            sliderFillBrush = g.CreateSolidBrush(Color.FromArgb(64, Color.Black));
        }

        public void ConditionalInvalidate()
        {
            Invalidate();
        }

        protected bool ShowExpandButtons()
        {
            if (App.Project.ExpansionAudio == Project.ExpansionFds  || // Modulation settings
                App.Project.ExpansionAudio == Project.ExpansionVrc7 || // A lot of stuff
                App.Project.ExpansionAudio == Project.ExpansionNamco)  // Wave size
            {
                return App.Project.Instruments.Find(i => i.ExpansionType != Project.ExpansionNone) != null;
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

                g.DrawText(button.GetText(App.Project), button.GetFont(selectedSong, selectedInstrument), icon == null ? buttonTextNoIconPosX : buttonTextPosX, buttonTextPosY, theme.BlackBrush, actualWidth - buttonTextNoIconPosX * 2);

                if (icon != null)
                    g.DrawBitmap(icon, buttonIconPosX, buttonIconPosY);

                if (leftPadding != 0)
                    g.PopTransform();

                if (button.param != ParamType.Max)
                {
                    var paramVal = button.GetParamValue();
                    var paramStr = button.GetParamString();

                    if (button.type == ButtonType.ParamSlider)
                    {
                        var paramMin = button.GetParamMinValue();
                        var paramMax = button.GetParamMaxValue();
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
                        g.PushTranslation(actualWidth - sliderPosX, sliderPosY);
                        g.DrawBitmap(bmpButtonLeft,  0, 0);
                        g.DrawBitmap(bmpButtonRight, sliderSizeX - bmpButtonRight.Size.Width, 0);
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
#if !FAMISTUDIO_LINUX
                // TODO LINUX: Cursors
                Cursor.Current = envelopeDragIdx == -1 ? Cursors.DragCursor : Cursors.CopyCursor;
#endif
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

                if (buttonType == ButtonType.SongHeader && subButtonType == SubButtonType.Add)
                {
                    tooltip = "{MouseLeft} Add new song";
                }
                else if (buttonType == ButtonType.Song)
                {
                    tooltip = "{MouseLeft} Make song current - {MouseLeft}{MouseLeft} Song properties - {MouseRight} Delete song";
                }
                else if (buttonType == ButtonType.InstrumentHeader && subButtonType == SubButtonType.Add)
                {
                    tooltip = "{MouseLeft} Add new instrument";
                }
                else if (buttonType == ButtonType.ProjectSettings)
                {
                    tooltip = "{MouseLeft}{MouseLeft} Project properties";
                }
                else if (buttonType == ButtonType.Instrument)
                {
                    if (subButtonType == SubButtonType.Max)
                    {
                        if (buttons[buttonIdx].instrument == null)
                            tooltip = "{MouseLeft} Select instrument";
                        else
                            tooltip = "{MouseLeft} Select instrument - {MouseLeft}{MouseLeft} Instrument properties - {MouseRight} Delete instrument - {Drag} Replace instrument";
                    }
                    else
                    {
                        if (subButtonType == SubButtonType.DPCM)
                            tooltip = "{MouseLeft} Edit DPCM samples";
                        else if (subButtonType < SubButtonType.EnvelopeMax)
                            tooltip = $"{{MouseLeft}} Edit {Envelope.EnvelopeNames[(int)subButtonType].ToLower()} envelope - {{MouseRight}} Delete envelope - {{Drag}} Copy envelope";
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
                    UpdateSliderValue(sliderDragButton, e);
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

                    if (instrumentSrc != instrumentDst && instrumentSrc != null && instrumentDst != null && instrumentSrc.ExpansionType == instrumentDst.ExpansionType)
                    {
                        if (envelopeDragIdx == -1)
                        {
                            if (PlatformUtils.MessageBox($"Are you sure you want to replace all notes of instrument '{instrumentDst.Name}' with '{instrumentSrc.Name}'?", "Replace intrument", MessageBoxButtons.YesNo) == DialogResult.Yes)
                            {
                                App.UndoRedoManager.BeginTransaction(TransactionScope.Project);
                                App.Project.ReplaceInstrument(instrumentDst, instrumentSrc);
                                App.UndoRedoManager.EndTransaction();

                                InstrumentReplaced?.Invoke(instrumentDst);
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

        bool UpdateSliderValue(Button button, MouseEventArgs e)
        {
            var buttonIdx = buttons.IndexOf(button);
            Debug.Assert(buttonIdx >= 0);

            bool shift = ModifierKeys.HasFlag(Keys.Shift);

            var actualWidth = Width - scrollBarSizeX;
            var buttonX = e.X;
            var buttonY = e.Y - buttonIdx * buttonSizeY;

            var paramVal = button.GetParamValue();
            var paramMin = button.GetParamMinValue();
            var paramMax = button.GetParamMaxValue();

            if (shift)
            {
                paramVal = Utils.Clamp(paramVal + (e.X - mouseLastX), paramMin, paramMax);
            }
            else
            {
                paramVal = (int)Math.Round(Utils.Lerp(paramMin, paramMax, Utils.Clamp((buttonX - (actualWidth - sliderPosX)) / (float)sliderSizeX, 0.0f, 1.0f)));
            }

            button.SetParamValue(paramVal);

            return (buttonX > (actualWidth - sliderPosX) &&
                    buttonX < (actualWidth - sliderPosX + sliderSizeX) &&
                    buttonY > (sliderPosY) &&
                    buttonY < (sliderPosY + sliderSizeY));
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (captureOperation != CaptureOperation.None)
                return;

            bool left   = e.Button.HasFlag(MouseButtons.Left);
            bool middle = e.Button.HasFlag(MouseButtons.Middle) || (e.Button.HasFlag(MouseButtons.Left) && ModifierKeys.HasFlag(Keys.Alt));
            bool right  = e.Button.HasFlag(MouseButtons.Right);

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
                            App.UndoRedoManager.BeginTransaction(TransactionScope.Project);
                            App.Project.CreateSong();
                            App.UndoRedoManager.EndTransaction();
                            RefreshButtons();
                            ConditionalInvalidate();
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
                                var dlg = new PropertyDialog(PointToScreen(new Point(e.X, e.Y)), 240, true);
                                dlg.Properties.AddStringList("Expansion:", expNames, Project.ExpansionNames[Project.ExpansionNone] ); // 0
                                dlg.Properties.Build();

                                if (dlg.ShowDialog() == DialogResult.OK)
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
                            var filename = PlatformUtils.ShowOpenFileDialog("Open File", "Fami Tracker Instrument (*.fti)|*.fti");
                            if (filename != null)
                            {
                                App.UndoRedoManager.BeginTransaction(TransactionScope.Project);
                                var instrument = FamitrackerInstrumentFile.CreateFromFile(App.Project, filename);
                                if (instrument == null)
                                    App.UndoRedoManager.AbortTransaction();
                                else
                                    App.UndoRedoManager.EndTransaction();
                            }

                            RefreshButtons();
                            ConditionalInvalidate();
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
                            InstrumentEdited?.Invoke(selectedInstrument, Envelope.Max);
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
                        if (UpdateSliderValue(button, e))
                        {
                            // MATTT: Transaction here.
                            sliderDragButton = button;
                            StartCaptureOperation(e, CaptureOperation.MoveSlider);
                            ConditionalInvalidate();
                        }
                    }
                    else if (button.type == ButtonType.ParamCheckbox)
                    {

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
                            App.UndoRedoManager.BeginTransaction(TransactionScope.Project);
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

                        if (subButtonType == SubButtonType.ArpeggioEnvelope ||
                            subButtonType == SubButtonType.PitchEnvelope ||
                            subButtonType == SubButtonType.VolumeEnvelope)
                        {
                            int envType = Envelope.Volume;

                            switch (subButtonType)
                            {
                                case SubButtonType.ArpeggioEnvelope: envType = Envelope.Arpeggio; break;
                                case SubButtonType.PitchEnvelope: envType = Envelope.Pitch;    break;
                            }

                            App.UndoRedoManager.BeginTransaction(TransactionScope.Instrument, instrument.Id);
                            instrument.Envelopes[envType].Length = 0;
                            App.UndoRedoManager.EndTransaction();
                            ConditionalInvalidate();
                        }
                        else if (subButtonType == SubButtonType.Max)
                        {
                            if (PlatformUtils.MessageBox($"Are you sure you want to delete '{instrument.Name}' ? All notes using this instrument will be deleted.", "Delete intrument", MessageBoxButtons.YesNo) == DialogResult.Yes)
                            {
                                bool selectNewInstrument = instrument == selectedInstrument;
                                App.StopInstrumentNoteAndWait();
                                App.UndoRedoManager.BeginTransaction(TransactionScope.Project);
                                App.Project.DeleteInstrument(instrument);
                                if (selectNewInstrument)
                                    selectedInstrument = App.Project.Instruments.Count > 0 ? App.Project.Instruments[0] : null;
                                SongSelected?.Invoke(selectedSong);
                                InstrumentDeleted?.Invoke(instrument);
                                App.UndoRedoManager.EndTransaction();
                                RefreshButtons();
                                ConditionalInvalidate();
                            }
                        }
                    }
                }
            }

            if (middle)
            {
                mouseLastY = e.Y;
            }
        }

        private int[] GenerateBarLengths(int patternLen)
        {
            var barLengths = Song.GenerateBarLengths(patternLen);

#if !FAMISTUDIO_WINDOWS
            Array.Reverse(barLengths);
#endif

            return barLengths;
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

                if (button.type == ButtonType.ProjectSettings)
                {
                    var project = App.Project;

                    var dlg = new PropertyDialog(PointToScreen(new Point(e.X, e.Y)), 300, true);
                    dlg.Properties.AddString("Title :", project.Name, 31); // 0
                    dlg.Properties.AddString("Author :", project.Author, 31); // 1
                    dlg.Properties.AddString("Copyright :", project.Copyright, 31); // 2
                    dlg.Properties.AddStringList("Expansion Audio:", Project.GetAllowedExpansionNames(), project.ExpansionAudioName); // 3
                    dlg.Properties.Build();

                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        App.UndoRedoManager.BeginTransaction(TransactionScope.Project);

                        project.Name = dlg.Properties.GetPropertyValue<string>(0);
                        project.Author = dlg.Properties.GetPropertyValue<string>(1);
                        project.Copyright = dlg.Properties.GetPropertyValue<string>(2);

                        var expansion = Array.IndexOf(Project.ExpansionNames, dlg.Properties.GetPropertyValue<string>(3));
                        if (expansion != project.ExpansionAudio)
                        {
                            if (project.ExpansionAudio == Project.ExpansionNone ||
                                PlatformUtils.MessageBox($"Switching expansion audio will delete all instruments and channels using the old expansion?", "Change expansion audio", MessageBoxButtons.YesNo) == DialogResult.Yes)
                            {
                                App.StopInstrumentPlayer();
                                project.SetExpansionAudio(expansion);
                                ExpansionAudioChanged?.Invoke();
                                App.StartInstrumentPlayer();
                                Reset();
                            }
                        }

                        App.UndoRedoManager.EndTransaction();
                        ConditionalInvalidate();
                    }
                }
                else if (button.type == ButtonType.Song)
                {
                    var song = button.song;

                    var dlg = new PropertyDialog(PointToScreen(new Point(e.X, e.Y)), 200, true);
                    dlg.Properties.AddColoredString(song.Name, song.Color); // 0
                    dlg.Properties.AddIntegerRange("Tempo :", song.Tempo, 32, 255); // 1
                    dlg.Properties.AddIntegerRange("Speed :", song.Speed, 1, 31); // 2
                    dlg.Properties.AddIntegerRange("Pattern Length :", song.DefaultPatternLength, 16, 256); // 3
                    dlg.Properties.AddDomainRange("Bar Length :", GenerateBarLengths(song.DefaultPatternLength), song.BarLength); // 4
                    dlg.Properties.AddIntegerRange("Song Length :", song.Length, 1, 128); // 5
                    dlg.Properties.AddColor(song.Color); // 6
                    dlg.Properties.Build();
                    dlg.Properties.PropertyChanged += Properties_PropertyChanged;

                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        App.UndoRedoManager.BeginTransaction(TransactionScope.Project);

                        App.Stop();
                        App.Seek(0);

                        var newName = dlg.Properties.GetPropertyValue<string>(0);

                        if (App.Project.RenameSong(song, newName))
                        {
                            song.Color = dlg.Properties.GetPropertyValue<System.Drawing.Color>(6);
                            song.Tempo = dlg.Properties.GetPropertyValue<int>(1);
                            song.Speed = dlg.Properties.GetPropertyValue<int>(2);
                            song.SetLength(dlg.Properties.GetPropertyValue<int>(5));
                            song.SetDefaultPatternLength(dlg.Properties.GetPropertyValue<int>(3));
                            song.SetBarLength(dlg.Properties.GetPropertyValue<int>(4));
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
                else if (button.type == ButtonType.Instrument && button.instrument != null)
                {
                    var instrument = button.instrument;

                    if (subButtonType == SubButtonType.Max)
                    {
                        var dlg = new PropertyDialog(PointToScreen(new Point(e.X, e.Y)), 160, true, e.Y > Height / 2);
                        dlg.Properties.AddColoredString(instrument.Name, instrument.Color); // 0
                        dlg.Properties.AddColor(instrument.Color); // 1
                        if (instrument.IsEnvelopeActive(Envelope.Pitch))
                            dlg.Properties.AddBoolean("Relative pitch:", instrument.Envelopes[Envelope.Pitch].Relative); // 2
                        dlg.Properties.Build();

                        if (dlg.ShowDialog() == DialogResult.OK)
                        {
                            var newName  = dlg.Properties.GetPropertyValue<string>(0);

                            App.UndoRedoManager.BeginTransaction(TransactionScope.Project);

                            if (App.Project.RenameInstrument(instrument, newName))
                            {
                                instrument.Color = dlg.Properties.GetPropertyValue<System.Drawing.Color>(1);
                                if (instrument.IsEnvelopeActive(Envelope.Pitch))
                                    instrument.Envelopes[Envelope.Pitch].Relative = dlg.Properties.GetPropertyValue<bool>(2);
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
                }
            }
        }

        private void Properties_PropertyChanged(PropertyPage props, int idx, object value)
        {
            if (idx == 3) // 3 = pattern length.
            {
                var barLengths = GenerateBarLengths((int)value);
                var barIdx = Array.IndexOf(barLengths, selectedSong.BarLength);

                if (barIdx == -1)
                    barIdx = barLengths.Length - 1;

                props.UpdateDomainRange(4, barLengths, barLengths[barIdx]); // 4 = bar length.
            }
        }

        public void SerializeState(ProjectBuffer buffer)
        {
            buffer.Serialize(ref selectedSong);
            buffer.Serialize(ref selectedInstrument);
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
