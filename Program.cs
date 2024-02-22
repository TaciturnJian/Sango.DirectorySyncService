using System.Collections.Immutable;
using Fleck;
using Sango.DirectorySyncService;

if (args.Length < 2)
{
    FleckLog.Info("使用方式：./SyncService <源文件夹> <目标文件夹1> <目标文件夹2> ...");
    FleckLog.Info("将会不断同步源文件夹里面的文件和子文件夹到目标文件夹，效果等同于多行 'cp 源文件夹/* 目标文件夹/ -rf'，但是只会在文件有改动时开始同步");
    return 0;
}

var source = args[0];
var destinations = args.Skip(1).ToImmutableList();
FleckLog.Info("Main> 同步参数如下：");
FleckLog.Info($"Main> [源]({source})");
var temp_index = 1;
foreach (var destination in destinations) FleckLog.Info($"Main> [目的地][{temp_index++}]({destination})");

ulong user_stop = 0;

Console.TreatControlCAsInput = false;
Console.CancelKeyPress += (_, _) =>
{
    user_stop = Interlocked.Increment(ref user_stop);
    FleckLog.Info("Main> 用户需要取消任务，已发送信号");
};

FleckLog.Info("Main> 挂起，等待用户输入回车后继续，当前可安全终止程序");
Console.ReadLine();

var syncer = new DirectorySyncer(source, destinations);
try
{
    syncer.RepeatCheck(TimeSpan.FromSeconds(1), in user_stop);
}
catch (Exception e)
{
    FleckLog.Error($"Main> 同步中出现异常：{e}");
}

FleckLog.Info("Main> 同步服务已停止");

return 0;