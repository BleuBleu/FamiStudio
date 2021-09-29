using System;
using System.IO;
using System.Drawing;
using System.Media;
using System.Windows.Forms;
using System.Collections.Generic;
using Color = System.Drawing.Color;
using System.Diagnostics;

using RenderBrush       = FamiStudio.GLBrush;
using RenderBitmap      = FamiStudio.GLBitmap;
using RenderBitmapAtlas = FamiStudio.GLBitmapAtlas;
using RenderFont        = FamiStudio.GLFont;
using RenderControl     = FamiStudio.GLControl;
using RenderGraphics    = FamiStudio.GLGraphics;
using RenderTheme       = FamiStudio.ThemeRenderResources;

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
        const int DefaultButtonTextNoIconPosX = 4;
        const int DefaultSubButtonSpacingX    = 18;
        const int DefaultSubButtonPosY        = 3;
        const int DefaultScrollBarThickness1  = 10;
        const int DefaultScrollBarThickness2  = 16;
        const int DefaultButtonSizeY          = 21;
        const int DefaultSliderPosX           = 100;
        const int DefaultSliderPosY           = 3;
        const int DefaultSliderSizeX          = 96;
        const int DefaultSliderSizeY          = 15;
        const int DefaultCheckBoxPosX         = 20;
        const int DefaultCheckBoxPosY         = 3;
        const int DefaultDraggedLineSizeY     = 5;

        int expandButtonSizeX;
        int buttonIconPosX;
        int buttonIconPosY;
        int buttonTextPosX;
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
        int checkBoxPosX;
        int checkBoxPosY;
        int virtualSizeY;
        int scrollBarThickness;
        int draggedLineSizeY;
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
            Reload,
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

        enum CaptureOperation
        {
            None,
            DragInstrument,
            DragArpeggio,
            DragSample,
            DragSong,
            MoveSlider,
            ScrollBar,
            MobilePan
        };

        static readonly bool[] captureNeedsThreshold = new[]
        {
            false, // None,
            true,  // DragInstrument,
            true,  // DragArpeggio,
            false, // DragSample,
            true,  // DragSong,
            false, // MoveSlider,
            false, // ScrollBar
            false  // MobilePan
        };

        int scrollY = 0;
        int mouseLastX = 0;
        int mouseLastY = 0;
        int captureMouseX = -1;
        int captureMouseY = -1;
        int captureButtonRelX = -1;
        int captureButtonRelY = -1;
        int captureButtonIdx = -1;
        int captureScrollY = -1;
        int envelopeDragIdx = -1;
        float flingVelY = 0.0f;
        float bitmapScale = 1.0f;
        bool captureThresholdMet = false;
        Button sliderDragButton = null;
        CaptureOperation captureOperation = CaptureOperation.None;
        Instrument draggedInstrument = null;
        Instrument expandedInstrument = null;
        DPCMSample expandedSample = null;
        Arpeggio draggedArpeggio = null;
        DPCMSample draggedSample = null;
        Song draggedSong = null;
        List<Button> buttons = new List<Button>();

        RenderBrush sliderFillBrush;
        RenderBrush disabledBrush;
        RenderBitmapAtlas bmpMiscAtlas;
        RenderBitmapAtlas bmpExpansionsAtlas;
        RenderBitmapAtlas bmpEnvelopesAtlas;

        enum MiscImageIndices
        {
            Expand,
            Expanded,
            Overflow,
            CheckBoxYes,
            CheckBoxNo,
            ButtonLeft,
            ButtonRight,
            Song,
            Add,
            Play,
            DPCM,
            Load,
            WaveEdit,
            Reload,
            Save,
            Count
        };

        readonly string[] MiscImageNames = new string[]
        {
            "InstrumentExpand",
            "InstrumentExpanded",
            "Warning",
            "CheckBoxYes",
            "CheckBoxNo",
            "ButtonLeft",
            "ButtonRight",
            "Music",
            "Add",
            "PlaySource",
            "ChannelDPCM",
            "InstrumentOpen",
            "WaveEdit",
            "Reload",
            "SaveSmall"
        };

        enum EnvelopesImageIndices
        {
            Volume,
            Arpeggio,
            Pitch,
            Duty,
            FdsWave,
            Mod,
            N163Wave,
            Count
        };

        readonly string[] EnvelopesImageNames = new string[]
        {
            "EnvelopeVolume",
            "EnvelopeArpeggio",
            "EnvelopePitch",
            "EnvelopeDuty",
            "EnvelopeWave",
            "EnvelopeMod",
            "EnvelopeWave",
        };

        class Button
        {
            public string text;
            public Color color = Theme.DarkGreyFillColor2;
            public RenderBrush textBrush;
            public RenderBrush textDisabledBrush;
            public RenderBitmapAtlas atlas;
            public int atlasIdx;

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
                textBrush = pe.ThemeResources.LightGreyFillBrush2;
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
                    case ButtonType.InstrumentHeader:
                        active = new[] { true, true };
                        return new[] { SubButtonType.Add, SubButtonType.Load };
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
                        active = new[] { true, true, !string.IsNullOrEmpty(sample.SourceFilename), true, true };
                        return new[] { SubButtonType.EditWave, SubButtonType.Save, SubButtonType.Reload, SubButtonType.Play, SubButtonType.Expand };
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
                        var label = Project.DPCMInstrumentName;
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
                    if ((type == ButtonType.Song       && song       == projectExplorer.App.SelectedSong)       ||
                        (type == ButtonType.Instrument && instrument == projectExplorer.App.SelectedInstrument) ||
                        (type == ButtonType.Arpeggio   && arpeggio   == projectExplorer.App.SelectedArpeggio))
                    {
                        return projectExplorer.ThemeResources.FontMediumBold;
                    }
                    else if (
                        type == ButtonType.ProjectSettings  ||
                        type == ButtonType.SongHeader       ||
                        type == ButtonType.InstrumentHeader ||
                        type == ButtonType.DpcmHeader       ||
                        type == ButtonType.ArpeggioHeader)
                    {
                        return projectExplorer.ThemeResources.FontMediumBold;
                    }
                    else
                    {
                        return projectExplorer.ThemeResources.FontMedium;
                    }
                }
            }

            public RenderTextFlags TextAlignment
            {
                get
                {
                    if (type == ButtonType.ProjectSettings ||
                        type == ButtonType.SongHeader ||
                        type == ButtonType.InstrumentHeader ||
                        type == ButtonType.DpcmHeader ||
                        type == ButtonType.ArpeggioHeader)
                    {
                        return RenderTextFlags.Center;
                    }
                    else
                    {
                        return RenderTextFlags.Left;
                    }
                }
            }

            public Color SubButtonTint => type == ButtonType.SongHeader || type == ButtonType.InstrumentHeader || type == ButtonType.DpcmHeader || type == ButtonType.ArpeggioHeader ? Theme.LightGreyFillColor1 : Color.Black;

            public bool TextEllipsis => type == ButtonType.ProjectSettings;
            
            public RenderBitmapAtlas GetIcon(SubButtonType sub, out int atlasIdx)
            {
                switch (sub)
                {
                    case SubButtonType.Add: 
                        atlasIdx = (int)MiscImageIndices.Add;  
                        return projectExplorer.bmpMiscAtlas;
                    case SubButtonType.Play: 
                        atlasIdx = (int)MiscImageIndices.Play; 
                        return projectExplorer.bmpMiscAtlas;
                    case SubButtonType.Save:
                        atlasIdx = (int)MiscImageIndices.Save; 
                        return projectExplorer.bmpMiscAtlas;
                    case SubButtonType.DPCM: 
                        atlasIdx = (int)MiscImageIndices.DPCM; 
                        return projectExplorer.bmpMiscAtlas;
                    case SubButtonType.EditWave:
                        atlasIdx = (int)MiscImageIndices.WaveEdit; 
                        return projectExplorer.bmpMiscAtlas;
                    case SubButtonType.Reload: 
                        atlasIdx = (int)MiscImageIndices.Reload;
                        return projectExplorer.bmpMiscAtlas;
                    case SubButtonType.Load: 
                        atlasIdx = (int)MiscImageIndices.Load;
                        return projectExplorer.bmpMiscAtlas;
                    case SubButtonType.Overflow: 
                        atlasIdx = (int)MiscImageIndices.Overflow;
                        return projectExplorer.bmpMiscAtlas;
                    case SubButtonType.Expand:
                    {
                        if (instrument != null)
                            atlasIdx = projectExplorer.expandedInstrument == instrument ? (int)MiscImageIndices.Expanded : (int)MiscImageIndices.Expand;
                        else
                            atlasIdx = projectExplorer.expandedSample == sample ? (int)MiscImageIndices.Expanded : (int)MiscImageIndices.Expand;

                        return projectExplorer.bmpMiscAtlas;
                    }
                }

                atlasIdx = (int)sub;
                return projectExplorer.bmpEnvelopesAtlas;
            }
        }

        public DPCMSample DraggedSample => captureOperation == CaptureOperation.DragSample ? draggedSample : null;

        public delegate void EmptyDelegate();
        public delegate void BoolDelegate(bool val);
        public delegate void InstrumentEnvelopeDelegate(Instrument instrument, int envelope);
        public delegate void InstrumentDelegate(Instrument instrument);
        public delegate void InstrumentPointDelegate(Instrument instrument, Point pos);
        public delegate void SongDelegate(Song song);
        public delegate void ArpeggioDelegate(Arpeggio arpeggio);
        public delegate void ArpeggioPointDelegate(Arpeggio arpeggio, Point pos);
        public delegate void DPCMSamplePointDelegate(DPCMSample instrument, Point pos);
        public delegate void DPCMSampleDelegate(DPCMSample sample);

        public event InstrumentEnvelopeDelegate InstrumentEdited;
        public event InstrumentDelegate InstrumentColorChanged;
        public event InstrumentDelegate InstrumentReplaced;
        public event InstrumentDelegate InstrumentDeleted;
        public event InstrumentPointDelegate InstrumentDroppedOutside;
        public event SongDelegate SongModified;
        public event ArpeggioDelegate ArpeggioEdited;
        public event ArpeggioDelegate ArpeggioColorChanged;
        public event ArpeggioDelegate ArpeggioDeleted;
        public event ArpeggioPointDelegate ArpeggioDroppedOutside;
        public event DPCMSampleDelegate DPCMSampleReloaded;
        public event DPCMSampleDelegate DPCMSampleEdited;
        public event DPCMSampleDelegate DPCMSampleColorChanged;
        public event DPCMSampleDelegate DPCMSampleDeleted;
        public event DPCMSamplePointDelegate DPCMSampleDraggedOutside;
        public event DPCMSamplePointDelegate DPCMSampleMapped;
        public event EmptyDelegate ProjectModified;
        public event BoolDelegate InstrumentsHovered;

        public ProjectExplorer()
        {
            UpdateRenderCoords();
        }

        private void UpdateRenderCoords()
        {
            expandButtonSizeX    = ScaleForMainWindow(DefaultExpandButtonSizeX);
            buttonIconPosX       = ScaleForMainWindow(DefaultButtonIconPosX);      
            buttonIconPosY       = ScaleForMainWindow(DefaultButtonIconPosY);      
            buttonTextPosX       = ScaleForMainWindow(DefaultButtonTextPosX);      
            buttonTextNoIconPosX = ScaleForMainWindow(DefaultButtonTextNoIconPosX);
            expandButtonPosX     = ScaleForMainWindow(DefaultExpandButtonPosX);
            expandButtonPosY     = ScaleForMainWindow(DefaultExpandButtonPosY);
            subButtonSpacingX    = ScaleForMainWindow(DefaultSubButtonSpacingX);   
            subButtonPosY        = ScaleForMainWindow(DefaultSubButtonPosY);       
            buttonSizeY          = ScaleForMainWindow(DefaultButtonSizeY);
            sliderPosX           = ScaleForMainWindow(DefaultSliderPosX);
            sliderPosY           = ScaleForMainWindow(DefaultSliderPosY);
            sliderSizeX          = ScaleForMainWindow(DefaultSliderSizeX);
            sliderSizeY          = ScaleForMainWindow(DefaultSliderSizeY);
            checkBoxPosX         = ScaleForMainWindow(DefaultCheckBoxPosX);
            checkBoxPosY         = ScaleForMainWindow(DefaultCheckBoxPosY);
            draggedLineSizeY     = ScaleForMainWindow(DefaultDraggedLineSizeY);
            
            virtualSizeY         = App?.Project == null ? Height : buttons.Count * buttonSizeY;
            needsScrollBar       = virtualSizeY > Height;

            if (needsScrollBar)
                scrollBarThickness = ScaleForMainWindow(Settings.ScrollBars == 1 ? DefaultScrollBarThickness1 : (Settings.ScrollBars == 2 ? DefaultScrollBarThickness2 : 0));
            else
                scrollBarThickness = 0;
        }

        public void Reset()
        {
            scrollY = 0;
            expandedInstrument = null;
            expandedSample = null;
            RefreshButtons();
            MarkDirty();
        }

        public void LayoutChanged()
        {
            UpdateRenderCoords();
            ClampScroll();
            MarkDirty();
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

            if (!IsRenderInitialized || project == null)
                return;

            var projectText = string.IsNullOrEmpty(project.Author) ? $"{project.Name}" : $"{project.Name} ({project.Author})";

            buttons.Add(new Button(this) { type = ButtonType.ProjectSettings, text = projectText });
            buttons.Add(new Button(this) { type = ButtonType.SongHeader, text = "Songs" });

            foreach (var song in project.Songs)
                buttons.Add(new Button(this) { type = ButtonType.Song, song = song, text = song.Name, color = song.Color, atlas = bmpMiscAtlas, atlasIdx = (int)MiscImageIndices.Song, textBrush = ThemeResources.BlackBrush });

            buttons.Add(new Button(this) { type = ButtonType.InstrumentHeader, text = "Instruments" });
            buttons.Add(new Button(this) { type = ButtonType.Instrument, color = Theme.LightGreyFillColor1, textBrush = ThemeResources.BlackBrush, atlas = bmpExpansionsAtlas, atlasIdx = ExpansionType.None });

            foreach (var instrument in project.Instruments)
            {
                buttons.Add(new Button(this) { type = ButtonType.Instrument, instrument = instrument, text = instrument.Name, color = instrument.Color, textBrush = ThemeResources.BlackBrush, atlas = bmpExpansionsAtlas, atlasIdx = instrument.Expansion });

                if (instrument != null && instrument == expandedInstrument)
                {
                    var instrumentParams = InstrumentParamProvider.GetParams(instrument);

                    if (instrumentParams != null)
                    {
                        foreach (var param in instrumentParams)
                        {
                            buttons.Add(new Button(this) { type = GetButtonTypeForParam(param), param = param, instrument = instrument, color = instrument.Color, text = param.Name, textBrush = ThemeResources.BlackBrush, paramScope = TransactionScope.Instrument, paramObjectId = instrument.Id });
                        }
                    }
                }
            }

            buttons.Add(new Button(this) { type = ButtonType.DpcmHeader });
            foreach (var sample in project.Samples)
            {
                buttons.Add(new Button(this) { type = ButtonType.Dpcm, sample = sample, color = sample.Color, textBrush = ThemeResources.BlackBrush, atlas = bmpMiscAtlas, atlasIdx = (int)MiscImageIndices.DPCM });

                if (sample == expandedSample)
                {
                    var sampleParams = DPCMSampleParamProvider.GetParams(sample);

                    foreach (var param in sampleParams)
                    {
                        buttons.Add(new Button(this) { type = GetButtonTypeForParam(param), param = param, sample = sample, color = sample.Color, text = param.Name, textBrush = ThemeResources.BlackBrush, paramScope = TransactionScope.DPCMSample, paramObjectId = sample.Id });
                    }
                }
            }

            buttons.Add(new Button(this) { type = ButtonType.ArpeggioHeader, text = "Arpeggios" });
            buttons.Add(new Button(this) { type = ButtonType.Arpeggio, text = "None", color = Theme.LightGreyFillColor1, textBrush = ThemeResources.BlackBrush });

            foreach (var arpeggio in project.Arpeggios)
            {
                buttons.Add(new Button(this) { type = ButtonType.Arpeggio, arpeggio = arpeggio, text = arpeggio.Name, color = arpeggio.Color, textBrush = ThemeResources.BlackBrush, atlas = bmpEnvelopesAtlas, atlasIdx = EnvelopeType.Arpeggio });
            }

            flingVelY = 0.0f;

            UpdateRenderCoords();
            ClampScroll();

            if (invalidate)
                MarkDirty();
        }

        protected override void OnRenderInitialized(RenderGraphics g)
        {
            Debug.Assert(MiscImageNames.Length      == (int)MiscImageIndices.Count);
            Debug.Assert(EnvelopesImageNames.Length == (int)EnvelopesImageIndices.Count);

            sliderFillBrush = g.CreateSolidBrush(Color.FromArgb(64, Color.Black));
            disabledBrush = g.CreateSolidBrush(Color.FromArgb(64, Color.Black));
            bmpMiscAtlas = g.CreateBitmapAtlasFromResources(MiscImageNames);
            bmpExpansionsAtlas = g.CreateBitmapAtlasFromResources(ExpansionType.Icons);
            bmpEnvelopesAtlas = g.CreateBitmapAtlasFromResources(EnvelopesImageNames);

            if (PlatformUtils.IsMobile)
                bitmapScale = g.WindowScaling * 0.25f;

            RefreshButtons();
        }

        protected override void OnRenderTerminated()
        {
            Utils.DisposeAndNullify(ref sliderFillBrush);
            Utils.DisposeAndNullify(ref disabledBrush);
            Utils.DisposeAndNullify(ref bmpMiscAtlas);
            Utils.DisposeAndNullify(ref bmpExpansionsAtlas);
            Utils.DisposeAndNullify(ref bmpEnvelopesAtlas);
        }

        protected bool ShowExpandButtons()
        {
            if (App.Project != null)
            {
                if (App.Project.Instruments.Find(i => InstrumentParamProvider.HasParams(i)) != null)
                    return true;

                if (App.Project.Samples.Count > 0)
                    return true;
            }

            return false;
        }

        protected override void OnRender(RenderGraphics g)
        {
            var c = g.CreateCommandList();

            c.DrawLine(0, 0, 0, Height, ThemeResources.BlackBrush);

            var showExpandButton = ShowExpandButtons();
            var actualWidth = Width - scrollBarThickness;
            var firstParam = true;
            var y = -scrollY;

            var minInstIdx = 1000000;
            var maxInstIdx = 0;
            var minArpIdx  = 1000000;
            var maxArpIdx  = 0;

            for (int i = 0; i < buttons.Count; i++)
            {
                var button = buttons[i];
                var atlas = button.atlas;
                var atlasIdx = button.atlasIdx;

                if (y + buttonSizeY >= 0)
                {
                    c.PushTranslation(0, y);

                    if (button.type == ButtonType.ParamCheckbox ||
                        button.type == ButtonType.ParamSlider ||
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

                            c.FillAndDrawRectangle(0, 0, actualWidth, numParamButtons * buttonSizeY, g.GetVerticalGradientBrush(button.color, numParamButtons * buttonSizeY, 0.8f), ThemeResources.BlackBrush);
                            firstParam = false;
                        }
                    }
                    else
                    {
                        c.FillAndDrawRectangle(0, 0, actualWidth, buttonSizeY, g.GetVerticalGradientBrush(button.color, buttonSizeY, 0.8f), ThemeResources.BlackBrush);
                    }

                    if (button.type == ButtonType.Instrument)
                    {
                        if (button.instrument != null)
                        {
                            minInstIdx = Math.Min(minInstIdx, i);
                            maxInstIdx = Math.Max(maxInstIdx, i);
                        }
                    }
                    else if (button.type == ButtonType.Arpeggio)
                    {
                        if (button.arpeggio != null)
                        {
                            minArpIdx = Math.Min(minArpIdx, i);
                            maxArpIdx = Math.Max(maxArpIdx, i);
                        }
                    }

                    var leftPadding = 0;
                    var leftAligned = button.type == ButtonType.Instrument || button.type == ButtonType.Song || button.type == ButtonType.ParamSlider || button.type == ButtonType.ParamCheckbox || button.type == ButtonType.ParamList || button.type == ButtonType.Arpeggio || button.type == ButtonType.Dpcm;

                    if (showExpandButton && leftAligned)
                    {
                        c.PushTranslation(1 + expandButtonSizeX, 0);
                        leftPadding = expandButtonSizeX;
                    }

                    var enabled = button.param == null || button.param.IsEnabled == null || button.param.IsEnabled();
                    var ellipsisFlag = button.TextEllipsis ? RenderTextFlags.Ellipsis : RenderTextFlags.None;

                    c.DrawText(button.Text, button.Font, atlas == null ? buttonTextNoIconPosX : buttonTextPosX, 0, enabled ? button.textBrush : disabledBrush, button.TextAlignment | ellipsisFlag | RenderTextFlags.Middle, actualWidth - buttonTextNoIconPosX * 2, buttonSizeY);

                    if (atlas != null)
                        c.DrawBitmapAtlas(atlas, atlasIdx, buttonIconPosX, buttonIconPosY, 1.0f, bitmapScale, Color.Black);

                    if (leftPadding != 0)
                        c.PopTransform();

                    if (button.param != null)
                    {
                        var paramVal = button.param.GetValue();
                        var paramStr = button.param.GetValueString();

                        if (button.type == ButtonType.ParamSlider)
                        {
                            var valSizeX = (int)Math.Round((paramVal - button.param.MinValue) / (float)(button.param.MaxValue - button.param.MinValue) * sliderSizeX);

                            c.PushTranslation(actualWidth - sliderPosX, sliderPosY);
                            c.FillRectangle(0, 0, valSizeX, sliderSizeY, sliderFillBrush);
                            c.DrawRectangle(0, 0, sliderSizeX, sliderSizeY, enabled ? ThemeResources.BlackBrush : disabledBrush, 1.0f);
                            c.DrawText(paramStr, ThemeResources.FontMedium, 0, -sliderPosY, ThemeResources.BlackBrush, RenderTextFlags.MiddleCenter, sliderSizeX, buttonSizeY);
                            c.PopTransform();
                        }
                        else if (button.type == ButtonType.ParamCheckbox)
                        {
                            c.DrawBitmapAtlas(bmpMiscAtlas, paramVal == 0 ? (int)MiscImageIndices.CheckBoxNo : (int)MiscImageIndices.CheckBoxYes, actualWidth - checkBoxPosX, checkBoxPosY, enabled ? 1.0f : 0.25f, bitmapScale, Color.Black);
                        }
                        else if (button.type == ButtonType.ParamList)
                        {
                            var paramPrev = button.param.SnapAndClampValue(paramVal - 1);
                            var paramNext = button.param.SnapAndClampValue(paramVal + 1);
                            var buttonWidth = (int)bmpMiscAtlas.GetElementSize((int)MiscImageIndices.ButtonLeft).Width;

                            c.PushTranslation(actualWidth - sliderPosX, sliderPosY);
                            c.DrawBitmapAtlas(bmpMiscAtlas, (int)MiscImageIndices.ButtonLeft, 0, 0, paramVal == paramPrev || !enabled ? 0.25f : 1.0f, bitmapScale, Color.Black);
                            c.DrawBitmapAtlas(bmpMiscAtlas, (int)MiscImageIndices.ButtonRight, sliderSizeX - buttonWidth, 0, paramVal == paramNext || !enabled ? 0.25f : 1.0f, bitmapScale, Color.Black);
                            c.DrawText(paramStr, ThemeResources.FontMedium, 0, -sliderPosY, ThemeResources.BlackBrush, RenderTextFlags.MiddleCenter, sliderSizeX, buttonSizeY);
                            c.PopTransform();
                        }
                    }
                    else
                    {
                        var subButtons = button.GetSubButtons(out var active);
                        var tint = button.SubButtonTint;

                        if (subButtons != null)
                        {
                            for (int j = 0, x = actualWidth - subButtonSpacingX; j < subButtons.Length; j++, x -= subButtonSpacingX)
                            {
                                atlas = button.GetIcon(subButtons[j], out atlasIdx);

                                if (subButtons[j] == SubButtonType.Expand)
                                    c.DrawBitmapAtlas(atlas, atlasIdx, expandButtonPosX, expandButtonPosY, 1.0f, bitmapScale, tint);
                                else
                                    c.DrawBitmapAtlas(atlas, atlasIdx, x, subButtonPosY, active[j] ? 1.0f : 0.2f, bitmapScale, tint);
                            }
                        }
                    }

                    c.PopTransform();
                }

                y += buttonSizeY;

                if (y > Height)
                {
                    break;
                }
            }

            if (captureOperation != CaptureOperation.None && captureThresholdMet)
            {
                if (captureOperation == CaptureOperation.DragSong)
                {
                    var pt = PointToClient(Cursor.Position);
                    var buttonIdx = GetButtonAtCoord(pt.X, pt.Y - buttonSizeY / 2, out _);

                    if (buttonIdx >= 0)
                    {
                        var button = buttons[buttonIdx];

                        if (button.type == ButtonType.Song ||
                            button.type == ButtonType.SongHeader)
                        {
                            var lineY = (buttonIdx + 1) * buttonSizeY;
                            c.DrawLine(0, lineY, Width - scrollBarThickness, lineY, g.GetSolidBrush(draggedSong.Color), draggedLineSizeY);
                        }
                    }
                }
                else if (captureOperation == CaptureOperation.DragInstrument || (captureOperation == CaptureOperation.DragArpeggio && draggedArpeggio != null))
                {
                    var pt = PointToClient(Cursor.Position);
                    if (ClientRectangle.Contains(pt))
                    {
                        if (envelopeDragIdx >= 0)
                        {
                            c.DrawBitmapAtlas(bmpEnvelopesAtlas, envelopeDragIdx, pt.X - captureButtonRelX, pt.Y - captureButtonRelY, 0.5f, bitmapScale);
                        }
                        else
                        {
                            var minY = (captureOperation == CaptureOperation.DragInstrument ? minInstIdx : minArpIdx) * buttonSizeY;
                            var maxY = (captureOperation == CaptureOperation.DragInstrument ? maxInstIdx : maxArpIdx) * buttonSizeY;
                            var color = (captureOperation == CaptureOperation.DragInstrument ? draggedInstrument.Color : draggedArpeggio.Color);
                            var dragY = Utils.Clamp(pt.Y - captureButtonRelY, minY, maxY);

                            c.FillRectangle(0, dragY, actualWidth, dragY + buttonSizeY, g.GetSolidBrush(color, 1, 0.5f));
                        }
                    }
                }
            }

            if (needsScrollBar)
            {
                int scrollBarSizeY = (int)Math.Round(Height * (Height  / (float)virtualSizeY));
                int scrollBarPosY  = (int)Math.Round(Height * (scrollY / (float)virtualSizeY));

                c.FillAndDrawRectangle(actualWidth, 0, Width - 1, Height, ThemeResources.DarkGreyFillBrush1, ThemeResources.BlackBrush);
                c.FillAndDrawRectangle(actualWidth, scrollBarPosY, Width - 1, scrollBarPosY + scrollBarSizeY, ThemeResources.MediumGreyFillBrush1, ThemeResources.BlackBrush);
            }

            g.Clear(Theme.DarkGreyFillColor1);
            g.DrawCommandList(c);
        }

        private bool GetScrollBarParams(out int posY, out int sizeY)
        {
            if (scrollBarThickness > 0)
            {
                sizeY = (int)Math.Round(Height * (Height  / (float)virtualSizeY));
                posY  = (int)Math.Round(Height * (scrollY / (float)virtualSizeY));
                return true;
            }
            else
            {
                posY = 0;
                sizeY = 0;
                return false;
            }
        }

        private bool ClampScroll()
        {
            int minScrollY = 0;
            int maxScrollY = Math.Max(virtualSizeY - Height, 0);

            var scrolled = true;
            if (scrollY < minScrollY) { scrollY = minScrollY; scrolled = false; }
            if (scrollY > maxScrollY) { scrollY = maxScrollY; scrolled = false; }
            return scrolled;
        }

        private bool DoScroll(int deltaY)
        {
            scrollY -= deltaY;
            MarkDirty();
            return ClampScroll();
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

        private int GetButtonAtCoord(int x, int y, out SubButtonType sub, out int buttonRelX, out int buttonRelY)
        {
            sub = SubButtonType.Max;
            buttonRelX = 0;
            buttonRelY = 0;

            if (needsScrollBar && x >= Width - scrollBarThickness)
                return -1;

            var buttonIndex = (y + scrollY) / buttonSizeY;

            if (buttonIndex >= 0 && buttonIndex < buttons.Count)
            {
                var button = buttons[buttonIndex];

                if (ShowExpandButtons() && x < (expandButtonPosX + expandButtonSizeX) && ((button.instrument != null && InstrumentParamProvider.HasParams(button.instrument)) || (button.sample != null))) 
                {
                    sub = SubButtonType.Expand;
                    return buttonIndex;
                }

                buttonRelX = x;
                buttonRelY = y - buttonIndex * buttonSizeY;

                var subButtons = button.GetSubButtons(out _);
                if (subButtons != null)
                {
                    y -= (buttonIndex * buttonSizeY - scrollY);

                    for (int i = 0; i < subButtons.Length; i++)
                    {
                        int sx = Width - scrollBarThickness - subButtonSpacingX * (i + 1);
                        int sy = subButtonPosY;
                        int dx = x - sx;
                        int dy = y - sy;

                        if (dx >= 0 && dx < 16 * DpiScaling.MainWindow &&
                            dy >= 0 && dy < 16 * DpiScaling.MainWindow)
                        {
                            buttonRelX = dx;
                            buttonRelY = dy;
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

        private int GetButtonAtCoord(int x, int y, out SubButtonType sub)
        {
            return GetButtonAtCoord(x, y, out sub, out _, out _);
        }

        private void UpdateToolTip(int x, int y)
        {
            var redTooltip = false;
            var tooltip = "";
            var buttonIdx = GetButtonAtCoord(x, y, out var subButtonType);

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
                    tooltip = "{MouseLeft} Make song current - {MouseLeft}{MouseLeft} Song properties - {MouseRight} Delete song\n{MouseLeft} {Drag} Re-order song";
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
                    if (x >= Width - scrollBarThickness - checkBoxPosX)
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
                    if (x >= Width - scrollBarThickness - sliderPosX)
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
                    if (x >= Width - scrollBarThickness - sliderPosX)
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
                    else if (subButtonType == SubButtonType.Reload)
                    {
                        tooltip = "{MouseLeft} Reload source data (if available)\nOnly possible when data was loaded from a DMC/WAV file";
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
            else if (needsScrollBar && x > Width - scrollBarThickness)
            {
                tooltip = "{MouseLeft} {Drag} Scroll";
            }

            App.SetToolTip(tooltip, redTooltip);
        }

        private void UpdateSlider(int x, int y, bool final)
        {
            if (!final)
            {
                UpdateSliderValue(sliderDragButton, x, y, false);
                MarkDirty();
            }
            else
            {
                App.UndoRedoManager.EndTransaction();
            }
        }

        private void UpdateScrollBar(int x, int y)
        {
            scrollY = captureScrollY + ((y - captureMouseY) * virtualSizeY / Height);
            ClampScroll();
            MarkDirty();
        }

        private void UpdateDragSample(int x, int y, bool final)
        {
            if (!ClientRectangle.Contains(x, y))
            {
                if (final)
                {
                    var mappingNote = App.GetDPCMSampleMappingNoteAtPos(PointToScreen(new Point(x, y)));
                    if (App.Project.NoteSupportsDPCM(mappingNote))
                    {
                        App.UndoRedoManager.BeginTransaction(TransactionScope.DPCMSamplesMapping, TransactionFlags.StopAudio);
                        App.Project.UnmapDPCMSample(mappingNote);
                        App.Project.MapDPCMSample(mappingNote, draggedSample);
                        App.UndoRedoManager.EndTransaction();

                        DPCMSampleMapped?.Invoke(draggedSample, PointToScreen(new Point(x, y)));
                    }
                }
                else
                {
                    DPCMSampleDraggedOutside?.Invoke(draggedSample, PointToScreen(new Point(x, y)));
                }
            }
        }

        private void UpdateDragSong(int x, int y, bool final)
        {
            if (final)
            {
                var buttonIdx = GetButtonAtCoord(x, y - buttonSizeY / 2, out _);

                if (buttonIdx >= 0)
                {
                    var button = buttons[buttonIdx];

                    if (button.type == ButtonType.Song ||
                        button.type == ButtonType.SongHeader)
                    {
                        var songBefore = buttons[buttonIdx].song;
                        if (songBefore != draggedSong)
                        {
                            App.UndoRedoManager.BeginTransaction(TransactionScope.ProjectNoDPCMSamples);
                            App.Project.MoveSong(draggedSong, songBefore);
                            App.UndoRedoManager.EndTransaction();

                            RefreshButtons();
                        }
                    }
                }
            }
        }

        private void UpdateDragInstrument(int x, int y, bool final)
        {
            if (final)
            {
                if (ClientRectangle.Contains(x, y))
                {
                    var buttonIdx = GetButtonAtCoord(x, y, out var subButtonType);

                    var instrumentSrc = draggedInstrument;
                    var instrumentDst = buttonIdx >= 0 && buttons[buttonIdx].type == ButtonType.Instrument ? buttons[buttonIdx].instrument : null;

                    if (instrumentSrc != instrumentDst && instrumentSrc != null && instrumentDst != null)
                    {
                        if (instrumentSrc.Expansion == instrumentDst.Expansion)
                        {
                            if (envelopeDragIdx == -1)
                            {
                                if (PlatformUtils.MessageBox($"Are you sure you want to replace all notes of instrument '{instrumentDst.Name}' with '{instrumentSrc.Name}'?", "Replace intrument", MessageBoxButtons.YesNo) == DialogResult.Yes)
                                {
                                    App.UndoRedoManager.BeginTransaction(TransactionScope.ProjectNoDPCMSamples);
                                    App.Project.ReplaceInstrument(instrumentDst, instrumentSrc);
                                    App.UndoRedoManager.EndTransaction();

                                    InstrumentReplaced?.Invoke(instrumentDst);
                                }
                            }
                            else
                            {
                                if (PlatformUtils.MessageBox($"Are you sure you want to copy the {EnvelopeType.Names[envelopeDragIdx]} envelope of instrument '{instrumentSrc.Name}' to '{instrumentDst.Name}'?", "Copy Envelope", MessageBoxButtons.YesNo) == DialogResult.Yes)
                                {
                                    App.UndoRedoManager.BeginTransaction(TransactionScope.Instrument, instrumentDst.Id);
                                    instrumentDst.Envelopes[envelopeDragIdx] = instrumentSrc.Envelopes[envelopeDragIdx].ShallowClone();
                                    instrumentDst.Envelopes[envelopeDragIdx].ClampToValidRange(instrumentDst, envelopeDragIdx);

                                    // HACK : Copy some envelope related stuff. Need to cleanup the envelope code.
                                    switch (envelopeDragIdx)
                                    {
                                        case EnvelopeType.FdsWaveform:
                                            instrumentDst.FdsWavePreset = instrumentSrc.FdsWavePreset;
                                            break;
                                        case EnvelopeType.FdsModulation:
                                            instrumentDst.FdsModPreset = instrumentSrc.FdsModPreset;
                                            break;
                                        case EnvelopeType.N163Waveform:
                                            instrumentDst.N163WavePreset = instrumentSrc.N163WavePreset;
                                            instrumentDst.N163WaveSize = instrumentSrc.N163WaveSize;
                                            break;
                                    }

                                    App.UndoRedoManager.EndTransaction();

                                    InstrumentEdited?.Invoke(instrumentDst, envelopeDragIdx);
                                }
                            }
                        }
                        else
                        {
                            App.DisplayWarning($"Incompatible audio expansion!"); ;
                        }
                    }
                }
                else
                {
                    InstrumentDroppedOutside(draggedInstrument, PointToScreen(new Point(x, y)));
                }
            }
        }
        private void UpdateDragArpeggio(int x, int y, bool final)
        {
            if (final)
            {
                if (ClientRectangle.Contains(x, y))
                {
                    var buttonIdx = GetButtonAtCoord(x, y, out var subButtonType);

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
                            }
                        }
                    }
                }
                else
                {
                    ArpeggioDroppedOutside?.Invoke(draggedArpeggio, PointToScreen(new Point(x, y)));
                }
            }
        }

        private void UpdateCaptureOperation(int x, int y)
        {
            // DROIDTODO : DO we need this?
            const int CaptureThreshold = PlatformUtils.IsDesktop ? 5 : 50;

            if (captureOperation != CaptureOperation.None && !captureThresholdMet)
            {
                if (Math.Abs(x - captureMouseX) >= CaptureThreshold ||
                    Math.Abs(y - captureMouseY) >= CaptureThreshold)
                {
                    captureThresholdMet = true;
                }
            }

            if (captureOperation != CaptureOperation.None && captureThresholdMet)
            {
                switch (captureOperation)
                {
                    case CaptureOperation.MoveSlider:
                        UpdateSlider(x, y, false);
                        break;
                    case CaptureOperation.ScrollBar:
                        UpdateScrollBar(x, y);
                        break;
                    case CaptureOperation.DragSample:
                        UpdateDragSample(x, y, false);
                        break;
                    case CaptureOperation.MobilePan:
                        DoScroll(y - mouseLastY);
                        break;
                    default:
                        MarkDirty();
                        break;
                }
            }
        }

        protected void EmitInstrumentHoverEvent(int x, int y)
        {
            var buttonIdx = GetButtonAtCoord(x, y, out _);
            var showExpansions = buttonIdx >= 0 && (buttons[buttonIdx].type == ButtonType.Instrument || buttons[buttonIdx].type == ButtonType.InstrumentHeader);
            InstrumentsHovered?.Invoke(showExpansions);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            bool middle = e.Button.HasFlag(MouseButtons.Middle) || (e.Button.HasFlag(MouseButtons.Left) && ModifierKeys.HasFlag(Keys.Alt));

            UpdateCursor();
            UpdateCaptureOperation(e.X, e.Y);

            if (middle)
            {
                DoScroll(e.Y - mouseLastY);
            }

            UpdateToolTip(e.X, e.Y);
            EmitInstrumentHoverEvent(e.X, e.Y);

            mouseLastX = e.X;
            mouseLastY = e.Y;
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            InstrumentsHovered?.Invoke(false);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            EndCaptureOperation(e.X, e.Y);
            UpdateCursor();
            MarkDirty();
        }

#if FALSE // // MATTT FAMISTUDIO_WINDOWS
        protected override void OnMouseCaptureChanged(EventArgs e)
        {
            AbortCaptureOperation();
            base.OnMouseCaptureChanged(e);
        }
#endif

        private void StartCaptureOperation(int x, int y, CaptureOperation op, int buttonIdx = -1, int buttonRelX = 0, int buttonRelY = 0)
        {
            Debug.Assert(captureOperation == CaptureOperation.None);
            mouseLastX = x;
            mouseLastY = y;
            captureMouseX = x;
            captureMouseY = y;
            captureButtonIdx = buttonIdx;
            captureButtonRelX = buttonRelX;
            captureButtonRelY = buttonRelY;
            captureScrollY = scrollY;
            Capture = true;
            captureOperation = op;
            captureThresholdMet = !captureNeedsThreshold[(int)op];
        }

        private void EndCaptureOperation(int x, int y)
        {
            if (captureOperation != CaptureOperation.None && captureThresholdMet)
            {
                switch (captureOperation)
                {
                    case CaptureOperation.DragInstrument:
                        UpdateDragInstrument(x, y, true);
                        break;
                    case CaptureOperation.DragArpeggio:
                        UpdateDragArpeggio(x, y, true);
                        break;
                    case CaptureOperation.DragSample:
                        UpdateDragSample(x, y, true);
                        break;
                    case CaptureOperation.MoveSlider:
                        UpdateSlider(x, y, true);
                        break;
                    case CaptureOperation.DragSong:
                        UpdateDragSong(x, y, true);
                        break;
                }
            }

            draggedArpeggio = null;
            draggedInstrument = null;
            draggedSample = null;
            draggedSong = null;
            sliderDragButton = null;
            captureOperation = CaptureOperation.None;
            Capture = false;
        }

        private void AbortCaptureOperation()
        {
            if (App.UndoRedoManager.HasTransactionInProgress)
                App.UndoRedoManager.AbortTransaction();

            MarkDirty();

            draggedArpeggio = null;
            draggedInstrument = null;
            draggedSample = null;
            draggedSong = null;
            sliderDragButton = null;
            captureOperation = CaptureOperation.None;
            Capture = false;
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            DoScroll(e.Delta > 0 ? buttonSizeY * 3 : -buttonSizeY * 3);
        }

        protected override void OnResize(EventArgs e)
        {
            UpdateRenderCoords();
            ClampScroll();
            base.OnResize(e);
        }

        bool UpdateSliderValue(Button button, int x, int y, bool mustBeInside)
        {
            var buttonIdx = buttons.IndexOf(button);
            Debug.Assert(buttonIdx >= 0);

            bool shift = ModifierKeys.HasFlag(Keys.Shift);

            var actualWidth = Width - scrollBarThickness;
            var buttonX = x;
            var buttonY = y + scrollY - buttonIdx * buttonSizeY;

            bool insideSlider = (buttonX > (actualWidth - sliderPosX) &&
                                 buttonX < (actualWidth - sliderPosX + sliderSizeX) &&
                                 buttonY > (sliderPosY) &&
                                 buttonY < (sliderPosY + sliderSizeY));

            if (mustBeInside && !insideSlider)
                return false;

            var paramVal = button.param.GetValue();

            if (shift)
            {
                var delta = (x - captureMouseX) / 4;
                if (delta != 0)
                {
                    paramVal = Utils.Clamp(paramVal + delta * button.param.SnapValue, button.param.MinValue, button.param.MaxValue);
                    captureMouseX = x;
                }
            }
            else
            {
                paramVal = (int)Math.Round(Utils.Lerp(button.param.MinValue, button.param.MaxValue, Utils.Clamp((buttonX - (actualWidth - sliderPosX)) / (float)sliderSizeX, 0.0f, 1.0f)));
                captureMouseX = x;
            }

            paramVal = button.param.SnapAndClampValue(paramVal);
            button.param.SetValue(paramVal);

            App.Project.GetPackedSampleData();

            return insideSlider;
        }

        private void ImportSongs()
        {
            Action<string> ImportSongsAction = (filename) =>
            {
                if (filename != null)
                {
                    App.BeginLogTask();

                    Project otherProject = App.OpenProjectFile(filename, false);

                    if (otherProject != null)
                    {
                        var songNames = new List<string>();
                        foreach (var song in otherProject.Songs)
                            songNames.Add(song.Name);

                        var dlg = new PropertyDialog("Import Songs", 300);
                        dlg.Properties.AddLabel(null, "Select songs to import:"); // 0
                        dlg.Properties.AddCheckBoxList(null, songNames.ToArray(), null); // 1
                        dlg.Properties.AddButton(null, "Select All"); // 2
                        dlg.Properties.AddButton(null, "Select None"); // 3
                        dlg.Properties.PropertyClicked += ImportSongs_PropertyClicked;
                        dlg.Properties.Build();

                        dlg.ShowDialogAsync(ParentForm, (r) =>
                        {
                            if (r == DialogResult.OK)
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
                                    otherProject.DeleteAllSongsBut(songIds.ToArray());
                                    success = App.Project.MergeProject(otherProject);
                                }

                                App.UndoRedoManager.AbortOrEndTransaction(success);
                                RefreshButtons();
                            }
                        });
                    }
                    App.EndLogTask();
                }
            };

            if (PlatformUtils.IsMobile)
            {
                MobileProjectDialog dlg = new MobileProjectDialog(App, "Import Songs", false, false);
                dlg.ShowDialogAsync((f) => ImportSongsAction(f));
            }
            else
            {
                var filename = PlatformUtils.ShowOpenFileDialog("Open File", "All Song Files (*.fms;*.txt;*.ftm)|*.fms;*.txt;*.ftm|FamiStudio Files (*.fms)|*.fms|FamiTracker Files (*.ftm)|*.ftm|FamiTracker Text Export (*.txt)|*.txt|FamiStudio Text Export (*.txt)|*.txt", ref Settings.LastInstrumentFolder);
                ImportSongsAction(filename);
            }
        }

        private void ImportSongs_PropertyClicked(PropertyPage props, ClickType click, int propIdx, int rowIdx, int colIdx)
        {
            if (click == ClickType.Button)
            {
                var newValues = new bool[props.GetPropertyValue<bool[]>(1).Length];

                if (propIdx == 2)
                {
                    for (int i = 0; i < newValues.Length; i++)
                        newValues[i] = true;
                }

                props.UpdateCheckBoxList(1, newValues);
            }
        }

        private void ImportInstruments()
        {
            Action<string> ImportInstrumentsAction = (filename) =>
            {
                if (filename != null)
                {
                    App.BeginLogTask();
                    if (filename.ToLower().EndsWith("fti"))
                    {
                        App.UndoRedoManager.BeginTransaction(TransactionScope.Project);
                        var success = new FamitrackerInstrumentFile().CreateFromFile(App.Project, filename) != null;
                        App.UndoRedoManager.AbortOrEndTransaction(success);
                        RefreshButtons();
                    }
                    else
                    {
                        Project instrumentProject = App.OpenProjectFile(filename, false);

                        if (instrumentProject != null)
                        {
                            var hasDpcmInstrument = false;

                            var instruments = new List<Instrument>();
                            var instrumentNames = new List<string>();

                            if (instrumentProject.HasAnyMappedSamples)
                            {
                                hasDpcmInstrument = true;
                                instruments.Add(null);
                                instrumentNames.Add(Project.DPCMInstrumentName);
                            }

                            foreach (var instrument in instrumentProject.Instruments)
                            {
                                instruments.Add(instrument);
                                instrumentNames.Add(instrument.NameWithExpansion);
                            }

                            var dlg = new PropertyDialog("Import Instruments", 300);
                            dlg.Properties.AddLabel(null, "Select instruments to import:"); // 0
                            dlg.Properties.AddCheckBoxList(null, instrumentNames.ToArray(), null); // 1
                            dlg.Properties.AddButton(null, "Select All"); // 2
                            dlg.Properties.AddButton(null, "Select None"); // 3
                            dlg.Properties.Build();
                            dlg.Properties.PropertyClicked += ImportInstrument_PropertyClicked;

                            dlg.ShowDialogAsync(ParentForm, (r) =>
                            {
                                if (r == DialogResult.OK)
                                {
                                    var selected = dlg.Properties.GetPropertyValue<bool[]>(1);
                                    var instrumentsIdsToMerge = new List<int>();

                                    for (int i = hasDpcmInstrument ? 1 : 0; i < selected.Length; i++)
                                    {
                                        if (selected[i])
                                            instrumentsIdsToMerge.Add(instruments[i].Id);
                                    }

                                    // Wipe everything but the instruments we want.
                                    if (!hasDpcmInstrument || selected[0] == false)
                                        instrumentProject.DeleteAllSamples();
                                    instrumentProject.DeleteAllSongs();
                                    instrumentProject.DeleteAllArpeggios();
                                    instrumentProject.DeleteAllInstrumentBut(instrumentsIdsToMerge.ToArray());
                                    instrumentProject.DeleteUnmappedSamples();

                                    App.UndoRedoManager.BeginTransaction(TransactionScope.Project);
                                    var success = App.Project.MergeProject(instrumentProject);
                                    App.UndoRedoManager.AbortOrEndTransaction(success);
                                    RefreshButtons();
                                }
                            });
                        }
                    }
                    App.EndLogTask();
                }
            };

            if (PlatformUtils.IsMobile)
            {
                MobileProjectDialog dlg = new MobileProjectDialog(App, "Import Instruments", false, false);
                dlg.ShowDialogAsync((f) => ImportInstrumentsAction(f));
            }
            else
            {
                var filename = PlatformUtils.ShowOpenFileDialog("Open File", "All Instrument Files (*.fti;*.fms;*.txt;*.ftm)|*.fti;*.fms;*.txt;*.ftm|FamiTracker Instrument File (*.fti)|*.fti|FamiStudio Files (*.fms)|*.fms|FamiTracker Files (*.ftm)|*.ftm|FamiTracker Text Export (*.txt)|*.txt|FamiStudio Text Export (*.txt)|*.txt", ref Settings.LastInstrumentFolder);
                ImportInstrumentsAction(filename);
            }
        }

        private void ImportInstrument_PropertyClicked(PropertyPage props, ClickType click, int propIdx, int rowIdx, int colIdx)
        {
            if (click == ClickType.Button)
            {
                var newValues = new bool[props.GetPropertyValue<bool[]>(1).Length];

                if (propIdx == 2)
                {
                    for (int i = 0; i < newValues.Length; i++)
                        newValues[i] = true;
                }

                props.UpdateCheckBoxList(1, newValues);
            }
        }

        private void LoadDPCMSample()
        {
            Action<string[]> LoadDPCMSampleAction = (filenames) =>
            {
                if (filenames != null && filenames.Length > 0)
                {
                    var numFamiStudioFiles = 0;
                    var numSamplesFiles = 0;
                    foreach (var fn in filenames)
                    {
                        var ext = Path.GetExtension(fn).ToLower();

                        if (ext == ".fms" && PlatformUtils.IsDesktop)
                            numFamiStudioFiles++;
                        else if (ext == ".dmc" || ext == ".wav")
                            numSamplesFiles++;
                    }

                    if (numFamiStudioFiles > 1 || (numFamiStudioFiles == 1 && numSamplesFiles != 0))
                    {
                        PlatformUtils.MessageBoxAsync("You can only select one FamiStudio project to import samples from.", "Error", MessageBoxButtons.OK);
                        return;
                    }
                    else if (numFamiStudioFiles == 1)
                    {
                        Project samplesProject = App.OpenProjectFile(filenames[0], false);

                        if (samplesProject != null)
                        {
                            if (samplesProject.Samples.Count == 0)
                            {
                                PlatformUtils.MessageBox("The selected project does not contain any samples.", "Error", MessageBoxButtons.OK);
                                return;
                            }

                            var samplesNames = new List<string>();

                            foreach (var sample in samplesProject.Samples)
                                samplesNames.Add(sample.Name);

                            var dlg = new PropertyDialog("Import DPCM Samples", 300);
                            dlg.Properties.AddLabel(null, "Select samples to import:"); // 0
                            dlg.Properties.AddCheckBoxList("Import DPCM Samples", samplesNames.ToArray(), null); // 1
                            dlg.Properties.AddButton(null, "Select All"); // 2
                            dlg.Properties.AddButton(null, "Select None"); // 3
                            dlg.Properties.Build();
                            dlg.Properties.PropertyClicked += ImportInstrument_PropertyClicked;

                            dlg.ShowDialogAsync(ParentForm, (r) =>
                            {
                                if (r == DialogResult.OK)
                                {
                                    App.BeginLogTask();
                                    {
                                        var selected = dlg.Properties.GetPropertyValue<bool[]>(1);
                                        var sampleIdsToMerge = new List<int>();

                                        for (int i = 0; i < selected.Length; i++)
                                        {
                                            if (selected[i])
                                                sampleIdsToMerge.Add(samplesProject.Samples[i].Id);
                                        }

                                        // Wipe everything but the instruments we want.
                                        samplesProject.DeleteAllSongs();
                                        samplesProject.DeleteAllArpeggios();
                                        samplesProject.DeleteAllSamplesBut(sampleIdsToMerge.ToArray());
                                        samplesProject.DeleteAllInstruments();
                                        samplesProject.DeleteAllMappings();

                                        App.UndoRedoManager.BeginTransaction(TransactionScope.DPCMSamples);
                                        bool success = App.Project.MergeProject(samplesProject);
                                        App.UndoRedoManager.AbortOrEndTransaction(success);
                                    }
                                    App.EndLogTask();
                                    RefreshButtons();
                                }
                            });
                        }
                    }
                    else if (numSamplesFiles > 0)
                    {
                        App.BeginLogTask();
                        {
                            App.UndoRedoManager.BeginTransaction(TransactionScope.DPCMSamples);

                            foreach (var filename in filenames)
                            {
                                var sampleName = Path.GetFileNameWithoutExtension(filename);
                                if (sampleName.Length > 16)
                                    sampleName = sampleName.Substring(0, 16);
                                sampleName = App.Project.GenerateUniqueDPCMSampleName(sampleName);

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

                                        App.Project.CreateDPCMSampleFromWavData(sampleName, wavData, sampleRate, filename);
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
                                    App.Project.CreateDPCMSampleFromDmcData(sampleName, dmcData, filename);
                                }
                            }

                            App.UndoRedoManager.EndTransaction();
                            RefreshButtons();
                        }
                        App.EndLogTask();
                    }
                }
            };

            if (PlatformUtils.IsMobile)
            {
                PlatformUtils.StartMobileLoadFileOperationAsync("*/*", (f) => LoadDPCMSampleAction(new[] { f }));
            }
            else
            {
                var filenames = PlatformUtils.ShowOpenFileDialog("Open File", "All Sample Files (*.wav;*.dmc;*.fms)|*.wav;*.dmc;*.fms|Wav Files (*.wav)|*.wav|DPCM Sample Files (*.dmc)|*.dmc|FamiStudio Files (*.fms)|*.fms", ref Settings.LastSampleFolder, true);
                LoadDPCMSampleAction(filenames);
            }
        }

        private void AddSong()
        {
            App.UndoRedoManager.BeginTransaction(TransactionScope.ProjectNoDPCMSamples, TransactionFlags.StopAudio);
            App.SelectedSong = App.Project.CreateSong();
            App.UndoRedoManager.EndTransaction();
            RefreshButtons();

            // DROIDTODO : Scroll to the new song.
        }

        private void AskAndDeleteSong(Song song)
        {
            if (PlatformUtils.MessageBox($"Are you sure you want to delete '{song.Name}' ?", "Delete song", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                bool selectNewSong = song == App.SelectedSong;
                App.UndoRedoManager.BeginTransaction(TransactionScope.ProjectNoDPCMSamples, TransactionFlags.StopAudio);
                App.Project.DeleteSong(song);
                if (selectNewSong)
                    App.SelectedSong = App.Project.Songs[0];
                App.UndoRedoManager.EndTransaction();
                RefreshButtons();
            }
        }

        private void AddInstrument(int expansionType)
        {
            App.UndoRedoManager.BeginTransaction(TransactionScope.ProjectNoDPCMSamples);
            App.SelectedInstrument = App.Project.CreateInstrument(expansionType);
            App.UndoRedoManager.EndTransaction();
            RefreshButtons();
        }

        private void AskAddInstrument(int x, int y)
        {
            var instrumentType = ExpansionType.None;

            if (App.Project.NeedsExpansionInstruments)
            {
                var activeExpansions = App.Project.GetActiveExpansions();
                var expNames = new List<string>();

                expNames.Add(ExpansionType.Names[ExpansionType.None]);
                for (int i = 0; i < activeExpansions.Length; i++)
                {
                    if (ExpansionType.NeedsExpansionInstrument(activeExpansions[i]))
                        expNames.Add(ExpansionType.Names[activeExpansions[i]]);
                }

                var dlg = new PropertyDialog("Add Instrument", PointToScreen(new Point(x, y)), 260, true);
                dlg.Properties.AddDropDownList("Expansion:", expNames.ToArray(), expNames[0]); // 0
                dlg.Properties.Build();

                dlg.ShowDialogAsync(ParentForm, (r) =>
                {
                    if (r == DialogResult.OK)
                    {
                        instrumentType = ExpansionType.GetValueForName(dlg.Properties.GetPropertyValue<string>(0));
                        AddInstrument(instrumentType);
                    }
                });
            }
            else
            {
                AddInstrument(instrumentType);
            }
        }

        private void ToggleExpandInstrument(Instrument inst)
        {
            expandedInstrument = expandedInstrument == inst ? null : inst;
            expandedSample = null;
            RefreshButtons(false);
        }

        private void AskDeleteInstrument(Instrument inst)
        {
            if (PlatformUtils.MessageBox($"Are you sure you want to delete '{inst.Name}' ? All notes using this instrument will be deleted.", "Delete instrument", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                bool selectNewInstrument = inst == App.SelectedInstrument;
                App.UndoRedoManager.BeginTransaction(TransactionScope.ProjectNoDPCMSamples, TransactionFlags.StopAudio);
                App.Project.DeleteInstrument(inst);
                if (selectNewInstrument)
                    App.SelectedInstrument = App.Project.Instruments.Count > 0 ? App.Project.Instruments[0] : null;
                InstrumentDeleted?.Invoke(inst);
                App.UndoRedoManager.EndTransaction();
                RefreshButtons();
            }
        }

        private void ClearInstrumentEnvelope(Instrument inst, int envelopeType)
        {
            App.UndoRedoManager.BeginTransaction(TransactionScope.Instrument, inst.Id);
            inst.Envelopes[envelopeType].ClearToDefault(envelopeType);
            App.UndoRedoManager.EndTransaction();
            MarkDirty();
        }

        private void AddArpeggio()
        {
            App.UndoRedoManager.BeginTransaction(TransactionScope.ProjectNoDPCMSamples);
            App.SelectedArpeggio = App.Project.CreateArpeggio();
            App.UndoRedoManager.EndTransaction();
            RefreshButtons();
        }

        private void AskDeleteArpeggio(Arpeggio arpeggio)
        {
            if (PlatformUtils.MessageBox($"Are you sure you want to delete '{arpeggio.Name}' ? All notes using this arpeggio will be no longer be arpeggiated.", "Delete arpeggio", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                bool selectNewArpeggio = arpeggio == App.SelectedArpeggio;
                App.UndoRedoManager.BeginTransaction(TransactionScope.ProjectNoDPCMSamples, TransactionFlags.StopAudio);
                App.Project.DeleteArpeggio(arpeggio);
                if (selectNewArpeggio)
                    App.SelectedArpeggio = App.Project.Arpeggios.Count > 0 ? App.Project.Arpeggios[0] : null;
                ArpeggioDeleted?.Invoke(arpeggio);
                App.UndoRedoManager.EndTransaction();
                RefreshButtons();
            }
        }

        private void ReloadDPCMSampleSourceData(DPCMSample sample)
        {
            if (!string.IsNullOrEmpty(sample.SourceFilename))
            {
                if (File.Exists(sample.SourceFilename))
                {
                    if (sample.SourceDataIsWav)
                    {
                        var wavData = WaveFile.Load(sample.SourceFilename, out var sampleRate);
                        if (wavData != null)
                        {
                            var maximumSamples = sampleRate * 2;
                            if (wavData.Length > maximumSamples)
                                Array.Resize(ref wavData, maximumSamples);

                            App.UndoRedoManager.BeginTransaction(TransactionScope.DPCMSamples);
                            sample.SetWavSourceData(wavData, sampleRate, sample.SourceFilename, false);
                            sample.Process();
                            App.UndoRedoManager.EndTransaction();
                        }
                    }
                    else
                    {
                        var dmcData = File.ReadAllBytes(sample.SourceFilename);
                        if (dmcData.Length > DPCMSample.MaxSampleSize)
                            Array.Resize(ref dmcData, DPCMSample.MaxSampleSize);

                        App.UndoRedoManager.BeginTransaction(TransactionScope.DPCMSamples);
                        sample.SetDmcSourceData(dmcData, sample.SourceFilename, false);
                        sample.Process();
                        App.UndoRedoManager.EndTransaction();
                    }

                    DPCMSampleReloaded?.Invoke(sample);
                }
                else
                {
                    App.DisplayWarning($"Cannot find source file '{sample.SourceFilename}'!"); ;
                }
            }
        }

        private void ExportDPCMSampleProcessedData(DPCMSample sample)
        {
            var filename = PlatformUtils.ShowSaveFileDialog("Save File", "DPCM Samples (*.dmc)|*.dmc", ref Settings.LastSampleFolder);
            if (filename != null)
                File.WriteAllBytes(filename, sample.ProcessedData);
        }

        private void ExportDPCMSampleSourceData(DPCMSample sample)
        {
            if (sample.SourceDataIsWav)
            {
                var filename = PlatformUtils.ShowSaveFileDialog("Save File", "Wav file (*.wav)|*.wav", ref Settings.LastSampleFolder);
                if (filename != null)
                    WaveFile.Save(sample.SourceWavData.Samples, filename, sample.SourceWavData.SampleRate, 1);
            }
            else
            {
                var filename = PlatformUtils.ShowSaveFileDialog("Save File", "DPCM Samples (*.dmc)|*.dmc", ref Settings.LastSampleFolder);
                if (filename != null)
                    File.WriteAllBytes(filename, sample.SourceDmcData.Data);
            }
        }

        private void ToggleExpandDPCMSample(DPCMSample sample)
        {
            expandedSample = expandedSample == sample ? null : sample;
            expandedInstrument = null;
            RefreshButtons();
        }

        private void AskDeleteDPCMSample(DPCMSample sample)
        {
            if (PlatformUtils.MessageBox($"Are you sure you want to delete DPCM Sample '{sample.Name}' ? It will be removed from the DPCM Instrument and every note using it will be silent.", "Delete DPCM Sample", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                App.UndoRedoManager.BeginTransaction(TransactionScope.DPCMSamples, TransactionFlags.StopAudio);
                App.Project.DeleteSample(sample);
                DPCMSampleDeleted?.Invoke(sample);
                App.UndoRedoManager.EndTransaction();
                RefreshButtons();
            }
        }

        private bool HandleMouseDownPan(MouseEventArgs e)
        {
            bool middle = e.Button.HasFlag(MouseButtons.Middle) || (e.Button.HasFlag(MouseButtons.Left) && ModifierKeys.HasFlag(Keys.Alt));

            if (middle)
            {
                mouseLastY = e.Y;
                return true;
            }

            return false;
        }

        private bool HandleMouseDownScrollbar(MouseEventArgs e)
        {
            if (e.Button.HasFlag(MouseButtons.Left) && needsScrollBar && e.X > Width - scrollBarThickness && GetScrollBarParams(out var scrollBarPosY, out var scrollBarSizeY))
            {
                if (e.Y < scrollBarPosY)
                {
                    scrollY -= Height;
                    ClampScroll();
                    MarkDirty();
                }
                else if (e.Y > (scrollBarPosY + scrollBarSizeY))
                {
                    scrollY += Height;
                    ClampScroll();
                    MarkDirty();
                }
                else
                {
                    StartCaptureOperation(e.X, e.Y, CaptureOperation.ScrollBar);
                }

                return true;
            }

            return false;
        }

        private bool HandleMouseDownSongHeaderButton(MouseEventArgs e, SubButtonType subButtonType)
        {
            if (e.Button.HasFlag(MouseButtons.Left))
            {
                if (subButtonType == SubButtonType.Add)
                    AddSong();
                else if (subButtonType == SubButtonType.Load)
                    ImportSongs();
            }

            return true;
        }

        private bool HandleMouseDownSongButton(MouseEventArgs e, Button button, int buttonIdx)
        {
            if (e.Button.HasFlag(MouseButtons.Left))
            {
                App.SelectedSong = button.song;
                StartCaptureOperation(e.X, e.Y, CaptureOperation.DragSong, buttonIdx);
                draggedSong = button.song;
            }
            else if (e.Button.HasFlag(MouseButtons.Right) && App.Project.Songs.Count > 1)
            {
                AskAndDeleteSong(button.song);
            }

            return true;
        }

        private bool HandleMouseDownInstrumentHeaderButton(MouseEventArgs e, SubButtonType subButtonType)
        {
            if (e.Button.HasFlag(MouseButtons.Left))
            {
                if (subButtonType == SubButtonType.Add)
                    AskAddInstrument(e.X, e.Y);
                if (subButtonType == SubButtonType.Load)
                    ImportInstruments();
            }

            return true;
        }

        private bool HandleMouseDownInstrumentButton(MouseEventArgs e, Button button, SubButtonType subButtonType, int buttonIdx, int buttonRelX, int buttonRelY)
        {
            if (e.Button.HasFlag(MouseButtons.Left))
            {
                App.SelectedInstrument = button.instrument;

                if (button.instrument != null)
                {
                    envelopeDragIdx = -1;
                    draggedInstrument = button.instrument;
                    StartCaptureOperation(e.X, e.Y, CaptureOperation.DragInstrument, buttonIdx, buttonRelX, buttonRelY);
                }

                if (subButtonType == SubButtonType.Expand)
                {
                    ToggleExpandInstrument(button.instrument);
                }
                else if (subButtonType == SubButtonType.DPCM)
                {
                    InstrumentEdited?.Invoke(button.instrument, EnvelopeType.Count);
                }
                else if (subButtonType < SubButtonType.EnvelopeMax)
                {
                    InstrumentEdited?.Invoke(button.instrument, (int)subButtonType);
                    envelopeDragIdx = (int)subButtonType;
                }
            }
            else if (e.Button.HasFlag(MouseButtons.Right) && button.instrument != null)
            {
                if (subButtonType < SubButtonType.EnvelopeMax)
                    ClearInstrumentEnvelope(button.instrument, (int)subButtonType);
                else if (subButtonType == SubButtonType.Max)
                    AskDeleteInstrument(button.instrument);
            }

            return true;
        }

        private bool HandleMouseDownParamSliderButton(MouseEventArgs e, Button button, int buttonIdx)
        {
            bool left = e.Button.HasFlag(MouseButtons.Left);
            bool right = e.Button.HasFlag(MouseButtons.Right);

            if (left || right)
            {
                App.UndoRedoManager.BeginTransaction(button.paramScope, button.paramObjectId);

                if (left)
                {
                    captureMouseX = e.X; // Hack, UpdateSliderValue relies on this.

                    if (UpdateSliderValue(button, e.X, e.Y, true))
                    {
                        sliderDragButton = button;
                        StartCaptureOperation(e.X, e.Y, CaptureOperation.MoveSlider, buttonIdx);
                        MarkDirty();
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
                    MarkDirty();
                }
            }

            return true;
        }

        private bool HandleMouseDownParamCheckboxButton(MouseEventArgs e, Button button)
        {
            bool left  = e.Button.HasFlag(MouseButtons.Left);
            bool right = e.Button.HasFlag(MouseButtons.Right);

            if (left || right)
            {
                var actualWidth = Width - scrollBarThickness;

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
                    MarkDirty();
                }
            }

            return true;
        }

        private bool HandleMouseDownParamListButton(MouseEventArgs e, Button button)
        {
            bool left  = e.Button.HasFlag(MouseButtons.Left);
            bool right = e.Button.HasFlag(MouseButtons.Right);

            if (left || right)
            {
                var actualWidth = Width - scrollBarThickness;
                var buttonWidth = (int)bmpMiscAtlas.GetElementSize((int)MiscImageIndices.ButtonLeft).Width;
                var buttonX = e.X;
                var leftButton = buttonX > (actualWidth - sliderPosX) && buttonX < (actualWidth - sliderPosX + buttonWidth);
                var rightButton = buttonX > (actualWidth - sliderPosX + sliderSizeX - buttonWidth) && buttonX < (actualWidth - sliderPosX + sliderSizeX);
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
                    MarkDirty();
                }
                else if (right && buttonX > (actualWidth - sliderPosX))
                {
                    App.UndoRedoManager.BeginTransaction(button.paramScope, button.paramObjectId);
                    button.param.SetValue(button.param.DefaultValue);
                    App.UndoRedoManager.EndTransaction();
                    MarkDirty();
                }
            }

            return true;
        }

        private bool HandleMouseDownArpeggioHeaderButton(MouseEventArgs e, SubButtonType subButtonType)
        {
            if (e.Button.HasFlag(MouseButtons.Left))
            {
                if (subButtonType == SubButtonType.Add)
                    AddArpeggio();
            }

            return true;
        }

        private bool HandleMouseDownArpeggioButton(MouseEventArgs e, Button button, SubButtonType subButtonType, int buttonIdx, int buttonRelX, int buttonRelY)
        {
            if (e.Button.HasFlag(MouseButtons.Left))
            {
                App.SelectedArpeggio = button.arpeggio;

                envelopeDragIdx = -1;
                draggedArpeggio = button.arpeggio;
                StartCaptureOperation(e.X, e.Y, CaptureOperation.DragArpeggio, buttonIdx, buttonRelX, buttonRelY);

                if (subButtonType < SubButtonType.EnvelopeMax)
                {
                    envelopeDragIdx = (int)subButtonType;
                    ArpeggioEdited?.Invoke(button.arpeggio);
                }
            }
            else if (e.Button.HasFlag(MouseButtons.Right) && button.arpeggio != null) 
            {
                AskDeleteArpeggio(button.arpeggio);
            }

            return true;
        }

        private bool HandleMouseDownDpcmHeaderButton(MouseEventArgs e, SubButtonType subButtonType)
        {
            if (e.Button.HasFlag(MouseButtons.Left) && subButtonType == SubButtonType.Load)
            {
                LoadDPCMSample();
            }

            return true;
        }

        private bool HandleMouseDownDpcmButton(MouseEventArgs e, Button button, SubButtonType subButtonType, int buttonIdx)
        {
            if (e.Button.HasFlag(MouseButtons.Left))
            {
                if (subButtonType == SubButtonType.EditWave)
                {
                    DPCMSampleEdited?.Invoke(button.sample);
                }
                else if (subButtonType == SubButtonType.Reload)
                {
                    ReloadDPCMSampleSourceData(button.sample);
                }
                else if (subButtonType == SubButtonType.Save)
                {
                    ExportDPCMSampleProcessedData(button.sample);
                }
                else if (subButtonType == SubButtonType.Play)
                {
                    App.PreviewDPCMSample(button.sample, false);
                }
                else if (subButtonType == SubButtonType.Expand)
                {
                    ToggleExpandDPCMSample(button.sample);
                }
                else if (subButtonType == SubButtonType.Max)
                {
                    draggedSample = button.sample;
                    StartCaptureOperation(e.X, e.Y, CaptureOperation.DragSample, buttonIdx);
                    MarkDirty();
                }
            }
            else if (e.Button.HasFlag(MouseButtons.Right))
            {
                if (subButtonType == SubButtonType.Play)
                {
                    App.PreviewDPCMSample(button.sample, true);
                }
                else if (subButtonType == SubButtonType.Save)
                {
                    ExportDPCMSampleSourceData(button.sample);
                }
                else if (subButtonType == SubButtonType.Max)
                {
                    AskDeleteDPCMSample(button.sample);
                }
            }

            return true;
        }

        private bool HandleMouseDownButtons(MouseEventArgs e)
        {
            var buttonIdx = GetButtonAtCoord(e.X, e.Y, out var subButtonType, out var buttonRelX, out var buttonRelY);

            if (buttonIdx >= 0)
            {
                bool left  = e.Button.HasFlag(MouseButtons.Left);
                bool right = e.Button.HasFlag(MouseButtons.Right);

                var button = buttons[buttonIdx];

                switch (button.type)
                {
                    case ButtonType.SongHeader:
                        return HandleMouseDownSongHeaderButton(e, subButtonType);
                    case ButtonType.Song:
                        return HandleMouseDownSongButton(e, button, buttonIdx);
                    case ButtonType.InstrumentHeader:
                        return HandleMouseDownInstrumentHeaderButton(e, subButtonType);
                    case ButtonType.Instrument:
                        return HandleMouseDownInstrumentButton(e, button, subButtonType, buttonIdx, buttonRelX, buttonRelY);
                    case ButtonType.ParamSlider:
                        return HandleMouseDownParamSliderButton(e, button, buttonIdx);
                    case ButtonType.ParamCheckbox:
                        return HandleMouseDownParamCheckboxButton(e, button);
                    case ButtonType.ParamList:
                        return HandleMouseDownParamListButton(e, button);
                    case ButtonType.ArpeggioHeader:
                        return HandleMouseDownArpeggioHeaderButton(e, subButtonType);
                    case ButtonType.Arpeggio:
                        return HandleMouseDownArpeggioButton(e, button, subButtonType, buttonIdx, buttonRelX, buttonRelY);
                    case ButtonType.DpcmHeader:
                        return HandleMouseDownDpcmHeaderButton(e, subButtonType);
                    case ButtonType.Dpcm:
                        return HandleMouseDownDpcmButton(e, button, subButtonType, buttonIdx);
                }

                return true;
            }

            return false;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (captureOperation != CaptureOperation.None)
                return;

            bool left   = e.Button.HasFlag(MouseButtons.Left);
            bool right  = e.Button.HasFlag(MouseButtons.Right);

            if (HandleMouseDownPan(e)) goto Handled;
            if (HandleMouseDownScrollbar(e)) goto Handled;
            if (HandleMouseDownButtons(e)) goto Handled;
            return;

        Handled: // Yes, i use a goto, sue me.
            MarkDirty();
        }

        private bool HandleTouchClickSongHeaderButton(int x, int y, SubButtonType subButtonType)
        {
            if (subButtonType == SubButtonType.Add)
                AddSong();
            else if (subButtonType == SubButtonType.Load)
                ImportSongs();

            return true;
        }

        private bool HandleTouchClickInstrumentHeaderButton(int x, int y, SubButtonType subButtonType)
        {
            if (subButtonType == SubButtonType.Add)
                AskAddInstrument(x, y);
            if (subButtonType == SubButtonType.Load)
                ImportInstruments();

            return true;
        }

        private bool HandleTouchClickSongButton(int x, int y, Button button)
        {
            App.SelectedSong = button.song;
            return true;
        }

        private bool HandleTouchClickInstrumentButton(int x, int y, Button button, SubButtonType subButtonType, int buttonIdx, int buttonRelX, int buttonRelY)
        {
            //if (e.Button.HasFlag(MouseButtons.Left))
            //{
                App.SelectedInstrument = button.instrument;

                if (subButtonType == SubButtonType.Expand)
                {
                    ToggleExpandInstrument(button.instrument);
                }
                else if (subButtonType == SubButtonType.DPCM)
                {
                    InstrumentEdited?.Invoke(button.instrument, EnvelopeType.Count);
                }
                else if (subButtonType < SubButtonType.EnvelopeMax)
                {
                    InstrumentEdited?.Invoke(button.instrument, (int)subButtonType);
                    envelopeDragIdx = (int)subButtonType;
                }
            //}
            //else if (e.Button.HasFlag(MouseButtons.Right) && button.instrument != null)
            //{
            //    if (subButtonType < SubButtonType.EnvelopeMax)
            //        ClearInstrumentEnvelope(button.instrument, (int)subButtonType);
            //    else if (subButtonType == SubButtonType.Max)
            //        AskDeleteInstrument(button.instrument);
            //}

            return true;
        }

        private bool HandleTouchClickArpeggioHeaderButton(int x, int y, SubButtonType subButtonType)
        {
            if (subButtonType == SubButtonType.Add)
                AddArpeggio();
            return true;
        }

        private bool HandleTouchClickArpeggioButton(int x, int y, Button button, SubButtonType subButtonType, int buttonIdx, int buttonRelX, int buttonRelY)
        {
            //if (e.Button.HasFlag(MouseButtons.Left))
            //{
                App.SelectedArpeggio = button.arpeggio;

                if (subButtonType < SubButtonType.EnvelopeMax)
                    ArpeggioEdited?.Invoke(button.arpeggio);
            //}
            //else if (e.Button.HasFlag(MouseButtons.Right) && button.arpeggio != null)
            //{
            //    AskDeleteArpeggio(button.arpeggio);
            //}

            return true;
        }

        private bool HandleTouchClickDpcmHeaderButton(int x, int y, SubButtonType subButtonType)
        {
            if (subButtonType == SubButtonType.Load)
                LoadDPCMSample();
            return true;
        }

        private bool HandleTouchClickDpcmButton(int x, int y, Button button, SubButtonType subButtonType, int buttonIdx)
        {
            //if (e.Button.HasFlag(MouseButtons.Left))
            //{
                if (subButtonType == SubButtonType.EditWave)
                {
                    DPCMSampleEdited?.Invoke(button.sample);
                }
                else if (subButtonType == SubButtonType.Reload)
                {
                    ReloadDPCMSampleSourceData(button.sample);
                }
                else if (subButtonType == SubButtonType.Save)
                {
                    ExportDPCMSampleProcessedData(button.sample);
                }
                else if (subButtonType == SubButtonType.Play)
                {
                    App.PreviewDPCMSample(button.sample, false);
                }
                else if (subButtonType == SubButtonType.Expand)
                {
                    ToggleExpandDPCMSample(button.sample);
                }
                //else if (subButtonType == SubButtonType.Max)
                //{
                //    draggedSample = button.sample;
                //    StartCaptureOperation(e.X, e.Y, CaptureOperation.DragSample, buttonIdx);
                //    MarkDirty();
                //}
            //}
            //else if (e.Button.HasFlag(MouseButtons.Right))
            //{
            //    if (subButtonType == SubButtonType.Play)
            //    {
            //        App.PreviewDPCMSample(button.sample, true);
            //    }
            //    else if (subButtonType == SubButtonType.Save)
            //    {
            //        ExportDPCMSampleSourceData(button.sample);
            //    }
            //    else if (subButtonType == SubButtonType.Max)
            //    {
            //        AskDeleteDPCMSample(button.sample);
            //    }
            //}

            return true;
        }


        private bool HandleTouchClickButtons(int x, int y)
        {
            var buttonIdx = GetButtonAtCoord(x, y, out var subButtonType, out var buttonRelX, out var buttonRelY);

            if (buttonIdx >= 0)
            {
                var button = buttons[buttonIdx];

                switch (button.type)
                {
                    case ButtonType.SongHeader:
                        return HandleTouchClickSongHeaderButton(x, y, subButtonType);
                    case ButtonType.Song:
                        return HandleTouchClickSongButton(x, y, button);
                    case ButtonType.InstrumentHeader:
                        return HandleTouchClickInstrumentHeaderButton(x, y, subButtonType);
                    case ButtonType.Instrument:
                        return HandleTouchClickInstrumentButton(x, y, button, subButtonType, buttonIdx, buttonRelX, buttonRelY);
                        /*
                    case ButtonType.ParamSlider:
                        return HandleMouseDownParamSliderButton(e, button, buttonIdx);
                    case ButtonType.ParamCheckbox:
                        return HandleMouseDownParamCheckboxButton(e, button);
                    case ButtonType.ParamList:
                        return HandleMouseDownParamListButton(e, button);
                        */
                    case ButtonType.ArpeggioHeader:
                        return HandleTouchClickArpeggioHeaderButton(x, y, subButtonType);
                    case ButtonType.Arpeggio:
                        return HandleTouchClickArpeggioButton(x, y, button, subButtonType, buttonIdx, buttonRelX, buttonRelY);
                    case ButtonType.DpcmHeader:
                        return HandleTouchClickDpcmHeaderButton(x, y, subButtonType);
                    case ButtonType.Dpcm:
                        return HandleTouchClickDpcmButton(x, y, button, subButtonType, buttonIdx);
                        
                }

                return true;
            }

            return false;
        }

        protected override void OnTouchDown(int x, int y)
        {
            flingVelY = 0;

            if (captureOperation != CaptureOperation.None)
                return;

            StartCaptureOperation(x, y, CaptureOperation.MobilePan);
        }

        protected override void OnTouchClick(int x, int y)
        {
            if (captureOperation != CaptureOperation.None)
                return;

            HandleTouchClickButtons(x, y);
        }

        protected override void OnTouchMove(int x, int y)
        {
            UpdateCursor();
            UpdateCaptureOperation(x, y);
            UpdateToolTip(x, y);
            EmitInstrumentHoverEvent(x, y);

            mouseLastX = x;
            mouseLastY = y;
        }

        protected override void OnTouchUp(int x, int y)
        {
            EndCaptureOperation(x, y);
        }

        protected override void OnTouchFling(int x, int y, float velX, float velY)
        {
            EndCaptureOperation(x, y);
            flingVelY = velY;
        }

        private void TickFling(float delta)
        {
            if (flingVelY != 0.0f)
            {
                var deltaPixel = (int)Math.Round(flingVelY * delta);
                if (deltaPixel != 0 && DoScroll(deltaPixel))
                    flingVelY *= (float)Math.Exp(delta * -4.5f);
                else
                    flingVelY = 0.0f;
            }
        }

        public void Tick(float delta)
        {
            TickFling(delta);
        }

        private void EditProjectProperties(Point pt)
        {
            var project = App.Project;

            var numExpansions = ExpansionType.End - ExpansionType.Start + 1;
            var expNames = new string[numExpansions];
            var expBools = new bool[numExpansions];
            for (int i = ExpansionType.Start; i <= ExpansionType.End; i++)
            {
                expNames[i - ExpansionType.Start] = ExpansionType.Names[i];
                expBools[i - ExpansionType.Start] = project.UsesExpansionAudio(i);
            }

            var dlg = new PropertyDialog("Project Properties", PointToScreen(pt), 360, true);
            dlg.Properties.ShowWarnings = true;
            dlg.Properties.AddTextBox("Title :", project.Name, 31); // 0
            dlg.Properties.AddTextBox("Author :", project.Author, 31); // 1
            dlg.Properties.AddTextBox("Copyright :", project.Copyright, 31); // 2
            dlg.Properties.AddDropDownList("Tempo Mode :", TempoType.Names, TempoType.Names[project.TempoMode], CommonTooltips.TempoMode); // 3
            dlg.Properties.AddDropDownList("Authoring Machine :", MachineType.NamesNoDual, MachineType.NamesNoDual[project.PalMode ? MachineType.PAL : MachineType.NTSC], CommonTooltips.AuthoringMachine); // 4
            dlg.Properties.AddNumericUpDown("N163 Channels :", project.ExpansionNumN163Channels, 1, 8, CommonTooltips.ExpansionNumChannels); // 5 (Namco)
            dlg.Properties.AddCheckBoxList("Expansion Audio :", expNames, expBools, CommonTooltips.ExpansionAudio, 150); // 6
            dlg.Properties.SetPropertyEnabled(4, project.UsesFamiStudioTempo && !project.UsesAnyExpansionAudio);
            dlg.Properties.SetPropertyEnabled(5, project.UsesExpansionAudio(ExpansionType.N163));
            dlg.Properties.PropertyChanged += ProjectProperties_PropertyChanged;
            UpdateProjectPropertiesWarnings(dlg.Properties);
            dlg.Properties.Build();

            dlg.ShowDialogAsync(ParentForm, (r) =>
            {
                if (r == DialogResult.OK)
                {
                    var selectedExpansions = dlg.Properties.GetPropertyValue<bool[]>(6);
                    var expansionMask = 0;
                    var expansionRemoved = false;

                    for (int i = 0; i < selectedExpansions.Length; i++)
                    {
                        var selected = selectedExpansions[i];
                        expansionMask |= (selected ? 1 : 0) << i;

                        if (project.UsesExpansionAudio(i + ExpansionType.Start) && !selected)
                            expansionRemoved = true;
                    }

                    var tempoMode = TempoType.GetValueForName(dlg.Properties.GetPropertyValue<string>(3));
                    var palAuthoring = MachineType.GetValueForName(dlg.Properties.GetPropertyValue<string>(4)) == 1;
                    var numChannels = dlg.Properties.GetPropertyValue<int>(5);

                    var changedTempoMode = tempoMode != project.TempoMode;
                    var changedExpansion = expansionMask != project.ExpansionAudioMask;
                    var changedNumChannels = numChannels != project.ExpansionNumN163Channels;
                    var changedAuthoringMachine = palAuthoring != project.PalMode;

                    var transFlags = TransactionFlags.None;

                    if (changedAuthoringMachine || changedExpansion || changedNumChannels)
                        transFlags = TransactionFlags.ReinitializeAudio;
                    else if (changedTempoMode)
                        transFlags = TransactionFlags.StopAudio;

                    App.UndoRedoManager.BeginTransaction(TransactionScope.ProjectNoDPCMSamples, transFlags);

                    project.Name = dlg.Properties.GetPropertyValue<string>(0);
                    project.Author = dlg.Properties.GetPropertyValue<string>(1);
                    project.Copyright = dlg.Properties.GetPropertyValue<string>(2);

                    if (changedExpansion || changedNumChannels)
                    {
                        if (!expansionRemoved || expansionRemoved && PlatformUtils.MessageBox($"Remove an expansion will delete all instruments and channels using it, continue?", "Change expansion audio", MessageBoxButtons.YesNo) == DialogResult.Yes)
                        {
                            App.SelectedInstrument = project.Instruments.Count > 0 ? project.Instruments[0] : null;
                            project.SetExpansionAudioMask(expansionMask, numChannels);
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
            });
        }

        private void UpdateProjectPropertiesWarnings(PropertyPage props)
        {
            var famiStudioTempo = props.GetPropertyValue<string>(3) == "FamiStudio";
            var selectedExpansions = props.GetPropertyValue<bool[]>(6);
            var numExpansionsSelected = 0;

            for (int i = 0; i < selectedExpansions.Length; i++)
            {
                if (selectedExpansions[i])
                    numExpansionsSelected++;
            }

            if (numExpansionsSelected > 1)
                props.SetPropertyWarning(6, CommentType.Warning, "Using multiple expansions will prevent you from exporting to the FamiStudio Sound Engine or FamiTracker.");
            else
                props.SetPropertyWarning(6, CommentType.Good, "");
        }

        private void ProjectProperties_PropertyChanged(PropertyPage props, int propIdx, int rowIdx, int colIdx, object value)
        {
            var selectedExpansions = props.GetPropertyValue<bool[]>(6);
            var anyExpansionsSelected = false;
            var n163Selected = false;

            for (int i = 0; i < selectedExpansions.Length; i++)
            {
                if (selectedExpansions[i])
                {
                    if (i + ExpansionType.Start == ExpansionType.N163)
                        n163Selected = true;
                    anyExpansionsSelected = true;
                }
            }

            if (propIdx == 6) // Expansion
            {
                props.SetPropertyEnabled(5, n163Selected);
                props.SetPropertyEnabled(4, props.GetPropertyValue<string>(3) == "FamiStudio" && !anyExpansionsSelected);

                if (anyExpansionsSelected)
                    props.SetDropDownListIndex(4, 0);
                else
                    props.SetDropDownListIndex(4, App.Project.PalMode ? 1 : 0);
            }
            else if (propIdx == 3) // Tempo Mode
            {
                props.SetPropertyEnabled(4, (string)value == TempoType.Names[TempoType.FamiStudio] && !anyExpansionsSelected);
            }

            UpdateProjectPropertiesWarnings(props);
        }

        private void EditSongProperties(Point pt, Song song)
        {
            var dlg = new PropertyDialog("Song Properties", PointToScreen(pt), 320, true); 

            var tempoProperties = new TempoProperties(dlg.Properties, song);

            dlg.Properties.AddColoredTextBox(song.Name, song.Color); // 0
            dlg.Properties.AddColorPicker(song.Color); // 1
            dlg.Properties.AddNumericUpDown("Song Length :", song.Length, 1, Song.MaxLength, CommonTooltips.SongLength); // 2
            tempoProperties.AddProperties();
            dlg.Properties.Build();

            dlg.ShowDialogAsync(ParentForm, (r) =>
            {
                if (r == DialogResult.OK)
                {
                    App.UndoRedoManager.BeginTransaction(TransactionScope.ProjectNoDPCMSamples, TransactionFlags.StopAudio);
                    App.SeekSong(0);

                    var newName = dlg.Properties.GetPropertyValue<string>(0);

                    if (App.Project.RenameSong(song, newName))
                    {
                        song.Color = dlg.Properties.GetPropertyValue<System.Drawing.Color>(1);
                        song.SetLength(dlg.Properties.GetPropertyValue<int>(2));

                        tempoProperties.Apply();

                        SongModified?.Invoke(song);
                        App.UndoRedoManager.EndTransaction();
                        RefreshButtons(false);
                    }
                    else
                    {
                        App.UndoRedoManager.AbortTransaction();
                        SystemSounds.Beep.Play();
                    }

                    MarkDirty();
                }
            });
        }

        private void EditInstrumentProperties(Point pt, Instrument instrument)
        {
            var dlg = new PropertyDialog("Instrument Properties", PointToScreen(pt), 240, true, pt.Y > Height / 2);
            dlg.Properties.AddColoredTextBox(instrument.Name, instrument.Color); // 0
            dlg.Properties.AddColorPicker(instrument.Color); // 1
            dlg.Properties.Build();

            dlg.ShowDialogAsync(ParentForm, (r) =>
            {
                if (r == DialogResult.OK)
                {
                    var newName = dlg.Properties.GetPropertyValue<string>(0);

                    App.UndoRedoManager.BeginTransaction(TransactionScope.ProjectNoDPCMSamples);

                    if (App.Project.RenameInstrument(instrument, newName))
                    {
                        instrument.Color = dlg.Properties.GetPropertyValue<Color>(1);
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
            });
        }

        private void EditArpeggioProperties(Point pt, Arpeggio arpeggio)
        {
            var dlg = new PropertyDialog("Arpeggio Properties", PointToScreen(pt), 240, true, pt.Y > Height / 2);
            dlg.Properties.AddColoredTextBox(arpeggio.Name, arpeggio.Color); // 0
            dlg.Properties.AddColorPicker(arpeggio.Color); // 1
            dlg.Properties.Build();

            dlg.ShowDialogAsync(ParentForm, (r) =>
            {
                if (r == DialogResult.OK)
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
            });
        }

        private void EditDPCMSampleProperties(Point pt, DPCMSample sample)
        {
            var dlg = new PropertyDialog("DPCM Sample Properties", PointToScreen(pt), 240, true, pt.Y > Height / 2);
            dlg.Properties.AddColoredTextBox(sample.Name, sample.Color); // 0
            dlg.Properties.AddColorPicker(sample.Color); // 1
            dlg.Properties.Build();

            dlg.ShowDialogAsync(ParentForm, (r) =>
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
            });
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

        public void ValidateIntegrity()
        {
        }

        public void SerializeState(ProjectBuffer buffer)
        {
            buffer.Serialize(ref expandedInstrument);
            buffer.Serialize(ref expandedSample);
            buffer.Serialize(ref scrollY);

            if (buffer.IsReading)
            {
                captureOperation = CaptureOperation.None;
                Capture = false;
                flingVelY = 0.0f;

                ClampScroll();
                RefreshButtons();
            }
        }
    }
}
