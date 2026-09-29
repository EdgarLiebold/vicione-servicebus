using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Scheduling;

/// <summary>Schedules messages by setting the native Azure Service Bus enqueue time.</summary>
public class ServiceBusScheduleMessageProvider :
    IScheduleMessageProvider,
    Advanced.IScheduleCancellationCapability
{
    /// <inheritdoc />
    public Advanced.ScheduleCancellationMode CancellationMode => Advanced.ScheduleCancellationMode.ProviderAssignedToken;

    readonly ISendEndpointProvider _sendEndpointProvider;

    /// <summary>Creates a scheduler using the supplied endpoint provider.</summary>
    /// <param name="sendEndpointProvider">The provider used to resolve destination endpoints.</param>
    public ServiceBusScheduleMessageProvider(ISendEndpointProvider sendEndpointProvider)
    {
        _sendEndpointProvider = sendEndpointProvider;
    }

    /// <summary>Creates a scheduler using a consume context while bypassing its outbox.</summary>
    /// <param name="consumeContext">The current consume context.</param>
    public ServiceBusScheduleMessageProvider(ConsumeContext consumeContext)
    {
        var context = InternalOutboxExtensions.SkipOutbox(consumeContext);

        _sendEndpointProvider = context;
    }

    /// <summary>Sends a message with a scheduled enqueue time.</summary>
    /// <typeparam name="T">The message contract.</typeparam>
    /// <param name="destinationAddress">The destination endpoint address.</param>
    /// <param name="dueAt">The UTC instant at which Azure Service Bus should enqueue the message.</param>
    /// <param name="message">The message to schedule.</param>
    /// <param name="pipe">The send pipe applied to the scheduled message.</param>
    /// <param name="cancellationToken">Cancels endpoint resolution and send.</param>
    /// <returns>A task that produces the scheduled-message handle after the broker accepts the send.</returns>
    public async Task<ScheduledMessage<T>> ScheduleSendAsync<T>(Uri destinationAddress, DateTimeOffset dueAt, T message, IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken)
        where T : class
    {
        if (!MessageTypeCache<T>.IsValidMessageType)
            throw new ArgumentException(MessageTypeCache<T>.InvalidMessageTypeReason, nameof(T));

        var scheduleMessagePipe = new ScheduleSendPipe<T>(pipe, dueAt);

        var endpoint = await _sendEndpointProvider.GetSendEndpointAsync(destinationAddress, cancellationToken: cancellationToken).ConfigureAwait(false);

        await endpoint.SendAsync(message, scheduleMessagePipe, cancellationToken).ConfigureAwait(false);

        var accepted = scheduleMessagePipe.AcceptResult();
        return new ScheduledMessageHandle<T>(accepted.ScheduledMessageId ?? NewId.NextGuid(), dueAt, destinationAddress, message);
    }

    /// <summary>Completes without contacting Azure Service Bus because cancellation requires a destination address.</summary>
    /// <param name="tokenId">The schedule token, retained for interface compatibility.</param>
    /// <param name="cancellationToken">Returns a canceled task when cancellation is already requested.</param>
    /// <returns>A completed task when cancellation was not requested.</returns>
    public Task CancelScheduledSendAsync(Guid tokenId, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return Task.CompletedTask;
    }

    /// <summary>Sends a cancellation command to the destination that scheduled the message.</summary>
    /// <param name="destinationAddress">The scheduling destination endpoint.</param>
    /// <param name="tokenId">The schedule token to cancel.</param>
    /// <param name="cancellationToken">Cancels endpoint resolution and command send.</param>
    /// <returns>A task that completes when the cancellation command has been sent.</returns>
    public async Task CancelScheduledSendAsync(Uri destinationAddress, Guid tokenId, CancellationToken cancellationToken)
    {
        var endpoint = await _sendEndpointProvider.GetSendEndpointAsync(destinationAddress, cancellationToken: cancellationToken).ConfigureAwait(false);

        await endpoint.SendAsync<CancelScheduledMessage>(new
        {
            Timestamp = TimeProvider.System.GetUtcNow(),
            TokenId = tokenId
        }, cancellationToken).ConfigureAwait(false);
    }
}
