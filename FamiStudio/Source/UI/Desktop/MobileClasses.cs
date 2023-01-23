using System;

namespace FamiStudio
{
    public class MobilePiano : Control
    {
        public MobilePiano(FamiStudioWindow win) // CTRLTODO : base(win)
        {
        }

        public override void Tick(float deltaTime)
        {
        }

        public void HighlightPianoNote(int note)
        {
        }
    }

    public class QuickAccessBar : Control
    {
        public QuickAccessBar(FamiStudioWindow win) // CTRLTODO : base(win)
        {
        }

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
