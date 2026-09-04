using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Scheduling;

public class PublishScheduleMessageProvider :
    BaseScheduleMessageProvider
{
    readonly IPublishEndpoint _publishEndpoint;

    public PublishScheduleMessageProvider(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    protected override Task ScheduleSendAsync(ScheduleMessage message, IPipe<SendContext<ScheduleMessage>> pipe, CancellationToken cancellationToken = default)
    {
        return _publishEndpoint.PublishAsync(message, pipe, cancellationToken);
    }

    protected override Task CancelScheduledSendAsync(Guid tokenId, Uri? destinationAddress, CancellationToken cancellationToken = default)
    {
        return _publishEndpoint.PublishAsync<CancelScheduledMessage>(new
        {
            InVar.Timestamp,
            TokenId = tokenId
        }, cancellationToken);
    }
}
