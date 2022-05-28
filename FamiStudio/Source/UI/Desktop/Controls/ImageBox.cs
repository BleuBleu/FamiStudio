using System.Drawing;
using System.Collections.Generic;
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
        private string imageName;
        private RenderBitmap bmp;
        private RenderBitmapAtlasRef bmpAtlas;

        public ImageBox2(string image)
        {
            height = DpiScaling.ScaleForMainWindow(24);
            imageName = image;
        }

        public ImageBox2(RenderBitmap b)
        {
            height = DpiScaling.ScaleForMainWindow(24);
            bmp = b;
        }

        public string Image
        {
            get { return imageName; }
            set { imageName = value; bmpAtlas = null; MarkDirty(); }
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
                c.FillRectangle(0, 0, width, height, ThemeResources.BlackBrush); // MATTT

                if (bmpAtlas == null)
                {
                    bmpAtlas = g.GetBitmapAtlasRef(imageName);
                    Debug.Assert(bmpAtlas != null);
                }

                c.DrawBitmapAtlasCentered(bmpAtlas, 0, 0, width, height);
            }
        }
    }
}
