using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>Owns all state changes for one scoped EF outbox session.</summary>
internal sealed class EntityFrameworkOutboxWriteCoordinator : IDisposable
{
    readonly SemaphoreSlim _gate = new(1, 1);

    public async Task ExecuteAsync(Func<Task> action, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await action().ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public void ExecuteSynchronous(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (!_gate.Wait(0))
            throw new InvalidOperationException("The transactional outbox is busy with an asynchronous state change.");
        try
        {
            action();
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Dispose()
    {
        _gate.Dispose();
    }
}
