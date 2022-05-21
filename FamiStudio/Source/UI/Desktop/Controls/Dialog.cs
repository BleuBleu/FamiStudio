using System.Drawing;
using System.Collections.Generic;

using RenderBitmapAtlas = FamiStudio.GLBitmapAtlas;
using RenderBrush       = FamiStudio.GLBrush;
using RenderGeometry    = FamiStudio.GLGeometry;
using RenderControl     = FamiStudio.GLControl;
using RenderGraphics    = FamiStudio.GLGraphics;
using RenderCommandList = FamiStudio.GLCommandList;

namespace FamiStudio
{
    public class Dialog : RenderControl
    {
        private List<RenderControl> controls = new List<RenderControl>();

        protected override void OnRender(RenderGraphics g)
        {
            g.BeginDrawDialog();

            // Render child controls
            foreach (var ctrl in controls)
            {
                g.BeginDrawControl(ctrl.Rectangle, g.WindowSizeY);
                g.Transform.PushTranslation(left, top);
                ctrl.Render(g);
                g.Transform.PopTransform();
                g.EndDrawControl();
            }

            g.EndDrawDialog(Color.Red);
        }
    }
}
