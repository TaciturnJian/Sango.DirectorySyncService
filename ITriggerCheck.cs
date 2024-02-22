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