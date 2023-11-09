using System.Diagnostics;

namespace FamiStudio
{
    public class ImageBox : Control
    {
        private string atlasImageName;
        private Texture bmp;
        private TextureAtlasRef bmpAtlas;
        private Color tint = Color.White;
        private bool scale;
        private bool flip;

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
            set { atlasImageName = value; UpdateAtlasBitmap(); MarkDirty(); }
        }

        public Texture Image
        {
            get { return bmp; }
            set { bmp = value; MarkDirty(); }
        }

        public Color Tint
        {
            get { return tint; }
            set { tint = value; MarkDirty(); }
        }

        public bool ScaleImage
        {
            get { return scale; }
            set { scale = value; MarkDirty(); }
        }

        public bool FlipImage
        {
            get { return flip; }
            set { flip = value; MarkDirty(); }
        }

        private void UpdateAtlasBitmap()
        {
            if (!string.IsNullOrEmpty(atlasImageName))
            {
                bmpAtlas = Graphics.GetTextureAtlasRef(atlasImageName);
                Debug.Assert(bmpAtlas != null);
            }
        }

        protected override void OnAddedToContainer()
        {
            UpdateAtlasBitmap();
        }

        protected override void OnRender(Graphics g)
        {
            Debug.Assert(enabled); // TODO : Add support for disabled state.

            var c = g.GetCommandList();

            if (bmp != null)
            {
                if (scale)
                {
                    c.DrawTextureScaled(bmp, 0, 0, width, height, flip);
                }
                else
                {
                    c.DrawTextureCentered(bmp, 0, 0, width, height, 1, tint);
                }
            }
            else if (!string.IsNullOrEmpty(atlasImageName))
            {
                if (bmpAtlas == null)
                {
                    bmpAtlas = g.GetTextureAtlasRef(atlasImageName);
                    Debug.Assert(bmpAtlas != null);
                }

                c.DrawTextureAtlasCentered(bmpAtlas, 0, 0, width, height, 1, 1, tint);
            }
            else
            {
                c.FillRectangle(0, 0, width, height, Theme.BlackColor);
            }
        }
    }
}
