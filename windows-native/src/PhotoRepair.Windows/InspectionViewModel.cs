using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using PhotoRepair.Core;

namespace PhotoRepair.Windows;

public sealed class MediaRow(MediaRecord record) : INotifyPropertyChanged
{
    public MediaRecord Record { get; } = record;
    private bool selected;
    public bool IsSelected { get => selected; internal set { selected = value; PropertyChanged?.Invoke(this, new(nameof(IsSelected))); } }
    public event PropertyChangedEventHandler? PropertyChanged;
}

// UI-agnostic presentation state. The dispatcher is injected by WinUI.
public sealed class InspectionViewModel(Action<Action> dispatch, MediaScanner? scanner = null) : INotifyPropertyChanged
{
    private readonly MediaScanner scanner = scanner ?? new();
    private IReadOnlyList<MediaRecord> records = [];
    private CancellationTokenSource? cancellation;
    private int generation;
    private string? rootPath;
    private bool applying;

    public ObservableCollection<MediaRow> Rows { get; } = [];
    public SelectionModel Selection { get; } = new();
    public IReadOnlyList<RepairMethod> RepairMethods => MediaRules.RepairMethods;
    public string SelectedRepairMethodId { get; private set; } = MediaRules.RepairMethods[0].Id;
    public bool CreateBackup { get; private set; } = true;
    public string Search { get; set; } = "";
    public string Media { get; set; } = "All";
    public string Issue { get; set; } = "All review items";
    public string View { get; set; } = "Library";
    public string? SortColumn { get; private set; }
    public bool Descending { get; private set; }
    public string Status { get; private set; } = "";
    public string StatusKind { get; private set; } = "Information";
    public bool HasScannedFolder { get; private set; }
    public string? SelectedFolder { get; private set; }
    public string SelectedFolderDisplay => SelectedFolder ?? "No folder selected";
    public bool IsChoosingFolder { get; private set; }

    public string Summary => $"{Rows.Count} shown · {records.Count} media files · {records.Count(r => r.Issues.Count > 0)} to review";
    public string SelectionSummary => $"{Selection.Selected.Count} selected · Backups {(CreateBackup ? "on" : "off")}";
    public string Log { get; private set; } = "No folder loaded.";
    public bool HasRepairLog { get; private set; }
    public bool IsScanning => cancellation is not null;
    public bool IsApplying => applying;
    public bool CanSelectFolder => !IsScanning && !IsApplying && !IsChoosingFolder;
    public bool CanScan => CanSelectFolder && SelectedFolder is not null;
    public void SetFolderPickerOpen(bool open) { IsChoosingFolder = open; Notify(); }
    public void SelectFolder(string root)
    {
        if (IsScanning || IsApplying) return;
        SelectedFolder = Path.GetFullPath(root);
        if (!HasScannedFolder) Status = "Ready to scan.";
        Notify();
    }
    public Task ScanSelectedFolderAsync() => CanScan ? ScanAsync(SelectedFolder!) : Task.CompletedTask;

    public bool CanReviewChanges => rootPath is not null && !IsScanning && !IsApplying && Selection.Selected.Count > 0;
    public bool IsDiscovering { get; private set; }
    public double Completed { get; private set; }
    public double Total { get; private set; } = 1;
    public string ProgressText { get; private set; } = "";
    public event PropertyChangedEventHandler? PropertyChanged;

    private void Notify() => PropertyChanged?.Invoke(this, new(null));
    public void ReportError(string message) { Status = message; StatusKind = "Error"; Notify(); }

    public async Task ScanAsync(string root)
    {
        if (IsScanning || IsApplying) return;
        SelectedFolder = Path.GetFullPath(root);
        using var source = new CancellationTokenSource();
        cancellation = source;
        int current = ++generation;
        IsDiscovering = true;
        StatusKind = "Information";
        Status = $"Scanning {root}"; ProgressText = "Discovering files…"; Completed = 0; Total = 1; Notify();
        try
        {
            if (!Directory.Exists(root)) throw new DirectoryNotFoundException("The selected folder is no longer available.");
            var scanned = await scanner.ScanAsync(root, p => dispatch(() =>
            {
                if (current != generation || !IsScanning) return;
                IsDiscovering = p.IsDiscovering;
                Completed = p.IsDiscovering ? 0 : p.Completed; Total = Math.Max(1, p.Total);
                ProgressText = p.IsDiscovering
                    ? $"Discovering files · {p.FoldersSearched:N0} folders searched · {p.Completed:N0} media files found"
                    : $"Reading metadata · {p.Completed:N0} / {p.Total:N0}"; Notify();
            }), source.Token);
            if (!source.IsCancellationRequested || scanned.Count > 0)
            {
                records = scanned;
                HasScannedFolder = true;
                rootPath = Path.GetFullPath(root);
                Refresh();
                await LoadLogAsync();
                Status = source.IsCancellationRequested ? $"Partial results: {root}" : root;
            }
            else Status = "Scan stopped. Previous results kept.";
            StatusKind = source.IsCancellationRequested ? "Warning" : "Success";
            ProgressText = source.IsCancellationRequested ? "Scan stopped" : "Scan complete";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        { Status = $"Scan failed: {ex.Message}"; StatusKind = "Error"; }
        finally { cancellation = null; IsDiscovering = false; Notify(); }
    }

    public void Stop() { cancellation?.Cancel(); if (IsScanning) { Status = "Stopping scan…"; Notify(); } }

    public void Refresh(bool clearSelection = true)
    {
        if (clearSelection) Selection.Clear();
        var visible = LibraryQuery.Filter(records, Search, Media, View == "Review", View == "Review" ? Issue : "All review items");
        if (SortColumn is { } column) visible = Descending ? visible.OrderByDescending(r => LibraryQuery.SortValue(r, column)) : visible.OrderBy(r => LibraryQuery.SortValue(r, column));
        Rows.Clear();
        foreach (var record in visible) Rows.Add(new(record) { IsSelected = Selection.Selected.Contains(record.Path) });
        Notify();
    }

    public void Sort(string column)
    {
        Descending = SortColumn == column && !Descending;
        SortColumn = column;
        var ordered = (Descending
            ? Rows.OrderByDescending(row => LibraryQuery.SortValue(row.Record, column))
            : Rows.OrderBy(row => LibraryQuery.SortValue(row.Record, column))).ToArray();
        // A Clear/Reset temporarily removes the entire table and resets its
        // ScrollViewer. Move existing rows so viewport and checkbox state survive.
        for (int index = 0; index < ordered.Length; index++)
            if (!ReferenceEquals(Rows[index], ordered[index]))
                Rows.Move(Rows.IndexOf(ordered[index]), index);
        Notify();
    }
    public void Click(MediaRow row, bool ctrl, bool shift, bool checkbox) { Selection.Click(Rows.Select(r => r.Record.Path).ToArray(), row.Record.Path, ctrl, shift, checkbox); SyncSelection(); }
    public void SelectAll() { Selection.SelectAll(Rows.Select(r => r.Record.Path).ToArray()); SyncSelection(); }
    public void ClearSelection() { Selection.Clear(); SyncSelection(); }

    public void SetRepairMethod(string id)
    {
        _ = RepairPlanner.FindMethod(id);
        SelectedRepairMethodId = id;
        Notify();
    }

    public void SetCreateBackup(bool enabled)
    {
        CreateBackup = enabled;
        Notify();
    }

    public RepairPreview BuildRepairPreview()
    {
        if (rootPath is null) throw new InvalidOperationException("Choose and scan a folder first.");
        RepairMethod method = RepairPlanner.FindMethod(SelectedRepairMethodId);
        var selected = records.Where(record => Selection.Selected.Contains(record.Path)).ToArray();
        if (selected.Length == 0) throw new InvalidOperationException("Select at least one file first.");
        return RepairPlanner.Preview(selected, method) with { CreateBackup = CreateBackup, ScanRoot = rootPath };
    }

    public async Task<IReadOnlyList<RepairExecutionResult>> ApplyRepairAsync(RepairPreview preview)
    {
        if (rootPath is null) throw new InvalidOperationException("Choose and scan a folder first.");
        if (IsScanning || IsApplying) throw new InvalidOperationException("Finish the current operation first.");
        if (preview.ApplicableCount == 0) return [];

        // Freeze the safety choice associated with the confirmation that just
        // occurred. Async execution must not observe a later UI toggle change.
        if (!string.Equals(preview.ScanRoot, rootPath, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The scanned folder changed since the preview. Review changes again.");
        bool createBackup = preview.CreateBackup;
        applying = true;
        StatusKind = "Information";
        Status = $"Applying {preview.ApplicableCount} repair{(preview.ApplicableCount == 1 ? "" : "s")}…";
        Notify();
        try
        {
            var service = new RepairService(rootPath);
            IReadOnlyList<RepairExecutionResult> results = await Task.Run(() => service.ApplyBatch(preview, createBackup));
            var replacements = results
                .Where(result => result.Success && result.RefreshedRecord is not null)
                .ToDictionary(result => result.Plan.Path, result => result.RefreshedRecord!, StringComparer.OrdinalIgnoreCase);
            records = records.Select(record => replacements.TryGetValue(record.Path, out var replacement) ? replacement : record).ToArray();
            Selection.Clear();
            Refresh(false);
            await LoadLogAsync();
            int succeeded = results.Count(result => result.Success);
            int failed = results.Count - succeeded;
            StatusKind = failed == 0 ? "Success" : "Warning";
            Status = failed == 0
                ? $"Applied {succeeded} repair{(succeeded == 1 ? "" : "s")}."
                : $"Applied {succeeded}; {failed} failed. See Repair log for details.";
            return results;
        }
        finally
        {
            applying = false;
            Notify();
        }
    }

    private async Task LoadLogAsync()
    {
        if (rootPath is null) { Log = "No folder loaded."; return; }
        string path = Path.Combine(rootPath, ".photo-repair-repair-log.csv");
        try
        {
            HasRepairLog = File.Exists(path);
            Log = HasRepairLog ? await File.ReadAllTextAsync(path) : "No repairs have been recorded for this folder.";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { HasRepairLog = true; Log = $"Could not read repair log: {ex.Message}"; }
    }

    private void SyncSelection()
    {
        foreach (var row in Rows) row.IsSelected = Selection.Selected.Contains(row.Record.Path);
        Notify();
    }
}
