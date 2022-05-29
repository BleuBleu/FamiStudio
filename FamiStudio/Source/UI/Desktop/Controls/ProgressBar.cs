using System.Diagnostics;

using RenderBitmapAtlasRef = FamiStudio.GLBitmapAtlasRef;
using RenderBrush          = FamiStudio.GLBrush;
using RenderGeometry       = FamiStudio.GLGeometry;
using RenderControl        = FamiStudio.GLControl;
using RenderGraphics       = FamiStudio.GLGraphics;
using RenderCommandList    = FamiStudio.GLCommandList;

namespace FamiStudio
{
    public class ProgressBar2 : RenderControl
    {
        private float progress;
        private float visibleProgress;

        public ProgressBar2()
        {
            height = DpiScaling.ScaleForMainWindow(10);
        }

        public float Progress
        {
            get { return progress; }
            set
            {
                var clampedValue = Utils.Clamp(value, 0.0f, 1.0f);
                if (clampedValue != progress)
                {
                    // Dont do animation when going backwards.
                    if (clampedValue < progress)
                        visibleProgress = clampedValue;
                    progress = clampedValue;
                    MarkDirty();
                }
            }
        }

        public override void Tick(float delta)
        {
            var newVisibleProgress = Utils.Lerp(visibleProgress, progress, 0.1f);
            SetAndMarkDirty(ref visibleProgress, newVisibleProgress);
        }

        protected override void OnRender(RenderGraphics g)
        {
            Debug.Assert(enabled); // TODO : Add support for disabled state.

            var c = parentDialog.CommandList;

            c.FillAndDrawRectangle(0, 0, width - 1, height - 1, ThemeResources.DarkGreyLineBrush1, ThemeResources.LightGreyFillBrush1);
            if (visibleProgress > 0.0f)
                c.FillAndDrawRectangle(0, 0, (visibleProgress * width) - 1, height - 1, ThemeResources.MediumGreyFillBrush1, ThemeResources.LightGreyFillBrush1);
        }
    }
}
