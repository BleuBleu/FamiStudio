using System.Drawing;
using System.Windows.Forms;

namespace FamiStudio
{
    class ConfigDialog
    {
        enum ConfigSection
        {
            UserInterface,
            MIDI,
            Max
        };

        readonly string[] ConfigSectionNames =
        {
            "Interface",
            "MIDI",
            ""
        };

        MultiPropertyDialog dialog;

        public unsafe ConfigDialog(Rectangle mainWinRect)
        {
            int width  = 450;
            int height = 375;
            int x = mainWinRect.Left + (mainWinRect.Width  - width)  / 2;
            int y = mainWinRect.Top  + (mainWinRect.Height - height) / 2;

            this.dialog = new MultiPropertyDialog(x, y, width, height);

            for (int i = 0; i < (int)ConfigSection.Max; i++)
            {
                var section = (ConfigSection)i;
                var page = dialog.AddPropertyPage(ConfigSectionNames[i], /*"Config" + section.ToString()*/ "ExportWav");
                CreatePropertyPage(page, section);
            }
        }

        private PropertyPage CreatePropertyPage(PropertyPage page, ConfigSection section)
        {
            switch (section)
            {
                case ConfigSection.UserInterface:
                    page.AddStringList("Sample Rate :", new[] { "11025", "22050", "44100", "48000" }, "44100"); 
                    break;
                case ConfigSection.MIDI:
                    page.AddString("Device :", "Allo", 31); 
                    break;
            }

            page.Build();
            //page.PropertyChanged += Page_PropertyChanged;

            return page;
        }

        public void ShowDialog()
        {
            if (dialog.ShowDialog() == DialogResult.OK)
            {
            }
        }
    }
}
