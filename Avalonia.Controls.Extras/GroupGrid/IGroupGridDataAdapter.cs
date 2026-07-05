namespace Avalonia.Controls;

/// <summary>
/// Provides indexed row and cell access for a group grid data source.
/// </summary>
public interface IGroupGridDataAdapter
{
    // ● public methods
    /// <summary>
    /// Returns the source row at a specified adapter row index.
    /// </summary>
    /// <param name="RowIndex">The adapter row index.</param>
    /// <returns>The source row.</returns>
    object GetRow(int RowIndex);
    /// <summary>
    /// Returns the cell value at a specified adapter row index and grid column.
    /// </summary>
    /// <param name="RowIndex">The adapter row index.</param>
    /// <param name="Column">The grid column.</param>
    /// <returns>The cell value.</returns>
    object GetValue(int RowIndex, GroupGridColumn Column);
    /// <summary>
    /// Sets the cell value at a specified adapter row index and grid column.
    /// </summary>
    /// <param name="RowIndex">The adapter row index.</param>
    /// <param name="Column">The grid column.</param>
    /// <param name="Value">The value to set.</param>
    void SetValue(int RowIndex, GroupGridColumn Column, object Value);
    /// <summary>
    /// Returns true when a value can be set at a specified adapter row index and grid column.
    /// </summary>
    /// <param name="RowIndex">The adapter row index.</param>
    /// <param name="Column">The grid column.</param>
    /// <returns>True when the cell value can be set; otherwise, false.</returns>
    bool CanSetValue(int RowIndex, GroupGridColumn Column);

    // ● properties
    /// <summary>
    /// Gets the number of rows exposed by the adapter.
    /// </summary>
    int RowCount { get; }

    // ● events
    /// <summary>
    /// Occurs when rows or cell values exposed by the adapter change.
    /// </summary>
    event EventHandler<GroupGridDataChangedEventArgs> Changed;
}
