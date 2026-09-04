using System.Collections.Generic;
using System.Threading;

namespace ViciOne.ServiceBus.Transports.Fabric;

/// <summary>
/// Provides a round robin receiver load balancer implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class RoundRobinReceiverLoadBalancer<T> :
    IReceiverLoadBalancer<T>
    where T : class
{
    Receiver _current;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="receivers">The receivers value.</param>
    public RoundRobinReceiverLoadBalancer(IMessageReceiver<T>[] receivers)
    {
        _current = BuildList(receivers.Copy().Shuffle());
    }

    /// <summary>
    /// Performs the select receiver operation.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <returns>The result of the operation.</returns>
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
