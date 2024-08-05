using System;
using System.Diagnostics;

namespace FamiStudio
{
    public class RadioButton : Label
    {
        public delegate void RadioChangedDelegate(Control sender, int index);
        public event RadioChangedDelegate RadioChanged;

        private bool check;
        private bool hover;
        private float imageScale = Platform.IsDesktop ? 1.0f : DpiScaling.ScaleForWindowFloat(0.25f);
        private int scaledRadioSize;
        private TextureAtlasRef bmpRadioOff;
        private TextureAtlasRef bmpRadioOn;

        public RadioButton(string txt, bool chk, bool multi = false) : base(txt, multi)
        {
            check = chk;
            height = DpiScaling.ScaleForWindow(Platform.IsMobile ? 16 : 24);
        }

        public bool Checked
        {
            get { return check; }
            set { SetAndMarkDirty(ref check, value); }
        }

        protected override void OnAddedToContainer()
        {
            var g = ParentWindow.Graphics;
            bmpRadioOff = g.GetTextureAtlasRef("RadioButtonOff");
            bmpRadioOn  = g.GetTextureAtlasRef("RadioButtonOn");
            scaledRadioSize = DpiScaling.ScaleCustom(bmpRadioOn.ElementSize.Height, imageScale);
            labelOffsetX = scaledRadioSize + DpiScaling.ScaleForWindow(4);
            base.OnAddedToContainer();
        }

        protected override void OnPointerMove(PointerEventArgs e)
        {
            SetAndMarkDirty(ref hover, Platform.IsDesktop);
        }

        protected override void OnPointerLeave(EventArgs e)
        {
            SetAndMarkDirty(ref hover, false);
        }

        protected override void OnPointerDown(PointerEventArgs e)
        {
            if (!e.IsTouchEvent)
            {
                ToggleChecked();
            }
        }

        protected override void OnTouchClick(PointerEventArgs e)
        {
            ToggleChecked();
        }

        private void ToggleChecked()
        {
            if (!Checked)
            {
                var index = 0;
                var count = 0;

                Checked = true;

                foreach (var ctrl in container.Controls)
                {
                    if (ctrl is RadioButton radio)
                    {
                        if (ctrl == this)
                        {
                            index = count;
                        }
                        else
                        {
                            radio.Checked = false;
                        }

                        count++;
                    }
                }

                RadioChanged?.Invoke(this, index);
            }
        }

        protected override void OnRender(Graphics g)
        {
            Debug.Assert(enabled); // TODO : Add support for disabled state.

            base.OnRender(g);

            var c = g.GetCommandList();
            var baseY = (height - scaledRadioSize) / 2;

            c.DrawTextureAtlas(check ? bmpRadioOn : bmpRadioOff, 0, baseY, imageScale, hover ? Theme.LightGreyColor2 : Theme.LightGreyColor1);
        }
    }
}
