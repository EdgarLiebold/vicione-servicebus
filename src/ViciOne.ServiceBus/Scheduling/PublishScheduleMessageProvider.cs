using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Scheduling;

/// <summary>Publishes scheduling commands for an external scheduler.</summary>
public sealed class PublishScheduleMessageProvider :
    BaseScheduleMessageProvider
{
    readonly IPublishEndpoint _publishEndpoint;
    readonly TimeProvider _timeProvider;

    /// <summary>Creates a provider that publishes scheduler commands.</summary>
    /// <param name="publishEndpoint">Publishes commands to the scheduler.</param>
    /// <param name="timeProvider">The clock used to timestamp cancellation commands.</param>
    public PublishScheduleMessageProvider(IPublishEndpoint publishEndpoint, TimeProvider? timeProvider = null)
    {
        _publishEndpoint = publishEndpoint ?? throw new ArgumentNullException(nameof(publishEndpoint));
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    protected override Task ScheduleSendAsync(ScheduleMessage message, IPipe<SendContext<ScheduleMessage>> pipe, CancellationToken cancellationToken = default)
    {
        return _publishEndpoint.PublishAsync(message, pipe, cancellationToken);
    }

    /// <inheritdoc />
    protected override Task CancelScheduledSendAsync(Guid tokenId, Uri? destinationAddress, CancellationToken cancellationToken = default)
    {
        return _publishEndpoint.PublishAsync<CancelScheduledMessage>(new
        {
            Timestamp = _timeProvider.GetUtcNow(),
            TokenId = tokenId
        }, cancellationToken);
    }
}
