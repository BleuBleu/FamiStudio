using System.Diagnostics;

using RenderBitmapAtlasRef = FamiStudio.GLBitmapAtlasRef;
using RenderBitmap         = FamiStudio.GLBitmap;
using RenderBrush          = FamiStudio.GLBrush;
using RenderGeometry       = FamiStudio.GLGeometry;
using RenderControl        = FamiStudio.GLControl;
using RenderGraphics       = FamiStudio.GLGraphics;
using RenderCommandList    = FamiStudio.GLCommandList;

namespace FamiStudio
{
    public class ImageBox2 : RenderControl
    {
        private string atlasImageName;
        private RenderBitmap bmp;
        private RenderBitmapAtlasRef bmpAtlas;

        public ImageBox2(string image)
        {
            height = DpiScaling.ScaleForMainWindow(24);
            atlasImageName = image;
        }

        public ImageBox2(RenderBitmap b)
        {
            height = DpiScaling.ScaleForMainWindow(24);
            bmp = b;
        }

        public string AtlasImageName
        {
            get { return atlasImageName; }
            set { atlasImageName = value; bmpAtlas = null; MarkDirty(); }
        }

        public RenderBitmap Image
        {
            get { return bmp; }
            set { bmp = value; MarkDirty(); }
        }

        protected override void OnRender(RenderGraphics g)
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
