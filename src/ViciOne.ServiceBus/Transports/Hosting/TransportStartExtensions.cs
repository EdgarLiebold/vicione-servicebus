using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Coordinates receive-pipeline availability notifications during transport startup.</summary>
public static class TransportStartExtensions
{
    /// <summary>Reports transport availability and waits for the receive pipeline to connect.</summary>
    /// <typeparam name="T">The transport pipe context type.</typeparam>
    /// <param name="context">The receive endpoint being started.</param>
    /// <param name="supervisor">The transport supervisor that supplies the connected context.</param>
    /// <param name="cancellationToken">The token that cancels startup.</param>
    /// <returns>A task that completes when the receive pipeline connects or the transport stops.</returns>
    public static async Task OnTransportStartupAsync<T>(this ReceiveEndpointContext context, ITransportSupervisor<T> supervisor,
        CancellationToken cancellationToken)
        where T : class, PipeContext
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(supervisor);

        CancellationToken transportStopping = supervisor.ConsumeStopping;
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, transportStopping);

        var pipe = new WaitForConnectionPipe<T>(context, cancellationToken, transportStopping, tokenSource.Token);

        Task startupTask = supervisor.SendAsync(pipe, cancellationToken)
            ?? throw new InvalidOperationException("The transport supervisor returned no startup task.");
        await startupTask.ConfigureAwait(false);
    }


    sealed class WaitForConnectionPipe<T> :
        IPipe<T>
        where T : class, PipeContext
    {
        readonly CancellationToken _callerCancellation;
        readonly ReceiveEndpointContext _context;
        readonly CancellationToken _transportStopping;
        readonly CancellationToken _waitCancellation;

        public WaitForConnectionPipe(ReceiveEndpointContext context, CancellationToken callerCancellation,
            CancellationToken transportStopping, CancellationToken waitCancellation)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _callerCancellation = callerCancellation;
            _transportStopping = transportStopping;
            _waitCancellation = waitCancellation;
        }

        public async Task SendAsync(T context)
        {
            ArgumentNullException.ThrowIfNull(context);
            await _context.TransportObservers.NotifyReadyAsync(_context.InputAddress, false).ConfigureAwait(false);

            try
            {
                await _context.ReceivePipe.Connected.OrCanceledAsync(_waitCancellation).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (_transportStopping.IsCancellationRequested)
            {
                await _context.TransportObservers.NotifyCompletedAsync(_context.InputAddress, Metrics.None).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                _callerCancellation.ThrowIfCancellationRequested();
                throw;
            }
        }

        public void Probe(ProbeContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
        }
    }


    sealed class Metrics :
        IDeliveryMetrics
    {
        public static IDeliveryMetrics None { get; } = new Metrics();

        public long DeliveryCount => 0;
        public int MaxConcurrentDeliveryCount => 0;
    }
}
