using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ViciOne.ServiceBus.InMemoryTransport;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Util;

#nullable enable
namespace ViciOne.ServiceBus.Transports.Fabric;

public class MessageQueue<TContext, T> :
    Agent,
    IMessageQueue<TContext, T>
    where T : class
    where TContext : class
{
    readonly Channel<DeliveryContext<T>> _channel;
    readonly IInMemoryDelayProvider _delayProvider;
    readonly SemaphoreSlim _delayedCapacity;
    readonly PendingTaskCollection _delayedDeliveries;
    readonly Task _dispatcher;
    readonly QueueMetric _metrics;
    readonly IMessageFabricObserver<TContext> _observer;
    readonly MessageReceiverCollection<T> _receivers;

    public MessageQueue(IMessageFabricObserver<TContext> observer, string name, IInMemoryDelayProvider delayProvider, int capacity = 1024)
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Queue capacity must be greater than zero.");

        _observer = observer;
        _delayProvider = delayProvider;
        Name = name;

        _receivers = new MessageReceiverCollection<T>(receivers => new RoundRobinReceiverLoadBalancer<T>(receivers));
        _metrics = new QueueMetric(name);
        _delayedCapacity = new SemaphoreSlim(capacity, capacity);
        _delayedDeliveries = new PendingTaskCollection(capacity);

        _channel = Channel.CreateBounded<DeliveryContext<T>>(new BoundedChannelOptions(capacity)
        {
            SingleWriter = false,
            SingleReader = true,
            AllowSynchronousContinuations = false,
            FullMode = BoundedChannelFullMode.Wait
        });

        _dispatcher = StartDispatcherAsync();
    }

    public string Name { get; }

    public TopologyHandle ConnectMessageReceiver(TContext nodeContext, IMessageReceiver<T> receiver)
    {
        try
        {
            var handle = _receivers.Connect(receiver);

            handle = _observer.ConsumerConnected(nodeContext, handle, Name);

            return handle;
        }
        catch (Exception exception)
        {
            throw new ConfigurationException($"Only a single consumer can be connected to a queue: {Name}", exception);
        }
    }

    public async Task DeliverAsync(DeliveryContext<T> context, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); if (context.WasAlreadyDelivered(this))
            return;

        if (context.EnqueueTime.HasValue)
        {
            await _delayedCapacity.WaitAsync(context.CancellationToken).ConfigureAwait(false);

            Task delivery = DeliverWithDelayAsync(context);
            _delayedDeliveries.Add(delivery);
        }
        else
        {
            await _channel.Writer.WriteAsync(context, context.CancellationToken).ConfigureAwait(false);

            _metrics.MessageCount.Add();
        }
    }

    public void Probe(ProbeContext context)
    {
        var scope = context.CreateScope("queue");
        scope.Add("name", Name);

        _receivers.Probe(scope);
    }

    protected override async Task StopAgentAsync(StopContext context)
    {
        await _delayedDeliveries.CompletedAsync().ConfigureAwait(false);

        _channel.Writer.TryComplete();

        await _channel.Reader.Completion.ConfigureAwait(false);

        await _dispatcher.ConfigureAwait(false);

        _delayedCapacity.Dispose();

        await base.StopAgentAsync(context).ConfigureAwait(false);
    }

    async Task DeliverWithDelayAsync(DeliveryContext<T> context)
    {
        var delayed = false;
        try
        {
            if (context.CancellationToken.IsCancellationRequested)
                return;

            DateTimeOffset enqueueTime = context.EnqueueTime
                ?? throw new InvalidOperationException("A delayed delivery requires an enqueue time.");
            if (enqueueTime > _delayProvider.UtcNow)
            {
                _metrics.DelayedMessageCount.Add();
                delayed = true;

                await _delayProvider.DelayAsync(enqueueTime, Stopping).ConfigureAwait(false);
            }

            await _channel.Writer.WriteAsync(context, Stopping).ConfigureAwait(false);

            _metrics.MessageCount.Add();
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
            if (delayed)
                await _metrics.DelayedMessageCount.RemoveAsync().ConfigureAwait(false);

            _delayedCapacity.Release();
        }
    }

    async Task StartDispatcherAsync()
    {
        try
        {
            while (await _channel.Reader.WaitToReadAsync().ConfigureAwait(false))
            {
                if (!_channel.Reader.TryRead(out DeliveryContext<T>? context))
                    continue;

                await _metrics.MessageCount.RemoveAsync().ConfigureAwait(false);

                try
                {
                    IMessageReceiver<T>? receiver = null;

                    if (context.ReceiverId.HasValue)
                    {
                        if (!_receivers.TryGetReceiver(context.ReceiverId.Value, out receiver))
                            LogContext.Debug?.Log("Receiver not found: {Queue}, {ReceiverId}", Name, context.ReceiverId.Value);
                    }

                    receiver ??= await _receivers.NextAsync(context.Message, Stopping).ConfigureAwait(false);
                    if (receiver != null)
                        await receiver.DeliverAsync(context.Message, Stopping).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception exception)
                {
                    LogContext.Warning?.Log(exception, "Failed to dispatch message");
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
