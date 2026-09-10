namespace ViciOne.ServiceBus.Transports.Fabric;

/// <summary>Selects connected receivers in a stable round-robin sequence.</summary>
/// <typeparam name="TMessage">The message type accepted by the receivers.</typeparam>
internal sealed class RoundRobinReceiverLoadBalancer<TMessage> :
    IReceiverLoadBalancer<TMessage>
    where TMessage : class
{
    Receiver _current;

    /// <summary>Initializes a load balancer over the specified receiver snapshot.</summary>
    /// <param name="receivers">The receivers included in the rotation.</param>
    public RoundRobinReceiverLoadBalancer(IMessageReceiver<TMessage>[] receivers)
    {
        ArgumentNullException.ThrowIfNull(receivers);
        if (receivers.Length == 0)
            throw new ArgumentException("At least one receiver is required.", nameof(receivers));
        if (Array.Exists(receivers, static receiver => receiver is null))
            throw new ArgumentException("The receiver collection cannot contain null entries.", nameof(receivers));

        _current = BuildRing(receivers);
    }

    /// <inheritdoc />
    public IMessageReceiver<TMessage> SelectReceiver(TMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        Receiver selected;
        do
        {
            selected = _current;
        }
        while (Interlocked.CompareExchange(ref _current, selected.Next, selected) != selected);

        return selected.Current;
    }

    static Receiver BuildRing(IReadOnlyList<IMessageReceiver<TMessage>> receivers)
    {
        var first = new Receiver(receivers[0]);
        Receiver last = first;
        for (var index = 1; index < receivers.Count; index++)
        {
            var next = new Receiver(receivers[index]);
            last.Next = next;
            last = next;
        }

        last.Next = first;
        return first;
    }

    sealed class Receiver
    {
        public Receiver(IMessageReceiver<TMessage> current)
        {
            Current = current;
            Next = this;
        }

        public IMessageReceiver<TMessage> Current { get; }
        public Receiver Next { get; set; }
    }
}
