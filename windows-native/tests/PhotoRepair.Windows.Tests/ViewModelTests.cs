using PhotoRepair.Core;

namespace PhotoRepair.Windows.Tests;

public sealed class ViewModelTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "PhotoRepair.ViewModel", Guid.NewGuid().ToString("N"));
    public ViewModelTests() => Directory.CreateDirectory(root);
    public void Dispose() => Directory.Delete(root, true);

    [Fact]
    public async Task SelectingFolderDoesNotScanAndScanRequiresExplicitAction()
    {
        File.WriteAllText(Path.Combine(root, "sample.jpg"), "fixture");
        int reads = 0;
        var scanner = new MediaScanner(reader: (path, _) =>
        {
            Interlocked.Increment(ref reads);
            return Task.FromResult<MediaRecord?>(new(path, "image", 1, DateTimeOffset.Now, DateTimeOffset.Now));
        });
        var model = new InspectionViewModel(a => a(), scanner);
        Assert.False(model.CanScan);
        Assert.False(model.HasScannedFolder);
        await model.ScanSelectedFolderAsync();
        Assert.Equal(0, reads);
        model.SelectFolder(root);
        Assert.False(model.HasScannedFolder);
        Assert.Equal(root, model.SelectedFolder);
        Assert.True(model.CanScan);
        Assert.Empty(model.Rows);
        Assert.Equal(0, reads);
        model.SetFolderPickerOpen(true);
        Assert.False(model.CanScan);
        Assert.False(model.CanSelectFolder);
        model.SetFolderPickerOpen(false);
        await model.ScanSelectedFolderAsync();
        Assert.Equal(1, reads);
        Assert.Single(model.Rows);
        Assert.True(model.HasScannedFolder);
        string other = Path.Combine(root, "other");
        Directory.CreateDirectory(other);
        model.SelectFolder(other);
        Assert.Equal(1, reads);
        Assert.Single(model.Rows);
        Assert.Equal(root, model.Status);
        await model.ScanSelectedFolderAsync();
        Assert.Empty(model.Rows);
        Assert.Equal(other, model.Status);
    }
    [Fact]
    public async Task IndividualSelectAllClearAndRetoggleKeepRowsAndCountsInSync()
    {
        foreach (string name in new[] { "a.jpg", "b.jpg", "c.jpg", "d.jpg" })
            File.WriteAllText(Path.Combine(root, name), "fixture");
        var model = new InspectionViewModel(a => a());
        await model.ScanAsync(root);
        var first = model.Rows[0];
        var second = model.Rows[1];
        void Check(int expected)
        {
            Assert.Equal(expected, model.Selection.Selected.Count);
            Assert.Equal(expected, model.Rows.Count(r => r.IsSelected));
            Assert.Equal($"{expected} selected · Backups on", model.SelectionSummary);
            Assert.All(model.Rows, row => Assert.Equal(model.Selection.Selected.Contains(row.Record.Path), row.IsSelected));
        }
        // Observe the same objects through all updates, as a realized template does.
        List<bool> firstUpdates = [];
        first.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(MediaRow.IsSelected)) firstUpdates.Add(first.IsSelected); };
        model.Click(first, false, false, true);
        model.Click(second, false, false, true);
        Check(2);
        model.SelectAll(); Check(4);
        model.ClearSelection(); Check(0);
        Assert.False(first.IsSelected); Assert.False(second.IsSelected);
        Assert.False(firstUpdates[^1]);
        model.SelectAll(); Check(4);
        model.Click(first, false, false, true); Check(3);
        model.Click(second, false, false, true); Check(2);
        model.Click(first, false, false, true); Check(3);
        model.Click(second, false, false, true); Check(4);
        model.ClearSelection(); Check(0);
        Assert.False(firstUpdates[^1]);
        model.Click(first, false, false, true); Check(1);
        model.Click(model.Rows[3], false, true, true); Check(4);
        model.Sort("File"); Check(4);
        model.ClearSelection(); Check(0);
    }
    [Fact]
    public async Task SortingTogglesDirectionChangesColumnAndPreservesSelection()
    {
        File.WriteAllText(Path.Combine(root, "a.jpg"), "1234567890");
        File.WriteAllText(Path.Combine(root, "b.jpg"), "1");
        var model = new InspectionViewModel(a => a());
        await model.ScanAsync(root);
        model.SelectAll();
        var originalRows = model.Rows.ToArray();
        List<System.Collections.Specialized.NotifyCollectionChangedAction> updates = [];
        model.Rows.CollectionChanged += (_, e) => updates.Add(e.Action);
        model.Sort("Size");
        Assert.Equal("Size", model.SortColumn);
        Assert.False(model.Descending);
        Assert.Equal(new[] { "b.jpg", "a.jpg" }, model.Rows.Select(r => r.Record.Name));
        model.Sort("Size");
        Assert.True(model.Descending);
        Assert.Equal(new[] { "a.jpg", "b.jpg" }, model.Rows.Select(r => r.Record.Name));
        model.Sort("File");
        Assert.False(model.Descending);
        Assert.Equal("File", model.SortColumn);
        Assert.Equal(new[] { "a.jpg", "b.jpg" }, model.Rows.Select(r => r.Record.Name));
        Assert.All(model.Rows, row => Assert.True(row.IsSelected));
        Assert.All(model.Rows, row => Assert.Contains(row, originalRows));
        Assert.NotEmpty(updates);
        Assert.All(updates, action => Assert.Equal(System.Collections.Specialized.NotifyCollectionChangedAction.Move, action));
        model.Search = "b.jpg"; model.Refresh();
        model.SelectAll();
        Assert.Equal(Path.Combine(root, "b.jpg"), Assert.Single(model.Selection.Selected));
        Assert.True(Assert.Single(model.Rows).IsSelected);
    }
    [Fact]
    public async Task ScanFilterSortAndSelectionShareOneModel()
    {
        File.WriteAllText(Path.Combine(root, "missing.jpg"), "bad image");
        File.Copy(Path.Combine(AppContext.BaseDirectory, "Fixtures", "nested.jpg"), Path.Combine(root, "taken.jpg"));
        File.WriteAllText(Path.Combine(root, "movie.mp4"), "video");
        var model = new InspectionViewModel(a => a());
        await model.ScanAsync(root);
        Assert.False(model.IsScanning);
        Assert.Equal(3, model.Rows.Count);
        Assert.Equal("Scan complete", model.ProgressText);
        model.View = "Review"; model.Refresh();
        Assert.Equal("missing.jpg", Assert.Single(model.Rows).Record.Name);
        model.SelectAll(); Assert.Single(model.Selection.Selected);
        model.View = "Library"; model.Search = "TAKEN"; model.Refresh();
        Assert.Empty(model.Selection.Selected);
        Assert.Equal("taken.jpg", Assert.Single(model.Rows).Record.Name);
        model.Search = ""; model.Media = "video"; model.Refresh();
        Assert.Equal("movie.mp4", Assert.Single(model.Rows).Record.Name);
        model.Media = "All"; model.Refresh(); model.Sort("Size");
        Assert.Equal(model.Rows.Select(r => r.Record.SizeBytes).Order(), model.Rows.Select(r => r.Record.SizeBytes));
        model.SelectAll(); model.Sort("Size"); Assert.Equal(3, model.Selection.Selected.Count);
    }

    [Fact]
    public async Task PreviewUsesSelectedReviewRecordsAndLeavesFilesUntouched()
    {
        string path = Path.Combine(root, "IMG_20240321_174532.jpg");
        byte[] bytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "empty.jpg"));
        File.WriteAllBytes(path, bytes);
        var model = new InspectionViewModel(a => a());
        await model.ScanAsync(root);
        Assert.False(model.CanReviewChanges);
        model.View = "Review"; model.Refresh(); model.SelectAll();
        Assert.True(model.CanReviewChanges);
        var plan = model.BuildRepairPreview();
        Assert.Equal(path, Assert.Single(plan.Items).Record.Path);
        Assert.Equal(bytes, File.ReadAllBytes(path));
        Assert.False(Directory.Exists(Path.Combine(root, MediaRules.BackupDirectory)));
        Assert.False(File.Exists(Path.Combine(root, ".photo-repair-repair-log.csv")));
        model.ClearSelection();
        Assert.Throws<InvalidOperationException>(() => model.BuildRepairPreview());
    }
    [Fact]
    public async Task FailedScanKeepsPreviousResultsAndReenablesScan()
    {
        File.WriteAllText(Path.Combine(root, "a.jpg"), "fixture");
        var model = new InspectionViewModel(a => a());
        await model.ScanAsync(root);
        await model.ScanAsync(Path.Combine(root, "missing"));
        Assert.Single(model.Rows);
        Assert.True(model.CanScan);
        Assert.StartsWith("Scan failed:", model.Status);
    }

    [Fact]
    public async Task RepairPreviewAndApplyRefreshTheRecordAndLog()
    {
        string path = Path.Combine(root, "IMG_20240321_174532.jpg");
        File.WriteAllText(path, "fixture");
        File.SetCreationTime(path, new DateTime(2024, 3, 21, 18, 0, 0));
        File.SetLastWriteTime(path, new DateTime(2024, 3, 21, 19, 0, 0));
        var model = new InspectionViewModel(a => a());
        await model.ScanAsync(root);
        model.SelectAll();
        model.SetRepairMethod("created:filename");

        RepairPreview preview = model.BuildRepairPreview();
        Assert.Equal(1, preview.ApplicableCount);
        Assert.True(model.CreateBackup);

        var results = await model.ApplyRepairAsync(preview);

        Assert.True(Assert.Single(results).Success);
        Assert.Equal("2024-03-21 17:45:32", Assert.Single(model.Rows).Record.Created);
        Assert.Empty(model.Selection.Selected);
        Assert.Contains("OK", model.Log);
        Assert.True(File.Exists(Path.Combine(root, MediaRules.BackupDirectory, Path.GetFileName(path))));
    }
    [Fact]
    public async Task PreviewPreservesBackupChoiceAndLoadedRootAfterFolderSelection()
    {
        string path = Path.Combine(root, "IMG_20240321_174532.jpg");
        File.Copy(Path.Combine(AppContext.BaseDirectory, "Fixtures", "empty.jpg"), path);
        var model = new InspectionViewModel(a => a());
        await model.ScanAsync(root);
        model.SelectAll();
        model.SetRepairMethod("taken:filename");
        model.SetCreateBackup(false);
        var preview = model.BuildRepairPreview();
        model.SetCreateBackup(true);
        string other = Path.Combine(root, "other");
        Directory.CreateDirectory(other);
        model.SelectFolder(other);
        var results = await model.ApplyRepairAsync(preview);
        Assert.True(Assert.Single(results).Success);
        Assert.False(Directory.Exists(Path.Combine(root, MediaRules.BackupDirectory)));
        Assert.Contains("OK (no backup)", model.Log);
        Assert.False(File.Exists(Path.Combine(other, ".photo-repair-repair-log.csv")));
        model.View = "Library"; model.Refresh();
        Assert.Equal("2024-03-21 17:45:32", Assert.Single(model.Rows).Record.Taken);
        model.View = "Review"; model.Refresh();
        Assert.Empty(model.Rows);
        await model.ScanSelectedFolderAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => model.ApplyRepairAsync(preview));
    }

}
