using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using PhotoRepair.Windows;
using PhotoRepair.Core;
using Windows.ApplicationModel.DataTransfer;
using Microsoft.Windows.Storage.Pickers;
using Windows.System;
using Windows.UI.Core;

namespace PhotoRepair.App;

public sealed partial class MainWindow : Window
{
    private readonly InspectionViewModel model;
    private bool ready;
    private ScrollViewer? tableScroll;
    public MainWindow()
    {
        model = new(action => DispatcherQueue.TryEnqueue(() => action()));
        InitializeComponent();
        Root.DataContext = model;
        Table.Loaded += (_, _) => ConnectTableScroll();
        AppWindow.Resize(new(1450, 820));
        Closed += (_, _) => model.Stop();
        ready = true;
        ReviewFilter.Visibility = Visibility.Collapsed;
        RepairMethodPicker.ItemsSource = MediaRules.RepairMethods;
        RepairMethodPicker.SelectedIndex = 0;
        model.PropertyChanged += (_, _) => UpdateSurface();
        UpdateSurface();
    }
    private void ConnectTableScroll()
    {
        Table.ApplyTemplate();
        var viewer = FindScrollViewer(Table);
        if (viewer is null || ReferenceEquals(viewer, tableScroll)) return;
        if (tableScroll is not null) tableScroll.ViewChanged -= TableScrolled;
        tableScroll = viewer;
        tableScroll.ViewChanged += TableScrolled;
        HeaderScroll.ChangeView(tableScroll.HorizontalOffset, null, null, true);
    }
    private void TableScrolled(object? sender, ScrollViewerViewChangedEventArgs e)
    {
        if (tableScroll is not null)
            HeaderScroll.ChangeView(tableScroll.HorizontalOffset, null, null, true);
    }
    private static ScrollViewer? FindScrollViewer(DependencyObject parent)
    {
        for (int index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index);
            if (child is ScrollViewer viewer) return viewer;
            if (FindScrollViewer(child) is { } nested) return nested;
        }
        return null;
    }
    private void UpdateSurface()
    {
        bool loaded = model.HasScannedFolder;
        bool history = model.View == "Repair log";
        ResultsControls.Visibility = loaded ? Visibility.Visible : Visibility.Collapsed;
        WelcomePanel.Visibility = !loaded || (!history && model.Rows.Count == 0) ? Visibility.Visible : Visibility.Collapsed;
        EmptyTitle.Text = !loaded ? "Inspect your photo dates" : model.View == "Review" && string.IsNullOrWhiteSpace(model.Search) && model.Media == "All" && model.Issue == "All review items"
            ? "No files need review" : "No files to show";
        EmptyDescription.Text = !loaded ? "Choose a folder above, then click Scan folder. You can review dates before making any changes."
            : "Try changing the filters or scanning a different folder.";
        Table.Visibility = loaded && !history ? Visibility.Visible : Visibility.Collapsed;
        TableRegion.Visibility = Table.Visibility;
        LogPanel.Visibility = loaded && history ? Visibility.Visible : Visibility.Collapsed;
        SelectionPanel.Visibility = loaded && !history ? Visibility.Visible : Visibility.Collapsed;
        ReviewFilter.Visibility = model.View == "Review" ? Visibility.Visible : Visibility.Collapsed;
        RepairBar.Visibility = model.View == "Review" ? Visibility.Visible : Visibility.Collapsed;
        StopButton.Visibility = model.IsScanning ? Visibility.Visible : Visibility.Collapsed;
        ScanProgress.Visibility = model.IsScanning ? Visibility.Visible : Visibility.Collapsed;
        // Keep warnings and results from an older folder visible, without repeating
        // the selected path after every successful scan.
        bool showStatus = model.IsScanning || (!string.IsNullOrEmpty(model.Status)
            && model.Status != model.SelectedFolder && model.Status != "Ready to scan.");
        StatusPanel.Visibility = showStatus ? Visibility.Visible : Visibility.Collapsed;
        foreach (var heading in TableHeader.Children.OfType<Button>())
        {
            string column = heading.Tag.ToString()!;
            bool active = model.SortColumn == column;
            heading.Content = column + (active ? model.Descending ? " ↓" : " ↑" : "");
            string direction = active && !model.Descending ? "descending" : "ascending";
            ToolTipService.SetToolTip(heading, $"Sort by {column}, {direction}");
            Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(heading, $"Sort by {column}");
            Microsoft.UI.Xaml.Automation.AutomationProperties.SetHelpText(heading,
                active ? $"Sorted {(model.Descending ? "descending" : "ascending")}. Activate to sort {direction}." : $"Activate to sort {direction}.");
        }
    }
    private async void PickFolder(object sender, RoutedEventArgs e)
    {
        if (!model.CanSelectFolder) return;
        model.SetFolderPickerOpen(true);
        try
        {
            var picker = new FolderPicker(AppWindow.Id);
            var folder = await picker.PickSingleFolderAsync();
            if (folder is not null) model.SelectFolder(folder.Path);
        }
        catch (Exception ex)
        {
            string detail = string.IsNullOrWhiteSpace(ex.Message) ? $"Windows returned error 0x{ex.HResult:X8}." : ex.Message;
            model.ReportError($"Could not open folder: {detail}");
            await new ContentDialog
            {
                XamlRoot = Root.XamlRoot,
                Title = "Could not select a folder",
                Content = $"Please try selecting the folder again.\n\n{detail}",
                CloseButtonText = "Close"
            }.ShowAsync();
        }
        finally { model.SetFolderPickerOpen(false); }
    }
    private async void ScanFolder(object sender, RoutedEventArgs e) => await model.ScanSelectedFolderAsync();
    private void StopScan(object sender, RoutedEventArgs e) => model.Stop();
    private void SearchChanged(object sender, TextChangedEventArgs e) { if (ready) { model.Search = SearchBox.Text; model.Refresh(); } }
    private void FilterChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!ready) return;
        model.Media = ((ComboBoxItem)MediaFilter.SelectedItem).Tag.ToString()!;
        model.Issue = ((ComboBoxItem)ReviewFilter.SelectedItem).Content.ToString()!;
        model.Refresh();
    }
    private void ViewChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!ready) return;
        model.View = ((ComboBoxItem)ViewPicker.SelectedItem).Content.ToString()!;
        model.Refresh();
    }
    private void SortColumn(object sender, RoutedEventArgs e)
    {
        double horizontal = tableScroll?.HorizontalOffset ?? 0;
        double vertical = tableScroll?.VerticalOffset ?? 0;
        model.Sort(((Button)sender).Tag.ToString()!);
        Table.UpdateLayout();
        tableScroll?.ChangeView(horizontal, vertical, null, true);
        HeaderScroll.ChangeView(horizontal, null, null, true);
    }
    private void TableKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.A && Down(VirtualKey.Control)) { model.SelectAll(); e.Handled = true; }
        else if (e.Key == VirtualKey.Escape) { model.ClearSelection(); e.Handled = true; }
    }
    private void ResizeColumn(object sender, DragDeltaEventArgs e)
    {
        if (sender is ColumnResizeGrip thumb && int.TryParse(thumb.Tag?.ToString(), out int index))
            ((TableColumns)Root.Resources["ColumnLayout"]).Items[index].Resize(e.HorizontalChange);
    }
    private void ResizeColumnKey(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key is not (VirtualKey.Left or VirtualKey.Right)) return;
        if (sender is ColumnResizeGrip thumb && int.TryParse(thumb.Tag?.ToString(), out int index))
        {
            ((TableColumns)Root.Resources["ColumnLayout"]).Items[index].Resize(e.Key == VirtualKey.Left ? -10 : 10);
            e.Handled = true;
        }
    }
    private void SelectAll(object sender, RoutedEventArgs e) => model.SelectAll();
    private void ClearSelection(object sender, RoutedEventArgs e) => model.ClearSelection();
    private void RepairMethodChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!ready || RepairMethodPicker.SelectedItem is not RepairMethod method) return;
        model.SetRepairMethod(method.Id);
    }
    private void BackupToggled(object sender, RoutedEventArgs e)
    {
        if (ready) model.SetCreateBackup(BackupOriginals.IsChecked == true);
    }
    private async void ReviewChanges(object sender, RoutedEventArgs e)
    {
        try
        {
            RepairPreview preview = model.BuildRepairPreview();
            string examples = string.Join("\n\n", preview.Examples.Select(item =>
                $"{item.Name}\n{item.Method.Label}\n{item.Before}  →  {item.After}"));
            string text = $"Selected: {preview.SelectedCount}\nApplicable: {preview.ApplicableCount}\nSkipped: {preview.SkippedCount}";
            if (!preview.CreateBackup)
                text += "\n\nWARNING: Backups are OFF. These changes will be made in place without a recovery copy created by this application.";
            if (preview.SkippedCount > 0)
            {
                var reasons = preview.Items.Where(item => !item.Applicable)
                    .GroupBy(item => item.SkipReason)
                    .Select(group => $"{group.Count()} skipped: {group.Key}");
                text += "\n\n" + string.Join("\n", reasons);
            }
            if (examples.Length > 0)
                text += $"\n\nExamples (up to 10):\n\n{examples}";

            var dialog = new ContentDialog
            {
                XamlRoot = Root.XamlRoot,
                Title = "Review repair changes",
                Content = new ScrollViewer
                {
                    MaxHeight = 520,
                    Content = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap, IsTextSelectionEnabled = true }
                },
                CloseButtonText = preview.ApplicableCount > 0 ? "Cancel" : "Close",
                DefaultButton = ContentDialogButton.Close
            };
            if (preview.ApplicableCount > 0) dialog.PrimaryButtonText = $"Apply {preview.ApplicableCount} changes";

            ContentDialogResult result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
                await model.ApplyRepairAsync(preview);
        }
        catch (Exception ex)
        {
            model.ReportError($"Repair could not start: {ex.Message}");
        }
    }
    private static bool Down(VirtualKey key) => InputKeyboardSource.GetKeyStateForCurrentThread(key).HasFlag(CoreVirtualKeyStates.Down);
    private void SelectRow(object sender, bool checkbox)
    {
        if (sender is FrameworkElement { DataContext: MediaRow row }) model.Click(row, Down(VirtualKey.Control), Down(VirtualKey.Shift), checkbox);
    }
    private void CheckboxClicked(object sender, RoutedEventArgs e) => SelectRow(sender, true);
    private void RowTapped(object sender, TappedRoutedEventArgs e)
    {
        // CheckBox handles its own click. Avoid toggling twice on its bubbled tap.
        var element = e.OriginalSource as DependencyObject;
        while (element is not null && !ReferenceEquals(element, sender))
        {
            if (element is CheckBox) return;
            element = VisualTreeHelper.GetParent(element);
        }
        SelectRow(sender, false); e.Handled = true;
    }
    private void RowKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Space && e.OriginalSource is not CheckBox) { SelectRow(sender, true); e.Handled = true; }
    }
    private async void OpenFile(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: MediaRow row }) return;
        try { await Launcher.LaunchFileAsync(await global::Windows.Storage.StorageFile.GetFileFromPathAsync(row.Record.Path)); }
        catch (Exception ex) { model.ReportError(ex.Message); }
    }
    private async void RevealFile(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: MediaRow row }) return;
        try
        {
            var folder = await global::Windows.Storage.StorageFolder.GetFolderFromPathAsync(row.Record.Location);
            var options = new FolderLauncherOptions();
            options.ItemsToSelect.Add(await global::Windows.Storage.StorageFile.GetFileFromPathAsync(row.Record.Path));
            await Launcher.LaunchFolderAsync(folder, options);
        }
        catch (Exception ex) { model.ReportError(ex.Message); }
    }
    private void CopyPath(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: MediaRow row }) return;
        var data = new DataPackage(); data.SetText(row.Record.Path); Clipboard.SetContent(data);
    }
}
