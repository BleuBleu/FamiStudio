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

        private int globalOverrideIndex;
        private int globalGridIndex;
        private int[] chipOverrideIndices = new int[ExpansionType.Count];
        private int[] chipGridIndices = new int[ExpansionType.Count];

        #region Localization

        //// Tooltips
        //private LocalizedString XXX;

        //// Labels
        //private LocalizedString YYY;

        #endregion

        public MixerProperties(PropertyPage props, Project project)
        {
            Localization.Localize(this);

            // MATTT : Only allow override for enabled expansions.
            if (project != null)
            {
                props.AddLabel(null,
                    "Project can override the app mixer setting for a specific expansion by checking the " +
                    "checkbox next to the expansion name. Leaving a section unchecked will fall back on " +
                    "the app settings.", true);
            }

            if (project != null)
            {
                globalOverrideIndex = props.AddLabelCheckBox("Override global settings", true);
            }
            else
            {
                props.AddLabel(null, "Global");
                globalOverrideIndex = -1;
            }

            globalGridIndex = props.AddGrid("Keys",
                new[]
                {
                    new ColumnDesc("Param", 0.3f),
                    new ColumnDesc("Value", 0.7f, ColumnType.Slider)
                },
                project != null ? 
                    new object[,] { { "Bass", 50 } } :
                    new object[,] { { "Volume", 50 }, { "Bass", 50 } 
                },
                project != null ? 1 : 2, null, GridOptions.NoHeader);

            for (var i = 0; i < ExpansionType.LocalizedChipNames.Length; i++)
            {
                if (project != null)
                {
                    // MATTT : Format as "OVerride xxx}
                    chipOverrideIndices[i] = props.AddLabelCheckBox(ExpansionType.LocalizedChipNames[i], true);
                }
                else
                {
                    props.AddLabel(null, ExpansionType.LocalizedChipNames[i]);
                    chipOverrideIndices[i] = -1;
                }

                chipGridIndices[i] = props.AddGrid("Keys",
                    new[]
                    {
                        new ColumnDesc("Param", 0.3f),
                        new ColumnDesc("Value", 0.7f, ColumnType.Slider)
                    },
                    new object[,] {
                        { "Volume", 50 },
                        { "Treble", 50 },
                        { "Treble Freq", 50 },
                    },
                    3, null, GridOptions.NoHeader);
                
                //props.SetPropertyEnabled();
            }

            if (project != null)
            {
                props.AddButton(null, "Copy app settings to project");
                props.AddButton(null, "Copy project settings to app");
            }

            props.AddButton(null, "Reset all to default");
        }
    }
}
