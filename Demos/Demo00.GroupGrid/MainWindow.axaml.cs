namespace Demo00.GroupGrid;

public partial class MainWindow : Window
{
    // ● private fields
    private bool fIsWindowInitialized;

    // ● private
    /// <summary>
    /// Initializes the window after it is opened.
    /// </summary>
    private void WindowInitialize()
    {
        Grid.Columns.Add(new GroupGridTextColumn { Name = "Customer", Header = "Customer", Width = 170 });
        Grid.Columns.Add(new GroupGridTextColumn { Name = "Region", Header = "Region", Width = 120 });
        Grid.Columns.Add(new GroupGridDateColumn { Name = "OrderDate", Header = "Order Date", Width = 120, DisplayFormat = "yyyy-MM-dd" });
        Grid.Columns.Add(new GroupGridNumberColumn { Name = "Quantity", Header = "Qty", Width = 80, ValueType = typeof(int), GroupSummary = GroupGridAggregateKind.Sum, TotalSummary = GroupGridAggregateKind.Sum });
        Grid.Columns.Add(new GroupGridNumberColumn { Name = "Amount", Header = "Amount", Width = 120, ValueType = typeof(decimal), DisplayFormat = "N2", GroupSummary = GroupGridAggregateKind.Sum, TotalSummary = GroupGridAggregateKind.Sum });
        Grid.Columns.Add(new GroupGridCheckBoxColumn { Name = "IsPaid", Header = "Paid", Width = 80 });
        Grid.Columns.Add(new GroupGridTextColumn { Name = "Notes", Header = "Notes", Width = 900 });

        Grid.ItemsSource = CreateRows();
        Grid.GroupColumn(Grid.Columns[1]);
        Grid.SetCurrentCell(0, Grid.Columns[0]);
        Grid.SelectCurrentCell();
    }
    /// <summary>
    /// Creates the demo rows.
    /// </summary>
    /// <returns>The demo rows.</returns>
    private List<SalesRow> CreateRows()
    {
        List<SalesRow> Result = new();
        string[] Regions = { "North", "South", "East", "West" };
        string[] Customers = { "Alpha", "Beacon", "Canyon", "Delta", "Eclipse", "Falcon", "Galaxy", "Harbor" };
        DateTime StartDate = new(2026, 1, 5);

        for (int Index = 0; Index < 80; Index++)
        {
            int Quantity = 1 + (Index % 9);
            Result.Add(new SalesRow
            {
                Customer = Customers[Index % Customers.Length],
                Region = Regions[Index % Regions.Length],
                OrderDate = StartDate.AddDays(Index),
                Quantity = Quantity,
                Amount = Quantity * (18.75m + (Index % 7)),
                IsPaid = Index % 3 != 0,
                Notes = string.Format("Order line {0:000} generated for horizontal scrolling checks.", Index + 1),
            });
        }

        return Result;
    }

    // ● protected
    /// <summary>
    /// Handles the window opened event.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        if (fIsWindowInitialized)
            return;

        WindowInitialize();
        fIsWindowInitialized = true;
        //LogBox.AppendLine("Application Started.");
    }


    // ● constructor
    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
    }
}

/// <summary>
/// Represents a demo sales row.
/// </summary>
public class SalesRow
{
    // ● constructor
    /// <summary>
    /// Initializes a new instance of the <see cref="SalesRow"/> class.
    /// </summary>
    public SalesRow()
    {
    }

    // ● properties
    /// <summary>
    /// Gets or sets the customer name.
    /// </summary>
    public string Customer { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the region.
    /// </summary>
    public string Region { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the order date.
    /// </summary>
    public DateTime OrderDate { get; set; }
    /// <summary>
    /// Gets or sets the quantity.
    /// </summary>
    public int Quantity { get; set; }
    /// <summary>
    /// Gets or sets the amount.
    /// </summary>
    public decimal Amount { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether the order is paid.
    /// </summary>
    public bool IsPaid { get; set; }
    /// <summary>
    /// Gets or sets the row notes.
    /// </summary>
    public string Notes { get; set; } = string.Empty;
}
