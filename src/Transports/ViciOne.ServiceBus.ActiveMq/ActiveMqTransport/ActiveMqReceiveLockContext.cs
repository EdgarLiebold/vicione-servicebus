using System;
using System.Threading.Tasks;
using Apache.NMS;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>Completes ActiveMQ receive acknowledgements for a native message.</summary>
public class ActiveMqReceiveLockContext :
    ReceiveLockContext
{
    readonly IMessage _message;

    /// <summary>Creates a receive-lock context for an Apache NMS message.</summary>
    /// <param name="message">The message to acknowledge.</param>
    public ActiveMqReceiveLockContext(IMessage message)
    {
        _message = message;
    }

    /// <summary>Acknowledges successful processing of the native message.</summary>
    /// <param name="cancellationToken">The token checked before acknowledgement begins.</param>
    /// <returns>A task that completes when the broker acknowledgement completes.</returns>
    public Task CompleteAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return _message.AcknowledgeAsync();
    }

    /// <summary>Completes the fault notification; ActiveMQ requires no separate lock action for this callback.</summary>
    /// <param name="exception">The processing failure reported by the receive pipeline.</param>
    /// <param name="cancellationToken">The token checked before completing the callback.</param>
    /// <returns>A completed task unless cancellation was already requested.</returns>
    public Task FaultedAsync(Exception exception, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return Task.CompletedTask;
    }

    /// <summary>Completes lock validation; Apache NMS does not expose a renewable message lock.</summary>
    /// <param name="cancellationToken">The token checked before completing validation.</param>
    /// <returns>A completed task unless cancellation was already requested.</returns>
    public Task ValidateLockStatusAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return Task.CompletedTask;
    }
}
