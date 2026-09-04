using System;
using System.Threading;
using System.Threading.Tasks;

#nullable enable
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

    public void ExecuteBlocking(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        _gate.Wait();
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
