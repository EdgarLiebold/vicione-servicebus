using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Transports.Fabric;

/// <summary>Routes message topic messages through an exchange.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class MessageTopicExchange<T> :
    IMessageExchange<T>
    where T : class
{
    readonly TopicNode<T> _root;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="name">The name.</param>
    /// <param name="comparer">The comparer.</param>
    public MessageTopicExchange(string name, StringComparer? comparer = default)
    {
        Name = name;

        _root = new TopicNode<T>(comparer ?? StringComparer.Ordinal);
    }

    /// <summary>Gets the sinks.</summary>
    public IEnumerable<IMessageSink<T>> Sinks => _root.Sinks;

    /// <summary>Gets the name.</summary>
    public string Name { get; }

    /// <summary>Delivers the current message.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task DeliverAsync(DeliveryContext<T> context, CancellationToken cancellationToken = default)
    {
        var routingKey = context.RoutingKey;

        return _root.DeliverAsync(context, routingKey, cancellationToken: cancellationToken);
    }

    /// <summary>Connects the configured observer or endpoint.</summary>
    /// <param name="sink">The sink.</param>
    /// <param name="routingKey">The routing key.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle Connect(IMessageSink<T> sink, string? routingKey)
    {
        return _root.Add(sink, routingKey);
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        var scope = context.CreateScope("exchange");
        scope.Add("name", Name);
        scope.Add("type", "topic");

        var topicScope = scope.CreateScope("topics");

        _root.Probe(topicScope);
    }

    /// <summary>Returns the string representation of this instance.</summary>
    /// <returns>The converted string.</returns>
    public override string ToString()
    {
        return $"Exchange({Name})";
    }
}
