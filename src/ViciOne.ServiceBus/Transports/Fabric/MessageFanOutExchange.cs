using System.Collections.Generic;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

#nullable enable
namespace ViciOne.ServiceBus.Transports.Fabric;

/// <summary>
/// Provides a message fan out exchange implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class MessageFanOutExchange<T> :
    IMessageExchange<T>
    where T : class
{
    readonly Connectable<IMessageSink<T>> _sinks;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="name">The name value.</param>
    public MessageFanOutExchange(string name)
    {
        Name = name;

        _sinks = new Connectable<IMessageSink<T>>();
    }

    /// <summary>
    /// Gets the sinks value.
    /// </summary>
    public IEnumerable<IMessageSink<T>> Sinks
    {
        get
        {
            var sinks = new List<IMessageSink<T>>();
            _sinks.ForEach(s => sinks.Add(s));

            return sinks;
        }
    }

    /// <summary>
    /// Gets the name value.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Performs the deliver operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Performs the connect operation.
    /// </summary>
    /// <param name="sink">The sink value.</param>
    /// <param name="routingKey">The routing key value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle Connect(IMessageSink<T> sink, string? routingKey)
    {
        return _sinks.Connect(sink);
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        var scope = context.CreateScope("exchange");
        scope.Add("name", Name);
        scope.Add("type", "fanOut");

        var sinkScope = scope.CreateScope("sinks");

        _sinks.ForEach(s => s.Probe(sinkScope));
    }

    /// <summary>
    /// Returns the string representation of this instance.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public override string ToString()
    {
        return $"Exchange({Name})";
    }
}
