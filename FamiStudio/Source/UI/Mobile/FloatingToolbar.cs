using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Color = System.Drawing.Color;

using RenderControl     = FamiStudio.GLControl;
using RenderGraphics    = FamiStudio.GLGraphics;
using RenderBitmapAtlas = FamiStudio.GLBitmapAtlas;


namespace FamiStudio
{
    public abstract class FloatingToolbar
    {
        protected class ButtonInfo
        {
            public int    image;
            public float  imageOpacity;
            public int    overlayImage;
            public float  overlayImageOpacity;
            public Color  color;
            public string text;
        };

        protected abstract void GetSubButtonsInfo(out ButtonInfo[] subButtons);
        protected abstract void GetButtonInfo(int index, ref ButtonInfo button);

        protected int x;
        protected int y;
        protected int width;
        protected int height;
        protected int numButtons;
        protected int buttonSize;
        protected string[] atlasImages;
        protected ButtonInfo bi;
        protected RenderControl control;
        protected RenderBitmapAtlas atlas;

        public int Width  => width;
        public int Height => height;
        public int ButtonSize => buttonSize;

        public FloatingToolbar(GLControl parent, int cnt, string[] images)
        {
            control = parent;
            numButtons = cnt;
            atlasImages = images;
            bi = new ButtonInfo();
        }

        public void InitializeGraphics(RenderGraphics g)
        {
            // MATTT : Landscape mode too.
            buttonSize = MobileUtils.ComputeIdealButtonSize(control.ParentFormSize.Width, control.ParentFormSize.Height);
            width = buttonSize;
            height = buttonSize * numButtons;
            atlas = g.CreateBitmapAtlasFromResources(atlasImages);
        }

        public void TerminateGraphics()
        {
            Utils.DisposeAndNullify(ref atlas);
        }

        public void Move(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public void Render(RenderGraphics g)
        {
            var cmd = g.CreateCommandList();

            var pixelSizeX = 1.0f / atlas.Size.Width;
            var pixelSizeY = 1.0f / atlas.Size.Height;

            for (int i = 0; i < numButtons; i++)
            {
                GetButtonInfo(i, ref bi);

                cmd.Transform.PushTranslation(x, y + i * buttonSize);
                cmd.FillRectangle(0, 0, buttonSize, buttonSize, g.GetVerticalGradientBrush(bi.color, buttonSize, 0.8f));

                atlas.GetElementUVs(bi.image, out var u0, out var v0, out var u1, out var v1);
                u0 += pixelSizeX * 1.5f;
                v0 += pixelSizeY * 1.5f;
                u1 -= pixelSizeX * 1.5f;
                v1 -= pixelSizeY * 1.5f;
                cmd.DrawBitmap(atlas, 20, 10, 90, 90, 1.0f, u0, v0, u1, v1);

                //cmd.DrawBitmapAtlas(atlas, bi.image, 20, 0, bi.imageOpacity, buttonSize / 45.0f);
                cmd.DrawText("Instrument 1", control.ThemeResources.FontBigBold, 10, 100, control.ThemeResources.BlackBrush);
                cmd.Transform.PopTransform();
            }

            g.DrawCommandList(cmd);
        }

        public void OnClick(int x, int y)
        {
        }

        public void OnLongClick(int x, int y)
        {
        }

        // Has a width/height (fixed)
        // Can be positioned within parent.
        //
        // When a drawer open, calls back ask for list of images, color + text + user data.
        // When click/long press callback
        // 
        // At each render, will ask for image + text + color of each selected button.
        // 

        // Const : number of buttons, list of all images.
    }

    public class PianoRollFloatingToolbar : FloatingToolbar
    {
        protected static readonly string[] ImageNames = new []
        {
            "MobileInstrument",
            "MobileSnapOn",
            "MobileArpeggio",
            "MobileChannelTriangle",
            "MobileInstrumentFds",
        };

        public PianoRollFloatingToolbar(GLControl parent) : base(parent, 5, ImageNames)
        {

        }

        protected override void GetButtonInfo(int index, ref ButtonInfo button)
        {
            button.image = index;
            button.imageOpacity = 1.0f;
            button.color = Theme.CustomColors[index, 0];
            button.text = "Allo";
        }

        protected override void GetSubButtonsInfo(out ButtonInfo[] subButtons)
        {
            subButtons = null;
        }
    }

    //public class SequencerFloatingToolbar : FloatingToolbar
    //{
    //}
}