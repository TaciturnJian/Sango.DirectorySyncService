using Fleck;

namespace Sango.DirectorySyncService;

public class DirectorySyncer(string source, IEnumerable<string> destinations) : ITriggerCheck
{
    public string Source { get; } = source;

    public string SourceName => Path.GetFileName(Source);

    private List<FileSyncer> FileWatchers { get; } = [];
    private List<DirectorySyncer> DirectorySyncers { get; } = [];

    public void Check()
    {
        if (!Directory.Exists(Source)) return;

        var child_files = Directory.GetFiles(Source);
        foreach (var child_full_name in child_files)
        {
            var relative_name = Path.GetRelativePath(Source, child_full_name);
            if (FileWatchers.Any(watcher => watcher.FilePath == child_full_name)) continue;

            FleckLog.Info($"文件夹监控> 源({SourceName}): 正在监控文件({relative_name})");
            var watcher = new FileSyncer(child_full_name);
            foreach (var destination in destinations) watcher.AddDestinationDirectory(destination);
            FileWatchers.Add(watcher);
        }

        foreach (var watcher in FileWatchers) watcher.Check();

        var children = Directory.GetDirectories(Source);
        foreach (var child_full_name in children)
        {
            var relative_name = Path.GetRelativePath(Source, child_full_name);
            if (DirectorySyncers.Any(syncer => syncer.Source == child_full_name)) continue;
            FleckLog.Info($"文件夹监控> 源({SourceName}): 正在监控文件夹({relative_name})");
            DirectorySyncers.Add(new DirectorySyncer(
                child_full_name, destinations.Select(
                    destination => Path.Combine(destination,
                        Path.GetFileName(child_full_name)))));
        }

        foreach (var syncer in DirectorySyncers) syncer.Check();
    }
}
