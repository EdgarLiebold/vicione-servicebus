using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Transports.Fabric;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.InMemoryTransport.Runtime;

/// <summary>Connects a bounded in-memory queue to the receive pipeline and dispatches messages with configured concurrency.</summary>
internal sealed class InMemoryReceiveTransport :
    IReceiveTransport
{
    readonly IInMemoryReceiveEndpointContext _context;
    readonly string _queueName;

    /// <summary>Creates a receive transport for one queue.</summary>
    /// <param name="context">The endpoint context that owns topology and observers.</param>
    /// <param name="queueName">The non-empty queue name.</param>
    public InMemoryReceiveTransport(IInMemoryReceiveEndpointContext context, string queueName)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        ArgumentException.ThrowIfNullOrWhiteSpace(queueName);
        _queueName = queueName;
    }

    /// <summary>Adds receive-transport capacity and concurrency settings to a diagnostic probe.</summary>
    /// <param name="context">The probe that receives transport state.</param>
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var scope = context.CreateScope("receiveTransport");
        scope.Add("type", "InMemory");
        scope.Set(new
        {
            Address = _context.InputAddress,
            _context.PrefetchCount,
            _context.ConcurrentMessageLimit
        });
    }

    ReceiveTransportHandle IReceiveTransport.Start()
    {
        _context.ConfigureTopology();

        IMessageQueue<IInMemoryTransportContext, InMemoryTransportMessage> queue =
            _context.MessageFabric.GetQueue(_context.TransportContext, _queueName);

        IDeadLetterTransport deadLetterTransport = new InMemoryMessageDeadLetterTransport(_context.MessageFabric.GetExchange(_context.TransportContext,
            _context.Send.DeadLetterQueueNameFormatter.FormatDeadLetterQueueName(_queueName)), _context.MessageFabric.DelayProvider);
        _context.AddOrUpdatePayload(() => deadLetterTransport, _ => deadLetterTransport);

        IErrorTransport errorTransport = new InMemoryMessageErrorTransport(_context.MessageFabric.GetExchange(_context.TransportContext,
            _context.Send.ErrorQueueNameFormatter.FormatErrorQueueName(_queueName)), _context.MessageFabric.DelayProvider);
        _context.AddOrUpdatePayload(() => errorTransport, _ => errorTransport);

        return new ReceiveTransportAgent(_context, queue);
    }

    ConnectHandle IReceiveObserverConnector.ConnectReceiveObserver(IReceiveObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return _context.ConnectReceiveObserver(observer);
    }

    ConnectHandle IReceiveTransportObserverConnector.ConnectReceiveTransportObserver(IReceiveTransportObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return _context.ConnectReceiveTransportObserver(observer);
    }

    ConnectHandle IPublishObserverConnector.ConnectPublishObserver(IPublishObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return _context.ConnectPublishObserver(observer);
    }

    ConnectHandle ISendObserverConnector.ConnectSendObserver(ISendObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return _context.ConnectSendObserver(observer);
    }


    sealed class ReceiveTransportAgent :
        ConsumerAgent<long>,
        ReceiveTransportHandle,
        IMessageReceiver<InMemoryTransportMessage>
    {
        readonly IInMemoryReceiveEndpointContext _context;
        readonly TaskExecutor _executor;
        readonly IMessageQueue<IInMemoryTransportContext, InMemoryTransportMessage> _queue;
        readonly Task _startupTask;
        TopologyHandle _topologyHandle = null!;

        public ReceiveTransportAgent(IInMemoryReceiveEndpointContext context, IMessageQueue<IInMemoryTransportContext, InMemoryTransportMessage> queue)
            : base(context)
        {
            _context = context;
            _queue = queue;

            _executor = new TaskExecutor(context.ConcurrentMessageLimit ?? context.PrefetchCount);

            _startupTask = StartupAsync();
        }

        public Task DeliverAsync(InMemoryTransportMessage message, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(message);
            if (IsStopping)
                return Task.CompletedTask;

            return _executor.EnqueueAsync(async () =>
            {
                LogContext.Current = _context.LogContext;

                var context = new InMemoryReceiveContext(message, _context);

                try
                {
                    await DispatchAsync(message.SequenceNumber, context, NoLockReceiveContext.Instance).ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    message.DeliveryCount++;
                    context.LogTransportFaulted(exception);
                }
                finally
                {
                    context.Dispose();
                }
            }, cancellationToken);
        }

        public void Probe(ProbeContext context)
        {
            context.CreateScope("inMemory");
        }

        Task ReceiveTransportHandle.StopAsync(CancellationToken cancellationToken)
        {
            return this.StopAsync("Stop Receive Transport", cancellationToken);
        }

        async Task StartupAsync()
        {
            try
            {
                await _context.DependenciesReady.OrCanceledAsync(Stopping).ConfigureAwait(false);

                _topologyHandle = _queue.ConnectMessageReceiver(_context.TransportContext, this);

                await _context.TransportObservers.NotifyReadyAsync(_context.InputAddress).ConfigureAwait(false);

                SetReady();
            }
            catch (OperationCanceledException exception) when (Stopping.IsCancellationRequested)
            {
                SetNotReady(exception);
            }
            catch (Exception exception)
            {
                SetNotReady(exception);

                try
                {
                    await _context.TransportObservers.NotifyFaultedAsync(_context.InputAddress, exception, true).ConfigureAwait(false);
                }
                catch (Exception observerException)
                {
                    LogContext.Warning?.Log(observerException, "In-memory receive startup fault observer failed: {InputAddress}",
                        _context.InputAddress);
                }
            }
        }

        protected override async Task ActiveAndActualAgentsCompletedAsync(StopContext context)
        {
            _topologyHandle?.Disconnect();

            try
            {
                await _startupTask.OrCanceledAsync(context.CancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }

            await base.ActiveAndActualAgentsCompletedAsync(context).ConfigureAwait(false);

            await _executor.DisposeAsync().ConfigureAwait(false);

            await _context.TransportObservers.NotifyCompletedAsync(_context.InputAddress, this).ConfigureAwait(false);

            _context.LogConsumerCompleted(DeliveryCount, ConcurrentDeliveryCount);
        }
    }
}
