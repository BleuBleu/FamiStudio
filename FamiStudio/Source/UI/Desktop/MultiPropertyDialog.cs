using System;
using System.Collections.Generic;

namespace FamiStudio
{
    public class MultiPropertyDialog : Dialog
    {
        class PropertyPageTab
        {
            public Button button;
            public PropertyPage properties;
            public bool visible = true;
        }

        private int selectedIndex = 0;
        private List<PropertyPageTab> tabs = new List<PropertyPageTab>();
        private int margin = DpiScaling.ScaleForWindow(8);
        private int tabsSizeX;
        private int tabsSizeY = DpiScaling.ScaleForWindow(32);

        private Button buttonYes;
        private Button buttonNo;

        public delegate void PageChangingDelegate(int oldPage, int newPage);
        public delegate void CustomVerbActivatedDelegate();
        public event PageChangingDelegate PageChanging;
        public event CustomVerbActivatedDelegate CustomVerbActivated;

        public MultiPropertyDialog(FamiStudioWindow win, string title, int width, int tabsWidth = 150) : base(win, title)
        {
            tabsSizeX = DpiScaling.ScaleForWindow(tabsWidth);
            Move(0, 0, DpiScaling.ScaleForWindow(width), DpiScaling.ScaleForWindow(width));
            Init();
        }

        public void SetVerb(string text, bool showOnTabPage = false)
        {
        }

        private void Init()
        {
            buttonYes = new Button("Yes", null);
            buttonYes.Click += ButtonYes_Click;
            buttonYes.ToolTip = "Accept";
            buttonYes.Resize(DpiScaling.ScaleForWindow(36), DpiScaling.ScaleForWindow(36));

            buttonNo = new Button("No", null);
            buttonNo.Click += ButtonNo_Click;
            buttonNo.ToolTip = "Cancel";
            buttonNo.Resize(DpiScaling.ScaleForWindow(36), DpiScaling.ScaleForWindow(36));

            AddControl(buttonYes);
            AddControl(buttonNo);
        }

        public PropertyPage AddPropertyPage(string text, string image)
        {
            var page = new PropertyPage(this, tabsSizeX + margin * 2, margin + titleBarSizeY, width - tabsSizeX - margin * 3);

            var tab = new PropertyPageTab();
            tab.button = AddButton(text, image);
            tab.properties = page;
            tabs.Add(tab);

            return page;
        }

        protected override void OnShowDialog()
        {
            var y = margin + titleBarSizeY;
            var maxHeight = 0;
            for (int i = 0; i < tabs.Count; i++)
            {
                var tab = tabs[i];
                tab.button.Visible = tab.visible;
                if (tab.visible)
                {
                    tab.button.Move(margin, y);
                    y += tab.button.Height;
                    maxHeight = Math.Max(maxHeight, tabs[i].properties.LayoutHeight);
                }
            }

            maxHeight = Math.Max(maxHeight, Width / 2);

            Resize(width, maxHeight + buttonNo.Height + margin * 3 + titleBarSizeY);

            var buttonY = maxHeight + margin * 2 + titleBarSizeY;
            buttonYes.Move(Width - buttonYes.Width * 2 - margin * 2, buttonY);
            buttonNo.Move(Width - buttonNo.Width - margin, buttonY);

            SetSelectedTab(0);
            CenterToWindow();
        }
        
        public PropertyPage GetPropertyPage(int idx)
        {
            return tabs[idx].properties;
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

        public int SelectedIndex => selectedIndex;

        private Button AddButton(string text, string image)
        {
            var btn = new Button(image, text);
            btn.Resize(tabsSizeX, tabsSizeY);
            btn.Click += Btn_Click;
            AddControl(btn);
            return btn;
        }

        private void Btn_Click(Control sender)
        {
            for (int i = 0; i < tabs.Count; i++)
            {
                if (tabs[i].button == sender)
                {
                    SetSelectedTab(i);
                    break;
                }
            }
        }

        private void SetSelectedTab(int idx)
        {
            selectedIndex = idx;

            for (int i = 0; i < tabs.Count; i++)
            {
                var visible = i == idx;
                tabs[i].button.BoldFont = visible;
                tabs[i].properties.Visible = visible;

                if (visible)
                    tabs[i].properties.ConditionalSetTextBoxFocus();
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (!e.Handled)
            {
                if (e.Key == Keys.Enter || e.Key == Keys.KeypadEnter)
                {
                    Close(DialogResult.OK);
                }
                else if (e.Key == Keys.Escape)
                {
                    Close(DialogResult.Cancel);
                }
            }
        }

        private void ButtonYes_Click(Control sender)
        {
            Close(DialogResult.OK);
        }

        private void ButtonNo_Click(Control sender)
        {
            Close(DialogResult.Cancel);
        }
    }
}
