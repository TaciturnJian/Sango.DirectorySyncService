using Fleck;

namespace Sango.DirectorySyncService;

/// <summary>
///     代表文件夹同步器，提供文件夹内文件监控/同步功能。
/// </summary>
/// <param name="source">源文件夹，此文件夹下的所有文件和所有子文件夹都会被监控和同步</param>
/// <param name="destinations">目标文件夹，将会把 source 中被影响的内容同步到目标文件夹，注意两个文件夹的名称可以不同</param>
public class DirectorySyncer(string source, IEnumerable<string> destinations) : ITriggerCheck
{
    /// <summary>
    ///     源文件夹的路径，此文件夹下的所有文件和所有子文件夹都会被监控和同步
    /// </summary>
    public string Source { get; } = source;

    /// <summary>
    ///     源文件夹的名称，例如 "C:\Users\user\Documents" 的名称是 "Documents"
    /// </summary>
    public string SourceName => Path.GetFileName(Source);

    private List<FileSyncer> FileSyncers { get; } = [];
    private List<DirectorySyncer> DirectorySyncers { get; } = [];

    /// <summary>
    ///     触发同步器的检查，将会检查源文件夹下的所有文件和子文件夹
    /// </summary>
    public void Check()
    {
        if (!Directory.Exists(Source)) return;

        CheckNewFileToWatch(Directory.GetFiles(Source));
        FileSyncers.Check();
        CheckNewDirectoryToWatch(Directory.GetDirectories(Source));
        DirectorySyncers.Check();
    }

    private void LogNewFileToWatch(string fullPath)
    {
        var relative_name = Path.GetRelativePath(Source, fullPath);
        FleckLog.Info($"文件夹监控> 源({SourceName}): 正在监控文件({relative_name})");
    }

    private void CheckNewFileToWatch(string fullPath)
    {
        if (FileSyncers.Any(watcher => watcher.FilePath == fullPath)) return;
        LogNewFileToWatch(fullPath);
        var watcher = new FileSyncer(fullPath);
        watcher.AddDestinations(destinations);
        FileSyncers.Add(watcher);
    }

    private void CheckNewFileToWatch(IEnumerable<string> files)
    {
        foreach (var file in files) CheckNewFileToWatch(file);
    }

    private void CheckNewDirectoryToWatch(string fullPath)
    {
        if (DirectorySyncers.Any(syncer => syncer.Source == fullPath)) return;
        var relative_name = Path.GetRelativePath(Source, fullPath);
        FleckLog.Info($"文件夹监控> 源({SourceName}): 正在监控文件夹({relative_name})");
        DirectorySyncers.Add(
            new DirectorySyncer(
                fullPath,
                destinations.Select(destination => Path.Combine(
                    destination,
                    Path.GetFileName(fullPath)))));
    }

    private void CheckNewDirectoryToWatch(IEnumerable<string> directories)
    {
        foreach (var directory in directories) CheckNewDirectoryToWatch(directory);
    }
}