using System;
using System.Collections.Generic;

namespace FamiStudio
{
    public class MultiPropertyDialog : TopBarDialog
    {
        class PropertyPageTab
        {
            public Button button;
            public PropertyDialog dialog;
            public Size windowSize;
            public bool visible = true;
        }

        private bool canAcceptOnTabsPage;
        private int selectedIndex = -1;
        private List<PropertyPageTab> tabs = new List<PropertyPageTab>();
        private TouchScrollContainer scrollContainer;

        public delegate void PageCustomActionActivatedDelegate(int page);
        public event PageCustomActionActivatedDelegate PageCustomActionActivated;
 
        public MultiPropertyDialog(FamiStudioWindow win, string title, int width, bool acceptOnTabsPage = false, int tabsWidth = 150) : base(win, title)
        {
            canAcceptOnTabsPage = acceptOnTabsPage;
            Init();
        }

        private void Init()
        {
            Move(0, 0, ParentContainer.Width, ParentContainer.Height);

            AcceptButtonVisible = canAcceptOnTabsPage;

            scrollContainer = new TouchScrollContainer();
            scrollContainer.Move(clientRect.Left, clientRect.Top, clientRect.Width, clientRect.Height);
            AddControl(scrollContainer);
        }

        public PropertyPage AddPropertyPage(string text, string image, int scroll = -1)
        {
            var tab = new PropertyPageTab();
            tab.dialog = new PropertyDialog(window, text, 0);
            tab.dialog.CancelButtonImage = "Back";
            tab.dialog.DialogClosing += Dialog_Closing;
            tab.windowSize = ParentWindowSize;
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
                else if (result == DialogResult.Cancel)
                {
                    selectedIndex = -1;
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

        public void SetPageCustomAction(int idx, string label)
        {
            tabs[idx].dialog.SetCustomAction(label);
            tabs[idx].dialog.CustomActionActivated += () => PageCustomActionActivated?.Invoke(idx);
        }

        public void SwitchToPage(int idx)
        {
            GoToTab(idx);
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

            scrollContainer.VirtualSizeY = scrollContainer.GetControlsRect().Bottom;
        }

        public int SelectedIndex => selectedIndex;

        private Button AddButton(string text, string image)
        {
            var buttonHeight = DpiScaling.ScaleForWindow(24);

            var btn = new Button(image, text);
            btn.Resize(window.Width, buttonHeight);
            btn.ImageScale = DpiScaling.Window * 0.5f;
            btn.VibrateOnClick = true;
            btn.Border = true;
            btn.Click += Btn_Click;
            return btn;
        }

        public override void OnWindowResize(EventArgs e)
        {
            base.OnWindowResize(e);

            scrollContainer.Resize(width, height);
            
            if (selectedIndex >= 0)
            {
                tabs[selectedIndex].windowSize = ParentWindowSize;
            }

            foreach (var tab in tabs) 
            {
                tab.button.Resize(width, tab.button.Height);
            }

            CenterDialog();
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

            var tab = tabs[selectedIndex];
            var dlg = tab.dialog;
            window.InitDialog(dlg);

            // If we rotated the screen while on another page, need to 
            // refresh the layout at the last minute.
            if (ParentWindowSize != tab.windowSize)
            {
                dlg.OnWindowResize(EventArgs.Empty);
                tab.windowSize = ParentWindowSize;
            }

            dlg.ShowDialogAsync(callback);
        }
    }
}
