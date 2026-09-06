using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Transports.Fabric;

/// <summary>Tracks the current value of a message-fabric measurement.</summary>
public class Gauge :
    Metric
{
    long _activeCount;
    long _concurrentActiveCount;

    /// <summary>Occurs when zero active.</summary>
    public event ZeroActiveHandler? ZeroActive;

    /// <summary>Adds the supplied value to the current collection.</summary>
    public void Add()
    {
        var currentActiveCount = Interlocked.Increment(ref _activeCount);
        while (currentActiveCount < _concurrentActiveCount)
            Interlocked.CompareExchange(ref _concurrentActiveCount, currentActiveCount, _concurrentActiveCount);
    }

    /// <summary>Removes the selected value.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task RemoveAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); var pendingCount = Interlocked.Decrement(ref _activeCount);
        if (pendingCount != 0)
            return Task.CompletedTask;

        var zeroActivity = ZeroActive;
        if (zeroActivity == null)
            return Task.CompletedTask;

        return NotifyZeroActivityAsync(zeroActivity);
    }

    static Task NotifyZeroActivityAsync(ZeroActiveHandler zeroActivity)
    {
        Delegate[] invocationList = zeroActivity.GetInvocationList();

        async Task InvokeAsync()
        {
            for (var i = 0; i < invocationList.Length; i++)
            {
                if (invocationList[i] is ZeroActiveHandler handler)
                    await handler().ConfigureAwait(false);
            }
        }

        return invocationList.Length switch
        {
            0 => Task.CompletedTask,
            1 when invocationList[0] is ZeroActiveHandler handler => handler(),
            _ => InvokeAsync()
        };
    }
}
