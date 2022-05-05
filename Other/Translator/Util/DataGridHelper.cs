using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;

namespace Translator.Util;

public static class DataGridHelper
{
    public static DataGridCell GetCell(DataGrid dg, int row, int column)
    {
        var rowContainer = GetRow(dg, row);

        if (rowContainer != null)
        {
            var presenter = VisualHelper.GetVisualChild<DataGridCellsPresenter>(rowContainer);

            // try to get the cell but it may possibly be virtualized
            var cell = (DataGridCell)presenter.ItemContainerGenerator.ContainerFromIndex(column);

            if (cell == null)
            {
                // now try to bring into view and retrieve the cell
                dg.ScrollIntoView(rowContainer, dg.Columns[column]);
                cell = (DataGridCell)presenter.ItemContainerGenerator.ContainerFromIndex(column);
            }

            return cell;
        }

        return null;
    }

    public static DataGridRow GetRow(DataGrid dg, int index)
    {
        dg.UpdateLayout();
        var row = (DataGridRow)dg.ItemContainerGenerator.ContainerFromIndex(index);

        if (row == null)
        {
            // may be virtualized, bring into view and try again
            dg.ScrollIntoView(dg.Items[index]);
            row = (DataGridRow)dg.ItemContainerGenerator.ContainerFromIndex(index);
        }

        return row;
    }

    public static int GetRowIndex(DataGrid dg, DataGridCellInfo dgci)
    {
        if (!dgci.IsValid)
            return -1;

        var dgrow = (DataGridRow)dg.ItemContainerGenerator.ContainerFromItem(dgci.Item);

        return dgrow?.GetIndex() ?? -1;
    }

    public static int GetColIndex(DataGridCellInfo dgci)
    {
        return dgci.Column.DisplayIndex;
    }

    public static DataGridCell FindParentCell(DataGrid grid, DependencyObject child, int i)
    {
        var parent = VisualTreeHelper.GetParent(child);
        var logicalParent = LogicalTreeHelper.GetParent(child);

        if (logicalParent is DataGridCell)
            return logicalParent as DataGridCell;

        if (i > 4 || parent == null || parent is DataGridCell)
            return parent as DataGridCell;

        return FindParentCell(grid, parent, i + 1);
    }

    public static DataGridCell GetDataGridCell(DataGridCellInfo cellInfo)
    {
        if (cellInfo.IsValid == false)
            return null;

        var cellContent = cellInfo.Column.GetCellContent(cellInfo.Item);

        return cellContent?.Parent as DataGridCell;
    }

    public static DataGridCell GetDataGridCell(DataGrid dataGrid)
    {
        if (dataGrid.CurrentCell.IsValid == false)
            return null;

        var cellContent = dataGrid.CurrentCell.Column.GetCellContent(dataGrid.CurrentCell.Item);

        if (cellContent == null)
        {
            return GetCell(dataGrid, GetColIndex(dataGrid.CurrentCell), GetRowIndex(dataGrid, dataGrid.CurrentCell));
        }

        return cellContent.Parent as DataGridCell;
    }

    public static void FocusOnFirstCell(this DataGrid dataGrid)
    {
        dataGrid.SelectedIndex = 0;
        //dataGrid.CurrentCell = new DataGridCellInfo(DataGrid.Items[0], DataGrid.Columns[0]);

        var cell = GetCell(dataGrid, 0, 0);

        cell?.Focus();
    }

    public static bool Sort(this DataGrid grid, ListSortDirection direction, string property, string second = null)
    {
        //If there's already a sort defined in another property.
        foreach (var column in grid.Columns)
        {
            if (column.SortDirection.HasValue)
                return false;

            var dataColumn = column as DataGridTextColumn;

            if (dataColumn == null || dataColumn.Binding == null) continue;

            var binding = dataColumn.Binding as Binding;

            if (binding != null && binding.Path != null && binding.Path.Path == property)
                column.SortDirection = direction;
        }

        //Add the new sort description.
        grid.Items.SortDescriptions.Add(new SortDescription(property, direction));

        if (second != null)
            grid.Items.SortDescriptions.Add(new SortDescription(second, direction));

        return true;
    }

    public static void ReSort(this DataGrid grid, Dictionary<string, ListSortDirection> sorted)
    {
        if (sorted == null || !sorted.Any())
            sorted = grid.Columns.Where(x => x.SortDirection.HasValue)
                .ToDictionary(w => w.SortMemberPath, w => w.SortDirection.Value);

        grid.Items.SortDescriptions.Clear();

        foreach (var sort in sorted)
        {
            #region Search for the column that should be sorted

            var column = grid.Columns.FirstOrDefault(x =>
            {
                var dataColumn = x as DataGridTextColumn;

                if (dataColumn == null || dataColumn.Binding == null)
                    return false;

                var binding = dataColumn.Binding as Binding;

                    //Only returns true if it's the match.
                    if (binding != null && binding.Path != null && binding.Path.Path == sort.Key)
                    return true;

                return false;
            });

            #endregion

            //Displays the sort direction glyph.
            if (column != null)
                column.SortDirection = sort.Value;

            //Add the new sort description.
            grid.Items.SortDescriptions.Add(new SortDescription(sort.Key, sort.Value));
        }
    }
}
