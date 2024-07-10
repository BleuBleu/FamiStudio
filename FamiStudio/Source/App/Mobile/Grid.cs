using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace FamiStudio
{
    public class Grid : Container
    {
        private ColumnDesc[] columns;
        private Control[,] gridControls;
        private GridOptions options;

        public Grid(Container parent, ColumnDesc[] cols, object[,] data, GridOptions opts = GridOptions.None)
        {
            clipRegion = false;
            columns = cols;
            options = opts;
            gridControls = new Control[data.GetLength(0), data.GetLength(1)];

            // HACK : Need to temporarely add to be able to add sub controls.
            parent.AddControl(this);

            var numRows = 0;
            var noHeader = options.HasFlag(GridOptions.NoHeader);
            var mergeCheckboxAndLabel = false;
            var firstColumnIsLabel = false;

            if (noHeader)
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
            var rowIdx = 0;
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
                        AddControl(checkBox);

                        gridControls[r, 0] = checkBox;
                        gridControls[r, 1] = checkBox;

                        c++;
                        rowIdx++;
                        y += checkBox.Height;
                        continue;
                    }

                    var x = 0;
                    var noLabel = noHeader || (firstColumnIsLabel && c == 0);

                    if (!noLabel)
                    {
                        var colLabel = new Label(columns[c].Name);
                        colLabel.Move(0, y, 300, rowHeight); // MATTT : Hardcoded 300, need to measure!
                        AddControl(colLabel);
                        x = colLabel.Right;
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

                    ctrl.Move(x, y, 700, rowHeight);
                    AddControl(ctrl);
                    y += rowHeight + rowMargin;

                    // MATTT : There was a different layout depending on noheader?
                    //gridLayout.AddView(view, CreateGridLayoutParams(rowIdx, noHeader ? c : 1));

                    gridControls[r, c] = ctrl;

                    if (!noHeader) rowIdx++;
                }

                if (noHeader) rowIdx++;
            }

            Resize(1000, y);
            parent.RemoveControl(this);
        }

        public void SetColumnEnabled(int colIdx, bool enabled)
        {
            for (int i = 0; i < gridControls.GetLength(0); i++)
            {
                gridControls[i, colIdx].Enabled = enabled;
            }
        }
    }
}