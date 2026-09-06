using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Completes or requeues one manually acknowledged RabbitMQ delivery.</summary>
public class RabbitMqReceiveLockContext :
    ReceiveLockContext
{
    readonly CancellationToken _cancellationToken;
    readonly ChannelContext _channel;
    readonly ulong _deliveryTag;

    /// <summary>Creates acknowledgement state for one channel-scoped delivery tag.</summary>
    /// <param name="channel">The RabbitMQ channel context that received the message.</param>
    /// <param name="deliveryTag">The channel-scoped delivery tag.</param>
    /// <param name="cancellationToken">The receive-context cancellation used for broker acknowledgements.</param>
    public RabbitMqReceiveLockContext(ChannelContext channel, ulong deliveryTag, CancellationToken cancellationToken)
    {
        _channel = channel;
        _deliveryTag = deliveryTag;
        _cancellationToken = cancellationToken;
    }

    /// <summary>Acknowledges successful processing of the delivery.</summary>
    /// <param name="cancellationToken">Cancellation checked before acknowledgement begins.</param>
    /// <returns>A task that completes after the acknowledgement is written.</returns>
    public async Task CompleteAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); if (_channel.Channel.IsClosed)
        {
            // Preserve a broker close reason when available; otherwise identify the synthesized
            // unavailable state as library-initiated.
            var reason = _channel.Channel.CloseReason;

            throw new OperationInterruptedException(reason
                ?? new ShutdownEventArgs(ShutdownInitiator.Library, 491, "The channel is no longer available"));
        }

        _cancellationToken.ThrowIfCancellationRequested();

        try
        {
            await _channel.BasicAckAsync(_deliveryTag, false, _cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            throw new TransportUnavailableException($"Message ACK failed: {_deliveryTag}", exception);
        }
    }

    /// <summary>Best-effort requeues the delivery after processing fails.</summary>
    /// <param name="exception">The processing failure included in acknowledgement diagnostics.</param>
    /// <param name="cancellationToken">Cancellation checked before negative acknowledgement begins.</param>
    /// <returns>A task that completes after the negative acknowledgement attempt.</returns>
    public async Task FaultedAsync(Exception exception, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); if (_channel.Channel.IsClosed || _cancellationToken.IsCancellationRequested)
            return;

        try
        {
            await _channel.BasicNackAsync(_deliveryTag, false, true, _cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ackEx)
        {
            LogContext.Error?.Log(ackEx, "Message NACK failed: {DeliveryTag}, Original Exception: {Exception}", _deliveryTag, exception);
        }
    }

    /// <summary>Verifies that the channel and receive context can still acknowledge the delivery.</summary>
    /// <param name="cancellationToken">Cancellation checked before validation.</param>
    /// <returns>A completed task when acknowledgement remains available.</returns>
    public Task ValidateLockStatusAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); if (_channel.Channel.IsClosed)
        {
            // Preserve a broker close reason when available; otherwise identify the synthesized
            // unavailable state as library-initiated.
            var reason = _channel.Channel.CloseReason;

            throw new OperationInterruptedException(reason
                ?? new ShutdownEventArgs(ShutdownInitiator.Library, 491, "The channel is no longer available"));
        }

        _cancellationToken.ThrowIfCancellationRequested();

        return Task.CompletedTask;
    }
}
