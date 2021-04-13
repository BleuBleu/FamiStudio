using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace FamiStudio
{
    class MidiImportDialog
    {
        private PropertyDialog dialog;
        private string[] trackNames;
        private string filename;
        private int[] channelMapping = new int[5] { 0, 1, 2, 3, 4 };

        public MidiImportDialog(string file)
        {
            filename = file;
            trackNames = new MidiFile().GetTrackNames(file);

            if (trackNames != null)
            {
                dialog = new PropertyDialog(500);
                dialog.Properties.AddDropDownList("Expansion:", ExpansionType.Names, ExpansionType.Names[0]); // 0
                dialog.Properties.AddCheckBox("Use velocity as volume:", true); // 1
                dialog.Properties.AddCheckBox("Create instruments:", true); // 2
                dialog.Properties.AddDropDownList("Polyphony behavior:", new[] { "Keep old note", "Use new note" }, "Keep old note"); // 3
                dialog.Properties.AddLabel(null, "Channel mapping (double-click on a row to change)"); // 4
                dialog.Properties.AddMultiColumnList(new[] { "NES Channel", "MIDI Source" }, null, MappingListDoubleClicked, null); // 5
                dialog.Properties.Build();
                dialog.Properties.PropertyChanged += Properties_PropertyChanged;

                UpdateListView();
            }
        }

        private void Properties_PropertyChanged(PropertyPage props, int idx, object value)
        {
            if (idx == 0)
            {
                var expansion = ExpansionType.GetValueForName((string)value);
                var newChannelCount = Channel.GetChannelCountForExpansion(expansion);
                var oldChannelCount = channelMapping.Length;

                Array.Resize(ref channelMapping, newChannelCount);

                for (int i = oldChannelCount; i < newChannelCount; i++)
                    channelMapping[i] = i;

                UpdateListView();
            }
        }

        void MappingListDoubleClicked(PropertyPage props, int propertyIndex, int itemIndex, int columnIndex)
        {
            var sources = new string[16 + trackNames.Length];
            for (int i = 0; i < 16; i++)
                sources[i] = $"Channel {i}";
            for (int i = 0; i < trackNames.Length; i++)
                sources[i + 16] = $"Track {i} ({trackNames[i]})";

            var dlg = new PropertyDialog(300, true, true, dialog);
            dlg.Properties.AddDropDownList("MIDI Source:", sources, sources[channelMapping[itemIndex]]);
            dlg.Properties.Build();

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                channelMapping[itemIndex] = Array.IndexOf(sources, dlg.Properties.GetPropertyValue<string>(0));
                UpdateListView();
            }
        }

        public void UpdateListView()
        {
            var expansion = ExpansionType.GetValueForName(dialog.Properties.GetPropertyValue<string>(0));
            var channels = Channel.GetChannelsForExpansion(expansion);

            Debug.Assert(channelMapping.Length == channels.Length);

            var gridData = new string[channels.Length, 2];

            for (int i = 0; i < channels.Length; i++)
            {
                gridData[i, 0] = ChannelType.Names[channels[i]];

                // >= 16 is a track, otherwise a channel.
                if (channelMapping[i] >= 16)
                {
                    gridData[i, 1] = $"Track {channelMapping[i] - 16} ({trackNames[channelMapping[i] - 16]})";
                }
                else
                {
                    gridData[i, 1] = $"Channel {channelMapping[i]}";
                }
            }

            dialog.Properties.UpdateMultiColumnList(5, gridData);
        }

        public Project ShowDialog(FamiStudioForm parent)
        {
            if (dialog != null && dialog.ShowDialog(parent) == DialogResult.OK)
            {
                return new MidiFile().Load(filename);
            }

            return null;
        }
    }
}
