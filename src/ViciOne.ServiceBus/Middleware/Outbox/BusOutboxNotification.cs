#nullable enable
namespace ViciOne.ServiceBus.Middleware.Outbox
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Options;


    public class BusOutboxNotification :
        IBusOutboxNotification
    {
        readonly object _lock = new object();
        readonly IOptions<OutboxDeliveryServiceOptions> _options;
        readonly TimeProvider _timeProvider;
        CancellationTokenSource? _cancellationTokenSource;
        bool _deliveryPending;

        public BusOutboxNotification(IOptions<OutboxDeliveryServiceOptions> options, TimeProvider timeProvider)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        }

        public async Task WaitForDelivery(CancellationToken cancellationToken)
        {
            CancellationTokenSource deliverySignal;
            lock (_lock)
            {
                if (_deliveryPending)
                {
                    _deliveryPending = false;
                    return;
                }

                if (_cancellationTokenSource is not null)
                    throw new InvalidOperationException("Only one outbox delivery waiter may own the notification signal.");

                deliverySignal = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                _cancellationTokenSource = deliverySignal;
            }

            try
            {
                await Task.Delay(_options.Value.QueryDelay, _timeProvider, deliverySignal.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                cancellationToken.ThrowIfCancellationRequested();
                throw;
            }
            catch (OperationCanceledException)
            {
                // Delivered() is the wake-up signal. It is not caller cancellation.
            }
            finally
            {
                lock (_lock)
                {
                    if (ReferenceEquals(_cancellationTokenSource, deliverySignal))
                    {
                        _cancellationTokenSource = null;

                        if (deliverySignal.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
                            _deliveryPending = false;
                    }

                    deliverySignal.Dispose();
                }
            }
        }

        public void Delivered()
        {
            lock (_lock)
            {
                _deliveryPending = true;
                _cancellationTokenSource?.Cancel();
            }
        }
    }
}
