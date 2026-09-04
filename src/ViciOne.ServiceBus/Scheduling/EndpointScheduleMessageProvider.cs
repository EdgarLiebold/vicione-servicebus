using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Scheduling;

public class EndpointScheduleMessageProvider :
    BaseScheduleMessageProvider
{
    readonly Func<Task<ISendEndpoint>> _schedulerEndpoint;

    public EndpointScheduleMessageProvider(Func<Task<ISendEndpoint>> schedulerEndpoint)
    {
        _schedulerEndpoint = schedulerEndpoint;
    }

    protected override async Task ScheduleSendAsync(ScheduleMessage message, IPipe<SendContext<ScheduleMessage>> pipe, CancellationToken cancellationToken = default)
    {
        var endpoint = await _schedulerEndpoint().ConfigureAwait(false);

        await endpoint.SendAsync(message, pipe, cancellationToken).ConfigureAwait(false);
    }

    protected override async Task CancelScheduledSendAsync(Guid tokenId, Uri? destinationAddress, CancellationToken cancellationToken = default)
    {
        var endpoint = await _schedulerEndpoint().ConfigureAwait(false);

        await endpoint.SendAsync<CancelScheduledMessage>(new
        {
            InVar.Timestamp,
            TokenId = tokenId
        }, cancellationToken)
            .ConfigureAwait(false);
    }
}
