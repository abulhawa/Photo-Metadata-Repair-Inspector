using System.IO;
using PhotoRepair.Core;

namespace PhotoRepair.Windows;

public sealed record ScanProgress(int Completed, int Total, bool IsDiscovering = false, int FoldersSearched = 0);

public sealed class MediaScanner
{
    private readonly Func<string, CancellationToken, Task<MediaRecord?>> readFile;
    public int WorkerCount { get; }
    public MediaScanner(int workerCount = 8, Func<string, CancellationToken, Task<MediaRecord?>>? reader = null)
    {
        if (workerCount is < 1 or > 8) throw new ArgumentOutOfRangeException(nameof(workerCount));
        WorkerCount = workerCount;
        var metadata = new MetadataReader();
        readFile = reader ?? ((path, _) => Task.FromResult<MediaRecord?>(metadata.ReadFile(path)));
    }

    public static IEnumerable<string> Discover(string root, CancellationToken cancellation = default, Action<ScanProgress>? progress = null)
    {
        if (cancellation.IsCancellationRequested) yield break;
        var options = new EnumerationOptions { IgnoreInaccessible = true, AttributesToSkip = FileAttributes.ReparsePoint };
        // Explicit stack skips backups before traversing, and avoids recursion depth limits.
        var pending = new Stack<string>();
        pending.Push(Path.GetFullPath(root));
        int folders = 0, found = 0;
        var clock = System.Diagnostics.Stopwatch.StartNew();
        progress?.Invoke(new(found, 0, true, folders));
        while (pending.TryPop(out string? folder) && !cancellation.IsCancellationRequested)
        {
            if (clock.ElapsedMilliseconds >= 200)
            {
                progress?.Invoke(new(found, 0, true, folders));
                clock.Restart();
            }
            folders++;
            string[] entries;
            try { entries = Directory.GetFileSystemEntries(folder, "*", options); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { continue; }
            List<string> directories = [];
            foreach (string path in entries)
            {
                if (cancellation.IsCancellationRequested) yield break;
                if (Directory.Exists(path))
                {
                    if (!string.Equals(Path.GetFileName(path), MediaRules.BackupDirectory, StringComparison.OrdinalIgnoreCase)) directories.Add(path);
                }
                else if (MediaRules.Classify(path) != "other") { found++; yield return path; }
                if (clock.ElapsedMilliseconds >= 200)
                {
                    progress?.Invoke(new(found, 0, true, folders));
                    clock.Restart();
                }
            }
            for (int i = directories.Count - 1; i >= 0; i--) pending.Push(directories[i]);
        }
        progress?.Invoke(new(found, 0, true, folders));
    }

    public Task<IReadOnlyList<MediaRecord>> ScanAsync(string root, Action<ScanProgress>? progress = null, CancellationToken cancellation = default) => Task.Run(async () =>
    {
        var paths = Discover(root, cancellation, progress).ToArray();
        progress?.Invoke(new(0, paths.Length));
        var results = new MediaRecord?[paths.Length];
        int next = -1, completed = 0;
        var gate = new object();
        async Task Worker()
        {
            while (!cancellation.IsCancellationRequested)
            {
                int index = Interlocked.Increment(ref next);
                if (index >= paths.Length) break;
                try { results[index] = await readFile(paths[index], cancellation).ConfigureAwait(false); }
                catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { break; }
                catch (Exception ex) when (MetadataReader.IsReadFailure(ex)) { /* One unreadable file cannot abort the collection. */ }
                lock (gate) { progress?.Invoke(new(++completed, paths.Length)); }
            }
        }
        if (!cancellation.IsCancellationRequested)
            await Task.WhenAll(Enumerable.Range(0, Math.Min(WorkerCount, paths.Length)).Select(_ => Task.Run(Worker))).ConfigureAwait(false);
        return (IReadOnlyList<MediaRecord>)results.OfType<MediaRecord>().ToArray();
    });
}
