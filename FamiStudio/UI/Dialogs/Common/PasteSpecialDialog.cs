using System.Drawing;
using System.Windows.Forms;

namespace FamiStudio
{
    class PasteSpecialDialog
    {
        private PropertyDialog dialog;

        public unsafe PasteSpecialDialog(Rectangle mainWinRect)
        {
            int width  = 200;
            int height = 300;
            int x = mainWinRect.Left + (mainWinRect.Width  - width)  / 2;
            int y = mainWinRect.Top  + (mainWinRect.Height - height) / 2;

            dialog = new PropertyDialog(x, y, width, height);
            dialog.Properties.AddLabelBoolean("Paste Notes", true);
            dialog.Properties.AddLabelBoolean("Paste Volumes", true);
            dialog.Properties.AddLabelBoolean("Paste Effects", true);
            dialog.Properties.AddLabelBoolean("Mix With Existing Notes", false);
            dialog.Properties.Build();
        }

        public DialogResult ShowDialog()
        {
            return dialog.ShowDialog();
        }

        public bool PasteNotes   => dialog.Properties.GetPropertyValue<bool>(0);
        public bool PasteVolumes => dialog.Properties.GetPropertyValue<bool>(1);
        public bool PasteEffects => dialog.Properties.GetPropertyValue<bool>(2);
        public bool PasteMix     => dialog.Properties.GetPropertyValue<bool>(3);
    }
}
