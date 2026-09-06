using System.Collections.Generic;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Transports.Fabric;

/// <summary>Routes message fan out messages through an exchange.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class MessageFanOutExchange<T> :
    IMessageExchange<T>
    where T : class
{
    readonly Connectable<IMessageSink<T>> _sinks;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="name">The name.</param>
    public MessageFanOutExchange(string name)
    {
        Name = name;

        _sinks = new Connectable<IMessageSink<T>>();
    }

    /// <summary>Gets the sinks.</summary>
    public IEnumerable<IMessageSink<T>> Sinks
    {
        get
        {
            var sinks = new List<IMessageSink<T>>();
            _sinks.ForEach(s => sinks.Add(s));

            return sinks;
        }
    }

    /// <summary>Gets the name.</summary>
    public string Name { get; }

    /// <summary>Delivers the current message.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task DeliverAsync(DeliveryContext<T> context, CancellationToken cancellationToken = default)
    {
        return _sinks.ForEachAsync(async sink =>
        {
            if (context.WasAlreadyDelivered(sink))
                return;

            await sink.DeliverAsync(context, cancellationToken: cancellationToken).ConfigureAwait(false);

            context.Delivered(sink);
        }, cancellationToken: cancellationToken);
    }

    /// <summary>Connects the configured observer or endpoint.</summary>
    /// <param name="sink">The sink.</param>
    /// <param name="routingKey">The routing key.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle Connect(IMessageSink<T> sink, string? routingKey)
    {
        return _sinks.Connect(sink);
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        var scope = context.CreateScope("exchange");
        scope.Add("name", Name);
        scope.Add("type", "fanOut");

        var sinkScope = scope.CreateScope("sinks");

        _sinks.ForEach(s => s.Probe(sinkScope));
    }

    /// <summary>Returns the string representation of this instance.</summary>
    /// <returns>The converted string.</returns>
    public override string ToString()
    {
        return $"Exchange({Name})";
    }
}
