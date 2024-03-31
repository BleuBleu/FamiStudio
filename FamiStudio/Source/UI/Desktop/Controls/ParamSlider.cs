using System;
using System.Diagnostics;
using System.Reflection.Metadata;

namespace FamiStudio
{
    public class ParamSlider : Control
    {
        // MATTT : What was that again?
        private float bmpScale = Platform.IsMobile ? DpiScaling.Window * 0.25f : 1.0f;

        private TextureAtlasRef bmpMinus;
        private TextureAtlasRef bmpPlus;

        private Color fillColor     = Color.FromArgb(64, Color.Black);
        private Color disabledColor = Color.FromArgb(64, Color.Black);

        private int buttonSize;
        private int min = 0;
        private int max = 100;
        private int val = 25;
        private float exp = 1.0f;
        private string text = "123"; // MATTT

        public ParamSlider(int value, int minValue, int maxValue)
        {
            val = value;
            min = minValue;
            max = maxValue;
            exp = max >= 4095 ? 4 : 1;
            height = DpiScaling.ScaleForWindow(16);
        }

        protected override void OnAddedToContainer()
        {
            bmpMinus = ParentWindow.Graphics.GetTextureAtlasRef("ButtonMinus");
            bmpPlus  = ParentWindow.Graphics.GetTextureAtlasRef("ButtonPlus");
            buttonSize = DpiScaling.ScaleCustom(bmpMinus.ElementSize.Width, bmpScale);
            height = DpiScaling.ScaleForWindow(buttonSize);
        }

        protected override void OnRender(Graphics g)
        {
            Debug.Assert(enabled); // TODO : Add support for disabled state.

            var c = g.GetCommandList();
            var opacity = 1.0f; // MATTT : Hover + disabled.
            var sliderWidth = width - buttonSize * 2;
            var ratio = (val - min) / (float)(max - min);
            var valWidth = max == min ? 0 : (int)Math.Round(MathF.Pow(ratio, exp) * sliderWidth);
            var buttonOffsetY = Utils.DivideAndRoundUp(height - buttonSize, 2);

            c.DrawTextureAtlas(bmpMinus, 0, buttonOffsetY, bmpScale, Color.Black.Transparent(opacity));
            c.PushTranslation(buttonSize, 0);
            c.FillRectangle(1, 1, valWidth, height, fillColor);
            c.DrawRectangle(0, 0, sliderWidth, height, enabled ? Theme.BlackColor : disabledColor, 1);
            c.DrawText(text, Fonts.FontMedium, 0, 0, enabled ? Theme.BlackColor : disabledColor, TextFlags.MiddleCenter, sliderWidth, height);
            c.PopTransform();
            c.DrawTextureAtlas(bmpPlus, buttonSize + sliderWidth, buttonOffsetY, bmpScale, Color.Black.Transparent(opacity));
        }
    }
}
