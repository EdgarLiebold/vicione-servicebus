using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

#nullable enable
namespace ViciOne.ServiceBus.Middleware.Outbox;

/// <summary>
/// Provides a bus outbox notification implementation.
/// </summary>
/// <typeparam name="TScope">The t scope type.</typeparam>
public class BusOutboxNotification<TScope> :
    IBusOutboxNotification<TScope>
    where TScope : class
{
    readonly object _lock = new();
    readonly OutboxDeliveryServiceOptions<TScope> _options;
    readonly TimeProvider _timeProvider;
    CancellationTokenSource? _deliverySignal;
    bool _deliveryPending;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="options">The options value.</param>
    /// <param name="timeProvider">The time provider value.</param>
    public BusOutboxNotification(IOptions<OutboxDeliveryServiceOptions<TScope>> options, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <summary>
    /// Performs the wait for delivery operation.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Performs the delivered operation.
    /// </summary>
    public void Delivered()
    {
        lock (_lock)
        {
            _deliveryPending = true;
            _deliverySignal?.Cancel();
        }
    }
}
