using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Markup;

namespace PhotoRepair.App;

// Thumb is sealed; the host supplies the cursor and forwards its drag event.
public sealed class ColumnResizeGrip : UserControl
{
    private InputSystemCursor? cursor;
    public event DragDeltaEventHandler? DragDelta;

    public ColumnResizeGrip()
    {
        IsTabStop = true;
        var thumb = new Thumb
        {
            IsTabStop = false,
            Template = (ControlTemplate)XamlReader.Load("""
                <ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" TargetType="Thumb">
                    <Grid Background="Transparent"><Border Width="1" Margin="0,5" Background="{ThemeResource ControlStrokeColorDefaultBrush}"/></Grid>
                </ControlTemplate>
                """)
        };
        thumb.DragDelta += (_, e) => DragDelta?.Invoke(this, e);
        Content = thumb;
        Loaded += (_, _) =>
        {
            cursor ??= InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);
            ProtectedCursor = cursor;
        };
        Unloaded += (_, _) =>
        {
            ProtectedCursor = null;
            cursor?.Dispose();
            cursor = null;
        };
    }
}
