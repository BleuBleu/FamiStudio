using System;
using RenderControl = FamiStudio.GLControl;

namespace FamiStudio
{
    public class MobilePiano : RenderControl
    {
        public override void Tick(float deltaTime)
        {
        }

        public void HighlightPianoNote(int note)
        {
        }
    }

    public class QuickAccessBar : RenderControl
    {
        public override void Tick(float deltaTime)
        {
        }
    }

    public class MobileProjectDialog
    {
        public MobileProjectDialog(FamiStudio fami, string title, bool save, bool allowStorage = true)
        {
        }

        public void ShowDialogAsync(Action<string> callback)
        {
        }
    }
}
