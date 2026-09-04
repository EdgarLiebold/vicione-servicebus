using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Scheduling;

/// <summary>
/// Provides a publish schedule message provider implementation.
/// </summary>
public class PublishScheduleMessageProvider :
    BaseScheduleMessageProvider
{
    readonly IPublishEndpoint _publishEndpoint;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="publishEndpoint">The publish endpoint value.</param>
    public PublishScheduleMessageProvider(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    /// <summary>
    /// Schedules send.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    protected override Task ScheduleSendAsync(ScheduleMessage message, IPipe<SendContext<ScheduleMessage>> pipe, CancellationToken cancellationToken = default)
    {
        return _publishEndpoint.PublishAsync(message, pipe, cancellationToken);
    }

    /// <summary>
    /// Determines whether the current value can cel scheduled send.
    /// </summary>
    /// <param name="tokenId">The token id value.</param>
    /// <param name="destinationAddress">The destination address value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    protected override Task CancelScheduledSendAsync(Guid tokenId, Uri? destinationAddress, CancellationToken cancellationToken = default)
    {
        return _publishEndpoint.PublishAsync<CancelScheduledMessage>(new
        {
            InVar.Timestamp,
            TokenId = tokenId
        }, cancellationToken);
    }
}
