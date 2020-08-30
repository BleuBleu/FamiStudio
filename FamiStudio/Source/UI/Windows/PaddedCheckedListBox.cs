using System.Windows.Forms;

namespace FamiStudio
{
    public class PaddedCheckedListBox : CheckedListBox
    {
        public PaddedCheckedListBox()
        {
        }

        public override int ItemHeight
        {
            get => base.ItemHeight + 4;
            set => base.ItemHeight = value;
        }
    }
}
