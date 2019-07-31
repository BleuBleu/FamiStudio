using System.Windows.Forms;
using System.Drawing.Drawing2D;

namespace FamiStudio
{
    public class NoInterpolationPictureBox : PictureBox
    {
        protected override void OnPaint(PaintEventArgs paintEventArgs)
        {
            paintEventArgs.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
            paintEventArgs.Graphics.PixelOffsetMode = PixelOffsetMode.Half;
            base.OnPaint(paintEventArgs);
        }
    }
}
