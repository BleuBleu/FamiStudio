using System;
using System.Collections.Generic;

namespace FamiStudio
{
    public class MultiPropertyDialog : Dialog
    {
        class PropertyPageTab
        {
            public Button button;
            public PropertyDialog dialog;
            public bool visible = true;
        }

        private int selectedIndex = -1;
        private List<PropertyPageTab> tabs = new List<PropertyPageTab>();
        private TouchScrollContainer scrollContainer;

        public delegate void PageChangingDelegate(int oldPage, int newPage);
        public delegate void CustomVerbActivatedDelegate();
        public delegate void AppSuspendedDelegate();
        public event PageChangingDelegate PageChanging;
        public event CustomVerbActivatedDelegate CustomVerbActivated;
        public event AppSuspendedDelegate AppSuspended;

        public MultiPropertyDialog(FamiStudioWindow win, string title, int width, int tabsWidth = 150) : base(win, title)
        {
            Init();
        }

        public void SetVerb(string text, bool showOnTabPage = false)
        {
        }

        private void Init()
        {
            Move(0, 0, ParentWindow.Width, ParentWindow.Height);

            scrollContainer = new TouchScrollContainer();
            scrollContainer.Move(dialogRect.Left, dialogRect.Top, dialogRect.Width, dialogRect.Height);
            AddControl(scrollContainer);
        }

        public PropertyPage AddPropertyPage(string text, string image, int scroll = -1)
        {
            // MATTT : Add a "back arrow" mode to the PropertyDialog.
            var tab = new PropertyPageTab();
            tab.dialog = new PropertyDialog(window, text, 0);
            tab.dialog.DialogClosing += Dialog_Closing;
            tab.button = AddButton(text, image);
            tabs.Add(tab);

            return tab.dialog.Properties;
        }

        private void Dialog_Closing(Control sender, DialogResult result, ref int numDialogToPop)
        {
            if (sender is Dialog dlg)
            {
                // Pop 2 dialogs to go back to the app directly when applying.
                if (result == DialogResult.OK || 
                    result == DialogResult.Yes)
                {
                    numDialogToPop = 2;
                }
            }
        }

        public PropertyPage GetPropertyPage(int idx)
        {
            return tabs[idx].dialog.Properties;
        }

        public void SetPageVisible(int idx, bool visible)
        {
            tabs[idx].visible = visible;
        }

        public void SetPageBackPage(int idx, int backIdx)
        {
        }

        public void AddPageCustomVerb(int idx, string verb)
        {
        }

        public void SwitchToPage(int idx)
        {
        }

        protected override void OnShowDialog()
        {
            var numVisibleButtons = 0;

            for (var i = 0; i < tabs.Count; i++) 
            {
                var tab = tabs[i];
                if (tab.visible)
                {
                    tab.button.Move(0, numVisibleButtons * tab.button.Height);
                    scrollContainer.AddControl(tab.button);
                    numVisibleButtons++;
                }
            }

            // MATTT : Test scrolling!
            scrollContainer.VirtualSizeY = tabs.Count * numVisibleButtons;
        }

        public int SelectedIndex => selectedIndex;

        private Button AddButton(string text, string image)
        {
            var buttonHeight = DpiScaling.ScaleForWindow(24);

            var btn = new Button(image, text);
            btn.Resize(window.Width, buttonHeight);
            btn.ImageScale = DpiScaling.Window * 0.5f;
            btn.Border = true;
            btn.Click += Btn_Click;
            return btn;
        }

        private void Btn_Click(Control sender)
        {
            for (int i = 0; i < tabs.Count; i++)
            {
                if (tabs[i].button == sender)
                {
                    GoToTab(i);
                    break;
                }
            }
        }

        private void GoToTab(int idx)
        {
            selectedIndex = idx;
            window.InitDialog(tabs[selectedIndex].dialog);
            tabs[selectedIndex].dialog.ShowDialogAsync(callback);
        }
    }
}
