using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Scheduling;

/// <summary>Provides endpoint schedule message services.</summary>
public class EndpointScheduleMessageProvider :
    BaseScheduleMessageProvider
{
    readonly Func<Task<ISendEndpoint>> _schedulerEndpoint;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="schedulerEndpoint">The scheduler endpoint.</param>
    public EndpointScheduleMessageProvider(Func<Task<ISendEndpoint>> schedulerEndpoint)
    {
        _schedulerEndpoint = schedulerEndpoint;
    }

    /// <summary>Schedules send.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    protected override async Task ScheduleSendAsync(ScheduleMessage message, IPipe<SendContext<ScheduleMessage>> pipe, CancellationToken cancellationToken = default)
    {
        var endpoint = await _schedulerEndpoint().ConfigureAwait(false);

        await endpoint.SendAsync(message, pipe, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Determines whether the current value can cel scheduled send.</summary>
    /// <param name="tokenId">The token id.</param>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    protected override async Task CancelScheduledSendAsync(Guid tokenId, Uri? destinationAddress, CancellationToken cancellationToken = default)
    {
        var endpoint = await _schedulerEndpoint().ConfigureAwait(false);

        await endpoint.SendAsync<CancelScheduledMessage>(new
        {
            Timestamp = TimeProvider.System.GetUtcNow(),
            TokenId = tokenId
        }, cancellationToken)
            .ConfigureAwait(false);
    }
}
