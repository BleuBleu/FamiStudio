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
            height = DpiScaling.ScaleForDialog(24);
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

            var c = g.CreateCommandList(GLGraphicsBase.CommandListUsage.Dialog);
            var bmpSize = bmp.ElementSize;

            c.DrawBitmapAtlas(bmp, (width - bmpSize.Width) / 2, (height - bmpSize.Height) / 2);
        }
    }
}
