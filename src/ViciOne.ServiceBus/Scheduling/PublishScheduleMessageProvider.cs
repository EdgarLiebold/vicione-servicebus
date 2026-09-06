using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Scheduling;

/// <summary>Provides publish schedule message services.</summary>
public class PublishScheduleMessageProvider :
    BaseScheduleMessageProvider
{
    readonly IPublishEndpoint _publishEndpoint;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="publishEndpoint">The publish endpoint.</param>
    public PublishScheduleMessageProvider(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    /// <summary>Schedules send.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    protected override Task ScheduleSendAsync(ScheduleMessage message, IPipe<SendContext<ScheduleMessage>> pipe, CancellationToken cancellationToken = default)
    {
        return _publishEndpoint.PublishAsync(message, pipe, cancellationToken);
    }

    /// <summary>Determines whether the current value can cel scheduled send.</summary>
    /// <param name="tokenId">The token id.</param>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    protected override Task CancelScheduledSendAsync(Guid tokenId, Uri? destinationAddress, CancellationToken cancellationToken = default)
    {
        return _publishEndpoint.PublishAsync<CancelScheduledMessage>(new
        {
            Timestamp = TimeProvider.System.GetUtcNow(),
            TokenId = tokenId
        }, cancellationToken);
    }
}
