namespace Avalonia.Controls.Extras.Grid.Tests;

/// <summary>
/// Tests the non-visual group grid engine behavior.
/// </summary>
public class GroupGridEngineTests
{
    // ● private methods
    ObservableCollection<GridTestRow> CreateRows()
    {
        return new ObservableCollection<GridTestRow>
        {
            new() { Category = "A", Name = "Alpha", Quantity = 2, Amount = 10m },
            new() { Category = "A", Name = "Beta", Quantity = 5, Amount = 25m },
            new() { Category = "B", Name = "Gamma", Quantity = 1, Amount = 7m },
            new() { Category = "B", Name = "Delta", Quantity = 8, Amount = 40m },
        };
    }
    GroupGridEngine CreateEngine(ObservableCollection<GridTestRow> Rows)
    {
        GroupGridEngine Result = new();
        Result.Columns.Add(new GroupGridTextColumn { Name = nameof(GridTestRow.Category), Header = "Category" });
        Result.Columns.Add(new GroupGridTextColumn { Name = nameof(GridTestRow.Name), Header = "Name" });
        Result.Columns.Add(new GroupGridNumberColumn { Name = nameof(GridTestRow.Quantity), Header = "Quantity", TotalSummary = GroupGridAggregateKind.Sum });
        Result.Columns.Add(new GroupGridNumberColumn { Name = nameof(GridTestRow.Amount), Header = "Amount", TotalSummary = GroupGridAggregateKind.Sum });
        Result.DataAdapter = new GroupGridListDataAdapter<GridTestRow>(Rows);
        return Result;
    }
    GroupGridColumn Column(GroupGridEngine Engine, string Name)
    {
        return Engine.Columns.First(Item => Item.Name == Name);
    }
    List<int> VisibleDataRowIndexes(GroupGridEngine Engine)
    {
        List<int> Result = new();
        for (int Index = 0; Index < Engine.VisibleNodeCount; Index++)
        {
            GroupGridRowInfo RowInfo = Engine.GetVisibleRowInfo(Index);
            if (RowInfo.IsDataRow)
                Result.Add(RowInfo.RowIndex);
        }

        return Result;
    }

    // ● tests
    /// <summary>
    /// Verifies that the engine projects flat data rows in source order.
    /// </summary>
    [Fact]
    public void RebuildProjection_WithFlatRows_ProjectsDataRowsInSourceOrder()
    {
        ObservableCollection<GridTestRow> Rows = CreateRows();
        GroupGridEngine Engine = CreateEngine(Rows);

        Assert.Equal(4, Engine.VisibleNodeCount);
        Assert.Equal(new[] { 0, 1, 2, 3 }, VisibleDataRowIndexes(Engine));
    }
    /// <summary>
    /// Verifies that grouping creates group and data rows.
    /// </summary>
    [Fact]
    public void GroupColumn_WithCategory_CreatesGroupedProjection()
    {
        ObservableCollection<GridTestRow> Rows = CreateRows();
        GroupGridEngine Engine = CreateEngine(Rows);
        GroupGridColumn CategoryColumn = Column(Engine, nameof(GridTestRow.Category));

        Assert.True(Engine.GroupColumn(CategoryColumn));

        Assert.True(Engine.VisibleNodeCount > Rows.Count);
        Assert.True(Engine.GetVisibleRowInfo(0).IsGroup);
        Assert.Equal("A", Engine.GetVisibleRowInfo(0).Key);
        Assert.Equal(new[] { 0, 1, 2, 3 }, VisibleDataRowIndexes(Engine));
    }
    /// <summary>
    /// Verifies that sorting changes visible data-row order.
    /// </summary>
    [Fact]
    public void ToggleSort_WithQuantity_SortsAscendingAndDescending()
    {
        GroupGridEngine Engine = CreateEngine(CreateRows());
        GroupGridColumn QuantityColumn = Column(Engine, nameof(GridTestRow.Quantity));

        Assert.True(Engine.ToggleSort(QuantityColumn));
        Assert.Equal(GroupGridSortDirection.Ascending, Engine.SortDirection);
        Assert.Equal(new[] { 2, 0, 1, 3 }, VisibleDataRowIndexes(Engine));

        Assert.True(Engine.ToggleSort(QuantityColumn));
        Assert.Equal(GroupGridSortDirection.Descending, Engine.SortDirection);
        Assert.Equal(new[] { 3, 1, 0, 2 }, VisibleDataRowIndexes(Engine));
    }
    /// <summary>
    /// Verifies contains, comparison, and wildcard filters.
    /// </summary>
    [Fact]
    public void SetColumnFilter_WithSupportedFilterText_FiltersProjection()
    {
        GroupGridEngine Engine = CreateEngine(CreateRows());
        GroupGridColumn NameColumn = Column(Engine, nameof(GridTestRow.Name));
        GroupGridColumn QuantityColumn = Column(Engine, nameof(GridTestRow.Quantity));

        Assert.True(Engine.SetColumnFilter(NameColumn, "ta"));
        Assert.Equal(new[] { 1, 3 }, VisibleDataRowIndexes(Engine));

        Assert.True(Engine.SetColumnFilter(QuantityColumn, ">=5"));
        Assert.Equal(new[] { 1, 3 }, VisibleDataRowIndexes(Engine));

        Assert.True(Engine.ClearFilters());
        Assert.True(Engine.SetColumnFilter(NameColumn, "G*"));
        Assert.Equal(new[] { 2 }, VisibleDataRowIndexes(Engine));
    }
    /// <summary>
    /// Verifies total summaries over the projected rows.
    /// </summary>
    [Fact]
    public void GetTotalSummary_WithSumColumn_ReturnsProjectedSum()
    {
        GroupGridEngine Engine = CreateEngine(CreateRows());
        GroupGridColumn QuantityColumn = Column(Engine, nameof(GridTestRow.Quantity));

        GroupGridSummaryValue Summary = Engine.GetTotalSummary(QuantityColumn);

        Assert.Equal(GroupGridAggregateKind.Sum, Summary.AggregateKind);
        Assert.Equal(16m, Convert.ToDecimal(Summary.Value));
    }
    /// <summary>
    /// Verifies current cell and selected cell behavior.
    /// </summary>
    [Fact]
    public void SetCurrentCellAndSelectCurrentCell_WithValidCell_UpdatesCellState()
    {
        GroupGridEngine Engine = CreateEngine(CreateRows());
        GroupGridColumn NameColumn = Column(Engine, nameof(GridTestRow.Name));

        Assert.True(Engine.SetCurrentCell(1, NameColumn));
        Assert.True(Engine.SelectCurrentCell());

        Assert.Equal(1, Engine.CurrentRowIndex);
        Assert.Same(NameColumn, Engine.CurrentColumn);
        Assert.True(Engine.IsSelectedRow(1));
        Assert.True(Engine.IsSelectedCell(new GroupGridCell(1, NameColumn)));
    }
    /// <summary>
    /// Verifies navigation to first and last visible data rows.
    /// </summary>
    [Fact]
    public void MoveCurrentToFirstAndLastRow_WithVisibleRows_UpdatesCurrentRow()
    {
        GroupGridEngine Engine = CreateEngine(CreateRows());

        Assert.True(Engine.MoveCurrentToFirstRow());
        Assert.Equal(0, Engine.CurrentRowIndex);

        Assert.True(Engine.MoveCurrentToLastRow());
        Assert.Equal(3, Engine.CurrentRowIndex);
    }
    /// <summary>
    /// Verifies edit commit updates the adapter value and raises commit events.
    /// </summary>
    [Fact]
    public void CommitEdit_WithValidValue_UpdatesValueAndRaisesCommitted()
    {
        ObservableCollection<GridTestRow> Rows = CreateRows();
        GroupGridEngine Engine = CreateEngine(Rows);
        GroupGridColumn NameColumn = Column(Engine, nameof(GridTestRow.Name));
        bool Committed = false;
        Engine.CellValueCommitted += (Sender, Args) => Committed = true;

        Assert.True(Engine.BeginEdit(0, NameColumn));
        Assert.True(Engine.CommitEdit("Changed"));

        Assert.Equal("Changed", Rows[0].Name);
        Assert.True(Committed);
        Assert.False(Engine.IsEditing);
    }
    /// <summary>
    /// Verifies validation cancellation keeps editing active and leaves the value unchanged.
    /// </summary>
    [Fact]
    public void CommitEdit_WhenValidationCancels_DoesNotUpdateValue()
    {
        ObservableCollection<GridTestRow> Rows = CreateRows();
        GroupGridEngine Engine = CreateEngine(Rows);
        GroupGridColumn NameColumn = Column(Engine, nameof(GridTestRow.Name));
        Engine.CellValidating += (Sender, Args) => Args.Cancel = true;

        Assert.True(Engine.BeginEdit(0, NameColumn));
        Assert.False(Engine.CommitEdit("Changed"));

        Assert.Equal("Alpha", Rows[0].Name);
        Assert.True(Engine.IsEditing);
    }
    /// <summary>
    /// Verifies cancel edit clears editing state and raises cancel event.
    /// </summary>
    [Fact]
    public void CancelEdit_WithActiveEdit_ClearsEditingState()
    {
        GroupGridEngine Engine = CreateEngine(CreateRows());
        GroupGridColumn NameColumn = Column(Engine, nameof(GridTestRow.Name));
        bool Canceled = false;
        Engine.EditCanceled += (Sender, Args) => Canceled = true;

        Assert.True(Engine.BeginEdit(0, NameColumn));
        Assert.True(Engine.CancelEdit());

        Assert.True(Canceled);
        Assert.False(Engine.IsEditing);
    }
    /// <summary>
    /// Verifies that clearing sorting restores source row order.
    /// </summary>
    [Fact]
    public void ClearSort_WithActiveSort_RestoresSourceOrder()
    {
        GroupGridEngine Engine = CreateEngine(CreateRows());
        GroupGridColumn QuantityColumn = Column(Engine, nameof(GridTestRow.Quantity));

        Assert.True(Engine.ToggleSort(QuantityColumn));
        Assert.True(Engine.ClearSort());

        Assert.Equal(GroupGridSortDirection.None, Engine.SortDirection);
        Assert.Null(Engine.SortColumn);
        Assert.Equal(new[] { 0, 1, 2, 3 }, VisibleDataRowIndexes(Engine));
    }
    /// <summary>
    /// Verifies that hiding the current column clears current and selected cells.
    /// </summary>
    [Fact]
    public void SetColumnVisible_WhenCurrentColumnIsHidden_ClearsCellState()
    {
        GroupGridEngine Engine = CreateEngine(CreateRows());
        GroupGridColumn NameColumn = Column(Engine, nameof(GridTestRow.Name));
        Assert.True(Engine.SetCurrentCell(0, NameColumn));
        Assert.True(Engine.SelectCurrentCell());

        Assert.True(Engine.SetColumnVisible(NameColumn, false));

        Assert.False(Engine.HasCurrentCell);
        Assert.False(Engine.HasSelectedCell);
    }
    /// <summary>
    /// Verifies that row removal coerces current cell state.
    /// </summary>
    [Fact]
    public void DataAdapterRowRemoved_WhenCurrentRowIsRemoved_ClearsCurrentCell()
    {
        ObservableCollection<GridTestRow> Rows = CreateRows();
        GroupGridEngine Engine = CreateEngine(Rows);
        GroupGridColumn NameColumn = Column(Engine, nameof(GridTestRow.Name));
        Assert.True(Engine.SetCurrentCell(3, NameColumn));

        Rows.RemoveAt(3);

        Assert.False(Engine.HasCurrentCell);
    }
    /// <summary>
    /// Verifies row insertion through the engine command API.
    /// </summary>
    [Fact]
    public void InsertRow_WhenAllowed_InsertsRowAndMovesCurrentCell()
    {
        ObservableCollection<GridTestRow> Rows = CreateRows();
        GroupGridEngine Engine = CreateEngine(Rows);
        GroupGridColumn NameColumn = Column(Engine, nameof(GridTestRow.Name));
        bool Inserted = false;
        Engine.RowInserted += (Sender, Args) => Inserted = true;
        Assert.True(Engine.SetCurrentCell(0, NameColumn));

        Assert.True(Engine.InsertRow());

        Assert.True(Inserted);
        Assert.Equal(5, Rows.Count);
        Assert.Equal(1, Engine.CurrentRowIndex);
        Assert.True(Engine.IsSelectedRow(1));
    }
    /// <summary>
    /// Verifies row insertion cancellation.
    /// </summary>
    [Fact]
    public void InsertRow_WhenCanceled_DoesNotInsertRow()
    {
        ObservableCollection<GridTestRow> Rows = CreateRows();
        GroupGridEngine Engine = CreateEngine(Rows);
        Engine.InsertingRow += (Sender, Args) => Args.Cancel = true;

        Assert.False(Engine.InsertRow());

        Assert.Equal(4, Rows.Count);
        Assert.False(Engine.HasCurrentCell);
    }
    /// <summary>
    /// Verifies row deletion through the engine command API.
    /// </summary>
    [Fact]
    public void DeleteCurrentRow_WhenAllowed_DeletesRowAndClearsCellState()
    {
        ObservableCollection<GridTestRow> Rows = CreateRows();
        GroupGridEngine Engine = CreateEngine(Rows);
        GroupGridColumn NameColumn = Column(Engine, nameof(GridTestRow.Name));
        bool Deleted = false;
        Engine.RowDeleted += (Sender, Args) => Deleted = true;
        Assert.True(Engine.SetCurrentCell(1, NameColumn));
        Assert.True(Engine.SelectCurrentCell());

        Assert.True(Engine.DeleteCurrentRow());

        Assert.True(Deleted);
        Assert.Equal(3, Rows.Count);
        Assert.Equal("Gamma", Rows[1].Name);
        Assert.False(Engine.HasCurrentCell);
        Assert.False(Engine.HasSelectedCell);
    }
    /// <summary>
    /// Verifies row deletion cancellation.
    /// </summary>
    [Fact]
    public void DeleteCurrentRow_WhenCanceled_DoesNotDeleteRow()
    {
        ObservableCollection<GridTestRow> Rows = CreateRows();
        GroupGridEngine Engine = CreateEngine(Rows);
        GroupGridColumn NameColumn = Column(Engine, nameof(GridTestRow.Name));
        Engine.DeletingRow += (Sender, Args) => Args.Cancel = true;
        Assert.True(Engine.SetCurrentCell(1, NameColumn));

        Assert.False(Engine.DeleteCurrentRow());

        Assert.Equal(4, Rows.Count);
        Assert.Equal(1, Engine.CurrentRowIndex);
    }
}
