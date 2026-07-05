namespace Avalonia.Controls;

/// <summary>
/// Provides the Avalonia visual surface for a <see cref="GroupGridEngine"/>.
/// </summary>
public class GroupGrid: Control
{
    // ● private fields
    static readonly IBrush fBackgroundBrush = new SolidColorBrush(Color.FromRgb(255, 255, 255));
    static readonly IBrush fToolBarBrush = new SolidColorBrush(Color.FromRgb(246, 247, 248));
    static readonly IBrush fGroupPanelBrush = new SolidColorBrush(Color.FromRgb(235, 239, 244));
    static readonly IBrush fHeaderBrush = new SolidColorBrush(Color.FromRgb(241, 243, 245));
    static readonly IBrush fFilterBrush = new SolidColorBrush(Color.FromRgb(250, 250, 250));
    static readonly IBrush fGroupRowBrush = new SolidColorBrush(Color.FromRgb(232, 238, 246));
    static readonly IBrush fGroupSummaryBrush = new SolidColorBrush(Color.FromRgb(247, 249, 252));
    static readonly IBrush fSelectedBrush = new SolidColorBrush(Color.FromRgb(218, 236, 255));
    static readonly IBrush fCurrentBrush = new SolidColorBrush(Color.FromRgb(255, 247, 213));
    static readonly IBrush fEditingBrush = new SolidColorBrush(Color.FromRgb(255, 238, 204));
    static readonly IBrush fFooterBrush = new SolidColorBrush(Color.FromRgb(238, 240, 242));
    static readonly IBrush fTextBrush = new SolidColorBrush(Color.FromRgb(32, 37, 42));
    static readonly IBrush fMutedTextBrush = new SolidColorBrush(Color.FromRgb(84, 91, 99));
    static readonly IBrush fScrollBarTrackBrush = new SolidColorBrush(Color.FromRgb(244, 246, 248));
    static readonly IBrush fScrollBarThumbBrush = new SolidColorBrush(Color.FromRgb(188, 196, 205));
    static readonly IBrush fColumnDragBrush = new SolidColorBrush(Color.FromArgb(220, 250, 252, 255));
    static readonly Pen fLinePen = new(new SolidColorBrush(Color.FromRgb(211, 216, 222)), 1);
    static readonly Pen fCurrentPen = new(new SolidColorBrush(Color.FromRgb(64, 122, 190)), 1);
    static readonly Pen fEditingPen = new(new SolidColorBrush(Color.FromRgb(194, 105, 0)), 2);
    static readonly Pen fResizePen = new(new SolidColorBrush(Color.FromRgb(80, 120, 170)), 1);
    GroupGridEngine fEngine;
    object fItemsSource;
    IDisposable fOwnedDataAdapter;
    int fFirstVisibleNodeIndex;
    bool fAutoGenerateColumns;
    bool fIsVerticalScrollDragging;
    bool fIsHorizontalScrollDragging;
    bool fIsColumnResizing;
    bool fIsColumnDragging;
    bool fIsColumnDragActive;
    bool fColumnDragFromGroupPanel;
    double fVerticalScrollDragOffset;
    double fHorizontalScrollDragOffset;
    double fHorizontalOffset;
    double fColumnResizeStartX;
    double fColumnResizeStartWidth;
    double fColumnResizeCurrentX;
    GroupGridColumn fColumnResizeColumn;
    double fColumnDragStartX;
    double fColumnDragCurrentX;
    double fColumnDragCurrentY;
    double fColumnDragDropX;
    int fColumnDragDropIndex = -1;
    int fColumnDragGroupDropIndex = -1;
    int fColumnDragSourceGroupIndex = -1;
    GroupGridColumn fColumnDragColumn;
    bool fHasHorizontalScrollBar;

    // ● private methods
    void Engine_Changed(object Sender, EventArgs Args)
    {
        UpdateViewport(Bounds.Size);
        InvalidateVisual();
    }
    void AttachEngine(GroupGridEngine Engine)
    {
        if (Engine == null)
            return;

        Engine.DataAdapterChanged += Engine_Changed;
        Engine.DataChanged += Engine_Changed;
        Engine.ColumnsChanged += Engine_Changed;
        Engine.GroupColumnsChanged += Engine_Changed;
        Engine.VisibleNodesChanged += Engine_Changed;
        Engine.CurrentCellChanged += Engine_Changed;
        Engine.CurrentRowChanged += Engine_Changed;
        Engine.SelectionChanged += Engine_Changed;
        Engine.EditingCellChanged += Engine_Changed;
        Engine.ViewportChanged += Engine_Changed;
        Engine.SummariesChanged += Engine_Changed;
    }
    void DetachEngine(GroupGridEngine Engine)
    {
        if (Engine == null)
            return;

        Engine.DataAdapterChanged -= Engine_Changed;
        Engine.DataChanged -= Engine_Changed;
        Engine.ColumnsChanged -= Engine_Changed;
        Engine.GroupColumnsChanged -= Engine_Changed;
        Engine.VisibleNodesChanged -= Engine_Changed;
        Engine.CurrentCellChanged -= Engine_Changed;
        Engine.CurrentRowChanged -= Engine_Changed;
        Engine.SelectionChanged -= Engine_Changed;
        Engine.EditingCellChanged -= Engine_Changed;
        Engine.ViewportChanged -= Engine_Changed;
        Engine.SummariesChanged -= Engine_Changed;
    }
    void UpdateViewport(Size Size)
    {
        if (fEngine == null)
            return;

        double ContentWidth = Math.Max(0, Size.Width);
        double BodyHeight = Math.Max(0, Size.Height - FixedBandHeight);
        fHasHorizontalScrollBar = NeedsHorizontalScrollBar(ContentWidth);
        if (fHasHorizontalScrollBar)
            BodyHeight = Math.Max(0, BodyHeight - fEngine.LayoutMetrics.HorizontalScrollBarHeight);

        int Count = fEngine.LayoutMetrics.RowHeight <= 0
            ? 0
            : Math.Min(fEngine.VisibleNodeCount, (int)Math.Floor(BodyHeight / fEngine.LayoutMetrics.RowHeight));
        bool HasVerticalScrollBar = fEngine.VisibleNodeCount > Count && fEngine.LayoutMetrics.VerticalScrollBarWidth > 0;
        ContentWidth = Math.Max(0, Size.Width - (HasVerticalScrollBar ? fEngine.LayoutMetrics.VerticalScrollBarWidth : 0));
        bool NeedsHorizontal = NeedsHorizontalScrollBar(ContentWidth);
        if (NeedsHorizontal != fHasHorizontalScrollBar)
        {
            fHasHorizontalScrollBar = NeedsHorizontal;
            BodyHeight = Math.Max(0, Size.Height - FixedBandHeight - (fHasHorizontalScrollBar ? fEngine.LayoutMetrics.HorizontalScrollBarHeight : 0));
            Count = fEngine.LayoutMetrics.RowHeight <= 0
                ? 0
                : Math.Min(fEngine.VisibleNodeCount, (int)Math.Floor(BodyHeight / fEngine.LayoutMetrics.RowHeight));
        }

        fFirstVisibleNodeIndex = ClampFirstVisibleNodeIndex(fFirstVisibleNodeIndex, Count);
        ClampHorizontalOffset(ContentWidth);
        GroupGridViewport Viewport = Count <= 0
            ? GroupGridViewport.Empty
            : new GroupGridViewport(fFirstVisibleNodeIndex, fFirstVisibleNodeIndex + Count - 1);

        fEngine.SetViewport(Viewport, BodyHeight);
    }
    int ClampFirstVisibleNodeIndex(int Value, int ViewportCount)
    {
        if (fEngine == null || fEngine.VisibleNodeCount == 0 || ViewportCount <= 0)
            return 0;

        int Max = Math.Max(0, fEngine.VisibleNodeCount - ViewportCount);
        if (Value < 0)
            return 0;
        if (Value > Max)
            return Max;

        return Value;
    }
    bool ScrollViewport(int Delta)
    {
        if (fEngine == null || Delta == 0)
            return false;

        return SetFirstVisibleNodeIndexCore(fFirstVisibleNodeIndex + Delta);
    }
    bool ScrollCurrentCellIntoViewCore()
    {
        if (fEngine == null || fEngine.CurrentCell.IsEmpty || fEngine.Viewport.IsEmpty)
            return false;

        bool Result = false;
        int VisibleNodeIndex = fEngine.IndexOfVisibleRow(fEngine.CurrentCell.RowIndex);
        if (VisibleNodeIndex >= 0)
        {
            if (VisibleNodeIndex < fEngine.Viewport.FirstVisibleNodeIndex)
                Result = SetFirstVisibleNodeIndexCore(VisibleNodeIndex);
            else if (VisibleNodeIndex > fEngine.Viewport.LastVisibleNodeIndex)
                Result = SetFirstVisibleNodeIndexCore(VisibleNodeIndex - fEngine.Viewport.Count + 1);
        }

        Result = ScrollCurrentColumnIntoViewCore() || Result;
        return Result;
    }
    double GetColumnLeft(GroupGridColumn Column)
    {
        double Result = 0;
        foreach (GroupGridColumn Item in fEngine.GetVisibleValueColumns())
        {
            if (ReferenceEquals(Item, Column))
                return Result;

            Result += Math.Max(Item.MinWidth, Item.Width);
        }

        return -1;
    }
    bool ScrollCurrentColumnIntoViewCore()
    {
        if (fEngine == null || fEngine.CurrentCell.IsEmpty || !HasHorizontalScrollBar())
            return false;

        double ColumnLeft = GetColumnLeft(fEngine.CurrentCell.Column);
        if (ColumnLeft < 0)
            return false;

        double ColumnWidth = Math.Max(fEngine.CurrentCell.Column.MinWidth, fEngine.CurrentCell.Column.Width);
        double ColumnRight = ColumnLeft + ColumnWidth;
        double ContentWidth = GetBodyContentWidth();
        if (ColumnLeft < fHorizontalOffset)
            return SetHorizontalOffsetCore(ColumnLeft);
        if (ColumnRight > fHorizontalOffset + ContentWidth)
            return SetHorizontalOffsetCore(ColumnRight - ContentWidth);

        return false;
    }
    bool SetFirstVisibleNodeIndexCore(int Value)
    {
        if (fEngine == null)
            return false;

        int NewFirstVisibleNodeIndex = ClampFirstVisibleNodeIndex(Value, fEngine.Viewport.Count);
        if (NewFirstVisibleNodeIndex == fFirstVisibleNodeIndex)
            return false;

        fFirstVisibleNodeIndex = NewFirstVisibleNodeIndex;
        UpdateViewport(Bounds.Size);
        InvalidateVisual();
        return true;
    }
    double GetTotalColumnsWidth()
    {
        if (fEngine == null)
            return 0;

        return fEngine.GetVisibleValueColumns().Sum(Column => Math.Max(Column.MinWidth, Column.Width));
    }
    bool NeedsHorizontalScrollBar(double ContentWidth)
    {
        return fEngine != null
               && fEngine.LayoutMetrics.HorizontalScrollBarHeight > 0
               && GetTotalColumnsWidth() > Math.Max(0, ContentWidth) + 0.1;
    }
    bool HasHorizontalScrollBar()
    {
        return fHasHorizontalScrollBar;
    }
    void ClampHorizontalOffset(double ContentWidth)
    {
        double Max = Math.Max(0, GetTotalColumnsWidth() - Math.Max(0, ContentWidth));
        if (fHorizontalOffset < 0)
            fHorizontalOffset = 0;
        if (fHorizontalOffset > Max)
            fHorizontalOffset = Max;
    }
    bool SetHorizontalOffsetCore(double Value)
    {
        if (fEngine == null)
            return false;

        double ContentWidth = GetBodyContentWidth();
        double Max = Math.Max(0, GetTotalColumnsWidth() - ContentWidth);
        double NewValue = Math.Clamp(Value, 0, Max);
        if (Math.Abs(NewValue - fHorizontalOffset) < 0.1)
            return false;

        fHorizontalOffset = NewValue;
        InvalidateVisual();
        return true;
    }
    bool HasVerticalScrollBar()
    {
        return fEngine != null
               && !fEngine.Viewport.IsEmpty
               && fEngine.VisibleNodeCount > fEngine.Viewport.Count
               && fEngine.LayoutMetrics.VerticalScrollBarWidth > 0;
    }
    double GetBodyTop()
    {
        if (fEngine == null)
            return 0;

        return fEngine.LayoutMetrics.ToolBarHeight
               + fEngine.LayoutMetrics.GroupPanelHeight
               + fEngine.LayoutMetrics.ColumnHeaderHeight
               + fEngine.LayoutMetrics.FilterRowHeight;
    }
    double GetGroupPanelTop()
    {
        if (fEngine == null)
            return 0;

        return fEngine.LayoutMetrics.ToolBarHeight;
    }
    Rect GetGroupPanelRect()
    {
        if (fEngine == null)
            return default;

        return new Rect(0, GetGroupPanelTop(), GetBodyContentWidth(), fEngine.LayoutMetrics.GroupPanelHeight);
    }
    Rect GetColumnHeaderRect()
    {
        if (fEngine == null)
            return default;

        double Y = fEngine.LayoutMetrics.ToolBarHeight + fEngine.LayoutMetrics.GroupPanelHeight;
        return new Rect(0, Y, GetBodyContentWidth(), fEngine.LayoutMetrics.ColumnHeaderHeight);
    }
    double GetBodyHeight()
    {
        if (fEngine == null)
            return 0;

        return Math.Max(0, Bounds.Height - GetBodyTop() - fEngine.LayoutMetrics.FooterSummaryHeight - GetHorizontalScrollBarHeight());
    }
    double GetBodyContentWidth()
    {
        if (fEngine == null)
            return Bounds.Width;

        double ScrollBarWidth = HasVerticalScrollBar() ? fEngine.LayoutMetrics.VerticalScrollBarWidth : 0;
        return Math.Max(0, Bounds.Width - ScrollBarWidth);
    }
    double GetHorizontalScrollBarHeight()
    {
        return HasHorizontalScrollBar() ? fEngine.LayoutMetrics.HorizontalScrollBarHeight : 0;
    }
    Rect GetVerticalScrollTrackRect()
    {
        if (!HasVerticalScrollBar())
            return default;

        double Width = fEngine.LayoutMetrics.VerticalScrollBarWidth;
        return new Rect(Math.Max(0, Bounds.Width - Width), GetBodyTop(), Width, GetBodyHeight());
    }
    Rect GetVerticalScrollThumbRect()
    {
        Rect TrackRect = GetVerticalScrollTrackRect();
        if (TrackRect.Width <= 0 || TrackRect.Height <= 0)
            return default;

        double ThumbHeight = Math.Max(fEngine.LayoutMetrics.VerticalScrollThumbMinHeight, TrackRect.Height * fEngine.Viewport.Count / fEngine.VisibleNodeCount);
        ThumbHeight = Math.Min(TrackRect.Height, ThumbHeight);
        int MaxFirstVisibleNodeIndex = Math.Max(0, fEngine.VisibleNodeCount - fEngine.Viewport.Count);
        double Top = TrackRect.Y;
        if (MaxFirstVisibleNodeIndex > 0 && TrackRect.Height > ThumbHeight)
            Top += (TrackRect.Height - ThumbHeight) * fFirstVisibleNodeIndex / MaxFirstVisibleNodeIndex;

        return new Rect(TrackRect.X + 2, Top + 2, Math.Max(0, TrackRect.Width - 4), Math.Max(0, ThumbHeight - 4));
    }
    bool SetFirstVisibleNodeIndexFromVerticalScroll(double Y)
    {
        Rect TrackRect = GetVerticalScrollTrackRect();
        Rect ThumbRect = GetVerticalScrollThumbRect();
        int MaxFirstVisibleNodeIndex = Math.Max(0, fEngine.VisibleNodeCount - fEngine.Viewport.Count);
        double Range = TrackRect.Height - ThumbRect.Height;
        if (Range <= 0 || MaxFirstVisibleNodeIndex <= 0)
            return false;

        double Ratio = (Y - TrackRect.Y - fVerticalScrollDragOffset) / Range;
        int NewFirstVisibleNodeIndex = (int)Math.Round(Math.Clamp(Ratio, 0, 1) * MaxFirstVisibleNodeIndex);
        return SetFirstVisibleNodeIndexCore(NewFirstVisibleNodeIndex);
    }
    Rect GetHorizontalScrollTrackRect()
    {
        if (!HasHorizontalScrollBar())
            return default;

        return new Rect(0, GetBodyTop() + GetBodyHeight() + fEngine.LayoutMetrics.FooterSummaryHeight, GetBodyContentWidth(), fEngine.LayoutMetrics.HorizontalScrollBarHeight);
    }
    Rect GetHorizontalScrollThumbRect()
    {
        Rect TrackRect = GetHorizontalScrollTrackRect();
        if (TrackRect.Width <= 0 || TrackRect.Height <= 0)
            return default;

        double TotalWidth = GetTotalColumnsWidth();
        double ThumbWidth = Math.Max(fEngine.LayoutMetrics.HorizontalScrollThumbMinWidth, TrackRect.Width * TrackRect.Width / TotalWidth);
        ThumbWidth = Math.Min(TrackRect.Width, ThumbWidth);
        double MaxOffset = Math.Max(0, TotalWidth - TrackRect.Width);
        double Left = TrackRect.X;
        if (MaxOffset > 0 && TrackRect.Width > ThumbWidth)
            Left += (TrackRect.Width - ThumbWidth) * fHorizontalOffset / MaxOffset;

        return new Rect(Left + 2, TrackRect.Y + 2, Math.Max(0, ThumbWidth - 4), Math.Max(0, TrackRect.Height - 4));
    }
    bool SetHorizontalOffsetFromScroll(double X)
    {
        Rect TrackRect = GetHorizontalScrollTrackRect();
        Rect ThumbRect = GetHorizontalScrollThumbRect();
        double MaxOffset = Math.Max(0, GetTotalColumnsWidth() - TrackRect.Width);
        double Range = TrackRect.Width - ThumbRect.Width;
        if (Range <= 0 || MaxOffset <= 0)
            return false;

        double Ratio = (X - TrackRect.X - fHorizontalScrollDragOffset) / Range;
        return SetHorizontalOffsetCore(Math.Clamp(Ratio, 0, 1) * MaxOffset);
    }
    bool SetColumnWidth(GroupGridColumn Column, double Width)
    {
        if (Column == null)
            return false;

        double NewWidth = Math.Max(Column.MinWidth, Width);
        if (Math.Abs(NewWidth - Column.Width) < 0.1)
            return false;

        Column.Width = NewWidth;
        UpdateViewport(Bounds.Size);
        InvalidateVisual();
        return true;
    }
    bool GetColumnDropInfo(Point Point, out int ColumnIndex, out double X)
    {
        ColumnIndex = -1;
        X = 0;

        IReadOnlyList<GroupGridColumn> Columns = fEngine.GetVisibleValueColumns();
        if (Columns.Count == 0)
            return false;

        double LogicalX = Math.Clamp(Point.X, 0, GetBodyContentWidth()) + fHorizontalOffset;
        double Left = 0;
        GroupGridColumn LastColumn = null;
        double LastRight = 0;

        foreach (GroupGridColumn Column in Columns)
        {
            double Width = Math.Max(Column.MinWidth, Column.Width);
            double Right = Left + Width;
            if (LogicalX < Left + (Width / 2))
            {
                ColumnIndex = fEngine.Columns.IndexOf(Column);
                X = Math.Clamp(Left - fHorizontalOffset, 0, GetBodyContentWidth());
                return ColumnIndex >= 0;
            }

            LastColumn = Column;
            LastRight = Right;
            Left = Right;
        }

        ColumnIndex = fEngine.Columns.IndexOf(LastColumn) + 1;
        X = Math.Clamp(LastRight - fHorizontalOffset, 0, GetBodyContentWidth());
        return ColumnIndex > 0;
    }
    bool GetGroupDropInfo(Point Point, out int GroupIndex, out double X)
    {
        GroupIndex = -1;
        X = 0;
        Rect PanelRect = GetGroupPanelRect();
        if (!PanelRect.Contains(Point) || fColumnDragColumn == null || !fColumnDragColumn.CanUserGroup)
            return false;

        X = 6;
        for (int Index = 0; Index < fEngine.GroupColumns.Count; Index++)
        {
            GroupGridColumn Column = fEngine.GroupColumns[Index];
            string Text = string.IsNullOrWhiteSpace(Column.Header) ? Column.Name : Column.Header;
            double ItemWidth = Math.Max(80, Text.Length * 8 + 24);
            if (Point.X < X + (ItemWidth / 2))
            {
                GroupIndex = Index;
                return true;
            }

            X += ItemWidth + 6;
        }

        GroupIndex = fEngine.GroupColumns.Count;
        return true;
    }
    bool GetGroupPanelColumnAt(Point Point, out GroupGridColumn Column, out int GroupIndex)
    {
        Column = null;
        GroupIndex = -1;
        Rect PanelRect = GetGroupPanelRect();
        if (!PanelRect.Contains(Point))
            return false;

        double X = 6;
        for (int Index = 0; Index < fEngine.GroupColumns.Count; Index++)
        {
            GroupGridColumn Item = fEngine.GroupColumns[Index];
            string Text = string.IsNullOrWhiteSpace(Item.Header) ? Item.Name : Item.Header;
            double ItemWidth = Math.Max(80, Text.Length * 8 + 24);
            Rect Rect = new(X, PanelRect.Y + 5, ItemWidth, Math.Max(0, PanelRect.Height - 10));
            if (Rect.Contains(Point))
            {
                Column = Item;
                GroupIndex = Index;
                return true;
            }

            X += ItemWidth + 6;
        }

        return false;
    }
    bool IsInsideGrid(Point Point)
    {
        return Point.X >= 0 && Point.Y >= 0 && Point.X < Bounds.Width && Point.Y < Bounds.Height;
    }
    int IndexOfGroupColumn(GroupGridColumn Column)
    {
        for (int Index = 0; Index < fEngine.GroupColumns.Count; Index++)
            if (ReferenceEquals(fEngine.GroupColumns[Index], Column))
                return Index;

        return -1;
    }
    bool MoveDraggedColumn()
    {
        if (fColumnDragColumn == null || fColumnDragDropIndex < 0)
            return false;

        int OldIndex = fEngine.Columns.IndexOf(fColumnDragColumn);
        if (OldIndex < 0)
            return false;

        int NewIndex = fColumnDragDropIndex;
        if (NewIndex > OldIndex)
            NewIndex--;
        NewIndex = Math.Clamp(NewIndex, 0, fEngine.Columns.Count - 1);

        return fEngine.MoveColumn(fColumnDragColumn, NewIndex);
    }
    bool MoveGroupedColumn()
    {
        if (fColumnDragColumn == null || fColumnDragGroupDropIndex < 0)
            return false;

        int OldIndex = IndexOfGroupColumn(fColumnDragColumn);
        if (OldIndex < 0)
            return false;

        int NewIndex = fColumnDragGroupDropIndex;
        if (NewIndex > OldIndex)
            NewIndex--;
        NewIndex = Math.Clamp(NewIndex, 0, fEngine.GroupColumns.Count - 1);
        return fEngine.MoveGroupedColumn(fColumnDragColumn, NewIndex);
    }
    bool UngroupDraggedColumnToHeader()
    {
        if (fColumnDragColumn == null || fColumnDragDropIndex < 0)
            return false;

        int NewIndex = Math.Clamp(fColumnDragDropIndex, 0, fEngine.Columns.Count - 1);
        bool Ungrouped = fEngine.UngroupColumn(fColumnDragColumn);
        bool Moved = fEngine.MoveColumn(fColumnDragColumn, NewIndex);
        return Ungrouped || Moved;
    }
    Type FindListItemType(object ItemsSource)
    {
        if (ItemsSource == null)
            return null;

        Type SourceType = ItemsSource.GetType();
        Type ListType = SourceType
            .GetInterfaces()
            .Concat(new[] { SourceType })
            .FirstOrDefault(Item => Item.IsGenericType && Item.GetGenericTypeDefinition() == typeof(IList<>));

        return ListType == null ? null : ListType.GetGenericArguments()[0];
    }
    IGroupGridDataAdapter CreateDataAdapter(object ItemsSource)
    {
        if (ItemsSource == null)
            return null;

        Type ItemType = FindListItemType(ItemsSource);
        if (ItemType == null)
            throw new ArgumentException("ItemsSource must implement IList<T>.", nameof(ItemsSource));

        Type AdapterType = typeof(GroupGridListDataAdapter<>).MakeGenericType(ItemType);
        return (IGroupGridDataAdapter)Activator.CreateInstance(AdapterType, ItemsSource);
    }
    GroupGridColumn CreateAutoColumn(PropertyInfo Property)
    {
        Type Type = Nullable.GetUnderlyingType(Property.PropertyType) ?? Property.PropertyType;
        GroupGridColumn Column;

        if (Type == typeof(bool))
            Column = new GroupGridCheckBoxColumn();
        else if (Type == typeof(DateTime) || Type == typeof(DateTimeOffset))
            Column = new GroupGridDateColumn();
        else if (Type == typeof(byte)
                 || Type == typeof(short)
                 || Type == typeof(int)
                 || Type == typeof(long)
                 || Type == typeof(float)
                 || Type == typeof(double)
                 || Type == typeof(decimal))
            Column = new GroupGridNumberColumn();
        else
            Column = new GroupGridTextColumn();

        Column.Name = Property.Name;
        Column.Header = Property.Name;
        Column.ValueType = Type;
        Column.IsReadOnly = !Property.CanWrite;
        return Column;
    }
    void GenerateColumnsFromItemType(Type ItemType)
    {
        if (!fAutoGenerateColumns || ItemType == null || Columns.Count > 0)
            return;

        foreach (PropertyInfo Property in ItemType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            if (Property.GetIndexParameters().Length == 0)
                Columns.Add(CreateAutoColumn(Property));
    }
    void SetItemsSource(object Value)
    {
        if (ReferenceEquals(fItemsSource, Value))
            return;

        if (fOwnedDataAdapter != null)
        {
            fOwnedDataAdapter.Dispose();
            fOwnedDataAdapter = null;
        }

        fItemsSource = Value;
        GenerateColumnsFromItemType(FindListItemType(Value));

        IGroupGridDataAdapter Adapter = CreateDataAdapter(Value);
        fOwnedDataAdapter = Adapter as IDisposable;
        DataAdapter = Adapter;
    }
    bool IsCurrentCell(GroupGridRowInfo RowInfo, GroupGridColumn Column)
    {
        return RowInfo.IsDataRow && fEngine.CurrentCell == new GroupGridCell(RowInfo.RowIndex, Column);
    }
    bool IsSelectedRow(GroupGridRowInfo RowInfo)
    {
        return RowInfo.IsDataRow && fEngine.IsSelectedRow(RowInfo.RowIndex);
    }
    bool IsEditingCell(GroupGridRowInfo RowInfo, GroupGridColumn Column)
    {
        return RowInfo.IsDataRow && fEngine.EditingCell == new GroupGridCell(RowInfo.RowIndex, Column);
    }
    Rect InsetRect(Rect Rect, double Value)
    {
        return new Rect(Rect.X + Value, Rect.Y + Value, Math.Max(0, Rect.Width - (Value * 2)), Math.Max(0, Rect.Height - (Value * 2)));
    }
    FormattedText CreateText(string Text, IBrush Brush, double MaxWidth, FontWeight Weight = FontWeight.Normal)
    {
        FormattedText Result = new(Text ?? string.Empty, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Segoe UI", FontStyle.Normal, Weight, FontStretch.Normal), 12, Brush);
        Result.MaxTextWidth = Math.Max(0, MaxWidth);
        Result.MaxLineCount = 1;
        Result.Trimming = TextTrimming.CharacterEllipsis;
        return Result;
    }
    double GetTextX(Rect Rect, FormattedText Text, GroupGridCellHorizontalAlignment Alignment)
    {
        switch (Alignment)
        {
            case GroupGridCellHorizontalAlignment.Center:
                return Rect.X + Math.Max(4, (Rect.Width - Text.Width) / 2);
            case GroupGridCellHorizontalAlignment.Right:
                return Rect.Right - Text.Width - 4;
        }

        return Rect.X + 4;
    }
    void DrawText(DrawingContext Context, string Text, Rect Rect, IBrush Brush, FontWeight Weight = FontWeight.Normal, GroupGridCellHorizontalAlignment Alignment = GroupGridCellHorizontalAlignment.Left)
    {
        if (string.IsNullOrEmpty(Text) || Rect.Width <= 4 || Rect.Height <= 4)
            return;

        using (Context.PushClip(Rect))
        {
            FormattedText FormattedText = CreateText(Text, Brush, Rect.Width - 8, Weight);
            double X = GetTextX(Rect, FormattedText, Alignment);
            double Y = Rect.Y + Math.Max(2, (Rect.Height - FormattedText.Height) / 2);
            Context.DrawText(FormattedText, new Point(X, Y));
        }
    }
    void DrawBand(DrawingContext Context, Rect Rect, IBrush Brush)
    {
        Context.DrawRectangle(Brush, fLinePen, Rect);
    }
    void DrawColumnHeaderCell(DrawingContext Context, GroupGridColumn Column, Rect Rect, IBrush Brush)
    {
        DrawBand(Context, Rect, Brush);

        string Text = string.IsNullOrWhiteSpace(Column.Header) ? Column.Name : Column.Header;
        DrawText(Context, Text, Rect, fTextBrush, FontWeight.SemiBold);
    }
    void DrawExpander(DrawingContext Context, Rect Rect, bool IsExpanded)
    {
        double Size = Math.Min(10, Math.Max(6, Math.Min(Rect.Width, Rect.Height) - 8));
        double CenterX = Rect.X + (Rect.Width / 2);
        double CenterY = Rect.Y + (Rect.Height / 2);
        StreamGeometry Geometry = new();

        using (StreamGeometryContext GeometryContext = Geometry.Open())
        {
            if (IsExpanded)
            {
                GeometryContext.BeginFigure(new Point(CenterX - (Size / 2), CenterY - (Size / 4)), true);
                GeometryContext.LineTo(new Point(CenterX + (Size / 2), CenterY - (Size / 4)));
                GeometryContext.LineTo(new Point(CenterX, CenterY + (Size / 2)));
            }
            else
            {
                GeometryContext.BeginFigure(new Point(CenterX - (Size / 4), CenterY - (Size / 2)), true);
                GeometryContext.LineTo(new Point(CenterX - (Size / 4), CenterY + (Size / 2)));
                GeometryContext.LineTo(new Point(CenterX + (Size / 2), CenterY));
            }

            GeometryContext.EndFigure(true);
        }

        Context.DrawGeometry(fMutedTextBrush, null, Geometry);
    }
    double DrawColumns(DrawingContext Context, double Y, double Height, double ContentWidth, IBrush Brush, bool DrawFilterText)
    {
        double X = -fHorizontalOffset;
        using (Context.PushClip(new Rect(0, Y, ContentWidth, Height)))
        {
            foreach (GroupGridColumn Column in fEngine.GetVisibleValueColumns())
            {
                double ColumnWidth = Math.Max(Column.MinWidth, Column.Width);
                if (X >= ContentWidth)
                    break;

                if (X + ColumnWidth > 0)
                {
                    Rect Rect = new(X, Y, ColumnWidth, Height);
                    if (DrawFilterText)
                        DrawBand(Context, Rect, Brush);
                    else
                        DrawColumnHeaderCell(Context, Column, Rect, Brush);
                }

                X += ColumnWidth;
            }
        }

        return X;
    }
    void DrawGroupPanel(DrawingContext Context, double Y, double Height, double Width)
    {
        DrawBand(Context, new Rect(0, Y, Width, Height), fGroupPanelBrush);

        double X = 6;
        foreach (GroupGridColumn Column in fEngine.GroupColumns)
        {
            string Text = string.IsNullOrWhiteSpace(Column.Header) ? Column.Name : Column.Header;
            double ItemWidth = Math.Max(80, Text.Length * 8 + 24);
            Rect Rect = new(X, Y + 5, ItemWidth, Math.Max(0, Height - 10));
            Context.DrawRectangle(fHeaderBrush, fLinePen, Rect, 3, 3);
            DrawText(Context, Text, Rect, fTextBrush, FontWeight.SemiBold);
            X += ItemWidth + 6;
        }
    }
    void DrawBody(DrawingContext Context, double Y, double Width)
    {
        double RowHeight = fEngine.LayoutMetrics.RowHeight;
        if (RowHeight <= 0 || fEngine.Viewport.IsEmpty)
            return;

        for (int Index = 0; Index < fEngine.Viewport.Count; Index++)
        {
            int VisibleNodeIndex = fEngine.Viewport.FirstVisibleNodeIndex + Index;
            GroupGridRowInfo RowInfo = fEngine.GetVisibleRowInfo(VisibleNodeIndex);
            Rect RowRect = new(0, Y + (Index * RowHeight), Width, RowHeight);

            if (RowInfo.IsGroup)
                DrawGroupRow(Context, RowRect, VisibleNodeIndex, RowInfo);
            else
                DrawValueRow(Context, RowRect, VisibleNodeIndex, RowInfo);
        }
    }
    string GetGroupSummaryText(int VisibleNodeIndex, GroupGridColumn Column)
    {
        GroupGridSummaryValue Summary = fEngine.GetGroupSummary(VisibleNodeIndex, Column);
        return Summary.AggregateKind == GroupGridAggregateKind.None
            ? string.Empty
            : Column.FormatSummaryValue(Summary);
    }
    double GetFirstCollapsedGroupSummaryX(int VisibleNodeIndex, double RowWidth)
    {
        double X = -fHorizontalOffset;
        foreach (GroupGridColumn Column in fEngine.GetVisibleValueColumns())
        {
            double Width = Math.Max(Column.MinWidth, Column.Width);
            if (X >= RowWidth)
                break;

            if (X + Width > 0 && !string.IsNullOrWhiteSpace(GetGroupSummaryText(VisibleNodeIndex, Column)))
                return Math.Max(0, X);

            X += Width;
        }

        return RowWidth;
    }
    void DrawCollapsedGroupSummaries(DrawingContext Context, Rect RowRect, int VisibleNodeIndex)
    {
        double X = -fHorizontalOffset;
        using (Context.PushClip(RowRect))
        {
            foreach (GroupGridColumn Column in fEngine.GetVisibleValueColumns())
            {
                double Width = Math.Max(Column.MinWidth, Column.Width);
                if (X >= RowRect.Width)
                    break;

                if (X + Width > 0)
                {
                    string Text = GetGroupSummaryText(VisibleNodeIndex, Column);
                    if (!string.IsNullOrWhiteSpace(Text))
                    {
                        Rect CellRect = new(X, RowRect.Y, Width, RowRect.Height);
                        DrawText(Context, Text, CellRect, fMutedTextBrush, FontWeight.SemiBold, Column.HorizontalAlignment);
                    }
                }

                X += Width;
            }
        }
    }
    void DrawGroupRow(DrawingContext Context, Rect RowRect, int VisibleNodeIndex, GroupGridRowInfo RowInfo)
    {
        DrawBand(Context, RowRect, fGroupRowBrush);

        double X = Math.Max(0, RowInfo.Level) * fEngine.LayoutMetrics.GroupIndentWidth;
        Rect ExpanderRect = new(X, RowRect.Y, fEngine.LayoutMetrics.GroupExpanderWidth, RowRect.Height);
        double HeaderRight = RowRect.Width;
        if (!RowInfo.IsExpanded)
            HeaderRight = Math.Min(HeaderRight, GetFirstCollapsedGroupSummaryX(VisibleNodeIndex, RowRect.Width));

        DrawExpander(Context, ExpanderRect, RowInfo.IsExpanded);
        DrawText(Context, fEngine.GetGroupHeaderText(VisibleNodeIndex), new Rect(ExpanderRect.Right, RowRect.Y, Math.Max(0, HeaderRight - ExpanderRect.Right), RowRect.Height), fTextBrush, FontWeight.SemiBold);

        if (!RowInfo.IsExpanded)
            DrawCollapsedGroupSummaries(Context, RowRect, VisibleNodeIndex);
    }
    void DrawValueRow(DrawingContext Context, Rect RowRect, int VisibleNodeIndex, GroupGridRowInfo RowInfo)
    {
        IBrush RowBrush = RowInfo.IsGroupSummary ? fGroupSummaryBrush : fBackgroundBrush;
        if (IsSelectedRow(RowInfo))
            RowBrush = fSelectedBrush;

        DrawBand(Context, RowRect, RowBrush);

        double X = -fHorizontalOffset;
        using (Context.PushClip(RowRect))
        {
            foreach (GroupGridColumn Column in fEngine.GetVisibleValueColumns())
            {
                double Width = Math.Max(Column.MinWidth, Column.Width);
                if (X >= RowRect.Width)
                    break;

                if (X + Width > 0)
                {
                    Rect CellRect = new(X, RowRect.Y, Width, RowRect.Height);
                    IBrush CellBrush = RowBrush;
                    if (IsSelectedRow(RowInfo))
                        CellBrush = fSelectedBrush;
                    if (IsCurrentCell(RowInfo, Column))
                        CellBrush = fCurrentBrush;
                    if (IsEditingCell(RowInfo, Column))
                        CellBrush = fEditingBrush;

                    Context.DrawRectangle(CellBrush, fLinePen, CellRect);
                    DrawText(Context, fEngine.GetDisplayText(VisibleNodeIndex, Column), CellRect, RowInfo.IsGroupSummary ? fMutedTextBrush : fTextBrush, FontWeight.Normal, Column.HorizontalAlignment);

                    if (IsCurrentCell(RowInfo, Column))
                        Context.DrawRectangle(null, fCurrentPen, CellRect);
                    if (IsEditingCell(RowInfo, Column))
                        Context.DrawRectangle(null, fEditingPen, InsetRect(CellRect, 1));
                }

                X += Width;
            }
        }
    }
    void DrawVerticalScrollBar(DrawingContext Context)
    {
        if (!HasVerticalScrollBar())
            return;

        Rect TrackRect = GetVerticalScrollTrackRect();
        Rect ThumbRect = GetVerticalScrollThumbRect();
        Context.DrawRectangle(fScrollBarTrackBrush, fLinePen, TrackRect);
        Context.DrawRectangle(fScrollBarThumbBrush, null, ThumbRect, 4, 4);
    }
    void DrawHorizontalScrollBar(DrawingContext Context)
    {
        if (!HasHorizontalScrollBar())
            return;

        Rect TrackRect = GetHorizontalScrollTrackRect();
        Rect ThumbRect = GetHorizontalScrollThumbRect();
        Context.DrawRectangle(fScrollBarTrackBrush, fLinePen, TrackRect);
        Context.DrawRectangle(fScrollBarThumbBrush, null, ThumbRect, 4, 4);

        if (HasVerticalScrollBar())
            DrawBand(Context, new Rect(TrackRect.Right, TrackRect.Y, Bounds.Width - TrackRect.Right, TrackRect.Height), fScrollBarTrackBrush);
    }
    void DrawColumnResizeGuide(DrawingContext Context)
    {
        if (!fIsColumnResizing)
            return;

        double X = Math.Clamp(fColumnResizeCurrentX, 0, GetBodyContentWidth());
        double Top = fEngine.LayoutMetrics.ToolBarHeight + fEngine.LayoutMetrics.GroupPanelHeight;
        double Bottom = GetBodyTop() + GetBodyHeight() + fEngine.LayoutMetrics.FooterSummaryHeight;
        Context.DrawLine(fResizePen, new Point(X, Top), new Point(X, Bottom));
    }
    void DrawColumnDragGuide(DrawingContext Context)
    {
        if (!fIsColumnDragActive)
            return;

        if (fColumnDragGroupDropIndex < 0 && fColumnDragDropIndex < 0)
            return;

        double X = Math.Clamp(fColumnDragDropX, 0, GetBodyContentWidth());
        if (fColumnDragGroupDropIndex >= 0)
        {
            Rect PanelRect = GetGroupPanelRect();
            Context.DrawLine(fResizePen, new Point(X, PanelRect.Y + 4), new Point(X, PanelRect.Bottom - 4));
            return;
        }

        double Top = fEngine.LayoutMetrics.ToolBarHeight + fEngine.LayoutMetrics.GroupPanelHeight;
        double Bottom = GetBodyTop() + GetBodyHeight() + fEngine.LayoutMetrics.FooterSummaryHeight;
        Context.DrawLine(fResizePen, new Point(X, Top), new Point(X, Bottom));
    }
    void DrawColumnDragGhost(DrawingContext Context)
    {
        if (!fIsColumnDragActive || fColumnDragColumn == null)
            return;

        double Width = Math.Max(fColumnDragColumn.MinWidth, fColumnDragColumn.Width);
        double Height = fEngine.LayoutMetrics.ColumnHeaderHeight;
        double X = Math.Clamp(fColumnDragCurrentX - (Width / 2), 0, Math.Max(0, Bounds.Width - Width));
        double Y = Math.Clamp(fColumnDragCurrentY - (Height / 2), fEngine.LayoutMetrics.ToolBarHeight, Math.Max(fEngine.LayoutMetrics.ToolBarHeight, Bounds.Height - Height));
        Rect Rect = new(X, Y, Width, Height);
        DrawColumnHeaderCell(Context, fColumnDragColumn, Rect, fColumnDragBrush);
    }
    void DrawFooter(DrawingContext Context, double Y, double Height, double ContentWidth)
    {
        double X = -fHorizontalOffset;
        using (Context.PushClip(new Rect(0, Y, ContentWidth, Height)))
        {
            foreach (GroupGridColumn Column in fEngine.GetVisibleValueColumns())
            {
                double ColumnWidth = Math.Max(Column.MinWidth, Column.Width);
                if (X >= ContentWidth)
                    break;

                if (X + ColumnWidth > 0)
                {
                    Rect Rect = new(X, Y, ColumnWidth, Height);
                    DrawBand(Context, Rect, fFooterBrush);
                    DrawText(Context, fEngine.GetTotalSummaryText(Column), Rect, fMutedTextBrush, FontWeight.SemiBold);
                }

                X += ColumnWidth;
            }
        }
    }
    void HandleHitTest(GroupGridHitTestResult Hit)
    {
        if (Hit == null || fEngine == null)
            return;

        if (Hit.Kind == GroupGridHitTestKind.GroupExpander)
        {
            fEngine.ToggleGroupExpanded(Hit.VisibleNodeIndex);
            return;
        }

        if (Hit.Kind == GroupGridHitTestKind.BodyCell && Hit.RowKind == GroupGridRowKind.DataRow && Hit.HasCell)
        {
            fEngine.SetCurrentCell(Hit.Cell);
            fEngine.SetSelectedCell(Hit.Cell);
        }
    }
    bool HandleVerticalScrollPointerPressed(PointerPressedEventArgs Args, Point Point)
    {
        if (!HasVerticalScrollBar())
            return false;

        Rect TrackRect = GetVerticalScrollTrackRect();
        if (!TrackRect.Contains(Point))
            return false;

        Rect ThumbRect = GetVerticalScrollThumbRect();
        if (ThumbRect.Contains(Point))
        {
            fIsVerticalScrollDragging = true;
            fVerticalScrollDragOffset = Point.Y - ThumbRect.Y;
            Args.Pointer.Capture(this);
            return true;
        }

        int Delta = Point.Y < ThumbRect.Y ? -fEngine.Viewport.Count : fEngine.Viewport.Count;
        ScrollViewport(Delta);
        return true;
    }
    bool HandleHorizontalScrollPointerPressed(PointerPressedEventArgs Args, Point Point)
    {
        if (!HasHorizontalScrollBar())
            return false;

        Rect TrackRect = GetHorizontalScrollTrackRect();
        if (!TrackRect.Contains(Point))
            return false;

        Rect ThumbRect = GetHorizontalScrollThumbRect();
        if (ThumbRect.Contains(Point))
        {
            fIsHorizontalScrollDragging = true;
            fHorizontalScrollDragOffset = Point.X - ThumbRect.X;
            Args.Pointer.Capture(this);
            return true;
        }

        double Delta = Point.X < ThumbRect.X ? -TrackRect.Width : TrackRect.Width;
        SetHorizontalOffsetCore(fHorizontalOffset + Delta);
        return true;
    }
    bool HandleColumnResizePointerPressed(PointerPressedEventArgs Args, Point Point)
    {
        GroupGridHitTestResult Hit = HitTest(Point);
        if (Hit == null || Hit.Kind != GroupGridHitTestKind.ColumnResizer || Hit.Column == null)
            return false;

        fIsColumnResizing = true;
        fColumnResizeColumn = Hit.Column;
        fColumnResizeStartX = Point.X;
        fColumnResizeCurrentX = Point.X;
        fColumnResizeStartWidth = Hit.Column.Width;
        Args.Pointer.Capture(this);
        return true;
    }
    bool HandleColumnResizePointerMoved(Point Point)
    {
        if (!fIsColumnResizing || fColumnResizeColumn == null)
            return false;

        fColumnResizeCurrentX = Point.X;
        SetColumnWidth(fColumnResizeColumn, fColumnResizeStartWidth + Point.X - fColumnResizeStartX);
        return true;
    }
    void EndColumnResize()
    {
        fIsColumnResizing = false;
        fColumnResizeColumn = null;
        fColumnResizeStartX = 0;
        fColumnResizeCurrentX = 0;
        fColumnResizeStartWidth = 0;
        InvalidateVisual();
    }
    bool HandleColumnDragPointerPressed(PointerPressedEventArgs Args, Point Point)
    {
        if (GetGroupPanelColumnAt(Point, out GroupGridColumn GroupColumn, out int GroupIndex))
        {
            fIsColumnDragging = true;
            fIsColumnDragActive = false;
            fColumnDragFromGroupPanel = true;
            fColumnDragColumn = GroupColumn;
            fColumnDragSourceGroupIndex = GroupIndex;
            fColumnDragStartX = Point.X;
            fColumnDragCurrentX = Point.X;
            fColumnDragCurrentY = Point.Y;
            fColumnDragDropX = Point.X;
            fColumnDragGroupDropIndex = GroupIndex;
            Args.Pointer.Capture(this);
            return true;
        }

        GroupGridHitTestResult Hit = HitTest(Point);
        if (Hit == null || Hit.Kind != GroupGridHitTestKind.ColumnHeader || Hit.Column == null || !Hit.Column.CanUserReorder)
            return false;

        fIsColumnDragging = true;
        fIsColumnDragActive = false;
        fColumnDragFromGroupPanel = false;
        fColumnDragColumn = Hit.Column;
        fColumnDragSourceGroupIndex = -1;
        fColumnDragStartX = Point.X;
        fColumnDragCurrentX = Point.X;
        fColumnDragCurrentY = Point.Y;
        fColumnDragDropX = Point.X;
        fColumnDragDropIndex = fEngine.Columns.IndexOf(Hit.Column);
        Args.Pointer.Capture(this);
        return true;
    }
    bool HandleColumnDragPointerMoved(Point Point)
    {
        if (!fIsColumnDragging || fColumnDragColumn == null)
            return false;

        fColumnDragCurrentX = Point.X;
        fColumnDragCurrentY = Point.Y;
        if (!fIsColumnDragActive && Math.Abs(Point.X - fColumnDragStartX) < 4)
            return true;

        fIsColumnDragActive = true;
        if (GetGroupDropInfo(Point, out fColumnDragGroupDropIndex, out fColumnDragDropX))
        {
            fColumnDragDropIndex = -1;
        }
        else if (GetColumnHeaderRect().Contains(Point))
        {
            fColumnDragGroupDropIndex = -1;
            GetColumnDropInfo(Point, out fColumnDragDropIndex, out fColumnDragDropX);
        }
        else
        {
            fColumnDragGroupDropIndex = -1;
            fColumnDragDropIndex = -1;
            fColumnDragDropX = Point.X;
        }

        InvalidateVisual();
        return true;
    }
    void EndColumnDrag()
    {
        if (fIsColumnDragActive)
        {
            if (fColumnDragFromGroupPanel && fColumnDragDropIndex >= 0)
                UngroupDraggedColumnToHeader();
            else if (fColumnDragFromGroupPanel && fColumnDragGroupDropIndex >= 0)
                MoveGroupedColumn();
            else if (fColumnDragFromGroupPanel)
                fEngine.UngroupColumn(fColumnDragColumn);
            else if (fColumnDragGroupDropIndex >= 0)
                fEngine.GroupColumn(fColumnDragColumn, fColumnDragGroupDropIndex);
            else if (!IsInsideGrid(new Point(fColumnDragCurrentX, fColumnDragCurrentY)))
                fEngine.SetColumnVisible(fColumnDragColumn, false);
            else
                MoveDraggedColumn();
        }

        fIsColumnDragging = false;
        fIsColumnDragActive = false;
        fColumnDragFromGroupPanel = false;
        fColumnDragColumn = null;
        fColumnDragStartX = 0;
        fColumnDragCurrentX = 0;
        fColumnDragCurrentY = 0;
        fColumnDragDropX = 0;
        fColumnDragDropIndex = -1;
        fColumnDragGroupDropIndex = -1;
        fColumnDragSourceGroupIndex = -1;
        InvalidateVisual();
    }
    void UpdatePointerCursor(Point Point)
    {
        if (fIsColumnResizing)
        {
            Cursor = new Cursor(StandardCursorType.SizeWestEast);
            return;
        }

        GroupGridHitTestResult Hit = HitTest(Point);
        Cursor = Hit != null && Hit.Kind == GroupGridHitTestKind.ColumnResizer
            ? new Cursor(StandardCursorType.SizeWestEast)
            : null;
    }
    bool IsHorizontalScrollableBand(Point Point)
    {
        if (!HasHorizontalScrollBar())
            return false;

        double Top = fEngine.LayoutMetrics.ToolBarHeight + fEngine.LayoutMetrics.GroupPanelHeight;
        double Bottom = GetBodyTop() + GetBodyHeight() + fEngine.LayoutMetrics.FooterSummaryHeight;
        return Point.X >= 0 && Point.X < GetBodyContentWidth() && Point.Y >= Top && Point.Y < Bottom;
    }
    GroupGridHitTestResult HitTest(Point Point)
    {
        double X = Point.X;
        double Y = Point.Y;
        double HorizontalScrollTop = GetBodyTop() + GetBodyHeight() + fEngine.LayoutMetrics.FooterSummaryHeight;
        if (HasHorizontalScrollBar() && Point.Y >= HorizontalScrollTop && Point.Y < HorizontalScrollTop + fEngine.LayoutMetrics.HorizontalScrollBarHeight)
            return GroupGridHitTestResult.Empty;

        GroupGridHitTestResult Result = fEngine.HitTest(X, Y);
        if (!IsHorizontalScrollableBand(Point))
            return Result;

        if (Result.Kind == GroupGridHitTestKind.GroupExpander)
            return Result;

        return fEngine.HitTest(X + fHorizontalOffset, Y);
    }
    bool HandleKey(KeyEventArgs Args)
    {
        if (fEngine == null)
            return false;

        if (fEngine.IsEditing)
        {
            if (Args.Key == Key.Escape)
                return fEngine.CancelEdit();

            return false;
        }

        switch (Args.Key)
        {
            case Key.Left:
                return fEngine.MoveCurrentColumn(-1);
            case Key.Right:
                return fEngine.MoveCurrentColumn(1);
            case Key.Up:
                return fEngine.MoveCurrentRow(-1);
            case Key.Down:
                return fEngine.MoveCurrentRow(1);
            case Key.Home:
                return fEngine.MoveCurrentToFirstColumn();
            case Key.End:
                return fEngine.MoveCurrentToLastColumn();
            case Key.Tab:
                return fEngine.MoveCurrentToNextEditableCell(!Args.KeyModifiers.HasFlag(KeyModifiers.Shift));
            case Key.Enter:
            case Key.F2:
                return fEngine.BeginEdit();
        }

        return false;
    }

    // ● protected methods
    /// <inheritdoc />
    protected override void OnPointerPressed(PointerPressedEventArgs Args)
    {
        base.OnPointerPressed(Args);

        if (fEngine == null)
            return;

        if (!Args.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;

        Focus(NavigationMethod.Pointer, KeyModifiers.None);

        Point Point = Args.GetPosition(this);
        if (HandleVerticalScrollPointerPressed(Args, Point))
        {
            Args.Handled = true;
            return;
        }
        if (HandleHorizontalScrollPointerPressed(Args, Point))
        {
            Args.Handled = true;
            return;
        }
        if (HandleColumnResizePointerPressed(Args, Point))
        {
            Args.Handled = true;
            return;
        }
        if (HandleColumnDragPointerPressed(Args, Point))
        {
            Args.Handled = true;
            return;
        }

        HandleHitTest(HitTest(Point));
        Args.Handled = true;
    }
    /// <inheritdoc />
    protected override void OnPointerMoved(PointerEventArgs Args)
    {
        base.OnPointerMoved(Args);

        Point Point = Args.GetPosition(this);
        if (HandleColumnResizePointerMoved(Point))
        {
            UpdatePointerCursor(Point);
            Args.Handled = true;
            return;
        }
        if (HandleColumnDragPointerMoved(Point))
        {
            Args.Handled = true;
            return;
        }
        if (fIsVerticalScrollDragging && SetFirstVisibleNodeIndexFromVerticalScroll(Point.Y))
            Args.Handled = true;
        if (fIsHorizontalScrollDragging && SetHorizontalOffsetFromScroll(Point.X))
            Args.Handled = true;
        if (!Args.Handled)
            UpdatePointerCursor(Point);
    }
    /// <inheritdoc />
    protected override void OnPointerReleased(PointerReleasedEventArgs Args)
    {
        base.OnPointerReleased(Args);

        if (!fIsVerticalScrollDragging && !fIsHorizontalScrollDragging && !fIsColumnResizing && !fIsColumnDragging)
            return;

        bool WasColumnResizing = fIsColumnResizing;
        bool WasColumnDragging = fIsColumnDragging;
        fIsVerticalScrollDragging = false;
        fIsHorizontalScrollDragging = false;
        fVerticalScrollDragOffset = 0;
        fHorizontalScrollDragOffset = 0;
        if (WasColumnResizing)
            EndColumnResize();
        if (WasColumnDragging)
            EndColumnDrag();
        Args.Pointer.Capture(null);
        UpdatePointerCursor(Args.GetPosition(this));
        Args.Handled = true;
    }
    /// <inheritdoc />
    protected override void OnPointerExited(PointerEventArgs Args)
    {
        base.OnPointerExited(Args);

        if (!fIsColumnResizing)
            Cursor = null;
    }
    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs Args)
    {
        base.OnKeyDown(Args);

        if (HandleKey(Args))
        {
            ScrollCurrentCellIntoViewCore();
            Args.Handled = true;
        }
    }
    /// <inheritdoc />
    protected override void OnPointerWheelChanged(PointerWheelEventArgs Args)
    {
        base.OnPointerWheelChanged(Args);

        if (Args.Delta.Y == 0)
            return;

        int Delta = Args.Delta.Y < 0 ? 3 : -3;
        if (ScrollViewport(Delta))
            Args.Handled = true;
    }
    /// <inheritdoc />
    protected override Size ArrangeOverride(Size FinalSize)
    {
        UpdateViewport(FinalSize);
        return base.ArrangeOverride(FinalSize);
    }

    // ● constructor
    /// <summary>
    /// Initializes a new instance of the <see cref="GroupGrid"/> class.
    /// </summary>
    public GroupGrid()
    {
        Focusable = true;
        Engine = new GroupGridEngine();
    }

    // ● public methods
    /// <summary>
    /// Sets the first visible node index in the virtual viewport.
    /// </summary>
    /// <param name="VisibleNodeIndex">The first visible node index.</param>
    /// <returns>True if the viewport changed; otherwise, false.</returns>
    public bool SetFirstVisibleNodeIndex(int VisibleNodeIndex)
    {
        return SetFirstVisibleNodeIndexCore(VisibleNodeIndex);
    }
    /// <summary>
    /// Sets the horizontal scroll offset in pixels.
    /// </summary>
    /// <param name="HorizontalOffset">The horizontal scroll offset.</param>
    /// <returns>True if the offset changed; otherwise, false.</returns>
    public bool SetHorizontalOffset(double HorizontalOffset)
    {
        return SetHorizontalOffsetCore(HorizontalOffset);
    }
    /// <summary>
    /// Scrolls the current cell into the virtual viewport.
    /// </summary>
    /// <returns>True if the viewport changed; otherwise, false.</returns>
    public bool ScrollCurrentCellIntoView()
    {
        return ScrollCurrentCellIntoViewCore();
    }
    /// <summary>
    /// Returns the visible columns in display order.
    /// </summary>
    /// <returns>The visible columns.</returns>
    public IReadOnlyList<GroupGridColumn> GetVisibleColumns()
    {
        return fEngine.GetVisibleColumns();
    }
    /// <summary>
    /// Returns the visible value columns in display order.
    /// </summary>
    /// <returns>The visible value columns.</returns>
    public IReadOnlyList<GroupGridColumn> GetVisibleValueColumns()
    {
        return fEngine.GetVisibleValueColumns();
    }
    /// <summary>
    /// Returns all columns in grid column order.
    /// </summary>
    /// <returns>A snapshot list with all columns.</returns>
    public IReadOnlyList<GroupGridColumn> GetAllColumns()
    {
        return fEngine.Columns.ToList();
    }
    /// <summary>
    /// Returns visible value columns in the order they are displayed in the header band.
    /// </summary>
    /// <returns>A snapshot list with visible value columns.</returns>
    public IReadOnlyList<GroupGridColumn> GetVisibleOrderedColumns()
    {
        return fEngine.GetVisibleValueColumns().ToList();
    }
    /// <summary>
    /// Returns hidden columns in grid column order.
    /// </summary>
    /// <returns>A snapshot list with hidden columns.</returns>
    public IReadOnlyList<GroupGridColumn> GetHiddenColumns()
    {
        return fEngine.Columns.Where(Column => !Column.IsVisible).ToList();
    }
    /// <summary>
    /// Returns grouped columns in grouping order.
    /// </summary>
    /// <returns>A snapshot list with grouped columns.</returns>
    public IReadOnlyList<GroupGridColumn> GetGroupedColumns()
    {
        return fEngine.GroupColumns.ToList();
    }
    /// <summary>
    /// Adds a column to the grouping list.
    /// </summary>
    /// <param name="Column">The grid column.</param>
    /// <param name="GroupIndex">The group index, or -1 to append.</param>
    /// <returns>True if the column was grouped; otherwise, false.</returns>
    public bool GroupColumn(GroupGridColumn Column, int GroupIndex = -1)
    {
        return fEngine.GroupColumn(Column, GroupIndex);
    }
    /// <summary>
    /// Removes a column from the grouping list.
    /// </summary>
    /// <param name="Column">The grid column.</param>
    /// <returns>True if the column was ungrouped; otherwise, false.</returns>
    public bool UngroupColumn(GroupGridColumn Column)
    {
        return fEngine.UngroupColumn(Column);
    }
    /// <summary>
    /// Moves a grouped column to a new group index.
    /// </summary>
    /// <param name="Column">The grouped column.</param>
    /// <param name="GroupIndex">The new group index.</param>
    /// <returns>True if the column moved; otherwise, false.</returns>
    public bool MoveGroupedColumn(GroupGridColumn Column, int GroupIndex)
    {
        return fEngine.MoveGroupedColumn(Column, GroupIndex);
    }
    /// <summary>
    /// Moves a column to a new display index in the column collection.
    /// </summary>
    /// <param name="Column">The grid column.</param>
    /// <param name="ColumnIndex">The new column index.</param>
    /// <returns>True if the column moved; otherwise, false.</returns>
    public bool MoveColumn(GroupGridColumn Column, int ColumnIndex)
    {
        return fEngine.MoveColumn(Column, ColumnIndex);
    }
    /// <summary>
    /// Shows or hides a column.
    /// </summary>
    /// <param name="Column">The grid column.</param>
    /// <param name="IsVisible">True to show the column; false to hide it.</param>
    /// <returns>True if the column visibility changed; otherwise, false.</returns>
    public bool SetColumnVisible(GroupGridColumn Column, bool IsVisible)
    {
        return fEngine.SetColumnVisible(Column, IsVisible);
    }
    /// <summary>
    /// Sets the expanded state of a group node by visible-node index.
    /// </summary>
    /// <param name="VisibleNodeIndex">The visible-node index.</param>
    /// <param name="IsExpanded">True to expand; false to collapse.</param>
    /// <returns>True if the group state changed; otherwise, false.</returns>
    public bool SetGroupExpanded(int VisibleNodeIndex, bool IsExpanded)
    {
        return fEngine.SetGroupExpanded(VisibleNodeIndex, IsExpanded);
    }
    /// <summary>
    /// Toggles the expanded state of a group node by visible-node index.
    /// </summary>
    /// <param name="VisibleNodeIndex">The visible-node index.</param>
    /// <returns>True if the group state changed; otherwise, false.</returns>
    public bool ToggleGroupExpanded(int VisibleNodeIndex)
    {
        return fEngine.ToggleGroupExpanded(VisibleNodeIndex);
    }
    /// <summary>
    /// Clears the current cell.
    /// </summary>
    public void ClearCurrentCell()
    {
        fEngine.ClearCurrentCell();
    }
    /// <summary>
    /// Sets the current cell.
    /// </summary>
    /// <param name="Cell">The current cell.</param>
    /// <returns>True if the current cell changed; otherwise, false.</returns>
    public bool SetCurrentCell(GroupGridCell Cell)
    {
        bool Result = fEngine.SetCurrentCell(Cell);
        ScrollCurrentCellIntoViewCore();
        return Result;
    }
    /// <summary>
    /// Sets the current cell by row index and column.
    /// </summary>
    /// <param name="RowIndex">The adapter row index.</param>
    /// <param name="Column">The grid column.</param>
    /// <returns>True if the current cell changed; otherwise, false.</returns>
    public bool SetCurrentCell(int RowIndex, GroupGridColumn Column) => SetCurrentCell(new GroupGridCell(RowIndex, Column));
    /// <summary>
    /// Clears the selected cell.
    /// </summary>
    public void ClearSelection()
    {
        fEngine.ClearSelection();
    }
    /// <summary>
    /// Sets the selected cell.
    /// </summary>
    /// <param name="Cell">The selected cell.</param>
    /// <returns>True if the selected cell changed; otherwise, false.</returns>
    public bool SetSelectedCell(GroupGridCell Cell)
    {
        return fEngine.SetSelectedCell(Cell);
    }
    /// <summary>
    /// Selects the current cell.
    /// </summary>
    /// <returns>True if the selected cell changed; otherwise, false.</returns>
    public bool SelectCurrentCell()
    {
        return fEngine.SelectCurrentCell();
    }
    /// <summary>
    /// Begins editing the current cell.
    /// </summary>
    /// <returns>True if editing started; otherwise, false.</returns>
    public bool BeginEdit()
    {
        return fEngine.BeginEdit();
    }
    /// <summary>
    /// Commits the edited value.
    /// </summary>
    /// <param name="Value">The editor value.</param>
    /// <returns>True if the edit was committed; otherwise, false.</returns>
    public bool CommitEdit(object Value)
    {
        return fEngine.CommitEdit(Value);
    }
    /// <summary>
    /// Cancels the current edit.
    /// </summary>
    /// <returns>True if an edit was canceled; otherwise, false.</returns>
    public bool CancelEdit()
    {
        return fEngine.CancelEdit();
    }
    /// <summary>
    /// Moves the current cell by visible row and column deltas.
    /// </summary>
    /// <param name="RowDelta">The visible row delta.</param>
    /// <param name="ColumnDelta">The visible column delta.</param>
    /// <returns>True if the current cell moved; otherwise, false.</returns>
    public bool MoveCurrentCell(int RowDelta, int ColumnDelta)
    {
        bool Result = fEngine.MoveCurrentCell(RowDelta, ColumnDelta);
        ScrollCurrentCellIntoViewCore();
        return Result;
    }
    /// <inheritdoc />
    public override void Render(DrawingContext Context)
    {
        base.Render(Context);

        Rect BoundsRect = new(0, 0, Bounds.Width, Bounds.Height);
        DrawBand(Context, BoundsRect, fBackgroundBrush);

        if (fEngine == null)
            return;

        double ContentWidth = GetBodyContentWidth();
        double Y = 0;
        DrawBand(Context, new Rect(0, Y, Bounds.Width, fEngine.LayoutMetrics.ToolBarHeight), fToolBarBrush);
        Y += fEngine.LayoutMetrics.ToolBarHeight;

        DrawGroupPanel(Context, Y, fEngine.LayoutMetrics.GroupPanelHeight, ContentWidth);
        if (HasVerticalScrollBar())
            DrawBand(Context, new Rect(ContentWidth, Y, Bounds.Width - ContentWidth, fEngine.LayoutMetrics.GroupPanelHeight), fGroupPanelBrush);
        Y += fEngine.LayoutMetrics.GroupPanelHeight;

        DrawColumns(Context, Y, fEngine.LayoutMetrics.ColumnHeaderHeight, ContentWidth, fHeaderBrush, false);
        if (HasVerticalScrollBar())
            DrawBand(Context, new Rect(ContentWidth, Y, Bounds.Width - ContentWidth, fEngine.LayoutMetrics.ColumnHeaderHeight), fHeaderBrush);
        Y += fEngine.LayoutMetrics.ColumnHeaderHeight;

        DrawColumns(Context, Y, fEngine.LayoutMetrics.FilterRowHeight, ContentWidth, fFilterBrush, true);
        if (HasVerticalScrollBar())
            DrawBand(Context, new Rect(ContentWidth, Y, Bounds.Width - ContentWidth, fEngine.LayoutMetrics.FilterRowHeight), fFilterBrush);
        Y += fEngine.LayoutMetrics.FilterRowHeight;

        double BodyHeight = GetBodyHeight();
        DrawBody(Context, Y, ContentWidth);
        DrawVerticalScrollBar(Context);
        Y += BodyHeight;

        DrawFooter(Context, Y, fEngine.LayoutMetrics.FooterSummaryHeight, ContentWidth);
        if (HasVerticalScrollBar())
            DrawBand(Context, new Rect(ContentWidth, Y, Bounds.Width - ContentWidth, fEngine.LayoutMetrics.FooterSummaryHeight), fFooterBrush);
        Y += fEngine.LayoutMetrics.FooterSummaryHeight;

        DrawHorizontalScrollBar(Context);
        DrawColumnResizeGuide(Context);
        DrawColumnDragGuide(Context);
        DrawColumnDragGhost(Context);
    }

    // ● properties
    /// <summary>
    /// Gets or sets the group grid engine rendered by this control.
    /// </summary>
    public GroupGridEngine Engine
    {
        get => fEngine;
        set
        {
            value ??= new GroupGridEngine();

            if (ReferenceEquals(fEngine, value))
                return;

            DetachEngine(fEngine);
            fEngine = value;
            AttachEngine(fEngine);
            InvalidateMeasure();
            InvalidateVisual();
        }
    }
    /// <summary>
    /// Gets the grid columns.
    /// </summary>
    public ObservableCollection<GroupGridColumn> Columns => fEngine.Columns;
    /// <summary>
    /// Gets the grouped columns in grouping order.
    /// </summary>
    public IReadOnlyList<GroupGridColumn> GroupColumns => fEngine.GroupColumns;
    /// <summary>
    /// Gets or sets the data adapter.
    /// </summary>
    public IGroupGridDataAdapter DataAdapter
    {
        get => fEngine.DataAdapter;
        set
        {
            if (fOwnedDataAdapter != null && !ReferenceEquals(fOwnedDataAdapter, value))
            {
                fOwnedDataAdapter.Dispose();
                fOwnedDataAdapter = null;
                fItemsSource = null;
            }

            fEngine.DataAdapter = value;
        }
    }
    /// <summary>
    /// Gets or sets an item source that implements <see cref="IList{T}"/>.
    /// </summary>
    public object ItemsSource
    {
        get => fItemsSource;
        set => SetItemsSource(value);
    }
    /// <summary>
    /// Gets or sets a value indicating whether columns are generated from public item properties when columns are empty.
    /// </summary>
    public bool AutoGenerateColumns
    {
        get => fAutoGenerateColumns;
        set
        {
            if (fAutoGenerateColumns == value)
                return;

            fAutoGenerateColumns = value;
            GenerateColumnsFromItemType(FindListItemType(fItemsSource));
        }
    }
    /// <summary>
    /// Gets the layout metrics used by the grid.
    /// </summary>
    public GroupGridLayoutMetrics LayoutMetrics => fEngine.LayoutMetrics;
    /// <summary>
    /// Gets the current cell.
    /// </summary>
    public GroupGridCell CurrentCell => fEngine.CurrentCell;
    /// <summary>
    /// Gets the selected cell.
    /// </summary>
    public GroupGridCell SelectedCell => fEngine.SelectedCell;
    /// <summary>
    /// Gets the editing cell.
    /// </summary>
    public GroupGridCell EditingCell => fEngine.EditingCell;
    /// <summary>
    /// Gets a value indicating whether the grid has a current cell.
    /// </summary>
    public bool HasCurrentCell => fEngine.HasCurrentCell;
    /// <summary>
    /// Gets a value indicating whether the grid has a selected cell.
    /// </summary>
    public bool HasSelectedCell => fEngine.HasSelectedCell;
    /// <summary>
    /// Gets a value indicating whether the grid is editing a cell.
    /// </summary>
    public bool IsEditing => fEngine.IsEditing;
    /// <summary>
    /// Gets the viewport window into the logical visible-node list.
    /// </summary>
    public GroupGridViewport Viewport => fEngine.Viewport;
    /// <summary>
    /// Gets the number of visible projected nodes.
    /// </summary>
    public int VisibleNodeCount => fEngine.VisibleNodeCount;
    /// <summary>
    /// Gets the number of adapter rows.
    /// </summary>
    public int RowCount => fEngine.RowCount;
    /// <summary>
    /// Gets the first visible node index in the virtual viewport.
    /// </summary>
    public int FirstVisibleNodeIndex => fFirstVisibleNodeIndex;
    /// <summary>
    /// Gets the horizontal scroll offset in pixels.
    /// </summary>
    public double HorizontalOffset => fHorizontalOffset;
    /// <summary>
    /// Gets the total height of the fixed non-body bands.
    /// </summary>
    public double FixedBandHeight
    {
        get
        {
            if (fEngine == null)
                return 0;

            return fEngine.LayoutMetrics.ToolBarHeight
                   + fEngine.LayoutMetrics.GroupPanelHeight
                   + fEngine.LayoutMetrics.ColumnHeaderHeight
                   + fEngine.LayoutMetrics.FilterRowHeight
                   + fEngine.LayoutMetrics.FooterSummaryHeight;
        }
    }
}
