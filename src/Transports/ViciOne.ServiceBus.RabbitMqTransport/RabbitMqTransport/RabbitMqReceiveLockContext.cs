using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.RabbitMqTransport;

public class RabbitMqReceiveLockContext :
    ReceiveLockContext
{
    readonly CancellationToken _cancellationToken;
    readonly ChannelContext _channel;
    readonly ulong _deliveryTag;

    public RabbitMqReceiveLockContext(ChannelContext channel, ulong deliveryTag, CancellationToken cancellationToken)
    {
        _channel = channel;
        _deliveryTag = deliveryTag;
        _cancellationToken = cancellationToken;
    }

    public async Task Complete()
    {
        if (_channel.Channel.IsClosed)
        {
            // Channel.IsClosed and Channel.CloseReason are read-only diagnostics and safe to read at
            // any time; the operations themselves go through the owning context and its lease. The
            // reason reported is the one the channel actually closed for. Where there is none — a
            // close this process started — the initiator is Library, because a locally produced
            // state must not claim the peer sent it.
            var reason = _channel.Channel.CloseReason;

            throw new OperationInterruptedException(reason
                ?? new ShutdownEventArgs(ShutdownInitiator.Library, 491, "The channel is no longer available"));
        }

        _cancellationToken.ThrowIfCancellationRequested();

        try
        {
            await _channel.BasicAck(_deliveryTag, false, _cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            throw new TransportUnavailableException($"Message ACK failed: {_deliveryTag}", exception);
        }
    }

    public async Task Faulted(Exception exception)
    {
        if (_channel.Channel.IsClosed || _cancellationToken.IsCancellationRequested)
            return;

        try
        {
            await _channel.BasicNack(_deliveryTag, false, true, _cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ackEx)
        {
            LogContext.Error?.Log(ackEx, "Message NACK failed: {DeliveryTag}, Original Exception: {Exception}", _deliveryTag, exception);
        }
    }

    public Task ValidateLockStatus()
    {
        if (_channel.Channel.IsClosed)
        {
            // Channel.IsClosed and Channel.CloseReason are read-only diagnostics and safe to read at
            // any time; the operations themselves go through the owning context and its lease. The
            // reason reported is the one the channel actually closed for. Where there is none — a
            // close this process started — the initiator is Library, because a locally produced
            // state must not claim the peer sent it.
            var reason = _channel.Channel.CloseReason;

            throw new OperationInterruptedException(reason
                ?? new ShutdownEventArgs(ShutdownInitiator.Library, 491, "The channel is no longer available"));
        }

        _cancellationToken.ThrowIfCancellationRequested();

        return Task.CompletedTask;
    }
}
