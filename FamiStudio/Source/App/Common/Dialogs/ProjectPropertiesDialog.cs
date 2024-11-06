using System;

namespace FamiStudio
{
    class ProjectPropertiesDialog
    {
        enum ProjectSection
        {
            Info,
            Expansion,
            Mixer,
            SoundEngine,
            Max
        };

        #region Localization

        // Title
        LocalizedString DialogTitle;

        // Sections
        LocalizedString[] SectionNames = new LocalizedString[(int)ProjectSection.Max];

        // Information labels
        LocalizedString TitleLabel;
        LocalizedString AuthorLabel;
        LocalizedString CopyrightLabel;
        LocalizedString TempoModeLabel;
        LocalizedString MachineLabel;
        LocalizedString TuningLabel;

        // Information tooltips
        LocalizedString TempoModeTooltip;
        LocalizedString AuthoringMachineTooltip;
        LocalizedString TuningTooltip;

        // Expansion labels
        LocalizedString N163ChannelsLabel;
        LocalizedString ExpansionLabel;

        // Expansion tooltips
        LocalizedString ExpansionAudioTooltip;
        LocalizedString ExpansionNumChannelsTooltip;

        // Expansion warnings
        LocalizedString MultipleExpansionsROMWarning;
        
        // Sound engine labels
        LocalizedString SoundEngineSettingsLabel;
        LocalizedString DPCMBankSwitchingLabel;
        LocalizedString DPCMExtendedRangeLabel;
        LocalizedString ExtendedInstrumentLabel;

        // Sound engine tooltips
        LocalizedString DPCMBankSwitchingTooltip;
        LocalizedString DPCMExtendedRangeTooltip;
        LocalizedString ExtendedInstrumentTooltip;

        #endregion

        private PropertyPage[] pages = new PropertyPage[(int)ProjectSection.Max];
        private MultiPropertyDialog dialog;
        private Project project;
        private MixerProperties mixerProperties;
        
        public PropertyPage InfoPage        => dialog.GetPropertyPage((int)ProjectSection.Info);
        public PropertyPage ExpansionPage   => dialog.GetPropertyPage((int)ProjectSection.Expansion);
        public PropertyPage SoundEnginePage => dialog.GetPropertyPage((int)ProjectSection.SoundEngine);

        public string Title     => InfoPage.GetPropertyValue<string>(0);
        public string Author    => InfoPage.GetPropertyValue<string>(1);
        public string Copyright => InfoPage.GetPropertyValue<string>(2);
        public int    TempoMode => InfoPage.GetSelectedIndex(3);
        public int    Machine   => InfoPage.GetSelectedIndex(4);
        public int    Tuning    => InfoPage.GetPropertyValue<int>(5);

        public MixerProperties MixerProperties => mixerProperties;

        public int ExpansionMask   => GetSelectedExpansionMask();
        public int NumN163Channels => ExpansionPage.GetPropertyValue<int>(1);

        public bool DPCMBankswitching => SoundEnginePage.GetPropertyValue<bool>(1);
        public bool DPCMExtendedRange => SoundEnginePage.GetPropertyValue<bool>(2);
        public bool InstrumentExtendedRange => SoundEnginePage.GetPropertyValue<bool>(3);

        public unsafe ProjectPropertiesDialog(FamiStudioWindow win, Project proj)
        {
            Localization.Localize(this);

            project = proj;

            dialog = new MultiPropertyDialog(win, DialogTitle, 550, true);

            for (int i = 0; i < (int)ProjectSection.Max; i++)
            {
                var section = (ProjectSection)i;
                var scroll = i == (int)ProjectSection.Mixer ? 300 : 0;
                var page = dialog.AddPropertyPage(SectionNames[i], "ProjectProperties" + section.ToString(), scroll);
                CreatePropertyPage(page, section);
            }

            dialog.SetPageVisible((int)ProjectSection.SoundEngine, Platform.IsDesktop);
        }

        private PropertyPage CreatePropertyPage(PropertyPage page, ProjectSection section)
        {
            switch (section)
            {
                case ProjectSection.Info:
                {
                    page.AddTextBox(TitleLabel.Colon, project.Name, 31); // 0
                    page.AddTextBox(AuthorLabel.Colon, project.Author, 31); // 1
                    page.AddTextBox(CopyrightLabel.Colon, project.Copyright, 31); // 2
                    page.AddDropDownList(TempoModeLabel.Colon, TempoType.Names, TempoType.Names[project.TempoMode], TempoModeTooltip); // 3
                    page.AddDropDownList(MachineLabel.Colon, Localization.ToStringArray(MachineType.LocalizedNames, MachineType.CountNoDual), MachineType.LocalizedNames[project.PalMode ? MachineType.PAL : MachineType.NTSC], AuthoringMachineTooltip); // 4
                    page.AddNumericUpDown(TuningLabel, project.Tuning, 300, 580, 1, TuningTooltip); // 5
                    page.SetPropertyEnabled(4, project.UsesFamiStudioTempo);
                    page.PropertyChanged += Info_PropertyChanged;
                    break;
                }
                case ProjectSection.Expansion:
                {
                    var numExpansions = ExpansionType.End - ExpansionType.Start + 1;
                    var expNames = new string[numExpansions];
                    var expBools = new bool[numExpansions];
                    for (int i = ExpansionType.Start; i <= ExpansionType.End; i++)
                    {
                        expNames[i - ExpansionType.Start] = ExpansionType.GetLocalizedName(i);
                        expBools[i - ExpansionType.Start] = project.UsesExpansionAudio(i);
                    }

                    page.ShowWarnings = true;
                    page.AddCheckBoxList(ExpansionLabel.Colon, expNames, expBools, ExpansionAudioTooltip); // 0
                    page.AddNumericUpDown(N163ChannelsLabel.Colon, project.ExpansionNumN163Channels, 1, 8, 1, ExpansionNumChannelsTooltip); // 1
                    page.SetPropertyEnabled(1, project.UsesExpansionAudio(ExpansionType.N163));
                    page.PropertyChanged += Expansion_PropertyChanged;
                    UpdateExpansionWarnings(page);
                    break;
                }
                case ProjectSection.Mixer:
                {
                    mixerProperties = new MixerProperties(page, project, project.ExpansionAudioMask);
                    break;
                }
                case ProjectSection.SoundEngine:
                {
                    page.AddLabel(null, SoundEngineSettingsLabel, true); // 0
                    page.AddLabelCheckBox(DPCMBankSwitchingLabel, project.SoundEngineUsesDpcmBankSwitching, 0, DPCMBankSwitchingTooltip); // 1
                    page.AddLabelCheckBox(DPCMExtendedRangeLabel, project.SoundEngineUsesDpcmBankSwitching || project.SoundEngineUsesExtendedDpcm, 0, DPCMExtendedRangeTooltip); // 2
                    page.AddLabelCheckBox(ExtendedInstrumentLabel, project.SoundEngineUsesExtendedInstruments, 0, ExtendedInstrumentTooltip); // 3
                    page.SetPropertyEnabled(2, !project.SoundEngineUsesDpcmBankSwitching);
                    page.PropertyChanged += SoundEngine_PropertyChanged;
                    break;
                }
            }

            page.Build();
            pages[(int)section] = page;

            return page;
        }

        private int GetSelectedExpansionMask()
        {
            var selectedExpansions = ExpansionPage.GetPropertyValue<bool[]>(0);
            var mask = 0;

            for (int i = 0; i < selectedExpansions.Length; i++)
            {
                if (selectedExpansions[i])
                {
                    mask |= ExpansionType.GetMaskFromValue(ExpansionType.Start + i);
                }
            }

            return mask;
        }

        private void Info_PropertyChanged(PropertyPage props, int propIdx, int rowIdx, int colIdx, object value)
        {
            if (propIdx == 3) // Tempo Mode
            {
                // Machine setting only makes sense in FamiStudio tempo.
                props.SetPropertyEnabled(4, (string)value == TempoType.Names[TempoType.FamiStudio]);
            }
        }

        private void SoundEngine_PropertyChanged(PropertyPage props, int propIdx, int rowIdx, int colIdx, object value)
        {
            if (propIdx == 1)
            {
                var bankswitching = (bool)value;
                props.SetPropertyEnabled(2, !bankswitching);
                if (bankswitching)
                    props.SetPropertyValue(2, true);
            }
        }

        private void Expansion_PropertyChanged(PropertyPage props, int propIdx, int rowIdx, int colIdx, object value)
        {
            if (propIdx == 0) // Expansion
            {
                var app = dialog.ParentWindow.FamiStudio;
                var infoPage = InfoPage;
                var expansionMask = GetSelectedExpansionMask();

                props.SetPropertyEnabled(1, (expansionMask & ExpansionType.N163Mask) != 0);

                infoPage.SetPropertyEnabled(4, infoPage.GetSelectedIndex(3) == TempoType.FamiStudio);
                infoPage.SetDropDownListIndex(4, app.Project.PalMode ? 1 : 0);

                mixerProperties.SetExpansionMask(expansionMask);
            }

            UpdateExpansionWarnings(props);
        }

        private void UpdateExpansionWarnings(PropertyPage props)
        {
            var expansionMask = GetSelectedExpansionMask();

            if (Utils.NumberOfSetBits(expansionMask) > 1)
                props.SetPropertyWarning(0, CommentType.Warning, MultipleExpansionsROMWarning);
            else
                props.SetPropertyWarning(0, CommentType.Good, "");
        }

        public void EditProjectPropertiesAsync(Action<DialogResult> callback)
        {
            dialog.ShowDialogAsync((r) =>
            {
                callback(r);
            });
        }
    }
}
