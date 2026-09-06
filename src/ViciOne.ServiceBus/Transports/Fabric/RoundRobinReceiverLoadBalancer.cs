using System.Collections.Generic;
using System.Threading;

namespace ViciOne.ServiceBus.Transports.Fabric;

/// <summary>Balances work across round robin receiver instances.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class RoundRobinReceiverLoadBalancer<T> :
    IReceiverLoadBalancer<T>
    where T : class
{
    Receiver _current;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="receivers">The receivers.</param>
    public RoundRobinReceiverLoadBalancer(IMessageReceiver<T>[] receivers)
    {
        _current = BuildList(receivers.Copy().Shuffle());
    }

    /// <summary>Selects receiver.</summary>
    /// <param name="message">The message to process.</param>
    /// <returns>The selected receiver.</returns>
    public IMessageReceiver<T> SelectReceiver(T message)
    {
        Receiver selected;
        do
        {
            selected = _current;
        }
        while (Interlocked.CompareExchange(ref _current, selected.Next, selected) != selected);

        return selected.Current;
    }

    static Receiver BuildList(IReadOnlyList<IMessageReceiver<T>> receivers)
    {
        var first = new Receiver(receivers[0]);
        var last = first;
        for (var i = 1; i < receivers.Count; i++)
        {
            var consumer = new Receiver(receivers[i]);
            last.Next = consumer;
            last = consumer;
        }

        last.Next = first;

        return last;
    }


    class Receiver
    {
        public Receiver(IMessageReceiver<T> current)
        {
            Current = current;
        }

        public IMessageReceiver<T> Current { get; }
        public Receiver Next { get; set; } = null!;
    }
}
