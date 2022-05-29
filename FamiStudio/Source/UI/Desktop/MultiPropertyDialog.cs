using System;
using System.Collections.Generic;

using RenderBitmapAtlasRef = FamiStudio.GLBitmapAtlasRef;
using RenderBrush          = FamiStudio.GLBrush;
using RenderGeometry       = FamiStudio.GLGeometry;
using RenderControl        = FamiStudio.GLControl;
using RenderGraphics       = FamiStudio.GLGraphics;
using RenderCommandList    = FamiStudio.GLCommandList;

namespace FamiStudio
{
    public partial class MultiPropertyDialog : Dialog
    {
        class PropertyPageTab
        {
            public Button2 button;
            public PropertyPage properties;
            public bool visible = true;
        }

        int selectedIndex = 0;
        List<PropertyPageTab> tabs = new List<PropertyPageTab>();
        private int margin = DpiScaling.ScaleForMainWindow(8);
        private int tabsSizeX;
        private int tabsSizeY = DpiScaling.ScaleForMainWindow(32);

        private Button2 buttonYes;
        private Button2 buttonNo;
        //private ToolTip toolTip; MATTT

        public MultiPropertyDialog(string title, int width, int height, int tabsWidth = 150)
        {
            // MATTT : Avoid using the DpiScaling, we are a GLControl, we can access.
            tabsSizeX = DpiScaling.ScaleForMainWindow(tabsWidth);
            Move(0, 0, DpiScaling.ScaleForMainWindow(width), DpiScaling.ScaleForMainWindow(width));
            Init();

            //toolTip.SetToolTip(buttonYes, "Accept");
            //toolTip.SetToolTip(buttonNo, "Cancel");
        }

        public void SetVerb(string text, bool showOnTabPage = false)
        {
        }

        private void Init()
        {
            buttonYes = new Button2("Yes", null);
            buttonYes.Click += ButtonYes_Click;
            buttonYes.Resize(DpiScaling.ScaleForMainWindow(36), DpiScaling.ScaleForMainWindow(36));

            buttonNo = new Button2("No", null);
            buttonNo.Click += ButtonNo_Click;
            buttonNo.Resize(DpiScaling.ScaleForMainWindow(36), DpiScaling.ScaleForMainWindow(36));
            
            AddControl(buttonYes);
            AddControl(buttonNo);
        }

        public PropertyPage AddPropertyPage(string text, string image)
        {
            var page = new PropertyPage(this, tabsSizeX + margin * 2, margin, width - tabsSizeX - margin * 3);

            var tab = new PropertyPageTab();
            tab.button = AddButton(text, image);
            tab.properties = page;
            tabs.Add(tab);

            return page;
        }

        protected override void OnShowDialog()
        {
            var y = margin;
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

            Resize(width, maxHeight + buttonNo.Height + margin * 3);

            var buttonY = maxHeight + margin * 2;
            buttonYes.Move(Width - buttonYes.Width * 2 - margin * 2, buttonY);
            buttonNo.Move(Width - buttonNo.Width - margin, buttonY);

            SetSelectedTab(0);
            CenterToForm();
        }
        
        public PropertyPage GetPropertyPage(int idx)
        {
            return tabs[idx].properties;
        }

        public void SetPageVisible(int idx, bool visible)
        {
            tabs[idx].visible = visible;
        }

        public int SelectedIndex => selectedIndex;

        private Button2 AddButton(string text, string image)
        {
            var btn = new Button2(image, text);
            btn.Resize(tabsSizeX, tabsSizeY);
            btn.Click += Btn_Click;
            AddControl(btn);
            return btn;
        }

        private void Btn_Click(RenderControl sender)
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
            }
        }

        protected override void OnKeyDown(KeyEventArgs2 e)
        {
            base.OnKeyDown(e);

            if (e.Key == Keys2.Enter)
            {
                Close(DialogResult2.OK);
            }
            else if (e.Key == Keys2.Escape)
            {
                Close(DialogResult2.Cancel);
            }
        }

        private void ButtonYes_Click(RenderControl sender)
        {
            Close(DialogResult2.OK);
        }

        private void ButtonNo_Click(RenderControl sender)
        {
            Close(DialogResult2.Cancel);
        }
    }
}
