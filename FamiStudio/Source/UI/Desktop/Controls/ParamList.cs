using System;
using System.Diagnostics;
using System.Reflection.Metadata;

namespace FamiStudio
{
    public class ParamList : Control
    {
        // MATTT : What was that again?
        private float bmpScale = Platform.IsMobile ? DpiScaling.Window * 0.25f : 1.0f;

        private TextureAtlasRef bmpLeft;
        private TextureAtlasRef bmpRight;

        private Color fillColor = Color.FromArgb(64, Color.Black);
        private Color disabledColor = Color.FromArgb(64, Color.Black);

        private int buttonSizeX;
        private int buttonSizeY;
        private ParamInfo param;

        public ParamList(ParamInfo p)
        {
            param = p;
            height = DpiScaling.ScaleForWindow(16);
        }

        protected override void OnAddedToContainer()
        {
            bmpLeft = ParentWindow.Graphics.GetTextureAtlasRef("ButtonLeft");
            bmpRight = ParentWindow.Graphics.GetTextureAtlasRef("ButtonRight");
            buttonSizeX = DpiScaling.ScaleCustom(bmpLeft.ElementSize.Width, bmpScale);
            buttonSizeY = DpiScaling.ScaleCustom(bmpLeft.ElementSize.Height, bmpScale);
            height = DpiScaling.ScaleForWindow(buttonSizeY);
        }

        protected override void OnRender(Graphics g)
        {
            Debug.Assert(enabled); // TODO : Add support for disabled state.

            var c = g.DefaultCommandList;
            var opacity = 1.0f; // MATTT : Hover + disabled.
            var sliderWidth = width - buttonSizeX * 2;
            var buttonOffsetY = Utils.DivideAndRoundUp(height - buttonSizeY, 2);

            c.DrawTextureAtlas(bmpLeft, 0, buttonOffsetY, bmpScale, Color.Black.Transparent(opacity));
            c.PushTranslation(buttonSizeX, 0);
            c.DrawText(param.GetValueString(), Fonts.FontMedium, 0, 0, enabled ? Theme.BlackColor : disabledColor, TextFlags.MiddleCenter, sliderWidth, height);
            c.PopTransform();
            c.DrawTextureAtlas(bmpRight, buttonSizeX + sliderWidth, buttonOffsetY, bmpScale, Color.Black.Transparent(opacity));
        }
    }
}
