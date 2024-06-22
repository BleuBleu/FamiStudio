using System.Diagnostics;
using System.Globalization;

namespace FamiStudio
{
    public class FileTextBox : TextBox
    {
        public delegate void ClickDelegate(Control sender);
        public event ClickDelegate Click;

        private TextureAtlasRef bmp;
        private bool hoverButton;
        private bool captureButton;

        protected int buttonWidth   = DpiScaling.ScaleForWindow(24);
        protected int textBoxMargin = DpiScaling.ScaleForWindow(2);

        public FileTextBox(string txt, int maxLength) : base(txt, maxLength)
        {
        }

        protected override void OnAddedToContainer()
        {
            outerMarginRight = GetButtonRect().Width + textBoxMargin;

            var g = ParentWindow.Graphics;
            bmp = g.GetTextureAtlasRef("FileFolder");

            // "outerMargin" needs to be set before calling this.
            base.OnAddedToContainer();
        }

        private Rectangle GetButtonRect()
        {
            return new Rectangle(width - buttonWidth - 1, 0, buttonWidth, height - 1);
        }

        private bool IsPointInButton(int x, int y)
        {
            return enabled && GetButtonRect().Contains(x, y);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Left && IsPointInButton(e.X, e.Y))
            {
                captureButton = true;
                Click?.Invoke(this);
            }
            else
            {
                base.OnMouseDown(e);
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            captureButton = false;
            base.OnMouseUp(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            SetAndMarkDirty(ref hoverButton, IsPointInButton(e.X, e.Y));
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(System.EventArgs e)
        {
            SetAndMarkDirty(ref hoverButton, false);
            base.OnMouseLeave(e);
        }

        protected override void OnRender(Graphics g)
        {
            base.OnRender(g);

            var c = g.GetCommandList();
            var color = enabled ? Theme.LightGreyColor1 : Theme.MediumGreyColor1;
            var rect = GetButtonRect();

            var fillBrush = enabled && captureButton ? Theme.MediumGreyColor1 :
                            enabled && hoverButton   ? Theme.DarkGreyColor6 :
                                                       Theme.DarkGreyColor5;

            c.FillAndDrawRectangle(rect, fillBrush, color);
            c.PushTranslation(0, captureButton ? 1 : 0);
            c.DrawTextureAtlasCentered(bmp, rect, 1, color);
            c.PopTransform();
        }
    }
}
