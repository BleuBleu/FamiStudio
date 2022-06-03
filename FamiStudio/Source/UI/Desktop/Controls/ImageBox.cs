using System.Diagnostics;
using System.Drawing;

namespace FamiStudio
{
    public class ImageBox : Control
    {
        private string atlasImageName;
        private Bitmap bmp;
        private BitmapAtlasRef bmpAtlas;
        private Color tint = Color.White;

        public ImageBox(string image)
        {
            height = DpiScaling.ScaleForWindow(24);
            atlasImageName = image;
        }

        public ImageBox(Bitmap b)
        {
            height = DpiScaling.ScaleForWindow(24);
            bmp = b;
        }

        public string AtlasImageName
        {
            get { return atlasImageName; }
            set { atlasImageName = value; bmpAtlas = null; MarkDirty(); }
        }

        public Bitmap Image
        {
            get { return bmp; }
            set { bmp = value; MarkDirty(); }
        }

        public Color Tint
        {
            get { return tint; }
            set { tint = value; MarkDirty(); }
        }

        protected override void OnRender(Graphics g)
        {
            Debug.Assert(enabled); // TODO : Add support for disabled state.

            var c = parentDialog.CommandList;

            if (bmp != null)
            {
                c.DrawBitmapCentered(bmp, 0, 0, width, height, 1, tint);
            }
            else
            {
                if (bmpAtlas == null)
                {
                    bmpAtlas = g.GetBitmapAtlasRef(atlasImageName);
                    Debug.Assert(bmpAtlas != null);
                }

                c.DrawBitmapAtlasCentered(bmpAtlas, 0, 0, width, height, 1, 1, tint);
            }
        }
    }
}
