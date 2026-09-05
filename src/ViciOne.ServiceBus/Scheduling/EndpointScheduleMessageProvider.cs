using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Scheduling;

/// <summary>
/// Provides an endpoint schedule message provider implementation.
/// </summary>
public class EndpointScheduleMessageProvider :
    BaseScheduleMessageProvider
{
    readonly Func<Task<ISendEndpoint>> _schedulerEndpoint;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="schedulerEndpoint">The scheduler endpoint value.</param>
    public EndpointScheduleMessageProvider(Func<Task<ISendEndpoint>> schedulerEndpoint)
    {
        _schedulerEndpoint = schedulerEndpoint;
    }

    /// <summary>
    /// Schedules send.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    protected override async Task ScheduleSendAsync(ScheduleMessage message, IPipe<SendContext<ScheduleMessage>> pipe, CancellationToken cancellationToken = default)
    {
        var endpoint = await _schedulerEndpoint().ConfigureAwait(false);

        await endpoint.SendAsync(message, pipe, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Determines whether the current value can cel scheduled send.
    /// </summary>
    /// <param name="tokenId">The token id value.</param>
    /// <param name="destinationAddress">The destination address value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
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
