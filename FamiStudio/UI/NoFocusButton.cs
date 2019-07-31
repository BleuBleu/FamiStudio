using System.Windows.Forms;

namespace FamiStudio
{
    public class NoFocusButton : Button
    {
        public NoFocusButton()
        {
            SetStyle(ControlStyles.Selectable, false);
        }
    }
}
