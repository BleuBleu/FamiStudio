using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace FamiStudio
{
    public class Grid : Container
    {
        private ColumnDesc[] columns;
        private Control[,] gridControls;
        private bool[,] gridDisabled;
        private object[,] data;
        private GridOptions options;

        public Grid(Container parent, ColumnDesc[] cols, object[,] d, GridOptions opts = GridOptions.None)
        {
            clipRegion = false;
            columns = cols;
            options = opts;
            data = d;
            gridControls = new Control[data.GetLength(0), data.GetLength(1)];
            gridDisabled = new bool[data.GetLength(0), data.GetLength(1)];
        }

        private void RecreateAllControls()
        {
            RemoveAllControls();

            var numRows = 0;
            var twoColumnLayout = options.HasFlag(GridOptions.MobileTwoColumnLayout);
            var mergeCheckboxAndLabel = false;
            var firstColumnIsLabel = false;

            if (twoColumnLayout)
            {
                Debug.Assert(columns.Length == 2);
                numRows = data.GetLength(0);
            }
            else
            {
                // Special case, if the 2 first columns are checkbox + label, we combine then in something nicer.
                // MATTT : MIDI import as another trivial use case we need to handle.
                if (columns[0].Type == ColumnType.CheckBox && columns[1].Type == ColumnType.Label)
                {
                    numRows = data.GetLength(0) * (data.GetLength(1) - 1);
                    mergeCheckboxAndLabel = true;
                }
                else if (columns[0].Type == ColumnType.Label)
                {
                    firstColumnIsLabel = true;
                }
                else
                {
                    numRows = data.GetLength(0) * data.GetLength(1);
                }
            }

            var rowMargin = DpiScaling.ScaleForWindow(2);
            var rowHeight = DpiScaling.ScaleForWindow(16);
            var y = 0;

            for (int r = 0; r < data.GetLength(0); r++)
            {
                for (int c = 0; c < columns.Length; c++)
                {
                    var ctrl = (Control)null;
                    var col = columns[c];

                    if (c == 0 && mergeCheckboxAndLabel)
                    {
                        var checkBox = new CheckBox((bool)data[r, c], (string)data[r, c + 1]);
                        checkBox.Move(0, y, 1000, rowHeight);
                        checkBox.Enabled = !gridDisabled[r, 0];
                        AddControl(checkBox);

                        gridControls[r, 0] = checkBox;
                        gridControls[r, 1] = checkBox;

                        c++;
                        y += checkBox.Height;
                        continue;
                    }

                    var x = 0;
                    var noLabel = twoColumnLayout || (firstColumnIsLabel && c == 0);

                    if (!noLabel)
                    {
                        var colLabel = new Label(columns[c].Name);
                        colLabel.Move(0, y, 300, rowHeight); // MATTT : Hardcoded 300, need to measure!
                        AddControl(colLabel);
                        x = colLabel.Right;
                    }
                    else if (twoColumnLayout && c == 1)
                    {
                        x = (int)(columns[0].Width * width);
                    }

                    switch (col.Type)
                    {
                        case ColumnType.CheckBox:
                            var checkBox = new CheckBox((bool)data[r, c]);
                            ctrl = checkBox;
                            break;
                        case ColumnType.Label:
                            var text = new Label((string)data[r, c]);
                            text.Font = firstColumnIsLabel && c == 0 ? fonts.FontMediumBold : fonts.FontMedium;
                            text.Ellipsis = twoColumnLayout;
                            ctrl = text;
                            break;
                        case ColumnType.Slider:
                            var seek = new Slider((int)data[r, c], col.MinValue, col.MaxValue, 1, false /*, col.Formatter*/); // MATTT : Format!
                            ctrl = seek;
                            break;
                        case ColumnType.NumericUpDown:
                            var upDown = new NumericUpDown((int)data[r, c], col.MinValue, col.MaxValue, 1);
                            ctrl = upDown;
                            break;
                        case ColumnType.DropDown:
                            var dropDown = new DropDown(col.DropDownValues, Array.IndexOf(col.DropDownValues, (string)data[r, c]));
                            ctrl = dropDown;
                            break;
                        case ColumnType.Button:
                            var button = new Button(null, "...");
                            button.Border = true;
                            ctrl = button;
                            break;
                        default:
                            Debug.Assert(false);
                            break;
                    }

                    ctrl.Move(x, y, false);

                    if (twoColumnLayout)
                    {
                        ctrl.Resize((int)(col.Width * width), rowHeight);
                    }
                    else
                    {
                        ctrl.Resize(700, rowHeight); // MATTT
                    }

                    AddControl(ctrl);

                    if (c == 1 || !twoColumnLayout)
                    {
                        y += rowHeight + rowMargin;
                    }

                    ctrl.Enabled = !gridDisabled[r, c];
                    gridControls[r, c] = ctrl;
                }
            }

            Resize(width, y);
        }

        // We will want to Re-run PropertyPage.Build() here, so cant handle it ourselves.
        //protected override void OnResize(EventArgs e)
        //{
        //    if (IsContainedByMainWindow)
        //    {
        //        RecreateAllControls();
        //    }
        //}

        protected override void OnAddedToContainer()
        {
            RecreateAllControls();
        }

        public void SetColumnEnabled(int colIdx, bool enabled)
        {
            for (int i = 0; i < gridControls.GetLength(0); i++)
            {
                gridDisabled[i, colIdx] = !enabled;

                if (gridControls[i, colIdx] != null)
                {
                    gridControls[i, colIdx].Enabled = enabled;
                }
            }
        }
    }
}