using System.Threading.Channels;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Providers.Transports;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Transports.Fabric;

/// <summary>Buffers messages and dispatches them across the receivers connected to a queue.</summary>
/// <typeparam name="TMessage">The message envelope type stored by the queue.</typeparam>
internal sealed class MessageQueue<TMessage> :
    Agent,
    IMessageQueue<TMessage>
    where TMessage : class
{
    readonly Channel<IMessageDeliveryContext<TMessage>> _channel;
    readonly IInMemoryDelayProvider _delayProvider;
    readonly SemaphoreSlim _delayedCapacity;
    readonly PendingTaskCollection _delayedDeliveries;
    readonly Task _dispatcher;
    readonly MessageReceiverCollection<TMessage> _receivers;

    /// <summary>Initializes a queue with the specified delay service and capacity.</summary>
    /// <param name="name">The queue name.</param>
    /// <param name="delayProvider">The clock and delay service used for scheduled messages.</param>
    /// <param name="capacity">The immediate-message buffer size and the separate delayed-message admission limit.</param>
    public MessageQueue(string name, IInMemoryDelayProvider delayProvider, int capacity = 1024)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(delayProvider);
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Queue capacity must be greater than zero.");

        _delayProvider = delayProvider;
        Name = name;

        _receivers = new MessageReceiverCollection<TMessage>(receivers => new RoundRobinReceiverLoadBalancer<TMessage>(receivers));
        _delayedCapacity = new SemaphoreSlim(capacity, capacity);
        _delayedDeliveries = new PendingTaskCollection(capacity);
        _channel = Channel.CreateBounded<IMessageDeliveryContext<TMessage>>(new BoundedChannelOptions(capacity)
        {
            SingleWriter = false,
            SingleReader = true,
            AllowSynchronousContinuations = false,
            FullMode = BoundedChannelFullMode.Wait
        });

        _dispatcher = StartDispatcherAsync();
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public ITopologyHandle ConnectMessageReceiver(IMessageReceiver<TMessage> receiver)
    {
        ArgumentNullException.ThrowIfNull(receiver);
        return _receivers.Connect(receiver);
    }

    /// <inheritdoc />
    public async Task DeliverAsync(IMessageDeliveryContext<TMessage> context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        using var admission = CancellationTokenSource.CreateLinkedTokenSource(
            context.CancellationToken,
            cancellationToken,
            Stopping);
        try
        {
            if (context.EnqueueTime.HasValue)
            {
                await _delayedCapacity.WaitAsync(admission.Token).ConfigureAwait(false);

                Task delivery = DeliverWithDelayAsync(context);
                _delayedDeliveries.Add(delivery);
                return;
            }

            await _channel.Writer.WriteAsync(context, admission.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(cancellationToken);
        }
        catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(context.CancellationToken);
        }
        catch (OperationCanceledException) when (Stopping.IsCancellationRequested)
        {
            throw new OperationCanceledException(Stopping);
        }
    }

    /// <inheritdoc />
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        ProbeContext scope = context.CreateScope("queue");
        scope.Add("name", Name);
        _receivers.Probe(scope);
    }

    /// <inheritdoc />
    protected override async Task StopAgentAsync(StopContext context)
    {
        await _delayedDeliveries.CompletedAsync().ConfigureAwait(false);

        _channel.Writer.TryComplete();
        await _dispatcher.ConfigureAwait(false);
        await _channel.Reader.Completion.ConfigureAwait(false);

        await base.StopAgentAsync(context).ConfigureAwait(false);
    }

    async Task DeliverWithDelayAsync(IMessageDeliveryContext<TMessage> context)
    {
        try
        {
            using var delivery = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken, Stopping);
            DateTimeOffset enqueueTime = context.EnqueueTime
                ?? throw new InvalidOperationException("A delayed delivery requires an enqueue time.");

            if (enqueueTime > _delayProvider.UtcNow)
                await _delayProvider.DelayAsync(enqueueTime, delivery.Token).ConfigureAwait(false);

            await _channel.Writer.WriteAsync(context, delivery.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            LogContext.Error?.Log(exception, "Message delivery faulted: {Queue}", Name);
        }
        finally
        {
            _delayedCapacity.Release();
        }
    }

    async Task StartDispatcherAsync()
    {
        try
        {
            while (await _channel.Reader.WaitToReadAsync().ConfigureAwait(false))
            {
                if (!_channel.Reader.TryRead(out IMessageDeliveryContext<TMessage>? context))
                    continue;

                try
                {
                    using var dispatch = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken, Stopping);
                    IMessageReceiver<TMessage> receiver = await _receivers.NextAsync(context.Message, dispatch.Token).ConfigureAwait(false);
                    await receiver.DeliverAsync(context.Message, dispatch.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception exception)
                {
                    LogContext.Warning?.Log(exception, "Message dispatch failed: {Queue}", Name);
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            LogContext.Warning?.Log(exception, "Queue dispatcher faulted: {Queue}", Name);
        }
    }
}
