using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Provides extension methods for transport start.</summary>
public static class TransportStartExtensions
{
    /// <summary>Handles the notification for transport startup.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="supervisor">The supervisor.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public static async Task OnTransportStartupAsync<T>(this ReceiveEndpointContext context, ITransportSupervisor<T> supervisor,
        CancellationToken cancellationToken)
        where T : class, PipeContext
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, supervisor.ConsumeStopping);

        var pipe = new WaitForConnectionPipe<T>(context, tokenSource.Token);

        await supervisor.SendAsync(pipe, cancellationToken).ConfigureAwait(false);
    }


    class WaitForConnectionPipe<T> :
        IPipe<T>
        where T : class, PipeContext
    {
        readonly ReceiveEndpointContext _context;
        readonly CancellationToken _stopping;

        public WaitForConnectionPipe(ReceiveEndpointContext context, CancellationToken stopping)
        {
            _context = context;
            _stopping = stopping;
        }

        public async Task SendAsync(T context)
        {
            await _context.TransportObservers.NotifyReadyAsync(_context.InputAddress, false).ConfigureAwait(false);

            try
            {
                await _context.ReceivePipe.Connected.OrCanceledAsync(_stopping).ConfigureAwait(false);
            }
            catch (OperationCanceledException ex) when (ex.CancellationToken == _stopping)
            {
                await _context.TransportObservers.NotifyCompletedAsync(_context.InputAddress, Metrics.None).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }

        public void Probe(ProbeContext context)
        {
        }
    }


    class Metrics :
        DeliveryMetrics
    {
        public static readonly DeliveryMetrics None = new Metrics();

        public long DeliveryCount => 0;
        public int ConcurrentDeliveryCount => 0;
    }
}
