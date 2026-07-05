namespace Avalonia.Controls;

/// <summary>
/// Represents a date or date-time column in a group grid.
/// </summary>
public class GroupGridDateColumn: GroupGridColumn
{
    // ● constructor
    /// <summary>
    /// Initializes a new instance of the <see cref="GroupGridDateColumn"/> class.
    /// </summary>
    public GroupGridDateColumn()
    {
        ValueType = typeof(DateTime);
    }

    // ● public methods
    /// <inheritdoc />
    public override object ParseValue(object Value)
    {
        if (Value == null || Value == DBNull.Value)
            return null;

        return Convert.ToDateTime(Value, CultureInfo.CurrentCulture);
    }
}
