namespace Sango.DirectorySyncService;

public interface ITriggerCheck
{
    void Check();
}

public static class EnumerableTriggerExtension
{
    public static void Check(this IEnumerable<ITriggerCheck> triggers)
    {
        foreach (var trigger in triggers) trigger.Check();
    }
}

public static class TriggerExtension
{
    public static void RepeatCheck(this ITriggerCheck trigger, TimeSpan delay, ref readonly ulong stop)
    {
        while (Interlocked.Read(in stop) == 0)
        {
            trigger.Check();
            Thread.Sleep(delay);
        }
    }
}
