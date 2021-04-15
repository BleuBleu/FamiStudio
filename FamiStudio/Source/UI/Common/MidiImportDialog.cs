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
        private MidiFileReader.MidiSource[] channelSources = new MidiFileReader.MidiSource[5]
        {
            new MidiFileReader.MidiSource() { index = 0 },
            new MidiFileReader.MidiSource() { index = 1 },
            new MidiFileReader.MidiSource() { index = 2 },
            new MidiFileReader.MidiSource() { index = 9 },
            new MidiFileReader.MidiSource() { type = MidiSourceType.None }
        };

        public MidiImportDialog(string file)
        {
            filename = file;
            trackNames = new MidiFileReader().GetTrackNames(file);

            if (trackNames != null)
            {
                dialog = new PropertyDialog(500);
                dialog.Properties.AddDropDownList("Expansion:", ExpansionType.Names, ExpansionType.Names[0]); // 0
                dialog.Properties.AddDropDownList("Polyphony behavior:", MidiPolyphonyBehavior.Names, MidiPolyphonyBehavior.Names[0]); // 1
                dialog.Properties.AddCheckBox("Use velocity as volume:", true); // 2
                dialog.Properties.AddLabel(null, "Channel mapping (double-click on a row to change)"); // 3
                dialog.Properties.AddMultiColumnList(new[] { "NES Channel        ", "MIDI Source                          " }, null, MappingListDoubleClicked, null); // 4
                dialog.Properties.AddLabel(null, "Disclaimer : The NES cannot play multiple notes on the same channel, any kind of polyphony is not supported. MIDI files must be properly curated. Moreover, blank instruments will be created and will sound nothing like their MIDI counterparts.", true);
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
                var oldChannelCount = channelSources.Length;

                var maxChannelIndex = 3;
                for (int i = 0; i < oldChannelCount; i++)
                {
                    if (channelSources[i].type == MidiSourceType.Channel && channelSources[i].index != 9)
                        maxChannelIndex = Math.Max(maxChannelIndex, channelSources[i].index);
                }

                Array.Resize(ref channelSources, newChannelCount);

                for (int i = oldChannelCount; i < newChannelCount; i++)
                {
                    channelSources[i] = new MidiFileReader.MidiSource() { index = maxChannelIndex++ };
                }

                UpdateListView();
            }
        }

        void MappingListDoubleClicked(PropertyPage props, int propertyIndex, int itemIndex, int columnIndex)
        {
            var src = channelSources[itemIndex];
            var srcNames = GetSourceNames(src.type);
            var allowChannel10Mapping = src.type == MidiSourceType.Channel && src.index == 9;

            var dlg = new PropertyDialog(300, true, true, dialog);
            dlg.Properties.AddDropDownList("Source Type:", MidiSourceType.Names, MidiSourceType.Names[src.type]); // 0
            dlg.Properties.AddDropDownList("Source:", srcNames, srcNames[src.index]); // 1
            dlg.Properties.AddLabel(null, "Channel 10 keys:"); // 2
            dlg.Properties.AddCheckBoxList(null, MidiFileReader.MidiDrumKeyNames, GetSelectedChannel10Keys(src)); // 3
            dlg.Properties.AddButton(null, "Select All",  SelectClicked); // 4
            dlg.Properties.AddButton(null, "Select None", SelectClicked); // 5
            dlg.Properties.Build();
            dlg.Properties.PropertyChanged += MappingProperties_PropertyChanged;
            dlg.Properties.SetPropertyEnabled(3, allowChannel10Mapping);
            dlg.Properties.SetPropertyEnabled(4, allowChannel10Mapping);
            dlg.Properties.SetPropertyEnabled(5, allowChannel10Mapping);

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                var sourceType = MidiSourceType.GetValueForName(dlg.Properties.GetPropertyValue<string>(0));
                var sourceName = dlg.Properties.GetPropertyValue<string>(1);
                var keysBool   = dlg.Properties.GetPropertyValue<bool[]>(3);

                src.type = sourceType;
                src.index = sourceType == MidiSourceType.None ? 0 : (int.Parse(sourceName.Substring(sourceType == MidiSourceType.Track ? 6 : 8)) - 1); // TEMPOTODO : Got a crash here.
                src.keys = 0ul;

                for (int i = 0; i < keysBool.Length; i++)
                {
                    if (keysBool[i])
                        src.keys |= (1ul << i);
                }

                UpdateListView();
            }
        }

        private void SelectClicked(PropertyPage props, int propertyIndex)
        {
            var keys = new bool[MidiFileReader.MidiDrumKeyNames.Length];

            if (propertyIndex == 4)
            {
                for (int i = 0; i < keys.Length; i++)
                    keys[i] = true;
            }

            props.UpdateCheckBoxList(3, keys);
        }

        private void MappingProperties_PropertyChanged(PropertyPage props, int idx, object value)
        {
            var sourceType = MidiSourceType.GetValueForName(props.GetPropertyValue<string>(0));

            if (idx == 0)
                props.UpdateDropDownListItems(1, GetSourceNames(sourceType));

            var allowChannel10Mapping = false;
            if (sourceType == MidiSourceType.Channel)
            {
                var channelIdx = int.Parse(props.GetPropertyValue<string>(1).Substring(8)) - 1;
                allowChannel10Mapping = channelIdx == 9;
            }

            props.SetPropertyEnabled(3, allowChannel10Mapping);
            props.SetPropertyEnabled(4, allowChannel10Mapping);
            props.SetPropertyEnabled(5, allowChannel10Mapping);
        }

        private string[] GetSourceNames(int type)
        {
            if (type == MidiSourceType.Track)
            {
                var sourceNames = new string[trackNames.Length];
                for (int i = 0; i < trackNames.Length; i++)
                    sourceNames[i] = $"Track {i + 1} ({trackNames[i]})";
                return sourceNames;
            }
            else if (type == MidiSourceType.Channel)
            {
                var sourceNames = new string[16];
                for (int i = 0; i < 16; i++)
                    sourceNames[i] = $"Channel {i + 1}";
                return sourceNames;
            }
            else
            {
                return new[] { "N/A" };
            }
        }

        private bool[] GetSelectedChannel10Keys(MidiFileReader.MidiSource source)
        {
            var keys = new bool[MidiFileReader.MidiDrumKeyNames.Length];

            for (int i = 0; i < MidiFileReader.MidiDrumKeyNames.Length; i++)
                keys[i] = (((1ul << i) & source.keys) != 0);

            return keys;
        }

        public void UpdateListView()
        {
            var expansion = ExpansionType.GetValueForName(dialog.Properties.GetPropertyValue<string>(0));
            var channels = Channel.GetChannelsForExpansion(expansion);

            Debug.Assert(channelSources.Length == channels.Length);

            var gridData = new string[channels.Length, 2];

            for (int i = 0; i < channels.Length; i++)
            {
                var src = channelSources[i];

                gridData[i, 0] = ChannelType.Names[channels[i]];

                if (i >= ChannelType.ExpansionAudioStart)
                    gridData[i, 0] += $" ({ExpansionType.ShortNames[expansion]})";

                if (src.type == MidiSourceType.Track)
                {
                    gridData[i, 1] = $"Track {src.index + 1}";

                    if (string.IsNullOrEmpty(trackNames[src.index]))
                        gridData[i, 1] += $" ({trackNames[src.index]})";
                }
                else if (src.type == MidiSourceType.Channel)
                {
                    if (src.index == 9 && src.keys != MidiFileReader.AllDrumKeysMask)
                        gridData[i, 1] = $"Channel {src.index + 1} (Filtered keys)";
                    else
                        gridData[i, 1] = $"Channel {src.index + 1}";
                }
                else
                {
                    gridData[i, 1] = "None";
                }
            }

            dialog.Properties.UpdateMultiColumnList(4, gridData);
        }

        public Project ShowDialog(FamiStudioForm parent)
        {
            if (dialog != null && dialog.ShowDialog(parent) == DialogResult.OK)
            {
                var expansion = dialog.Properties.GetSelectedIndex(0);
                var polyphony = dialog.Properties.GetSelectedIndex(1);
                var velocityAsVolume = dialog.Properties.GetPropertyValue<bool>(2);

                return new MidiFileReader().Load(filename, expansion, channelSources, velocityAsVolume, polyphony);
            }

            return null;
        }
    }
}
