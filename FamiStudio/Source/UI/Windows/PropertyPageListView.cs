using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
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
        private Brush foreColorBrush;

        private bool hasAnyButtons = false;
        private bool hasAnySliders = false;

        private int sliderItemIndex    = -1;
        private int sliderSubItemIndex = -1;
        private int comboItemIndex     = -1;
        private int comboSubItemIndex  = -1;

        private List<ColumnDesc> columnDescs = new List<ColumnDesc>();

        public delegate void ValueChangedDelegate(int itemIndex, int columnIndex, object value);
        public delegate void ButtonPressedDelegate(int itemIndex, int columnIndex);
        public event ValueChangedDelegate ValueChanged;
        public event ButtonPressedDelegate ButtonPressed;

        public PropertyPageListView()
        {
            comboBox.Items.Add("NC");
            comboBox.Items.Add("WA");
            comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox.Visible = false;
            comboBox.LostFocus += ComboBox_LostFocus;
            comboBox.SelectedValueChanged += ComboBox_SelectedValueChanged;
            comboBox.DropDownClosed += ComboBox_DropDownClosed;

            //comboBox.Font = new Font(PlatformUtils.PrivateFontCollection.Families[0], 8.0f, FontStyle.Regular);

            // MATTT : Only add when there is at least one dropdown columns.
            Controls.Add(comboBox);

            foreColorBrush = new SolidBrush(ForeColor);
            DoubleBuffered = true;
        }

        public ColumnHeader AddColumn(ColumnDesc desc)
        {
            Debug.Assert(desc.Type != ColumnType.CheckBox || Columns.Count == 0);

            if (desc.Type == ColumnType.CheckBox)
            {
                CheckBoxes = true;
            }
            if (desc.Type == ColumnType.Button || desc.Type == ColumnType.Slider)
            {
                OwnerDraw = true;
                if (desc.Type == ColumnType.Button) hasAnyButtons = true;
                if (desc.Type == ColumnType.Slider) hasAnySliders = true;
            }

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

        private Rectangle GetButtonRect(Rectangle subItemRect)
        {
            return new Rectangle(subItemRect.Right - subItemRect.Height + 2, subItemRect.Top + 2, subItemRect.Height - 5, subItemRect.Height - 6);
        }

        private Rectangle GetProgressBarRect(Rectangle subItemRect, float value = 1.0f)
        {
            var rc = subItemRect;
            rc.Inflate(-4, -4);
            rc = new Rectangle(rc.Left, rc.Top, (int)Math.Round(rc.Width * value), rc.Height);
            return rc;
        }

        protected override void OnDrawSubItem(DrawListViewSubItemEventArgs e)
        {
            var desc = columnDescs[e.ColumnIndex];

            if (desc.Type == ColumnType.Button ||
                desc.Type == ColumnType.Slider)
            {
                if ((e.ItemState & ListViewItemStates.Selected) != 0)
                    e.Graphics.FillRectangle(SystemBrushes.Highlight, e.Bounds);
                else
                    e.DrawBackground();
            }

            if (desc.Type == ColumnType.Button)
            {
                var textRect = e.Bounds;
                textRect.Inflate(-4, -4);
                e.Graphics.DrawString("B", Font, foreColorBrush, textRect);

                var buttonRect = GetButtonRect(e.Bounds);
                var clientCursorPos = PointToClient(Cursor.Position);

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
                e.Graphics.DrawString("...", Font, foreColorBrush, buttonRect, sf);
            }
            else if (desc.Type == ColumnType.Slider)
            {
                e.DrawBackground();

                // MATTT : Text label + hide if too small.
                var fillRect = GetProgressBarRect(e.Bounds, 0.5f); // MATTT
                var borderRect = GetProgressBarRect(e.Bounds);

                e.Graphics.FillRectangle(SystemBrushes.ActiveCaption, fillRect);
                e.Graphics.DrawRectangle(SystemPens.ControlDark, borderRect);

                var sf = new StringFormat();
                sf.Alignment = StringAlignment.Center;
                e.Graphics.DrawString("50%", Font, foreColorBrush, borderRect, sf); // MATTT
            }
            else
            {
                e.DrawDefault = true;
                base.OnDrawSubItem(e);
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

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (hasAnySliders && GetItemAndSubItemAt(e.X, e.Y, out var itemIdex, out var subItemIndex))
            {
                var desc = columnDescs[subItemIndex];
                if (desc.Type == ColumnType.Slider)
                {
                    var thumbRect = GetProgressBarRect(Items[itemIdex].SubItems[subItemIndex].Bounds);
                    if (thumbRect.Contains(e.X, e.Y))
                    {
                        sliderItemIndex = itemIdex;
                        sliderSubItemIndex = subItemIndex;
                        Capture = true;
                    }
                }
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (hasAnyButtons)
                Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (sliderItemIndex >= 0)
            {
                // MATTT : Raise event to change slider value here.
                sliderItemIndex = -1;
                sliderSubItemIndex = -1;
            }
            else if (GetItemAndSubItemAt(e.X, e.Y, out var itemIdex, out var subItemIndex))
            {
                var desc = columnDescs[subItemIndex];

                if (desc.Type == ColumnType.DropDown)
                {
                    comboItemIndex    = -1;
                    comboSubItemIndex = -1;

                    comboBox.Items.Clear();
                    comboBox.Items.AddRange(desc.DropDownValues);
                    comboBox.SelectedIndex = 0; // MATTT : How to get the selected index?
                    comboBox.Bounds = Items[itemIdex].SubItems[subItemIndex].Bounds;
                    //comboBox.Text = subItem.Text;
                    comboBox.Visible = true;
                    comboBox.DroppedDown = true;

                    comboItemIndex    = itemIdex;
                    comboSubItemIndex = subItemIndex;

                    comboBox.BringToFront();
                    comboBox.Focus();
                }
                else if (desc.Type == ColumnType.Button)
                {
                    var buttonRect = GetButtonRect(Items[itemIdex].SubItems[subItemIndex].Bounds);
                    if (buttonRect.Contains(e.X, e.Y))
                    {
                        ButtonPressed?.Invoke(itemIdex, subItemIndex);
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
                var combo = sender as ComboBox;
                if (combo.Visible)
                {
                    ValueChanged?.Invoke(comboItemIndex, comboSubItemIndex, combo.Text);
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
