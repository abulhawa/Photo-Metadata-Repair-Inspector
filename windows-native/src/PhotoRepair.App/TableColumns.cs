using System.ComponentModel;
using Microsoft.UI.Xaml;

namespace PhotoRepair.App;

public sealed class TableColumn(double width, double minimum) : INotifyPropertyChanged
{
    private double pixels = width;
    private bool visible = true;
    public GridLength Width => new(visible ? pixels : 0);
    public void SetVisible(bool value)
    {
        if (visible == value) return;
        visible = value;
        PropertyChanged?.Invoke(this, new(nameof(Width)));
    }
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
    private bool review;
    private bool initialized;
    private static readonly int[] reviewOrder = [0, 1, 9, 6, 7, 4, 5, 2, 3, 8];
    public IReadOnlyList<TableColumn> DisplayItems => review ? reviewOrder.Select(i => Items[i]).ToArray() : Items;
    public int SlotFor(int column) => review ? Array.IndexOf(reviewOrder, column) : column;
    public void SetReview(bool value)
    {
        if (initialized && review == value) return;
        initialized = true;
        review = value;
        Items[0].SetVisible(value);
        Items[9].SetVisible(value);
        PropertyChanged?.Invoke(this, new(nameof(DisplayItems)));
        PropertyChanged?.Invoke(this, new(nameof(TotalWidth)));
    }
    public double TotalWidth => Items.Sum(column => column.Width.Value);
    public TableColumns()
    {
        foreach (var column in Items)
            column.PropertyChanged += (_, _) => PropertyChanged?.Invoke(this, new(nameof(TotalWidth)));
    }
    public event PropertyChangedEventHandler? PropertyChanged;
}
