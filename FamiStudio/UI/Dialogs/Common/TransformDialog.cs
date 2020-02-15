using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FamiStudio
{
    class TransformDialog
    {
        enum TransformSection
        {
            Cleanup,
            Tempo,
            Max
        };

        readonly string[] ConfigSectionNames =
        {
            "Cleanup",
            "Tempo",
            ""
        };

        private PropertyPage[] pages = new PropertyPage[(int)TransformSection.Max];
        private MultiPropertyDialog dialog;

        public unsafe TransformDialog(Rectangle mainWinRect)
        {
            int width = 450;
            int height = 300;
            int x = mainWinRect.Left + (mainWinRect.Width - width) / 2;
            int y = mainWinRect.Top + (mainWinRect.Height - height) / 2;

            this.dialog = new MultiPropertyDialog(x, y, width, height);

            for (int i = 0; i < (int)TransformSection.Max; i++)
            {
                var section = (TransformSection)i;
                var page = dialog.AddPropertyPage(ConfigSectionNames[i], "ExportNsf"); // MATTT
                CreatePropertyPage(page, section);
            }
        }

        private PropertyPage CreatePropertyPage(PropertyPage page, TransformSection section)
        {
            switch (section)
            {
                case TransformSection.Cleanup:
                {
                    break;
                }
                case TransformSection.Tempo:
                {
                    break;
                }
            }

            page.Build();
            pages[(int)section] = page;

            return page;
        }

        public DialogResult ShowDialog()
        {
            var dialogResult = dialog.ShowDialog();

            if (dialogResult == DialogResult.OK)
            {

            }

            return dialogResult;
        }
    }
}
