using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FamiStudio
{
    public partial class LabelTrackBar : UserControl
    {
        public TrackBar TrackBar => trackBar;
        public Label    Label    => label;

        public LabelTrackBar()
        {
            InitializeComponent();
        }
    }
}
