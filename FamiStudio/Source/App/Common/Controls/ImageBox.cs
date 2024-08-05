using System.Diagnostics;

namespace FamiStudio
{
    public class ImageBox : Control
    {
        protected string atlasImageName;
        protected Texture bmp;
        protected TextureAtlasRef bmpAtlas;
        protected Color tint = Color.White;
        protected float imageScale = 1.0f;
        protected bool stretch;
        protected bool flip;
        protected bool whiteHighlight;

        public ImageBox(string image)
        {
            height = DpiScaling.ScaleForWindow(24);
            atlasImageName = image;
        }

        public ImageBox(Texture b)
        {
            height = DpiScaling.ScaleForWindow(24);
            bmp = b;
        }

        public string AtlasImageName
        {
            get { return atlasImageName; }
            set { if (SetAndMarkDirty(ref atlasImageName, value)) UpdateAtlasBitmap(); }
        }

        public Texture Image
        {
            get { return bmp; }
            set { bmp = value; MarkDirty(); }
        }

        public float ImageScale
        {
            get { return imageScale; }
            set { SetAndMarkDirty(ref imageScale, value); }
        }

        public Color Tint
        {
            get { return tint; }
            set { SetAndMarkDirty(ref tint, value); }
        }

        public bool StretchImageToFill
        {
            get { return stretch; }
            set { SetAndMarkDirty(ref stretch, value); }
        }

        public bool FlipImage
        {
            get { return flip; }
            set { SetAndMarkDirty(ref flip, value); }
        }

        public bool WhiteHighlight
        {
            get { return whiteHighlight; }
            set { SetAndMarkDirty(ref whiteHighlight, value); }
        }

        private void UpdateAtlasBitmap()
        {
            if (!string.IsNullOrEmpty(atlasImageName))
            {
                bmpAtlas = Graphics.GetTextureAtlasRef(atlasImageName);
                Debug.Assert(bmpAtlas != null);
            }
        }

        public void AutoSizeToImage()
        {
            Debug.Assert(bmpAtlas != null);
            Resize(
                DpiScaling.ScaleCustom(bmpAtlas.ElementSize.Width, imageScale),
                DpiScaling.ScaleCustom(bmpAtlas.ElementSize.Height, imageScale));
        }

        protected override void OnAddedToContainer()
        {
            UpdateAtlasBitmap();
        }

        protected override void OnRender(Graphics g)
        {
            Debug.Assert(enabled); // TODO : Add support for disabled state.

            var c = g.GetCommandList();

            if (whiteHighlight)
            {
                c.DrawRectangle(ClientRectangle, Theme.WhiteColor, 3, true, true);
            }

            if (bmp != null)
            {
                if (stretch)
                {
                    c.DrawTextureScaled(bmp, 0, 0, width, height, flip);
                }
                else
                {
                    c.DrawTextureCentered(bmp, 0, 0, width, height, tint);
                }
            }
            else if (!string.IsNullOrEmpty(atlasImageName))
            {
                if (bmpAtlas == null)
                {
                    bmpAtlas = g.GetTextureAtlasRef(atlasImageName);
                    Debug.Assert(bmpAtlas != null);
                }

                c.DrawTextureAtlas(bmpAtlas, 
                    (width  - DpiScaling.ScaleCustom(bmpAtlas.ElementSize.Width,  imageScale)) / 2, 
                    (height - DpiScaling.ScaleCustom(bmpAtlas.ElementSize.Height, imageScale)) / 2, imageScale, tint);
            }
            else
            {
                c.FillRectangle(0, 0, width, height, Theme.BlackColor);
            }
        }
    }
}
