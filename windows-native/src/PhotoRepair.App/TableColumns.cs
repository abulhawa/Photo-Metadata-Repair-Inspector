using System.ComponentModel;
using Microsoft.UI.Xaml;

namespace PhotoRepair.App;

public sealed class TableColumn(double width, double minimum) : INotifyPropertyChanged
{
    private double pixels = width;
    public GridLength Width => new(pixels);
    public void Resize(double delta)
    {
        double next = Math.Clamp(pixels + delta, minimum, 1600);
        if (next == pixels) return;
        pixels = next;
        PropertyChanged?.Invoke(this, new(nameof(Width)));
    }
    public event PropertyChangedEventHandler? PropertyChanged;
}

// Shared by header and every row, including rows realized after scrolling or sorting.
public sealed class TableColumns : INotifyPropertyChanged
{
    public IReadOnlyList<TableColumn> Items { get; } = new TableColumn[]
    {
        new(36, 32), new(200, 80), new(56, 48), new(64, 48),
        new(144, 80), new(144, 80), new(144, 80), new(144, 80),
        new(160, 80), new(180, 80)
    };
    public double TotalWidth => Items.Sum(column => column.Width.Value);
    public TableColumns()
    {
        foreach (var column in Items)
            column.PropertyChanged += (_, _) => PropertyChanged?.Invoke(this, new(nameof(TotalWidth)));
    }
    public event PropertyChangedEventHandler? PropertyChanged;
}
