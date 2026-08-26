#nullable enable
namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration;

using System;

/// <summary>Owns the synchronous mutation boundary for one scoped EF outbox context.</summary>
internal sealed class EntityFrameworkOutboxWriteCoordinator
{
    private readonly object _syncRoot = new();

    public void Execute(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        lock (_syncRoot)
            action();
    }
}
