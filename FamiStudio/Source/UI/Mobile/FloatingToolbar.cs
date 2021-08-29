using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

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

        protected int drawerX;
        protected int drawerY;
        protected int drawerTargetX;
        protected int drawerTargetY;
        protected int activeButtonIdx = -1;
        protected int nextButtonIdx = -1;

        protected int toolbarX;
        protected int toolbarY;
        protected int width;
        protected int height;
        protected int numButtons;
        protected int buttonSize;

        protected string[] atlasImages;
        protected ButtonInfo bi;
        protected RenderControl control;
        protected RenderBitmapAtlas atlas;

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

        public void UpdateLayout()
        {
            toolbarX = control.Width  - width  - buttonSize / 2;
            toolbarY = control.Height - height - buttonSize / 2;
        }

        public void Tick(float deltaTime)
        {
            if (activeButtonIdx >= 0 && drawerX != drawerTargetX)
            {
                drawerX = (int)Utils.Lerp(drawerX, drawerTargetX, deltaTime * 15);
                if (drawerX <= drawerTargetX + 5)
                    drawerX = drawerTargetX;
            }
        }

        public void Render(RenderGraphics g)
        {
            var cmd = g.CreateCommandList();

            if (activeButtonIdx >= 0)
            {
                var drawerStartX = toolbarX;
                var drawerStartY = toolbarY + activeButtonIdx * buttonSize;

                cmd.FillRectangle(drawerX, drawerY, drawerStartX, drawerStartY + buttonSize, control.ThemeResources.LightRedFillBrush);
            }

            for (int i = 0; i < numButtons; i++)
            {
                GetButtonInfo(i, ref bi);

                cmd.Transform.PushTranslation(toolbarX, toolbarY + i * buttonSize);
                cmd.FillRectangle(0, 0, buttonSize, buttonSize, g.GetVerticalGradientBrush(bi.color, buttonSize, 0.8f));
                cmd.DrawBitmapAtlas(atlas, bi.image, 20, 0, bi.imageOpacity, buttonSize / 90.0f); // MATTT : Figure out the whole scale thing.
                cmd.DrawText("Instrument 1", control.ThemeResources.FontVeryLargeBold, 10, 100, control.ThemeResources.BlackBrush);
                cmd.Transform.PopTransform();
            }

            g.DrawCommandList(cmd);
        }

        private int GetButtonForCoord(int x, int y)
        {
            var idx = ((y - toolbarY) / buttonSize);
            if (idx < 0 || idx >= numButtons)
                idx = -1;
            return idx;
        }

        private void OpenDrawer(int buttonIdx)
        {
            drawerX = toolbarX;
            drawerY = toolbarY + buttonIdx * buttonSize;
            drawerTargetX = buttonSize / 2;
            drawerTargetY = drawerY;
            activeButtonIdx = buttonIdx;
        }

        public bool OnClick(int x, int y)
        {
            var buttonIdx = GetButtonForCoord(x, y);
            if (buttonIdx >= 0)
            {
                OpenDrawer(buttonIdx);
                return true;
            }
            return false;
        }

        public bool OnLongClick(int x, int y)
        {
            return false;
        }

        // When a drawer open, calls back ask for list of images, color + text + user data.
        // When click/long press callback
        // At each render, will ask for image + text + color of each selected button.
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