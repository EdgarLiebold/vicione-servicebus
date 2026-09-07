using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace ViciOne.ServiceBus.Middleware.Outbox;

/// <summary>Coordinates persisted outbox work with the single delivery agent for a bus scope.</summary>
/// <typeparam name="TScope">The bus and persistence scope whose delivery agent is notified.</typeparam>
public sealed class BusOutboxNotification<TScope> :
    IBusOutboxNotification<TScope>
    where TScope : class
{
    readonly object _lock = new();
    readonly OutboxDeliveryServiceOptions<TScope> _options;
    readonly TimeProvider _timeProvider;
    CancellationTokenSource? _deliverySignal;
    bool _deliveryPending;

    /// <summary>Initializes a notification channel for one outbox delivery scope.</summary>
    /// <param name="options">The polling settings for the delivery service.</param>
    /// <param name="timeProvider">The clock used for the polling interval.</param>
    public BusOutboxNotification(IOptions<OutboxDeliveryServiceOptions<TScope>> options, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value ?? throw new ArgumentException("The options wrapper must contain a value.", nameof(options));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <summary>Waits until persisted work is signaled or the configured polling interval elapses.</summary>
    /// <param name="cancellationToken">The token that cancels the wait.</param>
    /// <returns>A task that completes when the delivery agent should query for work.</returns>
    public async Task WaitForDeliveryAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        CancellationTokenSource signal;
        lock (_lock)
        {
            if (_deliveryPending)
            {
                _deliveryPending = false;
                return;
            }

            if (_deliverySignal != null)
                throw new InvalidOperationException($"Only one outbox delivery agent may wait on {typeof(TScope).Name}.");

            signal = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _deliverySignal = signal;
        }

        try
        {
            await Task.Delay(_options.QueryDelay, _timeProvider, signal.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            // A delivery signal intentionally wakes the single waiting agent.
        }
        finally
        {
            lock (_lock)
            {
                if (ReferenceEquals(_deliverySignal, signal))
                {
                    _deliverySignal = null;
                    if (signal.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
                        _deliveryPending = false;
                }
            }

            signal.Dispose();
        }
    }

    /// <summary>Signals that persisted outbox work is available for delivery.</summary>
    public void SignalDelivery()
    {
        lock (_lock)
        {
            _deliveryPending = true;
            _deliverySignal?.Cancel();
        }
    }
}
