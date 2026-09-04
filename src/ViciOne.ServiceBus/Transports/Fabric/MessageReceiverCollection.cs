using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Transports.Fabric;

public class MessageReceiverCollection<T> :
    IProbeSite
    where T : class
{
    readonly LoadBalancerFactory<T> _balancerFactory;
    readonly Dictionary<long, IMessageReceiver<T>> _receivers;
    TaskCompletionSource<IReceiverLoadBalancer<T>> _balancer;
    long _nextId;

    public MessageReceiverCollection(LoadBalancerFactory<T> balancerFactory)
    {
        _balancerFactory = balancerFactory;

        _balancer = TaskCompletionSources.Create<IReceiverLoadBalancer<T>>();
        _receivers = new Dictionary<long, IMessageReceiver<T>>();
    }

    public void Probe(ProbeContext context)
    {
        IMessageReceiver<T>[] connected;
        lock (_receivers)
            connected = _receivers.Values.ToArray();

        if (connected.Length == 0)
            return;

        var scope = context.CreateScope("receiver");

        for (var i = 0; i < connected.Length; i++)
            connected[i].Probe(scope);
    }

    public TopologyHandle Connect(IMessageReceiver<T> receiver)
    {
        if (receiver == null)
            throw new ArgumentNullException(nameof(receiver));

        lock (_receivers)
        {
            var id = ++_nextId;

            _receivers.Add(id, receiver);

            IMessageReceiver<T>[] connected = _receivers.Values.ToArray();

            IReceiverLoadBalancer<T> balancer = connected.Length == 1
                ? new SingleReceiverLoadBalancer<T>(connected[0])
                : _balancerFactory(connected);

            if (!_balancer.TrySetResult(balancer))
            {
                _balancer = TaskCompletionSources.Create<IReceiverLoadBalancer<T>>();
                _balancer.SetResult(balancer);
            }

            return new Handle(id, this);
        }
    }

    public Task<IMessageReceiver<T>> NextAsync(T message, CancellationToken cancellationToken)
    {
        Task<IReceiverLoadBalancer<T>> task = _balancer.Task;
        if (task.IsCompletedSuccessfully())
        {
            IReceiverLoadBalancer<T> balancer = task.GetAwaiter().GetResult();
            IMessageReceiver<T> consumer = balancer.SelectReceiver(message);

            return Task.FromResult(consumer);
        }

        async Task<IMessageReceiver<T>> NextAsync()
        {
            IReceiverLoadBalancer<T> balancer = await _balancer.Task.OrCanceledAsync(cancellationToken).ConfigureAwait(false);

            return balancer.SelectReceiver(message);
        }

        return NextAsync();
    }

    public bool TryGetReceiver(long id, [NotNullWhen(true)] out IMessageReceiver<T>? consumer)
    {
        lock (_receivers)
            return _receivers.TryGetValue(id, out consumer);
    }

    void Disconnect(long id)
    {
        lock (_receivers)
        {
            _receivers.Remove(id);

            _balancer = TaskCompletionSources.Create<IReceiverLoadBalancer<T>>();

            IMessageReceiver<T>[] connected = _receivers.Values.ToArray();
            if (connected.Length <= 0)
                return;

            IReceiverLoadBalancer<T> balancer = connected.Length == 1
                ? new SingleReceiverLoadBalancer<T>(connected[0])
                : _balancerFactory(connected);

            _balancer.SetResult(balancer);
        }
    }


    class Handle :
        TopologyHandle
    {
        readonly MessageReceiverCollection<T> _connectable;

        public Handle(long id, MessageReceiverCollection<T> connectable)
        {
            Id = id;
            _connectable = connectable;
        }

        public long Id { get; }

        public void Disconnect()
        {
            _connectable.Disconnect(Id);
        }
    }
}
