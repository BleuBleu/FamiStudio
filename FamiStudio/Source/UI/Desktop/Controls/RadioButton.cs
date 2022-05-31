using System;
using System.Drawing;
using System.Diagnostics;

namespace FamiStudio
{
    public class RadioButton : Label
    {
        private bool check;
        private bool hover;
        private BitmapAtlasRef bmpRadioOff;
        private BitmapAtlasRef bmpRadioOn;

        public RadioButton(string txt, bool chk, bool multi = false) : base(txt, multi)
        {
            check = chk;
        }

        public bool Checked
        {
            get { return check; }
            set { SetAndMarkDirty(ref check, value); }
        }

        protected override void OnRenderInitialized(Graphics g)
        {
            bmpRadioOff = g.GetBitmapAtlasRef("RadioButtonOff");
            bmpRadioOn  = g.GetBitmapAtlasRef("RadioButtonOn");
            labelOffsetX = bmpRadioOff.ElementSize.Width + ScaleForMainWindow(8);
        }

        private Rectangle GetRadioRectangle()
        {
            return new Rectangle(0, (height - bmpRadioOff.ElementSize.Height) / 2, bmpRadioOff.ElementSize.Width, bmpRadioOff.ElementSize.Height);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            SetAndMarkDirty(ref hover, true /*GetRadioRectangle().Contains(e.X, e.Y)*/);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            SetAndMarkDirty(ref hover, false);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            //if (GetRadioRectangle().Contains(e.X, e.Y))
            {
                Checked = true;

                foreach (var ctrl in parentDialog.Controls)
                {
                    if (ctrl != this && ctrl is RadioButton radio)
                        radio.Checked = false;
                }
            }
        }

        protected override void OnRender(Graphics g)
        {
            Debug.Assert(enabled); // TODO : Add support for disabled state.

            base.OnRender(g);

            var c = parentDialog.CommandList;
            c.DrawBitmapAtlasCentered(check ? bmpRadioOn : bmpRadioOff, 0, 0, bmpRadioOn.ElementSize.Width, height, 1, 1, hover ? Theme.LightGreyFillColor2 : Theme.LightGreyFillColor1);
        }
    }
}
