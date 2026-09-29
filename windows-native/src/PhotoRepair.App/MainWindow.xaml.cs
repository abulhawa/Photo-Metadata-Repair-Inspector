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
    private bool updatingRepairPickers;
    private ScrollViewer? tableScroll;
    public MainWindow()
    {
        model = new(action => DispatcherQueue.TryEnqueue(() => action()));
        InitializeComponent();
        AppWindow.SetIcon(System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "PhotoRepair.ico"));
        Root.DataContext = model;
        Table.Loaded += (_, _) => ConnectTableScroll();
        Root.Loaded += (_, _) =>
        {
            double scale = Root.XamlRoot.RasterizationScale;
            var display = Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(
                AppWindow.Id, Microsoft.UI.Windowing.DisplayAreaFallback.Primary);
            AppWindow.Resize(new(
                (int)Math.Min(1100 * scale, display.WorkArea.Width * 0.9),
                (int)Math.Min(720 * scale, display.WorkArea.Height * 0.9)));
        };
        Closed += (_, _) => model.Stop();
        ready = true;
        ReviewFilter.Visibility = Visibility.Collapsed;
        RepairTargetPicker.ItemsSource = MediaRules.RepairTargets;
        RepairTargetPicker.SelectedIndex = 0;
        model.PropertyChanged += (_, _) => UpdateSurface();
        UpdateSurface();
    }
    private void LayoutChanged(object sender, SizeChangedEventArgs e)
    {
        if (!ready) return;
        bool narrow = e.NewSize.Width < 760;
        NavigationBar.Orientation = narrow ? Orientation.Vertical : Orientation.Horizontal;
        FilterBar.Orientation = narrow ? Orientation.Vertical : Orientation.Horizontal;
        SelectionActions.Orientation = narrow ? Orientation.Vertical : Orientation.Horizontal;
        RepairBar.Orientation = narrow ? Orientation.Vertical : Orientation.Horizontal;
    }
    private void TableRowLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not Grid row) return;
        var columns = (TableColumns)Root.Resources["ColumnLayout"];
        foreach (var child in row.Children.OfType<FrameworkElement>())
        {
            if (!int.TryParse(child.Tag?.ToString(), out int column)) continue;
            Grid.SetColumn(child, columns.SlotFor(column));
            child.Visibility = columns.Items[column].Width.Value > 0 ? Visibility.Visible : Visibility.Collapsed;
        }
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
    private void UpdateRealizedColumns(DependencyObject parent)
    {
        if (parent is Grid { Tag: "MediaRow" } row) TableRowLoaded(row, new RoutedEventArgs());
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            UpdateRealizedColumns(VisualTreeHelper.GetChild(parent, i));
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
        FolderMenu.IsEnabled = model.CanSelectFolder;
        ScanMenu.IsEnabled = model.CanScan;
        StopMenu.IsEnabled = model.IsScanning;
        FooterStatus.Text = model.IsScanning ? $"Scanning · {model.ProgressText}" : model.IsApplying ? model.Status :
            string.IsNullOrWhiteSpace(model.Status) || model.Status == model.SelectedFolder ? "Ready" : model.Status;
        ToolTipService.SetToolTip(FooterStatus, FooterStatus.Text);
        FooterCounts.Text = !model.HasScannedFolder || model.View == "Repair log" ? "" :
            model.View == "Review" ? $"{model.Rows.Count} shown · {model.Selection.Selected.Count} selected" : model.Summary;
        Notification.Severity = model.StatusKind switch
        {
            "Error" => InfoBarSeverity.Error, "Warning" => InfoBarSeverity.Warning,
            "Success" => InfoBarSeverity.Success, _ => InfoBarSeverity.Informational
        };
        bool loaded = model.HasScannedFolder;
        bool history = model.View == "Repair log";
        bool review = model.View == "Review";
        bool emptyLog = !model.HasRepairLog;
        FilterBar.Visibility = loaded && !history ? Visibility.Visible : Visibility.Collapsed;
        var columns = (TableColumns)Root.Resources["ColumnLayout"];
        columns.SetReview(review);
        UpdateRealizedColumns(Table);
        string[] headings = ["", "File", "Type", "Size", "Created", "Modified", "Taken At", "Filename Date", "Location", "Review reason"];
        foreach (var child in TableHeader.Children.OfType<FrameworkElement>())
        {
            int column = child is Button button ? Array.IndexOf(headings, button.Tag?.ToString()) : int.Parse(child.Tag.ToString()!);
            Grid.SetColumn(child, columns.SlotFor(column));
            child.Visibility = columns.Items[column].Width.Value > 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        ResultsControls.Visibility = loaded ? Visibility.Visible : Visibility.Collapsed;
        WelcomePanel.Visibility = !loaded || (history ? emptyLog : model.Rows.Count == 0) ? Visibility.Visible : Visibility.Collapsed;
        EmptyTitle.Text = !loaded ? "Inspect your photo dates" : history ? "No repairs recorded" : model.View == "Review" && string.IsNullOrWhiteSpace(model.Search) && model.Media == "All" && model.Issue == "All review items"
            ? "No files need review" : "No files to show";
        EmptyDescription.Text = !loaded ? "Choose a folder above, then click Scan folder. You can review dates before making any changes."
            : history ? "Repairs you apply to this folder will appear here." : "Try changing the filters or scanning a different folder.";
        Table.Visibility = loaded && !history ? Visibility.Visible : Visibility.Collapsed;
        TableRegion.Visibility = Table.Visibility;
        LogPanel.Visibility = loaded && history && !emptyLog ? Visibility.Visible : Visibility.Collapsed;
        SelectionPanel.Visibility = loaded && review ? Visibility.Visible : Visibility.Collapsed;
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
    // Let the flyout finish handling the click and release focus before opening
    // a modal picker or disabling its currently invoked menu item.
    private void PickFolderFromMenu(object sender, RoutedEventArgs e) =>
        DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,
            () => PickFolder(sender, e));
    private void ScanFolderFromMenu(object sender, RoutedEventArgs e) =>
        DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,
            () => ScanFolder(sender, e));
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
        model.View = ((ListBoxItem)ViewPicker.SelectedItem).Content.ToString()!;
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
        if (model.View != "Review") return;
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
    private void RepairTargetChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!ready || RepairTargetPicker.SelectedItem is not RepairDateField target) return;
        string? previousSource = (RepairSourcePicker.SelectedItem as RepairDateField)?.Id;
        var sources = MediaRules.SourcesForTarget(target.Id);
        updatingRepairPickers = true;
        try
        {
            RepairSourcePicker.ItemsSource = sources;
            RepairSourcePicker.SelectedItem = sources.FirstOrDefault(source => source.Id == previousSource) ?? sources[0];
        }
        finally { updatingRepairPickers = false; }
        UpdateRepairMethod();
    }
    private void RepairSourceChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ready && !updatingRepairPickers) UpdateRepairMethod();
    }
    private void UpdateRepairMethod()
    {
        if (RepairTargetPicker.SelectedItem is RepairDateField target &&
            RepairSourcePicker.SelectedItem is RepairDateField source)
            model.SetRepairMethod($"{target.Id}:{source.Id}");
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
    private void ExitApp(object sender, RoutedEventArgs e) => Close();
    private void ChooseView(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item && int.TryParse(item.Tag?.ToString(), out int index))
            ViewPicker.SelectedIndex = index;
    }
    private async Task ShowTextDialog(string title, string text)
    {
        await new ContentDialog
        {
            XamlRoot = Root.XamlRoot, Title = title, CloseButtonText = "Close",
            Content = new ScrollViewer { MaxHeight = 460, Content = new TextBlock
                { Text = text, TextWrapping = TextWrapping.Wrap, IsTextSelectionEnabled = true } }
        }.ShowAsync();
    }
    private async void ShowHelp(object sender, RoutedEventArgs e) => await ShowTextDialog("How to use",
        "1. Select a folder, then choose Scan folder. Scanning does not repair files.\n\n" +
        "2. Browse Library or open Review to inspect files that need attention. Use the filters and select the files you want to repair.\n\n" +
        "3. Choose a repair method. Keep Backup originals enabled for a recovery copy. Select Review changes to inspect the proposed changes.\n\n" +
        "4. Nothing is written until you explicitly choose Apply in the confirmation dialog. Inspect the Repair log afterward.\n\n" +
        "Photos stay on your computer. Backups and repair logs remain in the scanned folder after uninstalling the app.");
    private async void ShowAbout(object sender, RoutedEventArgs e)
    {
        string version;
        try
        {
            var v = global::Windows.ApplicationModel.Package.Current.Id.Version;
            version = $"{v.Major}.{v.Minor}.{v.Build}.{v.Revision}";
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.Runtime.InteropServices.COMException)
        {
            version = typeof(App).Assembly.GetName().Version?.ToString() ?? "Unknown";
            version += " (development build)";
        }
        var content = new StackPanel { Spacing = 12 };
        content.Children.Add(new Image { Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(
            new Uri("ms-appx:///Assets/Square44x44Logo.png")), Width = 64, Height = 64, HorizontalAlignment = HorizontalAlignment.Left });
        content.Children.Add(new TextBlock { Text = $"Photo Metadata Repair Inspector\nVersion {version}\nQortxAI", TextWrapping = TextWrapping.Wrap, IsTextSelectionEnabled = true });
        content.Children.Add(new HyperlinkButton { Content = "QortxAI website", NavigateUri = new Uri("https://qortxai.com") });
        content.Children.Add(new HyperlinkButton { Content = "Privacy policy", NavigateUri = new Uri("https://qortxai.com/projects/photo-metadata-repair-inspector/privacy/") });
        await new ContentDialog { XamlRoot = Root.XamlRoot, Title = "About", Content = content, CloseButtonText = "Close" }.ShowAsync();
    }
    private async Task OpenWeb(string url)
    {
        try
        {
            if (!await Launcher.LaunchUriAsync(new Uri(url))) model.ReportError("Could not open your browser. Try again from Help.");
        }
        catch (Exception ex) { model.ReportError($"Could not open your browser: {ex.Message}"); }
    }
    private async void OpenPrivacy(object sender, RoutedEventArgs e) => await OpenWeb("https://qortxai.com/projects/photo-metadata-repair-inspector/privacy/");
    private async void OpenIssues(object sender, RoutedEventArgs e) => await OpenWeb("https://github.com/abulhawa/Photo-Metadata-Repair-Inspector/issues");
    private static bool Down(VirtualKey key) => InputKeyboardSource.GetKeyStateForCurrentThread(key).HasFlag(CoreVirtualKeyStates.Down);
    private void SelectRow(object sender, bool checkbox)
    {
        if (model.View == "Review" && sender is FrameworkElement { DataContext: MediaRow row }) model.Click(row, Down(VirtualKey.Control), Down(VirtualKey.Shift), checkbox);
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
