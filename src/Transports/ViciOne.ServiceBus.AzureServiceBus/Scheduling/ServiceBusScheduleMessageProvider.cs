using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Scheduling;

/// <summary>
/// Provides a service bus schedule message provider implementation.
/// </summary>
public class ServiceBusScheduleMessageProvider :
    IScheduleMessageProvider
{
    readonly ISendEndpointProvider _sendEndpointProvider;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="sendEndpointProvider">The send endpoint provider value.</param>
    public ServiceBusScheduleMessageProvider(ISendEndpointProvider sendEndpointProvider)
    {
        _sendEndpointProvider = sendEndpointProvider;
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="consumeContext">The consume context value.</param>
    public ServiceBusScheduleMessageProvider(ConsumeContext consumeContext)
    {
        var context = InternalOutboxExtensions.SkipOutbox(consumeContext);

        _sendEndpointProvider = context;
    }

    /// <summary>
    /// Schedules send.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="destinationAddress">The destination address value.</param>
    /// <param name="dueAt">The due at value.</param>
    /// <param name="message">The message value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<ScheduledMessage<T>> ScheduleSendAsync<T>(Uri destinationAddress, DateTimeOffset dueAt, T message, IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken)
        where T : class
    {
        if (!MessageTypeCache<T>.IsValidMessageType)
            throw new ArgumentException(MessageTypeCache<T>.InvalidMessageTypeReason, nameof(T));

        var scheduleMessagePipe = new ScheduleSendPipe<T>(pipe, dueAt);

        var endpoint = await _sendEndpointProvider.GetSendEndpointAsync(destinationAddress, cancellationToken: cancellationToken).ConfigureAwait(false);

        await endpoint.SendAsync(message, scheduleMessagePipe, cancellationToken).ConfigureAwait(false);

        return new ScheduledMessageHandle<T>(scheduleMessagePipe.ScheduledMessageId ?? NewId.NextGuid(), dueAt, destinationAddress, message);
    }

    /// <summary>
    /// Determines whether the current value can cel scheduled send.
    /// </summary>
    /// <param name="tokenId">The token id value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task CancelScheduledSendAsync(Guid tokenId, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return Task.CompletedTask;
    }

    /// <summary>
    /// Determines whether the current value can cel scheduled send.
    /// </summary>
    /// <param name="destinationAddress">The destination address value.</param>
    /// <param name="tokenId">The token id value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task CancelScheduledSendAsync(Uri destinationAddress, Guid tokenId, CancellationToken cancellationToken)
    {
        var endpoint = await _sendEndpointProvider.GetSendEndpointAsync(destinationAddress, cancellationToken: cancellationToken).ConfigureAwait(false);

        await endpoint.SendAsync<CancelScheduledMessage>(new
        {
            InVar.Timestamp,
            TokenId = tokenId
        }, cancellationToken).ConfigureAwait(false);
    }
}
