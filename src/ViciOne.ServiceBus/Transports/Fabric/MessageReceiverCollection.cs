using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Transports.Fabric;

/// <summary>Maintains the receivers connected to a queue and exposes their current load balancer.</summary>
/// <typeparam name="TMessage">The message type accepted by the receivers.</typeparam>
internal sealed class MessageReceiverCollection<TMessage> :
    IProbeSite
    where TMessage : class
{
    readonly ReceiverLoadBalancerFactory<TMessage> _balancerFactory;
    readonly Dictionary<long, IMessageReceiver<TMessage>> _receivers = [];
    TaskCompletionSource<IReceiverLoadBalancer<TMessage>> _balancer = TaskCompletionSources.Create<IReceiverLoadBalancer<TMessage>>();
    long _nextId;

    /// <summary>Initializes the collection with the factory used when multiple receivers are connected.</summary>
    /// <param name="balancerFactory">The factory that creates a load balancer for a receiver snapshot.</param>
    public MessageReceiverCollection(ReceiverLoadBalancerFactory<TMessage> balancerFactory)
    {
        ArgumentNullException.ThrowIfNull(balancerFactory);
        _balancerFactory = balancerFactory;
    }

    /// <inheritdoc />
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        IMessageReceiver<TMessage>[] connected;
        lock (_receivers)
            connected = [.. _receivers.Values];

        if (connected.Length == 0)
            return;

        ProbeContext scope = context.CreateScope("receivers");
        foreach (IMessageReceiver<TMessage> receiver in connected)
            receiver.Probe(scope);
    }

    /// <summary>Connects a receiver and rebuilds the load balancer for the resulting receiver set.</summary>
    /// <param name="receiver">The receiver to connect.</param>
    /// <returns>A handle that disconnects the receiver.</returns>
    public ITopologyHandle Connect(IMessageReceiver<TMessage> receiver)
    {
        ArgumentNullException.ThrowIfNull(receiver);

        lock (_receivers)
        {
            long id = checked(++_nextId);
            _receivers.Add(id, receiver);
            PublishBalancer();
            return new ReceiverConnection(id, this);
        }
    }

    /// <summary>Waits for a receiver when necessary and selects one for the message.</summary>
    /// <param name="message">The message being dispatched.</param>
    /// <param name="cancellationToken">The token that cancels the wait.</param>
    /// <returns>The selected receiver.</returns>
    public Task<IMessageReceiver<TMessage>> NextAsync(TMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        cancellationToken.ThrowIfCancellationRequested();

        Task<IReceiverLoadBalancer<TMessage>> task;
        lock (_receivers)
            task = _balancer.Task;

        if (task.IsCompletedSuccessfully)
            return Task.FromResult(task.Result.SelectReceiver(message));

        return SelectAfterConnectionAsync(task, message, cancellationToken);
    }

    static async Task<IMessageReceiver<TMessage>> SelectAfterConnectionAsync(
        Task<IReceiverLoadBalancer<TMessage>> balancerTask,
        TMessage message,
        CancellationToken cancellationToken)
    {
        IReceiverLoadBalancer<TMessage> balancer = await balancerTask.OrCanceledAsync(cancellationToken).ConfigureAwait(false);
        return balancer.SelectReceiver(message);
    }

    void Disconnect(long id)
    {
        lock (_receivers)
        {
            if (!_receivers.Remove(id))
                return;

            _balancer = TaskCompletionSources.Create<IReceiverLoadBalancer<TMessage>>();
            PublishBalancer();
        }
    }

    void PublishBalancer()
    {
        IMessageReceiver<TMessage>[] connected = [.. _receivers.Values];
        if (connected.Length == 0)
            return;

        IReceiverLoadBalancer<TMessage> balancer = connected.Length == 1
            ? new SingleReceiverLoadBalancer<TMessage>(connected[0])
            : _balancerFactory(connected);

        if (!_balancer.TrySetResult(balancer))
        {
            _balancer = TaskCompletionSources.Create<IReceiverLoadBalancer<TMessage>>();
            _balancer.SetResult(balancer);
        }
    }

    sealed class ReceiverConnection :
        ITopologyHandle
    {
        readonly MessageReceiverCollection<TMessage> _collection;

        public ReceiverConnection(long id, MessageReceiverCollection<TMessage> collection)
        {
            Id = id;
            _collection = collection;
        }

        public long Id { get; }

        public void Disconnect() => _collection.Disconnect(Id);
    }
}
