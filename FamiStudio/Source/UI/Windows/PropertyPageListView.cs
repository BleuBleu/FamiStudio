using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FamiStudio
{
    public class PropertyPageListView : ListView
    {
        private const int WM_HSCROLL = 0x114;
        private const int WM_VSCROLL = 0x115;

        private const int thumbWidth =3;

        private ComboBox comboBox = new ComboBox();

        private bool hasAnyDropDowns  = false;
        private bool hasAnyButtons    = false;

        private int sliderItemIndex    = -1;
        private int sliderSubItemIndex = -1;
        private int comboItemIndex     = -1;
        private int comboSubItemIndex  = -1;

        private Bitmap bmpCheck;
        private object[,] listData;
        private List<ColumnDesc> columnDescs = new List<ColumnDesc>();

        public delegate void ValueChangedDelegate(object sender, int itemIndex, int columnIndex, object value);
        public delegate void ButtonPressedDelegate(object sender, int itemIndex, int columnIndex);
        public event ValueChangedDelegate ValueChanged;
        public event ButtonPressedDelegate ButtonPressed;

        [DllImport("user32.dll")]
        private static extern long ShowScrollBar(IntPtr hwnd, int wBar, bool bShow);
        private int SB_HORZ = 0;

        public PropertyPageListView(ColumnDesc[] columns)
        {
            foreach (var col in columns)
                AddColumn(col);

            if (hasAnyDropDowns)
            {
                comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
                comboBox.Visible = false;
                comboBox.LostFocus += ComboBox_LostFocus;
                comboBox.SelectedValueChanged += ComboBox_SelectedValueChanged;
                comboBox.DropDownClosed += ComboBox_DropDownClosed;

                Controls.Add(comboBox);
            }

            DoubleBuffered = true;
            OwnerDraw = true;
            MultiSelect = false;
            View = View.Details;
            GridLines = true;
            FullRowSelect = true;
            HeaderStyle = ColumnHeaderStyle.Nonclickable;
            BackColor = ThemeBase.LightGreyFillColor2;

            bmpCheck = System.Drawing.Image.FromStream(Assembly.GetExecutingAssembly().GetManifestResourceStream($"FamiStudio.Resources.Check.png")) as System.Drawing.Bitmap;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            ShowScrollBar(Handle, SB_HORZ, false);
        }

        public void SetColumnEnabled(int colIdx, bool enabled)
        {
            var desc = columnDescs[colIdx];
            desc.Enabled = enabled;
            Invalidate();
        }

        protected override void OnCreateControl()
        {
            base.OnCreateControl();
            AutoResizeColumns();
        }

        public void UpdateData(object[,] data)
        {
            BeginUpdate();

            listData = data;

            for (int i = 0; i < data.GetLength(0); i++)
            {
                if (i >= Items.Count)
                {
                    var item = Items.Add(string.Format(columnDescs[0].StringFormat, data[i, 0]));
                    for (int j = 1; j < data.GetLength(1); j++)
                        item.SubItems.Add(string.Format(columnDescs[j].StringFormat, data[i, j]));
                }
                else
                {
                    var item = Items[i];
                    for (int j = 0; j < data.GetLength(1); j++)
                        item.SubItems[j].Text = string.Format(columnDescs[j].StringFormat, data[i, j]);
                }
            }

            while (Items.Count > data.GetLength(0))
            {
                Items.RemoveAt(data.GetLength(0));
            }

            EndUpdate();
        }

        public object GetData(int rowIdx, int colIdx)
        {
            return listData[rowIdx, colIdx];
        }

        public void SetData(int rowIdx, int colIdx, object val)
        {
            listData[rowIdx, colIdx] = val;
        }

        public void UpdateData(int rowIdx, int colIdx, object data)
        {
            listData[rowIdx, colIdx] = data;
            Items[rowIdx].SubItems[colIdx].Text = string.Format(columnDescs[colIdx].StringFormat, data);
        }

        public void RenameColumns(string[] columnNames)
        {
            Debug.Assert(columnNames.Length == Columns.Count);

            for (int i = 0; i < Columns.Count; i++)
            {
                var header = Columns[i] as ColumnHeader;
                header.Text = columnNames[i];
            }
        }

        public void AutoResizeColumns()
        {
            if (Items.Count == 0 || !Created)
                return;

            var itemHeight = Items[0].Bounds.Height;
            var total = 0;
            var totalWidths = 0.0f;

            for (int i = 0; i < Columns.Count; i++)
            {
                var header = Columns[i] as ColumnHeader;
                if (columnDescs[i].Width == 0.0f)
                {
                    header.Width = itemHeight;
                    total += header.Width;
                }
                totalWidths += columnDescs[i].Width;
            }

            Debug.Assert(Utils.IsNearlyEqual(totalWidths, 1.0f));

            var remaining = ClientRectangle.Width - total;

            for (int i = 0; i < Columns.Count; i++)
            {
                var header = Columns[i] as ColumnHeader;
                if (columnDescs[i].Width != 0.0f)
                    header.Width = (int)Math.Floor(remaining * columnDescs[i].Width);
            }
        }

        private ColumnHeader AddColumn(ColumnDesc desc)
        {
            Debug.Assert(desc.Type != ColumnType.CheckBox || Columns.Count == 0);

            if (desc.Type == ColumnType.Button)
                hasAnyButtons = true;
            if (desc.Type == ColumnType.DropDown)
                hasAnyDropDowns = true;

            columnDescs.Add(desc);
            var header = Columns.Add(desc.Name);
            header.Width = -2; // Auto size.

            Debug.Assert(Columns.Count == columnDescs.Count);

            return header;
        }

        protected override void OnDrawColumnHeader(DrawListViewColumnHeaderEventArgs e)
        {
            e.DrawDefault = true;
            base.OnDrawColumnHeader(e);
        }

        protected override void OnColumnWidthChanging(ColumnWidthChangingEventArgs e)
        {
            e.Cancel = columnDescs[e.ColumnIndex].Type == ColumnType.CheckBox;
            e.NewWidth = Columns[e.ColumnIndex].Width; 
            base.OnColumnWidthChanging(e);
        }

        private Rectangle GetButtonRect(Rectangle subItemRect)
        {
            return new Rectangle(subItemRect.Right - subItemRect.Height + 2, subItemRect.Top + 2, subItemRect.Height - 5, subItemRect.Height - 6);
        }

        private Rectangle GetProgressBarRect(Rectangle subItemRect, int percent = 100)
        {
            var rc = subItemRect;
            rc.Inflate(-4, -4);
            rc = new Rectangle(rc.Left, rc.Top, (int)Math.Round(rc.Width * (percent / 100.0f)), rc.Height);
            return rc;
        }

        private Rectangle GetCheckBoxRect(Rectangle subItemRect)
        {
            var rc = subItemRect;
            var checkSize = rc.Height - 10;
            return new Rectangle(rc.Left + rc.Width / 2 - checkSize / 2 - 1, rc.Top + rc.Height / 2 - checkSize / 2 - 1, checkSize, checkSize);
        }

        protected override void OnDrawSubItem(DrawListViewSubItemEventArgs e)
        {
            var desc = columnDescs[e.ColumnIndex];

            var brush = desc.Enabled ? SystemBrushes.ControlText : SystemBrushes.GrayText;
            var pen   = desc.Enabled ? SystemPens.ControlText    : SystemPens.GrayText;

            if (desc.Type == ColumnType.Button)
            {
                e.DrawBackground();

                var textRect = e.Bounds;
                textRect.Inflate(-4, -4);

                if (desc.Enabled)
                {
                    var buttonRect = GetButtonRect(e.Bounds);
                    var clientCursorPos = PointToClient(Cursor.Position);

                    e.Graphics.SetClip(e.Bounds);
                    e.Graphics.DrawString(Items[e.ItemIndex].SubItems[e.ColumnIndex].Text, Font, brush, textRect.Left, textRect.Top);
                    e.Graphics.ResetClip();

                    if (buttonRect.Contains(clientCursorPos))
                    {
                        e.Graphics.FillRectangle(SystemBrushes.ActiveCaption, buttonRect);
                        e.Graphics.DrawRectangle(SystemPens.MenuHighlight, buttonRect);
                    }
                    else
                    {
                        e.Graphics.FillRectangle(SystemBrushes.ControlLight, buttonRect);
                        e.Graphics.DrawRectangle(SystemPens.ControlDark, buttonRect);
                    }

                    var sf = new StringFormat();
                    sf.Alignment = StringAlignment.Center;
                    sf.LineAlignment = StringAlignment.Far;
                    e.Graphics.DrawString("...", Font, brush, buttonRect, sf);
                }
                else
                {
                    e.Graphics.DrawString("N/A", Font, brush, textRect);
                }
            }
            else if (desc.Type == ColumnType.Slider)
            {
                e.DrawBackground();

                var fillRect = GetProgressBarRect(e.Bounds, (int)listData[e.ItemIndex, e.ColumnIndex]);
                var borderRect = GetProgressBarRect(e.Bounds);

                e.Graphics.DrawRectangle(SystemPens.ControlDark, borderRect);

                var sf = new StringFormat();
                sf.Alignment = StringAlignment.Center;

                if (desc.Enabled)
                {
                    e.Graphics.FillRectangle(SystemBrushes.ActiveCaption, fillRect);
                    e.Graphics.DrawString(Items[e.ItemIndex].SubItems[e.ColumnIndex].Text, Font, brush, borderRect, sf);
                }
                else
                {
                    e.Graphics.DrawString("N/A", Font, brush, borderRect, sf);
                }
            }
            else if (desc.Type == ColumnType.CheckBox)
            {
                var check = (bool)listData[e.ItemIndex, e.ColumnIndex];
                var checkRect = GetCheckBoxRect(e.Bounds);

                if (check && desc.Enabled)
                    e.Graphics.DrawImage(bmpCheck, checkRect);

                e.Graphics.DrawRectangle(pen, checkRect);
            }
            else 
            {
                var textRect = e.Bounds;
                textRect.Inflate(-4, -4);
                e.Graphics.SetClip(e.Bounds);
                e.Graphics.DrawString(Items[e.ItemIndex].SubItems[e.ColumnIndex].Text, Font, brush, textRect.Left, textRect.Top);
                e.Graphics.ResetClip();
            }
        }

        private bool GetItemAndSubItemAt(int x, int y, out int itemIndex, out int subItemIndex)
        {
            itemIndex = -1;
            subItemIndex = -1;
            
            var item = GetItemAt(x, y);

            if (item == null)
                return false;

            var subItem = item.GetSubItemAt(x, y);

            if (subItem != null)
            {
                itemIndex = item.Index;
                subItemIndex = item.SubItems.IndexOf(subItem);
                return true;
            }

            return false;
        }

        private void UpdateSliderDrag(MouseEventArgs e)
        {
            if (sliderItemIndex >= 0)
            {
                var sliderRect = GetProgressBarRect(Items[sliderItemIndex].SubItems[sliderSubItemIndex].Bounds);
                var value = Utils.Clamp((int)Math.Round((e.X - sliderRect.Left) / (float)sliderRect.Width * 100.0f), 0, 100);

                listData[sliderItemIndex, sliderSubItemIndex] = value;
                Items[sliderItemIndex].SubItems[sliderSubItemIndex].Text = string.Format(columnDescs[sliderSubItemIndex].StringFormat, value);

                ValueChanged?.Invoke(this, sliderItemIndex, sliderSubItemIndex, value);
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (e.Button == MouseButtons.Left)
            {
                if (GetItemAndSubItemAt(e.X, e.Y, out var itemIdex, out var subItemIndex))
                {
                    var desc = columnDescs[subItemIndex];
                    if (desc.Enabled)
                    {
                        if (desc.Type == ColumnType.Slider)
                        {
                            var sliderRect = GetProgressBarRect(Items[itemIdex].SubItems[subItemIndex].Bounds);
                            if (sliderRect.Contains(e.X, e.Y))
                            {
                                sliderItemIndex = itemIdex;
                                sliderSubItemIndex = subItemIndex;
                                Capture = true;
                            }
                        }
                        else if (desc.Type == ColumnType.CheckBox)
                        {
                            var newVal = !(bool)listData[itemIdex, subItemIndex];
                            listData[itemIdex, subItemIndex] = newVal;
                            ValueChanged?.Invoke(this, itemIdex, subItemIndex, newVal);
                            Invalidate();
                        }
                    }
                }
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (hasAnyButtons)
                Invalidate();

            UpdateSliderDrag(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (sliderItemIndex >= 0)
            {
                UpdateSliderDrag(e);

                sliderItemIndex = -1;
                sliderSubItemIndex = -1;
            }
            else if (GetItemAndSubItemAt(e.X, e.Y, out var itemIdex, out var subItemIndex))
            {
                var desc = columnDescs[subItemIndex];

                if (desc.Enabled)
                {
                    if (desc.Type == ColumnType.DropDown)
                    {
                        comboItemIndex = -1;
                        comboSubItemIndex = -1;

                        comboBox.Items.Clear();
                        comboBox.Items.AddRange(desc.DropDownValues);
                        comboBox.SelectedIndex = 0; // MATTT : How to get the selected index?
                        comboBox.Bounds = Items[itemIdex].SubItems[subItemIndex].Bounds;
                        //comboBox.Text = subItem.Text;
                        comboBox.Visible = true;
                        comboBox.DroppedDown = true;

                        comboItemIndex = itemIdex;
                        comboSubItemIndex = subItemIndex;

                        comboBox.BringToFront();
                        comboBox.Focus();
                    }
                    else if (desc.Type == ColumnType.Button)
                    {
                        var buttonRect = GetButtonRect(Items[itemIdex].SubItems[subItemIndex].Bounds);
                        if (buttonRect.Contains(e.X, e.Y))
                        {
                            ButtonPressed?.Invoke(this, itemIdex, subItemIndex);
                        }
                    }
                }
            }

            base.OnMouseUp(e);
        }

        private void ComboBox_LostFocus(object sender, EventArgs e)
        {
            (sender as ComboBox).Visible = false;
        }

        private void ComboBox_SelectedValueChanged(object sender, EventArgs e)
        {
            if (comboItemIndex >= 0 && comboSubItemIndex >= 0)
            {
                Debug.Assert(columnDescs[comboSubItemIndex].Enabled);

                var combo = sender as ComboBox;
                if (comboItemIndex >= 0)
                {
                    listData[comboItemIndex, comboSubItemIndex] = combo.Text;
                    Items[comboItemIndex].SubItems[comboSubItemIndex].Text = combo.Text;
                    ValueChanged?.Invoke(this, comboItemIndex, comboSubItemIndex, combo.Text);
                    combo.Visible = false;
                }
            }
        }

        private void ComboBox_DropDownClosed(object sender, EventArgs e)
        {
            (sender as ComboBox).Visible = false;
        }
    }
}
