using System;

namespace FamiStudio
{
    public class MobilePiano : Container
    {
        public void HighlightPianoNote(int note)
        {
        }
    }

    public class QuickAccessBar : Container
    {
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
