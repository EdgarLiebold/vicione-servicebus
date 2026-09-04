using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Transports.Fabric;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.InMemoryTransport;

/// <summary>
/// Support in-memory message queue that is not durable, but supports parallel delivery of messages
/// based on TPL usage.
/// </summary>
public class InMemoryReceiveTransport :
    IReceiveTransport
{
    readonly InMemoryReceiveEndpointContext _context;
    readonly string _queueName;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="queueName">The queue name value.</param>
    public InMemoryReceiveTransport(InMemoryReceiveEndpointContext context, string queueName)
    {
        _context = context;
        _queueName = queueName;
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
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

        IMessageQueue<InMemoryTransportContext, InMemoryTransportMessage> queue =
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
        return _context.ConnectReceiveObserver(observer);
    }

    ConnectHandle IReceiveTransportObserverConnector.ConnectReceiveTransportObserver(IReceiveTransportObserver observer)
    {
        return _context.ConnectReceiveTransportObserver(observer);
    }

    ConnectHandle IPublishObserverConnector.ConnectPublishObserver(IPublishObserver observer)
    {
        return _context.ConnectPublishObserver(observer);
    }

    ConnectHandle ISendObserverConnector.ConnectSendObserver(ISendObserver observer)
    {
        return _context.ConnectSendObserver(observer);
    }


    class ReceiveTransportAgent :
        ConsumerAgent<long>,
        ReceiveTransportHandle,
        IMessageReceiver<InMemoryTransportMessage>
    {
        readonly InMemoryReceiveEndpointContext _context;
        readonly TaskExecutor _executor;
        readonly IMessageQueue<InMemoryTransportContext, InMemoryTransportMessage> _queue;
        readonly Task _startupTask;
        TopologyHandle _topologyHandle = null!;

        public ReceiveTransportAgent(InMemoryReceiveEndpointContext context, IMessageQueue<InMemoryTransportContext, InMemoryTransportMessage> queue)
            : base(context)
        {
            _context = context;
            _queue = queue;

            _executor = new TaskExecutor(context.ConcurrentMessageLimit ?? context.PrefetchCount);

            _startupTask = StartupAsync();
        }

        public Task DeliverAsync(InMemoryTransportMessage message, CancellationToken cancellationToken)
        {
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
