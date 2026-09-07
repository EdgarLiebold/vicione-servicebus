using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Scheduling;

/// <summary>Submits scheduling commands to a dedicated endpoint.</summary>
public sealed class EndpointScheduleMessageProvider :
    BaseScheduleMessageProvider
{
    readonly Func<CancellationToken, Task<ISendEndpoint>> _schedulerEndpoint;
    readonly TimeProvider _timeProvider;

    /// <summary>Creates a provider that resolves the scheduler endpoint for each operation.</summary>
    /// <param name="schedulerEndpoint">Resolves the scheduler endpoint with caller cancellation.</param>
    /// <param name="timeProvider">The clock used to timestamp cancellation commands.</param>
    public EndpointScheduleMessageProvider(Func<CancellationToken, Task<ISendEndpoint>> schedulerEndpoint, TimeProvider? timeProvider = null)
    {
        _schedulerEndpoint = schedulerEndpoint ?? throw new ArgumentNullException(nameof(schedulerEndpoint));
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    protected override async Task ScheduleSendAsync(ScheduleMessage message, IPipe<SendContext<ScheduleMessage>> pipe, CancellationToken cancellationToken = default)
    {
        var endpoint = await _schedulerEndpoint(cancellationToken).ConfigureAwait(false);

        await endpoint.SendAsync(message, pipe, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override async Task CancelScheduledSendAsync(Guid tokenId, Uri? destinationAddress, CancellationToken cancellationToken = default)
    {
        var endpoint = await _schedulerEndpoint(cancellationToken).ConfigureAwait(false);

        await endpoint.SendAsync<CancelScheduledMessage>(new
        {
            Timestamp = _timeProvider.GetUtcNow(),
            TokenId = tokenId
        }, cancellationToken)
            .ConfigureAwait(false);
    }
}
