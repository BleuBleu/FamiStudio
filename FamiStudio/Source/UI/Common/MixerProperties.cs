using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace FamiStudio
{
    class MixerProperties
    {
        private PropertyPage props;
        private Project project;

        private int globalOverrideIndex = -1;
        private int globalGridIndex = -1;
        private int[] chipOverrideIndices = new int[ExpansionType.Count];
        private int[] chipGridIndices = new int[ExpansionType.Count];
        private int copyAppSettingsIndex = -1;
        private int resetDefaultIndex = -1;

        private bool changed = false;
        public bool Changed => changed;

        #region Localization

        // Labels
        private LocalizedString ProjectOverrideLabel;
        private LocalizedString GlobalOverrideLabel;
        private LocalizedString GlobalLabel;
        private LocalizedString VolumeLabel;
        private LocalizedString BassFilterLabel;
        private LocalizedString TrebleLabel;
        private LocalizedString TrebleFreqLabel;
        private LocalizedString ResetToAppSettingsLabel;
        private LocalizedString ResetToDefaultsLabel;
        private LocalizedString NoteLabel;
        private LocalizedString NoteMessageLabel;
        private LocalizedString ChipOverridLabel;

        // Tooltips
        private LocalizedString GlobalGridTooltip;
        private LocalizedString ExpansionGridTooltip;

        #endregion

        public MixerProperties(PropertyPage page, Project proj, int expansionMask = -1)
        {
            Localization.Localize(this);

            props = page;
            project = proj;

            var projectSettings = project != null;

            // TODO : Only allow override for enabled expansions.
            if (projectSettings)
            {
                props.AddLabel(null, ProjectOverrideLabel, true);
            }

            if (projectSettings)
            {
                globalOverrideIndex = props.AddLabelCheckBox(GlobalOverrideLabel, project.OverrideBassCutoffHz);
            }
            else if (Platform.IsDesktop)
            {
                props.AddLabel(null, GlobalLabel);
            }

            globalGridIndex = props.AddGrid(Platform.IsMobile ? GlobalLabel : string.Empty,
                new[]
                {
                    new ColumnDesc("", 0.4f),
                    new ColumnDesc("", 0.6f, ColumnType.Slider)
                },
                projectSettings ? 
                    new object[,] { { BassFilterLabel.ToString(), project.OverrideBassCutoffHz ? project.BassCutoffHz : Settings.BassCutoffHz } } :
                    new object[,] { { VolumeLabel.ToString(), (int)(Settings.GlobalVolumeDb * 10) }, { BassFilterLabel.ToString(), Settings.BassCutoffHz } 
                },
                projectSettings ? 1 : 2, GlobalGridTooltip, GridOptions.NoHeader);

            props.SetPropertyEnabled(globalGridIndex, !projectSettings || project.OverrideBassCutoffHz);

            var rowIdx = 0;
            if (!projectSettings)
                props.OverrideCellSlider(globalGridIndex, rowIdx++, 1, -100, 30, (o) => FormattableString.Invariant($"{(int)o / 10.0:F1} dB"));
            props.OverrideCellSlider(globalGridIndex, rowIdx, 1, 2, 100, (o) => FormattableString.Invariant($"{(int)o} Hz"));

            var expansionMixerSettings = projectSettings ? project.ExpansionMixerSettings : Settings.ExpansionMixerSettings;

            for (var i = 0; i < ExpansionType.LocalizedChipNames.Length; i++)
            {
                var mixerSetting = Settings.ExpansionMixerSettings[i];
                var overridden = false;

                if (projectSettings)
                {
                    var bit = 1 << i;
                    var expEnabled = i == 0 || (bit & (expansionMask << 1)) != 0;
                    overridden = project.ExpansionMixerSettings[i].Override && expEnabled;
                    chipOverrideIndices[i] = props.AddLabelCheckBox(ChipOverridLabel.Format(ExpansionType.LocalizedChipNames[i]), overridden);
                    if (overridden)
                        mixerSetting = project.ExpansionMixerSettings[i];
                }
                else
                {
                    if (Platform.IsDesktop)
                        props.AddLabel(null, ExpansionType.LocalizedChipNames[i]);
                    chipOverrideIndices[i] = -1;
                }

                chipGridIndices[i] = props.AddGrid(Platform.IsMobile ? ExpansionType.LocalizedChipNames[i] : string.Empty,
                    new[]
                    {
                        new ColumnDesc("", 0.4f),
                        new ColumnDesc("", 0.6f, ColumnType.Slider)
                    },
                    new object[,] {
                        { VolumeLabel.ToString(), (int)(mixerSetting.VolumeDb * 10) },
                        { TrebleLabel.ToString(), (int)(mixerSetting.TrebleDb * 10) },
                        { TrebleFreqLabel.ToString(), (int)(mixerSetting.TrebleRolloffHz / 100) },
                    },
                    3, ExpansionGridTooltip, GridOptions.NoHeader);

                props.OverrideCellSlider(chipGridIndices[i], 0, 1, -100, 100, (o) => FormattableString.Invariant($"{(int)o / 10.0:F1} dB"));
                props.OverrideCellSlider(chipGridIndices[i], 1, 1, -1000, 50, (o) => FormattableString.Invariant($"{(int)o / 10.0:F1} dB"));
                props.OverrideCellSlider(chipGridIndices[i], 2, 1, 1, 441, (o) => FormattableString.Invariant($"{(int)o * 100} Hz"));
                props.SetPropertyEnabled(chipGridIndices[i], project == null || overridden);
            }

            if (projectSettings)
                copyAppSettingsIndex = props.AddButton(null, ResetToAppSettingsLabel);
            resetDefaultIndex = props.AddButton(null, ResetToDefaultsLabel);

            page.AddLabel(null, NoteMessageLabel, true);

            props.PropertyChanged += Props_PropertyChanged;
            props.PropertyClicked += Props_PropertyClicked;
            props.PropertyCellEnabled += (p, i, r, c) => i != chipGridIndices[ExpansionType.Fds] || r != 1; // No cutoff on special FDS filter.

            if (projectSettings)
            {
                for (var i = 1; i < ExpansionType.LocalizedChipNames.Length; i++)
                {
                    var bit = 1 << i;
                    var expEnabled = (bit & (expansionMask << 1)) != 0;
                    props.SetPropertyEnabled(chipOverrideIndices[i], expEnabled);
                    props.SetPropertyEnabled(chipGridIndices[i], expEnabled && props.GetPropertyValue<bool>(chipOverrideIndices[i]));
                }
            }
        }

        public void SetExpansionMask(int expansionMask)
        {
            Debug.Assert(project != null);

            for (var i = 1; i < ExpansionType.LocalizedChipNames.Length; i++)
            {
                var bit = 1 << i;
                var expEnabled = (bit & (expansionMask << 1)) != 0;
                if (!expEnabled)
                    props.SetPropertyValue(chipOverrideIndices[i], false);
                props.SetPropertyEnabled(chipOverrideIndices[i], expEnabled);
                props.SetPropertyEnabled(chipGridIndices[i], expEnabled && props.GetPropertyValue<bool>(chipOverrideIndices[i]));
            }
        }

        private void LoadSettings(float globalVolume, int bassCutoffHz, ExpansionMixer[] settings)
        {
            if (project != null)
            {
                props.SetPropertyValue(globalGridIndex, 0, 1, bassCutoffHz);
            }
            else
            {
                // Defaults from settings
                props.SetPropertyValue(globalGridIndex, 0, 1, (int)(globalVolume * 10));
                props.SetPropertyValue(globalGridIndex, 1, 1, bassCutoffHz);
            }

            for (var i = 0; i < ExpansionType.LocalizedChipNames.Length; i++)
            {
                var def = settings[i];

                props.SetPropertyValue(chipGridIndices[i], 0, 1, (int)(def.VolumeDb * 10));
                props.SetPropertyValue(chipGridIndices[i], 1, 1, (int)(def.TrebleDb * 10));
                props.SetPropertyValue(chipGridIndices[i], 2, 1, def.TrebleRolloffHz / 100);
            }
        }

        private void Props_PropertyClicked(PropertyPage props, ClickType click, int propIdx, int rowIdx, int colIdx)
        {
            if (propIdx == resetDefaultIndex)
            {
                LoadSettings(Settings.DefaultGlobalVolumeDb, Settings.DefaultBassCutoffHz, ExpansionMixer.DefaultExpansionMixerSettings); 
            }
            else if (propIdx == copyAppSettingsIndex)
            {
                LoadSettings(Settings.GlobalVolumeDb, Settings.BassCutoffHz, Settings.ExpansionMixerSettings);
            }

            changed = true;
        }

        private void Props_PropertyChanged(PropertyPage props, int propIdx, int rowIdx, int colIdx, object value)
        {
            if (propIdx == globalOverrideIndex)
            {
                props.SetPropertyEnabled(globalGridIndex, (bool)value);
            }
            else
            {
                var chipIndex = Array.FindIndex(chipOverrideIndices, (i) => i == propIdx);
                if (chipIndex >= 0)
                    props.SetPropertyEnabled(chipGridIndices[chipIndex], (bool)value);
            }

            changed = true;
        }

        public void Apply()
        {
            var target = project != null ? project.ExpansionMixerSettings : Settings.ExpansionMixerSettings;

            if (project != null)
            {
                project.OverrideBassCutoffHz = props.GetPropertyValue<bool>(globalOverrideIndex);
                project.BassCutoffHz = project.OverrideBassCutoffHz ? props.GetPropertyValue<int>(globalGridIndex, 0, 1) : Settings.DefaultBassCutoffHz;
            }
            else
            {
                Settings.GlobalVolumeDb = props.GetPropertyValue<int>(globalGridIndex, 0, 1) / 10.0f;
                Settings.BassCutoffHz = props.GetPropertyValue<int>(globalGridIndex, 1, 1);
            }

            for (var i = 0; i < ExpansionType.LocalizedChipNames.Length; i++)
            {
                if (chipOverrideIndices[i] < 0 || props.GetPropertyValue<bool>(chipOverrideIndices[i]))
                {
                    target[i].Override = project != null;
                    target[i].VolumeDb = props.GetPropertyValue<int>(chipGridIndices[i], 0, 1) / 10.0f;
                    target[i].TrebleDb = props.GetPropertyValue<int>(chipGridIndices[i], 1, 1) / 10.0f;
                    target[i].TrebleRolloffHz = props.GetPropertyValue<int>(chipGridIndices[i], 2, 1) * 100;
                }
                else
                {
                    target[i].Override = false;
                }
            }
        }
    }
}
