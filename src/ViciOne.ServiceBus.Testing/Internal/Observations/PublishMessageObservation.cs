using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing.Internal;

/// <summary>Controls the lifetime and result of a temporary published-message observation endpoint.</summary>
/// <typeparam name="TMessage">The published message contract.</typeparam>
internal sealed class PublishMessageObservation<TMessage> :
    IPublishMessageObservation<TMessage>
    where TMessage : class
{
    readonly IHostReceiveEndpointHandle _endpointHandle;
    readonly TimeProvider _timeProvider;
    readonly TimeSpan _timeout;
    int _disposed;

    /// <summary>Creates an observation for an endpoint that has completed its readiness contract.</summary>
    /// <param name="endpointHandle">The temporary endpoint owned by the observation.</param>
    /// <param name="message">The first accepted message context.</param>
    /// <param name="timeout">The maximum time allowed to stop the endpoint.</param>
    /// <param name="timeProvider">The clock used to enforce the stop timeout.</param>
    public PublishMessageObservation(
        IHostReceiveEndpointHandle endpointHandle,
        Task<ConsumeContext<TMessage>> message,
        TimeSpan timeout,
        TimeProvider timeProvider)
    {
        _endpointHandle = endpointHandle ?? throw new ArgumentNullException(nameof(endpointHandle));
        Message = message ?? throw new ArgumentNullException(nameof(message));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(timeout, TimeSpan.Zero);
        _timeout = timeout;
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <inheritdoc />
    public Task<ConsumeContext<TMessage>> Message { get; }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        await _endpointHandle.StopAsync(CancellationToken.None)
            .WaitAsync(_timeout, _timeProvider, CancellationToken.None)
            .ConfigureAwait(false);
    }
}
