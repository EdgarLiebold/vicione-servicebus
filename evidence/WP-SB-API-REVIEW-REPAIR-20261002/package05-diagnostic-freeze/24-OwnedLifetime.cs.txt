using System.Runtime.ExceptionServices;

namespace ViciOneReview.CorePackage05;

// Runs every owned cleanup; a body failure remains primary and keeps its stack.
internal static class OwnedLifetime
{
    public static async Task RunAsync(Func<Task> body, params Func<Task>[] cleanup)
    {
        var failures = new List<Exception>();
        try { await body(); }
        catch (Exception failure) { failures.Add(failure); }
        foreach (Func<Task> action in cleanup)
        {
            try { await action(); }
            catch (Exception failure) { failures.Add(failure); }
        }
        if (failures.Count == 1) ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures.Count > 1) throw new AggregateException("Owned operation and cleanup failed.", failures);
    }
}
