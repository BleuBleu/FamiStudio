using System;
using System.Drawing;
using System.Windows.Forms;

namespace FamiStudio
{
    public partial class AutoScrollTextBox : TextBox
    {
        private bool showScrollBar = false;

        public AutoScrollTextBox()
        {
            CheckForScrollbars();
        }

        private void CheckForScrollbars()
        {
            bool showScrollBar = false;
            int padding = (this.BorderStyle == BorderStyle.Fixed3D) ? 14 : 10;

            using (Graphics g = this.CreateGraphics())
            {
                // Calcualte the size of the text area.
                SizeF textArea = g.MeasureString(this.Text,
                                                 this.Font,
                                                 this.Bounds.Width - padding);

                if (this.Text.EndsWith(Environment.NewLine))
                {
                    // Include the height of a trailing new line in the height calculation        
                    textArea.Height += g.MeasureString("A", this.Font).Height;
                }

                // Show the vertical ScrollBar if the text area
                // is taller than the control.
                showScrollBar = (Math.Ceiling(textArea.Height) >= (this.Bounds.Height - padding));

                if (showScrollBar != this.showScrollBar)
                {
                    this.showScrollBar = showScrollBar;
                    this.ScrollBars = showScrollBar ? ScrollBars.Vertical : ScrollBars.None;
                }
            }
        }

        protected override void OnTextChanged(EventArgs e)
        {
            CheckForScrollbars();
            base.OnTextChanged(e);
        }

        protected override void OnResize(EventArgs e)
        {
            CheckForScrollbars();
            base.OnResize(e);
        }
    }
}
