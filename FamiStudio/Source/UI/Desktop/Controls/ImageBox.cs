using System.Diagnostics;

namespace FamiStudio
{
    public class ImageBox : Control
    {
        private string atlasImageName;
        private Bitmap bmp;
        private BitmapAtlasRef bmpAtlas;

        public ImageBox(string image)
        {
            height = DpiScaling.ScaleForMainWindow(24);
            atlasImageName = image;
        }

        public ImageBox(Bitmap b)
        {
            height = DpiScaling.ScaleForMainWindow(24);
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

        protected override void OnRender(Graphics g)
        {
            Debug.Assert(enabled); // TODO : Add support for disabled state.

            var c = parentDialog.CommandList;

            if (bmp != null)
            {
                c.DrawBitmapCentered(bmp, 0, 0, width, height);
            }
            else
            {
                if (bmpAtlas == null)
                {
                    bmpAtlas = g.GetBitmapAtlasRef(atlasImageName);
                    Debug.Assert(bmpAtlas != null);
                }

                c.DrawBitmapAtlasCentered(bmpAtlas, 0, 0, width, height);
            }
        }
    }
}
