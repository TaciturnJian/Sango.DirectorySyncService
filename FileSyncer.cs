using Fleck;

namespace Sango.DirectorySyncService;

public class FileSyncer(string filePath) : ITriggerCheck
{
    public enum ChangeType
    {
        Created,
        Modified,
        Deleted
    }

    public bool IsExistsInLastCheck;
    public DateTime LastWriteTime = DateTime.MinValue;

    public string FilePath => filePath;

    public string SourceName => Path.GetFileName(FilePath);

    public void Check()
    {
        var is_exists = File.Exists(FilePath);
        if (!is_exists)
        {
            if (!IsExistsInLastCheck) return;
            IsExistsInLastCheck = false;
            WhenFileChanged?.Invoke(this, new FileChangeArgs(ChangeType.Deleted, FilePath));
            return;
        }

        var last_write_time = File.GetLastWriteTime(FilePath);
        if (!IsExistsInLastCheck)
        {
            IsExistsInLastCheck = true;
            LastWriteTime = last_write_time;
            WhenFileChanged?.Invoke(this, new FileChangeArgs(ChangeType.Created, FilePath));
            return;
        }

        if (last_write_time == LastWriteTime) return;
        LastWriteTime = last_write_time;
        WhenFileChanged?.Invoke(this, new FileChangeArgs(ChangeType.Modified, FilePath));
    }

    public event EventHandler<FileChangeArgs>? WhenFileChanged;

    public void AddDestination(string destination)
    {
        if (!Directory.Exists(destination))
        {
            FleckLog.Info($"文件监控> 源({SourceName})：正在创建文件夹({destination})");
            Directory.CreateDirectory(destination);
        }

        var relative_directory = Path.GetRelativePath(FilePath, destination);
        FleckLog.Info($"文件监控> 源({SourceName}) -> 目标文件夹({relative_directory})：正在注册源文件改动事件");

        WhenFileChanged += (_, args) =>
        {
            var (change, file_path) = args;
            var target_file = Path.Combine(destination, SourceName);
            var relative_target = Path.GetRelativePath(file_path, target_file);

            switch (change)
            {
                case ChangeType.Deleted:
                    FleckLog.Info($"文件监控> 源({SourceName}) -> 目标({relative_target})：正在删除目标文件");
                    File.Delete(target_file);
                    return;
                case ChangeType.Modified:
                case ChangeType.Created:
                    FleckLog.Info($"文件监控> 源({SourceName}) -> 目标({relative_target})：正在覆盖目标文件");
                    File.Copy(file_path, target_file, true);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(args));
            }
        };
    }

    public void AddDestinations(IEnumerable<string> destinations)
    {
        foreach (var dest in destinations) AddDestination(dest);
    }

    public readonly record struct FileChangeArgs(ChangeType Change, string FilePath);
}