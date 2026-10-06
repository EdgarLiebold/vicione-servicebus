using System.Runtime.ExceptionServices;

namespace BatchClock;

public sealed class OwnedLifetime(TimeSpan timeout) : IAsyncDisposable
{
    readonly List<Func<Task>> _cleanup = [];
    readonly List<Task> _tasks = [];
    readonly List<Action> _releases = [];
    public TimeSpan Timeout { get; } = timeout;
    public void Own(Func<Task> cleanup) => _cleanup.Add(cleanup);
    public void Release(Action release) => _releases.Add(release);
    public Task Track(Task task) { _tasks.Add(task); return task; }
    public Task<T> Track<T>(Task<T> task) { _tasks.Add(task); return task; }
    public async Task Await(Task actual) => await Track(actual).WaitAsync(Timeout);
    public async Task<T> Await<T>(Task<T> actual) => await Track(actual).WaitAsync(Timeout);
    public async ValueTask DisposeAsync()
    {
        List<Exception> failures = [];
        foreach (var release in _releases) try { release(); } catch (Exception e) { failures.Add(e); }
        // Faulted tasks have already been observed by the case, but timeout/incomplete work must fail cleanup.
        foreach (Task actual in _tasks)
        {
            try { await actual.WaitAsync(Timeout); }
            catch (Exception e) { if (!actual.IsCompleted) failures.Add(e); }
        }
        for (int i = _cleanup.Count - 1; i >= 0; i--)
            try { await _cleanup[i]().WaitAsync(Timeout); } catch (Exception e) { failures.Add(e); }
        if (failures.Count == 1) ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures.Count > 1) throw new AggregateException(failures);
    }
}
