using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Retains the last value that was sent through the filter, usable as a source to a join pipe
/// </summary>
public class LatestFilter<T> :
    IFilter<T>,
    ILatestFilter<T>
    where T : class, PipeContext
{
    readonly TaskCompletionSource<bool> _hasValue;
    T _latest = null!;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public LatestFilter()
    {
        _hasValue = new TaskCompletionSource<bool>(TaskCreationOptions.None | TaskCreationOptions.RunContinuationsAsynchronously);
    }

    Task IFilter<T>.SendAsync(T context, IPipe<T> next)
    {
        ArgumentNullException.ThrowIfNull(context);

        Volatile.Write(ref _latest, context);
        _hasValue.TrySetResult(true);

        return next.SendAsync(context);
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        context.CreateFilterScope("latest");
    }

    Task<T> ILatestFilter<T>.Latest => GetLatestAsync();

    async Task<T> GetLatestAsync()
    {
        await _hasValue.Task.ConfigureAwait(false);

        return Volatile.Read(ref _latest)
            ?? throw new InvalidOperationException("The latest filter was signaled without a context.");
    }
}
