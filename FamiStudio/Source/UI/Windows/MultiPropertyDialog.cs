using System;
using System.Reflection;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;

namespace FamiStudio
{
    public partial class MultiPropertyDialog : Form
    {
        class PropertyPageTab
        {
            public Button button;
            public PropertyPage properties;
            public bool visible = true;
        }

        int selectedIndex = 0;
        List<PropertyPageTab> tabs = new List<PropertyPageTab>();

        Font font;
        Font fontBold;

        private NoFocusButton buttonYes;
        private NoFocusButton buttonNo;
        private TableLayoutPanel tableLayout;
        private Panel panelProps;
        private Panel panelTabs;
        private ToolTip toolTip;

        public MultiPropertyDialog(string title, int width, int height, int tabsWidth = 150)
        {
            InitializeComponent();

            string suffix = DpiScaling.Dialog >= 2.0f ? "@2x" : "";

            buttonYes.Image = Image.FromStream(Assembly.GetExecutingAssembly().GetManifestResourceStream($"FamiStudio.Resources.Yes{suffix}.png"));
            buttonNo.Image  = Image.FromStream(Assembly.GetExecutingAssembly().GetManifestResourceStream($"FamiStudio.Resources.No{suffix}.png"));
            Width    = DpiScaling.ScaleForDialog(width);
            font     = new Font(PlatformUtils.PrivateFontCollection.Families[0], 10.0f, FontStyle.Regular);
            fontBold = new Font(PlatformUtils.PrivateFontCollection.Families[0], 10.0f, FontStyle.Bold);

            toolTip.SetToolTip(buttonYes, "Accept");
            toolTip.SetToolTip(buttonNo, "Cancel");

            tableLayout.ColumnStyles[0].Width = tabsWidth * DpiScaling.Dialog;
        }

        public void SetVerb(string text, bool showOnTabPage = false)
        {
        }

        private void InitializeComponent()
        {
            buttonYes = new NoFocusButton();
            buttonNo = new NoFocusButton();
            tableLayout = new TableLayoutPanel();
            panelProps = new Panel();
            panelTabs = new Panel();
            toolTip = new ToolTip();

            buttonYes.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonYes.FlatAppearance.BorderSize = 0;
            buttonYes.FlatStyle = FlatStyle.Flat;
            buttonYes.Location = new Point(374, 337);
            buttonYes.Size = new Size(32, 32);
            buttonYes.UseVisualStyleBackColor = true;
            buttonYes.Click += new EventHandler(buttonYes_Click);

            buttonNo.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonNo.FlatAppearance.BorderSize = 0;
            buttonNo.FlatStyle = FlatStyle.Flat;
            buttonNo.Location = new Point(411, 337);
            buttonNo.Size = new Size(32, 32);
            buttonNo.UseVisualStyleBackColor = true;
            buttonNo.Click += new EventHandler(buttonNo_Click);

            tableLayout.SuspendLayout();
            tableLayout.Anchor = AnchorStyles.Top | AnchorStyles.Left| AnchorStyles.Right;
            tableLayout.ColumnCount = 2;
            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
            tableLayout.ColumnStyles.Add(new ColumnStyle());
            tableLayout.Location = new Point(5, 5);
            tableLayout.Margin = new Padding(0);
            tableLayout.RowCount = 1;
            tableLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayout.Size = new Size(438, 327);
            tableLayout.Controls.Add(panelProps, 1, 0);
            tableLayout.Controls.Add(panelTabs, 0, 0);
            tableLayout.ResumeLayout(false);

            panelProps.Dock = DockStyle.Fill;
            panelProps.Location = new Point(150, 0);
            panelProps.Margin = new Padding(0);
            panelProps.Size = new Size(307, 327);

            panelTabs.BackColor = Theme.DarkGreyFillColor1;
            panelTabs.Dock = DockStyle.Fill;
            panelTabs.Location = new Point(0, 0);
            panelTabs.Margin = new Padding(0);
            panelTabs.Name = "panelTabs";
            panelTabs.Size = new Size(150, 327);
            panelTabs.TabIndex = 21;

            AutoScaleMode = AutoScaleMode.None;
            BackColor = Theme.DarkGreyFillColor1;
            ClientSize = new Size(448, 373);
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            ControlBox = false;
            KeyPreview = true;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            KeyDown += new KeyEventHandler(MultiPropertyDialog_KeyDown);

            SuspendLayout();
            Controls.Add(tableLayout);
            Controls.Add(buttonYes);
            Controls.Add(buttonNo);
            ResumeLayout(true);
        }

        public PropertyPage AddPropertyPage(string text, string image)
        {
            var suffix = DpiScaling.Dialog > 1.0f ? "@2x" : "";
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"FamiStudio.Resources.{image}{suffix}.png");
            
            if (stream == null)
                Debug.WriteLine($"Error loading bitmap {image }.");

            var bmp = stream != null ? Image.FromStream(stream) as Bitmap : null;

            if ((DpiScaling.Dialog % 1.0f) != 0.0f && bmp != null)
            {
                var newWidth  = (int)(bmp.Width  * (DpiScaling.Dialog / 2.0f));
                var newHeight = (int)(bmp.Height * (DpiScaling.Dialog / 2.0f));

                bmp = new Bitmap(bmp, newWidth, newHeight);
            }

            var page = new PropertyPage();
            page.Dock = DockStyle.Fill;
            panelProps.Controls.Add(page);

            var tab = new PropertyPageTab();
            tab.button = AddButton(text, bmp);
            tab.properties = page;

            tabs.Add(tab);

            return page;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            // Property pages need to be visible when doing the layout otherwise
            // they have the wrong size. 
            for (int i = 0; i < tabs.Count; i++)
            {
                tabs[i].properties.Visible = i == selectedIndex;
            }

            base.OnHandleCreated(e);
        }

        protected override void OnLoad(EventArgs e)
        {
            SuspendLayout();

            var y = 0;
            var maxHeight = 0;
            for (int i = 0; i < tabs.Count; i++)
            {
                var tab = tabs[i];
                tab.button.Visible = tab.visible;
                if (tab.visible)
                {
                    tab.button.Top = y;
                    y += tab.button.Height;
                    maxHeight = Math.Max(maxHeight, tabs[i].properties.LayoutHeight);
                }
            }

            maxHeight = Math.Max(maxHeight, Width / 2);

            tableLayout.Height = maxHeight;

            buttonYes.Width  = DpiScaling.ScaleForDialog(buttonYes.Width);
            buttonYes.Height = DpiScaling.ScaleForDialog(buttonYes.Height);
            buttonNo.Width   = DpiScaling.ScaleForDialog(buttonNo.Width);
            buttonNo.Height  = DpiScaling.ScaleForDialog(buttonNo.Height);

            Height = maxHeight + buttonNo.Height + 20;

            buttonYes.Left = Width  - buttonYes.Width * 2 - 17;
            buttonYes.Top  = Height - buttonNo.Height - 12;
            buttonNo.Left  = Width  - buttonNo.Width  - 12;
            buttonNo.Top   = Height - buttonNo.Height - 12;

            ResumeLayout();
            CenterToParent();

            base.OnLoad(e);
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

        private void Btn_Click(object sender, EventArgs e)
        {
            SuspendLayout();

            for (int i = 0; i < tabs.Count; i++)
            {
                if (tabs[i].button == sender)
                {
                    selectedIndex = i;
                    tabs[i].button.Font = fontBold;
                    tabs[i].properties.Visible = true;
                }
                else
                {
                    tabs[i].button.Font = font;
                    tabs[i].properties.Visible = false;
                }
            }

            ResumeLayout();
        }

        private Button AddButton(string text, Bitmap image)
        {
            var btn = new NoFocusButton();
            var sizeY = DpiScaling.ScaleForDialog(32);

            btn.BackColor = BackColor;
            btn.ForeColor = Theme.LightGreyFillColor2;
            btn.ImageAlign = ContentAlignment.MiddleLeft;
            btn.TextAlign = ContentAlignment.MiddleLeft;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Theme.DarkGreyFillColor2;
            btn.FlatAppearance.MouseDownBackColor = Theme.DarkGreyFillColor2;
            btn.Image = image;
            btn.Top = tabs.Count * sizeY;
            btn.Left = 0;
            btn.Width = panelTabs.Width;
            btn.Height = sizeY;
            btn.Font = tabs.Count == 0 ? fontBold : font;
            btn.Text = text;
            btn.TextImageRelation = TextImageRelation.ImageBeforeText;
            btn.Click += Btn_Click;

            panelTabs.Controls.Add(btn);

            return btn;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var p = base.CreateParams;
                p.Style &= (~0x02000000); // WS_CLIPCHILDREN
                return p;
            }
        }

        private void MultiPropertyDialog_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                DialogResult = DialogResult.OK;
                Close();
            }
            else if (e.KeyCode == Keys.Escape)
            {
                DialogResult = DialogResult.Cancel;
                Close();
            }
        }

        public void ShowDialog(IWin32Window parent, Action<DialogResult> callback)
        {
            callback(ShowDialog());
        }

        private void buttonYes_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void buttonNo_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
