using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace ViciOne.ServiceBus.Middleware.Outbox;

/// <summary>Signals changes to bus outbox.</summary>
/// <typeparam name="TScope">The scope type.</typeparam>
public class BusOutboxNotification<TScope> :
    IBusOutboxNotification<TScope>
    where TScope : class
{
    readonly object _lock = new();
    readonly OutboxDeliveryServiceOptions<TScope> _options;
    readonly TimeProvider _timeProvider;
    CancellationTokenSource? _deliverySignal;
    bool _deliveryPending;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="options">The options that control the operation.</param>
    /// <param name="timeProvider">The time source used by the operation.</param>
    public BusOutboxNotification(IOptions<OutboxDeliveryServiceOptions<TScope>> options, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <summary>Waits for for delivery.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task WaitForDeliveryAsync(CancellationToken cancellationToken)
    {
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
            // Delivered() intentionally wakes the single delivery agent.
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

    /// <summary>Delivers ed.</summary>
    public void Delivered()
    {
        lock (_lock)
        {
            _deliveryPending = true;
            _deliverySignal?.Cancel();
        }
    }
}
