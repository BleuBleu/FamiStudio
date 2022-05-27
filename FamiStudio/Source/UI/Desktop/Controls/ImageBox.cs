using System.Drawing;
using System.Collections.Generic;

using RenderBitmapAtlasRef = FamiStudio.GLBitmapAtlasRef;
using RenderBrush          = FamiStudio.GLBrush;
using RenderGeometry       = FamiStudio.GLGeometry;
using RenderControl        = FamiStudio.GLControl;
using RenderGraphics       = FamiStudio.GLGraphics;
using RenderCommandList    = FamiStudio.GLCommandList;
using System.Diagnostics;

namespace FamiStudio
{
    public class ImageBox2 : RenderControl
    {
        private string imageName;
        private RenderBitmapAtlasRef bmp;

        public ImageBox2(string image)
        {
            height = DpiScaling.ScaleForMainWindow(24);
            imageName = image;
        }

        public string Image
        {
            get { return imageName; }
            set { imageName = value; bmp = null; MarkDirty(); }
        }

        protected override void OnRender(RenderGraphics g)
        {
            if (bmp == null)
            {
                bmp = g.GetBitmapAtlasRef(imageName);
                Debug.Assert(bmp != null);
            }

            var c = parentDialog.CommandList;
            c.DrawBitmapAtlasCentered(bmp, 0, 0, width, height);
        }
    }
}
